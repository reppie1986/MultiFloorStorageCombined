// --------------------------------------------------------------------------------------
// File: Building_IOPusher.cs
// Purpose: Output pusher building for MultiFloorStorage.
//          Always operates in Output mode, pushes items to a cell in the facing direction.
// Class: Building_IOPusherMulti
// - Output-only port, not advanced.
// - WorkPosition: cell in front of the building, based on its rotation.
// - Sets its mode on spawn (for safety).
// Class: PlaceWorker_IOPusherHilight
// - Draws a placement ghost to show output cell during building placement.
// - Uses a custom color for clarity.
// --------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using Verse;
using ProjectRimFactory.Storage;

namespace MultiFloorStorage.Buildings
{
    // Output pusher building, always output mode
    public class Building_IOPusherMulti : Building_StorageUnitIOBaseMulti
    {
        // The cell in front of the building (in its facing direction)
        public override IntVec3 WorkPosition => this.Position + this.Rotation.FacingCell;


        public override StorageIOMode IOMode { get => StorageIOMode.Output; set => _ = value; }
// does nothing, just for interface compatibility

        // This is not an advanced port
        public override bool IsAdvancedPort => false;

        // On spawn: set the mode to Output for clarity/safety
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            base.mode = IOMode;
        }
    }

    // PlaceWorker for drawing the output cell as a ghost (placement preview)
    class PlaceWorker_IOPusherHilight : PlaceWorker
    {
        // Shows the output cell with a colored border when placing the building
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            IntVec3 outputCell = center + rot.FacingCell;

            GenDraw.DrawFieldEdges(new List<IntVec3> { outputCell }, Util.CommonColorsMulti.outputCell);
        }
    }
}