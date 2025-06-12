using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Taranchuk_ColorPicker
{
    [HotSwappable]
    public class CompCustomColorPicker : ThingComp
    {
        public Color? colorOne;
        public Color? colorTwo;
        public CompProperties_ColorPicker Props => base.props as CompProperties_ColorPicker;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var gizmo = GetGizmo();
            if (gizmo != null)
            {
                yield return gizmo;
            }
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            var gizmo = GetGizmo();
            if (gizmo != null)
            {
                yield return gizmo;
            }
        }

        [HarmonyPatch(typeof(Pawn_EquipmentTracker), "GetGizmos")]
        public static class Pawn_EquipmentTracker_GetGizmos_Patch
        {
            public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_EquipmentTracker __instance)
            {
                foreach (var r in __result)
                {
                    yield return r;
                }
                foreach (var eq in __instance.AllEquipmentListForReading)
                {
                    var comp = eq.GetComp<CompCustomColorPicker>();
                    if (comp != null)
                    {
                        var gizmo = comp.GetGizmo();
                        if (gizmo != null)
                        {
                            yield return gizmo;
                        }
                    }
                }
            }
        }

        private Command_Action GetGizmo()
        {
            if (parent is not Pawn || parent.Faction == Faction.OfPlayer)
            {
                return new Command_Action
                {
                    defaultLabel = Props.label,
                    defaultDesc = Props.description,
                    icon = ContentFinder<Texture2D>.Get(Props.iconPath),
                    action = () =>
                    {
                        Find.WindowStack.Add(new Window_ColorPicker(this));
                    }
                };
            }
            return null;
        }

        public void ApplyColors()
        {
            parent.Notify_ColorChanged();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            ApplyColors();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref colorOne, GetType() + "_colorOne");
            Scribe_Values.Look(ref colorTwo, GetType() + "_colorTwo");
        }
    }
}
