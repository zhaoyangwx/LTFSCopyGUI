Imports System
Imports Serilog
Imports Serilog.Context

Public Class TapeCopy
    Public TapeA As String, TapeB As String, Operation_Cancel_Flag As Boolean = False
    Public FlushFlag As Boolean = False
    Public progval As Long = 0
    Public progLastVal As Long = 0
    Public lastIncVal As Long = 0
    Public FlushCounter As Long = 0
    Private ReadOnly _logSessionId As String = $"tapecopy-{Guid.NewGuid().ToString("N").Substring(0, 8)}"
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        FlushFlag = True
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Flush")
                        Log.Information("Tape copy flush was requested by the user.")
                    End Using
                End Using
            End Using
        End Using
    End Sub

    Private Sub TapeCopy_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        FlushCounter = My.Settings.LTFSWriter_AutoCleanTimeThreashould
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Lifecycle")
                        Log.Information("Tape copy window loaded. AutoFlushThreshold={AutoFlushThreshold}.", FlushCounter)
                    End Using
                End Using
            End Using
        End Using
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        Dim progsnap As Integer = CInt(progval)
        lastIncVal = Math.Max(0, progsnap - progLastVal)
        progLastVal = progsnap

        If CheckBox1.Checked Then
            If lastIncVal <= My.Settings.LTFSWriter_AutoCleanUpperLim AndAlso lastIncVal >= My.Settings.LTFSWriter_AutoCleanDownLim Then
                FlushCounter -= 1
                If FlushCounter = 0 Then
                    FlushFlag = True
                    FlushCounter = My.Settings.LTFSWriter_AutoCleanTimeThreashould
                End If
            Else
                FlushCounter = My.Settings.LTFSWriter_AutoCleanTimeThreashould
            End If
        End If
    End Sub

    Private Sub SetLengthToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SetLengthToolStripMenuItem.Click
        If CheckBox3.Checked Then
            Dim targetLen As String = ""
            TapeB = TextBox2.Text
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "FileOperation")
                            Log.Information("Tape image length operation started. TapePath={TapePath}.", TapeB)
                        End Using
                    End Using
                End Using
            End Using
            Dim handleB As IntPtr
            Dim drivertype As TapeUtils.DriverType = TapeUtils.DriverTypeSetting
            TapeUtils.DriverTypeSetting = TapeUtils.DriverType.TapeStream
            If Not IO.File.Exists(TapeB) Then
                IO.File.Create(TapeB).Close()
            End If
            TapeUtils.OpenTapeDrive(TapeB, handleB)
            Dim vt As TapeImage = Nothing
            TapeStreamMapping.MappingTable.TryGetValue(handleB, vt)
            If vt IsNot Nothing Then
                targetLen = vt.CurrentStreamValidLength.ToString()
                If DisplayHelper.ShowInputDialog("Length", "", targetLen) = DialogResult.OK Then
                    Dim LenValue = Long.Parse(targetLen)
                    vt.CurrentStream.SetLength(Math.Max(vt.CurrentStreamValidLength, LenValue))
                    Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                        Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                            Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "FileOperation")
                                    Log.Information("Tape image length operation completed. TapePath={TapePath} RequestedLength={RequestedLength}.", TapeB, LenValue)
                                End Using
                            End Using
                        End Using
                    End Using
                End If
            End If
            TapeUtils.CloseTapeDrive(handleB)
            TapeUtils.DriverTypeSetting = drivertype
        End If
    End Sub

    Public Sub PrintMsg(s As String)
        Invoke(Sub() Label4.Text = $"Status: {s}")
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If Button1.Text = "Start" Then
            Button1.Text = "Stop"
            TapeA = TextBox1.Text
            TapeB = TextBox2.Text
            Dim handleA, handleB As IntPtr
            Dim drivertype As TapeUtils.DriverType = TapeUtils.DriverTypeSetting
            If drivertype = TapeUtils.DriverType.TapeStream Then drivertype = TapeUtils.DriverType.LTO
            Dim IsFileA As Boolean = CheckBox2.Checked
            Dim IsFileB As Boolean = CheckBox3.Checked
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Lifecycle")
                            Log.Information("Tape copy operation started. Source={Source} Destination={Destination} SourceIsFile={SourceIsFile} DestinationIsFile={DestinationIsFile} RequestedBlockCount={RequestedBlockCount} BlockLength={BlockLength} DriverType={DriverType}.",
                                            TapeA,
                                            TapeB,
                                            IsFileA,
                                            IsFileB,
                                            NumericUpDown1.Value,
                                            NumericUpDown2.Value,
                                            drivertype.ToString())
                        End Using
                    End Using
                End Using
            End Using
            If IsFileA Then
                TapeUtils.DriverTypeSetting = TapeUtils.DriverType.TapeStream
            Else
                TapeUtils.DriverTypeSetting = drivertype
            End If
            TapeUtils.OpenTapeDrive(TapeA, handleA)

            If IsFileB Then
                TapeUtils.DriverTypeSetting = TapeUtils.DriverType.TapeStream
                If Not IO.File.Exists(TapeB) Then
                    IO.File.Create(TapeB).Close()
                End If
            Else
                TapeUtils.DriverTypeSetting = drivertype
            End If
            TapeUtils.OpenTapeDrive(TapeB, handleB)
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Lifecycle")
                            Log.Information("Tape copy devices opened. SourceHandle={SourceHandle} DestinationHandle={DestinationHandle}.", handleA, handleB)
                        End Using
                    End Using
                End Using
            End Using
            Dim BlockCount As Integer = CInt(NumericUpDown1.Value)
            Dim BlockLen As UInteger = CUInt(NumericUpDown2.Value)
            progval = 0
            progLastVal = 0
            Dim running As Boolean = True
            Dim th As New Threading.Thread(
                Sub()
                    Dim sense(63) As Byte
                    Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                        Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                            Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Lifecycle")
                                    Log.Information("Tape copy worker started. Source={Source} Destination={Destination}.", TapeA, TapeB)
                                End Using
                            End Using
                        End Using
                    End Using
                    Invoke(Sub() Button1.Enabled = True)
                    Dim readData As Byte()
                    Dim Add_Key As UInt16
                    Dim i As Integer = 0
                    Do
                        i += 1
                        Try

                            If FlushFlag Then

                                If IsFileB Then
                                    TapeUtils.DriverTypeSetting = TapeUtils.DriverType.TapeStream
                                Else
                                    TapeUtils.DriverTypeSetting = drivertype
                                End If
                                TapeUtils.Flush(handleB)
                                FlushFlag = False
                                Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                                    Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                                        Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                            Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Flush")
                                                Log.Information("Tape copy destination flush completed. BlocksTransferred={BlocksTransferred}.", i)
                                            End Using
                                        End Using
                                    End Using
                                End Using
                            End If

                            If IsFileA Then
                                TapeUtils.DriverTypeSetting = TapeUtils.DriverType.TapeStream
                            Else
                                TapeUtils.DriverTypeSetting = drivertype
                            End If
                            readData = TapeUtils.ReadBlock(handleA, sense, BlockLen)
                            Add_Key = CUShort(CInt(sense(12)) << 8 Or sense(13))
                            Dim succ As Boolean = False
                            While Not succ
                                Dim sense2 As Byte() = Nothing

                                If IsFileB Then
                                    TapeUtils.DriverTypeSetting = TapeUtils.DriverType.TapeStream
                                Else
                                    TapeUtils.DriverTypeSetting = drivertype
                                End If
                                If readData.Length > 0 Then
                                    sense2 = TapeUtils.Write(handleB, readData)
                                    progval = i
                                ElseIf Add_Key <> 0 AndAlso Not ((Add_Key > 1 And Add_Key <> 4)) Then
                                    sense2 = TapeUtils.WriteFileMark(handleB)
                                    progval = i
                                Else
                                    succ = True
                                End If
                                If sense2 IsNot Nothing AndAlso sense2.Length > 2 AndAlso (sense2(2) And &HF) <> 0 Then
                                    Select Case MessageBox.Show(New Form With {.TopMost = True}, $"sense err {TapeUtils.Byte2Hex(sense2, True)}", "Warning", MessageBoxButtons.AbortRetryIgnore)
                                        Case DialogResult.Abort
                                            Exit Do
                                        Case DialogResult.Retry
                                            succ = False
                                        Case DialogResult.Ignore
                                            succ = True
                                            Exit While
                                    End Select
                                Else
                                    succ = True
                                End If
                            End While

                            If (Add_Key > 1 And Add_Key <> 4) AndAlso readData.Length = 0 Then
                                running = False
                                Threading.Thread.Sleep(200)
                                PrintMsg($"EOD detected. {i} blocks transferred.")
                                Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                                    Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                                        Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                            Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Lifecycle")
                                                Log.Information("Tape copy reached end of data. BlocksTransferred={BlocksTransferred}.", i)
                                            End Using
                                        End Using
                                    End Using
                                End Using
                                Exit Do
                            ElseIf Operation_Cancel_Flag Then
                                running = False
                                Threading.Thread.Sleep(200)
                                Operation_Cancel_Flag = False
                                PrintMsg($"Operation cancelled. {i} blocks transferred.")
                                Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                                    Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                                        Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                            Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Cancellation")
                                                Log.Information("Tape copy was canceled. BlocksTransferred={BlocksTransferred}.", i)
                                            End Using
                                        End Using
                                    End Using
                                End Using
                                Exit Do
                            End If
                        Catch ex As Exception
                            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Error")
                                            Log.Error(ex, "Tape copy worker iteration failed. BlocksTransferred={BlocksTransferred}.", i)
                                        End Using
                                    End Using
                                End Using
                            End Using
                            MessageBox.Show(New Form With {.TopMost = True}, ex.ToString())
                        End Try
                    Loop While i < BlockCount OrElse BlockCount <= 0

                    If IsFileB Then
                        TapeUtils.DriverTypeSetting = TapeUtils.DriverType.TapeStream
                    Else
                        TapeUtils.DriverTypeSetting = drivertype
                    End If
                    Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                        Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                            Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Cleanup")
                                    Log.Information("Tape copy cleanup flush started. BlocksTransferred={BlocksTransferred}.", i)
                                End Using
                            End Using
                        End Using
                    End Using
                    TapeUtils.Flush(TapeB)
                    Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                        Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                            Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Cleanup")
                                    Log.Information("Tape copy cleanup flush completed. BlocksTransferred={BlocksTransferred}.", i)
                                End Using
                            End Using
                        End Using
                    End Using

                    If IsFileA Then
                        TapeUtils.DriverTypeSetting = TapeUtils.DriverType.TapeStream
                    Else
                        TapeUtils.DriverTypeSetting = drivertype
                    End If
                    Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                        Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                            Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Cleanup")
                                    Log.Information("Tape copy source close started. BlocksTransferred={BlocksTransferred}.", i)
                                End Using
                            End Using
                        End Using
                    End Using
                    TapeUtils.CloseTapeDrive(handleA)
                    Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                        Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                            Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Cleanup")
                                    Log.Information("Tape copy source close completed. BlocksTransferred={BlocksTransferred}.", i)
                                End Using
                            End Using
                        End Using
                    End Using

                    If IsFileB Then
                        TapeUtils.DriverTypeSetting = TapeUtils.DriverType.TapeStream
                    Else
                        TapeUtils.DriverTypeSetting = drivertype
                    End If
                    Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                        Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                            Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Cleanup")
                                    Log.Information("Tape copy destination close started. BlocksTransferred={BlocksTransferred}.", i)
                                End Using
                            End Using
                        End Using
                    End Using
                    TapeUtils.CloseTapeDrive(handleB)
                    Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                            Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Cleanup")
                                    Log.Information("Tape copy destination close completed. BlocksTransferred={BlocksTransferred}.", i)
                                End Using
                            End Using
                        End Using
                    End Using
                    TapeUtils.DriverTypeSetting = drivertype
                    running = False
                    Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                        Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                            Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Lifecycle")
                                    Log.Information("Tape copy worker finished. BlocksTransferred={BlocksTransferred}.", i)
                                End Using
                            End Using
                        End Using
                    End Using
                    Invoke(Sub() Button1.Text = "Start")
                End Sub)
            Dim maxStr As String = ""
            If BlockCount > 0 Then maxStr = BlockCount.ToString Else maxStr = "unlimited"
            Dim thprog As New Threading.Thread(
                Sub()
                    Dim prog As Integer
                    While True
                        Dim exitflag = Not running
                        prog = CInt(Math.Min(10000, Math.Max(0, progval * 10000 / Math.Max(1, BlockCount))))
                        Invoke(Sub() ProgressBar1.Value = prog)
                        PrintMsg($"{progval}/{maxStr} (+ {(lastIncVal * (BlockLen / 1048576)).ToString("F2")}MiB/s)")
                        Threading.Thread.Sleep(100)
                        If exitflag Then Exit While
                    End While
                End Sub)
            Button1.Enabled = False
            thprog.Start()
            th.Start()
        ElseIf Button1.Text = "Stop" Then
            Operation_Cancel_Flag = True
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(TapeCopy))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "TapeCopy")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Cancellation")
                            Log.Information("Tape copy cancellation requested by the user.")
                        End Using
                    End Using
                End Using
            End Using
        End If
    End Sub
End Class
