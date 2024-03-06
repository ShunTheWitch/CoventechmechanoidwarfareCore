using HarmonyLib;
using System.Linq;
using Verse;

namespace taranchuk_mobilecrypto
{
    public class taranchuk_mobilecryptoMod : Mod
    {
        public taranchuk_mobilecryptoMod(ModContentPack pack) : base(pack)
        {
			new Harmony("taranchuk_mobilecryptoMod").PatchAll();
        }
    }
}
