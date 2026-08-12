; SOEN - Instalador de Sistema (Inno Setup 6)
; IMPORTANTE: o banco de dados (soen.db) e os dados do usuário NÃO são
; instalados nem removidos pelo desinstalador — garantindo a preservação dos dados.

#define AppName "SOEN - Sistema de Ordem de Serviço"
#define AppVersion "1.1.3"
#define AppPublisher "SOEN / Erika Lellis"

[Setup]
AppId={{ECAED90F-40E8-400D-9C58-4B9125E821DE}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={commonappdata}\SOEN
DefaultGroupName=SOEN
DisableProgramGroupPage=no
PrivilegesRequired=admin
OutputDir=Output
OutputBaseFilename=SoenInstaller
SetupIconFile=..\1562687-code-computer-creative-html-process-technology-web-development_107058.ico
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\Soen - Torrezim.exe
Uninstallable=yes

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Components]
Name: "app"; Description: "Aplicativo SOEN"; Types: full compact custom; Flags: fixed
Name: "webapi"; Description: "SoenWebApi (WhatsApp) - servidor local de mensagens"; Types: full custom

[Types]
Name: "full"; Description: "Instalação completa"
Name: "compact"; Description: "Somente o aplicativo SOEN"
Name: "custom"; Description: "Personalizada"

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho na Área de Trabalho"; GroupDescription: "Atalhos:"

; Pasta do programa acessível para gravação: o SOEN cria o soen.db, logs e
; backups na própria pasta de instalação, inclusive para usuários comuns.
; A webapi também precisa gravar (data/, server.log, .env).
[Dirs]
Name: "{app}"; Permissions: users-modify
Name: "{app}\SoenWebApi"; Permissions: users-modify; Components: webapi

[Files]
Source: "..\bin\Release\Soen - Torrezim.exe"; DestDir: "{app}"; Flags: ignoreversion; Components: app
Source: "..\bin\Release\Soen - Torrezim.exe.config"; DestDir: "{app}"; Flags: ignoreversion; Components: app
Source: "..\bin\Release\System.Data.SQLite.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: app
Source: "..\bin\Release\System.Buffers.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: app
Source: "..\bin\Release\System.Memory.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: app
Source: "..\bin\Release\System.Numerics.Vectors.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: app
Source: "..\bin\Release\System.Resources.Extensions.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: app
Source: "..\bin\Release\System.Runtime.CompilerServices.Unsafe.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: app
Source: "..\bin\Release\x86\*"; DestDir: "{app}\x86"; Flags: ignoreversion recursesubdirs; Components: app
Source: "..\bin\Release\x64\*"; DestDir: "{app}\x64"; Flags: ignoreversion recursesubdirs; Components: app
; O banco de dados (soen.db) NÃO é instalado: é criado no primeiro uso e NÃO é removido ao desinstalar.

; SoenWebApi (WhatsApp) — complemento opcional (Node.js + Chromium portáteis)
Source: "..\..\dist\pendrive\SOEN - TORREZIM Pendrive\SoenWebApi\*"; DestDir: "{app}\SoenWebApi"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: webapi

[Icons]
Name: "{group}\SOEN"; Filename: "{app}\Soen - Torrezim.exe"; Components: app
Name: "{group}\SoenWebApi (WhatsApp)"; Filename: "{app}\SoenWebApi\IniciarWebApi.bat"; Components: webapi
Name: "{group}\Desinstalar SOEN"; Filename: "{uninstallexe}"
Name: "{autodesktop}\SOEN"; Filename: "{app}\Soen - Torrezim.exe"; Tasks: desktopicon; Components: app

[Run]
Filename: "{app}\Soen - Torrezim.exe"; Description: "Executar o SOEN agora"; Flags: nowait postinstall skipifsilent; Components: app
Filename: "https://deepdarkness.com.br/"; Description: "Abrir o site Deep Darkness"; Flags: shellexec nowait postinstall skipifsilent unchecked

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    MsgBox('Os dados e o banco de dados (soen.db) NÃO serão removidos.' + #13#10 +
           'Eles continuarão disponíveis em: ' + ExpandConstant('{app}'), mbInformation, MB_OK);
end;