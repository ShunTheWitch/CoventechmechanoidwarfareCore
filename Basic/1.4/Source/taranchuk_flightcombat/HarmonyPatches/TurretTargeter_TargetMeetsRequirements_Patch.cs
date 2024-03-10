using HarmonyLib;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(TurretTargeter), "TargetMeetsRequirements")]
    public static class TurretTargeter_TargetMeetsRequirements_Patch
    {
        public static void Prefix(VehicleTurret turret, LocalTargetInfo target, out bool __state)
        {
            ThingDef projectileDef = turret.ProjectileDef;
            __state = projectileDef.projectile.flyOverhead;
            if (__state is false)
            {
                if (target.HasThing && (turret.TurretLocation.ToIntVec3().Roofed(turret.vehicle.Map) is false 
                    || (turret.vehicle.GetComp<CompFlightMode>()?.InAir ?? false)))
                {
                    var modExtension = projectileDef.GetModExtension<ProjectileModes>();
                    if (modExtension != null && modExtension.antiAir)
                    {
                        var comp = target.Thing.TryGetComp<CompFlightMode>();
                        if (comp != null && comp.InAir)
                        {
                            projectileDef.projectile.flyOverhead = true;
                        }
                    }
                }
            }
        }

        public static void Postfix(ref bool __result, bool __state, VehicleTurret turret, LocalTargetInfo target)
        {
            var projectile = turret.ProjectileDef;
            projectile.projectile.flyOverhead = __state;
            if (projectile.CanHitTarget(target) is false)
            {
                __result = false;
            }
        }

        public static bool CanHitTarget(this ThingDef projectile, LocalTargetInfo target)
        {
            if (projectile == null) return true;
            var modExtension = projectile.GetModExtension<ProjectileModes>();
            if (modExtension != null)
            {
                if (target.HasThing is false && modExtension.antiAir)
                {
                    return false;
                }
                var comp = target.Thing.TryGetComp<CompFlightMode>();
                if (comp != null)
                {
                    if (modExtension.ground && modExtension.antiAir is false && comp.InAir)
                    {
                        return false;
                    }
                    else if (modExtension.ground is false && modExtension.antiAir && comp.InAir is false)
                    {
                        return false;
                    }
                }
                else if (modExtension.antiAir)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
