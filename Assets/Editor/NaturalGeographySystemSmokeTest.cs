using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>自然地理72件、湖・河川生成、産出・文明効果、セーブv15を検証する。</summary>
public static class NaturalGeographySystemSmokeTest
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
            GameState state = ValidateGenerationAndDeterminism();
            ValidateYieldsAndEffects(state);
            ValidateSaveVersion15AndMigration(state);
            Debug.Log("NATURAL GEOGRAPHY SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("NATURAL GEOGRAPHY SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateCatalog()
    {
        if (NaturalFeatureCatalog.All.Count != 72)
            throw new Exception("自然地理台帳が72件ではない: " + NaturalFeatureCatalog.All.Count);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (NaturalFeatureDef item in NaturalFeatureCatalog.All)
        {
            if (!ids.Add(item.Id)) throw new Exception("自然地理ID重複: " + item.Id);
            if (string.IsNullOrEmpty(item.NameJa) || string.IsNullOrEmpty(item.RegionJa) ||
                string.IsNullOrEmpty(item.LocationJa) || string.IsNullOrEmpty(item.FormJa) ||
                string.IsNullOrEmpty(item.SummaryJa))
                throw new Exception("自然地理説明不足: " + item.Id);
        }
        List<NaturalFeatureDef> all = NaturalFeatureCatalog.ForRegion(GlobalHistoryIndex.AllRegions);
        foreach (NaturalFeatureKind kind in Enum.GetValues(typeof(NaturalFeatureKind)))
            if (NaturalFeatureCatalog.CountKind(all, kind) != 12)
                throw new Exception(kind + "が12件ではない");
        for (int i = 0; i < Regions.Length; i++)
            if (NaturalFeatureCatalog.ForRegion(Regions[i]).Count != 12)
                throw new Exception(Regions[i] + "が12件ではない");
        Debug.Log("[NaturalGeography] 6分類×12件・6地域×12件・安定ID OK");
    }

    static GameState ValidateGenerationAndDeterminism()
    {
        var config = new GameConfig
        {
            MapWidth = 44, MapHeight = 26, NumPlayers = 1, Seed = 20260723,
            MaxTurns = 250, HumanPlayerIndex = 0, MapType = 0,
        };
        List<HexCoord> startsA;
        List<HexCoord> startsB;
        HexMap a = MapGenerator.Generate(config, new System.Random(config.Seed), out startsA);
        HexMap b = MapGenerator.Generate(config, new System.Random(config.Seed), out startsB);
        if (startsA.Count != 1 || startsB.Count != 1 || !startsA[0].Equals(startsB[0]))
            throw new Exception("初期位置の決定論が壊れた");

        int riverCount = 0;
        foreach (Tile tile in a.AllTiles)
        {
            Tile other = b.Get(tile.Coord);
            if (other == null || tile.Terrain != other.Terrain || tile.HasHill != other.HasHill ||
                tile.HasForest != other.HasForest || tile.HasRiver != other.HasRiver ||
                tile.Resource != other.Resource)
                throw new Exception("同seedの自然地理が不一致: " + tile.Coord);
            if (tile.HasRiver)
            {
                riverCount++;
                if (!tile.IsPassable) throw new Exception("河川が通行不能地形上にある: " + tile.Coord);
            }
        }
        int lakeCount = NaturalGeographySystem.FindLakeTiles(a).Count;
        if (riverCount <= 0) throw new Exception("河川が生成されなかった");
        if (lakeCount <= 0) throw new Exception("内陸湖が生成されなかった");

        var player = new Player
        {
            Id = 0, CivilizationId = "japan", NameJa = "日本", RegionJa = "東・東南アジア",
            IsHuman = true, LeaderId = LeaderCatalog.DefaultForCivilization("japan").Id,
            Color = Color.white, CapitalCityId = 1,
        };
        var city = new City
        {
            Id = 1, PlayerId = 0, NameJa = "京都", Coord = startsA[0], Population = 1,
            Farmers = 1, Buildings = new List<string>(),
        };
        player.Cities.Add(city);
        a.Get(city.Coord).City = city;
        a.Get(city.Coord).OwnerPlayerId = player.Id;
        foreach (Tile tile in a.TilesInRange(city.Coord, 2)) tile.OwnerPlayerId = player.Id;
        var state = new GameState
        {
            Config = config, Map = a, Rng = new System.Random(config.Seed), TurnNumber = 1,
        };
        state.Players.Add(player);
        Debug.Log($"[NaturalGeography] 決定生成 河川{riverCount}タイル・湖{lakeCount}タイル OK");
        return state;
    }

    static void ValidateYieldsAndEffects(GameState state)
    {
        Tile river = null;
        foreach (Tile tile in state.Map.AllTiles)
            if (tile.HasRiver) { river = tile; break; }
        if (river == null) throw new Exception("産出検証用河川がない");
        river.HasRiver = false;
        int baseFood = river.GetYields().Food;
        river.HasRiver = true;
        if (river.GetYields().Food != baseFood + 1)
            throw new Exception("河川食料+1が反映されない");

        GameState effects = BuildEffectState();
        Player player = effects.Players[0];
        NaturalGeographyProfile profile = NaturalGeographySystem.PlayerProfile(effects, player);
        int science = NaturalGeographySystem.ScienceBonus(effects, player);
        int culture = NaturalGeographySystem.CultureBonus(effects, player);
        int access = NaturalGeographySystem.MarketAccessBonus(effects, player);
        if (profile.Diversity != 6 || science != 2 || culture != 2 || access != 4)
            throw new Exception($"自然地理効果が不正: diversity={profile.Diversity} science={science} culture={culture} market={access}");
        Debug.Log($"[NaturalGeography] 食料+1・多様性{profile.Diversity}・科学+{science}・文化+{culture}・市場+{access} OK");
    }

    static GameState BuildEffectState()
    {
        var map = new HexMap(9, 9);
        foreach (Tile tile in map.AllTiles) tile.Terrain = TerrainType.Plains;
        HexCoord center = HexCoord.FromOffset(2, 4);
        var city = new City
        {
            Id = 1, PlayerId = 0, NameJa = "検証都市", Coord = center,
            Population = 1, Farmers = 1, Buildings = new List<string>(),
        };
        var player = new Player
        {
            Id = 0, CivilizationId = "japan", NameJa = "日本", RegionJa = "東・東南アジア",
            IsHuman = true, LeaderId = LeaderCatalog.DefaultForCivilization("japan").Id,
            Color = Color.white, CapitalCityId = 1,
        };
        player.Cities.Add(city);
        map.Get(center).City = city;
        map.Get(center).HasRiver = true;

        // 都市半径2に山・森林・砂漠・内陸湖を置き、別方向の海は外縁まで接続する。
        map.Get(HexCoord.FromOffset(2, 3)).Terrain = TerrainType.Mountain;
        map.Get(HexCoord.FromOffset(3, 3)).HasForest = true;
        map.Get(HexCoord.FromOffset(3, 4)).Terrain = TerrainType.Desert;
        map.Get(HexCoord.FromOffset(2, 5)).Terrain = TerrainType.Coast; // 外縁非接続 = 湖
        map.Get(HexCoord.FromOffset(1, 4)).Terrain = TerrainType.Coast;
        map.Get(HexCoord.FromOffset(0, 4)).Terrain = TerrainType.Ocean; // 外縁接続 = 海

        var state = new GameState
        {
            Config = new GameConfig { MapWidth = 9, MapHeight = 9, NumPlayers = 1, Seed = 15 },
            Map = map, Rng = new System.Random(15), TurnNumber = 1,
        };
        state.Players.Add(player);
        return state;
    }

    static void ValidateSaveVersion15AndMigration(GameState state)
    {
        string json1 = SaveLoad.Serialize(state);
        if (!json1.Contains("\"version\":15") || !json1.Contains("\"hasRiver\":"))
            throw new Exception("セーブv15の河川配列がない");
        GameState restored = SaveLoad.Deserialize(json1);
        string json2 = SaveLoad.Serialize(restored);
        string json3 = SaveLoad.Serialize(SaveLoad.Deserialize(json2));
        if (json2 != json3) throw new Exception("自然地理を含むセーブ往復が非決定的");
        foreach (Tile tile in state.Map.AllTiles)
            if (tile.HasRiver != restored.Map.Get(tile.Coord).HasRiver)
                throw new Exception("河川復元が不一致: " + tile.Coord);

        SaveData legacy = JsonUtility.FromJson<SaveData>(json1);
        legacy.version = 14;
        legacy.hasRiver = null;
        GameState migrated = SaveLoad.Deserialize(JsonUtility.ToJson(legacy));
        int regenerated = 0;
        foreach (Tile tile in migrated.Map.AllTiles) if (tile.HasRiver) regenerated++;
        if (regenerated <= 0) throw new Exception("v14セーブから河川を補完できない");
        Debug.Log("[NaturalGeography] セーブv15決定往復・v14河川補完 OK");
    }
}
