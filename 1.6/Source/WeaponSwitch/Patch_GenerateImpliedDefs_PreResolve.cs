using HarmonyLib;
using RimWorld;
using Verse;

namespace WeaponSwitch
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public class Patch_GenerateImpliedDefs_PreResolve
    {
        public static void Postfix()
        {
            WeaponSwitchMod.WS_SwitchWeapon = new JobDef
            {
                defName = "WS_SwitchWeapon",
                driverClass = typeof(JobDriver_SwitchWeapon)
            };
            DefGenerator.AddImpliedDef(WeaponSwitchMod.WS_SwitchWeapon);
        }
    }
}
