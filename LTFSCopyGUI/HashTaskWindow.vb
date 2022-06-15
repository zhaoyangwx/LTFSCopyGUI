Imports System.ComponentModel

Public Class HashTaskWindow
    Public schema As ltfsindex
    Public HashTask As IOManager.HashTask
    Private tval, tmax, dval, dmax, ssum, smax As Long
    Private ddelta, fdelta As Long
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
        Try
            Me.Invoke(Sub()
                          'If TextBox1.Text.Length > 20000 Then TextBox1.Text = Mid(TextBox1.Text, 18000)

                          TextBox1.AppendText(vbCrLf & Message)
                          'TextBox1.Select(TextBox1.Text.Length, 0)
                          'TextBox1.ScrollToCaret()

                      End Sub)
        Catch ex As Exception

        End Try

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
                                              Me.Invoke(Sub()
                                                            Button1.Text = "Start"
                                                            ProgressBar1.Value = ProgressBar1.Maximum
                                                            ProgressBar2.Value = ProgressBar2.Maximum
                                                        End Sub)
                                          End Sub
        AddHandler HashTask.ErrorOccured, Sub(s As String)
                                              PrintMsg(s)
                                          End Sub
        AddHandler HashTask.ProgressReport, Sub(s As String)
                                                Me.Invoke(Sub()
                                                              Try
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
                                                                             IOManager.FormatSize(ddelta) & "/s) Total: " &
                                                                             IOManager.FormatSize(ssum + dval) & "/" & IOManager.FormatSize(smax)
                                                                      If smax <> 0 Then
                                                                          ProgressBar1.Value = (ssum + dval) / smax * ProgressBar1.Maximum
                                                                      End If
                                                                  Else
                                                                      PrintMsg(s)
                                                                  End If
                                                              Catch ex As Exception

                                                              End Try

                                                          End Sub)
                                            End Sub
        Try
            AxTChart1.Series(0).Clear()
            AxTChart1.Axis.Bottom.Maximum = SMaxNum
            AxTChart1.Series(0).AddArray(SpeedHistory.Count, SpeedHistory.ToArray)
        Catch ex As Exception
            PrintMsg(ex.ToString)
        End Try
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim th As New Threading.Thread(Sub()
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
                                       End Sub)
        th.Start()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim th As New Threading.Thread(Sub()
                                           HashTask.Stop()
                                       End Sub)
        th.Start()

    End Sub


    Private Sub SToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SToolStripMenuItem.Click
        SMaxNum = 60
        AxTChart1.Axis.Bottom.Title.Caption = "60s"
    End Sub

    Private Sub MinToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MinToolStripMenuItem.Click
        SMaxNum = 300
        AxTChart1.Axis.Bottom.Title.Caption = "5min"
    End Sub

    Private Sub MinToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles MinToolStripMenuItem1.Click
        SMaxNum = 600
        AxTChart1.Axis.Bottom.Title.Caption = "10min"
    End Sub

    Private Sub MinToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles MinToolStripMenuItem2.Click
        SMaxNum = 1800
        AxTChart1.Axis.Bottom.Title.Caption = "30min"
    End Sub

    Private Sub HToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem.Click
        SMaxNum = 3600
        AxTChart1.Axis.Bottom.Title.Caption = "1h"
    End Sub

    Private Sub LinearToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LinearToolStripMenuItem.Click
        AxTChart1.Axis.Left.Logarithmic = False
        LinearToolStripMenuItem.Checked = True
        LogrithmToolStripMenuItem.Checked = False
    End Sub

    Private Sub LogrithmToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LogrithmToolStripMenuItem.Click
        AxTChart1.Axis.Left.Logarithmic = True
        LinearToolStripMenuItem.Checked = False
        LogrithmToolStripMenuItem.Checked = True
    End Sub

    Private Sub WordwrapToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles WordwrapToolStripMenuItem.Click
        TextBox1.WordWrap = WordwrapToolStripMenuItem.Checked
    End Sub

    Public SpeedHistory As List(Of Long) = New Long(3600) {}.ToList()
    Public FileRateHistory As List(Of Long) = New Long(3600) {}.ToList()
    Public SMaxNum As Integer = 60
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Static d_last As Long = 0
        Static t_last As Long = 0
        Try
            Dim pnow As Long = ssum + dval
            If pnow >= d_last Then
                ddelta = pnow - d_last
                d_last = pnow
            End If
            If tval >= t_last Then
                fdelta = tval - t_last
                t_last = tval
            End If
            SpeedHistory.Add(ddelta / 1048576)
            FileRateHistory.Add(fdelta)
            While SpeedHistory.Count > 3600
                SpeedHistory.RemoveAt(0)
            End While
            While FileRateHistory.Count > 3600
                FileRateHistory.RemoveAt(0)
            End While

            AxTChart1.Series(0).Clear()
            AxTChart1.Series(0).AddArray(SMaxNum, SpeedHistory.GetRange(SpeedHistory.Count - SMaxNum, SMaxNum).ToArray())
            AxTChart1.Series(1).Clear()
            AxTChart1.Series(1).AddArray(SMaxNum, FileRateHistory.GetRange(FileRateHistory.Count - SMaxNum, SMaxNum).ToArray())

            Text = "[" & tval & "/" & tmax & "] " & IOManager.FormatSize(dval) &
               "/" & IOManager.FormatSize(dmax) & " (" &
               IOManager.FormatSize(ddelta) & "/s) Total: " &
               IOManager.FormatSize(ssum + dval) & "/" & IOManager.FormatSize(smax)
            If smax <> 0 Then
                ProgressBar1.Value = Math.Min((ssum + dval) / smax * ProgressBar1.Maximum, ProgressBar1.Maximum)
            End If
            If TextBox1.Text.Length > TextBox1.MaxLength Then
                TextBox1.Text = Mid(TextBox1.Text, TextBox1.Text.Length - TextBox1.MaxLength / 3 * 2)
                TextBox1.Select(TextBox1.Text.Length, 0)
                TextBox1.ScrollToCaret()
            End If
        Catch ex As Exception
            PrintMsg(ex.ToString)
        End Try
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
        If HashTask.Status <> IOManager.HashTask.TaskStatus.Idle Then
            MessageBox.Show("Task is still running.")
            e.Cancel = True
            Exit Sub
        End If
        My.Settings.ReHash = CheckBox1.Checked
        My.Settings.Save()
        Try
            HashTask.Stop()
        Catch ex As Exception

        End Try
    End Sub

End Class