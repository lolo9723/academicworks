using System;
using System.IO;
using System.Runtime.InteropServices;
using AcademicParaphraser.Core;
using Office = Microsoft.Office.Core;
namespace AcademicParaphraser.WordAddin
{
    [ComVisible(true)]
    public sealed class AcademicRibbon : Office.IRibbonExtensibility
    {
        private WordHost.AddinController? controller; private Office.IRibbonUI? ui;
        public string GetCustomUI(string ribbonId)
        {
            using (var stream = typeof(AcademicRibbon).Assembly.GetManifestResourceStream("AcademicParaphraser.WordAddin.Ribbon.xml")!)
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
        public void Attach(WordHost.AddinController value)
        {
            controller = value;
            controller.StateChanged += (s, e) => ui?.Invalidate();
            ui?.Invalidate();
        }
        public void OnLoad(Office.IRibbonUI ribbon) => ui = ribbon;
        public bool GetEnabled(Office.IRibbonControl control) => controller != null && !controller.Busy;
        public bool GetPressed(Office.IRibbonControl control)
        {
            if (controller == null)
                return false;
            var s = controller.Settings;
            switch (control.Id)
            {
                case "light":
                    return s.DefaultStrength == Strength.Light;
                case "moderate":
                    return s.DefaultStrength == Strength.Moderate;
                case "strong":
                    return s.DefaultStrength == Strength.Strong;
                case "citations":
                    return s.PreserveCitations;
                case "numbers":
                    return s.PreserveNumbers;
                case "names":
                    return s.PreserveNames;
                case "technical":
                    return s.PreserveTechnicalTerms;
                case "links":
                    return s.PreserveLinks;
                case "tracking":
                    return s.TrackChanges;
                default:
                    return false;
            }
        }
        public void OnToggle(Office.IRibbonControl control, bool pressed)
        {
            if (controller == null)
                return;
            controller.Guard(() => { switch (control.Id) { case "light": controller.SetStrength(Strength.Light); break; case "moderate": controller.SetStrength(Strength.Moderate); break; case "strong": controller.SetStrength(Strength.Strong); break; default: controller.SetOption(control.Id, pressed); break; } ui?.Invalidate(); });
        }
        public async void OnCommand(Office.IRibbonControl control)
        {
            if (controller == null)
                return;
            switch (control.Id)
            {
                case "paraphrase":
                case "alternatives":
                    await controller.GenerateAsync();
                    break;
                case "previous":
                    controller.Guard(() => controller.Next(-1));
                    break;
                case "next":
                    controller.Guard(() => controller.Next(1));
                    break;
                case "preview":
                    controller.ShowPreview?.Invoke();
                    break;
                case "lock":
                    controller.LockSelected();
                    break;
                case "unlock":
                    controller.UnlockSelected();
                    break;
                case "settings":
                    controller.OpenSettings();
                    break;
                case "dictionary":
                    controller.OpenDictionary();
                    break;
                case "history":
                    controller.OpenHistory();
                    break;
                case "undo":
                    controller.Undo();
                    break;
            }
        }
    }
}
