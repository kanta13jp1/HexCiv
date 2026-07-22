using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using HexCiv.Core;

/// <summary>国家運営の収支・政策効果・戦争疲弊・AI方針・セーブ互換を検証する。</summary>
public static class AdministrationSystemSmokeTest
{
    public static void Run()
    {
        try
        {
            ValidateEconomyAndTaxTradeoff();
            ValidateWarWearinessAndAI();
            ValidateSaveVersion10AndBackwardDefault();
            Debug.Log("ADMINISTRATION SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("ADMINISTRATION SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateEconomyAndTaxTradeoff()
    {
        var state = BuildState();
        var player = state.Players[0];

        if (AdministrationSystem.GrossRevenue(player) != 19)
            throw new Exception("総税源が不正: " + AdministrationSystem.GrossRevenue(player));
        if (AdministrationSystem.Revenue(player) != 19)
            throw new Exception("均衡税の収入が不正");
        if (AdministrationSystem.Expenses(player) != 5)
            throw new Exception("維持費が不正: " + AdministrationSystem.Expenses(player));

        AdministrationSystem.AdvancePlayer(state, player);
        if (player.Treasury != 134 || player.LastRevenue != 19 || player.LastExpenses != 5)
            throw new Exception("ターン収支の反映が不正");
        if (player.Stability != 60 || AdministrationSystem.OutputPercent(player) != 100)
            throw new Exception("標準安定度の産出倍率が不正");

        AdministrationSystem.SetTaxPolicy(state, player, TaxPolicy.Low, false);
        if (AdministrationSystem.Revenue(player) != 15 ||
            AdministrationSystem.OutputPercent(player) != 104)
            throw new Exception("減税の収入・産出トレードオフが不正");
        AdministrationSystem.SetTaxPolicy(state, player, TaxPolicy.High, false);
        if (AdministrationSystem.Revenue(player) != 24 ||
            AdministrationSystem.OutputPercent(player) != 96)
            throw new Exception("重税の収入・産出トレードオフが不正");

        Debug.Log("[Administration] 収入・維持費・税制トレードオフOK");
    }

    static void ValidateWarWearinessAndAI()
    {
        var state = BuildState();
        var player = state.Players[0];
        var rival = state.Players[1];
        player.DeclareWarOn(state, rival);
        player.Taxation = TaxPolicy.High;
        player.Treasury = 50;
        int beforeStability = player.Stability;

        for (int i = 0; i < 5; i++) AdministrationSystem.AdvancePlayer(state, player);
        if (player.WarWeariness != 5 || player.Stability >= beforeStability)
            throw new Exception("長期戦で疲弊・安定度が変化しない");

        player.MakePeaceWith(state, rival);
        AdministrationSystem.AdvancePlayer(state, player);
        if (player.WarWeariness != 3)
            throw new Exception("平和時の戦争疲弊回復が不正");

        player.Stability = 20;
        player.Treasury = 500;
        if (AdministrationSystem.RecommendTaxPolicy(player) != TaxPolicy.Low)
            throw new Exception("低安定度AIが減税を選ばない");
        player.Stability = 60;
        player.Treasury = -1;
        if (AdministrationSystem.RecommendTaxPolicy(player) != TaxPolicy.High)
            throw new Exception("赤字AIが重税を選ばない");

        Debug.Log("[Administration] 戦争疲弊・安定度・AI税制判断OK");
    }

    static void ValidateSaveVersion10AndBackwardDefault()
    {
        var state = BuildState();
        var player = state.Players[0];
        player.Treasury = 777;
        player.Taxation = TaxPolicy.High;
        player.Stability = 42;
        player.WarWeariness = 17;
        player.LastRevenue = 33;
        player.LastExpenses = 12;
        state.LastSavedAtIso = "2026-07-22T12:00:00";

        string json1 = SaveLoad.Serialize(state);
        if (!json1.Contains("\"version\":15"))
            throw new Exception("セーブversion 15ではない");
        var restored = SaveLoad.Deserialize(json1);
        string json2 = SaveLoad.Serialize(restored);
        string json3 = SaveLoad.Serialize(SaveLoad.Deserialize(json2));
        if (json2 != json3) throw new Exception("国家運営を含むセーブ往復が非決定的");
        var restoredPlayer = restored.GetPlayer(player.Id);
        if (restoredPlayer.Treasury != 777 || restoredPlayer.Taxation != TaxPolicy.High ||
            restoredPlayer.Stability != 42 || restoredPlayer.WarWeariness != 17 ||
            restoredPlayer.LastRevenue != 33 || restoredPlayer.LastExpenses != 12)
            throw new Exception("国家運営値の復元に失敗");

        // バージョン9相当（追加6フィールドなし）は初期値で安全に移行する。
        string old = json1.Replace("\"version\":15", "\"version\":9");
        string[] fields = { "treasury", "taxPolicy", "stability", "warWeariness", "lastRevenue", "lastExpenses" };
        for (int i = 0; i < fields.Length; i++)
            old = Regex.Replace(old, ",\"" + fields[i] + "\":-?[0-9]+", "");
        var migrated = SaveLoad.Deserialize(old).GetPlayer(player.Id);
        if (migrated.Treasury != AdministrationSystem.StartingTreasury ||
            migrated.Taxation != TaxPolicy.Balanced ||
            migrated.Stability != AdministrationSystem.StartingStability ||
            migrated.WarWeariness != 0)
            throw new Exception("バージョン9セーブの既定値移行に失敗");

        Debug.Log("[Administration] セーブv10決定往復・v9既定値移行OK");
    }

    static GameState BuildState()
    {
        var state = new GameState
        {
            Config = new GameConfig
            {
                MapWidth = 6,
                MapHeight = 4,
                NumPlayers = 2,
                Seed = 7210,
                MaxTurns = 250,
                HumanPlayerIndex = 0,
            },
            Map = new HexMap(6, 4),
            Rng = new System.Random(7210),
            TurnNumber = 10,
        };
        var first = new Player
        {
            Id = 0,
            CivilizationId = "egypt",
            NameJa = "エジプト",
            LeaderId = LeaderCatalog.DefaultForCivilization("egypt").Id,
            Color = Color.yellow,
            IsHuman = true,
            CapitalCityId = 1,
        };
        var second = new Player
        {
            Id = 1,
            CivilizationId = "sumer",
            NameJa = "シュメール",
            LeaderId = LeaderCatalog.DefaultForCivilization("sumer").Id,
            Color = Color.red,
            CapitalCityId = 2,
        };
        var firstCity = new City
        {
            Id = 1,
            PlayerId = 0,
            NameJa = "テーベ",
            Coord = HexCoord.FromOffset(1, 1),
            Population = 4,
            Buildings = new List<string> { "monument" },
        };
        var secondCity = new City
        {
            Id = 2,
            PlayerId = 1,
            NameJa = "ウル",
            Coord = HexCoord.FromOffset(4, 2),
            Population = 3,
        };
        first.Cities.Add(firstCity);
        second.Cities.Add(secondCity);
        first.Units.Add(new Unit { Id = 1, PlayerId = 0, DefId = "warrior", Coord = firstCity.Coord });
        first.Units.Add(new Unit { Id = 2, PlayerId = 0, DefId = "settler", Coord = HexCoord.FromOffset(2, 1) });
        state.Players.Add(first);
        state.Players.Add(second);
        state.Map.Get(firstCity.Coord).City = firstCity;
        state.Map.Get(secondCity.Coord).City = secondCity;
        return state;
    }
}
