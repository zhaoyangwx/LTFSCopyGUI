#define MyAppName "LTFSCopyGUI"
#define MyAppVersion "3.6.0"
#define MyAppPublisher "Nullpinter"
#define MyAppExeName "LTFSCopyGUI.exe"

[Setup]
AppId={{DBC4887A-8A7A-11F1-BAE5-325096B39F47}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName=C:\{#MyAppName}
OutputDir=.
OutputBaseFilename={#MyAppName}-{#MyAppVersion}
SetupIconFile=Koko.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern dynamic windows11 includetitlebar
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
chinesesimp.OverwriteOptionalFiles=覆盖已有可选文件
english.OverwriteOptionalFiles=Overwrite existing optional files

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce
Name: "overwriteoptional"; Description: "{cm:OverwriteOptionalFiles}"; Flags: unchecked

[Files]
; 主程序构建产物始终更新，并保持 Release 目录的子目录结构。
Source: "..\LTFSCopyGUI\bin\x64\Release\*"; DestDir: "{app}"; Excludes: "\LtfsCommand.dll,\LtfsCommand.pdb,\config\*,\log\*,\logpages\*,\schema\*"; Flags: ignoreversion recursesubdirs createallsubdirs

; deploy 中的文件属于可选文件。默认只安装目标位置中不存在的文件；
; 选中“覆盖已有可选文件”后才覆盖同名文件。
Source: "deploy\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: OverwriteOptionalFiles
Source: "deploy\*"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist recursesubdirs createallsubdirs; Check: not OverwriteOptionalFiles

[Icons]
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[Code]
function OverwriteOptionalFiles: Boolean;
begin
  Result := WizardIsTaskSelected('overwriteoptional');
end;
