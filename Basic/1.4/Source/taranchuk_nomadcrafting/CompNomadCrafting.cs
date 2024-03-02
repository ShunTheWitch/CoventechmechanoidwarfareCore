using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static RimWorld.WorkGiver_DoBill;

namespace taranchuk_nomadcrafting
{
    public class CompProperties_NomadCrafting : CompProperties
    {
        public float craftingSpeed;
        public List<RecipeDef> recipes;
        public List<ThingDef> pullRecipesFrom;
        public QualityCategory defaultQuality = QualityCategory.Normal;

        public CompProperties_NomadCrafting()
        {
            this.compClass = typeof(CompNomadCrafting);
        }
    }

    public class CompNomadCrafting : ThingComp
    {
        public CompProperties_NomadCrafting Props => base.props as CompProperties_NomadCrafting;
        public Bill activeBill;
        public BillStack billStack;
        public Pawn Pawn => parent as Pawn;
        private float workLeft;
        private List<Thing> consumedIngredients;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            billStack ??= new BillStack(Pawn);
        }

        public override string CompInspectStringExtra()
        {
            if (activeBill != null)
            {
                return "CVN.CraftingItem".Translate(activeBill.recipe.ProducedThingDef.label, (workLeft / activeBill.GetWorkAmount()).ToStringPercent());
            }
            return null;
        }

        private Thing CalculateDominantIngredient(List<Thing> ingredients)
        {
            if (!ingredients.NullOrEmpty())
            {
                if (activeBill.recipe.productHasIngredientStuff)
                {
                    return ingredients[0];
                }
                if (activeBill.recipe.products.Any((ThingDefCountClass x) => x.thingDef.MadeFromStuff))
                {
                    return ingredients.Where((Thing x) => x.def.IsStuff).RandomElementByWeight((Thing x) => x.stackCount);
                }
                return ingredients.RandomElementByWeight((Thing x) => x.stackCount);
            }
            return null;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (activeBill != null)
            {
                workLeft -= Props.craftingSpeed;
                if (workLeft <= 0)
                {
                    Thing dominantIngredient = CalculateDominantIngredient(consumedIngredients);
                    ThingStyleDef style = null;
                    if (ModsConfig.IdeologyActive && activeBill.recipe.products != null && activeBill.recipe.products.Count == 1)
                    {
                        style = ((!activeBill.globalStyle) ? activeBill.style : Faction.OfPlayer.ideos.PrimaryIdeo.style.StyleForThingDef(activeBill.recipe.ProducedThingDef)?.styleDef);
                    }
                    List<Thing> list = ((activeBill is Bill_Mech bill) ? 
                        GenRecipe.FinalizeGestatedPawns(bill, Pawn, style).ToList() 
                        : GenRecipe.MakeRecipeProducts(activeBill.recipe, Pawn, consumedIngredients, dominantIngredient, 
                        Pawn, activeBill.precept, style, activeBill.graphicIndexOverride).ToList());
                    foreach (var thing in list.ToList())
                    {
                        if (thing is Pawn newPawn)
                        {
                            GenSpawn.Spawn(newPawn, Pawn.Position, Pawn.Map);
                        }
                        else
                        {
                            Pawn.inventory.innerContainer.TryAdd(thing);
                        }
                    }

                    activeBill.Notify_BillWorkFinished(Pawn);
                    activeBill.Notify_IterationCompleted(Pawn, consumedIngredients);
                    activeBill = null;
                }
            }
            else
            {
                if (billStack.AnyShouldDoNow)
                {
                    for (int i = 0; i < billStack.Count; i++)
                    {
                        Bill bill = billStack[i];
                        if (!bill.ShouldDoNow())
                        {
                            continue;
                        }
                        var availableThings = Pawn.inventory.innerContainer.Where(x => IsUsableIngredient(x, bill)).ToList();
                        if (!TryFindBestIngredientsInSet(availableThings, bill.recipe.ingredients, chosenIngThings, missingIngredients, bill))
                        {
                            chosenIngThings.Clear();
                            continue;
                        }
                        consumedIngredients.Clear();
                        foreach (var item in chosenIngThings)
                        {
                            var consumed = item.Thing.SplitOff(item.count);
                            consumedIngredients.Add(consumed);
                        }
                        chosenIngThings.Clear();
                        activeBill = bill;
                        workLeft = bill.GetWorkAmount(null);
                    }
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
            Scribe_References.Look(ref activeBill, "activeBill");
            billStack ??= new BillStack(Pawn);
            Scribe_Values.Look(ref workLeft, "workLeft");
            Scribe_Collections.Look(ref consumedIngredients, "consumedIngredients", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                consumedIngredients ??= new List<Thing>();
            }
        }
    }
}
