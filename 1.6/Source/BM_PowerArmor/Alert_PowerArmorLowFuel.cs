using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace BM_PowerArmor
{
    public class Alert_PowerArmorLowFuel : Alert_Critical
    {
        private List<Pawn> Pawns
        {
            get
            {
                return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists
                    .Where(x => x.RaceProps.Humanlike && x.apparel.WornApparel.Any(x => x.GetComp<CompPowerArmor>()
                    is CompPowerArmor comp && comp.CompRefuelable != null && comp.CompRefuelable.FuelPercentOfMax >= 0.1f && 0.5f > comp.CompRefuelable.FuelPercentOfMax)).ToList();
            }
        }

        public override string GetLabel()
        {
            return "BM.PowerArmorsLowFuel".Translate();
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
                        var compRefuelable = comp.CompRefuelable;
                        if (compRefuelable != null && compRefuelable.FuelPercentOfMax >= 0.1f && 0.5f > compRefuelable.FuelPercentOfMax)
                        {
                            list.AppendLine("  - " + pawn.NameShortColored.Resolve() + " (" + compRefuelable.FuelPercentOfMax.ToStringPercent() + ")");
                        }
                    }
                }
            }
            return "BM.PowerArmorsLowFuelDesc".Translate(list.ToString().TrimEndNewlines());
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(Pawns);
        }
    }
}
