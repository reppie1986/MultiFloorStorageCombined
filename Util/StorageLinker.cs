// REFACTORED
using Verse;
using System.Linq;

namespace MultiFloorStorage.Util
{
    public static class StorageLinkHelper
    {
        /// <summary>
        /// The single source of truth for finding a linked storage building.
        /// It prioritizes the modern Comp_MultiFloorDSULinker component.
        /// </summary>
        public static Building GetEffectiveStorage(Building parentBuilding)
        {
            var linker = parentBuilding.GetComp<MultiFloorStorage.Components.Comp_MultiFloorDSULinker>();
            if (linker?.LinkedDSU != null)
            {
                return linker.LinkedDSU;
            }
            return null; // No other fallback is needed if the comp is the standard.
        }

        /// <summary>
        /// Generic version of GetEffectiveStorage.
        /// </summary>
        public static T GetEffectiveStorage<T>(Building parentBuilding) where T : Building
        {
            return GetEffectiveStorage(parentBuilding) as T;
        }
    }
}