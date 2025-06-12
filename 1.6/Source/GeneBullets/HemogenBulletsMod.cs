using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace GeneBullets
{
    public class VerbProperties_ResourceCost : VerbProperties
    {
        public float resourceCost;
        public GeneDef resourceGene;
    }

    public class Verb_Shoot_GeneResource : Verb_Shoot
    {
        public VerbProperties_ResourceCost Props => base.verbProps as VerbProperties_ResourceCost;
        public override bool Available()
        {
            var gene = CasterPawn.genes?.GetGene(Props.resourceGene) as Gene_Resource;
            if (gene == null)
            {
                return false;
            }
            if (gene.Value < (Props.resourceCost / 100f))
            {
                return false;
            }
            return base.Available();
        }

        public override bool TryCastShot()
        {
            var result = base.TryCastShot();
            if (result)
            {
                var gene = CasterPawn.genes.GetGene(Props.resourceGene) as Gene_Resource;
                gene.Value -= (Props.resourceCost / 100f);
            }
            return result;
        }
    }
}
