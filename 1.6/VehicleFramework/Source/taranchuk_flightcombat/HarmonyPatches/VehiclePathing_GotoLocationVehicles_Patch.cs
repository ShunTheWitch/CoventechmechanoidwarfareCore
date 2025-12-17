using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehiclePathFollower), nameof(VehiclePathFollower.StartPath), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(bool))]
    public static class VehiclePathing_GotoLocationVehicles_Patch
    {
        public static bool Prefix(VehiclePathFollower __instance, LocalTargetInfo __0, PathEndMode __1, bool __2)
        {
            var vehicle = __instance.vehicle as VehiclePawn;
            if (vehicle != null && vehicle.Faction == Faction.OfPlayer)
            {
                var comp = vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    comp.SetTarget(__0);
                    return false;
                }
            }
            return true;
        }
    }
}
