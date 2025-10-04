using MultiFloorStorage.Buildings;
using RimWorld;
using UnityEngine;
using ProjectRimFactory.Storage;

namespace MultiFloorStorage.UI
{
    public class ITab_IOPortStorageMulti : ITab_Storage
    {
        public Building_StorageUnitIOBaseMulti SelBuilding => (Building_StorageUnitIOBaseMulti)SelThing;
        public override bool IsVisible => SelBuilding != null && SelBuilding.mode == StorageIOMode.Output;
        public ITab_IOPortStorageMulti()
        {
            size = new Vector2(300f, 480f);
            this.labelKey = "TabStorage";
        }
    }
}
