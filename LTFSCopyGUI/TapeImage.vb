Imports System
Imports System.IO
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.WindowsAPICodePack.Sensors
Imports System.ComponentModel
Public Class TapeStreamMapping
    Public Shared MappingTable As New SerializableDictionary(Of IntPtr, TapeImage)
End Class

<Serializable>
Public Class TapeImage
    Implements IDisposable

    Public Class SenseData
        Public Shared BlankCheck As Byte() = {&HF0, 0, 8, 0, 0, 0, 0, &H10, 0, 0, 0, 0, 0, 5, 0, 0, &H50, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        Public Shared MediumError As Byte() = {&H70, 0, 3, 0, 0, 0, 0, &H10, 0, 0, 0, 0, &H31, 0, 0, 0, &H50, &H89, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        Public Shared NotPresent As Byte() = {&H70, 0, 2, 0, 0, 0, 0, &H10, 0, 0, 0, 0, &H3A, 0, 0, 0, &H94, &H50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        Public Shared NotFound As Byte() = {&H70, 0, 8, 0, 0, 0, 0, &H10, 0, 0, 0, 0, 0, 5, 0, 0, &H34, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        Public Shared FileMark As Byte() = {&HF0, 0, &H80, 0, 8, 0, 0, &H10, 0, 0, 0, 0, 0, 1, 0, 0, &H30, &H21, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        Public Shared EOD As Byte() = {&HF0, 0, 8, 0, 0, 0, 0, &H10, 0, 0, 0, 0, 0, 5, 0, 0, &H34, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        Public Shared NoSense As Byte() = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
    End Class
    <Xml.Serialization.XmlIgnore>
    Public Property idxFile As IO.FileInfo
    <Xml.Serialization.XmlIgnore>
    Public ReadOnly Property idxPath As String
        Get
            If idxFile Is Nothing Then
                Return Nothing
            Else
                Return idxFile.DirectoryName
            End If
        End Get
    End Property

    <Xml.Serialization.XmlIgnore>
    Public Property Position As New TapeUtils.PositionData

    Public Property DatesetLength As Long = 2473000

    Public Property PartitionMappingFile As New SerializableDictionary(Of Integer, String)
    Public Property PartitionEOD As New SerializableDictionary(Of Integer, Long)
    Public Property FilemarkBlockIndex As New SerializableDictionary(Of Integer, List(Of Long))
    <Xml.Serialization.XmlIgnore>
    Public ReadOnly Property PartitionCount As Integer
        Get
            Return PartitionMappingFile.Keys.Count
        End Get
    End Property
    <Xml.Serialization.XmlIgnore>
    Public ReadOnly Property CurrentStream As IO.Stream
        Get
            Return PartitionMappingStream(Position.PartitionNumber)
        End Get
    End Property
    <Xml.Serialization.XmlIgnore>
    Public ReadOnly Property CurrentFileOffset As Long
        Get
            Return DatesetLength * Position.SetNumber + CurrentIntraSetBlockOffset
        End Get
    End Property

    Public Property Compressed As Boolean = True
    Private PartitionMappingStream As New Dictionary(Of Integer, IO.Stream)
    Private CurrentDatasetID As Integer = 0
    Private CurrentIntraSetBlockOffset As Integer = 0
    Public Const BlockHeaderLen As Integer = 16
    Public ReadOnly Property CurrentSetResidueBytes As Integer
        Get
            Return DatesetLength - CurrentIntraSetBlockOffset
        End Get
    End Property

    Public Function GetSerializedString() As String
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(TapeImage))
        Dim sb As New Text.StringBuilder
        Dim t As New IO.StringWriter(sb)
        writer.Serialize(t, Me)
        Return sb.ToString()
    End Function
    Public Shared Function FromXML(s As String) As TapeImage
        Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(TapeImage))
        Dim t As IO.TextReader = New IO.StringReader(s)
        Return CType(reader.Deserialize(t), TapeImage)
    End Function
    Public Sub New()

    End Sub
    Public Sub New(filename As String, Optional ByVal PartitionCount As Integer = 1, Optional ByVal Compressed As Boolean = True)
        If IO.File.Exists(filename) AndAlso (New IO.FileInfo(filename)).Length = 0 Then
            IO.File.Delete(filename)
        End If
        If IO.File.Exists(filename) Then
            OpenFile(filename)
        Else
            CreateNewFile(filename, PartitionCount, Compressed)
        End If
    End Sub
    Public Sub OpenFile(filename As String)
        Dim idx As TapeImage = TapeImage.FromXML(IO.File.ReadAllText(filename))
        idxFile = New IO.FileInfo(filename)
        With idx
            DatesetLength = .DatesetLength
            PartitionMappingFile = .PartitionMappingFile
            FilemarkBlockIndex = .FilemarkBlockIndex
            PartitionEOD = .PartitionEOD
            PartitionMappingStream = New Dictionary(Of Integer, Stream)
        End With
        For Each id As Integer In PartitionMappingFile.Keys
            If PartitionMappingFile(id).ToLower().EndsWith(".lcgimg.zst") Then
                Compressed = True
                PartitionMappingStream.Add(id, New ZstdSharp.CompressionStream(New FileStream(IO.Path.Combine(idxPath, PartitionMappingFile(id)), FileMode.Open), 9, leaveOpen:=False))
            Else
                Compressed = False
                PartitionMappingStream.Add(id, New FileStream(IO.Path.Combine(idxPath, PartitionMappingFile(id)), FileMode.Open))
            End If
        Next
        Position = New TapeUtils.PositionData()
    End Sub
    Public Sub OpenStream(idx As TapeImage, partitions As List(Of Stream))
        With idx
            DatesetLength = .DatesetLength
            PartitionMappingFile = .PartitionMappingFile
            FilemarkBlockIndex = .FilemarkBlockIndex
            PartitionEOD = .PartitionEOD
            PartitionMappingStream = New Dictionary(Of Integer, Stream)
        End With
        For i As Integer = 0 To partitions.Count - 1
            PartitionMappingStream.Add(i, partitions(i))
        Next
        Position = New TapeUtils.PositionData()
    End Sub
    Public Sub CreateNewFile(filename As String, Optional ByVal PartitionCount As Integer = 1, Optional ByVal Compressed As Boolean = True)
        idxFile = New IO.FileInfo(filename)
        Dim name As String = idxFile.Name.Substring(0, idxFile.Name.Length - idxFile.Extension.Length)
        Me.Compressed = Compressed
        PartitionMappingFile = New SerializableDictionary(Of Integer, String)
        PartitionMappingStream = New Dictionary(Of Integer, Stream)
        FilemarkBlockIndex = New SerializableDictionary(Of Integer, List(Of Long))
        PartitionEOD = New SerializableDictionary(Of Integer, Long)
        For i As Integer = 0 To PartitionCount - 1
            Dim imgfilename As String = If(Me.Compressed, $"{name}.{i}.lcgimg.zst", $"{name}.{i}.lcgimg")
            PartitionMappingFile.Add(i, imgfilename)
            IO.File.Create(IO.Path.Combine(idxPath, imgfilename)).Close()
            If Me.Compressed Then
                PartitionMappingStream.Add(i, New ZstdSharp.CompressionStream(New FileStream(IO.Path.Combine(idxPath, imgfilename), FileMode.Open), 9, leaveOpen:=False))
            Else
                PartitionMappingStream.Add(i, New FileStream(IO.Path.Combine(idxPath, imgfilename), FileMode.Open))
            End If
            FilemarkBlockIndex.Add(i, New List(Of Long))
            PartitionEOD.Add(i, 0)
        Next
        IO.File.WriteAllText(filename, Me.GetSerializedString())
    End Sub

    Public Sub ResetPartitionNumber(PartitionCount As Integer)
        CloseFile()
        Dim name As String = idxFile.Name.Substring(0, idxFile.Name.Length - idxFile.Extension.Length)
        PartitionMappingFile = New SerializableDictionary(Of Integer, String)
        PartitionMappingStream = New Dictionary(Of Integer, Stream)
        FilemarkBlockIndex = New SerializableDictionary(Of Integer, List(Of Long))
        PartitionEOD = New SerializableDictionary(Of Integer, Long)
        For i As Integer = 0 To PartitionCount - 1
            Dim imgfilename As String = If(Me.Compressed, $"{name}.{i}.lcgimg.zst", $"{name}.{i}.lcgimg")
            PartitionMappingFile.Add(i, imgfilename)
            IO.File.Create(IO.Path.Combine(idxPath, imgfilename)).Close()
            If Compressed Then
                PartitionMappingStream.Add(i, New ZstdSharp.CompressionStream(New FileStream(IO.Path.Combine(idxPath, imgfilename), FileMode.Open), 9, leaveOpen:=False))
            Else
                PartitionMappingStream.Add(i, New FileStream(IO.Path.Combine(idxPath, imgfilename), FileMode.Open))
            End If
            FilemarkBlockIndex.Add(i, New List(Of Long))
            PartitionEOD.Add(i, 0)
        Next
    End Sub
    Public Sub ReOpen()
        CloseFile()
        OpenFile(idxFile.FullName)
    End Sub
    Public Sub CloseFile()
        Try
            IO.File.WriteAllText(idxFile.FullName, Me.GetSerializedString())
            For i As Integer = 0 To PartitionCount - 1
                PartitionMappingStream(i).Close()
            Next
        Catch ex As Exception

        End Try
    End Sub

    Public Sub WriteBlock(data As Byte(), Optional ByVal len As Integer = -1)
        If len = -1 Then len = data.Length
        Dim blocklen As Integer = Math.Min(data.Length, len)
        If CurrentSetResidueBytes < BlockHeaderLen Then
            CurrentStream.SetLength(CurrentFileOffset + CurrentSetResidueBytes + DatesetLength)
            CurrentStream.Seek(CurrentFileOffset, SeekOrigin.Begin)
            If CurrentSetResidueBytes > 0 Then
                Dim paddingdata(CurrentSetResidueBytes - 1) As Byte
                CurrentStream.Write(paddingdata, 0, CurrentSetResidueBytes)
            End If
            Position.SetNumber += 1
            CurrentIntraSetBlockOffset = 0
        End If
        If blocklen = 0 Then
            CurrentStream.SetLength(CurrentFileOffset + BlockHeaderLen)
            CurrentStream.Seek(CurrentFileOffset, SeekOrigin.Begin)
            CurrentStream.Write(GetByteArray(Position.BlockNumber), 0, 8)
            CurrentStream.Write(GetByteArray(0I), 0, 4)
            CurrentStream.Write(GetByteArray(0I), 0, 4)
            While FilemarkBlockIndex(Position.PartitionNumber).Count > 0 AndAlso FilemarkBlockIndex(Position.PartitionNumber).Last >= Position.BlockNumber
                FilemarkBlockIndex(Position.PartitionNumber).RemoveAt(FilemarkBlockIndex(Position.PartitionNumber).Count - 1)
            End While
            FilemarkBlockIndex(Position.PartitionNumber).Add(Position.BlockNumber)
            Position.BlockNumber += 1
            Position.FileNumber += 1
            CurrentIntraSetBlockOffset += BlockHeaderLen
        Else
            Dim residue As Integer = blocklen
            While residue > 0
                If CurrentSetResidueBytes < BlockHeaderLen Then
                    CurrentStream.SetLength(CurrentFileOffset + CurrentSetResidueBytes + DatesetLength)
                    CurrentStream.Seek(CurrentFileOffset, SeekOrigin.Begin)
                    If CurrentSetResidueBytes > 0 Then
                        Dim paddingdata(CurrentSetResidueBytes - 1) As Byte
                        CurrentStream.Write(paddingdata, 0, CurrentSetResidueBytes)
                    End If
                    Position.SetNumber += 1
                    CurrentIntraSetBlockOffset = 0
                End If
                Dim writelen As Integer = Math.Min(residue, CurrentSetResidueBytes - BlockHeaderLen)
                CurrentStream.SetLength(CurrentFileOffset + BlockHeaderLen + writelen)
                CurrentStream.Seek(CurrentFileOffset, SeekOrigin.Begin)
                CurrentStream.Write(GetByteArray(Position.BlockNumber), 0, 8)
                CurrentStream.Write(GetByteArray(blocklen), 0, 4)
                CurrentStream.Write(GetByteArray(writelen), 0, 4)
                CurrentIntraSetBlockOffset += writelen + BlockHeaderLen
                If writelen > 0 Then
                    CurrentStream.Write(data, blocklen - residue, writelen)
                End If
                residue -= writelen
            End While
            Position.BlockNumber += 1
        End If
        PartitionEOD(Position.PartitionNumber) = Position.BlockNumber
    End Sub
    Public Sub WriteFilemark(Optional ByVal count As Integer = 1)
        If count = 0 Then
            For Each s As Stream In PartitionMappingStream.Values
                s.Flush()
            Next
        Else
            For i As Integer = 0 To count - 1
                WriteBlock({})
            Next
        End If
    End Sub

    Public Function ReadBlock(ByRef sense As Byte()) As Byte()
        If CurrentFileOffset + BlockHeaderLen > CurrentStream.Length Then
            sense = SenseData.EOD
            Return {}
        End If
        If CurrentSetResidueBytes < BlockHeaderLen Then
            Position.SetNumber += 1
            CurrentIntraSetBlockOffset = 0
        End If
        If CurrentFileOffset <> CurrentStream.Position Then
            CurrentStream.Position = CurrentFileOffset
        End If
        Dim BlockHeader(BlockHeaderLen - 1) As Byte
        CurrentStream.Read(BlockHeader, 0, BlockHeaderLen)
        CurrentIntraSetBlockOffset += BlockHeaderLen
        Dim blocknum As ULong = GetULong(BlockHeader.Take(8).ToArray())
        Dim blocklen As Integer = GetInteger(BlockHeader.Skip(8).Take(4).ToArray())
        Dim blockfraglen As Integer = GetInteger(BlockHeader.Skip(12).Take(4).ToArray())
        If blocknum = 0 AndAlso blocklen = 0 Then
            If Position.SetNumber <> 0 OrElse CurrentIntraSetBlockOffset <> BlockHeaderLen Then
                sense = SenseData.EOD
                CurrentIntraSetBlockOffset -= BlockHeaderLen
                Return {}
            End If
        End If
        Position.BlockNumber = blocknum + 1
        If blocklen = 0 Then
            sense = SenseData.FileMark
            Position.FileNumber += 1
            Return {}
        End If
        Dim result(blocklen - 1) As Byte
        Dim residue As Integer = blocklen
        Dim HeaderReaded As Boolean = True
        While residue > 0
            If HeaderReaded Then
                If CurrentSetResidueBytes = 0 Then
                    Position.SetNumber += 1
                    CurrentIntraSetBlockOffset = BlockHeaderLen
                    CurrentStream.Seek(CurrentFileOffset, SeekOrigin.Begin)
                End If
                HeaderReaded = False
            Else
                If CurrentSetResidueBytes <= BlockHeaderLen Then
                    Position.SetNumber += 1
                    CurrentIntraSetBlockOffset = BlockHeaderLen
                Else
                    CurrentIntraSetBlockOffset += BlockHeaderLen
                End If
                CurrentStream.Seek(CurrentFileOffset, SeekOrigin.Begin)
            End If
            Dim readlen As Integer = Math.Min(residue, CurrentSetResidueBytes)
            CurrentStream.Read(result, blocklen - residue, readlen)
            CurrentIntraSetBlockOffset += readlen
            residue -= readlen
        End While
        sense = SenseData.NoSense
        Position.BlockNumber += 1
        Return result
    End Function
    Public Sub ChangePartition(partition As Byte, ByRef sense As Byte())
        If PartitionCount = 1 Then Exit Sub
        If partition >= PartitionCount Then
            sense = SenseData.BlankCheck
        Else
            sense = SenseData.NoSense
            Position.PartitionNumber = partition
            Position.BlockNumber = 0
            Position.FileNumber = 0
            Position.SetNumber = 0
            CurrentIntraSetBlockOffset = 0
        End If
    End Sub

    Public Sub LocateByBlock(blockIndex As Long, ByRef sense As Byte())
        If blockIndex = 0 Then
            Position.SetNumber = 0
            Position.BlockNumber = 0
            Position.FileNumber = 0
            CurrentIntraSetBlockOffset = 0
            sense = SenseData.NoSense
            Exit Sub
        End If
        If blockIndex > PartitionEOD(Position.PartitionNumber) Then
            sense = SenseData.NotFound
            Exit Sub
        End If
        Dim currset As Long = Position.SetNumber
        Dim currblock As ULong = GetHeaderBlockNumber(currset)
        While currset > 0 AndAlso currblock = 0
            currset -= 1
            currblock = GetHeaderBlockNumber(currset)
        End While
        Dim nextset As Long = currset
        Dim nextblock As ULong = currblock
        Dim lastset As Long = Math.Ceiling(CurrentStream.Length / DatesetLength)
        Dim lastblock As ULong = PartitionEOD(Position.PartitionNumber)
        While (currblock >= blockIndex) OrElse (nextblock < blockIndex AndAlso nextset < lastset) OrElse nextset - currset > 1
            While currblock > blockIndex
                Dim delta As Integer = currset - currset \ 2
                If delta = 0 Then
                    currset = 0
                    currblock = 0
                    Exit While
                End If
                currset \= 2
                currblock = GetHeaderBlockNumber(currset)
            End While
            While nextblock <= blockIndex
                Dim delta As Integer = nextset - Math.Ceiling((nextset + lastset) / 2)
                If delta = 0 Then
                    nextset = lastset
                    nextblock = GetHeaderBlockNumber(nextset)
                    Exit While
                End If
                nextset = Math.Ceiling((nextset + lastset) / 2)
                nextblock = GetHeaderBlockNumber(nextset)
            End While
            If nextset - currset < 5 Then
                For i As Integer = nextset To currset + 1 Step -1
                    If GetHeaderBlockNumber(i) >= blockIndex Then
                        nextset = i
                    End If
                Next
            End If
            Dim midset As Long = (currset + nextset) \ 2
            If GetHeaderBlockNumber(midset) < blockIndex Then
                currset = midset
            Else
                nextset = midset
            End If

            If currset < 0 Then
                currset = 0
                currblock = 0
                Exit While
            End If
            currblock = GetHeaderBlockNumber(currset)
            nextblock = GetHeaderBlockNumber(nextset)
        End While
        Dim iofs As Long = 0
        Position.SetNumber = currset
        Position.FileNumber = 0
        While currblock <> blockIndex
            If iofs > DatesetLength - BlockHeaderLen Then
                Position.SetNumber = nextset
                Position.BlockNumber = nextblock
                iofs = 0
            End If
            Dim header As Byte() = GetHeaderBlock(Position.SetNumber, iofs)
            currblock = GetULong(header.Take(8).ToArray())
            If currblock = 0 AndAlso (Position.SetNumber > 0 OrElse iofs > 0) Then
                sense = SenseData.NotFound
                Exit Sub
            End If
            If currblock = blockIndex Then Exit While
            Dim datalen As Integer = GetInteger(header.Skip(12).Take(4).ToArray())
            iofs += BlockHeaderLen + datalen
        End While
        CurrentIntraSetBlockOffset = iofs
        Position.BlockNumber = blockIndex
        For i As Integer = 0 To FilemarkBlockIndex(Position.PartitionNumber).Count - 1
            If FilemarkBlockIndex(Position.PartitionNumber)(i) < Position.BlockNumber Then
                Position.FileNumber = i + 1
            End If
        Next
    End Sub
    Public Function GetHeaderBlockNumber(setnum As Long, Optional ByVal offset As Long = 0) As ULong
        Dim streampos As Long = CurrentStream.Position
        CurrentStream.Seek(setnum * DatesetLength + offset, SeekOrigin.Begin)
        Dim blkdata(7) As Byte
        CurrentStream.Read(blkdata, 0, 8)
        CurrentStream.Seek(streampos, SeekOrigin.Begin)
        Return GetULong(blkdata)
    End Function
    Public Function GetHeaderBlock(setnum As Long, Optional ByVal offset As Long = 0) As Byte()
        Dim streampos As Long = CurrentStream.Position
        CurrentStream.Seek(setnum * DatesetLength + offset, SeekOrigin.Begin)
        Dim blkdata(15) As Byte
        CurrentStream.Read(blkdata, 0, 16)
        CurrentStream.Seek(streampos, SeekOrigin.Begin)
        Return blkdata
    End Function

    Public Sub LocateByFilemark(filemarkIndex As Long, ByRef sense As Byte())
        If filemarkIndex = 0 Then
            Position.SetNumber = 0
            Position.BlockNumber = 0
            Position.FileNumber = 0
            CurrentIntraSetBlockOffset = 0
            sense = SenseData.NoSense
            Exit Sub
        End If
        If FilemarkBlockIndex(Position.PartitionNumber).Count > filemarkIndex Then
            LocateByBlock(FilemarkBlockIndex(Position.PartitionNumber)(filemarkIndex - 1), sense)
        Else
            sense = SenseData.NotFound
        End If
    End Sub
    Public Sub LocateToEOD(ByRef sense As Byte())
        CurrentIntraSetBlockOffset = 0
        If CurrentStream.Length < BlockHeaderLen Then
            Position.BlockNumber = 0
            Position.SetNumber = 0
            Position.FileNumber = 0
            Exit Sub
        End If
        Position.SetNumber = (CurrentStream.Length - BlockHeaderLen) \ DatesetLength
        Dim BlockHeader(BlockHeaderLen - 1) As Byte
        CurrentStream.Seek(CurrentFileOffset, SeekOrigin.Begin)
        CurrentStream.Read(BlockHeader, 0, BlockHeaderLen)
        CurrentIntraSetBlockOffset += BlockHeaderLen
        Dim blocknum As ULong = GetULong(BlockHeader.Take(8).ToArray())
        Dim blocklen As Integer = GetInteger(BlockHeader.Skip(8).Take(4).ToArray())
        Dim blockfraglen As Integer = GetInteger(BlockHeader.Skip(12).Take(4).ToArray())
        CurrentIntraSetBlockOffset += blockfraglen
        If blocknum = 0 AndAlso blocklen = 0 AndAlso blockfraglen = 0 Then
            If Position.SetNumber <> 0 OrElse CurrentIntraSetBlockOffset <> BlockHeaderLen Then
                sense = SenseData.NoSense
                Position.BlockNumber = 0
                CurrentIntraSetBlockOffset -= BlockHeaderLen
                Exit Sub
            End If
        Else
            ReadBlock(sense)
            blocknum = Position.BlockNumber
        End If
        While blocknum > 0
            Position.BlockNumber = blocknum
            If CurrentSetResidueBytes < BlockHeaderLen Then Exit Sub
            CurrentStream.Seek(CurrentFileOffset, SeekOrigin.Begin)
            If CurrentStream.Read(BlockHeader, 0, BlockHeaderLen) < BlockHeaderLen Then Exit While
            CurrentIntraSetBlockOffset += BlockHeaderLen
            blocknum = GetULong(BlockHeader.Take(8).ToArray())
            blocklen = GetInteger(BlockHeader.Skip(8).Take(4).ToArray())
            blockfraglen = GetInteger(BlockHeader.Skip(12).Take(4).ToArray())
            CurrentIntraSetBlockOffset += blockfraglen
        End While
        LocateByBlock(Position.BlockNumber, sense)
        ReadBlock(sense)
    End Sub


    Public Function ReadPosition() As TapeUtils.PositionData
        Return Position
    End Function

    Public Function GetInteger(data As Byte()) As Integer
        Return BitConverter.ToInt32(data, 0)
    End Function
    Public Function GetULong(data As Byte()) As ULong
        Return BitConverter.ToUInt64(data, 0)
    End Function
    Public Function GetByteArray(data As Integer) As Byte()
        Return BitConverter.GetBytes(data)
    End Function
    Public Function GetByteArray(data As ULong) As Byte()
        Return BitConverter.GetBytes(data)
    End Function
    ' 释放
    Public Sub Dispose() Implements IDisposable.Dispose

    End Sub
    Public Shared Sub test()
        Dim dir As String = IO.Path.Combine(Application.StartupPath, "testimg")
        Dim path As String = IO.Path.Combine(dir, "test.lcgidx")
        If IO.Directory.Exists(dir) Then
            IO.Directory.Delete(dir, True)
        End If
        IO.Directory.CreateDirectory(dir)
        Dim sense As Byte()
        Dim testimg As New TapeImage(path, 2)
        testimg.WriteBlock({0, 0, 0, 0})
        testimg.WriteFilemark()
        testimg.WriteBlock({0, 0, 0, 1})
        testimg.WriteFilemark()
        testimg.WriteBlock({0, 0, 0, 2})
        testimg.WriteBlock({0, 0, 0, 3})
        testimg.ChangePartition(1, sense)
        testimg.WriteBlock({0, 0, 0, 0})
        testimg.WriteFilemark()
        testimg.WriteBlock({0, 0, 0, 1})
        testimg.WriteFilemark()
        testimg.WriteBlock({0, 0, 0, 2})
        testimg.WriteBlock({0, 0, 0, 3})
        testimg.WriteBlock({0, 0, 0, 4})
        testimg.CloseFile()
        testimg.Dispose()
        testimg = New TapeImage(path)
        Dim data As Byte()
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        testimg.ChangePartition(1, sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        data = testimg.ReadBlock(sense)
        testimg.LocateByBlock(0, sense)
        testimg.LocateByBlock(1, sense)
        testimg.LocateByFilemark(0, sense)
        testimg.LocateByFilemark(1, sense)
        testimg.CloseFile()
    End Sub
End Class