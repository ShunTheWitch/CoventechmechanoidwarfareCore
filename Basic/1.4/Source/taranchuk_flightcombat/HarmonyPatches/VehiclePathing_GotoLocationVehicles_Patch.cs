using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehiclePathing), nameof(VehiclePathing.GotoLocationVehicles))]
    public static class VehiclePathing_GotoLocationVehicles_Patch
    {
        public static bool Prefix(IntVec3 __0, Pawn __1, ref bool __result)
        {
            Log.Message("vehicle.Faction: " + __1?.Faction);
            var vehicle = __1 as VehiclePawn;
            if (vehicle != null && vehicle.Faction == Faction.OfPlayer)
            {
                var comp = vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    comp.SetTarget(__0);
                    if (comp.Props.waypointFleck != null)
                    {
                        FleckMaker.Static(__0, __1.Map, comp.Props.waypointFleck);
                    }
                    __result = false;
                    return false;
                }
            }
            Log.Message("vehicle.Faction: " + vehicle?.Faction);
            return true;
        }
    }
}
