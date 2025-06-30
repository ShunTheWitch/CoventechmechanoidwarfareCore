using HarmonyLib;
using Verse;

namespace universalflight
{
    [HarmonyPatch(typeof(Verb), "TryFindShootLineFromTo")]
    public static class Verb_TryFindShootLineFromTo_Patch
    {
        public static void Prefix(Verb __instance, IntVec3 root, LocalTargetInfo targ, out bool __state)
        {
            __state = __instance.verbProps.requireLineOfSight;
            if (__state)
            {
                var projectile = __instance.GetProjectile();
                if (projectile != null && targ.HasThing &&
                    __instance.Caster.Position.Roofed(__instance.Caster.Map) is false)
                {
                    var modExtension = projectile.GetModExtension<ProjectileModes>();
                    if (modExtension != null && modExtension.antiAir)
                    {
                        var comp = targ.Thing.TryGetComp<CompFlightMode>();
                        if (comp != null && comp.InAir)
                        {
                            __instance.verbProps.requireLineOfSight = false;
                        }
                    }
                }
            }
        }
        public static void Postfix(Verb __instance, ref bool __result, bool __state, IntVec3 root, LocalTargetInfo targ, ref ShootLine resultingLine)
        {
            __instance.verbProps.requireLineOfSight = __state;
            var projectile = __instance.GetProjectile();
            if (projectile.CanHitTarget(targ) is false)
            {
                resultingLine = default(ShootLine);
                __result = false;
            }
            else if (__instance.IsMeleeAttack && targ.HasThing
                && targ.Thing.TryGetComp<CompFlightMode>() is CompFlightMode comp && comp.InAir)
            {
                resultingLine = default(ShootLine);
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
