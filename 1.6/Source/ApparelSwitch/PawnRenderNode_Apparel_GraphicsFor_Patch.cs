using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace ApparelSwitch
{
    [HarmonyPatch(typeof(PawnRenderNode_Apparel), "GraphicsFor")]
    public static class PawnRenderNode_Apparel_GraphicsFor_Patch
    {
        public static bool Prefix(PawnRenderNode_Apparel __instance, Pawn pawn, ref IEnumerable<Graphic> __result)
        {
            var apparel = __instance.apparel;
            if (apparel != null)
            {
                var comp = apparel.TryGetComp<CompApparel_HideIfLayersWorn>();
                if (comp != null && comp.ShouldHide(pawn))
                {
                    __result = new List<Graphic>();
                    return false;
                }
            }
            return true;
        }
    }
}
