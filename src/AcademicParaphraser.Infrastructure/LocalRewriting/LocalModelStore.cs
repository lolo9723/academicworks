using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AcademicParaphraser.Infrastructure.LocalRewriting
{
    public sealed class LocalModelStore
    {
        public const string FileName = "Qwen3.5-9B-UD-IQ3_XXS.gguf";
        public const long Bytes = 4016235744;
        public const string Sha256 = "40d0f32cd3030b04f0784139a589fb63e876cfbf8667d56311b79783c74fd149";
        public const string Url = "https://huggingface.co/unsloth/Qwen3.5-9B-GGUF/resolve/3885219b6810b007914f3a7950a8d1b469d598a5/" + FileName;
        public string ModelPath { get; }
        public long ExpectedBytes { get; }
        public string ExpectedSha256 { get; }
        public string DownloadUrl { get; }
        public string ModelName { get; }
        private long verifiedWriteTicks;
        public LocalModelStore(string directory)
        {
            ModelPath = Path.Combine(directory, FileName);
            ExpectedBytes = Bytes; ExpectedSha256 = Sha256; DownloadUrl = Url;
            ModelName = "Qwen3.5-9B UD-IQ3_XXS";
        }
        public bool IsDownloaded => File.Exists(ModelPath) && new FileInfo(ModelPath).Length == ExpectedBytes;
        public async Task VerifyAsync(CancellationToken cancellation)
        {
            if (!IsDownloaded) throw new InvalidOperationException("Gelişmiş motor henüz indirilmedi. Önizleme panelindeki ‘Gelişmiş motoru indir’ düğmesini kullanın (bir defalık yaklaşık 4 GB). API anahtarı gerekmez.");
            var file = new FileInfo(ModelPath);
            if (verifiedWriteTicks == file.LastWriteTimeUtc.Ticks) return;
            string hash = await HashAsync(ModelPath, cancellation).ConfigureAwait(false);
            if (hash != ExpectedSha256) throw new InvalidOperationException("Model bütünlüğü doğrulanamadı. Model dosyasını kaldırıp gelişmiş motoru yeniden indirin.");
            verifiedWriteTicks = file.LastWriteTimeUtc.Ticks;
        }
        public async Task DownloadAsync(IProgress<string>? progress, CancellationToken cancellation)
        {
            if (IsDownloaded) { await VerifyAsync(cancellation).ConfigureAwait(false); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(ModelPath)!);
            string partial = ModelPath + ".partial";
            using (var handler = new HttpClientHandler { AllowAutoRedirect = true })
            using (var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan })
            using (var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 1048576, true))
                {
                    var buffer = new byte[1048576]; long total = 0; int read; DateTime last = DateTime.MinValue;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                    {
                        total += read;
                        if (total > ExpectedBytes) throw new InvalidOperationException("Model indirme boyutu beklenenden farklı.");
                        await output.WriteAsync(buffer, 0, read, cancellation).ConfigureAwait(false);
                        if ((DateTime.UtcNow - last).TotalSeconds >= 1) { progress?.Report($"Gelişmiş motor indiriliyor: {total / 1048576} / {ExpectedBytes / 1048576} MB. Metniniz gönderilmiyor."); last = DateTime.UtcNow; }
                    }
                    if (total != ExpectedBytes) throw new InvalidOperationException("İndirme tamamlanmadı. Düğmeyle yeniden deneyebilirsiniz.");
                }
            }
            progress?.Report("Model dosyası SHA256 ile doğrulanıyor…");
            if (await HashAsync(partial, cancellation).ConfigureAwait(false) != ExpectedSha256)
            { File.Delete(partial); throw new InvalidOperationException("İndirilen model SHA256 denetimini geçemedi."); }
            cancellation.ThrowIfCancellationRequested();
            if (File.Exists(ModelPath)) File.Delete(ModelPath);
            File.Move(partial, ModelPath);
        }
        private static async Task<string> HashAsync(string path, CancellationToken cancellation)
        {
            using (var hash = SHA256.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1048576, true))
            {
                var buffer = new byte[1048576]; int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0)
                    hash.TransformBlock(buffer, 0, read, buffer, 0);
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return BitConverter.ToString(hash.Hash!).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
