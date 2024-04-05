using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace taranchuk_nomadcrafting
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public static class GenerateImpliedDefs_PreResolve_Patch
    {
        public static HashSet<RecipeDef> resurrectionRecipes = new HashSet<RecipeDef>();
        public static HashSet<RecipeDef> gestationRecipes = new HashSet<RecipeDef>();
        public static void Postfix()
        {
            foreach (var recipe in DefDatabase<RecipeDef>.AllDefs.ToList())
            {
                if (recipe.mechResurrection)
                {
                    var newRecipe = CloneRecipe(recipe);
                    newRecipe.mechResurrection = false;
                    resurrectionRecipes.Add(newRecipe);
                }
                else if (recipe.gestationCycles > 0)
                {
                    var newRecipe = CloneRecipe(recipe);
                    gestationRecipes.Add(newRecipe);
                }
            }
        }

        private static RecipeDef CloneRecipe(RecipeDef recipe)
        {
            var newRecipe = recipe.Clone();
            newRecipe.fixedIngredientFilter = newRecipe.defaultIngredientFilter = null;
            newRecipe.ingredients = null;
            newRecipe.shortHash = 0;
            newRecipe.defName = "CVN_" + recipe.defName;
            //newRecipe.workAmount += recipe.formingTicks;
            newRecipe.workSpeedStat = newRecipe.efficiencyStat = null;
            if (recipe.defaultIngredientFilter != null)
            {
                newRecipe.defaultIngredientFilter = recipe.defaultIngredientFilter.CopyThingFilter(newRecipe + " - defaultIngredientFilter");
            }
            if (recipe.fixedIngredientFilter != null)
            {
                newRecipe.fixedIngredientFilter = recipe.fixedIngredientFilter.CopyThingFilter(newRecipe + " - fixedIngredientFilter");
            }
            if (recipe.ingredients != null)
            {
                newRecipe.ingredients = new List<IngredientCount>();
                foreach (var ingredient in recipe.ingredients)
                {
                    var newIngredient = new IngredientCount();
                    newIngredient.count = ingredient.count;
                    newIngredient.filter = ingredient.filter.CopyThingFilter(newRecipe + " - ingredient: " + ingredient.Summary);
                    newRecipe.ingredients.Add(newIngredient);
                }
            }
            newRecipe.gestationCycles = 0;
            newRecipe.mechanitorOnlyRecipe = false;
            newRecipe.PostLoad();
            DefDatabase<RecipeDef>.Add(newRecipe);
            return newRecipe;
        }

        private static ThingFilter CopyThingFilter(this ThingFilter thingFilter, string debugPrefix)
        {
            var newThingFilter = new ThingFilter();
            newThingFilter.thingDefs = thingFilter.thingDefs.ListFullCopyOrNull();
            newThingFilter.categories = thingFilter.categories.ListFullCopyOrNull();
            newThingFilter.specialFiltersToAllow = thingFilter.specialFiltersToAllow.ListFullCopyOrNull();
            newThingFilter.specialFiltersToDisallow = thingFilter.specialFiltersToDisallow.ListFullCopyOrNull();
            newThingFilter.disallowedCategories = thingFilter.disallowedCategories.ListFullCopyOrNull();
            newThingFilter.disallowedSpecialFilters = thingFilter.disallowedSpecialFilters.ListFullCopyOrNull();
            newThingFilter.disallowedThingDefs = thingFilter.disallowedThingDefs.ListFullCopyOrNull();
            return newThingFilter;
        }

        public static T Clone<T>(this T obj)
        {
            var inst = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)inst?.Invoke(obj, null);
        }
    }
}
