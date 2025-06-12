using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BM_PowerArmor
{
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "AppendDrawRequests")]
    public static class PawnRenderNodeWorker_AppendDrawRequests_Patch
    {
        public static bool Prefix(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            if ((node is PawnRenderNode_Body || node.parent is PawnRenderNode_Body) && parms.pawn.apparel.AnyApparel)
            {
                var powerArmor = parms.pawn.apparel.WornApparel
                    .FirstOrDefault(x => x.def.GetCompProperties<CompProperties_PowerArmor>() != null);
                if (powerArmor != null)
                {
                    requests.Add(new PawnGraphicDrawRequest(node)); // adds an empty draw request to not draw body
                    return false;
                }
            }
            return true;
        }
    }
}
