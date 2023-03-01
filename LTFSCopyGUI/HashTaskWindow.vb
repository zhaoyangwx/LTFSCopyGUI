Imports System.ComponentModel

Public Class HashTaskWindow
    Public schema As ltfsindex
    Public HashTask As IOManager.HashTask
    Private tval, tmax, dval, dmax, ssum, smax As Long
    Private ddelta, fdelta As Long
    Private _BaseDirectory As String
    Public LogEnabled As Boolean = True
    Public Property ErrorCount As Integer = 0
    Public StartTime As String = Now.ToString("yyyyMMdd_HHmmss")
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

                          TextBox1.AppendText(Message & vbCrLf)
                          'TextBox1.Select(TextBox1.Text.Length, 0)
                          'TextBox1.ScrollToCaret()
                          If LogEnabled Then
                              If Not My.Computer.FileSystem.DirectoryExists(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, "log")) Then
                                  My.Computer.FileSystem.CreateDirectory(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, "log"))
                              End If
                              My.Computer.FileSystem.WriteAllText(My.Computer.FileSystem.CombinePath(My.Computer.FileSystem.CurrentDirectory, "log\log_" & StartTime & ".txt"), Message & vbCrLf, True)
                          End If
                      End Sub)
        Catch ex As Exception

        End Try

    End Sub

    Private Sub HashTaskWindow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckBox1.Checked = My.Settings.ReHash
        If HashTask Is Nothing Then HashTask = New IOManager.HashTask With {.schema = schema, .BaseDirectory = BaseDirectory}
        If My.Computer.FileSystem.FileExists(My.Computer.FileSystem.CurrentDirectory & "\recovery.log") Then
            HashTask.LogFile = My.Computer.FileSystem.ReadAllText(My.Computer.FileSystem.CurrentDirectory & "\recovery.log").Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        End If
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
                                                            Dim thEject As New Threading.Thread(
                                                                Sub()
                                                                    Dim result As String
                                                                    Try
                                                                        result = TapeUtils.EjectTapeDrive(BaseDirectory(0))
                                                                    Catch ex As Exception
                                                                        result = ex.ToString
                                                                    End Try
                                                                    If result = "" Then result = "Tape Ejected."
                                                                    Me.Invoke(Sub() PrintMsg(result))
                                                                    If ErrorCount > 0 Then PrintMsg($"{ErrorCount} errors occured.")
                                                                End Sub)
                                                            If CheckBox3.Checked Then thEject.Start()
                                                            Button3_Click(Nothing, Nothing)
                                                        End Sub)
                                          End Sub
        AddHandler HashTask.ErrorOccured, Sub(s As String)
                                              Threading.Interlocked.Increment(ErrorCount)
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
            Chart1.Series(0).Points.Clear()
            Dim i As Integer = 0
            For Each val As Double In SpeedHistory
                Chart1.Series(0).Points.AddXY(i, val)
                i += 1
            Next
            'Chart1.ChartAreas(0).AxisX.Maximum = SMaxNum
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
        Chart1.Titles(0).Text = "60s"
    End Sub

    Private Sub MinToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MinToolStripMenuItem.Click
        SMaxNum = 300
        Chart1.Titles(0).Text = "5min"
    End Sub

    Private Sub MinToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles MinToolStripMenuItem1.Click
        SMaxNum = 600
        Chart1.Titles(0).Text = "10min"
    End Sub

    Private Sub MinToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles MinToolStripMenuItem2.Click
        SMaxNum = 1800
        Chart1.Titles(0).Text = "30min"
    End Sub

    Private Sub HToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem.Click
        SMaxNum = 3600
        Chart1.Titles(0).Text = "1h"
    End Sub

    Private Sub LinearToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LinearToolStripMenuItem.Click
        Chart1.ChartAreas(0).AxisY.IsLogarithmic = False
        LinearToolStripMenuItem.Checked = True
        LogrithmToolStripMenuItem.Checked = False
    End Sub

    Private Sub LogrithmToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LogrithmToolStripMenuItem.Click
        Chart1.ChartAreas(0).AxisY.IsLogarithmic = True
        LinearToolStripMenuItem.Checked = False
        LogrithmToolStripMenuItem.Checked = True
    End Sub

    Private Sub WordwrapToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles WordwrapToolStripMenuItem.Click
        TextBox1.WordWrap = WordwrapToolStripMenuItem.Checked
    End Sub

    Public PMaxNum As Integer = 3600 * 6
    Public SpeedHistory As List(Of Double) = (New Double(PMaxNum) {}).ToList()
    Public FileRateHistory As List(Of Double) = (New Double(PMaxNum) {}).ToList()
    Public SMaxNum As Integer = 600

    Private Sub HToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem1.Click
        SMaxNum = 3600 * 3
        Chart1.Titles(0).Text = "3h"
    End Sub

    Private Sub HToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles HToolStripMenuItem2.Click
        SMaxNum = 3600 * 6
        Chart1.Titles(0).Text = "6h"
    End Sub

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
                Dim q As New List(Of IOManager.IndexedLHashDirectory)
                q.Add(New IOManager.IndexedLHashDirectory(schema._directory(0), schhash._directory(0)))
                While q.Count > 0
                    Dim qtmp As New List(Of IOManager.IndexedLHashDirectory)
                    For Each d As IOManager.IndexedLHashDirectory In q
                        For Each f As ltfsindex.file In d.LTFSIndexDir.contents._file
                            Try
                                For Each flookup As ltfsindex.file In d.LHash_Dir.contents._file
                                    If flookup.name = f.name And flookup.length = f.length Then
                                        If flookup.sha1 IsNot Nothing Then
                                            If flookup.sha1 <> "" And flookup.sha1.Length = 40 Then
                                                PrintMsg("")
                                                PrintMsg(f.name)
                                                PrintMsg("    " & f.sha1 & " -> " & flookup.sha1)
                                                f.sha1 = flookup.sha1
                                            End If
                                        End If
                                        Exit For
                                    End If
                                Next
                            Catch ex As Exception
                                PrintMsg(ex.ToString)
                            End Try
                        Next
                        For Each sd As ltfsindex.directory In d.LTFSIndexDir.contents._directory
                            For Each dlookup As ltfsindex.directory In d.LHash_Dir.contents._directory
                                If dlookup.name = sd.name Then
                                    qtmp.Add(New IOManager.IndexedLHashDirectory(sd, dlookup))
                                    Exit For
                                End If
                            Next
                        Next
                    Next
                    q = qtmp
                End While

            Catch ex As Exception
                PrintMsg(ex.ToString)
            End Try
            PrintMsg("Finished")
        End If

    End Sub

    Private Sub NumericUpDown1_ValueChanged(sender As Object, e As EventArgs) Handles NumericUpDown1.ValueChanged
        If HashTask Is Nothing Then Exit Sub
        If NumericUpDown1.Value >= 1 Then HashTask.BufferWrite = NumericUpDown1.Value
    End Sub

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


            Chart1.Series(0).Points.Clear()
            Dim i As Integer = 0
            For Each val As Double In SpeedHistory.GetRange(SpeedHistory.Count - SMaxNum, SMaxNum)
                Chart1.Series(0).Points.AddXY(i, val)
                i += 1
            Next
            Chart1.Series(1).Points.Clear()
            i = 0
            For Each val As Double In FileRateHistory.GetRange(FileRateHistory.Count - SMaxNum, SMaxNum)
                Chart1.Series(1).Points.AddXY(i, val)
                i += 1
            Next


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
                My.Computer.FileSystem.WriteAllText(SaveFileDialog1.FileName, schema.GetSerializedText, False, New System.Text.UTF8Encoding(False))
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