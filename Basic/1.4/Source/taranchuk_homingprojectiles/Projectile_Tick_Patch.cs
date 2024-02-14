using HarmonyLib;
using Verse;

namespace taranchuk_homingprojectiles
{
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Tick))]
    public static class Projectile_Tick_Patch
    {
        public static void Postfix(Projectile __instance)
        {
            if (__instance.IsHomingProjectile(out var comp))
            {
                if (comp.CanChangeTrajectory())
                {
                    //if (comp.RotateTowards(__instance.intendedTarget.CenterVector3, out var destination))
                    //{
                    //    __instance.SetDestination(destination);
                    //}
                    __instance.SetDestination(__instance.intendedTarget.CenterVector3);
                }
                if (__instance.Destroyed is false && comp.Props.lifetimeTicks > 0 & Find.TickManager.TicksGame - comp.launchTick > comp.Props.lifetimeTicks)
                {
                    __instance.ImpactSomething();
                }
            }
        }
    }
}
