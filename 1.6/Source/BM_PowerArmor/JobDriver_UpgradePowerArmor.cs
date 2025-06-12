using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace BM_PowerArmor
{
	[HotSwappable]
	public class JobDriver_UpgradePowerArmor : JobDriver
	{
		private const TargetIndex BuildingInd = TargetIndex.A;
		private const int WorkTimeTicks = 3000; // 50 seconds of work

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		public override IEnumerable<Toil> MakeNewToils()
		{
			var building = job.targetA.Thing as Building;
			var comp = building.GetComp<CompPowerArmor>();
			bool isUpgrade = job.def == BM_DefOf.UpgradePowerArmor;

			// Fail if can't proceed
			this.FailOnDespawnedNullOrForbidden(BuildingInd);
			this.FailOn(() => isUpgrade ? !comp.upgradeInProgress : !comp.downgradeInProgress);
			if (isUpgrade)
			{
				// Collect each required resource using predefined toils
				foreach (Toil item in CollectIngredientsToils(TargetIndex.B, TargetIndex.A, subtractNumTakenFromJobCount: true, 
				failIfStackCountLessThanJobCount: false))
				{
					yield return item;
				}
			}

			// Go to the power armor first
			yield return Toils_Goto.GotoThing(BuildingInd, PathEndMode.Touch);

			// Do the work
			var workToil = new Toil
			{
				initAction = () =>
				{
					comp.StartProcess(isUpgrade);
				},
				tickAction = () =>
				{
					if ((isUpgrade && comp.upgradeWorkDone >= WorkTimeTicks) || (!isUpgrade && comp.downgradeWorkDone >= WorkTimeTicks))
					{
						comp.FinishProcess(isUpgrade);
						ReadyForNextToil();
					}
					if (isUpgrade)
						comp.upgradeWorkDone++;
					else
						comp.downgradeWorkDone++;
				},
				defaultCompleteMode = ToilCompleteMode.Never
			};
			workToil.WithProgressBar(BuildingInd, () => isUpgrade ? (float)comp.upgradeWorkDone / WorkTimeTicks : (float)comp.downgradeWorkDone / WorkTimeTicks);
			workToil.FailOnDespawnedNullOrForbidden(BuildingInd);
			yield return workToil;
		}

		public static IEnumerable<Toil> CollectIngredientsToils(TargetIndex ingredientInd, TargetIndex billGiverInd, 
		bool subtractNumTakenFromJobCount = false, bool failIfStackCountLessThanJobCount = true)
		{
			Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(ingredientInd);
			yield return extract;
			Toil jumpIfHaveTargetInQueue = Toils_Jump.JumpIfHaveTargetInQueue(ingredientInd, extract);
			yield return JobDriver_DoBill.JumpIfTargetInsideBillGiver(jumpIfHaveTargetInQueue, ingredientInd, billGiverInd);
			Toil getToHaulTarget = Toils_Goto.GotoThing(ingredientInd, PathEndMode.ClosestTouch)
				.FailOnDespawnedNullOrForbidden(ingredientInd)
				.FailOnSomeonePhysicallyInteracting(ingredientInd);
			yield return getToHaulTarget;
			yield return Toils_Haul.StartCarryThing(ingredientInd, putRemainderInQueue: true, subtractNumTakenFromJobCount, failIfStackCountLessThanJobCount);
			yield return JobDriver_DoBill.JumpToCollectNextIntoHandsForBill(getToHaulTarget, ingredientInd);
			yield return Toils_Goto.GotoThing(billGiverInd, PathEndMode.Touch)
				.FailOnDestroyedOrNull(ingredientInd);

			// Custom toil to store items in the CompPowerArmor's container
			Toil storeInContainer = new Toil
			{
				initAction = () =>
				{
					var actor = extract.actor;
					var building = actor.CurJob.GetTarget(billGiverInd).Thing as Building;
					var comp = building.GetComp<CompPowerArmor>();
					Thing carriedThing = actor.carryTracker.CarriedThing;
					if (actor.carryTracker.innerContainer.TryTransferToContainer(carriedThing, comp.GetDirectlyHeldThings()) is false)
					{
						Log.Message("[PowerArmor] Failed to store item in container: " + actor.carryTracker.CarriedThing);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return storeInContainer;
			yield return jumpIfHaveTargetInQueue;
		}
	}
}
