using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Backends;
using AcademicParaphraser.Core.Rewriting;
using AcademicParaphraser.Infrastructure.Backends;
using Xunit;
namespace AcademicParaphraser.Tests
{
    public sealed class BackendPipelineTests
    {
        private const string Source="Heyet raporu inceledi.";
        private const string Tree="1\tHeyet\theyet\tNOUN\t_\tCase=Nom\t3\tnsubj\t_\t_\n2\traporu\trapor\tNOUN\t_\tCase=Acc\t3\tobj\t_\t_\n3\tinceledi\tincele\tVERB\t_\tTense=Past|VerbForm=Fin\t0\troot\t_\t_\n4\t.\t.\tPUNCT\t_\t_\t3\tpunct\t_\t_\n";
        [Fact]
        public void ParsedOffsetsAreExactUtf16IncludingSupplementaryCharacters()
        {
            const string text="😀 Heyet raporu inceledi.";
            string tree="1\t😀\t😀\tSYM\t_\t_\t4\tdiscourse\t_\t_\n"+Tree.Replace("1\tHeyet","2\tHeyet").Replace("2\traporu","3\traporu").Replace("3\tinceledi","4\tinceledi").Replace("4\t.\t","5\t.\t").Replace("\t3\tnsubj","\t4\tnsubj").Replace("\t3\tobj","\t4\tobj").Replace("\t3\tpunct","\t4\tpunct");
            var parsed=ConlluReader.Parse(text,tree,"tr");
            Assert.All(parsed.Sentences.SelectMany(s=>s.Words),w=>Assert.Equal(w.Form,text.Substring(w.Start,w.Length)));
            Assert.Equal(3,parsed.Sentences[0].Words[1].Start);
        }
        [Fact]
        public void MissingTokensAndCyclesAreRejectedBeforeWordMapping()
        {
            Assert.Throws<ArgumentException>(()=>ConlluReader.Parse("Heyet yeni raporu inceledi.",Tree,"tr"));
            Assert.Throws<ArgumentException>(()=>ConlluReader.Parse(Source,Tree.Replace("\t3\tnsubj","\t2\tnsubj").Replace("\t3\tobj","\t1\tobj"),"tr"));
        }
        [Fact]
        public void AWholeObjectIsFrontedWithoutAThesaurusChange()
        {
            var context=Context();var proposals=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None);
            Assert.Equal("Raporu heyet inceledi.",Assert.Single(proposals).Text);
        }
        [Fact]
        public void AFluentInventedActionCannotPassTheRecordedConstruction()
        {
            var context=Context();var proposal=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None).Single();
            proposal.Text="Heyet raporu onayladı.";
            var target=ConlluReader.Parse(proposal.Text,Tree.Replace("inceledi","onayladı").Replace("incele\t","onayla\t"),"tr");
            Assert.False(new DerivationMeaningBackend().Compare(context,target,proposal).Passed);
        }
        [Fact]
        public void AnIndependentActorPatientDisagreementRejectsEvenAnExactReplay()
        {
            var context=Context();var proposal=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None).Single();
            var target=Target();target.Sentences[0].Words[0].Relation="nsubj";target.Sentences[0].Words[1].Relation="obj";
            Assert.False(new DerivationMeaningBackend().Compare(context,target,proposal).Passed);
        }
        [Fact]
        public void NewGrammarErrorsAreRejectedButUnchangedSourceErrorsAreNotInventedAsNew()
        {
            var context=Context();var proposal=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None).Single();var target=Target();
            var meaning=new DerivationMeaningBackend().Compare(context,target,proposal);Assert.True(meaning.Passed);
            var issue=new GrammarIssue{Code="PERSON",Kind="grammar",Message="Person disagreement"};
            Assert.False(new IndependentFinalEvaluationBackend().Evaluate(context,target,proposal,new[]{issue},meaning).Accepted);
            context.OriginalGrammar=new[]{issue};Assert.True(new IndependentFinalEvaluationBackend().Evaluate(context,target,proposal,new[]{issue},meaning).Accepted);
        }
        [Fact]
        public void IndependentEnglishNegationIsCheckedWithoutTurkishMorphologyOrAConstructionTrace()
        {
            var before=ConlluReader.Parse("Review was not completed.","1\tReview\treview\tNOUN\t_\t_\t4\tnsubj:pass\t_\t_\n2\twas\tbe\tAUX\t_\tTense=Past\t4\taux:pass\t_\t_\n3\tnot\tnot\tPART\t_\t_\t4\tadvmod\t_\t_\n4\tcompleted\tcomplete\tVERB\t_\tVoice=Pass|Tense=Past\t0\troot\t_\t_\n5\t.\t.\tPUNCT\t_\t_\t4\tpunct\t_\t_\n","en");
            var after=ConlluReader.Parse("Review was completed.","1\tReview\treview\tNOUN\t_\t_\t3\tnsubj:pass\t_\t_\n2\twas\tbe\tAUX\t_\tTense=Past\t3\taux:pass\t_\t_\n3\tcompleted\tcomplete\tVERB\t_\tVoice=Pass|Tense=Past\t0\troot\t_\t_\n4\t.\t.\tPUNCT\t_\t_\t3\tpunct\t_\t_\n","en");
            Assert.False(new DerivationMeaningBackend().CompareFacts(before,after,Array.Empty<LexiconEntry>()).Passed);
        }
        [Fact]
        public void AFocusParticleMakesTheMovementIneligible()
        {
            var context=Context();context.Source.Sentences[0].Words[0].Form="yalnızca";
            Assert.Empty(new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None));
        }
        [Fact]
        public async Task PipelineDoesNotInvokeAChatModelAndCompilesOnlyWritableWordRanges()
        {
            using var parser=new Parser();using var grammar=new Grammar();
            var result=await new MultiStageRewriteEngine(new Catalog(),parser,grammar).GenerateAsync(Source,new UserSettings(),new[]{new TextSpan{Start=0,Length=Source.Length}},Array.Empty<TextSpan>(),CancellationToken.None);
            var proposal=Assert.Single(result);Assert.Equal("Raporu heyet inceledi.",proposal.Text);Assert.Equal(1,proposal.RewrittenSentences);
            Assert.Equal(0,proposal.Score.SemanticSafety);Assert.Equal(2,parser.Calls);Assert.Equal(2,grammar.Calls);
        }
        [Fact]
        public void SeparateSentenceStylesDoNotPreventAnEditWithinOneStyle()
        {
            const string text="Heyet raporu inceledi. Kaynak bulundu.";
            var parsed=ConlluReader.Parse(Source,Tree,"tr");parsed.Text=text;
            var plan=RewritePlan.Create(text,new[]{new TextSpan{Start=0,Length=22},new TextSpan{Start=22,Length=text.Length-22}},Array.Empty<TextSpan>());
            Assert.False(plan.CanCompileNaturalText);
            var context=new BackendContext{Source=parsed,Plan=plan};
            var proposal=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None).Single();
            var result=WordProposalCompiler.Compile(context,proposal);
            Assert.Equal("Raporu heyet inceledi. Kaynak bulundu.",result.Text);
            Assert.All(result.Edits,e=>Assert.True(e.Start+e.Length<=22));
        }
        [Fact]
        public void AConstructionCrossingAnInlineStyleBoundaryIsRejected()
        {
            var context=Context();context.Plan=RewritePlan.Create(Source,new[]{new TextSpan{Start=0,Length=6},new TextSpan{Start=6,Length=Source.Length-6}},Array.Empty<TextSpan>());
            var proposal=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None).Single();
            Assert.Throws<ArgumentException>(()=>WordProposalCompiler.Compile(context,proposal));
        }
        [Fact]
        public async Task CancellationCannotYieldAPartialProposal()
        {
            using var parser=new Parser();using var grammar=new Grammar();using var cancel=new CancellationTokenSource();cancel.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new MultiStageRewriteEngine(new Catalog(),parser,grammar).GenerateAsync(Source,new UserSettings(),new[]{new TextSpan{Start=0,Length=Source.Length}},Array.Empty<TextSpan>(),cancel.Token));
            Assert.Equal(0,parser.Calls);
        }
        private static BackendContext Context()=>new BackendContext{Source=ConlluReader.Parse(Source,Tree,"tr"),Plan=RewritePlan.Create(Source,new[]{new TextSpan{Start=0,Length=Source.Length}},Array.Empty<TextSpan>())};
        [Fact]
        public void MarkedMovementNeedsBothUnchangedMorphologyAndIndependentSemanticSupport()
        {
            var context=Context();var target=Target();AddMorphology(context.Source);AddMorphology(target);
            var proposal=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None).Single();
            target.Sentences[0].Words[0].Relation="nsubj";target.Sentences[0].Words[1].Relation="obj";
            var judge=new DerivationMeaningBackend();
            Assert.False(judge.Compare(context,target,proposal).Passed);
            context.SemanticEvidence=new BackendEvidence{Stage="test-semantic",Passed=true};
            Assert.True(judge.Compare(context,target,proposal).Passed);
            target.Morphology.Single(t=>t.Lemma=="rapor").Morphemes.Remove("Acc");
            Assert.False(judge.Compare(context,target,proposal).Passed);
            AddMorphology(target);target.Morphology=target.Morphology.Where(t=>t.Lemma!="heyet").ToList();
            Assert.False(judge.Compare(context,target,proposal).Passed);
        }
        [Fact]
        public void SemanticSupportCannotAuthorizeAnUnmarkedObjectWhenTreesDisagree()
        {
            var context=Context();var target=Target();AddMorphology(context.Source);AddMorphology(target);
            context.Source.Morphology.Single(t=>t.Lemma=="rapor").Morphemes.Remove("Acc");
            target.Morphology.Single(t=>t.Lemma=="rapor").Morphemes.Remove("Acc");
            var proposal=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None).Single();
            target.Sentences[0].Words[0].Relation="nsubj";target.Sentences[0].Words[1].Relation="obj";
            context.SemanticEvidence=new BackendEvidence{Passed=true};
            Assert.False(new DerivationMeaningBackend().Compare(context,target,proposal).Passed);
        }
        [Fact]
        public void AClassifierCannotOverrideAnInventedActionOrAFailedReplay()
        {
            var context=Context();context.SemanticEvidence=new BackendEvidence{Passed=true};AddMorphology(context.Source);
            var proposal=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None).Single();
            proposal.Text="Heyet raporu onayladı.";
            var target=ConlluReader.Parse(proposal.Text,Tree.Replace("inceledi","onayladı").Replace("incele\t","onayla\t"),"tr");AddMorphology(target);
            Assert.False(new DerivationMeaningBackend().Compare(context,target,proposal).Passed);
        }
        [Fact]
        public void AFailedSemanticCheckVetoesAnOtherwiseValidConstruction()
        {
            var context=Context();context.SemanticEvidence=new BackendEvidence{Passed=false,Problems=new List<string>{"Test semantic veto"}};
            var proposal=new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None).Single();
            var meaning=new DerivationMeaningBackend().Compare(context,Target(),proposal);
            Assert.False(new IndependentFinalEvaluationBackend().Evaluate(context,Target(),proposal,Array.Empty<GrammarIssue>(),meaning).Accepted);
        }
        private static void AddMorphology(LinguisticAnalysis parsed)
        {
            parsed.Morphology=parsed.Sentences.SelectMany(s=>s.Words).Select(w=>new MorphToken{Start=w.Start,Length=w.Length,Surface=w.Form,Lemma=w.Lemma,Pos=w.Pos=="NOUN"?"Noun":w.Pos=="VERB"?"Verb":"Punctuation",Morphemes=w.Pos=="VERB"?new List<string>{"Verb","Past","A3sg"}:w.Pos=="NOUN"?new List<string>{"Noun","A3sg"}:new List<string>{"Punc"}}).ToList();
            foreach(var token in parsed.Morphology.Where(t=>t.Lemma=="rapor"))token.Morphemes.Add("Acc");
        }
        private static LinguisticAnalysis Target()=>ConlluReader.Parse("Raporu heyet inceledi.","1\tRaporu\trapor\tNOUN\t_\tCase=Acc\t3\tobj\t_\t_\n2\theyet\theyet\tNOUN\t_\tCase=Nom\t3\tnsubj\t_\t_\n3\tinceledi\tincele\tVERB\t_\tTense=Past|VerbForm=Fin\t0\troot\t_\t_\n4\t.\t.\tPUNCT\t_\t_\t3\tpunct\t_\t_\n","tr");
        private sealed class Parser:ILinguisticBackend
        {
            public int Calls;public void Dispose(){}
            public Task<LinguisticAnalysis> AnalyzeAsync(string text,string language,CancellationToken token){token.ThrowIfCancellationRequested();Calls++;return Task.FromResult(text==Source?Context().Source:Target());}
        }
        private sealed class Grammar:IGrammarBackend
        {
            public int Calls;public void Dispose(){}public Task<IReadOnlyList<GrammarIssue>> CheckAsync(LinguisticAnalysis analysis,CancellationToken token){Calls++;return Task.FromResult<IReadOnlyList<GrammarIssue>>(Array.Empty<GrammarIssue>());}
        }
        private sealed class Catalog:IRuleCatalog
        {
            public IReadOnlyList<RuleDefinition> GetRules()=>Array.Empty<RuleDefinition>();public IReadOnlyList<LexiconEntry> GetLexicon()=>Array.Empty<LexiconEntry>();public IReadOnlyList<string> GetLockedTerms()=>Array.Empty<string>();
        }
    }
}
