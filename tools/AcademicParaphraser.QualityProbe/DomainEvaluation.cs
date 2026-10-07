using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.CitationProtection;
using AcademicParaphraser.Core.Rewriting;
using AcademicParaphraser.Core.RuleEngine;

internal static class DomainEvaluation
{
    public static async Task RunAsync(TransformationEngine engine, ITurkishNlp nlp, string directory)
    {
        using var stream = typeof(DomainEvaluation).Assembly.GetManifestResourceStream("multidomain-v1.json")!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes);
        byte[] corpus = bytes.ToArray();
        var fixtures = JsonSerializer.Deserialize<Fixture[]>(corpus, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var reports = new List<object>(); bool allPassed = true; var settings = new UserSettings { DefaultStrength = Strength.Strong };
        foreach (var fixture in fixtures)
        {
            var clock = Stopwatch.StartNew();
            var candidates = await engine.GenerateAsync(fixture.Source, settings, null, CancellationToken.None);
            long elapsed = clock.ElapsedMilliseconds;
            var before = await nlp.AnalyzeAsync(fixture.Source, CancellationToken.None);
            var protections = new ProtectionDetector().Detect(fixture.Source, settings, Array.Empty<string>(), Array.Empty<LexiconEntry>());
            var sentences = SentenceSegmentation.Find(fixture.Source, protections);
            var outputs = new List<object>();
            foreach (var candidate in candidates)
            {
                var after = await nlp.AnalyzeAsync(candidate.Text, CancellationToken.None);
                // Ordinary terms may begin a new sentence with a capital letter.
                // Exact protected spans (including proper names) are checked separately.
                bool anchors = fixture.Anchors.All(anchor => Count(fixture.Source, anchor) == Count(candidate.Text, anchor));
                bool fixedSpans = !candidate.Edits.Any(e => protections.Any(p => p.Intersects(e.Start, e.Length)));
                bool signals = MeaningSignalGuard.Preserved(fixture.Source, candidate.Text, before, after);
                bool edits = EditApplication.Apply(fixture.Source, candidate.Edits) == candidate.Text && !candidate.Edits.Any(e => e.RuleId.StartsWith("lemma:"));
                var unchanged = sentences.Where(s => !candidate.Edits.Any(e => s.Intersects(e.Start, e.Length))).Select(s => fixture.Source.Substring(s.Start, s.Length).Trim()).ToArray();
                bool passed = anchors && fixedSpans && signals && edits && candidate.RewrittenSentences >= fixture.MinimumChangedSentences;
                allPassed &= passed;
                outputs.Add(new { candidate.Text, candidate.SentenceCount, candidate.RewrittenSentences, anchorsPreserved = anchors, protectedSpansUntouched = fixedSpans, meaningSignalsPreserved = signals, editApplicationVerified = edits, unchangedSentences = unchanged, ruleIds = candidate.Edits.Select(e => e.RuleId).Distinct(), candidate.Enrichment, passed });
            }
            reports.Add(new { fixture.Id, fixture.Domain, fixture.Source, sourceSha256 = Hash(Encoding.UTF8.GetBytes(fixture.Source)), elapsedMilliseconds = elapsed, outputs });
            Console.WriteLine(JsonSerializer.Serialize(new { fixture.Id, elapsed, changed = candidates[0].RewrittenSentences, sentences = candidates[0].SentenceCount }));
            Console.WriteLine(candidates[0].Text);
        }
        using var self = Process.GetCurrentProcess(); long javaBytes = 0;
        foreach (var process in Process.GetProcessesByName("java")) { using (process) try { javaBytes += process.WorkingSet64; } catch (InvalidOperationException) { } }
        var report = new { corpusSha256 = Hash(corpus), sourceIsSynthetic = true, originalUserTextUsed = false, wordExecuted = false, paragraphSent = false, modelUsed = false, wordChoiceEnabled = false, semanticEquivalenceProved = false, manualReviewRequired = true, runtimeClauseSchemas = ClauseStructureCatalog.Create().Count, memory = new { dotnetWorkingSetBytes = self.WorkingSet64, observedJavaProcessesWorkingSetBytes = javaBytes, javaHeapLimitMiB = 512, wordProcessMeasured = false }, domains = reports, passed = allPassed };
        File.WriteAllText(Path.Combine(directory, "domain-verification.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        if (!allPassed) throw new InvalidOperationException("Multidomain preservation/coverage verification failed; inspect the report.");
    }
    private static int Count(string text, string token) { int count = 0, start = 0; var tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR"); text = text.ToLower(tr); token = token.ToLower(tr); while ((start = text.IndexOf(token, start, StringComparison.Ordinal)) >= 0) { count++; start += token.Length; } return count; }
    private static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    private sealed class Fixture
    {
        public string Id { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Source { get; set; } = "";
        public string[] Anchors { get; set; } = Array.Empty<string>();
        public int MinimumChangedSentences { get; set; }
    }
}
