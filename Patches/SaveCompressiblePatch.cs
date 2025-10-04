using HarmonyLib;
using ProjectRimFactory.Common;
using RimWorld;
using Verse;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Prevents items inside MassStorageUnitMulti from being compressed by save system.
    /// </summary>
    [HarmonyPatch(typeof(CompressibilityDeciderUtility), "IsSaveCompressible")]
    public static class SaveCompressiblePatchMulti
    {
        private static T Get<T>(Map map, IntVec3 pos) where T : class
        {
            return pos.IsValid ? pos.GetFirst<T>(map) : null;
        }

        public static void Postfix(Thing t, ref bool __result)
        {
            if (!__result || t.Map == null)
                return;

            var dsu = Get<Buildings.Building_MassStorageUnitMulti>(t.Map, t.Position);
            if (dsu != null)
            {
                // Items inside active DSUs should always be saved
                __result = false;
            }
        }
    }
}