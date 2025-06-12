using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace WeaponSwitch
{
    public class JobDriver_SwitchWeapon : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
			var comp = TargetA.Thing.TryGetComp<CompSwitchWeapon>();
			yield return Toils_General.Wait(comp.curWeaponSwitchOption.ticksToSwitchWeapon)
				.WithProgressBarToilDelay(TargetIndex.A);
			yield return Toils_General.Do(delegate
			{
				comp.SwitchWeapon(comp.curWeaponSwitchOption);
			});
        }
    }
}
