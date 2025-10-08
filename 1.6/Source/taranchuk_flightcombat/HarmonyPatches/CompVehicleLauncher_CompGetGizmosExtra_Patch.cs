using HarmonyLib;
using System.Collections.Generic;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.CompGetGizmosExtra))]
    public static class CompVehicleLauncher_CompGetGizmosExtra_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompVehicleLauncher __instance)
        {
            var comp = __instance.Vehicle.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                var command = __instance.takeoffCommand;
                yield return command;
            }
            else
            {
                foreach (var g in __result)
                {
                    yield return g;
                }
            }
        }
    }
}
