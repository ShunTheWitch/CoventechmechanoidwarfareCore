using HarmonyLib;
using RimWorld;
using System.Text;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(CompOverseerSubject), "CompInspectStringExtra")]
    public static class CompOverseerSubject_CompInspectStringExtra_Patch
    {
        public static bool Prefix(CompOverseerSubject __instance, ref string __result)
        {
            if (__instance.parent.Faction == Faction.OfPlayer)
            {
                var comp = __instance.parent.GetComp<CompMechanitorControl>();
                if (comp != null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    Pawn overseer = __instance.Overseer;
                    TaggedString taggedString = "Overseer".Translate();
                    if (overseer?.mechanitor != null)
                    {
                        taggedString += ": " + __instance.Overseer.LabelShort;
                        if (!overseer.mechanitor.ControlledPawns.Contains(__instance.parent as Pawn))
                        {
                            taggedString += " (" + "InsufficientBandwidth".Translate() + ")";
                        }
                    }
                    else
                    {
                        taggedString += ": " + "OverseerNone".Translate();
                    }
                    stringBuilder.Append(taggedString);
                    __result = stringBuilder.ToString();
                    return false;
                }
            }
            return true;
        }
    }
}
