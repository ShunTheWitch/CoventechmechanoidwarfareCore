using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    [HarmonyPatch]
    public static class PawnsArrivalModeWorker_Arrive_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var subClass in typeof(PawnsArrivalModeWorker).AllSubclassesNonAbstract())
            {
                var method = subClass.GetMethod("Arrive", AccessTools.all);
                if (method != null)
                {
                    yield return method;
                }
            }
        }

        public static void Prefix(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            var worker = parms.raidArrivalMode.Worker;
            IntVec3 spawnCenter;
            if (parms.spawnCenter.IsValid is false || (worker is not PawnsArrivalModeWorker_EdgeDrop 
                && worker is not PawnsArrivalModeWorker_EdgeWalkIn))
            {
                TryResolveRaidSpawnCenter(parms, out spawnCenter);
            }
            else
            {
                spawnCenter = parms.spawnCenter;
            }
            var spawnRotation = Rot4.FromAngleFlat((map.Center - spawnCenter).AngleFlat);
            for (int i = pawns.Count - 1; i >= 0; i--)
            {
                var pawn = pawns[i];
                if (pawn is VehiclePawn)
                {
                    var comp = pawn.TryGetComp<CompFlightMode>();
                    if (comp != null)
                    {
                        var maxSize = Mathf.Max(pawn.def.Size.x, pawn.def.Size.z);
                        IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnCenter, map, Mathf.Max(8, maxSize), delegate (IntVec3 x)
                        {
                            return GenAdj.OccupiedRect(x, spawnRotation, new IntVec2(maxSize, maxSize)).ExpandedBy(1).Cells.All(x => x.InBounds(map));
                        });
                        GenSpawn.Spawn(pawn, loc, map, spawnRotation);
                        if (comp.Props.AISettings?.gunshipSettings?.gunshipMode == GunshipMode.Hovering)
                        {
                            comp.SetHoverMode(true);
                        }
                        else
                        {
                            comp.SetFlightMode(true);
                        }
                        comp.takeoffProgress = 1f;
                        pawns.RemoveAt(i);
                        Log.Message("Spawning " + pawn + " at " + loc + " with mode: " + comp.flightMode);
                    }
                }
            }
        }

        public static bool TryResolveRaidSpawnCenter(IncidentParms parms, out IntVec3 spawnCenter)
        {
            spawnCenter = IntVec3.Invalid;
            Map map = (Map)parms.target;
            if (parms.attackTargets != null && parms.attackTargets.Count > 0 && !RCellFinder.TryFindEdgeCellFromThingAvoidingColony(parms.attackTargets[0], map, predicate, out spawnCenter))
            {
                CellFinder.TryFindRandomEdgeCellWith((IntVec3 p) => !map.roofGrid.Roofed(p) && p.Walkable(map), map, CellFinder.EdgeRoadChance_Hostile, out spawnCenter);
            }
            if (!spawnCenter.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out spawnCenter, map, CellFinder.EdgeRoadChance_Hostile))
            {
                return false;
            }
            parms.spawnRotation = Rot4.FromAngleFlat((map.Center - spawnCenter).AngleFlat);
            return true;
            bool predicate(IntVec3 from, IntVec3 to)
            {
                if (!map.roofGrid.Roofed(from) && from.Walkable(map))
                {
                    return map.reachability.CanReach(from, to, PathEndMode.OnCell, TraverseMode.NoPassClosedDoors, Danger.Some);
                }
                return false;
            }
        }
    }
}
