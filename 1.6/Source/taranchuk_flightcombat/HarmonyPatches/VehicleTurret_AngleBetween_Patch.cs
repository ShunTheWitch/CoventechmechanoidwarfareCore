using HarmonyLib;
using SmashTools;
using System;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.AngleBetween))]
    public static class VehicleTurret_AngleBetween_Patch
    {
        public static bool Prefix(VehicleTurret __instance, Vector3 position, ref bool __result)
        {
            if (__instance.angleRestricted != Vector2.zero && __instance.attachedTo is null)
            {
                var comp = __instance.vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    __result = AngleBetween(__instance, position, comp);
                    return false;
                }
            }
            return true;
        }

        public static bool AngleBetween(VehicleTurret __instance, Vector3 position, CompFlightMode comp)
        {
            float rotationOffset = comp.AngleAdjusted(comp.CurAngle + comp.FlightAngleOffset);
            float start = __instance.angleRestricted.x + rotationOffset;
            float end = __instance.angleRestricted.y + rotationOffset;

            if (start > 360)
            {
                start -= 360;
            }
            if (end > 360)
            {
                end -= 360;
            }
            float mid = (position - __instance.TurretLocation).AngleFlat();
            end = (end - start) < 0f ? end - start + 360 : end - start;
            mid = (mid - start) < 0f ? mid - start + 360 : mid - start;
            var result = mid < end;
            return result;
        }
    }
}
