using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core.Backends;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace AcademicParaphraser.Infrastructure.Backends
{
    public sealed class LocalSemanticBackend : ISemanticBackend
    {
        public const string ModelSha256="27c39e884c14b03cf46cfc5485971b6db70ff330220d93dfe729c63fde43af0e";
        public const string TokenizerSha256="3aca3ce69a0a35aeb144a52c4f1d41c4246b8785f8f398315cc8fb6b24057810";
        private readonly string java,jar,model,tokenizer;private readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
        private Process? process;private StreamWriter? input;private bool disposed,verified;
        public long WorkingSetBytes{get{try{var p=process;if(p==null||p.HasExited)return 0;p.Refresh();return p.WorkingSet64;}catch(InvalidOperationException){return 0;}}}
        public LocalSemanticBackend(string java,string jar,string model,string tokenizer){this.java=java;this.jar=jar;this.model=model;this.tokenizer=tokenizer;}
        public async Task<BackendEvidence> CompareAsync(string source,string target,string language,CancellationToken cancellation)
        {
            if(disposed)throw new ObjectDisposedException(nameof(LocalSemanticBackend));
            if(language!="tr"&&language!="en")throw new ArgumentException("Desteklenmeyen anlam kontrolü dili.");
            await gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                await StartAsync(cancellation).ConfigureAwait(false);var current=process!;
                using(var registration=cancellation.Register(()=>Kill(current)))
                {
                    await input!.WriteLineAsync(JsonConvert.SerializeObject(new{source,target})).ConfigureAwait(false);await input.FlushAsync().ConfigureAwait(false);
                    var read=current.StandardOutput.ReadLineAsync();
                    if(await Task.WhenAny(read,Task.Delay(TimeSpan.FromSeconds(90),cancellation)).ConfigureAwait(false)!=read){Kill(current);cancellation.ThrowIfCancellationRequested();throw new InvalidOperationException("Anlam kontrolü zaman aşımına uğradı; belge değişmedi.");}
                    var response=JObject.Parse(await read.ConfigureAwait(false)??"{}");cancellation.ThrowIfCancellationRequested();
                    if(response["error"]!=null)throw new InvalidOperationException("Yerel anlam kontrolü tamamlanamadı; belge değişmedi.");
                    var forward=response["forward"]?.ToObject<double[]>();var reverse=response["reverse"]?.ToObject<double[]>();
                    if(!Valid(forward)||!Valid(reverse))throw new InvalidOperationException("Anlam kontrolü yanıtı geçersiz.");
                    var result=new BackendEvidence{Stage="bidirectional-local-nli",Passed=forward![0]>=.90&&reverse![0]>=.90&&forward[2]<=.05&&reverse[2]<=.05};
                    if(!result.Passed)result.Problems.Add("Kaynak ve öneri iki yönlü yerel anlam kontrolünü geçmedi.");
                    result.Notes.Add(string.Format(CultureInfo.InvariantCulture,"Model desteği (doğruluk yüzdesi değildir): ileri={0:F4}; geri={1:F4}; çelişki={2:F4}/{3:F4}.",forward[0],reverse![0],forward[2],reverse[2]));
                    result.Notes.Add("NLI olumsuzluk veya özne/nesne değişimini kaçırabilir; tek başına onay vermez.");
                    return result;
                }
            }
            catch(Exception) when(cancellation.IsCancellationRequested){throw new OperationCanceledException(cancellation);}
            finally{gate.Release();}
        }
        private async Task StartAsync(CancellationToken cancellation)
        {
            if(process!=null&&!process.HasExited)return;
            if(!File.Exists(java)||!File.Exists(jar))throw new InvalidOperationException("Yerel anlam bileşeni eksik. Kurulumu onarın.");
            if(!verified){CheckHash(model,ModelSha256,cancellation);CheckHash(tokenizer,TokenizerSha256,cancellation);verified=true;}
            input?.Dispose();process?.Dispose();
            string native=Path.Combine(Path.GetDirectoryName(jar)!,"native");
            bool directNative=File.Exists(Path.Combine(native,"tokenizers.dll"))||File.Exists(Path.Combine(native,"libtokenizers.so"));
            string nativeArgument=directNative?" -Donnxruntime.native.path=\""+native+"\"":"";
            process=new Process{StartInfo=new ProcessStartInfo(java,"-Dfile.encoding=UTF-8"+nativeArgument+" -Xmx512m -jar \""+jar+"\" \""+model+"\" \""+tokenizer+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8}};
            process.StartInfo.EnvironmentVariables["DJL_OFFLINE"]="true";
            process.StartInfo.EnvironmentVariables["RUST_FLAVOR"]="cpu";
            if(directNative)process.StartInfo.EnvironmentVariables["RUST_LIBRARY_PATH"]=native;
            process.StartInfo.EnvironmentVariables["DJL_CACHE_DIR"]=Path.Combine(Path.GetTempPath(),"AcademicParaphraser-native-cache");
            var current=process;current.ErrorDataReceived+=(s,e)=>{};current.Start();current.BeginErrorReadLine();
            input=new StreamWriter(current.StandardInput.BaseStream,new UTF8Encoding(false),4096,true);
            using(var registration=cancellation.Register(()=>Kill(current)))
            {
                var ready=current.StandardOutput.ReadLineAsync();
                if(await Task.WhenAny(ready,Task.Delay(TimeSpan.FromSeconds(60),cancellation)).ConfigureAwait(false)!=ready){Kill(current);cancellation.ThrowIfCancellationRequested();throw new InvalidOperationException("Yerel anlam bileşeni zamanında başlamadı.");}
                cancellation.ThrowIfCancellationRequested();
                var announcement=JObject.Parse(await ready.ConfigureAwait(false)??"{}");
                if(announcement.Value<bool?>("ready")!=true||announcement.Value<string>("nativeLoaderPatch")!="msvc-tokenizer-only-v2"){Kill(current);throw new InvalidOperationException("Yerel anlam bileşeni açılamadı.");}
            }
        }
        private static bool Valid(double[]? x)=>x!=null&&x.Length==3&&Array.TrueForAll(x,v=>!double.IsNaN(v)&&!double.IsInfinity(v)&&v>=0&&v<=1)&&Math.Abs(x[0]+x[1]+x[2]-1)<.002;
        private static void CheckHash(string path,string expected,CancellationToken cancellation)
        {
            if(!File.Exists(path))throw new InvalidOperationException("Anlam denetleyicisinin eğitimli dosyaları bulunamadı. Kurulumu onarın.");
            using(var hash=SHA256.Create())using(var file=File.OpenRead(path))
            {var buffer=new byte[1048576];int count;while((count=file.Read(buffer,0,buffer.Length))>0){cancellation.ThrowIfCancellationRequested();hash.TransformBlock(buffer,0,count,buffer,0);}hash.TransformFinalBlock(Array.Empty<byte>(),0,0);if(BitConverter.ToString(hash.Hash!).Replace("-","").ToLowerInvariant()!=expected)throw new InvalidOperationException("Anlam denetleyicisi bütünlük kontrolünü geçmedi.");}
        }
        private static void Kill(Process p){try{if(!p.HasExited)p.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}
        public void Dispose(){disposed=true;var p=process;if(p!=null)Kill(p);input?.Dispose();p?.Dispose();}
    }
}
