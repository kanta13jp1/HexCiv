using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using HexCiv.Core;

/// <summary>補給到達、敵遮断、技術・拠点、孤立消耗、戦闘補正、セーブ互換を検証する。</summary>
public static class LogisticsSystemSmokeTest
{
    public static void Run()
    {
        try
        {
            ValidateSupplyReachAndInfrastructure();
            ValidateEnemyCutoffAndAttrition();
            ValidateSaveVersion11AndBackwardDefault();
            Debug.Log("LOGISTICS SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("LOGISTICS SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateSupplyReachAndInfrastructure()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        Unit near = player.Units[0];
        Unit edge = player.Units[1];
        Unit far = player.Units[2];
        var costs = LogisticsSystem.BuildSupplyCosts(state, player);

        if (LogisticsSystem.LevelAt(player, near.Coord, costs) != SupplyLevel.Supplied)
            throw new Exception("近距離ユニットが補給されない");
        if (LogisticsSystem.LevelAt(player, edge.Coord, costs) != SupplyLevel.Strained)
            throw new Exception("補給限界ユニットが逼迫にならない");
        if (LogisticsSystem.LevelAt(player, far.Coord, costs) != SupplyLevel.Isolated)
            throw new Exception("遠距離ユニットが孤立しない");

        player.KnownTechs.Add("wheel");
        player.KnownTechs.Add("construction");
        player.Cities[0].Buildings.Add("granary");
        costs = LogisticsSystem.BuildSupplyCosts(state, player);
        if (LogisticsSystem.SupplyRange(player) != 9)
            throw new Exception("車輪・建築学の補給距離が不正");
        if (LogisticsSystem.LevelAt(player, edge.Coord, costs) != SupplyLevel.Supplied)
            throw new Exception("交通技術と穀物庫が補給を改善しない");

        Debug.Log("[Logistics] 都市補給・地形コスト・技術・穀物庫OK");
    }

    static void ValidateEnemyCutoffAndAttrition()
    {
        GameState state = BuildState();
        Player player = state.Players[0];
        Player enemy = state.Players[1];
        player.AtWarWith.Add(enemy.Id);
        enemy.AtWarWith.Add(player.Id);

        var blocker = new Unit
        {
            Id = 20,
            PlayerId = enemy.Id,
            DefId = "warrior",
            Coord = HexCoord.FromOffset(3, 0),
            MovesLeft = 2,
        };
        enemy.Units.Add(blocker);
        state.Map.Get(blocker.Coord).Unit = blocker;

        Unit cutOff = player.Units[0]; // 敵遮断点の先、col=4
        var costs = LogisticsSystem.BuildSupplyCosts(state, player);
        if (LogisticsSystem.LevelAt(player, cutOff.Coord, costs) != SupplyLevel.Isolated)
            throw new Exception("敵部隊が一本の補給線を遮断しない");

        Unit far = player.Units[2];
        LogisticsSystem.AdvancePlayer(state, player);
        if (far.Supply != SupplyLevel.Isolated || far.TurnsOutOfSupply != 1 || far.Hp != 100)
            throw new Exception("孤立初ターンの猶予が不正");
        LogisticsSystem.AdvancePlayer(state, player);
        if (far.TurnsOutOfSupply != 2 || far.Hp != 95)
            throw new Exception("孤立継続の消耗が不正");

        far.Hp = 50;
        far.ActedThisTurn = false;
        far.ResetForNewTurn(state);
        if (far.Hp != 50 || far.MovesLeft != 1)
            throw new Exception("孤立時の回復停止・移動低下が不正");
        if (Math.Abs(LogisticsSystem.ScaleCombat(far, 20f) - 15f) > 0.001f)
            throw new Exception("孤立時の戦闘力補正が不正");

        Debug.Log("[Logistics] 敵遮断・猶予・消耗・回復・移動・戦闘補正OK");
    }

    static void ValidateSaveVersion11AndBackwardDefault()
    {
        GameState state = BuildState();
        Unit unit = state.Players[0].Units[2];
        unit.Supply = SupplyLevel.Isolated;
        unit.TurnsOutOfSupply = 3;
        state.LastSavedAtIso = "2026-07-22T18:00:00";

        string json1 = SaveLoad.Serialize(state);
        if (!json1.Contains("\"version\":15"))
            throw new Exception("セーブversion 15ではない");
        GameState restored = SaveLoad.Deserialize(json1);
        string json2 = SaveLoad.Serialize(restored);
        string json3 = SaveLoad.Serialize(SaveLoad.Deserialize(json2));
        if (json2 != json3) throw new Exception("兵站を含むセーブ往復が非決定的");
        Unit restoredUnit = restored.GetPlayer(0).Units.Find(u => u.Id == unit.Id);
        if (restoredUnit == null || restoredUnit.Supply != SupplyLevel.Isolated ||
            restoredUnit.TurnsOutOfSupply != 3)
            throw new Exception("補給状態の復元に失敗");

        string old = json1.Replace("\"version\":15", "\"version\":10");
        old = Regex.Replace(old, ",\"supplyLevel\":-?[0-9]+", "");
        old = Regex.Replace(old, ",\"turnsOutOfSupply\":-?[0-9]+", "");
        GameState migrated = SaveLoad.Deserialize(old);
        Unit migratedUnit = migrated.GetPlayer(0).Units.Find(u => u.Id == unit.Id);
        if (migratedUnit == null || migratedUnit.Supply != SupplyLevel.Supplied ||
            migratedUnit.TurnsOutOfSupply != 0)
            throw new Exception("バージョン10セーブの補給既定値移行に失敗");

        Debug.Log("[Logistics] セーブv11決定往復・v10既定値移行OK");
    }

    static GameState BuildState()
    {
        var state = new GameState
        {
            Config = new GameConfig
            {
                MapWidth = 14,
                MapHeight = 1,
                NumPlayers = 2,
                Seed = 7221,
                MaxTurns = 250,
                HumanPlayerIndex = 0,
            },
            Map = new HexMap(14, 1),
            Rng = new System.Random(7221),
            TurnNumber = 20,
        };
        foreach (Tile tile in state.Map.AllTiles) tile.Terrain = TerrainType.Plains;

        var player = new Player
        {
            Id = 0,
            CivilizationId = "egypt",
            NameJa = "エジプト",
            LeaderId = LeaderCatalog.DefaultForCivilization("egypt").Id,
            Color = Color.yellow,
            IsHuman = true,
            CapitalCityId = 1,
        };
        var enemy = new Player
        {
            Id = 1,
            CivilizationId = "sumer",
            NameJa = "シュメール",
            LeaderId = LeaderCatalog.DefaultForCivilization("sumer").Id,
            Color = Color.red,
            CapitalCityId = -1,
        };
        var city = new City
        {
            Id = 1,
            PlayerId = player.Id,
            NameJa = "テーベ",
            Coord = HexCoord.FromOffset(0, 0),
            Population = 4,
            Buildings = new List<string>(),
        };
        player.Cities.Add(city);
        state.Map.Get(city.Coord).City = city;
        for (int col = 0; col <= 4; col++)
        {
            Tile tile = state.Map.Get(HexCoord.FromOffset(col, 0));
            tile.OwnerPlayerId = player.Id;
            tile.OwnerCityId = city.Id;
        }

        AddUnit(state, player, 1, 4);
        AddUnit(state, player, 2, 7);
        AddUnit(state, player, 3, 11);
        state.Players.Add(player);
        state.Players.Add(enemy);
        return state;
    }

    static void AddUnit(GameState state, Player player, int id, int col)
    {
        var unit = new Unit
        {
            Id = id,
            PlayerId = player.Id,
            DefId = "warrior",
            Coord = HexCoord.FromOffset(col, 0),
            Hp = 100,
            MovesLeft = 2,
        };
        player.Units.Add(unit);
        state.Map.Get(unit.Coord).Unit = unit;
    }
}
