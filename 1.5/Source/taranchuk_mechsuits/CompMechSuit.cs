using Verse;

namespace taranchuk_mechsuits
{

    public class CompProperties_MechSuit : CompProperties
    {
        public CompProperties_MechSuit()
        {
            this.compClass = typeof(CompMechSuit);
        }
    }

    public class CompMechSuit : ThingComp
    {
        public CompProperties_MechSuit Props => base.props as CompProperties_MechSuit;
    }
}
