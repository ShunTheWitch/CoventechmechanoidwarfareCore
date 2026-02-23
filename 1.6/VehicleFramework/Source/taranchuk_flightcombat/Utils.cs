using RimWorld;
using System.Collections.Generic;
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
    }
}
