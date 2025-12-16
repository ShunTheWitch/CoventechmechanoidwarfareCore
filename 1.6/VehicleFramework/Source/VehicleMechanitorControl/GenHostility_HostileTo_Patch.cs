using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(GenHostility), "HostileTo", new Type[] {typeof(Thing), typeof(Thing) })]
    public static class GenHostility_HostileTo_Patch
    {
        public static bool checkingNow;
        public static void Prefix() => checkingNow = true;
        public static void Postfix() => checkingNow = false;
    }
}
