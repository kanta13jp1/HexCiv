using System;

namespace HexCiv.Core
{
    /// <summary>文明が採用する基本法。セーブ値の順序を変更せず、追加は末尾へ行う。</summary>
    public enum CivicLaw
    {
        CouncilOfElders = 0,
        LocalAssemblies = 1,
        MerchantCharters = 2,
        CitizenMilitia = 3,
    }

    /// <summary>
    /// 政治力、正統性、4つの抽象的な利害集団、法律変更を扱う決定論的シミュレーション。
    /// 実在社会の人々を固定的な身分・思想へ割り当てず、ゲーム上の政策支持として集計する。
    /// </summary>
    public static class PoliticalSystem
    {
        public const int StartingPoliticalCapital = 20;
        public const int StartingLegitimacy = 60;
        public const int StartingSupport = 50;
        public const int ChangeLawCost = 30;
        public const int MaximumPoliticalCapital = 999;

        public static CivicLaw NormalizeLaw(CivicLaw law)
        {
            return law switch
            {
                CivicLaw.LocalAssemblies => CivicLaw.LocalAssemblies,
                CivicLaw.MerchantCharters => CivicLaw.MerchantCharters,
                CivicLaw.CitizenMilitia => CivicLaw.CitizenMilitia,
                _ => CivicLaw.CouncilOfElders,
            };
        }

        public static CivicLaw LawFromSaveValue(int value) => NormalizeLaw((CivicLaw)value);

        public static string LawNameJa(CivicLaw law)
        {
            return NormalizeLaw(law) switch
            {
                CivicLaw.LocalAssemblies => "地域民会",
                CivicLaw.MerchantCharters => "商業特許状",
                CivicLaw.CitizenMilitia => "市民兵制",
                _ => "長老評議会",
            };
        }

        public static string LawEffectJa(CivicLaw law)
        {
            return NormalizeLaw(law) switch
            {
                CivicLaw.LocalAssemblies => "文化+都市数、満足度+4、税収-5%",
                CivicLaw.MerchantCharters => "税収+10%、満足度-2",
                CivicLaw.CitizenMilitia => "補給距離+1、都市ごとに維持費+1",
                _ => "正統性目標+5",
            };
        }

        public static string InterestNameJa(int index)
        {
            return index switch
            {
                0 => "学術層",
                1 => "商業層",
                2 => "伝統層",
                3 => "軍事層",
                _ => "利害集団",
            };
        }

        public static bool SetLaw(GameState state, Player player, CivicLaw law,
            bool spendCapital = true, bool writeLog = true)
        {
            if (player == null) return false;
            CivicLaw normalized = NormalizeLaw(law);
            if (player.ActiveLaw == normalized) return false;
            if (spendCapital && player.PoliticalCapital < ChangeLawCost) return false;
            if (spendCapital) player.PoliticalCapital -= ChangeLawCost;
            player.ActiveLaw = normalized;
            if (writeLog && state != null)
                state.EmitLog($"「{player.NameJa}」が法律「{LawNameJa(normalized)}」を制定した");
            state?.Bump();
            return true;
        }

        public static int CultureBonus(Player player)
        {
            if (player == null || NormalizeLaw(player.ActiveLaw) != CivicLaw.LocalAssemblies) return 0;
            return Math.Max(1, player.Cities.Count);
        }

        public static int SatisfactionBonus(Player player)
        {
            if (player == null) return 0;
            return NormalizeLaw(player.ActiveLaw) switch
            {
                CivicLaw.LocalAssemblies => 4,
                CivicLaw.MerchantCharters => -2,
                _ => 0,
            };
        }

        public static int RevenuePercent(Player player)
        {
            if (player == null) return 100;
            return NormalizeLaw(player.ActiveLaw) switch
            {
                CivicLaw.LocalAssemblies => 95,
                CivicLaw.MerchantCharters => 110,
                _ => 100,
            };
        }

        public static int AdditionalExpenses(Player player)
        {
            return player != null && NormalizeLaw(player.ActiveLaw) == CivicLaw.CitizenMilitia
                ? Math.Max(0, player.Cities.Count)
                : 0;
        }

        public static int SupplyRangeBonus(Player player)
        {
            return player != null && NormalizeLaw(player.ActiveLaw) == CivicLaw.CitizenMilitia ? 1 : 0;
        }

