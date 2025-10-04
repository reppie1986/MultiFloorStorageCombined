// --------------------------------------------------------------------------------------
// File: ConditionalPatchHelperMulti.cs
// Purpose: Dynamically enable/disable Harmony patches based on MultiFloorStorage usage.
// - Defines TogglePatch class to manage individual patch states.
// - Activates rendering + logic patches only when MFS mass storages are present.
// - Used to reduce patch overhead during inactive states.
// --------------------------------------------------------------------------------------

using HarmonyLib; // For dynamic patching
using MultiFloorStorage.Buildings; // For tracking active storage units
using MultiFloorStorage.Patches; // Patch class references
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace MultiFloorStorage.Util
{
    public static class ConditionalPatchHelperMulti
    {
        // Represents a single conditional patch that can be toggled
        public class TogglePatch
        {
            private bool patched = false; // Tracks current patch status
            public bool Status => patched; // Expose patch state

            private readonly MethodInfo base_m;
            private readonly HarmonyMethod trans_hm = null;
            private readonly HarmonyMethod pre_hm = null;
            private readonly HarmonyMethod post_hm = null;
            private readonly MethodInfo trans_m = null;
            private readonly MethodInfo pre_m = null;
            private readonly MethodInfo post_m = null;

            // Store all patch variants during construction
            public TogglePatch(MethodInfo base_method, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo Transpiler = null)
            {
                base_m = base_method;
                if (Transpiler != null) trans_hm = new HarmonyMethod(Transpiler);
                if (prefix != null) pre_hm = new HarmonyMethod(prefix);
                if (postfix != null) post_hm = new HarmonyMethod(postfix);
                trans_m = Transpiler;
                pre_m = prefix;
                post_m = postfix;
            }

            // Applies or removes the patch based on the desired state
            public void PatchHandler(bool patch)
            {
                if (patch && !patched)
                {
                    harmony_instance.Patch(base_m, pre_hm, post_hm, trans_hm);
                    patched = true;
                }
                else if (patched && !patch)
                {
                    if (trans_m != null) harmony_instance.Unpatch(base_m, trans_m);
                    if (pre_m != null) harmony_instance.Unpatch(base_m, pre_m);
                    if (post_m != null) harmony_instance.Unpatch(base_m, post_m);
                    patched = false;
                }
            }
        }

        // Harmony instance to manage all toggleable patches
        private static Harmony harmony_instance = null;

        // Patch declarations (target + prefix/postfix)
        public static TogglePatch Patch_Reachability_CanReach_Multi = new TogglePatch(
            AccessTools.Method(typeof(Verse.Reachability), "CanReach", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms) }),
            null,
            AccessTools.Method(typeof(Patches.Patch_Reachability_CanReach_Multi), "Postfix")
        );

        public static TogglePatch Patch_MinifiedThing_Print = new TogglePatch(
            AccessTools.Method(typeof(RimWorld.MinifiedThing), "Print", new Type[] { typeof(SectionLayer) }),
            AccessTools.Method(typeof(Patch_MinifiedThing_Print_Multi), "Prefix")
        );

        public static TogglePatch Patch_Thing_Print = new TogglePatch(
            AccessTools.Method(typeof(Thing), "Print", new Type[] { typeof(SectionLayer) }),
            AccessTools.Method(typeof(Patch_Thing_Print_Multi), "Prefix")
        );

        public static TogglePatch Patch_ThingWithComps_DrawGUIOverlay = new TogglePatch(
            AccessTools.Method(typeof(ThingWithComps), "DrawGUIOverlay"),
            AccessTools.Method(typeof(Patch_ThingWithComps_DrawGUIOverlay_Multi), "Prefix")
        );

        public static TogglePatch Patch_Thing_DrawGUIOverlay = new TogglePatch(
            AccessTools.Method(typeof(Thing), "DrawGUIOverlay"),
            AccessTools.Method(typeof(Patch_Thing_DrawGUIOverlay_Multi), "Prefix")
        );

        public static TogglePatch Patch_FloatMenuMakerMap_ChoicesAtFor = new TogglePatch(
            AccessTools.Method(typeof(RimWorld.FloatMenuMakerMap), "ChoicesAtFor", new Type[] { typeof(UnityEngine.Vector3), typeof(Pawn), typeof(bool) }),
            AccessTools.Method(typeof(Patch_FloatMenuMakerMap_ChoicesAtFor_Multi), "Prefix")
        );

        public static TogglePatch Patch_Building_Storage_Accepts = new TogglePatch(
            AccessTools.Method(typeof(RimWorld.Building_Storage), "Accepts", new Type[] { typeof(Thing) }),
            AccessTools.Method(typeof(Patch_Building_Storage_Accepts_Multi), "Prefix")
        );

        public static TogglePatch Patch_StorageSettings_AllowedToAccept = new TogglePatch(
            AccessTools.Method(typeof(RimWorld.StorageSettings), "AllowedToAccept", new Type[] { typeof(Thing) }),
            AccessTools.Method(typeof(Patch_StorageSettings_AllowedToAccept_Multi), "Prefix")
        );

        public static TogglePatch Patch_ForbidUtility_IsForbidden = new TogglePatch(
            AccessTools.Method(typeof(RimWorld.ForbidUtility), "IsForbidden", new Type[] { typeof(Thing), typeof(Pawn) }),
            AccessTools.Method(typeof(Patch_ForbidUtility_IsForbidden_Multi), "Prefix")
        );

        // Initializes the Harmony instance from the mod entry point
        public static void InitHarmony(Harmony harmony)
        {
            harmony_instance = harmony;
        }

        static List<Building_MassStorageUnitMulti> building_MassStorages = new(); // Track live MFS storages

        // Enable/disable patches based on whether MFS buildings exist
        private static void UpdatePatchStorage()
        {
            bool state = building_MassStorages.Count > 0;

            Patch_MinifiedThing_Print.PatchHandler(state);
            Patch_Thing_Print.PatchHandler(state);
            Patch_ThingWithComps_DrawGUIOverlay.PatchHandler(state);
            Patch_Thing_DrawGUIOverlay.PatchHandler(state);
            Patch_FloatMenuMakerMap_ChoicesAtFor.PatchHandler(state);
            Patch_Building_Storage_Accepts.PatchHandler(state);
            Patch_StorageSettings_AllowedToAccept.PatchHandler(state);
            Patch_ForbidUtility_IsForbidden.PatchHandler(state);
        }

        // Registers a new building and reevaluates patch status
        public static void Register(Building_MassStorageUnitMulti building)
        {
            building_MassStorages.Add(building);
            UpdatePatchStorage();
        }

        // Deregisters a building and reevaluates patch status
        public static void Deregister(Building_MassStorageUnitMulti building)
        {
            building_MassStorages.Remove(building);
            UpdatePatchStorage();
        }
    }
}
