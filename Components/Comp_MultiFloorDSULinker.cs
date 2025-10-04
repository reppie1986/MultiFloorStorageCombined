// --------------------------------------------------------------------------------------
// File: Comp_MultiFloorDSULinker.cs
// Purpose: MultiFloorStorage Component for linking a Deep Storage Unit (DSU) to another
//          DSU across maps/floors, controlled by player via Gizmo.
// Main Class: Comp_MultiFloorDSULinker
// Description:
//   - This component is attached to a building to allow the player to link it to a target DSU.
//   - Handles the selection, storage, and validation of a DSU link, persisting across saves.
//   - Provides Gizmo UI for "Link" and "Unlink" in the game (shows only when appropriate).
//   - Handles logic for which buildings are valid link targets, using allowed DefNames and type checks.
//   - Uses a float menu to display all candidate DSUs on all maps, grouped by map.
//   - Stores the linked DSU as a reference; clears it if the target becomes invalid.
//   - Dependencies: Util.ILinkableStorageParentMulti, CompProperties_MultiFloorDSULinker, TexCommand
// --------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using MultiFloorStorage.Buildings; // For ILinkableStorageParentMulti

namespace MultiFloorStorage.Components
{
    /// <summary>
    /// ThingComp that holds a linked DSU building reference.
    /// This component provides the "Link" button (Gizmo) to the player.
    /// </summary>
    public class Comp_MultiFloorDSULinker : ThingComp
    {
        // Holds a reference to the currently linked DSU (can be on another map/floor)
        private Building linkedDSU;

        /// <summary>
        /// Property to get/set the linked DSU.
        /// Setter only allows valid ILinkableStorageParentMulti or clears if invalid.
        /// Logs a message when link is set/cleared.
        /// </summary>
        public Building LinkedDSU
        {
            get => linkedDSU;
            internal set
            {
                // Only accept valid storage parent (multi-link capable)
                if (value is Util.ILinkableStorageParentMulti)
                {
                    linkedDSU = value;
                }
                else
                {
                    linkedDSU = null;
                }

                var msg = linkedDSU != null
                    ? $"[DSULinker] Linked to DSU '{linkedDSU.LabelCap}' on Tile={linkedDSU.Map.Tile}."
                    : "[DSULinker] DSU link cleared.";
                Log.Message(msg);
            }
        }

        /// <summary>
        /// Gets the XML-defined properties for this Comp (see Defs).
        /// </summary>
        public CompProperties_MultiFloorDSULinker Props => (CompProperties_MultiFloorDSULinker)props;

        /// <summary>
        /// Save/load logic for persisting the linked DSU reference across saves.
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref linkedDSU, "linkedDSU");
        }

        /// <summary>
        /// Provides extra Gizmos ("Link" and "Unlink" buttons) in the UI for this building.
        /// - "Link" opens a FloatMenu listing all valid DSUs on all maps, grouped by map.
        /// - "Unlink" only shows if a DSU is currently linked; clears the link when clicked.
        /// </summary>
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Pass through base gizmos first (if any)
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            // --- LINK GIZMO ---
            yield return new Command_Action
            {
                defaultLabel = "Link Deep Storage Unit",
                defaultDesc = "Select which DSU this building should target for I/O.",
                icon = TexCommand.Attack,
                action = () =>
                {
                    var options = new List<FloatMenuOption>();

                    // Find all valid, selectable DSUs across all maps
                    foreach (var map in Find.Maps)
                    {
                        // Only DSUs allowed by XML DefNames and implementing interface
                        var dsusOnMap = map.listerBuildings.allBuildingsColonist
                            .Where(b => b is Util.ILinkableStorageParentMulti && Props.allowedDSUdefNames.Contains(b.def.defName))
                            .ToList();

                        if (dsusOnMap.Any())
                        {
                            // Group by map with a separator option
                            options.Add(new FloatMenuOption($"-- Map (Tile {map.Tile}) --", null));

                            foreach (var dsu in dsusOnMap)
                            {
                                var option = new FloatMenuOption(dsu.LabelCap, () => {
                                    // Link to the chosen DSU
                                    this.LinkedDSU = dsu;
                                });
                                options.Add(option);
                            }
                        }
                    }

                    if (options.Count == 0)
                    {
                        options.Add(new FloatMenuOption("No linkable storage found.", null));
                    }

                    // Show the menu to the player
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };

            // --- UNLINK GIZMO (shows only if linked) ---
            if (linkedDSU != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Unlink Deep Storage Unit",
                    defaultDesc = "Clear the DSU link to revert to default I/O behavior.",
                    icon = TexCommand.ClearPrioritizedWork,
                    action = () =>
                    {
                        this.LinkedDSU = null;
                    }
                };
            }
        }
    }
}