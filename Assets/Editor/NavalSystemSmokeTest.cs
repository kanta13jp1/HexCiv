using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

public static class NavalSystemSmokeTest
{
    public static void Run()
    {
        try
        {
            VerifyTechnologyAndProduction();
            VerifyDomainsCombatBlockadeAndSave();
            Debug.Log("NAVAL SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("NAVAL SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void VerifyTechnologyAndProduction()
    {
        UnitDef galleyDef = GameRules.GetUnit("galley");
        UnitDef triremeDef = GameRules.GetUnit("trireme");
        Require(galleyDef.IsNaval && triremeDef.IsNaval,
            "艦船ユニットに海上領域が設定されていない");
        Require(GameRules.GetTech("sailing").Prereqs.Length == 1 &&
            GameRules.GetTech("navigation").Prereqs.Length == 2,
            "帆走・航海術の前提が不正");
        Require(GameRules.GetBuilding("harbor").RequiresTech == "sailing" &&
            GameRules.GetBuilding("convoy_office").RequiresTech == "navigation",
            "港湾建物の解禁技術が不正");

        GameState state = CreateState();
        Player player = state.Players[0];
        City port = player.Cities[0];
        List<ProductionItem> items = port.AvailableProduction(state);
        Require(ContainsUnit(items, "galley") && ContainsUnit(items, "trireme"),
            "港湾都市で艦船を生産できない");

        port.Buildings.Remove("harbor");
        items = port.AvailableProduction(state);
        Require(!ContainsUnit(items, "galley") && !ContainsUnit(items, "trireme"),
            "港なしで艦船を生産できる");
        port.Buildings.Add("harbor");

        port.CurrentProduction = ProductionItem.FromUnit(galleyDef);
        port.ProductionStored = galleyDef.Cost;
        port.ProcessTurnStart(state);
        Unit built = FindUnit(player, "galley");
        Require(built != null, "ガレー船が完成しない");
        Require(state.Map.Get(built.Coord).IsWater,
            "完成した艦船が水域以外へ配置された");
    }

    static void VerifyDomainsCombatBlockadeAndSave()
    {
        GameState state = CreateState();
        Player player = state.Players[0];
        Player enemy = state.Players[1];
        player.AtWarWith.Add(enemy.Id);
        enemy.AtWarWith.Add(player.Id);

        City port = player.Cities[0];
        Tile homeWater = FirstEmptyWaterNeighbor(state, port.Coord);
        Unit galley = state.CreateUnit(player, "galley", homeWater.Coord);
        Tile seaStep = FirstEmptyWaterNeighbor(state, galley.Coord);
        Require(seaStep != null && galley.TryStepTo(state, seaStep.Coord),
            "艦船が隣接水域へ移動できない");
        Require(Pathfinder.FindPath(state, galley, port.Coord) == null,
            "艦船が陸上都市へ上陸できる");

        Unit warrior = state.CreateUnit(player, "warrior", port.Coord);
        Require(Pathfinder.FindPath(state, warrior, galley.Coord) == null,
            "陸軍が水域へ進入できる");

        Tile enemyWater = FirstEmptyWaterNeighbor(state, galley.Coord);
        Require(enemyWater != null, "海戦試験用の隣接水域がない");
        Unit trireme = state.CreateUnit(enemy, "trireme", enemyWater.Coord);
        Require(Combat.CanAttack(state, galley, enemyWater),
            "艦船同士で海戦できない");
        Require(!Combat.CanAttack(state, warrior, enemyWater),
            "陸軍が海上艦船を攻撃できる");
        Require(!Combat.CanAttack(state, galley, state.Map.Get(port.Coord)),
            "艦船が陸上都市を直接占領できる");

        // 敵艦2圧力 - 味方護衛1 = 実効1。護送船団庁なら突破、護衛を外すと実効2で遮断。
        port.Buildings.Add("convoy_office");
        Tile choke = CommonWaterNeighbor(state, galley.Coord, trireme.Coord);
        Require(choke != null, "封鎖試験用の共通水域がない");
        Require(LogisticsSystem.EffectiveBlockadePressure(state, player, choke) == 1,
            "艦船の封鎖圧力と護衛相殺が不正");
        Require(!LogisticsSystem.IsWaterBlockaded(state, player, choke),
            "護送船団と味方艦が敵艦封鎖を突破できない");

        Tile galleyTile = state.Map.Get(galley.Coord);
        galleyTile.Unit = null;
        player.Units.Remove(galley);
        Require(LogisticsSystem.IsWaterBlockaded(state, player, choke),
            "護衛艦なしでも敵艦封鎖を突破している");
        player.Units.Add(galley);
        galleyTile.Unit = galley;

        state.LastSavedAtIso = "2026-07-23T00:00:00";
        string json = SaveLoad.Serialize(state);
        GameState restored = SaveLoad.Deserialize(json);
        Unit restoredGalley = FindUnit(restored.GetPlayer(player.Id), "galley");
        Require(restoredGalley != null && restoredGalley.Def.IsNaval &&
            restored.Map.Get(restoredGalley.Coord).IsWater,
            "セーブ復元後に艦船領域または配置が失われた");
    }

    static GameState CreateState()
    {
        var map = new HexMap(8, 6);
        foreach (Tile tile in map.AllTiles) tile.Terrain = TerrainType.Ocean;
        HexCoord portCoord = HexCoord.FromOffset(2, 2);
        map.Get(portCoord).Terrain = TerrainType.Plains;

        var player = new Player
        {
            Id = 0,
            NameJa = "海洋文明",
            CivilizationId = "egypt",
            LeaderId = LeaderCatalog.DefaultForCivilization("egypt").Id,
            Color = Color.blue,
            IsHuman = true,
            CapitalCityId = 1,
        };
        player.KnownTechs.Add("agriculture");
        player.KnownTechs.Add("pottery");
        player.KnownTechs.Add("sailing");
        player.KnownTechs.Add("mathematics");
        player.KnownTechs.Add("navigation");
        var enemy = new Player
        {
            Id = 1,
            NameJa = "敵海軍",
            CivilizationId = "sumer",
            LeaderId = LeaderCatalog.DefaultForCivilization("sumer").Id,
            Color = Color.red,
        };
        var city = new City
        {
            Id = 1,
            PlayerId = player.Id,
            NameJa = "母港",
            Coord = portCoord,
            Population = 2,
            Buildings = new List<string> { "harbor" },
        };
        player.Cities.Add(city);
        Tile portTile = map.Get(portCoord);
        portTile.City = city;
        portTile.OwnerPlayerId = player.Id;
        portTile.OwnerCityId = city.Id;

        var state = new GameState
        {
            Map = map,
            Config = new GameConfig { MapWidth = 8, MapHeight = 6, NumPlayers = 2 },
            Rng = new System.Random(93),
            TurnNumber = 12,
        };
        state.Players.Add(player);
        state.Players.Add(enemy);
        return state;
    }

    static Tile FirstEmptyWaterNeighbor(GameState state, HexCoord coord)
    {
        foreach (Tile tile in state.Map.NeighborsOf(coord))
            if (tile.IsWater && tile.Unit == null && tile.City == null) return tile;
        return null;
    }

    static Tile CommonWaterNeighbor(GameState state, HexCoord a, HexCoord b)
    {
        foreach (Tile tile in state.Map.NeighborsOf(a))
            if (tile.IsWater && tile.Coord.DistanceTo(b) == 1 &&
                tile.Unit == null && tile.City == null) return tile;
        return null;
    }

    static bool ContainsUnit(List<ProductionItem> items, string id)
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i].Kind == ProductionKind.Unit && items[i].Id == id) return true;
        return false;
    }

    static Unit FindUnit(Player player, string defId)
    {
        if (player == null) return null;
        for (int i = 0; i < player.Units.Count; i++)
            if (player.Units[i] != null && !player.Units[i].IsDead &&
                player.Units[i].DefId == defId) return player.Units[i];
        return null;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
