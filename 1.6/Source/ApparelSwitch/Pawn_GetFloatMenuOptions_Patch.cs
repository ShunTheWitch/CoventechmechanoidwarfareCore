using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace ApparelSwitch
{
    [HarmonyPatch(typeof(Pawn), "GetFloatMenuOptions")]
    public static class Pawn_GetFloatMenuOptions_Patch
    {
        public static void Postfix(Pawn __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            if (__instance.apparel != null && __instance.IsColonistPlayerControlled && selPawn == __instance)
            {
                List<FloatMenuOption> list = __result.ToList();
                foreach (var apparel in __instance.apparel.WornApparel)
                {
                    var compSwitchApparel = apparel.TryGetComp<CompSwitchApparel>();
                    if (compSwitchApparel != null)
                    {
                        list.AddRange(compSwitchApparel.GetFloatMenuOptions());
                    }
                }
                __result = list;
            }
        }
    }
}
