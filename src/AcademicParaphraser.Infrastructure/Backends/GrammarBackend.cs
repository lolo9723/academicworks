using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core.Backends;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace AcademicParaphraser.Infrastructure.Backends
{
    public sealed class SeparateGrammarBackend : IGrammarBackend,IEnglishMorphologyBackend
    {
        private readonly string java,jar;private readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
        private Process? process;private StreamWriter? writer;private bool disposed;
        public long WorkingSetBytes { get {try{var p=process;if(p==null||p.HasExited)return 0;p.Refresh();return p.WorkingSet64;}catch(InvalidOperationException){return 0;}} }
        public SeparateGrammarBackend(string java,string jar){this.java=java;this.jar=jar;}
        public async Task<IReadOnlyList<GrammarIssue>> CheckAsync(LinguisticAnalysis analysis,CancellationToken cancellation)
        {
            if(disposed)throw new ObjectDisposedException(nameof(SeparateGrammarBackend));
            if(analysis.Language=="tr")return CheckTurkish(analysis);
            var result=await RequestEnglishAsync(new{text=analysis.Text},cancellation).ConfigureAwait(false);
            if(result["issues"]==null)throw new InvalidOperationException("Dil bilgisi yanıtı geçersiz.");
            return result["issues"]!.ToObject<List<GrammarIssue>>()!.Where(i=>i.Kind!="style"&&i.Kind!="locale-violation"&&i.Kind!="register").ToArray();
        }
        public async Task RefineVerbLemmasAsync(LinguisticAnalysis analysis,CancellationToken cancellation)
        {
            if(analysis.Language!="en")return;
            var words=analysis.Sentences.SelectMany(s=>s.Words).Where(w=>w.Pos=="VERB"&&w.Form.Length<=100).ToList();
            if(words.Count==0)return;
            if(words.Count>200)throw new ArgumentException("Çok fazla fiil çözümleme isteği.");
            var response=await RequestEnglishAsync(new{lemmaWords=words.Select(w=>w.Form).ToArray()},cancellation).ConfigureAwait(false);
            var entries=response["lemmaEntries"] as JArray;
            if(entries==null||entries.Count!=words.Count)throw new InvalidOperationException("İngilizce fiil sözlüğü yanıtı geçersiz.");
            for(int i=0;i<words.Count;i++)
            {
                var lemmas=entries[i].Children<JObject>().Select(e=>e.Value<string>("lemma")).Where(s=>!string.IsNullOrWhiteSpace(s)&&s!.Length<=100).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                // Preserve learned interpretation when the independent dictionary is
                // ambiguous. This does not replace words or invent lexical senses.
                if(lemmas.Count==1)words[i].Lemma=lemmas[0]!;
            }
            analysis.Backend+=" + LanguageTool 6.6 unambiguous verb dictionary";
        }
        private async Task<JObject> RequestEnglishAsync(object request,CancellationToken cancellation)
        {
            if(disposed)throw new ObjectDisposedException(nameof(SeparateGrammarBackend));
            await gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                await StartAsync(cancellation).ConfigureAwait(false);
                using(var registration=cancellation.Register(Kill))
                {
                    await writer!.WriteLineAsync(JsonConvert.SerializeObject(request)).ConfigureAwait(false);await writer.FlushAsync().ConfigureAwait(false);
                    var line=process!.StandardOutput.ReadLineAsync();var deadline=Task.Delay(TimeSpan.FromSeconds(60),cancellation);
                    if(await Task.WhenAny(line,deadline).ConfigureAwait(false)!=line){Kill();cancellation.ThrowIfCancellationRequested();throw new InvalidOperationException("Dil bilgisi denetimi zaman aşımına uğradı.");}
                    cancellation.ThrowIfCancellationRequested();var result=JObject.Parse(await line.ConfigureAwait(false)??"{}");
                    if(result["error"]!=null)throw new InvalidOperationException("Dil bilgisi/sözcük çözümlemesi tamamlanamadı.");
                    return result;
                }
            }
            catch(Exception) when(cancellation.IsCancellationRequested){throw new OperationCanceledException(cancellation);}
            finally{gate.Release();}
        }
        private static IReadOnlyList<GrammarIssue> CheckTurkish(LinguisticAnalysis analysis)
        {
            var issues=new List<GrammarIssue>();
            foreach(var word in analysis.Morphology.Where(w=>w.RuntimeGuess&&!w.Proper&&w.Surface.Length>=4&&w.Surface.All(char.IsLetter)))
                issues.Add(new GrammarIssue{Code="TR_UNKNOWN_MORPHOLOGY",Kind="spelling",Start=word.Start,Length=word.Length,Message="Sözcüğün bilinen Türkçe çekimi bulunamadı."});
            foreach(var sentence in analysis.Sentences)
            {
                var root=sentence.Root;if(root==null){issues.Add(new GrammarIssue{Code="TR_MISSING_ROOT",Kind="grammar",Message="Cümlede tek ana yüklem bulunamadı."});continue;}
                foreach(var predicate in sentence.Words.Where(w=>w.Pos=="VERB"&&w.Features.TryGetValue("VerbForm",out var form)&&form=="Fin"))
                {
                    var subject=sentence.Words.SingleOrDefault(w=>w.Head==predicate.Id&&w.Relation=="nsubj"&&w.Pos=="PRON");
                    if(subject!=null&&subject.Features.TryGetValue("Person",out var person)&&predicate.Features.TryGetValue("Person",out var verbPerson)&&person!=verbPerson)
                        issues.Add(new GrammarIssue{Code="TR_PERSON_AGREEMENT",Kind="grammar",Start=predicate.Start,Length=predicate.Length,Message="Açık zamir özne ile yüklemin kişi eki uyuşmuyor."});
                }
            }
            return issues;
        }
        private async Task StartAsync(CancellationToken cancellation)
        {
            if(process!=null&&!process.HasExited)return;
            writer?.Dispose();process?.Dispose();
            if(!File.Exists(java)||!File.Exists(jar))throw new InvalidOperationException("İngilizce dil bilgisi bileşeni bulunamadı. Kurulumu onarın.");
            process=new Process{StartInfo=new ProcessStartInfo(java,"-Dfile.encoding=UTF-8 -Xmx512m -jar \""+jar+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8}};
            process.ErrorDataReceived+=(s,e)=>{};process.Start();process.BeginErrorReadLine();writer=new StreamWriter(process.StandardInput.BaseStream,new UTF8Encoding(false),4096,true);
            using(var registration=cancellation.Register(Kill))
            {
                var ready=process.StandardOutput.ReadLineAsync();if(await Task.WhenAny(ready,Task.Delay(TimeSpan.FromSeconds(60),cancellation)).ConfigureAwait(false)!=ready){Kill();cancellation.ThrowIfCancellationRequested();throw new InvalidOperationException("İngilizce dil bilgisi bileşeni başlatılamadı.");}
                cancellation.ThrowIfCancellationRequested();if(JObject.Parse(await ready.ConfigureAwait(false)??"{}").Value<bool?>("ready")!=true)throw new InvalidOperationException("Dil bilgisi başlangıç yanıtı geçersiz.");
            }
        }
        private void Kill(){try{if(process!=null&&!process.HasExited)process.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}}
        public void Dispose(){disposed=true;Kill();writer?.Dispose();process?.Dispose();}
    }
}
