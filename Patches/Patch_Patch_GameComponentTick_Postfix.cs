using RimWorld;
using System.Collections.Generic;
using Verse;
using ProjectRimFactory.Storage;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Patches the FridgeUtility:Tick used to update .powerOutputInt for fridges
    /// This is needed to draw the correct amount of power for the Refrigerated DSU
    /// </summary>
    public static class Patch_FridgeUtility_Tick
    {
        public static void Postfix(List<CompPowerTrader> ___fridgeCache)
        {
            foreach (var item in ___fridgeCache)
            {
                if (item.parent is Building_MassStorageUnitPowered dsu)
                {
                    Util.FridgePowerPatchUtilMulti.FridgePowerDrawPerUnit.SetOrAdd(dsu, item.powerOutputInt);
                    item.powerOutputInt -= dsu.ExtraPowerDraw;
                }
            }
        }
    }
}
