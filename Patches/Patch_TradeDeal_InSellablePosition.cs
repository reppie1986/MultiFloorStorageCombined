using HarmonyLib;
using RimWorld;
using Verse;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Makes items inside unspawned ColdStorage buildings count as sellable.
    /// </summary>
    [HarmonyPatch(typeof(TradeDeal), "InSellablePosition")]
    public static class Patch_TradeDeal_InSellablePosition
    {
        public static bool Prefix(Thing t, out string reason, ref bool __result)
        {
            // If item is unspawned but still in a map, check if it's in cold storage
            if (!t.Spawned && t.MapHeld != null)
            {
                var coldStorage = Util.PatchStorageUtilMulti.GetMFSMapComponent(t.MapHeld).ColdStorageBuildings;
                foreach (var building in coldStorage)
                {
                    if (building.StoredItems.Contains(t))
                    {
                        reason = null;
                        __result = true;
                        return false; // Skip original method
                    }
                }
            }
            else if (t.MapHeld is null)
            {
                Log.Warning($"[MFS] TradeDeal.InSellablePosition: {t} has null MapHeld.");
            }

            // Fallback to vanilla
            reason = null;
            return true;
        }
    }
}