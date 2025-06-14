using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace BM_PowerArmor
{
    public class Alert_PowerArmorCriticallyLowDurability : Alert_Critical
    {
        private List<Pawn> Pawns
        {
            get
            {
                return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists
                    .Where(x => Validator(x)).ToList();
            }
        }

        private static bool Validator(Pawn x)
        {
            return x.RaceProps.Humanlike && x.apparel.WornApparel.Any(x => x.GetComp<CompPowerArmor>() != null &&
                                (x.HitPoints / (float)x.MaxHitPoints) < 0.1f);
        }

        public override string GetLabel()
        {
            return "BM.PowerArmorCriticallyLowDurability".Translate();
        }

        public override TaggedString GetExplanation()
        {
            var list = new StringBuilder();
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists
                    .Where(x => x.RaceProps.Humanlike))
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    var comp = apparel.GetComp<CompPowerArmor>();
                    if (comp != null && (apparel.HitPoints / (float)apparel.MaxHitPoints) < 0.1f)
                    {
                        list.AppendLine("  - " + pawn.NameShortColored.Resolve() + " (" + (apparel.HitPoints / (float)apparel.MaxHitPoints).ToStringPercent() + ")");
                    }
                }
            }
            return "BM.PowerArmorCriticallyLowDurabilityDesc".Translate(list);
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(Pawns);
        }
    }
}
