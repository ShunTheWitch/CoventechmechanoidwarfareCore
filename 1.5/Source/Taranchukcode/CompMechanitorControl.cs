using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;

namespace VehicleMechanitorControl
{
    public class CompProperties_MechanitorControl : CompProperties
    {
        public int bandwidthGain;
        public float mechControlRange;
        public bool canBeMechanitor;
        public CompProperties_MechanitorControl()
        {
            this.compClass = typeof(CompMechanitorControl);
        }
    }

    public class CompMechanitorControl : ThingComp
    {
        public CompProperties_MechanitorControl Props => base.props as CompProperties_MechanitorControl;

        public Pawn Pawn => parent as Pawn;

        public override void PostDraw()
        {
            base.PostDraw();
            Pawn overseer = Pawn.GetOverseer();
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

                if (this.Props.mechControlRange > 0 && overseer.mechanitor.AnySelectedDraftedMechs)
                {
                    GenDraw.DrawRadiusRing(parent.Position, this.Props.mechControlRange, Color.white, (IntVec3 c) => 
                    parent.Position.DistanceTo(c) <= this.Props.mechControlRange);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Pawn.RaceProps.IsMechanoid is false)
            {
                if (MechanitorUtility.IsMechanitor(Pawn))
                {
                    foreach (Gizmo gizmo6 in Pawn.mechanitor.GetGizmos())
                    {
                        yield return gizmo6;
                    }
                }
                foreach (Gizmo mechGizmo in MechanitorUtility.GetMechGizmos(Pawn))
                {
                    yield return mechGizmo;
                }
            }
        }

        public void AssignMechanitorControlComps()
        {
            var pawn = parent as Pawn;
            pawn.relations ??= new Pawn_RelationsTracker(pawn);
            pawn.drafter ??= new Pawn_DraftController(pawn);
            if (Props.canBeMechanitor)
            {
                if (MechanitorUtility.IsMechanitor(pawn) && pawn.mechanitor == null)
                {
                    pawn.mechanitor = new Pawn_MechanitorTracker(pawn);
                }
                else if (!MechanitorUtility.IsMechanitor(pawn) && pawn.mechanitor != null)
                {
                    pawn.mechanitor = null;
                }
            }
        }
    }
}
