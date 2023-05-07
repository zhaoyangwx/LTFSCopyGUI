Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.WindowsAPICodePack.Dialogs

Public Class LTFSWriter
    Public Property TapeDrive As String = ""
    Public Property schema As ltfsindex
    Public Property plabel As ltfslabel
    Public Property Modified As Boolean = False
    Public Property OfflineMode As Boolean = False
    Public Property IndexWriteInterval As Long
        Get
            Return My.Settings.LTFSWriter_IndexWriteInterval
        End Get
        Set(value As Long)
            value = Math.Max(0, value)
            My.Settings.LTFSWriter_IndexWriteInterval = value
            If value = 0 Then
                索引间隔36GiBToolStripMenuItem.Text = "索引间隔：禁用自动索引"
            Else
                索引间隔36GiBToolStripMenuItem.Text = $"索引间隔：{IOManager.FormatSize(value)}"
            End If
            My.Settings.Save()
        End Set
    End Property

    Private _TotalBytesUnindexed As Long
    Public Property TotalBytesUnindexed As Long
        Set(value As Long)
            _TotalBytesUnindexed = value
            If value <> 0 AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then 更新数据区索引ToolStripMenuItem.Enabled = True
        End Set
        Get
            Return _TotalBytesUnindexed
        End Get
    End Property
    Public Property TotalBytesProcessed As Long = 0
    Public Property TotalFilesProcessed As Long = 0
    Public Property CurrentBytesProcessed As Long = 0
    Public Property CurrentFilesProcessed As Long = 0
    Public Property CurrentHeight As Long = 0
    Public ReadOnly Property GetPos As TapeUtils.PositionData
        Get
            Return TapeUtils.ReadPosition(TapeDrive)
        End Get
    End Property
    Public Property ExtraPartitionCount As Long = 0
    Public Property CapReduceCount As Long = 0
    Public Property CapacityRefreshInterval As Integer
        Get
            Return My.Settings.LTFSWriter_CapacityRefreshInterval
        End Get
        Set(value As Integer)
            value = Math.Max(0, value)
            My.Settings.LTFSWriter_CapacityRefreshInterval = value
            If value = 0 Then
                容量刷新间隔30sToolStripMenuItem.Text = "容量刷新间隔：禁用"
            Else
                容量刷新间隔30sToolStripMenuItem.Text = $"容量刷新间隔：{value}s"
            End If
        End Set
    End Property
    Private _SpeedLimit As Integer = 0
    Public Property SpeedLimit As Integer
        Set(value As Integer)
            value = Math.Max(0, value)
            _SpeedLimit = value
            If _SpeedLimit = 0 Then
                限速不限制ToolStripMenuItem.Text = $"限速：无限制"
            Else
                限速不限制ToolStripMenuItem.Text = $"限速：{_SpeedLimit} MiB/s"
            End If
        End Set
        Get
            Return _SpeedLimit
        End Get
    End Property
    Public Property SpeedLimitLastTriggerTime As Date = Now
    Public CheckCount As Integer = 0
    Public Property CheckCycle As Integer = 10
    Public Property CleanCycle
        Set(value)
            value = Math.Max(0, value)
            If value = 0 Then
                重装带前清洁次数3ToolStripMenuItem.Text = $"重装带前清洁次数：禁用重装带"
            Else
                重装带前清洁次数3ToolStripMenuItem.Text = $"重装带前清洁次数：{value}"
            End If
            My.Settings.LTFSWriter_CleanCycle = value
            My.Settings.Save()
        End Set
        Get
            Return My.Settings.LTFSWriter_CleanCycle
        End Get
    End Property
    Public Property HashOnWrite As Boolean
        Get
            Return 计算校验ToolStripMenuItem.Checked
        End Get
        Set(value As Boolean)
            计算校验ToolStripMenuItem.Checked = value
        End Set
    End Property

    Public Property AllowOperation As Boolean = True
    Public OperationLock As New Object
    Public Property Barcode As String = ""
    Public Property StopFlag As Boolean = False
    Public Property Pause As Boolean = False
    Public Property Flush As Boolean = False
    Public Property Clean As Boolean = False
    Public Property Clean_last As Date = Now
    Public Property Session_Start_Time As Date = Now
    Public logFile As String = My.Computer.FileSystem.CombinePath(Application.StartupPath, $"log\LTFSWriter_{Session_Start_Time.ToString("yyyyMMdd_HHmmss.fffffff")}.log")
    Public Property SilentMode As Boolean = False
    Public Property SilentAutoEject As Boolean = False
    Public BufferedBytes As Long = 0
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
        启用日志记录ToolStripMenuItem.Checked = My.Settings.LTFSWriter_LogEnabled
        总是更新数据区索引ToolStripMenuItem.Checked = My.Settings.LTFSWriter_ForceIndex
        计算校验ToolStripMenuItem.Checked = My.Settings.LTFSWriter_HashOnWriting
        异步校验CPU占用高ToolStripMenuItem.Checked = My.Settings.LTFSWriter_HashAsync
        预读文件数5ToolStripMenuItem.Text = $"预读文件数：{My.Settings.LTFSWriter_PreLoadNum}"
        文件缓存32MiBToolStripMenuItem.Text = $"文件缓存：{IOManager.FormatSize(My.Settings.LTFSWriter_PreLoadBytes)}"
        CleanCycle = CleanCycle
        IndexWriteInterval = IndexWriteInterval
        CapacityRefreshInterval = CapacityRefreshInterval
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
        My.Settings.LTFSWriter_LogEnabled = 启用日志记录ToolStripMenuItem.Checked
        My.Settings.LTFSWriter_ForceIndex = 总是更新数据区索引ToolStripMenuItem.Checked
        My.Settings.LTFSWriter_HashOnWriting = 计算校验ToolStripMenuItem.Checked
        My.Settings.LTFSWriter_HashAsync = 异步校验CPU占用高ToolStripMenuItem.Checked
        My.Settings.Save()
    End Sub
    Private Text3 As String = "", Text5 As String = ""
    Private TextT3 As String = "", TextT5 As String = ""
    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        ToolStripStatusLabel3.Text = Text3
        ToolStripStatusLabel3.ToolTipText = TextT3
        ToolStripStatusLabel5.Text = Text5
        ToolStripStatusLabel5.ToolTipText = TextT5
    End Sub
    Public Sub PrintMsg(s As String, Optional ByVal Warning As Boolean = False, Optional ByVal TooltipText As String = "", Optional ByVal LogOnly As Boolean = False, Optional ByVal ForceLog As Boolean = False)
        Me.Invoke(Sub()
                      If ForceLog OrElse My.Settings.LTFSWriter_LogEnabled Then
                          Dim logType As String = "info"
                          If Warning Then logType = "warn"
                          Dim ExtraMsg As String = ""
                          If TooltipText <> "" Then
                              ExtraMsg = $"({TooltipText})"
                          End If
                          If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(Application.StartupPath, "log")) Then
                              My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(Application.StartupPath, "log"))
                          End If
                          My.Computer.FileSystem.WriteAllText(logFile, $"{vbCrLf}{Now.ToString("yyyy-MM-dd HH:mm:ss")} {logType}> {s} {ExtraMsg}", True)
                      End If
                      If LogOnly Then Exit Sub
                      If TooltipText = "" Then TooltipText = s
                      If Not Warning Then
                          Text3 = s
                          TextT3 = TooltipText
                      Else
                          Text5 = s
                          TextT5 = TooltipText
                      End If
                  End Sub)
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Static d_last As Long = 0
        Static t_last As Long = 0
        Try
            Dim pnow As Long = TotalBytesProcessed
            If pnow = 0 Then d_last = 0
            If pnow >= d_last Then
                ddelta = pnow - d_last
                d_last = pnow
            End If
            Dim tval As Long = TotalFilesProcessed
            If tval = 0 Then t_last = 0
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

            If APToolStripMenuItem.Checked AndAlso fdelta = 0 Then
                Dim FlushNow As Boolean = True
                For j As Integer = 1 To 3
                    Dim n As Double = SpeedHistory(SpeedHistory.Count - j)
                    If n < 70 Or n > 82 Then
                        FlushNow = False
                        Exit For
                    End If
                Next
                If CapReduceCount > 0 Then ToolStripDropDownButton3.ToolTipText = $"清洁磁头{vbCrLf}检测到容量缺失次数：{CapReduceCount}{vbCrLf}"
                If CapReduceCount >= CleanCycle Then ToolStripDropDownButton3.ToolTipText &= $"上次清洁：{Clean_last.ToString("yyyy/MM/dd HH:mm:ss")}"
                Flush = FlushNow
                If FlushNow Then
                    CapReduceCount += 1
                    If CleanCycle > 0 AndAlso (CapReduceCount Mod CleanCycle = 0) Then
                        Flush = False
                        Clean = True
                    End If
                End If
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
            Dim UFile As Long = UnwrittenCount
            ToolStripStatusLabel4.Text = ""
            ToolStripStatusLabel4.Text &= $"速度:{IOManager.FormatSize(ddelta)}/s"
            ToolStripStatusLabel4.Text &= $"  累计:{IOManager.FormatSize(TotalBytesProcessed)}"
            If CurrentBytesProcessed > 0 Then ToolStripStatusLabel4.Text &= $"({IOManager.FormatSize(CurrentBytesProcessed)})"
            ToolStripStatusLabel4.Text &= $"|{TotalFilesProcessed}"
            If CurrentFilesProcessed > 0 Then ToolStripStatusLabel4.Text &= $"({CurrentFilesProcessed})"
            ToolStripStatusLabel4.Text &= $"  待处理:"
            If UFile > 0 AndAlso UFile >= CurrentFilesProcessed Then ToolStripStatusLabel4.Text &= $"[{UFile - CurrentFilesProcessed}/{UFile}]"
            ToolStripStatusLabel4.Text &= $"{ IOManager.FormatSize(Math.Max(0, USize - CurrentBytesProcessed))}/{IOManager.FormatSize(USize)}"
            ToolStripStatusLabel4.Text &= $"  待索引:{IOManager.FormatSize(TotalBytesUnindexed)}"
            ToolStripStatusLabel4.ToolTipText = ToolStripStatusLabel4.Text
            If USize > 0 AndAlso CurrentBytesProcessed >= 0 AndAlso CurrentBytesProcessed <= USize Then
                ToolStripProgressBar1.Value = CurrentBytesProcessed / USize * 10000
                ToolStripProgressBar1.ToolTipText = $"进度:{IOManager.FormatSize(CurrentBytesProcessed)}/{IOManager.FormatSize(USize)}"
            End If
            Text = GetLocInfo()
            GC.Collect()
        Catch ex As Exception
            PrintMsg(ex.ToString)
        End Try
    End Sub
    Public Class FileRecord
        Public ParentDirectory As ltfsindex.directory
        Public SourcePath As String
        Public File As ltfsindex.file
        Public Buffer As Byte()
        Private OperationLock As New Object
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
                .openforwrite = False}
            With File
                Try
                    .creationtime = finf.CreationTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
                Catch ex As Exception
                    .creationtime = Now.ToUniversalTime.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
                End Try
                Try
                    .modifytime = finf.LastWriteTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
                Catch ex As Exception
                    .modifytime = .creationtime
                End Try
                Try
                    .accesstime = finf.LastAccessTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
                Catch ex As Exception
                    .accesstime = .creationtime
                End Try
                .changetime = .modifytime
                .backuptime = Now.ToUniversalTime.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
            End With
            ParentDirectory.contents.UnwrittenFiles.Add(File)
        End Sub
        Public fs As IO.FileStream
        Public Sub Open(Optional BufferSize As Integer = 16777216)
            SyncLock OperationLock
                If fs IsNot Nothing Then Exit Sub
                fs = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
            End SyncLock
        End Sub
        Public Sub BeginOpen(Optional BufferSize As Integer = 0, Optional ByVal BlockSize As Integer = 524288)
            If File.length <= BlockSize Then Exit Sub
            SyncLock OperationLock
                If fs IsNot Nothing Then Exit Sub
            End SyncLock
            If BufferSize = 0 Then BufferSize = My.Settings.LTFSWriter_PreLoadBytes
            If BufferSize = 0 Then BufferSize = 524288
            Task.Run(Sub()
                         SyncLock OperationLock
                             If fs IsNot Nothing Then Exit Sub
                             fs = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
                         End SyncLock
                     End Sub)
        End Sub
        Public Function Read(array As Byte(), offset As Integer, count As Integer) As Integer
            Return fs.Read(array, offset, count)
        End Function
        Public Sub Close()
            fs.Close()
            fs.Dispose()
        End Sub
        Public Sub CloseAsync()
            Task.Run(Sub()
                         fs.Close()
                         fs.Dispose()
                         fs = Nothing
                     End Sub)
        End Sub
        Public Function ReadAllBytes() As Byte()
            If Buffer IsNot Nothing AndAlso Buffer.Length > 0 Then
                Dim result As Byte() = Buffer
                Buffer = {}
                Return result
            Else
                Return My.Computer.FileSystem.ReadAllBytes(SourcePath)
            End If
        End Function
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
    Public Property ProgressbarOverrideSize As ULong = 0
    Public ReadOnly Property UnwrittenSize
        Get
            If ProgressbarOverrideSize > 0 Then Return ProgressbarOverrideSize
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
    Public Property ProgressbarOverrideCount As ULong = 0
    Public ReadOnly Property UnwrittenCount
        Get
            If ProgressbarOverrideCount > 0 Then Return ProgressbarOverrideCount
            Return UnwrittenFiles.Count
        End Get
    End Property
    Dim LastRefresh As Date = Now
    Private Sub LTFSWriter_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        FileDroper = New FileDropHandler(ListView1)
        Load_Settings()
        If OfflineMode Then Exit Sub
        Try
            读取索引ToolStripMenuItem_Click(sender, e)
        Catch ex As Exception
            PrintMsg("获取分区信息出错")
        End Try

    End Sub
    Private Sub LTFSWriter_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Static ForceCloseCount As Integer = 0
        e.Cancel = False
        If Not AllowOperation Then
            If ForceCloseCount < 3 Then
                MessageBox.Show("请等待操作完成")
            Else
                If MessageBox.Show("操作进行中，是否强制退出？", "警告", MessageBoxButtons.OKCancel) = DialogResult.OK Then
                    Save_Settings()
                    e.Cancel = False
                    End
                    Exit Sub
                End If
            End If
            ForceCloseCount += 1
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
        If schema Is Nothing Then Return $"未加载索引 - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}"
        Dim info As String = $"{Barcode.TrimEnd()} ".TrimStart()
        Try
            SyncLock schema
                info &= $"索引{schema.generationnumber} - 分区{schema.location.partition} - 块{schema.location.startblock}"
                If schema.previousgenerationlocation IsNot Nothing Then
                    If schema.previousgenerationlocation.startblock > 0 Then info &= $" (此前:分区{schema.previousgenerationlocation.partition} - 块{schema.previousgenerationlocation.startblock})"
                End If
            End SyncLock
            If CurrentHeight > 0 Then info &= $" 数据高度{CurrentHeight}"
            If Modified Then info &= "*"
            info &= $" - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}"
        Catch ex As Exception
            PrintMsg("获取位置出错")
        End Try
        Return info
    End Function
    Public Sub RefreshCapacity()
        Invoke(Sub()
                   Try
                       Dim cap0 As Long = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 0).AsNumeric
                       Dim cap1 As Long
                       If ExtraPartitionCount > 0 Then
                           cap1 = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 1).AsNumeric
                           ToolStripStatusLabel2.Text = $"可用空间 P0:{IOManager.FormatSize(cap0 << 20)} P1:{IOManager.FormatSize(cap1 << 20)}"
                           ToolStripStatusLabel2.ToolTipText = $"可用空间 P0:{LTFSConfigurator.ReduceDataUnit(cap0)} P1:{LTFSConfigurator.ReduceDataUnit(cap1)}"

                       Else
                           ToolStripStatusLabel2.Text = $"可用空间 P0:{IOManager.FormatSize(cap0 << 20)}"
                           ToolStripStatusLabel2.ToolTipText = $"可用空间 P0:{LTFSConfigurator.ReduceDataUnit(cap0)}"
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
                                    t.ImageIndex = 1
                                    t.SelectedImageIndex = 1
                                    t.StateImageIndex = 1
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
                            t.ImageIndex = 0
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
            If AllowOperation Then
                RefreshCapacity()
                PrintMsg("可用容量已刷新")
            Else
                LastRefresh = Now - New TimeSpan(0, 0, CapacityRefreshInterval)
            End If
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
                       ToolStrip1.Enabled = AllowOperation
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
                            li.ImageIndex = 2
                            li.StateImageIndex = 2
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
                            li.ForeColor = f.ItemForeColor
                            If Not f.SHA1ForeColor.Equals(Color.Black) Then
                                li.UseItemStyleForSubItems = False
                                li.SubItems(3).ForeColor = f.SHA1ForeColor
                            End If
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
        If (IndexWriteInterval > 0 AndAlso TotalBytesUnindexed >= IndexWriteInterval) Or ForceFlush Then
            WriteCurrentIndex(False, False)
            TotalBytesUnindexed = 0
            Invoke(Sub() Text = GetLocInfo())
        End If
    End Sub
    Public Sub WriteCurrentIndex(Optional ByVal GotoEOD As Boolean = True, Optional ByVal ClearCurrentStat As Boolean = True)
        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
        If GotoEOD Then TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.EOD)
        Dim CurrentPos As TapeUtils.PositionData = GetPos
        PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
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
        CurrentPos = GetPos
        PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
        schema.location.startblock = CurrentPos.BlockNumber
        PrintMsg("正在生成索引")
        Dim tmpf As String = $"{Application.StartupPath}\LWI_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
        schema.SaveFile(tmpf)
        'Dim sdata As Byte() = Encoding.UTF8.GetBytes(schema.GetSerializedText())
        PrintMsg("正在写入索引")
        'TapeUtils.Write(TapeDrive, sdata, plabel.blocksize)
        TapeUtils.Write(TapeDrive, tmpf, plabel.blocksize)
        My.Computer.FileSystem.DeleteFile(tmpf)
        'While sdata.Length > 0
        '    Dim wdata As Byte() = sdata.Take(Math.Min(plabel.blocksize, sdata.Length)).ToArray
        '    sdata = sdata.Skip(Math.Min(plabel.blocksize, sdata.Length)).ToArray()
        '    TapeUtils.Write(TapeDrive, wdata)
        '    If sdata.Length = 0 Then Exit While
        'End While
        TotalBytesUnindexed = 0
        If ClearCurrentStat Then
            CurrentBytesProcessed = 0
            CurrentFilesProcessed = 0
        End If
        TapeUtils.WriteFileMark(TapeDrive)
        PrintMsg("索引写入完成")
        CurrentPos = GetPos
        CurrentHeight = CurrentPos.BlockNumber
        PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
        Modified = ExtraPartitionCount > 0
    End Sub
    Public Sub RefreshIndexPartition()
        Dim block1 As Long = schema.location.startblock
        If schema.location.partition = ltfsindex.PartitionLabel.a Then
            block1 = schema.previousgenerationlocation.startblock
        End If
        If ExtraPartitionCount > 0 Then
            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
            PrintMsg("正在定位")
            TapeUtils.Locate(TapeDrive, 3, 0, TapeUtils.LocateDestType.FileMark)
            Dim p As TapeUtils.PositionData = GetPos
            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
            TapeUtils.WriteFileMark(TapeDrive)
            PrintMsg($"Filemark Written", LogOnly:=True)
            If schema.location.partition = ltfsindex.PartitionLabel.b Then
                schema.previousgenerationlocation = New ltfsindex.PartitionDef With {.partition = schema.location.partition, .startblock = schema.location.startblock}
            End If
            p = GetPos
            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
            schema.location.startblock = p.BlockNumber
        End If
        'schema.previousgenerationlocation.partition = ltfsindex.PartitionLabel.b
        Dim block0 As Long = schema.location.startblock
        If ExtraPartitionCount > 0 Then
            schema.location.partition = ltfsindex.PartitionLabel.a
            PrintMsg("正在生成索引")
            Dim tmpf As String = $"{Application.StartupPath}\LWI_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
            schema.SaveFile(tmpf)
            'Dim sdata As Byte() = Encoding.UTF8.GetBytes(schema.GetSerializedText())
            PrintMsg("正在写入索引")
            'TapeUtils.Write(TapeDrive, sdata, plabel.blocksize)
            TapeUtils.Write(TapeDrive, tmpf, plabel.blocksize)
            My.Computer.FileSystem.DeleteFile(tmpf)
            'While sdata.Length > 0
            '    Dim wdata As Byte() = sdata.Take(Math.Min(plabel.blocksize, sdata.Length)).ToArray
            '    sdata = sdata.Skip(Math.Min(plabel.blocksize, sdata.Length)).ToArray()
            '    TapeUtils.Write(TapeDrive, wdata)
            'End While
            TapeUtils.WriteFileMark(TapeDrive)
            PrintMsg("索引写入完成")
            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
        End If
        TapeUtils.WriteVCI(TapeDrive, schema.generationnumber, block0, block1, schema.volumeuuid.ToString(), ExtraPartitionCount)
        Modified = False
    End Sub
    Public Sub UpdataAllIndex()
        If (My.Settings.LTFSWriter_ForceIndex OrElse (TotalBytesUnindexed <> 0)) AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
            PrintMsg("正在更新数据区索引")
            WriteCurrentIndex(False)
        End If
        PrintMsg("正在更新索引")
        RefreshIndexPartition()
        AutoDump()
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
            If DoEject Then
                TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Eject)
                PrintMsg("磁带已弹出")
            End If
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
                If (My.Settings.LTFSWriter_ForceIndex OrElse TotalBytesUnindexed <> 0) AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
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
    Public Property ParallelAdd As Boolean = False
    Public ExplorerComparer As New ExplorerUtils()
    Public Function IsSameFile(f As IO.FileInfo, f0 As ltfsindex.file) As Boolean
        Dim Result As Boolean = True
        If f.Name <> f0.name Then
            Result = False
        ElseIf f.Length <> f0.length Then
            Result = False
        End If
        Dim validtime As String = ""
        Try
            validtime = f.LastWriteTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
            If validtime <> f0.modifytime Then
                Result = False
            End If
        Catch ex As Exception
            validtime = f.CreationTimeUtc.ToString("yyyy-MM-ddThh:mm:ss.fffffff00Z")
            If validtime <> f0.modifytime Then
                Result = False
            End If
        End Try
        If Not Result Then
            PrintMsg($"Different File: {f0.name}|{f0.length}|{f0.modifytime}->{f.Name}|{f.Length}|{validtime}", LogOnly:=True)
        End If
        Return Result
    End Function
    Public Sub AddFile(f As IO.FileInfo, d As ltfsindex.directory, Optional ByVal OverWrite As Boolean = False)
        Try
            Dim FileExist As Boolean = False
            Dim SameFile As Boolean = False
            '检查磁带已有文件
            SyncLock d.contents._file
                For i As Integer = d.contents._file.Count - 1 To 0 Step -1
                    Dim oldf As ltfsindex.file = d.contents._file(i)
                    If oldf.name.ToLower = f.Name.ToLower Then
                        SameFile = IsSameFile(f, oldf)
                        If OverWrite And Not SameFile Then d.contents._file.RemoveAt(i)
                        FileExist = True
                    End If
                Next
            End SyncLock
            If FileExist And (SameFile OrElse Not OverWrite) Then Exit Sub
            '检查写入队列
            If Not FileExist Then
                While True
                    Threading.Thread.Sleep(0)
                    SyncLock UFReadCount
                        If UFReadCount > 0 Then Continue While
                        For i As Integer = UnwrittenFiles.Count - 1 To 0 Step -1
                            Dim oldf As FileRecord = UnwrittenFiles(i)
                            If oldf.ParentDirectory Is d AndAlso oldf.File.name.ToLower = f.Name.ToLower Then
                                oldf.ParentDirectory.contents.UnwrittenFiles.Remove(oldf.File)
                                UnwrittenFiles.RemoveAt(i)
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
        Catch ex As Exception
            Invoke(Sub() MessageBox.Show(ex.ToString()))
        End Try
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
            Threading.Interlocked.Increment(schema.highestfileuid)
        End If
        If Not ParallelAdd Then
            For Each f As IO.FileInfo In dnew1.GetFiles()
                Try
                    Dim FileExist As Boolean = False
                    Dim SameFile As Boolean = False
                    '检查已有文件
                    SyncLock dT.contents._file
                        For i As Integer = dT.contents._file.Count - 1 To 0 Step -1
                            Dim fe As ltfsindex.file = dT.contents._file(i)
                            If fe.name = f.Name Then
                                FileExist = True
                                SameFile = IsSameFile(f, fe)
                                If OverWrite And Not SameFile Then dT.contents._file.RemoveAt(i)
                            End If
                        Next
                    End SyncLock
                    If FileExist And (SameFile OrElse Not OverWrite) Then Continue For
                    '检查写入队列
                    If Not FileExist Then
                        While True
                            Threading.Thread.Sleep(0)
                            SyncLock UFReadCount
                                If UFReadCount > 0 Then Continue While
                                For i As Integer = UnwrittenFiles.Count - 1 To 0 Step -1
                                    Dim oldf As FileRecord = UnwrittenFiles(i)
                                    If oldf.ParentDirectory Is dT AndAlso oldf.File.name.ToLower = f.Name.ToLower Then
                                        oldf.ParentDirectory.contents.UnwrittenFiles.Remove(oldf.File)
                                        UnwrittenFiles.RemoveAt(i)
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
                Catch ex As Exception
                    Invoke(Sub() MessageBox.Show(ex.ToString()))
                End Try
            Next
        Else
            Parallel.ForEach(dnew1.GetFiles(),
                Sub(f As IO.FileInfo)
                    Try
                        Dim FileExist As Boolean = False
                        Dim SameFile As Boolean = False
                        '检查已有文件
                        SyncLock dT.contents._file
                            For i As Integer = dT.contents._file.Count - 1 To 0 Step -1
                                Dim fe As ltfsindex.file = dT.contents._file(i)
                                If fe.name = f.Name Then
                                    FileExist = True
                                    SameFile = IsSameFile(f, fe)
                                    If OverWrite And Not SameFile Then dT.contents._file.RemoveAt(i)
                                End If
                            Next
                        End SyncLock
                        If FileExist And (SameFile OrElse Not OverWrite) Then Exit Sub
                        '检查写入队列
                        If Not FileExist Then
                            While True
                                Threading.Thread.Sleep(0)
                                SyncLock UFReadCount
                                    If UFReadCount > 0 Then Continue While
                                    For i As Integer = UnwrittenFiles.Count - 1 To 0 Step -1
                                        Dim oldf As FileRecord = UnwrittenFiles(i)
                                        If oldf.ParentDirectory Is dT AndAlso oldf.File.name.ToLower = f.Name.ToLower Then
                                            oldf.ParentDirectory.contents.UnwrittenFiles.Remove(oldf.File)
                                            UnwrittenFiles.RemoveAt(i)
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
                    Catch ex As Exception
                        Invoke(Sub() MessageBox.Show(ex.ToString()))
                    End Try
                End Sub)
        End If
        Dim dl As List(Of IO.DirectoryInfo) = dnew1.GetDirectories().ToList()
        dl.Sort(New Comparison(Of IO.DirectoryInfo)(Function(a As IO.DirectoryInfo, b As IO.DirectoryInfo) As Integer
                                                        Return ExplorerComparer.Compare(a.Name, b.Name)
                                                    End Function))
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
            If TreeView1.SelectedNode.Parent IsNot Nothing AndAlso MessageBox.Show($"是否删除{d.name}", "确认", MessageBoxButtons.OKCancel) = DialogResult.OK Then
                Dim pd As ltfsindex.directory = TreeView1.SelectedNode.Parent.Tag
                pd.contents._directory.Remove(d)
                If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
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
                If TreeView1.SelectedNode.Parent IsNot Nothing AndAlso TreeView1.SelectedNode.Parent.Tag IsNot Nothing AndAlso TypeOf (TreeView1.SelectedNode.Parent.Tag) Is ltfsindex.directory Then
                    TreeView1.SelectedNode = TreeView1.SelectedNode.Parent
                End If
                If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
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
                If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
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
            If newname = "" Then Exit Sub
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
            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
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
                        Dim f As ltfsindex.file = ItemSelected.Tag
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
                                            Exit For
                                        End If
                                    Next
                                    Exit While
                                End SyncLock
                            End While
                        End If
                        If d.contents._file.Contains(f) Then
                            d.contents._file.Remove(f)
                            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                        End If
                    End If
                Next
            End SyncLock
            RefreshDisplay()
        End If
    End Sub
    Private Sub 添加文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 添加文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing AndAlso OpenFileDialog1.ShowDialog = DialogResult.OK Then
            Dim d As ltfsindex.directory = ListView1.Tag
            Dim overwrite As Boolean = 覆盖已有文件ToolStripMenuItem.Checked
            AddFileOrDir(d, OpenFileDialog1.FileNames, overwrite)
            'For Each fpath As String In OpenFileDialog1.FileNames
            '    Dim f As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(fpath)
            '    Try
            '        AddFile(f, d, 覆盖已有文件ToolStripMenuItem.Checked)
            '        PrintMsg("文件添加成功")
            '    Catch ex As Exception
            '        PrintMsg("文件添加失败")
            '        MessageBox.Show(ex.ToString())
            '    End Try
            'Next
            'RefreshDisplay()
        End If
    End Sub
    Public Sub AddFileOrDir(d As ltfsindex.directory, Paths As String(), Optional ByVal overwrite As Boolean = False)
        Dim th As New Threading.Thread(
                Sub()
                    StopFlag = False
                    PrintMsg($"正在添加{Paths.Length}个项目")
                    Dim numi As Integer = 0
                    Dim PList As List(Of String) = Paths.ToList()
                    PList.Sort(ExplorerComparer)
                    For Each path As String In PList
                        Dim i As Integer = Threading.Interlocked.Increment(numi)
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

                    If ParallelAdd Then UnwrittenFiles.Sort(New Comparison(Of FileRecord)(Function(a As FileRecord, b As FileRecord) As Integer
                                                                                              Return ExplorerComparer.Compare(a.SourcePath, b.SourcePath)
                                                                                          End Function))
                    RefreshDisplay()
                    PrintMsg("添加完成")
                    LockGUI(False)
                End Sub)
        LockGUI()
        th.Start()
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
            AddFileOrDir(d, Paths, overwrite)
        End If
    End Sub
    Private Sub 导入文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 导入文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing AndAlso FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            Dim dnew As IO.DirectoryInfo = My.Computer.FileSystem.GetDirectoryInfo(FolderBrowserDialog1.SelectedPath)
            Dim Paths As New List(Of String)
            For Each f As IO.FileInfo In dnew.GetFiles("*", IO.SearchOption.TopDirectoryOnly)
                Paths.Add(f.FullName)
            Next
            For Each f As IO.DirectoryInfo In dnew.GetDirectories("*", IO.SearchOption.TopDirectoryOnly)
                Paths.Add(f.FullName)
            Next
            Dim d As ltfsindex.directory = ListView1.Tag
            AddFileOrDir(d, Paths.ToArray(), 覆盖已有文件ToolStripMenuItem.Checked)
            'Try
            '    ConcatDirectory(dnew, d, 覆盖已有文件ToolStripMenuItem.Checked)
            '    PrintMsg("导入成功")
            'Catch ex As Exception
            '    PrintMsg("导入失败")
            '    MessageBox.Show(ex.ToString())
            'End Try
            'RefreshDisplay()
        End If
    End Sub
    Private Sub 添加目录ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 添加目录ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing Then
            Dim COFD As New CommonOpenFileDialog
            COFD.Multiselect = True
            COFD.IsFolderPicker = True
            If COFD.ShowDialog = CommonFileDialogResult.Ok Then
                Dim d As ltfsindex.directory = ListView1.Tag
                AddFileOrDir(ListView1.Tag, COFD.FileNames, 覆盖已有文件ToolStripMenuItem.Checked)
                'For Each dirSelected As String In COFD.FileNames
                '    Dim dnew As IO.DirectoryInfo = My.Computer.FileSystem.GetDirectoryInfo(dirSelected)
                '    Try
                '        AddDirectry(dnew, d, 覆盖已有文件ToolStripMenuItem.Checked)
                '        PrintMsg("目录添加成功")
                '    Catch ex As Exception
                '        PrintMsg("目录添加失败")
                '        MessageBox.Show(ex.ToString())
                '    End Try
                'Next
                'RefreshDisplay()
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
            Dim CreateNew As Boolean = True
            For Each fe As ltfsindex.file.extent In FileIndex.extentinfo
                If Not TapeUtils.RawDump(TapeDrive, FileName, fe.startblock, fe.byteoffset, fe.fileoffset, Math.Min(ExtraPartitionCount, fe.partition), fe.bytecount, StopFlag, plabel.blocksize,
                                         Function(BytesReaded As Long)
                                             Threading.Interlocked.Add(TotalBytesProcessed, BytesReaded)
                                             Threading.Interlocked.Add(CurrentBytesProcessed, BytesReaded)
                                             Return StopFlag
                                         End Function, CreateNew) Then
                    PrintMsg($"{FileIndex.name}提取出错")
                    Exit For
                End If
                CreateNew = False
                If StopFlag Then Exit Sub
            Next
        End If
        Dim finfo As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(FileName)
        finfo.CreationTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.creationtime)
        finfo.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.accesstime)
        finfo.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.modifytime)
        finfo.IsReadOnly = FileIndex.readonly
        Threading.Interlocked.Increment(CurrentFilesProcessed)
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
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            ProgressbarOverrideSize = 0
                            ProgressbarOverrideCount = flist.Count
                            For Each FI As ltfsindex.file In flist
                                ProgressbarOverrideSize += FI.length
                            Next
                            PrintMsg("正在提取")
                            StopFlag = False
                            For Each FileIndex As ltfsindex.file In flist
                                Dim FileName As String = My.Computer.FileSystem.CombinePath(BasePath, FileIndex.name)
                                RestoreFile(FileName, FileIndex)
                                If StopFlag Then
                                    PrintMsg("操作取消")
                                    Exit Sub
                                End If
                            Next
                        Catch ex As Exception
                            PrintMsg("提取出错")
                        End Try
                        StopFlag = False
                        ProgressbarOverrideSize = 0
                        ProgressbarOverrideCount = 0
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
                            StopFlag = False
                            Dim FileList As New List(Of FileRecord)
                            Dim selectedDir As ltfsindex.directory = TreeView1.SelectedNode.Tag
                            Dim IterDir As Action(Of ltfsindex.directory, IO.DirectoryInfo) =
                                Sub(tapeDir As ltfsindex.directory, outputDir As IO.DirectoryInfo)
                                    For Each f As ltfsindex.file In tapeDir.contents._file
                                        FileList.Add(New FileRecord With {.File = f, .SourcePath = My.Computer.FileSystem.CombinePath(outputDir.FullName, f.name)})
                                        'RestoreFile(My.Computer.FileSystem.CombinePath(outputDir.FullName, f.name), f)
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
                            PrintMsg("正在准备文件")
                            Dim ODir As String = My.Computer.FileSystem.CombinePath(FolderBrowserDialog1.SelectedPath, selectedDir.name)
                            If Not My.Computer.FileSystem.DirectoryExists(ODir) Then My.Computer.FileSystem.CreateDirectory(ODir)
                            IterDir(selectedDir, My.Computer.FileSystem.GetDirectoryInfo(ODir))
                            FileList.Sort(New Comparison(Of FileRecord)(Function(a As FileRecord, b As FileRecord) As Integer
                                                                            If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count <> 0 Then Return 0.CompareTo(1)
                                                                            If b.File.extentinfo.Count = 0 And a.File.extentinfo.Count <> 0 Then Return 1.CompareTo(0)
                                                                            If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count = 0 Then Return 0.CompareTo(0)
                                                                            If a.File.extentinfo(0).partition = ltfsindex.PartitionLabel.a And b.File.extentinfo(0).partition = ltfsindex.PartitionLabel.b Then Return 0.CompareTo(1)
                                                                            If a.File.extentinfo(0).partition = ltfsindex.PartitionLabel.b And b.File.extentinfo(0).partition = ltfsindex.PartitionLabel.a Then Return 1.CompareTo(0)
                                                                            Return a.File.extentinfo(0).startblock.CompareTo(b.File.extentinfo(0).startblock)
                                                                        End Function))
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            ProgressbarOverrideSize = 0
                            ProgressbarOverrideCount = FileList.Count
                            For Each FI As FileRecord In FileList
                                ProgressbarOverrideSize += FI.File.length
                            Next
                            PrintMsg("正在提取文件")
                            Dim c As Integer = 0
                            For Each fr As FileRecord In FileList
                                c += 1
                                PrintMsg($"正在提取 [{c}/{FileList.Count}] {fr.File.name}", False, $"正在提取 [{c}/{FileList.Count}] {fr.SourcePath}")
                                RestoreFile(fr.SourcePath, fr.File)
                                If StopFlag Then
                                    PrintMsg("操作取消")
                                    Exit Try
                                End If
                            Next
                            PrintMsg("提取完成")
                        Catch ex As Exception
                            Invoke(Sub() MessageBox.Show(ex.ToString))
                            PrintMsg("提取出错")
                        End Try
                        ProgressbarOverrideSize = 0
                        ProgressbarOverrideCount = 0
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
                        PrintMsg("索引更新出错", False, $"索引更新出错: {ex.ToString}")
                        LockGUI(False)
                    End Try
                End Sub)
        LockGUI(True)
        th.Start()
    End Sub
    Private Sub 写入数据ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 写入数据ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
            Sub()
                Dim OnWriteFinishMessage As String = ""
                Try
                    Dim StartTime As Date = Now
                    PrintMsg("", True)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg("准备写入")
                    TapeUtils.ReserveUnit(TapeDrive)
                    TapeUtils.PreventMediaRemoval(TapeDrive)
                    If schema.location.partition = ltfsindex.PartitionLabel.a Then
                        TapeUtils.Locate(TapeDrive, schema.previousgenerationlocation.startblock, schema.previousgenerationlocation.partition, TapeUtils.LocateDestType.Block)
                        schema.location.startblock = schema.previousgenerationlocation.startblock
                        schema.location.partition = schema.previousgenerationlocation.partition
                        Dim p As TapeUtils.PositionData = GetPos
                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                        PrintMsg("正在读取索引")
                        Dim tmpf As String = $"{Application.StartupPath}\LWS_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
                        TapeUtils.ReadToFileMark(TapeDrive, tmpf)
                        'Dim schraw As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                        PrintMsg("正在解析索引")
                        'Dim sch2 As ltfsindex = ltfsindex.FromSchemaText(Encoding.UTF8.GetString(schraw))
                        Dim sch2 As ltfsindex = ltfsindex.FromSchFile(tmpf)
                        My.Computer.FileSystem.DeleteFile(tmpf)
                        PrintMsg("索引解析成功")
                        schema.previousgenerationlocation = sch2.previousgenerationlocation
                        p = GetPos
                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                        CurrentHeight = p.BlockNumber
                        Invoke(Sub() Text = GetLocInfo())
                    ElseIf CurrentHeight > 0 Then
                        Dim p As TapeUtils.PositionData = GetPos
                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                        If p.BlockNumber <> CurrentHeight Then
                            TapeUtils.Locate(TapeDrive, CurrentHeight, 1, TapeUtils.LocateDestType.Block)
                            p = GetPos
                            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                        End If
                    End If
                    Invoke(Sub() 更新数据区索引ToolStripMenuItem.Enabled = True)
                    UFReadCount.Inc()
                    CurrentFilesProcessed = 0
                    CurrentBytesProcessed = 0
                    ProgressbarOverrideSize = 0
                    ProgressbarOverrideCount = 0
                    Dim WriteList As New List(Of FileRecord)
                    UFReadCount.Inc()
                    For Each fr As FileRecord In UnwrittenFiles
                        WriteList.Add(fr)
                    Next
                    UFReadCount.Dec()
                    Dim wBufferPtr As IntPtr = Marshal.AllocHGlobal(plabel.blocksize)
                    Dim BytesReaded As Integer
                    Dim PNum As Integer = My.Settings.LTFSWriter_PreLoadNum
                    If PNum > 0 Then
                        For j As Integer = 0 To PNum
                            If j < WriteList.Count Then WriteList(j).BeginOpen(BlockSize:=plabel.blocksize)
                        Next
                    End If
                    Dim HashTaskAwaitNumber As Integer = 0
                    Threading.ThreadPool.SetMaxThreads(256, 256)
                    Threading.ThreadPool.SetMinThreads(128, 128)
                    For i As Integer = 0 To WriteList.Count - 1
                        PNum = My.Settings.LTFSWriter_PreLoadNum
                        If PNum > 0 AndAlso i + PNum < WriteList.Count Then
                            WriteList(i + PNum).BeginOpen(BlockSize:=plabel.blocksize)
                        End If
                        Dim fr As FileRecord = WriteList(i)
                        Try
                            Dim finfo As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(fr.SourcePath)
                            fr.File.fileuid = schema.highestfileuid + 1
                            schema.highestfileuid += 1
                            If finfo.Length > 0 Then
                                Dim p As TapeUtils.PositionData = GetPos
                                If p.EOP Then PrintMsg("磁带即将写满", True)
                                Dim fileextent As New ltfsindex.file.extent With
                            {.partition = ltfsindex.PartitionLabel.b,
                            .startblock = p.BlockNumber,
                            .bytecount = finfo.Length,
                            .byteoffset = 0,
                            .fileoffset = 0}
                                fr.File.extentinfo.Add(fileextent)
                                PrintMsg($"正在写入 {fr.File.name}  大小 {IOManager.FormatSize(fr.File.length)}", False,
                                     $"正在写入: {fr.SourcePath}{vbCrLf}大小: {IOManager.FormatSize(fr.File.length)}{vbCrLf _
                                     }此前累计写入: {IOManager.FormatSize(TotalBytesProcessed) _
                                     } 剩余: {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed)) _
                                     } -> {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed - fr.File.length))}")
                                'write to tape
                                TapeUtils.SetBlockSize(TapeDrive, plabel.blocksize)
                                If finfo.Length <= plabel.blocksize Then
                                    Dim succ As Boolean = False
                                    Dim FileData As Byte() = fr.ReadAllBytes()
                                    While Not succ
                                        Dim sense As Byte()
                                        Try
                                            sense = TapeUtils.Write(TapeDrive, FileData)
                                        Catch ex As Exception
                                            Select Case MessageBox.Show($"写入出错：SCSI指令执行失败", "警告", MessageBoxButtons.AbortRetryIgnore)
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
                                                PrintMsg("磁带已满")
                                                Invoke(Sub() MessageBox.Show("磁带已满"))
                                                StopFlag = True
                                                Exit For
                                            Else
                                                PrintMsg("磁带即将写满", True)
                                                succ = True
                                                Exit While
                                            End If
                                        ElseIf sense(2) And &HF <> 0 Then
                                            PrintMsg($"sense err {TapeUtils.Byte2Hex(sense, True)}", Warning:=True, LogOnly:=True)
                                            Select Case MessageBox.Show($"写入出错{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}原始sense数据{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}", "警告", MessageBoxButtons.AbortRetryIgnore)
                                                Case DialogResult.Abort
                                                    Throw New Exception(TapeUtils.ParseSenseData(sense))
                                                Case DialogResult.Retry
                                                    succ = False
                                                Case DialogResult.Ignore
                                                    succ = True
                                                    Exit While
                                            End Select
                                        Else
                                            succ = True
                                        End If
                                    End While
                                    If succ AndAlso HashOnWrite Then
                                        Task.Run(Sub()
                                                     Threading.Interlocked.Increment(HashTaskAwaitNumber)
                                                     Dim sh As New IOManager.SHA1BlockwiseCalculator
                                                     sh.Propagate(FileData)
                                                     sh.ProcessFinalBlock()
                                                     fr.File.sha1 = sh.SHA1Value
                                                     Threading.Interlocked.Decrement(HashTaskAwaitNumber)
                                                 End Sub)
                                    End If
                                    If fr.fs IsNot Nothing Then fr.Close()
                                    fr.File.WrittenBytes += finfo.Length
                                    TotalBytesProcessed += finfo.Length
                                    CurrentBytesProcessed += finfo.Length
                                    TotalFilesProcessed += 1
                                    CurrentFilesProcessed += 1
                                    TotalBytesUnindexed += finfo.Length
                                Else
                                    'Dim fs As New IO.FileStream(fr.SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, 512, True)
                                    fr.Open()
                                    Dim sh As IOManager.SHA1BlockwiseCalculator = Nothing
                                    If HashOnWrite Then sh = New IOManager.SHA1BlockwiseCalculator
                                    While Not StopFlag
                                        Dim buffer(plabel.blocksize - 1) As Byte
                                        BytesReaded = fr.Read(buffer, 0, plabel.blocksize)
                                        CheckCount += 1
                                        If CheckCount >= CheckCycle Then CheckCount = 0
                                        If SpeedLimit > 0 AndAlso CheckCount = 0 Then
                                            While ((plabel.blocksize * CheckCycle / 1048576) / (Now - SpeedLimitLastTriggerTime).TotalSeconds) > SpeedLimit
                                                Threading.Thread.Sleep(0)
                                            End While
                                            SpeedLimitLastTriggerTime = Now
                                        End If
                                        If BytesReaded > 0 Then
                                            Marshal.Copy(buffer, 0, wBufferPtr, BytesReaded)
                                            Dim succ As Boolean = False
                                            While Not succ
                                                Dim sense As Byte()
                                                Try
                                                    sense = TapeUtils.Write(TapeDrive, wBufferPtr, BytesReaded, BytesReaded < plabel.blocksize)
                                                Catch ex As Exception
                                                    Select Case MessageBox.Show($"写入出错：SCSI指令执行失败", "警告", MessageBoxButtons.AbortRetryIgnore)
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
                                                If (((sense(2) >> 6) And &H1) = 1) Then
                                                    If ((sense(2) And &HF) = 13) Then
                                                        PrintMsg("磁带已满")
                                                        Invoke(Sub() MessageBox.Show("磁带已满"))
                                                        StopFlag = True
                                                        fr.Close()
                                                        Exit For
                                                    Else
                                                        PrintMsg("磁带即将写满", True)
                                                        succ = True
                                                        Exit While
                                                    End If
                                                ElseIf sense(2) And &HF <> 0 Then
                                                    Select Case MessageBox.Show($"写入出错{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}原始sense数据{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}", "警告", MessageBoxButtons.AbortRetryIgnore)
                                                        Case DialogResult.Abort
                                                            Throw New Exception(TapeUtils.ParseSenseData(sense))
                                                        Case DialogResult.Retry
                                                            succ = False
                                                        Case DialogResult.Ignore
                                                            succ = True
                                                            Exit While
                                                    End Select
                                                Else
                                                    succ = True
                                                    Exit While
                                                End If
                                            End While
                                            If sh IsNot Nothing AndAlso succ Then
                                                If 异步校验CPU占用高ToolStripMenuItem.Checked Then
                                                    sh.PropagateAsync(buffer, BytesReaded)
                                                Else
                                                    sh.Propagate(buffer, BytesReaded)
                                                End If
                                            End If
                                            CheckFlush()
                                            CheckClean(True)
                                            fr.File.WrittenBytes += BytesReaded
                                            TotalBytesProcessed += BytesReaded
                                            CurrentBytesProcessed += BytesReaded
                                            TotalBytesUnindexed += BytesReaded
                                        Else
                                            Exit While
                                        End If
                                    End While
                                    fr.CloseAsync()

                                    If HashOnWrite AndAlso sh IsNot Nothing AndAlso Not StopFlag Then
                                        Threading.Interlocked.Increment(HashTaskAwaitNumber)
                                        Task.Run(Sub()
                                                     sh.ProcessFinalBlock()
                                                     fr.File.sha1 = sh.SHA1Value
                                                     sh.StopFlag = True
                                                     Threading.Interlocked.Decrement(HashTaskAwaitNumber)
                                                 End Sub)
                                    ElseIf sh IsNot Nothing Then
                                        sh.StopFlag = True
                                    End If
                                    TotalFilesProcessed += 1
                                    CurrentFilesProcessed += 1
                                End If
                                p = GetPos
                                If p.EOP Then PrintMsg("磁带即将写满", True)
                                PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                                CurrentHeight = p.BlockNumber
                            Else
                                fr.File.sha1 = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709"
                                TotalBytesUnindexed += 1
                                TotalFilesProcessed += 1
                                CurrentFilesProcessed += 1
                            End If
                            'mark as written
                            fr.ParentDirectory.contents._file.Add(fr.File)
                            fr.ParentDirectory.contents.UnwrittenFiles.Remove(fr.File)
                            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                            CheckUnindexedDataSizeLimit()
                            Invoke(Sub()
                                       If CapacityRefreshInterval > 0 AndAlso (Now - LastRefresh).TotalSeconds > CapacityRefreshInterval Then RefreshCapacity()
                                   End Sub)
                        Catch ex As Exception
                            MessageBox.Show($"写入出错{ex.ToString}")
                            PrintMsg($"写入出错{ex.Message}")
                        End Try
                        While Pause
                            Threading.Thread.Sleep(10)
                        End While
                        If StopFlag Then
                            Exit For
                        End If
                        UnwrittenFiles.Remove(fr)
                        fr = Nothing
                        WriteList(i) = Nothing
                    Next
                    Marshal.FreeHGlobal(wBufferPtr)
                    While HashTaskAwaitNumber > 0
                        Threading.Thread.Sleep(1)
                    End While
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
                    TapeUtils.Flush(TapeDrive)
                    If Not StopFlag Then
                        Dim TimeCost As TimeSpan = Now - StartTime
                        OnWriteFinishMessage = ($"写入完成，耗时{(Math.Floor(TimeCost.TotalHours)).ToString().PadLeft(2, "0")}:{TimeCost.Minutes.ToString().PadLeft(2, "0")}:{TimeCost.Seconds.ToString().PadLeft(2, "0")}")
                        Me.Invoke(Sub() OnWriteFinished())
                    Else
                        OnWriteFinishMessage = ("写入取消")
                    End If
                Catch ex As Exception
                    MessageBox.Show($"写入出错{ex.ToString}")
                    PrintMsg($"写入出错{ex.Message}")
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
                           PrintMsg(OnWriteFinishMessage)
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
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg("正在定位索引")
                    TapeUtils.Locate(TapeDrive, schema.location.startblock, schema.location.partition, TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg("正在读取索引")
                    Dim data As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim CurrentPos As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
                    If CurrentPos.PartitionNumber < ExtraPartitionCount Then
                        Invoke(Sub() MessageBox.Show("当前为索引区，操作取消"))
                        Exit Try
                    End If
                    TapeUtils.Locate(TapeDrive, CurrentPos.BlockNumber - 1, CurrentPos.PartitionNumber, TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.WriteFileMark(TapeDrive)
                    PrintMsg($"FileMark written", LogOnly:=True)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim outputfile As String = "LTFSIndex_Load_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = My.Computer.FileSystem.CombinePath(Application.StartupPath, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    PrintMsg("正在解析索引")
                    schema = ltfsindex.FromSchFile(outputfile)
                    PrintMsg("索引解析成功")
                    Modified = False
                    Dim p As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                    CurrentHeight = p.BlockNumber
                    If ExtraPartitionCount = 0 Then
                        TapeUtils.Write(TapeDrive, {0})
                        PrintMsg($"Byte written", LogOnly:=True)
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                        TapeUtils.Locate(TapeDrive, CurrentHeight, 0, TapeUtils.LocateDestType.Block)
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
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
    Private Sub 启用日志记录ToolStripMenuItem_CheckedChanged(sender As Object, e As EventArgs) Handles 启用日志记录ToolStripMenuItem.CheckedChanged
        My.Settings.LTFSWriter_LogEnabled = 启用日志记录ToolStripMenuItem.Checked
    End Sub
    Private Sub 总是更新数据区索引ToolStripMenuItem_CheckedChanged(sender As Object, e As EventArgs) Handles 总是更新数据区索引ToolStripMenuItem.CheckedChanged
        My.Settings.LTFSWriter_ForceIndex = 总是更新数据区索引ToolStripMenuItem.Checked
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
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.Locate(TapeDrive, schema.previousgenerationlocation.startblock, schema.previousgenerationlocation.partition, TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg("正在读取索引")
                    Dim data As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim outputfile As String = "LTFSIndex_Load_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = My.Computer.FileSystem.CombinePath(Application.StartupPath, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    PrintMsg("正在解析索引")
                    schema = ltfsindex.FromSchFile(outputfile)
                    PrintMsg("索引解析成功")
                    Modified = False
                    Dim p As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                    CurrentHeight = p.BlockNumber
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
    Private Sub 读取索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 读取索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg("正在定位")
                    ExtraPartitionCount = TapeUtils.ModeSense(TapeDrive, &H11)(3)
                    TapeUtils.Locate(TapeDrive, 0, 0, TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim header As String = Encoding.ASCII.GetString(TapeUtils.ReadBlock(TapeDrive))
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim VOL1LabelLegal As Boolean = False
                    VOL1LabelLegal = (header.Length = 80)
                    If VOL1LabelLegal Then VOL1LabelLegal = header.StartsWith("VOL1")
                    If VOL1LabelLegal Then VOL1LabelLegal = (header.Substring(24, 4) = "LTFS")
                    If Not VOL1LabelLegal Then
                        PrintMsg("未找到VOL1")
                        Invoke(Sub() MessageBox.Show("非LTFS格式", "错误"))
                        LockGUI(False)
                        Exit Try
                    End If
                    TapeUtils.Locate(TapeDrive, 1, 0, TapeUtils.LocateDestType.FileMark)
                    PrintMsg("正在读取LTFS信息")
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.ReadBlock(TapeDrive)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim pltext As String = Encoding.UTF8.GetString(TapeUtils.ReadToFileMark(TapeDrive))
                    plabel = ltfslabel.FromXML(pltext)
                    TapeUtils.SetBlockSize(TapeDrive, plabel.blocksize)
                    Barcode = TapeUtils.ReadBarcode(TapeDrive)
                    PrintMsg($"Barcode = {Barcode}", LogOnly:=True)
                    PrintMsg("正在定位")
                    TapeUtils.Locate(TapeDrive, 3, 0, TapeUtils.LocateDestType.FileMark)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.ReadBlock(TapeDrive)
                    If ExtraPartitionCount = 0 Then
                        TapeUtils.Locate(TapeDrive, 0, 0, TapeUtils.LocateDestType.EOD)
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                        PrintMsg("正在读取索引")
                        Dim p As TapeUtils.PositionData = GetPos
                        Dim FM As Long = p.FileNumber
                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                        If FM <= 1 Then
                            PrintMsg("索引读取失败")
                            Invoke(Sub() MessageBox.Show("非LTFS格式", "错误"))
                            LockGUI(False)
                            Exit Try
                        End If
                        TapeUtils.Locate(TapeDrive, FM - 1, 0, TapeUtils.LocateDestType.FileMark)
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                        TapeUtils.ReadBlock(TapeDrive)
                    End If
                    PrintMsg("正在读取索引")
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim tmpf As String = $"{Application.StartupPath}\LCG_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
                    'data = TapeUtils.ReadToFileMark(TapeDrive)
                    TapeUtils.ReadToFileMark(TapeDrive, tmpf)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg("正在解析索引")
                    'schema = ltfsindex.FromSchemaText(Encoding.UTF8.GetString(data))
                    schema = ltfsindex.FromSchFile(tmpf)
                    If ExtraPartitionCount = 0 Then
                        Dim p As TapeUtils.PositionData = GetPos
                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                        CurrentHeight = p.BlockNumber
                    Else
                        CurrentHeight = -1
                    End If
                    PrintMsg("保存备份文件")
                    Dim FileName As String = ""
                    If Barcode <> "" Then
                        FileName = Barcode
                    Else
                        If schema IsNot Nothing Then
                            FileName = schema.volumeuuid.ToString()
                        End If
                    End If
                    Dim outputfile As String = $"schema\LTFSIndex_Load_{FileName}_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.schema"
                    If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema")) Then
                        My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema"))
                    End If
                    outputfile = My.Computer.FileSystem.CombinePath(Application.StartupPath, outputfile)
                    My.Computer.FileSystem.MoveFile(tmpf, outputfile)
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
                                  Text = GetLocInfo()
                                  ToolStripStatusLabel1.Text = Barcode.TrimEnd(" ")
                                  ToolStripStatusLabel1.ToolTipText = $"磁带标签:{ToolStripStatusLabel1.Text}"
                                  RefreshDisplay()
                                  RefreshCapacity()
                              End Sub)

                    PrintMsg("索引读取成功")
                Catch ex As Exception
                    PrintMsg("索引读取失败")
                    PrintMsg(ex.ToString, LogOnly:=True)
                End Try
                LockGUI(False)
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
                    Dim currentPos As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {currentPos.ToString()}", LogOnly:=True)
                    If currentPos.PartitionNumber <> 1 Then TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.Block)
                    TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.EOD)
                    PrintMsg("正在读取索引")
                    currentPos = GetPos
                    PrintMsg($"Position = {currentPos.ToString()}", LogOnly:=True)
                    Dim FM As Long = currentPos.FileNumber
                    If FM <= 1 Then
                        PrintMsg("索引读取失败")
                        Invoke(Sub() MessageBox.Show("非LTFS格式", "错误"))
                        LockGUI(False)
                        Exit Try
                    End If
                    TapeUtils.Locate(TapeDrive, FM - 1, 1, TapeUtils.LocateDestType.FileMark)
                    TapeUtils.ReadBlock(TapeDrive)
                    PrintMsg("正在读取索引")
                    data = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim outputfile As String = "schema\LTFSIndex_Load_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema")) Then
                        My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema"))
                    End If
                    outputfile = My.Computer.FileSystem.CombinePath(Application.StartupPath, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    PrintMsg("正在解析索引")
                    schema = ltfsindex.FromSchFile(outputfile)
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
    Private Sub 更新数据区索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 更新数据区索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
        Sub()
            Try
                If (My.Settings.LTFSWriter_ForceIndex OrElse TotalBytesUnindexed <> 0) AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
                    WriteCurrentIndex(False)
                    PrintMsg("已写入数据区索引")
                End If
            Catch ex As Exception
                PrintMsg("写入数据区索引失败", False, $"写入数据区索引失败: {ex.ToString}")
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
    Public Sub LoadIndexFile(FileName As String, Optional ByVal Silent As Boolean = False)
        Try
            PrintMsg("正在读取索引")
            'Dim schtext As String = My.Computer.FileSystem.ReadAllText(FileName)
            PrintMsg("正在解析索引")
            ' Dim sch2 As ltfsindex = ltfsindex.FromSchemaText(schtext)
            Dim sch2 As ltfsindex = ltfsindex.FromSchFile(FileName)
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
            RefreshDisplay()
            Modified = False
            Dim MAM090C As TapeUtils.MAMAttribute = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 8, 12, 0)
            Dim VCI As Byte() = {}
            If MAM090C IsNot Nothing Then
                VCI = MAM090C.RawData
            End If
            If Not Silent Then MessageBox.Show($"已加载索引，请自行确保索引一致性{vbCrLf}{vbCrLf}当前磁带VCI数据：{vbCrLf}{TapeUtils.Byte2Hex(VCI, True)}")
        Catch ex As Exception
            MessageBox.Show($"文件解析出错：{ex.Message}")
        End Try
    End Sub
    Private Sub 加载外部索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 加载外部索引ToolStripMenuItem.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            LoadIndexFile(OpenFileDialog1.FileName)
        End If
    End Sub
    Public Function AutoDump() As String
        Dim FileName As String = Barcode
        If FileName = "" Then FileName = schema.volumeuuid.ToString()
        Dim outputfile As String = $"schema\LTFSIndex_Autosave_{FileName _
            }_GEN{schema.generationnumber _
            }_P{schema.location.partition _
            }_B{schema.location.startblock _
            }_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.schema"
        If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema")) Then
            My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema"))
        End If
        outputfile = My.Computer.FileSystem.CombinePath(Application.StartupPath, outputfile)
        PrintMsg("正在导出")
        schema.SaveFile(outputfile)
        PrintMsg("索引已备份", False, $"索引已备份至{vbCrLf}{outputfile}")
        Return outputfile
    End Function
    Private Sub 备份当前索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 备份当前索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    Dim outputfile As String = AutoDump()
                    Me.Invoke(Sub() MessageBox.Show($"索引已备份至{vbCrLf}{outputfile}"))
                Catch ex As Exception
                    PrintMsg("索引备份失败")
                End Try
                LockGUI(False)
            End Sub)
        LockGUI()
        th.Start()
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
            Barcode = TapeUtils.ReadBarcode(TapeDrive)
            Dim VolumeLabel As String
            Dim Confirm As Boolean = False
            While Not Confirm
                Barcode = InputBox("设置标签", "磁带标签", Barcode)
                If VolumeLabel = "" Then VolumeLabel = Barcode
                VolumeLabel = InputBox("设置卷标", "LTFS卷标", VolumeLabel)

                Select Case MessageBox.Show($"磁带标签：{Barcode}{vbCrLf}LTFS卷标：{VolumeLabel}", "确认", MessageBoxButtons.YesNoCancel)
                    Case DialogResult.Yes
                        Confirm = True
                        Exit While
                    Case DialogResult.No
                        Confirm = False
                    Case DialogResult.Cancel
                        Exit Sub
                End Select
            End While
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
    Public Function ImportSHA1(schhash As ltfsindex, Overwrite As Boolean) As String
        Dim fprocessed As Integer = 0, fhash As Integer = 0
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
                        f.openforwrite = False
                        Threading.Interlocked.Increment(fprocessed)
                        If f.sha1.Length = 40 Then
                            Threading.Interlocked.Increment(fhash)
                        ElseIf fprocessed - fhash <= 5 Then
                            MessageBox.Show($"{f.fileuid}:{d.LTFSIndexDir.name}\{f.name} {f.sha1}")
                        End If
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
        If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
        Return ($"{fhash}/{fprocessed}")
    End Function
    Private Sub 合并SHA1ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 合并SHA1ToolStripMenuItem.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            Try
                Dim schhash As ltfsindex
                PrintMsg("正在读取索引")
                'Dim s As String = My.Computer.FileSystem.ReadAllText(OpenFileDialog1.FileName)
                'If s.Contains("XMLSchema") Then
                '    PrintMsg("正在解析索引")
                '    schhash = ltfsindex.FromXML(s)
                'Else
                '    PrintMsg("正在解析索引")
                '    schhash = ltfsindex.FromSchemaText(s)
                'End If
                schhash = ltfsindex.FromSchFile(OpenFileDialog1.FileName)
                Dim dr As DialogResult = MessageBox.Show("是否覆盖现有SHA1？", "提示", MessageBoxButtons.YesNoCancel)
                PrintMsg("正在导入")
                Dim result As String = ""
                If dr = DialogResult.Yes Then
                    result = ImportSHA1(schhash, True)
                ElseIf dr = DialogResult.No Then
                    result = ImportSHA1(schhash, False)
                Else
                    PrintMsg("操作取消")
                    Exit Try
                End If
                RefreshDisplay()
                PrintMsg($"导入完成 {result}")
            Catch ex As Exception
                PrintMsg(ex.ToString)
            End Try
        End If
    End Sub
    Private Sub 设置高度ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 设置高度ToolStripMenuItem.Click
        Dim p As TapeUtils.PositionData = GetPos
        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
        Dim Pos As Long = p.BlockNumber
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
                            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                            TapeUtils.Locate(TapeDrive, ext.startblock, ext.partition, TapeUtils.LocateDestType.Block)
                            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                            LockGUI(False)
                            Invoke(Sub() MessageBox.Show($"已定位到{ext.startblock}"))
                            PrintMsg($"已定位到{ext.startblock}")
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
            PrintMsg("Flush Triggered", LogOnly:=True)
            Dim Loc As TapeUtils.PositionData = GetPos
            If Loc.EOP Then PrintMsg("磁带即将写满", True)
            PrintMsg($"Position = {Loc.ToString()}", LogOnly:=True)
            TapeUtils.Locate(TapeDrive, Loc.BlockNumber, Loc.PartitionNumber, TapeUtils.LocateDestType.Block)
            RefreshCapacity()
        End If
    End Sub
    Public Sub CheckClean(Optional ByVal LockVolume As Boolean = False)
        If Threading.Interlocked.Exchange(Clean, False) Then
            If (Now - Clean_last).TotalSeconds < 300 Then Exit Sub
            PrintMsg("Clean Triggered", LogOnly:=True)
            Clean_last = Now
            Dim Loc As TapeUtils.PositionData = GetPos
            If Loc.EOP Then PrintMsg("磁带即将写满", True)
            PrintMsg($"Position = {Loc.ToString()}", LogOnly:=True)
            If Not Loc.EOP Then
                TapeUtils.AllowMediaRemoval(TapeDrive)
                TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Unthread)
                TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.LoadThreaded)
                TapeUtils.Locate(TapeDrive, Loc.BlockNumber, Loc.PartitionNumber, TapeUtils.LocateDestType.Block)
                If LockVolume Then TapeUtils.PreventMediaRemoval(TapeDrive)
            End If
            RefreshCapacity()
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
        Pause = True
        If MessageBox.Show("此时取消会丢失未写完的文件，是否继续", "警告", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            StopFlag = True
        End If
        Pause = False
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
    Public Function CalculateSHA1(FileIndex As ltfsindex.file) As String
        Dim HT As New IOManager.SHA1BlockwiseCalculator
        If FileIndex.length > 0 Then
            Dim CreateNew As Boolean = True
            If FileIndex.extentinfo.Count > 1 Then FileIndex.extentinfo.Sort(New Comparison(Of ltfsindex.file.extent)(Function(a As ltfsindex.file.extent, b As ltfsindex.file.extent) As Integer
                                                                                                                          Return a.fileoffset.CompareTo(b.fileoffset)
                                                                                                                      End Function))
            For Each fe As ltfsindex.file.extent In FileIndex.extentinfo
                Dim p As New TapeUtils.PositionData(TapeDrive)
                If p.BlockNumber <> fe.startblock OrElse p.PartitionNumber <> fe.partition Then
                    TapeUtils.Locate(TapeDrive, fe.startblock, fe.partition)
                End If
                Dim TotalBytesToRead As Long = fe.bytecount
                Dim blk As Byte() = TapeUtils.ReadBlock(TapeDrive, BlockSizeLimit:=Math.Min(plabel.blocksize, TotalBytesToRead))
                If fe.byteoffset > 0 Then blk = blk.Skip(fe.byteoffset).ToArray()
                TotalBytesToRead -= blk.Length
                HT.Propagate(blk)
                Threading.Interlocked.Add(CurrentBytesProcessed, blk.Length)
                Threading.Interlocked.Add(TotalBytesProcessed, blk.Length)
                While TotalBytesToRead > 0
                    blk = TapeUtils.ReadBlock(TapeDrive, BlockSizeLimit:=Math.Min(plabel.blocksize, TotalBytesToRead))
                    Dim blklen As Integer = blk.Length
                    If blklen > TotalBytesToRead Then blklen = TotalBytesToRead
                    TotalBytesToRead -= blk.Length
                    HT.Propagate(blk, blklen)
                    Threading.Interlocked.Add(CurrentBytesProcessed, blk.Length)
                    Threading.Interlocked.Add(TotalBytesProcessed, blk.Length)
                    If StopFlag Then Return ""
                End While
            Next
        End If
        HT.ProcessFinalBlock()
        Threading.Interlocked.Increment(CurrentFilesProcessed)
        Threading.Interlocked.Increment(TotalFilesProcessed)
        Return HT.SHA1Value
    End Function

    Private Sub 生成标签ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 生成标签ToolStripMenuItem.Click
        If My.Settings.LTFSWriter_FileLabel = "" Then
            设置标签ToolStripMenuItem_Click(sender, e)
            Exit Sub
        End If
        If ListView1.Tag IsNot Nothing Then
            Dim d As ltfsindex.directory = ListView1.Tag
            For Each dir As ltfsindex.directory In d.contents._directory
                If CInt(Val(dir.name)).ToString = dir.name Then
                    Dim fExist As Boolean = False
                    For Each f As ltfsindex.file In d.contents._file
                        If f.name = $"{dir.name}.{My.Settings.LTFSWriter_FileLabel}" Then
                            fExist = True
                            Exit For
                        End If
                    Next
                    If Not fExist Then
                        Dim emptyfile As String = My.Computer.FileSystem.CombinePath(Application.StartupPath, "empty.file")
                        My.Computer.FileSystem.WriteAllBytes(emptyfile, {}, False)
                        Dim fnew As New FileRecord(emptyfile, d)
                        fnew.File.name = $"{dir.name}.{My.Settings.LTFSWriter_FileLabel}"
                        While True
                            Threading.Thread.Sleep(0)
                            SyncLock UFReadCount
                                If UFReadCount > 0 Then Continue While
                                UnwrittenFiles.Add(fnew)
                                Exit While
                            End SyncLock
                        End While
                    End If
                End If
            Next
            PrintMsg("操作完成")
            RefreshDisplay()
        End If
    End Sub

    Private Sub 设置标签ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 设置标签ToolStripMenuItem.Click
        My.Settings.LTFSWriter_FileLabel = InputBox("设置目录标签后缀", "标签工具", My.Settings.LTFSWriter_FileLabel)
        PrintMsg($"标签后缀已修改为 .{My.Settings.LTFSWriter_FileLabel}")
    End Sub

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        导入文件ToolStripMenuItem_Click(sender, e)
    End Sub

    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        写入数据ToolStripMenuItem_Click(sender, e)
    End Sub

    Private Sub ToolStripButton4_Click(sender As Object, e As EventArgs) Handles ToolStripButton4.Click
        备份当前索引ToolStripMenuItem_Click(sender, e)
    End Sub

    Private Sub ToolStripButton3_Click(sender As Object, e As EventArgs) Handles ToolStripButton3.Click
        Dim th As New Threading.Thread(
                Sub()
                    Try
                        If (My.Settings.LTFSWriter_ForceIndex OrElse TotalBytesUnindexed <> 0) AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
                            PrintMsg("正在更新数据区索引")
                            WriteCurrentIndex(False)
                            AutoDump()
                        End If
                        PrintMsg("正在更新索引")
                        RefreshIndexPartition()
                        TapeUtils.ReleaseUnit(TapeDrive)
                        TapeUtils.AllowMediaRemoval(TapeDrive)
                        PrintMsg("索引已更新")
                        If schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.a Then 更新数据区索引ToolStripMenuItem.Enabled = False
                        TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Eject)
                        PrintMsg("磁带已弹出")
                        Invoke(Sub()
                                   LockGUI(False)
                                   RefreshDisplay()
                               End Sub)
                    Catch ex As Exception
                        PrintMsg("索引更新出错")
                        LockGUI(False)
                    End Try
                End Sub)
        LockGUI(True)
        th.Start()

    End Sub

    Private Sub ToolStripButton5_Click(sender As Object, e As EventArgs) Handles ToolStripButton5.Click
        合并SHA1ToolStripMenuItem_Click(sender, e)
    End Sub

    Private Sub 校验源文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 校验源文件ToolStripMenuItem.Click
        If FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            Dim hw As New HashTaskWindow With {.schema = schema, .BaseDirectory = FolderBrowserDialog1.SelectedPath, .TargetDirectory = "", .DisableSkipInfo = True}
            Dim p As String = ""
            If OpenFileDialog1.FileName <> "" Then p = New IO.FileInfo(OpenFileDialog1.FileName).DirectoryName
            hw.schPath = Barcode & ".schema"
            If My.Computer.FileSystem.DirectoryExists(p) Then
                hw.schPath = My.Computer.FileSystem.CombinePath(p, hw.schPath)
            End If
            hw.CheckBox2.Visible = False
            hw.CheckBox3.Visible = False
            hw.Button3.Visible = False
            hw.Button4.Visible = False
            hw.ShowDialog()
            Dim q As New List(Of ltfsindex.directory)
            Dim hcount As Integer = 0, fcount As Integer = 0
            For Each d As ltfsindex.directory In schema._directory
                q.Add(d)
            Next
            For Each f As ltfsindex.file In schema._file
                Threading.Interlocked.Increment(fcount)
                If f.sha1 IsNot Nothing AndAlso f.sha1.Length = 40 Then Threading.Interlocked.Increment(hcount)
            Next
            While q.Count > 0
                Dim q1 As New List(Of ltfsindex.directory)
                For Each d As ltfsindex.directory In q
                    For Each f1 As ltfsindex.file In d.contents._file
                        Threading.Interlocked.Increment(fcount)
                        If f1.sha1 IsNot Nothing AndAlso f1.sha1.Length = 40 Then Threading.Interlocked.Increment(hcount)
                    Next
                    For Each d1 As ltfsindex.directory In d.contents._directory
                        q1.Add(d1)
                    Next
                Next
                q = q1
            End While
            PrintMsg($"{hcount}/{fcount}")
            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
            RefreshDisplay()
        End If
    End Sub

    Private Sub ToolStripButton6_Click(sender As Object, e As EventArgs) Handles ToolStripButton6.Click
        校验源文件ToolStripMenuItem_Click(sender, e)
    End Sub

    Private Sub 限速不限制ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 限速不限制ToolStripMenuItem.Click
        Dim sin As String = InputBox("设置写入限速 (MiB/s)", "设置", SpeedLimit)
        If sin = "" Then Exit Sub
        SpeedLimit = Val(sin)
    End Sub

    Private Sub 重装带前清洁次数3ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 重装带前清洁次数3ToolStripMenuItem.Click
        CleanCycle = Val(InputBox("设置重装带前清洁次数", "设置", CleanCycle))
    End Sub
    Public Sub HashSelectedFiles(Overwrite As Boolean, ValidOnly As Boolean)
        If ListView1.SelectedItems IsNot Nothing AndAlso
                ListView1.SelectedItems.Count > 0 Then
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
                            PrintMsg("正在校验")
                            StopFlag = False
                            CurrentBytesProcessed = 0
                            CurrentFilesProcessed = 0
                            ProgressbarOverrideSize = 0
                            ProgressbarOverrideCount = flist.Count
                            For Each FI As ltfsindex.file In flist
                                ProgressbarOverrideSize += FI.length
                            Next
                            For Each FileIndex As ltfsindex.file In flist
                                If ValidOnly Then
                                    If FileIndex.sha1 = "" OrElse (Not FileIndex.SHA1ForeColor.Equals(Color.Black)) Then
                                        'Skip
                                        Threading.Interlocked.Add(CurrentBytesProcessed, FileIndex.length)
                                        Threading.Interlocked.Increment(CurrentFilesProcessed)
                                    Else
                                        Dim result As String = CalculateSHA1(FileIndex)
                                        If result <> "" Then
                                            If FileIndex.sha1 = result Then
                                                FileIndex.SHA1ForeColor = Color.DarkGreen
                                            Else
                                                FileIndex.SHA1ForeColor = Color.Red
                                                PrintMsg($"SHA1 Mismatch at fileuid={FileIndex.fileuid} filename={FileIndex.name} sha1logged={FileIndex.sha1} sha1calc={result}", ForceLog:=True)
                                            End If
                                        End If
                                    End If
                                ElseIf Overwrite Then
                                    Dim result As String = CalculateSHA1(FileIndex)
                                    If result <> "" Then
                                        FileIndex.sha1 = result
                                        FileIndex.SHA1ForeColor = Color.Blue
                                        If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                                    End If
                                Else
                                    If FileIndex.sha1 = "" Then
                                        Dim result As String = CalculateSHA1(FileIndex)
                                        If result <> "" Then
                                            FileIndex.sha1 = result
                                            FileIndex.SHA1ForeColor = Color.Blue
                                            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                                        End If
                                    Else
                                        Threading.Interlocked.Add(CurrentBytesProcessed, FileIndex.length)
                                        Threading.Interlocked.Increment(CurrentFilesProcessed)
                                    End If
                                End If

                                If StopFlag Then Exit For
                            Next
                        Catch ex As Exception
                            PrintMsg("校验出错")
                        End Try
                        ProgressbarOverrideSize = 0
                        ProgressbarOverrideCount = 0
                        StopFlag = False
                        LockGUI(False)
                        RefreshDisplay()
                        PrintMsg("校验完成")
                    End Sub)
            th.Start()
        End If
    End Sub
    Private Sub 计算并更新ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 计算并更新ToolStripMenuItem.Click
        HashSelectedFiles(True, False)
    End Sub

    Private Sub 计算并跳过已有校验ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 计算并跳过已有校验ToolStripMenuItem.Click
        HashSelectedFiles(False, False)
    End Sub
    Private Sub 仅验证ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 仅验证ToolStripMenuItem.Click
        HashSelectedFiles(False, True)
    End Sub
    Public Sub HashSelectedDir(selectedDir As ltfsindex.directory, Overwrite As Boolean, ValidateOnly As Boolean)
        Dim th As New Threading.Thread(
                            Sub()
                                PrintMsg("正在校验")
                                Try
                                    StopFlag = False
                                    Dim FileList As New List(Of FileRecord)
                                    Dim IterDir As Action(Of ltfsindex.directory, String) =
                                        Sub(tapeDir As ltfsindex.directory, outputDir As String)
                                            For Each f As ltfsindex.file In tapeDir.contents._file
                                                FileList.Add(New FileRecord With {.File = f, .SourcePath = outputDir & "\" & f.name})
                                                'RestoreFile(My.Computer.FileSystem.CombinePath(outputDir.FullName, f.name), f)
                                            Next
                                            For Each d As ltfsindex.directory In tapeDir.contents._directory
                                                Dim dirOutput As String = outputDir & "\" & d.name
                                                IterDir(d, dirOutput)
                                            Next
                                        End Sub
                                    PrintMsg("正在准备文件")
                                    Dim ODir As String = selectedDir.name
                                    'If Not My.Computer.FileSystem.DirectoryExists(ODir) Then My.Computer.FileSystem.CreateDirectory(ODir)
                                    IterDir(selectedDir, ODir)
                                    FileList.Sort(New Comparison(Of FileRecord)(Function(a As FileRecord, b As FileRecord) As Integer
                                                                                    If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count <> 0 Then Return 0.CompareTo(1)
                                                                                    If b.File.extentinfo.Count = 0 And a.File.extentinfo.Count <> 0 Then Return 1.CompareTo(0)
                                                                                    If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count = 0 Then Return 0.CompareTo(0)
                                                                                    If a.File.extentinfo(0).partition = ltfsindex.PartitionLabel.a And b.File.extentinfo(0).partition = ltfsindex.PartitionLabel.b Then Return 0.CompareTo(1)
                                                                                    If a.File.extentinfo(0).partition = ltfsindex.PartitionLabel.b And b.File.extentinfo(0).partition = ltfsindex.PartitionLabel.a Then Return 1.CompareTo(0)
                                                                                    Return a.File.extentinfo(0).startblock.CompareTo(b.File.extentinfo(0).startblock)
                                                                                End Function))
                                    CurrentBytesProcessed = 0
                                    CurrentFilesProcessed = 0
                                    ProgressbarOverrideSize = 0
                                    ProgressbarOverrideCount = FileList.Count
                                    For Each FI As FileRecord In FileList
                                        ProgressbarOverrideSize += FI.File.length
                                    Next
                                    PrintMsg("正在校验")
                                    Dim c As Integer = 0
                                    For Each fr As FileRecord In FileList
                                        c += 1
                                        PrintMsg($"正在校验 [{c}/{FileList.Count}] {fr.File.name} 大小：{IOManager.FormatSize(fr.File.length)}", False, $"正在校验 [{c}/{FileList.Count}] {fr.SourcePath} 大小：{fr.File.length}")
                                        If ValidateOnly Then
                                            If fr.File.sha1 = "" OrElse (Not fr.File.SHA1ForeColor.Equals(Color.Black)) Then
                                                'skip
                                                Threading.Interlocked.Add(CurrentBytesProcessed, fr.File.length)
                                                Threading.Interlocked.Increment(CurrentFilesProcessed)
                                            Else
                                                Dim result As String = CalculateSHA1(fr.File)
                                                If result <> "" Then
                                                    If fr.File.sha1 = result Then
                                                        fr.File.SHA1ForeColor = Color.Green
                                                    Else
                                                        fr.File.SHA1ForeColor = Color.Red
                                                        PrintMsg($"SHA1 Mismatch at fileuid={fr.File.fileuid} filename={fr.File.name} sha1logged={fr.File.sha1} sha1calc={result}", ForceLog:=True)
                                                    End If
                                                End If
                                            End If
                                        ElseIf Overwrite Then
                                            Dim result As String = CalculateSHA1(fr.File)
                                            fr.File.sha1 = result
                                            fr.File.SHA1ForeColor = Color.Blue
                                            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                                        Else
                                            If fr.File.sha1 = "" Then
                                                Dim result As String = CalculateSHA1(fr.File)
                                                fr.File.sha1 = result
                                                fr.File.SHA1ForeColor = Color.Blue
                                                If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                                            Else
                                                Threading.Interlocked.Add(CurrentBytesProcessed, fr.File.length)
                                                Threading.Interlocked.Increment(CurrentFilesProcessed)
                                            End If
                                        End If

                                        If StopFlag Then
                                            PrintMsg("操作取消")
                                            Exit Try
                                        End If
                                    Next
                                    PrintMsg("校验完成")
                                Catch ex As Exception
                                    Invoke(Sub() MessageBox.Show(ex.ToString))
                                    PrintMsg("校验出错")
                                End Try
                                ProgressbarOverrideSize = 0
                                ProgressbarOverrideCount = 0
                                LockGUI(False)
                                RefreshDisplay()
                            End Sub)
        LockGUI()
        th.Start()
    End Sub
    Private Sub 计算并更新ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 计算并更新ToolStripMenuItem1.Click
        If TreeView1.SelectedNode IsNot Nothing Then
            Dim selectedDir As ltfsindex.directory = TreeView1.SelectedNode.Tag
            HashSelectedDir(selectedDir, True, False)
        End If
    End Sub

    Private Sub 跳过已有校验ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 跳过已有校验ToolStripMenuItem.Click
        If TreeView1.SelectedNode IsNot Nothing Then
            Dim selectedDir As ltfsindex.directory = TreeView1.SelectedNode.Tag
            HashSelectedDir(selectedDir, False, False)
        End If
    End Sub

    Private Sub 仅验证ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 仅验证ToolStripMenuItem1.Click
        If TreeView1.SelectedNode IsNot Nothing Then
            Dim selectedDir As ltfsindex.directory = TreeView1.SelectedNode.Tag
            HashSelectedDir(selectedDir, False, True)
        End If
    End Sub

    Private Sub 复制选中信息ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 复制选中信息ToolStripMenuItem.Click
        Dim result As New StringBuilder
        If ListView1.Tag IsNot Nothing AndAlso
        ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 Then
            SyncLock ListView1.SelectedItems
                For Each ItemSelected As ListViewItem In ListView1.SelectedItems
                    If ItemSelected.Tag IsNot Nothing AndAlso TypeOf (ItemSelected.Tag) Is ltfsindex.file Then
                        Dim f As ltfsindex.file = ItemSelected.Tag
                        result.AppendLine(f.name)
                    End If
                Next
            End SyncLock
        End If
        Clipboard.SetText(result.ToString)
    End Sub

    Private Sub 统计ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 统计ToolStripMenuItem.Click
        If TreeView1.SelectedNode IsNot Nothing Then
            Dim d As ltfsindex.directory = TreeView1.SelectedNode.Tag
            Dim fnum As Long = 0, fbytes As Long = 0
            Dim q As New List(Of ltfsindex.directory)
            q.Add(d)
            While q.Count > 0
                Dim q2 As New List(Of ltfsindex.directory)
                For Each qd As ltfsindex.directory In q
                    Threading.Interlocked.Add(fnum, qd.contents._file.Count)
                    For Each qf As ltfsindex.file In qd.contents._file
                        Threading.Interlocked.Add(fbytes, qf.length)
                    Next
                    q2.AddRange(qd.contents._directory)
                Next
                q = q2
            End While
            MessageBox.Show($"{d.name}{vbCrLf}文件数：{fnum}{vbCrLf}大小：{fbytes} 字节 ({IOManager.FormatSize(fbytes)})")
        End If
    End Sub

    Private Sub 预读文件数5ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 预读文件数5ToolStripMenuItem.Click
        Dim s As String = InputBox("设置预读文件数", "设置", My.Settings.LTFSWriter_PreLoadNum)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_PreLoadNum = Val(s)
        预读文件数5ToolStripMenuItem.Text = $"预读文件数：{My.Settings.LTFSWriter_PreLoadNum}"
    End Sub

    Private Sub 文件缓存32MiBToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 文件缓存32MiBToolStripMenuItem.Click
        Dim s As String = InputBox("设置文件缓存", "设置", My.Settings.LTFSWriter_PreLoadBytes)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_PreLoadBytes = Val(s)
        If My.Settings.LTFSWriter_PreLoadBytes = 0 Then My.Settings.LTFSWriter_PreLoadBytes = 4096
        文件缓存32MiBToolStripMenuItem.Text = $"文件缓存：{IOManager.FormatSize(My.Settings.LTFSWriter_PreLoadBytes)}"
    End Sub


    Private Sub 索引间隔36GiBToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 索引间隔36GiBToolStripMenuItem.Click
        IndexWriteInterval = Val(InputBox("设置索引间隔（字节）", "设置", IndexWriteInterval))
    End Sub

    Private Sub 容量刷新间隔30sToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 容量刷新间隔30sToolStripMenuItem.Click
        Dim s As String = InputBox("设置容量刷新间隔（秒）", "设置", CapacityRefreshInterval)
        If s = "" Then Exit Sub
        CapacityRefreshInterval = Val(s)
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
