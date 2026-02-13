using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    public class CompProperties_FlightMode : VehicleCompProperties
    {
        public FlightCommands flightCommands;
        public int takeoffTicks;
        public int landingTicks;
        public bool moveWhileTakingOff = true;
        public bool moveWhileLanding = true;
        public bool canFlyInSpace;
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
        public float? damageMultiplierFromNonAntiAirProjectiles;
        public CompProperties_FlightMode()
        {
            this.compClass = typeof(CompFlightMode);
        }
    }

    public enum LandingStage
    {
        Inactive, GotoInitialSpot, GotoRunwayStartSpot, GotoLanding
    }

    [HotSwappable]
    public class CompFlightMode : VehicleComp, IMaterialCacheTarget
    {
        public VehicleArrivalAction arrivalAction;
        public List<FlightNode> flightPath;
        public bool orderRecon;

        public FlightMode flightMode;
        private bool Flying => flightMode != FlightMode.Off;
        private bool GoingToWorld => arrivalAction != null;

        private LocalTargetInfo target;
        private LocalTargetInfo targetToFace = LocalTargetInfo.Invalid;
        private LocalTargetInfo targetToChase = LocalTargetInfo.Invalid;
        private LocalTargetInfo landingSpot;
        private LocalTargetInfo runwayStartingSpot;
        private Vector3? targetForRunway;
        private LandingStage landingStage;

        private LocalTargetInfo initialTarget = LocalTargetInfo.Invalid;
        private int bombardmentOptionInd;
        private int lastBombardmentTick;
        private int? tickToStartFiring;
        public float takeoffProgress;
        public bool shouldCrash;
        private bool TakingOff => flightMode != FlightMode.Off && takeoffProgress < 1f && runwayStartingSpot.IsValid is false;
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

        public bool CanFly
        {
            get
            {
                //var deploying = Vehicle.Deploying is false;
                //var canMove = Vehicle.CanMoveFinal;
                //var canMove2 = Vehicle.CanMove;
                //var stat = Vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed);
                //var moveSpeed = Vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) > 0.1f;
                //var perm = Vehicle.MovementPermissions > VehiclePermissions.NotAllowed 
                //    && Vehicle.movementStatus == VehicleMovementStatus.Online;
                //var flightSpeed = Vehicle.GetStatValue(VehicleStatDefOf.FlightSpeed) >
                //    Vehicle.VehicleDef.GetStatValueAbstract(VehicleStatDefOf.FlightSpeed) / 2f;
                //var flightControl = Vehicle.GetStatValue(VehicleStatDefOf.FlightControl) >
                //    Vehicle.VehicleDef.GetStatValueAbstract(VehicleStatDefOf.FlightControl) / 2f;
                //Log.Message("deploying: " + deploying + " - canMove: " + canMove + " - canMove2: " + canMove2
                //    + " - moveSpeed: " + moveSpeed + " - perm: " + perm + " - stat: " + stat
                //    + " - flightSpeed: " + flightSpeed
                //    + " - flightControl: " + flightControl);
                //return deploying && canMove && flightSpeed && flightControl;
                return Vehicle.CompVehicleTurrets.Deploying is false && Vehicle.CanMoveFinal
                    && Vehicle.GetStatValue(VehicleStatDefOf.FlightSpeed) >
                    Vehicle.VehicleDef.GetStatValueAbstract(VehicleStatDefOf.FlightSpeed) / 2f
                    && Vehicle.GetStatValue(VehicleStatDefOf.FlightControl) >
                    Vehicle.VehicleDef.GetStatValueAbstract(VehicleStatDefOf.FlightControl) / 2f;
            }
        }

        public float AngleAdjusted(float angle)
        {
            return angle.ClampAngle();
        }

        public Vector2 GetCurrentDrawSize()
        {
            if (!InAir || Props.flightGraphicData == null)
            {
                return Vehicle.DrawSize;
            }

            var originalSize = Vehicle.VehicleDef.graphicData.drawSize;
            var flightSize = Props.flightGraphicData.drawSize;

            if (Flying)
            {
                var x = Mathf.Lerp(originalSize.x, flightSize.x, takeoffProgress);
                var y = Mathf.Lerp(originalSize.y, flightSize.y, takeoffProgress);
                return new Vector2(x, y);
            }
            else if (Landing)
            {
                var x = Mathf.Lerp(flightSize.x, originalSize.x, 1f - takeoffProgress);
                var y = Mathf.Lerp(flightSize.y, originalSize.y, 1f - takeoffProgress);
                return new Vector2(x, y);
            }
            else
            {
                return flightSize;
            }
        }

        public Vector2 GetScaleFactors()
        {
            var originalSize = Vehicle.VehicleDef.graphicData.drawSize;
            var currentSize = GetCurrentDrawSize();
            return new Vector2(currentSize.x / originalSize.x, currentSize.y / originalSize.y);
        }

        public float BaseFlightSpeed => Props.flightSpeedPerTick;
        public Vector3 curPosition;
        private bool? clockwiseTurn;
        private bool continueRotating;

        private int bombingRunCount;
        private int bombingCooldownTicks;

        private Graphic_Vehicle flightGraphic;

        public Graphic_Vehicle FlightGraphic
        {
            get
            {
                var currentDrawSize = GetCurrentDrawSize();
                
                if (flightGraphic == null)
                {
                    if (UnityData.IsInMainThread)
                    {
                        flightGraphic = CreateFlightGraphic(this, Props.flightGraphicData);
                        if (flightGraphic != null)
                        {
                            flightGraphic.drawSize = currentDrawSize;
                        }
                    }
                }
                else
                {
                    flightGraphic.drawSize = currentDrawSize;
                }

                return flightGraphic;
            }
        }

        public CompProperties_FlightMode Props => base.props as CompProperties_FlightMode;

        public int MaterialCount => 8;

        public PatternDef PatternDef => Vehicle.PatternDef;

        public string Name => $"CompFlightMode_{Vehicle.ThingID}";

        public MaterialPropertyBlock PropertyBlock { get; private set; } = new MaterialPropertyBlock();
        
        private Material shadowMaterial;

        private BombOption BombOption => Props.bombOptions.FirstOrDefault(x => Props.bombOptions.IndexOf(x) == bombardmentOptionInd);

        private bool initialized;

        private List<VehicleTurret> VehicleTurrets
        {
            get
            {
                var turrets = Vehicle.CompVehicleTurrets?.turrets;
                if (turrets is null)
                {
                    return new List<VehicleTurret>();
                }
                return turrets;
            }
        }
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

        public override void PostDraw()
        {
            base.PostDraw();
            DrawShadow();
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
            if (Vehicle.Faction == Faction.OfPlayer && Vehicle.Drafted)
            {
                foreach (var g in GetAircraftGizmos())
                {
                    if (CanFly is false)
                    {
                        g.Disable("VF_VehicleUnableToMove".Translate(Vehicle));
                    }
                    yield return g;
                }
            }

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Set custom angle: " + CurAngle,
                    action = () =>
                    {
                        Find.WindowStack.Add(new Dialog_Slider("Set angle: " + CurAngle, 0, 360, delegate (int value)
                        {
                            CurAngle = value;
                        }));
                    }
                };

                if (InAir && !shouldCrash)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: Set to crash",
                        action = () =>
                        {
                            SetToCrash();
                        }
                    };
                }
            }
        }

        private IEnumerable<Gizmo> GetAircraftGizmos()
        {
            if (Flying && Props.flightCommands.landOnGround != null)
            {
                var landOnGround = Props.flightCommands.landOnGround.GetCommand();
                landOnGround.defaultLabel = "WIP: " + landOnGround.defaultLabel;
                landOnGround.action = () =>
                {
                    Find.Targeter.BeginTargeting(TargetingParamsForLanding, delegate (LocalTargetInfo landingSpot)
                    {
                        Find.Targeter.BeginTargeting(TargetingParamsForLanding, delegate (LocalTargetInfo runwayStartingSpot)
                        {
                            var runwayCells = GetRunwayCells(takingOff: false, runwayStartingSpot.Cell, (landingSpot.Cell - runwayStartingSpot.Cell).AngleFlat);
                            var blockingCells = GetBlockingCells(runwayCells);
                            if (blockingCells.Any())
                            {
                                Messages.Message("CVN_CannotLand".Translate(), MessageTypeDefOf.RejectInput);
                            }
                            else
                            {
                                this.landingSpot = landingSpot;
                                this.runwayStartingSpot = runwayStartingSpot;
                                if (flightMode != FlightMode.Flight)
                                {
                                    this.SetFlightMode(true);
                                }
                            }
                        }, highlightAction: delegate (LocalTargetInfo x)
                        {
                            if (x.IsValid && x.Cell != landingSpot.Cell)
                            {
                                var runwayCells = GetRunwayCells(takingOff: false, x.Cell, (landingSpot.Cell - x.Cell).AngleFlat);
                                GenDraw.DrawFieldEdges(runwayCells);
                                var blockingCells = GetBlockingCells(runwayCells);
                                GenDraw.DrawFieldEdges(blockingCells, Color.red);
                            }
                        }, null);
                    });
                };
                yield return landOnGround;
            }

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
                        var runwayCells = GetRunwayCells(takingOff: true);
                        var blockingCells = GetBlockingCells(runwayCells);
                        if (blockingCells.Any())
                        {
                            flightModeCommand.Disable("CVN_CannotTakeoff".Translate());
                        }
                    }
                    else if (InAir)
                    {
                        var runwayCells = GetRunwayCells(takingOff: false);
                        var blockingCells = GetBlockingCells(runwayCells);
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
                if (Props.canFlyInSpace is false && parent.Map.Biome.inVacuum)
                {
                    flightModeCommand.Disable("CVN_CannotTakeoffInSpace".Translate());
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

            if (Flying && Props.bombOptions.NullOrEmpty() is false)
            {
                var list = new List<FloatMenuOption>();
                foreach (var option in Props.bombOptions)
                {
                    list.Add(new FloatMenuOption(option.label, delegate
                    {
                        bombardmentOptionInd = Props.bombOptions.IndexOf(option);
                        tickToStartFiring = null;
                    }, iconTex: ContentFinder<Texture2D>.Get(option.texPath), iconColor: Color.white));
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
                yield return bombCommand;
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

        private TargetingParameters TargetingParamsForLanding => new TargetingParameters
        {
            canTargetPawns = false,
            canTargetLocations = true,
            validator = (TargetInfo x) => Vehicle.Drivable(x.Cell)
        };

        private void DrawRunway(bool takingOff)
        {
            var runwayCells = GetRunwayCells(takingOff: takingOff);
            GenDraw.DrawFieldEdges(runwayCells);
            var blockingCells = GetBlockingCells(runwayCells);
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
                curPosition = Vehicle.DrawTracker.DrawPos;
                CurAngle = Vehicle.FullRotation.AsAngle - FlightAngleOffset;
                if (Vehicle.vehiclePather.Moving)
                {
                    Vehicle.vehiclePather.StopDead();
                }
                UpdateVehicleAngleAndRotation();
                foreach (var turret in VehicleTurrets)
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
            ResetFlightData();
            this.flightMode = flightMode ? FlightMode.Flight : FlightMode.Off;
        }

        public void SetHoverMode(bool hoverMode)
        {
            if (hoverMode)
            {
                SetTarget(Vehicle.Position);
                if (InAir is false)
                {
                    curPosition = Vehicle.DrawTracker.DrawPos;
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
            ResetFlightData();
            if (Vehicle.jobs.curDriver is JobDriver_Goto)
            {
                Vehicle.vehiclePather.PatherFailed();
            }
        }

        private void ResetFlightData()
        {
            this.clockwiseTurn = null;
            targetToChase = LocalTargetInfo.Invalid;
            runwayStartingSpot = LocalTargetInfo.Invalid;
            landingSpot = LocalTargetInfo.Invalid;
            targetForRunway = null;
            landingStage = LandingStage.Inactive;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (InAir)
            {
                if (Vehicle.DrawTracker?.renderer is IParallelRenderer renderer)
                {
                    renderer.IsDirty = true;
                }

                if (Vehicle.CompFueledTravel != null && Props.fuelConsumptionPerTick > 0)
                {
                    if (Vehicle.CompFueledTravel.Fuel < Props.fuelConsumptionPerTick)
                    {
                        if (shouldCrash is false)
                        {
                            SetToCrash();
                        }
                    }
                    else
                    {
                        Vehicle.CompFueledTravel.ConsumeFuel(Props.fuelConsumptionPerTick);
                    }
                }

                if (CanFly is false && shouldCrash is false)
                {
                    SetToCrash();
                }

                if (TakingOff is false && initialTarget.IsValid && curPosition.ToIntVec3().InBounds(Vehicle.Map)
                    && OccupiedRect().Contains(initialTarget.Cell))
                {
                    initialTarget = LocalTargetInfo.Invalid;
                }

                if (InAIMode && !GoingToWorld)
                {
                    AITick();
                }

                if (TakingOff)
                {
                    Takeoff();
                }
                else if (Landing)
                {
                    var takeoffOffset = (1 / (float)Props.landingTicks);
                    if (shouldCrash)
                    {
                        takeoffOffset *= 2f;
                    }
                    takeoffProgress = Mathf.Max(0, takeoffProgress - takeoffOffset);
                    if (Props.moveWhileLanding)
                    {
                        if (shouldCrash)
                        {
                            MoveFurther(Props.flightSpeedPerTick);
                        }
                        else
                        {
                            MoveFurther(Props.flightSpeedPerTick * takeoffProgress);
                        }
                    }
                    if (takeoffProgress == 0)
                    {
                        FlightEnd();
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

                ProcessRotors();
                SpawnFlecks();

                var curPositionIntVec = curPosition.ToIntVec3();
                if (curPositionIntVec != Vehicle.Position && curPositionIntVec.InBounds(Vehicle.Map))
                {
                    var occupiedRect = Vehicle.OccupiedRect();
                    if (occupiedRect.MovedBy(curPositionIntVec.ToIntVec2 - Vehicle.Position.ToIntVec2).InBoundsLocal(Vehicle.Map))
                    {
                        try
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
                        catch
                        {

                        }
                    }
                    else if (GoingToWorld)
                    {
                        GotoWorld();
                        return;
                    }
                }
                UpdateVehicleAngleAndRotation();
            }
        }

        private void FlightEnd()
        {
            foreach (var turret in VehicleTurrets)
            {
                turret.parentRotCached = Vehicle.Rotation;
                turret.parentAngleCached = Vehicle.Angle;
            }
            if (shouldCrash)
            {
                shouldCrash = false;
                var damageAmount = (Vehicle.GetMass() + MassUtility.GearAndInventoryMass(Vehicle)) * 20f;
                var components = Vehicle.statHandler.components
                    .Where(x => x.props.depth == VehicleComponent.VehiclePartDepth.External).ToList();
                damageAmount /= components.Count;
                foreach (var component in components)
                {
                    component.TakeDamage(Vehicle, new DamageInfo(DamageDefOf.Blunt, damageAmount), ignoreArmor: true);
                }
            }
        }

        private void SetToCrash()
        {
            if (flightMode != FlightMode.Off)
            {
                SetFlightMode(false);
            }
            shouldCrash = true;
        }
        private void ProcessRotors()
        {
            var launcher = Vehicle.CompVehicleLauncher;
            if (launcher != null)
            {
                var props = launcher.Props.launchProtocol.LaunchProperties as PropellerProtocolProperties;
                if (props?.angularVelocityPropeller != null)
                {
                    var rotationRate = props.angularVelocityPropeller.Evaluate(takeoffProgress);
                    Vehicle.DrawTracker.overlayRenderer?.SetAcceleration(rotationRate);
                }
            }
        }

        private void GotoWorld()
        {
            var map = Vehicle.Map;
            Vehicle.Angle = 0;
            Vehicle.DeSpawn();
            if (Vehicle.Faction == Faction.OfPlayer)
            {
                Messages.Message("VF_AerialVehicleLeft".Translate(Vehicle.LabelShort), MessageTypeDefOf.PositiveEvent);
            }
            AerialVehicleInFlight aerialVehicle = AerialVehicleInFlight.Create(Vehicle, map.Tile);
            aerialVehicle.OrderFlyToTiles(new List<FlightNode>(flightPath), arrivalAction);
            if (orderRecon)
            {
                aerialVehicle.flightPath.ReconCircleAt(flightPath.LastOrDefault().tile);
            }

            Find.WorldPawns.PassToWorld(Vehicle);
            foreach (Pawn pawn in Vehicle.AllPawnsAboard)
            {
                if (!pawn.IsWorldPawn())
                {
                    Find.WorldPawns.PassToWorld(pawn);
                }
            }
            foreach (Thing thing in Vehicle.inventory.innerContainer)
            {
                if (thing is Pawn pawn && !pawn.IsWorldPawn())
                {
                    Find.WorldPawns.PassToWorld(pawn);
                }
            }
            Vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLeftMap].ExecuteEvents();
            arrivalAction = null;
            flightPath = null;
        }

        private void AITick()
        {
            foreach (var turret in VehicleTurrets)
            {
                if (turret.HasAmmo is false)
                {
                    ThingDef ammoType = Vehicle.inventory.innerContainer
                        .FirstOrDefault(t => turret.def.ammunition.Allows(t)
                        || turret.def.ammunition.Allows(t.def.projectileWhenLoaded))?.def;
                    if (ammoType != null)
                    {
                        turret.ReloadInternal(ammoType);
                    }
                }
            }

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
                        if (Vehicle.Position.DistanceTo(curTarget.Cell) < bomberSettings.distanceFromTarget)
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
                    else if (gunshipSettings.gunshipMode == GunshipMode.Hovering)
                    {
                        targetToFace = curTarget;
                        target = targetToChase = LocalTargetInfo.Invalid;
                    }
                }
            }
        }

        public LocalTargetInfo GetTarget()
        {
            var targets = Vehicle.Map.attackTargetsCache.GetPotentialTargetsFor(Vehicle).Select(x => x.Thing)
                .Concat(Vehicle.Map.listerThings.ThingsInGroup(ThingRequestGroup.PowerTrader));
            if (Vehicle.Faction != Faction.OfPlayer)
            {
            }

            targets = targets.Distinct().Where(x => IsValidTarget(x)).OrderByDescending(x => CombatPoints(x)).Take(3);
            var result = targets.OrderByDescending(x => NearbyAreaCombatPoints(x)).FirstOrDefault();
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

        public void SpawnFleck(FlightFleckData fleckData)
        {
            var fleckPos = fleckData.position.RotatedBy(CurAngle);
            var loc = curPosition - fleckPos;
            FleckCreationData data = FleckMaker.GetDataStatic(loc, Vehicle.Map, fleckData.fleck, fleckData.scale);
            if (fleckData.attachToVehicle)
            {
                data.link = new FleckAttachLink(Vehicle);
            }
            data.velocityAngle = AngleAdjusted(CurAngle - fleckData.angleOffset);
            data.solidTimeOverride = fleckData.solidTime;
            if (fleckData.solidTimeScaleByTakeoffInverse)
            {
                data.solidTimeOverride *= 1f - takeoffProgress;
            }
            else if (fleckData.solidTimeScaleByTakeoff)
            {
                data.solidTimeOverride *= takeoffProgress;
            }
            data.velocitySpeed = fleckData.velocitySpeed;
            Vehicle.Map.flecks.CreateFleck(data);
        }

        private void UpdateVehicleAngleAndRotation()
        {
            Vehicle.Angle = AngleAdjusted(CurAngle + FlightAngleOffset);
            if (Vehicle.Transform != null)
            {
                Vehicle.Transform.rotation = Vehicle.Angle;
            }

            UpdateRotation();
        }

        public List<IntVec3> GetRunwayCells(bool takingOff)
        {
            var position = Vehicle.Position;
            var rot = Vehicle.FullRotation;
            var angle = InAir ? AngleAdjusted(CurAngle + FlightAngleOffset) : rot.AsAngle;
            return GetRunwayCells(takingOff, position, angle);
        }

        private List<IntVec3> GetRunwayCells(bool takingOff, IntVec3 position, float angle)
        {
            var cells = new List<IntVec3>();
            var vehicle = Vehicle;
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
            var rotInAir = Rot8.FromAngle(angle);
            for (var i = 1; i <= width; i++)
            {
                var pos = CellOffset(position, i, width, angle);
                pos = AdjustPos(InAir ? rotInAir : Vehicle.FullRotation, pos);
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

        public List<IntVec3> GetBlockingCells(List<IntVec3> runwayCells)
        {
            var cells = new List<IntVec3>();
            foreach (var cell in runwayCells)
            {
                if (cell.InBounds(Vehicle.Map))
                {
                    var terrain = cell.GetTerrain(Vehicle.Map);
                    if (Props.canFlyInSpace && terrain == TerrainDefOf.Space)
                    {
                        continue;
                    }
                    if (Vehicle.Drivable(cell) is false || cell.GetThingList(Vehicle.Map).Any(x => x is Plant && x.def.plant.IsTree || x is Building))
                    {
                        cells.Add(cell);
                    }
                    if (Props.runwayTerrainRequirements != null)
                    {
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
                if (targetToFace.IsValid)
                {
                    bool rotated = RotateTowards(targetToFace.CenterVector3);
                    MoveTowards(initialTarget.CenterVector3, rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                }
                else
                {
                    bool rotated = RotateTowards(initialTarget.CenterVector3);
                    MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                }
            }
            else if (target.IsValid && target.Cell != Vehicle.Position)
            {
                if (targetToFace.IsValid)
                {
                    bool rotated = RotateTowards(targetToFace.CenterVector3);
                    MoveTowards(target.CenterVector3, rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                }
                else
                {
                    bool rotated = RotateTowards(target.CenterVector3);
                    MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                }
            }
            else if (targetToFace.IsValid)
            {
                bool rotated = RotateTowards(targetToFace.CenterVector3);
                if (InAIMode)
                {
                    var distance = targetToFace.Cell.DistanceTo(Vehicle.Position);
                    var baseSpeed = (rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                    var targetDistance = (Props.AISettings.gunshipSettings.distanceFromTarget + 5);
                    if (distance > Props.AISettings.gunshipSettings.distanceFromTarget)
                    {
                        var speedMult = Mathf.Min(1, distance / targetDistance);
                        var speed = baseSpeed * speedMult;
                        MoveFurther(speed, Props.AISettings.gunshipSettings.distanceFromTarget, targetToFace.CenterVector3);
                    }
                    else if (distance < Props.AISettings.gunshipSettings.distanceFromTarget - 1)
                    {
                        var speedMult = Mathf.Min(1, distance / targetDistance);
                        var speed = baseSpeed * speedMult;
                        MoveBack(speed);
                    }
                }
            }
        }

        private void Flight()
        {
            if (GoingToWorld)
            {
                MoveFurther(Props.flightSpeedPerTick);
            }
            else if (runwayStartingSpot.IsValid)
            {
                MoveToLandingSpot();
            }
            else if (initialTarget.IsValid)
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

        private void MoveToLandingSpot()
        {
            if (landingStage == LandingStage.Inactive)
            {
                if (targetForRunway is null)
                {
                    var newTarget = runwayStartingSpot.CenterVector3 + (Quaternion.AngleAxis(CurAngle, Vector3.up)
                        * (Vector3.forward * (Props.distanceFromTargetToStartTurningChaseMode * 2))).RotatedBy(FlightAngleOffset);
                    targetForRunway = newTarget;
                }
                bool rotated = RotateTowards(targetForRunway.Value);
                MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                if (rotated is false)
                {
                    landingStage = LandingStage.GotoInitialSpot;
                }
            }
            else
            {
                if (landingStage == LandingStage.GotoInitialSpot)
                {
                    bool shouldRotate = ShouldRotate(out bool? clockturn);
                    if (shouldRotate)
                    {
                        if (clockturn.Value)
                        {
                            CurAngle += Props.turnAngleCirclingPerTick;
                        }
                        else
                        {
                            CurAngle -= Props.turnAngleCirclingPerTick;
                        }
                        landingStage = LandingStage.GotoRunwayStartSpot;
                    }
                    MoveFurther(shouldRotate ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                }
                else
                {
                    if (landingStage == LandingStage.GotoRunwayStartSpot)
                    {
                        Vehicle.Map.debugDrawer.FlashCell(runwayStartingSpot.Cell);
                        Vehicle.Map.debugDrawer.FlashCell(landingSpot.Cell, 0.5f);
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                        bool rotated = RotateTowards(runwayStartingSpot.CenterVector3);
                        MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                        if (curPosition.ToIntVec3() == runwayStartingSpot.Cell)
                        {
                            landingStage = LandingStage.GotoLanding;
                        }
                    }
                    else if (landingStage == LandingStage.GotoLanding)
                    {
                        bool rotated = RotateTowards(landingSpot.CenterVector3);
                        MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
                        takeoffProgress = Mathf.Max(0, takeoffProgress - (1 / (float)Props.landingTicks));
                        Vehicle.Map.debugDrawer.FlashCell(runwayStartingSpot.Cell);
                        Vehicle.Map.debugDrawer.FlashCell(landingSpot.Cell, 0.5f);
                        if (takeoffProgress == 0)
                        {
                            SetFlightMode(false);
                            FlightEnd();
                        }
                    }
                }
            }
        }

        private bool ShouldRotate(out bool? clockturn)
        {
            clockturn = null;
            var speedPerTick = Props.flightSpeedPerTick;
            var dist = speedPerTick;
            var curAngle = CurAngle;
            Quaternion angleAxis = Quaternion.AngleAxis(curAngle, Vector3.up);
            var minAngle = float.MinValue;
            var targetAngle = AngleAdjusted((landingSpot.CenterVector3 - runwayStartingSpot.CenterVector3).AngleFlat() - FlightAngleOffset);
            while (true)
            {
                var newPosition = curPosition + (angleAxis * (Vector3.forward * dist)).RotatedBy(FlightAngleOffset);
                var newRunwayAngle = AngleAdjusted((newPosition.Yto0() - runwayStartingSpot.CenterVector3.Yto0()).AngleFlat());
                var newRunwayAngle2 = AngleAdjusted((newPosition.Yto0() - landingSpot.CenterVector3.Yto0()).AngleFlat());
                var minDiff = Mathf.Abs(newRunwayAngle - newRunwayAngle2);
                if (minAngle != float.MinValue && minDiff > minAngle && minAngle <= 1)
                {
                    clockturn = ClockWiseTurn(AngleAdjusted(newRunwayAngle + FlightAngleOffset));
                    if (clockturn is null)
                    {
                        return false;
                    }
                    else
                    {
                        var turnoffset = clockturn.Value ? Props.turnAnglePerTick : -Props.turnAnglePerTick;
                        var ticksPassedSimulated = (int)(dist / speedPerTick);
                        for (var i = 0; i < ticksPassedSimulated; i++)
                        {
                            curAngle = AngleAdjusted(curAngle + turnoffset);
                            bool angleInRange = AngleInRange(curAngle, AngleAdjusted(targetAngle - Props.turnAnglePerTick), AngleAdjusted(targetAngle + Props.turnAnglePerTick));
                            if (angleInRange)
                            {
                                Log.Message("curAngle: " + curAngle + " - targetAngle: " + targetAngle + " - i: " + i + " - ticksPassedSimulated: " + ticksPassedSimulated);
                            }
                            if (angleInRange && i >= ticksPassedSimulated - 3)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                minAngle = minDiff;
                dist += speedPerTick;
                if (dist > 1000)
                {
                    Log.Error("Broke cycle");
                    return false;
                }
            }
        }

        private bool AngleInRange(float angle, float lower, float upper)
        {
            return (angle - lower) % 360 <= (upper - lower) % 360;
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

        private void MoveFurther(float speed, float? minDistanceFromTarget = null, Vector3? target = null)
        {
            var newTarget = curPosition + (Quaternion.AngleAxis(CurAngle, Vector3.up) * Vector3.forward).RotatedBy(FlightAngleOffset);
            MoveTowards(newTarget, speed, minDistanceFromTarget, target);
        }

        private void MoveBack(float speed, float? minDistanceFromTarget = null, Vector3? target = null)
        {
            var newTarget = curPosition + (Quaternion.AngleAxis(CurAngle, Vector3.up) * Vector3.back).RotatedBy(FlightAngleOffset);
            MoveTowards(newTarget, speed, minDistanceFromTarget, target);
        }

        private void MoveTowards(Vector3 to, float speed, float? minDistanceFromTarget = null, Vector3? target = null)
        {
            var newPosition = Vector3.MoveTowards(Vehicle.DrawPos.Yto0(), to.Yto0(), speed);
            if (minDistanceFromTarget.HasValue && Vector3.Distance(newPosition.Yto0(), target.Value.Yto0()) < minDistanceFromTarget)
            {
                return;
            }
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
            return RotateTo(targetAngle);
        }

        private bool RotateTo(float targetAngle)
        {
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

        private bool? ClockWiseTurn(float targetAngle)
        {
            if (new FloatRange(targetAngle - Props.turnAnglePerTick, targetAngle + Props.turnAnglePerTick).Includes(CurAngle))
            {
                return null;
            }
            float diff = targetAngle - CurAngle;
            if (diff > 0 ? diff > 180f : diff >= -180f)
            {
                return false;
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


        private int drawThisFrame;
        public void DrawShadow()
        {
            if (InAir && shadowMaterial != null && FlightGraphic != null && drawThisFrame != Time.frameCount)
            {
                drawThisFrame = Time.frameCount;
                Vector3 drawPos = curPosition;
                drawPos.y = Vehicle.DrawPos.y - 1;
                drawPos.z -= 3f * takeoffProgress;

                Vector2 size = FlightGraphic.drawSize;
                Vector3 scale = new Vector3(size.x, 1f, size.y);

                float visualAngle = AngleAdjusted(CurAngle + FlightAngleOffset);
                Quaternion rot = Quaternion.AngleAxis(visualAngle, Vector3.up);

                Matrix4x4 matrix = Matrix4x4.TRS(drawPos, rot, scale);

                Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
            }
        }
        public void DestroyFlightGraphic()
        {
            RGBMaterialPool.Release(this);
            flightGraphic = null;
            shadowMaterial = null;
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
            if (graphic != null && graphic.MatNorth != null)
            {
                shadowMaterial = MaterialPool.MatFrom(
                    (Texture2D)graphic.MatNorth.mainTexture,
                    ShaderDatabase.Transparent,
                        new Color(0f, 0f, 0f, 0.25f)
                );
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
            Scribe_TargetInfo.Look(ref landingSpot, "landingSpot", LocalTargetInfo.Invalid);
            Scribe_TargetInfo.Look(ref runwayStartingSpot, "runwayStartingSpot", LocalTargetInfo.Invalid);
            Scribe_Values.Look(ref targetForRunway, "targetForRunway");
            Scribe_Values.Look(ref landingStage, "landingStage");
            Scribe_Values.Look(ref clockwiseTurn, "clockwiseTurn");
            Scribe_Values.Look(ref continueRotating, "continueRotating");
            Scribe_Values.Look(ref takeoffProgress, "takeoffProgress");
            Scribe_Values.Look(ref bombardmentOptionInd, "bombardmentOptionInd");
            Scribe_Values.Look(ref lastBombardmentTick, "lastBombardmentTick");
            Scribe_Values.Look(ref tickToStartFiring, "tickToStartFiring");
            Scribe_Values.Look(ref bombingRunCount, "bombingRunCount");
            Scribe_Values.Look(ref bombingCooldownTicks, "bombingCooldownTicks");
            Scribe_Values.Look(ref initialized, "initialized");
            Scribe_Deep.Look(ref arrivalAction, "arrivalAction", Array.Empty<object>());
            Scribe_Collections.Look(ref flightPath, "flightPath");
            Scribe_Values.Look(ref orderRecon, "orderRecon");
            Scribe_Values.Look(ref shouldCrash, "shouldCrash");
        }
    }
}
