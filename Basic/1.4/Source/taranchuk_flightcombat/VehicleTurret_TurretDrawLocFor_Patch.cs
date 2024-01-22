using HarmonyLib;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.TurretDrawLocFor))]
    public static class VehicleTurret_TurretDrawLocFor_Patch
    {
        public static void Prefix(VehicleTurret __instance, ref Rot8 rot, out CompFlightMode __state)
        {
            var comp = __instance.vehicle?.GetComp<CompFlightMode>();
            if (comp != null && __instance.vehicle.InFlightModeOrNonStandardAngle(comp))
            {
                __state = comp;
                rot = Rot8.West;
            }
            else
            {
                __state = null;
            }
        }
        public static void Postfix(ref Vector3 __result, CompFlightMode __state)
        {
            if (__state != null)
            {
                __result = __result.RotatedBy(__state.CurAngle);
            }
        }
    }
}
