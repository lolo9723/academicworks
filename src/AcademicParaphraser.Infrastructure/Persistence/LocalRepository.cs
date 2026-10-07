using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AcademicParaphraser.Core;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
namespace AcademicParaphraser.Infrastructure.Persistence
{
    public sealed class UserBackup
    {
        public int FormatVersion { get; set; } = 1; public UserSettings Settings { get; set; } = new UserSettings();
        public List<string> LockedTerms { get; set; } = new List<string>(); public List<RuleDefinition> Rules { get; set; } = new List<RuleDefinition>(); public List<LexiconEntry> Lexicon { get; set; } = new List<LexiconEntry>();
    }
    public sealed class HistoryEntry
    {
        public long Id
        {
            get; set;
        }
        public DateTime Timestamp
        {
            get; set;
        }
        public int Edits
        {
            get; set;
        }
        public string Original { get; set; } = ""; public string Result { get; set; } = "";
    }
    public interface ITextProtector
    {
        byte[] Protect(string text); string Unprotect(byte[] blob);
    }
    public sealed class LocalRepository : IRuleCatalog
    {
        private readonly string path; private readonly object sync = new object(); private readonly ITextProtector protector;
        public string DatabasePath => path;
        public LocalRepository(string path, ITextProtector protector)
        {
            this.path = path;
            this.protector = protector;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            Migrate();
        }
        private SqliteConnection Open()
        {
            var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate }.ToString());
            c.Open();
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "PRAGMA busy_timeout=5000; PRAGMA foreign_keys=ON;";
                cmd.ExecuteNonQuery();
            }
            return c;
        }
        private void Migrate()
        {
            lock (sync)
            {
                using (var c = Open())
                {
                    // Check an existing schema before executing any migration writes.
                    using (var schema = c.CreateCommand())
                    {
                        schema.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='migrations'";
                        if (Convert.ToInt32(schema.ExecuteScalar()) > 0)
                        {
                            schema.CommandText = "SELECT MAX(version) FROM migrations";
                            object? existing = schema.ExecuteScalar();
                            if (existing != null && existing != DBNull.Value && Convert.ToInt32(existing) > 1)
                                throw new InvalidOperationException("Veritabanı daha yeni bir sürüme ait; verileri korumak için açılmadı.");
                        }
                    }
                    using (var tx = c.BeginTransaction())
                    {
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "CREATE TABLE IF NOT EXISTS migrations(version INTEGER PRIMARY KEY); CREATE TABLE IF NOT EXISTS settings(id INTEGER PRIMARY KEY CHECK(id=1),json TEXT NOT NULL); CREATE TABLE IF NOT EXISTS rules(id TEXT PRIMARY KEY,json TEXT NOT NULL,user_defined INTEGER NOT NULL); CREATE TABLE IF NOT EXISTS lexicon(lemma TEXT PRIMARY KEY,json TEXT NOT NULL,user_defined INTEGER NOT NULL); CREATE TABLE IF NOT EXISTS locked_terms(term TEXT PRIMARY KEY); CREATE TABLE IF NOT EXISTS dictionary_cache(key TEXT PRIMARY KEY,json TEXT NOT NULL,expires TEXT NOT NULL); CREATE TABLE IF NOT EXISTS history(id INTEGER PRIMARY KEY AUTOINCREMENT,created TEXT NOT NULL,edits INTEGER NOT NULL,original BLOB NOT NULL,result BLOB NOT NULL); INSERT OR IGNORE INTO migrations VALUES(1);";
                            cmd.ExecuteNonQuery();
                            cmd.CommandText = "SELECT MAX(version) FROM migrations";
                            if (Convert.ToInt32(cmd.ExecuteScalar()) > 1)
                                throw new InvalidOperationException("Veritabanı daha yeni bir sürüme ait; verileri korumak için açılmadı.");
                        }
                        // Seed and schema commit together: avoid a separate disk flush for every default rule.
                        Seed<RuleDefinition>(c, tx, "rules", "rules.json", x => x.Id);
                        Seed<RuleDefinition>(c, tx, "rules", "rules.extended.json", x => x.Id);
                        Seed<RuleDefinition>(c, tx, "rules", "sentence-structures.json", x => x.Id);
                        Seed<LexiconEntry>(c, tx, "lexicon", "lexicon.json", x => x.Lemma);
                        tx.Commit();
                    }
                }
            }
        }
        private static void Seed<T>(SqliteConnection c, SqliteTransaction tx, string table, string resource, Func<T, string> key)
        {
            using (var stream = typeof(UserSettings).Assembly.GetManifestResourceStream("AcademicParaphraser.Core.Data." + resource) ?? throw new InvalidOperationException("Başlangıç veri kaynağı bulunamadı."))
            using (var reader = new StreamReader(stream))
            {
                foreach (var item in JsonConvert.DeserializeObject<List<T>>(reader.ReadToEnd())!)
                {
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT OR IGNORE INTO " + table + " VALUES($id,$json,0)";
                        cmd.Parameters.AddWithValue("$id", key(item));
                        cmd.Parameters.AddWithValue("$json", JsonConvert.SerializeObject(item));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        private List<T> Read<T>(string table)
        {
            lock (sync)
            {
                using (var c = Open())
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT json FROM " + table + " ORDER BY 1";
                    using (var r = cmd.ExecuteReader())
                    {
                        var list = new List<T>();
                        while (r.Read())
                            list.Add(JsonConvert.DeserializeObject<T>(r.GetString(0))!);
                        return list;
                    }
                }
            }
        }
        public IReadOnlyList<RuleDefinition> GetRules() => Read<RuleDefinition>("rules"); public IReadOnlyList<LexiconEntry> GetLexicon() => Read<LexiconEntry>("lexicon");
        public IReadOnlyList<string> GetLockedTerms()
        {
            lock (sync)
            {
                using (var c = Open())
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT term FROM locked_terms ORDER BY term";
                    using (var r = cmd.ExecuteReader())
                    {
                        var list = new List<string>();
                        while (r.Read())
                            list.Add(r.GetString(0));
                        return list;
                    }
                }
            }
        }
        public UserSettings GetSettings()
        {
            lock (sync)
            {
                using (var c = Open())
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT json FROM settings WHERE id=1";
                    return JsonConvert.DeserializeObject<UserSettings>((string?)cmd.ExecuteScalar() ?? "{}")!;
                }
            }
        }
        public static void ValidateSettings(UserSettings settings)
        {
            if (settings == null || settings.CustomProtectionPatterns == null || string.IsNullOrWhiteSpace(settings.Domain) || settings.Alternatives < 1 || settings.Alternatives > 5 || double.IsNaN(settings.MinimumConfidence) || settings.MinimumConfidence < .80 || settings.MinimumConfidence > 1 || !Enum.IsDefined(typeof(Strength), settings.DefaultStrength))
                throw new ArgumentException("Ayar değerleri geçersiz.");
            foreach (var p in settings.CustomProtectionPatterns)
                new Regex(p, RegexOptions.None, TimeSpan.FromMilliseconds(150));
        }
        public void SaveSettings(UserSettings settings)
        {
            ValidateSettings(settings);
            Execute("INSERT OR REPLACE INTO settings VALUES(1,$value)", ("$value", JsonConvert.SerializeObject(settings)));
        }
        public void LockTerm(string term)
        {
            term = term.Trim();
            if (term.Length < 2 || term.Length > 200 || term.Contains("\r") || term.Contains("\n"))
                throw new ArgumentException("Terim 2–200 karakter olmalı ve tek satırda bulunmalı.");
            Execute("INSERT OR IGNORE INTO locked_terms VALUES($term)", ("$term", term));
        }
        public void UnlockTerm(string term) => Execute("DELETE FROM locked_terms WHERE term=$term", ("$term", term));
        private static void ValidateRule(RuleDefinition rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Id) || string.IsNullOrWhiteSpace(rule.Pattern) || rule.Pattern.Length > 2000 || string.IsNullOrWhiteSpace(rule.Target) || rule.Target.Length > 2000 || rule.Target.Any(char.IsControl) || rule.ContextPattern == null || rule.RequiredMorphemes == null || rule.ForbiddenMorphemes == null || rule.PosConstraint == null || string.IsNullOrWhiteSpace(rule.Domain) || !Enum.IsDefined(typeof(Strength), rule.Strength) || double.IsNaN(rule.Confidence) || rule.Confidence < .80 || rule.Confidence > 1)
                throw new ArgumentException("Kural değerleri geçersiz.");
            var pattern = new Regex(rule.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(150));
            if (rule.GroupConstraints == null || rule.GroupConstraints.Any(c => c == null || string.IsNullOrWhiteSpace(c.Group) || c.Pos == null || c.Required == null || c.Any == null || c.Forbidden == null ||
                !pattern.GetGroupNames().Contains(c.Group) || c.Required.Concat(c.Any).Concat(c.Forbidden).Any(m => string.IsNullOrWhiteSpace(m))))
                throw new ArgumentException("Kural grup koşulları geçersiz.");
            if (pattern.IsMatch(""))
                throw new ArgumentException("Kural boş metinle eşleşmemeli.");
            new Regex(rule.ContextPattern, RegexOptions.None, TimeSpan.FromMilliseconds(150));
        }
        private static void ValidateLexicon(LexiconEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Lemma) || string.IsNullOrWhiteSpace(entry.Pos) || string.IsNullOrWhiteSpace(entry.Domain) || entry.Synonyms == null || entry.NearSynonyms == null || entry.ForbiddenReplacements == null || entry.AcademicAlternatives == null || entry.Examples == null || double.IsNaN(entry.Confidence) || entry.Confidence < .80 || entry.Confidence > 1)
                throw new ArgumentException("Sözlük kaydı geçersiz.");
        }
        public void SaveRule(RuleDefinition rule)
        {
            ValidateRule(rule);
            rule.UserDefined = true;
            Execute("INSERT OR REPLACE INTO rules VALUES($id,$json,1)", ("$id", rule.Id), ("$json", JsonConvert.SerializeObject(rule)));
        }
        public void RemoveRule(string id) => Execute("DELETE FROM rules WHERE id=$id AND user_defined=1", ("$id", id));
        public void SaveLexicon(LexiconEntry entry)
        {
            ValidateLexicon(entry);
            entry.UserDefined = true;
            Execute("INSERT OR REPLACE INTO lexicon VALUES($id,$json,1)", ("$id", entry.Lemma), ("$json", JsonConvert.SerializeObject(entry)));
        }
        public void RemoveLexicon(string lemma) => Execute("DELETE FROM lexicon WHERE lemma=$id AND user_defined=1", ("$id", lemma));
        public void CountUsage(IEnumerable<string> ids)
        {
            foreach (var id in ids.Distinct())
            {
                var rule = GetRules().FirstOrDefault(r => r.Id == id);
                if (rule == null)
                    continue;
                rule.UsageCount++;
                Execute("UPDATE rules SET json=$json WHERE id=$id", ("$id", id), ("$json", JsonConvert.SerializeObject(rule)));
            }
        }
        public void AddHistory(string original, string result, int edits)
        {
            Execute("INSERT INTO history(created,edits,original,result) VALUES($date,$edits,$old,$new)", ("$date", DateTime.UtcNow.ToString("O")), ("$edits", edits), ("$old", protector.Protect(original)), ("$new", protector.Protect(result)));
            Execute("DELETE FROM history WHERE id NOT IN(SELECT id FROM history ORDER BY id DESC LIMIT 200)");
        }
        public IReadOnlyList<HistoryEntry> GetHistory()
        {
            lock (sync)
            {
                using (var c = Open())
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT id,created,edits,original,result FROM history ORDER BY id DESC LIMIT 200";
                    using (var r = cmd.ExecuteReader())
                    {
                        var list = new List<HistoryEntry>();
                        while (r.Read())
                            list.Add(new HistoryEntry { Id = r.GetInt64(0), Timestamp = DateTime.Parse(r.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind), Edits = r.GetInt32(2), Original = protector.Unprotect((byte[])r[3]), Result = protector.Unprotect((byte[])r[4]) });
                        return list;
                    }
                }
            }
        }
        public void ClearHistory() => Execute("DELETE FROM history; VACUUM;");
        public string? GetCached(string key)
        {
            lock (sync)
            {
                using (var c = Open())
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT json FROM dictionary_cache WHERE key=$key AND expires>$now";
                    cmd.Parameters.AddWithValue("$key", key);
                    cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
                    return (string?)cmd.ExecuteScalar();
                }
            }
        }
        public void Cache(string key, string json, TimeSpan? lifetime = null) => Execute("INSERT OR REPLACE INTO dictionary_cache VALUES($key,$json,$expires)", ("$key", key), ("$json", json), ("$expires", DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromDays(30)).ToString("O")));
        public string Export()
        {
            return JsonConvert.SerializeObject(new UserBackup { Settings = GetSettings(), LockedTerms = GetLockedTerms().ToList(), Rules = GetRules().Where(r => r.UserDefined).ToList(), Lexicon = GetLexicon().Where(l => l.UserDefined).ToList() }, Formatting.Indented);
        }
        public void Import(string json)
        {
            if (json.Length > 2000000)
                throw new ArgumentException("Yedek dosyası çok büyük.");
            var backup = JsonConvert.DeserializeObject<UserBackup>(json) ?? throw new ArgumentException("Yedek okunamadı.");
            if (backup.FormatVersion != 1)
                throw new ArgumentException("Yedek sürümü desteklenmiyor.");
            if (backup.Settings == null || backup.Rules == null || backup.Lexicon == null || backup.LockedTerms == null)
                throw new ArgumentException("Yedek alanları eksik.");
            ValidateSettings(backup.Settings);
            foreach (var r in backup.Rules)
                ValidateRule(r);
            foreach (var l in backup.Lexicon)
                ValidateLexicon(l);
            foreach (var t in backup.LockedTerms)
                if (string.IsNullOrWhiteSpace(t) || t.Length > 200 || t.Contains("\n") || t.Contains("\r"))
                    throw new ArgumentException("Yedekte geçersiz terim var.");
            lock (sync)
            {
                using (var c = Open())
                using (var tx = c.BeginTransaction())
                {
                    void Write(string sql, params (string, object)[] values)
                    {
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = sql;
                            foreach (var v in values)
                                cmd.Parameters.AddWithValue(v.Item1, v.Item2);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    Write("INSERT OR REPLACE INTO settings VALUES(1,$json)", ("$json", JsonConvert.SerializeObject(backup.Settings)));
                    foreach (var t in backup.LockedTerms)
                        Write("INSERT OR IGNORE INTO locked_terms VALUES($term)", ("$term", t));
                    foreach (var r in backup.Rules)
                    {
                        r.UserDefined = true;
                        Write("INSERT OR REPLACE INTO rules VALUES($id,$json,1)", ("$id", r.Id), ("$json", JsonConvert.SerializeObject(r)));
                    }
                    foreach (var l in backup.Lexicon)
                    {
                        l.UserDefined = true;
                        Write("INSERT OR REPLACE INTO lexicon VALUES($id,$json,1)", ("$id", l.Lemma), ("$json", JsonConvert.SerializeObject(l)));
                    }
                    tx.Commit();
                }
            }
        }
        private void Execute(string sql, params (string, object)[] values)
        {
            lock (sync)
            {
                using (var c = Open())
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = sql;
                    foreach (var v in values)
                        cmd.Parameters.AddWithValue(v.Item1, v.Item2);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
