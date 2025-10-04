// Dialog_MapAndUnitSelector.cs
// Provides a two-step UI to pick exactly one Deep Storage Unit from allowed types across all floors.

using System;
using ProjectRimFactory.Storage; // Import DSU classes: Building_ColdStoragePowered, Building_MassStorageUnitPowered
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace MultiFloorStorage.UI
{
    /// <summary>
    /// Dialog to select a map (floor) and then a specific Deep Storage Unit building of allowed defNames.
    /// </summary>
    public class Dialog_MapAndUnitSelector : Window
    {
        private readonly List<Map> maps;
        private readonly List<string> allowedDefNames;
        private readonly Action<Building> onUnitSelected;
        private Map selectedMap;

        public override Vector2 InitialSize => new Vector2(400f, 300f);

        /// <param name="maps">All loaded maps (floors).</param>
        /// <param name="allowedDefNames">List of DSU defNames to show.</param>
        /// <param name="onUnitSelected">Callback invoked with the chosen building or null.</param>
        public Dialog_MapAndUnitSelector(List<Map> maps, List<string> allowedDefNames, Action<Building> onUnitSelected)
            : base()
        {
            this.maps = maps ?? new List<Map>();
            this.allowedDefNames = allowedDefNames ?? new List<string>();
            this.onUnitSelected = onUnitSelected;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            Log.Message($"[DSULinker] Dialog opened: {this.maps.Count} floors, {this.allowedDefNames.Count} allowed types.");
            if (this.maps.Count == 0)
            {
                Log.Error("[DSULinker] No maps loaded; closing selector immediately.");
                onUnitSelected?.Invoke(null);
                this.Close(true);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                var listing = new Listing_Standard();
                listing.Begin(inRect);

                // Step 1: floor selection
                if (selectedMap == null)
                {
                    listing.Label("Select a floor (map) to list DSUs:");
                    for (int i = 0; i < maps.Count; i++)
                    {
                        var map = maps[i];
                        string label = $"Floor {i + 1} (Tile {map.Tile})";
                        if (listing.ButtonText(label))
                        {
                            selectedMap = map;
                            Log.Message($"[DSULinker] Floor {i + 1} selected (Tile={map.Tile}).");
                        }
                    }
                    if (listing.ButtonText("Cancel"))
                    {
                        Log.Message("[DSULinker] Floor selection cancelled.");
                        onUnitSelected?.Invoke(null);
                        Close(true);
                    }
                }
                else // Step 2: DSU selection on chosen map
                {
                    int floorIndex = maps.IndexOf(selectedMap) + 1;
                    listing.Label($"Select a Deep Storage Unit on Floor {floorIndex}:");

                    var dsus = selectedMap.listerThings.AllThings
                        .Where(t => t is Buildings.Building_ColdStoragePoweredMulti || t is Buildings.Building_MassStorageUnitPoweredMulti)
                        .Cast<Building>()
                        .Where(b => allowedDefNames.Contains(b.def.defName))
                        .ToList();

                    Log.Message($"[DSULinker] Found {dsus.Count} allowed DSU(s) on Floor {floorIndex}.");

                    if (!dsus.Any())
                    {
                        listing.Label("No allowed DSUs found on this floor.");
                    }
                    else
                    {
                        foreach (var dsu in dsus)
                        {
                            if (listing.ButtonText(dsu.LabelCap)) // << FIXED
                            {
                                Log.Message($"[DSULinker] DSU selected: {dsu.def.defName} ('{dsu.LabelCap}') on Tile={dsu.Map.Tile}."); // << FIXED
                                onUnitSelected?.Invoke(dsu);
                                Close(true);
                                return;
                            }
                        }
                    }
                    if (listing.ButtonText("Back"))
                    {
                        Log.Message("[DSULinker] Returning to floor selection.");
                        selectedMap = null;
                    }
                }

                listing.End();
            }
            catch (Exception e)
            {
                Log.Error($"[DSULinker] Exception in DoWindowContents: {e}");
                onUnitSelected?.Invoke(null);
                Close(true);
            }
        }
    }
}