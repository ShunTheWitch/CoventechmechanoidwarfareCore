using HarmonyLib;
using Verse;

namespace WeaponSwitch
{
    public class WeaponSwitchMod : Mod
    {
        public static bool CEActive = ModsConfig.IsActive("CETeam.CombatExtended");

        public static JobDef WS_SwitchWeapon;
        public WeaponSwitchMod(ModContentPack content) : base(content)
        {
            new Harmony("WeaponSwitch.Mod").PatchAll();
        }
    }
}
