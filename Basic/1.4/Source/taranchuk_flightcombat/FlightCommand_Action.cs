using UnityEngine;
using Verse;

namespace taranchuk_flightcombat
{
    public class FlightCommand_Action : FlightCommand<Command_Action>
    {
        public override Command_Action GetCommand()
        {
            var command = new Command_Action
            {
                defaultLabel = label,
                defaultDesc = description,
                icon = ContentFinder<Texture2D>.Get(texPath),
            };
            return command;
        }
    }
}
