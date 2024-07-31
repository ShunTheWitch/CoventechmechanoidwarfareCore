using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;

namespace taranchuk_flightcombat
{
    [HotSwappable]
    public class PawnGroupMaker_FlightRaid : PawnGroupMaker
    {
        public float? minPoints;

        public int? minAircraftCount;

        public int? maxAircraftCount;
        [HotSwappable]
        [HarmonyPatch(typeof(PawnGroupMaker), "GeneratePawns")]
        public static class PawnGroupMaker_GeneratePawns_Patch
        {
            public static List<List<Pawn>> pawnsBeingGeneratedNow = new List<List<Pawn>>();

            public static bool Prefix(ref IEnumerable<Pawn> __result, PawnGroupMaker __instance, PawnGroupMakerParms parms, bool errorOnZeroResults = true)
            {
                if (__instance is PawnGroupMaker_FlightRaid flightRaid)
                {
                    __result = GeneratePawnsOverride(flightRaid, parms, __instance, errorOnZeroResults);
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public static List<Pawn> GeneratePawnsOverride(PawnGroupMaker_FlightRaid flightRaid, PawnGroupMakerParms parms, PawnGroupMaker groupMaker, bool errorOnZeroResults = true)
            {
                List<Pawn> list = new List<Pawn>();
                pawnsBeingGeneratedNow.Add(list);
                try
                {
                    GeneratePawns(flightRaid, parms, groupMaker, list, errorOnZeroResults);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception while generating pawn group: " + ex);
                    for (int i = 0; i < list.Count; i++)
                    {
                        list[i].Destroy();
                    }
                    list.Clear();
                }
                finally
                {
                    pawnsBeingGeneratedNow.Remove(list);
                }
                return list;
            }

            private static void GeneratePawns(PawnGroupMaker_FlightRaid __instance, PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns, bool errorOnZeroResults = true)
            {
                if (!__instance.kindDef.Worker.CanGenerateFrom(parms, groupMaker))
                {
                    if (errorOnZeroResults)
                    {
                        Log.Error(string.Concat("Cannot generate pawns for ", parms.faction, " with ", parms.points, ". Defaulting to a single random cheap group."));
                    }
                    return;
                }

                if (__instance.minAircraftCount.HasValue)
                {
                    for (var i = 0; i < __instance.minAircraftCount.Value; i++)
                    {
                        var flyingVehicle = groupMaker.options.Where(x => IsFlyingVehicle(x.kind)).RandomElementByWeight(x => x.selectionWeight);
                        PawnGenerationRequest request = new PawnGenerationRequest(flyingVehicle.kind, parms.faction, PawnGenerationContext.NonPlayer, fixedIdeo: parms.ideo, forcedXenotype: null, tile: parms.tile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: true, allowFood: false, allowAddictions: false, inhabitant: parms.inhabitants, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, biocodeWeaponChance: 0f, biocodeApparelChance: 0f, extraPawnForExtraRelationChance: null, relationWithExtraPawnChanceFactor: 1f, validatorPreGear: null, validatorPostGear: null);
                        Pawn pawn = PawnGenerator.GeneratePawn(request);
                        outPawns.Add(pawn);
                    }
                }
                bool allowFood = parms.raidStrategy == null || parms.raidStrategy.pawnsCanBringFood || (parms.faction != null && !parms.faction.HostileTo(Faction.OfPlayer));
                Predicate<Pawn> validatorPostGear = ((parms.raidStrategy != null) ? ((Predicate<Pawn>)((Pawn p) => parms.raidStrategy.Worker.CanUsePawn(parms.points, p, outPawns))) : null);
                bool flag = false;
                foreach (PawnGenOptionWithXenotype item in ChoosePawnGenOptionsByPoints(__instance,parms.points, outPawns, groupMaker.options, parms))
                {
                    PawnGenerationRequest request = new PawnGenerationRequest(item.Option.kind, parms.faction, PawnGenerationContext.NonPlayer, fixedIdeo: parms.ideo, forcedXenotype: item.Xenotype, tile: parms.tile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: true, allowFood: allowFood, allowAddictions: true, inhabitant: parms.inhabitants, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, biocodeWeaponChance: 0f, biocodeApparelChance: 0f, extraPawnForExtraRelationChance: null, relationWithExtraPawnChanceFactor: 1f, validatorPreGear: null, validatorPostGear: validatorPostGear);
                    if (parms.raidAgeRestriction != null && parms.raidAgeRestriction.Worker.ShouldApplyToKind(item.Option.kind))
                    {
                        request.BiologicalAgeRange = parms.raidAgeRestriction.ageRange;
                        request.AllowedDevelopmentalStages = parms.raidAgeRestriction.developmentStage;
                    }
                    if (item.Option.kind.pawnGroupDevelopmentStage.HasValue)
                    {
                        request.AllowedDevelopmentalStages = item.Option.kind.pawnGroupDevelopmentStage.Value;
                    }
                    if (!Find.Storyteller.difficulty.ChildRaidersAllowed && parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer))
                    {
                        request.AllowedDevelopmentalStages = DevelopmentalStage.Adult;
                    }
                    Pawn pawn = PawnGenerator.GeneratePawn(request);
                    if (parms.forceOneDowned && !flag)
                    {
                        pawn.health.forceDowned = true;
                        if (pawn.guest != null)
                        {
                            pawn.guest.Recruitable = true;
                        }
                        pawn.mindState.canFleeIndividual = false;
                        flag = true;
                    }
                    outPawns.Add(pawn);
                }
            }

            private static bool IsFlyingVehicle(PawnKindDef x)
            {
                return x.race is VehicleDef && x.race.HasComp(typeof(CompFlightMode));
            }

            public static IEnumerable<PawnGenOptionWithXenotype> ChoosePawnGenOptionsByPoints(PawnGroupMaker_FlightRaid __instance, float pointsTotal, List<Pawn> outPawns, List<PawnGenOption> options, PawnGroupMakerParms groupParms)
            {
                if (groupParms.seed.HasValue)
                {
                    Rand.PushState(groupParms.seed.Value);
                }
                List<PawnGenOptionWithXenotype> list = new List<PawnGenOptionWithXenotype>();
                List<PawnGenOptionWithXenotype> chosenOptions = new List<PawnGenOptionWithXenotype>();
                float num = pointsTotal;
                bool leaderChosen = false;
                float highestCost = -1f;
                while (true)
                {
                    list.Clear();
                    foreach (PawnGenOptionWithXenotype option in PawnGroupMakerUtility.GetOptions(groupParms, groupParms.faction.def, options, pointsTotal, num, null, chosenOptions, leaderChosen))
                    {
                        if (IsFlyingVehicle(option.Option.kind) && __instance.maxAircraftCount.HasValue)
                        {
                            int flyingVehiclesCount = (outPawns.Where(x => IsFlyingVehicle(x.kindDef)).Count()
                                                            + chosenOptions.Where(x => IsFlyingVehicle(x.option.kind)).Count());
                            if (flyingVehiclesCount >= __instance.maxAircraftCount)
                            {
                                continue;
                            }
                        }
                        if (!(option.Cost > num))
                        {
                            if (option.Cost > highestCost)
                            {
                                highestCost = option.Cost;
                            }
                            list.Add(option);
                        }
                    }
                    Func<PawnGenOptionWithXenotype, float> weightSelector = (PawnGenOptionWithXenotype gr) => (!PawnGroupMakerUtility.PawnGenOptionValid(gr.Option, groupParms, chosenOptions)) ? 0f : (gr.SelectionWeight * PawnGroupMakerUtility.PawnWeightFactorByMostExpensivePawnCostFractionCurve.Evaluate(gr.Cost / highestCost));
                    if (!list.TryRandomElementByWeight(weightSelector, out var result))
                    {
                        break;
                    }
                    chosenOptions.Add(result);
                    num -= result.Cost;
                    if (result.Option.kind.factionLeader)
                    {
                        leaderChosen = true;
                    }
                }
                list.Clear();
                if (chosenOptions.Count == 1 && num > pointsTotal / 2f)
                {
                    Log.Warning("Used only " + (pointsTotal - num) + " / " + pointsTotal + " points generating for " + groupParms.faction);
                }
                if (groupParms.seed.HasValue)
                {
                    Rand.PopState();
                }
                return chosenOptions;
            }
        }

        [HarmonyPatch(typeof(PawnGroupMaker), "CanGenerateFrom")]
        public static class PawnGroupMaker_CanGenerateFrom_Patch
        {
            public static void Postfix(PawnGroupMaker __instance, ref bool __result, PawnGroupMakerParms parms)
            {
                if (__instance is PawnGroupMaker_FlightRaid flightRaid)
                {
                    if (flightRaid.minPoints.HasValue && parms.points < flightRaid.minPoints || parms.raidStrategy.Worker is not RaidStrategyWorker_FlightCombatAttack)
                    {
                        __result = false;
                    }
                }
            }
        }
    }
}
