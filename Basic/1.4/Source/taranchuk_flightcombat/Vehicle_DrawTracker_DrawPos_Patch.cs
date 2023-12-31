using HarmonyLib;
using UnityEngine;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(Vehicle_DrawTracker), nameof(Vehicle_DrawTracker.DrawPos), MethodType.Getter)]
    public static class Vehicle_DrawTracker_DrawPos_Patch
    {
        public static bool Prefix(Vehicle_DrawTracker __instance, ref Vector3 __result)
        {
            var comp = __instance.vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.flightMode)
            {
                __result = comp.curPosition;
                return false;
            }
            return true;
        }
    }
}
