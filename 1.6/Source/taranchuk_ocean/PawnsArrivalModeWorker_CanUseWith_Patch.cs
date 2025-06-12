using HarmonyLib;
using RimWorld;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(PawnsArrivalModeWorker), "CanUseWith")]
    public static class PawnsArrivalModeWorker_CanUseWith_Patch
    {
        public static void Postfix(PawnsArrivalModeWorker __instance, ref bool __result, IncidentParms parms)
        {
            if (parms.target is Map map && map.IsWaterBiome() && __instance is not PawnsArrivalModeWorker_EdgeDrop
                && __instance is not PawnsArrivalModeWorker_CenterDrop && __instance is not PawnsArrivalModeWorker_EdgeDropGroups
                && __instance is not PawnsArrivalModeWorker_RandomDrop)
            {
                __result = false;
            }
        }
    }
}
