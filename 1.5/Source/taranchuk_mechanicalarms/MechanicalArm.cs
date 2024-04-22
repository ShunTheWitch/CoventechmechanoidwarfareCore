using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace taranchuk_mechanicalarms
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [HotSwappable]
    public class MechanicalArm : IExposable, ILoadReferenceable
    {
        public Thing parent;
        public MechanicalArmProps Props;
        public List<ArmSegment> armSegments;

        private Frame curFrameTarget;
        private Building curBuildingTarget;
        public string id;

        public bool Idle => curFrameTarget is null && curBuildingTarget is null;
        public Thing Target => curFrameTarget ?? curBuildingTarget;
        public void Tick()
        {
            var angle = 0f;
            foreach (var arm in armSegments)
            {
                arm.defaultAngleAdjusted = (arm.Props.defaultAngle + angle).AngleAdjusted();
                arm.Tick();
                var offset = arm.curAngle - arm.defaultAngleAdjusted;
                angle += offset;
            }
        }

        public void TryFindTarget()
        {
            if (curFrameTarget is null)
            {
                curFrameTarget = NextFrameTarget();
            }
        }

        private Frame NextFrameTarget()
        {
            return GenRadial.RadialDistinctThingsAround(this.parent.Position, parent.Map, 50, true).OfType<Frame>()
                .Where(x => x.TotalMaterialCost().Count <= 0).FirstOrDefault();
        }

        public void Draw()
        {
            var drawPos = parent.DrawPos;
            var pos = drawPos + Props.position.RotatedBy(GetParentAngle());
            var angle = GetParentAngle();
            for (var i = 0; i < armSegments.Count; i++)
            {
                var armSegment = armSegments[i];
                angle = (angle + armSegment.curAngle).AngleAdjusted();
                pos += armSegment.Props.offset.RotatedBy(angle);
                armSegment.DrawAt(pos, angle);
            }
        }

        private float GetParentAngle()
        {
            if (parent is Pawn pawn)
            {
                return pawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None);
            }
            return parent.Rotation.AsAngle;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_References.Look(ref parent, "parent");
            Scribe_Collections.Look(ref armSegments, "armSegments", LookMode.Deep);
            Scribe_References.Look(ref curFrameTarget, "curFrameTarget");
            Scribe_References.Look(ref curBuildingTarget, "curBuildingTarget");
        }

        public string GetUniqueLoadID()
        {
            return id;
        }
    }
}
