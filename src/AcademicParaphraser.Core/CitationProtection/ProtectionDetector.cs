using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
namespace AcademicParaphraser.Core.CitationProtection
{
    public sealed class ProtectionDetector
    {
        private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
        private static Regex Rx(string pattern) => new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(150));
        private static readonly Regex Citation = Rx(@"\([^()\r\n]*(?:\b(?:18|19|20|21)\d{2}[a-z]?\b|[ββα]\s*=|\bp\s*[<>=])[^()\r\n]*\)|\b\p{Lu}[\p{L}\p{M}'’\-]+(?:\s+(?:vd\.|et\s+al\.|ve\s+\p{Lu}[\p{L}\p{M}]+))?\s*\((?:18|19|20|21)\d{2}[a-z]?\)|\[\d+(?:\s*[,;–\-]\s*\d+)*\]");
        private static readonly Regex Identifier = Rx(@"(?:https?://|www\.)[^\s<>]+|\b10\.\d{4,9}/[^\s<>]+|\b(?:ISBN|ISSN|PMID)\s*[:=]?\s*[\dXx\-\s]+\d|\b[^\s@]+@[^\s@]+\.[^\s@]+");
        private static readonly Regex Numbers = Rx(@"\b[Nn]\s*=\s*\d+|%\s*\d+(?:[.,]\d+)?|[pββα]\s*[<>=≤≥]\s*[.,]?\d+(?:[.,]\d+)?|\b[tF]\s*\([\d,\s]+\)|R[²2]|\b\d+(?:[.,:/–\-]\d+)*\b");
        private static readonly Regex Names = Rx(@"\b\p{Lu}[\p{L}\p{M}]+(?:\s+\p{Lu}[\p{L}\p{M}]+)+|\b[\p{L}\p{M}]+['’][\p{L}\p{M}]+|\b[A-ZÇĞİÖŞÜ]{2,}\b");
        public IReadOnlyList<TextSpan> Detect(string text, UserSettings settings, IEnumerable<string> locks, IEnumerable<LexiconEntry> lexicon, IEnumerable<TextSpan>? external = null)
        {
            var spans = new List<TextSpan>();
            void Add(Regex rx, string reason)
            {
                foreach (Match m in rx.Matches(text))
                    spans.Add(new TextSpan { Start = m.Index, Length = m.Length, Reason = reason });
            }
            if (settings.PreserveCitations)
                Add(Citation, "atıf");
            if (settings.PreserveCitations || settings.PreserveLinks)
                Add(Identifier, "adres/kimlik");
            if (settings.PreserveNumbers)
                Add(Numbers, "sayısal veri");
            if (settings.PreserveNames)
                Add(Names, "özel isim");
            foreach (var pattern in settings.CustomProtectionPatterns)
                Add(Rx(pattern), "özel koruma");
            var terms = locks.Concat(settings.PreserveTechnicalTerms ? lexicon.Where(x => x.Technical).Select(x => x.Lemma) : Enumerable.Empty<string>());
            string lowered = text.ToLower(Turkish);
            foreach (string term in terms.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
            {
                string key = term.ToLower(Turkish);
                var keys = new List<string> { key };
                char last = key[key.Length - 1];
                char softened = last == 'p' ? 'b' : last == 't' ? 'd' : last == 'ç' ? 'c' : last == 'k' ? 'ğ' : last;
                if (softened != last)
                    keys.Add(key.Substring(0, key.Length - 1) + softened);
                foreach (string form in keys)
                {
                    int start = 0;
                    while ((start = lowered.IndexOf(form, start, StringComparison.Ordinal)) >= 0)
                    {
                        bool left = start == 0 || !char.IsLetterOrDigit(lowered[start - 1]);
                        int end = start + form.Length;
                        // Lock an inflected technical term's final word, including its Turkish suffix.
                        if (left)
                        {
                            while (end < text.Length && (char.IsLetter(text[end]) || text[end] == '\'' || text[end] == '’'))
                                end++;
                            spans.Add(new TextSpan { Start = start, Length = end - start, Reason = "kilitli/teknik terim" });
                        }
                        start += form.Length;
                    }
                }
            }
            if (external != null)
                spans.AddRange(external);
            return Merge(spans.Where(s => s.Start >= 0 && s.Length > 0 && s.End <= text.Length));
        }
        public static IReadOnlyList<TextSpan> Merge(IEnumerable<TextSpan> spans)
        {
            var result = new List<TextSpan>();
            foreach (var s in spans.OrderBy(s => s.Start).ThenBy(s => s.End))
            {
                var last = result.LastOrDefault();
                if (last != null && s.Start < last.End)
                {
                    last.Length = Math.Max(last.End, s.End) - last.Start;
                    last.Reason += ";" + s.Reason;
                }
                else
                    result.Add(new TextSpan { Start = s.Start, Length = s.Length, Reason = s.Reason });
            }
            return result;
        }
    }
}
