using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Backends;
using AcademicParaphraser.Infrastructure.Backends;
using AcademicParaphraser.Infrastructure.Persistence;
using AcademicParaphraser.Infrastructure.TurkishNlp;
using AcademicParaphraser.Core.Rewriting;
if(args.Length!=7)throw new ArgumentException("udpipe model-directory java nlp.jar grammar.jar semantic-directory report-directory");
string output=Path.GetFullPath(args[6]);Directory.CreateDirectory(output);
const string fixture="tests/fixtures/local-rewrite-v1.json",fixtureSha="a38b41287a8af00026c5e3b201f4eaf2f74019da7a0bcc129fa6a84e8a9c7187";
if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fixture))).ToLowerInvariant()!=fixtureSha)throw new InvalidOperationException("Frozen corpus changed.");
var options=new JsonSerializerOptions{PropertyNameCaseInsensitive=true,WriteIndented=true};var corpus=JsonSerializer.Deserialize<List<Fixture>>(File.ReadAllText(fixture),options)!;
string temporary=Path.Combine(Path.GetTempPath(),"AcademicBackendProbe-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);
using var nlp=new ZemberekProcess(Path.GetFullPath(args[2]),Path.GetFullPath(args[3]));
using var parser=new UdpipeBackend(Path.GetFullPath(args[0]),Path.GetFullPath(args[1]),nlp);
using var grammar=new SeparateGrammarBackend(Path.GetFullPath(args[2]),Path.GetFullPath(args[4]));
using var semantic=new LocalSemanticBackend(Path.GetFullPath(args[2]),Path.GetFullPath(Path.Combine(args[5],"local-semantic-1.0.0.jar")),Path.GetFullPath(Path.Combine(args[5],"model_quantized.onnx")),Path.GetFullPath(Path.Combine(args[5],"tokenizer.json")));
long peak=0;using var poll=new CancellationTokenSource();var monitor=Task.Run(async()=>{while(!poll.IsCancellationRequested){peak=Math.Max(peak,Process.GetCurrentProcess().WorkingSet64+nlp.WorkingSetBytes+parser.WorkingSetBytes+grammar.WorkingSetBytes+semantic.WorkingSetBytes);try{await Task.Delay(100,poll.Token);}catch(OperationCanceledException){}}});
var records=new List<object>();var pairRecords=new List<object>();int falseAccepts=0,falseRejects=0,semanticFalseAccepts=0,semanticFalseRejects=0;
try
{
 var repo=new LocalRepository(Path.Combine(temporary,"probe.sqlite"),new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
 var engine=new MultiStageRewriteEngine(repo,parser,grammar,semantic:semantic);
 foreach(var f in corpus)
 {
  Console.WriteLine("START "+f.Id);var timer=Stopwatch.StartNew();var diagnostics=new MultiStageDiagnostics();
  var sourceTree=await parser.AnalyzeAsync(f.Text,f.Language,CancellationToken.None);
  var results=await engine.GenerateAsync(f.Text,new UserSettings{DefaultStrength=Strength.Strong},new[]{new TextSpan{Start=0,Length=f.Text.Length}},Array.Empty<TextSpan>(),CancellationToken.None,new Progress<string>(s=>Console.WriteLine(f.Id+": "+s)),diagnostics);
  var record=new{f.Id,f.Language,original=f.Text,outputs=results.Select(c=>c.Text).ToArray(),rewrittenSentences=results.Select(c=>c.RewrittenSentences).ToArray(),elapsedSeconds=timer.Elapsed.TotalSeconds,sourceTree,diagnostics};records.Add(record);
  Console.WriteLine("OUTPUT "+JsonSerializer.Serialize(record));File.WriteAllText(Path.Combine(output,"backend-partial.json"),JsonSerializer.Serialize(records,options));
 }
 // These evaluate the independent fact-graph backend WITHOUT construction certificates.
 // Retain positive false rejections and parse failures; do not call a trivial replay
 // mismatch a successful semantic-model evaluation.
 var pairs=new[]{
  new Pair("Dosyanın onaylanması planlanmaktadır.","Dosya onaylanmıştır.","tr",false),
  new Pair("Raporda iki değişken arasında ilişki bulundu.","Rapora göre ilk değişken ikincisini etkiledi.","tr",false),
  new Pair("Komisyon başvuruyu değerlendirmemiştir.","Komisyon başvuruyu değerlendirmiştir.","tr",false),
  new Pair("Ürünle ilgili endişelerim dayanıklılık sorunlarından kaynaklanıyor.","Dayanıklılığına ilişkin endişelerim nedeniyle ürünü kullanmayı bıraktım.","tr",false),
  new Pair("The team found no evidence of contamination.","The team proved that contamination never occurred.","en",false),
  new Pair("Bu ölçüm, parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını göstermemektedir.","Parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağı söylenmemektedir.","tr",false),
  new Pair("Toplantı sırasında katılımcılara sonuçlar sunuldu.","Katılımcılara sonuçlar toplantı sırasında sunuldu.","tr",true),
  new Pair("Projeyle çevresel etkilerin ölçülmesi hedeflenmektedir.","Proje, çevresel etkileri ölçmeyi hedeflemektedir.","tr",true),
  new Pair("The team completed the report yesterday.","Yesterday, the report was completed by the team.","en",true),
  new Pair("Gözlemde kirlenmeye dair bulguya rastlanmadı.","Gözlem, herhangi bir kirlenme bulgusu ortaya koymadı.","tr",true),
  new Pair("The auditor reviewed the file.","The file reviewed the auditor.","en",false),
  new Pair("Kurul başvuruyu reddetti.","Başvuru kurulu reddetti.","tr",false)
 };
 var meaning=new DerivationMeaningBackend();
 foreach(var pair in pairs)
 {
  var before=await parser.AnalyzeAsync(pair.Source,pair.Language,CancellationToken.None);var after=await parser.AnalyzeAsync(pair.Target,pair.Language,CancellationToken.None);
  var review=meaning.CompareFacts(before,after,repo.GetLexicon());if(review.Passed&&!pair.Equivalent)falseAccepts++;if(!review.Passed&&pair.Equivalent)falseRejects++;
  var semanticReview=await semantic.CompareAsync(pair.Source,pair.Target,pair.Language,CancellationToken.None);
  if(semanticReview.Passed&&!pair.Equivalent)semanticFalseAccepts++;if(!semanticReview.Passed&&pair.Equivalent)semanticFalseRejects++;
  pairRecords.Add(new{pair.Source,pair.Target,pair.Equivalent,review,semanticReview});Console.WriteLine("PAIR "+JsonSerializer.Serialize(pairRecords.Last()));
 }
 bool cancellationStopped=false;using(var cancel=new CancellationTokenSource())
 {
  cancel.CancelAfter(10);try{await parser.AnalyzeAsync(corpus[0].Text,"tr",cancel.Token);}catch(OperationCanceledException){cancellationStopped=true;}
 }
 cancellationStopped&=parser.WorkingSetBytes==0;
 bool semanticCancellationStopped=false;using(var cancel=new CancellationTokenSource())
 {cancel.CancelAfter(10);try{await semantic.CompareAsync(corpus[3].Text+" "+corpus[3].Text,corpus[3].Text+" "+corpus[3].Text,"tr",cancel.Token);}catch(OperationCanceledException){semanticCancellationStopped=true;}}
 semanticCancellationStopped&=semantic.WorkingSetBytes==0;
 var report=new{sourceIsSynthetic=true,generativeModelUsed=false,semanticClassificationModelUsed=true,semanticModelUsedAloneForDocumentApproval=false,paragraphSentExternally=false,userTextUsedForTraining=false,wordExecuted=false,gpuUsed=false,os=Environment.OSVersion.ToString(),corpusSha256=fixtureSha,records,pairRecords,falseAccepts,falseRejects,semanticFalseAccepts,semanticFalseRejects,cancellationStopped,semanticCancellationStopped,peakCombinedWorkingSetMiB=peak/1048576.0,peakParserWorkingSetMiB=parser.PeakWorkingSetBytes/1048576.0,modelProvenance=JsonSerializer.Deserialize<object>(File.ReadAllText(Path.Combine(args[1],"dependency-model-verification.json"))),semanticProvenance=JsonSerializer.Deserialize<object>(File.ReadAllText(Path.Combine(args[5],"semantic-model-verification.json")))};
 File.WriteAllText(Path.Combine(output,"backend-verification.json"),JsonSerializer.Serialize(report,options));
 if(!cancellationStopped)throw new InvalidOperationException("Parser cancellation left an owned process.");
 if(!semanticCancellationStopped)throw new InvalidOperationException("Semantic cancellation left an owned process.");
 if(falseAccepts>0)throw new InvalidOperationException("Independent fact graph accepted a known meaning change; see evidence.");
}
finally{poll.Cancel();await monitor;Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(temporary,true);}
sealed record Fixture(string Id,string Language,string Text);
sealed record Pair(string Source,string Target,string Language,bool Equivalent);
