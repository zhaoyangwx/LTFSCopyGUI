Imports System.Runtime.Serialization
Imports System.Xml.Serialization
''' <summary>   
''' 支持XML序列化的泛型   
''' </summary>   
''' <typeparam name="TKey"></typeparam>   
''' <typeparam name="TValue"></typeparam>   
<XmlRoot("SerializableDictionary")>
Public Class SerializableDictionary(Of TKey, TValue)
    Inherits Dictionary(Of TKey, TValue)
    Implements IXmlSerializable
#Region "构造函数"
    Public Sub New()
        MyBase.New

    End Sub

    Public Sub New(ByVal dictionary As IDictionary(Of TKey, TValue))
        MyBase.New(dictionary)

    End Sub

    Public Sub New(ByVal comparer As IEqualityComparer(Of TKey))
        MyBase.New(comparer)

    End Sub

    Public Sub New(ByVal capacity As Integer)
        MyBase.New(capacity)

    End Sub

    Public Sub New(ByVal capacity As Integer, ByVal comparer As IEqualityComparer(Of TKey))
        MyBase.New(capacity, comparer)

    End Sub

    Protected Sub New(ByVal info As SerializationInfo, ByVal context As StreamingContext)
        MyBase.New(info, context)

    End Sub
#End Region
#Region "IXmlSerializable Members   "

    Public Function GetSchema() As System.Xml.Schema.XmlSchema Implements IXmlSerializable.GetSchema
        Return Nothing
    End Function

    ''' <summary>   
    ''' 从对象的 XML 表示形式生成该对象  
    ''' </summary>   
    ''' <param name="reader"></param>   
    Public Sub ReadXml(ByVal reader As System.Xml.XmlReader) Implements IXmlSerializable.ReadXml
        Dim keySerializer As XmlSerializer = New XmlSerializer(GetType(TKey))
        Dim valueSerializer As XmlSerializer = New XmlSerializer(GetType(TValue))
        Dim wasEmpty As Boolean = reader.IsEmptyElement
        reader.Read()

        If wasEmpty Then
            Return
        End If


        While (reader.NodeType <> System.Xml.XmlNodeType.EndElement)
            reader.ReadStartElement("item")
            reader.ReadStartElement("key")
            Dim key As TKey = CType(keySerializer.Deserialize(reader), TKey)
            reader.ReadEndElement()
            reader.ReadStartElement("value")
            Dim value As TValue = CType(valueSerializer.Deserialize(reader), TValue)
            reader.ReadEndElement()
            Me.Add(key, value)
            reader.ReadEndElement()
            reader.MoveToContent()

        End While

        reader.ReadEndElement()
    End Sub

    ''' <summary>   
    ''' 将对象转换为其 XML 表示形式 
    ''' </summary>   
    ''' <param name="writer"></param>   
    Public Sub WriteXml(ByVal writer As System.Xml.XmlWriter) Implements IXmlSerializable.WriteXml
        Dim keySerializer As XmlSerializer = New XmlSerializer(GetType(TKey))
        Dim valueSerializer As XmlSerializer = New XmlSerializer(GetType(TValue))
        For Each key As TKey In Me.Keys
            writer.WriteStartElement("item")
            writer.WriteStartElement("key")
            keySerializer.Serialize(writer, key)
            writer.WriteEndElement()
            writer.WriteStartElement("value")
            Dim value As TValue = Me(key)
            valueSerializer.Serialize(writer, value)
            writer.WriteEndElement()
            writer.WriteEndElement()
        Next
    End Sub
#End Region
End Class