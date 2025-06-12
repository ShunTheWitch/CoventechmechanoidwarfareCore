using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(MapGenerator), "GenerateMap")]
    public static class MapGenerator_GenerateMap_Patch
    {
        public static void Prefix(ref IntVec3 mapSize, MapParent parent)
        {
            if (parent.Biome.IsWaterBiome())
            {
                mapSize = new IntVec3(600, 1, 600);
            }
        }
    }
}
