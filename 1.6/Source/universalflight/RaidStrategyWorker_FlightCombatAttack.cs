using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace universalflight
{
    public class RaidStrategyWorker_FlightCombatAttack : RaidStrategyWorker
    {
        public override LordJob MakeLordJob(IncidentParms parms, Map map, List<Pawn> pawns, int raidSeed)
        {
            IntVec3 originCell = (parms.spawnCenter.IsValid ? parms.spawnCenter : pawns[0].PositionHeld);
            if (parms.attackTargets != null && parms.attackTargets.Count > 0)
            {
                return new LordJob_AssaultThings(parms.faction, parms.attackTargets);
            }
            if (parms.faction.HostileTo(Faction.OfPlayer))
            {
                return new LordJob_AssaultColony(parms.faction, canTimeoutOrFlee: parms.canTimeoutOrFlee, canKidnap: parms.canKidnap, sappers: false, useAvoidGridSmart: false, canSteal: parms.canSteal);
            }
            RCellFinder.TryFindRandomSpotJustOutsideColony(originCell, map, out var result);
            return new LordJob_AssistColony(parms.faction, result);
        }
    }
}
