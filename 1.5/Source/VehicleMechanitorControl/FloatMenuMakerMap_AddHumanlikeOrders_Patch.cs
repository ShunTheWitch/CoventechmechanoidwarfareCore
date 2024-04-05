using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    public static class FloatMenuMakerMap_AddHumanlikeOrders_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            List<Thing> thingList = c.GetThingList(pawn.Map);
            foreach (Thing thing in thingList)
            {
                var comp = thing.TryGetComp<CompMechanitorControl>();
                if (comp != null)
                {
                    opts.RemoveAll(x => x.Label == "DisassembleMech".Translate(thing.LabelCap)
                    || x.Label == "CannotDisassembleMech".Translate(thing.LabelCap)
                    + ": " + "MustBeOverseer".Translate().CapitalizeFirst());
                }
            }
        }
    }
}
