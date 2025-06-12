using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace taranchuk_combatgraphics
{
    public class taranchuk_combatgraphicsMod : Mod
    {
        public taranchuk_combatgraphicsMod(ModContentPack pack) : base(pack)
        {
            new Harmony("taranchuk_combatgraphicsMod").PatchAll();
        }
    }

    public class PawnGraphicExtension : DefModExtension
    {
        public GraphicData combatGraphic;
    }

    [HarmonyPatch(typeof(PawnRenderNode_AnimalPart), "GraphicFor")]
    public static class PawnRenderNode_AnimalPart_GraphicFor_Patch
    {
        public static void Postfix(Pawn pawn, ref Graphic __result)
        {
            var extension = pawn.def.GetModExtension<PawnGraphicExtension>();
            if (extension != null && __result != null)
            {
                Stance_Busy stance_Busy = pawn.stances?.curStance as Stance_Busy;
                if (stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid 
                    || PawnRenderUtility.CarryWeaponOpenly(pawn))
                {
                    __result = extension.combatGraphic.Graphic;
                }
            }
        }

        public static void TryRefreshGraphic(this Pawn pawn)
        {
            var extension = pawn.def.GetModExtension<PawnGraphicExtension>();
            if (extension != null)
            {
                pawn.drawer.renderer.SetAllGraphicsDirty();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController), "Drafted", MethodType.Setter)]
    public static class Pawn_DraftController_Drafted_Patch
    {
        public static void Postfix(Pawn_DraftController __instance)
        {
            __instance.pawn.TryRefreshGraphic();
        }
    }

    [HarmonyPatch(typeof(Pawn_StanceTracker), "SetStance")]
    public static class Pawn_StanceTracker_SetStance_Patch
    {
        public static void Postfix(Pawn_StanceTracker __instance)
        {
            __instance.pawn.TryRefreshGraphic();
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Pawn_JobTracker_StartJob_Patch
    {
        public static void Postfix(Pawn_JobTracker __instance)
        {
            __instance.pawn.TryRefreshGraphic();
        }
    }
}