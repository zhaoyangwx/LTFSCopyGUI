Imports System
Imports System.Collections.Generic

Public NotInheritable Class SCSIDeviceLockManager
    Private Shared ReadOnly _instance As New SCSIDeviceLockManager()

    Private ReadOnly _registryLock As New Object()
    Private ReadOnly _pathLocks As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _handleLocks As New Dictionary(Of Long, Object)()
    Private ReadOnly _fallbackLock As New Object()

    Private Sub New()
    End Sub

    Public Shared ReadOnly Property Instance As SCSIDeviceLockManager
        Get
            Return _instance
        End Get
    End Property

    Public ReadOnly Property FallbackLock As Object
        Get
            Return _fallbackLock
        End Get
    End Property

    Public Function GetLock(devicePath As String) As Object
        If String.IsNullOrWhiteSpace(devicePath) Then Return _fallbackLock

        Dim key As String = NormalizePath(devicePath)
        SyncLock _registryLock
            Dim result As Object = Nothing
            If Not _pathLocks.TryGetValue(key, result) Then
                result = New Object()
                _pathLocks.Add(key, result)
            End If
            Return result
        End SyncLock
    End Function

    Public Function GetLock(handle As IntPtr) As Object
        If IsInvalidHandle(handle) Then Return _fallbackLock

        Dim key As Long = handle.ToInt64()
        SyncLock _registryLock
            Dim result As Object = Nothing
            If Not _handleLocks.TryGetValue(key, result) Then
                result = New Object()
                _handleLocks.Add(key, result)
            End If
            Return result
        End SyncLock
    End Function

    Public Function RegisterHandle(devicePath As String, handle As IntPtr) As Object
        Dim result As Object = GetLock(devicePath)
        If IsInvalidHandle(handle) Then Return result

        SyncLock _registryLock
            _handleLocks(handle.ToInt64()) = result
        End SyncLock
        Return result
    End Function

    Private Shared Function NormalizePath(devicePath As String) As String
        Dim result As String = devicePath.Trim()
        Dim deviceName As String = result

        If deviceName.StartsWith("\\.", StringComparison.OrdinalIgnoreCase) Then
            deviceName = deviceName.Substring(4)
        End If

        Dim tapeIndex As Integer
        If deviceName.StartsWith("TAPE", StringComparison.OrdinalIgnoreCase) AndAlso
           Integer.TryParse(deviceName.Substring(4), tapeIndex) Then
            Return $"\\.\TAPE{tapeIndex}"
        End If

        If Integer.TryParse(deviceName, tapeIndex) Then
            Return $"\\.\TAPE{tapeIndex}"
        End If

        Return result
    End Function

    Private Shared Function IsInvalidHandle(handle As IntPtr) As Boolean
        Return handle = IntPtr.Zero OrElse handle = New IntPtr(-1)
    End Function
End Class
