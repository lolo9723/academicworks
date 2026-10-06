using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Rewriting;
using AcademicParaphraser.Core.RuleEngine;
using AcademicParaphraser.Infrastructure.InternetDictionaryProviders;
using AcademicParaphraser.Infrastructure.LexicalKnowledge;
using AcademicParaphraser.Infrastructure.Persistence;
using Xunit;

namespace AcademicParaphraser.Tests
{
    public sealed class InternetEnrichmentTests : IClassFixture<NlpFixture>, IDisposable
    {
        private readonly NlpFixture fixture;
        private readonly string folder = Path.Combine(Path.GetTempPath(), "InternetTests-" + Guid.NewGuid().ToString("N"));
        private readonly LocalRepository repo;
        private string WordNet => Environment.GetEnvironmentVariable("ACADEMIC_WORDNET")!;
        private static UserSettings Online => new UserSettings { InternetEnabled = true, OfflineMode = false, DefaultStrength = Strength.Strong };
        public InternetEnrichmentTests(NlpFixture fixture)
        {
            this.fixture = fixture;
            repo = new LocalRepository(Path.Combine(folder, "data.sqlite"), new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
        }
        public void Dispose() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(folder, true); }
        private static byte[] Bank()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "data", "internet-structures-v1.json"))) directory = directory.Parent;
            return File.ReadAllBytes(Path.Combine(directory!.FullName, "data", "internet-structures-v1.json"));
        }
        private sealed class BankHandler : HttpMessageHandler
        {
            public byte[] Bytes = Bank();
            public int Calls;
            public Uri? Request;
            public bool Wait;
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                Calls++; Request = request.RequestUri;
                if (Wait) await Task.Delay(Timeout.Infinite, cancellation);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Bytes) };
            }
        }
        private sealed class Provider : IDictionaryProvider
        {
            public string Name { get; set; } = "test-dictionary";
            public readonly List<string> Terms = new List<string>();
            public bool Fail;
            public bool Reciprocal;
            public bool WrongPos;
            public Task<DictionaryResult> LookupAsync(string term, CancellationToken cancellation)
            {
                cancellation.ThrowIfCancellationRequested(); Terms.Add(term);
                if (Fail) throw new HttpRequestException("test outage");
                if (term == "zqalf" || term == "zqbet")
                    return Task.FromResult(new DictionaryResult { Term = term, Source = Name, Content = "single test sense", SingleSense = true, Definitions = new List<string> { "test definition" }, PosTags = new List<string> { WrongPos ? "Verb" : "Noun" }, Synonyms = term == "zqalf" ? new List<string> { "zqbet" } : Reciprocal ? new List<string> { "zqalf" } : new List<string>() });
                return Task.FromResult(new DictionaryResult { Term = term, Source = Name, Error = "missing", NotFound = true });
            }
        }
        [Fact]
        public async Task AllUnresolvedRootsAreQueriedAndNegativeResultsAreCached()
        {
            var provider = new Provider();
            var dictionary = new DictionaryService(repo, new[] { provider });
            var knowledge = new WordNetLexicalSource(WordNet, dictionary);
            var words = Enumerable.Range(0, 8).Select(i => new MorphToken { Lemma = "zqroot" + (char)('a' + i), Surface = "test", Pos = "Noun" }).ToArray();
            var context = new EnrichmentContext();
            await knowledge.FindAsync("", words, Online, CancellationToken.None, context);
            Assert.Equal(8, provider.Terms.Count);
            Assert.Equal(8, context.Summary.RootsChecked);
            Assert.Equal(8, context.Summary.OnlineRootsQueried);
            Assert.Equal(8, context.Summary.RootsUnresolved);
            await knowledge.FindAsync("", words, Online, CancellationToken.None);
            Assert.Equal(8, provider.Terms.Count);
            Assert.All(provider.Terms, term => Assert.All(term, c => Assert.True(char.IsLetter(c))));
        }
        [Fact]
        public async Task LocalScanNoLongerStopsAtTwoHundredRoots()
        {
            var words = Enumerable.Range(0, 205).Select(i => new MorphToken { Lemma = "zq" + (char)('a' + i / 26) + (char)('a' + i % 26), Pos = "Noun" }).ToArray();
            var context = new EnrichmentContext();
            await new WordNetLexicalSource(WordNet).FindAsync("", words, new UserSettings(), CancellationToken.None, context);
            Assert.Equal(205, context.Summary.RootsTotal);
            Assert.Equal(205, context.Summary.RootsChecked);
        }
        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        public async Task LocallyAbsentWordRequiresReciprocalSingleSenseAndMatchingPos(bool reciprocal, bool wrongPos, bool accepted)
        {
            var provider = new Provider { Reciprocal = reciprocal, WrongPos = wrongPos };
            var source = new WordNetLexicalSource(WordNet, new DictionaryService(repo, new[] { provider }));
            var entries = await source.FindAsync("", new[] { new MorphToken { Lemma = "zqalf", Pos = "Noun" } }, Online, CancellationToken.None);
            Assert.Equal(accepted, entries.Any(e => e.Lemma == "zqalf" && e.Synonyms.Contains("zqbet")));
        }
        [Fact]
        public async Task FailedProviderStopsRetryingWhileSecondProviderContinues()
        {
            var failing = new Provider { Name = "failing", Fail = true };
            var working = new Provider { Name = "working" };
            var service = new DictionaryService(repo, new[] { failing, working });
            foreach (string root in new[] { "zqone", "zqtwo", "zqthree", "zqfour", "zqfive" })
                await service.LookupAsync(root, Online, CancellationToken.None);
            Assert.Equal(3, failing.Terms.Count);
            Assert.Equal(5, working.Terms.Count);
        }
        [Fact]
        public async Task StructureRequestHasNoTextAndVerifiedBankWorksFromOfflineCache()
        {
            var handler = new BankHandler(); using var client = new HttpClient(handler);
            using var source = new OnlineStructureSource(repo, client);
            var context = new EnrichmentContext();
            var rules = await source.FindAsync("Mevcut incelemede yanıtlar analiz edilmemiştir.", Online, CancellationToken.None, context);
            Assert.NotEmpty(rules);
            Assert.Equal(25104, context.Summary.StructureInventory);
            Assert.Equal("https://raw.githubusercontent.com/lolo9723/academicworks/main/data/internet-structures-v1.json", handler.Request!.AbsoluteUri);
            Assert.Equal("", handler.Request.Query);
            using var cachedSource = new OnlineStructureSource(repo, client);
            var cached = await cachedSource.FindAsync("Mevcut incelemede yanıtlar analiz edilmemiştir.", new UserSettings(), CancellationToken.None, new EnrichmentContext());
            Assert.NotEmpty(cached); Assert.Equal(1, handler.Calls);
        }
        [Fact]
        public async Task ColdOfflineStructureSearchMakesNoRequest()
        {
            var handler = new BankHandler(); using var client = new HttpClient(handler); using var source = new OnlineStructureSource(repo, client);
            Assert.Empty(await source.FindAsync("Bu çalışmada veriler incelenmiştir.", new UserSettings(), CancellationToken.None, new EnrichmentContext()));
            Assert.Equal(0, handler.Calls);
        }
        [Fact]
        public async Task TamperedBankIsNotCachedOrExecuted()
        {
            var handler = new BankHandler { Bytes = Encoding.UTF8.GetBytes("{\"formatVersion\":1,\"Pattern\":\".*\",\"Target\":\"invented\"}") };
            using var client = new HttpClient(handler); using var source = new OnlineStructureSource(repo, client);
            var context = new EnrichmentContext();
            Assert.Empty(await source.FindAsync("Bu çalışmada veriler incelenmiştir.", Online, CancellationToken.None, context));
            Assert.Contains("uyuşmadı", context.Summary.StructureStatus);
            Assert.Empty(await source.FindAsync("Bu çalışmada veriler incelenmiştir.", new UserSettings(), CancellationToken.None, new EnrichmentContext()));
        }
        [Fact]
        public async Task OversizedBankIsRejectedWithoutStoppingLocalGeneration()
        {
            var handler = new BankHandler { Bytes = new byte[65537] }; using var client = new HttpClient(handler); using var source = new OnlineStructureSource(repo, client);
            Assert.Empty(await source.FindAsync("Bu çalışmada veriler incelenmiştir.", Online, CancellationToken.None, new EnrichmentContext()));
        }
        [Fact]
        public async Task CancelReachesStructureRequest()
        {
            var handler = new BankHandler { Wait = true }; using var client = new HttpClient(handler); using var source = new OnlineStructureSource(repo, client);
            using var cancel = new CancellationTokenSource();
            var task = source.FindAsync("Bu çalışmada veriler incelenmiştir.", Online, cancel.Token, new EnrichmentContext());
            cancel.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }
        [Theory]
        [InlineData("Mevcut incelemede yanıtlar analiz edilmemiştir (Alfa, 2022; N=120).", "Yanıtlar mevcut incelemede çözümlenmemiştir (Alfa, 2022; N=120).")]
        [InlineData("İlgili analizin amacı, ilişkileri incelemektir.", "İlgili analiz, ilişkileri incelemeyi amaçlamaktadır.")]
        public async Task DownloadedFrameActuallyRewritesNewSentenceAndKeepsNegation(string original, string expected)
        {
            var handler = new BankHandler(); using var client = new HttpClient(handler); using var source = new OnlineStructureSource(repo, client);
            var candidates = await new TransformationEngine(repo, fixture.Nlp, null, source).GenerateAsync(original, Online, null, CancellationToken.None);
            Assert.Contains(candidates, c => c.Text == expected && c.Edits.Any(e => e.RuleId.StartsWith("online-frame:")));
            Assert.All(candidates, c => Assert.Equal(25104, c.Enrichment!.StructureInventory));
        }
    }
}
