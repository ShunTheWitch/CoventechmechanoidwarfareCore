using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace taranchuk_mechanicalarms
{
    public class taranchuk_mechanicalarmsMod : Mod
    {
        public taranchuk_mechanicalarmsMod(ModContentPack pack) : base(pack)
        {
			new Harmony("taranchuk_mechanicalarmsMod").PatchAll();
        }
    }


}
