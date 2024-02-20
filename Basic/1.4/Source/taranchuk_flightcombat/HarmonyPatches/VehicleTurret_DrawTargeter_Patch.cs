using HarmonyLib;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehicleTurret), "DrawTargeter")]
    public static class VehicleTurret_DrawTargeter_Patch
    {
        public static VehicleTurret curTurret;
        public static void Prefix(VehicleTurret __instance)
        {
            curTurret = __instance;
        }
        public static void Postfix(VehicleTurret __instance)
        {
            curTurret = null;
        }
    }
}
