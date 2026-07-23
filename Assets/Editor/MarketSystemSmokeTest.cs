using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>市場の生産・需要・交易・遮断・地域産業・AI・セーブv15を検証する。</summary>
public static class MarketSystemSmokeTest
{
    public static void Run()
    {
        try
        {
            ValidateProductionPoliciesAndEffects();
            ValidateTradeAndWarBlockade();
            ValidateRegionalIndustryAndAI();
            ValidateSaveVersion15AndMigration();
            Debug.Log("MARKET SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("MARKET SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateProductionPoliciesAndEffects()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        player.EconomicPolicy = EconomicPolicy.BalancedMarkets;
        int food = MarketSystem.Production(player, MarketGood.Food);
        int manufactures = MarketSystem.Production(player, MarketGood.Manufactures);

        player.EconomicPolicy = EconomicPolicy.SelfSufficiency;
        if (MarketSystem.Production(player, MarketGood.Food) <= food ||
            MarketSystem.ComputeMarketAccess(player) >= MarketSystem.StartingMarketAccess)
            throw new Exception("自給優先の食料・市場アクセス効果が不正");

        player.EconomicPolicy = EconomicPolicy.ExportPromotion;
        if (MarketSystem.Production(player, MarketGood.Manufactures) <= manufactures)
            throw new Exception("輸出振興の製品効果が不正");

        player.DemandFulfillment = 100;
        if (MarketSystem.SatisfactionBonus(player) <= 0 ||
            MarketSystem.ProductionPercent(player) < 100)
            throw new Exception("需要充足の満足・生産効果が不正");
        player.DemandFulfillment = 20;
        if (MarketSystem.SatisfactionBonus(player) >= 0)
            throw new Exception("需要不足が満足度を下げない");
        Debug.Log("[Market] 5財生産・4市場方針・需要効果 OK");
    }

    static void ValidateTradeAndWarBlockade()
    {
        GameState state = BuildState();
        Player exporter = state.Players[0];
        Player importer = state.Players[1];
        exporter.Cities[0].Farmers = 10;
        importer.Cities[0].Population = 12;
        importer.Cities[0].Farmers = 1;
        exporter.Treasury = importer.Treasury = 200;

        MarketSystem.AdvanceMarkets(state);
        if (exporter.LastExports <= 0 || importer.LastImports <= 0 ||
            exporter.LastTradeBalance <= 0 || importer.LastTradeBalance >= 0 ||
            exporter.LastTradeBalance + importer.LastTradeBalance != 0)
            throw new Exception("平時の交易量・収支が不正");
        if (exporter.LastTradePartnerId != importer.Id || importer.LastTradePartnerId != exporter.Id)
            throw new Exception("主要交易相手の記録が不正");

        exporter.DeclareWarOn(state, importer);
        MarketSystem.AdvanceMarkets(state);
        if (exporter.LastExports != 0 || exporter.LastImports != 0 ||
            importer.LastExports != 0 || importer.LastImports != 0)
            throw new Exception("交戦国間の交易が遮断されない");
        Debug.Log("[Market] 価格差交易・収支保存・戦争遮断 OK");
    }

    static void ValidateRegionalIndustryAndAI()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        player.DevelopedMaterialCultures.Clear();
        MarketSystem.AdvanceMarkets(state);
        if (player.DevelopedMaterialCultures.Count != 1 ||
            string.IsNullOrEmpty(player.FeaturedIndustryId) ||
            MaterialCultureCatalog.Find(player.FeaturedIndustryId) == null)
            throw new Exception("地域産業が生活技術台帳から発展しない");

        Player ai = state.Players[1];
        ai.IsHuman = false;
        ai.AtWarWith.Add(player.Id);
        if (MarketSystem.RecommendPolicy(ai) != EconomicPolicy.WarMobilization)
            throw new Exception("戦時AIが戦時動員を選ばない");
        ai.AtWarWith.Clear();
        ai.DemandFulfillment = 30;
        if (MarketSystem.RecommendPolicy(ai) != EconomicPolicy.SelfSufficiency)
            throw new Exception("需要不足AIが自給優先を選ばない");
        Debug.Log("[Market] 地域産業発展・AI市場方針 OK");
    }

    static void ValidateSaveVersion15AndMigration()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        player.EconomicPolicy = EconomicPolicy.ExportPromotion;
        player.FoodGoods = 17;
        player.MaterialGoods = 19;
        player.ManufacturedGoods = 23;
        player.KnowledgeGoods = 29;
        player.TransportGoods = 31;
        player.FoodPrice = 4;
        player.MarketAccess = 83;
        player.DemandFulfillment = 72;
        player.LastImports = 5;
        player.LastExports = 7;
        player.LastTradeBalance = 11;
        player.LastTradePartnerId = 1;
        player.FeaturedIndustryId = "couscous";
        player.DevelopedMaterialCultures.Add("kente_textile");
        player.DevelopedMaterialCultures.Add("couscous");

        string json1 = SaveLoad.Serialize(state);
        if (!json1.Contains("\"version\":16")) throw new Exception("セーブversion 16ではない");
        GameState restored = SaveLoad.Deserialize(json1);
        string json2 = SaveLoad.Serialize(restored);
        string json3 = SaveLoad.Serialize(SaveLoad.Deserialize(json2));
        if (json2 != json3) throw new Exception("市場を含むセーブ往復が非決定的");
        Player rp = restored.GetPlayer(player.Id);
        if (rp.EconomicPolicy != EconomicPolicy.ExportPromotion || rp.FoodGoods != 17 ||
            rp.MaterialGoods != 19 || rp.ManufacturedGoods != 23 || rp.KnowledgeGoods != 29 ||
            rp.TransportGoods != 31 || rp.FoodPrice != 4 || rp.MarketAccess != 83 ||
            rp.DemandFulfillment != 72 || rp.LastImports != 5 || rp.LastExports != 7 ||
            rp.LastTradeBalance != 11 || rp.LastTradePartnerId != 1 ||
            rp.FeaturedIndustryId != "couscous" || rp.DevelopedMaterialCultures.Count != 2)
            throw new Exception("市場・地域産業値の復元に失敗: " +
                $"policy={rp.EconomicPolicy} goods={rp.FoodGoods}/{rp.MaterialGoods}/{rp.ManufacturedGoods}/{rp.KnowledgeGoods}/{rp.TransportGoods} " +
                $"price={rp.FoodPrice} access={rp.MarketAccess} fulfill={rp.DemandFulfillment} " +
                $"trade={rp.LastImports}/{rp.LastExports}/{rp.LastTradeBalance}/{rp.LastTradePartnerId} " +
                $"feature={rp.FeaturedIndustryId} developed={rp.DevelopedMaterialCultures.Count}");

        string old = json1.Replace("\"version\":16", "\"version\":13");
        Player migrated = SaveLoad.Deserialize(old).GetPlayer(player.Id);
        if (migrated.EconomicPolicy != EconomicPolicy.BalancedMarkets ||
            migrated.FoodGoods != MarketSystem.StartingStock ||
            migrated.MarketAccess != MarketSystem.StartingMarketAccess ||
            migrated.DemandFulfillment != MarketSystem.StartingDemandFulfillment ||
            migrated.LastTradePartnerId != -1 || migrated.DevelopedMaterialCultures.Count != 0)
            throw new Exception("version 13セーブの市場既定値移行に失敗");
        Debug.Log("[Market] セーブv15決定往復・v13既定値移行 OK");
    }

