using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;
using Verse.AI;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_Mechanitor), "GetOptionsFor")]
    public static class FloatMenuOptionProvider_Mechanitor_GetOptionsFor_Patch
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Pawn clickedPawn, FloatMenuContext context)
        {
            var filteredResult = __result;
            if (clickedPawn.TryGetComp<CompMechanitorControl>() != null)
            {
                var disassembleLabel = "DisassembleMech".Translate(clickedPawn.LabelCap);
                var cannotDisassembleLabel = "CannotDisassembleMech".Translate(clickedPawn.LabelCap);
                filteredResult = __result.Where(opt => !opt.Label.Contains(disassembleLabel) && !opt.Label.Contains(cannotDisassembleLabel));
            }

            foreach (var res in filteredResult)
            {
                yield return res;
            }

            if (clickedPawn.IsColonyMech)
            {
                var mechanitor = context.FirstSelectedPawn;
                if (mechanitor.ParentHolder?.ParentHolder is VehicleRoleHandler vehicleHandler && vehicleHandler.vehicle.GetComp<CompMechanitorControl>() != null)
                {
                    if (clickedPawn.GetOverseer() != mechanitor)
                    {
                        if (!mechanitor.CanReach(clickedPawn, PathEndMode.Touch, Danger.Deadly))
                        {
                            yield return new FloatMenuOption("CannotControlMech".Translate(clickedPawn.LabelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                        }
                        else if (!MechanitorUtility.CanControlMech(mechanitor, clickedPawn))
                        {
                            AcceptanceReport acceptanceReport = MechanitorUtility.CanControlMech(mechanitor, clickedPawn);
                            if (!acceptanceReport.Reason.NullOrEmpty())
                            {
                                yield return new FloatMenuOption("CannotControlMech".Translate(clickedPawn.LabelShort) + ": " + acceptanceReport.Reason, null);
                            }
                        }
                        else
                        {
                            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ControlMech".Translate(clickedPawn.LabelShort), delegate
                            {
                                Job job2 = JobMaker.MakeJob(JobDefOf.ControlMech, clickedPawn);
                                mechanitor.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
                            }), mechanitor, new LocalTargetInfo(clickedPawn));
                        }
                    }
                }
            }
        }
    }
}