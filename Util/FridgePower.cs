using MultiFloorStorage.Buildings;
using ProjectRimFactory.SAL3;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MultiFloorStorage.Util
{
    public static class FridgePowerPatchUtilMulti
    {
        // Generalize the dictionary to accept any Building_Storage subclass
        public static Dictionary<Building, float> FridgePowerDrawPerUnit = new();

        public static void UpdatePowerDraw(Building dsu, CompPowerTrader powertrader, float extraPowerDraw)
        {
            if (!FridgePowerDrawPerUnit.TryGetValue(dsu, out float baseDraw))
            {
                // Reflect original base power draw
                baseDraw = -1f * (float)ReflectionUtility.CompProperties_Power_basePowerConsumption.GetValue(powertrader.Props);
                FridgePowerDrawPerUnit[dsu] = baseDraw;
            }

            // Apply custom draw with extra
            powertrader.powerOutputInt = baseDraw - extraPowerDraw;
        }
    }
}