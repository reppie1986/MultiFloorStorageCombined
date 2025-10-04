// MultiFloorStorage: Patch_DSU_OrbitalTrade.cs
// Harmony patches that enable orbital trading via powered deep storage units (DSUs)

using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using MultiFloorStorage.Buildings;
using ProjectRimFactory.Storage;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Appends items stored in powered DSUs to the list of launchable trade goods.
    /// </summary>
    [HarmonyPatch(typeof(TradeUtility), "AllLaunchableThingsForTrade")]
    public static class Patch_TradeUtility_AllLaunchableThingsForTrade
    {
        static void Postfix(Map map, ref IEnumerable<Thing> __result)
        {
            HashSet<Thing> yieldedThings = new HashSet<Thing>(__result);

            foreach (Util.ILinkableStorageParentMulti dsu in TradePatchHelper.AllPowered(map))
            {
                yieldedThings.AddRange(dsu.StoredItems);
            }

            __result = yieldedThings;
        }
    }

    /// <summary>
    /// Transpiler patch that allows initiating trades even if no vanilla beacon exists,
    /// as long as a powered DSU is present.
    /// </summary>
    [HarmonyPatch]
    public static class Patch_PassingShip_c__DisplayClass24_0
    {
        public static Type predicateClass;

        static MethodBase TargetMethod()
        {
            predicateClass = typeof(PassingShip).GetNestedTypes(AccessTools.all)
                .FirstOrDefault(t => t.FullName.Contains("c__DisplayClass23_0"));

            if (predicateClass == null)
            {
                Log.Error("[MFS] Harmony Error - predicateClass == null for Patch_PassingShip_c__DisplayClass24_0");
                return null;
            }

            var m = predicateClass.GetMethods(AccessTools.all)
                .FirstOrDefault(t => t.Name.Contains("b__1"));

            if (m == null)
            {
                Log.Error("[MFS] Harmony Error - TargetMethod == null for Patch_PassingShip_c__DisplayClass24_0");
            }

            return m;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var hiddenClass = typeof(PassingShip).GetNestedTypes(AccessTools.all)
                .FirstOrDefault(t => t.FullName.Contains("c__DisplayClass23_0"));

            bool foundCallToBeacons = false;

            foreach (var instruction in instructions)
            {
                // Locate: Building_OrbitalTradeBeacon.AllPowered(...)
                if (instruction.opcode == OpCodes.Call &&
                    instruction.operand is MethodInfo mi &&
                    mi == AccessTools.Method(typeof(Building_OrbitalTradeBeacon), nameof(Building_OrbitalTradeBeacon.AllPowered)))
                {
                    foundCallToBeacons = true;
                }

                // Extend the check: if (!Beacon.Any() && !DSU.Any())
                if (instruction.opcode == OpCodes.Brtrue_S && foundCallToBeacons)
                {
                    foundCallToBeacons = false;

                    yield return instruction; // Keep original branch
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(hiddenClass, "<>4__this"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PassingShip), "Map"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TradePatchHelper), nameof(TradePatchHelper.AnyPowerd), new[] { typeof(Map) }));
                    yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand);

                    continue;
                }

                yield return instruction;
            }
        }
    }

    /// <summary>
    /// Helper for evaluating whether any powered DSU/cold storage exists for trade logic.
    /// </summary>
    public static class TradePatchHelper
    {
        public static bool AnyPowerd(Map map)
        {
            return AllPowered(map, true).Any();
        }

        public static IEnumerable<Util.ILinkableStorageParentMulti> AllPowered(Map map, bool any = false)
        {
            // Old PRF DSU support
            foreach (Util.ILinkableStorageParentMulti item in map.listerBuildings.AllBuildingsColonistOfClass<Building_MassStorageUnitPowered>())
            {
                if (item.Powered)
                {
                    yield return item;
                    if (any) yield break;
                }
            }

            // MultiFloor ColdStorage support
            var cs = Util.PatchStorageUtilMulti.GetMFSMapComponent(map).ColdStorageBuildings
                .OfType<Util.ILinkableStorageParentMulti>();

            foreach (var item in cs)
            {
                yield return item;
                if (any) yield break;
            }
        }
    }
}