using HarmonyLib;
using RimWorld;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(GenStep_Terrain), "TerrainFrom")]
    public static class GenStep_Terrain_TerrainFrom_Patch
    {
        public static bool Prefix(ref TerrainDef __result, IntVec3 c, Map map)
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
