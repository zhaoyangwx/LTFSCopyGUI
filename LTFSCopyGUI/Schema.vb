Imports System.ComponentModel

<Serializable>
<TypeConverter(GetType(ExpandableObjectConverter))>
Public Class ltfsindex
    'Public Property version As String
    <Category("LTFSIndex")>
    Public Property creator As String = My.Application.Info.ProductName & " " & My.Application.Info.Version.ToString(3) & " - Windows - TapeUtils"
    <Category("LTFSIndex")>
    Public Property volumeuuid As Guid
    <Category("LTFSIndex")>
    Public Property generationnumber As ULong
    <Category("LTFSIndex")>
    Public Property updatetime As String
    Public Enum PartitionLabel
        a
        b
    End Enum
    <Serializable>
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class LocationDef

        Public Property partition As PartitionLabel = PartitionLabel.a
        Public Property startblock As ULong
    End Class

    <Category("LTFSIndex")>
    Public Property location As New LocationDef
    <Category("LTFSIndex")>
    Public Property previousgenerationlocation As New LocationDef
    <Category("LTFSIndex")>
    Public Property allowpolicyupdate As Boolean
    <Serializable>
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class policy
        Public Structure indexpartitioncriteria
            Public Property size As Long
        End Structure
    End Class
    <Category("LTFSIndex")>
    Public Property dataplacementpolicy As policy
    Public Enum volumelockstateValue
        unlocked
        locked
        permlocked
    End Enum
    <Category("LTFSIndex")>
    Public Property volumelockstate As volumelockstateValue = volumelockstateValue.unlocked
    <Category("LTFSIndex")>
    Public Property highestfileuid As Long
    <Serializable>
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class file
        <Category("LTFSIndex")>
        Public Property name As String
        <Category("LTFSIndex")>
        Public Property length As Long
        <Category("LTFSIndex")>
        Public Property [readonly] As Boolean = False
        <Category("LTFSIndex")>
        Public Property openforwrite As Boolean = True
        <Category("LTFSIndex")>
        Public Property creationtime As String
        <Category("LTFSIndex")>
        Public Property changetime As String
        <Category("LTFSIndex")>
        Public Property modifytime As String
        <Category("LTFSIndex")>
        Public Property accesstime As String
        <Category("LTFSIndex")>
        Public Property backuptime As String
        <Category("LTFSIndex")>
        Public Property fileuid As Long
        <Category("Deprecated")>
        <Xml.Serialization.XmlIgnore>
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
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property tag As String
        <TypeConverter(GetType(ExpandableObjectConverter))>
        Public Class refFile
            Public FileName As String
        End Class
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property fullpath As String
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property Selected As Boolean = True
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property WrittenBytes As Long = 0
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property TempObj As Object
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property SHA1ForeColor As Color = Color.Black
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property MD5ForeColor As Color = Color.Black
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property BLAKE3ForeColor As Color = Color.Black
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property XxHash3ForeColor As Color = Color.Black
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property XxHash128ForeColor As Color = Color.Black
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore>
        Public Property ItemForeColor As Color = Color.Black
        <Serializable>
        <TypeConverter(GetType(ExpandableObjectConverter))>
        Public Class xattr
            <Category("LTFSIndex")>
            Public Property key As String
            <Category("LTFSIndex")>
            Public Property value As String
            <TypeConverter(GetType(ExpandableObjectConverter))>
            <Serializable>
            Public Class HashType
                Public Shared ReadOnly Property CRC32 As String = "ltfs.hash.crc32sum"
                Public Shared ReadOnly Property MD5 As String = "ltfs.hash.md5sum"
                Public Shared ReadOnly Property SHA1 As String = "ltfs.hash.sha1sum"
                Public Shared ReadOnly Property SHA256 As String = "ltfs.hash.sha256sum"
                Public Shared ReadOnly Property SHA512 As String = "ltfs.hash.sha512sum"
                Public Shared ReadOnly Property BLAKE3 As String = "ltfs.hash.blake3sum"
                Public Shared ReadOnly Property XxHash3 As String = "ltfs.hash.xxhash3sum"
                Public Shared ReadOnly Property XxHash128 As String = "ltfs.hash.xxhash128sum"
                Public Enum Available
                    SHA1
                    MD5
                    BLAKE3
                    XxHash3
                    XxHash128
                End Enum
            End Class
            Public Shared Function FromXMLList(s As String) As List(Of xattr)
                Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(List(Of xattr)))
                Dim t As IO.TextReader = New IO.StringReader(s)
                Return CType(reader.Deserialize(t), List(Of xattr))
            End Function
        End Class
        <Category("LTFSIndex")>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of xattr), xattr)))>
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

        Public Sub SetXattr(key As String, value As String, Optional ByVal IgnoreBlank As Boolean = False)
            If IgnoreBlank AndAlso value.Length = 0 Then Exit Sub
            For Each x As xattr In extendedattributes
                If x.key.ToLower = key.ToLower Then
                    x.value = value
                    Exit Sub
                End If
            Next
            extendedattributes.Add(New xattr With {.key = key, .value = value})
        End Sub
        Private _symlink As String = Nothing
        <Category("LTFSIndex")>
        Public Property symlink As String
            Get
                Return _symlink
            End Get
            Set(value As String)
                _symlink = value
                'If value IsNot Nothing Then extentinfo = Nothing
            End Set
        End Property

        <Serializable>
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Category("LTFSIndex")>
        Public Class extent
            <Category("LTFSIndex")>
            Public Property fileoffset As Long
            <Category("LTFSIndex")>
            Public Property partition As PartitionLabel
            <Category("LTFSIndex")>
            Public Property startblock As Long
            <Category("LTFSIndex")>
            Public Property byteoffset As Long
            <Category("LTFSIndex")>
            Public Property bytecount As Long
            <Xml.Serialization.XmlIgnore>
            <Category("Internal")>
            Public Property TempInfo As Object
        End Class

        <Category("LTFSIndex")>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of extent), extent)))>
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
        Public Function GetCopy(fileuid1 As Long) As ltfsindex.file
            Dim result As New file With {.accesstime = accesstime, .backuptime = backuptime,
                .changetime = changetime, .creationtime = creationtime,
                .fileuid = fileuid1,
                .fullpath = fullpath, .length = length,
                .modifytime = modifytime, .name = name, .openforwrite = openforwrite, .readonly = [readonly],
                .tag = tag}
            result.extendedattributes = New List(Of xattr)
            For Each x As xattr In extendedattributes
                result.extendedattributes.Add(New xattr With {.key = x.key, .value = x.value})
            Next
            result.extentinfo = New List(Of extent)
            For Each xt As extent In extentinfo
                result.extentinfo.Add(New extent With {.bytecount = xt.bytecount, .byteoffset = xt.byteoffset, .fileoffset = xt.fileoffset, .partition = xt.partition, .startblock = xt.startblock})
            Next
            Return result
        End Function
    End Class
    <Serializable>
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class directory
        <Category("LTFSIndex")>
        Public Property name As String
        <Category("LTFSIndex")>
        Public Property [readonly] As Boolean = False
        <Category("LTFSIndex")>
        Public Property creationtime As String
        <Category("LTFSIndex")>
        Public Property changetime As String
        <Category("LTFSIndex")>
        Public Property modifytime As String
        <Category("LTFSIndex")>
        Public Property accesstime As String
        <Category("LTFSIndex")>
        Public Property backuptime As String
        <Category("LTFSIndex")>
        Public Property fileuid As Long
        <Xml.Serialization.XmlIgnore>
        <Category("Internal")>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of file), file)))>
        Public Property UnwrittenFiles As New List(Of file)
        <Xml.Serialization.XmlIgnore>
        <Category("Internal")>
        Public Property LastUnwrittenFilesCount As Integer
        <Category("LTFSIndex")>
        Public Property contents As New contentsDef
        '<Xml.Serialization.XmlIgnore>
        '<Category("Internal")>
        'Public ReadOnly Property Files As List(Of file)
        '    Get
        '        Return contents._file
        '    End Get
        'End Property
        '<Xml.Serialization.XmlIgnore>
        '<Category("Internal")>
        'Public ReadOnly Property Directories As List(Of directory)
        '    Get
        '        Return contents._directory
        '    End Get
        'End Property
        <Xml.Serialization.XmlIgnore>
        <Category("Internal")>
        Public Property tag As String

        Private _TotalFiles, _TotalDirectories, _TotalFilesUnwritten As Long
        <Xml.Serialization.XmlIgnore>
        <Category("Internal")>
        Public ReadOnly Property TotalFiles
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
        <Xml.Serialization.XmlIgnore>
        <Category("Internal")>
        Public ReadOnly Property TotalFilesUnwritten
            Get
                If _TotalDirectories = 0 AndAlso contents._directory IsNot Nothing AndAlso contents._directory.Count > 0 Then
                    RefreshCount()
                End If
                If _TotalFiles = 0 AndAlso contents._file IsNot Nothing AndAlso contents._file.Count > 0 Then
                    RefreshCount()
                End If
                If _TotalFilesUnwritten = 0 AndAlso UnwrittenFiles IsNot Nothing AndAlso UnwrittenFiles.Count > 0 Then
                    RefreshCount()
                End If
                Return _TotalFilesUnwritten
            End Get
        End Property
        <Xml.Serialization.XmlIgnore>
        <Category("Internal")>
        Public ReadOnly Property TotalDirectories
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
                If UnwrittenFiles IsNot Nothing Then
                    _TotalFilesUnwritten = UnwrittenFiles.Count
                Else
                    _TotalFilesUnwritten = 0
                End If
            Else
                If contents._file IsNot Nothing Then
                    _TotalFiles = contents._file.Count
                End If
                If UnwrittenFiles IsNot Nothing Then
                    _TotalFilesUnwritten = UnwrittenFiles.Count
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


        <Xml.Serialization.XmlIgnore>
        <Category("Internal")>
        Public Property fullpath As String
        <Xml.Serialization.XmlIgnore>
        <Category("Internal")>
        Public Property Selected As Boolean = True

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
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class contentsDef
        <Category("LTFSIndex")>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of file), file)))>
        Public Property _file As New List(Of file)
        <Category("LTFSIndex")>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of directory), directory)))>
        Public Property _directory As New List(Of directory)
    End Class
    <Category("LTFSIndex")>
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of file), file)))>
    Public Property _file As New List(Of file)
    <Category("LTFSIndex")>
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of directory), directory)))>
    Public Property _directory As New List(Of directory)
    <Xml.Serialization.XmlIgnore>
    <Category("Internal")>
    Public Shared Property Searializing As Boolean = False

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
        Dim sline As String = soutp.ReadLine()
        If sline.StartsWith("<?xml") Then
            sline = sline.Replace("utf-8", "UTF-8")
        End If
        If ReduceSize Then sline = sline.Replace("xmlns:v", "version")
        If sline.Length > 0 Then sout.AppendLine(sline)
        While Not soutp.EndOfStream
            sline = soutp.ReadLine()
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
        IO.File.Delete(tmpf)
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
        Dim sline As String = soutp.ReadLine()
        If sline.StartsWith("<?xml") Then
            sline = sline.Replace("utf-8", "UTF-8")
        End If
        sline = sline.Replace("xmlns:v", "version")
        If sline.Length > 0 Then sout.WriteLine(sline)
        While Not soutp.EndOfStream
            sline = soutp.ReadLine()
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
        IO.File.Delete(tmpf)
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
            MessageBox.Show(New Form With {.TopMost = True}, ex.ToString)
        End Try
        IO.File.Delete(tmpf)
        Return result
    End Function
    Public Function Clone() As ltfsindex
        Dim tmpf As String = $"{Application.StartupPath}\LWI_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
        Me.SaveFile(tmpf)
        Dim result As ltfsindex = ltfsindex.FromSchFile(tmpf)
        IO.File.Delete(tmpf)
        Return result
    End Function
    Public Shared Sub WSort(d As List(Of directory), OnFileFound As Action(Of file), OnDirectoryFound As Action(Of directory), Optional ByRef StopFlag As Boolean = False)
        Dim q As List(Of directory) = d
        While (Not StopFlag) AndAlso q.Count > 0
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

