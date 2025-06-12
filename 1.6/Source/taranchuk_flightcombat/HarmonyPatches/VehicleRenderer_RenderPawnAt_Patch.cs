using HarmonyLib;
using SmashTools;
using System;
using UnityEngine;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleRenderer), nameof(VehicleRenderer.RenderPawnAt_TEMP), new Type[] { typeof(Vector3), typeof(Rot8), typeof(float), typeof(bool) })]
    public static class VehicleRenderer_RenderPawnAt_Patch
    {
        public static void Prefix(VehicleRenderer __instance, Vector3 drawLoc, Rot8 rot, ref float extraRotation, bool northSouthRotation)
        {
            var comp = __instance.vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                extraRotation += comp.AngleAdjusted(comp.CurAngle + comp.FlightAngleOffset);
            }
        }
    }
}
