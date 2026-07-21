using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>
    /// 経路探索(A*)と移動範囲・攻撃対象の列挙。
    /// 占有タイルは通行不可。ただし目的地に敵がいる場合のみ allowEnemyGoal で許可(攻撃移動用)。
    /// 味方が占有するタイルは決して有効な目的地にならない。
    /// </summary>
    public static class Pathfinder
    {
        /// <summary>
        /// A* 経路探索。経路は開始タイルを含まず、目的地を含む。到達不能なら null。
        /// </summary>
        public static List<HexCoord> FindPath(GameState s, Unit u, HexCoord goal, bool allowEnemyGoal = false)
        {
            if (s == null || u == null || u.IsDead) return null;
            var map = s.Map;
            if (!map.InBounds(goal) || goal == u.Coord) return null;
            var goalTile = map.Get(goal);
            if (IsBlocked(u, goalTile, true, allowEnemyGoal)) return null;

            var open = new List<HexCoord>();
            var g = new Dictionary<HexCoord, int>();
            var f = new Dictionary<HexCoord, int>();
            var cameFrom = new Dictionary<HexCoord, HexCoord>();
            var closed = new HashSet<HexCoord>();

            g[u.Coord] = 0;
            f[u.Coord] = u.Coord.DistanceTo(goal);
            open.Add(u.Coord);

            int guard = map.Width * map.Height * 8;
            while (open.Count > 0 && guard-- > 0)
            {
                // 最小 f のノードを取り出す(決定論的:同値なら先頭優先)
                int bi = 0;
                for (int i = 1; i < open.Count; i++)
                    if (f[open[i]] < f[open[bi]]) bi = i;
                var cur = open[bi];
                open.RemoveAt(bi);

                if (cur == goal)
                    return Reconstruct(cameFrom, cur, u.Coord);

                closed.Add(cur);

                foreach (var t in map.NeighborsOf(cur))
                {
                    var n = t.Coord;
                    if (closed.Contains(n)) continue;
                    bool isGoal = n == goal;
                    if (IsBlocked(u, t, isGoal, allowEnemyGoal)) continue;
                    int stepCost = GameRules.MoveCostInto(t);
                    if (stepCost >= GameRules.ImpassableCost) continue;

                    int ng = g[cur] + stepCost;
                    if (!g.TryGetValue(n, out int old) || ng < old)
                    {
                        g[n] = ng;
                        f[n] = ng + n.DistanceTo(goal);
                        cameFrom[n] = cur;
                        if (!open.Contains(n)) open.Add(n);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// このターン中に到達できるタイル(coord → 累計コスト)。
        /// 移動力が残っていれば(コスト超過でも)進入できる Civ 式ルール。
        /// </summary>
        public static Dictionary<HexCoord, int> ReachableThisTurn(GameState s, Unit u)
        {
            var result = new Dictionary<HexCoord, int>();
            if (s == null || u == null || u.IsDead || u.MovesLeft <= 0) return result;

            var best = new Dictionary<HexCoord, int> { { u.Coord, 0 } };
            var frontier = new List<HexCoord> { u.Coord };

            while (frontier.Count > 0)
            {
                int bi = 0;
                for (int i = 1; i < frontier.Count; i++)
                    if (best[frontier[i]] < best[frontier[bi]]) bi = i;
                var cur = frontier[bi];
                frontier.RemoveAt(bi);

                int curCost = best[cur];
                // 移動力を使い切ったタイルからは先へ進めない
                if (curCost >= u.MovesLeft) continue;

                foreach (var t in s.Map.NeighborsOf(cur))
                {
                    if (IsBlocked(u, t, false, false)) continue;
                    int cost = curCost + GameRules.MoveCostInto(t);
                    if (best.TryGetValue(t.Coord, out int old) && old <= cost) continue;
                    best[t.Coord] = cost;
                    if (!frontier.Contains(t.Coord)) frontier.Add(t.Coord);
                }
            }

            foreach (var kv in best)
                if (kv.Key != u.Coord) result[kv.Key] = kv.Value;
            return result;
        }

        /// <summary>今すぐ攻撃できる敵ユニット/敵都市のあるタイル。</summary>
        public static List<HexCoord> AttackableTiles(GameState s, Unit u)
        {
            var result = new List<HexCoord>();
            if (s == null || u == null || u.IsDead || u.MovesLeft <= 0) return result;
            var def = u.Def;
            if (def.Strength <= 0 && def.RangedStrength <= 0) return result;

            int range = def.IsRanged ? def.Range : 1;
            if (range < 1) range = 1;
            foreach (var t in s.Map.TilesInRange(u.Coord, range))
            {
                if (t.Coord == u.Coord) continue;
                if (Combat.CanAttack(s, u, t)) result.Add(t.Coord);
            }
            return result;
        }

        /// <summary>
        /// タイルが通行不可か。占有タイルは常に不可。目的地に敵がいる場合のみ
        /// allowEnemyGoal で許可(攻撃は宣戦布告を伴うため和平中でも対象になり得る)。
        /// </summary>
        static bool IsBlocked(Unit u, Tile t, bool isGoal, bool allowEnemyGoal)
        {
            if (t == null || !t.IsPassable) return true;
            if (t.City != null && t.City.PlayerId != u.PlayerId)
                return !(isGoal && allowEnemyGoal);
            if (t.Unit != null && t.Unit != u)
            {
                if (t.Unit.PlayerId == u.PlayerId) return true;   // 味方は常に不可
                return !(isGoal && allowEnemyGoal);
            }
            return false;
        }

        static List<HexCoord> Reconstruct(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord cur, HexCoord start)
        {
            var path = new List<HexCoord> { cur };
            int guard = 100000;
            while (cur != start && cameFrom.TryGetValue(cur, out var prev) && guard-- > 0)
            {
                cur = prev;
                if (cur != start) path.Add(cur);
            }
            path.Reverse();
            return path;
        }
    }
}
