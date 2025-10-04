// --------------------------------------------------------------------------------------
// File: AdvancedIO_PatchHelper_MultiMap.cs
// Purpose: Patch/helper utilities for advanced IO ports (multi-map, multi-floor storage).
// Class: AdvancedIO_PatchHelper_MultiMap (static)
// - Gathers, sorts, and queries advanced IO ports on a map.
// - Provides utility methods for finding reachable ports, calculating paths, and filtering by proximity.
// - Used for pawn automation, item hauling AI, and linking logic between DSU and IO port systems.
// --------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Verse;
using MultiFloorStorage.Util;
using MultiFloorStorage.Components;
using ProjectRimFactory.Storage;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Static helper class for querying, sorting, and validating advanced IO ports across maps.
    /// Used for multi-map pawn logic, pathfinding, and automated item transfer.
    /// </summary>
    public static class AdvancedIO_PatchHelper_MultiMap
    {
        /// <summary>
        /// Yields all *powered and ready* advanced IO ports on the given map.
        /// Only includes ports whose linked DSU is powered and can accept a new item.
        /// </summary>
        public static IEnumerable<KeyValuePair<IntVec3, Buildings.Building_AdvancedStorageUnitIOPortMulti>> GetAdvancedIOPorts(Map map)
        {
            var mfsComp = GlobalMFSManager.Instance.Get(map.uniqueID);
            if (mfsComp == null)
                yield break;

            foreach (var kvp in mfsComp.GetAdvancedIOLocations())
            {
                var advIO = kvp.Value as Buildings.Building_AdvancedStorageUnitIOPortMulti;
                if (advIO == null) continue;
                var dsu = advIO.GetComp<Comp_MultiFloorDSULinker>()?.LinkedDSU as Buildings.Building_MassStorageUnitPoweredMulti;
                if ((dsu?.Powered ?? false) && advIO.CanGetNewItem)
                    yield return new KeyValuePair<IntVec3, Buildings.Building_AdvancedStorageUnitIOPortMulti>(kvp.Key, advIO);
            }
        }
		
        /// <summary>
        /// Checks if the given IO port can move the given item (is present in its DSU's items).
        /// </summary>
        public static bool CanMoveItem(Buildings.Building_AdvancedStorageUnitIOPortMulti port, Thing thing)
        {
            var dsu = StorageLinkHelper.GetEffectiveStorage<Building>(port);
            return (dsu as ILinkableStorageParentMulti)?.StoredItems?.Contains(thing) ?? false;
        }

        /// <summary>
        /// Checks if the IO port can move an item at the given cell (is present in DSU at that cell).
        /// </summary>
        public static bool CanMoveItem(Buildings.Building_AdvancedStorageUnitIOPortMulti port, IntVec3 thingPos)
        {
            var dsu = StorageLinkHelper.GetEffectiveStorage<Building>(port);
            return (dsu as ILinkableStorageParentMulti)?.HoldsPos(thingPos) ?? false;
        }

        /// <summary>
        /// Returns the total Euclidean path cost from pawnPos to thingPos to targetPos.
        /// Used for simple range-based queries (not real pathfinding).
        /// </summary>
        public static float CalculatePath(IntVec3 pawnPos, IntVec3 thingPos, IntVec3 targetPos)
        {
            return pawnPos.DistanceTo(thingPos) + thingPos.DistanceTo(targetPos);
        }

        /// <summary>
        /// Returns the total actual pathfinding cost from pawn to thing to target, using map's pathfinder.
        /// Used for real-world reachability/cost queries.
        /// </summary>
        public static float CalculatePath(Pawn pawn, IntVec3 thingPos, IntVec3 targetPos, Map map)
        {
            return map.pathFinder.FindPath(pawn.Position, thingPos, TraverseParms.For(pawn)).TotalCost
                 + map.pathFinder.FindPath(thingPos, targetPos, TraverseParms.For(pawn)).TotalCost;
        }

        /// <summary>
        /// Orders all advanced IO ports on the map by total path distance from pawn to port, then to target.
        /// Returns sorted list of (distance, port).
        /// </summary>
        public static List<KeyValuePair<float, Buildings.Building_AdvancedStorageUnitIOPortMulti>> GetOrderedAdvancedIOPorts(Map map, IntVec3 pawnPos, IntVec3 targetPos)
        {
            var dict_IOports = GetAdvancedIOPorts(map);
            List<KeyValuePair<float, Buildings.Building_AdvancedStorageUnitIOPortMulti>> Ports = new();
            foreach (var pair in dict_IOports)
            {
                var distance = CalculatePath(pawnPos, pair.Key, targetPos);
                Ports.Add(new KeyValuePair<float, Buildings.Building_AdvancedStorageUnitIOPortMulti>(distance, pair.Value));
            }
            return Ports.OrderBy(i => i.Key).ToList();
        }

        /// <summary>
        /// Yields advanced IO ports closer than a given distance to the reference position.
        /// </summary>
        public static IEnumerable<KeyValuePair<float, Buildings.Building_AdvancedStorageUnitIOPortMulti>> GetOrderedAdvancedIOPortsCloserThan(Map map, IntVec3 referencePos, float maxDistance)
        {
            return GetOrderedAdvancedIOPorts(map, referencePos).Where(i => i.Key < maxDistance);
        }

        /// <summary>
        /// Returns the closest advanced IO port to the given reference position.
        /// </summary>
        public static KeyValuePair<float, Buildings.Building_AdvancedStorageUnitIOPortMulti> GetClosestPort(Map map, IntVec3 referencePos)
        {
            return GetOrderedAdvancedIOPorts(map, referencePos).FirstOrDefault();
        }

        /// <summary>
        /// Returns the closest advanced IO port (pawnPos to thing totarget), within maxDistance, that can move the specified item.
        /// </summary>
        public static KeyValuePair<float, Buildings.Building_AdvancedStorageUnitIOPortMulti> GetClosestPort(Map map, IntVec3 pawnPos, IntVec3 targetPos, Thing thing, float maxDistance)
        {
            return GetOrderedAdvancedIOPorts(map, pawnPos, targetPos)
                .Where(p => p.Key < maxDistance && CanMoveItem(p.Value, thing))
                .FirstOrDefault();
        }

        /// <summary>
        /// Orders advanced IO ports by distance to a reference position (used for simple range checks).
        /// </summary>
        public static List<KeyValuePair<float, Buildings.Building_AdvancedStorageUnitIOPortMulti>> GetOrderedAdvancedIOPorts(Map map, IntVec3 referencePos)
        {
            var dict_IOPorts = GetAdvancedIOPorts(map);
            var Ports = new List<KeyValuePair<float, Buildings.Building_AdvancedStorageUnitIOPortMulti>>();
            foreach (var pair in dict_IOPorts)
            {
                Ports.Add(new KeyValuePair<float, Buildings.Building_AdvancedStorageUnitIOPortMulti>(
                    pair.Key.DistanceTo(referencePos), pair.Value));
            }
            return Ports.OrderBy(i => i.Key).ToList();
        }
    }
}