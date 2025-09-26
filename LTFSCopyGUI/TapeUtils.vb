Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.ComponentModel

Imports LTFSCopyGUI

<TypeConverter(GetType(ExpandableObjectConverter))>
Public Class TapeUtils
#Region "winapi"
    Public Class SetupAPIWheels
        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Ansi)>
        Public Structure SP_DEVINFO_DATA

            Public cbSize As UInteger

            Public ClassGuid As Guid

            Public DevInst As UInteger

            Public Reserved As IntPtr
        End Structure
        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Ansi)>
        Public Structure SP_DEVINFO_DETAIL_DATA

            Public cbSize As UInteger

            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=256)>
            Public DevicePath As String

        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Ansi)>
        Public Structure SP_DEVICE_INTERFACE_DATA

            Public cbSize As UInteger

            Public InterfaceClassGuid As Guid

            Public Flags As UInteger

            Public Reserved As IntPtr
        End Structure
        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Ansi)>
        Public Structure SP_DEVICE_INTERFACE_DETAIL_DATA

            Public cbSize As UInteger

            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=256)>
            Public DevicePath As String
        End Structure
        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Ansi)>
        Public Structure TAPE_DRIVE
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=8)>
            Public VendorId As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=16)>
            Public ProductId As String
            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=128)>
            Public SerialNumber As String
            Public DevIndex As Int32
            Public NextTapeDrive As IntPtr
        End Structure
        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Ansi)>
        Public Structure SP_DRVINFO_DATA

            Public cbSize As UInteger

            Public DriverType As UInteger

            Public Reserved As IntPtr

            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=256)>
            Public Description As String

            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=256)>
            Public MfgName As String

            <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=256)>
            Public ProviderName As String

            Public DriverDate As FILETIME

            Public DriverVersion As System.UInt64
        End Structure

        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Ansi)>
        Public Class STORAGE_DEVICE_NUMBER
            Public DeviceType As Int32
            Public DeviceNumber As Int32
            Public PartitionNumber As Int32
        End Class
        Private Function GetVersionFromLong(ByVal version As System.UInt64) As String
            Dim baseNumber As System.UInt64 = 65535
            Dim sb As StringBuilder = New StringBuilder
            Dim temp As System.UInt64
            Dim offset As Integer = 48
            Do While (offset >= 0)
                temp = ((version + offset) _
                        And baseNumber)
                sb.Append((temp.ToString + "."))
                offset = (offset - 16)
            Loop

            Return sb.ToString
        End Function
        ' Flags for CM_Locate_DevNode
        Public Const CM_LOCATE_DEVNODE_NORMAL As UInteger = 0

        Public Const CM_LOCATE_DEVNODE_PHANTOM As UInteger = 1

        Public Const CM_LOCATE_DEVNODE_CANCELREMOVE As UInteger = 2

        Public Const CM_LOCATE_DEVNODE_NOVALIDATION As UInteger = 4

        Public Const CM_LOCATE_DEVNODE_BITS As UInteger = 7

        ' Flags for CM_Disable_DevNode
        Public Const CM_DISABLE_POLITE As UInteger = 0

        Public Const CM_DISABLE_ABSOLUTE As UInteger = 1

        Public Const CM_DISABLE_HARDWARE As UInteger = 2

        Public Const CM_DISABLE_UI_NOT_OK As UInteger = 4

        Public Const CM_DISABLE_PERSIST As UInteger = 8

        Public Const CM_DISABLE_BITS As UInteger = 15

        ' Flags for CM_Query_And_Remove_SubTree
        Public Const CM_REMOVE_UI_OK As UInteger = 0

        Public Const CM_REMOVE_UI_NOT_OK As UInteger = 1

        Public Const CM_REMOVE_NO_RESTART As UInteger = 2

        Public Const CM_REMOVE_BITS As UInteger = 3

        Public Const DIGCF_DEFAULT As UInteger = &H1
        Public Const DIGCF_PRESENT As UInteger = &H2
        Public Const DIGCF_ALLCLASSES As UInteger = &H4
        Public Const DIGCF_PROFILE As UInteger = &H8
        Public Const DIGCF_DEVICEINTERFACE As UInteger = &H10
        Public Const INVALID_HANDLE_VALUE As Integer = -1
        Public Const MAX_DEV_LEN As Integer = 256
        Public Const SPDRP_DEVICEDESC As UInteger = &H0 ' DeviceDesc (R/W)
        Public Const SPDRP_HARDWAREID As UInteger = &H1 ' HardwareID (R/W)
        Public Const SPDRP_COMPATIBLEIDS As UInteger = &H2 ' CompatibleIDs (R/W)
        Public Const SPDRP_UNUSED0 As UInteger = &H3 ' unused
        Public Const SPDRP_SERVICE As UInteger = &H4 ' Service (R/W)
        Public Const SPDRP_UNUSED1 As UInteger = &H5 ' unused
        Public Const SPDRP_UNUSED2 As UInteger = &H6 ' unused
        Public Const SPDRP_CLASS As UInteger = &H7 ' Class (R--tied to ClassGUID)
        Public Const SPDRP_CLASSGUID As UInteger = &H8 ' ClassGUID (R/W)
        Public Const SPDRP_DRIVER As UInteger = &H9 ' Driver (R/W)
        Public Const SPDRP_CONFIGFLAGS As UInteger = &HA ' ConfigFlags (R/W)
        Public Const SPDRP_MFG As UInteger = &HB ' Mfg (R/W)
        Public Const SPDRP_FRIENDLYNAME As UInteger = &HC ' FriendlyName (R/W)
        Public Const SPDRP_LOCATION_INFORMATION As UInteger = &HD ' LocationInformation (R/W)
        Public Const SPDRP_PHYSICAL_DEVICE_OBJECT_NAME As UInteger = &HE ' PhysicalDeviceObjectName (R)
        Public Const SPDRP_CAPABILITIES As UInteger = &HF ' Capabilities (R)
        Public Const SPDRP_UI_NUMBER As UInteger = &H10 ' UiNumber (R)
        Public Const SPDRP_UPPERFILTERS As UInteger = &H11 ' UpperFilters (R/W)
        Public Const SPDRP_LOWERFILTERS As UInteger = &H12 ' LowerFilters (R/W)
        Public Const SPDRP_BUSTYPEGUID As UInteger = &H13 ' BusTypeGUID (R)
        Public Const SPDRP_LEGACYBUSTYPE As UInteger = &H14 ' LegacyBusType (R)
        Public Const SPDRP_BUSNUMBER As UInteger = &H15 ' BusNumber (R)
        Public Const SPDRP_ENUMERATOR_NAME As UInteger = &H16 ' Enumerator Name (R)
        Public Const SPDRP_SECURITY As UInteger = &H17 ' Security (R/W, binary form)
        Public Const SPDRP_SECURITY_SDS As UInteger = &H18 ' Security (W, SDS form)
        Public Const SPDRP_DEVTYPE As UInteger = &H19 ' Device Type (R/W)
        Public Const SPDRP_EXCLUSIVE As UInteger = &H1A ' Device is exclusive-access (R/W)
        Public Const SPDRP_CHARACTERISTICS As UInteger = &H1B ' Device Characteristics (R/W)
        Public Const SPDRP_ADDRESS As UInteger = &H1C ' Device Address (R)
        Public Const SPDRP_UI_NUMBER_DESC_FORMAT As UInteger = &H1D ' UiNumberDescFormat (R/W)
        Public Const SPDRP_DEVICE_POWER_DATA As UInteger = &H1E ' Device Power Data (R)
        Public Const SPDRP_REMOVAL_POLICY As UInteger = &H1F ' Removal Policy (R)
        Public Const SPDRP_REMOVAL_POLICY_HW_DEFAULT As UInteger = &H20 ' Hardware Removal Policy (R)
        Public Const SPDRP_REMOVAL_POLICY_OVERRIDE As UInteger = &H21 ' Removal Policy Override (RW)
        Public Const SPDRP_INSTALL_STATE As UInteger = &H22 ' Device Install State (R)
        Public Const SPDRP_LOCATION_PATHS As UInteger = &H23 ' Device Location Paths (R)
        Public Const SPDRP_BASE_CONTAINERID As UInteger = &H24 ' Base ContainerID (R)
        Public Const SPDRP_MAXIMUM_PROPERTY As UInteger = &H25 ' Upper bound on ordinals
        Public Const DICS_FLAG_GLOBAL As Integer = 1




        Public Class GUID_DEVINTERFACE
            Public Shared GUID_DEVINTERFACE_DISK As New Guid("53f56307-b6bf-11d0-94f2-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_CDROM As New Guid("53f56308-b6bf-11d0-94f2-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_PARTITION As New Guid("53f5630a-b6bf-11d0-94f2-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_TAPE As New Guid("53f5630b-b6bf-11d0-94f2-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_WRITEONCEDISK As New Guid("53f5630c-b6bf-11d0-94f2-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_VOLUME As New Guid("53f5630d-b6bf-11d0-94f2-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_MEDIUMCHANGER As New Guid("53f56310-b6bf-11d0-94f2-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_FLOPPY As New Guid("53f56311-b6bf-11d0-94f2-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_CDCHANGER As New Guid("53f56312-b6bf-11d0-94f2-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_STORAGEPORT As New Guid("2accfe60-c130-11d2-b082-00a0c91efb8b")
            Public Shared GUID_DEVINTERFACE_VMLUN As New Guid("6f416619-9f29-42a5-b20b-37e219ca02b0")
            Public Shared GUID_DEVINTERFACE_SES As New Guid("1790C9EC-47D5-4DF3-B5AF-9ADF3CF23E48")
            Public Shared GUID_DEVINTERFACE_SERVICE_VOLUME As New Guid("6EAD3D82-25EC-46BC-B7FD-C1F0DF8F5037")
            Public Shared GUID_DEVINTERFACE_HIDDEN_VOLUME As New Guid("7F108A28-9833-4B3B-B780-2C6B5FA5C062")
            Public Shared GUID_DEVINTERFACE_UNIFIED_ACCESS_RPMB As New Guid("27447C21-BCC3-4D07-A05B-A3395BB4EEE7")
            Public Shared GUID_DEVINTERFACE_COMPORT As New Guid("86E0D1E0-8089-11D0-9CE4-08003E301F73")
            Public Shared GUID_DEVINTERFACE_SERENUM_BUS_ENUMERATOR As New Guid("4D36E978-E325-11CE-BFC1-08002BE10318")
        End Class
        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function SetupDiGetClassDevs(
            ByRef classGuid As IntPtr,
            ByVal Enumerator As IntPtr,
            ByVal hwndParent As IntPtr,
            ByVal Flags As UInteger) As IntPtr
        End Function
        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function SetupDiEnumDeviceInfo(
            ByVal DeviceInfoSet As IntPtr,
            ByVal MemberIndex As UInteger,
            ByRef DeviceInfoData As SP_DEVINFO_DATA) As Boolean
        End Function
        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function SetupDiEnumDeviceInterfaces(
            ByVal DeviceInfoSet As IntPtr,
            ByVal DeviceInfoData As IntPtr,
            ByRef InterfaceClassGuid As IntPtr,
            ByVal MemberIndex As UInteger,
            ByRef DeviceInterfaceData As IntPtr) As Boolean
        End Function
        <DllImport("setupapi.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Public Shared Function SetupDiGetDeviceInterfaceDetail(
            ByVal hDevInfo As IntPtr,
            ByRef deviceInterfaceData As SP_DEVICE_INTERFACE_DATA,
            ByVal mustPassIntPtrZero As IntPtr,
            ByVal mustPassZero As Int32,
            ByRef RequiredSize As Int32,
            ByVal mustPassIntPtrZero2 As IntPtr) As Boolean
        End Function
        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function SetupDiGetDeviceRegistryProperty(
            ByVal DeviceInfoSet As IntPtr,
            ByRef DeviceInfoData As SP_DEVINFO_DATA,
            ByVal [Property] As UInteger,
            ByVal PropertyRegDataType As UInteger,
            ByVal PropertyBuffer As StringBuilder,
            ByVal PropertyBufferSize As UInteger,
            ByVal RequiredSize As IntPtr) As Boolean
        End Function


        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function SetupDiGetDeviceRegistryProperty(
            ByVal DeviceInfoSet As IntPtr,
            ByRef DeviceInfoData As SP_DEVINFO_DATA,
            ByVal [Property] As UInteger,
            ByVal PropertyRegDataType As UInteger,
            ByVal PropertyBuffer() As Byte,
            ByVal PropertyBufferSize As UInteger,
            ByVal RequiredSize As IntPtr) As Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function SetupDiGetDeviceInstanceId(
            ByVal DeviceInfoSet As IntPtr,
            ByRef DeviceInfoData As SP_DEVINFO_DATA,
            ByVal DeviceInstanceId As StringBuilder,
            ByVal DeviceInstanceIdSize As Integer,
            ByRef RequiredSize As Integer) As Boolean
        End Function
        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function SetupDiGetDeviceInterfaceDetailA(
            ByVal DeviceInfoSet As IntPtr,
            ByRef DeviceInterfaceData As SP_DEVICE_INTERFACE_DATA,
            ByVal DeviceInterfaceDetailData As SP_DEVINFO_DETAIL_DATA,
            ByVal DeviceInterfaceDetailDataSize As Integer,
            ByRef RequiredSize As Integer,
            ByRef DeviceInfoData As SP_DEVINFO_DATA) As Boolean
        End Function

        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function SetupDiDestroyDeviceInfoList(
            ByVal DeviceInfoSet As IntPtr) As Boolean
        End Function

    End Class


    <DllImport("kernel32.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
    Public Shared Function GetLastError() As UInteger
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
    Public Shared Function DeviceIoControl(
        HDevice As IntPtr,
        dwIoControlCode As UInt32,
        lpInBuffer As IntPtr,
        nInBufferSize As UInt32,
        lpOutBuffer As IntPtr,
        nOutBufferSize As UInt32,
        ByRef lpBytesReturned As UInt32,
        lpOverlapped As IntPtr) As Boolean
    End Function

#End Region

    Private Declare Function _GetTapeDriveList Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _GetDiskDriveList Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _GetMediumChangerList Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _GetDriveMappings Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _StartLtfsService Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _StopLtfsService Lib "LtfsCommand.dll" () As IntPtr
    Private Declare Function _RemapTapeDrives Lib "LtfsCommand.dll" () As IntPtr
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _MapTapeDrive(driveLetter As Char, TapeDrive As String, tapeIndex As Byte, ByVal logDir As String, ByVal workDir As String, showOffline As Boolean) As IntPtr

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

    Public Shared SCSIOperationLock As New Object
    Public Shared Function IOCtlDirect(TapeDrive As String,
                                           cdb As Byte(),
                                           dataBuffer As IntPtr,
                                           bufferLength As UInt32,
                                           dataIn As Byte,
                                           timeoutValue As UInt32,
                                           ByRef senseBuffer As Byte()) As Boolean

        Dim driveHandle As IntPtr
        Dim result As Boolean = False
        If OpenTapeDrive(TapeDrive, driveHandle) Then
            SyncLock SCSIOperationLock
                RaiseEvent IOCtlStart()
                result = SCSIIOCtl.IOCtlDirect(driveHandle, cdb, dataBuffer, bufferLength, dataIn, timeoutValue, senseBuffer)
                RaiseEvent IOCtlFinished()
            End SyncLock
            CloseTapeDrive(driveHandle)
        End If
        Return result
    End Function

    Public Class SCSIIOCtl
        Public Const IOCTL_SCSI_GET_INQUIRY_DATA As Integer = &H4100C
        Public Const IOCTL_SCSI_PASS_THROUGH_DIRECT As Integer = &H4D014
        Public Const IOCTL_STORAGE_BASE As Integer = &H2D
        Public Const METHOD_BUFFERED As Integer = 0
        Public Const FILE_ANY_ACCESS As Integer = 0
        Public Shared ReadOnly Property IOCTL_STORAGE_GET_DEVICE_NUMBER As Integer
            Get
                Return CTL_CODE(IOCTL_STORAGE_BASE, &H420, METHOD_BUFFERED, FILE_ANY_ACCESS)
            End Get
        End Property
        Public Shared Function CTL_CODE(DeviceType As Integer, [Function] As Integer, [Method] As Integer, Access As Integer) As Integer
            Return ((DeviceType << 16) Or (Access << 14) Or ([Function] << 2) Or [Method])
        End Function
        <StructLayout(LayoutKind.Sequential)>
        Public Class SCSI_PASS_THROUGH_DIRECT
            Public Const CdbBufferLength As Integer = 16

            Public Length As UShort
            Public ScsiStatus As Byte
            Public PathId As Byte
            Public TargetId As Byte
            Public Lun As Byte
            Public CdbLength As Byte
            Public SenseInfoLength As Byte
            Public DataIn As Byte
            Public DataTransferLength As UInteger
            Public TimeOutValue As UInteger
            Public DataBuffer As IntPtr
            Public SenseInfoOffset As UInteger

            <MarshalAs(UnmanagedType.ByValArray, SizeConst:=CdbBufferLength)>
            Public Cdb(CdbBufferLength - 1) As Byte

            Public Sub New()
                ReDim Cdb(CdbBufferLength)
            End Sub
        End Class
        <StructLayout(LayoutKind.Sequential)>
        Public Class SCSI_PASS_THROUGH_DIRECT_WITH_BUFFER
            Public Const SendBufferLength As Integer = 64
            Public Spt As SCSI_PASS_THROUGH_DIRECT = New SCSI_PASS_THROUGH_DIRECT()
            <MarshalAs(UnmanagedType.ByValArray, SizeConst:=SendBufferLength)>
            Public Sense(SendBufferLength - 1) As Byte
            Public Sub New()
            End Sub
        End Class
        Public Shared Function BuildSCSIPassThroughStructure(cdb As Byte(),
                                                   dataBuffer As IntPtr,
                                                   bufferLength As UInt32,
                                                   dataIn As Byte,
                                                   timeoutValue As UInt32) As SCSI_PASS_THROUGH_DIRECT_WITH_BUFFER
            Dim scsi As SCSI_PASS_THROUGH_DIRECT_WITH_BUFFER = New SCSI_PASS_THROUGH_DIRECT_WITH_BUFFER()
            With scsi.Spt
                .Length = CUShort(Marshal.SizeOf(scsi.Spt))
                .CdbLength = CByte(cdb.Length)
                Array.Copy(cdb, .Cdb, cdb.Length)
                .DataIn = dataIn
                .DataTransferLength = bufferLength
                .DataBuffer = dataBuffer
                .TimeOutValue = timeoutValue
                .SenseInfoOffset = CUInt(Marshal.OffsetOf(GetType(SCSI_PASS_THROUGH_DIRECT_WITH_BUFFER), "Sense"))
                .SenseInfoLength = CByte(scsi.Sense.Length)
            End With
            Return scsi
        End Function
        Public Shared Function IOCtlDirect(handle As IntPtr,
                                                   cdb As Byte(),
                                                   dataBuffer As IntPtr,
                                                   bufferLength As UInt32,
                                                   dataIn As Byte,
                                                   timeoutValue As UInt32,
                                                   ByRef sense As Byte()) As Boolean
            Dim scsi As SCSI_PASS_THROUGH_DIRECT_WITH_BUFFER = BuildSCSIPassThroughStructure(cdb, dataBuffer, bufferLength, dataIn, timeoutValue)
            Dim size As UInteger = CUInt(Marshal.SizeOf(scsi))
            Dim inBuffer As IntPtr = Marshal.AllocHGlobal(CInt(size))
            Marshal.StructureToPtr(scsi, inBuffer, True)
            Dim result As Boolean
            Dim bytesReturned As UInteger
            result = DeviceIoControl(handle, IOCTL_SCSI_PASS_THROUGH_DIRECT, inBuffer, size, inBuffer, size, bytesReturned, IntPtr.Zero)
            If result Then
                Marshal.PtrToStructure(inBuffer, scsi)
                sense = scsi.Sense
            End If
            Marshal.FreeHGlobal(inBuffer)
            Return result
        End Function
    End Class

    Public Shared Function TapeSCSIIOCtlUnmanaged(handle As IntPtr,
                                           cdb As Byte(),
                                           dataBuffer As IntPtr,
                                           bufferLength As UInt32,
                                           dataIn As Byte,
                                           timeoutValue As UInt32,
                                           ByRef senseBuffer As Byte()) As Boolean
        RaiseEvent IOCtlStart()
        Dim result As Boolean = SCSIIOCtl.IOCtlDirect(handle, cdb, dataBuffer, bufferLength, dataIn, timeoutValue, senseBuffer)
        RaiseEvent IOCtlFinished()
        Return result
    End Function
    Public Shared Event IOCtlStart()
    Public Shared Event IOCtlFinished()

    Public Shared Property DriveOpenCount As New SerializableDictionary(Of String, Integer)
    Public Shared Property DriveHandle As New SerializableDictionary(Of String, IntPtr)
    Public Shared Function OpenTapeDrive(TapeDrive As String, ByRef handle As IntPtr) As Boolean
        SyncLock DriveHandle
            If Not DriveHandle.ContainsKey(TapeDrive) Then
                DriveOpenCount.Add(TapeDrive, 0)
                DriveHandle.Add(TapeDrive, Nothing)
            End If
            If DriveOpenCount(TapeDrive) = 0 Then
                handle = CreateFile(TapeDrive, GENERIC_READ Or GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero)
                DriveHandle(TapeDrive) = handle
            Else
                handle = DriveHandle(TapeDrive)
            End If
            DriveOpenCount(TapeDrive) += 1
            Return handle <> IntPtr.Zero
        End SyncLock
    End Function
    Public Shared Function CloseTapeDrive(handle As IntPtr) As Boolean
        SyncLock DriveHandle
            If DriveHandle.ContainsValue(handle) Then
                For Each key As String In DriveHandle.Keys
                    If DriveHandle(key).Equals(handle) Then
                        If DriveOpenCount(key) > 1 Then
                            DriveOpenCount(key) -= 1
                            Return True
                        ElseIf DriveOpenCount(key) = 1 Then
                            DriveOpenCount(key) -= 1
                            Exit For
                        Else
                            Return True
                        End If
                    End If
                Next
            End If
            Dim result As Boolean = CloseHandle(handle)
            Return result
        End SyncLock
    End Function
    Public Shared Function IsOpened(TapeDrive As String) As Boolean
        SyncLock DriveHandle
            If DriveHandle.ContainsKey(TapeDrive) Then
                Return DriveOpenCount(TapeDrive) > 0
            Else
                Return False
            End If
        End SyncLock
    End Function
    Public Shared Function IsOpened(handle As IntPtr) As Boolean
        SyncLock DriveHandle
            If DriveHandle.ContainsValue(handle) Then
                For Each key As String In DriveHandle.Keys
                    If DriveHandle(key).Equals(handle) Then
                        If DriveOpenCount(key) > 0 Then
                            Return True
                        End If
                    End If
                Next
            Else
                Return False
            End If
        End SyncLock
    End Function


    Structure LPSECURITY_ATTRIBUTES
        Dim nLength As UInt32
        Dim lpSecurityDescriptor As UIntPtr
        Dim bInheritHandle As Boolean
    End Structure
    <DllImport("kernel32.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi, SetLastError:=True)>
    Public Shared Function CreateFile(lpFileName As String,
                                        dwDesiredAccess As UInt32,
                                        dwShareMode As UInt32,
                                        lpSecurityAttributes As IntPtr,
                                        dwCreationDisposition As UInt32,
                                        dwFlagsAndAttributes As UInt32,
                                        hTemplateFile As IntPtr
    ) As IntPtr

    End Function
    <DllImport("kernel32.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi, SetLastError:=True)>
    Public Shared Function CloseHandle(hObject As IntPtr) As Boolean

    End Function
    Public Const GENERIC_READ As UInteger = &H80000000UI
    Public Const GENERIC_WRITE As UInteger = &H40000000UI
    Public Const OPEN_EXISTING As UInteger = 3UI
    Public Const FILE_ATTRIBUTE_NORMAL As UInteger = &H80UI

    Public Shared AllowPartition As Boolean = True
    Public Shared Function ReadAppInfo(TapeDrive As String) As String
        'TC_MAM_APPLICATION_VENDOR = 0x0800 LEN = 8
        'TC_MAM_APPLICATION_NAME = 0x0801 = 0x0800 LEN = 32
        'TC_MAM_APPLICATION_VERSION = 0x0802 = 0x0800 LEN = 8
        Return ReadMAMAttributeString(TapeDrive, 8, 0).TrimEnd(" ") &
            " " & ReadMAMAttributeString(TapeDrive, 8, 1).TrimEnd(" ") &
            " " & ReadMAMAttributeString(TapeDrive, 8, 2).TrimEnd(" ")
    End Function
    Public Shared Function ReadBarcode(TapeDrive As String) As String
        'TC_MAM_BARCODE = 0x0806 LEN = 32
        Return ReadMAMAttributeString(TapeDrive, 8, 6).TrimEnd(" ")
    End Function
    Public Shared Function ReadBarcode(handle As IntPtr) As String
        'TC_MAM_BARCODE = 0x0806 LEN = 32
        Return ReadMAMAttributeString(handle, 8, 6).TrimEnd(" ")
    End Function
    Public Shared Function ReadRemainingCapacity(TapeDrive As String, Optional ByVal Partition As Byte = 0) As UInt64
        Return MAMAttribute.FromTapeDrive(TapeDrive, &H0, Partition).AsNumeric
    End Function
    Public Shared Function TestUnitReady(handle As IntPtr) As Byte()
        SyncLock SCSIOperationLock
            Dim result As Byte() = {}
            SCSIReadParam(handle, {0, 0, 0, 0, 0, 0}, 0, Function(sense As Byte()) As Boolean
                                                             result = sense
                                                             Return True
                                                         End Function)
            Return result
        End SyncLock
    End Function
    Public Shared Function CheckSwitchConfig(handle As IntPtr) As Boolean
        Return CheckSwitchConfig(Inquiry(handle))
    End Function
    Public Shared Function CheckSwitchConfig(Drive As BlockDevice) As Boolean
        If Drive.ProductId.Contains("LTO") OrElse Drive.ProductId.Contains("Ultrium") Then
            My.Settings.TapeUtils_DriverType = DriverType.LTO
            If Drive.ProductId.Contains("Ultrium 3") OrElse Drive.ProductId.Contains("Ultrium 2") OrElse Drive.ProductId.Contains("Ultrium 1") OrElse
            Drive.ProductId.Contains("Gen 3") OrElse Drive.ProductId.Contains("Gen 2") OrElse Drive.ProductId.Contains("Gen 1") Then
                My.Settings.LTFSWriter_DisablePartition = True
            Else
                My.Settings.LTFSWriter_DisablePartition = False
            End If
            Return True
        ElseIf Drive.ProductId.Contains("3592") Then
            My.Settings.TapeUtils_DriverType = DriverType.IBM3592
            If Drive.ProductId.Contains("E06") OrElse Drive.ProductId.Contains("E05") OrElse Drive.ProductId.Contains("J1A") Then
                My.Settings.LTFSWriter_DisablePartition = True
            Else
                My.Settings.LTFSWriter_DisablePartition = False
            End If
            TapeUtils.AllowPartition = Not My.Settings.LTFSWriter_DisablePartition
            Return True
        ElseIf Drive.ProductId.Contains("T10000") Then
            My.Settings.TapeUtils_DriverType = DriverType.T10K
            My.Settings.LTFSWriter_DisablePartition = False
        End If
        Return False
    End Function
    Public Shared Function Inquiry(handle As IntPtr) As BlockDevice
        SyncLock SCSIOperationLock
            Dim PageLen As Byte = SCSIReadParam(handle:=handle, cdbData:={&H12, 1, &H80, 0, 4, 0}, paramLen:=4, senseReport:=Nothing, timeout:=10)(3) + 4
            If PageLen <> 4 Then
                Dim PageData() As Byte = SCSIReadParam(handle:=handle, cdbData:={&H12, 1, &H80, 0, PageLen, 0}, paramLen:=PageLen, senseReport:=Nothing, timeout:=10)
                Dim SN As String = Encoding.ASCII.GetString(PageData.Skip(4).ToArray()).Replace(vbNullChar, "").TrimEnd(" ").TrimStart(" ")
                PageData = SCSIReadParam(handle:=handle, cdbData:={&H12, 0, 0, 0, &H60, 0}, paramLen:=&H60, senseReport:=Nothing, timeout:=10)
                Dim Vendor As String = Encoding.ASCII.GetString(PageData.Skip(8).Take(8).ToArray()).Replace(vbNullChar, "").TrimEnd(" ").TrimStart(" ")
                Dim Product As String = Encoding.ASCII.GetString(PageData.Skip(16).Take(16).ToArray()).Replace(vbNullChar, "").TrimEnd(" ").TrimStart(" ")
                Return New BlockDevice With {.SerialNumber = SN, .VendorId = Vendor, .ProductId = Product}
            Else
                PageLen = SCSIReadParam(handle:=handle, cdbData:={&H12, 0, 0, 0, 5, 0}, paramLen:=5, senseReport:=Nothing, timeout:=10)(4) + 4
                If PageLen = 4 Then Return Nothing
                Dim PageData() As Byte = SCSIReadParam(handle:=handle, cdbData:={&H12, 0, 0, 0, PageLen, 0}, paramLen:=PageLen, senseReport:=Nothing, timeout:=10)
                Dim SN As String = Encoding.ASCII.GetString(PageData.Skip(36).Take(8).ToArray()).Replace(vbNullChar, "").TrimEnd(" ").TrimStart(" ")
                Dim Vendor As String = Encoding.ASCII.GetString(PageData.Skip(8).Take(8).ToArray()).Replace(vbNullChar, "").TrimEnd(" ").TrimStart(" ")
                Dim Product As String = Encoding.ASCII.GetString(PageData.Skip(16).Take(16).ToArray()).Replace(vbNullChar, "").TrimEnd(" ").TrimStart(" ")
                Product &= " " & Encoding.ASCII.GetString(PageData.Skip(32).Take(4).ToArray()).Replace(vbNullChar, "").TrimEnd(" ").TrimStart(" ")
                Return New BlockDevice With {.SerialNumber = SN, .VendorId = Vendor, .ProductId = Product}
            End If
        End SyncLock
    End Function
    Public Shared Function Inquiry(TapeDrive As String) As BlockDevice
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As BlockDevice = Inquiry(handle)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Class BlockLimits
        Public MaximumBlockLength As UInt64
        Public MinimumBlockLength As UInt16
        Public Shared Widening Operator CType(data As BlockLimits) As UInt64
            Return data.MaximumBlockLength
        End Operator
    End Class
    Public Shared Property GlobalBlockLimit As Integer = 1048576
#Region "SCSIOP_READ"
    Public Shared Function ReadBlock(handle As IntPtr, Optional ByRef sense As Byte() = Nothing, Optional ByVal BlockSizeLimit As UInteger = &H80000, Optional ByVal Truncate As Boolean = False) As Byte()
        Dim senseRaw(63) As Byte
        BlockSizeLimit = Math.Min(BlockSizeLimit, GlobalBlockLimit)
        If sense Is Nothing Then sense = {}
        Dim RawDataU As IntPtr
        Dim DiffBytes As Int32
        Dim DataLen As Integer
        SyncLock SCSIOperationLock
            Select Case DriverTypeSetting
                Case DriverType.SLR1
                    Dim BlockCount As Integer = Math.Ceiling(BlockSizeLimit / 512)
                    RawDataU = SCSIReadParamUnmanaged(handle:=handle, cdbData:={8, 1, BlockCount >> 16 And &HFF, BlockCount >> 8 And &HFF, BlockCount And &HFF, 0},
                                              paramLen:=BlockSizeLimit, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                         senseRaw = senseData
                                                                                         Return True
                                                                                     End Function)
                Case Else
                    RawDataU = SCSIReadParamUnmanaged(handle:=handle, cdbData:={8, 0, BlockSizeLimit >> 16 And &HFF, BlockSizeLimit >> 8 And &HFF, BlockSizeLimit And &HFF, 0},
                                              paramLen:=BlockSizeLimit, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                         senseRaw = senseData
                                                                                         Return True
                                                                                     End Function)

            End Select
            sense = senseRaw
            For i As Integer = 3 To 6
                DiffBytes <<= 8
                DiffBytes = DiffBytes Or sense(i)
            Next
            If Truncate Then DiffBytes = Math.Max(DiffBytes, 0)
            DataLen = Math.Min(BlockSizeLimit, BlockSizeLimit - DiffBytes)
            If Not Truncate AndAlso DiffBytes < 0 AndAlso (BlockSizeLimit - DiffBytes) < GlobalBlockLimit Then
                Marshal.FreeHGlobal(RawDataU)
                Dim p As New PositionData(handle:=handle)
                Locate(handle:=handle, BlockAddress:=p.BlockNumber - 1, Partition:=p.PartitionNumber)
                Return ReadBlock(handle:=handle, sense:=sense, BlockSizeLimit:=(BlockSizeLimit - DiffBytes), Truncate:=Truncate)
            End If

        End SyncLock
        Dim RawData(DataLen - 1) As Byte
        Marshal.Copy(RawDataU, RawData, 0, Math.Min(BlockSizeLimit, DataLen))
        Marshal.FreeHGlobal(RawDataU)
        Return RawData
    End Function
    Public Shared Function ReadBlock(TapeDrive As String) As Byte()
        Return ReadBlock(TapeDrive, Nothing, &H80000, False)
    End Function
    Public Shared Function ReadBlock(TapeDrive As String, BlockSizeLimit As UInteger) As Byte()
        Return ReadBlock(TapeDrive, Nothing, BlockSizeLimit, False)
    End Function
    Public Shared Function ReadBlock(TapeDrive As String, ByRef sense As Byte(), BlockSizeLimit As UInteger) As Byte()
        Return ReadBlock(TapeDrive, sense, BlockSizeLimit, False)
    End Function
    Public Shared Function ReadBlock(TapeDrive As String, ByRef sense As Byte()) As Byte()
        Return ReadBlock(TapeDrive, sense, &H80000, False)
    End Function
    Public Shared Function ReadBlock(TapeDrive As String, ByRef sense As Byte(), ByVal BlockSizeLimit As UInteger, ByVal Truncate As Boolean) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = ReadBlock(handle, sense, BlockSizeLimit, Truncate)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function

#End Region
#Region "SCSIOP_READ_BLOCK_LIMITS"
    Public Shared Function ReadBlockLimits(TapeDrive As String) As BlockLimits
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As BlockLimits = ReadBlockLimits(handle)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ReadBlockLimits(handle As IntPtr) As BlockLimits
        Dim data As Byte() = SCSIReadParam(handle, {5, 0, 0, 0, 0, 0}, 6)
        Return New BlockLimits With {.MaximumBlockLength = CULng(data(1)) << 16 Or CULng(data(2)) << 8 Or data(3),
            .MinimumBlockLength = CUShort(data(4)) << 8 Or data(5)}
    End Function
#End Region
#Region "SCSIOP_READ_BUFFER"
    Public Shared Function ReadBuffer(handle As IntPtr, BufferID As Byte) As Byte()
        Return ReadBuffer(handle, BufferID, 2)
    End Function
    Public Shared Function ReadBuffer(handle As IntPtr, BufferID As Byte, Mode As Byte) As Byte()
        'Get EEPROM buffer Length
        Dim cdbD0 As Byte() = {&H3C, 3, BufferID, 0, 0, 0, 0, 0, 4, 0}
        Dim lenData As Byte() = {0, 0, 0, 0}
        Dim data0 As IntPtr = Marshal.AllocHGlobal(lenData.Length)
        Marshal.Copy(lenData, 0, data0, lenData.Length)
        Dim sense(64) As Byte
        SyncLock SCSIOperationLock
            Flush(handle)
            TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdbD0, data0, lenData.Length, 1, 60, sense)
            Marshal.Copy(data0, lenData, 0, lenData.Length)
            Marshal.FreeHGlobal(data0)
            Dim BufferLen As Integer
            For i As Integer = 1 To lenData.Length - 1
                BufferLen <<= 8
                BufferLen = BufferLen Or lenData(i)
            Next

            'Dump EEPROM
            Dim cdbD1 As Byte() = {&H3C, Mode, BufferID, 0, 0, 0, lenData(1), lenData(2), lenData(3), 0}
            Dim dumpData(BufferLen - 1) As Byte
            Dim data1 As IntPtr = Marshal.AllocHGlobal(dumpData.Length)
            Marshal.Copy(dumpData, 0, data1, dumpData.Length)
            TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdbD1, data1, dumpData.Length, 1, 60, sense)
            Marshal.Copy(data1, dumpData, 0, dumpData.Length)
            Marshal.FreeHGlobal(data1)
            Return dumpData
        End SyncLock
    End Function
    Public Shared Function ReadBuffer(TapeDrive As String, BufferID As Byte) As Byte()
        Return ReadBuffer(TapeDrive, BufferID, 2)
    End Function
    Public Shared Function ReadBuffer(TapeDrive As String, BufferID As Byte, Mode As Byte) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = ReadBuffer(handle, BufferID, Mode)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ReceiveDiagCM(TapeDrive As String, Optional ByVal len10h As Integer = 0) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = ReceiveDiagCM(handle, len10h)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ReceiveDiagCM(handle As IntPtr, Optional ByVal len10h As Integer = 0) As Byte()
        Dim bufferrawdata As Byte()
        SyncLock SCSIOperationLock
            TapeUtils.SendSCSICommand(handle, {&H1D, &H11, 0, 0, &H14, 0}, {&HB0, 0, 0, &H10, 0, 0, 0, 0, 0, 0, &H1F, &HE0, 0, 0, 0, &H15, 0, 0, 0, 8}, 0)
            Dim len As UInteger = &HC7A2
            If len10h = 0 Then len10h = TapeUtils.ReadBuffer(handle, &H10).Length
            If len10h > 0 Then len = 6 + (len10h \ 16) * 50 + (len10h Mod 16) * 3
            bufferrawdata = TapeUtils.SCSIReadParam(handle, {&H1C, 1, &HB0, CByte((len >> 8) And &HFF), CByte(len And &HFF), 0}, &HC7A2)
        End SyncLock

        Dim bufferdgtext As String = System.Text.Encoding.ASCII.GetString(bufferrawdata, 6, bufferrawdata.Count - 6)
        bufferdgtext = bufferdgtext.Replace(Chr(0), "")
        Dim textlines As String() = bufferdgtext.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        Dim bufferdata As New List(Of Byte)
        For Each l As String In textlines
            If l Is Nothing OrElse l.Length <= 2 Then Continue For
            Dim dataline As String() = l.Split({" "}, StringSplitOptions.RemoveEmptyEntries)
            For Each b As String In dataline
                Try
                    bufferdata.Add(Convert.ToByte(b, 16))
                Catch ex As Exception
                    If b IsNot Nothing Then
                        Throw New Exception($"Error with line:{l}{vbCrLf}    byte {b}({ex.ToString()}){vbCrLf}")
                    Else
                        Throw New Exception($"Error with line:{l}{vbCrLf}({ex.ToString()}){vbCrLf}")
                    End If
                End Try
            Next
        Next
        Return bufferdata.ToArray()
    End Function
#End Region

    Public Shared Function ReadFileMark(handle As IntPtr, Optional ByRef sense As Byte() = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim data As Byte() = ReadBlock(handle:=handle, sense:=sense)
            If data.Length = 0 Then Return True
            Dim p As New PositionData(handle)
            If Not TapeUtils.AllowPartition Then
                Space6(handle:=handle, Count:=-1, Code:=LocateDestType.Block)
            Else
                Locate(handle:=handle, BlockAddress:=p.BlockNumber - 1, Partition:=p.PartitionNumber)
            End If
            Return False
        End SyncLock
    End Function
    Public Shared Function ReadFileMark(TapeDrive As String, Optional ByRef sense As Byte() = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = ReadFileMark(handle, sense)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ReadToFileMark(handle As IntPtr) As Byte()
        Return ReadToFileMark(handle:=handle, BlockSizeLimit:=&H80000)
    End Function
    Public Shared Function ReadToFileMark(handle As IntPtr, ByVal BlockSizeLimit As UInteger) As Byte()
        SyncLock SCSIOperationLock
            Dim param As Byte() = SCSIReadParam(handle, {&H34, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 20)
            Dim buffer As New List(Of Byte)
            BlockSizeLimit = Math.Min(BlockSizeLimit, GlobalBlockLimit)
            While True
                Dim sense(63) As Byte
                Dim readData As Byte() = ReadBlock(handle, sense, BlockSizeLimit)
                Dim Add_Key As UInt16 = CInt(sense(12)) << 8 Or sense(13)
                If readData.Length > 0 Then
                    buffer.AddRange(readData)
                End If
                If (Add_Key >= 1 And Add_Key <> 4) Then
                    Exit While
                End If
            End While
            Return buffer.ToArray()
        End SyncLock
    End Function
    Public Shared Function ReadToFileMark(handle As IntPtr, outputFileName As String) As Boolean
        Return ReadToFileMark(handle:=handle, outputFileName:=outputFileName, BlockSizeLimit:=&H80000)
    End Function
    Public Shared Function ReadToFileMark(handle As IntPtr, outputFileName As String, ByVal BlockSizeLimit As Integer) As Boolean
        Return ReadToFileMark(handle, outputFileName, CUInt(BlockSizeLimit))
    End Function
    Public Shared Function ReadToFileMark(handle As IntPtr, outputFileName As String, ByVal BlockSizeLimit As UInteger) As Boolean
        SyncLock SCSIOperationLock
            Dim param As Byte() = SCSIReadParam(handle, {&H34, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 20)
            Dim buffer As New IO.FileStream(outputFileName, IO.FileMode.Create, IO.FileAccess.ReadWrite, IO.FileShare.Read)
            BlockSizeLimit = Math.Min(BlockSizeLimit, GlobalBlockLimit)
            While True
                Dim sense(63) As Byte
                Dim readData As Byte() = ReadBlock(handle, sense, BlockSizeLimit)
                Dim Add_Key As UInt16 = CInt(sense(12)) << 8 Or sense(13)
                If readData.Length > 0 Then
                    buffer.Write(readData, 0, readData.Length)
                End If
                If (Add_Key >= 1 And Add_Key <> 4) Then
                    Exit While
                End If
            End While
            buffer.Close()
            Return True
        End SyncLock
    End Function
    Public Shared Function ReadToFileMark(TapeDrive As String) As Byte()
        Return ReadToFileMark(TapeDrive:=TapeDrive, BlockSizeLimit:=&H80000)
    End Function
    Public Shared Function ReadToFileMark(TapeDrive As String, ByVal BlockSizeLimit As UInteger) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = ReadToFileMark(handle, BlockSizeLimit)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ReadToFileMark(TapeDrive As String, outputFileName As String) As Boolean
        Return ReadToFileMark(TapeDrive:=TapeDrive, outputFileName:=outputFileName, BlockSizeLimit:=&H80000)
    End Function
    Public Shared Function ReadToFileMark(TapeDrive As String, outputFileName As String, ByVal BlockSizeLimit As Integer) As Boolean
        Return ReadToFileMark(TapeDrive, outputFileName, CUInt(BlockSizeLimit))
    End Function
    Public Shared Function ReadToFileMark(TapeDrive As String, outputFileName As String, ByVal BlockSizeLimit As UInteger) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = ReadToFileMark(handle, outputFileName, BlockSizeLimit)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ReadEOWPosition(TapeDrive As String) As Byte()
        Dim lenData As Byte() = SCSIReadParam(TapeDrive, {&HA3, &H1F, &H45, 2, 0, 0, 0, 0, 0, 2, 0, 0}, 2)
        Dim len As UInt16 = lenData(0)
        len <<= 8
        len = len Or lenData(1)
        len += 2
        Dim rawParam As Byte() = SCSIReadParam(TapeDrive:=TapeDrive, cdbData:={&HA3, &H1F, &H45, 2, 0, 0, 0, 0, len >> 8, len And &HFF, 0, 0}, paramLen:=len)
        Return rawParam.Skip(4).ToArray()
    End Function
    Public Enum LocateDestType
        Block = 0
        FileMark = 1
        EOD = 3
    End Enum
    Public Shared Function Locate(handle As IntPtr, BlockAddress As UInt64, Partition As Byte) As UInt16
        Return Locate(handle:=handle, BlockAddress:=BlockAddress, Partition:=Partition, DestType:=0)
    End Function
    Public Shared Function Locate(handle As IntPtr, BlockAddress As UInt64, Partition As Byte, ByVal DestType As LocateDestType) As UInt16
        SyncLock SCSIOperationLock
            Dim sense(63) As Byte
            Select Case DriverTypeSetting

                Case DriverType.M2488

                Case DriverType.SLR3
                    Select Case DestType
                        Case LocateDestType.Block
                            SCSIReadParam(handle:=handle, cdbData:={&H2B, 4, 0, BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                                                 0, 0, 0}, paramLen:=0, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                                         sense = senseData
                                                                                                         Return True
                                                                                                     End Function)
                        Case LocateDestType.FileMark
                            Locate(handle, 0, 0)
                            Space6(handle:=handle, Count:=BlockAddress, Code:=LocateDestType.FileMark)
                        Case LocateDestType.EOD
                            If Not ReadPosition(handle).EOD Then
                                SendSCSICommand(handle:=handle, cdbData:={&H11, 3, 0, 0, 0, 0}, DataIn:=1, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                                                            sense = senseData
                                                                                                                            Return True
                                                                                                                        End Function)
                            End If
                    End Select
                Case DriverType.SLR1
                    Select Case DestType
                        Case LocateDestType.Block
                            SCSIReadParam(handle:=handle, cdbData:={&HC, 0, BlockAddress >> 16 And &HF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                                            0}, paramLen:=0, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                              sense = senseData
                                                                                              Return True
                                                                                          End Function)
                        Case LocateDestType.FileMark
                            Locate(handle, 0, 0)
                            Space6(handle:=handle, Count:=BlockAddress, Code:=LocateDestType.FileMark)
                        Case LocateDestType.EOD
                            If Not ReadPosition(handle).EOD Then
                                SendSCSICommand(handle:=handle, cdbData:={&H11, 3, 0, 0, 0, 0}, DataIn:=1, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                                                            sense = senseData
                                                                                                                            Return True
                                                                                                                        End Function)
                            End If
                    End Select
                Case Else
                    If AllowPartition OrElse DestType <> 0 Then
                        Dim CP As Byte = 0
                        If ReadPosition(handle).PartitionNumber <> Partition Then CP = 1
                        SCSIReadParam(handle:=handle, cdbData:={&H92, DestType << 3 Or CP << 1, 0, Partition,
                                                        BlockAddress >> 56 And &HFF, BlockAddress >> 48 And &HFF, BlockAddress >> 40 And &HFF, BlockAddress >> 32 And &HFF,
                                                        BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                                        0, 0, 0, 0}, paramLen:=0, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                                   sense = senseData
                                                                                                   Return True
                                                                                               End Function)
                    Else
                        SCSIReadParam(handle:=handle, cdbData:={&H2B, 0, 0, BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                                        0, 0, 0}, paramLen:=0, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                                sense = senseData
                                                                                                Return True
                                                                                            End Function)
                    End If
            End Select


            Dim Add_Code As UInt16 = CInt(sense(12)) << 8 Or sense(13)
            If Add_Code <> 0 AndAlso ((sense(2) And &HF) <> 8) Then
                If DestType = LocateDestType.EOD Then
                    If Not ReadPosition(handle).EOD Then
                        SendSCSICommand(handle:=handle, cdbData:={&H11, 3, 0, 0, 0, 0}, DataIn:=1, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                                                    sense = senseData
                                                                                                                    Return True
                                                                                                                End Function)
                    End If
                ElseIf DestType = LocateDestType.FileMark Then
                    Locate(handle, 0, 0)
                    Space6(handle:=handle, Count:=BlockAddress, Code:=LocateDestType.FileMark)
                Else

                    SCSIReadParam(handle:=handle, cdbData:={&H92, DestType << 3, 0, 0,
                                            BlockAddress >> 56 And &HFF, BlockAddress >> 48 And &HFF, BlockAddress >> 40 And &HFF, BlockAddress >> 32 And &HFF,
                                            BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                            0, 0, 0, 0}, paramLen:=64, senseReport:=Function(senseData As Byte()) As Boolean
                                                                                        sense = senseData
                                                                                        Return True
                                                                                    End Function)

                End If
                Add_Code = CInt(sense(12)) << 8 Or sense(13)
            Else
                Add_Code = 0
            End If
            Return Add_Code
        End SyncLock
    End Function
    Public Shared Function Locate(TapeDrive As String, BlockAddress As UInt64, Partition As Byte) As UInt16
        Return Locate(TapeDrive:=TapeDrive, BlockAddress:=BlockAddress, Partition:=Partition, DestType:=0)
    End Function
    Public Shared Function Locate(TapeDrive As String, BlockAddress As Int64, Partition As Byte, ByVal DestType As LocateDestType) As UInt16
        Return Locate(TapeDrive, CULng(BlockAddress), Partition, DestType)
    End Function
    Public Shared Function Locate(TapeDrive As String, BlockAddress As ULong, Partition As ltfsindex.PartitionLabel, DestType As LocateDestType)
        Return Locate(TapeDrive:=TapeDrive, BlockAddress:=BlockAddress, Partition:=CByte(Partition), DestType:=DestType)
    End Function
    Public Shared Function Locate(TapeDrive As String, BlockAddress As UInt64, Partition As Byte, ByVal DestType As LocateDestType) As UInt16
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As UInt16 = Locate(handle, BlockAddress, Partition, DestType)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function Space6(handle As IntPtr, Count As Integer) As UInt16
        Return Space6(handle:=handle, Count:=Count, Code:=0)
    End Function
    Public Shared Function Space6(handle As IntPtr, Count As Integer, Code As LocateDestType) As UInt16
        Dim sense(63) As Byte
        SCSIReadParam(handle:=handle, cdbData:={&H11, Code, Count >> 16 And &HFF, Count >> 8 And &HFF, Count And &HFF,
                                            0}, paramLen:=64, senseReport:=Function(senseData As Byte()) As Boolean
                                                                               sense = senseData
                                                                               Return True
                                                                           End Function)
        Dim Add_Code As UInt16 = CInt(sense(12)) << 8 Or sense(13)
        Return Add_Code
    End Function
    Public Shared Function Space6(TapeDrive As String, Count As Integer) As UInt16
        Return Space6(TapeDrive:=TapeDrive, Count:=Count, Code:=0)
    End Function
    Public Shared Function Space6(TapeDrive As String, Count As Integer, ByVal Code As LocateDestType) As UInt16
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As UInt16 = Space6(handle, Count, Code)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function SCSIReadParam(handle As IntPtr, cdbData As Byte(), paramLen As Integer) As Byte()
        Return SCSIReadParam(handle, cdbData, paramLen, Nothing)
    End Function
    Public Shared Function SCSIReadParam(handle As IntPtr, cdbData As Byte(), paramLen As Integer, ByVal senseReport As Func(Of Byte(), Boolean), Optional ByVal timeout As Integer = 60000) As Byte()
        Dim paramData(paramLen - 1) As Byte
        Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(paramLen)
        Marshal.Copy(paramData, 0, dataBuffer, paramLen)
        Dim senseData(63) As Byte
        TapeSCSIIOCtlUnmanaged(handle, cdbData, dataBuffer, paramLen, 1, timeout, senseData)
        Marshal.Copy(dataBuffer, paramData, 0, paramLen)
        Marshal.FreeHGlobal(dataBuffer)
        If senseReport IsNot Nothing Then senseReport(senseData)
        Return paramData
    End Function
    Public Shared Function SCSIReadParam(TapeDrive As String, cdbData As Byte(), paramLen As Integer) As Byte()
        Return SCSIReadParam(TapeDrive, cdbData, paramLen, Nothing)
    End Function
    Public Shared Function SCSIReadParam(TapeDrive As String, cdbData As Byte(), paramLen As Integer, ByVal senseReport As Func(Of Byte(), Boolean)) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = SCSIReadParam(handle, cdbData, paramLen, senseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function SCSIReadParamUnmanaged(handle As IntPtr, cdbData As Byte(), paramLen As Integer) As IntPtr
        Return SCSIReadParamUnmanaged(handle, cdbData, paramLen, Nothing)
    End Function
    Public Shared Function SCSIReadParamUnmanaged(handle As IntPtr, cdbData As Byte(), paramLen As Integer, ByVal senseReport As Func(Of Byte(), Boolean)) As IntPtr
        Dim paramData(paramLen - 1) As Byte
        Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(paramLen)
        Marshal.Copy(paramData, 0, dataBuffer, paramLen)
        Dim senseData(63) As Byte
        While Not TapeSCSIIOCtlUnmanaged(handle, cdbData, dataBuffer, paramLen, 1, 60000, senseData)
            Dim ErrCode As UInteger = GetLastError()
            Select Case MessageBox.Show($"{My.Resources.StrSCSIFail}{vbCrLf}{ParseSenseData(senseData)}{vbCrLf}{vbCrLf}ErrCode: 0x{Hex(ErrCode).PadLeft(8, "0")}h",
                                        My.Resources.ResText_Warning,
                                        MessageBoxButtons.AbortRetryIgnore,
                                        Nothing,
                                        MessageBoxDefaultButton.Button2,
                                        MessageBoxOptions.DefaultDesktopOnly)
                Case DialogResult.Abort
                    Throw New Exception("SCSI Error")
                    Exit While
                Case DialogResult.Retry

                Case DialogResult.Ignore
                    Exit While
            End Select
        End While
        If senseReport IsNot Nothing Then senseReport(senseData)
        Return dataBuffer
    End Function
    Public Shared Function SCSIReadParamUnmanaged(TapeDrive As String, cdbData As Byte(), paramLen As Integer) As IntPtr
        Return SCSIReadParamUnmanaged(TapeDrive, cdbData, paramLen, Nothing)
    End Function
    Public Shared Function SCSIReadParamUnmanaged(TapeDrive As String, cdbData As Byte(), paramLen As Integer, ByVal senseReport As Func(Of Byte(), Boolean)) As IntPtr
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As IntPtr = SCSIReadParamUnmanaged(handle, cdbData, paramLen, senseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function LogSense(TapeDrive As String, PageCode As Byte, SubPageCode As Byte, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing, Optional PageControl As Byte = &H1) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = LogSense(handle, PageCode, SubPageCode, senseReport, PageControl)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function LogSense(handle As IntPtr, PageCode As Byte, SubPageCode As Byte, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing, Optional PageControl As Byte = &H1) As Byte()
        SyncLock SCSIOperationLock
            Dim Header As Byte() = SCSIReadParam(handle, {&H4D, 0, PageControl << 6 Or PageCode, SubPageCode, 0, 0, 0, 0, 4, 0}, 4)
            If Header.Length < 4 Then Return {0, 0, 0, 0}
            Dim PageLen As Integer = Header(2)
            PageLen <<= 8
            PageLen = PageLen Or Header(3)
            Return SCSIReadParam(handle:=handle, cdbData:={&H4D, 0, PageControl << 6 Or PageCode, SubPageCode, 0, 0, 0, (PageLen + 4) >> 8 And &HFF, (PageLen + 4) And &HFF, 0}, paramLen:=PageLen + 4, senseReport:=senseReport)
        End SyncLock
    End Function
    Public Shared Function ModeSense(handle As IntPtr, PageID As Byte, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing, Optional ByVal SkipHeader As Boolean = True) As Byte()
        SyncLock SCSIOperationLock
            Dim Header As Byte() = SCSIReadParam(handle, {&H1A, 0, PageID, 0, 4, 0}, 4)
            If Header.Length = 0 Then Return {0, 0, 0, 0}
            Dim PageLen As Byte = Header(0)
            If PageLen = 0 Then Return {0, 0, 0, 0}
            Dim DescriptorLen As Byte = Header(3)
            If SkipHeader Then
                Return SCSIReadParam(handle:=handle, cdbData:={&H1A, 0, PageID, 0, PageLen + 1, 0}, paramLen:=PageLen + 1, senseReport:=senseReport).Skip(4 + DescriptorLen).ToArray()
            Else
                Return SCSIReadParam(handle:=handle, cdbData:={&H1A, 0, PageID, 0, PageLen + 1, 0}, paramLen:=PageLen + 1, senseReport:=senseReport).ToArray()
            End If
        End SyncLock
    End Function
    Public Shared Function ModeSense(TapeDrive As String, PageID As Byte, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing, Optional ByVal SkipHeader As Boolean = True) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = ModeSense(handle, PageID, senseReport, SkipHeader)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ModeSelect(handle As IntPtr, PageData As Byte(), Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Byte()
        Dim data As Byte() = {0, 0, &H10, 0}.Concat(PageData)
        Dim sense(63) As Byte
        If data.Length < 256 Then
            SendSCSICommand(handle:=handle, cdbData:={&H15, &H10, 0, 0, data.Length, 0}, Data:=data, DataIn:=0,
                            senseReport:=Function(senseData As Byte()) As Boolean
                                             sense = senseData
                                             Return True
                                         End Function)
        Else
            data = {0, 0, 0, &H10, 0, 0, 0, 0}.Concat(PageData)
            SendSCSICommand(handle:=handle, cdbData:={&H55, &H10, 0, 0, 0, 0, 0, data.Length >> 8 And &HFF, data.Length And &HFF, 0}, Data:=data, DataIn:=0,
                           senseReport:=Function(senseData As Byte()) As Boolean
                                            sense = senseData
                                            Return True
                                        End Function)
        End If
        Return sense
    End Function
    Public Shared Function ModeSelect(TapeDrive As String, PageData As Byte(), Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = ModeSelect(handle, PageData, senseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ReadDensityCode(handle As IntPtr) As Byte
        Return SCSIReadParam(handle, {&H1A, 0, 0, 0, &HC, 0}, 12)(4)
    End Function
    Public Shared Function ReadDensityCode(TapeDrive As String) As Byte
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte = ReadDensityCode(handle)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function SetBarcode(handle As IntPtr, barcode As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Dim cdb As Byte() = {&H8D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, &H29, 0, 0}
        Dim data As Byte() = {0, 0, 0, &H29, &H8, &H6, &H1, 0, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20,
            &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20}
        barcode = barcode.PadRight(32).Substring(0, 32)
        For i As Integer = 0 To barcode.Length - 1
            data(9 + i) = CByte(Asc(barcode(i)) And &HFF)
        Next
        Return SendSCSICommand(handle, cdb, data, 0, senseReport)
    End Function
    Public Shared Function SetBarcode(TapeDrive As String, barcode As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = SetBarcode(handle, barcode, senseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function SetBlockSize(handle As IntPtr) As Byte()
        Return SetBlockSize(handle, &H80000)
    End Function
    Public Shared Function SetBlockSize(handle As IntPtr, BlockSize As Int64) As Byte()
        Return SetBlockSize(handle, CULng(BlockSize))
    End Function
    Public Shared Function SetBlockSize(handle As IntPtr, ByVal BlockSize As UInt64) As Byte()
        Dim sense(63) As Byte
        SyncLock SCSIOperationLock
            Dim DensityCode As Byte = ReadDensityCode(handle:=handle)
            BlockSize = Math.Min(BlockSize, GlobalBlockLimit)
            SendSCSICommand(handle:=handle, cdbData:={&H15, &H10, 0, 0, &HC, 0},
                             Data:={0, 0, &H10, 8, DensityCode, 0, 0, 0, 0, BlockSize >> 16 And &HFF, BlockSize >> 8 And &HFF, BlockSize And &HFF}, DataIn:=0,
                            senseReport:=Function(senseData As Byte()) As Boolean
                                             sense = senseData
                                             Return True
                                         End Function)
        End SyncLock
        Return sense
    End Function
    Public Shared Function SetBlockSize(TapeDrive As String) As Byte()
        Return SetBlockSize(TapeDrive, &H80000)
    End Function
    Public Shared Function SetBlockSize(TapeDrive As String, BlockSize As Integer) As Byte()
        Return SetBlockSize(TapeDrive, CUInt(BlockSize))
    End Function
    Public Shared Function SetBlockSize(TapeDrive As String, ByVal BlockSize As UInteger) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = SetBlockSize(handle, BlockSize)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Enum AttributeFormat
        Binary = &H0
        Ascii = &H1
        Text = &H2
        Reserved = &H3
    End Enum
    Public Shared Function SetMAMAttribute(handle As IntPtr, PageID As UInt16, Data As Byte(), Optional ByVal Format As AttributeFormat = 0, Optional ByVal PartitionNumber As Byte = 0, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Dim Param_LEN As UInt64 = Data.Length + 9
        Dim cdb As Byte() = {&H8D, 0, 0, 0, 0, 0, 0, PartitionNumber, 0, 0,
                             Param_LEN >> 24 And &HFF, Param_LEN >> 16 And &HFF, Param_LEN >> 8 And &HFF, Param_LEN And &HFF, 0, 0}
        Dim param As Byte() = {Param_LEN >> 24 And &HFF, Param_LEN >> 16 And &HFF, Param_LEN >> 8 And &HFF, Param_LEN And &HFF,
                               PageID >> 8 And &HFF, PageID And &HFF, Format,
                               Data.Length >> 8 And &HFF, Data.Length And &HFF}
        param = param.Concat(Data).ToArray()
        Return SendSCSICommand(handle, cdb, param, 0, SenseReport)
    End Function
    Public Shared Function SetMAMAttribute(TapeDrive As String, PageID As UInt16, Data As Byte(), Optional ByVal Format As AttributeFormat = 0, Optional ByVal PartitionNumber As Byte = 0, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = SetMAMAttribute(handle, PageID, Data, Format, PartitionNumber, SenseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function SetMAMAttribute(handle As IntPtr, PageID As UInt16, Data As String, Optional ByVal Format As AttributeFormat = 1, Optional ByVal PartitionNumber As Byte = 0, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SetMAMAttribute(handle, PageID, Encoding.UTF8.GetBytes(Data), Format, PartitionNumber, SenseReport)
    End Function
    Public Shared Function SetMAMAttribute(TapeDrive As String, PageID As UInt16, Data As String, Optional ByVal Format As AttributeFormat = 1, Optional ByVal PartitionNumber As Byte = 0, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SetMAMAttribute(TapeDrive, PageID, Encoding.UTF8.GetBytes(Data), Format, PartitionNumber, SenseReport)
    End Function
    Public Shared Function WriteVCI(TapeDrive As String, Generation As UInt64, block0 As UInt64, block1 As UInt64,
                                    UUID As String, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return WriteVCI(TapeDrive, Generation, block0, block1, UUID, 1, SenseReport)
    End Function
    Public Shared Function WriteVCI(TapeDrive As String, Generation As UInt64, block0 As UInt64, block1 As UInt64,
                                    UUID As String, ByVal ExtraPartitionCount As Byte, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = WriteVCI(handle:=handle, Generation:=Generation, block0:=block0, block1:=block1, UUID:=UUID, ExtraPartitionCount:=ExtraPartitionCount, SenseReport:=SenseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function WriteVCI(handle As IntPtr, Generation As UInt64, block0 As UInt64, block1 As UInt64,
                                    UUID As String, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return WriteVCI(handle, Generation, block0, block1, UUID, 1, SenseReport)
    End Function
    Public Shared Function WriteVCI(handle As IntPtr, Generation As UInt64, block0 As UInt64, block1 As UInt64,
                                    UUID As String, ByVal ExtraPartitionCount As Byte, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Flush(handle)
            Dim VCIData As Byte()
            Dim VCI As Byte() = GetMAMAttributeBytes(handle, 0, 9)
            If VCI Is Nothing OrElse VCI.Length = 0 Then Return False
            If ExtraPartitionCount > 0 Then
                VCIData = {8, 0, 0, 0, 0, VCI(VCI.Length - 4), VCI(VCI.Length - 3), VCI(VCI.Length - 2), VCI(VCI.Length - 1),
                Generation >> 56 And &HFF, Generation >> 48 And &HFF, Generation >> 40 And &HFF, Generation >> 32 And &HFF,
                Generation >> 24 And &HFF, Generation >> 16 And &HFF, Generation >> 8 And &HFF, Generation And &HFF,
                block0 >> 56 And &HFF, block0 >> 48 And &HFF, block0 >> 40 And &HFF, block0 >> 32 And &HFF,
                block0 >> 24 And &HFF, block0 >> 16 And &HFF, block0 >> 8 And &HFF, block0 And &HFF,
                0, &H2B, &H4C, &H54, &H46, &H53, 0}
                VCIData = VCIData.Concat(Encoding.ASCII.GetBytes(UUID.PadRight(36).Substring(0, 36))).ToArray
                VCIData = VCIData.Concat({0, 1}).ToArray
                Dim Succ As Boolean = SetMAMAttribute(handle, &H80C, VCIData, AttributeFormat.Binary, 0, SenseReport)
                If Not Succ Then Return False
            End If
            VCIData = {8, 0, 0, 0, 0, VCI(VCI.Length - 4), VCI(VCI.Length - 3), VCI(VCI.Length - 2), VCI(VCI.Length - 1),
                Generation >> 56 And &HFF, Generation >> 48 And &HFF, Generation >> 40 And &HFF, Generation >> 32 And &HFF,
                Generation >> 24 And &HFF, Generation >> 16 And &HFF, Generation >> 8 And &HFF, Generation And &HFF,
                block1 >> 56 And &HFF, block1 >> 48 And &HFF, block1 >> 40 And &HFF, block1 >> 32 And &HFF,
                block1 >> 24 And &HFF, block1 >> 16 And &HFF, block1 >> 8 And &HFF, block1 And &HFF,
                0, &H2B, &H4C, &H54, &H46, &H53, 0}
            VCIData = VCIData.Concat(Encoding.ASCII.GetBytes(UUID.PadRight(36).Substring(0, 36))).ToArray()
            VCIData = VCIData.Concat({0, 1}).ToArray()
            Return SetMAMAttribute(handle, &H80C, VCIData, AttributeFormat.Binary, ExtraPartitionCount, SenseReport)
        End SyncLock
    End Function
    Public Shared Function ParseAdditionalSenseCode(Add_Code As UInt16) As String
        Dim Msg As New StringBuilder
        Select Case Add_Code
            Case &H0
                Msg.Append("No addition sense")
            Case &H1
                Msg.Append("Filemark detected")
            Case &H2
                Msg.Append("End of Tape detected")
            Case &H4
                Msg.Append("Beginning of Tape detected")
            Case &H5
                Msg.Append("End of Data detected")
            Case &H16
                Msg.Append("Operation in progress")
            Case &H18
                Msg.Append("Erase operation in progress")
            Case &H19
                Msg.Append("Locate operation in progress")
            Case &H1A
                Msg.Append("Rewind operation in progress")
            Case &H400
                Msg.Append("LUN not ready, cause not reportable")
            Case &H401
                Msg.Append("LUN in process of becoming ready")
            Case &H402
                Msg.Append("LUN not ready, Initializing command required")
            Case &H404
                Msg.Append("LUN not ready, format in progress")
            Case &H407
                Msg.Append("Command in progress")
            Case &H409
                Msg.Append("LUN not ready, self-test in progress")
            Case &H40C
                Msg.Append("LUN not accessible, port in unavailable state")
            Case &H412
                Msg.Append("Logical unit offline")
            Case &H800
                Msg.Append("Logical unit communication failure")
            Case &HB00
                Msg.Append("Warning")
            Case &HB01
                Msg.Append("Thermal limit exceeded")
            Case &HC00
                Msg.Append("Write error")
            Case &HE01
                Msg.Append("Information unit too short")
            Case &HE02
                Msg.Append("Information unit too long")
            Case &HE03
                Msg.Append("SK Illegal Request")
            Case &H1001
                Msg.Append("Logical block guard check failed")
            Case &H1100
                Msg.Append("Unrecovered read error")
            Case &H1112
                Msg.Append("Media Auxiliary Memory read error")
            Case &H1400
                Msg.Append("Recorded entity not found")
            Case &H1403
                Msg.Append("End of Data not found")
            Case &H1A00
                Msg.Append("Parameter list length error")
            Case &H2000
                Msg.Append("Invalid command operation code")
            Case &H2400
                Msg.Append("Invalid field in Command Descriptor Block")
            Case &H2500
                Msg.Append("LUN not supported")
            Case &H2600
                Msg.Append("Invalid field in parameter list")
            Case &H2601
                Msg.Append("Parameter not supported")
            Case &H2602
                Msg.Append("Parameter value invalid")
            Case &H2604
                Msg.Append("Invalid release of persistent reservation")
            Case &H2610
                Msg.Append("Data decryption key fail limit reached")
            Case &H2680
                Msg.Append("Invalid CA certificate")
            Case &H2700
                Msg.Append("Write-protected")
            Case &H2708
                Msg.Append("Too many logical objects on partition to support operation")
            Case &H2800
                Msg.Append("Not ready to ready transition, medium may have changed")
            Case &H2901
                Msg.Append("Power-on reset")
            Case &H2902
                Msg.Append("SCSI bus reset")
            Case &H2903
                Msg.Append("Bus device reset")
            Case &H2904
                Msg.Append("Internal firmware reboot")
            Case &H2907
                Msg.Append("I_T nexus loss occurred")
            Case &H2A01
                Msg.Append("Mode parameters changed")
            Case &H2A02
                Msg.Append("Log parameters changed")
            Case &H2A03
                Msg.Append("Reservations pre-empted")
            Case &H2A04
                Msg.Append("Reservations released")
            Case &H2A05
                Msg.Append("Registrations pre-empted")
            Case &H2A06
                Msg.Append("Asymmetric access state changed")
            Case &H2A07
                Msg.Append("Asymmetric access state transition failed")
            Case &H2A08
                Msg.Append("Priority changed")
            Case &H2A0D
                Msg.Append("Data encryption capabilities changed")
            Case &H2A10
                Msg.Append("Timestamp changed")
            Case &H2A11
                Msg.Append("Data encryption parameters changed by another initiator")
            Case &H2A12
                Msg.Append("Data encryption parameters changed by a vendor-specific event")
            Case &H2A13
                Msg.Append("Data Encryption Key Instance Counter has changed")
            Case &H2A14
                Msg.Append("SA creation capabilities data has changed")
            Case &H2A15
                Msg.Append("Medium removal prevention pre-empted")
            Case &H2A80
                Msg.Append("Security configuration changed")
            Case &H2C00
                Msg.Append("Command sequence invalid")
            Case &H2C07
                Msg.Append("Previous busy status")
            Case &H2C08
                Msg.Append("Previous task set full status")
            Case &H2C09
                Msg.Append("Previous reservation conflict status")
            Case &H2C0B
                Msg.Append("Not reserved")
            Case &H2F00
                Msg.Append("Commands cleared by another initiator")
            Case &H3000
                Msg.Append("Incompatible medium installed")
            Case &H3001
                Msg.Append("Cannot read media, unknown format")
            Case &H3002
                Msg.Append("Cannot read media: incompatible format")
            Case &H3003
                Msg.Append("Cleaning cartridge installed")
            Case &H3004
                Msg.Append("Cannot write medium")
            Case &H3005
                Msg.Append("Cannot write medium, incompatible format")
            Case &H3006
                Msg.Append("Cannot format, incompatible medium")
            Case &H3007
                Msg.Append("Cleaning failure")
            Case &H300C
                Msg.Append("WORM medium—overwrite attempted")
            Case &H300D
                Msg.Append("WORM medium—integrity check failed")
            Case &H3100
                Msg.Append("Medium format corrupted")
            Case &H3700
                Msg.Append("Rounded parameter")
            Case &H3A00
                Msg.Append("Medium not present")
            Case &H3A04
                Msg.Append("Medium not present, Media Auxiliary Memory accessible")
            Case &H3B00
                Msg.Append("Sequential positioning error")
            Case &H3B0C
                Msg.Append("Position past BOM")
            Case &H3B1C
                Msg.Append("Too many logical objects on partition to support operation.")
            Case &H3E00
                Msg.Append("Logical unit has not self-configured yet")
            Case &H3F01
                Msg.Append("Microcode has been changed")
            Case &H3F03
                Msg.Append("Inquiry data has changed")
            Case &H3F05
                Msg.Append("Device identifier changed")
            Case &H3F0E
                Msg.Append("Reported LUNs data has changed")
            Case &H3F0F
                Msg.Append("Echo buffer overwritten")
            Case &H4300
                Msg.Append("Message error")
            Case &H4400
                Msg.Append("Internal target failure")
            Case &H4500
                Msg.Append("Selection/reselection failure")
            Case &H4700
                Msg.Append("SCSI parity error")
            Case &H4800
                Msg.Append("Initiator Detected Error message received")
            Case &H4900
                Msg.Append("Invalid message")
            Case &H4B00
                Msg.Append("Data phase error")
            Case &H4B02
                Msg.Append("Too much write data")
            Case &H4B03
                Msg.Append("ACK/NAK timeout")
            Case &H4B04
                Msg.Append("NAK received")
            Case &H4B05
                Msg.Append("Data offset error")
            Case &H4B06
                Msg.Append("Initiator response timeout")
            Case &H4D00
                Msg.Append("Tagged overlapped command")
            Case &H4E00
                Msg.Append("Overlapped commands")
            Case &H5000
                Msg.Append("Write append error")
            Case &H5200
                Msg.Append("Cartridge fault")
            Case &H5300
                Msg.Append("Media load or eject failed")
            Case &H5301
                Msg.Append("Unload tape failure")
            Case &H5302
                Msg.Append("Medium removal prevented")
            Case &H5303
                Msg.Append("Insufficient resources")
            Case &H5304
                Msg.Append("Medium thread or unthread failure")
            Case &H5504
                Msg.Append("Insufficient registration resources")
            Case &H5506
                Msg.Append("Media Auxiliary Memory full")
            Case &H5B01
                Msg.Append("Threshold condition met")
            Case &H5D00
                Msg.Append("Failure prediction threshold exceeded")
            Case &H5DFF
                Msg.Append("Failure prediction threshold exceeded (false)")
            Case &H5E01
                Msg.Append("Idle condition activated by timer")
            Case &H7400
                Msg.Append("Security error")
            Case &H7401
                Msg.Append("Unable to decrypt data")
            Case &H7402
                Msg.Append("Unencrypted data encountered while decrypting")
            Case &H7403
                Msg.Append("Incorrect data encryption key")
            Case &H7404
                Msg.Append("Cryptographic integrity validation failed")
            Case &H7405
                Msg.Append("Key-associated data descriptors changed.")
            Case &H7408
                Msg.Append("Digital signature validation failure")
            Case &H7409
                Msg.Append("Encryption mode mismatch on read")
            Case &H740A
                Msg.Append("Encrypted block not RAW read-enabled")
            Case &H740B
                Msg.Append("Incorrect encryption parameters")
            Case &H7421
                Msg.Append("Data encryption configuration prevented")
            Case &H7440
                Msg.Append("Authentication failed")
            Case &H7461
                Msg.Append("External data encryption Key Manager access error")
            Case &H7462
                Msg.Append("External data encryption Key Manager error")
            Case &H7463
                Msg.Append("External data encryption management—key not found")
            Case &H7464
                Msg.Append("External data encryption management—request not authorized")
            Case &H746E
                Msg.Append("External data encryption control time-out")
            Case &H746F
                Msg.Append("External data encryption control unknown error")
            Case &H7471
                Msg.Append("Logical Unit access not authorized")
            Case &H7480
                Msg.Append("KAD changed")
            Case &H7482
                Msg.Append("Crypto KAD in CM failure")
            Case &H8282
                Msg.Append("Drive requires cleaning")
            Case &H8283
                Msg.Append("Bad microcode detected")
        End Select
        If Add_Code >> 8 = &H40 Then
            Msg.Append("Diagnostic failure on component " & Hex(Add_Code And &HFF) & "h")
        End If
        Return Msg.ToString()
    End Function
    Public Shared Function ParseSenseData(sense As Byte()) As String
        Dim Msg As New StringBuilder
        Dim Fixed As Boolean = False
        Dim Add_Code As Integer
        Dim Valid As Boolean = ((sense(0) >> 7) = 1)
        If (sense(0) And &H7F) = &H70 Then
            Msg.AppendLine("Error code represents current error")
            Fixed = True
        ElseIf (sense(0) And &H7F) = &H71 Then
            Msg.AppendLine("Error code represents deferred error")
            Fixed = True
        End If
        If Fixed Then
            If sense(2) >> 7 = 1 Then
                Msg.AppendLine("Filemark encountered")
            End If
            If ((sense(2) >> 6) And &H1) = 1 Then
                Msg.AppendLine("EOM encountered")
            End If
            If ((sense(2) >> 5) And &H1) = 1 Then
                Msg.AppendLine("Blocklen mismatch")
            End If
            Dim sensekey As Byte = sense(2) And &HF
            Msg.Append("Sense key: ")
            Select Case sensekey
                Case 0
                    Msg.AppendLine("NO SENSE")
                Case 1
                    Msg.AppendLine("RECOVERED ERROR")
                Case 2
                    Msg.AppendLine("NOT READY")
                Case 3
                    Msg.AppendLine("MEDIUM ERROR")
                Case 4
                    Msg.AppendLine("HARDWARE ERROR")
                Case 5
                    Msg.AppendLine("ILLEGAL REQUEST")
                Case 6
                    Msg.AppendLine("UNIT ATTENTION")
                Case 7
                    Msg.AppendLine("DATA PROTECT")
                Case 8
                    Msg.AppendLine("BLANK CHECK")
                Case 9
                    Msg.AppendLine("VENDOR SPECIFIC")
                Case 10
                    Msg.AppendLine("COPY ABORTED")
                Case 11
                    Msg.AppendLine("ABORTED COMMAND")
                Case 12
                    Msg.AppendLine("EQUAL")
                Case 13
                    Msg.AppendLine("VOLUME OVERFLOW")
                Case 14
                    Msg.AppendLine("MISCOMPARE")
                Case 15
                    Msg.AppendLine("RESERVED")
            End Select
            If Valid Then
                Msg.AppendLine("Info bytes: " & Byte2Hex({sense(3), sense(4), sense(5), sense(6)}))
            End If
            Dim Add_Len As Byte = sense(7)
            Add_Code = CInt(sense(12)) << 8 Or sense(13)
            Dim SKSV As Boolean = ((sense(15) >> 7) = 1)
            Dim CD As Boolean = ((sense(15) >> 6) And 1) = 1
            Dim BPV As Boolean = ((sense(15) >> 3) And 1) = 1

            If SKSV Then
                If sensekey = 5 Then
                    Msg.AppendLine("Error byte = " & (CInt(sense(16)) << 8 Or sense(17)) & " bit = " & (sense(15) And 7))
                ElseIf sensekey = 0 Or sensekey = 2 Then
                    Msg.AppendLine("Progress = " & (CInt(sense(16)) << 8 Or sense(17)))
                End If
            Else
                Msg.AppendLine("Drive Error Code = " & Byte2Hex({sense(16), sense(17)}))
            End If
            If ((sense(21) >> 3) And 1) = 1 Then
                Msg.AppendLine("Clean is required")
            End If
        End If
        Msg.Append("Additional code: ")
        Msg.AppendLine(ParseAdditionalSenseCode(Add_Code))
        Return Msg.ToString()
    End Function
    Public Shared Function PreventMediaRemoval(handle As IntPtr, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SendSCSICommand(handle, {&H1E, 0, 0, 0, 1, 0}, Nothing, 1, senseReport)
    End Function
    Public Shared Function PreventMediaRemoval(TapeDrive As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = PreventMediaRemoval(handle, senseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function AllowMediaRemoval(handle As IntPtr, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SendSCSICommand(handle, {&H1E, 0, 0, 0, 0, 0}, Nothing, 1, senseReport)
    End Function
    Public Shared Function AllowMediaRemoval(TapeDrive As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = AllowMediaRemoval(handle, senseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function DoReload(TapeDrive As String, Lock As Boolean, EncryptionKey As Byte()) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = DoReload(handle, Lock, EncryptionKey)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function DoReload(handle As IntPtr, Lock As Boolean, EncryptionKey As Byte()) As Boolean
        SyncLock SCSIOperationLock
            Dim Loc As New PositionData(handle)
            AllowMediaRemoval(handle)
            LoadEject(handle:=handle, LoadOption:=LoadOption.Unthread)
            LoadEject(handle:=handle, LoadOption:=LoadOption.LoadThreaded, EncryptionKey:=EncryptionKey)
            If Lock Then PreventMediaRemoval(handle)
            Locate(handle:=handle, BlockAddress:=Loc.BlockNumber, Partition:=Loc.PartitionNumber, DestType:=TapeUtils.LocateDestType.Block)

        End SyncLock
        Return True
    End Function
    Public Shared Function ReserveUnit(TapeDrive As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = ReserveUnit(handle, senseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ReserveUnit(handle As IntPtr, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SendSCSICommand(handle, {&H16, 0, 0, 0, 0, 0}, Nothing, 1, senseReport)
    End Function
    Public Shared Function ReleaseUnit(TapeDrive As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = ReleaseUnit(handle, senseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function ReleaseUnit(handle As IntPtr, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SendSCSICommand(handle, {&H17, 0, 0, 0, 0, 0}, Nothing, 1, senseReport)
    End Function

    Public Shared Function ReadMAMAttributeString(TapeDrive As String, PageCode_H As Byte, PageCode_L As Byte) As String 'TC_MAM_BARCODE = 0x0806 LEN = 32
        Return System.Text.Encoding.UTF8.GetString(GetMAMAttributeBytes(TapeDrive, PageCode_H, PageCode_L).ToArray())
    End Function
    Public Shared Function ReadMAMAttributeString(handle As IntPtr, PageCode_H As Byte, PageCode_L As Byte) As String 'TC_MAM_BARCODE = 0x0806 LEN = 32
        Return System.Text.Encoding.UTF8.GetString(GetMAMAttributeBytes(handle, PageCode_H, PageCode_L).ToArray())
    End Function
    Public Class PositionData
        Public Property BOP As Boolean
        Public Property EOP As Boolean
        Public Property MPU As Boolean
        Public Property PartitionNumber As Byte
        Public Property BlockNumber As UInt64
        Public Property FileNumber As UInt64
        Public Property SetNumber As UInt64
        Public Property AddSenseKey As UInt16
        Public ReadOnly Property EOD As Boolean
            Get
                Return AddSenseKey = 5
            End Get
        End Property
        Public Sub New()

        End Sub
        Public Sub New(TapeDrive As String)
            Dim data As PositionData = ReadPosition(TapeDrive)
            BOP = data.BOP
            EOP = data.EOP
            MPU = data.MPU
            AddSenseKey = data.AddSenseKey
            PartitionNumber = data.PartitionNumber
            BlockNumber = data.BlockNumber
            FileNumber = data.FileNumber
            SetNumber = data.SetNumber
        End Sub
        Public Sub New(handle As IntPtr)
            Dim data As PositionData = ReadPosition(handle)
            BOP = data.BOP
            EOP = data.EOP
            MPU = data.MPU
            AddSenseKey = data.AddSenseKey
            PartitionNumber = data.PartitionNumber
            BlockNumber = data.BlockNumber
            FileNumber = data.FileNumber
            SetNumber = data.SetNumber
        End Sub
        Public Overrides Function ToString() As String
            Dim Xtrs As String = ""
            If BOP Then Xtrs &= " BOP"
            If EOP Then Xtrs &= " EOP"
            If MPU Then Xtrs &= " MPU"
            If EOD Then Xtrs &= " EOD"
            Return $"P{PartitionNumber} B{BlockNumber} FM{FileNumber} SET{SetNumber}{Xtrs}"
        End Function
    End Class
    Public Shared DriverTypeSetting As DriverType = DriverType.LTO
    Public Enum DriverType
        LTO = 0
        M2488 = 1
        SLR3 = 2
        T10K = 3
        SLR1 = 4
        IBM3592 = 5
    End Enum
    Public Shared Function ReadPosition(handle As IntPtr) As PositionData
        Return ReadPosition(handle, DriverType.LTO)
    End Function
    Public Shared Function ReadPosition(handle As IntPtr, ByVal DriverType As DriverType) As PositionData
        If DriverTypeSetting <> DriverType.LTO Then DriverType = DriverTypeSetting
        Dim param As Byte()
        Dim result As New PositionData
        Dim sense As Byte()
        Select Case DriverType
            Case DriverType.M2488
            Case DriverType.SLR3
                param = SCSIReadParam(handle, {&H34, 1, 0, 0, 0, 0, 0, 0, 0, 0}, 32)
                result.BOP = param(0) >> 7 And &H1
                result.EOP = param(0) >> 6 And &H1
                For i As Integer = 0 To 3
                    result.BlockNumber <<= 8
                    result.BlockNumber = result.BlockNumber Or param(4 + i)
                Next
            Case DriverType.SLR1
                param = SCSIReadParam(handle, {&H2, 0, 0, 0, 3, 0}, 3)
                For i As Integer = 0 To 2
                    result.BlockNumber <<= 8
                    result.BlockNumber = result.BlockNumber Or param(0 + i)
                Next
            Case Else
                If AllowPartition Then
                    param = SCSIReadParam(handle, {&H34, 6, 0, 0, 0, 0, 0, 0, 0, 0}, 32, Function(sdata As Byte())
                                                                                             sense = sdata
                                                                                             Return True
                                                                                         End Function)
                    result.BOP = param(0) >> 7 And &H1
                    result.EOP = param(0) >> 6 And &H1
                    result.MPU = param(0) >> 3 And &H1
                    For i As Integer = 0 To 3
                        result.PartitionNumber <<= 8
                        result.PartitionNumber = result.PartitionNumber Or param(4 + i)
                    Next
                    For i As Integer = 0 To 7
                        result.BlockNumber <<= 8
                        result.BlockNumber = result.BlockNumber Or param(8 + i)
                        result.FileNumber <<= 8
                        result.FileNumber = result.FileNumber Or param(16 + i)
                        result.SetNumber <<= 8
                        result.SetNumber = result.SetNumber Or param(24 + i)
                    Next
                Else
                    param = SCSIReadParam(handle, {&H34, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 32)
                    result.BOP = param(0) >> 7 And &H1
                    result.EOP = param(0) >> 6 And &H1
                    For i As Integer = 0 To 3
                        result.BlockNumber <<= 8
                        result.BlockNumber = result.BlockNumber Or param(4 + i)
                    Next
                End If
        End Select
        If sense IsNot Nothing AndAlso sense.Length >= 14 Then result.AddSenseKey = CInt(sense(12)) << 8 Or sense(13)
        Return result
    End Function
    Public Shared Function ReadPosition(TapeDrive As String) As PositionData
        Return ReadPosition(TapeDrive, My.Settings.TapeUtils_DriverType)
    End Function
    Public Shared Function ReadPosition(TapeDrive As String, ByVal DriverType As DriverType) As PositionData
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As PositionData = ReadPosition(handle, DriverType)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Byte()) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = Write(handle, Data)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function Write(handle As IntPtr, Data As Byte()) As Byte()
        Dim sense(63) As Byte
        Select Case My.Settings.TapeUtils_DriverType

            Case DriverType.SLR3
                Dim succ As Boolean =
            SendSCSICommandUnmanaged(handle, {&HA, 0, Data.Length >> 16 And &HFF, Data.Length >> 8 And &HFF, Data.Length And &HFF, 0}, Data, 0,
                        Function(senseData As Byte()) As Boolean
                            sense = senseData
                            Return True
                        End Function)
                If Not succ Then Throw New Exception("SCSI Failure")
            Case DriverType.SLR1
                Dim BlockCount As Integer = Math.Ceiling(Data.Length / 512)
                Dim succ As Boolean =
            SendSCSICommandUnmanaged(handle, {&HA, 1, BlockCount >> 16 And &HFF, BlockCount >> 8 And &HFF, BlockCount And &HFF, 0}, Data, 0,
                        Function(senseData As Byte()) As Boolean
                            sense = senseData
                            Return True
                        End Function)
                If Not succ Then Throw New Exception("SCSI Failure")
            Case Else
                Dim succ As Boolean =
            SendSCSICommandUnmanaged(handle, {&HA, 0, Data.Length >> 16 And &HFF, Data.Length >> 8 And &HFF, Data.Length And &HFF, 0}, Data, 0,
                        Function(senseData As Byte()) As Boolean
                            sense = senseData
                            Return True
                        End Function)
                If Not succ Then Throw New Exception("SCSI Failure")
        End Select
        Return sense
    End Function
    Public Shared Function Write(TapeDrive As String, Data As IntPtr, Length As Integer) As Byte()
        Return Write(TapeDrive:=TapeDrive, Data:=Data, Length:=Length)
    End Function
    Public Shared Function Write(TapeDrive As String, Data As IntPtr, Length As UInteger) As Byte()
        Return Write(TapeDrive, Data, Length, False)
    End Function
    Public Shared Function Write(handle As IntPtr, Data As IntPtr, Length As UInteger, ByVal senseEnabled As Boolean) As Byte()
        Dim sense() As Byte = {0, 0, 0}
        If senseEnabled Then ReDim sense(63)
        Dim cdbData As Byte() = {&HA, 0, Length >> 16 And &HFF, Length >> 8 And &HFF, Length And &HFF, 0}
        Dim succ As Boolean = TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdbData, Data, Length, 0, 900, sense)
        If Not succ Then Throw New Exception("SCSI Failure")
        Return sense
    End Function
    Public Shared Function Write(TapeDrive As String, Data As IntPtr, Length As UInteger, ByVal senseEnabled As Boolean) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = Write(handle, Data, Length, senseEnabled)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Byte(), BlockSize As UInteger) As Byte()
        Return Write(TapeDrive:=TapeDrive, Data:=Data, BlockSize:=CInt(BlockSize))
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Byte(), BlockSize As Long) As Byte()
        Return Write(TapeDrive:=TapeDrive, Data:=Data, BlockSize:=CInt(BlockSize))
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Byte(), BlockSize As Integer) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = Write(handle, Data, BlockSize)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function

    Public Shared Function Write(handle As IntPtr, Data As Byte(), BlockSize As Integer) As Byte()
        If Data.Length <= BlockSize Then
            Return Write(handle, Data)
        End If
        Dim sense(63) As Byte
        Dim cdbData As Byte() = {}
        Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(BlockSize)
        For i As Integer = 0 To Data.Length - 1 Step BlockSize
            Dim TransferLen As UInteger = Math.Min(BlockSize, Data.Length - i)
            Select Case My.Settings.TapeUtils_DriverType

                Case DriverType.SLR1
                    cdbData = {&HA, 1, TransferLen >> 16 And &HFF, TransferLen >> 8 And &HFF, TransferLen And &HFF, 0}
                Case Else
                    cdbData = {&HA, 0, TransferLen >> 16 And &HFF, TransferLen >> 8 And &HFF, TransferLen And &HFF, 0}
            End Select
            Marshal.Copy(Data, i, dataBuffer, TransferLen)
            Dim succ As Boolean = TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdbData, dataBuffer, TransferLen, 0, 60000, sense)
            If Not succ Then
                Marshal.FreeHGlobal(dataBuffer)
                Throw New Exception("SCSI Failure")
                Return sense
            End If
        Next
        Marshal.FreeHGlobal(dataBuffer)
        Return sense
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Stream) As Byte()
        Return Write(TapeDrive, Data, 524288, False)
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Stream, ByVal BlockSize As UInteger) As Byte()
        Return Write(TapeDrive, Data, CInt(BlockSize), False)
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Stream, ByVal BlockSize As Integer, senseEnabled As Boolean) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = Write(handle, Data, BlockSize, senseEnabled)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function Write(handle As IntPtr, Data As Stream, ByVal BlockSize As Integer, senseEnabled As Boolean, Optional ByVal ProgressReport As Action(Of Long) = Nothing) As Byte()
        Dim sense(63) As Byte
        BlockSize = Math.Min(BlockSize, GlobalBlockLimit)
        Dim DataBuffer(BlockSize - 1) As Byte
        Dim DataPtr As IntPtr = Marshal.AllocHGlobal(BlockSize)
        Dim DataLen As Integer = Data.Read(DataBuffer, 0, BlockSize)
        Dim succ As Boolean
        While DataLen > 0
            Dim cdbData As Byte() = {&HA, 0, DataLen >> 16 And &HFF, DataLen >> 8 And &HFF, DataLen And &HFF, 0}
            Marshal.Copy(DataBuffer, 0, DataPtr, DataLen)
            Do
                succ = TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdbData, DataPtr, DataLen, 0, 60000, sense)
                If succ Then
                    If ProgressReport IsNot Nothing Then
                        ProgressReport(DataLen)
                    End If
                    Exit Do
                Else
                    Select Case MessageBox.Show(New Form With {.TopMost = True}, $"写入出错：SCSI指令执行失败", "警告", MessageBoxButtons.AbortRetryIgnore)
                        Case DialogResult.Abort
                            Exit While
                        Case DialogResult.Retry
                        Case DialogResult.Ignore
                            Exit Do
                    End Select
                End If
            Loop
            DataLen = Data.Read(DataBuffer, 0, BlockSize)
        End While
        Data.Close()
        Marshal.FreeHGlobal(DataPtr)
        If Not succ Then Throw New Exception("SCSI Failure")
        Return {0, 0, 0}
    End Function
    Public Shared Function Write(TapeDrive As String, sourceFile As String) As Byte()
        Return Write(TapeDrive, sourceFile, 524288, False)
    End Function
    Public Shared Function Write(TapeDrive As String, sourceFile As String, ByVal BlockLen As Integer) As Byte()
        Return Write(TapeDrive, sourceFile, BlockLen, False)
    End Function
    Public Shared Function Write(TapeDrive As String, sourceFile As String, ByVal BlockLen As UInteger) As Byte()
        Return Write(TapeDrive, sourceFile, CInt(BlockLen), False)
    End Function
    Public Shared Function Write(TapeDrive As String, sourceFile As String, ByVal BlockLen As Integer, ByVal senseEnabled As Boolean) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = Write(handle, sourceFile, BlockLen, senseEnabled)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function Write(handle As IntPtr, sourceFile As String, ByVal BlockLen As Integer, ByVal senseEnabled As Boolean) As Byte()
        Dim sense(63) As Byte
        BlockLen = Math.Min(BlockLen, GlobalBlockLimit)
        Dim DataBuffer(BlockLen - 1) As Byte
        Dim DataPtr As IntPtr = Marshal.AllocHGlobal(BlockLen)
        Dim fs As New IO.FileStream(sourceFile, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
        Dim DataLen As Integer = fs.Read(DataBuffer, 0, BlockLen)
        Dim succ As Boolean
        While DataLen > 0
            Dim cdbData As Byte() = {&HA, 0, DataLen >> 16 And &HFF, DataLen >> 8 And &HFF, DataLen And &HFF, 0}
            Marshal.Copy(DataBuffer, 0, DataPtr, DataLen)
            Do
                succ = TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdbData, DataPtr, DataLen, 0, 60000, sense)
                If succ Then
                    Exit Do
                Else
                    Select Case MessageBox.Show(New Form With {.TopMost = True}, $"写入出错：SCSI指令执行失败", "警告", MessageBoxButtons.AbortRetryIgnore)
                        Case DialogResult.Abort
                            Exit While
                        Case DialogResult.Retry
                        Case DialogResult.Ignore
                            Exit Do
                    End Select
                End If
            Loop
            DataLen = fs.Read(DataBuffer, 0, BlockLen)
        End While
        fs.Close()
        Marshal.FreeHGlobal(DataPtr)
        If Not succ Then Throw New Exception("SCSI Failure")
        Return {0, 0, 0}
    End Function
    Public Shared Function Flush(TapeDrive As String) As Byte()
        Return WriteFileMark(TapeDrive, 0)
    End Function
    Public Shared Function Flush(handle As IntPtr) As Byte()
        Return WriteFileMark(handle, 0)
    End Function
    Public Shared Function WriteFileMark(TapeDrive As String) As Byte()
        Return WriteFileMark(TapeDrive, 1)
    End Function
    Public Shared Function WriteFileMark(TapeDrive As String, ByVal Number As UInteger) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = WriteFileMark(handle, Number)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function WriteFileMark(handle As IntPtr) As Byte()
        Return WriteFileMark(handle, 1)
    End Function
    Public Shared Function WriteFileMark(handle As IntPtr, ByVal Number As UInteger) As Byte()
        Dim sense(63) As Byte
        SendSCSICommandUnmanaged(handle, {&H10, Math.Min(Number, 1), Number >> 16 And &HFF, Number >> 8 And &HFF, Number And &HFF, 0}, {}, 1,
                        Function(senseData As Byte()) As Boolean
                            sense = senseData
                            Return True
                        End Function)
        Return sense
    End Function
    Public Shared Function GetMAMAttributeBytes(handle As IntPtr, PageCode_H As Byte, PageCode_L As Byte) As Byte()
        Return GetMAMAttributeBytes(handle, PageCode_H, PageCode_L, 0)
    End Function
    Public Shared Function GetMAMAttributeBytes(handle As IntPtr, PageCode_H As Byte, PageCode_L As Byte, ByVal PartitionNumber As Byte) As Byte()
        Dim DATA_LEN As Integer = 0
        Dim sense(63) As Byte
        Dim cdbData As Byte() = {&H8C, 0, 0, 0, 0, 0, 0, PartitionNumber,
            PageCode_H,
            PageCode_L,
            (DATA_LEN + 9) >> 24 And &HFF,
            (DATA_LEN + 9) >> 16 And &HFF,
            (DATA_LEN + 9) >> 8 And &HFF,
            (DATA_LEN + 9) And &HFF, 0, 0}
        Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(DATA_LEN + 9)
        Dim BCArray(DATA_LEN + 8) As Byte
        Marshal.Copy(BCArray, 0, dataBuffer, 9)
        Dim Result As Byte() = {}
        Dim succ As Boolean = False
        SyncLock SCSIOperationLock
            Try
                succ = TapeSCSIIOCtlUnmanaged(handle, cdbData, dataBuffer, DATA_LEN + 9, 1, 60000, sense)
            Catch ex As Exception
                Marshal.FreeHGlobal(dataBuffer)
                Throw New Exception("SCSIIOError")
            End Try
            Marshal.Copy(dataBuffer, BCArray, 0, DATA_LEN + 9)
            If succ Then
                DATA_LEN = CInt(BCArray(7)) << 8 Or BCArray(8)
                If DATA_LEN > 0 Then
                    Dim dataBuffer2 As IntPtr = Marshal.AllocHGlobal(DATA_LEN + 9)
                    Dim BCArray2(DATA_LEN + 8) As Byte
                    Marshal.Copy(BCArray2, 0, dataBuffer2, DATA_LEN + 9)
                    cdbData = {&H8C, 0, 0, 0, 0, 0, 0, PartitionNumber,
                        PageCode_H,
                        PageCode_L,
                        (DATA_LEN + 9) >> 24 And &HFF,
                        (DATA_LEN + 9) >> 16 And &HFF,
                        (DATA_LEN + 9) >> 8 And &HFF,
                        (DATA_LEN + 9) And &HFF, 0, 0}
                    succ = False
                    Try
                        succ = TapeSCSIIOCtlUnmanaged(handle, cdbData, dataBuffer2, DATA_LEN + 9, 1, 60000, sense)
                    Catch ex As Exception
                        Marshal.FreeHGlobal(dataBuffer)
                        Marshal.FreeHGlobal(dataBuffer2)
                        Throw New Exception("SCSIIOError")
                    End Try
                    If succ Then
                        Marshal.Copy(dataBuffer2, BCArray2, 0, DATA_LEN + 9)
                        Result = BCArray2.Skip(9).ToArray()
                    End If
                    Marshal.FreeHGlobal(dataBuffer2)
                End If
            End If
        End SyncLock

        Marshal.FreeHGlobal(dataBuffer)
        Return Result
    End Function
    Public Shared Function GetMAMAttributeBytes(TapeDrive As String, PageCode_H As Byte, PageCode_L As Byte) As Byte()
        Return GetMAMAttributeBytes(TapeDrive, PageCode_H, PageCode_L, 0)
    End Function
    Public Shared Function GetMAMAttributeBytes(TapeDrive As String, PageCode_H As Byte, PageCode_L As Byte, ByVal PartitionNumber As Byte) As Byte()
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Byte() = GetMAMAttributeBytes(handle, PageCode_H, PageCode_L, PartitionNumber)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock

    End Function
    Public Shared Function Byte2Hex(bytes As Byte(), Optional ByVal ReadablePrint As Boolean = False) As String
        If ReadablePrint Then Return ByteArrayToString(bytes)
        If bytes Is Nothing Then Return ""
        If bytes.Length = 0 Then Return ""
        Dim sb As New System.Text.StringBuilder
        For i As Integer = 0 To bytes.Length - 1
            sb.Append(Convert.ToString((bytes(i) And &HFF) + &H100, 16).Substring(1).ToUpper)
            sb.Append(" ")
            If i Mod 16 = 15 Then
                sb.Append(vbCrLf)
            End If
        Next
        Return sb.ToString()
    End Function
    Public Shared Function RichByte2Hex(bytes As Byte(), Optional ByVal TextShow As Boolean = False) As String
        Const HalfWidthChars As String = "~!@#$%^&*()_+-=|\ <>?,./:;""''{}[]0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
        If bytes Is Nothing Then Return ""
        If bytes.Length = 0 Then Return ""
        Dim sb As New StringBuilder
        Dim tb As String = ""
        Dim ln As New StringBuilder
        For i As Integer = 0 To bytes.Length - 1
            If i Mod 16 = 0 And TextShow Then
                ln.Append("|" & Hex(i).PadLeft(5) & "h: ")
            End If
            ln.Append(Convert.ToString((bytes(i) And &HFF) + &H100, 16).Substring(1).ToUpper)
            ln.Append(" ")
            Dim c As Char = Chr(bytes(i))
            If Not HalfWidthChars.Contains(c) Then
                tb &= "."
            Else
                tb &= c
            End If
            If i Mod 16 = 15 Then
                If TextShow Then
                    ln.Append(tb)
                End If
                sb.Append(ln.ToString().PadRight(74) & "|")
                sb.Append(vbCrLf)
                ln = New StringBuilder()
                tb = ""
            End If
        Next
        If TextShow And tb <> "" Then
            ln.Append(tb)
        End If
        If ln.Length > 0 Then sb.Append(ln.ToString().PadRight(74) & "|")
        Return sb.ToString()
    End Function
    Public Shared Function ByteArrayToString(bytesArray As Byte(), Optional ByVal TextOnly As Boolean = False) As String
        Dim strBuilder As New StringBuilder()
        Dim rowSize As Integer = 16
        Dim numRows As Integer = Math.Ceiling(bytesArray.Length / rowSize)

        For row As Integer = 0 To numRows - 1
            Dim rowStart As Integer = row * rowSize
            Dim rowEnd As Integer = Math.Min((row + 1) * rowSize, bytesArray.Length)
            Dim rowBytes As Byte() = bytesArray.Skip(rowStart).Take(rowEnd - rowStart).ToArray()

            If Not TextOnly Then
                ' Append the hex values for this row
                strBuilder.Append($"{rowStart:X8}h: ")
                For Each b As Byte In rowBytes
                    strBuilder.Append($"{b:X2} ")
                Next
                strBuilder.Append(" "c, (rowSize - rowBytes.Length) * 3)
                strBuilder.Append("  ")
            End If
            ' Append the ASCII characters for this row
            For Each b As Byte In rowBytes
                strBuilder.Append(If(b >= 32 AndAlso b <= 126, ChrW(b), "."c))
            Next
            If Not TextOnly Then
                ' Append a newline character for the next row
                strBuilder.AppendLine()
            End If
        Next

        Return strBuilder.ToString()
    End Function

    <Serializable>
    Public Class MAMAttributeList
        Public Property Content As New List(Of MAMAttribute)
        Public Function GetSerializedText() As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(MAMAttributeList))
            Dim tmpf As String = Application.StartupPath & "\" & Now.ToString("MAM_yyyyMMdd_HHmmss.fffffff.tmp")
            While IO.File.Exists(tmpf)
                tmpf = Application.StartupPath & "\" & Now.ToString("MAM_yyyyMMdd_HHmmss.fffffff.tmp")
            End While
            Dim ms As New IO.FileStream(tmpf, IO.FileMode.Create)
            Dim t As IO.TextWriter = New IO.StreamWriter(ms, New System.Text.UTF8Encoding(False))
            writer.Serialize(t, Me)
            ms.Close()
            Dim soutp As New IO.StreamReader(tmpf)
            Dim sout As New System.Text.StringBuilder
            While Not soutp.EndOfStream
                sout.AppendLine(soutp.ReadLine)
            End While
            soutp.Close()
            IO.File.Delete(tmpf)
            Return sout.ToString()
        End Function
        Public Sub SaveSerializedText(ByVal FileName As String)
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(MAMAttributeList))
            Dim ms As New IO.FileStream(FileName, IO.FileMode.Create)
            Dim t As IO.TextWriter = New IO.StreamWriter(ms, New System.Text.UTF8Encoding(False))
            writer.Serialize(t, Me)
            ms.Close()
        End Sub
        Public Shared Function FromXML(s As String) As MAMAttributeList
            Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(MAMAttributeList))
            Dim t As IO.TextReader = New IO.StringReader(s)
            Return CType(reader.Deserialize(t), MAMAttributeList)
        End Function
    End Class
    <Serializable>
    Public Class MAMAttribute
        Public Property ID As UInt16
        Public Property ID_HexValue As String
            Get
                Return Byte2Hex({ID_MSB, ID_LSB})
            End Get
            Set(value As String)

            End Set
        End Property
        <Xml.Serialization.XmlIgnore>
        Public ReadOnly Property ID_MSB As Byte
            Get
                Return (ID >> 8) And &HFF
            End Get
        End Property
        <Xml.Serialization.XmlIgnore>
        Public ReadOnly Property ID_LSB As Byte
            Get
                Return ID And &HFF
            End Get
        End Property
        Public Property RawData As Byte() = {}
        Public Property Length As Integer
            Get
                Return RawData.Length
            End Get
            Set(value As Integer)

            End Set
        End Property
        Public Property AsString As String
            Get
                Try
                    Return System.Text.Encoding.UTF8.GetString(RawData)
                Catch ex As Exception
                    Return ""
                End Try
            End Get
            Set(value As String)

            End Set
        End Property
        Public Property AsNumeric As Int64
            Get
                If RawData.Length <> 1 And RawData.Length <> 2 And RawData.Length <> 4 And RawData.Length <> 8 Then Return 0
                Dim result As Int64 = 0
                For i As Integer = 0 To Math.Min(7, RawData.Length - 1)
                    result <<= 8
                    result = result Or RawData(i)
                Next
                Return result
            End Get
            Set(value As Int64)

            End Set
        End Property
        Public Property AsHexText As String
            Get
                Return Byte2Hex(RawData)
            End Get
            Set(value As String)

            End Set
        End Property
        Public Shared Function FromTapeDrive(driveHandle As IntPtr, PageCode_H As Byte, PageCode_L As Byte, Optional ByVal PartitionNumber As Byte = 0) As MAMAttribute
            Dim RawData As Byte() = GetMAMAttributeBytes(driveHandle, PageCode_H, PageCode_L, PartitionNumber)
            If RawData.Length = 0 Then Return Nothing
            Return New MAMAttribute With {.ID = (CUShort(PageCode_H) << 8) Or PageCode_L, .RawData = RawData}
        End Function
        Public Shared Function FromTapeDrive(TapeDrive As String, PageCode_H As Byte, PageCode_L As Byte, Optional ByVal PartitionNumber As Byte = 0) As MAMAttribute
            Dim RawData As Byte() = GetMAMAttributeBytes(TapeDrive, PageCode_H, PageCode_L, PartitionNumber)
            If RawData.Length = 0 Then Return Nothing
            Return New MAMAttribute With {.ID = (CUShort(PageCode_H) << 8) Or PageCode_L, .RawData = RawData}
        End Function
        Public Shared Function FromTapeDrive(TapeDrive As String, PageCode As UInt16, Optional ByVal PartitionNumber As Byte = 0) As MAMAttribute
            Return FromTapeDrive(TapeDrive:=TapeDrive, PageCode_H:=(PageCode >> 8) And &HFF, PageCode_L:=PageCode And &HFF, PartitionNumber:=PartitionNumber)
        End Function

        Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(MAMAttribute))
            Dim sb As New System.Text.StringBuilder()
            Dim t As IO.TextWriter = New IO.StringWriter(sb)
            writer.Serialize(t, Me)
            t.Close()
            Return sb.ToString
        End Function
    End Class
    Public Shared Function SendSCSICommand(handle As IntPtr, cdbData As Byte(), Optional ByRef Data As Byte() = Nothing, Optional DataIn As Byte = 2, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing, Optional ByVal TimeOut As Integer = 60000) As Boolean
        Dim dataBufferPtr As IntPtr
        Dim dataLen As Integer = 0
        If Data IsNot Nothing Then
            dataLen = Data.Length
            dataBufferPtr = Marshal.AllocHGlobal(Data.Length)
            Marshal.Copy(Data, 0, dataBufferPtr, Data.Length)
        Else
            dataBufferPtr = Marshal.AllocHGlobal(128)
        End If
        Dim senseBuffer(63) As Byte
        Dim succ As Boolean = TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdbData, dataBufferPtr, dataLen, DataIn, TimeOut, senseBuffer)
        If succ AndAlso Data IsNot Nothing Then Marshal.Copy(dataBufferPtr, Data, 0, Data.Length)
        If senseReport IsNot Nothing Then
            senseReport(senseBuffer)
        End If
        Marshal.FreeHGlobal(dataBufferPtr)
        Return succ
    End Function
    Public Shared Function SendSCSICommand(TapeDrive As String, cdbData As Byte(), Optional ByRef Data As Byte() = Nothing, Optional DataIn As Byte = 2, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing, Optional ByVal TimeOut As Integer = 60000) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = SendSCSICommand(handle, cdbData, Data, DataIn, senseReport, TimeOut)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function SendSCSICommandUnmanaged(handle As IntPtr, cdbData As Byte(), Optional ByRef Data As Byte() = Nothing, Optional DataIn As Byte = 2, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing, Optional ByVal TimeOut As Integer = 60000) As Boolean
        Dim dataBufferPtr As IntPtr
        Dim dataLen As Integer = 0
        If Data IsNot Nothing Then
            dataLen = Data.Length
            dataBufferPtr = Marshal.AllocHGlobal(Data.Length)
            Marshal.Copy(Data, 0, dataBufferPtr, Data.Length)
        Else
            dataBufferPtr = Marshal.AllocHGlobal(128)
        End If

        Dim senseBuffer(63) As Byte
        Dim succ As Boolean = TapeUtils.TapeSCSIIOCtlUnmanaged(handle, cdbData, dataBufferPtr, dataLen, DataIn, TimeOut, senseBuffer)
        If succ AndAlso Data IsNot Nothing Then Marshal.Copy(dataBufferPtr, Data, 0, Data.Length)
        If senseReport IsNot Nothing Then
            senseReport(senseBuffer)
        End If
        Marshal.FreeHGlobal(dataBufferPtr)
        Return succ
    End Function

    Public Const DEFAULT_LOG_DIR As String = "C:\ProgramData\HPE\LTFS"
    Public Const DEFAULT_WORK_DIR As String = "C:\tmp\LTFS"
    Public Shared Function GetTapeDriveList() As List(Of BlockDevice)
        Dim LDrive As New List(Of BlockDevice)
        Dim obj As List(Of SetupAPIHelper.Device) = SetupAPIHelper.Device.EnumerateDevices("SCSI").ToList()
        Dim tapeobj As New List(Of SetupAPIHelper.Device)
        For Each dev As SetupAPIHelper.Device In obj
            If dev.Present Then
                If dev.ClassName.ToLower = "tapedrive" OrElse dev.ClassName.ToLower = "unknown" Then
                    tapeobj.Add(dev)
                End If
            End If
        Next
        For Each dev As SetupAPIHelper.Device In tapeobj
            Dim handle As IntPtr = CreateFile($"\\.\Globalroot{dev.PDOName}", 3221225472UL, 7UL, IntPtr.Zero, 3, 0, IntPtr.Zero)
            Dim result As Boolean = False
            Dim devNum As New SetupAPIWheels.STORAGE_DEVICE_NUMBER

            Dim devNumPtr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(devNum))
            Dim lpBytesReturned As Int32
            Marshal.StructureToPtr(devNum, devNumPtr, True)
            result = DeviceIoControl(handle, SCSIIOCtl.IOCTL_STORAGE_GET_DEVICE_NUMBER, IntPtr.Zero, 0, devNumPtr, Marshal.SizeOf(GetType(SetupAPIWheels.STORAGE_DEVICE_NUMBER)), lpBytesReturned, IntPtr.Zero)
            If result Then Marshal.PtrToStructure(devNumPtr, devNum)
            Marshal.FreeHGlobal(devNumPtr)
            CloseHandle(handle)
            Dim drv As BlockDevice = TapeUtils.Inquiry($"\\.\Globalroot{dev.PDOName}")
            drv.DevicePath = $"\\.\Globalroot{dev.PDOName}"
            If result Then
                drv.DevIndex = devNum.DeviceNumber
                drv.DevicePath = $"\\.\TAPE{drv.DevIndex}"
            End If
            LDrive.Add(drv)
        Next
        Dim s() As String

        LDrive.Sort(New Comparison(Of BlockDevice)(
                        Function(A As BlockDevice, B As BlockDevice) As Integer
                            Return A.DevIndex.CompareTo(B.DevIndex)
                        End Function))
        s = GetDriveMappings().Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        For Each t As String In s
            Dim q() As String = t.Split({"|"}, StringSplitOptions.None)
            If q.Length = 3 Then
                For Each Drv As BlockDevice In LDrive
                    If "TAPE" & Drv.DevIndex = q(1) And Drv.SerialNumber = q(2) Then
                        Drv.DriveLetter = q(0)
                    End If
                Next
            End If
        Next


        Dim devpath As String = IO.Path.Combine(Application.StartupPath, "device")
        If IO.Directory.Exists(devpath) Then
            For Each f As IO.FileInfo In (New IO.DirectoryInfo(devpath).GetFiles)
                If f.Extension.ToLower() = ".xml" Then
                    Dim fcnt As String = IO.File.ReadAllText(f.FullName)
                    Try
                        Dim blkdev As BlockDevice = BlockDevice.FromXML(fcnt)
                        LDrive.Add(blkdev)
                    Catch ex As Exception

                    End Try
                End If
            Next
        End If
        Return LDrive
    End Function
    Public Shared Function GetDiskDriveList() As List(Of BlockDevice)
        Dim LDrive As New List(Of BlockDevice)
        Dim obj As List(Of SetupAPIHelper.Device) = SetupAPIHelper.Device.EnumerateDevices("SCSI").ToList()
        Dim diskobj As New List(Of SetupAPIHelper.Device)
        For Each dev As SetupAPIHelper.Device In obj
            If dev.Present Then
                If dev.ClassName.ToLower = "diskdrive" Then
                    diskobj.Add(dev)
                End If
            End If
        Next
        obj = SetupAPIHelper.Device.EnumerateDevices("MPIO").ToList()
        For Each dev As SetupAPIHelper.Device In obj
            If dev.Present Then
                If dev.ClassName.ToLower = "diskdrive" Then
                    diskobj.Add(dev)
                End If
            End If
        Next
        For Each dev As SetupAPIHelper.Device In diskobj

            'With New SettingPanel With {.SelectedObject = dev}
            '    .ShowDialog()
            'End With
            Dim handle As IntPtr = CreateFile($"\\.\Globalroot{dev.PDOName}", 3221225472UL, 7UL, IntPtr.Zero, 3, 0, IntPtr.Zero)
            Dim result As Boolean = False
            Dim devNum As New SetupAPIWheels.STORAGE_DEVICE_NUMBER

            Dim devNumPtr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(devNum))
            Dim lpBytesReturned As Int32
            Marshal.StructureToPtr(devNum, devNumPtr, True)
            result = DeviceIoControl(handle, SCSIIOCtl.IOCTL_STORAGE_GET_DEVICE_NUMBER, IntPtr.Zero, 0, devNumPtr, Marshal.SizeOf(GetType(SetupAPIWheels.STORAGE_DEVICE_NUMBER)), lpBytesReturned, IntPtr.Zero)
            If result Then Marshal.PtrToStructure(devNumPtr, devNum)
            Marshal.FreeHGlobal(devNumPtr)
            CloseHandle(handle)
            Dim drv As BlockDevice
            If Not result Then
                drv = TapeUtils.Inquiry($"\\.\Globalroot{dev.PDOName}")
            Else
                drv = TapeUtils.Inquiry($"\\.\PhysicalDrive{devNum.DeviceNumber}")
            End If
            If drv Is Nothing Then Continue For
            drv.DeviceType = "PhysicalDrive"
            If result Then
                drv.DevIndex = devNum.DeviceNumber
                drv.DevicePath = $"\\.\PhysicalDrive{drv.DevIndex}"
            Else
                drv.DevicePath = $"\\.\Globalroot{dev.PDOName}"
            End If
            LDrive.Add(drv)
        Next

        Dim s() As String
        'Dim p As IntPtr = _GetDiskDriveList()
        ''MessageBox.Show(New Form With {.TopMost = True}, Marshal.PtrToStringAnsi(p))
        's = Marshal.PtrToStringAnsi(p).Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        'For Each t As String In s
        '    Dim q() As String = t.Split({"|"}, StringSplitOptions.None)
        '    If q.Length = 4 Then
        '        LDrive.Add(New BlockDevice(q(0), q(1), q(2), q(3)) With {.DeviceType = "PhysicalDrive"})
        '    End If
        'Next
        LDrive.Sort(New Comparison(Of BlockDevice)(
                        Function(A As BlockDevice, B As BlockDevice) As Integer
                            Return A.DevIndex.CompareTo(B.DevIndex)
                        End Function))
        s = GetDriveMappings().Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        For Each t As String In s
            Dim q() As String = t.Split({"|"}, StringSplitOptions.None)
            If q.Length = 3 Then
                For Each Drv As BlockDevice In LDrive
                    If "PhysicalDrive" & Drv.DevIndex = q(1) And Drv.SerialNumber = q(2) Then
                        Drv.DriveLetter = q(0)
                    End If
                Next
            End If
        Next
        Return LDrive
    End Function
    Public Shared Function GetMediumChangerList() As List(Of MediumChanger)
        Dim LChanger As New List(Of MediumChanger)
        Dim obj As List(Of SetupAPIHelper.Device) = SetupAPIHelper.Device.EnumerateDevices("SCSI").ToList()
        Dim tapeobj As New List(Of SetupAPIHelper.Device)
        For Each dev As SetupAPIHelper.Device In obj
            If dev.Present Then
                If dev.ClassName.ToLower = "mediumchanger" Then
                    tapeobj.Add(dev)
                End If
            End If
        Next
        For Each dev As SetupAPIHelper.Device In tapeobj
            Dim handle As IntPtr = CreateFile($"\\.\Globalroot{dev.PDOName}", 3221225472UL, 7UL, IntPtr.Zero, 3, 0, IntPtr.Zero)
            Dim result As Boolean = False
            Dim devNum As New SetupAPIWheels.STORAGE_DEVICE_NUMBER

            Dim devNumPtr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(devNum))
            Dim lpBytesReturned As Int32
            Marshal.StructureToPtr(devNum, devNumPtr, True)
            result = DeviceIoControl(handle, SCSIIOCtl.IOCTL_STORAGE_GET_DEVICE_NUMBER, IntPtr.Zero, 0, devNumPtr, Marshal.SizeOf(GetType(SetupAPIWheels.STORAGE_DEVICE_NUMBER)), lpBytesReturned, IntPtr.Zero)
            If result Then Marshal.PtrToStructure(devNumPtr, devNum)
            Marshal.FreeHGlobal(devNumPtr)
            CloseHandle(handle)
            Dim drv As BlockDevice = TapeUtils.Inquiry($"\\.\Globalroot{dev.PDOName}")
            If drv Is Nothing Then Continue For
            drv.DevicePath = $"\\.\Globalroot{dev.PDOName}"
            If result Then
                drv.DevIndex = devNum.DeviceNumber
                drv.DevicePath = $"\\.\CHANGER{drv.DevIndex}"
            End If

            LChanger.Add(New MediumChanger(drv.DevIndex, drv.SerialNumber, drv.VendorId, drv.ProductId))
        Next

        'Dim p As IntPtr = _GetMediumChangerList()
        '
        'Dim chgresult As String = Marshal.PtrToStringAnsi(p)
        'Dim s() As String = chgresult.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        '
        'For Each t As String In s
        '    Dim q() As String = t.Split({"|"}, StringSplitOptions.None)
        '    If q.Length = 4 Then
        '        LChanger.Add(New MediumChanger(q(0), q(1), q(2), q(3)))
        '    End If
        'Next
        LChanger.Sort(New Comparison(Of MediumChanger)(
                        Function(A As MediumChanger, B As MediumChanger) As Integer
                            Return A.DevIndex.CompareTo(B.DevIndex)
                        End Function))
        Return LChanger
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
    Public Shared Function MapTapeDrive(driveLetter As Char, TapeDrive As String, Optional ByVal logDir As String = DEFAULT_LOG_DIR, Optional ByVal workDir As String = DEFAULT_WORK_DIR, Optional ByVal showOffline As Boolean = False) As String
        Dim tapeIndex As Byte = Byte.Parse(TapeDrive.Substring(4))
        Dim p As IntPtr = _MapTapeDrive(driveLetter, TapeDrive, tapeIndex, logDir, workDir, showOffline)
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
    Public Shared Function SetEncryption(handle As IntPtr, Optional ByVal EncryptionKey As Byte() = Nothing, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Dim result As Boolean = False
        Dim param As New List(Of Byte)
        If EncryptionKey IsNot Nothing AndAlso EncryptionKey.Length = 32 Then
            param.AddRange({&H0, &H10, &H0, &H30, &H40, &H34, &H2, &H3, &H1, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H20})
            param.AddRange(EncryptionKey.ToList())
            SendSCSICommand(handle, {&HB5, &H20, &H0, &H10, &H0, &H0, &H0, &H0, &H0, &H34, &H0, &H0},
                            param.ToArray(), 0, SenseReport, 10)
        Else
            Dim emptyValue(32) As Byte
            param.AddRange({&H0, &H10, &H0, &H30, &H40, 0, 0, 0, &H1, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H0, &H20})
            param.AddRange(emptyValue.ToList())
            SendSCSICommand(handle, {&HB5, &H20, &H0, &H10, &H0, &H0, &H0, &H0, &H0, &H34, &H0, &H0},
                            param.ToArray(), 0, SenseReport, 10)
        End If
        Return result
    End Function
    Public Shared Function SetEncryption(TapeDrive As String, Optional ByVal EncryptionKey As Byte() = Nothing, Optional ByVal SenseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = SetEncryption(handle, EncryptionKey, SenseReport)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Enum LoadOption As Byte
        LoadThreaded = 1
        LoadUnthreaded = 9
        Unthread = &HA
        Eject = 0
    End Enum
    Public Shared Function LoadEject(handle As IntPtr, LoadOption As LoadOption, Optional ByVal EncryptionKey As Byte() = Nothing) As Boolean
        Dim result As Boolean = False
        Dim retrycount As Integer = 1
        While True
            Dim sensereceived As Boolean = False
            Dim sensedata As Byte()
            SendSCSICommand(handle:=handle, cdbData:={&H1B, 0, 0, 0, LoadOption, 0}, DataIn:=1,
                                                           senseReport:=Function(sense As Byte()) As Boolean
                                                                            sensedata = sense
                                                                            If sense.Length > 0 Then
                                                                                If sense(0) <> 0 Then
                                                                                    result = False
                                                                                Else
                                                                                    result = True
                                                                                End If
                                                                            Else
                                                                                result = True
                                                                            End If
                                                                            sensereceived = True

                                                                            Return True
                                                                        End Function)
            While Not sensereceived
                Threading.Thread.Sleep(100)
            End While
            If result Then Exit While
            If retrycount > 0 Then
                retrycount -= 1
                Continue While
            End If
            Select Case MessageBox.Show(New Form With {.TopMost = True}, $"{My.Resources.ResText_WErrSCSI }{vbCrLf}{ParseSenseData(sensedata)}", My.Resources.ResText_Warning, MessageBoxButtons.AbortRetryIgnore)
                Case DialogResult.Abort
                    Return False
                Case DialogResult.Retry

                Case DialogResult.Ignore
                    Exit While
            End Select
        End While


        If result AndAlso LoadOption = LoadOption.LoadThreaded AndAlso EncryptionKey IsNot Nothing Then
            SetEncryption(handle:=handle, EncryptionKey:=EncryptionKey)
        End If
        Return result
    End Function
    Public Shared Function LoadEject(TapeDrive As String, LoadOption As LoadOption, Optional ByVal EncryptionKey As Byte() = Nothing) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = LoadEject(handle, LoadOption, EncryptionKey)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
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
    <Category("Options")>
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class MKLTFS_Param
        Private _Barcode As String = ""
        <Category("Common")>
        <LocalizedDescription("PropertyDescription_mkltfs_Barcode")>
        Public Property Barcode As String
            Get
                Return _Barcode
            End Get
            Set(value As String)
                Dim asciiString As New StringBuilder()
                For Each ch As Char In value
                    If Asc(ch) >= 0 AndAlso Asc(ch) <= 127 Then
                        asciiString.Append(ch)
                    End If
                Next
                value = asciiString.ToString()
                If value.Length > 20 Then value = value.Substring(0, 20)
                _Barcode = value
            End Set
        End Property
        <Category("Common")>
        <LocalizedDescription("PropertyDescription_mkltfs_VolumeLabel")>
        Public Property VolumeLabel As String = ""
        <Category("Expert")>
        <LocalizedDescription("PropertyDescription_mkltfs_MaxExtraPartitionAllowed")>
        Public ReadOnly Property MaxExtraPartitionAllowed As Byte
        Private Property _ExtraPartitionCount As Byte = 1
        <Category("Expert")>
        <LocalizedDescription("PropertyDescription_mkltfs_ExtraPartitionCount")>
        Public Property ExtraPartitionCount As Byte
            Get
                Return _ExtraPartitionCount
            End Get
            Set(value As Byte)
                _ExtraPartitionCount = Math.Min(1, value)
                _ExtraPartitionCount = Math.Min(_ExtraPartitionCount, MaxExtraPartitionAllowed)
            End Set
        End Property
        Private _BlockLen As Long = 524288
        <Category("Expert")>
        <LocalizedDescription("PropertyDescription_mkltfs_BlockLen")>
        Public Property BlockLen As Long
            Get
                Return _BlockLen
            End Get
            Set(value As Long)
                _BlockLen = Math.Max(1, Math.Min(value, 2097152))
            End Set
        End Property
        <Category("Expert")>
        <LocalizedDescription("PropertyDescription_mkltfs_ImmediateMode")>
        Public Property ImmediateMode As Boolean = True
        <Category("Expert")>
        <LocalizedDescription("PropertyDescription_mkltfs_Capacity")>
        Public Property Capacity As UInt16 = &HFFFF
        Private _P0Size As UInt16 = 1
        <Category("Expert")>
        <LocalizedDescription("PropertyDescription_mkltfs_P0Size")>
        Public Property P0Size As UInt16
            Get
                Return _P0Size
            End Get
            Set(value As UInt16)
                _P0Size = value
                If value < &HFFFF Then
                    _P1Size = &HFFFF
                Else
                    _P1Size = 1
                End If
            End Set
        End Property
        Private Property _P1Size As UInt16 = &HFFFF
        <Category("Expert")>
        <LocalizedDescription("PropertyDescription_mkltfs_P1Size")>
        Public Property P1Size As UInt16
            Get
                Return _P1Size
            End Get
            Set(value As UInt16)
                _P1Size = value
                If value < &HFFFF Then
                    _P0Size = &HFFFF
                Else
                    _P0Size = 1
                End If
            End Set
        End Property
        <Category("Expert")>
        <LocalizedDescription("PropertyDescription_mkltfs_EncryptionKey")>
        Public Property EncryptionKey As Byte() = Nothing
        Public Sub New(MaxExtraPartitionAllowed As Byte)
            Me.MaxExtraPartitionAllowed = MaxExtraPartitionAllowed
            Me._ExtraPartitionCount = Math.Min(Me._ExtraPartitionCount, Me.MaxExtraPartitionAllowed)
        End Sub
    End Class
    Public Shared Function mkltfs(TapeDrive As String,
                                  Optional ByVal Barcode As String = "",
                                  Optional ByVal VolumeName As String = "",
                                  Optional ByVal ExtraPartitionCount As Byte = 1,
                                  Optional ByVal BlockLen As Long = 524288,
                                  Optional ByVal ImmediateMode As Boolean = True,
                                  Optional ByVal ProgressReport As Action(Of String) = Nothing,
                                  Optional ByVal OnFinish As Action(Of String) = Nothing,
                                  Optional ByVal OnError As Action(Of String) = Nothing,
                                  Optional ByVal Capacity As UInt16 = &HFFFF,
                                  Optional ByVal P0Size As UInt16 = 1,
                                  Optional ByVal P1Size As UInt16 = &HFFFF,
                                  Optional ByVal EncryptionKey As Byte() = Nothing) As Boolean
        Dim handle As IntPtr
        Dim result As Boolean
        If ImmediateMode Then
            SyncLock SCSIOperationLock
                If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
                result = mkltfs(handle, Barcode, VolumeName, ExtraPartitionCount, BlockLen, True, ProgressReport, OnFinish, OnError, Capacity, P0Size, P1Size, EncryptionKey)
                If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
                Return result
            End SyncLock
        Else
            Dim th As New Threading.Thread(
                Sub()
                    SyncLock SCSIOperationLock
                        If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
                        mkltfs(handle, Barcode, VolumeName, ExtraPartitionCount, BlockLen, True, ProgressReport, OnFinish, OnError, Capacity, P0Size, P1Size, EncryptionKey)
                        If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
                    End SyncLock
                End Sub)
            th.Start()
            Return True
        End If

    End Function
    Public Shared Function mkltfs(handle As IntPtr,
                                  Optional ByVal Barcode As String = "",
                                  Optional ByVal VolumeName As String = "",
                                  Optional ByVal ExtraPartitionCount As Byte = 1,
                                  Optional ByVal BlockLen As Long = 524288,
                                  Optional ByVal ImmediateMode As Boolean = True,
                                  Optional ByVal ProgressReport As Action(Of String) = Nothing,
                                  Optional ByVal OnFinish As Action(Of String) = Nothing,
                                  Optional ByVal OnError As Action(Of String) = Nothing,
                                  Optional ByVal Capacity As UInt16 = &HFFFF,
                                  Optional ByVal P0Size As UInt16 = 1,
                                  Optional ByVal P1Size As UInt16 = &HFFFF,
                                  Optional ByVal EncryptionKey As Byte() = Nothing) As Boolean
        GlobalBlockLimit = TapeUtils.ReadBlockLimits(handle).MaximumBlockLength
        If IO.File.Exists(IO.Path.Combine(Application.StartupPath, "blocklen.ini")) Then
            Dim blval As Integer = Integer.Parse(IO.File.ReadAllText(IO.Path.Combine(Application.StartupPath, "blocklen.ini")))
            If blval > 0 Then TapeUtils.GlobalBlockLimit = blval
        End If
        BlockLen = Math.Min(BlockLen, GlobalBlockLimit)
        Dim mkltfs_op As Func(Of Boolean) =
            Function()
                Try
                    Dim senseReportFunc As Func(Of Byte(), Boolean) = Function(sense As Byte()) As Boolean
                                                                          If sense(2) And &HF = 0 Then Return True
                                                                          ProgressReport(ParseSenseData(sense))
                                                                          Return False
                                                                      End Function

                    'Load and Thread
                    ProgressReport("Loading..")
                    If TapeUtils.SendSCSICommand(handle, {&H1B, 0, 0, 0, 1, 0}, senseReport:=senseReportFunc) Then
                        ProgressReport("Load OK" & vbCrLf)
                    Else
                        OnError("Load Fail" & vbCrLf)
                        Return False
                    End If
                    ProgressReport("Mode Sense..")
                    Dim ModeData As Byte() = TapeUtils.ModeSense(handle, &H11, senseReport:=senseReportFunc)
                    ProgressReport(Byte2Hex(ModeData))
                    ReDim Preserve ModeData(11)
                    Dim MaxExtraPartitionAllowed As Byte = ModeData(2)
                    ExtraPartitionCount = Math.Min(MaxExtraPartitionAllowed, ExtraPartitionCount)
                    ProgressReport($"Extra partitions: {ExtraPartitionCount}")
                    If Not AllowPartition Then ExtraPartitionCount = 0
                    If ExtraPartitionCount > 1 Then ExtraPartitionCount = 1

                    'Set Capacity
                    ProgressReport("Set Capacity..")
                    If TapeUtils.SendSCSICommand(handle:=handle, cdbData:={&HB, 0, 0, (Capacity >> 8) And &HFF, Capacity And &HFF, 0}, senseReport:=senseReportFunc) Then
                        ProgressReport("Set Capacity OK" & vbCrLf)
                    Else
                        OnError("Set Capacity Fail" & vbCrLf)
                        Return False
                    End If

                    'Format
                    ProgressReport("Initializing tape..")
                    Dim DisableFormat As Boolean = False
                    Try
                        Dim cmdata As New CMParser(handle)
                        DisableFormat = cmdata.CartridgeMfgData.IsLTO9Plus OrElse (Not cmdata.CartridgeMfgData.IsLTO3Plus)
                    Catch ex As Exception
                        ProgressReport("CMData parse failed")
                    End Try
                    If DisableFormat Then
                        ProgressReport("LTO9 detected, skip initialization" & vbCrLf)
                    Else
                        If TapeUtils.SendSCSICommand(handle, {4, 0, 0, 0, 0, 0}, senseReport:=senseReportFunc) Then
                            ProgressReport("Initialization OK" & vbCrLf)
                        Else
                            OnError("Initialization Fail" & vbCrLf)
                            Return False
                        End If
                    End If

                    If ExtraPartitionCount > 0 Then
                        'Mode Select:1st Partition to Minimum 
                        ProgressReport("MODE SELECT - Partition mode page..")
                        If TapeUtils.SendSCSICommand(handle:=handle, cdbData:={&H15, &H10, 0, 0, &H10, 0}, Data:={0, 0, &H10, 0, &H11, &HA, MaxExtraPartitionAllowed, 1, ModeData(4), ModeData(5), ModeData(6), ModeData(7), (P0Size >> 8) And &HFF, P0Size And &HFF, (P1Size >> 8) And &HFF, P1Size And &HFF}, DataIn:=0, senseReport:=senseReportFunc) Then
                            ProgressReport("MODE SELECT 11h OK" & vbCrLf)
                        Else
                            OnError("MODE SELECT 11h Fail" & vbCrLf)
                            Return False
                        End If
                        'Format
                        ProgressReport("Partitioning..")
                        Select Case DriverTypeSetting
                            Case DriverType.T10K
                                If TapeUtils.SendSCSICommand(handle, {4, 0, 2, 0, 0, 0}, Nothing, 0, senseReport:=senseReportFunc) Then
                                    ProgressReport("     OK" & vbCrLf)
                                Else
                                    OnError("     Fail" & vbCrLf)
                                    Return False
                                End If
                            Case Else
                                If TapeUtils.SendSCSICommand(handle, {4, 0, 1, 0, 0, 0}, Nothing, 0, senseReport:=senseReportFunc) Then
                                    ProgressReport("     OK" & vbCrLf)
                                Else
                                    OnError("     Fail" & vbCrLf)
                                    Return False
                                End If
                        End Select

                    End If
                    'Set Vendor
                    ProgressReport($"WRITE ATTRIBUTE: Vendor=OPEN..")
                    If TapeUtils.SetMAMAttribute(handle, &H800, "OPEN".PadRight(8), SenseReport:=senseReportFunc) Then
                        ProgressReport("WRITE ATTRIBUTE: 0800 OK" & vbCrLf)
                    Else
                        OnError("WRITE ATTRIBUTE: 0800 Fail" & vbCrLf)
                        Return False
                    End If
                    'Set AppName
                    ProgressReport($"WRITE ATTRIBUTE: Application Name = LTFSCopyGUI..")
                    If TapeUtils.SetMAMAttribute(handle, &H801, "LTFSCopyGUI".PadRight(32), SenseReport:=senseReportFunc) Then
                        ProgressReport("WRITE ATTRIBUTE: 0801 OK" & vbCrLf)
                    Else
                        OnError("WRITE ATTRIBUTE: 0801 Fail" & vbCrLf)
                        Return False
                    End If
                    'Set Version
                    ProgressReport($"WRITE ATTRIBUTE: Application Version={My.Application.Info.Version.ToString(3)}..")
                    If TapeUtils.SetMAMAttribute(handle, &H802, My.Application.Info.Version.ToString(3).PadRight(8), SenseReport:=senseReportFunc) Then
                        ProgressReport("WRITE ATTRIBUTE: 0802 OK" & vbCrLf)
                    Else
                        OnError("WRITE ATTRIBUTE: 0802 Fail" & vbCrLf)
                        Return False
                    End If
                    'Set TextLabel
                    ProgressReport($"WRITE ATTRIBUTE: TextLabel= ..")
                    If TapeUtils.SetMAMAttribute(handle, &H803, "".PadRight(160), TapeUtils.AttributeFormat.Text, SenseReport:=senseReportFunc) Then
                        ProgressReport("WRITE ATTRIBUTE: 0803 OK" & vbCrLf)
                    Else
                        OnError("WRITE ATTRIBUTE: 0803 Fail" & vbCrLf)
                        Return False
                    End If
                    'Set TLI
                    ProgressReport($"WRITE ATTRIBUTE: Localization Identifier = 0..")
                    If TapeUtils.SetMAMAttribute(handle, &H805, {0}, TapeUtils.AttributeFormat.Binary, SenseReport:=senseReportFunc) Then
                        ProgressReport("WRITE ATTRIBUTE:0805 OK" & vbCrLf)
                    Else
                        OnError("WRITE ATTRIBUTE:0805 Fail" & vbCrLf)
                        Return False
                    End If
                    'Set Barcode
                    Barcode = Barcode.PadRight(32).Substring(0, 32)
                    ProgressReport($"WRITE ATTRIBUTE: Barcode={Barcode}..")
                    If TapeUtils.SetBarcode(handle, Barcode, senseReport:=senseReportFunc) Then
                        ProgressReport("WRITE ATTRIBUTE: 0806 OK" & vbCrLf)
                    Else
                        OnError("WRITE ATTRIBUTE: 0806 Fail" & vbCrLf)
                        Return False
                    End If
                    'Set Version
                    Dim LTFSVersion As String = "2.4.0"
                    If ExtraPartitionCount = 0 Then LTFSVersion = "2.4.1"
                    ProgressReport($"WRITE ATTRIBUTE: Format Version={LTFSVersion}..")
                    If TapeUtils.SetMAMAttribute(handle, &H80B, LTFSVersion.PadRight(16), SenseReport:=senseReportFunc) Then
                        ProgressReport("WRITE ATTRIBUTE: 080B OK" & vbCrLf)
                    Else
                        OnError("WRITE ATTRIBUTE: 080B Fail" & vbCrLf)
                        Return False
                    End If
                    'Mode Select:Block Length
                    ProgressReport($"MODE SELECT - Block Size {BlockLen}..")
                    Dim blkSenseData As Byte() = TapeUtils.SetBlockSize(handle:=handle, BlockSize:=BlockLen)
                    senseReportFunc(blkSenseData)
                    If blkSenseData.Length > 0 Then
                        ProgressReport($"MODE SELECT - Block Size {BlockLen} OK" & vbCrLf)
                    Else
                        OnError($"MODE SELECT - Block Size {BlockLen} Fail" & vbCrLf)
                        Return False
                    End If
                    'Locate
                    ProgressReport("Locate to data partition..")
                    Dim LocateAddCode As UShort = TapeUtils.Locate(handle, 0, ExtraPartitionCount)
                    If LocateAddCode = 0 Then
                        ProgressReport($"Locate P{ExtraPartitionCount}B0 OK" & vbCrLf)
                    Else
                        OnError($"Locate P{ExtraPartitionCount}B0 Fail{vbCrLf}{ParseAdditionalSenseCode(LocateAddCode)}" & vbCrLf)
                        Return False
                    End If
                    'Set Encryption key
                    SetEncryption(handle, EncryptionKey, SenseReport:=senseReportFunc)
                    'Write VOL1Label
                    ProgressReport("Write VOL1Label..")
                    If TapeUtils.Write(handle, New Vol1Label().GenerateRawData(Barcode)).Length > 0 Then
                        ProgressReport("Write VOL1Label OK" & vbCrLf)
                    Else
                        OnError("Write VOL1Label Fail" & vbCrLf)
                        Return False
                    End If

                    'Write FileMark
                    ProgressReport("Write FileMark..")
                    If TapeUtils.WriteFileMark(handle).Length > 0 Then
                        ProgressReport("Write FileMark OK" & vbCrLf)
                    Else
                        OnError("Write FileMark Fail" & vbCrLf)
                        Return False
                    End If

                    Dim plabel As New ltfslabel()
                    plabel.volumeuuid = Guid.NewGuid()
                    plabel.location.partition = ltfslabel.PartitionLabel.b
                    plabel.partitions.index = ltfslabel.PartitionLabel.a
                    plabel.partitions.data = ltfslabel.PartitionLabel.b
                    plabel.blocksize = BlockLen

                    'Write ltfslabel
                    ProgressReport("Write ltfslabel..")
                    Dim WriteSenseData As Byte() = TapeUtils.Write(handle, Encoding.UTF8.GetBytes(plabel.GetSerializedText()))
                    senseReportFunc(WriteSenseData)
                    If WriteSenseData.Length > 0 Then
                        ProgressReport("Write ltfslabel OK" & vbCrLf)
                    Else
                        OnError("Write ltfslabel Fail" & vbCrLf)
                        Return False
                    End If

                    'Write FileMark
                    ProgressReport("Write FileMark..")
                    WriteSenseData = TapeUtils.WriteFileMark(handle, 2)
                    senseReportFunc(WriteSenseData)
                    If WriteSenseData.Length > 0 Then
                        ProgressReport("Write 2FileMark OK" & vbCrLf)
                    Else
                        OnError("Write 2FileMark Fail" & vbCrLf)
                        Return False
                    End If

                    Dim pindex As New ltfsindex
                    pindex.volumeuuid = plabel.volumeuuid
                    pindex.generationnumber = 1
                    pindex.creator = plabel.creator
                    pindex.updatetime = plabel.formattime
                    pindex.location.partition = ltfslabel.PartitionLabel.b
                    pindex.location.startblock = TapeUtils.ReadPosition(handle).BlockNumber
                    pindex.previousgenerationlocation = Nothing
                    pindex.highestfileuid = 1
                    Dim block1 As ULong = pindex.location.startblock
                    pindex._directory = New List(Of ltfsindex.directory)
                    pindex._directory.Add(New ltfsindex.directory With {.name = VolumeName, .readonly = False,
                                                  .creationtime = plabel.formattime, .changetime = .creationtime,
                                                  .accesstime = .creationtime, .modifytime = .creationtime, .backuptime = .creationtime, .fileuid = 1, .contents = New ltfsindex.contentsDef()})

                    'Write ltfsindex
                    ProgressReport("Write ltfsindex..")
                    WriteSenseData = TapeUtils.Write(handle, Encoding.UTF8.GetBytes(pindex.GetSerializedText()))
                    senseReportFunc(WriteSenseData)
                    If WriteSenseData.Length > 0 Then
                        ProgressReport("Write ltfsindex OK" & vbCrLf)
                    Else
                        OnError("Write ltfsindex Fail" & vbCrLf)
                        Return False
                    End If

                    'Write FileMark
                    ProgressReport("Write FileMark..")
                    WriteSenseData = TapeUtils.WriteFileMark(handle)
                    senseReportFunc(WriteSenseData)
                    If WriteSenseData.Length > 0 Then
                        ProgressReport("Write FileMark OK" & vbCrLf)
                    Else
                        OnError("Write FileMark Fail" & vbCrLf)
                        Return False
                    End If
                    Dim block0 As ULong
                    If ExtraPartitionCount > 0 Then
                        'Locate
                        ProgressReport("Locate to index partition..")
                        LocateAddCode = TapeUtils.Locate(handle, 0, 0)
                        If LocateAddCode = 0 Then
                            ProgressReport("Locate P0B0 OK" & vbCrLf)
                        Else
                            OnError($"Locate P0B0 Fail{vbCrLf}{ParseAdditionalSenseCode(LocateAddCode)}" & vbCrLf)
                            Return False
                        End If
                        'Write VOL1Label
                        ProgressReport("Write VOL1Label..")
                        WriteSenseData = TapeUtils.Write(handle, New Vol1Label().GenerateRawData(Barcode))
                        senseReportFunc(WriteSenseData)
                        If WriteSenseData.Length > 0 Then
                            ProgressReport("Write VOL1Label OK" & vbCrLf)
                        Else
                            OnError("Write VOL1Label Fail" & vbCrLf)
                            Return False
                        End If
                        'Write FileMark
                        ProgressReport("Write FileMark..")
                        WriteSenseData = TapeUtils.WriteFileMark(handle)
                        senseReportFunc(WriteSenseData)
                        If WriteSenseData.Length > 0 Then
                            ProgressReport("Write FileMark OK" & vbCrLf)
                        Else
                            OnError("Write FileMark Fail" & vbCrLf)
                            Return False
                        End If
                        'Write ltfslabel
                        plabel.location.partition = ltfslabel.PartitionLabel.a
                        ProgressReport("Write ltfslabel..")
                        WriteSenseData = TapeUtils.Write(handle, Encoding.UTF8.GetBytes(plabel.GetSerializedText()))
                        senseReportFunc(WriteSenseData)
                        If WriteSenseData.Length > 0 Then
                            ProgressReport("Write ltfslabel OK" & vbCrLf)
                        Else
                            OnError("Write ltfslabel Fail" & vbCrLf)
                            Return False
                        End If
                        'Write FileMark
                        ProgressReport("Write FileMark..")
                        WriteSenseData = TapeUtils.WriteFileMark(handle, 2)
                        senseReportFunc(WriteSenseData)
                        If WriteSenseData.Length > 0 Then
                            ProgressReport("Write FileMark OK" & vbCrLf)
                        Else
                            OnError("Write FileMark Fail" & vbCrLf)
                            Return False
                        End If
                        'Write ltfsindex
                        pindex.previousgenerationlocation = New ltfsindex.LocationDef()
                        pindex.previousgenerationlocation.partition = pindex.location.partition
                        pindex.previousgenerationlocation.startblock = pindex.location.startblock
                        pindex.location.partition = ltfsindex.PartitionLabel.a
                        pindex.location.startblock = TapeUtils.ReadPosition(handle).BlockNumber
                        block0 = pindex.location.startblock
                        ProgressReport("Write ltfsindex..")
                        WriteSenseData = TapeUtils.Write(handle, Encoding.UTF8.GetBytes(pindex.GetSerializedText()))
                        senseReportFunc(WriteSenseData)
                        If WriteSenseData.Length > 0 Then
                            ProgressReport("Write ltfsindex OK" & vbCrLf)
                        Else
                            OnError("Write ltfsindex Fail" & vbCrLf)
                            Return False
                        End If
                        'Write FileMark
                        ProgressReport("Write FileMark..")
                        WriteSenseData = TapeUtils.WriteFileMark(handle)
                        senseReportFunc(WriteSenseData)
                        If WriteSenseData.Length > 0 Then
                            ProgressReport("Write FileMark OK" & vbCrLf)
                        Else
                            OnError("Write FileMark Fail" & vbCrLf)
                            Return False
                        End If
                    End If
                    'Set DateTime
                    Dim CurrentTime As String = Now.ToUniversalTime.ToString("yyyyMMddhhmm")
                    ProgressReport($"WRITE ATTRIBUTE: Written time={CurrentTime}..")
                    If TapeUtils.SetMAMAttribute(handle, &H804, CurrentTime.PadRight(12), SenseReport:=senseReportFunc) Then
                        ProgressReport("WRITE ATTRIBUTE: 0804 OK" & vbCrLf)
                    Else
                        OnError("WRITE ATTRIBUTE: 0804 Fail" & vbCrLf)
                        Return False
                    End If
                    'Set VCI
                    ProgressReport($"WRITE ATTRIBUTE: VCI..")
                    If TapeUtils.WriteVCI(handle:=handle, Generation:=pindex.generationnumber, block0:=block0, block1:=block1, UUID:=pindex.volumeuuid.ToString(), ExtraPartitionCount:=ExtraPartitionCount, SenseReport:=senseReportFunc) Then
                        ProgressReport("WRITE VCI OK" & vbCrLf)
                    Else
                        ProgressReport("WRITE VCI Fail" & vbCrLf)
                    End If
                    OnFinish("Format finished.")
                    Return True
                Catch ex As Exception
                    OnError(ex.ToString())
                End Try

            End Function
        If ImmediateMode Then
            SyncLock SCSIOperationLock
                Return mkltfs_op()
            End SyncLock
        Else
            Dim th As New Threading.Thread(
                Sub()
                    SyncLock SCSIOperationLock
                        mkltfs_op()
                    End SyncLock
                End Sub)
            th.Start()
            Return True
        End If
    End Function
    Public Shared Function RawDump(TapeDrive As String, OutputFile As String, BlockAddress As Long, ByteOffset As Long, FileOffset As Long, Partition As Long, TotalBytes As Long, ByRef StopFlag As Boolean, Optional ByVal BlockSize As Long = 524288, Optional ByVal ProgressReport As Func(Of Long, Boolean) = Nothing, Optional ByVal CreateNew As Boolean = True, Optional LockDrive As Boolean = True) As Boolean
        SyncLock SCSIOperationLock
            Dim handle As IntPtr
            If Not OpenTapeDrive(TapeDrive, handle) Then Throw New Exception($"Cannot open {TapeDrive}")
            Dim result As Boolean = RawDump(handle, OutputFile, BlockAddress, ByteOffset, FileOffset, Partition, TotalBytes, StopFlag, BlockSize, ProgressReport, CreateNew, LockDrive)
            If Not CloseTapeDrive(handle) Then Throw New Exception($"Cannot close {TapeDrive}")
            Return result
        End SyncLock
    End Function
    Public Shared Function RawDump(handle As IntPtr, OutputFile As String, BlockAddress As Long, ByteOffset As Long, FileOffset As Long, Partition As Long, TotalBytes As Long, ByRef StopFlag As Boolean, Optional ByVal BlockSize As Long = 524288, Optional ByVal ProgressReport As Func(Of Long, Boolean) = Nothing, Optional ByVal CreateNew As Boolean = True, Optional LockDrive As Boolean = True) As Boolean
        SyncLock SCSIOperationLock
            If LockDrive AndAlso Not ReserveUnit(handle) Then Return False
            BlockSize = Math.Min(BlockSize, GlobalBlockLimit)
            If LockDrive AndAlso Not PreventMediaRemoval(handle) Then
                ReleaseUnit(handle)
                Return False
            End If
            If Locate(handle:=handle, BlockAddress:=BlockAddress, Partition:=Partition, DestType:=LocateDestType.Block) <> 0 Then
                If LockDrive Then
                    AllowMediaRemoval(handle)
                    ReleaseUnit(handle)
                End If
                Return False
            End If
            Try
                If CreateNew Then IO.File.WriteAllBytes(OutputFile, {})
            Catch ex As Exception
                If LockDrive Then
                    AllowMediaRemoval(handle)
                    ReleaseUnit(handle)
                End If
                Return False
            End Try
            Dim fs As IO.FileStream
            Try
                fs = New IO.FileStream(OutputFile, IO.FileMode.OpenOrCreate, IO.FileAccess.ReadWrite, IO.FileShare.Read, 8388608, IO.FileOptions.SequentialScan)
            Catch ex As Exception
                If LockDrive Then
                    AllowMediaRemoval(handle)
                    ReleaseUnit(handle)
                End If
                Return False
            End Try
            Try
                fs.Seek(FileOffset, IO.SeekOrigin.Begin)
                Dim ReadedSize As Long = 0
                While (ReadedSize < TotalBytes + ByteOffset) And Not StopFlag
                    Dim len As Integer = Math.Min(BlockSize, TotalBytes + ByteOffset - ReadedSize)
                    Dim Data As Byte() = ReadBlock(handle:=handle, BlockSizeLimit:=len)
                    If Data.Length <> len OrElse len = 0 Then
                        If LockDrive Then
                            AllowMediaRemoval(handle)
                            ReleaseUnit(handle)
                        End If
                        Return False
                    End If
                    ReadedSize += len
                    fs.Write(Data, ByteOffset, len - ByteOffset)
                    If ProgressReport IsNot Nothing Then StopFlag = ProgressReport(len - ByteOffset)
                    ByteOffset = 0
                End While
                If LockDrive Then
                    AllowMediaRemoval(handle)
                    ReleaseUnit(handle)
                End If
                If StopFlag Then
                    fs.Close()
                    IO.File.Delete(OutputFile)
                    Return True
                End If
            Catch ex As Exception
                MessageBox.Show(New Form With {.TopMost = True}, ex.ToString)
                fs.Close()
                IO.File.Delete(OutputFile)
                If LockDrive Then
                    AllowMediaRemoval(handle)
                    ReleaseUnit(handle)
                End If
                Return False
            End Try
            fs.Close()
            Return True
        End SyncLock
    End Function
    Public Shared Function ParseTimeStamp(t As String) As Date
        If t Is Nothing OrElse t = "" Then Return Nothing
        'yyyy-MM-ddTHH:mm:ss.fffffff00Z
        Return Date.ParseExact(t, "yyyy-MM-ddTHH:mm:ss.fffffff00Z", Globalization.CultureInfo.InvariantCulture)
    End Function
    <Serializable>
    Public Class BlockDevice
        Public Property DevIndex As String = ""
        Public Property SerialNumber As String = ""
        Public Property VendorId As String = ""
        Public Property ProductId As String = ""
        Public Property DriveLetter As String = ""
        Public Property DeviceType As String = "TAPE"
        Public Property DevicePath As String = ""
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
            Dim o As String = DeviceType & DevIndex & ":"
            If DriveLetter <> "" Then o &= " (" & DriveLetter & ":)"
            o &= " [" & SerialNumber & "] " & VendorId & " " & ProductId
            Return o
        End Function
        Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(BlockDevice))
            Dim sb As New Text.StringBuilder
            Dim t As New IO.StringWriter(sb)
            Dim ns As New Xml.Serialization.XmlSerializerNamespaces({New Xml.XmlQualifiedName("v", "1")})
            writer.Serialize(t, Me, ns)
            Return sb.ToString()
        End Function
        Public Shared Function FromXML(s As String) As BlockDevice
            Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(BlockDevice))
            Dim t As IO.TextReader = New IO.StringReader(s)
            Return CType(reader.Deserialize(t), BlockDevice)
        End Function
    End Class
    <TypeConverter(GetType(ExpandableObjectConverter))>
    <Serializable>
    Public Class MediumChanger
        Public Property DevIndex As String
        Public Property SerialNumber As String
        Public Property VendorId As String
        Public Property ProductId As String
        Public Property RawElementData As Byte()
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of Element), Element)))>
        Public Property Elements As New List(Of Element)
        ''' <summary>
        ''' The lowest element address found for the specified Element Type Code that is greater than or equal to the Starting Element Address.
        ''' </summary>
        Public Property FirstElementAddressReported As UInt16
        ''' <summary>
        ''' The number of elements found for the specified Element Type Code that are greater than or equal to the Starting Element Address. This number is always less than or equal the Number of Elements specified in the CBD.
        ''' </summary>
        Public Property NumberofElementsAvailable As UInt16
        ''' <summary>
        ''' The number of bytes of element status data available. This count does not include the Element Status Data header bytes. The count is not adjusted to match the allocation length you specified in the Read Element Status command.
        ''' </summary>
        Public Property ByteCountofReportAvailable As UInt16
        Public Sub New()

        End Sub
        Public Sub New(DevIndex As String, SerialNumber As String, VendorId As String, ProductId As String)
            Me.DevIndex = DevIndex.TrimEnd(" ")
            Me.SerialNumber = SerialNumber.TrimEnd(" ")
            Me.VendorId = VendorId.TrimEnd(" ")
            Me.ProductId = ProductId.TrimEnd(" ")
        End Sub
        Public Overrides Function ToString() As String
            Dim o As String = "CHANGER" & DevIndex & ":"
            o &= " [" & SerialNumber & "] " & VendorId & " " & ProductId
            Return o
        End Function
        Public Shared Function SCSIReportLUNs(Changer As String, Optional ByRef sense As Byte() = Nothing) As List(Of Byte)
            Dim datalen As Byte() = SCSIReadParam(Changer, {&HA0, 0, 0, 0, 0, 0, 0, 0, 0, &H10, 0, 0}, 16)
            Dim rdLen As Integer = datalen(0)
            rdLen <<= 8
            rdLen = rdLen Or datalen(1)
            rdLen <<= 8
            rdLen = rdLen Or datalen(2)
            rdLen <<= 8
            rdLen = rdLen Or datalen(3)
            rdLen += 8
            datalen(0) = rdLen >> 24 And &HFF
            datalen(1) = rdLen >> 16 And &HFF
            datalen(2) = rdLen >> 8 And &HFF
            datalen(3) = rdLen And &HFF

            Dim rawData As Byte() = SCSIReadParam(Changer, {&HA0, 0, 0, 0, 0, 0, datalen(0), datalen(1), datalen(2), datalen(3), 0, 0}, rdLen)
            Dim valueCount As Integer = (rdLen - 8) / 8
            Dim result As New List(Of Byte)
            For i As Integer = 0 To valueCount - 1
                result.Add(rawData(i * 8 + 8 + 1))
            Next
            Return result
        End Function

        Public Shared Function SCSIReadElementStatus(Changer As String, Optional ByRef sense As Byte() = Nothing, Optional ByVal dSize As Integer = 8, Optional ByVal LUN As Byte = 0) As Byte()
            Dim cdbBytes As Byte()
            Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(dSize)
            Dim succ As Boolean
            If dSize <= 8 Then
                dSize = 8
                cdbBytes = {&HB8, LUN << 5 Or &H10, 0, 0, &HFF, &HFF, 3, dSize >> 16 And &HFF, dSize >> 8 And &HFF, dSize And &HFF, 0, 0}
                SyncLock TapeUtils.SCSIOperationLock
                    Dim handle As IntPtr
                    TapeUtils.OpenTapeDrive(Changer, handle)
                    succ = TapeSCSIIOCtlUnmanaged(handle, cdbBytes, dataBuffer, 8, 1, 60, sense)
                    TapeUtils.CloseTapeDrive(handle)
                End SyncLock
                If succ Then
                    Dim data0(dSize - 1) As Byte
                    Marshal.Copy(dataBuffer, data0, 0, dSize)
                    dSize = data0(5)
                    dSize <<= 8
                    dSize = dSize Or data0(6)
                    dSize <<= 8
                    dSize = dSize Or data0(7)
                    Marshal.FreeHGlobal(dataBuffer)
                Else
                    Marshal.FreeHGlobal(dataBuffer)
                    Return Nothing
                End If

                dSize += 8
            End If

            cdbBytes = {&HB8, &H10, 0, 0, &HFF, &HFF, 3, dSize >> 16 And &HFF, dSize >> 8 And &HFF, dSize And &HFF, 0, 0}
            dataBuffer = Marshal.AllocHGlobal(dSize)
            SyncLock TapeUtils.SCSIOperationLock
                Dim handle As IntPtr
                TapeUtils.OpenTapeDrive(Changer, handle)
                succ = TapeSCSIIOCtlUnmanaged(handle, cdbBytes, dataBuffer, dSize, 1, 60, sense)
                TapeUtils.CloseTapeDrive(handle)
            End SyncLock
            If succ Then
                Dim data1(dSize - 1) As Byte
                Marshal.Copy(dataBuffer, data1, 0, dSize)
                Marshal.FreeHGlobal(dataBuffer)
                Return data1
            Else
                Marshal.FreeHGlobal(dataBuffer)
                Return Nothing
            End If
        End Function
        Public Shared Sub MoveMedium(Changer As String, src As UInt32, dest As UInt32, Optional ByVal sense As Byte() = Nothing, Optional ByVal LUN As Byte = 0)
            SCSIReadParam(TapeDrive:=Changer, cdbData:={&HA5, LUN << 5, 0, 0, src >> 8 And &HFF, src And &HFF, dest >> 8 And &HFF, dest And &HFF, 0, 0, 0, 0}, paramLen:=12,
                senseReport:=Function(s As Byte()) As Boolean
                                 If sense IsNot Nothing AndAlso sense.Length >= 64 Then
                                     Array.Copy(s, sense, Math.Min(64, s.Length))
                                 End If
                                 Return True
                             End Function)
        End Sub
        Public Sub RefreshElementStatus(Optional ByVal IgnoreLUN0 As Boolean = True)
            Dim LUNList As List(Of Byte) = SCSIReportLUNs($"\\.\CHANGER{DevIndex}")
            If LUNList IsNot Nothing AndAlso LUNList.Count > 0 Then
                Elements = New List(Of Element)
                For Each LUN As Byte In LUNList
                    If IgnoreLUN0 AndAlso LUN = 0 Then Continue For
                    If RawElementData IsNot Nothing AndAlso RawElementData.Length > 8 Then

                        RawElementData = SCSIReadElementStatus($"\\.\CHANGER{DevIndex}", dSize:=RawElementData.Length, LUN:=LUN)
                    Else
                        RawElementData = SCSIReadElementStatus($"\\.\CHANGER{DevIndex}", LUN:=LUN)
                    End If
                    FirstElementAddressReported = CInt(RawElementData(0)) << 8 Or RawElementData(1)
                    NumberofElementsAvailable = CInt(RawElementData(2)) << 8 Or RawElementData(3)
                    ByteCountofReportAvailable = CInt(RawElementData(5)) << 16 Or CInt(RawElementData(6)) << 8 Or RawElementData(7)
                    Dim offset As Integer = 8
                    While offset < RawElementData.Length - 8
                        Dim PageHeader(7) As Byte
                        Array.Copy(RawElementData, offset, PageHeader, 0, 8)
                        offset += 8
                        Dim dlen As UInt16 = CInt(PageHeader(2)) << 8 Or PageHeader(3)
                        Dim totallen As UInt32 = CInt(PageHeader(5)) << 16 Or CInt(PageHeader(6)) << 8 Or PageHeader(7)
                        Dim dcount As Integer = totallen \ dlen
                        Dim pagedata(dlen - 1) As Byte
                        For i As Integer = 0 To dcount - 1
                            Array.Copy(RawElementData, offset + dlen * i, pagedata, 0, dlen)
                            Dim e As Element = Element.FromRaw(pagedata, New Element With {
                                .LUN = LUN,
                                .ElementTypeCode = PageHeader(0) And &HF,
                                .PVolTag = PageHeader(1) >> 7 And 1,
                                .AVolTag = PageHeader(1) >> 6 And 1,
                                .ElementDescriptorLength = dlen,
                                .ByteCountofDescriptorDataAvailable = totallen
                                })
                            Elements.Add(e)
                        Next
                        offset += totallen
                    End While
                Next

            End If





        End Sub
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable>
        Public Class Element
            Public Property LUN As Byte = 0
            Public Property RawData As Byte()
            ''' <summary>
            ''' <para>1h = Medium Transport Element (robot hand)</para>
            ''' <para>2h = Storage Element (cartridge cells)</para>
            ''' <para>3h = Import/Export Element (CAP cells)</para>
            ''' <para>4h = Data Transfer Element (drives Or empty drive slots)</para>
            ''' </summary>
            Public Enum ElementTypeCodes
                MediumTransportElement = 1
                StorageElement = 2
                ImportExportElement = 3
                DataTransferElement = 4
            End Enum
            Public Property ElementTypeCode As ElementTypeCodes
            ''' <summary>
            ''' <para>0 = The library omits Primary Volume Tag information from the element descriptors.</para>
            ''' <para>1 = The library includes Primary Volume Tag information in the element descriptors.</para>
            ''' </summary>
            Public Property PVolTag As Boolean
            ''' <summary>
            ''' 0 = The library does not support Alternative Volume Tags.
            ''' </summary>
            Public Property AVolTag As Boolean
            ''' <summary>
            ''' The total number of bytes contained in a single element descriptor.
            ''' </summary>
            Public Property ElementDescriptorLength As Integer
            ''' <summary>
            ''' The number of bytes of element descriptor data available. This count does not include the Element Status Page header bytes. The count is not adjusted to match the allocation length you specified in the Read Element Status command.
            ''' </summary>
            Public Property ByteCountofDescriptorDataAvailable As Integer
            Public Property ElementAddress As UInt16
            ''' <summary>
            ''' <para>0 = No operator intervention required to make the CAP accessible</para>
            ''' <para>1 = Operator intervention required to make the CAP accessible</para>
            ''' </summary>
            Public Property OIR As Boolean
            ''' <summary>
            ''' 0 = The import/export element is a CAP. The cartridge will not leave the library when prevented by the Prevent/Allow Medium Removal (1Eh) command.
            ''' </summary>
            Public Property CMC As Boolean
            ''' <summary>
            ''' 1 = The CAP supports importing cartridges.
            ''' </summary>
            Public Property InEnab As Boolean
            ''' <summary>
            ''' 1 = The CAP supports exporting cartridges.
            ''' </summary>
            Public Property ExEnab As Boolean
            ''' <summary>
            ''' <para>0 = The robot cannot access the element. For Import/Export elements, this can occur when the CAP is open or a CAP magazine was removed. For Data transfer elements, this can occur when a cartridge is loaded in a drive.</para>
            ''' <para>1 = The robot can access the element</para>
            ''' </summary>
            Public Property Access As Boolean
            ''' <summary>
            ''' <para>0 = The element is in a normal state</para>
            ''' <para>1 = The element Is in an abnormal state. The Additional Sense Code (ASC) And the Additional Sense Code Qualifier (ASCQ) fields contain information regarding the abnormal state. Other fields in the descriptor might be invalid And should be ignored.</para>
            ''' </summary>
            Public Property Except As Boolean
            ''' <summary>
            ''' <para>0 = The robot placed the cartridge in the CAP for an export operation.</para>
            ''' <para>1 = An operator placed the cartridge in the CAP for an import operation.</para>
            ''' </summary>
            Public Property ImpExp As Boolean
            ''' <summary>
            ''' <para>0 = The element does not contain a cartridge</para>
            ''' <para>1 = The element contains a cartridge</para>
            ''' </summary>
            Public Property Full As Boolean
            ''' <summary>
            ''' Additional Sense Code
            ''' <para>This field is valid only if the Except bit is set. In the case of an exception, it contains an ASC as defined for Request Sense data.</para>
            ''' <para>Condition_____________________ASC Value___________ASCQ Value</para>
            ''' <para>CAP Open____________________3Ah___________________02h</para>
            ''' <para>Empty Drive Slot_____________3Bh___________________1Ah</para>
            ''' <para>Drive Hardware Error_______40h___________________02h</para>
            ''' </summary>
            Public Property ASC As Byte
            ''' <summary>
            ''' Additional Sense Code Qualifier
            ''' <para>This field is valid only if the Except bit is set. In the case of an exception, it contains an ASCQ as defined for Request Sense data.</para>
            ''' <para>Condition_____________________ASC Value___________ASCQ Value</para>
            ''' <para>CAP Open____________________3Ah___________________02h</para>
            ''' <para>Empty Drive Slot_____________3Bh___________________1Ah</para>
            ''' <para>Drive Hardware Error_______40h___________________02h</para>
            ''' </summary>
            Public Property ASCQ As Byte
            ''' <summary>
            ''' <para>0 = The Source Element Address and Invert fields are not valid.</para>
            ''' <para>1 = The Source Element Address and Invert fields are valid.</para>
            ''' </summary>
            Public Property SValid As Boolean
            ''' <summary>
            ''' 0 = The library does not support multi-sided media.
            ''' </summary>
            Public Property Invert As Boolean
            ''' <summary>
            ''' <para>0 = The element is enabled.</para>
            ''' <para>1 = The element is disabled (for example an open CAP, a drive hardware error, or empty drive slot).</para>
            ''' </summary>
            ''' <returns></returns>
            Public Property ED As Boolean
            ''' <summary>
            ''' <para>0h = Unspecified - the medium changer cannot determine the medium type.</para>
            ''' <para>1h = Data Medium</para>
            ''' <para>2h = Cleaning Medium</para>
            ''' </summary>
            Public Property MediumType As Byte
            ''' <summary>
            ''' This field is valid only if the SValid field is 1. This field provides the address of the last storage element this cartridge occupied. The element address value may or may not be the same as this element.
            ''' </summary>
            Public Property SourceStorageElementAddress As UInt16
            ''' <summary>
            ''' <para>When PVolTag is 1, the library returns volume tag information. When PVolTag is 0, the library omits volume tag information.</para>
            ''' <para>The Primary Volume Tag field contains the null-terminated ASCII barcode label on the tape cartridge. If the label on the cartridge tape is not readable or if the element is empty, the Primary Volume Tag field is filled with 36 bytes of zeros. The "Volume Label Format" controls the presentation of the volser in the Primary Volume Tag field. The library supports the following settings:</para>
            ''' <para>· Full Label</para>
            ''' <para>· No Type Checking</para>
            ''' <para>· Prepend Last Two Characters</para>
            ''' <para>· Trim Last Character</para>
            ''' <para>· Trim Last Two Characters</para>
            ''' <para>· Trim First Two Characters</para>
            ''' <para>· Trim First Character</para>
            ''' </summary>
            Public Property PrimaryVolumeTagInformation As String = ""
            ''' <summary>
            ''' <para>0h = Reserved (not supported) for the Medium Transport Element, Storage Element, Import/Export Element, or Data Transfer Element (DvcID = 0) descriptors.</para>
            ''' <para>2h = The identifier contains ASCII graphic codes (code values 20h through 7Eh) for Data Transfer Element (DvcID = 1) descriptor.</para>
            ''' </summary>
            Public Property CodeSet As Byte
            ''' <summary>
            ''' The format and assignment authority for the identifier.
            ''' <para>0h = The library returns vendor specific data.</para>
            ''' </summary>
            ''' <returns></returns>
            Public Property IdentifierType As Byte
            ''' <summary>
            ''' The combined length of the Identifier and the Identifier Pad.
            ''' <para>00h = The library returns 0 bytes of identifier data in the descriptors for Medium Transport Elements, Storage Elements, Import/Export Elements, or Data Transfer Elements (DvcID = 0).</para>
            ''' <para>20h = The library returns 32 bytes of identifier data for the Data Transfer Element (DvcID = 1).</para>
            ''' </summary>
            Public Property IdentifierLength As Byte
            ''' <summary>
            ''' for Data Transfer Element DvcID = 1 Only
            ''' <para>The ASCII Serial Number for the tape drive associated with this data transfer element.</para>
            ''' Identifier Pad (for Data Transfer Element DvcID = 1 Only)
            ''' <para>Contains ASCII blanks. The number of blanks depends on the length of the Identifier field. The combined length of the Identifier field and the Identifier Pad is 32 bytes.</para>
            ''' </summary>
            Public Property Identifier As String = ""
            ''' <summary>
            ''' <para>43h ('C') = The element contains a cleaning cartridge.</para>
            ''' <para>4Ch ('L') = The element contains an LTO cartridge.</para>
            ''' <para>54h ('T') = The element contains a T10000 cartridge.</para>
            ''' <para>FFh = The media domain cannot be determined or the element is empty.</para>
            ''' </summary>
            Public Property MediaDomain As Byte
            ''' <summary>
            ''' FFh = The media type cannot be determined or the element is empty.
            ''' <para>If the Media Domain is 43h (C):</para>
            ''' <para>C = The element contains a T10000 Version 2 cleaning cartridge.</para>
            ''' <para>L = The element contains a T10000 Universal cleaning cartridge.</para>
            ''' <para>T = The element contains a T10000 Version 1 cleaning cartridge.</para>
            ''' <para>U = The element contains a Universal LTO cleaning cartridge.</para>
            ''' <para>If the Media Domain is 4Ch (L):</para>
            ''' <para>3-8 = The element contains a Generation 3-8 LTO cartridge.</para>
            ''' <para>T-Y = The element contains a Generation 3-8 LTO WORM cartridge.</para>
            ''' <para>If the Media Domain is 54h (T):</para>
            ''' <para>1 = The element contains a T10000 Version 1 cartridge.</para>
            ''' <para>2 = The element contains a T10000 Version 2 cartridge.</para>
            ''' <para>S = The element contains a T10000 Version 1 Sport cartridge.</para>
            ''' <para>T = The element contains a T10000 Version 2 Sport cartridge.</para>
            ''' </summary>
            Public Property MediaType As Byte
            ''' <summary>
            ''' <para>4Ch (L) = The drive supports LTO cartridges.</para>
            ''' <para>54h (T) = The drive supports T10000 cartridges.</para>
            ''' <para>FFh = The element domain cannot be determined.</para>
            ''' </summary>
            Public Property TransportDomain As Byte
            ''' <summary>
            ''' <para>If the Transport Domain is 4Ch (L):</para>
            ''' <para>· 3Bh = HP Generation 5 LTO drive</para>
            ''' <para>· 3Ch = IBM Generation 5 LTO drive</para>
            ''' <para>· 3Dh = HP Generation 6 LTO drive.</para>
            ''' <para>· 3Eh = IBM Generation 6 LTO drive.</para>
            ''' <para>· 2Dh = IBM Generation 7 LTO drive.</para>
            ''' <para>· 2Eh = IBM Generation 8 LTO drive.</para>
            ''' <para>If the Transport Domain is 54h (T):</para>
            ''' <para>· 0Dh = StorageTek T10000A drive.</para>
            ''' <para>· 0Eh = StorageTek T10000A drive in 3590 emulation mode.</para>
            ''' <para>· 18h = StorageTek T10000A Encrypting drive.</para>
            ''' <para>· 19h = StorageTek T10000A Encrypting drive in 3590 emulation mode.</para>
            ''' <para>· 1Ah = StorageTek T10000B drive.</para>
            ''' <para>· 1Bh = StorageTek T10000B drive in 3590 emulation mode.</para>
            ''' <para>· 1Ch = StorageTek T10000B Encrypting drive.</para>
            ''' <para>· 1Dh = StorageTek T10000B Encrypting drive in 3590 emulation mode.</para>
            ''' <para>· 22h = StorageTek T10000C drive.</para>
            ''' <para>· 23h = StorageTek T10000C drive in 3590 emulation mode.</para>
            ''' <para>· 24h = StorageTek T10000C Encrypting drive.</para>
            ''' <para>· 25h = StorageTek T10000C Encrypting drive in 3590 emulation mode.</para>
            ''' <para>· 26h = StorageTek T10000D drive.</para>
            ''' <para>· 27h = StorageTek T10000D drive in 3590 emulation mode.</para>
            ''' <para>· 28h = StorageTek T10000D Encrypting drive.</para>
            ''' <para>· 29h = StorageTek T10000D Encrypting drive in 3590 emulation mode.</para>
            ''' <para>· 2Ah = StorageTek T10000D Fibre Channel over Ethernet. </para>
            ''' <para>· 2Bh = StorageTek T10000D Fibre Channel over Ethernet Encrypting drive.</para>
            ''' </summary>
            Public Property TransportType As Byte
            ''' <summary>
            ''' The 32-byte ASCII serial number for the drive.
            ''' <para>For drives with a serial number less than 32 bytes, the library left-justifies the value by returning ASCII blanks for the unused less-significant bytes. If the serial number is not available from a drive that should support an ASCII serial number, the library returns all ASCII blanks.</para>
            ''' </summary>
            Public Property TransportSerialNumber As String = ""
            Public Shared Function FromRaw(RawData As Byte(), Optional ByVal Preset As Element = Nothing) As Element
                If RawData Is Nothing Then Return Nothing
                Dim result As Element
                If Preset IsNot Nothing Then result = Preset Else result = New Element
                result.RawData = RawData
                With result
                    Try
                        Dim DOffset As Byte = 0
                        .ElementAddress = CInt(RawData(DOffset + 0)) << 8 Or RawData(DOffset + 1)
                        .OIR = RawData(DOffset + 2) >> 7 And 1
                        .CMC = RawData(DOffset + 2) >> 6 And 1
                        .InEnab = RawData(DOffset + 2) >> 5 And 1
                        .ExEnab = RawData(DOffset + 2) >> 4 And 1
                        .Access = RawData(DOffset + 2) >> 3 And 1
                        .Except = RawData(DOffset + 2) >> 2 And 1
                        .ImpExp = RawData(DOffset + 2) >> 1 And 1
                        .Full = RawData(DOffset + 2) >> 0 And 1
                        .ASC = RawData(DOffset + 4)
                        .ASCQ = RawData(DOffset + 5)
                        .SValid = RawData(DOffset + 9) >> 7 And 1
                        .Invert = RawData(DOffset + 9) >> 6 And 1
                        .ED = RawData(DOffset + 9) >> 3 And 1
                        .MediumType = RawData(DOffset + 9) And &B111
                        .SourceStorageElementAddress = CInt(RawData(DOffset + 10)) << 8 Or RawData(DOffset + 11)
                        If .PVolTag Then
                            .PrimaryVolumeTagInformation = Encoding.ASCII.GetString(RawData, DOffset + 12, 36).TrimEnd(Chr(0)).TrimEnd(" ")
                            DOffset += 36
                        End If
                        .CodeSet = RawData(DOffset + 12) And &HF
                        .IdentifierType = RawData(DOffset + 13) And &HF
                        .IdentifierLength = RawData(DOffset + 15)
                        If .IdentifierLength > 0 Then
                            .Identifier = Encoding.ASCII.GetString(RawData, DOffset + 16, .IdentifierLength).TrimEnd(Chr(0)).TrimEnd(" ")
                            DOffset += 32
                        End If
                        .MediaDomain = RawData(DOffset + 16)
                        .MediaType = RawData(DOffset + 17)
                        .TransportDomain = RawData(DOffset + 18)
                        .TransportType = RawData(DOffset + 19)
                        If RawData.Length - 1 >= DOffset + 51 Then
                            .TransportSerialNumber = Encoding.ASCII.GetString(RawData, DOffset + 20, 32).TrimEnd(Chr(0)).TrimEnd(" ")
                        End If
                    Catch ex As Exception

                    End Try
                End With
                Return result
            End Function
        End Class
        Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(MediumChanger))
            Dim sb As New Text.StringBuilder
            Dim t As New IO.StringWriter(sb)
            Dim ns As New Xml.Serialization.XmlSerializerNamespaces({New Xml.XmlQualifiedName("v", "1")})
            writer.Serialize(t, Me, ns)
            Return sb.ToString()
        End Function
    End Class
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class GX256
        Public Shared ExpTable(255) As Byte
        Public Shared LogTable(255) As Byte
        Public Shared Sub Initialization()
            ExpTable(0) = 1
            For i As Integer = 1 To 255
                Dim tempval As Integer = (CInt(ExpTable(i - 1)) << 1) 'Xor ExpTable(i - 1)
                If tempval > 255 Then tempval = tempval Xor &H11D
                ExpTable(i) = tempval And &HFF
            Next
            For i As Integer = 0 To 254
                LogTable(ExpTable(i)) = CByte(i And &HFF)
            Next
        End Sub
        Public Shared Function Times(a As Byte, b As Byte) As Byte
            If ExpTable(0) = 0 Then Initialization()
            If a <> 0 AndAlso b <> 0 Then
                Return ExpTable((LogTable(a) + CUInt(LogTable(b))) Mod 255)
            Else
                Return 0
            End If
            'Dim result As Byte = 0
            'Dim num1 As Byte = a
            'For i As Integer = 0 To 7
            '    Dim num2 As Byte = b
            '    For j As Integer = 0 To 7
            '        If (num1 Mod 2) = 1 AndAlso (num2 Mod 2) = 1 Then result = result Xor ExpTable(i + j)
            '        num2 >>= 1
            '    Next
            '    num1 >>= 1
            'Next
            'Return result
        End Function
        Public Shared Function CalcCRC(Data As Byte()) As Byte()
            If ExpTable(0) = 0 Then
                Initialization()
                'Console.WriteLine(Byte2Hex(ExpTable))
                'Console.WriteLine(Byte2Hex(LogTable))
                'Console.WriteLine($"{ExpTable(201)} {ExpTable(246)}")
            End If
            'Console.WriteLine(Byte2Hex(Data))
            Dim R0 As Byte = 0, R1 As Byte = 0, R2 As Byte = 0, R3 As Byte = 0
            Dim TmpVal As Byte = 0
            For i As Integer = 0 To Data.Length - 1
                'Console.WriteLine(Byte2Hex({R3, R2, R1, R0}))
                TmpVal = R3 Xor Data(i)
                R3 = R2 Xor Times(ExpTable(201), TmpVal)
                R2 = R1 Xor Times(ExpTable(246), TmpVal)
                R1 = R0 Xor Times(ExpTable(201), TmpVal)
                R0 = TmpVal
            Next
            Return {R3, R2, R1, R0}
        End Function
    End Class

    <Serializable>
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class CMParser
        Const GUARD_WRAP_IDENTIFIER As Integer = &HFFFFFFFE
        Const UNUSED_WRAP_IDENTIFIER As Integer = &HFFFFFFFF
        <Category("Internal")>
        Public Property a_CMBuffer As Byte() = {}

        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_PageID As Integer
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_Offset As Integer
        <Category("Internal")>
        Public Property a_UnProt As Integer = 0
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_Key As Integer
        <Xml.Serialization.XmlIgnore> Public a_Index As Integer
        <Category("Internal")>
        Public Property a_Err As Integer = 0
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_Buffer As Byte()
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property at_Offset As Byte()
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_Length As Integer = 0
        <Category("Internal")>
        Public Property a_CleansRemaining As Integer = 0
        <Category("Internal")>
        Public Property a_CleanLength As Double
        <Category("Internal")>
        Public Property a_NWraps As Integer = 0
        <Category("Internal")>
        Public Property a_TapeDirLength As Integer = 16
        <Category("Internal")>
        Public Property a_SetsPerWrap As Integer = 0
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_SetID As Integer = 0
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_LastID As Integer = 0
        <Category("Internal")>
        Public Property a_Barcode As String
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_AttributeID As Integer
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_AttributeLength As Integer
        <Category("Internal")>
        Public Property a_HdrLength As Integer
        <Category("Internal")>
        Public Property a_DriveTypeIdentifier As Integer = 0
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_OutputStr As String = ""
        <Category("Internal")>
        Public Property a_TapeWritePassPartition As Integer = 0
        <Category("Internal")>
        Public Property a_NumPartitions As New List(Of EOD)
        <Xml.Serialization.XmlIgnore> Public a_PartitionKey As Integer
        <Category("Internal")>
        <Xml.Serialization.XmlIgnore> Public Property a_set As Integer
        <Category("Internal")>
        Public Property g_ValidCM As Boolean = False
        <Category("Internal")>
        Public Property g_Channels As Integer = 8
        <Category("Internal")>
        Public Property g_LoadCount As Integer = 0
        <Category("Internal")>
        Public Property g_CartridgeSN As String = "          "
        <Category("Internal")>
        Public Property g_TPM As Integer = 0
        <Category("Internal")>
        Public Property g_Barcode As String = "        "
        <Category("Internal")>
        Public Property g_DHLTimeStamp As Integer = 0
        <Category("Internal")>
        Public Property g_FaultLogSize As Integer = 0
        <Category("Internal")>
        Public Property g_LtoPearlFlagEnable As Integer = 0
        <Category("Internal")>
        Public Property g_DHLPowerCount As Integer = 0
        <Category("Internal")>
        Public Property g_DHLPocCount As Integer = 0


        <Category("Pages")>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of Page), Page)))>
        Public Property PageData As New List(Of Page)
        <Category("PageData")>
        Public Property CartridgeMfgData As New Cartridge_mfg
        <Category("PageData")>
        Public Property MediaMfgData As New Media_mfg
        <Category("PageData")>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of UsagePage), UsagePage)))>
        Public Property a_UsageData As New List(Of UsagePage)
        <Category("PageData")>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of Usage), Usage)))>
        Public Property UsageData As New List(Of Usage)
        <Category("PageData")>
        Public Property StatusData As New TapeStatus
        <Category("PageData")>
        Public Property InitialisationData As New Initialisation
        <Category("PageData")>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of EOD), EOD)))>
        Public Property PartitionEOD As New List(Of EOD)
        <Category("PageData")>
        Public Property CartridgeContentData As New CartridgeContent
        <Category("PageData")>
        Public Property TapeDirectoryData As New TapeDirectory
        <Category("PageData")>
        Public Property SuspendWriteData As New SuspendWrite
        <Category("PageData")>
        Public Property ApplicationSpecificData As New ApplicationSpecific
        Public Function a_Usage(index As Integer, Optional ByVal CreateNew As Boolean = True) As UsagePage
            For Each up As UsagePage In a_UsageData
                If up.index = index Then Return up
            Next
            If CreateNew Then
                Dim upn As New UsagePage With {.index = index}
                a_UsageData.Add(upn)
                Return upn
            End If
            Return Nothing
        End Function
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class UsagePage
            Public Property index As Integer
            Public Property data0 As Byte()
            Public Property data1 As Integer
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class Page
            Public Property a_Key As Integer
            Public Property Version As Integer
            Public Property Offset As Integer = -1
            Public Property Length As Integer = -1
            Public Property Type As TypeDef
            Public Enum TypeDef
                unprotected
                [protected]
            End Enum
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class Cartridge_mfg
            Public Property TapeVendor As String
            Public Property CartridgeSN As String
            Public Property CartridgeType As Integer
            Public Property Format As String = ""
            Public ReadOnly Property IsLTO3Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt IsNot Nothing AndAlso fmt.Contains("LTO-3") OrElse fmt.Contains("LTO-4") OrElse fmt.Contains("LTO-5") OrElse fmt.Contains("LTO-6") OrElse fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO4Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt IsNot Nothing AndAlso fmt.Contains("LTO-4") OrElse fmt.Contains("LTO-5") OrElse fmt.Contains("LTO-6") OrElse fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO5Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt IsNot Nothing AndAlso fmt.Contains("LTO-5") OrElse fmt.Contains("LTO-6") OrElse fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO6Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt IsNot Nothing AndAlso fmt.Contains("LTO-6") OrElse fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO7Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt IsNot Nothing AndAlso fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO8Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt IsNot Nothing AndAlso fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO9Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt IsNot Nothing AndAlso fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property CartridgeTypeAbbr As String
                Get
                    If ((CartridgeType >> 15) And 1) = 1 Then Return "CU"
                    Select Case CartridgeType And &HFF
                        Case 1
                            Return "L1"
                        Case 2
                            Return "L2"
                        Case 4
                            Return "L3"
                        Case 8
                            Return "L4"
                        Case 16
                            Return "L5"
                        Case 32
                            Return "L6"
                        Case 64
                            If Format.Contains("Type M") Then
                                Return "M8"
                            Else
                                Return "L7"
                            End If
                        Case 128
                            Return "L8"
                        Case 129
                            Return "L9"
                    End Select
                    Return ""
                End Get
            End Property
            Public Shared Function GetCMLength(Abbr As String) As Integer
                Select Case Abbr.ToUpper
                    Case "CU"
                        Return 4096
                    Case "L1"
                        Return 4096
                    Case "L2"
                        Return 4096
                    Case "L3"
                        Return 4096
                    Case "L4"
                        Return 8160
                    Case "L5"
                        Return 8160
                    Case "L6"
                        Return 16352
                    Case "L7"
                        Return 16352
                    Case "M8"
                        Return 16352
                    Case "L8"
                        Return 16352
                    Case "L9"
                        Return 32736
                    Case Else
                        Return 0
                End Select
            End Function
            Public Property KB_PER_DATASET As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 404
                        Case "L2"
                            Return 404
                        Case "L3"
                            Return 1617
                        Case "L4"
                            Return 1590
                        Case "L5"
                            Return 2473
                        Case "L6"
                            Return 2473
                        Case "L7"
                            Return 5032
                        Case "M8"
                            Return 5032
                        Case "L8"
                            Return 5032
                        Case "L9"
                            Return 9806
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property CCQ_PER_DATASET As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 64
                        Case "L2"
                            Return 64
                        Case "L3"
                            Return 128
                        Case "L4"
                            Return 128
                        Case "L5"
                            Return 192
                        Case "L6"
                            Return 192
                        Case "L7"
                            Return 192
                        Case "M8"
                            Return 192
                        Case "L8"
                            Return 192
                        Case "L9"
                            Return 384
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property SETS_PER_WRAP As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 5500
                        Case "L2"
                            Return 8200
                        Case "L3"
                            Return 6000
                        Case "L4"
                            Return 9500
                        Case "L5"
                            Return 7800
                        Case "L6"
                            Return 7805
                        Case "L7"
                            Return 10950
                        Case "M8"
                            Return 10950
                        Case "L8"
                            Return 11660
                        Case "L9"
                            Return 6770
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property MB_PER_WRAP_METRE As Double
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 3.84
                        Case "L2"
                            Return 5.75
                        Case "L3"
                            Return 14.98
                        Case "L4"
                            Return 19.27
                        Case "L5"
                            Return 23.9
                        Case "L6"
                            Return 23.89
                        Case "L7"
                            Return 59.85
                        Case "M8"
                            Return 59.85
                        Case "L8"
                            Return 63.64
                        Case "L9"
                            Return 66.59
                    End Select
                    Return 0
                End Get
                Set(value As Double)
                End Set
            End Property
            Public Property TAPE_LU_LIFE As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 20000
                        Case "L2"
                            Return 20000
                        Case "L3"
                            Return 20000
                        Case "L4"
                            Return 20000
                        Case "L5"
                            Return 20000
                        Case "L6"
                            Return 20000
                        Case "L7"
                            Return 20000
                        Case "M8"
                            Return 20000
                        Case "L8"
                            Return 20000
                        Case "L9"
                            Return 20000
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property TAPE_LIFE_IN_VOLS As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 260
                        Case "L2"
                            Return 260
                        Case "L3"
                            Return 260
                        Case "L4"
                            Return 260
                        Case "L5"
                            Return 260
                        Case "L6"
                            Return 130
                        Case "L7"
                            Return 130
                        Case "M8"
                            Return 98
                        Case "L8"
                            Return 75
                        Case "L9"
                            Return 55
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property WRAP_LEN_IN_MTRS As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 580
                        Case "L2"
                            Return 580
                        Case "L3"
                            Return 648
                        Case "L4"
                            Return 783
                        Case "L5"
                            Return 808
                        Case "L6"
                            Return 808
                        Case "L7"
                            Return 922
                        Case "M8"
                            Return 922
                        Case "L8"
                            Return 922
                        Case "L9"
                            Return 997
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property TAPE_LEN_IN_MTRS As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 609
                        Case "L2"
                            Return 609
                        Case "L3"
                            Return 680
                        Case "L4"
                            Return 820
                        Case "L5"
                            Return 846
                        Case "L6"
                            Return 846
                        Case "L7"
                            Return 960
                        Case "M8"
                            Return 960
                        Case "L8"
                            Return 960
                        Case "L9"
                            Return 1034
                        Case "CU"
                            Return 319
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property NO_WRAPS_ON_TAPE As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 48
                        Case "L2"
                            Return 64
                        Case "L3"
                            Return 44
                        Case "L4"
                            Return 56
                        Case "L5"
                            Return 80
                        Case "L6"
                            Return 136
                        Case "L7"
                            Return 112
                        Case "M8"
                            Return 168
                        Case "L8"
                            Return 208
                        Case "L9"
                            Return 280
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property MIN_DATASETS_FOR_ASSESSING_CAPACITY_LOSS As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return 11064
                        Case "L2"
                            Return 16600
                        Case "L3"
                            Return 12500
                        Case "L4"
                            Return 19500
                        Case "L5"
                            Return 15920
                        Case "L6"
                            Return 15620
                        Case "L7"
                            Return 11060
                        Case "M8"
                            Return 11060
                        Case "L8"
                            Return 12020
                        Case "L9"
                            Return 13540
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property DENSITY_CODE As Integer
                Get
                    Select Case CartridgeTypeAbbr
                        Case "L1"
                            Return &H40
                        Case "L2"
                            Return &H42
                        Case "L3"
                            Return &H44
                        Case "L4"
                            Return &H46
                        Case "L5"
                            Return &H58
                        Case "L6"
                            Return &H5A
                        Case "L7"
                            Return &H5C
                        Case "M8"
                            Return &H5D
                        Case "L8"
                            Return &H5E
                        Case "L9"
                            Return &H60
                    End Select
                    Return 0
                End Get
                Set(value As Integer)
                End Set
            End Property
            Public Property MB_PER_WRAP As Integer
                Get
                    Return KB_PER_DATASET * SETS_PER_WRAP / 1024
                End Get
                Set(value As Integer)
                End Set
            End Property

            Public Property MfgDate As String
            Public Property TapeLength As Integer = 0
            Public Property MediaCode As Integer
            Public Property ParticleType As particle
            Public Property SubstrateType As substrate
            Public Property Servo_Band_ID As svbid
            Public Enum particle
                MP
                BaFe
            End Enum
            Public Enum substrate
                PEN
                SPALTAN
            End Enum
            Public Enum svbid
                legacy_UDIM
                non_UDIM
            End Enum
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class Usage
            Public Property Index As Integer

            Public Property PageID As Integer
            Public Property DrvSN As String
            Public Property ThreadCount As Integer
            Public Property SetsWritten As Long
            Public Property SetsRead As Long
            Public Property TotalSets As Long
            Public Property WriteRetries As Integer
            Public Property ReadRetries As Integer
            Public Property UnRecovWrites As Integer
            Public Property UnRecovReads As Integer
            Public Property SuspendedWrites As Integer
            Public Property FatalSusWrites As Integer
            Public Property SuspendedAppendWrites As Integer
            Public Property LP3Passes As Integer
            Public Property MidpointPasses As Integer
            Public Property MaxTapeTemp As Integer

            Public Property CCQWriteFails As Integer
            Public Property C2RecovErrors As Integer
            Public Property DirectionChanges As Integer
            Public Property TapePullingTime As Integer
            Public Property TapeMetresPulled As Integer
            Public Property Repositions As Integer
            Public Property TotalLoadUnloads As Integer
            Public Property StreamFails As Integer

            Public Property MaxDriveTemp As Double
            Public Property MinDriveTemp As Double

            Public Property LifeSetsWritten As Integer
            Public Property LifeSetsRead As Integer
            Public Property LifeWriteRetries As Integer
            Public Property LifeReadRetries As Integer
            Public Property LifeUnRecovWrites As Integer
            Public Property LifeUnRecovReads As Integer
            Public Property LifeSuspendedWrites As Integer
            Public Property LifeFatalSuspWrites As Integer
            Public Property LifeTapeMetresPulled As Integer

            Public Property LifeSuspAppendWrites As Integer
            Public Property LifeLP3Passes As Integer
            Public Property LifeMidpointPasses As Integer

        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class Media_mfg
            Public Property MediaMfgDate As String
            Public Property MediaVendor As String
            Public ReadOnly Property MediaVendor_Code As Integer
                Get
                    With MediaVendor.ToUpper
                        If .Contains("SONY") Then Return 1
                        If .Contains("TDK") Then Return 2
                        If .Contains("FUJI") Then Return 3
                        If .Contains("MAXELL") Then Return 4
                        If .Contains("IMATION") Then Return 5
                        If .Contains("EMTEC") Then Return 6
                        Return 0
                    End With
                End Get
            End Property
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class TapeStatus
            Public Property ThreadCount As Integer
            Public Property EncryptedData As Boolean
            Public Property LastLocation As Integer = 0
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class Initialisation
            Public Property LP1 As Integer
            Public Property LP2 As Integer
            Public Property LP3 As Integer
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class EOD
            Public Property Partition As Integer
            Public Property Dataset As Integer
            Public Property WrapNumber As Integer
            Public Property Validity As Integer
            Public Property PhysicalPosition As Integer
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class CartridgeContent
            Public Property Drive_Id As String
            Public Property Cartridge_Content As Integer
            Public Property PartitionedCartridge As Boolean
            Public Property Type_M_Cartridge As Boolean
            Public Property Drive_Firmware_Id As String
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class TapeDirectory
            Public Property FID_Tape_Write_Pass_Partition_0 As Integer
            Public Property FID_Tape_Write_Pass_Partition_1 As Integer
            Public Property FID_Tape_Write_Pass_Partition_2 As Integer
            Public Property FID_Tape_Write_Pass_Partition_3 As Integer
            Public Property Wrap As String
            <TypeConverter(GetType(ListTypeDescriptor(Of List(Of WrapEntryItemSet), WrapEntryItemSet)))>
            Public Property WrapEntryInfo As New List(Of WrapEntryItemSet)
            <TypeConverter(GetType(ListTypeDescriptor(Of List(Of Double), Double)))>
            Public Property CapacityLoss As New List(Of Double)
            <TypeConverter(GetType(ListTypeDescriptor(Of List(Of Dataset), Dataset)))>
            Public Property DatasetsOnWrapData As New List(Of Dataset)
            <TypeConverter(GetType(ExpandableObjectConverter))>
            <Serializable> Public Class Dataset
                Public Property Index As Integer
                Public Property Data As Integer
            End Class
            <TypeConverter(GetType(ExpandableObjectConverter))>
            <Serializable> Public Class WrapEntryItemSet
                Public Property Index As Integer
                Public Property Content As String
                Public Property RawData As Integer()
                Public Property RecCount As Integer
                Public Property FileMarkCount As Integer
            End Class
            Public Function WrapEntry(Index As Integer, Optional ByVal CreateNew As Boolean = True) As WrapEntryItemSet
                For Each d As WrapEntryItemSet In WrapEntryInfo
                    If d.Index = Index Then Return d
                Next
                If CreateNew Then
                    Dim dn As New WrapEntryItemSet With {.Index = Index}
                    WrapEntryInfo.Add(dn)
                    Return dn
                End If
                Return Nothing
            End Function
            Public Function DatasetsOnWrap(Index As Integer, Optional ByVal CreateNew As Boolean = True) As Dataset
                For Each d As Dataset In DatasetsOnWrapData
                    If d.Index = Index Then Return d
                Next
                If CreateNew Then
                    Dim dn As New Dataset With {.Index = Index}
                    DatasetsOnWrapData.Add(dn)
                    Return dn
                End If
                Return Nothing
            End Function
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class SuspendWrite
            Public Function DataSetID(Index As Integer, Optional ByVal CreateNew As Boolean = True) As DataInfo
                For Each di As DataInfo In DataSetList
                    If di.Index = Index Then Return di
                Next
                If CreateNew Then
                    Dim din As New DataInfo With {.Index = Index}
                    DataSetList.Add(din)
                    Return din
                End If
                Return Nothing
            End Function
            Public Function WTapePass(Index As Integer, Optional ByVal CreateNew As Boolean = True) As DataInfo
                For Each di As DataInfo In WTapePassList
                    If di.Index = Index Then Return di
                Next
                If CreateNew Then
                    Dim din As New DataInfo With {.Index = Index}
                    WTapePassList.Add(din)
                    Return din
                End If
                Return Nothing
            End Function
            <TypeConverter(GetType(ListTypeDescriptor(Of List(Of DataInfo), DataInfo)))>
            Public Property DataSetList As New List(Of DataInfo)
            <TypeConverter(GetType(ListTypeDescriptor(Of List(Of DataInfo), DataInfo)))>
            Public Property WTapePassList As New List(Of DataInfo)
            <TypeConverter(GetType(ExpandableObjectConverter))>
            <Serializable> Public Class DataInfo
                Public Property Index As Integer
                Public Property Value As Integer
            End Class
        End Class
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class ApplicationSpecific
            Public Property Barcode As String
            Public Property Application_vendor As String
            Public Property Application_name As String
            Public Property Application_version As String
        End Class
        Public Enum gtype
            page
            cartridge_mfg
            media_mfg
            usage
            status
            initialisation
            EOD
            cartridge_content
            tape_directory
            suspended_writes
            application_specific
        End Enum
        Public Function g_CM(gtype As gtype, Optional ByVal gKey As Integer = 0, Optional ByVal createNew As Boolean = True) As Object
            Select Case gtype
                Case gtype.page
                    For Each p As Page In PageData
                        If p.a_Key = gKey Then Return p
                    Next
                    If createNew Then
                        Dim pnew As New Page With {.a_Key = gKey}
                        PageData.Add(pnew)
                        Return pnew
                    Else
                        Return Nothing
                    End If
                Case gtype.cartridge_mfg
                    Return CartridgeMfgData
                Case gtype.media_mfg
                    Return MediaMfgData
                Case gtype.usage
                    For Each u As Usage In UsageData
                        If u.Index = gKey Then Return u
                    Next
                    If createNew Then
                        Dim unew As New Usage With {.Index = gKey}
                        UsageData.Add(unew)
                        Return unew
                    End If
                Case gtype.status
                    Return StatusData
                Case gtype.initialisation
                    Return InitialisationData
                Case gtype.EOD
                    For Each eod As EOD In PartitionEOD
                        If eod.Partition = gKey Then Return eod
                    Next
                    If createNew Then
                        Dim eodn As New EOD With {.Partition = gKey}
                        PartitionEOD.Add(eodn)
                        Return eodn
                    End If
                Case gtype.cartridge_content
                    Return CartridgeContentData
                Case gtype.tape_directory
                    Return TapeDirectoryData
                Case gtype.suspended_writes
                    Return SuspendWriteData
                Case gtype.application_specific
                    Return ApplicationSpecificData
            End Select
            Return Nothing
        End Function
        Public Function RunParse(Optional ByVal Warn As Action(Of String) = Nothing) As Boolean
            If Warn Is Nothing Then
                Warn = Sub(text As String)
                       End Sub
            End If
            ' verify the checksum byte 4 is the xor of the first 4 bytes, otherwise CM data is not valid so don't parse the dataset
            ' also check that byte 5 (the size of CM in 1K units) is 0x04 (LTO1/2/3) or 0x08 (LTO4/5)
            If a_CMBuffer.Length < &H1000 Then
                If a_CMBuffer.Length <> 0 Then
                    Warn($"invalid CM buffer size (0x{Hex(a_CMBuffer.Length)}){vbCrLf}")
                End If
                Return False
            ElseIf ((a_CMBuffer(0) Xor a_CMBuffer(1) Xor a_CMBuffer(2) Xor a_CMBuffer(3)) <> a_CMBuffer(4)) Then
                Warn($"invalid CM checksum byte with length {a_CMBuffer.Length}{vbCrLf}")
                Return False
            Else
                ' Check the CM size byte, located at offset 5.
                a_Length = a_CMBuffer(5)
                ' CR 11775 : Changes For LTO9
                ' --------------------------------------------------------------------
                ' LTO9 has a 32kB CM (length Byte 0x20) so allow For that As well.
                If (a_Length <> 4 AndAlso a_Length <> 8 AndAlso a_Length <> 16 AndAlso a_Length <> 32) Then
                    If a_Length <> 0 Then ' only warn if there's a strange non-zero size
                        Warn($"invalid CM size byte (0x{Hex(a_CMBuffer(5))}){vbCrLf}")
                    End If
                    Return False
                End If
            End If

            ' assume valid CM for now (still need to check page IDs)
            g_ValidCM = True

            ' get page info
            a_UnProt = 0
            a_Offset = 36 ' start of the protected table
            While a_Offset < 400
                a_PageID = g_GetWord(a_CMBuffer, a_Offset) And &HFFF
                If a_PageID = &HFFF Then
                    ' EOPT (end of page table)
                    If a_UnProt = 0 Then
                        ' end of protected page table, therefore the offset value is the start of the unprotected page table
                        a_UnProt = 1
                        a_Offset = g_GetWord(a_CMBuffer, a_Offset + 2)
                    Else
                        Exit While ' end of unprotected page table
                    End If
                ElseIf (a_PageID = &HFFC OrElse a_PageID = &HFFE) Then
                    ' Empty or Pad
                    a_Offset += 4
                Else
                    a_Key = a_PageID
                    With CType(g_CM(gtype.page, a_Key), Page)
                        .Version = (a_CMBuffer(a_Offset) >> 4) And &HF
                        .Offset = g_GetWord(a_CMBuffer, a_Offset + 2)
                        .Length = g_GetWord(a_CMBuffer, .Offset + 2)
                        If a_UnProt > 0 Then .Type = Page.TypeDef.unprotected Else .Type = Page.TypeDef.protected
                        ' check that the page headers contain the correct page ID
                        If g_GetWord(a_CMBuffer, .Offset) <> g_GetWord(a_CMBuffer, a_Offset) Then
                            Warn($"CM Page Header Error: Offset = { .Offset} expected {g_GetWord(a_CMBuffer, a_Offset)}")
                        End If
                    End With
                    a_Offset += 4
                End If
            End While
            '===================== Parse the cartridge manufacturers page =====================
            a_Key = 1
            If g_CM(gtype.page, a_Key, False) IsNot Nothing Then
                With CType(g_CM(gtype.page, a_Key, False), Page)
                    If .Offset >= 0 AndAlso .Length >= 0 Then
                        a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                        With CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg)
                            .TapeVendor = getstr(a_Buffer, 4, 8)
                            .CartridgeSN = getstr(a_Buffer, 12, 10)
                            .CartridgeType = g_GetWord(a_Buffer, 22)
                            .MfgDate = getstr(a_Buffer, 24, 8) ' format YYYYMMDD
                            .TapeLength = g_GetWord(a_Buffer, 32) ' in .25 metre increments
                            .MediaCode = g_GetWord(a_Buffer, 46)
                            Dim a_PageRevision As Byte = a_Buffer(0)
                            Dim a_Particles As Byte = a_Buffer(42)
                            If a_PageRevision >= &H40 Then
                                If a_Particles And &HF Then
                                    .ParticleType = Cartridge_mfg.particle.BaFe
                                Else
                                    .ParticleType = Cartridge_mfg.particle.MP
                                End If
                                If a_Particles And &HF0 = &H10 Then
                                    .SubstrateType = Cartridge_mfg.substrate.SPALTAN
                                Else
                                    .SubstrateType = Cartridge_mfg.substrate.PEN
                                End If
                            Else
                                If a_Buffer(42) Then
                                    .ParticleType = Cartridge_mfg.particle.BaFe
                                Else
                                    .ParticleType = Cartridge_mfg.particle.MP
                                End If
                            End If
                            If (.CartridgeType >> 15 And &H1) = 1 Then
                                .Format = "Cleaning Tape"
                            ElseIf .CartridgeType = 1 Then
                                .Format = "LTO-1"
                                a_NWraps = 48
                                a_SetsPerWrap = 5500
                                a_TapeDirLength = 16
                            ElseIf .CartridgeType = 2 Then
                                .Format = "LTO-2"
                                a_NWraps = 64
                                a_SetsPerWrap = 8200
                                a_TapeDirLength = 28
                            Else 'LTO3+ supports WORM, so need to mask off the 'WORM' bit in the upper byte
                                Select Case .CartridgeType And &HFF
                                    Case 4
                                        .Format = "LTO-3"
                                        a_NWraps = 44
                                        a_SetsPerWrap = 6000
                                        a_TapeDirLength = 32
                                    Case 8
                                        .Format = "LTO-4"
                                        a_NWraps = 56
                                        a_SetsPerWrap = 9500
                                        a_TapeDirLength = 32
                                    Case 16
                                        .Format = "LTO-5"
                                        a_NWraps = 80
                                        a_SetsPerWrap = 7800
                                        a_TapeDirLength = 32
                                    Case 32
                                        .Format = "LTO-6"
                                        a_NWraps = 136
                                        a_SetsPerWrap = 7805
                                        a_TapeDirLength = 32
                                    Case 64
                                        .Format = "LTO-7"
                                        a_NWraps = 112
                                        a_SetsPerWrap = 10950
                                        a_TapeDirLength = 32
                                    Case 128
                                        .Format = "LTO-8"
                                        a_NWraps = 208
                                        a_SetsPerWrap = 11660
                                        a_TapeDirLength = 32
                                    Case 129
                                        .Format = "LTO-9"
                                        a_NWraps = 280
                                        a_SetsPerWrap = 6770
                                        a_TapeDirLength = 32
                                End Select
                                If (.CartridgeType >> 13 And 1) = 1 Then
                                    .Format &= " WORM"
                                End If
                            End If
                        End With
                    End If
                End With
            End If

            '===================== Parse the media manufacturers page =====================
            a_Key = 2
            If g_CM(gtype.page, a_Key, False) IsNot Nothing Then
                With CType(g_CM(gtype.page, a_Key, False), Page)
                    If .Offset >= 0 AndAlso .Length >= 0 Then
                        a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                        With CType(g_CM(gtype.media_mfg), Media_mfg)
                            .MediaMfgDate = getstr(a_Buffer, 4, 8)
                            .MediaVendor = getstr(a_Buffer, 12, 8)
                            ' check the MediaMfgDate for servo band ID
                            Try
                                If CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).Format.Contains("LTO-8") Then
                                    If .MediaMfgDate.StartsWith("22") Then
                                        CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).Servo_Band_ID = Cartridge_mfg.svbid.legacy_UDIM
                                    ElseIf .MediaVendor.StartsWith(">>") Then
                                        CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).Servo_Band_ID = Cartridge_mfg.svbid.non_UDIM
                                    End If
                                End If
                            Catch ex As Exception
                            End Try
                        End With
                    End If
                End With
            End If

            '===================== Parse the usage pages =====================
            ' usage information page definition Is changed For LTO5
            ' -- And the drive serial number field grew longer, so we'll use
            '    a variable For the length rather than assuming 10 chars.
            Dim driveSNlength As Integer
            With CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg)
                If .IsLTO5Plus Then
                    at_Offset = {32, 36, 44, 52, 56, 60, 62, 64, 66, 80}
                    driveSNlength = 16
                Else
                    at_Offset = {24, 28, 36, 44, 48, 52, 54, 56, 58}
                    driveSNlength = 10
                End If
            End With
            ' parse the 4 usage information pages and mechanism related sub-pages
            a_Length = &H40
            If g_CM(gtype.page, &H108, False) IsNot Nothing Then
                With CType(g_CM(gtype.page, &H108, False), Page)
                    If .Length >= 0 Then a_Length = .Length
                End With
            End If
            a_Err = 0
            ' The Mechanism Related Information (Page 106) is vendor specific so we
            ' need To determine which Vendor wrote it before decoding its contents.
            Dim MechRelatedInfoVendorID As String = ""
            If g_CM(gtype.page, &H106, False) IsNot Nothing Then
                With CType(g_CM(gtype.page, &H106), Page)
                    If .Offset >= 0 AndAlso .Length >= 0 Then
                        MechRelatedInfoVendorID = Text.Encoding.ASCII.GetString(substr(a_CMBuffer, .Offset + 4, 8)).TrimEnd
                    End If
                End With
            End If
            For a_Index = 0 To 3
                ' usage info page ID's 0x108-0x10B correspond to usage info pages 1-4 and mechanism related sub-pages 1-4
                a_Key = a_Index + &H108
                If g_CM(gtype.page, a_Key, False) IsNot Nothing AndAlso g_CM(gtype.page, &H106, False) IsNot Nothing Then
                    Dim valid As Boolean = True
                    With CType(g_CM(gtype.page, a_Key, False), Page)
                        If .Offset < 0 OrElse .Length < 0 Then
                            valid = False
                        End If
                    End With
                    With CType(g_CM(gtype.page, &H106, False), Page)
                        If .Offset < 0 OrElse .Length < 0 Then
                            valid = False
                        End If
                    End With
                    If valid Then
                        ' get the usage info page
                        a_Buffer = substr(a_CMBuffer, CType(g_CM(gtype.page, a_Key, False), Page).Offset, a_Length)
                        ' append the corresponding mechanism related sub-page
                        Dim b_Buffer As Byte() = substr(a_CMBuffer, CType(g_CM(gtype.page, &H106, False), Page).Offset + 12 + 64 * a_Index, 64)
                        a_Buffer = a_Buffer.Concat(b_Buffer).ToArray()
                        With a_Usage(a_Index, True)
                            .data0 = a_Buffer
                            .data1 = g_GetDWord(a_Buffer, at_Offset(0))
                        End With
                    Else
                        a_Err = 1
                    End If
                Else
                    a_Err = 1
                End If
            Next

            If a_Err = 0 Then
                'reverse sort the pages by tape thread count
                a_UsageData.Sort(New Comparison(Of UsagePage)(Function(a As UsagePage, b As UsagePage) As Integer
                                                                  Return b.data1.CompareTo(a.data1)
                                                              End Function))
                For i As Integer = 0 To a_UsageData.Count - 1
                    a_UsageData(i).index = i
                Next
                ' parse the sorted data structure and populate the return array
                For a_Index = 0 To 2
                    With CType(g_CM(gtype.usage, a_Index), Usage)
                        .PageID = g_GetWord(a_Usage(a_Index, False).data0, 0)
                        ' parameters need valid data for the current load in order to calculate (previous load fields are set to 0, per CM spec, so difference calculations will be correct)
                        If g_GetWord(a_Usage(a_Index, False).data0, 12) <> 0 Then
                            'Remove trailing whitespace
                            .DrvSN = Text.Encoding.ASCII.GetString(substr(a_Usage(a_Index, False).data0, 12, driveSNlength)).TrimEnd()
                            'IBM-based drives have two leading chars
                            If .DrvSN.Length > 10 Then
                                'Just the last ten chars
                                .DrvSN = .DrvSN.Substring(.DrvSN.Length - 10)
                            End If
                        Else
                            .DrvSN = ""
                        End If
                        .ThreadCount = g_GetDWord(a_Usage(a_Index, False).data0, at_Offset(0))
                        .SetsWritten = g_GetInt64(a_Usage(a_Index, False).data0, at_Offset(1)) - g_GetInt64(a_Usage(a_Index + 1, False).data0, at_Offset(1))
                        .SetsRead = g_GetInt64(a_Usage(a_Index, False).data0, at_Offset(2)) - g_GetInt64(a_Usage(a_Index + 1, False).data0, at_Offset(2))
                        .TotalSets = g_GetInt64(a_Usage(a_Index, False).data0, at_Offset(1)) + g_GetInt64(a_Usage(a_Index, False).data0, at_Offset(2))
                        .WriteRetries = g_GetDWord(a_Usage(a_Index, False).data0, at_Offset(3)) - g_GetDWord(a_Usage(a_Index + 1, False).data0, at_Offset(3))
                        .ReadRetries = g_GetDWord(a_Usage(a_Index, False).data0, at_Offset(4)) - g_GetDWord(a_Usage(a_Index + 1, False).data0, at_Offset(4))
                        .UnRecovWrites = g_GetWord(a_Usage(a_Index, False).data0, at_Offset(5)) - g_GetWord(a_Usage(a_Index + 1, False).data0, at_Offset(5))
                        .UnRecovReads = g_GetWord(a_Usage(a_Index, False).data0, at_Offset(6)) - g_GetWord(a_Usage(a_Index + 1, False).data0, at_Offset(6))
                        .SuspendedWrites = g_GetWord(a_Usage(a_Index, False).data0, at_Offset(7)) - g_GetWord(a_Usage(a_Index + 1, False).data0, at_Offset(7))
                        .FatalSusWrites = g_GetWord(a_Usage(a_Index, False).data0, at_Offset(8)) - g_GetWord(a_Usage(a_Index + 1, False).data0, at_Offset(8))

                        'LTO5 only (doesn't look like early LTO5 drives update this area, so check if there is valid temperature info first)
                        If CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).IsLTO5Plus AndAlso a_Usage(a_Index, False).data0(76) > 0 Then
                            .SuspendedAppendWrites = g_GetWord(a_Usage(a_Index, False).data0, 28) - g_GetWord(a_Usage(a_Index + 1, False).data0, 28)
                            .LP3Passes = g_GetDWord(a_Usage(a_Index, False).data0, 68) - g_GetDWord(a_Usage(a_Index + 1, False).data0, 68)
                            .MidpointPasses = g_GetDWord(a_Usage(a_Index, False).data0, 72) - g_GetDWord(a_Usage(a_Index + 1, False).data0, 72)
                            .MaxTapeTemp = a_Usage(a_Index, False).data0(76)
                        End If
                        ' Because the code above helpfully (?) appended the mech-related subpage to each usage page,
                        '  we can parse the mech-related data at the same time. But only If it's HP data..
                        If MechRelatedInfoVendorID.Contains("HP") Then
                            .CCQWriteFails = g_GetInt64(a_Usage(a_Index, False).data0, a_Length) - g_GetInt64(a_Usage(a_Index + 1, False).data0, a_Length)
                            .C2RecovErrors = g_GetDWord(a_Usage(a_Index, False).data0, a_Length + 8) - g_GetDWord(a_Usage(a_Index + 1, False).data0, a_Length + 8)
                            .DirectionChanges = g_GetDWord(a_Usage(a_Index, False).data0, a_Length + 24) - g_GetDWord(a_Usage(a_Index + 1, False).data0, a_Length + 24)
                            .TapePullingTime = g_GetDWord(a_Usage(a_Index, False).data0, a_Length + 28) - g_GetDWord(a_Usage(a_Index + 1, False).data0, a_Length + 28)
                            .TapeMetresPulled = g_GetDWord(a_Usage(a_Index, False).data0, a_Length + 32)
                            .Repositions = g_GetDWord(a_Usage(a_Index, False).data0, a_Length + 36) - g_GetDWord(a_Usage(a_Index + 1, False).data0, a_Length + 36)
                            .TotalLoadUnloads = g_GetDWord(a_Usage(a_Index, False).data0, a_Length + 40)
                            .StreamFails = g_GetDWord(a_Usage(a_Index, False).data0, a_Length + 44) - g_GetDWord(a_Usage(a_Index + 1, False).data0, a_Length + 44)

                            'for some reason, temperature doesn't always get recorded(??)
                            If g_GetWord(a_Usage(a_Index, False).data0, a_Length + 48) > 0 Then
                                .MaxDriveTemp = g_GetWord(a_Usage(a_Index, False).data0, a_Length + 48) / 256
                            End If
                            If g_GetWord(a_Usage(a_Index, False).data0, a_Length + 50) > 0 Then
                                .MinDriveTemp = g_GetWord(a_Usage(a_Index, False).data0, a_Length + 50) / 256
                            End If
                            If .CCQWriteFails < 0 Then .CCQWriteFails = 0
                            If .C2RecovErrors < 0 Then .C2RecovErrors = 0
                            If .DirectionChanges < 0 Then .DirectionChanges = 0
                            If .TapePullingTime < 0 Then .TapePullingTime = 0
                            If .TapeMetresPulled < 0 Then .TapeMetresPulled = 0
                            If .Repositions < 0 Then .Repositions = 0
                            If .StreamFails < 0 Then .StreamFails = 0
                        Else
                            .CCQWriteFails = 0
                            .C2RecovErrors = 0
                        End If
                        .LifeSetsWritten = g_GetInt64(a_Usage(a_Index, False).data0, at_Offset(1))
                        .LifeSetsRead = g_GetInt64(a_Usage(a_Index, False).data0, at_Offset(2))
                        .LifeWriteRetries = g_GetDWord(a_Usage(a_Index, False).data0, at_Offset(3))
                        .LifeReadRetries = g_GetDWord(a_Usage(a_Index, False).data0, at_Offset(4))
                        .LifeUnRecovWrites = g_GetWord(a_Usage(a_Index, False).data0, at_Offset(5))
                        .LifeUnRecovReads = g_GetWord(a_Usage(a_Index, False).data0, at_Offset(6))
                        .LifeSuspendedWrites = g_GetWord(a_Usage(a_Index, False).data0, at_Offset(7))
                        .LifeFatalSuspWrites = g_GetWord(a_Usage(a_Index, False).data0, at_Offset(8))
                        If at_Offset.Length >= 10 Then .LifeTapeMetresPulled = g_GetDWord(a_Usage(a_Index, False).data0, at_Offset(9))
                        If CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).IsLTO5Plus Then
                            .LifeSuspAppendWrites = g_GetWord(a_Usage(a_Index, False).data0, 28)
                            .LifeLP3Passes = g_GetDWord(a_Usage(a_Index, False).data0, 68)
                            .LifeMidpointPasses = g_GetDWord(a_Usage(a_Index, False).data0, 72)
                        End If
                        'the following should always be >= 0 (it's not always, as confirmed by looking at the CM buffer (drv fw issue?)
                        If .ThreadCount < 0 Then .ThreadCount = 0
                        If .SetsWritten < 0 Then .SetsWritten = 0
                        If .SetsRead < 0 Then .SetsRead = 0
                        If .WriteRetries < 0 Then .WriteRetries = 0
                        If .ReadRetries < 0 Then .ReadRetries = 0
                        If .UnRecovWrites < 0 Then .UnRecovWrites = 0
                        If .UnRecovReads < 0 Then .UnRecovReads = 0
                        If .SuspendedWrites < 0 Then .SuspendedWrites = 0
                        If .FatalSusWrites < 0 Then .FatalSusWrites = 0
                        If a_Index = 0 Then
                            g_TPM = .TapeMetresPulled
                        End If
                    End With
                Next
            End If

            '===================== Parse the "tape status and tape alert flags" page =====================
            a_Key = &H105
            If g_CM(gtype.page, a_Key, False) IsNot Nothing Then
                With CType(g_CM(gtype.page, a_Key, False), Page)
                    If .Offset >= 0 AndAlso .Length >= 0 Then
                        a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                        With CType(g_CM(gtype.status), TapeStatus)
                            .ThreadCount = g_GetDWord(a_Buffer, 12)
                            'check if any encrypted data (LTO4+ only) by checking if the First Encrypted Logical Object field is all 1's
                            If CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).IsLTO4Plus Then
                                If g_GetWord(a_Buffer, 22) = &HFFFF AndAlso g_GetWord(a_Buffer, 24) = &HFFFF AndAlso g_GetWord(a_Buffer, 26) = &HFFFF Then
                                    .EncryptedData = 0
                                Else
                                    .EncryptedData = 1
                                End If
                            End If

                            'for cleaning tapes, bytes 26-27 is the last location used on the tape in quarter-metres
                            If CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("Clean") Then
                                .LastLocation = g_GetWord(a_Buffer, 26)
                            End If
                        End With
                    End If


                End With
            End If
            'for cleaning tapes, check if expired
            If CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("Clean") Then
                'cleaning length depends on drive type
                If False Then
                    'LTO1
                    a_CleanLength = 18.5
                Else
                    a_CleanLength = 5.5
                End If
                If CType(g_CM(gtype.status, False), TapeStatus).LastLocation >= 0 AndAlso CType(g_CM(gtype.cartridge_mfg, False), Cartridge_mfg).TapeLength >= 0 Then
                    a_CleansRemaining = CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).TapeLength / 4 - 11
                    a_CleansRemaining -= CType(g_CM(gtype.status, createNew:=False), TapeStatus).LastLocation / 4
                    a_CleansRemaining /= a_CleanLength
                    If a_CleansRemaining <= 0 Then
                        CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format &= " (expired)"
                    End If
                End If
            End If

            '===================== Parse the initialisation page =====================
            a_Key = &H101
            If g_CM(gtype.page, a_Key, False) IsNot Nothing Then
                With CType(g_CM(gtype.page, a_Key, False), Page)
                    If .Offset >= 0 AndAlso .Length >= 0 Then
                        a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                        'initialisation page definition is changed for LTO5+
                        If CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).IsLTO5Plus Then
                            at_Offset = {28, 32, 40, 48}
                        Else
                            at_Offset = {22, 24, 32, 40}
                        End If
                        With CType(g_CM(gtype.initialisation), Initialisation)
                            .LP1 = g_GetDWord(a_Buffer, at_Offset(1))
                            .LP2 = g_GetDWord(a_Buffer, at_Offset(2))
                            .LP3 = g_GetDWord(a_Buffer, at_Offset(3))
                        End With
                    End If
                End With
            End If

            '#===================== Parse the EOD page for Partition 0-3 =====================
            For pnum As Integer = 0 To 3
                a_Key = {&H104, &H10E, &H10F, &H110}(pnum)
                If g_CM(gtype.page, a_Key, False) IsNot Nothing Then
                    With CType(g_CM(gtype.page, a_Key, False), Page)
                        If .Offset >= 0 AndAlso .Length >= 0 Then
                            a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                            With CType(g_CM(gtype.EOD, pnum), EOD)
                                .Dataset = g_GetDWord(a_Buffer, 24)
                                .WrapNumber = g_GetDWord(a_Buffer, 28)
                                .Validity = g_GetWord(a_Buffer, 32)
                                .PhysicalPosition = g_GetDWord(a_Buffer, 36)
                            End With
                        End If
                    End With
                End If
            Next

            '===================== Parse the cartridge content page (if it exists!) =====================
            If CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).IsLTO5Plus Then
                a_Key = &H10D
                If g_CM(gtype.page, a_Key, False) IsNot Nothing Then
                    With CType(g_CM(gtype.page, a_Key, False), Page)
                        If .Offset >= 0 AndAlso .Length >= 0 Then
                            a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                            With CType(g_CM(gtype.cartridge_content), CartridgeContent)
                                .Drive_Id = Encoding.ASCII.GetString(substr(a_Buffer, 12, 16)).TrimEnd
                                .Cartridge_Content = g_GetWord(a_Buffer, 28)
                                .PartitionedCartridge = a_Buffer(28) >> 3 And 1
                                If CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).IsLTO7Plus Then
                                    .Type_M_Cartridge = a_Buffer(28) And 1
                                End If
                                If CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-5") Then
                                    .Drive_Firmware_Id = Encoding.ASCII.GetString(substr(a_Buffer, 48, 4))
                                Else
                                    .Drive_Firmware_Id = Encoding.ASCII.GetString(substr(a_Buffer, 52, 4))
                                End If
                                If CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-7") AndAlso .Type_M_Cartridge Then
                                    CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format = "LTO-7 Type M"
                                    a_NWraps = 168
                                End If
                            End With
                        End If
                    End With
                End If
            End If

            '===================== Parse the Tape Directory page =====================
            a_Key = &H103
            If g_CM(gtype.page, a_Key, False) IsNot Nothing AndAlso g_CM(gtype.EOD, 0, False) IsNot Nothing Then
                With CType(g_CM(gtype.page, a_Key, False), Page)
                    If .Offset >= 0 AndAlso .Length >= 0 AndAlso CType(g_CM(gtype.EOD, 0, False), EOD).Validity Then
                        a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                        With CType(g_CM(gtype.tape_directory, createNew:=True), TapeDirectory)
                            If CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).IsLTO6Plus Then
                                a_HdrLength = 48
                                a_TapeWritePassPartition = g_GetDWord(a_Buffer, 4)
                                .FID_Tape_Write_Pass_Partition_0 = a_TapeWritePassPartition
                                a_TapeWritePassPartition = g_GetDWord(a_Buffer, 8)
                                .FID_Tape_Write_Pass_Partition_1 = a_TapeWritePassPartition
                                a_TapeWritePassPartition = g_GetDWord(a_Buffer, 12)
                                .FID_Tape_Write_Pass_Partition_2 = a_TapeWritePassPartition
                                a_TapeWritePassPartition = g_GetDWord(a_Buffer, 16)
                                .FID_Tape_Write_Pass_Partition_3 = a_TapeWritePassPartition
                                a_OutputStr = $"{"WritePass".PadRight(12) _
                                    }{"DatasetID".PadRight(14) _
                                    }{"HOW RecCnt".PadRight(14) _
                                    }{"EOW RecCnt".PadRight(14) _
                                    }{"HOW FMCnt".PadRight(14) _
                                    }{"EOW FMCnt".PadRight(14) _
                                    }{"FM Map".PadRight(14) _
                                    }{"CRC".PadRight(14)}"
                                .Wrap = a_OutputStr
                            ElseIf CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).IsLTO4Plus Then
                                a_HdrLength = 16
                                a_TapeWritePassPartition = g_GetDWord(a_Buffer, 4)
                                .FID_Tape_Write_Pass_Partition_0 = a_TapeWritePassPartition
                                a_TapeWritePassPartition = g_GetDWord(a_Buffer, 8)
                                .FID_Tape_Write_Pass_Partition_1 = a_TapeWritePassPartition
                                a_OutputStr = $"{"WritePass".PadRight(12) _
                                    }{"DatasetID".PadRight(14) _
                                    }{"HOW RecCnt".PadRight(14) _
                                    }{"EOW RecCnt".PadRight(14) _
                                    }{"HOW FMCnt".PadRight(14) _
                                    }{"EOW FMCnt".PadRight(14) _
                                    }{"FM Map".PadRight(14) _
                                    }{"CRC".PadRight(14)}"
                                .Wrap = a_OutputStr
                            ElseIf CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-3") Then
                                a_HdrLength = 16
                                a_OutputStr = $"{"WritePass".PadRight(12) _
                                    }{"DatasetID".PadRight(14) _
                                    }{"HOW RecCnt".PadRight(14) _
                                    }{"EOW RecCnt".PadRight(14) _
                                    }{"HOW FMCnt".PadRight(14) _
                                    }{"EOW FMCnt".PadRight(14) _
                                    }{"FM Map".PadRight(14) _
                                    }{"CRC".PadRight(14)}"
                                .Wrap = a_OutputStr
                            ElseIf CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-2") Then
                                a_HdrLength = 16
                                a_OutputStr = $"{"WritePass".PadRight(12) _
                                    }{"DatasetID".PadRight(14) _
                                    }{"HOW RecCnt".PadRight(14) _
                                    }{"EOW RecCnt".PadRight(14) _
                                    }{"HOW FMCnt".PadRight(14) _
                                    }{"EOW FMCnt".PadRight(14) _
                                    }{"CRC".PadRight(14)}"
                                .Wrap = a_OutputStr
                            ElseIf CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-1") Then
                                a_HdrLength = 16
                                a_OutputStr = $"{"WritePass".PadRight(12) _
                                    }{"DatasetID".PadRight(14) _
                                    }{"RecordCount".PadRight(14) _
                                    }{"FilemarkCount".PadRight(14) _
                                    }{"CRC".PadRight(14)}"
                                .Wrap = a_OutputStr
                            Else
                                a_HdrLength = 16
                            End If
                            PublishTapeDirectoryPage(a_Buffer)
                            a_LastID = 0

                            ' Get number of partitions.

                            a_NumPartitions = New List(Of EOD)
                            For Each eod As EOD In PartitionEOD
                                a_NumPartitions.Add(eod)
                            Next
                            .CapacityLoss = New List(Of Double)
                            For a_Index = 0 To a_NWraps - 1
                                a_SetID = g_GetDWord(a_Buffer, a_HdrLength + a_TapeDirLength * a_Index + 4)
                                If a_SetID = UNUSED_WRAP_IDENTIFIER Then
                                    .CapacityLoss.Add(-1)
                                    Continue For
                                ElseIf a_SetID = GUARD_WRAP_IDENTIFIER Then
                                    .CapacityLoss.Add(-3)
                                    Continue For
                                ElseIf a_SetID = 0 Then
                                    .CapacityLoss.Add(0)
                                    Continue For
                                Else
                                    a_set = 0
                                    For a_PartitionKey = 0 To a_NumPartitions.Count - 1
                                        If CType(g_CM(gtype.EOD, a_PartitionKey, False), EOD).Validity AndAlso CType(g_CM(gtype.EOD, a_PartitionKey, False), EOD).WrapNumber = a_Index Then
                                            .CapacityLoss.Add(-2)
                                            'partially written wrap (EOD Wrap).
                                            a_set = 1
                                            Exit For
                                        End If
                                    Next
                                    If a_set = 0 Then
                                        .CapacityLoss.Add(Math.Max(0, 100 * (1 - (a_SetID - a_LastID) / a_SetsPerWrap)))
                                        a_LastID = a_SetID
                                    End If
                                End If
                            Next
                            a_LastID = 0
                            Dim a As Integer
                            Dim DatasetsOnWrap_Index As Integer
                            Dim TapeDirectoryEntry As Byte()
                            For a_Index = 0 To a_NWraps - 1
                                DatasetsOnWrap_Index = a_Index
                                TapeDirectoryEntry = substr(a_Buffer, a_HdrLength + a_TapeDirLength * a_Index, a_TapeDirLength)
                                a = g_GetDWord(TapeDirectoryEntry, 4)
                                If a = UNUSED_WRAP_IDENTIFIER OrElse a = GUARD_WRAP_IDENTIFIER Then
                                    a = 0
                                    .DatasetsOnWrap(DatasetsOnWrap_Index).Data = a
                                Else
                                    .DatasetsOnWrap(DatasetsOnWrap_Index).Data = a - a_LastID
                                End If
                                a_LastID = a
                            Next
                        End With
                    End If
                End With
            End If

            'if partitioned cart and LTO5 or later
            If CType(g_CM(gtype.cartridge_content), CartridgeContent).PartitionedCartridge Then
                Dim pKeyList() As Integer = {}
                If CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).Format.Contains("LTO-5") Then
                    pKeyList = {&H10E}
                ElseIf CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).IsLTO6Plus Then
                    pKeyList = {&H10E, &H10F, &H110}
                End If
                For pNum As Integer = 1 To pKeyList.Count
                    a_Key = pKeyList(pNum - 1)
                    If g_CM(gtype.page, a_Key, False) IsNot Nothing Then
                        With CType(g_CM(gtype.page, a_Key, False), Page)
                            If .Length >= 0 AndAlso .Offset >= 0 Then
                                a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                                With CType(g_CM(gtype.EOD, pNum), EOD)
                                    .Dataset = g_GetDWord(a_Buffer, 24)
                                    .WrapNumber = g_GetDWord(a_Buffer, 28)
                                    .Validity = g_GetWord(a_Buffer, 32)
                                    .PhysicalPosition = g_GetDWord(a_Buffer, 36)
                                End With
                            End If
                        End With
                    End If
                Next
            End If

            '===================== Parse the Suspended Append Writes page =====================
            a_Key = &H107
            Dim a_NoSlot As Integer
            a_Index = 0
            a_Offset = 0
            If g_CM(gtype.page, a_Key, False) IsNot Nothing Then
                With CType(g_CM(gtype.page, a_Key, False), Page)
                    If .Offset >= 0 AndAlso .Length >= 0 Then
                        a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                        If Not CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).IsLTO5Plus Then
                            ' LTO-1 through LTO-4 tapes have 14 slots (0-13)
                            a_NoSlot = 14
                        ElseIf CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-5") Then
                            'LTO-5 tape has 22 slots (0-21)
                            a_NoSlot = 22
                        ElseIf CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-6") Then
                            'LTO-6 tape has 38 slots (0-37)
                            a_NoSlot = 38
                        ElseIf CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-7") Then
                            'LTO-7 tape has 38 slots (0-37)
                            a_NoSlot = 38
                        ElseIf CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-8") Then
                            'LTO-8 tape has 38 slots (0-37), including Type M
                            a_NoSlot = 38
                        ElseIf CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg).Format.Contains("LTO-9") Then
                            'LTO-9 tape has 38 slots (0-37)
                            a_NoSlot = 38
                        End If

                        'Single partition, look for available slots, index 0 - 13
                        For a_Index = 0 To a_NoSlot - 1
                            With CType(g_CM(gtype.suspended_writes), SuspendWrite)
                                .DataSetID(a_Index).Value = g_GetDWord(a_Buffer, a_Offset + 8)
                                .WTapePass(a_Index).Value = g_GetDWord(a_Buffer, a_Offset + 12)
                                a_Offset += 8
                            End With
                        Next
                    End If
                End With
            End If

            '===================== Parse the Tape Control page (LTO9 Only)=====================
            'a_Key = &H183
            'tapectrl
            '    TDS Data Version           BYTE        a_Buffer(4)
            '    Temperature                BYTE        a_Buffer(5)
            '    Humidity                   BYTE        a_Buffer(6)
            '    Characterization needed    BYTE        a_Buffer(7)
            '    Timestamp                  DWORD       a_Buffer(16)
            '    DriveSN                    Byte(12)    a_Buffer(34)

            '===================== Parse the Application Specific page =====================
            ' See MAMAttribute
            a_Key = &H200
            If g_CM(gtype.page, a_Key, False) IsNot Nothing Then
                With CType(g_CM(gtype.page, a_Key, False), Page)
                    If .Offset >= 0 AndAlso .Length >= 0 Then
                        a_Buffer = substr(a_CMBuffer, .Offset, .Length)
                        With CType(g_CM(gtype.application_specific), ApplicationSpecific)
                            If getstr(a_Buffer, 4, 6).Equals("MAM001") OrElse getstr(a_Buffer, 4, 6).Equals("MAM002") Then
                                a_Index = 10
                                While a_Index < a_Buffer.Length
                                    a_AttributeID = g_GetWord(a_Buffer, a_Index)
                                    ' Attribute length definition changed from "MAM001" To "MAM002".  "MAM001" has 2 bytes For attribute length, whereas
                                    ' "MAM002" has 1.5 bytes.  Using 1.5 bytes For both cases, As the attribute length Is always small compared To what
                                    ' can be represented by 1.5 bytes (QXCR1001109840).
                                    a_AttributeLength = g_GetWord(a_Buffer, a_Index + 2) And &HFFF
                                    If a_AttributeID = &HFFF OrElse a_AttributeLength = 0 Then Exit While
                                    ' barcode
                                    If a_AttributeID = &H806 Then
                                        a_Barcode = getstr(a_Buffer, a_Index + 4, a_AttributeLength).TrimEnd()
                                        If a_Barcode <> "" Then
                                            .Barcode = a_Barcode
                                            g_Barcode = a_Barcode
                                        End If
                                    End If

                                    ' LTFS vendor
                                    If a_AttributeID = &H800 Then
                                        .Application_vendor = getstr(a_Buffer, a_Index + 4, a_AttributeLength).TrimEnd()
                                    End If

                                    ' LTFS formatted
                                    If a_AttributeID = &H801 Then
                                        .Application_name = getstr(a_Buffer, a_Index + 4, a_AttributeLength).TrimEnd()
                                    End If

                                    ' LTFS version
                                    If a_AttributeID = &H802 Then
                                        .Application_version = getstr(a_Buffer, a_Index + 4, a_AttributeLength).TrimEnd()
                                    End If

                                    a_Index += 4 + a_AttributeLength
                                End While
                            End If
                        End With
                    End If
                End With
            End If
            Return True
        End Function
        Public Sub PublishTapeDirectoryPage(ByRef Buffer As Byte())
            Dim a_WrapIndex As Integer = 0
            Dim a_WritePass As Integer = 0
            Dim a_DataSetID As Integer = 0
            Dim a_HOW_RecCnt As Integer = 0
            Dim a_EOW_RecCnt As Integer = 0
            Dim a_HOW_FMCnt As Integer = 0
            Dim a_EOW_FMCnt As Integer = 0
            Dim a_FM_MAP As Integer = 0
            Dim a_CRC As Integer = 0
            Dim a_WrapsInDrive As Integer = 0
            Dim a_RecordCount As Integer = 0
            Dim a_FilemarkCount As Integer = 0

            Dim a_EvenDataSetID As Integer = 0
            Dim a_EvenRecordCount As Integer = 0
            Dim a_EvenFileMarkCount As Integer = 0
            Dim a_EvenCRC As Integer = 0
            Dim a_OddDataSetID As Integer = 0
            Dim a_OddRecordCount As Integer = 0
            Dim a_OddFileMarkCount As Integer = 0
            Dim a_OddCRC As Integer = 0
            Dim a_HdrLength As Integer = 0
            Dim a_OutputStr As String = ""

            With CType(g_CM(gtype.cartridge_mfg, createNew:=False), Cartridge_mfg)
                If .Format.Contains("LTO-1") Then
                    a_WrapsInDrive = 48
                    a_HdrLength = 16
                ElseIf .Format.Contains("LTO-2") Then
                    a_WrapsInDrive = 64
                    a_HdrLength = 16
                ElseIf .Format.Contains("LTO-3") Then
                    a_WrapsInDrive = 44
                    a_HdrLength = 16
                ElseIf .Format.Contains("LTO-4") Then
                    a_WrapsInDrive = 56
                    a_HdrLength = 16
                ElseIf .Format.Contains("LTO-5") Then
                    a_WrapsInDrive = 80
                    a_HdrLength = 16
                ElseIf .Format.Contains("LTO-6") Then
                    a_WrapsInDrive = 136
                    a_HdrLength = 48
                ElseIf .Format.Contains("LTO-7") Then
                    If .Format.Contains("Type M") Then
                        a_WrapsInDrive = 168
                    Else
                        a_WrapsInDrive = 112
                    End If
                    a_HdrLength = 48
                ElseIf .Format.Contains("LTO-8") Then
                    a_WrapsInDrive = 208
                    a_HdrLength = 48
                ElseIf .Format.Contains("LTO-9") Then
                    a_WrapsInDrive = 280
                    a_HdrLength = 48
                End If

                If .Format.Contains("LTO-2") Then
                    For a_WrapIndex = 0 To a_WrapsInDrive - 1
                        a_WritePass = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_DataSetID = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_HOW_RecCnt = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_EOW_RecCnt = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_HOW_FMCnt = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_EOW_FMCnt = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_CRC = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_OutputStr = $"{a_WritePass.ToString().PadRight(12) _
                            }{a_DataSetID.ToString().PadRight(12) _
                            }{a_HOW_RecCnt.ToString().PadRight(12) _
                            }{a_EOW_RecCnt.ToString().PadRight(12) _
                            }{a_HOW_FMCnt.ToString().PadRight(12) _
                            }{a_EOW_FMCnt.ToString().PadRight(12) _
                            }{a_CRC.ToString().PadRight(12) _
                            }"

                        With CType(g_CM(gtype.tape_directory), TapeDirectory).WrapEntry(a_WrapIndex)
                            .Content = a_OutputStr
                            .RawData = {a_DataSetID, a_HOW_RecCnt, a_EOW_RecCnt, a_HOW_FMCnt, a_EOW_FMCnt, a_CRC}
                            .RecCount = a_HOW_RecCnt + a_EOW_RecCnt
                            .FileMarkCount = a_HOW_FMCnt + a_EOW_FMCnt
                        End With
                    Next
                ElseIf .IsLTO3Plus Then
                    For a_WrapIndex = 0 To a_WrapsInDrive - 1
                        a_WritePass = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_DataSetID = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_HOW_RecCnt = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_EOW_RecCnt = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_HOW_FMCnt = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_EOW_FMCnt = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_FM_MAP = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_CRC = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_OutputStr = $"{a_WritePass.ToString().PadRight(12) _
                            }{a_DataSetID.ToString().PadRight(12) _
                            }{a_HOW_RecCnt.ToString().PadRight(12) _
                            }{a_EOW_RecCnt.ToString().PadRight(12) _
                            }{a_HOW_FMCnt.ToString().PadRight(12) _
                            }{a_EOW_FMCnt.ToString().PadRight(12) _
                            }{a_FM_MAP.ToString().PadRight(12) _
                            }{a_CRC.ToString().PadRight(12) _
                            }"
                        With CType(g_CM(gtype.tape_directory), TapeDirectory).WrapEntry(a_WrapIndex)
                            .Content = a_OutputStr
                            .RawData = {a_DataSetID, a_HOW_RecCnt, a_EOW_RecCnt, a_HOW_FMCnt, a_EOW_FMCnt, a_FM_MAP, a_CRC}
                            .RecCount = a_HOW_RecCnt + a_EOW_RecCnt
                            .FileMarkCount = a_HOW_FMCnt + a_EOW_FMCnt
                        End With
                    Next
                ElseIf .Format.Contains("LTO-1") Then
                    a_HdrLength = 16
                    a_WrapsInDrive = 48
                    For a_WrapIndex = 0 To a_WrapsInDrive - 1
                        a_EvenDataSetID = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_EvenRecordCount = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_EvenFileMarkCount = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_EvenCRC = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_OddDataSetID = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_OddRecordCount = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_OddFileMarkCount = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4
                        a_OddCRC = g_GetDWord(Buffer, a_HdrLength)
                        a_HdrLength += 4

                        a_OutputStr = $"{a_EvenDataSetID.ToString().PadRight(12) _
                            }{a_EvenRecordCount.ToString().PadRight(12) _
                            }{a_EvenFileMarkCount.ToString().PadRight(12) _
                            }{a_EvenCRC.ToString().PadRight(12) _
                            }{a_OddDataSetID.ToString().PadRight(12) _
                            }{a_OddRecordCount.ToString().PadRight(12) _
                            }{a_OddFileMarkCount.ToString().PadRight(12) _
                            }{a_OddCRC.ToString().PadRight(12) _
                            }"

                        With CType(g_CM(gtype.tape_directory), TapeDirectory).WrapEntry(a_WrapIndex)
                            .Content = a_OutputStr
                            .RawData = {a_EvenDataSetID, a_EvenRecordCount, a_EvenFileMarkCount, a_EvenCRC, a_OddDataSetID, a_OddRecordCount, a_OddFileMarkCount, a_OddCRC}
                            .RecCount = a_EvenRecordCount + a_OddRecordCount
                            .FileMarkCount = a_EvenFileMarkCount + a_OddFileMarkCount
                        End With
                    Next
                End If
            End With

        End Sub
        Public Function g_GetWord(buffer As Byte(), offset As Integer) As Integer
            Return CInt(buffer(offset)) << 8 Or buffer(offset + 1)
        End Function
        Public Function g_GetDWord(buffer As Byte(), offset As Integer) As Integer
            Dim result As Integer
            For i As Integer = 0 To 3
                result <<= 8
                result = result Or buffer(offset + i)
            Next
            Return result
        End Function
        Public Function g_GetInt64(buffer As Byte(), offset As Integer) As Long
            Dim result As Long
            For i As Integer = 0 To 7
                result <<= 8
                result = result Or buffer(offset + i)
            Next
            Return result
        End Function
        Public Function substr(buffer As Byte(), offset As Long, length As Long) As Byte()
            Dim result(length - 1) As Byte
            Array.Copy(buffer, offset, result, 0, length)
            Return result
        End Function
        Public Function getstr(buffer As Byte(), offset As Long, length As Long) As String
            Dim result(length - 1) As Byte
            Array.Copy(buffer, offset, result, 0, length)
            Return Encoding.ASCII.GetString(result)
        End Function
        Public Sub New()

        End Sub
        Public Sub New(TapeDrive As String, Optional ByVal BufferID As Byte = &H10)
            Select Case TapeUtils.DriverTypeSetting
                Case DriverType.LTO
                    a_CMBuffer = ReadBuffer(TapeDrive, BufferID)
                    If a_CMBuffer.Length = 0 Then
                        'IBM LTO
                        a_CMBuffer = ReadBuffer(TapeDrive, &H5)
                    End If
                Case DriverType.IBM3592
                    '3592
                    a_CMBuffer = ReadBuffer(TapeDrive, &H20)
            End Select
            If a_CMBuffer.Length <> 0 Then
                Try
                    RunParse()
                Catch
                End Try
            End If
        End Sub
        Public Sub New(handle As IntPtr, Optional ByVal BufferID As Byte = &H10)
            a_CMBuffer = ReadBuffer(handle, BufferID)
            If a_CMBuffer.Length = 0 Then a_CMBuffer = ReadBuffer(handle, &H5)
            RunParse()
        End Sub
        Public Sub New(ByVal BufferData As Byte(), Optional ByRef errorMsg As Exception = Nothing)
            a_CMBuffer = BufferData
            If a_CMBuffer.Length <> 0 Then
                Try
                    RunParse()
                Catch ex As Exception
                    errorMsg = ex
                End Try
            End If
        End Sub
        Public Shared Function FromTapeDrive(TapeDrive As String) As CMParser
            Return New CMParser(TapeDrive)
        End Function
        Public Function GetSerializedText() As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(CMParser))
            Dim sb As New Text.StringBuilder
            Dim t As New IO.StringWriter(sb)
            writer.Serialize(t, Me)
            Return sb.ToString()
        End Function
        Public Function GetReport() As String
            Dim Output As New StringBuilder
            Output.Append("+=========================== APPLICATION INFO ============================+" & vbCrLf)
            Try
                Dim BC As String = Me.ApplicationSpecificData.Barcode
                'BC = TapeUtils.ReadBarcode(ConfTapeDrive)
                'BC = TapeUtils.ReadBarcode(ConfTapeDrive)
                Output.Append(("| Barcode: ".PadRight(28) & BC).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Barcode: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
            End Try
            Try
                Dim AppInfo As String = $"{Me.ApplicationSpecificData.Application_vendor} {Me.ApplicationSpecificData.Application_name} {Me.ApplicationSpecificData.Application_version}" ' TapeUtils.ReadAppInfo(ConfTapeDrive)
                Output.Append(("| Application: ".PadRight(28) & AppInfo).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Application: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
            End Try
            Output.Append("+============================= MEDIUM USAGE ==============================+" & vbCrLf)
            If Me.CartridgeMfgData.CartridgeTypeAbbr = "CU" Then
                Try
                    Dim LoadCount As Int64 = Me.StatusData.ThreadCount ' TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 0, 3).AsNumeric
                    Output.Append(("| Cleans performed: ".PadRight(28) & LoadCount).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Cleans performed: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Dim CleanRemain As Int64 = Me.a_CleansRemaining ' TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 0, 3).AsNumeric
                    Output.Append(("| Cleans remain: ".PadRight(28) & CleanRemain).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Cleans remain: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Dim TapeLen As Int64 = Me.CartridgeMfgData.TapeLength ' TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 0, 3).AsNumeric
                    Dim LastLoc As Int64 = Me.StatusData.LastLocation ' TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 0, 3).AsNumeric
                    Output.Append(("| Used length: ".PadRight(28) & $"{(LastLoc / 4).ToString("f2")} m / {((TapeLen / 4) - 11).ToString("f2")} m").PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Used length: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try

            Else
                Try
                    Dim LoadCount As Int64 = Me.StatusData.ThreadCount ' TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 0, 3).AsNumeric
                    Output.Append(("| Load count: ".PadRight(28) & LoadCount).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Load count: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Dim TotalWriteMBytes As Int64 = Me.CartridgeMfgData.KB_PER_DATASET
                    TotalWriteMBytes *= Me.UsageData(0).LifeSetsWritten
                    TotalWriteMBytes \= 1024 'TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 2, &H20).AsNumeric
                    Output.Append(("| Total write: ".PadRight(28) & ReduceDataUnit(TotalWriteMBytes)).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Total write: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Dim TotalReadMBytes As Int64 = Me.CartridgeMfgData.KB_PER_DATASET
                    TotalReadMBytes *= Me.UsageData(0).LifeSetsRead
                    TotalReadMBytes \= 1024 'TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 2, &H21).AsNumeric
                    Output.Append(("| Total read: ".PadRight(28) & ReduceDataUnit(TotalReadMBytes)).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Total read: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Dim fve As Double = (Me.UsageData(0).LifeSetsRead + Me.UsageData(0).LifeSetsWritten) / (Me.a_SetsPerWrap * Me.a_NWraps)
                    Output.Append(("| Full volume equivalents: ".PadRight(28) & fve.ToString("f2") & $" FVE ({(fve / Me.CartridgeMfgData.TAPE_LIFE_IN_VOLS * 100).ToString("f2")}%)").PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Full volume equivalents: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Output.Append(("| Write retries: ".PadRight(28) & Me.UsageData(0).LifeWriteRetries).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Write retries: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Output.Append(("| Read retries: ".PadRight(28) & Me.UsageData(0).LifeReadRetries).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Read retries: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Output.Append(("| Unrecovered writes: ".PadRight(28) & Me.UsageData(0).LifeUnRecovWrites).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Unrecovered writes: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Output.Append(("| Unrecovered reads: ".PadRight(28) & Me.UsageData(0).LifeUnRecovReads).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Unrecovered reads: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Output.Append(("| Suspended writes: ".PadRight(28) & Me.UsageData(0).LifeSuspendedWrites).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Suspended writes: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Output.Append(("| Suspended append writes: ".PadRight(28) & Me.UsageData(0).LifeSuspAppendWrites).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Suspended append writes: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Output.Append(("| Fatal suspended writes: ".PadRight(28) & Me.UsageData(0).LifeFatalSuspWrites).PadRight(74) & "|" & vbCrLf)
                Catch ex As Exception
                    Output.Append(("| Fatal suspended writes: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
                End Try
            End If

            'Try
            '    Dim TapeMetresPulled As Int64 = Me.UsageData(0).LifeTapeMetresPulled
            '    If TapeMetresPulled <= 0 Then Throw New Exception("TMP Not available")
            '    TextBox8.Append(" Tape pulled: ".PadRight(28) & TapeMetresPulled & " m" & vbCrLf)
            'Catch ex As Exception
            '    TextBox8.Append(" Tape pulled: ".PadRight(28) & "Not available" & vbCrLf)
            'End Try
            Output.Append("+============================ MEDIUM IDENTITY ============================+" & vbCrLf)
            Try
                Dim Medium_Format As String = $"{Me.CartridgeMfgData.Format} (MC 0x{Me.CartridgeMfgData.MediaCode.ToString("X4")} DC 0x{Me.CartridgeMfgData.DENSITY_CODE.ToString("X2")})"
                Output.Append(("| Format: ".PadRight(28) & Medium_Format).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Format: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
            End Try
            Try
                Dim Medium_SN As String = Me.CartridgeMfgData.CartridgeSN ' TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 4, 1).AsString
                Output.Append(("| Serial number: ".PadRight(28) & Medium_SN).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Serial number: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
            End Try
            Try
                Dim Tape_Manufacturer As String = Me.CartridgeMfgData.TapeVendor 'TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 4, 0).AsString
                Output.Append(("| Tape Vendor: ".PadRight(28) & Tape_Manufacturer).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Tape Vendor: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
            End Try
            Try
                Dim Tape_Man_Date As String = Me.CartridgeMfgData.MfgDate 'TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 4, 6).AsString
                Output.Append(("| Tape mfg date: ".PadRight(28) & Tape_Man_Date).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Tape mfg date: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
            End Try
            Try
                Dim Medium_Manufacturer As String = Me.MediaMfgData.MediaVendor 'TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 4, 0).AsString
                Output.Append(("| Media Vendor: ".PadRight(28) & Medium_Manufacturer).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Media Vendor: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
            End Try
            Try
                Dim Medium_Man_Date As String = Me.MediaMfgData.MediaMfgDate 'TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 4, 6).AsString
                Output.Append(("| Media mfg date: ".PadRight(28) & Medium_Man_Date).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Media mfg date: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
            End Try
            Try
                Dim CMData As Byte() = Me.a_CMBuffer
                Dim Medium_ParticleType As String = Me.CartridgeMfgData.ParticleType.ToString()
                If Me.CartridgeMfgData.CartridgeTypeAbbr = "CU" Then Medium_ParticleType = "Universal Clean Cartridge"
                Output.Append(("| Particle type: ".PadRight(28) & Medium_ParticleType).PadRight(74) & "|" & vbCrLf)
                Output.Append("+============================= DATA ON TAPE ==============================+" & vbCrLf)
                Dim wares As New StringBuilder
                Dim nLossDS As Long = 0
                Dim DataSize As New List(Of Long)
                Try
                    If Me.CartridgeMfgData.CartridgeTypeAbbr = "CU" Then Exit Try
                    wares.AppendLine("+============================= WRAP ANALYSIS =============================+")
                    wares.AppendLine("| Wrap | Start Block |  End Block  | Filemark |      Set      | Capacity  |")
                    wares.AppendLine("|------+-------------+-------------+----------+---------------+-----------|")
                    Dim StartBlock As Integer = 0
                    Dim CurrSize As Long = 0
                    Dim gw As Boolean = False
                    For wn As Integer = 0 To Me.a_NWraps - 1
                        Dim StartBlockStr As String = StartBlock.ToString()
                        If Me.TapeDirectoryData.CapacityLoss(wn) = -1 Or Me.TapeDirectoryData.CapacityLoss(wn) = -3 Then StartBlockStr = ""
                        Dim EndBlock As Integer = StartBlock + Me.TapeDirectoryData.WrapEntryInfo(wn).RecCount + Me.TapeDirectoryData.WrapEntryInfo(wn).FileMarkCount - 1
                        If Me.TapeDirectoryData.CapacityLoss(wn) = -2 Then EndBlock += 1
                        wares.Append($"| {wn.ToString().PadLeft(3)}  |")
                        wares.Append($" {StartBlockStr.PadLeft(10)}  |")
                        If StartBlockStr <> "" Then
                            wares.Append($"  {EndBlock.ToString.PadLeft(10)} |")
                        Else
                            wares.Append($"  {"".PadLeft(10)} |")
                        End If
                        wares.Append($"  {Me.TapeDirectoryData.WrapEntryInfo(wn).FileMarkCount.ToString().PadLeft(5)}   |")
                        wares.Append($" {Me.TapeDirectoryData.DatasetsOnWrapData(wn).Data.ToString().PadLeft(5)} / {Me.a_SetsPerWrap.ToString().PadRight(5)} |")
                        StartBlock += Me.TapeDirectoryData.WrapEntryInfo(wn).RecCount + Me.TapeDirectoryData.WrapEntryInfo(wn).FileMarkCount
                        If Me.TapeDirectoryData.CapacityLoss(wn) >= 0 Then
                            nLossDS += Math.Max(0, Me.a_SetsPerWrap - Me.TapeDirectoryData.DatasetsOnWrapData(wn).Data)
                            CurrSize += Me.TapeDirectoryData.DatasetsOnWrapData(wn).Data
                            'wares.Append($" { (100 - Me.TapeDirectoryData.CapacityLoss(wn)).ToString("f2").PadLeft(7)}%  |")
                            wares.Append($" { (Me.TapeDirectoryData.DatasetsOnWrapData(wn).Data / Me.a_SetsPerWrap * 100).ToString("f2").PadLeft(7)}%  |")
                        ElseIf Me.TapeDirectoryData.CapacityLoss(wn) = -1 Then
                            StartBlock = 0
                            wares.Append($"           |")
                        ElseIf Me.TapeDirectoryData.CapacityLoss(wn) = -2 Then
                            CurrSize += Me.TapeDirectoryData.DatasetsOnWrapData(wn).Data
                            wares.Append($"  >>EOD<<  |")
                        ElseIf Me.TapeDirectoryData.CapacityLoss(wn) = -3 Then
                            StartBlock = 0
                            If gw Then
                                DataSize.Add(CurrSize)
                                CurrSize = 0
                                gw = False
                            Else
                                gw = True
                            End If
                            wares.Append($"  *GUARD*  |")
                        End If
                        wares.AppendLine()
                    Next
                    DataSize.Add(CurrSize)
                Catch ex As Exception
                    wares.Append("| CM data parsing failed.".PadRight(74) & "|" & vbCrLf)
                End Try
                Try
                    Dim DataWrapList As New List(Of Integer)
                    Dim DataWrapNum As Integer = 0
                    For Each l As Double In Me.TapeDirectoryData.CapacityLoss
                        If l = -3 Then
                            If DataWrapNum > 0 Then
                                DataWrapList.Add(DataWrapNum)
                                DataWrapNum = 0
                            End If
                        Else
                            DataWrapNum += 1
                        End If
                    Next
                    If DataWrapNum > 0 Then DataWrapList.Add(DataWrapNum)
                    Output.Append(("| Total partitions: ".PadRight(28) & DataWrapList.Count).PadRight(74) & "|" & vbCrLf)
                    For i As Integer = 0 To DataWrapList.Count - 1
                        Dim nWrap As Long = DataWrapList(i)
                        Dim len As Long = nWrap * Me.CartridgeMfgData.MB_PER_WRAP

                        Dim WrittenSize As String = ""
                        If DataSize.Count = DataWrapList.Count Then
                            WrittenSize = $"{IOManager.FormatSize(DataSize(i) * Me.CartridgeMfgData.KB_PER_DATASET * 1024, True)} / "
                        End If
                        Output.Append（($"| Partition {i} size: ".PadRight(28) & (WrittenSize & ReduceDataUnit(len)).PadRight(24) & $"[{nWrap.ToString().PadLeft(3)} wraps]").PadRight(74) & "|" & vbCrLf)
                    Next
                Catch ex As Exception
                    Output.Append("Partition page not available" & vbCrLf)
                End Try
                Output.Append(("| Estimated capacity loss: ".PadRight(28) & IOManager.FormatSize(nLossDS * Me.CartridgeMfgData.KB_PER_DATASET * 1000)).PadRight(74) & "|" & vbCrLf)
                Output.Append(wares.ToString())

                Output.Append("+============================== CM RAW DATA ==============================+" & vbCrLf)
                Output.Append(("| Length: " & CMData.Length).PadRight(74) & "|" & vbCrLf)
                Output.Append(RichByte2Hex(CMData, True))
                Output.Append("+=========================================================================+")
                Output.Append(vbCrLf)
            Catch ex As Exception
                Output.Append("| CM data parsing failed.".PadRight(74) & "|" & vbCrLf & ex.ToString & vbCrLf)
            End Try
            Return Output.ToString()
        End Function
    End Class



    <TypeConverter(GetType(ExpandableObjectConverter))>
    <Serializable> Public Class PageData
        <TypeConverter(GetType(ExpandableObjectConverter))>
        <Serializable> Public Class DataItem
            Public Property Name As String
            Public Property StartByte As Integer
            Public Property BitOffset As Byte
            Public Property TotalBits As Integer
            Public Property DynamicParamCodeStartByte As Integer
            Public Property DynamicParamCodeBitOffset As Byte
            Public Property DynamicParamCodeTotalBits As Byte
            Public Property DynamicParamLenStartByte As Integer
            Public Property DynamicParamLenBitOffset As Byte
            Public Property DynamicParamLenTotalBits As Byte
            Public Property DynamicParamDataStartByte As Integer
            <TypeConverter(GetType(ExpandableObjectConverter))>
            <Serializable> Public Class DynamicParamPage
                Public ReadOnly Property Name As String
                    Get
                        Dim value As String = ""
                        Parent.EnumTranslator.TryGetValue(ParamCode, value)
                        If value = "" Then value = $"0x{Hex(ParamCode).Replace("-", "")}"
                        Return value
                    End Get
                End Property
                Public Property ParamCode As Integer
                Public Property Parent As DataItem
                Public Property Type As DataType
                Public Property EnumTranslator As SerializableDictionary(Of Long, String)
                <Xml.Serialization.XmlIgnore>
                Public Property RawData As Byte()
                <Xml.Serialization.XmlIgnore>
                Public ReadOnly Property LastByte As Byte
                    Get
                        If RawData Is Nothing OrElse RawData.Length = 0 Then Return Nothing
                        Return RawData.Last
                    End Get
                End Property

                <Xml.Serialization.XmlIgnore>
                Public ReadOnly Property GetString As String
                    Get
                        If Parent Is Nothing Then Return ""
                        Dim rawdata As Byte() = Me.RawData()
                        If rawdata Is Nothing OrElse rawdata.Length = 0 Then Return ""
                        Select Case Type
                            Case DataType.Byte
                                Dim result As Byte
                                For i As Integer = 0 To rawdata.Length - 1
                                    result = result << 8
                                    result = result Or rawdata(i)
                                Next
                                Return result.ToString
                            Case DataType.Int16
                                Dim result As Int16
                                For i As Integer = 0 To rawdata.Length - 1
                                    result = result << 8
                                    result = result Or rawdata(i)
                                Next
                                Return result.ToString
                            Case DataType.Int32
                                Dim result As Integer
                                For i As Integer = 0 To rawdata.Length - 1
                                    result = result << 8
                                    result = result Or rawdata(i)
                                Next
                                Return result.ToString
                            Case DataType.Int64
                                Dim result As Long
                                For i As Integer = 0 To rawdata.Length - 1
                                    result = result << 8
                                    result = result Or rawdata(i)
                                Next
                                Return result.ToString
                            Case DataType.UInt64
                                Dim result As ULong
                                For i As Integer = 0 To rawdata.Length - 1
                                    result = result << 8
                                    result = result Or rawdata(i)
                                Next
                                Return result.ToString
                            Case DataType.Boolean
                                If rawdata.Last = 0 Then Return "False" Else Return True
                            Case DataType.Text
                                Return ByteArrayToString(rawdata, True)
                            Case DataType.Enum
                                Dim key As Long
                                For i As Integer = 0 To rawdata.Length - 1
                                    key = key << 8
                                    key = key Or rawdata(i)
                                Next
                                If EnumTranslator IsNot Nothing Then
                                    Dim result As String = ""
                                    If EnumTranslator.TryGetValue(key, result) Then
                                        Return $"0x{Hex(key)} {result}"
                                    Else
                                        Return key.ToString()
                                    End If
                                End If
                                Return key.ToString()
                            Case DataType.Binary
                                Return $"0x{BitConverter.ToString(rawdata).Replace("-", "").ToUpper}"
                            Case DataType.PageData
                                Dim pagedata As PageData
                                If Parent.PageDataTemplate.TryGetValue(ParamCode, pagedata) Then
                                    pagedata.RawData = rawdata
                                    Return pagedata.GetSummary(False)
                                Else
                                    Return ""
                                End If
                            Case Else
                                If rawdata.Length > 0 Then Return Byte2Hex(rawdata, True) Else Return ""
                        End Select
                    End Get
                End Property
                <Xml.Serialization.XmlIgnore>
                Public ReadOnly Property GetLong As Long
                    Get
                        Dim result As Long
                        For i As Integer = 0 To RawData.Length - 1
                            result <<= 8
                            result = result Or RawData(i)
                        Next
                        Return result
                    End Get
                End Property
                <Xml.Serialization.XmlIgnore>
                Public ReadOnly Property GetPage As PageData
                    Get
                        If Type = DataType.PageData Then
                            Dim pagedata As PageData
                            If Parent.PageDataTemplate.TryGetValue(ParamCode, pagedata) Then
                                pagedata.RawData = RawData
                                Return pagedata
                            End If
                        End If
                        Return Nothing
                    End Get
                End Property



                Public Shared Function [Next](ByVal PageData As DataItem, ByVal StartByte As Integer) As DynamicParamPage
                    Dim rawLen(Math.Ceiling(PageData.DynamicParamLenTotalBits / 8) - 1) As Byte
                    For i As Integer = 0 To PageData.DynamicParamLenTotalBits - 1
                        Dim resultByteNum As Integer = rawLen.Length - 1 - i \ 8
                        Dim resultBitNum As Byte = i Mod 8 '76543210
                        Dim sourceByteNum As Integer = StartByte + PageData.DynamicParamLenStartByte + (PageData.DynamicParamLenBitOffset + PageData.DynamicParamLenTotalBits - i - 1) \ 8
                        Dim sourceBitNum As Integer = 7 - (PageData.DynamicParamLenBitOffset + PageData.DynamicParamLenTotalBits - i - 1) Mod 8
                        rawLen(resultByteNum) = rawLen(resultByteNum) Or ((PageData.RawData(sourceByteNum) And 1 << sourceBitNum) >> sourceBitNum) << resultBitNum
                    Next
                    Dim LenValue As Integer
                    For i As Integer = 0 To rawLen.Length - 1
                        LenValue <<= 8
                        LenValue = LenValue Or rawLen(i)
                    Next
                    Dim rawPCode(Math.Ceiling(PageData.DynamicParamCodeTotalBits / 8) - 1) As Byte
                    For i As Integer = 0 To PageData.DynamicParamCodeTotalBits - 1
                        Dim resultByteNum As Integer = rawPCode.Length - 1 - i \ 8
                        Dim resultBitNum As Byte = i Mod 8 '76543210
                        Dim sourceByteNum As Integer = StartByte + PageData.DynamicParamCodeStartByte + (PageData.DynamicParamCodeBitOffset + PageData.DynamicParamCodeTotalBits - i - 1) \ 8
                        Dim sourceBitNum As Integer = 7 - (PageData.DynamicParamCodeBitOffset + PageData.DynamicParamCodeTotalBits - i - 1) Mod 8
                        rawPCode(resultByteNum) = rawPCode(resultByteNum) Or ((PageData.RawData(sourceByteNum) And 1 << sourceBitNum) >> sourceBitNum) << resultBitNum
                    Next
                    Dim PCode As Integer
                    For i As Integer = 0 To rawPCode.Length - 1
                        PCode <<= 8
                        PCode = PCode Or rawPCode(i)
                    Next
                    Dim resultData As Byte() = {}
                    LenValue = Math.Min(LenValue, PageData.RawData.Length - StartByte - PageData.DynamicParamDataStartByte)
                    If LenValue > 0 Then
                        ReDim resultData(LenValue - 1)
                        Array.Copy(PageData.RawData, StartByte + PageData.DynamicParamDataStartByte, resultData, 0, LenValue)
                    End If
                    Dim dataType As DataType
                    Try
                        PageData.DynamicParamType.TryGetValue(PCode, dataType)
                    Catch ex As Exception
                        Return Nothing
                    End Try
                    Return New DynamicParamPage With {.Parent = PageData, .ParamCode = PCode, .RawData = resultData, .Type = dataType}
                End Function
            End Class
            Public Enum DataType
                [Byte]
                Int16
                Int32
                Int64
                UInt64
                [Boolean]
                Text
                [Enum]
                Binary
                DynamicPage
                PageData
                RawData
            End Enum
            Public Property Type As DataType
            Public Property EnumTranslator As SerializableDictionary(Of Long, String)
            Public Property DynamicParamType As SerializableDictionary(Of Long, DataType)
            Public Property PageDataTemplate As SerializableDictionary(Of Long, PageData)
            Public ReadOnly Property RawData As Byte()
                Get
                    If Parent Is Nothing Then Return Nothing
                    If Type = DataType.DynamicPage Then
                        Return Parent.RawData.Skip(StartByte).ToArray()
                    End If
                    If (StartByte + (BitOffset + TotalBits - 1) \ 8) >= Parent.RawData.Length Then Return Nothing
                    Dim result(Math.Ceiling(TotalBits / 8) - 1) As Byte
                    For i As Integer = 0 To TotalBits - 1
                        Dim resultByteNum As Integer = result.Length - 1 - i \ 8
                        Dim resultBitNum As Byte = i Mod 8 '76543210
                        Dim sourceByteNum As Integer = StartByte + (BitOffset + TotalBits - i - 1) \ 8
                        If sourceByteNum >= Parent.RawData.Length Then Return result
                        Dim sourceBitNum As Integer = 7 - (BitOffset + TotalBits - i - 1) Mod 8
                        result(resultByteNum) = result(resultByteNum) Or ((Parent.RawData(sourceByteNum) And 1 << sourceBitNum) >> sourceBitNum) << resultBitNum
                    Next
                    Return result
                End Get
            End Property
            Public ReadOnly Property GetString As String
                Get
                    If Parent Is Nothing Then Return ""
                    Dim rawdata As Byte() = Me.RawData()
                    If rawdata Is Nothing OrElse rawdata.Length = 0 Then Return ""
                    Select Case Type
                        Case DataType.Byte
                            Dim result As Byte
                            For i As Integer = 0 To rawdata.Length - 1
                                result = result << 8
                                result = result Or rawdata(i)
                            Next
                            Return result.ToString
                        Case DataType.Int16
                            Dim result As Int16
                            For i As Integer = 0 To rawdata.Length - 1
                                result = result << 8
                                result = result Or rawdata(i)
                            Next
                            Return result.ToString
                        Case DataType.Int32
                            Dim result As Integer
                            For i As Integer = 0 To rawdata.Length - 1
                                result = result << 8
                                result = result Or rawdata(i)
                            Next
                            Return result.ToString
                        Case DataType.Int64
                            Dim result As Long
                            For i As Integer = 0 To rawdata.Length - 1
                                result = result << 8
                                result = result Or rawdata(i)
                            Next
                            Return result.ToString
                        Case DataType.UInt64
                            Dim result As ULong
                            For i As Integer = 0 To rawdata.Length - 1
                                result = result << 8
                                result = result Or rawdata(i)
                            Next
                            Return result.ToString
                        Case DataType.Boolean
                            If rawdata.Last = 0 Then Return "False" Else Return True
                        Case DataType.Text
                            Return ByteArrayToString(rawdata, True)
                        Case DataType.Enum
                            Dim key As Long
                            For i As Integer = 0 To rawdata.Length - 1
                                key = key << 8
                                key = key Or rawdata(i)
                            Next
                            If EnumTranslator IsNot Nothing Then
                                Dim result As String = ""
                                If EnumTranslator.TryGetValue(key, result) Then
                                    Return $"0x{Hex(key)} {result}"
                                Else
                                    Return key.ToString()
                                End If
                            End If
                            Return key.ToString()
                        Case DataType.DynamicPage
                            Dim i As Integer = 0
                            Dim sb As New StringBuilder
                            sb.Append($"↓")
                            While i < rawdata.Length - 1
                                Dim nextPage As DynamicParamPage = DynamicParamPage.Next(Me, i)
                                sb.Append($"{vbCrLf} * {nextPage.Name}")
                                Dim nPStr As String = nextPage.GetString()
                                If nPStr.Length > 0 Then
                                    For Each t As String In nPStr.Split({vbCrLf}, StringSplitOptions.RemoveEmptyEntries)
                                        sb.Append($"{vbCrLf}       {t}")
                                    Next
                                End If
                                i += nextPage.RawData.Length + DynamicParamDataStartByte
                            End While

                            Return sb.ToString()
                        Case DataType.Binary
                            Return $"0x{BitConverter.ToString(rawdata).Replace("-", "").ToUpper}"
                        Case Else
                            Return Byte2Hex(rawdata, True)
                    End Select
                End Get
            End Property
            <Xml.Serialization.XmlIgnore> Public Property Parent As PageData
        End Class
        Public Property Name As String
        Public Property PageCode As Integer
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of DataItem), DataItem)))>
        Public Property Items As New List(Of DataItem)
        <Xml.Serialization.XmlIgnore> Public Property RawData As Byte()
            Get
                Return _RawData
            End Get
            Set(value As Byte())
                _RawData = value
                _DynamicParamPages = Nothing
            End Set
        End Property
        <Xml.Serialization.XmlIgnore> Private _RawData As Byte()
        Public Function GetSummary(Optional ByVal ShowTitle As Boolean = True) As String
            Dim sb As New StringBuilder
            If ShowTitle Then sb.AppendLine($"{Name}".PadLeft(Math.Max(0, 32 + Name.Length \ 2), "=").PadRight(64, "="))
            For Each it As DataItem In Items
                Dim result As String = it.GetString()
                If result IsNot Nothing AndAlso result.Length > 0 Then sb.AppendLine($"{it.Name} = {result}")
            Next
            If ShowTitle Then sb.AppendLine("".PadRight(64, "="))
            Return sb.ToString()
        End Function

        <Xml.Serialization.XmlIgnore>
        <TypeConverter(GetType(ListTypeDescriptor(Of List(Of DataItem.DynamicParamPage), DataItem.DynamicParamPage)))>
        Public Property DynamicParamPages As List(Of DataItem.DynamicParamPage)
            Get
                If _DynamicParamPages Is Nothing Then
                    _DynamicParamPages = New List(Of DataItem.DynamicParamPage)
                    For Each it As TapeUtils.PageData.DataItem In Items
                        If it.EnumTranslator Is Nothing Then Continue For
                        Dim i As Integer = 0
                        While i < it.RawData.Length - 1
                            Dim nextPage As TapeUtils.PageData.DataItem.DynamicParamPage = TapeUtils.PageData.DataItem.DynamicParamPage.Next(it, i)
                            If nextPage Is Nothing Then Continue While
                            _DynamicParamPages.Add(nextPage)
                            i += nextPage.RawData.Length + it.DynamicParamDataStartByte
                        End While
                    Next
                End If
                Return _DynamicParamPages
            End Get
            Set(value As List(Of DataItem.DynamicParamPage))
                _DynamicParamPages = value
            End Set
        End Property
        <Xml.Serialization.XmlIgnore> Private _DynamicParamPages As List(Of DataItem.DynamicParamPage)
        Public Function TryGetPage(ParamCode As Integer) As DataItem.DynamicParamPage
            Try
                For Each p As DataItem.DynamicParamPage In DynamicParamPages
                    If p.ParamCode = ParamCode Then Return p
                Next
            Catch ex As Exception
            End Try
            Return Nothing
        End Function
        Public Function TryGetPage(ParamName As String, Optional ByVal IgnoreCase As Boolean = True) As DataItem.DynamicParamPage
            Try
                For Each p As DataItem.DynamicParamPage In DynamicParamPages
                    If IgnoreCase Then
                        If p.Name.ToLower = ParamName.ToLower Then Return p
                    Else
                        If p.Name = ParamName Then Return p
                    End If
                Next
            Catch ex As Exception
            End Try
            Return Nothing
        End Function
        Public Function GetSerializedText(Optional ByVal ReduceSize As Boolean = True) As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(PageData))
            Dim sb As New System.Text.StringBuilder()
            Dim t As IO.TextWriter = New IO.StringWriter(sb)
            writer.Serialize(t, Me)
            t.Close()
            Return sb.ToString
        End Function
        Public Shared Function FromXML(s As String) As PageData
            Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(PageData))
            Dim t As IO.TextReader = New IO.StringReader(s)
            Dim result As PageData = CType(reader.Deserialize(t), PageData)
            If result.Items IsNot Nothing Then
                Dim RemainingDataItem As New List(Of DataItem)
                RemainingDataItem = result.Items
                For Each it As DataItem In RemainingDataItem
                    it.Parent = result
                Next
                While RemainingDataItem.Count > 0
                    Dim newDI As New List(Of DataItem)
                    For Each it As DataItem In RemainingDataItem
                        If it.PageDataTemplate IsNot Nothing Then
                            For Each t2 As PageData In it.PageDataTemplate.Values
                                For Each i2 As DataItem In t2.Items
                                    i2.Parent = t2
                                    If i2.PageDataTemplate IsNot Nothing Then
                                        newDI.Add(i2)
                                    End If
                                Next
                            Next
                        End If
                    Next
                    RemainingDataItem = newDI
                End While

            End If
            Return result
        End Function
        Public Enum DefaultPages
            HPLTO6_SupportedLogPagesPage = &H0
            HPLTO6_WriteErrorCountersLogPage = &H2
            HPLTO6_ReadErrorCountersLogPage = &H3
            HPLTO6_SequentialAccessDeviceLogPage = &HC
            HPLTO6_TemperatureLogPage = &HD
            HPLTO6_DataTransferDeviceStatusLogPage = &H11
            HPLTO6_TapeAlertResponseLogPage = &H12
            HPLTO6_RequestedRecoveryLogPage = &H13
            HPLTO6_DeviceStatisticsLogPage = &H14
            HPLTO6_ServiceBuffersInformationLogPage = &H15
            HPLTO6_TapeDiagnosticLogPage = &H16
            HPLTO6_VolumeStatisticsLogPage = &H17
            HPLTO6_ProtocolSpecificPortLogPage = &H18
            HPLTO6_DataCompressionLogPage = &H1B
            HPLTO6_TapeAlertLogPage = &H2E
            HPLTO6_TapeUsageLogPage = &H30
            HPLTO6_TapeCapacityLogPage = &H31
            HPLTO6_DataCompressionHPLogPage = &H32
            HPLTO6_DeviceWellnessLogPage = &H33
            HPLTO6_PerformanceDataLogPage = &H34
            HPLTO6_DTDeviceErrorLogPage = &H35
            HPLTO6_DeviceStatusLogPage = &H3E
        End Enum
        Public Shared Function CreateDefault(PageTemplate As DefaultPages, ByVal logdata As Byte()) As PageData
            Dim pdata As PageData
            Dim DensityCodeTranslator As New SerializableDictionary(Of Long, String)
            Dim MediumTypeTranslator As New SerializableDictionary(Of Long, String)
            Dim SenseCodeTranslator As New SerializableDictionary(Of Long, String)
            With SenseCodeTranslator
                .Add(&H0, "NO SENSE")
                .Add(&H1, "RECOVERED ERROR")
                .Add(&H2, "NOT READY")
                .Add(&H3, "MEDIUM ERROR")
                .Add(&H4, "HARDWARE ERROR")
                .Add(&H5, "ILLEGAL REQUEST")
                .Add(&H6, "UNIT ATTENTION")
                .Add(&H7, "DATA PROTECT")
                .Add(&H8, "BLANK CHECK")
                .Add(&H9, "VENDOR SPECIFIC")
                .Add(&HA, "COPY ABORTED")
                .Add(&HB, "ABORTED COMMAND")
                .Add(&HC, "EQUAL")
                .Add(&HD, "VOLUME OVERFLOW")
                .Add(&HE, "MISCOMPARE")
                .Add(&HF, "RESERVED")
            End With
            Dim AdditionalSenseCodeTranslator As New SerializableDictionary(Of Long, String)
            With AdditionalSenseCodeTranslator
                .Add(&H0, "No addition sense")
                .Add(&H1, "Filemark detected")
                .Add(&H2, "End of Tape detected")
                .Add(&H4, "Beginning of Tape detected")
                .Add(&H5, "End of Data detected")
                .Add(&H16, "Operation in progress")
                .Add(&H18, "Erase operation in progress")
                .Add(&H19, "Locate operation in progress")
                .Add(&H1A, "Rewind operation in progress")
                .Add(&H400, "LUN not ready, cause not reportable")
                .Add(&H401, "LUN in process of becoming ready")
                .Add(&H402, "LUN not ready, Initializing command required")
                .Add(&H404, "LUN not ready, format in progress")
                .Add(&H407, "Command in progress")
                .Add(&H409, "LUN not ready, self-test in progress")
                .Add(&H40C, "LUN not accessible, port in unavailable state")
                .Add(&H412, "Logical unit offline")
                .Add(&H800, "Logical unit communication failure")
                .Add(&HB00, "Warning")
                .Add(&HB01, "Thermal limit exceeded")
                .Add(&HC00, "Write error")
                .Add(&HE01, "Information unit too short")
                .Add(&HE02, "Information unit too long")
                .Add(&HE03, "SK Illegal Request")
                .Add(&H1001, "Logical block guard check failed")
                .Add(&H1100, "Unrecovered read error")
                .Add(&H1112, "Media Auxiliary Memory read error")
                .Add(&H1400, "Recorded entity not found")
                .Add(&H1403, "End of Data not found")
                .Add(&H1A00, "Parameter list length error")
                .Add(&H2000, "Invalid command operation code")
                .Add(&H2400, "Invalid field in Command Descriptor Block")
                .Add(&H2500, "LUN not supported")
                .Add(&H2600, "Invalid field in parameter list")
                .Add(&H2601, "Parameter not supported")
                .Add(&H2602, "Parameter value invalid")
                .Add(&H2604, "Invalid release of persistent reservation")
                .Add(&H2610, "Data decryption key fail limit reached")
                .Add(&H2680, "Invalid CA certificate")
                .Add(&H2700, "Write-protected")
                .Add(&H2708, "Too many logical objects on partition to support operation")
                .Add(&H2800, "Not ready to ready transition, medium may have changed")
                .Add(&H2901, "Power-on reset")
                .Add(&H2902, "SCSI bus reset")
                .Add(&H2903, "Bus device reset")
                .Add(&H2904, "Internal firmware reboot")
                .Add(&H2907, "I_T nexus loss occurred")
                .Add(&H2A01, "Mode parameters changed")
                .Add(&H2A02, "Log parameters changed")
                .Add(&H2A03, "Reservations pre-empted")
                .Add(&H2A04, "Reservations released")
                .Add(&H2A05, "Registrations pre-empted")
                .Add(&H2A06, "Asymmetric access state changed")
                .Add(&H2A07, "Asymmetric access state transition failed")
                .Add(&H2A08, "Priority changed")
                .Add(&H2A0D, "Data encryption capabilities changed")
                .Add(&H2A10, "Timestamp changed")
                .Add(&H2A11, "Data encryption parameters changed by another initiator")
                .Add(&H2A12, "Data encryption parameters changed by a vendor-specific event")
                .Add(&H2A13, "Data Encryption Key Instance Counter has changed")
                .Add(&H2A14, "SA creation capabilities data has changed")
                .Add(&H2A15, "Medium removal prevention pre-empted")
                .Add(&H2A80, "Security configuration changed")
                .Add(&H2C00, "Command sequence invalid")
                .Add(&H2C07, "Previous busy status")
                .Add(&H2C08, "Previous task set full status")
                .Add(&H2C09, "Previous reservation conflict status")
                .Add(&H2C0B, "Not reserved")
                .Add(&H2F00, "Commands cleared by another initiator")
                .Add(&H3000, "Incompatible medium installed")
                .Add(&H3001, "Cannot read media, unknown format")
                .Add(&H3002, "Cannot read media: incompatible format")
                .Add(&H3003, "Cleaning cartridge installed")
                .Add(&H3004, "Cannot write medium")
                .Add(&H3005, "Cannot write medium, incompatible format")
                .Add(&H3006, "Cannot format, incompatible medium")
                .Add(&H3007, "Cleaning failure")
                .Add(&H300C, "WORM medium—overwrite attempted")
                .Add(&H300D, "WORM medium—integrity check failed")
                .Add(&H3100, "Medium format corrupted")
                .Add(&H3700, "Rounded parameter")
                .Add(&H3A00, "Medium not present")
                .Add(&H3A04, "Medium not present, Media Auxiliary Memory accessible")
                .Add(&H3B00, "Sequential positioning error")
                .Add(&H3B0C, "Position past BOM")
                .Add(&H3B1C, "Too many logical objects on partition to support operation.")
                .Add(&H3E00, "Logical unit has not self-configured yet")
                .Add(&H3F01, "Microcode has been changed")
                .Add(&H3F03, "Inquiry data has changed")
                .Add(&H3F05, "Device identifier changed")
                .Add(&H3F0E, "Reported LUNs data has changed")
                .Add(&H3F0F, "Echo buffer overwritten")
                .Add(&H4300, "Message error")
                .Add(&H4400, "Internal target failure")
                .Add(&H4500, "Selection/reselection failure")
                .Add(&H4700, "SCSI parity error")
                .Add(&H4800, "Initiator Detected Error message received")
                .Add(&H4900, "Invalid message")
                .Add(&H4B00, "Data phase error")
                .Add(&H4B02, "Too much write data")
                .Add(&H4B03, "ACK/NAK timeout")
                .Add(&H4B04, "NAK received")
                .Add(&H4B05, "Data offset error")
                .Add(&H4B06, "Initiator response timeout")
                .Add(&H4D00, "Tagged overlapped command")
                .Add(&H4E00, "Overlapped commands")
                .Add(&H5000, "Write append error")
                .Add(&H5200, "Cartridge fault")
                .Add(&H5300, "Media load or eject failed")
                .Add(&H5301, "Unload tape failure")
                .Add(&H5302, "Medium removal prevented")
                .Add(&H5303, "Insufficient resources")
                .Add(&H5304, "Medium thread or unthread failure")
                .Add(&H5504, "Insufficient registration resources")
                .Add(&H5506, "Media Auxiliary Memory full")
                .Add(&H5B01, "Threshold condition met")
                .Add(&H5D00, "Failure prediction threshold exceeded")
                .Add(&H5DFF, "Failure prediction threshold exceeded (false)")
                .Add(&H5E01, "Idle condition activated by timer")
                .Add(&H7400, "Security error")
                .Add(&H7401, "Unable to decrypt data")
                .Add(&H7402, "Unencrypted data encountered while decrypting")
                .Add(&H7403, "Incorrect data encryption key")
                .Add(&H7404, "Cryptographic integrity validation failed")
                .Add(&H7405, "Key-associated data descriptors changed.")
                .Add(&H7408, "Digital signature validation failure")
                .Add(&H7409, "Encryption mode mismatch on read")
                .Add(&H740A, "Encrypted block not RAW read-enabled")
                .Add(&H740B, "Incorrect encryption parameters")
                .Add(&H7421, "Data encryption configuration prevented")
                .Add(&H7440, "Authentication failed")
                .Add(&H7461, "External data encryption Key Manager access error")
                .Add(&H7462, "External data encryption Key Manager error")
                .Add(&H7463, "External data encryption management—key not found")
                .Add(&H7464, "External data encryption management—request not authorized")
                .Add(&H746E, "External data encryption control time-out")
                .Add(&H746F, "External data encryption control unknown error")
                .Add(&H7471, "Logical Unit access not authorized")
                .Add(&H7480, "KAD changed")
                .Add(&H7482, "Crypto KAD in CM failure")
                .Add(&H8282, "Drive requires cleaning")
                .Add(&H8283, "Bad microcode detected")
            End With
            With DensityCodeTranslator
                .Add(&H40, "L1")
                .Add(&H42, "L2")
                .Add(&H44, "L3")
                .Add(&H46, "L4")
                .Add(&H58, "L5")
                .Add(&H5A, "L6")
                .Add(&H5C, "L7")
                .Add(&H5D, "M8")
                .Add(&H5E, "L8")
                .Add(&H60, "L9")
            End With
            With MediumTypeTranslator
                .Add(0, "RW")
                .Add(1, "WORM")
            End With
            Dim TapeAlertFlag As New SerializableDictionary(Of Long, String)
            With TapeAlertFlag
                .Add(1, "Read")
                .Add(2, "Write")
                .Add(3, "Hard Error")
                .Add(4, "Medium")
                .Add(5, "Read Failure")
                .Add(6, "Write Failure")
                .Add(7, "Medium Life")
                .Add(8, "Not Data Grade")
                .Add(9, "Write-Protect")
                .Add(10, "Volume Removal Prevented")
                .Add(11, "Cleaning Volume")
                .Add(12, "Unsupported Format")
                .Add(13, "Recoverable Mechanical Cartridge Failure")
                .Add(14, "Unrecoverable Mechanical Cartridge Failure")
                .Add(15, "Memory Chip in Cartridge Failure")
                .Add(16, "Forced Eject")
                .Add(17, "Read-Only Format")
                .Add(18, "Tape Directory Corrupted on Load")
                .Add(19, "Nearing Medium Life")
                .Add(20, "Cleaning Required")
                .Add(21, "Cleaning Requested")
                .Add(22, "Expired Cleaning Volume")
                .Add(23, "Invalid Cleaning Volume")
                .Add(24, "Retension Requested")
                .Add(25, "Multi-port Interface Error on Primary Port")
                .Add(26, "Cooling Fan Failure")
                .Add(27, "Power Supply Failure")
                .Add(28, "Power Consumption")
                .Add(29, "Drive Preventative Maintenance Required")
                .Add(30, "Hardware A")
                .Add(31, "Hardware B")
                .Add(32, "Primary Interface")
                .Add(33, "Eject Media")
                .Add(34, "Microcode Update Failure")
                .Add(35, "Drive Humidity")
                .Add(36, "Drive Temperature")
                .Add(37, "Drive Voltage")
                .Add(38, "Predictive Failure")
                .Add(39, "Diagnostics Required")
                .Add(49, "Diminished Native Capacity")
                .Add(50, "Lost Statistics")
                .Add(51, "Tape Directory Invalid at Unload")
                .Add(52, "Tape System Area Write Failure")
                .Add(53, "Tape System Area Read Failure")
                .Add(54, "No Start of Data")
                .Add(55, "Loading or Threading Failure")
                .Add(56, "Unrecoverable Unload Failure")
                .Add(57, "Automation Interface Failure")
                .Add(58, "Microcode Failure")
                .Add(59, "WORM Medium — Integrity Check Failed")
                .Add(60, "WORM Medium — Overwrite Attempted")
                .Add(61, "Encryption Policy Violation")
            End With
            Select Case PageTemplate
                Case DefaultPages.HPLTO6_SupportedLogPagesPage
                    pdata = New TapeUtils.PageData With {.Name = "Supported Log Pages page", .PageCode = &H0, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 2,
                                    .TotalBits = 6,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Supported Page List",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 8,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 0,
                                    .DynamicParamLenTotalBits = 0,
                                    .DynamicParamDataStartByte = 1,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(&H0, "Supported Pages (00h)")
                        .Add(&H2, "Write Error Counters (02h)")
                        .Add(&H3, "Read Error Counters (03h)")
                        .Add(&HC, "Sequential Access Device Log (0Ch)")
                        .Add(&HD, "Temperature Log (0Dh)")
                        .Add(&H11, "DTD Status Log (11h)")
                        .Add(&H12, "TapeAlert Response Log (12h)")
                        .Add(&H13, "Requested Recovery Log (13h)")
                        .Add(&H14, "Device Statistics Log (14h)")
                        .Add(&H15, "Service Buffers Information Log (15h)")
                        .Add(&H16, "Tape Diagnostics Data Log (16h)")
                        .Add(&H17, "Volume Statistics Log (17h)")
                        .Add(&H18, "SAS Port Log (18h)")
                        .Add(&H1B, "Data Compression Log (1Bh)")
                        .Add(&H2E, "TapeAlert Log (2Eh)")
                        .Add(&H30, "Tape Usage Log (30h)")
                        .Add(&H31, "Tape Capacity Log (31h)")
                        .Add(&H32, "Data Compression (HP-only) Log (32h)")
                        .Add(&H33, "Device Wellness Log (33h)")
                        .Add(&H34, "Performance Log (34h)")
                        .Add(&H35, "DT Device Error Log (35h)")
                        .Add(&H3E, "Device Status Log (3Eh)")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(&H0, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H2, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H3, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&HC, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&HD, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H11, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H12, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H13, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H14, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H15, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H16, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H17, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H18, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H1B, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H2E, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H30, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H31, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H32, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H33, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H34, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H35, TapeUtils.PageData.DataItem.DataType.Binary)
                        .Add(&H3E, TapeUtils.PageData.DataItem.DataType.Binary)
                    End With
                Case DefaultPages.HPLTO6_WriteErrorCountersLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Write Error Counters log page", .PageCode = &H2, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 2,
                                    .TotalBits = 6,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Write Error Counters",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 1,
                                    .DynamicParamCodeTotalBits = 8,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(&H0, "Errors corrected without substantial delay (Total number of errors corrected without delay)")
                        .Add(&H1, "Errors corrected with possible delays (Total number of errors corrected using retries)")
                        .Add(&H2, "Total (Sum of parameters 3 and 6)")
                        .Add(&H3, "Total errors corrected (The number of data sets that needed to be rewritten)")
                        .Add(&H4, "Total times error correction processed (Number of CCQ sets rewritten)")
                        .Add(&H5, "Total data sets processed (The total number of data sets written)")
                        .Add(&H6, "Total uncorrected errors (The number of data sets that could not be written)")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int32)
                    End With
                Case DefaultPages.HPLTO6_ReadErrorCountersLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Read Error Counters log page", .PageCode = &H3, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 2,
                                    .TotalBits = 6,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Read Error Counters",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 1,
                                    .DynamicParamCodeTotalBits = 8,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(&H0, "Errors corrected without substantial delay (Total number of errors corrected without delay)")
                        .Add(&H1, "Errors corrected with possible delays (Total number of errors corrected using retries)")
                        .Add(&H2, "Total (Sum of parameters 3 and 6)")
                        .Add(&H3, "Total errors corrected (The number of data sets that were corrected after a read retry)")
                        .Add(&H4, "Total times error correction processed (Number of times C2 correction is invoked)")
                        .Add(&H5, "Total bytes processed (The total number of data sets read)")
                        .Add(&H6, "Total uncorrected errors (The number of data sets that could not be read after retries)")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int32)
                    End With
                Case DefaultPages.HPLTO6_SequentialAccessDeviceLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Sequential Access Device log page", .PageCode = &HC, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 2,
                                    .TotalBits = 6,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Supported Page List",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(&H0, "Total channel write bytes. The number of data bytes received from application clients during write command operations. This is the number of bytes transferred over SCSI, before compression.")
                        .Add(&H1, "Total device write bytes. The number of data bytes written to the media as a result of write command operations, not counting the overhead from ECC and formatting. This is the number of data bytes transferred to media, after compression.")
                        .Add(&H2, "Total device read bytes. The number of data bytes read from the media during read command operations, not counting the overhead from ECC and formatting. This is the number of data bytes transferred from media with compression.")
                        .Add(&H3, "Total channel read bytes. The number of data bytes transferred to the initiator or initiators during read command operations. This is the number of bytes transferred over SCSI, after decompression.")
                        .Add(&H4, "The approximate native capacity from BOP to EOD, in megabytes. ")
                        .Add(&H5, "The approximate native capacity between BOP and EW in the current partition in megabytes.")
                        .Add(&H6, "The minimum native capacity from EW and EOP in the current partition, in megabytes.")
                        .Add(&H7, "The approximate native capacity from BOP to the current position of the medium in megabytes.")
                        .Add(&H8, "The maximum native capacity that is currently allowed to be in the device object buffer, in megabytes.")
                        .Add(&H100, "Cleaning requested; a non-volatile cleaning indication.")
                        .Add(&H8000, "Total megabytes processed since cleaning. The Number of megabytes processed to tape since last cleaning (written after compression/read before decompression)")
                        .Add(&H8001, "Lifetime load cycles. This is the number of times the drive has been loaded in its lifetime.")
                        .Add(&H8002, "Lifetime cleaning cycles. This is the number of times over its lifetime the drive has been cleaned using a cleaner cartridge.")
                        .Add(&H8003, "Lifetime Power-on time. This is the number of seconds the drive has been powered on over its lifetime.")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H7, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H8, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H100, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H8000, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H8001, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H8002, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H8003, TapeUtils.PageData.DataItem.DataType.Int32)
                    End With
                Case DefaultPages.HPLTO6_TemperatureLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Temperature log page", .PageCode = &HD, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Temperature log parameter",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(&H0, "Temperature")
                        .Add(&H1, "Reference Temperature")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int16)
                    End With
                Case DefaultPages.HPLTO6_DataTransferDeviceStatusLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Data Transfer Device (DTD) Status log page", .PageCode = &H11, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "DTD Status log parameter",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})

                    With pdata.Items.Last.EnumTranslator
                        .Add(&H0, "Very High Frequency data")
                        .Add(&H1, "Very High Frequency polling delay")
                        .Add(&H2, "DT device ADC data encryption control status")
                        .Add(&H3, "Key management error data")
                        .Add(&H101, "DTD primary status - SAS/FC Port A")
                        .Add(&H102, "DTD primary status - SAS/FC Port B")
                        .Add(&H103, "DTD primary status - Fibre Channel NPIV port A")
                        .Add(&H104, "DTD primary status - Fibre Channel NPIV port B")
                        .Add(&H8000, "VU Very High Frequency data")
                        .Add(&H8003, "VU key management error")
                        .Add(&H8010, "VU extended VHF data")
                        .Add(&H8020, "VU multi-initiator conflict warning")
                        .Add(&HA101, "VU Fibre Channel port A failover status")
                        .Add(&HA102, "VU Fibre Channel port B failover status")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(&H0, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H1, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H2, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H3, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H101, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H102, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H103, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H104, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H8000, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H8003, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H8010, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H8020, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&HA101, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&HA102, TapeUtils.PageData.DataItem.DataType.PageData)
                    End With
                    With pdata.Items.Last.PageDataTemplate
                        Dim subPage As TapeUtils.PageData
                        subPage = New TapeUtils.PageData With {.PageCode = 0, .Name = "Very High Frequency data"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Prevent/Allow Medium Removal bit", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Initiated Unload bit", .StartByte = 0, .BitOffset = 1, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "MAM Accessible", .StartByte = 0, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Data Compression Enabled", .StartByte = 0, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Write Protect", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Clean Requested", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Cleaning Required", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DTD Initialized", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "In Transition", .StartByte = 1, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Robotic Access Allowed", .StartByte = 1, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Present", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Seated", .StartByte = 1, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Threaded", .StartByte = 1, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Data Accessible", .StartByte = 1, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DT Device Activity", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subPage.Items.Last.EnumTranslator
                            .Add(&H0, "No tape motion")
                            .Add(&H1, "Cleaning operation in progress")
                            .Add(&H2, "Tape being loaded")
                            .Add(&H3, "Tape being unloaded")
                            .Add(&H4, "Other tape activity")
                            .Add(&H5, "Reading")
                            .Add(&H6, "Writing")
                            .Add(&H7, "Locating")
                            .Add(&H8, "Rewinding")
                            .Add(&H9, "Erasing")
                            .Add(&HC, "Other DT device activity")
                            .Add(&HD, "Microcode update in progress")
                            .Add(&HE, "Reading encrypted data from tape")
                            .Add(&HF, "Writing encrypted data to tape")
                        End With
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "VU Extended VHF Data log parameter changed", .StartByte = 3, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Tape Diagnostic Data Entry Created", .StartByte = 3, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Parameters Present", .StartByte = 3, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Service Request", .StartByte = 3, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Recovery Requested", .StartByte = 3, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Interface Changed", .StartByte = 3, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "TapeAlert flag has changed", .StartByte = 3, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        .Add(&H0, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = 1, .Name = "Very High Frequency polling delay"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "VHF Polling Delay in ms", .StartByte = 0, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                        .Add(&H1, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = 2, .Name = "DT device ADC data encryption control status"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Aborted", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Key Management Error", .StartByte = 1, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Decryption Parameters Request", .StartByte = 1, .BitOffset = 1, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Parameters Request", .StartByte = 1, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Parameters Request Sequence Identifier", .StartByte = 2, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        .Add(&H2, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = 3, .Name = "Key management error data"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Error Type", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        subPage.Items.Last.EnumTranslator.Add(&H0, "No error")
                        subPage.Items.Last.EnumTranslator.Add(&H1, "Encryption parameters request error")
                        subPage.Items.Last.EnumTranslator.Add(&H2, "Decryption parameters request error")
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Key Timeout", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Parameters Request Sequence Identifier", .StartByte = 2, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        .Add(&H3, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H101, .Name = "DTD primary status - SAS/FC Port A"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Topology", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Speed", .StartByte = 0, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subPage.Items.Last.EnumTranslator
                            .Add(&H0, "1 Gbps")
                            .Add(&H1, "2 Gbps")
                            .Add(&H2, "4 Gbps")
                            .Add(&H3, "8 Gbps")
                            .Add(&H4, "16 Gbps")
                            .Add(&H5, "32 Gbps")
                            .Add(&H6, "64 Gbps")
                            .Add(&H7, "128 Gbps")
                        End With
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Login Complete", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC AL_PA conflict", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Signal", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Port Initialization Complete", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current N_Port ID", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current FC_AL Loop ID", .StartByte = 7, .BitOffset = 1, .TotalBits = 7, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Port Name", .StartByte = 8, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Node Name", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Negotiated Physical Link Rate", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Current Hashed Address", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Address", .StartByte = 4, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        .Add(&H101, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H102, .Name = "DTD primary status - SAS/FC Port B"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Topology", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Speed", .StartByte = 0, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subPage.Items.Last.EnumTranslator
                            .Add(&H0, "1 Gbps")
                            .Add(&H1, "2 Gbps")
                            .Add(&H2, "4 Gbps")
                            .Add(&H3, "8 Gbps")
                            .Add(&H4, "16 Gbps")
                            .Add(&H5, "32 Gbps")
                            .Add(&H6, "64 Gbps")
                            .Add(&H7, "128 Gbps")
                        End With
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Login Complete", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC AL_PA conflict", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Signal", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Port Initialization Complete", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current N_Port ID", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current FC_AL Loop ID", .StartByte = 7, .BitOffset = 1, .TotalBits = 7, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Port Name", .StartByte = 8, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Node Name", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Negotiated Physical Link Rate", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Current Hashed Address", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Address", .StartByte = 4, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        .Add(&H102, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H103, .Name = "DTD primary status - Fibre Channel NPIV port A"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Topology", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Speed", .StartByte = 0, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subPage.Items.Last.EnumTranslator
                            .Add(&H0, "1 Gbps")
                            .Add(&H1, "2 Gbps")
                            .Add(&H2, "4 Gbps")
                            .Add(&H3, "8 Gbps")
                            .Add(&H4, "16 Gbps")
                            .Add(&H5, "32 Gbps")
                            .Add(&H6, "64 Gbps")
                            .Add(&H7, "128 Gbps")
                        End With
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Login Complete", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC AL_PA conflict", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Signal", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Port Initialization Complete", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current N_Port ID", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current FC_AL Loop ID", .StartByte = 7, .BitOffset = 1, .TotalBits = 7, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Port Name", .StartByte = 8, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Node Name", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Negotiated Physical Link Rate", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Current Hashed Address", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Address", .StartByte = 4, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        .Add(&H103, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H104, .Name = "DTD primary status - Fibre Channel NPIV port B"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Topology", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Speed", .StartByte = 0, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subPage.Items.Last.EnumTranslator
                            .Add(&H0, "1 Gbps")
                            .Add(&H1, "2 Gbps")
                            .Add(&H2, "4 Gbps")
                            .Add(&H3, "8 Gbps")
                            .Add(&H4, "16 Gbps")
                            .Add(&H5, "32 Gbps")
                            .Add(&H6, "64 Gbps")
                            .Add(&H7, "128 Gbps")
                        End With
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Login Complete", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC AL_PA conflict", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Signal", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Port Initialization Complete", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current N_Port ID", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current FC_AL Loop ID", .StartByte = 7, .BitOffset = 1, .TotalBits = 7, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Port Name", .StartByte = 8, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "FC Current Node Name", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Negotiated Physical Link Rate", .StartByte = 0, .BitOffset = 0, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Current Hashed Address", .StartByte = 1, .BitOffset = 0, .TotalBits = 24, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "SAS Address", .StartByte = 4, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        .Add(&H104, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H8000, .Name = "VU Very High Frequency data"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Prevent/Allow Medium Removal bit", .StartByte = 0, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Initiated Unload bit", .StartByte = 0, .BitOffset = 1, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "MAM Accessible", .StartByte = 0, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Data Compression Enabled", .StartByte = 0, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Write Protect", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Clean Requested", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Cleaning Required", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DTD Initialized", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "In Transition", .StartByte = 1, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Robotic Access Allowed", .StartByte = 1, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Present", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Seated", .StartByte = 1, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Threaded", .StartByte = 1, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Data Accessible", .StartByte = 1, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DT Device Activity", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subPage.Items.Last.EnumTranslator
                            .Add(&H0, "No tape motion")
                            .Add(&H1, "Cleaning operation in progress")
                            .Add(&H2, "Tape being loaded")
                            .Add(&H3, "Tape being unloaded")
                            .Add(&H4, "Other tape activity")
                            .Add(&H5, "Reading")
                            .Add(&H6, "Writing")
                            .Add(&H7, "Locating")
                            .Add(&H8, "Rewinding")
                            .Add(&H9, "Erasing")
                            .Add(&HC, "Other DT device activity")
                            .Add(&HD, "Microcode update in progress")
                            .Add(&HE, "Reading encrypted data from tape")
                            .Add(&HF, "Writing encrypted data to tape")
                        End With
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "VU Extended VHF Data log parameter changed", .StartByte = 3, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Tape Diagnostic Data Entry Created", .StartByte = 3, .BitOffset = 2, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Parameters Present", .StartByte = 3, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Encryption Service Request", .StartByte = 3, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Recovery Requested", .StartByte = 3, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Interface Changed", .StartByte = 3, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "TapeAlert flag has changed", .StartByte = 3, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Login", .StartByte = 4, .BitOffset = 0, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Hardware Error", .StartByte = 4, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Media Error", .StartByte = 4, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Upgrade Cartridge", .StartByte = 4, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Loading", .StartByte = 4, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Unloading", .StartByte = 4, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Snapshot", .StartByte = 5, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Load Complete", .StartByte = 5, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Unload Complete", .StartByte = 5, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        .Add(&H8000, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H8003, .Name = "VU key management error data"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Key Timeout", .StartByte = 0, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Error Type", .StartByte = 0, .BitOffset = 5, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Sense Key", .StartByte = 4, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = SenseCodeTranslator
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code", .StartByte = 5, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = AdditionalSenseCodeTranslator
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code Qualifier", .StartByte = 6, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        .Add(&H8003, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H8010, .Name = "VU extended VHF data"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Multi-Initiator Conflict Warning", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Snapshot", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Hibernate Mode", .StartByte = 3, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Legacy Reservations Changed", .StartByte = 3, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Persistent Reservations Changed", .StartByte = 3, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Prevent Allow Medium Removal Changed", .StartByte = 3, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        .Add(&H8010, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H8020, .Name = "VU multi-initiator conflict warning"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Name 1 (previous)", .StartByte = 0, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Text})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Host Name 2 (latest)", .StartByte = 8, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Text})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Operation Code", .StartByte = 16, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Service Action", .StartByte = 17, .BitOffset = 3, .TotalBits = 5, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        .Add(&H8020, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &HA101, .Name = "VU Fibre Channel port A failover status"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Active", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Failover Trigger", .StartByte = 1, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subPage.Items.Last.EnumTranslator
                            .Add(&H0, "No failover trigger has been detected.")
                            .Add(&H1, "A signal loss failover trigger was detected.")
                            .Add(&H2, "A link error threshold exceeded trigger was detected.")
                            .Add(&H3, "A command transport error threshold exceeded trigger was detected.")
                        End With
                        .Add(&HA101, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &HA102, .Name = "VU Fibre Channel port B failover status"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Active", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Failover Trigger", .StartByte = 1, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subPage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subPage.Items.Last.EnumTranslator
                            .Add(&H0, "No failover trigger has been detected.")
                            .Add(&H1, "A signal loss failover trigger was detected.")
                            .Add(&H2, "A link error threshold exceeded trigger was detected.")
                            .Add(&H3, "A command transport error threshold exceeded trigger was detected.")
                        End With
                        .Add(&HA102, subPage)
                    End With
                Case DefaultPages.HPLTO6_TapeAlertResponseLogPage
                    pdata = New TapeUtils.PageData With {.Name = "TapeAlert Response log page", .PageCode = &H12, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})

                    For i As Integer = 0 To 7
                        For j As Integer = 0 To 7
                            Dim TAFValue As String = ""
                            If TapeAlertFlag.TryGetValue(i * 8 + j, TAFValue) Then TAFValue = $" {TAFValue}"
                            pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                                    .Parent = pdata,
                                                    .Name = $"Flag {Hex(i * 8 + j).ToUpper().PadLeft(2, "0")}h{TAFValue}",
                                                    .StartByte = i + 8,
                                                    .BitOffset = j,
                                                    .TotalBits = 1,
                                                    .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                            pdata.Items.Last.EnumTranslator = TapeAlertFlag
                        Next
                    Next
                Case DefaultPages.HPLTO6_RequestedRecoveryLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Requested Recovery log page", .PageCode = &H13, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Recovery Procedure",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                    pdata.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                    With pdata.Items.Last.EnumTranslator
                        .Add(&H0, "No recovery requested")
                        .Add(&HD, "Request creation of a DT device error log")
                        .Add(&HE, "Retrieve a DT device error log")
                        .Add(&HF, "Modify the configuration to allow microcode update")
                    End With
                Case DefaultPages.HPLTO6_DeviceStatisticsLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Device Statistics log page", .PageCode = &H14, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "DS",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 1,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "SPF",
                                    .StartByte = 0,
                                    .BitOffset = 1,
                                    .TotalBits = 1,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 2,
                                    .TotalBits = 6,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Subcode Page",
                                    .StartByte = 1,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Device Statistics log parameter",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(&H0, "Lifetime volume loads")
                        .Add(&H1, "Lifetime cleaning operations")
                        .Add(&H2, "Lifetime power-on hours")
                        .Add(&H3, "Lifetime media motion (head) hours")
                        .Add(&H4, "Lifetime meters of tape processed")
                        .Add(&H5, "Lifetime medium motion (head) hours when an incompatible volume was last loaded")
                        .Add(&H6, "Lifetime power-on hours when the last temperature condition occurred (TapeAlert code 24h)")
                        .Add(&H7, "Lifetime power-on hours when the last power consumption condition occurred (TapeAlert code 1Ch)")
                        .Add(&H8, "Medium motion (head) hours since the last successful cleaning operation")
                        .Add(&H9, "Medium motion (head) hours since the second to last successful cleaning operation")
                        .Add(&HA, "Medium motion (head) hours since the third to last successful cleaning operation")
                        .Add(&HB, "Lifetime power-on hours when the last operator-initiated forced reset or emergency eject occurred")
                        .Add(&HC, "Lifetime power cycles")
                        .Add(&HD, "Volume loads since last parameter reset")
                        .Add(&HE, "Hard write errors")
                        .Add(&HF, "Hard read errors")
                        .Add(&H10, "Duty cycle sample time (time in milliseconds since last hard reset)")
                        .Add(&H11, "Read duty cycle (percentage of duty cycle spent processing read-type commands)")
                        .Add(&H12, "Write duty cycle (percentage of duty cycle spent processing write-type commands)")
                        .Add(&H13, "Activity duty cycle (percentage of duty cycle spent processing commands that move the medium)")
                        .Add(&H14, "Volume not present duty cycle (percentage of duty cycle when there is no volume present)")
                        .Add(&H15, "Ready duty cycle (percentage of the duty cycle sample time when the drive is in the ready state)")
                        .Add(&H16, "MB transferred from the application client in the duty cycle sample time")
                        .Add(&H17, "MB transferred to the application client in the duty cycle sample time")
                        .Add(&H40, "Drive manufacturer’s serial number")
                        .Add(&H41, "Drive serial number")
                        .Add(&H80, "Medium removal prevented")
                        .Add(&H81, "Max recommended mechanism temperature exceeded")
                        .Add(&H1000, "Medium motion (head) hours for each medium type")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H7, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H8, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H9, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&HA, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&HB, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&HC, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&HD, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&HE, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&HF, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H10, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H11, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H12, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H13, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H14, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H15, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H16, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H17, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H40, TapeUtils.PageData.DataItem.DataType.Text)
                        .Add(&H41, TapeUtils.PageData.DataItem.DataType.Text)
                        .Add(&H80, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H81, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H1000, TapeUtils.PageData.DataItem.DataType.PageData)
                    End With

                    With pdata.Items.Last.PageDataTemplate
                        Dim subPage As TapeUtils.PageData
                        subPage = New TapeUtils.PageData With {.PageCode = 0, .Name = "Device statistics medium type log parameter"}
                        For i As Integer = 0 To 19
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Density Code", .StartByte = 2 + 8 * i, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                            subPage.Items.Last.EnumTranslator = DensityCodeTranslator
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Medium Type", .StartByte = 3 + 8 * i, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                            subPage.Items.Last.EnumTranslator = MediumTypeTranslator
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Medium Motion Hours", .StartByte = 4 + 8 * i, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        Next
                        .Add(&H1000, subPage)
                    End With
                Case DefaultPages.HPLTO6_ServiceBuffersInformationLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Service Buffers Information Log page", .PageCode = &H15, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Service Buffers",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(0, "DT Device Error log")
                        .Add(3, "Health and Error log")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(&H0, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H3, TapeUtils.PageData.DataItem.DataType.PageData)
                    End With
                    With pdata.Items.Last.PageDataTemplate
                        Dim subPage As TapeUtils.PageData
                        subPage = New TapeUtils.PageData With {.PageCode = 0, .Name = "DT Device Error Log service buffer"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Temporarily Unavailable", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "DT Device Error Log Service Buffer", .StartByte = 4, .BitOffset = 0, .TotalBits = 160, .Type = TapeUtils.PageData.DataItem.DataType.Text})
                        .Add(&H0, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = 3, .Name = "Health and Error Log service buffer"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Temporarily Unavailable", .StartByte = 1, .BitOffset = 3, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Health & Error Log Service Buffer", .StartByte = 4, .BitOffset = 0, .TotalBits = 160, .Type = TapeUtils.PageData.DataItem.DataType.Text})
                        .Add(&H3, subPage)
                    End With
                Case DefaultPages.HPLTO6_TapeDiagnosticLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Tape Diagnostic log page", .PageCode = &H16, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Tape diagnostic data log parameters",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    For i As Integer = 0 To 59
                        With pdata.Items.Last.EnumTranslator
                            .Add(i, $"Parameter Code {Hex(i).ToUpper().PadLeft(4, "0")}h")
                        End With
                        With pdata.Items.Last.DynamicParamType
                            .Add(i, TapeUtils.PageData.DataItem.DataType.PageData)
                        End With
                        With pdata.Items.Last.PageDataTemplate
                            Dim subPage As TapeUtils.PageData
                            subPage = New TapeUtils.PageData With {.PageCode = i, .Name = $"Parameter Code {Hex(i).ToUpper().PadLeft(4, "0")}h"}
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Density Code", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Density Code Representation", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                            subPage.Items.Last.EnumTranslator = DensityCodeTranslator
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Medium Type", .StartByte = 3, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Medium Type Representation", .StartByte = 3, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                            subPage.Items.Last.EnumTranslator = MediumTypeTranslator
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Lifetime Medium Motion Hours", .StartByte = 4, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code", .StartByte = 10, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code Representation", .StartByte = 10, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                            subPage.Items.Last.EnumTranslator = AdditionalSenseCodeTranslator
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Additional Sense Code Qualifier", .StartByte = 11, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Drive Error Code", .StartByte = 12, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Product Revision Level", .StartByte = 16, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Hours Since Last Clean", .StartByte = 20, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Opcode", .StartByte = 24, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Service Action", .StartByte = 25, .BitOffset = 3, .TotalBits = 5, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Medium Identifier", .StartByte = 28, .BitOffset = 0, .TotalBits = 256, .Type = TapeUtils.PageData.DataItem.DataType.RawData})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = "Medium Identifier", .StartByte = 62, .BitOffset = 0, .TotalBits = 48, .Type = TapeUtils.PageData.DataItem.DataType.Int64})

                            .Add(i, subPage)
                        End With
                    Next
                Case DefaultPages.HPLTO6_VolumeStatisticsLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Volume Statistics Log page", .PageCode = &H17, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Volume Statistics log parameters",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(&H0, "Page valid")
                        .Add(&H1, "Thread count")
                        .Add(&H2, "Total data sets written")
                        .Add(&H3, "Total write retries")
                        .Add(&H4, "Total unrecovered write errors")
                        .Add(&H5, "Total suspended writes")
                        .Add(&H6, "Total fatal suspended writes")
                        .Add(&H7, "Total datasets read")
                        .Add(&H8, "Total read retries")
                        .Add(&H9, "Total unrecovered read errors")
                        .Add(&HC, "Last mount unrecovered write errors")
                        .Add(&HD, "Last mount unrecovered read errors")
                        .Add(&HE, "Last mount megabytes written")
                        .Add(&HF, "Last mount megabytes read")
                        .Add(&H10, "Lifetime megabytes written")
                        .Add(&H11, "Lifetime megabytes read")
                        .Add(&H12, "Last load write compression ratio")
                        .Add(&H13, "Last load read compression ratio")
                        .Add(&H14, "Medium mount time")
                        .Add(&H15, "Medium ready time")
                        .Add(&H16, "Total native capacity")
                        .Add(&H17, "Total used native capacity")
                        .Add(&H40, "Volume serial number")
                        .Add(&H41, "Tape lot identifier")
                        .Add(&H42, "Volume barcode")
                        .Add(&H43, "Volume manufacturer")
                        .Add(&H44, "Volume license code")
                        .Add(&H45, "Volume personality")
                        .Add(&H46, "Volume manufacture date")
                        .Add(&H80, "Write protect")
                        .Add(&H81, "Volume is WORM")
                        .Add(&H82, "Maximum recommended tape path temperature exceeded")
                        .Add(&H101, "Beginning of medium passes")
                        .Add(&H102, "Middle of medium passes")
                        .Add(&H200, "First encrypted logical object identifiers")
                        .Add(&H201, "First unencrypted logical object on the EOP side of the first encrypted logical object identifiers")
                        .Add(&H202, "Approximate native capacity of partitions")
                        .Add(&H203, "Approximate used native capacity of partitions")
                        .Add(&H300, "Mount history")
                        .Add(&HF000, "Version number (vendor-unique)")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(&H0, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H1, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H2, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H3, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H4, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H5, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H6, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H7, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H8, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H9, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&HC, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&HD, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&HE, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&HF, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H10, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H11, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H12, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H13, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H14, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H15, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(&H16, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H17, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H40, TapeUtils.PageData.DataItem.DataType.Text)
                        .Add(&H41, TapeUtils.PageData.DataItem.DataType.Text)
                        .Add(&H42, TapeUtils.PageData.DataItem.DataType.Text)
                        .Add(&H43, TapeUtils.PageData.DataItem.DataType.Text)
                        .Add(&H44, TapeUtils.PageData.DataItem.DataType.Text)
                        .Add(&H45, TapeUtils.PageData.DataItem.DataType.Text)
                        .Add(&H46, TapeUtils.PageData.DataItem.DataType.Text)
                        .Add(&H80, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H81, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H82, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(&H101, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H102, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H200, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H201, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H202, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H203, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&H300, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(&HF000, TapeUtils.PageData.DataItem.DataType.Int16)
                    End With
                    With pdata.Items.Last.PageDataTemplate
                        Dim subPage As TapeUtils.PageData
                        subPage = New TapeUtils.PageData With {.PageCode = &H200, .Name = "First encrypted logical object identifiers"}
                        For i As Integer = 0 To 7
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Number", .StartByte = 2 + 12 * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Record Data Counter", .StartByte = 4 + 12 * i, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Int64})
                        Next
                        .Add(&H200, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H201, .Name = "First unencrypted logical object on the EOP side of the first encrypted logical object identifiers"}
                        For i As Integer = 0 To 7
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Number", .StartByte = 2 + 12 * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Record Data Counter", .StartByte = 4 + 12 * i, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Int64})
                        Next
                        .Add(&H201, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H202, .Name = "Approximate native capacity of partitions"}
                        For i As Integer = 0 To 7
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Number", .StartByte = 2 + 8 * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Record Data Counter", .StartByte = 4 + 8 * i, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        Next
                        .Add(&H202, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H203, .Name = "Approximate used native capacity of partitions"}
                        For i As Integer = 0 To 7
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Number", .StartByte = 2 + 8 * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Partition Record Data Counter", .StartByte = 4 + 8 * i, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        Next
                        .Add(&H203, subPage)
                        subPage = New TapeUtils.PageData With {.PageCode = &H300, .Name = "Mount history"}
                        For i As Integer = 0 To 3
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Mount History Index", .StartByte = 2 + &H2C * i, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Mount History Vendor ID", .StartByte = 4 + &H2C * i, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Text})
                            subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Mount History Unit Serial Number", .StartByte = 12 + &H2C * i, .BitOffset = 0, .TotalBits = 256, .Type = TapeUtils.PageData.DataItem.DataType.Text})
                        Next
                        .Add(&H300, subPage)
                    End With
                Case DefaultPages.HPLTO6_ProtocolSpecificPortLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Protocol-Specific Port Log page (SAS drives only)", .PageCode = &H18, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Protocol-Specific Log Parameter",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(0, "Protocol-Specific Log Parameter 0")
                        .Add(1, "Protocol-Specific Log Parameter 1")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(0, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(1, TapeUtils.PageData.DataItem.DataType.PageData)
                    End With
                    With pdata.Items.Last.PageDataTemplate
                        Dim subPage As TapeUtils.PageData
                        subPage = New TapeUtils.PageData With {.PageCode = 0, .Name = "Protocol-specific log parameters"}
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Generation Code", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"PHY Identifier", .StartByte = 5, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached Device Type", .StartByte = 8, .BitOffset = 1, .TotalBits = 3, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached Reason", .StartByte = 8, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Negotiated Physical Link Rate", .StartByte = 9, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SSP Initiator Port", .StartByte = 10, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached STP Initiator Port", .StartByte = 10, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SMP Initiator Port", .StartByte = 10, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SSP Target Port", .StartByte = 11, .BitOffset = 4, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached STP Target Port", .StartByte = 11, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SMP Target Port", .StartByte = 11, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"SAS Address", .StartByte = 12, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached SAS Address", .StartByte = 20, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Attached PHY Identifier", .StartByte = 28, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Invalid DWORD Count", .StartByte = 36, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Running Disparity Error Count", .StartByte = 40, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"Loss of DWORD synchronization", .StartByte = 44, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        subPage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subPage, .Name = $"PHY Reset Problem Count", .StartByte = 48, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        .Add(0, subPage)
                        .Add(1, subPage)
                    End With
                Case DefaultPages.HPLTO6_DataCompressionLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Data Compression log page", .PageCode = &H1B, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Data Compression Parameter",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(0, "Read compression ratio")
                        .Add(1, "Write compression ratio")
                        .Add(2, "Megabytes transferred to host")
                        .Add(3, "Bytes transferred to host")
                        .Add(4, "Megabytes read from tape")
                        .Add(5, "Bytes read from tape")
                        .Add(6, "Megabytes transferred from host")
                        .Add(7, "Bytes transferred from host")
                        .Add(8, "Megabytes written to tape")
                        .Add(9, "Bytes written to tape")
                        .Add(&H100, "Dara compression enabled")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(0, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(1, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(2, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(4, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(5, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(6, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(7, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(8, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(9, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H100, TapeUtils.PageData.DataItem.DataType.Boolean)
                    End With
                Case DefaultPages.HPLTO6_TapeAlertLogPage
                    pdata = New TapeUtils.PageData With {.Name = "TapeAlert log page", .PageCode = &H2E, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Data Compression Parameter",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = TapeAlertFlag,
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.DynamicParamType
                        .Add(1, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(2, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(3, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(4, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(5, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(6, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(7, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(8, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(9, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(10, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(11, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(12, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(13, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(14, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(15, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(16, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(17, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(18, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(19, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(20, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(21, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(22, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(23, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(24, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(25, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(26, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(27, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(28, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(29, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(30, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(31, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(32, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(33, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(34, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(35, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(36, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(37, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(38, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(39, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(49, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(50, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(51, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(52, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(53, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(54, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(55, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(56, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(57, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(58, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(59, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(60, TapeUtils.PageData.DataItem.DataType.Boolean)
                        .Add(61, TapeUtils.PageData.DataItem.DataType.Boolean)
                    End With
                Case DefaultPages.HPLTO6_TapeUsageLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Tape Usage log page", .PageCode = &H30, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Tape Usage Log Parameter",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(1, "Thread Count")
                        .Add(2, "Total Data Sets Written")
                        .Add(3, "Total Write Retries")
                        .Add(4, "Total Unrecovered Write Errors")
                        .Add(5, "Total Suspended Writes")
                        .Add(6, "Total Fatal Suspended Writes")
                        .Add(7, "Total Data Sets Read")
                        .Add(8, "Total Read Retries")
                        .Add(9, "Total Unrecovered Read Errors")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(1, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(2, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(4, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(5, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(6, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(7, TapeUtils.PageData.DataItem.DataType.Int64)
                        .Add(8, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(9, TapeUtils.PageData.DataItem.DataType.Int16)
                    End With
                Case DefaultPages.HPLTO6_TapeCapacityLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Tape Capacity log page", .PageCode = &H31, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Code",
                        .StartByte = 0,
                        .BitOffset = 0,
                        .TotalBits = 8,
                        .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Page Length",
                        .StartByte = 2,
                        .BitOffset = 0,
                        .TotalBits = 16,
                        .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                        .Parent = pdata,
                        .Name = "Tape Capacity Log Parameter",
                        .StartByte = 4,
                        .BitOffset = 0,
                        .TotalBits = 0,
                        .DynamicParamCodeBitOffset = 0,
                        .DynamicParamCodeStartByte = 0,
                        .DynamicParamCodeTotalBits = 16,
                        .DynamicParamLenBitOffset = 0,
                        .DynamicParamLenStartByte = 3,
                        .DynamicParamLenTotalBits = 8,
                        .DynamicParamDataStartByte = 4,
                        .EnumTranslator = New SerializableDictionary(Of Long, String),
                        .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                        .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(1, "Partition 0 Remaining Capacity")
                        .Add(2, "Partition 1 Remaining Capacity")
                        .Add(3, "Partition 0 Maximum Capacity")
                        .Add(4, "Partition 1 Maximum Capacity")
                        .Add(5, "Partition 2 Remaining Capacity")
                        .Add(6, "Partition 3 Remaining Capacity")
                        .Add(7, "Partition 2 Maximum Capacity")
                        .Add(8, "Partition 3 Maximum Capacity")
                        .Add(9, "Partition 4 Remaining Capacity")
                        .Add(&HA, "Partition 5 Remaining Capacity")
                        .Add(&HB, "Partition 4 Maximum Capacity")
                        .Add(&HC, "Partition 5 Maximum Capacity")
                        .Add(&HD, "Partition 6 Remaining Capacity")
                        .Add(&HE, "Partition 7 Remaining Capacity")
                        .Add(&HF, "Partition 6 Maximum Capacity")
                        .Add(&H10, "Partition 7 Maximum Capacity")
                        .Add(&H11, "Partition 8 Remaining Capacity")
                        .Add(&H12, "Partition 9 Remaining Capacity")
                        .Add(&H13, "Partition 8 Maximum Capacity")
                        .Add(&H14, "Partition 9 Maximum Capacity")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(1, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(2, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(4, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(5, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(6, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(7, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(8, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(9, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&HA, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&HB, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&HC, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&HD, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&HE, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&HF, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H10, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H11, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H12, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H13, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(&H14, TapeUtils.PageData.DataItem.DataType.Int32)
                    End With
                Case DefaultPages.HPLTO6_DataCompressionHPLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Data Compression (HP-only) log page", .PageCode = &H32, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Data Compression Parameter",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(0, "Read compression ratio x100")
                        .Add(1, "Write compression ratio x100")
                        .Add(2, "Megabytes transferred to host")
                        .Add(3, "Bytes transferred to host")
                        .Add(4, "Megabytes read from tape")
                        .Add(5, "Bytes read from tape")
                        .Add(6, "Megabytes transferred from host")
                        .Add(7, "Bytes transferred from host")
                        .Add(8, "Megabytes written to tape")
                        .Add(9, "Bytes written to tape")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(0, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(1, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(2, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(4, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(5, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(6, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(7, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(8, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(9, TapeUtils.PageData.DataItem.DataType.Int32)
                    End With
                Case DefaultPages.HPLTO6_DeviceWellnessLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Device Wellness Log page", .PageCode = &H33, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    For i As Integer = 0 To 15
                        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = $"Parameter Code {i}",
                                    .StartByte = 4 + i * 16,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = $"Time Stamp",
                                    .StartByte = 8 + i * 16,
                                    .BitOffset = 0,
                                    .TotalBits = 32,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = $"Media Signature",
                                    .StartByte = 12 + i * 16,
                                    .BitOffset = 0,
                                    .TotalBits = 32,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = $"Sense Key",
                                    .StartByte = 16 + i * 16,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .EnumTranslator = SenseCodeTranslator,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = $"Additional Sense Code",
                                    .StartByte = 17 + i * 16,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .EnumTranslator = AdditionalSenseCodeTranslator,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = $"Additional Sense Qualifier",
                                    .StartByte = 18 + i * 16,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Byte})

                        pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = $"Additional Error Information",
                                    .StartByte = 19 + i * 16,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                    Next
                Case DefaultPages.HPLTO6_PerformanceDataLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Performance Data log page", .PageCode = &H34, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Performance Data Log Parameter",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(0, "Repositions per 100 MB")
                        .Add(1, "Data rate into buffer")
                        .Add(2, "Maximum data rate")
                        .Add(3, "Current data rate")
                        .Add(4, "Native data rate")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(0, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(1, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(2, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(3, TapeUtils.PageData.DataItem.DataType.Int16)
                        .Add(4, TapeUtils.PageData.DataItem.DataType.Int16)
                    End With
                Case DefaultPages.HPLTO6_DTDeviceErrorLogPage
                    pdata = New TapeUtils.PageData With {.Name = "DT Device Error log page", .PageCode = &H35, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "DT Device Error Log Parameters",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(0, "Hardware Error data")
                        .Add(1, "Media Error data")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(0, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(1, TapeUtils.PageData.DataItem.DataType.RawData)
                    End With
                    With pdata.Items.Last.PageDataTemplate
                        Dim subpage As TapeUtils.PageData
                        subpage = New TapeUtils.PageData With {.PageCode = 0, .Name = "Hardware Error data log parameter"}
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Sense Key", .StartByte = 0, .BitOffset = 4, .TotalBits = 4, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subpage.Items.Last.EnumTranslator = SenseCodeTranslator
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Additional Sense Code", .StartByte = 1, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subpage.Items.Last.EnumTranslator = AdditionalSenseCodeTranslator
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Additional Sense Code Qualifier", .StartByte = 2, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Hardware Error", .StartByte = 3, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Host Identification", .StartByte = 5, .BitOffset = 0, .TotalBits = 64, .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Power-on Count", .StartByte = 13, .BitOffset = 0, .TotalBits = 16, .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Power-on Time of Error (seconds)", .StartByte = 15, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Int32})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Library Time of Error Hour", .StartByte = 19, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Library Time of Error Minutes", .StartByte = 20, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Library Time of Error Seconds", .StartByte = 21, .BitOffset = 0, .TotalBits = 8, .Type = TapeUtils.PageData.DataItem.DataType.Byte})

                        .Add(0, subpage)
                    End With
                Case DefaultPages.HPLTO6_DeviceStatusLogPage
                    pdata = New TapeUtils.PageData With {.Name = "Device Status log page", .PageCode = &H3E, .RawData = logdata}
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Code",
                                    .StartByte = 0,
                                    .BitOffset = 0,
                                    .TotalBits = 8,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Binary})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "Page Length",
                                    .StartByte = 2,
                                    .BitOffset = 0,
                                    .TotalBits = 16,
                                    .Type = TapeUtils.PageData.DataItem.DataType.Int16})
                    pdata.Items.Add(New TapeUtils.PageData.DataItem With {
                                    .Parent = pdata,
                                    .Name = "DT Device Status Log Parameters",
                                    .StartByte = 4,
                                    .BitOffset = 0,
                                    .TotalBits = 0,
                                    .DynamicParamCodeBitOffset = 0,
                                    .DynamicParamCodeStartByte = 0,
                                    .DynamicParamCodeTotalBits = 16,
                                    .DynamicParamLenBitOffset = 0,
                                    .DynamicParamLenStartByte = 3,
                                    .DynamicParamLenTotalBits = 8,
                                    .DynamicParamDataStartByte = 4,
                                    .EnumTranslator = New SerializableDictionary(Of Long, String),
                                    .DynamicParamType = New SerializableDictionary(Of Long, TapeUtils.PageData.DataItem.DataType),
                                    .PageDataTemplate = New SerializableDictionary(Of Long, TapeUtils.PageData),
                                    .Type = TapeUtils.PageData.DataItem.DataType.DynamicPage})
                    With pdata.Items.Last.EnumTranslator
                        .Add(0, "Device Type")
                        .Add(1, "Device Status Bits")
                        .Add(2, "Total Number of Loads")
                        .Add(3, "Cleaning Cartridge Status")
                        .Add(4, "Product Number")
                    End With
                    With pdata.Items.Last.DynamicParamType
                        .Add(0, TapeUtils.PageData.DataItem.DataType.RawData)
                        .Add(1, TapeUtils.PageData.DataItem.DataType.PageData)
                        .Add(2, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(3, TapeUtils.PageData.DataItem.DataType.Int32)
                        .Add(4, TapeUtils.PageData.DataItem.DataType.PageData)
                    End With
                    With pdata.Items.Last.PageDataTemplate

                        Dim subpage As TapeUtils.PageData
                        subpage = New TapeUtils.PageData With {.PageCode = 1, .Name = "Device Status Bits"}
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Cleaning Required flag", .StartByte = 0, .BitOffset = 5, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Cleaning Requested flag", .StartByte = 0, .BitOffset = 6, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Exhausted Cleaning Tape flag", .StartByte = 0, .BitOffset = 7, .TotalBits = 1, .Type = TapeUtils.PageData.DataItem.DataType.Boolean})
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Temperature", .StartByte = 1, .BitOffset = 4, .TotalBits = 2, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subpage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subpage.Items.Last.EnumTranslator
                            .Add(0, "Field not supported")
                            .Add(1, "Temperature OK")
                            .Add(2, "Temperature degraded")
                            .Add(3, "Temperature failed")
                        End With
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Device Status", .StartByte = 1, .BitOffset = 6, .TotalBits = 2, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subpage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subpage.Items.Last.EnumTranslator
                            .Add(0, "Field not supported")
                            .Add(1, "Device status OK")
                            .Add(2, "Device status degraded")
                            .Add(3, "Device status failed")
                        End With
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Medium Status", .StartByte = 2, .BitOffset = 6, .TotalBits = 2, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subpage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subpage.Items.Last.EnumTranslator
                            .Add(0, "Field not supported")
                            .Add(1, "Medium status OK")
                            .Add(2, "Medium status degraded")
                            .Add(3, "Medium status failed")
                        End With
                        .Add(1, subpage)
                        subpage = New TapeUtils.PageData With {.PageCode = 4, .Name = "Product Number"}
                        subpage.Items.Add(New TapeUtils.PageData.DataItem With {.Parent = subpage, .Name = "Product Number", .StartByte = 0, .BitOffset = 0, .TotalBits = 32, .Type = TapeUtils.PageData.DataItem.DataType.Enum})
                        subpage.Items.Last.EnumTranslator = New SerializableDictionary(Of Long, String)
                        With subpage.Items.Last.EnumTranslator
                            .Add(&H109022C, "LTO-6 full-height FC standalone")
                            .Add(&H109022D, "LTO-6 full-height FC automation")
                            .Add(&H109022E, "LTO-6 full-height SAS standalone")
                            .Add(&H109022F, "LTO-6 full-height SAS automation")
                            .Add(&H1090230, "LTO-6 half-height FC standalone")
                            .Add(&H1090231, "LTO-6 half-height FC automation")
                            .Add(&H1090232, "LTO-6 half-height SAS standalone")
                            .Add(&H1090233, "LTO-6 half-height SAS automation")
                        End With
                        .Add(4, subpage)
                    End With
                Case Else
                    pdata = Nothing
            End Select
            Return pdata
        End Function
        Public Shared Function GetAllPagesFromDrive(handle As IntPtr) As List(Of PageData)
            Dim result As New List(Of PageData)
            For Each pagecode As Byte In [Enum].GetValues(GetType(DefaultPages))
                Dim logdata As Byte() = LogSense(handle, pagecode, 0)
                result.Add(PageData.CreateDefault(pagecode, logdata))
            Next
            Return result
        End Function
    End Class
    Public Shared Function ReduceDataUnit(MBytes As Int64) As String
        Dim Result As Decimal = MBytes
        Dim ResultUnit As Integer = 0
        While Result >= 1000
            Result /= 1024
            ResultUnit += 3
        End While
        Dim ResultString As String = Math.Round(Result, 2)
        Select Case ResultUnit
            Case 0
                Return ResultString & " MiB"
            Case 3
                Return ResultString & " GiB"
            Case 6
                Return ResultString & " TiB"
            Case 9
                Return ResultString & " PiB"
            Case 12
                Return ResultString & " EiB"
            Case 15
                Return ResultString & " ZiB"
            Case 18
                Return ResultString & " YiB"
            Case Else
                Return ResultString & " << " & ResultUnit & "MiB"
        End Select
    End Function

    Public Shared Property TagDictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)
End Class
