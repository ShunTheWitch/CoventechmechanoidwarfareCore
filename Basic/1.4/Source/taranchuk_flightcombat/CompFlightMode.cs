using RimWorld;
using SmashTools;
using System.Collections.Generic;
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
        public GraphicData flightGraphicData;
        public CompProperties_FlightMode()
        {
            this.compClass = typeof(CompFlightMode);
        }
    }

    [HotSwappable]
    public class CompFlightMode : ThingComp
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
                curAngleInt = AngleAdjusted(value);
            }
        }

        private float AngleAdjusted(float angle)
        {
            if (angle > 360f)
            {
                angle -= 360f;
            }
            if (angle < 0f)
            {
                angle += 360f;
            }
            return angle;
        }

        public Vector3 curPosition;

        public VehiclePawn Vehicle => parent as VehiclePawn;
        private Graphic_Vehicle graphicInt;
        public Graphic_Vehicle FlightGraphic
        {
            get
            {
                if (graphicInt == null)
                {
                    Props.flightGraphicData.Init();
                    GraphicDataRGB graphicData = new GraphicDataRGB();
                    graphicData.CopyFrom(Props.flightGraphicData);

                    graphicData.color = Vehicle.patternData.color;
                    graphicData.colorTwo = Vehicle.patternData.colorTwo;
                    graphicData.colorThree = Vehicle.patternData.colorThree;
                    graphicData.tiles = Vehicle.patternData.tiles;
                    graphicData.displacement = Vehicle.patternData.displacement;
                    graphicData.pattern = Vehicle.patternData.patternDef;

                    if (graphicData.shaderType.Shader.SupportsRGBMaskTex())
                    {
                        RGBMaterialPool.CacheMaterialsFor(Vehicle);
                        graphicData.Init(Vehicle);
                        graphicInt = graphicData.Graphic as Graphic_Vehicle;
                        RGBMaterialPool.SetProperties(Vehicle, Vehicle.patternData, graphicInt.TexAt, graphicInt.MaskAt);
                    }
                    else
                    {
                        graphicInt = ((GraphicData)graphicData).Graphic as Graphic_Vehicle; //Triggers vanilla Init call for normal material caching
                    }
                }
                return graphicInt;
            }
        }

        public CompProperties_FlightMode Props => base.props as CompProperties_FlightMode;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Vehicle.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
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
            }
            this.flightMode = flightMode;
        }

        public void SetTarget(LocalTargetInfo targetInfo)
        {
            this.target = targetInfo;
            this.initialTarget = targetInfo.Cell;
            this.clockwiseTurn = null;
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

                if (initialTarget.IsValid && initialTarget.Cell == Vehicle.Position)
                {
                    initialTarget = LocalTargetInfo.Invalid;
                }

                if (initialTarget.IsValid)
                {
                    RotateTowards(initialTarget.CenterVector3);
                    MoveTowards(initialTarget.CenterVector3, Props.flightSpeedPerTick);
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
                }
                Vehicle.Angle = CurAngle;
                UpdateRotation();
            }
        }

        public void UpdateRotation()
        {
            Vehicle.Rotation = Rot4.West;
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            var newPosition = Vector3.MoveTowards(Vehicle.DrawPos.Yto0(), target.Yto0(), speed);
            curPosition = new Vector3(newPosition.x, curPosition.y, newPosition.z);
        }

        private void MoveFurther(float angle, float speed)
        {
            var newTarget = curPosition + (Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward).RotatedBy(-90);
            MoveTowards(newTarget, speed);
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
                + " - Vehicle.Angle: " + Vehicle.Angle + " - curPosition: " + curPosition 
                + " - curAngle: " + CurAngle + " - target: " + target);
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
