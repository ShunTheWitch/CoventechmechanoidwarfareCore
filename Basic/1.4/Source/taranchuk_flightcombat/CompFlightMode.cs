﻿using RimWorld;
using SmashTools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    public class CompProperties_FlightMode : CompProperties
    {
        public string flightModeLabel = "Flight mode";
        public string flightModeDesc = "Toggle flight mode.";
        public string flightModeUITexPath = "UI/FlightMode";
        public string hoverModeLabel;
        public string hoverModeDesc;
        public string hoverModeUITexPath;
        public string faceTargetLabel;
        public string faceTargetDesc;
        public string faceTargetUITexPath;
        public int takeoffTicks;
        public int landingTicks;
        public bool moveWhileTakingOff;
        public List<TerrainAffordanceDef> runwayTerrainRequirements;
        public float flightSpeedPerTick;
        public float flightSpeedTurningPerTick;
        public float turnAnglePerTick;
        public float turnAngleCirclingPerTick;
        public float distanceFromTargetToStartTurning;
        public float fuelConsumptionPerTick;
        public GraphicDataRGB flightGraphicData;
        public List<FlightFleckData> flightFlecks;
        public List<FlightFleckData> hoverFlecks;
        public List<FlightFleckData> takeoffFlecks;
        public List<FlightFleckData> landingFlecks;
        public FleckDef waypointFleck;
        public CompProperties_FlightMode()
        {
            this.compClass = typeof(CompFlightMode);
        }
    }
    [HotSwappable]
    public class CompFlightMode : ThingComp, IMaterialCacheTarget
    {
        private FlightMode flightMode;
        private bool Flying => flightMode != FlightMode.Off;
        private LocalTargetInfo target;
        private LocalTargetInfo targetToFace;
        private LocalTargetInfo initialTarget;
        private float takeoffProgress;
        private bool TakingOff => flightMode != FlightMode.Off && takeoffProgress < 1f;
        private bool Hovering => flightMode == FlightMode.Hover;
        private bool Landing => flightMode == FlightMode.Off && takeoffProgress > 0f;
        public bool InAir => Flying || TakingOff || Landing;
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

        public VehiclePawn Vehicle => parent as VehiclePawn;

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

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Vehicle.Faction == Faction.OfPlayer)
            {
                var flightModeCommand = new Command_FlightMode
                {
                    defaultLabel = Props.flightModeLabel,
                    defaultDesc = Props.flightModeDesc,
                    icon = ContentFinder<Texture2D>.Get(Props.flightModeUITexPath),
                    isActive = () => Flying,
                    toggleAction = () => 
                    {
                        SetFlightMode(this.flightMode != FlightMode.Flight);
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
                if (Props.hoverModeLabel.NullOrEmpty() is false)
                {
                    var hoverMode = new Command_FlightMode
                    {
                        defaultLabel = Props.hoverModeLabel,
                        defaultDesc = Props.hoverModeDesc,
                        icon = ContentFinder<Texture2D>.Get(Props.hoverModeUITexPath),
                        isActive = () => this.flightMode == FlightMode.Hover,
                        toggleAction = () =>
                        {
                            SetHoverMode(this.flightMode != FlightMode.Hover);
                        }
                    };
                    yield return hoverMode;
                }
                if (Props.faceTargetLabel.NullOrEmpty() is false)
                {
                    var faceTarget = new Command_Action
                    {
                        defaultLabel = Props.faceTargetLabel,
                        defaultDesc = Props.faceTargetDesc,
                        icon = ContentFinder<Texture2D>.Get(Props.faceTargetUITexPath),
                        action = () =>
                        {
                            Find.Targeter.BeginTargeting(TargetingParamsForFacing, delegate (LocalTargetInfo x)
                            {
                                if (Hovering)
                                {
                                    targetToFace = x;
                                }
                                else
                                {
                                    target = x;
                                    initialTarget = x;
                                }
                            });
                        }
                    };
                    yield return faceTarget;
                }
            }
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
                CurAngle = Vehicle.FullRotation.AsAngle + 90;
                if (Vehicle.vehiclePather.Moving)
                {
                    Vehicle.vehiclePather.StopDead();
                }
                UpdateVehicleAngleAndRotation();
            }
            else
            {
                target = null;
                Vehicle.FullRotation = Rot8.FromAngle(CurAngle);
                Vehicle.UpdateAngle();
            }
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
                    CurAngle = Vehicle.FullRotation.AsAngle - 90;
                }
                if (Vehicle.vehiclePather.Moving)
                {
                    Vehicle.vehiclePather.StopDead();
                }
            }
            this.flightMode = hoverMode ? FlightMode.Hover : FlightMode.Flight; 
        }

        public void SetTarget(LocalTargetInfo targetInfo)
        {
            this.target = targetInfo;
            this.initialTarget = targetInfo.Cell;
            this.clockwiseTurn = null;
            this.targetToFace = null;
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
                }
                else if (Hovering)
                {
                    Hover();
                }
                else if (Flying)
                {
                    Flight();
                }
                SpawnFlecks();

                var curPositionIntVec = curPosition.ToIntVec3();
                if (curPositionIntVec != Vehicle.Position)
                {
                    if (Vehicle.OccupiedRect().MovedBy(curPositionIntVec.ToIntVec2 - Vehicle.Position.ToIntVec2).InBounds(Vehicle.Map))
                    {
                        Vehicle.Position = curPositionIntVec;
                        Vehicle.vehiclePather.nextCell = curPositionIntVec;
                    }
                }

                UpdateVehicleAngleAndRotation();
            }
            //LogData("flightMode: " + flightMode);
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
            Vehicle.Angle = CurAngle;
            UpdateRotation();
        }

        public List<IntVec3> GetRunwayCells(bool takingOff)
        {
            var cells = new List<IntVec3>();
            var vehicle = Vehicle;
            var position = vehicle.Position;
            var rot = Vehicle.FullRotation;
            var angle = InAir ? AngleAdjusted(vehicle.Angle - 90) : rot.AsAngle;
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
            var rotInAir = Rot8.FromAngle(CurAngle - 90);
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

        private static IntVec3 AdjustPos(Rot8 rot, IntVec3 pos)
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
            else if (target.Cell != Vehicle.Position)
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
            var targetDistance = target.Cell.DistanceTo(Vehicle.Position);
            if (initialTarget.IsValid)
            {
                bool rotated = RotateTowards(initialTarget.CenterVector3);
                MoveFurther(rotated ? Props.flightSpeedTurningPerTick : Props.flightSpeedPerTick);
            }
            else if (targetDistance < Props.distanceFromTargetToStartTurning)
            {
                MoveFurther(Props.flightSpeedPerTick);
            }
            else
            {
                RotatePerperticular(target.CenterVector3);
                MoveFurther(Props.flightSpeedTurningPerTick);
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
            if (Vehicle.rotationInt != Rot4.West)
            {
                Vehicle.rotationInt = Rot4.West;
            }
        }

        public List<IntVec3> OccupiedRect()
        {
            if (InAir)
            {
                var vehicle = Vehicle;
                var angle = AngleAdjusted(CurAngle - 90);
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
            var newTarget = curPosition + (Quaternion.AngleAxis(CurAngle, Vector3.up) * Vector3.forward).RotatedBy(-90);
            MoveTowards(newTarget, speed);
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            var newPosition = Vector3.MoveTowards(Vehicle.DrawPos.Yto0(), target.Yto0(), speed);
            curPosition = new Vector3(newPosition.x, Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays), newPosition.z);
        }

        private bool? clockwiseTurn;
        private void RotatePerperticular(Vector3 target)
        {
            float targetAngle = GetAngleFromTarget(target);
            var curAnglePerpendicular = AngleAdjusted(CurAngle + 90);
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
            var targetAngle = (curPosition.Yto0() - target.Yto0()).AngleFlat() - 90f;
            return AngleAdjusted(targetAngle);
        }

        private void LogData(string prefix)
        {
            Log.Message(prefix + " - Vehicle.Position: " + Vehicle.Position + " - takeoffProgress: " + takeoffProgress 
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
            Scribe_TargetInfo.Look(ref targetToFace, "targetToFace");
            Scribe_TargetInfo.Look(ref initialTarget, "initialTarget");
            Scribe_Values.Look(ref clockwiseTurn, "clockwiseTurn");
            Scribe_Values.Look(ref takeoffProgress, "takeoffProgress");
        }
    }
}
