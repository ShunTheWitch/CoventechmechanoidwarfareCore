using HarmonyLib;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(Pawn), "ThreatDisabled")]
    public static class Pawn_ThreatDisabled_Patch
    {
        public static bool checkingNow;
        public static void Prefix() => checkingNow = true;
        public static void Postfix() => checkingNow = false;
    }
}
