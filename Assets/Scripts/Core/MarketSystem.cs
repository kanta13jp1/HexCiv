using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>市場で生産・消費・交易される5つの抽象財。</summary>
    public enum MarketGood
    {
        Food = 0,
        Materials = 1,
        Manufactures = 2,
        Knowledge = 3,
        Transport = 4,
    }

    /// <summary>文明の市場運営方針。値はセーブ対象なので順序を変更しない。</summary>
    public enum EconomicPolicy
    {
        SelfSufficiency = 0,
        BalancedMarkets = 1,
        ExportPromotion = 2,
        WarMobilization = 3,
    }

    /// <summary>
    /// 人口需要、職能生産、在庫、地域産業、価格、文明間交易を扱う決定論的システム。
    /// 個別の歴史文化を普遍的な「商品」に還元せず、MaterialCultureCatalogの項目は
    /// 地域産業・知識として発展させ、その効果だけを5つの抽象財へ接続する。
    /// </summary>
    public static class MarketSystem
    {
        public const int StartingStock = 3;
        public const int MaximumStock = 999;
        public const int StartingMarketAccess = 50;
        public const int StartingDemandFulfillment = 75;
        public const int IndustryDevelopmentInterval = 8;

        const int GoodCount = 5;

        sealed class Ledger
        {
            public Player Player;
            public readonly int[] Demand = new int[GoodCount];
            public readonly int[] Unmet = new int[GoodCount];
            public int TotalDemand;
            public int Fulfilled;
        }

        public static EconomicPolicy NormalizePolicy(EconomicPolicy policy)
        {
            return policy switch
            {
                EconomicPolicy.SelfSufficiency => EconomicPolicy.SelfSufficiency,
                EconomicPolicy.ExportPromotion => EconomicPolicy.ExportPromotion,
                EconomicPolicy.WarMobilization => EconomicPolicy.WarMobilization,
                _ => EconomicPolicy.BalancedMarkets,
            };
        }

        public static EconomicPolicy PolicyFromSaveValue(int value) =>
            NormalizePolicy((EconomicPolicy)value);

        public static string PolicyNameJa(EconomicPolicy policy)
        {
            return NormalizePolicy(policy) switch
            {
                EconomicPolicy.SelfSufficiency => "自給優先",
                EconomicPolicy.ExportPromotion => "輸出振興",
                EconomicPolicy.WarMobilization => "戦時動員",
                _ => "均衡市場",
            };
        }

        public static string PolicyEffectJa(EconomicPolicy policy)
        {
            return NormalizePolicy(policy) switch
            {
                EconomicPolicy.SelfSufficiency => "食料・素材+25%／市場アクセス-10",
                EconomicPolicy.ExportPromotion => "製品・輸送+25%／市場アクセス+10",
                EconomicPolicy.WarMobilization => "素材・輸送+25%／知識-25%／交易縮小",
                _ => "生産と交易を均衡",
            };
        }

        public static string GoodNameJa(MarketGood good)
        {
            return good switch
            {
                MarketGood.Food => "食料",
                MarketGood.Materials => "素材",
                MarketGood.Manufactures => "製品",
                MarketGood.Knowledge => "知識",
                MarketGood.Transport => "輸送",
                _ => "財",
            };
        }

        public static int BasePrice(MarketGood good)
        {
            return good switch
            {
                MarketGood.Food => 2,
                MarketGood.Materials => 3,
                MarketGood.Manufactures => 5,
                MarketGood.Knowledge => 6,
                MarketGood.Transport => 4,
                _ => 1,
            };
        }

        public static void Initialize(Player player)
        {
            if (player == null) return;
            player.EconomicPolicy = EconomicPolicy.BalancedMarkets;
            for (int i = 0; i < GoodCount; i++)
            {
                SetStock(player, (MarketGood)i, StartingStock);
                SetPrice(player, (MarketGood)i, BasePrice((MarketGood)i));
            }
            player.MarketAccess = StartingMarketAccess;
            player.DemandFulfillment = StartingDemandFulfillment;
            player.LastImports = 0;
            player.LastExports = 0;
            player.LastTradeBalance = 0;
            player.LastTradePartnerId = -1;
            player.FeaturedIndustryId = null;
            player.DevelopedMaterialCultures.Clear();
        }

        public static bool SetPolicy(GameState state, Player player, EconomicPolicy policy,
            bool writeLog = true)
        {
            if (player == null) return false;
            EconomicPolicy normalized = NormalizePolicy(policy);
            if (player.EconomicPolicy == normalized) return false;
            player.EconomicPolicy = normalized;
            state?.Bump();
            if (writeLog && state != null)
                state.EmitLog($"「{player.NameJa}」が市場方針を「{PolicyNameJa(normalized)}」に変更した");
            return true;
        }

        /// <summary>全存続文明をID順で処理し、同じ状態から常に同じ交易結果を得る。</summary>
        public static void AdvanceMarkets(GameState state)
        {
            if (state == null) return;
            var ledgers = new List<Ledger>();
            for (int i = 0; i < state.Players.Count; i++)
            {
                Player player = state.Players[i];
                if (player == null || player.IsEliminated || player.Cities.Count == 0) continue;
                ledgers.Add(new Ledger { Player = player });
            }
            ledgers.Sort((a, b) => a.Player.Id.CompareTo(b.Player.Id));

            for (int i = 0; i < ledgers.Count; i++) PrepareMarket(state, ledgers[i]);
            for (int good = 0; good < GoodCount; good++)
                TradeGoodBetweenMarkets(state, ledgers, (MarketGood)good);
            for (int i = 0; i < ledgers.Count; i++) FinalizeMarket(ledgers[i]);
        }

        static void PrepareMarket(GameState state, Ledger ledger)
        {
            Player player = ledger.Player;
            if (!player.IsHuman) player.EconomicPolicy = RecommendPolicy(player);
            player.LastImports = 0;
            player.LastExports = 0;
            player.LastTradeBalance = 0;
            player.LastTradePartnerId = -1;

            TryDevelopRegionalIndustry(state, player);
            player.MarketAccess = ComputeMarketAccess(player);

            for (int i = 0; i < GoodCount; i++)
            {
                MarketGood good = (MarketGood)i;
                int produced = Production(player, good);
                SetStock(player, good, Math.Clamp(GetStock(player, good) + produced, 0, MaximumStock));
                int demand = Demand(player, good);
                int consumed = Math.Min(GetStock(player, good), demand);
                SetStock(player, good, GetStock(player, good) - consumed);
                ledger.Demand[i] = demand;
                ledger.Unmet[i] = demand - consumed;
                ledger.TotalDemand += demand;
                ledger.Fulfilled += consumed;
                SetPrice(player, good, PriceFromBalance(good, GetStock(player, good), ledger.Unmet[i]));
            }
        }

        static void TradeGoodBetweenMarkets(GameState state, List<Ledger> ledgers, MarketGood good)
        {
            int goodIndex = (int)good;
            for (int importerIndex = 0; importerIndex < ledgers.Count; importerIndex++)
            {
                Ledger importer = ledgers[importerIndex];
                if (importer.Unmet[goodIndex] <= 0) continue;
                for (int exporterIndex = 0; exporterIndex < ledgers.Count; exporterIndex++)
                {
                    if (importer.Unmet[goodIndex] <= 0) break;
                    Ledger exporter = ledgers[exporterIndex];
                    if (exporter == importer || exporter.Player.IsAtWarWith(importer.Player.Id)) continue;

                    int reserve = Reserve(exporter.Player, good);
                    int available = GetStock(exporter.Player, good) - reserve;
                    if (available <= 0) continue;
                    int capacity = PairTradeCapacity(state, exporter.Player, importer.Player);
                    if (capacity <= 0) continue;
                    int quantity = Math.Min(importer.Unmet[goodIndex], Math.Min(available, capacity));
                    if (quantity <= 0) continue;

                    int unitPrice = Math.Max(1,
                        (GetPrice(exporter.Player, good) + GetPrice(importer.Player, good) + 1) / 2);
                    int buyingPower = Math.Max(0, importer.Player.Treasury + 50);
                    quantity = Math.Min(quantity, buyingPower / unitPrice);
                    if (quantity <= 0) continue;

                    int value = quantity * unitPrice;
                    SetStock(exporter.Player, good, GetStock(exporter.Player, good) - quantity);
                    importer.Unmet[goodIndex] -= quantity;
                    importer.Fulfilled += quantity;
                    exporter.Player.LastExports += quantity;
                    importer.Player.LastImports += quantity;
                    exporter.Player.LastTradeBalance += value;
                    importer.Player.LastTradeBalance -= value;
                    if (exporter.Player.LastTradePartnerId < 0)
                        exporter.Player.LastTradePartnerId = importer.Player.Id;
                    if (importer.Player.LastTradePartnerId < 0)
                        importer.Player.LastTradePartnerId = exporter.Player.Id;
                }
            }
        }

        static void FinalizeMarket(Ledger ledger)
        {
            Player player = ledger.Player;
            player.DemandFulfillment = ledger.TotalDemand <= 0
                ? 100
                : Math.Clamp(ledger.Fulfilled * 100 / ledger.TotalDemand, 0, 100);
            for (int i = 0; i < GoodCount; i++)
            {
                MarketGood good = (MarketGood)i;
                SetPrice(player, good,
                    PriceFromBalance(good, GetStock(player, good), ledger.Unmet[i]));
            }
        }

        public static EconomicPolicy RecommendPolicy(Player player)
        {
            if (player == null) return EconomicPolicy.BalancedMarkets;
            if (player.AtWarWith.Count > 0) return EconomicPolicy.WarMobilization;
            if (player.DemandFulfillment < 60) return EconomicPolicy.SelfSufficiency;
            if (player.Treasury < 40 || player.LastTradeBalance > 8)
                return EconomicPolicy.ExportPromotion;
            return EconomicPolicy.BalancedMarkets;
        }

        public static int Production(Player player, MarketGood good)
        {
            if (player == null) return 0;
            int cities = player.Cities.Count;
            int farmers = 0, artisans = 0, scholars = 0, granaries = 0, workshops = 0, libraries = 0;
            for (int i = 0; i < player.Cities.Count; i++)
            {
                City city = player.Cities[i];
                if (city == null) continue;
                farmers += Math.Max(0, city.Farmers);
                artisans += Math.Max(0, city.Artisans);
                scholars += Math.Max(0, city.Scholars);
                if (city.Buildings.Contains("granary")) granaries++;
                if (city.Buildings.Contains("workshop")) workshops++;
                if (city.Buildings.Contains("library")) libraries++;
            }

            int amount = good switch
            {
                MarketGood.Food => farmers * 2 + granaries * 2 + cities,
                MarketGood.Materials => cities * 2 + workshops + (player.HasTech("mining") ? cities : 0),
                MarketGood.Manufactures => artisans * 3 + workshops * 3 + cities,
                MarketGood.Knowledge => scholars * 2 + libraries * 2 + (player.HasTech("writing") ? cities : 0),
                MarketGood.Transport => cities + (player.HasTech("wheel") ? cities * 2 : 0) +
                    (player.HasTech("construction") ? cities : 0),
                _ => 0,
            };
            amount += IndustryProductionBonus(player, good);
            EconomicPolicy policy = NormalizePolicy(player.EconomicPolicy);
            if (policy == EconomicPolicy.SelfSufficiency &&
                (good == MarketGood.Food || good == MarketGood.Materials))
                amount = ScalePercent(amount, 125);
            else if (policy == EconomicPolicy.ExportPromotion &&
                (good == MarketGood.Manufactures || good == MarketGood.Transport))
                amount = ScalePercent(amount, 125);
            else if (policy == EconomicPolicy.WarMobilization)
            {
                if (good == MarketGood.Materials || good == MarketGood.Transport)
                    amount = ScalePercent(amount, 125);
                else if (good == MarketGood.Knowledge)
                    amount = ScalePercent(amount, 75);
            }
            return Math.Max(0, amount);
        }

        public static int Demand(Player player, MarketGood good)
        {
            if (player == null) return 0;
            int population = 0, buildings = 0, scholars = 0, military = 0;
            for (int i = 0; i < player.Cities.Count; i++)
            {
                City city = player.Cities[i];
                if (city == null) continue;
                population += Math.Max(0, city.Population);
                buildings += city.Buildings.Count;
                scholars += Math.Max(0, city.Scholars);
            }
            for (int i = 0; i < player.Units.Count; i++)
                if (player.Units[i] != null && !player.Units[i].IsDead && !player.Units[i].Def.IsCivilian)
                    military++;
            return good switch
            {
                MarketGood.Food => Math.Max(1, population * 2),
                MarketGood.Materials => Math.Max(1, player.Cities.Count + (military + 1) / 2),
                MarketGood.Manufactures => Math.Max(1, (population + 1) / 2 + buildings),
                MarketGood.Knowledge => Math.Max(1, player.Cities.Count + scholars),
                MarketGood.Transport => Math.Max(1, player.Cities.Count + (military + 1) / 2 +
                    player.AtWarWith.Count),
                _ => 0,
            };
        }

        public static int ComputeMarketAccess(Player player)
        {
            if (player == null || player.Cities.Count == 0) return 0;
            int access = 32 + player.Cities.Count * 5 + Math.Min(20, player.KnownTechs.Count / 5);
            if (player.HasTech("wheel")) access += 10;
            if (player.HasTech("construction")) access += 5;
            if (player.ActiveLaw == CivicLaw.MerchantCharters) access += 10;
            access -= player.AtWarWith.Count * 12;
            access += NormalizePolicy(player.EconomicPolicy) switch
            {
                EconomicPolicy.SelfSufficiency => -10,
                EconomicPolicy.ExportPromotion => 10,
                EconomicPolicy.WarMobilization => -15,
                _ => 0,
            };
            return Math.Clamp(access, 5, 100);
        }

        static int PairTradeCapacity(GameState state, Player exporter, Player importer)
        {
            int capacity = Math.Min(exporter.MarketAccess, importer.MarketAccess) / 25;
            if (exporter.ActiveLaw == CivicLaw.MerchantCharters) capacity++;
            if (importer.ActiveLaw == CivicLaw.MerchantCharters) capacity++;
            if (exporter.TransportGoods <= 0 || importer.TransportGoods <= 0) capacity--;
            if (NormalizePolicy(exporter.EconomicPolicy) == EconomicPolicy.WarMobilization ||
                NormalizePolicy(importer.EconomicPolicy) == EconomicPolicy.WarMobilization) capacity--;

            City exportCapital = FindCapital(exporter);
            City importCapital = FindCapital(importer);
            if (exportCapital != null && importCapital != null &&
                exportCapital.Coord.DistanceTo(importCapital.Coord) > 18) capacity--;
            return Math.Clamp(capacity, 0, 6);
        }

        static City FindCapital(Player player)
        {
            if (player == null) return null;
            for (int i = 0; i < player.Cities.Count; i++)
                if (player.Cities[i].Id == player.CapitalCityId) return player.Cities[i];
            return player.Cities.Count > 0 ? player.Cities[0] : null;
        }

        static int Reserve(Player player, MarketGood good)
        {
            int reserve = NormalizePolicy(player.EconomicPolicy) == EconomicPolicy.SelfSufficiency ? 3 : 1;
            if (good == MarketGood.Food) reserve++;
            if (NormalizePolicy(player.EconomicPolicy) == EconomicPolicy.ExportPromotion) reserve = 0;
            return reserve;
        }

        static int PriceFromBalance(MarketGood good, int stock, int unmet)
        {
            int value = BasePrice(good) + Math.Min(6, Math.Max(0, unmet));
            value -= Math.Min(Math.Max(0, BasePrice(good) - 1), Math.Max(0, stock) / 4);
            return Math.Clamp(value, 1, 20);
        }

        static void TryDevelopRegionalIndustry(GameState state, Player player)
        {
            if (player == null) return;
            bool first = player.DevelopedMaterialCultures.Count == 0;
            if (!first && state.TurnNumber % IndustryDevelopmentInterval !=
                Math.Abs(player.Id * 3) % IndustryDevelopmentInterval) return;

            string region = player.RegionJa;
            if (string.IsNullOrEmpty(region))
            {
                CivilizationDef civilization = CivilizationCatalog.Find(player.CivilizationId) ??
                    CivilizationCatalog.FindByName(player.NameJa);
                region = civilization != null ? civilization.RegionJa : "";
            }
            List<MaterialCultureDef> candidates = MaterialCultureCatalog.ForRegion(region);
            for (int i = 0; i < candidates.Count; i++)
            {
                MaterialCultureDef item = candidates[i];
                if (player.DevelopedMaterialCultures.Contains(item.Id)) continue;
                if (player.KnownTechs.Count < RequiredTechCount(item.Kind)) continue;
                player.DevelopedMaterialCultures.Add(item.Id);
                player.FeaturedIndustryId = item.Id;
                if (state != null)
                    state.EmitLog($"「{player.NameJa}」で地域産業「{item.NameJa}」が発展した");
                return;
            }
        }

        public static int RequiredTechCount(MaterialCultureKind kind)
        {
            return kind switch
            {
                MaterialCultureKind.SpecialtyProduct => 0,
                MaterialCultureKind.RegionalProduct => 3,
                MaterialCultureKind.LocalIcon => 4,
                MaterialCultureKind.Cuisine => 5,
                MaterialCultureKind.Ship => 8,
                MaterialCultureKind.Weapon => 10,
                MaterialCultureKind.Dance => 12,
                MaterialCultureKind.Song => 16,
                MaterialCultureKind.MartialArt => 20,
                MaterialCultureKind.Vehicle => 24,
                MaterialCultureKind.Aircraft => 60,
                MaterialCultureKind.Rocket => 100,
                _ => 0,
            };
        }

        static int IndustryProductionBonus(Player player, MarketGood good)
        {
            int result = 0;
            if (player == null) return result;
            foreach (string id in player.DevelopedMaterialCultures)
            {
                MaterialCultureDef item = MaterialCultureCatalog.Find(id);
                if (item == null) continue;
                if (good == MarketGood.Food && item.Kind == MaterialCultureKind.Cuisine) result++;
                else if (good == MarketGood.Materials &&
                    (item.Kind == MaterialCultureKind.RegionalProduct || item.Kind == MaterialCultureKind.Weapon)) result++;
                else if (good == MarketGood.Manufactures &&
                    (item.Kind == MaterialCultureKind.SpecialtyProduct || item.Kind == MaterialCultureKind.LocalIcon)) result++;
                else if (good == MarketGood.Knowledge &&
                    (item.Kind == MaterialCultureKind.Dance || item.Kind == MaterialCultureKind.Song ||
                     item.Kind == MaterialCultureKind.MartialArt)) result++;
                else if (good == MarketGood.Transport &&
                    (item.Kind == MaterialCultureKind.Ship || item.Kind == MaterialCultureKind.Vehicle ||
                     item.Kind == MaterialCultureKind.Aircraft || item.Kind == MaterialCultureKind.Rocket)) result++;
            }
            return result;
        }

        public static int SatisfactionBonus(Player player)
        {
            if (player == null) return 0;
            return Math.Clamp((player.DemandFulfillment - 65) / 8, -6, 4);
        }

        public static int ProductionPercent(Player player)
        {
            if (player == null) return 100;
            int percent = 90 + Math.Clamp(player.DemandFulfillment, 0, 100) / 10;
            if (player.ManufacturedGoods >= Math.Max(2, player.Cities.Count)) percent += 4;
            if (NormalizePolicy(player.EconomicPolicy) == EconomicPolicy.WarMobilization) percent += 3;
            return Math.Clamp(percent, 85, 115);
        }

        public static int ScaleProduction(Player player, int amount)
        {
            if (amount <= 0) return 0;
            return Math.Max(0, (amount * ProductionPercent(player) + 50) / 100);
        }

        public static int ScienceBonus(Player player)
        {
            if (player == null) return 0;
            int technologyIndustries = 0;
            foreach (string id in player.DevelopedMaterialCultures)
            {
                MaterialCultureDef item = MaterialCultureCatalog.Find(id);
                if (item != null && (item.Kind == MaterialCultureKind.Vehicle ||
                    item.Kind == MaterialCultureKind.Aircraft || item.Kind == MaterialCultureKind.Rocket))
                    technologyIndustries++;
            }
            return Math.Min(5, Math.Max(0, player.KnowledgeGoods) / 8 + technologyIndustries);
        }

        public static int CultureBonus(Player player)
        {
            if (player == null) return 0;
            int result = 0;
            foreach (string id in player.DevelopedMaterialCultures)
            {
                MaterialCultureDef item = MaterialCultureCatalog.Find(id);
                if (item != null && (item.Kind == MaterialCultureKind.Cuisine ||
                    item.Kind == MaterialCultureKind.Dance || item.Kind == MaterialCultureKind.Song))
                    result++;
            }
            return result;
        }

        public static int SupplyRangeBonus(Player player)
        {
            if (player == null || player.Cities.Count == 0) return 0;
            return player.TransportGoods >= Math.Max(6, player.Cities.Count * 3) ? 1 : 0;
        }

        public static int GetStock(Player player, MarketGood good)
        {
            if (player == null) return 0;
            return good switch
            {
                MarketGood.Food => player.FoodGoods,
                MarketGood.Materials => player.MaterialGoods,
                MarketGood.Manufactures => player.ManufacturedGoods,
                MarketGood.Knowledge => player.KnowledgeGoods,
                MarketGood.Transport => player.TransportGoods,
                _ => 0,
            };
        }

        public static void SetStock(Player player, MarketGood good, int value)
        {
            if (player == null) return;
            value = Math.Clamp(value, 0, MaximumStock);
            switch (good)
            {
                case MarketGood.Food: player.FoodGoods = value; break;
                case MarketGood.Materials: player.MaterialGoods = value; break;
                case MarketGood.Manufactures: player.ManufacturedGoods = value; break;
                case MarketGood.Knowledge: player.KnowledgeGoods = value; break;
                case MarketGood.Transport: player.TransportGoods = value; break;
            }
        }

        public static int GetPrice(Player player, MarketGood good)
        {
            if (player == null) return BasePrice(good);
            return good switch
            {
                MarketGood.Food => player.FoodPrice,
                MarketGood.Materials => player.MaterialPrice,
                MarketGood.Manufactures => player.ManufacturedPrice,
                MarketGood.Knowledge => player.KnowledgePrice,
                MarketGood.Transport => player.TransportPrice,
                _ => BasePrice(good),
            };
        }

        public static void SetPrice(Player player, MarketGood good, int value)
        {
            if (player == null) return;
            value = Math.Clamp(value, 1, 20);
            switch (good)
            {
                case MarketGood.Food: player.FoodPrice = value; break;
                case MarketGood.Materials: player.MaterialPrice = value; break;
                case MarketGood.Manufactures: player.ManufacturedPrice = value; break;
                case MarketGood.Knowledge: player.KnowledgePrice = value; break;
                case MarketGood.Transport: player.TransportPrice = value; break;
            }
        }

        static int ScalePercent(int amount, int percent) =>
            Math.Max(0, (amount * percent + 50) / 100);
    }
}
