using System.Reflection;
using HarmonyLib;
using Verse;

namespace universalflight
{
    [HotSwappable]
    [HarmonyPatch]
    public static class MechSuit_Patch
    {
        public static MethodBase targetMethod;
        public static bool Prepare()
        {
            var type = AccessTools.TypeByName("taranchuk_mechsuits.ModCompatability");
            if (type == null) return false;
            targetMethod = AccessTools.Method(type, "GetAdditionalAngle");
            return targetMethod != null;
        }
        
        public static MethodBase TargetMethod()
        {
            return targetMethod;
        }
        
        public static void Postfix(ref float __result, Pawn mech)
        {
            var compFlightMode = mech.GetComp<CompFlightMode>();
            if (compFlightMode != null && compFlightMode.InAir)
            {
                __result += compFlightMode.AngleAdjusted(compFlightMode.CurAngle - compFlightMode.FlightAngleOffset);
            }
        }
    }
}
