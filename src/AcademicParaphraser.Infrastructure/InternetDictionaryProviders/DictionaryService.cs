using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using AcademicParaphraser.Infrastructure.Persistence;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace AcademicParaphraser.Infrastructure.InternetDictionaryProviders
{
    public sealed class DictionaryResult
    {
        public string Term { get; set; } = ""; public string Source { get; set; } = ""; public string SourceUrl { get; set; } = ""; public string Content { get; set; } = ""; public string License { get; set; } = "CC BY-SA — kaynak sayfasının lisansına bakın"; public bool Cached
        {
            get; set;
        }
        public string Error { get; set; } = "";
    }
    public interface IDictionaryProvider
    {
        string Name
        {
            get;
        }
        Task<DictionaryResult> LookupAsync(string term, CancellationToken cancellation);
    }
    public abstract class WiktionaryProvider : IDictionaryProvider, IDisposable
    {
        private readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) }; private readonly string language;
        public string Name => language == "tr" ? "Vikisözlük" : "Wiktionary";
        protected WiktionaryProvider(string language)
        {
            this.language = language;
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AkademikParafraz/1.0 (local Word dictionary; user-initiated lookup)");
        }
        public async Task<DictionaryResult> LookupAsync(string term, CancellationToken cancellation)
        {
            string source = "https://" + language + ".wiktionary.org/wiki/" + Uri.EscapeDataString(term);
            string url = "https://" + language + ".wiktionary.org/w/api.php?action=parse&prop=text&format=json&formatversion=2&redirects=1&page=" + Uri.EscapeDataString(term);
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                timeout.CancelAfter(TimeSpan.FromSeconds(12));
                using (var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentLength > 2000000)
                        throw new InvalidOperationException("Sözlük yanıtı çok büyük.");
                    string json;
                    // ResponseHeadersRead alone does not bound the body or its timeout on Framework.
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var body = new MemoryStream())
                    {
                        var buffer = new byte[8192];
                        int count;
                        while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                        {
                            if (body.Length + count > 2000000)
                                throw new InvalidOperationException("Sözlük yanıtı çok büyük.");
                            body.Write(buffer, 0, count);
                        }
                        json = Encoding.UTF8.GetString(body.ToArray());
                    }
                    timeout.Token.ThrowIfCancellationRequested();
                    var data = JObject.Parse(json);
                    if (data["error"] != null)
                        return new DictionaryResult { Term = term, Source = Name, SourceUrl = source, Error = "Bu kaynakta kelime bulunamadı." };
                    var html = new HtmlDocument();
                    html.LoadHtml(data["parse"]?.Value<string>("text") ?? "");
                    foreach (var n in html.DocumentNode.SelectNodes("//script|//style|//table|//sup") ?? new HtmlNodeCollection(null))
                        n.Remove();
                    var nodes = html.DocumentNode.SelectNodes("//h2|//h3|//h4|//li|//p");
                    string content = string.Join(Environment.NewLine, (nodes ?? new HtmlNodeCollection(null)).Select(n => HtmlEntity.DeEntitize(n.InnerText).Trim()).Where(s => s.Length > 0).Distinct());
                    if (content.Length > 6000)
                        content = content.Substring(0, 6000);
                    return new DictionaryResult { Term = term, Source = Name, SourceUrl = source, Content = content };
                }
            }
        }
        public void Dispose() => http.Dispose();
    }
    public sealed class TurkishWiktionaryProvider : WiktionaryProvider
    {
        public TurkishWiktionaryProvider() : base("tr") { }
    }
    public sealed class EnglishWiktionaryProvider : WiktionaryProvider
    {
        public EnglishWiktionaryProvider() : base("en") { }
    }
    public sealed class DictionaryService
    {
        private readonly LocalRepository repository; private readonly IReadOnlyList<IDictionaryProvider> providers; private readonly SemaphoreSlim limiter = new SemaphoreSlim(1, 1);
        public DictionaryService(LocalRepository repository, IReadOnlyList<IDictionaryProvider> providers)
        {
            this.repository = repository;
            this.providers = providers;
        }
        public async Task<IReadOnlyList<DictionaryResult>> LookupAsync(string term, UserSettings settings, CancellationToken cancellation)
        {
            term = term.Trim();
            if (term.Length < 2 || term.Length > 100 || term.Any(c => !char.IsLetter(c) && c != '-' && c != '\'' && c != '’'))
                throw new ArgumentException("Sözlük için yalnızca tek bir kelime girin. Belge metni internet kaynağına gönderilmez.");
            var results = new List<DictionaryResult>();
            foreach (var provider in providers)
            {
                string key = provider.Name + ":" + term;
                string? cached = repository.GetCached(key);
                if (cached != null)
                {
                    var item = JsonConvert.DeserializeObject<DictionaryResult>(cached)!;
                    item.Cached = true;
                    results.Add(item);
                    continue;
                }
                if (!settings.InternetEnabled || settings.OfflineMode)
                {
                    results.Add(new DictionaryResult { Term = term, Source = provider.Name, Error = "Çevrimdışı: bu kelime henüz önbellekte yok." });
                    continue;
                }
                await limiter.WaitAsync(cancellation).ConfigureAwait(false);
                try
                {
                    var result = await provider.LookupAsync(term, cancellation).ConfigureAwait(false);
                    results.Add(result);
                    if (settings.CacheDictionary && result.Error.Length == 0)
                        repository.Cache(key, JsonConvert.SerializeObject(result));
                    await Task.Delay(250, cancellation).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { results.Add(new DictionaryResult { Term = term, Source = provider.Name, Error = "Sözlük kaynağı zaman aşımına uğradı; yerel parafraz çalışmaya devam eder." }); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (ex is HttpRequestException || ex is IOException || ex is InvalidOperationException || ex is JsonException) { results.Add(new DictionaryResult { Term = term, Source = provider.Name, Error = "Sözlük kaynağına ulaşılamadı; yerel parafraz çalışmaya devam eder." }); }
                finally { limiter.Release(); }
            }
            return results;
        }
    }
}
