using HarmonyLib;
using RimWorld;
using Verse;

namespace universalflight
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_DraftedMove), nameof(FloatMenuOptionProvider_DraftedMove.PawnGotoAction))]
    public static class FloatMenuOptionProvider_DraftedMove_PawnGotoAction_Patch
    {
        public static bool Prefix(IntVec3 clickCell, Pawn pawn, IntVec3 gotoLoc)
        {
            if (pawn != null && pawn.Faction == Faction.OfPlayer)
            {
                var comp = pawn.GetComp<CompFlightMode>();
                if (comp != null && comp.InAir)
                {
                    comp.SetTarget(gotoLoc);
                    if (comp.Props.waypointFleck != null)
                    {
                        FleckMaker.Static(gotoLoc, pawn.Map, comp.Props.waypointFleck);
                    }
                    return false;
                }
            }
            return true;
        }
    }
}
