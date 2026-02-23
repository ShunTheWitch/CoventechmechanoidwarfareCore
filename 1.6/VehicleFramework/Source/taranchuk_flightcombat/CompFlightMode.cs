using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Rendering;
using System;
using System.Collections.Generic;
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
        public bool loggingMode;
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
        public Rot4? designatedLandingRotation;
        private bool TakingOff => flightMode != FlightMode.Off && takeoffProgress < 1f && runwayStartingSpot.IsValid is false;
        private bool Hovering => flightMode == FlightMode.Hover;
        private bool Landing => flightMode == FlightMode.Off && takeoffProgress > 0f;
        public bool InAir => Flying || TakingOff || Landing;
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
                var vehicle = Vehicle;
                return vehicle.CompVehicleTurrets.Deploying is false && vehicle.CanMoveFinal
                    && vehicle.GetStatValue(VehicleStatDefOf.FlightSpeed) >
                    vehicle.VehicleDef.GetStatValueAbstract(VehicleStatDefOf.FlightSpeed) / 2f
                    && vehicle.GetStatValue(VehicleStatDefOf.FlightControl) >
                    vehicle.VehicleDef.GetStatValueAbstract(VehicleStatDefOf.FlightControl) / 2f;
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

            var vehicle = Vehicle;
            var originalSize = vehicle.VehicleDef.graphicData.drawSize;
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
            var vehicle = Vehicle;
            var originalSize = vehicle.VehicleDef.graphicData.drawSize;
            var currentSize = GetCurrentDrawSize();
            return new Vector2(currentSize.x / originalSize.x, currentSize.y / originalSize.y);
        }

        public float BaseFlightSpeed => Props.flightSpeedPerTick;
        public Vector3 curPosition;
        private Vector3 currentVelocity = Vector3.zero;
        private float angularVelocity = 0f;
        private float cachedAvoidanceBias = 0f;
        private float cachedAvoidanceSpeedMult = 1f;
        private int avoidanceComputedTick = -1;
        private int nextTargetSearchTick = 0;
        private int yieldUntilTick = 0;
        private Dictionary<VehicleTurret, ThingDef> cachedAmmoTypes;
        private Vector3 lastRenderPosition;
        private float lastRenderAngle;
        private bool continueRotating;
        private bool orbitClockwise = true;
        private Vector3 orbitPerpOffset;
        private Vector3 orbitOffsetDir;
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
            var vehicle = Vehicle;
            foreach (var entry in stock)
            {
                var stack = entry.countRange.RandomInRange;
                while (stack > 0)
                {
                    var thing = ThingMaker.MakeThing(entry.thingDef);
                    thing.stackCount = Mathf.Min(stack, thing.def.stackLimit);
                    stack -= thing.stackCount;
                    vehicle.inventory.TryAddItemNotForSale(thing);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var vehicle = Vehicle;
            if (vehicle.Faction == Faction.OfPlayer && vehicle.Drafted)
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
                            loggingMode = true;
                            LogAlways(() => "Gunship", () => $"Gunship mode: {Props.AISettings?.gunshipSettings?.chaseMode}, pattern: {Props.AISettings?.gunshipSettings?.flightPattern}, moveWhileTakingOff: {Props.moveWhileTakingOff}");
                        }
                    }
                };

                yield return new Command_Toggle
                {
                    defaultLabel = "DEV: Toggle runway requirement",
                    defaultDesc = "Toggles runway requirement (VTOL vs Runway mode).",
                    isActive = () => Props.moveWhileLanding,
                    toggleAction = () =>
                    {
                        Props.moveWhileLanding = !Props.moveWhileLanding;
                        Props.moveWhileTakingOff = !Props.moveWhileTakingOff;
                        LogAlways(() => "Dev", () => $"Runway requirement set to: {Props.moveWhileLanding}");
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: Copy Rotation Stats",
                    defaultDesc = "Copies internal rotation variables to clipboard for troubleshooting.",
                    action = () =>
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine($"--- Rotation Stats for {vehicle.LabelShort} (ID: {vehicle.ThingID}) ---");
                        sb.AppendLine($"[CompFlightMode]");
                        sb.AppendLine($"flightMode: {flightMode}");
                        sb.AppendLine($"InAir (Calculated): {InAir}");
                        sb.AppendLine($"TakingOff: {TakingOff}");
                        sb.AppendLine($"Landing: {Landing}");
                        sb.AppendLine($"Flying: {Flying}");
                        sb.AppendLine($"takeoffProgress: {takeoffProgress}");
                        sb.AppendLine($"CurAngle: {CurAngle}");
                        sb.AppendLine($"FlightAngleOffset: {FlightAngleOffset}");
                        sb.AppendLine($"designatedLandingRotation: {designatedLandingRotation}");

                        sb.AppendLine($"[VehiclePawn]");
                        sb.AppendLine($"Angle: {vehicle.Angle}");
                        sb.AppendLine($"Rotation (Rot4): {vehicle.Rotation} (Int: {vehicle.Rotation.AsInt})");
                        sb.AppendLine($"FullRotation (Rot8): {vehicle.FullRotation} (Int: {vehicle.FullRotation.AsInt})");
                        sb.AppendLine($"FullRotation.AsAngle: {vehicle.FullRotation.AsAngle}");

                        sb.AppendLine($"[Transform]");
                        if (vehicle.Transform != null)
                        {
                           sb.AppendLine($"Transform.rotation: {vehicle.Transform.rotation}");
                        }
                        else
                        {
                           sb.AppendLine($"Transform is null");
                        }

                        Log.Message(sb.ToString());
                        GUIUtility.systemCopyBuffer = sb.ToString();
                        Messages.Message("Rotation stats copied to clipboard", MessageTypeDefOf.TaskCompletion, false);
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
                                LogAlways(() => "Gunship", () => $"Gunship AI set to: Direct");
                            }));

                        options.Add(new FloatMenuOption(
                            "Hovering",
                            delegate
                            {
                                Props.AISettings.gunshipSettings.chaseMode = ChaseMode.Hovering;
                                LogAlways(() => "Gunship", () => $"Gunship AI set to: Hovering");
                            }));

                        foreach (var mode in new[] { ChaseMode.Circling, ChaseMode.Elliptical })
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
                                        LogAlways(() => "Gunship", () => $"Gunship AI set to: {modeCapture} / {patternCapture}");
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
                    isActive = () => loggingMode,
                    toggleAction = () =>
                    {
                        loggingMode = !loggingMode;
                        LogAlways(() => "Gunship", () => $"Detailed logging {(loggingMode ? "enabled" : "disabled")} for {vehicle}");
                    }
                };
            }
        }

        private IEnumerable<Gizmo> GetAircraftGizmos()
        {
            var vehicle = Vehicle;
            if (Props.flightCommands.flightMode != null)
            {
                var flightModeCommand = Props.flightCommands.flightMode.GetCommand();
                flightModeCommand.isActive = () => Flying;
                flightModeCommand.toggleAction = () =>
                {
                    LogAlways(() => "FlightModeCommand", () => $"Toggle called. Current flightMode: {this.flightMode}");
                    if (this.flightMode != FlightMode.Off)
                    {
                        LogAlways(() => "FlightModeCommand", () => "Opening landing designator");
                        Find.DesignatorManager.Select(new Designator_LandVehicle(this));
                    }
                    else
                    {
                        LogAlways(() => "FlightModeCommand", () => "Starting takeoff");
                        SetFlightMode(true);
                    }
                };
                if (Props.moveWhileTakingOff)
                {
                    flightModeCommand.onHover = () =>
                    {
                        if ((TakingOff || flightMode == FlightMode.Off) && Landing is false)
                        {
                            DrawRunway(takingOff: true);
                        }
                        else if(InAir && !targetForRunway.HasValue)
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
                }

                if (Props.canFlyInSpace is false && vehicle.Map.Biome.inVacuum)
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
            validator = (TargetInfo x) => x.Thing != Vehicle
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
            var vehicle = Vehicle;
            foreach (var cell in cells.ToList())
            {
                var adjcells = GenAdj.CellsAdjacent8Way(cell, vehicle.FullRotation, IntVec2.One);
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
            LogOnce(() => "SetFlightMode", () => $"flightMode: {flightMode}, current flightMode: {this.flightMode}");
            var vehicle = Vehicle;
            if (flightMode)
            {
                SetTarget(vehicle.vehiclePather.Moving ? vehicle.vehiclePather.Destination : vehicle.Position);
                if (!InAir)
                {
                    curPosition = vehicle.DrawTracker.DrawPos;
                    CurAngle = vehicle.FullRotation.AsAngle - FlightAngleOffset;
                }
                if (vehicle.vehiclePather.Moving)
                {
                    vehicle.vehiclePather.StopDead();
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

                if (designatedLandingRotation.HasValue)
                {
                    vehicle.FullRotation = Rot8.FromAngle(designatedLandingRotation.Value.AsAngle);
                    vehicle.Rotation = designatedLandingRotation.Value;
                    designatedLandingRotation = null;
                }
                else
                {
                    var landingAngle = AngleAdjusted(CurAngle + FlightAngleOffset);
                    var snappedRot = Rot4.FromAngleFlat(landingAngle);
                    vehicle.Rotation = snappedRot;
                    vehicle.FullRotation = Rot8.FromAngle(snappedRot.AsAngle);
                }

                vehicle.Angle = vehicle.FullRotation.AsAngle;

                if (vehicle.Transform != null)
                {
                    vehicle.Transform.rotation = 0f;
                }
            }
            ResetFlightData();
            this.flightMode = flightMode ? FlightMode.Flight : FlightMode.Off;
        }

        public void SetHoverMode(bool hoverMode)
        {
            LogOnce(() => "SetHoverMode", () => $"hoverMode: {hoverMode}, current flightMode: {this.flightMode}");
            var vehicle = Vehicle;
            if (hoverMode)
            {
                SetTarget(vehicle.Position);
                if (InAir is false)
                {
                    curPosition = vehicle.DrawTracker.DrawPos;
                    CurAngle = vehicle.FullRotation.AsAngle - FlightAngleOffset;
                }
                if (vehicle.vehiclePather.Moving)
                {
                    vehicle.vehiclePather.StopDead();
                }
            }
            faceTarget = LocalTargetInfo.Invalid;
            this.flightMode = hoverMode ? FlightMode.Hover : FlightMode.Flight;
        }

        public void SetTarget(LocalTargetInfo targetInfo)
        {
            LogOnce(() => "SetTarget", () => $"targetInfo: {targetInfo}, previous target: {this.target}");
            this.target = targetInfo;
            this.reachedInitialTarget = false;
            ResetFlightData();
            var vehicle = Vehicle;
            if (vehicle.jobs.curDriver is JobDriver_Goto)
            {
                vehicle.vehiclePather.PatherFailed();
            }
        }

        private void ResetFlightData()
        {
            LogOnce(() => "ResetFlightData", () => $"orbitClockwise: {orbitClockwise}, orbitInitialized: {orbitInitialized}");
            orbitClockwise = true;
            orbitInitialized = false;
            runwayStartingSpot = LocalTargetInfo.Invalid;
            landingSpot = LocalTargetInfo.Invalid;
            targetForRunway = null;
            landingStage = LandingStage.Inactive;
            designatedLandingRotation = null;
        }

        public void OrderLanding(IntVec3 finalCell, Rot4 finalRotation)
        {
            this.landingSpot = finalCell;
            this.designatedLandingRotation = finalRotation;

            float runwayDistance = 0f;

            if (Props.moveWhileLanding)
            {
                float curTakeoffProgress = 1f;
                while (curTakeoffProgress > 0)
                {
                    curTakeoffProgress = Mathf.Max(0, curTakeoffProgress - (1 / (float)Props.landingTicks));
                    runwayDistance += Props.flightSpeedPerTick * curTakeoffProgress;
                }
            }

            var direction = finalRotation.FacingCell.ToVector3();
            Vector3 touchDownVec = finalCell.ToVector3Shifted() - (direction * runwayDistance);
            this.runwayStartingSpot = new LocalTargetInfo(touchDownVec.ToIntVec3());

            if (flightMode != FlightMode.Flight)
            {
                this.SetFlightMode(true);
            }

            this.targetForRunway = null;
            this.landingStage = LandingStage.Inactive;

            LogAlways(() => "OrderLanding", () => $"Landing at {landingSpot}, touchdown {runwayStartingSpot}, dist {runwayDistance}, rot {finalRotation}");
        }

        private void SetChaseMode(ChaseMode mode, FlightPattern pattern = FlightPattern.Around)
        {
            LogOnce(() => "SetChaseMode", () => $"mode: {mode}, pattern: {pattern}, previous mode: {currentChaseMode}, previous pattern: {flightPattern}");
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
                LogAlways(() => "CompTick_Start", () => $"InAir: true, flightMode: {flightMode}, takeoffProgress: {takeoffProgress:F3}, TakingOff: {TakingOff}, Landing: {Landing}, Hovering: {Hovering}, Flying: {Flying}");

                var vehicle = Vehicle;
                if (vehicle.DrawTracker?.renderer is IParallelRenderer renderer)
                {
                    if (curPosition != lastRenderPosition || CurAngle != lastRenderAngle)
                    {
                        renderer.IsDirty = true;
                        lastRenderPosition = curPosition;
                        lastRenderAngle = CurAngle;
                    }
                }

                if (vehicle.CompFueledTravel != null && Props.fuelConsumptionPerTick > 0)
                {
                    if (vehicle.CompFueledTravel.Fuel < Props.fuelConsumptionPerTick)
                    {
                        if (shouldCrash is false)
                        {
                            SetToCrash();
                        }
                    }
                    else
                    {
                        vehicle.CompFueledTravel.ConsumeFuel(Props.fuelConsumptionPerTick);
                    }
                }

                if (CanFly is false && shouldCrash is false)
                {
                    LogOnce(() => "CompTick", () => "CanFly is false, setting to crash");
                    SetToCrash();
                }

                if (TakingOff is false && !reachedInitialTarget && curPosition.ToIntVec3().InBounds(vehicle.Map)
                    && OccupiedRect().Contains(target.Cell))
                {
                    reachedInitialTarget = true;
                    LogOnce(() => "CompTick", () => "Reached initial target");
                }

                if (InAIMode && !GoingToWorld)
                {
                    AITick();
                }

                if (TakingOff)
                {
                    LogOnce(() => "CompTick", () => "Taking off");
                    Takeoff();
                }
                else if (Landing)
                {
                    LogAlways(() => "CompTick_Landing", () => $"Starting landing. takeoffProgress: {takeoffProgress:F3}, runwayStartingSpot: {runwayStartingSpot}, landingSpot: {landingSpot}");

                    var takeoffOffset = (1 / (float)Props.landingTicks);
                    if (shouldCrash)
                    {
                        takeoffOffset *= 2f;
                    }
                    takeoffProgress = Mathf.Max(0, takeoffProgress - takeoffOffset);

                    LogOnce(() => "CompTick_Landing_Progress", () => $"takeoffOffset: {takeoffOffset:F4}, new takeoffProgress: {takeoffProgress:F3}, moveWhileLanding: {Props.moveWhileLanding}");

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

                    LogOnce(() => "CompTick_Landing_Move", () => $"takeoffProgress: {takeoffProgress:F3}, shouldCrash: {shouldCrash}");

                    if (takeoffProgress == 0)
                    {
                        LogOnce(() => "CompTick", () => "Landing progress reached 0, calling FlightEnd");
                        FlightEnd();
                    }
                    else
                    {
                        LogOnce(() => "CompTick", () => $"Landing not complete, takeoffProgress: {takeoffProgress:F3}");
                    }
                }
                else if (Hovering)
                {
                    LogOnce(() => "CompTick", () => "Hovering");
                    Hover();
                }
                else if (Flying)
                {
                    Flight();
                }

                ProcessRotors();
                SpawnFlecks();

                var curPositionIntVec = curPosition.ToIntVec3();
                if (curPositionIntVec != vehicle.Position && curPositionIntVec.InBounds(vehicle.Map))
                {
                    var occupiedRect = vehicle.OccupiedRect();
                    if (occupiedRect.MovedBy(curPositionIntVec.ToIntVec2 - vehicle.Position.ToIntVec2).InBoundsLocal(vehicle.Map))
                    {
                        try
                        {
                            vehicle.positionInt = curPositionIntVec;
                            vehicle.vehiclePather.nextCell = curPositionIntVec;
                            bool shouldRefreshCosts = false;
                            foreach (var cell in occupiedRect.ExpandedBy(vehicle.RotatedSize.x))
                            {
                                if (cell.InBounds(vehicle.Map))
                                {
                                    var grid = vehicle.Map.thingGrid.thingGrid[vehicle.Map.cellIndices.CellToIndex(cell)];
                                    Thing vehicleThing = null;
                                    foreach (var thing in grid)
                                    {
                                        if (thing is VehiclePawn vp && vp == vehicle)
                                        {
                                            vehicleThing = vp;
                                            break;
                                        }
                                    }
                                    if (vehicleThing != null && vehicle.OccupiedRect().Contains(cell) is false)
                                    {
                                        grid.Remove(vehicleThing);
                                        shouldRefreshCosts = true;
                                    }
                                }
                            }
                            if (shouldRefreshCosts)
                            {
                                PathingHelper.RecalculateAllPerceivedPathCosts(vehicle.Map);
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
                if (InAir)
                {
                    UpdateVehicleAngleAndRotation();
                }
            }
        }

        private void FlightEnd()
        {
            var vehicle = Vehicle;
            foreach (var turret in VehicleTurrets)
            {
                turret.parentRotCached = vehicle.Rotation;
                turret.parentAngleCached = vehicle.Angle;
            }
            if (shouldCrash)
            {
                shouldCrash = false;
                var damageAmount = (vehicle.GetMass() + MassUtility.GearAndInventoryMass(vehicle)) * 20f;
                var components = vehicle.statHandler.components
                    .Where(x => x.props.depth == VehicleComponent.VehiclePartDepth.External).ToList();
                damageAmount /= components.Count;
                foreach (var component in components)
                {
                    component.TakeDamage(vehicle, new DamageInfo(DamageDefOf.Blunt, damageAmount), ignoreArmor: true);
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
            var vehicle = Vehicle;
            var launcher = vehicle.CompVehicleLauncher;
            if (launcher != null)
            {
                var props = launcher.Props.launchProtocol.LaunchProperties as PropellerProtocolProperties;
                if (props?.angularVelocityPropeller != null)
                {
                    var rotationRate = props.angularVelocityPropeller.Evaluate(takeoffProgress);
                    vehicle.DrawTracker.overlayRenderer?.SetAcceleration(rotationRate);
                }
            }
        }

        private void GotoWorld()
        {
            var vehicle = Vehicle;
            var map = vehicle.Map;
            vehicle.Angle = 0;
            vehicle.DeSpawn();
            if (vehicle.Faction == Faction.OfPlayer)
            {
                Messages.Message("VF_AerialVehicleLeft".Translate(vehicle.LabelShort), MessageTypeDefOf.PositiveEvent);
            }
            var aerialVehicle = AerialVehicleInFlight.Create(vehicle, map.Tile);
            aerialVehicle.OrderFlyToTiles(new List<FlightNode>(flightPath), arrivalAction);
            if (orderRecon)
            {
                aerialVehicle.flightPath.ReconCircleAt(flightPath.LastOrDefault().tile);
            }

            Find.WorldPawns.PassToWorld(vehicle);
            foreach (var pawn in vehicle.AllPawnsAboard)
            {
                if (!pawn.IsWorldPawn())
                {
                    Find.WorldPawns.PassToWorld(pawn);
                }
            }
            foreach (var thing in vehicle.inventory.innerContainer)
            {
                if (thing is Pawn pawn && !pawn.IsWorldPawn())
                {
                    Find.WorldPawns.PassToWorld(pawn);
                }
            }
            vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLeftMap].ExecuteEvents();
            arrivalAction = null;
            flightPath = null;
        }

        private void AITick()
        {
            LogOnce(() => "AITick", () => $"target: {target}, faceTarget: {faceTarget}");
            if (cachedAmmoTypes == null)
            {
                cachedAmmoTypes = new Dictionary<VehicleTurret, ThingDef>();
            }
            var vehicle = Vehicle;
            foreach (var turret in VehicleTurrets)
            {
                if (turret.HasAmmo is false)
                {
                    ThingDef ammoType;
                    if (!cachedAmmoTypes.TryGetValue(turret, out ammoType))
                    {
                        ammoType = vehicle.inventory.innerContainer
                            .FirstOrDefault(t => turret.def.ammunition.Allows(t)
                            || turret.def.ammunition.Allows(t.def.projectileWhenLoaded))?.def;
                        cachedAmmoTypes[turret] = ammoType;
                    }
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
                        LogOnce(() => "AITick_BombCooldown", () => "Bomb cooldown active, returning");
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
                        if (vehicle.Position.DistanceTo(curTarget.Cell) < bomberSettings.distanceFromTarget)
                        {
                            var bombOption = Props.bombOptions.Where(x => bomberSettings.blacklistedBombs.Contains(x.projectile) is false).RandomElement();
                            if (TryDropBomb(bombOption))
                            {
                                LogOnce(() => "AITick_DroppedBomb", () => $"Dropped bomb: {bombOption.projectile}");
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
                    LogOnce(() => "AITick_curTarget", () => $"curTarget: {curTarget}{(curTarget.HasThing ? $" (Thing: {curTarget.Thing})" : "")}");
                    if (curTarget.IsValid && curTarget.HasThing)
                    {
                        Thing previousTargetThing = null;
                        if (gunshipSettings.chaseMode == ChaseMode.Hovering && faceTarget.HasThing)
                            previousTargetThing = faceTarget.Thing;
                        else if (gunshipSettings.chaseMode != ChaseMode.Hovering && target.HasThing)
                            previousTargetThing = target.Thing;

                        if (previousTargetThing != null && previousTargetThing != curTarget.Thing)
                        {
                            LogAlways(() => "AITick_TargetSwitch", () => $"Target switched from {previousTargetThing} to {curTarget.Thing}");
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
            var vehicle = Vehicle;
            var targets = vehicle.Map.attackTargetsCache.GetPotentialTargetsFor(vehicle).Select(x => x.Thing)
                .Concat(vehicle.Map.listerThings.ThingsInGroup(ThingRequestGroup.PowerTrader));

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
            var vehicle = Vehicle;
            if (x.Position.GetRoof(vehicle.Map) == RoofDefOf.RoofRockThick)
            {
                return false;
            }
            if (x is Pawn pawn && (pawn.Downed || pawn.Dead))
            {
                return false;
            }
            return x.HostileTo(vehicle) || x.Faction != null && x.Faction.HostileTo(vehicle.Faction);
        }

        private int nearbyCombatPointsCacheTick = -1;
        private Dictionary<Thing, float> nearbyCombatPointsCache = new Dictionary<Thing, float>();

        private float NearbyAreaCombatPoints(Thing x)
        {
            if (Find.TickManager.TicksGame != nearbyCombatPointsCacheTick)
            {
                nearbyCombatPointsCache.Clear();
                nearbyCombatPointsCacheTick = Find.TickManager.TicksGame;
            }
            if (nearbyCombatPointsCache.TryGetValue(x, out float cachedValue))
            {
                return cachedValue;
            }
            float value = GenRadial.RadialDistinctThingsAround(x.Position, x.Map, 10, true).Where(t => IsValidTarget(t)).Sum(y => CombatPoints(y));
            nearbyCombatPointsCache[x] = value;
            return value;
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
            var vehicle = Vehicle;
            if (HasStuffToBomb(bombOption))
            {
                foreach (var thingCost in bombOption.costList)
                {
                    var countToTake = thingCost.count;
                    foreach (var thing in vehicle.inventory.GetDirectlyHeldThings().Where(x => x.def == thingCost.thingDef).ToList())
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
                var bomb = (Projectile)GenSpawn.Spawn(bombOption.projectile, vehicle.Position + IntVec3.North, vehicle.Map);
                bomb.Launch(vehicle, vehicle.Position, vehicle.Position, ProjectileHitFlags.IntendedTarget, equipment: vehicle);
                lastBombardmentTick = Find.TickManager.TicksGame;
                bombingCooldownTicks = bombOption.cooldownTicks;
                return true;
            }
            return false;
        }

        private bool HasStuffToBomb(BombOption bombOption)
        {
            var vehicle = Vehicle;
            return bombOption.costList.All(thingCost => vehicle.inventory.GetDirectlyHeldThings()
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
            var vehicle = Vehicle;
            if (flecks != null)
            {
                foreach (var fleck in flecks)
                {
                    if (vehicle.IsHashIntervalTick(fleck.spawnTickRate))
                    {
                        SpawnFleck(fleck);
                    }
                }
            }
        }

        public void SpawnFleck(FlightFleckData fleckData)
        {
            var vehicle = Vehicle;
            var fleckPos = fleckData.position.RotatedBy(CurAngle);
            var loc = curPosition - fleckPos;
            var data = FleckMaker.GetDataStatic(loc, vehicle.Map, fleckData.fleck, fleckData.scale);
            if (fleckData.attachToVehicle)
            {
                data.link = new FleckAttachLink(vehicle);
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
            vehicle.Map.flecks.CreateFleck(data);
        }

        private void UpdateVehicleAngleAndRotation()
        {
            var vehicle = Vehicle;
            vehicle.Angle = AngleAdjusted(CurAngle + FlightAngleOffset);
            if (vehicle.Transform != null)
            {
                vehicle.Transform.rotation = vehicle.Angle;
            }

            UpdateRotation();
        }

        public List<IntVec3> GetRunwayCells(bool takingOff)
        {
            var vehicle = Vehicle;
            var position = vehicle.Position;
            var rot = vehicle.FullRotation;
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
                pos = AdjustPos(InAir ? rotInAir : vehicle.FullRotation, pos);
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

        public List<IntVec3> GetLandingRunwayCells(IntVec3 finalCell, Rot4 finalRotation)
        {
            var cells = new List<IntVec3>();

            var distance = 0f;

            if (Props.moveWhileLanding)
            {
                var curTakeoffProgress = 1f;
                while (curTakeoffProgress > 0)
                {
                    curTakeoffProgress = Mathf.Max(0, curTakeoffProgress - (1 / (float)Props.landingTicks));
                    distance += Props.flightSpeedPerTick * curTakeoffProgress;
                }
            }

            var angle = finalRotation.AsAngle;
            var vehicle = Vehicle;
            var width = vehicle.def.Size.x;
            var vehicleRot = Rot8.FromAngle(angle);

            var direction = finalRotation.FacingCell.ToVector3();
            var runwayStartVec = finalCell.ToVector3Shifted() - (direction * distance);
            var runwayStartPos = runwayStartVec.ToIntVec3();

            var north = IntVec3.North.ToVector3();

            for (var i = 1; i <= width; i++)
            {
                var pos = CellOffset(runwayStartPos, i, width, angle);
                pos = AdjustPos(vehicleRot, pos);

                cells.Add(pos);
                for (var j = 0; j < distance; j++)
                {
                    var offsetRotated = (north * j).RotatedBy(angle);
                    var cell = pos + offsetRotated.ToIntVec3();
                    cells.Add(cell);
                }
            }
            FillGaps(cells);

            var finalRect = GenAdj.OccupiedRect(finalCell, finalRotation, vehicle.def.Size);
            foreach(var c in finalRect)
            {
                if(!cells.Contains(c)) cells.Add(c);
            }

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
            var vehicle = Vehicle;
            var cells = new List<IntVec3>();
            foreach (var cell in runwayCells)
            {
                if (cell.InBounds(vehicle.Map))
                {
                    var terrain = cell.GetTerrain(vehicle.Map);
                    if (Props.canFlyInSpace && terrain == TerrainDefOf.Space)
                    {
                        continue;
                    }
                    if (vehicle.Drivable(cell) is false || cell.GetThingList(vehicle.Map).Any(x => x is Plant && x.def.plant.IsTree || x is Building))
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
            LogOnce(() => "Hover", () => $"CurAngle: {CurAngle}, curPosition: {curPosition}, target: {target}, faceTarget: {faceTarget}, reachedInitialTarget: {reachedInitialTarget}, targetPos: {hoverTargetPos}, distance: {hoverDist:F2}");

            if (!reachedInitialTarget || (target.IsValid && target.Cell != Vehicle.Position))
            {
                float angleDiff;
                if (faceTarget.IsValid)
                {
                    angleDiff = RotateTowards(faceTarget.CenterVector3);

                    var alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                    var speed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);

                    MoveTowards(target.CenterVector3, speed);
                }
                else
                {
                    angleDiff = RotateTowards(target.CenterVector3);

                    var alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                    var speed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);

                    MoveFurther(speed);
                }
            }
            else if (faceTarget.IsValid)
            {
                var angleDiff = RotateTowards(faceTarget.CenterVector3);
                if (InAIMode)
                {
                    var vehicle = Vehicle;
                    var distance = faceTarget.Cell.DistanceTo(vehicle.Position);
                    var targetDist = Props.AISettings.gunshipSettings.distanceFromTarget;

                    var distError = Mathf.Abs(distance - targetDist);
                    var speedMult = Mathf.Clamp01(distError / 5f);
                    var targetSpeed = Props.flightSpeedTurningPerTick * speedMult;

                    var alignmentFactor = Mathf.Clamp01(1f - ((angleDiff - 15f) / 180f));
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
                LogOnce(() => "Flight", () => "Going to world");
                MoveFurther(Props.flightSpeedPerTick);
            }
            else if (runwayStartingSpot.IsValid)
            {
                LogOnce(() => "Flight", () => "Moving to landing spot");
                MoveToLandingSpot();
            }
            else if (target.IsValid)
            {
                if (Find.TickManager.TicksGame < yieldUntilTick)
                {
                    LogOnce(() => "Flight", () => "Yielding - waiting");
                    MoveFurther(Props.flightSpeedTurningPerTick);
                    return;
                }

                if (ShouldYieldToOther())
                {
                    LogOnce(() => "Flight", () => "Yielding to other vehicle");
                    yieldUntilTick = Find.TickManager.TicksGame + 30;
                    MoveFurther(Props.flightSpeedTurningPerTick);
                    return;
                }

                LogOnce(() => "Flight", () => $"Chasing target: {target}");
                MoveInChaseMode(target);
            }
            else
            {
                LogOnce(() => "Flight", () => "No flight target - hovering");
            }
        }

        private bool ShouldYieldToOther()
        {
            var vehicle = Vehicle;
            float myRadius = (vehicle.def.Size.x + vehicle.def.Size.z) / 2f;

            Vector3 myDir = (Quaternion.AngleAxis(CurAngle, Vector3.up)
                * Vector3.forward).RotatedBy(FlightAngleOffset).normalized;

            var others = GetFlightVehicles().Where(c => c.Vehicle.thingIDNumber < vehicle.thingIDNumber).ToList();

            foreach (var otherComp in others)
            {
                var otherVehicle = otherComp.Vehicle;
                float otherRadius = (otherVehicle.def.Size.x + otherVehicle.def.Size.z) / 2f;
                float yieldRadius = (myRadius + otherRadius) * 2f;

                Vector3 toOther = otherComp.curPosition.Yto0() - curPosition.Yto0();
                float dist = toOther.magnitude;

                if (dist >= yieldRadius) continue;

                float dotAhead = Vector3.Dot(myDir, toOther.normalized);
                if (dotAhead < 0.3f) continue;

                Vector3 relVel = currentVelocity.Yto0() - otherComp.currentVelocity.Yto0();
                float closingSpeed = Vector3.Dot(relVel, toOther.normalized);
                if (closingSpeed < 0f) continue;

                return true;
            }
            return false;
        }

        private void MoveToLandingSpot()
        {
            Vector3 runwayDir = (landingSpot.CenterVector3 - runwayStartingSpot.CenterVector3).normalized;
            if (runwayDir.sqrMagnitude < 0.001f)
                runwayDir = designatedLandingRotation.HasValue
                    ? designatedLandingRotation.Value.FacingCell.ToVector3()
                    : Vector3.forward;

            var targetAngleFlat = runwayDir.AngleFlat();
            var targetCurAngle = AngleAdjusted(targetAngleFlat - FlightAngleOffset);

            float turnRadius = Props.flightSpeedPerTick / Mathf.Max(Props.turnAnglePerTick * Mathf.Deg2Rad, 0.001f);

            if (landingStage == LandingStage.Inactive)
            {
                float approachDist = Props.moveWhileLanding
                    ? Mathf.Max(30f, turnRadius * 3f)
                    : Mathf.Max(15f, turnRadius * 2f);

                targetForRunway = runwayStartingSpot.CenterVector3 - (runwayDir * approachDist);
                landingStage = LandingStage.GotoInitialSpot;
                LogAlways(() => "MoveToLandingSpot", () => $"Calculated approach point: {targetForRunway.Value}. Stage -> GotoInitialSpot.");
            }

            if (landingStage == LandingStage.GotoInitialSpot)
            {
                var distToApproach = Vector3.Distance(curPosition.Yto0(), targetForRunway.Value.Yto0());

                var angleDiff = RotateTowards(targetForRunway.Value);
                var alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 45f));
                var speed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);

                MoveFurther(speed);

                var hitRadius = Mathf.Max(5f, turnRadius * 0.75f);

                if (distToApproach < hitRadius || (distToApproach < hitRadius * 2f && angleDiff > 90f))
                {
                    landingStage = LandingStage.GotoRunwayStartSpot;
                    LogAlways(() => "MoveToLandingSpot", () => "Reached approach point. Stage -> GotoRunwayStartSpot.");
                }
            }

            else if (landingStage == LandingStage.GotoRunwayStartSpot)
            {
                var distToStart = Vector3.Distance(curPosition.Yto0(), runwayStartingSpot.CenterVector3.Yto0());

                var angleToSpot = GetAngleFromTarget(runwayStartingSpot.CenterVector3);
                var angleDiff = RotateTo(angleToSpot, Props.turnAnglePerTick);

                var alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 45f));
                var baseSpeed = Mathf.Lerp(Props.flightSpeedTurningPerTick, Props.flightSpeedPerTick, alignment);
                float speed = baseSpeed;

                if (Props.moveWhileLanding)
                {
                    speed = Mathf.Lerp(Props.flightSpeedPerTick * 0.4f, baseSpeed, Mathf.Clamp01(distToStart / 15f));
                }
                else
                {
                    speed = Mathf.Lerp(Props.flightSpeedPerTick * 0.2f, baseSpeed, Mathf.Clamp01(distToStart / 8f));
                }

                MoveFurther(speed);

                float hitRadius = Props.moveWhileLanding ? 3f : 1.5f;

                LogOnce(() => "GotoRunwayStartSpot", () => $"distToStart: {distToStart:F2}, angleToSpot: {angleToSpot:F1}, curAngle: {CurAngle:F1}, angleDiff: {angleDiff:F1}, speed: {speed:F3}");

                if (distToStart < hitRadius || (distToStart < turnRadius && angleDiff > 90f))
                {
                    landingStage = LandingStage.GotoLanding;
                    LogAlways(() => "MoveToLandingSpot", () => "Reached runway start. Stage -> GotoLanding.");
                }
            }

            else if (landingStage == LandingStage.GotoLanding)
            {
                var angleDiff = RotateTo(targetCurAngle, Props.turnAnglePerTick);

                takeoffProgress = Mathf.Max(0, takeoffProgress - (1f / (float)Props.landingTicks));

                float actualSpeed = Props.moveWhileLanding
                    ? Props.flightSpeedPerTick * takeoffProgress
                    : 0f;

                MoveFurther(actualSpeed);

                var distToLanding = Vector3.Distance(curPosition.Yto0(), landingSpot.CenterVector3.Yto0());
                LogOnce(() => "GotoLanding", () => $"progress: {takeoffProgress:F3}, speed: {actualSpeed:F3}, curAngle: {CurAngle:F1}, targetAngle: {targetCurAngle:F1}, distToLanding: {distToLanding:F2}");

                if (takeoffProgress <= 0)
                {
                    SetFlightMode(false);
                    FlightEnd();
                    LogAlways(() => "MoveToLandingSpot", () => "Landing complete.");
                }
            }
        }

        private bool ShouldRotate(out bool? clockturn)
        {
            clockturn = null;
            var speedPerTick = Props.flightSpeedPerTick;
            var dist = speedPerTick;
            var curAngle = CurAngle;
            var angleAxis = Quaternion.AngleAxis(curAngle, Vector3.up);
            var minAngle = float.MinValue;
            var targetAngle = AngleAdjusted((landingSpot.CenterVector3 - runwayStartingSpot.CenterVector3).AngleFlat() - FlightAngleOffset);

            LogOnce(() => "ShouldRotate", () => $"targetAngle: {targetAngle}, curAngle: {curAngle}");

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
                        LogOnce(() => "ShouldRotate", () => "No rotation needed (clockturn is null)");
                        return false;
                    }
                    else
                    {
                        var turnoffset = clockturn.Value ? Props.turnAnglePerTick : -Props.turnAnglePerTick;
                        var ticksPassedSimulated = (int)(dist / speedPerTick);
                        LogOnce(() => "ShouldRotate", () => $"Simulating {ticksPassedSimulated} ticks, turnoffset: {turnoffset}");
                        for (var i = 0; i < ticksPassedSimulated; i++)
                        {
                            curAngle = AngleAdjusted(curAngle + turnoffset);
                            var angleInRange = AngleInRange(curAngle, AngleAdjusted(targetAngle - Props.turnAnglePerTick), AngleAdjusted(targetAngle + Props.turnAnglePerTick));
                            if (angleInRange && i >= ticksPassedSimulated - 3)
                            {
                                LogOnce(() => "ShouldRotate", () => $"Should rotate at tick {i}, curAngle: {curAngle}");
                                return true;
                            }
                        }
                    }
                    LogOnce(() => "ShouldRotate", () => "Rotation not possible within simulation");
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
            LogOnce(() => "AngleInRange", () => $"angle: {angle}, lower: {lower}, upper: {upper}, result: {result}");
            return result;
        }

        private void Takeoff()
        {
            LogOnce(() => "Takeoff", () => $"takeoffProgress: {takeoffProgress}");
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

        private bool CanLand()
        {
            var vehicle = Vehicle;
            return OccupiedRect().All(x => vehicle.Drivable(x));
        }

        public void UpdateRotation()
        {
            var vehicle = Vehicle;
            if (vehicle.rotationInt != FlightRotation)
            {
                vehicle.rotationInt = FlightRotation;
            }
        }

        public List<IntVec3> OccupiedRect()
        {
            if (InAir)
            {
                var vehicle = Vehicle;
                var angle = AngleAdjusted(CurAngle + FlightAngleOffset);
                vehicle.rotationInt = Rot8.FromAngle(angle);
                var cells = vehicle.VehicleRect().Cells.ToList();
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
                var dist = Vector3.Distance(curPosition.Yto0(), stopTarget.Value.Yto0());
                if (dist < minDistanceFromTarget.Value)
                {
                    targetSpeed = 0f;
                }
            }

            Vector3 desiredVelocity = direction.normalized * targetSpeed;

            if (currentVelocity.sqrMagnitude > 0.0001f && desiredVelocity.sqrMagnitude > 0.0001f)
            {
                var maxTurnSpeed = Mathf.Max(Props.turnAnglePerTick * 2f, Mathf.Abs(angularVelocity) * 1.5f);
                float turnSpeedRadians = maxTurnSpeed * Mathf.Deg2Rad;
                var rotatedVelocityDir = Vector3.RotateTowards(currentVelocity.normalized, desiredVelocity.normalized, turnSpeedRadians, 0f);
                currentVelocity = rotatedVelocityDir * currentVelocity.magnitude;
            }

            float acceleration = 0.015f;
            if (targetSpeed <= 0f)
            {
                acceleration = 0.04f;
            }
            else if (currentVelocity.sqrMagnitude > 0.001f)
            {
                var angleShift = Vector3.Angle(currentVelocity.normalized, direction.normalized);
                acceleration = Mathf.Lerp(0.015f, 0.05f, angleShift / 90f);
            }

            currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration);

            Vector3 newPosition = curPosition + currentVelocity;
            curPosition = new Vector3(newPosition.x, Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays), newPosition.z);
        }

        private void MoveFurther(float speed, float? minDistanceFromTarget = null, Vector3? stopTarget = null)
        {
            ComputeAvoidance();
            var dir = (Quaternion.AngleAxis(CurAngle, Vector3.up) * Vector3.forward).RotatedBy(FlightAngleOffset);
            MoveDirection(dir, speed * cachedAvoidanceSpeedMult, minDistanceFromTarget, stopTarget);
        }

        private void MoveBack(float speed, float? minDistanceFromTarget = null, Vector3? stopTarget = null)
        {
            var dir = (Quaternion.AngleAxis(CurAngle, Vector3.up) * Vector3.back).RotatedBy(FlightAngleOffset);
            MoveDirection(dir, speed, minDistanceFromTarget, stopTarget);
        }

        private void MoveTowards(Vector3 to, float targetSpeed, float? minDistanceFromTarget = null, Vector3? stopTarget = null)
        {
            var toFlat = to.Yto0();
            var curFlat = curPosition.Yto0();
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
            var radiusAngle = toPlane.AngleFlat();

            var tangentCW = AngleAdjusted(radiusAngle + 180f);
            var tangentCCW = AngleAdjusted(radiusAngle);

            var diffCW = Mathf.Abs(Utils.AngleDiff(CurAngle, tangentCW));
            var diffCCW = Mathf.Abs(Utils.AngleDiff(CurAngle, tangentCCW));

            if (Mathf.Abs(diffCW - diffCCW) > 15f)
                orbitClockwise = diffCW <= diffCCW;

            float targetAngle = orbitClockwise ? tangentCW : tangentCCW;
            float distError = currentDist - desiredRadius;
            var correction = Mathf.Clamp(distError * 0.8f, -25f, 25f);

            targetAngle = orbitClockwise
                ? AngleAdjusted(targetAngle + correction)
                : AngleAdjusted(targetAngle - correction);

            float avoidanceStrength = 1.0f;

            if (flightPattern == FlightPattern.Over)
            {
                var distToTarget = Vector3.Distance(curPosition.Yto0(), realTarget.Yto0());

                if (distToTarget < 35f)
                {
                    avoidanceStrength = Mathf.Clamp01((distToTarget - 15f) / 20f);

                    if (distToTarget <= 3f)
                    {
                        targetAngle = CurAngle;
                    }
                    else
                    {
                        var directAngle = GetAngleFromTarget(realTarget);
                        var diffToDirect = Utils.AngleDiff(targetAngle, directAngle);

                        if (Mathf.Abs(diffToDirect) < 75f)
                        {
                            var blend = Mathf.Clamp01((35f - distToTarget) / 15f);
                            targetAngle = AngleAdjusted(targetAngle + diffToDirect * blend);
                        }
                    }
                }
            }

            return RotateTo(targetAngle, turnRate, avoidanceStrength);
        }

        private List<CompFlightMode> GetFlightVehicles()
        {
            var vehicle = Vehicle;
            var result = new List<CompFlightMode>();
            foreach (var def in Utils.flightCapableDefs)
            {
                foreach (var thing in vehicle.Map.listerThings.ThingsOfDef(def))
                {
                    if (thing is VehiclePawn v && v != vehicle && v.GetComp<CompFlightMode>() is CompFlightMode c && c.InAir)
                    {
                        result.Add(c);
                    }
                }
            }
            return result;
        }

        private void ComputeAvoidance()
        {
            if (avoidanceComputedTick == Find.TickManager.TicksGame) return;
            avoidanceComputedTick = Find.TickManager.TicksGame;
            cachedAvoidanceBias = 0f;
            cachedAvoidanceSpeedMult = 1f;

            var vehicle = Vehicle;
            var others = GetFlightVehicles();

            float myRadius = (vehicle.def.Size.x + vehicle.def.Size.z) / 2f;
            const float LookaheadTicks = 60f;

            float worstStrength = 0f;

            foreach (var otherComp in others)
            {
                var otherVehicle = otherComp.Vehicle;
                if (target.IsValid && target.HasThing && otherVehicle == target.Thing) continue;
                if (faceTarget.IsValid && faceTarget.HasThing && otherVehicle == faceTarget.Thing) continue;

                float otherRadius = (otherVehicle.def.Size.x + otherVehicle.def.Size.z) / 2f;
                float dangerRadius = (myRadius + otherRadius) * 3f;

                Vector3 relPos = curPosition.Yto0() - otherComp.curPosition.Yto0();
                float currentDist = relPos.magnitude;

                Vector3 relVel = currentVelocity.Yto0() - otherComp.currentVelocity.Yto0();
                float relSpeedSq = relVel.sqrMagnitude;

                float closestTime, closestDist;
                if (relSpeedSq < 0.0001f)
                {
                    closestTime = 0f;
                    closestDist = currentDist;
                }
                else
                {
                    closestTime = Mathf.Clamp(-Vector3.Dot(relPos, relVel) / relSpeedSq, 0f, LookaheadTicks);
                    closestDist = (relPos + relVel * closestTime).magnitude;
                }

                float effectiveDist = currentDist < dangerRadius ? Mathf.Min(currentDist, closestDist) : closestDist;

                if (effectiveDist >= dangerRadius) continue;

                float urgency = currentDist < dangerRadius
                    ? 1f
                    : 1f - (closestTime / LookaheadTicks);

                float penetration = 1f - (effectiveDist / dangerRadius);
                float strength = penetration * Mathf.Lerp(0.1f, 1f, urgency);

                if (strength <= worstStrength) continue;
                worstStrength = strength;

                Vector3 toOther = (otherComp.curPosition.Yto0() - curPosition.Yto0()).normalized;
                if (toOther.sqrMagnitude < 0.001f) toOther = Vector3.right;

                Vector3 perpCW  = new Vector3( toOther.z, 0f, -toOther.x);
                Vector3 perpCCW = new Vector3(-toOther.z, 0f,  toOther.x);

                Vector3 myDir = (Quaternion.AngleAxis(CurAngle, Vector3.up)
                    * Vector3.forward).RotatedBy(FlightAngleOffset).normalized;

                Vector3 avoidDir = Vector3.Dot(myDir, perpCW) >= Vector3.Dot(myDir, perpCCW)
                    ? perpCW : perpCCW;

                float avoidAngle = AngleAdjusted(avoidDir.AngleFlat() + FlightAngleOffset);

                cachedAvoidanceBias = Utils.AngleDiff(CurAngle, avoidAngle) * strength * 0.4f;
                cachedAvoidanceSpeedMult = Mathf.Lerp(1f, 0.6f, strength);
            }
        }

        private float RotateTo(float targetAngle, float turnRate, float avoidanceStrength = 1.0f)
        {
            ComputeAvoidance();
            var adjustedTarget = AngleAdjusted(targetAngle + cachedAvoidanceBias * avoidanceStrength);

            var diff = Utils.AngleDiff(CurAngle, adjustedTarget);
            var absDiff = Mathf.Abs(diff);

            float proportionalSpeed = diff * 0.3f;
            var desiredAngularVelocity = Mathf.Clamp(proportionalSpeed, -turnRate, turnRate);
            float acceleration = turnRate * 0.5f;
            angularVelocity = Mathf.MoveTowards(angularVelocity, desiredAngularVelocity, acceleration);
            CurAngle += angularVelocity;

            return absDiff;
        }

        private float RotateTowards(Vector3 target, float turnRateOverride = -1f)
        {
            var targetAngle = GetAngleFromTarget(target);
            float rate = turnRateOverride > 0f ? turnRateOverride : Props.turnAnglePerTick;
            return RotateTo(targetAngle, rate);
        }

        private float GetOrbitRadius()
        {
            float radius = Props.maxDistanceFromTargetCircle;
            if (InAIMode && Props.AISettings?.gunshipSettings != null)
                radius = Props.AISettings.gunshipSettings.distanceFromTarget;
            LogOnce(() => "GetOrbitRadius", () => $"radius: {radius}, InAIMode: {InAIMode}");
            return radius;
        }

        private (float a, float b) GetEllipseAxes()
        {
            var a = Props.ellipseMajorAxis;
            var b = Props.ellipseMinorAxis;

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

            LogOnce(() => "GetEllipseAxes", () => $"a: {a}, b: {b}, maxRadius: {maxRadius}, originalMajorAxis: {Props.ellipseMajorAxis}, originalMinorAxis: {Props.ellipseMinorAxis}");

            return (a, b);
        }

        private void InitOrbit(Vector3 targetPos)
        {
            var cur2D = curPosition.Yto0();
            var tgt2D = targetPos.Yto0();
            Vector3 toTarget = tgt2D - cur2D;
            float dist = toTarget.magnitude;
            Vector3 approachDir = dist > 0.001f ? toTarget / dist : Vector3.forward;

            float approachAngle = Mathf.Atan2(approachDir.x, approachDir.z) * Mathf.Rad2Deg;
            if (approachAngle < 0f) approachAngle += 360f;

            LogOnce(() => "InitOrbit_start", () => $"targetPos: {targetPos}, curPos: {cur2D}, curPosition: {curPosition}, dist: {dist}, approachAngle: {approachAngle}");

            if (currentChaseMode == ChaseMode.Circling && flightPattern == FlightPattern.Over)
            {
                var perpCW  = new Vector3( approachDir.z, 0f, -approachDir.x);
                var perpCCW = new Vector3(-approachDir.z, 0f,  approachDir.x);
                var chosenPerp = PickUnoccupiedSide(targetPos, perpCW, perpCCW, GetOrbitRadius());
                orbitOffsetDir = chosenPerp;
                orbitPerpOffset = chosenPerp * GetOrbitRadius();
                LogOnce(() => "InitOrbit", () => $"Circling/Over: orbitOffsetDir: {orbitOffsetDir}, orbitPerpOffset: {orbitPerpOffset}, orbitRadius: {GetOrbitRadius()}");
            }
            else if (currentChaseMode == ChaseMode.Elliptical && flightPattern == FlightPattern.Over)
            {
                var (a, b) = GetEllipseAxes();

                orbitOrientAngle = approachAngle;

                var perp = new Vector3(approachDir.z, 0f, -approachDir.x);
                orbitPerpOffset = perp * b;

                LogOnce(() => "InitOrbit", () => $"Elliptical/Over: orbitPerpOffset: {orbitPerpOffset}, orbitOrientAngle: {orbitOrientAngle}, a: {a}, b: {b}");
            }
            else if (currentChaseMode == ChaseMode.Elliptical && flightPattern == FlightPattern.Around)
            {
                orbitPerpOffset = Vector3.zero;
                orbitOrientAngle = approachAngle + 90f;
                LogOnce(() => "InitOrbit", () => $"Elliptical/Around: orbitOrientAngle: {orbitOrientAngle}");
            }

            orbitInitialized = true;
        }

        private void DoEllipseOrbit(Vector3 worldCenter, float a, float b, float orientDeg, Vector3 realTarget)
        {
            float baseSpeed = Props.flightSpeedCirclingPerTick ?? Props.flightSpeedTurningPerTick;
            var minCurveRadius = Mathf.Max((b * b) / Mathf.Max(a, 0.001f), 1f);
            float requiredTurnRate = (baseSpeed * 180f) / (Mathf.PI * minCurveRadius) * 1.5f;
            float configuredTurnRate = Props.turnAngleCirclingPerTick > 0f ? Props.turnAngleCirclingPerTick : Props.turnAnglePerTick;
            var turnRate = Mathf.Max(configuredTurnRate, requiredTurnRate);

            float oRad = orientDeg * Mathf.Deg2Rad;
            var mj = new Vector3(Mathf.Sin(oRad), 0f, Mathf.Cos(oRad));
            var mn = new Vector3(Mathf.Cos(oRad), 0f, -Mathf.Sin(oRad));

            var cur2D = curPosition.Yto0();
            var c2D = new Vector3(worldCenter.x, 0f, worldCenter.z);
            Vector3 d = cur2D - c2D;

            var localA = Vector3.Dot(d, mj);
            var localB = Vector3.Dot(d, mn);
            var theta = Mathf.Atan2(localB / b, localA / a);

            Vector3 ellipsePoint = c2D + (a * Mathf.Cos(theta)) * mj + (b * Mathf.Sin(theta)) * mn;

            float tangentAngleFlat = Mathf.Atan2((-a * Mathf.Sin(theta) * mj + b * Mathf.Cos(theta) * mn).normalized.x, (-a * Mathf.Sin(theta) * mj + b * Mathf.Cos(theta) * mn).normalized.z) * Mathf.Rad2Deg;
            var curAngleCW = AngleAdjusted(tangentAngleFlat + 90f);
            var curAngleCCW = AngleAdjusted(curAngleCW + 180f);

            if (Mathf.Abs(Mathf.Abs(Utils.AngleDiff(CurAngle, curAngleCW)) - Mathf.Abs(Utils.AngleDiff(CurAngle, curAngleCCW))) > 15f)
                orbitClockwise = Mathf.Abs(Utils.AngleDiff(CurAngle, curAngleCW)) <= Mathf.Abs(Utils.AngleDiff(CurAngle, curAngleCCW));

            float targetAngle = orbitClockwise ? curAngleCW : curAngleCCW;
            float distError = d.magnitude - (ellipsePoint - c2D).magnitude;
            var correction = Mathf.Clamp(distError * 0.8f, -25f, 25f);

            targetAngle = orbitClockwise ? AngleAdjusted(targetAngle + correction) : AngleAdjusted(targetAngle - correction);

            float avoidanceStrength = 1.0f;

            if (flightPattern == FlightPattern.Over)
            {
                var distToTarget = Vector3.Distance(curPosition.Yto0(), realTarget.Yto0());

                if (distToTarget < 35f)
                {
                    avoidanceStrength = Mathf.Clamp01((distToTarget - 15f) / 20f);

                    if (distToTarget <= 3f)
                    {
                        targetAngle = CurAngle;
                    }
                    else
                    {
                        var directAngle = GetAngleFromTarget(realTarget);
                        var diffToDirect = Utils.AngleDiff(targetAngle, directAngle);

                        if (Mathf.Abs(diffToDirect) < 75f)
                        {
                            var blend = Mathf.Clamp01((35f - distToTarget) / 15f);
                            targetAngle = AngleAdjusted(targetAngle + diffToDirect * blend);
                        }
                    }
                }
            }

            var angleDiff = RotateTo(targetAngle, turnRate, avoidanceStrength);

            float maxPhysicalSpeed = (turnRate * Mathf.PI * minCurveRadius) / 180f;
            var baseSpeedLimit = Mathf.Min(baseSpeed, maxPhysicalSpeed);
            var alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
            var finalSpeed = Mathf.Lerp(Mathf.Min(Props.flightSpeedTurningPerTick, baseSpeedLimit), baseSpeedLimit, alignment);

            MoveFurther(finalSpeed);
        }

        private void MoveInChaseMode(LocalTargetInfo chaseTarget)
        {
            var targetPos = chaseTarget.CenterVector3.Yto0();
            var distToTarget = Vector3.Distance(curPosition.Yto0(), targetPos);

            LogOnce(() => "MoveInChaseMode", () => $"chaseMode: {currentChaseMode}, flightPattern: {flightPattern}, targetPos: {targetPos}, distToTarget: {distToTarget:F2}, curPosition: {curPosition}");

            if (!orbitInitialized)
            {
                InitOrbit(targetPos);
            }

            switch (currentChaseMode)
            {
                case ChaseMode.Direct:
                    {
                        var angleToTarget = GetAngleFromTarget(target.CenterVector3);
                        float turnRate = Props.turnAnglePerTick > 0f ? Props.turnAnglePerTick : 1f;
                        float turnSpeed = Props.flightSpeedTurningPerTick > 0f ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick;

                        float minTurnRadius = (turnSpeed * 180f) / (Mathf.PI * turnRate);
                        var angleDiff = Mathf.Abs(Utils.AngleDiff(CurAngle, angleToTarget));

                        var avoidanceStrength = Mathf.Clamp01(1f - (angleDiff / 90f));

                        float rotateDiff = 0f;
                        if (!(distToTarget < minTurnRadius * 2.5f && angleDiff > 90f))
                        {
                            rotateDiff = RotateTo(angleToTarget, turnRate, avoidanceStrength);
                        }

                        var alignment = Mathf.Clamp01(1f - ((rotateDiff - 15f) / 45f));
                        var finalSpeed = Mathf.Lerp(turnSpeed, Props.flightSpeedPerTick, alignment);

                        MoveFurther(finalSpeed);
                        break;
                    }

                case ChaseMode.Circling:
                    {
                        var orbitRadius = GetOrbitRadius();
                        Vector3 orbitCenter = flightPattern == FlightPattern.Around
                            ? targetPos
                            : targetPos + orbitOffsetDir * orbitRadius;

                        float baseSpeed = Props.flightSpeedCirclingPerTick ?? Props.flightSpeedTurningPerTick;
                        float requiredTurnRate = (baseSpeed * 180f) / (Mathf.PI * Mathf.Max(orbitRadius, 1f)) * 1.5f;
                        var turnRate = Mathf.Max(Props.turnAngleCirclingPerTick > 0f ? Props.turnAngleCirclingPerTick : Props.turnAnglePerTick, requiredTurnRate);

                        LogOnce(() => "CirclingChase", () => $"orbitRadius: {orbitRadius}, orbitCenter: {orbitCenter}, baseSpeed: {baseSpeed}, turnRate: {turnRate}");

                        var angleDiff = RotatePerperticular(orbitCenter, orbitRadius, turnRate, targetPos);

                        float maxPhysicalSpeed = (turnRate * Mathf.PI * Mathf.Max(orbitRadius, 1f)) / 180f;
                        var baseSpeedLimit = Mathf.Min(baseSpeed, maxPhysicalSpeed);

                        var alignment = Mathf.Clamp01(1f - ((angleDiff - 15f) / 30f));
                        var finalSpeed = Mathf.Lerp(Mathf.Min(Props.flightSpeedTurningPerTick, baseSpeedLimit), baseSpeedLimit, alignment);

                        MoveFurther(finalSpeed);
                        break;
                    }

                case ChaseMode.Elliptical:
                    {
                        var (a, b) = GetEllipseAxes();
                        orbitPerpOffset = orbitOffsetDir * b;

                        Vector3 orbitCenter = flightPattern == FlightPattern.Around
                            ? targetPos
                            : targetPos + orbitPerpOffset;
                        var distToCenter = Vector3.Distance(curPosition.Yto0(), orbitCenter.Yto0());

                        LogOnce(() => "EllipticalChase", () => $"a: {a}, b: {b}, orbitCenter: {orbitCenter}, distToCenter: {distToCenter}");

                        DoEllipseOrbit(orbitCenter, a, b, orbitOrientAngle, targetPos);
                        break;
                    }
            }
        }

        private bool? ClockWiseTurn(float targetAngle)
        {
            if (new FloatRange(targetAngle - Props.turnAnglePerTick, targetAngle + Props.turnAnglePerTick).Includes(CurAngle))
            {
                LogOnce(() => "ClockWiseTurn", () => $"targetAngle: {targetAngle}, CurAngle: {CurAngle}, result: null (already aligned)");
                return null;
            }
            float diff = targetAngle - CurAngle;
            if (diff > 0 ? diff > 180f : diff >= -180f)
            {
                LogOnce(() => "ClockWiseTurn", () => $"targetAngle: {targetAngle}, CurAngle: {CurAngle}, diff: {diff}, result: false (counter-clockwise)");
                return false;
            }
            LogOnce(() => "ClockWiseTurn", () => $"targetAngle: {targetAngle}, CurAngle: {CurAngle}, diff: {diff}, result: true (clockwise)");
            return true;
        }

        private Vector3 PickUnoccupiedSide(Vector3 targetPos, Vector3 perpCW, Vector3 perpCCW, float offsetDist)
        {
            var vehicle = Vehicle;
            Vector3 posCW = targetPos + perpCW * offsetDist;
            Vector3 posCCW = targetPos + perpCCW * offsetDist;

            var cellCW = posCW.ToIntVec3();
            var cellCCW = posCCW.ToIntVec3();

            int occupiedCW = 0;
            int occupiedCCW = 0;

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    IntVec3 checkCell = cellCW + new IntVec3(x, 0, z);
                    if (checkCell.InBounds(vehicle.Map) && vehicle.Map.thingGrid.ThingsAt(checkCell).Any(t => t is Pawn || t is Building))
                    {
                        occupiedCW++;
                    }

                    checkCell = cellCCW + new IntVec3(x, 0, z);
                    if (checkCell.InBounds(vehicle.Map) && vehicle.Map.thingGrid.ThingsAt(checkCell).Any(t => t is Pawn || t is Building))
                    {
                        occupiedCCW++;
                    }
                }
            }

            return occupiedCW <= occupiedCCW ? perpCW : perpCCW;
        }

        private float GetAngleFromTarget(Vector3 target)
        {
            var rawAngle = (curPosition.Yto0() - target.Yto0()).AngleFlat();
            var targetAngle = rawAngle + FlightAngleOffset;
            return AngleAdjusted(targetAngle);
        }

        private void LogData(string prefix)
        {
            var vehicle = Vehicle;
            Log.Message(vehicle + " - " + prefix + " - Vehicle.Position: " + vehicle.Position + " - takeoffProgress: " + takeoffProgress
                + " - IsFlying: " + Flying + " - IsTakingOff: " + TakingOff + " - IsDescending: " + Landing
                + " - CurAngle: " + CurAngle + " - Vehicle.Angle: " + vehicle.Angle
                + " - FullRotation: " + vehicle.FullRotation.ToStringNamed() + " - Rotation: " + vehicle.Rotation.ToStringHuman()
                + " - reachedInitialTarget: " + reachedInitialTarget + " - target: " + target + " - faceTarget: " + faceTarget);
        }

        public void LogOnce(Func<string> keyFunc, Func<string> messageFunc)
        {
            if (!loggingMode)
            {
                return;
            }
            var key = keyFunc();
            var message = messageFunc();
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
            Log.ResetMessageCount();
        }

        public void LogAlways(Func<string> keyFunc, Func<string> messageFunc)
        {
            if (!loggingMode)
            {
                return;
            }
            Log.Message($"{keyFunc()} - {messageFunc()}");
            Log.ResetMessageCount();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            DestroyFlightGraphic();
            var vehicle = Vehicle;
            foreach (var cell in previousMap.AllCells)
            {
                var grid = previousMap.thingGrid.thingGrid[previousMap.cellIndices.CellToIndex(cell)];
                var vehiclePawn = grid.OfType<VehiclePawn>().Where(x => x == vehicle).FirstOrDefault();
                if (vehiclePawn != null)
                {
                    grid.Remove(vehiclePawn);
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
                var vehicle = Vehicle;
                Vector3 drawPos = curPosition;
                drawPos.y = vehicle.DrawPos.y - 1;
                drawPos.z -= 3f * takeoffProgress;

                Vector2 size = FlightGraphic.drawSize;
                var scale = new Vector3(size.x, 1f, size.y);

                var visualAngle = AngleAdjusted(CurAngle + FlightAngleOffset);
                var rot = Quaternion.AngleAxis(visualAngle, Vector3.up);

                var matrix = Matrix4x4.TRS(drawPos, rot, scale);

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
            var vehicle = Vehicle;
            var graphicData = new GraphicDataRGB();
            graphicData.CopyFrom(copyGraphicData);
            Graphic_Vehicle graphic;
            if ((graphicData.shaderType.Shader.SupportsMaskTex() || graphicData.shaderType.Shader.SupportsRGBMaskTex()))
            {

            }
            if (vehicle.patternData != null)
            {
                graphicData.CopyDrawData(vehicle.patternData);
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
            Scribe_Values.Look(ref designatedLandingRotation, "designatedLandingRotation");
            Scribe_Values.Look(ref targetForRunway, "targetForRunway");
            Scribe_Values.Look(ref landingStage, "landingStage");
            Scribe_Values.Look(ref orbitClockwise, "orbitClockwise", true);
            Scribe_Values.Look(ref continueRotating, "continueRotating");
            Scribe_Values.Look(ref orbitPerpOffset, "orbitPerpOffset");
            Scribe_Values.Look(ref orbitOffsetDir, "orbitOffsetDir");
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
            Scribe_Values.Look(ref loggingMode, "loggingMode");
            Scribe_Values.Look(ref yieldUntilTick, "yieldUntilTick");
        }
    }
}
