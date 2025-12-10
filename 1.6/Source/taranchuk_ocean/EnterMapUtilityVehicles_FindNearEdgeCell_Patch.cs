using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Vehicles;
using Vehicles.World;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(EnterMapUtilityVehicles), "FindNearEdgeCell", new System.Type[] { typeof(Map), typeof(VehicleDef), typeof(Faction), typeof(EnterMapUtilityVehicles.SpawnParams) })]
    public static class EnterMapUtilityVehicles_FindNearEdgeCell_Patch
    {
        public static bool Prefix(ref IntVec3 __result, Map map, VehicleDef vehicleDef, Faction faction, EnterMapUtilityVehicles.SpawnParams spawnParams)
        {
            if (map.IsWaterBiome())
            {
                __result = FindNearEdgeCell(map, vehicleDef, spawnParams);
                return false;
            }
            return true;
        }

        private static IntVec3 FindNearEdgeCell(Map map, VehicleDef vehicleDef, EnterMapUtilityVehicles.SpawnParams spawnParams)
        {
            IntVec3 result;
            try
            {
                if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => (spawnParams.extraCellValidator == null || spawnParams.extraCellValidator(x, map, vehicleDef)), map, Rot4.Random,
                    CellFinder.EdgeRoadChance_Ignore, out result))
                {
                    return CellFinderExtended.RandomClosewalkCellNear(result, map, vehicleDef, 5);
                }
            }
            catch (Exception e)
            {
                Log.Error("error: " + e.ToString());
            }
            try
            {
                if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => (spawnParams.extraCellValidator == null || spawnParams.extraCellValidator(x, map, vehicleDef)), map, CellFinder.EdgeRoadChance_Always, out result))
                {
                    return CellFinderExtended.RandomClosewalkCellNear(result, map, vehicleDef, 5);
                }
            }
            catch (Exception e)
            {
                Log.Error("error: " + e.ToString());
            }
            Log.Warning("Could not find any valid edge cell.");
            return CellFinder.RandomCell(map);
        }
    }
}
