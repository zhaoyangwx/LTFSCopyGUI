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
    Private _TargetDirectory As String
    Public Property TargetDirectory As String
        Set(value As String)
            _TargetDirectory = value.TrimEnd("\")
        End Set
        Get
            Return _TargetDirectory
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
                                                            Button3_Click(Nothing, Nothing)
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
        If HashTask IsNot Nothing Then HashTask.IgnoreExisting = Not CheckBox1.Checked
        Dim th As New Threading.Thread(Sub()
                                           Try
                                               SyncLock Button1.Text
                                                   Select Case Button1.Text
                                                       Case "Start"
                                                           If CheckBox2.Checked Then
                                                               HashTask.TargetDirectory = TargetDirectory
                                                           Else
                                                               HashTask.TargetDirectory = ""
                                                           End If
                                                           HashTask.Start()
                                                       Case "Pause"
                                                           HashTask.Pause()
                                                       Case "Resume"
                                                           HashTask.Resume()
                                                   End Select
                                               End SyncLock
                                           Catch ex As Exception
                                               PrintMsg(ex.ToString)
                                           End Try
                                           Invoke(Sub() Button1.Enabled = True)
                                       End Sub)
        Button1.Enabled = False
        th.Start()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim th As New Threading.Thread(Sub()
                                           Try
                                               HashTask.Stop()
                                           Catch ex As Exception
                                               PrintMsg(ex.ToString)
                                           End Try
                                           Try
                                               Invoke(Sub() Button2.Enabled = True)

                                           Catch ex As Exception

                                           End Try
                                       End Sub)
        th.Start()
        Button2.Enabled = False
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

    Public SpeedHistory As List(Of Long) = New Long(3600 * 24) {}.ToList()
    Public FileRateHistory As List(Of Long) = New Long(3600 * 24) {}.ToList()
    Public SMaxNum As Integer = 60

    Private Sub AllToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AllToolStripMenuItem.Click
        SMaxNum = 3600 * 24
        AxTChart1.Axis.Bottom.Title.Caption = "1d"
    End Sub

    Private Sub HToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem1.Click
        SMaxNum = 3600 * 3
        AxTChart1.Axis.Bottom.Title.Caption = "3h"
    End Sub

    Private Sub HToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem2.Click
        SMaxNum = 3600 * 6
        AxTChart1.Axis.Bottom.Title.Caption = "6h"
    End Sub

    Private Sub HToolStripMenuItem3_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem3.Click
        SMaxNum = 3600 * 12
        AxTChart1.Axis.Bottom.Title.Caption = "12h"
    End Sub
    Public Class IndexedLHashDirectory
        Public LTFSIndexDir As ltfsindex.directory
        Public LHash_Dir As ltfsindex.directory
        Public Sub New(index As ltfsindex.directory, lhash As ltfsindex.directory)
            LTFSIndexDir = index
            LHash_Dir = lhash
        End Sub
    End Class
    Public Class ldirStack
        Private ldir As New List(Of ltfsindex.directory)
        Public ReadOnly Property IsEmpty
            Get
                Return ldir.Count = 0
            End Get
        End Property
        Public Sub Push(v As ltfsindex.directory)
            ldir.Add(v)
        End Sub
        Public Function Pop() As ltfsindex.directory
            Try
                Dim r As ltfsindex.directory = ldir.Last
                ldir.RemoveAt(ldir.Count - 1)
                Return r
            Catch ex As Exception
                Return Nothing
            End Try
        End Function
    End Class
    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            Try
                Dim schhash As ltfsindex
                Dim s As String = My.Computer.FileSystem.ReadAllText(OpenFileDialog1.FileName)
                If s.Contains("XMLSchema") Then
                    schhash = ltfsindex.FromXML(s)
                Else
                    schhash = ltfsindex.FromSchemaText(s)
                End If
                Dim q As New List(Of IndexedLHashDirectory)
                q.Add(New IndexedLHashDirectory(schema._directory(0), schhash._directory(0)))
                While q.Count > 0
                    Dim qtmp As New List(Of IndexedLHashDirectory)
                    For Each d As IndexedLHashDirectory In q
                        For Each f As ltfsindex.file In d.LTFSIndexDir.contents._file
                            For Each flookup As ltfsindex.file In d.LHash_Dir.contents._file
                                If flookup.name = f.name And flookup.length = f.length Then
                                    If flookup.sha1 <> "" And flookup.sha1.Length = 40 Then
                                        PrintMsg("")
                                        PrintMsg(f.fullpath)
                                        PrintMsg("    " & f.sha1 & " -> " & flookup.sha1)
                                        f.sha1 = flookup.sha1
                                    End If
                                    Exit For
                                End If
                            Next
                        Next
                        For Each sd As ltfsindex.directory In d.LTFSIndexDir.contents._directory
                            For Each dlookup As ltfsindex.directory In d.LHash_Dir.contents._directory
                                If dlookup.name = sd.name Then
                                    qtmp.Add(New IndexedLHashDirectory(sd, dlookup))
                                    Exit For
                                End If
                            Next
                        Next
                    Next
                    q = qtmp
                End While

            Catch ex As Exception
                MessageBox.Show(ex.ToString)
            End Try
            PrintMsg("Finished")
        End If

    End Sub

    Public PMaxNum As Integer = 3600 * 24
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
            While SpeedHistory.Count > PMaxNum
                SpeedHistory.RemoveAt(0)
            End While
            While FileRateHistory.Count > PMaxNum
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
            d_last = pnow
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
            SaveFileDialog1.FileName = Form1.TextBox1.Text
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
            e.Cancel = True
            MessageBox.Show("Task is still running.")
            Exit Sub
        Else
            My.Settings.ReHash = CheckBox1.Checked
            My.Settings.Save()
            Try
                HashTask.Stop()
            Catch ex As Exception

            End Try
        End If
    End Sub

End Class