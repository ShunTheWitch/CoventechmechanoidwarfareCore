using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(MapParent), nameof(MapParent.MapGeneratorDef), MethodType.Getter)]
    public static class MapParent_MapGeneratorDef_Patch
    {
        public static void Postfix(MapParent __instance, ref MapGeneratorDef __result)
        {
            if (__instance.Biome.IsWaterBiome())
            {
                __result = CVN_DefOf.CVN_OceanMapGen;
            }
        }
    }
}
