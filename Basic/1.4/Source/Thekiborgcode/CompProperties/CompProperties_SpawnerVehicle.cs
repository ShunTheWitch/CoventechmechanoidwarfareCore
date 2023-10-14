using System.Collections.Generic;
using Verse;

namespace Thek_VFSpawner
{
    public class CompProperties_SpawnerVehicle : CompProperties
    {

        /// <summary>
        /// Goes on the VehicleDef that SPAWNS the second vehicle, indicates what ThingDef is consumed for spawning a vehicle.
        /// </summary>
        public List<ThingDef> thingDefsConsumedForVehicle;
        public float rangeArea;
        public int cooldownTimeInTicks;
        //Due to reasons, always end this by .9 .

        public CompProperties_SpawnerVehicle()
        {
            compClass = typeof(CompSpawnVehicle);
        }
    }
}
