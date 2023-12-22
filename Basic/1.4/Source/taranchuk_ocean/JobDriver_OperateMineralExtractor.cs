using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace taranchuk_ocean
{
    public class JobDriver_OperateMineralExtractor : JobDriver
    {
        private CompMineralExtractor CompMineralExtractor => job.targetA.Thing.TryGetComp<CompMineralExtractor>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            CompMineralExtractor.RegisterWorker(pawn);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
            this.FailOn(() => !CompMineralExtractor.CanOperate);
            yield return Toils_Goto.GotoCell(CompMineralExtractor.Workers[pawn], PathEndMode.OnCell);
            Toil work = ToilMaker.MakeToil("MakeNewToils");
            work.tickAction = delegate
            {
                Pawn actor = work.actor;
                ((Building)actor.CurJob.targetA.Thing).GetComp<CompMineralExtractor>().DrillWorkDone(actor);
            };
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return work;
            this.AddFinishAction(delegate
            {
                Log.Message("Finished");
                CompMineralExtractor.Workers.Remove(pawn);
            });
        }
    }
}
