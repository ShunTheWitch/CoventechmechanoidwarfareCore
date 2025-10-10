using HarmonyLib;
using System.Linq;
using Verse;

namespace taranchuk_ocean
{
    public class taranchuk_oceanMod : Mod
    {
        public taranchuk_oceanMod(ModContentPack pack) : base(pack)
        {
			new Harmony("taranchuk_oceanMod").PatchAll();
        }
    }
}
