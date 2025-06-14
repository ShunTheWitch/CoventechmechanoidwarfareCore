using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace BM_PowerArmor
{
    public class Alert_PowerArmorCriticallyLowFuel : Alert_Critical
    {
        private List<Pawn> Pawns
        {
            get
            {
                return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists
                    .Where(x => x.RaceProps.Humanlike && x.apparel.WornApparel.Any(x => x.GetComp<CompPowerArmor>()
                    is CompPowerArmor comp && comp.CompRefuelable != null && comp.CompRefuelable.HasFuel && comp.CompRefuelable.FuelPercentOfMax < 0.1f)).ToList();
            }
        }

        public override string GetLabel()
        {
            return "BM.PowerArmorsCriticalLowFuel".Translate();
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
                    if (comp != null)
                    {
                        var compRefuelable = comp.CompRefuelable;
                        if (compRefuelable != null && compRefuelable.HasFuel && compRefuelable.FuelPercentOfMax < 0.1f)
                        {
                            list.AppendLine("  - " + pawn.NameShortColored.Resolve() + " (" + compRefuelable.FuelPercentOfMax.ToStringPercent() + ")");
                        }
                    }
                }
            }
            return "BM.PowerArmorsCriticalLowFuelDesc".Translate(list.ToString().TrimEndNewlines());
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(Pawns);
        }
    }
}
