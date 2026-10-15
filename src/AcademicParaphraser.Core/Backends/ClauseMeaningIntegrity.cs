using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AcademicParaphraser.Core.Rewriting;

namespace AcademicParaphraser.Core.Backends
{
    // Supplemental deterministic candidates need independent evidence beyond
    // their originating rule. This is deliberately narrower than free rewriting.
    public static class ClauseMeaningIntegrity
    {
        private static readonly CultureInfo Turkish=CultureInfo.GetCultureInfo("tr-TR");
        private const string Owner=@"(?<owner>(?:(?:bu|mevcut|sunulan)\s+)?[\p{L}\p{M}]+)";
        private static readonly Regex Nominal=new Regex(@"\A"+Owner+@"\s+(?<goal>amacı|hedefi),?\s+(?<claim>[^.!?;]+?)(?:maktır|mektir)\.?\z",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
        private static readonly Regex Verbal=new Regex(@"\A"+Owner+@",?\s+(?<claim>[^.!?;]+?)(?:mayı|meyi)\s+(?<goal>amaçlamaktadır|hedeflemektedir)\.?\z",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));

        public static BackendEvidence Compare(BackendContext context,LinguisticAnalysis target,IReadOnlyList<int> changedSentences)
        {
            var result=new BackendEvidence{Stage="independent-clause-meaning"};
            result.Problems.AddRange(context.Plan.CheckFixedTextMultiplicity(target.Text));
            result.Problems.AddRange(FactGuard.Check(context.Source.Text,target.Text));
            result.Problems.AddRange(FactGuard.CheckPostpositionBindings(context.Source.Morphology,target.Morphology));
            result.Problems.AddRange(FactGuard.CheckPredicateFeatures(context.Source.Morphology,target.Morphology));
            result.Problems.AddRange(FactGuard.CheckObjects(context.Source.Text,target.Text,context.Source.Morphology,target.Morphology,context.Lexicon));
            result.Problems.AddRange(FactGuard.CheckEmbeddedFutureSubjects(context.Source.Text,target.Text,context.Source.Morphology,target.Morphology));
            if(context.Source.Language!="tr"||target.Language!="tr"||context.Source.Sentences.Count!=target.Sentences.Count)
                result.Problems.Add("Ek kuruluş için dil veya cümle sınırları uyuşmadı.");
            else foreach(int index in changedSentences)
            {
                var sourcePart=Part(context.Source,index);var targetPart=Part(target,index);
                int Negations(LinguisticAnalysis a)=>a.Morphology.Count(t=>t.Morphemes.Contains("Neg")||t.Lemma.Equals("değil",StringComparison.OrdinalIgnoreCase)||t.Lemma.Equals("yok",StringComparison.OrdinalIgnoreCase));
                if(Negations(sourcePart)!=Negations(targetPart))result.Problems.Add("Ek kuruluşta olumsuzluk ifadesi eklendi veya eksildi.");
                if(Regex.IsMatch(sourcePart.Text,@"\b(?:yalnızca|sadece|bile|dahi)\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant)
                    &&!EquivalentPurpose(sourcePart,targetPart))
                {result.Problems.Add("Odak parçacığının kapsamı ek kuruluşta bağımsız doğrulanamadı.");continue;}
                var facts=new DerivationMeaningBackend().CompareFacts(sourcePart,targetPart,context.Lexicon);
                if(facts.Passed)continue;
                // A complete, known morphological inventory must agree for movement;
                // a nominal purpose clause has its own explicit owner/claim contract.
                bool certified=SameMorphology(sourcePart,targetPart)||EquivalentPurpose(sourcePart,targetPart);
                if(!certified)result.Problems.AddRange(facts.Problems);
                else result.Notes.AddRange(facts.Problems.Select(p=>"Çözümleyici uyuşmazlığı (bağımsız öbek/amaç çerçevesi): "+p));
            }
            if(context.SemanticEvidence?.Passed!=true)result.Problems.Add("Ek kuruluş bağımsız iki yönlü anlam kontrolünden geçmedi.");
            result.Passed=result.Problems.Count==0;
            result.Notes.Add("Ek üretici; Word sabitleri, olgu sinyalleri, roller, çekim/amaç çerçevesi ve ayrı NLI ile denetlendi. Genel anlam kanıtı değildir.");
            return result;
        }

        private static bool Known(LinguisticAnalysis part)=>part.Morphology.Count>0
            &&part.Morphology.All(t=>!t.RuntimeGuess&&t.Pos!="Unknown")
            &&part.Sentences.SelectMany(s=>s.Words).Where(w=>w.Pos!="PUNCT")
                .All(w=>part.Morphology.Any(t=>t.Start<=w.Start&&t.Start+t.Length>=w.End));

        public static bool SameMorphology(LinguisticAnalysis source,LinguisticAnalysis target)
        {
            if(!Known(source)||!Known(target))return false;
            if(Regex.IsMatch(source.Text+" "+target.Text,@"\b(?:yalnızca|sadece|bile|dahi)\b|[“”«»"";]",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant))return false;
            string Signature(MorphToken t)=>t.Surface.ToLower(Turkish)+"/"+t.Lemma.ToLower(Turkish)+"/"+t.Pos+"/"+string.Join("|",t.Morphemes);
            if(!source.Morphology.Where(t=>t.Pos!="Punctuation").Select(Signature).OrderBy(t=>t,StringComparer.Ordinal)
                .SequenceEqual(target.Morphology.Where(t=>t.Pos!="Punctuation").Select(Signature).OrderBy(t=>t,StringComparer.Ordinal)))return false;
            // Unmarked objects and changed nominal attachments cannot be certified
            // from an unordered token inventory ("yaralı adama" != "yaralı gazeteci").
            foreach(var sentence in source.Sentences)
            {
                foreach(var obj in sentence.Words.Where(w=>w.Relation=="obj"))
                    if(!source.Morphology.Any(t=>t.Start<=obj.Start&&t.Start+t.Length>=obj.End&&t.Morphemes.Contains("Acc")))return false;
                if(sentence.Words.Any(w=>new[]{"ccomp","xcomp","advcl","acl"}.Contains(w.Relation.Split(':')[0])))return false;
            }
            string[] nominalRelations={"amod","nmod","compound","case","det","nummod"};
            string[] Edges(LinguisticAnalysis a)=>a.Sentences.SelectMany(s=>s.Words
                .Where(w=>nominalRelations.Contains(w.Relation.Split(':')[0]))
                .Select(w=>w.Form.ToLower(Turkish)+"/"+w.Relation+"/"+(s.Words.SingleOrDefault(p=>p.Id==w.Head)?.Form.ToLower(Turkish)??"ROOT")))
                .OrderBy(v=>v,StringComparer.Ordinal).ToArray();
            if(!Edges(source).SequenceEqual(Edges(target)))return false;
            // Do not certify a reattachment between multiple finite predicates.
            bool Finite(MorphToken t)=>t.Pos=="Verb"&&!t.Morphemes.Any(m=>m.EndsWith("Part",StringComparison.Ordinal)||m.StartsWith("Inf",StringComparison.Ordinal)||m=="Converb");
            return source.Morphology.Count(Finite)==1&&target.Morphology.Count(Finite)==1;
        }

        public static bool EquivalentPurpose(LinguisticAnalysis source,LinguisticAnalysis target)
        {
            if(source.Morphology.Count==0||target.Morphology.Count==0||source.Sentences.Count!=1||target.Sentences.Count!=1)return false;
            var a=Nominal.Match(source.Text.Trim());var b=Verbal.Match(target.Text.Trim());
            bool reverse=false;
            if(!a.Success||!b.Success){a=Nominal.Match(target.Text.Trim());b=Verbal.Match(source.Text.Trim());reverse=true;}
            if(!a.Success||!b.Success)return false;
            var nominal=reverse?target:source;var verbal=reverse?source:target;
            // Claim text is independently matched after stripping only the licensed
            // infinitival endings. Internal word order, negation and scope stay exact.
            if(!string.Equals(a.Groups["claim"].Value,b.Groups["claim"].Value,StringComparison.OrdinalIgnoreCase))return false;
            string goal=a.Groups["goal"].Value.ToLower(Turkish)=="amacı"?"amaçlamaktadır":"hedeflemektedir";
            if(!b.Groups["goal"].Value.Equals(goal,StringComparison.OrdinalIgnoreCase))return false;
            var nominalOwner=nominal.Morphology.Where(t=>t.Start<a.Groups["owner"].Length&&t.Pos!="Punctuation").ToArray();
            var verbalOwner=verbal.Morphology.Where(t=>t.Start<b.Groups["owner"].Length&&t.Pos!="Punctuation").ToArray();
            if(nominalOwner.Length==0||nominalOwner.Length!=verbalOwner.Length||nominalOwner.Concat(verbalOwner).Any(t=>t.RuntimeGuess||t.Pos=="Unknown")||nominalOwner.Last().Pos!="Noun"
                ||!nominalOwner.Last().Morphemes.Contains("Gen")||verbalOwner.Last().Morphemes.Any(m=>new[]{"Gen","Acc","Dat","Abl","Loc","Ins","Equ"}.Contains(m)))return false;
            if(verbalOwner.Any(t=>t.Morphemes.Any(m=>new[]{"P1sg","P2sg","P1pl","P2pl"}.Contains(m))))return false;
            string OwnerSignature(MorphToken t)=>t.Lemma.ToLower(Turkish)+"/"+t.Pos+"/"+string.Join("|",t.Morphemes.Where(m=>m!="Gen"&&m!="Nom"&&!(t.AmbiguousPersonal&&new[]{"P1sg","P2sg","P1pl","P2pl"}.Contains(m))));
            if(!nominalOwner.Select(OwnerSignature).SequenceEqual(verbalOwner.Select(OwnerSignature)))return false;
            var nominalAction=nominal.Morphology.LastOrDefault(t=>t.Pos!="Punctuation");
            var verbalAction=verbal.Morphology.LastOrDefault(t=>t.Start<b.Groups["claim"].Index+b.Groups["claim"].Length&&t.Pos!="Punctuation");
            var goalVerb=verbal.Morphology.LastOrDefault(t=>t.Pos!="Punctuation");
            return nominalAction!=null&&verbalAction!=null&&goalVerb!=null&&!goalVerb.RuntimeGuess&&!nominalAction.RuntimeGuess&&!verbalAction.RuntimeGuess&&nominalAction.Lemma==verbalAction.Lemma
                &&nominalAction.Morphemes.Contains("Inf1")
                &&verbalAction.Morphemes.Any(m=>m=="Inf1"||m=="Inf2")
                &&FactGuard.Check(source.Text,target.Text).Count==0;
        }

        public static LinguisticAnalysis Part(LinguisticAnalysis analysis,int index)
        {
            var sentence=analysis.Sentences[index];var part=new LinguisticAnalysis{Text=analysis.Text.Substring(sentence.Start,sentence.Length),Language=analysis.Language,Backend=analysis.Backend};
            part.Sentences=new[]{new DependencySentence{Length=sentence.Length,Words=sentence.Words.Select(w=>new DependencyWord{Id=w.Id,Head=w.Head,Form=w.Form,Lemma=w.Lemma,Pos=w.Pos,Relation=w.Relation,Features=w.Features,Start=w.Start-sentence.Start,Length=w.Length}).ToList()}};
            part.Morphology=analysis.Morphology.Where(t=>t.Start>=sentence.Start&&t.Start<sentence.End).Select(t=>new MorphToken{Start=t.Start-sentence.Start,Length=t.Length,Surface=t.Surface,Lemma=t.Lemma,Pos=t.Pos,Proper=t.Proper,RuntimeGuess=t.RuntimeGuess,AmbiguousProper=t.AmbiguousProper,AmbiguousPersonal=t.AmbiguousPersonal,Morphemes=t.Morphemes}).ToArray();
            return part;
        }
    }
}
