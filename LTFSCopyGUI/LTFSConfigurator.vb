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
            Button8.Enabled = (CurDrive.DriveLetter <> "")
            Button9.Enabled = (CurDrive.DriveLetter <> "")
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
            If CurDrive.DriveLetter <> "" And ComboBox1.Text <> "" Then
                Panel1.Enabled = False
                Dim dL As Char = ComboBox1.Text
                Dim th As New Threading.Thread(
                    Sub()
                        Dim result As String
                        Try
                            result = TapeUtils.LoadTapeDrive(dL, True)
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
        End If
        If Panel1.Enabled Then RefreshUI()

    End Sub

    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
        If Not LoadComplete Then Exit Sub
        Dim CurDrive As TapeUtils.TapeDrive = GetCurDrive()
        If CurDrive IsNot Nothing Then
            If CurDrive.DriveLetter <> "" And ComboBox1.Text <> "" Then
                Panel1.Enabled = False
                Dim dL As Char = ComboBox1.Text
                Dim th As New Threading.Thread(
                    Sub()
                        Dim result As String
                        Try
                            result = TapeUtils.EjectTapeDrive(dL)
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
End Class