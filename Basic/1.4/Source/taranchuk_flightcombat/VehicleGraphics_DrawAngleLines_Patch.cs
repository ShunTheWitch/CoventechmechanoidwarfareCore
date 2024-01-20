using HarmonyLib;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleGraphics), nameof(VehicleGraphics.DrawAngleLines))]
    public static class VehicleGraphics_DrawAngleLines_Patch
    {
        public static void Prefix(ref float additionalAngle)
        {
            if (VehicleTurret_DrawTargeter_Patch.curTurret != null)
            {
                var comp = VehicleTurret_DrawTargeter_Patch.curTurret.vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir) 
                {
                    additionalAngle = comp.AngleAdjusted(comp.CurAngle - 90);
                }
            }
        }
    }
}
