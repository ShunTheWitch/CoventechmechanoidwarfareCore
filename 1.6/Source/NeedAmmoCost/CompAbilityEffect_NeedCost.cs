using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NeedAmmoCost
{
    public class CompProperties_AbilityNeedCost : CompProperties_AbilityEffect
    {
        public List<NeedCost> needCosts;

        public string failReason;

        public CompProperties_AbilityNeedCost()
        {
            compClass = typeof(CompAbilityEffect_NeedCost);
        }
    }

    public class CompAbilityEffect_NeedCost : CompAbilityEffect
    {
        public new CompProperties_AbilityNeedCost Props => (CompProperties_AbilityNeedCost)props;

        private bool HasEnoughNeed
        {
            get
            {
                var pawn = parent.pawn;
                foreach (var need in pawn.needs.AllNeeds)
                {
                    foreach (var needCost in Props.needCosts)
                    {
                        if (needCost.need == need.def && need.CurLevel >= needCost.cost)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var pawn = parent.pawn;
            foreach (var need in pawn.needs.AllNeeds)
            {
                foreach (var needCost in Props.needCosts)
                {
                    if (need.def == needCost.need && need.CurLevel >= needCost.cost)
                    {
                        need.CurLevel -= needCost.cost;
                        return;
                    }
                }
            }
        }

        public override bool GizmoDisabled(out string reason)
        {
            if (HasEnoughNeed is false)
            {
                reason = Props.failReason;
                return true;
            }
            reason = "";
            return false;
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return HasEnoughNeed;
        }
    }
}
