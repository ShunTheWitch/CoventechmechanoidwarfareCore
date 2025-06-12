using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace taranchuk_mobilecrypto
{
    public class CompProperties_MobileCrypto : CompProperties
    {
        public int maxPawnCapacity = 1;
        public Command captureCommand;
        public Command releaseCommand;
        public JobDef captureJob;
        public JobDef releaseJob;
        public HediffDef hediffStoring;
        public CompProperties_MobileCrypto()
        {
            this.compClass = typeof(CompMobileCrypto);
        }
    }

    public class CompMobileCrypto : ThingComp, IThingHolder
    {
        public bool IsApparel => parent is Apparel;

        private ThingOwner innerContainer;

        public List<Pawn> StoredPawns => innerContainer.OfType<Pawn>().ToList();

        public CompProperties_MobileCrypto Props => props as CompProperties_MobileCrypto;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                innerContainer = new ThingOwner<Thing>(this);
            }
        }

        public Pawn Wearer
        {
            get
            {
                var apparel = parent as Apparel;
                if (apparel != null)
                {
                    return apparel.Wearer;
                }
                return parent as Pawn;
            }
        }

        public Thing Holder
        {
            get
            {
                var apparel = parent as Apparel;
                if (apparel?.Wearer != null)
                {
                    if (apparel.Wearer.Dead)
                    {
                        return apparel.Wearer.Corpse;
                    }
                    return apparel.Wearer;
                }
                if (parent is Pawn pawn && pawn.Dead)
                {
                    return pawn.Corpse;
                }
                return parent;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (IsApparel is false)
            {
                foreach (var gizmo in GetGizmos())
                {
                    yield return gizmo;
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (IsApparel)
            {
                foreach (var gizmo in GetGizmos())
                {
                    yield return gizmo;
                }
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }


        public IEnumerable<Gizmo> GetGizmos()
        {
            if (Wearer?.Faction == Faction.OfPlayer)
            {
                if (StoredPawns.Count < Props.maxPawnCapacity)
                {
                    var captureCommand = Props.captureCommand.GetCommand();
                    captureCommand.action = delegate
                    {
                        Find.Targeter.BeginTargeting(ForPawns, delegate (LocalTargetInfo x)
                        {
                            Wearer.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Props.captureJob, x, parent));

                        });
                    };
                    yield return captureCommand;
                }
                foreach (var pawn in StoredPawns)
                {
                    var releaseCommand = Props.releaseCommand.GetCommand();
                    releaseCommand.defaultLabel = releaseCommand.defaultLabel.Formatted(pawn);
                    releaseCommand.action = delegate
                    {
                        Wearer.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Props.releaseJob, pawn, parent, Holder));
                    };
                    yield return releaseCommand;
                }
            }
        }

        public TargetingParameters ForPawns => new TargetingParameters
        {
            canTargetPawns = true,
            validator = (TargetInfo x) => x.Thing is Pawn pawn && pawn != parent
        };

        public void StorePawn(Pawn pawn)
        {
            pawn.DeSpawn();
            innerContainer.TryAdd(pawn);
            if (Props.hediffStoring != null)
            {
                SetHediff();
            }
        }

        public void ReleasePawn(Pawn pawn)
        {
            innerContainer.Remove(pawn);
            GenSpawn.Spawn(pawn, Holder.Position, Holder.MapHeld);
            if (Props.hediffStoring != null)
            {
                SetHediff();
            }
        }

        public void SetHediff()
        {
            var hediff = Wearer.health.hediffSet.GetFirstHediffOfDef(Props.hediffStoring);
            if (hediff is null)
            {
                hediff = HediffMaker.MakeHediff(Props.hediffStoring, Wearer);
                Wearer.health.AddHediff(hediff);
            }
            hediff.Severity = StoredPawns.Count;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }
    }
}
