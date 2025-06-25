using HarmonyLib;
using Verse;
using Verse.AI;
using universalflight;

namespace universalflight
{
    [HarmonyPatch(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.PatherTick))]
    public static class Pawn_PathFollower_PatherTick_Patch
    {
        public static bool Prefix(Pawn_PathFollower __instance)
        {
            Pawn pawn = __instance.pawn;
            var compFlightMode = pawn.GetComp<CompFlightMode>();
            if (compFlightMode != null && compFlightMode.InAir)
            {
                return false;
            }
            return true;
        }
    }
}
