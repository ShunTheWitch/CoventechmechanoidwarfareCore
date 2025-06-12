using RimWorld;

namespace XenotypeTransformation
{
    public class CompProperties_AbilityGiveHediff_RequireXenotype : CompProperties_AbilityGiveHediff
    {
        public XenotypeDef requireXenotype;
        public string cannotTransformReason;

        public CompProperties_AbilityGiveHediff_RequireXenotype()
        {
            this.compClass = typeof(CompAbilityEffect_GiveHediff_RequireXenotype);
        }
    }

    public class CompAbilityEffect_GiveHediff_RequireXenotype : CompAbilityEffect_GiveHediff
    {
        public CompProperties_AbilityGiveHediff_RequireXenotype Props => base.props as CompProperties_AbilityGiveHediff_RequireXenotype;

        public override bool ShouldHideGizmo => parent.pawn.genes.xenotype == Props.hediffDef.CompProps<HediffCompProperties_XenotypeTransformation>().transformInto;

        public override bool GizmoDisabled(out string reason)
        {
            if (Props.requireXenotype != null && parent.pawn.genes.xenotype != Props.requireXenotype)
            {
                reason = Props.cannotTransformReason;
                return true;
            }
            return base.GizmoDisabled(out reason);
        }
    }


}
