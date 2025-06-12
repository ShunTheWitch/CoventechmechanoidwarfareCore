using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Taranchuk_ColorPicker
{
    [HotSwappable]
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "GetMaterialPropertyBlock")]
    public static class PawnRenderNodeWorker_GetMaterialPropertyBlock_Patch
    {
        public static void Postfix(PawnRenderNodeWorker __instance, PawnRenderNode node, Material material, PawnDrawParms parms)
        {
            if (parms.pawn.Faction == Faction.OfPlayer)
            {
                var comp = parms.pawn.GetComp<CompCustomColorPicker>();
                if (comp != null)
                {
                    MaterialPropertyBlock matPropBlock = node.MatPropBlock;
                    if (comp.colorOne.HasValue)
                    {
                        matPropBlock.SetColor(ShaderPropertyIDs.OverlayColor, comp.colorOne.Value);
                        matPropBlock.SetColor(ShaderPropertyIDs.Color, comp.colorOne.Value);
                    }
                    if (comp.colorTwo.HasValue)
                    {
                        matPropBlock.SetColor(ShaderPropertyIDs.ColorTwo, comp.colorTwo.Value);
                    }
                    matPropBlock.SetFloat(ShaderPropertyIDs.OverlayOpacity, 0.5f);
                }
            }
        }
    }
}
