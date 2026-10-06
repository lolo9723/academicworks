using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.RuleEngine;
using AcademicParaphraser.Infrastructure.LexicalKnowledge;
using AcademicParaphraser.Infrastructure.Persistence;
using AcademicParaphraser.Infrastructure.TurkishNlp;

if(args.Length != 4) throw new ArgumentException("wordnet.sqlite java nlp.jar output-folder required");
string output = Path.GetFullPath(args[3]); Directory.CreateDirectory(output);
string temporary = Path.Combine(Path.GetTempPath(), "AcademicQuality-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporary);
try {
    var repo = new LocalRepository(Path.Combine(temporary, "probe.sqlite"), new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
    using var nlp = new ZemberekProcess(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]));
    var knowledge = new WordNetLexicalSource(Path.GetFullPath(args[0]));
    var engine = new TransformationEngine(repo, nlp, knowledge);
    string source = "İçerik analizi, ziyaretçi deneyimlerini incelemenin en etkili yöntemlerinden biridir. Ziyaretçi deneyimlerini anlamayı mümkün kıldığı için bu araştırmada içerik analizi kullanılmıştır. Ziyaretçilerin çevrimiçi yorumları incelenmiştir. Araştırma alanı olarak 12 destinasyon belirlenmiştir. Bu yorumlarda deneyimlerin nasıl temsil edildiği analiz edilmiştir. Bu araştırmanın amacı, ziyaretçi deneyimlerini incelemektir (Alfa, 2022; N=120).";
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
    var outputs = candidates.Select(c => new { c.Text, changes = c.Edits.Count, ruleIds = c.Edits.Select(e => e.RuleId), structural = c.Score.StructuralDifference, lexical = c.Score.LexicalDifference }).ToList();
    var report = new { sourceIsSynthetic = true, wordExecuted = false, dictionary = knowledge.Describe(), rules = repo.GetRules().Count, coldParagraphMilliseconds = coldMs, warmTenParagraphsMilliseconds = batchMs, memory, source, outputs, batchChanges = batch[0].Edits.Count, passed = candidates[0].Edits.Count >= 6 && candidates.All(c => c.Text.Contains("(Alfa, 2022; N=120)") && c.Text.Contains("12 destinasyon")) && batch[0].Text.Count(c=>c=='\r')==9 };
    File.WriteAllText(Path.Combine(output, "quality-verification.json"), JsonSerializer.Serialize(report,new JsonSerializerOptions { WriteIndented=true }));
    Console.WriteLine(JsonSerializer.Serialize(new { report.passed, report.rules, report.dictionary, coldMs, batchMs, memory }));
    foreach(var item in outputs) Console.WriteLine(item.Text);
    if(!report.passed) throw new InvalidOperationException("Paragraph coverage/protection check failed");
} finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(temporary,true); }
