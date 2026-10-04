; =============================================================================
; Inno Setup 6 Script for NetSpeedWidget
; Per-user installation — NO administrator privileges required
; =============================================================================

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName        "NetSpeedWidget"
#define MyAppPublisher   "NetSpeedWidget"
#define MyAppDescription "Lightweight real-time Internet speed monitoring and network diagnostics for Windows."
#define MyAppURL         "https://github.com/netspeedwidget/netspeedwidget"
#define MyAppExeName     "NetSpeedWidget.exe"
#define MyAppMutex       "NetSpeedWidget_SingleInstance_Mutex"

; =============================================================================
; [Setup] — Core Installer Configuration
; =============================================================================
[Setup]
AppId={{5E65D7DE-7C15-4D56-8A61-2A5F89BEA201}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppContact={#MyAppURL}
AppComments={#MyAppDescription}

; Per-user install — no UAC elevation
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline

; Default install location under %LOCALAPPDATA%\Programs
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}

; Disable the program group page (always create Start Menu shortcuts)
DisableProgramGroupPage=yes

; Output
OutputDir=..\artifacts\dist
OutputBaseFilename=NetSpeedWidget-Setup-{#MyAppVersion}

; Branding
SetupIconFile=..\src\NetSpeedWidget\Resources\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}

; Compression
Compression=lzma2/ultra64
SolidCompression=yes

; UI
WizardStyle=modern
WizardSizePercent=110,110

; Architecture
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

; Upgrade & process handling
AppMutex={#MyAppMutex}
CloseApplications=force
CloseApplicationsFilter=*.exe
RestartApplications=no

; Misc
AllowNoIcons=yes
ShowLanguageDialog=no
MinVersion=10.0.17763

; Version info for the setup EXE itself
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppDescription}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

; =============================================================================
; [Languages]
; =============================================================================
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; =============================================================================
; [Messages] — Custom installer text
; =============================================================================
[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nNo administrator privileges are required.%n%nIt is recommended that you close all other applications before continuing.
FinishedLabel=Setup has finished installing [name] on your computer. The application may be launched from the Start Menu or Desktop shortcut.

; =============================================================================
; [Tasks] — User-selectable options
; =============================================================================
[Tasks]
Name: "startmenu";      Description: "Create a &Start Menu shortcut";                      GroupDescription: "Shortcuts:";            Flags: checkedonce
Name: "desktopicon";     Description: "Create a &Desktop shortcut";                          GroupDescription: "Shortcuts:";            Flags: checkedonce
Name: "startwithwindows"; Description: "Start {#MyAppName} automatically when &Windows starts"; GroupDescription: "Windows Integration:"; Flags: unchecked

; =============================================================================
; [Files] — Application files from publish output
; =============================================================================
[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Copy the icon to the install dir for shortcut references
Source: "..\src\NetSpeedWidget\Resources\app.ico"; DestDir: "{app}"; Flags: ignoreversion

; =============================================================================
; [Icons] — Start Menu and Desktop shortcuts
; =============================================================================
[Icons]
; Start Menu — Application
Name: "{userprograms}\{#MyAppName}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: startmenu
; Start Menu — Uninstall
Name: "{userprograms}\{#MyAppName}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"; Tasks: startmenu
; Desktop
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

; =============================================================================
; [Registry] — Start with Windows (per-user HKCU)
; =============================================================================
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startwithwindows

; =============================================================================
; [Run] — Post-install launch
; =============================================================================
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

; =============================================================================
; [UninstallDelete] — Cleanup installer-created files (NOT user data)
; =============================================================================
[UninstallDelete]
Type: filesandordirs; Name: "{app}"

; =============================================================================
; [Code] — Pascal Script for upgrade detection and optional data cleanup
; =============================================================================
[Code]

function IsAppRunning(): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if Exec('tasklist.exe', '/FI "IMAGENAME eq {#MyAppExeName}" /NH', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    // tasklist returns 0 whether or not process is found; we check via FindWindowByClassName or similar
    // Simplified: use mutex check
    Result := CheckForMutexes('{#MyAppMutex}');
  end;
end;

procedure CloseRunningApp();
var
  ResultCode: Integer;
begin
  // Attempt graceful WM_CLOSE via taskkill (no /F)
  Exec('taskkill.exe', '/IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(1500);
  // Force kill if still running
  if CheckForMutexes('{#MyAppMutex}') then
  begin
    Exec('taskkill.exe', '/F /IM {#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(500);
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  // Close any running instance before proceeding
  if CheckForMutexes('{#MyAppMutex}') then
  begin
    if MsgBox('{#MyAppName} is currently running. Setup will close it to continue.' + Chr(13) + Chr(10) + Chr(13) + Chr(10) + 'Continue?',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      CloseRunningApp();
    end
    else
    begin
      Result := False;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  UserDataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    UserDataDir := ExpandConstant('{localappdata}\{#MyAppName}');
    if DirExists(UserDataDir) then
    begin
      if MsgBox('Do you want to remove your settings and history data?' + Chr(13) + Chr(10) +
                Chr(13) + Chr(10) + 'Location: ' + UserDataDir + Chr(13) + Chr(10) +
                Chr(13) + Chr(10) + 'Click "Yes" to remove all data, or "No" to keep it.',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(UserDataDir, True, True, True);
      end;
    end;
  end;
end;
