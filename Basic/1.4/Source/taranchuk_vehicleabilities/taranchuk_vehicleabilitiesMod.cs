using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;

namespace taranchuk_vehicleabilities
{
    public class CompProperties_Abilities : VehicleCompProperties
    {
        public List<AbilityDef> abilities;
        public CompProperties_Abilities()
        {
            this.compClass = typeof(CompVehicleAbilities);
        }
    }

    public class CompVehicleAbilities : VehicleComp
    {
        public CompProperties_Abilities Props => base.props as CompProperties_Abilities;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            var pawn = Vehicle;
            if (pawn.abilities is null)
            {
                pawn.abilities = new Pawn_AbilityTracker(pawn);
            }
            foreach (var abilityDef in Props.abilities)
            {
                if (pawn.abilities.GetAbility(abilityDef) is null)
                {
                    pawn.abilities.GainAbility(abilityDef);
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            Vehicle.abilities.AbilitiesTick();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var gizmo in Vehicle.abilities.GetGizmos())
            {
                yield return gizmo;
            }
        }
    }
}
