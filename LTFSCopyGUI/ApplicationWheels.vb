Imports System.ComponentModel
Imports System.Runtime.Serialization
Imports System.Xml.Serialization
Public Class SerializationHelper
    Public Shared Function GetSerializeString(ByVal c As Object) As String
        Dim s As New IO.MemoryStream
        Dim b As Formatters.Binary.BinaryFormatter = New Formatters.Binary.BinaryFormatter
        b.Serialize(s, c)
        Return Convert.ToBase64String(s.ToArray())
    End Function
    Public Shared Function FromSerializeString(ByVal d As String) As Object
        Dim s As New IO.MemoryStream(Convert.FromBase64String(d))
        Dim b As Formatters.Binary.BinaryFormatter = New Formatters.Binary.BinaryFormatter
        Return b.Deserialize(s)
    End Function
End Class
Public Class LocalizedDescriptionAttribute
    Inherits DescriptionAttribute
    Public Shared Function Localize(key As String) As String
        Return My.Resources.ResourceManager.GetString(key)
    End Function
    Public Sub New(ByVal key As String)
        MyBase.New(LocalizedDescriptionAttribute.Localize(key))
    End Sub
End Class
<Serializable>
Public Class SettingImportExport
    Public Property MySettings As SerializableDictionary(Of String, String)
        Get
            Dim result As New SerializableDictionary(Of String, String)
            For Each setting As System.Configuration.SettingsPropertyValue In My.Settings.PropertyValues
                result.Add(setting.Name, SerializationHelper.GetSerializeString(setting.PropertyValue))
            Next
            Return result
        End Get
        Set(value As SerializableDictionary(Of String, String))
            For Each setting As String In value.Keys

                My.Settings.PropertyValues.Item(setting).PropertyValue = SerializationHelper.FromSerializeString(value(setting))
            Next
            My.Settings.Save()
        End Set
    End Property
    Public Shared Function GetSerializedText() As String
        Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(SettingImportExport))
        Dim sb As New Text.StringBuilder
        Dim t As New IO.StringWriter(sb)
        writer.Serialize(t, New SettingImportExport())
        Return sb.ToString()
    End Function
    Public Shared Sub LoadFromFile(FileName As String)
        Dim s As SettingImportExport = SettingImportExport.FromXML(IO.File.ReadAllText(FileName))
    End Sub
    Public Shared Function FromXML(s As String) As SettingImportExport
        Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(SettingImportExport))
        Dim t As IO.TextReader = New IO.StringReader(s)
        Return CType(reader.Deserialize(t), SettingImportExport)
    End Function
End Class
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