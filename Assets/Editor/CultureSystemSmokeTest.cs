using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HexCiv.Core;

/// <summary>文化史132件の政策化、進行、交流、勝利、セーブ往復を検証する。</summary>
public static class CultureSystemSmokeTest
{
    static readonly string[] Regions =
    {
        "アフリカ", "西・南アジア", "東・東南アジア",
        "ヨーロッパ・地中海", "アメリカ大陸", "オセアニア"
    };

    public static void Run()
    {
        try
        {
            ValidateCatalog();
            ValidateProgressAndEffects();
            ValidateExchangeVictoryAndSave();
            Debug.Log("CULTURE SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("CULTURE SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateCatalog()
    {
        if (CulturePolicyCatalog.All.Count != 132)
            throw new Exception("文化政策件数が不正: " + CulturePolicyCatalog.All.Count);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < CulturePolicyCatalog.All.Count; i++)
        {
            var policy = CulturePolicyCatalog.All[i];
            if (!ids.Add(policy.Id)) throw new Exception("文化政策ID重複: " + policy.Id);
            if (policy.Tradition == null || policy.Cost <= 0 ||
                string.IsNullOrEmpty(policy.EffectTextJa))
                throw new Exception("文化政策の必須情報不足: " + policy.Id);
            if (CulturePolicyCatalog.Get(policy.Id) != policy)
                throw new Exception("文化政策ID検索の参照不一致: " + policy.Id);
            if (CulturePolicyCatalog.TraditionForPolicy(policy.Id) != policy.Tradition)
                throw new Exception("文化史との対応不一致: " + policy.Id);
        }

        for (int r = 0; r < Regions.Length; r++)
        {
            var branch = CulturePolicyCatalog.ForRegion(Regions[r]);
            if (branch.Count != 22)
                throw new Exception(Regions[r] + "の政策数が不正: " + branch.Count);
            for (int tier = 0; tier < branch.Count; tier++)
            {
                int expectedCost = CulturePolicyCatalog.BaseCost + tier * CulturePolicyCatalog.TierCost;
                if (branch[tier].Cost != expectedCost)
                    throw new Exception("文化政策コスト不正: " + branch[tier].Id);
                if (tier == 0 && branch[tier].Prereqs.Length != 0)
                    throw new Exception("地域ルートに前提がある: " + branch[tier].Id);
                if (tier > 0 && (branch[tier].Prereqs.Length != 1 ||
                    branch[tier].Prereqs[0] != branch[tier - 1].Id))
                    throw new Exception("地域内前提が不正: " + branch[tier].Id);
            }
        }
        Debug.Log("[Culture] 文化史132件・6地域×22政策・前提・コストOK");
    }

    static void ValidateProgressAndEffects()
    {
        var state = BuildState();
        var player = state.Players[0];
        var branch = CulturePolicyCatalog.ForRegion(Regions[0]);
        player.SetCulturePolicy(branch[0].Id);
        player.CultureStored = branch[0].Cost;
        CultureSystem.AdvancePlayer(state, player);
        if (!player.KnownCulturePolicies.Contains(branch[0].Id) ||
            player.CurrentCulturePolicyId != null || player.TotalCulture <= 0)
            throw new Exception("文化ポイントによる政策採用に失敗");

        player.KnownCulturePolicies.Add(branch[1].Id); // 科学+1%
        player.KnownCulturePolicies.Add(branch[2].Id); // 生産+1%
        if (CultureSystem.ScaleScience(player, 100) != 101)
            throw new Exception("文化政策の科学効果が不正");
        if (CultureSystem.ScaleProduction(player, 100) != 101)
            throw new Exception("文化政策の生産効果が不正");
        if (player.AvailableCulturePolicies().Count == 0)
            throw new Exception("次の文化政策が解禁されていない");
        Debug.Log("[Culture] 文化産出・政策採用・科学/生産効果OK");
    }

    static void ValidateExchangeVictoryAndSave()
    {
        var state = BuildState();
        var player = state.Players[0];
        var rival = state.Players[1];

        CultureSystem.AdvanceExchange(state);
        if (CultureSystem.InfluenceOn(player, rival) <= 0 ||
            CultureSystem.InfluenceOn(rival, player) <= 0)
            throw new Exception("平和時の文化交流が進行していない");

        player.DeclareWarOn(state, rival);
        int before = CultureSystem.InfluenceOn(player, rival);
        CultureSystem.AdvanceExchange(state);
        if (CultureSystem.InfluenceOn(player, rival) != before)
            throw new Exception("戦争中も文化交流が進行した");
        player.MakePeaceWith(state, rival);

        for (int i = 0; i < CultureSystem.VictoryMinimumPolicies; i++)
            player.KnownCulturePolicies.Add(CulturePolicyCatalog.All[i].Id);
        player.CultureStored = 47;
        player.TotalCulture = CultureSystem.VictoryMinimumCulture + 500;
        rival.TotalCulture = 100;
        player.CulturalInfluence[rival.Id] = CultureSystem.VictoryThreshold(rival);
        state.TurnNumber = CultureSystem.VictoryMinimumTurn;

        string json1 = SaveLoad.Serialize(state);
        var restored = SaveLoad.Deserialize(json1);
        string json2 = SaveLoad.Serialize(restored);
        string json3 = SaveLoad.Serialize(SaveLoad.Deserialize(json2));
        // BuildStateはGameBootstrapを通さない最小状態なので、文明の派生情報などが
        // 1回目のロードで正規化される。正規化後の決定性はjson2==json3で検証する。
        // 本番BuildNewGame状態の1回往復一致は既存SmokeTestが別途保証する。
        if (json2 != json3) throw new Exception("文化進行の正規化後セーブ往復が不一致");
        var restoredPlayer = restored.GetPlayer(player.Id);
        var restoredRival = restored.GetPlayer(rival.Id);
        if (restoredPlayer.KnownCulturePolicies.Count != player.KnownCulturePolicies.Count ||
            restoredPlayer.CultureStored != 47 ||
            restoredPlayer.TotalCulture != CultureSystem.VictoryMinimumCulture + 500 ||
            CultureSystem.InfluenceOn(restoredPlayer, restoredRival) !=
                CultureSystem.VictoryThreshold(restoredRival))
            throw new Exception("文化進行の復元値が不正");

        if (!CultureSystem.CheckCulturalVictory(restored) || restored.Winner != restoredPlayer ||
            string.IsNullOrEmpty(restored.GameOverMessageJa) ||
            !restored.GameOverMessageJa.Contains("文化勝利"))
            throw new Exception("文化勝利判定に失敗");

        Debug.Log("[Culture] 平和交流・戦時停止・文化勝利・セーブv7往復OK");
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
                Seed = 123,
                HumanPlayerIndex = 0,
            },
            Map = new HexMap(6, 4),
            Rng = new System.Random(123),
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
            Buildings = new List<string> { "monument" },
        };
        first.Cities.Add(firstCity);
        second.Cities.Add(secondCity);
        state.Players.Add(first);
        state.Players.Add(second);
        state.Map.Get(firstCity.Coord).City = firstCity;
        state.Map.Get(secondCity.Coord).City = secondCity;
        return state;
    }
}
