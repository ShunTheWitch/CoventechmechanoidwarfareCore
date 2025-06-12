using HarmonyLib;
using System;
using Verse;

namespace Taranchuk_ColorPicker
{
    public class TaranchukColorPickerMod : Mod
    {
        public TaranchukColorPickerMod(ModContentPack pack) : base(pack)
        {
            new Harmony("TaranchukColorPickerMod").PatchAll();
        }
    }

    public class CompProperties_ColorPicker : CompProperties
    {
        public bool includeColorTwo;
        public string label;
        public string description;
        public string iconPath;

        public CompProperties_ColorPicker()
        {
            this.compClass = typeof(CompCustomColorPicker);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }
}
