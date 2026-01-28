

#define MyAppName "Roblox Account Manager"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Galkurta"
#define MyAppURL "https://github.com/Galkurta/Account-Manager"
#define MyAppExeName "RobloxAccountManager.exe"

[Setup]

AppId={{C6E66095-6834-4474-87E6-DBC48FF96C49}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}

AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes

PrivilegesRequired=lowest
OutputDir=Installer
OutputBaseFilename=RobloxAccountManager_Setup
SetupIconFile=RobloxAccountManager\Resources\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]

Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs


[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  UninstallPage: TInputOptionWizardPage;
  UninstallFound: Boolean;
  UninstallPath: String;


function CheckForPreviousVersion: Boolean;
var
  S: String;
begin

  if RegQueryStringValue(HKLM64, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{C6E66095-6834-4474-87E6-DBC48FF96C49}_is1', 'UninstallString', S) then begin
    UninstallPath := RemoveQuotes(S);
    Result := True;
  end

  else if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{C6E66095-6834-4474-87E6-DBC48FF96C49}_is1', 'UninstallString', S) then begin
    UninstallPath := RemoveQuotes(S);
    Result := True;
  end else begin
    Result := False;
  end;
end;

procedure InitializeWizard;
begin
  UninstallFound := CheckForPreviousVersion();

  if UninstallFound then begin
    UninstallPage := CreateInputOptionPage(wpWelcome,
      'Already Installed', 'Choose how you want to install Roblox Account Manager.',
      'An older version of Roblox Account Manager is installed on your system. It is recommended that you uninstall the current version before installing.' + #13#10 + #13#10 +
      'Select the operation you want to perform and click Next to continue.',
      True, False);

    UninstallPage.Add('Uninstall before installing');
    UninstallPage.Add('Do not uninstall (Overwrite)');
    

    UninstallPage.SelectedValueIndex := 0;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if (CurStep = ssInstall) and UninstallFound and (UninstallPage.SelectedValueIndex = 0) then begin

    Exec(UninstallPath, '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
  end;
end;

function IsDotNet8DesktopInstalled(): Boolean;
var
  RegKey: string;
  Version: string;
begin
  Result := False;
  RegKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  
  if RegQueryStringValue(HKLM, RegKey, '8.0.0', Version) or 
     RegKeyExists(HKLM, RegKey + '\8.0.0') or
     RegKeyExists(HKLM, RegKey + '\8.0') then
  begin
    Result := True;
  end;
  
  if not Result then begin
      Result := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{7C7A6560-CD00-4786-9040-51543306B620}')
             or DirExists(ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.0')); 
  end;
  

  if not Result then
    Result := DirExists(ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.0')) or
              DirExists(ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.1')) or
              DirExists(ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.2')) or
              DirExists(ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.3')) or
              DirExists(ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.4'));
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin

  if not IsDotNet8DesktopInstalled() then
  begin
    if MsgBox('It looks like .NET 8 Desktop Runtime is missing.' + #13#10 + 
              'The application requires it to run.' + #13#10 + #13#10 +
              'Do you want to download it now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0', '', '', SW_SHOW, ewNoWait, ErrorCode);
    end;

  end;

  Result := True;
end;
