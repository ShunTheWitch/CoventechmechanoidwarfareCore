using HarmonyLib;
using System;
using Vehicles;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(ReachabilityUtility), nameof(ReachabilityUtility.CanReach), new Type[] { typeof(Pawn), typeof(LocalTargetInfo), 
        typeof(PathEndMode), typeof(Danger), typeof(bool), typeof(bool), typeof(TraverseMode) })]
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
