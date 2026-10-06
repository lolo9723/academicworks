using System;
using System.Drawing;
using System.Windows.Forms;
using AcademicParaphraser.Infrastructure.Persistence;
namespace AcademicParaphraser.WordHost.UI
{
    public sealed class HistoryForm : Form
    {
        public HistoryForm(LocalRepository repo)
        {
            Text = "Akademik Parafraz — Dönüşüm Geçmişi";
            Width = 850;
            Height = 620;
            Font = new Font("Segoe UI", 9);
            var list = new ListBox { Dock = DockStyle.Top, Height = 170 };
            var text = new RichTextBox { ReadOnly = true, Dock = DockStyle.Fill };
            foreach (var entry in repo.GetHistory())
                list.Items.Add(new Item(entry));
            list.SelectedIndexChanged += (s, e) => { if (list.SelectedItem is Item item) text.Text = "ESKİ" + Environment.NewLine + item.Entry.Original + Environment.NewLine + Environment.NewLine + "YENİ" + Environment.NewLine + item.Entry.Result; };
            var clear = new Button { Text = "Geçmişi temizle", Dock = DockStyle.Bottom, Height = 35 };
            clear.Click += (s, e) => { if (MessageBox.Show(this, "Yerel dönüşüm geçmişi silinsin mi?", "Akademik Parafraz", MessageBoxButtons.YesNo) == DialogResult.Yes) { try { repo.ClearHistory(); list.Items.Clear(); text.Clear(); } catch (Exception) { MessageBox.Show(this, "Geçmiş temizlenemedi. Veritabanını kontrol edin.", "Akademik Parafraz"); } } };
            Controls.Add(text);
            Controls.Add(list);
            Controls.Add(clear);
        }
        private sealed class Item
        {
            public HistoryEntry Entry
            {
                get;
            }
            public Item(HistoryEntry entry)
            {
                Entry = entry;
            }
            public override string ToString() => Entry.Timestamp.ToLocalTime().ToString("g") + " · " + Entry.Edits + " dönüşüm";
        }
    }
}
