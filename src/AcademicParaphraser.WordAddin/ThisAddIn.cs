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
        public ThisAddIn(Microsoft.Office.Tools.Word.ApplicationFactory factory, IServiceProvider serviceProvider) : base(factory, serviceProvider, "AddIn", "ThisAddIn") { this.factory = factory; }
        protected override void Initialize()
        {
            base.Initialize();
            var app = GetHostItem<Word.Application>(typeof(Word.Application), "Application");
            System.Windows.Forms.Application.EnableVisualStyles();
            try
            {
                panes = factory.CreateCustomTaskPaneCollection(null, null, "CustomTaskPanes", "CustomTaskPanes", this);
                controller = new WordHost.AddinController(app, Path.GetDirectoryName(typeof(ThisAddIn).Assembly.Location)!);
                pane = panes.Add(controller.Preview, "Akademik Parafraz");
                pane.Width = 390;
                controller.ShowPreview = () => pane.Visible = true;
                if (ribbon != null)
                    ribbon.Attach(controller);
            }
            catch (Exception) { System.Windows.Forms.MessageBox.Show("Akademik Parafraz başlatılamadı. Kurulumu onarın; Word belgeniz değiştirilmedi.", "Akademik Parafraz"); }
        }
        protected override void FinishInitialization()
        {
            OnStartup();
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
