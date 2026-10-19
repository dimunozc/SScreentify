; Inno Setup 6 script for SScreentify. Build it with scripts\build-installer.ps1, which publishes the app first
; and passes AppVersion (from SScreentify.App.csproj) and PublishDir.

#ifndef AppVersion
  #error Run scripts\build-installer.ps1 instead of compiling this file directly.
#endif

[Setup]
; Never change AppId: Windows uses it to recognise upgrades and the uninstaller.
AppId={{CCB76E51-9533-4AD8-BA36-FAA913C8173D}
AppName=SScreentify
AppVersion={#AppVersion}
AppVerName=SScreentify {#AppVersion}
AppPublisher=Diego Muñoz
AppCopyright=Copyright (c) 2026 Diego Muñoz
; Per-user install: no administrator rights, files under %LOCALAPPDATA%\Programs.
PrivilegesRequired=lowest
DefaultDirName={autopf}\SScreentify
DisableProgramGroupPage=yes
DisableDirPage=auto
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.18362
OutputDir=..\dist
OutputBaseFilename=SScreentify-Setup-{#AppVersion}
SetupIconFile=..\src\SScreentify.App\Assets\app.ico
UninstallDisplayIcon={app}\SScreentify.exe
LicenseFile=..\LICENSE
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
; Closes a running SScreentify before replacing its files.
CloseApplications=force
RestartApplications=no

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
Source: "{#PublishDir}\SScreentify.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\SScreentify"; Filename: "{app}\SScreentify.exe"

[Registry]
; Created by the app's "Start with Windows" option, never by the installer; removed on uninstall so no dead entry stays.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "SScreentify"; ValueType: none; Flags: uninsdeletevalue

[Run]
Filename: "{app}\SScreentify.exe"; Description: "{cm:LaunchProgram,SScreentify}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; The app lives in the tray, so make sure it is not running while its files are removed.
Filename: "{sys}\taskkill.exe"; Parameters: "/IM SScreentify.exe /F"; Flags: runhidden; RunOnceId: "StopSScreentify"

; Settings in %APPDATA%\SScreentify and saved screenshots are kept on purpose.
