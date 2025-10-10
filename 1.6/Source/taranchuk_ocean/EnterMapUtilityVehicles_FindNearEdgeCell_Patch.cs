using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Vehicles;
using Vehicles.World;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(EnterMapUtilityVehicles), "FindNearEdgeCell")]
    public static class EnterMapUtilityVehicles_FindNearEdgeCell_Patch
    {
        public static bool Prefix(ref IntVec3 __result, Map map, VehicleDef vehicleDef, Predicate<IntVec3> extraCellValidator)
        {
            if (map.IsWaterBiome())
            {
                __result = FindNearEdgeCell(map, vehicleDef, extraCellValidator);
                return false;
            }
            return true;
        }

        private static IntVec3 FindNearEdgeCell(Map map, VehicleDef vehicleDef, Predicate<IntVec3> extraCellValidator)
        {
            Faction hostFaction = map.ParentFaction;
            IntVec3 result;
            try
            {
                if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x)
                    && (extraCellValidator == null || extraCellValidator(x)), map, Rot4.Random,
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
                if (CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => baseValidator(x)
                    && (extraCellValidator == null || extraCellValidator(x)), map, CellFinder.EdgeRoadChance_Always, out result))
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
            bool baseValidator(IntVec3 x)
            {
                return true;
            }
        }
    }
}
