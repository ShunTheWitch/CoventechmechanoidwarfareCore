using RimWorld;
using System.Collections.Generic;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    public class CompProperties_MechanitorControl : VehicleCompProperties
    {
        public int bandwidthGain;
        public float mechControlRange;
        public CompProperties_MechanitorControl()
        {
            this.compClass = typeof(CompMechanitorControl);
        }
    }
    public class CompMechanitorControl : VehicleComp
    {
        public CompProperties_MechanitorControl Props => base.props as CompProperties_MechanitorControl;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo mechGizmo in MechanitorUtility.GetMechGizmos(this.Vehicle))
            {
                yield return mechGizmo;
            }
        }
    }

}
