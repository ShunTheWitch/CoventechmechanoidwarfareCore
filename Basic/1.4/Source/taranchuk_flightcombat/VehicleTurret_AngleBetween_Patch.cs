using HarmonyLib;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    //[HotSwappable]
    //[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.TurretRotation), MethodType.Setter)]
    //public static class VehicleTurret_TurretRotation_Patch2
    //{
    //    public static void Postfix(VehicleTurret __instance, float value)
    //    {
    //        if (__instance.angleRestricted != Vector2.zero)
    //        {
    //            Log.Message(__instance + " - " + value);
    //        }
    //    }
    //}
    //[HotSwappable]
    //[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.TurretRotation), MethodType.Getter)]
    //public static class VehicleTurret_TurretRotation_Patch
    //{
    //    public static bool Prefix(VehicleTurret __instance, ref float __result)
    //    {
    //        if (__instance.angleRestricted != Vector2.zero)
    //        {
    //            __result = TurretRotation(__instance);
    //            return false;
    //        }
    //        return true;
    //    }
    //
    //    public static float TurretRotation(VehicleTurret __instance)
    //    {
    //        if (!__instance.IsTargetable && __instance.attachedTo is null)
    //        {
    //            Log.Message(__instance + " TurretRotation return 1");
    //            return __instance.defaultAngleRotated + __instance.vehicle.FullRotation.AsAngle;
    //        }
    //        __instance.ValidateLockStatus();
    //
    //        __instance.rotation = __instance.rotation.ClampAndWrap(0, 360);
    //
    //        if (__instance.attachedTo != null)
    //        {
    //            Log.Message(__instance + " TurretRotation return 2");
    //
    //            return __instance.rotation + __instance.attachedTo.TurretRotation;
    //        }
    //        Log.Message(__instance + " TurretRotation return 3");
    //        return __instance.rotation;
    //    }
    //}
    //
    //[HotSwappable]
    //[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.AlignToAngleRestricted))]
    //public static class VehicleTurret_AlignToAngleRestricted_Patch
    //{
    //    public static bool Prefix(VehicleTurret __instance, float angle)
    //    {
    //        if (__instance.angleRestricted != Vector2.zero)
    //        {
    //            AlignToAngleRestricted(__instance, angle);
    //            return false;
    //        }
    //        return true;
    //    }
    //
    //    public static void AlignToAngleRestricted(VehicleTurret __instance, float angle)
    //    {
    //        float additionalAngle = __instance.attachedTo?.TurretRotation ?? 0;
    //        if (__instance.turretDef.autoSnapTargeting)
    //        {
    //            __instance.TurretRotation = angle - additionalAngle;
    //            __instance.TurretRotationTargeted = __instance.rotation;
    //            Log.Message("1 AlignToAngleRestricted Angle: " + angle);
    //        }
    //        else
    //        {
    //            __instance.TurretRotationTargeted = (angle - additionalAngle).ClampAndWrap(0, 360);
    //            Log.Message("2 AlignToAngleRestricted Angle: " + angle);
    //        }
    //    }
    //
    //}

    //[HotSwappable]
    //[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.TurretTargeterTick))]
    //public static class test
    //{
    //    public static bool Prefix(VehicleTurret __instance, ref bool __result)
    //    {
    //        if (__instance.angleRestricted != Vector2.zero)
    //        {
    //            __result = TurretTargeterTick(__instance);
    //            return false;
    //        }
    //        return true;
    //    }
    //
    //    private static bool TurretTargeterTick(VehicleTurret __instance)
    //    {
    //        if (__instance.TurretTargetValid)
    //        {
    //            if (__instance.rotation == __instance.TurretRotationTargeted && !__instance.TargetLocked)
    //            {
    //                __instance.TargetLocked = true;
    //                __instance.ResetPrefireTimer();
    //            }
    //            else if (!__instance.TurretTargetValid)
    //            {
    //                __instance.SetTarget(LocalTargetInfo.Invalid);
    //                Log.Message(__instance + "TurretTargeterTick return 1");
    //                return TurretTargeter.Turret == __instance;
    //            }
    //
    //            if (__instance.IsTargetable && !TurretTargeter.TargetMeetsRequirements(__instance, __instance.cannonTarget))
    //            {
    //                __instance.SetTarget(LocalTargetInfo.Invalid);
    //                __instance.TargetLocked = false;
    //                Log.Message(__instance + " TurretTargeterTick return 2");
    //                return TurretTargeter.Turret == __instance;
    //            }
    //            if (__instance.PrefireTickCount > 0)
    //            {
    //                if (__instance.cannonTarget.HasThing)
    //                {
    //                    __instance.TurretRotationTargeted = __instance.TurretLocation.AngleToPoint(__instance.cannonTarget.Thing.DrawPos);
    //                    if (__instance.attachedTo != null)
    //                    {
    //                        __instance.TurretRotationTargeted -= __instance.attachedTo.TurretRotation;
    //                    }
    //                }
    //                else
    //                {
    //                    __instance.TurretRotationTargeted = __instance.TurretLocation.ToIntVec3().AngleToCell(__instance.cannonTarget.Cell, __instance.vehicle.Map);
    //                    if (__instance.attachedTo != null)
    //                    {
    //                        __instance.TurretRotationTargeted -= __instance.attachedTo.TurretRotation;
    //                    }
    //                }
    //
    //                if (__instance.turretDef.autoSnapTargeting)
    //                {
    //                    __instance.rotation = __instance.TurretRotationTargeted;
    //                }
    //
    //                if (__instance.TargetLocked && __instance.ReadyToFire)
    //                {
    //                    __instance.PrefireTickCount--;
    //                }
    //            }
    //            else if (__instance.ReadyToFire)
    //            {
    //                if (__instance.IsTargetable && __instance.RotationAligned && (__instance.cannonTarget.Pawn is null || !__instance.CheckTargetInvalid()))
    //                {
    //                    __instance.GroupTurrets.ForEach(t => t.PushTurretToQueue());
    //                }
    //                else if (__instance.FullAuto)
    //                {
    //                    __instance.GroupTurrets.ForEach(t => t.PushTurretToQueue());
    //                }
    //            }
    //            Log.Message(__instance + " TurretTargeterTick return 3");
    //
    //            return true;
    //        }
    //        else if (__instance.IsTargetable)
    //        {
    //            Log.Message(__instance + " TurretTargeterTick return 4");
    //            return TurretTargeter.Turret == __instance;
    //        }
    //        Log.Message(__instance + " TurretTargeterTick return 5");
    //        return false;
    //    }
    //}

    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.AngleBetween))]
    public static class VehicleTurret_AngleBetween_Patch
    {
        public static bool Prefix(VehicleTurret __instance, Vector3 mousePosition, ref bool __result)
        {
            if (__instance.angleRestricted != Vector2.zero && __instance.attachedTo is null)
            {
                var comp = __instance.vehicle.GetComp<CompFlightMode>();
                if (comp.InAir)
                {
                    __result = AngleBetween(__instance, mousePosition, comp);
                    return false;
                }
            }
            return true;
        }
    
        public static bool AngleBetween(VehicleTurret __instance, Vector3 mousePosition, CompFlightMode comp)
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
            float mid = (mousePosition - __instance.TurretLocation).AngleFlat();
            end = (end - start) < 0f ? end - start + 360 : end - start;
            mid = (mid - start) < 0f ? mid - start + 360 : mid - start;
            var result = mid < end;
            return result;
        }
    }
}
