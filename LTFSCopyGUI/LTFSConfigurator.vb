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

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    Dim succ As Boolean = TapeUtils._TapeSCSIIOCtl(TextBox3.Text, NumericUpDown1.Value)
                    If succ Then
                        MessageBox.Show("1")
                    Else
                        MessageBox.Show("0")
                    End If
                Catch ex As Exception
                    MessageBox.Show(ex.ToString)
                End Try
                Me.Invoke(Sub() Panel2.Enabled = True)
            End Sub)
        Panel2.Enabled = False
        th.Start()
    End Sub

    Private Sub Button11_Click(sender As Object, e As EventArgs) Handles Button11.Click
        Dim th As New Threading.Thread(
            Sub()
                Try
                    Dim succ As Boolean = TapeUtils._TapeDeviceIOCtl(TextBox4.Text, NumericUpDown2.Value << 16 Or NumericUpDown5.Value << 14 Or NumericUpDown3.Value << 2 Or NumericUpDown4.Value)
                    If succ Then
                        MessageBox.Show("1")
                    Else
                        MessageBox.Show("0")
                    End If
                Catch ex As Exception
                    MessageBox.Show(ex.ToString)
                End Try
                Me.Invoke(Sub() Panel2.Enabled = True)
            End Sub)
        Panel2.Enabled = False
        th.Start()
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
    Public Function Byte2Hex(bytes As Byte()) As String
        If bytes Is Nothing Then Return ""
        If bytes.Length = 0 Then Return ""
        Dim sb As New StringBuilder
        For i As Integer = 0 To bytes.Length - 1
            sb.Append(Convert.ToString((bytes(i) And &HFF) + &H100, 16).Substring(1).ToUpper)
            sb.Append(" ")
            If i Mod 16 = 15 Then
                sb.Append(vbCrLf)
            End If
        Next
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
                        dataBufferPtr = Marshal.AllocHGlobal(127)
                    End If
                    Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(127)

                    Dim senseBuffer(127) As Byte
                    Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(TextBox5.Text, cdb, cdbData.Length, dataBufferPtr, dataData.Length, 2, 300, senseBufferPtr)
                    Marshal.Copy(dataBufferPtr, dataData, 0, dataData.Length)
                    Marshal.Copy(senseBufferPtr, senseBuffer, 0, senseBuffer.Length)
                    Me.Invoke(Sub()
                                  TextBox8.Text = "DataBuffer" & vbCrLf
                                  TextBox8.Text &= Byte2Hex(dataData)
                                  TextBox8.Text &= vbCrLf & vbCrLf & "SenseBuffer" & vbCrLf
                                  TextBox8.Text &= Byte2Hex(senseBuffer)
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
                    Dim result As String = ""
                    Try
                        'result = TapeUtils.LoadTapeDrive(dL, True)
                        For i As Integer = 1 To NumericUpDown6.Value
                            result &= TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, &HA, 0}) 'Unthread
                            result &= TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 1, 0}) 'Thread
                            result &= TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H19, 1, 0, 0, 0, 0}) 'Erase
                        Next
                        result &= TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, &HA, 0}) 'Unthread
                        result &= TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 1, 0}) 'Thread
                        result &= TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {4, 0, 0, 0, 0, 0}) 'Remove Partition
                        result &= TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, {&H1B, 0, 0, 0, 0, 0}) 'Unload
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

    Private Sub Button16_Click(sender As Object, e As EventArgs) Handles Button16.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            Panel1.Enabled = False
            Dim dL As Char = ComboBox1.Text
            Dim barcode As String = TextBox9.Text
            Dim cdb As Byte() = {&H8D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, &H29, 0, 0}
            Dim data As Byte() = {0, 0, 0, &H29, &H8, &H6, &H1, 0, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20}
            For i As Integer = 0 To barcode.Length - 1
                data(9 + i) = CByte(Asc(barcode(i)) And &HFF)
            Next
            Dim th As New Threading.Thread(
                Sub()
                    Dim result As String = ""
                    Try
                        result &= TapeUtils.SendSCSICommand("\\.\TAPE" & CurDrive.DevIndex, cdb, data)
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
End Class