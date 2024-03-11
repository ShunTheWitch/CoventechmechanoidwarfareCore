using HarmonyLib;
using RimWorld;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(MechanitorUtility), "IsMechanitor")]
    public static class MechanitorUtility_IsMechanitor_Patch
    {
        public static void Postfix(ref bool __result, Pawn pawn)
        {
            if (!__result && pawn.Faction.IsPlayerSafe() && (pawn.GetComp<CompMechanitorControl>()?.Props.canBeMechanitor ?? false))
            {
                __result = true;
            }
        }
    }
}
