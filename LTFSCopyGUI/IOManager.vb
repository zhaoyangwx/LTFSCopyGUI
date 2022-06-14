Public Class IOManager
    Public Class fsReport
        Public fs As IO.BufferedStream
        Public Sub New()

        End Sub
        Public Sub New(fst As IO.BufferedStream)
            fs = fst
        End Sub
    End Class
    Public Event ErrorOccured(s As String)
    Public Shared Function SHA1(filename As String, Optional ByVal OnFinished As Action(Of String) = Nothing, Optional ByVal fs As fsReport = Nothing) As String
        If OnFinished Is Nothing Then
            Dim fsin0 As IO.FileStream = IO.File.Open(filename, IO.FileMode.Open, IO.FileAccess.Read)
            Dim fsin As New IO.BufferedStream(fsin0, 512 * 1024)
            If fs IsNot Nothing Then fs.fs = fsin
            Using algo As Security.Cryptography.SHA1 = Security.Cryptography.SHA1.Create()
                fsin.Position = 0
                Dim hashValue() As Byte
                hashValue = algo.ComputeHash(fsin)
                'While fsin.Read(block, 0, block.Length) > 0
                '
                'End While
                fsin.Close()
                Dim result As New Text.StringBuilder()
                For i As Integer = 0 To hashValue.Length - 1
                    result.Append(String.Format("{0:X}", hashValue(i)))
                Next
                Return result.ToString()
            End Using
        Else
            Dim thHash As New Threading.Thread(
                    Sub()
                        Dim fsin0 As IO.FileStream = IO.File.Open(filename, IO.FileMode.Open, IO.FileAccess.Read)
                        Dim fsin As New IO.BufferedStream(fsin0, 4 * 1024 * 1024)
                        If fs IsNot Nothing Then fs.fs = fsin
                        Using algo As Security.Cryptography.SHA1 = Security.Cryptography.SHA1.Create()
                            fsin.Position = 0
                            Dim hashValue() As Byte
                            hashValue = algo.ComputeHash(fsin)
                            fsin.Close()
                            Dim result As New Text.StringBuilder()
                            For i As Integer = 0 To hashValue.Length - 1
                                result.Append(String.Format("{0:X}", hashValue(i)))
                            Next
                            OnFinished(result.ToString)
                        End Using
                    End Sub)
            thHash.Start()
            Return ""
        End If
    End Function

    Public Class HashTask
        Public Event TaskStarted(Message As String)
        Public Event TaskCancelled(Message As String)
        Public Event TaskPaused(Message As String)
        Public Event TaskResumed(Message As String)
        Public Event TaskFinished(Message As String)
        Public Event ErrorOccured(Message As String)
        Public Event ProgressReport(Message As String)
        Public schema As ltfsindex
        Public IgnoreExisting As Boolean = True
        Private _BaseDirectory As String
        Public Property BaseDirectory As String
            Set(value As String)
                _BaseDirectory = value.TrimEnd("\")
            End Set
            Get
                Return _BaseDirectory
            End Get
        End Property
        Public OperationLock As New Object
        Private thHash As Threading.Thread
        Public Sub Start()
            SyncLock OperationLock
                If schema Is Nothing Then
                    RaiseEvent ErrorOccured("Error: No schema.")
                    Exit Sub
                End If
                If Status <> TaskStatus.Idle Then
                    Exit Sub
                End If
                Dim fs As New fsReport()
                Dim thProg As New Threading.Thread(
                    Sub()
                        While Status <> TaskStatus.Idle
                            Try
                                If fs.fs IsNot Nothing Then
                                    If Not fs.fs.CanSeek Then Exit Try
                                    If fs.fs.Length = 0 Then Exit Try
                                    RaiseEvent ProgressReport("#fmax" & 10000)
                                    RaiseEvent ProgressReport("#fval" & fs.fs.Position / fs.fs.Length * 10000)
                                    RaiseEvent ProgressReport("#dmax" & fs.fs.Length)
                                    RaiseEvent ProgressReport("#dval" & fs.fs.Position)
                                End If
                            Catch ex As Exception
                                'RaiseEvent ErrorOccured(ex.ToString)
                            End Try

                            Threading.Thread.Sleep(100)
                        End While
                    End Sub)
                thHash = New Threading.Thread(
                    Sub()
                        SyncLock OperationLock
                            _Status = TaskStatus.Running
                            thProg.Start()
                        End SyncLock
                        RaiseEvent ProgressReport("#max10000")
                        RaiseEvent ProgressReport("#val0")
                        Dim q As New List(Of ltfsindex.directory)
                        Dim flist As New List(Of ltfsindex.file)
                        For Each f As ltfsindex.file In schema._file
                            If Not f.Selected Then Continue For
                            f.fullpath = f.name
                            flist.Add(f)
                        Next
                        For Each d As ltfsindex.directory In schema._directory
                            If Not d.Selected Then Continue For
                            d.fullpath = ""
                            d.name = ""
                            q.Add(d)
                        Next
                        While q.Count > 0
                            Dim qtmp As New List(Of ltfsindex.directory)
                            For Each d As ltfsindex.directory In q
                                For Each f As ltfsindex.file In d.contents._file
                                    If Not f.Selected Then Continue For
                                    f.fullpath = d.fullpath & "\" & d.name & "\" & f.name
                                    f.fullpath = f.fullpath.TrimStart("\")
                                    flist.Add(f)
                                Next
                                For Each d2 As ltfsindex.directory In d.contents._directory
                                    If Not d2.Selected Then Continue For
                                    d2.fullpath = d.fullpath & "\" & d.name
                                    d2.fullpath = d2.fullpath.TrimStart("\")
                                    qtmp.Add(d2)
                                Next
                            Next
                            q = qtmp
                        End While
                        RaiseEvent ProgressReport("Sorting")
                        flist.Sort(New Comparison(Of ltfsindex.file)(Function(a As ltfsindex.file, b As ltfsindex.file) As Integer
                                                                         If a.extentinfo.Count = 0 Then Return a.extentinfo.Count.CompareTo(b.extentinfo.Count)
                                                                         If b.extentinfo.Count = 0 Then Return a.extentinfo.Count.CompareTo(b.extentinfo.Count)
                                                                         If a.extentinfo(0).partition <> b.extentinfo(0).partition Then
                                                                             Return a.extentinfo(0).partition.CompareTo(b.extentinfo(0).partition)
                                                                         End If
                                                                         Return a.extentinfo(0).startblock.CompareTo(b.extentinfo(0).startblock)
                                                                     End Function))
                        RaiseEvent ProgressReport("#max" & flist.Count)
                        RaiseEvent ProgressReport("#tmax" & flist.Count)
                        Dim progval As Integer = 0
                        For Each f As ltfsindex.file In flist
                            f.fullpath = My.Computer.FileSystem.CombinePath(BaseDirectory, f.fullpath)
                            If f.sha1 = "" Or Not IgnoreExisting Then
                                Try
                                    f.sha1 = SHA1(f.fullpath, Nothing, fs)
                                Catch ex As Exception
                                    RaiseEvent ErrorOccured(ex.ToString)
                                End Try
                            End If
                            Threading.Interlocked.Add(progval, 1)
                            RaiseEvent ProgressReport("#val" & progval)
                            RaiseEvent ProgressReport("#tval" & progval)
                            RaiseEvent ProgressReport(f.sha1 & " - " & f.fullpath)
                            Threading.Thread.Sleep(0)
                        Next
                        'RaiseEvent ProgressReport(Now.ToString)
                        SyncLock OperationLock
                            _Status = TaskStatus.Idle
                        End SyncLock
                        RaiseEvent TaskFinished("Finished")
                    End Sub)
                thHash.Start()
            End SyncLock
            RaiseEvent TaskStarted("")
        End Sub
        Public Sub [Stop]()
            SyncLock OperationLock
                Try
                    If Status = TaskStatus.Paused Then
                        thHash.Resume()
                        RaiseEvent TaskResumed("Resumed")
                    End If
                    thHash.Abort()
                    _Status = TaskStatus.Idle
                    RaiseEvent TaskCancelled("Cancelled")
                Catch ex As Exception
                    RaiseEvent ErrorOccured(ex.ToString)
                End Try
            End SyncLock
        End Sub
        Public Sub Pause()
            SyncLock OperationLock
                Try
                    thHash.Suspend()
                    _Status = TaskStatus.Paused
                    RaiseEvent TaskPaused("Paused")
                Catch ex As Exception
                    RaiseEvent ErrorOccured(ex.ToString)
                End Try
            End SyncLock
        End Sub
        Public Sub [Resume]()
            SyncLock OperationLock
                Try
                    thHash.Resume()
                    _Status = TaskStatus.Running
                    RaiseEvent TaskResumed("Resumed")
                Catch ex As Exception
                    RaiseEvent ErrorOccured(ex.ToString)
                End Try
            End SyncLock
        End Sub
        Public Enum TaskStatus
            Idle
            Running
            Paused
        End Enum
        Private _Status As TaskStatus = TaskStatus.Idle
        Public ReadOnly Property Status As TaskStatus
            Get
                Return _Status
            End Get
        End Property
    End Class
End Class