    static GameState BuildState()
    {
        var state = new GameState
        {
            Config = new GameConfig
            {
                MapWidth = 8, MapHeight = 5, NumPlayers = 2, Seed = 7214,
                MaxTurns = 250, HumanPlayerIndex = 0,
            },
            Map = new HexMap(8, 5),
            Rng = new System.Random(7214),
            TurnNumber = 32,
        };
        var first = new Player
        {
            Id = 0, CivilizationId = "egypt", NameJa = "エジプト", RegionJa = "アフリカ",
            IsHuman = true, LeaderId = LeaderCatalog.DefaultForCivilization("egypt").Id,
            Color = Color.yellow, CapitalCityId = 1, Treasury = 160,
        };
        var second = new Player
        {
            Id = 1, CivilizationId = "sumer", NameJa = "シュメール", RegionJa = "西・南アジア",
            IsHuman = true, LeaderId = LeaderCatalog.DefaultForCivilization("sumer").Id,
            Color = Color.red, CapitalCityId = 2, Treasury = 160,
        };
        var firstCity = new City
        {
            Id = 1, PlayerId = 0, NameJa = "テーベ", Coord = HexCoord.FromOffset(1, 2),
            Population = 4, Farmers = 2, Artisans = 1, Scholars = 1,
            Buildings = new List<string> { "granary", "workshop" },
        };
        var secondCity = new City
        {
            Id = 2, PlayerId = 1, NameJa = "ウル", Coord = HexCoord.FromOffset(6, 2),
            Population = 4, Farmers = 2, Artisans = 1, Scholars = 1,
            Buildings = new List<string> { "library" },
        };
        first.Cities.Add(firstCity);
        second.Cities.Add(secondCity);
        first.Units.Add(new Unit { Id = 1, PlayerId = 0, DefId = "warrior", Coord = firstCity.Coord });
        second.Units.Add(new Unit { Id = 2, PlayerId = 1, DefId = "warrior", Coord = secondCity.Coord });
        state.Players.Add(first);
        state.Players.Add(second);
        state.Map.Get(firstCity.Coord).City = firstCity;
        state.Map.Get(secondCity.Coord).City = secondCity;
        return state;
    }
}
