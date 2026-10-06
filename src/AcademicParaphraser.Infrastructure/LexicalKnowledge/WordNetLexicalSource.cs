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
        private static bool Neutral(IEnumerable<string> definitions) => !definitions.Any(d => Regex.IsMatch(d, @"\b(?:argo|eskimiş|eski dil|halk ağzı|hakaret|obsolete|archaic|slang|vulgar)\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)));
        public async Task<IReadOnlyList<LexiconEntry>> FindAsync(string text, IReadOnlyList<MorphToken> words, UserSettings settings, CancellationToken cancellation, EnrichmentContext? context = null)
        {
            context = context ?? new EnrichmentContext();
            var summary = context.Summary;
            var entries = new List<LexiconEntry>();
            var contexts = words.GroupBy(w => ParagraphStart(text, w.Start)).ToDictionary(g => g.Key, g => new HashSet<string>(g.Where(w => !Stop.Contains(w.Lemma)).Select(w => w.Lemma.ToLower(Turkish)), StringComparer.Ordinal));
            var locations = words.GroupBy(w => w.Lemma + "/" + w.Pos).ToDictionary(g => g.Key, g => g.Select(w => ParagraphStart(text, w.Start)).Distinct().Count());
            var roots = words.Where(w => !w.Proper && w.Lemma.Length >= 2 && w.Lemma.Length <= 100 && w.Lemma.All(char.IsLetter)).GroupBy(w => w.Lemma + "/" + w.Pos).Select(g => g.First()).ToList();
            summary.RootsTotal = roots.Count;
            using (var connection = Open())
            {
                var cache = new Dictionary<string, List<Sense>>(StringComparer.Ordinal);
                List<Sense> Resolve(string lemma, string pos)
                {
                    string key = lemma.ToLower(Turkish) + "/" + pos;
                    if (!cache.TryGetValue(key, out var value)) { value = Read(connection, lemma.ToLower(Turkish), pos); cache[key] = value; }
                    return value;
                }
                foreach (var word in roots)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var senses = Resolve(word.Lemma, Pos(word.Pos));
                    var paragraph = contexts[ParagraphStart(text, word.Start)];
                    var scored = senses.Select(s => new { Sense = s, Score = Overlap(s.Definition, paragraph) }).OrderByDescending(s => s.Score).ThenBy(s => s.Sense.Id, StringComparer.Ordinal).ToList();
                    bool oneContext = locations[word.Lemma + "/" + word.Pos] == 1;
                    Sense? chosen = senses.Count == 1 ? senses[0] : oneContext && scored.Count > 1 && scored[0].Score >= 2 && scored[0].Score - scored[1].Score >= 2 ? scored[0].Sense : null;
                    var alternatives = chosen != null && Word(word.Lemma) && Neutral(new[] { chosen.Definition })
                        ? chosen.Members.Where(v => v != word.Lemma.ToLower(Turkish) && Word(v))
                            .Where(v => Resolve(v, chosen.Pos).Count == 1 && Neutral(Resolve(v, chosen.Pos).Select(s => s.Definition)))
                            .OrderByDescending(Preferred.Contains).ThenBy(v => v, StringComparer.Ordinal).Take(3).ToList()
                        : new List<string>();
                    bool found = senses.Count > 0;
                    // No per-selection three-word or 200-root cutoff. Every eligible unresolved
                    // root is examined; bounded requests, a circuit breaker and user cancel govern time.
                    if (alternatives.Count == 0 && online != null && settings.InternetEnabled && !settings.OfflineMode)
                    {
                        context.Report("İnternet sözlüğü: " + (summary.RootsChecked + 1) + "/" + roots.Count + " uygun kök · " + word.Lemma);
                        summary.OnlineRootsQueried++;
                        var results = await online.LookupAsync(word.Lemma, settings, cancellation).ConfigureAwait(false);
                        summary.ProviderErrors += results.Count(r => r.Error.Length > 0 && !r.NotFound);
                        var usable = results.Where(r => r.Error.Length == 0 && r.Content.Length > 0).ToList();
                        if (usable.Count > 0) { summary.OnlineRootsFound++; found = true; }
                        if (Word(word.Lemma) && oneContext)
                        foreach (var result in usable.Where(r => r.SingleSense && r.PosTags.Count == 1 && r.PosTags[0] == word.Pos && r.Definitions.Count == 1 && Neutral(r.Definitions)))
                        foreach (string value in result.Synonyms.Select(v => v.ToLower(Turkish)).Where(v => Word(v) && v != word.Lemma.ToLower(Turkish)).Distinct().Take(3))
                        {
                            cancellation.ThrowIfCancellationRequested();
                            var targets = Resolve(value, Pos(word.Pos));
                            bool sameSense = targets.Count == 1 && senses.Any(s => s.Id == targets[0].Id) && Neutral(targets.Select(s => s.Definition));
                            if (!sameSense && senses.Count == 0)
                            {
                                // Truly absent local word: require explicit reciprocal synonyms,
                                // one meaning and matching POS from the reverse dictionary entry.
                                var reverse = await online.LookupAsync(value, settings, cancellation).ConfigureAwait(false);
                                summary.ProviderErrors += reverse.Count(r => r.Error.Length > 0 && !r.NotFound);
                                sameSense = reverse.Any(r => r.Error.Length == 0 && r.SingleSense && r.PosTags.Count == 1 && r.PosTags[0] == word.Pos && r.Definitions.Count == 1 && Neutral(r.Definitions) && r.Synonyms.Any(v => v.ToLower(Turkish) == word.Lemma.ToLower(Turkish)));
                            }
                            if (sameSense && !alternatives.Contains(value)) alternatives.Add(value);
                        }
                    }
                    if (alternatives.Count > 0)
                        entries.Add(new LexiconEntry { Lemma = word.Lemma, Pos = word.Pos, Synonyms = alternatives.Take(3).ToList(), Domain = "genel", Confidence = chosen != null && senses.Count == 1 ? .94 : .93, Examples = chosen == null ? new List<string>() : new List<string> { chosen.Definition } });
                    summary.RootsChecked++;
                    if (found) summary.RootsFound++; else summary.RootsUnresolved++;
                    context.Report("Sözlük taraması: " + summary.RootsChecked + "/" + roots.Count + " uygun kök · bulunan " + summary.RootsFound);
                }
            }
            summary.LexicalEntries = entries.Count;
            return entries;
        }
        private static int ParagraphStart(string text, int position)
        {
            while (position > 0 && text[position - 1] != '\r' && text[position - 1] != '\n' && text[position - 1] != '\a') position--;
            return position;
        }
    }
}
