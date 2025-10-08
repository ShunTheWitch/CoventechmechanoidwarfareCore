using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(Pawn_MechanitorTracker), "CanCommandTo")]
    public static class Pawn_MechanitorTracker_CanCommandTo_Patch
    {
        public static void Postfix(Pawn_MechanitorTracker __instance, ref bool __result, LocalTargetInfo target)
        {
            if (__instance.pawn.ParentHolder?.ParentHolder is VehicleRoleHandler roleHandler
                && roleHandler.vehicle.GetComp<CompMechanitorControl>() != null)
            {
                if (target.Cell.InBounds(__instance.pawn.MapHeld))
                {
                    __result = (float)roleHandler.vehicle.Position.DistanceToSquared(target.Cell) < 620.01f;
                }
            }
        }
    }
}
