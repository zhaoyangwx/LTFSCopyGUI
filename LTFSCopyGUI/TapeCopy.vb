Public Class TapeCopy
    Public TapeA As String, TapeB As String, Operation_Cancel_Flag As Boolean = False
    Public Sub PrintMsg(s As String)
        Invoke(Sub() Label4.Text = $"Status: {s}")
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If Button1.Text = "Start" Then
            Button1.Text = "Stop"
            TapeA = TextBox1.Text
            TapeB = TextBox2.Text
            Dim BlockCount As Integer = NumericUpDown1.Value
            Dim BlockLen As Integer = NumericUpDown1.Value
            Dim progval As Integer = 0
            Dim running As Boolean  = true
            Dim th As New Threading.Thread(
                Sub()
                    Dim sense(63) As Byte
                    Me.Invoke(Sub() Button1.Enabled = True)
                    For i As Integer = 1 To BlockCount

                        Dim readData As Byte() = TapeUtils.ReadBlock(TapeA, sense, BlockLen)
                        Dim Add_Key As UInt16 = CInt(sense(12)) << 8 Or sense(13)
                        Dim succ As Boolean = False
                        While Not succ
                            Dim sense2 As Byte() = Nothing
                            If readData.Length > 0 Then
                                sense2 = TapeUtils.Write(TapeB, readData)
                                progval = i
                            ElseIf Add_Key <> 0 Then
                                sense2 = TapeUtils.WriteFileMark(TapeB)
                                progval = i
                            Else
                                succ = True
                            End If
                            If sense2 IsNot Nothing AndAlso sense2.Length > 2 AndAlso (sense(2) And &HF) <> 0 Then
                                Select Case MessageBox.Show($"sense err {TapeUtils.Byte2Hex(sense2, True)}", "Warning", MessageBoxButtons.AbortRetryIgnore)
                                    Case DialogResult.Abort
                                        Exit For
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

                        If (Add_Key > 1 And Add_Key <> 4) Then
                            PrintMsg($"EOD detected. {i} blocks transferred.")
                            Exit For
                        ElseIf Operation_Cancel_Flag Then
                            Operation_Cancel_Flag = False
                            PrintMsg($"Operation cancelled. {i} blocks transferred.")
                            Exit For
                        End If
                    Next
                    running = False
                    Invoke(Sub() Button1.Text = "Start")
                End Sub)
            Dim thprog As New Threading.Thread(
                Sub()
                    Me.Invoke(Sub()
                                  While running
                                      ProgressBar1.Value = Math.Min(10000, Math.Max(0, progval * 10000 / BlockCount))
                                      PrintMsg($"{progval}/{BlockCount}")
                                      Threading.Thread.Sleep(100)
                                  End While
                              End Sub)
                End Sub)
            Button1.Enabled = False
            thprog.Start()
            th.Start()
        ElseIf Button1.Text = "Stop" Then
            Operation_Cancel_Flag = True
        End If
    End Sub
End Class