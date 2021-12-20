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
    End Class
    <Serializable>
    Public Class contentsDef
        Public Property _file As New List(Of file)
        Public Property _directory As New List(Of directory)
    End Class
    Public Property _file As New List(Of file)
    Public Property _directory As New List(Of directory)
    Public Function GetSerializedText() As String
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(ltfsindex))
        Dim sb As New System.Text.StringBuilder()
        Dim t As IO.TextWriter = New IO.StringWriter(sb)
        writer.Serialize(t, Me)
        t.Close()
        Return sb.ToString
    End Function
    Public Shared Function FromXML(s As String) As ltfsindex
        Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(ltfsindex))
        Dim t As IO.TextReader = New IO.StringReader(s)
        Return CType(reader.Deserialize(t), ltfsindex)
    End Function
End Class
