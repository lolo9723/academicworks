using System;
using System.Collections.Generic;
using System.Linq;

namespace AcademicParaphraser.Core.RuleEngine
{
    // Domain-independent clause schemas. No subject matter vocabulary or benchmark texts.
    public static class ClauseStructureCatalog
    {
        public static IReadOnlyList<RuleDefinition> Create()
        {
            var rules = new List<RuleDefinition>();
            void Add(string pattern, string target, string hint, params GroupConstraint[] constraints)
            {
                rules.Add(new RuleDefinition { Id = "clause-v1-" + (rules.Count + 1).ToString("0000"), Pattern = pattern, Target = target,
                    RequiredText = hint, Family = "cümle kuruluşu", Strength = Strength.Moderate, Confidence = .96,
                    SentenceBound = true, GroupConstraints = constraints.ToList() });
            }
            // Keep a manner adverb next to its predicate when moving the reporting
            // scope; do not leave 'deneysel olarak, bu çalışmada' as a dangling phrase.
            Add(@"\b(?<scope>(?:bu|mevcut|sunulan) (?:çalışmada|araştırmada|incelemede)) (?<subject>[^,.!?;\r\n\a]{0,900}?)(?<head>\p{L}+) (?<manner>\p{L}+ olarak) (?<verb>\p{L}+)\b",
                "${subject}${head} ${lower:scope} ${manner} ${verb}", " olarak ",
                new GroupConstraint { Group = "head", Pos = "Noun", Forbidden = new List<string> { "Acc", "Dat", "Gen", "Abl", "Loc", "Ins", "Equ" } },
                new GroupConstraint { Group = "verb", Pos = "Verb", Required = new List<string> { "Pass" }, Any = new List<string> { "Narr", "Past", "Prog1", "Prog2", "Aor", "Fut" }, Forbidden = new List<string> { "Imp" } });
            // Complement accusative -> nominative, reporting verb -> passive; the evidence
            // source becomes an instrumental. Negation and tense of both verbs stay intact.
            foreach (var reporter in new[] { "bulgular", "sonuçlar", "veriler", "ölçüm sonuçları", "deney sonuçları", "analiz sonuçları", "araştırma bulguları", "araştırma sonuçları" })
            {
                string suffix = reporter == "veriler" ? "le" : reporter.EndsWith("lar", StringComparison.Ordinal) ? "la" : "yla";
                foreach (var verb in new[] { "göstermektedir", "göstermemektedir", "göstermiştir", "göstermemiştir", "gösterir", "göstermez", "doğrulamaktadır", "doğrulamamaktadır", "doğrulamıştır", "doğrulamamıştır", "belirtmektedir", "belirtmemektedir" })
                {
                    Add(@"\b(?<reporter>" + reporter + @"),? (?<claim>[^!?;\r\n\a]{3,1100}?)(?<complement>\p{L}+(?:[dt][ıiuü]ğ[ıiuü]|[ae]cağ[ıiuü]))n[ıiuü] (?<verb>" + verb + @")\b",
                        "${claim}${complement}, ${lower:reporter}" + suffix + " ${structure:Passive:verb}", verb,
                        new GroupConstraint { Group = "complement", Any = new List<string> { "PastPart", "FutPart" } },
                        new GroupConstraint { Group = "verb", Pos = "Verb", Forbidden = new List<string> { "Pass", "Able", "Unable", "A1sg", "A2sg", "A1pl", "A2pl" } });
                }
            }
            // A past reporting clause before 'olmakla birlikte' is made finite; the
            // contrast is retained explicitly. Active hypothetical 'olsa da' is not promoted.
            foreach (var ending in new[] { new[] { "mış", "tır" }, new[] { "miş", "tir" }, new[] { "muş", "tur" }, new[] { "müş", "tür" } })
            {
                Add(@"\b(?<clause>[^.!?;\r\n\a]{3,1100}?)(?<verb>\p{L}+" + ending[0] + @") olmakla birlikte,? (?<following>[^.!?;\r\n\a]{3,1100})",
                    "${clause}${verb}" + ending[1] + "; bununla birlikte ${lower:following}", "olmakla birlikte",
                    new GroupConstraint { Group = "verb", Required = new List<string> { "Verb" }, Any = new List<string> { "Narr", "NarrPart" } });
            }
            // Temporal procedural passives have a narrow licensed nominalization. Noun
            // genitive/possessive suffixes come from the real generator, never string guessing.
            foreach (var procedure in new[] { "incelenirken", "karşılaştırılırken", "ölçülürken", "hesaplanırken", "değerlendirilirken" })
            {
                Add(@"\b(?<subject>[^,.!?;\r\n\a]{0,900}?)(?<head>\p{L}+) (?<verb>" + procedure + @"),? (?<following>[^.!?;\r\n\a]{3,1100})",
                    "${subject}${structure:Gen:head} ${structure:Nominal:verb} sırasında, ${lower:following}", procedure,
                    new GroupConstraint { Group = "head", Pos = "Noun", Forbidden = new List<string> { "Acc", "Dat", "Gen", "Abl", "Loc", "Ins", "Equ" } },
                    new GroupConstraint { Group = "verb", Required = new List<string> { "Pass", "While" }, Forbidden = new List<string> { "Neg", "Able", "Unable" } });
            }
            // Method frames retain the complete subject and explicit context. No actor,
            // chronology, causal relation or lexical synonym is supplied.
            foreach (var operation in new[] { new[] { "incelen", "inceleme" }, new[] { "araştırıl", "araştırma" }, new[] { "değerlendiril", "değerlendirme" }, new[] { "analiz edil", "analiz" }, new[] { "karşılaştırıl", "karşılaştırma" } })
            {
                foreach (var time in new[] { new[] { "miştir", "yapılmıştır" }, new[] { "memiştir", "yapılmamıştır" }, new[] { "mektedir", "yapılmaktadır" }, new[] { "memektedir", "yapılmamaktadır" } })
                {
                    string ending = operation[0].EndsWith("ıl", StringComparison.Ordinal) ? time[0].Replace('i', 'ı') : time[0];
                    Add(@"\b(?<object>[^,.!?;\r\n\a]{3,800}), (?<context>[^,.!?;\r\n\a]{3,220} (?:altında|bakımından|açısından|arasında|yoluyla|aracılığıyla)) " + operation[0] + ending + @"\b",
                        "${object} üzerinde ${lower:context} " + operation[1] + " " + time[1], " " + operation[0]);
                }
            }
            return rules;
        }
    }
}
