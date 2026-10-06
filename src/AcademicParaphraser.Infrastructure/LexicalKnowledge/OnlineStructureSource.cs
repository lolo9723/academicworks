using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.Rewriting;
using AcademicParaphraser.Infrastructure.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AcademicParaphraser.Infrastructure.LexicalKnowledge
{
    public sealed class OnlineStructureSource : IStructureSource, IDisposable
    {
        private readonly LocalRepository repository;
        private readonly HttpClient http;
        private readonly bool ownsClient;
        private readonly JObject pin;
        private string? verified;
        public OnlineStructureSource(LocalRepository repository, HttpClient? http = null)
        {
            this.repository = repository;
            ownsClient = http == null;
            this.http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            if (ownsClient) this.http.DefaultRequestHeaders.UserAgent.ParseAdd("AkademikParafraz/1.2 (versioned structure bank)");
            using (var stream = typeof(UserSettings).Assembly.GetManifestResourceStream("AcademicParaphraser.Core.Data.internet-structures-source.json")!)
            using (var reader = new StreamReader(stream, Encoding.UTF8)) pin = JObject.Parse(reader.ReadToEnd());
        }
        public void Dispose() { if (ownsClient) http.Dispose(); }
        private bool Verify(string content)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            if (bytes.Length > pin.Value<int>("maxBytes")) return false;
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() == pin.Value<string>("sha256");
        }
        public async Task<IReadOnlyList<RuleDefinition>> FindAsync(string text, UserSettings settings, CancellationToken cancellation, EnrichmentContext context)
        {
            string key = "structures:" + pin.Value<string>("sha256");
            string? content = verified ?? repository.GetCached(key);
            if (content != null && !Verify(content)) content = null;
            string status = content == null ? "kapalı" : "doğrulanmış önbellek";
            if (content == null && settings.InternetEnabled && !settings.OfflineMode)
            {
                context.Report("İnternetten cümle yapı bankası alınıyor…");
                try
                {
                    // Fixed HTTPS URL, no text, token, lemma, or query is attached to this request.
                    using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
                    {
                        timeout.CancelAfter(TimeSpan.FromSeconds(20));
                        using (var response = await http.GetAsync(pin.Value<string>("url"), HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();
                            if (response.Content.Headers.ContentLength > pin.Value<int>("maxBytes")) throw new InvalidOperationException("Yapı bankası boyut sınırını aştı.");
                            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            using (var body = new MemoryStream())
                            {
                                var buffer = new byte[4096]; int count;
                                while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                                {
                                    if (body.Length + count > pin.Value<int>("maxBytes")) throw new InvalidOperationException("Yapı bankası boyut sınırını aştı.");
                                    body.Write(buffer, 0, count);
                                }
                                content = Encoding.UTF8.GetString(body.ToArray());
                            }
                        }
                    }
                    if (!Verify(content)) { context.Summary.StructureStatus = "kaynak özeti uyuşmadı; kullanılmadı"; return Array.Empty<RuleDefinition>(); }
                    if (settings.CacheDictionary) repository.Cache(key, content);
                    status = "internet: SHA256 doğrulandı";
                }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { status = "kaynak zaman aşımı; yerel motor devam ediyor"; }
                catch (Exception ex) when (ex is HttpRequestException || ex is IOException || ex is InvalidOperationException || ex is JsonException) { status = "kaynağa ulaşılamadı; yerel motor devam ediyor"; }
            }
            context.Summary.StructureStatus = status;
            if (content == null || !Verify(content)) return Array.Empty<RuleDefinition>();
            verified = content;
            var bank = JObject.Parse(content);
            if (bank.Value<int>("formatVersion") != 1 || bank.Value<int>("inventory") != pin.Value<int>("inventory")) throw new InvalidOperationException("Yapı bankasının biçimi desteklenmiyor.");
            context.Summary.StructureInventory = bank.Value<int>("inventory");
            string lower = text.ToLower(CultureInfo.GetCultureInfo("tr-TR"));
            var rules = new List<RuleDefinition>();
            // Expand only frames which occur in this selection, rather than allocating 25,104 regexes.
            foreach (string intro in bank["intros"]!.Values<string>().Select(v => v!))
            foreach (string location in bank["locations"]!.Values<string>().Select(v => v!))
            foreach (string subject in bank["subjects"]!.Values<string>().Select(v => v!))
            {
                cancellation.ThrowIfCancellationRequested();
                string prefix = intro + " " + location + " " + subject + " ";
                if (lower.IndexOf(prefix, StringComparison.Ordinal) < 0) continue;
                foreach (var predicate in bank["predicates"]!)
                {
                    var ends = predicate["sourceEndings"]!.Values<string>().Select(v => v!).ToArray();
                    var targets = predicate["targetEndings"]!.Values<string>().Select(v => v!).ToArray();
                    for (int i = 0; i < ends.Length; i++)
                    {
                        string source = prefix + predicate.Value<string>("source") + ends[i];
                        if (lower.IndexOf(source, StringComparison.Ordinal) < 0) continue;
                        Add(rules, @"\b" + Regex.Escape(source) + @"\b", subject + " " + intro + " " + location + " " + predicate.Value<string>("target") + targets[i], "güvenli sıralama");
                    }
                }
            }
            foreach (string intro in bank["intros"]!.Values<string>().Select(v => v!))
            foreach (var purpose in bank["purposes"]!)
            foreach (string noun in bank["purposeNouns"]!.Values<string>().Select(v => v!))
            {
                string prefix = intro + " " + purpose.Value<string>("genitive") + " " + noun;
                if (lower.IndexOf(prefix, StringComparison.Ordinal) < 0) continue;
                foreach (var ending in bank["purposeEndings"]!)
                    Add(rules, @"\b" + Regex.Escape(prefix) + @",? (?<goal>[^.!?;\r\n\a]{3,220})" + ending.Value<string>("source") + @"\b",
                        intro + " " + purpose.Value<string>("subject") + ", ${goal}" + ending.Value<string>("target") + (noun == "temel amacı" ? " temel olarak" : "") + " amaçlamaktadır", "yüklem merkezli");
            }
            context.Summary.ApplicableStructures = rules.Count;
            return rules;
        }
        private static void Add(List<RuleDefinition> rules, string pattern, string target, string family)
        {
            using (var sha = SHA256.Create())
                rules.Add(new RuleDefinition { Id = "online-frame:" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(pattern + "\n" + target))).Replace("-", "").Substring(0, 16), Pattern = pattern, Target = target, Family = family, Strength = Strength.Strong, Confidence = .95 });
        }
    }
}
