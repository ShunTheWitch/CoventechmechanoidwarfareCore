using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BuildingEquipment
{
    public class CompProperties_BuildingEquipment : CompProperties
    {
        public ThingDef weapon;
        public ThingDef building;
        public List<ThingDef> requiredApparels;
        public int equipmentDurationTicks;
        public EffecterDef equippingEffecter;
        public EffecterDef equipFinished;
        public SoundDef equipFinishedSound;

        public CompProperties_BuildingEquipment()
        {
            this.compClass = typeof(CompBuildingEquipment);
        }
    }

    [DefOf]
    public static class BM_DefOf
    {
        public static JobDef BM_EquipWeaponBuilding;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [HotSwappableAttribute]

    public class CompBuildingEquipment : ThingComp
    {
        public CompProperties_BuildingEquipment Props => base.props as CompProperties_BuildingEquipment;

        public Rot4 buildingRot;

        public Building weaponAsBuilding;

        public bool HasRequiredApparels(Pawn pawn)
        {
            return Props.requiredApparels.All(x => pawn.apparel.WornApparel.Any(y => y.def == x));
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (selPawn.apparel != null && parent is Building)
            {
                string key = "CannotEquip";
                if (Props.requiredApparels != null && HasRequiredApparels(selPawn) is false)
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
                else
                {
                    var pawn = selPawn;
                    string key2 = "Equip";
                    FloatMenuOption option = ((!pawn.CanReach(parent, PathEndMode.ClosestTouch, Danger.Deadly))
                        ? new FloatMenuOption(key.Translate(parent.Label, parent) + ": " + "NoPath".Translate().CapitalizeFirst(),
                        null) : (parent.IsBurning() ? new FloatMenuOption(key.Translate(parent.Label, parent)
                        + ": " + "Burning".Translate(), null) 
                        : FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(key2.Translate(parent.LabelShort, parent),
                        delegate
                        {
                            if (parent.GetComp<CompForbiddable>() is not null)
                            {
                                parent.SetForbidden(value: false);
                            }
                            Job job16 = JobMaker.MakeJob(BM_DefOf.BM_EquipWeaponBuilding, parent);
                            pawn.jobs.TryTakeOrderedJob(job16, JobTag.Misc);
                        }, MenuOptionPriority.High), pawn, parent)));
                    yield return option;
                }
            }
        }

        public void Equip(Pawn pawn)
        {
            var weapon = ThingMaker.MakeThing(Props.weapon) as ThingWithComps;
            pawn.equipment.MakeRoomFor(weapon);
            pawn.equipment.AddEquipment(weapon);
            SyncData(weapon);
            var comp = weapon.GetComp<CompBuildingEquipment>();
            if (comp != null)
            {
                comp.buildingRot = parent.Rotation;
                comp.weaponAsBuilding = parent as Building;
            }
            parent.DeSpawn();
            Props.equipFinished.Spawn(pawn, pawn);
            Props.equipFinishedSound.PlayOneShot(pawn);
        }

        private void SyncData(Thing otherThing)
        {
            otherThing.HitPoints = parent.HitPoints;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent is not Building && parent.Spawned)
            {
                parent.Destroy();
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (parent is not Building)
            {
                Unequip(pawn);
            }
        }

        private void Unequip(Pawn pawn)
        {
            var building = weaponAsBuilding;
            if (building is null)
            {
                building = weaponAsBuilding = ThingMaker.MakeThing(Props.building) as Building;
            }
            SyncData(building);
            building.SetFaction(pawn.Faction);
            GenPlace.TryPlaceThing(building, pawn.Position, pawn.Map, ThingPlaceMode.Near, rot: buildingRot);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref buildingRot, "buildingRot");
            Scribe_Deep.Look(ref weaponAsBuilding, "weaponAsBuilding");
        }
    }
}
