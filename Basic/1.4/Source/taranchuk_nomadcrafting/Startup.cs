using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace taranchuk_nomadcrafting
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                var compProps = thingDef.GetCompProperties<CompProperties_NomadCrafting>();
                if (compProps != null)
                {
                    thingDef.inspectorTabs ??= new List<Type>();
                    thingDef.inspectorTabs.Add(typeof(ITab_BillsPawn));
                    thingDef.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_BillsPawn)));
                    if (compProps.pullRecipesFrom != null)
                    {
                        foreach (var table in compProps.pullRecipesFrom)
                        {
                            AddRecipesFrom(compProps, table);
                        }
                    }
                }
            }

            foreach (var recipe in DefDatabase<RecipeDef>.AllDefs)
            {
                if (recipe.mechResurrection || recipe.defName.StartsWith("CVN"))
                {
                    Log.Message("Startup: " + recipe + " - recipe.fixedIngredientFilter: " + string.Join(", ", recipe.fixedIngredientFilter.AllowedThingDefs));
                }
            }
        }

        private static void AddRecipesFrom(CompProperties_NomadCrafting compProps, ThingDef table)
        {
            var allRecipes = new List<RecipeDef>();
            if (table.recipes != null)
            {
                for (int i = 0; i < table.recipes.Count; i++)
                {
                    allRecipes.Add(table.recipes[i]);
                }
            }
            List<RecipeDef> allDefsListForReading = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int j = 0; j < allDefsListForReading.Count; j++)
            {
                if (allDefsListForReading[j].recipeUsers != null && allDefsListForReading[j]
                    .recipeUsers.Contains(table))
                {
                    allRecipes.Add(allDefsListForReading[j]);
                }
            }
            allRecipes = allRecipes.Where(x => compProps.recipes.Contains(x) is false).ToList();
            compProps.recipes.AddRange(allRecipes);
        }
    }
}
