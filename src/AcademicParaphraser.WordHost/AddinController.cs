using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AcademicParaphraser.Core;
using AcademicParaphraser.Core.RuleEngine;
using AcademicParaphraser.Infrastructure.Diagnostics;
using AcademicParaphraser.Infrastructure.InternetDictionaryProviders;
using AcademicParaphraser.Infrastructure.Persistence;
using AcademicParaphraser.Infrastructure.TurkishNlp;
using AcademicParaphraser.WordHost.DocumentProtection;
using AcademicParaphraser.WordHost.UI;
using Word = Microsoft.Office.Interop.Word;
namespace AcademicParaphraser.WordHost
{
    public sealed class AddinController : IDisposable
    {
        private readonly Word.Application app; private readonly WordDocumentAdapter adapter; private readonly LocalRepository repo; private readonly ZemberekProcess nlp; private readonly TransformationEngine engine; private readonly SafeLog log; private readonly DictionaryService dictionary; private readonly List<WiktionaryProvider> providers;
        private SelectionSnapshot? snapshot; private IReadOnlyList<Candidate> candidates = new List<Candidate>(); private int alternative; private CancellationTokenSource? operation; private bool busy, disposed;
        public PreviewPane Preview { get; } public Action? ShowPreview
        {
            get; set;
        }
        public event EventHandler? StateChanged;
        public AddinController(Word.Application app, string installDirectory) : this(app, installDirectory, null) { }
        public AddinController(Word.Application app, string installDirectory, Action<string>? startupProgress)
        {
            startupProgress?.Invoke("PREVIEW");
            Preview = new PreviewPane();
            this.app = app;
            adapter = new WordDocumentAdapter(app);
            startupProgress?.Invoke("DATA_DIRECTORY");
            string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AkademikParafraz");
            log = new SafeLog(Path.Combine(data, "logs"));
            startupProgress?.Invoke("DATABASE");
            repo = new LocalRepository(Path.Combine(data, "academic.sqlite"), new WindowsTextProtector());
            startupProgress?.Invoke("NLP_CONFIGURATION");
            nlp = new ZemberekProcess(Path.Combine(installDirectory, "runtime", "java", "bin", "java.exe"), Path.Combine(installDirectory, "nlp", "turkish-nlp-1.0.0.jar"));
            engine = new TransformationEngine(repo, nlp);
            startupProgress?.Invoke("DICTIONARY");
            providers = new List<WiktionaryProvider> { new TurkishWiktionaryProvider(), new EnglishWiktionaryProvider() };
            dictionary = new DictionaryService(repo, providers);
            Preview.ApplyRequested += (s, e) => Guard(Apply);
            Preview.NextRequested += (s, e) => Guard(() => Next(1));
            Preview.CancelRequested += (s, e) => { operation?.Cancel(); candidates = new List<Candidate>(); Preview.ShowMessage("İşlem iptal edildi; belge değişmedi."); StateChanged?.Invoke(this, EventArgs.Empty); };
        }
        public UserSettings Settings => repo.GetSettings(); public bool Busy => busy;
        public bool HasProposals => candidates.Count > 0;
        public string ApplyForAutomation()
        {
            if (!HasProposals)
                return "no_proposal";
            try
            {
                Apply();
                return "applied";
            }
            catch (Exception ex) { log.Write("AUTOMATION_APPLY_FAILED", ex); return "error"; }
        }
        public void SetStrength(Strength value)
        {
            var settings = Settings;
            settings.DefaultStrength = value;
            repo.SaveSettings(settings);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        public void SetOption(string id, bool value)
        {
            var s = Settings;
            switch (id)
            {
                case "citations":
                    s.PreserveCitations = value;
                    break;
                case "numbers":
                    s.PreserveNumbers = value;
                    break;
                case "names":
                    s.PreserveNames = value;
                    break;
                case "technical":
                    s.PreserveTechnicalTerms = value;
                    break;
                case "links":
                    s.PreserveLinks = value;
                    break;
                case "tracking":
                    s.TrackChanges = value;
                    break;
            }
            repo.SaveSettings(s);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        public async Task GenerateAsync()
        {
            if (busy || disposed)
                return;
            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            busy = true;
            candidates = new List<Candidate>();
            ShowPreview?.Invoke();
            Preview.SetBusy(true);
            operation = new CancellationTokenSource();
            StateChanged?.Invoke(this, EventArgs.Empty);
            try
            {
                snapshot?.Dispose();
                snapshot = adapter.Capture(Settings);
                var current = snapshot;
                var settings = Settings;
                var token = operation.Token;
                var result = await Task.Run(() => engine.GenerateAsync(current.Text, settings, current.Protected, token), token);
                if (disposed)
                    return;
                token.ThrowIfCancellationRequested();
                // Word formatting constraints are checked again after linguistic generation, before any write.
                candidates = result.Select(c => { var edits = c.Edits.Where(current.CanEdit).ToList(); return new Candidate { Text = EditApplication.Apply(current.Text, edits), Edits = edits, Score = c.Score }; }).Where(c => c.Edits.Count > 0).GroupBy(c => c.Text, StringComparer.Ordinal).Select(g => g.First()).ToList();
                alternative = 0;
                if (candidates.Count == 0)
                    Preview.ShowMessage("Bu seçim için anlamı ve biçimi güvenle koruyan dönüşüm bulunamadı. Metin olduğu gibi bırakıldı.");
                else
                    Display();
            }
            catch (OperationCanceledException) { if (!disposed) Preview.ShowMessage("İşlem iptal edildi; belge değişmedi."); }
            catch (Exception ex) { log.Write("GENERATE_FAILED", ex); if (!disposed) ShowError(ex, "Parafraz işlemi tamamlanamadı. Metin değişmedi."); }
            finally { busy = false; if (!disposed) { Preview.SetBusy(false); if (candidates.Count == 0) Preview.ShowMessage("Uygulanabilir öneri yok; belge değişmedi."); else Display(); StateChanged?.Invoke(this, EventArgs.Empty); } operation?.Dispose(); operation = null; }
        }
        public void Next(int step)
        {
            if (candidates.Count == 0)
                return;
            alternative = (alternative + step + candidates.Count) % candidates.Count;
            Display();
            ShowPreview?.Invoke();
        }
        private void Display()
        {
            if (snapshot != null && candidates.Count > 0)
                Preview.ShowCandidate(snapshot.Text, candidates[alternative], alternative, candidates.Count);
        }
        public void Apply()
        {
            if (busy || snapshot == null || candidates.Count == 0)
                return;
            var chosen = candidates[alternative];
            adapter.Apply(snapshot, chosen, Settings.TrackChanges);
            try
            {
                repo.AddHistory(snapshot.Text, chosen.Text, chosen.Edits.Count);
                repo.CountUsage(chosen.Edits.Select(e => e.RuleId));
            }
            catch (Exception ex) { log.Write("HISTORY_SAVE_FAILED", ex); }
            candidates = new List<Candidate>();
            Preview.ShowMessage("Parafraz uygulandı. Word’de tek Ctrl+Z ile geri alınabilir.");
        }
        public void Undo() => Guard(() => { adapter.Undo(snapshot); candidates = new List<Candidate>(); Preview.ShowMessage("Son parafraz geri alındı."); });
        public void LockSelected() => Guard(() => { repo.LockTerm(adapter.SelectedText()); Preview.ShowMessage("Seçili terim kişisel koruma sözlüğüne eklendi."); }); public void UnlockSelected() => Guard(() => repo.UnlockTerm(adapter.SelectedText()));
        public void OpenSettings() => Guard(() => { string diagnostics = "Word: " + app.Version + Environment.NewLine + "Office işlemi: " + (Environment.Is64BitProcess ? "64 bit" : "32 bit") + Environment.NewLine + "Eklenti: 1.0.0" + Environment.NewLine + "NLP: " + nlp.Status + Environment.NewLine + "Veritabanı: hazır (migration 1)" + Environment.NewLine + "Kurallar: " + repo.GetRules().Count + Environment.NewLine + "Sözlük: " + repo.GetLexicon().Count + Environment.NewLine + "İnternet: " + (Settings.InternetEnabled && !Settings.OfflineMode ? "isteğe bağlı açık" : "kapalı"); using (var form = new SettingsForm(repo, diagnostics)) form.ShowDialog(); StateChanged?.Invoke(this, EventArgs.Empty); });
        public void OpenDictionary() => Guard(() => { using (var form = new DictionaryForm(repo, dictionary)) form.ShowDialog(); }); public void OpenHistory() => Guard(() => { using (var form = new HistoryForm(repo)) form.ShowDialog(); });
        public void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex) { log.Write("COMMAND_FAILED", ex); ShowError(ex, "İşlem tamamlanamadı. Belgeyi ve eklenti ayarlarını kontrol edin."); }
        }
        private static void ShowError(Exception ex, string fallback)
        {
            string text = ex is InvalidOperationException || ex is ArgumentException ? ex.Message : fallback;
            MessageBox.Show(text, "Akademik Parafraz", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            operation?.Cancel();
            snapshot?.Dispose();
            nlp.Dispose();
            foreach (var provider in providers)
                provider.Dispose();
            Preview.Dispose();
        }
    }
}
