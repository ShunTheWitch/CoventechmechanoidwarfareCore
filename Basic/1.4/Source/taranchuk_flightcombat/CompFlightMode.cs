using RimWorld;
using SmashTools;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    public class CompProperties_FlightMode : VehicleCompProperties
    {
        public FlightCommands flightCommands;
        public int takeoffTicks;
        public int landingTicks;
        public bool moveWhileTakingOff;
        public List<TerrainAffordanceDef> runwayTerrainRequirements;
        public float flightSpeedPerTick;
        public float flightSpeedTurningPerTick;
        public float turnAnglePerTick;
        public float turnAngleCirclingPerTick;
        public float distanceFromTargetToStartTurningCircleMode;
        public float distanceFromTargetToStartTurningChaseMode;
        public float fuelConsumptionPerTick;
        public List<BombOption> bombOptions;
        public GraphicDataRGB flightGraphicData;
        public List<FlightFleckData> flightFlecks;
        public List<FlightFleckData> hoverFlecks;
        public List<FlightFleckData> takeoffFlecks;
        public List<FlightFleckData> landingFlecks;
        public FleckDef waypointFleck;
        public AISettings AISettings;
        public CompProperties_FlightMode()
        {
            this.compClass = typeof(CompFlightMode);
        }
    }

    [HotSwappable]
    public class CompFlightMode : VehicleComp, IMaterialCacheTarget
    {
        private FlightMode flightMode;
        private bool Flying => flightMode != FlightMode.Off;
        private LocalTargetInfo target;
        private LocalTargetInfo targetToFace = LocalTargetInfo.Invalid;
        private LocalTargetInfo targetToChase = LocalTargetInfo.Invalid;

        private LocalTargetInfo initialTarget = LocalTargetInfo.Invalid;
        private int bombardmentOptionInd;
        private int lastBombardmentTick;
        private int? tickToStartFiring;
        public float takeoffProgress;
        private bool TakingOff => flightMode != FlightMode.Off && takeoffProgress < 1f;
        private bool Hovering => flightMode == FlightMode.Hover;
        private bool Landing => flightMode == FlightMode.Off && takeoffProgress > 0f;
        public bool InAir => Vehicle.Spawned && (Flying || TakingOff || Landing);
        public Rot4 FlightRotation => Rot4.North;
        public float FlightAngleOffset => -90;
        public bool InAIMode => Props.AISettings != null && Vehicle.Faction != Faction.OfPlayer;
        private float curAngleInt;
        public float CurAngle
        {
            get
            {
                return curAngleInt;
            }
            set
            {
                var newValue = AngleAdjusted(value);
                curAngleInt = newValue;
            }
        }

        public float AngleAdjusted(float angle)
        {
            return angle.ClampAndWrap(0, 360);
        }

        public Vector3 curPosition;
        private bool? clockwiseTurn;
        private bool continueRotating;

        private int bombingRunCount;
        private int bombingCooldownTicks;

        public Graphic_Vehicle cachedFlightGraphic;

        public Graphic_Vehicle FlightGraphic
        {
            get
            {
                if (cachedFlightGraphic == null)
                {
                    cachedFlightGraphic = CreateFlightGraphic(this, Props.flightGraphicData);
                }
                if (Flying)
                {
                    var x = Mathf.Lerp(Vehicle.DrawSize.x, Props.flightGraphicData.drawSize.x, takeoffProgress);
                    var y = Mathf.Lerp(Vehicle.DrawSize.y, Props.flightGraphicData.drawSize.y, takeoffProgress);
                    cachedFlightGraphic.drawSize = new Vector2(x, y);
                }
                else if (Landing)
                {
                    var x = Mathf.Lerp(Props.flightGraphicData.drawSize.x,Vehicle.DrawSize.x, 1f - takeoffProgress);
                    var y = Mathf.Lerp(Props.flightGraphicData.drawSize.y, Vehicle.DrawSize.y, 1f - takeoffProgress);
                    cachedFlightGraphic.drawSize = new Vector2(x, y);
                }
                return cachedFlightGraphic;
            }
        }

        public CompProperties_FlightMode Props => base.props as CompProperties_FlightMode;

        public int MaterialCount => 8;

        public PatternDef PatternDef => Vehicle.PatternDef;

        public string Name => $"CompFlightMode_{Vehicle.ThingID}";

        private BombOption BombOption => Props.bombOptions.FirstOrDefault(x => Props.bombOptions.IndexOf(x) == bombardmentOptionInd);

        private bool initialized;

        public override bool IsThreat(IAttackTargetSearcher searcher)
        {
            return true;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && initialized is false)
            {
                initialized = true;
                if (Vehicle.Faction != Faction.OfPlayer && Props.AISettings != null)
                {
                    if (Props.AISettings.bomberSettings?.npcStock != null)
                    {
                        GenerateStock(Props.AISettings.bomberSettings.npcStock);
                    }
                    if (Props.AISettings.gunshipSettings?.npcStock != null)
                    {
                        GenerateStock(Props.AISettings.gunshipSettings.npcStock);
                    }
                }
            }

        }

        private void GenerateStock(List<ThingDefCountRangeClass> stock)
        {
            foreach (var entry in stock)
            {
                var stack = entry.countRange.RandomInRange;
                while (stack > 0)
                {
                    var thing = ThingMaker.MakeThing(entry.thingDef);
                    thing.stackCount = Mathf.Min(stack, thing.def.stackLimit);
                    stack -= thing.stackCount;
                    Vehicle.inventory.TryAddItemNotForSale(thing);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Vehicle.Faction == Faction.OfPlayer)
            {
                if (Props.flightCommands.flightMode != null)
                {
                    var flightModeCommand = Props.flightCommands.flightMode.GetCommand();
                    flightModeCommand.isActive = () => Flying;
                    flightModeCommand.toggleAction = () =>
                    {
                        SetFlightMode(this.flightMode != FlightMode.Flight);
                    };
                    if (Props.moveWhileTakingOff)
                    {
                        flightModeCommand.onHover = () =>
                        {
                            if ((TakingOff || flightMode == FlightMode.Off) && Landing is false)
                            {
                                DrawRunway(takingOff: true);
                            }
                            else
                            {
                                DrawRunway(takingOff: false);
                            }
                        };
                        if (flightMode == FlightMode.Off && Landing is false)
                        {
                            var blockingCells = GetBlockingCells(takingOff: true);
                            if (blockingCells.Any())
                            {
                                flightModeCommand.Disable("CVN_CannotTakeoff".Translate());
                            }
                        }
                        else if (InAir)
                        {
                            var blockingCells = GetBlockingCells(takingOff: false);
                            if (blockingCells.Any())
                            {
                                flightModeCommand.Disable("CVN_CannotLand".Translate());
                            }
                        }
                    }
                    else if (InAir && !CanLand())
                    {
                        flightModeCommand.Disable("CVN_CannotLandOnImpassableTiles".Translate());
                    }
                    yield return flightModeCommand;
                }

                if (Props.flightCommands.hoverMode != null)
                {
                    var hoverModeCommand = Props.flightCommands.hoverMode.GetCommand();
                    hoverModeCommand.isActive = () => this.flightMode == FlightMode.Hover;
                    hoverModeCommand.toggleAction = () =>
                    {
                        SetHoverMode(this.flightMode != FlightMode.Hover);
                    };
                    yield return hoverModeCommand;
                }

                if (Props.flightCommands.faceTarget != null)
                {
                    if (targetToFace.IsValid && Props.flightCommands.cancelFaceTarget != null)
                    {
                        var cancelFaceTargetCommand = Props.flightCommands.cancelFaceTarget.GetCommand();
                        cancelFaceTargetCommand.action = () =>
                        {
                            targetToFace = LocalTargetInfo.Invalid;
                        };
                        yield return cancelFaceTargetCommand;
                    }
                    else
                    {
                        var faceTargetCommand = Props.flightCommands.faceTarget.GetCommand();
                        faceTargetCommand.action = () =>
                        {
                            Find.Targeter.BeginTargeting(TargetingParamsForFacing, delegate (LocalTargetInfo x)
                            {
                                if (Hovering is false)
                                {
                                    SetHoverMode(true);
                                }
                                targetToFace = x;
                            });
                        };
                        yield return faceTargetCommand;
                    }

                }

                if (Props.flightCommands.chaseTarget != null)
                {
                    var chaseTargetcommand = Props.flightCommands.chaseTarget.GetCommand();
                    chaseTargetcommand.action = () =>
                    {
                        Find.Targeter.BeginTargeting(TargetingParamsForFacing, delegate (LocalTargetInfo x)
                        {
                            if (Hovering)
                            {
                                SetFlightMode(true);
                            }
                            targetToChase = x;
                        });
                    };
                    yield return chaseTargetcommand;
                }

                if (Props.bombOptions.NullOrEmpty() is false)
                {
                    var list = new List<FloatMenuOption>();
                    foreach (var option in Props.bombOptions)
                    {
                        list.Add(new FloatMenuOption(option.label, delegate
                        {
                            bombardmentOptionInd = Props.bombOptions.IndexOf(option);
                            tickToStartFiring = null;
                        }, itemIcon: ContentFinder<Texture2D>.Get(option.texPath), iconColor: Color.white));
                    }

                    BombOption bombOption = BombOption;
                    var bombCommand = new Command_Bomb(lastBombardmentTick, bombingCooldownTicks)
                    {
                        defaultLabel = bombOption.label,
                        icon = ContentFinder<Texture2D>.Get(bombOption.texPath),
                        action = () =>
                        {
                            TryDropBomb(bombOption);
                        },
                        bombOptions = list
                    };

                    if (BombCooldownActive())
                    {
                        bombCommand.Disable("CVN_OnCooldown".Translate(((lastBombardmentTick + bombingCooldownTicks) - Find.TickManager.TicksGame).ToStringTicksToPeriod()));
                    }
                }
            }
        }

        private bool BombCooldownActive()
        {
            return lastBombardmentTick > 0 && Find.TickManager.TicksGame - lastBombardmentTick < bombingCooldownTicks;
        }

        private TargetingParameters TargetingParamsForFacing => new TargetingParameters
        {
            canTargetPawns = true,
            canTargetLocations = true,
            validator = (TargetInfo x) => x.Thing != this.Vehicle
        };

        private void DrawRunway(bool takingOff)
        {
            var cells = GetRunwayCells(takingOff: takingOff);
            GenDraw.DrawFieldEdges(cells);
            var blockingCells = GetBlockingCells(takingOff: takingOff);
            GenDraw.DrawFieldEdges(blockingCells, Color.red);
        }

        private void FillGaps(List<IntVec3> cells)
        {
            foreach (var cell in cells.ToList()) 
            {
                var adjcells = GenAdj.CellsAdjacent8Way(cell, Vehicle.FullRotation, IntVec2.One);
                foreach (var adjcell in adjcells)
                {
                    if (cells.Contains(adjcell) is false)
                    {
                        cells.Add(adjcell);
                    }
                }
            }
        }

        public void SetFlightMode(bool flightMode)
        {
            if (flightMode)
            {
                SetTarget(Vehicle.vehiclePather.Moving ? Vehicle.vehiclePather.Destination : Vehicle.Position);
                curPosition = Vehicle.Drawer.DrawPos;
                CurAngle = Vehicle.FullRotation.AsAngle - FlightAngleOffset;
                if (Vehicle.vehiclePather.Moving)
                {
                    Vehicle.vehiclePather.StopDead();
                }
                UpdateVehicleAngleAndRotation();
                foreach (var turret in Vehicle.CompVehicleTurrets.turrets)
                {
                    turret.parentRotCached = this.FlightRotation;
                    turret.parentAngleCached = this.CurAngle;
                }
            }
            else
            {

                target = LocalTargetInfo.Invalid;
                Vehicle.FullRotation = Rot8.FromAngle(CurAngle);
                Vehicle.UpdateAngle();
            }
            targetToFace = targetToChase = LocalTargetInfo.Invalid;
            this.flightMode = flightMode ? FlightMode.Flight : FlightMode.Off;
        }

        public void SetHoverMode(bool hoverMode)
        {
            if (hoverMode)
            {
                SetTarget(Vehicle.Position);
                if (InAir is false)
                {
                    curPosition = Vehicle.Drawer.DrawPos;
                    CurAngle = Vehicle.FullRotation.AsAngle - FlightAngleOffset;
                }
                if (Vehicle.vehiclePather.Moving)
                {
                    Vehicle.vehiclePather.StopDead();
                }
            }
            targetToFace = targetToChase = LocalTargetInfo.Invalid;
            this.flightMode = hoverMode ? FlightMode.Hover : FlightMode.Flight; 
        }

        public void SetTarget(LocalTargetInfo targetInfo)
        {
            this.target = targetInfo;
            this.initialTarget = targetInfo.Cell;
            this.clockwiseTurn = null;
            targetToFace = targetToChase = LocalTargetInfo.Invalid;
            Vehicle.vehiclePather.PatherFailed();
        }

        public override void CompTick()
        {
            base.CompTick();
            //LogData("flightMode: " + flightMode);
            if (InAir)
            {
                if (Vehicle.CompFueledTravel != null && Props.fuelConsumptionPerTick > 0)
                {
                    if (Vehicle.CompFueledTravel.Fuel < Props.fuelConsumptionPerTick)
                    {
                        SetFlightMode(false);
                        return;
                    }
                    Vehicle.CompFueledTravel.ConsumeFuel(Props.fuelConsumptionPerTick);
                }
                if (initialTarget.IsValid && OccupiedRect().Contains(initialTarget.Cell))
                {
                    initialTarget = LocalTargetInfo.Invalid;
                }
                if (TakingOff)
                {
                    Takeoff();
                }
                else if (Landing)
                {
                    takeoffProgress = Mathf.Max(0, takeoffProgress - (1 / (float)Props.landingTicks));
                    if (Props.moveWhileTakingOff)
                    {
                        MoveFurther(Props.flightSpeedPerTick * takeoffProgress);
                    }
                    if (takeoffProgress == 0)
                    {
                        foreach (var turret in Vehicle.CompVehicleTurrets.turrets)
                        {
                            turret.parentRotCached = Vehicle.Rotation;
                            turret.parentAngleCached = Vehicle.Angle;
                        }
                    }
                }
                else if (Hovering)
                {
                    Hover();
                }
                else if (Flying)
                {
                    Flight(); 
                }

                if (InAIMode)
                {
                    AITick();
                }

                SpawnFlecks();

                var curPositionIntVec = curPosition.ToIntVec3();
                if (curPositionIntVec != Vehicle.Position)
                {
                    var occupiedRect = Vehicle.OccupiedRect();
                    if (occupiedRect.MovedBy(curPositionIntVec.ToIntVec2 - Vehicle.Position.ToIntVec2).InBounds(Vehicle.Map))
                    {
                        Vehicle.Position = curPositionIntVec;
                        Vehicle.vehiclePather.nextCell = curPositionIntVec;
                        bool shouldRefreshCosts = false;
                        foreach (var cell in occupiedRect.ExpandedBy(Vehicle.RotatedSize.x))
                        {
                            if (cell.InBounds(Vehicle.Map))
                            {
                                var grid = Vehicle.Map.thingGrid.thingGrid[Vehicle.Map.cellIndices.CellToIndex(cell)];
                                var vehicle = grid.OfType<VehiclePawn>().Where(x => x == this.Vehicle).FirstOrDefault();
                                if (vehicle != null && Vehicle.OccupiedRect().Contains(cell) is false)
                                {
                                    grid.Remove(vehicle);
                                    shouldRefreshCosts = true;
                                }
                            }
                        }

                        if (shouldRefreshCosts)
                        {
                            PathingHelper.RecalculateAllPerceivedPathCosts(Vehicle.Map);
                        }
                    }
                }
                UpdateVehicleAngleAndRotation();
            }
            //LogData("flightMode: " + flightMode);
        }

        private void AITick()
        {
            //foreach (var turret in Vehicle.CompVehicleTurrets.turrets)
            //{
            //    if (turret.HasAmmo is false)
            //    {
            //        ThingDef ammoType = Vehicle.inventory.innerContainer
            //            .FirstOrDefault(t => turret.turretDef.ammunition.Allows(t) 
            //            || turret.turretDef.ammunition.Allows(t.def.projectileWhenLoaded))?.def;
            //        if (ammoType != null)
            //        {
            //            turret.ReloadInternal(ammoType);
            //        }
            //    }
            //}

            var bomberSettings = Props.AISettings.bomberSettings;
            if (bomberSettings != null)
            {
                if (bomberSettings.maxBombRun.HasValue is false || bombingRunCount < bomberSettings.maxBombRun)
                {
                    if (BombCooldownActive())
                    {
                        return;
                    }
                    var curTarget = GetTarget();
                    if (curTarget.IsValid && curTarget.HasThing)
                    {
                        targetToChase = curTarget;
                        target = targetToFace = LocalTargetInfo.Invalid;
                    }
                    if (targetToChase.IsValid)
                    {
                        if (Vehicle.Position.DistanceTo(curTarget.Cell) < bomberSettings.minRangeToStartBombing)
                        {
                            var bombOption = Props.bombOptions.Where(x => bomberSettings.blacklistedBombs.Contains(x.projectile) is false).RandomElement();
                            if (TryDropBomb(bombOption))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            var gunshipSettings = Props.AISettings.gunshipSettings;
            if (gunshipSettings != null)
            {
                var curTarget = GetTarget();
                if (curTarget.IsValid && curTarget.HasThing)
                {
                    if (gunshipSettings.gunshipMode == GunshipMode.Circling)
                    {
                        target = curTarget;
                        targetToChase = targetToFace = LocalTargetInfo.Invalid;
                    }
                    else if (gunshipSettings.gunshipMode == GunshipMode.Chasing)
                    {
                        targetToChase = curTarget;
                        target = targetToFace = LocalTargetInfo.Invalid;
                    }
                }
            }
        }

        public LocalTargetInfo GetTarget()
        {
            var targets = (Vehicle.Map.attackTargetsCache.GetPotentialTargetsFor(Vehicle).Select(x => x.Thing)
                .Concat(Vehicle.Map.listerThings.ThingsInGroup(ThingRequestGroup.PowerTrader)));
            targets = targets.Distinct().Where(x => IsValidTarget(x)).OrderByDescending(x => CombatPoints(x)).Take(3);
            //Log.Message("Got first 3 targets: " + string.Join(", ", targets));
            //foreach (var target in targets)
            //{
            //    Log.Message(target + " - NearbyAreaCombatPoints(target): " + (NearbyAreaCombatPoints(target)));
            //}
            var result = targets.OrderByDescending(x => NearbyAreaCombatPoints(x)).FirstOrDefault();
            //Log.Message("Got result: " + result);
            return result;
        }

        private bool IsValidTarget(Thing x)
        {
            if (x.Position.GetRoof(Vehicle.Map) == RoofDefOf.RoofRockThick)
            {
                return false;
            }
            if (x is Pawn pawn && (pawn.Downed || pawn.Dead))
            {
                return false;
            }
            return x.HostileTo(Vehicle) || x.Faction != null && x.Faction.HostileTo(Vehicle.Faction);
        }

        private float NearbyAreaCombatPoints(Thing x)
        {
            return GenRadial.RadialDistinctThingsAround(x.Position, x.Map, 10, true).Where(x => IsValidTarget(x)).Sum(y => CombatPoints(y));
        }

        private float CombatPoints(Thing thing)
        {
            if (thing is Building_Turret)
            {
                return 5f;
            }
            else if (thing is VehiclePawn)
            {
                return 20f;
            }
            else if (thing is Pawn)
            {
                return 5f;
            }
            else if (thing.TryGetComp<CompPowerTrader>() != null)
            {
                return 5f;
            }
            return 0;
        }

        private bool TryDropBomb(BombOption bombOption)
        {
            if (HasStuffToBomb(bombOption))
            {
                foreach (var thingCost in bombOption.costList)
                {
                    var countToTake = thingCost.count;
                    foreach (var thing in Vehicle.inventory.GetDirectlyHeldThings().Where(x => x.def == thingCost.thingDef).ToList())
                    {
                        var thingToConsume = thing.SplitOff(Mathf.Min(countToTake, thing.stackCount));
                        countToTake -= thingToConsume.stackCount;
                        thingToConsume.Destroy();
                        if (countToTake <= 0)
                        {
                            break;
                        }
                    }
                }
                var bomb = (Projectile)GenSpawn.Spawn(bombOption.projectile, Vehicle.Position + IntVec3.North, Vehicle.Map);
                bomb.Launch(Vehicle, Vehicle.Position, Vehicle.Position, ProjectileHitFlags.IntendedTarget, equipment: Vehicle);
                lastBombardmentTick = Find.TickManager.TicksGame;
                bombingCooldownTicks = bombOption.cooldownTicks;
                return true;
            }
            return false;
        }

        private bool HasStuffToBomb(BombOption bombOption)
        {
            return bombOption.costList.All(thingCost => Vehicle.inventory.GetDirectlyHeldThings()
                            .Where(invThing => invThing.def == thingCost.thingDef).Sum(invThing => invThing.stackCount) >= thingCost.count);
        }

        private void SpawnFlecks()
        {
            if (TakingOff)
            {
                ProcessFlecks(Props.takeoffFlecks);
            }
            else if (Landing)
            {
                ProcessFlecks(Props.landingFlecks);
            }
            else if (Hovering)
            {
                ProcessFlecks(Props.hoverFlecks);
            }
            else if (Flying)
            {
                ProcessFlecks(Props.flightFlecks);
            }
        }

        private void ProcessFlecks(List<FlightFleckData> flecks)
        {
            if (flecks != null)
            {
                foreach (var fleck in flecks)
                {
                    if (Vehicle.IsHashIntervalTick(fleck.spawnTickRate))
                    {
                        SpawnFleck(fleck);
                    }
                }
            }
        }

        //[TweakValue("0test", -10, 10f)] public static float fleckX;
        //[TweakValue("0test", -10, 10f)] public static float fleckZ;

        public void SpawnFleck(FlightFleckData fleckData)
        {
            var fleckPos = fleckData.position.RotatedBy(CurAngle);
            var loc = curPosition - fleckPos;
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, Vehicle.Map, fleckData.fleck, fleckData.scale);
            dataStatic.velocityAngle = AngleAdjusted(CurAngle - fleckData.angleOffset);
            dataStatic.solidTimeOverride = fleckData.solidTime;
            if (fleckData.solidTimeScaleByTakeoffInverse)
            {
                dataStatic.solidTimeOverride *= 1f - takeoffProgress;
            }
            else if (fleckData.solidTimeScaleByTakeoff)
            {
                dataStatic.solidTimeOverride *= takeoffProgress;
            }
            dataStatic.velocitySpeed = fleckData.velocitySpeed;
            Vehicle.Map.flecks.CreateFleck(dataStatic);
        }

        private void UpdateVehicleAngleAndRotation()
        {
            Vehicle.Angle = AngleAdjusted(CurAngle + FlightAngleOffset);
            UpdateRotation();
        }

        public List<IntVec3> GetRunwayCells(bool takingOff)
        {
            var cells = new List<IntVec3>();
            var vehicle = Vehicle;
            var position = vehicle.Position;
            var rot = Vehicle.FullRotation;
            var angle = InAir ? AngleAdjusted(CurAngle + FlightAngleOffset) : rot.AsAngle;
            var distance = 0f;
            if (takingOff)
            {
                var curTakeoffProgress = takeoffProgress;
                while (curTakeoffProgress < 1)
                {
                    curTakeoffProgress = Mathf.Min(1, curTakeoffProgress + (1 / (float)Props.takeoffTicks));
                    distance += Props.flightSpeedPerTick * curTakeoffProgress;
                }
            }
            else
            {
                var curTakeoffProgress = takeoffProgress;
                while (curTakeoffProgress > 0)
                {
                    curTakeoffProgress = Mathf.Max(0, curTakeoffProgress - (1 / (float)Props.landingTicks));
                    distance += Props.flightSpeedPerTick * curTakeoffProgress;
                }
            }

            var width = vehicle.def.Size.x;
            var north = IntVec3.North.ToVector3();
            var rotInAir = Rot8.FromAngle(CurAngle + FlightAngleOffset);
            for (var i = 1; i <= width; i++)
            {
                var pos = CellOffset(position, i, width, angle);
                pos = AdjustPos(InAir ? rotInAir : rot, pos);
                cells.Add(pos);
                for (var j = 0; j < distance; j++)
                {
                    var offsetRotated = (north * j).RotatedBy(angle);
                    var cell = pos + offsetRotated.ToIntVec3();
                    cells.Add(cell);
                }
            }
            FillGaps(cells);
            return cells;
        }

        private IntVec3 AdjustPos(Rot8 rot, IntVec3 pos)
        {
            if (rot == Rot8.South || rot == Rot8.NorthEast || rot == Rot8.East)
            {
                pos.x += 1;
            }
            if (rot == Rot8.West || rot == Rot8.SouthWest)
            {
                pos.z -= 1;
            }
            if (rot == Rot8.SouthEast)
            {
                pos.x += 1;
                pos.z -= 1;
            }
            if (rot == Rot8.North)
            {
                pos.z += 1;
            }

            return pos;
        }

        public List<IntVec3> GetBlockingCells(bool takingOff)
        {
            var cells = new List<IntVec3>();
            foreach (var cell in GetRunwayCells(takingOff))
            {
                if (cell.InBounds(Vehicle.Map))
                {
                    if (Vehicle.Drivable(cell) is false || cell.GetThingList(Vehicle.Map).Any(x => x is Plant
                        && x.def.plant.IsTree || x is Building))
                    {
                        cells.Add(cell);
                    }
                    if (Props.runwayTerrainRequirements != null)
                    {
                        var terrain = cell.GetTerrain(Vehicle.Map);
                        if (Props.runwayTerrainRequirements.Intersect(terrain.affordances).Any() is false)
                        {
                            cells.Add(cell);
                        }
                    }
                }
            }
            return cells;
        }

        private void Hover()
        {
            if (initialTarget.IsValid)
            {
                bool rotated = RotateTowards(initialTarget.CenterVector3);
                MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
            }
            else if (target.IsValid && target.Cell != Vehicle.Position)
            {
                bool rotated = RotateTowards(target.CenterVector3);
                MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
            }
            else if (targetToFace.IsValid)
            {
                RotateTowards(targetToFace.CenterVector3);
            }
        }

        private void Flight()
        {
            if (initialTarget.IsValid)
            {
                bool rotated = RotateTowards(initialTarget.CenterVector3);
                MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
            }
            else if (targetToChase.IsValid)
            {
                var shouldRotate = targetToChase.Cell.DistanceTo(Vehicle.Position) > Props.distanceFromTargetToStartTurningChaseMode;
                if (shouldRotate)
                {
                    continueRotating = true;
                }
                if (shouldRotate || continueRotating)
                {
                    bool rotated = RotateTowards(targetToChase.CenterVector3);
                    if (rotated is false)
                    {
                        continueRotating = false;
                    }
                    MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                }
                else
                {
                    MoveFurther(Props.flightSpeedPerTick);
                }
            }
            else if (target.IsValid)
            {
                if (target.Cell.DistanceTo(Vehicle.Position) < Props.distanceFromTargetToStartTurningCircleMode)
                {
                    MoveFurther(Props.flightSpeedPerTick);
                }
                else
                {
                    RotatePerperticular(target.CenterVector3);
                    MoveFurther(Props.flightSpeedTurningPerTick);
                }
            }

        }

        private void Takeoff()
        {
            takeoffProgress = Mathf.Min(1, takeoffProgress + (1 / (float)Props.takeoffTicks));
            if (Props.moveWhileTakingOff)
            {
                MoveFurther(Props.flightSpeedPerTick * takeoffProgress);
            }
        }


        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            //LogData("flightMode: " + flightMode);
        }

        private bool CanLand() => OccupiedRect().All(x => Vehicle.Drivable(x));

        public void UpdateRotation()
        {
            if (Vehicle.rotationInt != FlightRotation)
            {
                Vehicle.rotationInt = FlightRotation;
            }
        }

        public List<IntVec3> OccupiedRect()
        {
            if (InAir)
            {
                var vehicle = Vehicle;
                var angle = AngleAdjusted(CurAngle + FlightAngleOffset);
                vehicle.rotationInt = Rot8.FromAngle(angle);
                var cells = Vehicle.VehicleRect().Cells.ToList();
                UpdateRotation();
                return cells;
            }
            return Vehicle.VehicleRect().Cells.ToList();
        }

        private IntVec3 CellOffset(IntVec3 pos, int ind, int width, float angle)
        {
            var halfWidth = width / 2f;
            var west = IntVec3.West.ToVector3();
            var offset = (west * halfWidth).RotatedBy(angle).ToIntVec3();
            var east = IntVec3.East.ToVector3();
            offset += (east * ind).RotatedBy(angle).ToIntVec3();
            return pos + offset;
        }

        private void MoveFurther(float speed)
        {
            var newTarget = curPosition + (Quaternion.AngleAxis(CurAngle, Vector3.up) * Vector3.forward).RotatedBy(FlightAngleOffset);
            MoveTowards(newTarget, speed);
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            var newPosition = Vector3.MoveTowards(Vehicle.DrawPos.Yto0(), target.Yto0(), speed);
            curPosition = new Vector3(newPosition.x, Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays), newPosition.z);
        }

        private void RotatePerperticular(Vector3 target)
        {
            float targetAngle = GetAngleFromTarget(target);
            var curAnglePerpendicular = AngleAdjusted(CurAngle - FlightAngleOffset);
            float diff = targetAngle - curAnglePerpendicular;
            if (diff > 0 ? diff > 180f : diff >= -180f)
            {
                if (clockwiseTurn is null)
                {
                    clockwiseTurn = true;
                }
                else if (clockwiseTurn is false)
                {
                    return;
                }
                CurAngle -= Props.turnAngleCirclingPerTick;
            }
            else
            {
                if (clockwiseTurn is null)
                {
                    clockwiseTurn = false;
                }
                else if (clockwiseTurn is true)
                {
                    return;
                }
                CurAngle += Props.turnAngleCirclingPerTick;
            }
        }

        private bool RotateTowards(Vector3 target)
        {
            float targetAngle = GetAngleFromTarget(target);
            if (new FloatRange(targetAngle - Props.turnAnglePerTick, targetAngle + Props.turnAnglePerTick).Includes(CurAngle))
            {
                return false;
            }
            float diff = targetAngle - CurAngle;
            if (diff > 0 ? diff > 180f : diff >= -180f)
            {
                CurAngle -= Props.turnAnglePerTick;
            }
            else
            {
                CurAngle += Props.turnAnglePerTick;
            }
            return true;
        }

        private float GetAngleFromTarget(Vector3 target)
        {
            var targetAngle = (curPosition.Yto0() - target.Yto0()).AngleFlat() + FlightAngleOffset;
            return AngleAdjusted(targetAngle);
        }

        private void LogData(string prefix)
        {
            Log.Message(this.Vehicle + " - " + prefix + " - Vehicle.Position: " + Vehicle.Position + " - takeoffProgress: " + takeoffProgress 
                + " - IsFlying: " + Flying + " - IsTakingOff: " + TakingOff + " - IsDescending: " + Landing
                + " - CurAngle: " + CurAngle + " - Vehicle.Angle: " + Vehicle.Angle
                + " - FullRotation: " + Vehicle.FullRotation.ToStringNamed() + " - Rotation: " + Vehicle.Rotation.ToStringHuman()
                + " - initialTarget: " + initialTarget + " - target: " + target + " - targetToFace: " + targetToFace);
            Log.ResetMessageCount();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            DestroyFlightGraphic();
            foreach (var cell in previousMap.AllCells)
            {
                var grid = previousMap.thingGrid.thingGrid[previousMap.cellIndices.CellToIndex(cell)];
                var vehicle = grid.OfType<VehiclePawn>().Where(x => x == this.Vehicle).FirstOrDefault();
                if (vehicle != null)
                {
                    grid.Remove(vehicle);
                }
            }
            PathingHelper.RecalculateAllPerceivedPathCosts(previousMap);
        }

        private void DestroyFlightGraphic()
        {
            RGBMaterialPool.Release(this);
            cachedFlightGraphic = null;
        }

        private Graphic_Vehicle CreateFlightGraphic(IMaterialCacheTarget cacheTarget, GraphicDataRGB copyGraphicData)
        {
            var graphicData = new GraphicDataRGB();
            graphicData.CopyFrom(copyGraphicData);
            Graphic_Vehicle graphic;
            if ((graphicData.shaderType.Shader.SupportsMaskTex() || graphicData.shaderType.Shader.SupportsRGBMaskTex()))
            {

            }
            if (Vehicle.patternData != null)
            {
                graphicData.CopyDrawData(Vehicle.patternData);
            }
            else
            {
                graphicData.CopyDrawData(copyGraphicData);
            }
            if (graphicData.shaderType != null && graphicData.shaderType.Shader.SupportsRGBMaskTex())
            {
                RGBMaterialPool.CacheMaterialsFor(cacheTarget);
                graphicData.Init(cacheTarget);
                graphic = graphicData.Graphic as Graphic_Vehicle;
                RGBMaterialPool.SetProperties(cacheTarget, graphicData, graphic.TexAt, graphic.MaskAt);
            }
            else
            {
                graphic = ((GraphicData)graphicData).Graphic as Graphic_Vehicle;
            }
            return graphic;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref flightMode, "flightMode");
            Scribe_Values.Look(ref curPosition, "curPosition");
            Scribe_Values.Look(ref curAngleInt, "curAngle");
            Scribe_TargetInfo.Look(ref target, "target");
            Scribe_TargetInfo.Look(ref targetToFace, "targetToFace", LocalTargetInfo.Invalid);
            Scribe_TargetInfo.Look(ref targetToChase, "targetToChase", LocalTargetInfo.Invalid);
            Scribe_TargetInfo.Look(ref initialTarget, "initialTarget", LocalTargetInfo.Invalid);
            Scribe_Values.Look(ref clockwiseTurn, "clockwiseTurn");
            Scribe_Values.Look(ref continueRotating, "continueRotating");
            Scribe_Values.Look(ref takeoffProgress, "takeoffProgress");
            Scribe_Values.Look(ref bombardmentOptionInd, "bombardmentOptionInd");
            Scribe_Values.Look(ref lastBombardmentTick, "lastBombardmentTick");
            Scribe_Values.Look(ref tickToStartFiring, "tickToStartFiring");
            Scribe_Values.Look(ref bombingRunCount, "bombingRunCount");
            Scribe_Values.Look(ref bombingCooldownTicks, "bombingCooldownTicks");
            Scribe_Values.Look(ref initialized, "initialized");
        }
    }
}
