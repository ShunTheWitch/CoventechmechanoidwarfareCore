using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ApparelSwitch
{
	public class ApparelSwitchOption : IExposable
	{
		public string label;
		public ThingDef apparel;
		public int ticksToSwitchApparel;
		public void ExposeData()
		{
			Scribe_Defs.Look(ref apparel, "apparel");
			Scribe_Values.Look(ref ticksToSwitchApparel, "ticksToSwitchApparel");
		}
	}

	public class CompProperties_SwitchApparel : CompProperties
	{
		public List<ApparelSwitchOption> apparelToSwitch;

		public CompProperties_SwitchApparel()
		{
			compClass = typeof(CompSwitchApparel);
		}
	}

	public class CompSwitchApparel : ThingComp
	{

		public Dictionary<ThingDef, Thing> generatedApparel;

		private List<ThingDef> thingDefs;

		private List<Thing> things;

		public ApparelSwitchOption curApparelSwitchOption;

		public CompProperties_SwitchApparel Props => props as CompProperties_SwitchApparel;

		public Pawn Pawn
		{
			get
			{
				var apparel = parent as Apparel;
				return apparel.Wearer;
			}
		}

		public IEnumerable<FloatMenuOption> GetFloatMenuOptions()
		{
			foreach (var apparelSwitchOption in Props.apparelToSwitch)
			{
				yield return new FloatMenuOption(apparelSwitchOption.label ?? apparelSwitchOption.apparel.LabelCap, delegate
				{
					if (apparelSwitchOption.ticksToSwitchApparel > 0)
					{
						curApparelSwitchOption = apparelSwitchOption;
						Pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(ApparelSwitchMod.AS_SwitchApparel, this.parent));
					}
					else
					{
						SwitchApparel(apparelSwitchOption);
					}
				});
			}
		}

		public void SwitchApparel(ApparelSwitchOption apparelSwitchOption)
		{
			var apparelDef = apparelSwitchOption.apparel;
			Pawn pawn = Pawn;
			if (generatedApparel == null)
			{
				generatedApparel = new Dictionary<ThingDef, Thing>();
			}
			if (!generatedApparel.TryGetValue(apparelDef, out var newApparel))
			{
				newApparel = ThingMaker.MakeThing(apparelDef, apparelDef.MadeFromStuff ? parent.Stuff ?? GenStuff.DefaultStuffFor(apparelDef) : null);
				generatedApparel[apparelDef] = newApparel;
			}
			newApparel.HitPoints = parent.HitPoints;
			if (parent.TryGetQuality(out var qc))
			{
				var qualityComp = newApparel.TryGetComp<CompQuality>();
				if (qualityComp != null)
				{
					qualityComp.qualityInt = qc;
				}
			}
			var parentColorable = parent.TryGetComp<CompColorable>();
			if (parentColorable != null)
			{
				var newColorable = newApparel.TryGetComp<CompColorable>();
				if (newColorable != null)
				{
					newColorable.SetColor(parentColorable.Color);
				}
			}
			generatedApparel[parent.def] = parent;
			newApparel.TryGetComp<CompSwitchApparel>().generatedApparel = generatedApparel;
			for (int num = pawn.apparel.WornApparel.Count - 1; num >= 0; num--)
			{
				Apparel apparel = pawn.apparel.WornApparel[num];
				if (apparel == parent)
				{
					continue;
				}
				if (!ApparelUtility.CanWearTogether(newApparel.def, apparel.def, pawn.RaceProps.body))
				{
					bool forbid = pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer);
					if (!pawn.apparel.TryDrop(apparel, out var _, pawn.PositionHeld, forbid))
					{
						Log.Error(pawn?.ToString() + " could not drop " + apparel?.ToString());
						return;
					}
				}
			}
			pawn.apparel.Remove(parent as Apparel);
			pawn.apparel.Wear(newApparel as Apparel);
			newApparel.Notify_ColorChanged();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Deep.Look(ref curApparelSwitchOption, "curApparelSwitchOption");
			generatedApparel?.Remove(this.parent.def);
			Scribe_Collections.Look<ThingDef, Thing>(ref generatedApparel, "generatedApparel", LookMode.Def, LookMode.Deep, ref thingDefs, ref things);
		}
	}
}
