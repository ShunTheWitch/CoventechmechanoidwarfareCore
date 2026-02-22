using System.Collections.Generic;
using Verse;

namespace taranchuk_flightcombat
{
    public abstract class AIFightSettings
    {
        public List<ThingDefCountRangeClass> npcStock;
        public float distanceFromTarget;
    }

    public class BomberSettings : AIFightSettings
    {
        public int? maxBombRun;
        public List<ThingDef> blacklistedBombs;
    }

    public class GunshipSettings : AIFightSettings
    {
        public FlightPattern flightPattern = FlightPattern.Around;
        public ChaseMode chaseMode = ChaseMode.Circling;
    }

    public class AISettings
    {
        public BomberSettings bomberSettings;
        public GunshipSettings gunshipSettings;
    }
}
