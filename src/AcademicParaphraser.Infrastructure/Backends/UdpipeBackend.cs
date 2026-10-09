using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Backends;
using Newtonsoft.Json.Linq;
namespace AcademicParaphraser.Infrastructure.Backends
{
    public sealed class UdpipeBackend : ILinguisticBackend
    {
        private readonly string executable,directory;private readonly ITurkishNlp nlp;
        private readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);private Process? active;private bool disposed;
        private readonly HashSet<string> verified=new HashSet<string>(StringComparer.Ordinal);
        public long WorkingSetBytes { get { try{var p=active;if(p==null||p.HasExited)return 0;p.Refresh();return p.WorkingSet64;}catch(InvalidOperationException){return 0;} } }
        public long PeakWorkingSetBytes { get; private set; }
        public UdpipeBackend(string executable,string directory,ITurkishNlp nlp){this.executable=executable;this.directory=directory;this.nlp=nlp;}
        public async Task<LinguisticAnalysis> AnalyzeAsync(string text,string language,CancellationToken cancellation)
        {
            if(language!="tr"&&language!="en")throw new ArgumentException("Desteklenmeyen çözümleme dili.");
            if(disposed)throw new ObjectDisposedException(nameof(UdpipeBackend));
            await gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                Verify(language);cancellation.ThrowIfCancellationRequested();string model=Path.Combine(directory,language+"-boun-ewt.udpipe");
                using(var process=new Process{StartInfo=new ProcessStartInfo(executable,"--tokenize --tag --parse \""+model+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8}})
                {
                    active=process;process.Start();var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
                    using(var registration=cancellation.Register(()=>Kill(process)))
                    {
                        using(var input=new StreamWriter(process.StandardInput.BaseStream,new UTF8Encoding(false),4096,true)){await input.WriteAsync(text).ConfigureAwait(false);await input.FlushAsync().ConfigureAwait(false);}
                        process.StandardInput.Close();
                        var deadline=Task.Delay(TimeSpan.FromSeconds(45),cancellation);
                        while(!output.IsCompleted)
                        {
                            var tick=Task.Delay(100,cancellation);await Task.WhenAny(output,tick,deadline).ConfigureAwait(false);
                            PeakWorkingSetBytes=Math.Max(PeakWorkingSetBytes,WorkingSetBytes);
                            if(cancellation.IsCancellationRequested||deadline.IsCompleted){Kill(process);cancellation.ThrowIfCancellationRequested();throw new InvalidOperationException("Cümle çözümlemesi zaman aşımına uğradı.");}
                        }
                        string conllu=await output.ConfigureAwait(false);await error.ConfigureAwait(false);process.WaitForExit();cancellation.ThrowIfCancellationRequested();
                        if(process.ExitCode!=0)throw new InvalidOperationException("Cümle çözümleme bileşeni çalışmadı; belge değişmedi.");
                        var parsed=ConlluReader.Parse(text,conllu,language);
                        if(language=="tr")parsed.Morphology=await nlp.AnalyzeAsync(text,cancellation).ConfigureAwait(false);
                        return parsed;
                    }
                }
            }
            catch(Exception) when(cancellation.IsCancellationRequested){throw new OperationCanceledException(cancellation);}
            finally{active=null;gate.Release();}
        }
        private void Verify(string language)
        {
            if(verified.Contains(language))return;
            string manifest=Path.Combine(directory,"dependency-model-verification.json");
            if(!File.Exists(executable)||!File.Exists(manifest))throw new InvalidOperationException("Çok aşamalı motorun çözümleyicisi bulunamadı. Kurulumu onarın.");
            if(new FileInfo(manifest).Length>1000000)throw new InvalidOperationException("Çözümleyici manifesti geçersiz.");
            var data=JObject.Parse(File.ReadAllText(manifest));
            CheckHash(executable,(string?)data["runtimeSha256"]);
            CheckHash(Path.Combine(directory,language+"-boun-ewt.udpipe"),(string?)data["models"]?[language]?["sha256"]);
            verified.Add(language);
        }
        private static void CheckHash(string path,string? expected)
        {
            if(!File.Exists(path)||expected==null||expected.Length!=64)throw new InvalidOperationException("Çözümleyici dosyası/özeti eksik.");
            using(var file=File.OpenRead(path))using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(file)).Replace("-","").ToLowerInvariant()!=expected)throw new InvalidOperationException("Çözümleyici bütünlük kontrolü başarısız.");
        }
        private static void Kill(Process p){try{if(!p.HasExited)p.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}
        public void Dispose(){disposed=true;var process=active;if(process!=null)Kill(process);}
    }
}
