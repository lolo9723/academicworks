using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.RuleEngine;
using AcademicParaphraser.Infrastructure.LexicalKnowledge;
using AcademicParaphraser.Infrastructure.Persistence;
using AcademicParaphraser.Infrastructure.InternetDictionaryProviders;
using HtmlAgilityPack;
using Xunit;

namespace AcademicParaphraser.Tests
{
    public sealed class ExpandedEngineTests : IClassFixture<NlpFixture>, IDisposable
    {
        private readonly NlpFixture fixture;
        private readonly string directory = Path.Combine(Path.GetTempPath(), "ExpandedTests-" + Guid.NewGuid().ToString("N"));
        private readonly LocalRepository repository;
        private readonly WordNetLexicalSource knowledge;
        public ExpandedEngineTests(NlpFixture fixture)
        {
            this.fixture = fixture;
            repository = new LocalRepository(Path.Combine(directory, "personal.sqlite"), new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
            knowledge = new WordNetLexicalSource(Environment.GetEnvironmentVariable("ACADEMIC_WORDNET") ?? throw new InvalidOperationException("Gerçek KeNet verisi test için gerekli."));
        }
        public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
        [Fact]
        public void RealBroadDictionaryAndNewDefaultsArePresent()
        {
            Assert.Contains("82155", knowledge.Describe());
            Assert.True(repository.GetRules().Count >= 1200);
        }
        [Fact]
        public async Task FullParagraphGetsStructuralRewritesAndKeepsStatistics()
        {
            const string source = "İçerik analizi, ziyaretçi deneyimlerini incelemenin en etkili yöntemlerinden biridir. Ziyaretçi deneyimlerini anlamayı mümkün kıldığı için bu araştırmada içerik analizi kullanılmıştır. Ziyaretçilerin çevrimiçi yorumları incelenmiştir. Araştırma alanı olarak 12 destinasyon belirlenmiştir. Bu yorumlarda deneyimlerin nasıl temsil edildiği analiz edilmiştir. Bu araştırmanın amacı, ziyaretçi deneyimlerini incelemektir (Alfa, 2022; N=120).";
            var candidates = await new TransformationEngine(repository, fixture.Nlp, knowledge).GenerateAsync(source, new UserSettings { DefaultStrength = Strength.Strong }, null, CancellationToken.None);
            Assert.NotEmpty(candidates);
            Assert.True(candidates[0].Edits.Count >= 6, candidates[0].Text);
            Assert.Contains(candidates, c => c.Text.Contains("olanak sağladığından"));
            Assert.Contains(candidates, c => c.Text.Contains("amaçlamaktadır"));
            Assert.All(candidates, c => { Assert.Contains("12 destinasyon", c.Text); Assert.Contains("(Alfa, 2022; N=120)", c.Text); });
        }
        [Theory]
        [InlineData("Bu çalışmada tematik analiz kullanılmıştır.", "Bu çalışma tematik analiz kullanılarak yürütülmüştür.")]
        [InlineData("Bu araştırmanın amacı, ilişkileri incelemektir.", "Bu araştırma, ilişkileri incelemeyi amaçlamaktadır.")]
        [InlineData("Bulgular incelenmemiştir.", "Bulgular incelemeye tabi tutulmamıştır.")]
        [InlineData("Çalışmada ölçüm yapılmıştır.", "Çalışmada ölçüm gerçekleştirilmiştir.")]
        [InlineData("Çalışmada karşılaştırma yapılmaktadır.", "Çalışmada karşılaştırma gerçekleştirilmektedir.")]
        public async Task SentenceFramesAndNegationRemainGrammatical(string source, string expected)
        {
            var result = await new TransformationEngine(repository, fixture.Nlp).GenerateAsync(source, new UserSettings { DefaultStrength = Strength.Strong }, null, CancellationToken.None);
            Assert.Contains(result, c => c.Text == expected);
        }
        [Fact]
        public async Task DictionaryAddsRealMorphologicalAlternatives()
        {
            var tokens = await fixture.Nlp.AnalyzeAsync("Çalışmada faktörler incelenmiştir.", CancellationToken.None);
            var entries = await knowledge.FindAsync("Çalışmada faktörler incelenmiştir.", tokens, new UserSettings(), CancellationToken.None);
            Assert.Contains(entries, e => e.Lemma == "faktör" && e.Synonyms.Contains("etmen"));
            var result = await new TransformationEngine(repository, fixture.Nlp, knowledge).GenerateAsync("Çalışmada faktörler incelenmiştir.", new UserSettings(), null, CancellationToken.None);
            Assert.Contains(result, c => c.Text.Contains("etmenler"));
        }
        [Fact]
        public async Task AmbiguousSenseDoesNotPickHayalForImaj()
        {
            const string source = "Çalışmada imaj ele alınmıştır.";
            var tokens = await fixture.Nlp.AnalyzeAsync(source, CancellationToken.None);
            var entries = await knowledge.FindAsync(source, tokens, new UserSettings(), CancellationToken.None);
            Assert.DoesNotContain(entries, e => e.Lemma == "imaj" && e.Synonyms.Contains("hayal"));
        }
        [Fact]
        public void DefaultsDoNotOverwritePersonalRuleOnUpgrade()
        {
            var rule = repository.GetRules().First(r => r.Id.StartsWith("extended-"));
            rule.Target = "kişisel tanım";
            repository.SaveRule(rule);
            var reopened = new LocalRepository(repository.DatabasePath, new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
            Assert.Equal("kişisel tanım", reopened.GetRules().Single(r => r.Id == rule.Id).Target);
        }
        [Fact]
        public void WebParserReadsOnlyLabelledInlineTurkishSynonyms()
        {
            var document = new HtmlDocument();
            document.LoadHtml("<h2>English</h2><span class='nyms synonym'><a href='/wiki/wrong'>wrong</a></span><h2>Turkish</h2><h3>Noun</h3><ol><li><a href='/wiki/method'>method</a><span class='nyms synonym'><a href='/wiki/metot#Turkish'>metot</a></span><span class='nyms antonym'><a href='/wiki/wrong'>wrong</a></span></li></ol>");
            var result = WikiLexicalParser.Parse(document);
            Assert.True(result.SingleSense);
            Assert.Equal(new[] { "metot" }, result.Synonyms);
        }
        [Fact]
        public void WebParserRequiresTurkishSectionAndCountsMeanings()
        {
            var document = new HtmlDocument();
            document.LoadHtml("<h2>English</h2><h3>Synonyms</h3><a href='/wiki/wrong'>wrong</a><h2>Turkish</h2><h3>Noun</h3><ol><li>Definition</li></ol><h4>Synonyms</h4><a href='/wiki/etmen'>etmen</a><h3>Further reading</h3><a href='/wiki/wrong'>wrong</a>");
            var result = WikiLexicalParser.Parse(document);
            Assert.True(result.SingleSense);
            Assert.Equal(new[] { "etmen" }, result.Synonyms);
        }
    }
}
