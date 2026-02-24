using HarmonyLib;
using System;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), new Type[] { typeof(Thing), typeof(IntVec3),
        typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) })]
    public static class GenSpawn_Spawn_Patch
    {
        public static void Prefix(ref Thing newThing, ref IntVec3 loc, Map map)
        {
            if (newThing is VehicleSkyfaller_Arriving skyfaller)
            {
                var comp = skyfaller.vehicle.GetComp<CompFlightMode>();
                if (comp != null && Utils.TryFindRandomEdgeSpawnCellFor(skyfaller.vehicle.def, map, out var spawnPos))
                {
                    newThing = skyfaller.vehicle;

                    if (comp.flightMode == FlightMode.Flight)
                    {
                        comp.CurAngle = (map.Center - spawnPos).AngleFlat - comp.FlightAngleOffset;
                        comp.SetTarget(map.Center);
                    }
                    else
                    {
                        comp.CurAngle = (loc - spawnPos).AngleFlat - comp.FlightAngleOffset;
                        comp.SetTarget(loc);
                    }
                    loc = spawnPos;
                }
            }
        }
    }
}
