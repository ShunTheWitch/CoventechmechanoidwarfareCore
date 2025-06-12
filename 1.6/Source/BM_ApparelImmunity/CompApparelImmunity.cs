using System.Collections.Generic;
using Verse;

namespace BM_ApparelImmunity
{
    public class CompProperties_ApparelImmunity : CompProperties
    {
        public List<HediffDef> immuneToHediffs;

        public CompProperties_ApparelImmunity() => compClass = typeof(CompApparelImmunity);
    }

    public class CompApparelImmunity : ThingComp
    {
        public CompProperties_ApparelImmunity Props => base.props as CompProperties_ApparelImmunity;
    }
}
