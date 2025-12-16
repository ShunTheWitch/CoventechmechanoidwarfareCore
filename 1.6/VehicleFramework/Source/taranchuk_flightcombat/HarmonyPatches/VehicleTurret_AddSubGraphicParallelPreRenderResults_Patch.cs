using HarmonyLib;
using SmashTools.Rendering;
using System.Collections.Generic;
using UnityEngine;
using Vehicles;
using Vehicles.Rendering;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), "AddSubGraphicParallelPreRenderResults")]
    public static class VehicleTurret_AddSubGraphicParallelPreRenderResults_Patch
    {
        public static void Postfix(VehicleTurret __instance, List<PreRenderResults> outList)
        {
            var comp = __instance.vehicle?.GetComp<CompFlightMode>();
            if (comp == null || !comp.InAir || comp.Props.flightGraphicData == null)
            {
                return;
            }

            if (outList == null || outList.Count == 0)
            {
                return;
            }

            var scaleFactors = comp.GetScaleFactors();
            var vehiclePos = comp.curPosition;
            for (int i = 0; i < outList.Count; i++)
            {
                var result = outList[i];
                
                if (!result.valid || !result.draw)
                {
                    continue;
                }
                var offsetFromVehicle = result.position - vehiclePos;
                offsetFromVehicle.x *= scaleFactors.x;
                offsetFromVehicle.z *= scaleFactors.y;
                result.position = vehiclePos + offsetFromVehicle;
                outList[i] = result;
            }
        }
    }
}
