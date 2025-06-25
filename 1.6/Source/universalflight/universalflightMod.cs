using HarmonyLib;
using Verse;

namespace universalflight
{
    public class universalflightMod : Mod
    {
        public universalflightMod(ModContentPack pack) : base(pack)
        {
            new Harmony("universalflightMod").PatchAll();
        }
    }
}
