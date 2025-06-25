using HarmonyLib;
using UnityEngine;
using Verse;

namespace universalflight
{
    [HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
    public static class Pawn_DrawTracker_DrawPos_Patch
    {
        public static void Postfix(Pawn_DrawTracker __instance, ref Vector3 __result)
        {
            Pawn pawn = __instance.pawn;
            var compFlightMode = pawn.GetComp<CompFlightMode>();
            if (compFlightMode != null && compFlightMode.InAir)
            {
                __result = compFlightMode.curPosition;
            }
        }
    }
}
