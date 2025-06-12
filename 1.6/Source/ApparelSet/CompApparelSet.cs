using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BM_ApparelSet
{
    public class CompProperties_ApparelSet : CompProperties
    {
        public List<ThingDef> requiredApparels;
        public List<ThingDef> onlyWearableApparels;
        public ApparelSetEffect apparelSetEffect;

        public CompProperties_ApparelSet()
        {
            this.compClass = typeof(CompApparelSet);
        }
    }

    public class CompApparelSet : ThingComp
    {
        public CompProperties_ApparelSet Props => base.props as CompProperties_ApparelSet;
        public Pawn wearer = null;
        public Hediff wearerHediff;
        public Apparel Apparel => parent as Apparel;

        public bool FullSetWorn(Pawn pawn)
        {
            return Props.apparelSetEffect.allApparels.All(x => pawn.apparel.WornApparel.Exists(y => y.def == x));
        }

        public bool HasAllRequiredApparels(Pawn pawn)
        {
            return Props.requiredApparels.All(x => pawn.apparel.WornApparel.Exists(y => y.def == x));
        }
        public override void CompTick()
        {
            base.CompTick();
            var apparel = Apparel;
            var curWearer = apparel.Wearer;
            if (curWearer != wearer)
            {
                if (wearer != null)
                {
                    if (wearer.health.hediffSet.hediffs.Contains(wearerHediff))
                    {
                        wearer.health.RemoveHediff(wearerHediff);
                    }
                }
                wearer = curWearer;
            }
            if (wearer != null)
            {
                if (Props.apparelSetEffect != null)
                {
                    var fullsetWorn = FullSetWorn(wearer);
                    var setHediff = wearer.health.hediffSet.GetFirstHediffOfDef(Props.apparelSetEffect.hediff);
                    if (fullsetWorn && setHediff is null)
                    {
                        wearerHediff = HediffMaker.MakeHediff(Props.apparelSetEffect.hediff, wearer);
                        wearer.health.AddHediff(wearerHediff);
                    }
                    else if (fullsetWorn is false && setHediff is not null)
                    {
                        wearer.health.RemoveHediff(wearerHediff);
                        wearerHediff = null;
                    }
                }
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            foreach (var apparel in pawn.apparel.WornApparel.ToList())
            {
                if (pawn.apparel.WornApparel.Contains(apparel))
                {
                    var comp = apparel.GetComp<CompApparelSet>();
                    if (comp != null && comp.Props.requiredApparels != null && comp.HasAllRequiredApparels(pawn) is false)
                    {
                        pawn.apparel.TryDrop(apparel);
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref wearer, "wearer");
            Scribe_References.Look(ref wearerHediff, "wearerHediff");
        }
    }
}
