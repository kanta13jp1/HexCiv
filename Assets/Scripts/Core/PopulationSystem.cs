using System;

namespace HexCiv.Core
{
    /// <summary>人口階層の自動配分に使う文明単位の社会重点。</summary>
    public enum SocialFocus
    {
        Balanced = 0,
        Agriculture = 1,
        Crafts = 2,
        Learning = 3,
    }

    /// <summary>
    /// 都市人口の階層、需要、教育、満足度、都市間移住を扱う決定論的シミュレーション。
    /// 歴史上の身分制度を普遍的・固定的な序列として扱わず、ゲーム上の職能集計として表現する。
    /// </summary>
    public static class PopulationSystem
    {
        public const int StartingEducation = 20;
        public const int StartingSatisfaction = 60;
        public const int MigrationInterval = 4;
        public const int MigrationAttractionGap = 18;

        public static SocialFocus NormalizeFocus(SocialFocus focus)
        {
            return focus switch
            {
                SocialFocus.Agriculture => SocialFocus.Agriculture,
                SocialFocus.Crafts => SocialFocus.Crafts,
                SocialFocus.Learning => SocialFocus.Learning,
                _ => SocialFocus.Balanced,
            };
        }

        public static SocialFocus FocusFromSaveValue(int value) => NormalizeFocus((SocialFocus)value);

        public static string FocusNameJa(SocialFocus focus)
        {
            return NormalizeFocus(focus) switch
            {
                SocialFocus.Agriculture => "農業重視",
                SocialFocus.Crafts => "工芸重視",
                SocialFocus.Learning => "学問重視",
                _ => "均衡",
            };
        }

        public static bool SetFocus(GameState state, Player player, SocialFocus focus, bool writeLog = true)
        {
            if (player == null) return false;
            SocialFocus normalized = NormalizeFocus(focus);
            if (player.SocialFocus == normalized) return false;
            player.SocialFocus = normalized;
            for (int i = 0; i < player.Cities.Count; i++) Rebalance(player, player.Cities[i]);
            state?.Bump();
            if (writeLog && state != null)
                state.EmitLog($"「{player.NameJa}」が社会重点を「{FocusNameJa(normalized)}」に変更した");
            return true;
        }

        /// <summary>旧セーブまたは新規に構成した都市へ安全な初期人口を割り当てる。</summary>
        public static void InitializeCity(City city)
        {
            if (city == null) return;
            city.Population = Math.Max(1, city.Population);
            city.Farmers = city.Population;
            city.Artisans = 0;
            city.Scholars = 0;
            city.Education = StartingEducation;
            city.Satisfaction = StartingSatisfaction;
            city.FoodNeedFulfillment = 100;
            city.HousingNeedFulfillment = 100;
            city.ServiceNeedFulfillment = 50;
            city.LastNetMigration = 0;
        }

        /// <summary>不正なロード値を丸め、人口総数と3階層の合計を一致させる。</summary>
        public static void NormalizeLoadedCity(Player player, City city)
        {
            if (city == null) return;
            city.Population = Math.Max(1, city.Population);
            city.Education = Math.Clamp(city.Education, 0, 100);
            city.Satisfaction = Math.Clamp(city.Satisfaction, 0, 100);
            city.FoodNeedFulfillment = Math.Clamp(city.FoodNeedFulfillment, 0, 100);
            city.HousingNeedFulfillment = Math.Clamp(city.HousingNeedFulfillment, 0, 100);
            city.ServiceNeedFulfillment = Math.Clamp(city.ServiceNeedFulfillment, 0, 100);
            Rebalance(player, city);
        }

        /// <summary>都市産出へ職能別ボーナスを加える。</summary>
        public static Yields YieldBonus(Player player, City city)
        {
            if (city == null) return new Yields(0, 0, 0);
            int food = Math.Max(0, city.Farmers) / 2;
            int production = Math.Max(0, city.Artisans);
            int science = Math.Max(0, city.Scholars);
            switch (NormalizeFocus(player != null ? player.SocialFocus : SocialFocus.Balanced))
            {
                case SocialFocus.Agriculture:
                    if (city.Farmers > 0) food++;
                    break;
                case SocialFocus.Crafts:
                    if (city.Artisans > 0) production++;
                    break;
                case SocialFocus.Learning:
                    if (city.Scholars > 0) science++;
                    break;
            }
            return new Yields(food, production, science);
        }

