Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text

Public Class TapeUtils
#Region "winapi"
    Public Class SetupAPI
        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Ansi)>
        Public Structure SP_DEVINFO_DATA

            Public cbSize As Integer

            Public ClassGuid As Guid

            Public DevInst As Integer

            Public Reserved As IntPtr
        End Structure
        <StructLayout(LayoutKind.Sequential, Pack:=1, CharSet:=CharSet.Ansi)>
        Public Structure SP_DEVINFO_DETAIL_DATA

            Public cbSize As Integer

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
            Public Shared GUID_DEVINTERFACE_DISK As New Guid({&H53, &HF5, &H63, &H7, &HB6, &HBF, &H11, &HD0, &H94, &HF2, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_CDROM As New Guid({&H53, &HF5, &H63, &H8, &HB6, &HBF, &H11, &HD0, &H94, &HF2, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_PARTITION As New Guid({&H53, &HF5, &H63, &HA, &HB6, &HBF, &H11, &HD0, &H94, &HF2, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_TAPE As New Guid({&H53, &HF5, &H63, &HB, &HB6, &HBF, &H11, &HD0, &H94, &HF2, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_WRITEONCEDISK As New Guid({&H53, &HF5, &H63, &HC, &HB6, &HBF, &H11, &HD0, &H94, &HF2, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_VOLUME As New Guid({&H53, &HF5, &H63, &HD, &HB6, &HBF, &H11, &HD0, &H94, &HF2, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_MEDIUMCHANGER As New Guid({&H53, &HF5, &H63, &H10, &HB6, &HBF, &H11, &HD0, &H94, &HF2, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_FLOPPY As New Guid({&H53, &HF5, &H63, &H11, &HB6, &HBF, &H11, &HD0, &H94, &HF2, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_CDCHANGER As New Guid({&H53, &HF5, &H63, &H12, &HB6, &HBF, &H11, &HD0, &H94, &HF2, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_STORAGEPORT As New Guid({&H2A, &HCC, &HFE, &H60, &HC1, &H30, &H11, &HD2, &HB0, &H82, 0, &HA0, &HC9, &H1E, &HFB, &H8B})
            Public Shared GUID_DEVINTERFACE_VMLUN As New Guid({&H6F, &H41, &H66, &H19, &H9F, &H29, &H42, &HA5, &HB2, &HB, 37, &HE2, &H19, &HCA, &H2, &HB0})
            Public Shared GUID_DEVINTERFACE_SES As New Guid({&H17, &H90, &HC9, &HEC, &H47, &HD5, &H4D, &HF3, &HB5, &HAF, &H9A, &HDF, &H3C, &HF2, &H3E, &H48})
            Public Shared GUID_DEVINTERFACE_SERVICE_VOLUME As New Guid({&H6E, &HAD, &H3D, &H82L, &H25, &HEC, &H46, &HBC, &HB7, &HFD, &HC1, &HF0, &HDF, &H8F, &H50, &H37})
            Public Shared GUID_DEVINTERFACE_HIDDEN_VOLUME As New Guid({&H7F, &H10, &H8A, &H28L, &H98, &H33, &H4B, &H3B, &HB7, &H80, &H2C, &H6B, &H5F, &HA5, &HC0, &H62})
            Public Shared GUID_DEVINTERFACE_UNIFIED_ACCESS_RPMB As New Guid({&H27, &H44, &H7C, &H21L, &HBC, &HC3, &H4D, &H7, &HA0, &H5B, &HA3, &H39, &H5B, &HB4, &HEE, &HE7})
            Public Shared GUID_DEVINTERFACE_COMPORT As New Guid({&H86, &HE0, &HD1, &HE0L, &H80, &H89, &H11, &HD0, &H9C, &HE4, &H8, &H0, &H3E, &H30, &H1F, &H73})
            Public Shared GUID_DEVINTERFACE_SERENUM_BUS_ENUMERATOR As New Guid({&H4D, &H36, &HE9, &H78L, &HE3, &H25, &H11, &HCE, &HBF, &HC1, &H8, &H0, &H2B, &HE1, &H3, &H18})
        End Class
        <DllImport("setupapi.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Winapi)>
        Public Shared Function SetupDiGetClassDevs(
            ByRef classGuid As Guid,
            ByVal Enumerator As String,
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
            ByVal DeviceInfoData As SP_DEVINFO_DATA,
            ByVal InterfaceClassGuid As Guid,
            ByVal MemberIndex As UInteger,
            ByRef DeviceInterfaceData As SP_DEVICE_INTERFACE_DATA) As Boolean
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
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _ScsiIoControl(hFile As IntPtr,
                                           deviceNumber As UInt32,
                                           cdb As UIntPtr,
                                           cdbLength As Byte,
                                           dataBuffer As UIntPtr,
                                           bufferLength As UInt32,
                                           dataIn As Byte,
                                           timeoutValue As UInt32,
                                           senseBuffer As UIntPtr) As Boolean

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function _TapeSCSIIOCtlFull(TapeDrive As String,
                                           cdb As IntPtr,
                                           cdbLength As Byte,
                                           dataBuffer As IntPtr,
                                           bufferLength As UInt32,
                                           dataIn As Byte,
                                           timeoutValue As UInt32,
                                           senseBuffer As IntPtr) As Boolean

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
    Public Shared Function _TapeSCSIIOCtl(TapeDrive As String, SCSIOPCode As Byte) As IntPtr

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function _TapeDeviceIOCtl(TapeDrive As String, DWIOCode As UInt32) As IntPtr

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function _Test(ByVal a As Char) As IntPtr

    End Function
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
    Public Shared Function ReadRemainingCapacity(TapeDrive As String, Optional ByVal Partition As Byte = 0) As UInt64
        Return MAMAttribute.FromTapeDrive(TapeDrive, &H0, Partition).AsNumeric
    End Function
    Public Class BlockLimits
        Public MaximumBlockLength As UInt64
        Public MinimumBlockLength As UInt16
        Public Shared Widening Operator CType(data As BlockLimits) As UInt64
            Return data.MaximumBlockLength
        End Operator
    End Class
    Public Shared Property GlobalBlockLimit As Integer = 524288

    Public Shared Function ReadBlockLimits(TapeDrive As String) As BlockLimits
        Dim data As Byte() = SCSIReadParam(TapeDrive, {5, 0, 0, 0, 0, 0}, 6)
        Return New BlockLimits With {.MaximumBlockLength = CULng(data(1)) << 16 Or CULng(data(2)) << 8 Or data(3),
            .MinimumBlockLength = CUShort(data(4)) << 8 Or data(5)}
    End Function
    Public Shared Function ReadBuffer(TapeDrive As String, BufferID As Byte) As Byte()
        'Get EEPROM buffer Length
        Dim cdbD0 As Byte() = {&H3C, 3, BufferID, 0, 0, 0, 0, 0, 4, 0}
        Dim lenData As Byte() = {0, 0, 0, 0}
        Dim cdb0 As IntPtr = Marshal.AllocHGlobal(cdbD0.Length)
        Marshal.Copy(cdbD0, 0, cdb0, cdbD0.Length)
        Dim data0 As IntPtr = Marshal.AllocHGlobal(lenData.Length)
        Marshal.Copy(lenData, 0, data0, lenData.Length)
        Dim sense As IntPtr = Marshal.AllocHGlobal(64)
        TapeUtils._TapeSCSIIOCtlFull(TapeDrive, cdb0, cdbD0.Length, data0, lenData.Length, 1, &HFFFF, sense)
        Marshal.Copy(data0, lenData, 0, lenData.Length)
        Marshal.FreeHGlobal(cdb0)
        Marshal.FreeHGlobal(data0)
        Dim BufferLen As Integer
        For i As Integer = 1 To lenData.Length - 1
            BufferLen <<= 8
            BufferLen = BufferLen Or lenData(i)
        Next

        'Dump EEPROM
        Dim cdbD1 As Byte() = {&H3C, 2, BufferID, 0, 0, 0, lenData(1), lenData(2), lenData(3), 0}
        Dim cdb1 As IntPtr = Marshal.AllocHGlobal(cdbD1.Length)
        Marshal.Copy(cdbD1, 0, cdb1, cdbD1.Length)
        Dim dumpData(BufferLen - 1) As Byte
        Dim data1 As IntPtr = Marshal.AllocHGlobal(dumpData.Length)
        Marshal.Copy(dumpData, 0, data1, dumpData.Length)
        TapeUtils._TapeSCSIIOCtlFull(TapeDrive, cdb1, cdbD1.Length, data1, dumpData.Length, 1, &HFFFF, sense)
        Marshal.Copy(data1, dumpData, 0, dumpData.Length)
        Marshal.FreeHGlobal(cdb1)
        Marshal.FreeHGlobal(data1)
        Marshal.FreeHGlobal(sense)
        Return dumpData
    End Function
    Public Shared Function ReadBlock(TapeDrive As String, Optional ByRef sense As Byte() = Nothing, Optional ByVal BlockSizeLimit As UInteger = &H80000, Optional ByVal Truncate As Boolean = False) As Byte()
        Dim senseRaw(63) As Byte
        BlockSizeLimit = Math.Min(BlockSizeLimit, GlobalBlockLimit)
        If sense Is Nothing Then sense = {}

        Dim RawDataU As IntPtr = SCSIReadParamUnmanaged(TapeDrive, {8, 0, BlockSizeLimit >> 16 And &HFF, BlockSizeLimit >> 8 And &HFF, BlockSizeLimit And &HFF, 0},
                                              BlockSizeLimit, Function(senseData As Byte()) As Boolean
                                                                  senseRaw = senseData
                                                                  Return True
                                                              End Function)
        sense = senseRaw
        Dim DiffBytes As Int32
        For i As Integer = 3 To 6
            DiffBytes <<= 8
            DiffBytes = DiffBytes Or sense(i)
        Next
        If Truncate Then DiffBytes = Math.Max(DiffBytes, 0)
        Dim DataLen As Integer = BlockSizeLimit - DiffBytes
        If Not Truncate Then
            Marshal.FreeHGlobal(RawDataU)
            Dim p As New PositionData(TapeDrive)
            Locate(TapeDrive, p.BlockNumber - 1, p.PartitionNumber)
            Return ReadBlock(TapeDrive, sense, DataLen, Truncate)
        End If
        Dim RawData(DataLen - 1) As Byte
        Marshal.Copy(RawDataU, RawData, 0, Math.Min(BlockSizeLimit, DataLen))
        Marshal.FreeHGlobal(RawDataU)
        Return RawData
    End Function
    Public Shared Function ReadFileMark(TapeDrive As String, Optional ByRef sense As Byte() = Nothing) As Boolean
        Dim data As Byte() = ReadBlock(TapeDrive, sense)
        If data.Length = 0 Then Return True
        Dim p As New PositionData(TapeDrive)
        Locate(TapeDrive, p.BlockNumber - 1, p.PartitionNumber)
        Return False
    End Function
    Public Shared Function ReadToFileMark(TapeDrive As String, Optional ByVal BlockSizeLimit As UInteger = &H80000) As Byte()
        Dim param As Byte() = TapeUtils.SCSIReadParam(TapeDrive, {&H34, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 20)
        Dim buffer As New List(Of Byte)
        BlockSizeLimit = Math.Min(BlockSizeLimit, GlobalBlockLimit)
        While True
            Dim sense(63) As Byte
            Dim readData As Byte() = TapeUtils.ReadBlock(TapeDrive, sense, BlockSizeLimit)
            Dim Add_Key As UInt16 = CInt(sense(12)) << 8 Or sense(13)
            If readData.Length > 0 Then
                buffer.AddRange(readData)
            End If
            If (Add_Key >= 1 And Add_Key <> 4) Then
                Exit While
            End If
        End While
        Return buffer.ToArray()
    End Function
    Public Shared Function ReadToFileMark(TapeDrive As String, outputFileName As String, Optional ByVal BlockSizeLimit As UInteger = &H80000) As Boolean
        Dim param As Byte() = TapeUtils.SCSIReadParam(TapeDrive, {&H34, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 20)
        Dim buffer As New IO.FileStream(outputFileName, IO.FileMode.Create, IO.FileAccess.ReadWrite, IO.FileShare.Read)
        BlockSizeLimit = Math.Min(BlockSizeLimit, GlobalBlockLimit)
        While True
            Dim sense(63) As Byte
            Dim readData As Byte() = TapeUtils.ReadBlock(TapeDrive, sense, BlockSizeLimit)
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
    End Function
    Public Shared Function ReadEOWPosition(TapeDrive As String) As Byte()
        Dim lenData As Byte() = SCSIReadParam(TapeDrive, {&HA3, &H1F, &H45, 2, 0, 0, 0, 0, 0, 2, 0, 0}, 2)
        Dim len As UInt16 = lenData(0)
        len <<= 8
        len = len Or lenData(1)
        len += 2
        Dim rawParam As Byte() = SCSIReadParam(TapeDrive, {&HA3, &H1F, &H45, 2, 0, 0, 0, 0, len >> 8, len And &HFF, 0, 0}, len)
        Return rawParam.Skip(4).ToArray()
    End Function
    Public Enum LocateDestType
        Block = 0
        FileMark = 1
        EOD = 3
    End Enum
    Public Shared Function Locate(TapeDrive As String, BlockAddress As UInt64, Partition As Byte, Optional ByVal DestType As LocateDestType = 0) As UInt16
        Dim sense(63) As Byte
        'Dim d As Byte() = SCSIReadParam(TapeDrive, {&H2B, 2, 0,
        '                                BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
        '                                0, Partition, 0}, 64, Function(senseData As Byte()) As Boolean
        '                                                          sense = senseData
        '                                                          Return True
        '                                                      End Function)
        If AllowPartition OrElse DestType <> 0 Then
            Dim CP As Byte = 0
            If ReadPosition(TapeDrive).PartitionNumber <> Partition Then CP = 1
            SCSIReadParam(TapeDrive, {&H92, DestType << 3 Or CP << 1, 0, Partition,
                                            BlockAddress >> 56 And &HFF, BlockAddress >> 48 And &HFF, BlockAddress >> 40 And &HFF, BlockAddress >> 32 And &HFF,
                                            BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                            0, 0, 0, 0}, 64, Function(senseData As Byte()) As Boolean
                                                                 sense = senseData
                                                                 Return True
                                                             End Function)
        Else
            SCSIReadParam(TapeDrive, {&H2B, 0, 0, BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                            0, 0, 0}, 64, Function(senseData As Byte()) As Boolean
                                                              sense = senseData
                                                              Return True
                                                          End Function)
        End If

        Dim Add_Code As UInt16 = CInt(sense(12)) << 8 Or sense(13)
        If Add_Code <> 0 Then
            If DestType = LocateDestType.EOD Then
                If Not ReadPosition(TapeDrive).EOP Then
                    SendSCSICommand(TapeDrive, {&H11, 3, 0, 0, 0, 0}, Nothing, 1, Function(senseData As Byte()) As Boolean
                                                                                      sense = senseData
                                                                                      Return True
                                                                                  End Function)
                End If
            ElseIf DestType = LocateDestType.FileMark Then
                Locate(TapeDrive, 0, 0)
                Space6(TapeDrive, BlockAddress, LocateDestType.FileMark)
            Else
                SCSIReadParam(TapeDrive, {&H92, DestType << 3, 0, 0,
                                        BlockAddress >> 56 And &HFF, BlockAddress >> 48 And &HFF, BlockAddress >> 40 And &HFF, BlockAddress >> 32 And &HFF,
                                        BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                        0, 0, 0, 0}, 64, Function(senseData As Byte()) As Boolean
                                                             sense = senseData
                                                             Return True
                                                         End Function)

            End If
            Add_Code = CInt(sense(12)) << 8 Or sense(13)
        End If
        Return Add_Code
    End Function
    Public Shared Function Space6(TapeDrive As String, Count As Integer, Optional ByVal Code As LocateDestType = 0) As UInt16
        Dim sense(63) As Byte
        SCSIReadParam(TapeDrive, {&H11, Code, Count >> 16 And &HFF, Count >> 8 And &HFF, Count And &HFF,
                                            0}, 64, Function(senseData As Byte()) As Boolean
                                                        sense = senseData
                                                        Return True
                                                    End Function)
        Dim Add_Code As UInt16 = CInt(sense(12)) << 8 Or sense(13)
        Return Add_Code
    End Function
    Public Shared Function SCSIReadParam(TapeDrive As String, cdbData As Byte(), paramLen As Integer, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Byte()
        Dim cdb As IntPtr = Marshal.AllocHGlobal(cdbData.Length)
        Marshal.Copy(cdbData, 0, cdb, cdbData.Length)
        Dim paramData(paramLen - 1) As Byte
        Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(paramLen)
        Marshal.Copy(paramData, 0, dataBuffer, paramLen)
        Dim senseData(63) As Byte
        Dim senseBuffer As IntPtr = Marshal.AllocHGlobal(64)
        _TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, dataBuffer, paramLen, 1, 60000, senseBuffer)
        Marshal.Copy(dataBuffer, paramData, 0, paramLen)
        Marshal.Copy(senseBuffer, senseData, 0, 64)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(dataBuffer)
        Marshal.FreeHGlobal(senseBuffer)
        If senseReport IsNot Nothing Then senseReport(senseData)
        Return paramData
    End Function
    Public Shared Function SCSIReadParamUnmanaged(TapeDrive As String, cdbData As Byte(), paramLen As Integer, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As IntPtr
        Dim cdb As IntPtr = Marshal.AllocHGlobal(cdbData.Length)
        Marshal.Copy(cdbData, 0, cdb, cdbData.Length)
        Dim paramData(paramLen - 1) As Byte
        Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(paramLen)
        Marshal.Copy(paramData, 0, dataBuffer, paramLen)
        Dim senseData(63) As Byte
        Dim senseBuffer As IntPtr = Marshal.AllocHGlobal(64)
        While Not _TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, dataBuffer, paramLen, 1, 60000, senseBuffer)
            Marshal.Copy(senseBuffer, senseData, 0, 64)
            Select Case MessageBox.Show($"读取出错{vbCrLf}{ParseSenseData(senseData)}{vbCrLf}{vbCrLf}原始sense数据{vbCrLf}{Byte2Hex(senseData, True)}", "警告", MessageBoxButtons.AbortRetryIgnore)
                Case DialogResult.Abort
                    Throw New Exception("SCSI Error")
                Case DialogResult.Retry

                Case DialogResult.Ignore
                    Exit While
            End Select
        End While
        Marshal.Copy(senseBuffer, senseData, 0, 64)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(senseBuffer)
        If senseReport IsNot Nothing Then senseReport(senseData)
        Return dataBuffer
    End Function
    Public Shared Function ModeSense(TapeDrive As String, PageID As Byte, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Byte()
        Dim Header As Byte() = SCSIReadParam(TapeDrive, {&H1A, 0, PageID, 0, 4, 0}, 4)
        If Header.Length = 0 Then Return {0, 0, 0, 0}
        Dim PageLen As Byte = Header(0)
        If PageLen = 0 Then Return {0, 0, 0, 0}
        Dim DescripterLen As Byte = Header(3)
        Return SCSIReadParam(TapeDrive, {&H1A, 0, PageID, 0, PageLen + 1, 0}, PageLen + 1, senseReport).Skip(4 + DescripterLen).ToArray()
    End Function
    Public Shared Function ModeSelect(TapeDrive As String, PageData As Byte(), Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Byte()
        Dim data As Byte() = {0, 0, &H10, 0}.Concat(PageData)
        Dim sense(63) As Byte
        If data.Length < 256 Then
            SendSCSICommand(TapeDrive, {&H15, &H10, 0, 0, data.Length, 0}, data, 0,
                            Function(senseData As Byte()) As Boolean
                                sense = senseData
                                Return True
                            End Function)
        Else
            data = {0, 0, 0, &H10, 0, 0, 0, 0}.Concat(PageData)
            SendSCSICommand(TapeDrive, {&H55, &H10, 0, 0, 0, 0, 0, data.Length >> 8 And &HFF, data.Length And &HFF, 0}, data, 0,
                            Function(senseData As Byte()) As Boolean
                                sense = senseData
                                Return True
                            End Function)
        End If
        Return sense
    End Function
    Public Shared Function ReadDensityCode(TapeDrive As String) As Byte
        Return SCSIReadParam(TapeDrive, {&H1A, 0, 0, 0, &HC, 0}, 12)(4)
    End Function
    Public Shared Function SetBarcode(TapeDrive As String, barcode As String) As Boolean
        Dim cdb As Byte() = {&H8D, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, &H29, 0, 0}
        Dim data As Byte() = {0, 0, 0, &H29, &H8, &H6, &H1, 0, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20,
            &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20, &H20}
        barcode = barcode.PadRight(32).Substring(0, 32)
        For i As Integer = 0 To barcode.Length - 1
            data(9 + i) = CByte(Asc(barcode(i)) And &HFF)
        Next
        Return SendSCSICommand(TapeDrive, cdb, data, 0)
    End Function
    Public Shared Function SetBlockSize(TapeDrive As String, Optional ByVal BlockSize As UInteger = &H80000) As Byte()
        Dim sense(63) As Byte
        Dim DensityCode As Byte = ReadDensityCode(TapeDrive)
        BlockSize = Math.Min(BlockSize, GlobalBlockLimit)
        SendSCSICommand(TapeDrive, {&H15, &H10, 0, 0, &HC, 0},
                        {0, 0, &H10, 8, DensityCode, 0, 0, 0, 0, BlockSize >> 16 And &HFF, BlockSize >> 8 And &HFF, BlockSize And &HFF}, 0,
                        Function(senseData As Byte()) As Boolean
                            sense = senseData
                            Return True
                        End Function)
        Return sense
    End Function
    Public Enum AttributeFormat
        Binary = &H0
        Ascii = &H1
        Text = &H2
        Reserved = &H3
    End Enum
    Public Shared Function SetMAMAttribute(TapeDrive As String, PageID As UInt16, Data As Byte(), Optional ByVal Format As AttributeFormat = 0, Optional ByVal PartitionNumber As Byte = 0) As Boolean
        Dim Param_LEN As UInt64 = Data.Length + 9
        Dim cdb As Byte() = {&H8D, 0, 0, 0, 0, 0, 0, PartitionNumber, 0, 0,
                             Param_LEN >> 24 And &HFF, Param_LEN >> 16 And &HFF, Param_LEN >> 8 And &HFF, Param_LEN And &HFF, 0, 0}
        Dim param As Byte() = {Param_LEN >> 24 And &HFF, Param_LEN >> 16 And &HFF, Param_LEN >> 8 And &HFF, Param_LEN And &HFF,
                               PageID >> 8 And &HFF, PageID And &HFF, Format,
                               Data.Length >> 8 And &HFF, Data.Length And &HFF}
        param = param.Concat(Data).ToArray()
        Return SendSCSICommand(TapeDrive, cdb, param, 0)
    End Function
    Public Shared Function SetMAMAttribute(TapeDrive As String, PageID As UInt16, Data As String, Optional ByVal Format As AttributeFormat = 1, Optional ByVal PartitionNumber As Byte = 0) As Boolean
        Return SetMAMAttribute(TapeDrive, PageID, Encoding.UTF8.GetBytes(Data), Format, PartitionNumber)
    End Function
    Public Shared Function WriteVCI(TapeDrive As String, Generation As UInt64, block0 As UInt64, block1 As UInt64,
                                    UUID As String, Optional ByVal ExtraPartitionCount As Byte = 1) As Boolean
        Flush(TapeDrive)
        Dim VCIData As Byte()
        Dim VCI As Byte() = GetMAMAttributeBytes(TapeDrive, 0, 9)
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
            Dim Succ As Boolean = SetMAMAttribute(TapeDrive, &H80C, VCIData, AttributeFormat.Binary, 0)
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
        Return SetMAMAttribute(TapeDrive, &H80C, VCIData, AttributeFormat.Binary, ExtraPartitionCount)
    End Function
    Public Shared Function ParseAdditionalSenseCode(Add_Code As UInt16) As String
        Dim Msg As String = ""
        Select Case Add_Code
            Case &H0
                Msg &= "No addition sense"
            Case &H1
                Msg &= "Filemark detected"
            Case &H2
                Msg &= "End of Tape detected"
            Case &H4
                Msg &= "Beginning of Tape detected"
            Case &H5
                Msg &= "End of Data detected"
            Case &H16
                Msg &= "Operation in progress"
            Case &H18
                Msg &= "Erase operation in progress"
            Case &H19
                Msg &= "Locate operation in progress"
            Case &H1A
                Msg &= "Rewind operation in progress"
            Case &H400
                Msg &= "LUN not ready, cause not reportable"
            Case &H401
                Msg &= "LUN in process of becoming ready"
            Case &H402
                Msg &= "LUN not ready, Initializing command required"
            Case &H404
                Msg &= "LUN not ready, format in progress"
            Case &H407
                Msg &= "Command in progress"
            Case &H409
                Msg &= "LUN not ready, self-test in progress"
            Case &H40C
                Msg &= "LUN not accessible, port in unavailable state"
            Case &H412
                Msg &= "Logical unit offline"
            Case &H800
                Msg &= "Logical unit communication failure"
            Case &HB00
                Msg &= "Warning"
            Case &HB01
                Msg &= "Thermal limit exceeded"
            Case &HC00
                Msg &= "Write error"
            Case &HE01
                Msg &= "Information unit too short"
            Case &HE02
                Msg &= "Information unit too long"
            Case &HE03
                Msg &= "SK Illegal Request"
            Case &H1001
                Msg &= "Logical block guard check failed"
            Case &H1100
                Msg &= "Unrecovered read error"
            Case &H1112
                Msg &= "Media Auxiliary Memory read error"
            Case &H1400
                Msg &= "Recorded entity not found"
            Case &H1403
                Msg &= "End of Data not found"
            Case &H1A00
                Msg &= "Parameter list length error"
            Case &H2000
                Msg &= "Invalid command operation code"
            Case &H2400
                Msg &= "Invalid field in Command Descriptor Block"
            Case &H2500
                Msg &= "LUN not supported"
            Case &H2600
                Msg &= "Invalid field in parameter list"
            Case &H2601
                Msg &= "Parameter not supported"
            Case &H2602
                Msg &= "Parameter value invalid"
            Case &H2604
                Msg &= "Invalid release of persistent reservation"
            Case &H2610
                Msg &= "Data decryption key fail limit reached"
            Case &H2680
                Msg &= "Invalid CA certificate"
            Case &H2700
                Msg &= "Write-protected"
            Case &H2708
                Msg &= "Too many logical objects on partition to support operation"
            Case &H2800
                Msg &= "Not ready to ready transition, medium may have changed"
            Case &H2901
                Msg &= "Power-on reset"
            Case &H2902
                Msg &= "SCSI bus reset"
            Case &H2903
                Msg &= "Bus device reset"
            Case &H2904
                Msg &= "Internal firmware reboot"
            Case &H2907
                Msg &= "I_T nexus loss occurred"
            Case &H2A01
                Msg &= "Mode parameters changed"
            Case &H2A02
                Msg &= "Log parameters changed"
            Case &H2A03
                Msg &= "Reservations pre-empted"
            Case &H2A04
                Msg &= "Reservations released"
            Case &H2A05
                Msg &= "Registrations pre-empted"
            Case &H2A06
                Msg &= "Asymmetric access state changed"
            Case &H2A07
                Msg &= "Asymmetric access state transition failed"
            Case &H2A08
                Msg &= "Priority changed"
            Case &H2A0D
                Msg &= "Data encryption capabilities changed"
            Case &H2A10
                Msg &= "Timestamp changed"
            Case &H2A11
                Msg &= "Data encryption parameters changed by another initiator"
            Case &H2A12
                Msg &= "Data encryption parameters changed by a vendor-specific event"
            Case &H2A13
                Msg &= "Data Encryption Key Instance Counter has changed"
            Case &H2A14
                Msg &= "SA creation capabilities data has changed"
            Case &H2A15
                Msg &= "Medium removal prevention pre-empted"
            Case &H2A80
                Msg &= "Security configuration changed"
            Case &H2C00
                Msg &= "Command sequence invalid"
            Case &H2C07
                Msg &= "Previous busy status"
            Case &H2C08
                Msg &= "Previous task set full status"
            Case &H2C09
                Msg &= "Previous reservation conflict status"
            Case &H2C0B
                Msg &= "Not reserved"
            Case &H2F00
                Msg &= "Commands cleared by another initiator"
            Case &H3000
                Msg &= "Incompatible medium installed"
            Case &H3001
                Msg &= "Cannot read media, unknown format"
            Case &H3002
                Msg &= "Cannot read media: incompatible format"
            Case &H3003
                Msg &= "Cleaning cartridge installed"
            Case &H3004
                Msg &= "Cannot write medium"
            Case &H3005
                Msg &= "Cannot write medium, incompatible format"
            Case &H3006
                Msg &= "Cannot format, incompatible medium"
            Case &H3007
                Msg &= "Cleaning failure"
            Case &H300C
                Msg &= "WORM medium—overwrite attempted"
            Case &H300D
                Msg &= "WORM medium—integrity check failed"
            Case &H3100
                Msg &= "Medium format corrupted"
            Case &H3700
                Msg &= "Rounded parameter"
            Case &H3A00
                Msg &= "Medium not present"
            Case &H3A04
                Msg &= "Medium not present, Media Auxiliary Memory accessible"
            Case &H3B00
                Msg &= "Sequential positioning error"
            Case &H3B0C
                Msg &= "Position past BOM"
            Case &H3B1C
                Msg &= "Too many logical objects on partition to support operation."
            Case &H3E00
                Msg &= "Logical unit has not self-configured yet"
            Case &H3F01
                Msg &= "Microcode has been changed"
            Case &H3F03
                Msg &= "Inquiry data has changed"
            Case &H3F05
                Msg &= "Device identifier changed"
            Case &H3F0E
                Msg &= "Reported LUNs data has changed"
            Case &H3F0F
                Msg &= "Echo buffer overwritten"
            Case &H4300
                Msg &= "Message error"
            Case &H4400
                Msg &= "Internal target failure"
            Case &H4500
                Msg &= "Selection/reselection failure"
            Case &H4700
                Msg &= "SCSI parity error"
            Case &H4800
                Msg &= "Initiator Detected Error message received"
            Case &H4900
                Msg &= "Invalid message"
            Case &H4B00
                Msg &= "Data phase error"
            Case &H4B02
                Msg &= "Too much write data"
            Case &H4B03
                Msg &= "ACK/NAK timeout"
            Case &H4B04
                Msg &= "NAK received"
            Case &H4B05
                Msg &= "Data offset error"
            Case &H4B06
                Msg &= "Initiator response timeout"
            Case &H4D00
                Msg &= "Tagged overlapped command"
            Case &H4E00
                Msg &= "Overlapped commands"
            Case &H5000
                Msg &= "Write append error"
            Case &H5200
                Msg &= "Cartridge fault"
            Case &H5300
                Msg &= "Media load or eject failed"
            Case &H5301
                Msg &= "Unload tape failure"
            Case &H5302
                Msg &= "Medium removal prevented"
            Case &H5303
                Msg &= "Insufficient resources"
            Case &H5304
                Msg &= "Medium thread or unthread failure"
            Case &H5504
                Msg &= "Insufficient registration resources"
            Case &H5506
                Msg &= "Media Auxiliary Memory full"
            Case &H5B01
                Msg &= "Threshold condition met"
            Case &H5D00
                Msg &= "Failure prediction threshold exceeded"
            Case &H5DFF
                Msg &= "Failure prediction threshold exceeded (false)"
            Case &H5E01
                Msg &= "Idle condition activated by timer"
            Case &H7400
                Msg &= "Security error"
            Case &H7401
                Msg &= "Unable to decrypt data"
            Case &H7402
                Msg &= "Unencrypted data encountered while decrypting"
            Case &H7403
                Msg &= "Incorrect data encryption key"
            Case &H7404
                Msg &= "Cryptographic integrity validation failed"
            Case &H7405
                Msg &= "Key-associated data descriptors changed."
            Case &H7408
                Msg &= "Digital signature validation failure"
            Case &H7409
                Msg &= "Encryption mode mismatch on read"
            Case &H740A
                Msg &= "Encrypted block not RAW read-enabled"
            Case &H740B
                Msg &= "Incorrect encryption parameters"
            Case &H7421
                Msg &= "Data encryption configuration prevented"
            Case &H7440
                Msg &= "Authentication failed"
            Case &H7461
                Msg &= "External data encryption Key Manager access error"
            Case &H7462
                Msg &= "External data encryption Key Manager error"
            Case &H7463
                Msg &= "External data encryption management—key not found"
            Case &H7464
                Msg &= "External data encryption management—request not authorized"
            Case &H746E
                Msg &= "External data encryption control time-out"
            Case &H746F
                Msg &= "External data encryption control unknown error"
            Case &H7471
                Msg &= "Logical Unit access not authorized"
            Case &H7480
                Msg &= "KAD changed"
            Case &H7482
                Msg &= "Crypto KAD in CM failure"
            Case &H8282
                Msg &= "Drive requires cleaning"
            Case &H8283
                Msg &= "Bad microcode detected"
        End Select
        If Add_Code >> 8 = &H40 Then
            Msg &= "Diagnostic failure on component " & Hex(Add_Code And &HFF) & "h"
        End If
        Return Msg
    End Function
    Public Shared Function ParseSenseData(sense As Byte()) As String
        Dim Msg As String = ""
        Dim Fixed As Boolean = False
        Dim Add_Code As Integer
        Dim Valid As Boolean = ((sense(0) >> 7) = 1)
        If (sense(0) And &H7F) = &H70 Then
            Msg &= "Error code represents current error" & vbCrLf
            Fixed = True
        ElseIf (sense(0) And &H7F) = &H71 Then
            Msg &= "Error code represents deferred error" & vbCrLf
            Fixed = True
        End If
        If Fixed Then
            If sense(2) >> 7 = 1 Then
                Msg &= "Filemark encountered" & vbCrLf
            End If
            If ((sense(2) >> 6) And &H1) = 1 Then
                Msg &= "EOM encountered" & vbCrLf
            End If
            If ((sense(2) >> 5) And &H1) = 1 Then
                Msg &= "Blocklen mismatch" & vbCrLf
            End If
            Dim sensekey As Byte = sense(2) And &HF
            Msg &= "Sense key: "
            Select Case sensekey
                Case 0
                    Msg &= "NO SENSE" & vbCrLf
                Case 1
                    Msg &= "RECOVERED ERROR" & vbCrLf
                Case 2
                    Msg &= "NOT READY" & vbCrLf
                Case 3
                    Msg &= "MEDIUM ERROR" & vbCrLf
                Case 4
                    Msg &= "HARDWARE ERROR" & vbCrLf
                Case 5
                    Msg &= "ILLEGAL REQUEST" & vbCrLf
                Case 6
                    Msg &= "UNIT ATTENTION" & vbCrLf
                Case 7
                    Msg &= "DATA PROTECT" & vbCrLf
                Case 8
                    Msg &= "BLANK CHECK" & vbCrLf
                Case 9
                    Msg &= "VENDOR SPECIFIC" & vbCrLf
                Case 10
                    Msg &= "COPY ABORTED" & vbCrLf
                Case 11
                    Msg &= "ABORTED COMMAND" & vbCrLf
                Case 12
                    Msg &= "EQUAL" & vbCrLf
                Case 13
                    Msg &= "VOLUME OVERFLOW" & vbCrLf
                Case 14
                    Msg &= "MISCOMPARE" & vbCrLf
                Case 15
                    Msg &= "RESERVED" & vbCrLf
            End Select
            If Valid Then
                Msg &= "Info bytes: " & Byte2Hex({sense(3), sense(4), sense(5), sense(6)}) & vbCrLf
            End If
            Dim Add_Len As Byte = sense(7)
            Add_Code = CInt(sense(12)) << 8 Or sense(13)
            Dim SKSV As Boolean = ((sense(15) >> 7) = 1)
            Dim CD As Boolean = ((sense(15) >> 6) And 1) = 1
            Dim BPV As Boolean = ((sense(15) >> 3) And 1) = 1

            If SKSV Then
                If sensekey = 5 Then
                    Msg &= "Error byte = " & (CInt(sense(16)) << 8 Or sense(17)) & " bit = " & (sense(15) And 7) & vbCrLf
                ElseIf sensekey = 0 Or sensekey = 2 Then
                    Msg &= "Progress = " & (CInt(sense(16)) << 8 Or sense(17)) & vbCrLf
                End If
            Else
                Msg &= "Drive Error Code = " & Byte2Hex({sense(16), sense(17)}) & vbCrLf
            End If
            If ((sense(21) >> 3) And 1) = 1 Then
                Msg &= "Clean is required" & vbCrLf
            End If
        End If
        Msg &= "Additional code: "
        Msg &= ParseAdditionalSenseCode(Add_Code) & vbCrLf
        Return Msg
    End Function
    Public Shared Function PreventMediaRemoval(TapeDrive As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SendSCSICommand(TapeDrive, {&H1E, 0, 0, 0, 1, 0}, Nothing, 1, senseReport)
    End Function
    Public Shared Function AllowMediaRemoval(TapeDrive As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SendSCSICommand(TapeDrive, {&H1E, 0, 0, 0, 0, 0}, Nothing, 1, senseReport)
    End Function
    Public Shared Function ReserveUnit(TapeDrive As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SendSCSICommand(TapeDrive, {&H16, 0, 0, 0, 0, 0}, Nothing, 1, senseReport)
    End Function
    Public Shared Function ReleaseUnit(TapeDrive As String, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
        Return SendSCSICommand(TapeDrive, {&H17, 0, 0, 0, 0, 0}, Nothing, 1, senseReport)
    End Function

    Public Shared Function ReadMAMAttributeString(TapeDrive As String, PageCode_H As Byte, PageCode_L As Byte) As String 'TC_MAM_BARCODE = 0x0806 LEN = 32
        Return System.Text.Encoding.UTF8.GetString(GetMAMAttributeBytes(TapeDrive, PageCode_H, PageCode_L).ToArray())
    End Function
    Public Class PositionData
        Public Property BOP As Boolean
        Public Property EOP As Boolean
        Public Property MPU As Boolean
        Public Property PartitionNumber As UInt32
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
    End Enum
    Public Shared Function ReadPosition(TapeDrive As String, Optional ByVal DriverType As DriverType = DriverType.LTO) As PositionData
        If DriverTypeSetting <> DriverType.LTO Then DriverType = DriverTypeSetting
        Dim param As Byte()
        Dim result As New PositionData
        Dim sense As Byte()
        Select Case DriverType
            Case DriverType.LTO
                If AllowPartition Then
                    param = SCSIReadParam(TapeDrive, {&H34, 6, 0, 0, 0, 0, 0, 0, 0, 0}, 32, Function(sdata As Byte())
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
                    param = SCSIReadParam(TapeDrive, {&H34, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 32)
                    result.BOP = param(0) >> 7 And &H1
                    result.EOP = param(0) >> 6 And &H1
                    For i As Integer = 0 To 3
                        result.BlockNumber <<= 8
                        result.BlockNumber = result.BlockNumber Or param(4 + i)
                    Next
                End If
                If sense IsNot Nothing AndAlso sense.Length >= 14 Then result.AddSenseKey = CInt(sense(12)) << 8 Or sense(13)
            Case DriverType.M2488

            Case Else
        End Select
        Return result
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Byte()) As Byte()
        Dim sense(63) As Byte
        Dim succ As Boolean =
            SendSCSICommand(TapeDrive, {&HA, 0, Data.Length >> 16 And &HFF, Data.Length >> 8 And &HFF, Data.Length And &HFF, 0}, Data, 0,
                        Function(senseData As Byte()) As Boolean
                            sense = senseData
                            Return True
                        End Function)
        If Not succ Then Throw New Exception("SCSI Failure")
        Return sense
    End Function
    Public Shared Function Write(TapeDrive As String, Data As IntPtr, Length As Integer, Optional ByVal senseEnabled As Boolean = False) As Byte()
        Dim sense() As Byte = {0, 0, 0}
        If senseEnabled Then ReDim sense(63)
        Dim cdbData As Byte() = {&HA, 0, Length >> 16 And &HFF, Length >> 8 And &HFF, Length And &HFF, 0}
        Dim cdb As IntPtr = Marshal.AllocHGlobal(cdbData.Length)
        Marshal.Copy(cdbData, 0, cdb, cdbData.Length)
        Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(64)
        Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, Data, Length, 0, 900, senseBufferPtr)
        If senseEnabled Then Marshal.Copy(senseBufferPtr, sense, 0, 64)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(senseBufferPtr)
        If Not succ Then Throw New Exception("SCSI Failure")
        Return sense
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Byte(), BlockSize As Integer) As Byte()
        If Data.Length <= BlockSize Then
            Return Write(TapeDrive, Data)
        End If
        Dim sense(63) As Byte
        Dim cdbData As Byte() = {}
        Dim cdb As IntPtr = Marshal.AllocHGlobal(6)
        Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(BlockSize)
        Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(64)
        For i As Integer = 0 To Data.Length - 1 Step BlockSize
            Dim TransferLen As UInteger = Math.Min(BlockSize, Data.Length - i)
            cdbData = {&HA, 0, TransferLen >> 16 And &HFF, TransferLen >> 8 And &HFF, TransferLen And &HFF, 0}
            Marshal.Copy(cdbData, 0, cdb, cdbData.Length)
            Marshal.Copy(Data, i, dataBuffer, TransferLen)
            Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, dataBuffer, TransferLen, 0, 60000, senseBufferPtr)
            If Not succ Then
                Marshal.Copy(senseBufferPtr, sense, 0, 64)
                Marshal.FreeHGlobal(cdb)
                Marshal.FreeHGlobal(dataBuffer)
                Marshal.FreeHGlobal(senseBufferPtr)
                Throw New Exception("SCSI Failure")
                Return sense
            End If
        Next
        Marshal.Copy(senseBufferPtr, sense, 0, 64)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(dataBuffer)
        Marshal.FreeHGlobal(senseBufferPtr)
        Return sense
    End Function
    Public Shared Function Write(TapeDrive As String, Data As Stream, Optional ByVal BlockSize As Integer = 524288, Optional ByVal senseEnabled As Boolean = False) As Byte()
        Dim sense(63) As Byte
        Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(64)
        BlockSize = Math.Min(BlockSize, GlobalBlockLimit)
        Dim DataBuffer(BlockSize - 1) As Byte
        Dim DataPtr As IntPtr = Marshal.AllocHGlobal(BlockSize)
        Dim cdb As IntPtr = Marshal.AllocHGlobal(6)

        Dim DataLen As Integer = Data.Read(DataBuffer, 0, BlockSize)
        Dim succ As Boolean
        While DataLen > 0
            Dim cdbData As Byte() = {&HA, 0, DataLen >> 16 And &HFF, DataLen >> 8 And &HFF, DataLen And &HFF, 0}
            Marshal.Copy(cdbData, 0, cdb, cdbData.Length)
            Marshal.Copy(DataBuffer, 0, DataPtr, DataLen)
            Do
                succ = TapeUtils._TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, DataPtr, DataLen, 0, 60000, senseBufferPtr)
                If succ Then
                    Exit Do
                Else
                    Select Case MessageBox.Show($"写入出错：SCSI指令执行失败", "警告", MessageBoxButtons.AbortRetryIgnore)
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
        If senseEnabled Then Marshal.Copy(senseBufferPtr, sense, 0, 64)
        Data.Close()
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(DataPtr)
        Marshal.FreeHGlobal(senseBufferPtr)
        If Not succ Then Throw New Exception("SCSI Failure")
        Return {0, 0, 0}
    End Function
    Public Shared Function Write(TapeDrive As String, sourceFile As String, Optional ByVal BlockLen As Integer = 524288, Optional ByVal senseEnabled As Boolean = False) As Byte()
        Dim sense(63) As Byte
        Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(64)
        BlockLen = Math.Min(BlockLen, GlobalBlockLimit)
        Dim DataBuffer(BlockLen - 1) As Byte
        Dim DataPtr As IntPtr = Marshal.AllocHGlobal(BlockLen)
        Dim cdb As IntPtr = Marshal.AllocHGlobal(6)
        Dim fs As New IO.FileStream(sourceFile, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
        Dim DataLen As Integer = fs.Read(DataBuffer, 0, BlockLen)
        Dim succ As Boolean
        While DataLen > 0
            Dim cdbData As Byte() = {&HA, 0, DataLen >> 16 And &HFF, DataLen >> 8 And &HFF, DataLen And &HFF, 0}
            Marshal.Copy(cdbData, 0, cdb, cdbData.Length)
            Marshal.Copy(DataBuffer, 0, DataPtr, DataLen)
            Do
                succ = TapeUtils._TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, DataPtr, DataLen, 0, 60000, senseBufferPtr)
                If succ Then
                    Exit Do
                Else
                    Select Case MessageBox.Show($"写入出错：SCSI指令执行失败", "警告", MessageBoxButtons.AbortRetryIgnore)
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
        If senseEnabled Then Marshal.Copy(senseBufferPtr, sense, 0, 64)
        fs.Close()
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(DataPtr)
        Marshal.FreeHGlobal(senseBufferPtr)
        If Not succ Then Throw New Exception("SCSI Failure")
        Return {0, 0, 0}
    End Function
    Public Shared Function Flush(TapeDrive As String) As Byte()
        Return WriteFileMark(TapeDrive, 0)
    End Function
    Public Shared Function WriteFileMark(TapeDrive As String, Optional ByVal Number As UInteger = 1) As Byte()
        Dim sense(63) As Byte
        SendSCSICommand(TapeDrive, {&H10, Math.Min(Number, 1), Number >> 16 And &HFF, Number >> 8 And &HFF, Number And &HFF, 0}, {}, 0,
                        Function(senseData As Byte()) As Boolean
                            sense = senseData
                            Return True
                        End Function)
        Return sense
    End Function
    Public Shared Function GetMAMAttributeBytes(TapeDrive As String, PageCode_H As Byte, PageCode_L As Byte, Optional ByVal PartitionNumber As Byte = 0) As Byte()
        Dim DATA_LEN As Integer = 0
        Dim cdb As IntPtr = Marshal.AllocHGlobal(16)
        Dim cdbData As Byte() = {&H8C, 0, 0, 0, 0, 0, 0, PartitionNumber,
            PageCode_H,
            PageCode_L,
            (DATA_LEN + 9) >> 24 And &HFF,
            (DATA_LEN + 9) >> 16 And &HFF,
            (DATA_LEN + 9) >> 8 And &HFF,
            (DATA_LEN + 9) And &HFF, 0, 0}
        Marshal.Copy(cdbData, 0, cdb, 16)
        Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(DATA_LEN + 9)
        Dim BCArray(DATA_LEN + 8) As Byte
        Marshal.Copy(BCArray, 0, dataBuffer, 9)
        Dim senseBuffer As IntPtr = Marshal.AllocHGlobal(64)
        Dim Result As Byte() = {}
        Dim succ As Boolean = False
        Try
            succ = _TapeSCSIIOCtlFull(TapeDrive, cdb, 16, dataBuffer, DATA_LEN + 9, 1, 60000, senseBuffer)
        Catch ex As Exception
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
                Dim cdb2 As IntPtr = Marshal.AllocHGlobal(16)
                Marshal.Copy(cdbData, 0, cdb2, 16)
                succ = False
                Dim senseBuffer2 As IntPtr = Marshal.AllocHGlobal(64)
                Try
                    succ = _TapeSCSIIOCtlFull(TapeDrive, cdb2, 16, dataBuffer2, DATA_LEN + 9, 1, 60000, senseBuffer)
                Catch ex As Exception
                    Throw New Exception("SCSIIOError")
                End Try
                If succ Then
                    Marshal.Copy(dataBuffer2, BCArray2, 0, DATA_LEN + 9)
                    Result = BCArray2.Skip(9).ToArray()
                End If
                Marshal.FreeHGlobal(dataBuffer2)
                Marshal.FreeHGlobal(cdb2)
                Marshal.FreeHGlobal(senseBuffer2)
            End If
        End If
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(dataBuffer)
        Marshal.FreeHGlobal(senseBuffer)
        Return Result
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
    Public Shared Function ByteArrayToString(bytesArray As Byte()) As String
        Dim strBuilder As New StringBuilder()
        Dim rowSize As Integer = 16
        Dim numRows As Integer = Math.Ceiling(bytesArray.Length / rowSize)

        For row As Integer = 0 To numRows - 1
            Dim rowStart As Integer = row * rowSize
            Dim rowEnd As Integer = Math.Min((row + 1) * rowSize, bytesArray.Length)
            Dim rowBytes As Byte() = bytesArray.Skip(rowStart).Take(rowEnd - rowStart).ToArray()

            ' Append the hex values for this row
            strBuilder.Append($"{rowStart:X8}h: ")
            For Each b As Byte In rowBytes
                strBuilder.Append($"{b:X2} ")
            Next
            strBuilder.Append(" "c, (rowSize - rowBytes.Length) * 3)

            ' Append the ASCII characters for this row
            strBuilder.Append("  ")
            For Each b As Byte In rowBytes
                strBuilder.Append(If(b >= 32 AndAlso b <= 126, ChrW(b), "."c))
            Next

            ' Append a newline character for the next row
            strBuilder.AppendLine()
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
        Public Shared Function FromTapeDrive(TapeDrive As String, PageCode_H As Byte, PageCode_L As Byte, Optional ByVal PartitionNumber As Byte = 0) As MAMAttribute
            Dim RawData As Byte() = GetMAMAttributeBytes(TapeDrive, PageCode_H, PageCode_L, PartitionNumber)
            If RawData.Length = 0 Then Return Nothing
            Return New MAMAttribute With {.ID = (CUShort(PageCode_H) << 8) Or PageCode_L, .RawData = RawData}
        End Function
        Public Shared Function FromTapeDrive(TapeDrive As String, PageCode As UInt16, Optional ByVal PartitionNumber As Byte = 0) As MAMAttribute
            Return FromTapeDrive(TapeDrive, (PageCode >> 8) And &HFF, PageCode And &HFF, PartitionNumber)
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
    Public Shared Function SendSCSICommand(TapeDrive As String, cdbData As Byte(), Optional ByRef Data As Byte() = Nothing, Optional DataIn As Byte = 2, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing, Optional ByVal TimeOut As Integer = 60000) As Boolean
        Dim cdb As IntPtr = Marshal.AllocHGlobal(cdbData.Length)
        Marshal.Copy(cdbData, 0, cdb, cdbData.Length)

        Dim dataBufferPtr As IntPtr
        Dim dataLen As Integer = 0
        If Data IsNot Nothing Then
            dataLen = Data.Length
            dataBufferPtr = Marshal.AllocHGlobal(Data.Length)
            Marshal.Copy(Data, 0, dataBufferPtr, Data.Length)
        Else
            dataBufferPtr = Marshal.AllocHGlobal(128)
        End If

        Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(64)

        Dim senseBuffer(63) As Byte
        Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, dataBufferPtr, dataLen, DataIn, TimeOut, senseBufferPtr)
        If succ AndAlso Data IsNot Nothing Then Marshal.Copy(dataBufferPtr, Data, 0, Data.Length)
        If senseReport IsNot Nothing Then
            Marshal.Copy(senseBufferPtr, senseBuffer, 0, 64)
            senseReport(senseBuffer)
        End If
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(dataBufferPtr)
        Marshal.FreeHGlobal(senseBufferPtr)
        Return succ
    End Function

    Public Const DEFAULT_LOG_DIR As String = "C:\ProgramData\HPE\LTFS"
    Public Const DEFAULT_WORK_DIR As String = "C:\tmp\LTFS"
    Public Shared Function GetTapeDriveList() As List(Of TapeDrive)
        Dim p As IntPtr = _GetTapeDriveList()
        'MessageBox.Show(Marshal.PtrToStringAnsi(p))
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
    Public Shared Function GetMediumChangerList() As List(Of MediumChanger)
        Dim p As IntPtr = _GetMediumChangerList()

        Dim result As String = Marshal.PtrToStringAnsi(p)
        Dim s() As String = result.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)

        Dim LChanger As New List(Of MediumChanger)
        For Each t As String In s
            Dim q() As String = t.Split({"|"}, StringSplitOptions.None)
            If q.Length = 4 Then
                LChanger.Add(New MediumChanger(q(0), q(1), q(2), q(3)))
            End If
        Next
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
    Public Enum LoadOption
        LoadThreaded = 1
        LoadUnthreaded = 9
        Unthread = &HA
        Eject = 0
    End Enum
    Public Shared Function LoadEject(TapeDrive As String, LoadOption As LoadOption) As Boolean
        Return SendSCSICommand(TapeDrive, {&H1B, 0, 0, 0, LoadOption, 0})
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
                                  Optional ByVal P1Size As UInt16 = &HFFFF) As Boolean
        GlobalBlockLimit = TapeUtils.ReadBlockLimits(TapeDrive).MaximumBlockLength
        BlockLen = Math.Min(BlockLen, GlobalBlockLimit)
        Dim mkltfs_op As Func(Of Boolean) =
            Function()

                'Load and Thread
                ProgressReport("Loading..")
                If TapeUtils.SendSCSICommand(TapeDrive, {&H1B, 0, 0, 0, 1, 0}) Then
                    ProgressReport("Load OK" & vbCrLf)
                Else
                    OnError("Load Fail" & vbCrLf)
                    Return False
                End If
                Dim MaxExtraPartitionAllowed As Byte = TapeUtils.ModeSense(TapeDrive, &H11)(2)
                ExtraPartitionCount = Math.Min(MaxExtraPartitionAllowed, ExtraPartitionCount)
                If Not AllowPartition Then ExtraPartitionCount = 0
                If ExtraPartitionCount > 1 Then ExtraPartitionCount = 1

                'Set Capacity
                ProgressReport("Set Capacity..")
                If TapeUtils.SendSCSICommand(TapeDrive, {&HB, 0, 0, (Capacity >> 8) And &HFF, Capacity And &HFF, 0}) Then
                    ProgressReport("Load OK" & vbCrLf)
                Else
                    OnError("Load Fail" & vbCrLf)
                    Return False
                End If

                'Erase
                ProgressReport("Initializing tape..")
                If TapeUtils.SendSCSICommand(TapeDrive, {4, 0, 0, 0, 0, 0}) Then
                    ProgressReport("Initialization OK" & vbCrLf)
                Else
                    OnError("Initialization Fail" & vbCrLf)
                    Return False
                End If
                If ExtraPartitionCount > 0 Then
                    'Mode Select:1st Partition to Minimum 
                    ProgressReport("MODE SELECT - Partition mode page..")
                    If TapeUtils.SendSCSICommand(TapeDrive, {&H15, &H10, 0, 0, &H10, 0}, {0, 0, &H10, 0, &H11, &HA, MaxExtraPartitionAllowed, 1, &H3C, 3, 9, 0, (P0Size >> 8) And &HFF, P0Size And &HFF, (P1Size >> 8) And &HFF, P1Size And &HFF}, 0) Then
                        ProgressReport("MODE SELECT 11h OK" & vbCrLf)
                    Else
                        OnError("MODE SELECT 11h Fail" & vbCrLf)
                        Return False
                    End If
                    'Format
                    ProgressReport("Partitioning..")
                    If TapeUtils.SendSCSICommand(TapeDrive, {4, 0, 1, 0, 0, 0}, Nothing, 0) Then
                        ProgressReport("     OK" & vbCrLf)
                    Else
                        OnError("     Fail" & vbCrLf)
                        Return False
                    End If
                End If
                'Set Vendor
                ProgressReport($"WRITE ATTRIBUTE: Vendor=OPEN..")
                If TapeUtils.SetMAMAttribute(TapeDrive, &H800, "OPEN".PadRight(8)) Then
                    ProgressReport("WRITE ATTRIBUTE: 0800 OK" & vbCrLf)
                Else
                    OnError("WRITE ATTRIBUTE: 0800 Fail" & vbCrLf)
                    Return False
                End If
                'Set AppName
                ProgressReport($"WRITE ATTRIBUTE: Application Name = LTFSCopyGUI..")
                If TapeUtils.SetMAMAttribute(TapeDrive, &H801, "LTFSCopyGUI".PadRight(32)) Then
                    ProgressReport("WRITE ATTRIBUTE: 0801 OK" & vbCrLf)
                Else
                    OnError("WRITE ATTRIBUTE: 0801 Fail" & vbCrLf)
                    Return False
                End If
                'Set Version
                ProgressReport($"WRITE ATTRIBUTE: Application Version={My.Application.Info.Version.ToString(3)}..")
                If TapeUtils.SetMAMAttribute(TapeDrive, &H802, My.Application.Info.Version.ToString(3).PadRight(8)) Then
                    ProgressReport("WRITE ATTRIBUTE: 0802 OK" & vbCrLf)
                Else
                    OnError("WRITE ATTRIBUTE: 0802 Fail" & vbCrLf)
                    Return False
                End If
                'Set TextLabel
                ProgressReport($"WRITE ATTRIBUTE: TextLabel= ..")
                If TapeUtils.SetMAMAttribute(TapeDrive, &H803, "".PadRight(160), TapeUtils.AttributeFormat.Text) Then
                    ProgressReport("WRITE ATTRIBUTE: 0803 OK" & vbCrLf)
                Else
                    OnError("WRITE ATTRIBUTE: 0803 Fail" & vbCrLf)
                    Return False
                End If
                'Set TLI
                ProgressReport($"WRITE ATTRIBUTE: Localization Identifier = 0..")
                If TapeUtils.SetMAMAttribute(TapeDrive, &H805, {0}, TapeUtils.AttributeFormat.Binary) Then
                    ProgressReport("WRITE ATTRIBUTE:0805 OK" & vbCrLf)
                Else
                    OnError("WRITE ATTRIBUTE:0805 Fail" & vbCrLf)
                    Return False
                End If
                'Set Barcode
                Barcode = Barcode.PadRight(32).Substring(0, 32)
                ProgressReport($"WRITE ATTRIBUTE: Barcode={Barcode}..")
                If TapeUtils.SetBarcode(TapeDrive, Barcode) Then
                    ProgressReport("WRITE ATTRIBUTE: 0806 OK" & vbCrLf)
                Else
                    OnError("WRITE ATTRIBUTE: 0806 Fail" & vbCrLf)
                    Return False
                End If
                'Set Version
                Dim LTFSVersion As String = "2.4.0"
                If ExtraPartitionCount = 0 Then LTFSVersion = "2.4.1"
                ProgressReport($"WRITE ATTRIBUTE: Format Version={LTFSVersion}..")
                If TapeUtils.SetMAMAttribute(TapeDrive, &H80B, LTFSVersion.PadRight(16)) Then
                    ProgressReport("WRITE ATTRIBUTE: 080B OK" & vbCrLf)
                Else
                    OnError("WRITE ATTRIBUTE: 080B Fail" & vbCrLf)
                    Return False
                End If
                'Mode Select:Block Length
                ProgressReport($"MODE SELECT - Block Size {BlockLen}..")
                If TapeUtils.SetBlockSize(TapeDrive, BlockLen).Length > 0 Then
                    ProgressReport($"MODE SELECT - Block Size {BlockLen} OK" & vbCrLf)
                Else
                    OnError($"MODE SELECT - Block Size {BlockLen} Fail" & vbCrLf)
                    Return False
                End If
                'Locate
                ProgressReport("Locate to data partition..")
                If TapeUtils.Locate(TapeDrive, 0, ExtraPartitionCount) = 0 Then
                    ProgressReport($"Locate P{ExtraPartitionCount}B0 OK" & vbCrLf)
                Else
                    OnError($"Locate P{ExtraPartitionCount}B0 Fail" & vbCrLf)
                    Return False
                End If

                'Write VOL1Label
                ProgressReport("Write VOL1Label..")
                If TapeUtils.Write(TapeDrive, New Vol1Label().GenerateRawData(Barcode)).Length > 0 Then
                    ProgressReport("Write VOL1Label OK" & vbCrLf)
                Else
                    OnError("Write VOL1Label Fail" & vbCrLf)
                    Return False
                End If

                'Write FileMark
                ProgressReport("Write FileMark..")
                If TapeUtils.WriteFileMark(TapeDrive).Length > 0 Then
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
                If TapeUtils.Write(TapeDrive, Encoding.UTF8.GetBytes(plabel.GetSerializedText())).Length > 0 Then
                    ProgressReport("Write ltfslabel OK" & vbCrLf)
                Else
                    OnError("Write ltfslabel Fail" & vbCrLf)
                    Return False
                End If

                'Write FileMark
                ProgressReport("Write FileMark..")
                If TapeUtils.WriteFileMark(TapeDrive, 2).Length > 0 Then
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
                pindex.location.startblock = TapeUtils.ReadPosition(TapeDrive).BlockNumber
                pindex.previousgenerationlocation = Nothing
                pindex.highestfileuid = 1
                Dim block1 As ULong = pindex.location.startblock
                pindex._directory = New List(Of ltfsindex.directory)
                pindex._directory.Add(New ltfsindex.directory With {.name = VolumeName, .readonly = False,
                                              .creationtime = plabel.formattime, .changetime = .creationtime,
                                              .accesstime = .creationtime, .modifytime = .creationtime, .backuptime = .creationtime, .fileuid = 1, .contents = New ltfsindex.contentsDef()})

                'Write ltfsindex
                ProgressReport("Write ltfsindex..")
                If TapeUtils.Write(TapeDrive, Encoding.UTF8.GetBytes(pindex.GetSerializedText())).Length > 0 Then
                    ProgressReport("Write ltfsindex OK" & vbCrLf)
                Else
                    OnError("Write ltfsindex Fail" & vbCrLf)
                    Return False
                End If

                'Write FileMark
                ProgressReport("Write FileMark..")
                If TapeUtils.WriteFileMark(TapeDrive).Length > 0 Then
                    ProgressReport("Write FileMark OK" & vbCrLf)
                Else
                    OnError("Write FileMark Fail" & vbCrLf)
                    Return False
                End If
                Dim block0 As ULong
                If ExtraPartitionCount > 0 Then
                    'Locate
                    ProgressReport("Locate to index partition..")
                    If TapeUtils.Locate(TapeDrive, 0, 0) = 0 Then
                        ProgressReport("Locate P0B0 OK" & vbCrLf)
                    Else
                        OnError("Locate P0B0 Fail" & vbCrLf)
                        Return False
                    End If
                    'Write VOL1Label
                    ProgressReport("Write VOL1Label..")
                    If TapeUtils.Write(TapeDrive, New Vol1Label().GenerateRawData(Barcode)).Length > 0 Then
                        ProgressReport("Write VOL1Label OK" & vbCrLf)
                    Else
                        OnError("Write VOL1Label Fail" & vbCrLf)
                        Return False
                    End If
                    'Write FileMark
                    ProgressReport("Write FileMark..")
                    If TapeUtils.WriteFileMark(TapeDrive).Length > 0 Then
                        ProgressReport("Write FileMark OK" & vbCrLf)
                    Else
                        OnError("Write FileMark Fail" & vbCrLf)
                        Return False
                    End If
                    'Write ltfslabel
                    plabel.location.partition = ltfslabel.PartitionLabel.a
                    ProgressReport("Write ltfslabel..")
                    If TapeUtils.Write(TapeDrive, Encoding.UTF8.GetBytes(plabel.GetSerializedText())).Length > 0 Then
                        ProgressReport("Write ltfslabel OK" & vbCrLf)
                    Else
                        OnError("Write ltfslabel Fail" & vbCrLf)
                        Return False
                    End If
                    'Write FileMark
                    ProgressReport("Write FileMark..")
                    If TapeUtils.WriteFileMark(TapeDrive, 2).Length > 0 Then
                        ProgressReport("Write FileMark OK" & vbCrLf)
                    Else
                        OnError("Write FileMark Fail" & vbCrLf)
                        Return False
                    End If
                    'Write ltfsindex
                    pindex.previousgenerationlocation = New ltfsindex.PartitionDef()
                    pindex.previousgenerationlocation.partition = pindex.location.partition
                    pindex.previousgenerationlocation.startblock = pindex.location.startblock
                    pindex.location.partition = ltfsindex.PartitionLabel.a
                    pindex.location.startblock = TapeUtils.ReadPosition(TapeDrive).BlockNumber
                    block0 = pindex.location.startblock
                    ProgressReport("Write ltfsindex..")
                    If TapeUtils.Write(TapeDrive, Encoding.UTF8.GetBytes(pindex.GetSerializedText())).Length > 0 Then
                        ProgressReport("Write ltfsindex OK" & vbCrLf)
                    Else
                        OnError("Write ltfsindex Fail" & vbCrLf)
                        Return False
                    End If
                    'Write FileMark
                    ProgressReport("Write FileMark..")
                    If TapeUtils.WriteFileMark(TapeDrive).Length > 0 Then
                        ProgressReport("Write FileMark OK" & vbCrLf)
                    Else
                        OnError("Write FileMark Fail" & vbCrLf)
                        Return False
                    End If
                End If
                'Set DateTime
                Dim CurrentTime As String = Now.ToUniversalTime.ToString("yyyyMMddhhmm")
                ProgressReport($"WRITE ATTRIBUTE: Written time={CurrentTime}..")
                If TapeUtils.SetMAMAttribute(TapeDrive, &H804, CurrentTime.PadRight(12)) Then
                    ProgressReport("WRITE ATTRIBUTE: 0804 OK" & vbCrLf)
                Else
                    OnError("WRITE ATTRIBUTE: 0804 Fail" & vbCrLf)
                    Return False
                End If
                'Set VCI
                ProgressReport($"WRITE ATTRIBUTE: VCI..")
                If TapeUtils.WriteVCI(TapeDrive, pindex.generationnumber, block0, block1, pindex.volumeuuid.ToString(), ExtraPartitionCount) Then
                    ProgressReport("WRITE VCI OK" & vbCrLf)
                Else
                    ProgressReport("WRITE VCI Fail" & vbCrLf)
                End If

                OnFinish("Format finished.")
                Return True
            End Function
        If ImmediateMode Then
            Return mkltfs_op()
        Else
            Dim th As New Threading.Thread(
                Sub()
                    mkltfs_op()
                End Sub)
            th.Start()
            Return True
        End If
    End Function
    Public Shared Function RawDump(TapeDrive As String, OutputFile As String, BlockAddress As Long, ByteOffset As Long, FileOffset As Long, Partition As Long, TotalBytes As Long, ByRef StopFlag As Boolean, Optional ByVal BlockSize As Long = 524288, Optional ByVal ProgressReport As Func(Of Long, Boolean) = Nothing, Optional ByVal CreateNew As Boolean = True, Optional LockDrive As Boolean = True) As Boolean
        If LockDrive AndAlso Not ReserveUnit(TapeDrive) Then Return False
        BlockSize = Math.Min(BlockSize, GlobalBlockLimit)
        If LockDrive AndAlso Not PreventMediaRemoval(TapeDrive) Then
            ReleaseUnit(TapeDrive)
            Return False
        End If
        If Locate(TapeDrive, BlockAddress, Partition, LocateDestType.Block) <> 0 Then
            If LockDrive Then
                AllowMediaRemoval(TapeDrive)
                ReleaseUnit(TapeDrive)
            End If
            Return False
        End If
        Try
            If CreateNew Then IO.File.WriteAllBytes(OutputFile, {})
        Catch ex As Exception
            If LockDrive Then
                AllowMediaRemoval(TapeDrive)
                ReleaseUnit(TapeDrive)
            End If
            Return False
        End Try
        Dim fs As IO.FileStream
        Try
            fs = New IO.FileStream(OutputFile, IO.FileMode.OpenOrCreate, IO.FileAccess.ReadWrite, IO.FileShare.Read, 8388608, IO.FileOptions.SequentialScan)
        Catch ex As Exception
            If LockDrive Then
                AllowMediaRemoval(TapeDrive)
                ReleaseUnit(TapeDrive)
            End If
            Return False
        End Try
        Try
            fs.Seek(FileOffset, IO.SeekOrigin.Begin)
            Dim ReadedSize As Long = 0
            While (ReadedSize < TotalBytes + ByteOffset) And Not StopFlag
                Dim len As Integer = Math.Min(BlockSize, TotalBytes + ByteOffset - ReadedSize)
                Dim Data As Byte() = ReadBlock(TapeDrive, Nothing, len)
                If Data.Length <> len OrElse len = 0 Then
                    If LockDrive Then
                        AllowMediaRemoval(TapeDrive)
                        ReleaseUnit(TapeDrive)
                    End If
                    Return False
                End If
                ReadedSize += len
                fs.Write(Data, ByteOffset, len - ByteOffset)
                If ProgressReport IsNot Nothing Then StopFlag = ProgressReport(len - ByteOffset)
                ByteOffset = 0
            End While
            If LockDrive Then
                AllowMediaRemoval(TapeDrive)
                ReleaseUnit(TapeDrive)
            End If
            If StopFlag Then
                fs.Close()
                IO.File.Delete(OutputFile)
                Return True
            End If
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
            fs.Close()
            IO.File.Delete(OutputFile)
            If LockDrive Then
                AllowMediaRemoval(TapeDrive)
                ReleaseUnit(TapeDrive)
            End If
            Return False
        End Try
        fs.Close()
        Return True
    End Function
    Public Shared Function ParseTimeStamp(t As String) As Date
        If t Is Nothing OrElse t = "" Then Return Nothing
        'yyyy-MM-ddTHH:mm:ss.fffffff00Z
        Return Date.ParseExact(t, "yyyy-MM-ddTHH:mm:ss.fffffff00Z", Globalization.CultureInfo.InvariantCulture)
    End Function
    <Serializable>
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
    <Serializable>
    Public Class MediumChanger
        Public Property DevIndex As String
        Public Property SerialNumber As String
        Public Property VendorId As String
        Public Property ProductId As String
        Public Property RawElementData As Byte()
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
        Public Shared Function SCSIReadElementStatus(Changer As String, Optional ByRef sense As Byte() = Nothing, Optional ByVal dSize As Integer = 8) As Byte()
            Dim cdb As IntPtr = Marshal.AllocHGlobal(12)
            Dim cdbBytes As Byte()
            Dim dataBuffer As IntPtr = Marshal.AllocHGlobal(dSize)
            Dim senseBuffer As IntPtr = Marshal.AllocHGlobal(64)
            If dSize <= 8 Then
                dSize = 8
                cdbBytes = {&HB8, &H10, 0, 0, &HFF, &HFF, 3, dSize >> 16 And &HFF, dSize >> 8 And &HFF, dSize And &HFF, 0, 0}
                Marshal.Copy(cdbBytes, 0, cdb, 12)
                If _TapeSCSIIOCtlFull(Changer, cdb, 12, dataBuffer, 8, 1, 60, senseBuffer) Then
                    Dim data0(dSize - 1) As Byte
                    Marshal.Copy(dataBuffer, data0, 0, dSize)
                    dSize = data0(5)
                    dSize <<= 8
                    dSize = dSize Or data0(6)
                    dSize <<= 8
                    dSize = dSize Or data0(7)
                    Marshal.FreeHGlobal(dataBuffer)
                Else
                    Marshal.FreeHGlobal(cdb)
                    Marshal.FreeHGlobal(dataBuffer)
                    Marshal.FreeHGlobal(senseBuffer)
                    Return Nothing
                End If
                dSize += 8
            End If



            cdbBytes = {&HB8, &H10, 0, 0, &HFF, &HFF, 3, dSize >> 16 And &HFF, dSize >> 8 And &HFF, dSize And &HFF, 0, 0}
            Marshal.Copy(cdbBytes, 0, cdb, 12)
            dataBuffer = Marshal.AllocHGlobal(dSize)
            If _TapeSCSIIOCtlFull(Changer, cdb, 12, dataBuffer, dSize, 1, 60, senseBuffer) Then
                Dim data1(dSize - 1) As Byte
                Marshal.Copy(dataBuffer, data1, 0, dSize)
                If sense IsNot Nothing AndAlso sense.Length >= 64 Then
                    Marshal.Copy(senseBuffer, sense, 0, 64)
                End If
                Marshal.FreeHGlobal(cdb)
                Marshal.FreeHGlobal(dataBuffer)
                Marshal.FreeHGlobal(senseBuffer)
                Return data1
            Else
                Marshal.FreeHGlobal(cdb)
                Marshal.FreeHGlobal(dataBuffer)
                Marshal.FreeHGlobal(senseBuffer)
                Return Nothing
            End If
        End Function
        Public Shared Sub MoveMedium(Changer As String, src As UInt32, dest As UInt32, Optional ByVal sense As Byte() = Nothing)
            SCSIReadParam(Changer, {&HA5, 0, 0, 0, src >> 8 And &HFF, src And &HFF, dest >> 8 And &HFF, dest And &HFF, 0, 0, 0, 0}, 12,
                Function(s As Byte()) As Boolean
                    If sense IsNot Nothing AndAlso sense.Length >= 64 Then
                        Array.Copy(s, sense, Math.Min(64, s.Length))
                    End If
                    Return True
                End Function)
        End Sub
        Public Sub RefreshElementStatus()
            If RawElementData IsNot Nothing AndAlso RawElementData.Length > 8 Then
                RawElementData = SCSIReadElementStatus($"\\.\CHANGER{DevIndex}", dSize:=RawElementData.Length)
            Else
                RawElementData = SCSIReadElementStatus($"\\.\CHANGER{DevIndex}")
            End If


            FirstElementAddressReported = CInt(RawElementData(0)) << 8 Or RawElementData(1)
            NumberofElementsAvailable = CInt(RawElementData(2)) << 8 Or RawElementData(3)
            ByteCountofReportAvailable = CInt(RawElementData(5)) << 16 Or CInt(RawElementData(6)) << 8 Or RawElementData(7)
            Dim offset As Integer = 8
            Elements = New List(Of Element)
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

        End Sub
        <Serializable>
        Public Class Element
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
    Public Class CMParser
        Const GUARD_WRAP_IDENTIFIER As Integer = &HFFFFFFFE
        Const UNUSED_WRAP_IDENTIFIER As Integer = &HFFFFFFFF

        Public a_CMBuffer As Byte() = {}

        <Xml.Serialization.XmlIgnore> Public a_PageID As Integer
        <Xml.Serialization.XmlIgnore> Public a_Offset As Integer
        Public a_UnProt As Integer = 0
        <Xml.Serialization.XmlIgnore> Public a_Key As Integer
        <Xml.Serialization.XmlIgnore> Public a_Index As Integer
        Public a_Err As Integer = 0
        <Xml.Serialization.XmlIgnore> Public a_Buffer As Byte()
        <Xml.Serialization.XmlIgnore> Public at_Offset As Byte()
        <Xml.Serialization.XmlIgnore> Public a_Length As Integer = 0
        Public a_CleansRemaining As Integer = 0
        Public a_CleanLength As Double
        Public a_NWraps As Integer = 0
        Public a_TapeDirLength As Integer = 16
        Public a_SetsPerWrap As Integer = 0
        <Xml.Serialization.XmlIgnore> Public a_SetID As Integer = 0
        <Xml.Serialization.XmlIgnore> Public a_LastID As Integer = 0
        Public a_Barcode As String
        <Xml.Serialization.XmlIgnore> Public a_AttributeID As Integer
        <Xml.Serialization.XmlIgnore> Public a_AttributeLength As Integer
        Public a_HdrLength As Integer
        Public a_DriveTypeIdentifier As Integer = 0
        <Xml.Serialization.XmlIgnore> Public a_OutputStr As String = ""
        Public a_TapeWritePassPartition As Integer = 0
        Public a_NumPartitions As New List(Of EOD)
        <Xml.Serialization.XmlIgnore> Public a_PartitionKey As Integer
        <Xml.Serialization.XmlIgnore> Public a_set As Integer
        Public g_ValidCM As Boolean = False

        Public g_Channels As Integer = 8
        Public g_LoadCount As Integer = 0
        Public g_CartridgeSN As String = "          "
        Public g_TPM As Integer = 0
        Public g_Barcode As String = "        "
        Public g_DHLTimeStamp As Integer = 0
        Public g_FaultLogSize As Integer = 0
        Public g_LtoPearlFlagEnable As Integer = 0
        Public g_DHLPowerCount As Integer = 0
        Public g_DHLPocCount As Integer = 0


        Public PageData As New List(Of Page)
        Public CartridgeMfgData As New Cartridge_mfg
        Public MediaMfgData As New Media_mfg
        Public a_UsageData As New List(Of UsagePage)
        Public UsageData As New List(Of Usage)
        Public StatusData As New TapeStatus
        Public InitialisationData As New Initialisation
        Public PartitionEOD As New List(Of EOD)
        Public CartridgeContentData As New CartridgeContent
        Public TapeDirectoryData As New TapeDirectory
        Public SuspendWriteData As New SuspendWrite
        Public ApplicationSpecificData As New ApplicationSpecific
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
        <Serializable> Public Class UsagePage
            Public index As Integer
            Public data0 As Byte()
            Public data1 As Integer
        End Class
        <Serializable> Public Class Page
            Public a_Key As Integer
            Public Version As Integer
            Public Offset As Integer = -1
            Public Length As Integer = -1
            Public Type As TypeDef
            Public Enum TypeDef
                unprotected
                [protected]
            End Enum
        End Class
        <Serializable> Public Class Cartridge_mfg
            Public TapeVendor As String
            Public CartridgeSN As String
            Public CartridgeType As Integer
            Public Format As String
            Public ReadOnly Property IsLTO3Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt.Contains("LTO-3") OrElse fmt.Contains("LTO-4") OrElse fmt.Contains("LTO-5") OrElse fmt.Contains("LTO-6") OrElse fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO4Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt.Contains("LTO-4") OrElse fmt.Contains("LTO-5") OrElse fmt.Contains("LTO-6") OrElse fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO5Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt.Contains("LTO-5") OrElse fmt.Contains("LTO-6") OrElse fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO6Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt.Contains("LTO-6") OrElse fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
                End Get
            End Property
            Public ReadOnly Property IsLTO7Plus As Boolean
                Get
                    Dim fmt As String = Format
                    Return fmt.Contains("LTO-7") OrElse fmt.Contains("LTO-8") OrElse fmt.Contains("LTO-9")
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

            Public MfgDate As String
            Public TapeLength As Integer = 0
            Public MediaCode As Integer
            Public ParticleType As particle
            Public SubstrateType As substrate
            Public Servo_Band_ID As svbid
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
        <Serializable> Public Class Usage
            Public Index As Integer

            Public PageID As Integer
            Public DrvSN As String
            Public ThreadCount As Integer
            Public SetsWritten As Long
            Public SetsRead As Long
            Public TotalSets As Long
            Public WriteRetries As Integer
            Public ReadRetries As Integer
            Public UnRecovWrites As Integer
            Public UnRecovReads As Integer
            Public SuspendedWrites As Integer
            Public FatalSusWrites As Integer
            Public SuspendedAppendWrites As Integer
            Public LP3Passes As Integer
            Public MidpointPasses As Integer
            Public MaxTapeTemp As Integer

            Public CCQWriteFails As Integer
            Public C2RecovErrors As Integer
            Public DirectionChanges As Integer
            Public TapePullingTime As Integer
            Public TapeMetresPulled As Integer
            Public Repositions As Integer
            Public TotalLoadUnloads As Integer
            Public StreamFails As Integer

            Public MaxDriveTemp As Double
            Public MinDriveTemp As Double

            Public LifeSetsWritten As Integer
            Public LifeSetsRead As Integer
            Public LifeWriteRetries As Integer
            Public LifeReadRetries As Integer
            Public LifeUnRecovWrites As Integer
            Public LifeUnRecovReads As Integer
            Public LifeSuspendedWrites As Integer
            Public LifeFatalSuspWrites As Integer
            Public LifeTapeMetresPulled As Integer

            Public LifeSuspAppendWrites As Integer
            Public LifeLP3Passes As Integer
            Public LifeMidpointPasses As Integer

        End Class
        <Serializable> Public Class Media_mfg
            Public MediaMfgDate As String
            Public MediaVendor As String
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
        <Serializable> Public Class TapeStatus
            Public ThreadCount As Integer
            Public EncryptedData As Boolean
            Public LastLocation As Integer = 0
        End Class
        <Serializable> Public Class Initialisation
            Public LP1 As Integer
            Public LP2 As Integer
            Public LP3 As Integer
        End Class
        <Serializable> Public Class EOD
            Public Partition As Integer
            Public Dataset As Integer
            Public WrapNumber As Integer
            Public Validity As Integer
            Public PhysicalPosition As Integer
        End Class
        <Serializable> Public Class CartridgeContent
            Public Drive_Id As String
            Public Cartridge_Content As Integer
            Public PartitionedCartridge As Boolean
            Public Type_M_Cartridge As Boolean
            Public Drive_Firmware_Id As String
        End Class
        <Serializable> Public Class TapeDirectory
            Public FID_Tape_Write_Pass_Partition_0 As Integer
            Public FID_Tape_Write_Pass_Partition_1 As Integer
            Public FID_Tape_Write_Pass_Partition_2 As Integer
            Public FID_Tape_Write_Pass_Partition_3 As Integer
            Public Wrap As String
            Public WrapEntryInfo As New List(Of WrapEntryItemSet)
            Public CapacityLoss As New List(Of Double)
            Public DatasetsOnWrapData As New List(Of Dataset)
            <Serializable> Public Class Dataset
                Public Index As Integer
                Public Data As Integer
            End Class
            <Serializable> Public Class WrapEntryItemSet
                Public Index As Integer
                Public Content As String
                Public RawData As Integer()
                Public RecCount As Integer
                Public FileMarkCount As Integer
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
            Public DataSetList As New List(Of DataInfo)
            Public WTapePassList As New List(Of DataInfo)
            <Serializable> Public Class DataInfo
                Public Index As Integer
                Public Value As Integer
            End Class
        End Class
        <Serializable> Public Class ApplicationSpecific
            Public Barcode As String
            Public Application_vendor As String
            Public Application_name As String
            Public Application_version As String
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
                            If CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).Format.Contains("LTO-8") Then
                                If .MediaMfgDate.StartsWith("22") Then
                                    CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).Servo_Band_ID = Cartridge_mfg.svbid.legacy_UDIM
                                ElseIf .MediaVendor.StartsWith(">>") Then
                                    CType(g_CM(gtype.cartridge_mfg), Cartridge_mfg).Servo_Band_ID = Cartridge_mfg.svbid.non_UDIM
                                End If
                            End If
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
                                ElseIf a_setID = 0 Then
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
                                TapeDirectoryEntry = substr(a_Buffer, a_HdrLength + a_TapeDirLength * a_Index, 32)
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
            a_CMBuffer = ReadBuffer(TapeDrive, BufferID)
            If a_CMBuffer.Length = 0 Then a_CMBuffer = ReadBuffer(TapeDrive, &H5)
            RunParse()
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
                Dim Medium_Manufacturer As String = Me.CartridgeMfgData.TapeVendor 'TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 4, 0).AsString
                Output.Append(("| Manufacturer: ".PadRight(28) & Medium_Manufacturer).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Manufacturer: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
            End Try
            Try
                Dim Medium_Man_Date As String = Me.CartridgeMfgData.MfgDate 'TapeUtils.MAMAttribute.FromTapeDrive(ConfTapeDrive, 4, 6).AsString
                Output.Append(("| Manufacture date: ".PadRight(28) & Medium_Man_Date).PadRight(74) & "|" & vbCrLf)
            Catch ex As Exception
                Output.Append(("| Manufacture date: ".PadRight(28) & "Not available").PadRight(74) & "|" & vbCrLf)
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
                Output.Append("| CM data parsing failed.".PadRight(74) & "|" & vbCrLf)
            End Try
            Return Output.ToString()
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
End Class
