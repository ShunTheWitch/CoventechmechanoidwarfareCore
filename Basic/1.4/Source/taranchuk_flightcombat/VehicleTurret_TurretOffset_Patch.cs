using HarmonyLib;
using SmashTools;
using System;
using UnityEngine;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.TurretOffset))]
    public static class VehicleTurret_TurretOffset_Patch
    {
        public static bool Prefix(VehicleTurret __instance, ref Vector3 __result, Rot8 rot)
        {
            var comp = __instance.vehicle.GetComp<CompFlightMode>();
            if (comp != null && __instance.vehicle.InFlightModeOrNonStandardAngle(comp))
            {
                __result = __instance.TurretDrawLocFor(rot);
                return false;
            }
            return true;
        }
    }
}
