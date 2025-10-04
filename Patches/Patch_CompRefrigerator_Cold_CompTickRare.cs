// MultiFloorStorage: Patch_CompRefrigerator_Cold_CompTickRare.cs
// Harmony patch to update power draw on each tick for powered cold storage

using HarmonyLib;
using MultiFloorStorage.Buildings;
using MultiFloorStorage.Util;
using RimWorld;
using Verse;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Harmony patch for Building_ColdStoragePoweredMulti.Tick().
    /// Ensures power draw is updated based on internal temperature and settings.
    /// </summary>
    [HarmonyPatch(typeof(Building_ColdStoragePoweredMulti), nameof(Building_ColdStoragePoweredMulti.Tick))]
    public static class Patch_CompRefrigerator_Cold_CompTickRare_Multi
    {
        /// <summary>
        /// Postfix method that updates power draw each tick.
        /// </summary>
        /// <param name="__instance">The cold storage building instance.</param>
        public static void Postfix(Building_ColdStoragePoweredMulti __instance)
        {
            var powertrader = __instance.GetComp<CompPowerTrader>();
            if (powertrader != null)
            {
                // Optional debug output
                // if (Prefs.DevMode)
                //     Log.Message($"[MFS] ColdStoragePoweredMulti at {__instance.Position} ticked. Temp: {__instance.AmbientTemperature}");

                FridgePowerPatchUtilMulti.UpdatePowerDraw(__instance, powertrader, __instance.ExtraPowerDraw);
            }
        }
    }
}