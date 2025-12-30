using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ApparelSwitch
{
    public class CompProperties_ChangeGraphicIfLayersWorn : CompProperties_ChangeGraphicBase
    {
        public List<ApparelLayerDef> changeGraphicIfLayersWorn;

        public CompProperties_ChangeGraphicIfLayersWorn()
        {
            compClass = typeof(CompApparel_ChangeGraphicIfLayersWorn);
        }
    }

    public class CompApparel_ChangeGraphicIfLayersWorn : CompApparel_ChangeGraphicBase
    {
        public CompProperties_ChangeGraphicIfLayersWorn Props => props as CompProperties_ChangeGraphicIfLayersWorn;

        public override bool ShouldChangeGraphic(Pawn pawn)
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
    }
}
