using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace XenotypeTransformation
{
    public class HediffCompProperties_XenotypeTransformation : HediffCompProperties
    {
        public XenotypeDef transformInto;
        public XenotypeDef transformBack;
        public HediffDef transformBackHediff;
        public string transformBackLabel;
        public string transformBackDesc;
        public string transformBackIcon;
        public List<AgeEntry> durations;
        public HediffCompProperties_XenotypeTransformation()
        {
            this.compClass = typeof(HediffComp_XenotypeTransformation);
        }
    }

    public class HediffComp_XenotypeTransformation : HediffComp
    {
        public HediffCompProperties_XenotypeTransformation Props => base.props as HediffCompProperties_XenotypeTransformation;
        public List<GeneDef> savedSkinGenesXenogenes = new List<GeneDef>();
        public List<GeneDef> savedSkinGenesEndogenes = new List<GeneDef>();

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            savedSkinGenesEndogenes = Pawn.genes.Endogenes.Where(x => x.Active 
                && (x.def.skinColorBase.HasValue || x.def.skinColorOverride.HasValue)).Select(x => x.def).ToList();
            savedSkinGenesXenogenes = Pawn.genes.Xenogenes.Where(x => x.Active
                && (x.def.skinColorBase.HasValue || x.def.skinColorOverride.HasValue)).Select(x => x.def).ToList();
            Pawn.genes.SetXenotype(Props.transformInto);
            var ageEntry = Props.durations.FirstOrDefault(x => x.age.Includes(parent.pawn.ageTracker.AgeBiologicalYearsFloat));
            parent.TryGetComp<HediffComp_Disappears>().ticksToDisappear = ageEntry.duration;
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Pawn.genes.SetXenotype(Props.transformBack);
            if (Props.transformBackHediff != null)
            {
                Pawn.health.AddHediff(Props.transformBackHediff);
            }
            var skinGenes = Pawn.genes.GenesListForReading.Where(x => x.def.skinColorBase.HasValue || x.def.skinColorOverride.HasValue).ToList();
            foreach (var gene in skinGenes)
            {
                Pawn.genes.RemoveGene(gene);
            }
            foreach (var gene in savedSkinGenesEndogenes)
            {
                GeneUtils.ApplyGene(gene, Pawn, false);
            }
            foreach (var gene in savedSkinGenesXenogenes)
            {
                GeneUtils.ApplyGene(gene, Pawn, true);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn.IsColonistPlayerControlled)
            {
                yield return new Command_Action
                {
                    defaultLabel = Props.transformBackLabel,
                    defaultDesc = Props.transformBackDesc,
                    icon = ContentFinder<Texture2D>.Get(Props.transformBackIcon),
                    action = () =>
                    {
                        this.Pawn.health.RemoveHediff(parent);
                    }
                };
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Collections.Look(ref savedSkinGenesEndogenes, "savedSkinGenesEndogenes", LookMode.Def);
            Scribe_Collections.Look(ref savedSkinGenesXenogenes, "savedSkinGenesXenogenes", LookMode.Def);
        }
    }
}
