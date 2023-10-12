using HarmonyLib;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(MechanitorUtility), "InMechanitorCommandRange")]
    public static class MechanitorUtility_InMechanitorCommandRange_Patch
    {
        public static void Postfix(Pawn mech, LocalTargetInfo target, ref bool __result)
        {
            Pawn overseer = mech.GetOverseer();
            if (overseer != null)
            {
                foreach (var pawn in overseer.mechanitor.ControlledPawns)
                {
                    if (pawn.OverseerSubject.Overseer == overseer)
                    {
                        var comp = pawn.GetComp<CompMechanitorControl>();
                        if (comp != null && comp.Props.mechControlRange > 0 && pawn.Map == mech.Map)
                        {
                            if (pawn.Position.DistanceTo(mech.Position) <= comp.Props.mechControlRange)
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
