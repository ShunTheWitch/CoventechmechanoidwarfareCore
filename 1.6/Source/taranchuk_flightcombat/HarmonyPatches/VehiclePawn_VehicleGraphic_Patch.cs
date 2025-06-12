using HarmonyLib;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.VehicleGraphic), MethodType.Getter)]
    public static class VehiclePawn_VehicleGraphic_Patch
    {
        public static bool Prefix(VehiclePawn __instance, ref Graphic_Vehicle __result)
        {
            var comp = __instance.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir && comp.Props.flightGraphicData != null)
            {
                __result = comp.FlightGraphic;
                return false;
            }
            return true;
        }
    }
}
