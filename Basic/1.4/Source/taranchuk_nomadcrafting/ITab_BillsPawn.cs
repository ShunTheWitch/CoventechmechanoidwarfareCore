using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace taranchuk_nomadcrafting
{
    public class ITab_BillsPawn : ITab
    {
        private float viewHeight = 1000f;

        private Vector2 scrollPosition;

        private Bill mouseoverBill;

        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        [TweakValue("Interface", 0f, 128f)]
        private static float PasteX = 48f;

        [TweakValue("Interface", 0f, 128f)]
        private static float PasteY = 3f;

        [TweakValue("Interface", 0f, 32f)]
        private static float PasteSize = 24f;

        public ITab_BillsPawn()
        {
            size = WinSize;
            labelKey = "TabBills";
            tutorTag = "Bills";
        }

        public override void FillTab()
        {
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            Rect rect2 = new Rect(WinSize.x - PasteX, PasteY, PasteSize, PasteSize);
            var comp = SelPawn.GetComp<CompNomadCrafting>();
            var allRecipes = comp.Props.recipes;
            if (BillUtility.Clipboard != null)
            {
                if (!allRecipes.Contains(BillUtility.Clipboard.recipe) || !BillUtility.Clipboard.recipe.AvailableNow 
                    || !BillUtility.Clipboard.recipe.AvailableOnNow(SelPawn))
                {
                    GUI.color = Color.gray;
                    Widgets.DrawTextureFitted(rect2, TexButton.Paste, 1f);
                    GUI.color = Color.white;
                    if (Mouse.IsOver(rect2))
                    {
                        TooltipHandler.TipRegion(rect2, "ClipboardBillNotAvailableHere".Translate() + ": " + BillUtility.Clipboard.LabelCap);
                    }
                }
                else
                {
                    if (Widgets.ButtonImageFitted(rect2, TexButton.Paste, Color.white))
                    {
                        Bill bill = BillUtility.Clipboard.Clone();
                        bill.InitializeAfterClone();
                        comp.billStack.AddBill(bill);
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    }
                    if (Mouse.IsOver(rect2))
                    {
                        TooltipHandler.TipRegion(rect2, "PasteBillTip".Translate() + ": " + BillUtility.Clipboard.LabelCap);
                    }
                }
            }
            Rect rect3 = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Func<List<FloatMenuOption>> recipeOptionsMaker = delegate
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                for (int i = 0; i < allRecipes.Count; i++)
                {
                    RecipeDef recipe;
                    if (allRecipes[i].AvailableNow && allRecipes[i].AvailableOnNow(SelPawn))
                    {
                        recipe = allRecipes[i];
                        Add(null);
                        foreach (Ideo allIdeo in Faction.OfPlayer.ideos.AllIdeos)
                        {
                            foreach (Precept_Building cachedPossibleBuilding in allIdeo.cachedPossibleBuildings)
                            {
                                if (cachedPossibleBuilding.ThingDef == recipe.ProducedThingDef)
                                {
                                    Add(cachedPossibleBuilding);
                                }
                            }
                        }
                    }
                    void Add(Precept_ThingStyle precept)
                    {
                        string label = ((precept != null) ? "RecipeMake".Translate(precept.LabelCap).CapitalizeFirst() : recipe.LabelCap);
                        opts.Add(new FloatMenuOption(label, delegate
                        {
                            Bill bill2 = recipe.MakeNewBill(precept);
                            Log.Message("Making bill: " + bill2.GetType() + " - " + recipe + " - " + recipe.gestationCycles + " - mechResurrection: " + recipe.mechResurrection);
                            comp.billStack.AddBill(bill2);
                            if (recipe.conceptLearned != null)
                            {
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
                            }
                            if (TutorSystem.TutorialMode)
                            {
                                TutorSystem.Notify_Event("AddBill-" + recipe.LabelCap.Resolve());
                            }
                        }, recipe.UIIconThing, null, forceBasicStyle: false, MenuOptionPriority.Default, delegate (Rect rect)
                        {
                            BillUtility.DoBillInfoWindow(i, label, rect, recipe);
                        }, null, 29f, (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, recipe, precept), null, playSelectionSound: true, -recipe.displayPriority));
                    }
                }
                if (!opts.Any())
                {
                    opts.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
                }
                return opts;
            };
            mouseoverBill = comp.billStack.DoListing(rect3, recipeOptionsMaker, ref scrollPosition, ref viewHeight);
        }

        public override void TabUpdate()
        {
            if (mouseoverBill != null)
            {
                mouseoverBill.TryDrawIngredientSearchRadiusOnMap(SelPawn.Position);
                mouseoverBill = null;
            }
        }
    }
}
