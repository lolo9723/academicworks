using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AcademicParaphraser.Infrastructure.InternetDictionaryProviders
{
    // A term-information fallback, not a thesaurus or a paraphrase equivalence source.
    public sealed class WikidataTermProvider : IFallbackDictionaryProvider, IDisposable
    {
        private readonly HttpClient http;
        private readonly bool ownsClient;
        public string Name => "Wikidata — terim bilgisi";
        public WikidataTermProvider() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(12) }, true) { }
        public WikidataTermProvider(HttpClient client) : this(client, false) { }
        private WikidataTermProvider(HttpClient client, bool owns)
        {
            http = client; ownsClient = owns;
            if (owns) http.DefaultRequestHeaders.UserAgent.ParseAdd("AkademikParafraz/1.2 (optional single-word term lookup)");
        }
        public void Dispose() { if (ownsClient) http.Dispose(); }
        public async Task<DictionaryResult> LookupAsync(string term, CancellationToken cancellation)
        {
            var result = new DictionaryResult { Term = term, Source = Name, License = "CC0 — Wikidata structured data", SourceUrl = "https://www.wikidata.org" };
            string url = "https://www.wikidata.org/w/api.php?action=wbsearchentities&format=json&language=tr&uselang=tr&type=item&limit=3&search=" + Uri.EscapeDataString(term);
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(12));
                using (var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentLength > 500000) throw new InvalidOperationException("Terim yanıtı çok büyük.");
                    string json;
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var body = new MemoryStream())
                    {
                        var buffer = new byte[4096]; int count;
                        while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                        {
                            if (body.Length + count > 500000) throw new InvalidOperationException("Terim yanıtı çok büyük.");
                            body.Write(buffer, 0, count);
                        }
                        json = Encoding.UTF8.GetString(body.ToArray());
                    }
                    var data = JObject.Parse(json);
                    if (data["error"] != null) { result.Error = "Terim kaynağı isteği tamamlayamadı."; return result; }
                    var turkish = CultureInfo.GetCultureInfo("tr-TR");
                    var matches = (data["search"] as JArray ?? new JArray()).Where(item => item["match"]?.Value<string>("language") == "tr"
                        && string.Equals(item["match"]?.Value<string>("text")?.ToLower(turkish), term.ToLower(turkish), StringComparison.Ordinal)
                        && Regex.IsMatch(item.Value<string>("id") ?? "", @"^Q[1-9][0-9]*$")).Take(3).ToList();
                    if (matches.Count == 0) { result.NotFound = true; result.Error = "Türkçe terim için tam eşleşme bulunamadı."; return result; }
                    result.SourceUrl = "https://www.wikidata.org/wiki/" + matches[0].Value<string>("id");
                    result.Content = string.Join(Environment.NewLine, matches.Select(m => (m.Value<string>("label") ?? term) + " — " + (m.Value<string>("description") ?? "Açıklama yok; kaynak kaydına bakın.")));
                    if (result.Content.Length > 3000) result.Content = result.Content.Substring(0, 3000);
                    // Keep Synonyms empty, SingleSense false and POS absent: encyclopedia
                    // descriptions, English labels and fuzzy results never become substitutions.
                    return result;
                }
            }
        }
    }
}
