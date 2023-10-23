Imports System.ComponentModel

Public Class Form1
    Public schema As ltfsindex
    Public contents As ltfsindex.contentsDef
    Public filelist As New List(Of String)
    Public Class TapeFileInfo
        Public Property Path As String
        Public Property Partition As ltfsindex.PartitionLabel
        Public Property BlockNumber As Long
        Public Property FileLength As Long
    End Class
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        LoadSchemaFile()
    End Sub
    Public Sub LoadSchemaFile(Optional ByVal ReloadFile As Boolean = True)
        Dim th As New Threading.Thread(
            Sub()
                Try
                    Invoke(Sub() Label4.Text = SchemaLoadText.Items(0))
                    Dim s As String = ""
                    If ReloadFile Or schema Is Nothing Then s = My.Computer.FileSystem.ReadAllText(TextBox1.Text)
                    Invoke(Sub() Label4.Text = SchemaLoadText.Items(1))
                    Invoke(Sub() Label4.Text = SchemaLoadText.Items(2))

                    If ReloadFile Or schema Is Nothing Then
                        If s.Contains("XMLSchema") Then
                            schema = ltfsindex.FromXML(s)
                        Else
                            schema = ltfsindex.FromSchemaText(s)
                        End If
                    End If
                    contents = New ltfsindex.contentsDef With {._directory = schema._directory(0).contents._directory, ._file = schema._directory(0).contents._file}
                    Dim flist As New List(Of TapeFileInfo)
                    ScanFile(contents, flist, "")
                    Dim alist As New List(Of TapeFileInfo)
                    Dim blist As New List(Of TapeFileInfo)
                    Invoke(Sub() Label4.Text = SchemaLoadText.Items(3))
                    Parallel.ForEach(flist,
                        Sub(f As TapeFileInfo)
                            If f.Partition = ltfsindex.PartitionLabel.a Then
                                SyncLock alist
                                    alist.Add(f)
                                End SyncLock
                            Else
                                SyncLock blist
                                    blist.Add(f)
                                End SyncLock
                            End If
                        End Sub)
                    Invoke(Sub() Label4.Text = SchemaLoadText.Items(4))
                    Parallel.ForEach({alist, blist},
                        Sub(nlist As List(Of TapeFileInfo))
                            If nlist Is alist Then
                                alist.Sort(New Comparison(Of TapeFileInfo)(Function(a As TapeFileInfo, b As TapeFileInfo) As Integer
                                                                               If a.BlockNumber <> b.BlockNumber Then
                                                                                   Return a.BlockNumber.CompareTo(b.BlockNumber)
                                                                               Else
                                                                                   Return a.Path.CompareTo(b.Path)
                                                                               End If
                                                                           End Function))
                            ElseIf nlist Is blist Then
                                blist.Sort(New Comparison(Of TapeFileInfo)(Function(a As TapeFileInfo, b As TapeFileInfo) As Integer
                                                                               If a.BlockNumber <> b.BlockNumber Then
                                                                                   Return a.BlockNumber.CompareTo(b.BlockNumber)
                                                                               Else
                                                                                   Return a.Path.CompareTo(b.Path)
                                                                               End If
                                                                           End Function))
                            End If
                        End Sub)
                    filelist = New List(Of String)
                    Invoke(Sub() Label4.Text = SchemaLoadText.Items(5))
                    Dim counter As Integer = 0
                    Dim total As Integer = alist.Count + blist.Count
                    Dim ran As New Random
                    Dim stepval As Integer = ran.Next(100, 1000)

                    Dim p As New System.Text.StringBuilder()
                    Dim fdir As String = TextBox3.Text
                    If fdir.EndsWith("\") Then fdir = fdir.TrimEnd("\")
                    fdir &= "\"
                    Dim tdir As String = TextBox4.Text
                    If tdir.EndsWith("\") Then tdir = tdir.TrimEnd("\")
                    tdir &= "\"
                    If Not CheckBox1.Checked Then
                        p.Append($"Partition{vbTab}Startblock{vbTab}Length{vbTab}Path{vbCrLf}")
                        For Each f As TapeFileInfo In alist.Concat(blist)
                            p.Append(f.Partition.ToString & vbTab & f.BlockNumber & vbTab & f.FileLength & vbTab & f.Path & vbCrLf)
                            filelist.Add(f.Path)
                            Threading.Interlocked.Increment(counter)
                            If counter Mod stepval = 0 Then
                                Invoke(Sub() Label4.Text = $"{SchemaLoadText.Items(5)}{counter}/{total}")
                                stepval = ran.Next(100, 1000)
                            End If
                        Next
                    Else
                        p.Append("chcp 65001" & vbCrLf)
                        For Each f As TapeFileInfo In alist.Concat(blist)
                            If CheckBox2.Checked Then
                                p.Append($"echo f|robocopy ""{fdir}{f.Path}"" ""{tdir }{f.Path}"" /Copy:D /MIR /W:10 /R:10 /J{vbCrLf}")
                            Else
                                p.Append($"echo f|xcopy /J /D /Y ""{fdir}{f.Path}"" ""{tdir }{f.Path}""{vbCrLf}")
                            End If
                            filelist.Add(f.Path)
                            Threading.Interlocked.Increment(counter)
                            If counter Mod stepval = 0 Then
                                Invoke(Sub() Label4.Text = $"{SchemaLoadText.Items(5)}{counter}/{total}")
                                stepval = ran.Next(100, 1000)
                            End If
                        Next
                    End If
                    Invoke(Sub() Label4.Text = SchemaLoadText.Items(6))
                    Invoke(Sub() TextBox2.Text = p.ToString)
                Catch ex As Exception
                    Invoke(Sub() TextBox2.Text = ex.Message)
                End Try
                Invoke(Sub()
                           Button1.Enabled = True
                           Button2.Enabled = True
                           Button3.Enabled = True
                           CheckBox1.Enabled = True
                           TextBox1.Enabled = True
                           TextBox2.Enabled = True
                           TextBox3.Enabled = True
                           TextBox4.Enabled = True
                           Label4.Visible = False
                       End Sub)
            End Sub)
        Button1.Enabled = False
        Button2.Enabled = False
        Button3.Enabled = False
        CheckBox1.Enabled = False
        TextBox1.Enabled = False
        TextBox2.Enabled = False
        TextBox3.Enabled = False
        TextBox4.Enabled = False
        Label4.Visible = True
        th.Start()
    End Sub
    Public Function LookforXMLEndPosition(ByRef s As String, ByVal Target As String, ByVal StartPos As String) As Long
        Dim i As Integer = StartPos
        Dim TargetBra As String = $"<{Target}>"
        Dim TargetKet As String = $"</{Target}>"
        While i < s.Length - 1
            i += 1
            If s.Substring(i, TargetBra.Length).Equals(TargetBra) Then
                i = LookforXMLEndPosition(s, Target, i)
                Continue While
            End If
            If s.Substring(i, TargetKet.Length).Equals(TargetKet) Then
                Return i
            End If
        End While
        Return i
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If My.Computer.FileSystem.FileExists(TextBox1.Text) Then
            OpenFileDialog1.FileName = TextBox1.Text
            OpenFileDialog1.InitialDirectory = My.Computer.FileSystem.GetFileInfo(TextBox1.Text).DirectoryName
        End If
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            TextBox1.Text = OpenFileDialog1.FileName
            Button2_Click(sender, e)
        End If
    End Sub

    Public Sub ScanFile(ByRef contents As ltfsindex.contentsDef, ByVal flist As List(Of TapeFileInfo), Optional ByVal ParentPath As String = "\")
        If contents._directory IsNot Nothing Then
            If contents._directory.Count > 0 Then
                Parallel.ForEach(contents._directory,
                    Sub(d As ltfsindex.directory)
                        ScanFile(d.contents, flist, ParentPath & d.name & "\")
                    End Sub)
            End If
        End If
        If contents._file IsNot Nothing Then
            If contents._file.Count > 0 Then
                Parallel.ForEach(contents._file,
                    Sub(f As ltfsindex.file)
                        If f IsNot Nothing Then
                            If Not f.Selected Then Exit Sub
                        End If
                        If f.extentinfo IsNot Nothing Then
                            If f.extentinfo.Count > 0 Then
                                SyncLock flist
                                    flist.Add(New TapeFileInfo With {.BlockNumber = f.extentinfo(0).startblock,
                                              .Partition = f.extentinfo(0).partition,
                                              .Path = ParentPath & f.name,
                                              .FileLength = f.length})
                                End SyncLock
                                Exit Sub
                            End If
                        End If
                        SyncLock flist
                            flist.Add(New TapeFileInfo With {.BlockNumber = 0,
                                  .Partition = ltfsindex.PartitionLabel.a,
                                  .Path = ParentPath & f.name,
                                  .FileLength = f.length})
                        End SyncLock
                    End Sub)
            End If
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If SaveFileDialog1.ShowDialog = DialogResult.OK Then
            My.Computer.FileSystem.WriteAllText(SaveFileDialog1.FileName, TextBox2.Text, False, New Text.UTF8Encoding(False))
        End If
    End Sub
    Public LoadComplete As Boolean = False
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TextBox1.Text = My.Settings.LastFile
        TextBox3.Text = My.Settings.Src
        TextBox4.Text = My.Settings.Dest
        CheckBox1.Checked = My.Settings.GenCMD
        Text = $"{FormTitle.Text} - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}{My.Settings.License}"
        LoadComplete = True
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        If schema Is Nothing Then Exit Sub
        Dim schfile As ltfsindex = schema.Clone()
        If FileBrowser.ShowDialog(schfile) = DialogResult.OK Then
            schema = schfile
        End If
        LoadSchemaFile(False)
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If Not LoadComplete Then Exit Sub
        LoadSchemaFile(False)
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        If schema Is Nothing Then Exit Sub
        Dim hw As New HashTaskWindow With {.schema = schema, .BaseDirectory = TextBox3.Text, .TargetDirectory = TextBox4.Text}
        hw.Show()
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.LastFile = TextBox1.Text
        My.Settings.Src = TextBox3.Text
        My.Settings.Dest = TextBox4.Text
        My.Settings.GenCMD = CheckBox1.Checked
        My.Settings.Save()
    End Sub
    Public Class IndexedDirectory
        Public LTFSIndexDir As ltfsindex.directory
        Public IO_Dir As IO.DirectoryInfo
        Public Sub New(index As ltfsindex.directory, dir As IO.DirectoryInfo)
            LTFSIndexDir = index
            IO_Dir = dir
        End Sub
    End Class
    Public Class ldirStack
        Private ldir As New List(Of ltfsindex.directory)
        Public ReadOnly Property IsEmpty
            Get
                Return ldir.Count = 0
            End Get
        End Property
        Public Sub Push(v As ltfsindex.directory)
            ldir.Add(v)
        End Sub
        Public Function Pop() As ltfsindex.directory
            Try
                Dim r As ltfsindex.directory = ldir.Last
                ldir.RemoveAt(ldir.Count - 1)
                Return r
            Catch ex As Exception
                Return Nothing
            End Try
        End Function
    End Class
    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        Try
            schema = New ltfsindex
            Dim fid As Integer = 0
            Dim RootDir As IO.DirectoryInfo = My.Computer.FileSystem.GetDirectoryInfo(TextBox3.Text)
            Dim BasePath As String = RootDir.Parent.FullName
            Dim q As New List(Of IndexedDirectory)
            q.Add(New IndexedDirectory(New ltfsindex.directory With {.name = RootDir.Name}, RootDir))
            schema._directory.Add(q(0).LTFSIndexDir)
            While q.Count > 0
                Dim qtmp As New List(Of IndexedDirectory)
                For Each d As IndexedDirectory In q
                    d.LTFSIndexDir.contents._file = New List(Of ltfsindex.file)
                    For Each f As IO.FileInfo In d.IO_Dir.GetFiles
                        Threading.Interlocked.Add(fid, 1)
                        d.LTFSIndexDir.contents._file.Add(New ltfsindex.file With {.name = f.Name, .length = f.Length, .extentinfo = New List(Of ltfsindex.file.extent)({New ltfsindex.file.extent With {.startblock = fid}})})
                    Next
                    For Each sd As IO.DirectoryInfo In d.IO_Dir.GetDirectories
                        Dim ld As New ltfsindex.directory With {.name = sd.Name}
                        d.LTFSIndexDir.contents._directory.Add(ld)
                        qtmp.Add(New IndexedDirectory(ld, sd))
                    Next
                Next
                q = qtmp
            End While
            Dim ds As New ldirStack
            fid = 0
            ds.Push(schema._directory(0))
            While Not ds.IsEmpty
                Dim currentdir As ltfsindex.directory = ds.Pop()
                For i As Integer = 0 To currentdir.contents._file.Count - 1
                    Threading.Interlocked.Add(fid, 1)
                    currentdir.contents._file(i).extentinfo(0).startblock = fid
                Next
                For i As Integer = currentdir.contents._directory.Count - 1 To 0 Step -1
                    ds.Push(currentdir.contents._directory(i))
                Next
            End While
            LoadSchemaFile(False)
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try
    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        If My.Computer.FileSystem.DirectoryExists(TextBox3.Text) Then FolderBrowserDialog1.SelectedPath = TextBox3.Text
        If FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            TextBox3.Text = FolderBrowserDialog1.SelectedPath
        End If
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        If My.Computer.FileSystem.DirectoryExists(TextBox4.Text) Then FolderBrowserDialog1.SelectedPath = TextBox4.Text
        If FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            TextBox4.Text = FolderBrowserDialog1.SelectedPath
        End If
    End Sub

    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
        Try
            Dim f() As IO.FileInfo = My.Computer.FileSystem.GetDirectoryInfo(TextBox1.Text).GetFiles("*.schema")
            For Each fl As IO.FileInfo In f
                Dim s As String = My.Computer.FileSystem.ReadAllText(fl.FullName)
                If s.Contains("XMLSchema") Then
                    schema = ltfsindex.FromXML(s)
                Else
                    schema = ltfsindex.FromSchemaText(s)
                End If
                schema.SaveFile(fl.FullName)
                'Dim tnew As String = schema.GetSerializedText
                'My.Computer.FileSystem.WriteAllText(fl.FullName, tnew, False, New System.Text.UTF8Encoding(False))
                TextBox2.AppendText(fl.FullName & vbCrLf)
            Next
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try

    End Sub

    Private Sub Form1_Click(sender As Object, e As EventArgs) Handles Me.Click
        Static q As Integer
        q += 1
        If q >= 10 Then
            Button9.Visible = True
        End If
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click
        LTFSConfigurator.Show()
    End Sub

    Private Sub 查找ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 查找ToolStripMenuItem.Click
        Dim patt As String = InputBox("Search kw", "Search", "")
        If patt <> "" Then
            Enabled = False
            Dim dir As String = TextBox1.Text.Substring(0, TextBox1.Text.LastIndexOf("\"))
            Dim result As New System.Text.StringBuilder

            If Not My.Computer.FileSystem.DirectoryExists(dir) Then Exit Sub
            Dim f() As IO.FileInfo = My.Computer.FileSystem.GetDirectoryInfo(dir).GetFiles("*.schema")
            Dim progmax As Integer = f.Length
            Dim progval As Integer = 0
            Dim th As New Threading.Thread(
                Sub()
                    Parallel.ForEach(Of IO.FileInfo)(f,
                        Sub(fl As IO.FileInfo)
                            Try
                                Dim sch As String = My.Computer.FileSystem.ReadAllText(fl.FullName)
                                If sch.Contains(patt) Then
                                    SyncLock result
                                        result.AppendLine(fl.Name)
                                    End SyncLock
                                End If
                            Catch ex As Exception

                            End Try
                            Threading.Interlocked.Increment(progval)
                        End Sub)
                    Invoke(Sub() Enabled = True)
                End Sub)
            Dim thprog As New Threading.Thread(
                Sub()
                    While True
                        Threading.Thread.Sleep(200)
                        Dim exitflag As Boolean = (progval >= progmax)
                        Me.Invoke(
                            Sub()
                                TextBox2.Text = "Search for " & patt & " in file "
                                TextBox2.AppendText(progval & "/" & progmax & vbCrLf)
                                SyncLock result
                                    TextBox2.AppendText(result.ToString)
                                End SyncLock
                            End Sub)
                        If exitflag Then Exit While
                    End While
                End Sub)
            th.Start()
            thprog.Start()
        End If
    End Sub

    Private Sub 错误检查ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 错误检查ToolStripMenuItem.Click
        Enabled = False
        Dim dir As String = TextBox1.Text.Substring(0, TextBox1.Text.LastIndexOf("\"))
        Dim result As New System.Text.StringBuilder

        If Not My.Computer.FileSystem.DirectoryExists(dir) Then Exit Sub
        Dim f() As IO.FileInfo = My.Computer.FileSystem.GetDirectoryInfo(dir).GetFiles("*.schema")
        Dim progmax As Integer = f.Length
        Dim progval As Integer = 0

        Dim th As New Threading.Thread(
            Sub()
                Parallel.ForEach(f,
                    Sub(fl As IO.FileInfo)
                        Try
                            Dim extlist As New List(Of ltfsindex.file.extent)
                            Dim sch As ltfsindex = ltfsindex.FromSchFile(fl.FullName)
                            ltfsindex.WSort(sch._directory,
                                            Sub(fid As ltfsindex.file)
                                                For Each ext As ltfsindex.file.extent In fid.extentinfo
                                                    ext.TempInfo = fid
                                                    extlist.Add(ext)
                                                Next
                                            End Sub, Nothing)
                            extlist.Sort(New Comparison(Of ltfsindex.file.extent)(Function(a As ltfsindex.file.extent, b As ltfsindex.file.extent) As Integer
                                                                                      If a.startblock <> b.startblock Then
                                                                                          Return a.startblock.CompareTo(b.startblock)
                                                                                      Else
                                                                                          Return a.byteoffset.CompareTo(b.byteoffset)
                                                                                      End If
                                                                                  End Function))
                            For i As Integer = 1 To extlist.Count - 1
                                If extlist(i).startblock * 524288 + extlist(i).byteoffset < extlist(i - 1).startblock * 524288 + extlist(i - 1).byteoffset + extlist(i - 1).bytecount Then
                                    result.AppendLine($"Error with {fl.Name}: fid {CType(extlist(i).TempInfo, ltfsindex.file).fileuid}")
                                End If
                            Next
                        Catch ex As Exception
                            result.Append(ex.ToString)
                        End Try
                        Threading.Interlocked.Increment(progval)
                    End Sub)
                Invoke(Sub() Enabled = True)
            End Sub)
        Dim thprog As New Threading.Thread(
            Sub()
                While True
                    Threading.Thread.Sleep(200)
                    Dim exitflag As Boolean = (progval >= progmax)
                    Me.Invoke(
                        Sub()
                            TextBox2.Text = "Checking files..."
                            TextBox2.AppendText(progval & "/" & progmax & vbCrLf)
                            SyncLock result
                                TextBox2.AppendText(result.ToString)
                            End SyncLock
                        End Sub)
                    If exitflag Then Exit While
                End While
            End Sub)
        th.Start()
        thprog.Start()
    End Sub

    Private Sub 合并文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 合并文件ToolStripMenuItem.Click
        Dim patt As String = InputBox("Search kw", "Search", "")
        If patt <> "" Then
            Enabled = False
            Dim dir As String = TextBox1.Text.Substring(0, TextBox1.Text.LastIndexOf("\"))
            Dim result As New ltfsindex
            result._directory = New List(Of ltfsindex.directory)
            result._directory.Add(New ltfsindex.directory With {.name = $"Search_{patt}"})
            Dim infoText As New System.Text.StringBuilder
            If Not My.Computer.FileSystem.DirectoryExists(dir) Then Exit Sub
            Dim f() As IO.FileInfo = My.Computer.FileSystem.GetDirectoryInfo(dir).GetFiles("*.schema")
            Dim progmax As Integer = f.Length
            Dim progval As Integer = 0
            Dim th As New Threading.Thread(
                Sub()
                    Parallel.ForEach(Of IO.FileInfo)(f,
                        Sub(fl As IO.FileInfo)
                            Try
                                Dim sch As String = My.Computer.FileSystem.ReadAllText(fl.FullName)
                                If sch.Contains(patt) Then
                                    SyncLock infoText
                                        infoText.AppendLine(fl.Name)
                                    End SyncLock
                                    Dim rsch As ltfsindex = ltfsindex.FromSchemaText(sch)
                                    Dim qf As New List(Of ltfsindex.directory)
                                    qf.AddRange(rsch._directory)
                                    While qf.Count > 0
                                        Dim qf2 As New List(Of ltfsindex.directory)
                                        For Each d As ltfsindex.directory In qf
                                            For Each fr As ltfsindex.file In d.contents._file
                                                fr.extendedattributes.Add(New ltfsindex.file.xattr With {.key = "Barcode", .value = fl.Name.Substring(0, fl.Name.Length - fl.Extension.Length)})
                                            Next
                                            qf2.AddRange(d.contents._directory)
                                        Next
                                        qf = qf2
                                    End While
                                    result._directory(0).contents._file.AddRange(rsch._directory(0).contents._file)
                                    result._directory(0).contents._directory.AddRange(rsch._directory(0).contents._directory)
                                End If
                            Catch ex As Exception

                            End Try
                            Threading.Interlocked.Increment(progval)
                        End Sub)
                    Dim q As New List(Of ltfsindex.directory)
                    q.Add(result._directory(0))
                    While q.Count > 0
                        Dim q2 As New List(Of ltfsindex.directory)
                        For Each d As ltfsindex.directory In q
                            With d.contents._directory
                                For i As Integer = .Count - 1 To 1 Step -1
                                    For j As Integer = 0 To i - 1
                                        If .ElementAt(i).name.Equals(.ElementAt(j).name) Then
                                            .ElementAt(j).contents._file.AddRange(.ElementAt(i).contents._file)
                                            .ElementAt(j).contents._directory.AddRange(.ElementAt(i).contents._directory)
                                            .RemoveAt(i)
                                            Exit For
                                        End If
                                    Next
                                Next
                            End With
                            q2.AddRange(d.contents._directory)
                        Next
                        q = q2
                    End While

                    result._directory(0).contents._directory.Sort(New Comparison(Of ltfsindex.directory)(
                                                                  Function(a As ltfsindex.directory, b As ltfsindex.directory) As Integer
                                                                      Return a.name.CompareTo(b.name)
                                                                  End Function))
                    result._directory(0).contents._file.Sort(New Comparison(Of ltfsindex.file)(
                                                             Function(a As ltfsindex.file, b As ltfsindex.file) As Integer
                                                                 If a.GetXAttr("Barcode") IsNot Nothing AndAlso
                                                                    b.GetXAttr("Barcode") IsNot Nothing AndAlso
                                                                    a.GetXAttr("Barcode") <> b.GetXAttr("Barcode") Then
                                                                     Return a.GetXAttr("Barcode").CompareTo(b.GetXAttr("Barcode"))
                                                                 End If
                                                                 Return a.name.CompareTo(b.name)
                                                             End Function))

                    schema = result
                    Invoke(Sub() Enabled = True)
                End Sub)
            Dim thprog As New Threading.Thread(
                Sub()
                    While True
                        Threading.Thread.Sleep(200)
                        Dim exitflag As Boolean = (progval >= progmax)
                        Me.Invoke(
                            Sub()
                                TextBox2.Text = "Search for " & patt & " in file "
                                TextBox2.AppendText(progval & "/" & progmax & vbCrLf)
                                SyncLock infoText
                                    TextBox2.AppendText(infoText.ToString)
                                End SyncLock
                            End Sub)
                        If exitflag Then Exit While
                    End While
                End Sub)
            th.Start()
            thprog.Start()
        End If
    End Sub

    Private Sub 未校验检查ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 未校验检查ToolStripMenuItem.Click
        Dim patt As String = InputBox("Search kw", "Search", "")
        If patt <> "" Then
            Enabled = False
            Dim dir As String = TextBox1.Text.Substring(0, TextBox1.Text.LastIndexOf("\"))
            Dim infoText As New System.Text.StringBuilder
            If Not My.Computer.FileSystem.DirectoryExists(dir) Then Exit Sub
            Dim f() As IO.FileInfo = My.Computer.FileSystem.GetDirectoryInfo(dir).GetFiles("*.schema")
            Dim progmax As Integer = f.Length
            Dim progval As Integer = 0
            Dim th As New Threading.Thread(
                Sub()
                    Parallel.ForEach(Of IO.FileInfo)(f,
                        Sub(fl As IO.FileInfo)
                            Try
                                Dim sch As String = My.Computer.FileSystem.ReadAllText(fl.FullName)
                                If sch.Contains(patt) Then
                                    Dim UNum As Integer = 0
                                    Dim result As New Text.StringBuilder
                                    result.AppendLine(fl.Name)
                                    Dim rsch As ltfsindex = ltfsindex.FromSchemaText(sch)
                                    Dim q As New List(Of ltfsindex.directory)
                                    q.AddRange(rsch._directory)
                                    While q.Count > 0
                                        Dim q2 As New List(Of ltfsindex.directory)
                                        For Each d As ltfsindex.directory In q
                                            For Each f2 As ltfsindex.file In d.contents._file
                                                If f2.sha1.Length <> 40 Then
                                                    result.AppendLine($"--[{f2.fileuid}]{f2.name}")
                                                    Threading.Interlocked.Increment(UNum)
                                                End If
                                            Next
                                            q2.AddRange(d.contents._directory)
                                        Next
                                        q = q2
                                    End While
                                    If UNum = 0 Then Exit Try
                                    SyncLock infoText
                                        infoText.AppendLine(result.ToString())
                                    End SyncLock
                                End If
                            Catch ex As Exception

                            End Try
                            Threading.Interlocked.Increment(progval)
                        End Sub)
                    Invoke(Sub() Enabled = True)
                End Sub)
            Dim thprog As New Threading.Thread(
                Sub()
                    While True
                        Threading.Thread.Sleep(200)
                        Dim exitflag As Boolean = (progval >= progmax)
                        Me.Invoke(
                            Sub()
                                TextBox2.Text = "Search for " & patt & " in file "
                                TextBox2.AppendText(progval & "/" & progmax & vbCrLf)
                                SyncLock infoText
                                    TextBox2.AppendText(infoText.ToString)
                                End SyncLock
                            End Sub)
                        If exitflag Then Exit While
                    End While
                End Sub)
            th.Start()
            thprog.Start()
        End If
    End Sub

    Private Sub 查看ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 查看ToolStripMenuItem.Click
        Dim LWF As New LTFSWriter With {.Barcode = "BROWSE", .TapeDrive = "", .OfflineMode = True}
        Dim OnLWFLoad As New EventHandler(Sub()
                                              LWF.Invoke(Sub()
                                                             LWF.schema = schema
                                                             LWF.RefreshDisplay()
                                                             LWF.ToolStripStatusLabel1.Text = "BROWSE"
                                                         End Sub)
                                              RemoveHandler LWF.Load, OnLWFLoad
                                          End Sub
            )
        AddHandler LWF.Load, OnLWFLoad
        LWF.Show()
    End Sub
End Class
