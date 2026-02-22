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
    public enum ChaseMode
    {
        Circling,
        Direct,
        Elliptical,
        Hovering
    }

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
        public float maxDistanceFromTargetCircle;
        public float maxDistanceFromTargetElliptical;
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
        public float ellipseMajorAxis = 120f;
        public float ellipseMinorAxis = 60f;
        public float? flightSpeedCirclingPerTick;
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
        public bool detailedLoggingMode;
        private Dictionary<string, string> lastLoggedMessages = new Dictionary<string, string>();

        public FlightMode flightMode;
        private bool Flying => flightMode != FlightMode.Off;
        private bool GoingToWorld => arrivalAction != null;

        private LocalTargetInfo target;
        private LocalTargetInfo faceTarget = LocalTargetInfo.Invalid;
        private LocalTargetInfo landingSpot;
        private LocalTargetInfo runwayStartingSpot;
        private Vector3? targetForRunway;
        private LandingStage landingStage;
        private bool reachedInitialTarget;
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
            get => curAngleInt;
            set => curAngleInt = AngleAdjusted(value);
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
        private Vector3 currentVelocity = Vector3.zero;
        private float angularVelocity = 0f;
        private int nextTargetSearchTick = 0;
        private bool continueRotating;
        private bool orbitClockwise = true;
        private Vector3 orbitPerpOffset;
        private bool orbitInitialized;
        private float orbitOrientAngle;
        private ChaseMode currentChaseMode = ChaseMode.Circling;
        private FlightPattern flightPattern = FlightPattern.Around;

        private int bombingRunCount;
        private int bombingCooldownTicks;
        private Graphic_Vehicle flightGraphic;
        private Vector2 lastFlightGraphicDrawSize = Vector2.zero;

        public Graphic_Vehicle FlightGraphic
        {
            get
            {
                var currentDrawSize = GetCurrentDrawSize();
                bool needsUpdate = flightGraphic == null || lastFlightGraphicDrawSize != currentDrawSize;

                if (needsUpdate)
                {
                    LongEventHandler.ExecuteWhenFinished(delegate
                    {
                        if (UnityData.IsInMainThread)
                        {
                            flightGraphic = CreateFlightGraphic(this, Props.flightGraphicData);
                            flightGraphic.drawSize = currentDrawSize;
                        }
                    });
                    lastFlightGraphicDrawSize = currentDrawSize;
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

                yield return new Command_Action
                {
                    defaultLabel = "DEV: Set as enemy NPC",
                    action = () =>
                    {
                        var hostileFaction = Find.FactionManager.AllFactionsVisible.FirstOrDefault(f => f.HostileTo(Faction.OfPlayer));
                        if (hostileFaction != null)
                        {
                            Vehicle.SetFaction(hostileFaction);
                            if (Props.AISettings != null)
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
                            if (!Vehicle.Drafted)
                            {
                                Vehicle.drafter.Drafted = true;
                            }
                            if (!Flying)
                            {
                                SetFlightMode(true);
                            }
                            detailedLoggingMode = true;
                            LogAlways("Gunship", $"Gunship mode: {Props.AISettings?.gunshipSettings?.chaseMode}, pattern: {Props.AISettings?.gunshipSettings?.flightPattern}, moveWhileTakingOff: {Props.moveWhileTakingOff}");
                        }
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: Set Gunship AI Mode",
                    action = () =>
                    {
                        var options = new List<FloatMenuOption>();

                        options.Add(new FloatMenuOption(
                            "Direct",
                            delegate
                            {
                                Props.AISettings.gunshipSettings.chaseMode = ChaseMode.Direct;
                                LogAlways("Gunship", $"Gunship AI set to: Direct");
                            }));

                        options.Add(new FloatMenuOption(
                            "Hovering",
                            delegate
                            {
                                Props.AISettings.gunshipSettings.chaseMode = ChaseMode.Hovering;
                                LogAlways("Gunship", $"Gunship AI set to: Hovering");
                            }));

                        foreach (ChaseMode mode in new[] { ChaseMode.Circling, ChaseMode.Elliptical })
                        {
                            var modeCapture = mode;
                            foreach (FlightPattern pattern in Enum.GetValues(typeof(FlightPattern)))
                            {
                                var patternCapture = pattern;
                                var label = mode + ": " + (pattern == FlightPattern.Around ? "orbit around" : "fly over");
                                options.Add(new FloatMenuOption(
                                    label,
                                    delegate
                                    {
                                        Props.AISettings.gunshipSettings.chaseMode = modeCapture;
                                        Props.AISettings.gunshipSettings.flightPattern = patternCapture;
                                        LogAlways("Gunship", $"Gunship AI set to: {modeCapture} / {patternCapture}");
                                    }));
                            }
                        }

                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };

                yield return new Command_Toggle
                {
                    defaultLabel = "DEV: Toggle detailed logging",
                    defaultDesc = "Toggle detailed flight mode logging",
                    isActive = () => detailedLoggingMode,
                    toggleAction = () =>
                    {
                        detailedLoggingMode = !detailedLoggingMode;
                        LogAlways("Gunship", $"Detailed logging {(detailedLoggingMode ? "enabled" : "disabled")} for {Vehicle}");
                    }
                };
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
                if (faceTarget.IsValid && Props.flightCommands.cancelFaceTarget != null)
                {
                    var cancelFaceTargetCommand = Props.flightCommands.cancelFaceTarget.GetCommand();
                    cancelFaceTargetCommand.action = () =>
                    {
                        faceTarget = LocalTargetInfo.Invalid;
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
                            faceTarget = x;
                        });
                    };
                    yield return faceTargetCommand;
                }
            }

            if (Props.flightCommands.chaseTarget != null)
            {
                var chaseCommand = new Command_Action
                {
                    defaultLabel = "CVN_ChaseMode".Translate() + ": " + ("CVN_ChaseMode_" + currentChaseMode.ToString()).Translate(),
                    defaultDesc = "CVN_ChaseModeDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get(Props.flightCommands.chaseTarget.texPath),
                    action = () =>
                    {
                        var options = new List<FloatMenuOption>();

                        options.Add(new FloatMenuOption(
                            ("CVN_ChaseMode_Direct").Translate(),
                            delegate
                            {
                                SetChaseMode(ChaseMode.Direct);
                                if (Hovering) SetFlightMode(true);
                                Find.Targeter.BeginTargeting(TargetingParamsForFacing, delegate (LocalTargetInfo x)
                                {
                                    target = x;
                                    faceTarget = LocalTargetInfo.Invalid;
                                });
                            }));

                        foreach (ChaseMode mode in new[] { ChaseMode.Circling, ChaseMode.Elliptical })
                        {
                            var modeCapture = mode;
                            foreach (FlightPattern pattern in Enum.GetValues(typeof(FlightPattern)))
                            {
                                var patternCapture = pattern;
                                options.Add(new FloatMenuOption(
                                    ("CVN_ChaseMode_" + mode).Translate() + ": " + ("CVN_FlightPattern_" + pattern).Translate(),
                                    delegate
                                    {
                                        SetChaseMode(modeCapture, patternCapture);
                                        if (Hovering) SetFlightMode(true);
                                        Find.Targeter.BeginTargeting(TargetingParamsForFacing, delegate (LocalTargetInfo x)
                                        {
                                            target = x;
                                            faceTarget = LocalTargetInfo.Invalid;
                                        });
                                    }));
                            }
                        }

                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
                yield return chaseCommand;
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
            LogOnce("SetFlightMode", $"flightMode: {flightMode}, current flightMode: {this.flightMode}");
            if (flightMode)
            {
                SetTarget(Vehicle.vehiclePather.Moving ? Vehicle.vehiclePather.Destination : Vehicle.Position);
                if (!InAir)
                {
                    curPosition = Vehicle.DrawTracker.DrawPos;
                    CurAngle = Vehicle.FullRotation.AsAngle - FlightAngleOffset;
                }
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
            LogOnce("SetHoverMode", $"hoverMode: {hoverMode}, current flightMode: {this.flightMode}");
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
            faceTarget = LocalTargetInfo.Invalid;
            this.flightMode = hoverMode ? FlightMode.Hover : FlightMode.Flight;
        }

        public void SetTarget(LocalTargetInfo targetInfo)
        {
            LogOnce("SetTarget", $"targetInfo: {targetInfo}, previous target: {this.target}");
            this.target = targetInfo;
            this.reachedInitialTarget = false;
            ResetFlightData();
            if (Vehicle.jobs.curDriver is JobDriver_Goto)
            {
                Vehicle.vehiclePather.PatherFailed();
            }
        }

        private void ResetFlightData()
        {
            LogOnce("ResetFlightData", $"orbitClockwise: {orbitClockwise}, orbitInitialized: {orbitInitialized}");
            orbitClockwise = true;
            orbitInitialized = false;
            runwayStartingSpot = LocalTargetInfo.Invalid;
            landingSpot = LocalTargetInfo.Invalid;
            targetForRunway = null;
            landingStage = LandingStage.Inactive;

            currentVelocity = Vector3.zero;
            angularVelocity = 0f;
        }

        private void SetChaseMode(ChaseMode mode, FlightPattern pattern = FlightPattern.Around)
        {
            LogOnce("SetChaseMode", $"mode: {mode}, pattern: {pattern}, previous mode: {currentChaseMode}, previous pattern: {flightPattern}");
            currentChaseMode = mode;
            flightPattern = pattern;
            orbitInitialized = false;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (InAir)
            {
                var targetDist = target.IsValid ? Vector3.Distance(curPosition.Yto0(), target.CenterVector3.Yto0()) : (float?)null;
                var faceTargetDist = faceTarget.IsValid ? Vector3.Distance(curPosition.Yto0(), faceTarget.CenterVector3.Yto0()) : (float?)null;
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

                if (TakingOff is false && !reachedInitialTarget && curPosition.ToIntVec3().InBounds(Vehicle.Map)
                    && OccupiedRect().Contains(target.Cell))
                {
                    reachedInitialTarget = true;
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
            LogOnce("AITick", $"target: {target}, faceTarget: {faceTarget}");
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
                        LogOnce("AITick_BombCooldown", "Bomb cooldown active, returning");
                        return;
                    }
                    var curTarget = GetTarget();
                    if (curTarget.IsValid && curTarget.HasThing)
                    {
                        target = curTarget;
                        faceTarget = LocalTargetInfo.Invalid;
                    }
                    if (target.IsValid)
                    {
                        if (Vehicle.Position.DistanceTo(curTarget.Cell) < bomberSettings.distanceFromTarget)
                        {
                            var bombOption = Props.bombOptions.Where(x => bomberSettings.blacklistedBombs.Contains(x.projectile) is false).RandomElement();
                            if (TryDropBomb(bombOption))
                            {
                                LogOnce("AITick_DroppedBomb", $"Dropped bomb: {bombOption.projectile}");
                                return;
                            }
                        }
                    }
                }
            }

            var gunshipSettings = Props.AISettings.gunshipSettings;
            if (gunshipSettings != null)
            {
                bool needsTarget = false;

                if (gunshipSettings.chaseMode == ChaseMode.Hovering)
                    needsTarget = !faceTarget.IsValid || (faceTarget.HasThing && faceTarget.Thing.Destroyed);
                else
                    needsTarget = !target.IsValid || (target.HasThing && target.Thing.Destroyed);

                if (needsTarget || Find.TickManager.TicksGame >= nextTargetSearchTick)
                {
                    nextTargetSearchTick = Find.TickManager.TicksGame + 60;

                    var curTarget = GetTarget();
                    LogOnce("AITick_curTarget", $"curTarget: {curTarget}{(curTarget.HasThing ? $" (Thing: {curTarget.Thing})" : "")}");
                    if (curTarget.IsValid && curTarget.HasThing)
                    {
                        Thing previousTargetThing = null;
                        if (gunshipSettings.chaseMode == ChaseMode.Hovering && faceTarget.HasThing)
                            previousTargetThing = faceTarget.Thing;
                        else if (gunshipSettings.chaseMode != ChaseMode.Hovering && target.HasThing)
                            previousTargetThing = target.Thing;

                        if (previousTargetThing != null && previousTargetThing != curTarget.Thing)
                        {
                            LogAlways("AITick_TargetSwitch", $"Target switched from {previousTargetThing} to {curTarget.Thing}");
                        }

                        if (gunshipSettings.chaseMode == ChaseMode.Hovering)
                        {
                            if (flightMode != FlightMode.Hover)
                            {
                                SetHoverMode(true);
                            }
                            reachedInitialTarget = true;
                            faceTarget = curTarget;
                            target = LocalTargetInfo.Invalid;
                        }
                        else
                        {
                            if (currentChaseMode != gunshipSettings.chaseMode || flightPattern != gunshipSettings.flightPattern)
                            {
                                SetChaseMode(gunshipSettings.chaseMode, gunshipSettings.flightPattern);
                            }
                            target = curTarget;
                            faceTarget = LocalTargetInfo.Invalid;
                        }
                    }
                }
            }
        }

        public LocalTargetInfo GetTarget()
        {
            var targets = Vehicle.Map.attackTargetsCache.GetPotentialTargetsFor(Vehicle).Select(x => x.Thing)
                .Concat(Vehicle.Map.listerThings.ThingsInGroup(ThingRequestGroup.PowerTrader));

            targets = targets.Distinct().Where(x => IsValidTarget(x)).OrderByDescending(x => CombatPoints(x)).Take(5);

            Thing currentTargetThing = null;
            if (target.IsValid && target.HasThing) currentTargetThing = target.Thing;
            else if (faceTarget.IsValid && faceTarget.HasThing) currentTargetThing = faceTarget.Thing;

            var result = targets.OrderByDescending(x =>
            {
                float score = NearbyAreaCombatPoints(x);

                if (currentTargetThing != null && x == currentTargetThing)
                {
                    score += 1000f;
                }

                return score;
            }).FirstOrDefault();

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
            var hoverTargetPos = target.IsValid ? target.CenterVector3 : (faceTarget.IsValid ? faceTarget.CenterVector3 : Vector3.zero);
            var hoverDist = hoverTargetPos != Vector3.zero ? Vector3.Distance(curPosition.Yto0(), hoverTargetPos.Yto0()) : 0f;
            LogOnce("Hover", $"CurAngle: {CurAngle}, curPosition: {curPosition}, target: {target}, faceTarget: {faceTarget}, reachedInitialTarget: {reachedInitialTarget}, targetPos: {hoverTargetPos}, distance: {hoverDist:F2}");

            if (!reachedInitialTarget || (target.IsValid && target.Cell != Vehicle.Position))
            {
                float angleDiff;
                if (faceTarget.IsValid)
                {
                    angleDiff = RotateTowards(faceTarget.CenterVector3);

                    float alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                    float speed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);

                    MoveTowards(target.CenterVector3, speed);
                }
                else
                {
                    angleDiff = RotateTowards(target.CenterVector3);

                    float alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                    float speed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);

                    MoveFurther(speed);
                }
            }
            else if (faceTarget.IsValid)
            {
                float angleDiff = RotateTowards(faceTarget.CenterVector3);
                if (InAIMode)
                {
                    var distance = faceTarget.Cell.DistanceTo(Vehicle.Position);
                    var targetDist = Props.AISettings.gunshipSettings.distanceFromTarget;

                    float distError = Mathf.Abs(distance - targetDist);
                    var speedMult = Mathf.Clamp01(distError / 5f);
                    var targetSpeed = Props.flightSpeedTurningPerTick * speedMult;

                    float alignmentFactor = Mathf.Clamp01(1f - ((angleDiff - 15f) / 180f));
                    float finalSpeed = targetSpeed * (0.3f + 0.7f * alignmentFactor);

                    if (distance > targetDist + 1f)
                        MoveFurther(finalSpeed, targetDist, faceTarget.CenterVector3);
                    else if (distance < targetDist - 1f)
                        MoveBack(finalSpeed);
                    else
                    {
                        MoveDirection(Vector3.forward, 0f);
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
            else if (target.IsValid)
            {
                MoveInChaseMode(target);
            }
        }

        private void MoveToLandingSpot()
        {
            var landingDist = targetForRunway.HasValue ? Vector3.Distance(curPosition.Yto0(), targetForRunway.Value.Yto0()) : 0f;
            LogOnce("MoveToLandingSpot", $"landingStage: {landingStage}, targetForRunway: {targetForRunway}, curPosition: {curPosition}, distance: {landingDist:F2}");
            if (landingStage == LandingStage.Inactive)
            {
                if (targetForRunway is null)
                {
                    var newTarget = runwayStartingSpot.CenterVector3 + (Quaternion.AngleAxis(CurAngle, Vector3.up)
                        * (Vector3.forward * (Props.maxDistanceFromTargetElliptical * 2))).RotatedBy(FlightAngleOffset);
                    targetForRunway = newTarget;
                    LogOnce("MoveToLandingSpot", $"Set targetForRunway: {targetForRunway}");
                }
                float angleDiff = RotateTowards(targetForRunway.Value);
                float alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                float speed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);
                MoveFurther(speed);
                if (angleDiff < 2.0f)
                {
                    landingStage = LandingStage.GotoInitialSpot;
                    LogOnce("MoveToLandingSpot", "Stage changed to GotoInitialSpot");
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
                        LogOnce("MoveToLandingSpot", $"Stage changed to GotoRunwayStartSpot, clockturn: {clockturn}");
                    }
                    float angleDiff = RotateTowards(runwayStartingSpot.CenterVector3);
                    float alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                    float speed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);
                    MoveFurther(speed);
                }
                else
                {
                    if (landingStage == LandingStage.GotoRunwayStartSpot)
                    {
                        Vehicle.Map.debugDrawer.FlashCell(runwayStartingSpot.Cell);
                        Vehicle.Map.debugDrawer.FlashCell(landingSpot.Cell, 0.5f);
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                        float angleDiff = RotateTowards(runwayStartingSpot.CenterVector3);
                        float alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                        float speed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);
                        MoveFurther(speed);
                        if (curPosition.ToIntVec3() == runwayStartingSpot.Cell)
                        {
                            landingStage = LandingStage.GotoLanding;
                            LogOnce("MoveToLandingSpot", "Stage changed to GotoLanding");
                        }
                    }
                    else if (landingStage == LandingStage.GotoLanding)
                    {
                        float angleDiff = RotateTowards(landingSpot.CenterVector3);
                        float alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                        float speed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);
                        MoveFurther(speed);
                        takeoffProgress = Mathf.Max(0, takeoffProgress - (1 / (float)Props.landingTicks));
                        Vehicle.Map.debugDrawer.FlashCell(runwayStartingSpot.Cell);
                        Vehicle.Map.debugDrawer.FlashCell(landingSpot.Cell, 0.5f);
                        if (takeoffProgress == 0)
                        {
                            SetFlightMode(false);
                            FlightEnd();
                            LogOnce("MoveToLandingSpot", "Landing complete");
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

            LogOnce("ShouldRotate", $"targetAngle: {targetAngle}, curAngle: {curAngle}");

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
                        LogOnce("ShouldRotate", "No rotation needed (clockturn is null)");
                        return false;
                    }
                    else
                    {
                        var turnoffset = clockturn.Value ? Props.turnAnglePerTick : -Props.turnAnglePerTick;
                        var ticksPassedSimulated = (int)(dist / speedPerTick);
                        LogOnce("ShouldRotate", $"Simulating {ticksPassedSimulated} ticks, turnoffset: {turnoffset}");
                        for (var i = 0; i < ticksPassedSimulated; i++)
                        {
                            curAngle = AngleAdjusted(curAngle + turnoffset);
                            bool angleInRange = AngleInRange(curAngle, AngleAdjusted(targetAngle - Props.turnAnglePerTick), AngleAdjusted(targetAngle + Props.turnAnglePerTick));
                            if (angleInRange && i >= ticksPassedSimulated - 3)
                            {
                                LogOnce("ShouldRotate", $"Should rotate at tick {i}, curAngle: {curAngle}");
                                return true;
                            }
                        }
                    }
                    LogOnce("ShouldRotate", "Rotation not possible within simulation");
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
            bool result = (angle - lower) % 360 <= (upper - lower) % 360;
            LogOnce("AngleInRange", $"angle: {angle}, lower: {lower}, upper: {upper}, result: {result}");
            return result;
        }

        private void Takeoff()
        {
            LogOnce("Takeoff", $"takeoffProgress: {takeoffProgress}");
            takeoffProgress = Mathf.Min(1, takeoffProgress + (1 / (float)Props.takeoffTicks));
            if (Props.moveWhileTakingOff)
            {
                var speed = Props.flightSpeedPerTick * takeoffProgress;
                MoveFurther(speed);
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

        private void MoveDirection(Vector3 direction, float targetSpeed, float? minDistanceFromTarget = null, Vector3? stopTarget = null)
        {
            if (minDistanceFromTarget.HasValue && stopTarget.HasValue)
            {
                float dist = Vector3.Distance(curPosition.Yto0(), stopTarget.Value.Yto0());
                if (dist < minDistanceFromTarget.Value)
                {
                    targetSpeed = 0f;
                }
            }

            Vector3 desiredVelocity = direction.normalized * targetSpeed;

            if (currentVelocity.sqrMagnitude > 0.0001f && desiredVelocity.sqrMagnitude > 0.0001f)
            {
                float maxTurnSpeed = Mathf.Max(Props.turnAnglePerTick * 2f, Mathf.Abs(angularVelocity) * 1.5f);
                float turnSpeedRadians = maxTurnSpeed * Mathf.Deg2Rad;
                Vector3 rotatedVelocityDir = Vector3.RotateTowards(currentVelocity.normalized, desiredVelocity.normalized, turnSpeedRadians, 0f);
                currentVelocity = rotatedVelocityDir * currentVelocity.magnitude;
            }

            float acceleration = 0.015f;
            if (targetSpeed <= 0f)
            {
                acceleration = 0.04f;
            }
            else if (currentVelocity.sqrMagnitude > 0.001f)
            {
                float angleShift = Vector3.Angle(currentVelocity.normalized, direction.normalized);
                acceleration = Mathf.Lerp(0.015f, 0.05f, angleShift / 90f);
            }

            currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration);

            Vector3 newPosition = curPosition + currentVelocity;
            curPosition = new Vector3(newPosition.x, Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays), newPosition.z);
        }

        private void MoveFurther(float speed, float? minDistanceFromTarget = null, Vector3? stopTarget = null)
        {
            Vector3 dir = (Quaternion.AngleAxis(CurAngle, Vector3.up) * Vector3.forward).RotatedBy(FlightAngleOffset);
            MoveDirection(dir, speed, minDistanceFromTarget, stopTarget);
        }

        private void MoveBack(float speed, float? minDistanceFromTarget = null, Vector3? stopTarget = null)
        {
            Vector3 dir = (Quaternion.AngleAxis(CurAngle, Vector3.up) * Vector3.back).RotatedBy(FlightAngleOffset);
            MoveDirection(dir, speed, minDistanceFromTarget, stopTarget);
        }

        private void MoveTowards(Vector3 to, float targetSpeed, float? minDistanceFromTarget = null, Vector3? stopTarget = null)
        {
            Vector3 toFlat = to.Yto0();
            Vector3 curFlat = curPosition.Yto0();
            Vector3 diff = toFlat - curFlat;
            float dist = diff.magnitude;

            if (dist < 0.01f)
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, 0.05f);
                curPosition += currentVelocity;
                return;
            }

            if (dist < targetSpeed) targetSpeed = dist;

            MoveDirection(diff.normalized, targetSpeed, minDistanceFromTarget, stopTarget);
        }

        private float RotatePerperticular(Vector3 orbitCenter, float desiredRadius, float turnRate, Vector3 realTarget)
        {
            Vector3 toPlane = curPosition.Yto0() - orbitCenter.Yto0();
            float currentDist = toPlane.magnitude;
            float radiusAngle = toPlane.AngleFlat();

            float tangentCW = AngleAdjusted(radiusAngle + 180f);
            float tangentCCW = AngleAdjusted(radiusAngle);

            float diffCW = Mathf.Abs(Utils.AngleDiff(CurAngle, tangentCW));
            float diffCCW = Mathf.Abs(Utils.AngleDiff(CurAngle, tangentCCW));

            if (Mathf.Abs(diffCW - diffCCW) > 15f)
                orbitClockwise = diffCW <= diffCCW;

            float targetAngle = orbitClockwise ? tangentCW : tangentCCW;
            float distError = currentDist - desiredRadius;
            float correction = Mathf.Clamp(distError * 0.8f, -25f, 25f);

            targetAngle = orbitClockwise
                ? AngleAdjusted(targetAngle + correction)
                : AngleAdjusted(targetAngle - correction);

            float avoidanceStrength = 1.0f;

            if (flightPattern == FlightPattern.Over)
            {
                float distToTarget = Vector3.Distance(curPosition.Yto0(), realTarget.Yto0());

                if (distToTarget < 35f)
                {
                    avoidanceStrength = Mathf.Clamp01((distToTarget - 15f) / 20f);

                    if (distToTarget <= 3f)
                    {
                        targetAngle = CurAngle;
                    }
                    else
                    {
                        float directAngle = GetAngleFromTarget(realTarget);
                        float diffToDirect = Utils.AngleDiff(targetAngle, directAngle);

                        if (Mathf.Abs(diffToDirect) < 75f)
                        {
                            float blend = Mathf.Clamp01((35f - distToTarget) / 15f);
                            targetAngle = AngleAdjusted(targetAngle + diffToDirect * blend);
                        }
                    }
                }
            }

            return RotateTo(targetAngle, turnRate, avoidanceStrength);
        }

        private Vector3 GetAvoidanceVector()
        {
            Vector3 totalRepulsion = Vector3.zero;
            if (Vehicle.Map is null)
            {
                return totalRepulsion;
            }

            var otherFlyingVehicles = Vehicle.Map.listerThings.ThingsInGroup(ThingRequestGroup.Everything)
                .OfType<VehiclePawn>()
                .Where(v => v != Vehicle && v.GetComp<CompFlightMode>() is CompFlightMode comp && comp.InAir);

            float myAvgRadius = (this.Vehicle.def.Size.x + this.Vehicle.def.Size.z) / 2f;
            int avoidanceCount = 0;

            foreach (var otherVehicle in otherFlyingVehicles)
            {
                if (target.IsValid && target.HasThing && otherVehicle == target.Thing) continue;
                if (faceTarget.IsValid && faceTarget.HasThing && otherVehicle == faceTarget.Thing) continue;

                var otherComp = otherVehicle.GetComp<CompFlightMode>();
                float otherAvgRadius = (otherVehicle.def.Size.x + otherVehicle.def.Size.z) / 2f;
                float avoidanceRadius = (myAvgRadius + otherAvgRadius) * 1.5f;

                float distance = Vector3.Distance(this.curPosition, otherComp.curPosition);

                if (distance < avoidanceRadius && distance > 0)
                {
                    Vector3 repulsionVector = this.curPosition - otherComp.curPosition;
                    float strength = 1f - (distance / avoidanceRadius);
                    totalRepulsion += repulsionVector.normalized * strength;
                    avoidanceCount++;
                }
            }

            if (avoidanceCount > 0)
            {
                LogOnce("GetAvoidanceVector", $"avoidanceCount: {avoidanceCount}, totalRepulsion: {totalRepulsion}");
            }

            return totalRepulsion;
        }

        private float GetAvoidanceModifiedTargetAngle(float currentAngle, float targetAngle, float strength)
        {
            if (strength <= 0f) return targetAngle;

            Vector3 avoidanceVector = GetAvoidanceVector();
            if (avoidanceVector.sqrMagnitude < 0.01f)
            {
                return targetAngle;
            }

            float avoidanceTargetAngle = AngleAdjusted(avoidanceVector.AngleFlat() + FlightAngleOffset);
            float avoidanceWeight = Mathf.Clamp01(avoidanceVector.magnitude);

            float angleDifference = Utils.AngleDiff(targetAngle, avoidanceTargetAngle);

            float maxNudge = 30f;
            float nudge = Mathf.Clamp(angleDifference, -maxNudge, maxNudge) * avoidanceWeight * strength;

            return AngleAdjusted(targetAngle + nudge);
        }

        private float RotateTo(float targetAngle, float turnRate, float avoidanceStrength = 1.0f)
        {
            float adjustedTargetAngle = GetAvoidanceModifiedTargetAngle(this.CurAngle, targetAngle, avoidanceStrength);
            float diff = Utils.AngleDiff(CurAngle, adjustedTargetAngle);
            float absDiff = Mathf.Abs(diff);

            float proportionalSpeed = diff * 0.3f;
            float desiredAngularVelocity = Mathf.Clamp(proportionalSpeed, -turnRate, turnRate);

            float acceleration = turnRate * 0.5f;
            angularVelocity = Mathf.MoveTowards(angularVelocity, desiredAngularVelocity, acceleration);

            CurAngle += angularVelocity;

            return absDiff;
        }

        private float RotateTowards(Vector3 target, float turnRateOverride = -1f)
        {
            float targetAngle = GetAngleFromTarget(target);
            float rate = turnRateOverride > 0f ? turnRateOverride : Props.turnAnglePerTick;
            return RotateTo(targetAngle, rate);
        }

        private float GetOrbitRadius()
        {
            float radius = Props.maxDistanceFromTargetCircle;
            if (InAIMode && Props.AISettings?.gunshipSettings != null)
                radius = Props.AISettings.gunshipSettings.distanceFromTarget;
            LogOnce("GetOrbitRadius", $"radius: {radius}, InAIMode: {InAIMode}");
            return radius;
        }

        private void GetEllipseAxes(out float a, out float b)
        {
            a = Props.ellipseMajorAxis;
            b = Props.ellipseMinorAxis;

            float maxRadius = Props.maxDistanceFromTargetElliptical;

            if (InAIMode && Props.AISettings?.gunshipSettings != null)
                maxRadius = Props.AISettings.gunshipSettings.distanceFromTarget;

            if (maxRadius > 0 && a > maxRadius)
            {
                float ratio = a > 0 ? b / a : 0.5f;
                a = maxRadius;
                b = a * ratio;
            }

            if (a <= 0) a = 10f;
            if (b <= 0) b = 5f;

            LogOnce("GetEllipseAxes", $"a: {a}, b: {b}, maxRadius: {maxRadius}, originalMajorAxis: {Props.ellipseMajorAxis}, originalMinorAxis: {Props.ellipseMinorAxis}");
        }

        private void InitOrbit(Vector3 targetPos)
        {
            Vector3 cur2D = curPosition.Yto0();
            Vector3 tgt2D = targetPos.Yto0();
            Vector3 toTarget = tgt2D - cur2D;
            float dist = toTarget.magnitude;
            Vector3 approachDir = dist > 0.001f ? toTarget / dist : Vector3.forward;

            float approachAngle = Mathf.Atan2(approachDir.x, approachDir.z) * Mathf.Rad2Deg;
            if (approachAngle < 0f) approachAngle += 360f;

            LogOnce("InitOrbit_start", $"targetPos: {targetPos}, curPos: {cur2D}, curPosition: {curPosition}, dist: {dist}, approachAngle: {approachAngle}");

            if (currentChaseMode == ChaseMode.Circling && flightPattern == FlightPattern.Over)
            {
                Vector3 perp = new Vector3(approachDir.z, 0f, -approachDir.x);
                orbitPerpOffset = perp * GetOrbitRadius();
                LogOnce("InitOrbit", $"Circling/Over: orbitPerpOffset: {orbitPerpOffset}, orbitRadius: {GetOrbitRadius()}");
            }
            else if (currentChaseMode == ChaseMode.Elliptical && flightPattern == FlightPattern.Over)
            {
                GetEllipseAxes(out float a, out float b);

                // RACETRACK PATTERN: Align major axis with approach to create a long straight attack run over the target
                orbitOrientAngle = approachAngle;

                // Offset center by 'b' so the target lies perfectly on the flat straightaway
                Vector3 perp = new Vector3(approachDir.z, 0f, -approachDir.x);
                orbitPerpOffset = perp * b;

                LogOnce("InitOrbit", $"Elliptical/Over: orbitPerpOffset: {orbitPerpOffset}, orbitOrientAngle: {orbitOrientAngle}, a: {a}, b: {b}");
            }
            else if (currentChaseMode == ChaseMode.Elliptical && flightPattern == FlightPattern.Around)
            {
                orbitPerpOffset = Vector3.zero;
                orbitOrientAngle = approachAngle + 90f;
                LogOnce("InitOrbit", $"Elliptical/Around: orbitOrientAngle: {orbitOrientAngle}");
            }

            orbitInitialized = true;
        }

        private void DoEllipseOrbit(Vector3 worldCenter, float a, float b, float orientDeg, Vector3 realTarget)
        {
            float baseSpeed = Props.flightSpeedCirclingPerTick ?? Props.flightSpeedTurningPerTick;
            float minCurveRadius = Mathf.Max((b * b) / Mathf.Max(a, 0.001f), 1f);
            float requiredTurnRate = (baseSpeed * 180f) / (Mathf.PI * minCurveRadius) * 1.5f;
            float configuredTurnRate = Props.turnAngleCirclingPerTick > 0f ? Props.turnAngleCirclingPerTick : Props.turnAnglePerTick;
            float turnRate = Mathf.Max(configuredTurnRate, requiredTurnRate);

            float oRad = orientDeg * Mathf.Deg2Rad;
            Vector3 mj = new Vector3(Mathf.Sin(oRad), 0f, Mathf.Cos(oRad));
            Vector3 mn = new Vector3(Mathf.Cos(oRad), 0f, -Mathf.Sin(oRad));

            Vector3 cur2D = curPosition.Yto0();
            Vector3 c2D = new Vector3(worldCenter.x, 0f, worldCenter.z);
            Vector3 d = cur2D - c2D;

            float localA = Vector3.Dot(d, mj);
            float localB = Vector3.Dot(d, mn);
            float theta = Mathf.Atan2(localB / b, localA / a);

            Vector3 ellipsePoint = c2D + (a * Mathf.Cos(theta)) * mj + (b * Mathf.Sin(theta)) * mn;

            float tangentAngleFlat = Mathf.Atan2((-a * Mathf.Sin(theta) * mj + b * Mathf.Cos(theta) * mn).normalized.x, (-a * Mathf.Sin(theta) * mj + b * Mathf.Cos(theta) * mn).normalized.z) * Mathf.Rad2Deg;
            float curAngleCW = AngleAdjusted(tangentAngleFlat + 90f);
            float curAngleCCW = AngleAdjusted(curAngleCW + 180f);

            if (Mathf.Abs(Mathf.Abs(Utils.AngleDiff(CurAngle, curAngleCW)) - Mathf.Abs(Utils.AngleDiff(CurAngle, curAngleCCW))) > 15f)
                orbitClockwise = Mathf.Abs(Utils.AngleDiff(CurAngle, curAngleCW)) <= Mathf.Abs(Utils.AngleDiff(CurAngle, curAngleCCW));

            float targetAngle = orbitClockwise ? curAngleCW : curAngleCCW;
            float distError = d.magnitude - (ellipsePoint - c2D).magnitude;
            float correction = Mathf.Clamp(distError * 0.8f, -25f, 25f);

            targetAngle = orbitClockwise ? AngleAdjusted(targetAngle + correction) : AngleAdjusted(targetAngle - correction);

            float avoidanceStrength = 1.0f;

            if (flightPattern == FlightPattern.Over)
            {
                float distToTarget = Vector3.Distance(curPosition.Yto0(), realTarget.Yto0());

                if (distToTarget < 35f)
                {
                    avoidanceStrength = Mathf.Clamp01((distToTarget - 15f) / 20f);

                    if (distToTarget <= 3f)
                    {
                        targetAngle = CurAngle;
                    }
                    else
                    {
                        float directAngle = GetAngleFromTarget(realTarget);
                        float diffToDirect = Utils.AngleDiff(targetAngle, directAngle);

                        if (Mathf.Abs(diffToDirect) < 75f)
                        {
                            float blend = Mathf.Clamp01((35f - distToTarget) / 15f);
                            targetAngle = AngleAdjusted(targetAngle + diffToDirect * blend);
                        }
                    }
                }
            }

            float angleDiff = RotateTo(targetAngle, turnRate, avoidanceStrength);

            float maxPhysicalSpeed = (turnRate * Mathf.PI * minCurveRadius) / 180f;
            float baseSpeedLimit = Mathf.Min(baseSpeed, maxPhysicalSpeed);
            float alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
            float finalSpeed = Mathf.Lerp(Mathf.Min(Props.flightSpeedTurningPerTick, baseSpeedLimit), baseSpeedLimit, alignment);

            MoveFurther(finalSpeed);
        }

        private void MoveInChaseMode(LocalTargetInfo chaseTarget)
        {
            Vector3 targetPos = chaseTarget.CenterVector3.Yto0();
            var distToTarget = Vector3.Distance(curPosition.Yto0(), targetPos);

            LogOnce("MoveInChaseMode", $"chaseMode: {currentChaseMode}, flightPattern: {flightPattern}, targetPos: {targetPos}, distToTarget: {distToTarget:F2}, curPosition: {curPosition}");

            if (!orbitInitialized)
            {
                InitOrbit(targetPos);
            }

            switch (currentChaseMode)
            {
                case ChaseMode.Direct:
                    {
                        float angleToTarget = GetAngleFromTarget(target.CenterVector3);
                        float turnRate = Props.turnAnglePerTick > 0f ? Props.turnAnglePerTick : 1f;
                        float turnSpeed = Props.flightSpeedTurningPerTick > 0f ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick;

                        float minTurnRadius = (turnSpeed * 180f) / (Mathf.PI * turnRate);
                        float angleDiff = Mathf.Abs(Utils.AngleDiff(CurAngle, angleToTarget));

                        float avoidanceStrength = 1.0f;
                        if (distToTarget < 35f)
                        {
                            avoidanceStrength = Mathf.Clamp01((distToTarget - 15f) / 20f);
                        }

                        float rotateDiff = 0f;
                        if (!(distToTarget < minTurnRadius * 2.5f && angleDiff > 90f))
                        {
                            rotateDiff = RotateTo(angleToTarget, turnRate, avoidanceStrength);
                        }

                        float alignment = Mathf.Clamp01(1f - ((rotateDiff - 15f) / 45f));
                        float finalSpeed = Mathf.Lerp(turnSpeed, Props.flightSpeedPerTick, alignment);

                        MoveFurther(finalSpeed);
                        break;
                    }

                case ChaseMode.Circling:
                    {
                        float orbitRadius = GetOrbitRadius();
                        Vector3 orbitCenter = flightPattern == FlightPattern.Around
                            ? targetPos
                            : new Vector3(targetPos.x + orbitPerpOffset.x, targetPos.y, targetPos.z + orbitPerpOffset.z);

                        float baseSpeed = Props.flightSpeedCirclingPerTick ?? Props.flightSpeedTurningPerTick;
                        float requiredTurnRate = (baseSpeed * 180f) / (Mathf.PI * Mathf.Max(orbitRadius, 1f)) * 1.5f;
                        float turnRate = Mathf.Max(Props.turnAngleCirclingPerTick > 0f ? Props.turnAngleCirclingPerTick : Props.turnAnglePerTick, requiredTurnRate);

                        LogOnce("CirclingChase", $"orbitRadius: {orbitRadius}, orbitCenter: {orbitCenter}, baseSpeed: {baseSpeed}, turnRate: {turnRate}");

                        // Pass the real targetPos into RotatePerperticular
                        float angleDiff = RotatePerperticular(orbitCenter, orbitRadius, turnRate, targetPos);

                        float maxPhysicalSpeed = (turnRate * Mathf.PI * Mathf.Max(orbitRadius, 1f)) / 180f;
                        float baseSpeedLimit = Mathf.Min(baseSpeed, maxPhysicalSpeed);

                        float alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                        float finalSpeed = Mathf.Lerp(Mathf.Min(Props.flightSpeedTurningPerTick, baseSpeedLimit), baseSpeedLimit, alignment);

                        MoveFurther(finalSpeed);
                        break;
                    }

                case ChaseMode.Elliptical:
                    {
                        GetEllipseAxes(out float a, out float b);

                        Vector3 orbitCenter = flightPattern == FlightPattern.Around
                            ? targetPos
                            : targetPos + orbitPerpOffset;
                        var distToCenter = Vector3.Distance(curPosition.Yto0(), orbitCenter.Yto0());

                        LogOnce("EllipticalChase", $"a: {a}, b: {b}, orbitCenter: {orbitCenter}, distToCenter: {distToCenter}");

                        // Pass the real targetPos into DoEllipseOrbit
                        DoEllipseOrbit(orbitCenter, a, b, orbitOrientAngle, targetPos);
                        break;
                    }
            }
        }

        private bool? ClockWiseTurn(float targetAngle)
        {
            if (new FloatRange(targetAngle - Props.turnAnglePerTick, targetAngle + Props.turnAnglePerTick).Includes(CurAngle))
            {
                LogOnce("ClockWiseTurn", $"targetAngle: {targetAngle}, CurAngle: {CurAngle}, result: null (already aligned)");
                return null;
            }
            float diff = targetAngle - CurAngle;
            if (diff > 0 ? diff > 180f : diff >= -180f)
            {
                LogOnce("ClockWiseTurn", $"targetAngle: {targetAngle}, CurAngle: {CurAngle}, diff: {diff}, result: false (counter-clockwise)");
                return false;
            }
            LogOnce("ClockWiseTurn", $"targetAngle: {targetAngle}, CurAngle: {CurAngle}, diff: {diff}, result: true (clockwise)");
            return true;
        }

        private float GetAngleFromTarget(Vector3 target)
        {
            var rawAngle = (curPosition.Yto0() - target.Yto0()).AngleFlat();
            var targetAngle = rawAngle + FlightAngleOffset;
            var adjustedAngle = AngleAdjusted(targetAngle);
            LogOnce("GetAngleFromTarget", $"target: {target}, rawAngle: {rawAngle}, targetAngle: {targetAngle}, adjustedAngle: {adjustedAngle}");
            return adjustedAngle;
        }

        private void LogData(string prefix)
        {
            Log.Message(this.Vehicle + " - " + prefix + " - Vehicle.Position: " + Vehicle.Position + " - takeoffProgress: " + takeoffProgress
                + " - IsFlying: " + Flying + " - IsTakingOff: " + TakingOff + " - IsDescending: " + Landing
                + " - CurAngle: " + CurAngle + " - Vehicle.Angle: " + Vehicle.Angle
                + " - FullRotation: " + Vehicle.FullRotation.ToStringNamed() + " - Rotation: " + Vehicle.Rotation.ToStringHuman()
                + " - reachedInitialTarget: " + reachedInitialTarget + " - target: " + target + " - faceTarget: " + faceTarget);
        }

        private void LogOnce(string key, string message)
        {
            if (!detailedLoggingMode)
            {
                return;
            }
            var fullMessage = $"{key} - {message}";
            if (lastLoggedMessages.TryGetValue(key, out string lastMessage))
            {
                if (lastMessage == message)
                {
                    return;
                }
            }
            lastLoggedMessages[key] = message;
            Log.Message(fullMessage);
        }

        private void LogAlways(string key, string message)
        {
            if (!detailedLoggingMode)
            {
                return;
            }
            Log.Message($"{key} - {message}");
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
            Scribe_TargetInfo.Look(ref faceTarget, "faceTarget", LocalTargetInfo.Invalid);
            Scribe_Values.Look(ref reachedInitialTarget, "reachedInitialTarget", false);
            Scribe_TargetInfo.Look(ref landingSpot, "landingSpot", LocalTargetInfo.Invalid);
            Scribe_TargetInfo.Look(ref runwayStartingSpot, "runwayStartingSpot", LocalTargetInfo.Invalid);
            Scribe_Values.Look(ref targetForRunway, "targetForRunway");
            Scribe_Values.Look(ref landingStage, "landingStage");
            Scribe_Values.Look(ref orbitClockwise, "orbitClockwise", true);
            Scribe_Values.Look(ref continueRotating, "continueRotating");
            Scribe_Values.Look(ref orbitPerpOffset, "orbitPerpOffset");
            Scribe_Values.Look(ref orbitOrientAngle, "orbitOrientAngle");
            Scribe_Values.Look(ref currentChaseMode, "currentChaseMode", ChaseMode.Circling);
            Scribe_Values.Look(ref flightPattern, "flightPattern", FlightPattern.Around);
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
            Scribe_Values.Look(ref detailedLoggingMode, "detailedLoggingMode");
        }
    }
}
