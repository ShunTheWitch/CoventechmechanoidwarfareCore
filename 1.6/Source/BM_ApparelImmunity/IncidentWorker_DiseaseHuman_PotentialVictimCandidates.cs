using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace BM_ApparelImmunity
{
    [HarmonyPatch(typeof(IncidentWorker_DiseaseHuman), "PotentialVictimCandidates")]
    public static class IncidentWorker_DiseaseHuman_PotentialVictimCandidates
    {
        public static IEnumerable<Pawn> Postfix(IEnumerable<Pawn> __result, IncidentWorker_DiseaseHuman __instance)
        {
            foreach (var p in __result)
            {
                if (p.PreventFromCatching(__instance.def.diseaseIncident))
                {
                    continue;
                }
                else
                {
                    yield return p;
                }
            }
        }
    }
}
