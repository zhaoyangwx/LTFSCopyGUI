<Serializable>
Public Class ltfsindex
    'Public Property version As String
    Public Property creator As String = My.Application.Info.ProductName & " " & My.Application.Info.Version.ToString(3) & " - Windows - TapeUtils"
    Public Property volumeuuid As Guid
    Public Property generationnumber As Integer
    Public Property updatetime As String
    Public Enum PartitionLabel
        a
        b
    End Enum
    <Serializable>
    Public Class PartitionDef

        Public Property partition As PartitionLabel = PartitionLabel.a
        Public Property startblock As Long
    End Class

    Public Property location As New PartitionDef
    Public Property previousgenerationlocation As New PartitionDef
    Public Property allowpolicyupdate As Boolean
    <Serializable>
    Public Class policy
        Public Structure indexpartitioncriteria
            Public Property size As Long
        End Structure
    End Class
    Public Property dataplacementpolicy As policy
    Public Enum volumelockstateValue
        unlocked
        locked
        permlocked
    End Enum
    Public Property volumelockstate As volumelockstateValue = volumelockstateValue.unlocked
    Public Property highestfileuid As Long
    <Serializable>
    Public Class file
        Public Property name As String
        Public Property length As Long
        Public Property [readonly] As Boolean = False
        Public Property openforwrite As Boolean = True
        Public Property creationtime As String
        Public Property changetime As String
        Public Property modifytime As String
        Public Property accesstime As String
        Public Property backuptime As String
        Public Property fileuid As Long
        Public Property sha1 As String
            Get
                If Searializing Then Return Nothing
                Dim result As String = GetXAttr(xattr.HashType.SHA1)
                If result Is Nothing Then Return ""
                Return result
            End Get
            Set(value As String)
                If value Is Nothing Then Exit Property
                If value.Length <> 40 Then Exit Property
                SetXattr(xattr.HashType.SHA1, value)
            End Set
        End Property
        Public Property tag As String
        Public Class refFile
            Public FileName As String
        End Class
        <Xml.Serialization.XmlIgnore> Public fullpath As String
        <Xml.Serialization.XmlIgnore> Public Selected As Boolean = True
        <Xml.Serialization.XmlIgnore> Public WrittenBytes As Long = 0
        <Xml.Serialization.XmlIgnore> Public TempObj As Object
        <Xml.Serialization.XmlIgnore> Public SHA1ForeColor As Color = Color.Black
        <Xml.Serialization.XmlIgnore> Public MD5ForeColor As Color = Color.Black
        <Xml.Serialization.XmlIgnore> Public ItemForeColor As Color = Color.Black
        <Serializable>
        Public Class xattr
            Public Property key As String
            Public Property value As String
            Public Class HashType
                Public Shared ReadOnly Property CRC32 As String = "ltfs.hash.crc32sum"
                Public Shared ReadOnly Property MD5 As String = "ltfs.hash.md5sum"
                Public Shared ReadOnly Property SHA1 As String = "ltfs.hash.sha1sum"
                Public Shared ReadOnly Property SHA256 As String = "ltfs.hash.sha256sum"
                Public Shared ReadOnly Property SHA512 As String = "ltfs.hash.sha512sum"
            End Class
            Public Shared Function FromXMLList(s As String) As List(Of xattr)
                Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(List(Of xattr)))
                Dim t As IO.TextReader = New IO.StringReader(s)
                Return CType(reader.Deserialize(t), List(Of xattr))
            End Function
        End Class
        Public Property extendedattributes As New List(Of xattr)
        Public Function GetXAttrText() As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(List(Of xattr)))
            Dim sb As New Text.StringBuilder
            Dim t As New IO.StringWriter(sb)
            writer.Serialize(t, extendedattributes)
            Return sb.ToString()
        End Function
        Public Function GetXAttr(key As String, Optional ByVal ReturnBlankIfNotFound As Boolean = False) As String
            For Each x As xattr In extendedattributes
                If x.key.ToLower = key.ToLower Then Return x.value
            Next
            If ReturnBlankIfNotFound Then
                Return ""
            Else
                Return Nothing
            End If
        End Function

        Public Sub SetXattr(key As String, value As String)
            For Each x As xattr In extendedattributes
                If x.key.ToLower = key.ToLower Then
                    x.value = value
                    Exit Sub
                End If
            Next
            extendedattributes.Add(New xattr With {.key = key, .value = value})
        End Sub

        <Serializable>
        Public Class extent
            Public Property fileoffset As Long
            Public Property partition As PartitionLabel
            Public Property startblock As Long
            Public Property byteoffset As Long
            Public Property bytecount As Long
            <Xml.Serialization.XmlIgnore> Public Property TempInfo As Object
        End Class
        Public Property extentinfo As New List(Of extent)
        Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(file))
            Dim sb As New Text.StringBuilder
            Dim t As New IO.StringWriter(sb)
            Dim ns As New Xml.Serialization.XmlSerializerNamespaces({New Xml.XmlQualifiedName("v", "1")})
            writer.Serialize(t, Me, ns)
            sb.Remove(0, 41)
            Return sb.ToString().Replace("<file xmlns:v=""1""", "<file")
        End Function

    End Class
    <Serializable>
    Public Class directory
        Public Property name As String
        Public Property [readonly] As Boolean = False
        Public Property creationtime As String
        Public Property changetime As String
        Public Property modifytime As String
        Public Property accesstime As String
        Public Property backuptime As String
        Public Property fileuid As Long
        Public Property contents As New contentsDef
        Public Property tag As String

        Private _TotalFiles, _TotalDirectories, _TotalFilesUnwritten As Long
        <Xml.Serialization.XmlIgnore> Public ReadOnly Property TotalFiles
            Get
                If _TotalDirectories = 0 AndAlso contents._directory IsNot Nothing AndAlso contents._directory.Count > 0 Then
                    RefreshCount()
                End If
                If _TotalFiles = 0 AndAlso contents._file IsNot Nothing AndAlso contents._file.Count > 0 Then
                    RefreshCount()
                End If
                Return _TotalFiles
            End Get
        End Property
        <Xml.Serialization.XmlIgnore> Public ReadOnly Property TotalFilesUnwritten
            Get
                If _TotalDirectories = 0 AndAlso contents._directory IsNot Nothing AndAlso contents._directory.Count > 0 Then
                    RefreshCount()
                End If
                If _TotalFiles = 0 AndAlso contents._file IsNot Nothing AndAlso contents._file.Count > 0 Then
                    RefreshCount()
                End If
                If _TotalFilesUnwritten = 0 AndAlso contents.UnwrittenFiles IsNot Nothing AndAlso contents.UnwrittenFiles.Count > 0 Then
                    RefreshCount()
                End If
                Return _TotalFilesUnwritten
            End Get
        End Property
        <Xml.Serialization.XmlIgnore> Public ReadOnly Property TotalDirectories
            Get
                If _TotalDirectories = 0 AndAlso contents._directory IsNot Nothing AndAlso contents._directory.Count > 0 Then
                    RefreshCount()
                End If
                If _TotalFiles = 0 AndAlso contents._file IsNot Nothing AndAlso contents._file.Count > 0 Then
                    RefreshCount()
                End If
                Return _TotalDirectories
            End Get
        End Property
        Public Sub RefreshCount()
            If contents._directory Is Nothing OrElse contents._directory.Count = 0 Then
                If contents._file IsNot Nothing Then
                    _TotalFiles = contents._file.Count
                Else
                    _TotalFiles = 0
                End If
                If contents.UnwrittenFiles IsNot Nothing Then
                    _TotalFilesUnwritten = contents.UnwrittenFiles.Count
                Else
                    _TotalFilesUnwritten = 0
                End If
            Else
                If contents._file IsNot Nothing Then
                    _TotalFiles = contents._file.Count
                End If
                If contents.UnwrittenFiles IsNot Nothing Then
                    _TotalFilesUnwritten = contents.UnwrittenFiles.Count
                End If
                For Each d As directory In contents._directory
                    _TotalFiles += d.TotalFiles
                    _TotalFilesUnwritten += d.TotalFilesUnwritten
                Next
            End If
            If contents._directory IsNot Nothing Then
                _TotalDirectories = contents._directory.Count
                For Each d As directory In contents._directory
                    _TotalDirectories += d.TotalDirectories
                Next
            End If
        End Sub
        Public Sub DeepRefreshCount()
            _TotalFiles = 0
            _TotalDirectories = 0
            For Each d As directory In contents._directory
                d.DeepRefreshCount()
            Next
            RefreshCount()
        End Sub


        <Xml.Serialization.XmlIgnore> Public fullpath As String
        <Xml.Serialization.XmlIgnore> Public Selected As Boolean = True

        Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(directory))
            Dim sb As New Text.StringBuilder
            Dim t As New IO.StringWriter(sb)
            writer.Serialize(t, Me)
            Return sb.ToString()
        End Function
        Public Function SaveFile(FileName As String) As Boolean
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(directory))
            Dim ms As New IO.FileStream(FileName, IO.FileMode.Create)
            Dim t As IO.TextWriter = New IO.StreamWriter(ms, New System.Text.UTF8Encoding(False))
            Dim ns As New Xml.Serialization.XmlSerializerNamespaces({New Xml.XmlQualifiedName("v", "2.4.0")})
            writer.Serialize(t, Me, ns)
            t.Close()
            ms.Close()
            Return True
        End Function
        Public Shared Function FromXML(s As String) As directory
            Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(directory))
            Dim t As IO.TextReader = New IO.StringReader(s)
            Return CType(reader.Deserialize(t), directory)
        End Function
        Public Shared Function FromFile(FileName As String) As directory
            Dim result As directory
            Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(directory))
            Dim t As IO.StreamReader = New IO.StreamReader(FileName)
            result = CType(reader.Deserialize(t), directory)
            t.Close()
            Return result
        End Function
    End Class
    <Serializable>
    Public Class contentsDef
        Public Property _file As New List(Of file)
        Public Property _directory As New List(Of directory)
        <Xml.Serialization.XmlIgnore> Public UnwrittenFiles As New List(Of file)
        <Xml.Serialization.XmlIgnore> Public LastUnwrittenFilesCount As Integer
    End Class
    Public Property _file As New List(Of file)
    Public Property _directory As New List(Of directory)
    <Xml.Serialization.XmlIgnore> Public Shared Searializing As Boolean = False
    Public Sub Standarize()
        Exit Sub
        Dim q As New List(Of directory)
        For Each f As file In _file
            If f.sha1 IsNot Nothing Then
                If f.sha1.Length = 40 Then
                    f.SetXattr("ltfs.hash.sha1sum", f.sha1)
                End If
                f.sha1 = Nothing
            End If
        Next
        For Each d As directory In _directory
            q.Add(d)
        Next
        While q.Count > 0
            Dim qn As New List(Of directory)
            For Each d As directory In q
                For Each fn As file In d.contents._file
                    If fn.sha1 IsNot Nothing Then
                        If fn.sha1.Length = 40 Then
                            fn.SetXattr("ltfs.hash.sha1sum", fn.sha1)
                        End If
                        fn.sha1 = Nothing
                    End If
                Next
                For Each dn As directory In d.contents._directory
                    qn.Add(dn)
                Next
            Next
            q = qn
        End While
    End Sub
    Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
        Searializing = True
        Me.Standarize()
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(ltfsindex))
        Dim tmpf As String = $"{Application.StartupPath}\LCG_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
        Dim ms As New IO.FileStream(tmpf, IO.FileMode.Create)
        Dim t As IO.TextWriter = New IO.StreamWriter(ms, New System.Text.UTF8Encoding(False))
        Dim ns As New Xml.Serialization.XmlSerializerNamespaces({New Xml.XmlQualifiedName("v", "2.4.0")})
        writer.Serialize(t, Me, ns)
        ms.Close()
        Searializing = False
        Dim soutp As New IO.StreamReader(tmpf)
        Dim sout As New System.Text.StringBuilder
        While Not soutp.EndOfStream
            Dim sline As String = soutp.ReadLine()
            If sline.StartsWith("<?xml") Then
                sline = sline.Replace("utf-8", "UTF-8")
            End If
            If ReduceSize Then
                sline = sline.Replace("xmlns:v", "version")
                sline = sline.Replace("<_file />", "")
                sline = sline.Replace("<_directory />", "")
                sline = sline.Replace("<_file>", "")
                sline = sline.Replace("</_file>", "")
                sline = sline.Replace("<_directory>", "")
                sline = sline.Replace("</_directory>", "")
                sline = sline.TrimEnd(" ").TrimStart(" ")
            End If
            If sline.Length > 0 Then sout.AppendLine(sline)
        End While
        soutp.Close()
        My.Computer.FileSystem.DeleteFile(tmpf)
        Return sout.ToString()
    End Function
    Public Function SaveFile(FileName As String) As Boolean
        Searializing = True
        Me.Standarize()
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(ltfsindex))
        Dim tmpf As String = $"{Application.StartupPath}\LCG_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
        Dim ms As New IO.FileStream(tmpf, IO.FileMode.Create)
        Dim t As IO.TextWriter = New IO.StreamWriter(ms, New System.Text.UTF8Encoding(False))
        Dim ns As New Xml.Serialization.XmlSerializerNamespaces({New Xml.XmlQualifiedName("v", "2.4.0")})
        writer.Serialize(t, Me, ns)
        t.Close()
        ms.Close()
        Searializing = False
        Dim soutp As New IO.StreamReader(tmpf)

        Dim sout As New IO.StreamWriter(FileName, False, New Text.UTF8Encoding(False))
        While Not soutp.EndOfStream
            Dim sline As String = soutp.ReadLine()
            If sline.StartsWith("<?xml") Then
                sline = sline.Replace("utf-8", "UTF-8")
            End If
            sline = sline.Replace("xmlns:v", "version")
            sline = sline.Replace("<_file />", "")
            sline = sline.Replace("<_directory />", "")
            sline = sline.Replace("<_file>", "")
            sline = sline.Replace("</_file>", "")
            sline = sline.Replace("<_directory>", "")
            sline = sline.Replace("</_directory>", "")
            sline = sline.TrimEnd(" ").TrimStart(" ")
            If sline.Length > 0 Then sout.WriteLine(sline)
        End While
        soutp.Close()
        sout.Close()
        My.Computer.FileSystem.DeleteFile(tmpf)
        Return True
    End Function
    Public Shared Function FromXML(s As String) As ltfsindex
        Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(ltfsindex))
        Dim t As IO.TextReader = New IO.StringReader(s)
        Return CType(reader.Deserialize(t), ltfsindex)
    End Function
    Public Shared Function FromSchemaText(s As String) As ltfsindex
        s = s.Replace("<directory>", "<_directory><directory>")
        s = s.Replace("</directory>", "</directory></_directory>")
        s = s.Replace("<file>", "<_file><file>")
        s = s.Replace("</file>", "</file></_file>")
        s = s.Replace("%25", "%")
        Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(ltfsindex))
        Dim t As IO.TextReader = New IO.StringReader(s)
        Dim result As ltfsindex = CType(reader.Deserialize(t), ltfsindex)
        result.Standarize()
        Return result
    End Function
    Public Shared Function FromSchFile(FileName As String) As ltfsindex
        Dim tmpf As String = $"{Application.StartupPath}\LCX_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}_{ Guid.NewGuid().ToString()}.tmp"
        Dim sin As New IO.StreamReader(FileName)
        Dim soutx As New IO.StreamWriter(tmpf, False, New Text.UTF8Encoding(False))
        Dim result As ltfsindex
        Try
            While Not sin.EndOfStream
                Dim s As String = sin.ReadLine()
                s = s.Replace("<directory>", "<_directory><directory>")
                s = s.Replace("</directory>", "</directory></_directory>")
                s = s.Replace("<file>", "<_file><file>")
                s = s.Replace("</file>", "</file></_file>")
                s = s.Replace("%25", "%")
                If s.Length > 0 Then soutx.WriteLine(s)
            End While
            sin.Close()
            soutx.Close()
            Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(ltfsindex))
            Dim t As IO.StreamReader = New IO.StreamReader(tmpf)
            result = CType(reader.Deserialize(t), ltfsindex)
            t.Close()
            result.Standarize()
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
        IO.File.Delete(tmpf)
        Return result
    End Function
    Public Function Clone() As ltfsindex
        Dim tmpf As String = $"{Application.StartupPath}\LWI_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
        Me.SaveFile(tmpf)
        Dim result As ltfsindex = ltfsindex.FromSchFile(tmpf)
        My.Computer.FileSystem.DeleteFile(tmpf)
        Return result
    End Function
    Public Shared Sub WSort(d As List(Of directory), OnFileFound As Action(Of file), OnDirectoryFound As Action(Of directory))
        Dim q As List(Of directory) = d
        While q.Count > 0
            Dim q2 As New List(Of directory)
            For Each dq As directory In q
                If OnDirectoryFound IsNot Nothing Then OnDirectoryFound(dq)
                For Each fi As file In dq.contents._file
                    If OnFileFound IsNot Nothing Then OnFileFound(fi)
                Next
                For Each di As directory In dq.contents._directory
                    q2.Add(di)
                Next
            Next
            q = q2
        End While
    End Sub
