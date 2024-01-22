using Verse;

namespace taranchuk_flightcombat
{
    public abstract class FlightCommand<T> where T : Command
    {
        public string label;
        public string description;
        public string texPath;

        public abstract T GetCommand();
    }
}
