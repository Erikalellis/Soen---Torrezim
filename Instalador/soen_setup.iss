; SOEN - Instalador de Sistema (Inno Setup 6)
; IMPORTANTE: o banco de dados (soen.db) e os dados do usuário NÃO são
; instalados nem removidos pelo desinstalador — garantindo a preservação dos dados.

#define AppName "SOEN - Sistema de Ordem de Serviço"
#define AppVersion "1.0.0"
#define AppPublisher "SOEN / Erika Lellis"

[Setup]
AppId={{ECAED90F-40E8-400D-9C58-4B9125E821DE}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\SOEN - Torrezim
DefaultGroupName=SOEN
DisableProgramGroupPage=no
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=SoenInstaller
SetupIconFile=..\1562687-code-computer-creative-html-process-technology-web-development_107058.ico
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\Soen - Torrezim.exe

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho na Área de Trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "..\bin\Release\Soen - Torrezim.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\Soen - Torrezim.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\System.Data.SQLite.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\x86\*"; DestDir: "{app}\x86"; Flags: ignoreversion recursesubdirs
Source: "..\bin\Release\x64\*"; DestDir: "{app}\x64"; Flags: ignoreversion recursesubdirs
; O banco de dados (soen.db) NÃO é instalado: é criado no primeiro uso e NÃO é removido ao desinstalar.

[Icons]
Name: "{group}\SOEN"; Filename: "{app}\Soen - Torrezim.exe"
Name: "{group}\Desinstalar SOEN"; Filename: "{uninstallexe}"
Name: "{autodesktop}\SOEN"; Filename: "{app}\Soen - Torrezim.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Soen - Torrezim.exe"; Description: "Executar o SOEN agora"; Flags: nowait postinstall skipifsilent
Filename: "https://deepdarkness.com.br/"; Description: "Abrir o site Deep Darkness"; Flags: shellexec nowait postinstall skipifsilent unchecked

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    MsgBox('Os dados e o banco de dados (soen.db) NÃO serão removidos.' + #13#10 +
           'Eles continuarão disponíveis em: ' + ExpandConstant('{app}'), mbInformation, MB_OK);
end;