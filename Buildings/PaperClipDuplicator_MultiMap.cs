// --------------------------------------------------------------------------------------
// File: PaperClipDuplicator_MultiMap.cs
// Purpose: Multi-map paperclip generator and storage tracker for MultiFloorStorage mods.
// Class: Building_PaperclipDuplicatorMulti
// - Generates (virtually) exponentially increasing number of "paperclips".
// - Tracks paperclip count and syncs with ticks, handles overflow.
// - Can be linked to a MassStorageUnitMulti for cross-map paperclip storage tracking.
// - Shows both internal and storage-linked paperclip count in inspect pane.
// - Includes debug gizmo for devs to double paperclip count (with overflow safety).
// --------------------------------------------------------------------------------------

using MultiFloorStorage.Util;
using ProjectRimFactory.Common;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace MultiFloorStorage.Buildings
{
    public class Building_PaperclipDuplicatorMulti : Building
    {
        // Internal (virtual) count of paperclips owned by this building
        private long paperclipCount;
        // Last game tick the count was updated
        private int lastTick = Find.TickManager.TicksGame;
        // Linked storage unit (optional, can be used for item transfer)
        public Building_MassStorageUnitMulti BoundStorageUnit;

        private CompOutputAdjustable outputComp;
        private CompPowerTrader powerComp;

        /// <summary>
        /// Cross-map aware DSU accessor.
        /// Uses Comp_MultiFloorDSULinker if available.
        /// </summary>
        public Building_MassStorageUnitMulti EffectiveDSU
        {
            get
            {
                return StorageLinkHelper.GetEffectiveStorage<Building_MassStorageUnitMulti>(this);
            }
        }

        // Property: Gets current actual number of paperclips (with exponential growth)
        public long PaperclipsActual
        {
            get
            {
                long result = 0;
                if (paperclipCount != long.MaxValue)
                {
                    try
                    {
                        checked
                        {
                            // Simulate exponential growth by 5% per day since lastTick
                            result = (long)(paperclipCount * Math.Pow(1.05, (Find.TickManager.TicksGame - lastTick).TicksToDays()));
                        }
                    }
                    catch (OverflowException)
                    {
                        // Hit the max value (overflow)! Clamp and notify.
                        Find.WindowStack.Add(new Dialog_MessageBox("PRF_ArchoCipher_BankOverflow".Translate()));
                        PaperclipsActual = long.MaxValue;
                        result = long.MaxValue;
                    }
                }
                return result;
            }
            set
            {
                paperclipCount = value;
                lastTick = Find.TickManager.TicksGame;
            }
        }

        // Add paperclips (increments total, uses property to update tick)
        public virtual void DepositPaperclips(int count)
        {
            PaperclipsActual += count;
        }

        // Remove paperclips (decrements total)
        public virtual void WithdrawPaperclips(int count)
        {
            PaperclipsActual -= count;
        }

        // Setup component references
        public override void PostMake()
        {
            base.PostMake();
            outputComp = GetComp<CompOutputAdjustable>();
            powerComp = GetComp<CompPowerTrader>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            outputComp = GetComp<CompOutputAdjustable>();
            powerComp = GetComp<CompPowerTrader>();
        }

        protected override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            // (Stub - can be extended for signals)
        }

        // Shows total paperclips in this building and in the linked storage unit (if any)
        public override string GetInspectString()
        {
            StringBuilder builder = new StringBuilder();
            string str = base.GetInspectString();
            if (!string.IsNullOrEmpty(str))
            {
                builder.AppendLine(str);
            }

            builder.AppendLine("PaperclipsInDuplicator".Translate(PaperclipsActual.ToString()));

            if (EffectiveDSU != null)
            {
                builder.AppendLine("PaperclipsInStorageUnit".Translate(
                    EffectiveDSU.StoredItems.Where(t => t.def == PRFDefOf.Paperclip).Sum(t => t.stackCount)));
            }
            else
            {
                builder.AppendLine("PRFNoBoundStorageUnit".Translate());
            }

            return builder.ToString().TrimEndNewlines();
        }

        // Dev mode: adds a debug gizmo to double the paperclip count (safe for overflow)
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Prefs.DevMode)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "DEBUG: Double paperclip amount",
                    action = () =>
                    {
                        try
                        {
                            checked
                            {
                                if (PaperclipsActual != long.MaxValue)
                                {
                                    PaperclipsActual *= 2;
                                }
                            }
                        }
                        catch (OverflowException)
                        {
                            Find.WindowStack.Add(new Dialog_MessageBox("PRF_ArchoCipher_BankOverflow".Translate()));
                            PaperclipsActual = long.MaxValue;
                        }
                    }
                };
            }
        }

        // Save/load state for all persistent fields
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref paperclipCount, "paperclipCount");
            Scribe_Values.Look(ref lastTick, "lastTick");
            Scribe_References.Look(ref BoundStorageUnit, "BoundStorageUnit");
        }
    }
}
