using RimWorld;
using Verse;

namespace universalflight
{
    public static class Utils
    {
        public static float GetMass(this Pawn pawn)
        {
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

        public static float ClampAndWrap(this float value, float min, float max)
        {
            if (value < min)
            {
                value = max - (min - value);
            }
            else if (value > max)
            {
                value = min + (value - max);
            }
            return value;
        }
    }
}
