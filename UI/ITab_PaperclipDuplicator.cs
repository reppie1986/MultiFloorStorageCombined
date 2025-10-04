// --------------------------------------------------------------------------------------
// File: ITab_PaperclipDuplicator.cs
//
// AI SUMMARY:
// This file creates the user interface tab for the "Paperclip Duplicator" building.
// The UI allows a player to link the duplicator to a mass storage unit, toggle
// between depositing and withdrawing paperclips, enter an amount, and execute the
// transaction. It handles all the back-end logic for moving the paperclips to and
// from the linked storage.
// --------------------------------------------------------------------------------------

using ProjectRimFactory.Common;
using ProjectRimFactory.Storage;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MultiFloorStorage.UI
{
    // Defines the custom "Paperclip Duplicator" tab.
    public class ITab_PaperclipDuplicatorMulti : ITab
    {
        // Constructor that sets the tab's size and label.
        public ITab_PaperclipDuplicatorMulti()
        {
            size = new Vector2(400f, 250f);
            labelKey = "PRFPaperclipDuplicatorTab";
        }
        
        // A helper property to get the currently selected duplicator building.
        public Buildings.Building_PaperclipDuplicatorMulti SelBuilding => (Buildings.Building_PaperclipDuplicatorMulti)SelThing;
        
        // Main method that draws all the UI elements within the tab.
        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Listing_Standard listing = new Listing_Standard(GameFont.Small);
            listing.Begin(rect);
            listing.Label(SelBuilding.LabelCap);
            if (listing.ButtonTextLabeled("PRFBoundStorageBuilding".Translate(), SelBuilding.BoundStorageUnit?.LabelCap ?? "NoneBrackets".Translate()))
            {
                List<FloatMenuOption> list = (from Buildings.Building_MassStorageUnitMulti b in Find.CurrentMap.listerBuildings.AllBuildingsColonistOfClass<Buildings.Building_MassStorageUnitMulti>()
                                              select new FloatMenuOption(b.LabelCap, () => SelBuilding.BoundStorageUnit = b)).ToList();
                if (list.Count == 0)
                {
                    list.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
            listing.GapLine();
            listing.Label("PRFDepositWithdraw".Translate());
            if (listing.ButtonTextLabeled("PRFDepositWithdrawMode".Translate(), (deposit ? "PRFDeposit" : "PRFWithdraw").Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>()
                {
                    new FloatMenuOption("PRFDeposit".Translate(), () =>
                    {
                        deposit = true;
                    }),
                    new FloatMenuOption("PRFWithdraw".Translate(), () =>
                    {
                        deposit = false;
                    })
                }));
            }
            amountTextArea = listing.TextEntryLabeled("PRFAmount".Translate(), amountTextArea);
            if (listing.ButtonText("PRFDepositWithdraw".Translate()))
            {
                HandleDepositWithdrawRequest();
            }
            listing.End();
        }

        // Handles the logic when the player clicks the deposit/withdraw button.
        private void HandleDepositWithdrawRequest()
        {
            if (SelBuilding.BoundStorageUnit != null)
            {
                if (SelBuilding.BoundStorageUnit.CanReceiveIO)
                {
                    if (int.TryParse(amountTextArea, out int result))
                    {
                        if (deposit)
                        {
                            List<ThingCount> selected = new List<ThingCount>();
                            int current = 0;
                            foreach (Thing item in SelBuilding.BoundStorageUnit.StoredItems.ToList())
                            {
                                if (item.def == PRFDefOf.Paperclip)
                                {
                                    int num = Math.Min(result - current, item.stackCount);
                                    selected.Add(new ThingCount(item, num));
                                    current += num;
                                }
                                if (current == result)
                                {
                                    break;
                                }
                            }
                            if (current == result)
                            {
                                SelBuilding.DepositPaperclips(result);
                                for (int i = 0; i < selected.Count; i++)
                                {
                                    selected[i].Thing.SplitOff(selected[i].Count);
                                }
                                Messages.Message("SuccessfullyDepositedPaperclips".Translate(result), MessageTypeDefOf.PositiveEvent);
                            }
                            else
                            {
                                Messages.Message("PRFNotEnoughPaperclips".Translate(), MessageTypeDefOf.RejectInput);
                            }
                        }
                        else
                        {
                            if (result < SelBuilding.PaperclipsActual)
                            {
                                List<Thing> output = new List<Thing>();
                                int current = 0;
                                while (current < result)
                                {
                                    int num = Math.Min(result - current, PRFDefOf.Paperclip.stackLimit);
                                    Thing paperclip = ThingMaker.MakeThing(PRFDefOf.Paperclip);
                                    paperclip.stackCount = num;
                                    output.Add(paperclip);
                                    current += num;
                                }
                                for (int i = 0; i < output.Count; i++)
                                {
                                    GenPlace.TryPlaceThing(output[i], SelBuilding.Position, SelBuilding.Map, ThingPlaceMode.Direct);
                                    SelBuilding.BoundStorageUnit.Notify_ReceivedThing(output[i]);
                                }
                                SelBuilding.WithdrawPaperclips(result);
                                Messages.Message("SuccessfullyWithdrawnPaperclips".Translate(result), MessageTypeDefOf.PositiveEvent);
                            }
                            else
                            {
                                Messages.Message("PRFNotEnoughPaperclips".Translate(), MessageTypeDefOf.RejectInput);
                            }
                        }
                    }
                    else
                    {
                        Messages.Message("PRFInputInvalid".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("PRFBoundStorageUnitNotAvailableForIO".Translate(), MessageTypeDefOf.RejectInput);
                }
            }
            else
            {
                Messages.Message("PRFNoBoundStorageUnit".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        bool deposit;
        string amountTextArea;
    }
}
