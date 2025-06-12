using RimWorld;
using UnityEngine;

namespace Taranchuk_ColorPicker
{
    public class ApparelColorable : Apparel
    {
        public CompCustomColorPicker _comp;
        public CompCustomColorPicker Comp => _comp ??= GetComp<CompCustomColorPicker>();

        public override Color DrawColor
        {
            get
            {
                var comp = Comp;
                if (comp.colorOne.HasValue)
                {
                    return comp.colorOne.Value;
                }
                return base.DrawColor;
            }
        }


        public override Color DrawColorTwo
        {
            get
            {
                var comp = Comp;
                if (comp.colorTwo.HasValue)
                {
                    return comp.colorTwo.Value;
                }
                return base.DrawColorTwo;
            }
        }
    }
}
