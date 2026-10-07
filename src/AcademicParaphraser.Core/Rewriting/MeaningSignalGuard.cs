using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AcademicParaphraser.Core.Rewriting
{
    // Necessary checks, not a semantic-equivalence proof. Clause scope still needs review.
    public static class MeaningSignalGuard
    {
        private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
        private static readonly string[] Cues = { "yalnızca", "sadece", "mutlaka", "henüz", "her durumda", "zorunludur", "gereklidir", "olabilir", "düşünülmektedir", "anlamına gelmemektedir" };
        private static readonly string[] Features = { "Neg", "Able", "Unable", "Cond", "Neces", "Fut", "FutPart" };
        public static bool Preserved(string source, string target, IReadOnlyList<MorphToken> before, IReadOnlyList<MorphToken> after)
        {
            string a = source.ToLower(Turkish), b = target.ToLower(Turkish);
            foreach (string cue in Cues)
                if (Regex.Matches(a, @"\b" + Regex.Escape(cue) + @"\b").Count != Regex.Matches(b, @"\b" + Regex.Escape(cue) + @"\b").Count) return false;
            foreach (string feature in Features)
                if (before.Count(t => HasSignal(t, feature)) != after.Count(t => HasSignal(t, feature))) return false;
            return true;
        }
        private static bool HasSignal(MorphToken token, string feature)
        {
            // Zemberek may resolve the academic nouns 'çalışma/inceleme' as A2sg
            // negative imperatives. These ambiguous readings are not evidence of a
            // negated proposition. Likewise 'ise' is often a discourse connective.
            if (feature == "Neg" && token.Morphemes.Contains("Imp")) return false;
            if (feature == "Cond" && token.Surface.Equals("ise", StringComparison.OrdinalIgnoreCase)) return false;
            return token.Morphemes.Contains(feature);
        }
    }
}
