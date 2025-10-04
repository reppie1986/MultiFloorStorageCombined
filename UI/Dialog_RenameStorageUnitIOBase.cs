using RimWorld;
using Verse;

namespace MultiFloorStorage.UI
{
    public class Dialog_RenameStorageUnitIOBaseMulti : Dialog_Rename<Buildings.Building_StorageUnitIOBaseMulti>
    {

        public Dialog_RenameStorageUnitIOBaseMulti(Buildings.Building_StorageUnitIOBaseMulti renaming) : base(renaming)
        {
            // TODO Check if we need that line
            curName = ((IRenameable)renaming).RenamableLabel;
        }

        protected override void OnRenamed(string name)
        {
            base.OnRenamed(name);
            // TODO Check if we still need to set that
            //building.UniqueName = curName;
            Messages.Message("PRFStorageBuildingGainsName".Translate(curName), MessageTypeDefOf.TaskCompletion);
        }
    }
}

