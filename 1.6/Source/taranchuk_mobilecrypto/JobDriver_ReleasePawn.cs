using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace taranchuk_mobilecrypto
{
    public class JobDriver_ReleasePawn : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            if (pawn != TargetPawnC)
            {
                yield return Toils_Goto.Goto(TargetIndex.C, PathEndMode.ClosestTouch);
                yield return Toils_General.WaitWith(TargetIndex.C, 180).WithProgressBarToilDelay(TargetIndex.C);
            }
            else
            {
                yield return Toils_General.Wait(180).WithProgressBarToilDelay(TargetIndex.C);
            }
            yield return Toils_General.Do(delegate
            {
                var comp = TargetB.Thing.TryGetComp<CompMobileCrypto>();
                comp.ReleasePawn(TargetA.Pawn);
            });
        }
    }
}