End Class

<Serializable> Public Class ltfslabel
    Public Property creator As String = My.Application.Info.ProductName & " " & My.Application.Info.Version.ToString(3) & " - Windows - TapeUtils"
    Public Property formattime As String = Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
    Public Property volumeuuid As Guid
    <Serializable>
    Public Enum PartitionLabel
        a
        b
    End Enum
    Public Class PartitionLocation

        Public Property partition As PartitionLabel = PartitionLabel.a
    End Class
    Public Property location As New PartitionLocation
    <Serializable>
    Public Class PartitionInfo

        Public Property index As PartitionLabel = PartitionLabel.a
        Public Property data As PartitionLabel = PartitionLabel.b
    End Class
    Public Property partitions As New PartitionInfo
    Public Property blocksize As Integer = 524288
    Public Property compression As Boolean = True
    Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(ltfslabel))
        Dim tmpf As String = $"{Application.StartupPath}\LCG_{Now.ToString("yyyyMMdd_HHmmss")}.tmp"
        Dim ms As New IO.FileStream(tmpf, IO.FileMode.Create)
        Dim t As IO.TextWriter = New IO.StreamWriter(ms, New System.Text.UTF8Encoding(False))
        Dim ns As New Xml.Serialization.XmlSerializerNamespaces({New Xml.XmlQualifiedName("v", "2.4.0")})
        writer.Serialize(t, Me, ns)

        ms.Close()
        Dim soutp As New IO.StreamReader(tmpf)

        Dim sout As New System.Text.StringBuilder
        While Not soutp.EndOfStream
            Dim sline As String = soutp.ReadLine
            If sline.StartsWith("<?xml") Then
                sline = sline.Replace("utf-8", "UTF-8")
            End If
            If ReduceSize Then
                sline = sline.Replace("xmlns:v", "version")
                sline = sline.Replace("<_file />", "")
                sline = sline.Replace("<_directory />", "")
                sline = sline.Replace("<_file>", "")
                sline = sline.Replace("</_file>", "")
                sline = sline.Replace("<_directory>", "")
                sline = sline.Replace("</_directory>", "")
                sline = sline.TrimEnd(" ")
            End If
            If sline.Length > 0 Then sout.AppendLine(sline)
        End While
        soutp.Close()
        My.Computer.FileSystem.DeleteFile(tmpf)
        Return sout.ToString()
    End Function
    Public Shared Function FromXML(s As String) As ltfslabel
        Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(ltfslabel))
        Dim t As IO.TextReader = New IO.StringReader(s)
        Return CType(reader.Deserialize(t), ltfslabel)
    End Function
    Public Function Clone() As ltfslabel
        Return (FromXML(GetSerializedText(False)))
    End Function
