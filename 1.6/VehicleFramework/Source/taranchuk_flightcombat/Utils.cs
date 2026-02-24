using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;
using UnityEngine;

namespace taranchuk_flightcombat
{
    public static class Utils
    {
        public static readonly HashSet<ThingDef> flightCapableDefs;
        static Utils()
        {
            flightCapableDefs = new HashSet<ThingDef>();
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.IsCorpse is false && def.comps != null)
                {
                    foreach (var compProps in def.comps)
                    {
                        if (compProps is CompProperties_FlightMode)
                        {
                            flightCapableDefs.Add(def);
                            break;
                        }
                    }
                }
            }
        }

        public static float AngleDiff(float from, float to)
        {
            float delta = (to - from + 180) % 360 - 180;
            return delta < -180 ? delta + 360 : delta;
        }

        public static float GetMass(this Pawn pawn)
        {
            if (pawn is VehiclePawn vehicle)
            {
                return vehicle.GetStatValue(VehicleStatDefOf.Mass);
            }
            return pawn.GetStatValue(StatDefOf.Mass);
        }

        public static bool InBoundsLocal(this CellRect occupiedRect, Map map)
        {
            for (int i = occupiedRect.minZ; i <= occupiedRect.maxZ; i++)
            {
                for (int j = occupiedRect.minX; j <= occupiedRect.maxX; j++)
                {
                    if (new IntVec3(j, 0, i).InBounds(map) is false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool TryFindRandomEdgeSpawnCellFor(ThingDef vehicleDef, Map map, out IntVec3 spawnCell)
        {
            int maxPush = Mathf.Max(vehicleDef.Size.x, vehicleDef.Size.z) + 2;

            var edgeCells = CellRect.WholeMap(map).EdgeCells.InRandomOrder().ToList();

            foreach (var edgeCell in edgeCells)
            {
                Rot4 inwardRot = Rot4.FromAngleFlat((map.Center - edgeCell).AngleFlat);

                for (int offset = 0; offset <= maxPush; offset++)
                {
                    IntVec3 testCell = edgeCell + (inwardRot.FacingCell * offset);

                    if (!testCell.InBounds(map)) break;

                    if (GenAdj.OccupiedRect(testCell, inwardRot, vehicleDef.Size).InBoundsLocal(map))
                    {
                        spawnCell = testCell;
                        return true;
                    }
                }
            }

            spawnCell = IntVec3.Invalid;
            return false;
        }
    }
}
