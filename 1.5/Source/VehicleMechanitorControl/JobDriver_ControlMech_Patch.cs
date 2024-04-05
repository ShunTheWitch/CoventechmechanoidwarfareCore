using HarmonyLib;
using RimWorld;
using System.Linq;
using System.Reflection;

namespace VehicleMechanitorControl
{
    [HarmonyPatch]
    public static class JobDriver_ControlMech_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase GetMethod()
        {
            return typeof(JobDriver_ControlMech).GetMethods(AccessTools.all).Where(x => x.Name.Contains("<MakeNewToils>")).Last();
        }

        public static void Postfix(JobDriver_ControlMech __instance)
        {
            var comp = __instance.Mech.GetComp<CompMechanitorControl>();
            if (comp != null && comp.Props.bandwidthGain > 0)
            {
                if (!__instance.pawn.health.hediffSet.HasHediff(CVN_DefOf.BandNode))
                {
                    __instance.pawn.health.AddHediff(CVN_DefOf.BandNode, __instance.pawn.health.hediffSet.GetBrain());
                }
            }
        }
    }
}
