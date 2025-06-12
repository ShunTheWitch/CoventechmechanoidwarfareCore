using HarmonyLib;
using System;
using Verse;

namespace BM_ApparelImmunity
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[]
    {
        typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult)
    })]
    public static class Pawn_HealthTracker_AddHediff_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        private static bool Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, ref Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
        {
            if (___pawn.PreventFromCatching(hediff.def))
            {
                return false;
            }
            return true;
        }

        public static bool PreventFromCatching(this Pawn pawn, HediffDef hediff)
        {
            if (pawn.RaceProps.Humanlike)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    var comp = apparel.GetComp<CompApparelImmunity>();
                    if (comp != null && comp.Props.immuneToHediffs.Contains(hediff)) 
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
