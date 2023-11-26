using HarmonyLib;
using RimWorld.Planet;

namespace taranchuk_ocean
{
    [HarmonyPatch(typeof(SettleInEmptyTileUtility), "SettleCommand")]
    public static class SettleInEmptyTileUtility_SettleCommand_Patch
    {
        public static bool lookingForWaterTile;
        public static void Prefix()
        {
            lookingForWaterTile = true;
        }
        public static void Postfix(Caravan caravan)
        {
            lookingForWaterTile = false;
        }
    }
}
