using RimWorld;
using Verse;

namespace taranchuk_vehicleabilities
{
    public class CompProperties_Ripscan : CompProperties_AbilityEffect
    {
        public ThingDef subcoreScannerOutputDef;
        public CompProperties_Ripscan()
        {
            compClass = typeof(CompRipscanAbilityEffect);
        }
    }

    public class CompRipscanAbilityEffect : CompAbilityEffect
    {
        public CompProperties_Ripscan Props => base.props as CompProperties_Ripscan;

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            return base.CanApplyOn(target, dest) && target.Pawn.Downed && target.Pawn.RaceProps.Humanlike;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            GenPlace.TryPlaceThing(ThingMaker.MakeThing(Props.subcoreScannerOutputDef), targetPawn.Position, targetPawn.Map, ThingPlaceMode.Near);
            DamageInfo dinfo = new DamageInfo(DamageDefOf.ExecutionCut, 9999f, 999f, -1f, null, targetPawn.health.hediffSet.GetBrain());
            dinfo.SetIgnoreInstantKillProtection(ignore: true);
            dinfo.SetAllowDamagePropagation(val: false);
            targetPawn.forceNoDeathNotification = true;
            targetPawn.TakeDamage(dinfo);
            targetPawn.forceNoDeathNotification = false;
            ThoughtUtility.GiveThoughtsForPawnExecuted(targetPawn, null, PawnExecutionKind.Ripscanned);
            Messages.Message("MessagePawnKilledRipscanner".Translate(targetPawn.Named("PAWN")), targetPawn, MessageTypeDefOf.NegativeHealthEvent);
        }
    }
}
