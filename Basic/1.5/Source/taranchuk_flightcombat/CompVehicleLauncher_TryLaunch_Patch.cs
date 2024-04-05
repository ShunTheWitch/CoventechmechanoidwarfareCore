using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(CompVehicleLauncher), "TryLaunch")]
    public static class CompVehicleLauncher_TryLaunch_Patch
    {
        public static bool Prefix(CompVehicleLauncher __instance, int destinationTile, 
            AerialVehicleArrivalAction arrivalAction, 
            bool recon = false)
        {
            var comp = __instance.Vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                List<FlightNode> flightPath = LaunchTargeter.FlightPath;
                if (flightPath.LastOrDefault().tile != destinationTile)
                {
                    flightPath.Add(new FlightNode(destinationTile, null));
                }
                comp.flightPath = flightPath;
                comp.arrivalAction = arrivalAction;
                comp.orderRecon = recon;
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
