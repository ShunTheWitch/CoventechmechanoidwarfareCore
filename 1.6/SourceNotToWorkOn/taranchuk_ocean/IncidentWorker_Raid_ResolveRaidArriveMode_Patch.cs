using HarmonyLib;
using RimWorld;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(IncidentWorker_Raid), "ResolveRaidArriveMode")]
    public static class IncidentWorker_Raid_ResolveRaidArriveMode_Patch
    {
        public static void Postfix(IncidentParms parms)
        {
            var worker = parms.raidArrivalMode.Worker;
            if (parms.target is Map map && map.IsWaterBiome() && worker is not PawnsArrivalModeWorker_EdgeDrop
                && worker is not PawnsArrivalModeWorker_CenterDrop && worker is not PawnsArrivalModeWorker_EdgeDropGroups
                && worker is not PawnsArrivalModeWorker_RandomDrop)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
            }
        }
    }
}
