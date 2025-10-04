// --------------------------------------------------------------------------------------
// File: Patch_Pawn_JobTracker_StartJob.cs
// Purpose: Harmony patch for Pawn_JobTracker.StartJob to redirect haul/bill jobs through MultiMap-aware I/O ports.
// - Only affects player pawns (Faction.IsPlayer).
// - Skips PickUpAndHaul jobs (defName == "HaulToInventory").
// - Checks if job is haul-type by examining targetA.Thing.def.category.
// - Extracts job's target position and item list depending on job type.
// - For each hidden item in MFS, checks if any I/O port is closer or reachable.
// - If so, enqueues the item to the I/O port's transfer queue.
// --------------------------------------------------------------------------------------

using HarmonyLib;
using MultiFloorStorage.Util;
using ProjectRimFactory.Storage;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Patch for Pawn_JobTracker.StartJob.
    /// Intercepts jobs that require hauling or bill input to redirect through I/O ports (MultiMap-aware).
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Patch_Pawn_JobTracker_StartJob
    {
        // Identifies the key target cell based on job type (targetA or targetB)
        private static bool TryGetTargetPos(ref IntVec3 targetPos, bool isHaulJobType, Job newJob, IntVec3 pawnPosition)
        {
            if (isHaulJobType)
            {
                // For haul jobs, use targetB or fallback to pawn position
                targetPos = newJob.targetB.Thing?.Position ?? newJob.targetB.Cell;
                if (targetPos == IntVec3.Invalid) targetPos = pawnPosition;
                if (newJob.targetA == null) return false;
            }
            else
            {
                // For bill jobs, use targetA, require valid targetB or queue
                targetPos = newJob.targetA.Thing?.Position ?? newJob.targetA.Cell;
                if (newJob.targetB == IntVec3.Invalid && (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0))
                    return false;
            }
            return true;
        }

        // Collects target items (single or queue) depending on job type
        private static void GetTargetItems(ref List<LocalTargetInfo> targetItems, bool isHaulJobType, Job newJob)
        {
            if (isHaulJobType)
            {
                // Haul jobs use targetA
                targetItems = new List<LocalTargetInfo> { newJob.targetA };
            }
            else
            {
                // Bill jobs use targetQueueB or fallback to targetB
                if (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)
                    targetItems = new List<LocalTargetInfo> { newJob.targetB };
                else
                    targetItems = newJob.targetQueueB;
            }
        }

        public static bool Prefix(Job newJob, ref Pawn ___pawn, JobCondition lastJobEndCondition = JobCondition.None,
            ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true,
            ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false)
        {
            // Skip if pawn is null, non-player, or job is "HaulToInventory" (PickUpAndHaul)
            if (___pawn?.Faction == null || !___pawn.Faction.IsPlayer) return true;
            if (newJob.def.defName == "HaulToInventory") return true;

            var map = ___pawn.Map;
            var pos = ___pawn.Position;
            var mfsComponent = PatchStorageUtilMulti.GetMFSMapComponent(map);

            // Determine if it's a haul job and extract key target position
            IntVec3 targetPos = IntVec3.Invalid;
            bool isHaulJobType = newJob.targetA.Thing?.def?.category == ThingCategory.Item;
            if (!TryGetTargetPos(ref targetPos, isHaulJobType, newJob, pos)) return true;

            // Get nearby I/O ports ordered by distance
            var ports = Patches.AdvancedIO_PatchHelper_MultiMap.GetOrderedAdvancedIOPorts(map, pos, targetPos);

            // Get all relevant target items for the job
            List<LocalTargetInfo> targetItems = null;
            GetTargetItems(ref targetItems, isHaulJobType, newJob);

            foreach (var target in targetItems)
            {
                if (target.Thing == null)
                    continue;

                // Estimate path distance from pawn to item via job's expected target
                float dist = AdvancedIO_PatchHelper_MultiMap.CalculatePath(pos, target.Cell, targetPos);

                // If item is in a hidden MFS cell, check if a port should handle it
                if (mfsComponent.ShouldHideItemsAtPos(target.Cell))
                {
                    foreach (var port in ports)
                    {
                        bool portIsCloser = port.Key < dist;

                        // Verify reachability both vanilla and via patch
                        bool canReach = ConditionalPatchHelperMulti.Patch_Reachability_CanReach_Multi.Status &&
                            map.reachability.CanReach(pos, target.Thing, PathEndMode.Touch, TraverseParms.For(___pawn)) &&
                            Patch_Reachability_CanReach_Multi.CanReachThing(target.Thing);

                        // If valid, enqueue item into I/O port and break
                        if (portIsCloser || canReach)
                        {
                            if (AdvancedIO_PatchHelper_MultiMap.CanMoveItem(port.Value, target.Cell))
                            {
                                port.Value.AddItemToQueue(target.Thing);
                                port.Value.UpdateQueue();
                                break;
                            }
                        }
                        else
                        {
                            break; // Remaining ports are farther away
                        }
                    }
                }
            }

            return true; // Let original method continue
        }
    }
}
