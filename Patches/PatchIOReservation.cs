using HarmonyLib;
using ProjectRimFactory.Storage;
using System.Linq;
using Verse;
using Verse.AI;
using MultiFloorStorage.Buildings;

namespace MultiFloorStorage.Patches
{
    // Prevents reservation denial for I/O ports in input mode
    [HarmonyPatch(typeof(ReservationManager), "Reserve")]
    public static class Patch_Reservation_Reservation_IOMulti
    {
        static bool Prefix(Pawn claimant, Job job, LocalTargetInfo target, ref bool __result, Map ___map)
        {
            // Only run for non-Thing targets (i.e., cells)
            if (!target.HasThing && ___map != null && target.Cell.InBounds(___map))
            {
                var buildingTarget = ___map.thingGrid.ThingsListAt(target.Cell)
                    .OfType<Building_StorageUnitIOBaseMulti>()
                    .FirstOrDefault();

                if (buildingTarget != null && buildingTarget.mode == StorageIOMode.Input)
                {
                    __result = true; // Force reservation to succeed
                    return false;    // Skip original method
                }
            }

            return true; // Fallback to vanilla logic
        }
    }
}