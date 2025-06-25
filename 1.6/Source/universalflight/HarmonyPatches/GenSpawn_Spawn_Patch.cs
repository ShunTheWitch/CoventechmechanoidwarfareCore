using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace universalflight
{
    [HotSwappable]
    [HarmonyPatch(typeof(GenSpawn), "Spawn", new Type[] { typeof(Thing), typeof(IntVec3),
        typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool) })]
    public static class GenSpawn_Spawn_Patch
    {
        public static void Prefix(ref Thing newThing, ref IntVec3 loc, Map map)
        {
            // TODO: figure out this mess
            //if (newThing is Skyfaller skyfaller)
            //{
            //    var comp = skyfaller.pawn.GetComp<CompFlightMode>();
            //    if (comp != null && PawnsArrivalModeWorker_Arrive_Patch.TryResolveRaidSpawnCenter(map, //out var spawnPos))
            //    {
            //        newThing = skyfaller.pawn;
            //        if (GenAdj.OccupiedRect(spawnPos, newThing.Rotation, newThing.def.size)
            //            .InBoundsLocal(map) is false)
            //        {
            //            foreach (var cell in GenRadial.RadialCellsAround(spawnPos, 5, true))
            //            {
            //                if (GenAdj.OccupiedRect(cell, newThing.Rotation, newThing.def.size).//InBoundsLocal(map))
            //                {
            //                    spawnPos = cell;
            //                    break;
            //                }
            //            }
            //        }
//
            //        if (comp.flightMode == FlightMode.Flight)
            //        {
            //            comp.CurAngle = (map.Center - spawnPos).AngleFlat - comp.FlightAngleOffset;
            //            comp.SetTarget(map.Center);
            //        }
            //        else
            //        {
            //            comp.CurAngle = (loc - spawnPos).AngleFlat - comp.FlightAngleOffset;
            //            comp.SetTarget(loc);
            //        }
            //        loc = spawnPos;
            //    }
            //}
        }
    }
}
