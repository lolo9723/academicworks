using System;
using System.Collections.Generic;
using System.Linq;
namespace AcademicParaphraser.Core
{
    [System.ComponentModel.TypeConverter(typeof(StrengthConverter))]
    public enum Strength
    {
        Light = 1, Moderate = 2, Strong = 3
    }
    public sealed class UserSettings
    {
        public Strength DefaultStrength { get; set; } = Strength.Moderate;
        public bool PreserveCitations { get; set; } = true;
        public bool PreserveNumbers { get; set; } = true;
        public bool PreserveNames { get; set; } = true;
        public bool PreserveTechnicalTerms { get; set; } = true;
        public bool PreserveLinks { get; set; } = true;
        public bool InternetEnabled { get; set; } = false;
        public bool OfflineMode { get; set; } = true;
        public bool TrackChanges
        {
            get; set;
        }
        public bool CacheDictionary { get; set; } = true;
        public int Alternatives { get; set; } = 3;
        public double MinimumConfidence { get; set; } = .92;
        public string Domain { get; set; } = "genel";
        public List<string> CustomProtectionPatterns { get; set; } = new List<string>();
    }
    public sealed class TextSpan
    {
        public int Start
        {
            get; set;
        }
        public int Length
        {
            get; set;
        }
        public int End => Start + Length;
        public string Reason { get; set; } = "";
        public bool Intersects(int start, int length) => start < End && start + length > Start;
    }
    public sealed class TextEdit
    {
        public int Start
        {
            get; set;
        }
        public int Length
        {
            get; set;
        }
        public string Original { get; set; } = ""; public string Replacement { get; set; } = "";
        public string RuleId { get; set; } = ""; public string Family { get; set; } = ""; public double Confidence
        {
            get; set;
        }
    }
    public sealed class CandidateScore
    {
        public double SemanticSafety
        {
            get; set;
        }
        public double GrammarConfidence
        {
            get; set;
        }
        public double StructuralDifference
        {
            get; set;
        }
        public double LexicalDifference
        {
            get; set;
        }
        public double AcademicStyle
        {
            get; set;
        }
    }
    public sealed class Candidate
    {
        public Rewriting.EnrichmentSummary? Enrichment { get; set; }
        public string Text { get; set; } = ""; public List<TextEdit> Edits { get; set; } = new List<TextEdit>(); public CandidateScore Score { get; set; } = new CandidateScore();
    }
    public sealed class RuleDefinition
    {
        public string Id { get; set; } = ""; public string Pattern { get; set; } = ""; public string Target { get; set; } = "";
        public string Family { get; set; } = ""; public string PosConstraint { get; set; } = "";
        public string ContextPattern { get; set; } = "";
        public string RequiredText { get; set; } = ""; public List<string> RequiredMorphemes { get; set; } = new List<string>();
        public List<string> ForbiddenMorphemes { get; set; } = new List<string>();
        public Strength Strength { get; set; } = Strength.Light; public double Confidence { get; set; } = .95;
        public string Domain { get; set; } = "genel"; public bool UserDefined
        {
            get; set;
        }
        public long UsageCount
        {
            get; set;
        }
    }
    public sealed class LexiconEntry
    {
        public string Lemma { get; set; } = ""; public string Pos { get; set; } = "Noun";
        public List<string> Synonyms { get; set; } = new List<string>(); public List<string> NearSynonyms { get; set; } = new List<string>();
        public List<string> ForbiddenReplacements { get; set; } = new List<string>(); public List<string> AcademicAlternatives { get; set; } = new List<string>();
        public string Domain { get; set; } = "genel"; public double Confidence { get; set; } = .95;
        public List<string> Examples { get; set; } = new List<string>(); public bool Technical
        {
            get; set;
        }
        public bool UserDefined
        {
            get; set;
        }
    }
    public sealed class MorphToken
    {
        public int Start
        {
            get; set;
        }
        public int Length
        {
            get; set;
        }
        public string Surface { get; set; } = ""; public string Lemma { get; set; } = "";
        public string Pos { get; set; } = ""; public List<string> Morphemes { get; set; } = new List<string>(); public bool Proper
        {
            get; set;
        }
    }
    public interface IRuleCatalog
    {
        IReadOnlyList<RuleDefinition> GetRules(); IReadOnlyList<LexiconEntry> GetLexicon(); IReadOnlyList<string> GetLockedTerms();
    }
    public interface ITurkishNlp : IDisposable
    {
        System.Threading.Tasks.Task<IReadOnlyList<MorphToken>> AnalyzeAsync(string text, System.Threading.CancellationToken cancellation);
        System.Threading.Tasks.Task<string?> InflectAsync(string source, string targetLemma, string pos, System.Threading.CancellationToken cancellation);
        string Status
        {
            get;
        }
    }
    public static class EditApplication
    {
        public static string Apply(string original, IEnumerable<TextEdit> edits)
        {
            string text = original;
            int boundary = original.Length;
            foreach (var e in edits.OrderByDescending(x => x.Start))
            {
                if (e.Start < 0 || e.Length <= 0 || e.Start + e.Length > boundary || original.Substring(e.Start, e.Length) != e.Original)
                    throw new InvalidOperationException("Dönüşüm aralıkları geçersiz.");
                text = text.Remove(e.Start, e.Length).Insert(e.Start, e.Replacement);
                boundary = e.Start;
            }
            return text;
        }
    }
}
