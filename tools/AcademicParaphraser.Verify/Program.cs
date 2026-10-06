using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.RuleEngine;
using AcademicParaphraser.Infrastructure.InternetDictionaryProviders;
using AcademicParaphraser.Infrastructure.Persistence;
using AcademicParaphraser.Infrastructure.TurkishNlp;
using Newtonsoft.Json;

// Developer verification only. It never reads a user's Word document.
string java = Environment.GetEnvironmentVariable("ACADEMIC_JAVA") ?? throw new ArgumentException("ACADEMIC_JAVA gerekli.");
string jar = Environment.GetEnvironmentVariable("ACADEMIC_NLP_JAR") ?? throw new ArgumentException("ACADEMIC_NLP_JAR gerekli.");
if (args.Any(a => a != "--online-dictionary")) throw new ArgumentException("Yalnızca --online-dictionary seçeneği desteklenir.");
string folder = Path.Combine(Path.GetTempPath(), "AcademicVerify-" + Guid.NewGuid().ToString("N"));
try
{
    var repository = new LocalRepository(Path.Combine(folder, "verify.sqlite"), new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
    repository.LockTerm("öznel zindelik");
    using var nlp = new ZemberekProcess(java, jar);
    var engine = new TransformationEngine(repository, nlp);
    string regression = "Doğayla ilişkinin öznel zindelik üzerindeki etkisinin anlamlı olduğu belirlenmiştir (Yılmaz & Demir, 2024; β=.43, p<.001).";
    string paragraph = "Bu çalışmada veriler anket yoluyla toplanmıştır. Araştırmacılar verileri analiz etmiştir (Yılmaz, 2024). Bulgular tespit edilmiştir.";
    var cases = new[] { (Name: "1 cümle — şartnamedeki koruma örneği", Text: regression), (Name: "1 paragraf — doğrulama girdisi", Text: paragraph), (Name: "10 paragraf — doğrulama girdisi", Text: string.Join("\r", Enumerable.Repeat(paragraph, 10))) };
    var results = new List<object>();
    foreach (var sample in cases)
    {
        foreach (Strength strength in Enum.GetValues<Strength>())
        {
            var stopwatch = Stopwatch.StartNew();
            var alternatives = await engine.GenerateAsync(sample.Text, new UserSettings { DefaultStrength = strength }, null, CancellationToken.None);
            stopwatch.Stop();
            results.Add(new
            {
                sample.Name,
                Strength = strength.ToString(),
                Characters = sample.Text.Length,
                Milliseconds = stopwatch.ElapsedMilliseconds,
                Original = sample.Text,
                Alternatives = alternatives.Select(c => new { c.Text, Edits = c.Edits.Count, c.Score }).ToList()
            });
        }
    }
    var lookups = new List<DictionaryResult>();
    if (args.Contains("--online-dictionary"))
    {
        using var tr = new TurkishWiktionaryProvider();
        using var en = new EnglishWiktionaryProvider();
        var dictionary = new DictionaryService(repository, new IDictionaryProvider[] { tr, en });
        lookups.AddRange(await dictionary.LookupAsync("yöntem", new UserSettings { InternetEnabled = true, OfflineMode = false }, CancellationToken.None));
    }
    Directory.CreateDirectory("artifacts");
    var report = new
    {
        CreatedUtc = DateTime.UtcNow.ToString("O"),
        Engine = nlp.Status,
        Environment = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        RuleCount = repository.GetRules().Count,
        LexiconCount = repository.GetLexicon().Count,
        WordExecuted = false,
        Cases = results,
        Dictionary = lookups
    };
    File.WriteAllText("artifacts/verification.json", JsonConvert.SerializeObject(report, Formatting.Indented));
    Console.WriteLine("Gerçek motor doğrulama raporu: artifacts/verification.json");
    Console.WriteLine("Bu rapor Word/VSTO çalıştırıldığını göstermez.");
}
finally
{
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    if (Directory.Exists(folder))
        Directory.Delete(folder, true);
}
