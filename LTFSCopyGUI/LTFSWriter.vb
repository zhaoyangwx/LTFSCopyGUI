Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Text
Imports Fsp.Interop
Imports Microsoft.WindowsAPICodePack.Dialogs

Public Class LTFSWriter
    Public Property TapeDrive As String = ""
    Public Property schema As ltfsindex
    Public Property plabel As New ltfslabel With {.blocksize = 524288}
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
                索引间隔36GiBToolStripMenuItem.Text = ResText_NoIndex.Text
            Else
                索引间隔36GiBToolStripMenuItem.Text = $"{ResText_IndexInterval.Text}{IOManager.FormatSize(value)}"
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
                容量刷新间隔30sToolStripMenuItem.Text = ResText_CRDisabled.Text
            Else
                容量刷新间隔30sToolStripMenuItem.Text = $"{ResText_CRIntv.Text}{value}s"
            End If
        End Set
    End Property
    Private _SpeedLimit As Integer = 0
    Public Property SpeedLimit As Integer
        Set(value As Integer)
            value = Math.Max(0, value)
            _SpeedLimit = value
            If _SpeedLimit = 0 Then
                限速不限制ToolStripMenuItem.Text = ResText_NoSLim.Text
            Else
                限速不限制ToolStripMenuItem.Text = $"{ResText_SLim.Text}{_SpeedLimit} MiB/s"
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
                重装带前清洁次数3ToolStripMenuItem.Text = ResText_RBCoff.Text
            Else
                重装带前清洁次数3ToolStripMenuItem.Text = $"{ResText_RBC.Text}{value}"
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
    Public Property DisablePartition As Boolean
        Get
            Return My.Settings.LTFSWriter_DisablePartition
        End Get
        Set(value As Boolean)
            My.Settings.LTFSWriter_DisablePartition = value
            My.Settings.Save()
            TapeUtils.AllowPartition = Not DisablePartition
        End Set
    End Property
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
    Public Event LTFSLoaded()
    Public Event WriteFinished()
    Public Event TapeEjected()

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
        预读文件数5ToolStripMenuItem.Text = $"{ResText_PFC.Text}{My.Settings.LTFSWriter_PreLoadNum}"
        文件缓存32MiBToolStripMenuItem.Text = $"{ResText_FB.Text}{IOManager.FormatSize(My.Settings.LTFSWriter_PreLoadBytes)}"
        禁用分区ToolStripMenuItem.Checked = DisablePartition
        速度下限ToolStripMenuItem.Text = $"{ResText_SMin.Text}{My.Settings.LTFSWriter_AutoCleanDownLim} MiB/s"
        速度上限ToolStripMenuItem.Text = $"{ResText_SMax.Text}{My.Settings.LTFSWriter_AutoCleanUpperLim} MiB/s"
        持续时间ToolStripMenuItem.Text = $"{ResText_STime.Text}{My.Settings.LTFSWriter_AutoCleanTimeThreashould}s"
        去重SHA1ToolStripMenuItem.Checked = My.Settings.LTFSWriter_DeDupe
        Chart1.Titles(1).Text = ResText_SpeedBT.Text
        Chart1.Titles(2).Text = ResText_FileRateBT.Text
        TapeUtils.AllowPartition = Not DisablePartition
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
                For j As Integer = 1 To My.Settings.LTFSWriter_AutoCleanTimeThreashould
                    Dim n As Double = SpeedHistory(SpeedHistory.Count - j)
                    If n < My.Settings.LTFSWriter_AutoCleanDownLim Or n > My.Settings.LTFSWriter_AutoCleanUpperLim Then
                        FlushNow = False
                        Exit For
                    End If
                Next
                If CapReduceCount > 0 Then
                    ToolStripDropDownButton3.ToolTipText = $"{ResText_C0.Text}{vbCrLf}{ResText_C1.Text}{CapReduceCount}{vbCrLf}"
                    If CapReduceCount >= CleanCycle Then ToolStripDropDownButton3.ToolTipText &= $"{ResText_C2.Text}{Clean_last.ToString("yyyy/MM/dd HH:mm:ss")}"
                End If
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
            ToolStripStatusLabel4.Text = " "
            ToolStripStatusLabel4.Text &= $"{ResText_S0.Text}{IOManager.FormatSize(ddelta)}/s"
            ToolStripStatusLabel4.Text &= $"  {ResText_S1.Text}{IOManager.FormatSize(TotalBytesProcessed)}"
            If CurrentBytesProcessed > 0 Then ToolStripStatusLabel4.Text &= $"({IOManager.FormatSize(CurrentBytesProcessed)})"
            ToolStripStatusLabel4.Text &= $"|{TotalFilesProcessed}"
            If CurrentFilesProcessed > 0 Then ToolStripStatusLabel4.Text &= $"({CurrentFilesProcessed})"
            ToolStripStatusLabel4.Text &= $"  {ResText_S2.Text}"
            If UFile > 0 AndAlso UFile >= CurrentFilesProcessed Then ToolStripStatusLabel4.Text &= $"[{UFile - CurrentFilesProcessed}/{UFile}]"
            ToolStripStatusLabel4.Text &= $"{ IOManager.FormatSize(Math.Max(0, USize - CurrentBytesProcessed))}/{IOManager.FormatSize(USize)}"
            ToolStripStatusLabel4.Text &= $"  {ResText_S3.Text}{IOManager.FormatSize(TotalBytesUnindexed)}"
            ToolStripStatusLabel4.ToolTipText = ToolStripStatusLabel4.Text
            If USize > 0 AndAlso CurrentBytesProcessed >= 0 AndAlso CurrentBytesProcessed <= USize Then
                ToolStripProgressBar1.Value = CurrentBytesProcessed / USize * 10000
                ToolStripProgressBar1.ToolTipText = $"{ResText_S4.Text}{IOManager.FormatSize(CurrentBytesProcessed)}/{IOManager.FormatSize(USize)}"
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
        Public Buffer As Byte() = Nothing
        Private OperationLock As New Object
        Public Sub RemoveUnwritten()
            ParentDirectory.contents.UnwrittenFiles.Remove(File)
        End Sub
        Public Sub New()

        End Sub
        Public Sub New(Path As String, ParentDir As ltfsindex.directory)
            If Not Path.StartsWith("\\") Then Path = $"\\?\{Path}"
            ParentDirectory = ParentDir
            SourcePath = Path
            Dim finf As IO.FileInfo = New IO.FileInfo(SourcePath)
            File = New ltfsindex.file With {
                .name = finf.Name,
                .fileuid = -1,
                .length = finf.Length,
                .readonly = False,
                .openforwrite = False}
            With File
                Try
                    .creationtime = finf.CreationTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
                Catch ex As Exception
                    .creationtime = Now.ToUniversalTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
                End Try
                Try
                    .modifytime = finf.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
                Catch ex As Exception
                    .modifytime = .creationtime
                End Try
                Try
                    .accesstime = finf.LastAccessTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
                Catch ex As Exception
                    .accesstime = .creationtime
                End Try
                .changetime = .modifytime
                .backuptime = Now.ToUniversalTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
                Try
                    If IO.File.Exists(Path & ".xattr") Then
                        Dim x As String = IO.File.ReadAllText(Path & ".xattr")
                        Dim xlist As List(Of ltfsindex.file.xattr) = ltfsindex.file.xattr.FromXMLList(x)
                        If .extendedattributes Is Nothing Then .extendedattributes = New List(Of ltfsindex.file.xattr)
                        .extendedattributes.AddRange(xlist)
                    End If
                Catch ex As Exception
                    MessageBox.Show(ex.ToString())
                End Try
            End With
            ParentDirectory.contents.UnwrittenFiles.Add(File)
        End Sub
        Public fs As IO.FileStream
        Public fsB As IO.BufferedStream
        Public fsPreRead As IO.FileStream
        Public PreReadOffset As Long = 0
        Public PreReadByteCount As Long = 0
        Public PreReadOffsetLock As New Object
        Public Event PreReadFinished()
        'Public ReadOnly Property PreReadEnabled
        '    Get
        '        Return (My.Settings.LTFSWriter_PreLoadNum = 0)
        '    End Get
        'End Property
        Const PreReadBufferSize As Long = 16777216
        Const PreReadBlockSize As Long = 8388608
        Public PreReadBuffer As Byte() = Nothing
        Public Sub PreReadThread()
            If PreReadBuffer Is Nothing Then ReDim PreReadBuffer(PreReadBufferSize * 2 - 1)
            While True
                Dim rBytes As Long = fsPreRead.Read(PreReadBuffer, PreReadByteCount Mod PreReadBufferSize, PreReadBlockSize)
                If rBytes = 0 Then Exit While
                Threading.Interlocked.Add(PreReadByteCount, rBytes)
                While PreReadByteCount - PreReadOffset >= PreReadBufferSize
                    Threading.Thread.Sleep(1)
                End While
            End While
            RaiseEvent PreReadFinished()
        End Sub
        Public Sub Open(Optional BufferSize As Integer = 16777216)
            SyncLock OperationLock
                If fs IsNot Nothing Then Exit Sub
                fs = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
                'If PreReadEnabled Then
                fsPreRead = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
                Task.Run(Sub() PreReadThread())
                ' Else
                fsB = New IO.BufferedStream(fs, PreReadBufferSize)
                'End If
            End SyncLock
        End Sub
        Public Sub BeginOpen(Optional BufferSize As Integer = 0, Optional ByVal BlockSize As Integer = 524288)
            If File.length <= BlockSize Then
                If Buffer IsNot Nothing Then Exit Sub
                Buffer = My.Computer.FileSystem.ReadAllBytes(SourcePath)
                Exit Sub
            End If
            SyncLock OperationLock
                If fs IsNot Nothing Then Exit Sub
            End SyncLock
            If BufferSize = 0 Then BufferSize = My.Settings.LTFSWriter_PreLoadBytes
            If BufferSize = 0 Then BufferSize = 524288
            Task.Run(Sub()
                         SyncLock OperationLock
                             If fs IsNot Nothing Then Exit Sub
                             fs = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
                             'If PreReadEnabled Then
                             fsPreRead = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
                             Task.Run(Sub() PreReadThread())
                             'Else
                             fsB = New IO.BufferedStream(fs, PreReadBufferSize)
                             'End If
                         End SyncLock
                     End Sub)
        End Sub
        Public Function Read(array As Byte(), offset As Integer, count As Integer) As Integer
            'If PreReadEnabled Then
            SyncLock PreReadOffsetLock
                PreReadOffset = Math.Max(fs.Position, PreReadOffset)
            End SyncLock
            'Return fs.Read(array, offset, count)
            'Else
            Return fsB.Read(array, offset, count)
            'End If
        End Function
        Public Sub Close()
            If fsB IsNot Nothing Then
                fsB.Close()
                fsB.Dispose()
                fsB = Nothing
            End If

            fs.Close()
            fs.Dispose()
            fs = Nothing
            If fsPreRead IsNot Nothing Then
                fsPreRead.Close()
                fsPreRead.Dispose()
                fsPreRead = Nothing
            End If
        End Sub
        Public Sub CloseAsync()
            Task.Run(Sub()
                         fs.Close()
                         fs.Dispose()
                         fs = Nothing
                         'If PreReadEnabled Then
                         fsPreRead.Close()
                         fsPreRead.Dispose()
                         fsPreRead = Nothing
                         'Else
                         fsB.Close()
                         fsB.Dispose()
                         fsB = Nothing
                         ' End If
                     End Sub)
        End Sub
        Public Function ReadAllBytes() As Byte()
            If Buffer IsNot Nothing AndAlso Buffer.Length > 0 Then
                Dim result As Byte() = Buffer
                Buffer = Nothing
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
    Public Property UnwrittenSizeOverrideValue As ULong = 0
    Public ReadOnly Property UnwrittenSize
        Get
            If UnwrittenSizeOverrideValue > 0 Then Return UnwrittenSizeOverrideValue
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
    Public Property UnwrittenCountOverwriteValue As ULong = 0
    Public ReadOnly Property UnwrittenCount
        Get
            If UnwrittenCountOverwriteValue > 0 Then Return UnwrittenCountOverwriteValue
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
            PrintMsg(ResText_ErrP.Text)
        End Try

    End Sub
    Private Sub LTFSWriter_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Static ForceCloseCount As Integer = 0
        e.Cancel = False
        If Not AllowOperation Then
            If ForceCloseCount < 3 Then
                MessageBox.Show(ResText_X0.Text)
            Else
                If MessageBox.Show(ResText_X1.Text, ResText_Warning.Text, MessageBoxButtons.OKCancel) = DialogResult.OK Then
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
            If MessageBox.Show(ResText_X2.Text, ResText_Warning.Text, MessageBoxButtons.YesNo) = DialogResult.No Then
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
            If MessageBox.Show(ResText_X3.Text, ResText_Warning.Text, MessageBoxButtons.YesNo) = DialogResult.No Then
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
        If schema Is Nothing Then Return $"{ResText_NIndex.Text} - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}{My.Settings.License}"
        Dim info As String = $"{Barcode.TrimEnd()} ".TrimStart()
        If TapeDrive <> "" Then info &= $"[{TapeDrive}] "
        Try
            SyncLock schema
                info &= $"{ResText_Index.Text}{schema.generationnumber} - {ResText_Partition.Text}{schema.location.partition} - {ResText_Block.Text}{schema.location.startblock}"
                If schema.previousgenerationlocation IsNot Nothing Then
                    If schema.previousgenerationlocation.startblock > 0 Then info &= $" ({ResText_Previous.Text}:{ResText_Partition.Text}{schema.previousgenerationlocation.partition} - {ResText_Block.Text}{schema.previousgenerationlocation.startblock})"
                End If
            End SyncLock
            If CurrentHeight > 0 Then info &= $" {ResText_WritePointer.Text}{CurrentHeight}"
            If Modified Then info &= "*"
            info &= $" - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}{My.Settings.License}"
        Catch ex As Exception
            PrintMsg(ResText_RPosErr.Text)
        End Try
        Return info
    End Function
    Public Sub RefreshCapacity()
        Invoke(Sub()
                   Try
                       Dim cap0 As Long = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 0).AsNumeric
                       Dim cap1 As Long
                       Dim loss As Long
                       Dim CMInfo As New TapeUtils.CMParser(TapeDrive)
                       Dim nLossDS As Long = 0
                       Dim DataSize As New List(Of Long)
                       If CMInfo.CartridgeMfgData.CartridgeTypeAbbr = "CU" Then Exit Try
                       Dim StartBlock As Integer = 0
                       Dim CurrSize As Long = 0
                       Dim gw As Boolean = False
                       For wn As Integer = 0 To CMInfo.a_NWraps - 1
                           Dim StartBlockStr As String = StartBlock.ToString()
                           If CMInfo.TapeDirectoryData.CapacityLoss(wn) = -1 Or CMInfo.TapeDirectoryData.CapacityLoss(wn) = -3 Then StartBlockStr = ""
                           Dim EndBlock As Integer = StartBlock + CMInfo.TapeDirectoryData.WrapEntryInfo(wn).RecCount + CMInfo.TapeDirectoryData.WrapEntryInfo(wn).FileMarkCount - 1
                           If CMInfo.TapeDirectoryData.CapacityLoss(wn) = -2 Then EndBlock += 1
                           StartBlock += CMInfo.TapeDirectoryData.WrapEntryInfo(wn).RecCount + CMInfo.TapeDirectoryData.WrapEntryInfo(wn).FileMarkCount
                           If CMInfo.TapeDirectoryData.CapacityLoss(wn) >= 0 Then
                               nLossDS += Math.Max(0, CMInfo.a_SetsPerWrap - CMInfo.TapeDirectoryData.DatasetsOnWrapData(wn).Data)
                               CurrSize += CMInfo.TapeDirectoryData.DatasetsOnWrapData(wn).Data
                               'wares.Append($" { (100 - CMInfo.TapeDirectoryData.CapacityLoss(wn)).ToString("f2").PadLeft(7)}%  |")
                           ElseIf CMInfo.TapeDirectoryData.CapacityLoss(wn) = -1 Then
                               StartBlock = 0
                           ElseIf CMInfo.TapeDirectoryData.CapacityLoss(wn) = -2 Then
                               CurrSize += CMInfo.TapeDirectoryData.DatasetsOnWrapData(wn).Data
                           ElseIf CMInfo.TapeDirectoryData.CapacityLoss(wn) = -3 Then
                               StartBlock = 0
                               If gw Then
                                   DataSize.Add(CurrSize)
                                   CurrSize = 0
                                   gw = False
                               Else
                                   gw = True
                               End If
                           End If
                       Next
                       loss = nLossDS * CMInfo.CartridgeMfgData.KB_PER_DATASET * 1000
                       If ExtraPartitionCount > 0 Then
                           cap1 = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 1).AsNumeric
                           ToolStripStatusLabel2.Text = $"{ResText_CapRem.Text} P0:{IOManager.FormatSize(cap0 << 20)} P1:{IOManager.FormatSize(cap1 << 20)} Loss:{IOManager.FormatSize(loss)}"
                           ToolStripStatusLabel2.ToolTipText = $"{ResText_CapRem.Text} P0:{LTFSConfigurator.ReduceDataUnit(cap0)} P1:{LTFSConfigurator.ReduceDataUnit(cap1)} Loss:{IOManager.FormatSize(loss)}"

                       Else
                           ToolStripStatusLabel2.Text = $"{ResText_CapRem.Text} P0:{IOManager.FormatSize(cap0 << 20)} Loss:{IOManager.FormatSize(loss)}"
                           ToolStripStatusLabel2.ToolTipText = $"{ResText_CapRem.Text} P0:{LTFSConfigurator.ReduceDataUnit(cap0)} Loss:{IOManager.FormatSize(loss)}"
                       End If
                       LastRefresh = Now
                   Catch ex As Exception
                       PrintMsg(ResText_RCErr.Text)
                   End Try
               End Sub)

    End Sub
    Public Function GetCapacityMegaBytes() As Long
        If ExtraPartitionCount > 0 Then
            Return TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 1).AsNumeric
        Else
            Return TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 0).AsNumeric
        End If
    End Function
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
                    ToolStripStatusLabel4.Text = $"{ResText_DNW.Text} {IOManager.FormatSize(UnwrittenSize)}"
                    ToolStripStatusLabel4.ToolTipText = ToolStripStatusLabel4.Text
                Catch ex As Exception
                    PrintMsg(ResText_RDErr.Text)
                End Try

            End Sub)
    End Sub
    Private Sub ToolStripStatusLabel2_Click(sender As Object, e As EventArgs) Handles ToolStripStatusLabel2.Click
        Try
            If AllowOperation Then
                RefreshCapacity()
                PrintMsg(ResText_CRef.Text)
            Else
                LastRefresh = Now - New TimeSpan(0, 0, CapacityRefreshInterval)
            End If
        Catch ex As Exception
            PrintMsg(ResText_CRefErr.Text)
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
                PrintMsg(ResText_NavErr.Text)
            End Try
        End If
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        TriggerTreeView1Event()
    End Sub
    Private Sub TreeView1_Click(sender As Object, e As EventArgs) Handles TreeView1.Click
        TriggerTreeView1Event()
    End Sub
    Public Function CheckUnindexedDataSizeLimit(Optional ByVal ForceFlush As Boolean = False) As Boolean
        If (IndexWriteInterval > 0 AndAlso TotalBytesUnindexed >= IndexWriteInterval) Or ForceFlush Then
            WriteCurrentIndex(False, False)
            TotalBytesUnindexed = 0
            Invoke(Sub() Text = GetLocInfo())
            Return True
        End If
        Return False
    End Function
    Public Sub WriteCurrentIndex(Optional ByVal GotoEOD As Boolean = True, Optional ByVal ClearCurrentStat As Boolean = True)
        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
        If GotoEOD Then TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.EOD)
        Dim CurrentPos As TapeUtils.PositionData = GetPos
        PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
        If ExtraPartitionCount > 0 AndAlso schema IsNot Nothing AndAlso schema.location.partition <> CurrentPos.PartitionNumber Then
            Throw New Exception($"{ResText_CurPos.Text}p{CurrentPos.PartitionNumber}b{CurrentPos.BlockNumber}{ResText_IndexNAllowed.Text}")
            Exit Sub
        End If
        If ExtraPartitionCount > 0 AndAlso schema IsNot Nothing AndAlso schema.location.startblock >= CurrentPos.BlockNumber Then
            Throw New Exception($"{ResText_CurPos.Text}p{CurrentPos.PartitionNumber}b{CurrentPos.BlockNumber}{ResText_IndexNAllowed.Text}")
            Exit Sub
        End If
        TapeUtils.WriteFileMark(TapeDrive)
        schema.generationnumber += 1
        schema.updatetime = Now.ToUniversalTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
        schema.location.partition = ltfsindex.PartitionLabel.b
        schema.previousgenerationlocation = New ltfsindex.PartitionDef With {.partition = schema.location.partition, .startblock = schema.location.startblock}
        CurrentPos = GetPos
        PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
        schema.location.startblock = CurrentPos.BlockNumber
        PrintMsg(ResText_GI.Text)
        Dim tmpf As String = $"{Application.StartupPath}\LWI_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
        schema.SaveFile(tmpf)
        'Dim sdata As Byte() = Encoding.UTF8.GetBytes(schema.GetSerializedText())
        PrintMsg(ResText_WI.Text)
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
        PrintMsg(ResText_WIF.Text)
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
            PrintMsg(ResText_Locating.Text)
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
            PrintMsg(ResText_GI.Text)
            Dim tmpf As String = $"{Application.StartupPath}\LWI_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
            schema.SaveFile(tmpf)
            'Dim sdata As Byte() = Encoding.UTF8.GetBytes(schema.GetSerializedText())
            PrintMsg(ResText_WI.Text)
            'TapeUtils.Write(TapeDrive, sdata, plabel.blocksize)
            TapeUtils.Write(TapeDrive, tmpf, plabel.blocksize)
            My.Computer.FileSystem.DeleteFile(tmpf)
            'While sdata.Length > 0
            '    Dim wdata As Byte() = sdata.Take(Math.Min(plabel.blocksize, sdata.Length)).ToArray
            '    sdata = sdata.Skip(Math.Min(plabel.blocksize, sdata.Length)).ToArray()
            '    TapeUtils.Write(TapeDrive, wdata)
            'End While
            TapeUtils.WriteFileMark(TapeDrive)
            PrintMsg(ResText_WIF.Text)
            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
        End If
        TapeUtils.WriteVCI(TapeDrive, schema.generationnumber, block0, block1, schema.volumeuuid.ToString(), ExtraPartitionCount)
        Modified = False
    End Sub
    Public Sub UpdataAllIndex()
        If (My.Settings.LTFSWriter_ForceIndex OrElse (TotalBytesUnindexed <> 0)) AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
            PrintMsg(ResText_UDI.Text)
            WriteCurrentIndex(False)
        End If
        PrintMsg(ResText_UI.Text)
        RefreshIndexPartition()
        AutoDump()
        TapeUtils.ReleaseUnit(TapeDrive)
        TapeUtils.AllowMediaRemoval(TapeDrive)
        PrintMsg(ResText_IUd.Text)
        If schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.a Then 更新数据区索引ToolStripMenuItem.Enabled = False
        If SilentMode Then
            If SilentAutoEject Then
                TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Eject)
                RaiseEvent TapeEjected()
            End If
        Else
            Dim DoEject As Boolean = False
            Invoke(Sub()
                       DoEject = WA3ToolStripMenuItem.Checked OrElse MessageBox.Show(ResText_PEj.Text, ResText_Hint.Text, MessageBoxButtons.OKCancel) = DialogResult.OK
                   End Sub)
            If DoEject Then
                TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Eject)
                PrintMsg(ResText_Ejd.Text)
                RaiseEvent TapeEjected()
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
                    TapeUtils.Flush(TapeDrive)
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
            validtime = f.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
            If validtime <> f0.modifytime Then
                Result = False
            End If
        Catch ex As Exception
            validtime = f.CreationTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
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
            If f.Extension.ToLower = ".xattr" Then Exit Sub
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
                  .creationtime = dnew1.CreationTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
                  .fileuid = schema.highestfileuid + 1,
                  .accesstime = dnew1.LastAccessTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
                  .modifytime = dnew1.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
                  .changetime = .modifytime,
                  .backuptime = Now.ToUniversalTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
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
                    If f.Extension.ToLower = ".xattr" Then Continue For
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
                        If f.Extension.ToLower = ".xattr" Then Exit Sub
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
                                            .creationtime = dn.CreationTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
                                            .fileuid = schema.highestfileuid + 1,
                                            .accesstime = dn.LastAccessTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
                                            .modifytime = dn.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
                                            .changetime = .modifytime,
                                            .backuptime = Now.ToUniversalTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
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
            If TreeView1.SelectedNode.Parent IsNot Nothing AndAlso MessageBox.Show($"{ResText_DelConfrm.Text}{d.name}", ResText_Confirm.Text, MessageBoxButtons.OKCancel) = DialogResult.OK Then
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
            Dim s As String = InputBox(ResText_DirName.Text, ResText_RenameDir.Text, d.name)
            If s <> "" Then
                If s = d.name Then Exit Sub
                If (s.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) Then
                    MessageBox.Show(ResText_DirNIllegal.Text)
                    Exit Sub
                End If
                If TreeView1.SelectedNode.Parent IsNot Nothing Then
                    Dim pd As ltfsindex.directory = TreeView1.SelectedNode.Parent.Tag
                    SyncLock pd.contents._directory
                        For Each d2 As ltfsindex.directory In pd.contents._directory
                            If d2 IsNot d And d2.name = s Then
                                MessageBox.Show(ResText_DirNExist.Text)
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
            Dim newname As String = InputBox(ResText_NFName.Text, ResText_Rename.Text, f.name)
            If newname = f.name Then Exit Sub
            If newname = "" Then Exit Sub
            If (newname.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) Then
                MessageBox.Show(ResText_FNIllegal.Text)
                Exit Sub
            End If
            SyncLock d.contents._file
                For Each allf As ltfsindex.file In d.contents._file
                    If allf IsNot f And allf.name.ToLower = newname.ToLower Then
                        MessageBox.Show(ResText_FNExist.Text)
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
        MessageBox.Show($"{ResText_DelConfrm.Text}{ListView1.SelectedItems.Count}{ResText_Files_C.Text}", ResText_Warning.Text, MessageBoxButtons.OKCancel) = DialogResult.OK Then
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
                    PrintMsg($"{ResText_Adding.Text}{Paths.Length}{ResText_Items_x.Text}")
                    Dim numi As Integer = 0
                    Dim PList As List(Of String) = Paths.ToList()
                    PList.Sort(ExplorerComparer)
                    For Each path As String In PList
                        If Not path.StartsWith("\\") Then path = $"\\?\{path}"
                        Dim i As Integer = Threading.Interlocked.Increment(numi)
                        If StopFlag Then Exit For
                        Try
                            If My.Computer.FileSystem.FileExists(path) Then
                                Dim f As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(path)
                                PrintMsg($"{ResText_Adding.Text} [{i}/{Paths.Length}] {f.Name}")
                                AddFile(f, d, overwrite)
                            ElseIf My.Computer.FileSystem.DirectoryExists(path) Then
                                Dim f As IO.DirectoryInfo = My.Computer.FileSystem.GetDirectoryInfo(path)
                                PrintMsg($"{ResText_Adding.Text} [{i}/{Paths.Length}] {f.Name}")
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
                    PrintMsg(ResText_AddFin.Text)
                    LockGUI(False)
                End Sub)
        LockGUI()
        th.Start()
    End Sub
    Private Sub ListView1_DragEnter(sender As Object, e As DragEventArgs) Handles ListView1.DragEnter
        If MenuStrip1.Enabled = False Then
            PrintMsg(ResText_DragNA.Text)
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
                Dim dirs As New List(Of String)
                For Each fn As String In COFD.FileNames
                    dirs.Add(fn)
                Next
                AddFileOrDir(ListView1.Tag, dirs.ToArray(), 覆盖已有文件ToolStripMenuItem.Checked)
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
            Dim s As String = InputBox(ResText_DirName.Text, ResText_NewDir.Text, "")
            If s <> "" Then
                If (s.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) Then
                    MessageBox.Show(ResText_DirNIllegal.Text)
                    Exit Sub
                End If
                Dim d As ltfsindex.directory = ListView1.Tag
                SyncLock d.contents._directory
                    For Each dold As ltfsindex.directory In d.contents._directory
                        If dold IsNot d And dold.name = s Then
                            MessageBox.Show(ResText_DirNExist.Text)
                            Exit Sub
                        End If
                    Next
                End SyncLock

                Dim newdir As New ltfsindex.directory With {
                    .name = s,
                    .creationtime = Now.ToUniversalTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z"),
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
    Dim RestorePosition As TapeUtils.PositionData
    Public Sub RestoreFile(FileName As String, FileIndex As ltfsindex.file)
        If Not FileName.StartsWith("\\") Then FileName = $"\\?\{FileName}"
        Dim FileExist As Boolean = True
        If Not IO.File.Exists(FileName) Then
            FileExist = False
        Else
            Dim finfo As New IO.FileInfo(FileName)
            If finfo.Length <> FileIndex.length Then
                FileExist = False
            ElseIf finfo.CreationTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z") <> FileIndex.creationtime Then
                FileExist = False
            ElseIf finfo.LastAccessTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z") <> FileIndex.accesstime Then
                FileExist = False
            ElseIf finfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z") <> FileIndex.modifytime Then
                FileExist = False
            End If
            If Not FileExist Then PrintMsg($"{ResText_OverwritingDF.Text}{FileName} {finfo.Length}->{FileIndex.length}{vbCrLf _
                                                  }ct{finfo.CreationTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")}->{FileIndex.creationtime}{vbCrLf _
                                                  }at{finfo.LastAccessTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")}->{FileIndex.accesstime}{vbCrLf _
                                                  }wt{finfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")}->{FileIndex.modifytime}{vbCrLf _
                                                  }", LogOnly:=True)
        End If
        If FileExist Then
            Threading.Interlocked.Increment(CurrentFilesProcessed)
            Threading.Interlocked.Increment(TotalFilesProcessed)
            Exit Sub
        End If
        If IO.File.Exists(FileName) Then
            IO.File.Delete(FileName)
        End If
        IO.File.WriteAllBytes(FileName, {})
        If FileIndex.length > 0 Then
            Dim reffile As String = ""
            If FileIndex.TempObj IsNot Nothing AndAlso TypeOf FileIndex.TempObj Is ltfsindex.file.refFile Then reffile = CType(FileIndex.TempObj, ltfsindex.file.refFile).FileName.ToString()
            If reffile <> "" AndAlso IO.File.Exists(reffile) Then
                IO.File.Copy(reffile, FileName, True)
                Dim finfo As New IO.FileInfo(FileName)
                finfo.CreationTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.creationtime)
                finfo.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.accesstime)
                finfo.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.modifytime)
                finfo.IsReadOnly = FileIndex.readonly
                Threading.Interlocked.Add(TotalBytesProcessed, FileIndex.length)
                Threading.Interlocked.Add(CurrentBytesProcessed, FileIndex.length)
            Else
                If FileIndex.TempObj Is Nothing OrElse TypeOf FileIndex.TempObj IsNot ltfsindex.file.refFile Then FileIndex.TempObj = New ltfsindex.file.refFile()
                CType(FileIndex.TempObj, ltfsindex.file.refFile).FileName = FileName
                Dim fs As New IO.FileStream(FileName, IO.FileMode.OpenOrCreate, IO.FileAccess.ReadWrite, IO.FileShare.Read, 8388608, IO.FileOptions.Asynchronous)
                For Each fe As ltfsindex.file.extent In FileIndex.extentinfo
                    Dim succ As Boolean = False
                    Do
                        Dim BlockAddress As Long = fe.startblock
                        Dim ByteOffset As Long = fe.byteoffset
                        Dim FileOffset As Long = fe.fileoffset
                        Dim Partition As Long = Math.Min(ExtraPartitionCount, fe.partition)
                        Dim TotalBytes As Long = fe.bytecount
                        'Dim p As New TapeUtils.PositionData(TapeDrive)
                        If RestorePosition.BlockNumber <> BlockAddress OrElse RestorePosition.PartitionNumber <> Partition Then
                            TapeUtils.Locate(TapeDrive, BlockAddress, Partition, TapeUtils.LocateDestType.Block)
                            RestorePosition = New TapeUtils.PositionData(TapeDrive)
                        End If
                        fs.Seek(FileOffset, IO.SeekOrigin.Begin)
                        Dim ReadedSize As Long = 0
                        While (ReadedSize < TotalBytes + ByteOffset) And Not StopFlag
                            Dim len As Integer = Math.Min(plabel.blocksize, TotalBytes + ByteOffset - ReadedSize)
                            Dim Data As Byte() = TapeUtils.ReadBlock(TapeDrive, Nothing, len, True)
                            SyncLock RestorePosition
                                RestorePosition.BlockNumber += 1
                            End SyncLock
                            If Data.Length <> len OrElse len = 0 Then
                                PrintMsg($"Error reading at p{RestorePosition.PartitionNumber}b{RestorePosition.BlockNumber}: readed length {Data.Length} should be {len}", LogOnly:=True)
                                succ = False
                                Exit Do
                            End If
                            ReadedSize += len
                            fs.Write(Data, ByteOffset, len - ByteOffset)

                            Threading.Interlocked.Add(TotalBytesProcessed, len - ByteOffset)
                            Threading.Interlocked.Add(CurrentBytesProcessed, len - ByteOffset)
                            ByteOffset = 0
                            While Pause
                                Threading.Thread.Sleep(10)
                            End While
                        End While
                        If StopFlag Then
                            fs.Close()
                            IO.File.Delete(FileName)
                            succ = True
                            Exit Do
                        End If
                        succ = True
                        Exit Do
                    Loop

                    If Not succ Then
                        PrintMsg($"{FileIndex.name}{ResText_RestoreErr.Text}")
                        Exit For
                    End If
                    'If Not TapeUtils.RawDump(TapeDrive, FileName, fe.startblock, fe.byteoffset, fe.fileoffset, Math.Min(ExtraPartitionCount, fe.partition), fe.bytecount, StopFlag, plabel.blocksize,
                    '                         Function(BytesReaded As Long)
                    '                             Threading.Interlocked.Add(TotalBytesProcessed, BytesReaded)
                    '                             Threading.Interlocked.Add(CurrentBytesProcessed, BytesReaded)
                    '                             Return StopFlag
                    '                         End Function, CreateNew, False) Then
                    '    PrintMsg($"{FileIndex.name}提取出错")
                    '    Exit For
                    'End If
                    If StopFlag Then Exit Sub
                Next
                fs.Flush()
                fs.Close()
                Task.Run(Sub()
                             Dim finfo As New IO.FileInfo(FileName)
                             finfo.CreationTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.creationtime)
                             finfo.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.accesstime)
                             finfo.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.modifytime)
                             finfo.IsReadOnly = FileIndex.readonly
                         End Sub)
            End If

        Else
            Dim finfo As New IO.FileInfo(FileName)
            finfo.CreationTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.creationtime)
            finfo.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.accesstime)
            finfo.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.modifytime)
            finfo.IsReadOnly = FileIndex.readonly
        End If

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
                            UnwrittenSizeOverrideValue = 0
                            UnwrittenCountOverwriteValue = flist.Count
                            For Each FI As ltfsindex.file In flist
                                UnwrittenSizeOverrideValue += FI.length
                            Next
                            PrintMsg(ResText_Restoring.Text)
                            StopFlag = False
                            TapeUtils.ReserveUnit(TapeDrive)
                            TapeUtils.PreventMediaRemoval(TapeDrive)
                            RestorePosition = New TapeUtils.PositionData(TapeDrive)
                            For Each FileIndex As ltfsindex.file In flist
                                Dim FileName As String = My.Computer.FileSystem.CombinePath(BasePath, FileIndex.name)
                                RestoreFile(FileName, FileIndex)
                                If StopFlag Then
                                    PrintMsg(ResText_OpCancelled.Text)
                                    Exit Sub
                                End If
                            Next
                        Catch ex As Exception
                            PrintMsg(ResText_RestoreErr.Text)
                        End Try
                        TapeUtils.AllowMediaRemoval(TapeDrive)
                        TapeUtils.ReleaseUnit(TapeDrive)
                        StopFlag = False
                        UnwrittenSizeOverrideValue = 0
                        UnwrittenCountOverwriteValue = 0
                        LockGUI(False)
                        PrintMsg(ResText_RestFin.Text)
                        Invoke(Sub() MessageBox.Show(ResText_RestFin.Text))
                    End Sub)
            th.Start()
        End If
    End Sub
    Private Sub 提取ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 提取ToolStripMenuItem1.Click
        If TreeView1.SelectedNode IsNot Nothing AndAlso FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            Dim th As New Threading.Thread(
                    Sub()
                        PrintMsg(ResText_Restoring.Text)
                        Try
                            StopFlag = False
                            Dim FileList As New List(Of FileRecord)
                            Dim selectedDir As ltfsindex.directory = TreeView1.SelectedNode.Tag
                            Dim IterDir As Action(Of ltfsindex.directory, IO.DirectoryInfo) =
                                Sub(tapeDir As ltfsindex.directory, outputDir As IO.DirectoryInfo)
                                    For Each f As ltfsindex.file In tapeDir.contents._file
                                        f.TempObj = New ltfsindex.file.refFile() With {.FileName = ""}
                                        FileList.Add(New FileRecord With {.File = f, .SourcePath = My.Computer.FileSystem.CombinePath(outputDir.FullName, f.name)})
                                        'RestoreFile(My.Computer.FileSystem.CombinePath(outputDir.FullName, f.name), f)
                                    Next
                                    For Each d As ltfsindex.directory In tapeDir.contents._directory
                                        Dim thisDir As String = My.Computer.FileSystem.CombinePath(outputDir.FullName, d.name)
                                        Dim dirOutput As IO.DirectoryInfo
                                        Dim RestoreTimeStamp As Boolean = Not My.Computer.FileSystem.DirectoryExists(thisDir)
                                        If RestoreTimeStamp Then IO.Directory.CreateDirectory(thisDir)
                                        dirOutput = My.Computer.FileSystem.GetDirectoryInfo(thisDir)
                                        IterDir(d, dirOutput)
                                        If RestoreTimeStamp Then
                                            dirOutput.CreationTimeUtc = TapeUtils.ParseTimeStamp(d.creationtime)
                                            dirOutput.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(d.modifytime)
                                            dirOutput.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(d.accesstime)
                                        End If
                                    Next
                                End Sub
                            PrintMsg(ResText_PrepFile.Text)
                            Dim ODir As String = My.Computer.FileSystem.CombinePath(FolderBrowserDialog1.SelectedPath, selectedDir.name)
                            If Not ODir.StartsWith("\\") Then ODir = $"\\?\{ODir}"
                            If Not My.Computer.FileSystem.DirectoryExists(ODir) Then IO.Directory.CreateDirectory(ODir)
                            IterDir(selectedDir, My.Computer.FileSystem.GetDirectoryInfo(ODir))
                            FileList.Sort(New Comparison(Of FileRecord)(Function(a As FileRecord, b As FileRecord) As Integer
                                                                            If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count <> 0 Then Return 0.CompareTo(1)
                                                                            If b.File.extentinfo.Count = 0 And a.File.extentinfo.Count <> 0 Then Return 1.CompareTo(0)
                                                                            If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count = 0 Then Return 0.CompareTo(0)
                                                                            If a.File.extentinfo(0).partition = ltfsindex.PartitionLabel.a And b.File.extentinfo(0).partition = ltfsindex.PartitionLabel.b Then Return 0.CompareTo(1)
                                                                            If a.File.extentinfo(0).partition = ltfsindex.PartitionLabel.b And b.File.extentinfo(0).partition = ltfsindex.PartitionLabel.a Then Return 1.CompareTo(0)
                                                                            Return a.File.extentinfo(0).startblock.CompareTo(b.File.extentinfo(0).startblock)
                                                                        End Function))
                            For i As Integer = 1 To FileList.Count - 1
                                If FileList(i).File.length = FileList(i - 1).File.length AndAlso FileList(i).File.sha1 = FileList(i - 1).File.sha1 Then
                                    FileList(i).File.TempObj = FileList(i - 1).File.TempObj
                                End If
                            Next
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            UnwrittenSizeOverrideValue = 0
                            UnwrittenCountOverwriteValue = FileList.Count
                            For Each FI As FileRecord In FileList
                                UnwrittenSizeOverrideValue += FI.File.length
                            Next
                            PrintMsg(ResText_RestFile.Text)
                            Dim c As Integer = 0
                            TapeUtils.ReserveUnit(TapeDrive)
                            TapeUtils.PreventMediaRemoval(TapeDrive)
                            RestorePosition = New TapeUtils.PositionData(TapeDrive)
                            For Each fr As FileRecord In FileList
                                c += 1
                                PrintMsg($"{ResText_Restoring.Text} [{c}/{FileList.Count}] {fr.File.name}", False, $"{ResText_Restoring.Text} [{c}/{FileList.Count}] {fr.SourcePath}")
                                RestoreFile(fr.SourcePath, fr.File)
                                If StopFlag Then
                                    PrintMsg(ResText_OpCancelled.Text)
                                    Exit Try
                                End If
                            Next
                            PrintMsg(ResText_RestFin.Text)
                        Catch ex As Exception
                            Invoke(Sub() MessageBox.Show(ex.ToString))
                            PrintMsg(ResText_RestoreErr.Text)
                        End Try
                        TapeUtils.AllowMediaRemoval(TapeDrive)
                        TapeUtils.ReleaseUnit(TapeDrive)
                        UnwrittenSizeOverrideValue = 0
                        UnwrittenCountOverwriteValue = 0
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
                        PrintMsg(ResText_IUErr.Text, False, $"{ResText_IUErr.Text}: {ex.ToString}")
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
                    PrintMsg(ResText_PrepW.Text)
                    TapeUtils.ReserveUnit(TapeDrive)
                    TapeUtils.PreventMediaRemoval(TapeDrive)
                    If schema.location.partition = ltfsindex.PartitionLabel.a Then
                        TapeUtils.Locate(TapeDrive, schema.previousgenerationlocation.startblock, schema.previousgenerationlocation.partition, TapeUtils.LocateDestType.Block)
                        schema.location.startblock = schema.previousgenerationlocation.startblock
                        schema.location.partition = schema.previousgenerationlocation.partition
                        Dim p As TapeUtils.PositionData = GetPos
                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                        PrintMsg(ResText_RI.Text)
                        Dim tmpf As String = $"{Application.StartupPath}\LWS_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
                        TapeUtils.ReadToFileMark(TapeDrive, tmpf)
                        'Dim schraw As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                        PrintMsg(ResText_AI.Text)
                        'Dim sch2 As ltfsindex = ltfsindex.FromSchemaText(Encoding.UTF8.GetString(schraw))
                        Dim sch2 As ltfsindex = ltfsindex.FromSchFile(tmpf)
                        My.Computer.FileSystem.DeleteFile(tmpf)
                        PrintMsg(ResText_AISucc.Text)
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
                    Else
                        Dim p As TapeUtils.PositionData = GetPos
                        If MessageBox.Show($"{ResText_CurPos.Text}P{p.PartitionNumber} B{p.BlockNumber}{ResText_NHWrn.Text}", ResText_WriteWarning.Text, MessageBoxButtons.OKCancel) = DialogResult.OK Then

                        Else
                            LockGUI(False)
                            Exit Sub
                        End If
                    End If
                    Invoke(Sub() 更新数据区索引ToolStripMenuItem.Enabled = True)
                    UFReadCount.Inc()
                    CurrentFilesProcessed = 0
                    CurrentBytesProcessed = 0
                    UnwrittenSizeOverrideValue = 0
                    UnwrittenCountOverwriteValue = 0
                    If UnwrittenFiles.Count > 0 Then
                        Dim WriteList As New List(Of FileRecord)
                        UFReadCount.Inc()
                        For Each fr As FileRecord In UnwrittenFiles
                            WriteList.Add(fr)
                        Next
                        UFReadCount.Dec()
                        UnwrittenCountOverwriteValue = UnwrittenCount
                        UnwrittenSizeOverrideValue = UnwrittenSize
                        Dim wBufferPtr As IntPtr = Marshal.AllocHGlobal(plabel.blocksize)

                        Dim PNum As Integer = My.Settings.LTFSWriter_PreLoadNum
                        If PNum > 0 Then
                            For j As Integer = 0 To PNum
                                If j < WriteList.Count Then WriteList(j).BeginOpen(BlockSize:=plabel.blocksize)
                            Next
                        End If
                        Dim HashTaskAwaitNumber As Integer = 0
                        Threading.ThreadPool.SetMaxThreads(256, 256)
                        Threading.ThreadPool.SetMinThreads(128, 128)
                        Dim ExitForFlag As Boolean = False
                        'DeDupe
                        Dim AllFile As New List(Of ltfsindex.file)
                        If My.Settings.LTFSWriter_DeDupe Then
                            Dim q As New List(Of ltfsindex.directory)
                            For Each d As ltfsindex.directory In schema._directory
                                q.Add(d)
                            Next
                            For Each f As ltfsindex.file In schema._file
                                AllFile.Add(f)
                            Next
                            While q.Count > 0
                                Dim q2 As New List(Of ltfsindex.directory)
                                For Each d As ltfsindex.directory In q
                                    For Each f As ltfsindex.file In d.contents._file
                                        AllFile.Add(f)
                                    Next
                                    For Each d2 As ltfsindex.directory In d.contents._directory
                                        q2.Add(d2)
                                    Next
                                Next
                                q = q2
                            End While
                        End If

                        Dim p As New TapeUtils.PositionData(TapeDrive)
                        TapeUtils.SetBlockSize(TapeDrive, plabel.blocksize)
                        For i As Integer = 0 To WriteList.Count - 1
                            If i < WriteList.Count - 1 Then
                                Dim CFNum As Integer = i
                                AddHandler WriteList(CFNum).PreReadFinished,
                                Sub()
                                    WriteList(CFNum + 1).BeginOpen()
                                End Sub
                            End If
                            If ExitForFlag Then Exit For
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
                                    'Dim p As New TapeUtils.PositionData(TapeDrive)
                                    If p.EOP Then PrintMsg(ResText_EWEOM.Text, True)
                                    Dim dupe As Boolean = False
                                    If My.Settings.LTFSWriter_DeDupe Then
                                        Dim dupeFile As ltfsindex.file = Nothing
                                        Dim sha1value As String = ""
                                        For Each fref As ltfsindex.file In AllFile
                                            If fref.length = finfo.Length AndAlso fref.sha1 <> "" Then
                                                PrintMsg($"{ResText_CHashing.Text}: {fr.File.name}  {ResText_Size.Text} {IOManager.FormatSize(fr.File.length)}")
                                                If sha1value = "" Then sha1value = IOManager.SHA1(fr.SourcePath)
                                                If fref.sha1.Equals(sha1value) Then
                                                    fr.File.sha1 = sha1value
                                                    dupe = True
                                                    dupeFile = fref
                                                End If
                                            End If
                                            If dupe Then Exit For
                                        Next
                                        If dupe AndAlso dupeFile IsNot Nothing Then
                                            For Each ext As ltfsindex.file.extent In dupeFile.extentinfo
                                                fr.File.extentinfo.Add(ext)
                                            Next
                                            If fr.fs IsNot Nothing Then fr.Close()
                                            PrintMsg($"{ResText_Skip.Text} {fr.File.name}  {ResText_Size.Text} {IOManager.FormatSize(fr.File.length)}", False,
                                                 $"{ResText_Skip.Text}: {fr.SourcePath}{vbCrLf}{ResText_Size.Text}: {IOManager.FormatSize(fr.File.length)}{vbCrLf _
                                                 }{ResText_WrittenTotal.Text}: {IOManager.FormatSize(TotalBytesProcessed) _
                                                 } {ResText_Remaining.Text}: {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed)) _
                                                 } -> {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed - fr.File.length))}")
                                            TotalBytesProcessed += finfo.Length
                                            CurrentBytesProcessed += finfo.Length
                                            TotalFilesProcessed += 1
                                            CurrentFilesProcessed += 1
                                            'TotalBytesUnindexed += finfo.Length
                                        Else
                                            AllFile.Add(fr.File)
                                        End If
                                    End If
                                    If Not dupe Then
                                        Dim fileextent As New ltfsindex.file.extent With
                                            {.partition = ltfsindex.PartitionLabel.b,
                                            .startblock = p.BlockNumber,
                                            .bytecount = finfo.Length,
                                            .byteoffset = 0,
                                            .fileoffset = 0}
                                        fr.File.extentinfo.Add(fileextent)
                                        PrintMsg($"{ResText_Writing.Text} {fr.File.name}  {ResText_Size.Text} {IOManager.FormatSize(fr.File.length)}", False,
                                             $"{ResText_Writing.Text}: {fr.SourcePath}{vbCrLf}{ResText_Size.Text}: {IOManager.FormatSize(fr.File.length)}{vbCrLf _
                                             }{ResText_WrittenTotal.Text}: {IOManager.FormatSize(TotalBytesProcessed) _
                                             } {ResText_Remaining.Text}: {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed)) _
                                             } -> {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed - fr.File.length))}")
                                        'write to tape
                                        If finfo.Length <= plabel.blocksize Then
                                            Dim succ As Boolean = False
                                            Dim FileData As Byte() = fr.ReadAllBytes()
                                            While Not succ
                                                Dim sense As Byte()
                                                Try
                                                    sense = TapeUtils.Write(TapeDrive, FileData)
                                                    SyncLock p
                                                        p.BlockNumber += 1
                                                    End SyncLock
                                                Catch ex As Exception
                                                    Select Case MessageBox.Show(ResText_WErrSCSI.Text, ResText_Warning.Text, MessageBoxButtons.AbortRetryIgnore)
                                                        Case DialogResult.Abort
                                                            Throw ex
                                                        Case DialogResult.Retry
                                                            succ = False
                                                        Case DialogResult.Ignore
                                                            succ = True
                                                            Exit While
                                                    End Select
                                                    p = New TapeUtils.PositionData(TapeDrive)
                                                    Continue While
                                                End Try
                                                If ((sense(2) >> 6) And &H1) = 1 Then
                                                    If (sense(2) And &HF) = 13 Then
                                                        PrintMsg(ResText_VOF.Text)
                                                        Invoke(Sub() MessageBox.Show(ResText_VOF.Text))
                                                        StopFlag = True
                                                        Exit For
                                                    Else
                                                        PrintMsg(ResText_EWEOM.Text, True)
                                                        succ = True
                                                        Exit While
                                                    End If
                                                ElseIf sense(2) And &HF <> 0 Then
                                                    PrintMsg($"sense err {TapeUtils.Byte2Hex(sense, True)}", Warning:=True, LogOnly:=True)
                                                    Select Case MessageBox.Show($"{ResText_WErr.Text}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}", ResText_Warning.Text, MessageBoxButtons.AbortRetryIgnore)
                                                        Case DialogResult.Abort
                                                            Throw New Exception(TapeUtils.ParseSenseData(sense))
                                                        Case DialogResult.Retry
                                                            succ = False
                                                        Case DialogResult.Ignore
                                                            succ = True
                                                            Exit While
                                                    End Select
                                                    p = New TapeUtils.PositionData(TapeDrive)
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
                                            'PrintMsg($"File Opened:{fr.SourcePath}", LogOnly:=True)
                                            Dim sh As IOManager.SHA1BlockwiseCalculator = Nothing
                                            If HashOnWrite Then sh = New IOManager.SHA1BlockwiseCalculator
                                            Dim LastWriteTask As Task = Nothing
                                            Dim ExitWhileFlag As Boolean = False
                                            While Not StopFlag
                                                Dim buffer(plabel.blocksize - 1) As Byte
                                                Dim BytesReaded As Integer = fr.Read(buffer, 0, plabel.blocksize)
                                                If LastWriteTask IsNot Nothing Then LastWriteTask.Wait()
                                                If ExitWhileFlag Then Exit While
                                                LastWriteTask = Task.Run(
                                                Sub()
                                                    If BytesReaded > 0 Then
                                                        CheckCount += 1
                                                        If CheckCount >= CheckCycle Then CheckCount = 0
                                                        If SpeedLimit > 0 AndAlso CheckCount = 0 Then
                                                            Dim ts As Double = (Now - SpeedLimitLastTriggerTime).TotalSeconds
                                                            While SpeedLimit > 0 AndAlso ts > 0 AndAlso ((plabel.blocksize * CheckCycle / 1048576) / ts) > SpeedLimit
                                                                Threading.Thread.Sleep(0)
                                                                ts = (Now - SpeedLimitLastTriggerTime).TotalSeconds
                                                            End While
                                                            SpeedLimitLastTriggerTime = Now
                                                        End If
                                                        Marshal.Copy(buffer, 0, wBufferPtr, BytesReaded)
                                                        Dim succ As Boolean = False
                                                        While Not succ
                                                            Dim sense As Byte()
                                                            Try
                                                                sense = TapeUtils.Write(TapeDrive, wBufferPtr, BytesReaded, BytesReaded < plabel.blocksize)
                                                                SyncLock p
                                                                    p.BlockNumber += 1
                                                                End SyncLock
                                                            Catch ex As Exception
                                                                Select Case MessageBox.Show(ResText_WErrSCSI.Text, ResText_Warning.Text, MessageBoxButtons.AbortRetryIgnore)
                                                                    Case DialogResult.Abort
                                                                        Throw ex
                                                                    Case DialogResult.Retry
                                                                        succ = False
                                                                    Case DialogResult.Ignore
                                                                        succ = True
                                                                        Exit While
                                                                End Select
                                                                p = New TapeUtils.PositionData(TapeDrive)
                                                                Continue While
                                                            End Try
                                                            If (((sense(2) >> 6) And &H1) = 1) Then
                                                                If ((sense(2) And &HF) = 13) Then
                                                                    PrintMsg(ResText_VOF.Text)
                                                                    Invoke(Sub() MessageBox.Show(ResText_VOF.Text))
                                                                    StopFlag = True
                                                                    fr.Close()
                                                                    ExitForFlag = True
                                                                    Exit Sub
                                                                Else
                                                                    PrintMsg(ResText_EWEOM.Text, True)
                                                                    succ = True
                                                                    Exit While
                                                                End If
                                                            ElseIf sense(2) And &HF <> 0 Then
                                                                Select Case MessageBox.Show($"{ResText_WErr.Text}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}", ResText_Warning.Text, MessageBoxButtons.AbortRetryIgnore)
                                                                    Case DialogResult.Abort
                                                                        Throw New Exception(TapeUtils.ParseSenseData(sense))
                                                                    Case DialogResult.Retry
                                                                        succ = False
                                                                    Case DialogResult.Ignore
                                                                        succ = True
                                                                        Exit While
                                                                End Select
                                                                p = New TapeUtils.PositionData(TapeDrive)
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
                                                        If Flush Then CheckFlush()
                                                        If Clean Then CheckClean(True)
                                                        fr.File.WrittenBytes += BytesReaded
                                                        TotalBytesProcessed += BytesReaded
                                                        CurrentBytesProcessed += BytesReaded
                                                        TotalBytesUnindexed += BytesReaded
                                                    Else
                                                        ExitWhileFlag = True
                                                    End If
                                                End Sub)
                                            End While
                                            If LastWriteTask IsNot Nothing Then LastWriteTask.Wait()
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
                                        If p.EOP Then PrintMsg(ResText_EWEOM.Text, True)
                                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                                        CurrentHeight = p.BlockNumber
                                    End If
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
                                If CheckUnindexedDataSizeLimit() Then p = New TapeUtils.PositionData(TapeDrive)
                                Invoke(Sub()
                                           If CapacityRefreshInterval > 0 AndAlso (Now - LastRefresh).TotalSeconds > CapacityRefreshInterval Then RefreshCapacity()
                                       End Sub)
                            Catch ex As Exception
                                MessageBox.Show($"{ResText_WErr.Text}{ex.ToString}")
                                PrintMsg($"{ResText_WErr.Text}{ex.Message}")
                            End Try
                            While Pause
                                Threading.Thread.Sleep(10)
                            End While
                            If StopFlag Then
                                Exit For
                            End If
                            UnwrittenFiles.Remove(fr)
                            WriteList(i) = Nothing
                        Next
                        Marshal.FreeHGlobal(wBufferPtr)
                        While HashTaskAwaitNumber > 0
                            Threading.Thread.Sleep(1)
                        End While
                    End If

                    UFReadCount.Dec()
                    Me.Invoke(Sub() Timer1_Tick(sender, e))
                    Dim TotalBytesWritten As Long = UnwrittenSizeOverrideValue
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            UnwrittenSizeOverrideValue = 0
                            UnwrittenCountOverwriteValue = 0
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            Exit While
                        End SyncLock
                    End While
                    Modified = True
                    If Not StopFlag Then
                        Dim TimeCost As TimeSpan = Now - StartTime
                        OnWriteFinishMessage = ($"{ResText_WFTime.Text}{(Math.Floor(TimeCost.TotalHours)).ToString().PadLeft(2, "0")}:{TimeCost.Minutes.ToString().PadLeft(2, "0")}:{TimeCost.Seconds.ToString().PadLeft(2, "0")} {ResText_AvgS.Text}{IOManager.FormatSize(TotalBytesWritten \ Math.Max(1, TimeCost.TotalSeconds))}/s")
                        Me.Invoke(Sub() OnWriteFinished())
                    Else
                        OnWriteFinishMessage = (ResText_WCnd.Text)
                    End If
                Catch ex As Exception
                    MessageBox.Show($"{ResText_WErr.Text}{ex.ToString}")
                    PrintMsg($"{ResText_WErr.Text}{ex.Message}")
                End Try
                TapeUtils.Flush(TapeDrive)
                TapeUtils.ReleaseUnit(TapeDrive)
                TapeUtils.AllowMediaRemoval(TapeDrive)
                Invoke(Sub()
                           LockGUI(False)
                           RefreshDisplay()
                           RefreshCapacity()
                           If Not StopFlag AndAlso WA0ToolStripMenuItem.Checked AndAlso MessageBox.Show(ResText_WFUp.Text, ResText_OpSucc.Text, MessageBoxButtons.OKCancel) = DialogResult.OK Then
                               更新数据区索引ToolStripMenuItem_Click(sender, e)
                           End If
                           PrintMsg(OnWriteFinishMessage)
                           RaiseEvent WriteFinished()
                       End Sub)
            End Sub)
        StopFlag = False
        LockGUI()
        th.Start()
    End Sub
    Private Sub 清除当前索引后数据ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 清除当前索引后数据ToolStripMenuItem.Click

        If MessageBox.Show(ResText_X2.Text, ResText_Warning.Text, MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
            Exit Sub
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(ResText_Locating.Text)
                    TapeUtils.Locate(TapeDrive, schema.location.startblock, schema.location.partition, TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(ResText_RI.Text)
                    Dim data As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim CurrentPos As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
                    If CurrentPos.PartitionNumber < ExtraPartitionCount Then
                        Invoke(Sub() MessageBox.Show(ResText_IPCanc.Text))
                        Exit Try
                    End If
                    TapeUtils.Locate(TapeDrive, CurrentPos.BlockNumber - 1, CurrentPos.PartitionNumber, TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.WriteFileMark(TapeDrive)
                    PrintMsg($"FileMark written", LogOnly:=True)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim outputfile As String = "schema\LTFSIndex_SetEOD_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = My.Computer.FileSystem.CombinePath(Application.StartupPath, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    PrintMsg(ResText_AI.Text)
                    schema = ltfsindex.FromSchFile(outputfile)
                    PrintMsg(ResText_AISucc.Text)
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
                    PrintMsg(ResText_RFailed.Text)
                End Try
                Modified = False
                PrintMsg(ResText_RollBacked.Text)
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
        If MessageBox.Show($"{ResText_RB1.Text}{schema.generationnumber}{ResText_RB2.Text} {ResText_Partition.Text}{schema.location.partition} {ResText_Block.Text}{schema.location.startblock}{vbCrLf _
                           }{ResText_RB3.Text} {ResText_Partition.Text}{schema.previousgenerationlocation.partition} {ResText_Block.Text}{schema.previousgenerationlocation.startblock}{vbCrLf _
                           }{ResText_RB4.Text}", ResText_Warning.Text, MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
            Exit Sub
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    PrintMsg(ResText_RBing.Text)
                    Dim genbefore As Integer = schema.generationnumber
                    Dim prevpart As ltfsindex.PartitionLabel = schema.previousgenerationlocation.partition
                    Dim prevblk As Long = schema.previousgenerationlocation.startblock
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.Locate(TapeDrive, schema.previousgenerationlocation.startblock, schema.previousgenerationlocation.partition, TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(ResText_RI.Text)
                    Dim data As Byte() = TapeUtils.ReadToFileMark(TapeDrive)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim outputfile As String = "schema\LTFSIndex_RollBack_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = My.Computer.FileSystem.CombinePath(Application.StartupPath, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    PrintMsg(ResText_AI.Text)
                    schema = ltfsindex.FromSchFile(outputfile)
                    PrintMsg(ResText_AISucc.Text)
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
                    PrintMsg(ResText_RFailed.Text)
                End Try
                Me.Invoke(Sub()
                              LockGUI(False)
                              Text = GetLocInfo()
                              PrintMsg(ResText_RBFin.Text)
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
                    PrintMsg(ResText_Locating.Text)
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
                        PrintMsg(ResText_NVOL1.Text)
                        Invoke(Sub() MessageBox.Show(ResText_NLTFS.Text, ResText_Error.Text))
                        LockGUI(False)
                        Exit Try
                    End If
                    TapeUtils.Locate(TapeDrive, 1, 0, TapeUtils.LocateDestType.FileMark)
                    PrintMsg(ResText_RLTFSInfo.Text)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.ReadFileMark(TapeDrive)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim pltext As String = Encoding.UTF8.GetString(TapeUtils.ReadToFileMark(TapeDrive))
                    plabel = ltfslabel.FromXML(pltext)
                    TapeUtils.SetBlockSize(TapeDrive, plabel.blocksize)
                    Barcode = TapeUtils.ReadBarcode(TapeDrive)
                    PrintMsg($"Barcode = {Barcode}", LogOnly:=True)
                    PrintMsg(ResText_Locating.Text)
                    TapeUtils.Locate(TapeDrive, 3, 0, TapeUtils.LocateDestType.FileMark)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.ReadFileMark(TapeDrive)
                    If ExtraPartitionCount = 0 Then
                        TapeUtils.Locate(TapeDrive, 0, 0, TapeUtils.LocateDestType.EOD)
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                        PrintMsg(ResText_RI.Text)
                        If DisablePartition Then
                            TapeUtils.Space6(TapeDrive, -2, TapeUtils.LocateDestType.FileMark)
                        Else
                            Dim p As TapeUtils.PositionData = GetPos
                            Dim FM As Long = p.FileNumber
                            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                            If FM <= 1 Then
                                PrintMsg(ResText_IRFailed.Text)
                                Invoke(Sub() MessageBox.Show(ResText_NLTFS.Text, ResText_Error.Text))
                                LockGUI(False)
                                Exit Try
                            End If
                            TapeUtils.Locate(TapeDrive, FM - 1, 0, TapeUtils.LocateDestType.FileMark)
                        End If
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                        TapeUtils.ReadFileMark(TapeDrive)
                    End If
                    PrintMsg(ResText_RI.Text)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim tmpf As String = $"{Application.StartupPath}\LCG_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
                    'data = TapeUtils.ReadToFileMark(TapeDrive)
                    TapeUtils.ReadToFileMark(TapeDrive, tmpf)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(ResText_AI.Text)
                    'schema = ltfsindex.FromSchemaText(Encoding.UTF8.GetString(data))
                    schema = ltfsindex.FromSchFile(tmpf)
                    If ExtraPartitionCount = 0 Then
                        Dim p As TapeUtils.PositionData = GetPos
                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                        CurrentHeight = p.BlockNumber
                    Else
                        CurrentHeight = -1
                    End If
                    PrintMsg(ResText_SvBak.Text)
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
                                  ToolStripStatusLabel1.ToolTipText = $"{ResText_Barcode.Text}:{ToolStripStatusLabel1.Text}{vbCrLf}{ResText_BlkSize.Text}:{plabel.blocksize}"
                                  RefreshDisplay()
                                  RefreshCapacity()
                              End Sub)

                    PrintMsg(ResText_IRSucc.Text)
                    LockGUI(False)
                    Invoke(Sub() RaiseEvent LTFSLoaded())
                Catch ex As Exception
                    PrintMsg(ResText_IRFailed.Text)
                    PrintMsg(ex.ToString, LogOnly:=True)
                    LockGUI(False)
                End Try
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
                    PrintMsg(ResText_Locating.Text)

                    Dim data As Byte()
                    Dim currentPos As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {currentPos.ToString()}", LogOnly:=True)
                    If currentPos.PartitionNumber <> 1 Then TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.Block)
                    TapeUtils.Locate(TapeDrive, 0, 1, TapeUtils.LocateDestType.EOD)
                    PrintMsg(ResText_RI.Text)
                    currentPos = GetPos
                    PrintMsg($"Position = {currentPos.ToString()}", LogOnly:=True)
                    If DisablePartition Then
                        TapeUtils.Space6(TapeDrive, -2, TapeUtils.LocateDestType.FileMark)
                    Else
                        Dim FM As Long = currentPos.FileNumber
                        If FM <= 1 Then
                            PrintMsg(ResText_IRFailed.Text)
                            Invoke(Sub() MessageBox.Show(ResText_NLTFS.Text, ResText_Error.Text))
                            LockGUI(False)
                            Exit Try
                        End If
                        TapeUtils.Locate(TapeDrive, FM - 1, 1, TapeUtils.LocateDestType.FileMark)
                    End If

                    TapeUtils.ReadFileMark(TapeDrive)
                    PrintMsg(ResText_RI.Text)
                    data = TapeUtils.ReadToFileMark(TapeDrive)
                    Dim outputfile As String = "schema\LTFSIndex_LoadDPIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema")) Then
                        My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema"))
                    End If
                    outputfile = My.Computer.FileSystem.CombinePath(Application.StartupPath, outputfile)
                    My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
                    PrintMsg(ResText_AI.Text)
                    schema = ltfsindex.FromSchFile(outputfile)
                    PrintMsg(ResText_AISucc.Text)
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
                    PrintMsg(ResText_IRSucc.Text)
                Catch ex As Exception
                    PrintMsg(ResText_IRFailed.Text)
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
                    TapeUtils.Flush(TapeDrive)
                    PrintMsg(ResText_DPIWritten.Text)
                End If
            Catch ex As Exception
                PrintMsg(ResText_DPIWFailed.Text, False, $"{ResText_DPIWFailed.Text}: {ex.ToString}")
                Invoke(Sub() MessageBox.Show(ex.ToString()))
            End Try
            Invoke(Sub()
                       LockGUI(False)
                       RefreshDisplay()
                       If Not SilentMode Then MessageBox.Show(ResText_DPIUed.Text)
                   End Sub)
        End Sub)
        LockGUI()
        th.Start()
    End Sub
    Public Sub LoadIndexFile(FileName As String, Optional ByVal Silent As Boolean = False)
        Try
            PrintMsg(ResText_RI.Text)
            'Dim schtext As String = My.Computer.FileSystem.ReadAllText(FileName)
            PrintMsg(ResText_AI.Text)
            ' Dim sch2 As ltfsindex = ltfsindex.FromSchemaText(schtext)
            Dim sch2 As ltfsindex = ltfsindex.FromSchFile(FileName)
            PrintMsg(ResText_AISucc.Text)
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
            ExtraPartitionCount = schema.location.partition
            RefreshDisplay()
            Modified = False
            Dim MAM090C As TapeUtils.MAMAttribute = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 8, 12, 0)
            Dim VCI As Byte() = {}
            If MAM090C IsNot Nothing Then
                VCI = MAM090C.RawData
            End If
            If Not Silent Then MessageBox.Show($"{ResText_ILdedP.Text}{vbCrLf}{vbCrLf}{ResText_VCID.Text}{vbCrLf}{TapeUtils.Byte2Hex(VCI, True)}")
        Catch ex As Exception
            MessageBox.Show($"{ResText_IAErrp.Text}{ex.Message}")
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
        PrintMsg(ResText_Exporting.Text)
        schema.SaveFile(outputfile)
        PrintMsg(ResText_IndexBaked.Text, False, $"{ResText_IndexBak2.Text}{vbCrLf}{outputfile}")
        Return outputfile
    End Function
    Private Sub 备份当前索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 备份当前索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    Dim outputfile As String = AutoDump()
                    Me.Invoke(Sub() MessageBox.Show($"{ResText_IndexBak2.Text}{vbCrLf}{outputfile}"))
                Catch ex As Exception
                    PrintMsg(ResText_IndexBakF.Text)
                End Try
                LockGUI(False)
            End Sub)
        LockGUI()
        th.Start()
    End Sub
    Private Sub 格式化ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 格式化ToolStripMenuItem.Click
        If MessageBox.Show(ResText_DataLossWarning.Text, ResText_Warning.Text, MessageBoxButtons.OKCancel) = DialogResult.OK Then
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
            'nop
            TapeUtils.ReadPosition(TapeDrive)
            Dim MaxExtraPartitionAllowed As Byte = TapeUtils.ModeSense(TapeDrive, &H11)(2)
            If MaxExtraPartitionAllowed > 1 Then MaxExtraPartitionAllowed = 1
            Barcode = TapeUtils.ReadBarcode(TapeDrive)
            Dim VolumeLabel As String = ""
            Dim Confirm As Boolean = False
            While Not Confirm
                Barcode = InputBox(ResText_SetBarcode.Text, ResText_Barcode.Text, Barcode)
                If VolumeLabel = "" Then VolumeLabel = Barcode
                VolumeLabel = InputBox(ResText_SetVolumeN.Text, ResText_LTFSVolumeN.Text, VolumeLabel)

                Select Case MessageBox.Show($"{ResText_Barcode2.Text}{Barcode}{vbCrLf}{ResText_LTFSVolumeN2.Text}{VolumeLabel}", ResText_Confirm.Text, MessageBoxButtons.YesNoCancel)
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
                    PrintMsg(ResText_FmtFin.Text)
                    LockGUI(False)
                    Me.Invoke(Sub()
                                  MessageBox.Show(ResText_FmtFin.Text)
                                  读取索引ToolStripMenuItem_Click(sender, e)
                              End Sub)
                End Sub,
                Sub(Message As String)
                    'OnError
                    PrintMsg(Message)
                    LockGUI(False)
                    Me.Invoke(Sub() MessageBox.Show($"{ResText_FmtFail.Text}{vbCrLf}{Message}"))
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
                PrintMsg(ResText_RI.Text)
                'Dim s As String = My.Computer.FileSystem.ReadAllText(OpenFileDialog1.FileName)
                'If s.Contains("XMLSchema") Then
                '    PrintMsg("正在解析索引")
                '    schhash = ltfsindex.FromXML(s)
                'Else
                '    PrintMsg("正在解析索引")
                '    schhash = ltfsindex.FromSchemaText(s)
                'End If
                schhash = ltfsindex.FromSchFile(OpenFileDialog1.FileName)
                Dim dr As DialogResult = MessageBox.Show(ResText_SHA1Overw.Text, ResText_Hint.Text, MessageBoxButtons.YesNoCancel)
                PrintMsg(ResText_Importing.Text)
                Dim result As String = ""
                If dr = DialogResult.Yes Then
                    result = ImportSHA1(schhash, True)
                ElseIf dr = DialogResult.No Then
                    result = ImportSHA1(schhash, False)
                Else
                    PrintMsg(ResText_OpCancelled.Text)
                    Exit Try
                End If
                RefreshDisplay()
                PrintMsg($"{ResText_Imported.Text} {result}")
            Catch ex As Exception
                PrintMsg(ex.ToString)
            End Try
        End If
    End Sub
    Private Sub 设置高度ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 设置高度ToolStripMenuItem.Click
        Dim p As TapeUtils.PositionData = GetPos
        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
        Dim Pos As Long = p.BlockNumber
        If MessageBox.Show($"{ResText_SetH1.Text}{Pos}{ResText_SetH2.Text}{vbCrLf}{ResText_SetH3.Text}", ResText_Confirm.Text, MessageBoxButtons.OKCancel) = DialogResult.OK Then
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
                            Invoke(Sub() MessageBox.Show($"{ResText_Located.Text}{ext.startblock}"))
                            PrintMsg($"{ResText_Located.Text}{ext.startblock}")
                        End Sub)
                LockGUI()
                PrintMsg(ResText_Locating.Text)
                th.Start()
            End If
        End If
    End Sub
    Private Sub S60ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles S60ToolStripMenuItem.Click
        SMaxNum = 60
        Chart1.Titles(0).Text = S60ToolStripMenuItem.Text
    End Sub
    Private Sub Min5ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Min5ToolStripMenuItem.Click
        SMaxNum = 300
        Chart1.Titles(0).Text = Min5ToolStripMenuItem.Text
    End Sub
    Private Sub Min10ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Min10ToolStripMenuItem.Click
        SMaxNum = 600
        Chart1.Titles(0).Text = Min10ToolStripMenuItem.Text
    End Sub
    Private Sub Min30ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Min30ToolStripMenuItem.Click
        SMaxNum = 1800
        Chart1.Titles(0).Text = Min30ToolStripMenuItem.Text
    End Sub
    Private Sub H1ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles H1ToolStripMenuItem.Click
        SMaxNum = 3600
        Chart1.Titles(0).Text = H1ToolStripMenuItem.Text
    End Sub
    Private Sub H3ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles H3ToolStripMenuItem.Click
        SMaxNum = 3600 * 3
        Chart1.Titles(0).Text = H3ToolStripMenuItem.Text
    End Sub
    Private Sub H6ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles H6ToolStripMenuItem.Click
        SMaxNum = 3600 * 6
        Chart1.Titles(0).Text = H6ToolStripMenuItem.Text
    End Sub
    Public Sub CheckFlush()
        If Threading.Interlocked.Exchange(Flush, False) Then
            PrintMsg("Flush Triggered", LogOnly:=True)
            Dim Loc As TapeUtils.PositionData = GetPos
            If Loc.EOP Then PrintMsg(ResText_EWEOM.Text, True)
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
            If Loc.EOP Then PrintMsg(ResText_EWEOM.Text, True)
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
        LogarithmicToolStripMenuItem.Checked = False
    End Sub
    Private Sub LogrithmToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LogarithmicToolStripMenuItem.Click
        Chart1.ChartAreas(0).AxisY.IsLogarithmic = True
        LinearToolStripMenuItem.Checked = False
        LogarithmicToolStripMenuItem.Checked = True
    End Sub
    Private Sub ToolStripDropDownButton1_Click(sender As Object, e As EventArgs) Handles ToolStripDropDownButton1.Click
        Pause = True
        If MessageBox.Show(ResText_CancelConfirm.Text, ResText_Warning.Text, MessageBoxButtons.OKCancel) = DialogResult.OK Then
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
        If MessageBox.Show(ResText_ClearWC.Text, ResText_Confirm.Text, MessageBoxButtons.OKCancel) = DialogResult.OK Then
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

                If RestorePosition.BlockNumber <> fe.startblock OrElse RestorePosition.PartitionNumber <> fe.partition Then
                    TapeUtils.Locate(TapeDrive, fe.startblock, fe.partition)
                    RestorePosition = New TapeUtils.PositionData(TapeDrive)
                End If
                Dim TotalBytesToRead As Long = fe.bytecount
                Dim blk As Byte() = TapeUtils.ReadBlock(TapeDrive, BlockSizeLimit:=Math.Min(plabel.blocksize, TotalBytesToRead))
                SyncLock RestorePosition
                    RestorePosition.BlockNumber += 1
                End SyncLock
                If fe.byteoffset > 0 Then blk = blk.Skip(fe.byteoffset).ToArray()
                TotalBytesToRead -= blk.Length
                HT.Propagate(blk)
                Threading.Interlocked.Add(CurrentBytesProcessed, blk.Length)
                Threading.Interlocked.Add(TotalBytesProcessed, blk.Length)
                While TotalBytesToRead > 0
                    blk = TapeUtils.ReadBlock(TapeDrive, BlockSizeLimit:=Math.Min(plabel.blocksize, TotalBytesToRead))
                    SyncLock RestorePosition
                        RestorePosition.BlockNumber += 1
                    End SyncLock
                    Dim blklen As Integer = blk.Length
                    If blklen > TotalBytesToRead Then blklen = TotalBytesToRead
                    TotalBytesToRead -= blk.Length
                    HT.Propagate(blk, blklen)
                    Threading.Interlocked.Add(CurrentBytesProcessed, blk.Length)
                    Threading.Interlocked.Add(TotalBytesProcessed, blk.Length)
                    If StopFlag Then Return ""
                    While Pause
                        Threading.Thread.Sleep(10)
                    End While
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
                If My.Settings.LTFSWriter_FileLabel = " " OrElse CInt(Val(dir.name)).ToString = dir.name Then
                    Dim fl As String = $".{My.Settings.LTFSWriter_FileLabel}"
                    If fl = ". " Then fl = ""
                    Dim fExist As Boolean = False
                    For Each f As ltfsindex.file In d.contents._file
                        If f.name = $"{dir.name}{fl}" Then
                            fExist = True
                            Exit For
                        End If
                    Next
                    If Not fExist Then
                        Dim emptyfile As String = My.Computer.FileSystem.CombinePath(Application.StartupPath, "empty.file")
                        My.Computer.FileSystem.WriteAllBytes(emptyfile, {}, False)
                        Dim fnew As New FileRecord(emptyfile, d)
                        fnew.File.name = $"{dir.name}{fl}"
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
            PrintMsg(ResText_OpSucc.Text)
            RefreshDisplay()
        End If
    End Sub

    Private Sub 设置标签ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 设置标签ToolStripMenuItem.Click
        My.Settings.LTFSWriter_FileLabel = InputBox(ResText_DLS.Text, ResText_DLT.Text, My.Settings.LTFSWriter_FileLabel)
        PrintMsg($"{ResText_DLFin.Text} .{My.Settings.LTFSWriter_FileLabel}")
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
                            PrintMsg(ResText_UDI.Text)
                            WriteCurrentIndex(False)
                            TapeUtils.Flush(TapeDrive)
                        End If
                        AutoDump()
                        PrintMsg(ResText_UI.Text)
                        RefreshIndexPartition()
                        TapeUtils.ReleaseUnit(TapeDrive)
                        TapeUtils.AllowMediaRemoval(TapeDrive)
                        PrintMsg(ResText_IUd.Text)
                        If schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.a Then 更新数据区索引ToolStripMenuItem.Enabled = False
                        TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Eject)
                        PrintMsg(ResText_Ejd.Text)
                        Invoke(Sub()
                                   LockGUI(False)
                                   RefreshDisplay()
                                   RaiseEvent TapeEjected()
                               End Sub)
                    Catch ex As Exception
                        PrintMsg(ResText_IUErr.Text)
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
        Dim sin As String = InputBox(ResText_WLimS.Text, ResText_Setting.Text, SpeedLimit)
        If sin = "" Then Exit Sub
        SpeedLimit = Val(sin)
    End Sub

    Private Sub 重装带前清洁次数3ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 重装带前清洁次数3ToolStripMenuItem.Click
        CleanCycle = Val(InputBox(ResText_CLNCS.Text, ResText_Setting.Text, CleanCycle))
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
                            PrintMsg(ResText_Hashing.Text)
                            StopFlag = False
                            CurrentBytesProcessed = 0
                            CurrentFilesProcessed = 0
                            UnwrittenSizeOverrideValue = 0
                            UnwrittenCountOverwriteValue = flist.Count
                            For Each FI As ltfsindex.file In flist
                                UnwrittenSizeOverrideValue += FI.length
                            Next
                            RestorePosition = New TapeUtils.PositionData(TapeDrive)
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
                            PrintMsg(ResText_HErr.Text)
                        End Try
                        UnwrittenSizeOverrideValue = 0
                        UnwrittenCountOverwriteValue = 0
                        StopFlag = False
                        LockGUI(False)
                        RefreshDisplay()
                        PrintMsg(ResText_HFin.Text)
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
                PrintMsg(ResText_Hashing.Text)
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
                    PrintMsg(ResText_PrepFile.Text)
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
                    UnwrittenSizeOverrideValue = 0
                    UnwrittenCountOverwriteValue = FileList.Count
                    For Each FI As FileRecord In FileList
                        UnwrittenSizeOverrideValue += FI.File.length
                    Next
                    PrintMsg(ResText_Hashing.Text)
                    Dim c As Integer = 0
                    RestorePosition = New TapeUtils.PositionData(TapeDrive)
                    For Each fr As FileRecord In FileList
                        c += 1
                        PrintMsg($"{ResText_Hashing.Text} [{c}/{FileList.Count}] {fr.File.name} {ResText_Size.Text}:{IOManager.FormatSize(fr.File.length)}", False, $"{ResText_Hashing.Text} [{c}/{FileList.Count}] {fr.SourcePath} {ResText_Size.Text}:{fr.File.length}")
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
                            PrintMsg(ResText_OpCancelled.Text)
                            Exit Try
                        End If
                    Next
                    PrintMsg(ResText_HFin.Text)
                Catch ex As Exception
                    Invoke(Sub() MessageBox.Show(ex.ToString))
                    PrintMsg(ResText_HErr.Text)
                End Try
                UnwrittenSizeOverrideValue = 0
                UnwrittenCountOverwriteValue = 0
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
            MessageBox.Show($"{d.name}{vbCrLf}{ResText_FCountP.Text}{fnum}{vbCrLf}{ResText_FSizeP.Text}{fbytes} {ResText_Byte.Text} ({IOManager.FormatSize(fbytes)})")
        End If
    End Sub

    Private Sub 预读文件数5ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 预读文件数5ToolStripMenuItem.Click
        Dim s As String = InputBox(ResText_SPreR.Text, ResText_Setting.Text, My.Settings.LTFSWriter_PreLoadNum)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_PreLoadNum = Val(s)
        预读文件数5ToolStripMenuItem.Text = $"{ResText_PFC.Text}{My.Settings.LTFSWriter_PreLoadNum}"
    End Sub

    Private Sub 文件缓存32MiBToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 文件缓存32MiBToolStripMenuItem.Click
        Dim s As String = InputBox("设置文件缓存", ResText_Setting.Text, My.Settings.LTFSWriter_PreLoadBytes)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_PreLoadBytes = Val(s)
        If My.Settings.LTFSWriter_PreLoadBytes = 0 Then My.Settings.LTFSWriter_PreLoadBytes = 4096
        文件缓存32MiBToolStripMenuItem.Text = $"文件缓存：{IOManager.FormatSize(My.Settings.LTFSWriter_PreLoadBytes)}"
    End Sub

    Private Sub 文件详情ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 文件详情ToolStripMenuItem.Click
        Dim result As New StringBuilder
        If ListView1.Tag IsNot Nothing AndAlso
        ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 Then
            SyncLock ListView1.SelectedItems
                For Each ItemSelected As ListViewItem In ListView1.SelectedItems
                    If ItemSelected.Tag IsNot Nothing AndAlso TypeOf (ItemSelected.Tag) Is ltfsindex.file Then
                        Dim f As ltfsindex.file = ItemSelected.Tag
                        result.AppendLine(f.GetSerializedText())
                    End If
                Next
            End SyncLock
        End If
        MessageBox.Show(result.ToString)
    End Sub

    Private Sub 禁用分区ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 禁用分区ToolStripMenuItem.Click
        DisablePartition = 禁用分区ToolStripMenuItem.Checked
    End Sub

    Private Sub 速度下限ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 速度下限ToolStripMenuItem.Click
        Dim s As String = InputBox(ResText_SSMin.Text, ResText_Setting.Text, My.Settings.LTFSWriter_AutoCleanDownLim)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_AutoCleanDownLim = Val(s)
        My.Settings.Save()
        速度下限ToolStripMenuItem.Text = $"{ResText_SMin.Text}{My.Settings.LTFSWriter_AutoCleanDownLim} MiB/s"
    End Sub

    Private Sub 速度上限ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 速度上限ToolStripMenuItem.Click
        Dim s As String = InputBox(ResText_SSMax.Text, ResText_Setting.Text, My.Settings.LTFSWriter_AutoCleanUpperLim)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_AutoCleanUpperLim = Val(s)
        My.Settings.Save()
        速度上限ToolStripMenuItem.Text = $"{ResText_SMax.Text}{My.Settings.LTFSWriter_AutoCleanUpperLim} MiB/s"
    End Sub

    Private Sub 持续时间ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 持续时间ToolStripMenuItem.Click
        Dim s As String = InputBox(ResText_SSTime.Text, ResText_Setting.Text, My.Settings.LTFSWriter_AutoCleanTimeThreashould)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_AutoCleanTimeThreashould = Val(s)
        My.Settings.Save()
        持续时间ToolStripMenuItem.Text = $"{ResText_STime.Text}{My.Settings.LTFSWriter_AutoCleanTimeThreashould}s"
    End Sub

    Private Sub 去重SHA1ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 去重SHA1ToolStripMenuItem.Click
        My.Settings.LTFSWriter_DeDupe = Not My.Settings.LTFSWriter_DeDupe
        去重SHA1ToolStripMenuItem.Checked = My.Settings.LTFSWriter_DeDupe
        My.Settings.Save()
    End Sub
    Public Class LTFSMountFSBase
        Inherits Fsp.FileSystemBase
        Public LW As LTFSWriter
        Public VolumeLabel As String
        Public TapeDrive As String
        Public Const ALLOCATION_UNIT As Integer = 4096

        Protected Shared Sub ThrowIoExceptionWithHResult(ByVal HResult As Int32)
            Throw New IO.IOException(Nothing, HResult)
        End Sub

        Protected Shared Sub ThrowIoExceptionWithWin32(ByVal [Error] As Int32)
            ThrowIoExceptionWithHResult(CType((2147942400 Or [Error]), Int32))
            'TODO: checked/unchecked is not supported at this time
        End Sub
        Protected Shared Sub ThrowIoExceptionWithNtStatus(ByVal Status As Int32)
            ThrowIoExceptionWithWin32(CType(Win32FromNtStatus(Status), Int32))
        End Sub
        Public Overrides Function ExceptionHandler(ByVal ex As Exception) As Int32
            Dim HResult As Int32 = ex.HResult
            If (2147942400 _
                    = (HResult And 4294901760)) Then
                Return NtStatusFromWin32((CType(HResult, UInt32) And 65535))
            End If
            Return STATUS_UNEXPECTED_IO_ERROR
        End Function
        Class FileDesc
            Public IsDirectory As Boolean
            Public LTFSFile As ltfsindex.file
            Public LTFSDirectory As ltfsindex.directory
            Public Parent As ltfsindex.directory

            Public FileSystemInfos() As DictionaryEntry


            Public Enum dwFilAttributesValue As UInteger
                FILE_ATTRIBUTE_ARCHIVE = &H20
                FILE_ATTRIBUTE_COMPRESSED = &H800
                FILE_ATTRIBUTE_DIRECTORY = &H10
                FILE_ATTRIBUTE_ENCRYPTED = &H4000
                FILE_ATTRIBUTE_HIDDEN = &H2
                FILE_ATTRIBUTE_NORMAL = &H80
                FILE_ATTRIBUTE_OFFLINE = &H1000
                FILE_ATTRIBUTE_READONLY = &H1
                FILE_ATTRIBUTE_REPARSE_POINT = &H400
                FILE_ATTRIBUTE_SPARSE_FILE = &H200
                FILE_ATTRIBUTE_SYSTEM = &H4
                FILE_ATTRIBUTE_TEMPORARY = &H100
                FILE_ATTRIBUTE_VIRTUAL = &H10000
            End Enum

            Public Function GetFileInfo(ByRef FileInfo As FileInfo) As Int32
                If (Not IsDirectory) Then
                    FileInfo.FileAttributes = dwFilAttributesValue.FILE_ATTRIBUTE_OFFLINE Or dwFilAttributesValue.FILE_ATTRIBUTE_ARCHIVE
                    If LTFSFile.readonly Then FileInfo.FileAttributes = FileInfo.FileAttributes Or dwFilAttributesValue.FILE_ATTRIBUTE_READONLY
                    FileInfo.ReparseTag = 0
                    FileInfo.FileSize = LTFSFile.length
                    FileInfo.AllocationSize = (FileInfo.FileSize + ALLOCATION_UNIT - 1) / ALLOCATION_UNIT * ALLOCATION_UNIT
                    FileInfo.CreationTime = TapeUtils.ParseTimeStamp(LTFSFile.creationtime).Ticks
                    FileInfo.LastAccessTime = TapeUtils.ParseTimeStamp(LTFSFile.accesstime).Ticks
                    FileInfo.LastWriteTime = TapeUtils.ParseTimeStamp(LTFSFile.changetime).Ticks
                    FileInfo.ChangeTime = TapeUtils.ParseTimeStamp(LTFSFile.changetime).Ticks
                    FileInfo.IndexNumber = 0
                    FileInfo.HardLinks = 0
                Else
                    FileInfo.FileAttributes = dwFilAttributesValue.FILE_ATTRIBUTE_OFFLINE Or dwFilAttributesValue.FILE_ATTRIBUTE_DIRECTORY
                    FileInfo.ReparseTag = 0
                    FileInfo.FileSize = 0
                    FileInfo.AllocationSize = 0
                    FileInfo.CreationTime = TapeUtils.ParseTimeStamp(LTFSDirectory.creationtime).Ticks
                    FileInfo.LastAccessTime = TapeUtils.ParseTimeStamp(LTFSDirectory.accesstime).Ticks
                    FileInfo.LastWriteTime = TapeUtils.ParseTimeStamp(LTFSDirectory.changetime).Ticks
                    FileInfo.ChangeTime = TapeUtils.ParseTimeStamp(LTFSDirectory.changetime).Ticks
                    FileInfo.IndexNumber = 0
                    FileInfo.HardLinks = 0
                End If
                Return STATUS_SUCCESS
            End Function

            Public Function GetFileAttributes() As UInt32
                Dim FileInfo As FileInfo
                Me.GetFileInfo(FileInfo)
                Return FileInfo.FileAttributes
            End Function

        End Class
        Public Overrides Function Init(Host0 As Object) As Integer
            Dim Host As Fsp.FileSystemHost = CType(Host0, Fsp.FileSystemHost)
            Try
                Host.SectorSize = 4096
                Host.SectorsPerAllocationUnit = LW.plabel.blocksize \ Host.SectorSize
                Host.MaxComponentLength = 65535
                Host.FileInfoTimeout = 10 * 1000
                Host.CaseSensitiveSearch = False
                Host.CasePreservedNames = True
                Host.UnicodeOnDisk = True
                Host.PersistentAcls = False
                Host.PostCleanupWhenModifiedOnly = True
                Host.PassQueryDirectoryPattern = True
                Host.FlushAndPurgeOnCleanup = True
                Host.FileSystemName = "LTFS"
                Host.VolumeCreationTime = TapeUtils.ParseTimeStamp(LW.plabel.formattime).ToFileTimeUtc()
                Host.VolumeSerialNumber = 0

            Catch ex As Exception
                MessageBox.Show(ex.ToString)
            End Try
            Return STATUS_SUCCESS
        End Function
        Private Class DirectoryEntryComparer
            Implements IComparer
            Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements IComparer.Compare
                Return String.Compare(CType(CType(x, DictionaryEntry).Key, String), CType(CType(y, DictionaryEntry).Key, String))
            End Function
        End Class
        Dim _DirectoryEntryComparer As DirectoryEntryComparer = New DirectoryEntryComparer
        Public Sub New(path0 As String)
            TapeDrive = path0
        End Sub
        Public Overrides Function GetVolumeInfo(<Out> ByRef VolumeInfo As VolumeInfo) As Int32
            VolumeInfo = New VolumeInfo()
            VolumeLabel = LW.schema._directory(0).name
            Try
                VolumeInfo.TotalSize = TapeUtils.MAMAttribute.FromTapeDrive(LW.TapeDrive, 0, 1, LW.ExtraPartitionCount).AsNumeric << 20
                VolumeInfo.FreeSize = TapeUtils.MAMAttribute.FromTapeDrive(LW.TapeDrive, 0, 0, LW.ExtraPartitionCount).AsNumeric << 20
                'VolumeInfo.SetVolumeLabel(VolumeLabel)
            Catch ex As Exception
                MessageBox.Show(ex.ToString)
            End Try
            Return STATUS_SUCCESS
        End Function

        Public Overrides Function GetSecurityByName(FileName As String, ByRef FileAttributes As UInteger, ByRef SecurityDescriptor() As Byte) As Integer
            If LW.schema._directory.Count = 0 Then Throw New Exception("Not LTFS formatted")
            Dim path As String() = FileName.Split({"\"}, StringSplitOptions.RemoveEmptyEntries)
            Dim filedesc As New FileDesc
            Dim FileInfo As New FileInfo
            If path.Length = 0 Then
                filedesc = New FileDesc With {.IsDirectory = True, .LTFSDirectory = LW.schema._directory(0)}
                filedesc.GetFileInfo(FileInfo)
                FileAttributes = FileInfo.FileAttributes
                Return STATUS_SUCCESS
            End If
            Dim FileExist As Boolean = False

            Dim LTFSDir As ltfsindex.directory = LW.schema._directory(0)
            For i As Integer = 0 To path.Length - 2
                Dim dirFound As Boolean = False
                For Each d As ltfsindex.directory In LTFSDir.contents._directory
                    If d.name = path(i) Then
                        LTFSDir = d
                        dirFound = True
                        Exit For
                    End If
                Next
                If Not dirFound Then Return STATUS_NOT_FOUND
            Next
            For Each d As ltfsindex.directory In LTFSDir.contents._directory
                If d.name = path(path.Length - 1) Then
                    FileExist = True
                    filedesc = New FileDesc With {.IsDirectory = True, .LTFSDirectory = d}
                    Exit For
                End If
            Next
            If Not FileExist Then
                For Each f As ltfsindex.file In LTFSDir.contents._file
                    If f.name = path(path.Length - 1) Then
                        FileExist = True
                        filedesc = New FileDesc With {.IsDirectory = False, .LTFSFile = f, .Parent = LTFSDir}
                    End If
                Next
            End If
            If FileExist Then
                filedesc.GetFileInfo(FileInfo)
            End If
            FileAttributes = FileInfo.FileAttributes
            Return STATUS_SUCCESS
        End Function
        Public Overrides Function Open(FileName As String,
                                       CreateOptions As UInteger,
                                       GrantedAccess As UInteger,
                                       ByRef FileNode As Object,
                                       ByRef FileDesc As Object,
                                       ByRef FileInfo As FileInfo,
                                       ByRef NormalizedName As String) As Integer
            Try
                FileNode = New Object()
                NormalizedName = ""
                If LW.schema._directory.Count = 0 Then Throw New Exception("Not LTFS formatted")
                Dim path As String() = FileName.Split({"\"}, StringSplitOptions.RemoveEmptyEntries)
                If path.Length = 0 Then
                    FileDesc = New FileDesc With {.IsDirectory = True, .LTFSDirectory = LW.schema._directory(0)}
                    Dim status As Integer = CType(FileDesc, FileDesc).GetFileInfo(FileInfo)
                    Return status
                End If
                Dim FileExist As Boolean = False

                Dim LTFSDir As ltfsindex.directory = LW.schema._directory(0)
                For i As Integer = 0 To path.Length - 2
                    Dim dirFound As Boolean = False
                    For Each d As ltfsindex.directory In LTFSDir.contents._directory
                        If d.name = path(i) Then
                            LTFSDir = d
                            Exit For
                        End If
                    Next
                    If Not dirFound Then Return STATUS_NOT_FOUND
                Next
                For Each d As ltfsindex.directory In LTFSDir.contents._directory
                    If d.name = path(path.Length - 1) Then
                        FileExist = True
                        FileDesc = New FileDesc With {.IsDirectory = True, .LTFSDirectory = d}
                        Exit For
                    End If
                Next
                If Not FileExist Then
                    For Each f As ltfsindex.file In LTFSDir.contents._file
                        If f.name = path(path.Length - 1) Then
                            FileExist = True
                            FileDesc = New FileDesc With {.IsDirectory = False, .LTFSFile = f, .Parent = LTFSDir}
                        End If
                    Next
                End If
                If FileExist Then
                    Return CType(FileDesc, FileDesc).GetFileInfo(FileInfo)
                End If
            Catch ex As Exception
                Throw
            End Try
            Return STATUS_NOT_FOUND
        End Function
        Public Overrides Sub Close(FileNode As Object, FileDesc As Object)

        End Sub
        Public Overrides Function Read(FileNode As Object,
                                       FileDesc As Object,
                                       Buffer As IntPtr,
                                       Offset As ULong,
                                       Length As UInteger,
                                       ByRef BytesTransferred As UInteger) As Integer
            If FileDesc Is Nothing OrElse TypeOf FileDesc IsNot FileDesc Then Return STATUS_NOT_FOUND
            Try
                With CType(FileDesc, FileDesc)
                    If .IsDirectory Then Return STATUS_NOT_FOUND
                    If .LTFSFile Is Nothing Then Return STATUS_NOT_FOUND
                    If Offset >= .LTFSFile.length Then ThrowIoExceptionWithNtStatus(STATUS_END_OF_FILE)
                    .LTFSFile.extentinfo.Sort(New Comparison(Of ltfsindex.file.extent)(Function(a As ltfsindex.file.extent, b As ltfsindex.file.extent) As Integer
                                                                                           Return (a.fileoffset).CompareTo(b.fileoffset)
                                                                                       End Function))
                    Dim BufferOffset As Long = 0
                    For ei As Integer = 0 To .LTFSFile.extentinfo.Count - 1
                        With .LTFSFile.extentinfo(ei)
                            If Offset >= .fileoffset + .bytecount Then Continue For
                            Dim CurrentFileOffset As Long = .fileoffset

                            TapeUtils.Locate(TapeDrive, .startblock, .partition)

                            Dim blkBuffer As Byte() = TapeUtils.ReadBlock(TapeDrive)
                            CurrentFileOffset += blkBuffer.Length - .byteoffset
                            While CurrentFileOffset <= Offset
                                blkBuffer = TapeUtils.ReadBlock(TapeDrive)
                                CurrentFileOffset += blkBuffer.Length
                            End While
                            Dim FirstBlockByteOffset As Integer = blkBuffer.Length - (CurrentFileOffset - Offset)
                            Marshal.Copy(blkBuffer, FirstBlockByteOffset, Buffer, blkBuffer.Length - FirstBlockByteOffset)
                            BufferOffset += blkBuffer.Length - FirstBlockByteOffset
                            While BufferOffset < .bytecount
                                blkBuffer = TapeUtils.ReadBlock(TapeDrive)
                                Marshal.Copy(blkBuffer, 0, New IntPtr(Buffer.ToInt64 + BufferOffset), Math.Min(blkBuffer.Length, .bytecount - BufferOffset))
                                BufferOffset += Math.Min(blkBuffer.Length, .bytecount - BufferOffset)
                            End While
                        End With
                    Next
                    Return STATUS_SUCCESS
                End With
            Catch ex As Exception
                Return STATUS_FILE_CORRUPT_ERROR
            End Try
        End Function
        Public Overrides Function GetFileInfo(FileNode As Object, FileDesc As Object, ByRef FileInfo As FileInfo) As Integer
            Dim result As Integer = CType(FileDesc, FileDesc).GetFileInfo(FileInfo)
            Return result
        End Function

        Public Overrides Function ReadDirectoryEntry(FileNode As Object, FileDesc0 As Object, Pattern As String, Marker As String, ByRef Context As Object, <Out> ByRef FileName As String, <Out> ByRef FileInfo As FileInfo) As Boolean

            Dim FileDesc As FileDesc = CType(FileDesc0, FileDesc)
            If FileDesc.FileSystemInfos Is Nothing Then
                If Pattern IsNot Nothing Then
                    Pattern = Pattern.Replace("<", "*").Replace(">", "?").Replace("""", ".")
                Else
                    Pattern = "*"
                End If
                Dim lst As New SortedList()
                If FileDesc.LTFSDirectory IsNot Nothing AndAlso FileDesc.Parent IsNot Nothing Then
                    lst.Add(".", FileDesc.LTFSDirectory)
                    lst.Add("..", FileDesc.Parent)
                End If
                For Each d As ltfsindex.directory In FileDesc.LTFSDirectory.contents._directory
                    If d.name Like Pattern Then
                        lst.Add(d.name, d)
                    End If
                Next
                For Each f As ltfsindex.file In FileDesc.LTFSDirectory.contents._file
                    If f.name Like Pattern Then
                        lst.Add(f.name, f)
                    End If
                Next
            End If
            Dim index As Long = 0
            If Context Is Nothing Then
                If Marker IsNot Nothing Then
                    index = Array.BinarySearch(FileDesc.FileSystemInfos, New DictionaryEntry(Marker, Nothing), _DirectoryEntryComparer)
                    If index >= 0 Then
                        index += 1
                    Else
                        index = -index
                    End If
                End If
            Else
                index = CLng(Context)
            End If
            If FileDesc.FileSystemInfos.Length > index Then
                Context = index + 1
                FileName = FileDesc.FileSystemInfos(index).Key
                FileInfo = New FileInfo()
                With FileDesc.FileSystemInfos(index)
                    If TypeOf FileDesc.FileSystemInfos(index).Value Is ltfsindex.directory Then
                        With CType(FileDesc.FileSystemInfos(index).Value, ltfsindex.directory)
                            FileInfo.FileAttributes = FileDesc.dwFilAttributesValue.FILE_ATTRIBUTE_OFFLINE Or FileDesc.dwFilAttributesValue.FILE_ATTRIBUTE_DIRECTORY
                            FileInfo.ReparseTag = 0
                            FileInfo.FileSize = 0
                            FileInfo.AllocationSize = 0
                            FileInfo.CreationTime = TapeUtils.ParseTimeStamp(.creationtime).Ticks
                            FileInfo.LastAccessTime = TapeUtils.ParseTimeStamp(.accesstime).Ticks
                            FileInfo.LastWriteTime = TapeUtils.ParseTimeStamp(.changetime).Ticks
                            FileInfo.ChangeTime = TapeUtils.ParseTimeStamp(.changetime).Ticks
                            FileInfo.IndexNumber = 0
                            FileInfo.HardLinks = 0
                        End With
                    ElseIf TypeOf FileDesc.FileSystemInfos(index).Value Is ltfsindex.file Then
                        With CType(FileDesc.FileSystemInfos(index).Value, ltfsindex.file)
                            FileInfo.FileAttributes = FileDesc.dwFilAttributesValue.FILE_ATTRIBUTE_OFFLINE Or FileDesc.dwFilAttributesValue.FILE_ATTRIBUTE_ARCHIVE
                            If .readonly Then FileInfo.FileAttributes = FileInfo.FileAttributes Or FileDesc.dwFilAttributesValue.FILE_ATTRIBUTE_READONLY
                            FileInfo.ReparseTag = 0
                            FileInfo.FileSize = .length
                            FileInfo.CreationTime = TapeUtils.ParseTimeStamp(.creationtime).Ticks
                            FileInfo.LastAccessTime = TapeUtils.ParseTimeStamp(.accesstime).Ticks
                            FileInfo.LastWriteTime = TapeUtils.ParseTimeStamp(.changetime).Ticks
                            FileInfo.ChangeTime = TapeUtils.ParseTimeStamp(.changetime).Ticks
                            FileInfo.IndexNumber = 0
                            FileInfo.HardLinks = 0
                        End With
                    End If
                End With
                Return True
            Else
                FileName = ""
                FileInfo = New FileInfo()
                Return False
            End If
        End Function
    End Class

    Public Class LTFSMountFuseSvc
        Inherits Fsp.Service
        Public LW As LTFSWriter
        Public _Host As Fsp.FileSystemHost
        Public TapeDrive As String = ""
        Public ReadOnly Property MountPath As String
            Get
                Return TapeDrive.Split({"\"}, StringSplitOptions.RemoveEmptyEntries).Last
            End Get
        End Property
        Public Sub New()
            MyBase.New("LTFSMountFuseServie")
        End Sub

        Protected Overrides Sub OnStart(Args As String())
            Dim Host As New Fsp.FileSystemHost(New LTFSMountFSBase(TapeDrive) With {.LW = LW})
            Host.Prefix = $"\ltfs\{MountPath}"
            Host.FileSystemName = "LTFS"
            Dim Code As Integer = Host.Mount("L:", Nothing, True, 0)
            _Host = Host
            'MessageBox.Show($"Code {Code} Name={Host.FileSystemName} MP={Host.MountPoint} Pf={Host.Prefix}")
        End Sub
        Protected Overrides Sub OnStop()
            _Host.Unmount()
            _Host = Nothing
        End Sub
    End Class
    Private Sub 挂载盘符只读ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 挂载盘符只读ToolStripMenuItem.Click
        '挂载
        Dim DriveLoc As String = TapeDrive
        If DriveLoc = "" Then DriveLoc = "\\.\TAPE0"
        Dim MountPath As String = DriveLoc.Split({"\"}, StringSplitOptions.RemoveEmptyEntries).ToList.Last
        Static svc As New LTFSMountFuseSvc()
        svc.LW = Me
        svc.TapeDrive = DriveLoc

        Task.Run(
            Sub()
                svc.Run()
            End Sub)

        MessageBox.Show($"Mounted as \\ltfs\{svc.MountPath}{vbCrLf}Press OK to unmount")

        '卸载
        svc.Stop()
        MessageBox.Show($"Unmounted. Code={svc.ExitCode}")
    End Sub

    Private Sub 子目录列表ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 子目录列表ToolStripMenuItem.Click
        Dim result As New StringBuilder
        If ListView1.Tag IsNot Nothing AndAlso TypeOf (ListView1.Tag) Is ltfsindex.directory Then
            For Each Dir As ltfsindex.directory In CType(ListView1.Tag, ltfsindex.directory).contents._directory
                result.AppendLine(Dir.name)
            Next
        End If
        Clipboard.SetText(result.ToString)
    End Sub

    Private Sub 文件详情ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 文件详情ToolStripMenuItem1.Click
        Dim result As New StringBuilder
        If ListView1.Tag IsNot Nothing AndAlso
        ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 Then
            SyncLock ListView1.SelectedItems
                For Each ItemSelected As ListViewItem In ListView1.SelectedItems
                    If ItemSelected.Tag IsNot Nothing AndAlso TypeOf (ItemSelected.Tag) Is ltfsindex.file Then
                        Dim f As ltfsindex.file = ItemSelected.Tag
                        result.AppendLine(f.GetSerializedText())
                    End If
                Next
            End SyncLock
        End If
        Clipboard.SetText(result.ToString)
    End Sub

    Private Sub XAttrToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles XAttrToolStripMenuItem.Click
        Dim result As New StringBuilder
        If ListView1.Tag IsNot Nothing AndAlso
        ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 Then
            SyncLock ListView1.SelectedItems
                For Each ItemSelected As ListViewItem In ListView1.SelectedItems
                    If ItemSelected.Tag IsNot Nothing AndAlso TypeOf (ItemSelected.Tag) Is ltfsindex.file Then
                        Dim f As ltfsindex.file = ItemSelected.Tag
                        result.AppendLine(f.GetXAttrText())
                    End If
                Next
            End SyncLock
        End If
        Clipboard.SetText(result.ToString)
    End Sub

    Private Sub 启动FTP服务只读ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 启动FTP服务只读ToolStripMenuItem.Click
        Dim svc As New FTPService()
        AddHandler svc.LogPrint, Sub(s As String)
                                     PrintMsg($"FTPSVC> {s}")
                                 End Sub
        svc.port = Integer.Parse(InputBox("Port", "FTP Service", "8021"))
        svc.schema = schema
        svc.TapeDrive = TapeDrive
        svc.BlockSize = plabel.blocksize
        svc.ExtraPartitionCount = ExtraPartitionCount
        svc.StartService()
        MessageBox.Show($"Service running on port {svc.port}.")
        svc.StopService()
        MessageBox.Show("Service stopped.")
    End Sub

    Private Sub 索引间隔36GiBToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 索引间隔36GiBToolStripMenuItem.Click
        IndexWriteInterval = Val(InputBox(ResText_SIIntv.Text, ResText_Setting.Text, IndexWriteInterval))
    End Sub

    Private Sub 容量刷新间隔30sToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 容量刷新间隔30sToolStripMenuItem.Click
        Dim s As String = InputBox(ResText_SCIntv.Text, ResText_Setting.Text, CapacityRefreshInterval)
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
            Do While (i <fileCount)
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
