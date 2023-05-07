Imports System.Runtime.InteropServices
Imports System.Text

Public Class TapeUtils
    Private Declare Function _GetTapeDriveList Lib "LtfsCommand.dll" () As IntPtr
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
    Public Shared Function ReadBlock(TapeDrive As String, Optional ByRef sense As Byte() = Nothing, Optional ByVal BlockSizeLimit As UInteger = &H80000) As Byte()
        Dim senseRaw(63) As Byte
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
        Dim DataLen As Integer = BlockSizeLimit - DiffBytes
        Dim RawData(DataLen - 1) As Byte
        Marshal.Copy(RawDataU, RawData, 0, DataLen)
        Marshal.FreeHGlobal(RawDataU)
        Return RawData
    End Function
    Public Shared Function ReadToFileMark(TapeDrive As String, Optional ByVal BlockSizeLimit As UInteger = &H80000) As Byte()
        Dim param As Byte() = TapeUtils.SCSIReadParam(TapeDrive, {&H34, 0, 0, 0, 0, 0, 0, 0, 0, 0}, 20)
        Dim buffer As New List(Of Byte)
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

        SCSIReadParam(TapeDrive, {&H92, DestType << 3 Or 2, 0, Partition,
                                        BlockAddress >> 56 And &HFF, BlockAddress >> 48 And &HFF, BlockAddress >> 40 And &HFF, BlockAddress >> 32 And &HFF,
                                        BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                        0, 0, 0, 0}, 64, Function(senseData As Byte()) As Boolean
                                                             sense = senseData
                                                             Return True
                                                         End Function)
        Dim Add_Code As UInt16 = CInt(sense(12)) << 8 Or sense(13)
        If Add_Code <> 0 Then
            If DestType = LocateDestType.EOD Then
                If Not ReadPosition(TapeDrive).EOP Then
                    SendSCSICommand(TapeDrive, {&H11, 3, 0, 0, 0, 0}, Nothing, 1, Function(senseData As Byte()) As Boolean
                                                                                      sense = senseData
                                                                                      Return True
                                                                                  End Function)
                End If
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
        _TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, dataBuffer, paramLen, 1, 60000, senseBuffer)
        Marshal.Copy(senseBuffer, senseData, 0, 64)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(senseBuffer)
        If senseReport IsNot Nothing Then senseReport(senseData)
        Return dataBuffer
    End Function
    Public Shared Function ModeSense(TapeDrive As String, PageID As Byte, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Byte()
        Dim Header As Byte() = SCSIReadParam(TapeDrive, {&H1A, 0, PageID, 0, 4, 0}, 4)
        Dim PageLen As Byte = Header(0)
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
        WriteFileMark(TapeDrive, 0)
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
        Public Sub New()

        End Sub
        Public Sub New(TapeDrive As String)
            Dim data As PositionData = ReadPosition(TapeDrive)
            BOP = data.BOP
            EOP = data.EOP
            MPU = data.MPU
            PartitionNumber = data.PartitionNumber
            BlockNumber = data.BlockNumber
            FileNumber = data.FileNumber
            SetNumber = data.SetNumber
        End Sub
        Public Overrides Function ToString() As String
            Dim Xtrs As String = " "
            If BOP Then Xtrs &= "BOP"
            If EOP Then Xtrs &= "EOP"
            If MPU Then Xtrs &= "MPU"
            Return $"P{PartitionNumber} B{BlockNumber} FM{FileNumber} SET{SetNumber}{Xtrs}"
        End Function
    End Class
    Public Shared Function ReadPosition(TapeDrive As String) As PositionData
        Dim param As Byte() = TapeUtils.SCSIReadParam(TapeDrive, {&H34, 6, 0, 0, 0, 0, 0, 0, 0, 0}, 32)
        Dim result As New PositionData
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
        Dim sense(63) As Byte
        Dim cdbData As Byte() = {&HA, 0, Length >> 16 And &HFF, Length >> 8 And &HFF, Length And &HFF, 0}
        Dim cdb As IntPtr = Marshal.AllocHGlobal(cdbData.Length)
        Marshal.Copy(cdbData, 0, cdb, cdbData.Length)
        Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(64)
        Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, Data, Length, 0, 60000, senseBufferPtr)
        If senseEnabled Then Marshal.Copy(senseBufferPtr, sense, 0, 64)
        Marshal.FreeHGlobal(cdb)
        Marshal.FreeHGlobal(senseBufferPtr)
        If Not succ Then Throw New Exception("SCSI Failure")
        Return {0, 0, 0}
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
    Public Shared Function Write(TapeDrive As String, sourceFile As String, Optional ByVal BlockLen As Integer = 524288, Optional ByVal senseEnabled As Boolean = False) As Byte()
        Dim sense(63) As Byte
        Dim senseBufferPtr As IntPtr = Marshal.AllocHGlobal(64)
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
            While My.Computer.FileSystem.FileExists(tmpf)
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
            My.Computer.FileSystem.DeleteFile(tmpf)
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
    Public Shared Function SendSCSICommand(TapeDrive As String, cdbData As Byte(), Optional ByRef Data As Byte() = Nothing, Optional DataIn As Byte = 2, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
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
        Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(TapeDrive, cdb, cdbData.Length, dataBufferPtr, dataLen, DataIn, 60000, senseBufferPtr)
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
                                  Optional ByVal OnError As Action(Of String) = Nothing) As Boolean
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
                If ExtraPartitionCount > 1 Then ExtraPartitionCount = 1

                'Set Capacity
                ProgressReport("Set Capacity..")
                If TapeUtils.SendSCSICommand(TapeDrive, {&HB, 0, 0, &HFF, &HFF, 0}) Then
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
                    If TapeUtils.SendSCSICommand(TapeDrive, {&H15, &H10, 0, 0, &H10, 0}, {0, 0, &H10, 0, &H11, &HA, MaxExtraPartitionAllowed, 1, &H3C, 3, 9, 0, 0, 1, &HFF, &HFF}, 0) Then
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
    Public Shared Function RawDump(TapeDrive As String, OutputFile As String, BlockAddress As Long, ByteOffset As Long, FileOffset As Long, Partition As Long, TotalBytes As Long, ByRef StopFlag As Boolean, Optional ByVal BlockSize As Long = 524288, Optional ByVal ProgressReport As Func(Of Long, Boolean) = Nothing, Optional ByVal CreateNew As Boolean = True) As Boolean
        If Not ReserveUnit(TapeDrive) Then Return False
        If Not PreventMediaRemoval(TapeDrive) Then
            ReleaseUnit(TapeDrive)
            Return False
        End If
        If Locate(TapeDrive, BlockAddress, Partition, LocateDestType.Block) <> 0 Then
            AllowMediaRemoval(TapeDrive)
            ReleaseUnit(TapeDrive)
            Return False
        End If
        Try
            If CreateNew Then My.Computer.FileSystem.WriteAllBytes(OutputFile, {}, False)
        Catch ex As Exception
            AllowMediaRemoval(TapeDrive)
            ReleaseUnit(TapeDrive)
            Return False
        End Try
        Dim fs As IO.FileStream
        Try
            fs = IO.File.Open(OutputFile, IO.FileMode.OpenOrCreate, IO.FileAccess.ReadWrite)
        Catch ex As Exception
            AllowMediaRemoval(TapeDrive)
            ReleaseUnit(TapeDrive)
            Return False
        End Try
        Try
            fs.Seek(FileOffset, IO.SeekOrigin.Begin)
            Dim ReadedSize As Long = 0
            While (ReadedSize < TotalBytes + ByteOffset) And Not StopFlag
                Dim Data As Byte() = ReadBlock(TapeDrive, Nothing, Math.Min(BlockSize, TotalBytes + ByteOffset - ReadedSize))
                If Data.Length = 0 Then
                    AllowMediaRemoval(TapeDrive)
                    ReleaseUnit(TapeDrive)
                    Return False
                End If
                ReadedSize += Data.Length
                fs.Write(Data, ByteOffset, Data.Length - ByteOffset)
                If ProgressReport IsNot Nothing Then StopFlag = ProgressReport(Data.Length - ByteOffset)
                ByteOffset = 0
            End While
            AllowMediaRemoval(TapeDrive)
            ReleaseUnit(TapeDrive)
            If StopFlag Then
                fs.Close()
                My.Computer.FileSystem.DeleteFile(OutputFile)
                AllowMediaRemoval(TapeDrive)
                ReleaseUnit(TapeDrive)
                Return True
            End If
        Catch ex As Exception
            MessageBox.Show(ex.ToString)
            fs.Close()
            My.Computer.FileSystem.DeleteFile(OutputFile)
            AllowMediaRemoval(TapeDrive)
            ReleaseUnit(TapeDrive)
            Return False
        End Try
        fs.Flush()
        fs.Close()
        Return True
    End Function
    Public Shared Function ParseTimeStamp(t As String) As Date
        'yyyy-MM-ddTHH:mm:ss.fffffff00Z
        Return Date.ParseExact(t, "yyyy-MM-ddTHH:mm:ss.fffffff00Z", Globalization.CultureInfo.InvariantCulture)
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
End Class
