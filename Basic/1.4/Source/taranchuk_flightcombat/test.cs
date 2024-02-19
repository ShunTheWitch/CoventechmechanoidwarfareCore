using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;
using Verse.AI;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    [HarmonyPatch(typeof(TargetingHelper), "TryGetTarget")]
    public static class test
    {
        public static bool Prefix(ref bool __result, VehicleTurret turret, ref LocalTargetInfo targetInfo, TargetingParameters param = null)
        {
            __result = TryGetTarget(turret, out targetInfo, param);
            Log.Message(targetInfo + " - " + turret.vehicle + " - __result: " + __result);
            return false;
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
                    Log.Message(thing + " - false 10: " + t.ThreatDisabled(searcherPawn));
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
            tmpTargets.AddRange(searcherPawn.Map.attackTargetsCache.GetPotentialTargetsFor(searcherPawn));
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
    }
}
