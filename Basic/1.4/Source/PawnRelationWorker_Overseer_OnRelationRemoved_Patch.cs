﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace VehicleMechanitorControl
{
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
