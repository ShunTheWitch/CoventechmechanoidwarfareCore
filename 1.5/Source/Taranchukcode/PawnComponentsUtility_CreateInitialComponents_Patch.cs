using HarmonyLib;
using RimWorld;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(PawnComponentsUtility), "CreateInitialComponents")]
    public static class PawnComponentsUtility_CreateInitialComponents_Patch
    {
        public static void Postfix(Pawn pawn)
        {
            var comp = pawn.GetComp<CompMechanitorControl>();
            if (comp != null)
            {
                comp.AssignMechanitorControlComps();
            }
        }
    }

}
