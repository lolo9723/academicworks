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
            values[first.Id] = "Bu araştırma kapsamında ";
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
        public void ProtectedNameCannotBeDuplicatedInsideAnEditableBlock()
        {
            const string source="Kayıtlar Ankara arşivinde saklanır.";
            var plan=RewritePlan.Create(source,new[]{new TextSpan{Start=0,Length=source.Length}},new[]{new TextSpan{Start=9,Length=6}});
            Assert.NotEmpty(plan.CheckFixedTextMultiplicity("Ankara kayıtları Ankara arşivinde saklanır."));
            Assert.Empty(plan.CheckFixedTextMultiplicity(source));
        }
        [Fact]
        public void MaskedParagraphCompilerKeepsQuantitiesAndWordBlocks()
        {
            const string source="Sensör 42 °C ölçtü.";
            var plan=RewritePlan.Create(source,new[]{new TextSpan{Start=0,Length=source.Length}},new[]{new TextSpan{Start=7,Length=5}});
            var marker=RewritePlan.Marker(plan.Blocks.Single(b=>!b.Editable));
            var values=plan.ExtractMaskedReplacements("Sıcaklık sensörle "+marker+" olarak ölçüldü.");
            Assert.Equal("Sıcaklık sensörle 42 °C olarak ölçüldü.",plan.BuildCandidate(values,"test").Text);
            Assert.Throws<ArgumentException>(()=>plan.ExtractMaskedReplacements("Sıcaklık "+marker+marker+" ölçüldü."));
            Assert.Throws<ArgumentException>(()=>plan.ExtractMaskedReplacements("Sıcaklık 42 °C ölçüldü."));
        }
        [Fact]
        public void NaturalParagraphCanBeCompiledWithoutHidingItsEvidencePredicate()
        {
            const string source="Sensör 42 °C ölçtü.";
            var plan=RewritePlan.Create(source,new[]{new TextSpan{Start=0,Length=source.Length}},new[]{new TextSpan{Start=7,Length=5}});
            Assert.True(plan.CanCompileNaturalText);
            const string target="Sıcaklık sensörle 42 °C olarak ölçüldü.";
            var candidate=plan.BuildCandidate(plan.CompileNaturalText(target),"test");
            Assert.Equal(target,candidate.Text);
            Assert.All(candidate.Edits,e=>Assert.False(new TextSpan{Start=7,Length=5}.Intersects(e.Start,e.Length)));
            Assert.Throws<ArgumentException>(()=>plan.CompileNaturalText("Sıcaklık 42 °F olarak ölçüldü."));
        }
        [Fact]
        public void SpacesAroundProtectedTextCanMoveWithoutEditingTheAnchor()
        {
            const string source="Ölçülen sıcaklık 42 °C olarak kaydedildi.";
            var span=new TextSpan{Start=source.IndexOf("42",StringComparison.Ordinal),Length=5};
            var plan=RewritePlan.Create(source,new[]{new TextSpan{Start=0,Length=source.Length}},new[]{span});
            const string target="Kaydedilen sıcaklık 42 °C.";
            var candidate=plan.BuildCandidate(plan.CompileNaturalText(target),"test");
            Assert.Equal(target,candidate.Text);
            Assert.All(candidate.Edits,e=>Assert.False(span.Intersects(e.Start,e.Length)));
        }
        [Fact]
        public void AdjacentWordFormatRunsHaveAnExplicitBoundaryInsteadOfAnAmbiguousSplit()
        {
            const string source="Örnekmetin.";
            var plan=RewritePlan.Create(source,new[]{new TextSpan{Start=0,Length=5},new TextSpan{Start=5,Length=6}},Array.Empty<TextSpan>());
            Assert.False(plan.CanCompileNaturalText);
            string marker=RewritePlan.Marker(plan.Blocks.Single(b=>!b.Editable));
            var candidate=plan.BuildCandidate(plan.ExtractMaskedReplacements("Başka"+marker+"ifade."),"test");
            Assert.Equal("Başkaifade.",candidate.Text);
            Assert.Equal(2,candidate.Edits.Count);
            Assert.Throws<ArgumentException>(()=>plan.CompileNaturalText("Başka ifade."));
        }
        [Fact]
        public void OnlyDuplicatedTerminalPunctuationCanBeNormalizedAfterAFixedMarker()
        {
            const string source="Kayıt yokluğu bunu kanıtlamaz.";
            int start=source.IndexOf("kanıtlamaz",StringComparison.Ordinal);
            var plan=RewritePlan.Create(source,new[]{new TextSpan{Start=0,Length=source.Length}},new[]{new TextSpan{Start=start,Length=10}});
            string marker=RewritePlan.Marker(plan.Blocks.Last());
            var values=plan.ExtractMaskedReplacements("Bunu kayıt yokluğu "+marker+" .");
            Assert.Equal("Bunu kayıt yokluğu kanıtlamaz.",plan.BuildCandidate(values,"test").Text);
            Assert.Throws<ArgumentException>(()=>plan.ExtractMaskedReplacements("Bunu kayıt yokluğu "+marker+" Ek bilgi."));
        }
        [Fact]
        public void FixedFormattingOrderCannotBeChanged()
        {
            const string source="Bir isim sonra başka isim.";
            var plan=RewritePlan.Create(source,new[]{new TextSpan{Start=0,Length=source.Length}},new[]{new TextSpan{Start=4,Length=4},new TextSpan{Start=20,Length=4}});
            var markers=plan.Blocks.Where(b=>!b.Editable).Select(RewritePlan.Marker).ToArray();
            Assert.Throws<ArgumentException>(()=>plan.ExtractMaskedReplacements("Metin "+markers[1]+" ve "+markers[0]+"."));
        }
        [Fact]
        public void PreviewMustExactlyMatchAllProposedEdits()
        {
            var candidate = new Candidate { Text = "Başka bir önizleme", Edits = new List<TextEdit> { new TextEdit { Start = 0, Length = 5, Original = "Örnek", Replacement = "Farklı" } } };
            Assert.False(CandidateIntegrity.IsApplicable("Örnek metin", candidate, e => true));
        }
    }
}
