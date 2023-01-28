Imports System.Runtime.InteropServices

Public Class TapeUtils
    Private Declare Function _GetTapeDriveList Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _GetDriveMappings Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _StartLtfsService Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _StopLtfsService Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _RemapTapeDrives Lib "LtfsCommand.dll" () As IntPtr
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _MapTapeDrive(driveLetter As Char, tapeDrive As String, tapeIndex As Byte, ByVal logDir As String, ByVal workDir As String, showOffline As Boolean) As IntPtr

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _UnmapTapeDrive(driveLetter As Char) As IntPtr

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _LoadTapeDrive(driveLetter As Char, mount As Boolean) As IntPtr

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _EjectTapeDrive(driveLetter As Char) As IntPtr

    End Function

    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _MountTapeDrive(driveLetter As Char) As IntPtr

    End Function

    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _CheckTapeMedia(driveLetter As Char) As IntPtr

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _ScsiIoControl(hFile As IntPtr,
                                           deviceNumber As UInt32,
                                           cdb As UIntPtr,
                                           cdbLength As Byte,
                                           dataBuffer As UIntPtr,
                                           bufferLength As UInt16,
                                           dataIn As Byte,
                                           timeoutValue As UInt32,
                                           senseBuffer As UIntPtr) As Boolean

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function _TapeSCSIIOCtlFull(tapeDrive As String,
                                           cdb As IntPtr,
                                           cdbLength As Byte,
                                           dataBuffer As IntPtr,
                                           bufferLength As UInt16,
                                           dataIn As Byte,
                                           timeoutValue As UInt32,
                                           senseBuffer As IntPtr) As Boolean

    End Function
    Public Shared Function SendSCSICommand(tapeDrive As String, cdbData As Byte(), Optional Data As Byte() = Nothing) As Boolean
        Dim cdb As IntPtr = Marshal.AllocHGlobal(cdbData.Length)
        Marshal.Copy(cdbData, 0, cdb, cdbData.Length)

        Dim dataBufferPtr As IntPtr
        Dim dataLen As Integer = 0
        If Data IsNot Nothing Then
            dataLen = Data.Length
            dataBufferPtr = Marshal.AllocHGlobal(Data.Length)
            Marshal.Copy(Data, 0, dataBufferPtr, Data.Length)
        Else
            dataBufferPtr = Marshal.AllocHGlobal(127)
        End If

        Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(127)

        Dim senseBuffer(127) As Byte
        Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(tapeDrive, cdb, cdbData.Length, dataBufferPtr, dataLen, 2, 300, senseBufferPtr)
        'Marshal.Copy(senseBufferPtr, senseBuffer, 0, 127)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(dataBufferPtr)
        Marshal.FreeHGlobal(senseBufferPtr)
        Return succ
    End Function
    Structure LPSECURITY_ATTRIBUTES
        Dim nLength As UInt32
        Dim lpSecurityDescriptor As UIntPtr
        Dim bInheritHandle As Boolean
    End Structure

    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _CreateFile(lpFileName As String,
                                        dwDesiredAccess As UInt32,
                                        dwShareMode As UInt32,
                                        lpSecurityAttributes As IntPtr,
                                        dwCreationDisposition As UInt32,
                                        dwFlagsAndAttributes As UInt32,
                                        hTemplateFile As IntPtr
    ) As IntPtr

    End Function
    Public Shared Function CreateFile(lpFileName As String,
                                        dwDesiredAccess As UInt32,
                                        dwShareMode As UInt32,
                                        lpSecurityAttributes As LPSECURITY_ATTRIBUTES,
                                        dwCreationDisposition As UInt32,
                                        dwFlagsAndAttributes As UInt32,
                                        hTemplateFile As IntPtr)
        Dim lpSecurityAttributesPtr As IntPtr
        Marshal.StructureToPtr(lpSecurityAttributes, lpSecurityAttributesPtr, True)
        Return _CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributesPtr, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile)
    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function _TapeSCSIIOCtl(tapeDrive As String, SCSIOPCode As Byte) As IntPtr

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function _TapeDeviceIOCtl(tapeDrive As String, DWIOCode As UInt32) As IntPtr

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _Test(ByVal a As Char) As IntPtr

    End Function


    Public Const DEFAULT_LOG_DIR As String = "C:\ProgramData\HPE\LTFS"
    Public Const DEFAULT_WORK_DIR As String = "C:\tmp\LTFS"
    Public Shared Function GetTapeDriveList() As List(Of TapeDrive)
        Dim p As IntPtr = _GetTapeDriveList()
        Dim s() As String = Marshal.PtrToStringAnsi(p).Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        Dim LDrive As New List(Of TapeDrive)
        For Each t As String In s
            Dim q() As String = t.Split({"|"}, StringSplitOptions.None)
            If q.Length = 4 Then
                LDrive.Add(New TapeDrive(q(0), q(1), q(2), q(3)))
            End If
        Next
        LDrive.Sort(New Comparison(Of TapeDrive)(
                        Function(A As TapeDrive, B As TapeDrive) As Integer
                            Return A.DevIndex.CompareTo(B.DevIndex)
                        End Function))
        s = GetDriveMappings().Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        For Each t As String In s
            Dim q() As String = t.Split({"|"}, StringSplitOptions.None)
            If q.Length = 3 Then
                For Each Drv As TapeDrive In LDrive
                    If "TAPE" & Drv.DevIndex = q(1) And Drv.SerialNumber = q(2) Then
                        Drv.DriveLetter = q(0)
                    End If
                Next
            End If
        Next
        Return LDrive
    End Function

    Public Shared Function GetDriveMappings() As String
        Dim p As IntPtr = _GetDriveMappings()
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function

    Public Shared Function StartLtfsService() As String
        Dim p As IntPtr = _StartLtfsService()
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function
    Public Shared Function StopLtfsService() As String
        Dim p As IntPtr = _StopLtfsService()
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function
    Public Shared Function RemapTapeDrives() As String
        Dim p As IntPtr = _RemapTapeDrives()
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function
    Public Shared Function MapTapeDrive(driveLetter As Char, tapeDrive As String, Optional ByVal logDir As String = DEFAULT_LOG_DIR, Optional ByVal workDir As String = DEFAULT_WORK_DIR, Optional ByVal showOffline As Boolean = False) As String
        Dim tapeIndex As Byte = Byte.Parse(tapeDrive.Substring(4))
        Dim p As IntPtr = _MapTapeDrive(driveLetter, tapeDrive, tapeIndex, logDir, workDir, showOffline)
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function

    Public Shared Function UnMapTapeDrive(driveLetter As Char) As String
        Dim p As IntPtr = _UnmapTapeDrive(driveLetter)
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function
    Public Shared Function LoadTapeDrive(driveLetter As Char, mount As Boolean) As String
        Dim p As IntPtr = _LoadTapeDrive(driveLetter, mount)
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function
    Public Shared Function EjectTapeDrive(driveLetter As Char) As String
        Dim p As IntPtr = _EjectTapeDrive(driveLetter)
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function
    Public Shared Function MountTapeDrive(driveLetter As Char) As String
        Dim p As IntPtr = _MountTapeDrive(driveLetter)
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function
    Public Shared Function CheckTapeMedia(driveLetter As Char) As String
        Dim p As IntPtr = _CheckTapeMedia(driveLetter)
        Dim s As String = Marshal.PtrToStringAnsi(p)
        Return s
    End Function
    Public Class TapeDrive
        Public Property DevIndex As String
        Public Property SerialNumber As String
        Public Property VendorId As String
        Public Property ProductId As String
        Public Property DriveLetter As String
        Public Sub New()

        End Sub
        Public Sub New(DevIndex As String, SerialNumber As String, VendorId As String, ProductId As String, Optional ByVal DriveLetter As String = "")
            Me.DevIndex = DevIndex.TrimEnd(" ")
            Me.SerialNumber = SerialNumber.TrimEnd(" ")
            Me.VendorId = VendorId.TrimEnd(" ")
            Me.ProductId = ProductId.TrimEnd(" ")
            Me.DriveLetter = DriveLetter.TrimEnd(" ")
        End Sub
        Public Overrides Function ToString() As String
            Dim o As String = "TAPE" & DevIndex & ":"
            If DriveLetter <> "" Then o &= " (" & DriveLetter & ":)"
            o &= " [" & SerialNumber & "] " & VendorId & " " & ProductId
            Return o
        End Function
    End Class
End Class
