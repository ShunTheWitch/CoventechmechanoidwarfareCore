using HarmonyLib;
using System;
using Vehicles;
using Verse;
using RimWorld.Planet;

namespace taranchuk_flightcombat
{

    [HotSwappable]
    [HarmonyPatch(typeof(TargetingHelper), nameof(TargetingHelper.TargetMeetsRequirements), new Type[] { typeof(VehicleTurret), typeof(LocalTargetInfo), typeof(IntVec3) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
    public static class TargetingHelper_TargetMeetsRequirements_Patch
    {
        public static void Prefix(VehicleTurret turret, LocalTargetInfo target, out bool __state)
        {
            ThingDef projectileDef = turret.ProjectileDef;
            __state = projectileDef.projectile.flyOverhead;
            if (__state is false && turret.vehicle.Map != null)
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
            if (modExtension == null) return true;

            if (!target.HasThing)
            {
                bool hasAnySpecialization = modExtension.antiAir || modExtension.antiVehicle || modExtension.antiBuilding;
                return !hasAnySpecialization;
            }

            var targetThing = target.Thing;
            
            var flightComp = targetThing.TryGetComp<CompFlightMode>();
            if (flightComp != null && flightComp.InAir)
            {
                return modExtension.antiAir;
            }

            if (modExtension.ground) return true;

            bool hasSpecialization = modExtension.antiAir || modExtension.antiVehicle || modExtension.antiBuilding;
            
            if (!hasSpecialization) return true;

            if (targetThing is VehiclePawn && modExtension.antiVehicle) return true;
            if (targetThing is Building && modExtension.antiBuilding) return true;
            
            return false;
        }
    }
}
