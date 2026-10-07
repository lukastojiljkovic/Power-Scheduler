; Inno Setup script for Pwrschdlr. Built by build.ps1 from the self-contained publish output.

#ifndef AppVersion
  #define AppVersion "1.1.1"
#endif

#define AppName "Pwrschdlr"
#define AppExeName "Pwrschdlr.exe"
#define AppPublisher "Luka Stojiljkovic"
#define AppUrl "https://github.com/lukastojiljkovic/Power-Scheduler"
#define PublishDir "..\artifacts\publish\win-x64"

[Setup]
AppId={{1D6EED68-DC50-4D83-AC90-E13E53528EEA}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DisableDirPage=auto
DisableProgramGroupPage=yes
; Users must accept the Terms of Use, which also cover the redistributed Microsoft components.
LicenseFile=..\TERMS.md
OutputDir=..\artifacts\installer
OutputBaseFilename={#AppName}-{#AppVersion}-Setup
SetupIconFile=..\src\Pwrschdlr\Assets\Pwrschdlr.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
; Pwrschdlr never needs administrator rights, and its timer belongs to one user, so it installs for that user only,
; under %LOCALAPPDATA%\Programs, without a UAC prompt.
PrivilegesRequired=lowest
CloseApplications=yes
CloseApplicationsFilter={#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion
Source: "..\TERMS.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PRIVACY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Cancels a running timer by removing its scheduled task, and removes the settings.
Filename: "{app}\{#AppExeName}"; Parameters: "--uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveUserData"

[UninstallDelete]
; An update installer that was downloaded but never run (PRIVACY.md).
Type: filesandordirs; Name: "{localappdata}\{#AppName}\Updates"
Type: dirifempty; Name: "{localappdata}\{#AppName}"
