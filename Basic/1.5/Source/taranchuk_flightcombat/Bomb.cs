using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace taranchuk_flightcombat
{
    public class Bomb : Projectile_Explosive
    {
        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            this.launcher = launcher;
            this.origin = origin;
            this.usedTarget = usedTarget;
            this.intendedTarget = intendedTarget;
            this.targetCoverDef = targetCoverDef;
            this.preventFriendlyFire = preventFriendlyFire;
            HitFlags = hitFlags;
            if (equipment != null)
            {
                equipmentDef = equipment.def;
                weaponDamageMultiplier = equipment.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier);
            }
            else
            {
                equipmentDef = null;
                weaponDamageMultiplier = 1f;
            }
            destination = usedTarget.Cell.ToVector3Shifted();
            ticksToImpact = Mathf.CeilToInt(StartingTicksToImpact);
            if (ticksToImpact < 1)
            {
                ticksToImpact = 1;
            }
            if (!def.projectile.soundAmbient.NullOrUndefined())
            {
                SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
                ambientSustainer = def.projectile.soundAmbient.TrySpawnSustainer(info);
            }
        }

        public override void Draw()
        {
            float num = ArcHeightFactor * GenMath.InverseParabola(DistanceCoveredFraction);
            Vector3 drawPos = DrawPos;
            Vector3 position = drawPos + new Vector3(0f, 0f, 1f) * num;
            if (def.projectile.shadowSize > 0f)
            {
                DrawShadow(drawPos, DistanceCoveredFraction);
            }
            var size = def.graphicData.drawSize * (DistanceCoveredFraction + 0.5f);
            Graphics.DrawMesh(MeshPool.GridPlane(size), position, ExactRotation, DrawMat, 0);
            Comps_PostDraw();
        }
    }
}
