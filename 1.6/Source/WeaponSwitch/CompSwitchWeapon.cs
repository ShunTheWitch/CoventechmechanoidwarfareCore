using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WeaponSwitch
{
	public class WeaponSwitchOption : IExposable
	{
		public string label;
		public string description;
		public string texPath;
		public ThingDef weapon;
		public bool retainAmmoOnTransfer;
		public int ticksToSwitchWeapon;
		public SoundDef sound;

        public void ExposeData()
        {
			Scribe_Defs.Look(ref weapon, "weapon");
            Scribe_Values.Look(ref retainAmmoOnTransfer, "retainAmmoOnTransfer");
            Scribe_Values.Look(ref ticksToSwitchWeapon, "ticksToSwitchWeapon");
        }
    }
    public class CompProperties_SwitchWeapon : CompProperties
    {
        public List<WeaponSwitchOption> weaponsToSwitch;
        public CompProperties_SwitchWeapon()
        {
            compClass = typeof(CompSwitchWeapon);
        }
    }
    public class CompSwitchWeapon : ThingComp
	{
		private CompEquippable compEquippable;

		public Dictionary<ThingDef, Thing> generatedWeapons;

		private List<ThingDef> thingDefs;

		private List<Thing> things;

		public WeaponSwitchOption curWeaponSwitchOption;
        public CompProperties_SwitchWeapon Props => props as CompProperties_SwitchWeapon;
		private CompEquippable CompEquippable
		{
			get
			{
				if (compEquippable == null)
				{
					compEquippable = parent.GetComp<CompEquippable>();
				}
				return compEquippable;
			}
		}

		public Pawn Pawn
		{
			get
			{
				Pawn_EquipmentTracker pawn_EquipmentTracker = CompEquippable.ParentHolder as Pawn_EquipmentTracker;
				if (pawn_EquipmentTracker != null && pawn_EquipmentTracker.pawn != null)
				{
					return pawn_EquipmentTracker.pawn;
				}
				return null;
			}
		}

		public IEnumerable<Gizmo> SwitchWeaponOptions()
		{
			foreach (var weaponSwithOption in Props.weaponsToSwitch)
			{
				yield return new Command_Action
                {
					defaultLabel = weaponSwithOption.label ?? weaponSwithOption.weapon.LabelCap,
					defaultDesc = weaponSwithOption.description ?? weaponSwithOption.weapon.description,
					activateSound = SoundDefOf.Click,
					icon = weaponSwithOption.texPath.NullOrEmpty() is false ?
                    ContentFinder<Texture2D>.Get(weaponSwithOption.texPath) : weaponSwithOption.weapon.uiIcon,
					action = delegate
                    {
						if (weaponSwithOption.ticksToSwitchWeapon > 0)
						{
							curWeaponSwitchOption = weaponSwithOption;
							Pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(WeaponSwitchMod.WS_SwitchWeapon, this.parent));
                        }
						else
						{
                            SwitchWeapon(weaponSwithOption);
                        }
                    }
                };
			}
		}

        public void SwitchWeapon(WeaponSwitchOption weaponSwithOption)
        {
            var weaponDef = weaponSwithOption.weapon;
            Pawn pawn = Pawn;
            if (generatedWeapons == null)
            {
                generatedWeapons = new Dictionary<ThingDef, Thing>();
            }
            if (!generatedWeapons.TryGetValue(weaponDef, out var newWeapon))
            {
                newWeapon = ThingMaker.MakeThing(weaponDef);
                generatedWeapons[weaponDef] = newWeapon;
            }
            newWeapon.HitPoints = parent.HitPoints;
            if (parent.TryGetQuality(out var qc))
            {
                var qualityComp = newWeapon.TryGetComp<CompQuality>();
                if (qualityComp != null)
                {
                    qualityComp.qualityInt = qc;
                }
            }

            if (weaponSwithOption.retainAmmoOnTransfer && WeaponSwitchMod.CEActive)
            {
                CECompat.TransferAmmo(parent, newWeapon);
            }
            generatedWeapons[parent.def] = parent;
            newWeapon.TryGetComp<CompSwitchWeapon>().generatedWeapons = generatedWeapons;
            pawn.equipment.Remove(parent);
            pawn.equipment.AddEquipment(newWeapon as ThingWithComps);
			if (weaponSwithOption.sound != null)
			{
				weaponSwithOption.sound.PlayOneShot(pawn);
			}
        }

        public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Deep.Look(ref curWeaponSwitchOption, "curWeaponSwitchOption");
			generatedWeapons?.Remove(this.parent.def);
            Scribe_Collections.Look<ThingDef, Thing>(ref generatedWeapons, "generatedWeapons", LookMode.Def, LookMode.Deep, ref thingDefs, ref things);
		}
	}
}
