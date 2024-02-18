using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(PawnUtility), "PawnsCanShareCellBecauseOfBodySize")]
    public static class PawnUtility_PawnsCanShareCellBecauseOfBodySize_Patch
    {
        public static void Postfix(ref bool __result, Pawn p1, Pawn p2)
        {
            if (__result is false)
            {
                __result = CanShareCell(p1);
                if (__result is false)
                {
                    __result = CanShareCell(p2);
                }
            }
        }
    
        private static bool CanShareCell(Pawn pawn)
        {
            if (pawn is VehiclePawn vehicle)
            {
                var comp = vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
