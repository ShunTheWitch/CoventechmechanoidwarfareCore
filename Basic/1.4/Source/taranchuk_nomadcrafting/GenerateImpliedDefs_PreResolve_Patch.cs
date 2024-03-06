using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace taranchuk_nomadcrafting
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public static class GenerateImpliedDefs_PreResolve_Patch
    {
        public static void Prefix()
        {
            foreach (var recipe in DefDatabase<RecipeDef>.AllDefs.ToList())
            {
                if (recipe.mechResurrection)
                {
                    var newRecipe = CloneRecipe(recipe);
                    newRecipe.mechResurrection = false;
                    Log.Message(newRecipe + " - " + newRecipe.mechResurrection);
                }
                else if (recipe.gestationCycles > 0)
                {
                    CloneRecipe(recipe);
                }
            }
        }

        private static RecipeDef CloneRecipe(RecipeDef recipe)
        {
            var newRecipe = recipe.Clone();
            newRecipe.shortHash = 0;
            newRecipe.defName = "CVN_" + recipe.defName;
            //newRecipe.workAmount += recipe.formingTicks;
            newRecipe.workSpeedStat = newRecipe.efficiencyStat = null;
            if (recipe.defaultIngredientFilter != null)
            {
                newRecipe.defaultIngredientFilter = recipe.defaultIngredientFilter.CopyThingFilter();
            }
            if (recipe.fixedIngredientFilter != null)
            {
                newRecipe.fixedIngredientFilter = recipe.fixedIngredientFilter.CopyThingFilter();
            }
            if (recipe.ingredients != null)
            {
                newRecipe.ingredients = new List<IngredientCount>();
                foreach (var ingredient in recipe.ingredients)
                {
                    var newIngredient = new IngredientCount();
                    newIngredient.count = ingredient.count;
                    newIngredient.filter = ingredient.filter.CopyThingFilter();
                    newRecipe.ingredients.Add(newIngredient);
                }
            }
            newRecipe.gestationCycles = 0;
            newRecipe.mechanitorOnlyRecipe = false;
            newRecipe.PostLoad();
            DefDatabase<RecipeDef>.Add(newRecipe);
            return newRecipe;
        }

        private static ThingFilter CopyThingFilter(this ThingFilter thingFilter)
        {
            var newThingFilter = new ThingFilter();
            if (thingFilter.thingDefs != null)
            {
                newThingFilter.thingDefs = thingFilter.thingDefs.ListFullCopy();
            }
            if (thingFilter.categories != null)
            {
                newThingFilter.categories = thingFilter.categories.ListFullCopy();
            }
            return newThingFilter;
        }

        public static T Clone<T>(this T obj)
        {
            var inst = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)inst?.Invoke(obj, null);
        }
    }
}
