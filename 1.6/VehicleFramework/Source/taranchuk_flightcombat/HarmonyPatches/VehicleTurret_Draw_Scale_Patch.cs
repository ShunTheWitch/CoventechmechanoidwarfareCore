using HarmonyLib;
using SmashTools.Rendering;
using UnityEngine;
using Vehicles;

namespace taranchuk_flightcombat
{

    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), "Draw")]
    public static class VehicleTurret_Draw_Scale_Patch
    {
        public static void Prefix(VehicleTurret __instance, ref Vector2? __state)
        {
            var comp = __instance.vehicle?.GetComp<CompFlightMode>();
            if (comp == null || !comp.InAir || comp.Props.flightGraphicData == null)
            {
                return;
            }
            if (__instance.Graphic == null)
            {
                return;
            }
            __state = __instance.Graphic.drawSize;
            var scaleFactors = comp.GetScaleFactors();
            var scaleFactor = (scaleFactors.x + scaleFactors.y) / 2f;
            __instance.Graphic.drawSize = __state.Value * scaleFactor;
        }
        public static void Postfix(VehicleTurret __instance, Vector2? __state)
        {
            if (__state.HasValue && __instance.Graphic != null)
            {
                __instance.Graphic.drawSize = __state.Value;
            }
        }
    }
}
