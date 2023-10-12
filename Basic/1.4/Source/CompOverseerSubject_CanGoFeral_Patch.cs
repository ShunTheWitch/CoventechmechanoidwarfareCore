using HarmonyLib;
using RimWorld;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(CompOverseerSubject), "CanGoFeral")]
    public static class CompOverseerSubject_CanGoFeral_Patch
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            var comp = pawn.GetComp<CompMechanitorControl>();
            if (comp != null)
            {
                __result = false;
            }
        }
    }
}
