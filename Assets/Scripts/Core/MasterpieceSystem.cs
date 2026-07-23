using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>
    /// 書籍・絵画・彫刻・建築・音楽・演劇・映画を世界で一度だけ収蔵し、文明へ効果を与える純Coreシステム。
    /// 作品の史実データは MasterpieceCatalog、ゲーム上の状態は Player が所有する。
    /// </summary>
    public static class MasterpieceSystem
    {
        public const int BaseCollectionCost = 65;
        public const int CollectionCostStep = 25;

        public static GameState CurrentState { get; private set; }

        public static void Bind(GameState state)
        {
            CurrentState = state;
        }

        public static int PointsPerTurn(Player player)
        {
            if (player == null || player.IsEliminated || player.Cities.Count == 0) return 0;
            return 1 + player.Cities.Count + player.KnownCulturePolicies.Count / 3
                + player.RecruitedGreatPeople.Count / 2;
        }

        /// <summary>
        /// 現モードでの毎ターン作品ポイント(2026-07-23 追加)。短期ゲームは2.5倍。
        /// 収蔵費(BaseCollectionCost + 収蔵数×Step)は据え置くため、100ターンでの累計作品ポイントと
        /// 収蔵件数は標準250ターンとほぼ同じになる。標準モードでは既存の単項版と完全に同値。
        /// </summary>
        public static int PointsPerTurn(GameState state, Player player)
        {
            return GameSpeedRules.ScaleOutput(state != null ? state.Config : null,
                PointsPerTurn(player));
        }

        public static int CulturePerTurnBonus(Player player)
        {
            if (player == null) return 0;
            int total = 0;
            foreach (var id in player.CollectedMasterpieces)
            {
                var work = MasterpieceCatalog.Find(id);
                if (work == null) continue;
                total += 1;
                if (work.Kind == MasterpieceKind.Painting || work.Kind == MasterpieceKind.Music ||
                    work.Kind == MasterpieceKind.Architecture || work.Kind == MasterpieceKind.Theater ||
                    work.Kind == MasterpieceKind.Film) total += 1;
            }
            return total;
        }

        public static int SciencePerTurnBonus(Player player)
        {
            if (player == null) return 0;
            int total = 0;
            foreach (var id in player.CollectedMasterpieces)
            {
                var work = MasterpieceCatalog.Find(id);
                if (work != null && (work.Kind == MasterpieceKind.Book ||
                    work.Kind == MasterpieceKind.Film)) total++;
            }
            return total;
        }

        public static void AdvancePlayer(GameState state, Player player)
        {
            if (state == null || player == null || player.IsEliminated) return;
            int gained = PointsPerTurn(state, player);
            player.MasterpiecePoints += gained;
            player.TotalMasterpiecePoints += gained;

            if (!player.IsHuman)
            {
                var candidates = AvailableMasterpieces(state, player);
                for (int i = 0; i < candidates.Count; i++)
                    if (TryCollect(state, player, candidates[i].Id)) break;
            }
        }

        public static List<MasterpieceDef> AvailableMasterpieces(GameState state, Player player)
        {
            var result = new List<MasterpieceDef>();
            for (int i = 0; i < MasterpieceCatalog.All.Count; i++)
            {
                var work = MasterpieceCatalog.All[i];
                if (!IsCollected(state, work.Id)) result.Add(work);
            }
            result.Sort((a, b) =>
            {
                int affinity = AffinityPercent(player, b).CompareTo(AffinityPercent(player, a));
                if (affinity != 0) return affinity;
                int kind = a.Kind.CompareTo(b.Kind);
                return kind != 0 ? kind : string.CompareOrdinal(a.Id, b.Id);
            });
            return result;
        }

        public static bool IsCollected(GameState state, string workId)
        {
            if (state == null || string.IsNullOrEmpty(workId)) return false;
            for (int i = 0; i < state.Players.Count; i++)
                if (state.Players[i].CollectedMasterpieces.Contains(workId)) return true;
            return false;
        }

        public static int CollectionCost(Player player, MasterpieceDef work)
        {
            int count = player != null ? player.CollectedMasterpieces.Count : 0;
            int baseCost = BaseCollectionCost + count * CollectionCostStep;
            int discount = AffinityPercent(player, work);
            return Math.Max(1, baseCost * (100 - discount) / 100);
        }

        public static int AffinityPercent(Player player, MasterpieceDef work)
        {
            if (player == null || work == null) return 0;
            int affinity = 0;
            if (!string.IsNullOrEmpty(work.RelatedCivilizationId) &&
                string.Equals(player.CivilizationId, work.RelatedCivilizationId,
                    StringComparison.OrdinalIgnoreCase))
                affinity += 20;
            else if (!string.IsNullOrEmpty(work.RegionJa) &&
                string.Equals(player.RegionJa, work.RegionJa, StringComparison.Ordinal))
                affinity += 10;

            if (!string.IsNullOrEmpty(work.RelatedGreatPersonId) &&
                player.RecruitedGreatPeople.Contains(work.RelatedGreatPersonId))
                affinity += 15;
            return Math.Min(30, affinity);
        }

        public static bool TryCollect(GameState state, Player player, string workId)
        {
            if (state == null || player == null || player.IsEliminated) return false;
            var work = MasterpieceCatalog.Find(workId);
            if (work == null || IsCollected(state, work.Id)) return false;
            int cost = CollectionCost(player, work);
            if (player.MasterpiecePoints < cost) return false;
            player.MasterpiecePoints -= cost;
            CollectInternal(state, player, work, false);
            return true;
        }

        /// <summary>偉人登用時、その人物に直接結び付く未収蔵作品を1件だけ無償収蔵する。</summary>
        public static bool TryCollectLinkedWork(GameState state, Player player, string greatPersonId)
        {
            if (state == null || player == null || string.IsNullOrEmpty(greatPersonId)) return false;
            for (int i = 0; i < MasterpieceCatalog.All.Count; i++)
            {
                var work = MasterpieceCatalog.All[i];
                if (!string.Equals(work.RelatedGreatPersonId, greatPersonId,
                        StringComparison.OrdinalIgnoreCase) || IsCollected(state, work.Id)) continue;
                CollectInternal(state, player, work, true);
                return true;
            }
            return false;
        }

        static void CollectInternal(GameState state, Player player, MasterpieceDef work, bool fromGreatPerson)
        {
            player.CollectedMasterpieces.Add(work.Id);
            ApplyImmediateEffect(state, player, work);
            string source = fromGreatPerson ? "偉人の活動により" : "";
            state.EmitLog($"「{player.NameJa}」が{source}作品「{work.NameJa}」を収蔵した！ " +
                EffectTextJa(work.Kind));
            state.Bump();
        }

        static void ApplyImmediateEffect(GameState state, Player player, MasterpieceDef work)
        {
            switch (work.Kind)
            {
                case MasterpieceKind.Book:
                    // 短期ゲームでは現ターンを標準基準へ戻す(標準では恒等。2026-07-23 追加)
                    player.ScienceStored += 70 + Math.Max(0,
                        GameSpeedRules.StandardTurn(state != null ? state.Config : null,
                            state != null ? state.TurnNumber : 0)) / 3;
                    player.CultureStored += 20;
                    player.TotalCulture += 20;
                    break;
                case MasterpieceKind.Painting:
                    AddCulture(player, 70);
                    AddInfluence(state, player, 15);
                    break;
                case MasterpieceKind.Sculpture:
                    AddCulture(player, 45);
                    AddProduction(player, 20);
                    break;
                case MasterpieceKind.Architecture:
                    AddCulture(player, 55);
                    AddProduction(player, 30);
                    break;
                case MasterpieceKind.Music:
                    AddCulture(player, 60);
                    AddInfluence(state, player, 20);
                    break;
                case MasterpieceKind.Theater:
                    AddCulture(player, 55);
                    AddProduction(player, 10);
                    AddInfluence(state, player, 10);
                    break;
                case MasterpieceKind.Film:
                    AddCulture(player, 50);
                    player.ScienceStored += 45;
                    AddInfluence(state, player, 10);
                    break;
            }
        }

        static void AddCulture(Player player, int amount)
        {
            player.CultureStored += amount;
            player.TotalCulture += amount;
        }

        static void AddProduction(Player player, int amount)
        {
            for (int i = 0; i < player.Cities.Count; i++)
                player.Cities[i].ProductionStored += amount;
        }

        static void AddInfluence(GameState state, Player player, int amount)
        {
            for (int i = 0; i < state.Players.Count; i++)
            {
                var target = state.Players[i];
                if (target == player || target.IsEliminated) continue;
                int current;
                player.CulturalInfluence.TryGetValue(target.Id, out current);
                player.CulturalInfluence[target.Id] = current + amount;
            }
        }

        public static string EffectTextJa(MasterpieceKind kind)
        {
            switch (kind)
            {
                case MasterpieceKind.Book: return "科学+70以上・文化+20、以後科学+1/ターン";
                case MasterpieceKind.Painting: return "文化+70・各文明への影響力+15";
                case MasterpieceKind.Sculpture: return "文化+45・全都市の生産+20";
                case MasterpieceKind.Architecture: return "文化+55・全都市の生産+30";
                case MasterpieceKind.Music: return "文化+60・各文明への影響力+20";
                case MasterpieceKind.Theater: return "文化+55・全都市の生産+10・各文明への影響力+10";
                case MasterpieceKind.Film: return "文化+50・科学+45・各文明への影響力+10、以後科学+1/ターン";
                default: return "";
            }
        }
    }
}
