using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>ターン開始時に確定するユニットの補給状態。</summary>
    public enum SupplyLevel
    {
        Supplied = 0,
        Strained = 1,
        Isolated = 2,
    }

    /// <summary>
    /// 都市から地形コスト付きで補給を伝播する純粋シミュレーション。
    /// 乱数を使わず、都市・技術・建物・領有・敵遮断だけから決定的に算出する。
    /// </summary>
    public static class LogisticsSystem
    {
        public const int BaseSupplyRange = 6;
        public const int WheelRangeBonus = 2;
        public const int ConstructionRangeBonus = 1;
        public const int GranarySourceBonus = 2;
        public const int StrainedMargin = 4;
        public const int AttritionGraceTurns = 1;
        public const int AttritionDamage = 5;

        /// <summary>技術による文明全体の補給到達距離。</summary>
        public static int SupplyRange(Player player)
        {
            if (player == null) return BaseSupplyRange;
            int range = BaseSupplyRange;
            if (player.HasTech("wheel")) range += WheelRangeBonus;
            if (player.HasTech("construction")) range += ConstructionRangeBonus;
            return range;
        }

        /// <summary>
        /// 全都市を起点にした最小補給コスト。穀物庫のある都市は強化補給拠点として
        /// 初期コストを低くする。敵部隊・敵都市のいるタイルは補給を遮断する。
        /// </summary>
        public static Dictionary<HexCoord, int> BuildSupplyCosts(GameState state, Player player)
        {
            var costs = new Dictionary<HexCoord, int>();
            if (state == null || state.Map == null || player == null || player.IsEliminated)
                return costs;

            var open = new List<SupplyNode>();
            for (int i = 0; i < player.Cities.Count; i++)
            {
                City city = player.Cities[i];
                if (city == null || state.Map.Get(city.Coord) == null) continue;
                int sourceCost = city.Buildings != null && city.Buildings.Contains("granary")
                    ? -GranarySourceBonus
                    : 0;
                if (!costs.TryGetValue(city.Coord, out int old) || sourceCost < old)
                {
                    costs[city.Coord] = sourceCost;
                    Push(open, new SupplyNode(city.Coord, sourceCost));
                }
            }

            while (open.Count > 0)
            {
                SupplyNode node = Pop(open);
                HexCoord current = node.Coord;
                if (!costs.TryGetValue(current, out int currentCost) || currentCost != node.Cost)
                    continue; // より短い経路が後から登録された旧ノード

                foreach (Tile tile in state.Map.NeighborsOf(current))
                {
                    if (tile == null || !tile.IsPassable || BlocksSupply(player, tile)) continue;
                    int candidate = currentCost + StepCost(player, tile);
                    if (costs.TryGetValue(tile.Coord, out int known) && known <= candidate) continue;
                    costs[tile.Coord] = candidate;
                    Push(open, new SupplyNode(tile.Coord, candidate));
                }
            }

            return costs;
        }

        public static SupplyLevel LevelAt(Player player, HexCoord coord,
            IDictionary<HexCoord, int> supplyCosts)
        {
            if (player == null || supplyCosts == null || !supplyCosts.TryGetValue(coord, out int cost))
                return SupplyLevel.Isolated;
            int range = SupplyRange(player);
            if (cost <= range) return SupplyLevel.Supplied;
            if (cost <= range + StrainedMargin) return SupplyLevel.Strained;
            return SupplyLevel.Isolated;
        }

        public static SupplyLevel EvaluateUnit(GameState state, Unit unit)
        {
            if (state == null || unit == null) return SupplyLevel.Isolated;
            Player player = state.GetPlayer(unit.PlayerId);
            return LevelAt(player, unit.Coord, BuildSupplyCosts(state, player));
        }

        /// <summary>
        /// 文明の全ユニットへ今ターンの補給状態を確定する。孤立2ターン目以降の
        /// 非民間ユニットはHP1を下限に消耗するため、補給だけでは消滅しない。
        /// </summary>
        public static void AdvancePlayer(GameState state, Player player)
        {
            if (state == null || player == null || player.IsEliminated) return;
            var costs = BuildSupplyCosts(state, player);
            for (int i = 0; i < player.Units.Count; i++)
            {
                Unit unit = player.Units[i];
                if (unit == null || unit.IsDead) continue;
                SupplyLevel previous = unit.Supply;
                unit.Supply = LevelAt(player, unit.Coord, costs);
                if (unit.Supply == SupplyLevel.Supplied)
                {
                    unit.TurnsOutOfSupply = 0;
                }
                else if (unit.Supply == SupplyLevel.Strained)
                {
                    unit.TurnsOutOfSupply = Math.Max(0, unit.TurnsOutOfSupply - 1);
                }
                else
                {
                    unit.TurnsOutOfSupply++;
                    if (!unit.Def.IsCivilian && unit.TurnsOutOfSupply > AttritionGraceTurns)
                        unit.Hp = Math.Max(1, unit.Hp - AttritionDamage);
                }

                if (player.IsHuman && previous != unit.Supply && unit.Supply == SupplyLevel.Isolated)
                    state.EmitLog($"{unit.Def.NameJa}が補給線から孤立した");
            }
        }

        public static int MovementAllowance(Unit unit)
        {
            if (unit == null) return 0;
            return unit.Supply == SupplyLevel.Isolated
                ? Math.Max(1, unit.Def.Moves - 1)
                : unit.Def.Moves;
        }

        public static int ScaleHealing(Unit unit, int healing)
        {
            if (unit == null || healing <= 0) return 0;
            if (unit.Supply == SupplyLevel.Isolated) return 0;
            if (unit.Supply == SupplyLevel.Strained) return Math.Max(1, healing / 2);
            return healing;
        }

        public static float ScaleCombat(Unit unit, float strength)
        {
            if (unit == null) return strength;
            switch (unit.Supply)
            {
                case SupplyLevel.Strained: return strength * 0.9f;
                case SupplyLevel.Isolated: return strength * 0.75f;
                default: return strength;
            }
        }

        public static void CountLevels(Player player, out int supplied, out int strained, out int isolated)
        {
            supplied = strained = isolated = 0;
            if (player == null) return;
            for (int i = 0; i < player.Units.Count; i++)
            {
                Unit unit = player.Units[i];
                if (unit == null || unit.IsDead) continue;
                if (unit.Supply == SupplyLevel.Supplied) supplied++;
                else if (unit.Supply == SupplyLevel.Strained) strained++;
                else isolated++;
            }
        }

        public static string LevelNameJa(SupplyLevel level)
        {
            switch (NormalizeLevel(level))
            {
                case SupplyLevel.Strained: return "補給逼迫";
                case SupplyLevel.Isolated: return "孤立";
                default: return "補給良好";
            }
        }

        public static SupplyLevel LevelFromSaveValue(int value)
        {
            return value >= (int)SupplyLevel.Supplied && value <= (int)SupplyLevel.Isolated
                ? (SupplyLevel)value
                : SupplyLevel.Supplied;
        }

        static SupplyLevel NormalizeLevel(SupplyLevel level)
        {
            return LevelFromSaveValue((int)level);
        }

        static bool BlocksSupply(Player player, Tile tile)
        {
            if (tile.Unit != null && tile.Unit.PlayerId != player.Id &&
                player.IsAtWarWith(tile.Unit.PlayerId)) return true;
            if (tile.City != null && tile.City.PlayerId != player.Id &&
                player.IsAtWarWith(tile.City.PlayerId)) return true;
            return false;
        }

        static int StepCost(Player player, Tile tile)
        {
            int cost = 1;
            if (tile.HasHill || tile.HasForest) cost++;

            if (tile.OwnerPlayerId == player.Id)
            {
                // 車輪を持つ文明の領内交通網を道路の抽象表現として扱う。
                if (player.HasTech("wheel")) return 1;
            }
            else if (tile.OwnerPlayerId >= 0)
            {
                cost += 2;
            }
            else
            {
                cost += 1;
            }
            return cost;
        }

        static int CompareCoord(HexCoord a, HexCoord b)
        {
            int cmp = a.r.CompareTo(b.r);
            return cmp != 0 ? cmp : a.q.CompareTo(b.q);
        }

        readonly struct SupplyNode
        {
            public readonly HexCoord Coord;
            public readonly int Cost;
            public SupplyNode(HexCoord coord, int cost) { Coord = coord; Cost = cost; }
        }

        static bool Before(SupplyNode a, SupplyNode b)
        {
            int cmp = a.Cost.CompareTo(b.Cost);
            return cmp < 0 || (cmp == 0 && CompareCoord(a.Coord, b.Coord) < 0);
        }

        static void Push(List<SupplyNode> heap, SupplyNode node)
        {
            heap.Add(node);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (!Before(heap[i], heap[parent])) break;
                SupplyNode tmp = heap[parent];
                heap[parent] = heap[i];
                heap[i] = tmp;
                i = parent;
            }
        }

        static SupplyNode Pop(List<SupplyNode> heap)
        {
            SupplyNode result = heap[0];
            int last = heap.Count - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);
            int i = 0;
            while (i < heap.Count)
            {
                int left = i * 2 + 1;
                if (left >= heap.Count) break;
                int right = left + 1;
                int best = right < heap.Count && Before(heap[right], heap[left]) ? right : left;
                if (!Before(heap[best], heap[i])) break;
                SupplyNode tmp = heap[i];
                heap[i] = heap[best];
                heap[best] = tmp;
                i = best;
            }
            return result;
        }
    }
}
