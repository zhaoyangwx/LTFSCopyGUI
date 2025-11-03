Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.Serialization
Imports System.Text
Imports System.Xml.Serialization
Imports Microsoft.Extensions.FileSystemGlobbing
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

Public Class GlobHelper
    Public Property schema As ltfsindex
    Public Property OnStopFlagInquiry As Func(Of Boolean)
    Public ReadOnly Property StopFlag As Boolean
        Get
            If OnStopFlagInquiry IsNot Nothing Then
                Return OnStopFlagInquiry.Invoke()
            Else
                Return False
            End If
        End Get
    End Property
    Public Function NormalizePath(p As String) As String
        If String.IsNullOrWhiteSpace(p) Then Return p
        Dim s = p.Replace("/"c, "\"c)
        If s.Length > 3 AndAlso s.EndsWith("\", StringComparison.Ordinal) Then
            s = s.TrimEnd("\"c)
        End If
        Return s
    End Function

    Public Function PathIsUnder(p As String, root As String) As Boolean
        If String.IsNullOrEmpty(p) OrElse String.IsNullOrEmpty(root) Then Return False
        Dim pn = NormalizePath(p)
        Dim rn = NormalizePath(root)
        If pn.Equals(rn, StringComparison.OrdinalIgnoreCase) Then Return True
        If pn.Length <= rn.Length Then Return False
        If pn.StartsWith(rn, StringComparison.OrdinalIgnoreCase) Then
            Return pn(rn.Length) = "\"c
        End If
        Return False
    End Function

    Public Function FindBestRoot(path As String, candidateRoots As List(Of String)) As String
        If candidateRoots Is Nothing OrElse candidateRoots.Count = 0 Then Return Nothing
        Dim best As String = Nothing
        Dim bestLen As Integer = -1
        For Each r In candidateRoots
            If PathIsUnder(path, r) Then
                Dim l = NormalizePath(r).Length
                If l > bestLen Then
                    best = r
                    bestLen = l
                End If
            End If
        Next
        Return best
    End Function

    Public Function GetRelativePathWin(basePath As String, fullPath As String) As String
        If String.IsNullOrWhiteSpace(basePath) OrElse String.IsNullOrWhiteSpace(fullPath) Then Return Nothing
        Dim b As String
        Dim f As String
        Try
            b = NormalizePath(System.IO.Path.GetFullPath(basePath))
            f = NormalizePath(System.IO.Path.GetFullPath(fullPath))
        Catch
            Return Nothing
        End Try
        If b.Equals(f, StringComparison.OrdinalIgnoreCase) Then Return String.Empty
        Dim bSeg = b.Split("\"c)
        Dim fSeg = f.Split("\"c)
        Dim i As Integer = 0
        Dim maxI = Math.Min(bSeg.Length, fSeg.Length)
        While i < maxI AndAlso bSeg(i).Equals(fSeg(i), StringComparison.OrdinalIgnoreCase)
            i += 1
        End While
        If i < bSeg.Length AndAlso Not b.EndsWith(":\", StringComparison.Ordinal) AndAlso Not b.StartsWith("\\") Then
            Return If(PathIsUnder(f, b), String.Join("\", fSeg, i, fSeg.Length - i), Nothing)
        End If
        Dim rel = String.Join("\", fSeg, i, fSeg.Length - i)
        Return rel
    End Function

    Public Function EnsureDirectoryChain(rootNode As ltfsindex.directory, baseAbs As String, relPath As String) As ltfsindex.directory
        Dim node = rootNode
        Dim currentAbs = NormalizePath(baseAbs)
        If String.IsNullOrEmpty(relPath) Then Return node

        Dim parts = relPath.Split(New Char() {"\"c, "/"c}, StringSplitOptions.RemoveEmptyEntries)
        For Each directoryName In parts
            If StopFlag Then Exit For
            currentAbs = If(currentAbs.EndsWith("\"), currentAbs & directoryName, currentAbs & "\" & directoryName)
            node = GetOrCreateSubdir(node, directoryName, currentAbs)
        Next
        Return node
    End Function

    Private Function GetOrCreateSubdir(parent As ltfsindex.directory, childName As String, absPath As String) As ltfsindex.directory
        Dim found As ltfsindex.directory = Nothing

        SyncLock parent.contents._directory
            For Each fe As ltfsindex.directory In parent.contents._directory
                If fe.name.Equals(childName, StringComparison.OrdinalIgnoreCase) Then
                    found = fe
                    Exit For
                End If
            Next

            If found Is Nothing Then
                Dim info As IO.DirectoryInfo = Nothing
                Try : info = New IO.DirectoryInfo(absPath)
                Catch : info = Nothing
                End Try

                Dim ct = If(info IsNot Nothing, info.CreationTimeUtc, Now.ToUniversalTime)
                Dim at = If(info IsNot Nothing, info.LastAccessTimeUtc, Now.ToUniversalTime)
                Dim mt = If(info IsNot Nothing, info.LastWriteTimeUtc, Now.ToUniversalTime)
                Dim tsFmt As Func(Of Date, String) =
                    Function(dt) dt.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")

                found = New ltfsindex.directory With {
                    .name = childName,
                    .creationtime = tsFmt(ct),
                    .fileuid = schema.highestfileuid + 1,
                    .accesstime = tsFmt(at),
                    .modifytime = tsFmt(mt),
                    .changetime = tsFmt(mt),
                    .backuptime = tsFmt(Now.ToUniversalTime),
                    .readonly = False
                }
                parent.contents._directory.Add(found)
                Threading.Interlocked.Increment(schema.highestfileuid)
            End If
        End SyncLock

        Return found
    End Function

    Public Class AddPlan
        Public ReadOnly Dirs As New List(Of String)()
        Public ReadOnly Files As New List(Of String)()
    End Class

    Public Class GlobCollector
        Public Shared Function BuildMatcherFromMatchFile(patterns As IEnumerable(Of String),
                                                  Optional caseSensitive As Boolean = False) As Matcher
            If patterns Is Nothing Then
                Throw New ArgumentNullException(NameOf(patterns))
            End If

            Dim comparison = If(caseSensitive,
                               StringComparison.Ordinal,
                               StringComparison.OrdinalIgnoreCase)
            Dim matcher = New Matcher(comparison)

            Dim lineNumber = 0
            Dim hasIncludeRules = False

            For Each rawLine As String In patterns
                lineNumber += 1

                ' 处理 null 输入
                If rawLine Is Nothing Then Continue For

                ' 去除首尾空白字符
                Dim line = rawLine.Trim()

                ' 跳过空行
                If line.Length = 0 Then Continue For

                ' 跳过注释行
                If line.StartsWith("#") Then Continue For

                Try
                    Dim isExclude = False
                    Dim pattern = line

                    ' 处理转义字符
                    If line.StartsWith("\") AndAlso line.Length > 1 Then
                        Dim nextChar = line(1)

                        ' 转义特殊字符: \!, \#, \\
                        If nextChar = "!"c OrElse nextChar = "#"c OrElse nextChar = "\"c Then
                            pattern = line.Substring(1)

                            ' 如果是其他字符,保持原样(\ 不是转义符)
                        End If

                    ElseIf line.StartsWith("!") Then
                        ' 排除规则
                        isExclude = True
                        pattern = line.Substring(1).TrimStart()

                        ' 检查排除规则是否为空
                        If pattern.Length = 0 Then
                            ' 忽略空的排除规则
                            Continue For
                        End If
                    End If

                    ' 验证模式有效性
                    If Not IsValidPattern(pattern) Then
                        ' 可选:记录警告或跳过无效模式
                        Continue For
                    End If

                    ' 添加到 Matcher
                    If isExclude Then
                        matcher.AddExclude(pattern)
                    Else
                        matcher.AddInclude(pattern)
                        hasIncludeRules = True
                    End If

                Catch ex As Exception
                    Continue For
                End Try
            Next

            ' 如果没有任何包含规则,添加默认的 "匹配所有" 规则
            ' 这样单独的排除规则才有意义
            If Not hasIncludeRules Then
                matcher.AddInclude("**/*")
            End If

            Return matcher
        End Function

        ''' <summary>
        ''' 验证 glob 模式是否有效
        ''' </summary>
        Private Shared Function IsValidPattern(pattern As String) As Boolean
            If String.IsNullOrEmpty(pattern) Then Return False

            ' 检查是否包含非法字符(Windows路径)
            Dim invalidChars = {"<"c, ">"c, "|"c, """"c, vbNullChar}
            For Each ch In invalidChars
                If pattern.Contains(ch) Then Return False
            Next

            ' 检查是否有不匹配的方括号
            Dim openBrackets = pattern.Count(Function(c) c = "["c)
            Dim closeBrackets = pattern.Count(Function(c) c = "]"c)
            If openBrackets <> closeBrackets Then Return False

            ' 检查是否有连续的星号(超过2个)
            If pattern.Contains("***") Then Return False

            Return True
        End Function

        ''' <summary>
        ''' 从文件读取并构建 Matcher
        ''' </summary>
        ''' <param name="filePath">规则文件路径</param>
        ''' <param name="encoding">文件编码(默认 UTF-8)</param>
        ''' <param name="caseSensitive">是否区分大小写</param>
        Public Shared Function BuildMatcherFromFile(filePath As String,
                                             Optional encoding As Encoding = Nothing,
                                             Optional caseSensitive As Boolean = False) As Matcher
            If String.IsNullOrEmpty(filePath) Then
                Throw New ArgumentNullException(NameOf(filePath))
            End If

            If Not File.Exists(filePath) Then
                Throw New FileNotFoundException("Pattern file not found", filePath)
            End If

            Dim enc = If(encoding, Encoding.UTF8)
            Dim lines = File.ReadAllLines(filePath, enc)

            Return BuildMatcherFromMatchFile(lines, caseSensitive)
        End Function

        Public Shared Function BuildMatcherFromString(patternText As String,
                                               Optional caseSensitive As Boolean = False) As Matcher
            If String.IsNullOrEmpty(patternText) Then
                Throw New ArgumentNullException(NameOf(patternText))
            End If

            Dim lines = patternText.Split({vbCrLf, vbLf, vbCr}, StringSplitOptions.None)
            Return BuildMatcherFromMatchFile(lines, caseSensitive)
        End Function

        ''' <summary>
        ''' 从输入路径收集匹配的文件和目录。目录链只收集到输入根,不会延伸到文件系统根目录。
        ''' </summary>
        Public Shared Function PlanAdd_ByFullPathInputs(inputs As IEnumerable(Of String),
                                                 matcher As Matcher) As AddPlan
            Dim plan As New AddPlan()
            Dim fileSet As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim dirSet As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            Dim normalized = inputs.
                Where(Function(p) Not String.IsNullOrWhiteSpace(p)).
                Select(Function(p) EnsureLongPathPrefix(Path.GetFullPath(p))).
                ToList()

            For Each inputRoot In normalized
                If Directory.Exists(inputRoot) Then
                    ' 处理目录输入
                    Dim results = matcher.GetResultsInFullPath(inputRoot)

                    For Each f In results
                        Dim ff = EnsureLongPathPrefix(f)
                        If fileSet.Add(ff) Then
                            ' 添加从文件到输入根的目录链
                            AddDirChainToRoot(Path.GetDirectoryName(ff), inputRoot, dirSet)
                        End If
                    Next

                ElseIf File.Exists(inputRoot) Then
                    ' 处理单个文件输入
                    Dim parentDir = Path.GetDirectoryName(inputRoot)
                    If parentDir IsNot Nothing Then
                        Dim results = matcher.GetResultsInFullPath(parentDir)
                        Dim inputNormal = StripLongPrefix(inputRoot)

                        ' 检查文件是否在匹配结果中
                        Dim matched = results.Any(Function(r)
                                                      Return r.Equals(inputNormal, StringComparison.OrdinalIgnoreCase)
                                                  End Function)

                        If matched Then
                            If fileSet.Add(inputRoot) Then
                                ' 添加从文件到父目录的目录链(这里父目录就是根)
                                AddDirChainToRoot(parentDir, parentDir, dirSet)
                            End If
                        End If
                    End If
                End If
            Next

            plan.Files.AddRange(fileSet)
            plan.Dirs.AddRange(dirSet)
            plan.Files.Sort(StringComparer.OrdinalIgnoreCase)
            plan.Dirs.Sort(StringComparer.OrdinalIgnoreCase)

            Return plan
        End Function

        ''' <summary>
        ''' 添加从 dir 到 rootDir 的目录链(不包含 rootDir 的父目录)
        ''' </summary>
        Private Shared Sub AddDirChainToRoot(dir As String, rootDir As String,
                                       dirSet As HashSet(Of String))
            If String.IsNullOrEmpty(dir) Then Exit Sub

            Dim cur = EnsureLongPathPrefix(dir)
            Dim root = EnsureLongPathPrefix(rootDir)

            Dim rootNormal = StripLongPrefix(root).TrimEnd("\"c, "/"c).ToLower()

            Do
                If Not dirSet.Add(cur) Then Exit Do

                Dim curNormal = StripLongPrefix(cur).TrimEnd("\"c, "/"c).ToLower()
                If curNormal = rootNormal Then Exit Do
                If curNormal.Length <= rootNormal.Length Then Exit Do

                Dim parent = Path.GetDirectoryName(cur)
                If String.IsNullOrEmpty(parent) Then Exit Do
                If parent.Equals(cur, StringComparison.OrdinalIgnoreCase) Then Exit Do

                cur = parent
            Loop
        End Sub

        Private Shared Function EnsureLongPathPrefix(p As String) As String
            If String.IsNullOrEmpty(p) Then Return p
            If p.StartsWith("\\?\") Then Return p
            If p.StartsWith("\\") Then
                Return "\\?\UNC\" & p.Substring(2)
            End If
            Return "\\?\" & p
        End Function

        Private Shared Function StripLongPrefix(p As String) As String
            If String.IsNullOrEmpty(p) Then Return p
            If p.StartsWith("\\?\UNC\", StringComparison.OrdinalIgnoreCase) Then
                Return "\\" & p.Substring(8)
            ElseIf p.StartsWith("\\?\", StringComparison.OrdinalIgnoreCase) Then
                Return p.Substring(4)
            End If
            Return p
        End Function
    End Class
End Class