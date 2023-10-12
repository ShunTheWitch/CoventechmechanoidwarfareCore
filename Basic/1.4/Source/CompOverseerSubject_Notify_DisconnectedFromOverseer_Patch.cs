using HarmonyLib;
using RimWorld;
using Verse;

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

    [HarmonyPatch(typeof(PawnRelationWorker_Overseer), "OnRelationCreated")]
    public static class PawnRelationWorker_Overseer_OnRelationCreated_Patch
    {
        public static void Postfix(Pawn firstPawn, Pawn secondPawn)
        {
            Pawn mechanitor = (MechanitorUtility.IsMechanitor(firstPawn) ? firstPawn : secondPawn);
            var hediff = mechanitor.health.hediffSet.GetFirstHediffOfDef(CVN_DefOf.BandNode) as Hediff_BandNode;
            if (hediff != null)
            {
                hediff.RecacheBandNodes();
            }
        }
    }

    [HarmonyPatch(typeof(PawnRelationWorker_Overseer), "OnRelationRemoved")]
    public static class PawnRelationWorker_Overseer_OnRelationRemoved_Patch
    {
        public static void Postfix(Pawn firstPawn, Pawn secondPawn)
        {
            Pawn mechanitor = (MechanitorUtility.IsMechanitor(firstPawn) ? firstPawn : secondPawn);
            var hediff = mechanitor.health.hediffSet.GetFirstHediffOfDef(CVN_DefOf.BandNode) as Hediff_BandNode;
            if (hediff != null)
            {
                hediff.RecacheBandNodes();
            }
        }
    }
}
