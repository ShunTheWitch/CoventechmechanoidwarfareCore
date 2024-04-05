using HarmonyLib;
using System;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool) })]
    public static class GenSpawn_Spawn_Patch
    {
        public static void Prefix(ref Thing newThing, ref IntVec3 loc, Map map)
        {
            if (newThing is VehicleSkyfaller_Arriving skyfaller)
            {
                var comp = skyfaller.vehicle.GetComp<CompFlightMode>();
                if (comp != null && PawnsArrivalModeWorker_Arrive_Patch.TryResolveRaidSpawnCenter(map, out var spawnPos))
                {
                    newThing = skyfaller.vehicle;
                    if (comp.Props.flightCommands.hoverMode != null)
                    {
                        comp.SetHoverMode(true);
                    }
                    comp.CurAngle = (loc - spawnPos).AngleFlat - comp.FlightAngleOffset;
                    comp.SetTarget(loc);
                    loc = spawnPos;
                }
            }
        }
    }
}
