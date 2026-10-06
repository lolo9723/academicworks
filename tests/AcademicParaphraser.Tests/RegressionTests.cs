using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.CitationProtection;
using AcademicParaphraser.Core.DocumentProtection;
using AcademicParaphraser.Core.RuleEngine;
using AcademicParaphraser.Infrastructure.Diagnostics;
using AcademicParaphraser.Infrastructure.InternetDictionaryProviders;
using AcademicParaphraser.Infrastructure.Persistence;
using AcademicParaphraser.Infrastructure.TurkishNlp;
using Xunit;

namespace AcademicParaphraser.Tests
{
    public sealed class NlpFixture : IDisposable
    {
        public ZemberekProcess Nlp
        {
            get;
        }
        public NlpFixture()
        {
            string java = Environment.GetEnvironmentVariable("ACADEMIC_JAVA") ?? throw new InvalidOperationException("ACADEMIC_JAVA gerçek Java çalıştırıcısına işaret etmeli.");
            string jar = Environment.GetEnvironmentVariable("ACADEMIC_NLP_JAR") ?? throw new InvalidOperationException("ACADEMIC_NLP_JAR derlenmiş Zemberek sidecar yoluna işaret etmeli.");
            Nlp = new ZemberekProcess(java, jar);
        }
        public void Dispose() => Nlp.Dispose();
    }
    public sealed class RegressionTests : IClassFixture<NlpFixture>, IDisposable
    {
        private readonly NlpFixture fixture; private readonly string folder = Path.Combine(Path.GetTempPath(), "AcademicTests-" + Guid.NewGuid().ToString("N")); private readonly LocalRepository repo;
        public RegressionTests(NlpFixture fixture)
        {
            this.fixture = fixture;
            repo = new LocalRepository(Path.Combine(folder, "data.sqlite"), new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
        }
        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(folder, true);
        }
        [Theory]
        [InlineData("(Yılmaz, 2024)")]
        [InlineData("(Yılmaz & Demir, 2024)")]
        [InlineData("(Yılmaz ve Demir, 2024)")]
        [InlineData("(Yılmaz et al., 2024)")]
        [InlineData("Yılmaz (2024)")]
        [InlineData("Yılmaz vd. (2024)")]
        [InlineData("(Smith, 2021; Brown, 2023)")]
        [InlineData("[12]")]
        [InlineData("[4–8]")]
        [InlineData("(Yılmaz & Demir, 2024; β=.43, p<.001)")]
        public void CitationsAreProtected(string citation)
        {
            string text = "Bulgu " + citation + " değerlendirildi.";
            var spans = new ProtectionDetector().Detect(text, new UserSettings(), Array.Empty<string>(), Array.Empty<LexiconEntry>());
            Assert.Contains(spans, s => s.Start <= text.IndexOf(citation, StringComparison.Ordinal) && s.End >= text.IndexOf(citation, StringComparison.Ordinal) + citation.Length);
        }
        [Theory]
        [InlineData("N=236")]
        [InlineData("n = 137")]
        [InlineData("%42,3")]
        [InlineData("p < .05")]
        [InlineData("β=.43")]
        [InlineData("t(235)")]
        [InlineData("F(2,233)")]
        [InlineData("R²")]
        [InlineData("α=.76")]
        [InlineData("2024")]
        [InlineData("12.03.2024")]
        public void NumericDataProtected(string number)
        {
            var spans = new ProtectionDetector().Detect("Değer " + number + " bulundu.", new UserSettings(), Array.Empty<string>(), Array.Empty<LexiconEntry>());
            Assert.Contains(spans, s => s.Start <= 6 && s.End >= 6 + number.Length);
        }
        [Theory]
        [InlineData("https://example.org/a?b=3")]
        [InlineData("10.1234/abc.2024")]
        [InlineData("ISBN 978-605-123-456-7")]
        [InlineData("ISSN 1234-5678")]
        [InlineData("PMID: 12345678")]
        public void IdentifiersProtected(string id)
        {
            var spans = new ProtectionDetector().Detect(id, new UserSettings(), Array.Empty<string>(), Array.Empty<LexiconEntry>());
            Assert.Contains(spans, s => s.Start == 0 && s.End >= id.Length);
        }
        [Fact]
        public void LockedTermsIncludeInflection()
        {
            var spans = new ProtectionDetector().Detect("Öznel zindeliğin etkisi", new UserSettings(), new[] { "öznel zindelik" }, Array.Empty<LexiconEntry>());
            Assert.Contains(spans, s => s.Start == 0 && s.Length == 16);
        }
        [Fact]
        public async Task RequiredCitationRegression()
        {
            repo.LockTerm("öznel zindelik");
            string input = "Doğayla ilişkinin öznel zindelik üzerindeki etkisinin anlamlı olduğu belirlenmiştir (Yılmaz & Demir, 2024; β=.43, p<.001).";
            var result = await new TransformationEngine(repo, fixture.Nlp).GenerateAsync(input, new UserSettings { EnableWordChoice = true }, null, CancellationToken.None);
            Assert.NotEmpty(result);
            Assert.Contains(result, c => c.Edits.Count > 0);
            foreach (var c in result)
            {
                Assert.Contains("öznel zindelik", c.Text);
                Assert.Contains("(Yılmaz & Demir, 2024; β=.43, p<.001)", c.Text);
            }
        }
        [Fact]
        public async Task MorphologyHandlesPluralAndCase()
        {
            var tokens = await fixture.Nlp.AnalyzeAsync("Araştırmada yöntemlerle karşılaştırma yapılmıştır.", CancellationToken.None);
            var word = Assert.Single(tokens, t => t.Surface == "yöntemlerle");
            Assert.Equal("yöntem", word.Lemma);
            Assert.Contains("A3pl", word.Morphemes);
            Assert.Equal("metotlarla", await fixture.Nlp.InflectAsync("yöntemlerle", "metot", "Noun", CancellationToken.None));
        }
        [Fact]
        public async Task DoesNotGuessAmbiguousInflections()
        {
            Assert.Null(await fixture.Nlp.InflectAsync("gelişimine", "ilerleme", "Noun", CancellationToken.None));
        }
        [Fact]
        public async Task CapitalizationAndPunctuationPreserved()
        {
            var result = await new TransformationEngine(repo, fixture.Nlp).GenerateAsync("Önem arz etmektedir.", new UserSettings { EnableWordChoice = true }, null, CancellationToken.None);
            Assert.Contains(result, c => c.Text == "Önem taşımaktadır.");
        }
        [Fact]
        public async Task DifferentStrengthsUseDifferentFamilies()
        {
            var engine = new TransformationEngine(repo, fixture.Nlp);
            string text = "Bu çalışmada veriler anket yoluyla toplanmıştır.";
            var light = await engine.GenerateAsync(text, new UserSettings { DefaultStrength = Strength.Light }, null, CancellationToken.None);
            var strong = await engine.GenerateAsync(text, new UserSettings { DefaultStrength = Strength.Strong }, null, CancellationToken.None);
            Assert.DoesNotContain(light.SelectMany(c => c.Edits), e => e.Family == "güvenli sıralama");
            Assert.Contains(strong.SelectMany(c => c.Edits), e => e.Family == "güvenli sıralama" || e.Family == "cümle kuruluşu");
        }
        [Fact]
        public async Task AlternativesAreDistinctAndDeterministic()
        {
            var e = new TransformationEngine(repo, fixture.Nlp);
            string text = "Bu çalışmada sonuçların anlamlı olduğu belirlenmiştir.";
            var a = await e.GenerateAsync(text, new UserSettings { EnableWordChoice = true }, null, CancellationToken.None);
            var b = await e.GenerateAsync(text, new UserSettings { EnableWordChoice = true }, null, CancellationToken.None);
            Assert.True(a.Count >= 2);
            Assert.Equal(a.Count, a.Select(c => c.Text).Distinct().Count());
            Assert.Equal(a.Select(c => c.Text), b.Select(c => c.Text));
        }
        [Fact]
        public async Task SentenceAndParagraphBoundariesRemain()
        {
            string input = "Bulgular tespit edilmiştir.\rÖnem arz etmektedir.\r";
            var result = await new TransformationEngine(repo, fixture.Nlp).GenerateAsync(input, new UserSettings(), null, CancellationToken.None);
            foreach (var c in result)
            {
                Assert.Equal(2, c.Text.Count(ch => ch == '\r'));
                Assert.Equal(2, c.Text.Count(ch => ch == '.'));
            }
        }
        [Fact]
        public async Task UnrecognizedTextIsUnchanged()
        {
            string input = "Bu cümlede güvenli bir dönüşüm yok.";
            var result = await new TransformationEngine(repo, fixture.Nlp).GenerateAsync(input, new UserSettings(), null, CancellationToken.None);
            Assert.All(result, c => { Assert.Equal(input, c.Text); Assert.Empty(c.Edits); });
        }
        private static string Doc(string body) => "<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><w:body>" + body + "</w:body></w:document>";
        [Fact]
        public void RunMappingPreservesSplitRuns()
        {
            var map = new OoxmlRunMap(Doc("<w:p><w:r><w:rPr><w:b/></w:rPr><w:t>katkı </w:t></w:r><w:r><w:rPr><w:b/></w:rPr><w:t>sağlamaktadır</w:t></w:r></w:p>"));
            Assert.True(map.CanEdit(0, 19));
            var xml = map.Apply(new[] { new TextEdit { Start = 0, Length = 19, Original = "katkı sağlamaktadır", Replacement = "katkıda bulunmaktadır" } });
            var doc = XDocument.Parse(xml);
            Assert.Equal(2, doc.Descendants(OoxmlRunMap.W + "b").Count());
            Assert.Contains("katkıda bulunmaktadır", xml);
        }
        [Fact]
        public void DifferentFormattingBlocksCrossRunRewrite()
        {
            var map = new OoxmlRunMap(Doc("<w:p><w:r><w:rPr><w:b/></w:rPr><w:t>katkı </w:t></w:r><w:r><w:rPr><w:i/></w:rPr><w:t>sağlamaktadır</w:t></w:r></w:p>"));
            Assert.False(map.CanEdit(0, 18));
        }
        [Fact]
        public void HyperlinksKeepTheirTargetAndText()
        {
            var xml = Doc("<w:p><w:r><w:t>Önem arz etmektedir. </w:t></w:r><w:hyperlink r:id='rId1'><w:r><w:t>Kaynak</w:t></w:r></w:hyperlink></w:p>");
            var map = new OoxmlRunMap(xml);
            Assert.False(map.CanEdit(21, 6));
            string output = map.Apply(new[] { new TextEdit { Start = 0, Length = 19, Original = "Önem arz etmektedir", Replacement = "Önem taşımaktadır" } });
            Assert.Equal(XDocument.Parse(xml).Descendants(OoxmlRunMap.W + "hyperlink").Single().ToString(), XDocument.Parse(output).Descendants(OoxmlRunMap.W + "hyperlink").Single().ToString());
        }
        [Fact]
        public void TablePropertiesAndCellEndsPreserved()
        {
            var xml = Doc("<w:tbl><w:tblPr><w:tblW w:w='9000' w:type='dxa'/></w:tblPr><w:tr><w:tc><w:tcPr><w:shd w:fill='AAAAAA'/></w:tcPr><w:p><w:r><w:t>tespit edilmiştir</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");
            var map = new OoxmlRunMap(xml);
            Assert.EndsWith("\r\a", map.Text);
            string output = map.Apply(new[] { new TextEdit { Start = 0, Length = 17, Original = "tespit edilmiştir", Replacement = "saptanmıştır" } });
            Assert.Equal(XDocument.Parse(xml).Descendants(OoxmlRunMap.W + "tblPr").Single().ToString(), XDocument.Parse(output).Descendants(OoxmlRunMap.W + "tblPr").Single().ToString());
        }
        [Fact]
        public void ZoteroFieldsAreNeverEdited()
        {
            var map = new OoxmlRunMap(Doc("<w:p><w:r><w:fldChar w:fldCharType='begin'/></w:r><w:r><w:instrText>ADDIN ZOTERO_ITEM CSL_CITATION</w:instrText></w:r><w:r><w:fldChar w:fldCharType='separate'/></w:r><w:r><w:t>Yılmaz, 2024</w:t></w:r><w:r><w:fldChar w:fldCharType='end'/></w:r></w:p>"));
            Assert.False(map.CanEdit(0, 11));
        }
        [Fact]
        public void ExplicitHyperlinkResultEditingPreservesCode()
        {
            string xml = Doc("<w:p><w:r><w:fldChar w:fldCharType='begin'/></w:r><w:r><w:instrText>HYPERLINK https://example.org</w:instrText></w:r><w:r><w:fldChar w:fldCharType='separate'/></w:r><w:r><w:t>tespit edilmiştir</w:t></w:r><w:r><w:fldChar w:fldCharType='end'/></w:r></w:p>");
            var locked = new OoxmlRunMap(xml, true);
            Assert.False(locked.CanEdit(0, 17));
            var editable = new OoxmlRunMap(xml, false);
            Assert.True(editable.CanEdit(0, 17));
            Assert.Contains("HYPERLINK https://example.org", editable.Apply(new[] { new TextEdit { Start = 0, Length = 17, Original = "tespit edilmiştir", Replacement = "saptanmıştır" } }));
        }
        [Fact]
        public async Task TemporalConnectorIsNotSimplified()
        {
            var c = await new TransformationEngine(repo, fixture.Nlp).GenerateAsync("İki uygulama aynı zamanda tamamlandı.", new UserSettings(), null, CancellationToken.None);
            Assert.All(c, x => Assert.Contains("aynı zamanda", x.Text));
        }
        [Fact]
        public void LinkBoundaryRemainsProtectedWhenDisplayTextIsEditable()
        {
            var map = new OoxmlRunMap(Doc("<w:p><w:r><w:t>katkı </w:t></w:r><w:hyperlink r:id='rId1'><w:r><w:t>sağlamaktadır</w:t></w:r></w:hyperlink></w:p>"), false);
            Assert.False(map.CanEdit(0, 19));
            Assert.True(map.CanEdit(6, 13));
        }
        [Fact]
        public async Task TenParagraphsPreserveEveryCitationAndBoundary()
        {
            string paragraph = "Bu çalışmada veriler anket yoluyla toplanmıştır (Yılmaz, 2024).";
            string input = string.Join("\r", Enumerable.Repeat(paragraph, 10));
            var output = await new TransformationEngine(repo, fixture.Nlp).GenerateAsync(input, new UserSettings { DefaultStrength = Strength.Strong }, null, CancellationToken.None);
            Assert.NotEmpty(output);
            Assert.All(output, c => { var lines = c.Text.Split('\r'); Assert.Equal(10, lines.Length); Assert.All(lines, line => Assert.EndsWith("(Yılmaz, 2024).", line)); Assert.Contains(c.Edits, e => e.Start > input.LastIndexOf('\r')); Assert.DoesNotContain(c.Edits, e => e.Original.Contains('\r')); });
        }
        [Fact]
        public async Task ContextAndMorphemeConstraintsAreLocalToEachParagraph()
        {
            repo.SaveRule(new RuleDefinition { Id = "contextual", Pattern = @"\bdeğerlendirmeler sunulmuştur\b", Target = "değerlendirmeler aktarılmıştır", ContextPattern = "nitel", PosConstraint = "Noun", RequiredMorphemes = new System.Collections.Generic.List<string> { "A3pl" } });
            string input = "nitel çalışma kapsamında değerlendirmeler sunulmuştur.\rdeğerlendirmeler sunulmuştur.";
            var c = await new TransformationEngine(repo, fixture.Nlp).GenerateAsync(input, new UserSettings(), null, CancellationToken.None);
            Assert.Contains(c.SelectMany(x => x.Edits), e => e.RuleId == "contextual");
            Assert.All(c, x => Assert.EndsWith("\rdeğerlendirmeler sunulmuştur.", x.Text));
            repo.SaveRule(new RuleDefinition { Id = "contextual", Pattern = @"\bdeğerlendirmeler sunulmuştur\b", Target = "değerlendirmeler aktarılmıştır", RequiredMorphemes = new System.Collections.Generic.List<string> { "Neg" } });
            var blocked = await new TransformationEngine(repo, fixture.Nlp).GenerateAsync(input, new UserSettings(), null, CancellationToken.None);
            Assert.DoesNotContain(blocked.SelectMany(x => x.Edits), e => e.RuleId == "contextual");
        }
        [Fact]
        public async Task InlineObjectMarkersKeepOffsetsAndDrawingXml()
        {
            string xml = Doc("<w:p><w:r><w:drawing><inline xmlns='http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing'><extent cx='10' cy='10'/></inline></w:drawing></w:r><w:r><w:t>Önem arz etmektedir.</w:t></w:r></w:p>");
            var map = new OoxmlRunMap(xml);
            Assert.StartsWith("\u0001Önem", map.Text);
            var candidates = await new TransformationEngine(repo, fixture.Nlp).GenerateAsync(map.Text, new UserSettings { EnableWordChoice = true }, map.ProtectedSpans, CancellationToken.None);
            var candidate = Assert.Single(candidates);
            Assert.True(map.CanEdit(candidate.Edits[0].Start, candidate.Edits[0].Length));
            string result = map.Apply(candidate.Edits);
            Assert.Equal(XDocument.Parse(xml).Descendants(OoxmlRunMap.W + "drawing").Single().ToString(), XDocument.Parse(result).Descendants(OoxmlRunMap.W + "drawing").Single().ToString());
            Assert.Contains("Önem taşımaktadır", result);
        }
        [Fact]
        public void ShapeTextDoesNotEnterBodyMapping()
        {
            var map = new OoxmlRunMap(Doc("<w:p><w:r><w:t>Ana metin</w:t></w:r><w:r><w:pict><w:txbxContent><w:p><w:r><w:t>Şekil metni</w:t></w:r></w:p></w:txbxContent></w:pict></w:r></w:p>"));
            Assert.Equal("Ana metin\r", map.Text);
        }
        [Fact]
        public void CustomRuleCannotInsertParagraphOrCellMarks()
        {
            Assert.Throws<ArgumentException>(() => repo.SaveRule(new RuleDefinition { Id = "unsafe", Pattern = "önem", Target = "başlık\r\a" }));
        }
        [Fact]
        public void XmlDtdIsRejected()
        {
            Assert.Throws<System.Xml.XmlException>(() => new OoxmlRunMap("<!DOCTYPE x [<!ENTITY y SYSTEM 'file:///etc/passwd'>]><x/>"));
        }
        [Theory]
        [InlineData("/word/footnotes.xml", "footnotes", "footnote")]
        [InlineData("/word/endnotes.xml", "endnotes", "endnote")]
        [InlineData("/word/header1.xml", "hdr", "hdr")]
        [InlineData("/word/footer1.xml", "ftr", "ftr")]
        public void StoryMappingUsesSelectedPackagePart(string part, string root, string item)
        {
            string nested = item == root ? "<w:p><w:r><w:t>tespit edilmiştir</w:t></w:r></w:p>" : "<w:" + item + "><w:p><w:r><w:t>tespit edilmiştir</w:t></w:r></w:p></w:" + item + ">";
            string xml = "<pkg:package xmlns:pkg='http://schemas.microsoft.com/office/2006/xmlPackage' xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><pkg:part pkg:name='/word/document.xml'><pkg:xmlData><w:document><w:body><w:p><w:r><w:t>Ana metin</w:t></w:r></w:p></w:body></w:document></pkg:xmlData></pkg:part><pkg:part pkg:name='" + part + "'><pkg:xmlData><w:" + root + ">" + nested + "</w:" + root + "></pkg:xmlData></pkg:part></pkg:package>";
            var map = new OoxmlRunMap(xml, true, part);
            Assert.Equal("tespit edilmiştir\r", map.Text);
            string edited = map.Apply(new[] { new TextEdit { Start = 0, Length = 17, Original = "tespit edilmiştir", Replacement = "saptanmıştır" } });
            Assert.Contains("Ana metin", edited);
            Assert.Contains("saptanmıştır", edited);
        }
        [Fact]
        public void FutureSchemaIsRejectedBeforeAnyMigrationWrite()
        {
            string path = Path.Combine(folder, "future.sqlite");
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + path))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE migrations(version INTEGER PRIMARY KEY);INSERT INTO migrations VALUES(3);";
                    command.ExecuteNonQuery();
                    Assert.Throws<InvalidOperationException>(() => new LocalRepository(path, new AesTextProtector(RandomNumberGenerator.GetBytes(32))));
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
                    Assert.Equal(1L, command.ExecuteScalar());
                    command.CommandText = "SELECT COUNT(*) FROM migrations WHERE version=1";
                    Assert.Equal(0L, command.ExecuteScalar());
                }
            }
        }
        [Theory]
        [InlineData("{\"FormatVersion\":1,\"Rules\":null}")]
        [InlineData("{\"FormatVersion\":1,\"Settings\":null}")]
        [InlineData("{\"FormatVersion\":1,\"Settings\":{\"MinimumConfidence\":NaN}}")]
        public void InvalidBackupValuesAreRejectedAtomically(string backup)
        {
            string before = repo.Export();
            Assert.ThrowsAny<ArgumentException>(() => repo.Import(backup));
            Assert.Equal(before, repo.Export());
        }
        [Fact]
        public void MigrationAndBackupPreserveUserData()
        {
            repo.LockTerm("etkinlik doyumu");
            repo.SaveRule(new RuleDefinition { Id = "personal", Pattern = "katkı sağlamaktadır", Target = "katkıda bulunmaktadır" });
            string backup = repo.Export();
            repo.Import(backup);
            Assert.Contains("etkinlik doyumu", repo.GetLockedTerms());
            Assert.Single(repo.GetRules(), r => r.Id == "personal");
            var reopened = new LocalRepository(repo.DatabasePath, new AesTextProtector(RandomNumberGenerator.GetBytes(32)));
            Assert.Contains("etkinlik doyumu", reopened.GetLockedTerms());
        }
        [Fact]
        public void InvalidImportIsAtomic()
        {
            repo.LockTerm("boş zaman");
            string previous = repo.Export();
            Assert.ThrowsAny<ArgumentException>(() => repo.Import("{\"FormatVersion\":1,\"LockedTerms\":[\"new\"],\"Rules\":[{\"Id\":\"bad\",\"Pattern\":\"(\",\"Confidence\":.95}]}"));
            Assert.Equal(previous, repo.Export());
        }
        [Fact]
        public void HistoryIsEncryptedAtRest()
        {
            string secret = "Akademik özel çalışma metni";
            repo.AddHistory(secret, "Yeni ifade", 1);
            Assert.Equal(secret, Assert.Single(repo.GetHistory()).Original);
            // Windows disallows File.ReadAllBytes while a pooled SQLite write handle remains open.
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Assert.DoesNotContain(secret, System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(repo.DatabasePath)));
        }
        [Fact]
        public void EncryptionRejectsTampering()
        {
            var protector = new AesTextProtector(RandomNumberGenerator.GetBytes(32));
            var data = protector.Protect("metin");
            data[20] ^= 1;
            Assert.Throws<CryptographicException>(() => protector.Unprotect(data));
        }
        [Fact]
        public void LogsNeverIncludeExceptionText()
        {
            var log = new SafeLog(Path.Combine(folder, "logs"));
            log.Write("ERROR", new Exception("secret academic text"));
            Assert.DoesNotContain("secret academic text", File.ReadAllText(Directory.GetFiles(Path.Combine(folder, "logs")).Single()));
        }
        [Fact]
        public void LogIoFailureDoesNotEscapeIntoWordErrorHandling()
        {
            string path=Path.Combine(folder,"unavailable-log-directory");
            File.WriteAllText(path,"A file occupies the expected log directory.");
            new SafeLog(path).Write("WORD_ERROR",new InvalidOperationException("private academic content"));
            Assert.Equal("A file occupies the expected log directory.",File.ReadAllText(path));
        }
        [Fact]
        public async Task OfflineDictionaryDoesNotRequireNetwork()
        {
            var service = new DictionaryService(repo, new IDictionaryProvider[] { new TurkishWiktionaryProvider() });
            var result = await service.LookupAsync("yöntem", new UserSettings(), CancellationToken.None);
            Assert.Contains("Çevrimdışı", Assert.Single(result).Error);
            await Assert.ThrowsAsync<ArgumentException>(() => service.LookupAsync("belgenin bütün metni", new UserSettings(), CancellationToken.None));
        }
    }
}