        public static CivicLaw RecommendLaw(Player player)
        {
            if (player == null) return CivicLaw.CouncilOfElders;
            if (player.AtWarWith.Count > 0) return CivicLaw.CitizenMilitia;
            if (player.Treasury < 30) return CivicLaw.MerchantCharters;
            if (PopulationSystem.AverageEducation(player) >= 60)
                return CivicLaw.LocalAssemblies;
            return CivicLaw.CouncilOfElders;
        }

        public static void AdvancePlayer(GameState state, Player player)
        {
            if (state == null || player == null || player.IsEliminated) return;

            if (!player.IsHuman && player.PoliticalCapital >= ChangeLawCost)
            {
                CivicLaw recommendation = RecommendLaw(player);
                if (recommendation != player.ActiveLaw)
                    SetLaw(state, player, recommendation, true, true);
            }

            int education = PopulationSystem.AverageEducation(player);
            int satisfaction = PopulationSystem.AverageSatisfaction(player);
            int artisans = 0;
            int scholars = 0;
            int monuments = 0;
            for (int i = 0; i < player.Cities.Count; i++)
            {
                City city = player.Cities[i];
                artisans += Math.Max(0, city.Artisans);
                scholars += Math.Max(0, city.Scholars);
                if (city.Buildings != null && city.Buildings.Contains("monument")) monuments++;
            }

            int scholarTarget = 25 + education / 2 + Math.Min(15, scholars * 3);
            if (player.SocialFocus == SocialFocus.Learning) scholarTarget += 10;
            int merchantTarget = 35 + Math.Min(30, artisans * 4) +
                (player.Treasury >= 150 ? 10 : player.Treasury < 0 ? -15 : 0);
            if (player.SocialFocus == SocialFocus.Crafts) merchantTarget += 10;
            int traditionalTarget = 55 + Math.Min(15, monuments * 5) - education / 5;
            if (player.SocialFocus == SocialFocus.Agriculture) traditionalTarget += 10;
            int militaryTarget = 30 + Math.Min(25, player.Units.Count * 3) +
                (player.AtWarWith.Count > 0 ? 25 : 0) - player.WarWeariness / 5;

            player.ScholarSupport = MoveTowards(player.ScholarSupport,
                Math.Clamp(scholarTarget, 0, 100), 2);
            player.MerchantSupport = MoveTowards(player.MerchantSupport,
                Math.Clamp(merchantTarget, 0, 100), 2);
            player.TraditionalSupport = MoveTowards(player.TraditionalSupport,
                Math.Clamp(traditionalTarget, 0, 100), 2);
            player.MilitarySupport = MoveTowards(player.MilitarySupport,
                Math.Clamp(militaryTarget, 0, 100), 2);

            int alignedSupport = SupportForLaw(player, player.ActiveLaw);
            int legitimacyTarget = 20 + player.Stability / 2 + satisfaction / 4 +
                (alignedSupport - 50) / 3 - player.WarWeariness / 10;
            if (NormalizeLaw(player.ActiveLaw) == CivicLaw.CouncilOfElders)
                legitimacyTarget += 5;
            player.Legitimacy = MoveTowards(player.Legitimacy,
                Math.Clamp(legitimacyTarget, 0, 100), 2);

            int gain = 1 + player.Cities.Count / 2 + player.Legitimacy / 30;
            if (alignedSupport >= 65) gain++;
            player.PoliticalCapital = Math.Clamp(player.PoliticalCapital + Math.Max(1, gain),
                0, MaximumPoliticalCapital);
        }

        public static int SupportForLaw(Player player, CivicLaw law)
        {
            if (player == null) return 0;
            return NormalizeLaw(law) switch
            {
                CivicLaw.LocalAssemblies => player.ScholarSupport,
                CivicLaw.MerchantCharters => player.MerchantSupport,
                CivicLaw.CitizenMilitia => player.MilitarySupport,
                _ => player.TraditionalSupport,
            };
        }

        public static void Initialize(Player player)
        {
            if (player == null) return;
            player.PoliticalCapital = StartingPoliticalCapital;
            player.Legitimacy = StartingLegitimacy;
            player.ActiveLaw = CivicLaw.CouncilOfElders;
            player.ScholarSupport = StartingSupport;
            player.MerchantSupport = StartingSupport;
            player.TraditionalSupport = StartingSupport;
            player.MilitarySupport = StartingSupport;
        }

        static int MoveTowards(int current, int target, int step)
        {
            current = Math.Clamp(current, 0, 100);
            if (current < target) return Math.Min(target, current + step);
            if (current > target) return Math.Max(target, current - step);
            return current;
        }
    }
}
