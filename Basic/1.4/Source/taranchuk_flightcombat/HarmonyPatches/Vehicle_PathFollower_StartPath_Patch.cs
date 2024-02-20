using HarmonyLib;
using Vehicles;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(Vehicle_PathFollower), nameof(Vehicle_PathFollower.StartPath))]
    public static class Vehicle_PathFollower_StartPath_Patch
    {
        public static bool Prefix(Vehicle_PathFollower __instance, LocalTargetInfo dest, PathEndMode peMode)
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
