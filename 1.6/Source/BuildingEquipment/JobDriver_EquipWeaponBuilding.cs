using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BuildingEquipment
{
    [HotSwappable]
    public class JobDriver_EquipWeaponBuilding : JobDriver
    {
        private int duration;

        private Building PowerArmorBuilding => (Building)job.GetTarget(TargetIndex.A).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref duration, "duration", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(PowerArmorBuilding, job, 1, -1, null, errorOnFailed);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            var comp = PowerArmorBuilding.GetComp<CompBuildingEquipment>();
            duration = comp.Props.equipmentDurationTicks;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            yield return Toils_General.Do(delegate
            {
                pawn.mindState.droppedWeapon = null;
            });
            Toil f = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return f.FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.defaultDuration = duration;
            toil.AddFinishAction(delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
            });
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.handlingFacing = true;
            yield return toil.WithProgressBarToilDelay(TargetIndex.A)
                .WithEffect(Comp.Props.equippingEffecter, TargetIndex.A);
            yield return Toils_General.Do(delegate
            {
                Comp.Equip(pawn);
            });

        }

        private CompBuildingEquipment Comp => PowerArmorBuilding.GetComp<CompBuildingEquipment>();
    }
}
