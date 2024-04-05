using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace taranchuk_mobilecrypto
{
    public class JobDriver_CapturePawn : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_General.WaitWith(TargetIndex.A, 180, true);
            yield return Toils_General.Do(delegate
            {
                var comp = TargetB.Thing.TryGetComp<CompMobileCrypto>();
                comp.StorePawn(TargetA.Pawn);
            });
        }
    }
}
