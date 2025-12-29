using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ApparelSwitch
{
    public class CompProperties_HideIfLayersWorn : CompProperties
    {
        public List<ApparelLayerDef> hideIfLayersWorn;

        public CompProperties_HideIfLayersWorn()
        {
            compClass = typeof(CompApparel_HideIfLayersWorn);
        }
    }

    public class CompApparel_HideIfLayersWorn : ThingComp
    {
        public CompProperties_HideIfLayersWorn Props => props as CompProperties_HideIfLayersWorn;

        public bool ShouldHide(Pawn pawn)
        {
            if (Props.hideIfLayersWorn == null || Props.hideIfLayersWorn.Count == 0)
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
                    if (Props.hideIfLayersWorn.Contains(layer))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
