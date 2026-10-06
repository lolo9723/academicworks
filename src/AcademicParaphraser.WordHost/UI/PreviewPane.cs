using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AcademicParaphraser.Core;
namespace AcademicParaphraser.WordHost.UI
{
    public sealed class PreviewPane : UserControl
    {
        private readonly Label status = new Label { AutoSize = true, MaximumSize = new Size(330, 0) };
        private readonly RichTextBox before = new RichTextBox { ReadOnly = true, Height = 180, Dock = DockStyle.Top };
        private readonly RichTextBox after = new RichTextBox { ReadOnly = true, Height = 180, Dock = DockStyle.Top };
        private readonly Button apply = new Button { Text = "Uygula", AutoSize = true }, next = new Button { Text = "Sonraki alternatif", AutoSize = true }, cancel = new Button { Text = "İptal", AutoSize = true };
        private readonly ProgressBar progress = new ProgressBar { Style = ProgressBarStyle.Marquee, Height = 8, Visible = false, Dock = DockStyle.Top };
        public event EventHandler? ApplyRequested, NextRequested, CancelRequested;
        public PreviewPane()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9);
            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(12) };
            layout.Controls.Add(new Label { Text = "Akademik Parafraz", Font = new Font("Segoe UI", 15, FontStyle.Bold), AutoSize = true });
            layout.Controls.Add(progress);
            layout.Controls.Add(status);
            layout.Controls.Add(new Label { Text = "Eski ifade", AutoSize = true });
            layout.Controls.Add(before);
            layout.Controls.Add(new Label { Text = "Yeni ifade", AutoSize = true });
            layout.Controls.Add(after);
            layout.Controls.Add(apply);
            layout.Controls.Add(next);
            layout.Controls.Add(cancel);
            Controls.Add(layout);
            apply.Click += (s, e) => ApplyRequested?.Invoke(this, EventArgs.Empty);
            next.Click += (s, e) => NextRequested?.Invoke(this, EventArgs.Empty);
            cancel.Click += (s, e) => CancelRequested?.Invoke(this, EventArgs.Empty);
            Resize += (s, e) => { before.Width = after.Width = progress.Width = Math.Max(220, Width - 45); };
            SetBusy(false);
        }
        public void SetBusy(bool busy)
        {
            progress.Visible = busy;
            apply.Enabled = !busy;
            next.Enabled = !busy;
            cancel.Enabled = true;
            if (busy)
                status.Text = "Metin yerel olarak çözümleniyor…";
        }
        public void ShowCandidate(string source, Candidate candidate, int index, int count)
        {
            before.Text = source;
            after.Text = candidate.Text;
            int shift = 0;
            foreach (var edit in candidate.Edits.OrderBy(e => e.Start))
            {
                before.Select(edit.Start, edit.Length);
                before.SelectionBackColor = Color.MistyRose;
                after.Select(edit.Start + shift, edit.Replacement.Length);
                after.SelectionBackColor = Color.Honeydew;
                shift += edit.Replacement.Length - edit.Length;
            }
            before.Select(0, 0);
            after.Select(0, 0);
            status.Text = $"Alternatif {index + 1}/{count} · {candidate.Edits.Count} dönüşüm · Kural güveni: %{candidate.Score.SemanticSafety * 100:0}\nBu puan dilbilimsel kurallara dayanır; anlam doğruluğunun garantisi değildir.";
            apply.Enabled = candidate.Edits.Count > 0;
            next.Enabled = count > 1;
        }
        public void ShowMessage(string message)
        {
            status.Text = message;
            apply.Enabled = false;
            next.Enabled = false;
        }
    }
}
