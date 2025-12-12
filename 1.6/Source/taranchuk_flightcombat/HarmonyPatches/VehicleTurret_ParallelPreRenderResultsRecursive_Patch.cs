using HarmonyLib;
using SmashTools.Rendering;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), "ParallelPreRenderResultsRecursive")]
    public static class VehicleTurret_ParallelPreRenderResultsRecursive_Patch
    {
        public static void Prefix(VehicleTurret __instance, TransformData transformData, ref float rotation)
        {
            var comp = __instance.vehicle.GetComp<CompFlightMode>();
            if (comp != null && __instance.vehicle.InFlightModeOrNonStandardAngle(comp))
            {
                rotation -= transformData.rotation;
            }
        }
    }
}
