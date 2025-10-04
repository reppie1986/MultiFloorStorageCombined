using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MultiFloorStorage.Util
{
    /// <summary>
    /// Interface for buildings that can be linked to I/O ports or other storage logic in MultiFloorStorage.
    /// </summary>
    public interface ILinkableStorageParentMulti
    {
        List<Thing> StoredItems { get; }

        bool AdvancedIOAllowed { get; }

        void HandleNewItem(Thing item);
        void HandleMoveItem(Thing item);
        bool CanReciveThing(Thing item);
        bool HoldsPos(IntVec3 pos);

        void DeregisterPort(Buildings.Building_StorageUnitIOBaseMulti port);
        void RegisterPort(Buildings.Building_StorageUnitIOBaseMulti port);

        StorageSettings GetGetSettings();

        IntVec3 GetPosition { get; }
        string LabelCap { get; }
        bool CanReceiveIO { get; }
        Map Map { get; }

        int StoredItemsCount { get; }
        string GetITabString(int itemsSelected);
        LocalTargetInfo GetTargetInfo { get; }
        bool OutputItem(Thing item);
        bool Powered { get; }
        bool CanUseIOPort { get; }
    }
}