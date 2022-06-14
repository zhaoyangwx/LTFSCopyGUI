Imports System.ComponentModel

Public Class HashTaskWindow
    Public schema As ltfsindex
    Public HashTask As IOManager.HashTask
    Private tval, tmax, dval, dmax, ssum, smax As Long
    Private ddelta As Long
    Private _BaseDirectory As String
    Public Property BaseDirectory As String
        Set(value As String)
            _BaseDirectory = value
            If HashTask IsNot Nothing Then HashTask.BaseDirectory = value
        End Set
        Get
            Return _BaseDirectory
        End Get
    End Property
    Public Sub PrintMsg(Message As String)
        Me.Invoke(Sub()
                      If TextBox1.Text.Length > 10000 Then TextBox1.Text = Mid(TextBox1.Text, TextBox1.Text.IndexOf(vbCrLf) + 3)
                      TextBox1.Text &= vbCrLf & Message
                      TextBox1.Select(TextBox1.Text.Length, 0)
                      TextBox1.ScrollToCaret()
                  End Sub)
    End Sub

    Private Sub HashTaskWindow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckBox1.Checked = My.Settings.ReHash
        If HashTask Is Nothing Then HashTask = New IOManager.HashTask With {.schema = schema, .BaseDirectory = BaseDirectory}
        AddHandler HashTask.TaskStarted, Sub(s As String)
                                             PrintMsg(s)
                                             Me.Invoke(Sub() Button1.Text = "Pause")
                                         End Sub
        AddHandler HashTask.TaskPaused, Sub(s As String)
                                            PrintMsg(s)
                                            Me.Invoke(Sub() Button1.Text = "Resume")
                                        End Sub
        AddHandler HashTask.TaskResumed, Sub(s As String)
                                             PrintMsg(s)
                                             Me.Invoke(Sub() Button1.Text = "Pause")
                                         End Sub
        AddHandler HashTask.TaskCancelled, Sub(s As String)
                                               PrintMsg(s)
                                               Me.Invoke(Sub() Button1.Text = "Start")
                                           End Sub
        AddHandler HashTask.TaskFinished, Sub(s As String)
                                              PrintMsg(s)
                                              Me.Invoke(Sub() Button1.Text = "Start")
                                          End Sub
        AddHandler HashTask.ErrorOccured, Sub(s As String)
                                              PrintMsg(s)
                                          End Sub
        AddHandler HashTask.ProgressReport, Sub(s As String)
                                                Me.Invoke(Sub()
                                                              If s.StartsWith("#") Then
                                                                  If s.StartsWith("#val") Then
                                                                      ProgressBar1.Value = Math.Min(ProgressBar1.Maximum, Val(s.Substring(4)))
                                                                  ElseIf s.StartsWith("#max") Then
                                                                      ProgressBar1.Maximum = Val(s.Substring(4))
                                                                  ElseIf s.StartsWith("#text") Then
                                                                      Text = s.Substring(5)
                                                                  ElseIf s.StartsWith("#fval") Then
                                                                      ProgressBar2.Value = Math.Min(ProgressBar2.Maximum, Val(s.Substring(5)))
                                                                  ElseIf s.StartsWith("#fmax") Then
                                                                      ProgressBar2.Maximum = Val(s.Substring(5))
                                                                  ElseIf s.StartsWith("#tval") Then
                                                                      tval = s.Substring(5)
                                                                  ElseIf s.StartsWith("#tmax") Then
                                                                      tmax = s.Substring(5)
                                                                  ElseIf s.StartsWith("#dval") Then
                                                                      dval = s.Substring(5)
                                                                  ElseIf s.StartsWith("#dmax") Then
                                                                      dmax = s.Substring(5)
                                                                  ElseIf s.StartsWith("#ssum") Then
                                                                      ssum = s.Substring(5)
                                                                  ElseIf s.StartsWith("#smax") Then
                                                                      smax = s.Substring(5)
                                                                  End If
                                                                  Text = "[" & tval & "/" & tmax & "] " & IOManager.FormatSize(dval) &
                                                                         "/" & IOManager.FormatSize(dmax) & " (" &
                                                                         IOManager.FormatSize(ddelta) & "/s) Total:" &
                                                                         IOManager.FormatSize(ssum + dval) & "/" & IOManager.FormatSize(smax)
                                                              Else
                                                                  PrintMsg(s)
                                                              End If
                                                          End Sub)
                                            End Sub
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        SyncLock Button1.Text
            Select Case Button1.Text
                Case "Start"
                    HashTask.Start()
                Case "Pause"
                    HashTask.Pause()
                Case "Resume"
                    HashTask.Resume()
            End Select
        End SyncLock
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        HashTask.Stop()
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Static d_last As Long = 0
        Dim pnow As Long = ssum + dval
        If pnow > d_last Then ddelta = pnow - d_last
        d_last = pnow
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If HashTask IsNot Nothing Then HashTask.IgnoreExisting = Not CheckBox1.Checked
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If schema IsNot Nothing Then
            If SaveFileDialog1.ShowDialog = DialogResult.OK Then
                My.Computer.FileSystem.WriteAllText(SaveFileDialog1.FileName, schema.GetSerializedText, False)
                PrintMsg("Saved to " & SaveFileDialog1.FileName)
            End If
        End If
    End Sub

    Private Sub HashTaskWindow_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Button2_Click(sender, e)
    End Sub

    Private Sub HashTaskWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.ReHash = CheckBox1.Checked
        My.Settings.Save()
    End Sub
End Class