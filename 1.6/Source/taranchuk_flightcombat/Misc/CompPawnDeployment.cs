using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    public class CompProperties_PawnDeployment : CompProperties
    {
        public IntVec2 pickupAreaSize;
        public FlightCommand_Action loadCommand;
        public FlightCommand_Action unloadCommand;
        public bool takePawnsOfAnyFaction;
        public float? maxPawnMass;
        public CompProperties_PawnDeployment()
        {
            this.compClass = typeof(CompPawnDeployment);
        }
    }
    [HotSwappable]
    public class CompPawnDeployment : ThingComp
    {
        public CompProperties_PawnDeployment Props => base.props as CompProperties_PawnDeployment;

        public VehiclePawn Vehicle => parent as VehiclePawn;

        public CompFlightMode compFlightMode;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compFlightMode = this.parent.GetComp<CompFlightMode>();
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Vehicle.Map != null)
            {
                GenDraw.DrawFieldEdges(GetPickupCells());
            }
        }

        public List<IntVec3> GetPickupCells()
        {
            var list = new List<IntVec3>();
            var cellRect = GenAdj.OccupiedRect(Vehicle.Position, Vehicle.Rotation, Props.pickupAreaSize);
            return cellRect.Where(x => x.InBounds(Vehicle.Map)).ToList();
        }

        public List<Pawn> GetPawnsInPickupCells()
        {
            var list = new List<Pawn>();
            foreach (var cell in GetPickupCells())
            {
                foreach (var pawn in cell.GetThingList(Vehicle.Map).OfType<Pawn>())
                {
                    list.Add(pawn);
                }
            }
            return list.Distinct().ToList();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Vehicle.Faction == Faction.OfPlayer)
            {
                var pawnLoad = Props.loadCommand.GetCommand();
                pawnLoad.action = () =>
                {
                    var pawns = GetPawnsInPickupCells().Where(x => x != Vehicle).OrderBy(LoadOrder).ToList();
                    var passengerHandler = Vehicle.handlers.Find(x => x.role.handlingTypes == HandlingType.None);
                    foreach (var pawn in pawns.ToList())
                    {
                        if (Props.takePawnsOfAnyFaction || pawn.Faction == Vehicle.Faction)
                        {
                            if (compFlightMode != null && compFlightMode.InAir && pawn.Position.Roofed(pawn.Map))
                            {
                                continue;
                            }
                            if (Props.maxPawnMass.HasValue && pawn.GetMass() > Props.maxPawnMass.Value)
                            {
                                continue;
                            }

                            if (pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid)
                            {
                                if (passengerHandler.AreSlotsAvailable)
                                {
                                    pawn.DeSpawn();
                                    passengerHandler.GetDirectlyHeldThings().TryAddOrTransfer(pawn);
                                }
                            }
                            else if (pawn is VehiclePawn || pawn.RaceProps.Animal)
                            {
                                int massAvailable = Mathf.RoundToInt(Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity)
                                    - MassUtility.GearAndInventoryMass(Vehicle));
                                if (massAvailable >= pawn.GetMass())
                                {
                                    pawn.DeSpawn();
                                    Vehicle.inventory.TryAddItemNotForSale(pawn);
                                }
                            }
                        }
                    }
                };
                yield return pawnLoad;
            }
            if (Vehicle.inventory.innerContainer.OfType<VehiclePawn>().Any())
            {
                var pawnUnload = Props.unloadCommand.GetCommand();
                pawnUnload.action = () =>
                {
                    var vehicles = Vehicle.inventory.innerContainer.OfType<VehiclePawn>().ToList();
                    var floatList = new List<FloatMenuOption>();
                    foreach (var vehicle in vehicles)
                    {
                        floatList.Add(new FloatMenuOption(vehicle.LabelCap, delegate
                        {
                            Vehicle.inventory.innerContainer.TryDrop(vehicle, ThingPlaceMode.Near, out _);
                            if (compFlightMode != null && compFlightMode.InAir)
                            {
                                var otherComp = vehicle.TryGetComp<CompFlightMode>();
                                if (otherComp != null)
                                {
                                    otherComp.SetFlightMode(true);
                                    otherComp.SetTarget(Vehicle.Position);
                                    otherComp.takeoffProgress = 1f;
                                }
                                else
                                {
                                    var landingThrusters = vehicle.GetComp<CompLandingThrusters>();
                                    if (landingThrusters is null)
                                    {
                                        var damageAmount = vehicle.GetMass() * vehicle.BodySize;
                                        vehicle.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damageAmount));
                                    }
                                }
                            }
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                };
                yield return pawnUnload;
            }
        }

        private int LoadOrder(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike)
            {
                return 4;
            }
            else if (pawn is VehiclePawn)
            {
                return 2;
            }
            else if (pawn.RaceProps.IsMechanoid)
            {
                return 3;
            }
            return 1;
        }
    }
}
