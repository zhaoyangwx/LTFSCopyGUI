Imports System.IO
Imports System.Security.Claims
Imports System.Threading
Imports FubarDev.FtpServer
Imports FubarDev.FtpServer.AccountManagement
Imports FubarDev.FtpServer.BackgroundTransfer
Imports FubarDev.FtpServer.FileSystem
Imports FubarDev.FtpServer.FileSystem.Unix
Imports Microsoft.Extensions.DependencyInjection

Public Class FTPService
    Public TapeDrive As String
    Public BlockSize As Integer = 524288
    Public ExtraPartitionCount As Integer = 1
    Public port As Integer
    Public schema As ltfsindex
    Public Services As ServiceCollection
    Public ftpServerHost As IFtpServerHost

    Public Event LogPrint(s As String)
    Public MustInherit Class LTFSFileSystemEntry
        Implements IUnixFileSystemEntry
        Public AccMode As New Generic.GenericAccessMode(True, False, True)
        Public ReadOnly Property FileInfo As ltfsindex.file
        Public ReadOnly Property DirectoryInfo As ltfsindex.directory
        Public ReadOnly Property Info
            Get
                If FileInfo IsNot Nothing Then Return FileInfo
                If DirectoryInfo IsNot Nothing Then Return DirectoryInfo
                Return Nothing
            End Get
        End Property
        Public Sub New()

        End Sub
        Public Sub New(fsInfo As ltfsindex.file)
            FileInfo = fsInfo
            Try
                CreatedTime = New DateTimeOffset(ParseTimeStamp(fsInfo.creationtime))
                LastWriteTime = New DateTimeOffset(ParseTimeStamp(fsInfo.changetime))
            Catch ex As Exception

            End Try
            Name = fsInfo.name
        End Sub
        Public Sub New(fsInfo As ltfsindex.directory)
            DirectoryInfo = fsInfo
            Try
                CreatedTime = New DateTimeOffset(ParseTimeStamp(fsInfo.creationtime))
                LastWriteTime = New DateTimeOffset(ParseTimeStamp(fsInfo.changetime))
            Catch ex As Exception

            End Try
            Name = fsInfo.name
        End Sub
        Public ReadOnly Property Name As String Implements IUnixFileSystemEntry.Name

        Public ReadOnly Property Permissions As IUnixPermissions Implements IUnixFileSystemEntry.Permissions
            Get
                Return New Generic.GenericUnixPermissions(AccMode, AccMode, AccMode)
            End Get
        End Property
        Public ReadOnly Property LastWriteTime As DateTimeOffset? Implements IUnixFileSystemEntry.LastWriteTime
        Public ReadOnly Property CreatedTime As DateTimeOffset? Implements IUnixFileSystemEntry.CreatedTime

        Public ReadOnly Property NumberOfLinks As Long Implements IUnixFileSystemEntry.NumberOfLinks
            Get
                Return 1
            End Get
        End Property

        Public ReadOnly Property Owner As String Implements IUnixOwner.Owner
            Get
                Return "LTFSUser"
            End Get
        End Property

        Public ReadOnly Property Group As String Implements IUnixOwner.Group
            Get
                Return "LTFSUsers"
            End Get
        End Property
    End Class
    Public Class LTFSFileEntry
        Inherits LTFSFileSystemEntry
        Implements IUnixFileEntry
        Public Sub New(info As ltfsindex.file)
            MyBase.New(info)
            FileInfo = info
        End Sub
        Public ReadOnly Property FileInfo As ltfsindex.file
        Public ReadOnly Property Size As Long Implements IUnixFileEntry.Size
            Get
                Return FileInfo.length
            End Get
        End Property
    End Class
    Public Class LTFSDirectoryEntry
        Inherits LTFSFileSystemEntry
        Implements IUnixDirectoryEntry
        Public Sub New(dirInfo As ltfsindex.directory, _isRoot As Boolean)
            MyBase.New(dirInfo)
            IsRoot = _isRoot
            DirectoryInfo = dirInfo
        End Sub
        Public ReadOnly Property IsRoot As Boolean Implements IUnixDirectoryEntry.IsRoot
        Public ReadOnly Property DirectoryInfo As ltfsindex.directory
        Public ReadOnly Property IsDeletable As Boolean Implements IUnixDirectoryEntry.IsDeletable
            Get
                Return False
            End Get
        End Property
        Public Shared Function HasChildEntries(directoryInfo As ltfsindex.directory) As Boolean
            If directoryInfo.contents._directory IsNot Nothing AndAlso directoryInfo.contents._directory.Count > 0 Then Return True
            If directoryInfo.contents._file IsNot Nothing AndAlso directoryInfo.contents._file.Count > 0 Then Return True
            Return False
        End Function
        Private Function CheckIfDeletable() As Boolean
            Return False
        End Function
    End Class
    Public Class LTFSFileSystem
        Implements IUnixFileSystem
        Public Property TapeDrive As String
        Public Property BlockSize As Integer = 524288
        Public Property ExtraPartitionCount As Integer = 1
        Public Event LogPrint(s As String)
        Public Sub New(rootPath As ltfsindex.directory, ByVal drive As String, ByVal blksize As Integer, ByVal _extraPartitionCount As Integer, Optional ByVal LogHandler As Action(Of String) = Nothing)
            FileSystemEntryComparer = StringComparer.OrdinalIgnoreCase
            Root = New LTFSDirectoryEntry(rootPath, True)
            TapeDrive = drive
            BlockSize = blksize
            ExtraPartitionCount = _extraPartitionCount
            If LogHandler IsNot Nothing Then AddHandler LogPrint,
                Sub(s As String)
                    LogHandler(s)
                End Sub
        End Sub

        Public ReadOnly Property SupportsAppend As Boolean Implements IUnixFileSystem.SupportsAppend
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property SupportsNonEmptyDirectoryDelete As Boolean Implements IUnixFileSystem.SupportsNonEmptyDirectoryDelete
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property FileSystemEntryComparer As StringComparer Implements IUnixFileSystem.FileSystemEntryComparer
        Public ReadOnly Property Root As IUnixDirectoryEntry Implements IUnixFileSystem.Root

        Public Function GetEntriesAsync(directoryEntry As IUnixDirectoryEntry, cancellationToken As CancellationToken) As Task(Of IReadOnlyList(Of IUnixFileSystemEntry)) Implements IUnixFileSystem.GetEntriesAsync
            Dim result As New List(Of IUnixFileSystemEntry)
            Dim searchDirInfo As ltfsindex.directory = CType(directoryEntry, LTFSDirectoryEntry).DirectoryInfo
            For Each d As ltfsindex.directory In searchDirInfo.contents._directory
                result.Add(New LTFSDirectoryEntry(d, False))
            Next
            For Each f As ltfsindex.file In searchDirInfo.contents._file
                result.Add(New LTFSFileEntry(f))
            Next
            Return Task.FromResult(Of IReadOnlyList(Of IUnixFileSystemEntry))(result)
        End Function

        Public Function GetEntryByNameAsync(directoryEntry As IUnixDirectoryEntry, name As String, cancellationToken As CancellationToken) As Task(Of IUnixFileSystemEntry) Implements IUnixFileSystem.GetEntryByNameAsync
            Dim searchDirInfo As ltfsindex.directory = CType(directoryEntry, LTFSDirectoryEntry).DirectoryInfo
            Dim result As IUnixFileSystemEntry = Nothing
            For Each f As ltfsindex.file In searchDirInfo.contents._file
                If f.name.ToLower().Equals(name.ToLower()) Then
                    result = New LTFSFileEntry(f)
                End If
            Next
            If result Is Nothing Then
                For Each d As ltfsindex.directory In searchDirInfo.contents._directory
                    If d.name.ToLower().Equals(name.ToLower()) Then
                        result = New LTFSDirectoryEntry(d, False)
                    End If
                Next
            End If
            Return Task.FromResult(result)
        End Function

        Public Function MoveAsync(parent As IUnixDirectoryEntry, source As IUnixFileSystemEntry, target As IUnixDirectoryEntry, fileName As String, cancellationToken As CancellationToken) As Task(Of IUnixFileSystemEntry) Implements IUnixFileSystem.MoveAsync
            Throw New NotImplementedException()
        End Function

        Public Function UnlinkAsync(entry As IUnixFileSystemEntry, cancellationToken As CancellationToken) As Task Implements IUnixFileSystem.UnlinkAsync
            Throw New NotImplementedException()
        End Function

        Public Function CreateDirectoryAsync(targetDirectory As IUnixDirectoryEntry, directoryName As String, cancellationToken As CancellationToken) As Task(Of IUnixDirectoryEntry) Implements IUnixFileSystem.CreateDirectoryAsync
            Throw New NotImplementedException()
        End Function

        Public Function OpenReadAsync(fileEntry As IUnixFileEntry, startPosition As Long, cancellationToken As CancellationToken) As Task(Of Stream) Implements IUnixFileSystem.OpenReadAsync
            Dim fileInfo As ltfsindex.file = CType(fileEntry, LTFSFileEntry).FileInfo
            RaiseEvent LogPrint($"OpenReadAsync file={fileInfo.name} position={startPosition}")
            Dim input As New IOManager.LTFSFileStream(fileInfo, TapeDrive, BlockSize, ExtraPartitionCount)
            AddHandler input.LogPrint, Sub(s As String)
                                           RaiseEvent LogPrint(s)
                                       End Sub
            Dim rstream As New BufferedStream(input, TapeUtils.GlobalBlockLimit)
            rstream.Seek(startPosition, SeekOrigin.Begin)
            Return Task.FromResult(Of Stream)(rstream)
        End Function

        Public Function AppendAsync(fileEntry As IUnixFileEntry, startPosition As Long?, data As Stream, cancellationToken As CancellationToken) As Task(Of IBackgroundTransfer) Implements IUnixFileSystem.AppendAsync
            Throw New NotImplementedException()
        End Function

        Public Function CreateAsync(targetDirectory As IUnixDirectoryEntry, fileName As String, data As Stream, cancellationToken As CancellationToken) As Task(Of IBackgroundTransfer) Implements IUnixFileSystem.CreateAsync
            Throw New NotImplementedException()
        End Function

        Public Function ReplaceAsync(fileEntry As IUnixFileEntry, data As Stream, cancellationToken As CancellationToken) As Task(Of IBackgroundTransfer) Implements IUnixFileSystem.ReplaceAsync
            Throw New NotImplementedException()
        End Function

        Public Function SetMacTimeAsync(entry As IUnixFileSystemEntry, modify As DateTimeOffset?, access As DateTimeOffset?, create As DateTimeOffset?, cancellationToken As CancellationToken) As Task(Of IUnixFileSystemEntry) Implements IUnixFileSystem.SetMacTimeAsync
            Throw New NotImplementedException()
        End Function
    End Class
    Public Class LTFSFileSystemProvider
        Implements IFileSystemClassFactory
        Private ReadOnly Property _root As ltfsindex.directory
        Private ReadOnly Property _drive As String
        Private ReadOnly Property _blocksize As Integer
        Private ReadOnly Property _extraPartitionCount As Integer
        Public ReadOnly Property LogHandler As Action(Of String)
        Public Sub New(options As Microsoft.Extensions.Options.IOptions(Of LTFSFileSystemOptions))
            _root = options.Value.Root
            _drive = options.Value.TapeDrive
            _blocksize = options.Value.BlockSize
            _extraPartitionCount = options.Value.ExtraPartitionCount
            LogHandler = options.Value.LogHandler
        End Sub
        Public Function Create(accountInformation As IAccountInformation) As Task(Of IUnixFileSystem) Implements IFileSystemClassFactory.Create
            Return Task.FromResult(Of IUnixFileSystem)(New LTFSFileSystem(_root, _drive, _blocksize, _extraPartitionCount, LogHandler))
        End Function
    End Class
    Public Class LTFSFileSystemOptions
        Public Property Root As ltfsindex.directory
        Public Property TapeDrive As String
        Public Property BlockSize As Integer
        Public Property ExtraPartitionCount As Integer
        Public Property LogHandler As Action(Of String)
    End Class
    Public Shared Function UseLTFSFileSystem(builder As IFtpServerBuilder) As IFtpServerBuilder
        builder.Services.AddSingleton(Of IFileSystemClassFactory, LTFSFileSystemProvider)()
        Return builder
    End Function
    Public Class NoPasswdMembershipProvider
        Implements AccountManagement.IMembershipProviderAsync
        Public Shared Function CreateAnonymousPrincipal(email As String) As ClaimsPrincipal
            Dim anonymousClaims As List(Of Claim) =
               {New Claim(ClaimsIdentity.DefaultNameClaimType, "anonymous"),
                New Claim(ClaimTypes.Anonymous, String.Empty),
                New Claim(ClaimsIdentity.DefaultRoleClaimType, "anonymous"),
                New Claim(ClaimsIdentity.DefaultRoleClaimType, "guest"),
                New Claim(ClaimTypes.AuthenticationMethod, "anonymous")}.ToList()
            If (Not String.IsNullOrWhiteSpace(email)) Then anonymousClaims.Add(New Claim(ClaimTypes.Email, email, ClaimValueTypes.Email))
            Dim identity As New ClaimsIdentity(anonymousClaims, "anonymous")
            Dim principal As New ClaimsPrincipal(identity)
            Return principal
        End Function
        Public Function ValidateUserAsync(username As String, password As String, Optional cancellationToken As CancellationToken = Nothing) As Task(Of MemberValidationResult) Implements IMembershipProviderAsync.ValidateUserAsync
            Return Task.FromResult(New MemberValidationResult(MemberValidationStatus.Anonymous, CreateAnonymousPrincipal(password)))
        End Function

        Public Function ValidateUserAsync(username As String, password As String) As Task(Of MemberValidationResult) Implements IMembershipProvider.ValidateUserAsync
            Return ValidateUserAsync(username, password, CancellationToken.None)
        End Function

        Public Function LogOutAsync(principal As ClaimsPrincipal, Optional cancellationToken As CancellationToken = Nothing) As Task Implements IMembershipProviderAsync.LogOutAsync
            Return Task.CompletedTask
        End Function
    End Class
    Public Sub StartService()
            If schema Is Nothing Then Exit Sub
            Services = New ServiceCollection()
            Services.Configure(
            Sub(opt As LTFSFileSystemOptions)
                opt.Root = schema._directory(0)
                opt.TapeDrive = TapeDrive
                opt.BlockSize = BlockSize
                opt.ExtraPartitionCount = ExtraPartitionCount
                opt.LogHandler = Sub(s As String)
                                     RaiseEvent LogPrint(s)
                                 End Sub
            End Sub)

            Services.AddFtpServer(
            Sub(builder As IFtpServerBuilder)
                UseLTFSFileSystem(builder)
                builder.Services.AddSingleton(Of IMembershipProvider, NoPasswdMembershipProvider)()
            End Sub)


        Services.Configure(
            Sub(opt As FtpServerOptions)
                opt.ServerAddress = "0.0.0.0"
                opt.Port = port
                opt.ConnectionInactivityCheckInterval = New TimeSpan(1, 0, 0)
            End Sub)

        Services.Configure(
            Sub(opt As FtpConnectionOptions)
                opt.DefaultEncoding = Text.Encoding.UTF8
                opt.InactivityTimeout = New TimeSpan(6, 0, 0)
            End Sub)

        With Services.BuildServiceProvider
                ftpServerHost = .GetRequiredService(Of IFtpServerHost)
                ftpServerHost.StartAsync(Threading.CancellationToken.None)
            End With
        End Sub

    Public Sub StopService()
        ftpServerHost.StopAsync(Threading.CancellationToken.None).Wait()
    End Sub
    Public Shared Function ParseTimeStamp(t As String) As Date
        'yyyy-MM-ddTHH:mm:ss.fffffff00Z
        Try
            Return Date.ParseExact(t, "yyyy-MM-ddTHH:mm:ss.fffffff00Z", Globalization.CultureInfo.InvariantCulture)
        Catch ex As Exception
            Return New Date()
        End Try
    End Function

End Class
