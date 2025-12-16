using HarmonyLib;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(SelectionDrawer), "DrawSelectionBracketFor")]
    public static class SelectionDrawer_DrawSelectionBracketFor_Patch
    {
        [HarmonyPriority(Priority.First)] 
        public static bool Prefix(object obj, Material overrideMat)
        {
            VehiclePawn vehicle = (obj as VehiclePawn) ?? (obj as VehicleBuilding)?.vehicle;
            if (vehicle != null)
            {
                var comp = vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    Vector3[] brackets = new Vector3[4];

                    float angle = vehicle.Angle;

                    Ext_Pawn.CalculateSelectionBracketPositionsWorldForMultiCellPawns(
                        brackets, 
                        vehicle, 
                        vehicle.DrawPos, 
                        vehicle.RotatedSize.ToVector2(), 
                        SelectionDrawer.SelectTimes, 
                        Vector2.one, 
                        angle
                    );

                    int angleInt = Mathf.CeilToInt(angle);
                    for (int i = 0; i < 4; i++)
                    {
                        Quaternion rotation = Quaternion.AngleAxis((float)angleInt, Vector3.up);
                        Material material = overrideMat != null ? overrideMat : MaterialPresets.SelectionBracketMat;
                        Graphics.DrawMesh(MeshPool.plane10, brackets[i], rotation, material, 0);
                        angleInt -= 90;
                    }
                    return false;
                }
            }

            return true;
        }
    }
}
