; ============================================================================
;  VK M — установщик на основе Inno Setup
;  Папка сборки приложения передаётся через #define SourceDir.
;  Пример локального запуска (после сборки конфигурации Unpacked):
;    ISCC.exe "Installer\VK_M_installer.iss"
;  Пример для CI (указывается абсолютный путь к распакованной сборке):
;    ISCC.exe "Installer\VK_M_installer.iss" /DSourceDir="C:\build" /DMyAppVersion="1.0.2"
; ============================================================================

#ifndef MyAppName
  #define MyAppName "Music M"
#endif

#ifndef MyAppVersion
  #define MyAppVersion "1.0.3"
#endif

#ifndef MyAppPublisher
  #define MyAppPublisher "Music M"
#endif

#ifndef SourceDir
  #define SourceDir "..\VK UI3\bin\x64\Unpacked"
#endif

#ifndef MyAppOutputDir
  #define MyAppOutputDir "..\InstallerOutput"
#endif

#ifndef MyAppOutputBase
  #define MyAppOutputBase "VK_M_Setup"
#endif

; Архитектура сборки: x64 | x86 | arm64
#ifndef MyAppArch
  #define MyAppArch "x64"
#endif

#if MyAppArch == "x86"
  #define MyArchAllowed "x86compatible"
  #define MyArchInstall ""
  #define MyExeSuffix "x86"
#elif MyAppArch == "arm64"
  #define MyArchAllowed "arm64"
  #define MyArchInstall "arm64"
  #define MyExeSuffix "arm64"
#else
  #define MyArchAllowed "x64compatible"
  #define MyArchInstall "x64compatible"
  #define MyExeSuffix "x64"
#endif

#define MyAppExeName "VK M.exe"

[Setup]
; NOTE: AppId — уникальный идентификатор приложения. Не меняйте после публикации,
; иначе Windows перестанет ассоциировать обновления с установленной версией.
AppId={{2F5C9A1B-4D3E-4A2B-8C1D-9E0F1A2B3C4D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#MyAppOutputDir}
OutputBaseFilename={#MyAppOutputBase}_{#MyAppVersion}_{#MyExeSuffix}
SetupIconFile=..\VK UI3\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed={#MyArchAllowed}
#if MyAppArch != "x86"
ArchitecturesInstallIn64BitMode={#MyArchInstall}
#endif
LicenseFile=..\LICENSE.txt

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent