using HarmonyLib;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(ReachabilityUtility), "CanReach")]
    public static class ReachabilityUtility_CanReach_Patch
    {
        public static void Postfix(ref bool __result, Pawn pawn, LocalTargetInfo dest)
        {
            if (dest.Thing is VehiclePawn vehicle)
            {
                var comp = vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    __result = false;
                }
            }
        }
    }
}
