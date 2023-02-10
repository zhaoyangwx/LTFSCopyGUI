Imports System.Runtime.InteropServices
Imports System.Text

Public Class LTFSConfigurator
    Private LoadComplete As Boolean = False
    Private _SelectedIndex As Integer
    Public Function GetCurDrive() As TapeUtils.TapeDrive
        Dim dlist As List(Of TapeUtils.TapeDrive) = DeviceList
        If dlist.Count <> ListBox1.Items.Count Then
            RefreshUI()
        End If
        If dlist.Count = 0 Then Return Nothing
        Return dlist(SelectedIndex)
    End Function

    Public Property SelectedIndex As Integer
        Set(value As Integer)
            _SelectedIndex = Math.Max(0, value)
            If Not LoadComplete Then Exit Property
            Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
            If CurDrive Is Nothing Then
                Button6.Enabled = False
                Button7.Enabled = False
                Button8.Enabled = False
                Button9.Enabled = False
                Button10.Enabled = False
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
            Button7.Enabled = (CurDrive.DriveLetter <> "")
            Button8.Enabled = True
            Button9.Enabled = True
            Button10.Enabled = (CurDrive.DriveLetter <> "")
        End Set
        Get
            Return _SelectedIndex
        End Get
    End Property
    Public ReadOnly Property DeviceList As List(Of TapeUtils.TapeDrive)
        Get
            Return TapeUtils.GetTapeDriveList()
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
    Public Sub RefreshUI()
        LoadComplete = False
        ListBox1.Items.Clear()
        For Each D As TapeUtils.TapeDrive In DeviceList
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
        RefreshUI()
        ComboBox2.SelectedIndex = 2
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
        RefreshUI()
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter = "" And ComboBox1.Text <> "" Then
                Dim result As String = TapeUtils.MapTapeDrive(ComboBox1.Text, "TAPE" & CurDrive.DevIndex)
                If result = "" Then result = "已将驱动器 TAPE" & CurDrive.DevIndex & " 挂载到" & ComboBox1.Text & ":"
                result &= vbCrLf
                TextBox2.AppendText(result)
            End If
        End If
        RefreshUI()
    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter <> "" Then
                Dim result As String = TapeUtils.UnMapTapeDrive(ComboBox1.Text)
                If result = "" Then result = "已卸载驱动器 TAPE" & CurDrive.DevIndex & " 的盘符" & ComboBox1.Text & ":"
                result &= vbCrLf
                TextBox2.AppendText(result)
            End If
        End If
        RefreshUI()
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
                        'result = TapeUtils.LoadTapeDrive(dL, True)
                        result = TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 1, 0})
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = "驱动器 TAPE" & CurDrive.DevIndex & " 已加载磁带"
                               result &= vbCrLf
                               TextBox2.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI()
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI()

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
                        'result = TapeUtils.EjectTapeDrive(dL)
                        result = TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 0, 0})
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = "驱动器 TAPE" & CurDrive.DevIndex & " 已弹出磁带"
                               result &= vbCrLf
                               TextBox2.AppendText(result)
                               Panel1.Enabled = True
                               RefreshUI()
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI()
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter <> "" And ComboBox1.Text <> "" Then
                Dim result As String = TapeUtils.MountTapeDrive(ComboBox1.Text)
                If result = "" Then result = "驱动器 TAPE" & CurDrive.DevIndex & " 已挂载"
                result &= vbCrLf
                TextBox2.AppendText(result)
            End If
        End If
        RefreshUI()
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        Panel2.Visible = CheckBox1.Checked
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
        If bytes Is Nothing Then Return ""
        If bytes.Length = 0 Then Return ""
        Dim sb As New StringBuilder
        Dim tb As String = ""
        For i As Integer = 0 To bytes.Length - 1
            If i Mod 16 = 0 And TextShow Then
                sb.Append(Hex(i).PadLeft(6) & "h:  ")
            End If
            sb.Append(Convert.ToString((bytes(i) And &HFF) + &H100, 16).Substring(1).ToUpper)
            sb.Append(" ")
            Dim c As Char = Chr(bytes(i))
            If Char.IsControl(c) Then
                tb &= "."
            Else
                tb &= c
            End If
            If i Mod 16 = 15 Then
                If TextShow Then
                    sb.Append("    " & tb)
                End If
                sb.Append(vbCrLf)
                tb = ""
            End If
        Next
        If TextShow And tb <> "" Then
            sb.Append("    " & tb)
        End If
        Return sb.ToString()
    End Function
    Private Sub Button12_Click(sender As Object, e As EventArgs) Handles Button12.Click
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
                    Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(TextBox5.Text, cdb, cdbData.Length, dataBufferPtr, dataData.Length, TextBox10.Text, 60000, senseBufferPtr)
                    Marshal.Copy(dataBufferPtr, dataData, 0, dataData.Length)
                    Marshal.Copy(senseBufferPtr, senseBuffer, 0, senseBuffer.Length)
                    Me.Invoke(Sub()
                                  TextBox8.Text = "DataBuffer" & vbCrLf
                                  TextBox8.Text &= Byte2Hex(dataData)
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
                        'result = TapeUtils.LoadTapeDrive(dL, True)
                        result = TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 9, 0})
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = "驱动器 TAPE" & CurDrive.DevIndex & " 已进仓"
                               result &= vbCrLf
                               TextBox2.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI()
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI()
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
                        'result = TapeUtils.LoadTapeDrive(dL, True)
                        result = TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, &HA, 0})
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = "驱动器 TAPE" & CurDrive.DevIndex & " 已退带"
                               result &= vbCrLf
                               TextBox2.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI()
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI()
    End Sub

    Private Sub Button15_Click(sender As Object, e As EventArgs) Handles Button15.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBox1.Text
            Dim th As New Threading.Thread(
                Sub()
                    Invoke(Sub() TextBox8.Text = "Start erase ..." & vbCrLf)
                    Try
                        'result = TapeUtils.LoadTapeDrive(dL, True)

                        'Load and Thread
                        Invoke(Sub() TextBox8.AppendText("Loading.."))
                        If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 1, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Mode Select:1st Partition to Minimum 
                        Invoke(Sub() TextBox8.AppendText("MODE SELECT.."))
                        If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H15, &H10, 0, 0, &H10, 0}, {0, 0, &H10, 0, &H11, &HA, 1, 1, &H3C, 3, 9, 0, 0, 1, &HFF, &HFF}, 0) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If

                        'Format
                        Invoke(Sub() TextBox8.AppendText("Partitioning.."))
                        If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {4, 0, 1, 0, 0, 0}, Nothing, 0) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        For i As Integer = 1 To NumericUpDown6.Value
                            'Unthread
                            Invoke(Sub() TextBox8.AppendText("Unthreading.."))
                            If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, &HA, 0}) Then
                                Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                            Else
                                Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                                Exit Try
                            End If
                            'Thread
                            Invoke(Sub() TextBox8.AppendText("Threading.."))
                            If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 1, 0}) Then
                                Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                            Else
                                Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                                Exit Try
                            End If
                            'Erase
                            Invoke(Sub() TextBox8.AppendText("Erasing " & i & "/" & NumericUpDown6.Value & ".."))
                            If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H19, 1, 0, 0, 0, 0}) Then
                                Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                            Else
                                Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                                Exit Try
                            End If
                        Next
                        'Unthread
                        Invoke(Sub() TextBox8.AppendText("Unthreading.."))
                        If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, &HA, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Thread
                        Invoke(Sub() TextBox8.AppendText("Threading.."))
                        If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 1, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Remove Partition
                        Invoke(Sub() TextBox8.AppendText("Reinitializing.."))
                        If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {4, 0, 0, 0, 0, 0}) Then
                            Invoke(Sub() TextBox8.AppendText("     OK" & vbCrLf))
                        Else
                            Invoke(Sub() TextBox8.AppendText("     Fail" & vbCrLf))
                            Exit Try
                        End If
                        'Unload
                        Invoke(Sub() TextBox8.AppendText("Unloading.."))
                        If TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 0, 0}) Then
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
                               RefreshUI()
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI()
    End Sub

    Private Sub Button16_Click(sender As Object, e As EventArgs) Handles Button16.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBox1.Text
            Dim barcode As String = TextBox9.Text
            Dim cdb As Byte() = {&H8D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, &H29, 0, 0}
            Dim data As Byte() = {0, 0, 0, &H29, &H8, &H6, &H1, 0, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20,
                &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20}
            For i As Integer = 0 To barcode.Length - 1
                data(9 + i) = CByte(Asc(barcode(i)) And &HFF)
            Next
            Dim th As New Threading.Thread(
                Sub()
                    Dim result As String = ""
                    Try
                        result &= TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, cdb, data, 0)
                        result = result.Replace("True", "").Replace("False", "Failed")
                    Catch ex As Exception
                        result = ex.ToString()
                    End Try
                    Invoke(Sub()
                               If result = "" Then result = "驱动器 TAPE" & CurDrive.DevIndex & " Barcode已修改为 " & barcode
                               result &= vbCrLf
                               TextBox2.AppendText(result)
                           End Sub)
                    Invoke(Sub()
                               Panel1.Enabled = True
                               RefreshUI()
                           End Sub)
                End Sub)
            th.Start()
        End If
        If Panel1.Enabled Then RefreshUI()
    End Sub

    Private Sub Label11_Click(sender As Object, e As EventArgs) Handles Label11.Click
    End Sub

    Private Sub Button17_Click(sender As Object, e As EventArgs) Handles Button17.Click
        Dim ResultB As Byte() = TapeUtils.GetMAMAttributeBytes("\\.\TAPE" & GetCurDrive().DevIndex, NumericUpDown8.Value, NumericUpDown9.Value)
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
    Private Sub Button18_Click(sender As Object, e As EventArgs) Handles Button18.Click
        Me.Enabled = False
        Dim tapeDrive As String = "\\.\TAPE" & GetCurDrive().DevIndex
        TextBox8.Text = ""
        TextBox8.AppendText("========Application Info========" & vbCrLf)
        Try
            Dim BC As String = TapeUtils.ReadBarcode(tapeDrive)
            TextBox8.AppendText("Barcode: " & BC & vbCrLf)
        Catch ex As Exception
            TextBox8.AppendText("Barcode: Not Available" & vbCrLf)
        End Try
        Try
            Dim AppInfo As String = TapeUtils.ReadAppInfo(tapeDrive)
            TextBox8.AppendText("Application: " & AppInfo & vbCrLf)
        Catch ex As Exception
            TextBox8.AppendText("Application: Not Available" & vbCrLf)
        End Try
        TextBox8.AppendText(vbCrLf)
        TextBox8.AppendText("==========Medium Usage==========" & vbCrLf)
        Try
            Dim LoadCount As Int64 = TapeUtils.MAMAttribute.FromTapeDrive(tapeDrive, 0, 3).AsNumeric
            TextBox8.AppendText("Load count: " & LoadCount & vbCrLf)
        Catch ex As Exception
            TextBox8.AppendText("Load count: Not Available" & vbCrLf)
        End Try
        Try
            Dim TotalWriteMBytes As Int64 = TapeUtils.MAMAttribute.FromTapeDrive(tapeDrive, 2, &H20).AsNumeric
            TextBox8.AppendText("Total write: " & ReduceDataUnit(TotalWriteMBytes) & vbCrLf)
        Catch ex As Exception
            TextBox8.AppendText("Total write: Not Available" & vbCrLf)
        End Try
        Try
            Dim TotalReadMBytes As Int64 = TapeUtils.MAMAttribute.FromTapeDrive(tapeDrive, 2, &H21).AsNumeric
            TextBox8.AppendText("Total read: " & ReduceDataUnit(TotalReadMBytes) & vbCrLf)
        Catch ex As Exception
            TextBox8.AppendText("Total read: Not Available" & vbCrLf)
        End Try
        'TextBox8.AppendText("Tape pulled: " & TapePulledMeter & " m" & vbCrLf)
        TextBox8.AppendText(vbCrLf)
        TextBox8.AppendText("=========Medium Identity========" & vbCrLf)
        Try
            Dim Medium_SN As String = TapeUtils.MAMAttribute.FromTapeDrive(tapeDrive, 4, 1).AsString
            TextBox8.AppendText("Serial number: " & Medium_SN & vbCrLf)
        Catch ex As Exception
            TextBox8.AppendText("Serial number: Not Available" & vbCrLf)
        End Try
        Try
            Dim Medium_Manufacturer As String = TapeUtils.MAMAttribute.FromTapeDrive(tapeDrive, 4, 0).AsString
            TextBox8.AppendText("Manufacturer: " & Medium_Manufacturer & vbCrLf)
        Catch ex As Exception
            TextBox8.AppendText("Manufacturer: Not Available" & vbCrLf)
        End Try
        Try
            Dim Medium_Man_Date As String = TapeUtils.MAMAttribute.FromTapeDrive(tapeDrive, 4, 6).AsString
            TextBox8.AppendText("Manufacture date: " & Medium_Man_Date & vbCrLf)
        Catch ex As Exception
            TextBox8.AppendText("Manufacture date: Not Available" & vbCrLf)
        End Try
        Try
            Dim Medium_Type As Byte = TapeUtils.MAMAttribute.FromTapeDrive(tapeDrive, 4, 8).AsNumeric
            Dim CMData As Byte() = TapeUtils.RawDump(tapeDrive, &H10)
            Dim Medium_ParticleType As String
            If CMData(&H40) >= &H40 Then
                If CMData(&H6A) And &HF Then
                    Medium_ParticleType = "BaFe"
                Else
                    Medium_ParticleType = "MP"
                End If
            Else
                If CMData(&H6A) Then
                    Medium_ParticleType = "BaFe"
                Else
                    Medium_ParticleType = "MP"
                End If
            End If
            If Medium_Type = 1 Then Medium_ParticleType = "Universal Clean Cartridge"
            TextBox8.AppendText("Particle Type: " & Medium_ParticleType & vbCrLf)
            TextBox8.AppendText(vbCrLf)
            TextBox8.AppendText("==========CM RAW DATA===========" & vbCrLf)
            TextBox8.AppendText("Length: " & CMData.Length & vbCrLf)
            TextBox8.Text &= (Byte2Hex(CMData, True))
        Catch ex As Exception
            TextBox8.AppendText("Particle Type: Not Available" & vbCrLf)
        End Try
        Me.Enabled = True
    End Sub

    Private Sub Button19_Click(sender As Object, e As EventArgs) Handles Button19.Click
        If SaveFileDialog1.ShowDialog = DialogResult.OK Then
            Dim tapeDrive As String = "\\.\TAPE" & GetCurDrive().DevIndex
            Button19.Enabled = False

            Dim th As New Threading.Thread(
                Sub()
                    Dim MAMData As New TapeUtils.MAMAttributeList
                    For i As UInt16 = &H0 To &HFFFF Step 1

                        Try
                            Dim Attr As TapeUtils.MAMAttribute = TapeUtils.MAMAttribute.FromTapeDrive(tapeDrive, i)
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
                    Me.Invoke(Sub() Button19.Enabled = True)
                End Sub)
            th.Start()
        End If
    End Sub

    Private Sub Button20_Click(sender As Object, e As EventArgs) Handles Button20.Click
        Dim tapeDrive As String = "\\.\TAPE" & GetCurDrive().DevIndex
        Me.Enabled = False
        Dim cdbData As Byte() = {1, 0, 0, 0, 0, 0}
        Dim cdb As IntPtr = Marshal.AllocHGlobal(6)
        Marshal.Copy(cdbData, 0, cdb, 6)
        Dim data As IntPtr = Marshal.AllocHGlobal(1)
        Dim sense As IntPtr = Marshal.AllocHGlobal(127)
        TapeUtils._TapeSCSIIOCtlFull(tapeDrive, cdb, 6, data, 0, 2, 60000, sense)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(data)
        Marshal.FreeHGlobal(sense)
        Me.Enabled = True
    End Sub

    Private Sub Button21_Click(sender As Object, e As EventArgs) Handles Button21.Click
        Dim tapeDrive As String = "\\.\TAPE" & GetCurDrive().DevIndex
        Me.Enabled = False
        Dim ReadLen As Integer = NumericUpDown7.Value
        Dim cdbData As Byte() = {8, 0, ReadLen >> 16 And &HFF, ReadLen >> 8 And &HFF, ReadLen And &HFF, 0}
        Dim cdb As IntPtr = Marshal.AllocHGlobal(6)
        Marshal.Copy(cdbData, 0, cdb, 6)
        Dim readData(ReadLen - 1) As Byte
        Dim data As IntPtr = Marshal.AllocHGlobal(ReadLen)
        Marshal.Copy(readData, 0, data, ReadLen)
        Dim sense As IntPtr = Marshal.AllocHGlobal(127)
        TapeUtils._TapeSCSIIOCtlFull(tapeDrive, cdb, 6, data, ReadLen, 1, &HFFFF, sense)
        Marshal.Copy(data, readData, 0, ReadLen)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(data)
        Marshal.FreeHGlobal(sense)
        TextBox8.Text = Byte2Hex(readData, True)
        Me.Enabled = True
    End Sub

    Private Sub Button22_Click(sender As Object, e As EventArgs) Handles Button22.Click
        Dim tapeDrive As String = "\\.\TAPE" & GetCurDrive().DevIndex
        Me.Enabled = False
        Dim BufferID = Convert.ToByte(ComboBox2.SelectedItem.Substring(0, 2), 16)
        Dim DumpData As Byte() = TapeUtils.RawDump(tapeDrive, BufferID)
        TextBox8.Text = "Buffer len=" & DumpData.Length & vbCrLf
        SaveFileDialog2.FileName = ComboBox2.SelectedItem & ".bin"
        If SaveFileDialog2.ShowDialog = DialogResult.OK Then
            My.Computer.FileSystem.WriteAllBytes(SaveFileDialog2.FileName, DumpData, False)
        End If
        TextBox8.Text &= Byte2Hex(DumpData, True)
        Me.Enabled = True
    End Sub

End Class