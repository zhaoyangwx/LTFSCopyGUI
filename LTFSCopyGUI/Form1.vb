Imports System.ComponentModel

Public Class Form1
    Public schema As ltfsindex
    Public contents As ltfsindex.contentsDef
    Public filelist As New List(Of String)
    Public Class TapeFileInfo
        Public Property Path As String
        Public Property Partition As ltfsindex.Partition.PartitionLabel
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
                    Invoke(Sub() Label4.Text = "1/7 正在加载...")
                    Dim s As String
                    If ReloadFile Or schema Is Nothing Then s = My.Computer.FileSystem.ReadAllText(TextBox1.Text)
                    Invoke(Sub() Label4.Text = "2/7 预处理...")
                    Invoke(Sub() Label4.Text = "3/7 解析文件...")

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
                    Invoke(Sub() Label4.Text = "4/7 读取分区信息...")
                    Parallel.ForEach(flist,
                        Sub(f As TapeFileInfo)
                            If f.Partition = ltfsindex.Partition.PartitionLabel.a Then
                                SyncLock alist
                                    alist.Add(f)
                                End SyncLock
                            Else
                                SyncLock blist
                                    blist.Add(f)
                                End SyncLock
                            End If
                        End Sub)
                    Invoke(Sub() Label4.Text = "5/7 文件排序中...")
                    Parallel.ForEach({alist, blist},
                        Sub(nlist As List(Of TapeFileInfo))
                            If nlist Is alist Then
                                alist.Sort(New Comparison(Of TapeFileInfo)(Function(a As TapeFileInfo, b As TapeFileInfo) As Integer
                                                                               Return a.BlockNumber.CompareTo(b.BlockNumber)
                                                                           End Function))
                            ElseIf nlist Is blist Then
                                blist.Sort(New Comparison(Of TapeFileInfo)(Function(a As TapeFileInfo, b As TapeFileInfo) As Integer
                                                                               Return a.BlockNumber.CompareTo(b.BlockNumber)
                                                                           End Function))
                            End If
                        End Sub)
                    filelist = New List(Of String)
                    Invoke(Sub() Label4.Text = "6/7 生成输出内容...")
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
                        p.Append("Partition" & vbTab & "Startblock" & vbTab & "Length" & vbTab & "Path" & vbCrLf)
                        For Each f As TapeFileInfo In alist
                            p.Append(f.Partition.ToString & vbTab & f.BlockNumber & vbTab & f.FileLength & vbTab & f.Path & vbCrLf)
                            filelist.Add(f.Path)
                            Threading.Interlocked.Increment(counter)
                            If counter Mod stepval = 0 Then
                                Invoke(Sub() Label4.Text = "6/7 生成输出内容..." & counter & "/" & total)
                                stepval = ran.Next(100, 1000)
                            End If

                        Next
                        For Each f As TapeFileInfo In blist
                            p.Append(f.Partition.ToString & vbTab & f.BlockNumber & vbTab & f.FileLength & vbTab & f.Path & vbCrLf)
                            filelist.Add(f.Path)
                            Threading.Interlocked.Increment(counter)
                            If counter Mod stepval = 0 Then
                                Invoke(Sub() Label4.Text = "6/7 生成输出内容..." & counter & "/" & total)
                                stepval = ran.Next(100, 1000)
                            End If
                        Next
                    Else
                        p.Append("chcp 65001" & vbCrLf)
                        For Each f As TapeFileInfo In alist
                            p.Append("echo f|xcopy /D /Y """ & fdir & f.Path & """ """ & tdir & f.Path & """" & vbCrLf)
                            filelist.Add(f.Path)
                            Threading.Interlocked.Increment(counter)
                            If counter Mod stepval = 0 Then
                                Invoke(Sub() Label4.Text = "6/7 生成输出内容..." & counter & "/" & total)
                                stepval = ran.Next(100, 1000)
                            End If
                        Next
                        For Each f As TapeFileInfo In blist
                            p.Append("echo f|xcopy /D /Y """ & fdir & f.Path & """ """ & tdir & f.Path & """" & vbCrLf)
                            filelist.Add(f.Path)
                            Threading.Interlocked.Increment(counter)
                            If counter Mod stepval = 0 Then
                                Invoke(Sub() Label4.Text = "6/7 生成输出内容..." & counter & "/" & total)
                                stepval = ran.Next(100, 1000)
                            End If
                        Next
                    End If
                    Invoke(Sub() Label4.Text = "7/7 完成...")
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
        Dim TargetBra As String = "<" & Target & ">"
        Dim TargetKet As String = "</" & Target & ">"
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
                                  .Partition = ltfsindex.Partition.PartitionLabel.a,
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
        Exit Sub
        Dim f As New Form With {.Height = 300, .Width = 800, .Text = "群主福利", .FormBorderStyle = FormBorderStyle.None, .StartPosition = FormStartPosition.CenterScreen}
        Dim p As New ProgressBar With {.Parent = f, .Maximum = 10000, .Value = 1, .Top = f.Height / 2, .Left = 80, .Width = f.Width - 160, .Height = 20}
        Dim l0 As New Label With {.Parent = f, .Top = 0, .Left = 0, .Text = f.Text}
        Dim l As New Label With {.Parent = f, .Top = f.Height / 4, .Text = "挂机100小时送5元优惠券", .AutoSize = True}
        l.Left = f.Width / 2 - l.Width
        l.Font = New Font(l.Font.FontFamily, l.Font.Size * 2)
        Dim l2 As New Label With {.Parent = f, .Top = p.Top, .Left = 10, .Text = "当前进度：0.01%"}
        l2.Top -= (p.Height - l2.Height)
        p.Left = l2.Left + l2.Width + 20
        Dim thprog As New Threading.Thread(
            Sub()
                Dim starttime As Date = Now
                While True
                    Threading.Thread.Sleep(100)
                    Dim prog As Decimal = (Now - starttime).TotalSeconds / (New TimeSpan(100, 0, 0)).TotalSeconds
                    f.Invoke(
                        Sub()
                            p.Value = Math.Min(p.Maximum, prog * p.Maximum)
                            l2.Text = "当前进度：" & Math.Round(prog * 100, 2) & "%"
                        End Sub)
                End While
            End Sub)
        thprog.Start()
        f.Show()
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.LastFile = TextBox1.Text
        My.Settings.Src = TextBox3.Text
        My.Settings.Dest = TextBox4.Text
        My.Settings.GenCMD = CheckBox1.Checked
        My.Settings.Save()
    End Sub
End Class
