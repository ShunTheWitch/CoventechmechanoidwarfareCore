using HarmonyLib;
using RimWorld;
using Verse;

namespace BM_PowerArmor
{
    [HarmonyPatch(typeof(EquipmentUtility), "CanEquip", [typeof(Thing), typeof(Pawn), typeof(string), typeof(bool)],
    [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal])]
    public static class EquipmentUtility_CanEquip_Patch
    {
        private static void Postfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason, bool checkBonded = true)
        {
            if (thing.def.IsWeapon && pawn.apparel?.WornApparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    var comp = apparel.GetComp<CompPowerArmor>();
                    if (comp != null && comp.Props.powerArmorWeapon != null)
                    {
                        cantReason = "BM.CannotUseOtherWeapons".Translate(apparel.def.label);
                        __result = false;
                        break;
                    }
                }
            }
        }
    }
}
