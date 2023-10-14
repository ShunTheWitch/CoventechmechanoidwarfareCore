using Vehicles;
using Verse;

namespace Thek_VFSpawner
{
    public class ModExtension_VehicleDefToSpawn : DefModExtension
    {

        /// <summary>
        /// Goes on the ThingDef, indicates what VehicleDef it will spawn
        /// </summary>
        public VehicleDef VehicleDefToSpawn;
    }
}