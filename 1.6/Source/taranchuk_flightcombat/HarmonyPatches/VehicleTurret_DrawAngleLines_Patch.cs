using HarmonyLib;
using Vehicles;
using Vehicles.Rendering;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.DrawAngleLines))]
    public static class VehicleTurret_DrawAngleLines_Patch
    {
        public static void Prefix(ref float rotation)
        {
            if (VehicleTurret_DrawTargeter_Patch.curTurret != null)
            {
                var comp = VehicleTurret_DrawTargeter_Patch.curTurret.vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    rotation = comp.AngleAdjusted(comp.CurAngle + comp.FlightAngleOffset);
                }
            }
        }
    }
}
