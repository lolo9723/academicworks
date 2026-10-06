using System;
using System.IO;
using Microsoft.Office.Tools;
using Word = Microsoft.Office.Interop.Word;
namespace AcademicParaphraser.WordAddin
{
    [Microsoft.VisualStudio.Tools.Applications.Runtime.StartupObject(0)]
    public sealed class ThisAddIn : AddInBase
    {
        private readonly Microsoft.Office.Tools.Word.ApplicationFactory factory;
        private WordHost.AddinController? controller; private AcademicRibbon? ribbon; private CustomTaskPaneCollection? panes; private CustomTaskPane? pane;
        private Word.Application? application;
        private string startupStage = "HOST";
        private bool startupFailed;
        public ThisAddIn(Microsoft.Office.Tools.Word.ApplicationFactory factory, IServiceProvider serviceProvider) : base(factory, serviceProvider, "AddIn", "ThisAddIn") { this.factory = factory; }
        protected override void Initialize()
        {
            base.Initialize();
            try
            {
                application = GetHostItem<Word.Application>(typeof(Word.Application), "Application");
                startupStage = "COLLECTION";
                panes = factory.CreateCustomTaskPaneCollection(null, null, "CustomTaskPanes", "CustomTaskPanes", this);
                panes.BeginInit();
                Startup += StartUserInterface;
            }
            catch (Exception ex) { ReportStartupFailure(ex); }
        }
        protected override void FinishInitialization()
        {
            if (!startupFailed)
            {
                try
                {
                    startupStage = "COLLECTION_READY";
                    panes!.EndInit();
                }
                catch (Exception ex) { ReportStartupFailure(ex); }
            }
            OnStartup();
        }
        private void StartUserInterface(object? sender, EventArgs e)
        {
            if (startupFailed)
                return;
            try
            {
                // A VSTO pane can be added only after its collection has completed initialization.
                startupStage = "CONTROLLER";
                System.Windows.Forms.Application.EnableVisualStyles();
                controller = new WordHost.AddinController(application!, Path.GetDirectoryName(typeof(ThisAddIn).Assembly.Location)!);
                startupStage = "PANE";
                pane = panes!.Add(controller.Preview, "Akademik Parafraz");
                pane.Width = 390;
                controller.ShowPreview = () => pane.Visible = true;
                if (ribbon != null)
                    ribbon.Attach(controller);
            }
            catch (Exception ex) { ReportStartupFailure(ex); }
        }
        private void ReportStartupFailure(Exception exception)
        {
            startupFailed = true;
            var cause = exception.GetBaseException();
            string code = "AP_" + startupStage + "_" + cause.GetType().Name + "_" + cause.HResult.ToString("X8");
            // Startup does not read a document. Still exclude messages, stacks, paths and document content.
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AkademikParafraz", "logs");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "startup-diagnostics.txt"), DateTime.UtcNow.ToString("O") + " " + code + " process=" + (Environment.Is64BitProcess ? "x64" : "x86") + Environment.NewLine);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException) { }
            System.Windows.Forms.MessageBox.Show("Akademik Parafraz açılışı tamamlanamadı. Word belgeniz değiştirilmedi.\n\nHata kodu: " + code + "\n\nBu kodu paylaşın veya kurulum paketindeki Tanila.cmd dosyasını çalıştırın.", "Akademik Parafraz");
        }
        protected override object RequestComAddInAutomationService() => new WordHost.Automation.AcademicAutomation(() => controller);
        protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject()
        {
            ribbon = new AcademicRibbon();
            if (controller != null)
                ribbon.Attach(controller);
            return ribbon;
        }
        protected override void OnShutdown()
        {
            controller?.Dispose();
            panes?.Dispose();
            base.OnShutdown();
        }
    }
}
