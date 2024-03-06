using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace taranchuk_nomadcrafting
{
    public class BillProcess : IExposable
    {
        public Bill bill;
        public float workLeft;
        public List<Thing> consumedIngredients = new List<Thing>();

        private Thing CalculateDominantIngredient(List<Thing> ingredients)
        {
            if (!ingredients.NullOrEmpty())
            {
                if (bill.recipe.productHasIngredientStuff)
                {
                    return ingredients[0];
                }
                if (bill.recipe.products.Any((ThingDefCountClass x) => x.thingDef.MadeFromStuff))
                {
                    return ingredients.Where((Thing x) => x.def.IsStuff).RandomElementByWeight((Thing x) => x.stackCount);
                }
                return ingredients.RandomElementByWeight((Thing x) => x.stackCount);
            }
            return null;
        }

        public void FinishBill(Pawn doer, CompNomadCrafting comp)
        {
            Thing dominantIngredient = CalculateDominantIngredient(consumedIngredients);
            ThingStyleDef style = null;
            if (ModsConfig.IdeologyActive && this.bill.recipe.products != null && this.bill.recipe.products.Count == 1)
            {
                style = ((!this.bill.globalStyle) ? this.bill.style : Faction.OfPlayer.ideos.PrimaryIdeo.style.StyleForThingDef(this.bill.recipe.ProducedThingDef)?.styleDef);
            }
            List<Thing> list = ((bill is Bill_Mech billMech) ?
                GenRecipe.FinalizeGestatedPawns(billMech, doer, style).ToList()
                : GenRecipe.MakeRecipeProducts(bill.recipe, doer, consumedIngredients, dominantIngredient,
                doer, bill.precept, style, bill.graphicIndexOverride).ToList());
            Log.Message("Finising bill: " + bill + " - things produced: " + string.Join(", ", list) + " - consumedIngredients: " + string.Join(", ", consumedIngredients));
            foreach (var thing in list.ToList())
            {
                if (comp.Props.qualityRange != null)
                {
                    var quality = comp.Props.qualityRange.RandomElement();
                    var compQuality = thing.TryGetComp<CompQuality>();
                    if (compQuality != null)
                    {
                        compQuality.SetQuality(quality, ArtGenerationContext.Colony);
                    }
                }

                if (thing is Pawn newPawn)
                {
                    GenSpawn.Spawn(newPawn, doer.Position, doer.Map);
                    newPawn.SetFaction(doer.Faction, doer);
                }
                else
                {
                    doer.inventory.innerContainer.TryAdd(thing);
                }
            }

            this.bill.Notify_BillWorkFinished(doer);
            this.bill.Notify_IterationCompleted(doer, consumedIngredients);
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref bill, "activeBill");
            Scribe_Values.Look(ref workLeft, "workLeft");
            Scribe_Collections.Look(ref consumedIngredients, "consumedIngredients", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                consumedIngredients ??= new List<Thing>();
            }
        }

    }
}
