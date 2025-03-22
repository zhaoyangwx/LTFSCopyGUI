Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Text
Imports LTFSCopyGUI.TapeUtils
Imports LTFSCopyGUI.TapeUtils.SetupAPI

Public Class ChangerTool
    Public LoadComplete As Boolean = False
    '<TypeConverter(GetType(ExpandableObjectConverter))>
    <TypeConverter(GetType(ListTypeDescriptor(Of List(Of TapeUtils.MediumChanger), TapeUtils.MediumChanger)))>
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
        Text = $"ChangerTool - {My.Application.Info.ProductName} {My.Application.Info.Version.ToString(3)}{My.Settings.Application_License}"
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
            CurrentChanger.RefreshElementStatus(Not CheckBox2.Checked)
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
                Dim ctext As String = $"{e.PrimaryVolumeTagInformation} @0x{Hex(e.ElementAddress).PadLeft(4, "0")} LUN{e.LUN}"
                Dim itext As String = e.Identifier.Replace("  ", " ").Replace("  ", " ").Replace("  ", " ")
                Dim ttext As String = e.ElementTypeCode.ToString()
                If itext.Length > 0 Then itext &= " "
                itext &= ttext
                itext = itext.TrimEnd(" ")
                If itext.Length > 0 Then ctext = $"{ctext}({itext})"
                ComboBox1.Items.Add(ctext)
            Next
            For Each e As MediumChanger.Element In EmptyElement
                Dim ctext As String = $"0x{Hex(e.ElementAddress).PadLeft(4, "0")} LUN{e.LUN}"
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
        If MessageBox.Show(New Form With {.TopMost = True}, $"{ComboBox1.SelectedItem} -> {ComboBox2.SelectedItem}", "", MessageBoxButtons.OKCancel) = DialogResult.OK Then
            Dim drv As String = $"\\.\CHANGER{CurrentChanger.DevIndex}"
            Dim LUN As Byte = FullElement(ComboBox1.SelectedIndex).LUN
            Dim srcElementIndex = ComboBox1.SelectedIndex
            Dim destElementIndex = ComboBox2.SelectedIndex
            Dim srcElement As MediumChanger.Element = FullElement(srcElementIndex)
            Dim destElement As MediumChanger.Element = EmptyElement(destElementIndex)
            Dim src As UInt32 = srcElement.ElementAddress
            Dim dest As UInt32 = destElement.ElementAddress
            srcElement.Full = Not srcElement.Full
            destElement.Full = Not destElement.Full
            destElement.Identifier = srcElement.Identifier
            destElement.IdentifierLength = srcElement.IdentifierLength
            srcElement.Identifier = ""
            srcElement.IdentifierLength = 0

            SetUILock(True)
            Dim th As New Threading.Thread(
                Sub()
                    Dim sense(63) As Byte
                    Dim succ As Boolean = False
                    Dim ex As Exception = Nothing
                    Try
                        MediumChanger.MoveMedium(drv, src, dest, sense, LUN:=LUN)
                    Catch ex
                        succ = False
                    Finally
                        succ = True
                    End Try
                    Me.Invoke(Sub()
                                  If CheckBox1.Checked Then
                                      Threading.Tasks.Task.Run(
                                      Sub()
                                          RefreshCurrentChanger()
                                          Me.Invoke(Sub()
                                                        SwitchChanger()
                                                        SetUILock(False)
                                                    End Sub)
                                      End Sub)
                                  Else
                                      Me.Invoke(Sub()
                                                    SwitchChanger()
                                                    SetUILock(False)
                                                End Sub)
                                  End If
                                  If succ Then
                                      MessageBox.Show(New Form With {.TopMost = True}, $"Finished{vbCrLf}{ParseSenseData(sense)}")
                                  Else
                                      MessageBox.Show(New Form With {.TopMost = True}, $"Error: {ex.ToString}")
                                  End If
                              End Sub)
                End Sub)
            th.Start()
        End If

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        SetUILock(True)
        Threading.Tasks.Task.Run(
            Sub()
                RefreshCurrentChanger()
                Me.Invoke(Sub()
                              SwitchChanger()
                              SetUILock(False)
                          End Sub)
            End Sub)
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        SetUILock(True)
        Threading.Tasks.Task.Run(
            Sub()
                RefreshCurrentChanger()
                Dim drv As String = $"\\.\CHANGER{CurrentChanger.DevIndex}"
                Invoke(Sub() TextBox1.Clear())
                Dim srcList As New List(Of MediumChanger.Element)
                Dim TLList As New List(Of String)
                For Each el As MediumChanger.Element In CurrentChanger.Elements
                    If el.ElementTypeCode = MediumChanger.Element.ElementTypeCodes.StorageElement Then
                        srcList.Add(el)
                        If el.Full Then TLList.Add(el.PrimaryVolumeTagInformation)
                    End If
                Next
                srcList.Sort(New Comparison(Of MediumChanger.Element)(
                     Function(a As MediumChanger.Element, b As MediumChanger.Element) As Integer
                         Return a.ElementAddress.CompareTo(b.ElementAddress)
                     End Function))
                TLList.Sort()
                Dim IsOnTarget As New Func(Of Boolean)(
                    Function() As Boolean
                        For i As Integer = 0 To TLList.Count - 1
                            If srcList(i).PrimaryVolumeTagInformation <> TLList(i) Then Return False
                        Next
                        Return True
                    End Function)
                While Not IsOnTarget()
                    Dim movecount As Integer = 0
                    For i As Integer = 0 To TLList.Count - 1
                        If Not srcList(i).Full Then
                            Dim j As Integer = 0
                            For j = 0 To srcList.Count - 1
                                If srcList(j).PrimaryVolumeTagInformation = TLList(i) Then
                                    Dim info As String = $"0x{Hex(srcList(j).ElementAddress).PadLeft(4, "0")} {srcList(j).PrimaryVolumeTagInformation} -> 0x{Hex(srcList(i).ElementAddress).PadLeft(4, "0")}{vbCrLf}"
                                    Invoke(Sub() TextBox1.AppendText(info))
                                    While True
                                        Dim sense(63) As Byte
                                        Try
                                            MediumChanger.MoveMedium(drv, srcList(j).ElementAddress, srcList(i).ElementAddress, sense, LUN:=srcList(j).LUN)
                                            Dim sensekey As Byte = sense(2) And &HF
                                            If sensekey <> 0 Then Throw New Exception("SCSI Sense Error")
                                        Catch ex As Exception
                                            Dim result As DialogResult
                                            Me.Invoke(Sub() result = MessageBox.Show(New Form With {.TopMost = True}, $"Error: {ex.ToString}{vbCrLf}{ParseSenseData(sense)}", "", MessageBoxButtons.AbortRetryIgnore))
                                            Select Case result
                                                Case DialogResult.Ignore
                                                    Exit While
                                                Case DialogResult.Cancel
                                                    Me.Invoke(Sub()
                                                                  SetUILock(False)
                                                              End Sub)
                                                    Exit Sub
                                                Case DialogResult.Retry
                                                    Continue While
                                            End Select
                                        End Try
                                        Exit While
                                    End While
                                    srcList(i).Full = True
                                    srcList(i).PrimaryVolumeTagInformation = srcList(j).PrimaryVolumeTagInformation
                                    srcList(j).Full = False
                                    srcList(j).PrimaryVolumeTagInformation = ""
                                    movecount += 1
                                    Exit For
                                End If
                            Next
                        End If
                    Next
                    If movecount = 0 Then
                        For i As Integer = TLList.Count To srcList.Count - 1
                            If Not srcList(i).Full Then
                                For j As Integer = 0 To TLList.Count - 1
                                    If srcList(j).Full AndAlso srcList(j).PrimaryVolumeTagInformation <> TLList(j) Then
                                        Dim info As String = $"0x{Hex(srcList(j).ElementAddress).PadLeft(4, "0")} {srcList(j).PrimaryVolumeTagInformation} -> 0x{Hex(srcList(i).ElementAddress).PadLeft(4, "0")}{vbCrLf}"
                                        Invoke(Sub() TextBox1.AppendText(info))
                                        While True
                                            Dim sense(63) As Byte
                                            Try
                                                MediumChanger.MoveMedium(drv, srcList(j).ElementAddress, srcList(i).ElementAddress, sense, LUN:=srcList(j).LUN)
                                                Dim sensekey As Byte = sense(2) And &HF
                                                If sensekey <> 0 Then Throw New Exception("SCSI Sense Error")
                                            Catch ex As Exception
                                                Dim result As DialogResult
                                                Me.Invoke(Sub() result = MessageBox.Show(New Form With {.TopMost = True}, $"Error: {ex.ToString}{vbCrLf}{ParseSenseData(sense)}", "", MessageBoxButtons.AbortRetryIgnore))
                                                Select Case result
                                                    Case DialogResult.Ignore
                                                        Exit While
                                                    Case DialogResult.Cancel
                                                        Me.Invoke(Sub()
                                                                      SetUILock(False)
                                                                  End Sub)
                                                        Exit Sub
                                                    Case DialogResult.Retry
                                                        Continue While
                                                End Select
                                            End Try
                                            Exit While
                                        End While
                                        srcList(i).Full = True
                                        srcList(i).PrimaryVolumeTagInformation = srcList(j).PrimaryVolumeTagInformation
                                        srcList(j).Full = False
                                        srcList(j).PrimaryVolumeTagInformation = ""
                                        movecount += 1
                                        Exit For
                                    End If
                                Next
                                Exit For
                            End If
                        Next
                    End If
                    If movecount = 0 Then Exit While
                End While
                Me.Invoke(Sub()
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

    Private Sub ChangerTool_SizeChanged(sender As Object, e As EventArgs) Handles Me.SizeChanged
        If Not LoadComplete Then Exit Sub
        SuspendLayout()
        Dim sample As New ChangerTool
        ComboBox1.Width = (Width - sample.Width) / 2 + sample.ComboBox1.Width
        ComboBox2.Width = (Width - sample.Width) / 2 + sample.ComboBox2.Width
        ComboBox2.Left = (Width - sample.Width) / 2 + sample.ComboBox2.Left
        Label1.Left = (Width - sample.Width) / 2 + sample.Label1.Left
        ResumeLayout()
    End Sub

    Private Sub ChangerTool_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Select Case e.KeyCode
            Case Keys.F12
                Dim SP1 As New SettingPanel
                SP1.Text = Text
                SP1.SelectedObject = Me
                SP1.Show()
        End Select
    End Sub
End Class