using HarmonyLib;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret.TurretDrawData), nameof(VehicleTurret.TurretDrawData.DrawOffset))]
    public static class TurretDrawData_DrawOffset_Scale_Patch
    {
        public static void Postfix(VehicleTurret.TurretDrawData __instance, ref Vector3 __result, Rot8 rot, float parentRotation, float rotation)
        {
            var turret = __instance.turret;
            if (turret?.vehicle == null)
            {
                return;
            }

            var comp = turret.vehicle.GetComp<CompFlightMode>();
            if (comp == null || !comp.InAir || comp.Props.flightGraphicData == null)
            {
                return;
            }

            var scaleFactors = comp.GetScaleFactors();
            __result.x *= scaleFactors.x;
            __result.z *= scaleFactors.y;
        }
    }
}
