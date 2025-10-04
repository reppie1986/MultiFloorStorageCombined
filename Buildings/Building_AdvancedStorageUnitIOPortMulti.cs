// --------------------------------------------------------------------------------------
// File: Building_AdvancedStorageUnitIOPortMulti.cs
// Purpose: Advanced output-only IO Port for MultiFloorStorage.
//          Handles item placement queueing, global registration, and strict pawn restriction.
// Class: Building_AdvancedStorageUnitIOPortMulti
// - Always operates in Output mode (cannot be changed by player).
// - Placement queue: stores incoming items until port is ready to receive.
// - Registers/deregisters itself with MFSMapComponent for global advanced IO lookups.
// - Forbids all pawn input (ForbidPawnInput always true).
// - Only places queued items if output cell is free and powered.
// --------------------------------------------------------------------------------------

using MultiFloorStorage.Components;
using ProjectRimFactory.Storage;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MultiFloorStorage.Buildings
{
    public class Building_AdvancedStorageUnitIOPortMulti : Buildings.Building_StorageUnitIOBaseMulti
    {
        // Items waiting to be placed at this port
        private readonly List<Thing> placementQueue = new();

        // Hides the min/max gizmo in the UI (always false)
        public override bool ShowLimitGizmo => false;

        // Prevents pawns from ever placing items here
        public override bool ForbidPawnInput => true;

        // This port is always in Output mode
        public override StorageIOMode IOMode
        {
            get => StorageIOMode.Output;
            set { } // intentionally does nothing
        }

        // Identifies this port as an "advanced" IO port for linking/logic
        public override bool IsAdvancedPort => true;

        // External logic can add items to be placed at this port
        public void AddItemToQueue(Thing thing)
        {
            placementQueue.Add(thing);
        }

        // On spawn, register this port in the global map registry for advanced IO ports
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            MFSMapComponent.GetForMap(map)?.RegisterAdvancedIOLocation(Position, this);
        }

        // On despawn, deregister from the map registry
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            MFSMapComponent.GetForMap(Map)?.DeregisterAdvancedIOLocation(Position);
            base.DeSpawn(mode);
        }

        // Checks if the port can receive a new item (cell is empty, powered on)
        public bool CanGetNewItem => GetStoredItem() == null && (powerComp?.PowerOn ?? false);

        // Returns the item currently stored at this port (if any)
        private Thing GetStoredItem()
        {
            return Map == null ? null : WorkPosition.GetFirstItem(Map);
        }

        // Try to place the next queued item if possible
        public void UpdateQueue()
        {
            if (CanGetNewItem && placementQueue.Count > 0)
            {
                var nextItem = placementQueue[0];
                PlaceThingNow(nextItem);
                placementQueue.RemoveAt(0);
            }
        }

        // Actually places the item at this port's position
        public void PlaceThingNow(Thing thing)
        {
            if (thing != null)
            {
                thing.Position = Position;
                // Optionally: GenSpawn.Spawn(thing, Position, Map);
            }
        }

        // Each tick: process the placement queue, and refresh input logic if needed
        public override void Tick()
        {
            base.Tick();
            UpdateQueue();

            // Every 10 ticks: if the stored item is not reserved, refresh input (cleanup, triggers logic)
            if (this.IsHashIntervalTick(10))
            {
                var stored = GetStoredItem();
                if (stored != null && !Map.reservationManager.AllReservedThings().Contains(stored))
                {
                    RefreshInput();
                }
            }
        }
    }
}