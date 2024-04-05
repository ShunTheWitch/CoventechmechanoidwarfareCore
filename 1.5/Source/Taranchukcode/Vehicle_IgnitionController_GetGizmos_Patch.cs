using HarmonyLib;
using System.Collections.Generic;
using Vehicles;
using Verse;

namespace VehicleMechanitorControl
{
    [HarmonyPatch(typeof(Vehicle_IgnitionController), "GetGizmos")]
    public static class Vehicle_IgnitionController_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Vehicle_IgnitionController __instance)
        {
            foreach (var r in __result)
            {
                yield return r;
                if (r is Command_Toggle toggle && toggle.defaultLabel == __instance.DraftGizmoLabel)
                {
                    var comp = __instance.vehicle.GetComp<CompMechanitorControl>();
                    if (comp != null)
                    {
                        var overseer = __instance.vehicle.GetOverseer();
                        if (overseer is null)
                        {
                            r.Disable("CVN_UncontrolledByMechanitor".Translate());
                        }
                    }
                }
            }
        }
    }
}
