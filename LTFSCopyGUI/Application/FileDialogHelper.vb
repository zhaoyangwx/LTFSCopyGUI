Imports System.IO
Imports Microsoft.WindowsAPICodePack.Dialogs

Public Module FileDialogHelper
    Public Function SelectFolder(Optional initialDirectory As String = Nothing) As String
        Using dialog As New CommonOpenFileDialog()
            dialog.IsFolderPicker = True
            dialog.Multiselect = False

            If Not String.IsNullOrWhiteSpace(initialDirectory) AndAlso Directory.Exists(initialDirectory) Then
                dialog.InitialDirectory = initialDirectory
            End If

            If dialog.ShowDialog() = CommonFileDialogResult.Ok Then
                Return dialog.FileName
            End If
        End Using

        Return Nothing
    End Function
End Module
