using HarmonyLib;
using System.Linq;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(CompVehicleTurrets), "CompTick")]
    public static class CompVehicleTurrets_CompTick_Patch
    {
        public static bool Prefix(CompVehicleTurrets __instance)
        {
            if (__instance.Vehicle.Map != null)
            {
                if (__instance.Vehicle.Position.InBounds(__instance.Vehicle.Map) is false
                    || __instance.Vehicle.OccupiedRect().Cells.Any(x => x.InBounds(__instance.Vehicle.Map) is false))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
