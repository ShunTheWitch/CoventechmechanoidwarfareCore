using HarmonyLib;
using RimWorld;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(IncidentWorker_NeutralGroup), "TryResolveParmsGeneral")]
    public static class IncidentWorker_NeutralGroup_TryResolveParmsGeneral_Patch
    {
        public static bool Prefix(IncidentParms parms)
        {
            if (parms.target is Map map && map.IsWaterBiome())
            {
                return false;
            }
            return true;
        }
    }
}
