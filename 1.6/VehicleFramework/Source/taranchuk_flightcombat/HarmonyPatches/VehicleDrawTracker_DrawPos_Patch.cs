using HarmonyLib;
using UnityEngine;
using Vehicles;
using Vehicles.Rendering;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleDrawTracker), nameof(VehicleDrawTracker.DrawPos), MethodType.Getter)]
    public static class VehicleDrawTracker_DrawPos_Patch
    {
        public static bool Prefix(VehicleDrawTracker __instance, ref Vector3 __result)
        {
            var comp = __instance.vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                __result = new Vector3(comp.curPosition.x, Altitudes.AltitudeFor(AltitudeLayer.Skyfaller), comp.curPosition.z);
                return false;
            }
            return true;
        }
    }
}
