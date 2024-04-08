using RimWorld;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    public class CompProperties_RepairMech : CompProperties_AbilityEffect
    {
        public CompProperties_RepairMech()
        {
            this.compClass = typeof(CompRepairMech);
        }
    }

    public class CompRepairMech : CompMechanitorAbility
    {
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var mech = target.Pawn;
            return mech.IsColonyMech && mech is not VehiclePawn && MechRepairUtility.CanRepair(mech);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var mech = target.Pawn;
            while (MechRepairUtility.GetHediffToHeal(mech) is Hediff hediffToHeal)
            {
                mech.health.RemoveHediff(hediffToHeal);
            }
        }
    }
}
