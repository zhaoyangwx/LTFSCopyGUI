Imports System
Imports System.Collections.Concurrent
Imports System.IO
Imports System.IO.Pipelines
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Buffers

' 高性能文件数据提供器：
' - 仅暴露一个 PipeReader（单读者），内部 Pipe 使用 256MiB 背压阈值（可配），避免过量内存占用
' - 小文件(<16KiB)积极缓存到内存，最多缓存 1000 个，超出则排队等待
' - 大文件采用 FileStream 异步顺序读取，按顺序积极写入 Pipe（默认开启），以充分利用 256MiB 管线缓存
' - 通过 AutoResetEvent 可选地控制文件间的连续处理（当 requireSignal=True 时，消费者每完成一个文件需调用 RequestNextFile 开始下一个）
' - 生产端自动在后台填充小文件缓存，并顺序将（小/大）文件内容写入 Pipe
' 用法建议（示意）：
'   ' 连续积极缓存（默认）：
'   Dim provider = New FileDataProvider(WriteList)
'   provider.Start()
'   For Each fr In WriteList
'       Dim remaining = fr.File.length
'       While remaining > 0
'           Dim result = Await provider.Reader.ReadAsync()
'           Dim buffer = result.Buffer
'           Dim toConsume = Math.Min(remaining, buffer.Length)
'           Dim slice = buffer.Slice(0, toConsume)
'           ' 将 slice 写入磁带（略）
'           provider.Reader.AdvanceTo(slice.End, slice.End)
'           remaining -= toConsume
'           If result.IsCompleted AndAlso remaining > 0 Then Throw New EndOfStreamException()
'       End While
'   Next
'   Await provider.CompleteAsync()
'   ' 事件驱动（与旧设计兼容）：
'   Dim provider2 = New FileDataProvider(WriteList, requireSignal:=True)
'   provider2.Start()
'   For i = 0 To WriteList.Count - 1
'       provider2.RequestNextFile()
'       ' 同上消费逻辑...

