; ============================================================
; Inno Setup Script — Missed GitHub Updates
; Builds: MissedGitHubUpdates-Setup.exe
; ============================================================

#define AppName      "Missed GitHub Updates"
#define AppVersion   "1.0.0"
#define AppPublisher "Prem Patil"
#define AppURL       "https://github.com/prempatil03/missed-github-updates"
#define AppExeName   "MissedGitHubUpdates.exe"
#define SourceDir    "..\MissedGitHubUpdates\bin\Release\net9.0-windows10.0.17763.0\win-x64\publish"
#define IconFile     "..\MissedGitHubUpdates\Assets\LOGO.ico"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=output
OutputBaseFilename=MissedGitHubUpdates-Setup
SetupIconFile={#IconFile}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

; Minimum Windows 10
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Ask user if they want the app to start with Windows
Name: "startupentry"; Description: "Start {#AppName} automatically when Windows starts"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
; The single self-contained exe
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; App icon
Source: "{#IconFile}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start Menu shortcut
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\LOGO.ico"

; Desktop shortcut
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\LOGO.ico"

; Uninstall shortcut in Start Menu
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

[Registry]
; Add to Windows startup if user ticked the checkbox
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startupentry

[Run]
; Launch the app after install finishes (optional)
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Make sure the app is closed before uninstall
Filename: "taskkill"; Parameters: "/F /IM {#AppExeName}"; Flags: runhidden; RunOnceId: "KillApp"
