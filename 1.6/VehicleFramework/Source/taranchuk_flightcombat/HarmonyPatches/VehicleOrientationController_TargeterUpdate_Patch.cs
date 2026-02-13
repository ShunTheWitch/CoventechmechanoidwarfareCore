using System.Linq;
using HarmonyLib;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleOrientationController), nameof(VehicleOrientationController.TargeterUpdate))]
    public static class VehicleOrientationController_TargeterUpdate_Patch
    {
        public static bool Prefix(VehicleOrientationController __instance)
        {
            var isMultiSelect = __instance.vehicles.Count > 1;

            if (!isMultiSelect)
            {
                return true;
            }

            if (!__instance.IsDragging)
            {
                return true;
            }

            var isFlyingVehicles = __instance.vehicles.All(v => v.GetComp<CompFlightMode>()?.InAir == true);
            if (!isFlyingVehicles)
            {
                return true;
            }

            var lineAngle = (__instance.end.ToVector3() - __instance.start.ToVector3()).AngleFlat() + 90f;
            Log.Message("WtF:?");
            var vehicleAltitude = AltitudeLayer.MetaOverlays.AltitudeFor();
            var lineAltitude = vehicleAltitude - 0.03658537f;

            var a = __instance.start.ToVector3ShiftedWithAltitude(lineAltitude);
            var b = __instance.end.ToVector3ShiftedWithAltitude(lineAltitude);
            GenDraw.DrawLineBetween(a, b, VehicleOrientationController.GotoBetweenLineMaterial, 0.9f);

            for (int i = 0; i < __instance.vehicles.Count; i++)
            {
                var drawVehicle = __instance.vehicles[i];
                var dest = __instance.dests[i];
                if (drawVehicle.Spawned && dest.IsValid && !dest.Fogged(drawVehicle.Map))
                {
                    var comp = drawVehicle.GetComp<CompFlightMode>();
                    if (comp != null && comp.InAir)
                    {
                        var drawPos = dest.ToVector3ShiftedWithAltitude(vehicleAltitude);
                        var flightGraphic = comp.FlightGraphic;
                        if (flightGraphic != null)
                        {
                            var angle = lineAngle + comp.FlightAngleOffset;
                            var rot = Quaternion.AngleAxis(angle, Vector3.up);
                            var matrix = Matrix4x4.TRS(drawPos, rot, new Vector3(flightGraphic.drawSize.x, 1f, flightGraphic.drawSize.y));
                            Graphics.DrawMesh(MeshPool.plane10, matrix, flightGraphic.MatAt(Rot4.North), 0);
                        }
                    }
                }
            }

            return false;
        }
    }
}
