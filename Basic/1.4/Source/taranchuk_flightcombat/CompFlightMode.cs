using HarmonyLib;
using RimWorld;
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
        public float flightSpeedPerTick;
        public float flightSpeedTurningPerTick;
        public float turnAnglePerTick;
        public float turnAngleCirclingPerTick;
        public float distanceFromTargetToStartTurning;
        public float fuelConsumptionPerTick;
        public GraphicDataRGB flightGraphicData;
        public CompProperties_FlightMode()
        {
            this.compClass = typeof(CompFlightMode);
        }
    }

    [HotSwappable]
    public class CompFlightMode : ThingComp, IMaterialCacheTarget
    {
        public bool flightMode;

        private LocalTargetInfo target;
        private LocalTargetInfo initialTarget;

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

        protected Graphic_Vehicle cachedGraphic;

        public Graphic_Vehicle FlightGraphic
        {
            get
            {
                if (cachedGraphic == null)
                {
                    cachedGraphic = GenerateGraphicData(this, Props.flightGraphicData);
                }
                return cachedGraphic;
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
                var command = new Command_Toggle
                {
                    defaultLabel = Props.flightModeLabel,
                    defaultDesc = Props.flightModeDesc,
                    icon = ContentFinder<Texture2D>.Get(Props.flightModeUITexPath),
                    isActive = () => flightMode,
                    toggleAction = () => 
                    { 
                        SetFlightMode(!flightMode);
                    }
                };
                if (flightMode && CanLand() is false)
                {
                    command.Disable("CVN_CannotLandOnImpassableTiles".Translate());
                }
                yield return command;
            }
        }

        public void SetFlightMode(bool flightMode)
        {
            if (flightMode)
            {
                SetTarget(Vehicle.vehiclePather.Moving ? Vehicle.vehiclePather.Destination : Vehicle.Position);
                curPosition = Vehicle.Drawer.DrawPos;
                curAngleInt = Vehicle.Angle;
                if (Vehicle.vehiclePather.Moving)
                {
                    Vehicle.vehiclePather.StopDead();
                }
            }
            else
            {
                target = null;
                Vehicle.Rotation = Rot8.FromAngle(CurAngle);
                Vehicle.UpdateAngle();
            }
            this.flightMode = flightMode;
        }

        public void SetTarget(LocalTargetInfo targetInfo)
        {
            this.target = targetInfo;
            this.initialTarget = targetInfo.Cell;
            this.clockwiseTurn = null;
            Vehicle.vehiclePather.StopDead();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (flightMode)
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

                if (initialTarget.IsValid)
                {
                    RotateTowards(initialTarget.CenterVector3);
                    MoveFurther(CurAngle, Props.flightSpeedTurningPerTick);
                }

                else if (target.Cell.DistanceTo(Vehicle.Position) < Props.distanceFromTargetToStartTurning)
                {
                    MoveFurther(CurAngle, Props.flightSpeedPerTick);
                }
                else
                {
                    RotatePerperticular(target.CenterVector3);
                    MoveFurther(CurAngle, Props.flightSpeedTurningPerTick);
                }

                var curPositionIntVec = curPosition.ToIntVec3();
                if (curPositionIntVec != Vehicle.Position)
                {
                    Vehicle.Position = curPositionIntVec;
                    Vehicle.vehiclePather.nextCell = curPositionIntVec;
                }
                Vehicle.Angle = CurAngle;
                UpdateRotation();
            }
            //LogData("flightMode: " + flightMode);
        }


        //public override void PostDrawExtraSelectionOverlays()
        //{
        //    base.PostDrawExtraSelectionOverlays();
        //    LogData("flightMode: " + flightMode);
        //}

        private bool CanLand() => OccupiedRect().Cells.All(x => Vehicle.Drivable(x));
        public void UpdateRotation()
        {
            Vehicle.Rotation = Rot4.West;
        }

        public CellRect OccupiedRect()
        {
            Vehicle.Rotation = Rot8.FromAngle(CurAngle);
            var rect = Vehicle.OccupiedRect();
            UpdateRotation();
            return rect;
        }

        private void MoveFurther(float angle, float speed)
        {
            var newTarget = curPosition + (Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward).RotatedBy(-90);
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

        private void RotateTowards(Vector3 target)
        {
            float targetAngle = GetAngleFromTarget(target);
            if (new FloatRange(targetAngle - Props.turnAnglePerTick, targetAngle + Props.turnAnglePerTick).Includes(CurAngle))
            {
                return;
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
        }

        private float GetAngleFromTarget(Vector3 target)
        {
            var targetAngle = (curPosition.Yto0() - target.Yto0()).AngleFlat() - 90f;
            return AngleAdjusted(targetAngle);
        }

        private void LogData(string prefix)
        {
            Log.Message(prefix + " - Vehicle.Position: " + Vehicle.Position + " - Vehicle.Rotation: " + Vehicle.Rotation.ToStringHuman()
                + " - Vehicle.Angle: " + Vehicle.Angle);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            DestroyFlightGraphic();
        }

        private void DestroyFlightGraphic()
        {
            RGBMaterialPool.Release(this);
            cachedGraphic = null;
        }

        private static Graphic_Vehicle GenerateGraphicData(IMaterialCacheTarget cacheTarget, GraphicDataRGB copyGraphicData)
        {
            var graphicData = new GraphicDataRGB();
            graphicData.CopyFrom(copyGraphicData);
            Graphic_Vehicle graphic;
            if ((graphicData.shaderType.Shader.SupportsMaskTex() || graphicData.shaderType.Shader.SupportsRGBMaskTex()))
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
            Scribe_TargetInfo.Look(ref initialTarget, "initialTarget");
            Scribe_Values.Look(ref clockwiseTurn, "clockwiseTurn");
        }
    }
}
