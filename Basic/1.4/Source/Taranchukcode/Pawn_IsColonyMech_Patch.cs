﻿using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(Pawn), "IsColonyMech", MethodType.Getter)]
    public static class Pawn_IsColonyMech_Patch
    {
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (!__result && __instance is VehiclePawn vehicle && vehicle.Faction == Faction.OfPlayer)
            {
                if (Pawn_ThreatDisabled_Patch.checkingNow || GenHostility_HostileTo_Patch.checkingNow)
                {
                    return;
                }
                var comp = vehicle.GetComp<CompMechanitorControl>();
                if (comp != null)
                {
                    __result = true;
                }
            }
        }
    }
}
