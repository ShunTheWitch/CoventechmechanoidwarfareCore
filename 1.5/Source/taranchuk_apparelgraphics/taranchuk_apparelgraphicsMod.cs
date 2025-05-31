using HarmonyLib;
using RimWorld;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Verse;

namespace taranchuk_apparelgraphics
{
    public class taranchuk_apparelgraphicsMod : Mod
    {
        public taranchuk_apparelgraphicsMod(ModContentPack pack) : base(pack)
        {
            new Harmony("taranchuk_apparelgraphicsMod").PatchAll();
        }
    }

    public class ApparelExtension : DefModExtension
    {
        public bool hideBody;
        public bool hideOtherApparels;
        public BodyTypeDef femaleBody;
        public BodyTypeDef maleBody;
    }

    [StaticConstructorOnStartup]
    public static class Utils
    {
        private static ConcurrentDictionary<ThingDef, ApparelExtension> cachedExtensions = new ConcurrentDictionary<ThingDef, ApparelExtension>();
        public static bool ShouldHideBody(this ThingDef def)
        {
            if (!cachedExtensions.TryGetValue(def, out var extension))
            {
                cachedExtensions[def] = extension = def.GetModExtension<ApparelExtension>();
            }
            if (extension != null && extension.hideBody)
            {
                return true;
            }
            return false;
        }

        public static bool ShouldHideApparel(this ThingDef def)
        {
            if (!cachedExtensions.TryGetValue(def, out var extension))
            {
                cachedExtensions[def] = extension = def.GetModExtension<ApparelExtension>();
            }
            if (extension != null && extension.hideOtherApparels)
            {
                return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnRenderNodeWorker), "AppendDrawRequests")]
    public static class PawnRenderNodeWorker_AppendDrawRequests_Patch
    {
        public static bool Prefix(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            bool nodeIsApparel = node is PawnRenderNode_Apparel;
            if (parms.pawn.apparel?.AnyApparel ?? false)
            {
                if (node is PawnRenderNode_Body || node.parent is PawnRenderNode_Body)
                {
                    foreach (var apparel in parms.pawn.apparel.WornApparel)
                    {
                        if (apparel.def.ShouldHideBody())
                        {
                            requests.Add(new PawnGraphicDrawRequest(node));
                            return false;
                        }
                    }
                }

                if (nodeIsApparel)
                {
                    bool shouldHideApparel = false;
                    foreach (var apparel in parms.pawn.apparel.WornApparel)
                    {
                        if (apparel.def.ShouldHideApparel())
                        {
                            shouldHideApparel = true;
                            break;
                        }
                    }
                    if (shouldHideApparel)
                    {
                        if (node.apparel.def.ShouldHideApparel() is false)
                        {
                            requests.Add(new PawnGraphicDrawRequest(node));
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PawnRenderNode_Body), "GraphicFor")]
    public static class PawnRenderNode_Body_GraphicFor_Patch
    {
        public static void Prefix(PawnRenderNode_Body __instance, Pawn pawn, out BodyTypeDef __state)
        {
            __state = pawn.story.bodyType;
            pawn.TryOverrideBody(ref pawn.story.bodyType);
        }

        public static void Postfix(PawnRenderNode_Body __instance, Pawn pawn, BodyTypeDef __state)
        {
            pawn.story.bodyType = __state;
        }
    }

    [HarmonyPatch(typeof(ApparelGraphicRecordGetter), "TryGetGraphicApparel")]
    public static class ApparelGraphicRecordGetter_TryGetGraphicApparel_Patch
    {
        public static void Prefix(Apparel apparel, ref BodyTypeDef bodyType)
        {
            var pawn = apparel.Wearer;
            if (pawn != null)
            {
                pawn.TryOverrideBody(ref bodyType);
            }
        }

        public static void TryOverrideBody(this Pawn pawn, ref BodyTypeDef bodyType)
        {
            var shell = pawn.apparel.WornApparel.Find(x => x.def.apparel.layers.Contains(ApparelLayerDefOf.Shell));
            foreach (var apparel2 in pawn.apparel.WornApparel)
            {
                if (shell != apparel2)
                {
                    SetBodyType(pawn, ref bodyType, apparel2);
                }
            }
            SetBodyType(pawn, ref bodyType, shell);
        }

        private static void SetBodyType(Pawn pawn, ref BodyTypeDef bodyType, Apparel apparel2)
        {
            var extension = apparel2?.def?.GetModExtension<ApparelExtension>();
            if (extension != null)
            {
                //flango's fix for genderless pawns
                if (extension.maleBody != null)
                {
                    if (pawn.gender != Gender.Female) // Male or None
                    {
                        bodyType = extension.maleBody;
                    }
                }
                if (extension.femaleBody != null)
                {
                    if (pawn.gender != Gender.Male) // Female or None
                    {
                        bodyType = extension.femaleBody;
                    }
                }
            }
        }
    }
}