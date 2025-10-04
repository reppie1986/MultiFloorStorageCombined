// --------------------------------------------------------------------------------------
// File: Building_StorageUnitIOPortMulti.cs
// Purpose: Standard (non-advanced) IO Port for MultiFloorStorage—handles player-selectable Input/Output mode, item transfer, port linking, and UI controls.
// Classes: Building_StorageUnitIOPortMulti, DefModExtension_StorageUnitIOPortColor
// - Implements key transfer logic for IO ports (refactored from base), player mode switching, and custom color overlays via mod extension.
// --------------------------------------------------------------------------------------

using ProjectRimFactory.Storage.UI;
using ProjectRimFactory.Storage;
using ProjectRimFactory;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using MFUtil = MultiFloorStorage.Util;
using MultiFloorStorage.Components;
using MultiFloorStorage.UI;

namespace MultiFloorStorage.Buildings
{
    [StaticConstructorOnStartup]
    public class Building_StorageUnitIOPortMulti : Building_StorageUnitIOBaseMulti
    {
        // Property for current IO mode (Input/Output), notifies system on change
        public override StorageIOMode IOMode
        {
            get => mode;
            set
            {
                if (mode == value) return;
                mode = value;
                Notify_NeedRefresh();
            }
        }

        // This is not an advanced port
        public override bool IsAdvancedPort => false;

        // Transfers items from port to linked DSU (input mode)
        public override void RefreshInput()
        {
            if (powerComp.PowerOn)
            {
                Thing item = Position.GetFirstItem(Map);
                if (mode == StorageIOMode.Input && item != null && (BoundStorageUnit?.CanReciveThing(item) ?? false))
                {
                    // Use the safe transfer method
                    Thing itemToMove = item.SplitOff(item.stackCount);
                    BoundStorageUnit.HandleNewItem(itemToMove);
                }
            }
        }

        // Helper: Absorb a specific number of stacks from toBeAbsorbed into baseThing, updating map/UI
        private static bool AbsorbAmmount(ref Thing baseThing, ref Thing toBeAbsorbed, int count)
        {
            if (!baseThing.CanStackWith(toBeAbsorbed))
            {
                return false;
            }
            int num = count;

            // Weighted average HP if item uses hitpoints
            if (baseThing.def.useHitPoints)
            {
                baseThing.HitPoints = Mathf.CeilToInt((float)(baseThing.HitPoints * baseThing.stackCount + toBeAbsorbed.HitPoints * num) / (float)(baseThing.stackCount + num));
            }

            baseThing.stackCount += num;
            toBeAbsorbed.stackCount -= num;
            if (baseThing.Map != null)
            {
                baseThing.DirtyMapMesh(baseThing.Map);
            }
            if (baseThing.Spawned)
            {
                baseThing.Map.listerMergeables.Notify_ThingStackChanged(baseThing);
            }
            if (toBeAbsorbed.stackCount <= 0)
            {
                toBeAbsorbed.Destroy();
                return true;
            }
            return false;
        }

