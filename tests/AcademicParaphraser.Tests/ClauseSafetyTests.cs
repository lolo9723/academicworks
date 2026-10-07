using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.CitationProtection;
using AcademicParaphraser.Core.Rewriting;
using AcademicParaphraser.Core.RuleEngine;
using AcademicParaphraser.Infrastructure.Persistence;
using Xunit;

namespace AcademicParaphraser.Tests
{
    public sealed class ClauseSafetyTests : IClassFixture<NlpFixture>, IDisposable
    {
        private readonly NlpFixture fixture;
        private readonly string directory = Path.Combine(Path.GetTempPath(), "ClauseTests-" + Guid.NewGuid().ToString("N"));
        private readonly LocalRepository repo;
        public ClauseSafetyTests(NlpFixture fixture)
        {
            this.fixture = fixture;
            repo = new LocalRepository(Path.Combine(directory, "personal.sqlite"), new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
        }
        public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
        private Task<IReadOnlyList<Candidate>> Rewrite(string source, IEnumerable<TextSpan>? spans = null) => new TransformationEngine(repo, fixture.Nlp).GenerateAsync(source, new UserSettings { DefaultStrength = Strength.Strong }, spans, CancellationToken.None);

        [Theory]
        [InlineData("süreç", "Gen", "sürecin")]
        [InlineData("savunması", "Gen", "savunmasının")]
        [InlineData("kalınlığı", "Gen", "kalınlığının")]
        [InlineData("göstermemektedir", "Passive", "gösterilmemektedir")]
        [InlineData("göstermiştir", "Passive", "gösterilmiştir")]
        [InlineData("süreçleri", "Gen", "süreçlerinin")]
        [InlineData("incelenirken", "Nominal", "incelenmesi")]
        [InlineData("ölçülürken", "Nominal", "ölçülmesi")]
        [InlineData("karşılaştırılırken", "Nominal", "karşılaştırılması")]
        [InlineData("hesaplanırken", "Nominal", "hesaplanması")]
        public async Task RealGeneratorPreservesRequiredSuffixes(string source, string mode, string expected) => Assert.Equal(expected, await fixture.Nlp.TransformStructureAsync(source, mode, CancellationToken.None));

        [Theory]
        [InlineData("doğrulamamıştır", "Passive")]
        [InlineData("Ankara", "Gen")]
        [InlineData("incelenmezken", "Nominal")]
        [InlineData("artarken", "Nominal")]
        [InlineData("gösterilmektedir", "Passive")]
        public async Task UnlicensedOrAmbiguousInflectionsAreRefused(string source, string mode) => Assert.Null(await fixture.Nlp.TransformStructureAsync(source, mode, CancellationToken.None));

        [Fact]
        public async Task ContextualVerbReadingCannotSwitchNegationToInability()
        {
            var words = await fixture.Nlp.AnalyzeAsync("Veriler bu iddiayı doğrulamamıştır.", CancellationToken.None);
            var predicate = words.Single(t => t.Surface == "doğrulamamıştır");
            Assert.Equal("doğrulamak", predicate.Lemma);
            Assert.Equal("doğrulanmamıştır", await fixture.Nlp.TransformStructureAsync(predicate, "Passive", CancellationToken.None));
        }

        [Theory]
        [InlineData("Bu incelemede eserler değerlendirilmiştir. Listede bu bilgiler korunmuştur.", "Bu")]
        [InlineData("Öğrencilerin çizimleri incelenmiştir. İkinci listede öğrencilerin adları korunmuştur.", "Öğrencilerin")]
        public async Task CapitalizedFirstOccurrenceCannotJumpToLaterLowercaseOccurrence(string source, string first)
        {
            var words = await fixture.Nlp.AnalyzeAsync(source, CancellationToken.None);
            Assert.Equal(0, words[0].Start); Assert.Equal(first, words[0].Surface);
            Assert.Contains(words, t => t.Surface == "incelenmiştir" || t.Surface == "değerlendirilmiştir");
            Assert.All(words, t => Assert.Equal(t.Surface, source.Substring(t.Start, t.Length)));
            Assert.Equal(words.Select(t => t.Start).OrderBy(i => i), words.Select(t => t.Start));
        }
        [Theory]
        [InlineData("Bulgular, ritmin dinleyicileri etkilemediğini göstermektedir.", "etkilemediği", "gösterilmektedir")]
        [InlineData("Veriler, bu farklılığın kalıcı olduğunu doğrulamamıştır.", "olduğu", "doğrulanmamıştır")]
        public async Task ComplementReconstructionPreservesBothNegations(string source, string complement, string predicate)
        {
            var candidates = await Rewrite(source);
            Assert.Contains(candidates, c => c.RewrittenSentences == 1 && c.Text.Contains(complement + ",") && c.Text.Contains(predicate));
            foreach (var c in candidates) Assert.True(MeaningSignalGuard.Preserved(source, c.Text, await fixture.Nlp.AnalyzeAsync(source, CancellationToken.None), await fixture.Nlp.AnalyzeAsync(c.Text, CancellationToken.None)));
        }
        [Fact]
        public async Task ConcessionCanBeSplitWithoutDroppingNegativeFinding()
        {
            const string source = "Arşiv belgeleri okunmuş olmakla birlikte, tarihler doğrulanmamıştır.";
            var candidates = await Rewrite(source);
            Assert.Contains(candidates, c => c.Text.Contains("okunmuştur; bununla birlikte tarihler doğrulanmamıştır"));
        }
        [Fact]
        public async Task ProceduralWhileUsesPossessiveNominalization()
        {
            const string source = "Gövde kalınlığı ölçülürken, ses frekansı kaydedilmiştir.";
            var candidates = await Rewrite(source);
            Assert.Contains(candidates, c => c.Text.Contains("kalınlığının ölçülmesi sırasında, ses frekansı kaydedilmiştir"));
        }
        [Fact]
        public async Task ScopeMovementKeepsMannerAdverbBesidePredicate()
        {
            const string source = "Bu incelemede el yazmaları sistematik olarak sınıflandırılmıştır.";
            var candidates = await Rewrite(source);
            Assert.Contains(candidates, c => c.Text == "El yazmaları bu incelemede sistematik olarak sınıflandırılmıştır.");
            Assert.DoesNotContain(candidates, c => c.Text.Contains("olarak, bu"));
        }
        [Theory]
        [InlineData("İlk kayıt saklanmıştır. Belgeler okunmuş olmakla birlikte, tarihler doğrulanmamıştır.")]
        [InlineData("İlk kayıt saklanmıştır. Bulgular, bu farklılığın kalıcı olduğunu göstermektedir.")]
        public async Task ClauseCannotConsumeThePreviousSentence(string source)
        {
            var candidates = await Rewrite(source);
            Assert.Contains(candidates, c => c.RewrittenSentences == 1);
            Assert.All(candidates, c => Assert.StartsWith("İlk kayıt saklanmıştır.", c.Text));
        }
        [Fact]
        public async Task ReportingVerbInsideFixedHyperlinkBlocksEntireClauseProposal()
        {
            const string source = "Bulgular, bu farklılığın kalıcı olduğunu göstermektedir.";
            int start = source.IndexOf("göstermektedir", StringComparison.Ordinal);
            var candidates = await Rewrite(source, new[] { new TextSpan { Start = start, Length = 14, Reason = "hyperlink" } });
            Assert.All(candidates, c => Assert.Equal(source, c.Text));
        }
        [Fact]
        public async Task MeaningSignalGuardRejectsLostConditionAndUncertainty()
        {
            const string a = "Yağış artarsa sonuç değişebilir.";
            const string b = "Yağış artar, sonuç değişir.";
            Assert.False(MeaningSignalGuard.Preserved(a, b, await fixture.Nlp.AnalyzeAsync(a, CancellationToken.None), await fixture.Nlp.AnalyzeAsync(b, CancellationToken.None)));
        }
        [Fact]
        public void MeaningSignalGuardRejectsMissingScopeCueEvenWithoutMorphology() => Assert.False(MeaningSignalGuard.Preserved("Yalnızca ilk ölçüm kullanılabilir.", "İlk ölçüm kullanılabilir.", Array.Empty<MorphToken>(), Array.Empty<MorphToken>()));
        [Fact]
        public void DecimalAndAbbreviationDoNotSplitTheSameSentence()
        {
            const string source = "Dr. Eren 12.5 saatlik kaydı inceledi. Sonuç doğrulanmadı.";
            var spans = new ProtectionDetector().Detect(source, new UserSettings(), Array.Empty<string>(), Array.Empty<LexiconEntry>());
            Assert.Equal(2, SentenceSegmentation.Find(source, spans).Count);
        }
        [Fact]
        public async Task SlowPersonalRuleCannotAbortOtherSentenceRewrites()
        {
            repo.SaveRule(new RuleDefinition { Id = "slow-user-rule", Pattern = "(a+)+$", Target = "x", Confidence = .95 });
            string source = new string('a', 500) + "b\rArşiv belgeleri okunmuş olmakla birlikte, tarihler doğrulanmamıştır.";
            var candidates = await Rewrite(source);
            Assert.Contains(candidates, c => c.Text.Contains("okunmuştur; bununla birlikte"));
            Assert.All(candidates, c => Assert.True(c.Enrichment!.RejectedForRuleTimeouts > 0));
        }
    }
}
