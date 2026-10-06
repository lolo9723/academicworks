using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
namespace AcademicParaphraser.Core.DocumentProtection
{
    public sealed class WordRun
    {
        public int Start
        {
            get; set;
        }
        public int Length
        {
            get; set;
        }
        public string FormatKey { get; set; } = ""; public string ContainerKey { get; set; } = ""; public bool Protected
        {
            get; set;
        }
        public XElement Node { get; set; } = null!;
    }
    public sealed class OoxmlRunMap
    {
        public static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private readonly XDocument document; private readonly List<WordRun> runs = new List<WordRun>();
        private sealed class FieldContext
        {
            public int Id
            {
                get; set;
            }
            public StringBuilder Code { get; } = new StringBuilder(); public bool Result
            {
                get; set;
            }
        }
        public string Text
        {
            get;
        }
        public IReadOnlyList<WordRun> Runs => runs;
        public OoxmlRunMap(string xml, bool preserveHyperlinks = true, string? preferredPart = null)
        {
            using (var reader = XmlReader.Create(new System.IO.StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, MaxCharactersInDocument = 16000000, XmlResolver = null }))
                document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            XNamespace pkg = "http://schemas.microsoft.com/office/2006/xmlPackage";
            var parts = document.Descendants(pkg + "part").ToList();
            var preferred = preferredPart == null ? null : parts.FirstOrDefault(x => ((string?)x.Attribute(pkg + "name") ?? "").StartsWith(preferredPart, StringComparison.Ordinal));
            var part = preferred ?? parts.FirstOrDefault(x => (string?)x.Attribute(pkg + "name") == "/word/document.xml");
            // A selection in another Word story can be serialized in a footnote/header package part.
            var body = part?.Descendants(pkg + "xmlData").Elements().FirstOrDefault() ?? document.Descendants(W + "body").FirstOrDefault() ?? document.Root;
            if (body == null)
                throw new InvalidOperationException("Word XML okunamadı.");
            var buffer = new StringBuilder();
            var fields = new List<FieldContext>();
            int fieldId = 0;
            var containers = body.Descendants().Where(e => e.Name == W + "hyperlink" || e.Name == W + "fldSimple").Select((element, index) => new { element, index }).ToDictionary(x => x.element, x => x.index);
            foreach (var p in body.Descendants(W + "p").Where(p => !p.Ancestors().Any(a => new[] { "drawing", "pict", "object", "txbxContent" }.Contains(a.Name.LocalName))))
            {
                bool whole = p.Ancestors().Any(a => new[] { "drawing", "pict", "object", "txbxContent", "sdt", "del", "ins", "moveFrom", "moveTo" }.Contains(a.Name.LocalName));
                foreach (var e in p.Descendants())
                {
                    if (e.Ancestors(W + "p").FirstOrDefault() != p)
                        continue;
                    if (e.Name == W + "drawing" && e.Descendants().Any(n => n.Name.LocalName == "inline") && !e.Ancestors().Any(a => new[] { "drawing", "pict", "object" }.Contains(a.Name.LocalName)))
                    {
                        buffer.Append('\u0001');
                        continue;
                    }
                    if (e.Name == W + "object" && !e.Ancestors().Any(a => new[] { "drawing", "pict", "object" }.Contains(a.Name.LocalName)))
                    {
                        buffer.Append('\u0001');
                        continue;
                    }
                    if (e.Name == W + "footnoteReference" || e.Name == W + "endnoteReference" || e.Name == W + "footnoteRef" || e.Name == W + "endnoteRef")
                    {
                        buffer.Append('\u0002');
                        continue;
                    }
                    if (e.Name == W + "fldChar")
                    {
                        var type = (string?)e.Attribute(W + "fldCharType");
                        if (type == "begin")
                            fields.Add(new FieldContext { Id = ++fieldId });
                        else if (type == "separate" && fields.Count > 0)
                            fields[fields.Count - 1].Result = true;
                        else if (type == "end" && fields.Count > 0)
                            fields.RemoveAt(fields.Count - 1);
                        continue;
                    }
                    if (e.Name == W + "instrText" && fields.Count > 0)
                    {
                        fields[fields.Count - 1].Code.Append(e.Value);
                        continue;
                    }
                    if (e.Name == W + "t")
                    {
                        var run = e.Ancestors(W + "r").FirstOrDefault();
                        string format = run?.Element(W + "rPr")?.ToString(SaveOptions.DisableFormatting) ?? "";
                        bool fieldProtected = fields.Any(f => !f.Result || preserveHyperlinks || !f.Code.ToString().TrimStart().StartsWith("HYPERLINK", StringComparison.OrdinalIgnoreCase));
                        bool simpleProtected = e.Ancestors(W + "fldSimple").Any(f => preserveHyperlinks || !((string?)f.Attribute(W + "instr") ?? "").TrimStart().StartsWith("HYPERLINK", StringComparison.OrdinalIgnoreCase));
                        bool blocked = whole || fieldProtected || simpleProtected || e.Ancestors().Any(a => new[] { "sdt", "del", "ins", "moveFrom", "moveTo", "oMath", "drawing", "pict", "object", "ruby" }.Contains(a.Name.LocalName)) || (preserveHyperlinks && e.Ancestors(W + "hyperlink").Any());
                        // Equal fonts do not make the boundary between a link and ordinary text editable.
                        string container = string.Join(",", e.Ancestors().Where(containers.ContainsKey).Select(a => containers[a])) + "/" + string.Join(",", fields.Select(f => f.Id));
                        runs.Add(new WordRun { Start = buffer.Length, Length = e.Value.Length, Node = e, FormatKey = format, ContainerKey = container, Protected = blocked });
                        buffer.Append(e.Value);
                    }
                    else if (e.Name == W + "tab")
                        buffer.Append('\t');
                    else if (e.Name == W + "br")
                        buffer.Append((string?)e.Attribute(W + "type") == "page" ? '\f' : '\v');
                    else if (e.Name == W + "cr")
                        buffer.Append('\v');
                }
                buffer.Append('\r');
                if (p.Parent?.Name == W + "tc" && p == p.Parent.Elements(W + "p").LastOrDefault())
                    buffer.Append('\a');
            }
            Text = buffer.ToString();
        }
        public IReadOnlyList<TextSpan> ProtectedSpans => runs.Where(r => r.Protected).Select(r => new TextSpan { Start = r.Start, Length = r.Length, Reason = "Word alanı" }).ToList();
        public IReadOnlyList<TextSpan> EditableSpans
        {
            get
            {
                var result = new List<TextSpan>();
                WordRun? previous = null;
                foreach (var run in runs)
                {
                    if (run.Protected || run.Length == 0) { previous = null; continue; }
                    if (previous != null && previous.Start + previous.Length == run.Start
                        && previous.FormatKey == run.FormatKey && previous.ContainerKey == run.ContainerKey)
                        result[result.Count - 1].Length += run.Length;
                    else
                        result.Add(new TextSpan { Start = run.Start, Length = run.Length });
                    previous = run;
                }
                return result;
            }
        }
        public bool CanEdit(int start, int length)
        {
            var affected = runs.Where(r => start < r.Start + r.Length && start + length > r.Start).ToList();
            return start >= 0 && length > 0 && affected.Count > 0 && !affected.Any(r => r.Protected) && affected.Select(r => r.FormatKey).Distinct().Count() == 1 && affected.Select(r => r.ContainerKey).Distinct().Count() == 1 && affected.Sum(r => Math.Max(0, Math.Min(start + length, r.Start + r.Length) - Math.Max(start, r.Start))) == length;
        }
        public string Apply(IEnumerable<TextEdit> edits)
        {
            var ordered = edits.OrderByDescending(e => e.Start).ToList();
            int boundary = Text.Length;
            foreach (var edit in ordered)
            {
                if (edit.Start + edit.Length > boundary || !CanEdit(edit.Start, edit.Length) || Text.Substring(edit.Start, edit.Length) != edit.Original)
                    throw new InvalidOperationException("Biçim sınırı veya korunan Word alanı aşılıyor.");
                boundary = edit.Start;
                var affected = runs.Where(r => edit.Start < r.Start + r.Length && edit.Start + edit.Length > r.Start).ToList();
                for (int i = affected.Count - 1; i >= 0; i--)
                {
                    var run = affected[i];
                    int lo = Math.Max(edit.Start, run.Start) - run.Start;
                    int hi = Math.Min(edit.Start + edit.Length, run.Start + run.Length) - run.Start;
                    run.Node.Value = run.Node.Value.Remove(lo, hi - lo).Insert(lo, i == 0 ? edit.Replacement : "");
                    run.Node.SetAttributeValue(XNamespace.Xml + "space", "preserve");
                }
            }
            return document.ToString(SaveOptions.DisableFormatting);
        }
    }
}
