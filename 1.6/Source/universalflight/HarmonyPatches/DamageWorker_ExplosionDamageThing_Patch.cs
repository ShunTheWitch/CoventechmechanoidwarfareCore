using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace universalflight
{
    [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageThing")]
    public static class DamageWorker_ExplosionDamageThing_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            bool found = false;
            var applyEffects = AccessTools.Method(typeof(DamageWorker_ExplosionDamageThing_Patch), nameof(ApplyEffects));
            var codes = instructions.ToList();

            for (var i = 0; i < codes.Count; i++)
            {
                if (!found && i + 1 < codes.Count &&
                    (codes[i + 1].opcode == OpCodes.Callvirt || codes[i + 1].opcode == OpCodes.Call))
                {
                    var method = codes[i + 1].operand as System.Reflection.MethodInfo;
                    if (method != null &&
                        method.ReturnType.Name == "DamageResult" &&
                        method.GetParameters().Length > 0 &&
                        method.GetParameters()[0].ParameterType.Name == "DamageInfo")
                    {
                        found = true;
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_2);
                        yield return new CodeInstruction(OpCodes.Ldloca_S, 1);
                        yield return new CodeInstruction(OpCodes.Call, applyEffects);
                    }
                }
                yield return codes[i];
            }

            if (!found)
            {
                Log.Error("UniversalFlight: Failed to find TakeDamage call in ExplosionDamageThing!");
            }
        }

        public static void ApplyEffects(Explosion explosion, Thing hitThing, ref DamageInfo damageInfo)
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