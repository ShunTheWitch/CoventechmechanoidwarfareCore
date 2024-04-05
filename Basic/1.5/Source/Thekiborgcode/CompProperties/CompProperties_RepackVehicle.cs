using Verse;
using RimWorld;

namespace Thek_VFSpawner
{
    public class CompProperties_RepackVehicle : CompProperties
    {
        public ThingDef thingDefSpawnedFromRepack;

        public CompProperties_RepackVehicle()
        {
            compClass = typeof(CompRepackVehicle);
        }
    }
}
