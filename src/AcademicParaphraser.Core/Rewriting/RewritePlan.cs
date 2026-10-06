using System;
using System.Collections.Generic;
using System.Linq;

namespace AcademicParaphraser.Core.Rewriting
{
    public sealed class RewriteBlock
    {
        public string Id { get; }
        public int Start { get; }
        public string Text { get; }
        public bool Editable { get; }
        internal RewriteBlock(int index, int start, string text, bool editable)
        {
            Id = "B" + index.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
            Start = start;
            Text = text;
            Editable = editable;
        }
    }

    public sealed class RewritePlan
    {
        public string Source { get; }
        public IReadOnlyList<RewriteBlock> Blocks { get; }
        private RewritePlan(string source, List<RewriteBlock> blocks)
        {
            Source = source;
            Blocks = blocks.AsReadOnly();
        }

        // The writer sees whole paragraphs, but may replace only these ranges. Citations,
        // numbers, fields, links, run boundaries and Word control characters remain anchors.
        public static RewritePlan Create(string source, IEnumerable<TextSpan> writable, IEnumerable<TextSpan> protectedSpans)
        {
            if (string.IsNullOrWhiteSpace(source) || source.Length > 80000)
                throw new ArgumentException("Yeniden yazılacak seçim boş veya çok uzun.");
            var allowed = writable.OrderBy(s => s.Start).ToList();
            var blocked = protectedSpans.ToList();
            int end = 0;
            foreach (var span in allowed)
            {
                ValidateSpan(source, span);
                if (span.Start < end) throw new ArgumentException("Düzenlenebilir aralıklar çakışıyor.");
                end = span.End;
            }
            foreach (var span in blocked) ValidateSpan(source, span);
            for (int i = 0; i < source.Length; i++)
                if (char.IsControl(source[i])) blocked.Add(new TextSpan { Start = i, Length = 1 });

            var editable = new List<TextSpan>();
            foreach (var region in allowed)
            {
                var boundaries = blocked.Where(s => s.Intersects(region.Start, region.Length))
                    .SelectMany(s => new[] { Math.Max(region.Start, s.Start), Math.Min(region.End, s.End) })
                    .Concat(new[] { region.Start, region.End }).Distinct().OrderBy(i => i).ToList();
                for (int i = 1; i < boundaries.Count; i++)
                {
                    int lo = boundaries[i - 1], hi = boundaries[i];
                    if (blocked.Any(s => s.Intersects(lo, hi - lo))) continue;
                    // Preserve whitespace between a rewritten expression and its fixed neighbour.
                    while (lo < hi && char.IsWhiteSpace(source[lo])) lo++;
                    while (hi > lo && char.IsWhiteSpace(source[hi - 1])) hi--;
                    if (hi > lo && source.Substring(lo, hi - lo).Any(char.IsLetter))
                        editable.Add(new TextSpan { Start = lo, Length = hi - lo });
                }
            }
            var blocks = new List<RewriteBlock>();
            int position = 0;
            foreach (var span in editable)
            {
                if (span.Start > position) blocks.Add(new RewriteBlock(blocks.Count, position, source.Substring(position, span.Start - position), false));
                blocks.Add(new RewriteBlock(blocks.Count, span.Start, source.Substring(span.Start, span.Length), true));
                position = span.End;
            }
            if (position < source.Length) blocks.Add(new RewriteBlock(blocks.Count, position, source.Substring(position), false));
            return new RewritePlan(source, blocks);
        }

        private static void ValidateSpan(string source, TextSpan span)
        {
            if (span == null || span.Start < 0 || span.Length <= 0 || span.Start > source.Length - span.Length)
                throw new ArgumentException("Yeniden yazım aralığı geçersiz.");
        }

        public Candidate BuildCandidate(IReadOnlyDictionary<string, string> replacements, string origin)
        {
            var editable = Blocks.Where(b => b.Editable).ToList();
            if (replacements.Count != editable.Count || replacements.Keys.Any(id => !editable.Any(b => b.Id == id)))
                throw new ArgumentException("Yeniden yazım yanıtında eksik veya bilinmeyen metin aralığı var.");
            var edits = new List<TextEdit>();
            foreach (var block in editable)
            {
                if (!replacements.TryGetValue(block.Id, out var replacement) || string.IsNullOrWhiteSpace(replacement)
                    || replacement.Any(char.IsControl) || replacement.Length > Math.Max(1000, block.Text.Length * 3)
                    || char.IsWhiteSpace(replacement[0]) || char.IsWhiteSpace(replacement[replacement.Length - 1]))
                    throw new ArgumentException("Yeniden yazım yanıtı Word yapısına uygun değil.");
                if (!string.Equals(block.Text, replacement, StringComparison.Ordinal))
                    edits.Add(new TextEdit { Start = block.Start, Length = block.Text.Length, Original = block.Text,
                        Replacement = replacement, RuleId = origin, Family = "paragraf yeniden yazımı", Confidence = 0 });
            }
            return new Candidate { Text = EditApplication.Apply(Source, edits), Edits = edits };
        }
    }
}
