Imports Microsoft.Win32.SafeHandles
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading

' Control-plane wrapper for ltfscopy_fastreader.dll. File payloads never enter
' CLR memory: Rust owns every slot and TapeUtils.Write consumes its pointer
' directly before AdvanceSlot releases it back to Rust.
Public Class RustFastReaderProvider
    Implements IDisposable

    Public Structure Slot
        Public Property Token As ULong
        Public Property FileIndex As Long
        Public Property FileOffset As Long
        Public Property Length As Integer
        Public Property Flags As Integer
        Public Property DataPtr As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure NativeConfig
        Public StructSize As UInteger
        Public AbiVersion As UInteger
        Public SlotSize As UInteger
        Public ReadChunkSize As UInteger
        Public QueueDepth As UInteger
        Public CapacityBytes As ULong
        Public SmallOpenConcurrency As UInteger
        Public SmallActiveFiles As UInteger
        Public SmallInflightBytes As ULong
        Public SmallThreshold As ULong
        Public HashMask As UInteger
        Public NextFilePrimeDepth As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure PerformanceStats
        Public StructSize As UInteger
        Public AbiVersion As UInteger
        Public BytesRead As ULong
        Public BytesPublished As ULong
        Public BufferedBytes As ULong
        Public OccupiedSlots As ULong
        Public ReadWaitNanoseconds As ULong
        Public HashNanoseconds As ULong
        Public PublishWaitNanoseconds As ULong
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure NativeSlot
        Public Token As ULong
        Public FileIndex As Long
        Public FileOffset As ULong
        Public Data As IntPtr
        Public Length As UInteger
        Public Flags As UInteger
    End Structure

    Private NotInheritable Class NativeReaderHandle
        Inherits SafeHandleZeroOrMinusOneIsInvalid

        Public Sub New()
            MyBase.New(True)
        End Sub

        Public Sub Initialize(value As IntPtr)
            SetHandle(value)
        End Sub

        Protected Overrides Function ReleaseHandle() As Boolean
            NativeMethods.lfr_destroy(handle)
            Return True
        End Function
    End Class

    Private NotInheritable Class NativeMethods
        Public Const DllName As String = "ltfscopy_fastreader.dll"

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_abi_version() As UInteger
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_create(ByRef config As NativeConfig, ByRef output As IntPtr) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi, CharSet:=CharSet.Unicode)>
        Public Shared Function lfr_add_file(context As IntPtr,
                                            index As Long,
                                            <MarshalAs(UnmanagedType.LPWStr)> path As String,
                                            pathLength As UInteger,
                                            expectedLength As ULong) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_select_file(context As IntPtr, index As Long) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_start(context As IntPtr) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_buffered_bytes(context As IntPtr) As ULong
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_buffer_capacity(context As IntPtr) As ULong
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_occupied_slots(context As IntPtr) As ULong
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_get_stats(context As IntPtr, ByRef output As PerformanceStats) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_wait_until_buffered(context As IntPtr, target As ULong, timeoutMs As UInteger) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_acquire_slot(context As IntPtr,
                                                expectedFileIndex As Long,
                                                timeoutMs As UInteger,
                                                ByRef output As NativeSlot) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_release_slot(context As IntPtr, token As ULong) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_hash_file(context As IntPtr,
                                             index As Long,
                                             <Out> buffer As Byte(),
                                             capacity As UInteger,
                                             ByRef written As UInteger) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_get_file_hashes(context As IntPtr,
                                                   index As Long,
                                                   <Out> buffer As Byte(),
                                                   capacity As UInteger,
                                                   ByRef written As UInteger) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_last_error(context As IntPtr,
                                              <Out> buffer As Byte(),
                                              capacity As UInteger,
                                              ByRef written As UInteger) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function lfr_cancel(context As IntPtr) As Integer
        End Function

        <DllImport(DllName, ExactSpelling:=True, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Sub lfr_destroy(context As IntPtr)
        End Sub

        <DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
        Public Shared Function LoadLibrary(lpFileName As String) As IntPtr
        End Function

        <DllImport("kernel32.dll", SetLastError:=True)>
        Public Shared Function FreeLibrary(moduleHandle As IntPtr) As Boolean
        End Function
    End Class

    Private Const AbiVersion As UInteger = 2UI
    Private Const ResultOk As Integer = 0
    Private Const ResultTimeout As Integer = 1
    Private Const ResultDone As Integer = 2
    Private Const ResultInvalid As Integer = -1
    Private Const ResultError As Integer = -2
    Private Const ResultCancelled As Integer = -3
    Private Const FlagEof As Integer = 1
    Private Const HashSha1 As UInteger = 1UI << 0
    Private Const HashSha256 As UInteger = 1UI << 1
    Private Const HashSha512 As UInteger = 1UI << 2
    Private Const HashMd5 As UInteger = 1UI << 3
    Private Const HashCrc32 As UInteger = 1UI << 4
    Private Const HashBlake3 As UInteger = 1UI << 5
    Private Const HashXxh3 As UInteger = 1UI << 6
    Private Const HashXxh128 As UInteger = 1UI << 7
    Private Const ReadChunkSize As UInteger = 16UI * 1024UI * 1024UI
    Private Const QueueDepth As UInteger = 4UI
    Private Const NextFilePrimeDepth As UInteger = 1UI
    Private Const SmallOpenConcurrency As UInteger = 32UI
    Private Const SmallActiveFileLimit As UInteger = 64UI
    Private Const SmallMinimumThreshold As Long = 64L * 1024L
    Private Const SmallMaximumThreshold As Long = 4L * 1024L * 1024L
    Private Const SmallMaximumInflightBytes As Long = 128L * 1024L * 1024L
    Private Const NativeWaitSliceMs As UInteger = 50UI

    Private ReadOnly _writeList As List(Of LTFSWriter.FileRecord)
    Private ReadOnly _requestedCapacityBytes As Long
    Private ReadOnly _slotSize As Integer
    Private ReadOnly _readChunkSize As Integer
    Private ReadOnly _smallInflightByteLimit As Long
    Private ReadOnly _smallFileThreshold As Long
    Private ReadOnly _configuredFiles As New HashSet(Of Long)()
    Private _handle As NativeReaderHandle
    Private _moduleHandle As IntPtr
    Private _remainingBytes As Long
    Private _started As Boolean
    Private _streamStarted As Boolean
    Private _cancelRequested As Integer
    Private _disposed As Integer

    Public Sub New(writeList As IEnumerable(Of LTFSWriter.FileRecord), blockSize As Integer, capacityBytes As Long)
        If IntPtr.Size <> 8 Then Throw New PlatformNotSupportedException("The native fast reader requires an x64 process")
        _writeList = writeList.ToList()
        _slotSize = Math.Max(1, blockSize)
        Dim alignedReadChunk = ((CLng(ReadChunkSize) + _slotSize - 1L) \ _slotSize) * _slotSize
        If alignedReadChunk > 64L * 1024L * 1024L Then Throw New ArgumentOutOfRangeException(NameOf(blockSize))
        _readChunkSize = CInt(alignedReadChunk)
        _requestedCapacityBytes = Math.Max(CLng(_slotSize) * 2L, capacityBytes)
        _smallInflightByteLimit = Math.Max(SmallMinimumThreshold, Math.Min(SmallMaximumInflightBytes, _requestedCapacityBytes))
        _smallFileThreshold = Math.Max(SmallMinimumThreshold, Math.Min(SmallMaximumThreshold, _smallInflightByteLimit \ CInt(SmallActiveFileLimit)))
    End Sub

    Public ReadOnly Property BufferedBytes As Long
        Get
            If Not HasContext() Then Return 0
            Return CLng(NativeMethods.lfr_buffered_bytes(Context))
        End Get
    End Property

    Public ReadOnly Property BufferCapacityBytes As Long
        Get
            If Not HasContext() Then Return _requestedCapacityBytes
            Return CLng(NativeMethods.lfr_buffer_capacity(Context))
        End Get
    End Property

    Public ReadOnly Property OccupiedSlotCount As ULong
        Get
            If Not HasContext() Then Return 0UL
            Return NativeMethods.lfr_occupied_slots(Context)
        End Get
    End Property

    Public Sub Start()
        If _started Then Return
        ThrowIfDisposed()

        Dim dllPath = FindNativeDll()
        If Not File.Exists(dllPath) Then Throw New DllNotFoundException($"{NativeMethods.DllName} not found: {dllPath}")
        _moduleHandle = NativeMethods.LoadLibrary(dllPath)
        If _moduleHandle = IntPtr.Zero Then
            Throw New Win32Exception(Marshal.GetLastWin32Error(), $"Unable to load native fast reader: {dllPath}")
        End If

        Try
            Dim actualAbi = NativeMethods.lfr_abi_version()
            If actualAbi <> AbiVersion Then Throw New InvalidDataException($"Native fast reader ABI mismatch. expected={AbiVersion} actual={actualAbi}")

            Dim config As New NativeConfig With {
                .StructSize = CUInt(Marshal.SizeOf(GetType(NativeConfig))),
                .AbiVersion = AbiVersion,
                .SlotSize = CUInt(_slotSize),
                .ReadChunkSize = CUInt(_readChunkSize),
                .QueueDepth = QueueDepth,
                .CapacityBytes = CULng(_requestedCapacityBytes),
                .SmallOpenConcurrency = SmallOpenConcurrency,
                .SmallActiveFiles = SmallActiveFileLimit,
                .SmallInflightBytes = CULng(_smallInflightByteLimit),
                .SmallThreshold = CULng(_smallFileThreshold),
                .HashMask = BuildHashMask(),
                .NextFilePrimeDepth = NextFilePrimeDepth
            }
            Dim rawContext = IntPtr.Zero
            Dim result = NativeMethods.lfr_create(config, rawContext)
            If result <> ResultOk OrElse rawContext = IntPtr.Zero Then Throw New IOException($"Native fast reader create failed ({result})")
            _handle = New NativeReaderHandle()
            _handle.Initialize(rawContext)

            For index = 0 To _writeList.Count - 1
                Dim fr = _writeList(index)
                If fr Is Nothing OrElse fr.File Is Nothing Then Continue For
                If String.IsNullOrEmpty(fr.SourcePath) Then Throw New InvalidDataException($"Missing source path for file index {index}")
                CheckResult(NativeMethods.lfr_add_file(Context,
                                                       index,
                                                       fr.SourcePath,
                                                       CUInt(fr.SourcePath.Length),
                                                       CULng(fr.File.length)),
                            $"add file {index}")
            Next
            _started = True
        Catch
            If _handle IsNot Nothing Then
                _handle.Dispose()
                _handle = Nothing
            End If
            If _moduleHandle <> IntPtr.Zero Then
                NativeMethods.FreeLibrary(_moduleHandle)
                _moduleHandle = IntPtr.Zero
            End If
            Throw
        End Try
    End Sub

    Public Sub ConfigureOrderedFiles(fileIndices As IEnumerable(Of Long))
        EnsureStarted()
        If _streamStarted Then Throw New InvalidOperationException("Native fast reader ordered queue is already configured")

        Dim previous As Long = -1
        Dim remaining As Long = 0
        For Each fileIndex In fileIndices
            If fileIndex < 0 OrElse fileIndex >= _writeList.Count Then Throw New ArgumentOutOfRangeException(NameOf(fileIndices))
            If fileIndex <= previous Then Throw New InvalidOperationException("Native fast reader queue must contain unique ascending file indices")
            previous = fileIndex
            Dim fr = _writeList(CInt(fileIndex))
            If fr Is Nothing OrElse fr.File Is Nothing OrElse fr.File.length <= 0 Then Continue For
            CheckResult(NativeMethods.lfr_select_file(Context, fileIndex), $"select file {fileIndex}")
            _configuredFiles.Add(fileIndex)
            remaining = checkedAdd(remaining, fr.File.length)
        Next
        Interlocked.Exchange(_remainingBytes, remaining)
        CheckResult(NativeMethods.lfr_start(Context), "start stream")
        _streamStarted = True
    End Sub

    Public Sub QueueFile(fileIndex As Long)
        ThrowIfDisposed()
        If Not _streamStarted OrElse Not _configuredFiles.Contains(fileIndex) Then
            Throw New InvalidOperationException($"Native fast reader file {fileIndex} is not in the ordered queue")
        End If
    End Sub

    Public Function ReadSlot(expectedFileIndex As Long, ct As CancellationToken) As Slot
        QueueFile(expectedFileIndex)
        While True
            ct.ThrowIfCancellationRequested()
            Dim native As NativeSlot
            Dim result = NativeMethods.lfr_acquire_slot(Context, expectedFileIndex, NativeWaitSliceMs, native)
            Select Case result
                Case ResultOk
                    Return New Slot With {
                        .Token = native.Token,
                        .FileIndex = native.FileIndex,
                        .FileOffset = CLng(native.FileOffset),
                        .Length = CInt(native.Length),
                        .Flags = CInt(native.Flags),
                        .DataPtr = native.Data
                    }
                Case ResultTimeout
                    Continue While
                Case ResultDone
                    Throw New EndOfStreamException($"Native fast reader completed before file {expectedFileIndex} EOF")
                Case Else
                    ThrowNative(result, $"acquire file {expectedFileIndex} slot")
            End Select
        End While
    End Function

    Public Function GetPerformanceStats() As PerformanceStats
        EnsureStarted()
        Dim result As New PerformanceStats With {
            .StructSize = CUInt(Marshal.SizeOf(GetType(PerformanceStats)))
        }
        CheckResult(NativeMethods.lfr_get_stats(Context, result), "get performance stats")
        Return result
    End Function

    Public Sub AdvanceSlot(slot As Slot)
        If slot.Token = 0 Then Return
        CheckResult(NativeMethods.lfr_release_slot(Context, slot.Token), "release slot")
        If slot.Length > 0 Then Interlocked.Add(_remainingBytes, -CLng(slot.Length))
    End Sub

    Public Sub Drain(fileIndex As Long, bytesToDrain As Long, Optional ct As CancellationToken = Nothing)
        QueueFile(fileIndex)
        Dim remaining = bytesToDrain
        Dim eofSeen = False
        While remaining > 0
            Dim slot = ReadSlot(fileIndex, ct)
            If (slot.Flags And FlagEof) <> 0 Then
                AdvanceSlot(slot)
                eofSeen = True
                Exit While
            End If
            remaining -= slot.Length
            AdvanceSlot(slot)
        End While
        If Not eofSeen Then DrainEof(fileIndex, ct)
    End Sub

    Public Sub DrainEof(fileIndex As Long, Optional ct As CancellationToken = Nothing)
        QueueFile(fileIndex)
        While True
            Dim slot = ReadSlot(fileIndex, ct)
            AdvanceSlot(slot)
            If (slot.Flags And FlagEof) <> 0 Then Exit While
        End While
    End Sub

    Public Function HashFile(fileIndex As Long, Optional timeoutMs As Integer = Timeout.Infinite) As Dictionary(Of String, String)
        Return HashFiles({fileIndex}, timeoutMs)(fileIndex)
    End Function

    Public Function HashFiles(fileIndices As IEnumerable(Of Long),
                              Optional timeoutMs As Integer = Timeout.Infinite) As Dictionary(Of Long, Dictionary(Of String, String))
        EnsureStarted()
        If _streamStarted Then Throw New InvalidOperationException("Hash scan must finish before the ordered native stream starts")
        If timeoutMs = 0 Then Throw New TimeoutException("Native hash timeout")

        Dim results As New Dictionary(Of Long, Dictionary(Of String, String))()
        For Each fileIndex In fileIndices.Distinct()
            If fileIndex < 0 OrElse fileIndex >= _writeList.Count Then Throw New ArgumentOutOfRangeException(NameOf(fileIndices))
            Dim fr = _writeList(CInt(fileIndex))
            If fr Is Nothing OrElse fr.File Is Nothing Then
                results(fileIndex) = EmptyHashes()
                Continue For
            End If
            results(fileIndex) = InvokeText(Function(buffer As Byte(), capacity As UInteger, ByRef written As UInteger) As Integer
                                                Return NativeMethods.lfr_hash_file(Context, fileIndex, buffer, capacity, written)
                                            End Function,
                                            $"hash file {fileIndex}")
        Next
        Return results
    End Function

    Public Function WaitFileDone(fileIndex As Long, Optional timeoutMs As Integer = 30000) As Dictionary(Of String, String)
        Dim timer = Stopwatch.StartNew()
        While timeoutMs = Timeout.Infinite OrElse timer.ElapsedMilliseconds < timeoutMs
            ThrowIfFailed()
            Dim text As String = Nothing
            Dim result = TryGetText(Function(buffer As Byte(), capacity As UInteger, ByRef written As UInteger) As Integer
                                        Return NativeMethods.lfr_get_file_hashes(Context, fileIndex, buffer, capacity, written)
                                    End Function,
                                    text)
            If result = ResultOk Then Return ParseHashes(text)
            If result <> ResultTimeout Then ThrowNative(result, $"get file {fileIndex} hashes")
            Thread.Sleep(10)
        End While
        Return EmptyHashes()
    End Function

    Public Sub WaitForStreamFillFraction(fraction As Double, ct As CancellationToken)
        ThrowIfFailed()
        Dim boundedFraction = Math.Max(0.0, Math.Min(1.0, fraction))
        While True
            ct.ThrowIfCancellationRequested()
            ThrowIfFailed()
            Dim remaining = Math.Max(0L, Interlocked.Read(_remainingBytes))
            Dim target = Math.Min(CLng(Math.Ceiling(BufferCapacityBytes * boundedFraction)), remaining)
            If target <= 0 OrElse BufferedBytes >= target Then Return
            Dim result = NativeMethods.lfr_wait_until_buffered(Context, CULng(target), NativeWaitSliceMs)
            If result = ResultOk Then Return
            If result <> ResultTimeout Then ThrowNative(result, "wait for native buffer fill")
        End While
    End Sub

    Public Sub Cancel()
        If Interlocked.Exchange(_cancelRequested, 1) <> 0 Then Return
        If HasContext() Then NativeMethods.lfr_cancel(Context)
    End Sub

    Public Sub Complete()
        ' The native worker receives the complete ordered file set at start.
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If Interlocked.Exchange(_disposed, 1) <> 0 Then Return
        Try
            Cancel()
            If _handle IsNot Nothing Then
                _handle.Dispose()
                _handle = Nothing
            End If
        Finally
            If _moduleHandle <> IntPtr.Zero Then
                NativeMethods.FreeLibrary(_moduleHandle)
                _moduleHandle = IntPtr.Zero
            End If
        End Try
    End Sub

    Private Delegate Function NativeTextCall(buffer As Byte(), capacity As UInteger, ByRef written As UInteger) As Integer

    Private Function InvokeText(callNative As NativeTextCall, operation As String) As Dictionary(Of String, String)
        Dim text As String = Nothing
        Dim result = TryGetText(callNative, text)
        If result <> ResultOk Then ThrowNative(result, operation)
        Return ParseHashes(text)
    End Function

    Private Function TryGetText(callNative As NativeTextCall, ByRef text As String) As Integer
        Dim buffer(2047) As Byte
        Dim written As UInteger = 0
        Dim result = callNative(buffer, CUInt(buffer.Length), written)
        If result = ResultInvalid AndAlso written + 1UI > CUInt(buffer.Length) Then
            ReDim buffer(CInt(written))
            result = callNative(buffer, CUInt(buffer.Length), written)
        End If
        If result = ResultOk Then text = Encoding.UTF8.GetString(buffer, 0, CInt(written))
        Return result
    End Function

    Private Shared Function ParseHashes(text As String) As Dictionary(Of String, String)
        Dim result = EmptyHashes()
        If String.IsNullOrEmpty(text) Then Return result
        For Each part In text.Split({vbTab}, StringSplitOptions.RemoveEmptyEntries)
            Dim pair = part.Split({"="c}, 2)
            If pair.Length = 2 Then result(pair(0)) = pair(1)
        Next
        Return result
    End Function

    Private Shared Function EmptyHashes() As Dictionary(Of String, String)
        Return New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    End Function

    Private Function BuildHashMask() As UInteger
        Dim result As UInteger = 0UI
        If IsHashRequired("SHA1") Then result = result Or HashSha1
        If IsHashRequired("SHA256") Then result = result Or HashSha256
        If IsHashRequired("SHA512") Then result = result Or HashSha512
        If IsHashRequired("MD5") Then result = result Or HashMd5
        If IsHashRequired("CRC32") Then result = result Or HashCrc32
        If IsHashRequired("BLAKE3") Then result = result Or HashBlake3
        If IsHashRequired("XxHash3") Then result = result Or HashXxh3
        If IsHashRequired("XxHash128") Then result = result Or HashXxh128
        Return result
    End Function

    Private Function IsHashRequired(name As String) As Boolean
        Select Case name
            Case "SHA1"
                Return My.Settings.LTFSWriter_ChecksumEnabled_SHA1 OrElse (My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.SHA1)
            Case "SHA256"
                Return My.Settings.LTFSWriter_ChecksumEnabled_SHA256 OrElse (My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.SHA256)
            Case "SHA512"
                Return My.Settings.LTFSWriter_ChecksumEnabled_SHA512 OrElse (My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.SHA512)
            Case "MD5"
                Return My.Settings.LTFSWriter_ChecksumEnabled_MD5 OrElse (My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.MD5)
            Case "CRC32"
                Return My.Settings.LTFSWriter_ChecksumEnabled_CRC32 OrElse (My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.CRC32)
            Case "BLAKE3"
                Return My.Settings.LTFSWriter_ChecksumEnabled_BLAKE3 OrElse (My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.BLAKE3)
            Case "XxHash3"
                Return My.Settings.LTFSWriter_ChecksumEnabled_XxHash3 OrElse (My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.XxHash3)
            Case "XxHash128"
                Return My.Settings.LTFSWriter_ChecksumEnabled_XxHash128 OrElse (My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.XxHash128)
        End Select
        Return False
    End Function

    Private Sub ThrowIfFailed()
        ThrowIfDisposed()
        If Volatile.Read(_cancelRequested) <> 0 Then Throw New OperationCanceledException("Native fast reader operation cancelled")
        If Not HasContext() Then Throw New InvalidOperationException("Native fast reader is not initialized")
        Dim message = ReadLastError()
        If Not String.IsNullOrEmpty(message) Then Throw New IOException(message)
    End Sub

    Private Sub CheckResult(result As Integer, operation As String)
        If result <> ResultOk Then ThrowNative(result, operation)
    End Sub

    Private Sub ThrowNative(result As Integer, operation As String)
        If result = ResultCancelled Then Throw New OperationCanceledException($"Native fast reader cancelled during {operation}")
        Dim message = If(HasContext(), ReadLastError(), String.Empty)
        If String.IsNullOrEmpty(message) Then message = $"Native fast reader {operation} failed ({result})"
        Throw New IOException(message)
    End Sub

    Private Function ReadLastError() As String
        If Not HasContext() Then Return String.Empty
        Dim buffer(4095) As Byte
        Dim written As UInteger = 0
        Dim result = NativeMethods.lfr_last_error(Context, buffer, CUInt(buffer.Length), written)
        If result <> ResultOk OrElse written = 0 Then Return String.Empty
        Return Encoding.UTF8.GetString(buffer, 0, CInt(written))
    End Function

    Private Sub EnsureStarted()
        If Not _started Then Start()
        ThrowIfFailed()
    End Sub

    Private Sub ThrowIfDisposed()
        If Volatile.Read(_disposed) <> 0 Then Throw New ObjectDisposedException(NameOf(RustFastReaderProvider))
    End Sub

    Private Function HasContext() As Boolean
        Return _handle IsNot Nothing AndAlso Not _handle.IsInvalid AndAlso Not _handle.IsClosed
    End Function

    Private ReadOnly Property Context As IntPtr
        Get
            If Not HasContext() Then Throw New ObjectDisposedException(NameOf(RustFastReaderProvider))
            Return _handle.DangerousGetHandle()
        End Get
    End Property

    Private Shared Function checkedAdd(left As Long, right As Long) As Long
        If right > 0 AndAlso left > Long.MaxValue - right Then Return Long.MaxValue
        Return left + right
    End Function

    Private Shared Function FindNativeDll() As String
        Dim direct = Path.Combine(Application.StartupPath, NativeMethods.DllName)
        If File.Exists(direct) Then Return direct

        Dim directory As New DirectoryInfo(Application.StartupPath)
        While directory IsNot Nothing
            Dim candidate = Path.Combine(directory.FullName,
                                         "LtfsFastReader",
                                         "target",
                                         "x86_64-win7-windows-msvc",
                                         "release",
                                         NativeMethods.DllName)
            If File.Exists(candidate) Then Return candidate
            directory = directory.Parent
        End While
        Return direct
    End Function
End Class
