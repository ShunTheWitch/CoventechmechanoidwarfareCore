using HarmonyLib;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(Patch_Construction), "RegisterThingSpawned")]
    public static class Construction_RegisterThingSpawned_Patch
    {
        public static bool Prefix(Thing thing, ref IntVec3 loc, Map map, ref Rot4 rot, ref Thing thing2, bool respawningAfterLoad)
        {
            if (PawnsArrivalModeWorker_FlightRaid.spawningFlightRaid)
            {
                return false;
            }
            return true;
        }
    }
}
