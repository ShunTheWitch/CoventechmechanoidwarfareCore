using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace taranchuk_lasers
{
    [HotSwappable]
    public class LaserProjectile : Projectile
    {
        public Vector3 originOld;
        public Vector3 launcherPosOld;
        public float launcherAngleOld;
        public int launchTick;
        public float originAngle;
        public Vector3 originDest;
        private LaserProperties laserProperties;
        public LaserProperties LaserProperties => laserProperties ??= def.GetModExtension<LaserProperties>();
        private float angleOffset;
        private Effecter endEffecter;
        private Sustainer activeSustainer;
        private int sustainerStartTick;

        private int remainingChains;
        private HashSet<Thing> chainedTargets = new HashSet<Thing>();
        private int nextChainTick;
        private bool isChaining;
        private Thing currentChainTarget;
        private Vector3 chainStartPos;
        private List<ChainArc> activeArcs = new List<ChainArc>();
        private bool chainStarted;

        private List<BranchArc> activeBranches = new List<BranchArc>();
        private HashSet<Thing> branchedTargets = new HashSet<Thing>();
        private List<PendingBranchChain> pendingBranchChains = new List<PendingBranchChain>();

        private class ChainArc
        {
            public Vector3 start;
            public Vector3 end;
            public Thing target;
            public int chainIndex;
            public int lifetimeTicks;
            public int maxLifetime;
            public int nextDamageTick;

            public ChainArc(Vector3 start, Vector3 end, Thing target, int chainIndex, int lifetimeTicks = 60)
            {
                this.start = start;
                this.end = end;
                this.target = target;
                this.chainIndex = chainIndex;
                this.maxLifetime = lifetimeTicks;
                this.lifetimeTicks = 0;
                this.nextDamageTick = 0;
            }

            public void Update()
            {
                lifetimeTicks++;
            }

            public bool IsExpired => lifetimeTicks >= maxLifetime;
        }

        private class BranchArc
        {
            public Vector3 start;
            public Vector3 end;
            public Thing target;
            public int chainIndex;
            public int lifetimeTicks;
            public int maxLifetime;
            public int nextDamageTick;
            public Thing parentTarget;

            public BranchArc(Vector3 start, Vector3 end, Thing target, int chainIndex, int lifetimeTicks = 60, Thing parentTarget = null)
            {
                this.start = start;
                this.end = end;
                this.target = target;
                this.chainIndex = chainIndex;
                this.maxLifetime = lifetimeTicks;
                this.lifetimeTicks = 0;
                this.nextDamageTick = 0;
                this.parentTarget = parentTarget;
            }

            public void Update()
            {
                lifetimeTicks++;
            }

            public bool IsExpired => lifetimeTicks >= maxLifetime;
        }

        private class PendingBranchChain
        {
            public Thing target;
            public int chainIndex;
            public int processTick;
        }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false,
            Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, targetCoverDef);
            launchTick = Find.TickManager.TicksGame;
            originAngle = ExactRotation.eulerAngles.y;
            angleOffset = LaserProperties.sweepRatePerTick;
            originDest = destination;
            launcherPosOld = launcher.DrawPos;
            launcherAngleOld = Utilities.GetBodyAngle(launcher);
            originOld = origin;

            remainingChains = LaserProperties.maxChainTargets;
            isChaining = false;
            chainedTargets.Clear();
            activeArcs.Clear();
            activeBranches.Clear();
            branchedTargets.Clear();
            pendingBranchChains.Clear();
        }

        private float GetAngleFromTarget(Vector3 target)
        {
            var targetAngle = (origin.Yto0() - target.Yto0()).AngleFlat() - 180f;
            return AngleAdjusted(targetAngle);
        }

        public float AngleAdjusted(float angle)
        {
            if (angle > 360f)
            {
                angle -= 360f;
            }
            if (angle < 0f)
            {
                angle += 360f;
            }
            return angle;
        }

        private Vector2 textureScroll;

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var num = this.ArcHeightFactor * GenMath.InverseParabola(DistanceCoveredFraction);
            var distanceSize = Vector3.Distance(origin.Yto0(), drawLoc.Yto0());
            var drawPos = Vector3.Lerp(origin, drawLoc, 0.5f);
            drawPos.y += 1f;
            var position = drawPos + new Vector3(0f, 0f, 1f) * num;
            Comps_PostDraw();
            var mat = DrawMat;
            if (textureScroll != Vector2.zero)
            {
                mat.SetTextureOffset("_MainTex", textureScroll);
            }
            foreach (var arc in activeArcs)
            {
                DrawChainArc(arc.start, arc.end);
            }
            foreach (var branch in activeBranches)
            {
                DrawChainArc(branch.start, branch.end);
            }
            Graphics.DrawMesh(MeshPool.GridPlane(new(LaserProperties.beamWidth * LaserProperties.beamWidthDrawScale,
                distanceSize)), position, ExactRotation, DrawMat, 0);
        }

        private void DrawChainArc(Vector3 start, Vector3 end)
        {
            var distance = Vector3.Distance(start.Yto0(), end.Yto0());
            var drawPos = Vector3.Lerp(start, end, 0.5f);
            drawPos.y += 1f;
            var rotation = Quaternion.LookRotation(end - start);
            var mat = DrawMat;
            Graphics.DrawMesh(MeshPool.GridPlane(new(LaserProperties.beamWidth * LaserProperties.beamWidthDrawScale * 0.7f,
                distance)), drawPos, rotation, mat, 0);
        }

        public override void Impact(Thing hitThing, bool blockedByShield = false)
        {

        }

        private void TryStartChain(Thing hitThing)
        {
            if (chainStarted) return;
            if (LaserProperties.maxChainTargets <= 0) return;
            if (hitThing == null) return;

            chainStarted = true;
            isChaining = true;
            chainedTargets.Add(hitThing);
            currentChainTarget = hitThing;
            chainStartPos = ExactPosition;
            remainingChains = LaserProperties.maxChainTargets - 1;

            var mainArc = new ChainArc(origin, hitThing.DrawPos, hitThing, 0, LaserProperties.lifetimeTicks);
            mainArc.nextDamageTick = int.MaxValue;
            activeArcs.Add(mainArc);

            TryCreateBranches(hitThing, 0);

            if (remainingChains > 0)
            {
                nextChainTick = Find.TickManager.TicksGame + LaserProperties.chainDelayTicks;
            }
        }

        protected void ImpactOverride(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 position = Position;
            BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(launcher, hitThing, intendedTarget.Thing, equipmentDef, def, targetCoverDef);
            Find.BattleLog.Add(battleLogEntry_RangedImpact);

            if (hitThing != null && !blockedByShield)
            {
                bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;
                DamageInfo dinfo = new DamageInfo(def.projectile.damageDef, DamageAmount, ArmorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                dinfo.SetWeaponQuality(equipmentQuality);
                hitThing.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);

                Pawn pawn2 = hitThing as Pawn;
                if (def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(), ExactRotation.eulerAngles.y, launcher, null, equipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, intendedTarget.Thing, instigatorGuilty);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }

                if (Rand.Chance(DamageDef.igniteCellChance))
                {
                    FireUtility.TryStartFireIn(Position, map, Rand.Range(0.55f, 0.85f), launcher);
                }

                TryStartChain(hitThing);
            }
        }

        private bool shouldBeDestroyed;

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (activeSustainer != null)
            {
                activeSustainer.End();
                activeSustainer = null;
            }
            if (shouldBeDestroyed)
            {
                base.Destroy(mode);
            }
        }

        private Thing GetNextChainTarget()
        {
            if (remainingChains <= 0) return null;

            var currentPos = currentChainTarget != null ? currentChainTarget.DrawPos : ExactPosition;
            var bestTarget = (Thing)null;
            var bestDistance = float.MaxValue;

            var allThings = new List<Thing>();
            var rect = CellRect.CenteredOn(currentPos.ToIntVec3(), Mathf.CeilToInt(LaserProperties.chainRange));

            for (int x = rect.minX; x <= rect.maxX; x++)
            {
                for (int z = rect.minZ; z <= rect.maxZ; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (cell.InBounds(Map))
                    {
                        allThings.AddRange(cell.GetThingList(Map));
                    }
                }
            }

            foreach (var thing in allThings)
            {
                if (chainedTargets.Contains(thing)) continue;
                if (thing == launcher) continue;
                if (!IsDamagable(thing)) continue;
                if (!LaserProperties.chainToAnything && !(thing is Pawn)) continue;
                if (!LaserProperties.chainAllowFriendlyFire && IsFriendly(thing)) continue;

                var distance = Vector3.Distance(currentPos, thing.DrawPos);
                if (distance <= LaserProperties.chainRange && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = thing;
                }
            }

            return bestTarget;
        }

        private bool IsFriendly(Thing thing)
        {
            if (launcher is Pawn launcherPawn && thing is Pawn targetPawn)
            {
                return !launcherPawn.HostileTo(targetPawn);
            }
            return false;
        }

        private void ExecuteChain()
        {
            if (!isChaining || remainingChains <= 0) return;

            var nextTarget = GetNextChainTarget();
            if (nextTarget == null)
            {
                isChaining = false;
                return;
            }

            Vector3 arcStart = currentChainTarget != null ? currentChainTarget.DrawPos : chainStartPos;

            var chainIndex = LaserProperties.maxChainTargets - remainingChains;
            var damageMultiplier = Mathf.Pow(LaserProperties.chainDamageFalloff, chainIndex);
            var chainDamage = DamageAmount * damageMultiplier;
            var chainArmorPen = ArmorPenetration * damageMultiplier;

            var chainArc = new ChainArc(arcStart, nextTarget.DrawPos, nextTarget, chainIndex, LaserProperties.chainArcLifetime);
            chainArc.nextDamageTick = Find.TickManager.TicksGame;
            activeArcs.Add(chainArc);

            chainedTargets.Add(nextTarget);
            currentChainTarget = nextTarget;
            remainingChains--;

            TryCreateBranches(currentChainTarget, chainIndex);

            if (remainingChains > 0)
            {
                nextChainTick = Find.TickManager.TicksGame + LaserProperties.chainDelayTicks;
            }
            else
            {
                isChaining = false;
            }
        }

        private void TryCreateBranches(Thing fromTarget, int currentChainIndex)
        {
            if (LaserProperties.branchChance <= 0) return;

            if (currentChainIndex >= LaserProperties.maxChainTargets) return;

            var possibleBranchTargets = GetPossibleBranchTargets(fromTarget);
            int branchesCreated = 0;

            foreach (var target in possibleBranchTargets)
            {
                if (Rand.Chance(LaserProperties.branchChance))
                {
                    if (branchesCreated >= LaserProperties.maxBranches) break;

                    if (!LaserProperties.branchToSameTargets &&
                        (chainedTargets.Contains(target) || branchedTargets.Contains(target)))
                        continue;

                    CreateBranch(fromTarget, target, currentChainIndex + 1);
                    branchesCreated++;
                    branchedTargets.Add(target);
                }
            }
        }

        private List<Thing> GetPossibleBranchTargets(Thing fromTarget)
        {
            var potentialTargets = new List<Thing>();
            var currentPos = fromTarget.DrawPos;
            var rect = CellRect.CenteredOn(currentPos.ToIntVec3(), Mathf.CeilToInt(LaserProperties.chainRange));

            for (int x = rect.minX; x <= rect.maxX; x++)
            {
                for (int z = rect.minZ; z <= rect.maxZ; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (cell.InBounds(Map))
                    {
                        foreach (var thing in cell.GetThingList(Map))
                        {
                            if (IsValidBranchTarget(thing, fromTarget))
                            {
                                potentialTargets.Add(thing);
                            }
                        }
                    }
                }
            }

            potentialTargets.Sort((a, b) =>
                Vector3.Distance(currentPos, a.DrawPos)
                    .CompareTo(Vector3.Distance(currentPos, b.DrawPos)));

            return potentialTargets;
        }

        private bool IsValidBranchTarget(Thing target, Thing fromTarget)
        {
            if (target == fromTarget) return false;
            if (target == launcher) return false;
            if (!IsDamagable(target)) return false;
            if (!LaserProperties.chainToAnything && !(target is Pawn)) return false;
            if (!LaserProperties.chainAllowFriendlyFire && IsFriendly(target)) return false;

            var distance = Vector3.Distance(fromTarget.DrawPos, target.DrawPos);
            if (distance > LaserProperties.chainRange) return false;

            return true;
        }

        private void CreateBranch(Thing fromTarget, Thing toTarget, int chainIndex)
        {
            if (chainIndex >= LaserProperties.maxChainTargets) return;

            var damageMultiplier = Mathf.Pow(LaserProperties.chainDamageFalloff, chainIndex);
            var branchArc = new BranchArc(
                fromTarget.DrawPos,
                toTarget.DrawPos,
                toTarget,
                chainIndex,
                LaserProperties.chainArcLifetime,
                fromTarget
            );
            branchArc.nextDamageTick = Find.TickManager.TicksGame;

            activeBranches.Add(branchArc);

            if (chainIndex + 1 < LaserProperties.maxChainTargets)
            {
                ScheduleBranchChaining(toTarget, chainIndex);
            }
        }

        private void ScheduleBranchChaining(Thing target, int currentChainIndex)
        {
            pendingBranchChains.Add(new PendingBranchChain
            {
                target = target,
                chainIndex = currentChainIndex,
                processTick = Find.TickManager.TicksGame + LaserProperties.chainDelayTicks
            });
        }

        private void ProcessPendingBranches()
        {
            for (int i = pendingBranchChains.Count - 1; i >= 0; i--)
            {
                var pending = pendingBranchChains[i];
                if (Find.TickManager.TicksGame >= pending.processTick)
                {
                    TryCreateBranches(pending.target, pending.chainIndex);
                    pendingBranchChains.RemoveAt(i);
                }
            }
        }

        private void DamageChainTargets()
        {
            if (LaserProperties.damageTickRate <= 0) return;

            foreach (var arc in activeArcs)
            {
                if (arc.chainIndex == 0) continue;
                if (arc.target == null || arc.target.Destroyed) continue;

                if (Find.TickManager.TicksGame >= arc.nextDamageTick)
                {
                    var chainIndex = arc.chainIndex;
                    var damageMultiplier = Mathf.Pow(LaserProperties.chainDamageFalloff, chainIndex);
                    var chainDamage = DamageAmount * damageMultiplier;
                    var chainArmorPen = ArmorPenetration * damageMultiplier;
                    bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;

                    var dinfo = new DamageInfo(
                        def.projectile.damageDef,
                        (int)chainDamage,
                        chainArmorPen,
                        (arc.target.DrawPos - arc.start).AngleFlat(),
                        launcher,
                        null,
                        equipmentDef,
                        DamageInfo.SourceCategory.ThingOrUnknown,
                        intendedTarget.Thing,
                        !LaserProperties.chainAllowFriendlyFire
                    );

                    arc.target.TakeDamage(dinfo);

                    if (def.projectile.extraDamages != null)
                    {
                        foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                        {
                            if (Rand.Chance(extraDamage.chance))
                            {
                                DamageInfo dinfo2 = new DamageInfo(
                                    extraDamage.def,
                                    (int)(extraDamage.amount * damageMultiplier),
                                    extraDamage.AdjustedArmorPenetration() * damageMultiplier,
                                    (arc.target.DrawPos - arc.start).AngleFlat(),
                                    launcher,
                                    null,
                                    equipmentDef,
                                    DamageInfo.SourceCategory.ThingOrUnknown,
                                    intendedTarget.Thing,
                                    !LaserProperties.chainAllowFriendlyFire
                                );
                                arc.target.TakeDamage(dinfo2);
                            }
                        }
                    }

                    if (Rand.Chance(DamageDef.igniteCellChance))
                    {
                        FireUtility.TryStartFireIn(arc.target.Position, Map, Rand.Range(0.55f, 0.85f), launcher);
                    }

                    arc.nextDamageTick = Find.TickManager.TicksGame + LaserProperties.damageTickRate;
                }
            }

            foreach (var branch in activeBranches)
            {
                if (branch.target == null || branch.target.Destroyed) continue;

                if (Find.TickManager.TicksGame >= branch.nextDamageTick)
                {
                    var damageMultiplier = Mathf.Pow(LaserProperties.chainDamageFalloff, branch.chainIndex);
                    var chainDamage = DamageAmount * damageMultiplier;
                    var chainArmorPen = ArmorPenetration * damageMultiplier;
                    bool instigatorGuilty = !(launcher is Pawn pawn) || !pawn.Drafted;

                    var dinfo = new DamageInfo(
                        def.projectile.damageDef,
                        (int)chainDamage,
                        chainArmorPen,
                        (branch.target.DrawPos - branch.start).AngleFlat(),
                        launcher,
                        null,
                        equipmentDef,
                        DamageInfo.SourceCategory.ThingOrUnknown,
                        intendedTarget.Thing,
                        !LaserProperties.chainAllowFriendlyFire
                    );

                    branch.target.TakeDamage(dinfo);

                    if (def.projectile.extraDamages != null)
                    {
                        foreach (ExtraDamage extraDamage in def.projectile.extraDamages)
                        {
                            if (Rand.Chance(extraDamage.chance))
                            {
                                DamageInfo dinfo2 = new DamageInfo(
                                    extraDamage.def,
                                    (int)(extraDamage.amount * damageMultiplier),
                                    extraDamage.AdjustedArmorPenetration() * damageMultiplier,
                                    (branch.target.DrawPos - branch.start).AngleFlat(),
                                    launcher,
                                    null,
                                    equipmentDef,
                                    DamageInfo.SourceCategory.ThingOrUnknown,
                                    intendedTarget.Thing,
                                    !LaserProperties.chainAllowFriendlyFire
                                );
                                branch.target.TakeDamage(dinfo2);
                            }
                        }
                    }

                    if (Rand.Chance(DamageDef.igniteCellChance))
                    {
                        FireUtility.TryStartFireIn(branch.target.Position, Map, Rand.Range(0.55f, 0.85f), launcher);
                    }

                    branch.nextDamageTick = Find.TickManager.TicksGame + LaserProperties.damageTickRate;
                }
            }
        }

        public override void Tick()
        {
            base.Tick();

            for (int i = activeArcs.Count - 1; i >= 0; i--)
            {
                activeArcs[i].Update();
                if (activeArcs[i].IsExpired)
                {
                    activeArcs.RemoveAt(i);
                }
            }

            for (int i = activeBranches.Count - 1; i >= 0; i--)
            {
                activeBranches[i].Update();
                if (activeBranches[i].IsExpired)
                {
                    activeBranches.RemoveAt(i);
                }
            }

            LockOnCaster();
            textureScroll += LaserProperties.textureScrollOffsetPerTick;

            if (LaserProperties.damageTickRate > 0 && this.IsHashIntervalTick(LaserProperties.damageTickRate))
            {
                DamageThings();
                DamageChainTargets();
            }

            if (LaserProperties.sweepRatePerTick > 0)
            {
                DoSweep();
            }
            else
            {
                if (LaserProperties.lockOnTarget)
                {
                    destination = intendedTarget.CenterVector3;
                }
            }
            if (LaserProperties.groundFleckDef != null && Rand.Chance(LaserProperties.fleckChancePerTick))
            {
                FleckMaker.Static(ExactPosition, Map, LaserProperties.groundFleckDef);
            }

            var intVec = ExactPosition.ToIntVec3();
            Vector3 vector3 = ExactPosition - intVec.ToVector3Shifted();
            if (endEffecter == null && LaserProperties.endEffecterDef != null)
            {
                endEffecter = LaserProperties.endEffecterDef.Spawn(intVec, Map, vector3);
            }
            if (endEffecter != null)
            {
                endEffecter.offset = vector3;
                endEffecter.EffectTick(new TargetInfo(intVec, Map), TargetInfo.Invalid);
                endEffecter.ticksLeft--;
            }

            if (LaserProperties.sustainerSoundDef != null)
            {
                if (activeSustainer == null)
                {
                    if (LaserProperties.sustainerTickPeriod <= 0 || sustainerStartTick == 0)
                    {
                        activeSustainer = SoundStarter.TrySpawnSustainer(LaserProperties.sustainerSoundDef, SoundInfo.InMap(this, MaintenanceType.PerTick));
                        sustainerStartTick = Find.TickManager.TicksGame;
                    }
                }
                else if (LaserProperties.sustainerTickPeriod > 0 && sustainerStartTick > 0 && Find.TickManager.TicksGame - sustainerStartTick >= LaserProperties.sustainerTickPeriod)
                {
                    activeSustainer.End();
                    activeSustainer = null;
                    sustainerStartTick = -1;
                    if (LaserProperties.trailSoundDef != null)
                    {
                        SoundStarter.PlayOneShot(LaserProperties.trailSoundDef, SoundInfo.InMap(this, MaintenanceType.None));
                    }
                }

                if (activeSustainer != null)
                {
                    activeSustainer.Maintain();
                }
            }

            if (isChaining && Find.TickManager.TicksGame >= nextChainTick)
            {
                ExecuteChain();
            }

            ProcessPendingBranches();

            if (Find.TickManager.TicksGame > launchTick + LaserProperties.lifetimeTicks)
            {
                Explode();
                shouldBeDestroyed = true;
                Destroy();
            }
        }

        private void LockOnCaster()
        {
            var bodyAngle = Utilities.GetBodyAngle(launcher);
            var drawPos = launcher.DrawPos.Yto0();
            var angleDiff = bodyAngle - launcherAngleOld;
            var originOffset = (originOld.Yto0() - launcherPosOld.Yto0()).RotatedBy(angleDiff);
            origin = drawPos + originOffset;
        }

        private void DoSweep()
        {
            var origAngle = LaserProperties.lockOnTarget ? GetAngleFromTarget(intendedTarget.CenterVector3) : originAngle;
            var angle = ExactRotation.eulerAngles.y - origAngle;
            if (angle > LaserProperties.maxSweepAngle)
            {
                angleOffset = -LaserProperties.sweepRatePerTick;
            }
            else if (0 > angle && Mathf.Abs(angle) > LaserProperties.maxSweepAngle)
            {
                angleOffset = LaserProperties.sweepRatePerTick;
            }
            destination = RotatePointAroundPivot(destination, origin, new Vector3(0, angleOffset, 0));
            if (LaserProperties.lockOnTarget)
            {
                var distance = Vector3.Distance(origin.Yto0(), destination.Yto0());
                var distance2 = Vector3.Distance(origin.Yto0(), intendedTarget.CenterVector3.Yto0());
                var diff = distance2 - distance;
                destination += (Vector3.forward * diff).RotatedBy(ExactRotation.eulerAngles.y);
            }
        }

        private List<IntVec3> GetSweepCells()
        {
            var cells = new HashSet<IntVec3>();
            var angleOffsetTmp = -LaserProperties.maxSweepAngle;
            var origDest = LaserProperties.lockOnTarget ? intendedTarget.CenterVector3 : originDest;
            var destTmp = RotatePointAroundPivot(origDest, origin, new Vector3(0, angleOffsetTmp, 0));
            cells.Add(destTmp.ToIntVec3());
            while (true)
            {
                angleOffsetTmp += LaserProperties.sweepRatePerTick;
                if (angleOffsetTmp > LaserProperties.maxSweepAngle)
                {
                    break;
                }
                destTmp = RotatePointAroundPivot(destTmp, origin, new Vector3(0, LaserProperties.sweepRatePerTick, 0));
                cells.Add(destTmp.ToIntVec3());
            }
            if (LaserProperties.explosionRadius > 0)
            {
                foreach (var cell in cells.ToList())
                {
                    cells.AddRange(GenRadial.RadialCellsAround(cell, LaserProperties.explosionRadius, true));
                }
            }
            return cells.ToList();
        }

        private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            var dir = point - pivot;
            dir = Quaternion.Euler(angles) * dir;
            point = dir + pivot;
            return point;
        }

        private void DamageThings()
        {
            HashSet<IntVec3> allCellsImpact = GetImpactCells(LaserProperties.damageThingsAcrossBeamLine);
            var thingsToDamage = new HashSet<Thing>();
            foreach (var cell in allCellsImpact)
            {
                if (cell.InBounds(Map))
                {
                    var things = cell.GetThingList(Map);
                    thingsToDamage.AddRange(things);
                    if (LaserProperties.debugCells)
                    {
                        Map.debugDrawer.FlashCell(cell);
                    }
                }
            }
            foreach (var thing in thingsToDamage)
            {
                if (IsDamagable(thing))
                {
                    ImpactOverride(thing, false);
                }
            }
        }

        private HashSet<IntVec3> GetImpactCells(bool includeCellsAcrossBeamLine)
        {
            var allCellsImpact = new HashSet<IntVec3>();
            allCellsImpact.AddRange(GenRadial.RadialCellsAround(ExactPosition.ToIntVec3(), LaserProperties.beamWidth, true));
            if (includeCellsAcrossBeamLine)
            {
                var distance = Vector3.Distance(origin.Yto0(), ExactPosition.Yto0());
                float dist = 0;
                while (dist < distance)
                {
                    dist += 0.5f;
                    var newPos = Vector3.MoveTowards(origin, ExactPosition, dist);
                    var cell = newPos.ToIntVec3();
                    if (allCellsImpact.Add(cell))
                    {
                        allCellsImpact.AddRange(GenRadial.RadialCellsAround(cell, LaserProperties.beamWidth, true));
                    }
                }
            }
            return allCellsImpact;
        }

        private bool IsDamagable(Thing thing)
        {
            return (thing is Pawn || thing.def.useHitPoints) && thing != launcher
                && thing is not Projectile && thing is not Filth && thing is not Mote;
        }

        private void Explode()
        {
            if (LaserProperties.explosionOnEnd != null)
            {
                IntVec3 position = origin.ToIntVec3();
                var cells = LaserProperties.sweepRatePerTick > 0 ? GetSweepCells()
                    : GetImpactCells(LaserProperties.damageThingsAcrossBeamLine).ToList();
                GenExplosion.DoExplosion(center: position, map: Map, radius: 0f,
                    damType: LaserProperties.explosionOnEnd,
                    instigator: launcher,
                    overrideCells: cells, propagationSpeed: LaserProperties.explosionSpeed);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref launchTick, "launchTick");
            Scribe_Values.Look(ref originAngle, "originAngle");
            Scribe_Values.Look(ref angleOffset, "angleOffset");
            Scribe_Values.Look(ref originDest, "originDest");
            Scribe_Values.Look(ref launcherPosOld, "launcherPosOld");
            Scribe_Values.Look(ref launcherAngleOld, "launcherAngleOld");
            Scribe_Values.Look(ref sustainerStartTick, "sustainerStartTick");

            Scribe_Values.Look(ref remainingChains, "remainingChains");
            Scribe_Values.Look(ref nextChainTick, "nextChainTick");
            Scribe_Values.Look(ref isChaining, "isChaining");
            Scribe_Collections.Look(ref chainedTargets, "chainedTargets", LookMode.Reference);
        }
    }
}