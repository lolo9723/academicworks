param([string]$SigningThumbprint='', [ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Set-Location $root
if (-not [Environment]::Is64BitOperatingSystem) {throw 'Bu paket 64-bit Windows gerektirir. 32-bit ve 64-bit Office desteklenir.'}
if (Get-Process WINWORD -ErrorAction SilentlyContinue) {throw 'Derleme ve paketleme için Wordü kapatın.'}
foreach($tool in @('dotnet','mvn','java')){if(-not(Get-Command $tool -ErrorAction SilentlyContinue)){throw "$tool bulunamadı. Geliştirici gereksinimlerini READMEden yükleyin."}}
$vswhere="${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if(-not(Test-Path $vswhere)){throw 'Visual Studio / Build Tools bulunamadı.'}
$vs=& $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.TeamOffice -property installationPath
if(-not $vs){$vs=& $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.TeamOffice.BuildTools -property installationPath}
if(-not $vs){throw 'Visual Studio Office/VSTO geliştirme bileşeni kurulu değil.'}
$msbuild=Join-Path $vs 'MSBuild\Current\Bin\MSBuild.exe'
New-Item artifacts -ItemType Directory -Force | Out-Null
$javaCheck=New-Object System.Diagnostics.Process
try {
 $javaCheck.StartInfo.FileName=(Get-Command java).Source
 $javaCheck.StartInfo.Arguments='-version'
 $javaCheck.StartInfo.UseShellExecute=$false
 $javaCheck.StartInfo.CreateNoWindow=$true
 $javaCheck.StartInfo.RedirectStandardError=$true
 $javaCheck.StartInfo.RedirectStandardOutput=$true
 $javaCheck.Start() | Out-Null
 $jdkVersion=$javaCheck.StandardError.ReadToEnd()+$javaCheck.StandardOutput.ReadToEnd()
 $javaCheck.WaitForExit()
 if($javaCheck.ExitCode -ne 0){throw 'Java sürümü okunamadı.'}
} finally {$javaCheck.Dispose()}
if($jdkVersion -notmatch 'version "(?:17|18|19|2\d)\.') {throw 'Java JDK 17 veya üstü gerekiyor.'}
& mvn -f nlp/pom.xml clean package
if($LASTEXITCODE -ne 0){throw 'Türkçe NLP derlemesi başarısız.'}
$pin=Get-Content build/java-runtime.json -Raw | ConvertFrom-Json
$archive=Join-Path $root 'artifacts\java-windows.zip'
if(-not(Test-Path $archive)){Invoke-WebRequest $pin.url -OutFile $archive -UseBasicParsing}
if((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pin.sha256.ToLowerInvariant()){throw 'Java çalışma ortamı SHA256 doğrulaması başarısız.'}
$unpack=Join-Path $root 'artifacts\java-extracted'
if(Test-Path $unpack){Remove-Item $unpack -Recurse -Force}
Expand-Archive $archive $unpack
$jre=Get-ChildItem $unpack -Directory | Select-Object -First 1
if(-not(Test-Path (Join-Path $jre.FullName 'bin\java.exe'))){throw 'Java paket düzeni beklenenden farklı.'}
New-Item runtime -ItemType Directory -Force | Out-Null
if(Test-Path runtime/java){Remove-Item runtime/java -Recurse -Force}
Copy-Item $jre.FullName runtime/java -Recurse
$env:ACADEMIC_JAVA=(Join-Path $root 'runtime\java\bin\java.exe')
$env:ACADEMIC_NLP_JAR=(Join-Path $root 'nlp\target\turkish-nlp-1.0.0.jar')
& dotnet restore AcademicParaphraser.Engine.sln --locked-mode
if($LASTEXITCODE -ne 0){throw 'NuGet restore başarısız.'}
foreach($mode in @('Debug','Release')){
 & dotnet build AcademicParaphraser.Engine.sln -c $mode --no-restore -warnaserror
 if($LASTEXITCODE -ne 0){throw "$mode derlemesi başarısız."}
 & dotnet test tests/AcademicParaphraser.Tests/AcademicParaphraser.Tests.csproj -c $mode --no-build --logger "trx;LogFileName=$mode.trx" --results-directory artifacts/tests
 if($LASTEXITCODE -ne 0){throw "$mode testleri başarısız."}
}
if(-not $SigningThumbprint){
 $cert=New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=Akademik Parafraz Development' -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(2)
 $SigningThumbprint=$cert.Thumbprint
 Write-Warning 'Bu derleme geliştirme sertifikası ile imzalanıyor; dağıtım için güvenilir yayıncı sertifikası kullanın.'
}
$cert=Get-Item "Cert:\CurrentUser\My\$SigningThumbprint"
if(-not $cert.HasPrivateKey){throw 'İmzalama sertifikasının özel anahtarı bulunamadı.'}
foreach($mode in @('Debug','Release')) {
 & $msbuild src/AcademicParaphraser.WordAddin/AcademicParaphraser.WordAddin.csproj /restore /t:Rebuild "/p:Configuration=$mode" /p:RestoreLockedMode=true /p:SignManifests=true "/p:ManifestCertificateThumbprint=$SigningThumbprint" /p:Platform=AnyCPU /warnaserror /v:minimal "/bl:artifacts\vsto-$mode.binlog"
 if($LASTEXITCODE -ne 0){throw "$mode VSTO derlemesi / manifest imzalama başarısız."}
}
$output=Join-Path $root "src\AcademicParaphraser.WordAddin\bin\$Configuration"
foreach($file in @('AcademicParaphraser.WordAddin.dll','AcademicParaphraser.WordAddin.vsto','AcademicParaphraser.WordAddin.dll.manifest','runtimes\win-x86\native\e_sqlite3.dll','runtimes\win-x64\native\e_sqlite3.dll','runtime\java\bin\java.exe','nlp\turkish-nlp-1.0.0.jar')){if(-not(Test-Path (Join-Path $output $file))){throw "Dağıtım çıktısı eksik: $file"}}
$payload=Join-Path $root 'artifacts\payload'
if(Test-Path $payload){Remove-Item $payload -Recurse -Force}
Copy-Item $output $payload -Recurse
Copy-Item README.md,docs/Windows-Acceptance.md,THIRD-PARTY-NOTICES.md $payload
Copy-Item licenses $payload -Recurse
Export-Certificate -Cert $cert -FilePath (Join-Path $payload 'publisher.cer') | Out-Null
$iscc=Get-Command ISCC.exe -ErrorAction SilentlyContinue
if(-not $iscc){$candidate="${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe";if(Test-Path $candidate){$iscc=$candidate}else{throw 'Inno Setup 6 bulunamadı; installer derlenemedi.'}}
$compiler=if($iscc -is [System.Management.Automation.CommandInfo]){$iscc.Source}else{$iscc}
& $compiler "/DPayloadDir=$payload" "/DOutputDir=$root\artifacts\installer" Installer/AcademicParaphraser.iss
if($LASTEXITCODE -ne 0){throw 'Installer derlemesi başarısız.'}
Write-Host 'Derleme, test ve kurulum paketi oluşturma tamamlandı. Gerçek Word kabul testlerini docs/Windows-Acceptance.md ile çalıştırın.'
