using RimWorld;
using Verse;

namespace ApparelSwitch
{
    public class CompProperties_ChangeGraphicIfGender : CompProperties_ChangeGraphicBase
    {
        public Gender gender;

        public CompProperties_ChangeGraphicIfGender()
        {
            compClass = typeof(CompApparel_ChangeGraphicIfGender);
        }
    }

    public class CompApparel_ChangeGraphicIfGender : CompApparel_ChangeGraphicBase
    {
        public CompProperties_ChangeGraphicIfGender Props => props as CompProperties_ChangeGraphicIfGender;

        public override bool ShouldChangeGraphic(Pawn pawn)
        {
            return pawn.gender == Props.gender;
        }
    }
}
