Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Text

Public Class LTFSConfigurator
    Private LoadComplete As Boolean = False
    Private _SelectedIndex As Integer
    Public Function GetCurDrive() As TapeUtils.TapeDrive
        Dim dlist As List(Of TapeUtils.TapeDrive)
        If CheckBoxAutoRefresh.Checked OrElse LastDeviceList Is Nothing Then
            dlist = DeviceList
        Else
            dlist = LastDeviceList
        End If
        If dlist.Count <> ListBox1.Items.Count Then
            RefreshUI()
        End If
        If dlist.Count = 0 Then Return Nothing
        Return dlist(SelectedIndex)
    End Function
    Public ReadOnly Property TapeDrive As String
        Get
            Dim result As String = ""
            Try
                result = $"\\.\{ GetCurDrive.DeviceType}{GetCurDrive.DevIndex}"
            Catch ex As Exception
                If TextBoxDevicePath.Text <> "" Then
                    result = TextBoxDevicePath.Text
                Else
                    result = "\\.\TAPE0"
                End If

            End Try
            Return result
        End Get
    End Property
    Public Property ConfTapeDrive As String
        Get
            Return TextBoxDevicePath.Text
        End Get
        Set(value As String)
            TextBoxDevicePath.Text = value
        End Set
    End Property

    Public Property SelectedIndex As Integer
        Set(value As Integer)
            _SelectedIndex = Math.Max(0, value)
            If Not LoadComplete Then Exit Property
            Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
            If CurDrive Is Nothing Then
                ButtonAssign.Enabled = False
                ButtonLTFSWriter.Enabled = False
                ButtonRemove.Enabled = False
                ButtonLoadThreaded.Enabled = False
                ButtonEject.Enabled = False
                ButtonMount.Enabled = False
                'Button27.Enabled = False
                Exit Property
            End If
            TextBoxDevInfo.Text = CurDrive.ToString()
            If CurDrive.DriveLetter <> "" Then
                If Not ComboBoxDriveLetter.Items.Contains(CurDrive.DriveLetter) Then ComboBoxDriveLetter.Items.Add(CurDrive.DriveLetter)
                ComboBoxDriveLetter.SelectedItem = CurDrive.DriveLetter
                TextBoxMsg.AppendText(TapeUtils.CheckTapeMedia(CurDrive.DriveLetter) & vbCrLf)
            End If
            ComboBoxDriveLetter.Enabled = (CurDrive.DriveLetter = "")
            ButtonAssign.Enabled = (CurDrive.DriveLetter = "")
            ButtonLTFSWriter.Enabled = (CurDrive.DriveLetter = "")
            ButtonRemove.Enabled = (CurDrive.DriveLetter <> "")
            TextBoxDevicePath.Text = TapeDrive
            ButtonLoadThreaded.Enabled = True
            ButtonEject.Enabled = True
            ButtonMount.Enabled = (CurDrive.DriveLetter <> "")
        End Set
        Get
            Return _SelectedIndex
        End Get
    End Property
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of TapeUtils.TapeDrive), TapeUtils.TapeDrive)))>
    Public Property LastDeviceList As List(Of TapeUtils.TapeDrive)
    Public ReadOnly Property DeviceList As List(Of TapeUtils.TapeDrive)
        Get
            LastDeviceList = TapeUtils.GetTapeDriveList()
            Return LastDeviceList
        End Get
    End Property
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of Char), Char)))>
    Public ReadOnly Property AvailableDriveLetters As List(Of Char)
        Get
            Dim Result As New List(Of Char)
            Dim drv() As IO.DriveInfo = System.IO.DriveInfo.GetDrives()
            Dim UsedList As New List(Of Integer)
            For Each d As IO.DriveInfo In drv
                UsedList.Add(Asc(d.Name(0)))
            Next
            For c As Integer = Asc("D") To Asc("Z")
                If Not UsedList.Contains(c) Then
                    Result.Add(Chr(c))
                End If
            Next
            Return Result
        End Get
    End Property
    Public UILock As New Object
    Public Sub RefreshUI(Optional RefreshDevList As Boolean = True)
        If Not LoadComplete Then Exit Sub
        Task.Run(Sub()
                     LoadComplete = False
                     If Threading.Monitor.TryEnter(UILock, 100) Then
                         Dim DevList As List(Of TapeUtils.TapeDrive)
                         If RefreshDevList OrElse LastDeviceList Is Nothing Then DevList = DeviceList Else DevList = LastDeviceList
                         Invoke(Sub()
                                    ListBox1.Items.Clear()
                                    For Each D As TapeUtils.TapeDrive In DevList
                                        ListBox1.Items.Add(D.ToString())
                                    Next
                                    ListBox1.SelectedIndex = Math.Min(SelectedIndex, ListBox1.Items.Count - 1)
                                    Dim t As String = ComboBoxDriveLetter.Text
                                    ComboBoxDriveLetter.Items.Clear()
                                    ComboBoxDriveLetter.Text = ""
                                    For Each s As String In AvailableDriveLetters
                                        ComboBoxDriveLetter.Items.Add(s)
                                    Next
                                    If ComboBoxDriveLetter.Items.Count > 0 Then
                                        If Not ComboBoxDriveLetter.Items.Contains(t) Then
                                            ComboBoxDriveLetter.SelectedIndex = 0
                                        Else
                                            ComboBoxDriveLetter.Text = t
                                        End If
                                    End If

                                    LoadComplete = True
                                    SelectedIndex = ListBox1.SelectedIndex
                                End Sub)
                         Threading.Monitor.Exit(UILock)
                     End If
                 End Sub)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles ButtonRefresh.Click
        RefreshUI()
    End Sub

    Private Sub LTFSConfigurator_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Not New Security.Principal.WindowsPrincipal(Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(Security.Principal.WindowsBuiltInRole.Administrator) Then
            Process.Start(New ProcessStartInfo With {.FileName = Application.ExecutablePath, .Verb = "runas", .Arguments = "-c"})
            Me.Close()
            Exit Sub
        End If
        CheckBoxAutoRefresh.Checked = My.Settings.LTFSConf_AutoRefresh
        ComboBoxBufferPage.SelectedIndex = 3
        ComboBoxLocateType.SelectedIndex = 0
        Text = $"LTFSConfigurator - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}{My.Settings.Application_License}"
        LoadComplete = True
        RefreshUI()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles ButtonStartFUSESvc.Click
        Dim s As String = TapeUtils.StartLtfsService()
        If s = "" Then s = "OK"
        MessageBox.Show(New Form With {.TopMost = True}, s)
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles ButtonStopFUSESvc.Click
        Dim s As String = TapeUtils.StopLtfsService()
        If s = "" Then s = "OK"
        MessageBox.Show(New Form With {.TopMost = True}, s)
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles ButtonRemount.Click
        Dim s As String = TapeUtils.RemapTapeDrives()
        If s = "" Then s = "OK"
        MessageBox.Show(New Form With {.TopMost = True}, s)
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged
        If Not LoadComplete Then Exit Sub
        _SelectedIndex = ListBox1.SelectedIndex
        RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles ButtonAssign.Click
        If Not LoadComplete Then Exit Sub
        If MessageBox.Show(New Form With {.TopMost = True}, $"{ButtonAssign.Text} {TextBoxDevInfo.Text} <=> {ComboBoxDriveLetter.Text}", My.Resources.ResText_Confirm, MessageBoxButtons.OKCancel) = DialogResult.Cancel Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter = "" And ComboBoxDriveLetter.Text <> "" Then
                Dim result As String = TapeUtils.MapTapeDrive(ComboBoxDriveLetter.Text, CurDrive.DeviceType & CurDrive.DevIndex)
                If result = "" Then result = CurDrive.DeviceType & CurDrive.DevIndex & " <=> " & ComboBoxDriveLetter.Text & ":"
                result &= vbCrLf
                TextBoxMsg.AppendText(result)
            End If
        End If
        RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles ButtonRemove.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter <> "" Then
                Dim result As String = TapeUtils.UnMapTapeDrive(ComboBoxDriveLetter.Text)
                If result = "" Then result = CurDrive.DeviceType & CurDrive.DevIndex & " <=> ---" & ComboBoxDriveLetter.Text
                result &= vbCrLf
                TextBoxMsg.AppendText(result)
            End If
        End If
        RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles ButtonLoadThreaded.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBoxDriveLetter.Text
            Dim th As New Threading.Thread(
                Sub()
                    Dim result As String
                    Try
                        result = TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.LoadThreaded)
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = CurDrive.DeviceType & CurDrive.DevIndex & " loaded"
                               result &= vbCrLf
                               TextBoxMsg.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBoxAutoRefresh.Checked)
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI(CheckBoxAutoRefresh.Checked)

    End Sub

    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles ButtonEject.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBoxDriveLetter.Text
            Dim th As New Threading.Thread(
                Sub()
                    Dim result As String
                    Try
                        result = TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Eject)
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = CurDrive.DeviceType & CurDrive.DevIndex & " ejected"
                               result &= vbCrLf
                               TextBoxMsg.AppendText(result)
                               Panel1.Enabled = True
                               RefreshUI(CheckBoxAutoRefresh.Checked)
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles ButtonMount.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter <> "" And ComboBoxDriveLetter.Text <> "" Then
                Dim result As String = TapeUtils.MountTapeDrive(ComboBoxDriveLetter.Text)
                If result = "" Then result = CurDrive.DeviceType & CurDrive.DevIndex & " mounted"
                result &= vbCrLf
                TextBoxMsg.AppendText(result)
            End If
        End If
        RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxDebugPanel.CheckedChanged
        Panel2.Visible = CheckBoxDebugPanel.Checked
        ButtonLTFSWriter.Enabled = True
    End Sub

    Public Function HexStringToByteArray(s As String) As Byte()
        s = s.ToUpper
        Dim dataList As New List(Of Byte)
        Dim charbuffer As String = ""
        Dim allowedChar() As String = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F"}
        For i As Integer = 0 To s.Length - 1
            If allowedChar.Contains(s(i)) Then
                charbuffer &= s(i)
            End If
            If charbuffer.Length = 2 Or (charbuffer.Length = 1 And s(i) = " ") Then
                dataList.Add(Convert.ToByte(charbuffer, 16))
                charbuffer = ""
            End If
        Next
        Return dataList.ToArray()
    End Function
    Public Shared Function Byte2Hex(bytes As Byte(), Optional ByVal TextShow As Boolean = False) As String
        Const HalfWidthChars As String = "~!@#$%^&*()_+-=|\ <>?,./:;""''{}[]0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
        If bytes Is Nothing Then Return ""
        If bytes.Length = 0 Then Return ""
        Dim sb As New StringBuilder
        Dim tb As String = ""
        Dim ln As New StringBuilder
        For i As Integer = 0 To bytes.Length - 1
            If i Mod 16 = 0 And TextShow Then
                ln.Append("|" & Hex(i).PadLeft(5) & "h: ")
            End If
            ln.Append(Convert.ToString((bytes(i) And &HFF) + &H100, 16).Substring(1).ToUpper)
            ln.Append(" ")
            Dim c As Char = Chr(bytes(i))
            If Not HalfWidthChars.Contains(c) Then
                tb &= "."
            Else
                tb &= c
            End If
            If i Mod 16 = 15 Then
                If TextShow Then
                    ln.Append(tb)
                End If
                sb.Append(ln.ToString().PadRight(74) & "|")
                sb.Append(vbCrLf)
                ln = New StringBuilder()
                tb = ""
            End If
        Next
        If TextShow And tb <> "" Then
            ln.Append(tb)
        End If
        If ln.Length > 0 Then sb.Append(ln.ToString().PadRight(74) & "|")
        Return sb.ToString()
    End Function
    Private Sub ButtonDebugSendSCSICommand_Click(sender As Object, e As EventArgs) Handles ButtonDebugSendSCSICommand.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    Dim cdbData() As Byte = HexStringToByteArray(TextBoxCDBData.Text)
                    Dim dataData() As Byte = {}
                    Dim cdb As IntPtr = Marshal.AllocHGlobal(cdbData.Length)
                    Marshal.Copy(cdbData, 0, cdb, cdbData.Length)
                    Dim dataBufferPtr As IntPtr
                    If TextBoxParamData.Text.Length >= 2 Then
                        dataData = HexStringToByteArray(TextBoxParamData.Text)
                        dataBufferPtr = Marshal.AllocHGlobal(dataData.Length)
                        Marshal.Copy(dataData, 0, dataBufferPtr, dataData.Length)
                    Else
                        dataBufferPtr = Marshal.AllocHGlobal(64)
                    End If
                    Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(64)

                    Dim senseBuffer(63) As Byte
                    Marshal.Copy(senseBuffer, 0, senseBufferPtr, 64)
                    Dim succ As Boolean
                    SyncLock TapeUtils.SCSIOperationLock
                        Dim handle As IntPtr
                        TapeUtils.OpenTapeDrive(ConfTapeDrive, handle)
                        succ = TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdb, cdbData.Length, dataBufferPtr, dataData.Length, TextBoxDataDir.Text, CInt(TextBoxTimeoutValue.Text), senseBufferPtr)
                        TapeUtils.CloseTapeDrive(handle)
                    End SyncLock
                    Marshal.Copy(dataBufferPtr, dataData, 0, dataData.Length)
                    Marshal.Copy(senseBufferPtr, senseBuffer, 0, senseBuffer.Length)
                    Me.Invoke(Sub()
                                  TextBoxDebugOutput.Text = "DataBuffer" & vbCrLf
                                  TextBoxDebugOutput.Text &= Byte2Hex(dataData, True)
                                  TextBoxDebugOutput.Text &= vbCrLf & vbCrLf & "SenseBuffer" & vbCrLf
                                  TextBoxDebugOutput.Text &= Byte2Hex(senseBuffer) & vbCrLf
                                  TextBoxDebugOutput.Text &= TapeUtils.ParseSenseData(senseBuffer) & vbCrLf
                              End Sub)
                    'Marshal.Copy(senseBufferPtr, senseBuffer, 0, 127)
                    Marshal.FreeHGlobal(cdb)
                    Marshal.FreeHGlobal(dataBufferPtr)
                    Marshal.FreeHGlobal(senseBufferPtr)
                    If succ Then
                        Me.Invoke(Sub() TextBoxDebugOutput.Text &= vbCrLf & "OK")
                    Else
                        Me.Invoke(Sub() TextBoxDebugOutput.Text &= vbCrLf & "FAIL")
                    End If
                Catch ex As Exception
                    MessageBox.Show(New Form With {.TopMost = True}, ex.ToString)
                End Try
                Me.Invoke(Sub() Panel2.Enabled = True)
            End Sub)
        Panel2.Enabled = False
        th.Start()

    End Sub

    Private Sub Button13_Click(sender As Object, e As EventArgs) Handles ButtonLoadUnthreaded.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBoxDriveLetter.Text
            Dim th As New Threading.Thread(
                Sub()
                    Dim result As String
                    Try
                        result = TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.LoadUnthreaded)
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = CurDrive.DeviceType & CurDrive.DevIndex & " loaded (unthread)"
                               result &= vbCrLf
                               TextBoxMsg.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBoxAutoRefresh.Checked)
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub Button14_Click(sender As Object, e As EventArgs) Handles ButtonUnthread.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBoxDriveLetter.Text
            Dim th As New Threading.Thread(
                Sub()
                    Dim result As String
                    Try
                        result = TapeUtils.LoadEject(TapeDrive, TapeUtils.LoadOption.Unthread)
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = CurDrive.DeviceType & CurDrive.DevIndex & " unthreaded"
                               result &= vbCrLf
                               TextBoxMsg.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBoxAutoRefresh.Checked)
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub ButtonDebugErase_Click(sender As Object, e As EventArgs) Handles ButtonDebugErase.Click
        If Not LoadComplete Then Exit Sub
        If MessageBox.Show(New Form With {.TopMost = True}, "Data will be cleared on this tape. Continue?", "Warning", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then Exit Sub
        Panel1.Enabled = False
        Dim dL As Char = ComboBoxDriveLetter.Text
        Dim th As New Threading.Thread(
                Sub()
                    Invoke(Sub() TextBoxDebugOutput.Text = "Start erase ..." & vbCrLf)
                    Try
                        'result = TapeUtils.LoadTapeDrive(dL, True)

                        'Load and Thread
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Loading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 1, 0}) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Mode Sense
                        Invoke(Sub() TextBoxDebugOutput.AppendText("MODE SENSE"))
                        Dim ModeData As Byte()
                        ModeData = TapeUtils.ModeSense(TapeDrive, &H11)
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"     Mode Data: {Byte2Hex(ModeData)}{vbCrLf}"))
                        ReDim Preserve ModeData(11)
                        'Mode Select:1st Partition to Minimum 
                        Invoke(Sub() TextBoxDebugOutput.AppendText("MODE SELECT - Partition mode page.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H15, &H10, 0, 0, &H10, 0}, {0, 0, &H10, 0, &H11, &HA, ModeData(2), 1, ModeData(4), ModeData(5), ModeData(6), ModeData(7), 0, 1, &HFF, &HFF}, 0) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Format
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Partitioning.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {4, 0, 1, 0, 0, 0}, Nothing, 0) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        For i As Integer = 1 To NumericUpDownEraseCycle.Value
                            'Unthread
                            Invoke(Sub() TextBoxDebugOutput.AppendText("Unthreading.."))
                            If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, &HA, 0}) Then
                                Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                            Else
                                Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                                Exit Try
                            End If
                            'Thread
                            Invoke(Sub() TextBoxDebugOutput.AppendText("Threading.."))
                            If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 1, 0}) Then
                                Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                            Else
                                Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                                Exit Try
                            End If
                            'Erase
                            Invoke(Sub() TextBoxDebugOutput.AppendText("Erasing " & i & "/" & NumericUpDownEraseCycle.Value & ".."))
                            If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H19, 1, 0, 0, 0, 0}, TimeOut:=320) Then
                                Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                            Else
                                Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                                Exit Try
                            End If
                        Next
                        'Unthread
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Unthreading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, &HA, 0}) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Thread
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Threading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 1, 0}) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Remove Partition
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Reinitializing.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {4, 0, 0, 0, 0, 0}) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Unload
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Unloading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 0, 0}) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                    Catch ex As Exception
                        Invoke(Sub() TextBoxDebugOutput.AppendText(ex.ToString()))
                    End Try
                    Invoke(Sub() TextBoxDebugOutput.AppendText("Erase finished."))
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBoxAutoRefresh.Checked)
                           End Sub)
                End Sub)
        th.Start()
        If Panel1.Enabled Then RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub ButtonDebugWriteBarcode_Click(sender As Object, e As EventArgs) Handles ButtonDebugWriteBarcode.Click
        If Not LoadComplete Then Exit Sub
        Panel1.Enabled = False
        Dim barcode As String = TextBoxBarcode.Text
        Dim th As New Threading.Thread(
                Sub()
                    Dim result As String = ""
                    Try
                        result &= TapeUtils.SetBarcode(ConfTapeDrive, barcode)
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = ConfTapeDrive & " Barcode = " & barcode
                               result &= vbCrLf
                               TextBoxMsg.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBoxAutoRefresh.Checked)
                           End Sub)
                End Sub)
        th.Start()
        If Panel1.Enabled Then RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub Label11_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub ButtonDebugReadMAM_Click(sender As Object, e As EventArgs) Handles ButtonDebugReadMAM.Click
        If Not LoadComplete Then Exit Sub
        Panel1.Enabled = False
        Dim PCH As Byte = NumericUpDownPCHigh.Value
        Dim PCL As Byte = NumericUpDownPCLow.Value
        Dim PN As Byte = NumericUpDownPartitionNum.Value
        Task.Run(Sub()
                     Dim ResultB As Byte() = TapeUtils.GetMAMAttributeBytes(ConfTapeDrive, PCH, PCL, PN)
                     If ResultB.Length = 0 Then Exit Sub
                     Dim Result As String = System.Text.Encoding.UTF8.GetString(ResultB)
                     Invoke(Sub()
                                If Result <> "" Then TextBoxDebugOutput.Text = ("Result: " & vbCrLf & Result & vbCrLf & vbCrLf)
                                TextBoxDebugOutput.AppendText(Byte2Hex(ResultB))
                                Panel1.Enabled = True
                            End Sub)
                 End Sub)


    End Sub

    Private Sub Label13_Click(sender As Object, e As EventArgs) Handles LabelMAMAttrib.Click
        MessageBox.Show(New Form With {.TopMost = True}, "/* Page code of Application Name */
#define TC_MAM_PAGE_APP_NAME       (0x0801) 
#define TC_MAM_PAGE_APP_NAME_SIZE  (0x20)

/* Page code for Application Vendor */
#define TC_MAM_APPLICATION_VENDOR    (0x0800) 
#define TC_MAM_APPLICATION_VENDOR_LEN  8

/* Page code for Application Name */
#define TC_MAM_APPLICATION_NAME      0x0801 
#define TC_MAM_APPLICATION_NAME_LEN    32

/* Page code for Application Version */
#define TC_MAM_APPLICATION_VERSION   0x0802 
#define TC_MAM_APPLICATION_VERSION_LEN 8

/* Page code for Format Version */
#define TC_MAM_APP_FORMAT_VERSION    0x080B 
#define TC_MAM_APP_FORMAT_VERSION_LEN 16

/* Page code for Custom Volume Name */
#define TC_MAM_USR_MED_TXT_LABEL     0x0803 
#define TC_MAM_USR_MED_TXT_LABEL_LEN 160

/* Page code for Custom Barcode Name */
#define TC_MAM_BARCODE     			 0x0806 
#define TC_MAM_BARCODE_LEN 			 32

/* Page code for Volume Lock State */
#define TC_MAM_VOL_LOCK_STATE		 0x1623 
#define TC_MAM_VOL_LOCK_STATE_LEN	 1
")
    End Sub
    Public Function ReduceDataUnit(MBytes As Int64) As String
        Dim Result As Decimal = MBytes
        Dim ResultUnit As Integer = 0
        While Result >= 1000
            Result /= 1024
            ResultUnit += 3
        End While
        Dim ResultString As String = Math.Round(Result, 2)
        Select Case ResultUnit
            Case 0
                Return ResultString & " MiB"
            Case 3
                Return ResultString & " GiB"
            Case 6
                Return ResultString & " TiB"
            Case 9
                Return ResultString & " PiB"
            Case 12
                Return ResultString & " EiB"
            Case 15
                Return ResultString & " ZiB"
            Case 18
                Return ResultString & " YiB"
            Case Else
                Return ResultString & " << " & ResultUnit & "MiB"
        End Select
    End Function
    Private Sub ButtonDebugReadInfo_Click(sender As Object, e As EventArgs) Handles ButtonDebugReadInfo.Click
        Me.Enabled = False
        Dim CMInfo As TapeUtils.CMParser = Nothing
        TextBoxDebugOutput.Text = ""
        Task.Run(Sub()
                     Try
                         CMInfo = New TapeUtils.CMParser(TapeDrive)
                     Catch ex As Exception
                         Invoke(Sub() TextBoxDebugOutput.AppendText("CM Data Parsing Failed." & vbCrLf & ex.ToString & vbCrLf))
                     End Try
                     Invoke(Sub()
                                Try
                                    TextBoxDebugOutput.AppendText(CMInfo.GetReport())
                                Catch ex As Exception
                                    TextBoxDebugOutput.AppendText("Report generation failed.".PadRight(74) & vbCrLf & ex.ToString & vbCrLf)
                                End Try
                                Try
                                    If CheckBoxParseCMData.Checked AndAlso CMInfo IsNot Nothing Then
                                        TextBoxDebugOutput.AppendText(CMInfo.GetSerializedText())
                                        Dim PG1 As New SettingPanel
                                        PG1.SelectedObject = CMInfo
                                        PG1.Text = CMInfo.CartridgeMfgData.CartridgeSN
                                        PG1.Show()
                                        TextBoxDebugOutput.AppendText(vbCrLf)
                                    End If
                                Catch ex As Exception
                                    TextBoxDebugOutput.AppendText("CM Data Parsing failed.".PadRight(74) & vbCrLf & ex.ToString & vbCrLf)
                                End Try
                                TextBoxDebugOutput.Select(0, 0)
                                TextBoxDebugOutput.ScrollToCaret()
                                If IO.Directory.Exists(My.Application.Info.DirectoryPath & "\Info") Then
                                    Dim fn As String
                                    Try
                                        fn = CMInfo.ApplicationSpecificData.Barcode
                                        If fn Is Nothing OrElse fn.Length = 0 Then fn = CMInfo.CartridgeMfgData.CartridgeSN
                                        If fn Is Nothing Then fn = ""
                                        IO.File.WriteAllText($"{My.Application.Info.DirectoryPath}\Info\{fn}.txt", TextBoxDebugOutput.Text)
                                    Catch ex As Exception

                                    End Try
                                End If
                                Me.Enabled = True
                            End Sub)
                 End Sub)
    End Sub

    Private Sub ButtonDebugDumpMAM_Click(sender As Object, e As EventArgs) Handles ButtonDebugDumpMAM.Click
        If SaveFileDialog1.ShowDialog = DialogResult.OK Then
            ButtonDebugDumpMAM.Enabled = False

            Dim th As New Threading.Thread(
                Sub()
                    Dim MAMData As New TapeUtils.MAMAttributeList
                    For i As UInt16 = &H0 To &HFFFF Step 1

                        Try
                            Dim Attr As TapeUtils.MAMAttribute = TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, i, CByte(NumericUpDownPartitionNum.Value))
                            If Attr IsNot Nothing Then
                                Me.Invoke(Sub()
                                              TextBoxDebugOutput.Text = Byte2Hex({Attr.ID_MSB, Attr.ID_LSB}) & " LEN=" & Attr.RawData.Length & vbCrLf & vbCrLf
                                              TextBoxDebugOutput.AppendText(Attr.AsNumeric & vbCrLf & vbCrLf)
                                              TextBoxDebugOutput.AppendText(Attr.AsString & vbCrLf & vbCrLf)
                                              TextBoxDebugOutput.AppendText(Byte2Hex(Attr.RawData) & vbCrLf)
                                          End Sub)
                                MAMData.Content.Add(Attr)
                            Else
                                If (i And &H7F) = 0 Then
                                    Dim i2 As UInt16 = i
                                    Me.Invoke(Sub()
                                                  TextBoxDebugOutput.Text = Byte2Hex({i2 >> 8 And &HFF, i2 And &HFF}) & " LEN=0"
                                              End Sub)
                                End If

                            End If
                        Catch ex As Exception
                            MessageBox.Show(New Form With {.TopMost = True}, i & vbCrLf & ex.ToString)
                        End Try
                        If i = &HFFFF Then Exit For
                    Next
                    MessageBox.Show(New Form With {.TopMost = True}, "Dump Complete")
                    MAMData.SaveSerializedText(SaveFileDialog1.FileName)
                    Me.Invoke(Sub() ButtonDebugDumpMAM.Enabled = True)
                End Sub)
            th.Start()
        End If
    End Sub

    Private Sub ButtonDebugRewind_Click(sender As Object, e As EventArgs) Handles ButtonDebugRewind.Click
        Me.Enabled = False
        Task.Run(Sub()
                     Dim cdbData As Byte() = {1, 0, 0, 0, 0, 0}
                     Dim cdb As IntPtr = Marshal.AllocHGlobal(6)
                     Marshal.Copy(cdbData, 0, cdb, 6)
                     Dim data As IntPtr = Marshal.AllocHGlobal(1)
                     Dim sense As IntPtr = Marshal.AllocHGlobal(127)
                     Dim handle As IntPtr
                     SyncLock TapeUtils.SCSIOperationLock
                         TapeUtils.OpenTapeDrive(ConfTapeDrive, handle)
                         TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdb, 6, data, 0, 2, 60000, sense)
                         TapeUtils.CloseTapeDrive(handle)
                     End SyncLock
                     Marshal.FreeHGlobal(cdb)
                     Marshal.FreeHGlobal(data)
                     Marshal.FreeHGlobal(sense)
                     Invoke(Sub() Me.Enabled = True)
                 End Sub)

    End Sub

    Private Sub ButtonDebugReadBlock_Click(sender As Object, e As EventArgs) Handles ButtonDebugReadBlock.Click
        Me.Enabled = False
        Dim ReadLen As UInteger = NumericUpDownBlockLen.Value
        Task.Run(Sub()
                     Dim sense(63) As Byte
                     Dim readData As Byte()
                     Try
                         readData = TapeUtils.ReadBlock(ConfTapeDrive, sense, ReadLen)
                         Invoke(Sub()
                                    Dim DiffBytes As Int32
                                    For i As Integer = 3 To 6
                                        DiffBytes <<= 8
                                        DiffBytes = DiffBytes Or sense(i)
                                    Next
                                    Dim Add_Key As UInt16 = CInt(sense(12)) << 8 Or sense(13)
                                    TextBoxDebugOutput.Text = TapeUtils.ParseAdditionalSenseCode(Add_Key) & vbCrLf & vbCrLf & "Raw data:" & vbCrLf
                                    TextBoxDebugOutput.Text &= "Length: " & readData.Length & vbCrLf
                                    If DiffBytes < 0 Then
                                        TextBoxDebugOutput.Text &= TapeUtils.ParseSenseData(sense) & vbCrLf
                                        TextBoxDebugOutput.Text &= "Excess data is discarded. Block length should be " & readData.Length - DiffBytes & vbCrLf & vbCrLf
                                    End If
                                    TextBoxDebugOutput.Text &= Byte2Hex(readData, True)
                                End Sub)
                     Catch ex As Exception

                     End Try

                     Invoke(Sub()
                                Enabled = True
                            End Sub)
                 End Sub)
    End Sub

    Private Sub ButtonDebugDumpBuffer_Click(sender As Object, e As EventArgs) Handles ButtonDebugDumpBuffer.Click
        Me.Enabled = False
        Dim BufferID = Convert.ToByte(ComboBoxBufferPage.SelectedItem.Substring(0, 2), 16)
        Task.Run(Sub()
                     Dim DumpData As Byte() = TapeUtils.ReadBuffer(ConfTapeDrive, BufferID)
                     Invoke(Sub()
                                TextBoxDebugOutput.Text = "Buffer len=" & DumpData.Length & vbCrLf
                                SaveFileDialog2.FileName = ComboBoxBufferPage.SelectedItem & ".bin"
                                If SaveFileDialog2.ShowDialog = DialogResult.OK Then
                                    IO.File.WriteAllBytes(SaveFileDialog2.FileName, DumpData)
                                End If
                                TextBoxDebugOutput.Text &= Byte2Hex(DumpData, True)
                                Me.Enabled = True
                            End Sub)
                 End Sub)
    End Sub

    Private Sub Label9_Click(sender As Object, e As EventArgs) Handles LabelParam.Click
        Dim n As String = InputBox("Byte count?", "cdb SetBytes", "")
        If n <> "" Then
            If Val(n) > 0 Then
                TextBoxParamData.Text = ""
                Dim sb As New StringBuilder
                For i As Integer = 1 To Val(n)
                    sb.Append("00 ")
                Next
                TextBoxParamData.Text = sb.ToString()
            End If
        End If
    End Sub

    Private Sub ButtonDebugLocate_Click(sender As Object, e As EventArgs) Handles ButtonDebugLocate.Click
        Me.Enabled = False
        Dim blk As ULong = NumericUpDownBlockNum.Value
        Dim partition As Byte = NumericUpDownPartitionNum.Value
        Dim dest As TapeUtils.LocateDestType = System.Enum.Parse(GetType(TapeUtils.LocateDestType), ComboBoxLocateType.SelectedItem)
        Task.Run(Sub()
                     Dim result As String = ""
                     Try
                         result = TapeUtils.ParseAdditionalSenseCode(TapeUtils.Locate(ConfTapeDrive, blk, partition, dest))
                     Catch ex As Exception

                     End Try

                     Invoke(Sub()
                                TextBoxDebugOutput.Text = result
                                Me.Enabled = True
                            End Sub)
                 End Sub)



    End Sub

    Private Sub ButtonDebugReadPosition_Click(sender As Object, e As EventArgs) Handles ButtonDebugReadPosition.Click
        Panel1.Enabled = False
        Task.Run(Sub()
                     Dim pos As New TapeUtils.PositionData(ConfTapeDrive)
                     Invoke(Sub()
                                TextBoxDebugOutput.Text = ""
                                TextBoxDebugOutput.Text &= "Partition " & pos.PartitionNumber & vbCrLf
                                TextBoxDebugOutput.Text &= "Block " & pos.BlockNumber & vbCrLf
                                TextBoxDebugOutput.Text &= "FileMark " & pos.FileNumber & vbCrLf
                                TextBoxDebugOutput.Text &= "Set " & pos.SetNumber & vbCrLf
                                TextBoxDebugOutput.Text &= vbCrLf
                                If pos.BOP Then TextBoxDebugOutput.Text &= "BOM - Beginning of media" & vbCrLf
                                If pos.EOP Then TextBoxDebugOutput.Text &= "EW-EOM - Early warning" & vbCrLf
                                If pos.EOD Then TextBoxDebugOutput.Text &= "End of Data detected" & vbCrLf
                                Panel1.Enabled = True
                            End Sub)
                 End Sub)
    End Sub
    Public Operation_Cancel_Flag As Boolean = False
    Private Sub ButtonDebugDumpTape_Click(sender As Object, e As EventArgs) Handles ButtonDebugDumpTape.Click
        If FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            If New IO.DirectoryInfo(FolderBrowserDialog1.SelectedPath).GetFiles("*.bin", IO.SearchOption.TopDirectoryOnly).Length > 0 Then
                MessageBox.Show(New Form With {.TopMost = True}, "File exist: *.bin; Cancelled.")
                Exit Sub
            End If

            For Each c As Control In Panel1.Controls
                c.Enabled = False
            Next
            Panel2.Enabled = True
            For Each c As Control In Panel2.Controls
                c.Enabled = False
            Next
            For Each c As Control In TabControl1.Controls
                c.Enabled = False
            Next
            For Each c As Control In TabPageCommand.Controls
                c.Enabled = False
            Next
            TabControl1.Enabled = True
            TabPageCommand.Enabled = True
            ButtonStopRawDump.Enabled = True
            TextBoxDebugOutput.Text = ""
            Dim log As Boolean = CheckBoxEnableDumpLog.Checked
            Dim thprog As New Threading.Thread(
                Sub()
                    Dim ReadLen As UInteger = NumericUpDownBlockLen.Value
                    Dim FileNum As Integer = 0

                    'Position
                    Dim pos As New TapeUtils.PositionData(ConfTapeDrive)

                    Dim Partition As Byte = pos.PartitionNumber
                    Dim Block As UInteger = pos.BlockNumber
                    Dim BlkNum As Integer = Block
                    While True
                        Dim sense(63) As Byte
                        Dim readData As Byte() = TapeUtils.ReadBlock(ConfTapeDrive, sense, ReadLen)
                        Dim Add_Key As UInt16 = CInt(sense(12)) << 8 Or sense(13)
                        If readData.Length > 0 Then
                            My.Computer.FileSystem.WriteAllBytes($"{FolderBrowserDialog1.SelectedPath}\FM{FileNum}_BLK{BlkNum}.bin", readData, True)
                            If Not readData.Length = ReadLen Then
                                BlkNum = Block
                            End If
                        End If
                        If Add_Key <> 0 Then
                            FileNum += 1
                            BlkNum = Block
                        End If
                        If (Add_Key > 1 And Add_Key <> 4) Or Operation_Cancel_Flag Then
                            Operation_Cancel_Flag = False
                            Exit While
                        End If
                        If log Then
                            Invoke(Sub()
                                       If TextBoxDebugOutput.Text.Length > 10000 Then TextBoxDebugOutput.Text = ""
                                       TextBoxDebugOutput.AppendText("Processing file " & FileNum.ToString.PadRight(10) & " (Block = " & Block & ") Sense:")
                                       TextBoxDebugOutput.AppendText(TapeUtils.ParseAdditionalSenseCode(Add_Key) & vbCrLf)
                                   End Sub)
                        End If
                        Block += 1
                    End While
                    Invoke(Sub()
                               For Each c As Control In Panel1.Controls
                                   c.Enabled = True
                               Next
                               For Each c As Control In Panel2.Controls
                                   c.Enabled = True
                               Next
                               For Each c As Control In TabControl1.Controls
                                   c.Enabled = True
                               Next
                               For Each c As Control In TabPageCommand.Controls
                                   c.Enabled = True
                               Next
                           End Sub)
                End Sub)
            thprog.Start()

        End If
    End Sub

    Private Sub Button24_Click(sender As Object, e As EventArgs) Handles ButtonStopRawDump.Click
        Operation_Cancel_Flag = True
    End Sub

    Private Sub ButtonDebugDumpIndex_Click(sender As Object, e As EventArgs) Handles ButtonDebugDumpIndex.Click
        Me.Enabled = False
        Task.Run(Sub()
                     Try
                         TapeUtils.Locate(ConfTapeDrive, 3, 0, TapeUtils.LocateDestType.FileMark)
                         TapeUtils.ReadBlock(ConfTapeDrive)
                         Dim data As Byte() = TapeUtils.ReadToFileMark(ConfTapeDrive)
                         Dim outputfile As String = "schema\LTFSIndex_" & Now.ToString("yyyyMMdd_HHmmss.fffffff") & ".schema"
                         If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "schema")) Then
                             IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "schema"))
                         End If
                         outputfile = IO.Path.Combine(Application.StartupPath, outputfile)
                         IO.File.WriteAllBytes(outputfile, data)
                         Form1.Invoke(Sub()
                                          Form1.TextBox1.Text = outputfile
                                          Form1.LoadSchemaFile()
                                      End Sub)
                     Catch ex As Exception

                     End Try
                     Invoke(Sub() Enabled = True)
                 End Sub)

    End Sub

    Private Sub ButtonDebugFormat_Click(sender As Object, e As EventArgs) Handles ButtonDebugFormat.Click
        If Not LoadComplete Then Exit Sub
        If MessageBox.Show(New Form With {.TopMost = True}, "Data will be cleared on this tape. Continue?", "Warning", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then Exit Sub
        Panel1.Enabled = False
        Dim dL As Char = ComboBoxDriveLetter.Text
        Dim barcode As String = TextBoxBarcode.Text
        Dim th As New Threading.Thread(
                Sub()
                    Invoke(Sub() TextBoxDebugOutput.Text = "Start format ..." & vbCrLf)
                    Try
                        'Load and Thread
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Loading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 1, 0}) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        Dim MaxExtraPartitionAllowed As Byte = TapeUtils.ModeSense(ConfTapeDrive, &H11)(2)
                        'Erase
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Initializing tape.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {4, 0, 0, 0, 0, 0}) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Mode Sense
                        Invoke(Sub() TextBoxDebugOutput.AppendText("MODE SENSE"))
                        Dim ModeData As Byte()
                        ModeData = TapeUtils.ModeSense(TapeDrive, &H11)
                        ReDim Preserve ModeData(11)
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"     Mode Data: {Byte2Hex(ModeData)}{vbCrLf}"))
                        'Mode Select:1st Partition to Minimum 
                        Invoke(Sub() TextBoxDebugOutput.AppendText("MODE SELECT - Partition mode page.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H15, &H10, 0, 0, &H10, 0}, {0, 0, &H10, 0, &H11, &HA, MaxExtraPartitionAllowed, 1, ModeData(4), ModeData(5), ModeData(6), ModeData(7), 0, 1, &HFF, &HFF}, 0) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Format
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Partitioning.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {4, 0, 1, 0, 0, 0}, Nothing, 0) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set Vendor
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"WRITE ATTRIBUTE: Vendor=OPEN.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H800, "OPEN".PadRight(8)) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set AppName
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"WRITE ATTRIBUTE: Application name = LTFSCopyGUI.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H801, "LTFSCopyGUI".PadRight(32)) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set Version
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"WRITE ATTRIBUTE: Application Version={My.Application.Info.Version.ToString(3)}.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H802, My.Application.Info.Version.ToString(3).PadRight(8)) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set TextLabel
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"WRITE ATTRIBUTE: TextLabel= .."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H803, "".PadRight(160), TapeUtils.AttributeFormat.Text) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set TLI
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"WRITE ATTRIBUTE: Localization Identifier = 0.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H805, {0}, TapeUtils.AttributeFormat.Binary) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set Barcode
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"WRITE ATTRIBUTE: Barcode={barcode}.."))
                        If TapeUtils.SetBarcode(ConfTapeDrive, barcode) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set Version
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"WRITE ATTRIBUTE: Format Version=2.4.0.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H80B, "2.4.0".PadRight(16)) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Mode Select:Block Length
                        Invoke(Sub() TextBoxDebugOutput.AppendText("MODE SELECT - Block size.."))
                        If TapeUtils.SetBlockSize(ConfTapeDrive, 524288).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Locate
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Locate to data partition.."))
                        If TapeUtils.Locate(ConfTapeDrive, 0, 1) = 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write VOL1Label
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write VOL1Label.."))
                        If TapeUtils.Write(ConfTapeDrive, New Vol1Label().GenerateRawData(barcode)).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        Dim plabel As New ltfslabel()
                        plabel.volumeuuid = Guid.NewGuid()
                        plabel.location.partition = ltfslabel.PartitionLabel.b
                        plabel.partitions.index = ltfslabel.PartitionLabel.a
                        plabel.partitions.data = ltfslabel.PartitionLabel.b
                        plabel.blocksize = 524288

                        'Write ltfslabel
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write ltfslabel.."))
                        If TapeUtils.Write(ConfTapeDrive, Encoding.UTF8.GetBytes(plabel.GetSerializedText())).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive, 2).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        Dim pindex As New ltfsindex
                        pindex.volumeuuid = plabel.volumeuuid
                        pindex.generationnumber = 1
                        pindex.creator = plabel.creator
                        pindex.updatetime = plabel.formattime
                        pindex.location.partition = ltfsindex.PartitionLabel.b
                        pindex.location.startblock = TapeUtils.ReadPosition(ConfTapeDrive).BlockNumber
                        pindex.previousgenerationlocation = Nothing
                        pindex.highestfileuid = 1
                        Dim block1 As ULong = pindex.location.startblock
                        pindex._directory = New List(Of ltfsindex.directory)
                        pindex._directory.Add(New ltfsindex.directory With {.name = barcode, .readonly = False,
                                              .creationtime = plabel.formattime, .changetime = .creationtime,
                                              .accesstime = .creationtime, .modifytime = .creationtime, .backuptime = .creationtime, .fileuid = 1, .contents = New ltfsindex.contentsDef()})

                        'Write ltfsindex
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write ltfsindex.."))
                        If TapeUtils.Write(ConfTapeDrive, Encoding.UTF8.GetBytes(pindex.GetSerializedText())).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Locate
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Locate to index partition.."))
                        If TapeUtils.Locate(ConfTapeDrive, 0, 0) = 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write VOL1Label
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write VOL1Label.."))
                        If TapeUtils.Write(ConfTapeDrive, New Vol1Label().GenerateRawData(barcode)).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write ltfslabel
                        plabel.location.partition = ltfslabel.PartitionLabel.a
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write ltfslabel.."))
                        If TapeUtils.Write(ConfTapeDrive, Encoding.UTF8.GetBytes(plabel.GetSerializedText())).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive, 2).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write ltfsindex
                        pindex.previousgenerationlocation = New ltfsindex.LocationDef()
                        pindex.previousgenerationlocation.partition = pindex.location.partition
                        pindex.previousgenerationlocation.startblock = pindex.location.startblock
                        pindex.location.partition = ltfsindex.PartitionLabel.a
                        pindex.location.startblock = TapeUtils.ReadPosition(ConfTapeDrive).BlockNumber
                        Dim block0 As ULong = pindex.location.startblock
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write ltfsindex.."))
                        If TapeUtils.Write(ConfTapeDrive, Encoding.UTF8.GetBytes(pindex.GetSerializedText())).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBoxDebugOutput.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive).Length > 0 Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Set DateTime
                        Dim CurrentTime As String = Now.ToUniversalTime.ToString("yyyyMMddhhmm")
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"WRITE ATTRIBUTE: Written time={CurrentTime}.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H804, CurrentTime.PadRight(12)) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Set VCI
                        Invoke(Sub() TextBoxDebugOutput.AppendText($"WRITE ATTRIBUTE: VCI.."))
                        If TapeUtils.WriteVCI(ConfTapeDrive, pindex.generationnumber, block0, block1, pindex.volumeuuid.ToString()) Then
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBoxDebugOutput.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        Invoke(Sub() TextBoxDebugOutput.AppendText("Format finished."))

                    Catch ex As Exception
                        Invoke(Sub()
                                   TextBoxDebugOutput.AppendText(ex.ToString & vbCrLf)
                                   TextBoxDebugOutput.AppendText("Format failed.")
                               End Sub)
                    End Try
                    Invoke(Sub() Panel1.Enabled = True)
                End Sub)
        th.Start()
        If Panel1.Enabled Then RefreshUI(CheckBoxAutoRefresh.Checked)
    End Sub

    Private Sub Button27_Click(sender As Object, e As EventArgs) Handles ButtonLTFSWriter.Click
        Dim appcmd As String = $"""{Application.ExecutablePath}"" -t {TapeDrive}"
        Try
            Process.Start(IO.Path.Combine(Application.StartupPath, "PsExec64.exe"), $"-accepteula -s -i -d {appcmd}")
        Catch ex As Exception
            Process.Start(New ProcessStartInfo With {.FileName = Application.ExecutablePath, .Arguments = $"-t {TapeDrive}"})
        End Try
        ' Dim LWF As New LTFSWriter With {.TapeDrive = TapeDrive}
        ' LWF.Show()
    End Sub

    Private Sub ButtonDebugReleaseUnit_Click(sender As Object, e As EventArgs) Handles ButtonDebugReleaseUnit.Click
        Panel1.Enabled = False
        Task.Run(Sub()
                     TapeUtils.ReleaseUnit(ConfTapeDrive,
                                                   Function(sense As Byte()) As Boolean
                                                       Invoke(Sub()
                                                                  TextBoxDebugOutput.Text = "RELEASE UNIT" & vbCrLf
                                                                  TextBoxDebugOutput.AppendText(TapeUtils.ParseSenseData(sense))
                                                              End Sub)
                                                       Return True
                                                   End Function)
                     Invoke(Sub() Panel1.Enabled = True)
                 End Sub)
    End Sub

    Private Sub ButtonDebugAllowMediaRemoval_Click(sender As Object, e As EventArgs) Handles ButtonDebugAllowMediaRemoval.Click
        Panel1.Enabled = False
        Task.Run(Sub()
                     TapeUtils.AllowMediaRemoval(ConfTapeDrive,
                              Function(sense As Byte()) As Boolean
                                  Invoke(Sub()
                                             TextBoxDebugOutput.Text = "ALLOW MEDIA REMOVAL" & vbCrLf
                                             TextBoxDebugOutput.AppendText(TapeUtils.ParseSenseData(sense))
                                         End Sub)
                                  Return True
                              End Function)
                     Invoke(Sub() Panel1.Enabled = True)
                 End Sub)
    End Sub

    Private Sub Button30_Click(sender As Object, e As EventArgs) Handles ButtonFileSorter.Click
        Form1.Show()
    End Sub

    Private Sub LTFSConfigurator_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.LTFSConf_AutoRefresh = CheckBoxAutoRefresh.Checked
        My.Settings.Save()
    End Sub

    Private Sub ButtonDebugDumpTape_MouseUp(sender As Object, e As MouseEventArgs) Handles ButtonDebugDumpTape.MouseUp
        If Not e.Button = MouseButtons.Right Then Exit Sub
        If MessageBox.Show(New Form With {.TopMost = True}, "Write will destroy everything after current position. Continue?", "Warning", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            If OpenFileDialog1.ShowDialog = DialogResult.OK Then
                Dim fname As String = OpenFileDialog1.FileName
                Dim th As New Threading.Thread(
                    Sub()
                        Dim fs As New IO.FileStream(fname, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                        If fs.Length = 0 Then
                            TapeUtils.WriteFileMark(ConfTapeDrive)
                            Invoke(Sub()
                                       TextBoxDebugOutput.Text = $"Filemark written."
                                       Panel1.Enabled = True
                                   End Sub)
                        Else
                            Invoke(Sub() TextBoxDebugOutput.Text = $"Writing: {fname}")
                            Dim buffer(Math.Min(NumericUpDownBlockLen.Value - 1, fs.Length - 1)) As Byte
                            While fs.Read(buffer, 0, buffer.Length) > 0
                                TapeUtils.Write(ConfTapeDrive, buffer)
                            End While
                            Invoke(Sub()
                                       TextBoxDebugOutput.Text = $"Write finished: {fname}"
                                       Panel1.Enabled = True
                                   End Sub)
                        End If
                        fs.Close()
                    End Sub)
                Panel1.Enabled = False
                th.Start()
            End If
        End If
    End Sub

    Private Sub CheckBox1_MouseUp(sender As Object, e As MouseEventArgs) Handles CheckBoxDebugPanel.MouseUp
        If e.Button = MouseButtons.Right Then
            ButtonStartFUSESvc.Visible = Not ButtonStartFUSESvc.Visible
            ButtonStopFUSESvc.Visible = Not ButtonStopFUSESvc.Visible
            ButtonRemount.Visible = Not ButtonRemount.Visible
            ButtonChangerTool.Visible = Not ButtonChangerTool.Visible
        End If
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles ButtonChangerTool.Click
        ChangerTool.Show()
    End Sub

    Private Sub Button11_Click(sender As Object, e As EventArgs) Handles ButtonResetLogPage.Click
        Panel1.Enabled = False
        Task.Run(Sub()
                     TextBoxDebugOutput.Clear()
                     Dim logdata As Byte()
                     Dim pdata As TapeUtils.PageData
#Region "0x00"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H0, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_SupportedLogPagesPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x00.xml"), pdata.GetSerializedText())
#End Region
#Region "0x02"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H2, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_WriteErrorCountersLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x02.xml"), pdata.GetSerializedText())
#End Region
#Region "0x03"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H3, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_ReadErrorCountersLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x03.xml"), pdata.GetSerializedText())
#End Region
#Region "0x0C"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &HC, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_SequentialAccessDeviceLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x0C.xml"), pdata.GetSerializedText())
#End Region
#Region "0x0D"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &HD, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_TemperatureLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x0D.xml"), pdata.GetSerializedText())
#End Region
#Region "0x11"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H11, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_DataTransferDeviceStatusLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x11.xml"), pdata.GetSerializedText())
#End Region
#Region "0x12"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H12, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_TapeAlertResponseLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x12.xml"), pdata.GetSerializedText())
#End Region
#Region "0x13"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H13, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_RequestedRecoveryLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x13.xml"), pdata.GetSerializedText())
#End Region
#Region "0x14"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H14, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_DeviceStatisticsLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x14.xml"), pdata.GetSerializedText())
#End Region
#Region "0x15"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H15, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_ServiceBuffersInformationLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x15.xml"), pdata.GetSerializedText())
#End Region
#Region "0x16"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H16, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_TapeDiagnosticLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x16.xml"), pdata.GetSerializedText())
#End Region
#Region "0x17"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H17, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_VolumeStatisticsLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x17.xml"), pdata.GetSerializedText())
#End Region
#Region "0x18"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H18, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_ProtocolSpecificPortLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x18.xml"), pdata.GetSerializedText())
#End Region
#Region "0x1B"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H1B, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_DataCompressionLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x1B.xml"), pdata.GetSerializedText())
#End Region
#Region "0x2E"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H2E, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_TapeAlertLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x2E.xml"), pdata.GetSerializedText())
#End Region
#Region "0x30"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H30, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_TapeUsageLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x30.xml"), pdata.GetSerializedText())
#End Region
#Region "0x31"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H31, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_TapeCapacityLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x31.xml"), pdata.GetSerializedText())
#End Region
#Region "0x32"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H32, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_DataCompressionHPLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x32.xml"), pdata.GetSerializedText())
#End Region
#Region "0x33"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H33, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_DeviceWellnessLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x33.xml"), pdata.GetSerializedText())
#End Region
#Region "0x34"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H34, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_PerformanceDataLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x34.xml"), pdata.GetSerializedText())
#End Region
#Region "0x35"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H35, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_DTDeviceErrorLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x35.xml"), pdata.GetSerializedText())
#End Region
#Region "0x3E"
                     logdata = TapeUtils.LogSense(ConfTapeDrive, &H3E, PageControl:=1)
                     pdata = TapeUtils.PageData.CreateDefault(TapeUtils.PageData.DefaultPages.HPLTO6_DeviceStatusLogPage, logdata)
                     Invoke(Sub() TextBoxDebugOutput.AppendText(pdata.GetSummary())）
                     If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
                     IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x3E.xml"), pdata.GetSerializedText())
#End Region
                     Invoke(Sub() Panel1.Enabled = True)
                 End Sub)


    End Sub
    Public PageItem As New List(Of TapeUtils.PageData)
    Private Sub TabControl1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles TabControl1.SelectedIndexChanged
        If TabControl1.SelectedTab Is TabPageLog Then
            If IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then
                ComboBox4.Items.Clear()
                PageItem.Clear()
                For Each f As IO.FileInfo In New IO.DirectoryInfo(IO.Path.Combine(Application.StartupPath, "logpages")).GetFiles
                    Try
                        Dim pdata As TapeUtils.PageData = TapeUtils.PageData.FromXML(IO.File.ReadAllText(f.FullName))
                        PageItem.Add(pdata)
                        ComboBox4.Items.Add($"0x{Hex(pdata.PageCode).PadLeft(2, "0")} - {pdata.Name}")
                        ComboBox5.SelectedIndex = 1
                    Catch ex As Exception

                    End Try
                Next
            End If
        End If
    End Sub

    Private Sub Button12_Click(sender As Object, e As EventArgs) Handles ButtonRunLogSense.Click
        If ComboBox4.SelectedIndex >= 0 Then
            Dim index As Integer = ComboBox4.SelectedIndex
            Dim pc As Integer = ComboBox5.SelectedIndex
            Panel1.Enabled = False
            Task.Run(Sub()
                         Dim logdata As Byte() = TapeUtils.LogSense(TapeDrive:=ConfTapeDrive, PageCode:=PageItem(index).PageCode, PageControl:=pc)
                         Invoke(Sub()
                                    PageItem(index).RawData = logdata
                                    TextBoxDebugOutput.Text = PageItem(index).GetSummary()
                                    If CheckBoxShowRawLogPageData.Checked Then
                                        TextBoxDebugOutput.AppendText("Raw Data".PadLeft(41, "=").PadRight(75, "="))
                                        TextBoxDebugOutput.AppendText(vbCrLf)
                                        TextBoxDebugOutput.AppendText(Byte2Hex(PageItem(index).RawData, True))
                                    End If
                                    Panel1.Enabled = True
                                End Sub)

                     End Sub)
        End If
    End Sub
    Public TestEnabled As Boolean
    Public blist As List(Of Byte())
    Private Sub ButtonTest_Click(sender As Object, e As EventArgs) Handles ButtonTest.Click
        If TestEnabled Then
            ButtonTest.Enabled = False
            TestEnabled = False
            Exit Sub
        End If
        Dim progmax As Long
        If MessageBox.Show(New Form With {.TopMost = True}, "Write will destroy everything after current position. Continue?", "Warning", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            TestEnabled = True
            Dim progval As Long = 0
            Dim info As String = ""
            Dim running As Boolean = True
            Dim randomNum As Boolean = RadioButtonTest1.Checked
            Dim blkLen As Integer = NumericUpDownTestBlkSize.Value
            Dim blkNum As Long = NumericUpDownTestBlkNum.Value
            progmax = blkNum * blkLen
            Dim sec As Integer = -1
            Dim SenseMsg As String = ""
            Dim th As New Threading.Thread(
            Sub()
                Dim r As New Random()
                Dim b(blkLen - 1) As Byte
                If blist Is Nothing Then
                    blist = New List(Of Byte())(1000)
                    For i As Integer = 0 To 999
                        If randomNum Then
                            r.NextBytes(b)
                        End If
                        blist.Add(b.Clone())
                    Next
                End If

                Invoke(Sub() TextBoxDebugOutput.AppendText($"Start{vbCrLf}"))
                sec = 0
                Dim handle As IntPtr
                Dim bH(7) As Byte
                If Not TapeUtils.OpenTapeDrive(ConfTapeDrive, handle) Then MessageBox.Show(New Form With {.TopMost = True}, "False")
                    If blkLen = 0 Then
                        For i As Long = 0 To blkNum
                            If Not TestEnabled Then Exit For
                            TapeUtils.WriteFileMark(handle)
                            progval = i * blkLen
                        Next
                    Else
                        Dim LastC1Err(31) As Integer, LastNoCCPs(31) As Integer
                    For i As Long = 0 To blkNum
                        If Not TestEnabled Then Exit For
                        Dim sense As Byte() = TapeUtils.Write(handle, blist(i Mod 1000), blkLen)
                        If ((sense(2) >> 6) And &H1) = 1 Then
                            If (sense(2) And &HF) = 13 Then
                                Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, My.Resources.ResText_VOF))
                                Exit For
                            Else
                                SenseMsg = My.Resources.ResText_EWEOM
                            End If
                        ElseIf sense(2) And &HF <> 0 Then
                            SenseMsg = TapeUtils.ParseSenseData(sense)
                            Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErr}{vbCrLf}{TapeUtils.ParseSenseData(sense)}{vbCrLf}{vbCrLf}sense{vbCrLf}{TapeUtils.Byte2Hex(sense, True)}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                                Case DialogResult.Abort
                                    Exit For
                                Case DialogResult.Retry
                                    i -= 1
                                    Continue For
                                Case DialogResult.Ignore
                                    Continue For
                            End Select
                        Else
                            SenseMsg = ""
                        End If
                        If i Mod 200 = 0 Then
                            Dim result As New StringBuilder
                            Dim WERLHeader As Byte()
                            Dim WERLPage As Byte()
                            Dim WERLPageLen As Integer
                            SyncLock TapeUtils.SCSIOperationLock
                                WERLHeader = TapeUtils.SCSIReadParam(handle, {&H1C, &H1, &H88, &H0, &H4, &H0}, 4)
                                If WERLHeader.Length <> 4 Then Exit Sub
                                WERLPageLen = WERLHeader(2)
                                WERLPageLen <<= 8
                                WERLPageLen = WERLPageLen Or WERLHeader(3)
                                If WERLPageLen = 0 Then Exit Sub
                                WERLPageLen += 4
                                WERLPage = TapeUtils.SCSIReadParam(handle:=handle, cdbData:={&H1C, &H1, &H88, (WERLPageLen >> 8) And &HFF, WERLPageLen And &HFF, &H0}, paramLen:=WERLPageLen)
                            End SyncLock
                            Dim WERLData As String() = System.Text.Encoding.ASCII.GetString(WERLPage, 4, WERLPage.Length - 4).Split({vbCr, vbLf, vbTab}, StringSplitOptions.RemoveEmptyEntries)
                            info = ""
                            Try
                                For ch As Integer = 4 To WERLData.Length - 5 Step 5
                                    Dim chan As Integer = (ch - 4) \ 5
                                    Dim C1err As Integer = Integer.Parse(WERLData(ch + 0), Globalization.NumberStyles.HexNumber)
                                    Dim NoCCPs As Integer = Integer.Parse(WERLData(ch + 4), Globalization.NumberStyles.HexNumber)

                                    If NoCCPs - LastNoCCPs(chan) > 0 Then
                                        result.Append(Math.Round(Math.Log10((C1err - LastC1Err(chan)) / (NoCCPs - LastNoCCPs(chan)) / 2 / 1920), 2).ToString("f2").PadLeft(6).PadRight(7))
                                        LastC1Err(chan) = C1err
                                        LastNoCCPs(chan) = NoCCPs
                                    End If
                                Next
                            Catch
                            End Try
                            info = result.ToString()
                        End If
                        progval = i * blkLen
                    Next
                End If
                TapeUtils.Flush(handle)
                TapeUtils.CloseTapeDrive(handle)


                running = False
                TestEnabled = False
                Invoke(Sub()
                           ButtonTest.Enabled = True
                           ButtonTest.Text = "Start"
                       End Sub)
            End Sub)
            Dim thprog As New Threading.Thread(
            Sub()
                Dim lastval As Long = 0
                Dim len1 As Integer = progmax.ToString().Length
                While running
                    Threading.Thread.Sleep(1000)
                    Dim prognow As Long = progval
                    Invoke(Sub()
                               TextBoxDebugOutput.AppendText($"{sec}: {prognow.ToString().PadLeft(len1)} (+{IOManager.FormatSize(prognow - lastval).PadLeft(10)}){info} {SenseMsg}{vbCrLf}")
                           End Sub)
                    If sec >= 0 Then sec += 1
                    lastval = prognow
                End While
                Invoke(Sub()
                           TextBoxDebugOutput.AppendText($"{sec}: {progval.ToString().PadLeft(len1)} (+{IOManager.FormatSize(progval - lastval).PadLeft(10)}){info} {SenseMsg}{vbCrLf}")
                           TextBoxDebugOutput.AppendText($"End")
                       End Sub)
            End Sub)
            TextBoxDebugOutput.Clear()
            TextBoxDebugOutput.AppendText($"Preparing... {vbCrLf}")
            thprog.Start()
            ButtonTest.Text = "Stop"
            th.Start()
        End If

    End Sub

    Private Sub Button15_Click(sender As Object, e As EventArgs) Handles ButtonRDErrRateLog.Click
        Panel1.Enabled = False
        Task.Run(Sub()
                     Dim result As New StringBuilder
                     Dim WERLHeader As Byte()
                     Dim RERLHeader As Byte()
                     Dim WERLPage As Byte()
                     Dim RERLPage As Byte()
                     SyncLock TapeUtils.SCSIOperationLock
                         WERLHeader = TapeUtils.SCSIReadParam(ConfTapeDrive, {&H1C, &H1, &H88, &H0, &H4, &H0}, 4)
                         If WERLHeader.Length <> 4 Then Exit Sub
                         RERLHeader = TapeUtils.SCSIReadParam(ConfTapeDrive, {&H1C, &H1, &H87, &H0, &H4, &H0}, 4)
                         If RERLHeader.Length <> 4 Then Exit Sub
                         Dim WERLPageLen As Integer = WERLHeader(2)
                         WERLPageLen <<= 8
                         WERLPageLen = WERLPageLen Or WERLHeader(3)
                         If WERLPageLen = 0 Then Exit Sub
                         WERLPageLen += 4
                         WERLPage = TapeUtils.SCSIReadParam(TapeDrive:=ConfTapeDrive, cdbData:={&H1C, &H1, &H88, (WERLPageLen >> 8) And &HFF, WERLPageLen And &HFF, &H0}, paramLen:=WERLPageLen)

                         Dim RERLPageLen As Integer = RERLHeader(2)
                         RERLPageLen <<= 8
                         RERLPageLen = RERLPageLen Or RERLHeader(3)
                         If RERLPageLen = 0 Then Exit Sub
                         RERLPageLen += 4
                         RERLPage = TapeUtils.SCSIReadParam(TapeDrive:=ConfTapeDrive, cdbData:={&H1C, &H1, &H87, (RERLPageLen >> 8) And &HFF, RERLPageLen And &HFF, &H0}, paramLen:=RERLPageLen)
                     End SyncLock
                     Dim WERLData As String() = System.Text.Encoding.ASCII.GetString(WERLPage, 4, WERLPage.Length - 4).Split({vbCr, vbLf, vbTab}, StringSplitOptions.RemoveEmptyEntries)
                     Dim RERLData As String() = System.Text.Encoding.ASCII.GetString(RERLPage, 4, RERLPage.Length - 4).Split({vbCr, vbLf, vbTab}, StringSplitOptions.RemoveEmptyEntries)
                     Try
                         result.AppendLine($"Write Error Rate Log")
                         result.AppendLine($"  Datasets Written     : {WERLData(0)}")
                         result.AppendLine($"  CWI-4 Sets Written   : {WERLData(1)}")
                         result.AppendLine($"  CWI-4 Set Retries    : {WERLData(2)}")
                         result.AppendLine($"  Unwritable Datasets  : {WERLData(3)}")
                         result.AppendLine($"  =========+==========+==========+===============+==========+============+=============+=============+==================")
                         result.AppendLine($"   Channel | No. CCPs | C1 code  |  C1 codeword  |  Header  | Write Pass | C1 codeword |  Bit Error  |    Block per     ")
                         result.AppendLine($"           |          |  error   | uncorrectable |  error   |   error    | error rate  | rate(log10) | Uncorrectable C1 ")
                         result.AppendLine($"  ---------+----------+----------+---------------+----------+------------+-------------+-------------+------------------")
                         For i As Integer = 4 To WERLData.Length - 5 Step 5
                             Dim chan As Integer = (i - 4) \ 5
                             Dim C1err As Integer = Integer.Parse(WERLData(i + 0), Globalization.NumberStyles.HexNumber)
                             Dim C1cwerr As Integer = Integer.Parse(WERLData(i + 1), Globalization.NumberStyles.HexNumber)
                             Dim Headerrr As Integer = Integer.Parse(WERLData(i + 2), Globalization.NumberStyles.HexNumber)
                             Dim WrPasserr As Integer = Integer.Parse(WERLData(i + 3), Globalization.NumberStyles.HexNumber)
                             Dim NoCCPs As Integer = Integer.Parse(WERLData(i + 4), Globalization.NumberStyles.HexNumber)
                             result.Append("   ")
                             result.Append(chan.ToString.PadLeft(5))
                             result.Append("   | ")
                             result.Append(WERLData(i + 4).PadLeft(8, "0"))
                             result.Append(" | ")
                             result.Append(WERLData(i + 0).PadLeft(8, "0"))
                             result.Append(" |   ")
                             result.Append(WERLData(i + 1).PadLeft(8, "0"))
                             result.Append("    | ")
                             result.Append(WERLData(i + 2).PadLeft(8, "0"))
                             result.Append(" |  ")
                             result.Append(WERLData(i + 3).PadLeft(8, "0"))
                             result.Append("  |  ")
                             If NoCCPs > 0 Then
                                 result.Append((C1err / NoCCPs / 2).ToString("E3"))
                             Else
                                 result.Append("          ")
                             End If
                             result.Append(" |    ")
                             If NoCCPs > 0 Then
                                 result.Append(Math.Round(Math.Log10(C1err / NoCCPs / 2 / 1920), 2).ToString("f2").PadRight(4))
                             Else
                                 result.Append("     ")
                             End If
                             result.Append("    |")
                             If C1cwerr > 0 Then
                                 result.AppendLine(Math.Round(NoCCPs * 2 / C1cwerr, 1).ToString("f1").PadLeft(11).PadRight(18))
                             Else
                                 result.AppendLine("".PadLeft(11).PadRight(18))
                             End If
                         Next
                         result.AppendLine($"  =========+==========+==========+===============+==========+============+=============+=============+==================")
                         result.AppendLine()
                         result.AppendLine($"Read Error Rate Log")
                         result.AppendLine($"  Datasets Read        : {RERLData(0)}")
                         result.AppendLine($"  Subdataset C2 Errors : {RERLData(2)}")
                         result.AppendLine($"  Dataset C2 Errors    : {RERLData(3)}")
                         result.AppendLine($"  X-Chan Interpolations: {RERLData(4)}")
                         result.AppendLine($"  =========+==========+==========+===============+==========+============+=============+=============+==================")
                         result.AppendLine($"   Channel | No. CCPs | C1 code  |  C1 codeword  |  Header  | Write Pass | C1 codeword |  Bit Error  |    Block per     ")
                         result.AppendLine($"           |          |  error   | uncorrectable |  error   |   error    | error rate  | rate(log10) | Uncorrectable C1 ")
                         result.AppendLine($"  ---------+----------+----------+---------------+----------+------------+-------------+-------------+------------------")
                         For i As Integer = 5 To RERLData.Length - 5 Step 5
                             Dim chan As Integer = (i - 4) \ 5
                             Dim C1err As Integer = Integer.Parse(RERLData(i + 0), Globalization.NumberStyles.HexNumber)
                             Dim C1cwerr As Integer = Integer.Parse(RERLData(i + 1), Globalization.NumberStyles.HexNumber)
                             Dim Headerrr As Integer = Integer.Parse(RERLData(i + 2), Globalization.NumberStyles.HexNumber)
                             Dim WrPasserr As Integer = Integer.Parse(RERLData(i + 3), Globalization.NumberStyles.HexNumber)
                             Dim NoCCPs As Integer = Integer.Parse(RERLData(i + 4), Globalization.NumberStyles.HexNumber)
                             result.Append("   ")
                             result.Append(chan.ToString.PadLeft(5))
                             result.Append("   | ")
                             result.Append(RERLData(i + 4).PadLeft(8, "0"))
                             result.Append(" | ")
                             result.Append(RERLData(i + 0).PadLeft(8, "0"))
                             result.Append(" |   ")
                             result.Append(RERLData(i + 1).PadLeft(8, "0"))
                             result.Append("    | ")
                             result.Append(RERLData(i + 2).PadLeft(8, "0"))
                             result.Append(" |  ")
                             result.Append(RERLData(i + 3).PadLeft(8, "0"))
                             result.Append("  |  ")
                             If NoCCPs > 0 Then
                                 result.Append((C1err / NoCCPs / 2).ToString("E3"))
                             Else
                                 result.Append("          ")
                             End If
                             result.Append(" |    ")
                             If NoCCPs > 0 Then
                                 result.Append(Math.Round(Math.Log10(C1err / NoCCPs / 2 / 1920), 2).ToString("f2").PadRight(4))
                             Else
                                 result.Append("     ")
                             End If
                             result.Append("    |")
                             If C1cwerr > 0 Then
                                 result.AppendLine(Math.Round(NoCCPs * 2 / C1cwerr, 1).ToString("f1").PadLeft(11).PadRight(18))
                             Else
                                 result.AppendLine("".PadLeft(11).PadRight(18))
                             End If
                         Next
                         result.AppendLine($"  =========+==========+==========+===============+==========+============+=============+=============+==================")
                     Catch ex As Exception
                         result.Append(ex.ToString())
                     End Try
                     Invoke(Sub()
                                TextBoxDebugOutput.Text = result.ToString()
                                Panel1.Enabled = True
                            End Sub)
                 End Sub)

    End Sub

    Private Sub 在当前进程运行ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 在当前进程运行ToolStripMenuItem.Click
        Dim LWF As New LTFSWriter With {.TapeDrive = ConfTapeDrive}
        LWF.Show()
    End Sub

    Private Sub 不读取索引ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 不读取索引ToolStripMenuItem.Click
        Dim LWF As New LTFSWriter With {.TapeDrive = TapeDrive, .OfflineMode = True}
        LWF.Show()
    End Sub

    Private Sub BrowseBinaryFileToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles BrowseBinaryFileToolStripMenuItem.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            Me.Enabled = False
            Dim CMInfo As TapeUtils.CMParser = Nothing
            Try
                CMInfo = New TapeUtils.CMParser() With {.a_CMBuffer = IO.File.ReadAllBytes(OpenFileDialog1.FileName)}
                CMInfo.RunParse()
            Catch ex As Exception
                TextBoxDebugOutput.AppendText("CM Data Parsing Failed." & vbCrLf)
            End Try
            TextBoxDebugOutput.Text = ""
            Try
                TextBoxDebugOutput.AppendText(CMInfo.GetReport())
                If CheckBoxParseCMData.Checked AndAlso CMInfo IsNot Nothing Then
                    TextBoxDebugOutput.AppendText(CMInfo.GetSerializedText())
                    Dim PG1 As New SettingPanel
                    PG1.SelectedObject = CMInfo
                    PG1.Text = CMInfo.CartridgeMfgData.CartridgeSN
                    PG1.Show()
                    TextBoxDebugOutput.AppendText(vbCrLf)
                End If
            Catch ex As Exception
                TextBoxDebugOutput.AppendText("| CM data parsing failed.".PadRight(74) & "|" & vbCrLf)
            End Try
            TextBoxDebugOutput.Select(0, 0)
            TextBoxDebugOutput.ScrollToCaret()
            Me.Enabled = True
        End If
    End Sub

    Private Sub ReadThroughDiagnosticCommandToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ReadThroughDiagnosticCommandToolStripMenuItem.Click
        Me.Enabled = False
        TextBoxDebugOutput.Text = ""
        Task.Run(Sub()
                     Dim CMInfo As TapeUtils.CMParser = Nothing
                     Try
                         Dim errormsg As Exception = Nothing
                         CMInfo = New TapeUtils.CMParser(TapeUtils.ReceiveDiagCM(TapeDrive), errormsg)
                         If errormsg IsNot Nothing Then Throw errormsg
                     Catch ex As Exception
                         Invoke(Sub() TextBoxDebugOutput.AppendText("CM Data Parsing Failed." & vbCrLf & ex.ToString & vbCrLf))
                     End Try
                     Invoke(
                     Sub()
                         Try
                             TextBoxDebugOutput.AppendText(CMInfo.GetReport())
                         Catch ex As Exception
                             TextBoxDebugOutput.AppendText("Report generation failed.".PadRight(74) & vbCrLf & ex.ToString & vbCrLf)
                         End Try
                         Try
                             If CheckBoxParseCMData.Checked AndAlso CMInfo IsNot Nothing Then
                                 TextBoxDebugOutput.AppendText(CMInfo.GetSerializedText())
                                 Dim PG1 As New SettingPanel
                                 PG1.SelectedObject = CMInfo
                                 PG1.Text = CMInfo.CartridgeMfgData.CartridgeSN
                                 PG1.Show()
                                 TextBoxDebugOutput.AppendText(vbCrLf)
                             End If
                         Catch ex As Exception
                             TextBoxDebugOutput.AppendText("CM Data Parsing failed.".PadRight(74) & vbCrLf & ex.ToString & vbCrLf)
                         End Try
                         TextBoxDebugOutput.Select(0, 0)
                         TextBoxDebugOutput.ScrollToCaret()
                         If IO.Directory.Exists(My.Application.Info.DirectoryPath & "\Info") Then
                             Dim fn As String
                             Try
                                 fn = CMInfo.ApplicationSpecificData.Barcode
                                 If fn Is Nothing OrElse fn.Length = 0 Then fn = CMInfo.CartridgeMfgData.CartridgeSN
                                 If fn Is Nothing Then fn = ""
                                 IO.File.WriteAllText($"{My.Application.Info.DirectoryPath}\Info\{fn}.txt", TextBoxDebugOutput.Text)
                             Catch ex As Exception

                             End Try
                         End If
                         Me.Enabled = True
                     End Sub)
                 End Sub)
    End Sub

    Private Sub DiskToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DiskToolStripMenuItem.Click
        LoadComplete = False
        CheckBoxAutoRefresh.Checked = False
        ListBox1.Items.Clear()
        Dim DevList As List(Of TapeUtils.TapeDrive)
        LastDeviceList = TapeUtils.GetDiskDriveList()
        DevList = LastDeviceList
        For Each D As TapeUtils.TapeDrive In DevList
            D.DeviceType = "PhysicalDrive"
            ListBox1.Items.Add(D.ToString())
        Next
        ListBox1.SelectedIndex = Math.Min(SelectedIndex, ListBox1.Items.Count - 1)
        Dim t As String = ComboBoxDriveLetter.Text
        ComboBoxDriveLetter.Items.Clear()
        ComboBoxDriveLetter.Text = ""
        For Each s As String In AvailableDriveLetters
            ComboBoxDriveLetter.Items.Add(s)
        Next
        If ComboBoxDriveLetter.Items.Count > 0 Then
            If Not ComboBoxDriveLetter.Items.Contains(t) Then
                ComboBoxDriveLetter.SelectedIndex = 0
            Else
                ComboBoxDriveLetter.Text = t
            End If
        End If

        LoadComplete = True
        SelectedIndex = ListBox1.SelectedIndex
    End Sub

    Private Sub LTFSConfigurator_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.KeyCode.F12
                Dim SP1 As New SettingPanel
                SP1.Text = Text
                SP1.SelectedObject = Me
                SP1.Show()
        End Select
    End Sub
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

    Private Sub RadioButtonTest1_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButtonTest1.CheckedChanged
        blist = Nothing
    End Sub
End Class