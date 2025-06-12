using RimWorld;
using UnityEngine;
using Verse;

namespace XenotypeTransformation
{
    public static class GeneUtils
    {
        public static Gene ApplyGene(GeneDef geneDef, Pawn pawn, bool xenogene)
        {
            var gene = pawn.genes.GetGene(geneDef);
            if (gene is null)
            {
                gene = pawn.genes.AddGene(geneDef, xenogene);
            }
            if (gene != null)
            {
                ApplyGene(gene, pawn);
                return gene;
            }
            return null;
        }

        public static void ApplyGene(Gene gene, Pawn pawn)
        {
            OverrideAllConflicting(gene, pawn);
            if (gene.def.graphicData != null && gene.def.graphicData.skinIsHairColor)
            {
                pawn.story.skinColorOverride = pawn.story.HairColor;
            }
            if (gene.def.hairColorOverride.HasValue)
            {
                Color value = gene.def.hairColorOverride.Value;
                if (gene.def.randomBrightnessFactor != 0f)
                {
                    value *= 1f + Rand.Range(0f - gene.def.randomBrightnessFactor, gene.def.randomBrightnessFactor);
                }
                pawn.story.HairColor = value.ClampToValueRange(GeneTuning.HairColorValueRange);
            }
            if (gene.def.skinColorBase.HasValue)
            {
                if (gene.def.skinColorBase.HasValue)
                {
                    pawn.story.SkinColorBase = gene.def.skinColorBase.Value;
                }
            }
            if (ModLister.BiotechInstalled)
            {
                if (gene.def.skinColorOverride.HasValue)
                {
                    if (gene.def.skinColorOverride.HasValue)
                    {
                        Color value2 = gene.def.skinColorOverride.Value;
                        if (gene.def.randomBrightnessFactor != 0f)
                        {
                            value2 *= 1f + Rand.Range(0f - gene.def.randomBrightnessFactor, gene.def.randomBrightnessFactor);
                        }
                        pawn.story.skinColorOverride = value2.ClampToValueRange(GeneTuning.SkinColorValueRange);
                    }
                }
                if (gene.def.bodyType.HasValue && !pawn.DevelopmentalStage.Juvenile())
                {
                    if (gene.def.bodyType.HasValue)
                    {
                        pawn.story.bodyType = gene.def.bodyType.Value.ToBodyType(pawn);
                    }
                }
                if (!gene.def.forcedHeadTypes.NullOrEmpty())
                {
                    if (!gene.def.forcedHeadTypes.NullOrEmpty())
                    {
                        pawn.story.TryGetRandomHeadFromSet(gene.def.forcedHeadTypes);
                    }
                }
                if ((gene.def.forcedHair != null || gene.def.hairTagFilter != null)
                    && !PawnStyleItemChooser.WantsToUseStyle(pawn, pawn.story.hairDef))
                {
                    pawn.story.hairDef = PawnStyleItemChooser.RandomHairFor(pawn);
                }
                if (gene.def.beardTagFilter != null && pawn.style != null
                    && !PawnStyleItemChooser.WantsToUseStyle(pawn, pawn.style.beardDef))
                {
                    pawn.style.beardDef = PawnStyleItemChooser.RandomBeardFor(pawn);
                }
                if (gene.def.graphicData?.fur != null)
                {
                    pawn.story.furDef = gene.def.graphicData.fur;
                }


                if (gene.def.soundCall != null)
                {
                    PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);
                }
                pawn.needs?.AddOrRemoveNeedsAsAppropriate();
                pawn.health.hediffSet.DirtyCache();
                pawn.skills?.Notify_GenesChanged();
                pawn.Notify_DisabledWorkTypesChanged();
            }
        }

        public static void OverrideAllConflicting(Gene gene, Pawn pawn)
        {
            gene.OverrideBy(null);
            foreach (Gene item in pawn.genes.GenesListForReading)
            {
                if (item != gene && item.def.ConflictsWith(gene.def))
                {
                    item.OverrideBy(gene);
                }
            }
        }
    }
}
