using HarmonyLib;
using System;
using Verse;
using Verse.AI;

namespace universalflight
{
    [HarmonyPatch(typeof(ReachabilityUtility), "CanReach", new Type[] { typeof(Pawn), typeof(LocalTargetInfo),
        typeof(PathEndMode), typeof(Danger), typeof(bool), typeof(bool), typeof(TraverseMode) })]
    public static class ReachabilityUtility_CanReach_Patch
    {
        public static void Postfix(ref bool __result, LocalTargetInfo dest)
        {
            if (dest.Thing is Pawn pawn)
            {
                var comp = pawn.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    __result = false;
                }
            }
        }
    }
}
