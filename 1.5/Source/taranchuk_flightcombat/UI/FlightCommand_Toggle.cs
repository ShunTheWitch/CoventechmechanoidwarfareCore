using UnityEngine;
using Verse;

namespace taranchuk_flightcombat
{
    public class FlightCommand_Toggle : FlightCommand<Command_FlightMode>
    {
        public override Command_FlightMode GetCommand()
        {
            var command = new Command_FlightMode
            {
                defaultLabel = label,
                defaultDesc = description,
                icon = ContentFinder<Texture2D>.Get(texPath),
            };
            return command; 
        }
    }
}
