using HarmonyLib;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(Patch_Construction), "RegisterThingSpawned")]
    public static class Construction_RegisterThingSpawned_Patch
    {
        public static bool Prefix()
        {
            if (PawnsArrivalModeWorker_FlightRaid.spawningFlightRaid)
            {
                return false;
            }
            return true;
        }
    }
}
