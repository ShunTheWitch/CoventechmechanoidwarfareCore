using HarmonyLib;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(Verb), "TryFindShootLineFromTo")]
    public static class Verb_TryFindShootLineFromTo_Patch
    {
        public static void Postfix(Verb __instance, ref bool __result, IntVec3 root, LocalTargetInfo targ, ref ShootLine resultingLine)
        {
            var projectile = __instance.GetProjectile();
            if (projectile.CanHitTarget(targ) is false)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                __result = false;
            }
        }
    }
}
