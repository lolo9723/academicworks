using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Backends;
using AcademicParaphraser.Core.Rewriting;
using Xunit;

namespace AcademicParaphraser.Tests
{
    public sealed class ClauseMeaningTests:IClassFixture<NlpFixture>
    {
        private readonly NlpFixture fixture;
        public ClauseMeaningTests(NlpFixture fixture){this.fixture=fixture;}
        private async Task<LinguisticAnalysis> Analyze(string text)
        {
            var tokens=await fixture.Nlp.AnalyzeAsync(text,CancellationToken.None);
            return new LinguisticAnalysis{Text=text,Language="tr",Morphology=tokens,
                Sentences=new[]{new DependencySentence{Length=text.Length,Words=tokens.Select((t,i)=>new DependencyWord{Id=i+1,Head=i==tokens.Count-2?0:tokens.Count-1,
                    Start=t.Start,Length=t.Length,Form=t.Surface,Lemma=t.Lemma,Pos=t.Pos=="Punctuation"?"PUNCT":t.Pos=="Verb"?"VERB":"NOUN",Relation=i==tokens.Count-2?"root":"dep"}).ToList()}}};
        }
        [Theory]
        [InlineData("Bu projenin amacı, enerji tüketimini azaltmaktır.","Bu proje, enerji tüketimini azaltmayı amaçlamaktadır.",true)]
        [InlineData("Bu programın hedefi okuma becerisini geliştirmektir.","Bu program okuma becerisini geliştirmeyi hedeflemektedir.",true)]
        [InlineData("Bu projenin amacı, enerji tüketimini azaltmaktır.","Bu proje, enerji tüketimini artırmayı amaçlamaktadır.",false)]
        [InlineData("Bu projenin amacı, enerji tüketimini azaltmaktır.","Bu program, enerji tüketimini azaltmayı amaçlamaktadır.",false)]
        [InlineData("Bu projenin amacı, enerji tüketimini azaltmaktır.","Bu proje, enerji tüketimini azaltmamayı amaçlamaktadır.",false)]
        [InlineData("Bu projenin amacı, enerji tüketimini azaltmaktır.","Bu proje, enerji tüketimini azaltmıştır.",false)]
        [InlineData("Bu projenin hedefi, enerji tüketimini azaltmaktır.","Bu proje, enerji tüketimini azaltmayı amaçlamaktadır.",false)]
        [InlineData("Bu projenin amacı, enerji tüketimini azaltmaktır.","Bu proje, enerji tüketimini önemli ölçüde azaltmayı amaçlamaktadır.",false)]
        [InlineData("Bu projenin amacı, enerji tüketimini azaltmaktır.","Bu projen, enerji tüketimini azaltmayı amaçlamaktadır.",false)]
        public async Task PurposeFrameMatchesOwnerClaimAndModality(string source,string target,bool equivalent)
        {
            var before=await Analyze(source);var after=await Analyze(target);
            Assert.Equal(equivalent,ClauseMeaningIntegrity.EquivalentPurpose(before,after));
            Assert.Equal(equivalent,ClauseMeaningIntegrity.EquivalentPurpose(after,before));
        }
        [Fact]
        public async Task AProvenPurposeFrameCannotApproveWithoutIndependentSemanticEvidence()
        {
            const string source="Bu projenin amacı, enerji tüketimini azaltmaktır.";
            var before=await Analyze(source);var after=await Analyze("Bu proje, enerji tüketimini azaltmayı amaçlamaktadır.");
            var context=new BackendContext{Source=before,Plan=RewritePlan.Create(source,new[]{new TextSpan{Length=source.Length}},Array.Empty<TextSpan>())};
            Assert.False(ClauseMeaningIntegrity.Compare(context,after,new[]{0}).Passed);
            context.SemanticEvidence=new BackendEvidence{Passed=false};Assert.False(ClauseMeaningIntegrity.Compare(context,after,new[]{0}).Passed);
            context.SemanticEvidence=new BackendEvidence{Passed=true};Assert.True(ClauseMeaningIntegrity.Compare(context,after,new[]{0}).Passed);
        }
        [Fact]
        public async Task UnknownMorphologyCannotBeCertifiedByThePurposeFrame()
        {
            var before=await Analyze("Bu projenin amacı, enerji tüketimini azaltmaktır.");var after=await Analyze("Bu proje, enerji tüketimini azaltmayı amaçlamaktadır.");
            after.Morphology.Last(t=>t.Pos!="Punctuation").RuntimeGuess=true;
            Assert.False(ClauseMeaningIntegrity.EquivalentPurpose(before,after));
        }
        [Theory]
        [InlineData("projenin","Nominative","proje")]
        [InlineData("programın","Nominative","program")]
        [InlineData("azaltmaktır","PurposeAcc","azaltmayı")]
        [InlineData("geliştirmektir","PurposeAcc","geliştirmeyi")]
        [InlineData("azaltmamaktır","PurposeAcc","azaltmamayı")]
        public async Task PurposeCaseFormsAreGeneratedFromActualMorphology(string source,string mode,string expected)
        {
            var tokens=await fixture.Nlp.AnalyzeAsync(source,CancellationToken.None);
            Assert.Equal(expected,await fixture.Nlp.TransformStructureAsync(tokens.Single(),mode,CancellationToken.None));
        }
        [Theory]
        [InlineData("Bu projenin amacı, enerji tüketimini azaltmaktır.","Bu proje, enerji tüketimini azaltmayı amaçlamaktadır.")]
        [InlineData("Bu programın hedefi, okuma becerisini geliştirmektir.","Bu program, okuma becerisini geliştirmeyi hedeflemektedir.")]
        [InlineData("Bu projenin amacı, enerji tüketimini artırmamaktır.","Bu proje, enerji tüketimini artırmamayı amaçlamaktadır.")]
        public async Task IndependentRuleProducerActuallyBuildsPurposeFramesWithoutWordChoice(string source,string expected)
        {
            var before=await Analyze(source);var context=new BackendContext{Source=before,Plan=RewritePlan.Create(source,new[]{new TextSpan{Length=source.Length}},Array.Empty<TextSpan>())};
            var candidates=await new RuleConstructionBackend(new EmptyCatalog(),fixture.Nlp).GenerateAsync(context,new UserSettings{EnableWordChoice=true},CancellationToken.None,new EnrichmentContext());
            Assert.Contains(candidates,c=>c.Text==expected);
            Assert.All(candidates,c=>Assert.DoesNotContain(c.Edits,e=>e.RuleId.StartsWith("lemma:",StringComparison.Ordinal)));
        }
        private sealed class EmptyCatalog:IRuleCatalog
        {
            public IReadOnlyList<RuleDefinition> GetRules()=>Array.Empty<RuleDefinition>();
            public IReadOnlyList<LexiconEntry> GetLexicon()=>Array.Empty<LexiconEntry>();
            public IReadOnlyList<string> GetLockedTerms()=>Array.Empty<string>();
        }
        [Theory]
        [InlineData("Rapor yeterli değildir.","Rapor yeterlidir.","olumsuzluk")]
        [InlineData("Yalnızca kurul raporu inceledi.","Kurul yalnızca raporu inceledi.","Odak parçacığı")]
        public async Task FalsePositiveSemanticEvidenceCannotOverrideNegationOrFocusScope(string source,string target,string expectedReason)
        {
            var before=await Analyze(source);var after=await Analyze(target);
            var context=new BackendContext{Source=before,Plan=RewritePlan.Create(source,new[]{new TextSpan{Length=source.Length}},Array.Empty<TextSpan>()),SemanticEvidence=new BackendEvidence{Passed=true}};
            var result=ClauseMeaningIntegrity.Compare(context,after,new[]{0});
            Assert.False(result.Passed);Assert.Contains(result.Problems,p=>p.Contains(expectedReason));
        }
        [Theory]
        [InlineData("Müfettiş başvuruya ilişkin belgeleri değerlendirdi.","Başvuruya ilişkin belgeleri müfettiş değerlendirdi.",true)]
        [InlineData("Müfettiş başvuruya ilişkin belgeleri değerlendirdi.","İlişkin belgeleri müfettiş başvuruya değerlendirdi.",false)]
        public async Task PostpositionComplementsStayAttachedWhenTheirPhraseMoves(string source,string target,bool valid)
        {
            var before=await Analyze(source);var after=await Analyze(target);
            Assert.Equal(valid,FactGuard.CheckPostpositionBindings(before.Morphology,after.Morphology).Count==0);
            if(!valid)
            {
                var context=new BackendContext{Source=before,Plan=RewritePlan.Create(source,new[]{new TextSpan{Length=source.Length}},Array.Empty<TextSpan>()),SemanticEvidence=new BackendEvidence{Passed=true}};
                Assert.False(ClauseMeaningIntegrity.Compare(context,after,new[]{0}).Passed);
            }
        }
    }
}
