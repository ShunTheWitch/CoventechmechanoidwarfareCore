using HarmonyLib;
using Verse;

namespace BuildingEquipment
{
    public class BuildingEquipmentMod : Mod
    {
        public BuildingEquipmentMod(ModContentPack pack) : base(pack)
        {
            new Harmony("BuildingEquipmentMod").PatchAll();
        }
    }
}
