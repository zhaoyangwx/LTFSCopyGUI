Imports System
Imports System.Diagnostics
Imports System.Threading

Public NotInheritable Class SpscRingBuffer
    Implements IDisposable

    Private ReadOnly _buffer As Byte()
    Private ReadOnly _capacity As Long
    Private ReadOnly _mask As Integer ' only valid when power-of-two

    Private _headPos As Long
    Private _tailPos As Long

    Private ReadOnly _dataAvailable As AutoResetEvent = New AutoResetEvent(False)
    Private ReadOnly _spaceAvailable As AutoResetEvent = New AutoResetEvent(False)

    Private _completed As Integer
    Private _disposed As Integer

    Public Sub New(capacityBytes As Long)
        If capacityBytes <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(capacityBytes))
        If Not IsPowerOfTwo(capacityBytes) Then
            Throw New ArgumentException("capacityBytes must be a power of two for fast masking.", NameOf(capacityBytes))
        End If

        _capacity = capacityBytes
        _mask = capacityBytes - 1
        _buffer = New Byte(capacityBytes - 1) {}
    End Sub

    Public ReadOnly Property Capacity As Long
        Get
            Return _capacity
        End Get
    End Property

    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return Thread.VolatileRead(_completed) <> 0
        End Get
    End Property

    ''' <summary>Mark no more writes. Reader will drain remaining data then see completion.</summary>
    Public Sub Complete()
        If Interlocked.Exchange(_completed, 1) = 0 Then
            _dataAvailable.Set()
            _spaceAvailable.Set()
        End If
    End Sub

    ''' <summary>Bytes currently available to read.</summary>
    Public Function AvailableToRead() As Integer
        Dim head As Long = Thread.VolatileRead(_headPos)
        Dim tail As Long = Thread.VolatileRead(_tailPos)
        Dim n As Long = tail - head
        If n <= 0 Then Return 0
        If n >= Integer.MaxValue Then Return Integer.MaxValue
        Return CInt(n)
    End Function

    ''' <summary>Bytes currently available to write.</summary>
    Public Function AvailableToWrite() As Integer
        Dim head As Long = Thread.VolatileRead(_headPos)
        Dim tail As Long = Thread.VolatileRead(_tailPos)
        Dim used As Long = tail - head
        Dim free As Long = CLng(_capacity) - used
        If free <= 0 Then Return 0
        If free >= Integer.MaxValue Then Return Integer.MaxValue
        Return CInt(free)
    End Function

    ''' <summary>
    ''' Get a contiguous writable segment. May block until space exists (unless completed/disposed).
    ''' The returned segment length can be smaller than requested due to wrap boundary.
    ''' </summary>
    Public Function GetWriteSegment(minBytes As Integer, ct As CancellationToken) As ArraySegment(Of Byte)
        If minBytes <= 0 Then minBytes = 1
        EnsureNotDisposed()
        If IsCompleted Then Throw New InvalidOperationException("Buffer is completed; cannot write.")

        WaitForCondition(
            Function() AvailableToWrite() >= minBytes,
            _spaceAvailable,
            ct
        )

        Dim head As Long = Thread.VolatileRead(_headPos)
        Dim tail As Long = Thread.VolatileRead(_tailPos)
        Dim used As Long = tail - head
        Dim free As Integer = CInt(CLng(_capacity) - used)
        If free <= 0 Then Return New ArraySegment(Of Byte)(_buffer, 0, 0)

        Dim tailIdx As Integer = CInt(tail And _mask)
        Dim contiguous As Integer = Math.Min(free, _capacity - tailIdx) ' to end of array

        Return New ArraySegment(Of Byte)(_buffer, tailIdx, contiguous)
    End Function

    ''' <summary>Commit written bytes (0..segment.Count). Must be called by producer.</summary>
    Public Sub AdvanceWrite(bytesWritten As Integer)
        EnsureNotDisposed()
        If bytesWritten < 0 Then Throw New ArgumentOutOfRangeException(NameOf(bytesWritten))

        If bytesWritten = 0 Then Return

        ' Only producer writes tailPos.
        Dim newTail As Long = Thread.VolatileRead(_tailPos) + bytesWritten
        Thread.VolatileWrite(_tailPos, newTail)

        _dataAvailable.Set()
    End Sub

    ''' <summary>
    ''' Get a contiguous readable segment. May block until data exists or completion.
    ''' When completed and empty, returns Count=0.
    ''' </summary>
    Public Function GetReadSegment(minBytes As Integer, ct As CancellationToken) As ArraySegment(Of Byte)
        If minBytes <= 0 Then minBytes = 1
        EnsureNotDisposed()

        WaitForCondition(
            Function()
                Dim available = AvailableToRead()
                If available >= minBytes Then Return True
                If available > 0 Then Return True ' allow partial read
                If IsCompleted Then Return True ' allow Count=0 to signal end
                Return False
            End Function,
            _dataAvailable,
            ct
        )

        Dim head As Long = Thread.VolatileRead(_headPos)
        Dim tail As Long = Thread.VolatileRead(_tailPos)
        Dim availLong As Long = tail - head
        If availLong <= 0 Then
            ' empty; if completed => EOF
            Return New ArraySegment(Of Byte)(_buffer, 0, 0)
        End If

        Dim avail As Integer = CInt(Math.Min(availLong, CLng(Integer.MaxValue)))
        Dim headIdx As Integer = CInt(head And _mask)
        Dim contiguous As Integer = Math.Min(avail, _capacity - headIdx)

        Return New ArraySegment(Of Byte)(_buffer, headIdx, contiguous)
    End Function

    ''' <summary>Consume bytes read (0..segment.Count). Must be called by consumer.</summary>
    Public Sub AdvanceRead(bytesConsumed As Integer)
        EnsureNotDisposed()
        If bytesConsumed < 0 Then Throw New ArgumentOutOfRangeException(NameOf(bytesConsumed))
        If bytesConsumed = 0 Then Return

        ' Only consumer writes headPos.
        Dim newHead As Long = Thread.VolatileRead(_headPos) + bytesConsumed
        Thread.VolatileWrite(_headPos, newHead)

        _spaceAvailable.Set()
    End Sub

    Private Sub WaitForCondition(cond As Func(Of Boolean),
                                 gate As AutoResetEvent,
                                 ct As CancellationToken)

        If cond() Then Return

        Dim sw As Stopwatch = Stopwatch.StartNew()
        Dim spinner As SpinWait = New SpinWait()

        ' Spin up to 2 seconds
        While sw.ElapsedMilliseconds < 2000
            ct.ThrowIfCancellationRequested()
            If Thread.VolatileRead(_disposed) <> 0 Then Throw New ObjectDisposedException(NameOf(SpscRingBuffer))
            If cond() Then Return
            spinner.SpinOnce()
        End While

        ' Then block, but still re-check loop to avoid missed signals
        While True
            ct.ThrowIfCancellationRequested()
            If Thread.VolatileRead(_disposed) <> 0 Then Throw New ObjectDisposedException(NameOf(SpscRingBuffer))
            If cond() Then Return

            ' Use small wait to re-check periodically; AutoResetEvent may be lost if signaled before WaitOne
            gate.WaitOne(50)
        End While
    End Sub

    Private Sub EnsureNotDisposed()
        If Thread.VolatileRead(_disposed) <> 0 Then Throw New ObjectDisposedException(NameOf(SpscRingBuffer))
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If Interlocked.Exchange(_disposed, 1) = 0 Then
            Try
                _dataAvailable.Set()
                _spaceAvailable.Set()
            Catch
            End Try
            _dataAvailable.Dispose()
            _spaceAvailable.Dispose()
        End If
    End Sub

    Public Shared Function IsPowerOfTwo(x As Long) As Boolean
        Return x > 0 AndAlso (x And (x - 1)) = 0
    End Function
End Class
