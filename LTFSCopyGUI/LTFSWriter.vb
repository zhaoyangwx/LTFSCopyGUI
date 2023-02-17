Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.WindowsAPICodePack.Dialogs

Public Class LTFSWriter
    Public TapeDrive As String = "\\.\TAPE0"
    Public Property schema As ltfsindex
    Public Property plabel As ltfslabel
    Public Property Modified As Boolean = False
    Public Property IndexWriteInterval As Long = CLng(36) << 30
    Public Property TotalBytesUnindexed As Long = 0
    Public Property TotalBytesProcessed As Long = 0
    Public Property TotalFilesProcessed As Long = 0
    Public Property CurrentBytesProcessed As Long = 0
    Public Property CurrentFilesProcessed As Long = 0
    Public Property CurrentHeight As Long = 0
    Public Property ExtraPartitionCount As Long = 0

    Private ddelta, fdelta As Long
    Public SMaxNum As Integer = 600
    Public PMaxNum As Integer = 3600 * 6
    Public SpeedHistory As List(Of Double) = New Double(PMaxNum) {}.ToList()
    Public FileRateHistory As List(Of Double) = New Double(PMaxNum) {}.ToList()

    Public Sub PrintMsg(s As String)
        Me.Invoke(Sub() ToolStripStatusLabel3.Text = s)
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Static d_last As Long = 0
        Static t_last As Long = 0
        Try
            Dim pnow As Long = TotalBytesProcessed
            If pnow >= d_last Then
                ddelta = pnow - d_last
                d_last = pnow
            End If
            Dim tval As Long = TotalFilesProcessed
            If tval >= t_last Then
                fdelta = tval - t_last
                t_last = tval
            End If
            SpeedHistory.Add(ddelta / 1048576)
            FileRateHistory.Add(fdelta)
            While SpeedHistory.Count > PMaxNum
                SpeedHistory.RemoveAt(0)
            End While
            While FileRateHistory.Count > PMaxNum
                FileRateHistory.RemoveAt(0)
            End While


            Chart1.Series(0).Points.Clear()
            Dim i As Integer = 0
            For Each val As Double In SpeedHistory.GetRange(SpeedHistory.Count - SMaxNum, SMaxNum)
                Chart1.Series(0).Points.AddXY(i, val)
                i += 1
            Next
            Chart1.Series(1).Points.Clear()
            i = 0
            For Each val As Double In FileRateHistory.GetRange(FileRateHistory.Count - SMaxNum, SMaxNum)
                Chart1.Series(1).Points.AddXY(i, val)
                i += 1
            Next
            ToolStripStatusLabel4.Text = $"速度:{IOManager.FormatSize(ddelta)}/s  写队列数据:{ IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed))}/{IOManager.FormatSize(UnwrittenSize)}  未索引数据:{IOManager.FormatSize(TotalBytesUnindexed)}  累计处理:{IOManager.FormatSize(TotalBytesProcessed)}"
            ToolStripStatusLabel4.Text &= $"  处理文件数:{TotalFilesProcessed}  本次处理:{IOManager.FormatSize(CurrentBytesProcessed)}  本次处理文件数:{CurrentFilesProcessed}"
            Text = GetLocInfo()
        Catch ex As Exception
            PrintMsg(ex.ToString)
        End Try
    End Sub
    Public Class FileRecord
        Public ParentDirectory As ltfsindex.directory
        Public SourcePath As String
        Public File As ltfsindex.file
        Public Sub RemoveUnwritten()
            ParentDirectory.contents.UnwrittenFiles.Remove(File)
        End Sub
        Public Sub New()

        End Sub
        Public Sub New(Path As String, ParentDir As ltfsindex.directory)
            ParentDirectory = ParentDir
            SourcePath = Path
            Dim finf As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(SourcePath)
            File = New ltfsindex.file With {
                .name = finf.Name,
                .fileuid = -1,
                .length = finf.Length,
                .readonly = False,
                .openforwrite = False,
                .modifytime = finf.LastWriteTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                .creationtime = finf.CreationTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                .accesstime = finf.LastAccessTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                .changetime = .modifytime,
                .backuptime = Now.ToUniversalTime.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")}
            ParentDirectory.contents.UnwrittenFiles.Add(File)
        End Sub
    End Class

    Public Class IntLock
        Public Property Value As Integer = 0
        Public Sub Inc()
            SyncLock Me
                Threading.Interlocked.Increment(Value)
            End SyncLock
        End Sub
        Public Sub Dec()
            SyncLock Me
                Threading.Interlocked.Decrement(Value)
            End SyncLock
        End Sub
        Public Shared Widening Operator CType(n As Integer) As IntLock
            Return New IntLock With {.Value = n}
        End Operator
        Public Shared Operator >(a As IntLock, b As Integer) As Boolean
            Return a.Value > b
        End Operator
        Public Shared Operator <(a As IntLock, b As Integer) As Boolean
            Return a.Value < b
        End Operator
    End Class
    Public UFReadCount As IntLock = 0
    Public UnwrittenFiles As New List(Of FileRecord)
    Public ReadOnly Property UnwrittenSize
        Get
            If UnwrittenFiles Is Nothing Then Return 0
            Dim result As Long = 0

            UFReadCount.Inc()
            If UnwrittenFiles.Count > 0 Then
                For Each fr As FileRecord In UnwrittenFiles
                    result += fr.File.length
                Next
            End If
            UFReadCount.Dec()
            Return result
        End Get
    End Property

    Dim LastRefresh As Date = Now
    Public Function GetLocInfo() As String
        If schema Is Nothing Then Return "无索引"
        Dim info As String = ""
        Try
            SyncLock schema
                info = $"Generation {schema.generationnumber} - Partition {schema.location.partition} - Block {schema.location.startblock}"
                If schema.previousgenerationlocation IsNot Nothing Then
                    If schema.previousgenerationlocation.startblock > 0 Then info &= $" (Previous:Partition {schema.previousgenerationlocation.partition} - Block {schema.previousgenerationlocation.startblock})"
                End If
            End SyncLock
            If CurrentHeight > 0 Then info &= $" CurrentHeight {CurrentHeight}"
        Catch ex As Exception
            PrintMsg("获取位置出错")
        End Try
        Return info
    End Function
    Public Sub RefreshCapacity()
        Invoke(Sub()
                   Try
                       Dim cap0 As Integer = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 0).AsNumeric
                       Dim cap1 As Integer
                       If ExtraPartitionCount > 0 Then
                           cap1 = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 1).AsNumeric
                           ToolStripStatusLabel2.Text = $"可用空间 P0:{LTFSConfigurator.ReduceDataUnit(cap0)} P1:{LTFSConfigurator.ReduceDataUnit(cap1)}"
                       Else
                           ToolStripStatusLabel2.Text = $"可用空间 P0:{LTFSConfigurator.ReduceDataUnit(cap0)}"
                       End If
                       LastRefresh = Now
                   Catch ex As Exception
                       PrintMsg("获取容量失败")
                   End Try
               End Sub)

    End Sub
    Public Sub RefreshDisplay()
        Invoke(
            Sub()
                If schema Is Nothing Then Exit Sub
                Try
                    Dim old_select As ltfsindex.directory = Nothing
                    Dim new_select As TreeNode = Nothing
                    Dim IterDirectory As Action(Of ltfsindex.directory, TreeNode) =
                        Sub(dir As ltfsindex.directory, node As TreeNode)
                            SyncLock dir.contents._directory
                                For Each d As ltfsindex.directory In dir.contents._directory
                                    Dim t As New TreeNode
                                    t.Text = d.name
                                    t.Tag = d
                                    node.Nodes.Add(t)
                                    IterDirectory(d, t)
                                    If old_select Is d Then
                                        new_select = t
                                    End If
                                Next
                            End SyncLock

                        End Sub
                    If TreeView1.SelectedNode IsNot Nothing Then
                        If TreeView1.SelectedNode.Tag IsNot Nothing Then
                            old_select = TreeView1.SelectedNode.Tag
                        End If
                    End If
                    If old_select Is Nothing And ListView1.Tag IsNot Nothing Then
                        old_select = ListView1.Tag
                    End If
                    TreeView1.Nodes.Clear()
                    SyncLock schema._directory
                        For Each d As ltfsindex.directory In schema._directory
                            Dim t As New TreeNode
                            t.Text = d.name
                            t.Tag = d
                            TreeView1.Nodes.Add(t)
                            IterDirectory(d, t)
                        Next
                    End SyncLock
                    TreeView1.TopNode.Expand()
                    If new_select IsNot Nothing Then
                        TreeView1.SelectedNode = new_select
                        new_select.Expand()
                    Else
                        TreeView1.SelectedNode = TreeView1.TopNode
                    End If
                Catch ex As Exception

                End Try
                Try
                    Text = GetLocInfo()
                    ToolStripStatusLabel4.Text = $"未写入数据 {IOManager.FormatSize(UnwrittenSize)}"
                Catch ex As Exception
                    PrintMsg("刷新显示失败")
                End Try

            End Sub)
    End Sub
    Public Sub LockGUI(Optional ByVal Lock As Boolean = True)
        Invoke(Sub()
                   MenuStrip1.Enabled = Not Lock
                   ContextMenuStrip1.Enabled = Not Lock
                   ContextMenuStrip3.Enabled = Not Lock
               End Sub)
    End Sub
    Public Sub TriggerTreeView1Event()
        If TreeView1.SelectedNode IsNot Nothing Then
            If TreeView1.SelectedNode.Tag IsNot Nothing Then
                Try
                    If TypeOf (TreeView1.SelectedNode.Tag) Is ltfsindex.directory Then
                        Dim d As ltfsindex.directory = TreeView1.SelectedNode.Tag
                        ListView1.Items.Clear()
                        ListView1.Tag = d
                        SyncLock d.contents._file
                            For Each f As ltfsindex.file In d.contents._file
                                Dim li As New ListViewItem
                                li.Tag = f
                                li.Text = f.name
                                Dim s(14) As String
                                s(0) = f.length
                                s(1) = f.creationtime
                                s(2) = f.sha1
                                s(3) = f.fileuid
                                s(4) = f.openforwrite
                                s(5) = f.readonly
                                s(6) = f.changetime
                                s(7) = f.modifytime
                                s(8) = f.accesstime
                                s(9) = f.backuptime
                                If f.tag IsNot Nothing Then
                                    s(10) = f.tag.ToString()
                                Else
                                    s(10) = ""
                                End If
                                If f.extentinfo IsNot Nothing Then
                                    If f.extentinfo.Count > 0 Then
                                        Try
                                            s(11) = (f.extentinfo(0).startblock.ToString())
                                            s(12) = (f.extentinfo(0).partition.ToString())
                                        Catch ex As Exception
                                            s(11) = ("-")
                                            s(12) = ("-")
                                        End Try
                                    End If
                                Else
                                    s(11) = ("-")
                                    s(12) = ("-")
                                End If
                                s(13) = IOManager.FormatSize(f.length)
                                If f.WrittenBytes > 0 Then
                                    s(14) = (IOManager.FormatSize(f.WrittenBytes))
                                Else
                                    s(14) = ("-")
                                End If
                                For Each t As String In s
                                    li.SubItems.Add(t)
                                Next
                                ListView1.Items.Add(li)
                            Next

                        End SyncLock
                        SyncLock d.contents.UnwrittenFiles
                            For Each f As ltfsindex.file In d.contents.UnwrittenFiles
                                Dim li As New ListViewItem
                                SyncLock f
                                    li.Tag = f
                                    li.Text = f.name
                                    Dim s(14) As String
                                    s(0) = f.length
                                    s(1) = f.creationtime
                                    s(2) = f.sha1
                                    s(3) = f.fileuid
                                    s(4) = f.openforwrite
                                    s(5) = f.readonly
                                    s(6) = f.changetime
                                    s(7) = f.modifytime
                                    s(8) = f.accesstime
                                    s(9) = f.backuptime
                                    If f.tag IsNot Nothing Then
                                        s(10) = f.tag.ToString()
                                    Else
                                        s(10) = ""
                                    End If
                                    If f.extentinfo IsNot Nothing Then
                                        If f.extentinfo.Count > 0 Then
                                            Try
                                                s(11) = (f.extentinfo(0).startblock.ToString())
                                                s(12) = (f.extentinfo(0).partition.ToString())
                                            Catch ex As Exception
                                                s(11) = ("-")
                                                s(12) = ("-")
                                            End Try
                                        End If
                                    Else
                                        s(11) = ("-")
                                        s(12) = ("-")
                                    End If
                                    s(13) = IOManager.FormatSize(f.length)
                                    If f.WrittenBytes > 0 Then
                                        s(14) = (IOManager.FormatSize(f.WrittenBytes))
                                    Else
                                        s(14) = ("-")
                                    End If
                                    For Each t As String In s
                                        li.SubItems.Add(t)
                                    Next
                                End SyncLock
                                li.ForeColor = Color.Gray
                                ListView1.Items.Add(li)
                            Next
                        End SyncLock
                    End If
                Catch ex As Exception
                    PrintMsg("导航出错，请重试")
                End Try
            End If
        End If
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        TriggerTreeView1Event()
    End Sub
    Private Sub LTFSWriter_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            读取索引ToolStripMenuItem_Click(sender, e)
        Catch ex As Exception
            PrintMsg("获取分区信息出错")
        End Try
    End Sub

    Private Sub 读取索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 读取索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    PrintMsg("正在定位")
                    ExtraPartitionCount = TapeUtils.ModeSense(TapeDrive, &H11)(3)
                    TapeUtils.Locate(TapeDrive, 0, 0, TapeUtils.LocateDestType.Block)
                    Dim header As String = Encoding.ASCII.GetString(TapeUtils.ReadBlock(TapeDrive))
                    Dim VOL1LabelLegal As Boolean = False
                    VOL1LabelLegal = (header.Length = 80)
                    If VOL1LabelLegal Then VOL1LabelLegal = header.StartsWith("VOL1")
                    If VOL1LabelLegal Then VOL1LabelLegal = (header.Substring(24, 4) = "LTFS")
                    If Not VOL1LabelLegal Then
                        MessageBox.Show("非LTFS格式", "错误")
                        LockGUI(False)
                        Exit Sub
                    End If
                    TapeUtils.Locate(TapeDrive, 1, 0, TapeUtils.LocateDestType.File)
                    PrintMsg("正在读取LTFS信息")
                    TapeUtils.ReadBlock(TapeDrive)
                    Dim pltext As String = Encoding.UTF8.GetString(TapeUtils.ReadToFileMark(TapeDrive))
                    plabel = ltfslabel.FromXML(pltext)
                    TapeUtils.SetBlockSize(TapeDrive, plabel.blocksize)
                    Dim barcode As String = TapeUtils.ReadBarcode(TapeDrive)
                    PrintMsg("正在定位")
                    TapeUtils.Locate(TapeDrive, 3, 0, TapeUtils.LocateDestType.File)
                    TapeUtils.ReadBlock(TapeDrive)
                    Dim data As Byte()
                    If ExtraPartitionCount = 0 Then
                        TapeUtils.Locate(TapeDrive, 0, 0, TapeUtils.LocateDestType.EOD)
                        PrintMsg("正在读取索引")
                        Dim FM As Long = TapeUtils.ReadPosition(TapeDrive).FileNumber
                        If FM <= 1 Then
                            MessageBox.Show("非LTFS格式", "错误")
                            PrintMsg("索引读取失败")
                            LockGUI(False)
                            Exit Sub
                        End If
                        TapeUtils.Locate(TapeDrive, FM - 1, 0, TapeUtils.LocateDestType.File)
                        TapeUtils.ReadBlock(TapeDrive)
                    End If
                    PrintMsg("正在读取索引")
                    data = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim outputfile As String = "schema\LTFSIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, "schema")) Then
                        My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, "schema"))
                    End If
                    outputfile = My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    schema = ltfsindex.FromSchemaText(My.Computer.FileSystem.ReadAllText(outputfile))
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            Exit While
                        End SyncLock
                    End While
                    Modified = False
                    Me.Invoke(Sub()
                                  Text = barcode
                                  ToolStripStatusLabel1.Text = barcode.TrimEnd(" ")
                                  RefreshDisplay()
                                  RefreshCapacity()
                              End Sub)
                    CurrentHeight = -1
                    PrintMsg("索引读取成功")
                Catch ex As Exception
                    PrintMsg("索引读取失败")
                End Try
                LockGUI(False)
            End Sub)
        LockGUI()
        th.Start()
    End Sub

    Private Sub 重命名ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 重命名文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing Then
            If ListView1.SelectedItems IsNot Nothing Then
                If ListView1.SelectedItems.Count > 0 Then
                    If ListView1.SelectedItems.Item(0).Tag IsNot Nothing Then
                        If TypeOf (ListView1.SelectedItems.Item(0).Tag) Is ltfsindex.file Then
                            Dim f As ltfsindex.file = ListView1.SelectedItems.Item(0).Tag
                            Dim d As ltfsindex.directory = ListView1.Tag
                            Dim newname As String = InputBox("新文件名", "重命名", f.name)
                            If newname = f.name Then Exit Sub
                            If (newname.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) Then
                                MessageBox.Show("文件名存在非法字符")
                                Exit Sub
                            End If
                            SyncLock d.contents._file
                                For Each allf As ltfsindex.file In d.contents._file
                                    If allf IsNot f And allf.name.ToLower = newname.ToLower Then
                                        MessageBox.Show("存在重名文件")
                                        Exit Sub
                                    End If
                                Next
                            End SyncLock
                            f.name = newname
                            RefreshDisplay()
                        End If
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub 删除文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 删除文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing Then
            If ListView1.SelectedItems IsNot Nothing Then
                If ListView1.SelectedItems.Count > 0 Then
                    If MessageBox.Show($"确认删除{ListView1.SelectedItems.Count}个文件？", "警告", MessageBoxButtons.OKCancel) = DialogResult.OK Then
                        SyncLock ListView1.SelectedItems
                            For Each ItemSelected As ListViewItem In ListView1.SelectedItems
                                If ItemSelected.Tag IsNot Nothing Then
                                    If TypeOf (ItemSelected.Tag) Is ltfsindex.file Then
                                        Dim f As ltfsindex.file = ListView1.SelectedItems.Item(0).Tag
                                        Dim d As ltfsindex.directory = ListView1.Tag
                                        If d.contents.UnwrittenFiles.Contains(f) Then
                                            While True
                                                Threading.Thread.Sleep(0)
                                                SyncLock UFReadCount
                                                    If UFReadCount > 0 Then Continue While
                                                    For Each fr As FileRecord In UnwrittenFiles
                                                        If fr.File Is f Then
                                                            fr.RemoveUnwritten()
                                                            UnwrittenFiles.Remove(fr)
                                                            RefreshDisplay()
                                                            Exit Sub
                                                        End If
                                                    Next
                                                    Exit While
                                                End SyncLock
                                            End While
                                        End If
                                        If d.contents._file.Contains(f) Then
                                            d.contents._file.Remove(f)
                                            RefreshDisplay()
                                        End If
                                    End If
                                End If
                            Next

                        End SyncLock
                    End If
                End If
            End If
        End If
    End Sub

    Private Sub 添加文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 添加文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing Then
            If OpenFileDialog1.ShowDialog = DialogResult.OK Then
                Dim d As ltfsindex.directory = ListView1.Tag
                For Each fpath As String In OpenFileDialog1.FileNames
                    Dim f As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(fpath)
                    Dim fexist As Boolean = False
                    SyncLock d.contents._file
                        For Each oldf As ltfsindex.file In d.contents._file
                            If oldf.name.ToLower = f.Name.ToLower Then
                                d.contents._file.Remove(oldf)
                                fexist = True
                                Exit For
                            End If
                        Next
                    End SyncLock

                    If Not fexist Then
                        While True
                            Threading.Thread.Sleep(0)
                            SyncLock UFReadCount
                                If UFReadCount > 0 Then Continue While
                                For Each oldf As FileRecord In UnwrittenFiles
                                    If oldf.File.name.ToLower = f.Name.ToLower Then
                                        oldf.ParentDirectory.contents.UnwrittenFiles.Remove(oldf.File)
                                        UnwrittenFiles.Remove(oldf)
                                        fexist = True
                                        Exit For
                                    End If
                                Next
                                Exit While
                            End SyncLock
                        End While

                    End If
                    Dim frnew As New FileRecord(f.FullName, d)
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Add(frnew)
                            Exit While
                        End SyncLock
                    End While
                Next
                RefreshDisplay()
            End If
        End If
    End Sub
    Private Sub 导入文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 导入文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing Then
            If FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
                Dim dnew As IO.DirectoryInfo = My.Computer.FileSystem.GetDirectoryInfo(FolderBrowserDialog1.SelectedPath)
                Dim d As ltfsindex.directory = ListView1.Tag
                Dim ConcatDirectory As Action(Of IO.DirectoryInfo, ltfsindex.directory) =
                        Sub(dnew1 As IO.DirectoryInfo, d1 As ltfsindex.directory)
                            For Each f As IO.FileInfo In dnew1.GetFiles()
                                SyncLock d1.contents._file
                                    For Each fe As ltfsindex.file In d1.contents._file
                                        If fe.name = f.Name Then
                                            d1.contents._file.Remove(fe)
                                        End If
                                    Next
                                End SyncLock
                                While True
                                    Threading.Thread.Sleep(0)
                                    SyncLock UFReadCount
                                        If UFReadCount > 0 Then Continue While
                                        UnwrittenFiles.Add(New FileRecord(f.FullName, d1))
                                        Exit While
                                    End SyncLock
                                End While
                            Next
                            For Each dn As IO.DirectoryInfo In dnew1.GetDirectories()
                                Dim dirExist As Boolean = False
                                Dim dT As ltfsindex.directory = Nothing
                                SyncLock d1.contents._directory
                                    For Each fe As ltfsindex.directory In d1.contents._directory
                                        If fe.name = dn.Name Then
                                            dirExist = True
                                            dT = fe
                                            Exit For
                                        End If
                                    Next
                                End SyncLock

                                If Not dirExist Then
                                    dT = New ltfsindex.directory With {
                                            .name = dn.Name,
                                            .creationtime = dn.CreationTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                                            .fileuid = schema.highestfileuid + 1,
                                            .accesstime = dn.LastAccessTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                                            .modifytime = dn.LastWriteTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                                            .changetime = .modifytime,
                                            .backuptime = Now.ToUniversalTime.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                                            .readonly = False
                                            }
                                    d1.contents._directory.Add(dT)
                                    schema.highestfileuid += 1
                                End If
                                ConcatDirectory(dn, dT)
                            Next
                        End Sub
                ConcatDirectory(dnew, d)
                RefreshDisplay()
            End If
        End If

    End Sub

    Private Sub 添加目录ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 添加目录ToolStripMenuItem.Click

        If ListView1.Tag IsNot Nothing Then
            Dim COFD As New CommonOpenFileDialog
            COFD.Multiselect = True
            COFD.IsFolderPicker = True
            If COFD.ShowDialog = CommonFileDialogResult.Ok Then
                For Each dirSelected As String In COFD.FileNames
                    Dim dnew As IO.DirectoryInfo = My.Computer.FileSystem.GetDirectoryInfo(dirSelected)
                    Dim d As ltfsindex.directory = ListView1.Tag
                    Dim ConcatDirectory As Action(Of IO.DirectoryInfo, ltfsindex.directory) =
                        Sub(dnew1 As IO.DirectoryInfo, d1 As ltfsindex.directory)

                            Dim dirExist As Boolean = False
                            Dim dT As ltfsindex.directory = Nothing
                            SyncLock d1.contents._directory
                                For Each fe As ltfsindex.directory In d1.contents._directory
                                    If fe.name = dnew1.Name Then
                                        dirExist = True
                                        dT = fe
                                        Exit For
                                    End If
                                Next
                            End SyncLock

                            If Not dirExist Then
                                dT = New ltfsindex.directory With {
                                        .name = dnew1.Name,
                                        .creationtime = dnew1.CreationTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                                        .fileuid = schema.highestfileuid + 1,
                                        .accesstime = dnew1.LastAccessTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                                        .modifytime = dnew1.LastWriteTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                                        .changetime = .modifytime,
                                        .backuptime = Now.ToUniversalTime.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                                        .readonly = False
                                        }
                                d1.contents._directory.Add(dT)
                                schema.highestfileuid += 1
                            End If
                            For Each f As IO.FileInfo In dnew1.GetFiles()
                                SyncLock dT.contents._file
                                    For Each fe As ltfsindex.file In dT.contents._file
                                        If fe.name = f.Name Then
                                            dT.contents._file.Remove(fe)
                                        End If
                                    Next
                                End SyncLock
                                While True
                                    Threading.Thread.Sleep(0)
                                    SyncLock UFReadCount
                                        If UFReadCount > 0 Then Continue While
                                        UnwrittenFiles.Add(New FileRecord(f.FullName, dT))
                                        Exit While
                                    End SyncLock
                                End While
                            Next
                            For Each dn As IO.DirectoryInfo In dnew1.GetDirectories()
                                ConcatDirectory(dn, dT)
                            Next
                        End Sub
                    ConcatDirectory(dnew, d)
                    RefreshDisplay()
                Next
            End If
        End If
    End Sub

    Private Sub 新建目录ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 新建目录ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing Then
            Dim s As String = InputBox("目录名", "新建目录", "")
            If s <> "" Then
                If (s.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) Then
                    MessageBox.Show("目录名存在非法字符")
                    Exit Sub
                End If
                Dim d As ltfsindex.directory = ListView1.Tag
                SyncLock d.contents._directory
                    For Each dold As ltfsindex.directory In d.contents._directory
                        If dold IsNot d And dold.name = s Then
                            MessageBox.Show("存在重名目录")
                            Exit Sub
                        End If
                    Next
                End SyncLock

                Dim newdir As New ltfsindex.directory With {
                    .name = s,
                    .creationtime = Now.ToUniversalTime.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z"),
                    .fileuid = schema.highestfileuid + 1,
                    .backuptime = .creationtime,
                    .accesstime = .creationtime,
                    .changetime = .creationtime,
                    .modifytime = .creationtime,
                    .readonly = False
                    }
                schema.highestfileuid += 1
                d.contents._directory.Add(newdir)
                RefreshDisplay()
            End If
        End If
    End Sub

    Public Sub DeleteDir()
        If TreeView1.SelectedNode IsNot Nothing Then
            Dim d As ltfsindex.directory = TreeView1.SelectedNode.Tag
            If MessageBox.Show($"是否删除{d.name}") = DialogResult.OK Then
                Dim pd As ltfsindex.directory = TreeView1.SelectedNode.Parent.Tag
                pd.contents._directory.Remove(d)
                Dim IterAllDirectory As Action(Of ltfsindex.directory) =
                    Sub(d1 As ltfsindex.directory)
                        Dim RList As New List(Of FileRecord)
                        SyncLock d1.contents.UnwrittenFiles
                            For Each f As ltfsindex.file In d1.contents.UnwrittenFiles
                                UFReadCount.Inc()
                                For Each fr As FileRecord In UnwrittenFiles
                                    If fr.File Is f Then
                                        RList.Add(fr)
                                    End If
                                Next
                                UFReadCount.Dec()
                            Next
                        End SyncLock

                        For Each fr As FileRecord In RList
                            While True
                                Threading.Thread.Sleep(0)
                                SyncLock UFReadCount
                                    If UFReadCount > 0 Then Continue While
                                    UnwrittenFiles.Remove(fr)
                                    Exit While
                                End SyncLock
                            End While
                        Next
                        SyncLock d1.contents._directory
                            For Each d2 As ltfsindex.directory In d1.contents._directory
                                IterAllDirectory(d2)
                            Next
                        End SyncLock

                    End Sub
                IterAllDirectory(d)
                RefreshDisplay()
            End If
        End If

    End Sub
    Private Sub 删除目录ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 删除目录ToolStripMenuItem.Click
        DeleteDir()
    End Sub
    Public Sub RenameDir()
        If TreeView1.SelectedNode IsNot Nothing Then
            Dim d As ltfsindex.directory = TreeView1.SelectedNode.Tag
            Dim s As String = InputBox("目录名", "重命名目录", d.name)
            If s <> "" Then
                If s = d.name Then Exit Sub
                If (s.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) Then
                    MessageBox.Show("目录名存在非法字符")
                    Exit Sub
                End If
                If TreeView1.SelectedNode.Parent IsNot Nothing Then
                    Dim pd As ltfsindex.directory = TreeView1.SelectedNode.Parent.Tag
                    SyncLock pd.contents._directory
                        For Each d2 As ltfsindex.directory In pd.contents._directory
                            If d2 IsNot d And d2.name = s Then
                                MessageBox.Show("存在重名目录")
                                Exit Sub
                            End If
                        Next
                    End SyncLock
                End If
                d.name = s
                RefreshDisplay()
            End If
        End If

    End Sub
    Private Sub 重命名目录ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 重命名目录ToolStripMenuItem.Click
        RenameDir()
    End Sub
    Public Sub CheckUnindexedDataSizeLimit(Optional ByVal ForceFlush As Boolean = False)
        If TotalBytesUnindexed >= IndexWriteInterval Or ForceFlush Then
            WriteCurrentIndex(False)
            TotalBytesUnindexed = 0
            Invoke(Sub() Text = GetLocInfo())
        End If
    End Sub

    Public Sub WriteCurrentIndex(Optional ByVal GotoEOD As Boolean = True)
        If GotoEOD Then TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.EOD)
        TapeUtils.WriteFileMark(TapeDrive)
        schema.generationnumber += 1
        schema.updatetime = Now.ToUniversalTime.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
        schema.location.partition = ltfsindex.PartitionLabel.b
        schema.previousgenerationlocation = New ltfsindex.PartitionDef With {.partition = schema.location.partition, .startblock = schema.location.startblock}
        schema.location.startblock = TapeUtils.ReadPosition(TapeDrive).BlockNumber
        Dim sdata As Byte() = Encoding.UTF8.GetBytes(schema.GetSerializedText())
        While sdata.Length > 0
            Dim wdata As Byte() = sdata.Take(Math.Min(plabel.blocksize, sdata.Length)).ToArray
            sdata = sdata.Skip(Math.Min(plabel.blocksize, sdata.Length)).ToArray()
            TapeUtils.Write(TapeDrive, wdata)
            If sdata.Length = 0 Then Exit While
        End While
        TotalBytesUnindexed = 0
        TapeUtils.WriteFileMark(TapeDrive)
        CurrentHeight = TapeUtils.ReadPosition(TapeDrive).BlockNumber
    End Sub
    Public Sub RefreshIndexPartition()
        Dim block1 As Long = schema.location.startblock
        If ExtraPartitionCount > 0 Then
            TapeUtils.Locate(TapeDrive, 3, 0, TapeUtils.LocateDestType.File)
            TapeUtils.WriteFileMark(TapeDrive)
            If schema.location.partition = ltfsindex.PartitionLabel.b Then
                schema.previousgenerationlocation = New ltfsindex.PartitionDef With {.partition = schema.location.partition, .startblock = schema.location.startblock}
            End If
            schema.location.startblock = TapeUtils.ReadPosition(TapeDrive).BlockNumber
        End If
        'schema.previousgenerationlocation.partition = ltfsindex.PartitionLabel.b
        Dim block0 As Long = schema.location.startblock
        If ExtraPartitionCount > 0 Then
            schema.location.partition = ltfsindex.PartitionLabel.a
            Dim sdata As Byte() = Encoding.UTF8.GetBytes(schema.GetSerializedText())
            While sdata.Length > 0
                Dim wdata As Byte() = sdata.Take(Math.Min(plabel.blocksize, sdata.Length)).ToArray
                sdata = sdata.Skip(Math.Min(plabel.blocksize, sdata.Length)).ToArray()
                TapeUtils.Write(TapeDrive, wdata)
            End While
            TapeUtils.WriteFileMark(TapeDrive)
        End If
        TapeUtils.WriteVCI(TapeDrive, schema.generationnumber, block0, block1, schema.volumeuuid.ToString(), ExtraPartitionCount)
    End Sub

    Private Sub 更新索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 更新索引ToolStripMenuItem.Click

        Dim th As New Threading.Thread(
                Sub()
                    Try
                        If TotalBytesUnindexed > 0 Then
                            PrintMsg("正在更新数据区索引")
                            WriteCurrentIndex()
                        End If
                        PrintMsg("正在更新索引")
                        RefreshIndexPartition()
                        TapeUtils.ReleaseUnit(TapeDrive)
                        TapeUtils.AllowMediaRemoval(TapeDrive)
                        PrintMsg("")
                    Catch ex As Exception
                        PrintMsg("索引更新出错")
                    End Try
                    Invoke(Sub()
                               LockGUI(False)
                               RefreshDisplay()
                               MessageBox.Show("现在可以安全弹出了")
                           End Sub)
                End Sub)
        LockGUI(True)
        th.Start()
    End Sub

    Private Sub ToolStripStatusLabel2_Click(sender As Object, e As EventArgs) Handles ToolStripStatusLabel2.Click
        Try
            RefreshCapacity()
            PrintMsg("")
        Catch ex As Exception
            PrintMsg("可用容量刷新失败")
        End Try

    End Sub
    Public StopFlag As Boolean = False
    Private Sub 写入数据ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 写入数据ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    PrintMsg("准备写入")
                    TapeUtils.ReserveUnit(TapeDrive)
                    TapeUtils.PreventMediaRemoval(TapeDrive)
                    If schema.location.partition = ltfsindex.PartitionLabel.a Then
                        TapeUtils.Locate(TapeDrive, schema.previousgenerationlocation.startblock, schema.previousgenerationlocation.partition, TapeUtils.LocateDestType.Block)
                        schema.location.startblock = schema.previousgenerationlocation.startblock
                        schema.location.partition = schema.previousgenerationlocation.partition
                        Dim sch2 As ltfsindex = ltfsindex.FromSchemaText(Encoding.UTF8.GetString(TapeUtils.ReadToFileMark(TapeDrive)))
                        schema.previousgenerationlocation = sch2.previousgenerationlocation
                        CurrentHeight = TapeUtils.ReadPosition(TapeDrive).BlockNumber
                        Invoke(Sub() Text = GetLocInfo())
                    ElseIf CurrentHeight > 0 Then
                        If TapeUtils.ReadPosition(TapeDrive).BlockNumber <> CurrentHeight Then
                            TapeUtils.Locate(TapeDrive, CurrentHeight, 1, TapeUtils.LocateDestType.Block)
                        End If
                    End If
                    UFReadCount.Inc()
                    CurrentFilesProcessed = 0
                    CurrentBytesProcessed = 0
                    For Each fr As FileRecord In UnwrittenFiles
                        Dim finfo As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(fr.SourcePath)
                        fr.File.fileuid = schema.highestfileuid + 1
                        schema.highestfileuid += 1
                        If finfo.Length > 0 Then
                            Dim fileextent As New ltfsindex.file.extent With
                            {.partition = ltfsindex.PartitionLabel.b,
                            .startblock = TapeUtils.ReadPosition(TapeDrive).BlockNumber,
                            .bytecount = finfo.Length,
                            .byteoffset = 0,
                            .fileoffset = 0}
                            fr.File.extentinfo.Add(fileextent)
                            PrintMsg($"正在写入 {fr.File.name}  大小 {IOManager.FormatSize(fr.File.length)}")
                            'write to tape
                            TapeUtils.SetBlockSize(TapeDrive, plabel.blocksize)
                            If finfo.Length <= plabel.blocksize Then
                                Dim sense As Byte() = TapeUtils.Write(TapeDrive, My.Computer.FileSystem.ReadAllBytes(fr.SourcePath))
                                If ((sense(2) >> 6) And &H1) = 1 Then
                                    If sense(2) And &HF = 13 Then
                                        PrintMsg("磁带已满")
                                        MessageBox.Show("磁带已满")
                                        StopFlag = True
                                        Exit For
                                    Else
                                        PrintMsg($"磁带即将写满")
                                    End If
                                End If
                                fr.File.WrittenBytes += finfo.Length
                                TotalBytesProcessed += finfo.Length
                                CurrentBytesProcessed += finfo.Length
                                TotalFilesProcessed += 1
                                CurrentFilesProcessed += 1
                                TotalBytesUnindexed += finfo.Length
                                CheckUnindexedDataSizeLimit()
                            Else
                                Dim fs As New IO.FileStream(fr.SourcePath, IO.FileMode.Open)
                                Dim buffer(plabel.blocksize - 1) As Byte
                                Dim wBufferPtr As IntPtr = Marshal.AllocHGlobal(plabel.blocksize)
                                While Not StopFlag
                                    Dim BytesReaded As Integer = fs.Read(buffer, 0, plabel.blocksize)
                                    If BytesReaded > 0 Then
                                        Marshal.Copy(buffer, 0, wBufferPtr, BytesReaded)
                                        Dim sense As Byte() = TapeUtils.Write(TapeDrive, wBufferPtr, BytesReaded, BytesReaded < plabel.blocksize)
                                        If ((sense(2) >> 6) And &H1) = 1 Then
                                            If sense(2) And &HF = 13 Then
                                                PrintMsg("磁带已满")
                                                MessageBox.Show("磁带已满")
                                                StopFlag = True
                                                Exit For
                                            Else
                                                PrintMsg("磁带即将写满   正在写入 {fr.File.name}  大小 {IOManager.FormatSize(fr.File.length)}")
                                            End If
                                        End If
                                        fr.File.WrittenBytes += BytesReaded
                                        TotalBytesProcessed += BytesReaded
                                        CurrentBytesProcessed += BytesReaded
                                        TotalBytesUnindexed += BytesReaded
                                    Else
                                        Exit While
                                    End If
                                End While

                                Marshal.FreeHGlobal(wBufferPtr)
                                TotalFilesProcessed += 1
                                CurrentFilesProcessed += 1
                                CheckUnindexedDataSizeLimit()
                            End If
                            CurrentHeight = TapeUtils.ReadPosition(TapeDrive).BlockNumber
                        Else
                            TotalBytesUnindexed += 1
                            TotalFilesProcessed += 1
                        End If
                        If StopFlag Then Exit For
                        'mark as written
                        fr.ParentDirectory.contents._file.Add(fr.File)
                        fr.ParentDirectory.contents.UnwrittenFiles.Remove(fr.File)
                        Invoke(Sub()
                                   If (Now - LastRefresh).TotalSeconds > 10 Then RefreshCapacity()
                               End Sub)
                    Next
                    UFReadCount.Dec()
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            Exit While
                        End SyncLock
                    End While
                Catch ex As Exception
                    PrintMsg("写入出错")
                End Try
                TapeUtils.ReleaseUnit(TapeDrive)
                TapeUtils.AllowMediaRemoval(TapeDrive)
                Invoke(Sub()
                           LockGUI(False)
                           RefreshDisplay()
                           RefreshCapacity()
                           If MessageBox.Show("写入完成，是否更新数据区索引？（推荐）", "操作成功成功", MessageBoxButtons.OKCancel) = DialogResult.OK Then
                               更新数据区索引ToolStripMenuItem_Click(sender, e)
                           End If
                       End Sub)
            End Sub)
        StopFlag = False
        LockGUI()
        th.Start()
    End Sub

    Private Sub 放弃未索引数据ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 放弃未索引数据ToolStripMenuItem.Click
        If MessageBox.Show("未索引的数据可能丢失，是否继续", "警告", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
            Exit Sub
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    PrintMsg("正在定位索引")
                    TapeUtils.Locate(TapeDrive, schema.location.startblock, schema.location.partition, TapeUtils.LocateDestType.Block)
                    Dim data As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim outputfile As String = "LTFSIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    schema = ltfsindex.FromSchemaText(My.Computer.FileSystem.ReadAllText(outputfile))
                    Modified = False
                    CurrentHeight = TapeUtils.ReadPosition(TapeDrive).BlockNumber
                    If ExtraPartitionCount = 0 Then
                        TapeUtils.Write(TapeDrive, {0})
                        TapeUtils.Locate(TapeDrive, CurrentHeight, 0, TapeUtils.LocateDestType.Block)
                    End If
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            Exit While
                        End SyncLock
                    End While
                    Me.Invoke(Sub()
                                  RefreshDisplay()
                                  RefreshCapacity()
                              End Sub)
                Catch ex As Exception
                    PrintMsg("读取失败")
                End Try
                PrintMsg("已回到索引位置")
                Me.Invoke(Sub()
                              LockGUI(False)
                              Text = GetLocInfo()
                          End Sub)
            End Sub)
        LockGUI()
        th.Start()
    End Sub

    Private Sub 回滚ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 回滚ToolStripMenuItem.Click
        If MessageBox.Show("未索引的数据将丢失，且回滚后一旦写入将丢失后面数据，是否继续", "警告", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
            Exit Sub
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    PrintMsg("正在回滚")
                    Dim genbefore As Integer = schema.generationnumber
                    Dim prevpart As ltfsindex.PartitionLabel = schema.previousgenerationlocation.partition
                    Dim prevblk As Long = schema.previousgenerationlocation.startblock
                    TapeUtils.Locate(TapeDrive, schema.previousgenerationlocation.startblock, schema.previousgenerationlocation.partition, TapeUtils.LocateDestType.Block)
                    Dim data As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim outputfile As String = "LTFSIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    schema = ltfsindex.FromSchemaText(My.Computer.FileSystem.ReadAllText(outputfile))
                    Modified = False
                    CurrentHeight = TapeUtils.ReadPosition(TapeDrive).BlockNumber
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            Exit While
                        End SyncLock
                    End While
                    Me.Invoke(Sub()
                                  PrintMsg($"gen{genbefore}->{schema.generationnumber}: p{prevpart} block{prevblk}->p{schema.location.partition} block{schema.location.startblock}")
                                  RefreshDisplay()
                                  RefreshCapacity()
                              End Sub)
                Catch ex As Exception
                    PrintMsg("读取失败")
                End Try
                Me.Invoke(Sub()
                              LockGUI(False)
                              Text = GetLocInfo()
                              PrintMsg("回滚完成")
                          End Sub)
            End Sub)
        LockGUI()
        th.Start()
    End Sub

    Private Sub ToolStripSplitButton1_ButtonClick(sender As Object, e As EventArgs) Handles ToolStripSplitButton1.ButtonClick
        If MessageBox.Show("此时取消会丢失未写完的文件，是否继续", "警告", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            StopFlag = True
        End If
    End Sub

    Private Sub 更新数据区索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 更新数据区索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
        Sub()
            Try
                If TotalBytesUnindexed > 0 Then
                    WriteCurrentIndex()
                End If
            Catch ex As Exception

            End Try
            Invoke(Sub()
                       LockGUI(False)
                       RefreshDisplay()
                       MessageBox.Show("数据区索引已更新")
                   End Sub)
        End Sub)
        LockGUI()
        th.Start()
    End Sub

    Private Sub LTFSWriter_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If TotalBytesUnindexed > 0 Then
            If MessageBox.Show("未索引的数据将会丢失，是否继续", "警告", MessageBoxButtons.YesNo) = DialogResult.No Then
                e.Cancel = True
            Else
                e.Cancel = False
                UFReadCount.Value = 0
            End If
        End If
    End Sub

    Private Sub SToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SToolStripMenuItem.Click
        SMaxNum = 60
        Chart1.Titles(0).Text = "60秒"
    End Sub

    Private Sub MinToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MinToolStripMenuItem.Click
        SMaxNum = 300
        Chart1.Titles(0).Text = "5分钟"
    End Sub

    Private Sub MinToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles MinToolStripMenuItem1.Click
        SMaxNum = 600
        Chart1.Titles(0).Text = "10分钟"
    End Sub

    Private Sub MinToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles MinToolStripMenuItem2.Click
        SMaxNum = 1800
        Chart1.Titles(0).Text = "30分钟"
    End Sub

    Private Sub HToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem.Click
        SMaxNum = 3600
        Chart1.Titles(0).Text = "1小时"
    End Sub

    Private Sub HToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem1.Click
        SMaxNum = 3600 * 3
        Chart1.Titles(0).Text = "3小时"
    End Sub

    Private Sub HToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem2.Click
        SMaxNum = 3600 * 6
        Chart1.Titles(0).Text = "6小时"
    End Sub

    Private Sub LinearToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LinearToolStripMenuItem.Click
        Chart1.ChartAreas(0).AxisY.IsLogarithmic = False
        LinearToolStripMenuItem.Checked = True
        LogrithmToolStripMenuItem.Checked = False
    End Sub

    Private Sub LogrithmToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LogrithmToolStripMenuItem.Click
        Chart1.ChartAreas(0).AxisY.IsLogarithmic = True
        LinearToolStripMenuItem.Checked = False
        LogrithmToolStripMenuItem.Checked = True
    End Sub

    Private Sub ToolStripStatusLabel4_Click(sender As Object, e As EventArgs) Handles ToolStripStatusLabel4.Click
        If MessageBox.Show("确定要清零写入计数？", "确认", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            TotalBytesProcessed = 0
            TotalFilesProcessed = 0
        End If
    End Sub

    Private Sub 加载外部索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 加载外部索引ToolStripMenuItem.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            Try
                Dim sch2 As ltfsindex = ltfsindex.FromSchemaText(My.Computer.FileSystem.ReadAllText(OpenFileDialog1.FileName))
                If sch2 IsNot Nothing Then
                    schema = sch2
                Else
                    Throw New Exception
                End If
                While True
                    Threading.Thread.Sleep(0)
                    SyncLock UFReadCount
                        If UFReadCount > 0 Then Continue While
                        UnwrittenFiles.Clear()
                        Exit While
                    End SyncLock
                End While
                MessageBox.Show($"已加载索引，请自行确保索引一致性{vbCrLf}{vbCrLf}当前磁带VCI数据：{vbCrLf}{TapeUtils.Byte2Hex(TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 8, 12, 0).RawData, True)}")
                RefreshDisplay()
                Modified = False
            Catch ex As Exception
                MessageBox.Show("文件解析失败")
            End Try
        End If
    End Sub

    Private Sub 设置高度ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 设置高度ToolStripMenuItem.Click
        Dim Pos As Long = TapeUtils.ReadPosition(TapeDrive).BlockNumber
        If MessageBox.Show($"将当前位置{Pos}设置为数据高度，是否继续？{vbCrLf}如果不明白该操作含义，请点取消", "确认", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            CurrentHeight = Pos
        End If
    End Sub

    Private Sub 定位到起始块ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 定位到起始块ToolStripMenuItem.Click
        If ListView1.SelectedItems IsNot Nothing Then
            If ListView1.SelectedItems.Count > 0 Then
                If ListView1.SelectedItems(0).Tag IsNot Nothing Then
                    If TypeOf (ListView1.SelectedItems(0).Tag) Is ltfsindex.file Then

                        Dim f As ltfsindex.file = ListView1.SelectedItems(0).Tag
                        If f.extentinfo IsNot Nothing Then
                            If f.extentinfo.Count > 0 Then
                                Dim ext As ltfsindex.file.extent = f.extentinfo(0)

                                Dim th As New Threading.Thread(
                                    Sub()
                                        TapeUtils.Locate(TapeDrive, ext.startblock, ext.partition, TapeUtils.LocateDestType.Block)
                                        LockGUI(False)
                                        Invoke(Sub() MessageBox.Show($"已定位到{ext.startblock}"))
                                    End Sub)
                                LockGUI()
                                PrintMsg("正在定位")
                                th.Start()
                            End If
                        End If
                    End If
                End If
            End If
        End If
    End Sub
    Public Sub RestoreFile(FileName As String, FileIndex As ltfsindex.file)
        My.Computer.FileSystem.WriteAllBytes(FileName, {}, False)
        If FileIndex.length > 0 Then
            For Each fe As ltfsindex.file.extent In FileIndex.extentinfo
                If Not TapeUtils.RawDump(TapeDrive, FileName, fe.startblock, fe.byteoffset, fe.fileoffset, Math.Min(ExtraPartitionCount, fe.partition), fe.bytecount, StopFlag, plabel.blocksize,
                                         Sub(BytesReaded As Long)
                                             Threading.Interlocked.Add(TotalBytesProcessed, BytesReaded)
                                         End Sub) Then
                    PrintMsg($"{FileIndex.name}提取出错")
                    Exit For
                End If
                If StopFlag Then Exit Sub
            Next
        End If
        Dim finfo As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(FileName)
        finfo.CreationTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.creationtime)
        finfo.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.accesstime)
        finfo.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.modifytime)
        finfo.IsReadOnly = FileIndex.readonly
        Threading.Interlocked.Increment(TotalFilesProcessed)
    End Sub
    Private Sub 提取ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 提取ToolStripMenuItem.Click
        If ListView1.SelectedItems IsNot Nothing Then
            If ListView1.SelectedItems.Count > 0 Then
                If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
                    Dim BasePath As String = FolderBrowserDialog1.SelectedPath
                    LockGUI()
                    Dim flist As New List(Of ltfsindex.file)
                    For Each SI As ListViewItem In ListView1.SelectedItems
                        If TypeOf SI.Tag Is ltfsindex.file Then
                            flist.Add(SI.Tag)
                        End If
                    Next

                    Dim th As New Threading.Thread(
                            Sub()
                                Try
                                    PrintMsg("正在提取")
                                    StopFlag = False
                                    For Each FileIndex As ltfsindex.file In flist
                                        Dim FileName As String = My.Computer.FileSystem.CombinePath(BasePath, FileIndex.name)
                                        RestoreFile(FileName, FileIndex)
                                        If StopFlag Then Exit For
                                    Next
                                Catch ex As Exception
                                    PrintMsg("提取出错")
                                End Try
                                StopFlag=False
                                LockGUI(False)
                                PrintMsg("提取完成")
                                Invoke(Sub() MessageBox.Show("提取完成"))
                            End Sub)
                    th.Start()
                End If
            End If
        End If
    End Sub
    Private Sub 提取ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 提取ToolStripMenuItem1.Click
        If TreeView1.SelectedNode IsNot Nothing Then
            If FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
                Dim th As New Threading.Thread(
                    Sub()
                        PrintMsg("正在提取")
                        Try
                            Dim selectedDir As ltfsindex.directory = TreeView1.SelectedNode.Tag
                            Dim IterDir As Action(Of ltfsindex.directory, IO.DirectoryInfo) =
                            Sub(tapeDir As ltfsindex.directory, outputDir As IO.DirectoryInfo)
                                For Each f As ltfsindex.file In tapeDir.contents._file
                                    RestoreFile(My.Computer.FileSystem.CombinePath(outputDir.FullName, f.name), f)
                                Next
                                For Each d As ltfsindex.directory In tapeDir.contents._directory
                                    Dim thisDir As String = My.Computer.FileSystem.CombinePath(outputDir.FullName, d.name)
                                    Dim dirOutput As IO.DirectoryInfo
                                    Dim RestoreTimeStamp As Boolean = Not My.Computer.FileSystem.DirectoryExists(thisDir)
                                    If RestoreTimeStamp Then My.Computer.FileSystem.CreateDirectory(thisDir)
                                    dirOutput = My.Computer.FileSystem.GetDirectoryInfo(thisDir)
                                    IterDir(d, dirOutput)
                                    If RestoreTimeStamp Then
                                        dirOutput.CreationTimeUtc = TapeUtils.ParseTimeStamp(d.creationtime)
                                        dirOutput.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(d.modifytime)
                                        dirOutput.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(d.accesstime)
                                    End If
                                Next
                            End Sub
                            IterDir(selectedDir, My.Computer.FileSystem.GetDirectoryInfo(FolderBrowserDialog1.SelectedPath))
                            PrintMsg("提取完成")
                        Catch ex As Exception
                            PrintMsg("提取出错")
                            Invoke(Sub() MessageBox.Show("提取完成"))
                        End Try
                        LockGUI(False)
                    End Sub)
                LockGUI()
                th.Start()
            End If
        End If
    End Sub

    Private Sub 删除ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 删除ToolStripMenuItem.Click
        DeleteDir()
    End Sub

    Private Sub 重命名ToolStripMenuItem_Click_1(sender As Object, e As EventArgs) Handles 重命名ToolStripMenuItem.Click
        RenameDir()
    End Sub

    Private Sub 格式化ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 格式化ToolStripMenuItem.Click
        If MessageBox.Show("全部数据将丢失且无法恢复，是否继续？", "警告", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            While True
                Threading.Thread.Sleep(0)
                SyncLock UFReadCount
                    If UFReadCount > 0 Then Continue While
                    UnwrittenFiles.Clear()
                    Exit While
                End SyncLock
            End While
            Dim MaxExtraPartitionAllowed As Byte = TapeUtils.ModeSense(TapeDrive, &H11)(2)
            If MaxExtraPartitionAllowed > 1 Then MaxExtraPartitionAllowed = 1
            Dim Barcode As String = TapeUtils.ReadBarcode(TapeDrive)
            Barcode = InputBox("设置标签", "磁带标签", Barcode)
            Dim VolumeLabel As String = InputBox("设置卷标", "LTFS卷标", Barcode)
            LockGUI()
            Dim DefaultBlockSize As Long = 524288
            If MaxExtraPartitionAllowed = 0 Then DefaultBlockSize = 65536
            TapeUtils.mkltfs(TapeDrive, Barcode, VolumeLabel, MaxExtraPartitionAllowed, DefaultBlockSize, False,
                Sub(Message As String)
                    'ProgressReport
                    PrintMsg(Message)
                End Sub,
                Sub(Message As String)
                    'OnFinished
                    PrintMsg(Message)
                    LockGUI(False)
                    Me.Invoke(Sub()
                                  MessageBox.Show("格式化完成")
                                  读取索引ToolStripMenuItem_Click(sender, e)
                              End Sub)
                End Sub,
                Sub(Message As String)
                    'OnError
                    PrintMsg(Message)
                    LockGUI(False)
                    Me.Invoke(Sub() MessageBox.Show($"格式化失败{vbCrLf}{Message}"))
                End Sub)
        End If
    End Sub


    Private Sub TreeView1_Click(sender As Object, e As EventArgs) Handles TreeView1.Click
        TriggerTreeView1Event()
    End Sub
End Class