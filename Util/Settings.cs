using UnityEngine;
using Verse;

namespace MultiFloorStorage.Util
{
    // This class will hold your settings data
    public class MultiFloorStorage_Settings : ModSettings
    {
        public bool overrideDsuLimit = false;
        public int dsuLimit = 2048;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref overrideDsuLimit, "overrideDsuLimit", false);
            Scribe_Values.Look(ref dsuLimit, "dsuLimit", 2048);
            base.ExposeData();
        }
    }

    // This class will manage the settings window
    public class MultiFloorStorage_Mod : Mod
    {
        public static MultiFloorStorage_Settings Settings;

        public MultiFloorStorage_Mod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MultiFloorStorage_Settings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // Add a checkbox to enable/disable the override
            listing.CheckboxLabeled("Override DSU storage limit", ref Settings.overrideDsuLimit, "When checked, all Mass Storage Units will use the limit defined below.");

            // Add a slider to set the limit
            if (Settings.overrideDsuLimit)
            {
                listing.Label("DSU storage limit: " + Settings.dsuLimit);
                Settings.dsuLimit = (int)listing.Slider(Settings.dsuLimit, 100, 8000);
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Multi-Floor Storage"; // The name of your mod in the settings list
        }
    }
}