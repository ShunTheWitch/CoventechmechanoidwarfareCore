using HarmonyLib;
using RimWorld;
using Verse;

namespace ApparelSwitch
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public class DefGenerator_GenerateImpliedDefs_PreResolve_Patch
    {
        public static void Postfix()
        {
            ApparelSwitchMod.AS_SwitchApparel = new JobDef
            {
                defName = "AS_SwitchApparel",
                driverClass = typeof(JobDriver_SwitchApparel)
            };
            DefGenerator.AddImpliedDef(ApparelSwitchMod.AS_SwitchApparel);
        }
    }
}
