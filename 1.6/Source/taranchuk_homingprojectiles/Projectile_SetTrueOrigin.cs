using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace taranchuk_homingprojectiles
{
    [HarmonyPatch]
    public static class Projectile_SetTrueOrigin
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Projectile), "CheckForFreeInterceptBetween");
            yield return AccessTools.Method(typeof(Projectile), "CheckForFreeIntercept");
            yield return AccessTools.Method(typeof(Projectile), "ImpactSomething");
        }

        public static MethodInfo InterceptChanceFactorFromDistanceInfo = typeof(Verse.VerbUtility).Method("InterceptChanceFactorFromDistance");
        public static FieldInfo Projectile_origin = typeof(Projectile).Field("origin");

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var codes = codeInstructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                var codeInstruction = codes[i];

                bool shouldPatch = codeInstruction.LoadsField(Projectile_origin) &&
                                  codes.Skip(i + 1).Any(c => c.Calls(InterceptChanceFactorFromDistanceInfo));
                yield return codeInstruction;
                if (shouldPatch)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(Projectile_SetTrueOrigin), nameof(GetTrueOrigin)));
                }
            }
        }

        public static Vector3 GetTrueOrigin(Vector3 origin, Projectile projectile)
        {
            if (projectile.IsHomingProjectile(out var comp))
            {
                return comp.originLaunchCell;
            }
            return origin;
        }
    }
}
