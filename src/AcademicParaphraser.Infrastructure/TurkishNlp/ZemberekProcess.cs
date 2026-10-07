using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AcademicParaphraser.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace AcademicParaphraser.Infrastructure.TurkishNlp
{
    public sealed class ZemberekProcess : ITurkishNlp, ITurkishStructuralInflector
    {
        private readonly string executable, jar; private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1); private Process? process; private StreamWriter? input; private bool disposed;
        public long WorkingSetBytes
        {
            get { try { var child = process; if (child == null || child.HasExited) return 0; child.Refresh(); return child.WorkingSet64; } catch (InvalidOperationException) { return 0; } }
        }
        public string Status { get; private set; } = "Başlatılmadı";
        public ZemberekProcess(string executable, string jar)
        {
            this.executable = executable;
            this.jar = jar;
        }
        private async Task StartAsync(CancellationToken cancellation)
        {
            if (process != null && !process.HasExited)
                return;
            if (!File.Exists(executable) || !File.Exists(jar))
                throw new InvalidOperationException("Yerel NLP bileşeni bulunamadı. Kurulumu onarın.");
            process = new Process { StartInfo = new ProcessStartInfo(executable, "-Dfile.encoding=UTF-8 -Xmx512m -jar \"" + jar + "\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 } };
            process.ErrorDataReceived += (s, e) => { };
            process.Start();
            // Framework's default stdin writer can use a Windows code page. Never lose Turkish letters.
            input = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false), 4096, true);
            process.BeginErrorReadLine();
            var startup = process.StandardOutput.ReadLineAsync();
            var timeout = Task.Delay(TimeSpan.FromSeconds(35), cancellation);
            if (await Task.WhenAny(startup, timeout).ConfigureAwait(false) != startup)
            {
                Kill();
                cancellation.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Türkçe NLP motoru zamanında başlayamadı.");
            }
            var ready = JObject.Parse(await startup.ConfigureAwait(false) ?? "{}");
            if (ready.Value<bool?>("ready") != true)
            {
                Kill();
                throw new InvalidOperationException("Türkçe NLP motoru başlatılamadı.");
            }
            Status = "Zemberek 0.17.1 — yerel, hazır";
        }
        private async Task<JObject> RequestAsync(object request, CancellationToken cancellation)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ZemberekProcess));
            await gate.WaitAsync(cancellation).ConfigureAwait(false);
            try
            {
                await StartAsync(cancellation).ConfigureAwait(false);
                cancellation.ThrowIfCancellationRequested();
                await input!.WriteLineAsync(JsonConvert.SerializeObject(request)).ConfigureAwait(false);
                await input.FlushAsync().ConfigureAwait(false);
                var read = process!.StandardOutput.ReadLineAsync();
                var timeout = Task.Delay(TimeSpan.FromSeconds(120), cancellation);
                if (await Task.WhenAny(read, timeout).ConfigureAwait(false) != read)
                {
                    Kill();
                    cancellation.ThrowIfCancellationRequested();
                    throw new InvalidOperationException("Yerel NLP işlemi zaman aşımına uğradı.");
                }
                var result = JObject.Parse(await read.ConfigureAwait(false) ?? "{}");
                if (result["error"] != null)
                    throw new InvalidOperationException("Türkçe çözümleme tamamlanamadı; metin değişmedi.");
                return result;
            }
            catch { if (process == null || process.HasExited) Status = "NLP kullanılamıyor"; throw; }
            finally { gate.Release(); }
        }
        public async Task<IReadOnlyList<MorphToken>> AnalyzeAsync(string text, CancellationToken cancellation)
        {
            var result = await RequestAsync(new
            {
                op = "analyze",
                text
            }, cancellation).ConfigureAwait(false);
            return result["tokens"]?.ToObject<List<MorphToken>>() ?? throw new InvalidOperationException("NLP yanıtı geçersiz.");
        }
        public async Task<string?> InflectAsync(string source, string targetLemma, string pos, CancellationToken cancellation)
        {
            var result = await RequestAsync(new
            {
                op = "inflect",
                source,
                target = targetLemma,
                pos
            }, cancellation).ConfigureAwait(false);
            return result.Value<string>("surface");
        }
        public async Task<string?> TransformStructureAsync(string source, string mode, CancellationToken cancellation)
        {
            if (mode != "Gen" && mode != "Passive" && mode != "Nominal") throw new ArgumentException("Yapısal çekim türü geçersiz.");
            var result = await RequestAsync(new { op = "structure", source, mode }, cancellation).ConfigureAwait(false);
            return result.Value<string>("surface");
        }
        public async Task<string?> TransformStructureAsync(MorphToken source, string mode, CancellationToken cancellation)
        {
            if (mode != "Gen" && mode != "Passive" && mode != "Nominal") throw new ArgumentException("Yapısal çekim türü geçersiz.");
            if (source.Proper) return null;
            var result = await RequestAsync(new { op = "structure", source = source.Surface, mode, lemma = source.Lemma, morphemes = source.Morphemes }, cancellation).ConfigureAwait(false);
            return result.Value<string>("surface");
        }
        private void Kill()
        {
            try
            {
                if (process != null && !process.HasExited)
                    process.Kill();
            }
            catch (InvalidOperationException) { }
            finally { input = null; process?.Dispose(); process = null; Status = "Durduruldu"; }
        }
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            try
            {
                if (process != null && !process.HasExited)
                {
                    input?.WriteLine("{\"op\":\"shutdown\"}");
                    input?.Flush();
                    if (!process.WaitForExit(2000))
                        process.Kill();
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is IOException) { }
            finally { Kill(); }
        }
    }
}
