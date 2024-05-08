Imports System.Runtime.InteropServices
Imports System.Text
Imports LTFSCopyGUI.TapeUtils
Imports LTFSCopyGUI.TapeUtils.SetupAPI

Public Class ChangerTool
    Public LoadComplete As Boolean = False
    Public Property LastDeviceList As List(Of TapeUtils.MediumChanger)
    Public ReadOnly Property CurrentChanger As MediumChanger
        Get
            If LastDeviceList Is Nothing Then Return Nothing
            If SelectedIndex < 0 Or SelectedIndex > LastDeviceList.Count - 1 Then Return Nothing
            Return LastDeviceList(SelectedIndex)
        End Get
    End Property
    Private _SelectedIndex As Integer
    Public Property SelectedIndex As Integer
        Set(value As Integer)
            If _SelectedIndex = value Then Exit Property
            _SelectedIndex = Math.Max(0, value)
            If Not LoadComplete Then Exit Property
            If CheckBox1.Checked Then
                SetUILock(True)
                Threading.Tasks.Task.Run(Sub()
                                             RefreshCurrentChanger()
                                             Me.Invoke(Sub()
                                                           SwitchChanger()
                                                           SetUILock(False)
                                                       End Sub)
                                         End Sub)
            Else
                SwitchChanger()
            End If
        End Set
        Get
            Return _SelectedIndex
        End Get
    End Property
    Private Sub ChangerTool_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Not New Security.Principal.WindowsPrincipal(Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(Security.Principal.WindowsBuiltInRole.Administrator) Then
            Process.Start(New ProcessStartInfo With {.FileName = Application.ExecutablePath, .Verb = "runas", .Arguments = "-l"})
            Me.Close()
            Exit Sub
        End If
        Text = $"ChangerTool - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}{My.Settings.License}"
        RefreshMCList()
        SetUILock(True)
        Threading.Tasks.Task.Run(Sub()
                                     RefreshCurrentChanger()
                                     Me.Invoke(Sub()
                                                   SwitchChanger()
                                                   SetUILock(False)
                                               End Sub)
                                 End Sub)
        LoadComplete = True
    End Sub
    Public Sub RefreshMCList()
        Dim DeviceList As List(Of MediumChanger) = GetMediumChangerList()
        LoadComplete = False
        ListBox1.Items.Clear()
        Dim DevList As List(Of TapeUtils.MediumChanger)
        DevList = DeviceList
        LastDeviceList = DeviceList
        For Each D As TapeUtils.MediumChanger In DevList
            ListBox1.Items.Add(D.ToString())
        Next
        ListBox1.SelectedIndex = Math.Min(SelectedIndex, ListBox1.Items.Count - 1)
        LoadComplete = True
        SelectedIndex = ListBox1.SelectedIndex
    End Sub
    Public FullElement, EmptyElement As List(Of MediumChanger.Element)
    Public Sub RefreshCurrentChanger()
        If CurrentChanger Is Nothing Then Exit Sub
        Try
            CurrentChanger.RefreshElementStatus()
        Catch ex As Exception

        End Try
    End Sub
    Public Sub SwitchChanger()
        If CurrentChanger Is Nothing OrElse CurrentChanger.Elements Is Nothing OrElse CurrentChanger.Elements.Count = 0 Then RefreshCurrentChanger()
        Try
            TextBox1.Text = CurrentChanger.GetSerializedText()
            FullElement = New List(Of MediumChanger.Element)
            EmptyElement = New List(Of MediumChanger.Element)
            For Each e As MediumChanger.Element In CurrentChanger.Elements
                If e.Full Then FullElement.Add(e) Else EmptyElement.Add(e)
            Next
            ComboBox1.Items.Clear()
            ComboBox2.Items.Clear()
            For Each e As MediumChanger.Element In FullElement
                Dim ctext As String = $"{e.PrimaryVolumeTagInformation} @0x{Hex(e.ElementAddress).PadLeft(4, "0")}"
                Dim itext As String = e.Identifier.Replace("  ", " ").Replace("  ", " ").Replace("  ", " ")
                Dim ttext As String = e.ElementTypeCode.ToString()
                If itext.Length > 0 Then itext &= " "
                itext &= ttext
                itext = itext.TrimEnd(" ")
                If itext.Length > 0 Then ctext = $"{ctext}({itext})"
                ComboBox1.Items.Add(ctext)
            Next
            For Each e As MediumChanger.Element In EmptyElement
                Dim ctext As String = $"0x{Hex(e.ElementAddress).PadLeft(4, "0")}"
                Dim itext As String = e.Identifier.Replace("  ", " ").Replace("  ", " ").Replace("  ", " ")
                Dim ttext As String = e.ElementTypeCode.ToString()
                If itext.Length > 0 Then itext &= " "
                itext &= ttext
                itext = itext.TrimEnd(" ")
                If itext.Length > 0 Then ctext = $"{ctext}({itext})"
                ComboBox2.Items.Add(ctext)
            Next
        Catch ex As Exception

        End Try

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        RefreshMCList()
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged
        If Not LoadComplete Then Exit Sub
        SelectedIndex = ListBox1.SelectedIndex
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If ComboBox1.SelectedIndex < 0 Then Exit Sub
        If ComboBox2.SelectedIndex < 0 Then Exit Sub
        If FullElement Is Nothing Then Exit Sub
        If EmptyElement Is Nothing Then Exit Sub
        If ComboBox1.SelectedIndex > FullElement.Count - 1 Then Exit Sub
        If ComboBox2.SelectedIndex > EmptyElement.Count - 1 Then Exit Sub
        If MessageBox.Show(New Form With {.TopMost = True}, $"{ComboBox1.SelectedItem} -> {ComboBox2.SelectedItem}", "", MessageBoxButtons.OKCancel) Then
            Dim drv As String = $"\\.\CHANGER{CurrentChanger.DevIndex}"
            Dim src As UInt32 = FullElement(ComboBox1.SelectedIndex).ElementAddress
            Dim dest As UInt32 = EmptyElement(ComboBox2.SelectedIndex).ElementAddress
            SetUILock(True)
            Dim th As New Threading.Thread(
                Sub()
                    Try
                        MediumChanger.MoveMedium(drv, src, dest)
                    Catch ex As Exception
                        Me.Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, $"Error: {ex.ToString}"))
                    Finally
                        Me.Invoke(Sub() MessageBox.Show(New Form With {.TopMost = True}, "Finished"))
                    End Try
                    SetUILock(False)
                    Me.Invoke(Sub()
                                  If CheckBox1.Checked Then
                                      SetUILock(True)
                                      Threading.Tasks.Task.Run(
                                      Sub()
                                          RefreshCurrentChanger()
                                          Me.Invoke(Sub()
                                                        SwitchChanger()
                                                        SetUILock(False)
                                                    End Sub)
                                      End Sub)
                                  End If
                              End Sub)
                End Sub)
            th.Start()
        End If

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Threading.Tasks.Task.Run(
            Sub()
                RefreshCurrentChanger()
                Me.Invoke(Sub()
                              SwitchChanger()
                              SetUILock(False)
                          End Sub)
            End Sub)
    End Sub
    Public Sub SetUILock(Lock As Boolean)
        Me.Invoke(Sub()
                      For Each c As Control In Me.Controls
                          c.Enabled = Not Lock
                      Next
                  End Sub)
    End Sub
End Class