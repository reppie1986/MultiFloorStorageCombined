// --------------------------------------------------------------------------------------
// File: Building_StorageUnitIOBaseMulti.cs
// Purpose: Abstract base for all MultiFloorStorage I/O port buildings.
//           Handles advanced linking to DSUs, input/output mode, pawn restriction, UI gizmos, and per-port settings.
// Classes: Building_StorageUnitIOBaseMulti (abstract)
// - Handles Scribe data, I/O logic, multi-map linking, mod extension integration, and RimWorld interface hooks.
// - Extends: Building_Storage, implements IForbidPawnInputItem, IRenameable
// - Key fields: StorageIOMode, BoundStorageUnit, OutputSettings, uniqueName
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
    public abstract class Building_StorageUnitIOBaseMulti : Building_Storage, Components.IForbidPawnInputItem, IRenameable
    {
        // --- Textures for UI icons ---
        public static readonly Texture2D CargoPlatformTex = ContentFinder<Texture2D>.Get("Storage/CargoPlatform");
        public static readonly Texture2D IOModeTex = ContentFinder<Texture2D>.Get("PRFUi/IoIcon");

        // Current port mode: Input or Output
        public StorageIOMode mode;
        // The storage building this IO port is linked to (only for saving; not used in logic)
        private Building linkedStorageParentBuilding;
        // Storage filter for output (output mode only)
        protected StorageSettings outputStoreSettings;
        // Min/max output settings for output mode
        private OutputSettings outputSettings;

        // Main work position (defaults to this.Position; override in children if needed)
        public virtual IntVec3 WorkPosition => this.Position;
        // Power component, if present
        protected CompPowerTrader powerComp;

        // Indicates if this is an advanced IO port (children override as needed)
        public abstract bool IsAdvancedPort { get; }
        // Whether to show the min/max limit gizmo in the UI
        public virtual bool ShowLimitGizmo => true;

        // --- BoundStorageUnit links this IO port to a storage parent (DSU) for cross-map linking ---
        public MFUtil.ILinkableStorageParentMulti BoundStorageUnit
        {
            get => MFUtil.StorageLinkHelper.GetEffectiveStorage(this) as MFUtil.ILinkableStorageParentMulti;
            set
            {
                BoundStorageUnit?.DeregisterPort(this);
                linkedStorageParentBuilding = value as Building;
                value?.RegisterPort(this);
                Notify_NeedRefresh();
            }
        }

        // --- Renaming support ---
        private string uniqueName;
        public string RenamableLabel { get => uniqueName ?? LabelCapNoCount; set => uniqueName = value; }
        public string BaseLabel => LabelCapNoCount;
        public string InspectLabel => LabelCap;

        private static readonly Texture2D RenameTex = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

        // Pawn-forbid toggle for placement (used for output mode)
        private bool forbidOnPlacement = false;
        public virtual bool ForbidOnPlacement => forbidOnPlacement;

        // Changes the graphic based on IO mode and any color mod extensions
        public override Graphic Graphic
        {
            get
            {
                var graphic = base.Graphic;
                var colorExtension = this.def.GetModExtension<DefModExtension_StorageUnitIOPortColor>();
                if (colorExtension != null)
                {
                    graphic = graphic.GetColoredVersion(base.Graphic.Shader, this.IOMode == StorageIOMode.Input ? colorExtension.inColor : colorExtension.outColor, Color.white);
                }
                return graphic;
            }
        }

        // IO Mode property, triggers refresh on change
        public virtual StorageIOMode IOMode
        {
            get => mode;
            set
            {
                if (mode == value) return;
                mode = value;
                Notify_NeedRefresh();
            }
        }

        // Settings for output mode (min/max, tooltips)
        protected OutputSettings OutputSettings
        {
            get
            {
                if (outputSettings == null)
                {
                    outputSettings = new OutputSettings("IOPort_Minimum_UseTooltip", "IOPort_Maximum_UseTooltip");
                }
                return outputSettings;
            }
            set => outputSettings = value;
        }

        // Pawn input restriction for this port (true if output mode and max reached)
        public virtual bool ForbidPawnInput
        {
            get
            {
                if (IOMode == StorageIOMode.Output && OutputSettings.useMax)
                {
                    Thing currentItem = WorkPosition.GetFirstItem(Map);
                    if (currentItem != null)
                    {
                        return OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit) <= 0;
                    }
                }
                return false;
            }
        }

        // The item at this port (if any)
        public Thing NPDI_Item => WorkPosition.GetFirstItem(this.Map);

        // --- Save/load all important fields ---
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref mode, "mode");
            Scribe_References.Look(ref linkedStorageParentBuilding, "boundStorageUnit");
            Scribe_Deep.Look(ref outputStoreSettings, "outputStoreSettings", this);
            Scribe_Deep.Look(ref outputSettings, "outputSettings", "IOPort_Minimum_UseTooltip", "IOPort_Maximum_UseTooltip");
            Scribe_Values.Look(ref uniqueName, "uniqueName");
            Scribe_Values.Look(ref forbidOnPlacement, "forbidOnPlacement");
        }

        // --- RimWorld inspection panel ---
        public override string GetInspectString()
        {
            if (OutputSettings.useMin && OutputSettings.useMax)
                return base.GetInspectString() + "\n" + "IOPort_Minimum".Translate(OutputSettings.min) + "\n" + "IOPort_Maximum".Translate(OutputSettings.max);
            else if (OutputSettings.useMin && !OutputSettings.useMax)
                return base.GetInspectString() + "\n" + "IOPort_Minimum".Translate(OutputSettings.min);
            else if (!OutputSettings.useMin && OutputSettings.useMax)
                return base.GetInspectString() + "\n" + "IOPort_Maximum".Translate(OutputSettings.max);
            else
                return base.GetInspectString();
        }

        // --- Initialization ---
        public override void PostMake()
        {
            base.PostMake();
            powerComp = GetComp<CompPowerTrader>();
            outputStoreSettings = new StorageSettings(this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            if (BoundStorageUnit?.Map != map && (linkedStorageParentBuilding?.Spawned ?? false))
            {
                this.GetComp<Comp_MultiFloorDSULinker>().LinkedDSU = null;
            }
            this.def.building.groupingLabel = this.LabelCapNoCount;
        }

        // React to power-on signal
        protected override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == CompPowerTrader.PowerTurnedOnSignal)
            {
                Notify_NeedRefresh();
            }
        }

        // Unregister this IO port from its bound storage when despawning
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn();
            BoundStorageUnit?.DeregisterPort(this);
        }

        // --- Core logic: triggers refresh for store/input/output logic when needed ---
        public void Notify_NeedRefresh()
        {
            RefreshStoreSettings();
            switch (IOMode)
            {
                case StorageIOMode.Input:
                    RefreshInput();
                    break;
                case StorageIOMode.Output:
                    RefreshOutput();
                    break;
            }
        }

        // When receiving a new thing (input)
        public override void Notify_ReceivedThing(Thing newItem)
        {
            base.Notify_ReceivedThing(newItem);
            if (mode == StorageIOMode.Input)
            {
                RefreshInput();
            }
        }

        // When losing a thing (output)
        public override void Notify_LostThing(Thing newItem)
        {
            base.Notify_LostThing(newItem);
            if (mode == StorageIOMode.Output)
            {
                RefreshOutput();
            }
        }

        // Ticks every 10 ticks for periodic refresh
        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(10))
            {
                Notify_NeedRefresh();
            }
        }

        // --- Synchronizes the StorageSettings for this port (delegates to bound storage or own output settings) ---
        public void RefreshStoreSettings()
        {
            if (IOMode == StorageIOMode.Output)
            {
                settings = outputStoreSettings;
                if (BoundStorageUnit != null && settings.Priority != BoundStorageUnit.GetGetSettings().Priority)
                {
                    settings.Priority = BoundStorageUnit.GetGetSettings().Priority;
                }
            }
            else if (BoundStorageUnit != null)
            {
                settings = BoundStorageUnit.GetGetSettings();
            }
            else
            {
                settings = new StorageSettings(this);
            }
        }

        // --- Input mode: moves item from port to the bound storage ---
        public virtual void RefreshInput()
        {
            if (powerComp?.PowerOn != true) return;

            Thing item = this.WorkPosition.GetFirstItem(this.Map);

            if (mode == StorageIOMode.Input && item != null && (BoundStorageUnit?.CanReciveThing(item) ?? false))
            {
                Thing itemToMove = item.SplitOff(item.stackCount);
                BoundStorageUnit.HandleNewItem(itemToMove);
            }
        }

        // --- Utility for filtering output items by min ---
        protected bool ItemsThatSatisfyMin(ref List<Thing> itemCandidates, Thing currentItem)
        {
            if (currentItem != null)
            {
                itemCandidates = itemCandidates.Where(t => currentItem.CanStackWith(t)).ToList();
                int minRequired = OutputSettings.useMin ? outputSettings.min : 0;
                int count = currentItem.stackCount;
                int i = 0;
                while (i < itemCandidates.Count && count < minRequired)
                {
                    count += itemCandidates[i].stackCount;
                    i++;
                }
                return OutputSettings.SatisfiesMin(count);
            }
            return itemCandidates.GroupBy(t => t.def)
                .FirstOrDefault(g => OutputSettings.SatisfiesMin(g.Sum(t => t.stackCount)))?.Any() ?? false;
        }

        // --- Output mode: moves item(s) from the bound storage to this port ---
        protected virtual void RefreshOutput()
        {
            if (powerComp.PowerOn)
            {
                Thing currentItem = WorkPosition.GetFirstItem(Map);
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
                                            var ThingToRemove = item.SplitOff(count);
                                            if (item.stackCount <= 0) BoundStorageUnit.HandleMoveItem(item);
                                            currentItem.TryAbsorbStack(ThingToRemove, true);
                                        }
                                    }
                                }
                                else
                                {
                                    int count = OutputSettings.CountNeededToReachMax(0, item.stackCount);
                                    if (count > 0)
                                    {
                                        var ThingToRemove = item.SplitOff(count);
                                        if (item.stackCount <= 0) BoundStorageUnit.HandleMoveItem(item);
                                        currentItem = GenSpawn.Spawn(ThingToRemove, WorkPosition, Map);
                                    }
                                }
                                if (currentItem != null && !OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit))
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if (currentItem != null && (!settings.AllowedToAccept(currentItem) || !OutputSettings.SatisfiesMin(currentItem.stackCount)) && BoundStorageUnit.GetGetSettings().AllowedToAccept(currentItem))
                    {
                        currentItem.SetForbidden(false, false);
                        BoundStorageUnit.HandleNewItem(currentItem);
                    }
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
                    if (currentItem != null)
                    {
                        currentItem.SetForbidden(ForbidOnPlacement, false);
                    }
                }
            }
        }

        // --- UI: adds gizmos for linking, renaming, min/max output and forbid toggle ---
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos()) yield return g;

            // Main link menu
            yield return new Command_Action()
            {
                defaultLabel = "PRFBoundStorageBuilding".Translate() + ": " + ((BoundStorageUnit as Building)?.LabelCap ?? "NoneBrackets".Translate()),
                action = () =>
                {
                    var options = new List<FloatMenuOption>();
                    foreach (var map in Find.Maps)
                    {
                        var dsusOnMap = map.listerBuildings.allBuildingsColonist
                            .Where(b => b is MFUtil.ILinkableStorageParentMulti && (b as MFUtil.ILinkableStorageParentMulti).CanUseIOPort)
                            .ToList();
                        if (IsAdvancedPort)
                            dsusOnMap.RemoveAll(b => !(b as MFUtil.ILinkableStorageParentMulti).AdvancedIOAllowed);

                        if (dsusOnMap.Any())
                        {
                            options.Add(new FloatMenuOption($"-- Map (Tile {map.Tile}) --", null));
                            foreach (var dsu in dsusOnMap)
                            {
                                var option = new FloatMenuOption(dsu.LabelCap, () => {
                                    SelectedPorts().ToList().ForEach(p => p.BoundStorageUnit = (dsu as MFUtil.ILinkableStorageParentMulti));
                                });
                                options.Add(option);
                            }
                        }
                    }

                    if (options.Count == 0)
                    {
                        options.Add(new FloatMenuOption("No linkable storage found.", null));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                },
                icon = CargoPlatformTex
            };
            // Rename
            yield return new Command_Action
            {
                icon = RenameTex,
                action = () => Find.WindowStack.Add(new Dialog_RenameStorageUnitIOBaseMulti(this)),
                hotKey = KeyBindingDefOf.Misc1,
                defaultLabel = "PRFRenameMassStorageUnitLabel".Translate(),
                defaultDesc = "PRFRenameMassStorageUnitDesc".Translate()
            };
            // Min/max output setting
            if (IOMode == StorageIOMode.Output && ShowLimitGizmo)
            {
                yield return new Command_Action()
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel"),
                    defaultLabel = "PRFIOOutputSettings".Translate(),
                    action = () => Find.WindowStack.Add(new Dialog_OutputMinMax(OutputSettings, () => SelectedPorts().Where(p => p.IOMode == StorageIOMode.Output).ToList().ForEach(p => p.OutputSettings.Copy(this.OutputSettings))))
                };
            }
            // Forbid on placement toggle (output only)
            if (mode == StorageIOMode.Output)
            {
                yield return new Command_Toggle()
                {
                    isActive = () => this.forbidOnPlacement,
                    toggleAction = () => this.forbidOnPlacement = !this.forbidOnPlacement,
                    defaultLabel = "PRF_Toggle_ForbidOnPlacement".Translate(),
                    defaultDesc = "PRF_Toggle_ForbidOnPlacementDesc".Translate(),
                    icon = forbidOnPlacement ? TexCommand.ForbidOn : TexCommand.ForbidOff
                };
            }
        }

        // --- Utility: gets all currently selected IO ports (for mass linking/settings) ---
        private IEnumerable<Building_StorageUnitIOBaseMulti> SelectedPorts()
        {
            var l = Find.Selector.SelectedObjects.Where(o => o is Building_StorageUnitIOBaseMulti).Select(o => (Building_StorageUnitIOBaseMulti)o).ToList();
            if (!l.Contains(this))
            {
                l.Add(this);
            }
            return l;
        }

        // --- Output an item from storage if allowed ---
        public virtual bool OutputItem(Thing thing)
        {
            if (BoundStorageUnit?.CanReceiveIO ?? false)
            {
                return GenPlace.TryPlaceThing(thing.SplitOff(thing.stackCount), WorkPosition, Map, ThingPlaceMode.Near,
                    null, pos =>
                    {
                        if (settings.AllowedToAccept(thing) && OutputSettings.SatisfiesMin(thing.stackCount))
                            if (pos == WorkPosition)
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
}
