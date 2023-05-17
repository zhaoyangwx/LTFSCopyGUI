Imports System.IO
Imports System.Security.Cryptography
Imports System.Threading
Imports LTFSCopyGUI

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
    Public Shared Function FormatSize(l As Long) As String
        If l < 1024 Then
            Return l & " Bytes"
        ElseIf l < 1024 ^ 2 Then
            Return (l / 1024).ToString("F2") & " KiB"
        ElseIf l < 1024 ^ 3 Then
            Return (l / 1024 ^ 2).ToString("F2") & " MiB"
        Else
            Return (l / 1024 ^ 3).ToString("F2") & " GiB"
        End If
    End Function
    Public Shared Function SHA1(filename As String, LogFile As String()) As String
        If LogFile.Contains("[hash] " & filename) Then

            Return LogFile(Array.IndexOf(LogFile, LogFile.First(Function(s As String) As Boolean
                                                                    Return s = "[hash] " & filename
                                                                End Function)) + 1).TrimStart(" ").Substring(0, 40)
        End If
        Return ""
    End Function
    Public Shared Function SHA1(filename As String, Optional ByVal OnFinished As Action(Of String) = Nothing, Optional ByVal fs As fsReport = Nothing, Optional ByVal OnFileReading As Action(Of EventedStream.ReadStreamEventArgs, EventedStream) = Nothing) As String
        If OnFinished Is Nothing Then

            Using fsin0 As IO.FileStream = IO.File.Open(filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                Dim fsinb As New IO.BufferedStream(fsin0, 512 * 1024)
                Dim fsine As New EventedStream With {.baseStream = fsinb}
                AddHandler fsine.Readed, Sub(args As EventedStream.ReadStreamEventArgs) OnFileReading(args, fsine)
                'Dim fsin As New IO.BufferedStream(fsine, 512 * 1024)
                Dim fsin As New IO.BufferedStream(fsine, 512 * 1024)
                If fs IsNot Nothing Then fs.fs = fsin
                Using algo As Security.Cryptography.SHA1 = Security.Cryptography.SHA1.Create()
                    fsin.Position = 0
                    Dim hashValue() As Byte

                    hashValue = algo.ComputeHash(fsine)
                    'While fsin.Read(block, 0, block.Length) > 0
                    '
                    'End While
                    fsin.Close()
                    Dim result As New Text.StringBuilder()
                    For i As Integer = 0 To hashValue.Length - 1
                        result.Append(String.Format("{0:X2}", hashValue(i)))
                    Next
                    Return result.ToString()
                End Using
            End Using
        Else
            Dim thHash As New Threading.Thread(
                    Sub()
                        Using fsin0 As IO.FileStream = IO.File.Open(filename, IO.FileMode.Open, IO.FileAccess.Read)
                            Dim fsinb As New IO.BufferedStream(fsin0, 512 * 1024)
                            Dim fsine As New EventedStream With {.baseStream = fsinb}
                            AddHandler fsine.Readed, Sub(args As EventedStream.ReadStreamEventArgs) OnFileReading(args, fsine)
                            'Dim fsin As New IO.BufferedStream(fsine, 512 * 1024)
                            Dim fsin As New IO.BufferedStream(fsine, 512 * 1024)
                            If fs IsNot Nothing Then fs.fs = fsin
                            Using algo As Security.Cryptography.SHA1 = Security.Cryptography.SHA1.Create()
                                fsin.Position = 0
                                Dim hashValue() As Byte

                                hashValue = algo.ComputeHash(fsine)
                                'While fsin.Read(block, 0, block.Length) > 0
                                '
                                'End While
                                fsin.Close()
                                Dim result As New Text.StringBuilder()
                                For i As Integer = 0 To hashValue.Length - 1
                                    result.Append(String.Format("{0:X2}", hashValue(i)))
                                Next
                                OnFinished(result.ToString)
                            End Using
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
        Public Property BufferWrite As Integer = 4 * 1024 * 1024
        Public schema As ltfsindex
        Public IgnoreExisting As Boolean = True
        Public ReportSkip As Boolean = True
        Private _TargetDirectory As String
        Public LogFile As String() = {}
        Public Property TargetDirectory As String
            Set(value As String)
                _TargetDirectory = value.TrimEnd("\")
            End Set
            Get
                Return _TargetDirectory
            End Get
        End Property
        Private _BaseDirectory As String
        Private fs As fsReport
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
        Private fout As IO.FileStream = Nothing
        Private fob As IO.BufferedStream = Nothing
        Dim f_outpath As String
        Public Sub Start()
            SyncLock OperationLock
                If schema Is Nothing Then
                    RaiseEvent ErrorOccured("Error: No schema.")
                    Exit Sub
                End If
                If Status <> TaskStatus.Idle Then
                    Exit Sub
                End If
                fs = New fsReport()
                Dim thProg As New Threading.Thread(
                    Sub()
                        While Status <> TaskStatus.Idle
                            Try
                                If fs.fs IsNot Nothing Then
                                    If Not fs.fs.CanSeek Then Exit Try
                                    If fs.fs.Length = 0 Then Exit Try
                                    SyncLock OperationLock
                                        Dim p As Long = fs.fs.Position
                                        Dim l As Long = fs.fs.Length
                                        RaiseEvent ProgressReport("#fmax" & 10000)
                                        RaiseEvent ProgressReport("#fval" & p / l * 10000)
                                        RaiseEvent ProgressReport("#dmax" & l)
                                        RaiseEvent ProgressReport("#dval" & p)
                                    End SyncLock
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
                        Dim dnlist As New List(Of String)
                        For Each d As ltfsindex.directory In schema._directory
                            If Not d.Selected Then Continue For
                            dnlist.Add(d.name.Clone())
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
                        For i As Integer = 0 To schema._directory.Count - 1
                            schema._directory(i).name = dnlist(i)
                        Next
                        Dim totalSize As Long = 0
                        Dim hashedSize As Long = 0
                        For Each f As ltfsindex.file In flist
                            Threading.Interlocked.Add(totalSize, f.length)
                        Next
                        If totalSize = 0 Then totalSize = 1
                        RaiseEvent ProgressReport("#smax" & totalSize)
                        RaiseEvent ProgressReport("Sorting")
                        flist.Sort(New Comparison(Of ltfsindex.file)(Function(a As ltfsindex.file, b As ltfsindex.file) As Integer
                                                                         If a.extentinfo.Count = 0 Then Return a.extentinfo.Count.CompareTo(b.extentinfo.Count)
                                                                         If b.extentinfo.Count = 0 Then Return a.extentinfo.Count.CompareTo(b.extentinfo.Count)
                                                                         If a.extentinfo(0).partition <> b.extentinfo(0).partition Then
                                                                             Return a.extentinfo(0).partition.CompareTo(b.extentinfo(0).partition)
                                                                         End If
                                                                         If a.extentinfo(0).startblock <> b.extentinfo(0).startblock Then
                                                                             Return a.extentinfo(0).startblock.CompareTo(b.extentinfo(0).startblock)
                                                                         Else
                                                                             Return a.name.CompareTo(b.name)
                                                                         End If
                                                                     End Function))
                        RaiseEvent ProgressReport("#max" & 10000)
                        RaiseEvent ProgressReport("#tmax" & flist.Count)
                        Dim progval As Integer = 0
                        For Each f As ltfsindex.file In flist
                            Dim SkipCurrent As Boolean = False
                            Try
                                If TargetDirectory <> "" Then f_outpath = "\\?\" & My.Computer.FileSystem.CombinePath(TargetDirectory, f.fullpath)
                                f.fullpath = "\\?\" & My.Computer.FileSystem.CombinePath(BaseDirectory, f.fullpath)
                                If f.sha1 Is Nothing Then f.sha1 = ""
                                If f.sha1 = "" Or Not IgnoreExisting Or f.sha1.Length <> 40 Or (TargetDirectory <> "" And Not My.Computer.FileSystem.FileExists(f_outpath)) Then
                                    RaiseEvent ProgressReport("[hash] " & f.fullpath)
                                    Try
                                        Dim action_writefile As Action(Of EventedStream.ReadStreamEventArgs, EventedStream) = Sub(args As EventedStream.ReadStreamEventArgs, st As EventedStream)
                                                                                                                              End Sub

                                        If TargetDirectory <> "" Then
                                            If Not My.Computer.FileSystem.DirectoryExists(TargetDirectory) Then
                                                Try
                                                    My.Computer.FileSystem.CreateDirectory(TargetDirectory)
                                                Catch ex As Exception
                                                    RaiseEvent ErrorOccured(ex.ToString)
                                                End Try
                                            End If
                                            Try
                                                Dim outdir As String = My.Computer.FileSystem.GetFileInfo(f_outpath).DirectoryName
                                                If Not My.Computer.FileSystem.DirectoryExists(outdir) Then
                                                    My.Computer.FileSystem.CreateDirectory(outdir)
                                                End If
                                                If My.Computer.FileSystem.FileExists(f_outpath) Then
                                                    fout = Nothing
                                                    Exit Try
                                                End If
                                                fout = New IO.FileStream(f_outpath, IO.FileMode.CreateNew, IO.FileAccess.Write, IO.FileShare.Write, BufferWrite, FileOptions.WriteThrough)
                                                'fout = IO.File.OpenWrite(f_outpath)

                                                'fob = New IO.BufferedStream(fout, 1024 * 1024)
                                                action_writefile =
                                                    Sub(args As EventedStream.ReadStreamEventArgs, st As EventedStream)
                                                        fout.Write(args.Buffer, args.Offset, Math.Min(args.Count, st.Length - fout.Position))
                                                    End Sub
                                            Catch ex As Exception
                                                RaiseEvent ErrorOccured(ex.ToString)
                                            End Try
                                        End If
                                        f.sha1 = ""
                                        If LogFile.Count > 0 Then f.sha1 = SHA1(f.fullpath, LogFile)
                                        If f.sha1.Length <> 40 Then
                                            f.sha1 = SHA1(f.fullpath, Nothing, fs, action_writefile)
                                        Else
                                            Exit Try
                                        End If
                                        If fout IsNot Nothing Then
                                            Try
                                                'fob.Flush()
                                                'fob.Close()
                                                fout.Flush()
                                                fout.Close()
                                                My.Computer.FileSystem.GetFileInfo(f_outpath).CreationTimeUtc = My.Computer.FileSystem.GetFileInfo(f.fullpath).CreationTimeUtc
                                                My.Computer.FileSystem.GetFileInfo(f_outpath).Attributes = My.Computer.FileSystem.GetFileInfo(f.fullpath).Attributes
                                                My.Computer.FileSystem.GetFileInfo(f_outpath).LastWriteTimeUtc = My.Computer.FileSystem.GetFileInfo(f.fullpath).LastWriteTimeUtc
                                                fout.Dispose()
                                                fout = Nothing
                                            Catch ex As Exception
                                                RaiseEvent ErrorOccured(ex.ToString)
                                            End Try
                                        End If
                                        fs.fs.Dispose()
                                    Catch ex As Exception
                                        If fout IsNot Nothing Then
                                            Try
                                                'fob.Flush()
                                                'fob.Close()
                                                fout.Flush()
                                                fout.Close()
                                                fout.Dispose()
                                                fout = Nothing
                                            Catch ex2 As Exception
                                                RaiseEvent ErrorOccured(ex2.ToString)
                                            End Try
                                        End If
                                        If ex.Message = "正在中止线程。" Then Exit Try
                                        RaiseEvent ErrorOccured(ex.ToString)
                                    End Try
                                Else
                                    SkipCurrent = True
                                    If ReportSkip Then RaiseEvent ProgressReport("[skip] " & f.fullpath)
                                End If
                            Catch ex As Exception
                                If fout IsNot Nothing Then
                                    Try
                                        'fob.Flush()
                                        'fob.Close()
                                        fout.Flush()
                                        fout.Close()
                                        fout.Dispose()
                                        fout = Nothing
                                    Catch ex2 As Exception
                                        RaiseEvent ErrorOccured(ex2.ToString)
                                    End Try
                                End If
                                If ex.Message = "正在中止线程。" Then Exit Try
                                RaiseEvent ErrorOccured(ex.ToString)
                            End Try


                            Threading.Interlocked.Add(progval, 1)
                            SyncLock OperationLock
                                RaiseEvent ProgressReport("#dval" & 0)
                                Threading.Interlocked.Add(hashedSize, f.length)
                                RaiseEvent ProgressReport("#val" & hashedSize / totalSize * 10000)
                                RaiseEvent ProgressReport("#tval" & progval)
                                If ReportSkip OrElse (Not SkipCurrent) Then RaiseEvent ProgressReport("  " & f.sha1 & "  " & f.length & vbCrLf)
                                RaiseEvent ProgressReport("#ssum" & hashedSize)
                            End SyncLock

                            Threading.Thread.Sleep(0)
                        Next

                        RaiseEvent ProgressReport("#ssum" & hashedSize)
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
                    Try
                        fs.fs.Close()
                    Catch ex As Exception
                        RaiseEvent ErrorOccured(ex.ToString)
                    End Try
                    thHash.Abort()
                Catch ex As Exception
                    RaiseEvent ErrorOccured(ex.ToString)
                End Try
                Try
                    If fout IsNot Nothing Then
                        fout.Close()
                        If My.Computer.FileSystem.FileExists(f_outpath) Then
                            My.Computer.FileSystem.DeleteFile(f_outpath)
                        End If
                    End If
                Catch ex As Exception
                    RaiseEvent ErrorOccured(ex.ToString)
                End Try
                Try
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
    Public Class EventedStream
        Inherits Stream
        Implements IDisposable
        Class ReadStreamEventArgs
            Inherits EventArgs

            Public Buffer() As Byte

            Public Offset As Integer

            Public Count As Integer

            Public Denied As Boolean = False
        End Class
        Class WriteStreamEventArgs
            Inherits EventArgs

            Public Buffer() As Byte

            Public Offset As Integer

            Public Count As Integer

            Public Denied As Boolean = False
        End Class
        Public Class FlushStreamEventArgs
            Inherits EventArgs

            Public Denied As Boolean = False
        End Class

        Public Class SetStreamLengthEventArgs
            Inherits EventArgs

            Public Value As Long

            Public Denied As Boolean = False
        End Class

        Public Class SeekStreamEventArgs
            Inherits EventArgs

            Public Offset As Long

            Public SeekOrigin As SeekOrigin

            Public Denied As Boolean = False
        End Class

        Public baseStream As Stream

        Public Overrides ReadOnly Property CanRead As Boolean
            Get
                Return baseStream.CanRead
            End Get
        End Property

        Public Overrides ReadOnly Property CanSeek As Boolean
            Get
                Return baseStream.CanSeek
            End Get
        End Property

        Public Overrides ReadOnly Property CanWrite As Boolean
            Get
                Return baseStream.CanWrite
            End Get
        End Property

        Public Overrides ReadOnly Property Length As Long
            Get
                Return baseStream.Length
            End Get
        End Property

        Public Overrides Property Position As Long
            Get
                Return baseStream.Position
            End Get
            Set(value As Long)
                baseStream.Position = value
            End Set
        End Property

        Public Overrides Sub Flush()
            Dim args As New FlushStreamEventArgs()
            RaiseEvent PreviewFlush(args)
            If args.Denied Then
                Return
            End If
            baseStream.Flush()
        End Sub

        Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
            Dim args As New SeekStreamEventArgs() With {.Offset = offset, .SeekOrigin = origin}
            RaiseEvent PreviewSeek(args)
            If args.Denied Then
                Return 0
            End If
            Return baseStream.Seek(offset, origin)
        End Function


        Public Overrides Sub SetLength(value As Long)
            Dim args As New SetStreamLengthEventArgs With {.Value = value}
            RaiseEvent PreviewSetLength(args)
            If args.Denied Then
                Return
            End If
            baseStream.SetLength(value)
        End Sub


        Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
            Dim args As New ReadStreamEventArgs() With {.Buffer = buffer, .Offset = offset, .Count = count}
            RaiseEvent PreviewRead(args)
            If args.Denied Then
                Return 0
            End If
            Dim result As Integer = baseStream.Read(buffer, offset, count)
            RaiseEvent Readed(args)
            Return result
        End Function

        Public Overrides Sub Write(buffer() As Byte, offset As Integer, count As Integer)
            Dim args As New WriteStreamEventArgs With {.Buffer = buffer, .Offset = offset, .Count = count}
            RaiseEvent PreviewWrite(args)
            If args.Denied Then
                Return
            End If
            baseStream.Write(buffer, offset, count)
        End Sub
        Public Event PreviewFlush(args As FlushStreamEventArgs)
        Public Event PreviewSetLength(args As SetStreamLengthEventArgs)
        Public Event PreviewSeek(args As SeekStreamEventArgs)
        Public Event PreviewWrite(args As WriteStreamEventArgs)
        Public Event PreviewRead(args As ReadStreamEventArgs)
        Public Event Readed(args As ReadStreamEventArgs)


    End Class
    Public Class IndexedLHashDirectory
        Public LTFSIndexDir As ltfsindex.directory
        Public LHash_Dir As ltfsindex.directory
        Public Sub New(index As ltfsindex.directory, lhash As ltfsindex.directory)
            LTFSIndexDir = index
            LHash_Dir = lhash
        End Sub
    End Class

    Public Class SHA1BlockwiseCalculator
        Private sha1 As SHA1
        Private resultBytes As Byte()
        Private Lock As New Object
        Public StopFlag As Boolean = False
        Private thStarted As Boolean = False
        Structure QueueBlock
            Public block As Byte()
            Public Len As Integer
        End Structure
        Private q As New Queue(Of QueueBlock)
        Dim thHashAsync As New Task(
            Sub()
                While Not StopFlag
                    SyncLock Lock
                        While q.Count > 0
                            Dim blk As QueueBlock
                            SyncLock q
                                blk = q.Dequeue()
                            End SyncLock
                            With blk
                                If .Len = -1 Then .Len = .block.Length
                                sha1.TransformBlock(.block, 0, .Len, .block, 0)
                            End With
                            blk.block = Nothing
                        End While
                    End SyncLock
                    Threading.Thread.Sleep(1)
                End While

            End Sub)
        Public Sub New()
            sha1 = SHA1.Create()
        End Sub

        Public Sub Propagate(block As Byte(), Optional ByVal Len As Integer = -1)
            While q.Count > 0
                Threading.Thread.Sleep(1)
            End While
            SyncLock Lock
                If Len = -1 Then Len = block.Length
                sha1.TransformBlock(block, 0, Len, block, 0)
            End SyncLock
        End Sub
        Public Sub PropagateAsync(block As Byte(), Optional ByVal Len As Integer = -1)
            SyncLock Lock
                If Not thStarted Then
                    thHashAsync.Start()
                    thStarted = True
                End If
            End SyncLock
            If Len = -1 Then Len = block.Length
            While q.Count > 32
                Threading.Thread.Sleep(0)
            End While
            SyncLock Lock
                SyncLock q
                    q.Enqueue(New QueueBlock With {.block = block, .Len = Len})
                End SyncLock
            End SyncLock
        End Sub
        Public Sub ProcessFinalBlock()
            While q.Count > 0
                Threading.Thread.Sleep(1)
            End While
            SyncLock Lock
                sha1.TransformFinalBlock({}, 0, 0)
                resultBytes = sha1.Hash
            End SyncLock
            StopFlag = True
        End Sub
        Public ReadOnly Property SHA1Value As String
            Get
                SyncLock Lock
                    Return BitConverter.ToString(resultBytes).Replace("-", "").ToUpper()
                End SyncLock
            End Get
        End Property
    End Class

End Class
Public Class ExplorerUtils
    Implements IComparer(Of String)
    Declare Unicode Function StrCmpLogicalW Lib "shlwapi.dll" (ByVal s1 As String, ByVal s2 As String) As Int32
    Public Function Compare(ByVal x As String, ByVal y As String) As Integer Implements System.Collections.Generic.IComparer(Of String).Compare
        Return StrCmpLogicalW(x, y)
    End Function
End Class