using RimWorld;
using UnityEngine;
using Verse;

namespace ApparelSwitch
{
    public abstract class CompProperties_ChangeGraphicBase : CompProperties
    {
        public string alternateGraphicPath;
    }

    public abstract class CompApparel_ChangeGraphicBase : ThingComp
    {
        public abstract bool ShouldChangeGraphic(Pawn pawn);

        public Graphic GetAlternateGraphic(Apparel apparel, bool forStatue)
        {
            var graphicProps = props as CompProperties_ChangeGraphicBase;
            if (graphicProps == null || graphicProps.alternateGraphicPath.NullOrEmpty())
            {
                return null;
            }
            string path = graphicProps.alternateGraphicPath;
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
