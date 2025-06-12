using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace BM_PowerArmor
{
    [HotSwappable]
    public class JobDriver_WearPowerArmorBuilding : JobDriver
    {
        private int duration;

        private int unequipBuffer;

        private Building PowerArmorBuilding => (Building)job.GetTarget(TargetIndex.A).Thing;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref duration, "duration", 0);
            Scribe_Values.Look(ref unequipBuffer, "unequipBuffer", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(PowerArmorBuilding, job, 1, -1, null, errorOnFailed);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            var comp = PowerArmorBuilding.GetComp<CompPowerArmor>();
            duration = (int)(PowerArmorBuilding.GetStatValue(StatDefOf.EquipDelay) * 60f);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int num = wornApparel.Count - 1; num >= 0; num--)
            {
                if (!ApparelUtility.CanWearTogether(comp.Props.apparel, wornApparel[num].def, pawn.RaceProps.body))
                {
                    duration += (int)(wornApparel[num].GetStatValue(StatDefOf.EquipDelay) * 60f);
                }
            }
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnBurningImmobile(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(TargetIndex.A);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(TargetA);
                unequipBuffer++;
                TryUnequipSomething();
            };
            toil.handlingFacing = true;
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = duration;
            yield return toil;
            yield return Toils_General.Do(delegate
            {
                var comp = PowerArmorBuilding.GetComp<CompPowerArmor>();
                comp.Equip(pawn, job.playerForced);
            });
        }

        private void TryUnequipSomething()
        {
            var comp = PowerArmorBuilding.GetComp<CompPowerArmor>();
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int num = wornApparel.Count - 1; num >= 0; num--)
            {
                if (!ApparelUtility.CanWearTogether(comp.Props.apparel, wornApparel[num].def, pawn.RaceProps.body))
                {
                    int num2 = (int)(wornApparel[num].GetStatValue(StatDefOf.EquipDelay) * 60f);
                    if (unequipBuffer >= num2)
                    {
                        bool forbid = pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);
                        if (!pawn.apparel.TryDrop(wornApparel[num], out var _, pawn.PositionHeld, forbid))
                        {
                            Log.Error(string.Concat(pawn, " could not drop ", wornApparel[num].ToStringSafe()));
                            EndJobWith(JobCondition.Errored);
                        }
                    }
                    break;
                }
            }
        }
    }
}
