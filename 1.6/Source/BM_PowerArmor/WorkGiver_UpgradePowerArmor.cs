using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace BM_PowerArmor
{

	[HotSwappable]
	public class WorkGiver_UpgradePowerArmor : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t is not Building building)
			{
				return false;
			}

			var comp = building.GetComp<CompPowerArmor>();
			if (comp == null)
			{
				return false;
			}

			if (IsUpgrade)
			{
				if (comp.Props.upgradedBuilding == null || comp.upgradeInProgress is false)
				{
					return false;
				}
				else if (!comp.CanUpgradeNow(out var reason))
				{
					JobFailReason.Is(reason);
					return false;
				}

				List<ThingCount> chosen = new List<ThingCount>();
				List<IngredientCount> ingredients = comp.Props.upgradeRequirements.Select(req => req.ToIngredientCount()).ToList();

				if (!WorkGiver_DoBill.TryFindBestFixedIngredients(ingredients, pawn, building, chosen))
				{
					var message = "MissingMaterials".Translate(ingredients.Select(kvp => kvp.Summary).ToCommaList());
					JobFailReason.Is(message);
					return false;
				}
			}
			else
			{
				if (comp.Props.downgradedBuilding == null || comp.downgradeInProgress is false)
				{
					return false;
				}
				else if (!comp.CanDowngradeNow(out var reason))
				{
					JobFailReason.Is(reason);
					return false;
				}
			}

			if (pawn.skills.GetSkill(SkillDefOf.Construction).Level < comp.Props.requiredConstructionSkill)
			{
				var message = "BM.RequiresConstructionSkill".Translate(comp.Props.requiredConstructionSkill);
				JobFailReason.Is(message);
				return false;
			}

			return true;
		}

		private bool IsUpgrade => JobDef == BM_DefOf.UpgradePowerArmor;

		protected virtual JobDef JobDef => BM_DefOf.UpgradePowerArmor;

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			var building = t as Building;
			var comp = building.GetComp<CompPowerArmor>();
			if (IsUpgrade)
			{
				var extraIngredients = new List<ThingCount>();
				if (WorkGiver_DoBill.TryFindBestFixedIngredients(comp.Props.upgradeRequirements
				.Select(req => req.ToIngredientCount()).ToList(), pawn, building, extraIngredients))
				{
					var job = JobMaker.MakeJob(JobDef, building);
					if (!extraIngredients.NullOrEmpty())
					{
						job.targetQueueB = new List<LocalTargetInfo>(extraIngredients.Count);
						job.countQueue = new List<int>(extraIngredients.Count);
						foreach (ThingCount extraIngredient in extraIngredients)
						{
							job.targetQueueB.Add(extraIngredient.Thing);
							job.countQueue.Add(extraIngredient.Count);
						}
					}
					job.haulMode = HaulMode.ToCellNonStorage;
					return job;
				}
			}
			else
			{
				var job = JobMaker.MakeJob(JobDef, building);
				return job;
			}
			return null;
		}
	}

	public class WorkGiver_DowngradePowerArmor : WorkGiver_UpgradePowerArmor
	{
		protected override JobDef JobDef => BM_DefOf.DowngradePowerArmor;
	}
}
