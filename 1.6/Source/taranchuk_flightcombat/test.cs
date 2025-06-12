using HarmonyLib;
using RimWorld;
using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(VehicleTurret), "TurretAutoTick")]
    public static class VehicleTurret_TurretAutoTick_Patch
    {
        public static bool Prefix(VehicleTurret __instance, ref bool __result)
        {
            if (__instance.HasAmmo is false)
            {
                Log.Message("REloading: " + __instance);
                ThingDef ammoType = __instance.vehicle.inventory.innerContainer
                    .FirstOrDefault(t => __instance.turretDef.ammunition.Allows(t) 
                    || __instance.turretDef.ammunition.Allows(t.def.projectileWhenLoaded))?.def;
                if (ammoType != null)
                {
                    __instance.ReloadInternal(ammoType);
                }
                Log.Message("REloading: " + __instance + " - " + __instance.HasAmmo);
            }
            __result = TurretAutoTick(__instance);
            return false;
        }


        private static bool TurretAutoTick(VehicleTurret __instance)
        {
            if (__instance.vehicle.Spawned && !__instance.queuedToFire && __instance.AutoTarget)
            {
                if (Find.TickManager.TicksGame % VehicleTurret.AutoTargetInterval == 0)
                {
                    if (__instance.TurretDisabled)
                    {
                        Log.Message("Fail 2");
                        return false;
                    }
                    if (!__instance.cannonTarget.IsValid && TurretTargeter.Turret != __instance && __instance.ReloadTicks <= 0 
                        && __instance.HasAmmo)
                    {
                        if (__instance.TryGetTarget(out LocalTargetInfo autoTarget))
                        {
                            __instance.AlignToAngleRestricted(__instance.TurretLocation.AngleToPoint(autoTarget.Thing.DrawPos));
                            __instance.SetTarget(autoTarget);
                        }
                        else
                        {
                            Log.Message("Fail 4");
                        }
                    }
                    else
                    {
                        Log.Message(__instance.vehicle.Faction + " - Fail 3: __instance.HasAmmo: " + __instance.HasAmmo);
                    }
                }
                return true;
            }
            else
            {
                Log.Message("Fail 1");
            }
            return false;
        }

    }
    [HotSwappable]
    [HarmonyPatch(typeof(TargetingHelper), "TryGetTarget")]
    public static class test
    {
        public static bool Prefix(ref bool __result, VehicleTurret turret, ref LocalTargetInfo targetInfo, TargetingParameters param = null)
        {
            //if (turret.vehicle.HostileTo(Faction.OfPlayer))
            {
                __result = TryGetTarget(turret, out targetInfo, param);
                Log.Message(targetInfo + " - " + turret.vehicle + " - __result: " + __result);
                return false;
            }
            return true;
        }

        public static bool TryGetTarget(this VehicleTurret turret, out LocalTargetInfo targetInfo, TargetingParameters param = null)
        {
            targetInfo = LocalTargetInfo.Invalid;
            TargetScanFlags targetScanFlags = turret.turretDef.targetScanFlags;
            Thing thing = (Thing)BestAttackTarget(turret, targetScanFlags, delegate (Thing thing)
            {
                return TurretTargeter.TargetMeetsRequirements(turret, thing);
            }, canTakeTargetsCloserThanEffectiveMinRange: false);
            if (thing != null)
            {
                targetInfo = new LocalTargetInfo(thing);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Best attack target for VehicleTurret
        /// </summary>
        public static IAttackTarget BestAttackTarget(VehicleTurret turret, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f, float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = 3.4028235E+38f, bool canBash = false, bool canTakeTargetsCloserThanEffectiveMinRange = true)
        {
            VehiclePawn searcherPawn = turret.vehicle;

            float minDistSquared = minDist * minDist;
            float num = maxTravelRadiusFromLocus + turret.MaxRange;
            float maxLocusDistSquared = num * num;
            Func<IntVec3, bool> losValidator = null;
            if (flags.HasFlag(TargetScanFlags.LOSBlockableByGas))
            {
                losValidator = (pos) => pos.AnyGas(searcherPawn.Map, GasType.BlindSmoke);
            }
            Predicate<IAttackTarget> innerValidator = delegate (IAttackTarget t)
            {
                Thing thing = t.Thing;
                if (t == searcherPawn)
                {
                    Log.Message(thing + " - false 1");
                    return false;
                }
                if (minDistSquared > 0f && (searcherPawn.Position - thing.Position).LengthHorizontalSquared < minDistSquared)
                {
                    Log.Message(thing + " - false 2");
                    return false;
                }
                if (!canTakeTargetsCloserThanEffectiveMinRange)
                {
                    float num2 = turret.MinRange;
                    if (num2 > 0f && (turret.vehicle.Position - thing.Position).LengthHorizontalSquared < num2 * num2)
                    {
                        Log.Message(thing + " - false 3");
                        return false;
                    }
                }
                if (maxTravelRadiusFromLocus < 9999f && (thing.Position - locus).LengthHorizontalSquared > maxLocusDistSquared)
                {
                    Log.Message(thing + " - false 4");
                    return false;
                }
                if (!searcherPawn.HostileTo(thing))
                {
                    Log.Message(thing + " - false 5");
                    return false;
                }
                if (validator != null && !validator(thing))
                {
                    Log.Message(thing + " - false 6");
                    return false;
                }
                if ((flags & TargetScanFlags.NeedLOSToAll) != TargetScanFlags.None)
                {
                    if (losValidator != null && (!losValidator(searcherPawn.Position) || !losValidator(thing.Position)))
                    {
                        Log.Message(thing + " - false 7");
                        return false;
                    }
                    if (!searcherPawn.CanSee(thing, losValidator))
                    {
                        if (t is Pawn)
                        {
                            if ((flags & TargetScanFlags.NeedLOSToPawns) != TargetScanFlags.None)
                            {
                                Log.Message(thing + " - false 8");
                                return false;
                            }
                        }
                        else if ((flags & TargetScanFlags.NeedLOSToNonPawns) != TargetScanFlags.None)
                        {
                            Log.Message(thing + " - false 9");
                            return false;
                        }
                    }
                }
                if (((flags & TargetScanFlags.NeedThreat) != TargetScanFlags.None 
                || (flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None) 
                && t.ThreatDisabled(searcherPawn))
                {
                    if (t is VehiclePawn vehiclePawn)
                    {
                    }
                    Log.Message(thing + " - false 10: " + t.ThreatDisabled(searcherPawn) + " - t.ThreatDisabled: " + t.ThreatDisabled(null));
                    return false;
                }
                if ((flags & TargetScanFlags.NeedAutoTargetable) != TargetScanFlags.None && !AttackTargetFinder.IsAutoTargetable(t))
                {
                    Log.Message(thing + " - false 11");
                    return false;
                }
                if ((flags & TargetScanFlags.NeedActiveThreat) != TargetScanFlags.None && !GenHostility.IsActiveThreatTo(t, searcherPawn.Faction))
                {
                    Log.Message(thing + " - false 12");
                    return false;
                }
                Pawn pawn = t as Pawn;
                if ((flags & TargetScanFlags.NeedNonBurning) != TargetScanFlags.None && thing.IsBurning())
                {
                    Log.Message(thing + " - false 13");
                    return false;
                }

                if (thing.def.size.x == 1 && thing.def.size.z == 1)
                {
                    if (thing.Position.Fogged(thing.Map))
                    {
                        Log.Message(thing + " - false 14");
                        return false;
                    }
                }
                else
                {
                    bool flag2 = false;
                    using (CellRect.Enumerator enumerator = thing.OccupiedRect().GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (!enumerator.Current.Fogged(thing.Map))
                            {
                                flag2 = true;
                                break;
                            }
                        }
                    }
                    if (!flag2)
                    {
                        Log.Message(thing + " - false 15");
                        return false;
                    }
                }
                return true;
            };

            List<IAttackTarget> tmpTargets = new List<IAttackTarget>();
            tmpTargets.AddRange(GetPotentialTargetsFor(searcherPawn.Map.attackTargetsCache, searcherPawn));
            Log.Message("1 tmpTargets: " + string.Join(", ", tmpTargets.Select(x => x.Thing)));
            bool flag = false;
            for (int i = 0; i < tmpTargets.Count; i++)
            {
                IAttackTarget attackTarget = tmpTargets[i];
                if (attackTarget.Thing.Position.InHorDistOf(searcherPawn.Position, maxDist) && innerValidator(attackTarget) && VehicleTurret.TryFindShootLineFromTo(searcherPawn.Position, new LocalTargetInfo(attackTarget.Thing), out ShootLine resultingLine))
                {
                    flag = true;
                    break;
                }
            }
            Log.Message("2 tmpTargets: " + string.Join(", ", tmpTargets.Select(x => x.Thing)));

            IAttackTarget result;
            if (flag)
            {
                tmpTargets.RemoveAll((IAttackTarget x) => !x.Thing.Position.InHorDistOf(searcherPawn.Position, maxDist) || !innerValidator(x));
                Log.Message("2.5 tmpTargets: " + string.Join(", ", tmpTargets.Select(x => x.Thing)));
                result = TargetingHelper.GetRandomShootingTargetByScore(tmpTargets, searcherPawn);
            }
            else
            {
                Predicate<Thing> validator2;
                if ((flags & TargetScanFlags.NeedReachableIfCantHitFromMyPos) != TargetScanFlags.None && (flags & TargetScanFlags.NeedReachable) == TargetScanFlags.None)
                {
                    validator2 = ((Thing t) => innerValidator((IAttackTarget)t) && VehicleTurret.TryFindShootLineFromTo(searcherPawn.Position, new LocalTargetInfo(t), out ShootLine resultingLine));
                }
                else
                {
                    validator2 = ((Thing t) => innerValidator((IAttackTarget)t));
                }
                result = (IAttackTarget)GenClosest.ClosestThing_Global(searcherPawn.Position, tmpTargets, maxDist, validator2, null);
            }
            Log.Message("3 tmpTargets: " + string.Join(", ", tmpTargets.Select(x => x.Thing)) + " - result: " + result);
            tmpTargets.Clear();
            return result;
        }

        public static bool HostileToTest(this Thing a, Thing b)
        {
            if (a.Destroyed || b.Destroyed || a == b)
            {
                return false;
            }
            if ((a.Faction == null && a.TryGetComp<CompCauseGameCondition>() != null) || (b.Faction == null && b.TryGetComp<CompCauseGameCondition>() != null))
            {
                return true;
            }
            Pawn pawn = a as Pawn;
            Pawn pawn2 = b as Pawn;
            if (pawn != null && pawn2 != null && ((pawn.story != null && pawn.story.traits.DisableHostilityFrom(pawn2)) || (pawn2.story != null && pawn2.story.traits.DisableHostilityFrom(pawn))))
            {
                return false;
            }
            if ((pawn != null && pawn.MentalState != null && pawn.MentalState.ForceHostileTo(b)) || (pawn2 != null && pawn2.MentalState != null && pawn2.MentalState.ForceHostileTo(a)))
            {
                return true;
            }
            if (pawn != null && pawn2 != null && (GenHostility.IsPredatorHostileTo(pawn, pawn2) || GenHostility.IsPredatorHostileTo(pawn2, pawn)))
            {
                return true;
            }
            if ((a.Faction != null && pawn2 != null && pawn2.HostFaction == a.Faction && (pawn == null || pawn.HostFaction == null) && PrisonBreakUtility.IsPrisonBreaking(pawn2)) || (b.Faction != null && pawn != null && pawn.HostFaction == b.Faction && (pawn2 == null || pawn2.HostFaction == null) && PrisonBreakUtility.IsPrisonBreaking(pawn)))
            {
                return true;
            }
            if ((a.Faction != null && pawn2 != null && pawn2.IsSlave && pawn2.Faction == a.Faction && (pawn == null || !pawn.IsSlave) && SlaveRebellionUtility.IsRebelling(pawn2)) || (b.Faction != null && pawn != null && pawn.IsSlave && pawn.Faction == b.Faction && (pawn2 == null || !pawn2.IsSlave) && SlaveRebellionUtility.IsRebelling(pawn)))
            {
                return true;
            }
            if ((a.Faction != null && pawn2 != null && pawn2.HostFaction == a.Faction) || (b.Faction != null && pawn != null && pawn.HostFaction == b.Faction))
            {
                return false;
            }
            if (pawn != null && pawn.IsPrisoner && pawn2 != null && pawn2.IsPrisoner)
            {
                return false;
            }
            if (pawn != null && pawn.IsSlave && pawn2 != null && pawn2.IsSlave)
            {
                return false;
            }
            if (pawn != null && pawn2 != null && ((pawn.IsPrisoner && pawn.HostFaction == pawn2.HostFaction && !PrisonBreakUtility.IsPrisonBreaking(pawn)) || (pawn2.IsPrisoner && pawn2.HostFaction == pawn.HostFaction && !PrisonBreakUtility.IsPrisonBreaking(pawn2))))
            {
                return false;
            }
            if (pawn != null && pawn2 != null && ((pawn.HostFaction != null && pawn2.Faction != null && !pawn.HostFaction.HostileTo(pawn2.Faction) && !PrisonBreakUtility.IsPrisonBreaking(pawn)) || (pawn2.HostFaction != null && pawn.Faction != null && !pawn2.HostFaction.HostileTo(pawn.Faction) && !PrisonBreakUtility.IsPrisonBreaking(pawn2))))
            {
                return false;
            }
            if ((a.Faction != null && a.Faction.IsPlayer && pawn2 != null && pawn2.mindState.WillJoinColonyIfRescued) || (b.Faction != null && b.Faction.IsPlayer && pawn != null && pawn.mindState.WillJoinColonyIfRescued))
            {
                return false;
            }
            if (pawn != null && pawn2 != null && (pawn.ThreatDisabledBecauseNonAggressiveRoamer(pawn2) || pawn2.ThreatDisabledBecauseNonAggressiveRoamer(pawn)))
            {
                return false;
            }
            if ((pawn != null && MechanitorUtility.IsPlayerOverseerSubject(pawn) && !pawn.IsColonyMechPlayerControlled) 
                || (pawn2 != null && MechanitorUtility.IsPlayerOverseerSubject(pawn2) && !pawn2.IsColonyMechPlayerControlled))
            {
                Log.Message("FAlse 1");
                return false;
            }
            if ((pawn != null && pawn.Faction == null && pawn.RaceProps.Humanlike && b.Faction != null && b.Faction.def.hostileToFactionlessHumanlikes) || (pawn2 != null && pawn2.Faction == null && pawn2.RaceProps.Humanlike && a.Faction != null && a.Faction.def.hostileToFactionlessHumanlikes))
            {
                return true;
            }
            if (a.Faction == null || b.Faction == null)
            {
                Log.Message("FAlse 2: " + a + " - " + b + " - " + a.Faction + " - " + b.Faction);
                return false;
            }
            var res = a.Faction.HostileTo(b.Faction);
            if (res is false)
            {
                Log.Message("FAlse 3");
            }
            return res;
        }


        public static List<IAttackTarget> GetPotentialTargetsFor(AttackTargetsCache cache, IAttackTargetSearcher th)
        {
            Thing thing = th.Thing;
            AttackTargetsCache.targets.Clear();
            Faction faction = thing.Faction;
            if (faction != null)
            {
                foreach (IAttackTarget item in cache.TargetsHostileToFaction(faction))
                {
                    if (thing.HostileTo(item.Thing))
                    {
                        AttackTargetsCache.targets.Add(item);
                    }
                    else
                    {
                        Log.Message(thing + " 1 is not hostile to " + item.Thing + " - " + thing.Faction + " - " + item.Thing.Faction);
                    }
                }
            }
            foreach (Pawn item2 in cache.pawnsInAggroMentalState)
            {
                if (thing.HostileTo(item2))
                {
                    AttackTargetsCache.targets.Add(item2);
                }
                else
                {
                    Log.Message(thing + " 2 is not hostile to " + item2);
                }
            }
            foreach (Pawn factionlessHumanlike in cache.factionlessHumanlikes)
            {
                if (thing.HostileTo(factionlessHumanlike))
                {
                    AttackTargetsCache.targets.Add(factionlessHumanlike);
                }
                else
                {
                    Log.Message(thing + " 3 is not hostile to " + factionlessHumanlike);
                }
            }
            Pawn pawn = th as Pawn;
            if (pawn != null && PrisonBreakUtility.IsPrisonBreaking(pawn))
            {
                Faction hostFaction = pawn.guest.HostFaction;
                List<Pawn> list = cache.map.mapPawns.SpawnedPawnsInFaction(hostFaction);
                for (int i = 0; i < list.Count; i++)
                {
                    if (thing.HostileTo(list[i]))
                    {
                        AttackTargetsCache.targets.Add(list[i]);
                    }
                    else
                    {
                        Log.Message(thing + " 4 is not hostile to " + list[i]);
                    }
                }
            }
            if (pawn != null && ModsConfig.IdeologyActive && SlaveRebellionUtility.IsRebelling(pawn))
            {
                Faction faction2 = pawn.Faction;
                List<Pawn> list2 = cache.map.mapPawns.SpawnedPawnsInFaction(faction2);
                for (int j = 0; j < list2.Count; j++)
                {
                    if (thing.HostileTo(list2[j]))
                    {
                        AttackTargetsCache.targets.Add(list2[j]);
                    }
                }
            }
            return AttackTargetsCache.targets;
        }

    }
}
