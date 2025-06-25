using HarmonyLib;
using RimWorld;
using Verse;

namespace universalflight
{
    [HarmonyPatch(typeof(PawnUtility), "PawnsCanShareCellBecauseOfBodySize")]
    public static class PawnUtility_PawnsCanShareCellBecauseOfBodySize_Patch
    {
        public static void Postfix(ref bool __result, Pawn p1, Pawn p2)
        {
            if (__result is false)
            {
                __result = CanShareCell(p1);
                if (__result is false)
                {
                    __result = CanShareCell(p2);
                }
            }
        }

        private static bool CanShareCell(Pawn pawn)
        {
            var comp = pawn.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                return true;
            }
            return false;
        }
    }
}
