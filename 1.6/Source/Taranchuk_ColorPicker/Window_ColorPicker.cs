using RimWorld;
using System;
using System.Globalization;
using UnityEngine;
using Verse;

namespace Taranchuk_ColorPicker
{
    [HotSwappable]
    public class Window_ColorPicker : Window
    {
        private Color colorOne;

        private Color oldColorOne;

        private Color colorTwo;

        private Color oldColorTwo;

        private bool colorTwoChosen;

        private CompCustomColorPicker comp;

        private bool hsvColorWheelDragging;

        private bool colorTemperatureDragging;

        private string[] textfieldBuffers = new string[6];

        private Color textfieldColorBuffer;

        private string previousFocusedControlName;

        public static Widgets.ColorComponents visibleColorTextfields = Widgets.ColorComponents.Hue | Widgets.ColorComponents.Sat;

        public static Widgets.ColorComponents editableColorTextfields = Widgets.ColorComponents.Hue | Widgets.ColorComponents.Sat;
        public override Vector2 InitialSize => new Vector2(600f, 410f);

        public Window_ColorPicker(CompCustomColorPicker comp)
        {
            this.doCloseX = true;
            colorOne = comp.colorOne ?? Color.white;
            oldColorOne = colorOne;
            colorTwo = comp.colorTwo ?? Color.white;
            oldColorTwo = colorTwo;
            this.comp = comp;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            closeOnAccept = false;
        }

        private static void HeaderRow(ref RectDivider layout)
        {
            using (new TextBlock(GameFont.Medium))
            {
                TaggedString taggedString = "ChooseAColor".Translate().CapitalizeFirst();
                RectDivider rectDivider = layout.NewRow(Text.CalcHeight(taggedString, layout.Rect.width));
                GUI.SetNextControlName(Dialog_GlowerColorPicker.focusableControlNames[0]);
                Widgets.Label(rectDivider, taggedString);
            }
        }

        private void BottomButtons(ref RectDivider layout)
        {
            RectDivider rectDivider = layout.NewRow(Dialog_GlowerColorPicker.ButSize.y, VerticalJustification.Bottom);
            if (Widgets.ButtonText(rectDivider.NewCol(Dialog_GlowerColorPicker.ButSize.x), "Cancel".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(rectDivider.NewCol(Dialog_GlowerColorPicker.ButSize.x, HorizontalJustification.Right), "Accept".Translate()))
            {
                if (colorOne != oldColorOne)
                {
                    comp.colorOne = colorOne;
                }
                if (colorTwo != oldColorTwo)
                {
                    comp.colorTwo = colorTwo;
                }
                Close();
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            base.Close(doCloseSound);
            comp.ApplyColors();
        }

        private void ColorTextfields(ref RectDivider layout, ref Color color, out Vector2 size)
        {
            RectAggregator aggregator = new RectAggregator(new Rect(layout.Rect.position, new Vector2(125f, 0f)), 195906069);
            bool num = Widgets.ColorTextfields(ref aggregator, ref color, ref textfieldBuffers, ref textfieldColorBuffer, previousFocusedControlName, "colorTextfields", editableColorTextfields, visibleColorTextfields);
            size = aggregator.Rect.size;
            if (num)
            {
                Color.RGBToHSV(color, out var H, out var S, out var _);
                color = Color.HSVToRGB(H, S, 1f);
            }

            var hexRect = new Rect(aggregator.Rect.x, aggregator.Rect.yMax + 4, 125, 32);
            if (Widgets.ButtonText(hexRect, "ColorPicker.PasteHex".Translate()))
            {
                if (TryGetColorFromHex(GUIUtility.systemCopyBuffer, out var tempColor))
                {
                    color = tempColor;
                }
            }
        }

        public static bool TryGetColorFromHex(string hex, out Color color)
        {
            color = Color.white;
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            if (hex.Length != 6 && hex.Length != 8)
            {
                return false;
            }

            int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            int a = 255;
            if (hex.Length == 8)
            {
                a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
            }

            color = GenColor.FromBytes(r, g, b, a);
            return true;
        }

        private static void ColorReadback(Rect rect, Color color, Color oldColor)
        {
            rect.SplitVertically((rect.width - 26f) / 2f, out var left, out var right);
            RectDivider rectDivider = new RectDivider(left, 195906069);
            TaggedString label = "CurrentColor".Translate().CapitalizeFirst();
            TaggedString label2 = "OldColor".Translate().CapitalizeFirst();
            float width = Mathf.Max(100f, label.GetWidthCached(), label2.GetWidthCached());
            RectDivider rectDivider2 = rectDivider.NewRow(Text.LineHeight);
            Widgets.Label(rectDivider2.NewCol(width), label);
            Widgets.DrawBoxSolid(rectDivider2, color);
            RectDivider rectDivider3 = rectDivider.NewRow(Text.LineHeight);
            Widgets.Label(rectDivider3.NewCol(width), label2);
            Widgets.DrawBoxSolid(rectDivider3, oldColor);
            RectDivider rectDivider4 = new RectDivider(right, 195906069);
            rectDivider4.NewCol(26f);
            if (DarklightUtility.IsDarklight(color))
            {
                Widgets.Label(rectDivider4, "Darklight".Translate().CapitalizeFirst());
            }
            else
            {
                Widgets.Label(rectDivider4, "NotDarklight".Translate().CapitalizeFirst());
            }
        }

        private static void TabControl()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab)
            {
                bool num = !Event.current.shift;
                Event.current.Use();
                string text = GUI.GetNameOfFocusedControl();
                if (text.NullOrEmpty())
                {
                    text = Dialog_GlowerColorPicker.focusableControlNames[0];
                }
                int num2 = Dialog_GlowerColorPicker.focusableControlNames.IndexOf(text);
                if (num2 < 0)
                {
                    num2 = Dialog_GlowerColorPicker.focusableControlNames.Count;
                }
                num2 = ((!num) ? (num2 - 1) : (num2 + 1));
                if (num2 >= Dialog_GlowerColorPicker.focusableControlNames.Count)
                {
                    num2 = 0;
                }
                else if (num2 < 0)
                {
                    num2 = Dialog_GlowerColorPicker.focusableControlNames.Count - 1;
                }
                GUI.FocusControl(Dialog_GlowerColorPicker.focusableControlNames[num2]);
            }
        }
        private static readonly Vector3 PortraitOffset = new Vector3(0f, 0f, 0.15f);