<Serializable>
<TypeConverter(GetType(ExpandableObjectConverter))>
<Category("LTFSIndex")>
Public Class ltfslabel
    <Category("LTFSIndex")>
    Public Property creator As String = My.Application.Info.ProductName & " " & My.Application.Info.Version.ToString(3) & " - Windows - TapeUtils"
    <Category("LTFSIndex")>
    Public Property formattime As String = Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
    <Category("LTFSIndex")>
    Public Property volumeuuid As Guid
    <Serializable>
    Public Enum PartitionLabel
        a
        b
    End Enum
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class PartitionLocation

        Public Property partition As PartitionLabel = PartitionLabel.a
    End Class
    <Category("LTFSIndex")>
    Public Property location As New PartitionLocation
    <Serializable>
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class PartitionInfo

        Public Property index As PartitionLabel = PartitionLabel.a
        Public Property data As PartitionLabel = PartitionLabel.b
    End Class
    <Category("LTFSIndex")>
    Public Property partitions As New PartitionInfo
    <Category("LTFSIndex")>
    Public Property blocksize As Integer = 524288
    <Category("LTFSIndex")>
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
        Dim sline As String = soutp.ReadLine
        If sline.StartsWith("<?xml") Then
            sline = sline.Replace("utf-8", "UTF-8")
        End If
        If ReduceSize Then
            sline = sline.Replace("xmlns:v", "version")
        End If
        If sline.Length > 0 Then sout.AppendLine(sline)
        While Not soutp.EndOfStream
            sline = soutp.ReadLine
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
        IO.File.Delete(tmpf)
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

