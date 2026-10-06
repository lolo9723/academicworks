param(
 [string]$Payload,
 [string]$ReportPath = (Join-Path $PSScriptRoot 'AkademikParafraz-Tani.json'),
 [switch]$Strict,
 [switch]$ShadowCopy,
 [switch]$SkipNativeBootstrap
)
$ErrorActionPreference='Stop'
$report=[ordered]@{wordExecuted=$false;processArchitecture=$(if([Environment]::Is64BitProcess){'x64'}else{'x86'});startupCode='NOT_RECORDED';shadowCopyRequested=[bool]$ShadowCopy;assemblyStorage='UNKNOWN';failureOrigins=@();files=@();components=@();passed=$false}
$domain=$null;$temporary=$null
$probeStage='ENVIRONMENT'
try {
 if($env:OS -ne 'Windows_NT' -or $PSVersionTable.PSEdition -eq 'Core'){throw 'Windows PowerShell ve .NET Framework gerekiyor.'}
 if(-not $Payload){
  $manifest=(Get-ItemProperty 'HKCU:\Software\Microsoft\Office\Word\Addins\AcademicParaphraser.WordAddin' -ErrorAction SilentlyContinue).Manifest
  if($manifest){$Payload=Split-Path ([Uri]($manifest -replace '\|vstolocal$','')).LocalPath}
  if(-not $Payload){$Payload=Join-Path $env:LOCALAPPDATA 'Programs\AkademikParafraz'}
 }
 $probeStage='PAYLOAD_PATH'
 $Payload=(Resolve-Path -LiteralPath $Payload).Path
 $startup=Join-Path $env:LOCALAPPDATA 'AkademikParafraz\logs\startup-diagnostics.txt'
 if((Test-Path $startup) -and (Get-Item $startup).Length -lt 4096){
  $text=Get-Content $startup -Raw
  if($text -match '\bAP_[A-Z_]+_[A-Za-z0-9]+_[A-F0-9]{8}\b'){$report.startupCode=$Matches[0]}
 }
 foreach($relative in @('AcademicParaphraser.WordAddin.dll','AcademicParaphraser.WordAddin.dll.config','AcademicParaphraser.WordHost.dll','AcademicParaphraser.Infrastructure.dll','AcademicParaphraser.Core.dll','Microsoft.Data.Sqlite.dll','SQLitePCLRaw.core.dll','SQLitePCLRaw.batteries_v2.dll','SQLitePCLRaw.provider.dynamic_cdecl.dll','runtimes\win-x86\native\e_sqlite3.dll','runtimes\win-x64\native\e_sqlite3.dll','runtime\java\bin\java.exe','nlp\turkish-nlp-1.0.0.jar','data\kenet.sqlite','data\KENET-NOTICE.txt')){
  $report.files+=@{file=$relative;exists=(Test-Path -LiteralPath (Join-Path $Payload $relative) -PathType Leaf)}
 }
 $probeStage='HELPER_COMPILE'
 $temporary=Join-Path ([IO.Path]::GetTempPath()) ('AcademicProbe-'+[Guid]::NewGuid().ToString('N'))
 New-Item $temporary -ItemType Directory | Out-Null
 $helper=Join-Path $temporary 'AcademicStartupProbe.dll'
 Add-Type -OutputAssembly $helper -TypeDefinition @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
public sealed class AcademicStartupProbe : MarshalByRefObject
{
    public override object InitializeLifetimeService() { return null; }
    public string[] Run(string payload, string temporary, string nativeMode)
    {
        var results = new List<string>();
        Assembly infrastructure = null;
        Probe(results, "FRAMEWORK_LOAD", delegate {
            infrastructure = Assembly.LoadFrom(Path.Combine(payload, "AcademicParaphraser.Infrastructure.dll"));
        });
        if (infrastructure == null) return results.ToArray();
        Probe(results, "STORAGE_LOAD", delegate {
            var host = Assembly.LoadFrom(Path.Combine(payload, "AcademicParaphraser.WordHost.dll"));
            var bootstrap = host.GetType("AcademicParaphraser.WordHost.NativeSqliteBootstrap", true);
            results.Add("STORAGE:" + (string)bootstrap.GetMethod("GetStorageMode").Invoke(null, null));
            if (nativeMode == "prepare") {
                Probe(results, "NATIVE_SQLITE", delegate {
                    bootstrap.GetMethod("Prepare").Invoke(null, new object[] { payload });
                    // Repeating preparation must preserve an already verified file.
                    bootstrap.GetMethod("Prepare").Invoke(null, new object[] { payload });
                });
            }
        });
        Probe(results, "DATABASE", delegate {
            var protector = Activator.CreateInstance(infrastructure.GetType("AcademicParaphraser.Infrastructure.Persistence.WindowsTextProtector", true));
            var repositoryType = infrastructure.GetType("AcademicParaphraser.Infrastructure.Persistence.LocalRepository", true);
            var repository = Activator.CreateInstance(repositoryType, new object[] { Path.Combine(temporary, "probe.sqlite"), protector });
            var rules = (ICollection)repositoryType.GetMethod("GetRules").Invoke(repository, null);
            var lexicon = (ICollection)repositoryType.GetMethod("GetLexicon").Invoke(repository, null);
            if (rules.Count < 61 || lexicon.Count < 39) throw new InvalidOperationException();
            repositoryType.GetMethod("GetSettings").Invoke(repository, null);
        });
        Probe(results, "WORDNET", delegate {
            var type = infrastructure.GetType("AcademicParaphraser.Infrastructure.LexicalKnowledge.WordNetLexicalSource", true);
            var knowledge = Activator.CreateInstance(type, new object[] { Path.Combine(payload, "data", "kenet.sqlite"), null });
            string description = (string)type.GetMethod("Describe").Invoke(knowledge, null);
            if (int.Parse(description.Split(' ')[0]) < 60000) throw new InvalidOperationException();
        });
        Probe(results, "PREVIEW_UI", delegate {
            var host = Assembly.LoadFrom(Path.Combine(payload, "AcademicParaphraser.WordHost.dll"));
            using (var preview = (IDisposable)Activator.CreateInstance(host.GetType("AcademicParaphraser.WordHost.UI.PreviewPane", true))) { }
        });
        foreach (var name in new[] { "TurkishWiktionaryProvider", "EnglishWiktionaryProvider" }) {
            Probe(results, "DICTIONARY_" + name, delegate {
                using (var provider = (IDisposable)Activator.CreateInstance(infrastructure.GetType("AcademicParaphraser.Infrastructure.InternetDictionaryProviders." + name, true))) { }
            });
        }
        Probe(results, "NLP_LOCAL", delegate {
            var type = infrastructure.GetType("AcademicParaphraser.Infrastructure.TurkishNlp.ZemberekProcess", true);
            using (var nlp = (IDisposable)Activator.CreateInstance(type, new object[] { Path.Combine(payload, "runtime", "java", "bin", "java.exe"), Path.Combine(payload, "nlp", "turkish-nlp-1.0.0.jar") })) {
                var task = (Task)type.GetMethod("AnalyzeAsync").Invoke(nlp, new object[] { "belirlenmiştir", CancellationToken.None });
                task.GetAwaiter().GetResult();
                var tokens = (ICollection)task.GetType().GetProperty("Result").GetValue(task, null);
                if (tokens.Count == 0) throw new InvalidOperationException();
            }
        });
        return results.ToArray();
    }
    private static void Probe(List<string> results, string component, Action action)
    {
        try { action(); results.Add(component + ":PASS"); }
        catch (Exception ex) {
            var cause = ex.GetBaseException();
            results.Add(component + ":FAIL:" + cause.GetType().Name + ":" + cause.HResult.ToString("X8"));
            var frame = new System.Diagnostics.StackTrace(cause, false).GetFrame(0);
            var method = frame == null ? null : frame.GetMethod();
            if (method != null) results.Add("SITE:" + component + ":" + method.DeclaringType.FullName + "." + method.Name);
        }
    }
}
'@
 $probeStage='FRAMEWORK_DOMAIN'
 $setup=New-Object AppDomainSetup
 $setup.ApplicationBase=$Payload
 $setup.ConfigurationFile=Join-Path $Payload 'AcademicParaphraser.WordAddin.dll.config'
 if($ShadowCopy){
  $setup.ShadowCopyFiles='true'
  $setup.ShadowCopyDirectories=$Payload
  $setup.CachePath=Join-Path $temporary 'shadow'
  $setup.ApplicationName='AcademicStartupProbe'
 }
 $domain=[AppDomain]::CreateDomain('AcademicStartupProbe',$null,$setup)
 $probeStage='HELPER_LOAD'
 $helperAssembly=[Reflection.Assembly]::LoadFrom($helper)
 $runner=$helperAssembly.GetType('AcademicStartupProbe').GetMethod('Run')
 $probe=$domain.CreateInstanceFromAndUnwrap($helper,'AcademicStartupProbe')
 $probeStage='COMPONENTS'
 # Windows PowerShell adapts the transparent proxy as MarshalByRefObject; invoke its actual contract.
 $nativeMode=if($SkipNativeBootstrap){'baseline'}else{'prepare'}
 $results=@($runner.Invoke($probe,[string[]]@($Payload,$temporary,$nativeMode)))
 $storage=@($results | Where-Object {$_ -match '^STORAGE:(DIRECT|SHADOW)$'})
 if($storage.Count -eq 1){$report.assemblyStorage=$storage[0].Substring(8)}
 $report.failureOrigins=@($results | Where-Object {$_ -match '^SITE:'})
 $report.components=@($results | Where-Object {$_ -notmatch '^(STORAGE:|SITE:|STORAGE_LOAD:PASS$)'})
 $required=@('FRAMEWORK_LOAD','DATABASE','WORDNET','PREVIEW_UI','DICTIONARY_TurkishWiktionaryProvider','DICTIONARY_EnglishWiktionaryProvider','NLP_LOCAL')
 if(-not $SkipNativeBootstrap){$required+='NATIVE_SQLITE'}
 $missing=@($required | Where-Object {$report.components -notcontains ($_+':PASS')})
 $report.passed=(@($report.files | Where-Object {-not $_.exists}).Count -eq 0 -and @($report.components | Where-Object {$_ -match ':FAIL:'}).Count -eq 0 -and $missing.Count -eq 0 -and $report.assemblyStorage -eq $(if($ShadowCopy){'SHADOW'}else{'DIRECT'}))
}catch{
 $cause=$_.Exception.GetBaseException()
 $report.components+=('PROBE_'+$probeStage+':FAIL:'+$cause.GetType().Name+':'+$cause.HResult.ToString('X8'))
 if($env:GITHUB_ACTIONS -eq 'true'){Write-Host $_.ToString();Write-Host $_.InvocationInfo.PositionMessage}
}finally{
 if($domain){[AppDomain]::Unload($domain)}
 if($temporary){Remove-Item -LiteralPath $temporary -Recurse -Force -ErrorAction SilentlyContinue}
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding UTF8
$report.components | ForEach-Object {Write-Host $_}
Write-Host ('Tanilama raporu: '+$ReportPath)
Write-Host 'Word belgesi acilmadi; mevcut sozluk/veritabani degistirilmedi; internet sorgusu yapilmadi.'
if(-not $report.passed -and $Strict){throw 'Gercek .NET Framework payload acilis kontrolu basarisiz; JSON hata kodlarini inceleyin.'}
