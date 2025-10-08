using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(Pawn), "IsColonyMechPlayerControlled", MethodType.Getter)]
    public static class Pawn_IsColonyMechPlayerControlled_Patch
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (!__result && __instance is VehiclePawn vehicle && __instance.Faction == Faction.OfPlayer)
            {
                var comp = __instance.GetComp<CompMechanitorControl>();
                if (comp != null && __instance.OverseerSubject is null)
                {
                    __result = true;
                }
            }
        }
    }
}