        // Transfers items from DSU to port (output mode), with all stacking/min/max logic
        protected override void RefreshOutput()
        {
            if (powerComp.PowerOn)
            {
                Thing currentItem = Position.GetFirstItem(Map);
                bool storageSlotAvailable = currentItem == null || (settings.AllowedToAccept(currentItem) && OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit));
                if (BoundStorageUnit != null && BoundStorageUnit.CanReceiveIO)
                {
                    if (storageSlotAvailable)
                    {
                        List<Thing> itemCandidates = new List<Thing>(from Thing t in BoundStorageUnit.StoredItems where settings.AllowedToAccept(t) select t);
                        if (ItemsThatSatisfyMin(ref itemCandidates, currentItem))
                        {
                            foreach (Thing item in itemCandidates)
                            {
                                if (currentItem != null)
                                {
                                    if (currentItem.CanStackWith(item))
                                    {
                                        int count = Math.Min(item.stackCount, OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit));
                                        if (count > 0)
                                        {
                                            Thing Mything = item;
                                            AbsorbAmmount(ref currentItem, ref Mything, count);
                                            if (Mything.stackCount <= 0) BoundStorageUnit.HandleMoveItem(Mything);
                                        }
                                    }
                                }
                                else
                                {
                                    int count = OutputSettings.CountNeededToReachMax(0, item.stackCount);
                                    if (count > 0)
                                    {
                                        var ThingToRemove = item.SplitOff(count);
                                        if (item.stackCount <= 0 || ThingToRemove == item) BoundStorageUnit.HandleMoveItem(item);
                                        currentItem = GenSpawn.Spawn(ThingToRemove, Position, Map);
                                    }
                                }
                                if (currentItem != null && !OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit))
                                {
                                    break;
                                }
                            }
                        }
                    }
                    // If item in port is no longer allowed, send it back to DSU
                    if (currentItem != null && (!settings.AllowedToAccept(currentItem) || !OutputSettings.SatisfiesMin(currentItem.stackCount)) && BoundStorageUnit.GetGetSettings().AllowedToAccept(currentItem))
                    {
                        currentItem.SetForbidden(false, false);
                        BoundStorageUnit.HandleNewItem(currentItem);
                    }
                    // If item in port is over max, split and send to DSU
                    if (currentItem != null && (!OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit) && BoundStorageUnit.GetGetSettings().AllowedToAccept(currentItem)))
                    {
                        int splitCount = -OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit);
                        if (splitCount > 0)
                        {
                            Thing returnThing = currentItem.SplitOff(splitCount);
                            returnThing.SetForbidden(false, false);
                            BoundStorageUnit.HandleNewItem(returnThing);
                        }
                    }
                    // Set forbidden state for pawns (based on settings)
                    if (currentItem != null)
                    {
                        currentItem.SetForbidden(ForbidOnPlacement, false);
                    }
                }
            }
        }

        // Adds the IO mode (Input/Output) toggle as a gizmo in the UI
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos()) yield return g;
            yield return new Command_Action()
            {
                defaultLabel = "PRFIOMode".Translate() + ": " + (IOMode == StorageIOMode.Input ? "PRFIOInput".Translate() : "PRFIOOutput".Translate()),
                action = () =>
                {
                    Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>()
                    {
                        new FloatMenuOption("PRFIOInput".Translate(), () => SelectedPorts().ToList().ForEach(p => p.IOMode = StorageIOMode.Input)),
                        new FloatMenuOption("PRFIOOutput".Translate(), () => SelectedPorts().ToList().ForEach(p => p.IOMode = StorageIOMode.Output))
                    }));
                },
                icon = IOModeTex
            };
        }

        // Returns all currently selected IO ports for bulk operations
        private IEnumerable<Building_StorageUnitIOPortMulti> SelectedPorts()
        {
            var l = Find.Selector.SelectedObjects.Where(o => o is Building_StorageUnitIOPortMulti).Select(o => (Building_StorageUnitIOPortMulti)o).ToList();
            if (!l.Contains(this))
            {
                l.Add(this);
            }
            return l;
        }

        // Places an item at this port if allowed, respecting forbidden and stacking logic
        public override bool OutputItem(Thing thing)
        {
            if (BoundStorageUnit?.CanReceiveIO ?? false)
            {
                return GenPlace.TryPlaceThing(thing.SplitOff(thing.stackCount), Position, Map, ThingPlaceMode.Near,
                    null, pos =>
                    {
                        if (settings.AllowedToAccept(thing) && OutputSettings.SatisfiesMin(thing.stackCount))
                            if (pos == Position)
                                return true;
                        foreach (Thing t in Map.thingGrid.ThingsListAt(pos))
                        {
                            if (t is Building_StorageUnitIOPortMulti) return false;
                        }
                        return true;
                    });
            }
            return false;
        }
    }

    // Mod extension for per-port color overlays (input/output)
    public class DefModExtension_StorageUnitIOPortColor : DefModExtension
    {
        public Color inColor;
        public Color outColor;
    }
}
