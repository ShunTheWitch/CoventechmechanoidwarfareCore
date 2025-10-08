using RimWorld;
using Verse;

namespace VehicleMechanitorControl
{
    public class CompProperties_DisassembleMech : CompProperties_AbilityEffect
    {
        public CompProperties_DisassembleMech()
        {
            this.compClass = typeof(CompDisassembleMech);
        }
    }
    public class CompDisassembleMech : CompMechanitorAbility
    {
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            var mech = target.Pawn;
            return mech.GetOverseer() == parent.pawn;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            var mech = target.Pawn;

            var costList = MechanitorUtility.IngredientsFromDisassembly(mech.def);
            if (costList.Empty())
            {
                costList = mech.CostListAdjusted();
            }
            foreach (ThingDefCountClass item in costList)
            {
                Thing thing = ThingMaker.MakeThing(item.thingDef);
                thing.stackCount = item.count;
                GenPlace.TryPlaceThing(thing, mech.Position, mech.Map, ThingPlaceMode.Near);
            }
            mech.forceNoDeathNotification = true;
            mech.Kill(null, null);
            mech.forceNoDeathNotification = false;
            mech.Corpse?.Destroy();
        }
    }
}
