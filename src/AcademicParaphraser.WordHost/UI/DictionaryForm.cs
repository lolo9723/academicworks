using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using AcademicParaphraser.Core;
using AcademicParaphraser.Infrastructure.InternetDictionaryProviders;
using AcademicParaphraser.Infrastructure.Persistence;
namespace AcademicParaphraser.WordHost.UI
{
    public sealed class DictionaryForm : Form
    {
        private readonly LocalRepository repo; private readonly DictionaryService service; private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        public DictionaryForm(LocalRepository repo, DictionaryService service)
        {
            this.repo = repo;
            this.service = service;
            Text = "Akademik Parafraz — Sözlük ve Kurallar";
            Width = 820;
            Height = 680;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9);
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var terms = new TabPage("Kilitli terimler");
            var list = new ListBox { Dock = DockStyle.Fill };
            void Reload()
            {
                list.Items.Clear();
                list.Items.AddRange(repo.GetLockedTerms().Cast<object>().ToArray());
            }
            Reload();
            var entry = new TextBox { Width = 340 };
            var add = new Button { Text = "Terim ekle", AutoSize = true };
            var remove = new Button { Text = "Kilidi kaldır", AutoSize = true };
            add.Click += (s, e) => Run(() => { repo.LockTerm(entry.Text); Reload(); });
            remove.Click += (s, e) => Run(() => { if (list.SelectedItem is string term) { repo.UnlockTerm(term); Reload(); } });
            var controls = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45 };
            controls.Controls.AddRange(new Control[] { entry, add, remove });
            terms.Controls.Add(list);
            terms.Controls.Add(controls);
            tabs.TabPages.Add(terms);
            var rules = new TabPage("Dönüşüm kuralları");
            var rulesList = new ListBox { Dock = DockStyle.Left, Width = 260 };
            var ruleGrid = new PropertyGrid { Dock = DockStyle.Fill };
            void ReloadRules()
            {
                rulesList.Items.Clear();
                rulesList.Items.AddRange(repo.GetRules().Select(r => new RuleItem(r)).Cast<object>().ToArray());
            }
            ReloadRules();
            rulesList.SelectedIndexChanged += (s, e) => { if (rulesList.SelectedItem is RuleItem item) ruleGrid.SelectedObject = new RuleView(item.Rule); };
            var rb = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45 };
            var create = new Button { Text = "Yeni kural", AutoSize = true };
            create.Click += (s, e) => { ruleGrid.SelectedObject = new RuleView(new RuleDefinition { Id = "user-" + Guid.NewGuid().ToString("N"), Family = "kişisel dönüşüm", UserDefined = true }); };
            var rs = new Button { Text = "Kuralı kaydet", AutoSize = true };
            rs.Click += (s, e) => Run(() => { if (ruleGrid.SelectedObject is RuleView view) { repo.SaveRule(view.Rule); ReloadRules(); } });
            var rd = new Button { Text = "Özel kuralı sil", AutoSize = true };
            rd.Click += (s, e) => Run(() => { if (ruleGrid.SelectedObject is RuleView view) { repo.RemoveRule(view.Rule.Id); ReloadRules(); } });
            rb.Controls.AddRange(new Control[] { create, rs, rd });
            rules.Controls.Add(ruleGrid);
            rules.Controls.Add(rulesList);
            rules.Controls.Add(rb);
            tabs.TabPages.Add(rules);
            var lex = new TabPage("Yerel sözcükler");
            var lexList = new ListBox { Dock = DockStyle.Left, Width = 200 };
            var lexGrid = new PropertyGrid { Dock = DockStyle.Fill };
            void ReloadLex()
            {
                lexList.Items.Clear();
                lexList.Items.AddRange(repo.GetLexicon().Select(l => new LexItem(l)).Cast<object>().ToArray());
            }
            ReloadLex();
            lexList.SelectedIndexChanged += (s, e) => { if (lexList.SelectedItem is LexItem item) lexGrid.SelectedObject = new LexiconView(item.Entry); };
            var lb = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 45 };
            var lc = new Button { Text = "Yeni sözcük", AutoSize = true };
            lc.Click += (s, e) => lexGrid.SelectedObject = new LexiconView(new LexiconEntry { UserDefined = true });
            var ls = new Button { Text = "Sözcüğü kaydet", AutoSize = true };
            ls.Click += (s, e) => Run(() => { if (lexGrid.SelectedObject is LexiconView view) { repo.SaveLexicon(view.Entry); ReloadLex(); } });
            var ld = new Button { Text = "Özel sözcüğü sil", AutoSize = true };
            ld.Click += (s, e) => Run(() => { if (lexGrid.SelectedObject is LexiconView view) { repo.RemoveLexicon(view.Entry.Lemma); ReloadLex(); } });
            lb.Controls.AddRange(new Control[] { lc, ls, ld });
            lex.Controls.Add(lexGrid);
            lex.Controls.Add(lexList);
            lex.Controls.Add(lb);
            tabs.TabPages.Add(lex);
            var online = new TabPage("İsteğe bağlı internet sözlüğü");
            var word = new TextBox { Width = 280 };
            var lookup = new Button { Text = "Kelimeyi ara", AutoSize = true };
            var info = new Label { AutoSize = true, Text = "Yalnızca yazdığınız kelime kaynaklara gönderilir. İnternet varsayılan olarak kapalıdır." };
            var content = new RichTextBox { ReadOnly = true, Dock = DockStyle.Fill, DetectUrls = true };
            var ob = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80 };
            ob.Controls.AddRange(new Control[] { info, word, lookup });
            lookup.Click += async (s, e) => { lookup.Enabled = false; try { var results = await service.LookupAsync(word.Text, repo.GetSettings(), cancellation.Token); if (IsDisposed) return; content.Text = string.Join(Environment.NewLine + Environment.NewLine, results.Select(r => r.Source + (r.Cached ? " (önbellek)" : "") + Environment.NewLine + r.SourceUrl + Environment.NewLine + (r.Error.Length > 0 ? r.Error : r.Content) + Environment.NewLine + r.License)); } catch (OperationCanceledException) { } catch (Exception) { if (!IsDisposed) content.Text = "Kelime sorgusu tamamlanamadı. Yerel parafraz kullanılabilir."; } finally { if (!IsDisposed) lookup.Enabled = true; } };
            online.Controls.Add(content);
            online.Controls.Add(ob);
            tabs.TabPages.Add(online);
            Controls.Add(tabs);
            FormClosed += (s, e) => cancellation.Cancel();
        }
        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex) { MessageBox.Show(this, ex is ArgumentException ? ex.Message : "Sözlük işlemi tamamlanamadı. Veritabanını ve eklenti kurulumunu kontrol edin.", "Akademik Parafraz"); }
        }
        private sealed class RuleItem
        {
            public RuleDefinition Rule
            {
                get;
            }
            public RuleItem(RuleDefinition r)
            {
                Rule = r;
            }
            public override string ToString() => Rule.Id + " · " + Rule.Family;
        }
        private sealed class LexItem
        {
            public LexiconEntry Entry
            {
                get;
            }
            public LexItem(LexiconEntry e)
            {
                Entry = e;
            }
            public override string ToString() => Entry.Lemma;
        }
    }
}
