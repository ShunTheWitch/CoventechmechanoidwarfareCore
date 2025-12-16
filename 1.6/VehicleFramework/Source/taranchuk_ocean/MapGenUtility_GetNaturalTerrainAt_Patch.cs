using HarmonyLib;
using RimWorld;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(MapGenUtility), "GetNaturalTerrainAt")]
    public static class MapGenUtility_GetNaturalTerrainAt_Patch
    {
        public static bool Prefix(ref TerrainDef __result, IntVec3 cell, Map map)
        {
            if (map.Biome.IsWaterBiome())
            {
                __result = TerrainDefOf.WaterOceanShallow;
                return false;
            }
            return true;
        }
    }
}
