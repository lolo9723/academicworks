# Run on an ephemeral Windows CI machine with Visual Studio 2022 already installed.
$ErrorActionPreference='Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Bu hazırlık betiği Windows gerektirir.' }
$vswhere="${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) { throw 'Visual Studio Installer bulunamadı.' }
$vs=& $vswhere -latest -products * -version '[17.0,18.0)' -property installationPath
if (-not $vs) { throw 'Visual Studio 2022 bulunamadı.' }
$ready=& $vswhere -latest -products * -version '[17.0,18.0)' -requires Microsoft.VisualStudio.Component.TeamOffice Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $ready) {
 $installer="${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\setup.exe"
 $process=Start-Process -FilePath $installer -ArgumentList @('modify','--installPath',('"'+$vs+'"'),'--add','Microsoft.VisualStudio.Component.TeamOffice','--add','Microsoft.Net.Component.4.8.TargetingPack','--add','Microsoft.VisualStudio.Component.VC.Tools.x86.x64','--quiet','--norestart') -PassThru -Wait
 if ($process.ExitCode -notin @(0,3010)) { throw "VSTO geliştirme bileşeni kurulamadı: $($process.ExitCode)" }
 if ($process.ExitCode -eq 3010) { Write-Warning 'Visual Studio Installer yeniden başlatma önerdi; dosyalar ve gerçek derleme ayrıca kontrol edilecek.' }
}
$ready=& $vswhere -latest -products * -version '[17.0,18.0)' -requires Microsoft.VisualStudio.Component.TeamOffice Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $ready) { throw 'VSTO bileşeni kurulum sonrası bulunamadı.' }
$targets=Join-Path $ready 'MSBuild\Microsoft\VisualStudio\v17.0\OfficeTools\Microsoft.VisualStudio.Tools.Office.targets'
if (-not (Test-Path $targets)) { throw 'Gerçek OfficeTools MSBuild hedefi eksik.' }
foreach($tool in @('dotnet','mvn','java')) {
 if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "Windows CI gereksinimi eksik: $tool" }
}
$iscc="${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { throw 'Inno Setup 6 bulunamadı.' }
Write-Host 'PASS: Windows CI OfficeTools, MSBuild, .NET, Java, Maven ve Inno Setup.'
