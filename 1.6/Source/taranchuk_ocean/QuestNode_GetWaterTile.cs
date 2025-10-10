using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace taranchuk_ocean
{
    public class QuestNode_GetWaterTile : QuestNode
    {
        [NoTranslate]
        public SlateRef<string> storeAs;

        public SlateRef<bool> preferCloserTiles;

        public SlateRef<bool> allowCaravans;

        public SlateRef<bool?> clampRangeBySiteParts;

        public SlateRef<IEnumerable<SitePartDef>> sitePartDefs;

        public override bool TestRunInt(Slate slate)
        {
            SettleInEmptyTileUtility_SettleCommand_Patch.lookingForWaterTile = true;
            var result = Inner(slate);
            SettleInEmptyTileUtility_SettleCommand_Patch.lookingForWaterTile = false;
            return result;
        }

        private bool Inner(Slate slate)
        {
            if (!TryFindTile(slate, out var tile))
            {
                return false;
            }
            if (clampRangeBySiteParts.GetValue(slate) == true && sitePartDefs.GetValue(slate) == null)
            {
                return false;
            }
            slate.Set(storeAs.GetValue(slate), tile);
            return true;
        }

        public override void RunInt()
        {
            SettleInEmptyTileUtility_SettleCommand_Patch.lookingForWaterTile = true;
            Slate slate = QuestGen.slate;
            if (TryFindTile(QuestGen.slate, out var tile))
            {
                QuestGen.slate.Set(storeAs.GetValue(slate), tile);
            }
            SettleInEmptyTileUtility_SettleCommand_Patch.lookingForWaterTile = false;
        }

        private bool TryFindTile(Slate slate, out PlanetTile tile)
        {
            PlanetTile nearThisTile = (slate.Get<Map>("map") ?? Find.RandomPlayerHomeMap)?.Tile ?? PlanetTile.Invalid;
            int num = int.MaxValue;
            bool? value = clampRangeBySiteParts.GetValue(slate);
            if (value.HasValue && value.Value)
            {
                foreach (SitePartDef item in sitePartDefs.GetValue(slate))
                {
                    if (item.conditionCauserDef != null)
                    {
                        num = Mathf.Min(num, item.conditionCauserDef.GetCompProperties<CompProperties_CausesGameCondition>().worldRange);
                    }
                }
            }
            if (!slate.TryGet<IntRange>("siteDistRange", out IntRange siteDistRange))
            {
                siteDistRange = new IntRange(7, 24);
            }
            TileFinderMode tileFinderMode = (preferCloserTiles.GetValue(slate) ? TileFinderMode.Near : TileFinderMode.Random);
            if (TryFindNewSiteTile(out tile, nearThisTile, siteDistRange.min, siteDistRange.max, allowCaravans.GetValue(slate), tileFinderMode) is false)
            {
                if (num != int.MaxValue)
                {
                    siteDistRange = new IntRange(Mathf.Min(siteDistRange.min, num), Mathf.Min(siteDistRange.max, num));
                }
                else
                {
                    siteDistRange = new IntRange(7, int.MaxValue);
                }
                return TryFindNewSiteTile(out tile, nearThisTile, siteDistRange.min, siteDistRange.max, allowCaravans.GetValue(slate), tileFinderMode); ;
            }
            return true;
        }

        public static bool TryFindNewSiteTile(out PlanetTile tile, int minDist = 7, int maxDist = 27, bool allowCaravans = false, TileFinderMode tileFinderMode = TileFinderMode.Near, bool exitOnFirstTileFound = false)
        {
            return TryFindNewSiteTile(out tile, PlanetTile.Invalid, minDist, maxDist, allowCaravans, tileFinderMode, exitOnFirstTileFound);
        }

        public static bool TryFindNewSiteTile(out PlanetTile tile, PlanetTile nearThisTile, int minDist = 7, int maxDist = 27, bool allowCaravans = false, TileFinderMode tileFinderMode = TileFinderMode.Near, bool exitOnFirstTileFound = false)
        {
            Func<PlanetTile, PlanetTile> findTile = delegate (PlanetTile root)
            {
                if (TryFindPassableTileWithTraversalDistance(root, minDist, maxDist, out var result, (PlanetTile x) =>
                !Find.WorldObjects.AnyWorldObjectAt(x) && TileFinder.IsValidTileForNewSettlement(x)
                && Find.WorldGrid[x].biome.IsWaterBiome(), ignoreFirstTilePassability: true, tileFinderMode, canTraverseImpassable: true, exitOnFirstTileFound))
                {
                    return result;
                }
                return PlanetTile.Invalid;
            };
            PlanetTile tile2;
            if (nearThisTile.Valid)
            {
                tile2 = nearThisTile;
            }
            else if (!TileFinder.TryFindRandomPlayerTile(out tile2, allowCaravans, (PlanetTile x) => findTile(x) != PlanetTile.Invalid))
            {
                tile = PlanetTile.Invalid;
                return false;
            }
            tile = findTile(tile2);
            return tile != PlanetTile.Invalid;
        }

        public static bool TryFindPassableTileWithTraversalDistance(PlanetTile rootTile, int minDist, int maxDist, out PlanetTile result, Predicate<PlanetTile> validator = null, bool ignoreFirstTilePassability = false, TileFinderMode tileFinderMode = TileFinderMode.Random, bool canTraverseImpassable = false, bool exitOnFirstTileFound = false)
        {
            TileFinder.tmpTiles.Clear();
            rootTile.Layer.Filler.FloodFill(rootTile, (PlanetTile x) => true, delegate(PlanetTile tile, int traversalDistance)
            {
                if (traversalDistance > maxDist)
                {
                    return true;
                }
                if (traversalDistance >= minDist && (validator == null || validator(tile)))
                {
                    TileFinder.tmpTiles.Add((tile, traversalDistance));
                    if (exitOnFirstTileFound)
                    {
                        return true;
                    }
                }
                return false;
            });
            if (exitOnFirstTileFound && TileFinder.tmpTiles.Count > 0)
            {
                result = TileFinder.tmpTiles[0].tile;
                return true;
            }
            (PlanetTile, int) result2;
            switch (tileFinderMode)
            {
            case TileFinderMode.Near:
                if (TileFinder.tmpTiles.TryRandomElementByWeight<(PlanetTile, int)>(((PlanetTile tile, int traversalDistance) x) => 1f - (float)(x.traversalDistance - minDist) / ((float)(maxDist - minDist) + 0.01f), out result2))
                {
                    (result, _) = result2;
                    return true;
                }
                result = PlanetTile.Invalid;
                return false;
            case TileFinderMode.Furthest:
                if (TileFinder.tmpTiles.Count > 0)
                {
                    int maxDistanceWithOffset = Mathf.Clamp(TileFinder.tmpTiles.MaxBy(((PlanetTile tile, int traversalDistance) t) => t.traversalDistance).traversalDistance - 2, minDist, maxDist);
                    if (TileFinder.tmpTiles.Where(((PlanetTile tile, int traversalDistance) t) => t.traversalDistance >= maxDistanceWithOffset - 1).TryRandomElement<(PlanetTile, int)>(out var result3))
                    {
                        (result, _) = result3;
                        return true;
                    }
                }
                result = PlanetTile.Invalid;
                return false;
            case TileFinderMode.Random:
                if (TileFinder.tmpTiles.TryRandomElement<(PlanetTile, int)>(out result2))
                {
                    (result, _) = result2;
                    return true;
                }
                result = PlanetTile.Invalid;
                return false;
            default:
                Log.Error($"Unknown tile distance preference {tileFinderMode}.");
                result = PlanetTile.Invalid;
                return false;
            }
        }
    }
}
