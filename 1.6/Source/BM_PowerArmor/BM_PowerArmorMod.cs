using HarmonyLib;
using UnityEngine;
using Verse;

namespace BM_PowerArmor
{
    public class BM_PowerArmorMod : Mod
    {
        public BM_PowerArmorMod(ModContentPack pack) : base(pack)
        {
			new Harmony("BM_PowerArmorMod").PatchAll();
        }
    }
}
