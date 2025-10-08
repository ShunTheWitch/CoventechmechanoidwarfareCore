using RimWorld;
using Verse;

namespace VehicleMechanitorControl
{
    public class Verb_CastAbilityVehicle : Verb_CastAbility
    {
        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            if (verbProps.range <= 0f)
            {
                return true;
            }
            if (caster == null || !caster.Spawned)
            {
                return false;
            }
            if (targ == caster)
            {
                return true;
            }
            return CanHitTargetFrom(caster.Position, targ);
        }
    }
}
