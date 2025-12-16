using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(Pawn_MechanitorTracker), "CanControlMechs", MethodType.Getter)]
    public static class Pawn_MechanitorTracker_CanControlMechs_Patch
    {
        public static void Postfix(ref AcceptanceReport __result, Pawn_MechanitorTracker __instance)
        {
            if (__result.Reason.NullOrEmpty() && __result.Accepted is false && __instance.pawn.Spawned is false 
                && __instance.pawn.ParentHolder?.ParentHolder is VehicleRoleHandler roleHandler
                && roleHandler.vehicle.GetComp<CompMechanitorControl>() != null)
            {
                __result = true;
            }
        }
    }
}
