using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BM_PowerArmor
{
    public class CompAssignableToPawn_PowerArmor : CompAssignableToPawn
    {

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
        }

        public override IEnumerable<Pawn> AssigningCandidates
        {
            get
            {
                if (!parent.Spawned)
                {
                    return Enumerable.Empty<Pawn>();
                }
                return parent.Map.mapPawns.FreeColonists.OrderByDescending((Pawn p) => CanAssignTo(p).Accepted);
            }
        }

        public override AcceptanceReport CanAssignTo(Pawn pawn)
        {
            var comp = parent.GetComp<CompPowerArmor>();
            if (comp.Props.genderRestriction.HasValue && pawn.gender != comp.Props.genderRestriction)
            {
                return "BM.ForOnly".Translate(comp.GetGenderOnlyLabel());
            }
            return base.CanAssignTo(pawn);
        }

        public override string GetAssignmentGizmoDesc()
        {
            return "BM.CommandPowerArmorSetOwnerDesc".Translate();
        }

        public override string CompInspectStringExtra()
        {
            if (base.AssignedPawnsForReading.Count == 0)
            {
                return "Owner".Translate() + ": " + "Nobody".Translate();
            }
            if (base.AssignedPawnsForReading.Count == 1)
            {
                return "Owner".Translate() + ": " + base.AssignedPawnsForReading[0].Label;
            }
            return "";
        }

        public override bool AssignedAnything(Pawn pawn)
        {
            return pawn.GetPowerArmor() != null;
        }

        public override void TryAssignPawn(Pawn pawn)
        {
            var newPowerArmor = parent as Building;
            if (newPowerArmor.GetAssignedPawn() == pawn)
            {
                return;
            }
            var oldPowerArmor = pawn.GetPowerArmor();
            if (oldPowerArmor != null)
            {
                oldPowerArmor.TryGetComp<CompAssignableToPawn>().ForceRemovePawn(pawn);
            }
            if (newPowerArmor.GetAssignedPawn() != null)
            {
                TryUnassignPawn(newPowerArmor.GetAssignedPawn());
            }
            newPowerArmor.TryGetComp<CompAssignableToPawn>().ForceAddPawn(pawn);
            pawn.SetPowerArmor(newPowerArmor);
        }

        public override void TryUnassignPawn(Pawn pawn, bool sort = true, bool uninstall = false)
        {
            var newSpot = parent as Building;
            newSpot.TryGetComp<CompAssignableToPawn>().ForceRemovePawn(pawn);
            pawn.SetPowerArmor(null);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit && assignedPawns.RemoveAll((Pawn x) => x.GetPowerArmor() != parent) > 0)
            {
                Log.Warning(parent.ToStringSafe() + " had pawns assigned that don't have it as an assigned power armor. Removing.");
            }
        }
    }
}
