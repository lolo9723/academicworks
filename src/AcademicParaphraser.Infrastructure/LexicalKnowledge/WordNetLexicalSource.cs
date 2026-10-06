using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Rewriting;
using AcademicParaphraser.Infrastructure.InternetDictionaryProviders;
using Microsoft.Data.Sqlite;

namespace AcademicParaphraser.Infrastructure.LexicalKnowledge
{
    public sealed class WordNetLexicalSource : ILexicalSource
    {
        private readonly string path;
        private readonly DictionaryService? online;
        private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
        private static readonly HashSet<string> Stop = new HashSet<string>(new[] { "bir", "bu", "şu", "o", "ile", "ve", "veya", "için", "gibi", "en", "çok", "daha", "az", "her", "hiç", "kadar", "da", "de", "olarak", "olan", "olmak", "etmek", "yapmak", "kullanmak", "aynı", "var", "yok", "ise" }, StringComparer.Ordinal);
        // Broad lexical coverage does not mean arbitrary synonym replacement. Prefer a
        // neutral academic register; historical/colloquial members remain dictionary information.
        private static readonly HashSet<string> Preferred = new HashSet<string>(("etmen kriter gereksinim ihtiyaç katılımcı birey kişi toplum inceleme değerlendirme çözümleme bulgu sonuç yöntem yaklaşım araştırma çalışma düzey seviye olanak imkân ortam çevre etki neden süreç bağlam koşul şart yarar fayda veri bilgi özellik nitelik benzerlik farklılık bağlantı ilişki görüş düşünce amaç hedef kanıt gösterge öngörü tanımlamak saptamak belirlemek incelemek değerlendirmek açıklamak göstermek desteklemek sağlamak sunmak oluşturmak artırmak azaltmak kapsamak içermek önemli temel genel farklı benzer çeşitli olası muhtemel uygun belirgin kapsamlı sınırlı bütünsel gözlem konuk ziyaretçi").Split(' '), StringComparer.Ordinal);
        private sealed class Sense
        {
            public string Id = "", Pos = "", Definition = "";
            public List<string> Members = new List<string>();
        }
        public WordNetLexicalSource(string databasePath, DictionaryService? online = null)
        {
            path = databasePath;
            this.online = online;
            if (!File.Exists(path)) throw new InvalidOperationException("Geniş sözlük dosyası bulunamadı. Yeni kurulum paketini yeniden çalıştırın.");
        }
        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Cache = SqliteCacheMode.Private }.ToString());
            connection.Open();
            using (var command = connection.CreateCommand()) { command.CommandText = "PRAGMA cache_size=-4096"; command.ExecuteNonQuery(); }
            return connection;
        }
        public string Describe()
        {
            using (var connection = Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT value FROM metadata WHERE key='lemmas'";
                return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) + " kelime / KeNet — yerel ve indeksli";
            }
        }
        private static string Pos(string pos) => pos == "Noun" ? "n" : pos == "Verb" ? "v" : pos == "Adjective" ? "a" : pos == "Adverb" ? "b" : "";
        private static List<Sense> Read(SqliteConnection connection, string lemma, string pos)
        {
            var senses = new List<Sense>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT DISTINCT s.id,s.pos,s.definition FROM members m JOIN senses s ON s.id=m.sense_id WHERE m.lemma=$lemma AND s.pos=$pos ORDER BY s.id";
                command.Parameters.AddWithValue("$lemma", lemma); command.Parameters.AddWithValue("$pos", pos);
                using (var reader = command.ExecuteReader()) while (reader.Read()) senses.Add(new Sense { Id = reader.GetString(0), Pos = reader.GetString(1), Definition = reader.GetString(2) });
            }
            foreach (var sense in senses)
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT DISTINCT lemma FROM members WHERE sense_id=$id ORDER BY lemma"; command.Parameters.AddWithValue("$id", sense.Id);
                    using (var reader = command.ExecuteReader()) while (reader.Read()) sense.Members.Add(reader.GetString(0));
                }
            return senses;
        }
        private static bool Word(string value) => value.Length >= 3 && value.Length <= 40 && value.All(char.IsLetter) && !Stop.Contains(value);
        private static int Overlap(string definition, HashSet<string> context) => Regex.Matches(definition.ToLower(Turkish), @"\p{L}{3,}").Cast<Match>().Select(m => m.Value).Distinct().Count(context.Contains);
        public async Task<IReadOnlyList<LexiconEntry>> FindAsync(string text, IReadOnlyList<MorphToken> words, UserSettings settings, CancellationToken cancellation)
        {
            var entries = new List<LexiconEntry>();
            var contexts = words.Where(w => !Stop.Contains(w.Lemma)).GroupBy(w => ParagraphStart(text, w.Start)).ToDictionary(g => g.Key, g => new HashSet<string>(g.Select(w => w.Lemma.ToLower(Turkish)), StringComparer.Ordinal));
            var pending = new List<(MorphToken Word, List<Sense> Senses)>();
            var locations = words.GroupBy(w => w.Lemma + "/" + w.Pos).ToDictionary(g => g.Key, g => g.Select(w => ParagraphStart(text, w.Start)).Distinct().Count());
            using (var connection = Open())
            {
                foreach (var word in words.Where(w => !w.Proper && Word(w.Lemma)).GroupBy(w => w.Lemma + "/" + w.Pos).Select(g => g.First()).Take(200))
                {
                    cancellation.ThrowIfCancellationRequested();
                    var senses = Read(connection, word.Lemma.ToLower(Turkish), Pos(word.Pos));
                    var context = contexts[ParagraphStart(text, word.Start)];
                    var scored = senses.Select(s => new { Sense = s, Score = Overlap(s.Definition, context) }).OrderByDescending(s => s.Score).ThenBy(s => s.Sense.Id, StringComparer.Ordinal).ToList();
                    Sense? chosen = senses.Count == 1 ? senses[0] : locations[word.Lemma + "/" + word.Pos] == 1 && scored.Count > 1 && scored[0].Score >= 2 && scored[0].Score - scored[1].Score >= 2 ? scored[0].Sense : null;
                    if (chosen == null) { if (senses.Count > 0 && locations[word.Lemma + "/" + word.Pos] == 1) pending.Add((word, senses)); continue; }
                    if (Regex.IsMatch(chosen.Definition, @"\b(?:argo|eskimiş|eski dil|halk ağzı|hakaret)\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100))) continue;
                    var alternatives = chosen.Members.Where(v => v != word.Lemma.ToLower(Turkish) && Word(v) && Preferred.Contains(v))
                        .Where(v => Read(connection, v, chosen.Pos).Count == 1).Take(3).ToList();
                    if (alternatives.Count > 0) entries.Add(new LexiconEntry { Lemma = word.Lemma, Pos = word.Pos, Synonyms = alternatives, Domain = "genel", Confidence = senses.Count == 1 ? .94 : .93, Examples = new List<string> { chosen.Definition } });
                }
            }
            // Explicitly enabled Internet enrichment asks only distinct lemma words, never paragraphs.
            // A web synonym must also share an existing WordNet sense and have one unambiguous target sense.
            if (online != null && settings.InternetEnabled && !settings.OfflineMode)
            using (var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                budget.CancelAfter(TimeSpan.FromSeconds(8));
                foreach (var item in pending.Take(3))
                {
                    try
                    {
                        var results = await online.LookupAsync(item.Word.Lemma, settings, budget.Token).ConfigureAwait(false);
                        var suggestions = results.Where(r => r.Error.Length == 0 && r.SingleSense).SelectMany(r => r.Synonyms).Distinct().Where(v => Word(v) && Preferred.Contains(v)).Take(6).ToList();
                        if (suggestions.Count == 0) continue;
                        using (var connection = Open())
                        {
                            var validated = suggestions.Where(v => {
                                var senses = Read(connection, v.ToLower(Turkish), Pos(item.Word.Pos));
                                return senses.Count == 1 && item.Senses.Any(s => s.Id == senses[0].Id);
                            }).Take(3).ToList();
                            if (validated.Count > 0) entries.Add(new LexiconEntry { Lemma = item.Word.Lemma, Pos = item.Word.Pos, Synonyms = validated, Domain = "genel", Confidence = .93 });
                        }
                    }
                    catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { break; }
                }
            }
            return entries;
        }
        private static int ParagraphStart(string text, int position)
        {
            while (position > 0 && text[position - 1] != '\r' && text[position - 1] != '\n' && text[position - 1] != '\a') position--;
            return position;
        }
    }
}
