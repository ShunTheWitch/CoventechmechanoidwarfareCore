using HarmonyLib;
using Verse;

namespace ApparelSwitch
{
    public class ApparelSwitchMod : Mod
    {
        public static JobDef AS_SwitchApparel;

        public ApparelSwitchMod(ModContentPack content) : base(content)
        {
            new Harmony("ApparelSwitch.Mod").PatchAll();
        }
    }
}
