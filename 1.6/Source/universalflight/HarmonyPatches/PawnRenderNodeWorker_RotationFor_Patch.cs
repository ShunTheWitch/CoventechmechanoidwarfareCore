using HarmonyLib;
using UnityEngine;
using Verse;

namespace universalflight
{
    [HotSwappable]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "RotationFor")]
    public static class PawnRenderNodeWorker_RotationFor_Patch
    {
        public static void Postfix(PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms, ref Quaternion __result)
        {
            var pawn = node.tree.pawn;
            if (!parms.Portrait && node is PawnRenderNode_Body)
            {
                var comp = pawn.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    __result *= Quaternion.Euler(Vector3.up * comp.CurAngle);
                }
            }
        }
    }
}
