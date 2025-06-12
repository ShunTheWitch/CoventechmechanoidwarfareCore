using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.MapGeneratorDef), MethodType.Getter)]
    public static class Settlement_MapGeneratorDef_Patch
    {
        public static void Postfix(Settlement __instance, ref MapGeneratorDef __result)
        {
            if (__instance.Biome.IsWaterBiome())
            {
                __result = CVN_DefOf.CVN_OceanMapGen;
            }
        }
    }
}
