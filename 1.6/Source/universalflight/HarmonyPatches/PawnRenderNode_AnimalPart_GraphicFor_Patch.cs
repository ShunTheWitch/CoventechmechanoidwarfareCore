using HarmonyLib;
using Verse;

namespace universalflight
{
    [HarmonyPatch(typeof(PawnRenderNode_AnimalPart), "GraphicFor")]
    public static class PawnRenderNode_AnimalPart_GraphicFor_Patch
    {
        public static void Postfix(Pawn pawn, ref Graphic __result)
        {
            if (CompFlightMode.skipFlightGraphic)
            {
                return;
            }
            var comp = pawn.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                __result = comp.FlightGraphic;
            }
        }
    }
}
