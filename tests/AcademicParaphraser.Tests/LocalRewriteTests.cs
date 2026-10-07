using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Rewriting;
using AcademicParaphraser.Infrastructure.LocalRewriting;
using Xunit;

namespace AcademicParaphraser.Tests
{
    public sealed class LocalRewriteTests
    {
        private const string Source = "Kurul dosyayı incelemiştir.";
        private static readonly TextSpan[] Writable = { new TextSpan { Start=0, Length=Source.Length } };
        [Fact]
        public async Task AddedBehaviorCannotBecomeWordProposalEvenIfFluent()
        {
            var model = new FakeModel("Dosyayı kurul incelemiştir ve başvuruyu onaylamıştır.", new MeaningReview { Equivalent=false,Natural=true,AddedInformation=true,Reasons="Onay kaynakta yok." });
            var diagnostics = new RewriteDiagnostics();
            var result = await new LocalRewriteEngine(new Catalog(),new Nlp(),model).GenerateAsync(Source,new UserSettings(),Writable,Array.Empty<TextSpan>(),CancellationToken.None,null,diagnostics);
            Assert.Empty(result);Assert.Equal(2,diagnostics.Rejected);Assert.True(model.Disposed);Assert.Equal(2,model.Reviews);
        }
        [Fact]
        public async Task RejectedFirstAttemptCanBeRepairedAndIsAcceptedAtomically()
        {
            var model = new FakeModel("Dosyayı kurul incelemiştir.",new MeaningReview{Equivalent=true,Natural=true});
            model.InvalidFirst=true;
            var result = await new LocalRewriteEngine(new Catalog(),new Nlp(),model).GenerateAsync(Source,new UserSettings(),Writable,Array.Empty<TextSpan>(),CancellationToken.None);
            var candidate=Assert.Single(result);Assert.Equal("Dosyayı kurul incelemiştir.",candidate.Text);
            Assert.Equal(1,model.Reviews);Assert.Equal(2,model.Writes);Assert.True(model.Disposed);
            Assert.Equal(0,candidate.Score.SemanticSafety);Assert.All(candidate.Edits,e=>Assert.Equal(0,e.Confidence));
        }
        [Fact]
        public async Task NewNumericalClaimIsRejectedBeforeModelReview()
        {
            var model = new FakeModel("Kurul 3 dosyayı incelemiştir.",new MeaningReview{Equivalent=true,Natural=true});
            Assert.Empty(await new LocalRewriteEngine(new Catalog(),new Nlp(),model).GenerateAsync(Source,new UserSettings(),Writable,Array.Empty<TextSpan>(),CancellationToken.None));
            Assert.Equal(0,model.Reviews);Assert.True(model.Disposed);
        }
        [Fact]
        public async Task CancellationReleasesSessionAndCannotYieldPartialProposal()
        {
            using var cancellation=new CancellationTokenSource();
            var model=new FakeModel("Dosya incelendi.",new MeaningReview{Equivalent=true,Natural=true}) { Cancel=cancellation };
            await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new LocalRewriteEngine(new Catalog(),new Nlp(),model).GenerateAsync(Source,new UserSettings(),Writable,Array.Empty<TextSpan>(),cancellation.Token));
            Assert.True(model.Disposed);
        }
        [Theory]
        [InlineData("Sıcaklık 42 °C ölçüldü.","Sıcaklık 42 °C ölçüldü; 42 gün sürdü.")]
        [InlineData("Veri yalnızca örneklem içindir.","Yalnızca bu veri yalnızca örneklem içindir.")]
        public void DuplicatedFactsInWritableBlocksAreDetected(string a,string b)=>Assert.NotEmpty(FactGuard.Check(a,b));
        [Fact]
        public void QuantityAnchorIncludesItsUnit()=>Assert.Contains(FactGuard.Anchors("Sıcaklık 42 °C ölçüldü."),s=>s.Start==9&&s.Length==5);
        [Theory]
        [InlineData("Oran 12 % olarak bulundu.","12 %")]
        [InlineData("Alan 24 m² ölçüldü.","24 m²")]
        [InlineData("Hacim 8 m³ ölçüldü.","8 m³")]
        public void SymbolAndSuperscriptUnitsArePartOfTheProtectedQuantity(string source,string quantity)
        {
            Assert.Contains(FactGuard.Anchors(source),s=>source.Substring(s.Start,s.Length)==quantity);
            Assert.NotEmpty(FactGuard.Check(source,source.Replace(quantity,quantity.Split(' ')[0]+" kg")));
        }
        [Fact]
        public async Task MissingModelGivesSetupInstructionRatherThanSilentlyWeakOutput()
        {
            var store=new LocalModelStore(System.IO.Path.Combine(System.IO.Path.GetTempPath(),Guid.NewGuid().ToString("N")));
            var exception=await Assert.ThrowsAsync<InvalidOperationException>(()=>store.VerifyAsync(CancellationToken.None));
            Assert.Contains("indir",exception.Message);Assert.False(store.IsDownloaded);
        }
        [Fact]
        public async Task EnglishGenerationNeverUsesTurkishMorphology()
        {
            const string text="The survey found a link between variables.";
            var model=new FakeModel("A link between variables was found by the survey.",new MeaningReview{Equivalent=true,Natural=true});
            var result=await new LocalRewriteEngine(new Catalog(),new RefusingNlp(),model).GenerateAsync(text,new UserSettings(),new[]{new TextSpan{Start=0,Length=text.Length}},Array.Empty<TextSpan>(),CancellationToken.None);
            Assert.Single(result);Assert.True(model.Disposed);
        }
        private sealed class RefusingNlp:ITurkishNlp
        {
            public string Status=>"never used for English";public void Dispose(){}
            public Task<IReadOnlyList<MorphToken>> AnalyzeAsync(string text,CancellationToken token)=>throw new InvalidOperationException("English was routed to Turkish NLP.");
            public Task<string?> InflectAsync(string a,string b,string p,CancellationToken t)=>throw new InvalidOperationException();
        }
        [Fact]
        public void EvidenceAbsenceCannotBecomeAbsenceOfAStatement()
        {
            Assert.NotEmpty(FactGuard.Check("Bu ölçüm sürekliliği göstermemektedir.","Süreklilik söylenmemektedir."));
        }
        [Theory]
        [InlineData("The survey found an association.",true)]
        [InlineData("Bu araştırma ilişkileri incelemektedir.",false)]
        public void EnglishTextDoesNotGoThroughTurkishProperNounGuessing(string text,bool english)=>Assert.Equal(english,LanguageProfile.IsEnglish(text,MetinDili.Otomatik));
        [Fact]
        public void SynonymOnlyProposalIsNotAReconstruction()
        {
            Assert.False(StructuralDiversity.HasSyntaxChange("Mektuplar ticari ilişkiler hakkında bilgi sağlamaktadır.","Mektuplar ticari ilişkiler hakkında bilgi vermektedir.",Array.Empty<MorphToken>(),Array.Empty<MorphToken>()));
            Assert.True(StructuralDiversity.HasSyntaxChange("Kurul bugün dosyayı incelemiştir.","Dosyayı bugün kurul incelemiştir.",Array.Empty<MorphToken>(),Array.Empty<MorphToken>()));
        }
        [Fact]
        public void PassiveReconstructionMayKeepFutureButCannotTurnPastIntoFuture()
        {
            MorphToken Token(params string[] features)=>new MorphToken{Lemma="tamamlamak",Pos="Verb",Morphemes=features.ToList()};
            Assert.Empty(FactGuard.CheckPredicateFeatures(new[]{Token("Verb","Fut")},new[]{Token("Verb","Pass","Fut")}));
            Assert.NotEmpty(FactGuard.CheckPredicateFeatures(new[]{Token("Verb","Past")},new[]{Token("Verb","Fut")}));
            Assert.NotEmpty(FactGuard.CheckPredicateFeatures(new[]{Token("Verb","Neg","Past")},new[]{Token("Verb","Past")}));
        }
        private sealed class Catalog:IRuleCatalog
        {
            public IReadOnlyList<RuleDefinition> GetRules()=>Array.Empty<RuleDefinition>();
            public IReadOnlyList<LexiconEntry> GetLexicon()=>Array.Empty<LexiconEntry>();
            public IReadOnlyList<string> GetLockedTerms()=>Array.Empty<string>();
        }
        private sealed class Nlp:ITurkishNlp
        {
            public string Status=>"test double";
            public void Dispose(){}
            public Task<IReadOnlyList<MorphToken>> AnalyzeAsync(string text,CancellationToken token){token.ThrowIfCancellationRequested();return Task.FromResult<IReadOnlyList<MorphToken>>(Array.Empty<MorphToken>());}
            public Task<string?> InflectAsync(string a,string b,string p,CancellationToken t)=>Task.FromResult<string?>(null);
        }
        private sealed class FakeModel:IRewriteModel,IRewriteSession
        {
            private readonly string replacement;private readonly MeaningReview review;
            public bool Disposed,InvalidFirst;public int Reviews,Writes;public CancellationTokenSource? Cancel;
            public FakeModel(string replacement,MeaningReview review){this.replacement=replacement;this.review=review;}
            public Task<IRewriteSession> OpenAsync(CancellationToken token)=>Task.FromResult<IRewriteSession>(this);
            public Task<IReadOnlyDictionary<string,string>> RewriteAsync(RewritePlan plan,Strength strength,string feedback,CancellationToken token)
            {
                Writes++;if(Cancel!=null){Cancel.Cancel();token.ThrowIfCancellationRequested();}
                IReadOnlyDictionary<string,string> result=InvalidFirst&&Writes==1?new Dictionary<string,string>():plan.Blocks.Where(b=>b.Editable).ToDictionary(b=>b.Id,b=>replacement);
                return Task.FromResult(result);
            }
            public Task<MeaningReview> ReviewAsync(string source,string target,CancellationToken token){Reviews++;return Task.FromResult(review);}
            public void Dispose()=>Disposed=true;
        }
    }
}
