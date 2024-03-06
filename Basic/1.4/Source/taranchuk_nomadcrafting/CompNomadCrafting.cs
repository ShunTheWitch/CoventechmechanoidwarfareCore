using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static RimWorld.WorkGiver_DoBill;

namespace taranchuk_nomadcrafting
{
    public class CompProperties_NomadCrafting : CompProperties
    {
        public int countOfBillsProcessed = 1;
        public float craftingSpeed;
        public List<RecipeDef> recipes;
        public List<ThingDef> pullRecipesFrom;
        public List<QualityCategory> qualityRange;

        public CompProperties_NomadCrafting()
        {
            this.compClass = typeof(CompNomadCrafting);
        }
    }

    public class CompNomadCrafting : ThingComp
    {
        public CompProperties_NomadCrafting Props => base.props as CompProperties_NomadCrafting;
        public BillStack billStack;
        public List<BillProcess> billProcesses = new List<BillProcess>();
        public Pawn Pawn => parent as Pawn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            billStack ??= new BillStack(Pawn);
        }

        public override string CompInspectStringExtra()
        {
            if (billProcesses.Any())
            {
                var sb = new StringBuilder();
                foreach (var process in billProcesses)
                {
                    sb.AppendLine("CVN.CraftingItem".Translate(process.bill.recipe.ProducedThingDef.label, (process.workLeft / process.bill.GetWorkAmount()).ToStringPercent()));
                }
                return sb.ToString().TrimEndNewlines();
            }
            return null;
        }

        public override void CompTick()
        {
            base.CompTick();
            TryAddBillProcesses();
            foreach (var process in billProcesses.ToList())
            {
                process.workLeft -= Props.craftingSpeed;
                if (process.workLeft <= 0)
                {
                    process.FinishBill(Pawn, this);
                    billProcesses.Remove(process);
                }
            }
        }

        private void TryAddBillProcesses()
        {
            while (billStack.AnyShouldDoNow && billProcesses.Count < Props.countOfBillsProcessed)
            {
                bool added = false;
                for (int i = 0; i < billStack.Count; i++)
                {
                    Bill bill = billStack[i];
                    if (!bill.ShouldDoNow())
                    {
                        Log.Message(bill + " - shouldn't do");
                        continue;
                    }
                    if (bill is Bill_Production billProduction)
                    {
                        if (billProduction.repeatMode == BillRepeatModeDefOf.RepeatCount
                            && billProcesses.Where(x => x.bill == bill).Count() >= billProduction.repeatCount)
                        {
                            Log.Message(bill + " - shouldn't do 2");
                            continue;
                        }
                    }

                    var availableThings = Pawn.inventory.innerContainer.Where(x => IsUsableIngredient(x, bill)).ToList();
                    if (!TryFindBestIngredientsInSet(availableThings, bill.recipe.ingredients, chosenIngThings, missingIngredients, bill))
                    {
                        Log.Message("No ingredients found: " + string.Join(", ", availableThings));
                        chosenIngThings.Clear();
                        continue;
                    }

                    var billProcess = new BillProcess();
                    foreach (var item in chosenIngThings)
                    {
                        var consumed = item.Thing.SplitOff(item.count);
                        billProcess.consumedIngredients.Add(consumed);
                    }
                    chosenIngThings.Clear();
                    billProcess.bill = bill;
                    billProcess.workLeft = bill.GetWorkAmount(null);
                    billProcesses.Add(billProcess);
                    added = true;
                    break;
                }

                if (added is false)
                {
                    break;
                }
            }
        }

