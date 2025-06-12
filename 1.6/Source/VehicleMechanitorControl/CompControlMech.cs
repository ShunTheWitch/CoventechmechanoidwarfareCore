using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

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
            return CanControlMech(parent.pawn, mech) && mech.GetOverseer() != parent.pawn;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            var canControl = CanControlMech(parent.pawn, target.Pawn);
            if (canControl == false)
            {
                if (throwMessages)
                {
                    Messages.Message(canControl.Reason, target.Pawn, MessageTypeDefOf.RejectInput);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }

        public static AcceptanceReport CanControlMech(Pawn pawn, Pawn mech)
        {
            if (pawn.mechanitor == null || mech.Dead)
            {
                return false;
            }
            if (!MechanitorUtility.EverControllable(mech))
            {
                return "CannotControlMechNeverControllable".Translate();
            }
            if (mech.GetOverseer() == pawn)
            {
                return "CannotControlMechAlreadyControlled".Translate(pawn.LabelShort);
            }
            int num = pawn.mechanitor.TotalBandwidth - pawn.mechanitor.UsedBandwidth;
            float statValue = mech.GetStatValue(StatDefOf.BandwidthCost);
            if ((float)num < statValue)
            {
                return "CannotControlMechNotEnoughBandwidth".Translate();
            }
            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var targetPawn = target.Pawn;
            targetPawn.relations ??= new Pawn_RelationsTracker(targetPawn);
            targetPawn.drafter ??= new Pawn_DraftController(targetPawn);
            if (targetPawn.Faction != parent.pawn.Faction)
            {
                targetPawn.SetFaction(parent.pawn.Faction, parent.pawn);
            }
            targetPawn.GetOverseer()?.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, targetPawn);
            parent.pawn.relations.AddDirectRelation(PawnRelationDefOf.Overseer, targetPawn);

            var comp = targetPawn.GetComp<CompMechanitorControl>();
            if (comp != null && comp.Props.bandwidthGain > 0)
            {
                if (!parent.pawn.health.hediffSet.HasHediff(CVN_DefOf.BandNode))
                {
                    var brain = parent.pawn.health.hediffSet.GetBrain();
                    var hediff = HediffMaker.MakeHediff(CVN_DefOf.BandNode, parent.pawn, brain);
                    parent.pawn.health.hediffSet.AddDirect(hediff);
                }
                var hediffNode = parent.pawn.health.hediffSet.GetFirstHediffOfDef(CVN_DefOf.BandNode) as Hediff_BandNode;
                hediffNode.RecacheBandNodes();
            }

        }
    }
}
