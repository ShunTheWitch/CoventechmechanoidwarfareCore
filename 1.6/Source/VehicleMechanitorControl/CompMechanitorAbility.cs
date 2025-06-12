using RimWorld;

namespace VehicleMechanitorControl
{
    public abstract class CompMechanitorAbility : CompAbilityEffect
    {
        public override bool ShouldHideGizmo => MechanitorUtility.IsMechanitor(parent.pawn) is false;
    }
}
