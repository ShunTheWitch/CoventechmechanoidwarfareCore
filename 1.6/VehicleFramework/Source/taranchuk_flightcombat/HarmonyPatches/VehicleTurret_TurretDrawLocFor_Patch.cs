using HarmonyLib;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.DrawPosition), typeof(Rot8))]
    public static class VehicleTurret_TurretDrawLocFor_Patch
    {
        public static void Prefix(VehicleTurret __instance, ref Rot8 rot)
        {
            var comp = __instance.vehicle?.GetComp<CompFlightMode>();
            if (comp != null && comp.InAir)
            {
                rot = comp.FlightRotation;
            }
        }
    }
}
