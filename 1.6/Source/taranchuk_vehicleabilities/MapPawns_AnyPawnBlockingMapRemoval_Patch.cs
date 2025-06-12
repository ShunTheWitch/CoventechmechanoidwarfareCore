using HarmonyLib;
using System.Linq;
using Verse;

namespace taranchuk_vehicleabilities
{
    [HarmonyPatch(typeof(MapPawns), "AnyPawnBlockingMapRemoval", MethodType.Getter)]
    public static class MapPawns_AnyPawnBlockingMapRemoval_Patch
    {
        public static void Postfix(ref bool __result, Map ___map)
        {
            if (!__result)
            {
                __result = ___map.listerThings.AllThings.OfType<PawnSpawner>().Any();
            }
        }
    }
}
