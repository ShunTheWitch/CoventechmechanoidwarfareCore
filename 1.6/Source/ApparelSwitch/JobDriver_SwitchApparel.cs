using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ApparelSwitch
{
    public class JobDriver_SwitchApparel : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            var comp = TargetA.Thing.TryGetComp<CompSwitchApparel>();
            yield return Toils_General.Wait(comp.curApparelSwitchOption.ticksToSwitchApparel)
                .WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_General.Do(delegate
            {
                comp.SwitchApparel(comp.curApparelSwitchOption);
            });
        }
    }
}
