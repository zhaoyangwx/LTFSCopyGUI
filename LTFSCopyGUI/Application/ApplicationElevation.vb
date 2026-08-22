Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Text
Imports System.Windows.Forms
Imports Serilog

''' <summary>
''' Starts an elevated copy of the application and provides a single, orderly
''' way for the unelevated process to leave.
''' </summary>
Public Module ApplicationElevation
    Private Const ElevationCanceledErrorCode As Integer = 1223

    Public ReadOnly Property IsAdministrator As Boolean
        Get
            Return New Security.Principal.WindowsPrincipal(
                Security.Principal.WindowsIdentity.GetCurrent()).IsInRole(
                    Security.Principal.WindowsBuiltInRole.Administrator)
        End Get
    End Property

    Public Function StartElevated(arguments As IEnumerable(Of String)) As Boolean
        Dim startInfo As New ProcessStartInfo With {
            .FileName = Application.ExecutablePath,
            .Arguments = BuildCommandLine(arguments),
            .Verb = "runas",
            .UseShellExecute = True,
            .WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
        }

        Try
            Process.Start(startInfo)
            Log.Information("Elevated application process started.")
            Return True
        Catch ex As Win32Exception When ex.NativeErrorCode = ElevationCanceledErrorCode
            Log.Information("Elevation request canceled by the user.")
            Return False
        Catch ex As Exception
            Log.Error(ex, "Failed to start elevated application process.")
            Return False
        End Try
    End Function

    Public Function StartElevated(ParamArray arguments() As String) As Boolean
        Return StartElevated(CType(arguments, IEnumerable(Of String)))
    End Function

    ''' <summary>
    ''' Ends the current process after the elevated process has been started.
    ''' The current logger is flushed before the exit request so no startup
    ''' record is left in the async queue.
    ''' </summary>
    Public Sub ExitCurrentProcess()
        Try
            Log.Information("Current process exiting after an elevation request.")
        Finally
            Log.CloseAndFlush()
            Environment.Exit(0)
        End Try
    End Sub

    Private Function BuildCommandLine(arguments As IEnumerable(Of String)) As String
        If arguments Is Nothing Then Return String.Empty

        Dim builder As New StringBuilder()
        For Each argument In arguments
            If builder.Length > 0 Then builder.Append(" "c)
            builder.Append(QuoteArgument(argument))
        Next
        Return builder.ToString()
    End Function

    Private Function QuoteArgument(value As String) As String
        If value Is Nothing Then Return """"""

        Dim requiresQuotes As Boolean = value.Length = 0
        If Not requiresQuotes Then
            For Each ch As Char In value
                If Char.IsWhiteSpace(ch) OrElse ch = """"c Then
                    requiresQuotes = True
                    Exit For
                End If
            Next
        End If

        If Not requiresQuotes Then Return value

        Dim builder As New StringBuilder(value.Length + 2)
        builder.Append(""""c)
        Dim backslashCount As Integer = 0

        For Each ch As Char In value
            If ch = "\"c Then
                backslashCount += 1
            ElseIf ch = """"c Then
                builder.Append("\"c, backslashCount * 2 + 1)
                builder.Append(""""c)
                backslashCount = 0
            Else
                builder.Append("\"c, backslashCount)
                builder.Append(ch)
                backslashCount = 0
            End If
        Next

        builder.Append("\"c, backslashCount * 2)
        builder.Append(""""c)
        Return builder.ToString()
    End Function
End Module
