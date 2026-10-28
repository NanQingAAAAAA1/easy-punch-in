; PunchPal / 打卡小护卫 安装包脚本（Inno Setup）
; 用法：用 Inno Setup 打开本文件，点击 Compile。
; 发布前请先执行：dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
; 并将生成的 PunchPal.exe 放到本脚本同级的 ..\publish\win-x64\ 目录。

#define MyAppName "打卡小护卫 (PunchPal)"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "EasyPunchIn"
#define MyAppExeName "PunchPal.exe"

[Setup]
AppId={{A1B2C3D4-0000-1111-2222-333344445555}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\dist
OutputBaseFilename=PunchPal-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest

[Languages]
Name: "chinese"; MessagesFile: "compiler:ChineseSimplified.isl"

[Files]
Source: "..\publish\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式:"

[Registry]
; 开机自启（当前用户 HKCU，无需管理员权限）
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "PunchPal"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue
