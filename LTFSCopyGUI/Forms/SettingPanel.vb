Imports System
Imports Serilog
Imports Serilog.Context

Public Class SettingPanel
    Dim result As DialogResult = DialogResult.Cancel
    Private ReadOnly _logSessionId As String = $"settings-{Guid.NewGuid().ToString("N").Substring(0, 8)}"
    Public Property SelectedObject As Object
        Get
            Return PropertyGrid1.SelectedObject
        End Get
        Set(value As Object)
            PropertyGrid1.SelectedObject = value
        End Set
    End Property
    Private Sub PropertyGrid1_PropertyValueChanged(s As Object, e As PropertyValueChangedEventArgs) Handles PropertyGrid1.PropertyValueChanged
        result = DialogResult.OK
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(SettingPanel))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "Settings")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Changed")
                        Log.Information("Setting value changed. PropertyName={PropertyName}.", If(e.ChangedItem Is Nothing, String.Empty, e.ChangedItem.Label))
                    End Using
                End Using
            End Using
        End Using
    End Sub

    Private Sub SettingPanel_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        DialogResult = result
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(SettingPanel))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "Settings")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "WindowClose")
                        Log.Information("Settings window closed. DialogResult={DialogResult}.", result.ToString())
                    End Using
                End Using
            End Using
        End Using
    End Sub

    Private Sub 打开ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 打开ToolStripMenuItem.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(SettingPanel))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "Settings")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Import")
                            Log.Information("Settings import started. FilePath={FilePath}.", OpenFileDialog1.FileName)
                        End Using
                    End Using
                End Using
            End Using
            Try
                SettingImportExport.LoadFromFile(OpenFileDialog1.FileName)
                PropertyGrid1.SelectedObject = My.MySettings.Default
                result = DialogResult.OK
            Catch ex As Exception
                Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(SettingPanel))
                    Using categoryScope As IDisposable = LogContext.PushProperty("Category", "Settings")
                        Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                            Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Error")
                                Log.Error(ex, "Settings import failed. FilePath={FilePath}.", OpenFileDialog1.FileName)
                            End Using
                        End Using
                    End Using
                End Using
                Throw
            End Try
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(SettingPanel))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "Settings")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Import")
                            Log.Information("Settings import completed. FilePath={FilePath}.", OpenFileDialog1.FileName)
                        End Using
                    End Using
                End Using
            End Using
        End If
    End Sub

    Private Sub 保存ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 保存ToolStripMenuItem.Click
        If SaveFileDialog1.ShowDialog = DialogResult.OK Then
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(SettingPanel))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "Settings")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Export")
                            Log.Information("Settings export started. FilePath={FilePath}.", SaveFileDialog1.FileName)
                        End Using
                    End Using
                End Using
            End Using
            Try
                IO.File.WriteAllText(SaveFileDialog1.FileName, SettingImportExport.GetSerializedText())
            Catch ex As Exception
                Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(SettingPanel))
                    Using categoryScope As IDisposable = LogContext.PushProperty("Category", "Settings")
                        Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                            Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Error")
                                Log.Error(ex, "Settings export failed. FilePath={FilePath}.", SaveFileDialog1.FileName)
                            End Using
                        End Using
                    End Using
                End Using
                Throw
            End Try
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(SettingPanel))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "Settings")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Export")
                            Log.Information("Settings export completed. FilePath={FilePath}.", SaveFileDialog1.FileName)
                        End Using
                    End Using
                End Using
            End Using
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        result = DialogResult.OK
        Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        result = DialogResult.Cancel
        Close()
    End Sub
End Class
