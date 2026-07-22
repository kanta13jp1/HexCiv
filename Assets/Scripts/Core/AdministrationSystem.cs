using System;
using System.Linq;

namespace HexCiv.Core
{
    /// <summary>国の税率。値はセーブ対象なので既存値の順序を変更しないこと。</summary>
    public enum TaxPolicy
    {
        Low = 0,
        Balanced = 1,
        High = 2,
    }

    /// <summary>
    /// 国庫・税制・行政維持費・安定度・戦争疲弊を扱う純粋なゲームロジック。
    /// </summary>
    public static class AdministrationSystem
    {
        public const int StartingTreasury = 120;
        public const int StartingStability = 60;
        public const int MaximumStability = 100;
        public const int MaximumWarWeariness = 100;
        public const int MinimumTreasury = -9999;
        public const int MaximumTreasury = 999999;

        public static TaxPolicy NormalizePolicy(TaxPolicy policy)
        {
            return policy switch
            {
                TaxPolicy.Low => TaxPolicy.Low,
                TaxPolicy.High => TaxPolicy.High,
                _ => TaxPolicy.Balanced,
            };
        }

        public static TaxPolicy PolicyFromSaveValue(int value) => NormalizePolicy((TaxPolicy)value);

        public static string PolicyNameJa(TaxPolicy policy)
        {
            return NormalizePolicy(policy) switch
            {
                TaxPolicy.Low => "減税",
                TaxPolicy.High => "重税",
                _ => "均衡税",
            };
        }

        public static int TaxPercent(TaxPolicy policy)
        {
            return NormalizePolicy(policy) switch
            {
                TaxPolicy.Low => 80,
                TaxPolicy.High => 130,
                _ => 100,
            };
        }

        public static bool SetTaxPolicy(GameState state, Player player, TaxPolicy policy, bool writeLog = true)
        {
            if (player == null) return false;
            TaxPolicy normalized = NormalizePolicy(policy);
            if (player.Taxation == normalized) return false;
            player.Taxation = normalized;
            state?.Bump();
            if (writeLog && state != null)
                state.EmitLog($"「{player.NameJa}」が税制を「{PolicyNameJa(normalized)}」に変更した");
            return true;
        }

        public static int GrossRevenue(Player player)
        {
            if (player == null) return 0;
            int population = player.Cities.Where(c => c != null).Sum(c => Math.Max(0, c.Population));
            int buildings = player.Cities.Where(c => c != null).Sum(c => c.Buildings?.Count ?? 0);
            bool capitalAlive = player.CapitalCityId >= 0 &&
                player.Cities.Any(c => c != null && c.Id == player.CapitalCityId);
            return population * 2 + player.Cities.Count * 4 + buildings + (capitalAlive ? 6 : 0);
        }

        public static int Revenue(Player player)
        {
            if (player == null) return 0;
            return GrossRevenue(player) * TaxPercent(player.Taxation) / 100;
        }

        public static int UnitMaintenance(Player player)
        {
            if (player == null) return 0;
            int result = 0;
            foreach (Unit unit in player.Units)
            {
                if (unit == null || unit.IsDead) continue;
                if (unit.Def.IsCivilian)
                    result += 1;
                else
                    result += 2 + Math.Max(0, unit.Def.Strength + unit.Def.RangedStrength) / 15;
            }
            return result;
        }

        public static int Expenses(Player player)
        {
            if (player == null) return 0;
            int cityMaintenance = player.Cities.Count * 2;
            int warAdministration = player.AtWarWith.Count * 3;
            return cityMaintenance + UnitMaintenance(player) + warAdministration;
        }

        public static int Balance(Player player) => Revenue(player) - Expenses(player);

        /// <summary>安定度と税制が科学・文化・生産に与える共通倍率。100が標準。</summary>
        public static int OutputPercent(Player player)
        {
            if (player == null) return 100;
            int stability = Math.Clamp(player.Stability, 0, MaximumStability);
            int result = 88 + stability / 5;
            result += NormalizePolicy(player.Taxation) switch
            {
                TaxPolicy.Low => 4,
                TaxPolicy.High => -4,
                _ => 0,
            };
            if (player.Treasury < 0) result -= 8;
            return Math.Clamp(result, 70, 120);
        }

        public static int ScaleOutput(Player player, int amount)
        {
            if (amount <= 0) return 0;
            return Math.Max(0, (amount * OutputPercent(player) + 50) / 100);
        }

        public static TaxPolicy RecommendTaxPolicy(Player player)
        {
            if (player == null) return TaxPolicy.Balanced;
            if (player.Stability <= 25) return TaxPolicy.Low;
            if (player.Treasury < 0) return TaxPolicy.High;
            if (player.AtWarWith.Count > 0 && player.Treasury < 80) return TaxPolicy.High;
            if (player.Treasury > 260 && player.Stability < 72) return TaxPolicy.Low;
            return TaxPolicy.Balanced;
        }

        public static void AdvancePlayer(GameState state, Player player)
        {
            if (player == null) return;

            player.LastRevenue = Revenue(player);
            player.LastExpenses = Expenses(player);
            long treasury = (long)player.Treasury + player.LastRevenue - player.LastExpenses;
            player.Treasury = (int)Math.Clamp(treasury, MinimumTreasury, MaximumTreasury);

            if (player.AtWarWith.Count > 0)
                player.WarWeariness = Math.Min(MaximumWarWeariness,
                    player.WarWeariness + player.AtWarWith.Count);
            else
                player.WarWeariness = Math.Max(0, player.WarWeariness - 2);

            int target = 60;
            target += NormalizePolicy(player.Taxation) switch
            {
                TaxPolicy.Low => 15,
                TaxPolicy.High => -15,
                _ => 0,
            };
            if (player.Treasury < 0) target -= 20;
            else if (player.Treasury > 250) target += 5;
            target -= Math.Min(30, player.WarWeariness / 2);
            bool capitalAlive = player.CapitalCityId >= 0 &&
                player.Cities.Any(c => c != null && c.Id == player.CapitalCityId);
            if (!capitalAlive) target -= 20;
            target = Math.Clamp(target, 0, MaximumStability);

            player.Stability = Math.Clamp(player.Stability, 0, MaximumStability);
            if (player.Stability < target) player.Stability++;
            else if (player.Stability > target) player.Stability--;
            if (player.Treasury < 0 && player.Stability > 0) player.Stability--;
            player.Stability = Math.Clamp(player.Stability, 0, MaximumStability);
        }
    }
}
