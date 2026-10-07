using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.RuleEngine;
using AcademicParaphraser.Infrastructure.LexicalKnowledge;
using AcademicParaphraser.Infrastructure.Persistence;
using AcademicParaphraser.Infrastructure.TurkishNlp;
using AcademicParaphraser.Infrastructure.InternetDictionaryProviders;
using AcademicParaphraser.Core.Rewriting;

if(args.Length != 4 && !(args.Length == 5 && (args[4] == "--online" || args[4] == "--clinical" || args[4] == "--domains")) && !(args.Length == 6 && args[4] == "--input")) throw new ArgumentException("wordnet.sqlite java nlp.jar output-folder [--online | --clinical | --domains | --input local-file] required");
string output = Path.GetFullPath(args[3]); Directory.CreateDirectory(output);
string temporary = Path.Combine(Path.GetTempPath(), "AcademicQuality-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporary);
try {
    var repo = new LocalRepository(Path.Combine(temporary, "probe.sqlite"), new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
    using var nlp = new ZemberekProcess(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]));
    if (args.Length == 5 && args[4] == "--online")
    {
        using var turkish = new TurkishWiktionaryProvider();
        using var english = new EnglishWiktionaryProvider();
        using var termProvider = new WikidataTermProvider();
        var dictionary = new DictionaryService(repo, new IDictionaryProvider[] { turkish, english, termProvider });
        var settings = new UserSettings { InternetEnabled = true, OfflineMode = false, DefaultStrength = Strength.Strong };
        var lookups = new List<object>(); int found = 0;
        string[] terms = { "yöntem", "örneklem", "parafraz", "ontoloji", "metot" };
        foreach (string term in terms)
        {
            var results = await dictionary.LookupAsync(term, settings, CancellationToken.None);
            found += results.Count(r => r.Error.Length == 0 && r.Content.Length > 0);
            lookups.AddRange(results.Select(r => new { term, r.Source, r.Error, r.NotFound, r.SingleSense, r.PosTags, r.Synonyms }));
        }
        var context = new EnrichmentContext();
        var knowledgeOnline = new WordNetLexicalSource(Path.GetFullPath(args[0]), dictionary);
        await knowledgeOnline.FindAsync("", terms.Select(t => new MorphToken { Lemma = t, Pos = "Noun" }).ToArray(), settings, CancellationToken.None, context);
        using var structures = new OnlineStructureSource(repo);
        const string original = "Mevcut incelemede yanıtlar analiz edilmemiştir (Alfa, 2022; N=120).";
        var proposals = await new TransformationEngine(repo, nlp, null, structures).GenerateAsync(original, settings, null, CancellationToken.None);
        bool rewritten = proposals.Any(c => c.Text == "Yanıtlar mevcut incelemede çözümlenmemiştir (Alfa, 2022; N=120)." && c.Edits.Any(e => e.RuleId.StartsWith("online-frame:")));
        var onlineReport = new { sourceIsSynthetic = true, wordExecuted = false, apiKeyUsed = false, paragraphSent = false, dictionaryAvailable = found > 0, foundResponses = found, lookups, lexicalScan = context.Summary, structures = proposals[0].Enrichment, original, outputs = proposals.Select(c => c.Text), passed = rewritten && proposals[0].Enrichment!.StructureInventory == 25104 && context.Summary.RootsChecked == terms.Length };
        File.WriteAllText(Path.Combine(output, "online-verification.json"), JsonSerializer.Serialize(onlineReport, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(onlineReport));
        if (!onlineReport.passed) throw new InvalidOperationException("Live verified structure retrieval/rewrite failed");
        return;
    }
    var knowledge = new WordNetLexicalSource(Path.GetFullPath(args[0]));
    var engine = new TransformationEngine(repo, nlp, knowledge);
    if (args.Length == 5 && args[4] == "--domains")
    {
        await DomainEvaluation.RunAsync(engine, nlp, output);
        return;
    }
    string source = "İçerik analizi, ziyaretçi deneyimlerini incelemenin en etkili yöntemlerinden biridir. Ziyaretçi deneyimlerini anlamayı mümkün kıldığı için bu araştırmada içerik analizi kullanılmıştır. Ziyaretçilerin çevrimiçi yorumları incelenmiştir. Araştırma alanı olarak 12 destinasyon belirlenmiştir. Bu yorumlarda deneyimlerin nasıl temsil edildiği analiz edilmiştir. Bu araştırmanın amacı, ziyaretçi deneyimlerini incelemektir (Alfa, 2022; N=120).";
    bool clinical = args.Length == 5 && args[4] == "--clinical";
    if (clinical) source = "Bu araştırmanın amacı, hasta dosyalarını kullanarak Ortodonti ve Periodontoloji bölümleri arasındaki iletişimi incelemektir. Araştırmanın ikincil amacı ise konsültasyon yanıtlarını uzman ve lisansüstü öğrenci gruplarına göre karşılaştırmaktır. Tıp fakültesi arşivindeki 286 hastaya ait konsültasyon kayıtları retrospektif olarak incelenmiş ve kategorize edilmiştir.";
    if (args.Length == 6) source = File.ReadAllText(args[5]);
    var clock = Stopwatch.StartNew();
    var candidates = await engine.GenerateAsync(source, new UserSettings { DefaultStrength = Strength.Strong }, null, CancellationToken.None);
    long coldMs = clock.ElapsedMilliseconds;
    if(candidates.Count == 0) throw new InvalidOperationException("No genuine paragraph output");
    string paragraphs = string.Join("\r", Enumerable.Repeat(source,10));
    clock.Restart();
    var batch = await engine.GenerateAsync(paragraphs, new UserSettings { DefaultStrength = Strength.Strong }, null, CancellationToken.None);
    long batchMs = clock.ElapsedMilliseconds;
    var processes = Process.GetProcessesByName("java");
    long javaBytes = 0;
    foreach(var process in processes) { using(process) try { javaBytes += process.WorkingSet64; } catch(InvalidOperationException) { } }
    using var self = Process.GetCurrentProcess();
    var memory = new { dotnetWorkingSetBytes = self.WorkingSet64, observedJavaProcessesWorkingSetBytes = javaBytes, javaHeapLimitMiB = 512, wordProcessMeasured = false };
    var outputs = candidates.Select(c => new { c.Text, changes = c.Edits.Count, ruleIds = c.Edits.Select(e => e.RuleId), c.SentenceCount, c.RewrittenSentences, structural = c.Score.StructuralDifference, lexical = c.Score.LexicalDifference }).ToList();
    bool clinicalPassed = candidates.All(c => c.RewrittenSentences == 3 && c.SentenceCount == 3 && c.Text.Contains("Ortodonti ve Periodontoloji") && c.Text.Contains("Tıp fakültesi") && c.Text.Contains("286") && c.Text.Contains("konsültasyon") && !c.Text.Contains("konsulto") && !c.Text.Contains("sekunder") && !c.Text.Contains("tabip") && !c.Edits.Any(e => e.RuleId.StartsWith("lemma:")));
    var report = new { sourceIsSynthetic = args.Length != 6, wordExecuted = false, wordChoiceEnabled = false, dictionary = knowledge.Describe(), rules = repo.GetRules().Count, coldParagraphMilliseconds = coldMs, warmTenParagraphsMilliseconds = batchMs, memory, source, outputs, batchChanges = batch[0].Edits.Count, passed = clinical ? clinicalPassed : candidates[0].RewrittenSentences >= (args.Length == 6 ? 1 : 4) && (args.Length == 6 || candidates.All(c => c.Text.Contains("(Alfa, 2022; N=120)") && c.Text.Contains("12 destinasyon"))) && batch[0].Text.Count(c=>c=='\r')==9 };
    File.WriteAllText(Path.Combine(output, clinical ? "clinical-verification.json" : "quality-verification.json"), JsonSerializer.Serialize(report,new JsonSerializerOptions { WriteIndented=true }));
    Console.WriteLine(JsonSerializer.Serialize(new { report.passed, report.rules, report.dictionary, coldMs, batchMs, memory }));
    foreach(var item in outputs) Console.WriteLine(item.Text);
    if(!report.passed) throw new InvalidOperationException("Paragraph coverage/protection check failed");
} finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(temporary,true); }
