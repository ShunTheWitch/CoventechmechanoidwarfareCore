using HarmonyLib;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(Vehicle_DrawTracker), nameof(Vehicle_DrawTracker.DrawPos), MethodType.Getter)]
    public static class Vehicle_DrawTracker_DrawPos_Patch
    {
        public static bool Prefix(Vehicle_DrawTracker __instance, ref Vector3 __result)
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
