using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace taranchuk_damageTiers
{
    public class taranchuk_damageTiersMod : Mod
    {
        public taranchuk_damageTiersMod(ModContentPack pack) : base(pack)
        {
            new Harmony("taranchuk_damageTiersMod").PatchAll();
        }
    }

    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Thing_TakeDamage_Patch
    {
        public static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            var extension = dinfo.defInt?.GetModExtension<DamageExtension>();
            if (extension != null)
            {
                if (extension.damagesByMass != null)
                {
                    var mass = __instance.GetMass();
                    if (mass >= extension.damagesByMass[0].minimumMass)
                    {
                        var damageByMassChosen = extension.damagesByMass[0];
                        foreach (var damageByMass in extension.damagesByMass)
                        {
                            if (mass >= damageByMass.minimumMass)
                            {
                                damageByMassChosen = damageByMass;
                            }
                        }
                        dinfo.defInt = damageByMassChosen.damageDef;
                        dinfo.amountInt = damageByMassChosen.damageAmount;
                    }
                }
            }
        }

        public static float GetMass(this Thing thing)
        {
            try
            {
                if (thing is Vehicles.VehiclePawn vehicle)
                {
                    return vehicle.GetStatValue(Vehicles.VehicleStatDefOf.Mass);
                }
            }
            catch { }
            return thing.GetStatValue(StatDefOf.Mass);
        }
    }

    public class DamageByMass
    {
        public float minimumMass;
        public float damageAmount;
        public DamageDef damageDef;
    }

    public class DamageExtension : DefModExtension
    {
        public List<DamageByMass> damagesByMass;
    }
}
