using RimWorld;
using Verse;

namespace VehicleMechanitorControl
{
    public class CompProperties_ControlMech : CompProperties_AbilityEffect
    {
        public CompProperties_ControlMech()
        {
            this.compClass = typeof(CompControlMech);
        }
    }

    public class CompControlMech : CompMechanitorAbility
    {
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var mech = target.Pawn;
            return mech.IsColonyMech && MechanitorUtility.CanControlMech(parent.pawn, mech) && mech.GetOverseer() != parent.pawn;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var mech = target.Pawn;
            mech.GetOverseer()?.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, mech);
            parent.pawn.relations.AddDirectRelation(PawnRelationDefOf.Overseer, mech);
        }
    }
}
