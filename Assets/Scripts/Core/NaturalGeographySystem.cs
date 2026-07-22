using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>手続き生成世界または文明圏に含まれる自然地理のタイル集計。</summary>
    public sealed class NaturalGeographyProfile
    {
        public int MountainTiles;
        public int RiverTiles;
        public int SeaTiles;
        public int LakeTiles;
        public int ForestTiles;
        public int DesertTiles;

        public int Diversity
        {
            get
            {
                int value = 0;
                if (MountainTiles > 0) value++;
                if (RiverTiles > 0) value++;
                if (SeaTiles > 0) value++;
                if (LakeTiles > 0) value++;
                if (ForestTiles > 0) value++;
                if (DesertTiles > 0) value++;
                return value;
            }
        }

        public int TotalRecordedTiles => MountainTiles + RiverTiles + SeaTiles + LakeTiles +
            ForestTiles + DesertTiles;
    }

    /// <summary>
    /// 地形から内陸湖・河川・自然多様性を決定論的に求める純Coreシステム。
    /// 実在地名は NaturalFeatureCatalog の図鑑だけに保持し、生成マップへ誤って割り当てない。
    /// </summary>
    public static class NaturalGeographySystem
    {
        /// <summary>
        /// 標高配列の局所低地から小さな内陸湖を作る。乱数を消費せず、同じ地形なら同じ結果になる。
        /// 周囲6タイルが陸地の候補だけを使うため、生成直後は外洋と接続しない。
        /// </summary>
        public static int GenerateInlandLakes(HexMap map, float[] elevation)
        {
            if (map == null || elevation == null || elevation.Length != map.Width * map.Height)
                return 0;

            var candidates = new List<Tile>();
            foreach (Tile tile in map.AllTiles)
            {
                tile.Coord.ToOffset(out int col, out int row);
                if (!tile.IsLand || col < 3 || row < 3 || col >= map.Width - 3 ||
                    row >= map.Height - 3) continue;
                int neighbors = 0;
                bool allLand = true;
                foreach (Tile neighbor in map.NeighborsOf(tile.Coord))
                {
                    neighbors++;
                    if (!neighbor.IsLand) { allLand = false; break; }
                }
                if (neighbors == 6 && allLand) candidates.Add(tile);
            }

            candidates.Sort((a, b) =>
            {
                a.Coord.ToOffset(out int ac, out int ar);
                b.Coord.ToOffset(out int bc, out int br);
                int cmp = elevation[ar * map.Width + ac].CompareTo(
                    elevation[br * map.Width + bc]);
                return cmp != 0 ? cmp : CompareCoord(a.Coord, b.Coord);
            });

            int target = Math.Clamp(map.Width * map.Height / 850, 1, 3);
            var selected = new List<HexCoord>();
            for (int i = 0; i < candidates.Count && selected.Count < target; i++)
            {
                Tile candidate = candidates[i];
                bool separated = true;
                for (int j = 0; j < selected.Count; j++)
                    if (selected[j].DistanceTo(candidate.Coord) < 7)
                    {
                        separated = false;
                        break;
                    }
                if (!separated) continue;
                candidate.Terrain = TerrainType.Coast;
                candidate.HasHill = false;
                candidate.HasForest = false;
                candidate.HasRiver = false;
                candidate.Resource = ResourceType.None;
                selected.Add(candidate.Coord);
            }
            return selected.Count;
        }

        /// <summary>
        /// 山麓（山が無い特殊マップでは丘陵）から最寄りの水域または既存河川へ河道を引く。
        /// 幅優先探索と座標順タイブレークだけを使い、乱数状態を変えない。
        /// </summary>
        public static int GenerateRivers(HexMap map)
        {
            if (map == null) return 0;
            foreach (Tile tile in map.AllTiles) tile.HasRiver = false;

            var sources = new List<Tile>();
            foreach (Tile tile in map.AllTiles)
                if (tile.Terrain == TerrainType.Mountain) sources.Add(tile);
            if (sources.Count == 0)
                foreach (Tile tile in map.AllTiles)
                    if (tile.HasHill && tile.IsPassable) sources.Add(tile);
            sources.Sort((a, b) => CompareCoord(a.Coord, b.Coord));

            int target = Math.Clamp(map.Width * map.Height / 700, 1, 4);
            var usedSources = new List<HexCoord>();
            int riverTiles = 0;
            for (int i = 0; i < sources.Count && usedSources.Count < target; i++)
            {
                Tile source = sources[i];
                bool separated = true;
                for (int j = 0; j < usedSources.Count; j++)
                    if (usedSources[j].DistanceTo(source.Coord) < 7)
                    {
                        separated = false;
                        break;
                    }
                if (!separated) continue;

                int added = RouteRiver(map, source);
                if (added <= 0) continue;
                riverTiles += added;
                usedSources.Add(source.Coord);
            }
            return riverTiles;
        }

        static int RouteRiver(HexMap map, Tile mountainOrHill)
        {
            var starts = SortedNeighbors(map, mountainOrHill.Coord);
            var queue = new Queue<HexCoord>();
            var visited = new HashSet<HexCoord>();
            var previous = new Dictionary<HexCoord, HexCoord>();
            for (int i = 0; i < starts.Count; i++)
            {
                Tile tile = starts[i];
                if (!tile.IsPassable || !visited.Add(tile.Coord)) continue;
                queue.Enqueue(tile.Coord);
            }
            if (queue.Count == 0) return 0;

            HexCoord endpoint = default(HexCoord);
            bool found = false;
            while (queue.Count > 0)
            {
                HexCoord current = queue.Dequeue();
                Tile currentTile = map.Get(current);
                if (currentTile == null) continue;
                if (currentTile.HasRiver || AdjacentToWater(map, current))
                {
                    endpoint = current;
                    found = true;
                    break;
                }

                var neighbors = SortedNeighbors(map, current);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    Tile next = neighbors[i];
                    if (!next.IsPassable || !visited.Add(next.Coord)) continue;
                    previous[next.Coord] = current;
                    queue.Enqueue(next.Coord);
                }
            }
            if (!found) return 0;

            int added = 0;
            HexCoord cursor = endpoint;
            while (true)
            {
                Tile tile = map.Get(cursor);
                if (tile != null && tile.IsPassable && !tile.HasRiver)
                {
                    tile.HasRiver = true;
                    added++;
                }
                HexCoord parent;
                if (!previous.TryGetValue(cursor, out parent)) break;
                cursor = parent;
            }
            return added;
        }

        static bool AdjacentToWater(HexMap map, HexCoord coord)
        {
            foreach (Tile tile in map.NeighborsOf(coord)) if (tile.IsWater) return true;
            return false;
        }

        static List<Tile> SortedNeighbors(HexMap map, HexCoord coord)
        {
            var result = new List<Tile>();
            foreach (Tile tile in map.NeighborsOf(coord)) result.Add(tile);
            result.Sort((a, b) => CompareCoord(a.Coord, b.Coord));
            return result;
        }

        /// <summary>外縁へ接続しない水域を内陸湖として返す。</summary>
        public static HashSet<HexCoord> FindLakeTiles(HexMap map)
        {
            var lakes = new HashSet<HexCoord>();
            if (map == null) return lakes;
            var visited = new HashSet<HexCoord>();
            foreach (Tile seed in map.AllTiles)
            {
                if (!seed.IsWater || !visited.Add(seed.Coord)) continue;
                bool touchesBorder = false;
                var component = new List<HexCoord>();
                var queue = new Queue<HexCoord>();
                queue.Enqueue(seed.Coord);
                while (queue.Count > 0)
                {
                    HexCoord current = queue.Dequeue();
                    component.Add(current);
                    current.ToOffset(out int col, out int row);
                    if (col == 0 || row == 0 || col == map.Width - 1 || row == map.Height - 1)
                        touchesBorder = true;
                    foreach (Tile neighbor in map.NeighborsOf(current))
                        if (neighbor.IsWater && visited.Add(neighbor.Coord))
                            queue.Enqueue(neighbor.Coord);
                }
                if (!touchesBorder)
                    for (int i = 0; i < component.Count; i++) lakes.Add(component[i]);
            }
            return lakes;
        }

        public static NaturalGeographyProfile WorldProfile(HexMap map)
        {
            return Profile(map, null);
        }

        /// <summary>領土と都市半径2を合わせ、文明が実際に接する自然環境を集計する。</summary>
        public static NaturalGeographyProfile PlayerProfile(GameState state, Player player)
        {
            if (state == null || state.Map == null || player == null)
                return new NaturalGeographyProfile();
            var relevant = new HashSet<HexCoord>();
            foreach (Tile tile in state.Map.AllTiles)
                if (tile.OwnerPlayerId == player.Id) relevant.Add(tile.Coord);
            for (int i = 0; i < player.Cities.Count; i++)
                foreach (Tile tile in state.Map.TilesInRange(player.Cities[i].Coord, 2))
                    relevant.Add(tile.Coord);
            return Profile(state.Map, relevant);
        }

        static NaturalGeographyProfile Profile(HexMap map, HashSet<HexCoord> relevant)
        {
            var result = new NaturalGeographyProfile();
            if (map == null) return result;
            HashSet<HexCoord> lakes = FindLakeTiles(map);
            foreach (Tile tile in map.AllTiles)
            {
                if (relevant != null && !relevant.Contains(tile.Coord)) continue;
                if (tile.Terrain == TerrainType.Mountain) result.MountainTiles++;
                if (tile.HasRiver) result.RiverTiles++;
                if (tile.IsWater)
                {
                    if (lakes.Contains(tile.Coord)) result.LakeTiles++;
                    else result.SeaTiles++;
                }
                if (tile.HasForest) result.ForestTiles++;
                if (tile.Terrain == TerrainType.Desert) result.DesertTiles++;
            }
            return result;
        }

        /// <summary>河港・海港・湖港に相当する都市立地を市場アクセスへ反映する（最大+10）。</summary>
        public static int MarketAccessBonus(GameState state, Player player)
        {
            if (state == null || state.Map == null || player == null) return 0;
            int bonus = 0;
            for (int i = 0; i < player.Cities.Count; i++)
            {
                Tile center = state.Map.Get(player.Cities[i].Coord);
                bool river = center != null && center.HasRiver;
                bool water = false;
                foreach (Tile neighbor in state.Map.NeighborsOf(player.Cities[i].Coord))
                {
                    river |= neighbor.HasRiver;
                    water |= neighbor.IsWater;
                }
                if (river) bonus += 2;
                if (water) bonus += 2;
            }
            return Math.Min(10, bonus);
        }

        public static int ScienceBonus(GameState state, Player player)
        {
            return Math.Min(2, PlayerProfile(state, player).Diversity / 3);
        }

        public static int CultureBonus(GameState state, Player player)
        {
            int diversity = PlayerProfile(state, player).Diversity;
            return diversity >= 5 ? 2 : diversity >= 3 ? 1 : 0;
        }

        static int CompareCoord(HexCoord a, HexCoord b)
        {
            int cmp = a.r.CompareTo(b.r);
            return cmp != 0 ? cmp : a.q.CompareTo(b.q);
        }
    }
}
