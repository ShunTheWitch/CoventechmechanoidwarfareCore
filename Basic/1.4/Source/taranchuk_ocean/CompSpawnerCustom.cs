using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace taranchuk_ocean
{
    public class CompProperties_Spawner : CompProperties
    {
        public List<ThingCountWeighted> things;
        public int cooldownTicks;
        public bool spawnInInventory;
        public bool passive;

        public CompProperties_Spawner()
        {
            this.compClass = typeof(CompSpawnerCustom);
        }
    }

    public class CompSpawnerCustom : ThingComp
    {
        public CompProperties_Spawner Props => base.props as CompProperties_Spawner;
        public int lastSpawnedTicks;
        public int ticksUntilSpawn;
        public virtual bool CanOperate => parent.Faction == Faction.OfPlayer;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!respawningAfterLoad && Props.passive)
            {
                ResetCountdown();
            }
        }

        public override void CompTick()
        {
            TickInterval(1);
        }

        public override void CompTickRare()
        {
            TickInterval(250);
        }

        protected virtual void TickInterval(int interval)
        {
            if (CanOperate && Props.passive)
            {
                ticksUntilSpawn -= interval;
                CheckShouldSpawn();
            }
        }

        protected void CheckShouldSpawn()
        {
            if (ticksUntilSpawn <= 0)
            {
                ResetCountdown();
                TryDoSpawn();
            }
        }

        protected virtual void TryDoSpawn()
        {
            lastSpawnedTicks = Find.TickManager.TicksGame;
            var thingCount = Props.things.RandomElementByWeight(x => x.weight);
            var spawnCount = thingCount.count.RandomInRange;
            List<Thing> things = new List<Thing>();
            while (spawnCount > 0)
            {
                Thing thing = ThingMaker.MakeThing(thingCount.thingDef);
                thing.stackCount = Mathf.Min(thing.def.stackLimit, spawnCount);
                spawnCount -= thing.stackCount;
                things.Add(thing);
            }

            if (Props.spawnInInventory)
            {
                var pawn = parent as Pawn;
                foreach (var thing in things)
                {
                    pawn.inventory.TryAddItemNotForSale(thing);
                }
            }
            else
            {
                foreach (var thing in things)
                {
                    if (TryFindRandomCellNear(parent.Position, parent.Map, 3, out var result))
                    {
                        if (result.IsValid)
                        {
                            GenPlace.TryPlaceThing(thing, result, parent.Map, ThingPlaceMode.Near, out var lastResultingThing);
                        }
                    }
                }
            }
        }

        public bool TryFindRandomCellNear(IntVec3 root, Map map, float radius, out IntVec3 result)
        {
            foreach (var cell in GenRadial.RadialCellsAround(root, radius, true))
            {
                if (cell.GetFirstItem(map) is null)
                {
                    result = cell;
                    return true;
                }
            }
            result = IntVec3.Invalid;
            return false;
        }

        protected void ResetCountdown()
        {
            ticksUntilSpawn = TicksUntilSpawn;
        }

        public int TicksUntilSpawn => Props.cooldownTicks;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksUntilSpawn, "ticksUntilSpawn", 0);
            Scribe_Values.Look(ref lastSpawnedTicks, "lastSpawnedTicks", 0);
        }
    }
}
