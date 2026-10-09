using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AcademicParaphraser.Core.Rewriting;
namespace AcademicParaphraser.Core.Backends
{
    public sealed class DerivationMeaningBackend : IMeaningBackend
    {
        public BackendEvidence Compare(BackendContext context,LinguisticAnalysis target,ConstructionProposal proposal)
        {
            var evidence=new BackendEvidence{Stage="meaning-and-derivation"};
            string? replay=ConstructionRenderer.Replay(context.Source,proposal.Operations);
            if(replay==null||!string.Equals(replay,proposal.Text,StringComparison.Ordinal)||target.Text!=proposal.Text)
                evidence.Problems.Add("Kuruluş işlemleri kaynak ve sonuçla bire bir eşleşmiyor.");
            evidence.Problems.AddRange(context.Plan.CheckFixedTextMultiplicity(target.Text));
            var facts=CompareFacts(context.Source,target,context.Lexicon);
            // Only a precisely replayed Turkish, case-marked constituent movement may
            // use the independent morphology + NLI route. Free text, unmarked objects
            // and English voice conversions still require agreement of the fact graph.
            bool markedMovement=replay==proposal.Text&&target.Text==proposal.Text
                &&context.SemanticEvidence?.Passed==true
                &&TurkishMovementIntegrity.Check(context.Source,target,proposal).Passed;
            if(!facts.Passed&&!markedMovement)evidence.Problems.AddRange(facts.Problems);
            else if(!facts.Passed)evidence.Notes.AddRange(facts.Problems.Select(p=>"Çözümleyici uyuşmazlığı (ekli öbek/yerel NLI yolu): "+p));
            if(context.SemanticEvidence!=null&&!context.SemanticEvidence.Passed)evidence.Problems.AddRange(context.SemanticEvidence.Problems);
            evidence.Notes.Add("Kayıtlı kuruluş işlemi yeniden uygulandı; Word sabit alanları, miktar, olumsuzluk, zaman ve roller karşılaştırıldı.");
            evidence.Notes.Add("Bağımlılık ağacı ve kuruluş izi genel anlam eşdeğerliğinin matematiksel kanıtı değildir.");
            evidence.Passed=evidence.Problems.Count==0;return evidence;
        }
        public BackendEvidence CompareFacts(LinguisticAnalysis source,LinguisticAnalysis target,IReadOnlyList<LexiconEntry> lexicon)
        {
            var evidence=new BackendEvidence{Stage="independent-fact-graph"};
            evidence.Problems.AddRange(FactGuard.Check(source.Text,target.Text));
            evidence.Problems.AddRange(FactGuard.CheckPredicateFeatures(source.Morphology,target.Morphology));
            evidence.Problems.AddRange(FactGuard.CheckObjects(source.Text,target.Text,source.Morphology,target.Morphology,lexicon));
            evidence.Problems.AddRange(FactGuard.CheckEmbeddedFutureSubjects(source.Text,target.Text,source.Morphology,target.Morphology));
            if(source.Sentences.Count!=target.Sentences.Count)evidence.Problems.Add("Cümle sınırları değişti.");
            else for(int i=0;i<source.Sentences.Count;i++)
            {
                var a=source.Sentences[i];var b=target.Sentences[i];
                if(source.Language=="en")
                {
                    foreach(string cue in new[]{"no","not","never","may","might","must","should","could","can","will","would"})
                    {
                        int before=a.Words.Count(w=>w.Form.Equals(cue,StringComparison.OrdinalIgnoreCase));
                        int after=b.Words.Count(w=>w.Form.Equals(cue,StringComparison.OrdinalIgnoreCase));
                        if(before!=after)evidence.Problems.Add("İngilizce olumsuzluk/kip ifadesi değişti: "+cue);
                    }
                }
                bool sourcePassive=a.Root!=null&&(a.Root.Features.TryGetValue("Voice",out var sourceVoice)&&sourceVoice=="Pass"||a.Words.Any(w=>w.Relation=="aux:pass"));
                bool passive=b.Root!=null&&(b.Root.Features.TryGetValue("Voice",out var voice)&&voice=="Pass"||b.Words.Any(w=>w.Relation=="aux:pass"));
                if(a.Root==null||b.Root==null){evidence.Problems.Add("Yüklem ağacı eksik.");continue;}
                // Independent re-parsing is fallible; explicit disagreement is retained and
                // fails the proposal rather than being called an equivalence proof.
                if(Normalize(a.Root.Lemma,source.Language)!=Normalize(b.Root.Lemma,source.Language))
                    evidence.Problems.Add("Yeniden çözümlemede ana yüklem değişti.");
                if(a.Root.Features.TryGetValue("Tense",out var oldTense)&&b.Root.Features.TryGetValue("Tense",out var newTense)&&oldTense!=newTense)
                    evidence.Problems.Add("Yüklemin zamanı değişti.");
                CompareRole("eyleyen",Actor(a,sourcePassive),Actor(b,passive),source.Language,evidence);
                CompareRole("nesne/önerme",Patient(a,sourcePassive,source.Language),Patient(b,passive,source.Language),source.Language,evidence);
                foreach(var clause in a.Words.Where(w=>w.Relation=="advcl"||w.Relation=="ccomp"||w.Relation=="xcomp"))
                {
                    string form=Normalize(clause.Form,source.Language);
                    var corresponding=b.Words.Where(w=>Normalize(w.Form,source.Language)==form).ToList();
                    if(corresponding.Count!=1)continue;
                    var oldParent=a.Words.SingleOrDefault(w=>w.Id==clause.Head);
                    var newParent=b.Words.SingleOrDefault(w=>w.Id==corresponding[0].Head);
                    if(oldParent!=null&&newParent!=null&&Normalize(oldParent.Lemma,source.Language)!=Normalize(newParent.Lemma,source.Language))
                        evidence.Problems.Add("Yan cümle başka bir yükleme bağlandı; kapsam belirsiz: "+clause.Form);
                }
            }
            var sourceVerbs=source.Sentences.SelectMany(s=>s.Words).Where(w=>w.Pos=="VERB").Select(w=>Normalize(w.Lemma,source.Language)).OrderBy(w=>w,StringComparer.Ordinal);
            var targetVerbs=target.Sentences.SelectMany(s=>s.Words).Where(w=>w.Pos=="VERB").Select(w=>Normalize(w.Lemma,source.Language)).OrderBy(w=>w,StringComparer.Ordinal);
            if(!sourceVerbs.SequenceEqual(targetVerbs))evidence.Problems.Add("Bir eylem/yan eylem eklendi, eksildi veya kökü değişti.");
            evidence.Passed=evidence.Problems.Count==0;return evidence;
        }
        private static List<DependencyWord> Actor(DependencySentence s,bool passive)
        {
            var root=s.Root!;return s.Words.Where(w=>w.Head==root.Id&&(passive?w.Relation=="obl:agent"||w.Relation=="nmod:agent":w.Relation=="nsubj"||w.Relation=="csubj")).SelectMany(w=>s.Subtree(w.Id)).Where(w=>w.Pos!="PUNCT"&&!(passive&&w.Relation=="case"&&w.Form.Equals("by",StringComparison.OrdinalIgnoreCase))).ToList();
        }
        private static List<DependencyWord> Patient(DependencySentence s,bool passive,string language)
        {
            var root=s.Root!;return s.Words.Where(w=>w.Head==root.Id&&(passive?w.Relation.StartsWith("nsubj",StringComparison.Ordinal)||w.Relation.StartsWith("csubj",StringComparison.Ordinal):w.Relation=="obj"||w.Relation=="ccomp")).SelectMany(w=>language=="en"?EnglishNominalConstituents.Patient(s,w):s.Subtree(w.Id)).Where(w=>w.Pos!="PUNCT").ToList();
        }
        private static void CompareRole(string name,List<DependencyWord> before,List<DependencyWord> after,string language,BackendEvidence evidence)
        {
            var a=before.Select(w=>Normalize(w.Form,language)).OrderBy(x=>x,StringComparer.Ordinal);
            var b=after.Select(w=>Normalize(w.Form,language)).OrderBy(x=>x,StringComparer.Ordinal);
            if(!a.SequenceEqual(b))evidence.Problems.Add("Yeniden çözümlemede "+name+" ilişkisi değişti veya kayboldu.");
        }
        private static string Normalize(string text,string language)=>text.ToLower(language=="tr"?CultureInfo.GetCultureInfo("tr-TR"):CultureInfo.InvariantCulture);
    }
    public sealed class IndependentFinalEvaluationBackend : IFinalEvaluationBackend
    {
        public BackendEvaluation Evaluate(BackendContext context,LinguisticAnalysis target,ConstructionProposal proposal,IReadOnlyList<GrammarIssue> grammar,BackendEvidence meaning)
        {
            var result=new BackendEvaluation();result.Evidence.Add(meaning);
            if(context.SemanticEvidence!=null)result.Evidence.Add(context.SemanticEvidence);
            var grammarEvidence=new BackendEvidence{Stage="grammar"};
            var baseline=context.OriginalGrammar.GroupBy(IssueKey).ToDictionary(g=>g.Key,g=>g.Count(),StringComparer.Ordinal);
            foreach(var group in grammar.GroupBy(IssueKey))
                if(!baseline.TryGetValue(group.Key,out var count)||group.Count()>count)
                    grammarEvidence.Problems.Add(group.First().Message+" ["+group.First().Code+"]");
            grammarEvidence.Passed=grammarEvidence.Problems.Count==0;result.Evidence.Add(grammarEvidence);
            var structure=new BackendEvidence{Stage="final-structure-and-language"};
            var a=Bigrams(context.Source.Text);var b=Bigrams(target.Text);
            result.Diversity=a.Count==0||b.Count==0?0:1.0-(double)a.Intersect(b).Count()/a.Union(b).Count();
            if(proposal.Operations.Count==0||result.Diversity<.12)structure.Problems.Add("Cümle kuruluşunda yeterli değişiklik yok.");
            if(LanguageProfile.HasClearLanguageChange(target.Text,context.Source.Language=="en"))structure.Problems.Add("Kaynağın dili değişti.");
            result.ReconstructedSentences=proposal.Operations.Count;
            structure.Passed=structure.Problems.Count==0;result.Evidence.Add(structure);
            result.Accepted=result.Evidence.All(e=>e.Passed);return result;
        }
        private static string IssueKey(GrammarIssue issue)=>issue.Code+"/"+issue.Kind;
        private static HashSet<string> Bigrams(string text)
        {
            var words=Regex.Matches(text.ToLowerInvariant(),@"[\p{L}\p{M}\d]+").Cast<Match>().Select(m=>m.Value).ToArray();
            return new HashSet<string>(Enumerable.Range(0,Math.Max(0,words.Length-1)).Select(i=>words[i]+" "+words[i+1]),StringComparer.Ordinal);
        }
    }
}
