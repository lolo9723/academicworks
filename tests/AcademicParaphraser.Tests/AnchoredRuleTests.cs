using System.Linq;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Rewriting;
using Xunit;

namespace AcademicParaphraser.Tests
{
    public sealed class AnchoredRuleTests
    {
        private static RuleDefinition Rule => new RuleDefinition { Id = "test", Family = "test", Confidence = .95 };

        [Fact]
        public void SentenceChangeSplitsAroundProtectedStatistic()
        {
            const string source = "Araştırmada 12 destinasyon incelenmiştir.";
            const string replacement = "Çalışmada 12 destinasyon incelemeye tabi tutulmuştur.";
            var span = new TextSpan { Start = source.IndexOf("12"), Length = "12 destinasyon".Length };
            var edits = AnchoredRuleEdits.Create(source, 0, source.Length, replacement, new[] { span }, Rule);
            Assert.NotNull(edits);
            Assert.Equal(2, edits!.Count);
            Assert.All(edits, edit => Assert.False(span.Intersects(edit.Start, edit.Length)));
            Assert.Equal(replacement, EditApplication.Apply(source, edits));
            var candidate = new Candidate { Text = replacement, Edits = edits.ToList() };
            Assert.False(CandidateIntegrity.IsApplicable(source, candidate, edit => edit.Start > span.End));
        }

        [Fact]
        public void ChangedOrMissingProtectedAnchorRejectsWholeRule()
        {
            const string source = "12 destinasyon incelenmiştir.";
            var span = new TextSpan { Start = 0, Length = "12 destinasyon".Length };
            Assert.Null(AnchoredRuleEdits.Create(source, 0, source.Length, "13 destinasyon değerlendirilmiştir.", new[] { span }, Rule));
        }

        [Fact]
        public void InsertionImmediatelyBeforeProtectedRangeCannotBorrowItsCharacter()
        {
            const string source = "12 destinasyon incelenmiştir.";
            var span = new TextSpan { Start = 0, Length = "12 destinasyon".Length };
            Assert.Null(AnchoredRuleEdits.Create(source, 0, source.Length, "Toplam 12 destinasyon incelenmiştir.", new[] { span }, Rule));
        }
    }
}
