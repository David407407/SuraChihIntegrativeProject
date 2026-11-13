; =====================================================================
; Instalador de SuraChih (Inno Setup 6). No se compila a mano: usa crear-instalador.ps1, que primero
; publica la app en ..\publicar y luego arma Instalador\salida\SuraChih-Instalador.exe.
;
; Se instala para el usuario actual (sin pedir permisos de administrador) en
; %LocalAppData%\Programs\SuraChih, con acceso en el menú Inicio y, si se elige, en el escritorio.
; Se desinstala desde Configuración > Aplicaciones, como cualquier programa.
; =====================================================================

#define Nombre "SuraChih"
#define Version "0.2.0"
#define Publicado "..\publicar"

[Setup]
AppId={{7E3B1F52-9C4A-4D8E-A6B2-5F1C0D9E8A31}
AppName={#Nombre}
AppVersion={#Version}
AppPublisher=Equipo SuraChih · UTCH 2026
DefaultDirName={localappdata}\Programs\{#Nombre}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=salida
OutputBaseFilename={#Nombre}-Instalador
SetupIconFile=..\Resources\AppIcon\appicon.ico
UninstallDisplayIcon={app}\SuraChih.exe
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "escritorio"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
Source: "{#Publicado}\SuraChih.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Publicado}\SuraChih.staticwebassets.endpoints.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Publicado}\wwwroot\*"; DestDir: "{app}\wwwroot"; Flags: ignoreversion recursesubdirs createallsubdirs
; La conexión a la base de datos (si existe al crear el instalador). Contiene la contraseña de MySQL:
; el instalador no se sube al repositorio público.
Source: "{#Publicado}\dbsettings.local.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{autoprograms}\{#Nombre}"; Filename: "{app}\SuraChih.exe"
Name: "{autodesktop}\{#Nombre}"; Filename: "{app}\SuraChih.exe"; Tasks: escritorio

[Run]
Filename: "{app}\SuraChih.exe"; Description: "Abrir SuraChih"; Flags: nowait postinstall skipifsilent
