// --------------------------------------------------------------------------------------
// File: MFSMapComponent.cs
// Purpose: Handles all multi-floor storage (MFS) logic specific to one RimWorld Map.
//          Tracks storage-related states, registrations, and cell/blocking info per map.
// Main Class: MFSMapComponent (extends MapComponent)
// Description:
//   - Stores collections for hiding UI menus, restricting pawn output/input, and tracking advanced I/O ports.
//   - Handles registration of "Cold Storage" buildings and other special DSU types for cross-map coordination.
//   - Ties into the GlobalMFSManager for world-level access and cross-map operations.
//   - Used by almost all MFS-enabled buildings and components for registering themselves.
//   - Also defines interfaces for hiding items/menus, restricting pawn movement, etc.
//   - GlobalMFSManager: Tracks all MFSMapComponents by mapID and globally tracks all DSU buildings.
// --------------------------------------------------------------------------------------

using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Linq;
using MultiFloorStorage.Components;

namespace MultiFloorStorage.Components
{
    /// <summary>
    /// MapComponent responsible for tracking all MultiFloorStorage data and logic on a specific RimWorld map.
    /// Handles registration of DSUs, advanced I/O ports, menu/UI hiding, and cell-based restrictions.
    /// Linked to global manager for multi-map support.
    /// </summary>
    public class MFSMapComponent : MapComponent
    {
        // Used to hide context menus (right-click) on certain cells/things
        private readonly HashSet<IHideRightClickMenu> rightClickHiders = new();

        // Track which cells should hide items for UI/logic
        private readonly Dictionary<IntVec3, List<IHideItem>> hideItemsAt = new();

        // Track which cells should forbid pawn output (i.e. pawns can't take from here)
        private readonly Dictionary<IntVec3, List<IForbidPawnOutputItem>> forbidOutputsAt = new();

        // Track which cells should forbid pawn input (i.e. pawns can't drop here)
        private readonly Dictionary<IntVec3, List<IForbidPawnInputItem>> forbidInputsAt = new();

        // Tracks the location and reference for advanced storage I/O ports on this map
        private readonly Dictionary<IntVec3, Buildings.Building_StorageUnitIOBaseMulti> advancedIOLocations = new();

        // List of all registered ColdStorageMulti buildings on this map
        public List<Buildings.Building_ColdStorageMulti> ColdStorageBuildings = new List<Buildings.Building_ColdStorageMulti>();

        /// <summary>
        /// Exposes advanced IO port locations as dictionary
        /// </summary>
        public Dictionary<IntVec3, Buildings.Building_StorageUnitIOBaseMulti> GetAdvancedIOLocations()
            => advancedIOLocations;

        /// <summary>
        /// Exposes advanced IO port locations (property version)
        /// </summary>
        public Dictionary<IntVec3, Buildings.Building_StorageUnitIOBaseMulti> AdvancedIOLocations => advancedIOLocations;

        /// <summary>
        /// MapComponent constructor, registers itself globally.
        /// </summary>
        public MFSMapComponent(Map map) : base(map)
        {
            GlobalMFSManager.Instance.Register(map.uniqueID, this);
        }

        /// <summary>
        /// Called after all world/building state is initialized (game load/new map).
        /// Finds and registers all relevant DSU buildings on this map with global manager.
        /// </summary>
        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // Find all DSU types (deep storage units) on this map and register globally
            foreach (Building building in this.map.listerBuildings.allBuildingsColonist)
            {
                if (building is Buildings.Building_ColdStorageMulti ||
                    building is Buildings.Building_ColdStoragePoweredMulti ||
                    building is Buildings.Building_MassStorageUnitMulti ||
                    building is Buildings.Building_MassStorageUnitPoweredMulti)
                {
                    GlobalMFSManager.Instance.RegisterDSU(building);
                }
            }
        }

        // --- Registration/deregistration for right-click menu hiders ---
        public void RegisterHideRightClickMenu(IHideRightClickMenu obj) => rightClickHiders.Add(obj);
        public void DeregisterHideRightClickMenu(IHideRightClickMenu obj) => rightClickHiders.Remove(obj);

        // --- Register/deregister ColdStorage buildings for special handling ---
        public void RegisterColdStorageBuilding(Buildings.Building_ColdStorageMulti port)
        {
            if (!ColdStorageBuildings.Contains(port))
            {
                ColdStorageBuildings.Add(port);
            }
        }
        public void DeRegisterColdStorageBuilding(Buildings.Building_ColdStorageMulti port)
        {
            if (ColdStorageBuildings.Contains(port))
            {
                ColdStorageBuildings.Remove(port);
            }
        }

        // --- UI/logic: check if any object is hiding right-click menu at a cell ---
        public bool HasHideRightClickAt(IntVec3 cell)
        {
            foreach (var h in rightClickHiders)
                if (h is Thing t && t.OccupiedRect().Contains(cell))
                    return true;
            return false;
        }
        public bool ShouldHideRightMenus(IntVec3 cell) => HasHideRightClickAt(cell);

