using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AcademicParaphraser.Core;
using AcademicParaphraser.Infrastructure.Persistence;
namespace AcademicParaphraser.WordHost.UI
{
    public sealed class SettingsForm : Form
    {
        private readonly LocalRepository repo; private readonly UserSettings settings; private readonly PropertyGrid grid; private readonly TextBox patterns;
        public SettingsForm(LocalRepository repo, string diagnostics)
        {
            this.repo = repo;
            settings = repo.GetSettings();
            Text = "Akademik Parafraz — Ayarlar";
            Width = 720;
            Height = 640;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9);
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var general = new TabPage("Genel");
            grid = new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = new SettingsView(settings) };
            general.Controls.Add(grid);
            tabs.TabPages.Add(general);
            var protection = new TabPage("Özel regex korumaları");
            patterns = new TextBox { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, Text = string.Join(Environment.NewLine, settings.CustomProtectionPatterns) };
            protection.Controls.Add(patterns);
            tabs.TabPages.Add(protection);
            var diag = new TabPage("Tanılama");
            diag.Controls.Add(new TextBox { ReadOnly = true, Multiline = true, Dock = DockStyle.Fill, Text = diagnostics, ScrollBars = ScrollBars.Vertical });
            tabs.TabPages.Add(diag);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45 };
            var save = new Button { Text = "Kaydet", AutoSize = true };
            save.Click += (s, e) => Run(() => { settings.CustomProtectionPatterns = patterns.Lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToList(); repo.SaveSettings(settings); DialogResult = DialogResult.OK; Close(); });
            var export = new Button { Text = "Yedek dışa aktar", AutoSize = true };
            export.Click += (s, e) => Run(() => { using (var d = new SaveFileDialog { Filter = "JSON yedeği|*.json", FileName = "akademik-parafraz-yedek.json" }) if (d.ShowDialog(this) == DialogResult.OK) File.WriteAllText(d.FileName, repo.Export(), System.Text.Encoding.UTF8); });
            var import = new Button { Text = "Yedek içe aktar", AutoSize = true };
            import.Click += (s, e) => Run(() => { using (var d = new OpenFileDialog { Filter = "JSON yedeği|*.json" }) if (d.ShowDialog(this) == DialogResult.OK) { repo.Import(File.ReadAllText(d.FileName)); MessageBox.Show("Yedek birleştirildi. Ayarları yeniden açın."); Close(); } });
            buttons.Controls.AddRange(new Control[] { save, export, import });
            Controls.Add(tabs);
            Controls.Add(buttons);
        }
        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex) { MessageBox.Show(this, ex is ArgumentException || ex is IOException || ex is Newtonsoft.Json.JsonException ? "İşlem tamamlanamadı: " + ex.Message : "Ayar işlemi tamamlanamadı. Veritabanını ve eklenti kurulumunu kontrol edin.", "Akademik Parafraz", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
    }
    public sealed class SettingsView
    {
        private readonly UserSettings s; public SettingsView(UserSettings settings)
        {
            s = settings;
        }
        [System.ComponentModel.DisplayName("Varsayılan parafraz düzeyi")]
        public Strength Düzey
        {
            get => s.DefaultStrength; set => s.DefaultStrength = value;
        }
        public bool AtıflarıKoru
        {
            get => s.PreserveCitations; set => s.PreserveCitations = value;
        }
        public bool SayılarıKoru
        {
            get => s.PreserveNumbers; set => s.PreserveNumbers = value;
        }
        public bool ÖzelİsimleriKoru
        {
            get => s.PreserveNames; set => s.PreserveNames = value;
        }
        public bool TeknikTerimleriKoru
        {
            get => s.PreserveTechnicalTerms; set => s.PreserveTechnicalTerms = value;
        }
        public bool BağlantılarıKoru
        {
            get => s.PreserveLinks; set => s.PreserveLinks = value;
        }
        [System.ComponentModel.DisplayName("İnternet erişimini tamamen kapat")]
        public bool İnternetiKapat
        {
            get => !s.InternetEnabled; set => s.InternetEnabled = !value;
        }
        public bool ÇevrimdışıMod
        {
            get => s.OfflineMode; set => s.OfflineMode = value;
        }
        public bool Değişiklikleriİzle
        {
            get => s.TrackChanges; set => s.TrackChanges = value;
        }
        public bool SözlükÖnbelleği
        {
            get => s.CacheDictionary; set => s.CacheDictionary = value;
        }
        public int AlternatifSayısı
        {
            get => s.Alternatives; set => s.Alternatives = value;
        }
        public double MinimumKuralGüveni
        {
            get => s.MinimumConfidence; set => s.MinimumConfidence = value;
        }
        [System.ComponentModel.TypeConverter(typeof(DomainConverter))]
        public string AkademikAlan
        {
            get => s.Domain; set => s.Domain = value;
        }
    }
}
