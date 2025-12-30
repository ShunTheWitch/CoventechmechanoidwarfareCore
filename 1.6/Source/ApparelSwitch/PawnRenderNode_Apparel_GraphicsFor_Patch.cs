using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
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
                var hideComp = apparel.TryGetComp<CompApparel_HideIfLayersWorn>();
                if (hideComp != null && hideComp.ShouldHide(pawn))
                {
                    __result = new List<Graphic>();
                    return false;
                }

                foreach (var comp in apparel.AllComps)
                {
                    if (comp is CompApparel_ChangeGraphicBase changeGraphicComp && changeGraphicComp.ShouldChangeGraphic(pawn))
                    {
                        var alternateGraphic = changeGraphicComp.GetAlternateGraphic(apparel, pawn.Drawer.renderer.StatueColor.HasValue);
                        if (alternateGraphic != null)
                        {
                            var result = new List<Graphic>();
                            result.Add(alternateGraphic);
                            __result = result;
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
