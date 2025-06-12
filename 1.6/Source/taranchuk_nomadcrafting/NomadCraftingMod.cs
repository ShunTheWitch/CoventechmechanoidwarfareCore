using HarmonyLib;
using Verse;

namespace taranchuk_nomadcrafting
{
    public class NomadCraftingMod : Mod
    {
        public NomadCraftingMod(ModContentPack content) : base(content)
        {
            new Harmony("taranchuk_nomadcraftingMod").PatchAll();
        }
    }
}
