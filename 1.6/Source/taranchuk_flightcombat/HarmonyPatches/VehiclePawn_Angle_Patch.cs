using HarmonyLib;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.Angle), MethodType.Getter)]
    public static class VehiclePawn_Angle_Patch
    {
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(VehiclePawn __instance, ref float __result)
        {
            var comp = __instance.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                __result = __instance.angle;
                return false;
            }
            return true;
        }
    }
}
