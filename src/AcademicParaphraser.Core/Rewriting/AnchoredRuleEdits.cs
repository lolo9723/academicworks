using System;
using System.Collections.Generic;
using System.Linq;

namespace AcademicParaphraser.Core.Rewriting
{
    public static class AnchoredRuleEdits
    {
        // A rule may reshape the words around a number/term/field without replacing that anchor.
        // All returned edits form one indivisible proposal; a missing anchor rejects the rule.
        public static IReadOnlyList<TextEdit>? Create(string source, int start, int length, string replacement, IReadOnlyList<TextSpan> protectedSpans, RuleDefinition rule)
        {
            int end = start + length;
            var anchors = protectedSpans.Where(s => s.Intersects(start, length)).OrderBy(s => s.Start).ToList();
            if (anchors.Any(a => a.Start < start || a.End > end)) return null;
            var edits = new List<TextEdit>();
            int oldPosition = start, newPosition = 0;
            foreach (var anchor in anchors)
            {
                string value = source.Substring(anchor.Start, anchor.Length);
                int target = replacement.IndexOf(value, newPosition, StringComparison.Ordinal);
                if (target < 0 || !Gap(source, oldPosition, anchor.Start, replacement.Substring(newPosition, target - newPosition), rule, edits)) return null;
                oldPosition = anchor.End; newPosition = target + anchor.Length;
            }
            return Gap(source, oldPosition, end, replacement.Substring(newPosition), rule, edits) ? edits : null;
        }
        private static bool Gap(string source, int start, int end, string replacement, RuleDefinition rule, List<TextEdit> edits)
        {
            string original = source.Substring(start, end - start);
            if (original == replacement) return true;
            int left = 0;
            while (left < original.Length && left < replacement.Length && original[left] == replacement[left]) left++;
            int right = 0;
            while (right < original.Length - left && right < replacement.Length - left && original[original.Length - right - 1] == replacement[replacement.Length - right - 1]) right++;
            // Word edits use a verified nonempty source Range. Absorb one unchanged character
            // inside this unprotected gap when the change is a pure insertion.
            if (original.Length - left - right == 0)
            {
                if (right > 0) right--;
                else if (left > 0) left--;
                else return false;
            }
            string old = original.Substring(left, original.Length - left - right);
            string value = replacement.Substring(left, replacement.Length - left - right);
            if (old.Any(char.IsControl) || value.Any(char.IsControl)) return false;
            edits.Add(new TextEdit { Start = start + left, Length = old.Length, Original = old, Replacement = value,
                RuleId = rule.Id, Family = rule.Family, Confidence = rule.Confidence });
            return true;
        }
    }
}
