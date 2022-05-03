Imports LTFSCopyGUI

Public Class FileBrowser
    Public Property schema As ltfsindex
    Public Overloads Shared Sub Show(FList As ltfsindex)
        Dim FB1 As New FileBrowser
        With FB1
            .schema = FList
            .Show()
        End With
    End Sub
    Public Overloads Shared Function ShowDialog(FList As ltfsindex) As DialogResult
        Dim FB1 As New FileBrowser
        With FB1
            .schema = FList
            Return .ShowDialog()
        End With
    End Function
    Private Sub FileBrowser_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SyncLock EventLock
            If EventLock Then Exit Sub
            EventLock = True
        End SyncLock
        TreeView1.Nodes.Clear()
        If schema IsNot Nothing Then
            AddItem(TreeView1.Nodes, New ltfsindex.contentsDef With {._directory = schema._directory, ._file = schema._file})
        End If
        For Each n As TreeNode In TreeView1.Nodes
            RefreshChackState(n)
        Next
        SyncLock EventLock
            EventLock = False
        End SyncLock

    End Sub
    Private Sub AddItem(Root As TreeNodeCollection, FList As ltfsindex.contentsDef)
        If FList Is Nothing Then Exit Sub
        If Root Is Nothing Then Exit Sub
        Try
            For i As Integer = 0 To FList._directory.Count - 1
                Dim newitem As TreeNode = Root.Add(FList._directory(i).name)
                newitem.Tag = FList._directory(i)
                AddItem(newitem.Nodes, FList._directory(i).contents)
            Next
            For i As Integer = 0 To FList._file.Count - 1
                Dim newitem As TreeNode = Root.Add(FList._file(i).name)
                newitem.Tag = FList._file(i)
                newitem.Checked = FList._file(i).Selected
            Next
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
        End Try

    End Sub

    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect

    End Sub
    Public Sub RecursivelySetNodeCheckStatus(n As TreeNode, Checked As Boolean)
        n.Checked = Checked
        For Each nc As TreeNode In n.Nodes
            RecursivelySetNodeCheckStatus(nc, Checked)
        Next
    End Sub
    Public Sub RefreshIndexSelection(n As Object, Selected As Boolean)
        If TypeOf (n) Is ltfsindex.file Then
            CType(n, ltfsindex.file).Selected = Selected
        End If
        If TypeOf (n) Is ltfsindex.directory Then
            CType(n, ltfsindex.directory).Selected = Selected
        End If
    End Sub
    Public Function RefreshChackState(n As TreeNode) As CheckState
        If n.Nodes Is Nothing Then
            RefreshIndexSelection(n.Tag, n.Checked)
            Return GetCheckState(n.Checked)
        End If
        If n.Nodes.Count = 0 Then
            RefreshIndexSelection(n.Tag, n.Checked)
            Return GetCheckState(n.Checked)
        End If
        Dim nChecked As Integer = 0, nUnChecked As Integer = 0
        For Each nd As TreeNode In n.Nodes
            Dim status As CheckState = RefreshChackState(nd)
            Select Case status
                Case CheckState.Checked
                    nChecked += 1
                Case CheckState.Unchecked
                    nUnChecked += 1
                Case CheckState.Indeterminate
                    nChecked += 1
                    nUnChecked += 1
            End Select
        Next
        Dim Result As CheckState
        If nChecked > 0 And nUnChecked = 0 Then
            RefreshIndexSelection(n.Tag, True)
            Result = CheckState.Checked
        ElseIf nChecked = 0 And nUnChecked > 0 Then
            RefreshIndexSelection(n.Tag, False)
            Result = CheckState.Unchecked
        ElseIf nChecked > 0 And nUnChecked > 0 Then
            RefreshIndexSelection(n.Tag, True)
            Result = CheckState.Indeterminate
        Else
            RefreshIndexSelection(n.Tag, n.Checked)
            Result = GetCheckState(n.Checked)
        End If
        TreeView1.SetNodeCheckState(n, Result)
        Return Result
    End Function
    Public Function GetCheckState(Checked As Boolean) As CheckState
        If Checked Then Return CheckState.Checked Else Return CheckState.Unchecked
    End Function
    Public Class ObjectBoolean
        Public Value As Boolean = False
        Public Sub New(v As Boolean)
            Value = v
        End Sub
        Public Sub New()

        End Sub

        Public Shared Widening Operator CType(v As ObjectBoolean) As Boolean
            Return v.Value
        End Operator

        Public Shared Widening Operator CType(v As Boolean) As ObjectBoolean
            Return New ObjectBoolean(v)
        End Operator
    End Class
    Private EventLock As New ObjectBoolean
    Private Sub TreeView1_AfterCheck(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterCheck
        SyncLock EventLock
            If EventLock Then Exit Sub
            EventLock = True
        End SyncLock
        If e.Node.Nodes IsNot Nothing Then
            If e.Node.Nodes.Count > 0 Then
                RecursivelySetNodeCheckStatus(e.Node, e.Node.Checked)
            End If
        End If
        For Each n As TreeNode In TreeView1.Nodes
            RefreshChackState(n)
        Next
        SyncLock EventLock
            EventLock = False
        End SyncLock
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        DialogResult = DialogResult.Cancel
        Close()
    End Sub
End Class