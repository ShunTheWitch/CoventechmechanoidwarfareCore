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

        private bool TryFindTile(Slate slate, out int tile)
        {
            int nearThisTile = (slate.Get<Map>("map") ?? Find.RandomPlayerHomeMap)?.Tile ?? (-1);
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
            if (!slate.TryGet<IntRange>("siteDistRange", out var var))
            {
                var = new IntRange(7, 24);
            }
            TileFinderMode tileFinderMode = (preferCloserTiles.GetValue(slate) ? TileFinderMode.Near : TileFinderMode.Random);
            if (TryFindNewSiteTile(out tile, var.min, var.max, allowCaravans.GetValue(slate), tileFinderMode, nearThisTile) is false)
            {
                if (num != int.MaxValue)
                {
                    var = new IntRange(Mathf.Min(var.min, num), Mathf.Min(var.max, num));
                }
                else
                {
                    var = new IntRange(7, int.MaxValue);
                }
                return TryFindNewSiteTile(out tile, var.min, var.max, allowCaravans.GetValue(slate), tileFinderMode, nearThisTile); ;
            }
            return true;
        }

        public static bool TryFindNewSiteTile(out int tile, int minDist = 7, int maxDist = 27, bool allowCaravans = false, TileFinderMode tileFinderMode = TileFinderMode.Near, int nearThisTile = -1, bool exitOnFirstTileFound = false)
        {
            Func<int, int> findTile = delegate (int root)
            {
                if (TryFindPassableTileWithTraversalDistance(root, minDist, maxDist, out var result, (int x) => 
                !Find.WorldObjects.AnyWorldObjectAt(x) && TileFinder.IsValidTileForNewSettlement(x) 
                && Find.WorldGrid[x].biome.IsWaterBiome(), ignoreFirstTilePassability: true, tileFinderMode, canTraverseImpassable: true, exitOnFirstTileFound))
                {
                    return result;
                }
                return -1;
            };
            int tile2;
            if (nearThisTile != -1)
            {
                tile2 = nearThisTile;
            }
            else if (!TileFinder.TryFindRandomPlayerTile(out tile2, allowCaravans, (int x) => findTile(x) != -1))
            {
                tile = -1;
                return false;
            }
            tile = findTile(tile2);
            return tile != -1;
        }

        public static bool TryFindPassableTileWithTraversalDistance(int rootTile, int minDist, int maxDist, out int result, Predicate<int> validator = null, bool ignoreFirstTilePassability = false, TileFinderMode tileFinderMode = TileFinderMode.Random, bool canTraverseImpassable = false, bool exitOnFirstTileFound = false)
        {
            TileFinder.tmpTiles.Clear();
            Find.WorldFloodFiller.FloodFill(rootTile, (int x) => true, 
                delegate (int tile, int traversalDistance)
            {
                if (traversalDistance > maxDist)
                {
                    return true;
                }
                if (traversalDistance >= minDist && (validator == null || validator(tile)))
                {
                    TileFinder.tmpTiles.Add(new Pair<int, int>(tile, traversalDistance));
                    if (exitOnFirstTileFound)
                    {
                        return true;
                    }
                }
                return false;
            });
            if (exitOnFirstTileFound)
            {
                if (TileFinder.tmpTiles.Count > 0)
                {
                    result = TileFinder.tmpTiles[0].First;
                    return true;
                }
                result = -1;
                return false;
            }
            Pair<int, int> result2;
            switch (tileFinderMode)
            {
                case TileFinderMode.Near:
                    if (TileFinder.tmpTiles.TryRandomElementByWeight((Pair<int, int> x) => 1f - (float)(x.Second - minDist) / ((float)(maxDist - minDist) + 0.01f), out result2))
                    {
                        result = result2.First;
                        return true;
                    }
                    result = -1;
                    return false;
                case TileFinderMode.Furthest:
                    if (TileFinder.tmpTiles.Count > 0)
                    {
                        int maxDistanceWithOffset = Mathf.Clamp(TileFinder.tmpTiles.MaxBy((Pair<int, int> t) => t.Second).Second - 2, minDist, maxDist);
                        if (TileFinder.tmpTiles.Where((Pair<int, int> t) => t.Second >= maxDistanceWithOffset - 1).TryRandomElement(out var result3))
                        {
                            result = result3.First;
                            return true;
                        }
                    }
                    result = -1;
                    return false;
                case TileFinderMode.Random:
                    if (TileFinder.tmpTiles.TryRandomElement(out result2))
                    {
                        result = result2.First;
                        return true;
                    }
                    result = -1;
                    return false;
                default:
                    Log.Error($"Unknown tile distance preference {tileFinderMode}.");
                    result = -1;
                    return false;
            }
        }
    }
}
