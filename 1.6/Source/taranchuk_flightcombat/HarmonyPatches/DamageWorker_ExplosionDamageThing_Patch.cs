using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(DamageWorker), nameof(DamageWorker.ExplosionDamageThing))]
    public static class DamageWorker_ExplosionDamageThing_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            bool found = false;
            var takeDamage = AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage));
            var applyEffects = AccessTools.Method(typeof(DamageWorker_ExplosionDamageThing_Patch), nameof(ApplyEffects));
            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                if (!found && codes[i + 1].Calls(takeDamage))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldloc, 1);
                    yield return new CodeInstruction(OpCodes.Call, applyEffects);
                }
                yield return codes[i];
            }
        }

        public static void ApplyEffects(Explosion explosion, Thing hitThing, DamageInfo damageInfo)
        {
            if (hitThing != null)
            {
                var comp = hitThing.TryGetComp<CompFlightMode>();
                if (comp != null && comp.InAir && comp.Props.damageMultiplierFromNonAntiAirProjectiles.HasValue)
                {
                    var extension = explosion.projectile?.GetModExtension<ProjectileModes>();
                    if (extension is null || extension.antiAir is false)
                    {
                        damageInfo.SetAmount(damageInfo.Amount * comp.Props.damageMultiplierFromNonAntiAirProjectiles.Value);
                    }
                }
            }
        }
    }
}
