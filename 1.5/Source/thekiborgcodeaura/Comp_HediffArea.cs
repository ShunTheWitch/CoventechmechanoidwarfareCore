namespace Thek_HediffArea
{
    public class Comp_HediffArea : ThingComp
    {
        public CompProperties_HediffArea Props => (CompProperties_HediffArea)props;


        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            GenDraw.DrawRadiusRing(parent.Position, Props.areaRange);
        }


        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(Props.refreshRateInTicks))
            {
                base.CompTick();
                DoHediffArea();
            }
        }


        private void DoHediffArea()
        {
            foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned)
            {
                bool findsAnySuitablePawn = pawn != null && pawn.Position.DistanceToSquared(parent.Position) < Props.areaRange * Props.areaRange;
                if (findsAnySuitablePawn)
                {
                    continue;
                }
                if (Props.mechanoidExclusive == true)
                {
                    AddHediffToMechanoid(pawn);
                    continue;
                }
                else if (Props.mechanoidExclusive == false)
                {
                    AddHediffToHumanlike(pawn);
                }

            }
        }


        private void AddHediffToMechanoid(Pawn pawn)
        {
            if (Props.appliesToFriendlies == true && FactionUtility.HostileTo(pawn.Faction, parent.Faction))
            {
                Hediff hediff = HediffMaker.MakeHediff(Props.hediffToApplyMechanoids, pawn);
                pawn.health.AddHediff(hediff);
                return;
            }
            else if (Props.appliesToHostiles == true && FactionUtility.HostileTo(pawn.Faction, parent.Faction))
            {
                Hediff hediff = HediffMaker.MakeHediff(Props.hediffToApplyMechanoids, pawn);
                pawn.health.AddHediff(hediff);
                return;
            }
        }


        private void AddHediffToHumanlike(Pawn pawn)
        {
            if (Props.appliesToFriendlies == true && !FactionUtility.HostileTo(pawn.Faction, parent.Faction) && pawn.RaceProps.IsFlesh)
            {
                HealthUtility.AdjustSeverity(pawn, Props.hediffToApplyFleshlings, 0.2f);
            }
            else if (Props.appliesToHostiles == true && FactionUtility.HostileTo(pawn.Faction, parent.Faction) && pawn.RaceProps.IsFlesh)
            {
                HealthUtility.AdjustSeverity(pawn, Props.hediffToApplyFleshlings, 0.2f);
            }
        }
    }
}