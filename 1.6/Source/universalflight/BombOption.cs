using System.Collections.Generic;
using Verse;

namespace universalflight
{

    public class BombOption
    {
        public string label;
        public string texPath;
        public List<ThingDefCountClass> costList;
        public ThingDef projectile;
        public int cooldownTicks;
    }
}
