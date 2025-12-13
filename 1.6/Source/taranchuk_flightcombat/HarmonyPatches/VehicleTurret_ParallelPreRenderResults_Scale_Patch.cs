using HarmonyLib;
using SmashTools.Rendering;
using UnityEngine;
using Vehicles;
using Vehicles.Rendering;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), "ParallelPreRenderResults")]
    public static class VehicleTurret_ParallelPreRenderResults_Scale_Patch
    {
        public static void Postfix(VehicleTurret __instance, ref PreRenderResults __result)
        {
            var comp = __instance.vehicle?.GetComp<CompFlightMode>();
            if (comp == null || !comp.InAir || comp.Props.flightGraphicData == null)
            {
                return;
            }
            if (!__result.valid || !__result.draw)
            {
                return;
            }

            var scaleFactors = comp.GetScaleFactors();
            var vehiclePos = comp.curPosition;
            var offsetFromVehicle = __result.position - vehiclePos;
            offsetFromVehicle.x *= scaleFactors.x;
            offsetFromVehicle.z *= scaleFactors.y;
            __result.position = vehiclePos + offsetFromVehicle;
        }
    }
}
