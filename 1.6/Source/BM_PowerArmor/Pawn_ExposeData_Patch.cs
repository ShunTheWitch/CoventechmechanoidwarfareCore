using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace BM_PowerArmor
{
    [HarmonyPatch(typeof(Pawn), "ExposeData")]
    public static class Pawn_ExposeData_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            var powerArmorParkingSpot = __instance.GetPowerArmorParkingSpot();
            Scribe_References.Look(ref powerArmorParkingSpot, "powerArmorParkingSpot");
            if (powerArmorParkingSpot != null)
            {
                __instance.SetPowerArmorParkingSpot(powerArmorParkingSpot);
            }
            var powerArmor = __instance.GetPowerArmor();
            Scribe_References.Look(ref powerArmor, "powerArmor");
            if (powerArmor != null)
            {
                __instance.SetPowerArmor(powerArmor);
            }
        }

        private static Dictionary<Pawn, Building> pawnPowerArmorParkingSpots = new Dictionary<Pawn, Building>();
        public static Building GetPowerArmorParkingSpot(this Pawn pawn)
        {
            if (pawnPowerArmorParkingSpots.TryGetValue(pawn, out var spot))
            {
                return spot;
            }
            return null;
        }

        public static void SetPowerArmorParkingSpot(this Pawn pawn, Building powerArmorParkingSpot)
        {
            pawnPowerArmorParkingSpots[pawn] = powerArmorParkingSpot;
        }


        private static Dictionary<Pawn, Building> pawnPowerArmors = new Dictionary<Pawn, Building>();
        public static Building GetPowerArmor(this Pawn pawn)
        {
            if (pawnPowerArmors.TryGetValue(pawn, out var spot))
            {
                return spot;
            }
            return null;
        }

        public static void SetPowerArmor(this Pawn pawn, Building powerArmor)
        {
            pawnPowerArmors[pawn] = powerArmor;
        }
    }
}
