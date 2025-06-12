using Verse;

namespace taranchuk_vehicleabilities
{

    public class PawnSpawner : ThingWithComps
    {
        public Pawn pawn;

        public int spawnInTicks;

        public int effecterDuration;

        private Effecter warmupEffecter;

        public EffecterDef effecterDef;

        public override void Tick()
        {
            base.Tick();
            spawnInTicks--;
            if (spawnInTicks == 0)
            {
                GenSpawn.Spawn(pawn, Position, Map);
            }
            if (spawnInTicks < 0)
            {
                effecterDuration--;
                if (warmupEffecter == null)
                {
                    warmupEffecter = effecterDef.Spawn(pawn.Position, pawn.MapHeld);
                    warmupEffecter.Trigger(pawn, pawn);
                }
                warmupEffecter?.EffectTick(pawn, pawn);
                if (effecterDuration <= 0)
                {
                    warmupEffecter.Cleanup();
                    warmupEffecter = null;
                    Thing.allowDestroyNonDestroyable = true;
                    Destroy();
                    Thing.allowDestroyNonDestroyable = false;
                }
            }

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref spawnInTicks, "spawnInTicks");
            Scribe_Values.Look(ref effecterDuration, "effecterDuration");
            Scribe_Defs.Look(ref effecterDef, "effecterDef");
        }
    }
}
