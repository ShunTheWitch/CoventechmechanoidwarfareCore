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
            if (comp != null && comp.flightMode)
            {
                comp.UpdateRotation();
                return false;
            }
            return true;
        }
    }
}
