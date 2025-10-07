Imports System.Runtime.InteropServices
Imports System.Threading
Imports ISCSI
Imports ISCSI.Server
Imports SCSI
Public Class iSCSIService
    Public driveHandle As IntPtr
    Public BlockSize As Integer = 524288
    Public ExtraPartitionCount As Integer = 1
    Public port As Integer = 3261
    Public Event LogPrint(s As String)
    Public svc As ISCSI.Server.ISCSIServer
    Public target As ISCSITarget
    Public Class SCSIDirectInterface
        Implements SCSITargetInterface
        Public driveHandle As IntPtr
        Public Event OnStandardInquiry As EventHandler(Of StandardInquiryEventArgs) Implements SCSITargetInterface.OnStandardInquiry
        Public Event OnUnitSerialNumberInquiry As EventHandler(Of UnitSerialNumberInquiryEventArgs) Implements SCSITargetInterface.OnUnitSerialNumberInquiry
        Public Event OnDeviceIdentificationInquiry As EventHandler(Of DeviceIdentificationInquiryEventArgs) Implements SCSITargetInterface.OnDeviceIdentificationInquiry
        Public QueueTaskProcessor As Task
        Private Shared _DataDir As Dictionary(Of Byte(), Byte)
        Public Shared ReadOnly Property DataDir As Dictionary(Of Byte(), Byte)
            Get
                If _DataDir Is Nothing Then
                    _DataDir = New Dictionary(Of Byte(), Byte)
                    _DataDir.Add({&H0}, 1)
                    _DataDir.Add({&H1}, 1)
                    _DataDir.Add({&H3}, 1)
                    _DataDir.Add({&H4}, 1)
                    _DataDir.Add({&H5}, 1)
                    _DataDir.Add({&H8}, 1)
                    _DataDir.Add({&HA}, 0)
                    _DataDir.Add({&HB}, 1)
                    _DataDir.Add({&H10}, 1)
                    _DataDir.Add({&H11}, 1)
                    _DataDir.Add({&H12}, 1)
                    _DataDir.Add({&H13}, 1)
                    _DataDir.Add({&H15}, 0)
                    _DataDir.Add({&H16}, 1)
                    _DataDir.Add({&H17}, 1)
                    _DataDir.Add({&H19}, 1)
                    _DataDir.Add({&H1A}, 1)
                    _DataDir.Add({&H1B}, 1)
                    _DataDir.Add({&H1C}, 1)
                    _DataDir.Add({&H1D}, 0)
                    _DataDir.Add({&H1E}, 1)
                    _DataDir.Add({&H25}, 1)
                    _DataDir.Add({&H28}, 1)
                    _DataDir.Add({&H2B}, 1)
                    _DataDir.Add({&H34}, 1)
                    _DataDir.Add({&H3B}, 0)
                    _DataDir.Add({&H3C}, 1)
                    _DataDir.Add({&H43}, 1)
                    _DataDir.Add({&H44}, 1)
                    _DataDir.Add({&H4C}, 0)
                    _DataDir.Add({&H4D}, 1)
                    _DataDir.Add({&H55}, 0)
                    _DataDir.Add({&H56}, 1)
                    _DataDir.Add({&H57}, 1)
                    _DataDir.Add({&H5A}, 1)
                    _DataDir.Add({&H5E}, 1)
                    _DataDir.Add({&H5F}, 0)
                    _DataDir.Add({&H8C}, 1)
                    _DataDir.Add({&H8D}, 0)
                    _DataDir.Add({&H91}, 1)
                    _DataDir.Add({&H92}, 1)
                    _DataDir.Add({&HA0}, 1)
                    _DataDir.Add({&HA2}, 1)
                    _DataDir.Add({&HA3, &H5}, 1)
                    _DataDir.Add({&HA3, &HA}, 1)
                    _DataDir.Add({&HA3, &HC}, 1)
                    _DataDir.Add({&HA3, &HD}, 1)
                    _DataDir.Add({&HA3, &HF}, 1)
                    _DataDir.Add({&HA3, &H1F, &H0}, 0)
                    _DataDir.Add({&HA3, &H1F, &H1}, 1)
                    _DataDir.Add({&HA3, &H1F, &H5}, 1)
                    _DataDir.Add({&HA3, &H1F, &H7}, 1)
                    _DataDir.Add({&HA3, &H1F, &H8}, 1)
                    _DataDir.Add({&HA3, &H1F, &H9}, 1)
                    _DataDir.Add({&HA3, &H1F, &HA}, 1)
                    _DataDir.Add({&HA3, &H1F, &HB}, 1)
                    _DataDir.Add({&HA3, &H1F, &HD}, 1)
                    _DataDir.Add({&HA3, &H1F, &HE}, 1)
                    _DataDir.Add({&HA3, &H1F, &H10}, 1)
                    _DataDir.Add({&HA3, &H1F, &H12}, 1)
                    _DataDir.Add({&HA3, &H1F, &H15}, 1)
                    _DataDir.Add({&HA3, &H1F, &H18}, 1)
                    _DataDir.Add({&HA4, &H1F, &H5}, 1)
                    _DataDir.Add({&HA4, &H1F, &H6}, 1)
                    _DataDir.Add({&HA4, &H1F, &H7}, 1)
                    _DataDir.Add({&HA4, &H1F, &HC}, 1)
                    _DataDir.Add({&HA4, &H1F, &H14}, 1)
                    _DataDir.Add({&HA4, &H6}, 0)
                    _DataDir.Add({&HA4, &H1F, &HA}, 0)
                    _DataDir.Add({&HA4, &H1F, &HB}, 0)
                    _DataDir.Add({&HA4, &H1F, &HD}, 0)
                    _DataDir.Add({&HA4, &H1F, &HE}, 0)
                    _DataDir.Add({&HA4, &H1F, &H12}, 0)
                    _DataDir.Add({&HAB}, 1)
                    _DataDir.Add({&HB5}, 0)
                    _DataDir.Add({&HC2}, 1)
                End If
                Return _DataDir
            End Get
        End Property
        Public Function GetDataDir(data As Byte()) As Byte
            Dim dataabbr As String = BitConverter.ToString(data, 0, 3)
            For Each k As Byte() In DataDir.Keys
                If dataabbr.StartsWith(BitConverter.ToString(k)) Then
                    Return DataDir(k)
                End If
            Next
            Return 2
        End Function

        Public Sub StartProcessingQueue()
            _stopping = False
            QueueTaskProcessor = Task.Run(Sub()
                                              While Not _stopping OrElse _pendingTask IsNot Nothing
                                                  If _pendingTask Is Nothing Then
                                                      _commandLock.WaitOne()
                                                      If _stopping AndAlso _pendingTask Is Nothing Then Exit While
                                                  End If

                                                  Dim t As Task
                                                  SyncLock Me
                                                      t = _pendingTask
                                                      _pendingTask = Nothing
                                                  End SyncLock

                                                  If t IsNot Nothing Then
                                                      t.Start()
                                                      t.Wait()
                                                  End If
                                              End While
                                          End Sub)
        End Sub
        Public Sub StopProcessingQueue()
            If QueueTaskProcessor Is Nothing Then Exit Sub
            _stopping = True
            _commandLock.Set()

            Dim tstop As Task = QueueTaskProcessor
            QueueTaskProcessor = Nothing
            tstop.Wait()
        End Sub
        Public Sub New(handle As IntPtr)
            AddHandler Me.OnStandardInquiry,
                Sub(sender As Object, e As StandardInquiryEventArgs)
                    Dim devdata As TapeUtils.BlockDevice = TapeUtils.Inquiry(driveHandle)
                    If devdata Is Nothing Then devdata = New TapeUtils.BlockDevice
                    With e.Data
                        .DriveSerialNumber = devdata.SerialNumber
                        .VendorIdentification = devdata.VendorId
                        .ProductIdentification = devdata.ProductId
                    End With
                End Sub
            AddHandler Me.OnUnitSerialNumberInquiry,
                Sub(sender As Object, e As UnitSerialNumberInquiryEventArgs)
                    Dim devdata As TapeUtils.BlockDevice = TapeUtils.Inquiry(driveHandle)
                    If devdata Is Nothing Then devdata = New TapeUtils.BlockDevice
                    With e.Page
                        .ProductSerialNumber = devdata.SerialNumber
                    End With
                End Sub
            'AddHandler Me.OnDeviceIdentificationInquiry,
            '    Sub(sender As Object, e As DeviceIdentificationInquiryEventArgs)
            '    End Sub
            driveHandle = handle
            If QueueTaskProcessor IsNot Nothing Then StopProcessingQueue()
            StartProcessingQueue()
        End Sub

        Private _pendingTask As Task = Nothing
        Private _stopping As Boolean = False
        Private ReadOnly _commandLock As AutoResetEvent = New AutoResetEvent(False)

        Public Function GetValue(data As Byte(), startbyte As Integer, endbyte As Integer) As Long
            Dim result As Long = 0
            For i As Integer = startbyte To endbyte
                result <<= 8
                result = result Or data(i)
            Next
            Return result
        End Function
        Public Sub QueueCommand(commandBytes() As Byte, lun As LUNStructure, data() As Byte, task As Object, OnCommandCompleted As OnCommandCompleted) Implements SCSITargetInterface.QueueCommand
            Dim t = New Task(
                              Sub()
                                  Dim cmddir As Byte = GetDataDir(commandBytes)
                                  Dim datalen As Integer = data.Length

                                  If cmddir <> 0 Then
                                      datalen = 0
                                      Select Case commandBytes(0)
                                          Case &H3
                                              datalen = commandBytes(4)
                                          Case &H5
                                              datalen = 6
                                          Case &H8
                                              datalen = GetValue(commandBytes, 2, 4)
                                          Case &H12
                                              datalen = GetValue(commandBytes, 3, 4)
                                          Case &H1A
                                              datalen = commandBytes(4)
                                          Case &H1C
                                              datalen = GetValue(commandBytes, 3, 4)
                                          Case &H25
                                              datalen = 8
                                          Case &H28
                                              datalen = GetValue(commandBytes, 7, 8)
                                          Case &H34
                                              If commandBytes(1) = 0 Then
                                                  datalen = 20
                                              Else
                                                  datalen = 32
                                              End If
                                          Case &H3C
                                              datalen = GetValue(commandBytes, 6, 8)
                                          Case &H43
                                              datalen = Math.Max(GetValue(commandBytes, 7, 8), 20)
                                          Case &H44
                                              datalen = GetValue(commandBytes, 7, 8)
                                          Case &H4D
                                              datalen = GetValue(commandBytes, 7, 8)
                                          Case &H5A
                                              datalen = GetValue(commandBytes, 7, 8)
                                          Case &H5E
                                              datalen = GetValue(commandBytes, 7, 8)
                                          Case &H8C
                                              datalen = GetValue(commandBytes, 10, 13)
                                          Case &HA0
                                              datalen = Math.Min(32, GetValue(commandBytes, 6, 9))
                                          Case &HA2
                                              datalen = GetValue(commandBytes, 6, 9)
                                          Case &HA3
                                              Select Case commandBytes(1)
                                                  Case &H5, &HA, &HC, &HD, &HF
                                                      datalen = GetValue(commandBytes, 6, 9)
                                                  Case &H1F
                                                      Select Case commandBytes(2)
                                                          Case &H6, &H10, &H12, &H15
                                                              datalen = GetValue(commandBytes, 6, 9)
                                                          Case &H7, &HA, &HB, &HD, &HE, &H18
                                                              datalen = GetValue(commandBytes, 6, 7)
                                                          Case &H8, &H9
                                                              datalen = GetValue(commandBytes, 6, 8)
                                                          Case &H14
                                                              datalen = commandBytes(9)
                                                      End Select
                                              End Select
                                          Case &HAB
                                              datalen = GetValue(commandBytes, 6, 9)
                                      End Select
                                  End If
                                  Dim databuffer As IntPtr = Marshal.AllocHGlobal(datalen)
                                  Dim sense(63) As Byte
                                  If cmddir <> 1 Then Marshal.Copy(data, 0, databuffer, datalen)
                                  TapeUtils.TapeSCSIIOCtlUnmanaged(driveHandle, commandBytes, databuffer, datalen, cmddir, 24 * 3600, sense)
                                  Dim responsedata(datalen - 1) As Byte
                                  If cmddir <> 0 Then Marshal.Copy(databuffer, responsedata, 0, responsedata.Length)
                                  Marshal.FreeHGlobal(databuffer)
                                  Dim response As Byte()
                                  Dim status As SCSIStatusCodeName
                                  If sense(0) = 0 Then
                                      status = SCSIStatusCodeName.Good
                                      If cmddir <> 0 Then
                                          response = responsedata
                                      Else
                                          response = {}
                                      End If
                                  Else
                                      status = SCSIStatusCodeName.CheckCondition
                                      response = {sense.Length And &HFF, (sense.Length >> 8) And &HFF}
                                      response = response.Concat(sense).Concat(responsedata).ToArray()
                                  End If
                                  OnCommandCompleted(status, response, task)
                              End Sub)

            Do
                Dim placed = False
                SyncLock Me
                    If _pendingTask Is Nothing Then
                        _pendingTask = t
                        placed = True
                    End If
                End SyncLock
                If placed Then
                    _commandLock.Set()
                    Exit Do
                End If
                Thread.Sleep(1)
            Loop
        End Sub

        Public Function ExecuteCommand(commandBytes() As Byte, lun As LUNStructure, data() As Byte, ByRef response() As Byte) As SCSIStatusCodeName Implements SCSITargetInterface.ExecuteCommand
            Dim rdata() As Byte, retstatus As SCSIStatusCodeName
            QueueCommand(commandBytes, lun, data, Nothing, Sub(status As SCSIStatusCodeName, resp() As Byte, task As Object)
                                                               rdata = resp
                                                               retstatus = status
                                                           End Sub)
            While _pendingTask IsNot Nothing
                Thread.Sleep(1)
            End While
            response = rdata
            Return retstatus
        End Function
    End Class
    Public Sub New()

    End Sub
    Public Sub StartService(Optional ByVal TargetName As String = "iqn.2019-01.com.ltfscopygui:target1")
        svc = New ISCSIServer()
        target = New ISCSITarget(TargetName, New SCSIDirectInterface(driveHandle))
        svc.AddTarget(target)
        svc.Start(port)
    End Sub
    Public Sub StopService()
        svc.Stop()
    End Sub

End Class
