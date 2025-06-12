using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BM_PowerArmor
{
    public class CompAssignableToPawn_PowerArmorSpot : CompAssignableToPawn
    {
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

        public override string GetAssignmentGizmoDesc()
        {
            return "BM.CommandPowerArmorSpotSetOwnerDesc".Translate();
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
            return pawn.GetPowerArmorParkingSpot() != null;
        }

        public override void TryAssignPawn(Pawn pawn)
        {
            var newSpot = parent as Building;
            if (newSpot.GetAssignedPawn() == pawn)
            {
                return;
            }
            var oldSpot = pawn.GetPowerArmorParkingSpot();
            if (oldSpot != null)
            {
                oldSpot.TryGetComp<CompAssignableToPawn>().ForceRemovePawn(pawn);
            }
            if (newSpot.GetAssignedPawn() != null)
            {
                TryUnassignPawn(newSpot.GetAssignedPawn());
            }
            newSpot.TryGetComp<CompAssignableToPawn>().ForceAddPawn(pawn);
            pawn.SetPowerArmorParkingSpot(newSpot);
        }

        public override void TryUnassignPawn(Pawn pawn, bool sort = true, bool uninstall = false)
        {
            var newSpot = parent as Building;
            newSpot.TryGetComp<CompAssignableToPawn>().ForceRemovePawn(pawn);
            pawn.SetPowerArmorParkingSpot(null);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit && assignedPawns.RemoveAll((Pawn x) => x.GetPowerArmorParkingSpot() != parent) > 0)
            {
                Log.Warning(parent.ToStringSafe() + " had pawns assigned that don't have it as an assigned power armor spot. Removing.");
            }
        }
    }
}
