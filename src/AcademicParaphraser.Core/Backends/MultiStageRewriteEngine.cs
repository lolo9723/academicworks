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
        public MultiStageRewriteEngine(IRuleCatalog catalog,ILinguisticBackend analysis,IGrammarBackend grammar,IConstructionBackend? construction=null,IMeaningBackend? meaning=null,IFinalEvaluationBackend? evaluation=null,ISemanticBackend? semantic=null)
        {
            this.catalog=catalog;this.analysis=analysis;this.grammar=grammar;
            this.construction=construction??new DependencyConstructionBackend();this.meaning=meaning??new DerivationMeaningBackend();this.evaluation=evaluation??new IndependentFinalEvaluationBackend();
            this.semantic=semantic;
        }
        public async Task<IReadOnlyList<Candidate>> GenerateAsync(string source,UserSettings settings,IEnumerable<TextSpan> writable,IEnumerable<TextSpan> external,CancellationToken cancellation,IProgress<string>? progress=null,MultiStageDiagnostics? diagnostics=null)
        {
            if(string.IsNullOrWhiteSpace(source)||source.Length>80000)throw new ArgumentException("En fazla 80.000 karakterlik bir seçim yapın.");
            diagnostics=diagnostics??new MultiStageDiagnostics();var allowed=writable.ToList();
            var fixedSpans=new ProtectionDetector().Detect(source,settings,catalog.GetLockedTerms(),catalog.GetLexicon(),external).ToList();
            fixedSpans.AddRange(FactGuard.Anchors(source));var accepted=new List<List<TextEdit>>();int reconstructed=0;int sentenceCount=0;
            foreach(Match paragraph in Regex.Matches(source,@"[^\r\n\a\v\f]+"))
            {
                cancellation.ThrowIfCancellationRequested();diagnostics.Paragraphs++;
                if(paragraph.Length>2400)continue;
                string text=paragraph.Value;string language=LanguageProfile.IsEnglish(text,settings.InputLanguage)?"en":"tr";
                progress?.Report("Cümleler ve dil bilgisi ilişkileri çözümleniyor…");
                var parsed=await analysis.AnalyzeAsync(text,language,cancellation).ConfigureAwait(false);sentenceCount+=parsed.Sentences.Count;
                if(language=="en"&&grammar is IEnglishMorphologyBackend english)await english.RefineVerbLemmasAsync(parsed,cancellation).ConfigureAwait(false);
                var protectedSpans=Clip(fixedSpans,paragraph.Index,paragraph.Length);
                if(settings.PreserveNames)
                {
                    protectedSpans.AddRange(parsed.Sentences.SelectMany(s=>s.Words).Where(w=>w.Pos=="PROPN").Select(w=>new TextSpan{Start=w.Start,Length=w.Length,Reason="bağımlılık özel isim"}));
                    protectedSpans.AddRange(parsed.Morphology.Where(w=>w.Proper&&!w.AmbiguousProper&&!w.RuntimeGuess).Select(w=>new TextSpan{Start=w.Start,Length=w.Length,Reason="morfoloji özel isim"}));
                }
                var plan=RewritePlan.Create(text,Clip(allowed,paragraph.Index,paragraph.Length),protectedSpans);
                // A tree rewrite is allowed only when its output has an exact Word mapping.
                if(!plan.Blocks.Any(b=>b.Editable))continue;
                var context=new BackendContext{Source=parsed,Plan=plan,Lexicon=catalog.GetLexicon(),OriginalGrammar=await grammar.CheckAsync(parsed,cancellation).ConfigureAwait(false)};
                progress?.Report("Cümle kuruluşu seçenekleri hazırlanıyor…");var alternatives=construction.Generate(context,settings,cancellation);
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
                if(best!=null)
                {
                    foreach(var edit in best.Edits)edit.Start+=paragraph.Index;
                    accepted.Add(best.Edits);reconstructed+=bestChanged;
                }
            }
            cancellation.ThrowIfCancellationRequested();var edits=accepted.SelectMany(e=>e).ToList();if(edits.Count==0)return Array.Empty<Candidate>();
            var output=new Candidate{Text=EditApplication.Apply(source,edits),Edits=edits,SentenceCount=sentenceCount,RewrittenSentences=reconstructed,ReviewNote="Cümle kuruluşu, anlam sinyalleri ve dil bilgisi ayrı ayrı denetlendi. Öneriyi okuyarak onaylayın."};
            if(!CandidateIntegrity.IsApplicable(source,output,e=>allowed.Any(s=>e.Start>=s.Start&&e.Start+e.Length<=s.End)&&!fixedSpans.Any(s=>s.Intersects(e.Start,e.Length))))throw new InvalidOperationException("Öneri Word alanlarına güvenli olarak eşlenemedi.");
            return new[]{output};
        }
        private static List<TextSpan> Clip(IEnumerable<TextSpan> spans,int start,int length)=>spans.Where(s=>s.Intersects(start,length)).Select(s=>new TextSpan{Start=Math.Max(start,s.Start)-start,Length=Math.Min(start+length,s.End)-Math.Max(start,s.Start),Reason=s.Reason}).ToList();
    }
}
