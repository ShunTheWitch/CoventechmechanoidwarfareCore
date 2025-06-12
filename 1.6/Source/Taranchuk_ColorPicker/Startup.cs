using RimWorld;
using Verse;

namespace Taranchuk_ColorPicker
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.race != null && def.IsCorpse is false && def.race.IsMechanoid)
                {
                    var comp = def.GetCompProperties<CompProperties_ColorPicker>();
                    if (comp is null)
                    {
                        def.comps.Add(new CompProperties_ColorPicker
                        {
                            label = "ColorPicker.MechPersonalizedPaint".Translate(),
                            description = "ColorPicker.MechPersonalizedPaintDesc".Translate(),
                            includeColorTwo = false,
                            iconPath = "UI/PaintIcon_militor",
                        });
                    }
                }
            }
        }
    }
}
