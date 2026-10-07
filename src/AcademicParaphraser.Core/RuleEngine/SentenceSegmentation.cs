using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AcademicParaphraser.Core.RuleEngine
{
    public static class SentenceSegmentation
    {
        private static readonly HashSet<string> Abbreviations = new HashSet<string>(new[] { "dr", "prof", "doç", "vd", "vb", "vs", "örn", "bkz", "no", "sf", "s", "m", "md" }, StringComparer.OrdinalIgnoreCase);
        public static IReadOnlyList<TextSpan> Find(string text, IReadOnlyList<TextSpan> protections)
        {
            var result = new List<TextSpan>(); int start = 0;
            for (int i = 0; i <= text.Length; i++)
            {
                bool delimiter = i < text.Length && (text[i] == '.' || text[i] == '!' || text[i] == '?') &&
                    (i + 1 == text.Length || char.IsWhiteSpace(text[i + 1])) && !protections.Any(s => s.Intersects(i, 1));
                if (delimiter && text[i] == '.')
                {
                    int wordStart = i;
                    while (wordStart > start && char.IsLetter(text[wordStart - 1])) wordStart--;
                    string word = text.Substring(wordStart, i - wordStart);
                    if (Abbreviations.Contains(word) || (word.Length == 1 && char.IsUpper(word[0]))) delimiter = false;
                }
                bool end = i == text.Length || text[i] == '\r' || text[i] == '\n' || text[i] == '\a' || delimiter;
                if (!end) continue;
                int length = i == text.Length ? i - start : i + 1 - start;
                if (length > 0 && text.Substring(start, length).Any(char.IsLetter)) result.Add(new TextSpan { Start = start, Length = length });
                start = i + 1;
            }
            return result;
        }
        public static bool ContainsWholeMatchAtStart(string source, int start, int length, IReadOnlyList<TextSpan> sentences)
        {
            var sentence = sentences.FirstOrDefault(s => start >= s.Start && start + length <= s.End);
            return sentence != null && source.Substring(sentence.Start, start - sentence.Start).All(char.IsWhiteSpace);
        }
    }
}
