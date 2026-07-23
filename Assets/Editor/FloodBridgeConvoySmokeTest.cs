using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

public static class FloodBridgeConvoySmokeTest
{
    public static void Run()
    {
        try
        {
            VerifySeasonalFloodAndBridgeworks();
            VerifyBlockadeAndConvoys();
            Debug.Log("FLOOD BRIDGE CONVOY SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("FLOOD BRIDGE CONVOY SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void VerifySeasonalFloodAndBridgeworks()
    {
        HexMap map = LandMap(7, 7);
        HexCoord riverCoord = HexCoord.FromOffset(3, 3);
        HexCoord bankCoord = riverCoord.Neighbor(2);
        HexCoord cityCoord = riverCoord.Neighbor(3);
        Tile river = map.Get(riverCoord);
        river.Terrain = TerrainType.Plains;
        river.HasRiver = true;
        river.HasFloodplain = true;
        river.RiverOutflowDirection = 0;
        map.Get(riverCoord.Neighbor(0)).HasRiver = true;

        var player = new Player
        {
            Id = 0,
            NameJa = "河川文明",
            CivilizationId = "egypt",
            LeaderId = LeaderCatalog.DefaultForCivilization("egypt").Id,
            Color = Color.yellow,
            IsHuman = true,
            CapitalCityId = 1,
        };
        player.KnownTechs.Add("construction");
        var city = new City
        {
            Id = 1,
            PlayerId = player.Id,
            NameJa = "河港",
            Coord = cityCoord,
            Population = 2,
            Buildings = new List<string>(),
        };
        player.Cities.Add(city);
        map.Get(cityCoord).City = city;
        map.Get(cityCoord).OwnerPlayerId = player.Id;
        map.Get(cityCoord).OwnerCityId = city.Id;

        var state = new GameState
        {
            Map = map,
            Config = new GameConfig { MapWidth = 7, MapHeight = 7, NumPlayers = 2 },
            Rng = new System.Random(81),
            TurnNumber = 1,
        };
        state.Players.Add(player);
        var unit = new Unit
        {
            Id = 1,
            PlayerId = player.Id,
            DefId = "warrior",
            Coord = bankCoord,
            MovesLeft = 2,
        };

        Require(NaturalGeographySystem.FloodStageAt(1) == FloodStage.Inundated &&
            NaturalGeographySystem.FloodStageAt(3) == FloodStage.Fertile &&
            NaturalGeographySystem.FloodStageAt(6) == FloodStage.Normal &&
            NaturalGeographySystem.FloodStageAt(13) == FloodStage.Inundated,
            "12ターンの洪水季節サイクルが不正");
        Require(NaturalGeographySystem.YieldsAt(state, river).Food == 2,
            "増水期の氾濫原食料が2ではない");
        Require(NaturalGeographySystem.MovementCost(state, unit, bankCoord, riverCoord) == 3,
            "増水期の未架橋渡河コストが3ではない");

        state.TurnNumber = 3;
        Require(NaturalGeographySystem.YieldsAt(state, river).Food == 4,
            "肥沃期の氾濫原食料が4ではない");
        state.TurnNumber = 6;
        Require(NaturalGeographySystem.YieldsAt(state, river).Food == 3,
            "平常期の氾濫原食料が3ではない");
        Require(NaturalGeographySystem.MovementCost(state, unit, bankCoord, riverCoord) == 2,
            "建築学だけで渡河ペナルティが消えた");

        List<ProductionItem> available = city.AvailableProduction(state);
        Require(ContainsBuilding(available, "bridgeworks"), "河川圏都市で橋梁網を生産できない");
        Require(!ContainsBuilding(available, "convoy_office"), "港なしで護送船団庁を生産できる");
        city.Buildings.Add("harbor");
        Require(ContainsBuilding(city.AvailableProduction(state), "convoy_office"),
            "港完成後も護送船団庁を生産できない");

        city.Buildings.Add("bridgeworks");
        Require(NaturalGeographySystem.HasBridgeCoverage(state, player, bankCoord, riverCoord),
            "都市圏の橋梁網が渡河地点を覆わない");
        Require(NaturalGeographySystem.MovementCost(state, unit, bankCoord, riverCoord) == 1,
            "橋梁網完成後も渡河コストが残る");
        Require(Math.Abs(NaturalGeographySystem.RiverCrossingAttackMultiplier(
            state, unit, riverCoord) - 1f) < 0.001f,
            "橋梁網完成後も渡河攻撃補正が残る");

        city.Buildings.Add("convoy_office");
        var conqueror = new Player
        {
            Id = 1,
            NameJa = "征服文明",
            CivilizationId = "sumer",
            LeaderId = LeaderCatalog.DefaultForCivilization("sumer").Id,
            Color = Color.red,
        };
        state.Players.Add(conqueror);
        state.CaptureCity(city, conqueror);
        Require(!city.Buildings.Contains("bridgeworks") &&
            !city.Buildings.Contains("convoy_office"),
            "占領後も橋梁網または護送船団指揮系統が残る");
    }

    static void VerifyBlockadeAndConvoys()
    {
        var map = new HexMap(8, 5);
        foreach (Tile tile in map.AllTiles) tile.Terrain = TerrainType.Mountain;
        HexCoord cityCoord = HexCoord.FromOffset(1, 2);
        HexCoord landingCoord = HexCoord.FromOffset(6, 2);
        for (int col = 2; col <= 5; col++)
            map.Get(HexCoord.FromOffset(col, 2)).Terrain = TerrainType.Coast;
        map.Get(cityCoord).Terrain = TerrainType.Plains;
        map.Get(landingCoord).Terrain = TerrainType.Plains;

        var player = new Player
        {
            Id = 0,
            NameJa = "海洋文明",
            CivilizationId = "egypt",
            LeaderId = LeaderCatalog.DefaultForCivilization("egypt").Id,
            Color = Color.yellow,
            CapitalCityId = 1,
        };
        var enemy = new Player
        {
            Id = 1,
            NameJa = "封鎖文明",
            CivilizationId = "sumer",
            LeaderId = LeaderCatalog.DefaultForCivilization("sumer").Id,
            Color = Color.red,
        };
        player.AtWarWith.Add(enemy.Id);
        enemy.AtWarWith.Add(player.Id);

        var city = new City
        {
            Id = 1,
            PlayerId = player.Id,
            NameJa = "母港",
            Coord = cityCoord,
            Buildings = new List<string> { "harbor" },
        };
        player.Cities.Add(city);
        Tile cityTile = map.Get(cityCoord);
        cityTile.City = city;
        cityTile.OwnerPlayerId = player.Id;
        cityTile.OwnerCityId = city.Id;
        Tile landing = map.Get(landingCoord);
        landing.OwnerPlayerId = player.Id;
        landing.OwnerCityId = city.Id;

        var state = new GameState
        {
            Map = map,
            Config = new GameConfig { MapWidth = 8, MapHeight = 5, NumPlayers = 2 },
            Rng = new System.Random(82),
            TurnNumber = 6,
        };
        state.Players.Add(player);
        state.Players.Add(enemy);

        HexCoord choke = HexCoord.FromOffset(4, 2);
        var flankTiles = new List<Tile>();
        foreach (Tile neighbor in map.NeighborsOf(choke))
            if (!neighbor.IsWater && neighbor.Coord != cityCoord &&
                neighbor.Coord != landingCoord) flankTiles.Add(neighbor);
        Require(flankTiles.Count >= 2, "封鎖試験用の沿岸タイルが不足");
        AddEnemyUnit(state, enemy, 10, flankTiles[0]);

        Require(LogisticsSystem.IsWaterBlockaded(state, player, map.Get(choke)),
            "単独の敵沿岸戦力が海域を封鎖しない");
        Require(!LogisticsSystem.BuildSupplyCosts(state, player).ContainsKey(landingCoord),
            "封鎖中でも海上補給が対岸へ届く");

        city.Buildings.Add("convoy_office");
        Require(LogisticsSystem.HasConvoyNetwork(player), "護送船団網が有効にならない");
        Require(!LogisticsSystem.IsWaterBlockaded(state, player, map.Get(choke)),
            "護送船団が単独封鎖を突破できない");
        Require(LogisticsSystem.BuildSupplyCosts(state, player).ContainsKey(landingCoord),
            "護送船団ありでも対岸へ補給が届かない");

        AddEnemyUnit(state, enemy, 11, flankTiles[1]);
        Require(LogisticsSystem.IsWaterBlockaded(state, player, map.Get(choke)),
            "二重封鎖を護送船団だけで突破している");
        Require(!LogisticsSystem.BuildSupplyCosts(state, player).ContainsKey(landingCoord),
            "二重封鎖中でも対岸へ補給が届く");

        player.AtWarWith.Clear();
        enemy.AtWarWith.Clear();
        Require(LogisticsSystem.BuildSupplyCosts(state, player).ContainsKey(landingCoord),
            "和平後も沿岸封鎖が解除されない");
    }

    static void AddEnemyUnit(GameState state, Player enemy, int id, Tile tile)
    {
        var unit = new Unit
        {
            Id = id,
            PlayerId = enemy.Id,
            DefId = "warrior",
            Coord = tile.Coord,
            Hp = 100,
            MovesLeft = 2,
        };
        enemy.Units.Add(unit);
        tile.Unit = unit;
    }

    static bool ContainsBuilding(List<ProductionItem> items, string id)
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i].Kind == ProductionKind.Building && items[i].Id == id) return true;
        return false;
    }

    static HexMap LandMap(int width, int height)
    {
        var map = new HexMap(width, height);
        foreach (Tile tile in map.AllTiles) tile.Terrain = TerrainType.Grassland;
        return map;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
