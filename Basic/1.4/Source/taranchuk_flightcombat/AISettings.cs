using System.Collections.Generic;
using Verse;

namespace taranchuk_flightcombat
{
    public abstract class AIFightSettings
    {
        public List<ThingDefCountRangeClass> npcStock;
    }

    public class BomberSettings : AIFightSettings
    {
        public int? maxBombRun;
        public List<ThingDef> blacklistedBombs;
        public float minRangeToStartBombing;
    }

    public class GunshipSettings : AIFightSettings
    {
        public GunshipMode gunshipMode;
    }
    public enum GunshipMode
    {
        Circling, Chasing
    }

    public class AISettings
    {
        public BomberSettings bomberSettings;
        public GunshipSettings gunshipSettings;
    }
}
