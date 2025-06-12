using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BM_ApparelSet
{
    public class BM_ApparelSetMod : Mod
    {
        public BM_ApparelSetMod(ModContentPack pack) : base(pack)
        {
			new Harmony("BM_ApparelSetMod").PatchAll();
        }
    }
}
