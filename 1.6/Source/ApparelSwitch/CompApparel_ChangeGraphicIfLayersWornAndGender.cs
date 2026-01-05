using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ApparelSwitch
{
    public class CompProperties_ChangeGraphicIfLayersWornAndGender : CompProperties_ChangeGraphicBase
    {
        public List<ApparelLayerDef> changeGraphicIfLayersWorn;
        public Gender gender;

        public CompProperties_ChangeGraphicIfLayersWornAndGender()
        {
            compClass = typeof(CompApparel_ChangeGraphicIfLayersWornAndGender);
        }
    }

    public class CompApparel_ChangeGraphicIfLayersWornAndGender : CompApparel_ChangeGraphicBase
    {
        public CompProperties_ChangeGraphicIfLayersWornAndGender Props => props as CompProperties_ChangeGraphicIfLayersWornAndGender;

        public override bool ShouldChangeGraphic(Pawn pawn)
        {
            if (pawn.gender != Props.gender)
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
