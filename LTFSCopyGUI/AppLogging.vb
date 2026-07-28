Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Threading

Public Enum AppLogLevel
    Debug = 0
    Info = 1
    Warning = 2
    [Error] = 3
    Critical = 4
End Enum

Friend Enum LogQueueItemKind
    Message
    Flush
    StopWorker
End Enum

Friend Structure LogQueueItem
    Public Kind As LogQueueItemKind
    Public Text As String
    Public Barrier As ManualResetEventSlim
End Structure

' A single-consumer, non-blocking producer sink. The queue is deliberately
' unbounded: logging must not throttle tape I/O or lose messages under load.
Friend NotInheritable Class AsyncTextLogSink
    Implements IDisposable

    Private Const WriterBufferSize As Integer = 64 * 1024
    Private Const MaximumItemsPerWake As Integer = 512
    Private Const PeriodicFlushMilliseconds As Integer = 1000

    Private ReadOnly _queue As New ConcurrentQueue(Of LogQueueItem)()
    Private ReadOnly _wake As New AutoResetEvent(False)
    Private ReadOnly _worker As Thread
    Private ReadOnly _baseFilePath As String
    Private ReadOnly _maximumFileBytes As Long
    Private ReadOnly _failureLock As New Object()
    Private _writer As StreamWriter
    Private _currentFileBytes As Long
    Private _fileSequence As Integer
    Private _accepting As Integer = 1
    Private _activeEnqueueCount As Integer
    Private _lastFailure As Exception
    Private _shutdownStarted As Integer

    Public Sub New(baseFilePath As String, maximumFileBytes As Long, workerName As String)
        If String.IsNullOrWhiteSpace(baseFilePath) Then Throw New ArgumentException("A log file path is required.", NameOf(baseFilePath))
        If maximumFileBytes <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(maximumFileBytes))
        _baseFilePath = baseFilePath
        _maximumFileBytes = maximumFileBytes
        _worker = New Thread(AddressOf WorkerLoop) With {
            .IsBackground = True,
            .Name = workerName
        }
        _worker.Start()
    End Sub

    Public ReadOnly Property CurrentFilePath As String
        Get
            Return GetFilePath(_fileSequence)
        End Get
    End Property

    Public ReadOnly Property LastFailure As Exception
        Get
            SyncLock _failureLock
                Return _lastFailure
            End SyncLock
        End Get
    End Property

    Public Function Enqueue(text As String) As Boolean
        If text Is Nothing Then text = String.Empty
        Interlocked.Increment(_activeEnqueueCount)
        Try
            If Interlocked.CompareExchange(_accepting, 0, 0) = 0 Then Return False
            _queue.Enqueue(New LogQueueItem With {
                .Kind = LogQueueItemKind.Message,
                .Text = EnsureTrailingNewLine(text)
            })
            _wake.Set()
            Return True
        Finally
            Interlocked.Decrement(_activeEnqueueCount)
        End Try
    End Function

    Public Function Flush(timeout As TimeSpan) As Boolean
        If timeout < TimeSpan.Zero Then Throw New ArgumentOutOfRangeException(NameOf(timeout))
        If Interlocked.CompareExchange(_shutdownStarted, 0, 0) <> 0 Then
            Return _queue.IsEmpty AndAlso LastFailure Is Nothing
        End If

        Dim barrier As New ManualResetEventSlim(False)
        _queue.Enqueue(New LogQueueItem With {
            .Kind = LogQueueItemKind.Flush,
            .Barrier = barrier
        })
        _wake.Set()
        Dim completed = barrier.Wait(timeout)
        If completed Then barrier.Dispose()
        Return completed AndAlso LastFailure Is Nothing
    End Function

    Public Function Shutdown(timeout As TimeSpan) As Boolean
        If timeout < TimeSpan.Zero Then Throw New ArgumentOutOfRangeException(NameOf(timeout))
        If Interlocked.Exchange(_shutdownStarted, 1) <> 0 Then
            Return Not _worker.IsAlive AndAlso LastFailure Is Nothing
        End If

        Interlocked.Exchange(_accepting, 0)
        Dim waitWatch = Stopwatch.StartNew()
        While Interlocked.CompareExchange(_activeEnqueueCount, 0, 0) <> 0
            If waitWatch.Elapsed >= timeout Then Return False
            Thread.Yield()
        End While

        Dim remaining = timeout - waitWatch.Elapsed
        If remaining < TimeSpan.Zero Then remaining = TimeSpan.Zero
        Dim barrier As New ManualResetEventSlim(False)
        _queue.Enqueue(New LogQueueItem With {
            .Kind = LogQueueItemKind.StopWorker,
            .Barrier = barrier
        })
        _wake.Set()
        Dim stopped = barrier.Wait(remaining)
        If stopped Then barrier.Dispose()
        If stopped AndAlso _worker.IsAlive Then
            remaining = timeout - waitWatch.Elapsed
            If remaining > TimeSpan.Zero Then _worker.Join(remaining)
        End If
        Return stopped AndAlso Not _worker.IsAlive AndAlso LastFailure Is Nothing
    End Function

    Private Sub WorkerLoop()
        Dim lastFlush = Stopwatch.StartNew()
        Try
            Do
                _wake.WaitOne(250)
                Dim processed As Integer = 0
                Dim item As New LogQueueItem()
                While processed < MaximumItemsPerWake AndAlso _queue.TryDequeue(item)
                    processed += 1
                    Select Case item.Kind
                        Case LogQueueItemKind.Message
                            SafeWrite(item.Text)
                        Case LogQueueItemKind.Flush
                            SafeFlush()
                            lastFlush.Restart()
                            If item.Barrier IsNot Nothing Then item.Barrier.Set()
                        Case LogQueueItemKind.StopWorker
                            SafeFlush()
                            SafeCloseWriter()
                            If item.Barrier IsNot Nothing Then item.Barrier.Set()
                            Return
                    End Select
                End While

                If lastFlush.ElapsedMilliseconds >= PeriodicFlushMilliseconds Then
                    SafeFlush()
                    lastFlush.Restart()
                End If
                If Not _queue.IsEmpty Then _wake.Set()
            Loop
        Catch ex As Exception
            RememberFailure(ex)
            Debug.WriteLine($"Logging worker failed: {ex}")
        Finally
            SafeCloseWriter()
        End Try
    End Sub

    Private Sub SafeWrite(text As String)
        Try
            Dim byteCount = Encoding.UTF8.GetByteCount(text)
            EnsureWriter()
            If _currentFileBytes > 0 AndAlso _currentFileBytes + byteCount > _maximumFileBytes Then
                SafeCloseWriter()
                _fileSequence += 1
                EnsureWriter()
            End If
            _writer.Write(text)
            _currentFileBytes += byteCount
        Catch ex As Exception
            RememberFailure(ex)
            Debug.WriteLine($"Writing log failed: {ex}")
            SafeCloseWriter()
        End Try
    End Sub

    Private Sub EnsureWriter()
        If _writer IsNot Nothing Then Return
        Dim path = GetFilePath(_fileSequence)
        Dim directory = IO.Path.GetDirectoryName(path)
        If Not String.IsNullOrEmpty(directory) Then IO.Directory.CreateDirectory(directory)
        Dim stream As New FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, WriterBufferSize, FileOptions.SequentialScan)
        _currentFileBytes = stream.Length
        _writer = New StreamWriter(stream, New UTF8Encoding(False), WriterBufferSize) With {.AutoFlush = False}
    End Sub

    Private Sub SafeFlush()
        Try
            If _writer IsNot Nothing Then _writer.Flush()
        Catch ex As Exception
            RememberFailure(ex)
            Debug.WriteLine($"Flushing log failed: {ex}")
            SafeCloseWriter()
        End Try
    End Sub

    Private Sub SafeCloseWriter()
        If _writer Is Nothing Then Return
        Try
            _writer.Flush()
            _writer.Dispose()
        Catch ex As Exception
            RememberFailure(ex)
            Debug.WriteLine($"Closing log failed: {ex}")
        Finally
            _writer = Nothing
            _currentFileBytes = 0
        End Try
    End Sub

    Private Sub RememberFailure(ex As Exception)
        SyncLock _failureLock
            _lastFailure = ex
        End SyncLock
    End Sub

    Private Function GetFilePath(sequence As Integer) As String
        If sequence = 0 Then Return _baseFilePath
        Dim directory = Path.GetDirectoryName(_baseFilePath)
        Dim fileName = Path.GetFileNameWithoutExtension(_baseFilePath)
        Dim extension = Path.GetExtension(_baseFilePath)
        Return Path.Combine(directory, $"{fileName}.{sequence:000}{extension}")
    End Function

    Private Shared Function EnsureTrailingNewLine(text As String) As String
        If text.EndsWith(vbCrLf, StringComparison.Ordinal) Then Return text
        If text.EndsWith(vbLf, StringComparison.Ordinal) Then Return text.TrimEnd(ChrW(10), ChrW(13)) & vbCrLf
        Return text & vbCrLf
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        Dim stopped = Shutdown(TimeSpan.FromSeconds(5))
        If stopped OrElse Not _worker.IsAlive Then _wake.Dispose()
    End Sub
