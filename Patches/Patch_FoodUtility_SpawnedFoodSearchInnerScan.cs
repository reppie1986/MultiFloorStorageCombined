// MultiFloorStorage: Patch_FoodUtility_SpawnedFoodSearchInnerScanMulti.cs
// Enhances food search logic to prefer nearby I/O ports if better than default distance

using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using MultiFloorStorage.Patches;
using MultiFloorStorage.Util;
using MultiFloorStorage.Buildings;
using Verse.AI;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Harmony patch for FoodUtility.SpawnedFoodSearchInnerScan.
    /// Allows pawns to consider moving food via nearby I/O ports if efficient.
    /// </summary>
    [HarmonyPatch(typeof(FoodUtility), "SpawnedFoodSearchInnerScan")]
    public static class Patch_FoodUtility_SpawnedFoodSearchInnerScanMulti
    {
        static int thingLocalIndex = -1;
        static int distLocalIndex = -1;

        private static float mindist = float.MaxValue;
        private static Building_AdvancedStorageUnitIOPortMulti closestPort = null;
        private static bool ioPortSelected = false;
        private static Thing ioPortSelectedFor = null;

        /// <summary>
        /// Transpiler that injects logic to evaluate whether I/O ports offer a better food pickup option.
        /// </summary>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            bool afterFloatMin = false;

            for (int i = 0; i < codes.Count; i++)
            {
                var instruction = codes[i];

                // Track ldc_r4 -3.402823E+38
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float f && f == float.MinValue)
                {
                    afterFloatMin = true;
                    yield return instruction;
                    continue;
                }

                // Capture distance local index
                if (afterFloatMin && instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder distVar)
                {
                    distLocalIndex = distVar.LocalIndex;
                    afterFloatMin = false;

                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(
                        typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScanMulti),
                        nameof(findClosestPort));
                    continue;
                }

                // Capture Thing local index
                if (thingLocalIndex == -1 && instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder thingVar && thingVar.LocalType == typeof(Thing))
                {
                    thingLocalIndex = thingVar.LocalIndex;
                    yield return instruction;
                    continue;
                }

                // Inject comparison logic after storing new distance
                if (instruction.opcode == OpCodes.Stloc_S && distLocalIndex != -1 && instruction.operand is LocalBuilder storeVar && storeVar.LocalIndex == distLocalIndex)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloca_S, distLocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, thingLocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.Call(
                        typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScanMulti),
                        nameof(isIOPortBetter));
                    continue;
                }

                // Inject final override logic before return
                if (instruction.opcode == OpCodes.Ret && thingLocalIndex != -1)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, thingLocalIndex);
                    yield return CodeInstruction.Call(
                        typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScanMulti),
                        nameof(moveItemIfNeeded));
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Finds the closest I/O port for the given pawn on the current map.
        /// </summary>
        public static void findClosestPort(Pawn pawn, IntVec3 root)
        {
            mindist = float.MaxValue;
            closestPort = null;

            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
                return;

            var closest = AdvancedIO_PatchHelper_MultiMap.GetClosestPort(pawn.Map, pawn.Position);
            mindist = closest.Key;
            closestPort = closest.Value;
        }

        /// <summary>
        /// Determines whether an I/O port is a better candidate than the current best distance.
        /// </summary>
        public static void isIOPortBetter(ref float Distance, Thing thing, Pawn pawn, IntVec3 start)
        {
            ioPortSelected = false;

            if (mindist < Distance ||
               (ConditionalPatchHelperMulti.Patch_Reachability_CanReach_Multi.Status &&
                pawn.Map.reachability.CanReach(start, thing, PathEndMode.Touch, TraverseParms.For(pawn)) &&
                Patch_Reachability_CanReach_Multi.CanReachThing(thing)))
            {
                if (closestPort != null && AdvancedIO_PatchHelper_MultiMap.CanMoveItem(closestPort, thing))
                {
                    Distance = mindist;
                    ioPortSelected = true;
                    ioPortSelectedFor = thing;
                }
            }
        }

        /// <summary>
        /// Moves the selected food item into the I/O port if chosen.
        /// </summary>
        public static void moveItemIfNeeded(Thing thing)
        {
            if (thing != ioPortSelectedFor || !ioPortSelected || thing == null)
                return;

            ioPortSelected = false;
            closestPort?.PlaceThingNow(thing);
        }
    }
}