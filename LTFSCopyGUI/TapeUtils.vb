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

    Public Shared Function RawDump(TapeDrive As String, BufferID As Byte) As Byte()
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
    Public Shared Function SendSCSICommand(tapeDrive As String, cdbData As Byte(), Optional Data As Byte() = Nothing, Optional DataIn As Byte = 2) As Boolean
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
