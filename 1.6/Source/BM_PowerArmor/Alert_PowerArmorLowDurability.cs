using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace BM_PowerArmor
{
    public class Alert_PowerArmorLowDurability : Alert_Critical
    {
        private List<Pawn> Pawns
        {
            get
            {
                return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists
                    .Where(x => Validator(x)).ToList();
            }
        }

        private static bool Validator(Pawn x)
        {
            return x.RaceProps.Humanlike && x.apparel.WornApparel.Any(x => x.GetComp<CompPowerArmor>() != null &&
                                (x.HitPoints / (float)x.MaxHitPoints) >= 0.1f && (x.HitPoints / (float)x.MaxHitPoints) < 0.5f);
        }

        public override string GetLabel()
        {
            return "BM.PowerArmorLowDurability".Translate();
        }

        public override TaggedString GetExplanation()
        {
            var list = new StringBuilder();
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists
                    .Where(x => x.RaceProps.Humanlike))
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    var comp = apparel.GetComp<CompPowerArmor>();
                    if (comp != null)
                    {
                        var durability = (apparel.HitPoints / (float)apparel.MaxHitPoints);
                        if (durability >= 0.1f && durability < 0.5f)
                        {
                            list.AppendLine("  - " + pawn.NameShortColored.Resolve() + " (" + (apparel.HitPoints / (float)apparel.MaxHitPoints).ToStringPercent() + ")");
                        }
                    }
                }
            }
            return "BM.PowerArmorLowDurabilityDesc".Translate(list);
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(Pawns);
        }
    }
}
