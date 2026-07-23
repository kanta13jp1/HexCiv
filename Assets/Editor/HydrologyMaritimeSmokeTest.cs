using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

public static class HydrologyMaritimeSmokeTest
{
    public static void Run()
    {
        try
        {
            VerifyGeneratedHydrology();
            VerifyCrossingAndFloodplain();
            VerifyHarborSupplyAndSave();
            Debug.Log("HYDROLOGY MARITIME SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("HYDROLOGY MARITIME SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void VerifyGeneratedHydrology()
    {
        var config = new GameConfig
        {
            MapWidth = 40,
            MapHeight = 24,
            NumPlayers = 4,
            Seed = 42,
            HumanPlayerIndex = -1,
        };
        List<HexCoord> starts;
        HexMap map = MapGenerator.Generate(config, new System.Random(config.Seed), out starts);
        int riverTiles = 0;
        int mouths = 0;
        foreach (Tile tile in map.AllTiles)
        {
            if (!tile.HasRiver) continue;
            riverTiles++;
            var visited = new HashSet<HexCoord> { tile.Coord };
            Tile cursor = tile;
            bool reachedWater = false;
            for (int guard = 0; guard < map.Width * map.Height; guard++)
            {
                Tile downstream = NaturalGeographySystem.RiverDestination(map, cursor);
                Require(downstream != null, "河川に下流方向がない: " + cursor.Coord);
                if (downstream.IsWater)
                {
                    reachedWater = true;
                    mouths++;
                    break;
                }
                Require(downstream.HasRiver, "河川の下流が河川または水域ではない");
                Require(visited.Add(downstream.Coord), "河川流向に循環がある");
                cursor = downstream;
            }
            Require(reachedWater, "河川が水域へ到達しない");
        }
        Require(riverTiles > 0 && mouths > 0, "生成地図に検証可能な河川がない");
    }

    static void VerifyCrossingAndFloodplain()
    {
        var map = LandMap(7, 7);
        HexCoord riverCoord = HexCoord.FromOffset(3, 3);
        HexCoord downstreamCoord = riverCoord.Neighbor(0);
        HexCoord bankCoord = riverCoord.Neighbor(2);
        Tile river = map.Get(riverCoord);
        Tile downstream = map.Get(downstreamCoord);
        river.Terrain = TerrainType.Plains;
        river.HasRiver = true;
        river.RiverOutflowDirection = 0;
        downstream.HasRiver = true;
        downstream.RiverOutflowDirection = 0;
        Tile mouth = map.Get(downstreamCoord.Neighbor(0));
        mouth.Terrain = TerrainType.Coast;

        Require(NaturalGeographySystem.IsRiverSegment(map, riverCoord, downstreamCoord),
            "流路方向の隣接判定が不正");
        Require(!NaturalGeographySystem.CrossesRiver(map, riverCoord, downstreamCoord),
            "河川に沿う移動を渡河扱いした");
        Require(NaturalGeographySystem.CrossesRiver(map, bankCoord, riverCoord),
            "河岸から河道への移動が渡河扱いされない");

        NaturalGeographySystem.GenerateFloodplains(map);
        Require(river.HasFloodplain, "平原河道に氾濫原が付かない");
        Require(river.GetYields().Food == 3, "平原+河川+氾濫原の食料が3ではない");

        var player = new Player { Id = 0, NameJa = "試験文明" };
        var state = new GameState
        {
            Map = map,
            Config = new GameConfig(),
            Rng = new System.Random(7),
            TurnNumber = 6,
        };
        state.Players.Add(player);
        var unit = new Unit { Id = 1, PlayerId = 0, DefId = "warrior", Coord = bankCoord, MovesLeft = 2 };
        Require(NaturalGeographySystem.MovementCost(state, unit, bankCoord, riverCoord) == 2,
            "橋梁前の渡河移動コストが2ではない");
        Require(Math.Abs(NaturalGeographySystem.RiverCrossingAttackMultiplier(state, unit, riverCoord) -
            GameRules.RiverCrossingAttackMultiplier) < 0.001f, "渡河攻撃補正が不正");
        player.KnownTechs.Add("construction");
        Require(NaturalGeographySystem.MovementCost(state, unit, bankCoord, riverCoord) == 2,
            "橋梁網なしでも建築学だけで渡河移動ペナルティが消える");
        var bridgeCity = new City
        {
            Id = 9,
            PlayerId = player.Id,
            NameJa = "橋都",
            Coord = riverCoord.Neighbor(3),
            Buildings = new List<string> { "bridgeworks" },
        };
        player.Cities.Add(bridgeCity);
        Require(NaturalGeographySystem.MovementCost(state, unit, bankCoord, riverCoord) == 1,
            "橋梁網完成後も渡河移動ペナルティが残る");
        Require(Math.Abs(NaturalGeographySystem.RiverCrossingAttackMultiplier(state, unit, riverCoord) - 1f) < 0.001f,
            "橋梁網完成後も渡河攻撃ペナルティが残る");
    }

    static void VerifyHarborSupplyAndSave()
    {
        var map = new HexMap(7, 3);
        HexCoord cityCoord = HexCoord.FromOffset(1, 1);
        HexCoord landingCoord = HexCoord.FromOffset(5, 1);
        Tile cityTile = map.Get(cityCoord);
        Tile landing = map.Get(landingCoord);
        cityTile.Terrain = TerrainType.Grassland;
        cityTile.OwnerPlayerId = 0;
        cityTile.OwnerCityId = 1;
        landing.Terrain = TerrainType.Plains;
        landing.OwnerPlayerId = 0;
        landing.OwnerCityId = 1;
        for (int col = 2; col <= 4; col++)
            map.Get(HexCoord.FromOffset(col, 1)).Terrain = TerrainType.Coast;

        var player = new Player { Id = 0, NameJa = "港湾文明", IsHuman = true, CapitalCityId = 1 };
        player.KnownTechs.Add("construction");
        var city = new City { Id = 1, PlayerId = 0, NameJa = "港都", Coord = cityCoord };
        city.Buildings.Add("harbor");
        player.Cities.Add(city);
        cityTile.City = city;

        var state = new GameState
        {
            Map = map,
            Config = new GameConfig
            {
                MapWidth = 7,
                MapHeight = 3,
                NumPlayers = 1,
                Seed = 99,
                HumanPlayerIndex = 0,
            },
            Rng = new System.Random(99),
        };
        state.Players.Add(player);

        var withHarbor = LogisticsSystem.BuildSupplyCosts(state, player);
        Require(withHarbor.ContainsKey(landingCoord), "港から自領沿岸へ海上補給が届かない");
        city.Buildings.Remove("harbor");
        var withoutHarbor = LogisticsSystem.BuildSupplyCosts(state, player);
        Require(!withoutHarbor.ContainsKey(landingCoord), "港がなくても海上補給が通る");

        city.Buildings.Add("harbor");
        landing.HasRiver = true;
        landing.RiverOutflowDirection = DirectionIndex(landingCoord, HexCoord.FromOffset(4, 1));
        landing.HasFloodplain = true;
        string json = SaveLoad.Serialize(state);
        Require(json.Contains("\"version\":17"), "セーブversionが17ではない");
        GameState loaded = SaveLoad.Deserialize(json);
        Tile loadedLanding = loaded.Map.Get(landingCoord);
        Require(loadedLanding.HasRiver && loadedLanding.HasFloodplain &&
            loadedLanding.RiverOutflowDirection == landing.RiverOutflowDirection,
            "流向・氾濫原がセーブ往復しない");
        // このテストはGameBootstrapを通らない最小状態なので、文明の派生情報などが
        // 1回目のロードで正規化される。正規化後の決定性を既存テストと同じ形で検証する。
        string normalized = SaveLoad.Serialize(loaded);
        Require(normalized == SaveLoad.Serialize(SaveLoad.Deserialize(normalized)),
            "version 17セーブが正規化後に決定的に往復しない");
    }

    static HexMap LandMap(int width, int height)
    {
        var map = new HexMap(width, height);
        foreach (Tile tile in map.AllTiles) tile.Terrain = TerrainType.Grassland;
        return map;
    }

    static int DirectionIndex(HexCoord from, HexCoord to)
    {
        for (int i = 0; i < 6; i++) if (from.Neighbor(i) == to) return i;
        return -1;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
