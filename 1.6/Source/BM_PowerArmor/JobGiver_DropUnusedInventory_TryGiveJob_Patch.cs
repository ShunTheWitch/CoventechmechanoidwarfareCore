using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace BM_PowerArmor
{
    [HarmonyPatch(typeof(JobGiver_DropUnusedInventory), "TryGiveJob")]
    public static class JobGiver_DropUnusedInventory_TryGiveJob_Patch
    {
        public static void Postfix(ref Job __result, Pawn pawn)
        {
            if (__result is null)
            {
                if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    return;
                }
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    var comp = apparel.GetComp<CompPowerArmor>();
                    if (comp != null && comp.autopark)
                    {
                        var spot = pawn.GetPowerArmorParkingSpot();
                        if (spot.DestroyedOrNull() is false && spot.Position.GetThingList(spot.Map)
                            .Any(x => x is Building && x.TryGetComp<CompPowerArmor>() != null) is false
                            && pawn.CanReserveAndReach(spot, PathEndMode.OnCell, Danger.Deadly))
                        {
                            __result = JobMaker.MakeJob(BM_DefOf.BM_ParkPowerArmor, apparel, spot);
                            return;
                        }
                    }
                }
            }
        }
    }
}
