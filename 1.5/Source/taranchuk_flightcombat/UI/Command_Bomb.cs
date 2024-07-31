using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace taranchuk_flightcombat
{
    [StaticConstructorOnStartup]
    public class Command_Bomb : Command_Action
    {
        public List<FloatMenuOption> bombOptions;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions => bombOptions;

        private static readonly Texture2D cooldownBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(Color.grey.r, Color.grey.g, Color.grey.b, 0.6f));
        private int lastUsedTick;
        private int cooldownTicks;
        public Command_Bomb(int lastUsedTick, int cooldownTicks)
        {
            this.lastUsedTick = lastUsedTick;
            this.cooldownTicks = cooldownTicks;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
            if (this.lastUsedTick > 0)
            {
                var cooldownTicksRemaining = Find.TickManager.TicksGame - this.lastUsedTick;
                if (cooldownTicksRemaining < this.cooldownTicks)
                {
                    float num = Mathf.InverseLerp(this.cooldownTicks, 0, cooldownTicksRemaining);
                    Widgets.FillableBar(rect, Mathf.Clamp01(num), cooldownBarTex, null, doBorder: false);
                }
            }
            if (result.State == GizmoState.Interacted)
            {
                return result;
            }
            return new GizmoResult(result.State);
        }
    }
}
