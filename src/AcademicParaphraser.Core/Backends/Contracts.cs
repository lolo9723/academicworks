using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core.Rewriting;
namespace AcademicParaphraser.Core.Backends
{
    public sealed class DependencyWord
    {
        public int Id { get; set; } public int Start { get; set; } public int Length { get; set; }
        public int End => Start + Length;
        public int Head { get; set; } public string Form { get; set; } = ""; public string Lemma { get; set; } = "";
        public string Pos { get; set; } = ""; public string Relation { get; set; } = "";
        public Dictionary<string,string> Features { get; set; } = new Dictionary<string,string>(StringComparer.Ordinal);
    }
    public sealed class DependencySentence
    {
        public int Start { get; set; } public int Length { get; set; } public int End => Start+Length;
        public List<DependencyWord> Words { get; set; } = new List<DependencyWord>();
        public DependencyWord? Root => Words.SingleOrDefault(w=>w.Head==0);
        public IReadOnlyList<DependencyWord> Subtree(int root)
        {
            var ids=new HashSet<int>{root};bool changed;
            do{changed=false;foreach(var word in Words)if(ids.Contains(word.Head)&&ids.Add(word.Id))changed=true;}while(changed);
            return Words.Where(w=>ids.Contains(w.Id)).OrderBy(w=>w.Start).ToArray();
        }
    }
    public sealed class LinguisticAnalysis
    {
        public string Text { get; set; } = ""; public string Language { get; set; } = "tr";
        public string Backend { get; set; } = "";
        public IReadOnlyList<DependencySentence> Sentences { get; set; } = Array.Empty<DependencySentence>();
        public IReadOnlyList<MorphToken> Morphology { get; set; } = Array.Empty<MorphToken>();
    }
    public sealed class GrammarIssue
    {
        public string Code { get; set; } = ""; public string Message { get; set; } = "";
        public string Kind { get; set; } = ""; public int Start { get; set; } public int Length { get; set; }
    }
    public sealed class BackendEvidence
    {
        public string Stage { get; set; } = ""; public bool Passed { get; set; }
        public List<string> Problems { get; set; } = new List<string>();
        public List<string> Notes { get; set; } = new List<string>();
    }
    public sealed class ConstructionOperation
    {
        public int SentenceIndex { get; set; } public int ArgumentId { get; set; }
        public string Kind { get; set; } = "";
    }
    public sealed class ConstructionProposal
    {
        public string Text { get; set; } = "";
        public List<ConstructionOperation> Operations { get; set; } = new List<ConstructionOperation>();
    }
    public sealed class BackendContext
    {
        public LinguisticAnalysis Source { get; set; } = new LinguisticAnalysis();
        public RewritePlan Plan { get; set; } = null!;
        public IReadOnlyList<GrammarIssue> OriginalGrammar { get; set; } = Array.Empty<GrammarIssue>();
        public IReadOnlyList<LexiconEntry> Lexicon { get; set; } = Array.Empty<LexiconEntry>();
        public BackendEvidence? SemanticEvidence { get; set; }
    }
    public sealed class BackendEvaluation
    {
        public bool Accepted { get; set; } public int ReconstructedSentences { get; set; }
        public double Diversity { get; set; }
        public List<BackendEvidence> Evidence { get; set; } = new List<BackendEvidence>();
    }
    public interface ILinguisticBackend : IDisposable
    {
        Task<LinguisticAnalysis> AnalyzeAsync(string text,string language,CancellationToken cancellation);
    }
    public interface IGrammarBackend : IDisposable
    {
        Task<IReadOnlyList<GrammarIssue>> CheckAsync(LinguisticAnalysis analysis,CancellationToken cancellation);
    }
    public interface ISemanticBackend : IDisposable
    {
        Task<BackendEvidence> CompareAsync(string source,string target,string language,CancellationToken cancellation);
    }
    public interface IEnglishMorphologyBackend
    {
        Task RefineVerbLemmasAsync(LinguisticAnalysis analysis,CancellationToken cancellation);
    }
    public interface IConstructionBackend
    {
        IReadOnlyList<ConstructionProposal> Generate(BackendContext context,UserSettings settings,CancellationToken cancellation);
    }
    public interface IMeaningBackend
    {
        BackendEvidence Compare(BackendContext context,LinguisticAnalysis target,ConstructionProposal proposal);
    }
    public interface IFinalEvaluationBackend
    {
        BackendEvaluation Evaluate(BackendContext context,LinguisticAnalysis target,ConstructionProposal proposal,
            IReadOnlyList<GrammarIssue> grammar,BackendEvidence meaning);
    }
    public sealed class MultiStageDiagnostics
    {
        public List<BackendAttempt> Attempts { get; set; } = new List<BackendAttempt>();
        public int Paragraphs { get; set; } public int Proposals { get; set; } public int Accepted { get; set; }
        public int WorkUnits { get; set; }
        public int OversizedUnits { get; set; }
        public int ProtectedUnits { get; set; }
        public int UnsupportedUnits { get; set; }
        public int DictionaryScans { get; set; }
        public int DictionaryUnsupportedUnits { get; set; }
        public EnrichmentSummary Enrichment { get; set; } = new EnrichmentSummary();
        public string Describe()
        {
            var lines = new List<string> { "İncelenen bölüm: " + WorkUnits + " · üretilen aday: " + Proposals + " · denetimlerden geçen: " + Accepted };
            if (OversizedUnits > 0) lines.Add(OversizedUnits + " bölüm, bölünemeyen cümlesi 2.400 karakteri aştığı için işlenemedi.");
            if (ProtectedUnits > 0) lines.Add(ProtectedUnits + " bölümde düzenlenebilir Word alanı bulunamadı.");
            if (UnsupportedUnits > 0) lines.Add(UnsupportedUnits + " bölümün cümle yapısı mevcut üretim motorunca desteklenmiyor.");
            foreach (var reason in Attempts.SelectMany(a => a.Evaluation.Evidence).Where(e => !e.Passed)
                .SelectMany(e => e.Problems).Distinct().Take(3)) lines.Add(reason);
            if (DictionaryScans > 0) lines.Add("Sözlük: " + Enrichment.RootsChecked + "/" + Enrichment.RootsTotal
                + " kök incelendi; " + Enrichment.RootsFound + " kökte bilgi bulundu. İnternette aranan: "
                + Enrichment.OnlineRootsQueried + "; kaynak hatası: " + Enrichment.ProviderErrors + ".");
            else lines.Add("Bu işlemde sözlük taraması yapılmadı.");
            if (DictionaryUnsupportedUnits > 0) lines.Add("İngilizce bölümlerde geniş sözlük taraması henüz desteklenmiyor.");
            return string.Join(Environment.NewLine, lines);
        }
    }
    public sealed class BackendAttempt
    {
        public string Text { get; set; } = "";
        public List<ConstructionOperation> Operations { get; set; } = new List<ConstructionOperation>();
        public BackendEvaluation Evaluation { get; set; } = new BackendEvaluation();
    }
}
