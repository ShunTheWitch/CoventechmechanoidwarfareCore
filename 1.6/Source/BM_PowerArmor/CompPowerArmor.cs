using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Taranchuk_ColorPicker;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BM_PowerArmor
{
	public class CompProperties_PowerArmor : CompProperties
	{
		public ThingDef apparel;
		public ThingDef building;
		public ThingDef powerArmorWeapon; // Weapon provided by the power armor
		public HediffDef hediffOnEmptyFuel;
		public Gender? genderRestriction;
		public List<ThingDef> requiredApparels;
		public HediffDef requiredHediff;
		public ThingDef upgradedBuilding; // The building this power armor upgrades into
		public ThingDef downgradedBuilding; // The building this power armor downgrades into
		public ResearchProjectDef requiredResearch; // Research required for upgrading
		public List<ThingDefCountClass> upgradeRequirements; // Resources needed for upgrade
		public List<ThingDefCountClass> refundItems; // Items to refund after degrading
		public bool requireParking; // Whether upgrade requires parking spot
		public int requiredConstructionSkill = 0; // Default to 0 if not specified
		public CompProperties_PowerArmor()
		{
			this.compClass = typeof(CompPowerArmor);
		}
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class HotSwappableAttribute : Attribute
	{
	}

	[HotSwappableAttribute]
	[StaticConstructorOnStartup]
	public class CompPowerArmor : ThingComp, IThingHolder
	{
		public CompProperties_PowerArmor Props => props as CompProperties_PowerArmor;
		private static readonly Texture2D CancelCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

		private CompRefuelable _compRefuelable;
		public CompRefuelable CompRefuelable => _compRefuelable ??= parent.GetComp<CompRefuelable>();

		public Rot4 buildingRot;
		public Building powerArmorAsBuilding;
		private int? remainingCharges;
		private ThingWithComps originalWeapon;
		private ThingOwner innerContainer;
		public bool autopark;
		public bool upgradeInProgress;
		public bool downgradeInProgress;
		public int upgradeWorkDone;
		public int downgradeWorkDone;

		public CompPowerArmor()
		{
			innerContainer = new ThingOwner<Thing>(this);
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (innerContainer == null)
			{
				innerContainer = new ThingOwner<Thing>(this);
			}
		}

		public ThingOwner GetDirectlyHeldThings()
		{
			return innerContainer;
		}

		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public override void CompTick()
		{
			base.CompTick();
			if (parent is Apparel apparel)
			{
				if (apparel.Wearer is not null)
				{
					if (Props.hediffOnEmptyFuel != null)
					{
						var comp = parent.GetComp<CompRefuelable>();
						if (comp.HasFuel is false)
						{
							var hediff = apparel.Wearer.health.hediffSet.GetFirstHediffOfDef(Props.hediffOnEmptyFuel);
							if (hediff is null)
							{
								apparel.Wearer.health.AddHediff(Props.hediffOnEmptyFuel);
							}
						}
					}
				}
				else
				{
					parent.Destroy();
				}
			}
		}

		public TaggedString GetGenderOnlyLabel()
		{
			return this.Props.genderRestriction.Value.GetLabel() + " " + "Gender".Translate().ToLower();
		}

		public bool HasRequiredApparels(Pawn pawn)
		{
			return Props.requiredApparels.All(x => pawn.apparel.WornApparel.Any(y => y.def == x));
		}

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			if (selPawn.apparel != null && parent is Building)
			{
				string key = "CannotWear";
				if (Props.genderRestriction.HasValue && selPawn.gender != Props.genderRestriction)
				{
					yield return new FloatMenuOption(key.Translate(parent.Label, parent) + ": " + "BM.ForOnly".Translate(GetGenderOnlyLabel()), null);
				}
				else if (Props.requiredApparels != null && HasRequiredApparels(selPawn) is false)
				{
					var cannotWearReason = "";
					if (Props.requiredApparels.Count == 1)
					{
						cannotWearReason = "BM.Requires".Translate(Props.requiredApparels[0].label);
					}
					else
					{
						cannotWearReason = "BM.Requires".Translate(string.Join(", ", Props.requiredApparels.Select(x => x.label)));
					}
					yield return new FloatMenuOption(key.Translate(parent.Label, parent) + ": " + cannotWearReason, null);
				}
				else if (Props.requiredHediff != null && selPawn.health.hediffSet.GetFirstHediffOfDef(Props.requiredHediff) is null)
				{
					yield return new FloatMenuOption(key.Translate(parent.Label, parent) + ": " + "BM.Requires".Translate(Props.requiredHediff.label), null);
				}
				else
				{
					var comp = parent.GetComp<CompAssignableToPawn_PowerArmor>();
					if (comp != null && comp.AssignedPawns.Any() && comp.AssignedPawns.Contains(selPawn) is false)
					{
						yield return new FloatMenuOption(key.Translate(parent.Label, parent) + ": " + "BM.ForOnly".Translate(comp.AssignedPawns.First().LabelShort), null);
					}
					else
					{
						var pawn = selPawn;
						string key2 = "ForceWear";
						FloatMenuOption option = ((!pawn.CanReach(parent, PathEndMode.ClosestTouch, Danger.Deadly))
							? new FloatMenuOption(key.Translate(parent.Label, parent) + ": " + "NoPath".Translate().CapitalizeFirst(),
							null) : (parent.IsBurning() ? new FloatMenuOption(key.Translate(parent.Label, parent)
							+ ": " + "Burning".Translate(), null) :
							((!ApparelUtility.HasPartsToWear(pawn, Props.apparel))
							? new FloatMenuOption(key.Translate(parent.Label, parent) + ": "
							+ "CannotWearBecauseOfMissingBodyParts".Translate().CapitalizeFirst(), null)
							: FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(key2.Translate(parent.LabelShort, parent),
							delegate
							{
								if (parent.GetComp<CompForbiddable>() is not null)
								{
									parent.SetForbidden(value: false);
								}
								Job job16 = JobMaker.MakeJob(BM_DefOf.BM_WearPowerArmorBuilding, parent);
								pawn.jobs.TryTakeOrderedJob(job16, JobTag.Misc);
							}, MenuOptionPriority.High), pawn, parent))));
						yield return option;
					}
				}
			}
		}

		public void Equip(Pawn wearer, bool playerForced)
		{
			bool selected = Find.Selector.SelectedObjects.Contains(parent);
			var apparel = ThingMaker.MakeThing(Props.apparel) as Apparel;
			SyncData(apparel);
			var compReloadable = apparel.TryGetComp<CompApparelReloadable>();
			if (compReloadable != null && remainingCharges.HasValue)
			{
				compReloadable.remainingCharges = remainingCharges.Value;
			}
			wearer.apparel.Wear(apparel);
			// Store and replace weapon
			if (Props.powerArmorWeapon != null)
			{
				// Store original weapon
				if (wearer.equipment?.Primary != null)
				{
					var weaponToStore = wearer.equipment.Primary;
					wearer.equipment.Remove(weaponToStore);
					if (!wearer.inventory.innerContainer.TryAdd(weaponToStore)) // Use innerContainer.TryAdd
					{
						// If adding fails (e.g., inventory full), drop it near the pawn
						ThingPlaceMode placeMode = ThingPlaceMode.Near;
						Thing resultingThing;
						GenPlace.TryPlaceThing(weaponToStore, wearer.Position, wearer.Map, placeMode, out resultingThing);
					}
					originalWeapon = weaponToStore; // Store reference after attempting to add/drop
				}

				// Equip power armor weapon
				var powerArmorWeapon = ThingMaker.MakeThing(Props.powerArmorWeapon) as ThingWithComps;
				wearer.equipment.MakeRoomFor(powerArmorWeapon);
				wearer.equipment.AddEquipment(powerArmorWeapon);
			}

			if (wearer.outfits != null && playerForced)
			{
				wearer.outfits.forcedHandler.SetForced(apparel, forced: true);
			}
			var position = parent.Position;
			var apparelComp = apparel.GetComp<CompPowerArmor>();
			apparelComp.buildingRot = parent.Rotation;
			apparelComp.powerArmorAsBuilding = parent as Building;
			parent.DeSpawn();
			wearer.teleporting = true;
			wearer.Position = position;
			wearer.Notify_Teleported(false, true);
			if (parent.def.Size.x == 2)
			{
				wearer.Drawer.tweener.ResetTweenedPosToRoot();
				wearer.Drawer.tweener.tweenedPos = wearer.Drawer.tweener.TweenedPosRoot() + new Vector3(0.5f, 0, 0.5f);
			}
			wearer.teleporting = false;
			if (selected)
			{
				Find.Selector.Select(wearer);
			}
		}

		private void SyncData(Thing otherThing)
		{
			otherThing.HitPoints = parent.HitPoints;
			var compRefuelable = otherThing.TryGetComp<CompRefuelable>();
			if (compRefuelable != null)
			{
				var myComp = parent.GetComp<CompRefuelable>();
				compRefuelable.fuel = myComp.fuel;
			}

			var colorComp = otherThing.TryGetComp<CompCustomColorPicker>();
			if (colorComp != null)
			{
				var myComp = this.parent.GetComp<CompCustomColorPicker>();
				if (myComp != null)
				{
					colorComp.colorOne = myComp.colorOne;
					colorComp.colorTwo = myComp.colorTwo;
				}
			}
		}

		public override void Notify_Unequipped(Pawn pawn)
		{
			base.Notify_Unequipped(pawn);
			if (parent is Apparel)
			{
				Unequip(pawn);
			}
		}

		private void Unequip(Pawn pawn)
		{
			if (Props.hediffOnEmptyFuel != null)
			{
				var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffOnEmptyFuel);
				if (hediff != null)
				{
					pawn.health.RemoveHediff(hediff);
				}
			}

			// Remove power armor weapon and restore original weapon
			if (Props.powerArmorWeapon != null && pawn.equipment != null)
			{
				// Remove power armor weapon
				var currentWeapon = pawn.equipment.Primary;
				if (currentWeapon?.def == Props.powerArmorWeapon)
				{
					pawn.equipment.Remove(currentWeapon);
					currentWeapon.Destroy();
				}
			}
			
			var building = powerArmorAsBuilding;
			if (building is null)
			{
				building = powerArmorAsBuilding = ThingMaker.MakeThing(Props.building) as Building;
			}
			var compReloadable = parent.TryGetComp<CompApparelReloadable>();
			var compBuildingPowerArmor = building.GetComp<CompPowerArmor>();

			if (compReloadable != null)
			{
				compBuildingPowerArmor.remainingCharges = compReloadable.remainingCharges;
			}
			// Restore original weapon
			if (compBuildingPowerArmor.originalWeapon != null)
			{
				// Remove from inventory first if it exists there
				if (pawn.inventory.Contains(compBuildingPowerArmor.originalWeapon))
				{
					pawn.inventory.innerContainer.Remove(compBuildingPowerArmor.originalWeapon); // Use innerContainer.Remove
				}
				pawn.equipment.AddEquipment(compBuildingPowerArmor.originalWeapon);
				compBuildingPowerArmor.originalWeapon = null;
			}
			SyncData(building);
			building.SetFaction(pawn.Faction);
			GenPlace.TryPlaceThing(building, pawn.Position, pawn.Map, ThingPlaceMode.Near, rot: buildingRot);
		}

		public bool CanUpgradeNow(out string reason)
		{
			reason = null;
			if (Props.upgradedBuilding == null)
			{
				return false;
			}

			if (Props.requiredResearch != null && Props.requiredResearch.IsFinished is false)
			{
				reason = "BM.RequiresResearch".Translate(Props.requiredResearch.label);
				return false;
			}

			if (Props.requireParking && parent.Position.GetFirstThingWithComp<CompAssignableToPawn_PowerArmorSpot>(parent.Map) is null)
			{
				reason = "BM.RequiresParking".Translate();
				return false;
			}
			return true;
		}

		public bool CanDowngradeNow(out string reason)
		{
			reason = null;
			if (Props.downgradedBuilding == null)
			{
				return false;
			}
			return true;
		}

		public void StartProcess(bool isUpgrade)
		{
			if (isUpgrade)
			{
				upgradeInProgress = true;
				upgradeWorkDone = 0;
			}
			else
			{
				downgradeInProgress = true;
				downgradeWorkDone = 0;
			}
		}

		public void CancelProcess(bool isUpgrade)
		{
			if (isUpgrade)
			{
				upgradeInProgress = false;
				upgradeWorkDone = 0;
				// Return all items in the container
				var items = innerContainer.ToList();
				foreach (var item in items)
				{
					if (!GenPlace.TryPlaceThing(item, parent.Position, parent.Map, ThingPlaceMode.Near))
					{
						Log.Error($"[PowerArmor] Failed to return item {item} when canceling upgrade");
					}
				}
				innerContainer.Clear();
			}
			else
			{
				downgradeInProgress = false;
				downgradeWorkDone = 0;
			}
		}

		public void FinishProcess(bool isUpgrade)
		{
			if (isUpgrade)
			{
				// Complete the upgrade
				upgradeInProgress = false;

				// Destroy all items in container as they've been consumed
				innerContainer.ClearAndDestroyContents();
				var pos = parent.Position;
				var map = parent.Map;

				parent.Destroy();

				// Spawn the upgraded building
				var upgraded = ThingMaker.MakeThing(Props.upgradedBuilding) as Building;
				upgraded.SetFaction(parent.Faction);
				GenSpawn.Spawn(upgraded, pos, map, parent.Rotation);
			}
			else
			{
				// Complete the downgrade
				downgradeInProgress = false;

				// Spawn refund items if any
				if (Props.refundItems != null)
				{
					foreach (var refund in Props.refundItems)
					{
						Thing thing = ThingMaker.MakeThing(refund.thingDef);
						thing.stackCount = refund.count;
						GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near);
					}
				}

				var pos = parent.Position;
				var map = parent.Map;
				// Destroy the original building
				parent.Destroy();

				if (Props.downgradedBuilding != null)
				{
					// Spawn the downgraded building
					var downgraded = ThingMaker.MakeThing(Props.downgradedBuilding) as Building;
					downgraded.SetFaction(parent.Faction);
					GenSpawn.Spawn(downgraded, pos, map, parent.Rotation);
				}
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (var gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}

			if (parent is Building)
			{
				if (upgradeInProgress)
				{
					Command_Action cancelUpgradeCommand = new Command_Action
					{
						defaultLabel = "BM.CancelUpgrade".Translate(),
						defaultDesc = "BM.CancelUpgradeDesc".Translate(),
						icon = CancelCommandTex,
						action = delegate
						{
							CancelProcess(true);
						}
					};
					yield return cancelUpgradeCommand;
				}
				else if (downgradeInProgress)
				{
					Command_Action cancelDowngradeCommand = new Command_Action
					{
						defaultLabel = "BM.CancelDowngrade".Translate(),
						defaultDesc = "BM.CancelDowngradeDesc".Translate(),
						icon = CancelCommandTex,
						action = delegate
						{
							CancelProcess(false);
						}
					};
					yield return cancelDowngradeCommand;
				}
				else
				{
					if (Props.upgradedBuilding != null)
					{
						bool canUpgrade = CanUpgradeNow(out var failReason);
						Command_Action upgradeCommand = new Command_Action
						{
							defaultLabel = "BM.UpgradeTo".Translate(Props.upgradedBuilding.label),
							defaultDesc = "BM.UpgradeDesc".Translate(Props.upgradedBuilding.label),
							icon = Props.upgradedBuilding.uiIcon,
							action = delegate
							{
								if (canUpgrade)
								{
									Find.WindowStack.Add(new Dialog_MessageBox(
										"BM.UpgradeConfirmation".Translate(
											parent.Label,
											Props.upgradedBuilding.label,
											string.Join("\n", Props.upgradeRequirements.Select(r => "- " + r.count + "x " + r.thingDef.label))
										),
										"Confirm".Translate(),
										() =>
										{
											StartProcess(true);
										},
										"Cancel".Translate(),
										null,
										null,
										true
									));
								}
							}
						};

						if (!canUpgrade)
						{
							upgradeCommand.Disable(failReason);
						}

						yield return upgradeCommand;
					}

					if (Props.downgradedBuilding != null)
					{
						bool canDowngrade = CanDowngradeNow(out var failReason);
						Command_Action downgradeCommand = new Command_Action
						{
							defaultLabel = "BM.DowngradeTo".Translate(Props.downgradedBuilding.label),
							defaultDesc = "BM.DowngradeDesc".Translate(Props.downgradedBuilding.label),
							icon = Props.downgradedBuilding.uiIcon,
							action = delegate
							{
								if (canDowngrade)
								{
									Find.WindowStack.Add(new Dialog_MessageBox(
										"BM.DowngradeConfirmation".Translate(
											parent.Label,
											Props.downgradedBuilding.label,
											string.Join("\n", Props.refundItems.Select(r => "- " + r.count + "x " + r.thingDef.label))
										),
										"Confirm".Translate(),
										() =>
										{
											StartProcess(false);
										},
										"Cancel".Translate(),
										null,
										null,
										true
									));
								}
							}
						};

						if (!canDowngrade)
						{
							downgradeCommand.Disable(failReason);
						}

						yield return downgradeCommand;
					}
				}
			}
		}

		public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
		{
			if (parent is Apparel apparel)
			{
				var comp = parent.GetComp<CompRefuelable>();
				if (comp.Props.showFuelGizmo && Find.Selector.SingleSelectedThing == apparel.Wearer)
				{
					Gizmo_RefuelableFuelStatus gizmo_RefuelableFuelStatus = new Gizmo_RefuelableFuelStatus();
					gizmo_RefuelableFuelStatus.refuelable = comp;
					yield return gizmo_RefuelableFuelStatus;
				}
				foreach (var g in comp.CompGetGizmosExtra())
				{
					yield return g;
				}

				yield return new Command_Toggle
				{
					defaultLabel = "BM.ParkPowerArmor".Translate(),
					defaultDesc = "BM.ParkPowerArmorDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Buttons/ParkPowerArmor"),
					isActive = () => autopark,
					toggleAction = () => { autopark = !autopark; },
				};
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref buildingRot, "buildingRot");
			Scribe_References.Look(ref powerArmorAsBuilding, "powerArmorAsBuilding");
			Scribe_Values.Look(ref remainingCharges, "remainingCharges");
			Scribe_References.Look(ref originalWeapon, "originalWeapon");
			Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
			Scribe_Values.Look(ref upgradeInProgress, "upgradeInProgress");
			Scribe_Values.Look(ref downgradeInProgress, "downgradeInProgress");
			Scribe_Values.Look(ref upgradeWorkDone, "upgradeWorkDone");
			Scribe_Values.Look(ref downgradeWorkDone, "downgradeWorkDone");
			Scribe_Values.Look(ref autopark, "autopark");
		}
	}
}
