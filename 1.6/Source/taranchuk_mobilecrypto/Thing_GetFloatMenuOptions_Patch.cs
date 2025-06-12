using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace taranchuk_mobilecrypto
{
    [HarmonyPatch(typeof(Thing), "GetFloatMenuOptions")]
    public static class Thing_GetFloatMenuOptions_Patch
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> result, Thing __instance, Pawn selPawn)
        {
            foreach (FloatMenuOption option in result)
            {
                yield return option;
            }

            foreach (var comp in GetCryptoComps(__instance))
            {
                foreach (var pawn in comp.StoredPawns)
                {
                    var releaseCommand = comp.Props.releaseCommand.GetCommand();
                    yield return new FloatMenuOption(releaseCommand.defaultLabel.Formatted(pawn), delegate
                    {
                        selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(comp.Props.releaseJob, pawn, comp.parent, comp.Holder));
                    });
                }
            }
        }

        public static IEnumerable<CompMobileCrypto> GetCryptoComps(Thing __instance)
        {
            var comp = __instance.TryGetComp<CompMobileCrypto>();
            if (comp != null)
            {
                yield return comp;
            }
            var pawn = __instance as Pawn;
            if (pawn is null && __instance is Corpse corpse)
            {
                pawn = corpse.InnerPawn;
                comp = pawn.TryGetComp<CompMobileCrypto>(); 
                if (comp != null)
                {
                    yield return comp;
                }
            }
            if (pawn != null && pawn.apparel?.WornApparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    comp = apparel.GetComp<CompMobileCrypto>();
                    if (comp != null)
                    {
                        yield return comp;
                    }
                }
            }
        }
    }
}
