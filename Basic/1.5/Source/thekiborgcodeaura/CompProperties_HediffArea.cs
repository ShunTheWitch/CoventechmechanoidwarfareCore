namespace Thek_HediffArea
{
    public class CompProperties_HediffArea : CompProperties
    {
        public HediffDef hediffToApplyFleshlings;

        public HediffDef hediffToApplyMechanoids;

        public float areaRange;

        public bool appliesToFriendlies;

        public bool appliesToHostiles;

        public bool mechanoidExclusive;

        public int refreshRateInTicks;

        public CompProperties_HediffArea()
        {
            compClass = typeof(Comp_HediffArea);
        }
    }
}