        public static int ScienceBonus(Player player, City city)
        {
            return YieldBonus(player, city).Science + (city != null && city.Education >= 70 ? 1 : 0);
        }

        public static int CultureBonus(Player player)
        {
            if (player == null) return 0;
            int result = 0;
            for (int i = 0; i < player.Cities.Count; i++)
            {
                City city = player.Cities[i];
                result += Math.Max(0, city.Scholars) / 2;
                if (city.Satisfaction >= 75) result++;
            }
            return result;
        }

        public static int SpecialistTaxBase(Player player)
        {
            if (player == null) return 0;
            int result = 0;
            for (int i = 0; i < player.Cities.Count; i++)
                result += Math.Max(0, player.Cities[i].Artisans) + Math.Max(0, player.Cities[i].Scholars);
            return result;
        }

        public static void AdvancePlayer(GameState state, Player player)
        {
            if (state == null || player == null || player.IsEliminated) return;
            if (!player.IsHuman) player.SocialFocus = RecommendFocus(player);

            for (int i = 0; i < player.Cities.Count; i++)
            {
                player.Cities[i].LastNetMigration = 0;
                AdvanceCity(state, player, player.Cities[i]);
            }
            TryMigration(state, player);
        }

        public static SocialFocus RecommendFocus(Player player)
        {
            if (player == null || player.Cities.Count == 0) return SocialFocus.Balanced;
            int education = 0;
            bool hasLibrary = false;
            for (int i = 0; i < player.Cities.Count; i++)
            {
                City city = player.Cities[i];
                if (city.FoodNeedFulfillment < 70) return SocialFocus.Agriculture;
                education += city.Education;
                if (city.Buildings.Contains("library")) hasLibrary = true;
            }
            if (player.AtWarWith.Count > 0 || player.Treasury < 40) return SocialFocus.Crafts;
            if (hasLibrary && education / player.Cities.Count < 55) return SocialFocus.Learning;
            return SocialFocus.Balanced;
        }

        public static int AverageEducation(Player player) => Average(player, c => c.Education);
        public static int AverageSatisfaction(Player player) => Average(player, c => c.Satisfaction);

        static void AdvanceCity(GameState state, Player player, City city)
        {
            city.Population = Math.Max(1, city.Population);
            Rebalance(player, city);

            Yields yields = city.ComputeYields(state);
            int foodRequired = Math.Max(1, city.Population * GameRules.FoodPerPop);
            city.FoodNeedFulfillment = Math.Clamp(yields.Food * 100 / foodRequired, 0, 100);

            int housingCapacity = 2 + city.Buildings.Count * 2 +
                (city.Buildings.Contains("walls") ? 2 : 0);
            city.HousingNeedFulfillment = city.Population <= housingCapacity
                ? 100
                : Math.Clamp(100 - (city.Population - housingCapacity) * 18, 25, 100);

            int services = 35;
            if (city.Buildings.Contains("monument")) services += 20;
            if (city.Buildings.Contains("granary")) services += 10;
            if (city.Buildings.Contains("library")) services += 25;
            if (city.Buildings.Contains("workshop")) services += 10;
            if (city.Buildings.Contains("walls")) services += 5;
            if (city.Id == player.CapitalCityId) services += 10;
            city.ServiceNeedFulfillment = Math.Clamp(services, 0, 100);

            int satisfactionTarget = (city.FoodNeedFulfillment + city.HousingNeedFulfillment +
                city.ServiceNeedFulfillment) / 3;
            satisfactionTarget += (player.Stability - AdministrationSystem.StartingStability) / 4;
            satisfactionTarget += player.Taxation switch
            {
                TaxPolicy.Low => 6,
                TaxPolicy.High => -8,
                _ => 0,
            };
            satisfactionTarget -= player.WarWeariness / 10;
            satisfactionTarget += PoliticalSystem.SatisfactionBonus(player);
            satisfactionTarget += MarketSystem.SatisfactionBonus(player);
            if (player.SocialFocus == SocialFocus.Balanced) satisfactionTarget += 2;
            city.Satisfaction = MoveTowards(city.Satisfaction,
                Math.Clamp(satisfactionTarget, 0, 100), 2);

            int educationTarget = 10 + Math.Min(30, player.KnownTechs.Count / 4) + city.Scholars * 8;
            if (player.HasTech("writing")) educationTarget += 10;
            if (city.Buildings.Contains("library")) educationTarget += 25;
            if (city.Id == player.CapitalCityId) educationTarget += 5;
            if (player.SocialFocus == SocialFocus.Learning) educationTarget += 12;
            city.Education = MoveTowards(city.Education, Math.Clamp(educationTarget, 0, 100), 2);
            Rebalance(player, city);
        }

