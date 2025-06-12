using HarmonyLib;
using RimWorld;
using System.Text;
using Verse;

namespace BM_PowerArmor
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    public static class Pawn_GetInspectString_Patch
    {
        public static void Postfix(ref string __result, Pawn __instance)
        {
            var sb = new StringBuilder(__result);
            if (__instance.apparel != null)
            {
                foreach (var apparel in __instance.apparel.WornApparel)
                {
                    var comp = apparel.GetComp<CompPowerArmor>();
                    if (comp != null)
                    {
                        var compRefuelable = apparel.GetComp<CompRefuelable>();
                        if (compRefuelable != null)
                        {
                            sb.AppendLine("\n" + compRefuelable.CompInspectStringExtra());
                        }
                    }
                }
            }
            __result = sb.ToString().TrimEndNewlines();
        }
    }
}
