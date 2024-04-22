using Verse;

namespace taranchuk_lasers
{
    [HotSwappable]

    public static class Utilities
    {
        public static float GetBodyAngle(Thing thing)
        {
            try
            {
                if (thing is Vehicles.VehiclePawn vehiclePawn)
                {
                    var comp = vehiclePawn.GetComp<taranchuk_flightcombat.CompFlightMode>();
                    if (comp != null && taranchuk_flightcombat.VehiclePawn_UpdateRotation_Patch.InFlightModeOrNonStandardAngle(vehiclePawn, comp))
                    {
                        return comp.CurAngle;
                    }
                }
            }
            catch 
            {

            }
            try
            {
                if (thing is Vehicles.VehiclePawn vehiclePawn)
                {
                    return vehiclePawn.Angle;
                }
            }
            catch { }
            return thing.Rotation.AsAngle;
        }
    }
}
