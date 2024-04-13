Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Text

Public Class LTFSConfigurator
    Private LoadComplete As Boolean = False
    Private _SelectedIndex As Integer
    Public Function GetCurDrive() As TapeUtils.TapeDrive
        Dim dlist As List(Of TapeUtils.TapeDrive)
        If CheckBox3.Checked OrElse LastDeviceList Is Nothing Then
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
                result = "\\.\TAPE" & GetCurDrive.DevIndex
            Catch ex As Exception
                If TextBox5.Text <> "" Then
                    result = TextBox5.Text
                Else
                    result = "\\.\TAPE0"
                End If

            End Try
            Return result
        End Get
    End Property
    Public Property ConfTapeDrive As String
        Get
            Return TextBox5.Text
        End Get
        Set(value As String)
            TextBox5.Text = value
        End Set
    End Property

    Public Property SelectedIndex As Integer
        Set(value As Integer)
            _SelectedIndex = Math.Max(0, value)
            If Not LoadComplete Then Exit Property
            Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
            If CurDrive Is Nothing Then
                Button6.Enabled = False
                Button27.Enabled = False
                Button7.Enabled = False
                Button8.Enabled = False
                Button9.Enabled = False
                Button10.Enabled = False
                'Button27.Enabled = False
                Exit Property
            End If
            TextBox1.Text = CurDrive.ToString()
            If CurDrive.DriveLetter <> "" Then
                If Not ComboBox1.Items.Contains(CurDrive.DriveLetter) Then ComboBox1.Items.Add(CurDrive.DriveLetter)
                ComboBox1.SelectedItem = CurDrive.DriveLetter
                TextBox2.AppendText(TapeUtils.CheckTapeMedia(CurDrive.DriveLetter) & vbCrLf)
            End If
            ComboBox1.Enabled = (CurDrive.DriveLetter = "")
            Button6.Enabled = (CurDrive.DriveLetter = "")
            Button27.Enabled = (CurDrive.DriveLetter = "")
            Button7.Enabled = (CurDrive.DriveLetter <> "")
            TextBox5.Text = TapeDrive
            Button8.Enabled = True
            Button9.Enabled = True
            Button10.Enabled = (CurDrive.DriveLetter <> "")
            Button27.Enabled = True
        End Set
        Get
            Return _SelectedIndex
        End Get
    End Property
    Public Property LastDeviceList As List(Of TapeUtils.TapeDrive)
    Public ReadOnly Property DeviceList As List(Of TapeUtils.TapeDrive)
        Get
            LastDeviceList = TapeUtils.GetTapeDriveList()
            Return LastDeviceList
        End Get
    End Property
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
    Public Sub RefreshUI(Optional RefreshDevList As Boolean = True)
        LoadComplete = False
        ListBox1.Items.Clear()
        Dim DevList As List(Of TapeUtils.TapeDrive)
        If RefreshDevList OrElse LastDeviceList Is Nothing Then DevList = DeviceList Else DevList = LastDeviceList
        For Each D As TapeUtils.TapeDrive In DevList
            ListBox1.Items.Add(D.ToString())
        Next
        ListBox1.SelectedIndex = Math.Min(SelectedIndex, ListBox1.Items.Count - 1)
        Dim t As String = ComboBox1.Text
        ComboBox1.Items.Clear()
        ComboBox1.Text = ""
        For Each s As String In AvailableDriveLetters
            ComboBox1.Items.Add(s)
        Next
        If ComboBox1.Items.Count > 0 Then
            If Not ComboBox1.Items.Contains(t) Then
                ComboBox1.SelectedIndex = 0
            Else
                ComboBox1.Text = t
            End If
        End If

        LoadComplete = True
        SelectedIndex = ListBox1.SelectedIndex
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        RefreshUI()
    End Sub

    Private Sub LTFSConfigurator_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Not New Security.Principal.WindowsPrincipal(Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(Security.Principal.WindowsBuiltInRole.Administrator) Then
            Process.Start(New ProcessStartInfo With {.FileName = Application.ExecutablePath, .Verb = "runas", .Arguments = "-c"})
            Me.Close()
            Exit Sub
        End If
        RefreshUI()
        CheckBox3.Checked = My.Settings.LTFSConf_AutoRefresh
        ComboBox2.SelectedIndex = 3
        ComboBox3.SelectedIndex = 0
        Text = $"LTFSConfigurator - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}{My.Settings.License}"
        LoadComplete = True
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim s As String = TapeUtils.StartLtfsService()
        If s = "" Then s = "OK"
        MessageBox.Show(s)
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Dim s As String = TapeUtils.StopLtfsService()
        If s = "" Then s = "OK"
        MessageBox.Show(s)
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Dim s As String = TapeUtils.RemapTapeDrives()
        If s = "" Then s = "OK"
        MessageBox.Show(s)
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged
        If Not LoadComplete Then Exit Sub
        _SelectedIndex = ListBox1.SelectedIndex
        RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter = "" And ComboBox1.Text <> "" Then
                Dim result As String = TapeUtils.MapTapeDrive(ComboBox1.Text, "TAPE" & CurDrive.DevIndex)
                If result = "" Then result = "TAPE" & CurDrive.DevIndex & " <=> " & ComboBox1.Text & ":"
                result &= vbCrLf
                TextBox2.AppendText(result)
            End If
        End If
        RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter <> "" Then
                Dim result As String = TapeUtils.UnMapTapeDrive(ComboBox1.Text)
                If result = "" Then result = "TAPE" & CurDrive.DevIndex & " <=> ---" & ComboBox1.Text
                result &= vbCrLf
                TextBox2.AppendText(result)
            End If
        End If
        RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBox1.Text
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
                               If result = "" Then result = "TAPE" & CurDrive.DevIndex & " loaded"
                               result &= vbCrLf
                               TextBox2.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBox3.Checked)
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI(CheckBox3.Checked)

    End Sub

    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBox1.Text
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
                               If result = "" Then result = "TAPE" & CurDrive.DevIndex & " ejected"
                               result &= vbCrLf
                               TextBox2.AppendText(result)
                               Panel1.Enabled = True
                               RefreshUI(CheckBox3.Checked)
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter <> "" And ComboBox1.Text <> "" Then
                Dim result As String = TapeUtils.MountTapeDrive(ComboBox1.Text)
                If result = "" Then result = "TAPE" & CurDrive.DevIndex & " mounted"
                result &= vbCrLf
                TextBox2.AppendText(result)
            End If
        End If
        RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        Panel2.Visible = CheckBox1.Checked
        Button27.Enabled = True
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
                    Dim cdbData() As Byte = HexStringToByteArray(TextBox6.Text)
                    Dim dataData() As Byte = {}
                    Dim cdb As IntPtr = Marshal.AllocHGlobal(cdbData.Length)
                    Marshal.Copy(cdbData, 0, cdb, cdbData.Length)
                    Dim dataBufferPtr As IntPtr
                    If TextBox7.Text.Length >= 2 Then
                        dataData = HexStringToByteArray(TextBox7.Text)
                        dataBufferPtr = Marshal.AllocHGlobal(dataData.Length)
                        Marshal.Copy(dataData, 0, dataBufferPtr, dataData.Length)
                    Else
                        dataBufferPtr = Marshal.AllocHGlobal(64)
                    End If
                    Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(64)

                    Dim senseBuffer(63) As Byte
                    Marshal.Copy(senseBuffer, 0, senseBufferPtr, 64)
                    Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFullC(ConfTapeDrive, cdb, cdbData.Length, dataBufferPtr, dataData.Length, TextBox10.Text, CInt(TextBox3.Text), senseBufferPtr)
                    Marshal.Copy(dataBufferPtr, dataData, 0, dataData.Length)
                    Marshal.Copy(senseBufferPtr, senseBuffer, 0, senseBuffer.Length)
                    Me.Invoke(Sub()
                                  TextBox8.Text = "DataBuffer" & vbCrLf
                                  TextBox8.Text &= Byte2Hex(dataData, True)
                                  TextBox8.Text &= vbCrLf & vbCrLf & "SenseBuffer" & vbCrLf
                                  TextBox8.Text &= Byte2Hex(senseBuffer) & vbCrLf
                                  TextBox8.Text &= TapeUtils.ParseSenseData(senseBuffer) & vbCrLf
                              End Sub)
                    'Marshal.Copy(senseBufferPtr, senseBuffer, 0, 127)
                    Marshal.FreeHGlobal(cdb)
                    Marshal.FreeHGlobal(dataBufferPtr)
                    Marshal.FreeHGlobal(senseBufferPtr)
                    If succ Then
                        Me.Invoke(Sub() TextBox8.Text &= vbCrLf & "OK")
                    Else
                        Me.Invoke(Sub() TextBox8.Text &= vbCrLf & "FAIL")
                    End If
                Catch ex As Exception
                    MessageBox.Show(ex.ToString)
                End Try
                Me.Invoke(Sub() Panel2.Enabled = True)
            End Sub)
        Panel2.Enabled = False
        th.Start()

    End Sub

    Private Sub Button13_Click(sender As Object, e As EventArgs) Handles Button13.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBox1.Text
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
                               If result = "" Then result = "TAPE" & CurDrive.DevIndex & " loaded (unthread)"
                               result &= vbCrLf
                               TextBox2.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBox3.Checked)
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub Button14_Click(sender As Object, e As EventArgs) Handles Button14.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBox1.Text
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
                               If result = "" Then result = "TAPE" & CurDrive.DevIndex & " unthreaded"
                               result &= vbCrLf
                               TextBox2.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBox3.Checked)
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub ButtonDebugErase_Click(sender As Object, e As EventArgs) Handles ButtonDebugErase.Click
        If Not LoadComplete Then Exit Sub
        Panel1.Enabled = False
        Dim dL As Char = ComboBox1.Text
        Dim th As New Threading.Thread(
                Sub()
                    Invoke(Sub() TextBox8.Text = "Start erase ..." & vbCrLf)
                    Try
                        'result = TapeUtils.LoadTapeDrive(dL, True)

                        'Load and Thread
                        Invoke(Sub() TextBox8.AppendText("Loading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 1, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Mode Sense
                        Invoke(Sub() TextBox8.AppendText("MODE SENSE"))
                        Dim ModeData As Byte()
                        ModeData = TapeUtils.ModeSense(TapeDrive, &H11)
                        Invoke(Sub() TextBox8.AppendText($"     Mode Data: {Byte2Hex(ModeData)}{vbCrLf}"))
                        ReDim Preserve ModeData(11)
                        'Mode Select:1st Partition to Minimum 
                        Invoke(Sub() TextBox8.AppendText("MODE SELECT - Partition mode page.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H15, &H10, 0, 0, &H10, 0}, {0, 0, &H10, 0, &H11, &HA, ModeData(2), 1, ModeData(4), ModeData(5), ModeData(6), ModeData(7), 0, 1, &HFF, &HFF}, 0) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Format
                        Invoke(Sub() TextBox8.AppendText("Partitioning.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {4, 0, 1, 0, 0, 0}, Nothing, 0) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        For i As Integer = 1 To NumericUpDown6.Value
                            'Unthread
                            Invoke(Sub() TextBox8.AppendText("Unthreading.."))
                            If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, &HA, 0}) Then
                                Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                            Else
                                Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                                Exit Try
                            End If
                            'Thread
                            Invoke(Sub() TextBox8.AppendText("Threading.."))
                            If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 1, 0}) Then
                                Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                            Else
                                Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                                Exit Try
                            End If
                            'Erase
                            Invoke(Sub() TextBox8.AppendText("Erasing " & i & "/" & NumericUpDown6.Value & ".."))
                            If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H19, 1, 0, 0, 0, 0}, TimeOut:=320) Then
                                Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                            Else
                                Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                                Exit Try
                            End If
                        Next
                        'Unthread
                        Invoke(Sub() TextBox8.AppendText("Unthreading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, &HA, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Thread
                        Invoke(Sub() TextBox8.AppendText("Threading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 1, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Remove Partition
                        Invoke(Sub() TextBox8.AppendText("Reinitializing.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {4, 0, 0, 0, 0, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Unload
                        Invoke(Sub() TextBox8.AppendText("Unloading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 0, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                    Catch ex As Exception
                        Invoke(Sub() TextBox8.AppendText(ex.ToString()))
                    End Try
                    Invoke(Sub() TextBox8.AppendText("Erase finished."))
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBox3.Checked)
                           End Sub)
                End Sub)
        th.Start()
        If Panel1.Enabled Then RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub ButtonDebugWriteBarcode_Click(sender As Object, e As EventArgs) Handles ButtonDebugWriteBarcode.Click
        If Not LoadComplete Then Exit Sub
        Panel1.Enabled = False
        Dim barcode As String = TextBox9.Text
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
                               TextBox2.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI(CheckBox3.Checked)
                           End Sub)
                End Sub)
        th.Start()
        If Panel1.Enabled Then RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub Label11_Click(sender As Object, e As EventArgs) Handles Label11.Click
    End Sub

    Private Sub ButtonDebugReadMAM_Click(sender As Object, e As EventArgs) Handles ButtonDebugReadMAM.Click
        Dim ResultB As Byte() = TapeUtils.GetMAMAttributeBytes(ConfTapeDrive, NumericUpDown8.Value, NumericUpDown9.Value, NumericUpDown1.Value)
        If ResultB.Length = 0 Then Exit Sub
        Dim Result As String = System.Text.Encoding.UTF8.GetString(ResultB)
        If Result <> "" Then TextBox8.Text = ("Result: " & vbCrLf & Result & vbCrLf & vbCrLf)
        TextBox8.AppendText(Byte2Hex(ResultB))

    End Sub

    Private Sub Label13_Click(sender As Object, e As EventArgs) Handles Label13.Click
        MessageBox.Show("/* Page code of Application Name */
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
        Try
            CMInfo = New TapeUtils.CMParser(TapeDrive)

        Catch ex As Exception
            TextBox8.AppendText("CM Data Parsing Failed." & vbCrLf)
        End Try
        TextBox8.Text = ""

        Try
            TextBox8.AppendText(CMInfo.GetReport())
            If CheckBox4.Checked AndAlso CMInfo IsNot Nothing Then
                TextBox8.AppendText(CMInfo.GetSerializedText())
                TextBox8.AppendText(vbCrLf)
            End If
        Catch ex As Exception
            TextBox8.AppendText("| CM data parsing failed.".PadRight(74) & "|" & vbCrLf)
        End Try
        TextBox8.Select(0, 0)
        TextBox8.ScrollToCaret()
        If IO.Directory.Exists(My.Application.Info.DirectoryPath & "\Info") Then
            Dim fn As String
            Try
                fn = CMInfo.ApplicationSpecificData.Barcode
                If fn Is Nothing OrElse fn.Length = 0 Then fn = CMInfo.CartridgeMfgData.CartridgeSN
                If fn Is Nothing Then fn = ""
                IO.File.WriteAllText($"{My.Application.Info.DirectoryPath}\Info\{fn}.txt", TextBox8.Text)
            Catch ex As Exception

            End Try
        End If
        Me.Enabled = True
    End Sub

    Private Sub ButtonDebugDumpMAM_Click(sender As Object, e As EventArgs) Handles ButtonDebugDumpMAM.Click
        If SaveFileDialog1.ShowDialog = DialogResult.OK Then
            ButtonDebugDumpMAM.Enabled = False

            Dim th As New Threading.Thread(
                Sub()
                    Dim MAMData As New TapeUtils.MAMAttributeList
                    For i As UInt16 = &H0 To &HFFFF Step 1

                        Try
                            Dim Attr As TapeUtils.MAMAttribute = TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, i, CByte(NumericUpDown1.Value))
                            If Attr IsNot Nothing Then
                                Me.Invoke(Sub()
                                              TextBox8.Text = Byte2Hex({Attr.ID_MSB, Attr.ID_LSB}) & " LEN=" & Attr.RawData.Length & vbCrLf & vbCrLf
                                              TextBox8.AppendText(Attr.AsNumeric & vbCrLf & vbCrLf)
                                              TextBox8.AppendText(Attr.AsString & vbCrLf & vbCrLf)
                                              TextBox8.AppendText(Byte2Hex(Attr.RawData) & vbCrLf)
                                          End Sub)
                                MAMData.Content.Add(Attr)
                            Else
                                If (i And &H7F) = 0 Then
                                    Dim i2 As UInt16 = i
                                    Me.Invoke(Sub()
                                                  TextBox8.Text = Byte2Hex({i2 >> 8 And &HFF, i2 And &HFF}) & " LEN=0"
                                              End Sub)
                                End If

                            End If
                        Catch ex As Exception
                            MessageBox.Show(i & vbCrLf & ex.ToString)
                        End Try
                        If i = &HFFFF Then Exit For
                    Next
                    MessageBox.Show("Dump Complete")
                    MAMData.SaveSerializedText(SaveFileDialog1.FileName)
                    Me.Invoke(Sub() ButtonDebugDumpMAM.Enabled = True)
                End Sub)
            th.Start()
        End If
    End Sub

    Private Sub ButtonDebugRewind_Click(sender As Object, e As EventArgs) Handles ButtonDebugRewind.Click
        Me.Enabled = False
        Dim cdbData As Byte() = {1, 0, 0, 0, 0, 0}
        Dim cdb As IntPtr = Marshal.AllocHGlobal(6)
        Marshal.Copy(cdbData, 0, cdb, 6)
        Dim data As IntPtr = Marshal.AllocHGlobal(1)
        Dim sense As IntPtr = Marshal.AllocHGlobal(127)
        TapeUtils._TapeSCSIIOCtlFullC(ConfTapeDrive, cdb, 6, data, 0, 2, 60000, sense)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(data)
        Marshal.FreeHGlobal(sense)
        Me.Enabled = True
    End Sub

    Private Sub ButtonDebugReadBlock_Click(sender As Object, e As EventArgs) Handles ButtonDebugReadBlock.Click
        Me.Enabled = False
        Dim ReadLen As Integer = NumericUpDown7.Value

        'Dim cdbData As Byte() = {8, 0, ReadLen >> 16 And &HFF, ReadLen >> 8 And &HFF, ReadLen And &HFF, 0}
        'Dim cdb As IntPtr = Marshal.AllocHGlobal(6)
        'Marshal.Copy(cdbData, 0, cdb, 6)
        'Dim readData(ReadLen - 1) As Byte
        'Dim data As IntPtr = Marshal.AllocHGlobal(ReadLen)
        'Marshal.Copy(readData, 0, data, ReadLen)
        'Dim sense As IntPtr = Marshal.AllocHGlobal(127)
        'TapeUtils._TapeSCSIIOCtlFull(ConfTapeDrive, cdb, 6, data, ReadLen, 1, &HFFFF, sense)
        'Marshal.Copy(data, readData, 0, ReadLen)
        'Marshal.FreeHGlobal(cdb)
        'Marshal.FreeHGlobal(data)
        'Marshal.FreeHGlobal(sense)
        Dim sense(63) As Byte
        Dim readData As Byte() = TapeUtils.ReadBlock(ConfTapeDrive, sense, ReadLen)
        Dim DiffBytes As Int32
        For i As Integer = 3 To 6
            DiffBytes <<= 8
            DiffBytes = DiffBytes Or sense(i)
        Next
        Dim Add_Key As UInt16 = CInt(sense(12)) << 8 Or sense(13)
        TextBox8.Text = TapeUtils.ParseAdditionalSenseCode(Add_Key) & vbCrLf & vbCrLf & "Raw data:" & vbCrLf
        TextBox8.Text &= "Length: " & readData.Length & vbCrLf
        If DiffBytes < 0 Then
            TextBox8.Text &= TapeUtils.ParseSenseData(sense) & vbCrLf
            TextBox8.Text &= "Excess data Is discarded. Block length should be " & readData.Length - DiffBytes & vbCrLf & vbCrLf
        End If
        TextBox8.Text &= Byte2Hex(readData, True)
        Me.Enabled = True
    End Sub

    Private Sub ButtonDebugDumpBuffer_Click(sender As Object, e As EventArgs) Handles ButtonDebugDumpBuffer.Click
        Me.Enabled = False
        Dim BufferID = Convert.ToByte(ComboBox2.SelectedItem.Substring(0, 2), 16)
        Dim DumpData As Byte() = TapeUtils.ReadBuffer(ConfTapeDrive, BufferID)
        TextBox8.Text = "Buffer len=" & DumpData.Length & vbCrLf
        SaveFileDialog2.FileName = ComboBox2.SelectedItem & ".bin"
        If SaveFileDialog2.ShowDialog = DialogResult.OK Then
            IO.File.WriteAllBytes(SaveFileDialog2.FileName, DumpData)
        End If
        TextBox8.Text &= Byte2Hex(DumpData, True)
        Me.Enabled = True
    End Sub

    Private Sub Label9_Click(sender As Object, e As EventArgs) Handles Label9.Click
        Dim n As String = InputBox("Byte count?", "cdb SetBytes", "")
        If n <> "" Then
            If Val(n) > 0 Then
                TextBox7.Text = ""
                Dim sb As New StringBuilder
                For i As Integer = 1 To Val(n)
                    sb.Append("00 ")
                Next
                TextBox7.Text = sb.ToString()
            End If
        End If
    End Sub

    Private Sub ButtonDebugLocate_Click(sender As Object, e As EventArgs) Handles ButtonDebugLocate.Click
        Me.Enabled = False
        TextBox8.Text = TapeUtils.ParseAdditionalSenseCode(TapeUtils.Locate(ConfTapeDrive,
                                                                            NumericUpDown2.Value,
                                                                            NumericUpDown1.Value,
                                                                            System.Enum.Parse(GetType(TapeUtils.LocateDestType), ComboBox3.SelectedItem)))
        Me.Enabled = True

    End Sub

    Private Sub ButtonDebugReadPosition_Click(sender As Object, e As EventArgs) Handles ButtonDebugReadPosition.Click
        Me.Enabled = False
        Dim pos As New TapeUtils.PositionData(ConfTapeDrive)
        'Dim param As Byte() = TapeUtils.SCSIReadParam(ConfTapeDrive, {&H34, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 20)
        'Dim BOP As Boolean = param(0) >> 7 = 1
        'Dim EOP As Boolean = ((param(0) >> 6) And &H1) = 1
        'Dim LOCU As Boolean = ((param(0) >> 5) And &H1) = 1
        'Dim BYCU As Boolean = ((param(0) >> 4) And &H1) = 1
        'Dim LOLU As Boolean = ((param(0) >> 2) And &H1) = 1
        TextBox8.Text = ""
        TextBox8.Text &= "Partition " & pos.PartitionNumber & vbCrLf
        TextBox8.Text &= "Block " & pos.BlockNumber & vbCrLf
        TextBox8.Text &= "FileMark " & pos.FileNumber & vbCrLf
        TextBox8.Text &= "Set " & pos.SetNumber & vbCrLf
        TextBox8.Text &= vbCrLf
        Me.Enabled = True
        If pos.BOP Then TextBox8.Text &= "BOM - Beginning of media" & vbCrLf
        If pos.EOP Then TextBox8.Text &= "EW-EOM - Early warning" & vbCrLf
        If pos.EOD Then TextBox8.Text &= "End of Data detected" & vbCrLf
        'If LOCU Then TextBox8.Text &= "LOCU" & vbCrLf
        'If BYCU Then TextBox8.Text &= "BYCU" & vbCrLf
        'If LOLU Then TextBox8.Text &= "LOLU" & vbCrLf
    End Sub
    Public Operation_Cancel_Flag As Boolean = False
    Private Sub ButtonDebugDumpTape_Click(sender As Object, e As EventArgs) Handles ButtonDebugDumpTape.Click
        If FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            If New IO.DirectoryInfo(FolderBrowserDialog1.SelectedPath).GetFiles("*.bin", IO.SearchOption.TopDirectoryOnly).Length > 0 Then
                MessageBox.Show("File exist: *.bin; Cancelled.")
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
            For Each c As Control In TabPage1.Controls
                c.Enabled = False
            Next
            TabControl1.Enabled = True
            TabPage1.Enabled = True
            Button24.Enabled = True
            TextBox8.Text = ""
            Dim log As Boolean = CheckBox2.Checked
            Dim thprog As New Threading.Thread(
                Sub()
                    Dim ReadLen As Integer = NumericUpDown7.Value
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
                                       If TextBox8.Text.Length > 10000 Then TextBox8.Text = ""
                                       TextBox8.AppendText("Processing file " & FileNum.ToString.PadRight(10) & " (Block = " & Block & ") Sense:")
                                       TextBox8.AppendText(TapeUtils.ParseAdditionalSenseCode(Add_Key) & vbCrLf)
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
                               For Each c As Control In TabPage1.Controls
                                   c.Enabled = True
                               Next
                           End Sub)
                End Sub)
            thprog.Start()

        End If
    End Sub

    Private Sub Button24_Click(sender As Object, e As EventArgs) Handles Button24.Click
        Operation_Cancel_Flag = True
    End Sub

    Private Sub ButtonDebugDumpIndex_Click(sender As Object, e As EventArgs) Handles ButtonDebugDumpIndex.Click
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
    End Sub

    Private Sub ButtonDebugFormat_Click(sender As Object, e As EventArgs) Handles ButtonDebugFormat.Click
        If Not LoadComplete Then Exit Sub
        Panel1.Enabled = False
        Dim dL As Char = ComboBox1.Text
        Dim barcode As String = TextBox9.Text
        Dim th As New Threading.Thread(
                Sub()
                    Invoke(Sub() TextBox8.Text = "Start format ..." & vbCrLf)
                    Try
                        'Load and Thread
                        Invoke(Sub() TextBox8.AppendText("Loading.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H1B, 0, 0, 0, 1, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        Dim MaxExtraPartitionAllowed As Byte = TapeUtils.ModeSense(ConfTapeDrive, &H11)(2)
                        'Erase
                        Invoke(Sub() TextBox8.AppendText("Initializing tape.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {4, 0, 0, 0, 0, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Mode Sense
                        Invoke(Sub() TextBox8.AppendText("MODE SENSE"))
                        Dim ModeData As Byte()
                        ModeData = TapeUtils.ModeSense(TapeDrive, &H11)
                        ReDim Preserve ModeData(11)
                        Invoke(Sub() TextBox8.AppendText($"     Mode Data: {Byte2Hex(ModeData)}{vbCrLf}"))
                        'Mode Select:1st Partition to Minimum 
                        Invoke(Sub() TextBox8.AppendText("MODE SELECT - Partition mode page.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H15, &H10, 0, 0, &H10, 0}, {0, 0, &H10, 0, &H11, &HA, MaxExtraPartitionAllowed, 1, ModeData(4), ModeData(5), ModeData(6), ModeData(7), 0, 1, &HFF, &HFF}, 0) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Format
                        Invoke(Sub() TextBox8.AppendText("Partitioning.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {4, 0, 1, 0, 0, 0}, Nothing, 0) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set Vendor
                        Invoke(Sub() TextBox8.AppendText($"WRITE ATTRIBUTE: Vendor=OPEN.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H800, "OPEN".PadRight(8)) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set AppName
                        Invoke(Sub() TextBox8.AppendText($"WRITE ATTRIBUTE: Application name = LTFSCopyGUI.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H801, "LTFSCopyGUI".PadRight(32)) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set Version
                        Invoke(Sub() TextBox8.AppendText($"WRITE ATTRIBUTE: Application Version={My.Application.Info.Version.ToString(3)}.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H802, My.Application.Info.Version.ToString(3).PadRight(8)) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set TextLabel
                        Invoke(Sub() TextBox8.AppendText($"WRITE ATTRIBUTE: TextLabel= .."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H803, "".PadRight(160), TapeUtils.AttributeFormat.Text) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set TLI
                        Invoke(Sub() TextBox8.AppendText($"WRITE ATTRIBUTE: Localization Identifier = 0.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H805, {0}, TapeUtils.AttributeFormat.Binary) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set Barcode
                        Invoke(Sub() TextBox8.AppendText($"WRITE ATTRIBUTE: Barcode={barcode}.."))
                        If TapeUtils.SetBarcode(ConfTapeDrive, barcode) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Set Version
                        Invoke(Sub() TextBox8.AppendText($"WRITE ATTRIBUTE: Format Version=2.4.0.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H80B, "2.4.0".PadRight(16)) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Mode Select:Block Length
                        Invoke(Sub() TextBox8.AppendText("MODE SELECT - Block size.."))
                        If TapeUtils.SetBlockSize(ConfTapeDrive, 524288).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Locate
                        Invoke(Sub() TextBox8.AppendText("Locate to data partition.."))
                        If TapeUtils.Locate(ConfTapeDrive, 0, 1) = 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write VOL1Label
                        Invoke(Sub() TextBox8.AppendText("Write VOL1Label.."))
                        If TapeUtils.Write(ConfTapeDrive, New Vol1Label().GenerateRawData(barcode)).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBox8.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        Dim plabel As New ltfslabel()
                        plabel.volumeuuid = Guid.NewGuid()
                        plabel.location.partition = ltfslabel.PartitionLabel.b
                        plabel.partitions.index = ltfslabel.PartitionLabel.a
                        plabel.partitions.data = ltfslabel.PartitionLabel.b
                        plabel.blocksize = 524288

                        'Write ltfslabel
                        Invoke(Sub() TextBox8.AppendText("Write ltfslabel.."))
                        If TapeUtils.Write(ConfTapeDrive, Encoding.UTF8.GetBytes(plabel.GetSerializedText())).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBox8.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive, 2).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
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
                        Invoke(Sub() TextBox8.AppendText("Write ltfsindex.."))
                        If TapeUtils.Write(ConfTapeDrive, Encoding.UTF8.GetBytes(pindex.GetSerializedText())).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBox8.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Locate
                        Invoke(Sub() TextBox8.AppendText("Locate to index partition.."))
                        If TapeUtils.Locate(ConfTapeDrive, 0, 0) = 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write VOL1Label
                        Invoke(Sub() TextBox8.AppendText("Write VOL1Label.."))
                        If TapeUtils.Write(ConfTapeDrive, New Vol1Label().GenerateRawData(barcode)).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBox8.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write ltfslabel
                        plabel.location.partition = ltfslabel.PartitionLabel.a
                        Invoke(Sub() TextBox8.AppendText("Write ltfslabel.."))
                        If TapeUtils.Write(ConfTapeDrive, Encoding.UTF8.GetBytes(plabel.GetSerializedText())).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBox8.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive, 2).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write ltfsindex
                        pindex.previousgenerationlocation = New ltfsindex.PartitionDef()
                        pindex.previousgenerationlocation.partition = pindex.location.partition
                        pindex.previousgenerationlocation.startblock = pindex.location.startblock
                        pindex.location.partition = ltfsindex.PartitionLabel.a
                        pindex.location.startblock = TapeUtils.ReadPosition(ConfTapeDrive).BlockNumber
                        Dim block0 As ULong = pindex.location.startblock
                        Invoke(Sub() TextBox8.AppendText("Write ltfsindex.."))
                        If TapeUtils.Write(ConfTapeDrive, Encoding.UTF8.GetBytes(pindex.GetSerializedText())).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Write FileMark
                        Invoke(Sub() TextBox8.AppendText("Write FileMark.."))
                        If TapeUtils.WriteFileMark(ConfTapeDrive).Length > 0 Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Set DateTime
                        Dim CurrentTime As String = Now.ToUniversalTime.ToString("yyyyMMddhhmm")
                        Invoke(Sub() TextBox8.AppendText($"WRITE ATTRIBUTE: Written time={CurrentTime}.."))
                        If TapeUtils.SetMAMAttribute(ConfTapeDrive, &H804, CurrentTime.PadRight(12)) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Set VCI
                        Invoke(Sub() TextBox8.AppendText($"WRITE ATTRIBUTE: VCI.."))
                        If TapeUtils.WriteVCI(ConfTapeDrive, pindex.generationnumber, block0, block1, pindex.volumeuuid.ToString()) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        Invoke(Sub() TextBox8.AppendText("Format finished."))

                    Catch ex As Exception
                        Invoke(Sub()
                                   TextBox8.AppendText(ex.ToString & vbCrLf)
                                   TextBox8.AppendText("Format failed.")
                               End Sub)
                    End Try
                    Invoke(Sub() Panel1.Enabled = True)
                End Sub)
        th.Start()
        If Panel1.Enabled Then RefreshUI(CheckBox3.Checked)
    End Sub

    Private Sub Button27_Click(sender As Object, e As EventArgs) Handles Button27.Click
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
        TapeUtils.ReleaseUnit(ConfTapeDrive,
                              Function(sense As Byte()) As Boolean
                                  Invoke(Sub()
                                             TextBox8.Text = "RELEASE UNIT" & vbCrLf
                                             TextBox8.AppendText(TapeUtils.ParseSenseData(sense))
                                         End Sub)
                                  Return True
                              End Function)
    End Sub

    Private Sub ButtonDebugAllowMediaRemoval_Click(sender As Object, e As EventArgs) Handles ButtonDebugAllowMediaRemoval.Click
        TapeUtils.AllowMediaRemoval(ConfTapeDrive,
                              Function(sense As Byte()) As Boolean
                                  Invoke(Sub()
                                             TextBox8.Text = "ALLOW MEDIA REMOVAL" & vbCrLf
                                             TextBox8.AppendText(TapeUtils.ParseSenseData(sense))
                                         End Sub)
                                  Return True
                              End Function)
    End Sub

    Private Sub Button30_Click(sender As Object, e As EventArgs) Handles Button30.Click
        Form1.Show()
    End Sub

    Private Sub LTFSConfigurator_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.LTFSConf_AutoRefresh = CheckBox3.Checked
        My.Settings.Save()
    End Sub

    Private Sub ButtonDebugDumpTape_MouseUp(sender As Object, e As MouseEventArgs) Handles ButtonDebugDumpTape.MouseUp
        If Not e.Button = MouseButtons.Right Then Exit Sub
        If MessageBox.Show("Write will destroy everything after current position. Continue?", "Warning", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            If OpenFileDialog1.ShowDialog = DialogResult.OK Then
                Dim fname As String = OpenFileDialog1.FileName
                Dim th As New Threading.Thread(
                    Sub()
                        Dim fs As New IO.FileStream(fname, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                        If fs.Length = 0 Then
                            TapeUtils.WriteFileMark(ConfTapeDrive)
                            Invoke(Sub()
                                       TextBox8.Text = $"Filemark written."
                                       Panel1.Enabled = True
                                   End Sub)
                        Else
                            Invoke(Sub() TextBox8.Text = $"Writing: {fname}")
                            Dim buffer(Math.Min(NumericUpDown7.Value - 1, fs.Length - 1)) As Byte
                            While fs.Read(buffer, 0, buffer.Length) > 0
                                TapeUtils.Write(ConfTapeDrive, buffer)
                            End While
                            Invoke(Sub()
                                       TextBox8.Text = $"Write finished: {fname}"
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

    Private Sub CheckBox1_MouseUp(sender As Object, e As MouseEventArgs) Handles CheckBox1.MouseUp
        If e.Button = MouseButtons.Right Then
            Button2.Visible = Not Button2.Visible
            Button3.Visible = Not Button3.Visible
            Button4.Visible = Not Button4.Visible
            Button5.Visible = Not Button5.Visible
        End If
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        ChangerTool.Show()
    End Sub

    Private Sub Button11_Click(sender As Object, e As EventArgs) Handles Button11.Click
        TextBox8.Clear()
        Dim logdata As Byte()
        Dim pdata As TapeUtils.PageData
#Region "0x00"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H0, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Supported Log Pages page", .PageCode = &H0, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 2,
                        .TotalBits = 6,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Supported Page List",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 8,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 0,
                        .DynamicParamLenTotalBits = 0,
                        .DynamicParamDataStartByte = 1,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(&H0, "Supported Pages (00h)")
            .Add(&H2, "Write Error Counters (02h)")
            .Add(&H3, "Read Error Counters (03h)")
            .Add(&HC, "Sequential Access Device Log (0Ch)")
            .Add(&HD, "Temperature Log (0Dh)")
            .Add(&H11, "DTD Status Log (11h)")
            .Add(&H12, "TapeAlert Response Log (12h)")
            .Add(&H13, "Requested Recovery Log (13h)")
            .Add(&H14, "Device Statistics Log (14h)")
            .Add(&H15, "Service Buffers Information Log (15h)")
            .Add(&H16, "Tape Diagnostics Data Log (16h)")
            .Add(&H17, "Volume Statistics Log (17h)")
            .Add(&H18, "SAS Port Log (18h)")
            .Add(&H1B, "Data Compression Log (1Bh)")
            .Add(&H2E, "TapeAlert Log (2Eh)")
            .Add(&H30, "Tape Usage Log (30h)")
            .Add(&H31, "Tape Capacity Log (31h)")
            .Add(&H32, "Data Compression (HP-only) Log (32h)")
            .Add(&H33, "Device Wellness Log (33h)")
            .Add(&H34, "Performance Log (34h)")
            .Add(&H35, "DT Device Error Log (35h)")
            .Add(&H3E, "Device Status Log (3Eh)")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(&H0, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H2, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H3, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&HC, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&HD, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H11, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H12, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H13, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H14, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H15, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H16, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H17, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H18, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H1B, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H2E, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H30, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H31, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H32, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H33, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H34, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H35, TapeUtils.PageData.DataItem.DataType.Binary)
            .Add(&H3E, TapeUtils.PageData.DataItem.DataType.Binary)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x00.xml"), pdata.GetSerializedText())
#End Region
#Region "0x02"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H2, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Write Error Counters log page", .PageCode = &H2, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 2,
                        .TotalBits = 6,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Write Error Counters",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 1,
                        .DynamicParamCodeTotalBits = 8,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(&H0, "Errors corrected without substantial delay (Total number of errors corrected without delay)")
            .Add(&H1, "Errors corrected with possible delays (Total number of errors corrected using retries)")
            .Add(&H2, "Total (Sum of parameters 3 and 6)")
            .Add(&H3, "Total errors corrected (The number of data sets that needed to be rewritten)")
            .Add(&H4, "Total times error correction processed (Number of CCQ sets rewritten)")
            .Add(&H5, "Total data sets processed (The total number of data sets written)")
            .Add(&H6, "Total uncorrected errors (The number of data sets that could not be written)")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int32)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x02.xml"), pdata.GetSerializedText())
#End Region
#Region "0x03"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H3, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Read Error Counters log page", .PageCode = &H3, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 2,
                        .TotalBits = 6,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Read Error Counters",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 1,
                        .DynamicParamCodeTotalBits = 8,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(&H0, "Errors corrected without substantial delay (Total number of errors corrected without delay)")
            .Add(&H1, "Errors corrected with possible delays (Total number of errors corrected using retries)")
            .Add(&H2, "Total (Sum of parameters 3 and 6)")
            .Add(&H3, "Total errors corrected (The number of data sets that were corrected after a read retry)")
            .Add(&H4, "Total times error correction processed (Number of times C2 correction is invoked)")
            .Add(&H5, "Total bytes processed (The total number of data sets read)")
            .Add(&H6, "Total uncorrected errors (The number of data sets that could not be read after retries)")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int32)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x03.xml"), pdata.GetSerializedText())
#End Region
#Region "0x0C"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &HC, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Sequential Access Device log page", .PageCode = &HC, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 2,
                        .TotalBits = 6,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Supported Page List",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(&H0, "Total channel write bytes. The number of data bytes received from application clients during write command operations. This is the number of bytes transferred over SCSI, before compression.")
            .Add(&H1, "Total device write bytes. The number of data bytes written to the media as a result of write command operations, not counting the overhead from ECC and formatting. This is the number of data bytes transferred to media, after compression.")
            .Add(&H2, "Total device read bytes. The number of data bytes read from the media during read command operations, not counting the overhead from ECC and formatting. This is the number of data bytes transferred from media with compression.")
            .Add(&H3, "Total channel read bytes. The number of data bytes transferred to the initiator or initiators during read command operations. This is the number of bytes transferred over SCSI, after decompression.")
            .Add(&H4, "The approximate native capacity from BOP to EOD, in megabytes. ")
            .Add(&H5, "The approximate native capacity between BOP and EW in the current partition in megabytes.")
            .Add(&H6, "The minimum native capacity from EW and EOP in the current partition, in megabytes.")
            .Add(&H7, "The approximate native capacity from BOP to the current position of the medium in megabytes.")
            .Add(&H8, "The maximum native capacity that is currently allowed to be in the device object buffer, in megabytes.")
            .Add(&H100, "Cleaning requested; a non-volatile cleaning indication.")
            .Add(&H8000, "Total megabytes processed since cleaning. The Number of megabytes processed to tape since last cleaning (written after compression/read before decompression)")
            .Add(&H8001, "Lifetime load cycles. This is the number of times the drive has been loaded in its lifetime.")
            .Add(&H8002, "Lifetime cleaning cycles. This is the number of times over its lifetime the drive has been cleaned using a cleaner cartridge.")
            .Add(&H8003, "Lifetime Power-on time. This is the number of seconds the drive has been powered on over its lifetime.")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H7, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H8, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H100, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H8000, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H8001, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H8002, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H8003, TapeUtils.PageData.DataItem.DataType.Int32)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x0C.xml"), pdata.GetSerializedText())
#End Region
#Region "0x0D"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &HD, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Temperature log page", .PageCode = &HD, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Temperature log parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(&H0, "Temperature")
            .Add(&H1, "Reference Temperature")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int16)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x0D.xml"), pdata.GetSerializedText())
#End Region
#Region "0x11"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H11, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Data Transfer Device (DTD) Status log page", .PageCode = &H11, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "DTD Status log parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        Dim SenseCodeTranslator As New SerializableDictionary(Of Long, String)
        With SenseCodeTranslator
            .Add(&H0, "NO SENSE")
            .Add(&H1, "RECOVERED ERROR")
            .Add(&H2, "NOT READY")
            .Add(&H3, "MEDIUM ERROR")
            .Add(&H4, "HARDWARE ERROR")
            .Add(&H5, "ILLEGAL REQUEST")
            .Add(&H6, "UNIT ATTENTION")
            .Add(&H7, "DATA PROTECT")
            .Add(&H8, "BLANK CHECK")
            .Add(&H9, "VENDOR SPECIFIC")
            .Add(&HA, "COPY ABORTED")
            .Add(&HB, "ABORTED COMMAND")
            .Add(&HC, "EQUAL")
            .Add(&HD, "VOLUME OVERFLOW")
            .Add(&HE, "MISCOMPARE")
            .Add(&HF, "RESERVED")
        End With
        Dim AdditionalSenseCodeTranslator As New SerializableDictionary(Of Long, String)
        With AdditionalSenseCodeTranslator
            .Add(&H0, "No addition sense")
            .Add(&H1, "Filemark detected")
            .Add(&H2, "End of Tape detected")
            .Add(&H4, "Beginning of Tape detected")
            .Add(&H5, "End of Data detected")
            .Add(&H16, "Operation in progress")
            .Add(&H18, "Erase operation in progress")
            .Add(&H19, "Locate operation in progress")
            .Add(&H1A, "Rewind operation in progress")
            .Add(&H400, "LUN not ready, cause not reportable")
            .Add(&H401, "LUN in process of becoming ready")
            .Add(&H402, "LUN not ready, Initializing command required")
            .Add(&H404, "LUN not ready, format in progress")
            .Add(&H407, "Command in progress")
            .Add(&H409, "LUN not ready, self-test in progress")
            .Add(&H40C, "LUN not accessible, port in unavailable state")
            .Add(&H412, "Logical unit offline")
            .Add(&H800, "Logical unit communication failure")
            .Add(&HB00, "Warning")
            .Add(&HB01, "Thermal limit exceeded")
            .Add(&HC00, "Write error")
            .Add(&HE01, "Information unit too short")
            .Add(&HE02, "Information unit too long")
            .Add(&HE03, "SK Illegal Request")
            .Add(&H1001, "Logical block guard check failed")
            .Add(&H1100, "Unrecovered read error")
            .Add(&H1112, "Media Auxiliary Memory read error")
            .Add(&H1400, "Recorded entity not found")
            .Add(&H1403, "End of Data not found")
            .Add(&H1A00, "Parameter list length error")
            .Add(&H2000, "Invalid command operation code")
            .Add(&H2400, "Invalid field in Command Descriptor Block")
            .Add(&H2500, "LUN not supported")
            .Add(&H2600, "Invalid field in parameter list")
            .Add(&H2601, "Parameter not supported")
            .Add(&H2602, "Parameter value invalid")
            .Add(&H2604, "Invalid release of persistent reservation")
            .Add(&H2610, "Data decryption key fail limit reached")
            .Add(&H2680, "Invalid CA certificate")
            .Add(&H2700, "Write-protected")
            .Add(&H2708, "Too many logical objects on partition to support operation")
            .Add(&H2800, "Not ready to ready transition, medium may have changed")
            .Add(&H2901, "Power-on reset")
            .Add(&H2902, "SCSI bus reset")
            .Add(&H2903, "Bus device reset")
            .Add(&H2904, "Internal firmware reboot")
            .Add(&H2907, "I_T nexus loss occurred")
            .Add(&H2A01, "Mode parameters changed")
            .Add(&H2A02, "Log parameters changed")
            .Add(&H2A03, "Reservations pre-empted")
            .Add(&H2A04, "Reservations released")
            .Add(&H2A05, "Registrations pre-empted")
            .Add(&H2A06, "Asymmetric access state changed")
            .Add(&H2A07, "Asymmetric access state transition failed")
            .Add(&H2A08, "Priority changed")
            .Add(&H2A0D, "Data encryption capabilities changed")
            .Add(&H2A10, "Timestamp changed")
            .Add(&H2A11, "Data encryption parameters changed by another initiator")
            .Add(&H2A12, "Data encryption parameters changed by a vendor-specific event")
            .Add(&H2A13, "Data Encryption Key Instance Counter has changed")
            .Add(&H2A14, "SA creation capabilities data has changed")
            .Add(&H2A15, "Medium removal prevention pre-empted")
            .Add(&H2A80, "Security configuration changed")
            .Add(&H2C00, "Command sequence invalid")
            .Add(&H2C07, "Previous busy status")
            .Add(&H2C08, "Previous task set full status")
            .Add(&H2C09, "Previous reservation conflict status")
            .Add(&H2C0B, "Not reserved")
            .Add(&H2F00, "Commands cleared by another initiator")
            .Add(&H3000, "Incompatible medium installed")
            .Add(&H3001, "Cannot read media, unknown format")
            .Add(&H3002, "Cannot read media: incompatible format")
            .Add(&H3003, "Cleaning cartridge installed")
            .Add(&H3004, "Cannot write medium")
            .Add(&H3005, "Cannot write medium, incompatible format")
            .Add(&H3006, "Cannot format, incompatible medium")
            .Add(&H3007, "Cleaning failure")
            .Add(&H300C, "WORM medium—overwrite attempted")
            .Add(&H300D, "WORM medium—integrity check failed")
            .Add(&H3100, "Medium format corrupted")
            .Add(&H3700, "Rounded parameter")
            .Add(&H3A00, "Medium not present")
            .Add(&H3A04, "Medium not present, Media Auxiliary Memory accessible")
            .Add(&H3B00, "Sequential positioning error")
            .Add(&H3B0C, "Position past BOM")
            .Add(&H3B1C, "Too many logical objects on partition to support operation.")
            .Add(&H3E00, "Logical unit has not self-configured yet")
            .Add(&H3F01, "Microcode has been changed")
            .Add(&H3F03, "Inquiry data has changed")
            .Add(&H3F05, "Device identifier changed")
            .Add(&H3F0E, "Reported LUNs data has changed")
            .Add(&H3F0F, "Echo buffer overwritten")
            .Add(&H4300, "Message error")
            .Add(&H4400, "Internal target failure")
            .Add(&H4500, "Selection/reselection failure")
            .Add(&H4700, "SCSI parity error")
            .Add(&H4800, "Initiator Detected Error message received")
            .Add(&H4900, "Invalid message")
            .Add(&H4B00, "Data phase error")
            .Add(&H4B02, "Too much write data")
            .Add(&H4B03, "ACK/NAK timeout")
            .Add(&H4B04, "NAK received")
            .Add(&H4B05, "Data offset error")
            .Add(&H4B06, "Initiator response timeout")
            .Add(&H4D00, "Tagged overlapped command")
            .Add(&H4E00, "Overlapped commands")
            .Add(&H5000, "Write append error")
            .Add(&H5200, "Cartridge fault")
            .Add(&H5300, "Media load or eject failed")
            .Add(&H5301, "Unload tape failure")
            .Add(&H5302, "Medium removal prevented")
            .Add(&H5303, "Insufficient resources")
            .Add(&H5304, "Medium thread or unthread failure")
            .Add(&H5504, "Insufficient registration resources")
            .Add(&H5506, "Media Auxiliary Memory full")
            .Add(&H5B01, "Threshold condition met")
            .Add(&H5D00, "Failure prediction threshold exceeded")
            .Add(&H5DFF, "Failure prediction threshold exceeded (false)")
            .Add(&H5E01, "Idle condition activated by timer")
            .Add(&H7400, "Security error")
            .Add(&H7401, "Unable to decrypt data")
            .Add(&H7402, "Unencrypted data encountered while decrypting")
            .Add(&H7403, "Incorrect data encryption key")
            .Add(&H7404, "Cryptographic integrity validation failed")
            .Add(&H7405, "Key-associated data descriptors changed.")
            .Add(&H7408, "Digital signature validation failure")
            .Add(&H7409, "Encryption mode mismatch on read")
            .Add(&H740A, "Encrypted block not RAW read-enabled")
            .Add(&H740B, "Incorrect encryption parameters")
            .Add(&H7421, "Data encryption configuration prevented")
            .Add(&H7440, "Authentication failed")
            .Add(&H7461, "External data encryption Key Manager access error")
            .Add(&H7462, "External data encryption Key Manager error")
            .Add(&H7463, "External data encryption management—key not found")
            .Add(&H7464, "External data encryption management—request not authorized")
            .Add(&H746E, "External data encryption control time-out")
            .Add(&H746F, "External data encryption control unknown error")
            .Add(&H7471, "Logical Unit access not authorized")
            .Add(&H7480, "KAD changed")
            .Add(&H7482, "Crypto KAD in CM failure")
            .Add(&H8282, "Drive requires cleaning")
            .Add(&H8283, "Bad microcode detected")
        End With
        With pdata.Items.Last.EnumTranslator
            .Add(&H0, "Very High Frequency data")
            .Add(&H1, "Very High Frequency polling delay")
            .Add(&H2, "DT device ADC data encryption control status")
            .Add(&H3, "Key management error data")
            .Add(&H101, "DTD primary status - SAS/FC Port A")
            .Add(&H102, "DTD primary status - SAS/FC Port B")
            .Add(&H103, "DTD primary status - Fibre Channel NPIV port A")
            .Add(&H104, "DTD primary status - Fibre Channel NPIV port B")
            .Add(&H8000, "VU Very High Frequency data")
            .Add(&H8003, "VU key management error")
            .Add(&H8010, "VU extended VHF data")
            .Add(&H8020, "VU multi-initiator conflict warning")
            .Add(&HA101, "VU Fibre Channel port A failover status")
            .Add(&HA102, "VU Fibre Channel port B failover status")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(&H0, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H1, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H2, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H3, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H101, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H102, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H103, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H104, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H8000, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H8003, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H8010, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H8020, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&HA101, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&HA102, TapeUtils.PageData.DataItem.DataType.PageData)
        End With
        With pdata.Items.Last.PageDataTemplate
            Dim subPage As TapeUtils.PageData
            subPage = New TapeUtils.PageData With {.PageCode = 0, .Name = "Very High Frequency data"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Prevent/Allow Medium Removal bit", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Initiated Unload bit", .StartByte = 0, .BitOffset = 1, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "MAM Accessible", .StartByte = 0, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Data Compression Enabled", .StartByte = 0, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Write Protect", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Clean Requested", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Cleaning Required", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DTD Initialized", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "In Transition", .StartByte = 1, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Robotic Access Allowed", .StartByte = 1, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Present", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Seated", .StartByte = 1, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Threaded", .StartByte = 1, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Data Accessible", .StartByte = 1, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DT Device Activity", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subPage.Items.Last.EnumTranslator
                .Add(&H0, "No tape motion")
                .Add(&H1, "Cleaning operation in progress")
                .Add(&H2, "Tape being loaded")
                .Add(&H3, "Tape being unloaded")
                .Add(&H4, "Other tape activity")
                .Add(&H5, "Reading")
                .Add(&H6, "Writing")
                .Add(&H7, "Locating")
                .Add(&H8, "Rewinding")
                .Add(&H9, "Erasing")
                .Add(&HC, "Other DT device activity")
                .Add(&HD, "Microcode update in progress")
                .Add(&HE, "Reading encrypted data from tape")
                .Add(&HF, "Writing encrypted data to tape")
            End With
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "VU Extended VHF Data log parameter changed", .StartByte = 3, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Tape Diagnostic Data Entry Created", .StartByte = 3, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Parameters Present", .StartByte = 3, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Service Request", .StartByte = 3, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Recovery Requested", .StartByte = 3, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Interface Changed", .StartByte = 3, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "TapeAlert flag has changed", .StartByte = 3, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            .Add(&H0, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = 1, .Name = "Very High Frequency polling delay"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "VHF Polling Delay in ms", .StartByte = 0, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
            .Add(&H1, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = 2, .Name = "DT device ADC data encryption control status"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Aborted", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Key Management Error", .StartByte = 1, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Decryption Parameters Request", .StartByte = 1, .BitOffset = 1, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Parameters Request", .StartByte = 1, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Parameters Request Sequence Identifier", .StartByte = 2, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            .Add(&H2, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = 3, .Name = "Key management error data"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Error Type", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            subPage.Items.Last.EnumTranslator.Add(&H0, "No error")
            subPage.Items.Last.EnumTranslator.Add(&H1, "Encryption parameters request error")
            subPage.Items.Last.EnumTranslator.Add(&H2, "Decryption parameters request error")
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Key Timeout", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Parameters Request Sequence Identifier", .StartByte = 2, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            .Add(&H3, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H101, .Name = "DTD primary status - SAS/FC Port A"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Topology", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Speed", .StartByte = 0, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subPage.Items.Last.EnumTranslator
                .Add(&H0, "1 Gbps")
                .Add(&H1, "2 Gbps")
                .Add(&H2, "4 Gbps")
                .Add(&H3, "8 Gbps")
                .Add(&H4, "16 Gbps")
                .Add(&H5, "32 Gbps")
                .Add(&H6, "64 Gbps")
                .Add(&H7, "128 Gbps")
            End With
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Login Complete", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC AL_PA conflict", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Signal", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Port Initialization Complete", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current N_Port ID", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current FC_AL Loop ID", .StartByte = 7, .BitOffset = 1, .TotalBits = 7, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Port Name", .StartByte = 8, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Node Name", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Negotiated Physical Link Rate", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Current Hashed Address", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Address", .StartByte = 4, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            .Add(&H101, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H102, .Name = "DTD primary status - SAS/FC Port B"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Topology", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Speed", .StartByte = 0, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subPage.Items.Last.EnumTranslator
                .Add(&H0, "1 Gbps")
                .Add(&H1, "2 Gbps")
                .Add(&H2, "4 Gbps")
                .Add(&H3, "8 Gbps")
                .Add(&H4, "16 Gbps")
                .Add(&H5, "32 Gbps")
                .Add(&H6, "64 Gbps")
                .Add(&H7, "128 Gbps")
            End With
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Login Complete", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC AL_PA conflict", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Signal", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Port Initialization Complete", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current N_Port ID", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current FC_AL Loop ID", .StartByte = 7, .BitOffset = 1, .TotalBits = 7, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Port Name", .StartByte = 8, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Node Name", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Negotiated Physical Link Rate", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Current Hashed Address", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Address", .StartByte = 4, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            .Add(&H102, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H103, .Name = "DTD primary status - Fibre Channel NPIV port A"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Topology", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Speed", .StartByte = 0, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subPage.Items.Last.EnumTranslator
                .Add(&H0, "1 Gbps")
                .Add(&H1, "2 Gbps")
                .Add(&H2, "4 Gbps")
                .Add(&H3, "8 Gbps")
                .Add(&H4, "16 Gbps")
                .Add(&H5, "32 Gbps")
                .Add(&H6, "64 Gbps")
                .Add(&H7, "128 Gbps")
            End With
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Login Complete", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC AL_PA conflict", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Signal", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Port Initialization Complete", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current N_Port ID", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current FC_AL Loop ID", .StartByte = 7, .BitOffset = 1, .TotalBits = 7, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Port Name", .StartByte = 8, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Node Name", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Negotiated Physical Link Rate", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Current Hashed Address", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Address", .StartByte = 4, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            .Add(&H103, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H104, .Name = "DTD primary status - Fibre Channel NPIV port B"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Topology", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Speed", .StartByte = 0, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subPage.Items.Last.EnumTranslator
                .Add(&H0, "1 Gbps")
                .Add(&H1, "2 Gbps")
                .Add(&H2, "4 Gbps")
                .Add(&H3, "8 Gbps")
                .Add(&H4, "16 Gbps")
                .Add(&H5, "32 Gbps")
                .Add(&H6, "64 Gbps")
                .Add(&H7, "128 Gbps")
            End With
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Login Complete", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC AL_PA conflict", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Signal", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Port Initialization Complete", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current N_Port ID", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current FC_AL Loop ID", .StartByte = 7, .BitOffset = 1, .TotalBits = 7, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Port Name", .StartByte = 8, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Node Name", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Negotiated Physical Link Rate", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Current Hashed Address", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Address", .StartByte = 4, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            .Add(&H104, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H8000, .Name = "VU Very High Frequency data"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Prevent/Allow Medium Removal bit", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Initiated Unload bit", .StartByte = 0, .BitOffset = 1, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "MAM Accessible", .StartByte = 0, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Data Compression Enabled", .StartByte = 0, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Write Protect", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Clean Requested", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Cleaning Required", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DTD Initialized", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "In Transition", .StartByte = 1, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Robotic Access Allowed", .StartByte = 1, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Present", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Seated", .StartByte = 1, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Threaded", .StartByte = 1, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Data Accessible", .StartByte = 1, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DT Device Activity", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subPage.Items.Last.EnumTranslator
                .Add(&H0, "No tape motion")
                .Add(&H1, "Cleaning operation in progress")
                .Add(&H2, "Tape being loaded")
                .Add(&H3, "Tape being unloaded")
                .Add(&H4, "Other tape activity")
                .Add(&H5, "Reading")
                .Add(&H6, "Writing")
                .Add(&H7, "Locating")
                .Add(&H8, "Rewinding")
                .Add(&H9, "Erasing")
                .Add(&HC, "Other DT device activity")
                .Add(&HD, "Microcode update in progress")
                .Add(&HE, "Reading encrypted data from tape")
                .Add(&HF, "Writing encrypted data to tape")
            End With
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "VU Extended VHF Data log parameter changed", .StartByte = 3, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Tape Diagnostic Data Entry Created", .StartByte = 3, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Parameters Present", .StartByte = 3, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Service Request", .StartByte = 3, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Recovery Requested", .StartByte = 3, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Interface Changed", .StartByte = 3, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "TapeAlert flag has changed", .StartByte = 3, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Login", .StartByte = 4, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Hardware Error", .StartByte = 4, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Error", .StartByte = 4, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Upgrade Cartridge", .StartByte = 4, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Loading", .StartByte = 4, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Unloading", .StartByte = 4, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Snapshot", .StartByte = 5, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Load Complete", .StartByte = 5, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Unload Complete", .StartByte = 5, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            .Add(&H8000, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H8003, .Name = "VU key management error data"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Key Timeout", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Error Type", .StartByte = 0, .BitOffset = 5, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Sense Key", .StartByte = 4, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = SenseCodeTranslator
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code", .StartByte = 5, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = AdditionalSenseCodeTranslator
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code Qualifier", .StartByte = 6, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            .Add(&H8003, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H8010, .Name = "VU extended VHF data"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Multi-Initiator Conflict Warning", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Snapshot", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Hibernate Mode", .StartByte = 3, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Legacy Reservations Changed", .StartByte = 3, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Persistent Reservations Changed", .StartByte = 3, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Prevent Allow Medium Removal Changed", .StartByte = 3, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            .Add(&H8010, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H8020, .Name = "VU multi-initiator conflict warning"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Name 1 (previous)", .StartByte = 0, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Text})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Name 2 (latest)", .StartByte = 8, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Text})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Operation Code", .StartByte = 16, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Service Action", .StartByte = 17, .BitOffset = 3, .TotalBits = 5, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            .Add(&H8020, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &HA101, .Name = "VU Fibre Channel port A failover status"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Active", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Failover Trigger", .StartByte = 1, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subPage.Items.Last.EnumTranslator
                .Add(&H0, "No failover trigger has been detected.")
                .Add(&H1, "A signal loss failover trigger was detected.")
                .Add(&H2, "A link error threshold exceeded trigger was detected.")
                .Add(&H3, "A command transport error threshold exceeded trigger was detected.")
            End With
            .Add(&HA101, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &HA102, .Name = "VU Fibre Channel port B failover status"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Active", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Failover Trigger", .StartByte = 1, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subPage.Items.Last.EnumTranslator
                .Add(&H0, "No failover trigger has been detected.")
                .Add(&H1, "A signal loss failover trigger was detected.")
                .Add(&H2, "A link error threshold exceeded trigger was detected.")
                .Add(&H3, "A command transport error threshold exceeded trigger was detected.")
            End With
            .Add(&HA102, subPage)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x11.xml"), pdata.GetSerializedText())
#End Region
#Region "0x12"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H12, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "TapeAlert Response log page", .PageCode = &H12, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        Dim TapeAlertFlag As New SerializableDictionary(Of Long, String)
        With TapeAlertFlag
            .Add(1, "Read")
            .Add(2, "Write")
            .Add(3, "Hard Error")
            .Add(4, "Medium")
            .Add(5, "Read Failure")
            .Add(6, "Write Failure")
            .Add(7, "Medium Life")
            .Add(8, "Not Data Grade")
            .Add(9, "Write-Protect")
            .Add(10, "Volume Removal Prevented")
            .Add(11, "Cleaning Volume")
            .Add(12, "Unsupported Format")
            .Add(13, "Recoverable Mechanical Cartridge Failure")
            .Add(14, "Unrecoverable Mechanical Cartridge Failure")
            .Add(15, "Memory Chip in Cartridge Failure")
            .Add(16, "Forced Eject")
            .Add(17, "Read-Only Format")
            .Add(18, "Tape Directory Corrupted on Load")
            .Add(19, "Nearing Medium Life")
            .Add(20, "Cleaning Required")
            .Add(21, "Cleaning Requested")
            .Add(22, "Expired Cleaning Volume")
            .Add(23, "Invalid Cleaning Volume")
            .Add(24, "Retension Requested")
            .Add(25, "Multi-port Interface Error on Primary Port")
            .Add(26, "Cooling Fan Failure")
            .Add(27, "Power Supply Failure")
            .Add(28, "Power Consumption")
            .Add(29, "Drive Preventative Maintenance Required")
            .Add(30, "Hardware A")
            .Add(31, "Hardware B")
            .Add(32, "Primary Interface")
            .Add(33, "Eject Media")
            .Add(34, "Microcode Update Failure")
            .Add(35, "Drive Humidity")
            .Add(36, "Drive Temperature")
            .Add(37, "Drive Voltage")
            .Add(38, "Predictive Failure")
            .Add(39, "Diagnostics Required")
            .Add(49, "Diminished Native Capacity")
            .Add(50, "Lost Statistics")
            .Add(51, "Tape Directory Invalid at Unload")
            .Add(52, "Tape System Area Write Failure")
            .Add(53, "Tape System Area Read Failure")
            .Add(54, "No Start of Data")
            .Add(55, "Loading or Threading Failure")
            .Add(56, "Unrecoverable Unload Failure")
            .Add(57, "Automation Interface Failure")
            .Add(58, "Microcode Failure")
            .Add(59, "WORM Medium — Integrity Check Failed")
            .Add(60, "WORM Medium — Overwrite Attempted")
            .Add(61, "Encryption Policy Violation")
        End With
        For i As Integer = 0 To 7
            For j As Integer = 0 To 7
                Dim TAFValue As String = ""
                If TapeAlertFlag.TryGetValue(i * 8 + j, TAFValue) Then TAFValue = $" {TAFValue}"
                pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                        .Parent = pdata,
                                        .Name = $"Flag {Hex(i * 8 + j).ToUpper().PadLeft(2, "0")}h{TAFValue}",
                                        .StartByte = i + 8,
                                        .BitOffset = j,
                                        .TotalBits = 1,
                                        .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                pdata.Items.Last.EnumTranslator = TapeAlertFlag
            Next
        Next
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x12.xml"), pdata.GetSerializedText())

#End Region
#Region "0x13"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H13, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Requested Recovery log page", .PageCode = &H13, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Recovery Procedure",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Enum})
        pdata.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
        With pdata.Items.Last.EnumTranslator
            .Add(&H0, "No recovery requested")
            .Add(&HD, "Request creation of a DT device error log")
            .Add(&HE, "Retrieve a DT device error log")
            .Add(&HF, "Modify the configuration to allow microcode update")
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x13.xml"), pdata.GetSerializedText())
#End Region
#Region "0x14"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H14, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Device Statistics log page", .PageCode = &H14, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "DS",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 1,
                        .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "SPF",
                        .StartByte = 0,
                        .BitOffset = 1,
                        .TotalBits = 1,
                        .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 2,
                        .TotalBits = 6,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Subcode Page",
                        .StartByte = 1,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Device Statistics log parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(&H0, "Lifetime volume loads")
            .Add(&H1, "Lifetime cleaning operations")
            .Add(&H2, "Lifetime power-on hours")
            .Add(&H3, "Lifetime media motion (head) hours")
            .Add(&H4, "Lifetime meters of tape processed")
            .Add(&H5, "Lifetime medium motion (head) hours when an incompatible volume was last loaded")
            .Add(&H6, "Lifetime power-on hours when the last temperature condition occurred (TapeAlert code 24h)")
            .Add(&H7, "Lifetime power-on hours when the last power consumption condition occurred (TapeAlert code 1Ch)")
            .Add(&H8, "Medium motion (head) hours since the last successful cleaning operation")
            .Add(&H9, "Medium motion (head) hours since the second to last successful cleaning operation")
            .Add(&HA, "Medium motion (head) hours since the third to last successful cleaning operation")
            .Add(&HB, "Lifetime power-on hours when the last operator-initiated forced reset or emergency eject occurred")
            .Add(&HC, "Lifetime power cycles")
            .Add(&HD, "Volume loads since last parameter reset")
            .Add(&HE, "Hard write errors")
            .Add(&HF, "Hard read errors")
            .Add(&H10, "Duty cycle sample time (time in milliseconds since last hard reset)")
            .Add(&H11, "Read duty cycle (percentage of duty cycle spent processing read-type commands)")
            .Add(&H12, "Write duty cycle (percentage of duty cycle spent processing write-type commands)")
            .Add(&H13, "Activity duty cycle (percentage of duty cycle spent processing commands that move the medium)")
            .Add(&H14, "Volume not present duty cycle (percentage of duty cycle when there is no volume present)")
            .Add(&H15, "Ready duty cycle (percentage of the duty cycle sample time when the drive is in the ready state)")
            .Add(&H16, "MB transferred from the application client in the duty cycle sample time")
            .Add(&H17, "MB transferred to the application client in the duty cycle sample time")
            .Add(&H40, "Drive manufacturer’s serial number")
            .Add(&H41, "Drive serial number")
            .Add(&H80, "Medium removal prevented")
            .Add(&H81, "Max recommended mechanism temperature exceeded")
            .Add(&H1000, "Medium motion (head) hours for each medium type")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H7, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H8, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H9, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&HA, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&HB, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&HC, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&HD, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&HE, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&HF, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H10, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H11, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H12, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H13, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H14, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H15, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H16, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H17, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H40, TapeUtils.PageData.DataItem.DataType.Text）
            .Add(&H41, TapeUtils.PageData.DataItem.DataType.Text）
            .Add(&H80, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H81, TapeUtils.PageData.DataItem.DataType.Int64）
            .Add(&H1000, TapeUtils.PageData.DataItem.DataType.PageData)
        End With
        Dim DensityCodeTranslator As New SerializableDictionary(Of Long, String)
        Dim MediumTypeTranslator As New SerializableDictionary(Of Long, String)
        With DensityCodeTranslator
            .Add(&H40, "L1")
            .Add(&H42, "L2")
            .Add(&H44, "L3")
            .Add(&H46, "L4")
            .Add(&H58, "L5")
            .Add(&H5A, "L6")
            .Add(&H5C, "L7")
            .Add(&H5D, "M8")
            .Add(&H5E, "L8")
            .Add(&H60, "L9")
        End With
        With MediumTypeTranslator
            .Add(0, "RW")
            .Add(1, "WORM")
        End With
        With pdata.Items.Last.PageDataTemplate
            Dim subPage As TapeUtils.PageData
            subPage = New TapeUtils.PageData With {.PageCode = 0, .Name = "Device statistics medium type log parameter"}
            For i As Integer = 0 To 19
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Density Code", .StartByte = 2 + 8 * i, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                subPage.Items.Last.EnumTranslator = DensityCodeTranslator
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Medium Type", .StartByte = 3 + 8 * i, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                subPage.Items.Last.EnumTranslator = MediumTypeTranslator
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Medium Motion Hours", .StartByte = 4 + 8 * i, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            Next
            .Add(&H1000, subPage)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x14.xml"), pdata.GetSerializedText())
#End Region
#Region "0x15"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H15, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Service Buffers Information Log page", .PageCode = &H15, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Service Buffers",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(0, "DT Device Error log")
            .Add(3, "Health and Error log")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(&H0, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H3, TapeUtils.PageData.DataItem.DataType.PageData)
        End With
        With pdata.Items.Last.PageDataTemplate
            Dim subPage As TapeUtils.PageData
            subPage = New TapeUtils.PageData With {.PageCode = 0, .Name = "DT Device Error Log service buffer"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Temporarily Unavailable", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DT Device Error Log Service Buffer", .StartByte = 4, .BitOffset = 0, .TotalBits = 160, .Type = TapeUtils.PageData.DataItem.DataType.Text})
            .Add(&H0, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = 3, .Name = "Health and Error Log service buffer"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Temporarily Unavailable", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Health & Error Log Service Buffer", .StartByte = 4, .BitOffset = 0, .TotalBits = 160, .Type = TapeUtils.PageData.DataItem.DataType.Text})
            .Add(&H3, subPage)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x15.xml"), pdata.GetSerializedText())
#End Region
#Region "0x16"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H16, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Tape Diagnostic log page", .PageCode = &H16, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Tape diagnostic data log parameters",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        For i As Integer = 0 To 59
            With pdata.Items.Last.EnumTranslator
                .Add(i, $"Parameter Code {Hex(i).ToUpper().PadLeft(4, "0")}h")
            End With
            With pdata.Items.Last.DynamicParamType
                .Add(i, TapeUtils.PageData.DataItem.DataType.PageData)
            End With
            With pdata.Items.Last.PageDataTemplate
                Dim subPage As TapeUtils.PageData
                subPage = New TapeUtils.PageData With {.PageCode = i, .Name = $"Parameter Code {Hex(i).ToUpper().PadLeft(4, "0")}h"}
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Density Code", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Density Code Representation", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                subPage.Items.Last.EnumTranslator = DensityCodeTranslator
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Medium Type", .StartByte = 3, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Medium Type Representation", .StartByte = 3, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                subPage.Items.Last.EnumTranslator = MediumTypeTranslator
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Lifetime Medium Motion Hours", .StartByte = 4, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code", .StartByte = 10, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code Representation", .StartByte = 10, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                subPage.Items.Last.EnumTranslator = AdditionalSenseCodeTranslator
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code Qualifier", .StartByte = 11, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Drive Error Code", .StartByte = 12, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Product Revision Level", .StartByte = 16, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Hours Since Last Clean", .StartByte = 20, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Opcode", .StartByte = 24, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Service Action", .StartByte = 25, .BitOffset = 3, .TotalBits = 5, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Medium Identifier", .StartByte = 28, .BitOffset = 0, .TotalBits = 256, .Type = TapeUtils.PageData.DataItem.DataType.RawData})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Medium Identifier", .StartByte = 62, .BitOffset = 0, .TotalBits = 48, .Type = TapeUtils.PageData.DataItem.DataType.Int64})

                .Add(i, subPage)
            End With
        Next

        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x16.xml"), pdata.GetSerializedText())
#End Region
#Region "0x17"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H17, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Volume Statistics Log page", .PageCode = &H17, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Volume Statistics log parameters",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(&H0, "Page valid")
            .Add(&H1, "Thread count")
            .Add(&H2, "Total data sets written")
            .Add(&H3, "Total write retries")
            .Add(&H4, "Total unrecovered write errors")
            .Add(&H5, "Total suspended writes")
            .Add(&H6, "Total fatal suspended writes")
            .Add(&H7, "Total datasets read")
            .Add(&H8, "Total read retries")
            .Add(&H9, "Total unrecovered read errors")
            .Add(&HC, "Last mount unrecovered write errors")
            .Add(&HD, "Last mount unrecovered read errors")
            .Add(&HE, "Last mount megabytes written")
            .Add(&HF, "Last mount megabytes read")
            .Add(&H10, "Lifetime megabytes written")
            .Add(&H11, "Lifetime megabytes read")
            .Add(&H12, "Last load write compression ratio")
            .Add(&H13, "Last load read compression ratio")
            .Add(&H14, "Medium mount time")
            .Add(&H15, "Medium ready time")
            .Add(&H16, "Total native capacity")
            .Add(&H17, "Total used native capacity")
            .Add(&H40, "Volume serial number")
            .Add(&H41, "Tape lot identifier")
            .Add(&H42, "Volume barcode")
            .Add(&H43, "Volume manufacturer")
            .Add(&H44, "Volume license code")
            .Add(&H45, "Volume personality")
            .Add(&H46, "Volume manufacture date")
            .Add(&H80, "Write protect")
            .Add(&H81, "Volume is WORM")
            .Add(&H82, "Maximum recommended tape path temperature exceeded")
            .Add(&H101, "Beginning of medium passes")
            .Add(&H102, "Middle of medium passes")
            .Add(&H200, "First encrypted logical object identifiers")
            .Add(&H201, "First unencrypted logical object on the EOP side of the first encrypted logical object identifiers")
            .Add(&H202, "Approximate native capacity of partitions")
            .Add(&H203, "Approximate used native capacity of partitions")
            .Add(&H300, "Mount history")
            .Add(&HF000, "Version number (vendor-unique)")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H7, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H8, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H9, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&HC, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&HD, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&HE, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&HF, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H10, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H11, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H12, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H13, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H14, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H15, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(&H16, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H17, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H40, TapeUtils.PageData.DataItem.DataType.Text)
            .Add(&H41, TapeUtils.PageData.DataItem.DataType.Text)
            .Add(&H42, TapeUtils.PageData.DataItem.DataType.Text)
            .Add(&H43, TapeUtils.PageData.DataItem.DataType.Text)
            .Add(&H44, TapeUtils.PageData.DataItem.DataType.Text)
            .Add(&H45, TapeUtils.PageData.DataItem.DataType.Text)
            .Add(&H46, TapeUtils.PageData.DataItem.DataType.Text)
            .Add(&H80, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H81, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H82, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(&H101, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H102, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H200, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H201, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H202, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H203, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&H300, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(&HF000, TapeUtils.PageData.DataItem.DataType.Int16)
        End With
        With pdata.Items.Last.PageDataTemplate
            Dim subPage As TapeUtils.PageData
            subPage = New TapeUtils.PageData With {.PageCode = &H200, .Name = "First encrypted logical object identifiers"}
            For i As Integer = 0 To 7
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Number", .StartByte = 2 + 12 * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Record Data Counter", .StartByte = 4 + 12 * i, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Int64})
            Next
            .Add(&H200, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H201, .Name = "First unencrypted logical object on the EOP side of the first encrypted logical object identifiers"}
            For i As Integer = 0 To 7
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Number", .StartByte = 2 + 12 * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Record Data Counter", .StartByte = 4 + 12 * i, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Int64})
            Next
            .Add(&H201, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H202, .Name = "Approximate native capacity of partitions"}
            For i As Integer = 0 To 7
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Number", .StartByte = 2 + 8 * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Record Data Counter", .StartByte = 4 + 8 * i, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            Next
            .Add(&H202, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H203, .Name = "Approximate used native capacity of partitions"}
            For i As Integer = 0 To 7
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Number", .StartByte = 2 + 8 * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Record Data Counter", .StartByte = 4 + 8 * i, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            Next
            .Add(&H203, subPage)
            subPage = New TapeUtils.PageData With {.PageCode = &H300, .Name = "Mount history"}
            For i As Integer = 0 To 3
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Mount History Index", .StartByte = 2 + &H2C * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Mount History Vendor ID", .StartByte = 4 + &H2C * i, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Text})
                subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Mount History Unit Serial Number", .StartByte = 12 + &H2C * i, .BitOffset = 0, .TotalBits = 256, .Type = TapeUtils.PageData.DataItem.DataType.Text})
            Next
            .Add(&H300, subPage)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x17.xml"), pdata.GetSerializedText())
#End Region
#Region "0x18"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H18, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Protocol-Specific Port Log page (SAS drives only)", .PageCode = &H18, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Protocol-Specific Log Parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(0, "Protocol-Specific Log Parameter 0")
            .Add(1, "Protocol-Specific Log Parameter 1")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(0, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(1, TapeUtils.PageData.DataItem.DataType.PageData)
        End With
        With pdata.Items.Last.PageDataTemplate
            Dim subPage As TapeUtils.PageData
            subPage = New TapeUtils.PageData With {.PageCode = 0, .Name = "Protocol-specific log parameters"}
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Generation Code", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"PHY Identifier", .StartByte = 5, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached Device Type", .StartByte = 8, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached Reason", .StartByte = 8, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Negotiated Physical Link Rate", .StartByte = 9, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SSP Initiator Port", .StartByte = 10, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached STP Initiator Port", .StartByte = 10, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SMP Initiator Port", .StartByte = 10, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SSP Target Port", .StartByte = 11, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached STP Target Port", .StartByte = 11, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SMP Target Port", .StartByte = 11, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"SAS Address", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SAS Address", .StartByte = 20, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached PHY Identifier", .StartByte = 28, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Invalid DWORD Count", .StartByte = 36, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Running Disparity Error Count", .StartByte = 40, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Loss of DWORD synchronization", .StartByte = 44, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"PHY Reset Problem Count", .StartByte = 48, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            .Add(0, subPage)
            .Add(1, subPage)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x18.xml"), pdata.GetSerializedText())
#End Region
#Region "0x1B"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H1B, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Data Compression log page", .PageCode = &H1B, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Data Compression Parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(0, "Read compression ratio")
            .Add(1, "Write compression ratio")
            .Add(2, "Megabytes transferred to host")
            .Add(3, "Bytes transferred to host")
            .Add(4, "Megabytes read from tape")
            .Add(5, "Bytes read from tape")
            .Add(6, "Megabytes transferred from host")
            .Add(7, "Bytes transferred from host")
            .Add(8, "Megabytes written to tape")
            .Add(9, "Bytes written to tape")
            .Add(&H100, "Dara compression enabled")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(0, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(1, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(2, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(4, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(5, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(6, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(7, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(8, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(9, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(&H100, TapeUtils.PageData.DataItem.DataType.Boolean)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x1B.xml"), pdata.GetSerializedText())
#End Region
#Region "0x2E"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H2E, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "TapeAlert log page", .PageCode = &H2E, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Data Compression Parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = TapeAlertFlag,
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.DynamicParamType
            .Add(1, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(2, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(3, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(4, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(5, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(6, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(7, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(8, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(9, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(10, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(11, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(12, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(13, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(14, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(15, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(16, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(17, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(18, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(19, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(20, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(21, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(22, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(23, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(24, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(25, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(26, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(27, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(28, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(29, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(30, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(31, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(32, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(33, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(34, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(35, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(36, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(37, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(38, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(39, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(49, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(50, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(51, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(52, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(53, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(54, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(55, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(56, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(57, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(58, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(59, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(60, TapeUtils.PageData.DataItem.DataType.Boolean)
            .Add(61, TapeUtils.PageData.DataItem.DataType.Boolean)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x2E.xml"), pdata.GetSerializedText())
#End Region
#Region "0x30"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H30, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Tape Usage log page", .PageCode = &H30, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Tape Usage Log Parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(1, "Thread Count")
            .Add(2, "Total Data Sets Written")
            .Add(3, "Total Write Retries")
            .Add(4, "Total Unrecovered Write Errors")
            .Add(5, "Total Suspended Writes")
            .Add(6, "Total Fatal Suspended Writes")
            .Add(7, "Total Data Sets Read")
            .Add(8, "Total Read Retries")
            .Add(9, "Total Unrecovered Read Errors")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(1, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(2, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(4, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(5, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(6, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(7, TapeUtils.PageData.DataItem.DataType.Int64)
            .Add(8, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(9, TapeUtils.PageData.DataItem.DataType.Int16)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x30.xml"), pdata.GetSerializedText())
#End Region
#Region "0x31"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H31, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Tape Capacity log page", .PageCode = &H31, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Tape Capacity Log Parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(1, "Partition 0 Remaining Capacity")
            .Add(2, "Partition 1 Remaining Capacity")
            .Add(3, "Partition 0 Maximum Capacity")
            .Add(4, "Partition 1 Maximum Capacity")
            .Add(5, "Partition 2 Remaining Capacity")
            .Add(6, "Partition 3 Remaining Capacity")
            .Add(7, "Partition 2 Maximum Capacity")
            .Add(8, "Partition 3 Maximum Capacity")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(1, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(2, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(4, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(5, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(6, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(7, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(8, TapeUtils.PageData.DataItem.DataType.Int32)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x31.xml"), pdata.GetSerializedText())
#End Region
#Region "0x32"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H32, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Data Compression (HP-only) log page", .PageCode = &H32, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Data Compression Parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(0, "Read compression ratio x100")
            .Add(1, "Write compression ratio x100")
            .Add(2, "Megabytes transferred to host")
            .Add(3, "Bytes transferred to host")
            .Add(4, "Megabytes read from tape")
            .Add(5, "Bytes read from tape")
            .Add(6, "Megabytes transferred from host")
            .Add(7, "Bytes transferred from host")
            .Add(8, "Megabytes written to tape")
            .Add(9, "Bytes written to tape")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(0, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(1, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(2, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(4, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(5, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(6, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(7, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(8, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(9, TapeUtils.PageData.DataItem.DataType.Int32)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x32.xml"), pdata.GetSerializedText())
#End Region
#Region "0x33"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H33, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Device Wellness Log page", .PageCode = &H33, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        For i As Integer = 0 To 15
            pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = $"Parameter Code {i}",
                        .StartByte = 4 + i * 16,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
            pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = $"Time Stamp",
                        .StartByte = 8 + i * 16,
                        .BitOffset = 0,
                        .TotalBits = 32,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = $"Media Signature",
                        .StartByte = 12 + i * 16,
                        .BitOffset = 0,
                        .TotalBits = 32,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = $"Sense Key",
                        .StartByte = 16 + i * 16,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .EnumTranslator = SenseCodeTranslator,
                        .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = $"Additional Sense Code",
                        .StartByte = 17 + i * 16,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .EnumTranslator = AdditionalSenseCodeTranslator,
                        .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = $"Additional Sense Qualifier",
                        .StartByte = 18 + i * 16,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Byte})

            pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = $"Additional Error Information",
                        .StartByte = 19 + i * 16,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Byte})
        Next
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x33.xml"), pdata.GetSerializedText())
#End Region
#Region "0x34"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H34, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Performance Data log page", .PageCode = &H34, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Performance Data Log Parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(0, "Repositions per 100 MB")
            .Add(1, "Data rate into buffer")
            .Add(2, "Maximum data rate")
            .Add(3, "Current data rate")
            .Add(4, "Native data rate")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(0, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(1, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(2, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(3, TapeUtils.PageData.DataItem.DataType.Int16)
            .Add(4, TapeUtils.PageData.DataItem.DataType.Int16)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x34.xml"), pdata.GetSerializedText())
#End Region
#Region "0x35"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H35, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "DT Device Error log page", .PageCode = &H35, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "DT Device Error Log Parameters",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(0, "Hardware Error data")
            .Add(1, "Media Error data")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(0, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(1, TapeUtils.PageData.DataItem.DataType.RawData)
        End With
        With pdata.Items.Last.PageDataTemplate
            Dim subpage As TapeUtils.PageData
            subpage = New TapeUtils.PageData With {.PageCode = 0, .Name = "Hardware Error data log parameter"}
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Sense Key", .StartByte = 0, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subpage.Items.Last.EnumTranslator = SenseCodeTranslator
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Additional Sense Code", .StartByte = 1, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subpage.Items.Last.EnumTranslator = AdditionalSenseCodeTranslator
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Additional Sense Code Qualifier", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Hardware Error", .StartByte = 3, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Host Identification", .StartByte = 5, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Power-on Count", .StartByte = 13, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Power-on Time of Error (seconds)", .StartByte = 15, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Library Time of Error Hour", .StartByte = 19, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Library Time of Error Minutes", .StartByte = 20, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Library Time of Error Seconds", .StartByte = 21, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})

            .Add(0, subpage)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x35.xml"), pdata.GetSerializedText())
#End Region
#Region "0x3E"
        logdata = TapeUtils.LogSense(ConfTapeDrive, &H3E, PageControl:=1)
        pdata = New TapeUtils.PageData With {.Name = "Device Status log page", .PageCode = &H3E, .RawData = logdata}
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "DT Device Status Log Parameters",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
        With pdata.Items.Last.EnumTranslator
            .Add(0, "Device Type")
            .Add(1, "Device Status Bits")
            .Add(2, "Total Number of Loads")
            .Add(3, "Cleaning Cartridge Status")
            .Add(4, "Product Number")
        End With
        With pdata.Items.Last.DynamicParamType
            .Add(0, TapeUtils.PageData.DataItem.DataType.RawData)
            .Add(1, TapeUtils.PageData.DataItem.DataType.PageData)
            .Add(2, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
            .Add(4, TapeUtils.PageData.DataItem.DataType.PageData)
        End With
        With pdata.Items.Last.PageDataTemplate

            Dim subpage As TapeUtils.PageData
            subpage = New TapeUtils.PageData With {.PageCode = 1, .Name = "Device Status Bits"}
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Cleaning Required flag", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Cleaning Requested flag", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Exhausted Cleaning Tape flag", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Temperature", .StartByte = 1, .BitOffset = 4, .TotalBits = 2, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subpage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subpage.Items.Last.EnumTranslator
                .Add(0, "Field not supported")
                .Add(1, "Temperature OK")
                .Add(2, "Temperature degraded")
                .Add(3, "Temperature failed")
            End With
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Device Status", .StartByte = 1, .BitOffset = 6, .TotalBits = 2, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subpage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subpage.Items.Last.EnumTranslator
                .Add(0, "Field not supported")
                .Add(1, "Device status OK")
                .Add(2, "Device status degraded")
                .Add(3, "Device status failed")
            End With
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Device Status", .StartByte = 2, .BitOffset = 6, .TotalBits = 2, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subpage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subpage.Items.Last.EnumTranslator
                .Add(0, "Field not supported")
                .Add(1, "Medium status OK")
                .Add(2, "Medium status degraded")
                .Add(3, "Medium status failed")
            End With
            .Add(1, subpage)
            subpage = New TapeUtils.PageData With {.PageCode = 4, .Name = "Product Number"}
            subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Product Number", .StartByte = 0, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
            subpage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
            With subpage.Items.Last.EnumTranslator
                .Add(&H109022C, "LTO-6 full-height FC standalone")
                .Add(&H109022D, "LTO-6 full-height FC automation")
                .Add(&H109022E, "LTO-6 full-height SAS standalone")
                .Add(&H109022F, "LTO-6 full-height SAS automation")
                .Add(&H1090230, "LTO-6 half-height FC standalone")
                .Add(&H1090231, "LTO-6 half-height FC automation")
                .Add(&H1090232, "LTO-6 half-height SAS standalone")
                .Add(&H1090233, "LTO-6 half-height SAS automation")
            End With
            .Add(4, subpage)
        End With
        TextBox8.AppendText(pdata.GetSummary())
        If Not IO.Directory.Exists(IO.Path.Combine(Application.StartupPath, "logpages")) Then IO.Directory.CreateDirectory(IO.Path.Combine(Application.StartupPath, "logpages"))
        IO.File.WriteAllText(IO.Path.Combine(Application.StartupPath, "logpages\0x3E.xml"), pdata.GetSerializedText())
#End Region
    End Sub
    Public PageItem As New List(Of TapeUtils.PageData)
    Private Sub TabControl1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles TabControl1.SelectedIndexChanged
        If TabControl1.SelectedTab Is TabPage4 Then
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

    Private Sub Button12_Click(sender As Object, e As EventArgs) Handles Button12.Click
        If ComboBox4.SelectedIndex >= 0 Then
            Dim logdata As Byte() = TapeUtils.LogSense(ConfTapeDrive, PageItem(ComboBox4.SelectedIndex).PageCode, PageControl:=ComboBox5.SelectedIndex)
            PageItem(ComboBox4.SelectedIndex).RawData = logdata
            TextBox8.Text = PageItem(ComboBox4.SelectedIndex).GetSummary()
            If CheckBox5.Checked Then
                TextBox8.AppendText("Raw Data".PadLeft(41, "=").PadRight(75, "="))
                TextBox8.AppendText(vbCrLf)
                TextBox8.AppendText(Byte2Hex(PageItem(ComboBox4.SelectedIndex).RawData, True))
            End If
        End If
    End Sub
    Public TestEnabled As Boolean
    Private Sub ButtonTest_Click(sender As Object, e As EventArgs) Handles ButtonTest.Click
        If TestEnabled Then
            ButtonTest.Enabled = False
            TestEnabled = False
            Exit Sub
        End If
        If MessageBox.Show("Write will destroy everything after current position. Continue?", "Warning", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            TestEnabled = True
            Dim progval As Long = 0
            Dim running As Boolean = True
            Dim randomNum As Boolean = RadioButtonTest1.Checked
            Dim blkLen As Long = NumericUpDownTestBlkSize.Value
            Dim blkNum As Long = NumericUpDownTestBlkNum.Value
            Dim sec As Integer = -1
            Dim th As New Threading.Thread(
            Sub()
                Dim r As New Random()
                Dim b(blkLen - 1) As Byte
                Dim blist As New List(Of Byte())
                For i As Integer = 0 To 999
                    If randomNum Then
                        r.NextBytes(b)
                    End If
                    blist.Add(b.Clone())
                Next
                Invoke(Sub() TextBox8.AppendText($"Start{vbCrLf}"))
                sec = 0
                If blkLen = 0 Then
                    For i As Long = 0 To blkNum
                        If Not TestEnabled Then Exit For
                        TapeUtils.WriteFileMark(ConfTapeDrive)
                        progval = i * blkLen
                    Next
                Else
                    For i As Long = 0 To blkNum
                        If Not TestEnabled Then Exit For
                        TapeUtils.Write(ConfTapeDrive, blist(i Mod 1000), blkLen)
                        progval = i * blkLen
                    Next
                End If
                TapeUtils.Flush(ConfTapeDrive)
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
                While running
                    Threading.Thread.Sleep(1000)
                    Dim prognow As Long = progval
                    Invoke(Sub()
                               TextBox8.AppendText($"{sec}: {prognow} (+{IOManager.FormatSize(prognow - lastval)}){vbCrLf}")
                           End Sub)
                    If sec >= 0 Then sec += 1
                    lastval = prognow
                End While
                Invoke(Sub()
                           TextBox8.AppendText($"{sec}: {progval} (+{IOManager.FormatSize(progval - lastval)}){vbCrLf}")
                           TextBox8.AppendText($"End")
                       End Sub)
            End Sub)
            TextBox8.Clear()
            TextBox8.AppendText($"Preparing... {vbCrLf}")
            thprog.Start()
            ButtonTest.Text = "Stop"
            th.Start()
        End If

    End Sub

    Private Sub Button15_Click(sender As Object, e As EventArgs) Handles Button15.Click
        Dim result As New StringBuilder
        Dim WERLHeader As Byte() = TapeUtils.SCSIReadParam(ConfTapeDrive, {&H1C, &H1, &H88, &H0, &H4, &H0}, 4)
        If WERLHeader.Length <> 4 Then Exit Sub
        Dim RERLHeader As Byte() = TapeUtils.SCSIReadParam(ConfTapeDrive, {&H1C, &H1, &H87, &H0, &H4, &H0}, 4)
        If RERLHeader.Length <> 4 Then Exit Sub
        Dim WERLPageLen As Integer = WERLHeader(2)
        WERLPageLen <<= 8
        WERLPageLen = WERLPageLen Or WERLHeader(3)
        If WERLPageLen = 0 Then Exit Sub
        WERLPageLen += 4
        Dim WERLPage As Byte() = TapeUtils.SCSIReadParam(ConfTapeDrive, {&H1C, &H1, &H88, (WERLPageLen >> 8) And &HFF, WERLPageLen And &HFF, &H0}, WERLPageLen)
        Dim WERLData As String() = System.Text.Encoding.ASCII.GetString(WERLPage, 4, WERLPage.Length - 4).Split({vbCr, vbLf, vbTab}, StringSplitOptions.RemoveEmptyEntries)

        Dim RERLPageLen As Integer = RERLHeader(2)
        RERLPageLen <<= 8
        RERLPageLen = RERLPageLen Or RERLHeader(3)
        If RERLPageLen = 0 Then Exit Sub
        RERLPageLen += 4
        Dim RERLPage As Byte() = TapeUtils.SCSIReadParam(ConfTapeDrive, {&H1C, &H1, &H87, (RERLPageLen >> 8) And &HFF, RERLPageLen And &HFF, &H0}, RERLPageLen)
        Dim RERLData As String() = System.Text.Encoding.ASCII.GetString(RERLPage, 4, RERLPage.Length - 4).Split({vbCr, vbLf, vbTab}, StringSplitOptions.RemoveEmptyEntries)
        Try
            result.AppendLine($"Write Error Rate Log")
            result.AppendLine($"  Datasets Written     : {WERLData(0)}")
            result.AppendLine($"  CWI-4 Sets Written   : {WERLData(1)}")
            result.AppendLine($"  CWI-4 Set Retries    : {WERLData(2)}")
            result.AppendLine($"  Unwritable Datasets  : {WERLData(3)}")
            result.AppendLine($"  Channel | No. CCPs | C1 code err | C1 codewrd uncorr | Header err | WrPass err | C1 codewrd err rate | Bit err rate(log10) | Blk per Uncorr C1 ")
            For i As Integer = 4 To WERLData.Length - 5 Step 5
                Dim chan As Integer = (i - 4) \ 5
                Dim C1err As Integer = Integer.Parse(WERLData(i + 0), Globalization.NumberStyles.HexNumber)
                Dim C1cwerr As Integer = Integer.Parse(WERLData(i + 1), Globalization.NumberStyles.HexNumber)
                Dim Headerrr As Integer = Integer.Parse(WERLData(i + 2), Globalization.NumberStyles.HexNumber)
                Dim WrPasserr As Integer = Integer.Parse(WERLData(i + 3), Globalization.NumberStyles.HexNumber)
                Dim NoCCPs As Integer = Integer.Parse(WERLData(i + 4), Globalization.NumberStyles.HexNumber)
                result.Append("     ")
                result.Append(chan.ToString.PadRight(5))
                result.Append("| ")
                result.Append(WERLData(i + 4).PadLeft(8, "0"))
                result.Append(" |  ")
                result.Append(WERLData(i + 0).PadLeft(8, "0"))
                result.Append("   |     ")
                result.Append(WERLData(i + 1).PadLeft(8, "0"))
                result.Append("      |  ")
                result.Append(WERLData(i + 2).PadLeft(8, "0"))
                result.Append("  |  ")
                result.Append(WERLData(i + 3).PadLeft(8, "0"))
                result.Append("  |     ")
                If NoCCPs > 0 Then
                    result.Append((C1err / NoCCPs / 2).ToString("E3"))
                Else
                    result.Append("          ")
                End If
                result.Append("      |        ")
                If NoCCPs > 0 Then
                    result.Append(Math.Round(Math.Log10(C1err / NoCCPs / 2 / 1920), 2).ToString("f2"))
                Else
                    result.Append("     ")
                End If
                result.Append("        | ")
                If C1cwerr > 0 Then
                    result.AppendLine(Math.Round(NoCCPs * 2 / C1cwerr, 1).ToString("f1").PadRight(12).PadLeft(17))
                Else
                    result.AppendLine("".PadRight(12).PadLeft(17))
                End If
            Next

            result.AppendLine($"Read Error Rate Log")
            result.AppendLine($"  Datasets Read        : {RERLData(0)}")
            result.AppendLine($"  Subdataset C2 Errors : {RERLData(2)}")
            result.AppendLine($"  Dataset C2 Errors    : {RERLData(3)}")
            result.AppendLine($"  X-Chan Interpolations: {RERLData(4)}")
            result.AppendLine($"  Channel | No. CCPs | C1 code err | C1 codewrd uncorr | Header err | WrPass err | C1 codewrd err rate | Bit err rate(log10) | Blk per Uncorr C1 ")
            For i As Integer = 5 To RERLData.Length - 5 Step 5
                Dim chan As Integer = (i - 4) \ 5
                Dim C1err As Integer = Integer.Parse(RERLData(i + 0), Globalization.NumberStyles.HexNumber)
                Dim C1cwerr As Integer = Integer.Parse(RERLData(i + 1), Globalization.NumberStyles.HexNumber)
                Dim Headerrr As Integer = Integer.Parse(RERLData(i + 2), Globalization.NumberStyles.HexNumber)
                Dim WrPasserr As Integer = Integer.Parse(RERLData(i + 3), Globalization.NumberStyles.HexNumber)
                Dim NoCCPs As Integer = Integer.Parse(RERLData(i + 4), Globalization.NumberStyles.HexNumber)
                result.Append("     ")
                result.Append(chan.ToString.PadRight(5))
                result.Append("| ")
                result.Append(RERLData(i + 4).PadLeft(8, "0"))
                result.Append(" |  ")
                result.Append(RERLData(i + 0).PadLeft(8, "0"))
                result.Append("   |     ")
                result.Append(RERLData(i + 1).PadLeft(8, "0"))
                result.Append("      |  ")
                result.Append(RERLData(i + 2).PadLeft(8, "0"))
                result.Append("  |  ")
                result.Append(RERLData(i + 3).PadLeft(8, "0"))
                result.Append("  |     ")
                If NoCCPs > 0 Then
                    result.Append((C1err / NoCCPs / 2).ToString("E3"))
                Else
                    result.Append("          ")
                End If
                result.Append("      |        ")
                If NoCCPs > 0 Then
                    result.Append(Math.Round(Math.Log10(C1err / NoCCPs / 2 / 1920), 2).ToString("f2"))
                Else
                    result.Append("     ")
                End If
                result.Append("        | ")
                If C1cwerr > 0 Then
                    result.AppendLine(Math.Round(NoCCPs * 2 / C1cwerr, 1).ToString("f1").PadRight(12).PadLeft(17))
                Else
                    result.AppendLine("".PadRight(12).PadLeft(17))
                End If
            Next
        Catch ex As Exception
            result.Append(ex.ToString())
        End Try

        TextBox8.Text = result.ToString()
    End Sub
End Class