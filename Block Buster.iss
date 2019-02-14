#define MyAppName "Block Buster"
#define MyAppVersion "1.0"
#define MyAppPublisher "InanZen"
#define MyAppURL "http://inanzen.eu/"
#define MyAppExeName "Block Buster.exe"      

; XNA Game Studio
#define MyGameStudioLocation "C:\Program Files (x86)\Microsoft XNA\XNA Game Studio\v4.0"
#define XNARedist "xnafx40_redist.msi"
 
; .NET redistributable
#define MyRedistLocation "D:\Downloads"   
#define DotNetSetup "dotNetFx40_Client_x86_x64.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{B69AF612-DF70-4A60-AF6F-F523AD06CD25}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=C:\Users\Peter\Documents\Visual Studio 2010\Projects\Block Buster\Block Buster\Block Buster\bin\x86\Release\licence.txt
OutputBaseFilename=BlockBuster_Setup
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; XNA Framework redistributable
Source: {#MyGameStudioLocation}\Redist\XNA FX Redist\{#XNARedist}; DestDir: {tmp}          
; .NET redistributables
Source: {#MyRedistLocation}\{#DotNetSetup}; DestDir: {tmp}

Source: "C:\Users\Peter\Documents\Visual Studio 2010\Projects\Block Buster\Block Buster\Block Buster\bin\x86\Release\Block Buster.exe"; DestDir: "{app}"; Flags: ignoreversion     
Source: "C:\Users\Peter\Documents\Visual Studio 2010\Projects\Block Buster\Block Buster\Block Buster\bin\x86\Release\XNA_GUI_Controls.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Peter\Documents\Visual Studio 2010\Projects\Block Buster\Block Buster\Block Buster\bin\x86\Release\licence.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\Peter\Documents\Visual Studio 2010\Projects\Block Buster\Block Buster\Block Buster\bin\x86\Release\Content\*"; DestDir: "{app}\Content"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: "HKLM"; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers\"; ValueType: String; ValueName: "{app}\Block Buster.exe"; ValueData: "RUNASADMIN"; Flags: uninsdeletekeyifempty uninsdeletevalue;


[Run]                                      
Filename: {tmp}\{#DotNetSetup}; Flags: skipifdoesntexist; StatusMsg: "Installing required component: .NET Framework 4.0 Client."; Parameters: "/norestart /passive"; Check: CheckNetFramework
Filename: msiexec.exe; StatusMsg: "Installing required component: XNA Framework Redistributable 4.0 Refresh."; Parameters: "/qb /i ""{tmp}\{#XNARedist}"; Check: CheckXNAFramework
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNetDetected: boolean;
var
    key: string;
    install: cardinal;
    success: boolean;
 
begin
    WizardForm.StatusLabel.Caption := 'Checking for .Net Framework 4.0 Client.';
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client';
    success := RegQueryDWordValue(HKLM, key, 'Install', install);
    result := success and (install = 1);
end;
function CheckNetFramework: boolean;
begin
    if IsDotNetDetected then begin
        WizardForm.StatusLabel.Caption := '.Net Framework 4.0 Client detected.';
    end;
    result := not IsDotNetDetected;
end;
 
function IsXNAFrameworkDetected: Boolean;
var
    key: string;
    install: cardinal;
    success: boolean;
 
begin
    WizardForm.StatusLabel.Caption := 'Checking for XNA Framework Redistributable 4.0 Refresh.';
    if IsWin64 then begin
        key := 'SOFTWARE\Wow6432Node\Microsoft\XNA\Framework\v4.0';
    end else begin
        key := 'SOFTWARE\Microsoft\XNA\Framework\v4.0';
    end;
    success := RegQueryDWordValue(HKLM, key, 'Installed', install);
    result := success and (install = 1);
end;
 
function CheckXNAFramework: boolean;
begin
    if IsXNAFrameworkDetected then begin
        WizardForm.StatusLabel.Caption := 'XNA Framework Redistributable 4.0 Refresh detected.';
    end;
    result := not IsXNAFrameworkDetected;
end;