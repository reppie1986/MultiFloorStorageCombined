// --------------------------------------------------------------------------------------
// File: Building_AbstractMultiStorageUnit.cs
// Purpose: Abstract and concrete classes for all major MultiFloorStorage (MFS) units
//          (Mass Storage, Powered, ColdStorage, etc.)
// Main Classes:
//   - Building_AbstractMultiStorageUnit: Base for all multi-floor storage, implements key MFS interfaces
//   - Building_MassStorageUnitMulti / PoweredMulti: Deep storage, with/without power
//   - Building_ColdStorageMulti / PoweredMulti: Refrigerated storage, with/without power
// Description:
//   - Provides a unified base for all custom storage units in MFS, encapsulating:
//       • Internal item container logic (ThingOwner)
//       • Pawn access/power checks
//       • Capacity display, custom overlays, custom labels
//       • Registration for advanced IO and custom UI
//   - Exposes abstract methods for child classes to handle storage specifics, new item logic, moving items, etc.
//   - Implements all interfaces needed for IO ports, pawn restriction, hiding, renaming, etc.
//   - All classes persist item storage and custom labels across saves.
//   - UI methods include overlays for capacity, rename dialogs, and pawn-access toggles.
// Dependencies:
//   - ProjectRimFactory.Storage.Editables, MultiFloorStorage.Util, MultiFloorStorage.Components
//   - Various RimWorld/Verse types (CompPowerTrader, ThingOwner, Dialogs, etc.)
// --------------------------------------------------------------------------------------

using MultiFloorStorage.UI;
using MultiFloorStorage.Util;
using ProjectRimFactory.Storage.Editables;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using System;
using MultiFloorStorage.Components;

