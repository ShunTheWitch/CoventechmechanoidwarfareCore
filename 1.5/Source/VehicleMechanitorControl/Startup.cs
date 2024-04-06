using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.Sound;

namespace VehicleMechanitorControl
{
    /*
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            foreach (var kind in DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race is VehicleDef))
            {
                kind.controlGroupPortraitZoom = 0.5f;
                Log.Message(kind + " - " +  kind.controlGroupPortraitZoom);
            }
        }
    }

    //[HarmonyPatch(typeof(MechanitorControlGroupGizmo), "GizmoOnGUI")]
    public static class test
    {
        public static bool Prefix(GizmoResult __result, MechanitorControlGroupGizmo __instance, Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            __result = GizmoOnGUI(__instance, topLeft, maxWidth, parms);
            return false;
        }

        public static GizmoResult GizmoOnGUI(MechanitorControlGroupGizmo __instance, Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (!ModLister.CheckBiotech("Mechanitor control group gizmo"))
            {
                return new GizmoResult(GizmoState.Clear);
            }
            AcceptanceReport canControlMechs = __instance.controlGroup.Tracker.CanControlMechs;
            __instance.disabled = !canControlMechs;
            __instance.disabledReason = canControlMechs.Reason;
            Rect rect = new Rect(topLeft.x, topLeft.y, __instance.GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(6f);
            bool flag = Mouse.IsOver(rect2);
            List<Pawn> mechsForReading = __instance.controlGroup.MechsForReading;
            Color white = Color.white;
            Material material = ((__instance.disabled || parms.lowLight || mechsForReading.Count <= 0) ? TexUI.GrayscaleGUI : null);
            GUI.color = (parms.lowLight ? Command.LowLightBgColor : white);
            GenUI.DrawTextureWithMaterial(rect, parms.shrunk ? Command.BGTexShrunk : Command.BGTex, material);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect rect3 = rect2;
            TaggedString str = ((!__instance.mergedControlGroups.NullOrEmpty()) ? "Groups".Translate() : "Group".Translate());
            str += " " + __instance.controlGroup.Index;
            if (!__instance.mergedControlGroups.NullOrEmpty())
            {
                __instance.mergedControlGroups.SortBy((MechanitorControlGroup c) => c.Index);
                for (int i = 0; i < __instance.mergedControlGroups.Count; i++)
                {
                    str += ", " + __instance.mergedControlGroups[i].Index;
                }
            }
            str = str.Truncate(rect2.width);
            Vector2 vector = Text.CalcSize(str);
            rect3.width = vector.x;
            rect3.height = vector.y;
            Widgets.Label(rect3, str);
            if (mechsForReading.Count <= 0)
            {
                GUI.color = ColoredText.SubtleGrayColor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect2, "(" + "NoMechs".Translate() + ")");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return new GizmoResult(GizmoState.Clear);
            }
            if (Mouse.IsOver(rect3))
            {
                Widgets.DrawHighlight(rect3);
                if (Widgets.ButtonInvisible(rect3))
                {
                    Find.Selector.ClearSelection();
                    for (int j = 0; j < mechsForReading.Count; j++)
                    {
                        Find.Selector.Select(mechsForReading[j]);
                    }
                }
            }
            bool flag2 = false;
            Rect rect4 = new Rect(rect.x + rect.width - 26f - 6f, rect.y + 6f, 26f, 26f);
            Widgets.DrawTextureFitted(rect4, MechanitorControlGroupGizmo.PowerIcon.Texture, 1f);
            if (!__instance.disabled && Mouse.IsOver(rect4))
            {
                flag2 = true;
                Widgets.DrawHighlight(rect4);
                if (Widgets.ButtonInvisible(rect4))
                {
                    Find.WindowStack.Add(new Dialog_RechargeSettings(__instance.controlGroup));
                }
            }
            bool flag3 = false;
            Rect rect5 = new Rect(rect.x + rect.width - 52f - 6f, rect.y + 6f, 26f, 26f);
            Widgets.DrawTextureFitted(rect5, __instance.controlGroup.WorkMode.uiIcon, 1f);
            if (!__instance.disabled && Mouse.IsOver(rect5))
            {
                flag3 = true;
                Widgets.DrawHighlight(rect5);
            }
            Rect rect6 = new Rect(rect2.x, rect2.y + 26f + 4f, rect2.width, rect2.height - 26f - 4f);
            float num = rect6.height;
            int num2 = 0;
            int num3 = 0;
            for (float num4 = num; num4 >= 0f; num4 -= 1f)
            {
                num2 = Mathf.FloorToInt(rect6.width / num4);
                num3 = Mathf.FloorToInt(rect6.height / num4);
                if (num2 * num3 >= mechsForReading.Count)
                {
                    num = num4;
                    break;
                }
            }
            float num5 = (rect6.width - (float)num2 * num) / 2f;
            float num6 = (rect6.height - (float)num3 * num) / 2f;
            int num7 = 0;
            for (int k = 0; k < num2; k++)
            {
                for (int l = 0; l < num2; l++)
                {
                    if (num7 >= mechsForReading.Count)
                    {
                        break;
                    }
                    Rect rect7 = new Rect(rect6.x + (float)l * num + num5, rect6.y + (float)k * num + num6, num, num);
                    RenderTexture image = PortraitsCache.Get(mechsForReading[num7], rect7.size, Rot4.South, default(Vector3), mechsForReading[num7].kindDef.controlGroupPortraitZoom);
                    if (!__instance.controlGroup.Tracker.ControlledPawns.Contains(mechsForReading[num7]))
                    {
                        Widgets.DrawRectFast(rect7, MechanitorControlGroupGizmo.UncontrolledMechBackgroundColor);
                    }
                    GUI.DrawTexture(rect7, image);
                    if (Mouse.IsOver(rect7))
                    {
                        Widgets.DrawHighlight(rect7);
                        MouseoverSounds.DoRegion(rect7, SoundDefOf.Mouseover_Command);
                        if (Event.current.type == EventType.MouseDown)
                        {
                            if (Event.current.shift)
                            {
                                Find.Selector.Select(mechsForReading[num7]);
                            }
                            else
                            {
                                CameraJumper.TryJumpAndSelect(mechsForReading[num7]);
                            }
                        }
                        TargetHighlighter.Highlight(mechsForReading[num7], arrow: true, colonistBar: false);
                    }
                    if (Find.Selector.IsSelected(mechsForReading[num7]))
                    {
                        SelectionDrawerUtility.DrawSelectionOverlayOnGUI(mechsForReading[num7], rect7, 0.8f / (float)num2, 20f);
                    }
                    num7++;
                }
                if (num7 >= mechsForReading.Count)
                {
                    break;
                }
            }
            if (flag3 && Event.current.type == EventType.MouseDown)
            {
                return new GizmoResult(GizmoState.OpenedFloatMenu, Event.current);
            }
            return new GizmoResult(flag ? GizmoState.Mouseover : GizmoState.Clear);
        }
    }

    */
}
