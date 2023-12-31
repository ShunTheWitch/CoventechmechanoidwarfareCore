using HarmonyLib;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.UpdateRotation))]
    public static class VehiclePawn_UpdateRotation_Patch
    {
        public static bool Prefix(VehiclePawn __instance)
        {
            var comp = __instance.GetComp<CompFlightMode>();
            if (comp != null && __instance.InFlightModeOrNonStandardAngle(comp))
            {
                comp.UpdateRotation();
                return false;
            }
            return true;
        }

        public static bool InFlightModeOrNonStandardAngle(this VehiclePawn __instance, CompFlightMode comp)
        {
            return comp.flightMode || (__instance.Angle != 0 && __instance.Angle != -45 && __instance.Angle != 45);
        }
    }
}
