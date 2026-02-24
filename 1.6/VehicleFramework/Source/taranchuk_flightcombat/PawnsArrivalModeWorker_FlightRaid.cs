using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    public class PawnsArrivalModeWorker_FlightRaid : PawnsArrivalModeWorker
    {
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!parms.spawnCenter.IsValid)
            {
                parms.spawnCenter = DropCellFinder.FindRaidDropCenterDistant(map);
            }
            parms.spawnRotation = Rot4.Random;
            return true;
        }

        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            var flyingVehicles = new List<Pawn>();
            var otherPawns = new List<Pawn>();

            foreach (var pawn in pawns)
            {
                if (pawn is VehiclePawn vehicle && vehicle.HasComp<CompFlightMode>())
                {
                    flyingVehicles.Add(pawn);
                }
                else
                {
                    otherPawns.Add(pawn);
                }
            }

            if (flyingVehicles.Any())
            {
                foreach (var pawn in flyingVehicles)
                {
                    var vehicle = (VehiclePawn)pawn;
                    var comp = vehicle.GetComp<CompFlightMode>();

                    IntVec3 spawnCell;
                    if (!Utils.TryFindRandomEdgeSpawnCellFor(vehicle.def, map, out spawnCell))
                    {
                        spawnCell = parms.spawnCenter;
                        Log.Warning($"[FlightCombat] Could not find a valid edge spawn cell for {vehicle.LabelCap}. Spawning at raid center.");
                    }
                    
                    var spawnRotation = Rot4.FromAngleFlat((map.Center - spawnCell).AngleFlat);
                    GenSpawn.Spawn(vehicle, spawnCell, map, spawnRotation);

                    if (comp.Props.AISettings?.gunshipSettings?.chaseMode == ChaseMode.Hovering)
                        comp.SetHoverMode(true);
                    else
                        comp.SetFlightMode(true);
                    
                    comp.takeoffProgress = 1f;
                    comp.curPosition = vehicle.DrawPos;
                    comp.CurAngle = vehicle.Rotation.AsAngle - comp.FlightAngleOffset;
                }
            }

            if (otherPawns.Any())
            {
                PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms, otherPawns);
            }
        }
    }
}
