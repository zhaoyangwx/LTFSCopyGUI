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
        Public Property tag As String
        <Xml.Serialization.XmlIgnore> Public fullpath As String

        <Xml.Serialization.XmlIgnore> Public Selected As Boolean = True
        <Xml.Serialization.XmlIgnore> Public WrittenBytes As Long = 0
        <Serializable>
        Public Class extent
            Public Property fileoffset As Long
            Public Property partition As PartitionLabel
            Public Property startblock As Long
            Public Property byteoffset As Long
            Public Property bytecount As Long
        End Class
        Public Property extentinfo As New List(Of extent)
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

        <Xml.Serialization.XmlIgnore> Public fullpath As String
        <Xml.Serialization.XmlIgnore> Public Selected As Boolean = True
    End Class
    <Serializable>
    Public Class contentsDef
        Public Property _file As New List(Of file)
        Public Property _directory As New List(Of directory)
        <Xml.Serialization.XmlIgnore> Public UnwrittenFiles As New List(Of file)
    End Class
    Public Property _file As New List(Of file)
    Public Property _directory As New List(Of directory)
    Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(ltfsindex))
        Dim tmpf As String = My.Computer.FileSystem.CurrentDirectory & "\" & Now.ToString("LCG_yyyyMMdd_HHmmss.tmp")
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
                sline = sline.TrimEnd(" ").TrimStart(" ")
            End If
            If sline.Length > 0 Then sout.AppendLine(sline)
        End While
        soutp.Close()
        My.Computer.FileSystem.DeleteFile(tmpf)
        Return sout.ToString()
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
        Return CType(reader.Deserialize(t), ltfsindex)
    End Function
    Public Function Clone() As ltfsindex
        Return (FromXML(GetSerializedText(False)))
    End Function
End Class

<Serializable> Public Class ltfslabel
    Public Property creator As String = My.Application.Info.ProductName & " " & My.Application.Info.Version.ToString(3) & " - Windows - TapeUtils"
    Public Property formattime As String = Now.ToUniversalTime().ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
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
        Dim tmpf As String = My.Computer.FileSystem.CurrentDirectory & "\" & Now.ToString("LCG_yyyyMMdd_HHmmss.tmp")
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
