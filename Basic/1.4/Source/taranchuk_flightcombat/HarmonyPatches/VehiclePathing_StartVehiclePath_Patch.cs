﻿using HarmonyLib;
using Vehicles;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    [HarmonyPatch(typeof(VehiclePathing), nameof(VehiclePathing.StartVehiclePath))]
    public static class VehiclePathing_StartVehiclePath_Patch
    {
        public static bool Prefix(LocalTargetInfo __0, PathEndMode __1, Pawn __2, ref bool __result)
        {
            if (__2 is VehiclePawn vehicle)
            {
                var comp = vehicle.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    comp.SetTarget(__0);
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
