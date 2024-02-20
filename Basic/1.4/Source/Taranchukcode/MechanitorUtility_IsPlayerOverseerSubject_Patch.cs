using HarmonyLib;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(MechanitorUtility), "IsPlayerOverseerSubject")]
    public static class MechanitorUtility_IsPlayerOverseerSubject_Patch
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (__result && pawn is VehiclePawn vehicle)
            {
                if (Pawn_ThreatDisabled_Patch.checkingNow || GenHostility_HostileTo_Patch.checkingNow)
                {
                    __result = false;
                }
            }
        }
    }
}