        public override void DoWindowContents(Rect inRect)
        {
            using (TextBlock.Default())
            {
                var layoutRect = new Rect(inRect.x, inRect.y, inRect.width, 240);
                RectDivider layout = new RectDivider(layoutRect, 195906069);
                HeaderRow(ref layout);
                layout.NewRow(0f);
                var color = colorTwoChosen is false ? colorOne : colorTwo;
                Color.RGBToHSV(color, out var H, out var S, out var _);
                Color defaultColor = Color.HSVToRGB(H, S, 1f);
                defaultColor.a = 1f;
                ColorPalette(ref layout, ref color, defaultColor, false, out var paletteHeight);
                ColorTextfields(ref layout, ref color, out var size);
                float height = Mathf.Max(paletteHeight, 128f, size.y);
                RectDivider rectDivider = layout.NewRow(height);
                rectDivider.NewCol(size.x);
                rectDivider.NewCol(250f, HorizontalJustification.Right);
                Widgets.HSVColorWheel(rectDivider.Rect.ContractedBy((rectDivider.Rect.width - 128f) / 2f, (rectDivider.Rect.height - 128f) / 2f), ref color, ref hsvColorWheelDragging, 1f);
                layout = new RectDivider(new Rect(inRect.x, layoutRect.yMax, inRect.width, inRect.height - layoutRect.height), 65436135);
                BottomButtons(ref layout);
                layout.NewRow(0f, VerticalJustification.Bottom);
                Widgets.ColorTemperatureBar(layout.NewRow(34f), ref color, ref colorTemperatureDragging, 1f);
                //layout.NewRow(26f);
                if (colorTwoChosen is false)
                {
                    ColorReadback(layout, colorOne, oldColorOne);
                }
                else
                {
                    ColorReadback(layout, colorTwo, oldColorTwo);
                }

                if (colorTwoChosen is false)
                {
                    colorOne = color;
                }
                else
                {
                    colorTwo = color;
                }

                if (comp.Props.includeColorTwo)
                {
                    var buttonsRect = new Rect(layoutRect.x, layoutRect.yMax - 24, 150, 24);
                    Widgets.Label(buttonsRect, "ColorPicker.ColorChannel".Translate(colorTwoChosen is false ? "ColorPicker.ColorA".Translate() : "ColorPicker.ColorB".Translate()));
                    if (Widgets.RadioButton(new Vector2(buttonsRect.xMax, buttonsRect.y), colorTwoChosen == false))
                    {
                        colorTwoChosen = false;
                    }
                    if (Widgets.RadioButton(new Vector2(buttonsRect.xMax + 40, buttonsRect.y), colorTwoChosen == true))
                    {
                        colorTwoChosen = true;
                    }
                }
                TabControl();
                if (Event.current.type == EventType.Layout)
                {
                    previousFocusedControlName = GUI.GetNameOfFocusedControl();
                }
            }
        }

        private void ColorPalette(ref RectDivider layout, ref Color color, Color defaultColor, bool showDarklight, out float paletteHeight)
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                RectDivider rectDivider = layout;
                RectDivider rectDivider2 = rectDivider.NewCol(250f, HorizontalJustification.Right);
                int num = 26;
                RectDivider rectDivider3 = rectDivider2.NewRow(num);
                int num2 = 4;
                rectDivider3.Rect.SplitVertically(num2 * (num + 2), out var left, out var right);
                RectDivider rectDivider4 = new RectDivider(left, 195906069, new Vector2(10f, 2f));
                Widgets.ColorBox(rectDivider4.NewCol(num), ref color, defaultColor);
                Widgets.Label(rectDivider4, "Default".Translate().CapitalizeFirst());
                RectDivider rectDivider5 = new RectDivider(right, 195906069, new Vector2(10f, 2f));
                Color defaultDarklight = DarklightUtility.DefaultDarklight;
                Rect rect = rectDivider5.NewCol(num);
                if (showDarklight)
                {
                    Widgets.ColorBox(rect, ref color, defaultDarklight);
                    Widgets.Label(rectDivider5, "Darklight".Translate().CapitalizeFirst());
                }
                Widgets.ColorSelector(rectDivider2, ref color, Dialog_GlowerColorPicker.colors, out paletteHeight);
                paletteHeight += num + 2;
            }
        }

    }
}
