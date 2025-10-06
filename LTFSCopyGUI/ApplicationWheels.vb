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
<XmlRoot("SerializableDictionary"),
    Serializable>
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
<TypeConverter(GetType(ExpandableObjectConverter))>
Public Class NamedObject
    Public Property Name As String
    <Category("Edit")>
    Public ReadOnly Property Type As System.Type
        Get
            Return Value.GetType()
        End Get
    End Property
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Property Value As Object
        Get
            Return FieldInfo.GetValue(Instance)
        End Get
        Set(value As Object)
            FieldInfo.SetValue(Instance, value)
        End Set
    End Property

    <Category("Edit")>
    Public Property AsString As String
        Get
            Dim result As Object = Value
            If TypeOf result Is String Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As String)
            If TypeOf Me.Value IsNot String Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsBoolean As Boolean
        Get
            Dim result As Object = Value
            If TypeOf result Is Boolean Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As Boolean)
            If TypeOf Me.Value IsNot Boolean Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsByte As Byte
        Get
            Dim result As Object = Value
            If TypeOf result Is Byte Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As Byte)
            If TypeOf Me.Value IsNot Byte Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsShort As Short
        Get
            Dim result As Object = Value
            If TypeOf result Is Short Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As Short)
            If TypeOf Me.Value IsNot Short Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsInteger As Integer
        Get
            Dim result As Object = Value
            If TypeOf result Is Integer Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As Integer)
            If TypeOf Me.Value IsNot Integer Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsLong As Long
        Get
            Dim result As Object = Value
            If TypeOf result Is Long Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As Long)
            If TypeOf Me.Value IsNot Long Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsUByte As SByte
        Get
            Dim result As Object = Value
            If TypeOf result Is SByte Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As SByte)
            If TypeOf Me.Value IsNot SByte Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsUShort As UShort
        Get
            Dim result As Object = Value
            If TypeOf result Is UShort Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As UShort)
            If TypeOf Me.Value IsNot UShort Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsUInteger As UInteger
        Get
            Dim result As Object = Value
            If TypeOf result Is UInteger Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As UInteger)
            If TypeOf Me.Value IsNot UInteger Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsULong As ULong
        Get
            Dim result As Object = Value
            If TypeOf result Is ULong Then
                Return result
            Else
                Return Nothing
            End If
        End Get
        Set(value As ULong)
            If TypeOf Me.Value IsNot ULong Then Exit Property
            Me.Value = value
        End Set
    End Property
    <Category("Edit")>
    Public Property AsCollection As List(Of Object)
        Get
            Dim result As New List(Of Object)
            For Each o As Object In Value
                result.Add(o)
            Next
            Return result
        End Get
        Set(value As List(Of Object))
            Me.Value = value
        End Set
    End Property
    Public Property Instance As Object
    Public Property FieldInfo As Reflection.FieldInfo
    Public Sub New(Name As String, ByVal f As Reflection.FieldInfo, ByVal Instance As Object)
        Me.Name = Name
        FieldInfo = f
        Me.Instance = Instance
    End Sub
End Class

Public Class ListTypeDescriptor(Of TColl As IList, TItem)
    Inherits ExpandableObjectConverter

    Public Overrides Function GetProperties(context As ITypeDescriptorContext, value As Object, attributes() As Attribute) As PropertyDescriptorCollection
        Dim coll = DirectCast(value, TColl)
        Dim props(coll.Count - 1) As PropertyDescriptor
        Dim digits As Integer = (coll.Count - 1).ToString().Length
        For i = 0 To coll.Count - 1
            Dim Name As String = ""
            If i < coll.Count AndAlso coll(i) IsNot Nothing Then
                Dim obj As Object = coll(i)
                Dim myType As Type = obj.GetType()
                Dim myprops As New List(Of Reflection.PropertyInfo)(myType.GetProperties)
                'If myprops.Count > 0 Then Name = $": {myprops(0).GetValue(obj)}"
            End If
            props(i) = New ListPropertyDescriptor(Of TColl, TItem)($"Item{CStr(i).PadLeft(digits, "0")}{Name}")
        Next
        Return New PropertyDescriptorCollection(props)
    End Function
    Public Overrides Function GetPropertiesSupported(context As ITypeDescriptorContext) As Boolean
        Return True
    End Function
End Class

Public Class ListPropertyDescriptor(Of TColl, TItem)
    Inherits PropertyDescriptor

    Private _index As Integer = 0

    Public Sub New(name As String)
        MyBase.New(name, Nothing)
        Dim indexStr = System.Text.RegularExpressions.Regex.Match(name, "\d+$").Value
        _index = CInt(indexStr)
    End Sub

    Public Overrides Function CanResetValue(component As Object) As Boolean
        Return False
    End Function

    Public Overrides ReadOnly Property ComponentType As Type
        Get
            Return GetType(TColl)
        End Get
    End Property

    Public Overrides Function GetValue(component As Object) As Object
        Dim coll = DirectCast(component, IList)
        Return coll(_index)
    End Function

    Public Overrides ReadOnly Property IsReadOnly As Boolean
        Get
            Return True
        End Get
    End Property

    Public Overrides ReadOnly Property PropertyType As Type
        Get
            Return GetType(TItem)
        End Get
    End Property

    Public Overrides Sub ResetValue(component As Object)

    End Sub

    Public Overrides Sub SetValue(component As Object, value As Object)

    End Sub

    Public Overrides Function ShouldSerializeValue(component As Object) As Boolean
        Return False
    End Function

End Class