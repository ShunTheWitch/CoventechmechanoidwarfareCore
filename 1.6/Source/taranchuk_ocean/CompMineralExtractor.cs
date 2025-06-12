using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace taranchuk_ocean
{
    public class CompProperties_MineralExtractor : CompProperties_Spawner
    {
        public int workAmount;
        public CompProperties_MineralExtractor()
        {
            this.compClass = typeof(CompMineralExtractor);
        }
    }

    public class CompMineralExtractor : CompSpawnerCustom
    {
        public override bool CanOperate => base.CanOperate && (powerComp is null || powerComp.PowerOn);
        public int workDone;

        private Dictionary<Pawn, IntVec3> workers = new Dictionary<Pawn, IntVec3>();
        public Dictionary<Pawn, IntVec3> Workers
        {
            get
            {
                workers.RemoveAll(x => x.Key.DestroyedOrNull() || x.Key.Dead 
                || x.Key.CurJobDef != CVN_DefOf.CVN_OperateMineralExtractor 
                || x.Key.CurJob?.targetA.Thing != this.parent);
                return workers;
            }
        }

        private CompPowerTrader powerComp;

        public new CompProperties_MineralExtractor Props => props as CompProperties_MineralExtractor;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            powerComp = parent.TryGetComp<CompPowerTrader>();
        }

        public void RegisterWorker(Pawn worker)
        {
            var freeSpot = parent.InteractionCells.Where(x => Workers.Values.Contains(x) is false).RandomElement();
            Workers[worker] = freeSpot;
        }

        public void DrillWorkDone(Pawn driller)
        {
            workDone++;
            if (workDone >= Props.workAmount)
            {
                workDone = 0;
                TryDoSpawn();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref workDone, "workDone");
            Scribe_Collections.Look(ref workers, "workers", LookMode.Reference, LookMode.Value, ref pawnKeys, ref cellsValues);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                workers ??= new Dictionary<Pawn, IntVec3>();
            }
        }

        private List<Pawn> pawnKeys;
        private List<IntVec3> cellsValues;
    }


}
