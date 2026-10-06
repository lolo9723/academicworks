param([Parameter(Mandatory=$true)][string]$Installer)
$ErrorActionPreference='Stop'
$Installer=(Resolve-Path $Installer).Path
foreach($view in @([Microsoft.Win32.RegistryView]::Registry32,[Microsoft.Win32.RegistryView]::Registry64)){
 $hive=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine,$view)
 try{$key=$hive.OpenSubKey('SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WINWORD.EXE');if($key){$key.Dispose();throw 'Bu CI kontrolü Word bulunmayan gerçek Windows makinesi gerektirir.'}}finally{$hive.Dispose()}
}
$addinKey='HKCU:\Software\Microsoft\Office\Word\Addins\AcademicParaphraser.WordAddin'
$before=if(Test-Path $addinKey){Get-ItemProperty $addinKey | ConvertTo-Json -Compress}else{''}
$log=Join-Path (Get-Location) 'artifacts\installer-prerequisite.log'
$process=Start-Process -FilePath $Installer -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+$log+'"')) -PassThru
try{
 if(-not $process.WaitForExit(60000)){$process.Kill();throw 'Installer önkoşul kontrolünde zaman aşımı.'}
 if($process.ExitCode -ne 1){throw "Eksik Word için beklenen erken kurulum reddi alınamadı: $($process.ExitCode)"}
}finally{$process.Dispose()}
$after=if(Test-Path $addinKey){Get-ItemProperty $addinKey | ConvertTo-Json -Compress}else{''}
if($before -ne $after){throw 'Reddedilen kurulum Word eklentisi kaydını değiştirdi.'}
if(-not (Get-Content $log -Raw).Contains('PREREQUISITE_WORD_MISSING')){throw 'Installer eksik Word gerekçesini kaydetmedi.'}
@{missingWordRejected=$true;addinRegistrationUnchanged=$true;wordExecuted=$false} | ConvertTo-Json | Set-Content artifacts/installer-prerequisite-verification.json -Encoding UTF8
Write-Host 'PASS: gerçek Setup.exe eksik Word için erken durdu; eklenti kaydı değişmedi.'
