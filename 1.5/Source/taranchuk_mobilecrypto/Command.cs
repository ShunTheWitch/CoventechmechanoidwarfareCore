using UnityEngine;
using Verse;

namespace taranchuk_mobilecrypto
{
    public class Command
    {
        public string label;
        public string description;
        public string texPath;

        public Command_Action GetCommand()
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