<TypeConverter(GetType(ExpandableObjectConverter))>
Public Class Vol1Label
    Private _label_identifier As String = "VOL".PadRight(3)
    <Category("LTFSIndex")>
    Public Property label_identifier As String
        Set(value As String)
            _label_identifier = value.PadRight(3).Substring(0, 3)
        End Set
        Get
            Return _label_identifier
        End Get
    End Property
    <Category("LTFSIndex")>
    Public Property label_number As Char = "1"
    Private _volume_identifier As String = "".PadRight(6)
    <Category("LTFSIndex")>
    Public Property volume_identifier As String
        Set(value As String)
            _volume_identifier = value.PadRight(6).Substring(0, 6)
        End Set
        Get
            Return _volume_identifier
        End Get
    End Property
    <Category("LTFSIndex")>
    Public Property volume_accessibility As Char = "L"
    Private _implementation_identifier As String = "LTFS".PadRight(13)
    <Category("LTFSIndex")>
    Public Property implementation_identifier As String
        Set(value As String)
            _implementation_identifier = value.PadRight(13).Substring(0, 13)
        End Set
        Get
            Return _implementation_identifier
        End Get
    End Property
    Private _owner_identifier As String = "".PadRight(14).Substring(0, 14)
    <Category("LTFSIndex")>
    Public Property owner_identifier As String
        Set(value As String)
            _owner_identifier = value.PadRight(14)
        End Set
        Get
            Return _owner_identifier
        End Get
    End Property
    <Category("LTFSIndex")>
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
