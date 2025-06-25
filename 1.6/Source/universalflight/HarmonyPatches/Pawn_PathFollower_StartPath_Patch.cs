using HarmonyLib;
using Verse;
using Verse.AI;

namespace universalflight
{
    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath))]
    public static class Pawn_PathFollower_StartPath_Patch
    {
        public static bool Prefix(Pawn_PathFollower __instance, LocalTargetInfo dest, PathEndMode peMode)
        {
            var comp = __instance.pawn.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                comp.SetTarget(dest);
                return false;
            }
            return true;
        }
    }
}
