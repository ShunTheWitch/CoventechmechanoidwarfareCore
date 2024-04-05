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
    }
}
