using Verse;

namespace WeaponSwitch
{
    public static class CECompat
	{
		public static void TransferAmmo(Thing source, Thing dest)
		{
			var comp = source.TryGetComp<CombatExtended.CompAmmoUser>();
			if (comp != null)
			{
				var destComp = dest.TryGetComp<CombatExtended.CompAmmoUser>();
				if (destComp != null)
				{
					destComp.SelectedAmmo = comp.SelectedAmmo;
					destComp.CurrentAmmo = comp.CurrentAmmo;
					destComp.CurMagCount = comp.CurMagCount;

                    comp.SelectedAmmo = null;
                    comp.CurrentAmmo = null;
                    comp.CurMagCount = 0;
				}
			}
		}
	}
}
