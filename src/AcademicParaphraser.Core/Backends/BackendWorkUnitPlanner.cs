using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AcademicParaphraser.Core.RuleEngine;

namespace AcademicParaphraser.Core.Backends
{
    public static class BackendWorkUnitPlanner
    {
        public const int MaximumLength = 2400;

        // Keep absolute Word offsets and original whitespace. Split only long
        // paragraphs at existing sentence boundaries, never inside protected text.
        public static IReadOnlyList<TextSpan> Plan(string source, IReadOnlyList<TextSpan> protections)
        {
            var result = new List<TextSpan>();
            foreach (Match paragraph in Regex.Matches(source, @"[^\r\n\a\v\f]+"))
            {
                if (paragraph.Length <= MaximumLength)
                {
                    result.Add(new TextSpan { Start = paragraph.Index, Length = paragraph.Length });
                    continue;
                }
                var local = protections.Where(p => p.Intersects(paragraph.Index, paragraph.Length))
                    .Select(p => new TextSpan { Start = Math.Max(p.Start, paragraph.Index) - paragraph.Index,
                        Length = Math.Min(p.End, paragraph.Index + paragraph.Length) - Math.Max(p.Start, paragraph.Index) }).ToArray();
                TextSpan? current = null;
                foreach (var sentence in SentenceSegmentation.Find(paragraph.Value, local))
                {
                    int start = paragraph.Index + sentence.Start, end = paragraph.Index + sentence.End;
                    while (start < end && char.IsWhiteSpace(source[start])) start++;
                    if (start == end) continue;
                    if (current != null && end - current.Start > MaximumLength)
                    {
                        result.Add(current);
                        current = null;
                    }
                    if (current == null) current = new TextSpan { Start = start, Length = end - start };
                    else current.Length = end - current.Start;
                }
                if (current != null) result.Add(current);
            }
            return result;
        }
    }
}
