using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    public class CompProperties_MechanitorControl : VehicleCompProperties
    {
        public int bandwidthGain;
        public float mechControlRange;
        public CompProperties_MechanitorControl()
        {
            this.compClass = typeof(CompMechanitorControl);
        }
    }
    public class CompMechanitorControl : VehicleComp
    {
        public CompProperties_MechanitorControl Props => base.props as CompProperties_MechanitorControl;

        public override void PostDraw()
        {
            base.PostDraw();
            Pawn overseer = this.Vehicle.GetOverseer();
            if (overseer != null)
            {
                foreach (var pawn in overseer.mechanitor.ControlledPawns)
                {
                    if (pawn.OverseerSubject.Overseer == overseer)
                    {
                        if (pawn is VehiclePawn vehicle)
                        {
                            foreach (var passenger in vehicle.handlers.SelectMany(x => x.handlers.OfType<Pawn>()))
                            {
                                if (passenger == overseer)
                                {
                                    if (passenger.mechanitor.AnySelectedDraftedMechs)
                                    {
                                        GenDraw.DrawRadiusRing(vehicle.Position, 24.9f, Color.white, (IntVec3 c) => passenger.mechanitor.CanCommandTo(c));
                                    }
                                }
                            }
                        }
                    }
                }
            }


        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo mechGizmo in MechanitorUtility.GetMechGizmos(this.Vehicle))
            {
                yield return mechGizmo;
            }
        }
    }

}
