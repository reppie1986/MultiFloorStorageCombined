// --------------------------------------------------------------------------------------
// File: Patch_Pawn_TraderTracker_ColonyThingsWillingToBuy.cs
// Purpose: Harmony patch for Pawn_TraderTracker.ColonyThingsWillingToBuy to include items from MFS cold storage.
// - Applies only to storage units where AdvancedIO is not allowed (cold storage units).
// - Iterates through ILinkableStorageParentMulti instances returned by TradePatchHelper.AllPowered(map).
// - Adds all stored items from valid storages to the original result set (__result).
// --------------------------------------------------------------------------------------

using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MultiFloorStorage.Patches
{
    [HarmonyPatch(typeof(Pawn_TraderTracker), nameof(Pawn_TraderTracker.ColonyThingsWillingToBuy))]
    public static class Patch_Pawn_TraderTracker_ColonyThingsWillingToBuy_Multi
    {
        static void Postfix(Pawn playerNegotiator, ref IEnumerable<Thing> __result)
        {
            // Get map from negotiator pawn
            Map map = playerNegotiator?.Map;
            if (map == null) return;

            // Copy all current tradable things into a hashset
            var allThings = new HashSet<Thing>(__result);

            // Add stored items from all powered cold storages
            foreach (Util.ILinkableStorageParentMulti storage in TradePatchHelper.AllPowered(map))
            {
                // Skip if storage allows advanced IO (i.e. not a cold storage)
                if (storage.AdvancedIOAllowed)
                    continue;

                // Add all stored items to tradables
                if (storage.StoredItems != null)
                {
                    foreach (Thing item in storage.StoredItems)
                    {
                        allThings.Add(item);
                    }
                }
            }

            // Update the result with extended item list
            __result = allThings;
        }
    }
}
