using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>
    /// 文化ポイント、政策採用、文明間交流、文化勝利を扱う純粋シミュレーション。
    /// UIは CurrentState を読み取れるが、CoreからUnityの表示機能には依存しない。
    /// </summary>
    public static class CultureSystem
    {
        public const int VictoryMinimumTurn = 150;
        public const int VictoryMinimumPolicies = 14;
        public const int VictoryMinimumCulture = 1500;
        public const int VictoryBaseInfluence = 220;
        public const int VictoryCultureDivisor = 4;

        /// <summary>現在のプレイ状態。TurnManager構築時に差し替わる独立UI用の読み取り窓口。</summary>
        public static GameState CurrentState { get; private set; }

        public static void Bind(GameState state)
        {
            CurrentState = state;
        }

        /// <summary>都市・人口・記念碑・採用政策から得る毎ターン文化。</summary>
        public static int CulturePerTurn(GameState state, Player player)
        {
            if (player == null || player.IsEliminated || player.Cities.Count == 0) return 0;

            int population = 0;
            int monuments = 0;
            bool capitalAlive = false;
            for (int i = 0; i < player.Cities.Count; i++)
            {
                var city = player.Cities[i];
                population += Math.Max(0, city.Population);
                if (city.Id == player.CapitalCityId) capitalAlive = true;
                if (city.Buildings.Contains("monument")) monuments++;
            }

            int total = player.Cities.Count + population / 2 + monuments * 2;
            if (capitalAlive) total += 2;
            total += CulturePolicyCatalog.EffectTotal(player, CulturePolicyEffect.Culture);
            total += MasterpieceSystem.CulturePerTurnBonus(player);
            total += PopulationSystem.CultureBonus(player);
            total += PoliticalSystem.CultureBonus(player);
            return AdministrationSystem.ScaleOutput(player, Math.Max(0, total));
        }

        public static int ScaleScience(Player player, int amount)
        {
            return ScalePercent(amount,
                CulturePolicyCatalog.EffectTotal(player, CulturePolicyEffect.Science));
        }

        public static int ScaleProduction(Player player, int amount)
        {
            return ScalePercent(amount,
                CulturePolicyCatalog.EffectTotal(player, CulturePolicyEffect.Production));
        }

        public static List<CulturePolicyDef> AvailablePolicies(Player player)
        {
            var result = new List<CulturePolicyDef>();
            if (player == null) return result;

            for (int i = 0; i < CulturePolicyCatalog.All.Count; i++)
            {
                var policy = CulturePolicyCatalog.All[i];
                if (player.KnownCulturePolicies.Contains(policy.Id)) continue;
                bool available = true;
                for (int p = 0; p < policy.Prereqs.Length; p++)
                    if (!player.KnownCulturePolicies.Contains(policy.Prereqs[p]))
                    {
                        available = false;
                        break;
                    }
                if (available) result.Add(policy);
            }
            return result;
        }

        public static bool CanSelectPolicy(Player player, string policyId)
        {
            if (player == null || string.IsNullOrEmpty(policyId)) return false;
            var available = AvailablePolicies(player);
            for (int i = 0; i < available.Count; i++)
                if (string.Equals(available[i].Id, policyId, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>文化を産出し、選択中の政策コストを満たしたら採用する。</summary>
        public static void AdvancePlayer(GameState state, Player player)
        {
            if (state == null || player == null || player.IsEliminated) return;

            if (string.IsNullOrEmpty(player.CurrentCulturePolicyId) && !player.IsHuman)
                SelectPolicyForAI(state, player);

            int output = CulturePerTurn(state, player);
            player.CultureStored += output;
            player.TotalCulture += output;

            CulturePolicyDef policy;
            if (string.IsNullOrEmpty(player.CurrentCulturePolicyId)) return;
            if (!CulturePolicyCatalog.TryGet(player.CurrentCulturePolicyId, out policy) ||
                player.KnownCulturePolicies.Contains(policy.Id))
            {
                player.CurrentCulturePolicyId = null;
                return;
            }

            if (player.CultureStored < policy.Cost) return;
            player.CultureStored -= policy.Cost;
            player.KnownCulturePolicies.Add(policy.Id);
            player.CurrentCulturePolicyId = null;
            state.EmitLog($"「{player.NameJa}」が文化政策「{policy.NameJa}」を採用した");
        }

        /// <summary>非戦争文明の間で、互いの文化的影響力を蓄積する。</summary>
        public static void AdvanceExchange(GameState state)
        {
            if (state == null) return;
            for (int i = 0; i < state.Players.Count; i++)
            {
                var source = state.Players[i];
                if (source.IsEliminated || source.Cities.Count == 0) continue;

                int output = CulturePerTurn(state, source);
                int gain = 1 + output / 4 + source.KnownCulturePolicies.Count / 8;
                gain += CulturePolicyCatalog.EffectTotal(source, CulturePolicyEffect.Influence);
                gain = Math.Max(1, gain);

                for (int j = 0; j < state.Players.Count; j++)
                {
                    var target = state.Players[j];
                    if (target == source || target.IsEliminated || target.Cities.Count == 0) continue;
                    if (source.IsAtWarWith(target.Id)) continue;

                    int current;
                    source.CulturalInfluence.TryGetValue(target.Id, out current);
                    source.CulturalInfluence[target.Id] = current + gain;
                }
            }
        }

        public static int InfluenceOn(Player source, Player target)
        {
            if (source == null || target == null) return 0;
            int value;
            return source.CulturalInfluence.TryGetValue(target.Id, out value) ? value : 0;
        }

        public static int VictoryThreshold(Player target)
        {
            if (target == null) return VictoryBaseInfluence;
            return VictoryBaseInfluence + Math.Max(0, target.TotalCulture) / VictoryCultureDivisor;
        }

        /// <summary>全生存文明に対する文化勝利の最低進捗率（0～1）。</summary>
        public static float VictoryProgress(GameState state, Player player)
        {
            if (state == null || player == null || player.IsEliminated) return 0f;
            float minimum = 1f;
            int rivals = 0;
            for (int i = 0; i < state.Players.Count; i++)
            {
                var target = state.Players[i];
                if (target == player || target.IsEliminated) continue;
                rivals++;
                float progress = InfluenceOn(player, target) / (float)VictoryThreshold(target);
                if (progress < minimum) minimum = progress;
            }
            return rivals == 0 ? 0f : Math.Max(0f, Math.Min(1f, minimum));
        }

        /// <summary>全生存文明へ十分な影響力を持つ文化首位文明を勝者にする。</summary>
        public static bool CheckCulturalVictory(GameState state)
        {
            if (state == null || state.IsGameOver || state.TurnNumber < VictoryMinimumTurn)
                return false;

            Player winner = null;
            for (int i = 0; i < state.Players.Count; i++)
            {
                var candidate = state.Players[i];
                if (candidate.IsEliminated || candidate.KnownCulturePolicies.Count < VictoryMinimumPolicies ||
                    candidate.TotalCulture < VictoryMinimumCulture) continue;

                bool qualifies = true;
                int rivals = 0;
                for (int j = 0; j < state.Players.Count; j++)
                {
                    var target = state.Players[j];
                    if (target == candidate || target.IsEliminated) continue;
                    rivals++;
                    if (candidate.TotalCulture <= target.TotalCulture ||
                        InfluenceOn(candidate, target) < VictoryThreshold(target))
                    {
                        qualifies = false;
                        break;
                    }
                }

                if (!qualifies || rivals == 0) continue;
                if (winner == null || candidate.TotalCulture > winner.TotalCulture ||
                    (candidate.TotalCulture == winner.TotalCulture && candidate.Id < winner.Id))
                    winner = candidate;
            }

            if (winner == null) return false;
            state.IsGameOver = true;
            state.Winner = winner;
            state.GameOverMessageJa = $"「{winner.NameJa}」が文化勝利を収めた!その文化的影響が世界へ広がった";
            state.EmitLog(state.GameOverMessageJa);
            state.RaiseGameEnded(winner, state.GameOverMessageJa);
            return true;
        }

        static void SelectPolicyForAI(GameState state, Player player)
        {
            var available = AvailablePolicies(player);
            if (available.Count == 0) return;
            available.Sort((a, b) =>
            {
                int cost = a.Cost.CompareTo(b.Cost);
                return cost != 0 ? cost : string.CompareOrdinal(a.Id, b.Id);
            });
            int index = available.Count > 1 && state.Rng != null && state.Rng.Next(100) < 25 ? 1 : 0;
            player.CurrentCulturePolicyId = available[index].Id;
        }

        static int ScalePercent(int amount, int bonusPercent)
        {
            if (amount <= 0 || bonusPercent <= 0) return amount;
            return (amount * (100 + bonusPercent) + 50) / 100;
        }
    }
}