        static void Rebalance(Player player, City city)
        {
            if (city == null) return;
            city.Population = Math.Max(1, city.Population);
            int scholars = city.Population >= 3
                ? Math.Min(city.Population / 3, Math.Max(0, (city.Education + 10) / 35))
                : 0;
            if (city.Buildings.Contains("library") && city.Population >= 2) scholars++;
            int artisans = city.Population / 3;
            if (city.Buildings.Contains("workshop") && city.Population >= 2) artisans++;

            SocialFocus focus = NormalizeFocus(player != null ? player.SocialFocus : SocialFocus.Balanced);
            if (focus == SocialFocus.Agriculture)
            {
                scholars = Math.Max(0, scholars - 1);
                artisans = Math.Max(0, artisans - 1);
            }
            else if (focus == SocialFocus.Crafts) artisans++;
            else if (focus == SocialFocus.Learning) scholars++;

            int available = Math.Max(0, city.Population - 1); // 食料生産を担う人口を最低1残す
            if (focus == SocialFocus.Crafts)
            {
                artisans = Math.Min(artisans, available);
                available -= artisans;
                scholars = Math.Min(scholars, available);
            }
            else
            {
                scholars = Math.Min(scholars, available);
                available -= scholars;
                artisans = Math.Min(artisans, available);
            }
            city.Scholars = Math.Max(0, scholars);
            city.Artisans = Math.Max(0, artisans);
            city.Farmers = city.Population - city.Scholars - city.Artisans;
        }

        static void TryMigration(GameState state, Player player)
        {
            if (player.Cities.Count < 2 || state.TurnNumber % MigrationInterval != player.Id % MigrationInterval)
                return;

            City source = null;
            City destination = null;
            int sourceScore = int.MaxValue;
            int destinationScore = int.MinValue;
            for (int i = 0; i < player.Cities.Count; i++)
            {
                City city = player.Cities[i];
                int score = Attraction(city);
                if (city.Population > 1 && (score < sourceScore ||
                    (score == sourceScore && (source == null || city.Id < source.Id))))
                {
                    source = city;
                    sourceScore = score;
                }
                if (score > destinationScore ||
                    (score == destinationScore && (destination == null || city.Id < destination.Id)))
                {
                    destination = city;
                    destinationScore = score;
                }
            }

            if (source == null || destination == null || source == destination ||
                destinationScore - sourceScore < MigrationAttractionGap) return;

            source.Population--;
            destination.Population++;
            source.LastNetMigration = -1;
            destination.LastNetMigration = 1;
            Rebalance(player, source);
            Rebalance(player, destination);
            if (player.IsHuman)
                state.EmitLog($"都市「{source.NameJa}」から「{destination.NameJa}」へ人口が移住した");
        }

        static int Attraction(City city)
        {
            if (city == null) return int.MinValue / 2;
            return city.Satisfaction + city.Education / 2 + city.FoodNeedFulfillment / 4 +
                city.HousingNeedFulfillment / 4 + city.Buildings.Count * 3 - city.Population * 2;
        }

        static int MoveTowards(int current, int target, int step)
        {
            current = Math.Clamp(current, 0, 100);
            if (current < target) return Math.Min(target, current + step);
            if (current > target) return Math.Max(target, current - step);
            return current;
        }

        static int Average(Player player, Func<City, int> selector)
        {
            if (player == null || player.Cities.Count == 0) return 0;
            int total = 0;
            for (int i = 0; i < player.Cities.Count; i++) total += selector(player.Cities[i]);
            return total / player.Cities.Count;
        }
    }
}
