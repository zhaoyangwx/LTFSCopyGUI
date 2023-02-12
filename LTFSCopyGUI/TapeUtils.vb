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
                                           bufferLength As UInt32,
                                           dataIn As Byte,
                                           timeoutValue As UInt32,
                                           senseBuffer As UIntPtr) As Boolean

    End Function
    <DllImport("LtfsCommand.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)>
    Public Shared Function _TapeSCSIIOCtlFull(tapeDrive As String,
                                           cdb As IntPtr,
                                           cdbLength As Byte,
                                           dataBuffer As IntPtr,
                                           bufferLength As UInt32,
                                           dataIn As Byte,
                                           timeoutValue As UInt32,
                                           senseBuffer As IntPtr) As Boolean

    End Function
    Public Shared Function ReadAppInfo(tapeDrive As String) As String
        'TC_MAM_APPLICATION_VENDOR = 0x0800 LEN = 8
        'TC_MAM_APPLICATION_NAME = 0x0801 = 0x0800 LEN = 32
        'TC_MAM_APPLICATION_VERSION = 0x0802 = 0x0800 LEN = 8
        Return ReadMAMAttributeString(tapeDrive, 8, 0).TrimEnd(" ") &
            " " & ReadMAMAttributeString(tapeDrive, 8, 1).TrimEnd(" ") &
            " " & ReadMAMAttributeString(tapeDrive, 8, 2).TrimEnd(" ")
    End Function
    Public Shared Function ReadBarcode(tapeDrive As String) As String
        'TC_MAM_BARCODE = 0x0806 LEN = 32
        Return ReadMAMAttributeString(tapeDrive, 8, 6)
    End Function

    Public Class BlockLimits
        Public MaximumBlockLength As UInt64
        Public MinimumBlockLength As UInt16
        Public Shared Widening Operator CType(data As BlockLimits) As UInt64
            Return data.MaximumBlockLength
        End Operator
    End Class
    Public Shared Function ReadBlockLimits(tapeDrive As String) As BlockLimits
        Dim data As Byte() = SCSIReadParam(tapeDrive, {5, 0, 0, 0, 0, 0}, 6)
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
    Public Shared Function ReadBlock(TapeDrive As String, ByRef sense As Byte(), Optional ByVal BlockSizeLimit As UInteger = &H80000) As Byte()
        Dim senseRaw(63) As Byte
        Dim RawData As Byte() = SCSIReadParam(TapeDrive, {8, 0, BlockSizeLimit >> 16 And &HFF, BlockSizeLimit >> 8 And &HFF, BlockSizeLimit And &HFF, 0},
                                              BlockSizeLimit, Function(senseData As Byte()) As Boolean
                                                                  senseRaw = senseData
                                                                  Return True
                                                              End Function)
        sense = senseRaw
        Dim DiffBytes As Int32 = CLng(sense(3)) << 24 Or CLng(sense(4)) << 16 Or CLng(sense(5)) << 8 Or sense(6)
        If DiffBytes > 0 Then
            Return RawData.Take(RawData.Length - DiffBytes).ToArray
        Else
            Return RawData
        End If
    End Function

    Public Shared Function ReadEOWPosition(TapeDrive As String) As Byte()
        Dim lenData As Byte() = SCSIReadParam(TapeDrive, {&HA3, &H1F, &H45, 2, 0, 0, 0, 0, 0, 2, 0, 0}, 2)
        Dim rawParam As Byte() = SCSIReadParam(TapeDrive, {&HA3, &H1F, &H45, 2, 0, 0, 0, 0, lenData(0), lenData(1), 0, 0}, CInt(lenData(0)) << 8 Or lenData(1))
        Return rawParam.Skip(4).ToArray()
    End Function
    Public Shared Function Locate(TapeDrive As String, BlockAddress As UInt64, Partition As Byte) As UInt16
        Dim sense(63) As Byte
        Dim d As Byte() = SCSIReadParam(TapeDrive, {&H2B, 2, 0,
                                        BlockAddress >> 24 And &HFF, BlockAddress >> 16 And &HFF, BlockAddress >> 8 And &HFF, BlockAddress And &HFF,
                                        0, Partition, 0}, 64, Function(senseData As Byte()) As Boolean
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
    Public Shared Function ModeSense(TapeDrive As String, PageID As Byte, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Byte()
        Dim Header As Byte() = SCSIReadParam(TapeDrive, {&H1A, 0, PageID, 0, 4, 0}, 4)
        Dim PageLen As Byte = Header(0)
        Dim DescripterLen As Byte = Header(3)
        Return SCSIReadParam(TapeDrive, {&H1A, 0, PageID, 0, PageLen + 1, 0}, PageLen + 1, senseReport).Skip(4 + DescripterLen).ToArray()
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
    Public Shared Function ReadMAMAttributeString(tapeDrive As String, PageCode_H As Byte, PageCode_L As Byte) As String 'TC_MAM_BARCODE = 0x0806 LEN = 32
        Return System.Text.Encoding.UTF8.GetString(GetMAMAttributeBytes(tapeDrive, PageCode_H, PageCode_L).ToArray())
    End Function
    Public Shared Function ReadMAMAttributeByteString(tapeDrive As String, PageCode_H As Byte, PageCode_L As Byte) As String 'TC_MAM_BARCODE = 0x0806 LEN = 32
        Return Byte2Hex(GetMAMAttributeBytes(tapeDrive, PageCode_H, PageCode_L).ToArray())
    End Function


    Public Shared Function GetMAMAttributeBytes(tapeDrive As String, PageCode_H As Byte, PageCode_L As Byte) As Byte()
        Dim DATA_LEN As Integer = 0
        Dim cdb As IntPtr = Marshal.AllocHGlobal(16)
        Dim cdbData As Byte() = {&H8C, 0, 0, 0, 0, 0, 0, 0,
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
            succ = _TapeSCSIIOCtlFull(tapeDrive, cdb, 16, dataBuffer, DATA_LEN + 9, 1, 60000, senseBuffer)
        Catch ex As Exception
            MessageBox.Show("SCSIIOErr")
        End Try
        Marshal.Copy(dataBuffer, BCArray, 0, DATA_LEN + 9)
        If succ Then
            DATA_LEN = CInt(BCArray(7)) << 8 Or BCArray(8)
            If DATA_LEN > 0 Then
                Dim dataBuffer2 As IntPtr = Marshal.AllocHGlobal(DATA_LEN + 9)
                Dim BCArray2(DATA_LEN + 8) As Byte
                Marshal.Copy(BCArray2, 0, dataBuffer2, DATA_LEN + 9)
                cdbData = {&H8C, 0, 0, 0, 0, 0, 0, 0,
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
                    succ = _TapeSCSIIOCtlFull(tapeDrive, cdb2, 16, dataBuffer2, DATA_LEN + 9, 1, 60000, senseBuffer)
                Catch ex As Exception
                    MessageBox.Show("SCSIIOErr2")
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
    Public Shared Function Byte2Hex(bytes As Byte()) As String
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
    <Serializable>
    Public Class MAMAttributeList
        Public Property Content As New List(Of MAMAttribute)
        Public Function GetSerializedText() As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(MAMAttributeList))
            Dim tmpf As String = My.Computer.FileSystem.CurrentDirectory & "\" & Now.ToString("MAM_yyyyMMdd_hhmmss.tmp")
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
        Public Shared Function FromTapeDrive(tapeDrive As String, PageCode_H As Byte, PageCode_L As Byte) As MAMAttribute
            Dim RawData As Byte() = GetMAMAttributeBytes(tapeDrive, PageCode_H, PageCode_L)
            If RawData.Length = 0 Then Return Nothing
            Return New MAMAttribute With {.ID = (CUShort(PageCode_H) << 8) Or PageCode_L, .RawData = RawData}
        End Function
        Public Shared Function FromTapeDrive(tapeDrive As String, PageCode As UInt16) As MAMAttribute
            Return FromTapeDrive(tapeDrive, (PageCode >> 8) And &HFF, PageCode And &HFF)
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
    Public Shared Function SendSCSICommand(tapeDrive As String, cdbData As Byte(), Optional Data As Byte() = Nothing, Optional DataIn As Byte = 2, Optional ByVal senseReport As Func(Of Byte(), Boolean) = Nothing) As Boolean
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

        Dim senseBuffer(64) As Byte
        Dim succ As Boolean = TapeUtils._TapeSCSIIOCtlFull(tapeDrive, cdb, cdbData.Length, dataBufferPtr, dataLen, DataIn, 60000, senseBufferPtr)
        If senseReport IsNot Nothing Then
            Marshal.Copy(senseBufferPtr, senseBuffer, 0, 64)
            senseReport(senseBuffer)
        End If
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
