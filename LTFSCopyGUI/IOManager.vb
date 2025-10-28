Imports System.ComponentModel
Imports System.IO
Imports System.Security.Cryptography
Imports System.Threading
Imports Blake3
Imports System.IO.Hashing
Imports LTFSCopyGUI
Imports System.Runtime.InteropServices
Imports NAudio.Wave
Imports System.Text
Imports System.Net.Sockets
Imports System.Globalization
Imports LTFSCopyGUI.IOManager
Imports System.Xml.Serialization
Imports SetupAPIHelper
Imports System.Buffers

<TypeConverter(GetType(ExpandableObjectConverter))>
Public Class IOManager
    Public Shared PublicArrayPool As ArrayPool(Of Byte) = ArrayPool(Of Byte).Create(16777216, 2048)
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

    Public Shared Function SHA1(filename As String, Optional ByVal OnFinished As Action(Of String) = Nothing,
                                Optional ByVal fs As fsReport = Nothing,
                                Optional ByVal OnFileReading As _
                                   Action(Of EventedStream.ReadStreamEventArgs, EventedStream) = Nothing) As String
        If OnFinished Is Nothing Then

            Using _
                fsin0 As IO.FileStream = IO.File.Open(filename, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                Dim fsinb As New IO.BufferedStream(fsin0, 512 * 1024)
                Dim fsine As New EventedStream With {.baseStream = fsinb}
                If OnFileReading IsNot Nothing Then _
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
                    Dim result As New System.Text.StringBuilder()
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
                        AddHandler fsine.Readed,
                                                  Sub(args As EventedStream.ReadStreamEventArgs) _
                                                  OnFileReading(args, fsine)
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
                            Dim result As New System.Text.StringBuilder()
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
        Public Event SHA1Changed(f As ltfsindex.file, Message As String)
        Public Event ProgressReport(Message As String)
        Public Property BufferWrite As Integer = 4 * 1024 * 1024
        Public Property schema As ltfsindex
        Public Property IgnoreExisting As Boolean = True
        Public Property ReportSkip As Boolean = True
        Private _TargetDirectory As String
        Public Property LogFile As String() = {}

        Public Property TargetDirectory As String
            Set(value As String)
                _TargetDirectory = value.TrimEnd("\") & "\"
            End Set
            Get
                Return _TargetDirectory
            End Get
        End Property

        Private _BaseDirectory As String
        Private fs As fsReport

        Public Property BaseDirectory As String
            Set(value As String)
                _BaseDirectory = value.TrimEnd("\") & "\"
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
                        flist.Sort(
                            New Comparison(Of ltfsindex.file)(
                                Function(a As ltfsindex.file, b As ltfsindex.file) As Integer
                                    If a.extentinfo.Count = 0 Then _
                                                                 Return a.extentinfo.Count.CompareTo(b.extentinfo.Count)
                                    If b.extentinfo.Count = 0 Then _
                                                                 Return a.extentinfo.Count.CompareTo(b.extentinfo.Count)
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
                                If TargetDirectory <> "" Then _
                                                 f_outpath = "\\?\" & IO.Path.Combine(TargetDirectory, f.fullpath)
                                f.fullpath = "\\?\" & IO.Path.Combine(BaseDirectory, f.fullpath)

                                If f.sha1 Is Nothing Then f.sha1 = ""
                                If f.sha1 = "" Or Not IgnoreExisting Or f.sha1.Length <> 40 Or
                                                 (TargetDirectory <> "" And Not IO.File.Exists(f_outpath)) Then
                                    RaiseEvent ProgressReport("[hash] " & f.fullpath)
                                    Try
                                        Dim action_writefile _
                                                As Action(Of EventedStream.ReadStreamEventArgs, EventedStream) =
                                                Sub(args As EventedStream.ReadStreamEventArgs, st As EventedStream)
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
                                                fout = New IO.FileStream(f_outpath, IO.FileMode.CreateNew,
                                                                         IO.FileAccess.Write, IO.FileShare.Write,
                                                                         BufferWrite, FileOptions.WriteThrough)
                                                'fout = IO.File.OpenWrite(f_outpath)

                                                'fob = New IO.BufferedStream(fout, 1024 * 1024)
                                                action_writefile =
                                                 Sub(args As EventedStream.ReadStreamEventArgs, st As EventedStream)
                                                     fout.Write(args.Buffer, args.Offset,
                                                                Math.Min(args.Count, st.Length - fout.Position))
                                                 End Sub
                                            Catch ex As Exception
                                                RaiseEvent ErrorOccured(ex.ToString)
                                            End Try
                                        End If
                                        Dim oldvalue As String = f.sha1
                                        f.sha1 = ""
                                        If IO.File.Exists(f.fullpath) Then
                                            If LogFile.Count > 0 Then f.sha1 = SHA1(f.fullpath, LogFile)
                                            If oldvalue.Length = 40 AndAlso f.sha1.Length = 40 AndAlso oldvalue.ToUpper <> f.sha1.ToUpper Then
                                                RaiseEvent SHA1Changed(f, $"{oldvalue} -> {f.sha1}")
                                            End If
                                            If (Not IgnoreExisting) OrElse f.sha1.Length <> 40 Then
                                                f.sha1 = SHA1(f.fullpath, Nothing, fs, action_writefile)
                                                If oldvalue.Length = 40 AndAlso f.sha1.Length = 40 AndAlso oldvalue.ToUpper <> f.sha1.ToUpper Then
                                                    RaiseEvent SHA1Changed(f, $"{oldvalue} -> {f.sha1}")
                                                End If
                                            Else
                                                Exit Try
                                            End If
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
                                If ReportSkip OrElse (Not SkipCurrent) Then _
                                                 RaiseEvent ProgressReport("  " & f.sha1 & "  " & f.length & vbCrLf)
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
        Private Property Blake As Hasher
        Private Property XxHash3 As XxHash3
        Private Property XxHash128 As XxHash128
        Private Property resultBytesSHA1 As Byte()
        Private Property resultBytesMD5 As Byte()
        Private Property resultBytesBlake As Hash
        Private Property resultXxHash3 As Byte()
        Private Property resultXxHash128 As Byte()
        Private Property Lock As New Object
        Public Property StopFlag As Boolean = False
        Private Property thStarted As Boolean = False
        'Private Property BlakeStream As New MemoryStream
        'Private Property WrittenBlakeBlock1 As Int32 = 0
        'Private Property WrittenBlakeBlock2 As Int32 = 0
        Structure QueueBlock
            Public block As Byte()
            Public Len As Integer
            Public OnFinished As Action(Of Byte())
        End Structure
        Public EnQSig As New AutoResetEvent(False)
        Public DeQSig As New AutoResetEvent(False)
        Public Property q As New Queue(Of QueueBlock)

        Dim thHashAsync As New Task(
            Sub()
                While Not StopFlag
                    SyncLock Lock
                        While q.Count > 0
                            Dim blk As QueueBlock
                            SyncLock q
                                blk = q.Dequeue()
                                DeQSig.Set()
                            End SyncLock
                            With blk
                                If .Len = -1 Then .Len = .block.Length
                                Dim md5task As Task
                                If My.Settings.LTFSWriter_ChecksumEnabled_MD5 Then md5task = Task.Run(
                                Sub()
                                    md5.TransformBlock(.block, 0, .Len, .block, 0)
                                End Sub)
                                Dim sha1task As Task
                                If My.Settings.LTFSWriter_ChecksumEnabled_SHA1 Then sha1task = Task.Run(
                                Sub()
                                    sha1.TransformBlock(.block, 0, .Len, .block, 0)
                                End Sub)
                                Dim blaketask As Task
                                If My.Settings.LTFSWriter_ChecksumEnabled_BLAKE3 Then blaketask = Task.Run(
                                Sub()
                                    'BlakeStream.Write(.block, WrittenBlakeBlock1, .Len)
                                    'WrittenBlakeBlock1 += .Len
                                    Dim segment As New ArraySegment(Of Byte)(.block, 0, .Len)
                                    Try
                                        Blake.UpdateWithJoin(segment)
                                    Catch ex As Exception

                                    End Try
                                End Sub)
                                Dim xxhash3task As Task
                                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash3 Then xxhash3task = Task.Run(
                                Sub()
                                    Dim segment As New ArraySegment(Of Byte)(.block, 0, .Len)
                                    Try
                                        XxHash3.Append(segment)
                                    Catch ex As Exception

                                    End Try
                                End Sub)
                                Dim xxhash128task As Task
                                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash128 Then xxhash128task = Task.Run(
                                Sub()
                                    Dim segment As New ArraySegment(Of Byte)(.block, 0, .Len)
                                    Try
                                        XxHash128.Append(segment)
                                    Catch ex As Exception

                                    End Try
                                End Sub)
                                If My.Settings.LTFSWriter_ChecksumEnabled_SHA1 Then sha1task.Wait()
                                If My.Settings.LTFSWriter_ChecksumEnabled_MD5 Then md5task.Wait()
                                If My.Settings.LTFSWriter_ChecksumEnabled_BLAKE3 Then blaketask.Wait()
                                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash3 Then xxhash3task.Wait()
                                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash128 Then xxhash128task.Wait()
                            End With
                            If blk.OnFinished IsNot Nothing Then blk.OnFinished(blk.block)
                            blk.block = Nothing
                        End While
                    End SyncLock
                    EnQSig.WaitOne(10)
                End While
            End Sub)

        Public Sub New()
            Try
                If My.Settings.LTFSWriter_ChecksumEnabled_BLAKE3 Then Blake = Hasher.NewInstance()
            Catch ex As Exception

            End Try
            If My.Settings.LTFSWriter_ChecksumEnabled_SHA1 Then sha1 = SHA1.Create()
            If My.Settings.LTFSWriter_ChecksumEnabled_MD5 Then md5 = MD5.Create()
            Try
                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash3 Then XxHash3 = New IO.Hashing.XxHash3()
            Catch ex As Exception
            End Try
            Try
                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash128 Then XxHash128 = New IO.Hashing.XxHash128()
            Catch ex As Exception
            End Try
        End Sub

        Public Sub Propagate(block As Byte(), Optional ByVal Len As Integer = -1, Optional ByVal OnFinished As Action(Of Byte()) = Nothing)
            While q.Count > 0
                DeQSig.WaitOne(10)
            End While
            SyncLock Lock
                If Len = -1 Then Len = block.Length
                Dim sha1task As Task
                If My.Settings.LTFSWriter_ChecksumEnabled_SHA1 Then sha1task = Task.Run(
                    Sub()
                        sha1.TransformBlock(block, 0, Len, block, 0)
                    End Sub)
                Dim md5task As Task
                If My.Settings.LTFSWriter_ChecksumEnabled_MD5 Then md5task = Task.Run(
                    Sub()
                        md5.TransformBlock(block, 0, Len, block, 0)
                    End Sub)
                Dim blaketask As Task
                If My.Settings.LTFSWriter_ChecksumEnabled_BLAKE3 Then blaketask = Task.Run(
                    Sub()
                        'BlakeStream.Write(block, WrittenBlakeBlock2, Len)
                        'WrittenBlakeBlock2 += Len
                        Dim segment As New ArraySegment(Of Byte)(block, 0, Len)
                        Try
                            Blake.UpdateWithJoin(segment)
                        Catch ex As Exception

                        End Try
                    End Sub)
                Dim xxhash3task As Task
                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash3 Then xxhash3task = Task.Run(
                    Sub()
                        Dim segment As New ArraySegment(Of Byte)(block, 0, Len)
                        Try
                            XxHash3.Append(segment)
                        Catch ex As Exception

                        End Try
                    End Sub)
                Dim xxhash128task As Task
                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash128 Then xxhash128task = Task.Run(
                    Sub()
                        Dim segment As New ArraySegment(Of Byte)(block, 0, Len)
                        Try
                            XxHash128.Append(segment)
                        Catch ex As Exception

                        End Try
                    End Sub)
                If My.Settings.LTFSWriter_ChecksumEnabled_BLAKE3 Then blaketask.Wait()
                If My.Settings.LTFSWriter_ChecksumEnabled_SHA1 Then sha1task.Wait()
                If My.Settings.LTFSWriter_ChecksumEnabled_MD5 Then md5task.Wait()
                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash3 Then xxhash3task.Wait()
                If My.Settings.LTFSWriter_ChecksumEnabled_XxHash128 Then xxhash128task.Wait()
            End SyncLock
            If OnFinished IsNot Nothing Then OnFinished(block)
        End Sub

        Public Sub PropagateAsync(block As Byte(), Optional ByVal Len As Integer = -1, Optional ByVal OnFinished As Action(Of Byte()) = Nothing)
            SyncLock Lock
                If Not thStarted Then
                    thHashAsync.Start()
                    thStarted = True
                End If
            End SyncLock
            If Len = -1 Then Len = block.Length
            While q.Count > 1024
                DeQSig.WaitOne(10)
            End While
            SyncLock Lock
                SyncLock q
                    q.Enqueue(New QueueBlock With {.block = block, .Len = Len, .OnFinished = OnFinished})
                    EnQSig.Set()
                End SyncLock
            End SyncLock
        End Sub

        Public Sub ProcessFinalBlock()
            While q.Count > 0
                DeQSig.WaitOne(10)
            End While
            SyncLock Lock
                If My.Settings.LTFSWriter_ChecksumEnabled_SHA1 Then
                    sha1.TransformFinalBlock({}, 0, 0)
                    resultBytesSHA1 = sha1.Hash
                End If
                If My.Settings.LTFSWriter_ChecksumEnabled_MD5 Then
                    md5.TransformFinalBlock({}, 0, 0)
                    resultBytesMD5 = md5.Hash
                End If
                Try
                    If My.Settings.LTFSWriter_ChecksumEnabled_BLAKE3 Then resultBytesBlake = Blake.Finalize()
                Catch ex As Exception
                    resultBytesBlake = Nothing
                End Try
                Try
                    If My.Settings.LTFSWriter_ChecksumEnabled_XxHash3 Then
                        resultXxHash3 = XxHash3.GetHashAndReset()
                    End If
                Catch ex As Exception
                    resultXxHash3 = Nothing
                End Try
                Try
                    If My.Settings.LTFSWriter_ChecksumEnabled_XxHash128 Then
                        resultXxHash128 = XxHash128.GetHashAndReset()
                    End If
                Catch ex As Exception
                    resultXxHash128 = Nothing
                End Try
            End SyncLock
            StopFlag = True
        End Sub

        Public ReadOnly Property SHA1Value As String
            Get
                SyncLock Lock
                    Try
                        Return BitConverter.ToString(resultBytesSHA1).Replace("-", "").ToUpper()
                    Catch ex As Exception
                        Return Nothing
                    End Try
                End SyncLock
            End Get
        End Property

        Public ReadOnly Property MD5Value As String
            Get
                SyncLock Lock
                    Try
                        Return BitConverter.ToString(resultBytesMD5).Replace("-", "").ToUpper()
                    Catch ex As Exception
                        Return Nothing
                    End Try
                End SyncLock
            End Get
        End Property

        Public ReadOnly Property BlakeValue As String
            Get
                SyncLock Lock
                    Try
                        Return resultBytesBlake.ToString().ToUpper()
                    Catch ex As Exception
                        Return Nothing
                    End Try
                End SyncLock
            End Get
        End Property
        Public ReadOnly Property XXHash3Value As String
            Get
                SyncLock Lock
                    If resultXxHash3 Is Nothing Then Return Nothing
                    Try
                        Return BitConverter.ToString(resultXxHash3).Replace("-", "").ToUpper()
                    Catch ex As Exception
                        Return Nothing
                    End Try
                End SyncLock
            End Get
        End Property
        Public ReadOnly Property XXHash128Value As String
            Get
                SyncLock Lock
                    If resultXxHash128 Is Nothing Then Return Nothing
                    Try
                        Return BitConverter.ToString(resultXxHash128).Replace("-", "").ToUpper()
                    Catch ex As Exception
                        Return Nothing
                    End Try
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
            FileInfo.extentinfo.Sort(
                New Comparison(Of ltfsindex.file.extent)(
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
                Return _
                    .fileoffset <= offset AndAlso .fileoffset + .bytecount > offset AndAlso
                    partition = Math.Min(ExtraPartitionCount, ext.partition)
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
                        RaiseEvent _
                            LogPrint($"LOCATE {TapeDrive} B{targetBlock}P{targetPartition} (File position {value})")
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
                    Dim data As Byte() = TapeUtils.ReadBlock(TapeDrive:=TapeDrive,
                                                             BlockSizeLimit:=Math.Min(BlockSize, BytesRemaining))
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
    ''' <summary>
    ''' 智能流：用于线性存储介质的缓存流，具备预读、定位优化、缓存淘汰与最小阻塞控制等功能。
    ''' </summary>
    Public Class SmartStream
        Inherits Stream

        ''' <summary>
        ''' 表示一个缓冲块，包含起始位置、数据、最后访问时间与访问计数。
        ''' </summary>
        Private Class BufferBlock
            Public StartPos As Long
            Public Data() As Byte
            Public LastAccess As DateTime = DateTime.Now
            Public AccessCount As Integer = 0

            Public ReadOnly Property EndPos As Long
                Get
                    Return StartPos + Data.Length - 1
                End Get
            End Property

            Public Function Covers(pos As Long) As Boolean
                Return pos >= StartPos AndAlso pos <= EndPos
            End Function

            Public Function GetOffset(pos As Long) As Integer
                Return CInt(pos - StartPos)
            End Function
        End Class

        Private ReadOnly baseStream As Stream
        Private ReadOnly fixedReadSize As Integer
        Private ReadOnly minReadThreshold As Integer
        Private ReadOnly maxSeekReadSize As Integer
        Private ReadOnly bufferSize As Integer

        Private bufferBlocks As New List(Of BufferBlock)
        Private PositionValue As Long = 0
        Private ReadLock As New SemaphoreSlim(1, 1)
        Private bufferLock As New Object()

        ''' <summary>
        ''' 调试日志事件。使用 RaiseEvent 而不是直接打印。
        ''' </summary>
        Public Event DebugLog(message As String)

        ''' <summary>
        ''' 初始化智能流实例。
        ''' </summary>
        ''' <param name="baseStream">底层线性流</param>
        ''' <param name="fixedReadSize">每次从底层读取的固定大小</param>
        ''' <param name="minReadThreshold">当有其他 Seek 阻塞时的最小读取量</param>
        ''' <param name="maxSeekReadSize">无阻塞时最大读取量</param>
        ''' <param name="bufferSize">缓存块上限数量</param>
        Public Sub New(baseStream As Stream, fixedReadSize As Integer, minReadThreshold As Integer, maxSeekReadSize As Integer, bufferSize As Integer)
            Me.baseStream = baseStream
            Me.fixedReadSize = fixedReadSize
            Me.minReadThreshold = minReadThreshold
            Me.maxSeekReadSize = maxSeekReadSize
            Me.bufferSize = bufferSize
        End Sub

        ''' <summary>
        ''' 从当前位置读取数据。会优先从缓存中读取。
        ''' </summary>
        Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
            SyncLock bufferLock
                Dim block = FindBlock(PositionValue)
                If block Is Nothing Then
                    RaiseEvent DebugLog($"[SmartStream] 缓存未命中：位置 {PositionValue}，准备缓冲...")
                    EnsureBuffered(PositionValue)
                    block = FindBlock(PositionValue)
                    If block Is Nothing Then Return 0
                Else
                    RaiseEvent DebugLog($"[SmartStream] 缓存命中：位置 {PositionValue}。")
                End If

                block.LastAccess = DateTime.Now
                block.AccessCount += 1

                Dim offsetInBlock = block.GetOffset(PositionValue)
                Dim readable = Math.Min(count, block.Data.Length - offsetInBlock)

                Array.Copy(block.Data, offsetInBlock, buffer, offset, readable)
                PositionValue += readable

                TryPreloadNextBlock(PositionValue)

                Return readable
            End SyncLock
        End Function

        ''' <summary>
        ''' 尝试预读取当前位置后一个块。
        ''' </summary>
        Private Sub TryPreloadNextBlock(currentPos As Long)
            Dim nextPos = ((currentPos \ fixedReadSize) + 1) * fixedReadSize
            If nextPos >= Length Then Exit Sub
            If FindBlock(nextPos) Is Nothing Then
                RaiseEvent DebugLog($"[SmartStream] 预读取下一块：{nextPos}...")
                LoadBuffer(nextPos, fixedReadSize)
            End If
        End Sub

        ''' <summary>
        ''' 确保指定位置被缓冲。
        ''' </summary>
        Private Sub EnsureBuffered(pos As Long)
            If pos >= Length Then Exit Sub
            If FindBlock(pos) IsNot Nothing Then Return

            ReadLock.Wait()
            Try
                If FindBlock(pos) Is Nothing Then
                    Dim readSize = If(OtherSeekPending(), minReadThreshold, maxSeekReadSize)
                    readSize = CInt(Math.Min(readSize, Length - pos))
                    RaiseEvent DebugLog($"[SmartStream] 从位置 {pos} 读取 {readSize} 字节...")
                    LoadBuffer(pos, readSize)
                End If
            Finally
                ReadLock.Release()
            End Try
        End Sub

        ''' <summary>
        ''' 检测是否有其他 Seek 正在等待（伪实现）。
        ''' </summary>
        Private Function OtherSeekPending() As Boolean
            Return False
        End Function

        ''' <summary>
        ''' 从底层流读取数据并加入缓存。
        ''' </summary>
        Private Sub LoadBuffer(position As Long, size As Integer)
            SyncLock bufferLock
                Dim alignedPos = (position \ fixedReadSize) * fixedReadSize
                If alignedPos >= Length Then Exit Sub

                baseStream.Seek(alignedPos, SeekOrigin.Begin)
                size = CInt(Math.Min(size, Length - alignedPos))

                Dim data(size - 1) As Byte
                Dim bytesRead = baseStream.Read(data, 0, size)
                If bytesRead > 0 Then
                    ReDim Preserve data(bytesRead - 1)
                    bufferBlocks.Add(New BufferBlock With {
                    .StartPos = alignedPos,
                    .Data = data
                })
                    CleanupBuffer()
                    RaiseEvent DebugLog($"[SmartStream] 已缓存块：起始 {alignedPos}，长度 {bytesRead} 字节。")
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' 淘汰最不常用的缓存块。
        ''' </summary>
        Private Sub CleanupBuffer()
            While bufferBlocks.Count > bufferSize
                Dim oldest = bufferBlocks.OrderByDescending(Function(b)
                                                                Dim age = (DateTime.Now - b.LastAccess).TotalSeconds
                                                                Return age / Math.Max(1, b.AccessCount)
                                                            End Function).First()
                RaiseEvent DebugLog($"[SmartStream] 淘汰缓存块：起始位置 {oldest.StartPos}。")
                bufferBlocks.Remove(oldest)
            End While
        End Sub

        ''' <summary>
        ''' 查找包含指定位置的缓存块。
        ''' </summary>
        Private Function FindBlock(pos As Long) As BufferBlock
            Return bufferBlocks.FirstOrDefault(Function(b) b.Covers(pos))
        End Function

        Public Overrides Property Position As Long
            Get
                Return PositionValue
            End Get
            Set(value As Long)
                PositionValue = value
            End Set
        End Property

        Public Overrides ReadOnly Property CanRead As Boolean = True
        Public Overrides ReadOnly Property CanSeek As Boolean = True
        Public Overrides ReadOnly Property CanWrite As Boolean = False
        Public Overrides ReadOnly Property Length As Long = baseStream.Length

        Public Overrides Sub Flush()
            ' 不执行任何操作
        End Sub

        ''' <summary>
        ''' 定位到指定位置，不清空缓存。
        ''' </summary>
        Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
            Select Case origin
                Case SeekOrigin.Begin
                    Position = offset
                Case SeekOrigin.Current
                    Position += offset
                Case SeekOrigin.End
                    Position = Length + offset
            End Select
            RaiseEvent DebugLog($"[SmartStream] Seek 定位至 {Position}。")
            Return Position
        End Function

        Public Overrides Sub SetLength(value As Long)
            Throw New NotSupportedException()
        End Sub

        Public Overrides Sub Write(buffer() As Byte, offset As Integer, count As Integer)
            Throw New NotSupportedException()
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                baseStream.Dispose()
                ReadLock.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub
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
            Dim sb As New System.Text.StringBuilder
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
            Dim rawdata() As Byte = System.Text.Encoding.UTF8.GetBytes(Me.GetSerializedText())
            Dim senddata As New List(Of Byte)
            senddata.AddRange(BitConverter.GetBytes(rawdata.Length))
            senddata.AddRange(rawdata)

            Dim _
                sck As _
                    New Net.Sockets.Socket(Net.Sockets.AddressFamily.InterNetwork, Net.Sockets.SocketType.Stream,
                                           Net.Sockets.ProtocolType.Tcp)
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
                         Dim msg As String = System.Text.Encoding.UTF8.GetString(data)
                         sck.Close()
                         result = NetworkCommand.FromXML(msg)
                         TaskFinished = True
                     End Sub).Wait(10000)
            Return result
        End Function
    End Class


    Public Class StreamPcmPlayer
        Private waveOut As IWavePlayer
        Private waveProvider As BufferedWaveProvider
        Private waveFormat As WaveFormat
        Private isInitialized As Boolean = False

        ''' <summary>
        ''' 初始化播放器
        ''' </summary>
        ''' <param name="sampleRate">采样率（如 44100）</param>
        ''' <param name="channels">通道数（1=单声道，2=立体声）</param>
        ''' <param name="bitsPerSample">位深（如 16）</param>
        Public Sub Init(sampleRate As Integer, channels As Integer, bitsPerSample As Integer, isFloat As Boolean, Optional ByVal ReInitialize As Boolean = False, Optional ByVal MinBufferLen As Integer = 1048576)
            If isFloat Then
                waveFormat = New WaveFormat(sampleRate, channels)
            Else
                waveFormat = New WaveFormat(sampleRate, bitsPerSample, channels)
            End If
            If ReInitialize Then LastExtraBytes = {}
            waveProvider = New BufferedWaveProvider(waveFormat)
            waveProvider.DiscardOnBufferOverflow = True ' 防止溢出
            If waveProvider.BufferLength < MinBufferLen Then waveProvider.BufferLength = MinBufferLen
            waveOut = New WaveOut()
            waveOut.Init(waveProvider)
            waveOut.Volume = 1
            waveOut.Play()
            isInitialized = True
        End Sub
        Public Shared LastExtraBytes As Byte()

        ''' <summary>
        ''' 追加 PCM 数据流（字节数组）
        ''' </summary>
        ''' <param name="pcmBytes">PCM 数据块</param>
        Public Sub AddData(pcmBytes As Byte(), Optional ByVal convertFloatToInt As Boolean = False)
            If isInitialized Then
                If convertFloatToInt Then
                    If LastExtraBytes IsNot Nothing AndAlso LastExtraBytes.Length > 0 Then
                        pcmBytes = LastExtraBytes.Concat(pcmBytes).ToArray()
                    End If

                    If pcmBytes.Length Mod 4 <> 0 Then
                        Dim xtra As New List(Of Byte)
                        For i As Integer = (pcmBytes.Length \ 4) * 4 To pcmBytes.Length - 1
                            xtra.Add(pcmBytes(i))
                        Next
                        LastExtraBytes = xtra.ToArray()
                    Else
                        LastExtraBytes = {}
                    End If

                    Dim newValues As Byte()
                    Dim outValue As New List(Of Byte)
                    For i As Integer = 0 To ((pcmBytes.Length \ 4) * 4 - 1) Step 4
                        Dim fvalue As Single = BitConverter.ToSingle(pcmBytes, i)
                        Dim intvalue As Int16 = Math.Min(32767.0, Math.Max(-32768.0, fvalue * 32767.0))
                        outValue.AddRange(BitConverter.GetBytes(intvalue))
                    Next
                    pcmBytes = outValue.ToArray()
                End If

                While waveProvider.BufferedBytes + pcmBytes.Length > waveProvider.BufferLength
                    Threading.Thread.Sleep(20)
                End While
                ' 直接添加 PCM 数据
                waveProvider.AddSamples(pcmBytes, 0, pcmBytes.Length)
            End If
        End Sub
        Public Sub Flush()
            If Not IsPlaying Then Exit Sub
            If LastExtraBytes Is Nothing OrElse LastExtraBytes.Length = 0 Then
            Else
                While waveProvider.BufferedBytes + LastExtraBytes.Length > waveProvider.BufferLength
                    Threading.Thread.Sleep(100)
                End While
                waveProvider.AddSamples(LastExtraBytes, 0, LastExtraBytes.Length)
            End If
        End Sub
        ''' <summary>
        ''' 判断是否正在播放（缓冲区有数据 且 播放器状态为 Playing）
        ''' </summary>
        Public ReadOnly Property IsPlaying As Boolean
            Get
                If Not isInitialized OrElse waveOut Is Nothing Then Return False
                Return waveOut.PlaybackState = PlaybackState.Playing AndAlso waveProvider.BufferedBytes > 0
            End Get
        End Property
        ''' <summary>
        ''' 停止播放并释放资源
        ''' </summary>
        Public Sub StopPlayback()
            If waveOut IsNot Nothing Then
                waveOut.Stop()
                waveOut.Dispose()
                waveOut = Nothing
            End If
            isInitialized = False
        End Sub
    End Class


End Class
Public Class ZBCDeviceHelper
    Public Property handle As IntPtr
    Public Property MaximumLBA As ULong
    Public Property SectorLength As UInt16 = 512
    Public Property CommandLengthLimit As Integer = 524288

    Private _CMRStartLBA As ULong
    Public ReadOnly Property CMRStartLBA As ULong
        Get
            Return _CMRStartLBA
        End Get
    End Property
    Private _CMRLBACount As ULong
    Public ReadOnly Property CMRLBACount As ULong
        Get
            Return _CMRLBACount
        End Get
    End Property
    Public ReadOnly Property CMREndLBA As ULong
        Get
            Return CMRStartLBA + CMRLBACount - 1
        End Get
    End Property
    Public Class Zone
        Public Enum ZoneTypeDef As Byte
            Reserved = 0
            Conventional = 1
            Sequential = 2
        End Enum
        Public Property ZoneType As ZoneTypeDef
        Public Enum ZoneConditionDef As Byte
            NOT_WRITE_POINTER = 0
            EMPTY = 1
            IMPLICIT_OPENED = 2
            EXPLICIT_OPENED = 3
            CLOSED = 4
            FULL = 5
        End Enum
        Public Property ZoneCondition As ZoneConditionDef
        Public Property NON_SEQ As Boolean
        Public Property RESET As Boolean
        Public ReadOnly Property WRITER_POINTER_LBA_INVALID As Boolean
            Get
                Return ZoneCondition = ZoneConditionDef.NOT_WRITE_POINTER OrElse ZoneCondition = ZoneConditionDef.FULL
            End Get
        End Property
        Public Property ZoneLength As ULong
        Public Property ZoneStartLBA As ULong
        Public ReadOnly Property ZoneEndLBA As ULong
            Get
                Return ZoneStartLBA + ZoneLength - 1
            End Get
        End Property
        Public Property ZoneWritePointerLBA As ULong
        Public Sub New()

        End Sub
        Public Sub New(RawData As Byte(), Optional ByVal StartByte As Integer = 0)
            If RawData.Length < StartByte + 64 Then Exit Sub
            ZoneType = RawData(StartByte + 0) And &HF
            ZoneCondition = RawData(StartByte + 1) >> 4 And &HF
            NON_SEQ = RawData(StartByte + 1) >> 1 And 1
            RESET = RawData(StartByte + 1) >> 0 And 1
            ZoneLength = BigEndianConverter.ToUInt64(RawData, StartByte + 8)
            ZoneStartLBA = BigEndianConverter.ToUInt64(RawData, StartByte + 16)
            ZoneWritePointerLBA = BigEndianConverter.ToUInt64(RawData, StartByte + 24)
        End Sub
    End Class
    Public Property ZoneList As New List(Of Zone)
    Private ZoneLBAMap As New Dictionary(Of ULong, Zone)
    Public Sub InitDevice()
        TapeUtils.LoadEject(handle, TapeUtils.LoadOption.LoadThreaded)
        Dim MP03 As Byte() = TapeUtils.ModeSense(handle, 3)
        SectorLength = BigEndianConverter.ToUInt16(MP03, 12)
        ReportZones()
        LoadData()
    End Sub
    Public Sub ReportZones()
        Dim data0 As Byte() = TapeUtils.SCSIReadParam(handle, {&H95, 0,
                                                      0, 0, 0, 0, 0, 0, 0, 0,
                                                      0, 0, 0, &H40,
                                                      0, 0}, 64)
        MaximumLBA = BigEndianConverter.ToUInt64(data0, 8)
        Dim ZoneListLen As UInteger = BigEndianConverter.ToUInt32(data0, 0)
        Dim ZoneCount As UInteger = ZoneListLen \ 64UI
        Dim currLBA As ULong = 0
        ZoneList.Clear()
        ZoneLBAMap.Clear()
        While True
            Dim data1 As Byte() = TapeUtils.SCSIReadParam(handle, {&H95, 0,
                                                          CByte((currLBA >> 56) And &HFF),
                                                          CByte((currLBA >> 48) And &HFF),
                                                          CByte((currLBA >> 40) And &HFF),
                                                          CByte((currLBA >> 32) And &HFF),
                                                          CByte((currLBA >> 24) And &HFF),
                                                          CByte((currLBA >> 16) And &HFF),
                                                          CByte((currLBA >> 8) And &HFF),
                                                          CByte((currLBA >> 0) And &HFF),
                                                          CByte((CommandLengthLimit >> 24) And &HFF),
                                                          CByte((CommandLengthLimit >> 16) And &HFF),
                                                          CByte((CommandLengthLimit >> 8) And &HFF),
                                                          CByte((CommandLengthLimit >> 0) And &HFF),
                                                          &H80, 0}, CommandLengthLimit)
            ZoneListLen = BigEndianConverter.ToUInt32(data1, 0)
            If ZoneListLen = 0 Then Exit While
            ZoneCount = ZoneListLen \ 64UI
            Dim readed As Zone = Nothing
            For i As Integer = 0 To ZoneCount - 1
                readed = New Zone(data1, 64 + 64 * i)
                ZoneList.Add(readed)
                ZoneLBAMap.Add(readed.ZoneStartLBA, readed)
            Next
            If readed IsNot Nothing Then currLBA = readed.ZoneStartLBA + readed.ZoneLength
            If ZoneListLen < data1.Length - 64 Then Exit While
        End While
        ZoneList.Sort(New Comparison(Of Zone)(Function(a As Zone, b As Zone) As Integer
                                                  Return a.ZoneStartLBA.CompareTo(b.ZoneStartLBA)
                                              End Function))
        Dim ZIDC1 As Integer = -1
        For i As Integer = 0 To ZoneList.Count - 1
            If ZoneList(i).ZoneType = Zone.ZoneTypeDef.Conventional Then
                _CMRStartLBA = ZoneList(i).ZoneStartLBA
                ZIDC1 = i
                Exit For
            End If
        Next
        If ZIDC1 >= 0 Then
            Dim idstep1 As Integer = ZoneList.Count \ 1000
            For i As Integer = ZIDC1 To ZoneList.Count - 1 Step idstep1
                If (i + idstep1) >= ZoneList.Count OrElse (ZoneList(i).ZoneType = Zone.ZoneTypeDef.Conventional AndAlso ZoneList(i + idstep1).ZoneType <> Zone.ZoneTypeDef.Conventional) Then
                    Dim found As Boolean = False
                    For j As Integer = i + 1 To Math.Min(i + idstep1 - 1, ZoneList.Count - 1)
                        If ZoneList(j).ZoneType <> Zone.ZoneTypeDef.Conventional Then
                            _CMRLBACount = ZoneList(j - 1).ZoneEndLBA - CMRStartLBA + 1
                            found = True
                            Exit For
                        End If
                    Next
                    If found Then Exit For
                End If
            Next
        End If
    End Sub
    Public Sub RefreshZoneCondition(ToRefresh As Zone)
        Dim data1 As Byte() = TapeUtils.SCSIReadParam(handle, {&H95, 0,
                                                          CByte((ToRefresh.ZoneStartLBA >> 56) And &HFF),
                                                          CByte((ToRefresh.ZoneStartLBA >> 48) And &HFF),
                                                          CByte((ToRefresh.ZoneStartLBA >> 40) And &HFF),
                                                          CByte((ToRefresh.ZoneStartLBA >> 32) And &HFF),
                                                          CByte((ToRefresh.ZoneStartLBA >> 24) And &HFF),
                                                          CByte((ToRefresh.ZoneStartLBA >> 16) And &HFF),
                                                          CByte((ToRefresh.ZoneStartLBA >> 8) And &HFF),
                                                          CByte((ToRefresh.ZoneStartLBA >> 0) And &HFF),
                                                          0, 0, 0, 128,
                                                          &H80, 0}, CommandLengthLimit)
        Dim readed As New Zone(data1, 64)
        With ToRefresh
            .NON_SEQ = readed.NON_SEQ
            .RESET = readed.RESET
            .ZoneCondition = readed.ZoneCondition
            .ZoneLength = readed.ZoneLength
            .ZoneStartLBA = readed.ZoneStartLBA
            .ZoneType = readed.ZoneType
            .ZoneWritePointerLBA = readed.ZoneWritePointerLBA
        End With
    End Sub
    Public Function GetZoneByLBA(LBA As ULong) As Zone
        Dim result As Zone
        If ZoneList Is Nothing OrElse ZoneList.Count = 0 Then ReportZones()
        If ZoneList Is Nothing OrElse ZoneList.Count = 0 Then Return Nothing
        If ZoneLBAMap.TryGetValue(LBA, result) Then Return result
        Dim SearchStart As Integer = 0, SearchEnd As Integer = ZoneList.Count - 1
        Dim idx As Integer = (SearchStart + SearchEnd) \ 2
        If LBA > ZoneList.Last.ZoneEndLBA Then Return Nothing
        While Not (ZoneList(idx).ZoneStartLBA <= LBA AndAlso ZoneList(idx).ZoneEndLBA >= LBA)
            If ZoneList(idx).ZoneStartLBA > LBA Then
                SearchEnd = idx
            ElseIf ZoneList(idx).ZoneEndLBA < LBA Then
                SearchStart = idx
            Else
                Return Nothing
            End If
            If (SearchEnd - SearchStart) > 1 Then
                idx = (SearchStart + SearchEnd) \ 2
            Else
                idx += 1
            End If
        End While
        Return ZoneList(idx)
    End Function
    Public Function CloseAllZones(Optional ByRef sense As Byte() = Nothing) As Boolean
        Dim senseFin As Boolean = False
        Dim senseresult As Byte()
        Dim result As Boolean = TapeUtils.SendSCSICommand(handle, {&H94, &H1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0}, Nothing, 1,
                                         Function(sdata As Byte())
                                             senseresult = sdata
                                             senseFin = True
                                             Return True
                                         End Function)
        For i As Integer = 0 To 10
            If senseFin Then Exit For
            Threading.Thread.Sleep(1)
        Next
        sense = senseresult
        Return result
    End Function
    Public Function ResetWritePointer(LowestLBA As ULong, Optional ByRef sense As Byte() = Nothing) As Boolean
        Dim senseFin As Boolean = False
        Dim senseresult As Byte()
        Dim result As Boolean = TapeUtils.SendSCSICommand(
            handle, {&H94, &H4,
            CByte((LowestLBA >> 56) And &HFF),
            CByte((LowestLBA >> 48) And &HFF),
            CByte((LowestLBA >> 40) And &HFF),
            CByte((LowestLBA >> 32) And &HFF),
            CByte((LowestLBA >> 24) And &HFF),
            CByte((LowestLBA >> 16) And &HFF),
            CByte((LowestLBA >> 8) And &HFF),
            CByte((LowestLBA >> 0) And &HFF),
            0, 0, 0, 0, 0, 0}, Nothing, 1,
                                         Function(sdata As Byte())
                                             senseresult = sdata
                                             senseFin = True
                                             Return True
                                         End Function)

        For i As Integer = 0 To 10
            If senseFin Then Exit For
            Threading.Thread.Sleep(1)
        Next
        sense = senseresult
        Return result
    End Function
    Public Function OpenZone(LowestLBA As ULong, Optional ByRef sense As Byte() = Nothing) As Boolean
        Dim senseFin As Boolean = False
        Dim senseresult As Byte()
        Dim result As Boolean = TapeUtils.SendSCSICommand(
            handle, {&H94, &H3,
            CByte((LowestLBA >> 56) And &HFF),
            CByte((LowestLBA >> 48) And &HFF),
            CByte((LowestLBA >> 40) And &HFF),
            CByte((LowestLBA >> 32) And &HFF),
            CByte((LowestLBA >> 24) And &HFF),
            CByte((LowestLBA >> 16) And &HFF),
            CByte((LowestLBA >> 8) And &HFF),
            CByte((LowestLBA >> 0) And &HFF),
            0, 0, 0, 0, 0, 0}, Nothing, 1,
                                         Function(sdata As Byte())
                                             senseresult = sdata
                                             senseFin = True
                                             Return True
                                         End Function)

        For i As Integer = 0 To 10
            If senseFin Then Exit For
            Threading.Thread.Sleep(1)
        Next
        sense = senseresult
        Return result
    End Function
    Public Function CloseZone(LowestLBA As ULong, Optional ByRef sense As Byte() = Nothing) As Boolean
        Dim senseFin As Boolean = False
        Dim senseresult As Byte()
        Dim result As Boolean = TapeUtils.SendSCSICommand(
            handle, {&H94, &H1,
            CByte((LowestLBA >> 56) And &HFF),
            CByte((LowestLBA >> 48) And &HFF),
            CByte((LowestLBA >> 40) And &HFF),
            CByte((LowestLBA >> 32) And &HFF),
            CByte((LowestLBA >> 24) And &HFF),
            CByte((LowestLBA >> 16) And &HFF),
            CByte((LowestLBA >> 8) And &HFF),
            CByte((LowestLBA >> 0) And &HFF),
            0, 0, 0, 0, 0, 0}, Nothing, 1,
                                         Function(sdata As Byte())
                                             senseresult = sdata
                                             senseFin = True
                                             Return True
                                         End Function)

        For i As Integer = 0 To 10
            If senseFin Then Exit For
            Threading.Thread.Sleep(1)
        Next
        sense = senseresult
        Return result
    End Function
    Public Function FinishZone(LowestLBA As ULong, Optional ByRef sense As Byte() = Nothing) As Boolean
        Dim senseFin As Boolean = False
        Dim senseresult As Byte()
        Dim result As Boolean = TapeUtils.SendSCSICommand(
            handle, {&H94, &H2,
            CByte((LowestLBA >> 56) And &HFF),
            CByte((LowestLBA >> 48) And &HFF),
            CByte((LowestLBA >> 40) And &HFF),
            CByte((LowestLBA >> 32) And &HFF),
            CByte((LowestLBA >> 24) And &HFF),
            CByte((LowestLBA >> 16) And &HFF),
            CByte((LowestLBA >> 8) And &HFF),
            CByte((LowestLBA >> 0) And &HFF),
            0, 0, 0, 0, 0, 0}, Nothing, 1,
                                         Function(sdata As Byte())
                                             senseresult = sdata
                                             senseFin = True
                                             Return True
                                         End Function)

        For i As Integer = 0 To 10
            If senseFin Then Exit For
            Threading.Thread.Sleep(1)
        Next
        sense = senseresult
        Return result
    End Function
    Public Function ReadBytes(StartLBA As ULong, ByVal ByteOffset As UInt16, ReadLen As ULong) As Byte()
        Dim result As New List(Of Byte)
        Dim remain As ULong = ReadLen
        Dim oncereadsectorcount As Integer = Math.Truncate(CommandLengthLimit / SectorLength)
        Dim currentLBA As ULong = StartLBA
        While remain > 0
            Dim data As Byte() = TapeUtils.SCSIReadParam(handle, {
                &H28, 0,
                CByte((StartLBA >> 24) And &HFF),
                CByte((StartLBA >> 16) And &HFF),
                CByte((StartLBA >> 8) And &HFF),
                CByte((StartLBA >> 0) And &HFF),
                0,
                CByte((oncereadsectorcount >> 8) And &HFF),
                CByte((oncereadsectorcount >> 0) And &HFF),
                 0}, oncereadsectorcount * SectorLength)
            If currentLBA = StartLBA AndAlso ByteOffset > 0 Then
                data = data.Skip(ByteOffset).ToArray()
            End If
            If data.Length > remain Then data = data.Take(remain).ToArray()
            result.AddRange(data)
            remain -= data.Length
            currentLBA += oncereadsectorcount
        End While
        Return result.ToArray()
    End Function
    Public Function WriteBytes(ByVal source As Byte(), StartLBA As ULong, ByVal ByteOffset As UInt16, Optional ByVal Conventional As Boolean = True) As Boolean
        Dim result As New List(Of Byte)
        Dim remain As Integer = source.Length
        Dim oncewritesectorcount As Integer = Math.Truncate(CommandLengthLimit / SectorLength)
        Dim currentLBA As ULong = StartLBA
        Dim totalSector As ULong = Math.Ceiling((source.Length + ByteOffset) / SectorLength)
        Dim EndLBA As ULong = StartLBA + totalSector - 1
        Dim tempData As New Dictionary(Of ULong, Byte())
        Dim zone0, zone1 As Zone
        If ByteOffset > 0 Then
            source = ReadBytes(StartLBA, 0, ByteOffset).Concat(source).ToArray()
        End If
        If Not Conventional Then
            ' check zone condition
            zone0 = GetZoneByLBA(StartLBA)
            zone1 = GetZoneByLBA(EndLBA)
            If zone0.ZoneStartLBA < StartLBA Then
                If zone0.ZoneCondition = Zone.ZoneConditionDef.EMPTY Then
                    'empty
                    Dim empty(SectorLength - 1) As Byte
                    OpenZone(zone0.ZoneStartLBA)
                    For lba As ULong = zone0.ZoneStartLBA To StartLBA - 1
                        WriteBytes(empty, lba, 0, True)
                    Next
                ElseIf (Not zone0.WRITER_POINTER_LBA_INVALID) AndAlso zone0.ZoneWritePointerLBA <= StartLBA Then
                    'data end < write position
                    If zone0.ZoneCondition = Zone.ZoneConditionDef.CLOSED Then OpenZone(zone0.ZoneStartLBA)
                    Dim empty(SectorLength - 1) As Byte
                    For lba As ULong = zone0.ZoneWritePointerLBA To StartLBA - 1
                        WriteBytes(empty, lba, 0, True)
                    Next
                ElseIf ((Not zone0.WRITER_POINTER_LBA_INVALID) AndAlso zone0.ZoneWritePointerLBA > StartLBA) OrElse zone0.ZoneCondition = Zone.ZoneConditionDef.FULL Then
                    'data end >= write position
                    For lba As ULong = zone0.ZoneStartLBA To StartLBA - 1
                        tempData.Add(lba, ReadBytes(lba, 0, SectorLength))
                    Next
                    ResetWritePointer(zone0.ZoneStartLBA)
                    OpenZone(zone0.ZoneStartLBA)
                    For lba As ULong = zone0.ZoneStartLBA To StartLBA - 1
                        WriteBytes(tempData(lba), lba, 0, True)
                    Next
                Else
                    'cmr zone
                End If
            End If
            If zone1.ZoneEndLBA > EndLBA Then
                For lba As ULong = EndLBA + 1 To zone1.ZoneEndLBA
                    tempData.Add(lba, ReadBytes(lba, 0, SectorLength))
                Next
                ResetWritePointer(zone1.ZoneStartLBA)
            End If
        End If
        Dim currentZone As Zone = zone0
        Dim currentEndZone As Zone
        While remain > 0
            Dim sendlen As Integer = Math.Min(oncewritesectorcount * SectorLength, remain)
            Dim currentsendsectorcount As Integer = Math.Ceiling(sendlen / SectorLength)
            currentEndZone = GetZoneByLBA(currentLBA + currentsendsectorcount - 1)
            For i As Integer = ZoneList.IndexOf(currentZone) + 1 To ZoneList.IndexOf(currentEndZone)
                OpenZone(ZoneList(i).ZoneStartLBA)
            Next
            Dim toSend(currentsendsectorcount * SectorLength - 1) As Byte
            Array.Copy(source, source.Length - remain, toSend, 0, sendlen)
            TapeUtils.SendSCSICommand(handle, {
                &H2A, 0,
                CByte((currentLBA >> 24) And &HFF),
                CByte((currentLBA >> 16) And &HFF),
                CByte((currentLBA >> 8) And &HFF),
                CByte((currentLBA >> 0) And &HFF),
                0,
                CByte((currentsendsectorcount >> 8) And &HFF),
                CByte((currentsendsectorcount >> 0) And &HFF),
                 0}, toSend, 0)

            remain -= sendlen
            currentLBA += currentsendsectorcount
            If Not Conventional Then
                Dim nextZone As Zone = GetZoneByLBA(currentLBA)
                If nextZone.ZoneStartLBA > currentEndZone.ZoneStartLBA Then
                    For i As Integer = ZoneList.IndexOf(currentZone) To ZoneList.IndexOf(currentZone)
                        FinishZone(ZoneList(i).ZoneStartLBA)
                    Next
                Else
                    For i As Integer = ZoneList.IndexOf(currentZone) To ZoneList.IndexOf(currentEndZone) - 1
                        FinishZone(ZoneList(i).ZoneStartLBA)
                    Next
                End If
                currentZone = nextZone
            End If
        End While

        If Not Conventional Then
            'check zone condition
            RefreshZoneCondition(currentEndZone)
            If currentEndZone.ZoneCondition <> Zone.ZoneConditionDef.FULL AndAlso
                currentEndZone.ZoneCondition <> Zone.ZoneConditionDef.CLOSED AndAlso
                currentEndZone.ZoneCondition <> Zone.ZoneConditionDef.NOT_WRITE_POINTER Then
                CloseZone(currentEndZone.ZoneStartLBA)
            End If
        End If
        Return True
    End Function
    Public Property Data As ZBCDataHelper
    Public Property DataStartLBA As ULong
    Public Property DataEndLBA As ULong
    Public Sub LoadData()
        Dim vol1 As Byte() = ReadBytes(0, 0, SectorLength)
        Dim header As String = BitConverter.ToString(vol1, &H163, 16)
        If Not header.StartsWith("LCGZBC") Then Exit Sub
        DataStartLBA = BigEndianConverter.ToUInt64(vol1, &H1CE + 8) And &HFFFFFF
        Dim ZBCDataLen As ULong = (BigEndianConverter.ToUInt64(vol1, &H1CE + 12) And &HFFFFFF) * SectorLength
        DataEndLBA = DataStartLBA + Math.Ceiling(ZBCDataLen / SectorLength) - 1
        Data = ZBCDataHelper.FromXML(BitConverter.ToString(ReadBytes(DataStartLBA, 0, ZBCDataLen)).TrimEnd(vbNullChar))
    End Sub
    Public Sub SaveData()
        Dim vol1 As Byte() = ReadBytes(0, 0, SectorLength)
        Dim header As Byte() = Encoding.ASCII.GetBytes("LCGZBC")
        Array.Copy(header, 0, vol1, &H163, header.Length)
        Dim dataBinary As Byte() = Encoding.UTF8.GetBytes(Data.GetSerializedText())
        Dim StartLBABytes As Byte() = BigEndianConverter.GetBytes(DataStartLBA)
        Dim LenLBABytes As Byte() = BigEndianConverter.GetBytes(CULng(Math.Ceiling((dataBinary.Length) / SectorLength)))
        Array.Copy(StartLBABytes, 0, vol1, &H1CE + 8, 8)
        Array.Copy(LenLBABytes, 0, vol1, &H1CE + 12, 8)
        WriteBytes(vol1, 0, 0, True)
    End Sub
    <Serializable>
    Public Class ZBCDataHelper
        <XmlIgnore>
        Public Device As ZBCDeviceHelper
        Public Property CMRDataStartLBA As ULong
        Public Property CMRDataLength As ULong
        Public Sub WriteCMRData(toWrite As Byte())
            CMRDataLength = toWrite.Length
            Device.WriteBytes(toWrite, CMRDataStartLBA, 0, True)
        End Sub
        Public Function ReadCMRData() As Byte()
            Device.ReadBytes(CMRDataStartLBA, 0, CMRDataLength)
        End Function
        Public Property DataStreamList As New List(Of DataStream)

        <Serializable>
        Public Class DataStream
            Inherits IO.Stream

            Public Parent As ZBCDataHelper
            Public Property StartLBA As ULong
            Public Property MaxLength As ULong = ULong.MaxValue

            Private _Position As ULong = 0
            Private _Length As ULong = 0

            Private writeBuffer As IO.MemoryStream = Nothing
            Private bufferStartPos As ULong = 0

            Private currentZoneStartLBA As ULong = 0
            Private currentZoneOpen As Boolean = False

            ' ------------------- Stream 基本属性 -------------------
            Public Overrides ReadOnly Property CanRead As Boolean
                Get
                    Return Parent IsNot Nothing
                End Get
            End Property
            Public Overrides ReadOnly Property CanSeek As Boolean
                Get
                    Return Parent IsNot Nothing
                End Get
            End Property
            Public Overrides ReadOnly Property CanWrite As Boolean
                Get
                    Return Parent IsNot Nothing
                End Get
            End Property
            Public Overrides ReadOnly Property Length As Long
                Get
                    Return CLng(_Length)
                End Get
            End Property
            Public Overrides Property Position As Long
                Get
                    Return CLng(_Position)
                End Get
                Set(value As Long)
                    Seek(value, SeekOrigin.Begin)
                End Set
            End Property
            Public Overrides Sub SetLength(value As Long)
                If value < 0 Then Throw New ArgumentOutOfRangeException("value")
                SyncLock Me
                    _Length = CULng(value)
                End SyncLock
            End Sub

            ' ------------------- 工具函数 -------------------
            Private Function BytePosToLBA(pos As ULong) As ULong
                Return StartLBA + pos \ Parent.Device.SectorLength
            End Function
            Private Function BytePosToSectorOffset(pos As ULong) As Integer
                Return CInt(pos Mod CUInt(Parent.Device.SectorLength))
            End Function
            Private Function GetZoneForBytePos(pos As ULong) As Zone
                Return Parent.Device.GetZoneByLBA(BytePosToLBA(pos))
            End Function

            ' 确保 zone 处于 OPEN 状态；必要时打开并刷新
            Private Sub EnsureZoneOpen(zoneStartLBA As ULong)
                SyncLock Me
                    If currentZoneOpen AndAlso currentZoneStartLBA = zoneStartLBA Then Return

                    ' 切换 zone 前刷新旧 zone 状态
                    If currentZoneOpen Then
                        Dim oldZone As Zone = Parent.Device.GetZoneByLBA(currentZoneStartLBA)
                        Parent.Device.RefreshZoneCondition(oldZone)
                    End If

                    ' 打开新 zone
                    Parent.Device.OpenZone(zoneStartLBA)
                    Dim z As Zone = Parent.Device.GetZoneByLBA(zoneStartLBA)
                    ' 打开后刷新一次（open 状态已改变）
                    Parent.Device.RefreshZoneCondition(z)

                    currentZoneStartLBA = zoneStartLBA
                    currentZoneOpen = True
                End SyncLock
            End Sub

            ' 内部写出缓存
            Private Sub FlushWriteBufferInternal(curZone As Zone)
                If writeBuffer Is Nothing OrElse writeBuffer.Length = 0 Then Return
                Dim data() As Byte = writeBuffer.ToArray()
                Dim startPos As ULong = bufferStartPos
                Dim firstLBA As ULong = BytePosToLBA(startPos)
                Dim byteOffset As UInteger = CUInt(startPos Mod CUInt(Parent.Device.SectorLength))

                ' ⚠️ WriteBytes仅执行纯数据写入，不负责zone状态。
                Parent.Device.WriteBytes(data, firstLBA, byteOffset, True)

                ' 写后刷新zone状态（因为write pointer改变）
                Parent.Device.RefreshZoneCondition(curZone)

                writeBuffer.Dispose()
                writeBuffer = Nothing
                bufferStartPos = 0
            End Sub

            Private Sub FlushWriteBuffer()
                SyncLock Me
                    Dim z As Zone = GetZoneForBytePos(_Position)
                    FlushWriteBufferInternal(z)
                End SyncLock
            End Sub

            ' 填充 0 扇区（用于填补 gap）
            Private Sub WriteZeroSectors(startLBA As ULong, sectorCount As ULong, z As Zone)
                Dim sectorLen As Integer = Parent.Device.SectorLength
                Dim zero(sectorLen - 1) As Byte
                For i As ULong = 0 To sectorCount - 1UL
                    ' 纯写入，不让WriteBytes处理zone
                    Parent.Device.WriteBytes(zero, startLBA + i, 0, True)
                Next
                ' 写完后刷新写指针
                Parent.Device.RefreshZoneCondition(z)
            End Sub

            ' ------------------- Flush 重载 -------------------
            Public Overrides Sub Flush()
                Flush(True)
            End Sub

            ''' <summary>
            ''' Flush(Close As Boolean)
            '''  - Close=False: 写出缓存但保持 zone open。
            '''  - Close=True: 写出后 finish+close zone 并刷新状态。
            ''' </summary>
            Public Overloads Sub Flush(Close As Boolean)
                SyncLock Me
                    Dim curZone As Zone = GetZoneForBytePos(_Position)
                    FlushWriteBufferInternal(curZone)

                    If currentZoneOpen Then
                        If Close Then
                            ' 先执行状态改变，再刷新
                            Parent.Device.FinishZone(currentZoneStartLBA)
                            Parent.Device.CloseZone(currentZoneStartLBA)
                            Parent.Device.RefreshZoneCondition(curZone)
                            currentZoneOpen = False
                        Else
                            Parent.Device.RefreshZoneCondition(curZone)
                        End If
                    End If
                End SyncLock
            End Sub

            ' ------------------- Read -------------------
            Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
                If buffer Is Nothing Then Throw New ArgumentNullException("buffer")
                SyncLock Me
                    Dim z As Zone = GetZoneForBytePos(_Position)
                    FlushWriteBufferInternal(z)
                    If _Position >= _Length Then Return 0

                    Dim available As ULong = _Length - _Position
                    Dim toRead As Integer = CInt(Math.Min(available, CULng(count)))
                    Dim sectorLen As Integer = Parent.Device.SectorLength
                    Dim bytesRead As Integer = 0

                    While bytesRead < toRead
                        Dim curLBA As ULong = BytePosToLBA(_Position)
                        Dim byteOffset As UInteger = CUInt(_Position Mod CUInt(sectorLen))
                        Dim chunk As Integer = Math.Min(toRead - bytesRead, sectorLen - CInt(byteOffset))
                        Dim data() As Byte = Parent.Device.ReadBytes(curLBA, byteOffset, CUInt(chunk))
                        Array.Copy(data, 0, buffer, offset + bytesRead, data.Length)
                        bytesRead += data.Length
                        _Position += CULng(data.Length)
                    End While
                    Return bytesRead
                End SyncLock
            End Function

            ' ------------------- Write -------------------
            Public Overrides Sub Write(buffer() As Byte, offset As Integer, count As Integer)
                If buffer Is Nothing Then Throw New ArgumentNullException("buffer")
                SyncLock Me
                    Dim bytesLeft As ULong = CULng(count)
                    Dim dataOff As Integer = offset
                    Dim sectorLen As Integer = Parent.Device.SectorLength

                    While bytesLeft > 0UL
                        Dim curZone As Zone = GetZoneForBytePos(_Position)
                        Parent.Device.RefreshZoneCondition(curZone)

                        Dim zoneStart As ULong = curZone.ZoneStartLBA
                        Dim zoneEnd As ULong = curZone.ZoneEndLBA
                        Dim curLBA As ULong = BytePosToLBA(_Position)
                        Dim byteOff As Integer = BytePosToSectorOffset(_Position)

                        EnsureZoneOpen(zoneStart)
                        Parent.Device.RefreshZoneCondition(curZone)

                        ' 若写入位置超过 write pointer，则填 0
                        If curZone.ZoneWritePointerLBA < curLBA Then
                            Dim fillCount As ULong = curLBA - curZone.ZoneWritePointerLBA
                            If fillCount > 0UL Then WriteZeroSectors(curZone.ZoneWritePointerLBA, fillCount, curZone)
                        End If

                        Dim bytesLeftInZone As ULong = (zoneEnd - curLBA + 1UL) * CULng(sectorLen) - CULng(byteOff)
                        Dim chunk As ULong = Math.Min(bytesLeftInZone, bytesLeft)

                        If writeBuffer Is Nothing Then
                            writeBuffer = New IO.MemoryStream()
                            bufferStartPos = _Position
                        ElseIf bufferStartPos + CULng(writeBuffer.Length) <> _Position Then
                            FlushWriteBufferInternal(curZone)
                            writeBuffer = New IO.MemoryStream()
                            bufferStartPos = _Position
                        End If

                        writeBuffer.Write(buffer, dataOff, CInt(chunk))
                        _Position += chunk
                        If _Position > _Length Then _Length = _Position
                        dataOff += CInt(chunk)
                        bytesLeft -= chunk

                        ' 跨 zone：必须 finish/close 旧 zone 后刷新状态
                        If bytesLeft > 0UL Then
                            FlushWriteBufferInternal(curZone)
                            Parent.Device.FinishZone(zoneStart)
                            Parent.Device.CloseZone(zoneStart)
                            Parent.Device.RefreshZoneCondition(curZone)

                            currentZoneOpen = False
                            Dim nextZone As Zone = GetZoneForBytePos(_Position)
                            EnsureZoneOpen(nextZone.ZoneStartLBA)
                        End If
                    End While
                End SyncLock
            End Sub

            ' ------------------- Seek -------------------
            Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
                Dim newPos As Long = If(origin = SeekOrigin.Begin, offset,
                           If(origin = SeekOrigin.Current, CLng(_Position) + offset,
                              CLng(_Length) + offset))
                If newPos < 0 Then Throw New ArgumentOutOfRangeException()

                SyncLock Me
                    If newPos < _Position Then
                        ' 向前 Seek：若要覆写，必须 Reset Write Pointer
                        Dim curZone As Zone = GetZoneForBytePos(_Position)
                        Parent.Device.RefreshZoneCondition(curZone)

                        Dim zoneStart As ULong = curZone.ZoneStartLBA
                        Dim secLen As Integer = Parent.Device.SectorLength
                        Dim zoneStartBytePos As ULong = (zoneStart - StartLBA) * CULng(secLen)
                        Dim writePtrBytePos As ULong = (curZone.ZoneWritePointerLBA - StartLBA) * CULng(secLen)

                        If newPos >= zoneStartBytePos AndAlso newPos < writePtrBytePos Then
                            EnsureZoneOpen(zoneStart)
                            Parent.Device.RefreshZoneCondition(curZone)

                            ' 读出 zone 已写数据
                            Dim preserved As New IO.MemoryStream()
                            Dim curByte As ULong = zoneStartBytePos
                            While curByte < writePtrBytePos
                                Dim lba As ULong = BytePosToLBA(curByte)
                                Dim off As UInteger = CUInt(curByte Mod CUInt(secLen))
                                Dim chunk As Integer = Math.Min(secLen - CInt(off), CInt(writePtrBytePos - curByte))
                                Dim data() As Byte = Parent.Device.ReadBytes(lba, off, CUInt(chunk))
                                preserved.Write(data, 0, data.Length)
                                curByte += CULng(data.Length)
                            End While

                            ' Reset Write Pointer -> 再刷新状态
                            Parent.Device.ResetWritePointer(zoneStart)
                            Parent.Device.RefreshZoneCondition(curZone)

                            ' 回写前缀
                            preserved.Seek(0, IO.SeekOrigin.Begin)
                            Dim restoreBytes As ULong = newPos - zoneStartBytePos
                            Dim tmp(secLen - 1) As Byte
                            Dim written As ULong = 0
                            While written < restoreBytes
                                Dim r As Integer = preserved.Read(tmp, 0, CInt(Math.Min(restoreBytes - written, tmp.Length)))
                                If r <= 0 Then Exit While
                                Dim lba As ULong = BytePosToLBA(zoneStartBytePos + written)
                                Dim off As UInteger = CUInt((zoneStartBytePos + written) Mod CUInt(secLen))
                                Parent.Device.WriteBytes(tmp, lba, off, True)
                                Parent.Device.RefreshZoneCondition(curZone)
                                written += CULng(r)
                            End While

                            _Position = CULng(newPos)
                            If _Position > _Length Then _Length = _Position
                            currentZoneStartLBA = zoneStart
                            currentZoneOpen = True
                            Return _Position
                        End If
                    End If
                    _Position = CULng(newPos)
                    If _Position > _Length Then _Length = _Position
                    Return _Position
                End SyncLock
            End Function
        End Class

        Public Sub CreateLTFSDefault(Optional ByVal SinglePartition As Boolean = False)
            CMRDataStartLBA = Device.DataStartLBA + 1024
            DataStreamList.Clear()
            Dim indexpartition As New DataStream With {.StartLBA = Device.CMREndLBA + 1}
            DataStreamList.Add(indexpartition)
            If SinglePartition Then
                indexpartition.MaxLength = (Device.MaximumLBA - indexpartition.StartLBA + 1) * Device.SectorLength
            Else
                indexpartition.MaxLength = Math.Ceiling(107374182400 / Device.SectorLength) * Device.SectorLength
                Dim datapartition As New DataStream With {.StartLBA = Device.CMREndLBA + 1 + Math.Ceiling(107374182400 / Device.SectorLength)}
                datapartition.MaxLength = (Device.MaximumLBA - datapartition.StartLBA + 1) * Device.SectorLength
                DataStreamList.Add(datapartition)
            End If
        End Sub

        Public Function GetSerializedText() As String
            Dim writer As New System.Xml.Serialization.XmlSerializer(GetType(ZBCDataHelper))
            Dim sb As New System.Text.StringBuilder
            Dim t As New IO.StringWriter(sb)
            writer.Serialize(t, Me)
            Return sb.ToString()
        End Function

        Public Shared Function FromXML(s As String) As ZBCDataHelper
            Dim reader As New System.Xml.Serialization.XmlSerializer(GetType(ZBCDataHelper))
            Dim t As IO.TextReader = New IO.StringReader(s)
            Return CType(reader.Deserialize(t), ZBCDataHelper)
        End Function

    End Class
End Class
Public Class ExplorerUtils
    Implements IComparer(Of String)
    Declare Unicode Function StrCmpLogicalW Lib "shlwapi.dll" (ByVal s1 As String, ByVal s2 As String) As Int32

    Public Function Compare(ByVal x As String, ByVal y As String) As Integer _
        Implements System.Collections.Generic.IComparer(Of String).Compare
        Return StrCmpLogicalW(x, y)
    End Function
End Class


Public Module ByteFormatter
    Public Enum Style
        SI
        IEC
        JEDEC
    End Enum

    Public Function FormatBytes(bytes As Long,
                                Optional decimals As Integer = 2,
                                Optional style As Style = Style.IEC,
                                Optional culture As CultureInfo = Nothing) As String
        If culture Is Nothing Then culture = CultureInfo.CurrentCulture

        If bytes = 0L Then
            Return "0 B"
        End If

        Dim negative As Boolean = (bytes < 0)
        Dim value As Double = Math.Abs(CDbl(bytes))

        Dim unitBase As Double
        Dim units As String()

        Select Case style
            Case Style.SI
                unitBase = 1000.0
                units = New String() {"B", "kB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"}
            Case Style.IEC
                unitBase = 1024.0
                units = New String() {"B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB"}
            Case Else
                unitBase = 1024.0
                units = New String() {"B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"}
        End Select

        Dim unitIndex = 0
        While unitIndex < units.Length - 1 AndAlso value >= unitBase
            value /= unitBase
            unitIndex += 1
        End While

        Dim decimalsToUse As Integer = If(unitIndex = 0, 0, decimals)

        If negative Then value = -value

        Return String.Format(culture, "{0} {1}",
                             value.ToString("N" & decimalsToUse, culture),
                             units(unitIndex))
    End Function
End Module

Public Class BigEndianConverter
    Public Shared Function ToUInt16(data As Byte(), Optional ByVal StartIndex As Integer = 0) As UInt32
        Dim result As UInt32
        For i As Integer = StartIndex To StartIndex + 1
            result <<= 8
            If i < data.Length Then result = result Or data(i)
        Next
        Return result
    End Function
    Public Shared Function ToUInt32(data As Byte(), Optional ByVal StartIndex As Integer = 0) As UInt32
        Dim result As UInt32
        For i As Integer = StartIndex To StartIndex + 3
            result <<= 8
            If i < data.Length Then result = result Or data(i)
        Next
        Return result
    End Function
    Public Shared Function ToUInt64(data As Byte(), Optional ByVal StartIndex As Integer = 0) As UInt32
        Dim result As UInt32
        For i As Integer = StartIndex To StartIndex + 7
            result <<= 8
            If i < data.Length Then result = result Or data(i)
        Next
        Return result
    End Function
    Public Shared Function GetBytes(ByVal input As UInt64) As Byte()
        Dim result(7) As Byte
        For i As Integer = result.Length - 1 To 0 Step -1
            result(i) = CByte(input And &HFF)
            input >>= 8
        Next
        Return result
    End Function
    Public Shared Function GetBytes(ByVal input As UInt32) As Byte()
        Dim result(3) As Byte
        For i As Integer = result.Length - 1 To 0 Step -1
            result(i) = CByte(input And &HFF)
            input >>= 8
        Next
        Return result
    End Function
    Public Shared Function GetBytes(ByVal input As UInt16) As Byte()
        Dim result(1) As Byte
        For i As Integer = result.Length - 1 To 0 Step -1
            result(i) = CByte(input And &HFF)
            input >>= 8
        Next
        Return result
    End Function
End Class