using HarmonyLib;
using Verse;

namespace taranchuk_mechsuits
{
    public class taranchuk_mechsuitsMod : Mod
    {
        public taranchuk_mechsuitsMod(ModContentPack pack) : base(pack)
        {
            new Harmony("taranchuk_mechsuitsMod").PatchAll();
        }
    }


}
