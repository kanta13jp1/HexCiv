using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using HexCiv.Core;

/// <summary>人口階層・需要・教育・移住・AI重点・セーブ互換を検証する。</summary>
public static class PopulationSystemSmokeTest
{
    public static void Run()
    {
        try
        {
            ValidateStrataAndFocusYields();
            ValidateNeedsAndMigration();
            ValidateAIAndSaveVersion12();
            Debug.Log("POPULATION SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("POPULATION SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateStrataAndFocusYields()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        City city = player.Cities[0];
        PopulationSystem.SetFocus(state, player, SocialFocus.Balanced, false);
        AssertPopulationInvariant(city);
        PopulationSystem.SetFocus(state, player, SocialFocus.Agriculture, false);
        Yields agriculture = PopulationSystem.YieldBonus(player, city);
        PopulationSystem.SetFocus(state, player, SocialFocus.Crafts, false);
        Yields crafts = PopulationSystem.YieldBonus(player, city);
        PopulationSystem.SetFocus(state, player, SocialFocus.Learning, false);
        Yields learning = PopulationSystem.YieldBonus(player, city);
        if (agriculture.Food <= crafts.Food || crafts.Production <= agriculture.Production ||
            learning.Science <= crafts.Science)
            throw new Exception("社会重点の産出差が成立しない");
        AssertPopulationInvariant(city);
        Debug.Log("[Population] 三階層合計・社会重点の産出差OK");
    }

    static void ValidateNeedsAndMigration()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        City source = player.Cities[0];
        City destination = player.Cities[1];
        source.Satisfaction = 0;
        source.Education = 0;
        source.FoodNeedFulfillment = 0;
        source.HousingNeedFulfillment = 25;
        destination.Satisfaction = 100;
        destination.Education = 100;
        destination.Buildings.AddRange(new[] { "monument", "granary", "library", "workshop", "walls" });
        int sourceBefore = source.Population;
        int destinationBefore = destination.Population;
        PopulationSystem.AdvancePlayer(state, player);
        if (source.Population != sourceBefore - 1 || destination.Population != destinationBefore + 1 ||
            source.LastNetMigration != -1 || destination.LastNetMigration != 1)
            throw new Exception("決定論的な都市間移住が成立しない");
        if (source.ServiceNeedFulfillment <= 0 || destination.HousingNeedFulfillment <= 0)
            throw new Exception("都市需要が更新されない");
        AssertPopulationInvariant(source);
        AssertPopulationInvariant(destination);
        Debug.Log("[Population] 需要更新・魅力差による1人口移住OK");
    }

    static void ValidateAIAndSaveVersion12()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        player.Cities[0].FoodNeedFulfillment = 50;
        if (PopulationSystem.RecommendFocus(player) != SocialFocus.Agriculture)
            throw new Exception("食料不足AIが農業重視を選ばない");
        for (int i = 0; i < player.Cities.Count; i++) player.Cities[i].FoodNeedFulfillment = 100;
        player.AtWarWith.Add(1);
        if (PopulationSystem.RecommendFocus(player) != SocialFocus.Crafts)
            throw new Exception("戦争中AIが工芸重視を選ばない");
        player.AtWarWith.Clear();
        player.Cities[0].Education = 20;
        if (!player.Cities[0].Buildings.Contains("library")) player.Cities[0].Buildings.Add("library");
        if (PopulationSystem.RecommendFocus(player) != SocialFocus.Learning)
            throw new Exception("図書館を持つ低教育AIが学問重視を選ばない");

        PopulationSystem.SetFocus(state, player, SocialFocus.Learning, false);
        string json1 = SaveLoad.Serialize(state);
        if (!json1.Contains("\"version\":17")) throw new Exception("セーブversion 17ではない");
        GameState restored = SaveLoad.Deserialize(json1);
        string json2 = SaveLoad.Serialize(restored);
        string json3 = SaveLoad.Serialize(SaveLoad.Deserialize(json2));
        if (json2 != json3) throw new Exception("人口社会を含むセーブ往復が非決定的");
        Player restoredPlayer = restored.GetPlayer(0);
        if (restoredPlayer.SocialFocus != SocialFocus.Learning || restoredPlayer.Cities[0].Education != 20)
            throw new Exception("人口社会値の復元に失敗");
        AssertPopulationInvariant(restoredPlayer.Cities[0]);

        string old = json1.Replace("\"version\":17", "\"version\":11");
        string[] fields = { "socialFocus", "farmers", "artisans", "scholars", "education",
            "satisfaction", "foodNeedFulfillment", "housingNeedFulfillment",
            "serviceNeedFulfillment", "lastNetMigration" };
        for (int i = 0; i < fields.Length; i++)
            old = Regex.Replace(old, ",\"" + fields[i] + "\":-?[0-9]+", "");
        GameState migrated = SaveLoad.Deserialize(old);
        Player migratedPlayer = migrated.GetPlayer(0);
        City migratedCity = migratedPlayer.Cities[0];
        if (migratedPlayer.SocialFocus != SocialFocus.Balanced ||
            migratedCity.Education != PopulationSystem.StartingEducation ||
            migratedCity.Satisfaction != PopulationSystem.StartingSatisfaction ||
            migratedCity.Farmers != migratedCity.Population)
            throw new Exception("バージョン11セーブの人口社会既定値移行に失敗");
        Debug.Log("[Population] AI重点・セーブv12決定往復・v11移行OK");
    }

    static void AssertPopulationInvariant(City city)
    {
        if (city.Farmers < 1 || city.Artisans < 0 || city.Scholars < 0 ||
            city.Farmers + city.Artisans + city.Scholars != city.Population)
            throw new Exception("人口階層の合計不変条件に違反: " + city.NameJa);
    }

    static GameState BuildState()
    {
        var state = new GameState
        {
            Config = new GameConfig { MapWidth = 8, MapHeight = 4, NumPlayers = 2,
                Seed = 7222, MaxTurns = 250, HumanPlayerIndex = 0 },
            Map = new HexMap(8, 4),
            Rng = new System.Random(7222),
            TurnNumber = 20,
        };
        foreach (Tile tile in state.Map.AllTiles) tile.Terrain = TerrainType.Plains;
        var player = new Player { Id = 0, CivilizationId = "egypt", NameJa = "エジプト",
            LeaderId = LeaderCatalog.DefaultForCivilization("egypt").Id, Color = Color.yellow,
            IsHuman = true, CapitalCityId = 1, Treasury = 100 };
        var enemy = new Player { Id = 1, CivilizationId = "sumer", NameJa = "シュメール",
            LeaderId = LeaderCatalog.DefaultForCivilization("sumer").Id, Color = Color.red };
        var source = new City { Id = 1, PlayerId = 0, NameJa = "テーベ",
            Coord = HexCoord.FromOffset(1, 1), Population = 6, Education = 70,
            Buildings = new List<string> { "library", "workshop" } };
        var destination = new City { Id = 2, PlayerId = 0, NameJa = "メンフィス",
            Coord = HexCoord.FromOffset(5, 1), Population = 2 };
        player.Cities.Add(source);
        player.Cities.Add(destination);
        state.Players.Add(player);
        state.Players.Add(enemy);
        state.Map.Get(source.Coord).City = source;
        state.Map.Get(destination.Coord).City = destination;
        PopulationSystem.NormalizeLoadedCity(player, source);
        PopulationSystem.NormalizeLoadedCity(player, destination);
        return state;
    }
}
