using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace taranchuk_projectiletrails
{
    public class RocketExtension : DefModExtension
    {
        public float archHeightMultiplier = 1f;
        public Color trailColor = Color.white;
    }
    [DefOf]
    public static class TPT_DefOf
    {
        public static FleckDef CVN_RocketSmoke;
        public static FleckDef CVN_RocketExhaust;
    }
    public class RocketWithTrails : Projectile_Explosive
    {
        public float ArchHeightMultiplier => def.GetModExtension<RocketExtension>()?.archHeightMultiplier ?? 1f;
        private Vector3 LookTowards =>
            new(this.destination.x - this.origin.x, this.def.Altitude, this.destination.z - this.origin.z +
                (this.ArcHeightFactor * ArchHeightMultiplier) * (4 - 8 * this.DistanceCoveredFraction));

        public override Quaternion ExactRotation => Quaternion.LookRotation(this.LookTowards);

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            ThrowDustPuffThick(this.DrawPos, Map, 5f);
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float num = (ArcHeightFactor * ArchHeightMultiplier) * GenMath.InverseParabola(DistanceCoveredFraction);
            Vector3 drawPos = DrawPos;
            Vector3 position = drawPos + new Vector3(0f, 0f, 1f) * num;
            if (def.projectile.shadowSize > 0f)
            {
                DrawShadow(drawPos, num);
            }
            Graphics.DrawMesh(MeshPool.GridPlane(def.graphicData.drawSize), position, ExactRotation, DrawMat, 0);
            Comps_PostDraw();
        }

        public override void Tick()
        {
            base.Tick();
            if (this.Map != null)
            {
                float num = (ArcHeightFactor * ArchHeightMultiplier) * GenMath.InverseParabola(DistanceCoveredFraction);
                Vector3 drawPos = DrawPos;
                Vector3 position = drawPos + new Vector3(0f, 0f, 1f) * num;
                if (Rand.Chance(0.5f))
                {
                    ThrowSmokeTrail(position, base.Map, Vector3.Angle(origin, position), 1.5f);
                }
                ThrowRocketExhaust(position, base.Map, Vector3.Angle(origin, position), 1f);
            }
        }

        public void ThrowSmokeTrail(Vector3 loc, Map map, float angle, float size)
        {
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, TPT_DefOf.CVN_RocketSmoke, size);
            dataStatic.rotationRate = Rand.Range(-30f, 30f);
            dataStatic.velocityAngle = angle;
            dataStatic.velocitySpeed = Rand.Range(0.008f, 0.012f);
            dataStatic.instanceColor = def.GetModExtension<RocketExtension>()?.trailColor;
            map.flecks.CreateFleck(dataStatic);
        }

        public void ThrowRocketExhaust(Vector3 loc, Map map, float angle, float size)
        {
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, TPT_DefOf.CVN_RocketExhaust, size);
            dataStatic.velocityAngle = angle;
            dataStatic.solidTimeOverride = 0.20f * (1f - (DistanceCoveredFraction + 0.1f));
            dataStatic.velocitySpeed = 0.01f;
            map.flecks.CreateFleck(dataStatic);
        }

        public void ThrowDustPuffThick(Vector3 loc, Map map, float scale)
        {
            FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc, map, FleckDefOf.DustPuffThick, scale);
            dataStatic.rotationRate = Rand.Range(-60, 60);
            dataStatic.velocityAngle = Rand.Range(0, 360);
            dataStatic.velocitySpeed = Rand.Range(0.6f, 0.75f);
            map.flecks.CreateFleck(dataStatic);
        }
    }
}
