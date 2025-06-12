using HarmonyLib;
using Verse;

namespace BM_ApparelImmunity
{
    [HarmonyPatch(typeof(HediffSet), "AddDirect")]
    public static class HediffSet_AddDirect_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        private static bool Prefix(HediffSet __instance, Pawn ___pawn, Hediff hediff)
        {
            if (___pawn.PreventFromCatching(hediff.def))
            {
                return false;
            }
            return true;
        }
    }
}
