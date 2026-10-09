using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AcademicParaphraser.Infrastructure.LocalRewriting
{
    public enum LocalModelProfile { Instruct4B, Hybrid4B, Hybrid9B }
    public sealed class LocalModelStore
    {
        public const string FileName = "Qwen3-4B-Instruct-2507-Q4_K_M.gguf";
        public const long Bytes = 2497281120;
        public const string Sha256 = "3605803b982cb64aead44f6c1b2ae36e3acdb41d8e46c8a94c6533bc4c67e597";
        public const string Url = "https://huggingface.co/unsloth/Qwen3-4B-Instruct-2507-GGUF/resolve/a06e946bb6b655725eafa393f4a9745d460374c9/" + FileName;
        public string ModelPath { get; }
        public long ExpectedBytes { get; }
        public string ExpectedSha256 { get; }
        public string DownloadUrl { get; }
        public string ModelName { get; }
        private long verifiedWriteTicks;
        public LocalModelStore(string directory,LocalModelProfile profile=LocalModelProfile.Instruct4B)
        {
            if(!Enum.IsDefined(typeof(LocalModelProfile),profile))throw new ArgumentException("Model seçimi geçersiz.");
            string file=profile==LocalModelProfile.Hybrid9B ? "Qwen3.5-9B-UD-IQ3_XXS.gguf" : profile==LocalModelProfile.Hybrid4B ? "Qwen3.5-4B-Q4_K_M.gguf" : FileName;
            ModelPath = Path.Combine(directory, file);
            ExpectedBytes = profile==LocalModelProfile.Hybrid9B ? 4016235744 : profile==LocalModelProfile.Hybrid4B ? 2740937888 : Bytes;
            ExpectedSha256 = profile==LocalModelProfile.Hybrid9B ? "40d0f32cd3030b04f0784139a589fb63e876cfbf8667d56311b79783c74fd149" : profile==LocalModelProfile.Hybrid4B ? "00fe7986ff5f6b463e62455821146049db6f9313603938a70800d1fb69ef11a4" : Sha256;
            DownloadUrl = profile==LocalModelProfile.Hybrid9B ? "https://huggingface.co/unsloth/Qwen3.5-9B-GGUF/resolve/3885219b6810b007914f3a7950a8d1b469d598a5/"+file : profile==LocalModelProfile.Hybrid4B ? "https://huggingface.co/unsloth/Qwen3.5-4B-GGUF/resolve/e87f176479d0855a907a41277aca2f8ee7a09523/"+file : Url;
            ModelName = profile==LocalModelProfile.Hybrid9B ? "Qwen3.5-9B UD-IQ3_XXS" : profile==LocalModelProfile.Hybrid4B ? "Qwen3.5-4B Q4_K_M" : "Qwen3-4B-Instruct-2507 Q4_K_M";
        }
        public bool IsDownloaded => File.Exists(ModelPath) && new FileInfo(ModelPath).Length == ExpectedBytes;
        public async Task VerifyAsync(CancellationToken cancellation)
        {
            if (!IsDownloaded) throw new InvalidOperationException("Gelişmiş motor henüz indirilmedi. Önizleme panelindeki ‘Gelişmiş motoru indir’ düğmesini kullanın (bir defalık yaklaşık " + (ExpectedBytes/1000000000.0).ToString("0.0",System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + " GB). API anahtarı gerekmez.");
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
