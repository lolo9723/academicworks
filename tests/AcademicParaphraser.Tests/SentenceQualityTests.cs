using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.CitationProtection;
using AcademicParaphraser.Core.RuleEngine;
using AcademicParaphraser.Infrastructure.LexicalKnowledge;
using AcademicParaphraser.Infrastructure.Persistence;
using Newtonsoft.Json;
using Xunit;

namespace AcademicParaphraser.Tests
{
    // All passages here are synthetic and public, independent of the user's document.
    public sealed class SentenceQualityTests : IClassFixture<NlpFixture>, IDisposable
    {
        private readonly NlpFixture fixture;
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SentenceTests-" + Guid.NewGuid().ToString("N"));
        private readonly LocalRepository repo;
        private readonly WordNetLexicalSource knowledge;
        public SentenceQualityTests(NlpFixture fixture)
        {
            this.fixture = fixture;
            repo = new LocalRepository(Path.Combine(directory, "personal.sqlite"), new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
            knowledge = new WordNetLexicalSource(Environment.GetEnvironmentVariable("ACADEMIC_WORDNET") ?? throw new InvalidOperationException("KeNet is required."));
        }
        public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
        private Task<IReadOnlyList<Candidate>> Rewrite(string source, IEnumerable<TextSpan>? fixedSpans = null) =>
            new TransformationEngine(repo, fixture.Nlp, knowledge).GenerateAsync(source, new UserSettings { DefaultStrength = Strength.Strong }, fixedSpans, CancellationToken.None);

        [Fact]
        public void OldSettingsUpgradeWithoutEnablingWordReplacement()
        {
            var settings = JsonConvert.DeserializeObject<UserSettings>("{\"InternetEnabled\":true,\"OfflineMode\":false,\"DefaultStrength\":3}")!;
            Assert.False(settings.EnableWordChoice);
        }
        [Theory]
        [InlineData("Konsültasyon sekonder bir klinik değerlendirme değildir.")]
        [InlineData("Diş hekimliği fakültesi klinisyenleri konsültasyon notlarını okudu.")]
        [InlineData("Araştırmanın ikincil hedefi klinisyenlerin eğitim düzeyidir.")]
        public async Task DictionaryCannotReplaceWordsInDefaultParaphrasing(string source)
        {
            var candidates = await Rewrite(source);
            Assert.All(candidates, c => { Assert.Equal(source, c.Text); Assert.DoesNotContain(c.Edits, e => e.RuleId.StartsWith("lemma:")); });
        }
        [Fact]
        public async Task ClinicalParagraphRebuildsAllThreeSentencesWithoutThesaurusTerms()
        {
            const string source = "Bu araştırmanın amacı, hasta dosyalarını kullanarak Ortodonti ve Periodontoloji bölümleri arasındaki iletişimi incelemektir. Araştırmanın ikincil amacı ise konsültasyon yanıtlarını uzman ve lisansüstü öğrenci gruplarına göre karşılaştırmaktır. Tıp fakültesi arşivindeki 286 hastaya ait konsültasyon kayıtları retrospektif olarak incelenmiş ve kategorize edilmiştir.";
            var candidates = await Rewrite(source);
            Assert.All(candidates, c => {
                Assert.Equal(3, c.SentenceCount);
                Assert.Equal(3, c.RewrittenSentences);
                Assert.Contains("Ortodonti ve Periodontoloji", c.Text);
                Assert.Contains("Tıp fakültesi", c.Text);
                Assert.Contains("286", c.Text);
                Assert.Contains("konsültasyon", c.Text);
                Assert.Contains("ikincil", c.Text.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")));
                Assert.DoesNotContain("konsulto", c.Text);
                Assert.DoesNotContain("sekunder", c.Text);
                Assert.DoesNotContain("tabip", c.Text);
                Assert.DoesNotContain(c.Edits, e => e.RuleId.StartsWith("lemma:"));
                Assert.Equal(c.Text, EditApplication.Apply(source, c.Edits));
            });
        }
        [Theory]
        [InlineData("Bu çalışmanın amacı, öğrencilerin kaygı düzeylerini karşılaştırmaktır.", "karşılaştırmayı")]
        [InlineData("İncelemenin ikincil amacı ise çevresel etkileri belirlemektir.", "belirlemeyi")]
        [InlineData("Araştırmanın temel amacı, enerji tüketimini azaltmaktır.", "azaltmayı")]
        [InlineData("Bu araştırmanın amacı, her farklılığı nedensel etki olarak değerlendirmemektir.", "değerlendirmemeyi")]
        public async Task GoalsAcrossDomainsKeepObjectCaseAndNegation(string source, string expected)
        {
            var candidates = await Rewrite(source);
            Assert.Contains(candidates, c => c.Text.Contains(expected) && c.RewrittenSentences == 1);
            Assert.DoesNotContain(candidates, c => c.Text.Contains("incelemeda") || c.Text.Contains("incelemanın"));
        }
        [Theory]
        [InlineData("Bu çalışmada grup yanıtları incelenmemiştir.", "incelenmemiştir")]
        [InlineData("Mevcut araştırmada enerji miktarları ölçülmemiştir.", "ölçülmemiştir")]
        [InlineData("Sunulan incelemede anket verileri toplanmaktadır.", "toplanmaktadır")]
        public async Task SentenceReorderingPreservesOperationTenseAndNegation(string source, string predicate)
        {
            var candidates = await Rewrite(source);
            Assert.All(candidates, c => { Assert.NotEqual(source, c.Text); Assert.Contains(predicate, c.Text); Assert.Equal(1, c.RewrittenSentences); });
        }
        [Fact]
        public async Task AlternativeDetectionRetainsPureWordOrderChanges()
        {
            const string source = "Bu çalışmada veriler anket yoluyla toplanmıştır.";
            var candidates = await Rewrite(source);
            Assert.Contains(candidates, c => c.Text.Contains("Veriler anket yoluyla, bu çalışmada toplanmıştır"));
            Assert.Equal(candidates.Count, candidates.Select(c => c.Text).Distinct().Count());
        }
        [Fact]
        public async Task FixedHyperlinkAndCitationRemainIdenticalDuringWholeSentenceRewrite()
        {
            const string source = "Bu araştırmanın amacı, hasta kayıtlarını incelemektir (Beta, 2023).";
            int linkStart = source.IndexOf("hasta kayıtlarını", StringComparison.Ordinal);
            var candidates = await Rewrite(source, new[] { new TextSpan { Start = linkStart, Length = "hasta kayıtlarını".Length, Reason = "hyperlink" } });
            Assert.Contains(candidates, c => c.RewrittenSentences == 1);
            Assert.All(candidates, c => { Assert.Contains("hasta kayıtlarını", c.Text); Assert.Contains("(Beta, 2023)", c.Text); Assert.DoesNotContain(c.Edits, e => e.Start < linkStart + 17 && e.Start + e.Length > linkStart); });
        }
        [Fact]
        public void LowercaseInstitutionNameIsProtected()
        {
            const string source = "diş hekimliği fakültesi tarafından hazırlanan form";
            var spans = new ProtectionDetector().Detect(source, new UserSettings(), Array.Empty<string>(), Array.Empty<LexiconEntry>());
            Assert.Contains(spans, s => s.Start == 0 && s.Length >= "diş hekimliği fakültesi".Length);
        }
    }
}
