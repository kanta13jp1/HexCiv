using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>1ゲーム内に配置された遺産。最初に到達した文明だけが発見報酬を得る。</summary>
    public sealed class HeritageSiteInstance
    {
        public string SiteId;
        public HexCoord Coord;
        public int DiscoveredByPlayerId = -1;
        public int DiscoveredTurn;

        public HeritageSiteDef Def => HeritageSiteCatalog.Find(SiteId);
        public bool IsDiscovered => DiscoveredByPlayerId >= 0;
    }

    /// <summary>偉人登用時に発動する、史実上の活動分野を大分類したゲーム効果。</summary>
    public enum GreatPersonEffectKind
    {
        Scholarship,
        Culture,
        Engineering,
        Civic,
        Exploration,
        Military,
    }

    /// <summary>遺産発見時の確定報酬。UIとテストからも同じ計算を参照する。</summary>
    public struct HeritageReward
    {
        public int Culture;
        public int Science;
        public int GreatPersonPoints;
        public int AffinityPercent;
    }

    /// <summary>
    /// 世界史台帳を実ゲームへ接続する純Coreシステム。遺産配置・発見、偉人ポイント、
    /// 世界で一度だけの偉人登用、分野別の即時効果を決定的に処理する。
    /// </summary>
    public static class WorldLegacySystem
    {
        public const int StandardHeritageCount = 12;
        public const int BaseGreatPersonCost = 80;
        public const int GreatPersonCostStep = 45;

        static readonly string[] Regions =
        {
            "アフリカ", "西・南アジア", "東・東南アジア",
            "ヨーロッパ・地中海", "アメリカ大陸", "オセアニア"
        };

        /// <summary>独立UI・描画が参照する現在のゲーム。シミュレーション所有権は持たない。</summary>
        public static GameState CurrentState { get; private set; }

        public static void Bind(GameState state)
        {
            CurrentState = state;
            EnsureInitialized(state);
        }

        /// <summary>
        /// まだ遺産がない新規ゲーム・旧セーブへ配置する。ゲーム本体のRngを消費せず、
        /// マップseed由来の専用乱数を使うためAIの乱数列を変えない。
        /// </summary>
        public static void EnsureInitialized(GameState state)
        {
            if (state == null || state.Map == null || state.HeritageSites.Count > 0) return;

            int seed = state.Config != null ? state.Config.Seed : 0;
            var rng = new Random(unchecked(seed * 486187739 ^ state.Map.Width * 73856093 ^
                state.Map.Height * 19349663 ^ 0x4C454741));

            var sites = SelectSites(rng, StandardHeritageCount);
            var candidates = ReachableCandidateTiles(state);
            Shuffle(candidates, rng);

            var chosen = new List<HexCoord>();
            for (int i = 0; i < sites.Count; i++)
            {
                int index = FindSeparatedCandidate(candidates, chosen, 4);
                if (index < 0) index = FindSeparatedCandidate(candidates, chosen, 2);
                if (index < 0) index = FindSeparatedCandidate(candidates, chosen, 0);
                if (index < 0) break;

                var coord = candidates[index];
                candidates.RemoveAt(index);
                chosen.Add(coord);
                state.HeritageSites.Add(new HeritageSiteInstance
                {
                    SiteId = sites[i].Id,
                    Coord = coord,
                });
            }
        }

        static List<HeritageSiteDef> SelectSites(Random rng, int target)
        {
            var result = new List<HeritageSiteDef>();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 各地域から最低2件を選び、世界全域が毎ゲームに登場するようにする。
            for (int r = 0; r < Regions.Length && result.Count < target; r++)
            {
                var pool = HeritageSiteCatalog.ForRegion(Regions[r]);
                Shuffle(pool, rng);
                for (int i = 0; i < pool.Count && i < 2 && result.Count < target; i++)
                    if (used.Add(pool[i].Id)) result.Add(pool[i]);
            }

            if (result.Count < target)
            {
                var rest = new List<HeritageSiteDef>(HeritageSiteCatalog.All);
                Shuffle(rest, rng);
                for (int i = 0; i < rest.Count && result.Count < target; i++)
                    if (used.Add(rest[i].Id)) result.Add(rest[i]);
            }
            return result;
        }

        static List<HexCoord> ReachableCandidateTiles(GameState state)
        {
            var starts = new Queue<HexCoord>();
            var reachable = new HashSet<HexCoord>();
            foreach (var u in state.AllUnits)
                if (state.Map.Get(u.Coord) != null && reachable.Add(u.Coord)) starts.Enqueue(u.Coord);
            foreach (var c in state.AllCities)
                if (state.Map.Get(c.Coord) != null && reachable.Add(c.Coord)) starts.Enqueue(c.Coord);

            while (starts.Count > 0)
            {
                var cur = starts.Dequeue();
                foreach (var tile in state.Map.NeighborsOf(cur))
                    if (tile.IsPassable && reachable.Add(tile.Coord)) starts.Enqueue(tile.Coord);
            }

            // テスト用の空状態や非常用マップでは全通行可能タイルへフォールバックする。
            if (reachable.Count == 0)
                foreach (var tile in state.Map.AllTiles)
                    if (tile.IsPassable) reachable.Add(tile.Coord);

            var occupiedStarts = new List<HexCoord>();
            foreach (var u in state.AllUnits) occupiedStarts.Add(u.Coord);
            foreach (var c in state.AllCities) occupiedStarts.Add(c.Coord);

            var result = new List<HexCoord>();
            foreach (var coord in reachable)
            {
                var tile = state.Map.Get(coord);
                if (tile == null || !tile.IsPassable || tile.City != null) continue;
                bool nearStart = false;
                for (int i = 0; i < occupiedStarts.Count; i++)
                    if (coord.DistanceTo(occupiedStarts[i]) < 3) { nearStart = true; break; }
                if (!nearStart) result.Add(coord);
            }

            // 小マップでは開始地点からの距離条件だけを緩める。
            if (result.Count < StandardHeritageCount)
                foreach (var coord in reachable)
                    if (!result.Contains(coord) && state.Map.Get(coord).City == null) result.Add(coord);
            result.Sort((a, b) =>
            {
                int cmp = a.r.CompareTo(b.r);
                return cmp != 0 ? cmp : a.q.CompareTo(b.q);
            });
            return result;
        }

        static int FindSeparatedCandidate(List<HexCoord> candidates, List<HexCoord> chosen, int minDistance)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                bool ok = true;
                for (int j = 0; j < chosen.Count; j++)
                    if (candidates[i].DistanceTo(chosen[j]) < minDistance) { ok = false; break; }
                if (ok) return i;
            }
            return -1;
        }

        /// <summary>指定座標に未発見遺産があれば発見し、到達文明へ一度だけ報酬を与える。</summary>
        public static bool CheckDiscoveryAt(GameState state, Player player, HexCoord coord)
        {
            if (state == null || player == null || player.IsEliminated) return false;
            for (int i = 0; i < state.HeritageSites.Count; i++)
            {
                var placed = state.HeritageSites[i];
                if (placed.Coord != coord || placed.IsDiscovered) continue;
                var def = placed.Def;
                if (def == null) return false;

                var reward = GetHeritageReward(state, player, def);
                placed.DiscoveredByPlayerId = player.Id;
                placed.DiscoveredTurn = state.TurnNumber;
                player.DiscoveredHeritageSites.Add(def.Id);
                player.CultureStored += reward.Culture;
                player.TotalCulture += reward.Culture;
                player.ScienceStored += reward.Science;
                player.GreatPersonPoints += reward.GreatPersonPoints;
                player.TotalGreatPersonPoints += reward.GreatPersonPoints;

                string affinity = reward.AffinityPercent > 0
                    ? $"（{player.LeaderNameJa}の地域親和性 +{reward.AffinityPercent}%）"
                    : "";
                state.EmitLog($"「{player.NameJa}」が遺産「{def.NameJa}」を発見！ " +
                    $"文化+{reward.Culture} 科学+{reward.Science} 偉人P+{reward.GreatPersonPoints}{affinity}");
                state.Bump();
                return true;
            }
            return false;
        }

        public static HeritageReward GetHeritageReward(GameState state, Player player,
            HeritageSiteDef site)
        {
            int turn = state != null ? state.TurnNumber : 1;
            int affinity = AffinityPercent(player, site != null ? site.RelatedCivilizationId : "",
                site != null ? site.RegionJa : "");
            int cultureBase = 28 + turn / 6;
            int scienceBase = 22 + turn / 8;
            int personBase = 20;
            return new HeritageReward
            {
                Culture = Scale(cultureBase, affinity),
                Science = Scale(scienceBase, affinity),
                GreatPersonPoints = Scale(personBase, affinity),
                AffinityPercent = affinity,
            };
        }

        /// <summary>ユニット・都市位置を検査し、ターンごとの偉人ポイントを加算してAIを自動登用する。</summary>
        public static void AdvancePlayer(GameState state, Player player)
        {
            if (state == null || player == null || player.IsEliminated) return;
            for (int i = 0; i < player.Units.Count; i++)
                CheckDiscoveryAt(state, player, player.Units[i].Coord);
            for (int i = 0; i < player.Cities.Count; i++)
                CheckDiscoveryAt(state, player, player.Cities[i].Coord);

            int gained = GreatPersonPointsPerTurn(player);
            player.GreatPersonPoints += gained;
            player.TotalGreatPersonPoints += gained;

            if (!player.IsHuman)
            {
                var candidates = AvailableGreatPeople(state, player);
                for (int i = 0; i < candidates.Count; i++)
                    if (TryRecruit(state, player, candidates[i].Id)) break;
            }
        }

        public static int GreatPersonPointsPerTurn(Player player)
        {
            if (player == null || player.IsEliminated) return 0;
            return 1 + player.Cities.Count * 2 + player.KnownCulturePolicies.Count / 4;
        }

        public static List<GreatPersonDef> AvailableGreatPeople(GameState state, Player player)
        {
            var result = new List<GreatPersonDef>();
            for (int i = 0; i < GreatPersonCatalog.All.Count; i++)
            {
                var person = GreatPersonCatalog.All[i];
                if (!IsRecruited(state, person.Id)) result.Add(person);
            }
            result.Sort((a, b) =>
            {
                int affinity = AffinityPercent(player, b.RelatedCivilizationId, b.RegionJa)
                    .CompareTo(AffinityPercent(player, a.RelatedCivilizationId, a.RegionJa));
                return affinity != 0 ? affinity : string.CompareOrdinal(a.Id, b.Id);
            });
            return result;
        }

        public static bool IsRecruited(GameState state, string personId)
        {
            if (state == null || string.IsNullOrEmpty(personId)) return false;
            for (int i = 0; i < state.Players.Count; i++)
                if (state.Players[i].RecruitedGreatPeople.Contains(personId)) return true;
            return false;
        }

        public static int RecruitmentCost(Player player, GreatPersonDef person)
        {
            int baseCost = BaseGreatPersonCost +
                (player != null ? player.RecruitedGreatPeople.Count : 0) * GreatPersonCostStep;
            int affinity = AffinityPercent(player,
                person != null ? person.RelatedCivilizationId : "",
                person != null ? person.RegionJa : "");
            // 発見報酬の親和性より控えめにし、世界共通プールの競争性を残す。
            int discount = affinity >= 50 ? 20 : affinity > 0 ? 10 : 0;
            return Math.Max(1, (baseCost * (100 - discount) + 99) / 100);
        }

        public static bool TryRecruit(GameState state, Player player, string personId)
        {
            var person = GreatPersonCatalog.Find(personId);
            if (state == null || player == null || person == null || player.IsEliminated ||
                IsRecruited(state, personId)) return false;
            int cost = RecruitmentCost(player, person);
            if (player.GreatPersonPoints < cost) return false;

            player.GreatPersonPoints -= cost;
            player.RecruitedGreatPeople.Add(person.Id);
            string result = ApplyGreatPersonEffect(state, player, person);
            bool createdWork = MasterpieceSystem.TryCollectLinkedWork(state, player, person.Id);
            int affinity = AffinityPercent(player, person.RelatedCivilizationId, person.RegionJa);
            string affinityText = affinity > 0 ? "（地域親和性で登用費軽減）" : "";
            string workText = createdWork ? " 関連作品も収蔵" : "";
            state.EmitLog($"「{player.NameJa}」が偉人「{person.NameJa}」を登用！ {result}{affinityText}{workText}");
            state.Bump();
            return true;
        }

        static string ApplyGreatPersonEffect(GameState state, Player player, GreatPersonDef person)
        {
            int amount = 90 + state.TurnNumber / 2;
            switch (EffectKind(person))
            {
                case GreatPersonEffectKind.Culture:
                    player.CultureStored += amount;
                    player.TotalCulture += amount;
                    return $"文化+{amount}";
                case GreatPersonEffectKind.Engineering:
                    int production = 0;
                    for (int i = 0; i < player.Cities.Count; i++)
                    {
                        player.Cities[i].ProductionStored += 45;
                        production += 45;
                    }
                    player.ScienceStored += 30;
                    return $"都市生産+{production} 科学+30";
                case GreatPersonEffectKind.Civic:
                    player.CultureStored += 45;
                    player.TotalCulture += 45;
                    for (int i = 0; i < state.Players.Count; i++)
                    {
                        var other = state.Players[i];
                        if (other == player || other.IsEliminated) continue;
                        int current;
                        player.CulturalInfluence.TryGetValue(other.Id, out current);
                        player.CulturalInfluence[other.Id] = current + 30;
                    }
                    return "文化+45 各文明への影響力+30";
                case GreatPersonEffectKind.Exploration:
                    var origin = CapitalCoord(player);
                    if (origin.HasValue)
                        foreach (var tile in state.Map.TilesInRange(origin.Value, 6))
                            player.Explored.Add(tile.Coord);
                    player.ScienceStored += 45;
                    player.CultureStored += 35;
                    player.TotalCulture += 35;
                    return "首都周辺を探検 科学+45 文化+35";
                case GreatPersonEffectKind.Military:
                    int healed = 0;
                    for (int i = 0; i < player.Units.Count; i++)
                    {
                        int before = player.Units[i].Hp;
                        player.Units[i].Hp = Math.Min(GameRules.UnitMaxHp, before + 35);
                        healed += player.Units[i].Hp - before;
                    }
                    player.CultureStored += 30;
                    player.TotalCulture += 30;
                    return $"全軍回復+{healed} 文化+30";
                default:
                    player.ScienceStored += amount;
                    return $"科学+{amount}";
            }
        }

        public static GreatPersonEffectKind EffectKind(GreatPersonDef person)
        {
            string category = person != null ? person.CategoryJa ?? "" : "";
            if (ContainsAny(category, "軍事", "軍略", "海軍", "戦争"))
                return GreatPersonEffectKind.Military;
            if (ContainsAny(category, "航海", "探検", "航空", "旅行", "地理"))
                return GreatPersonEffectKind.Exploration;
            if (ContainsAny(category, "建築", "工学", "技術", "発明", "印刷", "農学"))
                return GreatPersonEffectKind.Engineering;
            if (ContainsAny(category, "社会運動", "政治", "法学", "外交", "公衆衛生", "土地権", "平和"))
                return GreatPersonEffectKind.Civic;
            if (ContainsAny(category, "文学", "美術", "音楽", "演劇", "詩", "哲学", "思想",
                "宗教", "歴史", "教育", "言語", "翻訳", "出版", "文化"))
                return GreatPersonEffectKind.Culture;
            return GreatPersonEffectKind.Scholarship;
        }

        public static string EffectTextJa(GreatPersonDef person)
        {
            switch (EffectKind(person))
            {
                case GreatPersonEffectKind.Culture: return "大作：文化を大量獲得";
                case GreatPersonEffectKind.Engineering: return "革新：全都市の生産と科学を獲得";
                case GreatPersonEffectKind.Civic: return "社会改革：文化と全文明への影響力を獲得";
                case GreatPersonEffectKind.Exploration: return "地平線：首都周辺を探索し科学・文化を獲得";
                case GreatPersonEffectKind.Military: return "統率：全軍を回復し文化を獲得";
                default: return "洞察：科学を大量獲得";
            }
        }

        public static int AffinityPercent(Player player, string relatedCivilizationId, string regionJa)
        {
            if (player == null) return 0;
            if (!string.IsNullOrEmpty(relatedCivilizationId) &&
                string.Equals(player.CivilizationId, relatedCivilizationId,
                    StringComparison.OrdinalIgnoreCase)) return 50;
            if (!string.IsNullOrEmpty(regionJa) &&
                string.Equals(player.RegionJa, regionJa, StringComparison.Ordinal)) return 25;
            return 0;
        }

        static HexCoord? CapitalCoord(Player player)
        {
            if (player == null) return null;
            for (int i = 0; i < player.Cities.Count; i++)
                if (player.Cities[i].Id == player.CapitalCityId) return player.Cities[i].Coord;
            if (player.Cities.Count > 0) return player.Cities[0].Coord;
            if (player.Units.Count > 0) return player.Units[0].Coord;
            return null;
        }

        static bool ContainsAny(string value, params string[] terms)
        {
            for (int i = 0; i < terms.Length; i++)
                if (value.IndexOf(terms[i], StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        static int Scale(int value, int percent)
        {
            return Math.Max(1, (value * (100 + percent) + 50) / 100);
        }

        static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }

    /// <summary>世界遺産配置をゲーム状態に保持する拡張。</summary>
    public partial class GameState
    {
        public List<HeritageSiteInstance> HeritageSites = new List<HeritageSiteInstance>();
    }
}
