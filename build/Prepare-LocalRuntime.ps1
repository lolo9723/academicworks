param([Parameter(Mandatory=$true)][string]$VisualStudioRoot)
$ErrorActionPreference='Stop'
$pin=Get-Content (Join-Path $PSScriptRoot 'local-runtime.json') -Raw | ConvertFrom-Json
$root=Split-Path -Parent $PSScriptRoot
$archive=Join-Path $root 'artifacts\llama-windows.zip'
if(-not(Test-Path $archive)){Invoke-WebRequest $pin.url -OutFile $archive -UseBasicParsing}
if((Get-Item $archive).Length -ne $pin.bytes -or (Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $pin.sha256){throw 'Local CPU runtime integrity failed.'}
$unpack=Join-Path $root 'artifacts\llama-extracted'
if(Test-Path $unpack){Remove-Item $unpack -Recurse -Force}
Expand-Archive $archive $unpack
$destination=Join-Path $root 'runtime\llama'
New-Item $destination -ItemType Directory -Force | Out-Null
# Keep all CPU dispatch variants, basic libraries and notices, omit unrelated CLI executables.
foreach($name in @('llama-server.exe','llama-server-impl.dll','llama-common.dll','mtmd.dll','llama.dll','ggml.dll','ggml-base.dll','libomp.dll')){
 $item=Get-ChildItem $unpack -Recurse -File -Filter $name | Select-Object -First 1
 if(-not $item){throw "CPU runtime dependency missing: $name"}
 Copy-Item $item.FullName $destination
}
Get-ChildItem $unpack -Recurse -File | Where-Object {$_.Name -like 'ggml-cpu-*.dll' -or $_.Name -like 'LICENSE*'} | Copy-Item -Destination $destination

# Official redistributable CRT files, app-local: users do not need a compiler/SDK.
$redist=Join-Path $VisualStudioRoot 'VC\Redist\MSVC'
$crt=Get-ChildItem $redist -Directory | Sort-Object Name -Descending | ForEach-Object {
 Get-ChildItem $_.FullName -Directory -Recurse | Where-Object { $_.Name -match '^Microsoft\.VC14[0-9]\.CRT$' -and $_.FullName -match '\\x64\\' }
} | Select-Object -First 1
if(-not $crt){throw 'Official Visual C++ x64 redistributable folder missing; install C++ build tools for packaging.'}
Get-ChildItem $crt.FullName -Filter '*.dll' | Copy-Item -Destination $destination
foreach($file in @('MSVCP140.dll','VCRUNTIME140.dll','VCRUNTIME140_1.dll','llama-common.dll','mtmd.dll')){
 if(-not(Test-Path (Join-Path $destination $file))){throw "Runtime dependency missing: $file"}
}
