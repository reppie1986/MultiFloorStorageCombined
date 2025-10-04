using HarmonyLib;
using MultiFloorStorage.Components;
using MultiFloorStorage.Util;
using Verse;
using Verse.AI;
using System; // <-- CRITICAL: This was missing.

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Correctly patches the game's pathfinding to allow pawns to "reach" items
    /// that are stored in a DSU by pathing to the nearest I/O port instead.
    /// </summary>
    [HarmonyPatch(typeof(Verse.Reachability))]
    [HarmonyPatch(nameof(Verse.Reachability.CanReach), new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) })]
    public static class Patch_Reachability_CanReach_Multi
    {
        private static Thing canReachThing = null;

        public static bool CanReachThing(Thing thing)
        {
            var ret = thing == canReachThing;
            canReachThing = null;
            return ret;
        }

        public static void Postfix(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result, Map ___map, Reachability __instance)
        {
            // If the game already found a path, we don't need to do anything.
            if (__result) return;

            var thing = dest.Thing;
            if (thing == null || thing.def.category != ThingCategory.Item) return;

            var mapComp = MultiMapStorageUtil.GetMFSMapComponent(___map);
            if (mapComp == null) return;

            // Check if our custom pathing logic can find a route via an I/O port.
            if (HasPathToItemViaIOPort(thing, mapComp, __instance, start, traverseParams))
            {
                canReachThing = thing; // Mark this thing as reachable for this one check
                __result = true;       // Tell the game a path was found
            }
        }

        private static bool HasPathToItemViaIOPort(Thing thing, MFSMapComponent mapComp, Reachability reachability, IntVec3 start, TraverseParms traverseParams)
        {
            var thingPos = thing.Position;
            if (!mapComp.ShouldHideItemsAtPos(thingPos)) return false;

            var advancedIOLocations = mapComp.GetAdvancedIOLocations();
            foreach (var kv in advancedIOLocations)
            {
                var port = kv.Value;

                // Check if the port is linked to the DSU holding the item.
                var dsu = StorageLinkHelper.GetEffectiveStorage(port);
                if (dsu != null && dsu is IStorageWithPositionMulti posDSU && posDSU.GetPosition() == thingPos)
                {
                    // If the pawn can reach the port, they can "reach" the item.
                    if (reachability.CanReach(start, kv.Key, PathEndMode.Touch, traverseParams))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    // NOTE: The fragile Patch_MassStorageUnit_SpawnSetup has been removed.
    // Manual linking via Gizmos is a more stable and user-friendly approach.
}