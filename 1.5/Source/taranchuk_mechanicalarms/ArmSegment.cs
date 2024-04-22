using UnityEngine;
using Verse;

namespace taranchuk_mechanicalarms
{
    [HotSwappable]
    public class ArmSegment : IExposable
    {
        public ArmSegmentProps Props;

        public float curAngle;

        public MechanicalArm parent;

        public Graphic graphic;

        public Graphic Graphic
        {
            get
            {
                if (graphic == null)
                {
                    graphic = Props.graphicData.GraphicColoredFor(parent.parent);
                }
                return graphic;
            }
        }

        public void DrawAt(Vector3 pos, float rotation)
        {
            var loc = pos;
            Mesh mesh = MeshPool.plane10;
            Quaternion quat = Quaternion.identity;
            quat *= Quaternion.Euler(Vector3.up * rotation);
            Material mat = Graphic.MatSingleFor(parent.parent);
            Graphics.DrawMesh(mesh, loc, quat, mat, 0);
        }

        public float angleOffset;
        public float defaultAngleAdjusted;
        public void Tick()
        {
            if (Props.swingPerTick != 0)
            {
                if (parent.Idle)
                {
                    var angleDiff = Mathf.DeltaAngle(curAngle, defaultAngleAdjusted);
                    if (angleDiff > Props.maxSwingAngle)
                    {
                        angleOffset = Props.swingPerTick;
                    }
                    else if (0 > angleDiff && Mathf.Abs(angleDiff) > Props.maxSwingAngle)
                    {
                        angleOffset = -Props.swingPerTick;
                    }
                    else if (new FloatRange(-Props.swingPerTick, Props.swingPerTick).Includes(angleDiff))
                    {
                        curAngle = defaultAngleAdjusted;
                        angleOffset = 0;
                    }
                    curAngle = (curAngle + angleOffset).AngleAdjusted();
                }
                else
                {
                    var target = parent.Target;
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref curAngle, "curAngle");
            Scribe_References.Look(ref parent, "parent");
        }
    }
}
