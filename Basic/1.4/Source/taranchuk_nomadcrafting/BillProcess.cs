using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace taranchuk_nomadcrafting
{
    [HotSwappable]
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
            foreach (var thing in MakeProducts(doer, dominantIngredient, style).ToList())
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
                }
                else
                {
                    doer.inventory.innerContainer.TryAdd(thing);
                }
            }

            this.bill.Notify_BillWorkFinished(doer);
            this.bill.Notify_IterationCompleted(doer, consumedIngredients);
        }

        private IEnumerable<Thing> MakeProducts(Pawn doer, Thing dominantIngredient, ThingStyleDef style)
        {
            if (GenerateImpliedDefs_PreResolve_Patch.resurrectionRecipes.Contains(bill.recipe))
            {
                var corpse = consumedIngredients.OfType<Corpse>().FirstOrDefault();
                if (corpse != null)
                {
                    var mechanitor = MechanitorUtility.IsMechanitor(doer) ? doer : doer.GetOverseer();
                    yield return ResurrectMech(corpse, mechanitor);
                }
                else
                {
                    Log.Error("Failed to resurrect a mech, no corpse found!");
                }
            }
            else if (GenerateImpliedDefs_PreResolve_Patch.gestationRecipes.Contains(bill.recipe))
            {
                var mechanitor = MechanitorUtility.IsMechanitor(doer) ? doer : doer.GetOverseer();
                yield return ProducePawn(mechanitor, bill.recipe, mechanitor?.Faction ?? doer.Faction);
            }
            else
            {
                foreach (var product in GenRecipe.MakeRecipeProducts(bill.recipe, doer, consumedIngredients, dominantIngredient,
                            doer, bill.precept, style, bill.graphicIndexOverride))
                {
                    yield return product;
                }
            }
        }

        public Pawn ResurrectMech(Corpse corpse, Pawn mechanitor)
        {
            Pawn innerPawn = corpse.InnerPawn;
            ResurrectionUtility.Resurrect(innerPawn);
            innerPawn.needs.energy.CurLevel = innerPawn.needs.energy.MaxLevel * 0.5f;
            innerPawn.health.RemoveAllHediffs();
            if (mechanitor != null)
            {
                mechanitor.relations.AddDirectRelation(PawnRelationDefOf.Overseer, innerPawn);
            }
            if (innerPawn.IsWorldPawn())
            {
                Find.WorldPawns.RemovePawn(innerPawn);
            }
            return innerPawn;
        }

        public Pawn ProducePawn(Pawn mechanitor, RecipeDef recipe, Faction faction)
        {
            PawnKindDef kind = DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef pk) => pk.race == recipe.ProducedThingDef).First();
            Pawn newMech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction,
                PawnGenerationContext.NonPlayer, -1, forceGenerateNewPawn: false, allowDead: false,
                allowDowned: true, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, 1f,
                forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true,
                allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, 0f, 0f, null,
                1f, null, null, null, null, null, null, null, null, null, null, null, null, forceNoIdeo: false,
                forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, null, null, null, null, null, 0f,
                DevelopmentalStage.Newborn));
            if (mechanitor != null)
            {
                mechanitor.relations.AddDirectRelation(PawnRelationDefOf.Overseer, newMech);
            }
            return newMech;
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
