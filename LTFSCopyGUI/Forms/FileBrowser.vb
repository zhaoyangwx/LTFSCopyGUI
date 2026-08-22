Imports System.ComponentModel
Imports LTFSCopyGUI
Imports System
Imports Serilog
Imports Serilog.Context

Public Class FileBrowser
    Public Property schema As ltfsindex
    Private ReadOnly _logSessionId As String = $"filebrowser-{Guid.NewGuid().ToString("N").Substring(0, 8)}"
    Public Overloads Shared Sub Show(FList As ltfsindex)
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "WindowOpen")
                    Log.Information("File browser window requested.")
                End Using
            End Using
        End Using
        Dim FB1 As New FileBrowser
        With FB1
            .schema = FList
            .Show()
        End With
    End Sub
    Public Overloads Shared Function ShowDialog(FList As ltfsindex) As DialogResult
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "WindowOpen")
                    Log.Information("File browser dialog requested.")
                End Using
            End Using
        End Using
        Dim FB1 As New FileBrowser
        With FB1
            .schema = FList
            Return .ShowDialog()
        End With
    End Function
    Private Sub FileBrowser_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Lifecycle")
                        Log.Information("File browser loading index tree. HasSchema={HasSchema}.", schema IsNot Nothing)
                    End Using
                End Using
            End Using
        End Using
        SuspendLayout()
        SyncLock EventLock
            If EventLock Then Exit Sub
            EventLock = True
        End SyncLock
        CheckBox1.Checked = My.Settings.FileBrowser_CopyInfo
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
        ResumeLayout()
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Lifecycle")
                        Log.Information("File browser index tree loaded. RootNodeCount={RootNodeCount}.", TreeView1.Nodes.Count)
                    End Using
                End Using
            End Using
        End Using
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
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Error")
                            Log.Error(ex, "File browser failed to build an index tree.")
                        End Using
                    End Using
                End Using
            End Using
            MessageBox.Show(New Form With {.TopMost = True}, ex.ToString)
        End Try

    End Sub

    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeView1.AfterSelect
        SyncLock EventLock
            If EventLock Then Exit Sub
            EventLock = True
        End SyncLock
        If e.Node.Nodes IsNot Nothing Then
            Dim n As Object = e.Node.Tag
            If TypeOf (n) Is ltfsindex.file Then
                Text = "File: " & CType(n, ltfsindex.file).name
                If CheckBox1.Checked Then
                    Clipboard.SetText("File" & vbTab & CType(n, ltfsindex.file).name & vbCrLf)
                End If
            End If
            If TypeOf (n) Is ltfsindex.directory Then
                Text = "Directory: " & CType(n, ltfsindex.directory).name & " (DirCount=" & CType(n, ltfsindex.directory).contents._directory.Count & " FileCount=" & CType(n, ltfsindex.directory).contents._file.Count & ")"

                If CheckBox1.Checked Then
                    Dim o As String = ""
                    For Each d As ltfsindex.directory In CType(n, ltfsindex.directory).contents._directory
                        o &= "Directory" & vbTab & d.name & vbCrLf
                    Next
                    For Each d As ltfsindex.file In CType(n, ltfsindex.directory).contents._file
                        o &= "File" & vbTab & d.name & vbCrLf
                    Next
                    Clipboard.SetText(o)
                End If
            End If
        End If
        SyncLock EventLock
            EventLock = False
        End SyncLock
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "SelectionChanged")
                        Log.Information("File browser selection changed. NodeText={NodeText} Checked={Checked}.", e.Node.Text, e.Node.Checked)
                    End Using
                End Using
            End Using
        End Using
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
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "SelectionChanged")
                        Log.Information("File browser node check state changed. NodeText={NodeText} Checked={Checked}.", e.Node.Text, e.Node.Checked)
                    End Using
                End Using
            End Using
        End Using
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "WindowClose")
                        Log.Information("File browser accepted the current selection.")
                    End Using
                End Using
            End Using
        End Using
        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "WindowClose")
                        Log.Information("File browser canceled the current selection.")
                    End Using
                End Using
            End Using
        End Using
        DialogResult = DialogResult.Cancel
        Close()
    End Sub

    Private Sub 全选ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 全选ToolStripMenuItem.Click
        For Each n As TreeNode In TreeView1.Nodes
            n.Checked = True
        Next
    End Sub

    Private Sub 按大小ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 按大小ToolStripMenuItem.Click
        SuspendLayout()
        For Each n As TreeNode In TreeView1.Nodes
            n.Checked = False
        Next
        Dim sMin As Long = 0, sMax As Long = Long.MaxValue
        DisplayHelper.ShowInputDialog("Minimum Bytes", "By Size", sMin)
        DisplayHelper.ShowInputDialog("Maximum Bytes", "By Size", sMax)
        If sMax < sMin Then
            Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
                Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                    Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                        Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Filter")
                            Log.Warning("File browser size filter was ignored because the maximum was below the minimum. MinimumBytes={MinimumBytes} MaximumBytes={MaximumBytes}.", sMin, sMax)
                        End Using
                    End Using
                End Using
            End Using
            ResumeLayout()
            Exit Sub
        End If
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Filter")
                        Log.Information("File browser size filter applied. MinimumBytes={MinimumBytes} MaximumBytes={MaximumBytes}.", sMin, sMax)
                    End Using
                End Using
            End Using
        End Using
        Dim NList As New List(Of TreeNode)
        Dim DirQ As New List(Of TreeNode)
        For Each n As TreeNode In TreeView1.Nodes
            If n.Nodes.Count = 0 Then
                NList.Add(n)
            Else
                DirQ.Add(n)
            End If
        Next
        While DirQ.Count > 0
            Dim DirT As New List(Of TreeNode)
            For Each n As TreeNode In DirQ
                If n.Nodes.Count = 0 Then
                    NList.Add(n)
                Else
                    For Each n2 As TreeNode In n.Nodes
                        DirT.Add(n2)
                    Next
                End If
            Next
            DirQ = DirT
            DirT = New List(Of TreeNode)
        End While
        For Each n As TreeNode In NList
            If TypeOf n.Tag Is ltfsindex.file Then
                Dim nlen As Long = CType(n.Tag, ltfsindex.file).length
                If sMin <= nlen And sMax >= nlen Then
                    n.Checked = True
                End If
            End If
        Next
        ResumeLayout()
    End Sub

    Private Sub 匹配文件名ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles 匹配文件名ToolStripMenuItem.Click
        SuspendLayout()
        Dim pattern As String = "*"
        DisplayHelper.ShowInputDialog("Regex", "By regex", pattern)
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Filter")
                        Log.Information("File browser filename filter applied. Pattern={Pattern}.", pattern)
                    End Using
                End Using
            End Using
        End Using
        For Each n As TreeNode In TreeView1.Nodes
            n.Checked = False
        Next
        Dim NList As New List(Of TreeNode)
        Dim DirQ As New List(Of TreeNode)
        For Each n As TreeNode In TreeView1.Nodes
            If n.Nodes.Count = 0 Then
                NList.Add(n)
            Else
                DirQ.Add(n)
            End If
        Next
        While DirQ.Count > 0
            Dim DirT As New List(Of TreeNode)
            For Each n As TreeNode In DirQ
                If n.Nodes.Count = 0 Then
                    NList.Add(n)
                Else
                    For Each n2 As TreeNode In n.Nodes
                        DirT.Add(n2)
                    Next
                End If
            Next
            DirQ = DirT
            DirT = New List(Of TreeNode)
        End While
        For Each n As TreeNode In NList
            If TypeOf n.Tag Is ltfsindex.file Then
                Dim nName As String = CType(n.Tag, ltfsindex.file).name
                If System.Text.RegularExpressions.Regex.IsMatch(nName, pattern, System.Text.RegularExpressions.RegexOptions.Compiled) Then
                    n.Checked = True
                End If
            End If
        Next
        ResumeLayout()
    End Sub

    Private Sub FileBrowser_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.FileBrowser_CopyInfo = CheckBox1.Checked
        My.Settings.Save()
        Using sourceContextScope As IDisposable = LogContext.PushProperty("SourceContext", NameOf(FileBrowser))
            Using categoryScope As IDisposable = LogContext.PushProperty("Category", "FileBrowser")
                Using sessionScope As IDisposable = LogContext.PushProperty("SessionId", _logSessionId)
                    Using eventTypeScope As IDisposable = LogContext.PushProperty("EventType", "Lifecycle")
                        Log.Information("File browser closed. CopyInfoEnabled={CopyInfoEnabled}.", CheckBox1.Checked)
                    End Using
                End Using
            End Using
        End Using
    End Sub
End Class
