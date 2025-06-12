using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace BM_ApparelSet
{
    [HarmonyPatch(typeof(EquipmentUtility), "CanEquip", [typeof(Thing), typeof(Pawn), typeof(string), typeof(bool)],
        [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal])]
    public static class EquipmentUtility_CanEquip_Patch
    {
        private static void Postfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason, bool checkBonded = true)
        {
            if (pawn.apparel != null && thing is Apparel)
            {
                var comp = thing.TryGetComp<CompApparelSet>();
                if (comp != null)
                {
                    if (comp.Props.requiredApparels != null
                        && comp.HasAllRequiredApparels(pawn) is false)
                    {
                        cantReason = "BM.RequiresApparel".Translate(string.Join(", ", comp.Props.requiredApparels.Select(x => x.label)));
                        __result = false;
                        return;
                    }
                    if (comp.Props.onlyWearableApparels != null)
                    {
                        var incompatibleApparels = pawn.apparel.WornApparel.Where(x => comp.Props.onlyWearableApparels.Contains(x.def) is false).ToList();
                        if (incompatibleApparels.Any())
                        {
                            cantReason = "BM.CannotWearWith".Translate(string.Join(", ", incompatibleApparels.Select(x => x.def.label)));
                            __result = false;
                            return;
                        }
                    }
                }

                foreach (var wornApparel in pawn.apparel.WornApparel)
                {
                    var otherComp = wornApparel.GetComp<CompApparelSet>();
                    if (otherComp != null && otherComp.Props.onlyWearableApparels != null 
                        && otherComp.Props.onlyWearableApparels.Contains(thing.def) is false)
                    {
                        cantReason = "BM.CannotWearWith".Translate(wornApparel.def.label);
                        __result = false;
                        return;
                    }
                }
            }
        }
    }
}
