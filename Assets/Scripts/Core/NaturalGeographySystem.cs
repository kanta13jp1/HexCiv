using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>12ターンの河川季節サイクルにおける氾濫原の状態。</summary>
    public enum FloodStage
    {
        Normal = 0,
        Inundated = 1,
        Fertile = 2,
    }

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
                candidate.RiverOutflowDirection = -1;
                candidate.HasFloodplain = false;
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
            foreach (Tile tile in map.AllTiles)
            {
                tile.HasRiver = false;
                tile.RiverOutflowDirection = -1;
                tile.HasFloodplain = false;
            }

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
            GenerateFloodplains(map);
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

            var path = new List<HexCoord>();
            HexCoord cursor = endpoint;
            while (true)
            {
                path.Add(cursor);
                HexCoord parent;
                if (!previous.TryGetValue(cursor, out parent)) break;
                cursor = parent;
            }
            path.Reverse();

            int added = 0;
            for (int i = 0; i < path.Count; i++)
            {
                Tile tile = map.Get(path[i]);
                if (tile == null || !tile.IsPassable) continue;
                bool joinedExistingRiver = tile.HasRiver;
                if (!joinedExistingRiver)
                {
                    tile.HasRiver = true;
                    added++;
                }

                // 既存河川へ合流した末端は、その河川が既に持つ流向を保つ。
                if (joinedExistingRiver && i == path.Count - 1) continue;
                if (i + 1 < path.Count)
                    tile.RiverOutflowDirection = DirectionIndex(path[i], path[i + 1]);
                else
                    tile.RiverOutflowDirection = DirectionToFirstWater(map, tile.Coord);
            }
            return added;
        }

        /// <summary>
        /// version 15以前の保存データなど、河道だけがある地図へ決定的な流向と氾濫原を補う。
        /// 河口から河川グラフを逆向きに幅優先探索し、各タイルを一つ下流の隣接タイルへ向ける。
        /// </summary>
        public static void RebuildRiverMetadata(HexMap map)
        {
            if (map == null) return;
            var distance = new Dictionary<HexCoord, int>();
            var queue = new Queue<HexCoord>();
            foreach (Tile tile in map.AllTiles)
            {
                tile.RiverOutflowDirection = -1;
                tile.HasFloodplain = false;
                if (!tile.HasRiver) continue;
                int waterDirection = DirectionToFirstWater(map, tile.Coord);
                if (waterDirection < 0) continue;
                tile.RiverOutflowDirection = waterDirection;
                distance[tile.Coord] = 0;
                queue.Enqueue(tile.Coord);
            }

            while (queue.Count > 0)
            {
                HexCoord current = queue.Dequeue();
                int nextDistance = distance[current] + 1;
                var neighbors = SortedNeighbors(map, current);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    Tile neighbor = neighbors[i];
                    if (!neighbor.HasRiver || distance.ContainsKey(neighbor.Coord)) continue;
                    distance[neighbor.Coord] = nextDistance;
                    queue.Enqueue(neighbor.Coord);
                }
            }

            foreach (Tile tile in map.AllTiles)
            {
                if (!tile.HasRiver || tile.RiverOutflowDirection >= 0) continue;
                if (!distance.TryGetValue(tile.Coord, out int currentDistance)) continue;
                var neighbors = SortedNeighbors(map, tile.Coord);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    Tile neighbor = neighbors[i];
                    if (neighbor.HasRiver && distance.TryGetValue(neighbor.Coord, out int d) &&
                        d == currentDistance - 1)
                    {
                        tile.RiverOutflowDirection = DirectionIndex(tile.Coord, neighbor.Coord);
                        break;
                    }
                }
            }
            GenerateFloodplains(map);
        }

        /// <summary>丘陵・森林を除く平原と砂漠の河道を肥沃な氾濫原として扱う。</summary>
        public static int GenerateFloodplains(HexMap map)
        {
            if (map == null) return 0;
            int count = 0;
            foreach (Tile tile in map.AllTiles)
            {
                tile.HasFloodplain = tile.HasRiver && !tile.HasHill && !tile.HasForest &&
                    (tile.Terrain == TerrainType.Plains || tile.Terrain == TerrainType.Desert);
                if (tile.HasFloodplain) count++;
            }
            return count;
        }

        public static Tile RiverDestination(HexMap map, Tile tile)
        {
            if (map == null || tile == null || !tile.HasRiver ||
                tile.RiverOutflowDirection < 0 || tile.RiverOutflowDirection >= 6) return null;
            return map.Get(tile.Coord.Neighbor(tile.RiverOutflowDirection));
        }

        public static bool IsRiverSegment(HexMap map, HexCoord a, HexCoord b)
        {
            if (map == null || a.DistanceTo(b) != 1) return false;
            Tile first = map.Get(a);
            Tile second = map.Get(b);
            if (first == null || second == null || !first.HasRiver || !second.HasRiver) return false;
            Tile firstDownstream = RiverDestination(map, first);
            Tile secondDownstream = RiverDestination(map, second);
            return (firstDownstream != null && firstDownstream.Coord == b) ||
                (secondDownstream != null && secondDownstream.Coord == a);
        }

        /// <summary>
        /// 河道タイルへ出入りするが、河川の流路そのものに沿わない一歩を渡河とみなす。
        /// タイル中心線で河川を表す現在の地図モデルに合わせた抽象化。
        /// </summary>
        public static bool CrossesRiver(HexMap map, HexCoord from, HexCoord to)
        {
            if (map == null || from.DistanceTo(to) != 1) return false;
            Tile a = map.Get(from);
            Tile b = map.Get(to);
            if (a == null || b == null || (!a.HasRiver && !b.HasRiver)) return false;
            return !IsRiverSegment(map, from, to);
        }

        /// <summary>
        /// 12ターン周期の氾濫季。最初の2ターンは増水、次の3ターンは退水後の肥沃期、
        /// 残り7ターンは平常とする。乱数や保存項目を増やさない決定論的な季節モデル。
        /// </summary>
        public static FloodStage FloodStageAt(int turnNumber)
        {
            int phase = (turnNumber - 1) % 12;
            if (phase < 0) phase += 12;
            if (phase < 2) return FloodStage.Inundated;
            if (phase < 5) return FloodStage.Fertile;
            return FloodStage.Normal;
        }

        /// <summary>季節効果を含むタイル産出。恒久的な氾濫原+1食料の上に季節差を加える。</summary>
        public static Yields YieldsAt(GameState state, Tile tile)
        {
            if (tile == null) return new Yields(0, 0, 0);
            Yields yields = tile.GetYields();
            if (!tile.HasFloodplain || state == null) return yields;

            switch (FloodStageAt(state.TurnNumber))
            {
                case FloodStage.Inundated:
                    yields.Food = Math.Max(0, yields.Food - 1);
                    break;
                case FloodStage.Fertile:
                    yields.Food += 1;
                    break;
            }
            return yields;
        }

        /// <summary>都市労働圏内に河道があり、橋梁網を建設できるか。</summary>
        public static bool HasRiverInRange(HexMap map, HexCoord center, int range)
        {
            if (map == null) return false;
            foreach (Tile tile in map.TilesInRange(center, range))
                if (tile.HasRiver) return true;
            return false;
        }

        /// <summary>指定河道が文明の橋梁網で覆われているか。</summary>
        public static bool HasBridgeAtTile(GameState state, Player player, HexCoord riverCoord)
        {
            if (state == null || state.Map == null || player == null) return false;
            Tile river = state.Map.Get(riverCoord);
            if (river == null || !river.HasRiver) return false;
            for (int i = 0; i < player.Cities.Count; i++)
            {
                City city = player.Cities[i];
                if (city == null || city.Buildings == null ||
                    !city.Buildings.Contains("bridgeworks")) continue;
                if (city.Coord.DistanceTo(riverCoord) <= GameRules.CityWorkRadius)
                    return true;
            }
            return false;
        }

        /// <summary>渡河地点を覆う橋梁網の有無。河道側のタイルを橋の設置地点とする。</summary>
        public static bool HasBridgeCoverage(GameState state, Player player, HexCoord from, HexCoord to)
        {
            if (state == null || state.Map == null || player == null ||
                !CrossesRiver(state.Map, from, to)) return false;
            Tile a = state.Map.Get(from);
            Tile b = state.Map.Get(to);
            if (a != null && a.HasRiver && HasBridgeAtTile(state, player, a.Coord)) return true;
            return b != null && b.HasRiver && HasBridgeAtTile(state, player, b.Coord);
        }

        /// <summary>表示用に河道を覆う橋梁網の所有文明を決定的に返す。</summary>
        public static Player BridgeOwnerAt(GameState state, HexCoord riverCoord)
        {
            if (state == null) return null;
            Player best = null;
            for (int i = 0; i < state.Players.Count; i++)
            {
                Player player = state.Players[i];
                if (!HasBridgeAtTile(state, player, riverCoord)) continue;
                if (best == null || player.Id < best.Id) best = player;
            }
            return best;
        }

        public static int MovementCost(GameState state, Unit unit, HexCoord from, HexCoord to)
        {
            Tile destination = state != null && state.Map != null ? state.Map.Get(to) : null;
            int cost = GameRules.MoveCostInto(destination);
            if (cost >= GameRules.ImpassableCost || unit == null || state == null) return cost;
            Player owner = state.GetPlayer(unit.PlayerId);
            bool crossesRiver = CrossesRiver(state.Map, from, to);
            bool hasBridge = crossesRiver && HasBridgeCoverage(state, owner, from, to);
            if (crossesRiver && !hasBridge)
                cost += GameRules.RiverCrossingMovePenalty;
            if (destination != null && destination.HasFloodplain &&
                FloodStageAt(state.TurnNumber) == FloodStage.Inundated && !hasBridge)
                cost += GameRules.FloodedMovePenalty;
            return cost;
        }

        public static float RiverCrossingAttackMultiplier(GameState state, Unit attacker, HexCoord target)
        {
            if (state == null || attacker == null || attacker.Def.IsRanged) return 1f;
            Player owner = state.GetPlayer(attacker.PlayerId);
            return CrossesRiver(state.Map, attacker.Coord, target) &&
                !HasBridgeCoverage(state, owner, attacker.Coord, target)
                ? GameRules.RiverCrossingAttackMultiplier
                : 1f;
        }

        public static bool IsWaterfront(HexMap map, HexCoord coord)
        {
            if (map == null) return false;
            foreach (Tile neighbor in map.NeighborsOf(coord))
                if (neighbor.IsWater) return true;
            return false;
        }

        public static Tile FirstAdjacentWater(HexMap map, HexCoord coord)
        {
            if (map == null) return null;
            var neighbors = SortedNeighbors(map, coord);
            for (int i = 0; i < neighbors.Count; i++)
                if (neighbors[i].IsWater) return neighbors[i];
            return null;
        }

        static int DirectionToFirstWater(HexMap map, HexCoord coord)
        {
            for (int direction = 0; direction < 6; direction++)
            {
                Tile tile = map.Get(coord.Neighbor(direction));
                if (tile != null && tile.IsWater) return direction;
            }
            return -1;
        }

        static int DirectionIndex(HexCoord from, HexCoord to)
        {
            for (int direction = 0; direction < 6; direction++)
                if (from.Neighbor(direction) == to) return direction;
            return -1;
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
                if (LogisticsSystem.HasHarbor(player.Cities[i])) bonus += 2;
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
