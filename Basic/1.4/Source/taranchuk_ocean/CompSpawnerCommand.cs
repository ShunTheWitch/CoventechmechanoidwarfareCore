using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace taranchuk_ocean
{
    public class CompProperties_Extractor : CompProperties_Spawner
    {
        public string label;
        public string description;
        public string iconPath;

        public CompProperties_Extractor()
        {
            this.compClass = typeof(CompSpawnerCommand);
        }
    }

    public class CompSpawnerCommand : CompSpawnerCustom
    {
        public new CompProperties_Extractor Props => base.props as CompProperties_Extractor;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (CanOperate)
            {
                yield return new Command_ActionWithCooldown(lastSpawnedTicks, Props.cooldownTicks)
                {
                    defaultLabel = Props.label,
                    defaultDesc = Props.description,
                    icon = ContentFinder<Texture2D>.Get(Props.iconPath),
                    action = delegate
                    {
                        TryDoSpawn();
                    }
                };
            }
        }
    }
}
