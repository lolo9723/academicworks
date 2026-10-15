using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core.CitationProtection;
using AcademicParaphraser.Core.Rewriting;
namespace AcademicParaphraser.Core.Backends
{
    public sealed class MultiStageRewriteEngine
    {
        private readonly IRuleCatalog catalog;private readonly ILinguisticBackend analysis;private readonly IGrammarBackend grammar;
        private readonly IConstructionBackend construction;private readonly IMeaningBackend meaning;private readonly IFinalEvaluationBackend evaluation;
        private readonly ISemanticBackend? semantic;
        private readonly ILexicalSource? knowledge;
        private readonly ISupplementalConstructionBackend? supplemental;
        public MultiStageRewriteEngine(IRuleCatalog catalog,ILinguisticBackend analysis,IGrammarBackend grammar,IConstructionBackend? construction=null,IMeaningBackend? meaning=null,IFinalEvaluationBackend? evaluation=null,ISemanticBackend? semantic=null,ILexicalSource? knowledge=null,ISupplementalConstructionBackend? supplemental=null)
        {
            this.catalog=catalog;this.analysis=analysis;this.grammar=grammar;
            this.construction=construction??new DependencyConstructionBackend();this.meaning=meaning??new DerivationMeaningBackend();this.evaluation=evaluation??new IndependentFinalEvaluationBackend();
            this.semantic=semantic;
            this.knowledge=knowledge;
            this.supplemental=supplemental;
        }
        public async Task<IReadOnlyList<Candidate>> GenerateAsync(string source,UserSettings settings,IEnumerable<TextSpan> writable,IEnumerable<TextSpan> external,CancellationToken cancellation,IProgress<string>? progress=null,MultiStageDiagnostics? diagnostics=null,EnrichmentContext? enrichment=null)
        {
            if(string.IsNullOrWhiteSpace(source)||source.Length>80000)throw new ArgumentException("En fazla 80.000 karakterlik bir seçim yapın.");
            diagnostics=diagnostics??new MultiStageDiagnostics();var allowed=writable.ToList();
            enrichment=enrichment??new EnrichmentContext{Progress=progress};
            enrichment.Summary.StructureStatus=supplemental==null?"çok aşamalı motor: internet yapı bankası kullanılmıyor":"ek cümle üreticisi hazırlanıyor";
            diagnostics.Enrichment=enrichment.Summary;
            var fixedSpans=new ProtectionDetector().Detect(source,settings,catalog.GetLockedTerms(),catalog.GetLexicon(),external).ToList();
            fixedSpans.AddRange(FactGuard.Anchors(source));var accepted=new List<List<TextEdit>>();int reconstructed=0;int sentenceCount=0;
            diagnostics.Paragraphs=Regex.Matches(source,@"[^\r\n\a\v\f]+").Count;
            foreach(var paragraph in BackendWorkUnitPlanner.Plan(source,fixedSpans))
            {
                cancellation.ThrowIfCancellationRequested();diagnostics.WorkUnits++;
                if(paragraph.Length>BackendWorkUnitPlanner.MaximumLength){diagnostics.OversizedUnits++;continue;}
                string text=source.Substring(paragraph.Start,paragraph.Length);string language=LanguageProfile.IsEnglish(text,settings.InputLanguage)?"en":"tr";
                progress?.Report("Cümleler ve dil bilgisi ilişkileri çözümleniyor…");
                var parsed=await analysis.AnalyzeAsync(text,language,cancellation).ConfigureAwait(false);sentenceCount+=parsed.Sentences.Count;
                if(language=="en"&&grammar is IEnglishMorphologyBackend english)await english.RefineVerbLemmasAsync(parsed,cancellation).ConfigureAwait(false);
                var protectedSpans=Clip(fixedSpans,paragraph.Start,paragraph.Length);
                if(settings.PreserveNames)
                {
                    protectedSpans.AddRange(parsed.Sentences.SelectMany(s=>s.Words).Where(w=>w.Pos=="PROPN").Select(w=>new TextSpan{Start=w.Start,Length=w.Length,Reason="bağımlılık özel isim"}));
                    protectedSpans.AddRange(parsed.Morphology.Where(w=>w.Proper&&!w.AmbiguousProper&&!w.RuntimeGuess).Select(w=>new TextSpan{Start=w.Start,Length=w.Length,Reason="morfoloji özel isim"}));
                }
                var plan=RewritePlan.Create(text,Clip(allowed,paragraph.Start,paragraph.Length),protectedSpans);
                // A tree rewrite is allowed only when its output has an exact Word mapping.
                if(!plan.Blocks.Any(b=>b.Editable)){diagnostics.ProtectedUnits++;continue;}
                IReadOnlyList<LexiconEntry> lexical=Array.Empty<LexiconEntry>();
                if(knowledge!=null&&language=="tr")
                {
                    var names=parsed.Sentences.SelectMany(s=>s.Words).Where(w=>w.Pos=="PROPN").ToArray();
                    var eligible=parsed.Morphology.Where(w=>!w.Proper&&w.Lemma.Length>=2&&w.Lemma.All(char.IsLetter)&&!names.Any(n=>n.Start<w.Start+w.Length&&n.End>w.Start)
                        &&!protectedSpans.Any(p=>p.Intersects(w.Start,w.Length))
                        &&plan.Blocks.Any(b=>b.Editable&&w.Start>=b.Start&&w.Start+w.Length<=b.Start+b.Text.Length)).ToArray();
                    // Only eligible roots leave the device when the user enables online
                    // lookup. Full text is used locally for sense selection, never uploaded.
                    var scan=new EnrichmentContext{Progress=progress,DefinitionsOnly=true};
                    lexical=await knowledge.FindAsync(text,eligible,settings,cancellation,scan).ConfigureAwait(false);
                    diagnostics.DictionaryScans++;AddScan(enrichment.Summary,scan.Summary);
                }
                else if(language=="en")diagnostics.DictionaryUnsupportedUnits++;
                var context=new BackendContext{Source=parsed,Plan=plan,Lexicon=catalog.GetLexicon().Concat(lexical).ToArray(),OriginalGrammar=await grammar.CheckAsync(parsed,cancellation).ConfigureAwait(false)};
                progress?.Report("Cümle kuruluşu seçenekleri hazırlanıyor…");var alternatives=construction.Generate(context,settings,cancellation);
                if(alternatives.Count==0)diagnostics.UnsupportedUnits++;
                Candidate? best=null;double bestRank=-1;int bestChanged=0;
                foreach(var proposal in alternatives)
                {
                    cancellation.ThrowIfCancellationRequested();diagnostics.Proposals++;
                    Candidate candidate;
                    try{candidate=WordProposalCompiler.Compile(context,proposal);}
                    catch(ArgumentException ex){diagnostics.Attempts.Add(new BackendAttempt{Text=proposal.Text,Operations=proposal.Operations,Evaluation=new BackendEvaluation{Evidence=new List<BackendEvidence>{new BackendEvidence{Stage="word-mapping",Problems=new List<string>{ex.Message}}}}});continue;}
                    progress?.Report("Anlam bütünlüğü ve yeni dil bilgisi hataları denetleniyor…");
                    var rewritten=await analysis.AnalyzeAsync(proposal.Text,language,cancellation).ConfigureAwait(false);
                    if(language=="en"&&grammar is IEnglishMorphologyBackend targetEnglish)await targetEnglish.RefineVerbLemmasAsync(rewritten,cancellation).ConfigureAwait(false);
                    var issues=await grammar.CheckAsync(rewritten,cancellation).ConfigureAwait(false);
                    context.SemanticEvidence=null;
                    if(semantic!=null)
                    {
                        var review=new BackendEvidence{Stage="bidirectional-local-nli",Passed=true};
                        if(parsed.Sentences.Count!=rewritten.Sentences.Count){review.Passed=false;review.Problems.Add("Anlam kontrolünde cümle sınırları değişti.");}
                        else foreach(var op in proposal.Operations)
                        {
                            var a=parsed.Sentences[op.SentenceIndex];var b=rewritten.Sentences[op.SentenceIndex];
                            var part=await semantic.CompareAsync(parsed.Text.Substring(a.Start,a.Length),rewritten.Text.Substring(b.Start,b.Length),language,cancellation).ConfigureAwait(false);
                            review.Passed&=part.Passed;review.Problems.AddRange(part.Problems);review.Notes.AddRange(part.Notes);
                        }
                        context.SemanticEvidence=review;
                    }
                    var result=evaluation.Evaluate(context,rewritten,proposal,issues,meaning.Compare(context,rewritten,proposal));
                    diagnostics.Attempts.Add(new BackendAttempt{Text=proposal.Text,Operations=proposal.Operations,Evaluation=result});
                    if(!result.Accepted||candidate.Edits.Count==0)continue;
                    diagnostics.Accepted++;
                    double rank=result.ReconstructedSentences+result.Diversity;
                    if(rank<=bestRank)continue;
                    candidate.Score.StructuralDifference=result.Diversity;candidate.ReviewNote="Cümle kuruluşu, anlam sinyalleri ve dil bilgisi ayrı ayrı denetlendi. Öneriyi okuyarak onaylayın.";
                    best=candidate;bestRank=rank;bestChanged=result.ReconstructedSentences;
                }
                if(supplemental!=null&&language=="tr")
                {
                    progress?.Report("İkinci üreticinin cümle kuruluşları bağımsız olarak denetleniyor…");
                    foreach(var candidate in await supplemental.GenerateAsync(context,settings,cancellation,enrichment).ConfigureAwait(false))
                    {
                        cancellation.ThrowIfCancellationRequested();diagnostics.Proposals++;
                        var evidence=new BackendEvidence{Stage="supplemental-word-mapping"};
                        if(candidate.Edits.Count==0||candidate.Text==text
                            ||!CandidateIntegrity.IsApplicable(text,candidate,e=>plan.Blocks.Any(b=>b.Editable&&e.Start>=b.Start&&e.Start+e.Length<=b.Start+b.Text.Length)))
                            evidence.Problems.Add("Ek kuruluş özgün düzenlenebilir Word alanlarına eşlenemedi.");
                        evidence.Problems.AddRange(plan.CheckFixedTextMultiplicity(candidate.Text));
                        if(evidence.Problems.Count>0){diagnostics.Attempts.Add(new BackendAttempt{Text=candidate.Text,Evaluation=new BackendEvaluation{Evidence=new List<BackendEvidence>{evidence}}});continue;}
                        var target=await analysis.AnalyzeAsync(candidate.Text,language,cancellation).ConfigureAwait(false);
                        if(target.Text!=candidate.Text){diagnostics.Attempts.Add(new BackendAttempt{Text=candidate.Text,Evaluation=new BackendEvaluation{Evidence=new List<BackendEvidence>{new BackendEvidence{Stage="supplemental-reanalysis",Problems=new List<string>{"Çözümleme önerinin gerçek metniyle eşleşmedi."}}}}});continue;}
                        var changed=new List<int>();
                        if(parsed.Sentences.Count==target.Sentences.Count)
                            for(int i=0;i<parsed.Sentences.Count;i++)
                                if(ClauseMeaningIntegrity.Part(parsed,i).Text!=ClauseMeaningIntegrity.Part(target,i).Text)changed.Add(i);
                        context.SemanticEvidence=new BackendEvidence{Stage="bidirectional-local-nli",Passed=semantic!=null&&parsed.Sentences.Count==target.Sentences.Count&&changed.Count>0};
                        if(!context.SemanticEvidence.Passed)context.SemanticEvidence.Problems.Add("Ek kuruluşun cümleleri veya bağımsız anlam denetleyicisi hazır değil.");
                        else foreach(int i in changed)
                        {
                            var part=await semantic!.CompareAsync(ClauseMeaningIntegrity.Part(parsed,i).Text,ClauseMeaningIntegrity.Part(target,i).Text,language,cancellation).ConfigureAwait(false);
                            context.SemanticEvidence.Passed&=part.Passed;context.SemanticEvidence.Problems.AddRange(part.Problems);context.SemanticEvidence.Notes.AddRange(part.Notes);
                        }
                        var trace=new ConstructionProposal{Text=candidate.Text,Operations=changed.Select(i=>new ConstructionOperation{SentenceIndex=i,Kind="independently-reviewed-clause"}).ToList()};
                        var review=evaluation.Evaluate(context,target,trace,await grammar.CheckAsync(target,cancellation).ConfigureAwait(false),ClauseMeaningIntegrity.Compare(context,target,changed));
                        if(!StructuralDiversity.HasReconstruction(text,candidate.Text)||!StructuralDiversity.HasSyntaxChange(text,candidate.Text,parsed.Morphology,target.Morphology))
                        {review.Accepted=false;review.Evidence.Add(new BackendEvidence{Stage="supplemental-syntax",Problems=new List<string>{"Ek üretici cümle kuruluşunu yeterince değiştirmedi."}});}
                        diagnostics.Attempts.Add(new BackendAttempt{Text=candidate.Text,Operations=trace.Operations,Evaluation=review});
                        if(!review.Accepted)continue;
                        diagnostics.Accepted++;double rank=review.ReconstructedSentences+review.Diversity;
                        if(rank<=bestRank)continue;
                        candidate.Score.StructuralDifference=review.Diversity;best=candidate;bestRank=rank;bestChanged=review.ReconstructedSentences;
                    }
                }
                if(best!=null)
                {
                    foreach(var edit in best.Edits)edit.Start+=paragraph.Start;
                    accepted.Add(best.Edits);reconstructed+=bestChanged;
                }
            }
            cancellation.ThrowIfCancellationRequested();var edits=accepted.SelectMany(e=>e).ToList();if(edits.Count==0)return Array.Empty<Candidate>();
            var output=new Candidate{Text=EditApplication.Apply(source,edits),Edits=edits,Enrichment=enrichment.Summary,SentenceCount=sentenceCount,RewrittenSentences=reconstructed,ReviewNote="Cümle kuruluşu, anlam sinyalleri ve dil bilgisi ayrı ayrı denetlendi. Öneriyi okuyarak onaylayın."};
            if(!CandidateIntegrity.IsApplicable(source,output,e=>allowed.Any(s=>e.Start>=s.Start&&e.Start+e.Length<=s.End)&&!fixedSpans.Any(s=>s.Intersects(e.Start,e.Length))))throw new InvalidOperationException("Öneri Word alanlarına güvenli olarak eşlenemedi.");
            return new[]{output};
        }
        private static void AddScan(EnrichmentSummary total,EnrichmentSummary part)
        {
            total.RootsTotal+=part.RootsTotal;total.RootsChecked+=part.RootsChecked;total.RootsFound+=part.RootsFound;
            total.OnlineRootsQueried+=part.OnlineRootsQueried;total.OnlineRootsFound+=part.OnlineRootsFound;
            total.RootsUnresolved+=part.RootsUnresolved;total.ProviderErrors+=part.ProviderErrors;total.LexicalEntries+=part.LexicalEntries;
            total.ProviderSkipped+=part.ProviderSkipped;
            foreach(var item in part.ProviderErrorCodes){total.ProviderErrorCodes.TryGetValue(item.Key,out int count);total.ProviderErrorCodes[item.Key]=count+item.Value;}
        }
        private static List<TextSpan> Clip(IEnumerable<TextSpan> spans,int start,int length)=>spans.Where(s=>s.Intersects(start,length)).Select(s=>new TextSpan{Start=Math.Max(start,s.Start)-start,Length=Math.Min(start+length,s.End)-Math.Max(start,s.Start),Reason=s.Reason}).ToList();
    }
}
