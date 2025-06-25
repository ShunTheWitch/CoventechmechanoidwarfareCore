using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace universalflight
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
        public CompProperties_PawnDeployment Props => props as CompProperties_PawnDeployment;

        public Pawn Pawn => parent as Pawn;

        public CompFlightMode compFlightMode;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compFlightMode = this.parent.GetComp<CompFlightMode>();
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Pawn.Map != null)
            {
                GenDraw.DrawFieldEdges(GetPickupCells());
            }
        }

        public List<IntVec3> GetPickupCells()
        {
            var list = new List<IntVec3>();
            var cellRect = GenAdj.OccupiedRect(Pawn.Position, Pawn.Rotation, Props.pickupAreaSize);
            return cellRect.Where(x => x.InBounds(Pawn.Map)).ToList();
        }

        public List<Pawn> GetPawnsInPickupCells()
        {
            var list = new List<Pawn>();
            foreach (var cell in GetPickupCells())
            {
                foreach (var pawn in cell.GetThingList(Pawn.Map).OfType<Pawn>())
                {
                    list.Add(pawn);
                }
            }
            return list.Distinct().ToList();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Pawn.Faction == Faction.OfPlayer)
            {
                var pawnLoad = Props.loadCommand.GetCommand();
                pawnLoad.action = () =>
                {
                    var pawns = GetPawnsInPickupCells().Where(x => x != Pawn).OrderBy(LoadOrder).ToList();
                    foreach (var pawn in pawns.ToList())
                    {
                        if (Props.takePawnsOfAnyFaction || pawn.Faction == Pawn.Faction)
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
                                if (compFlightMode.AreSlotsAvailable(pawn))
                                {
                                    compFlightMode.TryAddOrTransfer(pawn);
                                }
                            }
                            else if (pawn is Pawn || pawn.RaceProps.Animal)
                            {
                                int massAvailable = Mathf.RoundToInt(Pawn.GetStatValue(StatDefOf.CarryingCapacity)
                                    - MassUtility.GearAndInventoryMass(Pawn));
                                if (massAvailable >= pawn.GetMass())
                                {
                                    pawn.DeSpawn();
                                    Pawn.inventory.TryAddItemNotForSale(pawn);
                                }
                            }
                        }
                    }
                };
                yield return pawnLoad;
            }
            if (Pawn.inventory.innerContainer.OfType<Pawn>().Any())
            {
                var pawnUnload = Props.unloadCommand.GetCommand();
                pawnUnload.action = () =>
                {
                    var vehicles = Pawn.inventory.innerContainer.OfType<Pawn>().ToList();
                    var floatList = new List<FloatMenuOption>();
                    foreach (var pawn in vehicles)
                    {
                        floatList.Add(new FloatMenuOption(pawn.LabelCap, delegate
                        {
                            Pawn.inventory.innerContainer.TryDrop(pawn, ThingPlaceMode.Near, out _);
                            if (compFlightMode != null && compFlightMode.InAir)
                            {
                                var otherComp = pawn.TryGetComp<CompFlightMode>();
                                if (otherComp != null)
                                {
                                    otherComp.SetFlightMode(true);
                                    otherComp.SetTarget(Pawn.Position);
                                    otherComp.takeoffProgress = 1f;
                                }
                                else
                                {
                                    var landingThrusters = pawn.GetComp<CompLandingThrusters>();
                                    if (landingThrusters is null)
                                    {
                                        var damageAmount = pawn.GetMass() * pawn.BodySize;
                                        pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, damageAmount));
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
            else if (pawn is Pawn)
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
