// --------------------------------------------------------------------------------------
// File: StorageOutputUtil.cs
// Purpose: Utility for outputting items from MultiFloorStorage buildings.
// - Resolves output cell using CompOutputAdjustable if available, or a fallback position.
// - Validates output destination to avoid overlapping with other ILinkableStorageParentMulti.
// - Attempts to place full item stack at or near the output cell using GenPlace.TryPlaceThing.
// --------------------------------------------------------------------------------------

using ProjectRimFactory.Common; // For CompOutputAdjustable
using System;
using Verse; // RimWorld base map and item logic

namespace MultiFloorStorage.Util
{
    public class StorageOutputUtilMulti
    {
        private IntVec3 outputCell = IntVec3.Invalid; // Target output cell
        private Map map; // Map context for item placement

        // Constructor determines output cell and caches map reference
        public StorageOutputUtilMulti(Building building)
        {
            if (building == null)
                throw new ArgumentNullException(nameof(building));

            map = building.Map;

            // Use CompOutputAdjustable if available, else fallback to offset
            outputCell = building.GetComp<CompOutputAdjustable>()?.CurrentCell ?? building.Position + new IntVec3(0, 0, -2);
        }

        // Validates output location to avoid stacking with another MFS storage unit
        private bool ValidatePos(IntVec3 intVec3)
        {
            return !intVec3.GetThingList(map).Any(e => e is ILinkableStorageParentMulti);
        }

        /// <summary>
        /// Attempts to place the specified item near the output cell.
        /// </summary>
        public bool OutputItem(Thing item)
        {
            // Split full stack off and attempt placement near output cell
            if (item == null) throw new ArgumentNullException(nameof(item));
            return GenPlace.TryPlaceThing(item.SplitOff(item.stackCount), outputCell, map, ThingPlaceMode.Near, null, ValidatePos);
        }
    }
}