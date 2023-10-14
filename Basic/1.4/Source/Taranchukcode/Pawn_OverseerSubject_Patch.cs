using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(Pawn), "OverseerSubject", MethodType.Getter)]
    public static class Pawn_OverseerSubject_Patch
    {
        public static void Postfix(Pawn __instance, ref CompOverseerSubject __result)
        {
            if (__instance.overseerSubject is null && __instance is VehiclePawn vehicle)
            {
                var comp = vehicle.GetComp<CompMechanitorControl>();
                if (comp != null)
                {
                    __instance.overseerSubject = __instance.GetComp<CompOverseerSubject>();
                    __result = __instance.overseerSubject;
                }
            }
        }
    }
}
