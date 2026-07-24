using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HexCiv.Campaigns;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>ウルク史実キャンペーン第1マイルストーンの純データ・固定マップ・セーブ検証。</summary>
public static class HistoricalCampaignFoundationSmokeTest
{
    [MenuItem("HexCiv/Run Historical Campaign Foundation Smoke Test")]
    public static void Run()
    {
        try
        {
            var definition = HistoricalCampaignRepository.LoadBuiltIn(
                HistoricalCampaignRepository.Uruk4000Id);
            ValidateDefinition(definition);
            ValidateMap(definition);
            ValidateSession(definition);
            ValidateSave(definition);
            Debug.Log("HISTORICAL CAMPAIGN FOUNDATION SMOKE OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("HISTORICAL CAMPAIGN FOUNDATION SMOKE FAIL: " + ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static void ValidateDefinition(HistoricalCampaignDefinition definition)
    {
        var errors = HistoricalCampaignValidator.Validate(definition);
        if (errors.Count != 0)
            throw new Exception("定義検証エラー: " + string.Join(" | ", errors));
        if (definition.id != "uruk_4000" || definition.schemaVersion != 1 ||
            definition.datasetVersion != 1 || definition.reviewStatus != "verified")
            throw new Exception("キャンペーン安定ID・版数・確認状態が不正");
        if (definition.startYear != -4000 || definition.endYear != -3000 ||
            definition.maxTurns != 50)
            throw new Exception("ウルク縦切り版の年代・ターン数が不正");
        if (definition.mapWidth != 32 || definition.mapHeight != 20)
            throw new Exception("固定マップ規模が不正");
        if (definition.factions.Length != 8 || definition.factions.Count(f => f.human) != 1)
            throw new Exception("8勢力または人間勢力が不正");
        if (definition.factions[0].id != "uruk_community" ||
            !definition.factions[0].human)
            throw new Exception("ウルクが先頭の人間勢力ではない");
        if (definition.goods.Length < 8 ||
            !definition.goods.Any(g => g.id == "barley") ||
            !definition.goods.Any(g => g.id == "reeds") ||
            !definition.goods.Any(g => g.id == "alluvial_clay") ||
            !definition.goods.Any(g => g.id == "copper"))
            throw new Exception("実在物資台帳が不足");
        if (definition.sources.Length < 6)
            throw new Exception("出典台帳が不足");
        if (definition.sources.Any(s => s.reviewStatus != "verified" ||
            s.usage != "reference_only" || string.IsNullOrWhiteSpace(s.accessedDate)))
            throw new Exception("出典の確認状態・用途・参照日が不足");
        if (definition.startingScenario == null ||
            definition.startingScenario.actualPopulation != 1500 ||
            definition.startingScenario.roles.Total != 1500 ||
            definition.startingScenario.statuses.enslaved != 0 ||
            definition.startingScenario.improvements.Count(i => i.kind == "farm") != 2)
            throw new Exception("小集落1,500人・農地2区画の開始条件が不正");
        if (HistoricalCampaignCalendar.YearAtTurnStart(definition, 1) != -4000 ||
            HistoricalCampaignCalendar.YearAtTurnEnd(definition, 1) != -3980 ||
            HistoricalCampaignCalendar.YearAtTurnStart(definition, 51) != -3000)
            throw new Exception("可変年代の変換が不正");
        if (HistoricalCampaignCalendar.TurnIntervalJa(definition, 1) !=
            "紀元前4000年～紀元前3980年")
            throw new Exception("年代日本語表示が不正");

        string originalStatus = definition.goods[0].reviewStatus;
        definition.goods[0].reviewStatus = "draft";
        if (!HistoricalCampaignValidator.Validate(definition)
            .Any(e => e.Contains("verified必須")))
            throw new Exception("未確認データが製品検証を通過した");
        definition.goods[0].reviewStatus = originalStatus;
    }

    static void ValidateMap(HistoricalCampaignDefinition definition)
    {
        var a = HistoricalCampaignFactory.BuildMap(definition);
        var b = HistoricalCampaignFactory.BuildMap(definition);
        if (MapFingerprint(a) != MapFingerprint(b))
            throw new Exception("固定マップが決定的でない");

        int water = a.AllTiles.Count(t => t.IsWater);
        int river = a.AllTiles.Count(t => t.HasRiver);
        int floodplain = a.AllTiles.Count(t => t.HasFloodplain);
        int mountain = a.AllTiles.Count(t => t.Terrain == TerrainType.Mountain);
        if (water < 40 || river < 20 || floodplain != river || mountain < 60)
            throw new Exception(
                $"固定地理の構成が不足: water={water} river={river} flood={floodplain} mountain={mountain}");

        foreach (var faction in definition.factions)
        {
            var tile = a.Get(HexCoord.FromOffset(faction.startCol, faction.startRow));
            if (tile == null || !tile.IsPassable || tile.IsWater)
                throw new Exception("勢力開始地が陸上通行可能でない: " + faction.id);
        }
        foreach (var riverDef in definition.rivers)
        {
            var last = riverDef.points[riverDef.points.Length - 1];
            var mouth = a.Get(HexCoord.FromOffset(last.col, last.row));
            if (mouth == null || !mouth.IsWater)
                throw new Exception("河川終端が水域でない: " + riverDef.id);
        }
    }

    static void ValidateSession(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var state = session.State;
        if (session.CampaignId != definition.id || state.Config.MaxTurns != 50 ||
            state.Config.MapWidth != 32 || state.Config.MapHeight != 20)
            throw new Exception("セッション設定が定義と不一致");
        if (state.Players.Count != 8 || state.AllCities.Count() != 8)
            throw new Exception("8勢力・8初期集落が構築されていない");
        if (state.HumanPlayer == null || state.HumanPlayer.NameJa != "ウルク共同体" ||
            state.HumanPlayer.Cities.Count != 1 ||
            state.HumanPlayer.Cities[0].NameJa != "ウルク")
            throw new Exception("人間ウルク勢力の構築が不正");

        var cityNames = new HashSet<string>();
        for (int i = 0; i < definition.factions.Length; i++)
        {
            var faction = definition.factions[i];
            var player = state.Players[i];
            if (player.NameJa != faction.name.ja ||
                player.LeaderNameJa != faction.leaderTitle.ja ||
                player.Cities[0].Population != faction.initialPopulation ||
                player.Cities[0].NameJa != faction.capitalName.ja)
                throw new Exception("勢力状態がJSONと不一致: " + faction.id);
            if (!cityNames.Add(player.Cities[0].NameJa))
                throw new Exception("初期集落名が重複");
        }
        if (session.CurrentYear != -4000 || session.CompletedTurns != 0)
            throw new Exception("初期セッション年代が不正");
        if (session.Progress.actualPopulation != 1500 ||
            session.Progress.statuses.enslaved != 0)
            throw new Exception("ウルク専用人口進捗が不正");
    }

    static void ValidateSave(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        session.State.TurnNumber = 7;
        string json = HistoricalCampaignSave.Serialize(session);
        if (!HistoricalCampaignSave.IsHistoricalCampaignSave(json))
            throw new Exception("史実キャンペーンセーブを識別できない");
        string ordinary = SaveLoad.Serialize(session.State);
        if (HistoricalCampaignSave.IsHistoricalCampaignSave(ordinary))
            throw new Exception("通常セーブを史実キャンペーンと誤認した");

        var loaded = HistoricalCampaignSave.Deserialize(json,
            id => id == definition.id ? definition : null);
        if (loaded.CampaignId != definition.id || loaded.State.TurnNumber != 7 ||
            loaded.CompletedTurns != 6 || loaded.State.Players.Count != 8 ||
            loaded.State.HumanPlayer == null ||
            loaded.State.HumanPlayer.Cities[0].NameJa != "ウルク")
            throw new Exception("史実キャンペーンセーブ往復が不正");
        string reserialized = HistoricalCampaignSave.Serialize(loaded);
        if (reserialized != json)
        {
            int mismatch = FirstMismatch(json, reserialized);
            throw new Exception(
                $"史実キャンペーンセーブが決定的でない: first={mismatch} " +
                $"before={json.Length} after={reserialized.Length} " +
                $"A={Slice(json, mismatch)} B={Slice(reserialized, mismatch)}");
        }
    }

    static string MapFingerprint(HexMap map)
    {
        var builder = new StringBuilder(map.Width * map.Height * 6);
        foreach (var tile in map.AllTiles)
            builder.Append((int)tile.Terrain).Append(':')
                .Append(tile.HasHill ? '1' : '0')
                .Append(tile.HasRiver ? '1' : '0')
                .Append(tile.HasFloodplain ? '1' : '0')
                .Append(tile.RiverOutflowDirection).Append(';');
        return builder.ToString();
    }

    static int FirstMismatch(string a, string b)
    {
        int length = Math.Min(a.Length, b.Length);
        for (int i = 0; i < length; i++)
            if (a[i] != b[i]) return i;
        return length;
    }

    static string Slice(string value, int index)
    {
        int start = Math.Max(0, index - 35);
        int length = Math.Min(value.Length - start, 90);
        return value.Substring(start, length);
    }
}
