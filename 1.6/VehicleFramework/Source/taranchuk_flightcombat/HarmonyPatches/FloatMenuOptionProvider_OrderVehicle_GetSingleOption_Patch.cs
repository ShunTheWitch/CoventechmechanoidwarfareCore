using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_OrderVehicle), "GetSingleOption")]
    public static class FloatMenuOptionProvider_OrderVehicle_GetSingleOption_Patch
    {
        public static bool Prefix(FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (context.FirstSelectedPawn is VehiclePawn vehicle)
            {
                var comp = vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    var cell = context.ClickedCell;
                    if (comp.Props.canFlyInSpace is false && cell.GetTerrain(vehicle.Map) == TerrainDefOf.Space)
                    {
                        __result = null;
                    }
                    else
                    {
                        __result = new FloatMenuOption("GoHere".Translate(), delegate
                        {
                            comp.SetTarget(cell);
                            if (comp.Props.waypointFleck != null)
                            {
                                FleckMaker.Static(cell, vehicle.Map, comp.Props.waypointFleck);
                            }
                        })
                        {
                            isGoto = true,
                            autoTakeable = true,
                            autoTakeablePriority = 10f
                        };
                    }
                    return false;
                }
            }
            return true;
        }
    }
}