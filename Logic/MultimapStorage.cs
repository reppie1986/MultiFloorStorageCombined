// --------------------------------------------------------------------------------------
// File: MultimapStorage.cs
// Purpose: Multi-map aware DSU proxy for MultiFloorStorage mods.
// Class: Building_MassStorageUnitMultiMap
// - Acts as a "remote control" building to interact with a DSU (Deep Storage Unit) on another map.
// - Forwards all inventory and logic calls to a linked DSU, allowing seamless multi-map storage.
// - Exposes key info, actions, and UI to help players and devs debug and interact with remote storage.
// - *Does not* store items itself; all storage is performed remotely.
// --------------------------------------------------------------------------------------

using MultiFloorStorage.Util;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MultiFloorStorage.Logic
{
    /// <summary>
    /// Multi-map aware proxy for a remote DSU (Deep Storage Unit).
    /// This building does not store items itself — it forwards all interactions to a linked DSU on another map.
    /// </summary>
    public class Building_MassStorageUnitMultiMap : Building_Storage
    {
        // ──────── REMOTE DSU LINK STATE ─────────

        public int remoteTile = -1;      // Tile of the remote map
        public int remoteThingID = -1;   // ThingID of the remote DSU

        /// <summary>
        /// Attempts to resolve the remote DSU, first by Comp, then by tile+ID.
        /// </summary>
        public Buildings.Building_AbstractMultiStorageUnit GetRemoteStorage()
        {
            // Just ask the helper. That's its only job.
            return StorageLinkHelper.GetEffectiveStorage(this) as Buildings.Building_MassStorageUnitMulti;
        }

        // ──────── OVERRIDDEN METHODS ─────────

        // Whenever something is received, forward it to the linked DSU if available
        public override void Notify_ReceivedThing(Thing newItem)
        {
            var dsu = GetRemoteStorage();
            if (dsu != null)
            {
                dsu.Notify_ReceivedThing(newItem);
                Log.Message("[MultiMapDSU] Item received and forwarded to remote DSU.");
            }
            else
            {
                Log.Warning("[MultiMapDSU] Received item but remote DSU unavailable. Dropping fallback to base behavior.");
                base.Notify_ReceivedThing(newItem);
            }
        }

        // Adds a UI Gizmo (button) for logging remote DSU info, in addition to base gizmos
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;

            yield return new Command_Action
            {
                defaultLabel = "Log Remote DSU Info",
                defaultDesc = "Logs the linked remote DSU’s position and map info to the log.",
                action = () =>
                {
                    var dsu = GetRemoteStorage();
                    if (dsu != null)
                        Log.Message("[MultiMapDSU] Linked to DSU at position: " + dsu.Position + ", map: " + dsu.Map);
                    else
                        Log.Warning("[MultiMapDSU] No valid remote DSU linked.");
                }
            };
        }

        // Shows overlay with tile and ID of the linked DSU
        public override void DrawGUIOverlay()
        {
            base.DrawGUIOverlay();
            GenMapUI.DrawThingLabel(this, "Linked DSU\nTile=" + remoteTile + ", ID=" + remoteThingID);
        }

        /// <summary>
        /// Mirror the inventory of the remote DSU (for UI & pawn logic).
        /// </summary>
        public override string GetInspectString()
        {
            var baseStr = base.GetInspectString();
            var dsu = GetRemoteStorage();
            if (dsu == null)
                return baseStr + "\n[MultiMapDSU] No remote DSU linked.";

            return baseStr +
                   $"\nLinked to: {dsu.LabelCap} ({dsu.StoredItemsCount} stacks)\nMap: {dsu.Map.Tile}";
        }

        /// <summary>
        /// Proxy access to the remote DSU's stored items.
        /// </summary>
        public List<Thing> StoredItems => GetRemoteStorage()?.StoredItems ?? new List<Thing>();

        /// <summary>
        /// Proxy count of stored item stacks.
        /// </summary>
        public int StoredItemsCount => StoredItems.Count;

        /// <summary>
        /// Rebuild storage cache from the linked DSU.
        /// </summary>
        public void RefreshStorage()
        {
            var dsu = GetRemoteStorage();
            if (dsu == null)
            {
                Log.Warning("[MultiMapDSU] Cannot refresh; no DSU found.");
                return;
            }
            dsu.RefreshStorage();
            Log.Message("[MultiMapDSU] Triggered remote DSU RefreshStorage().");
        }

        // ──────── I/O INTERFACE PROXY ─────────

        // Proxy: can this DSU receive IO? (forwards to linked DSU)
        public virtual bool CanReceiveIO
        {
            get
            {
                var dsu = GetRemoteStorage();
                if (dsu == null)
                {
                    Log.Warning("[MultiMapDSU] CanReceiveIO: remote DSU is null.");
                    return false;
                }
                return dsu.CanReceiveIO;
            }
        }

        // Proxy: can this DSU receive this item? (forwards to linked DSU)
        public virtual bool CanReciveThing(Thing item)
        {
            var dsu = GetRemoteStorage();
            if (dsu == null)
            {
                Log.Warning("[MultiMapDSU] CanReciveThing: remote DSU is null.");
                return false;
            }
            return dsu.CanReciveThing(item);
        }

        // Proxy: output an item to this DSU (forwards to linked DSU)
        public virtual bool OutputItem(Thing item)
        {
            var dsu = GetRemoteStorage();
            if (dsu == null)
            {
                Log.Warning("[MultiMapDSU] OutputItem: remote DSU is null.");
                return false;
            }
            return dsu.OutputItem(item);
        }
    }
}