End Class

Public Class Vol1Label
    Private _label_identifier As String = "VOL".PadRight(3)
    Public Property label_identifier As String
        Set(value As String)
            _label_identifier = value.PadRight(3).Substring(0, 3)
        End Set
        Get
            Return _label_identifier
        End Get
    End Property
    Public Property label_number As Char = "1"
    Private _volume_identifier As String = "".PadRight(6)
    Public Property volume_identifier As String
        Set(value As String)
            _volume_identifier = value.PadRight(6).Substring(0, 6)
        End Set
        Get
            Return _volume_identifier
        End Get
    End Property
    Public Property volume_accessibility As Char = "L"
    Private _implementation_identifier As String = "LTFS".PadRight(13)
    Public Property implementation_identifier As String
        Set(value As String)
            _implementation_identifier = value.PadRight(13).Substring(0, 13)
        End Set
        Get
            Return _implementation_identifier
        End Get
    End Property
    Private _owner_identifier As String = "".PadRight(14).Substring(0, 14)
    Public Property owner_identifier As String
        Set(value As String)
            _owner_identifier = value.PadRight(14)
        End Set
        Get
            Return _owner_identifier
        End Get
    End Property
    Public Property label_standard_version As Char = "4"

    Public Function GenerateRawData(Optional ByVal Barcode As String = "") As Byte()
        If Barcode <> "" Then volume_identifier = Barcode.Substring(0, Math.Min(6, Barcode.Length))
        Dim RawData(79) As Byte
        For i As Integer = 0 To 79
            RawData(i) = &H20
        Next
        For i As Integer = 0 To 2
            RawData(i + 0) = Asc(label_identifier(i))
        Next
        RawData(3) = Asc(label_number)
        For i As Integer = 0 To 5
            RawData(i + 4) = Asc(volume_identifier(i))
        Next
        RawData(10) = Asc(volume_accessibility)
        For i As Integer = 0 To 12
            RawData(i + 24) = Asc(implementation_identifier(i))
        Next
        For i As Integer = 0 To 13
            RawData(i + 37) = Asc(owner_identifier(i))
        Next
        RawData(79) = Asc(label_standard_version)
        Return RawData
    End Function
End Class
