Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Text
Imports Fsp.Interop
Imports Microsoft.WindowsAPICodePack.Dialogs

Public Class LTFSWriter
    <Category("LTFSWriter")>
    Public Property TapeDrive As String = ""
    <Category("LTFSWriter")>
    Public Property schema As ltfsindex
    <Category("LTFSWriter")>
    Public Property plabel As New ltfslabel With {.blocksize = 524288}
    <Category("LTFSWriter")>
    Public Property Modified As Boolean = False
    <Category("LTFSWriter")>
    Public Property OfflineMode As Boolean = False
    <Category("LTFSWriter")>
    Public Property IndexPartition As Byte = 0
    <Category("LTFSWriter")>
    Public Property DataPartition As Byte = 1
    Private _EncryptionKey As Byte()
    <Category("LTFSWriter")>
    Public Property EncryptionKey As Byte()
        Get
            Return _EncryptionKey
        End Get
        Set(value As Byte())
            _EncryptionKey = value
            If value IsNot Nothing AndAlso value.Length = 32 Then
                IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "encryption.key"), BitConverter.ToString(value).Replace("-", "").ToUpper)
            Else
                If IO.File.Exists(IO.Path.Combine(Application.StartupPath, "encryption.key")) Then
                    IO.File.Delete(IO.Path.Combine(Application.StartupPath, "encryption.key"))
                End If
            End If
        End Set
    End Property
    Public Function GetPartitionNumber(partition As ltfslabel.PartitionLabel) As Byte
        If plabel Is Nothing Then Return partition
        If partition = plabel.partitions.index Then
            Return IndexPartition
        Else
            Return DataPartition
        End If
    End Function

    <Category("LTFSWriter")>
    Public Property IndexWriteInterval As Long
        Get
            Return My.Settings.LTFSWriter_IndexWriteInterval
        End Get
        Set(value As Long)
            value = Math.Max(0, value)
            My.Settings.LTFSWriter_IndexWriteInterval = value
            If value = 0 Then
                索引间隔36GiBToolStripMenuItem.Text = My.Resources.ResText_NoIndex
            Else
                索引间隔36GiBToolStripMenuItem.Text = $"{My.Resources.ResText_IndexInterval}{IOManager.FormatSize(value)}"
            End If
            My.Settings.Save()
        End Set
    End Property

    Private _TotalBytesUnindexed As Long
    <Category("LTFSWriter")>
    Public Property TotalBytesUnindexed As Long
        Set(value As Long)
            _TotalBytesUnindexed = value
            If Not 更新数据区索引ToolStripMenuItem.Enabled AndAlso
                value <> 0 AndAlso schema IsNot Nothing AndAlso
                schema.location.partition = ltfsindex.PartitionLabel.b Then Invoke(Sub() 更新数据区索引ToolStripMenuItem.Enabled = True)
        End Set
        Get
            Return _TotalBytesUnindexed
        End Get
    End Property
    <Category("LTFSWriter")>
    Public Property TotalBytesProcessed As Long = 0
    <Category("LTFSWriter")>
    Public Property TotalFilesProcessed As Long = 0
    <Category("LTFSWriter")>
    Public Property CurrentBytesProcessed As Long = 0
    <Category("LTFSWriter")>
    Public Property CurrentFilesProcessed As Long = 0
    <Category("LTFSWriter")>
    Public Property CurrentHeight As Long = 0
    <Category("LTFSWriter")>
    Public ReadOnly Property GetPos As TapeUtils.PositionData
        Get
            Return TapeUtils.ReadPosition(driveHandle)
        End Get
    End Property
    <Category("LTFSWriter")>
    Public Property ExtraPartitionCount As Byte = 0
    <Category("LTFSWriter")>
    Public Property CapReduceCount As Long = 0
    <Category("LTFSWriter")>
    Public Property CapacityRefreshInterval As Integer
        Get
            Return My.Settings.LTFSWriter_CapacityRefreshInterval
        End Get
        Set(value As Integer)
            value = Math.Max(0, value)
            My.Settings.LTFSWriter_CapacityRefreshInterval = value
            If value = 0 Then
                容量刷新间隔30sToolStripMenuItem.Text = My.Resources.ResText_CRDisabled
            Else
                容量刷新间隔30sToolStripMenuItem.Text = $"{My.Resources.ResText_CRIntv}{value}s"
            End If
        End Set
    End Property
    Private _SpeedLimit As Integer = 0
    <Category("LTFSWriter")>
    Public Property SpeedLimit As Integer
        Set(value As Integer)
            value = Math.Max(0, value)
            _SpeedLimit = value
            If _SpeedLimit = 0 Then
                限速不限制ToolStripMenuItem.Text = My.Resources.ResText_NoSLim
            Else
                限速不限制ToolStripMenuItem.Text = $"{My.Resources.ResText_SLim}{_SpeedLimit} MiB/s"
            End If
        End Set
        Get
            Return _SpeedLimit
        End Get
    End Property
    <Category("LTFSWriter")>
    Public Property SpeedLimitLastTriggerTime As Date = Now
    <Category("LTFSWriter")>
    Public Property CheckCount As Integer = 0
    <Category("LTFSWriter")>
    Public Property CheckCycle As Integer = 10
    <Category("LTFSWriter")>
    Public Property CleanCycle
        Set(value)
            value = Math.Max(0, value)
            If value = 0 Then
                重装带前清洁次数3ToolStripMenuItem.Text = My.Resources.ResText_RBCoff
            Else
                重装带前清洁次数3ToolStripMenuItem.Text = $"{My.Resources.ResText_RBC}{value}"
            End If
            My.Settings.LTFSWriter_CleanCycle = value
            My.Settings.Save()
        End Set
        Get
            Return My.Settings.LTFSWriter_CleanCycle
        End Get
    End Property
    <Category("LTFSWriter")>
    Public Property HashOnWrite As Boolean
        Get
            Return 计算校验ToolStripMenuItem.Checked
        End Get
        Set(value As Boolean)
            计算校验ToolStripMenuItem.Checked = value
        End Set
    End Property

    <Category("LTFSWriter")>
    Public Property AllowOperation As Boolean = True
    Public OperationLock As New Object
    <Category("LTFSWriter")>
    Public Property Barcode As String = ""
    <Category("LTFSWriter")>
    Public Property StopFlag As Boolean = False
    <Category("LTFSWriter")>
    Public Property Pause As Boolean = False
    <Category("LTFSWriter")>
    Public Property Flush As Boolean = False
    <Category("LTFSWriter")>
    Public Property ForceFlush As Boolean = False
    <Category("LTFSWriter")>
    Public Property Clean As Boolean = False
    <Category("LTFSWriter")>
    Public Property Clean_last As Date = Now
    <Category("LTFSWriter")>
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
    <Category("LTFSWriter")>
    Public Property Session_Start_Time As Date = Now
    <Category("LTFSWriter")>
    Public Property logFile As String = IO.Path.Combine(Application.StartupPath, $"log\LTFSWriter_{Session_Start_Time.ToString("yyyyMMdd_HHmmss.fffffff")}.log")
    <Category("LTFSWriter")>
    Public Property SilentMode As Boolean = False
    <Category("LTFSWriter")>
    Public Property SilentAutoEject As Boolean = False
    <Category("LTFSWriter")>
    Public Property BufferedBytes As Long = 0
    Private ddelta, fdelta, rwhdelta, rwtdelta As Long
    <Category("LTFSWriter")>
    Public Property SMaxNum As Integer = 600
    <Category("LTFSWriter")>
    Public Property PMaxNum As Integer = 3600 * 6
    <Category("LTFSWriter")>
    Public Property SpeedHistory As List(Of Double) = New Double(PMaxNum) {}.ToList()
    <Category("LTFSWriter")>
    Public Property ErrRateLog As List(Of Double) = New Double(PMaxNum) {}.ToList()
    <Category("LTFSWriter")>
    Public Property FileRateHistory As List(Of Double) = New Double(PMaxNum) {}.ToList()

    Public FileDroper As FileDropHandler
    Public Event LTFSLoaded()
    Public Event WriteFinished()
    Public Event TapeEjected()

    <Category("LTFSWriter")>
    Public Property MyClipBoard As New LTFSClipBoard With {.ContentChanged =
        Sub()
            Me.Invoke(Sub()
                          粘贴选中ToolStripMenuItem.Visible = Not MyClipBoard.IsEmpty
                          粘贴选中ToolStripMenuItem1.Visible = Not MyClipBoard.IsEmpty
                      End Sub)
        End Sub}
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class LTFSClipBoard
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of ltfsindex.directory), ltfsindex.directory)))>
        Public Property Directory As New List(Of ltfsindex.directory)
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of ltfsindex.file), ltfsindex.file)))>
        Public Property File As New List(Of ltfsindex.file)
        Public ContentChanged As Action
        Public ReadOnly Property IsEmpty
            Get
                Return Directory.Count + File.Count = 0
            End Get
        End Property
        Public Sub Add(content As ltfsindex.file)
            File.Add(content)
            ContentChanged()
        End Sub
        Public Sub Add(content As IEnumerable(Of ltfsindex.file))
            File.AddRange(content)
            ContentChanged()
        End Sub
        Public Sub Add(content As ltfsindex.directory)
            Directory.Add(content)
            ContentChanged()
        End Sub
        Public Sub Add(content As IEnumerable(Of ltfsindex.directory))
            Directory.AddRange(content)
            ContentChanged()
        End Sub
        Public Sub Clear()
            Directory.Clear()
            File.Clear()
            ContentChanged()
        End Sub
    End Class

    Public Sub Load_Settings()

        覆盖已有文件ToolStripMenuItem.Checked = My.Settings.LTFSWriter_OverwriteExist
        跳过符号链接ToolStripMenuItem.Checked = My.Settings.LTFSWriter_SkipSymlink
        显示文件数ToolStripMenuItem.Checked = My.Settings.LTFSWriter_ShowFileCount
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
        预读文件数5ToolStripMenuItem.Text = $"{My.Resources.ResText_PFC}{My.Settings.LTFSWriter_PreLoadFileCount}"
        文件缓存32MiBToolStripMenuItem.Text = $"{My.Resources.ResText_FB}{IOManager.FormatSize(My.Settings.LTFSWriter_PreLoadBytes)}"
        禁用分区ToolStripMenuItem.Checked = DisablePartition
        速度下限ToolStripMenuItem.Text = $"{My.Resources.ResText_SMin}{My.Settings.LTFSWriter_AutoCleanDownLim} MiB/s"
        速度上限ToolStripMenuItem.Text = $"{My.Resources.ResText_SMax}{My.Settings.LTFSWriter_AutoCleanUpperLim} MiB/s"
        持续时间ToolStripMenuItem.Text = $"{My.Resources.ResText_STime}{My.Settings.LTFSWriter_AutoCleanTimeThreashould}s"
        错误率ToolStripMenuItem.Text = $"{My.Resources.ResText_ErrRateLog}{My.Settings.LTFSWriter_AutoCleanErrRateLogThreashould}"
        去重SHA1ToolStripMenuItem.Checked = My.Settings.LTFSWriter_DeDupe
        右下角显示容量损失ToolStripMenuItem.Checked = My.Settings.LTFSWriter_ShowLoss
        Select Case My.Settings.LTFSWriter_PowerPolicyOnWriteBegin
            Case Guid.Empty
                无更改ToolStripMenuItem.Checked = True
            Case New Guid("381b4222-f694-41f0-9685-ff5bb260df2e")
                平衡ToolStripMenuItem.Checked = True
            Case New Guid("a1841308-3541-4fab-bc81-f71556f20b4a")
                节能ToolStripMenuItem.Checked = True
            Case New Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")
                高性能ToolStripMenuItem.Checked = True
            Case Else
                其他ToolStripMenuItem.Checked = True
                其他ToolStripMenuItem.Text = $"{My.Resources.ResText_Other}: {My.Settings.LTFSWriter_PowerPolicyOnWriteBegin.ToString()}"
        End Select
        Select Case My.Settings.LTFSWriter_PowerPolicyOnWriteEnd
            Case Guid.Empty
                无更改ToolStripMenuItem1.Checked = True
            Case New Guid("381b4222-f694-41f0-9685-ff5bb260df2e")
                平衡ToolStripMenuItem1.Checked = True
            Case New Guid("a1841308-3541-4fab-bc81-f71556f20b4a")
                节能ToolStripMenuItem1.Checked = True
            Case New Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")
                高性能ToolStripMenuItem1.Checked = True
            Case Else
                其他ToolStripMenuItem1.Checked = True
                其他ToolStripMenuItem1.Text = $"{My.Resources.ResText_Other}: {My.Settings.LTFSWriter_PowerPolicyOnWriteEnd.ToString()}"
        End Select
        Chart1.Titles(1).Text = My.Resources.ResText_SpeedBT
        Chart1.Titles(2).Text = My.Resources.ResText_FileRateBT
        TapeUtils.AllowPartition = Not DisablePartition
        CleanCycle = CleanCycle
        IndexWriteInterval = IndexWriteInterval
        CapacityRefreshInterval = CapacityRefreshInterval
    End Sub
    Public Sub Save_Settings()
        My.Settings.LTFSWriter_OverwriteExist = 覆盖已有文件ToolStripMenuItem.Checked
        My.Settings.LTFSWriter_SkipSymlink = 跳过符号链接ToolStripMenuItem.Checked
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
        If 无更改ToolStripMenuItem.Checked Then
            My.Settings.LTFSWriter_PowerPolicyOnWriteBegin = Guid.Empty
        ElseIf 平衡ToolStripMenuItem.Checked Then
            My.Settings.LTFSWriter_PowerPolicyOnWriteBegin = New Guid("381b4222-f694-41f0-9685-ff5bb260df2e")
        ElseIf 节能ToolStripMenuItem.Checked Then
            My.Settings.LTFSWriter_PowerPolicyOnWriteBegin = New Guid("a1841308-3541-4fab-bc81-f71556f20b4a")
        ElseIf 高性能ToolStripMenuItem.Checked Then
            My.Settings.LTFSWriter_PowerPolicyOnWriteBegin = New Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")
        End If
        If 无更改ToolStripMenuItem1.Checked Then
            My.Settings.LTFSWriter_PowerPolicyOnWriteEnd = Guid.Empty
        ElseIf 平衡ToolStripMenuItem1.Checked Then
            My.Settings.LTFSWriter_PowerPolicyOnWriteEnd = New Guid("381b4222-f694-41f0-9685-ff5bb260df2e")
        ElseIf 节能ToolStripMenuItem1.Checked Then
            My.Settings.LTFSWriter_PowerPolicyOnWriteEnd = New Guid("a1841308-3541-4fab-bc81-f71556f20b4a")
        ElseIf 高性能ToolStripMenuItem1.Checked Then
            My.Settings.LTFSWriter_PowerPolicyOnWriteEnd = New Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")
        End If
        My.Settings.Save()
    End Sub
    Private Text3 As String = "", Text5 As String = ""
    Private TextT3 As String = "", TextT5 As String = ""
    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        Static blinkcycle As Integer
        Const blinkticks As Integer = 2
        blinkcycle += 1
        blinkcycle = blinkcycle Mod (2 * blinkticks)
        ToolStripStatusLabel3.Text = Text3
        ToolStripStatusLabel3.ToolTipText = TextT3
        ToolStripStatusLabel5.Text = Text5
        ToolStripStatusLabel5.ToolTipText = TextT5
        If TapeUtils.IsOpened(driveHandle) Then
            If IOCtlNum > 0 Then
                If blinkcycle < blinkticks Then
                    ToolStripStatusLabelS6.ForeColor = Color.Transparent
                Else
                    ToolStripStatusLabelS6.ForeColor = Color.LimeGreen
                End If
            Else
                ToolStripStatusLabelS6.ForeColor = Color.Green
            End If
        Else
            ToolStripStatusLabelS6.ForeColor = Color.Gray
        End If
        If blinkcycle < blinkticks Then
            If ToolStripStatusLabelS3.ForeColor <> Color.Gray Then
                ToolStripStatusLabelS3.ForeColor = Color.Transparent
            End If
            If ToolStripStatusLabelS4.ForeColor <> Color.Gray Then
                ToolStripStatusLabelS4.ForeColor = Color.Transparent
            End If
        Else
            If ToolStripStatusLabelS3.ForeColor <> Color.Gray Then
                ToolStripStatusLabelS3.ForeColor = Color.Orange
            End If
            If ToolStripStatusLabelS4.ForeColor <> Color.Gray Then
                ToolStripStatusLabelS4.ForeColor = Color.Orange
            End If
        End If
        If schema IsNot Nothing AndAlso
            schema._directory IsNot Nothing AndAlso
            schema._directory.Count > 0 AndAlso
            schema._directory(0) IsNot Nothing AndAlso (
            ListView1.Items Is Nothing OrElse
            ListView1.Items.Count = 0) Then
            Try
                Dim img As Image = IOManager.FitImage(My.Resources.dragdrop, ListView1.Size)
                ListView1.CreateGraphics().DrawImage(img, 0, 0)
            Catch ex As Exception

            End Try
        End If
    End Sub
    Public Enum LWStatus
        NotReady
        Idle
        Busy
        Succ
        Err
    End Enum
    Public Sub SetStatusLight(status As LWStatus)
        Invoke(Sub()
                   Select Case status
                       Case LWStatus.NotReady
                           ToolStripStatusLabelS1.ForeColor = Color.Gray
                           ToolStripStatusLabelS1.ToolTipText = My.Resources.ResText_NotReady
                       Case LWStatus.Idle
                           ToolStripStatusLabelS1.ForeColor = Color.Blue
                           ToolStripStatusLabelS1.ToolTipText = My.Resources.ResText_Idle
                       Case LWStatus.Busy
                           ToolStripStatusLabelS1.ForeColor = Color.Orange
                           ToolStripStatusLabelS1.ToolTipText = My.Resources.ResText_Busy
                       Case LWStatus.Succ
                           ToolStripStatusLabelS1.ForeColor = Color.Green
                           ToolStripStatusLabelS1.ToolTipText = My.Resources.ResText_Succ
                       Case LWStatus.Err
                           ToolStripStatusLabelS1.ForeColor = Color.Red
                           ToolStripStatusLabelS1.ToolTipText = My.Resources.ResText_Error
                   End Select
               End Sub)

    End Sub
    Public Sub PrintMsg(s As String, Optional ByVal Warning As Boolean = False, Optional ByVal TooltipText As String = "", Optional ByVal LogOnly As Boolean = False, Optional ByVal ForceLog As Boolean = False)
        Me.BeginInvoke(Sub()
                           If ForceLog OrElse My.Settings.LTFSWriter_LogEnabled Then
                               Dim logType As String = "info"
                               If Warning Then logType = "warn"
                               Dim ExtraMsg As String = ""
                               If TooltipText IsNot Nothing AndAlso TooltipText <> "" Then
                                   ExtraMsg = $"({TooltipText})"
                               End If
                               If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "log")) Then
                                   IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "log"))
                               End If
                               IO.File.AppendAllText(logFile, $"{vbCrLf}{Now.ToString("yyyy-MM-dd HH:mm:ss")} {logType}> {s} {ExtraMsg}")
                           End If
                           If LogOnly Then Exit Sub
                           If TooltipText IsNot Nothing AndAlso TooltipText = "" Then TooltipText = s
                           If Not Warning Then
                               Text3 = s
                               If TooltipText IsNot Nothing Then TextT3 = TooltipText
                           Else
                               Text5 = s
                               If TooltipText IsNot Nothing Then TextT5 = TooltipText
                           End If
                       End Sub)
    End Sub
    <Category("LTFSWriter")>
    Public Property DataCompressionLogPage As TapeUtils.PageData
    <Category("UI")>
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of Object), Object)))>
    Public ReadOnly Property ControlList As List(Of Object)
        Get
            Dim result As New List(Of Object)
            Dim q As New List(Of Control)
            For Each c As Control In Controls
                q.Add(c)
            Next
            While q.Count > 0
                Dim q2 As New List(Of Control)
                For Each c As Control In q
                    result.Add(c)
                    If TypeOf c Is SplitContainer Then
                        For Each d As Control In CType(c, SplitContainer).Panel1.Controls
                            q2.Add(d)
                        Next
                        For Each d As Control In CType(c, SplitContainer).Panel2.Controls
                            q2.Add(d)
                        Next
                    ElseIf TypeOf c Is Panel Then
                        For Each d As Control In CType(c, Panel).Controls
                            q2.Add(d)
                        Next
                    ElseIf TypeOf c Is TabControl Then
                        For Each d As TabPage In CType(c, TabControl).TabPages
                            For Each e As Control In d.Controls
                                q2.Add(e)
                            Next
                        Next
                    End If
                Next
                q = q2
            End While
            Return result
        End Get
    End Property
    <Category("UI")>
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of NamedObject), NamedObject)))>
    Public ReadOnly Property Field As List(Of NamedObject)
        Get
            Dim result As New List(Of NamedObject)
            For Each f As Reflection.FieldInfo In Me.GetType().GetFields(
                Reflection.BindingFlags.Public Or
                Reflection.BindingFlags.NonPublic Or
                Reflection.BindingFlags.Instance Or
                Reflection.BindingFlags.Static)
                Dim o As Object = f.GetValue(Me)
                If o IsNot Nothing Then result.Add(New NamedObject(f.Name, f, Me))
            Next
            Return result
        End Get
    End Property
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class MyThreadInfo
        Public Property Address As ULong
        Public Property ManagedThreadId As Integer
        Public Property Stack As String
        Public Property IsThreadpoolCompletionPort As Boolean
        Public Property IsThreadpoolWorker As Boolean
        Public Property IsThreadpoolWait As Boolean
        Public Property OSThreadId As UInteger
        Public Property IsAborted As Boolean
        Public Property IsAlive As Boolean
        Public Property IsBackground As Boolean
        Public Property IsGC As Boolean
        Public Property IsThreadpoolGate As Boolean
        Public Property IsThreadpoolTimer As Boolean
    End Class
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class StackTraceResult
        Public Property Message As String
        Public Property ThreadCount As Integer
        Public Property ThreadInfo As New List(Of MyThreadInfo)
        Public Property StackSummary As Dictionary(Of String, Integer)
    End Class
    <Category("Application")>
    Public ReadOnly Property StackTraces As StackTraceResult
        Get
            Try
                Dim threadLogDic As New Dictionary(Of String, Integer)()
                Dim threads = New List(Of MyThreadInfo)()

                Using target As Microsoft.Diagnostics.Runtime.DataTarget = Microsoft.Diagnostics.Runtime.DataTarget.CreateSnapshotAndAttach(Process.GetCurrentProcess().Id)
                    Dim runtime As Microsoft.Diagnostics.Runtime.ClrRuntime = target.ClrVersions.First().CreateRuntime()
                    ' We can't get the thread name from the ClrThead objects, so we'll look for
                    ' Thread instances on the heap and get the names from those.    
                    For Each thread As Microsoft.Diagnostics.Runtime.ClrThread In runtime.Threads
                        Dim t As Microsoft.Diagnostics.Runtime.ClrThread = thread

                        Dim stack As String = ""
                        For Each clrStackFrame In thread.EnumerateStackTrace()
                            stack += $"{clrStackFrame.Method}" & vbLf
                        Next
                        threads.Add(New MyThreadInfo() With {
                            .Address = t.Address,
                            .ManagedThreadId = t.ManagedThreadId,
                            .OSThreadId = t.OSThreadId,
                            .IsAborted = Nothing,
                            .IsAlive = t.IsAlive,
                            .IsBackground = Nothing,
                            .IsGC = t.IsGc,
                            .IsThreadpoolGate = Nothing,
                            .IsThreadpoolTimer = Nothing,
                            .IsThreadpoolWait = Nothing,
                            .IsThreadpoolWorker = Nothing,
                            .IsThreadpoolCompletionPort = Nothing,
                            .Stack = stack
                        })
                    Next
                End Using

                Dim stackDic = threads.GroupBy(Function(t) t.Stack).ToDictionary(Function(t) t.Key, Function(t) t.Count)

                Dim output As New StackTraceResult
                output.ThreadCount = threads.Count
                output.ThreadInfo = threads
                output.StackSummary = stackDic
                Return output
            Catch e As Exception
                Console.WriteLine(e)
                Return New StackTraceResult With {.Message = e.Message & e.StackTrace}
            End Try

            Return New StackTraceResult With {.Message = "err"}
        End Get

    End Property
    <Category("Application")>
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of NamedObject), NamedObject)))>
    Public ReadOnly Property App As List(Of NamedObject)
        Get
            Dim result As New List(Of NamedObject)
            Dim AppInstance As Application = CType(Activator.CreateInstance(GetType(Application), True), Application)
            For Each f As Reflection.FieldInfo In GetType(Application).GetFields(
                Reflection.BindingFlags.Public Or
                Reflection.BindingFlags.NonPublic Or
                Reflection.BindingFlags.Instance Or
                Reflection.BindingFlags.Static)
                Dim o As Object = f.GetValue(AppInstance)
                If o IsNot Nothing Then result.Add(New NamedObject(f.Name, f, AppInstance))
            Next
            Return result
        End Get
    End Property
    <Category("Application")>
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public ReadOnly Property Computer
        Get
            Return My.Computer
        End Get
    End Property
    <TypeConverter(GetType(ExpandableObjectConverter))>
    <Category("Application")>
    Public ReadOnly Property Forms
        Get
            Return My.Forms
        End Get
    End Property
    <TypeConverter(GetType(ExpandableObjectConverter))>
    <Category("Application")>
    Public ReadOnly Property Settings
        Get
            Return My.Settings
        End Get
    End Property
    '<TypeConverter(GetType(ExpandableObjectConverter))>
    '<Category("Application")>
    'Public ReadOnly Property Resources As List(Of NamedObject)
    '    Get
    '        Dim result As New List(Of NamedObject)
    '        Dim asm As Reflection.Assembly = Reflection.Assembly.GetExecutingAssembly()
    '        Dim tRes As Type = GetType(My.Resources.Resources)
    '        For Each f As Reflection.FieldInfo In tRes.GetFields(
    '            Reflection.BindingFlags.Public Or
    '            Reflection.BindingFlags.NonPublic Or
    '            Reflection.BindingFlags.Instance Or
    '            Reflection.BindingFlags.Static)
    '            Dim o As Object = f.GetValue(Nothing)
    '            If o IsNot Nothing Then result.Add(New NamedObject(f.Name, f, Nothing))
    '        Next
    '        Return result
    '    End Get
    'End Property
    <TypeConverter(GetType(ExpandableObjectConverter))>
    <Category("Application")>
    Public ReadOnly Property User
        Get
            Return My.User
        End Get
    End Property
    <Category("Application")>
    Public ReadOnly Property WebServices
        Get
            Return My.WebServices
        End Get
    End Property
    <Category("TapeUtils")>
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of TapeUtils.PageData), TapeUtils.PageData)))>
    Public ReadOnly Property CurrentLogPages As List(Of TapeUtils.PageData)
        Get
            Return TapeUtils.PageData.GetAllPagesFromDrive(Handle)
        End Get
    End Property
    Public LastNoCCPs(31) As Integer
    Public LastC1Err(31) As Integer
    Public ChanErrLogRateHistory As New List(Of Double)
    Private _ErrLogRateHistory As Double
    Public Property ErrLogRateHistory As Double
        Set(value As Double)
            _ErrLogRateHistory = value
            BeginInvoke(Sub() ToolStripStatusLabelErrLog.Text = $"{value.ToString("f2")}")
            BeginInvoke(Sub()
                            Dim result As New StringBuilder
                            result.Append($"Max Error Rate Log: {value.ToString("f2")}")
                            SyncLock ChanErrLogRateHistory
                                For i As Integer = 0 To ChanErrLogRateHistory.Count - 1
                                    If ChanErrLogRateHistory(i) < 0 Then
                                        result.Append($"{vbCrLf}Channel {i}: {ChanErrLogRateHistory(i).ToString("f2")}")
                                    Else
                                        result.Append($"{vbCrLf}Channel {i}: N/A")
                                    End If

                                Next
                            End SyncLock
                            SetCapLossChannelInfo(result.ToString())
                        End Sub)
        End Set
        Get
            Return _ErrLogRateHistory
        End Get
    End Property
    Public Function ReadChanLRInfo(Optional ByVal TimeOut As Integer = 200) As Double
        Dim result As Double = Double.NegativeInfinity
        Dim debuginfo As New StringBuilder
        Dim WERLHeader As Byte()
        Dim WERLPage As Byte()
        If Threading.Monitor.TryEnter(TapeUtils.SCSIOperationLock, TimeOut) Then
            Dim pos As New TapeUtils.PositionData(driveHandle)
            Dim TapeCapLogPage As TapeUtils.PageData = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_TapeCapacityLogPage, TapeUtils.LogSense(handle:=driveHandle, PageCode:=TapeUtils.PageData.DefaultPages.HPLTO6_TapeCapacityLogPage))
            Dim RemainCapacity As Integer = TapeCapLogPage.TryGetPage(pos.PartitionNumber + 1).GetLong
            Dim TapeUsageLogPage As TapeUtils.PageData = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_TapeUsageLogPage, TapeUtils.LogSense(handle:=driveHandle, PageCode:=TapeUtils.PageData.DefaultPages.HPLTO6_TapeUsageLogPage))
            Dim TotalDataSetW As Integer = TapeUsageLogPage.TryGetPage(2).GetLong
            debuginfo.Append($"[ERRLOGRATE] P={pos.PartitionNumber} B={pos.BlockNumber} RemainCapacity={RemainCapacity} TotalDatasetWritten={TotalDataSetW}{vbTab}")
            WERLHeader = TapeUtils.SCSIReadParam(driveHandle, {&H1C, &H1, &H88, &H0, &H4, &H0}, 4)
            If WERLHeader.Length <> 4 Then
                Threading.Monitor.Exit(TapeUtils.SCSIOperationLock)
                PrintMsg("Invalid page. Skip Errrate Check", LogOnly:=True)
                Return 0
            End If
            Dim WERLPageLen As Integer = WERLHeader(2)
            WERLPageLen <<= 8
            WERLPageLen = WERLPageLen Or WERLHeader(3)
            If WERLPageLen = 0 Then
                Threading.Monitor.Exit(TapeUtils.SCSIOperationLock)
                PrintMsg("Page is empty. Skip Errrate Check", LogOnly:=True)
                Return 0
            End If
            WERLPageLen += 4
            WERLPage = TapeUtils.SCSIReadParam(handle:=driveHandle, cdbData:={&H1C, &H1, &H88, (WERLPageLen >> 8) And &HFF, WERLPageLen And &HFF, &H0}, paramLen:=WERLPageLen)
            Threading.Monitor.Exit(TapeUtils.SCSIOperationLock)
        Else
            PrintMsg("Device is busy. Skip Errrate Check", LogOnly:=True)
            Return 0
        End If
        Dim WERLData As String() = System.Text.Encoding.ASCII.GetString(WERLPage, 4, WERLPage.Length - 4).Split({vbCr, vbLf, vbTab}, StringSplitOptions.RemoveEmptyEntries)
        Try
            Dim AllResults As New List(Of Double)
            For ch As Integer = 4 To WERLData.Length - 5 Step 5
                Dim chan As Integer = (ch - 4) \ 5
                Dim C1err As Integer = Integer.Parse(WERLData(ch + 0), Globalization.NumberStyles.HexNumber)
                'Dim C1cwerr As Integer = Integer.Parse(WERLData(ch + 1), Globalization.NumberStyles.HexNumber)
                'Dim Headerrr As Integer = Integer.Parse(WERLData(ch + 2), Globalization.NumberStyles.HexNumber)
                'Dim WrPasserr As Integer = Integer.Parse(WERLData(ch + 3), Globalization.NumberStyles.HexNumber)
                Dim NoCCPs As Integer = Integer.Parse(WERLData(ch + 4), Globalization.NumberStyles.HexNumber)
                debuginfo.Append($"CH={chan} CCP={NoCCPs} C1={C1err}")
                If NoCCPs - LastNoCCPs(chan) > 0 Then
                    Dim errRateLogValue As Double = Math.Log10((C1err - LastC1Err(chan)) / (NoCCPs - LastNoCCPs(chan)) / 2 / 1920)
                    AllResults.Add(errRateLogValue)
                    If errRateLogValue < 0 Then
                        result = Math.Max(result, errRateLogValue)
                    End If
                    debuginfo.Append($" LR={errRateLogValue}{vbTab}")
                End If
                LastC1Err(chan) = C1err
                LastNoCCPs(chan) = NoCCPs
            Next
            ChanErrLogRateHistory = AllResults
        Catch
        End Try
        If result < -10 Then result = 0
        If result < 0 Then ErrLogRateHistory = result
        debuginfo.Append($" Result={result}")
        PrintMsg(debuginfo.ToString(), LogOnly:=True)
        Return result
    End Function


    Public d_last As Long = 0
    Public t_last As Long = 0
    Public TickCount As Long = 0
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Try
            Dim i As Integer
            If False Then
                Dim lr As Double = Math.Max(-11, ReadChanLRInfo())

                ErrRateLog.Add(lr)
                While ErrRateLog.Count > PMaxNum
                    ErrRateLog.RemoveAt(0)
                End While
                i = 0
                Chart1.Series(2).Points.Clear()
                For Each val As Double In ErrRateLog.GetRange(ErrRateLog.Count - SMaxNum, SMaxNum)
                    Chart1.Series(2).Points.AddXY(i, val)
                    i += 1
                Next
            End If

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
                    ToolStripDropDownButton3.ToolTipText = $"{My.Resources.ResText_C0}{vbCrLf}{My.Resources.ResText_C1}{CapReduceCount}{vbCrLf}"
                    If CapReduceCount >= CleanCycle Then ToolStripDropDownButton3.ToolTipText &= $"{My.Resources.ResText_C2}{Clean_last.ToString("yyyy/MM/dd HH:mm:ss")}"
                End If
                Flush = FlushNow
                If FlushNow Then
                    If CleanCycle > 0 AndAlso (CapReduceCount Mod CleanCycle = 0) Then
                        Flush = False
                        Clean = True
                    End If
                End If
            End If
            If Threading.Monitor.TryEnter(OperationLock) Then
                If AllowOperation Then CheckClean()
                Threading.Monitor.Exit(OperationLock)
            End If

            i = 0
            Chart1.Series(0).Points.Clear()
            For Each val As Double In SpeedHistory.GetRange(SpeedHistory.Count - SMaxNum, SMaxNum)
                Chart1.Series(0).Points.AddXY(i, val)
                i += 1
            Next
            i = 0
            Chart1.Series(1).Points.Clear()
            For Each val As Double In FileRateHistory.GetRange(FileRateHistory.Count - SMaxNum, SMaxNum)
                Chart1.Series(1).Points.AddXY(i, val)
                i += 1
            Next
            Dim USize As Long = UnwrittenSize
            Dim UFile As Long = UnwrittenCount
            ToolStripStatusLabel4.Text = " "
            ToolStripStatusLabel4.Text &= $"{My.Resources.ResText_S0}{IOManager.FormatSize(ddelta)}/s"
            ToolStripStatusLabel4.Text &= $"  {My.Resources.ResText_S1}{IOManager.FormatSize(TotalBytesProcessed)}"
            If CurrentBytesProcessed > 0 Then ToolStripStatusLabel4.Text &= $"({IOManager.FormatSize(CurrentBytesProcessed)})"
            ToolStripStatusLabel4.Text &= $"|{TotalFilesProcessed}"
            If CurrentFilesProcessed > 0 Then ToolStripStatusLabel4.Text &= $"({CurrentFilesProcessed})"
            ToolStripStatusLabel4.Text &= $"  {My.Resources.ResText_S2}"
            If UFile > 0 AndAlso UFile >= CurrentFilesProcessed Then ToolStripStatusLabel4.Text &= $"[{UFile - CurrentFilesProcessed}/{UFile}]"
            ToolStripStatusLabel4.Text &= $"{ IOManager.FormatSize(Math.Max(0, USize - CurrentBytesProcessed))}/{IOManager.FormatSize(USize)}"
            ToolStripStatusLabel4.Text &= $"  {My.Resources.ResText_S3}{IOManager.FormatSize(TotalBytesUnindexed)}"
            ToolStripStatusLabel4.ToolTipText = ToolStripStatusLabel4.Text
            ToolStripStatusLabel6.Text = ""
            If USize > 0 AndAlso CurrentBytesProcessed >= 0 AndAlso CurrentBytesProcessed <= USize Then
                ToolStripProgressBar1.Value = CurrentBytesProcessed / USize * 10000
                ToolStripProgressBar1.ToolTipText = $"{My.Resources.ResText_S4}{IOManager.FormatSize(CurrentBytesProcessed)}/{IOManager.FormatSize(USize)}"
                Dim CurrentTime As Date = Now
                Dim totalTimeCost As Long = (CurrentTime - StartTime).Ticks
                If totalTimeCost > 0 AndAlso CurrentBytesProcessed > 0 Then
                    Dim eteTotalCost As Double = totalTimeCost / CurrentBytesProcessed * USize
                    Dim RemainTicks As Long = Math.Min(Long.MaxValue, eteTotalCost) - totalTimeCost
                    Dim remainTime As New TimeSpan(RemainTicks)
                    ToolStripStatusLabel6.Text = $"{My.Resources.ResText_Remaining} {Math.Truncate(remainTime.TotalHours).ToString().PadLeft(2, "0")}:{remainTime.Minutes.ToString().PadLeft(2, "0")}:{remainTime.Seconds.ToString().PadLeft(2, "0")}"
                    ToolStripProgressBar1.ToolTipText &= vbCrLf & ToolStripStatusLabel6.Text
                End If
            End If
            ToolStripStatusLabel6.ToolTipText = ToolStripStatusLabel6.Text
            Text = GetLocInfo()
            Static GCCollectCounter As Integer
            GCCollectCounter += 1
            If GCCollectCounter >= 60 Then
                GC.Collect()
                GCCollectCounter = 0
            End If
        Catch ex As Exception
            PrintMsg(ex.ToString)
            SetStatusLight(LWStatus.Err)
        End Try
        If TickCount < Long.MaxValue Then
            Threading.Interlocked.Increment(TickCount)
        Else
            TickCount = 0
        End If
    End Sub
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class FileRecord
        Public Property ParentDirectory As ltfsindex.directory
        Public Property SourcePath As String
        Public Property File As ltfsindex.file
        Public Property Buffer As Byte() = Nothing
        Private OperationLock As New Object
        Public Sub RemoveUnwritten()
            ParentDirectory.UnwrittenFiles.Remove(File)
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
                    MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString()}{vbCrLf}{ex.StackTrace}")
                End Try
            End With
            ParentDirectory.UnwrittenFiles.Add(File)
        End Sub
        Public Property fs As IO.FileStream
        Public Property fsB As IO.BufferedStream
        Public Property fsPreRead As IO.FileStream
        Public Property PreReadOffset As Long = 0
        Public Property PreReadByteCount As Long = 0
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
        Public Function Open(Optional BufferSize As Integer = 16777216) As Integer
            SyncLock OperationLock
                While True
                    Try
                        If fs IsNot Nothing Then Return 1
                        fs = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
                        fsPreRead = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
                        Task.Run(Sub() PreReadThread())
                        fsB = New IO.BufferedStream(fs, PreReadBufferSize)
                        Exit While
                    Catch ex As Exception
                        Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr }{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                            Case DialogResult.Abort
                                Return 3
                            Case DialogResult.Retry

                            Case DialogResult.Ignore
                                Return 5
                        End Select
                    End Try
                End While
            End SyncLock
            Return 1
        End Function
        Public Function BeginOpen(Optional BufferSize As Integer = 0, Optional ByVal BlockSize As UInteger = 524288) As Integer
            Dim retryCount As Integer = 0
            While retryCount < 3
                Try
                    SyncLock OperationLock
                        If fs IsNot Nothing Then Return 1
                        If File.length > 0 AndAlso File.length <= BlockSize Then
                            Task.Run(Sub()
                                         SyncLock OperationLock
                                             If Buffer IsNot Nothing Then Buffer = IO.File.ReadAllBytes(SourcePath)
                                         End SyncLock
                                     End Sub)
                            Return 1
                        ElseIf File.length = 0 Then
                            Buffer = {}
                            Return 1
                        End If
                    End SyncLock
                    If BufferSize = 0 Then BufferSize = My.Settings.LTFSWriter_PreLoadBytes
                    If BufferSize = 0 Then BufferSize = 524288
                    Exit While
                Catch ex As Exception
                    Threading.Thread.Sleep(100)
                    Threading.Interlocked.Increment(retryCount)
                End Try
            End While
            Task.Run(Sub()
                         retryCount = 0
                         While retryCount < 3
                             Try
                                 SyncLock OperationLock
                                     If fs IsNot Nothing Then Exit Sub
                                     fs = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
                                     'If PreReadEnabled Then
                                     fsPreRead = New IO.FileStream(SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, BufferSize, True)
                                     Task.Run(Sub() PreReadThread())
                                     'Else
                                     fsB = New IO.BufferedStream(fs, PreReadBufferSize)
                                     'End If
                                     Exit While
                                 End SyncLock
                             Catch ex As Exception
                                 Threading.Thread.Sleep(100)
                                 Threading.Interlocked.Increment(retryCount)
                             End Try
                         End While
                     End Sub)
            Return 1
        End Function
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
            SyncLock OperationLock
                Try
                    If fsB IsNot Nothing Then
                        fsB.Close()
                        fsB.Dispose()
                        fsB = Nothing
                    End If
                Catch ex As Exception
                End Try
                Try
                    fs.Close()
                    fs.Dispose()
                    fs = Nothing
                Catch ex As Exception
                End Try
                If fsPreRead IsNot Nothing Then
                    fsPreRead.Close()
                    fsPreRead.Dispose()
                    fsPreRead = Nothing
                End If
            End SyncLock
        End Sub
        Public Sub CloseAsync()
            Task.Run(Sub()
                         SyncLock OperationLock
                             Try
                                 If fsB IsNot Nothing Then
                                     fsB.Close()
                                     fsB.Dispose()
                                     fsB = Nothing
                                 End If
                             Catch ex As Exception
                             End Try
                             Try
                                 fs.Close()
                                 fs.Dispose()
                                 fs = Nothing
                             Catch ex As Exception
                             End Try
                             If fsPreRead IsNot Nothing Then
                                 fsPreRead.Close()
                                 fsPreRead.Dispose()
                                 fsPreRead = Nothing
                             End If
                         End SyncLock
                     End Sub)
        End Sub
        Public Function ReadAllBytes() As Byte()
            If Buffer IsNot Nothing AndAlso Buffer.Length > 0 Then
                Dim result As Byte() = Buffer
                Buffer = Nothing
                Return result
            Else
                Return IO.File.ReadAllBytes(SourcePath)
            End If
        End Function
    End Class
    <TypeConverter(GetType(ExpandableObjectConverter))>
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
    <Category("LTFSWriter")>
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of FileRecord), FileRecord)))>
    Public Property UnwrittenFiles As New List(Of FileRecord)
    <Category("LTFSWriter")>
    Public Property UnwrittenSizeOverrideValue As ULong = 0
    <Category("LTFSWriter")>
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
    <Category("LTFSWriter")>
    Public Property UnwrittenCountOverwriteValue As ULong = 0
    <Category("LTFSWriter")>
    Public ReadOnly Property UnwrittenCount
        Get
            If UnwrittenCountOverwriteValue > 0 Then Return UnwrittenCountOverwriteValue
            Return UnwrittenFiles.Count
        End Get
    End Property
    <Category("LTFSWriter")>
    Public Property LastRefresh As Date = Now
    <Category("LTFSWriter")>
    Public Property driveHandle As IntPtr
    <Category("TapeUtils")>
    Public Property IOCtlNum As Integer

    Private Sub LTFSWriter_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim scrH As Integer = Screen.GetWorkingArea(Me).Height
        If scrH - Top - Height <= 0 Then
            Height += scrH - Top - Height
        End If
        FileDroper = New FileDropHandler(ListView1)
        Load_Settings()
        If OfflineMode Then Exit Sub
        Try
            TapeUtils.OpenTapeDrive(TapeDrive, driveHandle)
            读取索引ToolStripMenuItem_Click(sender, e)
        Catch ex As Exception
            PrintMsg(My.Resources.ResText_ErrP)
            SetStatusLight(LWStatus.Err)
        End Try
        AddHandler TapeUtils.IOCtlStart, Sub() Threading.Interlocked.Increment(_IOCtlNum)
        AddHandler TapeUtils.IOCtlFinished, Sub() Threading.Interlocked.Decrement(_IOCtlNum)
        Task.Run(Sub()
                     Dim LastTick As Long = 0
                     Dim UIHangCount As Integer = 0
                     Dim DebugPanelAutoShowup As Boolean = True
                     Dim ToolTipChanErrLogShown As Boolean = False
                     Dim ToolTipChanErrLogShownLock As New Object
                     Dim thF12Lock As New Object
                     While True AndAlso Me IsNot Nothing AndAlso Me.Visible
                         Try
                             Threading.Thread.Sleep(Timer1.Interval)
                             Dim NowTick As Long = TickCount
                             If LastTick = NowTick Then
                                 UIHangCount += 1
                             Else
                                 UIHangCount = 0
                             End If
                             LastTick = NowTick
                             If UIHangCount > 20 AndAlso DebugPanelAutoShowup Then
                                 Dim th As New Threading.Thread(
                                    Sub()
                                        If Threading.Monitor.TryEnter(thF12Lock) Then
                                            If MessageBox.Show("UI hang detected. Show debug panel?", "Debug", MessageBoxButtons.OKCancel) = DialogResult.OK Then
                                                DebugPanelAutoShowup = False
                                                Dim SP1 As New SettingPanel
                                                SP1.Text = Text
                                                SP1.SelectedObject = Me
                                                SP1.ShowDialog()
                                                DebugPanelAutoShowup = True
                                            End If
                                            Threading.Monitor.Exit(thF12Lock)
                                        End If
                                    End Sub)
                                 If Threading.Monitor.TryEnter(thF12Lock) Then
                                     Threading.Monitor.Exit(thF12Lock)
                                     th.Start()
                                 End If

                             End If
                             If driveHandle <> -1 AndAlso TapeDrive.Length > 0 Then
                                 If Threading.Monitor.TryEnter(TapeUtils.SCSIOperationLock, 200) Then
                                     Threading.Monitor.Exit(TapeUtils.SCSIOperationLock)
                                     SyncLock TapeUtils.SCSIOperationLock
                                         RefreshDriveLEDIndicator()
                                     End SyncLock
                                 End If
                             End If
                             If ToolTipChanErrLogShowing Then
                                 If Not ToolTipChanErrLogShown Then
                                     BeginInvoke(Sub() ToolTipChanErrLog.Show(CapLossChannelInfo, StatusStrip2, New Point(ToolStripStatusLabelErrLog.Bounds.Right - 1, ToolStripStatusLabelErrLog.Bounds.Bottom - 1)))
                                     ToolTipChanErrLogShown = True
                                 End If
                             Else
                                 ToolTipChanErrLogShown = False
                                 'If ToolTipChanErrLogShown Then
                                 '    If Threading.Monitor.TryEnter(ToolTipChanErrLogShownLock) Then
                                 '        Task.Run(Sub()
                                 '                     Threading.Thread.Sleep(1000)
                                 '                     If Threading.Interlocked.Exchange(ToolTipChanErrLogShowingChanged, False) Then Exit Sub
                                 '                     If Not ToolTipChanErrLogShowing Then
                                 '                         BeginInvoke(Sub() ToolTipChanErrLog.Hide(StatusStrip2))
                                 '                         ToolTipChanErrLogShown = False
                                 '                     End If
                                 '                 End Sub)
                                 '        Threading.Monitor.Exit(ToolTipChanErrLogShownLock)
                                 '    End If
                                 '
                                 'End If
                             End If
                         Catch ex As Exception

                         End Try
                     End While
                 End Sub)
    End Sub
    Private Sub LTFSWriter_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Static ForceCloseCount As Integer = 0
        e.Cancel = False
        If Not AllowOperation Then
            If ForceCloseCount < 3 Then
                MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_X0)
            Else
                If MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_X1, My.Resources.ResText_Warning, MessageBoxButtons.OKCancel) = DialogResult.OK Then
                    Save_Settings()
                    e.Cancel = False
                    Exit Sub
                End If
            End If
            ForceCloseCount += 1
            e.Cancel = True
            Exit Sub
        End If
        If TotalBytesUnindexed > 0 Then
            If MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_X2, My.Resources.ResText_Warning, MessageBoxButtons.YesNo) = DialogResult.No Then
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
            If MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_X3, My.Resources.ResText_Warning, MessageBoxButtons.YesNo) = DialogResult.No Then
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
        If schema Is Nothing Then Return $"{My.Resources.ResText_NIndex} [{TapeDrive}] - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}{My.Settings.Application_License}"
        Dim info As String = $"{Barcode.TrimEnd()} ".TrimStart()
        If TapeDrive <> "" Then info &= $"[{TapeDrive}] "
        Try
            SyncLock schema
                info &= $"{My.Resources.ResText_Index}{schema.generationnumber} - {My.Resources.ResText_Partition}{schema.location.partition} - {My.Resources.ResText_Block}{schema.location.startblock}"
                If schema.previousgenerationlocation IsNot Nothing Then
                    If schema.previousgenerationlocation.startblock > 0 Then info &= $" ({My.Resources.ResText_Previous}:{My.Resources.ResText_Partition}{schema.previousgenerationlocation.partition} - {My.Resources.ResText_Block}{schema.previousgenerationlocation.startblock})"
                End If
            End SyncLock
            If CurrentHeight > 0 Then info &= $" {My.Resources.ResText_WritePointer}{CurrentHeight}"
            If Modified Then info &= "*"
            info &= $" - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}{My.Settings.Application_License}"
        Catch ex As Exception
            PrintMsg(My.Resources.ResText_RPosErr)
            SetStatusLight(LWStatus.Err)
        End Try
        Return info
    End Function
    Public Function GetProgressImage(ByVal value As Integer, ByVal maximum As Integer, ByVal color As Color) As Bitmap
        If maximum = 0 Then Return Nothing
        Dim result As New Bitmap(100, 1)
        Dim bd As Imaging.BitmapData = result.LockBits(New Rectangle(0, 0, 100, 1), Imaging.ImageLockMode.ReadWrite, Imaging.PixelFormat.Format24bppRgb)
        Dim b(bd.Stride - 1) As Byte
        value = Math.Max(0, value)
        value = Math.Min(value, maximum)
        For i As Integer = 0 To b.Length - 1
            b(i) = 255
        Next
        For i As Integer = 0 To value / maximum * 99
            b(i * 3 + 0) = color.B
            b(i * 3 + 1) = color.G
            b(i * 3 + 2) = color.R
        Next
        Marshal.Copy(b, 0, bd.Scan0, b.Length)
        result.UnlockBits(bd)
        Return result
    End Function
    <Category("LTFSWriter")>
    Public Property MaxCapacity As Long = 0
    <Category("LTFSWriter")>
    Public Property CapacityLogPage As TapeUtils.PageData
    <Category("LTFSWriter")>
    Public Property VolumeStatisticsLogPage As TapeUtils.PageData
    Public Property DeviceStatusLogPage As TapeUtils.PageData
    Public Property DTDStatusLogPage As TapeUtils.PageData
    Public Sub RefreshDriveLEDIndicator()
        Dim logdataDSLP As Byte() = TapeUtils.LogSense(driveHandle, &H3E, PageControl:=1)
        Dim logdataDTD As Byte() = TapeUtils.LogSense(driveHandle, &H11, PageControl:=1)
        Invoke(Sub()
                   DeviceStatusLogPage = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_DeviceStatusLogPage, logdataDSLP)
                   DTDStatusLogPage = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_DataTransferDeviceStatusLogPage, logdataDTD)
                   Dim DevStatusBits As TapeUtils.PageData = DeviceStatusLogPage.TryGetPage(&H1).GetPage
                   Dim TapeFlag, DriveFlag, CleanFlag, EncryptionFlag As Boolean
                   If DevStatusBits IsNot Nothing Then
                       For Each item As TapeUtils.PageData.DataItem In DevStatusBits.Items
                           Select Case item.Name.ToLower
                               Case "cleaning required flag"
                                   CleanFlag = CleanFlag Or item.RawData(0)
                               Case "cleaning requested flag"
                                   CleanFlag = CleanFlag Or item.RawData(0)
                               Case "device status"
                                   DriveFlag = (item.RawData(0) <> 1)
                               Case "medium status"
                                   TapeFlag = (item.RawData(0) <> 1)
                           End Select
                       Next
                   End If
                   Dim VHFData As TapeUtils.PageData = DTDStatusLogPage.TryGetPage(0).GetPage
                   If VHFData IsNot Nothing Then
                       For Each item As TapeUtils.PageData.DataItem In VHFData.Items
                           Select Case item.Name.ToLower
                               Case "encryption parameters present"
                                   EncryptionFlag = (item.RawData(0) = 1)
                           End Select
                       Next
                   End If
                   If EncryptionFlag Then
                       ToolStripStatusLabelS2.ForeColor = Color.Blue
                   Else
                       ToolStripStatusLabelS2.ForeColor = Color.Gray
                   End If
                   If CleanFlag Then
                       ToolStripStatusLabelS3.ForeColor = Color.Orange
                   Else
                       ToolStripStatusLabelS3.ForeColor = Color.Gray
                   End If
                   If TapeFlag Then
                       ToolStripStatusLabelS4.ForeColor = Color.Orange
                   Else
                       ToolStripStatusLabelS4.ForeColor = Color.Gray
                   End If
                   If DriveFlag Then
                       ToolStripStatusLabelS5.ForeColor = Color.Orange
                   Else
                       ToolStripStatusLabelS5.ForeColor = Color.Gray
                   End If
               End Sub)
    End Sub
    Public Property CapLossChannelInfo As String
    Public Sub SetCapLossChannelInfo(Text As String)
        CapLossChannelInfo = Text
    End Sub
    Public CMOnce As TapeUtils.CMParser
    Public Function RefreshCapacity() As Long()
        Dim result(3) As Long
        Dim logdataCap As Byte() = TapeUtils.LogSense(driveHandle, &H31, PageControl:=1)
        Dim logdataVStat As Byte() = TapeUtils.LogSense(driveHandle, &H17, PageControl:=1)
        Try
            CapacityLogPage = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_TapeCapacityLogPage, logdataCap)
            Dim Gen As Integer, WORM As Boolean, WP As Boolean, GenStr As String = ""
            If logdataVStat Is Nothing OrElse logdataVStat.Length <= 4 Then
                If CMOnce Is Nothing Then CMOnce = New TapeUtils.CMParser(driveHandle)
                If CMOnce IsNot Nothing Then
                    GenStr = CMOnce.CartridgeMfgData.CartridgeTypeAbbr
                End If
            Else
                VolumeStatisticsLogPage = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_VolumeStatisticsLogPage, logdataVStat)
                Dim GenPage As TapeUtils.PageData.DataItem.DynamicParamPage = VolumeStatisticsLogPage.TryGetPage(&H45)
                If GenPage IsNot Nothing Then
                    Gen = Integer.Parse(GenPage.GetString().Last)
                    GenStr = $"L{Gen}"
                    If Gen = 7 OrElse Gen = 8 Then
                        If CMOnce Is Nothing Then CMOnce = New TapeUtils.CMParser(driveHandle)
                        If CMOnce IsNot Nothing Then
                            GenStr = CMOnce.CartridgeMfgData.CartridgeTypeAbbr
                        End If
                    End If
                Else
                    If CMOnce Is Nothing Then CMOnce = New TapeUtils.CMParser(driveHandle)
                    If CMOnce IsNot Nothing Then
                        GenStr = CMOnce.CartridgeMfgData.CartridgeTypeAbbr
                    End If
                End If
                Dim WORMPage As TapeUtils.PageData.DataItem.DynamicParamPage = VolumeStatisticsLogPage.TryGetPage(&H81)
                If WORMPage IsNot Nothing Then WORM = WORMPage.LastByte
                Dim WPPage As TapeUtils.PageData.DataItem.DynamicParamPage = VolumeStatisticsLogPage.TryGetPage(&H80)
                If WPPage IsNot Nothing Then WP = WPPage.LastByte
            End If
            Dim errRate As Double = 0
            If Gen > 0 Then errRate = ReadChanLRInfo()
            RefreshDriveLEDIndicator()

            Dim MediaDescription As String = $"{GenStr}"
            If WORM Then MediaDescription &= " WORM"
            If WP Then MediaDescription &= " RO" Else MediaDescription &= " RW"

            Dim cap0, cap1, max0, max1 As Long
            Dim cp0 As TapeUtils.PageData.DataItem.DynamicParamPage = CapacityLogPage.TryGetPage(1)
            Dim cp1 As TapeUtils.PageData.DataItem.DynamicParamPage = CapacityLogPage.TryGetPage(2)
            Dim mp0 As TapeUtils.PageData.DataItem.DynamicParamPage = CapacityLogPage.TryGetPage(3)
            Dim mp1 As TapeUtils.PageData.DataItem.DynamicParamPage = CapacityLogPage.TryGetPage(4)
            If cp0 IsNot Nothing Then cap0 = cp0.GetLong
            If cp1 IsNot Nothing Then cap1 = cp1.GetLong
            If mp0 IsNot Nothing Then max0 = mp0.GetLong
            If mp1 IsNot Nothing Then max1 = mp1.GetLong


            'cap0 = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 0).AsNumeric
            Dim loss As Long
            If My.Settings.LTFSWriter_ShowLoss Then
                Dim CMInfo As TapeUtils.CMParser
                Try
                    Dim errormsg As Exception = Nothing
                    CMInfo = New TapeUtils.CMParser(TapeUtils.ReceiveDiagCM(driveHandle, TapeUtils.CMParser.Cartridge_mfg.GetCMLength($"L{Gen}")), errormsg)
                    If errormsg IsNot Nothing Then Throw errormsg
                Catch ex As Exception
                    CMInfo = New TapeUtils.CMParser(driveHandle)
                End Try
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

            End If
            Dim lshbits As Byte = 20

            'DAT Unit in KB
            If max0 > 20 * 1024 * 1024 Then lshbits = 10

            If ExtraPartitionCount > 0 Then
                MaxCapacity = max1
                If MaxCapacity = 0 Then MaxCapacity = TapeUtils.MAMAttribute.FromTapeDrive(driveHandle, 0, 1, 1).AsNumeric
                'cap1 = TapeUtils.MAMAttribute.FromTapeDrive(TapeDrive, 0, 0, 1).AsNumeric
                Invoke(Sub()
                           ToolStripStatusLabel2.Text = $"{MediaDescription} {My.Resources.ResText_CapRem} P0:{IOManager.FormatSize(cap0 << lshbits)} P1:{IOManager.FormatSize(cap1 << lshbits)}"
                           ToolStripStatusLabel2.ToolTipText = $"{MediaDescription} {My.Resources.ResText_CapRem} P0:{LTFSConfigurator.ReduceDataUnit(cap0 >> (20 - lshbits))} P1:{LTFSConfigurator.ReduceDataUnit(cap1 >> (20 - lshbits))}"
                           If cap1 >= 4096 Then
                               ToolStripStatusLabel2.BackgroundImage = GetProgressImage(MaxCapacity - cap1, MaxCapacity, Color.FromArgb(121, 196, 232))
                           Else
                               ToolStripStatusLabel2.BackgroundImage = GetProgressImage(MaxCapacity - cap1, MaxCapacity, Color.FromArgb(255, 127, 127))
                           End If
                       End Sub)
                result(2) = max1 - cap1
                result(3) = max1
            Else
                MaxCapacity = max0
                If MaxCapacity = 0 Then MaxCapacity = TapeUtils.MAMAttribute.FromTapeDrive(driveHandle, 0, 1, 0).AsNumeric
                Invoke(Sub()
                           ToolStripStatusLabel2.Text = $"{MediaDescription} {My.Resources.ResText_CapRem} P0:{IOManager.FormatSize(cap0 << lshbits)}"
                           ToolStripStatusLabel2.ToolTipText = $"{MediaDescription} {My.Resources.ResText_CapRem} P0:{LTFSConfigurator.ReduceDataUnit(cap0 >> (20 - lshbits))}"
                           If cap0 >= 4096 Then
                               ToolStripStatusLabel2.BackgroundImage = GetProgressImage(MaxCapacity - cap0, MaxCapacity, Color.FromArgb(121, 196, 232))
                           Else
                               ToolStripStatusLabel2.BackgroundImage = GetProgressImage(MaxCapacity - cap0, MaxCapacity, Color.FromArgb(255, 127, 127))
                           End If
                       End Sub)

            End If
            result(0) = max0 - cap0
            result(1) = max0
            'If errRate < 0 Then
            '    Invoke(Sub()
            '               ToolStripStatusLabel2.Text &= $" Err:{errRate.ToString("f2")}"
            '               ChanInfo &= $" Err:{errRate.ToString("f2")}"
            '           End Sub)
            'End If

            If My.Settings.LTFSWriter_ShowLoss Then
                Invoke(Sub()
                           ToolStripStatusLabel2.Text &= $" Loss:{IOManager.FormatSize(loss)}"
                           ToolStripStatusLabel2.ToolTipText &= $" Loss:{IOManager.FormatSize(loss)}"
                       End Sub)
            End If
            LastRefresh = Now
        Catch ex As Exception
            PrintMsg(My.Resources.ResText_RCErr, TooltipText:=ex.ToString)
            SetStatusLight(LWStatus.Err)
        End Try
        Return result
    End Function
    <Category("LTFSWriter")>
    Public ReadOnly Property GetCapacityMegaBytes As Long
        Get
            If Threading.Monitor.TryEnter(TapeUtils.SCSIOperationLock) Then
                Threading.Monitor.Exit(TapeUtils.SCSIOperationLock)
                If ExtraPartitionCount > 0 Then
                    Return TapeUtils.MAMAttribute.FromTapeDrive(driveHandle, 0, 0, 1).AsNumeric
                Else
                    Return TapeUtils.MAMAttribute.FromTapeDrive(driveHandle, 0, 0, 0).AsNumeric
                End If
            Else
                Return 0
            End If
        End Get
    End Property

    Public Sub RefreshDisplay()
        If schema Is Nothing Then Exit Sub
        Invoke(
            Sub()
                If My.Settings.LTFSWriter_ShowFileCount Then schema._directory(0).DeepRefreshCount()
                Try
                    Dim old_select As ltfsindex.directory = Nothing
                    Dim old_select_path As String = ""
                    Dim new_select As TreeNode = Nothing
                    Dim IterDirectory As Action(Of ltfsindex.directory, TreeNode, Integer) =
                        Sub(dir As ltfsindex.directory, node As TreeNode, ByVal MaxDepth As Integer)
                            Dim NodeExpand As Action =
                                   Sub()
                                       'PrintMsg(dir.name, LogOnly:=True, ForceLog:=True)
                                       SyncLock dir.contents._directory
                                           For Each d As ltfsindex.directory In dir.contents._directory
                                               Dim t As New TreeNode
                                               If My.Settings.LTFSWriter_ShowFileCount Then
                                                   If d.TotalFilesUnwritten = 0 Then
                                                       t.Text = $"{d.TotalFiles.ToString.PadRight(6)}| {d.name}"
                                                   Else
                                                       t.Text = $"{$"{d.TotalFiles.ToString}+{d.TotalFilesUnwritten.ToString}".PadRight(6)}| {d.name}"
                                                   End If
                                               Else
                                                   t.Text = d.name
                                               End If
                                               t.Tag = d
                                               t.ImageIndex = 1
                                               t.SelectedImageIndex = 1
                                               t.StateImageIndex = 1
                                               node.Nodes.Add(t)
                                               IterDirectory(d, t, MaxDepth - 1)
                                               If old_select Is d Then
                                                   new_select = t
                                               End If
                                           Next
                                           'Compressed Dir
                                           For Each f As ltfsindex.file In dir.contents._file
                                               Dim s As String = f.GetXAttr("ltfscopygui.archive")
                                               If s IsNot Nothing AndAlso s.ToLower = "true" Then
                                                   Dim t As New TreeNode
                                                   t.Text = $"*{f.name}"
                                                   t.Tag = f
                                                   t.ImageIndex = 3
                                                   t.SelectedImageIndex = 3
                                                   t.StateImageIndex = 3
                                                   node.Nodes.Add(t)
                                               End If
                                           Next
                                       End SyncLock
                                   End Sub
                            Dim tvNodeExpand As New TreeViewEventHandler(
                                Sub(sender As Object, e As TreeViewEventArgs)
                                    If e.Node IsNot node.Parent Then Exit Sub
                                    If node.Nodes IsNot Nothing AndAlso node.Nodes.Count > 0 Then Exit Sub
                                    NodeExpand()
                                    RemoveHandler TreeView1.AfterExpand, tvNodeExpand
                                End Sub)
                            Dim tvNodeSelect As New TreeViewEventHandler(
                                Sub(sender As Object, e As TreeViewEventArgs)
                                    If e.Node IsNot node Then Exit Sub
                                    If node.Nodes IsNot Nothing AndAlso node.Nodes.Count > 0 Then Exit Sub
                                    NodeExpand()
                                    RemoveHandler TreeView1.AfterSelect, tvNodeSelect
                                End Sub)
                            Dim isParentOfOldSelect As Boolean = False
                            If MaxDepth = 0 AndAlso Not old_select_path.StartsWith(GetPath(node)) Then
                                AddHandler TreeView1.AfterExpand, tvNodeExpand
                                AddHandler TreeView1.AfterSelect, tvNodeSelect
                                MaxDepth = 2
                                Exit Sub
                            Else
                                NodeExpand()
                            End If
                        End Sub
                    If TreeView1.SelectedNode IsNot Nothing Then
                        If TreeView1.SelectedNode.Tag IsNot Nothing Then
                            If TypeOf TreeView1.SelectedNode.Tag Is ltfsindex.directory Then
                                old_select = TreeView1.SelectedNode.Tag
                                old_select_path = GetPath(TreeView1.SelectedNode)
                            End If
                        End If
                    End If
                    If old_select Is Nothing And ListView1.Tag IsNot Nothing Then
                        old_select = ListView1.Tag
                        old_select_path = GetPath(TreeView1.TopNode)
                    End If
                    TreeView1.Nodes.Clear()
                    SyncLock schema._directory
                        For Each d As ltfsindex.directory In schema._directory
                            Dim t As New TreeNode
                            t.Text = d.name
                            t.Tag = d
                            t.ImageIndex = 0
                            TreeView1.Nodes.Add(t)
                            IterDirectory(d, t, 2)
                        Next
                    End SyncLock
                    TreeView1.TopNode.Expand()
                    If new_select IsNot Nothing Then
                        TreeView1.SelectedNode = new_select
                        new_select.Expand()
                    Else
                        TreeView1.SelectedNode = TreeView1.TopNode
                        TreeView1.SelectedNode.Expand()
                    End If
                Catch ex As Exception

                End Try
                Try
                    Text = GetLocInfo()
                    ToolStripStatusLabel4.Text = $"{My.Resources.ResText_DNW} {IOManager.FormatSize(UnwrittenSize)}"
                    ToolStripStatusLabel4.ToolTipText = ToolStripStatusLabel4.Text
                Catch ex As Exception
                    PrintMsg(My.Resources.ResText_RDErr)
                    SetStatusLight(LWStatus.Err)
                End Try

            End Sub)
    End Sub
    Private Sub ToolStripStatusLabel2_Click(sender As Object, e As EventArgs) Handles ToolStripStatusLabel2.Click
        Try
            If True OrElse AllowOperation Then
                Task.Run(Sub()
                             If Threading.Monitor.TryEnter(TapeUtils.SCSIOperationLock) Then
                                 Threading.Monitor.Exit(TapeUtils.SCSIOperationLock)
                                 RefreshCapacity()
                                 PrintMsg(My.Resources.ResText_CRef, TooltipText:=Nothing)
                             End If
                         End Sub)
            Else
                LastRefresh = Now - New TimeSpan(0, 0, CapacityRefreshInterval)
            End If
        Catch ex As Exception
            PrintMsg(My.Resources.ResText_CRefErr)
            SetStatusLight(LWStatus.Err)
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
    Public Function GetPath(ByVal n As TreeNode) As String
        Dim l As New List(Of ltfsindex.directory)
        Dim n0 As TreeNode = n
        While n0 IsNot Nothing AndAlso TypeOf n0.Tag Is ltfsindex.directory
            l.Add(n0.Tag)
            n0 = n0.Parent
        End While
        Dim sb As New StringBuilder
        For i As Integer = l.Count - 1 To 0 Step -1
            sb.Append("\")
            sb.Append(l(i).name)
        Next
        Return sb.ToString()
    End Function
    Public Sub TriggerTreeView1Event()
        If TreeView1.SelectedNode IsNot Nothing AndAlso TreeView1.SelectedNode.Tag IsNot Nothing Then
            ListView1.BeginUpdate()
            Dim old_select_index As Integer, old_node As Object = ListView1.Tag
            If ListView1.SelectedIndices.Count > 0 Then old_select_index = ListView1.SelectedIndices(0) Else old_select_index = -1
            Try

                If TypeOf (TreeView1.SelectedNode.Tag) Is ltfsindex.directory Then
                    If TreeView1.SelectedNode.Parent IsNot Nothing Then
                        压缩索引ToolStripMenuItem.Enabled = True
                        剪切目录ToolStripMenuItem.Enabled = True
                        删除ToolStripMenuItem.Enabled = True
                    Else
                        压缩索引ToolStripMenuItem.Enabled = False
                        剪切目录ToolStripMenuItem.Enabled = False
                        删除ToolStripMenuItem.Enabled = False
                    End If
                    压缩索引ToolStripMenuItem.Visible = True
                    剪切目录ToolStripMenuItem.Enabled = True
                    解压索引ToolStripMenuItem.Visible = False
                    提取ToolStripMenuItem1.Enabled = True
                    校验ToolStripMenuItem1.Enabled = True
                    重命名ToolStripMenuItem.Enabled = True
                    统计ToolStripMenuItem.Enabled = True
                    TextBoxSelectedPath.Text = GetPath(TreeView1.SelectedNode)
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
                            Dim s(15) As String
                            s(0) = f.length
                            s(1) = f.creationtime
                            s(2) = f.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True)
                            s(15) = f.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True)
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
                            If Not f.MD5ForeColor.Equals(Color.Black) Then
                                li.UseItemStyleForSubItems = False
                                li.SubItems(16).ForeColor = f.MD5ForeColor
                            End If
                            ListView1.Items.Add(li)
                        Next

                    End SyncLock
                    SyncLock d.UnwrittenFiles
                        For Each f As ltfsindex.file In d.UnwrittenFiles
                            Dim li As New ListViewItem
                            SyncLock f
                                li.Tag = f
                                li.Text = f.name
                                Dim s(15) As String
                                s(0) = f.length
                                s(1) = f.creationtime
                                s(2) = f.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True)
                                s(15) = f.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True)
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
                ElseIf TypeOf (TreeView1.SelectedNode.Tag) Is ltfsindex.file Then
                    Dim f As ltfsindex.file = TreeView1.SelectedNode.Tag
                    Dim t As String = f.GetXAttr("ltfscopygui.archive")
                    If t IsNot Nothing AndAlso t.ToLower = "true" Then
                        压缩索引ToolStripMenuItem.Visible = False
                        剪切目录ToolStripMenuItem.Enabled = False
                        解压索引ToolStripMenuItem.Visible = True
                        提取ToolStripMenuItem1.Enabled = False
                        校验ToolStripMenuItem1.Enabled = False
                        重命名ToolStripMenuItem.Enabled = False
                        删除ToolStripMenuItem.Enabled = False
                        统计ToolStripMenuItem.Enabled = False
                    End If
                    ListView1.Items.Clear()
                End If
                If ListView1.Items Is Nothing OrElse ListView1.Items.Count = 0 AndAlso schema IsNot Nothing Then
                    'ListView1.BackgroundImage = IOManager.FitImage(My.Resources.dragdrop, ListView1.Size)
                Else
                    'ListView1.BackgroundImage = Nothing
                End If

            Catch ex As Exception
                PrintMsg(My.Resources.ResText_NavErr)
                SetStatusLight(LWStatus.Err)
            End Try
            ListView1.EndUpdate()
            If old_node IsNot Nothing AndAlso ListView1.Tag IsNot Nothing AndAlso old_node Is ListView1.Tag AndAlso old_select_index >= 0 Then
                ListView1.Items(Math.Min(old_select_index, ListView1.Items.Count - 1)).Focused = True
                ListView1.Items(Math.Min(old_select_index, ListView1.Items.Count - 1)).Selected = True
                ListView1.Items(Math.Min(old_select_index, ListView1.Items.Count - 1)).EnsureVisible()
            End If
        End If
    End Sub
    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        TriggerTreeView1Event()
    End Sub
    Private Sub TreeView1_Click(sender As Object, e As EventArgs) Handles TreeView1.Click
        TriggerTreeView1Event()
    End Sub
    Private Sub TreeView1_NodeMouseClick(sender As Object, e As TreeNodeMouseClickEventArgs) Handles TreeView1.NodeMouseClick
        If e.Button = MouseButtons.Right Then
            TreeView1.SelectedNode = e.Node
        End If
    End Sub

    Public Function CheckUnindexedDataSizeLimit(Optional ByVal ForceFlush As Boolean = False, Optional ByVal CheckOnly As Boolean = False) As Boolean
        If CheckOnly Then Return (IndexWriteInterval > 0 AndAlso TotalBytesUnindexed >= IndexWriteInterval) Or ForceFlush
        If (IndexWriteInterval > 0 AndAlso TotalBytesUnindexed >= IndexWriteInterval) Or ForceFlush Then
            WriteCurrentIndex(False, False)
            TotalBytesUnindexed = 0
            Invoke(Sub() Text = GetLocInfo())
            Return True
        End If
        Return False
    End Function

    Public Function TryExecute(ByVal command As Func(Of Byte()))
        Dim succ As Boolean = False
        While Not succ
            Dim sense() As Byte
            Try
                sense = command()
            Catch ex As Exception
                Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_RErrSCSI}{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
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
                PrintMsg($"sense err {TapeUtils.Byte2Hex(sense, True)}", Warning:=True, LogOnly:=True)
                Try
                    Throw New Exception("SCSI sense error")
                Catch ex As Exception
                    Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_RestoreErr}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                        Case DialogResult.Abort
                            Throw New Exception(TapeUtils.ParseSenseData(sense))
                        Case DialogResult.Retry
                            succ = False
                        Case DialogResult.Ignore
                            succ = True
                            Exit While
                    End Select
                End Try
            Else
                succ = True
            End If
        End While
        Return succ
    End Function
    Public Sub WriteCurrentIndex(Optional ByVal GotoEOD As Boolean = True, Optional ByVal ClearCurrentStat As Boolean = True)
        SetStatusLight(LWStatus.Busy)
        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
        If GotoEOD Then TapeUtils.Locate(driveHandle, 0UL, DataPartition, TapeUtils.LocateDestType.EOD)
        Dim CurrentPos As TapeUtils.PositionData = GetPos
        PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
        If ExtraPartitionCount > 0 AndAlso schema IsNot Nothing AndAlso schema.location.partition <> CurrentPos.PartitionNumber Then
            Throw New Exception($"{My.Resources.ResText_CurPos}p{CurrentPos.PartitionNumber}b{CurrentPos.BlockNumber}{My.Resources.ResText_IndexNAllowed}")
            Exit Sub
        End If
        If ExtraPartitionCount > 0 AndAlso schema IsNot Nothing AndAlso schema.location.startblock >= CurrentPos.BlockNumber Then
            Throw New Exception($"{My.Resources.ResText_CurPos}p{CurrentPos.PartitionNumber}b{CurrentPos.BlockNumber}{My.Resources.ResText_IndexNAllowed}")
            Exit Sub
        End If
        TryExecute(Function() As Byte()
                       Return TapeUtils.WriteFileMark(driveHandle)
                   End Function)
        schema.generationnumber += 1
        schema.updatetime = Now.ToUniversalTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
        schema.location.partition = ltfsindex.PartitionLabel.b
        schema.previousgenerationlocation = New ltfsindex.LocationDef With {.partition = schema.location.partition, .startblock = schema.location.startblock}
        CurrentPos = GetPos
        PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
        schema.location.startblock = CurrentPos.BlockNumber
        PrintMsg(My.Resources.ResText_GI)
        Dim tmpf As String = $"{Application.StartupPath}\LWI_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
        schema.SaveFile(tmpf)
        'Dim sdata As Byte() = Encoding.UTF8.GetBytes(schema.GetSerializedText())
        PrintMsg(My.Resources.ResText_WI)
        'TapeUtils.Write(TapeDrive, sdata, plabel.blocksize)
        TapeUtils.Write(driveHandle, tmpf, plabel.blocksize, False)
        IO.File.Delete(tmpf)
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
        TapeUtils.WriteFileMark(driveHandle)
        PrintMsg(My.Resources.ResText_WIF)
        CurrentPos = GetPos
        CurrentHeight = CurrentPos.BlockNumber
        PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
        Modified = ExtraPartitionCount > 0
        SetStatusLight(LWStatus.Succ)
    End Sub
    Public Sub RefreshIndexPartition()
        SetStatusLight(LWStatus.Busy)
        Dim block1 As ULong = schema.location.startblock
        If schema.location.partition = ltfsindex.PartitionLabel.a Then
            block1 = schema.previousgenerationlocation.startblock
        End If
        If ExtraPartitionCount > 0 Then
            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
            PrintMsg(My.Resources.ResText_Locating)
            TapeUtils.Locate(driveHandle, 3UL, IndexPartition, TapeUtils.LocateDestType.FileMark)
            Dim p As TapeUtils.PositionData = GetPos
            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
            TapeUtils.WriteFileMark(driveHandle)
            PrintMsg($"Filemark Written", LogOnly:=True)
            If schema.location.partition = ltfsindex.PartitionLabel.b Then
                schema.previousgenerationlocation = New ltfsindex.LocationDef With {.partition = schema.location.partition, .startblock = schema.location.startblock}
            End If

            schema.location.startblock = p.BlockNumber + 1
            schema.location.partition = ltfsindex.PartitionLabel.a
            p = GetPos
            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
        End If
        Dim block0 As ULong = schema.location.startblock
        If ExtraPartitionCount > 0 Then
            PrintMsg(My.Resources.ResText_GI)
            Dim tmpf As String = $"{Application.StartupPath}\LWI_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
            schema.SaveFile(tmpf)
            PrintMsg(My.Resources.ResText_WI)
            TapeUtils.Write(driveHandle, tmpf, plabel.blocksize, False)
            IO.File.Delete(tmpf)
            TapeUtils.WriteFileMark(driveHandle)
            PrintMsg(My.Resources.ResText_WIF)
            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
        End If
        TapeUtils.WriteVCI(driveHandle, schema.generationnumber, block0, block1, schema.volumeuuid.ToString(), ExtraPartitionCount)
        Modified = False
        SetStatusLight(LWStatus.Succ)
    End Sub
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Data"></param>
    ''' <param name="RetainPosisiton"></param>
    ''' <returns>
    ''' Data start position
    ''' </returns>
    Public Function DumpDataToIndexPartition(ByVal Data As IO.Stream, Optional ByVal RetainPosisiton As Boolean = True) As Long
        Try
            If ExtraPartitionCount = 0 Then Return -1
            'record previous position
            Dim pPrevious As New TapeUtils.PositionData(driveHandle)
            'locate
            TapeUtils.Locate(driveHandle, 3UL, IndexPartition, TapeUtils.LocateDestType.FileMark)
            Dim pFMIndex As New TapeUtils.PositionData(driveHandle)
            Dim pStartBlock As Long = pFMIndex.BlockNumber
            'Dump old index
            If Not TapeUtils.ReadFileMark(driveHandle) Then Return -1
            Dim tmpf As String = $"{Application.StartupPath}\LIT_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
            TapeUtils.ReadToFileMark(driveHandle, tmpf, plabel.blocksize)
            'Write data
            TapeUtils.Locate(driveHandle, pFMIndex.BlockNumber, pFMIndex.PartitionNumber)
            TapeUtils.Write(handle:=driveHandle, Data:=Data, BlockSize:=plabel.blocksize, senseEnabled:=False)
            'Recover old index
            TapeUtils.WriteFileMark(driveHandle)
            TapeUtils.Write(driveHandle, tmpf, plabel.blocksize, False)
            IO.File.Delete(tmpf)
            TapeUtils.WriteFileMark(driveHandle)
            'Recover position
            If RetainPosisiton Then TapeUtils.Locate(driveHandle, pPrevious.BlockNumber, pPrevious.PartitionNumber)
            Return pStartBlock
        Catch ex As Exception
            MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString()}{vbCrLf}{ex.StackTrace}")
        End Try
        Return -1
    End Function
    Public Sub MoveToIndexPartition(ByVal f As ltfsindex.file)
        Try
            If ExtraPartitionCount = 0 Then Exit Sub
            If f Is Nothing Then Exit Sub
            If f.extentinfo Is Nothing OrElse f.extentinfo.Count = 0 Then Exit Sub
            If f.extentinfo(0).partition = ltfsindex.PartitionLabel.a Then Exit Sub
            Dim tmpf As String = $"{Application.StartupPath}\LFT_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
            RestoreFile(tmpf, f)
            Dim fs As New IO.FileStream(tmpf, IO.FileMode.Open)
            Dim len As Long = fs.Length
            Dim startblock As Integer = DumpDataToIndexPartition(fs)
            f.extentinfo = {New ltfsindex.file.extent With {.startblock = startblock, .bytecount = len, .byteoffset = 0, .fileoffset = 0, .partition = ltfsindex.PartitionLabel.a}}.ToList()
            IO.File.Delete(tmpf)
        Catch ex As Exception
            MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString}")
        End Try

    End Sub
    Public Function DumpDataToIndexPartition(ByVal Data As Byte(), Optional ByVal RetainPosisiton As Boolean = True) As Long
        Dim s As New IO.MemoryStream(Data)
        Return DumpDataToIndexPartition(s, RetainPosisiton)
    End Function
    Public Sub UpdataAllIndex()
        SetStatusLight(LWStatus.Busy)
        If (My.Settings.LTFSWriter_ForceIndex OrElse (TotalBytesUnindexed <> 0)) AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
            PrintMsg(My.Resources.ResText_UDI)
            WriteCurrentIndex(False)
        End If
        PrintMsg(My.Resources.ResText_UI)
        RefreshIndexPartition()
        AutoDump()
        TapeUtils.ReleaseUnit(driveHandle)
        TapeUtils.AllowMediaRemoval(driveHandle)
        PrintMsg(My.Resources.ResText_IUd)
        If schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.a Then Me.Invoke(Sub() 更新数据区索引ToolStripMenuItem.Enabled = False)
        If SilentMode Then
            If SilentAutoEject Then
                TapeUtils.LoadEject(driveHandle, TapeUtils.LoadOption.Eject)
                RaiseEvent TapeEjected()
            End If
        Else
            Dim DoEject As Boolean = False
            Invoke(Sub()
                       DoEject = WA3ToolStripMenuItem.Checked OrElse MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_PEj, My.Resources.ResText_Hint, MessageBoxButtons.OKCancel) = DialogResult.OK
                   End Sub)
            If DoEject Then
                TapeUtils.LoadEject(driveHandle, TapeUtils.LoadOption.Eject)
                PrintMsg(My.Resources.ResText_Ejd)
                RaiseEvent TapeEjected()
            End If
        End If
        Invoke(Sub()
                   LockGUI(False)
                   RefreshDisplay()
               End Sub)

        SetStatusLight(LWStatus.Succ)
    End Sub
    Public Sub OnWriteFinished()
        If WA0ToolStripMenuItem.Checked Then Exit Sub
        If WA1ToolStripMenuItem.Checked Then
            Dim SilentBefore As Boolean = SilentMode
            SilentMode = True
            Try
                If (My.Settings.LTFSWriter_ForceIndex OrElse TotalBytesUnindexed <> 0) AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
                    WriteCurrentIndex(False)
                    TapeUtils.Flush(driveHandle)
                End If
            Catch ex As Exception
                PrintMsg($"{ex.ToString()}{vbCrLf}{ex.StackTrace}")
                SetStatusLight(LWStatus.Err)
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
    <Category("LTFSWriter")>
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
            If StopFlag Then Exit Sub
            If f.Extension.ToLower = ".xattr" Then Exit Sub
            'symlink
            If My.Settings.LTFSWriter_SkipSymlink AndAlso ((f.Attributes And IO.FileAttributes.ReparsePoint) <> 0) Then
                Exit Sub
            End If
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

                        For i As Integer = d.LastUnwrittenFilesCount - 1 To 0 Step -1
                            If f.Name.ToLower = d.UnwrittenFiles(i).name.ToLower Then
                                d.UnwrittenFiles.RemoveAt(i)
                                For j As Integer = UnwrittenFiles.Count - 1 To 0 Step -1
                                    Dim oldf As FileRecord = UnwrittenFiles(j)
                                    If oldf.ParentDirectory Is d AndAlso oldf.File.name.ToLower = f.Name.ToLower Then
                                        UnwrittenFiles.RemoveAt(j)
                                        FileExist = True
                                        Exit For
                                    End If
                                Next
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
            Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString()}{vbCrLf}{ex.StackTrace}"))
        End Try
    End Sub
    Public Sub AddDirectry(dnew1 As IO.DirectoryInfo, d1 As ltfsindex.directory, Optional ByVal OverWrite As Boolean = False, Optional ByVal exceptExtention As String() = Nothing)
        If My.Settings.LTFSWriter_SkipSymlink AndAlso ((dnew1.Attributes And IO.FileAttributes.ReparsePoint) <> 0) Then
            Exit Sub
        End If
        If StopFlag Then Exit Sub
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
            Dim flist As List(Of IO.FileInfo) = dnew1.GetFiles().ToList()
            flist.Sort(New Comparison(Of IO.FileInfo)(Function(a As IO.FileInfo, b As IO.FileInfo) As Integer
                                                          Return ExplorerComparer.Compare(a.Name, b.Name)
                                                      End Function))
            For Each f As IO.FileInfo In flist
                Try
                    If StopFlag Then Exit For
                    If exceptExtention IsNot Nothing AndAlso exceptExtention.Count > 0 Then
                        For Each ext As String In exceptExtention
                            If f.FullName.ToLower().EndsWith(ext.ToLower()) Then
                                Exit Try
                            End If
                        Next
                    End If
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
                                For i As Integer = dT.LastUnwrittenFilesCount - 1 To 0 Step -1
                                    If dT.UnwrittenFiles(i).name.ToLower = f.Name.ToLower Then
                                        dT.UnwrittenFiles.RemoveAt(i)
                                        For j As Integer = UnwrittenFiles.Count - 1 To 0 Step -1
                                            Dim oldf As FileRecord = UnwrittenFiles(j)
                                            If oldf.ParentDirectory Is dT AndAlso oldf.File.name.ToLower = f.Name.ToLower Then
                                                UnwrittenFiles.RemoveAt(j)
                                                FileExist = True
                                                Exit For
                                            End If
                                        Next
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
                    Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString()}{vbCrLf}{ex.StackTrace}"))
                End Try
            Next
        Else
            Parallel.ForEach(dnew1.GetFiles(),
                Sub(f As IO.FileInfo)
                    Try
                        If exceptExtention IsNot Nothing AndAlso exceptExtention.Count > 0 Then
                            For Each ext As String In exceptExtention
                                If f.FullName.ToLower().EndsWith(ext.ToLower()) Then
                                    Exit Try
                                End If
                            Next
                        End If
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
                                            oldf.ParentDirectory.UnwrittenFiles.Remove(oldf.File)
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
                        Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString()}{vbCrLf}{ex.StackTrace}"))
                    End Try
                End Sub)
        End If
        Dim dl As List(Of IO.DirectoryInfo) = dnew1.GetDirectories().ToList()
        dl.Sort(New Comparison(Of IO.DirectoryInfo)(Function(a As IO.DirectoryInfo, b As IO.DirectoryInfo) As Integer
                                                        Return ExplorerComparer.Compare(a.Name, b.Name)
                                                    End Function))
        For Each dn As IO.DirectoryInfo In dnew1.GetDirectories()
            AddDirectry(dn, dT, OverWrite, exceptExtention)
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
                                oldf.ParentDirectory.UnwrittenFiles.Remove(oldf.File)
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
            If TreeView1.SelectedNode.Parent IsNot Nothing AndAlso MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_DelConfrm}{d.name}", My.Resources.ResText_Confirm, MessageBoxButtons.OKCancel) = DialogResult.OK Then
                Dim pd As ltfsindex.directory = TreeView1.SelectedNode.Parent.Tag
                pd.contents._directory.Remove(d)
                If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                Dim IterAllDirectory As Action(Of ltfsindex.directory) =
                    Sub(d1 As ltfsindex.directory)
                        Dim RList As New List(Of FileRecord)
                        SyncLock d1.UnwrittenFiles
                            For Each f As ltfsindex.file In d1.UnwrittenFiles
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
            Dim s As String = InputBox(My.Resources.ResText_DirName, My.Resources.ResText_RenameDir, d.name)
            If s <> "" Then
                If s = d.name Then Exit Sub
                If (s.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) Then
                    MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_DirNIllegal)
                    Exit Sub
                End If
                If TreeView1.SelectedNode.Parent IsNot Nothing Then
                    Dim pd As ltfsindex.directory = TreeView1.SelectedNode.Parent.Tag
                    SyncLock pd.contents._directory
                        For Each d2 As ltfsindex.directory In pd.contents._directory
                            If d2 IsNot d And d2.name = s Then
                                MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_DirNExist)
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
            Dim newname As String = InputBox(My.Resources.ResText_NFName, My.Resources.ResText_Rename, f.name)
            If newname = f.name Then Exit Sub
            If newname = "" Then Exit Sub
            If (newname.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) Then
                MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_FNIllegal)
                Exit Sub
            End If
            SyncLock d.contents._file
                For Each allf As ltfsindex.file In d.contents._file
                    If allf IsNot f And allf.name.ToLower = newname.ToLower Then
                        MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_FNExist)
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
        MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_DelConfrm}{ListView1.SelectedItems.Count}{My.Resources.ResText_Files_C}", My.Resources.ResText_Warning, MessageBoxButtons.OKCancel) = DialogResult.OK Then
            SyncLock ListView1.SelectedItems
                For Each ItemSelected As ListViewItem In ListView1.SelectedItems
                    If ItemSelected.Tag IsNot Nothing AndAlso TypeOf (ItemSelected.Tag) Is ltfsindex.file Then
                        Dim f As ltfsindex.file = ItemSelected.Tag
                        Dim d As ltfsindex.directory = ListView1.Tag
                        If d.UnwrittenFiles.Contains(f) Then
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
            '    Dim f As IO.FileInfo = New IO.FileInfo(fpath)
            '    Try
            '        AddFile(f, d, 覆盖已有文件ToolStripMenuItem.Checked)
            '        PrintMsg("文件添加成功")
            '    Catch ex As Exception
            '        PrintMsg("文件添加失败")
            '        MessageBox.Show(New Form With {.TopMost = True}, ex.ToString())
            '    End Try
            'Next
            'RefreshDisplay()
        End If
    End Sub
    Public Sub AddFileOrDir(d As ltfsindex.directory, Paths As String(), Optional ByVal overwrite As Boolean = False,
                            Optional ByVal exceptExtention As String() = Nothing)
        SetStatusLight(LWStatus.Busy)
        Dim th As New Threading.Thread(
                Sub()
                    StopFlag = False
                    PrintMsg($"{My.Resources.ResText_Adding}{Paths.Length}{My.Resources.ResText_Items_x}")
                    Dim numi As Integer = 0
                    Dim PList As List(Of String) = Paths.ToList()
                    PList.Sort(ExplorerComparer)
                    ltfsindex.WSort({d}.ToList, Nothing, Sub(d1 As ltfsindex.directory)
                                                             d1.LastUnwrittenFilesCount = d1.UnwrittenFiles.Count
                                                         End Sub)
                    For Each path As String In PList
                        If Not path.StartsWith("\\") Then path = $"\\?\{path}"
                        Dim i As Integer = Threading.Interlocked.Increment(numi)
                        If StopFlag Then Exit For
                        Try
                            If IO.File.Exists(path) Then
                                Dim f As IO.FileInfo = New IO.FileInfo(path)
                                Dim skip As Boolean = False
                                If exceptExtention IsNot Nothing AndAlso exceptExtention.Count > 0 Then
                                    For Each ext As String In exceptExtention
                                        If path.ToLower().EndsWith(ext.ToLower()) Then
                                            skip = True
                                            Exit For
                                        End If
                                    Next
                                End If
                                If Not skip Then
                                    PrintMsg($"{My.Resources.ResText_Adding} [{i}/{Paths.Length}] {f.Name}")
                                    AddFile(f, d, overwrite)
                                Else
                                    PrintMsg($"{My.Resources.ResText_Skip} [{i}/{Paths.Length}] {f.Name}")
                                End If
                            ElseIf IO.Directory.Exists(path) Then
                                Dim f As IO.DirectoryInfo = New IO.DirectoryInfo(path)
                                PrintMsg($"{My.Resources.ResText_Adding} [{i}/{Paths.Length}] {f.Name}")
                                AddDirectry(f, d, overwrite, exceptExtention)
                            End If
                        Catch ex As Exception
                            Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString()}{vbCrLf}{ex.StackTrace}"))
                            SetStatusLight(LWStatus.Err)
                        End Try
                    Next

                    If ParallelAdd Then UnwrittenFiles.Sort(New Comparison(Of FileRecord)(Function(a As FileRecord, b As FileRecord) As Integer
                                                                                              Return ExplorerComparer.Compare(a.SourcePath, b.SourcePath)
                                                                                          End Function))
                    StopFlag = False
                    RefreshDisplay()
                    PrintMsg(My.Resources.ResText_AddFin)
                    SetStatusLight(LWStatus.Succ)
                    LockGUI(False)
                End Sub)
        LockGUI()
        th.Start()
    End Sub
    Private Sub ListView1_DragEnter(sender As Object, e As DragEventArgs) Handles ListView1.DragEnter
        If Not AllowOperation OrElse Not MenuStrip1.Enabled Then
            PrintMsg(My.Resources.ResText_DragNA)
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
            Dim dnew As IO.DirectoryInfo = New IO.DirectoryInfo(FolderBrowserDialog1.SelectedPath)
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
            '    MessageBox.Show(New Form With {.TopMost = True}, ex.ToString())
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
                '    Dim dnew As IO.DirectoryInfo = New IO.DirectoryInfo(dirSelected)
                '    Try
                '        AddDirectry(dnew, d, 覆盖已有文件ToolStripMenuItem.Checked)
                '        PrintMsg("目录添加成功")
                '    Catch ex As Exception
                '        PrintMsg("目录添加失败")
                '        MessageBox.Show(New Form With {.TopMost = True}, ex.ToString())
                '    End Try
                'Next
                'RefreshDisplay()
            End If
        End If
    End Sub
    Private Sub 新建目录ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 新建目录ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing Then
            Dim s As String = InputBox(My.Resources.ResText_DirName, My.Resources.ResText_NewDir, "")
            If s <> "" Then
                If (s.Replace("\", "").Replace("/", "").IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0) Then
                    MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_DirNIllegal)
                    Exit Sub
                End If
                Dim dirList As String() = s.Split({"\", "/"}, StringSplitOptions.RemoveEmptyEntries)
                Dim d As ltfsindex.directory = ListView1.Tag
                For Each newdirName As String In dirList

                    SyncLock d.contents._directory
                        For Each dold As ltfsindex.directory In d.contents._directory
                            If dold IsNot d And dold.name = newdirName Then
                                MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_DirNExist)
                                Exit Sub
                            End If
                        Next
                    End SyncLock

                    Dim newdir As New ltfsindex.directory With {
                        .name = newdirName,
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
                    d = newdir
                Next

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
    <Category("LTFSWriter")>
    Public Property RestorePosition As TapeUtils.PositionData
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
                'ElseIf finfo.LastAccessTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z") <> FileIndex.accesstime Then
                '    FileExist = False
            ElseIf finfo.LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z") <> FileIndex.modifytime Then
                FileExist = False
            End If
            If Not FileExist Then PrintMsg($"{My.Resources.ResText_OverwritingDF}{FileName} {finfo.Length}->{FileIndex.length}{vbCrLf _
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
            Dim fi As New IO.FileInfo(FileName)
            fi.Attributes = fi.Attributes And Not IO.FileAttributes.ReadOnly
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
                finfo.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.modifytime)
                finfo.IsReadOnly = FileIndex.readonly
                finfo.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.accesstime)
                Threading.Interlocked.Add(TotalBytesProcessed, FileIndex.length)
                Threading.Interlocked.Add(CurrentBytesProcessed, FileIndex.length)
            Else
                If FileIndex.TempObj Is Nothing OrElse TypeOf FileIndex.TempObj IsNot ltfsindex.file.refFile Then FileIndex.TempObj = New ltfsindex.file.refFile()
                CType(FileIndex.TempObj, ltfsindex.file.refFile).FileName = FileName
                Dim fs As New IO.FileStream(FileName, IO.FileMode.OpenOrCreate, IO.FileAccess.ReadWrite, IO.FileShare.Read, 8388608, IO.FileOptions.None)
                Try
                    FileIndex.extentinfo.Sort(New Comparison(Of ltfsindex.file.extent)(Function(a As ltfsindex.file.extent, b As ltfsindex.file.extent)
                                                                                           If a.startblock <> b.startblock Then Return a.startblock.CompareTo(b.startblock)
                                                                                           Return a.fileoffset.CompareTo(b.fileoffset)
                                                                                       End Function))
                    For Each fe As ltfsindex.file.extent In FileIndex.extentinfo
                        Dim succ As Boolean = False
                        Do
                            Dim BlockAddress As ULong = fe.startblock
                            Dim ByteOffset As Long = fe.byteoffset
                            Dim FileOffset As Long = fe.fileoffset
                            Dim Partition As Long = fe.partition
                            Dim TotalBytes As Long = fe.bytecount
                            'Dim p As New TapeUtils.PositionData(TapeDrive)
                            If RestorePosition Is Nothing OrElse RestorePosition.BlockNumber <> BlockAddress OrElse RestorePosition.PartitionNumber <> Partition Then
                                TapeUtils.Locate(driveHandle, BlockAddress, GetPartitionNumber(Partition), TapeUtils.LocateDestType.Block)
                                RestorePosition = New TapeUtils.PositionData(driveHandle)
                            End If
                            fs.Seek(FileOffset, IO.SeekOrigin.Begin)
                            Dim ReadedSize As Long = 0
                            While (ReadedSize < TotalBytes + ByteOffset) And Not StopFlag
                                Dim CurrentBlockLen As UInteger = Math.Min(plabel.blocksize, TotalBytes + ByteOffset - ReadedSize)
                                Dim Data As Byte()
                                Dim readsucc As Boolean = False
                                While Not readsucc
                                    Dim sense() As Byte = {}
                                    Try
                                        Data = TapeUtils.ReadBlock(driveHandle, sense, CurrentBlockLen, True)

                                    Catch ex As Exception
                                        Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_RErrSCSI}{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                            Case DialogResult.Abort
                                                StopFlag = True
                                                Throw ex
                                            Case DialogResult.Retry
                                                readsucc = False
                                            Case DialogResult.Ignore
                                                readsucc = True
                                                Exit While
                                        End Select
                                        Continue While
                                    End Try
                                    If ((sense(2) >> 6) And &H1) = 1 Then
                                        If (sense(2) And &HF) = 13 Then
                                            readsucc = True
                                        Else
                                            PrintMsg(My.Resources.ResText_EWEOM, True)
                                            readsucc = True
                                            Exit While
                                        End If
                                    ElseIf sense(2) And &HF <> 0 Then
                                        PrintMsg($"sense err {TapeUtils.Byte2Hex(sense, True)}", Warning:=True, LogOnly:=True)
                                        Try
                                            Throw New Exception("SCSI sense error")
                                        Catch ex As Exception
                                            Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_RestoreErr}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                Case DialogResult.Abort
                                                    fs.Close()
                                                    StopFlag = True
                                                    Throw New Exception(TapeUtils.ParseSenseData(sense))
                                                Case DialogResult.Retry
                                                    readsucc = False
                                                Case DialogResult.Ignore
                                                    readsucc = True
                                                    Exit While
                                            End Select
                                        End Try
                                    Else
                                        readsucc = True
                                    End If
                                End While
                                SyncLock RestorePosition
                                    RestorePosition.BlockNumber += 1
                                End SyncLock
                                If Data.Length <> CurrentBlockLen OrElse CurrentBlockLen = 0 Then
                                    PrintMsg($"Error reading at p{RestorePosition.PartitionNumber}b{RestorePosition.BlockNumber}: readed length {Data.Length} should be {CurrentBlockLen}", LogOnly:=True, ForceLog:=True)
                                    succ = False
                                    SetStatusLight(LWStatus.Err)
                                    Exit Do
                                End If
                                ReadedSize += CurrentBlockLen - ByteOffset
                                fs.Write(Data, ByteOffset, CurrentBlockLen - ByteOffset)
                                Threading.Interlocked.Add(TotalBytesProcessed, CurrentBlockLen - ByteOffset)
                                Threading.Interlocked.Add(CurrentBytesProcessed, CurrentBlockLen - ByteOffset)
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
                            PrintMsg($"{FileIndex.name}{My.Resources.ResText_RestoreErr}", ForceLog:=True)
                            SetStatusLight(LWStatus.Err)
                            Exit For
                        End If
                        If StopFlag Then Exit Sub
                    Next
                Catch ex As Exception
                    PrintMsg($"{FileIndex.name}{My.Resources.ResText_RestoreErr}{ex.ToString}", ForceLog:=True)
                    SetStatusLight(LWStatus.Err)
                End Try

                fs.Flush()
                fs.Close()
                Dim finfo As New IO.FileInfo(FileName)
                Try
                    finfo.CreationTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.creationtime)
                    finfo.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.modifytime)
                    finfo.IsReadOnly = FileIndex.readonly
                    finfo.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.accesstime)

                Catch ex As Exception

                End Try
            End If

        Else
            Dim finfo As New IO.FileInfo(FileName)
            finfo.CreationTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.creationtime)
            finfo.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.modifytime)
            finfo.IsReadOnly = FileIndex.readonly
            finfo.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(FileIndex.accesstime)
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
                            StartTime = Now
                            For Each FI As ltfsindex.file In flist
                                UnwrittenSizeOverrideValue += FI.length
                                FI.TempObj = Nothing
                            Next
                            SetStatusLight(LWStatus.Busy)
                            PrintMsg(My.Resources.ResText_Restoring)
                            StopFlag = False
                            TapeUtils.ReserveUnit(driveHandle)
                            TapeUtils.PreventMediaRemoval(driveHandle)
                            RestorePosition = New TapeUtils.PositionData(driveHandle)
                            For Each FileIndex As ltfsindex.file In flist
                                Dim FileName As String = IO.Path.Combine(BasePath, FileIndex.name)
                                RestoreFile(FileName, FileIndex)
                                If StopFlag Then
                                    PrintMsg(My.Resources.ResText_OpCancelled)
                                    SetStatusLight(LWStatus.Idle)
                                    LockGUI(False)
                                    Exit Sub
                                End If
                            Next
                        Catch ex As Exception
                            PrintMsg($"{My.Resources.ResText_RestoreErr}{ex.ToString}", ForceLog:=True)
                            SetStatusLight(LWStatus.Err)
                        End Try
                        TapeUtils.AllowMediaRemoval(driveHandle)
                        TapeUtils.ReleaseUnit(driveHandle)
                        StopFlag = False
                        UnwrittenSizeOverrideValue = 0
                        UnwrittenCountOverwriteValue = 0
                        LockGUI(False)
                        PrintMsg(My.Resources.ResText_RestFin)
                        SetStatusLight(LWStatus.Succ)
                        Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_RestFin))
                    End Sub)
            th.Start()
        End If
    End Sub
    Private Sub 提取ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 提取ToolStripMenuItem1.Click
        If TreeView1.SelectedNode IsNot Nothing AndAlso FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            Dim FileList As New List(Of FileRecord)
            Dim selectedDir As ltfsindex.directory = TreeView1.SelectedNode.Tag
            Dim th As New Threading.Thread(
                    Sub()
                        PrintMsg(My.Resources.ResText_Restoring)
                        SetStatusLight(LWStatus.Busy)
                        Try
                            StopFlag = False
                            Dim IterDir As Action(Of ltfsindex.directory, IO.DirectoryInfo) =
                                Sub(tapeDir As ltfsindex.directory, outputDir As IO.DirectoryInfo)
                                    For Each f As ltfsindex.file In tapeDir.contents._file
                                        f.TempObj = New ltfsindex.file.refFile() With {.FileName = ""}
                                        FileList.Add(New FileRecord With {.File = f, .SourcePath = IO.Path.Combine(outputDir.FullName, f.name)})
                                        'RestoreFile(IO.Path.Combine(outputDir.FullName, f.name), f)
                                    Next
                                    For Each d As ltfsindex.directory In tapeDir.contents._directory
                                        Dim thisDir As String = IO.Path.Combine(outputDir.FullName, d.name)
                                        Dim dirOutput As IO.DirectoryInfo
                                        Dim RestoreTimeStamp As Boolean = Not IO.Directory.Exists(thisDir)
                                        If RestoreTimeStamp Then IO.Directory.CreateDirectory(thisDir)
                                        dirOutput = New IO.DirectoryInfo(thisDir)
                                        IterDir(d, dirOutput)
                                        If RestoreTimeStamp Then
                                            dirOutput.CreationTimeUtc = TapeUtils.ParseTimeStamp(d.creationtime)
                                            dirOutput.LastWriteTimeUtc = TapeUtils.ParseTimeStamp(d.modifytime)
                                            dirOutput.LastAccessTimeUtc = TapeUtils.ParseTimeStamp(d.accesstime)
                                        End If
                                    Next
                                End Sub
                            PrintMsg(My.Resources.ResText_PrepFile)
                            Dim ODir As String = IO.Path.Combine(FolderBrowserDialog1.SelectedPath, selectedDir.name)
                            If Not ODir.StartsWith("\\") Then ODir = $"\\?\{ODir}"
                            If Not IO.Directory.Exists(ODir) Then IO.Directory.CreateDirectory(ODir)
                            IterDir(selectedDir, New IO.DirectoryInfo(ODir))
                            FileList.Sort(New Comparison(Of FileRecord)(Function(a As FileRecord, b As FileRecord) As Integer
                                                                            If a.File.extentinfo Is Nothing And b.File.extentinfo IsNot Nothing Then Return 0.CompareTo(1)
                                                                            If b.File.extentinfo Is Nothing And a.File.extentinfo IsNot Nothing Then Return 1.CompareTo(0)
                                                                            If a.File.extentinfo Is Nothing And b.File.extentinfo Is Nothing Then Return 0.CompareTo(0)
                                                                            If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count <> 0 Then Return 0.CompareTo(1)
                                                                            If b.File.extentinfo.Count = 0 And a.File.extentinfo.Count <> 0 Then Return 1.CompareTo(0)
                                                                            If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count = 0 Then Return 0.CompareTo(0)
                                                                            If a.File.extentinfo(0).partition = ltfsindex.PartitionLabel.a And b.File.extentinfo(0).partition = ltfsindex.PartitionLabel.b Then Return 0.CompareTo(1)
                                                                            If a.File.extentinfo(0).partition = ltfsindex.PartitionLabel.b And b.File.extentinfo(0).partition = ltfsindex.PartitionLabel.a Then Return 1.CompareTo(0)
                                                                            Return a.File.extentinfo(0).startblock.CompareTo(b.File.extentinfo(0).startblock)
                                                                        End Function))
                            For i As Integer = 1 To FileList.Count - 1
                                If FileList(i).File.length = FileList(i - 1).File.length AndAlso FileList(i).File.sha1.Length = 40 AndAlso FileList(i).File.sha1 = FileList(i - 1).File.sha1 Then
                                    FileList(i).File.TempObj = FileList(i - 1).File.TempObj
                                End If
                            Next
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            UnwrittenSizeOverrideValue = 0
                            UnwrittenCountOverwriteValue = FileList.Count
                            StartTime = Now
                            For Each FI As FileRecord In FileList
                                UnwrittenSizeOverrideValue += FI.File.length
                                FI.File.TempObj = Nothing
                            Next
                            PrintMsg(My.Resources.ResText_RestFile)
                            Dim c As Integer = 0
                            TapeUtils.ReserveUnit(driveHandle)
                            TapeUtils.PreventMediaRemoval(driveHandle)
                            RestorePosition = New TapeUtils.PositionData(driveHandle)
                            For Each fr As FileRecord In FileList
                                c += 1
                                PrintMsg($"{My.Resources.ResText_Restoring} [{c}/{FileList.Count}] {fr.File.name}", False, $"{My.Resources.ResText_Restoring} [{c}/{FileList.Count}] {fr.SourcePath}")
                                RestoreFile(fr.SourcePath, fr.File)
                                If StopFlag Then
                                    PrintMsg(My.Resources.ResText_OpCancelled)
                                    SetStatusLight(LWStatus.Idle)
                                    Exit Try
                                End If
                            Next
                            PrintMsg(My.Resources.ResText_RestFin)
                            SetStatusLight(LWStatus.Succ)
                        Catch ex As Exception
                            Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString}"))
                            PrintMsg($"{My.Resources.ResText_RestoreErr}{ex.ToString}", ForceLog:=True)
                            SetStatusLight(LWStatus.Err)
                        End Try
                        TapeUtils.AllowMediaRemoval(driveHandle)
                        TapeUtils.ReleaseUnit(driveHandle)
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
                        PrintMsg(My.Resources.ResText_IUErr, False, $"{My.Resources.ResText_IUErr}: {ex.ToString}")
                        SetStatusLight(LWStatus.Err)
                        LockGUI(False)
                    End Try
                End Sub)
        LockGUI(True)
        th.Start()
    End Sub
    Public Function LocateToWritePosition() As Boolean
        If schema.location.partition = ltfsindex.PartitionLabel.a Then
            Dim p As TapeUtils.PositionData
            While True
                Dim add_code As UShort = TapeUtils.Locate(driveHandle, schema.previousgenerationlocation.startblock, CByte(schema.previousgenerationlocation.partition), TapeUtils.LocateDestType.Block)
                p = GetPos
                If p.PartitionNumber <> CByte(schema.previousgenerationlocation.partition) OrElse p.BlockNumber <> schema.previousgenerationlocation.startblock Then
                    Select Case MessageBox.Show(New Form With {.TopMost = True}, $"Current: P{p.PartitionNumber} B{p.BlockNumber}{vbCrLf}Expected: P{schema.previousgenerationlocation.partition} B{schema.previousgenerationlocation.startblock}{vbCrLf}Additional sense code: 0x{Hex(add_code).ToUpper.PadLeft(4, "0")} {TapeUtils.ParseAdditionalSenseCode(add_code)}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                        Case DialogResult.Ignore
                            Exit While
                        Case DialogResult.Abort
                            LockGUI(False)
                            Return False
                        Case DialogResult.Retry

                    End Select
                Else
                    Exit While
                End If
            End While

            schema.location.startblock = schema.previousgenerationlocation.startblock
            schema.location.partition = schema.previousgenerationlocation.partition
            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
            PrintMsg(My.Resources.ResText_RI)
            Dim tmpf As String = $"{Application.StartupPath}\LWS_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
            TapeUtils.ReadToFileMark(driveHandle, tmpf, plabel.blocksize)
            PrintMsg(My.Resources.ResText_AI)
            'Dim sch2 As ltfsindex = ltfsindex.FromSchemaText(Encoding.UTF8.GetString(schraw))
            Dim sch2 As ltfsindex = ltfsindex.FromSchFile(tmpf)
            IO.File.Delete(tmpf)
            PrintMsg(My.Resources.ResText_AISucc)
            schema.previousgenerationlocation = sch2.previousgenerationlocation
            p = GetPos
            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
            CurrentHeight = p.BlockNumber
            Invoke(Sub() Text = GetLocInfo())
        ElseIf CurrentHeight > 0 Then
            Dim p As TapeUtils.PositionData = GetPos
            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
            If p.BlockNumber <> CurrentHeight Then
                While True
                    Dim add_code As UShort = TapeUtils.Locate(driveHandle, CULng(CurrentHeight), DataPartition, TapeUtils.LocateDestType.Block)
                    p = GetPos
                    If p.PartitionNumber <> DataPartition OrElse p.BlockNumber <> CULng(CurrentHeight) Then
                        Select Case MessageBox.Show(New Form With {.TopMost = True}, $"Current: P{p.PartitionNumber} B{p.BlockNumber}{vbCrLf}Expected: P{DataPartition} B{CULng(CurrentHeight)}{vbCrLf}Additional sense code: 0x{Hex(add_code).ToUpper.PadLeft(4, "0")} {TapeUtils.ParseAdditionalSenseCode(add_code)}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                            Case DialogResult.Ignore
                                Exit While
                            Case DialogResult.Abort
                                LockGUI(False)
                                Return False
                            Case DialogResult.Retry

                        End Select
                    Else
                        Exit While
                    End If
                End While
                PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
            End If
        Else
            Dim p As TapeUtils.PositionData = GetPos
            If MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_CurPos}P{p.PartitionNumber} B{p.BlockNumber}{My.Resources.ResText_NHWrn}", My.Resources.ResText_WriteWarning, MessageBoxButtons.OKCancel) = DialogResult.OK Then

            Else
                LockGUI(False)
                Return False
            End If
        End If
        Return True
    End Function
    Public StartTime As Date
    Private Sub 写入数据ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 写入数据ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
            Sub()
                Dim OnWriteFinishMessage As String = ""
                Try
                    SetStatusLight(LWStatus.Busy)
                    StartTime = Now
                    PrintMsg("", True)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_PrepW)
                    TapeUtils.ReserveUnit(driveHandle)
                    TapeUtils.PreventMediaRemoval(driveHandle)
                    If Not LocateToWritePosition() Then Exit Sub
                    Invoke(Sub() 更新数据区索引ToolStripMenuItem.Enabled = True)
                    If My.Settings.LTFSWriter_PowerPolicyOnWriteBegin <> Guid.Empty Then
                        Process.Start(New ProcessStartInfo With {.FileName = "powercfg",
                                      .Arguments = $"/s {My.Settings.LTFSWriter_PowerPolicyOnWriteBegin.ToString()}",
                                      .WindowStyle = ProcessWindowStyle.Hidden})
                    End If
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
                        Dim wBufferPtr As IntPtr = Marshal.AllocHGlobal(CInt(plabel.blocksize))

                        Dim PNum As Integer = My.Settings.LTFSWriter_PreLoadFileCount
                        If PNum > 0 Then
                            For j As Integer = 0 To PNum
                                If j < WriteList.Count Then WriteList(j).BeginOpen(BlockSize:=plabel.blocksize)
                            Next
                        End If
                        Dim HashTaskAwaitNumber As Integer = 0
                        Threading.ThreadPool.SetMaxThreads(1024, 1024)
                        Threading.ThreadPool.SetMinThreads(256, 256)
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

                        Dim p As New TapeUtils.PositionData(driveHandle)
                        TapeUtils.SetBlockSize(driveHandle, plabel.blocksize)
                        For i As Integer = 0 To WriteList.Count - 1
                            If i < WriteList.Count - 1 Then
                                Dim CFNum As Integer = i
                                Dim dl As New LTFSWriter.FileRecord.PreReadFinishedEventHandler(
                                    Sub()
                                        WriteList(CFNum + 1).BeginOpen()
                                    End Sub)
                                AddHandler WriteList(CFNum).PreReadFinished, dl
                            End If
                            If ExitForFlag Then Exit For
                            PNum = My.Settings.LTFSWriter_PreLoadFileCount
                            If PNum > 0 AndAlso i + PNum < WriteList.Count Then
                                WriteList(i + PNum).BeginOpen(BlockSize:=plabel.blocksize)
                            End If
                            Dim fr As FileRecord = WriteList(i)
                            Try
                                Dim finfo As IO.FileInfo = New IO.FileInfo(fr.SourcePath)
                                fr.File.fileuid = schema.highestfileuid + 1
                                schema.highestfileuid += 1
                                If finfo.Length > 0 Then
                                    'p = New TapeUtils.PositionData(TapeDrive)
                                    'If p.EOP Then PrintMsg(My.Resources.ResText_EWEOM.Text, True)
                                    Dim dupe As Boolean = False
                                    If My.Settings.LTFSWriter_DeDupe Then
                                        Dim dupeFile As ltfsindex.file = Nothing
                                        Dim sha1value As String = ""
                                        For Each fref As ltfsindex.file In AllFile
                                            If fref.length = finfo.Length AndAlso fref.sha1 <> "" Then
                                                PrintMsg($"{My.Resources.ResText_CHashing}: {fr.File.name}  {My.Resources.ResText_Size} {IOManager.FormatSize(fr.File.length)}")
                                                If sha1value = "" Then sha1value = IOManager.SHA1(fr.SourcePath)
                                                If fref.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True).Equals(sha1value) Then
                                                    fr.File.SetXattr(ltfsindex.file.xattr.HashType.SHA1, fref.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True))
                                                    fr.File.SetXattr(ltfsindex.file.xattr.HashType.MD5, fref.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True))
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
                                            PrintMsg($"{My.Resources.ResText_Skip} {fr.File.name}  {My.Resources.ResText_Size} {IOManager.FormatSize(fr.File.length)}", False,
                                                 $"{My.Resources.ResText_Skip}: {fr.SourcePath}{vbCrLf}{My.Resources.ResText_Size}: {IOManager.FormatSize(fr.File.length)}{vbCrLf _
                                                 }{My.Resources.ResText_WrittenTotal}: {IOManager.FormatSize(TotalBytesProcessed) _
                                                 } {My.Resources.ResText_Remaining}: {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed)) _
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
                                        PrintMsg($"{My.Resources.ResText_Writing} {fr.File.name}  {My.Resources.ResText_Size} {IOManager.FormatSize(fr.File.length)}", False,
                                             $"{My.Resources.ResText_Writing}: {fr.SourcePath}{vbCrLf}{My.Resources.ResText_Size}: {IOManager.FormatSize(fr.File.length)}{vbCrLf _
                                             }{My.Resources.ResText_WrittenTotal}: {IOManager.FormatSize(TotalBytesProcessed) _
                                             } {My.Resources.ResText_Remaining}: {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed)) _
                                             } -> {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed - fr.File.length))}")
                                        'write to tape
                                        If finfo.Length <= plabel.blocksize Then
                                            Dim succ As Boolean = False
                                            Dim FileData As Byte()
                                            While True
                                                Try
                                                    FileData = fr.ReadAllBytes()
                                                    Exit While
                                                Catch ex As Exception
                                                    Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr }{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                        Case DialogResult.Abort
                                                            StopFlag = True
                                                            fr.Close()
                                                            Throw ex
                                                        Case DialogResult.Retry

                                                        Case DialogResult.Ignore
                                                            PrintMsg($"Cannot read file {fr.SourcePath}", LogOnly:=True, ForceLog:=True)
                                                            Continue For
                                                    End Select
                                                End Try
                                            End While
                                            If i < WriteList.Count - 1 Then WriteList(i + 1).BeginOpen()
                                            While Not succ
                                                Dim sense As Byte()
                                                Try
                                                    sense = TapeUtils.Write(driveHandle, FileData)
                                                    SyncLock p
                                                        p.BlockNumber += 1
                                                    End SyncLock
                                                Catch ex As Exception
                                                    Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErrSCSI}{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                        Case DialogResult.Abort
                                                            StopFlag = True
                                                            fr.Close()
                                                            Throw ex
                                                        Case DialogResult.Retry
                                                            succ = False
                                                        Case DialogResult.Ignore
                                                            succ = True
                                                            Exit While
                                                    End Select
                                                    p = New TapeUtils.PositionData(driveHandle)
                                                    Continue While
                                                End Try
                                                If ((sense(2) >> 6) And &H1) = 1 Then
                                                    If (sense(2) And &HF) = 13 Then
                                                        PrintMsg(My.Resources.ResText_VOF)
                                                        Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_VOF))
                                                        StopFlag = True
                                                        Exit For
                                                    Else
                                                        PrintMsg(My.Resources.ResText_EWEOM, True)
                                                        succ = True
                                                        Exit While
                                                    End If
                                                ElseIf sense(2) And &HF <> 0 Then
                                                    PrintMsg($"sense err {TapeUtils.Byte2Hex(sense, True)}", Warning:=True, LogOnly:=True)
                                                    Try
                                                        Throw New Exception("SCSI sense error")
                                                    Catch ex As Exception
                                                        Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                            Case DialogResult.Abort
                                                                fr.Close()
                                                                StopFlag = True
                                                                Throw New Exception(TapeUtils.ParseSenseData(sense))
                                                            Case DialogResult.Retry
                                                                succ = False
                                                            Case DialogResult.Ignore
                                                                succ = True
                                                                Exit While
                                                        End Select
                                                    End Try

                                                    p = New TapeUtils.PositionData(driveHandle)
                                                Else
                                                    succ = True
                                                End If
                                            End While
                                            If succ AndAlso HashOnWrite Then
                                                Task.Run(Sub()
                                                             Threading.Interlocked.Increment(HashTaskAwaitNumber)
                                                             Dim sh As New IOManager.CheckSumBlockwiseCalculator
                                                             sh.Propagate(FileData)
                                                             sh.ProcessFinalBlock()
                                                             fr.File.SetXattr(ltfsindex.file.xattr.HashType.SHA1, sh.SHA1Value)
                                                             fr.File.SetXattr(ltfsindex.file.xattr.HashType.MD5, sh.MD5Value)
                                                             Threading.Interlocked.Decrement(HashTaskAwaitNumber)
                                                         End Sub)
                                            End If
                                            If fr.fs IsNot Nothing Then fr.Close()
                                            If Flush Then
                                                If CheckFlush() Then
                                                    If My.Settings.LTFSWriter_PowerPolicyOnWriteBegin <> Guid.Empty Then
                                                        Process.Start(New ProcessStartInfo With {.FileName = "powercfg",
                                                                      .Arguments = $"/s {My.Settings.LTFSWriter_PowerPolicyOnWriteBegin.ToString()}",
                                                                      .WindowStyle = ProcessWindowStyle.Hidden})
                                                    End If
                                                End If
                                            End If
                                            fr.File.WrittenBytes += finfo.Length
                                            TotalBytesProcessed += finfo.Length
                                            CurrentBytesProcessed += finfo.Length
                                            TotalFilesProcessed += 1
                                            CurrentFilesProcessed += 1
                                            TotalBytesUnindexed += finfo.Length
                                        Else
                                            Select Case fr.Open()
                                                Case DialogResult.Ignore
                                                    PrintMsg($"Cannot open file {fr.SourcePath}", LogOnly:=True, ForceLog:=True)
                                                    Continue For
                                                Case DialogResult.Abort
                                                    StopFlag = True
                                                    Throw New Exception(My.Resources.ResText_FileOpenError)
                                            End Select
                                            'PrintMsg($"File Opened:{fr.SourcePath}", LogOnly:=True)
                                            Dim sh As IOManager.CheckSumBlockwiseCalculator = Nothing
                                            If HashOnWrite Then sh = New IOManager.CheckSumBlockwiseCalculator
                                            Dim LastWriteTask As Task = Nothing
                                            Dim ExitWhileFlag As Boolean = False
                                            'Dim tstart As Date = Now
                                            'Dim tsub As Double = 0
                                            While Not StopFlag
                                                Dim buffer(plabel.blocksize - 1) As Byte
                                                Dim BytesReaded As UInteger
                                                While True
                                                    Try
                                                        BytesReaded = fr.Read(buffer, 0, plabel.blocksize)
                                                        Exit While
                                                    Catch ex As Exception
                                                        Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr }{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                            Case DialogResult.Abort
                                                                StopFlag = True
                                                                fr.Close()
                                                                Throw ex
                                                            Case DialogResult.Retry

                                                            Case DialogResult.Ignore
                                                                PrintMsg($"Cannot read file {fr.SourcePath}", LogOnly:=True, ForceLog:=True)
                                                                SetStatusLight(LWStatus.Err)
                                                                Continue For
                                                        End Select
                                                    End Try
                                                End While

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
                                                                'Dim t0 As Date = Now
                                                                sense = TapeUtils.Write(driveHandle, wBufferPtr, BytesReaded, True)
                                                                'tsub += (Now - t0).TotalMilliseconds
                                                                'Invoke(Sub() Text = tsub / (Now - tstart).TotalMilliseconds)
                                                                SyncLock p
                                                                    p.BlockNumber += 1
                                                                End SyncLock
                                                            Catch ex As Exception
                                                                Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErrSCSI}{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                                    Case DialogResult.Abort
                                                                        fr.Close()
                                                                        StopFlag = True
                                                                        Throw ex
                                                                    Case DialogResult.Retry
                                                                        succ = False
                                                                    Case DialogResult.Ignore
                                                                        succ = True
                                                                        Exit While
                                                                End Select
                                                                p = New TapeUtils.PositionData(driveHandle)
                                                                Continue While
                                                            End Try
                                                            If (((sense(2) >> 6) And &H1) = 1) Then
                                                                If ((sense(2) And &HF) = 13) Then
                                                                    PrintMsg(My.Resources.ResText_VOF)
                                                                    Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_VOF))
                                                                    StopFlag = True
                                                                    fr.Close()
                                                                    ExitForFlag = True
                                                                    SetStatusLight(LWStatus.Err)
                                                                    Exit Sub
                                                                Else
                                                                    PrintMsg(My.Resources.ResText_EWEOM, True)
                                                                    succ = True
                                                                    Exit While
                                                                End If
                                                            ElseIf sense(2) And &HF <> 0 Then
                                                                Try
                                                                    Throw New Exception("SCSI sense error")
                                                                Catch ex As Exception
                                                                    Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                                        Case DialogResult.Abort
                                                                            fr.Close()
                                                                            StopFlag = True
                                                                            Throw New Exception(TapeUtils.ParseSenseData(sense))
                                                                        Case DialogResult.Retry
                                                                            succ = False
                                                                        Case DialogResult.Ignore
                                                                            succ = True
                                                                            Exit While
                                                                    End Select
                                                                End Try

                                                                p = New TapeUtils.PositionData(driveHandle)
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
                                                        If Flush Then
                                                            If CheckFlush() Then
                                                                If My.Settings.LTFSWriter_PowerPolicyOnWriteBegin <> Guid.Empty Then
                                                                    Process.Start(New ProcessStartInfo With {.FileName = "powercfg",
                                                                                  .Arguments = $"/s {My.Settings.LTFSWriter_PowerPolicyOnWriteBegin.ToString()}",
                                                                                  .WindowStyle = ProcessWindowStyle.Hidden})
                                                                End If
                                                            End If
                                                        End If
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
                                            If i < WriteList.Count - 1 Then WriteList(i + 1).BeginOpen()
                                            If LastWriteTask IsNot Nothing Then LastWriteTask.Wait()
                                            fr.CloseAsync()
                                            If HashOnWrite AndAlso sh IsNot Nothing AndAlso Not StopFlag Then
                                                Threading.Interlocked.Increment(HashTaskAwaitNumber)
                                                Dim HashTask As Task =
                                                Task.Run(Sub()
                                                             sh.ProcessFinalBlock()
                                                             fr.File.SetXattr(ltfsindex.file.xattr.HashType.SHA1, sh.SHA1Value)
                                                             fr.File.SetXattr(ltfsindex.file.xattr.HashType.MD5, sh.MD5Value)
                                                             sh.StopFlag = True
                                                             Threading.Interlocked.Decrement(HashTaskAwaitNumber)
                                                         End Sub)
                                                If CheckUnindexedDataSizeLimit(CheckOnly:=True) Then
                                                    HashTask.Wait()
                                                    SetStatusLight(LWStatus.Busy)
                                                End If
                                            ElseIf sh IsNot Nothing Then
                                                sh.StopFlag = True
                                            End If
                                            TotalFilesProcessed += 1
                                            CurrentFilesProcessed += 1
                                        End If
                                        p = GetPos
                                        If p.EOP Then PrintMsg(My.Resources.ResText_EWEOM, True)
                                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                                        CurrentHeight = p.BlockNumber
                                    End If
                                Else
                                    fr.File.SetXattr(ltfsindex.file.xattr.HashType.SHA1, "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709")
                                    fr.File.SetXattr(ltfsindex.file.xattr.HashType.MD5, "D41D8CD98F00B204E9800998ECF8427E")
                                    TotalBytesUnindexed += 1
                                    TotalFilesProcessed += 1
                                    CurrentFilesProcessed += 1
                                End If
                                'mark as written
                                fr.ParentDirectory.contents._file.Add(fr.File)
                                fr.ParentDirectory.UnwrittenFiles.Remove(fr.File)
                                If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                                If CheckUnindexedDataSizeLimit() Then
                                    p = New TapeUtils.PositionData(driveHandle)
                                    SetStatusLight(LWStatus.Busy)
                                End If
                                If CapacityRefreshInterval > 0 AndAlso (Now - LastRefresh).TotalSeconds > CapacityRefreshInterval Then
                                    p = New TapeUtils.PositionData(driveHandle)
                                    Dim capValue As Long() = RefreshCapacity()
                                    fr.File.SetXattr("ltfscopygui.capacityremain", capValue(p.PartitionNumber * 2))
                                    Dim p2 As New TapeUtils.PositionData(driveHandle)
                                    If p2.BlockNumber <> p.BlockNumber OrElse p2.PartitionNumber <> p.PartitionNumber Then
                                        Invoke(Sub()
                                                   If MessageBox.Show(New Form With {.TopMost = True}, $"Position changed! {p.BlockNumber} -> {p2.BlockNumber}", "Warning", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
                                                       StopFlag = True
                                                   End If
                                               End Sub)
                                    End If
                                End If
                            Catch ex As Exception
                                MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr}{vbCrLf}{ex.ToString}")
                                PrintMsg($"{My.Resources.ResText_WErr}{ex.Message}{vbCrLf}{ex.StackTrace}")
                                SetStatusLight(LWStatus.Err)
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
                        For Each fr As FileRecord In WriteList
                            Try
                                If fr IsNot Nothing Then fr.Close()
                            Catch ex As Exception
                            End Try
                        Next
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
                        OnWriteFinishMessage = ($"{My.Resources.ResText_WFTime}{(Math.Floor(TimeCost.TotalHours)).ToString().PadLeft(2, "0")}:{TimeCost.Minutes.ToString().PadLeft(2, "0")}:{TimeCost.Seconds.ToString().PadLeft(2, "0")} {My.Resources.ResText_AvgS}{IOManager.FormatSize(TotalBytesWritten \ Math.Max(1, TimeCost.TotalSeconds))}/s")
                        OnWriteFinished()
                    Else
                        OnWriteFinishMessage = (My.Resources.ResText_WCnd)
                    End If
                Catch ex As Exception
                    MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr}{vbCrLf}{ex.ToString}")
                    PrintMsg($"{My.Resources.ResText_WErr}{ex.Message}")
                    SetStatusLight(LWStatus.Err)
                End Try
                TapeUtils.Flush(driveHandle)
                TapeUtils.ReleaseUnit(driveHandle)
                TapeUtils.AllowMediaRemoval(driveHandle)
                If My.Settings.LTFSWriter_PowerPolicyOnWriteEnd <> Guid.Empty Then
                    Process.Start(New ProcessStartInfo With {.FileName = "powercfg",
                                  .Arguments = $"/s {My.Settings.LTFSWriter_PowerPolicyOnWriteEnd.ToString()}",
                                  .WindowStyle = ProcessWindowStyle.Hidden})
                End If
                LockGUI(False)
                RefreshDisplay()
                RefreshCapacity()
                Invoke(Sub()
                           If Not StopFlag AndAlso WA0ToolStripMenuItem.Checked AndAlso MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_WFUp, My.Resources.ResText_OpSucc, MessageBoxButtons.OKCancel) = DialogResult.OK Then
                               更新数据区索引ToolStripMenuItem_Click(sender, e)
                           End If
                           PrintMsg(OnWriteFinishMessage)
                           SetStatusLight(LWStatus.Succ)
                           RaiseEvent WriteFinished()
                       End Sub)
            End Sub)
        StopFlag = False
        LockGUI()
        th.Start()
    End Sub
    Private Sub 清除当前索引后数据ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 清除当前索引后数据ToolStripMenuItem.Click

        If MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_X2, My.Resources.ResText_Warning, MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
            Exit Sub
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    SetStatusLight(LWStatus.Busy)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_Locating)
                    TapeUtils.Locate(handle:=driveHandle, BlockAddress:=schema.location.startblock, Partition:=schema.location.partition, DestType:=TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_RI)
                    Dim outputfile As String = "schema\LTFSIndex_SetEOD_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = IO.Path.Combine(Application.StartupPath, outputfile)
                    TapeUtils.ReadToFileMark(driveHandle, outputfile, plabel.blocksize)
                    Dim CurrentPos As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {CurrentPos.ToString()}", LogOnly:=True)
                    If CurrentPos.PartitionNumber < ExtraPartitionCount Then
                        Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_IPCanc))
                        Exit Try
                    End If
                    TapeUtils.Locate(driveHandle, CULng(CurrentPos.BlockNumber - 1), CurrentPos.PartitionNumber, TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.WriteFileMark(driveHandle)
                    PrintMsg($"FileMark written", LogOnly:=True)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_AI)
                    schema = ltfsindex.FromSchFile(outputfile)
                    PrintMsg(My.Resources.ResText_AISucc)
                    Modified = False
                    Dim p As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                    CurrentHeight = p.BlockNumber
                    If ExtraPartitionCount = 0 Then
                        TapeUtils.Write(driveHandle, {0})
                        PrintMsg($"Byte written", LogOnly:=True)
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                        TapeUtils.Locate(driveHandle, CULng(CurrentHeight), CByte(0), TapeUtils.LocateDestType.Block)
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
                    RefreshDisplay()
                    RefreshCapacity()
                Catch ex As Exception
                    PrintMsg(My.Resources.ResText_RFailed)
                    SetStatusLight(LWStatus.Err)
                End Try
                Modified = False
                PrintMsg(My.Resources.ResText_RollBacked)
                SetStatusLight(LWStatus.Succ)
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
        If MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_RB1}{schema.generationnumber}{My.Resources.ResText_RB2} {My.Resources.ResText_Partition}{schema.location.partition} {My.Resources.ResText_Block}{schema.location.startblock}{vbCrLf _
                           }{My.Resources.ResText_RB3} {My.Resources.ResText_Partition}{schema.previousgenerationlocation.partition} {My.Resources.ResText_Block}{schema.previousgenerationlocation.startblock}{vbCrLf _
                           }{My.Resources.ResText_RB4}", My.Resources.ResText_Warning, MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
            Exit Sub
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    SetStatusLight(LWStatus.Busy)
                    PrintMsg(My.Resources.ResText_RBing)
                    Dim genbefore As Integer = schema.generationnumber
                    Dim prevpart As ltfsindex.PartitionLabel = schema.previousgenerationlocation.partition
                    Dim prevblk As Long = schema.previousgenerationlocation.startblock
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.Locate(handle:=driveHandle, BlockAddress:=schema.previousgenerationlocation.startblock, Partition:=schema.previousgenerationlocation.partition, DestType:=TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_RI)
                    Dim outputfile As String = "schema\LTFSIndex_RollBack_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    outputfile = IO.Path.Combine(Application.StartupPath, outputfile)
                    TapeUtils.ReadToFileMark(driveHandle, outputfile, plabel.blocksize)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_AI)
                    schema = ltfsindex.FromSchFile(outputfile)
                    PrintMsg(My.Resources.ResText_AISucc)
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
                              End Sub)
                    RefreshDisplay()
                    RefreshCapacity()
                Catch ex As Exception
                    PrintMsg(My.Resources.ResText_RFailed)
                    SetStatusLight(LWStatus.Err)
                End Try
                Me.Invoke(Sub()
                              LockGUI(False)
                              Text = GetLocInfo()
                              PrintMsg(My.Resources.ResText_RBFin)
                          End Sub)
                SetStatusLight(LWStatus.Succ)
            End Sub)
        LockGUI()
        th.Start()
    End Sub
    Private Sub 读取索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 读取索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    If Not TapeUtils.IsOpened(driveHandle) Then TapeUtils.OpenTapeDrive(TapeDrive, driveHandle)
                    SetStatusLight(LWStatus.Busy)
                    PrintMsg($"Position = {TapeUtils.ReadPosition(driveHandle).ToString()}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_Locating)
                    ExtraPartitionCount = TapeUtils.ModeSense(driveHandle, &H11)(3)
                    TapeUtils.GlobalBlockLimit = TapeUtils.ReadBlockLimits(driveHandle).MaximumBlockLength
                    If IO.File.Exists(IO.Path.Combine(Application.StartupPath, "blocklen.ini")) Then
                        Dim blval As Integer = Integer.Parse(IO.File.ReadAllText(IO.Path.Combine(Application.StartupPath, "blocklen.ini")))
                        If blval > 0 Then TapeUtils.GlobalBlockLimit = blval
                    End If
                    If IO.File.Exists(IO.Path.Combine(Application.StartupPath, "encryption.key")) Then
                        Dim key As String = (IO.File.ReadAllText(IO.Path.Combine(Application.StartupPath, "encryption.key")))
                        Dim newkey As Byte() = LTFSConfigurator.HexStringToByteArray(key)
                        If newkey.Length <> 32 Then
                            EncryptionKey = Nothing
                        Else
                            Dim sum As Integer = 0
                            For i As Integer = 0 To newkey.Length - 1
                                sum += newkey(i)
                            Next
                            If sum = 0 Then
                                EncryptionKey = Nothing
                            Else
                                EncryptionKey = newkey
                            End If
                        End If
                        TapeUtils.SetEncryption(driveHandle, EncryptionKey)
                    End If
                    TapeUtils.Locate(driveHandle, 0UL, Math.Min(ExtraPartitionCount, IndexPartition), TapeUtils.LocateDestType.Block)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim senseData As Byte()
                    Dim header As String = Encoding.ASCII.GetString(TapeUtils.ReadBlock(driveHandle, senseData))
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim VOL1LabelLegal As Boolean = False
                    VOL1LabelLegal = (header.Length = 80)
                    If VOL1LabelLegal Then VOL1LabelLegal = header.StartsWith("VOL1")
                    If VOL1LabelLegal Then VOL1LabelLegal = (header.Substring(24, 4) = "LTFS")
                    If Not VOL1LabelLegal Then
                        Dim Add_Key As UInt16
                        If senseData.Length >= 14 Then Add_Key = CInt(senseData(12)) << 8 Or senseData(13)
                        PrintMsg(My.Resources.ResText_NVOL1)
                        Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_NLTFS}{vbCrLf}{TapeUtils.ParseSenseData(senseData)}", My.Resources.ResText_Error))
                        LockGUI(False)
                        SetStatusLight(LWStatus.NotReady)
                        Exit Try
                    End If
                    TapeUtils.Locate(driveHandle, 1UL, Math.Min(ExtraPartitionCount, IndexPartition), TapeUtils.LocateDestType.FileMark)
                    PrintMsg(My.Resources.ResText_RLTFSInfo)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    TapeUtils.ReadFileMark(driveHandle)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim pltext As String = Encoding.UTF8.GetString(TapeUtils.ReadToFileMark(driveHandle))
                    plabel = ltfslabel.FromXML(pltext)
                    TapeUtils.SetBlockSize(driveHandle, plabel.blocksize)
                    If plabel.location.partition = plabel.partitions.data Then
                        DataPartition = GetPos().PartitionNumber
                        IndexPartition = (DataPartition + 1) Mod 2
                        If ExtraPartitionCount > 0 Then
                            IndexPartition = 255
                            PrintMsg($"Data partition detected. Switching to index partition", LogOnly:=True)
                            TapeUtils.Locate(driveHandle, 1UL, IndexPartition, TapeUtils.LocateDestType.FileMark)
                            PrintMsg(My.Resources.ResText_RLTFSInfo)
                            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                            TapeUtils.ReadFileMark(driveHandle)
                            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                            pltext = Encoding.UTF8.GetString(TapeUtils.ReadToFileMark(driveHandle))
                            plabel = ltfslabel.FromXML(pltext)
                        End If
                    Else
                        IndexPartition = GetPos().PartitionNumber
                        DataPartition = (IndexPartition + 1) Mod 2
                    End If

                    Barcode = TapeUtils.ReadBarcode(driveHandle)
                    PrintMsg($"Barcode = {Barcode}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_Locating)
                    If ExtraPartitionCount = 0 Then
                        IndexPartition = 0
                        TapeUtils.Locate(driveHandle, 0UL, CByte(0), TapeUtils.LocateDestType.EOD)
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                        PrintMsg(My.Resources.ResText_RI)
                        If DisablePartition Then
                            TapeUtils.Space6(driveHandle, -2, TapeUtils.LocateDestType.FileMark)
                        Else
                            Dim p As TapeUtils.PositionData = GetPos
                            Dim FM As ULong = p.FileNumber
                            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                            If FM <= 1 Then
                                PrintMsg(My.Resources.ResText_IRFailed)
                                Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_NLTFS, My.Resources.ResText_Error))
                                SetStatusLight(LWStatus.Err)
                                LockGUI(False)
                                Exit Try
                            End If
                            TapeUtils.Locate(driveHandle, CULng(FM - 1), CByte(0), TapeUtils.LocateDestType.FileMark)
                        End If
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                        TapeUtils.ReadFileMark(driveHandle)
                    Else
                        TapeUtils.Locate(driveHandle, 3UL, IndexPartition, TapeUtils.LocateDestType.FileMark)
                        PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                        TapeUtils.ReadFileMark(driveHandle)
                    End If
                    PrintMsg(My.Resources.ResText_RI)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    Dim tmpf As String = $"{Application.StartupPath}\LCG_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"

                    TapeUtils.ReadToFileMark(driveHandle, tmpf, plabel.blocksize)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_AI)
                    schema = ltfsindex.FromSchFile(tmpf)
                    If ExtraPartitionCount = 0 Then
                        Dim p As TapeUtils.PositionData = GetPos
                        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                        CurrentHeight = p.BlockNumber
                    Else
                        CurrentHeight = -1
                    End If
                    PrintMsg(My.Resources.ResText_SvBak)
                    Dim FileName As String = ""
                    If Barcode <> "" Then
                        FileName = Barcode
                    Else
                        If schema IsNot Nothing Then
                            FileName = schema.volumeuuid.ToString()
                        End If
                    End If
                    Dim outputfile As String = $"schema\LTFSIndex_Load_{FileName}_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.schema"
                    If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "schema")) Then
                        IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "schema"))
                    End If
                    outputfile = IO.Path.Combine(Application.StartupPath, outputfile)
                    IO.File.Move(tmpf, outputfile)
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            TotalBytesUnindexed = 0
                            Exit While
                        End SyncLock
                    End While
                    Modified = False
                    Me.Invoke(Sub()
                                  MaxCapacity = 0
                                  Text = GetLocInfo()
                                  ToolStripStatusLabel1.Text = Barcode.TrimEnd(" ")
                                  ToolStripStatusLabel1.ToolTipText = $"{My.Resources.ResText_Barcode}:{ToolStripStatusLabel1.Text}{vbCrLf}{My.Resources.ResText_BlkSize}:{plabel.blocksize}"
                              End Sub)
                    RefreshDisplay()
                    RefreshCapacity()

                    PrintMsg(My.Resources.ResText_IRSucc)
                    SetStatusLight(LWStatus.Succ)
                    LockGUI(False)
                    Invoke(Sub() RaiseEvent LTFSLoaded())
                Catch ex As Exception
                    PrintMsg(My.Resources.ResText_IRFailed)
                    PrintMsg($"{ex.ToString}", LogOnly:=True)
                    SetStatusLight(LWStatus.Err)
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
                    If Not TapeUtils.IsOpened(driveHandle) Then TapeUtils.OpenTapeDrive(TapeDrive, driveHandle)
                    SetStatusLight(LWStatus.Busy)
                    PrintMsg(My.Resources.ResText_Locating)
                    Dim currentPos As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {currentPos.ToString()}", LogOnly:=True)
                    If currentPos.PartitionNumber <> 1 Then TapeUtils.Locate(driveHandle, 0UL, CByte(1), TapeUtils.LocateDestType.Block)
                    TapeUtils.Locate(driveHandle, 0UL, DataPartition, TapeUtils.LocateDestType.EOD)
                    PrintMsg(My.Resources.ResText_RI)
                    currentPos = GetPos
                    PrintMsg($"Position = {currentPos.ToString()}", LogOnly:=True)
                    If DisablePartition Then
                        TapeUtils.Space6(driveHandle, -2, TapeUtils.LocateDestType.FileMark)
                    Else
                        Dim FM As Long = currentPos.FileNumber
                        If FM <= 1 Then
                            PrintMsg(My.Resources.ResText_IRFailed)
                            SetStatusLight(LWStatus.Err)
                            Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_NLTFS, My.Resources.ResText_Error))
                            LockGUI(False)
                            Exit Try
                        End If
                        TapeUtils.Locate(handle:=driveHandle, BlockAddress:=FM - 1, Partition:=DataPartition, DestType:=TapeUtils.LocateDestType.FileMark)
                    End If

                    TapeUtils.ReadFileMark(driveHandle)
                    PrintMsg(My.Resources.ResText_RI)
                    Dim outputfile As String = "schema\LTFSIndex_LoadDPIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "schema")) Then
                        IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "schema"))
                    End If
                    outputfile = IO.Path.Combine(Application.StartupPath, outputfile)
                    TapeUtils.ReadToFileMark(driveHandle, outputfile, plabel.blocksize)
                    PrintMsg(My.Resources.ResText_AI)
                    schema = ltfsindex.FromSchFile(outputfile)
                    PrintMsg(My.Resources.ResText_AISucc)
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            TotalBytesUnindexed = 0
                            Exit While
                        End SyncLock
                    End While
                    Modified = False
                    Me.Invoke(Sub()
                                  MaxCapacity = 0
                                  ToolStripStatusLabel1.ToolTipText = ToolStripStatusLabel1.Text
                              End Sub)
                    RefreshDisplay()
                    RefreshCapacity()
                    CurrentHeight = -1
                    PrintMsg(My.Resources.ResText_IRSucc)
                    SetStatusLight(LWStatus.Succ)
                Catch ex As Exception
                    PrintMsg(My.Resources.ResText_IRFailed)
                    SetStatusLight(LWStatus.Err)
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
                SetStatusLight(LWStatus.Busy)
                If (My.Settings.LTFSWriter_ForceIndex OrElse TotalBytesUnindexed <> 0) AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
                    WriteCurrentIndex(False)
                    TapeUtils.Flush(driveHandle)
                    PrintMsg(My.Resources.ResText_DPIWritten)
                    SetStatusLight(LWStatus.Succ)
                End If
            Catch ex As Exception
                PrintMsg(My.Resources.ResText_DPIWFailed, False, $"{My.Resources.ResText_DPIWFailed}: {ex.ToString}")
                Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString()}{vbCrLf}{ex.StackTrace}"))
                SetStatusLight(LWStatus.Err)
            End Try
            Invoke(Sub()
                       LockGUI(False)
                       RefreshDisplay()
                       If Not SilentMode Then MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_DPIUed)
                   End Sub)
        End Sub)
        LockGUI()
        th.Start()
    End Sub
    Public Sub LoadIndexFile(FileName As String, Optional ByVal Silent As Boolean = False)
        Try
            PrintMsg(My.Resources.ResText_RI)
            PrintMsg(My.Resources.ResText_AI)
            Dim sch2 As ltfsindex = ltfsindex.FromSchFile(FileName)
            PrintMsg(My.Resources.ResText_AISucc)
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
            Dim MAM080C As TapeUtils.MAMAttribute = TapeUtils.MAMAttribute.FromTapeDrive(driveHandle, 8, 12, 0)
            Dim VCI As Byte() = {}
            If MAM080C IsNot Nothing Then
                VCI = MAM080C.RawData
            End If
            If Not Silent Then MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_ILdedP}{vbCrLf}{vbCrLf}{My.Resources.ResText_VCID}{vbCrLf}{TapeUtils.Byte2Hex(VCI, True)}")
        Catch ex As Exception
            MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_IAErrp}{ex.Message}")
            SetStatusLight(LWStatus.Err)
        End Try
    End Sub
    Private Sub 加载外部索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 加载外部索引ToolStripMenuItem.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            LoadIndexFile(OpenFileDialog1.FileName)
        End If
    End Sub
    Public Function AutoDump() As String
        SetStatusLight(LWStatus.Busy)
        Dim FileName As String = Barcode
        If FileName = "" Then FileName = schema.volumeuuid.ToString()
        Dim outputfile As String = $"schema\LTFSIndex_Autosave_{FileName _
            }_GEN{schema.generationnumber _
            }_P{schema.location.partition _
            }_B{schema.location.startblock _
            }_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.schema"
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "schema")) Then
            IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "schema"))
        End If
        outputfile = IO.Path.Combine(Application.StartupPath, outputfile)
        PrintMsg(My.Resources.ResText_Exporting)
        schema.SaveFile(outputfile)
        Dim cmData As New TapeUtils.CMParser(driveHandle)
        Try
            Dim CMReport As String = cmData.GetReport()
            If CMReport.Length > 0 Then IO.File.WriteAllText(outputfile.Substring(0, outputfile.Length - 7) & ".cm", CMReport)
        Catch ex As Exception
            SetStatusLight(LWStatus.Err)
        End Try
        PrintMsg(My.Resources.ResText_IndexBaked, False, $"{My.Resources.ResText_IndexBak2}{vbCrLf}{outputfile}")
        SetStatusLight(LWStatus.Succ)
        Return outputfile
    End Function
    Private Sub 备份当前索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 备份当前索引ToolStripMenuItem.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    SetStatusLight(LWStatus.Busy)
                    Dim outputfile As String = AutoDump()
                    Me.Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_IndexBak2}{vbCrLf}{outputfile}"))
                Catch ex As Exception
                    PrintMsg(My.Resources.ResText_IndexBakF)
                    SetStatusLight(LWStatus.Err)
                End Try
                LockGUI(False)
            End Sub)
        LockGUI()
        th.Start()
    End Sub
    Private Sub 格式化ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 格式化ToolStripMenuItem.Click
        If MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_DataLossWarning, My.Resources.ResText_Warning, MessageBoxButtons.OKCancel) = DialogResult.OK Then
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
            If Not TapeUtils.IsOpened(driveHandle) Then TapeUtils.OpenTapeDrive(TapeDrive, driveHandle)
            TapeUtils.ReadPosition(driveHandle)
            Dim modedata As Byte() = TapeUtils.ModeSense(driveHandle, &H11)
            Dim MaxExtraPartitionAllowed As Byte = modedata(2)
            If MaxExtraPartitionAllowed > 1 Then MaxExtraPartitionAllowed = 1
            Dim param As New TapeUtils.MKLTFS_Param(MaxExtraPartitionAllowed)

            If param.MaxExtraPartitionAllowed = 0 Then param.BlockLen = 65536
            param.Barcode = TapeUtils.ReadBarcode(driveHandle)
            param.EncryptionKey = EncryptionKey
            Dim Confirm As Boolean = False
            Dim msDialog As New SettingPanel With {.SelectedObject = param, .StartPosition = FormStartPosition.Manual, .TopMost = True, .Text = $"{格式化ToolStripMenuItem.Text} - {My.Resources.ResText_Setting}"}
            msDialog.Top = Me.Top + Me.Height / 2 - msDialog.Height / 2
            msDialog.Left = Me.Left + Me.Width / 2 - msDialog.Width / 2
            While Not Confirm
                If param.VolumeLabel = "" Then param.VolumeLabel = param.Barcode
                If msDialog.ShowDialog() = DialogResult.Cancel Then Exit Sub
                'param.Barcode = InputBox(My.Resources.ResText_SetBarcode, My.Resources.ResText_Barcode, param.Barcode)
                'param.VolumeLabel = InputBox(My.Resources.ResText_SetVolumeN, My.Resources.ResText_LTFSVolumeN, param.VolumeLabel)

                Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_Barcode2}{param.Barcode}{vbCrLf}{My.Resources.ResText_LTFSVolumeN2}{param.VolumeLabel}", My.Resources.ResText_Confirm, MessageBoxButtons.YesNoCancel)
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

            SetStatusLight(LWStatus.Busy)
            TapeUtils.mkltfs(driveHandle, param.Barcode, param.VolumeLabel, param.ExtraPartitionCount, param.BlockLen, False,
                Sub(Message As String)
                    'ProgressReport
                    PrintMsg(Message)
                End Sub,
                Sub(Message As String)
                    'OnFinished
                    PrintMsg(My.Resources.ResText_FmtFin)
                    SetStatusLight(LWStatus.Succ)
                    LockGUI(False)
                    Me.Invoke(Sub()
                                  MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_FmtFin)
                                  读取索引ToolStripMenuItem_Click(sender, e)
                              End Sub)
                End Sub,
                Sub(Message As String)
                    'OnError
                    PrintMsg(Message)
                    SetStatusLight(LWStatus.Err)
                    LockGUI(False)
                    Me.Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_FmtFail}{vbCrLf}{Message}"))
                End Sub, param.Capacity, param.P0Size, param.P1Size, param.EncryptionKey)
        End If
    End Sub
    Public Function ImportSHA1(schhash As ltfsindex, Overwrite As Boolean) As String
        SetStatusLight(LWStatus.Busy)
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
                                Dim sha1value0 As String = f.GetXAttr(ltfsindex.file.xattr.HashType.SHA1)
                                Dim md5value0 As String = f.GetXAttr(ltfsindex.file.xattr.HashType.MD5)
                                If Not Overwrite Then
                                    If Not (sha1value0 IsNot Nothing AndAlso sha1value0 <> "" AndAlso sha1value0.Length = 40) Then
                                        PrintMsg($"{f.name}", False, $"{f.name}    {sha1value0} -> { flookup.GetXAttr(ltfsindex.file.xattr.HashType.SHA1)}")
                                        f.SetXattr(ltfsindex.file.xattr.HashType.SHA1, flookup.GetXAttr(ltfsindex.file.xattr.HashType.SHA1))
                                    End If
                                    If Not (md5value0 IsNot Nothing AndAlso md5value0 <> "" AndAlso md5value0.Length = 32) Then
                                        PrintMsg($"{f.name}", False, $"{f.name}    {md5value0} -> { flookup.GetXAttr(ltfsindex.file.xattr.HashType.MD5)}")
                                        f.SetXattr(ltfsindex.file.xattr.HashType.MD5, flookup.GetXAttr(ltfsindex.file.xattr.HashType.MD5))
                                    End If
                                Else
                                    If flookup.GetXAttr(ltfsindex.file.xattr.HashType.SHA1) IsNot Nothing AndAlso flookup.GetXAttr(ltfsindex.file.xattr.HashType.SHA1) <> "" And flookup.GetXAttr(ltfsindex.file.xattr.HashType.SHA1).Length = 40 Then
                                        PrintMsg($"{f.name}", False, $"{f.name}    {sha1value0} -> { flookup.GetXAttr(ltfsindex.file.xattr.HashType.SHA1)}")
                                        f.SetXattr(ltfsindex.file.xattr.HashType.SHA1, flookup.GetXAttr(ltfsindex.file.xattr.HashType.SHA1))
                                    End If
                                    If flookup.GetXAttr(ltfsindex.file.xattr.HashType.MD5) IsNot Nothing AndAlso flookup.GetXAttr(ltfsindex.file.xattr.HashType.MD5) <> "" And flookup.GetXAttr(ltfsindex.file.xattr.HashType.MD5).Length = 32 Then
                                        PrintMsg($"{f.name}", False, $"{f.name}    {md5value0} -> { flookup.GetXAttr(ltfsindex.file.xattr.HashType.MD5)}")
                                        f.SetXattr(ltfsindex.file.xattr.HashType.MD5, flookup.GetXAttr(ltfsindex.file.xattr.HashType.MD5))
                                    End If
                                End If

                                Exit For
                            End If
                        Next
                        f.openforwrite = False
                        Threading.Interlocked.Increment(fprocessed)
                        If f.sha1.Length = 40 Then
                            Threading.Interlocked.Increment(fhash)
                        ElseIf fprocessed - fhash <= 5 Then
                            MessageBox.Show(New Form With {.TopMost = True}, $"{f.fileuid}:{d.LTFSIndexDir.name}\{f.name} {f.sha1}")
                        End If
                    Catch ex As Exception
                        SetStatusLight(LWStatus.Err)
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
            SetStatusLight(LWStatus.Busy)
            Try
                Dim schhash As ltfsindex
                PrintMsg(My.Resources.ResText_RI)
                schhash = ltfsindex.FromSchFile(OpenFileDialog1.FileName)
                Dim dr As DialogResult = MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_SHA1Overw, My.Resources.ResText_Hint, MessageBoxButtons.YesNoCancel)
                PrintMsg(My.Resources.ResText_Importing)
                Dim result As String = ""
                If dr = DialogResult.Yes Then
                    result = ImportSHA1(schhash, True)
                ElseIf dr = DialogResult.No Then
                    result = ImportSHA1(schhash, False)
                Else
                    PrintMsg(My.Resources.ResText_OpCancelled)
                    Exit Try
                End If
                RefreshDisplay()
                PrintMsg($"{My.Resources.ResText_Imported} {result}")
            Catch ex As Exception
                PrintMsg(ex.ToString)
                SetStatusLight(LWStatus.Err)
            End Try
            SetStatusLight(LWStatus.Succ)
        End If
    End Sub
    Private Sub 设置高度ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 设置高度ToolStripMenuItem.Click
        Dim p As TapeUtils.PositionData = GetPos
        PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
        Dim Pos As Long = p.BlockNumber
        If MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_SetH1}{Pos}{My.Resources.ResText_SetH2}{vbCrLf}{My.Resources.ResText_SetH3}", My.Resources.ResText_Confirm, MessageBoxButtons.OKCancel) = DialogResult.OK Then
            CurrentHeight = Pos
            SetStatusLight(LWStatus.Idle)
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
                            TapeUtils.Locate(handle:=driveHandle, BlockAddress:=ext.startblock, Partition:=GetPartitionNumber(ext.partition), DestType:=TapeUtils.LocateDestType.Block)
                            PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                            LockGUI(False)
                            Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_Located}{ext.startblock}"))
                            PrintMsg($"{My.Resources.ResText_Located}{ext.startblock}")
                            SetStatusLight(LWStatus.Idle)
                        End Sub)
                LockGUI()
                SetStatusLight(LWStatus.Busy)
                PrintMsg(My.Resources.ResText_Locating)
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
    Public Function CheckFlush() As Boolean
        If Threading.Interlocked.Exchange(Flush, False) Then
            PrintMsg("Flush Triggered", LogOnly:=True)
            Dim Loc As TapeUtils.PositionData = GetPos
            If Loc.EOP Then PrintMsg(My.Resources.ResText_EWEOM, True)
            PrintMsg($"Position = {Loc.ToString()}", LogOnly:=True)
            If Not ForceFlush AndAlso ReadChanLRInfo(10000) < My.Settings.LTFSWriter_AutoCleanErrRateLogThreashould Then
                PrintMsg("Error rate log OK, ignore", LogOnly:=True)
                Return False
            Else
                ForceFlush = False
                Threading.Interlocked.Increment(CapReduceCount)
                TapeUtils.Flush(driveHandle)
                RefreshCapacity()
                Return True
            End If
        Else
            Return False
        End If
    End Function
    Public Sub CheckClean(Optional ByVal LockVolume As Boolean = False)
        If Threading.Interlocked.Exchange(Clean, False) Then
            If (Now - Clean_last).TotalSeconds < 300 Then Exit Sub
            PrintMsg("Clean Triggered", LogOnly:=True)
            Clean_last = Now
            Dim Loc As TapeUtils.PositionData = GetPos
            If Loc.EOP Then PrintMsg(My.Resources.ResText_EWEOM, True)
            PrintMsg($"Position = {Loc.ToString()}", LogOnly:=True)
            If Not Loc.EOP Then
                TapeUtils.DoReload(driveHandle, LockVolume, EncryptionKey)
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
        If MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_CancelConfirm, My.Resources.ResText_Warning, MessageBoxButtons.OKCancel) = DialogResult.OK Then
            StopFlag = True
        End If
        Pause = False
    End Sub
    Private Sub ToolStripDropDownButton2_Click(sender As Object, e As EventArgs) Handles ToolStripDropDownButton2.Click
        ForceFlush = True
        Flush = True
    End Sub
    Private Sub ToolStripDropDownButton3_Click(sender As Object, e As EventArgs) Handles ToolStripDropDownButton3.Click
        ToolStripDropDownButton3.Enabled = False
        Task.Run(Sub()
                     TapeUtils.DoReload(driveHandle, Not AllowOperation, EncryptionKey)
                     Invoke(Sub() ToolStripDropDownButton3.Enabled = True)
                 End Sub)

    End Sub
    Private Sub ToolStripStatusLabel4_Click(sender As Object, e As EventArgs) Handles ToolStripStatusLabel4.Click
        If MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_ClearWC, My.Resources.ResText_Confirm, MessageBoxButtons.OKCancel) = DialogResult.OK Then
            TotalBytesProcessed = 0
            TotalFilesProcessed = 0
            t_last = 0
            d_last = 0
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
    Public Function CalculateChecksum(FileIndex As ltfsindex.file, Optional ByVal blk0 As Byte() = Nothing) As Dictionary(Of String, String)
        Dim HT As New IOManager.CheckSumBlockwiseCalculator
        If FileIndex.length > 0 Then
            Dim CreateNew As Boolean = True
            If FileIndex.extentinfo.Count > 1 Then FileIndex.extentinfo.Sort(New Comparison(Of ltfsindex.file.extent)(Function(a As ltfsindex.file.extent, b As ltfsindex.file.extent) As Integer
                                                                                                                          Return a.fileoffset.CompareTo(b.fileoffset)
                                                                                                                      End Function))
            For Each fe As ltfsindex.file.extent In FileIndex.extentinfo
                If blk0 IsNot Nothing Then
                    RestorePosition = New TapeUtils.PositionData(driveHandle)
                    RestorePosition.BlockNumber -= 1
                Else
                    If RestorePosition.BlockNumber <> fe.startblock OrElse RestorePosition.PartitionNumber <> Math.Min(ExtraPartitionCount, fe.partition) Then
                        TapeUtils.Locate(handle:=driveHandle, BlockAddress:=fe.startblock, Partition:=GetPartitionNumber(fe.partition))
                        RestorePosition = New TapeUtils.PositionData(driveHandle)
                    End If
                End If
                Dim TotalBytesToRead As Long = fe.bytecount
                Dim blk As Byte()
                Dim sense As Byte() = {}
                If blk0 IsNot Nothing Then
                    blk = blk0
                    blk0 = Nothing
                Else
                    Dim succ As Boolean = False
                    While Not succ
                        blk = TapeUtils.ReadBlock(handle:=driveHandle, sense:=sense, BlockSizeLimit:=Math.Min(plabel.blocksize, TotalBytesToRead))
                        If ((sense(2) >> 6) And &H1) = 1 Then
                            succ = True
                            Exit While
                        ElseIf sense(2) And &HF <> 0 Then
                            PrintMsg($"sense err {TapeUtils.Byte2Hex(sense, True)}", Warning:=True, LogOnly:=True)
                            Try
                                Throw New Exception("SCSI sense error")
                            Catch ex As Exception
                                Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_RErrSCSI}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                    Case DialogResult.Abort
                                        StopFlag = True
                                        Throw New Exception(TapeUtils.ParseSenseData(sense))
                                    Case DialogResult.Retry
                                        succ = False
                                    Case DialogResult.Ignore
                                        succ = True
                                        Exit While
                                End Select
                            End Try
                        Else
                            succ = True
                        End If
                    End While
                End If
                SyncLock RestorePosition
                    RestorePosition.BlockNumber += 1
                End SyncLock
                If fe.byteoffset > 0 Then blk = blk.Skip(fe.byteoffset).ToArray()
                TotalBytesToRead -= blk.Length
                HT.Propagate(blk)
                Threading.Interlocked.Add(CurrentBytesProcessed, blk.Length)
                Threading.Interlocked.Add(TotalBytesProcessed, blk.Length)
                While TotalBytesToRead > 0
                    Dim succ As Boolean = False
                    While Not succ
                        blk = TapeUtils.ReadBlock(handle:=driveHandle, sense:=sense, BlockSizeLimit:=Math.Min(plabel.blocksize, TotalBytesToRead))
                        If ((sense(2) >> 6) And &H1) = 1 Then
                            succ = True
                            Exit While
                        ElseIf sense(2) And &HF <> 0 Then
                            PrintMsg($"sense err {TapeUtils.Byte2Hex(sense, True)}", Warning:=True, LogOnly:=True)
                            Try
                                Throw New Exception("SCSI sense error")
                            Catch ex As Exception
                                Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_RErrSCSI}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                    Case DialogResult.Abort
                                        StopFlag = True
                                        Throw New Exception(TapeUtils.ParseSenseData(sense))
                                    Case DialogResult.Retry
                                        succ = False
                                    Case DialogResult.Ignore
                                        succ = True
                                        Exit While
                                End Select
                            End Try
                        Else
                            succ = True
                        End If
                    End While
                    SyncLock RestorePosition
                        RestorePosition.BlockNumber += 1
                    End SyncLock
                    Dim blklen As Integer = blk.Length
                    If blklen = 0 Then Exit While
                    If blklen > TotalBytesToRead Then blklen = TotalBytesToRead
                    TotalBytesToRead -= blk.Length
                    HT.Propagate(blk, blklen)
                    Threading.Interlocked.Add(CurrentBytesProcessed, blk.Length)
                    Threading.Interlocked.Add(TotalBytesProcessed, blk.Length)
                    If StopFlag Then Return Nothing
                    While Pause
                        Threading.Thread.Sleep(10)
                    End While
                End While
            Next
        End If
        HT.ProcessFinalBlock()
        Threading.Interlocked.Increment(CurrentFilesProcessed)
        Threading.Interlocked.Increment(TotalFilesProcessed)
        Dim result As New Dictionary(Of String, String)
        result.Add("SHA1", HT.SHA1Value)
        result.Add("MD5", HT.MD5Value)
        Return result
    End Function

    Private Sub 生成标签ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 生成标签ToolStripMenuItem.Click
        If My.Settings.LTFSWriter_FileLabel = "" Then
            设置标签ToolStripMenuItem_Click(sender, e)
            If My.Settings.LTFSWriter_FileLabel = "" Then Exit Sub
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
                        Dim emptyfile As String = IO.Path.Combine(Application.StartupPath, "empty.file")
                        IO.File.WriteAllBytes(emptyfile, {})
                        Dim fnew As New FileRecord(emptyfile, d)
                        With fnew.File
                            .name = $"{dir.name}{fl}"
                            .backuptime = Now.ToUniversalTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff00Z")
                            .creationtime = .backuptime
                            .modifytime = .backuptime
                            .accesstime = .backuptime
                            .changetime = .modifytime
                        End With
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
            PrintMsg(My.Resources.ResText_OpSucc)
            SetStatusLight(LWStatus.Idle)
            RefreshDisplay()
        End If
    End Sub

    Private Sub 设置标签ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 设置标签ToolStripMenuItem.Click
        My.Settings.LTFSWriter_FileLabel = InputBox(My.Resources.ResText_DLS, My.Resources.ResText_DLT, My.Settings.LTFSWriter_FileLabel)
        PrintMsg($"{My.Resources.ResText_DLFin} .{My.Settings.LTFSWriter_FileLabel}")
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
        If MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_UIE, My.Resources.ResText_Confirm, MessageBoxButtons.OKCancel) = DialogResult.Cancel Then Exit Sub
        Dim th As New Threading.Thread(
                Sub()
                    Try
                        SetStatusLight(LWStatus.Busy)
                        If (My.Settings.LTFSWriter_ForceIndex OrElse TotalBytesUnindexed <> 0) AndAlso schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.b Then
                            PrintMsg(My.Resources.ResText_UDI)
                            WriteCurrentIndex(False)
                            TapeUtils.Flush(driveHandle)
                        End If
                        AutoDump()
                        PrintMsg(My.Resources.ResText_UI)
                        RefreshIndexPartition()
                        TapeUtils.ReleaseUnit(driveHandle)
                        TapeUtils.AllowMediaRemoval(driveHandle)
                        PrintMsg(My.Resources.ResText_IUd)
                        If schema IsNot Nothing AndAlso schema.location.partition = ltfsindex.PartitionLabel.a Then Invoke(Sub() 更新数据区索引ToolStripMenuItem.Enabled = False)
                        SetStatusLight(LWStatus.Busy)
                        TapeUtils.LoadEject(driveHandle, TapeUtils.LoadOption.Eject)
                        PrintMsg(My.Resources.ResText_Ejd)
                        Invoke(Sub()
                                   SetStatusLight(LWStatus.Succ)
                                   LockGUI(False)
                                   RefreshDisplay()
                                   RaiseEvent TapeEjected()
                               End Sub)
                    Catch ex As Exception
                        PrintMsg(My.Resources.ResText_IUErr, TooltipText:=$"{My.Resources.ResText_IUErr}{vbCrLf}{ex.ToString()}")
                        SetStatusLight(LWStatus.Err)
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
            SetStatusLight(LWStatus.Busy)
            Dim hw As New HashTaskWindow With {.schema = schema, .BaseDirectory = FolderBrowserDialog1.SelectedPath, .TargetDirectory = "", .DisableSkipInfo = True}
            Dim p As String = ""
            If OpenFileDialog1.FileName <> "" Then p = New IO.FileInfo(OpenFileDialog1.FileName).DirectoryName
            hw.schPath = Barcode & ".schema"
            If IO.Directory.Exists(p) Then
                hw.schPath = IO.Path.Combine(p, hw.schPath)
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
            SetStatusLight(LWStatus.Idle)
            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
            RefreshDisplay()
        End If
    End Sub

    Private Sub ToolStripButton6_Click(sender As Object, e As EventArgs) Handles ToolStripButton6.Click
        校验源文件ToolStripMenuItem_Click(sender, e)
    End Sub

    Private Sub 限速不限制ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 限速不限制ToolStripMenuItem.Click
        Dim sin As String = InputBox(My.Resources.ResText_WLimS, My.Resources.ResText_Setting, SpeedLimit)
        If sin = "" Then Exit Sub
        SpeedLimit = Val(sin)
    End Sub

    Private Sub 重装带前清洁次数3ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 重装带前清洁次数3ToolStripMenuItem.Click
        CleanCycle = Val(InputBox(My.Resources.ResText_CLNCS, My.Resources.ResText_Setting, CleanCycle))
    End Sub
    Public Sub HashSelectedFiles(Overwrite As Boolean, ValidOnly As Boolean)
        Dim fc As Long = 0, ec As Long = 0
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
            StartTime = Now
            Dim th As New Threading.Thread(
                    Sub()
                        Try
                            PrintMsg(My.Resources.ResText_Hashing)
                            StopFlag = False
                            CurrentBytesProcessed = 0
                            CurrentFilesProcessed = 0
                            UnwrittenSizeOverrideValue = 0
                            UnwrittenCountOverwriteValue = flist.Count
                            For Each FI As ltfsindex.file In flist
                                UnwrittenSizeOverrideValue += FI.length
                            Next
                            RestorePosition = New TapeUtils.PositionData(driveHandle)
                            For Each FileIndex As ltfsindex.file In flist
                                If ValidOnly Then
                                    If (FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) = "" OrElse (Not (FileIndex.SHA1ForeColor.Equals(Color.Black) OrElse FileIndex.SHA1ForeColor.Equals(Color.Red)))) AndAlso
                                       (FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) = "" OrElse (Not (FileIndex.MD5ForeColor.Equals(Color.Black) OrElse FileIndex.MD5ForeColor.Equals(Color.Red)))) Then
                                        'Skip
                                        Threading.Interlocked.Add(CurrentBytesProcessed, FileIndex.length)
                                        Threading.Interlocked.Increment(CurrentFilesProcessed)
                                    Else
                                        Dim blk0 As Byte() = Nothing
                                        If FileIndex.length > 0 AndAlso FileIndex.symlink Is Nothing AndAlso (FileIndex.extentinfo.Count = 0 OrElse FileIndex.extentinfo(0).startblock = 0) Then
                                            PrintMsg("Extent missing. Try to reconstruct.", LogOnly:=True)
                                            blk0 = TapeUtils.ReadBlock(handle:=driveHandle, BlockSizeLimit:=Math.Min(plabel.blocksize, FileIndex.length))
                                            Dim p As New TapeUtils.PositionData(handle:=driveHandle)
                                            If blk0.Count = 0 Then
                                                PrintMsg("Filemark Found. Skip index.", LogOnly:=True)
                                                TapeUtils.ReadToFileMark(driveHandle)
                                                blk0 = TapeUtils.ReadBlock(handle:=driveHandle, BlockSizeLimit:=Math.Min(plabel.blocksize, FileIndex.length))
                                                p = New TapeUtils.PositionData(handle:=driveHandle)
                                            End If
                                            FileIndex.extentinfo.Clear()
                                            FileIndex.extentinfo.Add(New ltfsindex.file.extent With {.bytecount = FileIndex.length, .startblock = p.BlockNumber - 1, .partition = ltfsindex.PartitionLabel.b})
                                        End If
                                        Dim result As Dictionary(Of String, String) = CalculateChecksum(FileIndex, blk0)
                                        If result IsNot Nothing Then
                                            If FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) = result.Item("SHA1") Then
                                                FileIndex.SHA1ForeColor = Color.DarkGreen
                                            ElseIf FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) <> "" Then
                                                FileIndex.SHA1ForeColor = Color.Red
                                                Threading.Interlocked.Increment(ec)
                                                PrintMsg($"SHA1 Mismatch at fileuid={FileIndex.fileuid} filename={FileIndex.name} sha1logged={FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True)} sha1calc={result.Item("SHA1")}", ForceLog:=True)
                                            End If
                                            If FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) = result.Item("MD5") Then
                                                FileIndex.MD5ForeColor = Color.DarkGreen
                                            ElseIf FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) <> "" Then
                                                FileIndex.MD5ForeColor = Color.Red
                                                Threading.Interlocked.Increment(ec)
                                                PrintMsg($"MD5 Mismatch at fileuid={FileIndex.fileuid} filename={FileIndex.name} md5logged={FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True)} md5calc={result.Item("MD5")}", ForceLog:=True)
                                            End If
                                        End If
                                    End If
                                ElseIf Overwrite Then
                                    Dim result As Dictionary(Of String, String) = CalculateChecksum(FileIndex)
                                    If result IsNot Nothing Then
                                        If FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) <> result.Item("SHA1") Then
                                            FileIndex.SetXattr(ltfsindex.file.xattr.HashType.SHA1, result.Item("SHA1"))
                                            FileIndex.SHA1ForeColor = Color.Blue
                                        Else
                                            FileIndex.SHA1ForeColor = Color.Green
                                        End If
                                        If FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) <> result.Item("MD5") Then
                                            FileIndex.SetXattr(ltfsindex.file.xattr.HashType.MD5, result.Item("MD5"))
                                            FileIndex.MD5ForeColor = Color.Blue
                                        Else
                                            FileIndex.MD5ForeColor = Color.Green
                                        End If
                                        If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                                    End If
                                Else
                                    If FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) = "" OrElse FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) = "" Then
                                        Dim result As Dictionary(Of String, String) = CalculateChecksum(FileIndex)
                                        If result IsNot Nothing Then
                                            If FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) <> result.Item("SHA1") Then
                                                FileIndex.SetXattr(ltfsindex.file.xattr.HashType.SHA1, result.Item("SHA1"))
                                                FileIndex.SHA1ForeColor = Color.Blue
                                            Else
                                                FileIndex.SHA1ForeColor = Color.Green
                                            End If
                                            If FileIndex.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) <> result.Item("MD5") Then
                                                FileIndex.SetXattr(ltfsindex.file.xattr.HashType.MD5, result.Item("MD5"))
                                                FileIndex.MD5ForeColor = Color.Blue
                                            Else
                                                FileIndex.MD5ForeColor = Color.Green
                                            End If
                                            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                                        End If
                                    Else
                                        Threading.Interlocked.Add(CurrentBytesProcessed, FileIndex.length)
                                        Threading.Interlocked.Increment(CurrentFilesProcessed)
                                    End If
                                End If
                                Threading.Interlocked.Increment(fc)
                                If StopFlag Then Exit For
                            Next
                        Catch ex As Exception
                            PrintMsg(My.Resources.ResText_HErr)
                            SetStatusLight(LWStatus.Err)
                        End Try
                        UnwrittenSizeOverrideValue = 0
                        UnwrittenCountOverwriteValue = 0
                        StopFlag = False
                        LockGUI(False)
                        RefreshDisplay()
                        PrintMsg($"{My.Resources.ResText_HFin} {fc - ec}/{fc} | {ec} {My.Resources.ResText_Error}")
                        SetStatusLight(LWStatus.Idle)
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
                StartTime = Now
                SetStatusLight(LWStatus.Busy)
                Dim fc As Long = 0, ec As Long = 0
                PrintMsg(My.Resources.ResText_Hashing)
                Try
                    StopFlag = False
                    Dim FileList As New List(Of FileRecord)
                    Dim IterDir As Action(Of ltfsindex.directory, String) =
                        Sub(tapeDir As ltfsindex.directory, outputDir As String)
                            For Each f As ltfsindex.file In tapeDir.contents._file
                                FileList.Add(New FileRecord With {.File = f, .SourcePath = outputDir & "\" & f.name})
                                'RestoreFile(IO.Path.Combine(outputDir.FullName, f.name), f)
                            Next
                            For Each d As ltfsindex.directory In tapeDir.contents._directory
                                Dim dirOutput As String = outputDir & "\" & d.name
                                IterDir(d, dirOutput)
                            Next
                        End Sub
                    PrintMsg(My.Resources.ResText_PrepFile)
                    Dim ODir As String = selectedDir.name
                    'If Not IO.Directory.Exists(ODir) Then IO.Directory.CreateDirectory(ODir)
                    IterDir(selectedDir, ODir)
                    FileList.Sort(New Comparison(Of FileRecord)(Function(a As FileRecord, b As FileRecord) As Integer
                                                                    If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count <> 0 Then Return a.File.fileuid.CompareTo(b.File.fileuid)
                                                                    If b.File.extentinfo.Count = 0 And a.File.extentinfo.Count <> 0 Then Return a.File.fileuid.CompareTo(b.File.fileuid)
                                                                    If a.File.extentinfo.Count = 0 And b.File.extentinfo.Count = 0 Then Return a.File.fileuid.CompareTo(b.File.fileuid)
                                                                    If a.File.extentinfo(0).startblock = 0 OrElse b.File.extentinfo(0).startblock = 0 Then
                                                                        Return a.File.fileuid.CompareTo(b.File.fileuid)
                                                                    End If
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
                    PrintMsg(My.Resources.ResText_Hashing)
                    Dim c As Integer = 0
                    RestorePosition = New TapeUtils.PositionData(driveHandle)
                    For Each fr As FileRecord In FileList
                        c += 1
                        PrintMsg($"{My.Resources.ResText_Hashing} [{c}/{FileList.Count}] {fr.File.name} {My.Resources.ResText_Size}:{IOManager.FormatSize(fr.File.length)}", False, $"{My.Resources.ResText_Hashing} [{c}/{FileList.Count}] {fr.SourcePath} {My.Resources.ResText_Size}:{fr.File.length}")
                        If ValidateOnly Then
                            If (fr.File.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) = "" OrElse (Not (fr.File.SHA1ForeColor.Equals(Color.Black) OrElse fr.File.SHA1ForeColor.Equals(Color.Red)))) AndAlso
                               (fr.File.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) = "" OrElse (Not (fr.File.MD5ForeColor.Equals(Color.Black) OrElse fr.File.MD5ForeColor.Equals(Color.Red)))) Then
                                'skip
                                Threading.Interlocked.Add(CurrentBytesProcessed, fr.File.length)
                                Threading.Interlocked.Increment(CurrentFilesProcessed)
                            Else
                                Dim blk0 As Byte() = Nothing
                                If fr.File.length > 0 AndAlso fr.File.symlink Is Nothing AndAlso (fr.File.extentinfo.Count = 0 OrElse fr.File.extentinfo(0).startblock = 0) Then
                                    PrintMsg("Extent missing. Try to reconstruct.", LogOnly:=True)
                                    blk0 = TapeUtils.ReadBlock(handle:=driveHandle, BlockSizeLimit:=Math.Min(plabel.blocksize, fr.File.length))
                                    Dim p As New TapeUtils.PositionData(handle:=driveHandle)
                                    If blk0.Count = 0 Then
                                        PrintMsg("Filemark Found. Skip index.", LogOnly:=True)
                                        TapeUtils.ReadToFileMark(driveHandle)
                                        blk0 = TapeUtils.ReadBlock(handle:=driveHandle, BlockSizeLimit:=Math.Min(plabel.blocksize, fr.File.length))
                                        p = New TapeUtils.PositionData(handle:=driveHandle)
                                    End If
                                    fr.File.extentinfo.Clear()
                                    fr.File.extentinfo.Add(New ltfsindex.file.extent With {.bytecount = fr.File.length, .startblock = p.BlockNumber - 1, .partition = ltfsindex.PartitionLabel.b})
                                End If
                                Dim result As Dictionary(Of String, String) = CalculateChecksum(fr.File, blk0)
                                If result IsNot Nothing Then
                                    If fr.File.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) = result.Item("SHA1") Then
                                        fr.File.SHA1ForeColor = Color.Green
                                    ElseIf fr.File.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) <> "" Then
                                        fr.File.SHA1ForeColor = Color.Red
                                        PrintMsg($"SHA1 Mismatch at fileuid={fr.File.fileuid} filename={fr.File.name} sha1logged={fr.File.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True)} sha1calc={result.Item("SHA1")}", ForceLog:=True)
                                        Threading.Interlocked.Increment(ec)
                                    End If
                                    If fr.File.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) = result.Item("MD5") Then
                                        fr.File.MD5ForeColor = Color.Green
                                    ElseIf fr.File.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) <> "" Then
                                        fr.File.MD5ForeColor = Color.Red
                                        PrintMsg($"MD5 Mismatch at fileuid={fr.File.fileuid} filename={fr.File.name} md5logged={fr.File.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True)} md5calc={result.Item("MD5")}", ForceLog:=True)
                                        Threading.Interlocked.Increment(ec)
                                    End If
                                End If
                            End If
                        ElseIf Overwrite Then
                            Dim result As Dictionary(Of String, String) = CalculateChecksum(fr.File)
                            If fr.File.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) <> result.Item("SHA1") Then
                                fr.File.SetXattr(ltfsindex.file.xattr.HashType.SHA1, result.Item("SHA1"))
                                fr.File.SHA1ForeColor = Color.Blue
                            Else
                                fr.File.SHA1ForeColor = Color.Green
                            End If
                            If fr.File.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) <> result.Item("MD5") Then
                                fr.File.SetXattr(ltfsindex.file.xattr.HashType.MD5, result.Item("MD5"))
                                fr.File.MD5ForeColor = Color.Blue
                            Else
                                fr.File.MD5ForeColor = Color.Green
                            End If
                            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                        Else
                            If fr.File.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) = "" OrElse fr.File.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) = "" Then
                                Dim result As Dictionary(Of String, String) = CalculateChecksum(fr.File)
                                If fr.File.GetXAttr(ltfsindex.file.xattr.HashType.SHA1, True) <> result.Item("SHA1") Then
                                    fr.File.SetXattr(ltfsindex.file.xattr.HashType.SHA1, result.Item("SHA1"))
                                    fr.File.SHA1ForeColor = Color.Blue
                                Else
                                    fr.File.SHA1ForeColor = Color.Green
                                End If
                                If fr.File.GetXAttr(ltfsindex.file.xattr.HashType.MD5, True) <> result.Item("MD5") Then
                                    fr.File.SetXattr(ltfsindex.file.xattr.HashType.MD5, result.Item("MD5"))
                                    fr.File.MD5ForeColor = Color.Blue
                                Else
                                    fr.File.SHA1ForeColor = Color.Green
                                End If
                                If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                            Else
                                Threading.Interlocked.Add(CurrentBytesProcessed, fr.File.length)
                                Threading.Interlocked.Increment(CurrentFilesProcessed)
                            End If
                        End If
                        Threading.Interlocked.Increment(fc)
                        If StopFlag Then
                            PrintMsg(My.Resources.ResText_OpCancelled)
                            Exit Try
                        End If
                    Next
                    PrintMsg($"{My.Resources.ResText_HFin} {fc - ec}/{fc} | {ec} {My.Resources.ResText_Error}")
                Catch ex As Exception
                    SetStatusLight(LWStatus.Err)
                    Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString}"))
                    PrintMsg(My.Resources.ResText_HErr)
                End Try
                UnwrittenSizeOverrideValue = 0
                UnwrittenCountOverwriteValue = 0
                SetStatusLight(LWStatus.Idle)
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
            If TypeOf TreeView1.SelectedNode.Tag IsNot ltfsindex.directory Then Exit Sub
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
            MessageBox.Show(New Form With {.TopMost = True}, $"{d.name}{vbCrLf}{My.Resources.ResText_FCountP}{fnum}{vbCrLf}{My.Resources.ResText_FSizeP}{fbytes} {My.Resources.ResText_Byte} ({IOManager.FormatSize(fbytes)})")
        End If
    End Sub

    Private Sub 预读文件数5ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 预读文件数5ToolStripMenuItem.Click
        Dim s As String = InputBox(My.Resources.ResText_SPreR, My.Resources.ResText_Setting, My.Settings.LTFSWriter_PreLoadFileCount)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_PreLoadFileCount = Val(s)
        预读文件数5ToolStripMenuItem.Text = $"{My.Resources.ResText_PFC}{My.Settings.LTFSWriter_PreLoadFileCount}"
    End Sub

    Private Sub 文件缓存32MiBToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 文件缓存32MiBToolStripMenuItem.Click
        Dim s As String = InputBox("设置文件缓存", My.Resources.ResText_Setting, My.Settings.LTFSWriter_PreLoadBytes)
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
            If ListView1.SelectedItems.Count > 1 Then
                SyncLock ListView1.SelectedItems
                    For Each ItemSelected As ListViewItem In ListView1.SelectedItems
                        If ItemSelected.Tag IsNot Nothing AndAlso TypeOf (ItemSelected.Tag) Is ltfsindex.file Then
                            Dim f As ltfsindex.file = ItemSelected.Tag
                            result.AppendLine(f.GetSerializedText())
                        End If
                    Next
                End SyncLock
                MessageBox.Show(New Form With {.TopMost = True}, result.ToString)
            Else
                Dim PG1 As New SettingPanel
                PG1.PropertyGrid1.SelectedObject = CType(ListView1.SelectedItems(0).Tag, ltfsindex.file)
                PG1.Text = $"{TextBoxSelectedPath.Text}\{ CType(ListView1.SelectedItems(0).Tag, ltfsindex.file).name}"
                If PG1.ShowDialog() = DialogResult.OK Then
                    If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                End If
            End If

        End If

    End Sub

    Private Sub 禁用分区ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 禁用分区ToolStripMenuItem.Click
        DisablePartition = 禁用分区ToolStripMenuItem.Checked
    End Sub

    Private Sub 速度下限ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 速度下限ToolStripMenuItem.Click
        Dim s As String = InputBox(My.Resources.ResText_SSMin, My.Resources.ResText_Setting, My.Settings.LTFSWriter_AutoCleanDownLim)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_AutoCleanDownLim = Val(s)
        My.Settings.Save()
        速度下限ToolStripMenuItem.Text = $"{My.Resources.ResText_SMin}{My.Settings.LTFSWriter_AutoCleanDownLim} MiB/s"
    End Sub

    Private Sub 速度上限ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 速度上限ToolStripMenuItem.Click
        Dim s As String = InputBox(My.Resources.ResText_SSMax, My.Resources.ResText_Setting, My.Settings.LTFSWriter_AutoCleanUpperLim)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_AutoCleanUpperLim = Val(s)
        My.Settings.Save()
        速度上限ToolStripMenuItem.Text = $"{My.Resources.ResText_SMax}{My.Settings.LTFSWriter_AutoCleanUpperLim} MiB/s"
    End Sub

    Private Sub 持续时间ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 持续时间ToolStripMenuItem.Click
        Dim s As String = InputBox(My.Resources.ResText_SSTime, My.Resources.ResText_Setting, My.Settings.LTFSWriter_AutoCleanTimeThreashould)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_AutoCleanTimeThreashould = Val(s)
        My.Settings.Save()
        持续时间ToolStripMenuItem.Text = $"{My.Resources.ResText_STime}{My.Settings.LTFSWriter_AutoCleanTimeThreashould}s"
    End Sub

    Private Sub 去重SHA1ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 去重SHA1ToolStripMenuItem.Click
        My.Settings.LTFSWriter_DeDupe = Not My.Settings.LTFSWriter_DeDupe
        去重SHA1ToolStripMenuItem.Checked = My.Settings.LTFSWriter_DeDupe
        My.Settings.Save()
    End Sub
    <TypeConverter(GetType(ExpandableObjectConverter))>
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
        <TypeConverter(GetType(ExpandableObjectConverter))>
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
                    FileInfo.CreationTime = TapeUtils.ParseTimeStamp(LTFSFile.creationtime).ToFileTimeUtc
                    FileInfo.LastAccessTime = TapeUtils.ParseTimeStamp(LTFSFile.accesstime).ToFileTimeUtc
                    FileInfo.LastWriteTime = TapeUtils.ParseTimeStamp(LTFSFile.changetime).ToFileTimeUtc
                    FileInfo.ChangeTime = TapeUtils.ParseTimeStamp(LTFSFile.changetime).ToFileTimeUtc
                    FileInfo.IndexNumber = 0
                    FileInfo.HardLinks = 0
                Else
                    FileInfo.FileAttributes = dwFilAttributesValue.FILE_ATTRIBUTE_OFFLINE Or dwFilAttributesValue.FILE_ATTRIBUTE_DIRECTORY
                    FileInfo.ReparseTag = 0
                    FileInfo.FileSize = 0
                    FileInfo.AllocationSize = 0
                    FileInfo.CreationTime = TapeUtils.ParseTimeStamp(LTFSDirectory.creationtime).ToFileTimeUtc
                    FileInfo.LastAccessTime = TapeUtils.ParseTimeStamp(LTFSDirectory.accesstime).ToFileTimeUtc
                    FileInfo.LastWriteTime = TapeUtils.ParseTimeStamp(LTFSDirectory.changetime).ToFileTimeUtc
                    FileInfo.ChangeTime = TapeUtils.ParseTimeStamp(LTFSDirectory.changetime).ToFileTimeUtc
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
                Host.FileInfoTimeout = 10 * 1000
                Host.FileSystemName = "LTFS"
                Host.SectorSize = 4096
                Host.SectorsPerAllocationUnit = LW.plabel.blocksize \ Host.SectorSize
                Host.VolumeCreationTime = TapeUtils.ParseTimeStamp(LW.plabel.formattime).ToFileTimeUtc()
                Host.VolumeSerialNumber = 0
                Host.CaseSensitiveSearch = False
                Host.CasePreservedNames = True
                Host.UnicodeOnDisk = True
                Host.PersistentAcls = False
                Host.ReparsePoints = False
                Host.ReparsePointsAccessCheck = False
                Host.NamedStreams = False
                Host.PostCleanupWhenModifiedOnly = True
                Host.FlushAndPurgeOnCleanup = True
                Host.PassQueryDirectoryPattern = True
                Host.MaxComponentLength = 4096

            Catch ex As Exception
                MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString}")
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
                MessageBox.Show(New Form With {.TopMost = True}, $"{ex.ToString}")
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
                        Exit For
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
                'FileNode = New Object()
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
                            dirFound = True
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
                    Dim status As Integer = CType(FileDesc, FileDesc).GetFileInfo(FileInfo)
                    FileInfo = FileInfo
                    Return status
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
                    Dim BufferOffset As Long = Offset
                    For ei As Integer = 0 To .LTFSFile.extentinfo.Count - 1
                        With .LTFSFile.extentinfo(ei)
                            If Offset >= .fileoffset + .bytecount Then Continue For
                            Dim CurrentFileOffset As Long = .fileoffset

                            TapeUtils.Locate(TapeDrive:=TapeDrive, BlockAddress:= .startblock, Partition:=LW.GetPartitionNumber(.partition))

                            Dim blkBuffer As Byte() = TapeUtils.ReadBlock(TapeDrive)
                            CurrentFileOffset += blkBuffer.Length - .byteoffset
                            While CurrentFileOffset <= Offset
                                blkBuffer = TapeUtils.ReadBlock(TapeDrive)
                                CurrentFileOffset += blkBuffer.Length
                            End While
                            Dim FirstBlockByteOffset As Integer = blkBuffer.Length - (CurrentFileOffset - Offset)
                            Marshal.Copy(blkBuffer, FirstBlockByteOffset, Buffer, Math.Min(Length, blkBuffer.Length - FirstBlockByteOffset))
                            BufferOffset += Math.Min(Length, blkBuffer.Length - FirstBlockByteOffset)
                            BytesTransferred += Math.Min(Length, blkBuffer.Length - FirstBlockByteOffset)
                            While BufferOffset < .bytecount AndAlso BufferOffset < Length
                                blkBuffer = TapeUtils.ReadBlock(TapeDrive)
                                Marshal.Copy(blkBuffer, 0, New IntPtr(Buffer.ToInt64 + BufferOffset), Math.Min(Length - BufferOffset, Math.Min(blkBuffer.Length, .bytecount - BufferOffset)))
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
                    If d.name.ToLower() Like Pattern.ToLower() Then
                        lst.Add(d.name, d)
                    End If
                Next
                For Each f As ltfsindex.file In FileDesc.LTFSDirectory.contents._file
                    If f.name.ToLower() Like Pattern.ToLower() Then
                        lst.Add(f.name, f)
                    End If
                Next
                ReDim FileDesc.FileSystemInfos(lst.Count - 1)
                lst.CopyTo(FileDesc.FileSystemInfos, 0)
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
                            FileInfo.CreationTime = TapeUtils.ParseTimeStamp(.creationtime).ToFileTimeUtc
                            FileInfo.LastAccessTime = TapeUtils.ParseTimeStamp(.accesstime).ToFileTimeUtc
                            FileInfo.LastWriteTime = TapeUtils.ParseTimeStamp(.changetime).ToFileTimeUtc
                            FileInfo.ChangeTime = TapeUtils.ParseTimeStamp(.changetime).ToFileTimeUtc
                            FileInfo.IndexNumber = 0
                            FileInfo.HardLinks = 0
                        End With
                    ElseIf TypeOf FileDesc.FileSystemInfos(index).Value Is ltfsindex.file Then
                        With CType(FileDesc.FileSystemInfos(index).Value, ltfsindex.file)
                            FileInfo.FileAttributes = FileDesc.dwFilAttributesValue.FILE_ATTRIBUTE_OFFLINE Or FileDesc.dwFilAttributesValue.FILE_ATTRIBUTE_ARCHIVE
                            If .readonly Then FileInfo.FileAttributes = FileInfo.FileAttributes Or FileDesc.dwFilAttributesValue.FILE_ATTRIBUTE_READONLY
                            FileInfo.ReparseTag = 0
                            FileInfo.FileSize = .length
                            FileInfo.CreationTime = TapeUtils.ParseTimeStamp(.creationtime).ToFileTimeUtc
                            FileInfo.LastAccessTime = TapeUtils.ParseTimeStamp(.accesstime).ToFileTimeUtc
                            FileInfo.LastWriteTime = TapeUtils.ParseTimeStamp(.changetime).ToFileTimeUtc
                            FileInfo.ChangeTime = TapeUtils.ParseTimeStamp(.changetime).ToFileTimeUtc
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

    <TypeConverter(GetType(ExpandableObjectConverter))>
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
            'MessageBox.Show(New Form With {.TopMost = True}, $"Code {Code} Name={Host.FileSystemName} MP={Host.MountPoint} Pf={Host.Prefix}")
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

        MessageBox.Show(New Form With {.TopMost = True}, $"Mounted as \\ltfs\{svc.MountPath}{vbCrLf}Press OK to unmount")

        '卸载
        svc.Stop()
        MessageBox.Show(New Form With {.TopMost = True}, $"Unmounted. Code={svc.ExitCode}")
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
        SetStatusLight(LWStatus.Busy)
        AddHandler svc.LogPrint, Sub(s As String)
                                     PrintMsg($"FTPSVC> {s}")
                                 End Sub
        svc.port = Integer.Parse(InputBox("Port", "FTP Service", "8021"))
        svc.schema = schema
        svc.TapeDrive = TapeDrive
        svc.BlockSize = plabel.blocksize
        svc.ExtraPartitionCount = ExtraPartitionCount
        svc.StartService()
        MessageBox.Show(New Form With {.TopMost = True}, $"Service running on port {svc.port}.")
        svc.StopService()
        MessageBox.Show(New Form With {.TopMost = True}, "Service stopped.")
        SetStatusLight(LWStatus.Idle)
    End Sub

    Private Sub 右下角显示容量损失ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 右下角显示容量损失ToolStripMenuItem.Click
        If Not My.Settings.LTFSWriter_ShowLoss Then
            If MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_CapLossPerfWarning, My.Resources.ResText_Warning, MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
                右下角显示容量损失ToolStripMenuItem.Checked = My.Settings.LTFSWriter_ShowLoss
                Exit Sub
            End If
        End If
        My.Settings.LTFSWriter_ShowLoss = Not My.Settings.LTFSWriter_ShowLoss
        右下角显示容量损失ToolStripMenuItem.Checked = My.Settings.LTFSWriter_ShowLoss
        My.Settings.Save()
    End Sub

    Private Sub 压缩索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 压缩索引ToolStripMenuItem.Click
        If TreeView1.SelectedNode IsNot Nothing AndAlso TypeOf TreeView1.SelectedNode.Tag Is ltfsindex.directory Then
            LockGUI(True)
            Dim d As ltfsindex.directory = TreeView1.SelectedNode.Tag
            Dim p As ltfsindex.directory = TreeView1.SelectedNode.Parent.Tag
            Task.Run(Sub()
                         Dim tmpf As String = $"{Application.StartupPath}\LDS_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
                         d.SaveFile(tmpf)
                         Dim ms As New IO.FileStream(tmpf, IO.FileMode.Open)
                         TapeUtils.ReserveUnit(driveHandle)
                         TapeUtils.PreventMediaRemoval(driveHandle)
                         If Not LocateToWritePosition() Then Exit Sub
                         Dim pos As New TapeUtils.PositionData(driveHandle)
                         Dim fadd As New ltfsindex.file With {.name = d.name,
                                 .accesstime = d.accesstime,
                                 .backuptime = d.backuptime,
                                 .changetime = d.changetime,
                                 .creationtime = d.creationtime,
                                 .modifytime = d.modifytime,
                                 .extendedattributes = {New ltfsindex.file.xattr With {.key = "ltfscopygui.archive", .value = "True"}}.ToList(),
                                 .fileuid = schema.highestfileuid,
                                 .length = ms.Length,
                                 .extentinfo = {New ltfsindex.file.extent With {
                                 .bytecount = ms.Length,
                                 .startblock = pos.BlockNumber,
                                 .byteoffset = 0,
                                 .fileoffset = 0,
                                 .partition = pos.PartitionNumber}
                                 }.ToList()}
                         p.contents._file.Add(fadd)

                         Dim LastWriteTask As Task = Nothing
                         Dim ExitWhileFlag As Boolean = False
                         Dim wBufferPtr As IntPtr = Marshal.AllocHGlobal(plabel.blocksize)
                         Dim sh As New IOManager.CheckSumBlockwiseCalculator
                         While Not StopFlag
                             Dim buffer(plabel.blocksize - 1) As Byte
                             Dim BytesReaded As Integer = ms.Read(buffer, 0, plabel.blocksize)
                             sh.Propagate(buffer, BytesReaded)
                             If ExitWhileFlag Then Exit While
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
                                         sense = TapeUtils.Write(handle:=driveHandle, Data:=wBufferPtr, Length:=BytesReaded, senseEnabled:=True)
                                         SyncLock pos
                                             pos.BlockNumber += 1
                                         End SyncLock
                                     Catch ex As Exception
                                         Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{ My.Resources.ResText_WErrSCSI}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                             Case DialogResult.Abort
                                                 SetStatusLight(LWStatus.Err)
                                                 Throw ex
                                             Case DialogResult.Retry
                                                 succ = False
                                             Case DialogResult.Ignore
                                                 succ = True
                                                 Exit While
                                         End Select
                                         pos = New TapeUtils.PositionData(driveHandle)
                                         Continue While
                                     End Try
                                     If (((sense(2) >> 6) And &H1) = 1) Then
                                         If ((sense(2) And &HF) = 13) Then
                                             PrintMsg(My.Resources.ResText_VOF)
                                             Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_VOF))
                                             StopFlag = True
                                             ms.Close()
                                             SetStatusLight(LWStatus.Err)
                                             Exit Sub
                                         Else
                                             PrintMsg(My.Resources.ResText_EWEOM, True)
                                             succ = True
                                             Exit While
                                         End If
                                     ElseIf sense(2) And &HF <> 0 Then
                                         Try
                                             Throw New Exception("SCSI sense error")
                                         Catch ex As Exception
                                             Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                 Case DialogResult.Abort
                                                     SetStatusLight(LWStatus.Err)
                                                     Throw New Exception(TapeUtils.ParseSenseData(sense))
                                                 Case DialogResult.Retry
                                                     succ = False
                                                 Case DialogResult.Ignore
                                                     succ = True
                                                     Exit While
                                             End Select
                                         End Try

                                         pos = New TapeUtils.PositionData(driveHandle)
                                     Else
                                         succ = True
                                         Exit While
                                     End If
                                 End While
                                 If Flush Then CheckFlush()
                                 If Clean Then CheckClean(True)
                                 TotalBytesProcessed += BytesReaded
                                 CurrentBytesProcessed += BytesReaded
                                 TotalBytesUnindexed += BytesReaded
                             Else
                                 ExitWhileFlag = True
                             End If
                         End While
                         sh.ProcessFinalBlock()
                         fadd.SetXattr(ltfsindex.file.xattr.HashType.SHA1, sh.SHA1Value)
                         fadd.SetXattr(ltfsindex.file.xattr.HashType.MD5, sh.MD5Value)
                         If LastWriteTask IsNot Nothing Then LastWriteTask.Wait()
                         schema.highestfileuid += 1
                         p.contents._directory.Remove(d)
                         ms.Close()
                         IO.File.Delete(tmpf)
                         TotalFilesProcessed += 1
                         CurrentFilesProcessed += 1
                         Marshal.FreeHGlobal(wBufferPtr)
                         If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                         pos = GetPos
                         If pos.EOP Then PrintMsg(My.Resources.ResText_EWEOM, True)
                         PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                         CurrentHeight = pos.BlockNumber
                         Invoke(Sub() 更新数据区索引ToolStripMenuItem.Enabled = True)
                         SetStatusLight(LWStatus.Succ)
                         TapeUtils.Flush(driveHandle)
                         TapeUtils.ReleaseUnit(driveHandle)
                         TapeUtils.AllowMediaRemoval(driveHandle)
                         Invoke(Sub() TreeView1.SelectedNode = TreeView1.SelectedNode.Parent)
                         RefreshDisplay()
                         RefreshCapacity()
                         LockGUI(False)
                     End Sub)
        End If
    End Sub

    Private Sub 解压索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 解压索引ToolStripMenuItem.Click
        If TreeView1.SelectedNode IsNot Nothing AndAlso TypeOf TreeView1.SelectedNode.Tag Is ltfsindex.file Then
            Dim f As ltfsindex.file = TreeView1.SelectedNode.Tag
            Dim d As ltfsindex.directory = TreeView1.SelectedNode.Parent.Tag
            If f.GetXAttr("ltfscopygui.archive").ToLower = "true" Then
                LockGUI(True)
                Task.Run(Sub()
                             SetStatusLight(LWStatus.Busy)
                             Try
                                 Dim tmpf As String = $"{Application.StartupPath}\LDS_{Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp"
                                 RestorePosition = New TapeUtils.PositionData(driveHandle)
                                 RestoreFile(tmpf, f)
                                 Dim dindex As ltfsindex.directory = ltfsindex.directory.FromFile(tmpf)
                                 d.contents._file.Remove(f)
                                 d.contents._directory.Add(dindex)
                                 IO.File.Delete(tmpf)
                                 SetStatusLight(LWStatus.Idle)
                             Catch ex As Exception
                                 SetStatusLight(LWStatus.Err)
                                 PrintMsg($"解压索引出错：{ex.ToString}", ForceLog:=True)
                             End Try

                             RefreshDisplay()
                             LockGUI(False)
                         End Sub)

            End If
        End If
    End Sub

    Private Sub 跳过符号链接ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 跳过符号链接ToolStripMenuItem.Click
        My.Settings.LTFSWriter_SkipSymlink = 跳过符号链接ToolStripMenuItem.Checked
    End Sub

    Private Sub 覆盖已有文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 覆盖已有文件ToolStripMenuItem.Click
        My.Settings.LTFSWriter_OverwriteExist = 覆盖已有文件ToolStripMenuItem.Checked
    End Sub

    Private Sub 显示文件数ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 显示文件数ToolStripMenuItem.Click
        My.Settings.LTFSWriter_ShowFileCount = 显示文件数ToolStripMenuItem.Checked
        RefreshDisplay()
    End Sub

    Private Sub 移动到索引区ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 移动到索引区ToolStripMenuItem.Click
        If ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 Then
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
                            SetStatusLight(LWStatus.Busy)
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            UnwrittenSizeOverrideValue = 0
                            UnwrittenCountOverwriteValue = flist.Count
                            For Each FI As ltfsindex.file In flist
                                UnwrittenSizeOverrideValue += FI.length
                            Next
                            PrintMsg(My.Resources.ResText_Writing)
                            StopFlag = False
                            TapeUtils.ReserveUnit(driveHandle)
                            TapeUtils.PreventMediaRemoval(driveHandle)
                            RestorePosition = New TapeUtils.PositionData(driveHandle)
                            For Each FileIndex As ltfsindex.file In flist
                                MoveToIndexPartition(FileIndex)
                                If StopFlag Then
                                    PrintMsg(My.Resources.ResText_OpCancelled)
                                    SetStatusLight(LWStatus.Idle)
                                    Exit Sub
                                End If
                            Next
                        Catch ex As Exception
                            SetStatusLight(LWStatus.Err)
                            PrintMsg(My.Resources.ResText_RestoreErr)
                        End Try
                        TapeUtils.AllowMediaRemoval(driveHandle)
                        TapeUtils.ReleaseUnit(driveHandle)
                        StopFlag = False
                        UnwrittenSizeOverrideValue = 0
                        UnwrittenCountOverwriteValue = 0
                        LockGUI(False)
                        PrintMsg(My.Resources.ResText_AddFin)
                        SetStatusLight(LWStatus.Succ)
                        Invoke(Sub()
                                   RefreshDisplay()
                                   MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_AddFin)
                               End Sub)
                    End Sub)
            th.Start()
        End If
    End Sub


    Private Sub 索引间隔36GiBToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 索引间隔36GiBToolStripMenuItem.Click
        Dim result As String = InputBox(My.Resources.ResText_SIIntv, My.Resources.ResText_Setting, IndexWriteInterval)
        If result = "" Then Exit Sub
        IndexWriteInterval = Val(result)
    End Sub

    Private Sub DebugToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DebugToolStripMenuItem.Click
        LTFSConfigurator.Show()
    End Sub

    Private Sub 设置密钥ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 设置密钥ToolStripMenuItem.Click
        Dim key As String = ""
        If EncryptionKey IsNot Nothing AndAlso EncryptionKey.Length = 32 Then
            key = BitConverter.ToString(EncryptionKey).Replace("-", "").ToUpper
        End If
        key = InputBox(设置密钥ToolStripMenuItem.Text, "LTFSWriter", key)
        Dim newkey As Byte() = LTFSConfigurator.HexStringToByteArray(key)
        If newkey.Length <> 32 Then
            EncryptionKey = Nothing
            If key = "test" Then
                Dim frm As New Form With {.Width = 640, .Height = 200}
                Dim lbl1 As New Label With {.Parent = frm, .Top = 11, .Left = 7, .Width = 51, .Text = "SrcPath"}
                Dim txtLocation As New TextBox With {.Top = 7, .Left = 65, .Width = 640 - 7 * 3 - 65,
                    .Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top,
                    .Text = "F:\DLTEMP\test", .Parent = frm}
                Dim lbl2 As New Label With {.Parent = frm, .Top = 7 + txtLocation.Top + txtLocation.Height, .Left = 7, .Width = 51, .Text = "Filter"}
                Dim txtFilter As New TextBox With {.Top = 7 + txtLocation.Top + txtLocation.Height, .Left = 65, .Width = 640 - 7 * 3 - 65,
                   .Anchor = AnchorStyles.Left Or AnchorStyles.Right, .Parent = frm,
                   .Text = ".!qb|.downloading|.downloading.cfg|.tmp"}
                Dim chkAutoDelete As New CheckBox With {.Parent = frm, .Top = txtFilter.Top + txtFilter.Height + 7,
                    .Left = 7, .Text = "AutoDelete", .Checked = True}
                Dim ButtonStart As New Button With {.Parent = frm, .Width = 73, .Height = 23,
                    .Top = frm.Height - 73 - 23, .Left = frm.Width / 2 - 73 / 2, .Anchor = AnchorStyles.Bottom,
                    .Text = "Start"}
                Dim isStarted As Boolean = False
                Dim frmLock As New Object
                AddHandler frm.FormClosing, Sub()
                                                isStarted = False
                                            End Sub

                AddHandler ButtonStart.Click,
                Sub(sender0 As Object, e0 As EventArgs)
                    Task.Run(Sub()
                                 isStarted = Not isStarted
                                 SyncLock frmLock
                                     Threading.Thread.Sleep(10)
                                 End SyncLock
                                 frm.Invoke(Sub()
                                                If isStarted Then
                                                    ButtonStart.Text = "Stop"
                                                    Task.Run(
                                                    Sub()
                                                        SyncLock frmLock
                                                            While isStarted
                                                                Threading.Thread.Sleep(100)
                                                                If Not AllowOperation Then Continue While
                                                                frm.Invoke(Sub() frm.Text = $"Idle")
                                                                Dim dirListen As New IO.DirectoryInfo(txtLocation.Text)
                                                                If dirListen.GetDirectories().Count > 0 Then
                                                                    For i As Integer = 10 To 1 Step -1
                                                                        Dim startsec As Integer = i
                                                                        frm.Invoke(Sub() frm.Text = $"Will start in {startsec}s")
                                                                        Threading.Thread.Sleep(1000)
                                                                    Next
                                                                End If
                                                                Dim pathlist As New List(Of String)
                                                                For Each f As IO.FileInfo In dirListen.GetFiles()
                                                                    pathlist.Add(f.FullName)
                                                                Next
                                                                For Each d As IO.DirectoryInfo In dirListen.GetDirectories()
                                                                    pathlist.Add(d.FullName)
                                                                Next
                                                                Dim filter As String() = txtFilter.Text.Split({"|"}, StringSplitOptions.RemoveEmptyEntries)
                                                                If pathlist.Count > 0 Then
                                                                    If Not isStarted Then Exit Sub
                                                                    frm.Invoke(Sub() ButtonStart.Enabled = False)
                                                                    frm.Invoke(Sub() frm.Text = $"Writing")
                                                                    Dim cap As Long() = RefreshCapacity()
                                                                    Dim cap1 As Long
                                                                    If cap(3) > 0 Then cap1 = cap(2) Else cap1 = cap(0)
                                                                    Invoke(Sub()
                                                                               AddFileOrDir(schema._directory(0), pathlist.ToArray(), 覆盖已有文件ToolStripMenuItem.Checked, filter)
                                                                           End Sub)
                                                                    While AllowOperation
                                                                        Threading.Thread.Sleep(100)
                                                                    End While
                                                                    While Not AllowOperation
                                                                        Threading.Thread.Sleep(100)
                                                                    End While
                                                                    Dim DelList As New List(Of String)
                                                                    For Each f As FileRecord In UnwrittenFiles
                                                                        DelList.Add(f.SourcePath)
                                                                    Next
                                                                    Invoke(Sub()
                                                                               写入数据ToolStripMenuItem_Click(sender0, e0)
                                                                           End Sub)
                                                                    While AllowOperation
                                                                        Threading.Thread.Sleep(100)
                                                                    End While
                                                                    While Not AllowOperation
                                                                        Threading.Thread.Sleep(100)
                                                                    End While
                                                                    If chkAutoDelete.Checked OrElse MessageBox.Show(New Form With {.TopMost = True}, $"Delete written files?{vbCrLf}{DelList.Count}", "Confirm", MessageBoxButtons.OKCancel) = DialogResult.OK Then
                                                                        For Each s As String In DelList
                                                                            If IO.File.Exists(s) Then IO.File.Delete(s)
                                                                        Next
                                                                    End If

                                                                    Threading.Thread.Sleep(5000)
                                                                    frm.BeginInvoke(Sub() ButtonStart.Enabled = True)
                                                                End If
                                                            End While
                                                        End SyncLock
                                                    End Sub)
                                                Else
                                                    ButtonStart.Text = "Start"
                                                End If
                                            End Sub)

                             End Sub)

                End Sub
                frm.Show()
            End If

        Else
            Dim sum As Integer = 0
            For i As Integer = 0 To newkey.Length - 1
                sum += newkey(i)
            Next
            If sum = 0 Then
                EncryptionKey = Nothing
            Else
                EncryptionKey = newkey
            End If
        End If
        TapeUtils.SetEncryption(driveHandle, EncryptionKey)
    End Sub

    Private Sub 设置密码ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 设置密码ToolStripMenuItem.Click
        Dim key As String = ""
        key = InputBox(设置密码ToolStripMenuItem.Text, "LTFSWriter", key)
        If key.Length = 0 Then
            EncryptionKey = Nothing
        Else
            Dim newkey As Byte()
            Dim sha256 As System.Security.Cryptography.SHA256
            sha256 = System.Security.Cryptography.SHA256.Create()
            Dim strData As Byte() = Encoding.UTF8.GetBytes(key)
            sha256.TransformFinalBlock(strData, 0, strData.Length)
            newkey = sha256.Hash()
            EncryptionKey = newkey
        End If
        TapeUtils.SetEncryption(driveHandle, EncryptionKey)
    End Sub

    Private Sub 剪切文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 剪切文件ToolStripMenuItem.Click
        If ListView1.SelectedItems IsNot Nothing AndAlso
        ListView1.SelectedItems.Count > 0 Then
            Dim flist As New List(Of ltfsindex.file)
            Dim d As ltfsindex.directory = ListView1.Tag
            For Each SI As ListViewItem In ListView1.SelectedItems
                If TypeOf SI.Tag Is ltfsindex.file Then
                    Dim f As ltfsindex.file = CType(SI.Tag, ltfsindex.file)
                    If Not d.UnwrittenFiles.Contains(f) Then
                        flist.Add(f)
                        d.contents._file.Remove(f)
                    End If
                End If
            Next
            MyClipBoard.Add(flist)
            RefreshDisplay()
        End If
    End Sub

    Private Sub 剪切目录ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 剪切目录ToolStripMenuItem.Click
        If TreeView1.SelectedNode IsNot Nothing AndAlso TreeView1.SelectedNode.Parent IsNot Nothing Then
            Dim d As ltfsindex.directory = TreeView1.SelectedNode.Tag
            Dim dp As ltfsindex.directory = TreeView1.SelectedNode.Parent.Tag
            MyClipBoard.Add(d)
            dp.contents._directory.Remove(d)
            If TreeView1.SelectedNode.Parent IsNot Nothing AndAlso TreeView1.SelectedNode.Parent.Tag IsNot Nothing AndAlso TypeOf (TreeView1.SelectedNode.Parent.Tag) Is ltfsindex.directory Then
                TreeView1.SelectedNode = TreeView1.SelectedNode.Parent
            End If
            RefreshDisplay()
        End If
    End Sub

    Private Sub 粘贴选中ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 粘贴选中ToolStripMenuItem.Click
        If TreeView1.SelectedNode IsNot Nothing Then
            Dim droot As ltfsindex.directory = TreeView1.SelectedNode.Tag
            For Each f As ltfsindex.file In MyClipBoard.File
                schema.highestfileuid += 1
                droot.contents._file.Add(f)
            Next
            For Each d As ltfsindex.directory In MyClipBoard.Directory
                schema.highestfileuid += 1
                droot.contents._directory.Add(d)
            Next
            MyClipBoard.Clear()
            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
            RefreshDisplay()
        End If
    End Sub

    Private Sub ToolStripStatusLabel1_Click(sender As Object, e As EventArgs) Handles ToolStripStatusLabel1.Click
        If MyClipBoard.IsEmpty Then Exit Sub
        Dim fMsg As New Form With {.Width = 800, .Height = 600, .TopMost = True, .Text = My.Resources.ResText_ClipBoard}
        Dim ostr As New StringBuilder
        For Each d As ltfsindex.directory In MyClipBoard.Directory
            ostr.AppendLine($"DIR   {d.name}")
        Next
        For Each f As ltfsindex.file In MyClipBoard.File
            ostr.AppendLine($"FILE  {f.name}")
        Next
        fMsg.Controls.Add(New TextBox With {.Parent = fMsg, .Dock = DockStyle.Fill, .WordWrap = False,
                          .Multiline = True, .ScrollBars = ScrollBars.Both, .ReadOnly = True, .Text = ostr.ToString()})
        fMsg.ShowDialog()
    End Sub

    Private Sub 粘贴选中ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 粘贴选中ToolStripMenuItem1.Click
        粘贴选中ToolStripMenuItem_Click(sender, e)
    End Sub

    Private Sub 查找指定位置前的索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 查找指定位置前的索引ToolStripMenuItem.Click
        Dim blocknum As ULong
        Try
            blocknum = CULng(InputBox("Block number", "Index search", "0"))
        Catch ex As Exception

        End Try
        If blocknum <= 0 Then Exit Sub
        Dim th As New Threading.Thread(
            Sub()
                Try
                    If Not TapeUtils.IsOpened(driveHandle) Then TapeUtils.OpenTapeDrive(TapeDrive, driveHandle)
                    SetStatusLight(LWStatus.Busy)
                    PrintMsg(My.Resources.ResText_Locating)
                    Dim data As Byte()
                    Dim currentPos As TapeUtils.PositionData = GetPos
                    PrintMsg($"Position = {currentPos.ToString()}", LogOnly:=True)
                    If ExtraPartitionCount = 0 Then
                        TapeUtils.Locate(driveHandle, blocknum, 0)
                    Else
                        If currentPos.PartitionNumber <> 1 Then TapeUtils.Locate(driveHandle, 0UL, CByte(1), TapeUtils.LocateDestType.Block)
                        TapeUtils.Locate(driveHandle, blocknum, DataPartition)
                    End If
                    PrintMsg(My.Resources.ResText_RI)
                    currentPos = GetPos
                    PrintMsg($"Position = {currentPos.ToString()}", LogOnly:=True)
                    If DisablePartition Then
                        TapeUtils.Space6(driveHandle, -2, TapeUtils.LocateDestType.FileMark)
                    Else
                        Dim FM As Long = currentPos.FileNumber
                        If FM <= 1 Then
                            PrintMsg(My.Resources.ResText_IRFailed)
                            SetStatusLight(LWStatus.Err)
                            Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_NLTFS, My.Resources.ResText_Error))
                            LockGUI(False)
                            Exit Try
                        End If
                        TapeUtils.Locate(driveHandle, CULng(FM - 1), DataPartition, TapeUtils.LocateDestType.FileMark)
                    End If

                    TapeUtils.ReadFileMark(driveHandle)
                    PrintMsg(My.Resources.ResText_RI)
                    Dim outputfile As String = "schema\LTFSIndex_LoadDPIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                    If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "schema")) Then
                        IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "schema"))
                    End If
                    outputfile = IO.Path.Combine(Application.StartupPath, outputfile)
                    TapeUtils.ReadToFileMark(driveHandle, outputfile)
                    PrintMsg(My.Resources.ResText_AI)
                    schema = ltfsindex.FromSchFile(outputfile)
                    PrintMsg(My.Resources.ResText_AISucc)
                    While True
                        Threading.Thread.Sleep(0)
                        SyncLock UFReadCount
                            If UFReadCount > 0 Then Continue While
                            UnwrittenFiles.Clear()
                            CurrentFilesProcessed = 0
                            CurrentBytesProcessed = 0
                            TotalBytesUnindexed = 0
                            Exit While
                        End SyncLock
                    End While
                    Modified = False
                    Me.Invoke(Sub()
                                  ToolStripStatusLabel1.ToolTipText = ToolStripStatusLabel1.Text
                                  MaxCapacity = 0
                              End Sub)
                    RefreshDisplay()
                    RefreshCapacity()
                    CurrentHeight = -1
                    PrintMsg(My.Resources.ResText_IRSucc)
                    SetStatusLight(LWStatus.Idle)
                Catch ex As Exception
                    SetStatusLight(LWStatus.Err)
                    PrintMsg(My.Resources.ResText_IRFailed)
                End Try
                LockGUI(False)
            End Sub)
        LockGUI()
        th.Start()
    End Sub
    <Category("TapeUtils")>
    Public ReadOnly Property TapeUtils_DriveOpenCount As SerializableDictionary(Of String, Integer)
        Get
            Return TapeUtils.DriveOpenCount
        End Get
    End Property
    <Category("TapeUtils")>
    Public ReadOnly Property TapeUtils_DriveHandle As SerializableDictionary(Of String, IntPtr)
        Get
            Return TapeUtils.DriveHandle
        End Get
    End Property


    Private Sub 加锁ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 加锁ToolStripMenuItem.Click
        TapeUtils.OpenTapeDrive(TapeDrive, driveHandle)
        If TapeUtils.IsOpened(driveHandle) Then SetStatusLight(LWStatus.Idle)
        MessageBox.Show(New Form With {.TopMost = True}, $"Lock: {TapeUtils.DriveOpenCount(TapeDrive)}")
    End Sub

    Private Sub 新建压缩文件ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 新建压缩文件ToolStripMenuItem.Click
        If ListView1.Tag IsNot Nothing AndAlso FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            Dim dirname As String = FolderBrowserDialog1.SelectedPath

            Dim th As New Threading.Thread(
            Sub()
                Dim OnWriteFinishMessage As String = ""
                Try
                    Dim StartTime As Date = Now
                    PrintMsg("", True)
                    PrintMsg($"Position = {GetPos.ToString()}", LogOnly:=True)
                    PrintMsg(My.Resources.ResText_PrepW)
                    TapeUtils.ReserveUnit(driveHandle)
                    TapeUtils.PreventMediaRemoval(driveHandle)
                    If Not LocateToWritePosition() Then Exit Sub
                    Invoke(Sub() 更新数据区索引ToolStripMenuItem.Enabled = True)
                    UFReadCount.Inc()
                    CurrentFilesProcessed = 0
                    CurrentBytesProcessed = 0
                    UnwrittenSizeOverrideValue = 0
                    UnwrittenCountOverwriteValue = 0
                    If UnwrittenFiles.Count > 0 Then
                        UFReadCount.Inc()
                        UFReadCount.Dec()
                        UnwrittenCountOverwriteValue = UnwrittenCount
                        UnwrittenSizeOverrideValue = UnwrittenSize
                        Dim wBufferPtr As IntPtr = Marshal.AllocHGlobal(CInt(plabel.blocksize))

                        Dim HashTaskAwaitNumber As Integer = 0
                        Threading.ThreadPool.SetMaxThreads(1024, 1024)
                        Threading.ThreadPool.SetMinThreads(256, 256)

                        Dim p As New TapeUtils.PositionData(driveHandle)
                        TapeUtils.SetBlockSize(driveHandle, plabel.blocksize)
                        Try
                            Dim fr As New FileRecord
                            fr.File.fileuid = schema.highestfileuid + 1
                            schema.highestfileuid += 1
                            Dim fileextent As New ltfsindex.file.extent With
                                            {.partition = ltfsindex.PartitionLabel.b,
                                            .startblock = p.BlockNumber,
                                            .bytecount = 0,
                                            .byteoffset = 0,
                                            .fileoffset = 0}
                            fr.File.extentinfo.Add(fileextent)
                            PrintMsg($"{My.Resources.ResText_Writing} {fr.File.name}  {My.Resources.ResText_Size} {IOManager.FormatSize(fr.File.length)}", False,
                                 $"{My.Resources.ResText_Writing}: {fr.SourcePath}{vbCrLf}{My.Resources.ResText_Size}: {IOManager.FormatSize(fr.File.length)}{vbCrLf _
                                 }{My.Resources.ResText_WrittenTotal}: {IOManager.FormatSize(TotalBytesProcessed) _
                                 } {My.Resources.ResText_Remaining}: {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed)) _
                                 } -> {IOManager.FormatSize(Math.Max(0, UnwrittenSize - CurrentBytesProcessed - fr.File.length))}")
                            'write to tape

                            Select Case fr.Open()
                                Case DialogResult.Ignore
                                    PrintMsg($"Cannot open file {fr.SourcePath}", LogOnly:=True, ForceLog:=True)
                                Case DialogResult.Abort
                                    StopFlag = True
                                    Throw New Exception(My.Resources.ResText_FileOpenError)
                            End Select
                            'PrintMsg($"File Opened:{fr.SourcePath}", LogOnly:=True)
                            Dim sh As IOManager.CheckSumBlockwiseCalculator = Nothing
                            If HashOnWrite Then sh = New IOManager.CheckSumBlockwiseCalculator
                            Dim LastWriteTask As Task = Nothing
                            Dim ExitWhileFlag As Boolean = False
                            'Dim tstart As Date = Now
                            'Dim tsub As Double = 0
                            While Not StopFlag
                                Dim buffer(plabel.blocksize - 1) As Byte
                                Dim BytesReaded As UInteger
                                While True
                                    Try
                                        BytesReaded = fr.Read(buffer, 0, plabel.blocksize)
                                        Exit While
                                    Catch ex As Exception
                                        Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr }{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                            Case DialogResult.Abort
                                                StopFlag = True
                                                fr.Close()
                                                Throw ex
                                            Case DialogResult.Retry

                                            Case DialogResult.Ignore
                                                PrintMsg($"Cannot read file {fr.SourcePath}", LogOnly:=True, ForceLog:=True)
                                        End Select
                                    End Try
                                End While

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
                                                'Dim t0 As Date = Now
                                                sense = TapeUtils.Write(driveHandle, wBufferPtr, BytesReaded, True)
                                                'tsub += (Now - t0).TotalMilliseconds
                                                'Invoke(Sub() Text = tsub / (Now - tstart).TotalMilliseconds)
                                                SyncLock p
                                                    p.BlockNumber += 1
                                                End SyncLock
                                            Catch ex As Exception
                                                Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErrSCSI}{vbCrLf}{ex.ToString}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                    Case DialogResult.Abort
                                                        fr.Close()
                                                        StopFlag = True
                                                        Throw ex
                                                    Case DialogResult.Retry
                                                        succ = False
                                                    Case DialogResult.Ignore
                                                        succ = True
                                                        Exit While
                                                End Select
                                                p = New TapeUtils.PositionData(driveHandle)
                                                Continue While
                                            End Try
                                            If (((sense(2) >> 6) And &H1) = 1) Then
                                                If ((sense(2) And &HF) = 13) Then
                                                    PrintMsg(My.Resources.ResText_VOF)
                                                    Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_VOF))
                                                    StopFlag = True
                                                    fr.Close()
                                                    Exit Sub
                                                Else
                                                    PrintMsg(My.Resources.ResText_EWEOM, True)
                                                    succ = True
                                                    Exit While
                                                End If
                                            ElseIf sense(2) And &HF <> 0 Then
                                                Try
                                                    Throw New Exception("SCSI sense error")
                                                Catch ex As Exception
                                                    Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}{vbCrLf}{ex.StackTrace}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                                        Case DialogResult.Abort
                                                            fr.Close()
                                                            StopFlag = True
                                                            Throw New Exception(TapeUtils.ParseSenseData(sense))
                                                        Case DialogResult.Retry
                                                            succ = False
                                                        Case DialogResult.Ignore
                                                            succ = True
                                                            Exit While
                                                    End Select
                                                End Try

                                                p = New TapeUtils.PositionData(driveHandle)
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
                                             fr.File.SetXattr(ltfsindex.file.xattr.HashType.SHA1, sh.SHA1Value)
                                             fr.File.SetXattr(ltfsindex.file.xattr.HashType.MD5, sh.MD5Value)
                                             sh.StopFlag = True
                                             Threading.Interlocked.Decrement(HashTaskAwaitNumber)
                                         End Sub)
                            ElseIf sh IsNot Nothing Then
                                sh.StopFlag = True
                            End If
                            TotalFilesProcessed += 1
                            CurrentFilesProcessed += 1
                            p = GetPos
                            If p.EOP Then PrintMsg(My.Resources.ResText_EWEOM, True)
                            PrintMsg($"Position = {p.ToString()}", LogOnly:=True)
                            CurrentHeight = p.BlockNumber
                            'mark as written
                            fr.ParentDirectory.contents._file.Add(fr.File)
                            fr.ParentDirectory.UnwrittenFiles.Remove(fr.File)
                            If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
                            If CheckUnindexedDataSizeLimit() Then p = New TapeUtils.PositionData(driveHandle)
                            If CapacityRefreshInterval > 0 AndAlso (Now - LastRefresh).TotalSeconds > CapacityRefreshInterval Then
                                p = New TapeUtils.PositionData(driveHandle)
                                RefreshCapacity()
                                Dim p2 As New TapeUtils.PositionData(driveHandle)
                                If p2.BlockNumber <> p.BlockNumber OrElse p2.PartitionNumber <> p.PartitionNumber Then
                                    Invoke(Sub()
                                               If MessageBox.Show(New Form With {.TopMost = True}, $"Position changed! {p.BlockNumber} -> {p2.BlockNumber}", "Warning", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
                                                   StopFlag = True
                                               End If
                                           End Sub)
                                End If
                            End If
                        Catch ex As Exception
                            MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr}{vbCrLf}{ex.ToString}")
                            PrintMsg($"{My.Resources.ResText_WErr}{ex.Message}{vbCrLf}{ex.StackTrace}")
                        End Try
                        While Pause
                            Threading.Thread.Sleep(10)
                        End While
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
                        OnWriteFinishMessage = ($"{My.Resources.ResText_WFTime}{(Math.Floor(TimeCost.TotalHours)).ToString().PadLeft(2, "0")}:{TimeCost.Minutes.ToString().PadLeft(2, "0")}:{TimeCost.Seconds.ToString().PadLeft(2, "0")} {My.Resources.ResText_AvgS}{IOManager.FormatSize(TotalBytesWritten \ Math.Max(1, TimeCost.TotalSeconds))}/s")
                        OnWriteFinished()
                    Else
                        OnWriteFinishMessage = (My.Resources.ResText_WCnd)
                    End If
                Catch ex As Exception
                    MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr}{vbCrLf}{ex.ToString}")
                    PrintMsg($"{My.Resources.ResText_WErr}{ex.Message}")
                End Try
                TapeUtils.Flush(driveHandle)
                TapeUtils.ReleaseUnit(driveHandle)
                TapeUtils.AllowMediaRemoval(driveHandle)
                LockGUI(False)
                RefreshDisplay()
                RefreshCapacity()
                Invoke(Sub()
                           If Not StopFlag AndAlso WA0ToolStripMenuItem.Checked AndAlso MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_WFUp, My.Resources.ResText_OpSucc, MessageBoxButtons.OKCancel) = DialogResult.OK Then
                               更新数据区索引ToolStripMenuItem_Click(sender, e)
                           End If
                           PrintMsg(OnWriteFinishMessage)
                           RaiseEvent WriteFinished()
                       End Sub)
            End Sub)
            StopFlag = False
            LockGUI()
            th.Start()
        End If
    End Sub
    Public Sub ResetPowerPolicyUI0()
        无更改ToolStripMenuItem.Checked = False
        平衡ToolStripMenuItem.Checked = False
        节能ToolStripMenuItem.Checked = False
        高性能ToolStripMenuItem.Checked = False
        其他ToolStripMenuItem.Checked = False
        其他ToolStripMenuItem.Text = My.Resources.ResText_Other
    End Sub
    Public Sub ResetPowerPolicyUI1()
        无更改ToolStripMenuItem1.Checked = False
        平衡ToolStripMenuItem1.Checked = False
        节能ToolStripMenuItem1.Checked = False
        高性能ToolStripMenuItem1.Checked = False
        其他ToolStripMenuItem1.Checked = False
        其他ToolStripMenuItem1.Text = My.Resources.ResText_Other
    End Sub
    Private Sub 无更改ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 无更改ToolStripMenuItem.Click
        ResetPowerPolicyUI0()
        无更改ToolStripMenuItem.Checked = True
        My.Settings.LTFSWriter_PowerPolicyOnWriteBegin = Guid.Empty
    End Sub

    Private Sub 平衡ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 平衡ToolStripMenuItem.Click
        ResetPowerPolicyUI0()
        平衡ToolStripMenuItem.Checked = True
        My.Settings.LTFSWriter_PowerPolicyOnWriteBegin = New Guid("381b4222-f694-41f0-9685-ff5bb260df2e")
    End Sub

    Private Sub 节能ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 节能ToolStripMenuItem.Click
        ResetPowerPolicyUI0()
        节能ToolStripMenuItem.Checked = True
        My.Settings.LTFSWriter_PowerPolicyOnWriteBegin = New Guid("a1841308-3541-4fab-bc81-f71556f20b4a")
    End Sub

    Private Sub 高性能ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 高性能ToolStripMenuItem.Click
        ResetPowerPolicyUI0()
        高性能ToolStripMenuItem.Checked = True
        My.Settings.LTFSWriter_PowerPolicyOnWriteBegin = New Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")
    End Sub

    Private Sub 其他ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 其他ToolStripMenuItem.Click
        Try
            Dim s As String = InputBox("Power policy GUID", "Power policy on write begin", "")
            My.Settings.LTFSWriter_PowerPolicyOnWriteBegin = New Guid(s)
            ResetPowerPolicyUI0()
            其他ToolStripMenuItem.Checked = True
            其他ToolStripMenuItem.Text = $"{My.Resources.ResText_Other}: {My.Settings.LTFSWriter_PowerPolicyOnWriteBegin.ToString()}"
        Catch ex As Exception
            MessageBox.Show(New Form With {.TopMost = True}, ex.ToString())
        End Try
    End Sub

    Private Sub 无更改ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 无更改ToolStripMenuItem1.Click
        ResetPowerPolicyUI1()
        无更改ToolStripMenuItem1.Checked = True
        My.Settings.LTFSWriter_PowerPolicyOnWriteEnd = Guid.Empty
    End Sub

    Private Sub 平衡ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 平衡ToolStripMenuItem1.Click
        ResetPowerPolicyUI1()
        平衡ToolStripMenuItem1.Checked = True
        My.Settings.LTFSWriter_PowerPolicyOnWriteEnd = New Guid("381b4222-f694-41f0-9685-ff5bb260df2e")
    End Sub

    Private Sub 节能ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 节能ToolStripMenuItem1.Click
        ResetPowerPolicyUI1()
        节能ToolStripMenuItem1.Checked = True
        My.Settings.LTFSWriter_PowerPolicyOnWriteEnd = New Guid("a1841308-3541-4fab-bc81-f71556f20b4a")
    End Sub

    Private Sub 高性能ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 高性能ToolStripMenuItem1.Click
        ResetPowerPolicyUI1()
        高性能ToolStripMenuItem1.Checked = True
        My.Settings.LTFSWriter_PowerPolicyOnWriteEnd = New Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")
    End Sub

    Private Sub 其他ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles 其他ToolStripMenuItem1.Click
        Try
            Dim s As String = InputBox("Power policy GUID", "Power policy on write end", "")
            My.Settings.LTFSWriter_PowerPolicyOnWriteEnd = New Guid(s)
            ResetPowerPolicyUI1()
            其他ToolStripMenuItem1.Checked = True
            其他ToolStripMenuItem1.Text = $"{My.Resources.ResText_Other}: {My.Settings.LTFSWriter_PowerPolicyOnWriteEnd.ToString()}"
        Catch ex As Exception
            MessageBox.Show(New Form With {.TopMost = True}, ex.ToString())
        End Try
    End Sub

    Private Sub 详情ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 详情ToolStripMenuItem.Click
        If TreeView1.SelectedNode IsNot Nothing Then
            If TypeOf TreeView1.SelectedNode.Tag IsNot ltfsindex.directory Then Exit Sub
            Dim d As ltfsindex.directory = TreeView1.SelectedNode.Tag
            Dim PG1 As New SettingPanel
            PG1.PropertyGrid1.SelectedObject = d
            PG1.Text = TextBoxSelectedPath.Text
            If PG1.ShowDialog() = DialogResult.OK Then
                If TotalBytesUnindexed = 0 Then TotalBytesUnindexed = 1
            End If
        End If
    End Sub

    Private Sub 解锁ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 解锁ToolStripMenuItem.Click
        TapeUtils.CloseTapeDrive(driveHandle)
        If Not TapeUtils.IsOpened(driveHandle) Then SetStatusLight(LWStatus.NotReady)
        MessageBox.Show(New Form With {.TopMost = True}, $"Lock: {TapeUtils.DriveOpenCount(TapeDrive)}")
    End Sub

    Private Sub 错误率ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 错误率ToolStripMenuItem.Click
        Dim s As String = InputBox(My.Resources.ResText_ErrRateLog, My.Resources.ResText_Setting, My.Settings.LTFSWriter_AutoCleanErrRateLogThreashould)
        If s = "" Then Exit Sub
        My.Settings.LTFSWriter_AutoCleanErrRateLogThreashould = Val(s)
        My.Settings.Save()
        错误率ToolStripMenuItem.Text = $"{My.Resources.ResText_ErrRateLog}{My.Settings.LTFSWriter_AutoCleanErrRateLogThreashould}s"
    End Sub

    Private Sub 容量刷新间隔30sToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 容量刷新间隔30sToolStripMenuItem.Click
        Dim s As String = InputBox(My.Resources.ResText_SCIntv, My.Resources.ResText_Setting, CapacityRefreshInterval)
        If s = "" Then Exit Sub
        CapacityRefreshInterval = Val(s)
    End Sub


    Private Sub WA3ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles WA3ToolStripMenuItem.Click
        WA0ToolStripMenuItem.Checked = False
        WA1ToolStripMenuItem.Checked = False
        WA2ToolStripMenuItem.Checked = False
        WA3ToolStripMenuItem.Checked = True
    End Sub

    <Category("LTFSWriter")>
    Public Property LastSearchKW As String = ""
    Public Function GetSearchInput() As String
        LastSearchKW = InputBox("Keyword", "Search", LastSearchKW)
        Return LastSearchKW
    End Function
    Public Sub Search(Optional ByVal KW As String = Nothing)
        If KW Is Nothing Then KW = LastSearchKW
        Dim SearchStart As String = ""
        Dim result As New StringBuilder
        If TreeView1.SelectedNode IsNot Nothing Then
            If TreeView1.SelectedNode.Tag IsNot Nothing Then
                If TypeOf TreeView1.SelectedNode.Tag Is ltfsindex.directory Then
                    SearchStart = GetPath(TreeView1.SelectedNode) & "\"
                    If ListView1.Tag IsNot Nothing AndAlso
                                             ListView1.SelectedItems IsNot Nothing AndAlso
                                             ListView1.SelectedItems.Count > 0 Then
                        SearchStart &= CType(ListView1.SelectedItems(0).Tag, ltfsindex.file).name
                    End If
                End If
            End If
        End If
        SearchStart = SearchStart.TrimStart("\")
        If SearchStart = "" Then SearchStart = "\"
        Dim dirIndexStack As New List(Of Integer)
        Dim dirStack As New List(Of ltfsindex.directory)
        Dim fileindex As Integer = 0
        Dim pathSeg() As String = SearchStart.Split({"\"}, StringSplitOptions.None)
        dirIndexStack.Add(0)
        dirStack.Add(schema._directory(0))
        For i As Integer = 1 To pathSeg.Count - 2
            For j As Integer = 0 To dirStack(i - 1).contents._directory.Count - 1
                If dirStack(i - 1).contents._directory(j).name = pathSeg(i) Then
                    dirIndexStack.Add(j)
                    dirStack.Add(dirStack(i - 1).contents._directory(j))
                    Exit For
                End If
            Next
        Next
        If pathSeg.Last = "" Then
            fileindex = 0
            While dirStack.Last.contents._directory.Count > 0
                dirStack.Add(dirStack.Last.contents._directory(0))
                dirIndexStack.Add(0)
            End While
        Else
            For j As Integer = 0 To dirStack.Last.contents._file.Count - 1
                If dirStack.Last.contents._file(j).name = pathSeg.Last Then
                    fileindex = j + 1
                    Exit For
                End If
            Next
        End If
        LockGUI(True)
        Dim FIDMode As Boolean = KW.ToLower.StartsWith("fid:")
        Dim FID As String = ""
        If FIDMode Then
            FID = KW.Substring(4).TrimStart().TrimEnd()
        End If
        Task.Run(Sub()

                     While dirStack.Count > 0
                         Dim currpath As String = "Searching: "
                         For Each d As ltfsindex.directory In dirStack
                             currpath &= "\" & d.name
                         Next
                         PrintMsg(currpath)
                         If dirIndexStack.Last <= dirStack(dirStack.Count - 2).contents._directory.Count - 1 Then
                             Do
                                 If fileindex > dirStack.Last.contents._file.Count - 1 Then Exit Do
                                 If (FIDMode AndAlso dirStack.Last.contents._file(fileindex).fileuid.ToString = FID) OrElse dirStack.Last.contents._file(fileindex).name.Contains(KW) Then
                                     Invoke(Sub()
                                                Dim nd As TreeNode = TreeView1.Nodes(0)
                                                Try
                                                    For n As Integer = 1 To dirIndexStack.Count - 1
                                                        Invoke(Sub() nd.Expand())
                                                        nd = nd.Nodes(dirIndexStack(n))
                                                    Next
                                                Catch ex As Exception

                                                End Try
                                                If nd IsNot TreeView1.SelectedNode Then
                                                    TreeView1.SelectedNode = nd
                                                    RefreshDisplay()
                                                End If
                                                For Each it As ListViewItem In ListView1.Items
                                                    it.Selected = False
                                                Next
                                                Try
                                                    ListView1.Items(fileindex).Focused = True
                                                    ListView1.Items(fileindex).Selected = True
                                                    ListView1.EnsureVisible(fileindex)
                                                    PrintMsg($"{currpath}\{dirStack.Last.contents._file(fileindex).name}")
                                                Catch ex As Exception

                                                End Try
                                                LockGUI(False)
                                            End Sub)
                                     Exit Sub
                                 End If
                                 fileindex += 1
                             Loop While fileindex <= dirStack.Last.contents._file.Count - 1
                         End If

                         Dim returned As Boolean = False
                         If dirStack.Count - 2 >= 0 Then
                             While dirIndexStack.Last > dirStack(dirStack.Count - 2).contents._directory.Count - 1
                                 dirStack.RemoveAt(dirStack.Count - 1)
                                 dirIndexStack.RemoveAt(dirIndexStack.Count - 1)
                                 fileindex = 0
                                 returned = True
                                 If dirStack.Count <= 1 Then Exit While
                             End While
                             If returned Then
                                 If dirStack.Count <= 1 Then Exit While
                                 dirIndexStack(dirIndexStack.Count - 1) += 1
                                 If dirIndexStack.Last <= dirStack(dirStack.Count - 2).contents._directory.Count - 1 Then
                                     dirStack.RemoveAt(dirStack.Count - 1)
                                     dirStack.Add(dirStack(dirStack.Count - 1).contents._directory(dirIndexStack.Last))
                                 End If
                                 Continue While
                             End If

                         End If
                         If dirIndexStack.Last <= dirStack(dirStack.Count - 2).contents._directory.Count - 1 Then
                             If dirStack(dirStack.Count - 1).contents._directory.Count > 0 Then
                                 dirStack.Add(dirStack.Last.contents._directory(0))
                                 dirIndexStack.Add(0)
                                 fileindex = 0
                             Else
                                 If dirStack.Count <= 1 Then Exit While
                                 dirIndexStack(dirIndexStack.Count - 1) += 1
                                 If dirIndexStack.Last <= dirStack(dirStack.Count - 2).contents._directory.Count - 1 Then
                                     dirStack.RemoveAt(dirStack.Count - 1)
                                     dirStack.Add(dirStack(dirStack.Count - 1).contents._directory(dirIndexStack.Last))
                                 End If

                             End If
                         End If
                         If dirStack.Count <= 1 Then Exit While
                     End While
                     Invoke(Sub() LockGUI(False))
                     PrintMsg($"""{LastSearchKW}"" not found.")
                     MessageBox.Show(New Form With {.TopMost = True}, $"""{LastSearchKW}"" not found.")
                 End Sub)
    End Sub
    Private Sub LTFSWriter_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.F
                If Not AllowOperation Then Exit Sub
                If Not e.Control Then Exit Select
                If e.Alt Then Exit Select
                If e.Shift Then Exit Select
                Search(GetSearchInput())
            Case Keys.F3
                If Not AllowOperation Then Exit Sub
                If LastSearchKW = "" Then GetSearchInput()
                Search()
            Case Keys.F5
                If e.Control Then
                    Dim cmp As New ExplorerUtils
                    If ListView1.Tag IsNot Nothing AndAlso TypeOf ListView1.Tag Is ltfsindex.directory Then
                        Dim dir As ltfsindex.directory = CType(ListView1.Tag, ltfsindex.directory)
                        dir.contents._file.Sort(New Comparison(Of ltfsindex.file)(
                                                Function(a As ltfsindex.file, b As ltfsindex.file) As Integer
                                                    If e.Alt Then
                                                        Return a.name.CompareTo(b.name)
                                                    Else
                                                        Return cmp.Compare(a.name, b.name)
                                                    End If
                                                End Function))
                        dir.contents._directory.Sort(New Comparison(Of ltfsindex.directory)(
                                                Function(a As ltfsindex.directory, b As ltfsindex.directory) As Integer
                                                    If e.Alt Then
                                                        Return a.name.CompareTo(b.name)
                                                    Else
                                                        Return cmp.Compare(a.name, b.name)
                                                    End If
                                                End Function))

                    End If
                End If
                RefreshDisplay()
            Case Keys.F8
                LockGUI(AllowOperation)
            Case Keys.F12
                Task.Run(Sub()
                             Dim SP1 As New SettingPanel
                             SP1.Text = Text
                             SP1.SelectedObject = Me
                             SP1.ShowDialog()
                         End Sub)

        End Select
    End Sub

    Private Sub ToolStripStatusLabel1_MouseUp(sender As Object, e As MouseEventArgs) Handles ToolStripStatusLabel1.MouseUp
        If e.Button = MouseButtons.Right Then
            ContextMenuStrip4.Show(ToolStripStatusLabel1.GetCurrentParent, e.Location + ToolStripStatusLabel1.Bounds.Location)
        End If
    End Sub

    Private Sub LTFSWriter_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Try
            TapeUtils.CloseTapeDrive(driveHandle)
        Catch
        End Try
    End Sub

    Private Sub ToolTipChanErrLog_Popup(sender As Object, e As PopupEventArgs) Handles ToolTipChanErrLog.Popup
        Task.Run(Sub()
                     Threading.Thread.Sleep(3000)
                     ToolTipChanErrLogShowing = False
                     BeginInvoke(Sub() ToolTipChanErrLog.Hide(StatusStrip2))
                 End Sub)
    End Sub

    Private ToolTipChanErrLogShowingChanged As Boolean = False
    Private _ToolTipChanErrLogShowing As Boolean
    Public Property ToolTipChanErrLogShowing As Boolean
        Get
            Return _ToolTipChanErrLogShowing
        End Get
        Set(value As Boolean)
            _ToolTipChanErrLogShowing = value
            ToolTipChanErrLogShowingChanged = True
        End Set
    End Property
    Private Sub ToolStripStatusLabelErrLog_MouseHover(sender As Object, e As EventArgs) Handles ToolStripStatusLabelErrLog.MouseHover
        ToolTipChanErrLogShowing = True
    End Sub

    Private Sub ToolStripStatusLabelErrLog_MouseLeave(sender As Object, e As EventArgs) Handles ToolStripStatusLabelErrLog.MouseLeave
        ToolTipChanErrLogShowing = False
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
