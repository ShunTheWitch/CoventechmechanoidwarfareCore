using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(GenStep_FindPlayerStartSpot), "Generate")]
    public static class GenStep_FindPlayerStartSpot_Generate_Patch
    {
        public static bool Prefix(Map map, GenStepParams parms)
        {
            if (map.Biome.IsWaterBiome())
            {
                DeepProfiler.Start("RebuildAllRegions");
                map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
                DeepProfiler.End();
                MapGenerator.PlayerStartSpot = TryFindCentralCell(map, 7, (IntVec3 x) => !x.Roofed(map));
                return false;
            }
            return true;
        }

        public static IntVec3 TryFindCentralCell(Map map, int tightness, Predicate<IntVec3> extraValidator = null)
        {
            int debug_numStand = 0;
            int debug_numDistrict = 0;
            int debug_numTouch = 0;
            int debug_numDistrictCellCount = 0;
            int debug_numExtraValidator = 0;
            Predicate<IntVec3> validator = delegate (IntVec3 c)
            {
                if (!c.Standable(map))
                {
                    debug_numStand++;
                    return false;
                }
                if (extraValidator != null && !extraValidator(c))
                {
                    debug_numExtraValidator++;
                    return false;
                }
                return true;
            };
            for (int num = tightness; num >= 1; num--)
            {
                int num2 = map.Size.x / num;
                if (CellFinderLoose.TryFindRandomNotEdgeCellWith((map.Size.x - num2) / 2, validator, map, out var result))
                {
                    return result;
                }
            }
            Log.Error("Found no good central spot. Choosing randomly. numStand=" + debug_numStand + ", numDistrict=" + debug_numDistrict + ", numTouch=" + debug_numTouch + ", numDistrictCellCount=" + debug_numDistrictCellCount + ", numExtraValidator=" + debug_numExtraValidator);
            return CellFinderLoose.RandomCellWith((IntVec3 x) => x.Standable(map), map);
        }
    }
}
