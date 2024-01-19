using System;
using Verse;

namespace taranchuk_flightcombat
{
    public class Command_FlightMode : Command_Toggle
    {
        public Action onHover;

        public override void GizmoUpdateOnMouseover()
        {
            if (onHover != null)
            {
                onHover();
            }
        }
    }
}
