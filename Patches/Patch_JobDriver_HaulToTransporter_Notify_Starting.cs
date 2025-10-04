// MultiFloorStorage: Patch_JobDriver_HaulToTransporter_Notify_Starting.cs
// Redirects hauled items to IOPortMulti if closer and valid, upon haul job start.

using HarmonyLib;
using MultiFloorStorage.Patches;
using ProjectRimFactory.Storage;
using RimWorld;

using Verse;
using Verse.AI;
using MultiFloorStorage.Buildings;

namespace MultiFloorStorage.Patches
{
    [HarmonyPatch(typeof(JobDriver_HaulToTransporter), "Notify_Starting")]
    class Patch_JobDriver_HaulToTransporter_Notify_Starting_Multi
    {
        public static void Postfix(JobDriver_HaulToTransporter __instance)
        {
            Pawn pawn = __instance.pawn;
            Thing thing = __instance.job.targetA.Thing;
            IntVec3 thingPos = __instance.job.targetA.Cell;
            IntVec3 transporterPos = __instance.job.targetB.Cell;
            Map map = pawn.Map;

            float originalDistance = AdvancedIO_PatchHelper_MultiMap.CalculatePath(pawn.Position, thingPos, transporterPos);

            var closest = AdvancedIO_PatchHelper_MultiMap.GetClosestPort(map, pawn.Position, transporterPos, thing, originalDistance);
            Building_AdvancedStorageUnitIOPortMulti closestPort = closest.Value;

            if (closestPort != null && AdvancedIO_PatchHelper_MultiMap.CanMoveItem(closestPort, thing))
            {
                // Only move the thing if it’s already spawned
                if (thing.Spawned)
                {
                    thing.DeSpawn();
                    GenPlace.TryPlaceThing(thing, closestPort.Position, map, ThingPlaceMode.Near);
                }
                else
                {
                    closestPort.PlaceThingNow(thing);
                }
            }
        }
    }
}