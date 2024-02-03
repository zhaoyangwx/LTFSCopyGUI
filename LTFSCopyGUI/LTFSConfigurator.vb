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
                    Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(ConfTapeDrive, cdb, cdbData.Length, dataBufferPtr, dataData.Length, TextBox10.Text, 60000, senseBufferPtr)
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

                        'Mode Select:1st Partition to Minimum 
                        Invoke(Sub() TextBox8.AppendText("MODE SELECT.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H15, &H10, 0, 0, &H10, 0}, {0, 0, &H10, 0, &H11, &HA, 1, 1, &H3C, 3, 9, 0, 0, 1, &HFF, &HFF}, 0) Then
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
        TapeUtils._TapeSCSIIOCtlFull(ConfTapeDrive, cdb, 6, data, 0, 2, 60000, sense)
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
            My.Computer.FileSystem.WriteAllBytes(SaveFileDialog2.FileName, DumpData, False)
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
            If My.Computer.FileSystem.GetDirectoryInfo(FolderBrowserDialog1.SelectedPath).GetFiles("*.bin", IO.SearchOption.TopDirectoryOnly).Length > 0 Then
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
            Button24.Enabled = True
            TextBox8.Text = ""
            Dim log As Boolean = CheckBox2.Checked
            Dim thprog As New Threading.Thread(
                Sub()
                    Dim ReadLen As Integer = NumericUpDown7.Value
                    Dim FileNum As Integer = 0

                    'Position
                    Dim param As Byte() = TapeUtils.SCSIReadParam(ConfTapeDrive, {&H34, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 20)

                    Dim Partition As Byte = param(1)
                    Dim Block As UInteger = 0
                    For i As Integer = 4 To 7
                        Block <<= 8
                        Block = Block Or param(i)
                    Next
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
            If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema")) Then
                My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(Application.StartupPath, "schema"))
            End If
            outputfile = My.Computer.FileSystem.CombinePath(Application.StartupPath, outputfile)
            My.Computer.FileSystem.WriteAllBytes(outputfile, data, False)
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
                        'Mode Select:1st Partition to Minimum 
                        Invoke(Sub() TextBox8.AppendText("MODE SELECT - Partition mode page.."))
                        If TapeUtils.SendSCSICommand(ConfTapeDrive, {&H15, &H10, 0, 0, &H10, 0}, {0, 0, &H10, 0, &H11, &HA, MaxExtraPartitionAllowed, 1, &H3C, 3, 9, 0, 0, 1, &HFF, &HFF}, 0) Then
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
End Class