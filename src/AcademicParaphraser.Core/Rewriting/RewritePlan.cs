using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
        public string Language { get; set; } = "tr";
        public IReadOnlyList<SentenceFrame> Analysis { get; set; } = Array.Empty<SentenceFrame>();
        public string StructuralScaffold { get; set; } = "";
        public IReadOnlyDictionary<string,string> DictionaryClues { get; set; } = new Dictionary<string,string>();
        public IReadOnlyList<RewriteBlock> Blocks { get; }
        public string MaskedSource => string.Concat(Blocks.Select(b => b.Editable ? b.Text : Marker(b)));
        public static string Marker(RewriteBlock block) => "[[AP_" + block.Id + "]]";
        // Literal anchors let the model read real words, rather than hiding essential
        // predicates behind tokens. Ambiguous format boundaries still need explicit markers.
        public bool CanCompileNaturalText => Blocks.Where(b=>!b.Editable)
            .All(b=>b.Text.Length>0 && (b.Text.Any(char.IsLetterOrDigit) || b == Blocks.First() || b == Blocks.Last()));
        public IReadOnlyDictionary<string,string> CompileNaturalText(string target)
        {
            if(!CanCompileNaturalText || target==null || target.Contains("[[AP_") || target.Length>Math.Max(4000,Source.Length*3))
                throw new ArgumentException("Metin Word biçim sınırlarına kesin olarak eşlenemiyor.");
            string pattern="\\A"+string.Concat(Blocks.Select(b=>b.Editable ? "(?<"+b.Id+">[^\\r\\n]*?)" : Regex.Escape(b.Text)))+"\\z";
            var match=Regex.Match(target,pattern,RegexOptions.None,TimeSpan.FromMilliseconds(200));
            if(!match.Success)throw new ArgumentException("Korunan metin veya sırası değişmiş; Word eşlemesi yapılamadı.");
            var replacements=Blocks.Where(b=>b.Editable).ToDictionary(b=>b.Id,b=>match.Groups[b.Id].Value,StringComparer.Ordinal);
            var compiled=BuildCandidate(replacements,"local-model:natural-paragraph");
            if(!string.Equals(compiled.Text,target,StringComparison.Ordinal))
                throw new ArgumentException("Yeni metnin boşlukları Word sınırlarına uymuyor.");
            return replacements;
        }
        public IReadOnlyDictionary<string,string> ExtractMaskedReplacements(string masked)
        {
            if (masked == null || masked.Length > Math.Max(4000, Source.Length * 3)) throw new ArgumentException("Yeniden yazım çok uzun/boş.");
            masked=masked.Trim();
            var last=Blocks.LastOrDefault();
            if(last!=null&&!last.Editable)
            {
                string punctuation=System.Text.RegularExpressions.Regex.Match(last.Text,@"[.!?]+$").Value;
                if(punctuation.Length>0)
                {
                    int marker=masked.LastIndexOf(Marker(last),StringComparison.Ordinal);
                    if(marker>=0&&masked.Substring(marker+Marker(last).Length).Trim()==punctuation)
                        masked=masked.Substring(0,marker+Marker(last).Length);
                }
            }
            var values = new Dictionary<string,string>(StringComparer.Ordinal);
            int cursor = 0;
            foreach (var block in Blocks)
            {
                if (!block.Editable)
                {
                    string marker = Marker(block);
                    if (cursor > masked.Length - marker.Length || masked.Substring(cursor,marker.Length) != marker)
                        throw new ArgumentException("Sabit Word/terim işareti eksik veya sırası değişmiş.");
                    cursor += marker.Length; continue;
                }
                var next = Blocks.SkipWhile(b => b.Id != block.Id).Skip(1).FirstOrDefault(b => !b.Editable);
                int end = next == null ? masked.Length : masked.IndexOf(Marker(next),cursor,StringComparison.Ordinal);
                if (end < cursor) throw new ArgumentException("Korunan metin işareti kayboldu.");
                string value = masked.Substring(cursor,end-cursor);
                if (value.Contains("[[AP_")) throw new ArgumentException("Bilinmeyen/tekrarlanan koruma işareti bulundu.");
                values.Add(block.Id,value);cursor=end;
            }
            if (cursor != masked.Length) throw new ArgumentException("Sabit son alanın dışında metin bulundu.");
            return values;
        }
        private RewritePlan(string source, List<RewriteBlock> blocks)
        {
            Source = source;
            Blocks = blocks.AsReadOnly();
        }

        // The writer sees whole paragraphs, but may replace only these ranges. Citations,
        // numbers, fields, links, run boundaries and Word control characters remain anchors.
        public static RewritePlan Create(string source, IEnumerable<TextSpan> writable, IEnumerable<TextSpan> protectedSpans)
        {
            if (string.IsNullOrWhiteSpace(source) || source.Length > 80000 || source.Contains("[[AP_"))
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
                    // Keep the paragraph edges fixed; spaces inside a writable run may move.
                    if (lo == 0) while (lo < hi && char.IsWhiteSpace(source[lo])) lo++;
                    if (hi == source.Length) while (hi > lo && char.IsWhiteSpace(source[hi - 1])) hi--;
                    if (hi > lo && source.Substring(lo, hi - lo).Any(char.IsLetter))
                        editable.Add(new TextSpan { Start = lo, Length = hi - lo });
                }
            }
            var blocks = new List<RewriteBlock>();
            int position = 0;
            foreach (var span in editable)
            {
                if (span.Start > position) blocks.Add(new RewriteBlock(blocks.Count, position, source.Substring(position, span.Start - position), false));
                else if(blocks.Count>0 && blocks[blocks.Count-1].Editable)
                    blocks.Add(new RewriteBlock(blocks.Count,position,"",false));
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

        public IReadOnlyList<string> CheckFixedTextMultiplicity(string target)
        {
            var problems=new List<string>();
            foreach(string anchor in Blocks.Where(b=>!b.Editable).Select(b=>b.Text.Trim()).Where(t=>t.Length>=2&&t.Any(char.IsLetterOrDigit)).Distinct(StringComparer.Ordinal))
            {
                string pattern=(char.IsLetterOrDigit(anchor[0])?@"(?<![\p{L}\p{M}\d])":"")+System.Text.RegularExpressions.Regex.Escape(anchor)
                    +(char.IsLetterOrDigit(anchor[anchor.Length-1])?@"(?![\p{L}\p{M}\d])":"");
                if(System.Text.RegularExpressions.Regex.Matches(Source,pattern,System.Text.RegularExpressions.RegexOptions.IgnoreCase|System.Text.RegularExpressions.RegexOptions.CultureInvariant).Count
                    !=System.Text.RegularExpressions.Regex.Matches(target,pattern,System.Text.RegularExpressions.RegexOptions.IgnoreCase|System.Text.RegularExpressions.RegexOptions.CultureInvariant).Count)
                    problems.Add("Korunan isim, terim veya alan metni başka bir yerde tekrarlandı/eksildi.");
            }
            return problems;
        }
        public Candidate BuildCandidate(IReadOnlyDictionary<string, string> replacements, string origin)
        {
            var editable = Blocks.Where(b => b.Editable).ToList();
            if (replacements.Count != editable.Count || replacements.Keys.Any(id => !editable.Any(b => b.Id == id)))
                throw new ArgumentException("Yeniden yazım yanıtında eksik veya bilinmeyen metin aralığı var.");
            var edits = new List<TextEdit>();
            foreach (var block in editable)
            {
                if (!replacements.TryGetValue(block.Id, out var replacement) || replacement == null
                    || (string.IsNullOrWhiteSpace(replacement) && editable.Count == 1 && Blocks.All(b => b.Editable))
                    || replacement.Any(char.IsControl) || replacement.Length > Math.Max(1000, block.Text.Length * 3)
                    || (replacement.Length > 0 && char.IsWhiteSpace(replacement[0]) && !char.IsWhiteSpace(block.Text[0]))
                    || (replacement.Length > 0 && char.IsWhiteSpace(replacement[replacement.Length - 1]) && !char.IsWhiteSpace(block.Text[block.Text.Length - 1])))
                    throw new ArgumentException("Yeniden yazım yanıtı Word yapısına uygun değil.");
                if (!string.Equals(block.Text, replacement, StringComparison.Ordinal))
                    edits.Add(new TextEdit { Start = block.Start, Length = block.Text.Length, Original = block.Text,
                        Replacement = replacement, RuleId = origin, Family = "paragraf yeniden yazımı", Confidence = 0 });
            }
            return new Candidate { Text = EditApplication.Apply(Source, edits), Edits = edits };
        }
    }
}
