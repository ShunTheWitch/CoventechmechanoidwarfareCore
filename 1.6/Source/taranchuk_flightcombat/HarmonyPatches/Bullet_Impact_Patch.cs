using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(Bullet), nameof(Bullet.Impact))]
    public static class Bullet_Impact_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            bool found = false;
            var takeDamage = AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage));
            var applyEffects = AccessTools.Method(typeof(Bullet_Impact_Patch), nameof(ApplyEffects));
            var codes = instructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                if (!found && codes[i].Calls(takeDamage))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 5);
                    yield return new CodeInstruction(OpCodes.Call, applyEffects);
                }
                yield return codes[i];
            }
        }

        public static void ApplyEffects(Projectile projectile, Thing hitThing, DamageInfo damageInfo)
        {
            if (hitThing != null)
            {
                var comp = hitThing.TryGetComp<CompFlightMode>();
                if (comp != null && comp.InAir && comp.Props.damageMultiplierFromNonAntiAirProjectiles.HasValue)
                {
                    var extension = projectile.def.GetModExtension<ProjectileModes>();
                    if (extension is null || extension.antiAir is false)
                    {
                        damageInfo.SetAmount(damageInfo.Amount * comp.Props.damageMultiplierFromNonAntiAirProjectiles.Value);
                    }
                }
            }
        }
    }
}