End Class

Public NotInheritable Class TraceLogSession
    Implements IDisposable

    Private ReadOnly _sink As AsyncTextLogSink
    Private ReadOnly _onDisposed As Action(Of TraceLogSession)
    Private _disposed As Integer

    Friend Sub New(sink As AsyncTextLogSink, onDisposed As Action(Of TraceLogSession))
        _sink = sink
        _onDisposed = onDisposed
    End Sub

    Public ReadOnly Property CurrentFilePath As String
        Get
            Return _sink.CurrentFilePath
        End Get
    End Property

    Public Function Write(text As String) As Boolean
        If Interlocked.CompareExchange(_disposed, 0, 0) <> 0 Then Return False
        Return _sink.Enqueue(text)
    End Function

    Public Function Flush(timeout As TimeSpan) As Boolean
        Return _sink.Flush(timeout)
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        If Interlocked.Exchange(_disposed, 1) <> 0 Then Return
        _sink.Dispose()
        If _onDisposed IsNot Nothing Then _onDisposed(Me)
    End Sub

    Friend Function Shutdown(timeout As TimeSpan) As Boolean
        If Interlocked.Exchange(_disposed, 1) <> 0 Then Return _sink.LastFailure Is Nothing
        Return _sink.Shutdown(timeout)
    End Function
