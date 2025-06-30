using HarmonyLib;
using System;
using Verse;
using Verse.AI;

namespace universalflight
{
    [HarmonyPatch(typeof(Reachability), "CanReach", new Type[] { typeof(IntVec3), typeof(LocalTargetInfo),
        typeof(PathEndMode), typeof(TraverseParms)})]
    public static class Reachability_CanReach_Patch
    {
        public static void Postfix(Reachability __instance, IntVec3 start, LocalTargetInfo dest, PathEndMode peMode,
            TraverseParms traverseParams, ref bool __result)
        {
            if (dest.Thing is Pawn pawn)
            {
                var comp = pawn.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    __result = false;
                }
            }
            
            if (traverseParams.pawn is Pawn pawn2)
            {
                var comp = pawn2.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    __result = true;
                }
            }
        }
    }
}