        // --- Register/deregister hiding items at cell positions ---
        public void RegisterIHideItemPos(IntVec3 pos, IHideItem item)
        {
            if (!hideItemsAt.TryGetValue(pos, out var list))
                hideItemsAt[pos] = list = new();
            if (!list.Contains(item)) list.Add(item);
        }
        public void DeRegisterIHideItemPos(IntVec3 pos, IHideItem item)
        {
            if (hideItemsAt.TryGetValue(pos, out var list))
                list.Remove(item);
        }
        public bool HasHiddenItemAt(IntVec3 pos) =>
            hideItemsAt.TryGetValue(pos, out var list) && list.Count > 0;
        public bool ShouldHideItemsAtPos(IntVec3 pos) =>
            hideItemsAt.TryGetValue(pos, out var list) && list.Any(i => i.HideItems);

        // --- Register/deregister restricting pawn output/input at cell positions ---
        public void RegisterIForbidPawnOutputItem(IntVec3 pos, IForbidPawnOutputItem item)
        {
            if (!forbidOutputsAt.TryGetValue(pos, out var list))
                forbidOutputsAt[pos] = list = new();
            if (!list.Contains(item)) list.Add(item);
        }
        public void DeRegisterIForbidPawnOutputItem(IntVec3 pos, IForbidPawnOutputItem item)
        {
            if (forbidOutputsAt.TryGetValue(pos, out var list))
                list.Remove(item);
        }
        public bool ForbidPawnOutputAt(IntVec3 pos) =>
            forbidOutputsAt.TryGetValue(pos, out var list) && list.Any(i => i.ForbidPawnOutput);
        public bool ShouldForbidPawnOutputAtPos(IntVec3 pos) => ForbidPawnOutputAt(pos);

        public void RegisterIForbidPawnInputItem(IntVec3 pos, IForbidPawnInputItem item)
        {
            if (!forbidInputsAt.TryGetValue(pos, out var list))
                forbidInputsAt[pos] = list = new();
            if (!list.Contains(item)) list.Add(item);
        }
        public void DeRegisterIForbidPawnInputItem(IntVec3 pos, IForbidPawnInputItem item)
        {
            if (forbidInputsAt.TryGetValue(pos, out var list))
                list.Remove(item);
        }
        public bool ForbidPawnInputAt(IntVec3 pos) =>
            forbidInputsAt.TryGetValue(pos, out var list) && list.Any(i => i.ForbidPawnInput);
        public bool ShouldForbidPawnInputAtPos(IntVec3 pos) => ForbidPawnInputAt(pos);

        // --- Register/deregister advanced IO locations ---
        public void RegisterAdvancedIOLocation(IntVec3 pos, Buildings.Building_StorageUnitIOBaseMulti port)
        {
            if (!advancedIOLocations.ContainsKey(pos))
                advancedIOLocations[pos] = port;
        }
        public void DeregisterAdvancedIOLocation(IntVec3 pos)
        {
            if (advancedIOLocations.ContainsKey(pos))
                advancedIOLocations.Remove(pos);
        }

        /// <summary>
        /// Global lookup wrapper for the MFSMapComponent for a given map.
        /// </summary>
        public static MFSMapComponent GetForMap(Map map) =>
            GlobalMFSManager.Instance.Get(map.uniqueID);
    }

    /// <summary>
    /// Tracks all MFSMapComponents globally by map ID, and all DSU buildings world-wide.
    /// Used for multi-map linking and management.
    /// </summary>
    public class GlobalMFSManager : GameComponent
    {
        private static GlobalMFSManager instance;
        public static GlobalMFSManager Instance => instance ??= Current.Game.GetComponent<GlobalMFSManager>();

        private readonly Dictionary<int, MFSMapComponent> mapComps = new();

        // Master list of all DSU buildings (deep storage) across all maps
        public List<Building> AllDSUs { get; private set; } = new List<Building>();

        public GlobalMFSManager(Game game) : base() => instance = this;

        public void Register(int mapID, MFSMapComponent comp) => mapComps[mapID] = comp;

        public MFSMapComponent Get(int mapID)
        {
            mapComps.TryGetValue(mapID, out var comp);
            return comp;
        }

        public void RegisterDSU(Building dsu)
        {
            if (!AllDSUs.Contains(dsu))
            {
                AllDSUs.Add(dsu);
            }
        }
        public void DeregisterDSU(Building dsu)
        {
            if (AllDSUs.Contains(dsu))
            {
                AllDSUs.Remove(dsu);
            }
        }
    }

    // --- Interfaces for item/menu/pawn input/output logic (per-building) ---
    public interface IHideItem { bool HideItems { get; } }
    public interface IForbidPawnOutputItem { bool ForbidPawnOutput { get; } }
    public interface IHideRightClickMenu { bool HideRightClickMenus { get; } }

    public interface IForbidPawnInputItem : ISlotGroupParent, IHaulDestination { bool ForbidPawnInput { get; }
    }
}