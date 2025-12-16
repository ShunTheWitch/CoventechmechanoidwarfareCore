using HarmonyLib;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.UpdateRotationLock))]
    public static class VehicleTurret_UpdateRotationLock_Patch
    {
        public static bool Prefix(VehicleTurret __instance)
        {
            var comp = __instance.vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                UpdateRotationLockForFlight(__instance, comp);
                return false;
            }
            return true;
        }
        private static void UpdateRotationLockForFlight(VehicleTurret __instance, CompFlightMode comp)
        {
            if (__instance.vehicle != null)
            {
                if (!__instance.targetInfo.IsValid && TurretTargeter.Turret != __instance && !__instance.vehicle.CompVehicleTurrets.Deploying)
                {
                    float angleDifference = comp.CurAngle - __instance.parentAngleCached;
                    if (__instance.attachedTo is null)
                    {
                        var rotOffset = 90 * (comp.FlightRotation.AsInt - __instance.parentRotCached.AsInt) + angleDifference;
                        __instance.transform.rotation += rotOffset;
                    }
                    __instance.TurretRotationTargeted = __instance.transform.rotation;
                }
                __instance.parentRotCached = comp.FlightRotation;
                __instance.parentAngleCached = comp.CurAngle;
            }
        }
    }
}