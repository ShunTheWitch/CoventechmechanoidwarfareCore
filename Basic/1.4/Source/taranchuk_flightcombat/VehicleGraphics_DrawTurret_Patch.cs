using HarmonyLib;
using SmashTools;
using System;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleGraphics), nameof(VehicleGraphics.DrawTurret), new Type[] { typeof(VehicleTurret), typeof(Vector3), typeof(Rot8)})]
    public static class VehicleGraphics_DrawTurret_Patch
    {
        public static bool Prefix(VehicleTurret turret, Vector3 drawPos, Rot8 rot)
        {
            var comp = turret.vehicle.GetComp<CompFlightMode>();
            if (comp != null && turret.vehicle.InFlightModeOrNonStandardAngle(comp))
            {
                rot = Rot8.West;
                DrawTurret(comp, turret, drawPos, rot);
                return false;
            }
            return true;
        }

        public static void DrawTurret(CompFlightMode comp, VehicleTurret turret, Vector3 drawPos, Rot8 rot)
        {
            try
            {
                Vector3 turretDrawLoc = turret.TurretDrawLocFor(rot).RotatedBy(comp.CurAngle);
                Vector3 rootPos = drawPos + turretDrawLoc;
                Vector3 recoilOffset = Vector3.zero;
                Vector3 parentRecoilOffset = Vector3.zero;
                if (turret.recoilTracker != null && turret.recoilTracker.Recoil > 0f)
                {
                    recoilOffset = Ext_Math.PointFromAngle(Vector3.zero, turret.recoilTracker.Recoil, turret.recoilTracker.Angle);
                }
                if (turret.attachedTo?.recoilTracker != null && turret.attachedTo.recoilTracker.Recoil > 0f)
                {
                    parentRecoilOffset = Ext_Math.PointFromAngle(Vector3.zero, turret.attachedTo.recoilTracker.Recoil, turret.attachedTo.recoilTracker.Angle);
                }
                Mesh cannonMesh = turret.CannonGraphic.MeshAt(rot);
                Graphics.DrawMesh(cannonMesh, rootPos + recoilOffset + parentRecoilOffset, turret.TurretRotation.ToQuat(), turret.CannonMaterial, 0);
                VehicleGraphics.DrawTurretOverlays(turret, rootPos + parentRecoilOffset, rot);
            }
            catch (Exception ex)
            {
                Log.Error($"Error occurred during rendering of attached thing on {turret.vehicle.Label}. Exception: {ex}");
            }
        }
    }
}
