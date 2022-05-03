Public Structure TV_ITEM
    Public mask As UInteger
    Public hItem As IntPtr
    Public state As UInteger
    Public stateMask As UInteger
    <Runtime.InteropServices.MarshalAs(Runtime.InteropServices.UnmanagedType.LPTStr)>
    Public pszText As String
    Public cchTextMax As Integer
    Public iImage As Integer
    Public iSelectedImage As Integer
    Public cChildren As Integer
    Public lParam As IntPtr
End Structure

Public Class TreeViewEx
    Inherits TreeView
    Private Const TVIF_HANDLE As UInteger = &H10
    Private Const TVIF_STATE As UInteger = &H8
    Private Const TVIS_STATEIMAGEMASK As UInteger = &HF000
    Private Const TV_FIRST As UInteger = &H1100
    Private Const TVM_SETITEM As UInteger = TV_FIRST + 13
    Private Const TVM_SETEXTENDEDSTYLE As UInteger = TV_FIRST + 44
    Private Const TVS_EX_DOUBLEBUFFER As UInteger = &H4
    Private Const TVS_EX_PARTIALCHECKBOXES As UInteger = &H80

    Private Declare Auto Function SendMessage Lib "user32" (ByVal hWnd As IntPtr, ByVal Msg As UInteger, ByVal wParam As IntPtr, ByRef lParam As TV_ITEM) As IntPtr
    Private Declare Auto Function SendMessage Lib "user32" (ByVal hWnd As IntPtr, ByVal Msg As UInteger, ByVal wParam As IntPtr, ByRef lParam As IntPtr) As IntPtr

    Private Function INDEXTOSTATEIMAGEMASK(i As Integer) As Integer
        Return i << 12
    End Function

    Protected Overrides Sub OnHandleCreated(e As System.EventArgs)
        Dim style As UInteger = TVS_EX_DOUBLEBUFFER Or TVS_EX_PARTIALCHECKBOXES
        SendMessage(Me.Handle, TVM_SETEXTENDEDSTYLE, New IntPtr(style), New IntPtr(style))
        MyBase.OnHandleCreated(e)
    End Sub

    Public Sub SetNodeCheckState(node As TreeNode, state As CheckState)
        If state = CheckState.Indeterminate Then
            If System.Environment.OSVersion.Version.Major >= 6 Then
                Dim it As TV_ITEM = Nothing
                it.mask = TVIF_HANDLE Or TVIF_STATE
                it.hItem = node.Handle
                it.stateMask = TVIS_STATEIMAGEMASK
                it.state = INDEXTOSTATEIMAGEMASK(3) 'indeterminate
                SendMessage(Me.Handle, TVM_SETITEM, IntPtr.Zero, it)
            Else
                node.Checked = False
            End If
        Else
            node.Checked = (state = CheckState.Checked)
        End If
    End Sub
End Class