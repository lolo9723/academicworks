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
    }
    public sealed class BackendAttempt
    {
        public string Text { get; set; } = "";
        public List<ConstructionOperation> Operations { get; set; } = new List<ConstructionOperation>();
        public BackendEvaluation Evaluation { get; set; } = new BackendEvaluation();
    }
}
