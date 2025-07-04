using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace universalflight
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
            if (parms?.raidArrivalMode != null)
            {
                Map map = (Map)parms.target;
                var worker = parms.raidArrivalMode.Worker;
                IntVec3 spawnCenter;
                if (parms.spawnCenter.IsValid is false || (worker is not PawnsArrivalModeWorker_EdgeDrop
                    && worker is not PawnsArrivalModeWorker_EdgeWalkIn))
                {
                    TryResolveRaidSpawnCenter((Map)parms.target, out spawnCenter);
                }
                else
                {
                    spawnCenter = parms.spawnCenter;
                }
                var spawnRotation = Rot4.FromAngleFlat((map.Center - spawnCenter).AngleFlat);
                for (int i = pawns.Count - 1; i >= 0; i--)
                {
                    var pawn = pawns[i];
                    if (pawn is Pawn)
                    {
                        var comp = pawn.TryGetComp<CompFlightMode>();
                        if (comp != null)
                        {
                            var maxSize = Mathf.Max(pawn.def.Size.x, pawn.def.Size.z);
                            CellFinder.TryFindRandomCellNear(spawnCenter, map, Mathf.Max(8, maxSize),
                                delegate (IntVec3 x)
                                {
                                    return GenAdj.OccupiedRect(x, spawnRotation, new IntVec2(maxSize, maxSize)).ExpandedBy(1).Cells.All(x => x.InBounds(map));
                                }, out var loc);
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
                        }
                    }
                }
            }
        }

        public static bool TryResolveRaidSpawnCenter(Map map, out IntVec3 spawnCenter)
        {
            spawnCenter = IntVec3.Invalid;
            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => true, map, CellFinder.EdgeRoadChance_Hostile, out spawnCenter))
            {
                return false;
            }
            return true;
        }
    }
}