Public Class FileDataProvider
    Private ReadOnly _pipe As Pipe
    Private ReadOnly _writer As PipeWriter
    Public ReadOnly Property Reader As PipeReader

    Private ReadOnly _writeList As List(Of LTFSWriter.FileRecord)
    Private ReadOnly _smallThreshold As Integer
    Private ReadOnly _smallCacheCapacity As Integer
    Private ReadOnly _requireSignal As Boolean

    Private ReadOnly _smallCacheQueue As New ConcurrentQueue(Of Tuple(Of LTFSWriter.FileRecord, Byte()))
    Private ReadOnly _smallCacheMap As New ConcurrentDictionary(Of LTFSWriter.FileRecord, Byte())

    Private ReadOnly _nextFileSignal As New AutoResetEvent(False)
    Private ReadOnly _cts As New CancellationTokenSource()

    Private _currentIndex As Integer = -1
    Private _started As Integer = 0
    Private _current As LTFSWriter.FileRecord = Nothing

    Public ReadOnly Property Current As LTFSWriter.FileRecord
        Get
            Return _current
        End Get
    End Property

    ' 参数：
    ' - pipeBufferMiB: Pipe 背压阈值（默认 256MiB）
    ' - smallThresholdBytes: 小文件阈值（默认 16KiB）
    ' - smallCacheCapacity: 小文件缓存容量上限（默认 1000 个）
    ' - requireSignal: 是否需要外部通过 RequestNextFile 触发下一个文件（默认 False=积极连续缓存）
    Public Sub New(writeList As IEnumerable(Of LTFSWriter.FileRecord),
                   Optional pipeBufferBytes As Integer = 256 << 20,
                   Optional smallThresholdBytes As Integer = 16 * 1024,
                   Optional smallCacheCapacity As Integer = 1000,
                   Optional requireSignal As Boolean = False)
        _writeList = writeList.ToList()
        _smallThreshold = Math.Max(1, smallThresholdBytes)
        _smallCacheCapacity = Math.Max(1, smallCacheCapacity)
        _requireSignal = requireSignal

        Dim pause As Long = pipeBufferBytes
        Dim resumeTh As Long = Math.Max(1L, pause \ 2)
        _pipe = New Pipe(New PipeOptions(
            pauseWriterThreshold:=pause,
            resumeWriterThreshold:=resumeTh,
            minimumSegmentSize:=64 * 1024,
            useSynchronizationContext:=False
        ))
        Reader = _pipe.Reader
        _writer = _pipe.Writer
    End Sub

    Public Sub Start()
        If Interlocked.Exchange(_started, 1) <> 0 Then Return
        Task.Run(AddressOf PreloadSmallFilesAsync)
        Task.Run(AddressOf ProducerLoopAsync)
        ' 积极模式下，立即允许开始
        If Not _requireSignal Then _nextFileSignal.Set()
    End Sub

    ' 由消费者调用，指示可以开始传输下一个文件（仅当 requireSignal=True 时需要）
    Public Sub RequestNextFile()
        _nextFileSignal.Set()
    End Sub

    Public Sub Cancel()
        _cts.Cancel()
        _nextFileSignal.Set()
    End Sub

    Public Async Function CompleteAsync() As Task
        Try
            _cts.Cancel()
            _nextFileSignal.Set()
        Finally
            Try
                _writer.Complete()
            Catch
            End Try
        End Try
    End Function

    Private Async Function ProducerLoopAsync() As Task
        Try
            While Not _cts.IsCancellationRequested
                ' requireSignal=True 时按事件推进；否则积极连续推进
                If _requireSignal Then
                    _nextFileSignal.WaitOne()
                    If _cts.IsCancellationRequested Then Exit While
                End If

                Dim nextIdx As Integer = Interlocked.Increment(_currentIndex)
                If nextIdx >= _writeList.Count Then Exit While

                Dim fr As LTFSWriter.FileRecord = _writeList(nextIdx)
                _current = fr

                If fr.File IsNot Nothing AndAlso fr.File.length < _smallThreshold Then
                    ' 小文件：优先从缓存获取，否则即时读取
                    Dim data As Byte() = Nothing
                    If Not _smallCacheMap.TryRemove(fr, data) Then
                        data = ReadAllBytesSafe(fr)
                    Else
                        ' 从队列中移除同一个条目（可选，防止长时间积压）
                        Dim tmp As Tuple(Of LTFSWriter.FileRecord, Byte()) = Nothing
                        While _smallCacheQueue.TryDequeue(tmp)
                            If tmp IsNot Nothing AndAlso tmp.Item1 Is fr Then Exit While
                        End While
                    End If

                    If data IsNot Nothing AndAlso data.Length > 0 Then
                        _writer.Write(data.AsSpan())
                        Dim res = Await _writer.FlushAsync(_cts.Token)
                        If res.IsCanceled OrElse res.IsCompleted Then Exit While
                    End If
                Else
                    ' 大文件：流式拷贝到 Pipe（积极缓存，受 Pipe 背压调节）
                    Await StreamFileToPipeAsync(fr, _cts.Token)
                End If

                ' 在积极模式下，自动继续下一个文件；在信号模式下，等待下一次 RequestNextFile
                If _requireSignal = False Then
                    ' 继续循环即可
                End If
            End While
        Catch ex As Exception
            Try
                _writer.Complete()
            Catch
            End Try
        End Try
    End Function

    Private Function ReadAllBytesSafe(fr As LTFSWriter.FileRecord) As Byte()
        Try
            Return fr.ReadAllBytes()
        Catch
            Try
                Return File.ReadAllBytes(fr.SourcePath)
            Catch
                Return Array.Empty(Of Byte)()
            End Try
        End Try
    End Function

    Private Async Function StreamFileToPipeAsync(fr As LTFSWriter.FileRecord, ct As CancellationToken) As Task
        Dim fs As FileStream = Nothing
        Try
            ' 1MiB 缓冲，异步顺序读取
            fs = New FileStream(fr.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous Or FileOptions.SequentialScan)
        Catch
            ' 备用：尝试使用现有 FileRecord 打开
            Try
                Select Case fr.Open(BufferSize:=1024 * 1024)
                    Case DialogResult.Ignore
                        Exit Function
                    Case DialogResult.Abort
                        Throw New IOException("Open aborted")
                End Select
                fs = fr.fs
            Catch
                Exit Function
            End Try
        End Try

        Using fs
            Dim remaining As Long = If(fr.File IsNot Nothing, fr.File.length, Math.Max(0, fs.Length))
            Dim chunk As Integer = 1024 * 1024 ' 1MiB
            Dim rented As Byte() = ArrayPool(Of Byte).Shared.Rent(chunk)
            Try
                While remaining > 0 AndAlso Not ct.IsCancellationRequested
                    Dim toRead As Integer = CInt(Math.Min(chunk, remaining))
                    Dim n As Integer = Await fs.ReadAsync(rented, 0, toRead)
                    If n <= 0 Then Exit While
                    _writer.Write(rented.AsSpan(0, n))
                    remaining -= n
                    Dim res = Await _writer.FlushAsync(ct)
                    If res.IsCanceled OrElse res.IsCompleted Then Exit While
                End While
            Finally
                ArrayPool(Of Byte).Shared.Return(rented)
            End Try
        End Using
    End Function

    Private Async Function PreloadSmallFilesAsync() As Task
        Try
            For Each fr In _writeList
                If _cts.IsCancellationRequested Then Exit For
                If fr.File IsNot Nothing AndAlso fr.File.length < _smallThreshold Then
                    ' 控制小文件缓存上限
                    While _smallCacheQueue.Count >= _smallCacheCapacity AndAlso Not _cts.IsCancellationRequested
                        Await Task.Delay(10, _cts.Token)
                    End While

                    Dim data As Byte() = Nothing
                    Try
                        data = ReadAllBytesSafe(fr)
                    Catch
                        data = Nothing
                    End Try
                    If data IsNot Nothing Then
                        _smallCacheMap.TryAdd(fr, data)
                        _smallCacheQueue.Enqueue(Tuple.Create(fr, data))
                    End If
                End If
            Next
        Catch
        End Try
    End Function
End Class
