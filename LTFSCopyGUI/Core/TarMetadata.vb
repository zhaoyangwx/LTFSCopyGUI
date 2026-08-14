Imports System.Collections.Generic
Imports System.IO
Imports System.IO.Hashing
Imports System.Runtime.InteropServices
Imports System.Text
Imports Newtonsoft.Json
Imports ZstdSharp

Public Class TarMetadata
    <JsonProperty("version")>
    Public Property Version As Integer = 1
    <JsonProperty("format")>
    Public Property Format As String = "tar"
    <JsonProperty("entries")>
    Public Property Entries As New List(Of TarMetadataEntry)
End Class

Public Class TarMetadataEntry
    <JsonProperty("path")>
    Public Property Path As String
    <JsonProperty("type")>
    Public Property Type As String
    <JsonProperty("dataOffset")>
    Public Property DataOffset As Long
    <JsonProperty("length")>
    Public Property Length As Long
    <JsonProperty("xxh3_128")>
    Public Property Xxh3128 As String
End Class

Public Class TarVirtualDirectory
    Public ReadOnly Property SourceFile As ltfsindex.file
    Public ReadOnly Property RelativePath As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property Parent As TarVirtualDirectory
    Public ReadOnly Property Directories As New List(Of TarVirtualDirectory)
    Public ReadOnly Property Files As New List(Of TarVirtualFile)

    Public Sub New(source As ltfsindex.file, relativePathValue As String, nameValue As String, parentValue As TarVirtualDirectory)
        SourceFile = source
        RelativePath = relativePathValue
        Name = nameValue
        Parent = parentValue
    End Sub
End Class

Public Class TarVirtualFile
    Public ReadOnly Property SourceFile As ltfsindex.file
    Public ReadOnly Property Entry As TarMetadataEntry
    Public ReadOnly Property Parent As TarVirtualDirectory

    Public ReadOnly Property Name As String
        Get
            If Entry Is Nothing OrElse String.IsNullOrEmpty(Entry.Path) Then Return ""
            Dim p As Integer = Entry.Path.LastIndexOf("/"c)
            If p < 0 Then Return Entry.Path
            Return Entry.Path.Substring(p + 1)
        End Get
    End Property

    Public Sub New(source As ltfsindex.file, entryValue As TarMetadataEntry, parentValue As TarVirtualDirectory)
        SourceFile = source
        Entry = entryValue
        Parent = parentValue
    End Sub
End Class

Public NotInheritable Class TarMetadataCodec
    Private Sub New()
    End Sub

    Public Shared Function Encode(metadata As TarMetadata) As String
        If metadata Is Nothing Then Throw New ArgumentNullException(NameOf(metadata))
        Dim json As Byte() = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(metadata, Formatting.None))
        Using compressed As New MemoryStream()
            Using zs As New CompressionStream(compressed, 9)
                zs.Write(json, 0, json.Length)
            End Using
            Return Convert.ToBase64String(compressed.ToArray())
        End Using
    End Function

    Public Shared Function TryDecode(value As String, ByRef metadata As TarMetadata) As Boolean
        metadata = Nothing
        If String.IsNullOrWhiteSpace(value) Then Return False
        Try
            Dim compressed As Byte() = Convert.FromBase64String(value)
            Using input As New MemoryStream(compressed, writable:=False)
                Using decompressed As New DecompressionStream(input)
                    Using output As New MemoryStream()
                        decompressed.CopyTo(output)
                        metadata = JsonConvert.DeserializeObject(Of TarMetadata)(Encoding.UTF8.GetString(output.ToArray()))
                    End Using
                End Using
            End Using
            If metadata Is Nothing OrElse metadata.Version <> 1 OrElse
                Not String.Equals(metadata.Format, "tar", StringComparison.OrdinalIgnoreCase) OrElse
                metadata.Entries Is Nothing Then
                metadata = Nothing
                Return False
            End If
            Return True
        Catch
            metadata = Nothing
            Return False
        End Try
    End Function
End Class

