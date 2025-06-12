using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BM_PowerArmor
{
    public class JobDriver_ParkPowerArmor : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetB, job);
        }

        private int duration;

        private Apparel Apparel => (Apparel)job.GetTarget(TargetIndex.A).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref duration, "duration", 0);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.InteractionCell);
            yield return Toils_General.Do(delegate
            {
                pawn.Rotation = Rot4.South;
            });
            yield return Toils_General.Wait(duration).WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(delegate
            {
                if (pawn.apparel.WornApparel.Contains(Apparel))
                {
                    if (pawn.apparel.TryDrop(Apparel, out var resultingAp))
                    {
                        job.targetA = resultingAp;
                        EndJobWith(JobCondition.Succeeded);
                    }
                    else
                    {
                        EndJobWith(JobCondition.Incompletable);
                    }
                }
                else
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            });
        }
    }
}
