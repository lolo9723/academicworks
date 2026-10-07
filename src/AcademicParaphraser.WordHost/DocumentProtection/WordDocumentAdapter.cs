using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.DocumentProtection;
using Word = Microsoft.Office.Interop.Word;
namespace AcademicParaphraser.WordHost.DocumentProtection
{
    public sealed class SelectionSnapshot : IDisposable
    {
        internal Word.Range Native
        {
            get;
        }
        internal Word.Document Document
        {
            get;
        }
        internal OoxmlRunMap RunMap
        {
            get;
        }
        internal int XmlOffset
        {
            get;
        }
        public string Text
        {
            get;
        }
        public string Xml
        {
            get;
        }
        public IReadOnlyList<TextSpan> Protected
        {
            get;
        }
        internal SelectionSnapshot(Word.Range range, Word.Document document, string text, string xml, OoxmlRunMap map, int offset, List<TextSpan> spans)
        {
            Native = range;
            Document = document;
            Text = text;
            Xml = xml;
            RunMap = map;
            XmlOffset = offset;
            Protected = spans;
        }
        public IReadOnlyList<TextSpan> Writable => RunMap.EditableSpans
            .Where(s => s.Intersects(XmlOffset, Text.Length))
            .Select(s => new TextSpan { Start = Math.Max(s.Start, XmlOffset) - XmlOffset,
                Length = Math.Min(s.End, XmlOffset + Text.Length) - Math.Max(s.Start, XmlOffset) }).ToList();
        public bool CanEdit(TextEdit edit) => RunMap.CanEdit(XmlOffset + edit.Start, edit.Length);
        public void Dispose()
        {
            Release(Native);
            Release(Document);
        }
        internal static void Release(object? obj)
        {
            if (obj != null && Marshal.IsComObject(obj))
                Marshal.ReleaseComObject(obj);
        }
    }
    public sealed class WordDocumentAdapter
    {
        private readonly Word.Application app; private readonly int thread; private string? lastXml;
        public WordDocumentAdapter(Word.Application app)
        {
            this.app = app;
            thread = Thread.CurrentThread.ManagedThreadId;
        }
        private void AssertSta()
        {
            if (Thread.CurrentThread.ManagedThreadId != thread)
                throw new InvalidOperationException("Word işlemi yalnızca Word arayüz iş parçacığında çalışabilir.");
        }
        public SelectionSnapshot Capture(UserSettings settings)
        {
            AssertSta();
            lastXml = null;
            if (app.Documents.Count == 0)
                throw new InvalidOperationException("Önce bir Word belgesi açın.");
            var doc = app.ActiveDocument;
            if (doc.ReadOnly || doc.ProtectionType != Word.WdProtectionType.wdNoProtection)
            {
                SelectionSnapshot.Release(doc);
                throw new InvalidOperationException("Belge salt okunur veya korumalı. Düzenlenebilir bir kopya açın.");
            }
            var range = app.Selection.Range.Duplicate;
            try
            {
                string text = range.Text ?? "";
                if (string.IsNullOrWhiteSpace(text) || text.All(c => c == '\r' || c == '\a' || c == '\u0001'))
                    throw new InvalidOperationException("Parafraz için metin seçin; yalnızca görsel seçimi işlenmez.");
                string xml = range.WordOpenXML;
                var map = new OoxmlRunMap(xml, settings.PreserveLinks, StoryPart(range.StoryType));
                int offset = map.Text.IndexOf(text, StringComparison.Ordinal);
                if (offset < 0 || map.Text.IndexOf(text, offset + 1, StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("Bu seçimin metin/Word yapısı güvenle eşleştirilemedi. Daha küçük bir metin aralığı seçin.");
                var spans = map.ProtectedSpans.Select(s => new TextSpan { Start = Math.Max(0, s.Start - offset), Length = Math.Max(0, Math.Min(text.Length, s.End - offset) - Math.Max(0, s.Start - offset)), Reason = s.Reason }).Where(s => s.Length > 0).ToList();
                void Protect(Word.Range native, string reason)
                {
                    try
                    {
                        if (native.StoryType != range.StoryType || native.End <= range.Start || native.Start >= range.End)
                            return;
                        int start = Math.Max(native.Start, range.Start), end = Math.Min(native.End, range.End);
                        var before = range.Duplicate;
                        before.SetRange(range.Start, start);
                        int visibleStart = (before.Text ?? "").Length;
                        before.SetRange(start, end);
                        int length = (before.Text ?? "").Length;
                        SelectionSnapshot.Release(before);
                        if (length > 0)
                            spans.Add(new TextSpan { Start = visibleStart, Length = length, Reason = reason });
                    }
                    finally { SelectionSnapshot.Release(native); }
                }
                var fields = range.Fields;
                for (int i = 1; i <= fields.Count; i++)
                {
                    var field = fields[i];
                    if (field.Type != Word.WdFieldType.wdFieldHyperlink || settings.PreserveLinks)
                    {
                        Protect(field.Code.Duplicate, "alan kodu");
                        Protect(field.Result.Duplicate, "alan sonucu");
                    }
                    SelectionSnapshot.Release(field);
                }
                SelectionSnapshot.Release(fields);
                // Expand the actual story: Document.Fields alone omits footnotes and headers.
                var fullStory = range.Duplicate;
                fullStory.Expand(Word.WdUnits.wdStory);
                var documentFields = fullStory.Fields;
                for (int i = 1; i <= documentFields.Count; i++)
                {
                    var field = documentFields[i];
                    if (field.Type != Word.WdFieldType.wdFieldHyperlink || settings.PreserveLinks)
                        Protect(field.Result.Duplicate, "belge alanı");
                    SelectionSnapshot.Release(field);
                }
                SelectionSnapshot.Release(documentFields);
                var bookmarks = doc.Bookmarks;
                bool hiddenBookmarks = bookmarks.ShowHidden;
                try
                {
                    // Word normally hides _Ref/_Toc bookmarks; a partial selection may omit their OOXML markers.
                    bookmarks.ShowHidden = true;
                    for (int i = 1; i <= bookmarks.Count; i++)
                    {
                        var bookmark = bookmarks[i];
                        Protect(bookmark.Range.Duplicate, "yer işareti");
                        SelectionSnapshot.Release(bookmark);
                    }
                }
                finally { bookmarks.ShowHidden = hiddenBookmarks; SelectionSnapshot.Release(bookmarks); }
                var controls = fullStory.ContentControls;
                for (int i = 1; i <= controls.Count; i++)
                {
                    var control = controls[i];
                    Protect(control.Range.Duplicate, "içerik denetimi");
                    SelectionSnapshot.Release(control);
                }
                SelectionSnapshot.Release(controls);
                SelectionSnapshot.Release(fullStory);
                var revisions = range.Revisions;
                for (int i = 1; i <= revisions.Count; i++)
                {
                    var revision = revisions[i];
                    Protect(revision.Range.Duplicate, "mevcut değişiklik");
                    SelectionSnapshot.Release(revision);
                }
                SelectionSnapshot.Release(revisions);
                return new SelectionSnapshot(range, doc, text, xml, map, offset, spans);
            }
            catch { SelectionSnapshot.Release(range); SelectionSnapshot.Release(doc); throw; }
        }
        private static string? StoryPart(Word.WdStoryType type)
        {
            switch (type)
            {
                case Word.WdStoryType.wdFootnotesStory:
                    return "/word/footnotes.xml";
                case Word.WdStoryType.wdEndnotesStory:
                    return "/word/endnotes.xml";
                case Word.WdStoryType.wdPrimaryHeaderStory:
                case Word.WdStoryType.wdEvenPagesHeaderStory:
                case Word.WdStoryType.wdFirstPageHeaderStory:
                    return "/word/header";
                case Word.WdStoryType.wdPrimaryFooterStory:
                case Word.WdStoryType.wdEvenPagesFooterStory:
                case Word.WdStoryType.wdFirstPageFooterStory:
                    return "/word/footer";
                default:
                    return null;
            }
        }
        private Word.Range Locate(SelectionSnapshot snapshot, TextEdit edit)
        {
            var search = snapshot.Native.Duplicate;
            int start = search.Start, end = search.End;
            string needle = edit.Original.Substring(0, Math.Min(200, edit.Original.Length));
            while (start < end)
            {
                search.SetRange(start, end);
                var find = search.Find;
                find.ClearFormatting();
                bool found = find.Execute(FindText: needle, MatchCase: true, MatchWholeWord: false, MatchWildcards: false, Forward: true, Wrap: Word.WdFindWrap.wdFindStop, Format: false);
                SelectionSnapshot.Release(find);
                if (!found)
                    break;
                var prefix = snapshot.Native.Duplicate;
                prefix.SetRange(snapshot.Native.Start, search.Start);
                int index = (prefix.Text ?? "").Length;
                SelectionSnapshot.Release(prefix);
                if (index == edit.Start)
                {
                    int actualStart = search.Start;
                    search.SetRange(actualStart, actualStart + edit.Length);
                    if (search.Text == edit.Original)
                        return search;
                    break;
                }
                start = search.Start + 1;
            }
            SelectionSnapshot.Release(search);
            throw new InvalidOperationException("Dönüşüm konumu Word içinde doğrulanamadı; metin değişmedi.");
        }
        private static string[] Links(Word.Document doc, Word.Range selected)
        {
            var values = new List<string>();
            void Add(Word.Hyperlinks links)
            {
                for (int i = 1; i <= links.Count; i++)
                {
                    var link = links[i];
                    values.Add((link.Address ?? "") + "#" + (link.SubAddress ?? ""));
                    SelectionSnapshot.Release(link);
                }
                SelectionSnapshot.Release(links);
            }
            Add(doc.Hyperlinks);
            Add(selected.Hyperlinks);
            return values.OrderBy(v => v, StringComparer.Ordinal).ToArray();
        }
        public void Apply(SelectionSnapshot snapshot, Candidate candidate, bool trackChanges)
        {
            AssertSta();
            if (snapshot.Native.Text != snapshot.Text || snapshot.Native.WordOpenXML != snapshot.Xml)
                throw new InvalidOperationException("Seçim veya biçimi işlem sırasında değişti. Yeniden parafraz üretin.");
            if (snapshot.Document.ReadOnly || snapshot.Document.ProtectionType != Word.WdProtectionType.wdNoProtection)
                throw new InvalidOperationException("Belge artık düzenlenebilir durumda değil.");
            var targets = new List<(TextEdit Edit, Word.Range Range, Word.Font Font)>();
            try
            {
                foreach (var edit in candidate.Edits.OrderByDescending(e => e.Start))
                {
                    if (!snapshot.CanEdit(edit) || snapshot.Protected.Any(p => p.Intersects(edit.Start, edit.Length)))
                        throw new InvalidOperationException("Korunan alan veya biçim sınırı değiştirilmeye çalışıldı.");
                    var target = Locate(snapshot, edit);
                    targets.Add((edit, target, target.Font.Duplicate));
                }
                if (targets.Count == 0)
                    return;
                string[] beforeLinks = Links(snapshot.Document, snapshot.Native);
                bool originalTrack = snapshot.Document.TrackRevisions;
                bool changed = false, failed = false;
                var undo = app.UndoRecord;
                undo.StartCustomRecord("Akademik Parafraz");
                try
                {
                    if (trackChanges)
                        snapshot.Document.TrackRevisions = true;
                    foreach (var item in targets)
                    {
                        item.Range.Text = item.Edit.Replacement;
                        changed = true;
                        item.Range.Font = item.Font;
                    }
                    if (!beforeLinks.SequenceEqual(Links(snapshot.Document, snapshot.Native)))
                        throw new InvalidOperationException("Bağlantı hedefleri değişti; işlem geri alındı.");
                    // Include the user's restored tracking setting in the undo guard signature.
                    snapshot.Document.TrackRevisions = originalTrack;
                    lastXml = DocumentSignature(snapshot.Document);
                }
                catch { failed = true; throw; }
                finally { snapshot.Document.TrackRevisions = originalTrack; undo.EndCustomRecord(); SelectionSnapshot.Release(undo); if (failed && changed) { object times = 1; snapshot.Document.Undo(ref times); lastXml = null; } }
            }
            finally { foreach (var item in targets) { SelectionSnapshot.Release(item.Font); SelectionSnapshot.Release(item.Range); } }
        }
        private static string DocumentSignature(Word.Document document)
        {
            var content = document.Content;
            try
            {
                using (var hash = System.Security.Cryptography.SHA256.Create())
                    return Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content.WordOpenXML)));
            }
            finally { SelectionSnapshot.Release(content); }
        }
        public void Undo(SelectionSnapshot? snapshot)
        {
            AssertSta();
            if (lastXml == null || snapshot == null)
                throw new InvalidOperationException("Geri alınabilecek bir eklenti işlemi yok. Word’ün Ctrl+Z komutunu kullanabilirsiniz.");
            if (DocumentSignature(snapshot.Document) != lastXml)
                throw new InvalidOperationException("Son parafrazdan sonra başka düzenlemeler yapıldı. Word’ün Ctrl+Z geçmişini kullanın.");
            object count = 1;
            snapshot.Document.Undo(ref count);
            lastXml = null;
        }
        public string SelectedText()
        {
            AssertSta();
            return (app.Selection.Text ?? "").Trim();
        }
    }
}
