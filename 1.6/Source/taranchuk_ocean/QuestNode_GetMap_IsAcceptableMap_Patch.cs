using HarmonyLib;
using RimWorld.QuestGen;
using Verse;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(QuestNode_GetMap), "IsAcceptableMap")]
    public static class QuestNode_GetMap_IsAcceptableMap_Patch
    {
        public static void Postfix(Map map, Slate slate, ref bool __result)
        {
            if (map.IsWaterBiome() && SettleInEmptyTileUtility_SettleCommand_Patch.lookingForWaterTile is false)
            {
                __result = false;
            }
        }
    }
}
