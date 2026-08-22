Imports System
Imports System.Collections.Generic
Imports System.Windows.Forms

''' <summary>
''' Keeps normal window navigation inside the current LCG process.
''' A process restart is used only when the requested operation needs UAC.
''' </summary>
Public Module ApplicationNavigation
    Public Sub ShowConfigurator()
        If Not EnsureAdministrator("configurator") Then Return
        ShowAndActivate(LTFSConfigurator)
    End Sub

    Public Sub ShowChangerTool()
        If Not EnsureAdministrator("changer") Then Return
        ShowAndActivate(ChangerTool)
    End Sub

    Public Sub ShowTapeCopy()
        If Not EnsureAdministrator("tapecopy") Then Return
        ShowAndActivate(TapeCopy)
    End Sub

    Public Sub ShowWriter(tapeDrive As String, Optional offlineMode As Boolean = False)
        If String.IsNullOrWhiteSpace(tapeDrive) Then Return
        Dim normalizedTapeDrive = NormalizeTapeDrive(tapeDrive)
        If Not EnsureAdministrator("writer", normalizedTapeDrive, If(offlineMode, "offline", String.Empty)) Then Return

        Dim writer As New LTFSWriter With {
            .TapeDrive = normalizedTapeDrive,
            .OfflineMode = offlineMode
        }
        writer.Show()
        writer.BringToFront()
    End Sub

    ''' <summary>
    ''' Restores the normal parent window for a UAC-restarted process, then
    ''' opens the requested child window after the parent has been displayed.
    ''' </summary>
    Public Sub ScheduleStartupNavigation(route As String, routeArguments As IEnumerable(Of String))
        Dim parent As Form = Form1
        Dim handler As EventHandler = Nothing
        handler = Sub(sender As Object, e As EventArgs)
                      RemoveHandler parent.Shown, handler
                      ShowRoute(route, routeArguments)
                  End Sub
        AddHandler parent.Shown, handler
    End Sub

    Private Function EnsureAdministrator(route As String, ParamArray routeArguments() As String) As Boolean
        If ApplicationElevation.IsAdministrator Then Return True

        Dim arguments As New List(Of String) From {"-open", route}
        If routeArguments IsNot Nothing Then
            For Each routeArgument In routeArguments
                If Not String.IsNullOrEmpty(routeArgument) Then arguments.Add(routeArgument)
            Next
        End If

        If ApplicationElevation.StartElevated(arguments) Then
            ApplicationElevation.ExitCurrentProcess()
        End If
        Return False
    End Function

    Private Function NormalizeTapeDrive(tapeDrive As String) As String
        If String.IsNullOrWhiteSpace(tapeDrive) Then Return String.Empty

        If tapeDrive.StartsWith("TAPE") Then
            Return "\\." & tapeDrive
        ElseIf tapeDrive.StartsWith("\\.") Then
            Return tapeDrive
        ElseIf tapeDrive = Val(tapeDrive).ToString Then
            Return "\\.\TAPE" & tapeDrive
        End If

        Return tapeDrive
    End Function

    Private Sub ShowRoute(route As String, routeArguments As IEnumerable(Of String))
        Dim arguments As New List(Of String)
        If routeArguments IsNot Nothing Then arguments.AddRange(routeArguments)

        Select Case If(route, String.Empty).ToLowerInvariant()
            Case "configurator"
                ShowConfigurator()
            Case "changer"
                ShowChangerTool()
            Case "tapecopy"
                ShowTapeCopy()
            Case "writer"
                If arguments.Count = 0 Then Return
                Dim offlineMode = arguments.Count > 1 AndAlso
                                   String.Equals(arguments(1), "offline", StringComparison.OrdinalIgnoreCase)
                ShowWriter(arguments(0), offlineMode)
        End Select
    End Sub

    Private Sub ShowAndActivate(form As Form)
        form.Show()
        If form.WindowState = FormWindowState.Minimized Then form.WindowState = FormWindowState.Normal
        form.BringToFront()
        form.Activate()
    End Sub
End Module
