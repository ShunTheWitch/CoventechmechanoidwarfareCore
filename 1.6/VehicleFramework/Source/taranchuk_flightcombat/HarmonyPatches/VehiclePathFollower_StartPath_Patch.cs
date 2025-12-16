using HarmonyLib;
using Vehicles;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehiclePathFollower), nameof(VehiclePathFollower.StartPath))]
    public static class VehiclePathFollower_StartPath_Patch
    {
        public static bool Prefix(VehiclePathFollower __instance, LocalTargetInfo dest, PathEndMode peMode)
        {
            var comp = __instance.vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                comp.SetTarget(dest);
                return false;
            }
            return true;
        }
    }
}
