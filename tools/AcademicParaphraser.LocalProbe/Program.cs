using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Rewriting;
using AcademicParaphraser.Infrastructure.LocalRewriting;
using AcademicParaphraser.Infrastructure.LexicalKnowledge;
using AcademicParaphraser.Infrastructure.Persistence;
using AcademicParaphraser.Infrastructure.TurkishNlp;

if (args.Length == 2 && args[0] == "--download-model")
{
    var download = new LocalModelStore(Path.GetFullPath(args[1]));
    await download.DownloadAsync(new Progress<string>(s => Console.WriteLine(s)),CancellationToken.None);
    Console.WriteLine("Pinned model verified: " + LocalModelStore.Sha256);return;
}
if (args.Length != 5 && !(args.Length == 6 && args[5] == "--inspect")) throw new ArgumentException("llama-server model-directory java nlp.jar output-directory");
string output = Path.GetFullPath(args[4]); Directory.CreateDirectory(output);
const string corpusPath = "tests/fixtures/local-rewrite-v1.json";
const string corpusSha = "a38b41287a8af00026c5e3b201f4eaf2f74019da7a0bcc129fa6a84e8a9c7187";
if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(corpusPath))).ToLowerInvariant() != corpusSha) throw new InvalidOperationException("Frozen corpus differs");
var fixtures = JsonSerializer.Deserialize<List<Fixture>>(File.ReadAllText(corpusPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
if(args.Length==6&&args[5]=="--inspect")
{
    using var parser=new ZemberekProcess(Path.GetFullPath(args[2]),Path.GetFullPath(args[3]));
    foreach(var f in fixtures){var tokens=await parser.AnalyzeAsync(f.Text,CancellationToken.None);Console.WriteLine(JsonSerializer.Serialize(new{f.Id,words=tokens.Select(t=>new{t.Surface,t.Lemma,t.Pos,t.Proper,t.Morphemes})}));}return;
}
var store = new LocalModelStore(Path.GetFullPath(args[1]));
await store.VerifyAsync(CancellationToken.None);
var model = new LocalLlamaModel(Path.GetFullPath(args[0]), store) { SyntheticDiagnosticSink = s => { File.AppendAllText(Path.Combine(output,"synthetic-model-errors.log"),s+Environment.NewLine); Console.WriteLine(s); } };
string temporary = Path.Combine(Path.GetTempPath(), "AcademicLocalProbe-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
var records = new List<object>();
long peakRss = 0;
ZemberekProcess? activeNlp = null;
using var polling = new CancellationTokenSource();
var memory = Task.Run(async () => { while (!polling.IsCancellationRequested) { long total = Process.GetCurrentProcess().WorkingSet64 + model.CurrentWorkingSetBytes + (activeNlp?.WorkingSetBytes ?? 0); peakRss = Math.Max(peakRss,total); try { await Task.Delay(250,polling.Token); } catch (OperationCanceledException) { } } });
try
{
    var repo = new LocalRepository(Path.Combine(temporary,"probe.sqlite"),new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
    using var nlp = new ZemberekProcess(Path.GetFullPath(args[2]),Path.GetFullPath(args[3]));
    activeNlp = nlp;
    var knowledge = new WordNetLexicalSource("artifacts/lexical-data/kenet.sqlite");
    var engine = new LocalRewriteEngine(repo,nlp,model,knowledge);
    foreach (var f in fixtures)
    {
        var timer = Stopwatch.StartNew(); var diagnostics = new RewriteDiagnostics();
        Console.WriteLine("START " + f.Id);
        var result = await engine.GenerateAsync(f.Text,new UserSettings {DefaultStrength=Strength.Strong},new[]{new TextSpan{Start=0,Length=f.Text.Length}},Array.Empty<TextSpan>(),CancellationToken.None,new Progress<string>(s=>Console.WriteLine(f.Id+": "+s)),diagnostics);
        records.Add(new { f.Id,f.Language,original=f.Text, outputs=result.Select(c=>c.Text).ToArray(),elapsedSeconds=timer.Elapsed.TotalSeconds,diagnostics});
        Console.WriteLine("OUTPUT " + JsonSerializer.Serialize(records.Last()));
        File.WriteAllText(Path.Combine(output,"local-model-partial.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
    }
    // Genuine model review of adversarial semantic traps, unrelated to user's private examples.
    var traps = new[]{
        new[]{"Dosyanın onaylanması planlanmaktadır.","Dosya onaylanmıştır."},
        new[]{"Raporda iki değişken arasında ilişki bulundu.","Rapora göre ilk değişken ikincisini etkiledi."},
        new[]{"Komisyon başvuruyu değerlendirmemiştir.","Komisyon başvuruyu değerlendirmiştir."},
        new[]{"Ürünle ilgili endişelerim dayanıklılık sorunlarından kaynaklanıyor.","Dayanıklılığına ilişkin endişelerim nedeniyle ürünü kullanmayı bıraktım."},
        new[]{"The team found no evidence of contamination.","The team proved that contamination never occurred."},
        new[]{"Bu ölçüm, parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağını göstermemektedir.","Parçanın uzun süreli kullanımda aynı sıcaklıkta kalacağı söylenmemektedir."}
    };
    var reviews=new List<object>();int falseAccepts=0;
    using(var session=await model.OpenAsync(CancellationToken.None))
        foreach(var trap in traps){var review=await session.ReviewAsync(trap[0],trap[1],CancellationToken.None);if(review.Accepted)falseAccepts++;reviews.Add(new{source=trap[0],proposal=trap[1],review});Console.WriteLine("TRAP "+JsonSerializer.Serialize(reviews.Last()));}
    var positivePairs=new[]{
        new[]{"Toplantı sırasında katılımcılara sonuçlar sunuldu.","Katılımcılara sonuçlar toplantı sırasında sunuldu."},
        new[]{"Projeyle çevresel etkilerin ölçülmesi hedeflenmektedir.","Proje, çevresel etkileri ölçmeyi hedeflemektedir."},
        new[]{"The team completed the report yesterday.","Yesterday, the report was completed by the team."},
        new[]{"Gözlemde kirlenmeye dair bulguya rastlanmadı.","Gözlem, herhangi bir kirlenme bulgusu ortaya koymadı."}
    };
    var positiveReviews=new List<object>();int falseRejects=0;
    using(var session=await model.OpenAsync(CancellationToken.None))
        foreach(var pair in positivePairs){var review=await session.ReviewAsync(pair[0],pair[1],CancellationToken.None);if(!review.Accepted)falseRejects++;positiveReviews.Add(new{source=pair[0],proposal=pair[1],review});Console.WriteLine("POSITIVE "+JsonSerializer.Serialize(positiveReviews.Last()));}
    bool cancellationStopped=false;
    using(var cancel=new CancellationTokenSource())
    {
        using(var session=await model.OpenAsync(cancel.Token))
        {
            var text=fixtures[0].Text;
            var plan=RewritePlan.Create(text,new[]{new TextSpan{Start=0,Length=text.Length}},Array.Empty<TextSpan>());
            cancel.CancelAfter(80);
            try{await session.RewriteAsync(plan,Strength.Strong,"",cancel.Token);}
            catch(OperationCanceledException){cancellationStopped=true;}
        }
    }
    cancellationStopped=cancellationStopped&&model.CurrentWorkingSetBytes==0;
    var report=new{model=store.ModelName,modelSha256=store.ExpectedSha256,corpusSha256=corpusSha,sourceIsSynthetic=true,paragraphSentExternally=false,gpuUsed=false,wordExecuted=false,os=Environment.OSVersion.ToString(),processorCount=Environment.ProcessorCount,peakCombinedWorkingSetMiB=peakRss/1048576.0,peakModelWorkingSetMiB=model.PeakWorkingSetBytes/1048576.0,records,reviews,falseAccepts,positiveReviews,falseRejects,cancellationStopped};
    File.WriteAllText(Path.Combine(output,"local-model-verification.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
    // Do not assert every proposal is human quality; retain zero-output cases and critic errors.
    if(!cancellationStopped)throw new InvalidOperationException("Owned local model cancellation did not complete.");
    if(falseAccepts>0)throw new InvalidOperationException("Reviewer falsely accepted an adversarial meaning change; see actual evidence.");
}
finally{polling.Cancel();await memory;Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(temporary,true);}
sealed class Fixture {public string Id{get;set;}="";public string Language{get;set;}="";public string Text{get;set;}="";}