Public NotInheritable Class TarVirtualTreeBuilder
    Private Sub New()
    End Sub

    Public Shared Function TryBuild(source As ltfsindex.file, metadata As TarMetadata, ByRef root As TarVirtualDirectory) As Boolean
        root = Nothing
        If source Is Nothing OrElse metadata Is Nothing OrElse metadata.Entries Is Nothing Then Return False
        If metadata.Version <> 1 OrElse Not String.Equals(metadata.Format, "tar", StringComparison.OrdinalIgnoreCase) Then Return False

        Dim localRoot As New TarVirtualDirectory(source, "", source.name, Nothing)
        Dim directories As New Dictionary(Of String, TarVirtualDirectory)(StringComparer.OrdinalIgnoreCase)
        directories(String.Empty) = localRoot
        Dim files As New Dictionary(Of String, TarVirtualFile)(StringComparer.OrdinalIgnoreCase)

        Try
            For Each entry As TarMetadataEntry In metadata.Entries
                If entry Is Nothing Then Return False
                Dim normalized As String = NormalizePath(entry.Path)
                If String.IsNullOrEmpty(normalized) Then Return False
                If entry.Type Is Nothing Then Return False
                If entry.Type.Equals("directory", StringComparison.OrdinalIgnoreCase) Then
                    If entry.DataOffset <> 0 OrElse entry.Length <> 0 Then Return False
                    Dim directory = EnsureDirectory(localRoot, directories, normalized)
                    If directory Is Nothing Then Return False
                ElseIf entry.Type.Equals("file", StringComparison.OrdinalIgnoreCase) Then
                    If entry.DataOffset < 0 OrElse entry.Length < 0 OrElse
                        entry.DataOffset > source.length OrElse entry.Length > source.length - entry.DataOffset Then Return False
                    If String.IsNullOrEmpty(entry.Xxh3128) OrElse entry.Xxh3128.Length <> 32 OrElse
                        Not IsUpperHex(entry.Xxh3128) Then Return False
                    Dim separator As Integer = normalized.LastIndexOf("/"c)
                    Dim parentPath As String = If(separator < 0, String.Empty, normalized.Substring(0, separator))
                    Dim parent = EnsureDirectory(localRoot, directories, parentPath)
                    If parent Is Nothing Then Return False
                    If files.ContainsKey(normalized) Then
                        Dim oldFile = files(normalized)
                        oldFile.Parent.Files.Remove(oldFile)
                    End If
                    Dim virtualFile As New TarVirtualFile(source, New TarMetadataEntry With {
                        .Path = normalized,
                        .Type = "file",
                        .DataOffset = entry.DataOffset,
                        .Length = entry.Length,
                        .Xxh3128 = entry.Xxh3128.ToUpperInvariant()}, parent)
                    parent.Files.Add(virtualFile)
                    files(normalized) = virtualFile
                Else
                    Return False
                End If
            Next
            root = localRoot
            Return True
        Catch
            root = Nothing
            Return False
        End Try
    End Function

    Public Shared Function NormalizePath(value As String) As String
        If String.IsNullOrWhiteSpace(value) Then Return Nothing
        Dim path As String = value.Replace("\"c, "/"c)
        While path.StartsWith("/", StringComparison.Ordinal)
            path = path.Substring(1)
        End While
        While path.EndsWith("/", StringComparison.Ordinal)
            path = path.Substring(0, path.Length - 1)
        End While
        If String.IsNullOrEmpty(path) Then Return Nothing
        Dim parts As String() = path.Split("/"c)
        Dim clean As New List(Of String)
        For Each part As String In parts
            If String.IsNullOrEmpty(part) OrElse part = "." OrElse part = ".." Then Return Nothing
            If part.IndexOfAny(IO.Path.GetInvalidFileNameChars()) >= 0 Then Return Nothing
            clean.Add(part)
        Next
        Return String.Join("/", clean)
    End Function

    Private Shared Function EnsureDirectory(root As TarVirtualDirectory,
                                             directories As Dictionary(Of String, TarVirtualDirectory),
                                             path As String) As TarVirtualDirectory
        If path Is Nothing Then Return Nothing
        If path.Length = 0 Then Return root
        If directories.ContainsKey(path) Then Return directories(path)
        Dim separator As Integer = path.LastIndexOf("/"c)
        Dim parentPath As String = If(separator < 0, String.Empty, path.Substring(0, separator))
        Dim parent = EnsureDirectory(root, directories, parentPath)
        If parent Is Nothing Then Return Nothing
        Dim name As String = If(separator < 0, path, path.Substring(separator + 1))
        Dim result As New TarVirtualDirectory(root.SourceFile, path, name, parent)
        parent.Directories.Add(result)
        directories(path) = result
        Return result
    End Function

    Private Shared Function IsUpperHex(value As String) As Boolean
        For Each c As Char In value
            If Not ((c >= "0"c AndAlso c <= "9"c) OrElse (c >= "A"c AndAlso c <= "F"c)) Then Return False
        Next
        Return True
    End Function
End Class

Public NotInheritable Class TarMetadataScanner
    Private Const TarBlockSize As Integer = 512
    Private Const MaxMetadataPayload As Long = 16L * 1024L * 1024L

    Private Enum ScanState
        Header
        Data
        Padding
        Trailing
    End Enum

    Private Enum PayloadKind
        Skip
        RegularFile
        PaxExtended
        PaxGlobal
        GnuLongName
        GnuLongLink
    End Enum

    Private ReadOnly _metadata As New TarMetadata()
    Private ReadOnly _header(TarBlockSize - 1) As Byte
    Private ReadOnly _globalPax As New Dictionary(Of String, String)(StringComparer.Ordinal)
    Private ReadOnly _pendingPax As New Dictionary(Of String, String)(StringComparer.Ordinal)
    Private _headerBytes As Integer
    Private _position As Long
    Private _remaining As Long
    Private _padding As Long
    Private _zeroBlocks As Integer
    Private _state As ScanState = ScanState.Header
    Private _payloadKind As PayloadKind = PayloadKind.Skip
    Private _payload As MemoryStream
    Private _hash As XxHash128
    Private _currentEntry As TarMetadataEntry
    Private _pendingGnuName As String
    Private _failed As Boolean
    Private _errorMessage As String

    Public ReadOnly Property Failed As Boolean
        Get
            Return _failed
        End Get
    End Property

    Public ReadOnly Property ErrorMessage As String
        Get
            Return _errorMessage
        End Get
    End Property

    Public Sub Process(buffer As Byte(), offset As Integer, count As Integer)
        If _failed OrElse buffer Is Nothing OrElse count <= 0 Then Return
        Try
            If offset < 0 OrElse count < 0 OrElse offset > buffer.Length - count Then Throw New ArgumentOutOfRangeException()
            Dim currentOffset As Integer = offset
            Dim remainingInput As Integer = count
            While remainingInput > 0 AndAlso Not _failed
                Select Case _state
                    Case ScanState.Header
                        Dim take As Integer = Math.Min(remainingInput, TarBlockSize - _headerBytes)
                        System.Buffer.BlockCopy(buffer, currentOffset, _header, _headerBytes, take)
                        _headerBytes += take
                        currentOffset += take
                        remainingInput -= take
                        _position += take
                        If _headerBytes = TarBlockSize Then
                            _headerBytes = 0
                            ParseHeader()
                        End If
                    Case ScanState.Data
                        Dim take As Integer = CInt(Math.Min(CLng(remainingInput), _remaining))
                        If _payloadKind = PayloadKind.RegularFile Then
                            _hash.Append(New ArraySegment(Of Byte)(buffer, currentOffset, take))
                        ElseIf _payloadKind = PayloadKind.PaxExtended OrElse
                            _payloadKind = PayloadKind.PaxGlobal OrElse
                            _payloadKind = PayloadKind.GnuLongName OrElse
                            _payloadKind = PayloadKind.GnuLongLink Then
                            If _payload Is Nothing Then _payload = New MemoryStream()
                            If _payload.Length + take > MaxMetadataPayload Then Throw New InvalidDataException("tar metadata payload is too large")
                            _payload.Write(buffer, currentOffset, take)
                        End If
                        currentOffset += take
                        remainingInput -= take
                        _position += take
                        _remaining -= take
                        If _remaining = 0 Then FinishPayload()
                    Case ScanState.Padding
                        Dim take As Integer = CInt(Math.Min(CLng(remainingInput), _padding))
                        currentOffset += take
                        remainingInput -= take
                        _position += take
                        _padding -= take
                        If _padding = 0 Then _state = ScanState.Header
                    Case ScanState.Trailing
                        For i As Integer = 0 To remainingInput - 1
                            If buffer(currentOffset + i) <> 0 Then Throw New InvalidDataException("non-zero bytes after tar end blocks")
                        Next
                        _position += remainingInput
                        currentOffset += remainingInput
                        remainingInput = 0
                End Select
            End While
        Catch ex As Exception
            Fail(ex)
        End Try
    End Sub

    Public Sub ProcessUnmanaged(data As IntPtr, count As Integer)
        If _failed OrElse data = IntPtr.Zero OrElse count <= 0 Then Return
        Try
            Dim buffer(count - 1) As Byte
            Marshal.Copy(data, buffer, 0, count)
            Process(buffer, 0, count)
        Catch ex As Exception
            Fail(ex)
        End Try
    End Sub

    Public Function TryComplete(expectedLength As Long, ByRef metadata As TarMetadata) As Boolean
        metadata = Nothing
        If _failed Then Return False
        Try
            If _headerBytes <> 0 OrElse _state = ScanState.Data OrElse _state = ScanState.Padding OrElse
                _zeroBlocks < 2 OrElse _position <> expectedLength Then Throw New InvalidDataException("incomplete tar stream")
            metadata = _metadata
            Return True
        Catch ex As Exception
            Fail(ex)
            Return False
        End Try
    End Function

    Private Sub ParseHeader()
        If IsZeroBlock(_header) Then
            _zeroBlocks += 1
            If _zeroBlocks >= 2 Then _state = ScanState.Trailing
            Return
        End If
        If _zeroBlocks > 0 Then Throw New InvalidDataException("tar data follows end blocks")
        If Not ValidateChecksum(_header) Then Throw New InvalidDataException("invalid tar header checksum")

        Dim typeFlag As Char = ChrW(_header(156))
        Dim size As Long = ParseTarNumber(_header, 124, 12)
        If size < 0 Then Throw New InvalidDataException("negative tar member size")

        If typeFlag = "x"c OrElse typeFlag = "g"c OrElse typeFlag = "L"c OrElse typeFlag = "K"c Then
            _payloadKind = If(typeFlag = "x"c, PayloadKind.PaxExtended,
                              If(typeFlag = "g"c, PayloadKind.PaxGlobal,
                                 If(typeFlag = "L"c, PayloadKind.GnuLongName, PayloadKind.GnuLongLink)))
            _payload = New MemoryStream()
            BeginPayload(size)
            Return
        End If

        Dim name As String = ReadName(_header)
        Dim paxPath As String = GetPaxValue("path")
        If Not String.IsNullOrEmpty(paxPath) Then name = paxPath
        If String.IsNullOrEmpty(paxPath) AndAlso Not String.IsNullOrEmpty(_pendingGnuName) Then name = _pendingGnuName
        Dim paxSize As String = GetPaxValue("size")
        If Not String.IsNullOrEmpty(paxSize) Then
            Dim parsedSize As Long
            If Not Long.TryParse(paxSize, Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture, parsedSize) OrElse parsedSize < 0 Then
                Throw New InvalidDataException("invalid PAX size")
            End If
            size = parsedSize
        End If
        _pendingPax.Clear()
        _pendingGnuName = Nothing

        Dim normalized As String = TarVirtualTreeBuilder.NormalizePath(name)
        If typeFlag = "0"c OrElse typeFlag = ChrW(0) OrElse typeFlag = "7"c Then
            If String.IsNullOrEmpty(normalized) Then Throw New InvalidDataException("tar regular file has no safe path")
            _payloadKind = PayloadKind.RegularFile
            _currentEntry = New TarMetadataEntry With {
                .Path = normalized,
                .Type = "file",
                .DataOffset = _position,
                .Length = size,
                .Xxh3128 = Nothing}
            _hash = New XxHash128()
        ElseIf typeFlag = "5"c Then
            If String.IsNullOrEmpty(normalized) Then Throw New InvalidDataException("tar directory has no safe path")
            _payloadKind = PayloadKind.Skip
            _currentEntry = New TarMetadataEntry With {
                .Path = normalized,
                .Type = "directory",
                .DataOffset = 0,
                .Length = 0,
                .Xxh3128 = Nothing}
        Else
            _payloadKind = PayloadKind.Skip
            _currentEntry = Nothing
        End If
        BeginPayload(size)
    End Sub

    Private Sub BeginPayload(size As Long)
        If size < 0 Then Throw New InvalidDataException("negative tar payload")
        _remaining = size
        _padding = (TarBlockSize - (size Mod TarBlockSize)) Mod TarBlockSize
        _state = ScanState.Data
        If _remaining = 0 Then FinishPayload()
    End Sub

    Private Sub FinishPayload()
        Select Case _payloadKind
            Case PayloadKind.RegularFile
                _currentEntry.Xxh3128 = BitConverter.ToString(_hash.GetHashAndReset()).Replace("-", "").ToUpperInvariant()
                _metadata.Entries.Add(_currentEntry)
            Case PayloadKind.Skip
                If _currentEntry IsNot Nothing Then _metadata.Entries.Add(_currentEntry)
            Case PayloadKind.PaxExtended, PayloadKind.PaxGlobal
                ParsePax(_payload.ToArray(), _payloadKind = PayloadKind.PaxGlobal)
            Case PayloadKind.GnuLongName
                _pendingGnuName = DecodeMetadataText(_payload.ToArray()).TrimEnd(ChrW(0))
            Case PayloadKind.GnuLongLink
                ' Hard-link targets are intentionally not indexed.
        End Select
        If _payload IsNot Nothing Then
            _payload.Dispose()
            _payload = Nothing
        End If
        _currentEntry = Nothing
        _payloadKind = PayloadKind.Skip
        If _padding = 0 Then _state = ScanState.Header Else _state = ScanState.Padding
    End Sub

    Private Sub ParsePax(data As Byte(), globalScope As Boolean)
        Dim offset As Integer = 0
        Dim target As Dictionary(Of String, String) = If(globalScope, _globalPax, _pendingPax)
        While offset < data.Length
            Dim space As Integer = Array.IndexOf(data, CByte(AscW(" "c)), offset)
            If space <= offset Then Throw New InvalidDataException("invalid PAX record length")
            Dim recordLengthText As String = Encoding.ASCII.GetString(data, offset, space - offset)
            Dim recordLength As Integer
            If Not Integer.TryParse(recordLengthText, Globalization.NumberStyles.None, Globalization.CultureInfo.InvariantCulture, recordLength) OrElse
                recordLength <= space - offset OrElse offset + recordLength > data.Length Then Throw New InvalidDataException("invalid PAX record")
            Dim record As String = Encoding.UTF8.GetString(data, space + 1, recordLength - (space - offset) - 1)
            If Not record.EndsWith(vbLf, StringComparison.Ordinal) Then Throw New InvalidDataException("PAX record without newline")
            record = record.Substring(0, record.Length - 1)
            Dim equals As Integer = record.IndexOf("="c)
            If equals <= 0 Then Throw New InvalidDataException("invalid PAX key/value")
            target(record.Substring(0, equals)) = record.Substring(equals + 1)
            offset += recordLength
        End While
    End Sub

    Private Function GetPaxValue(key As String) As String
        Dim value As String = Nothing
        If _pendingPax.TryGetValue(key, value) Then Return value
        If _globalPax.TryGetValue(key, value) Then Return value
        Return Nothing
    End Function

    Private Shared Function DecodeMetadataText(data As Byte()) As String
        Return New UTF8Encoding(False, True).GetString(data)
    End Function

    Private Shared Function ReadName(header As Byte()) As String
        Dim name As String = ReadField(header, 0, 100)
        Dim magic As String = ReadField(header, 257, 6)
        Dim prefix As String = ReadField(header, 345, 155)
        If magic.StartsWith("ustar", StringComparison.Ordinal) AndAlso Not String.IsNullOrEmpty(prefix) Then
            name = prefix & "/" & name
        End If
        Return name
    End Function

    Private Shared Function ReadField(data As Byte(), offset As Integer, length As Integer) As String
        Dim endOffset As Integer = offset
        While endOffset < offset + length AndAlso data(endOffset) <> 0
            endOffset += 1
        End While
        Return Encoding.UTF8.GetString(data, offset, endOffset - offset).TrimEnd(" "c)
    End Function

    Private Shared Function ParseTarNumber(data As Byte(), offset As Integer, length As Integer) As Long
        If (data(offset) And &H80) <> 0 Then
            Dim value As Long = 0
            For i As Integer = 0 To length - 1
                Dim b As Integer = data(offset + i)
                If i = 0 Then b = b And &H7F
                If value > (Long.MaxValue >> 8) Then Throw New OverflowException()
                value = (value << 8) Or b
            Next
            Return value
        End If
        Dim result As Long = 0
        Dim found As Boolean = False
        For i As Integer = 0 To length - 1
            Dim b As Byte = data(offset + i)
            If b = 0 OrElse b = 32 Then Continue For
            If b < AscW("0"c) OrElse b > AscW("7"c) Then Throw New InvalidDataException("invalid tar numeric field")
            found = True
            If result > (Long.MaxValue >> 3) Then Throw New OverflowException()
            result = (result << 3) + (b - AscW("0"c))
        Next
        Return If(found, result, 0)
    End Function

    Private Shared Function ValidateChecksum(header As Byte()) As Boolean
        Dim expected As Long = ParseTarNumber(header, 148, 8)
        Dim actual As Long = 0
        For i As Integer = 0 To header.Length - 1
            If i >= 148 AndAlso i < 156 Then
                actual += 32
            Else
                actual += header(i)
            End If
        Next
        Return expected = actual
    End Function

    Private Shared Function IsZeroBlock(block As Byte()) As Boolean
        For Each b As Byte In block
            If b <> 0 Then Return False
        Next
        Return True
    End Function

    Private Sub Fail(ex As Exception)
        If _failed Then Return
        _failed = True
        _errorMessage = If(ex Is Nothing, "tar metadata scan failed", ex.Message)
    End Sub
End Class
