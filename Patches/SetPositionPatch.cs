using HarmonyLib;
using ProjectRimFactory.Common;
using ProjectRimFactory.Storage;
using Verse;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Notifies a MassStorageUnitMulti if an item inside it is moved out (via Position set).
    /// </summary>
    [HarmonyPatch(typeof(Thing), "set_Position")]
    public static class SetPositionPatchMulti
    {
        private static T Get<T>(Map map, IntVec3 pos) where T : class
        {
            return pos.IsValid ? pos.GetFirst<T>(map) : null;
        }

        public static void Prefix(IntVec3 value, Thing __instance, out Buildings.Building_MassStorageUnitMulti __state)
        {
            __state = null;
            if (__instance.def.category != ThingCategory.Item || !__instance.Position.IsValid)
                return;

            var map = __instance.Map;
            if (map != null)
            {
                var building = Get<Buildings.Building_MassStorageUnitMulti>(map, __instance.Position);

                // Avoid notifying if this is just an internal move within the same DSU
                if (building != null && building.Position != value)
                {
                    __state = building;
                }
            }
        }

        public static void Postfix(Thing __instance, Buildings.Building_MassStorageUnitMulti __state)
        {
            __state?.Notify_LostThing(__instance);
        }
    }
}