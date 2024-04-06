using RimWorld;
using Verse;

namespace VehicleMechanitorControl
{
    public class CompProperties_DisconnectMech : CompProperties_AbilityEffect
    {
        public CompProperties_DisconnectMech()
        {
            this.compClass = typeof(CompDisconnectMech);
        }
    }
    public class CompDisconnectMech : CompMechanitorAbility
    {
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var mech = target.Pawn;
            return mech.GetOverseer() == parent.pawn;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var mech = target.Pawn;
            MechanitorUtility.ForceDisconnectMechFromOverseer(mech);
        }
    }
}
