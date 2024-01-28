using Verse;

namespace taranchuk_flightcombat
{
    public class CompProperties_LandingThrusters : CompProperties
    {
        public CompProperties_LandingThrusters()
        {
            this.compClass = typeof(CompLandingThrusters);
        }
    }
    public class CompLandingThrusters : ThingComp
    {

    }
}
