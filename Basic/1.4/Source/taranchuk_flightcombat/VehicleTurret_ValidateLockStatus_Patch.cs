using HarmonyLib;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), "ValidateLockStatus")]
    public static class VehicleTurret_ValidateLockStatus_Patch
    {
        public static bool Prefix(VehicleTurret __instance)
        {
            var comp = __instance.vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                ValidateLockStatus(__instance, comp);
                return false;
            }
            //ValidateLockStatusGround(__instance);
            //return false;
            return true;
        }

        //private static void ValidateLockStatusGround(VehicleTurret __instance)
        //{
        //    if (__instance.vehicle != null)
        //    {
        //        if (!__instance.cannonTarget.IsValid && TurretTargeter.Turret != __instance && !__instance.vehicle.Deploying)
        //        {
        //            float angleDifference = __instance.vehicle.Angle - __instance.parentAngleCached;
        //            if (__instance.attachedTo is null)
        //            {
        //                var rotOffset = 90 * (__instance.vehicle.Rotation.AsInt - __instance.parentRotCached.AsInt) + angleDifference;
        //                __instance.rotation += rotOffset;
        //                if (rotOffset != 0)
        //                {
        //                    Log.Message("ground rotOffset: " + rotOffset);
        //                    Log.ResetMessageCount();
        //                }
        //            }
        //            __instance.TurretRotationTargeted = __instance.rotation;
        //        }
        //        __instance.parentRotCached = __instance.vehicle.Rotation;
        //        __instance.parentAngleCached = __instance.vehicle.Angle;
        //    }
        //}
        private static void ValidateLockStatus(VehicleTurret __instance, CompFlightMode comp)
        {
            if (__instance.vehicle != null)
            {
                if (!__instance.cannonTarget.IsValid && TurretTargeter.Turret != __instance && !__instance.vehicle.Deploying)
                {
                    float angleDifference = comp.CurAngle - __instance.parentAngleCached;
                    if (__instance.attachedTo is null)
                    {
                        var oldValue = __instance.rotation;
                        var rotOffset = 90 * (comp.FlightRotation.AsInt - __instance.parentRotCached.AsInt) + angleDifference;
                        __instance.rotation += rotOffset;
                        var newValue = __instance.rotation;
                        if (oldValue != newValue)
                        {
                            Log.Message("__instance.rotation: oldValue: " + oldValue + " - newValue: " + (oldValue + rotOffset) + " - rotOffset: " + rotOffset);
                        }
                    }
                    __instance.TurretRotationTargeted = __instance.rotation;
                }
                __instance.parentRotCached = comp.FlightRotation;
                __instance.parentAngleCached = comp.CurAngle;
            }
        }
    }
}
