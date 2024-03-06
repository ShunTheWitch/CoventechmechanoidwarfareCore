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
            yield return Toils_General.Wait(180).WithProgressBarToilDelay(TargetIndex.B);
            yield return Toils_General.Do(delegate
            {
                var comp = TargetB.Thing.TryGetComp<CompMobileCrypto>();
                comp.ReleasePawn(TargetA.Pawn);
            });
        }
    }
}
