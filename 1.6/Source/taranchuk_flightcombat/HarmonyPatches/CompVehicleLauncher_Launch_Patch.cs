using HarmonyLib;
using SmashTools.Targeting;
using System.Linq;
using Vehicles;
using Vehicles.World;
using RimWorld.Planet;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.Launch))]
    public static class CompVehicleLauncher_Launch_Patch
    {
        public static bool Prefix(CompVehicleLauncher __instance, TargetData<GlobalTargetInfo> targetData, IArrivalAction arrivalAction)
        {
            var comp = __instance.Vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                comp.flightPath.AddRange(targetData.targets.Select(x => new FlightNode(x)));
                comp.arrivalAction = arrivalAction as VehicleArrivalAction;
                if (comp.flightMode != FlightMode.Flight)
                {
                    comp.SetFlightMode(true);
                }
                return false;
            }
            return true;
        }
    }
}
