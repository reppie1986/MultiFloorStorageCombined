using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Extends "Do until X" bill counting logic.
    /// Adds product counts from:
    /// - AssemblerQueue
    /// - ColdStorage (Multi DSUs)
    /// </summary>
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    public static class Patch_RecipeWorkerCounter_CountProducts
    {
        static void Postfix(RecipeWorkerCounter __instance, ref int __result, Bill_Production bill)
        {
            // Only run if "Include in Storage" is set to "Everywhere"
            if (bill.GetIncludeSlotGroup() == null)
            {
                Map billMap = bill.Map;
                ThingDef targetDef = __instance.recipe.products[0].thingDef;

                // Add items from AssemblerQueue
                var gameComp = Current.Game.GetComponent<Components.MFSGameComponent>();
                for (int i = 0; i < gameComp.AssemblerQueue.Count; i++)
                {
                    if (billMap != gameComp.AssemblerQueue[i].Map)
                        continue;

                    foreach (Thing heldThing in gameComp.AssemblerQueue[i].GetThingQueue())
                    {
                        TryUpdateResult(ref __result, targetDef, heldThing);
                    }
                }

                // Add items from ColdStorage DSUs
                var units = Util.PatchStorageUtilMulti.GetMFSMapComponent(billMap)
                    .ColdStorageBuildings
                    .OfType<Util.ILinkableStorageParentMulti>()
                    .ToList();

                foreach (Util.ILinkableStorageParentMulti dsu in units)
                {
                    foreach (Thing thing in dsu.StoredItems)
                    {
                        TryUpdateResult(ref __result, targetDef, thing);
                    }
                }
            }
        }

        private static void TryUpdateResult(ref int __result, ThingDef targetDef, Thing heldThing)
        {
            Thing inner = heldThing.GetInnerIfMinified();
            if (inner.def == targetDef)
            {
                __result += inner.stackCount;
            }
        }
    }
}