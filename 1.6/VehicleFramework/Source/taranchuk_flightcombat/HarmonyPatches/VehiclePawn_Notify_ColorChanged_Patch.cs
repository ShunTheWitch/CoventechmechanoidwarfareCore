using HarmonyLib;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.Notify_ColorChanged))]
    public static class VehiclePawn_Notify_ColorChanged_Patch
    {
        public static void Postfix(VehiclePawn __instance)
        {
            var comp = __instance.GetComp<CompFlightMode>();
            if (comp != null && comp.Props.flightGraphicData != null)
            {
                comp.DestroyFlightGraphic();
            }
        }
    }
}
