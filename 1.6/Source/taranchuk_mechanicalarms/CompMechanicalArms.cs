using System.Collections.Generic;
using System.Linq;
using Verse;

namespace taranchuk_mechanicalarms
{
    public class CompProperties_MechanicalArms : CompProperties
    {
        public List<MechanicalArmProps> arms;
        public CompProperties_MechanicalArms()
        {
            compClass = typeof(CompMechanicalArms);
        }
    }

    [HotSwappable]
    public class CompMechanicalArms : ThingComp
    {
        public CompProperties_MechanicalArms Props => base.props as CompProperties_MechanicalArms;
        public List<MechanicalArm> arms;

        public override void CompTick()
        {
            base.CompTick();
            foreach (var arm in arms)
            {
                arm.Tick();
            }
        }

        public override void Notify_DefsHotReloaded()
        {
            base.Notify_DefsHotReloaded();
            MakeArms();
        }

        public override void PostDraw()
        {
            base.PostDraw();
            foreach (var arm in arms)
            {
                arm.Draw();
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            MakeArms();
        }

        private void MakeArms()
        {
            arms = new List<MechanicalArm>();
            for (int i = 0; i < Props.arms.Count; i++)
            {
                var arm = new MechanicalArm
                {
                    Props = Props.arms[i],
                    parent = parent,
                };
                arms.Add(arm);
                arm.id = parent.GetUniqueLoadID() + "_" + arms.IndexOf(arm) + "_ID";
                arm.armSegments = new List<ArmSegment>();
                for (int j = 0; j < arm.Props.segments.Count; j++)
                {
                    arm.armSegments.Add(new ArmSegment
                    {
                        Props = arm.Props.segments[j],
                        parent = arm,
                        curAngle = arm.Props.segments[j].defaultAngle
                    });
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref arms, "arms", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (arms is null || arms.Count != Props.arms.Count 
                    || arms.Sum(x => x.armSegments.Count()) != Props.arms.Sum(y => y.segments.Count()))
                {
                    MakeArms();
                }
                try
                {
                    InitProps();
                }
                catch
                {
                    MakeArms();
                    InitProps();
                }
            }
        }

        private void InitProps()
        {
            for (int i = 0; i < arms.Count; i++)
            {
                var arm = arms[i];
                arm.Props = Props.arms[i];
                for (var j = 0; j < arm.armSegments.Count; j++)
                {
                    var segment = arm.armSegments[j];
                    segment.Props = arm.Props.segments[j];
                }
            }
        }
    }
}
