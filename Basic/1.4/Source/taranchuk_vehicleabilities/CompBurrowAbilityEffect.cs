using RimWorld;
using Verse;

namespace taranchuk_vehicleabilities
{
    public class CompProperties_Burrow : CompProperties_AbilityEffect
    {
        public EffecterDef warmupEffecter;
        public int baseRespawnDuration;
        public float respawnDurationPerCell;
        public ThingDef spawnerThing;
        public CompProperties_Burrow()
        {
            compClass = typeof(CompBurrowAbilityEffect);
        }
    }

    public class CompBurrowAbilityEffect : CompAbilityEffect
    {
        public CompProperties_Burrow Props => base.props as CompProperties_Burrow;
        private Effecter warmupEffecter;
        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.Casting)
            {
                if (warmupEffecter == null)
                {
                    warmupEffecter = Props.warmupEffecter.Spawn(parent.pawn.Position, parent.pawn.MapHeld);
                    warmupEffecter.Trigger(parent.pawn, parent.pawn);
                }
                warmupEffecter?.EffectTick(parent.pawn, parent.pawn);
            }
            else if (warmupEffecter != null)
            {
                warmupEffecter.Cleanup();
                warmupEffecter = null;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var spawner = ThingMaker.MakeThing(Props.spawnerThing) as PawnSpawner;
            spawner.pawn = parent.pawn;
            spawner.spawnInTicks = Props.baseRespawnDuration;
            spawner.spawnInTicks += (int)(Props.respawnDurationPerCell * parent.pawn.Position.DistanceTo(target.Cell));
            GenSpawn.Spawn(spawner, target.Cell, parent.pawn.Map);
            parent.pawn.DeSpawn();
        }
    }
}
