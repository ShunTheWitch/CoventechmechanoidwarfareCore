using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(TileFinder), "IsValidTileForNewSettlement")]
    public static class TileFinder_IsValidTileForNewSettlement_Patch
    {
        public static void Prefix(int tile, out bool __state)
        {
            Tile tile2 = Find.WorldGrid[tile];
            __state = tile2.biome.canBuildBase;
            if (SettleInEmptyTileUtility_SettleCommand_Patch.lookingForWaterTile)
            {
                if (tile2.biome.IsWaterBiome())
                {
                    tile2.biome.canBuildBase = true;
                }
            }
        }

        public static bool IsWaterBiome(this BiomeDef biome)
        {
            return biome == BiomeDefOf.Ocean || biome == BiomeDefOf.Lake;
        }

        public static bool IsWaterBiome(this Map map)
        {
            return map.Biome == BiomeDefOf.Ocean || map.Biome == BiomeDefOf.Lake;
        }

        public static void Postfix(int tile, bool __state)
        {
            Tile tile2 = Find.WorldGrid[tile]; 
            tile2.biome.canBuildBase = __state;
        }
    }
}
