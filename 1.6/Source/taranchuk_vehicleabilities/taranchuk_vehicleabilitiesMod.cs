using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;

namespace taranchuk_vehicleabilities
{
    public class taranchuk_vehicleabilitiesMod : Mod
    {
        public taranchuk_vehicleabilitiesMod(ModContentPack content) : base(content)
        {
            new Harmony("taranchuk_vehicleabilitiesMod").PatchAll();
        }
    }
}
