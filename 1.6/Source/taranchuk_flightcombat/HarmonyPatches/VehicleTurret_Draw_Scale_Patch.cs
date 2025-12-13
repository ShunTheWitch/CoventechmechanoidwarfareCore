using HarmonyLib;
using SmashTools.Rendering;
using UnityEngine;
using Vehicles;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), "Draw")]
    public static class VehicleTurret_Draw_Scale_Patch
    {
        // Store original graphic size before we modify it
        public static void Prefix(VehicleTurret __instance, ref Vector2? __state)
        {
            var comp = __instance.vehicle?.GetComp<CompFlightMode>();
            if (comp == null || !comp.InAir || comp.Props.flightGraphicData == null)
            {
                return;
            }

            // Only process if turret has a graphic
            if (__instance.Graphic == null)
            {
                return;
            }

            // Store the original draw size so we can restore it
            __state = __instance.Graphic.drawSize;
            
            // Calculate average scale factor
            var scaleFactors = comp.GetScaleFactors();
            var scaleFactor = (scaleFactors.x + scaleFactors.y) / 2f;
            
            // Temporarily scale the turret graphic
            __instance.Graphic.drawSize = __state.Value * scaleFactor;
        }
        
        // Restore original graphic size after drawing
        public static void Postfix(VehicleTurret __instance, Vector2? __state)
        {
            // If we stored a state, restore the original size
            if (__state.HasValue && __instance.Graphic != null)
            {
                __instance.Graphic.drawSize = __state.Value;
            }
        }
    }
}