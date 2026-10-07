using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AcademicParaphraser.Core.RuleEngine;

namespace AcademicParaphraser.Core.Rewriting
{
    // Morphological evidence and literal markers, not a dependency parse or a fact proof.
    public sealed class SentenceFrame
    {
        public string Source { get; set; } = "";
        public string[] Predicates { get; set; } = Array.Empty<string>();
        public string[] Signals { get; set; } = Array.Empty<string>();
        public string[] FixedRelations { get; set; } = Array.Empty<string>();
        public static IReadOnlyList<SentenceFrame> Analyze(string text, IReadOnlyList<MorphToken> morphology)
        {
            var result=new List<SentenceFrame>();
            foreach(var sentence in SentenceSegmentation.Find(text,Array.Empty<TextSpan>()))
            {
                var words=morphology.Where(t=>t.Start>=sentence.Start&&t.Start<sentence.End).ToList();
                string source=text.Substring(sentence.Start,sentence.Length);
                result.Add(new SentenceFrame{Source=source,
                    Predicates=words.Where(t=>t.Pos=="Verb"&&!t.Morphemes.Contains("Imp")).Select(t=>t.Surface+" ["+t.Lemma+"]").Distinct().ToArray(),
                    Signals=words.Where(t=>!t.Morphemes.Contains("Imp")).SelectMany(t=>t.Morphemes.Where(m=>new[]{"Neg","Unable","Cond","Neces","Fut","Past","Narr"}.Contains(m))).Distinct().ToArray(),
                    FixedRelations=Regex.Matches(source,@"\b(?:koşuluyla|ancak|çünkü|rağmen|yalnızca|sadece|göstermemektedir|kanıtlamaz|anlamına gelmez|if|because|although|association|observational|did not)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant).Cast<Match>().Select(m=>m.Value).ToArray()});
            }
            return result;
        }
    }
    public static class StructuralDiversity
    {
        // A surface rejection of near-identical proposals. This ratio is NOT an accuracy score.
        public static bool HasReconstruction(string source,string target)
        {
            var a=Bigrams(source);var b=Bigrams(target);
            if(a.Count==0||b.Count==0)return !string.Equals(source,target,StringComparison.OrdinalIgnoreCase);
            double changed=1.0-(double)a.Intersect(b).Count()/a.Union(b).Count();
            return changed>=.22;
        }
        public static bool HasSyntaxChange(string source,string target,IReadOnlyList<MorphToken> before,IReadOnlyList<MorphToken> after)
        {
            string passive=@"\b(?:is|are|was|were|been|be)\s+(?:[a-z]+ed|found|shown|seen|known|given|taken|made|written|built)\b";
            if(Regex.Matches(source,passive,RegexOptions.IgnoreCase).Count!=Regex.Matches(target,passive,RegexOptions.IgnoreCase).Count)return true;
            string[] features={"Pass","Inf1","Inf2","Inf3","PastPart","FutPart","PresPart","While","When","AfterDoing","ByDoing"};
            if(features.Any(f=>before.Count(t=>t.Morphemes.Contains(f))!=after.Count(t=>t.Morphemes.Contains(f))))return true;
            var a=Regex.Matches(source.ToLowerInvariant(),@"[\p{L}\p{M}]+").Cast<Match>().Select(m=>m.Value).ToArray();
            var b=Regex.Matches(target.ToLowerInvariant(),@"[\p{L}\p{M}]+").Cast<Match>().Select(m=>m.Value).ToArray();
            var common=a.Where(w=>w.Length>=3&&a.Count(x=>x==w)==1&&b.Count(x=>x==w)==1).ToArray();
            if(common.Length<3)return false;
            var positions=common.Select(w=>Array.IndexOf(b,w)).ToArray();
            for(int i=1;i<positions.Length;i++)if(positions[i]<positions[i-1])return true;
            return false;
        }
        private static HashSet<string> Bigrams(string text)
        {
            var words=Regex.Matches(text.ToLowerInvariant(),@"[\p{L}\p{M}\d]+").Cast<Match>().Select(m=>m.Value).ToArray();
            return new HashSet<string>(Enumerable.Range(0,Math.Max(0,words.Length-1)).Select(i=>words[i]+" "+words[i+1]),StringComparer.Ordinal);
        }
    }
}
