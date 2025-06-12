using HarmonyLib;
using System.Linq;
using Verse;

namespace taranchuk_flightcombat
{
    public class taranchuk_flightcombatMod : Mod
    {
        public taranchuk_flightcombatMod(ModContentPack pack) : base(pack)
        {
			new Harmony("taranchuk_flightcombatMod").PatchAll();
        }
    }
}
