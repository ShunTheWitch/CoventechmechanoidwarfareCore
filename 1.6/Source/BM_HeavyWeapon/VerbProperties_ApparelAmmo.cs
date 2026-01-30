using System.Collections.Generic;
using Verse;

namespace BM_HeavyWeapon
{
    public class VerbProperties_ApparelAmmo : VerbProperties
    {
        public int pelletCount = 1;
        public List<ThingDef> apparelList;
        public string missingReason;
        public string noAmmoReason;
    }
}
