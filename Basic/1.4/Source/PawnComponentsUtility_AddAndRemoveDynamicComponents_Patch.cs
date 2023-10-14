using HarmonyLib;
using RimWorld;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(PawnComponentsUtility), "AddAndRemoveDynamicComponents")]
    public static class PawnComponentsUtility_AddAndRemoveDynamicComponents_Patch
    {
        public static void Postfix(Pawn pawn)
        {
            var comp = pawn.GetComp<CompMechanitorControl>();
            if (comp != null)
            {
                pawn.relations ??= new Pawn_RelationsTracker(pawn);
            }
        }
    }

}
