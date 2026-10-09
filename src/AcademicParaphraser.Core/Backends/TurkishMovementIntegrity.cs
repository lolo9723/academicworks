using System;
using System.Globalization;
using System.Linq;
using AcademicParaphraser.Core.Rewriting;
namespace AcademicParaphraser.Core.Backends
{
    // This is a narrow morphological check for recorded constituent movements,
    // not a general semantic-equivalence checker or permission for free generation.
    public static class TurkishMovementIntegrity
    {
        private static readonly CultureInfo Turkish=CultureInfo.GetCultureInfo("tr-TR");
        public static BackendEvidence Check(LinguisticAnalysis source,LinguisticAnalysis target,ConstructionProposal proposal)
        {
            var result=new BackendEvidence{Stage="case-marked-movement"};
            if(source.Language!="tr"||target.Language!="tr"||source.Morphology.Count==0||target.Morphology.Count==0
                ||source.Sentences.Count!=target.Sentences.Count||proposal.Operations.Count==0
                ||ConstructionRenderer.Replay(source,proposal.Operations)!=target.Text)
            {result.Problems.Add("Ekli öbek doğrulaması bu dönüşüme uygulanamaz.");return result;}
            result.Problems.AddRange(FactGuard.Check(source.Text,target.Text));
            result.Problems.AddRange(FactGuard.CheckPredicateFeatures(source.Morphology,target.Morphology));
            foreach(var op in proposal.Operations)
            {
                if(op.Kind!="tr-front-constituent"||op.SentenceIndex<0||op.SentenceIndex>=source.Sentences.Count)
                {result.Problems.Add("Yalnızca kayıtlı Türkçe öbek taşıması desteklenir.");continue;}
                var a=source.Sentences[op.SentenceIndex];var b=target.Sentences[op.SentenceIndex];
                var head=a.Words.SingleOrDefault(w=>w.Id==op.ArgumentId);
                var token=head==null?null:source.Morphology.SingleOrDefault(t=>t.Start==head.Start&&t.Length>=head.Length);
                if(token==null||token.RuntimeGuess||token.Proper||!token.Morphemes.Any(m=>new[]{"Acc","Dat","Loc","Abl","Ins"}.Contains(m)))
                    result.Problems.Add("Taşınan öbeğin görevi bilinen hâl ekiyle doğrulanamadı.");
                var before=source.Morphology.Where(t=>t.Start>=a.Start&&t.Start<a.End&&t.Pos!="Punctuation").ToList();
                var after=target.Morphology.Where(t=>t.Start>=b.Start&&t.Start<b.End&&t.Pos!="Punctuation").ToList();
                bool Covered(DependencyWord w,System.Collections.Generic.List<MorphToken> tokens)=>tokens.Any(t=>t.Start<=w.Start&&t.Start+t.Length>=w.End);
                if(a.Words.Any(w=>w.Pos!="PUNCT"&&!Covered(w,before))||b.Words.Any(w=>w.Pos!="PUNCT"&&!Covered(w,after)))
                    result.Problems.Add("Morfolojik çözümlemede sözcük kapsamı eksik.");
                string Signature(MorphToken t)=>t.Surface.ToLower(Turkish)+"/"+t.Lemma.ToLower(Turkish)+"/"+t.Pos+"/"+string.Join("|",t.Morphemes);
                if(before.Any(t=>t.RuntimeGuess||t.Pos=="Unknown")||after.Any(t=>t.RuntimeGuess||t.Pos=="Unknown")
                    ||!before.Select(Signature).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(after.Select(Signature).OrderBy(x=>x,StringComparer.Ordinal)))
                    result.Problems.Add("Sözcük, kök veya çekim imzası değişti/belirsizleşti.");
                // The moved phrase must end before the same finite surface predicate.
                // Its constituent internal order is already enforced by independent replay.
                bool Finite(MorphToken t)=>t.Pos=="Verb"&&!t.Morphemes.Any(m=>m.EndsWith("Part",StringComparison.Ordinal)||m.StartsWith("Inf",StringComparison.Ordinal)||m=="Converb");
                var oldFinite=before.Where(Finite).ToList();var newFinite=after.Where(Finite).ToList();
                if(oldFinite.Count!=1||newFinite.Count!=1||Signature(oldFinite[0])!=Signature(newFinite[0])
                    ||oldFinite[0].Start!=before.Where(t=>t.Pos!="Punctuation").Last().Start
                    ||newFinite[0].Start!=after.Where(t=>t.Pos!="Punctuation").Last().Start)
                    result.Problems.Add("Tek ve sonda kalan yüklem doğrulanamadı.");
            }
            result.Passed=result.Problems.Count==0;
            result.Notes.Add("Bu sınırlı kontrol; kayıtlı taşıma, aynı sözcük/ekler ve açık hâl eki içindir. Genel anlam veya doğal üslup kanıtı değildir.");
            return result;
        }
    }
}
