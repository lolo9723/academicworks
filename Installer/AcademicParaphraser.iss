#ifndef PayloadDir
 #error PayloadDir must be supplied by Build-Windows.ps1
#endif
#ifndef OutputDir
 #error OutputDir must be supplied by Build-Windows.ps1
#endif
[Setup]
AppId={{482D00C1-39AE-45A9-AB33-1253584ED67B}
AppName=Akademik Parafraz
AppVersion=1.5.0
AppPublisher=Akademik Parafraz
DefaultDirName={localappdata}\Programs\AkademikParafraz
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
MinVersion=10.0
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=AkademikParafraz-1.5.0-Setup
Compression=lzma2
SolidCompression=yes
UninstallDisplayName=Akademik Parafraz
CloseApplications=yes
RestartApplications=no
WizardStyle=modern
[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Office\Word\Addins\AcademicParaphraser.WordAddin"; ValueType: string; ValueName: "FriendlyName"; ValueData: "Akademik Parafraz"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Office\Word\Addins\AcademicParaphraser.WordAddin"; ValueType: string; ValueName: "Description"; ValueData: "Yerel ve kural tabanlı akademik Türkçe parafraz"
Root: HKCU; Subkey: "Software\Microsoft\Office\Word\Addins\AcademicParaphraser.WordAddin"; ValueType: dword; ValueName: "LoadBehavior"; ValueData: "3"
Root: HKCU; Subkey: "Software\Microsoft\Office\Word\Addins\AcademicParaphraser.WordAddin"; ValueType: string; ValueName: "Manifest"; ValueData: "file:///{app}\AcademicParaphraser.WordAddin.vsto|vstolocal"
[UninstallDelete]
Type: files; Name: "{app}\e_sqlite3.dll"
[UninstallRun]
Filename: "{code:GetVstoInstaller}"; Parameters: "/Uninstall ""{app}\AcademicParaphraser.WordAddin.vsto"""; Flags: waituntilterminated; RunOnceId: "RemoveVstoTrustRegistration"
[Code]
var VstoInstaller: String;
function GetVstoInstaller(Param: String): String;
begin
 if VstoInstaller = '' then begin
 if not RegQueryStringValue(HKLM32,'SOFTWARE\Microsoft\VSTO Runtime Setup\v4','InstallerPath',VstoInstaller) then
   if not RegQueryStringValue(HKLM64,'SOFTWARE\Microsoft\VSTO Runtime Setup\v4','InstallerPath',VstoInstaller) then VstoInstaller := ExpandConstant('{commonpf}\Common Files\Microsoft Shared\VSTO\10.0\VSTOInstaller.exe');
 end;
 Result := VstoInstaller;
end;
function InitializeSetup(): Boolean;
var Release, WordVersionMS, WordVersionLS: Cardinal; WordPath: String;
begin
 Result := False;
 if FindWindowByClassName('OpusApp') <> 0 then begin SuppressibleMsgBox('Önce Microsoft Word pencerelerini kapatın.',mbInformation,MB_OK,IDOK);exit;end;
 if not RegQueryDWordValue(HKLM32,'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full','Release',Release) or (Release < 528040) then begin SuppressibleMsgBox('.NET Framework 4.8 gerekiyor. Microsoft .NET Framework 4.8 kurulumunu tamamlayıp tekrar deneyin.',mbInformation,MB_OK,IDOK);exit;end;
 if not RegQueryStringValue(HKLM32,'SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE','',WordPath) then
  if not RegQueryStringValue(HKLM64,'SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE','',WordPath) then begin Log('PREREQUISITE_WORD_MISSING'); SuppressibleMsgBox('Windows masaüstü Microsoft Word bulunamadı. Word web bu VSTO paketini çalıştıramaz.',mbInformation,MB_OK,IDOK);exit;end;
 if not GetVersionNumbers(WordPath, WordVersionMS, WordVersionLS) or ((WordVersionMS shr 16) < 16) then begin Log('PREREQUISITE_WORD_VERSION_UNSUPPORTED'); SuppressibleMsgBox('Microsoft Word 2016 veya daha yeni bir masaüstü sürümü gerekiyor.',mbInformation,MB_OK,IDOK);exit;end;
 if not RegQueryStringValue(HKLM32,'SOFTWARE\Microsoft\VSTO Runtime Setup\v4','InstallerPath',VstoInstaller) then
  if not RegQueryStringValue(HKLM64,'SOFTWARE\Microsoft\VSTO Runtime Setup\v4','InstallerPath',VstoInstaller) then VstoInstaller := ExpandConstant('{commonpf}\Common Files\Microsoft Shared\VSTO\10.0\VSTOInstaller.exe');
 if not FileExists(VstoInstaller) then begin SuppressibleMsgBox('Microsoft VSTO Runtime gerekiyor. Resmî indirme: https://www.microsoft.com/en-us/download/details.aspx?id=105522',mbInformation,MB_OK,IDOK);exit;end;
 Result := True;
end;
procedure CurStepChanged(CurStep: TSetupStep);
var ExitCode: Integer;
begin
 if CurStep = ssPostInstall then begin
  WizardForm.StatusLabel.Caption := 'Word eklentisinin imzası doğrulanıyor…';
  if not Exec(GetVstoInstaller(''), '/Install "'+ExpandConstant('{app}')+'\AcademicParaphraser.WordAddin.vsto"', '', SW_SHOW, ewWaitUntilTerminated, ExitCode) then RaiseException('VSTO kurulum aracı çalıştırılamadı.');
  if ExitCode <> 0 then RaiseException('VSTO imza/güven doğrulaması tamamlanmadı. Kurulum hata kodu: '+IntToStr(ExitCode));
 end;
 if CurStep = ssDone then begin
  RegWriteStringValue(HKCU,'Software\Microsoft\Office\Word\Addins\AcademicParaphraser.WordAddin','Manifest','file:///'+ExpandConstant('{app}')+'\AcademicParaphraser.WordAddin.vsto|vstolocal');
 end;
end;
