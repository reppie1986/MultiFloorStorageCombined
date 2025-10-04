// MultiFloorStorage: MultiFloorStorage_ModComponent.cs
// Initializes Harmony patches and conditional patch handlers for MultiFloorStorage

using HarmonyLib;
using ProjectRimFactory.Common;
using System.Reflection;
using System;
using Verse;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Main mod component class for MultiFloorStorage. Initializes Harmony and conditional patch helpers.
    /// </summary>
    public class MultiFloorStorage_ModComponent : Mod
    {
        /// <summary>
        /// Harmony instance used for patching.
        /// </summary>
        public Harmony HarmonyInstance { get; private set; }

        /// <summary>
        /// Initializes Harmony patches and sets up conditional patches from settings.
        /// </summary>
        /// <param name="content">The mod content pack.</param>
        public MultiFloorStorage_ModComponent(ModContentPack content) : base(content)
        {
            try
            {
                // Initialize Harmony
                this.HarmonyInstance = new Harmony("com.yourname.MultiFloorStorage");
                this.HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("[MultiFloorStorage] Harmony patches applied.");

                // Initialize conditional patch helpers with the same instance
                Util.ConditionalPatchHelperMulti.InitHarmony(this.HarmonyInstance);

                // Apply conditional patch if setting is enabled
                Util.ConditionalPatchHelperMulti.Patch_Reachability_CanReach_Multi.PatchHandler(
                    ProjectRimFactory_ModSettings.PRF_Patch_Reachability_CanReach
                );
            }
            catch (Exception ex)
            {
                Log.Error("MultiFloorStorage :: Caught exception: " + ex);
            }
        }
    }
}