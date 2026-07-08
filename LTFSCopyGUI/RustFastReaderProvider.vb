Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading

Public Class RustFastReaderProvider
    Implements IDisposable

    Public Class Slot
        Public Property Index As ULong
        Public Property FileIndex As Long
        Public Property FileOffset As Long
        Public Property Length As Integer
        Public Property Flags As Integer
        Public Property DataPtr As IntPtr
    End Class

    Private Const HeaderSize As Integer = 4096
    Private Const SlotMetaSize As Integer = 64
    Private Const StatusEmpty As Integer = 0
    Private Const StatusFull As Integer = 1
    Private Const FlagEof As Integer = 1
    Private Const FlagError As Integer = 2
    Private Const FileMapAllAccess As UInteger = &H1F
    Private Const EventModifyState As UInteger = &H2
    Private Const Synchronize As UInteger = &H100000
    Private Const Infinite As UInteger = &HFFFFFFFFUI
    Private Const WaitObject0 As UInteger = 0

    Private ReadOnly _writeList As List(Of LTFSWriter.FileRecord)
    Private ReadOnly _capacityBytes As Long
    Private ReadOnly _slotSize As Integer
    Private ReadOnly _id As String = Guid.NewGuid().ToString("N")
    Private ReadOnly _shmName As String
    Private ReadOnly _dataEventName As String
    Private ReadOnly _spaceEventName As String
    Private ReadOnly _done As New ConcurrentDictionary(Of Long, Dictionary(Of String, String))
    Private ReadOnly _errors As New ConcurrentQueue(Of String)
    Private ReadOnly _stdoutDone As New ManualResetEvent(False)
    Private ReadOnly _inputLock As New Object()
    Private ReadOnly _queuedFiles As New HashSet(Of Long)

    Private _proc As Process
    Private _mapHandle As IntPtr = IntPtr.Zero
    Private _basePtr As IntPtr = IntPtr.Zero
    Private _dataEvent As IntPtr = IntPtr.Zero
    Private _spaceEvent As IntPtr = IntPtr.Zero
    Private _slotCount As ULong
    Private _dataOffset As Long
    Private _mapSize As Long
    Private _readIndex As ULong
    Private _started As Boolean
    Private _disposed As Boolean

    Public Sub New(writeList As IEnumerable(Of LTFSWriter.FileRecord), blockSize As Integer, capacityBytes As Long)
        _writeList = writeList.ToList()
        _slotSize = Math.Max(1, blockSize)
        _capacityBytes = Math.Max(CLng(_slotSize) * 2, capacityBytes)
        _shmName = "Local\LTFSCopyGUI.FastReader." & _id
        _dataEventName = "Local\LTFSCopyGUI.FastReader.Data." & _id
        _spaceEventName = "Local\LTFSCopyGUI.FastReader.Space." & _id
    End Sub

    Public ReadOnly Property BufferedBytes As Long
        Get
            If _basePtr = IntPtr.Zero Then Return 0
            Dim writeIndex As ULong = ReadUInt64(32)
            If writeIndex <= _readIndex Then Return 0
            Dim count As ULong = Math.Min(writeIndex - _readIndex, _slotCount)
            Dim total As Long = 0
            For i As ULong = 0 To count - 1UL
                Dim meta = SlotMetaPtr(_readIndex + i)
                If Marshal.ReadInt32(meta, 0) <> StatusFull Then Exit For
                total += Math.Max(0, Marshal.ReadInt32(meta, 24))
            Next
            Return total
        End Get
    End Property

    Public ReadOnly Property BufferCapacityBytes As Long
        Get
            Return CLng(_slotCount) * CLng(_slotSize)
        End Get
    End Property

    Public Sub Start()
        If _started Then Return
        _started = True

        Dim exe = FindHelperExe()
        If Not File.Exists(exe) Then Throw New FileNotFoundException("ltfscopy-fastreader.exe not found", exe)

        _proc = New Process()
        _proc.StartInfo = New ProcessStartInfo With {
            .FileName = exe,
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardInput = True,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .StandardOutputEncoding = Encoding.UTF8,
            .StandardErrorEncoding = Encoding.UTF8
        }
        _proc.Start()
        AddHandler _proc.ErrorDataReceived, Sub(sender, e)
                                                If e.Data IsNot Nothing Then _errors.Enqueue(e.Data)
                                            End Sub
        _proc.BeginErrorReadLine()

        Dim init = BuildInitLine()
        _proc.StandardInput.WriteLine(init)
        _proc.StandardInput.Flush()

        Dim readyLine As String = _proc.StandardOutput.ReadLine()
        If readyLine Is Nothing OrElse Not readyLine.StartsWith("READY" & vbTab) Then
            Throw New IOException("fastreader did not return READY: " & If(readyLine, "<eof>"))
        End If
        ParseReady(readyLine)
        OpenSharedObjects()

        Dim th As New Thread(AddressOf StdoutLoop)
        th.IsBackground = True
        th.Start()

    End Sub

    Private Function BuildInitLine() As String
        Dim parts As New List(Of String) From {
            "INIT",
            "shm=" & _shmName,
            "data_event=" & _dataEventName,
            "space_event=" & _spaceEventName,
            "capacity=" & _capacityBytes.ToString(),
            "slot_size=" & _slotSize.ToString(),
            "SHA1=" & If(IsHashRequired("SHA1"), "1", "0"),
            "SHA256=" & If(IsHashRequired("SHA256"), "1", "0"),
            "SHA512=" & If(IsHashRequired("SHA512"), "1", "0"),
            "MD5=" & If(IsHashRequired("MD5"), "1", "0"),
            "CRC32=" & If(IsHashRequired("CRC32"), "1", "0"),
            "BLAKE3=" & If(IsHashRequired("BLAKE3"), "1", "0"),
            "XxHash3=" & If(IsHashRequired("XxHash3"), "1", "0"),
            "XxHash128=" & If(IsHashRequired("XxHash128"), "1", "0")
        }
        Return String.Join(vbTab, parts)
    End Function

    Private Function IsHashRequired(name As String) As Boolean
        Select Case name
            Case "SHA1"
                If My.Settings.LTFSWriter_ChecksumEnabled_SHA1 Then Return True
                Return My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.SHA1
            Case "SHA256"
                If My.Settings.LTFSWriter_ChecksumEnabled_SHA256 Then Return True
                Return My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.SHA256
            Case "SHA512"
                If My.Settings.LTFSWriter_ChecksumEnabled_SHA512 Then Return True
                Return My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.SHA512
            Case "MD5"
                If My.Settings.LTFSWriter_ChecksumEnabled_MD5 Then Return True
                Return My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.MD5
            Case "CRC32"
                If My.Settings.LTFSWriter_ChecksumEnabled_CRC32 Then Return True
                Return My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.CRC32
            Case "BLAKE3"
                If My.Settings.LTFSWriter_ChecksumEnabled_BLAKE3 Then Return True
                Return My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.BLAKE3
            Case "XxHash3"
                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash3 Then Return True
                Return My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.XxHash3
            Case "XxHash128"
                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash128 Then Return True
                Return My.Settings.LTFSWriter_DeDupe AndAlso My.Settings.LTFSWriter_DedupeAlgorithm = ltfsindex.file.xattr.HashType.Available.XxHash128
        End Select
        Return False
    End Function

    Private Sub ParseReady(line As String)
        For Each part In line.Split({vbTab}, StringSplitOptions.RemoveEmptyEntries).Skip(1)
            Dim kv = part.Split({"="c}, 2)
            If kv.Length <> 2 Then Continue For
            Select Case kv(0)
                Case "slot_count"
                    _slotCount = ULong.Parse(kv(1))
                Case "data_offset"
                    _dataOffset = Long.Parse(kv(1))
                Case "map_size"
                    _mapSize = Long.Parse(kv(1))
            End Select
        Next
        If _slotCount = 0 OrElse _dataOffset <= 0 OrElse _mapSize <= 0 Then Throw New InvalidDataException("Invalid READY metadata: " & line)
    End Sub

    Private Sub OpenSharedObjects()
        _mapHandle = OpenFileMapping(FileMapAllAccess, False, _shmName)
        If _mapHandle = IntPtr.Zero Then Throw New ComponentModel.Win32Exception(Marshal.GetLastWin32Error())
        _basePtr = MapViewOfFile(_mapHandle, FileMapAllAccess, 0, 0, UIntPtr.Zero)
        If _basePtr = IntPtr.Zero Then Throw New ComponentModel.Win32Exception(Marshal.GetLastWin32Error())
        _dataEvent = OpenEvent(Synchronize, False, _dataEventName)
        If _dataEvent = IntPtr.Zero Then Throw New ComponentModel.Win32Exception(Marshal.GetLastWin32Error())
        _spaceEvent = OpenEvent(EventModifyState Or Synchronize, False, _spaceEventName)
        If _spaceEvent = IntPtr.Zero Then Throw New ComponentModel.Win32Exception(Marshal.GetLastWin32Error())
    End Sub

    Private Sub StdoutLoop()
        Try
            While Not _proc.HasExited
                Dim line = _proc.StandardOutput.ReadLine()
                If line Is Nothing Then Exit While
                If line.StartsWith("FILE_DONE" & vbTab) Then
                    ParseFileDone(line)
                ElseIf line.StartsWith("FILE_ERROR" & vbTab) Then
                    _errors.Enqueue(line)
                End If
            End While
        Catch ex As Exception
            _errors.Enqueue(ex.ToString())
        Finally
            _stdoutDone.Set()
        End Try
    End Sub

    Private Sub ParseFileDone(line As String)
        Dim parts = line.Split({vbTab}, StringSplitOptions.None)
        If parts.Length < 2 Then Return
        Dim idx As Long
        If Not Long.TryParse(parts(1), idx) Then Return
        Dim dict As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For i = 2 To parts.Length - 1
            Dim kv = parts(i).Split({"="c}, 2)
            If kv.Length = 2 Then dict(kv(0)) = kv(1)
        Next
        _done(idx) = dict
    End Sub

    Public Function ReadSlot(expectedFileIndex As Long, ct As CancellationToken) As Slot
        EnsureFileQueued(expectedFileIndex)
        While True
            ThrowIfFailed()
            ct.ThrowIfCancellationRequested()
            Dim meta = SlotMetaPtr(_readIndex)
            Dim status = Marshal.ReadInt32(meta, 0)
            If status = StatusFull Then
                Dim flags = Marshal.ReadInt32(meta, 4)
                Dim fileIndex = CLng(ReadUInt64(meta, 8))
                Dim fileOffset = CLng(ReadUInt64(meta, 16))
                Dim len = Marshal.ReadInt32(meta, 24)
                If fileIndex <> expectedFileIndex Then Throw New InvalidDataException($"fastreader file index mismatch. expected={expectedFileIndex} actual={fileIndex}")
                If (flags And FlagError) <> 0 Then Throw New IOException("fastreader read error")
                Return New Slot With {
                    .Index = _readIndex,
                    .FileIndex = fileIndex,
                    .FileOffset = fileOffset,
                    .Length = len,
                    .Flags = flags,
                    .DataPtr = SlotDataPtr(_readIndex)
                }
            End If
            WaitForSingleObject(_dataEvent, 50)
        End While
    End Function

    Public Sub AdvanceSlot(slot As Slot)
        If slot Is Nothing Then Return
        Dim meta = SlotMetaPtr(slot.Index)
        Marshal.WriteInt32(meta, 0, StatusEmpty)
        _readIndex = slot.Index + 1UL
        WriteUInt64(40, _readIndex)
        SetEvent(_spaceEvent)
    End Sub

    Public Sub Drain(fileIndex As Long, bytesToDrain As Long, Optional ct As CancellationToken = Nothing)
        EnsureFileQueued(fileIndex)
        Dim remain = bytesToDrain
        Dim eofSeen As Boolean = False
        While remain > 0
            Dim slot = ReadSlot(fileIndex, ct)
            If (slot.Flags And FlagEof) <> 0 Then
                AdvanceSlot(slot)
                eofSeen = True
                Exit While
            End If
            remain -= slot.Length
            AdvanceSlot(slot)
        End While
        If Not eofSeen Then DrainEof(fileIndex, ct)
    End Sub

    Public Sub DrainEof(fileIndex As Long, Optional ct As CancellationToken = Nothing)
        EnsureFileQueued(fileIndex)
        While True
            Dim slot = ReadSlot(fileIndex, ct)
            AdvanceSlot(slot)
            If (slot.Flags And FlagEof) <> 0 Then Exit While
        End While
    End Sub

    Public Function HashFile(fileIndex As Long, Optional timeoutMs As Integer = Timeout.Infinite) As Dictionary(Of String, String)
        ThrowIfFailed()
        If fileIndex < 0 OrElse fileIndex >= _writeList.Count Then Throw New ArgumentOutOfRangeException(NameOf(fileIndex))
        SyncLock _inputLock
            If _queuedFiles.Contains(fileIndex) Then Throw New InvalidOperationException("fastreader file is already queued")
            Dim ignored As Dictionary(Of String, String) = Nothing
            _done.TryRemove(fileIndex, ignored)
            Dim fr = _writeList(CInt(fileIndex))
            If fr Is Nothing OrElse fr.File Is Nothing Then Return New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            _proc.StandardInput.WriteLine($"HASH{vbTab}{fileIndex}{vbTab}{fr.File.length}{vbTab}{EncodePath(fr.SourcePath)}")
            _proc.StandardInput.Flush()
        End SyncLock
        Return WaitFileDone(fileIndex, timeoutMs)
    End Function

    Public Function WaitFileDone(fileIndex As Long, Optional timeoutMs As Integer = 30000) As Dictionary(Of String, String)
        Dim sw = Stopwatch.StartNew()
        While timeoutMs = Timeout.Infinite OrElse sw.ElapsedMilliseconds < timeoutMs
            ThrowIfFailed()
            Dim result As Dictionary(Of String, String) = Nothing
            If _done.TryGetValue(fileIndex, result) Then Return result
            Thread.Sleep(10)
        End While
        Return New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    End Function

    Public Sub WaitForFillFraction(fileIndex As Long, fraction As Double, stagnantTimeoutMs As Integer, ct As CancellationToken)
        QueueFile(fileIndex)
        Dim target = CLng(BufferCapacityBytes * fraction)
        If fileIndex >= 0 AndAlso fileIndex < _writeList.Count Then
            Dim fr = _writeList(CInt(fileIndex))
            If fr IsNot Nothing AndAlso fr.File IsNot Nothing Then target = Math.Min(target, fr.File.length)
        End If
        If target <= 0 Then Return
        Dim last = BufferedBytes
        Dim stagnant = Stopwatch.StartNew()
        While BufferedBytes < target
            ct.ThrowIfCancellationRequested()
            ThrowIfFailed()
            If HasBufferedEof(fileIndex) Then Exit While
            Dim cur = BufferedBytes
            If cur <> last Then
                last = cur
                stagnant.Restart()
            ElseIf stagnant.ElapsedMilliseconds >= stagnantTimeoutMs Then
                Exit While
            End If
            WaitForSingleObject(_dataEvent, 50)
        End While
    End Sub

    Public Function HasBufferedEof(fileIndex As Long) As Boolean
        If _basePtr = IntPtr.Zero Then Return False
        Dim writeIndex As ULong = ReadUInt64(32)
        If writeIndex <= _readIndex Then Return False
        Dim count As ULong = Math.Min(writeIndex - _readIndex, _slotCount)
        For i As ULong = 0 To count - 1UL
            Dim meta = SlotMetaPtr(_readIndex + i)
            If Marshal.ReadInt32(meta, 0) <> StatusFull Then Exit For
            Dim actualFileIndex = CLng(ReadUInt64(meta, 8))
            Dim flags = Marshal.ReadInt32(meta, 4)
            If actualFileIndex = fileIndex AndAlso (flags And FlagEof) <> 0 Then Return True
        Next
        Return False
    End Function

    Private Sub ThrowIfFailed()
        Dim msg As String = Nothing
        If _errors.TryDequeue(msg) Then Throw New IOException(msg)
        If _basePtr <> IntPtr.Zero AndAlso Marshal.ReadInt32(AddPtr(_basePtr, 52)) <> 0 Then Throw New IOException("fastreader reported an error")
        If _proc IsNot Nothing AndAlso _proc.HasExited AndAlso Marshal.ReadInt32(AddPtr(_basePtr, 48)) = 0 Then Throw New IOException("fastreader exited unexpectedly")
    End Sub

    Public Sub QueueFile(fileIndex As Long)
        ThrowIfFailed()
        If fileIndex < 0 OrElse fileIndex >= _writeList.Count Then Throw New ArgumentOutOfRangeException(NameOf(fileIndex))
        SyncLock _inputLock
            If _queuedFiles.Contains(fileIndex) Then Return
            Dim fr = _writeList(CInt(fileIndex))
            If fr IsNot Nothing AndAlso fr.File IsNot Nothing Then
                _proc.StandardInput.WriteLine($"FILE{vbTab}{fileIndex}{vbTab}{fr.File.length}{vbTab}{EncodePath(fr.SourcePath)}")
                _proc.StandardInput.Flush()
            End If
            _queuedFiles.Add(fileIndex)
        End SyncLock
    End Sub

    Private Sub EnsureFileQueued(fileIndex As Long)
        QueueFile(fileIndex)
    End Sub

    Private Function SlotMetaPtr(index As ULong) As IntPtr
        Return AddPtr(_basePtr, CLng(HeaderSize) + CLng(index Mod _slotCount) * CLng(SlotMetaSize))
    End Function

    Private Function SlotDataPtr(index As ULong) As IntPtr
        Return AddPtr(_basePtr, _dataOffset + CLng(index Mod _slotCount) * CLng(_slotSize))
    End Function

    Private Function ReadUInt64(offset As Integer) As ULong
        Return CULng(Marshal.ReadInt64(AddPtr(_basePtr, offset)))
    End Function

    Private Shared Function ReadUInt64(ptr As IntPtr, offset As Integer) As ULong
        Return CULng(Marshal.ReadInt64(AddPtr(ptr, offset)))
    End Function

    Private Sub WriteUInt64(offset As Integer, value As ULong)
        Marshal.WriteInt64(AddPtr(_basePtr, offset), CLng(value))
    End Sub

    Private Shared Function AddPtr(ptr As IntPtr, offset As Long) As IntPtr
        Return New IntPtr(ptr.ToInt64() + offset)
    End Function

    Private Shared Function EncodePath(path As String) As String
        Dim bytes = Encoding.Unicode.GetBytes(path)
        Dim sb As New StringBuilder(bytes.Length * 2)
        For Each b In bytes
            sb.Append(b.ToString("X2"))
        Next
        Return sb.ToString()
    End Function

    Private Shared Function FindHelperExe() As String
        Const helperName As String = "ltfscopy-fastreader.exe"
        Dim direct = Path.Combine(Application.StartupPath, helperName)
        If File.Exists(direct) Then Return direct

        Dim dir As New DirectoryInfo(Application.StartupPath)
        While dir IsNot Nothing
            Dim candidate = Path.Combine(dir.FullName, "LtfsFastReader", "target", "release", helperName)
            If File.Exists(candidate) Then Return candidate
            dir = dir.Parent
        End While

        Return direct
    End Function

    Public Sub Complete()
        Try
            If _proc IsNot Nothing AndAlso Not _proc.HasExited Then
                _proc.StandardInput.WriteLine("DONE")
                _proc.StandardInput.Flush()
            End If
        Catch
        End Try
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If _disposed Then Return
        _disposed = True
        Complete()
        Try
            If _proc IsNot Nothing AndAlso Not _proc.WaitForExit(1000) Then _proc.Kill()
        Catch
        End Try
        If _basePtr <> IntPtr.Zero Then UnmapViewOfFile(_basePtr) : _basePtr = IntPtr.Zero
        If _mapHandle <> IntPtr.Zero Then CloseHandle(_mapHandle) : _mapHandle = IntPtr.Zero
        If _dataEvent <> IntPtr.Zero Then CloseHandle(_dataEvent) : _dataEvent = IntPtr.Zero
        If _spaceEvent <> IntPtr.Zero Then CloseHandle(_spaceEvent) : _spaceEvent = IntPtr.Zero
        If _proc IsNot Nothing Then _proc.Dispose() : _proc = Nothing
    End Sub

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Private Shared Function OpenFileMapping(dwDesiredAccess As UInteger, <MarshalAs(UnmanagedType.Bool)> bInheritHandle As Boolean, lpName As String) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function MapViewOfFile(hFileMappingObject As IntPtr, dwDesiredAccess As UInteger, dwFileOffsetHigh As UInteger, dwFileOffsetLow As UInteger, dwNumberOfBytesToMap As UIntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function UnmapViewOfFile(lpBaseAddress As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Private Shared Function OpenEvent(dwDesiredAccess As UInteger, <MarshalAs(UnmanagedType.Bool)> bInheritHandle As Boolean, lpName As String) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function SetEvent(hEvent As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function WaitForSingleObject(hHandle As IntPtr, dwMilliseconds As UInteger) As UInteger
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function CloseHandle(hObject As IntPtr) As Boolean
    End Function
End Class
