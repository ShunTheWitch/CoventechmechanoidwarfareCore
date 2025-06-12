using HarmonyLib;
using UnityEngine;
using Verse;

namespace VehicleMechanitorControl
{
    public class VehicleMechanitorControlMod : Mod
    {
        public VehicleMechanitorControlMod(ModContentPack pack) : base(pack)
        {
			new Harmony("VehicleMechanitorControlMod").PatchAll();
        }
    }
}
