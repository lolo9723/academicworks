using System;
using System.Collections.Generic;
using System.Linq;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.CitationProtection;
using AcademicParaphraser.Core.DocumentProtection;
using AcademicParaphraser.Core.Rewriting;
using Xunit;

namespace AcademicParaphraser.Tests
{
    public sealed class RewritePlanTests
    {
        [Fact]
        public void ParagraphRewritingKeepsCitationAndNumbersInPlace()
        {
            const string source = "Çalışmada 24 katılımcının deneyimleri değerlendirilmiştir (Alfa, 2022).\r";
            var protectedSpans = new ProtectionDetector().Detect(source, new UserSettings(), Array.Empty<string>(), Array.Empty<LexiconEntry>());
            var plan = RewritePlan.Create(source, new[] { new TextSpan { Start = 0, Length = source.Length } }, protectedSpans);
            Assert.Equal(source, string.Concat(plan.Blocks.Select(b => b.Text)));
            var values = plan.Blocks.Where(b => b.Editable).ToDictionary(b => b.Id, b => b.Text);
            var first = plan.Blocks.First(b => b.Editable);
            values[first.Id] = "Bu araştırma kapsamında";
            var candidate = plan.BuildCandidate(values, "test:rewrite");
            Assert.StartsWith("Bu araştırma kapsamında 24", candidate.Text);
            Assert.Contains("(Alfa, 2022)", candidate.Text);
            Assert.EndsWith("\r", candidate.Text);
            Assert.All(candidate.Edits, e => Assert.DoesNotContain(protectedSpans, p => p.Intersects(e.Start, e.Length)));
        }

        [Fact]
        public void WordRunsAndHyperlinksStayIntactUnderFullBlockReplacement()
        {
            const string xml = "<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><w:body><w:p><w:pPr><w:jc w:val='both'/></w:pPr><w:r><w:t>Bu çalışma </w:t></w:r><w:r><w:t>sonuçları inceler. </w:t></w:r><w:hyperlink r:id='rId1'><w:r><w:t>Kaynak</w:t></w:r></w:hyperlink><w:r><w:rPr><w:i/></w:rPr><w:t> Açıklama</w:t></w:r></w:p></w:body></w:document>";
            var map = new OoxmlRunMap(xml);
            Assert.Equal(2, map.EditableSpans.Count);
            var plan = RewritePlan.Create(map.Text, map.EditableSpans, map.ProtectedSpans);
            var values = plan.Blocks.Where(b => b.Editable).ToDictionary(b => b.Id, b => b.Text);
            values[plan.Blocks.First(b => b.Editable).Id] = "Araştırmanın odağında bulguların değerlendirilmesi yer almaktadır.";
            var candidate = plan.BuildCandidate(values, "test:rewrite");
            Assert.True(CandidateIntegrity.IsApplicable(map.Text, candidate, e => map.CanEdit(e.Start, e.Length)));
            string result = map.Apply(candidate.Edits);
            var after = new OoxmlRunMap(result);
            Assert.Equal(candidate.Text, after.Text);
            Assert.Contains("rId1", result);
            Assert.Contains("Kaynak", result);
            Assert.Contains("<w:i", result);
            Assert.Contains("both", result);
        }

        [Theory]
        [InlineData("Yeni\rparagraf")]
        [InlineData("Yeni\a hücre")]
        [InlineData(" Başında boşluk")]
        [InlineData("Sonunda boşluk ")]
        [InlineData("")]
        public void InvalidReplacementCannotAlterWordBoundaries(string replacement)
        {
            var plan = RewritePlan.Create("Örnek metin.", new[] { new TextSpan { Start = 0, Length = 12 } }, Array.Empty<TextSpan>());
            Assert.Throws<ArgumentException>(() => plan.BuildCandidate(new Dictionary<string, string> { [plan.Blocks[0].Id] = replacement }, "test:rewrite"));
        }

        [Fact]
        public void MissingOrUnknownBlocksAreRejected()
        {
            var plan = RewritePlan.Create("Örnek metin.", new[] { new TextSpan { Start = 0, Length = 12 } }, Array.Empty<TextSpan>());
            Assert.Throws<ArgumentException>(() => plan.BuildCandidate(new Dictionary<string, string>(), "test:rewrite"));
            Assert.Throws<ArgumentException>(() => plan.BuildCandidate(new Dictionary<string, string> { ["unknown"] = "Yeni ifade." }, "test:rewrite"));
        }

        [Fact]
        public void OneBlockedEditRejectsTheWholeProposal()
        {
            const string source = "Araştırma veri sunar.";
            var edits = new List<TextEdit> {
                new TextEdit { Start = 0, Length = 9, Original = "Araştırma", Replacement = "Çalışma" },
                new TextEdit { Start = 15, Length = 5, Original = "sunar", Replacement = "sağlar" }
            };
            var candidate = new Candidate { Text = EditApplication.Apply(source, edits), Edits = edits };
            Assert.False(CandidateIntegrity.IsApplicable(source, candidate, e => e.Start == 0));
            Assert.Equal(2, candidate.Edits.Count);
            Assert.True(CandidateIntegrity.IsApplicable(source, candidate, e => true));
        }

        [Fact]
        public void PreviewMustExactlyMatchAllProposedEdits()
        {
            var candidate = new Candidate { Text = "Başka bir önizleme", Edits = new List<TextEdit> { new TextEdit { Start = 0, Length = 5, Original = "Örnek", Replacement = "Farklı" } } };
            Assert.False(CandidateIntegrity.IsApplicable("Örnek metin", candidate, e => true));
        }
    }
}
