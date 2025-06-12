using RimWorld;
using System;
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
        public List<MechWeightClassDef> mechClassesToGestate;
        public List<QualityCategory> qualityRange;

        public CompProperties_NomadCrafting()
        {
            this.compClass = typeof(CompNomadCrafting);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (mechClassesToGestate != null)
            {
                recipes ??= new List<RecipeDef>();
                foreach (var mechClass in mechClassesToGestate)
                {
                    foreach (var recipe in GenerateImpliedDefs_PreResolve_Patch.gestationRecipes)
                    {
                        if (recipe.ProducedThingDef.race.mechWeightClass == mechClass)
                        {
                            recipes.Add(recipe);
                        }
                    }
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [HotSwappableAttribute]
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
                    var progress = 1f - (process.workLeft / process.bill.GetWorkAmount());
                    sb.AppendLine("CVN.Bill".Translate(process.bill.recipe.label, progress.ToStringPercent()));
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
                        continue;
                    }
                    if (bill is Bill_Production billProduction)
                    {
                        if (billProduction.repeatMode == BillRepeatModeDefOf.RepeatCount
                            && billProcesses.Where(x => x.bill == bill).Count() >= billProduction.repeatCount)
                        {
                            continue;
                        }
                    }

                    var availableThings = Pawn.inventory.innerContainer.Where(x => IsUsableIngredient(x, bill)).ToList();
                    if (!TryFindBestIngredientsInSet(availableThings, bill.recipe.ingredients, chosenIngThings, missingIngredients, bill))
                    {
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
