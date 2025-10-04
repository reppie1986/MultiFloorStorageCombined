// --------------------------------------------------------------------------------------
// File: MFS_DrawPatches.cs
// Purpose: Harmony patches to hide overlays and prevent rendering for hidden items in MultiFloorStorage.
// - Patches both overlays (UI icons, labels) and Print (map rendering) for items that are "hidden" by MFS storage logic.
// - Checks the map component (PatchStorageUtilMulti.GetMFSMapComponent) for whether an item should be hidden.
// --------------------------------------------------------------------------------------

using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using MultiFloorStorage.Util;

namespace MultiFloorStorage.Patches
{
    // Patch: Hides GUI overlays (icons, stack labels) for items hidden by MFS
    [HarmonyPatch(typeof(Thing), nameof(Thing.DrawGUIOverlay))]
    public static class Patch_Thing_DrawGUIOverlay_MFS
    {
        static bool Prefix(Thing __instance)
        {
            // Only applies to items
            if (__instance.def.category == ThingCategory.Item)
            {
                // If the item is at a hidden location, skip overlay
                if (PatchStorageUtilMulti.GetMFSMapComponent(__instance.Map)?.HasHiddenItemAt(__instance.Position) == true)
                    return false;
            }
            return true; // Otherwise, allow normal overlay
        }
    }

    // Patch: Hides GUI overlays for items with comps (same logic)
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.DrawGUIOverlay))]
    public static class Patch_ThingWithComps_DrawGUIOverlay_MFS
    {
        static bool Prefix(ThingWithComps __instance)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtilMulti.GetMFSMapComponent(__instance.Map)?.HasHiddenItemAt(__instance.Position) == true)
                    return false;
            }
            return true;
        }
    }

    // Patch: Prevents map rendering for hidden items (so they're not drawn at all)
    [HarmonyPatch(typeof(Thing), nameof(Thing.Print))]
    public static class Patch_Thing_Print_MFS
    {
        static bool Prefix(Thing __instance, SectionLayer layer)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtilMulti.GetMFSMapComponent(__instance.Map)?.HasHiddenItemAt(__instance.Position) == true)
                    return false;
            }
            return true;
        }
    }

    // Patch: Prevents map rendering for hidden minified items (e.g. blueprints, minified buildings)
    [HarmonyPatch(typeof(MinifiedThing), nameof(MinifiedThing.Print))]
    public static class Patch_MinifiedThing_Print_MFS
    {
        static bool Prefix(MinifiedThing __instance, SectionLayer layer)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtilMulti.GetMFSMapComponent(__instance.Map)?.HasHiddenItemAt(__instance.Position) == true)
                    return false;
            }
            return true;
        }
    }
}