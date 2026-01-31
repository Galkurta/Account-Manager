

#define MyAppName "Roblox Account Manager"
#define MyAppVersion "1.4.0"
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
  AppId: String;
begin
  AppId := '{C6E66095-6834-4474-87E6-DBC48FF96C49}';
  Result := False;

  // Check HKCU (Current User - Most likely due to PrivilegesRequired=lowest)
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + AppId + '_is1', 'UninstallString', S) then begin
    UninstallPath := RemoveQuotes(S);
    Result := True;
  end
  // Check HKLM (32-bit View - Default for non-64bit mode installer)
  else if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + AppId + '_is1', 'UninstallString', S) then begin
    UninstallPath := RemoveQuotes(S);
    Result := True;
  end
  // Check HKLM64 (64-bit View - Explicit check)
  else if RegQueryStringValue(HKLM64, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + AppId + '_is1', 'UninstallString', S) then begin
    UninstallPath := RemoveQuotes(S);
    Result := True;
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
  FindRec: TFindRec;
begin
  Result := False;
  RegKey := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  
  // 1. Registry Check (Simple explicit checks for common base versions)
  if RegQueryStringValue(HKLM, RegKey, '8.0.0', Version) or 
     RegKeyExists(HKLM, RegKey + '\8.0.0') or
     RegKeyExists(HKLM, RegKey + '\8.0') then
  begin
    Result := True;
    Exit;
  end;
  
  // 2. Registry Check for Uninstall key (Fallback)
  if RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{7C7A6560-CD00-4786-9040-51543306B620}') then
  begin
      Result := True;
      Exit;
  end;

  // 3. Filesystem Check (Robust Wildcard Search for 8.0.*)
  if FindFirst(ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.0.*'), FindRec) then begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then begin
          if (FindRec.Name <> '.') and (FindRec.Name <> '..') then begin
            // Found a folder starting with 8.0, assume installed
            Result := True;
            Break;
          end;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
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
