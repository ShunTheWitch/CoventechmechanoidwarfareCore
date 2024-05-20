using RimWorld;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    public static class Utils
    {
        public static float GetMass(this Pawn pawn)
        {
            if (pawn is VehiclePawn vehicle)
            {
                return vehicle.GetStatValue(VehicleStatDefOf.Mass);
            }
            return pawn.GetStatValue(StatDefOf.Mass);
        }

        public static bool InBoundsLocal(this CellRect occupiedRect, Map map)
        {
            for (int i = occupiedRect.minZ; i <= occupiedRect.maxZ; i++)
            {
                for (int j = occupiedRect.minX; j <= occupiedRect.maxX; j++)
                {
                    if (new IntVec3(j, 0, i).InBounds(map) is false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
