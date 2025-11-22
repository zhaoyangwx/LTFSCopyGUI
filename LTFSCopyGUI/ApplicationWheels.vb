Imports System.ComponentModel
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Runtime.Serialization
Imports System.Text
Imports System.Xml.Serialization
Imports Microsoft.Diagnostics.Runtime
Imports Microsoft.Extensions.FileSystemGlobbing
Imports stdole
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
    Public Shared Function NormalizePath(p As String) As String
        If String.IsNullOrWhiteSpace(p) Then Return p
        Dim s = p.Replace("/"c, "\"c)
        If s.Length > 3 AndAlso s.EndsWith("\", StringComparison.Ordinal) Then
            s = s.TrimEnd("\"c)
        End If
        Return s
    End Function

    Public Shared Function PathIsUnder(p As String, root As String) As Boolean
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

    Public Shared Function GetRelativePathWin(basePath As String, fullPath As String) As String
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
    Public Class AddFile
        Public Property SourceFullPath As String
        Public Property RelativePath As String
    End Class

    Public Class AddPlan
        Public ReadOnly Dirs As New List(Of String)()
        Public ReadOnly Files As New List(Of AddFile)()
    End Class
End Class

Public Module GlobCollector
    Public Function BuildMatcherFromStrings(patterns As IEnumerable(Of String),
                                              Optional caseSensitive As Boolean = False) As Matcher
        If patterns Is Nothing Then Throw New ArgumentNullException(NameOf(patterns))

        Dim comparison = If(caseSensitive, StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase)
        Dim matcher = New Matcher(comparison)

        Dim hasIncludeRules As Boolean = False
        Dim lineNumber As Integer = 0

        For Each rawLine In patterns
            lineNumber += 1
            If rawLine Is Nothing Then Continue For

            Dim line = rawLine.Trim()
            If line.Length = 0 Then Continue For
            If line.StartsWith("#"c) Then Continue For

            Try
                Dim isExclude As Boolean = False
                Dim pattern As String = line

                ' 处理转义：\!, \#, \\ 前缀
                If line.StartsWith("\"c) AndAlso line.Length > 1 Then
                    Dim nextChar = line(1)
                    If nextChar = "!"c OrElse nextChar = "#"c OrElse nextChar = "\"c Then
                        pattern = line.Substring(1)
                    End If
                ElseIf line.StartsWith("!"c) Then
                    isExclude = True
                    pattern = line.Substring(1).TrimStart()
                    If pattern.Length = 0 Then Continue For
                End If

                If Not IsValidPattern(pattern) Then Continue For

                If isExclude Then
                    matcher.AddExclude(pattern)
                Else
                    matcher.AddInclude(pattern)
                    hasIncludeRules = True
                End If

            Catch
                Continue For
            End Try
        Next

        If Not hasIncludeRules Then
            matcher.AddInclude("**/*")
        End If

        Return matcher
    End Function

    Public Function BuildMatcherFromFile(filePath As String,
                                         Optional encoding As Encoding = Nothing,
                                         Optional caseSensitive As Boolean = False) As Matcher
        If String.IsNullOrWhiteSpace(filePath) Then Throw New ArgumentNullException(NameOf(filePath))
        If Not File.Exists(filePath) Then Throw New FileNotFoundException("Pattern file not found.", filePath)

        Dim enc = If(encoding, Encoding.UTF8)
        Dim lines = File.ReadAllLines(filePath, enc)
        Return BuildMatcherFromStrings(lines, caseSensitive)
    End Function

    Public Function BuildMatcherFromString(patternText As String,
                                           Optional caseSensitive As Boolean = False) As Matcher
        If String.IsNullOrWhiteSpace(patternText) Then Throw New ArgumentNullException(NameOf(patternText))
        Dim lines = patternText.Split({vbCrLf, vbLf, vbCr}, StringSplitOptions.None)
        Return BuildMatcherFromStrings(lines, caseSensitive)
    End Function

    Private Function IsValidPattern(pattern As String) As Boolean
        If String.IsNullOrEmpty(pattern) Then Return False

        Dim invalidChars = {"<"c, ">"c, "|"c, """"c, vbNullChar}
        For Each ch In invalidChars
            If pattern.Contains(ch) Then Return False
        Next

        ' 方括号匹配
        Dim openBrackets = pattern.Count(Function(c) c = "["c)
        Dim closeBrackets = pattern.Count(Function(c) c = "]"c)
        If openBrackets <> closeBrackets Then Return False

        ' 连续 * 超过 2
        If pattern.Contains("***") Then Return False

        Return True
    End Function

    Public Function PlanAdd_ByFullPathInputs(inputs As IEnumerable(Of String),
                                             matcher As Matcher) As GlobHelper.AddPlan
        If inputs Is Nothing Then Throw New ArgumentNullException(NameOf(inputs))
        If matcher Is Nothing Then Throw New ArgumentNullException(NameOf(matcher))

        Dim plan As New GlobHelper.AddPlan()

        Dim seenSource As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim seenRelative As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim dirSet As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        Dim normalized = inputs.
            Where(Function(p) Not String.IsNullOrWhiteSpace(p)).
            Select(Function(p) NormalizeFullPath(p)).
            ToList()

        For Each inputPath In normalized
            If Directory.Exists(inputPath) Then
                Dim baseDirForRel = Path.GetDirectoryName(TrailingTrimSeparators(inputPath))
                If String.IsNullOrEmpty(baseDirForRel) Then
                    Continue For
                End If

                Dim matched = matcher.GetResultsInFullPath(inputPath)
                For Each fileFull In matched
                    If Not File.Exists(fileFull) Then Continue For
                    Dim rel = GetRelativePathWin(baseDirForRel, fileFull)
                    rel = NormalizeRelativeSeparators(rel)

                    If rel.StartsWith(Path.DirectorySeparatorChar) OrElse rel.StartsWith(Path.AltDirectorySeparatorChar) Then
                        rel = rel.Substring(1)
                    End If
                    If String.IsNullOrEmpty(rel) Then Continue For

                    If Not seenRelative.Add(rel) Then Continue For
                    If Not seenSource.Add(fileFull) Then Continue For

                    plan.Files.Add(New GlobHelper.AddFile With {
                        .SourceFullPath = fileFull,
                        .RelativePath = rel
                    })

                    AddParentDirsOfRelativePath(rel, dirSet)
                Next

            ElseIf File.Exists(inputPath) Then
                Dim parentDir = Path.GetDirectoryName(inputPath)
                If String.IsNullOrEmpty(parentDir) Then Continue For

                Dim relForMatch = GetRelativePathWin(parentDir, inputPath)
                relForMatch = NormalizeRelativeSeparators(relForMatch)

                If matcher.Match(relForMatch).HasMatches Then
                    Dim relDisplay = Path.GetFileName(inputPath)
                    If String.IsNullOrEmpty(relDisplay) Then Continue For

                    If Not seenRelative.Add(relDisplay) Then Continue For
                    If Not seenSource.Add(inputPath) Then Continue For

                    plan.Files.Add(New GlobHelper.AddFile With {
                        .SourceFullPath = inputPath,
                        .RelativePath = relDisplay
                    })
                End If
            Else
                Continue For
            End If
        Next

        plan.Dirs.AddRange(dirSet.OrderBy(Function(s) s, StringComparer.OrdinalIgnoreCase))
        plan.Files.Sort(Function(a, b) StringComparer.OrdinalIgnoreCase.Compare(a.RelativePath, b.RelativePath))

        Return plan
    End Function

    Private Function NormalizeFullPath(p As String) As String
        Dim full = Path.GetFullPath(p)
        Return TrailingTrimSeparators(full)
    End Function

    Private Function TrailingTrimSeparators(p As String) As String
        If String.IsNullOrEmpty(p) Then Return p
        If Path.GetPathRoot(p).Equals(p, StringComparison.OrdinalIgnoreCase) Then
            Return p
        End If
        Return p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
    End Function

    Public Function GetRelativePathWin(baseDir As String, targetPath As String) As String
        If String.IsNullOrEmpty(baseDir) Then Throw New ArgumentNullException(NameOf(baseDir))
        If String.IsNullOrEmpty(targetPath) Then Throw New ArgumentNullException(NameOf(targetPath))

        Dim baseFixed = baseDir
        If Not baseFixed.EndsWith(Path.DirectorySeparatorChar) AndAlso Not baseFixed.EndsWith(Path.AltDirectorySeparatorChar) Then
            baseFixed &= Path.DirectorySeparatorChar
        End If

        Dim baseUri As New Uri(PathToUri(baseFixed))
        Dim targetUri As New Uri(PathToUri(targetPath))

        Dim relUri = baseUri.MakeRelativeUri(targetUri)
        Dim rel = Uri.UnescapeDataString(relUri.ToString())

        rel = rel.Replace("/"c, Path.DirectorySeparatorChar)
        Return rel
    End Function

    Private Function PathToUri(p As String) As String
        Dim u = New Uri(p, UriKind.Absolute)
        Return u.AbsoluteUri
    End Function

    Private Function NormalizeRelativeSeparators(rel As String) As String
        If String.IsNullOrEmpty(rel) Then Return rel
        Return rel.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
    End Function

    Private Sub AddParentDirsOfRelativePath(relativePath As String, dirSet As HashSet(Of String))
        Dim dir = Path.GetDirectoryName(relativePath)
        If String.IsNullOrEmpty(dir) Then Exit Sub

        Dim parts = dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).
                         Where(Function(s) Not String.IsNullOrEmpty(s)).
                         ToArray()
        If parts.Length = 0 Then Exit Sub

        Dim cur As String = parts(0)
        dirSet.Add(cur)
        For i = 1 To parts.Length - 1
            cur = cur & Path.DirectorySeparatorChar & parts(i)
            dirSet.Add(cur)
        Next
    End Sub
End Module

Public Class DisplayHelper
    Public Shared Function GetScreenScale() As Single
        Using graphics As Graphics = Graphics.FromHwnd(IntPtr.Zero)
            Return graphics.DpiX / 96
        End Using
    End Function
    Public Shared Property ScreenScale As Single = 1
    Private Shared _font As System.Drawing.Font
    Public Shared Property DisplayFont As System.Drawing.Font
        Get
            If _font Is Nothing Then Return New Drawing.Font("SimSun", 12 * ScreenScale, GraphicsUnit.Pixel)
            Return _font
        End Get
        Set(value As System.Drawing.Font)
            _font = value
        End Set
    End Property
    Public Shared Sub BeforeInitializeComponent(frm As Form)
        frm.SuspendLayout()
        frm.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
    End Sub
    Public Shared Sub AfterInitializeComponent(frm As Form)
        frm.PerformAutoScale()
        frm.Font = DisplayFont

        Dim cchk As New List(Of Control)
        For Each c As Control In frm.Controls
            cchk.Add(c)
        Next
        Dim clist As New List(Of Control)
        While cchk.Count > 0
            Dim cchk2 As New List(Of Control)
            For Each c As Control In cchk
                For Each c2 As Control In c.Controls
                    cchk2.Add(c2)
                Next
            Next
            clist.AddRange(cchk)
            cchk = cchk2
        End While

        For Each f As Reflection.FieldInfo In frm.GetType().GetFields(
                Reflection.BindingFlags.Public Or
                Reflection.BindingFlags.NonPublic Or
                Reflection.BindingFlags.Instance Or
                Reflection.BindingFlags.Static)
            Dim o As Object = f.GetValue(frm)
            If TypeOf o Is ContextMenuStrip Then clist.Add(o)
        Next

        For Each c As Control In clist
            If TypeOf c Is ListView Then
                For Each col As ColumnHeader In DirectCast(c, ListView).Columns
                    col.Width *= ScreenScale
                Next
            ElseIf TypeOf c Is ToolStrip Then
                If ScreenScale <> 1 AndAlso TypeOf c Is MenuStrip OrElse TypeOf c Is ContextMenuStrip Then DirectCast(c, ToolStrip).Renderer = New RichMenuStrip.HiDPIRenderer()
                DirectCast(c, ToolStrip).ImageScalingSize = New Size(16 * ScreenScale, 16 * ScreenScale)
                Dim items As New List(Of ToolStripMenuItem)
                Dim icd As New List(Of ToolStripMenuItem)
                For Each itm As ToolStripItem In DirectCast(c, ToolStrip).Items
                    If TypeOf itm Is ToolStripMenuItem Then
                        icd.Add(itm)
                    ElseIf TypeOf itm Is ToolStripButton Then
                        itm.Width *= DisplayHelper.ScreenScale
                        itm.Height *= DisplayHelper.ScreenScale
                    End If
                Next
                While icd.Count > 0
                    Dim icd2 As New List(Of ToolStripMenuItem)
                    For Each itm As ToolStripMenuItem In icd
                        For Each ditm As ToolStripItem In itm.DropDownItems
                            If TypeOf ditm Is ToolStripMenuItem Then
                                icd2.Add(ditm)
                            ElseIf TypeOf ditm Is ToolStripButton Then
                                ditm.Width *= DisplayHelper.ScreenScale
                                ditm.Height *= DisplayHelper.ScreenScale
                            End If
                        Next
                    Next
                    items.AddRange(icd)
                    icd = icd2
                End While
                For Each itm As ToolStripMenuItem In items
                    If itm.DropDown IsNot Nothing Then
                        itm.DropDown.ImageScalingSize = New Size(16 * ScreenScale, 16 * ScreenScale)
                    End If
                Next
            End If
        Next

        frm.ResumeLayout(True)
    End Sub

    Public Shared Function ShowInputDialog(Prompt As String, Title As String, ByRef Response As UShort) As DialogResult
        Dim resp As String = Response.ToString()
        Dim result As DialogResult = ShowInputDialog(Prompt, Title, resp)
        If Not UShort.TryParse(resp, Response) Then Return DialogResult.Cancel
        Return result
    End Function
    Public Shared Function ShowInputDialog(Prompt As String, Title As String, ByRef Response As Integer) As DialogResult
        Dim resp As String = Response.ToString()
        Dim result As DialogResult = ShowInputDialog(Prompt, Title, resp)
        If Not Integer.TryParse(resp, Response) Then Return DialogResult.Cancel
        Return result
    End Function
    Public Shared Function ShowInputDialog(Prompt As String, Title As String, ByRef Response As Long) As DialogResult
        Dim resp As String = Response.ToString()
        Dim result As DialogResult = ShowInputDialog(Prompt, Title, resp)
        If Not Long.TryParse(resp, Response) Then Return DialogResult.Cancel
        Return result
    End Function
    Public Shared Function ShowInputDialog(Prompt As String, Title As String, ByRef Response As ULong) As DialogResult
        Dim resp As String = Response.ToString()
        Dim result As DialogResult = ShowInputDialog(Prompt, Title, resp)
        If Not ULong.TryParse(resp, Response) Then Return DialogResult.Cancel
        Return result
    End Function
    Public Shared Function ShowInputDialog(Prompt As String, Title As String, ByRef Response As Double) As DialogResult
        Dim resp As String = Response.ToString()
        Dim result As DialogResult = ShowInputDialog(Prompt, Title, resp)
        If Not Double.TryParse(resp, Response) Then Return DialogResult.Cancel
        Return result
    End Function
    Public Shared Function ShowInputDialog(Prompt As String, Title As String, ByRef Response As String) As DialogResult
        Dim size As System.Drawing.Size = New System.Drawing.Size(200, 90)
        Dim inputDialog As Form = New Form()
        inputDialog.StartPosition = FormStartPosition.CenterParent
        inputDialog.Font = DisplayFont
        inputDialog.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        inputDialog.MinimizeBox = False
        inputDialog.MaximizeBox = False
        inputDialog.ClientSize = New Size(size.Width * ScreenScale, size.Height * ScreenScale)
        inputDialog.AutoScaleMode = AutoScaleMode.Font
        inputDialog.Text = Title
        Dim promptLabel As Label = New Label()
        promptLabel.Location = New Point(5 * ScreenScale, 5 * ScreenScale)
        promptLabel.Text = Prompt
        promptLabel.AutoSize = True
        inputDialog.Controls.Add(promptLabel)
        Dim textBox As System.Windows.Forms.TextBox = New TextBox()
        textBox.Size = New System.Drawing.Size((size.Width - 10) * ScreenScale, 23 * ScreenScale)
        textBox.Location = New System.Drawing.Point(5 * ScreenScale, 25 * ScreenScale)
        textBox.Text = Response
        inputDialog.Controls.Add(textBox)
        Dim okButton As Button = New Button()
        okButton.DialogResult = System.Windows.Forms.DialogResult.OK
        okButton.Name = "okButton"
        okButton.Size = New System.Drawing.Size(75 * ScreenScale, 23 * ScreenScale)
        okButton.Text = $"&OK"
        okButton.Location = New System.Drawing.Point((size.Width - 80 - 80) * ScreenScale, 59 * ScreenScale)
        inputDialog.Controls.Add(okButton)
        Dim cancelButton As Button = New Button()
        cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
        cancelButton.Name = "cancelButton"
        cancelButton.Size = New System.Drawing.Size(75 * ScreenScale, 23 * ScreenScale)
        cancelButton.Text = $"&Cancel"
        cancelButton.Location = New System.Drawing.Point((size.Width - 80) * ScreenScale, 59 * ScreenScale)
        inputDialog.Controls.Add(cancelButton)
        inputDialog.AcceptButton = okButton
        inputDialog.CancelButton = cancelButton
        inputDialog.PerformAutoScale()
        Dim result As DialogResult = inputDialog.ShowDialog()
        If result = DialogResult.OK Then Response = textBox.Text
        Return result
    End Function
End Class
Public Class RichMenuStrip
    Inherits System.Windows.Forms.MenuStrip
    Private Overloads Sub RescaleConstantsForDpi(deviceDpiOld As Integer, deviceDpiNew As Integer)
        ' Use reflection to invoke the internal ResetScaling method
        Dim resetScalingMethod = GetType(System.Windows.Forms.MenuStrip).GetMethod("ResetScaling", BindingFlags.NonPublic Or BindingFlags.Instance)
        If (resetScalingMethod IsNot Nothing) Then
            resetScalingMethod.Invoke(Me, {deviceDpiNew})
        End If
    End Sub
    Public Sub New(container As IContainer)
        RescaleConstantsForDpi(96, DeviceDpi)
    End Sub
    Public Sub New()
        RescaleConstantsForDpi(96, DeviceDpi)
    End Sub
    Public Class HiDPIRenderer
        Inherits ToolStripProfessionalRenderer

        Protected Overrides Sub OnRenderItemCheck(e As ToolStripItemImageRenderEventArgs)
            Dim g = e.Graphics
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            Dim size = CInt(16 * DisplayHelper.ScreenScale)

            Dim rect = New Rectangle(e.ImageRectangle.X, e.ImageRectangle.Y, size, size)
            Dim rect2 As New Rectangle(rect.X + rect.Width / 8, rect.Y + rect.Height / 8, rect.Width * 0.75, rect.Height * 0.75)
            Using pen As New Pen(Color.Black, 1.5 * DisplayHelper.ScreenScale)
                g.FillRectangle(New SolidBrush(Color.FromArgb(181, 215, 243)), rect)
                g.DrawRectangle(New Pen(Color.FromArgb(36, 138, 220)), rect)
                g.DrawLines(pen, {
                    New Point(rect2.Left + size * 0.75 * 0.2, rect2.Top + size * 0.75 * 0.55),
                    New Point(rect2.Left + size * 0.75 * 0.45, rect2.Top + size * 0.75 * 0.8),
                    New Point(rect2.Left + size * 0.75 * 0.85, rect2.Top + size * 0.75 * 0.2)
                })
            End Using
        End Sub
    End Class
End Class

Public Class ErrRateHelper
    Public Shared Function MixColor(col1 As Color, col2 As Color, val As Double, min As Double, max As Double) As Color
        If val <= min Then Return col1
        If val >= max Then Return col2
        Dim ratio As Double = (val - min) / (max - min)
        Return Color.FromArgb(col1.R * (1 - ratio) + col2.R * ratio, col1.G * (1 - ratio) + col2.G * ratio, col1.B * (1 - ratio) + col2.B * ratio)
    End Function
    Public Shared Function GetColor(segvalue As String, Optional ByVal colorCoeff As Double = 1) As Color
        Static col1 As Color = Color.FromArgb(101 * colorCoeff, 225 * colorCoeff, 111 * colorCoeff)
        Static col2 As Color = Color.FromArgb(237 * colorCoeff, 208 * colorCoeff, 120 * colorCoeff)
        Static col3 As Color = Color.FromArgb(251 * colorCoeff, 145 * colorCoeff, 85 * colorCoeff)
        Static col4 As Color = Color.FromArgb(255 * colorCoeff, 71 * colorCoeff, 73 * colorCoeff)

        If segvalue.Length > 0 Then
            Dim errratelog As Double = 0
            If segvalue.Length = 5 Then
                Double.TryParse(segvalue, errratelog)
            End If

            If segvalue = "-Inf" OrElse errratelog <= -6 Then
                Return col1
            ElseIf errratelog <= -5 Then
                Return MixColor(col1, col2, errratelog, -6, -5)
            ElseIf errratelog <= -4 Then
                Return MixColor(col2, col3, errratelog, -5, -4)
            ElseIf errratelog <= -3.5 Then
                Return MixColor(col3, col4, errratelog, -4, -3.5)
            ElseIf errratelog > -3.5 Then
                Return col4
            End If
        Else
        End If
    End Function
End Class

Partial Public Class ApplicationWheels
    Public Shared ReadOnly Property ApplicationInfo As String
        Get
            Return $"{My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)} Build {Build}{My.Settings.Application_License}"
        End Get
    End Property
    Public Shared Function TryExecute(ByVal command As Func(Of Byte()), Optional ByVal AutoRetryCount As Integer = 0) As Boolean
        Dim succ As Boolean = False
        While Not succ
            Dim sense() As Byte
            Try
                sense = command()
            Catch ex As Exception
                Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_Error}{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                    Case DialogResult.Abort
                        Throw ex
                    Case DialogResult.Retry
                        succ = False
                    Case DialogResult.Ignore
                        succ = True
                        Exit While
                End Select
                Continue While
            End Try
            If ((sense(2) >> 6) And &H1) = 1 Then
                If (sense(2) And &HF) = 13 Then
                    succ = True
                Else
                    succ = True
                    Exit While
                End If
            ElseIf sense(2) And &HF <> 0 Then
                Try
                    Throw New Exception("SCSI sense error")
                Catch ex As Exception
                    If AutoRetryCount > 0 Then
                        AutoRetryCount -= 1
                        succ = False
                    Else
                        Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_RestoreErr}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                            Case DialogResult.Abort
                                Throw New Exception(TapeUtils.ParseSenseData(sense))
                            Case DialogResult.Retry
                                succ = False
                            Case DialogResult.Ignore
                                succ = True
                                Exit While
                        End Select
                    End If
                End Try
            Else
                succ = True
            End If
        End While
        Return succ
    End Function
End Class

Public NotInheritable Class FileDropHandler
    Implements IMessageFilter, IDisposable
    <DllImport("user32.dll", SetLastError:=True, CallingConvention:=CallingConvention.Winapi)>
    Private Shared Function ChangeWindowMessageFilterEx(ByVal hWnd As IntPtr, ByVal message As UInteger, ByVal action As ChangeFilterAction, pChangeFilterStruct As ChangeFilterStruct) As <MarshalAs(UnmanagedType.Bool)> Boolean

    End Function

    <DllImport("shell32.dll", SetLastError:=False, CallingConvention:=CallingConvention.Winapi)>
    Private Shared Sub DragAcceptFiles(ByVal hWnd As IntPtr, ByVal fAccept As Boolean)
    End Sub

    <DllImport("shell32.dll", SetLastError:=False, CharSet:=CharSet.Unicode, CallingConvention:=CallingConvention.Winapi)>
    Private Shared Function DragQueryFile(ByVal hWnd As IntPtr, ByVal iFile As UInteger, ByVal lpszFile As StringBuilder, ByVal cch As Integer) As UInteger

    End Function

    <DllImport("shell32.dll", SetLastError:=False, CallingConvention:=CallingConvention.Winapi)>
    Private Shared Sub DragFinish(ByVal hDrop As IntPtr)

    End Sub

    <StructLayout(LayoutKind.Sequential)>
    Private Structure ChangeFilterStruct

        Public CbSize As UInteger

        Public ExtStatus As ChangeFilterStatus
    End Structure

    Private Enum ChangeFilterAction As UInteger

        MSGFLT_RESET

        MSGFLT_ALLOW

        MSGFLT_DISALLOW
    End Enum

    Private Enum ChangeFilterStatus As UInteger

        MSGFLTINFO_NONE

        MSGFLTINFO_ALREADYALLOWED_FORWND

        MSGFLTINFO_ALREADYDISALLOWED_FORWND

        MSGFLTINFO_ALLOWED_HIGHER
    End Enum

    Private Const WM_COPYGLOBALDATA As UInteger = 73

    Private Const WM_COPYDATA As UInteger = 74

    Private Const WM_DROPFILES As UInteger = 563

    Private Const GetIndexCount As UInteger = 4294967295

    Private _ContainerControl As Control

    Private _DisposeControl As Boolean

    Public ReadOnly Property ContainerControl As Control
        Get
            Return _ContainerControl
        End Get
    End Property

    Public Sub New(ByVal containerControl As Control)
        Me.New(containerControl, False)

    End Sub

    Public Sub New(ByVal containerControl As Control, ByVal releaseControl As Boolean)
        Try
            _ContainerControl = containerControl
        Catch ex As Exception
            Throw New ArgumentNullException("control", "control is null.")
        End Try
        If containerControl.IsDisposed Then
            Throw New ObjectDisposedException("control")
        End If

        Me._DisposeControl = releaseControl
        Dim status = New ChangeFilterStruct With {.CbSize = 8}
        If Not ChangeWindowMessageFilterEx(containerControl.Handle, WM_DROPFILES, ChangeFilterAction.MSGFLT_ALLOW, Nothing) Then
            Throw New Win32Exception(Marshal.GetLastWin32Error)
        End If

        If Not ChangeWindowMessageFilterEx(containerControl.Handle, WM_COPYGLOBALDATA, ChangeFilterAction.MSGFLT_ALLOW, Nothing) Then
            Throw New Win32Exception(Marshal.GetLastWin32Error)
        End If

        If Not ChangeWindowMessageFilterEx(containerControl.Handle, WM_COPYDATA, ChangeFilterAction.MSGFLT_ALLOW, Nothing) Then
            Throw New Win32Exception(Marshal.GetLastWin32Error)
        End If

        DragAcceptFiles(containerControl.Handle, True)
        Application.AddMessageFilter(Me)
    End Sub

    Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
        If ((Me._ContainerControl Is Nothing) OrElse Me._ContainerControl.IsDisposed) Then
            Return False
        End If

        If Me._ContainerControl.AllowDrop Then
            _ContainerControl.AllowDrop = False
            Return False
        End If
        If (m.Msg = WM_DROPFILES) Then
            Dim handle = m.WParam
            Dim fileCount = DragQueryFile(handle, GetIndexCount, Nothing, 0)
            Dim fileNames((fileCount) - 1) As String
            Dim sb = New StringBuilder(262)
            Dim charLength = sb.Capacity
            Dim i As UInteger = 0
            Do While (i < fileCount)
                If (DragQueryFile(handle, i, sb, charLength) > 0) Then
                    fileNames(i) = sb.ToString
                End If

                i = (i + 1)
            Loop

            DragFinish(handle)
            Me._ContainerControl.AllowDrop = True
            Me._ContainerControl.DoDragDrop(fileNames, DragDropEffects.All)
            Me._ContainerControl.AllowDrop = False
            Return True
        End If

        Return False
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        If (Me._ContainerControl Is Nothing) Then
            If (Me._DisposeControl AndAlso Not Me._ContainerControl.IsDisposed) Then
                Me._ContainerControl.Dispose()
            End If

            Application.RemoveMessageFilter(Me)
            Me._ContainerControl = Nothing
        End If

    End Sub
End Class


