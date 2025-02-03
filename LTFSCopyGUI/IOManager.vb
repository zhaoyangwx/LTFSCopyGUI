Imports System.ComponentModel
Imports System.IO
Imports System.Security.Cryptography
Imports System.Threading
Imports Blake3
Imports LTFSCopyGUI

<TypeConverter(GetType(ExpandableObjectConverter))>
Public Class IOManager
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class fsReport
        Public fs As IO.BufferedStream
        Public Sub New()

        End Sub
        Public Sub New(fst As IO.BufferedStream)
            fs = fst
        End Sub
    End Class
    Public Event ErrorOccured(s As String)
    Public Shared Function FormatSize(l As Long, Optional ByVal More As Boolean = False) As String
        If l < 1024 Then
            Return l & " Bytes"
        ElseIf l < 1024 ^ 2 Then
            Return (l / 1024).ToString("F2") & " KiB"
        ElseIf l < 1024 ^ 3 Then
            Return (l / 1024 ^ 2).ToString("F2") & " MiB"
        ElseIf Not More OrElse l < 1024 ^ 4 Then
            Return (l / 1024 ^ 3).ToString("F2") & " GiB"
        ElseIf l < 1024 ^ 5 Then
            Return (l / 1024 ^ 4).ToString("F2") & " TiB"
        Else
            Return (l / 1024 ^ 5).ToString("F2") & " PiB"
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
                If OnFileReading IsNot Nothing Then AddHandler fsine.Readed, Sub(args As EventedStream.ReadStreamEventArgs) OnFileReading(args, fsine)
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

    Public Shared Function FitImage(input As Bitmap, outputsize As Size) As Bitmap
        Dim result As New Bitmap(outputsize.Width, outputsize.Height, Imaging.PixelFormat.Format24bppRgb)
        If input Is Nothing Then Return result
        Dim g As Graphics = Graphics.FromImage(result)
        Dim outputw As Integer = input.Width
        Dim outputh As Integer = input.Height
        If outputw > outputsize.Width Then
            outputw = outputsize.Width
            outputh = input.Height / input.Width * outputsize.Width
        End If
        If outputh > outputsize.Height Then
            outputh = outputsize.Height
            outputw = input.Width / input.Height * outputsize.Height
        End If
        g.FillRectangle(Brushes.White, New Rectangle(0, 0, result.Width, result.Height))
        g.DrawImage(input, outputsize.Width \ 2 - outputw \ 2, outputsize.Height \ 2 - outputh \ 2, outputw, outputh)
        g.Dispose()
        Return result
    End Function

    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class HashTask
        Public Event TaskStarted(Message As String)
        Public Event TaskCancelled(Message As String)
        Public Event TaskPaused(Message As String)
        Public Event TaskResumed(Message As String)
        Public Event TaskFinished(Message As String)
        Public Event ErrorOccured(Message As String)
        Public Event ProgressReport(Message As String)
        Public Property BufferWrite As Integer = 4 * 1024 * 1024
        Public Property schema As ltfsindex
        Public Property IgnoreExisting As Boolean = True
        Public Property ReportSkip As Boolean = True
        Private _TargetDirectory As String
        Public Property LogFile As String() = {}
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
                                If TargetDirectory <> "" Then f_outpath = "\\?\" & IO.Path.Combine(TargetDirectory, f.fullpath)
                                f.fullpath = "\\?\" & IO.Path.Combine(BaseDirectory, f.fullpath)
                                If f.sha1 Is Nothing Then f.sha1 = ""
                                If f.sha1 = "" Or Not IgnoreExisting Or f.sha1.Length <> 40 Or (TargetDirectory <> "" And Not IO.File.Exists(f_outpath)) Then
                                    RaiseEvent ProgressReport("[hash] " & f.fullpath)
                                    Try
                                        Dim action_writefile As Action(Of EventedStream.ReadStreamEventArgs, EventedStream) = Sub(args As EventedStream.ReadStreamEventArgs, st As EventedStream)
                                                                                                                              End Sub

                                        If TargetDirectory <> "" Then
                                            If Not IO.Directory.Exists(TargetDirectory) Then
                                                Try
                                                    IO.Directory.CreateDirectory(TargetDirectory)
                                                Catch ex As Exception
                                                    RaiseEvent ErrorOccured(ex.ToString)
                                                End Try
                                            End If
                                            Try
                                                Dim outdir As String = New IO.FileInfo(f_outpath).DirectoryName
                                                If Not IO.Directory.Exists(outdir) Then
                                                    IO.Directory.CreateDirectory(outdir)
                                                End If
                                                If IO.File.Exists(f_outpath) Then
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
                                                Dim foinfo As New IO.FileInfo(f_outpath)
                                                Dim fiinfo As New IO.FileInfo(f.fullpath)
                                                foinfo.CreationTimeUtc = fiinfo.CreationTimeUtc
                                                foinfo.Attributes = fiinfo.Attributes
                                                foinfo.LastWriteTimeUtc = fiinfo.LastWriteTimeUtc
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
                        If IO.File.Exists(f_outpath) Then
                            IO.File.Delete(f_outpath)
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
    <TypeConverter(GetType(ExpandableObjectConverter))>
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
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class IndexedLHashDirectory
        Public Property LTFSIndexDir As ltfsindex.directory
        Public Property LHash_Dir As ltfsindex.directory
        Public Sub New(index As ltfsindex.directory, lhash As ltfsindex.directory)
            LTFSIndexDir = index
            LHash_Dir = lhash
        End Sub
    End Class

    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class CheckSumBlockwiseCalculator
        Private Property sha1 As SHA1
        Private Property md5 As MD5
        Private Property resultBytesSHA1 As Byte()
        Private Property resultBytesMD5 As Byte()
        Private Property resultBytesBlake As Hash
        Private Property Lock As New Object
        Public Property StopFlag As Boolean = False
        Private Property thStarted As Boolean = False
        Private Property BlakeStream As New MemoryStream
        Private Property WrittenBlakeBlock1 As Int32 = 0
        Private Property WrittenBlakeBlock2 As Int32 = 0
        Structure QueueBlock
            Public block As Byte()
            Public Len As Integer
        End Structure
        Public Property q As New Queue(Of QueueBlock)
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
                                Dim md5task As Task = Task.Run(Sub()
                                                                   md5.TransformBlock(.block, 0, .Len, .block, 0)
                                                               End Sub)
                                Dim sha1task As Task = Task.Run(Sub()
                                                                    sha1.TransformBlock(.block, 0, .Len, .block, 0)
                                                                End Sub)
                                Dim blaketask As Task = Task.Run(Sub()
                                                                     BlakeStream.Write(.block, WrittenBlakeBlock1, .Len)
                                                                     WrittenBlakeBlock1 += .Len
                                                                 End Sub)
                                sha1task.Wait()
                                md5task.Wait()
                                blaketask.Wait()
                            End With
                            blk.block = Nothing
                        End While
                    End SyncLock
                    Threading.Thread.Sleep(1)
                End While

            End Sub)
        Public Sub New()
            sha1 = SHA1.Create()
            md5 = MD5.Create()
        End Sub

        Public Sub Propagate(block As Byte(), Optional ByVal Len As Integer = -1)
            While q.Count > 0
                Threading.Thread.Sleep(1)
            End While
            SyncLock Lock
                If Len = -1 Then Len = block.Length
                Dim sha1task As Task = Task.Run(Sub()
                                                    sha1.TransformBlock(block, 0, Len, block, 0)
                                                End Sub)
                Dim md5task As Task = Task.Run(Sub()
                                                   md5.TransformBlock(block, 0, Len, block, 0)
                                               End Sub)
                Dim blaketask As Task = Task.Run(Sub()
                                                     BlakeStream.Write(block, WrittenBlakeBlock2, Len)
                                                     WrittenBlakeBlock2 += Len
                                                 End Sub)
                blaketask.Wait()
                sha1task.Wait()
                md5task.Wait()
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
            While q.Count > 1024
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
                md5.TransformFinalBlock({}, 0, 0)
                resultBytesSHA1 = sha1.Hash
                resultBytesMD5 = md5.Hash
                Dim blake3Stream = New Blake3Stream(BlakeStream)
                resultBytesBlake = blake3Stream.ComputeHash()
                blake3Stream.Dispose()
            End SyncLock
            StopFlag = True
        End Sub
        Public ReadOnly Property SHA1Value As String
            Get
                SyncLock Lock
                    Return BitConverter.ToString(resultBytesSHA1).Replace("-", "").ToUpper()
                End SyncLock
            End Get
        End Property
        Public ReadOnly Property MD5Value As String
            Get
                SyncLock Lock
                    Return BitConverter.ToString(resultBytesMD5).Replace("-", "").ToUpper()
                End SyncLock
            End Get
        End Property
        Public ReadOnly Property BlakeValue As String
            Get
                SyncLock Lock
                    Return resultBytesBlake.ToString()
                End SyncLock
            End Get
        End Property
    End Class

    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class LTFSFileStream
        Inherits Stream
        Public Event LogPrint(s As String)
        Public ReadOnly Property FileInfo As ltfsindex.file
        Public Property TapeDrive As String
        Public Property BlockSize As Integer
        Public Property ExtraPartitionCount As Integer
        Public Shared OperationLock As New Object
        Public Sub New(file As ltfsindex.file, drive As String, blksize As Integer, xtrPCount As Integer)
            FileInfo = file
            TapeDrive = drive
            BlockSize = blksize
            ExtraPartitionCount = xtrPCount
            FileInfo.extentinfo.Sort(New Comparison(Of ltfsindex.file.extent)(
                                     Function(a As ltfsindex.file.extent, b As ltfsindex.file.extent)
                                         Return a.fileoffset.CompareTo(b.fileoffset)
                                     End Function))
        End Sub
        Public Overrides ReadOnly Property CanRead As Boolean
            Get
                Return TapeDrive <> ""
            End Get
        End Property

        Public Overrides ReadOnly Property CanSeek As Boolean
            Get
                Return CanRead
            End Get
        End Property

        Public Overrides ReadOnly Property CanWrite As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Length As Long
            Get
                Return FileInfo.length
            End Get
        End Property
        Private _Position As Long
        Public Function GetExtent(offset As Long)
            SyncLock OperationLock
                For i As Integer = 0 To FileInfo.extentinfo.Count - 1
                    With FileInfo.extentinfo(i)
                        If .fileoffset <= offset AndAlso .fileoffset + .bytecount > offset Then
                            Return FileInfo.extentinfo(i)
                        End If
                    End With
                Next
            End SyncLock

            Return Nothing
        End Function
        Public Function WithinExtent(offset As Long, partition As Integer, ext As ltfsindex.file.extent) As Boolean
            If ext Is Nothing Then Return False
            With ext
                Return .fileoffset <= offset AndAlso .fileoffset + .bytecount > offset AndAlso partition = Math.Min(ExtraPartitionCount, ext.partition)
            End With
        End Function
        Public Overrides Property Position As Long
            Get
                Return _Position
            End Get
            Set(value As Long)
                SyncLock OperationLock
                    If value < 0 Then value = 0
                    If value >= FileInfo.length Then value = FileInfo.length - 1
                    Dim ext As ltfsindex.file.extent = GetExtent(value)
                    Dim p As New TapeUtils.PositionData(TapeDrive)
                    Dim targetBlock As ULong = ext.startblock + (value - ext.fileoffset) \ BlockSize
                    Dim targetPartition As Byte = Math.Min(ExtraPartitionCount, ext.partition)
                    If p.BlockNumber <> targetBlock OrElse p.PartitionNumber <> targetPartition Then
                        RaiseEvent LogPrint($"LOCATE {TapeDrive} B{targetBlock}P{targetPartition} (File position {value})")
                        TapeUtils.Locate(TapeDrive, targetBlock, targetPartition)
                    End If
                    _Position = value
                End SyncLock
            End Set
        End Property

        Public Overrides Sub Flush()

        End Sub

        Public Overrides Sub SetLength(value As Long)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Sub Write(buffer() As Byte, offset As Integer, count As Integer)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
            SyncLock ReadLock
                RaiseEvent LogPrint($"Seek Offset {offset} Origin {origin}")
                Select Case origin
                    Case SeekOrigin.Begin
                        Position = offset
                    Case SeekOrigin.Current
                        Position = Position + offset
                    Case SeekOrigin.End
                        Position = Length + offset
                End Select
                Return Position
            End SyncLock
        End Function
        Public Shared ReadLock As New Object
        Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
            SyncLock ReadLock
                RaiseEvent LogPrint($"ReadFile: Offset {offset} Count{count}")
                Dim rBytes As Integer = 0
                Dim fCurrentPos As Long = Position
                Dim CUrrentP As Integer = New TapeUtils.PositionData(TapeDrive).PartitionNumber
                Dim ext As ltfsindex.file.extent = Nothing
                While rBytes < count
                    If Not WithinExtent(fCurrentPos, CUrrentP, ext) Then
                        ext = GetExtent(fCurrentPos)
                        Position = fCurrentPos
                        CUrrentP = New TapeUtils.PositionData(TapeDrive).PartitionNumber
                    End If
                    If ext Is Nothing Then Exit While
                    Dim fStartBlock As Long = ext.startblock + (fCurrentPos - ext.fileoffset + ext.byteoffset) \ BlockSize
                    Dim fByteOffset As Integer = (ext.byteoffset + fCurrentPos - ext.fileoffset) Mod BlockSize
                    Dim BytesRemaining As Long = ext.bytecount - (fCurrentPos - ext.fileoffset)
                    Dim data As Byte() = TapeUtils.ReadBlock(TapeDrive:=TapeDrive, BlockSizeLimit:=Math.Min(BlockSize, BytesRemaining))
                    Dim bytesReaded As Integer = data.Length - fByteOffset
                    Dim destIndex As Integer = offset + rBytes
                    Array.Copy(data, fByteOffset, buffer, destIndex, Math.Min(bytesReaded, buffer.Length - destIndex))
                    rBytes += bytesReaded
                    fCurrentPos += bytesReaded
                End While
                _Position = fCurrentPos
                Return rBytes
            End SyncLock
        End Function
    End Class
    <TypeConverter(GetType(ExpandableObjectConverter))>
    Public Class NetworkCommand
        Public Property HashCode As Integer
        Public Property CommandType As CommandTypeDef
        Enum CommandTypeDef
            SCSICommand
            SCSISenseData
            General
            GeneralData
            SCSIIOCtlError
        End Enum
        Public Property PayLoad As New List(Of Byte())
        Public Function GetSerializedText() As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(NetworkCommand))
            Dim sb As New Text.StringBuilder
            Dim t As New IO.StringWriter(sb)
            writer.Serialize(t, Me)
            Return sb.ToString()
        End Function
        Public Shared Function FromXML(s As String) As NetworkCommand
            Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(NetworkCommand))
            Dim t As IO.TextReader = New IO.StringReader(s)
            Return CType(reader.Deserialize(t), NetworkCommand)
        End Function
        Public Function SendTo(target As Net.IPAddress, port As Integer) As NetworkCommand
            Dim rawdata() As Byte = Text.Encoding.UTF8.GetBytes(Me.GetSerializedText())
            Dim senddata As New List(Of Byte)
            senddata.AddRange(BitConverter.GetBytes(rawdata.Length))
            senddata.AddRange(rawdata)

            Dim sck As New Net.Sockets.Socket(Net.Sockets.AddressFamily.InterNetwork, Net.Sockets.SocketType.Stream, Net.Sockets.ProtocolType.Tcp)
            sck.Connect(New Net.IPEndPoint(target, port))
            sck.SendTo(senddata.ToArray(), New Net.IPEndPoint(target, port))
            Dim header(3) As Byte
            Dim TaskFinished As Boolean = False
            Dim result As NetworkCommand = Nothing
            Task.Run(Sub()
                         Dim hLength As Integer = sck.Receive(header, 4, Net.Sockets.SocketFlags.None)
                         Dim length As Integer = BitConverter.ToInt32(header, 0)
                         Dim data(length - 1) As Byte
                         Dim dLength As Integer = sck.Receive(data, length, Net.Sockets.SocketFlags.None)
                         Dim msg As String = Text.Encoding.UTF8.GetString(data)
                         sck.Close()
                         result = NetworkCommand.FromXML(msg)
                         TaskFinished = True
                     End Sub).Wait(10000)
            Return result
        End Function
    End Class
End Class
Public Class ExplorerUtils
    Implements IComparer(Of String)
    Declare Unicode Function StrCmpLogicalW Lib "shlwapi.dll" (ByVal s1 As String, ByVal s2 As String) As Int32
    Public Function Compare(ByVal x As String, ByVal y As String) As Integer Implements System.Collections.Generic.IComparer(Of String).Compare
        Return StrCmpLogicalW(x, y)
    End Function
End Class