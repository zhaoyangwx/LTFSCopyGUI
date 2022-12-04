<Serializable>
Public Class ltfsindex
    Public Property version As String
    Public Property creator As String
    Public Property volumeuuid As Guid
    Public Property generationnumber As Integer
    Public Property updatetime As String
    <Serializable>
    Public Class Partition
        Public Enum PartitionLabel
            a
            b
        End Enum

        Public Property partition As PartitionLabel = PartitionLabel.a
        Public Property startblock As Long
    End Class
    Public Property location As New Partition
    Public Property previousgenerationlocation As New Partition
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

        <Serializable>
        Public Class extent
            Public Property fileoffset As Long
            Public Property partition As Partition.PartitionLabel
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
    End Class
    Public Property _file As New List(Of file)
    Public Property _directory As New List(Of directory)
    Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(ltfsindex))
        Dim tmpf As String = My.Computer.FileSystem.CurrentDirectory & "\" & Now.ToString("LCG_yyyyMMdd_hhmmss.tmp")
        Dim ms As New IO.FileStream(tmpf, IO.FileMode.Create)
        Dim t As IO.TextWriter = New IO.StreamWriter(ms, New System.Text.UTF8Encoding(False))
        Dim ns As New Xml.Serialization.XmlSerializerNamespaces({New Xml.XmlQualifiedName("v", "LTFSCopyGUI 1.0")})
        writer.Serialize(t, Me, ns)
        ms.Close()
        Dim soutp As New IO.StreamReader(tmpf)

        Dim sout As New System.Text.StringBuilder
        While Not soutp.EndOfStream
            Dim sline As String = soutp.ReadLine
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
