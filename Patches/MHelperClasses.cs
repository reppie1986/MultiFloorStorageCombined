// MultiFloorStorage: MHelperClasses.cs
// Harmony patches for suppressing overlays, float menus, and storage behavior
// when items are hidden or storage forbids pawn interaction

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using MultiFloorStorage.Components;
using MultiFloorStorage.Util;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Prevents item from being considered forbidden if the MFS map component allows output at that position.
    /// </summary>
    class Patch_ForbidUtility_IsForbidden_Multi
    {
        static bool Prefix(Thing t, Pawn pawn, out bool __result)
        {
            __result = true;
            if (t != null)
            {
                Map map = t.Map;
                if (map != null && t.def.category == ThingCategory.Item)
                {
                    if (PatchStorageUtilMulti.GetMFSMapComponent(map)?.ShouldForbidPawnOutputAtPos(t.Position) ?? false)
                        return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Prevents storage from accepting an item if input is forbidden and position mismatches.
    /// </summary>
    class Patch_Building_Storage_Accepts_Multi
    {
        static bool Prefix(Building_Storage __instance, Thing t, out bool __result)
        {
            __result = false;
            if (!PatchStorageUtilMulti.SkipAcceptsPatch && (__instance as Components.IForbidPawnInputItem)?.ForbidPawnInput == true)
            {
                if (__instance.Position != t.Position)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Prevents storage settings from allowing an item if input is forbidden and position mismatches.
    /// </summary>
    class Patch_StorageSettings_AllowedToAccept_Multi
    {
        static bool Prefix(IStoreSettingsParent ___owner, Thing t, out bool __result)
        {
            __result = false;
            if (___owner is Building_Storage storage)
            {
                if (!PatchStorageUtilMulti.SkipAcceptsPatch && (storage as IForbidPawnInputItem)?.ForbidPawnInput == true)
                {
                    if (storage.Position != t.Position)
                        return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Hides float menu options if the click position is hidden on the MFS map component.
    /// </summary>
    class Patch_FloatMenuMakerMap_ChoicesAtFor_Multi
    {
        static bool Prefix(Vector3 clickPos, Pawn pawn, out List<FloatMenuOption> __result)
        {
            if (pawn.Map.GetComponent<MFSMapComponent>()?.ShouldHideRightMenus(clickPos.ToIntVec3()) == true)
            {
                __result = new List<FloatMenuOption>();
                return false;
            }
            __result = null;
            return true;
        }
    }

    /// <summary>
    /// Prevents drawing GUI overlay for items that should be hidden by the MFS component.
    /// </summary>
    class Patch_Thing_DrawGUIOverlay_Multi
    {
        static bool Prefix(Thing __instance)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtilMulti.GetMFSMapComponent(__instance.Map)?.ShouldHideItemsAtPos(__instance.Position) ?? false)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Prevents drawing GUI overlay for ThingWithComps if hidden by MFS component.
    /// </summary>
    class Patch_ThingWithComps_DrawGUIOverlay_Multi
    {
        static bool Prefix(Thing __instance)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtilMulti.GetMFSMapComponent(__instance.Map)?.ShouldHideItemsAtPos(__instance.Position) ?? false)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Prevents printing items on the map layer if hidden by MFS component.
    /// </summary>
    class Patch_Thing_Print_Multi
    {
        static bool Prefix(Thing __instance, SectionLayer layer)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtilMulti.GetMFSMapComponent(__instance.Map)?.ShouldHideItemsAtPos(__instance.Position) ?? false)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Prevents printing minified things on the map layer if hidden by MFS component.
    /// </summary>
    class Patch_MinifiedThing_Print_Multi
    {
        static bool Prefix(Thing __instance, SectionLayer layer)
        {
            if (__instance.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtilMulti.GetMFSMapComponent(__instance.Map)?.ShouldHideItemsAtPos(__instance.Position) ?? false)
                    return false;
            }
            return true;
        }
    }
}
