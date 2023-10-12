using HarmonyLib;
using System.Linq;
using UnityEngine;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(MechanitorUtility), "InMechanitorCommandRange")]
    public static class MechanitorUtility_InMechanitorCommandRange_Patch
    {
        public static void Postfix(Pawn mech, LocalTargetInfo target, ref bool __result)
        {
            if (!__result)
            {
                Pawn overseer = mech.GetOverseer();
                if (overseer != null)
                {
                    foreach (var pawn in overseer.mechanitor.ControlledPawns)
                    {
                        if (pawn.OverseerSubject.Overseer == overseer && pawn is VehiclePawn vehicle)
                        {
                            var comp = vehicle.GetComp<CompMechanitorControl>();
                            if (comp != null && comp.Props.mechControlRange > 0 && vehicle.Map == mech.Map)
                            {
                                if (vehicle.Position.DistanceTo(mech.Position) <= comp.Props.mechControlRange)
                                {
                                    __result = true;
                                    return;
                                }
                            }
                            foreach (var passenger in vehicle.handlers.SelectMany(x => x.handlers.OfType<Pawn>()))
                            {
                                if (passenger == overseer && overseer.mechanitor.CanCommandTo(target))
                                {
                                    __result = true;
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
