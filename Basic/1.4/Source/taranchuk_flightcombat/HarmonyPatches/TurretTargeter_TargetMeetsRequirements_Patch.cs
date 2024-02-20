using HarmonyLib;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(TurretTargeter), "TargetMeetsRequirements")]
    public static class TurretTargeter_TargetMeetsRequirements_Patch
    {
        public static void Postfix(ref bool __result, VehicleTurret turret, LocalTargetInfo target)
        {
            var projectile = turret.ProjectileDef;
            if (projectile.CanHitTarget(target) is false)
            {
                __result = false;
            }
        }

        public static bool CanHitTarget(this ThingDef projectile, LocalTargetInfo target)
        {
            if (projectile == null) return true;
            if (projectile.HasModExtension<AntiAirProjectile>())
            {
                if (target.HasThing is false)
                {
                    return false;
                }
                else
                {
                    var comp = target.Thing.TryGetComp<CompFlightMode>();
                    if (comp is null || comp.InAir is false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
