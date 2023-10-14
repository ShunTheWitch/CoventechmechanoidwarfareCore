using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Vehicles;

namespace Thek_VFSpawner
{
    public class CompRepackVehicle : ThingComp
    {
        public CompProperties_RepackVehicle Props => (CompProperties_RepackVehicle)props;
        public VehiclePawn Vehicle => parent as VehiclePawn;
        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action repackAction = new()
            {
                defaultLabel = "Repack Vehicle",
                icon = ContentFinder<Texture2D>.Get("Gizmo/RepackGizmo", true),
                action = delegate()
                {
                    if (Event.current.button == 0)
                    {   
                        GenSpawn.Spawn(Props.thingDefSpawnedFromRepack, Vehicle.Position, Vehicle.Map);
                        Vehicle.DeSpawn();
                    }
                }
            };
            yield return repackAction;
        }
    }
}
