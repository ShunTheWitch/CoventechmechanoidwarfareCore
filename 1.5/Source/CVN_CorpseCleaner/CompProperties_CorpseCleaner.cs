using RimWorld;
using Verse;

namespace CVN_CorpseCleaner
{
    public class CompProperties_CorpseCleaner : CompProperties
    {
        public float radius = 5;

        public int timeToComplete = 300;

        public ThingDef thingToSpawn;

        public ThingDef orbDef;

        public int amountToSpawnPerCorpse;

        public string gizmoName;

        public string gizmoDesc;

        public string gizmoIcon;

        public CompProperties_CorpseCleaner()
        {
            compClass = typeof(CompCorpseCleaner);
        }
    }
}