End Class

Public NotInheritable Class AppLogger
    Private Const MainLogMaximumBytes As Long = 128L * 1024L * 1024L
    Private Const TraceLogMaximumBytes As Long = 1024L * 1024L * 1024L
    Private Shared ReadOnly StateLock As New Object()
    Private Shared ReadOnly TraceSessions As New List(Of TraceLogSession)()
    Private Shared _mainSink As AsyncTextLogSink
    Private Shared _logDirectory As String
    Private Shared _informationEnabled As Integer
    Private Shared _runId As String
    Private Shared _processExitRegistered As Boolean

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property RunId As String
        Get
            EnsureInitialized()
            Return _runId
        End Get
    End Property

    Public Shared ReadOnly Property CurrentLogFile As String
        Get
            EnsureInitialized()
            Return _mainSink.CurrentFilePath
        End Get
    End Property

    Public Shared Property InformationEnabled As Boolean
        Get
            Return Interlocked.CompareExchange(_informationEnabled, 0, 0) <> 0
        End Get
        Set(value As Boolean)
            Interlocked.Exchange(_informationEnabled, If(value, 1, 0))
        End Set
    End Property

    Public Shared Sub Initialize(logDirectory As String, informationEnabled As Boolean)
        SyncLock StateLock
            If _mainSink IsNot Nothing Then
                informationEnabled = informationEnabled
                Return
            End If
            _logDirectory = ResolveLogDirectory(logDirectory)
            _runId = Guid.NewGuid().ToString("N").Substring(0, 8)
            informationEnabled = informationEnabled
            Dim stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff", CultureInfo.InvariantCulture)
            Dim path = IO.Path.Combine(_logDirectory, $"LTFSCopyGUI_{stamp}.log")
            _mainSink = New AsyncTextLogSink(path, MainLogMaximumBytes, "LTFSCopyGUI main log")
            If Not _processExitRegistered Then
                AddHandler AppDomain.CurrentDomain.ProcessExit, AddressOf OnProcessExit
                _processExitRegistered = True
            End If
        End SyncLock
        Write(AppLogLevel.Info, "Application", "Logging initialized.", sessionId:=_runId, force:=True)
    End Sub

    Public Shared Sub Write(level As AppLogLevel,
                            category As String,
                            message As String,
                            Optional ex As Exception = Nothing,
                            Optional sessionId As String = Nothing,
                            Optional force As Boolean = False)
        If Not force AndAlso level < AppLogLevel.Warning AndAlso Not InformationEnabled Then Return
        EnsureInitialized()
        Dim sink As AsyncTextLogSink
        SyncLock StateLock
            sink = _mainSink
        End SyncLock
        If sink Is Nothing Then Return
        sink.Enqueue(FormatRecord(level, category, message, ex, sessionId))
    End Sub

    Public Shared Function CreateTraceSession(name As String, Optional sessionId As String = Nothing) As TraceLogSession
        EnsureInitialized()
        Dim safeName = MakeSafeFileName(If(String.IsNullOrWhiteSpace(name), "trace", name))
        Dim stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff", CultureInfo.InvariantCulture)
        Dim idPart = If(String.IsNullOrWhiteSpace(sessionId), String.Empty, $"_{MakeSafeFileName(sessionId)}")
        Dim path = IO.Path.Combine(_logDirectory, "trace", $"{safeName}{idPart}_{stamp}.log")
        Dim sink As New AsyncTextLogSink(path, TraceLogMaximumBytes, $"LTFSCopyGUI {safeName} trace")
        Dim result As TraceLogSession = Nothing
        result = New TraceLogSession(sink, AddressOf UnregisterTraceSession)
        SyncLock StateLock
            TraceSessions.Add(result)
        End SyncLock
        Return result
    End Function

    Public Shared Function Flush(timeout As TimeSpan) As Boolean
        Dim sink As AsyncTextLogSink
        Dim traces As TraceLogSession()
        SyncLock StateLock
            sink = _mainSink
            traces = TraceSessions.ToArray()
        End SyncLock
        Dim deadline = Stopwatch.StartNew()
        Dim success = sink Is Nothing OrElse sink.Flush(timeout)
        For Each trace In traces
            Dim remaining = timeout - deadline.Elapsed
            If remaining <= TimeSpan.Zero OrElse Not trace.Flush(remaining) Then success = False
        Next
        Return success
    End Function

    Public Shared Function Shutdown(timeout As TimeSpan) As Boolean
        Dim sink As AsyncTextLogSink
        Dim traces As TraceLogSession()
        SyncLock StateLock
            sink = _mainSink
            _mainSink = Nothing
            traces = TraceSessions.ToArray()
            TraceSessions.Clear()
        End SyncLock
        Dim deadline = Stopwatch.StartNew()
        Dim success As Boolean = True
        For Each trace In traces
            Dim remaining = timeout - deadline.Elapsed
            If remaining <= TimeSpan.Zero Then
                success = False
                Exit For
            End If
            If Not trace.Shutdown(remaining) Then success = False
        Next
        If sink IsNot Nothing Then
            Dim remaining = timeout - deadline.Elapsed
            If remaining <= TimeSpan.Zero OrElse Not sink.Shutdown(remaining) Then success = False
        End If
        Return success
    End Function

    Private Shared Sub EnsureInitialized()
        If _mainSink IsNot Nothing Then Return
        SyncLock StateLock
            If _mainSink IsNot Nothing Then Return
            Dim defaultDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log")
            Initialize(defaultDirectory, False)
        End SyncLock
    End Sub

    Private Shared Function ResolveLogDirectory(requestedDirectory As String) As String
        Dim fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LTFSCopyGUI", "log")
        Dim temporary = Path.Combine(Path.GetTempPath(), "LTFSCopyGUI", "log")
        For Each candidate In {requestedDirectory, fallback, temporary}
            If String.IsNullOrWhiteSpace(candidate) Then Continue For
            If CanWriteDirectory(candidate) Then Return candidate
        Next
        ' Keep logging non-fatal. The worker will report the write failure via
        ' LastFailure while producers remain isolated from the I/O exception.
        Return If(String.IsNullOrWhiteSpace(requestedDirectory), fallback, requestedDirectory)
    End Function

    Private Shared Function CanWriteDirectory(directory As String) As Boolean
        Dim probePath As String = Nothing
        Try
            IO.Directory.CreateDirectory(directory)
            probePath = Path.Combine(directory, $".log-probe-{Guid.NewGuid():N}.tmp")
            Using probe As New FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)
                probe.WriteByte(0)
            End Using
            Return True
        Catch
            Return False
        Finally
            If probePath IsNot Nothing Then
                Try
                    If File.Exists(probePath) Then File.Delete(probePath)
                Catch
                End Try
            End If
        End Try
    End Function

    Private Shared Function FormatRecord(level As AppLogLevel,
                                         category As String,
                                         message As String,
                                         ex As Exception,
                                         sessionId As String) As String
        Dim timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture)
        Dim levelText = level.ToString().ToUpperInvariant()
        Dim categoryText = If(String.IsNullOrWhiteSpace(category), "General", category.Trim())
        Dim sessionText = If(String.IsNullOrWhiteSpace(sessionId), _runId, sessionId.Trim())
        Dim body = If(message, String.Empty)
        If ex IsNot Nothing Then
            If body.Length > 0 Then body &= vbCrLf
            body &= ex.ToString()
        End If
        body = NormalizeContinuationLines(body)
        Return $"{timestamp} [{levelText}] [{categoryText}] [session={sessionText}] [thread={Thread.CurrentThread.ManagedThreadId}] {body}"
    End Function

    Private Shared Function NormalizeContinuationLines(value As String) As String
        Dim normalized = value.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Return normalized.Replace(vbLf, vbCrLf & "    ")
    End Function

    Private Shared Function MakeSafeFileName(value As String) As String
        Dim invalid = Path.GetInvalidFileNameChars()
        Dim builder As New StringBuilder(value.Length)
        For Each ch In value
            builder.Append(If(Array.IndexOf(invalid, ch) >= 0, "_"c, ch))
        Next
        Return builder.ToString()
    End Function

    Private Shared Sub UnregisterTraceSession(session As TraceLogSession)
        SyncLock StateLock
            TraceSessions.Remove(session)
        End SyncLock
    End Sub

    Private Shared Sub OnProcessExit(sender As Object, e As EventArgs)
        Shutdown(TimeSpan.FromSeconds(2))
    End Sub
End Class
