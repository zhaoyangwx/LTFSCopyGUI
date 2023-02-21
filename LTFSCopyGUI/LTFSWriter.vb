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
    Public Property AllowOperation As Boolean = True
    Public OperationLock As New Object
    Public Property Barcode As String
    Public Property StopFlag As Boolean = False
    Public Property Flush As Boolean = False
    Public Property Clean As Boolean = False
    Public Property SilentMode As Boolean = False
    Public Property SilentAutoEject As Boolean = False
    Private ddelta, fdelta As Long
    Public SMaxNum As Integer = 600
    Public PMaxNum As Integer = 3600 * 6
    Public SpeedHistory As List(Of Double) = New Double(PMaxNum) {}.ToList()
    Public FileRateHistory As List(Of Double) = New Double(PMaxNum) {}.ToList()

    Public FileDroper As FileDropHandler

    Public Sub Load_Settings()
        覆盖已有文件ToolStripMenuItem.Checked = My.Settings.LTFSWriter_OverwriteExist
        Select Case My.Settings.LTFSWriter_OnWriteFinished
            Case 0
                WA0ToolStripMenuItem.Checked = True
                WA1ToolStripMenuItem.Checked = False
                WA2ToolStripMenuItem.Checked = False
                WA3ToolStripMenuItem.Checked = False
            Case 1
                WA0ToolStripMenuItem.Checked = False
                WA1ToolStripMenuItem.Checked = True
                WA2ToolStripMenuItem.Checked = False
                WA3ToolStripMenuItem.Checked = False
            Case 2
                WA0ToolStripMenuItem.Checked = False
                WA1ToolStripMenuItem.Checked = False
                WA2ToolStripMenuItem.Checked = True
                WA3ToolStripMenuItem.Checked = False
            Case 3
                WA0ToolStripMenuItem.Checked = False
                WA1ToolStripMenuItem.Checked = False
                WA2ToolStripMenuItem.Checked = False
                WA3ToolStripMenuItem.Checked = True
        End Select
        APToolStripMenuItem.Checked = My.Settings.LTFSWriter_AutoFlush
    End Sub
    Public Sub Save_Settings()
        My.Settings.LTFSWriter_OverwriteExist = 覆盖已有文件ToolStripMenuItem.Checked
        If WA0ToolStripMenuItem.Checked Then
            My.Settings.LTFSWriter_OnWriteFinished = 0
        ElseIf WA1ToolStripMenuItem.Checked Then
            My.Settings.LTFSWriter_OnWriteFinished = 1
        ElseIf WA2ToolStripMenuItem.Checked Then
            My.Settings.LTFSWriter_OnWriteFinished = 2
        ElseIf WA3ToolStripMenuItem.Checked Then
            My.Settings.LTFSWriter_OnWriteFinished = 3
        End If
        My.Settings.LTFSWriter_AutoFlush = APToolStripMenuItem.Checked
        My.Settings.Save()
    End Sub
    Public Sub PrintMsg(s As String, Optional ByVal Warning As Boolean = False, Optional ByVal TooltipText As String = "")
        Me.Invoke(Sub()
                      If TooltipText = "" Then TooltipText = s
                      If Not Warning Then
                          ToolStripStatusLabel3.Text = s
                          ToolStripStatusLabel3.ToolTipText = TooltipText
                      Else
                          ToolStripStatusLabel5.Text = s
                          ToolStripStatusLabel5.ToolTipText = TooltipText
                      End If
                  End Sub)
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

            If APToolStripMenuItem.Checked Then
                Dim FlushNow As Boolean = True
                For j As Integer = 1 To 3
                    Dim n As Double = SpeedHistory(SpeedHistory.Count - j)
                    If n < 70 Or n > 85 Then
                        FlushNow = False
                        Exit For
                    End If
                Next
                Flush = FlushNow
            End If
            SyncLock OperationLock
                If AllowOperation Then CheckClean()
            End SyncLock
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
            Dim USize As Long = UnwrittenSize
            Dim UFile As Long = UnwrittenFiles.Count
            ToolStripStatusLabel4.Text = ""
            ToolStripStatusLabel4.Text &= $"速度:{IOManager.FormatSize(ddelta)}/s"
            ToolStripStatusLabel4.Text &= $"  累计:{IOManager.FormatSize(TotalBytesProcessed)}"
            If CurrentBytesProcessed > 0 Then ToolStripStatusLabel4.Text &= $"({IOManager.FormatSize(CurrentBytesProcessed)})"
            ToolStripStatusLabel4.Text &= $"|{TotalFilesProcessed}"
            If CurrentFilesProcessed > 0 Then ToolStripStatusLabel4.Text &= $"({CurrentFilesProcessed})"
            ToolStripStatusLabel4.Text &= $"  待写:"
            If UFile > 0 AndAlso UFile >= CurrentFilesProcessed Then ToolStripStatusLabel4.Text &= $"[{UFile - CurrentFilesProcessed}/{UFile}]"
            ToolStripStatusLabel4.Text &= $"{ IOManager.FormatSize(Math.Max(0, USize - CurrentBytesProcessed))}/{IOManager.FormatSize(USize)}"
            ToolStripStatusLabel4.Text &= $"  待索引:{IOManager.FormatSize(TotalBytesUnindexed)}"
            ToolStripStatusLabel4.ToolTipText = ToolStripStatusLabel4.Text
            If USize > 0 AndAlso CurrentBytesProcessed >= 0 AndAlso CurrentBytesProcessed <= USize Then
                ToolStripProgressBar1.Value = CurrentBytesProcessed / USize * 10000
                ToolStripProgressBar1.ToolTipText = $"进度:{IOManager.FormatSize(CurrentBytesProcessed)}/{IOManager.FormatSize(USize)}"
            End If
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
    Private Sub LTFSWriter_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        FileDroper = New FileDropHandler(ListView1)
        Load_Settings()
        Try
            读取索引ToolStripMenuItem_Click(sender, e)
        Catch ex As Exception
            PrintMsg("获取分区信息出错")
        End Try

    End Sub
    Private Sub LTFSWriter_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        e.Cancel = False
        If Not AllowOperation Then
            MessageBox.Show("请等待操作完成")
            e.Cancel = True
            Exit Sub
        End If
        If TotalBytesUnindexed > 0 Then
            If MessageBox.Show("未索引的数据将会丢失，是否继续", "警告", MessageBoxButtons.YesNo) = DialogResult.No Then
                e.Cancel = True
                Exit Sub
            Else
                Save_Settings()
                e.Cancel = False
                UFReadCount.Value = 0
                Exit Sub
            End If
        End If
        If ExtraPartitionCount > 0 AndAlso Modified Then
            If MessageBox.Show("未安全弹出将导致索引不一致，是否继续", "警告", MessageBoxButtons.YesNo) = DialogResult.No Then
                e.Cancel = True
            Else
                Save_Settings()
                e.Cancel = False
                UFReadCount.Value = 0
            End If
        End If
        Save_Settings()
    End Sub
    Public Function GetLocInfo() As String
        If schema Is Nothing Then Return "无索引"
        Dim info As String = ""
        Try
            SyncLock schema
                info = $"索引{schema.generationnumber} - 分区{schema.location.partition} - 块{schema.location.startblock}"
                If schema.previousgenerationlocation IsNot Nothing Then
                    If schema.previousgenerationlocation.startblock > 0 Then info &= $" (此前:分区{schema.previousgenerationlocation.partition} - 块{schema.previousgenerationlocation.startblock})"
                End If
            End SyncLock
            If CurrentHeight > 0 Then info &= $" 数据高度{CurrentHeight}"
            If Modified Then info &= "*"
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
                       ToolStripStatusLabel2.ToolTipText = ToolStripStatusLabel2.Text
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
                    ToolStripStatusLabel4.ToolTipText = ToolStripStatusLabel4.Text
                Catch ex As Exception
                    PrintMsg("刷新显示失败")
                End Try

            End Sub)
    End Sub
    Private Sub ToolStripStatusLabel2_Click(sender As Object, e As EventArgs) Handles ToolStripStatusLabel2.Click
        Try
            RefreshCapacity()
            PrintMsg("可用容量已刷新")
        Catch ex As Exception
            PrintMsg("可用容量刷新失败")
        End Try

    End Sub
    Public Sub LockGUI(Optional ByVal Lock As Boolean = True)
        Invoke(Sub()
                   SyncLock OperationLock
                       AllowOperation = Not Lock
                       'MenuStrip1.Enabled = AllowOperation
                       ContextMenuStrip1.Enabled = AllowOperation
                       For Each Items As ToolStripMenuItem In MenuStrip1.Items
                           For Each SubItem In Items.DropDownItems
                               If TypeOf (SubItem) Is ToolStripDropDownItem Then
                                   CType(SubItem, ToolStripDropDownItem).Enabled = AllowOperation
                               End If
                           Next
                       Next
                       自动化ToolStripMenuItem1.Enabled = True
                       ContextMenuStrip3.Enabled = AllowOperation
                   End SyncLock
               End Sub)
    End Sub
    Public Sub TriggerTreeView1Event()
        If TreeView1.SelectedNode IsNot Nothing AndAlso TreeView1.SelectedNode.Tag IsNot Nothing Then
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
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        TriggerTreeView1Event()
    End Sub
    Private Sub TreeView1_Click(sender As Object, e As EventArgs) Handles TreeView1.Click
        TriggerTreeView1Event()
    End Sub
    Public Sub CheckUnindexedDataSizeLimit(Optional ByVal ForceFlush As Boolean = False)
        If TotalBytesUnindexed >= IndexWriteInterval Or ForceFlush Then
            WriteCurrentIndex(False, False)
            TotalBytesUnindexed = 0
            Invoke(Sub() Text = GetLocInfo())
        End If
    End Sub
    Public Sub WriteCurrentIndex(Optional ByVal GotoEOD As Boolean = True, Optional ByVal ClearCurrentStat As Boolean = True)
        If GotoEOD Then TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.EOD)
        Dim CurrentPos As TapeUtils.PositionData = TapeUtils.ReadPosition(TapeDrive)
        If ExtraPartitionCount > 0 AndAlso schema IsNot Nothing AndAlso schema.location.partition <> CurrentPos.PartitionNumber Then
            Throw New Exception($"当前位置p{CurrentPos.PartitionNumber}b{CurrentPos.BlockNumber}不允许写入新的索引")
            Exit Sub
        End If
        If ExtraPartitionCount > 0 AndAlso schema IsNot Nothing AndAlso schema.location.startblock >= CurrentPos.BlockNumber Then
            Throw New Exception($"当前位置p{CurrentPos.PartitionNumber}b{CurrentPos.BlockNumber}不允许写入新的索引")
            Exit Sub
        End If
        TapeUtils.WriteFileMark(TapeDrive)
        schema.generationnumber += 1
        schema.updatetime = Now.ToUniversalTime.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
        schema.location.partition = ltfsindex.PartitionLabel.b
        schema.previousgenerationlocation = New ltfsindex.PartitionDef With {.partition = schema.location.partition, .startblock = schema.location.startblock}
        schema.location.startblock = TapeUtils.ReadPosition(TapeDrive).BlockNumber
        PrintMsg("正在生成索引")
        Dim sdata As Byte() = Encoding.UTF8.GetBytes(schema.GetSerializedText())
        PrintMsg("正在写入索引")
        While sdata.Length > 0
            Dim wdata As Byte() = sdata.Take(Math.Min(plabel.blocksize, sdata.Length)).ToArray
            sdata = sdata.Skip(Math.Min(plabel.blocksize, sdata.Length)).ToArray()
            TapeUtils.Write(TapeDrive, wdata)
            If sdata.Length = 0 Then Exit While
        End While
        TotalBytesUnindexed = 0
        If ClearCurrentStat Then
            CurrentBytesProcessed = 0
            CurrentFilesProcessed = 0
        End If
        TapeUtils.WriteFileMark(TapeDrive)
        PrintMsg("索引写入完成")
        CurrentHeight = TapeUtils.ReadPosition(TapeDrive).BlockNumber
        Modified = ExtraPartitionCount > 0
    End Sub
    Public Sub RefreshIndexPartition()
        Dim block1 As Long = schema.location.startblock
        If ExtraPartitionCount > 0 Then
            PrintMsg("正在定位")
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
            PrintMsg("正在生成索引")
            Dim sdata As Byte() = Encoding.UTF8.GetBytes(schema.GetSerializedText())
            PrintMsg("正在写入索引")
            While sdata.Length > 0
                Dim wdata As Byte() = sdata.Take(Math.Min(plabel.blocksize, sdata.Length)).ToArray
                sdata = sdata.Skip(Math.Min(plabel.blocksize, sdata.Length)).ToArray()
                TapeUtils.Write(TapeDrive, wdata)
            End While
            TapeUtils.WriteFileMark(TapeDrive)
            PrintMsg("索引写入完成")
        End If
        TapeUtils.WriteVCI(TapeDrive, schema.generationnumber, block0, block1, schema.volumeuuid.ToString(), ExtraPartitionCount)
        Modified = False
    End Sub
    Public Sub UpdataAllIndex()
        If TotalBytesUnindexed > 0 Then
            PrintMsg("正在更新数据区索引")
            WriteCurrentIndex(False)
        End If
        PrintMsg("正在更新索引")
        RefreshIndexPartition()
        TapeUtils.ReleaseUnit(TapeDrive)
        TapeUtils.AllowMediaRemoval(TapeDrive)
        PrintMsg("索引已更新")
        If schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.a Then 更新数据区索引ToolStripMenuItem.Enabled = False
        If SilentMode Then
            If SilentAutoEject Then
                TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Eject)
            End If
        Else
            Dim DoEject As Boolean = False
            Invoke(Sub()
                       DoEject = WA3ToolStripMenuItem.Checked OrElse MessageBox.Show("现在可以安全弹出了。是否弹出？", "提示", MessageBoxButtons.OKCancel) = DialogResult.OK
                   End Sub)
            If DoEject Then TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Eject)
        End If
        Invoke(Sub()

                   LockGUI(False)
                   RefreshDisplay()
               End Sub)
    End Sub
    Public Sub OnWriteFinished()
        If WA0ToolStripMenuItem.Checked Then Exit Sub
        If WA1ToolStripMenuItem.Checked Then
            Dim SilentBefore As Boolean = SilentMode
            SilentMode = True
            Try
                If TotalBytesUnindexed > 0 AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
                    WriteCurrentIndex(False)
                End If
            Catch ex As Exception
                PrintMsg(ex.ToString())
            End Try
            SilentMode = SilentBefore
            Exit Sub
        End If
        If WA2ToolStripMenuItem.Checked Then
            Dim SilentBefore As Boolean = SilentMode
            SilentMode = True
            SilentAutoEject = False
            UpdataAllIndex()
            SilentMode = SilentBefore
            Exit Sub
        End If
        If WA3ToolStripMenuItem.Checked Then
            SilentMode = True
            SilentAutoEject = True
            UpdataAllIndex()
            Exit Sub
        End If
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
                        PrintMsg("未找到VOL1")
                        Invoke(Sub() MessageBox.Show("非LTFS格式", "错误"))
                        LockGUI(False)
                        Exit Sub
                    End If
                    TapeUtils.Locate(TapeDrive, 1, 0, TapeUtils.LocateDestType.File)
                    PrintMsg("正在读取LTFS信息")
                    TapeUtils.ReadBlock(TapeDrive)
                    Dim pltext As String = Encoding.UTF8.GetString(TapeUtils.ReadToFileMark(TapeDrive))
                    plabel = ltfslabel.FromXML(pltext)
                    TapeUtils.SetBlockSize(TapeDrive, plabel.blocksize)
                    Barcode = TapeUtils.ReadBarcode(TapeDrive)
                    PrintMsg("正在定位")
                    TapeUtils.Locate(TapeDrive, 3, 0, TapeUtils.LocateDestType.File)
                    TapeUtils.ReadBlock(TapeDrive)
                    Dim data As Byte()
                    If ExtraPartitionCount = 0 Then
                        TapeUtils.Locate(TapeDrive, 0, 0, TapeUtils.LocateDestType.EOD)
                        PrintMsg("正在读取索引")
                        Dim FM As Long = TapeUtils.ReadPosition(TapeDrive).FileNumber
                        If FM <= 1 Then
                            PrintMsg("索引读取失败")
                            Invoke(Sub() MessageBox.Show("非LTFS格式", "错误"))
                            LockGUI(False)
                            Exit Sub
                        End If
                        TapeUtils.Locate(TapeDrive, FM - 1, 0, TapeUtils.LocateDestType.File)
                        TapeUtils.ReadBlock(TapeDrive)
                    End If
                    PrintMsg("正在读取索引")
                    data = TapeUtils.ReadToFileMark(TapeDrive)
                    PrintMsg("正在解析索引")
                    schema = ltfsindex.FromSchemaText(Encoding.UTF8.GetString(data))
                    PrintMsg("保存备份文件")
                    Dim FileName As String = ""
                    If Barcode <> "" Then
                        FileName = Barcode
                    Else
                        If schema IsNot Nothing Then
                            FileName = schema.volumeuuid.ToString()
                        End If
                    End If
                    Dim outputfile As String = $"schema\LTFSIndex_{FileName}_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.schema"
                    If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, "schema")) Then
                        My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, "schema"))
                    End If
                    outputfile = My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            Exit While
                        End SyncLock
                    End While
                    Modified = False
                    Me.Invoke(Sub()
                                  Text = Barcode
                                  ToolStripStatusLabel1.Text = Barcode.TrimEnd(" ")
                                  ToolStripStatusLabel1.ToolTipText = $"磁带标签:{ToolStripStatusLabel1.Text}"
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
    Public Sub AddFile(f As IO.FileInfo, d As ltfsindex.directory, Optional ByVal OverWrite As Boolean = False)
        Dim FileExist As Boolean = False
        '检查磁带已有文件
        SyncLock d.contents._file
            For Each oldf As ltfsindex.file In d.contents._file
                If oldf.name.ToLower = f.Name.ToLower Then
                    If OverWrite Then d.contents._file.Remove(oldf)
                    FileExist = True
                End If
            Next
        End SyncLock
        If FileExist And (Not OverWrite) Then Exit Sub
        '检查写入队列
        If Not FileExist Then
            While True
                Threading.Thread.Sleep(0)
                SyncLock UFReadCount
                    If UFReadCount > 0 Then Continue While
                    For Each oldf As FileRecord In UnwrittenFiles
                        If oldf.ParentDirectory Is d AndAlso oldf.File.name.ToLower = f.Name.ToLower Then
                            oldf.ParentDirectory.contents.UnwrittenFiles.Remove(oldf.File)
                            UnwrittenFiles.Remove(oldf)
                            FileExist = True
                            Exit For
                        End If
                    Next
                    Exit While
                End SyncLock
            End While
        End If
        '添加到队列
        Dim frnew As New FileRecord(f.FullName, d)
        While True
            Threading.Thread.Sleep(0)
            SyncLock UFReadCount
                If UFReadCount > 0 Then Continue While
                UnwrittenFiles.Add(frnew)
                Exit While
            End SyncLock
        End While
    End Sub
    Public Sub AddDirectry(dnew1 As IO.DirectoryInfo, d1 As ltfsindex.directory, Optional ByVal OverWrite As Boolean = False)
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
            Dim FileExist As Boolean = False
            '检查已有文件
            SyncLock dT.contents._file
                For Each fe As ltfsindex.file In dT.contents._file
                    If fe.name = f.Name Then
                        FileExist = True
                        If OverWrite Then dT.contents._file.Remove(fe)
                    End If
                Next
            End SyncLock
            If FileExist And (Not OverWrite) Then Continue For
            '检查写入队列
            If Not FileExist Then
                While True
                    Threading.Thread.Sleep(0)
                    SyncLock UFReadCount
                        If UFReadCount > 0 Then Continue While
                        For Each oldf As FileRecord In UnwrittenFiles
                            If oldf.ParentDirectory Is dT AndAlso oldf.File.name.ToLower = f.Name.ToLower Then
                                oldf.ParentDirectory.contents.UnwrittenFiles.Remove(oldf.File)
                                UnwrittenFiles.Remove(oldf)
                                FileExist = True
                                Exit For
                            End If
                        Next
                        Exit While
                    End SyncLock
                End While
            End If
            '添加到队列
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
            AddDirectry(dn, dT, OverWrite)
        Next
    End Sub
    Public Sub ConcatDirectory(dnew1 As IO.DirectoryInfo, d1 As ltfsindex.directory, Optional ByVal OverWrite As Boolean = False)
        For Each f As IO.FileInfo In dnew1.GetFiles()
            Dim FileExist As Boolean = False
            '检查磁带已有文件
            SyncLock d1.contents._file
                For Each fe As ltfsindex.file In d1.contents._file
                    If fe.name = f.Name Then
                        FileExist = True
                        If OverWrite Then
                            d1.contents._file.Remove(fe)
                        End If
                    End If
                Next
            End SyncLock
            If (Not OverWrite) And FileExist Then Continue For
            '检查写入队列
            If Not FileExist Then
                While True
                    Threading.Thread.Sleep(0)
                    SyncLock UFReadCount
                        If UFReadCount > 0 Then Continue While
                        For Each oldf As FileRecord In UnwrittenFiles
                            If oldf.ParentDirectory Is d1 AndAlso oldf.File.name.ToLower = f.Name.ToLower Then
                                oldf.ParentDirectory.contents.UnwrittenFiles.Remove(oldf.File)
                                UnwrittenFiles.Remove(oldf)
                                FileExist = True
                                Exit For
                            End If
                        Next
                        Exit While
                    End SyncLock
                End While
            End If
            '添加到队列
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
            ConcatDirectory(dn, dT, OverWrite)
        Next
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
    Private Sub 重命名ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 重命名文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing AndAlso
        ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 AndAlso
        ListView1.SelectedItems.Item(0).Tag IsNot Nothing AndAlso
        TypeOf (ListView1.SelectedItems.Item(0).Tag) Is ltfsindex.file Then
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
    End Sub
    Private Sub 删除文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 删除文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing AndAlso
        ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 AndAlso
        MessageBox.Show($"确认删除{ListView1.SelectedItems.Count}个文件？", "警告", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            SyncLock ListView1.SelectedItems
                For Each ItemSelected As ListViewItem In ListView1.SelectedItems
                    If ItemSelected.Tag IsNot Nothing AndAlso TypeOf (ItemSelected.Tag) Is ltfsindex.file Then
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
                Next
            End SyncLock
        End If
    End Sub
    Private Sub 添加文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 添加文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing AndAlso OpenFileDialog1.ShowDialog = DialogResult.OK Then
            Dim d As ltfsindex.directory = ListView1.Tag
            For Each fpath As String In OpenFileDialog1.FileNames
                Dim f As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(fpath)
                Try
                    AddFile(f, d, 覆盖已有文件ToolStripMenuItem.Checked)
                    PrintMsg("文件添加成功")
                Catch ex As Exception
                    PrintMsg("文件添加失败")
                    MessageBox.Show(ex.ToString())
                End Try
            Next
            RefreshDisplay()
        End If
    End Sub
    Private Sub ListView1_DragEnter(sender As Object, e As DragEventArgs) Handles ListView1.DragEnter
        If MenuStrip1.Enabled = False Then
            PrintMsg("当前无法进行拖放操作")
            Exit Sub
        End If
        If ListView1.Tag IsNot Nothing AndAlso TypeOf ListView1.Tag Is ltfsindex.directory Then
            Dim Paths As String() = e.Data.GetData(GetType(String()))
            Dim d As ltfsindex.directory = ListView1.Tag
            Dim overwrite As Boolean = 覆盖已有文件ToolStripMenuItem.Checked
            Dim th As New Threading.Thread(
                Sub()
                    StopFlag = False
                    PrintMsg($"正在添加{Paths.Length}个项目")
                    Dim i As Integer = 0
                    For Each path As String In Paths
                        i += 1
                        If StopFlag Then Exit For
                        Try
                            If My.Computer.FileSystem.FileExists(path) Then
                                Dim f As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(path)
                                PrintMsg($"正在添加 [{i}/{Paths.Length}] {f.Name}")
                                AddFile(f, d, overwrite)
                            ElseIf My.Computer.FileSystem.DirectoryExists(path) Then
                                Dim f As IO.DirectoryInfo = My.Computer.FileSystem.GetDirectoryInfo(path)
                                PrintMsg($"正在添加 [{i}/{Paths.Length}] {f.Name}")
                                AddDirectry(f, d, overwrite)
                            End If
                        Catch ex As Exception
                            Invoke(Sub() MessageBox.Show(ex.ToString()))
                        End Try
                    Next
                    RefreshDisplay()
                    PrintMsg("添加完成")
                    LockGUI(False)
                End Sub)
            LockGUI()
            th.Start()
        End If
    End Sub
    Private Sub 导入文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 导入文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing AndAlso FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            Dim dnew As IO.DirectoryInfo = My.Computer.FileSystem.GetDirectoryInfo(FolderBrowserDialog1.SelectedPath)
            Dim d As ltfsindex.directory = ListView1.Tag
            Try
                ConcatDirectory(dnew, d, 覆盖已有文件ToolStripMenuItem.Checked)
                PrintMsg("导入成功")
            Catch ex As Exception
                PrintMsg("导入失败")
                MessageBox.Show(ex.ToString())
            End Try
            RefreshDisplay()
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
                    Try
                        AddDirectry(dnew, d, 覆盖已有文件ToolStripMenuItem.Checked)
                        PrintMsg("目录添加成功")
                    Catch ex As Exception
                        PrintMsg("目录添加失败")
                        MessageBox.Show(ex.ToString())
                    End Try
                Next
                RefreshDisplay()
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
    Private Sub 删除目录ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 删除目录ToolStripMenuItem.Click
        DeleteDir()
    End Sub
    Private Sub 重命名目录ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 重命名目录ToolStripMenuItem.Click
        RenameDir()
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
        If ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 AndAlso
        FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
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
                        StopFlag = False
                        LockGUI(False)
                        PrintMsg("提取完成")
                        Invoke(Sub() MessageBox.Show("提取完成"))
                    End Sub)
            th.Start()
        End If
    End Sub
    Private Sub 提取ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 提取ToolStripMenuItem1.Click
        If TreeView1.SelectedNode IsNot Nothing AndAlso FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
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
    End Sub
    Private Sub 删除ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 删除ToolStripMenuItem.Click
        DeleteDir()
    End Sub
    Private Sub 重命名ToolStripMenuItem_Click_1(sender As Object, e As EventArgs) Handles 重命名ToolStripMenuItem.Click
        RenameDir()
    End Sub
    Private Sub 更新索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 更新全部索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
                Sub()
                    Try
                        UpdataAllIndex()
                    Catch ex As Exception
                        PrintMsg("索引更新出错")
                    End Try
                End Sub)
        LockGUI(True)
        th.Start()
    End Sub
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
                        PrintMsg("正在读取索引")
                        Dim schraw As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                        PrintMsg("正在解析索引")
                        Dim sch2 As ltfsindex = ltfsindex.FromSchemaText(Encoding.UTF8.GetString(schraw))
                        PrintMsg("索引解析成功")
                        schema.previousgenerationlocation = sch2.previousgenerationlocation
                        CurrentHeight = TapeUtils.ReadPosition(TapeDrive).BlockNumber
                        Invoke(Sub() Text = GetLocInfo())
                    ElseIf CurrentHeight > 0 Then
                        If TapeUtils.ReadPosition(TapeDrive).BlockNumber <> CurrentHeight Then
                            TapeUtils.Locate(TapeDrive, CurrentHeight, 1, TapeUtils.LocateDestType.Block)
                        End If
                    End If
                    Invoke(Sub() 更新数据区索引ToolStripMenuItem.Enabled = True)
                    UFReadCount.Inc()
                    CurrentFilesProcessed = 0
                    CurrentBytesProcessed = 0
                    Dim WriteList As New List(Of FileRecord)
                    UFReadCount.Inc()
                    For Each fr As FileRecord In UnwrittenFiles
                        WriteList.Add(fr)
                    Next
                    UFReadCount.Dec()
                    For Each fr As FileRecord In WriteList
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
                            PrintMsg($"正在写入 {fr.File.name}  大小 {IOManager.FormatSize(fr.File.length)}", False,
                                     $"正在写入: {fr.SourcePath}{vbCrLf}大小: {IOManager.FormatSize(fr.File.length)}{vbCrLf}此前累计: {IOManager.FormatSize(TotalBytesProcessed)}")
                            'write to tape
                            TapeUtils.SetBlockSize(TapeDrive, plabel.blocksize)
                            If finfo.Length <= plabel.blocksize Then
                                Dim sense As Byte() = TapeUtils.Write(TapeDrive, My.Computer.FileSystem.ReadAllBytes(fr.SourcePath))
                                If ((sense(2) >> 6) And &H1) = 1 Then
                                    If (sense(2) And &HF) = 13 Then
                                        PrintMsg("磁带已满")
                                        Invoke(Sub() MessageBox.Show("磁带已满"))
                                        StopFlag = True
                                        Exit For
                                    Else
                                        PrintMsg($"磁带即将写满", True)
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
                                            If (sense(2) And &HF) = 13 Then
                                                PrintMsg("磁带已满")
                                                Invoke(Sub() MessageBox.Show("磁带已满"))
                                                StopFlag = True
                                                fs.Close()
                                                Exit For
                                            Else
                                                PrintMsg("磁带即将写满", True)
                                            End If
                                        End If
                                        CheckFlush()
                                        CheckClean()
                                        fr.File.WrittenBytes += BytesReaded
                                        TotalBytesProcessed += BytesReaded
                                        CurrentBytesProcessed += BytesReaded
                                        TotalBytesUnindexed += BytesReaded
                                    Else
                                        Exit While
                                    End If
                                End While
                                fs.Close()
                                Marshal.FreeHGlobal(wBufferPtr)
                                TotalFilesProcessed += 1
                                CurrentFilesProcessed += 1
                                CheckUnindexedDataSizeLimit()
                            End If
                            CurrentHeight = TapeUtils.ReadPosition(TapeDrive).BlockNumber
                        Else
                            TotalBytesUnindexed += 1
                            TotalFilesProcessed += 1
                            CurrentFilesProcessed += 1
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
                    Me.Invoke(Sub() Timer1_Tick(sender, e))
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            Exit While
                        End SyncLock
                    End While
                    Modified = True
                    If Not StopFlag Then
                        PrintMsg("写入完成")
                        Me.Invoke(Sub() OnWriteFinished())
                    Else
                        PrintMsg("写入取消")
                    End If
                    PrintMsg("", True)
                Catch ex As Exception
                    PrintMsg($"写入出错{ex.ToString}")
                End Try
                TapeUtils.ReleaseUnit(TapeDrive)
                TapeUtils.AllowMediaRemoval(TapeDrive)
                Invoke(Sub()
                           LockGUI(False)
                           RefreshDisplay()
                           RefreshCapacity()
                           If Not StopFlag AndAlso WA0ToolStripMenuItem.Checked AndAlso MessageBox.Show("写入完成，是否更新数据区索引？（推荐）", "操作成功成功", MessageBoxButtons.OKCancel) = DialogResult.OK Then
                               更新数据区索引ToolStripMenuItem_Click(sender, e)
                           End If
                       End Sub)
            End Sub)
        StopFlag = False
        LockGUI()
        th.Start()
    End Sub
    Private Sub 清除当前索引后数据ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 清除当前索引后数据ToolStripMenuItem.Click

        If MessageBox.Show("未索引的数据将永久丢失，是否继续", "警告", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
            Exit Sub
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    PrintMsg("正在定位索引")
                    TapeUtils.Locate(TapeDrive, schema.location.startblock, schema.location.partition, TapeUtils.LocateDestType.Block)
                    PrintMsg("正在读取索引")
                    Dim data As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim CurrentPos As New TapeUtils.PositionData(TapeDrive)
                    If CurrentPos.PartitionNumber < ExtraPartitionCount Then
                        Invoke(Sub() MessageBox.Show("当前为索引区，操作取消"))
                        Exit Sub
                    End If
                    TapeUtils.Locate(TapeDrive, CurrentPos.BlockNumber - 1, CurrentPos.PartitionNumber, TapeUtils.LocateDestType.Block)
                    TapeUtils.WriteFileMark(TapeDrive)
                    Dim outputfile As String = "LTFSIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    PrintMsg("正在解析索引")
                    schema = ltfsindex.FromSchemaText(My.Computer.FileSystem.ReadAllText(outputfile))
                    PrintMsg("索引解析成功")
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
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
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
                Modified = False
                PrintMsg("已回到索引位置")
                Me.Invoke(Sub()
                              LockGUI(False)
                              Text = GetLocInfo()
                          End Sub)
            End Sub)
        LockGUI()
        th.Start()
    End Sub
    Private Sub 读取数据区索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 读取数据区索引ToolStripMenuItem.Click
        If ExtraPartitionCount = 0 Then
            读取索引ToolStripMenuItem_Click(sender, e)
            Exit Sub
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    PrintMsg("正在定位")

                    Dim data As Byte()
                    Dim currentPos As TapeUtils.PositionData = TapeUtils.ReadPosition(TapeDrive)
                    If currentPos.PartitionNumber <> 1 Then TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.Block)
                    TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.EOD)
                    PrintMsg("正在读取索引")
                    Dim FM As Long = TapeUtils.ReadPosition(TapeDrive).FileNumber
                    If FM <= 1 Then
                        PrintMsg("索引读取失败")
                        Invoke(Sub() MessageBox.Show("非LTFS格式", "错误"))
                        LockGUI(False)
                        Exit Sub
                    End If
                    TapeUtils.Locate(TapeDrive, FM - 1, 1, TapeUtils.LocateDestType.File)
                    TapeUtils.ReadBlock(TapeDrive)
                    PrintMsg("正在读取索引")
                    data = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim outputfile As String = "schema\LTFSIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, "schema")) Then
                        My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, "schema"))
                    End If
                    outputfile = My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    PrintMsg("正在解析索引")
                    schema = ltfsindex.FromSchemaText(My.Computer.FileSystem.ReadAllText(outputfile))
                    PrintMsg("索引解析成功")
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            Exit While
                        End SyncLock
                    End While
                    Modified = False
                    Me.Invoke(Sub()
                                  ToolStripStatusLabel1.ToolTipText = ToolStripStatusLabel1.Text
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
    Private Sub 回滚ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 回滚ToolStripMenuItem.Click
        If MessageBox.Show($"当前第{schema.generationnumber}代索引位置: 分区{schema.location.partition} 块{schema.location.startblock}{vbCrLf _
                           }上一代索引位置: 分区{schema.previousgenerationlocation.partition} 块{schema.previousgenerationlocation.startblock}{vbCrLf _
                           }未索引的数据将丢失，且回滚后一旦继续写入将丢失后面数据，是否继续", "警告", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
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
                    PrintMsg("正在读取索引")
                    Dim data As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim outputfile As String = "LTFSIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    PrintMsg("正在解析索引")
                    schema = ltfsindex.FromSchemaText(My.Computer.FileSystem.ReadAllText(outputfile))
                    PrintMsg("索引解析成功")
                    Modified = False
                    CurrentHeight = TapeUtils.ReadPosition(TapeDrive).BlockNumber
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
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
    Private Sub 更新数据区索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 更新数据区索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
        Sub()
            Try
                If TotalBytesUnindexed > 0 AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
                    WriteCurrentIndex(False)
                    PrintMsg("已写入数据区索引")
                End If
            Catch ex As Exception
                PrintMsg("写入数据区索引失败")
                Invoke(Sub() MessageBox.Show(ex.ToString()))
            End Try
            Invoke(Sub()
                       LockGUI(False)
                       RefreshDisplay()
                       If Not SilentMode Then MessageBox.Show("数据区索引已更新")
                   End Sub)
        End Sub)
        LockGUI()
        th.Start()
    End Sub
    Private Sub 加载外部索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 加载外部索引ToolStripMenuItem.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            Try
                PrintMsg("正在读取索引")
                Dim schtext As String = My.Computer.FileSystem.ReadAllText(OpenFileDialog1.FileName)
                PrintMsg("正在解析索引")
                Dim sch2 As ltfsindex = ltfsindex.FromSchemaText(schtext)
                PrintMsg("索引解析成功")
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
                        CurrentFilesProcessed = 0
                        CurrentBytesProcessed = 0
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
    Private Sub 格式化ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 格式化ToolStripMenuItem.Click
        If MessageBox.Show("全部数据将丢失且无法恢复，是否继续？", "警告", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            While True
                Threading.Thread.Sleep(0)
                SyncLock UFReadCount
                    If UFReadCount > 0 Then Continue While
                    UnwrittenFiles.Clear()
                    CurrentFilesProcessed = 0
                    CurrentBytesProcessed = 0
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
                    PrintMsg("格式化完成")
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
    Public Sub ImportSHA1(schhash As ltfsindex, Overwrite As Boolean)

        Dim q As New List(Of IOManager.IndexedLHashDirectory)
        q.Add(New IOManager.IndexedLHashDirectory(schema._directory(0), schhash._directory(0)))
        While q.Count > 0
            Dim qtmp As New List(Of IOManager.IndexedLHashDirectory)
            For Each d As IOManager.IndexedLHashDirectory In q
                For Each f As ltfsindex.file In d.LTFSIndexDir.contents._file
                    Try
                        For Each flookup As ltfsindex.file In d.LHash_Dir.contents._file
                            If flookup.name = f.name And flookup.length = f.length Then
                                If Not Overwrite Then
                                    If f.sha1 IsNot Nothing AndAlso f.sha1 <> "" AndAlso f.sha1.Length = 40 Then Exit For
                                End If
                                If flookup.sha1 IsNot Nothing AndAlso flookup.sha1 <> "" And flookup.sha1.Length = 40 Then
                                    PrintMsg($"{f.name}", False, $"{f.name}    {f.sha1} -> { flookup.sha1}")
                                    f.sha1 = flookup.sha1
                                End If
                                Exit For
                            End If
                        Next
                    Catch ex As Exception
                        PrintMsg(ex.ToString)
                    End Try
                Next
                For Each sd As ltfsindex.directory In d.LTFSIndexDir.contents._directory
                    For Each dlookup As ltfsindex.directory In d.LHash_Dir.contents._directory
                        If dlookup.name = sd.name Then
                            qtmp.Add(New IOManager.IndexedLHashDirectory(sd, dlookup))
                            Exit For
                        End If
                    Next
                Next
            Next
            q = qtmp
        End While
    End Sub
    Private Sub 合并SHA1ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 合并SHA1ToolStripMenuItem.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            Try
                Dim schhash As ltfsindex
                PrintMsg("正在读取索引")
                Dim s As String = My.Computer.FileSystem.ReadAllText(OpenFileDialog1.FileName)
                If s.Contains("XMLSchema") Then
                    PrintMsg("正在解析索引")
                    schhash = ltfsindex.FromXML(s)
                Else
                    PrintMsg("正在解析索引")
                    schhash = ltfsindex.FromSchemaText(s)
                End If
                Dim dr As DialogResult = MessageBox.Show("是否覆盖现有SHA1？", "提示", MessageBoxButtons.YesNoCancel)
                PrintMsg("正在导入")
                If dr = DialogResult.Yes Then
                    ImportSHA1(schhash, True)
                ElseIf dr = DialogResult.No Then
                    ImportSHA1(schhash, False)
                Else
                    PrintMsg("操作取消")
                    Exit Sub
                End If
                RefreshDisplay()
                PrintMsg("导入完成")
            Catch ex As Exception
                PrintMsg(ex.ToString)
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
        If ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 AndAlso
        ListView1.SelectedItems(0).Tag IsNot Nothing AndAlso
            TypeOf (ListView1.SelectedItems(0).Tag) Is ltfsindex.file Then

            Dim f As ltfsindex.file = ListView1.SelectedItems(0).Tag
            If f.extentinfo IsNot Nothing AndAlso f.extentinfo.Count > 0 Then
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
    Public Sub CheckFlush()
        If Threading.Interlocked.Exchange(Flush, False) Then
            Dim Loc As TapeUtils.PositionData = TapeUtils.ReadPosition(TapeDrive)
            TapeUtils.Locate(TapeDrive, Loc.BlockNumber, Loc.PartitionNumber, TapeUtils.LocateDestType.Block)
        End If
    End Sub
    Public Sub CheckClean()
        If Threading.Interlocked.Exchange(Clean, False) Then
            Dim Loc As TapeUtils.PositionData = TapeUtils.ReadPosition(TapeDrive)
            TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Unthread)
            TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.LoadThreaded)
            TapeUtils.Locate(TapeDrive, Loc.BlockNumber, Loc.PartitionNumber, TapeUtils.LocateDestType.Block)
        End If
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
    Private Sub ToolStripDropDownButton1_Click(sender As Object, e As EventArgs) Handles ToolStripDropDownButton1.Click
        If MessageBox.Show("此时取消会丢失未写完的文件，是否继续", "警告", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            StopFlag = True
        End If
    End Sub
    Private Sub ToolStripDropDownButton2_Click(sender As Object, e As EventArgs) Handles ToolStripDropDownButton2.Click
        Flush = True
    End Sub
    Private Sub ToolStripDropDownButton3_Click(sender As Object, e As EventArgs) Handles ToolStripDropDownButton3.Click
        Clean = True
    End Sub
    Private Sub ToolStripStatusLabel4_Click(sender As Object, e As EventArgs) Handles ToolStripStatusLabel4.Click
        If MessageBox.Show("确定要清零写入计数？", "确认", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            TotalBytesProcessed = 0
            TotalFilesProcessed = 0
        End If
    End Sub
    Private Sub WA0ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles WA0ToolStripMenuItem.Click
        WA0ToolStripMenuItem.Checked = True
        WA1ToolStripMenuItem.Checked = False
        WA2ToolStripMenuItem.Checked = False
        WA3ToolStripMenuItem.Checked = False
    End Sub
    Private Sub WA1ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles WA1ToolStripMenuItem.Click
        WA0ToolStripMenuItem.Checked = False
        WA1ToolStripMenuItem.Checked = True
        WA2ToolStripMenuItem.Checked = False
        WA3ToolStripMenuItem.Checked = False
    End Sub
    Private Sub WA2ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles WA2ToolStripMenuItem.Click
        WA0ToolStripMenuItem.Checked = False
        WA1ToolStripMenuItem.Checked = False
        WA2ToolStripMenuItem.Checked = True
        WA3ToolStripMenuItem.Checked = False
    End Sub
    Private Sub WA3ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles WA3ToolStripMenuItem.Click
        WA0ToolStripMenuItem.Checked = False
        WA1ToolStripMenuItem.Checked = False
        WA2ToolStripMenuItem.Checked = False
        WA3ToolStripMenuItem.Checked = True
    End Sub
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
