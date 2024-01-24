using HarmonyLib;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(PathingHelper), nameof(PathingHelper.CalculateAngle))]
    public static class PathingHelper_CalculateAngle_Patch
    {
        public static bool Prefix(VehiclePawn vehicle, ref float __result)
        {
            var comp = vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir && comp.Props.flightGraphicData != null)
            {
                __result = comp.AngleAdjusted(comp.CurAngle + comp.FlightAngleOffset);
                return false;
            }
            return true;
        }
    }
}
