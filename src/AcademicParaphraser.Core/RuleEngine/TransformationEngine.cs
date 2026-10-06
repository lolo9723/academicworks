using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core.CitationProtection;
using AcademicParaphraser.Core.Rewriting;
namespace AcademicParaphraser.Core.RuleEngine
{
    public sealed class TransformationEngine
    {
        private readonly IRuleCatalog catalog; private readonly ITurkishNlp nlp; private readonly ILexicalSource? lexicalSource; private readonly ProtectionDetector detector = new ProtectionDetector();
        private readonly ConcurrentDictionary<string, Regex> patterns = new ConcurrentDictionary<string, Regex>(StringComparer.Ordinal);
        private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
        private static readonly HashSet<string> StructuralFamilies = new HashSet<string>(new[] { "etken-edilgen", "edilgen-etken", "isim-fiil", "fiil-isim", "isim tamlaması", "yüklem", "yan cümlecik", "sıfat-fiil", "zarf-fiil", "güvenli sıralama", "yüklem merkezli", "neden-sonuç", "karşılaştırma", "yöntem", "sonuç" }, StringComparer.Ordinal);
        public TransformationEngine(IRuleCatalog catalog, ITurkishNlp nlp, ILexicalSource? lexicalSource = null)
        {
            this.lexicalSource = lexicalSource;
            this.catalog = catalog;
            this.nlp = nlp;
        }
        public async Task<IReadOnlyList<Candidate>> GenerateAsync(string text, UserSettings settings, IEnumerable<TextSpan>? external, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Önce bir metin seçin.");
            if (text.Length > 80000)
                throw new ArgumentException("Seçim çok uzun; daha küçük bir bölüm seçin.");
            var lexicon = catalog.GetLexicon();
            var protectedSpans = detector.Detect(text, settings, catalog.GetLockedTerms(), lexicon, external).ToList();
            var morphology = new List<MorphToken>();
            // Paragraphs and table cells have independent contexts; retain original UTF-16 offsets.
            foreach (Match paragraph in Regex.Matches(text, @"[^\r\n\a]+"))
            {
                token.ThrowIfCancellationRequested();
                string analysisText = new string(paragraph.Value.Select(c => char.IsControl(c) ? ' ' : c).ToArray());
                var parsed = await nlp.AnalyzeAsync(analysisText, token).ConfigureAwait(false);
                foreach (var word in parsed)
                {
                    word.Start += paragraph.Index;
                    morphology.Add(word);
                }
            }
            if (settings.PreserveNames)
                protectedSpans.AddRange(morphology.Where(t => t.Proper).Select(t => new TextSpan { Start = t.Start, Length = t.Length, Reason = "morfolojik özel isim" }));
            protectedSpans = ProtectionDetector.Merge(protectedSpans).ToList();
            var lookup = lexicon.GroupBy(e => e.Lemma + "/" + e.Pos).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            if (lexicalSource != null)
            {
                var eligible = morphology.Where(t => !t.Proper && !protectedSpans.Any(p => p.Intersects(t.Start, t.Length)) && !lookup.ContainsKey(t.Lemma + "/" + t.Pos)).ToList();
                foreach (var item in await lexicalSource.FindAsync(text, eligible, settings, token).ConfigureAwait(false))
                    if (!lookup.ContainsKey(item.Lemma + "/" + item.Pos)) lookup.Add(item.Lemma + "/" + item.Pos, item);
            }
            var candidates = new List<List<TextEdit>>();
            string lowered = text.ToLower(Turkish);
            foreach (var rule in catalog.GetRules().Where(r => (int)r.Strength <= (int)settings.DefaultStrength && r.Confidence >= settings.MinimumConfidence && (r.Domain == "genel" || r.Domain == settings.Domain)))
            {
                token.ThrowIfCancellationRequested();
                if (!rule.UserDefined && !string.IsNullOrEmpty(rule.RequiredText) && lowered.IndexOf(rule.RequiredText, StringComparison.Ordinal) < 0) continue;
                if (patterns.Count > 4096) patterns.Clear();
                var regex = patterns.GetOrAdd(rule.Pattern, key => new Regex(key, RegexOptions.None, TimeSpan.FromMilliseconds(150)));
                foreach (Match match in regex.Matches(lowered))
                {
                    if (match.Value.Any(char.IsControl))
                        continue;
                    var tokens = morphology.Where(t => t.Start >= match.Index && t.Start < match.Index + match.Length).ToList();
                    if (rule.PosConstraint.Length > 0 && !tokens.Any(t => t.Pos == rule.PosConstraint))
                        continue;
                    if (rule.RequiredMorphemes.Any(m => !tokens.Any(t => t.Morphemes.Contains(m))) || rule.ForbiddenMorphemes.Any(m => tokens.Any(t => t.Morphemes.Contains(m))))
                        continue;
                    if (rule.ContextPattern.Length > 0 && !Regex.IsMatch(ParagraphAt(text, match.Index).ToLower(Turkish), rule.ContextPattern, RegexOptions.None, TimeSpan.FromMilliseconds(150)))
                        continue;
                    string original = text.Substring(match.Index, match.Length);
                    string replacement = ExpandTarget(rule.Target, match, text);
                    replacement = CaseLike(original, replacement);
                    if (replacement.Any(char.IsControl))
                        continue;
                    if (original != replacement)
                    {
                        var changes = AnchoredRuleEdits.Create(text, match.Index, match.Length, replacement, protectedSpans, rule);
                        if (changes != null && changes.Count > 0) candidates.Add(changes.ToList());
                    }
                }
            }
            var inflections = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var word in morphology.Where(t => !t.Proper && t.Length > 1 && !protectedSpans.Any(s => s.Intersects(t.Start, t.Length))))
            {
                lookup.TryGetValue(word.Lemma + "/" + word.Pos, out var entry);
                if (entry == null || entry.Technical || entry.Confidence < settings.MinimumConfidence || (entry.Domain != "genel" && entry.Domain != settings.Domain))
                    continue;
                foreach (var synonym in entry.Synonyms.Concat(entry.AcademicAlternatives).Where(x => !entry.ForbiddenReplacements.Contains(x)).Distinct())
                {
                    string key = word.Surface + "/" + synonym + "/" + word.Pos;
                    if (!inflections.TryGetValue(key, out var inflected))
                    {
                        inflected = await nlp.InflectAsync(word.Surface, synonym, word.Pos, token).ConfigureAwait(false);
                        inflections[key] = inflected;
                    }
                    if (string.IsNullOrWhiteSpace(inflected))
                        continue;
                    string replacement = CaseLike(word.Surface, inflected!);
                    if (replacement == word.Surface)
                        continue;
                    candidates.Add(new List<TextEdit> { new TextEdit { Start = word.Start, Length = word.Length, Original = word.Surface, Replacement = replacement, RuleId = "lemma:" + word.Lemma + ":" + synonym, Family = "morfolojik eş anlam", Confidence = entry.Confidence } });
                }
            }
            if (candidates.Count == 0)
                return new[] { new Candidate { Text = text, Score = new CandidateScore { SemanticSafety = 1, GrammarConfidence = 1, AcademicStyle = 1 } } };
            var proposals = new List<Candidate>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            // Deterministic variants rotate competing rules, then reject duplicates and near-identical alternatives.
            for (int variant = 0; variant < Math.Max(12, settings.Alternatives * 8); variant++)
            {
                token.ThrowIfCancellationRequested();
                var edits = new List<TextEdit>();
                var groups = candidates.GroupBy(e => e.Min(x => x.Start))
                    .OrderByDescending(g => g.Max(e => Rank(e, settings.DefaultStrength)))
                    .ThenByDescending(g => g.Max(e => e.Max(x => x.Start + x.Length) - e.Min(x => x.Start)))
                    .ThenBy(g => g.Key).ToList();
                foreach (var group in groups)
                {
                    var options = group.OrderByDescending(e => Rank(e, settings.DefaultStrength)).ThenByDescending(e => e.Max(x => x.Start + x.Length) - e.Min(x => x.Start)).ThenByDescending(e => e.Min(x => x.Confidence)).ThenBy(e => e[0].RuleId, StringComparer.Ordinal).ToList();
                    var selected = options[(variant + groups.IndexOf(group)) % options.Count];
                    if (selected.Any(x => edits.Any(e => e.Start < x.Start + x.Length && e.Start + e.Length > x.Start)))
                        continue;
                    if (variant >= 6 && groups.Count > 1 && (groups.IndexOf(group) + variant) % 3 == 0)
                        continue;
                    edits.AddRange(selected);
                }
                string result = EditApplication.Apply(text, edits);
                if (!seen.Add(result))
                    continue;
                if (proposals.Any(c => Difference(c.Text, result) < .04))
                    continue;
                var score = new CandidateScore { SemanticSafety = edits.Count == 0 ? 1 : edits.Min(e => e.Confidence), GrammarConfidence = edits.Count == 0 ? 1 : edits.Min(e => e.Confidence), StructuralDifference = edits.Count(e => StructuralFamilies.Contains(e.Family)) / (double)Math.Max(1, edits.Count), LexicalDifference = Difference(text, result), AcademicStyle = edits.Count == 0 ? 1 : edits.Average(e => e.Confidence) };
                proposals.Add(new Candidate { Text = result, Edits = edits, Score = score });
                if (proposals.Count >= settings.Alternatives)
                    break;
            }
            return proposals.OrderByDescending(p => p.Score.StructuralDifference).ThenByDescending(p => p.Score.LexicalDifference).ThenByDescending(p => p.Score.SemanticSafety).ToList();
        }
        private static int Rank(List<TextEdit> edits, Strength strength)
        {
            if (strength == Strength.Strong && edits.Any(e => e.Family == "güvenli sıralama")) return 2;
            return strength != Strength.Light && edits.Any(e => StructuralFamilies.Contains(e.Family)) ? 1 : 0;
        }
        private static string ExpandTarget(string target, Match match, string original)
        {
            return Regex.Replace(target, @"\$\$|\$&|\$\{(?<lower>lower:)?(?<name>[A-Za-z0-9_]+)\}|\$(?<number>[0-9]+)", m =>
            {
                if (m.Value == "$$") return "$";
                if (m.Value == "$&") return original.Substring(match.Index, match.Length);
                string key = m.Groups["name"].Success ? m.Groups["name"].Value : m.Groups["number"].Value;
                var group = match.Groups[key];
                string value = group.Success ? original.Substring(group.Index, group.Length) : "";
                return m.Groups["lower"].Success && value.Length > 0 ? char.ToLower(value[0], Turkish) + value.Substring(1) : value;
            });
        }
        private static string CaseLike(string original, string replacement)
        {
            if (original.Length == 0 || replacement.Length == 0)
                return replacement;
            if (original.All(c => !char.IsLetter(c) || char.IsUpper(c)))
                return replacement.ToUpper(Turkish);
            return char.IsUpper(original[0]) ? char.ToUpper(replacement[0], Turkish) + replacement.Substring(1) : replacement;
        }
        private static string ParagraphAt(string text, int position)
        {
            int start = position, end = position;
            while (start > 0 && text[start - 1] != '\r' && text[start - 1] != '\n' && text[start - 1] != '\a')
                start--;
            while (end < text.Length && text[end] != '\r' && text[end] != '\n' && text[end] != '\a')
                end++;
            return text.Substring(start, end - start);
        }
        private static double Difference(string a, string b)
        {
            var x = new HashSet<string>(Regex.Matches(a.ToLower(Turkish), @"\p{L}+").Cast<Match>().Select(m => m.Value));
            var y = new HashSet<string>(Regex.Matches(b.ToLower(Turkish), @"\p{L}+").Cast<Match>().Select(m => m.Value));
            return 1 - x.Intersect(y).Count() / (double)Math.Max(1, x.Union(y).Count());
        }
    }
}
