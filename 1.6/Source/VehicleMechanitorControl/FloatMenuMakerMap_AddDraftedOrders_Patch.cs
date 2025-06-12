using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddDraftedOrders")]
    public static class FloatMenuMakerMap_AddDraftedOrders_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool suppressAutoTakeableGoto = false)
        {
            if (pawn.GetComp<CompMechanitorControl>() != null 
                && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                if (MechanitorUtility.IsMechanitor(pawn))
                {
                    IntVec3 c = IntVec3.FromVector3(clickPos);
                    List<Thing> thingList = c.GetThingList(pawn.Map);
                    foreach (Thing thing2 in thingList)
                    {
                        Pawn mech;
                        if ((mech = thing2 as Pawn) == null || !mech.IsColonyMech)
                        {
                            continue;
                        }
                        if (mech.GetOverseer() != pawn)
                        {
                            if (!pawn.CanReach(mech, PathEndMode.Touch, Danger.Deadly))
                            {
                                opts.Add(new FloatMenuOption("CannotControlMech".Translate(mech.LabelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                            }
                            else if (!MechanitorUtility.CanControlMech(pawn, mech))
                            {
                                AcceptanceReport acceptanceReport = MechanitorUtility.CanControlMech(pawn, mech);
                                if (!acceptanceReport.Reason.NullOrEmpty())
                                {
                                    opts.Add(new FloatMenuOption("CannotControlMech".Translate(mech.LabelShort) + ": " + acceptanceReport.Reason, null));
                                }
                            }
                            else
                            {
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("ControlMech".Translate(mech.LabelShort), delegate
                                {
                                    Job job22 = JobMaker.MakeJob(JobDefOf.ControlMech, thing2);
                                    pawn.jobs.TryTakeOrderedJob(job22, JobTag.Misc);
                                }), pawn, new LocalTargetInfo(thing2)));
                            }
                            opts.Add(new FloatMenuOption("CannotDisassembleMech".Translate(mech.LabelCap) + ": " + "MustBeOverseer".Translate().CapitalizeFirst(), null));
                        }
                        else
                        {
                            opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("DisconnectMech".Translate(mech.LabelShort), delegate
                            {
                                MechanitorUtility.ForceDisconnectMechFromOverseer(mech);
                            }, MenuOptionPriority.Low, null, null, 0f, null, null, playSelectionSound: true, -10), pawn, new LocalTargetInfo(thing2)));
                            if (!mech.IsFighting())
                            {
                                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("DisassembleMech".Translate(mech.LabelCap), delegate
                                {
                                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmDisassemblingMech".Translate(mech.LabelCap) + ":\n" + (from x in MechanitorUtility.IngredientsFromDisassembly(mech.def)
                                                                                                                                                             select x.Summary).ToLineList("  - "), delegate
                                                                                                                                                             {
                                                                                                                                                                 pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DisassembleMech, thing2), JobTag.Misc);
                                                                                                                                                             }, destructive: true));
                                }, MenuOptionPriority.Low, null, null, 0f, null, null, playSelectionSound: true, -20), pawn, new LocalTargetInfo(thing2)));
                            }
                        }
                        if (!pawn.Drafted || !MechRepairUtility.CanRepair(mech))
                        {
                            continue;
                        }
                        if (!pawn.CanReach(mech, PathEndMode.Touch, Danger.Deadly))
                        {
                            opts.Add(new FloatMenuOption("CannotRepairMech".Translate(mech.LabelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                            continue;
                        }
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("RepairThing".Translate(mech.LabelShort), delegate
                        {
                            Job job21 = JobMaker.MakeJob(JobDefOf.RepairMech, mech);
                            pawn.jobs.TryTakeOrderedJob(job21, JobTag.Misc);
                        }), pawn, new LocalTargetInfo(thing2)));
                    }
                }
            }
        }
    }
}
