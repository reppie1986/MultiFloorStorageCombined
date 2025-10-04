namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Ensures items in ColdStorage contribute to map wealth.
    /// Runs every ~5000 ticks (1K items ~1ms).
    /// </summary>
    public static class Patch_WealthWatcher_CalculateWealthItems
    {
        public static void Postfix(Verse.Map ___map, ref float __result)
        {
            var buildings = Util.PatchStorageUtilMulti.GetMFSMapComponent(___map).ColdStorageBuildings;
            for (int i = 0; i < buildings.Count; i++)
            {
                __result += buildings[i].GetItemWealth();
            }
        }
    }
}