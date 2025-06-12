using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BM_ApparelImmunity
{
    public class BM_ApparelImmunityMod : Mod
    {
        public BM_ApparelImmunityMod(ModContentPack pack) : base(pack)
        {
            new Harmony("BM_ApparelImmunityMod").PatchAll();
        }
    }


}
