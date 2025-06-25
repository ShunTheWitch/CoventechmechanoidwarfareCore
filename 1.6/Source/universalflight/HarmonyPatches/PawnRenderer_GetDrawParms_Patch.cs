using HarmonyLib;
using UnityEngine;
using Verse;

namespace universalflight
{
    [HotSwappable]
    [HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
    public static class PawnRenderer_GetDrawParms_Patch
    {
        public static void Prefix(PawnRenderer __instance, Vector3 rootLoc, ref float angle, ref PawnRenderFlags flags)
        {
            var compFlightMode = __instance.pawn.GetComp<CompFlightMode>();
            if (compFlightMode != null && compFlightMode.InAir)
            {
                angle = compFlightMode.AngleAdjusted(compFlightMode.CurAngle + compFlightMode.FlightAngleOffset);
            }
        }
    }
}
