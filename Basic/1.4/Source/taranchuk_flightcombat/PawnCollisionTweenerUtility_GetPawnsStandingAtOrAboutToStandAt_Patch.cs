using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(PawnCollisionTweenerUtility), "GetPawnsStandingAtOrAboutToStandAt")]
    public static class PawnCollisionTweenerUtility_GetPawnsStandingAtOrAboutToStandAt_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var codes = codeInstructions.ToList();
            var getPostureInfo = AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.GetPosture));
            var shouldSkipInfo = AccessTools.Method(typeof(PawnCollisionTweenerUtility_GetPawnsStandingAtOrAboutToStandAt_Patch), "ShouldSkip");
            for (var i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                yield return code;
                if (code.opcode == OpCodes.Brtrue && codes[i - 1].Calls(getPostureInfo))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return new CodeInstruction(OpCodes.Call, shouldSkipInfo);
                    yield return new CodeInstruction(OpCodes.Brtrue_S, code.operand);
                }
            }
        }

        public static bool ShouldSkip(Pawn pawn)
        {
            if (pawn is VehiclePawn vehicle)
            {
                var comp = vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
