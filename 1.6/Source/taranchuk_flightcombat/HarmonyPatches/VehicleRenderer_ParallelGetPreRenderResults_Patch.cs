using HarmonyLib;
using SmashTools;
using SmashTools.Rendering;
using UnityEngine;
using Vehicles;
using Vehicles.Rendering;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleRenderer), nameof(VehicleRenderer.ParallelGetPreRenderResults))]
    public static class VehicleRenderer_ParallelGetPreRenderResults_Patch
    {
        public static void Postfix(VehicleRenderer __instance, ref PreRenderResults __result)
        {
            var comp = __instance.vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                float angle = comp.AngleAdjusted(comp.CurAngle + comp.FlightAngleOffset);
                Quaternion extraRot = Quaternion.AngleAxis(angle, Vector3.up);
                __result.quaternion *= extraRot;
            }
        }
    }
}
