using System;
using System.Runtime.InteropServices;
using AcademicParaphraser.Core;
namespace AcademicParaphraser.WordHost.Automation
{
    [ComVisible(true), Guid("2EB26C55-EDAF-4A66-B582-5C67B8DC0262"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IAcademicAutomation
    {
        string State
        {
            get;
        }
        bool Tracking
        {
            get;
        }
        bool LinksProtected
        {
            get;
        }
        int DefaultStrength
        {
            get;
        }
        void Generate(int strength); string Apply(); void SetTracking(bool enabled); void SetLinks(bool enabled); void SetStrength(int strength);
    }
    [ComVisible(true), ClassInterface(ClassInterfaceType.None), ComDefaultInterface(typeof(IAcademicAutomation))]
    // External COM test calls must return to the Word apartment rather than use a free-threaded CCW.
    public sealed class AcademicAutomation : StandardOleMarshalObject, IAcademicAutomation
    {
        private readonly Func<AddinController?> getController;
        public AcademicAutomation(Func<AddinController?> getController)
        {
            this.getController = getController;
        }
        public string State
        {
            get
            {
                var controller = getController();
                return controller == null ? "unavailable" : controller.Busy ? "busy" : controller.HasProposals ? "ready" : "idle";
            }
        }
        public bool Tracking => getController()?.Settings.TrackChanges ?? false;
        public bool LinksProtected => getController()?.Settings.PreserveLinks ?? true;
        public int DefaultStrength => (int)(getController()?.Settings.DefaultStrength ?? Strength.Moderate);
        public void SetStrength(int strength)
        {
            if (strength >= 1 && strength <= 3)
                getController()?.SetStrength((Strength)strength);
        }
        public async void Generate(int strength)
        {
            var controller = getController();
            if (controller == null || strength < 1 || strength > 3)
                return;
            controller.SetStrength((Strength)strength);
            await controller.GenerateAsync();
        }
        public string Apply() => getController()?.ApplyForAutomation() ?? "unavailable";
        public void SetTracking(bool enabled) => getController()?.SetOption("tracking", enabled);
        public void SetLinks(bool enabled) => getController()?.SetOption("links", enabled);
    }
}
