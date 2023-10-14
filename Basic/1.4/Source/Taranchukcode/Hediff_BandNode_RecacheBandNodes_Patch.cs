using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(Hediff_BandNode), "RecacheBandNodes")]
    public static class Hediff_BandNode_RecacheBandNodes_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var field = AccessTools.Field(typeof(Hediff_BandNode), "cachedTunedBandNodesCount");
            bool patched = false;
            foreach (var codeInstruction in codeInstructions)
            {
                yield return codeInstruction;
                if (!patched && codeInstruction.StoresField(field))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldflda, field);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, 
                        AccessTools.Method(typeof(Hediff_BandNode_RecacheBandNodes_Patch), "ModifyCount"));
                    patched = true;
                }
            }
        }

        public static void ModifyCount(ref int count, Hediff_BandNode hediff)
        {
            var pawn = hediff.pawn;
            foreach (var controlledPawn in pawn.mechanitor.ControlledPawns)
            {
                var comp = controlledPawn.GetComp<CompMechanitorControl>();
                if (comp != null && controlledPawn.OverseerSubject.Overseer == pawn)
                {
                    count += comp.Props.bandwidthGain;
                }
            }
        }
    }
}
