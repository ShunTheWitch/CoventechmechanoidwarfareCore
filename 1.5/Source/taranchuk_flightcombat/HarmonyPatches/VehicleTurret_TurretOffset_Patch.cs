using HarmonyLib;
using SmashTools;
using System;
using System.Diagnostics;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.TurretOffset))]
    public static class VehicleTurret_TurretOffset_Patch
    {
        public static bool Prefix(VehicleTurret __instance, ref Vector3 __result, Rot8 rot)
        {
            if (UnityData.IsInMainThread && Current.ProgramState == ProgramState.Playing)
            {
                var comp = __instance.vehicle.GetComp<CompFlightMode>();
                if (comp != null && __instance.vehicle.InFlightModeOrNonStandardAngle(comp))
                {
                    __result = __instance.TurretDrawLocFor(rot);
                    return false;
                }
            }
            return true;
        }
    }
}
