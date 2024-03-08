using Verse;

namespace taranchuk_vehicleabilities
{

    public class PawnSpawner : ThingWithComps
    {
        public Pawn pawn;

        public int spawnInTicks;

        public override void Tick()
        {
            base.Tick();
            spawnInTicks--;
            if (spawnInTicks <= 0)
            {
                GenSpawn.Spawn(pawn, Position, Map);
                Thing.allowDestroyNonDestroyable = true;
                Destroy();
                Thing.allowDestroyNonDestroyable = false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref spawnInTicks, "spawnInTicks");
        }
    }
}
