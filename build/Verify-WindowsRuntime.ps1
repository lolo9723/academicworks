param([Parameter(Mandatory=$true)][string]$Payload)
$ErrorActionPreference='Stop'
if ($env:OS -ne 'Windows_NT') {throw 'Gerçek Windows çalışma ortamı gerekiyor.'}
$Payload=(Resolve-Path $Payload).Path
$report=[ordered]@{windows=$true; frameworkVersion=[Environment]::Version.ToString(); dpapiRoundTrip=$false; dpapiTamperRejected=$false; nativeArchitectures=@(); manifests=@(); wordExecuted=$false}

[Reflection.Assembly]::LoadFrom((Join-Path $Payload 'AcademicParaphraser.Infrastructure.dll')) | Out-Null
$protector=New-Object AcademicParaphraser.Infrastructure.Persistence.WindowsTextProtector
$plain='Doğayla ilişkinin öznel zindelik üzerindeki etkisi (Yılmaz, 2024).'
[byte[]]$encrypted=$protector.Protect($plain)
if($protector.Unprotect($encrypted) -ne $plain){throw 'Windows CurrentUser DPAPI geri çözme başarısız.'}
if([Text.Encoding]::UTF8.GetString($encrypted).Contains($plain)){throw 'DPAPI çıktısında düz metin var.'}
$report.dpapiRoundTrip=$true
$encrypted[20]=$encrypted[20] -bxor 1
try{$protector.Unprotect($encrypted) | Out-Null}catch{
 if($_.Exception -is [Security.Cryptography.CryptographicException] -or $_.Exception.InnerException -is [Security.Cryptography.CryptographicException]){$report.dpapiTamperRejected=$true}else{throw}
}
if(-not $report.dpapiTamperRejected){throw 'Değiştirilmiş DPAPI verisi reddedilmedi.'}

foreach($native in @(@{path='runtimes\win-x86\native\e_sqlite3.dll';machine=0x14c},@{path='runtimes\win-x64\native\e_sqlite3.dll';machine=0x8664},@{path='runtime\java\bin\java.exe';machine=0x8664})){
 $reader=New-Object IO.BinaryReader([IO.File]::OpenRead((Join-Path $Payload $native.path)))
 try{
  if($reader.ReadUInt16() -ne 0x5a4d){throw 'Native dosyada DOS başlığı yok.'}
  $reader.BaseStream.Position=0x3c;$offset=$reader.ReadInt32();$reader.BaseStream.Position=$offset
  if($reader.ReadUInt32() -ne 0x00004550 -or $reader.ReadUInt16() -ne $native.machine){throw "Native mimari beklenenden farklı: $($native.path)"}
 }finally{$reader.Dispose()}
 $report.nativeArchitectures+=@{path=$native.path;machine=$native.machine}
}

Add-Type -AssemblyName System.Security
# ClickOnce uses Microsoft's legacy SHA256 XMLDSig URIs. Map them to real RSA/PKCS1 and SHA256 verification.
Add-Type -TypeDefinition @'
using System.Security.Cryptography;
public sealed class ClickOnceRsaSha256Description : SignatureDescription
{
    public ClickOnceRsaSha256Description() { KeyAlgorithm = typeof(RSA).AssemblyQualifiedName; }
    public override HashAlgorithm CreateDigest() { return SHA256.Create(); }
    public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
    {
        var result = new RSAPKCS1SignatureDeformatter(key);
        result.SetHashAlgorithm("SHA256");
        return result;
    }
    public override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
    {
        var result = new RSAPKCS1SignatureFormatter(key);
        result.SetHashAlgorithm("SHA256");
        return result;
    }
}
'@
[Security.Cryptography.CryptoConfig]::AddAlgorithm([ClickOnceRsaSha256Description],'http://www.w3.org/2000/09/xmldsig#rsa-sha256')
[Security.Cryptography.CryptoConfig]::AddAlgorithm([Security.Cryptography.SHA256CryptoServiceProvider],'http://www.w3.org/2000/09/xmldsig#sha256')
foreach($name in @('AcademicParaphraser.WordAddin.dll.manifest','AcademicParaphraser.WordAddin.vsto')){
 $settings=New-Object Xml.XmlReaderSettings;$settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit;$settings.XmlResolver=$null
 $reader=[Xml.XmlReader]::Create((Join-Path $Payload $name),$settings)
 $xml=New-Object Xml.XmlDocument;$xml.PreserveWhitespace=$true;$xml.XmlResolver=$null
 try{$xml.Load($reader)}finally{$reader.Dispose()}
 $ns=New-Object Xml.XmlNamespaceManager($xml.NameTable);$ns.AddNamespace('ds','http://www.w3.org/2000/09/xmldsig#')
 $signature=$xml.SelectSingleNode('/*/ds:Signature',$ns)
 if(-not $signature){throw "Manifestte gerçek XML imzası yok: $name"}
 $signed=New-Object Security.Cryptography.Xml.SignedXml($xml);$signed.LoadXml($signature)
 if(-not $signed.CheckSignature()){throw "Manifestin kriptografik XML imzası doğrulanamadı: $name"}
 $checked=0
 foreach($node in $xml.SelectNodes('//*[local-name()="file"][@name] | //*[local-name()="dependentAssembly"][@codebase]')){
  $relative=if($node.HasAttribute('codebase')){$node.GetAttribute('codebase')}else{$node.GetAttribute('name')}
  $file=Join-Path $Payload ([Uri]::UnescapeDataString($relative))
  if(-not (Test-Path -LiteralPath $file -PathType Leaf)){throw "Manifest bağımlılığı eksik: $relative"}
  $hash=$node.SelectSingleNode('./*[local-name()="hash"]')
  if(-not $hash){throw "Dosyanın manifest özeti eksik: $relative"}
  $method=$hash.SelectSingleNode('./ds:DigestMethod',$ns).GetAttribute('Algorithm')
  $expected=$hash.SelectSingleNode('./ds:DigestValue',$ns).InnerText
  $algorithm=switch($method){'http://www.w3.org/2000/09/xmldsig#sha1' {'SHA1'} 'http://www.w3.org/2001/04/xmlenc#sha256' {'SHA256'} 'http://www.w3.org/2000/09/xmldsig#sha256' {'SHA256'} default {throw "Bilinmeyen manifest özeti: $method"}}
  $hasher=[Security.Cryptography.HashAlgorithm]::Create($algorithm)
  $stream=[IO.File]::OpenRead($file)
  try{$actual=[Convert]::ToBase64String($hasher.ComputeHash($stream))}finally{$stream.Dispose();$hasher.Dispose()}
  if($actual -ne $expected){throw "Manifest dosya özeti uyuşmuyor: $relative"}
  $checked++
 }
 $report.manifests+=@{path=$name;signatureValid=$true;verifiedFiles=$checked}
}
$report | ConvertTo-Json -Depth 6 | Set-Content artifacts/windows-runtime-verification.json -Encoding UTF8
Write-Host 'PASS: gerçek Windows DPAPI, native mimariler, VSTO XML imzaları ve manifest dosya özetleri.'
