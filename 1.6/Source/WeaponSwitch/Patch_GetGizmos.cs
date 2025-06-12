using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace WeaponSwitch
{
	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public static class Patch_GetGizmos
	{
		public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.IsColonistPlayerControlled)
			{
				CompSwitchWeapon compSwitchWeapon = __instance.equipment?.Primary.TryGetComp<CompSwitchWeapon>();
				if (compSwitchWeapon != null)
				{
					List<Gizmo> list = __result.ToList();
					list.AddRange(compSwitchWeapon.SwitchWeaponOptions());
					__result = list;
				}
			}
		}
	}
}
