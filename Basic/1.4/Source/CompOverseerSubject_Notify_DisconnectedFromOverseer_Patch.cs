using HarmonyLib;
using RimWorld;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(CompOverseerSubject), "Notify_DisconnectedFromOverseer")]
    public static class CompOverseerSubject_Notify_DisconnectedFromOverseer_Patch
    {
        public static void Prefix(CompOverseerSubject __instance)
        {
            if (__instance.Parent.drafter is null && __instance.parent.GetComp<CompMechanitorControl>() != null)
            {
                __instance.Parent.drafter = new Pawn_DraftController(__instance.Parent);
            }
        }
    }
}
