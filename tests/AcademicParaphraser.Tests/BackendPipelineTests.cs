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
        [Fact]
        public void EnglishPairedNominalCoordinationKeepsBothEndpointsDuringVoiceConversion()
        {
            const string text="The audit found a gap between sales and stock.";
            const string tree="1\tThe\tthe\tDET\t_\t_\t2\tdet\t_\t_\n2\taudit\taudit\tNOUN\t_\tNumber=Sing\t3\tnsubj\t_\t_\n3\tfound\tfind\tVERB\t_\tTense=Past|VerbForm=Fin\t0\troot\t_\t_\n4\ta\ta\tDET\t_\t_\t5\tdet\t_\t_\n5\tgap\tgap\tNOUN\t_\tNumber=Sing\t3\tobj\t_\t_\n6\tbetween\tbetween\tADP\t_\t_\t7\tcase\t_\t_\n7\tsales\tsale\tNOUN\t_\tNumber=Plur\t5\tnmod\t_\t_\n8\tand\tand\tCCONJ\t_\t_\t9\tcc\t_\t_\n9\tstock\tstock\tNOUN\t_\tNumber=Sing\t3\tconj\t_\t_\n10\t.\t.\tPUNCT\t_\t_\t3\tpunct\t_\t_\n";
            var source=ConlluReader.Parse(text,tree,"en");var context=new BackendContext{Source=source,Plan=RewritePlan.Create(text,new[]{new TextSpan{Start=0,Length=text.Length}},Array.Empty<TextSpan>())};
            var proposal=Assert.Single(new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None));
            Assert.Equal("A gap between sales and stock was found by the audit.",proposal.Text);
            const string targetTree="1\tA\ta\tDET\t_\t_\t2\tdet\t_\t_\n2\tgap\tgap\tNOUN\t_\tNumber=Sing\t8\tnsubj:pass\t_\t_\n3\tbetween\tbetween\tADP\t_\t_\t4\tcase\t_\t_\n4\tsales\tsale\tNOUN\t_\tNumber=Plur\t2\tnmod\t_\t_\n5\tand\tand\tCCONJ\t_\t_\t6\tcc\t_\t_\n6\tstock\tstock\tNOUN\t_\tNumber=Sing\t4\tconj\t_\t_\n7\twas\tbe\tAUX\t_\tTense=Past\t8\taux:pass\t_\t_\n8\tfound\tfind\tVERB\t_\tTense=Past|VerbForm=Part|Voice=Pass\t0\troot\t_\t_\n9\tby\tby\tADP\t_\t_\t11\tcase\t_\t_\n10\tthe\tthe\tDET\t_\t_\t11\tdet\t_\t_\n11\taudit\taudit\tNOUN\t_\tNumber=Sing\t8\tobl:agent\t_\t_\n12\t.\t.\tPUNCT\t_\t_\t8\tpunct\t_\t_\n";
            var target=ConlluReader.Parse(proposal.Text,targetTree,"en");
            Assert.True(new DerivationMeaningBackend().Compare(context,target,proposal).Passed);
        }
        [Fact]
        public void AnAlreadyCompleteBetweenPhraseDoesNotAbsorbAnAdditionalMatrixObject()
        {
            const string text="The audit found a gap between sales and stock and a loss.";
            const string tree="1\tThe\tthe\tDET\t_\t_\t2\tdet\t_\t_\n2\taudit\taudit\tNOUN\t_\tNumber=Sing\t3\tnsubj\t_\t_\n3\tfound\tfind\tVERB\t_\tTense=Past|VerbForm=Fin\t0\troot\t_\t_\n4\ta\ta\tDET\t_\t_\t5\tdet\t_\t_\n5\tgap\tgap\tNOUN\t_\tNumber=Sing\t3\tobj\t_\t_\n6\tbetween\tbetween\tADP\t_\t_\t7\tcase\t_\t_\n7\tsales\tsale\tNOUN\t_\tNumber=Plur\t5\tnmod\t_\t_\n8\tand\tand\tCCONJ\t_\t_\t9\tcc\t_\t_\n9\tstock\tstock\tNOUN\t_\tNumber=Sing\t7\tconj\t_\t_\n10\tand\tand\tCCONJ\t_\t_\t12\tcc\t_\t_\n11\ta\ta\tDET\t_\t_\t12\tdet\t_\t_\n12\tloss\tloss\tNOUN\t_\tNumber=Sing\t3\tconj\t_\t_\n13\t.\t.\tPUNCT\t_\t_\t3\tpunct\t_\t_\n";
            var source=ConlluReader.Parse(text,tree,"en");var context=new BackendContext{Source=source,Plan=RewritePlan.Create(text,new[]{new TextSpan{Start=0,Length=text.Length}},Array.Empty<TextSpan>())};
            Assert.Empty(new DependencyConstructionBackend().Generate(context,new UserSettings(),CancellationToken.None));
        }
        private static LinguisticAnalysis Target()=>ConlluReader.Parse("Raporu heyet inceledi.","1\tRaporu\trapor\tNOUN\t_\tCase=Acc\t3\tobj\t_\t_\n2\theyet\theyet\tNOUN\t_\tCase=Nom\t3\tnsubj\t_\t_\n3\tinceledi\tincele\tVERB\t_\tTense=Past|VerbForm=Fin\t0\troot\t_\t_\n4\t.\t.\tPUNCT\t_\t_\t3\tpunct\t_\t_\n","tr");
        [Fact]
        public async Task DefaultBackendScansEligibleRootsAndReportsTotalsAcrossParagraphs()
        {
            using var parser=new MorphParser();using var grammar=new Grammar();var knowledge=new RecordingKnowledge();
            string text=Source+"\r"+Source;var diagnostics=new MultiStageDiagnostics();
            var results=await new MultiStageRewriteEngine(new Catalog(),parser,grammar,knowledge:knowledge)
                .GenerateAsync(text,new UserSettings{InternetEnabled=true,OfflineMode=false},new[]{new TextSpan{Length=text.Length}},Array.Empty<TextSpan>(),CancellationToken.None,diagnostics:diagnostics);
            Assert.Equal(2,knowledge.Words.Count);Assert.True(knowledge.OnlineEnabled);
            Assert.Equal(6,diagnostics.Enrichment.RootsChecked);Assert.Equal(6,diagnostics.Enrichment.RootsTotal);
            Assert.Equal(2,diagnostics.DictionaryScans);Assert.Equal(2,diagnostics.Paragraphs);
            var result=Assert.Single(results);Assert.Equal("Raporu heyet inceledi.\rRaporu heyet inceledi.",result.Text);
            Assert.Same(diagnostics.Enrichment,result.Enrichment);
        }
        [Fact]
        public async Task LookupExcludesProtectedWordsAndParserNamesEvenWhenNamePreservationIsDisabled()
        {
            using var parser=new MorphParser{FirstWordIsName=true};using var grammar=new Grammar();var knowledge=new RecordingKnowledge();
            await new MultiStageRewriteEngine(new Catalog(),parser,grammar,knowledge:knowledge).GenerateAsync(Source,
                new UserSettings{PreserveNames=false},new[]{new TextSpan{Length=Source.Length}},new[]{new TextSpan{Start=6,Length=6}},CancellationToken.None);
            var words=Assert.Single(knowledge.Words);Assert.Single(words);Assert.Equal("incele",words[0].Lemma);
            Assert.False(knowledge.OnlineEnabled);
        }
        [Fact]
        public async Task NoConstructionHasAnExplicitReasonAndStillReportsTheDictionaryScan()
        {
            using var parser=new MorphParser();using var grammar=new Grammar();var diagnostics=new MultiStageDiagnostics();
            var results=await new MultiStageRewriteEngine(new Catalog(),parser,grammar,construction:new EmptyConstruction(),knowledge:new RecordingKnowledge())
                .GenerateAsync(Source,new UserSettings(),new[]{new TextSpan{Length=Source.Length}},Array.Empty<TextSpan>(),CancellationToken.None,diagnostics:diagnostics);
            Assert.Empty(results);Assert.Equal(1,diagnostics.UnsupportedUnits);Assert.Equal(0,diagnostics.Proposals);
            Assert.Contains("desteklenmiyor",diagnostics.Describe());Assert.Contains("3/3",diagnostics.Describe());
        }
        [Fact]
        public async Task AWhollyProtectedSelectionMakesNoDictionaryRequests()
        {
            using var parser=new MorphParser();using var grammar=new Grammar();var knowledge=new RecordingKnowledge();var diagnostics=new MultiStageDiagnostics();
            var results=await new MultiStageRewriteEngine(new Catalog(),parser,grammar,knowledge:knowledge).GenerateAsync(Source,
                new UserSettings(),new[]{new TextSpan{Length=Source.Length}},new[]{new TextSpan{Length=Source.Length}},CancellationToken.None,diagnostics:diagnostics);
            Assert.Empty(results);Assert.Empty(knowledge.Words);Assert.Equal(1,diagnostics.ProtectedUnits);
            Assert.Contains("Word alanı",diagnostics.Describe());
        }
        [Fact]
        public void LongParagraphsAreSplitWithoutChangingOffsetsOrSplittingCitationAndAbbreviation()
        {
            const string sentence="Dr. A. Deniz raporu inceledi (Yazar vd., 2024). ";
            string text=string.Concat(Enumerable.Repeat(sentence,100));
            var protections=new AcademicParaphraser.Core.CitationProtection.ProtectionDetector().Detect(text,new UserSettings(),Array.Empty<string>(),Array.Empty<LexiconEntry>());
            var units=BackendWorkUnitPlanner.Plan(text,protections);
            Assert.True(units.Count>1);Assert.All(units,u=>Assert.InRange(u.Length,1,2400));
            Assert.All(units,u=>{Assert.StartsWith("Dr. A. Deniz",text.Substring(u.Start,u.Length));Assert.EndsWith(").",text.Substring(u.Start,u.Length).TrimEnd());});
            Assert.All(protections,p=>Assert.DoesNotContain(units,u=>u.Start>p.Start&&u.Start<p.End));
            var edits=units.Select(u=>new TextEdit{Start=u.Start,Length=u.Length,Original=text.Substring(u.Start,u.Length),Replacement=text.Substring(u.Start,u.Length)}).ToList();
            Assert.Equal(text,EditApplication.Apply(text,edits));
        }
        [Fact]
        public async Task AnOversizedUnbrokenSentenceIsReportedWithoutStartingNativeProcesses()
        {
            using var parser=new Parser();using var grammar=new Grammar();var diagnostics=new MultiStageDiagnostics();string text=new string('a',2500)+".";
            var result=await new MultiStageRewriteEngine(new Catalog(),parser,grammar).GenerateAsync(text,new UserSettings(),new[]{new TextSpan{Length=text.Length}},Array.Empty<TextSpan>(),CancellationToken.None,diagnostics:diagnostics);
            Assert.Empty(result);Assert.Equal(0,parser.Calls);Assert.Equal(1,diagnostics.OversizedUnits);Assert.Contains("2.400",diagnostics.Describe());
        }
        [Fact]
        public async Task CancellingDuringDictionaryLookupDoesNotReturnAPartialRewrite()
        {
            using var parser=new MorphParser();using var grammar=new Grammar();using var cancel=new CancellationTokenSource();
            var knowledge=new RecordingKnowledge{Cancel=cancel};
            await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new MultiStageRewriteEngine(new Catalog(),parser,grammar,knowledge:knowledge)
                .GenerateAsync(Source,new UserSettings(),new[]{new TextSpan{Length=Source.Length}},Array.Empty<TextSpan>(),cancel.Token));
            Assert.Equal(0,grammar.Calls);
        }
        private sealed class EmptyConstruction:IConstructionBackend
        {
            public IReadOnlyList<ConstructionProposal> Generate(BackendContext context,UserSettings settings,CancellationToken cancellation)=>Array.Empty<ConstructionProposal>();
        }
        private sealed class MorphParser:ILinguisticBackend
        {
            public bool FirstWordIsName;public void Dispose(){}
            public Task<LinguisticAnalysis> AnalyzeAsync(string text,string language,CancellationToken cancellation)
            {
                cancellation.ThrowIfCancellationRequested();var result=text==Source?Context().Source:Target();AddMorphology(result);
                if(FirstWordIsName&&text==Source)result.Sentences[0].Words[0].Pos="PROPN";
                return Task.FromResult(result);
            }
        }
        private sealed class RecordingKnowledge:ILexicalSource
        {
            public List<IReadOnlyList<MorphToken>> Words=new List<IReadOnlyList<MorphToken>>();public bool OnlineEnabled;public CancellationTokenSource? Cancel;
            public Task<IReadOnlyList<LexiconEntry>> FindAsync(string text,IReadOnlyList<MorphToken> words,UserSettings settings,CancellationToken cancellation,EnrichmentContext? context=null)
            {
                Words.Add(words);OnlineEnabled=settings.InternetEnabled&&!settings.OfflineMode;Cancel?.Cancel();cancellation.ThrowIfCancellationRequested();
                context!.Summary.RootsTotal=words.Count(w=>w.Pos!="Punctuation");context.Summary.RootsChecked=context.Summary.RootsTotal;
                return Task.FromResult<IReadOnlyList<LexiconEntry>>(Array.Empty<LexiconEntry>());
            }
        }
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
