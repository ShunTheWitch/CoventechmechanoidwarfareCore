using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ApparelSwitch
{
    public class CompProperties_ChangeGraphicIfLayersWorn : CompProperties
    {
        public List<ApparelLayerDef> changeGraphicIfLayersWorn;
        public string alternateGraphicPath;

        public CompProperties_ChangeGraphicIfLayersWorn()
        {
            compClass = typeof(CompApparel_ChangeGraphicIfLayersWorn);
        }
    }

    public class CompApparel_ChangeGraphicIfLayersWorn : ThingComp
    {
        public CompProperties_ChangeGraphicIfLayersWorn Props => props as CompProperties_ChangeGraphicIfLayersWorn;

        public bool ShouldChangeGraphic(Pawn pawn)
        {
            if (Props.changeGraphicIfLayersWorn == null || Props.changeGraphicIfLayersWorn.Count == 0)
            {
                return false;
            }
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (apparel == parent)
                {
                    continue;
                }
                foreach (var layer in apparel.def.apparel.layers)
                {
                    if (Props.changeGraphicIfLayersWorn.Contains(layer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Graphic GetAlternateGraphic(Apparel apparel, BodyTypeDef bodyType, bool forStatue)
        {
            if (Props.alternateGraphicPath.NullOrEmpty())
            {
                return null;
            }
            string path = Props.alternateGraphicPath;
            Shader shader = ShaderDatabase.Cutout;
            if (!forStatue)
            {
                if (apparel.StyleDef?.graphicData.shaderType != null)
                {
                    shader = apparel.StyleDef.graphicData.shaderType.Shader;
                }
                else if ((apparel.StyleDef == null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef != null && apparel.StyleDef.UseWornGraphicMask))
                {
                    shader = ShaderDatabase.CutoutComplex;
                }
            }
            return GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
        }
    }
}
