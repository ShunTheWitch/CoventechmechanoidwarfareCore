using HarmonyLib;
using Verse;
using universalflight;

namespace universalflight
{
    [HotSwappable]
    [HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.UpdateRotation))]
    public static class Pawn_RotationTracker_UpdateRotation_Patch
    {
        public static bool Prefix(Pawn_RotationTracker __instance)
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