namespace MultiFloorStorage.Buildings
{
    // Abstract base for all multi-floor storage units (Mass Storage, Powered, Cold, etc).
    public abstract class Building_AbstractMultiStorageUnit : Building_Storage, IHideItem, IHideRightClickMenu,
        IForbidPawnOutputItem, IForbidPawnInputItem, IRenameable, ILinkableStorageParentMulti, IStoreSettingsParent
    {
        // Unique, player-editable name for the building (persistent)
        #region Abstract Base Class
        protected string uniqueName;
        // Helper for outputting items to the map (e.g., auto-drop)
        protected StorageOutputUtilMulti outputUtil;
        // Mod extension for settings like capacity, pawn access, overlays, etc.
        protected DefModExtension_Crate ModExtension_Crate;

        // List of all things held by this storage unit (child must implement)
        public abstract List<Thing> StoredItems { get; }
        // Current number of stacks/items
        public int StoredItemsCount => StoredItems.Count;

        // Display name for renaming UI; falls back to base label if not renamed
        public string RenamableLabel { get => uniqueName ?? LabelCapNoCount; set => uniqueName = value; }

        // Storage settings for filters, priorities, etc.
        public StorageSettings GetGetSettings()
        {
            return settings;
        }

        // Save/load persistent fields (e.g., unique name)
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref uniqueName, "uniqueName");
        }

        // On spawn, setup helpers and cache mod extension
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            outputUtil = new StorageOutputUtilMulti(this);
            ModExtension_Crate ??= def.GetModExtension<DefModExtension_Crate>();
        }

        // --- Abstracts for child class logic ---
        public abstract void RefreshStorage();               // Implement item refresh/cleanup if needed
        public abstract void HandleNewItem(Thing item);      // How to store new items
        public abstract void HandleMoveItem(Thing item);     // How to remove/move items out

        // Display label with no count (for base label)
        public string BaseLabel => LabelCapNoCount;
        // Full inspect label
        public string InspectLabel => LabelCap;
        // Map position
        public IntVec3 GetPosition => Position;
        // Target info for UI
        public LocalTargetInfo GetTargetInfo => this;

        // --- Virtuals and mod-driven properties for override by children ---
        public virtual bool CanStoreMoreItems => true;
        public virtual bool Powered => true;
        public virtual bool CanReceiveIO => true;
        public bool CanUseIOPort => def.GetModExtension<DefModExtension_CanUseStorageIOPorts>() != null;
        public bool ForbidPawnAccess => ModExtension_Crate?.forbidPawnAccess ?? false;
        public virtual bool ForbidPawnInput => ForbidPawnAccess;
        public virtual bool ForbidPawnOutput => ForbidPawnAccess;
        public bool HideItems => ModExtension_Crate?.hideItems ?? false;
        public bool HideRightClickMenus => ModExtension_Crate?.hideRightClickMenus ?? false;
        public bool AdvancedIOAllowed => true;
        public virtual float ExtraPowerDraw => 0f;
		// Get the label for the ITab based on capacity limits
        public virtual string GetITabString(int selected) => ModExtension_Crate?.limit != int.MaxValue ? "PRFCrateUIThingLabel".Translate(StoredItemsCount, ModExtension_Crate.limit) : "PRFItemsTabLabel".Translate(StoredItemsCount, selected);
        public virtual float GetItemWealth()
        {
            float sum = 0f;
            foreach (var t in StoredItems) sum += t.MarketValue * t.stackCount;
            return sum;
        }

        // Try to output an item using the utility class
        public bool OutputItem(Thing t) => outputUtil.OutputItem(t);

        // Whether this storage can receive a specific thing (based on filter, IO state, and capacity)
        public virtual bool CanReciveThing(Thing t) => settings.AllowedToAccept(t) && CanReceiveIO && CanStoreMoreItems;

        // Checks if this storage covers a given cell (for overlays/UI)
        public bool HoldsPos(IntVec3 pos) => GenAdj.OccupiedRect(this).Contains(pos);

        // IO Port registration/deregistration (stubbed here; used in advanced IO)
        public void RegisterPort(Building_StorageUnitIOBaseMulti port) { }
        public void DeregisterPort(Building_StorageUnitIOBaseMulti port) { }

        // Abstract: child class must define capacity
        public abstract int MaxCapacity { get; }
        #endregion
        // Draws label and capacity as an overlay when zoomed in and mouseover
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();

            // Only draw the label when the camera is zoomed in close
            if (Current.CameraDriver.CurrentZoom <= CameraZoomRange.Close)
            {
                Rect screenRect = this.OccupiedRect().ToScreenRect();
                if (Mouse.IsOver(screenRect))
                {
                    // Get the capacity limit from the DefModExtension.
                    // We can reuse the GetITabString method to generate the capacity part of the label.
                    string labelText = $"{this.StoredItemsCount} / {this.MaxCapacity} stacks";
                    string fullLabel = this.RenamableLabel + "\n" + labelText;

                    // Draw the label
                    GenMapUI.DrawThingLabel(this, fullLabel);
                }
            }
        }		
    }

    // =====================================================================
    //  MASS STORAGE UNIT - GECORRIGEERD om een interne container te gebruiken
    // =====================================================================
    public class Building_MassStorageUnitMulti : Building_AbstractMultiStorageUnit, IThingHolder
    {
        // All stored items
        protected ThingOwner<Thing> innerContainer;
        public override List<Thing> StoredItems => innerContainer?.InnerListForReading ?? new List<Thing>();

        // Constructor: always create inner container
        public Building_MassStorageUnitMulti()
        {
            innerContainer = new ThingOwner<Thing>(this, false);
        }

        // Maximum capacity (from mod settings or XML, fallback 2048)
        public override int MaxCapacity
        {
            get
            {
                // This now checks YOUR mod's settings, not PRF's.
                if (MultiFloorStorage_Mod.Settings.overrideDsuLimit)
                {
                    return MultiFloorStorage_Mod.Settings.dsuLimit;
                }
                // Fallback to the limit defined in the building's XML file
                return this.ModExtension_Crate?.limit ?? 2048;
            }
        }

        // Save/load storage contents
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        // Tick for item decay, etc.
        public override void Tick()
        {
            base.Tick();
            innerContainer?.ThingOwnerTick();
        }

        // Required by IThingHolder (item stacking, minification, etc.)
        public ThingOwner GetDirectlyHeldThings() => innerContainer;
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            // Only needed if you add sub-containers.
        }

        public override void RefreshStorage() { }

        // Adds an item to storage (handles splitting/stacks)
        public override void HandleNewItem(Thing item) => innerContainer.TryAddOrTransfer(item);

        // Removes an item from storage (if it leaves)
        public override void HandleMoveItem(Thing item)
        {
            if (innerContainer.Contains(item))
                innerContainer.Remove(item);
        }

        // Adds a rename button to the building's gizmos
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename"),
                action = () => Find.WindowStack.Add(new Dialog_RenameMassStorageUnitMulti(this)),
                defaultLabel = "CommandRename".Translate()
            };
        }
    }

    // =====================================================================
    //  POWERED MASS STORAGE UNIT - Adds pawn access and power usage
    // =====================================================================
    public class Building_MassStorageUnitPoweredMulti : Building_MassStorageUnitMulti
    {
        // Cached power component
        private CompPowerTrader compPowerTrader;
        // Pawn access toggle state
        private bool pawnAccess = true;

        // Is the building powered (checks CompPowerTrader)
        public override bool Powered => compPowerTrader?.PowerOn ?? false;

        // On spawn, cache the power comp
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPowerTrader = GetComp<CompPowerTrader>();
        }

        // Toggle for pawn access, shown as a gizmo
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
            yield return new Command_Toggle
            {
                defaultLabel = "PRFPawnAccessLabel".Translate(),
                defaultDesc = "PRFPawnAccessDesc".Translate(),
                isActive = () => pawnAccess,
                toggleAction = () => pawnAccess = !pawnAccess,
                icon = ContentFinder<Texture2D>.Get("PRFUi/dsu", true)
            };
        }

        // On tick, update extra power draw and handle errors
        public override void Tick()
        {
            try
            {
                base.Tick();
                if (this.IsHashIntervalTick(60) && Powered && compPowerTrader != null)
                {
                    FridgePowerPatchUtilMulti.UpdatePowerDraw(this, compPowerTrader, ExtraPowerDraw);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[MFS] Tick error: {ex}");
            }
        }
    }

    // =====================================================================
    //  COLD STORAGE UNIT - Fridge-like storage, huge stacks
    // =====================================================================
    public class Building_ColdStorageMulti : Building_AbstractMultiStorageUnit, IThingHolder
    {
        // All stored items
        protected ThingOwner<Thing> innerContainer;
        public override List<Thing> StoredItems => innerContainer?.InnerListForReading ?? new List<Thing>();

        public Building_ColdStorageMulti()
        {
            innerContainer = new ThingOwner<Thing>(this, false);
        }

        // Large default cap (100,000), XML/mod-overridable
        public override int MaxCapacity
        {
            get
            {
                return this.ModExtension_Crate?.limit ?? 100000;
            }
        }

        // Save/load container
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        // Tick for item decay, etc.
        public override void Tick()
        {
            base.Tick();
            innerContainer?.ThingOwnerTick();
        }

        // Required by IThingHolder
        public ThingOwner GetDirectlyHeldThings() => innerContainer;
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            // Only add child containers if you have any (not used here)
        }

        // No special logic on refresh by default
        public override void RefreshStorage() { }

        // Adds an item to storage (handles splitting/stacks)
        public override void HandleNewItem(Thing item) => innerContainer.TryAddOrTransfer(item);

        // Removes an item from storage (if it leaves)
        public override void HandleMoveItem(Thing item)
        {
            if (innerContainer.Contains(item))
                innerContainer.Remove(item);
        }
                #region Gizmos
        // Adds a rename button to the building's gizmos
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename"),
                action = () => Find.WindowStack.Add(new Dialog_RenameColdStorageMulti(this)),
                defaultLabel = "CommandRename".Translate()
            };
        }
                #endregion
    }

    // =====================================================================
    //  POWERED COLD STORAGE UNIT - Adds pawn access and dynamic power
    // =====================================================================
    public class Building_ColdStoragePoweredMulti : Building_ColdStorageMulti
    {
        // Cached power component
        private CompPowerTrader compPowerTrader;
        // Pawn access toggle state
        private bool pawnAccess = true;

        // Powered state from comp
        public override bool Powered => compPowerTrader?.PowerOn ?? false;

        // Each item adds 10W draw (example logic; tweak as needed)
        public override float ExtraPowerDraw => StoredItems.Count * 10f;

        // On spawn, cache power comp
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPowerTrader = GetComp<CompPowerTrader>();
        }

        // Tick for extra power draw, error-safe
        public override void Tick()
        {
            try
            {
                base.Tick(); // Already ticks innerContainer in parent
                if (this.IsHashIntervalTick(60) && Powered && compPowerTrader != null)
                {
                    FridgePowerPatchUtilMulti.UpdatePowerDraw(this, compPowerTrader, ExtraPowerDraw);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[MFS] ColdStoragePoweredMulti.Tick error: {ex}");
            }
        }

        // Toggle for pawn access, shown as a gizmo
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;
            yield return new Command_Toggle
            {
                defaultLabel = "PRFPawnAccessLabel".Translate(),
                defaultDesc = "PRFPawnAccessDesc".Translate(),
                isActive = () => pawnAccess,
                toggleAction = () => pawnAccess = !pawnAccess,
                icon = ContentFinder<Texture2D>.Get("PRFUi/dsu", true)
            };
        }
    }
}