using RimWorld;
using Verse;
using Verse.AI;

namespace taranchuk_ocean
{
    public class WorkGiver_DrillAtMineralExtractor : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(CVN_DefOf.CVN_MineralExtractor);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            Building building = t as Building;
            if (building == null)
            {
                return false;
            }
            if (building.IsForbidden(pawn))
            {
                return false;
            }
            var comp = building.TryGetComp<CompMineralExtractor>();
            if (comp.CanOperate is false)
            {
                return false;
            }
            if (comp.Workers.Count >= 4f)
            {
                return false;
            }
            if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
            {
                return false;
            }
            if (building.IsBurning())
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(CVN_DefOf.CVN_OperateMineralExtractor, t, 1500, checkOverrideOnExpiry: true);
        }
    }
}