        public static bool IsFixedOrAllowedIngredient(Bill bill, Thing thing)
        {
            for (int i = 0; i < bill.recipe.ingredients.Count; i++)
            {
                IngredientCount ingredientCount = bill.recipe.ingredients[i];
                if (ingredientCount.IsFixedIngredient && ingredientCount.filter.Allows(thing))
                {
                    return true;
                }
                else
                {
                    Log.Message("1: " + thing + " doesn't allow: " + ingredientCount + " - ingredientCount.IsFixedIngredient: " + ingredientCount.IsFixedIngredient);
                }
            }
            if (bill.recipe.fixedIngredientFilter.Allows(thing))
            {
                if (bill.ingredientFilter.Allows(thing))
                {
                    return true;
                }
                else
                {
                    Log.Message("3: " + thing + " doesn't allow: " + bill.ingredientFilter);
                }
            }
            else
            {
                Log.Message("2: " + thing + " doesn't allow: " + bill.recipe.fixedIngredientFilter + " - bill.recipe.fixedIngredientFilter: " + string.Join(", ", bill.recipe.fixedIngredientFilter.AllowedThingDefs));
            }
            return false;
        }

        private static bool IsUsableIngredient(Thing t, Bill bill)
        {
            if (!IsFixedOrAllowedIngredient(bill, t))
            {
                Log.Message(t + " is not usable 1");
                return false;
            }
            foreach (IngredientCount ingredient in bill.recipe.ingredients)
            {
                if (ingredient.filter.Allows(t))
                {
                    return true;
                }
                else
                {
                    Log.Message(t + " is not usable 1.5: " + ingredient.filter.Summary);
                }
            }
            Log.Message(t + " is not usable 2");
            return false;
        }

        private List<ThingCount> chosenIngThings = new List<ThingCount>();

        private static List<IngredientCount> missingIngredients = new List<IngredientCount>();

        private static DefCountList availableCounts = new DefCountList();

        private static bool TryFindBestIngredientsInSet(List<Thing> availableThings,
            List<IngredientCount> ingredients, List<ThingCount> chosen,
            List<IngredientCount> missingIngredients, Bill bill = null)
        {
            chosen.Clear();
            availableCounts.Clear();
            missingIngredients?.Clear();
            availableCounts.GenerateFrom(availableThings);
            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientCount ingredientCount = ingredients[i];
                bool flag = false;
                for (int j = 0; j < availableCounts.Count; j++)
                {
                    float num = ((bill != null) ? ((float)ingredientCount.CountRequiredOfFor(availableCounts.GetDef(j), bill.recipe, bill)) : ingredientCount.GetBaseCount());
                    if ((bill != null && !bill.recipe.ignoreIngredientCountTakeEntireStacks && num > availableCounts.GetCount(j)) || !ingredientCount.filter.Allows(availableCounts.GetDef(j)) || (bill != null && !ingredientCount.IsFixedIngredient && !bill.ingredientFilter.Allows(availableCounts.GetDef(j))))
                    {
                        continue;
                    }
                    for (int k = 0; k < availableThings.Count; k++)
                    {
                        if (availableThings[k].def != availableCounts.GetDef(j))
                        {
                            continue;
                        }
                        int num2 = availableThings[k].stackCount - ThingCountUtility.CountOf(chosen, availableThings[k]);
                        if (num2 > 0)
                        {
                            if (bill != null && bill.recipe.ignoreIngredientCountTakeEntireStacks)
                            {
                                ThingCountUtility.AddToList(chosen, availableThings[k], num2);
                                return true;
                            }
                            int num3 = Mathf.Min(Mathf.FloorToInt(num), num2);
                            ThingCountUtility.AddToList(chosen, availableThings[k], num3);
                            num -= (float)num3;
                            if (num < 0.001f)
                            {
                                flag = true;
                                float count = availableCounts.GetCount(j);
                                count -= num;
                                availableCounts.SetCount(j, count);
                                break;
                            }
                        }
                    }
                    if (flag)
                    {
                        break;
                    }
                }
                if (!flag)
                {
                    if (missingIngredients == null)
                    {
                        return false;
                    }
                    missingIngredients.Add(ingredientCount);
                }
            }
            if (missingIngredients != null)
            {
                return missingIngredients.Count == 0;
            }
            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref billStack, "billStack", Pawn);
            Scribe_Collections.Look(ref billProcesses, "billProcesses", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                billStack ??= new BillStack(Pawn);
                billProcesses ??= new List<BillProcess>();
                billProcesses.RemoveAll(x => x.bill?.recipe is null);
            }
        }
    }
}
