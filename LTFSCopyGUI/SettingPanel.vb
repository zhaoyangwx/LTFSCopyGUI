Public Class SettingPanel
    Dim result As DialogResult = DialogResult.Cancel
    Private Sub PropertyGrid1_PropertyValueChanged(s As Object, e As PropertyValueChangedEventArgs) Handles PropertyGrid1.PropertyValueChanged
        result = DialogResult.OK
    End Sub

    Private Sub SettingPanel_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        DialogResult = result
    End Sub

    Private Sub 打开ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 打开ToolStripMenuItem.Click
        If OpenFileDialog1.ShowDialog = DialogResult.OK Then
            SettingImportExport.LoadFromFile(OpenFileDialog1.FileName)
            PropertyGrid1.SelectedObject = My.MySettings.Default
            result = DialogResult.OK
        End If
    End Sub

    Private Sub 保存ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 保存ToolStripMenuItem.Click
        If SaveFileDialog1.ShowDialog = DialogResult.OK Then
            IO.File.WriteAllText(SaveFileDialog1.FileName, SettingImportExport.GetSerializedText())
        End If
    End Sub
End Class