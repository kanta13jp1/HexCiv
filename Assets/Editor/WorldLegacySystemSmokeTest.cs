using System;
using System.Collections.Generic;
using HexCiv;
using HexCiv.Core;
using HexCiv.Core.AI;
using UnityEditor;
using UnityEngine;

/// <summary>遺産配置・発見、偉人登用、親和性、セーブ往復をヘッドレス検証する。</summary>
public static class WorldLegacySystemSmokeTest
{
    public static void Run()
    {
        try
        {
            ValidateCatalogEffects();
            ValidatePlacementAndDiscovery();
            ValidateRecruitmentAndSave();
            Debug.Log("WORLD LEGACY SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("WORLD LEGACY SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateCatalogEffects()
    {
        if (HeritageSiteCatalog.All.Count != 120 || GreatPersonCatalog.All.Count != 132)
            throw new Exception("世界史台帳件数が不正");

        var effects = new HashSet<GreatPersonEffectKind>();
        for (int i = 0; i < GreatPersonCatalog.All.Count; i++)
        {
            var person = GreatPersonCatalog.All[i];
            effects.Add(WorldLegacySystem.EffectKind(person));
            if (string.IsNullOrEmpty(WorldLegacySystem.EffectTextJa(person)))
                throw new Exception("偉人効果説明がない: " + person.Id);
        }
        if (effects.Count != 6)
            throw new Exception("偉人効果6系統が網羅されていない: " + effects.Count);
        Debug.Log("[Legacy] 遺産120件・偉人132人・分野別効果6系統OK");
    }

    static void ValidatePlacementAndDiscovery()
    {
        var first = BuildState(7319);
        var second = BuildState(7319);
        if (first.HeritageSites.Count != WorldLegacySystem.StandardHeritageCount)
            throw new Exception("標準遺産配置数が不正: " + first.HeritageSites.Count);
        if (second.HeritageSites.Count != first.HeritageSites.Count)
            throw new Exception("同seedの遺産配置数が一致しない");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var coords = new HashSet<HexCoord>();
        var regions = new HashSet<string>();
        for (int i = 0; i < first.HeritageSites.Count; i++)
        {
            var a = first.HeritageSites[i];
            var b = second.HeritageSites[i];
            if (a.SiteId != b.SiteId || a.Coord != b.Coord)
                throw new Exception("同seedの遺産配置が非決定的(index=" + i + ")");
            if (!ids.Add(a.SiteId) || !coords.Add(a.Coord))
                throw new Exception("遺産Idまたは座標が重複");
            if (a.Def == null || !first.Map.Get(a.Coord).IsPassable)
                throw new Exception("遺産の定義または配置地が不正: " + a.SiteId);
            regions.Add(a.Def.RegionJa);
        }
        if (regions.Count != 6) throw new Exception("配置遺産が6地域を網羅していない");

        var player = first.Players[0];
        player.CivilizationId = "egypt";
        player.RegionJa = "アフリカ";
        var site = first.HeritageSites[0];
        site.SiteId = "giza_pyramids";
        var reward = WorldLegacySystem.GetHeritageReward(first, player, site.Def);
        if (reward.AffinityPercent != 50)
            throw new Exception("関連文明の遺産親和性が不正");
        int cultureBefore = player.TotalCulture;
        int scienceBefore = player.ScienceStored;
        int pointsBefore = player.GreatPersonPoints;
        if (!WorldLegacySystem.CheckDiscoveryAt(first, player, site.Coord))
            throw new Exception("遺産を発見できない");
        if (WorldLegacySystem.CheckDiscoveryAt(first, player, site.Coord))
            throw new Exception("同じ遺産を二重発見した");
        if (player.TotalCulture - cultureBefore != reward.Culture ||
            player.ScienceStored - scienceBefore != reward.Science ||
            player.GreatPersonPoints - pointsBefore != reward.GreatPersonPoints)
            throw new Exception("遺産発見報酬が計算値と一致しない");
        Debug.Log("[Legacy] 6地域12件の決定的配置・発見一度限り・文明親和性OK");
    }

    static void ValidateRecruitmentAndSave()
    {
        var state = BuildState(8128);
        var player = state.Players[0];
        var rival = state.Players[1];
        var person = GreatPersonCatalog.Find("mary_golda_ross");
        if (person == null) throw new Exception("偉人テスト定義がない");
        player.GreatPersonPoints = 500;
        int scienceBefore = player.ScienceStored;
        if (!WorldLegacySystem.TryRecruit(state, player, person.Id))
            throw new Exception("偉人を登用できない");
        if (!player.RecruitedGreatPeople.Contains(person.Id) ||
            player.ScienceStored <= scienceBefore)
            throw new Exception("第3弾偉人の登用または工学効果が反映されない");
        rival.GreatPersonPoints = 500;
        if (WorldLegacySystem.TryRecruit(state, rival, person.Id))
            throw new Exception("世界共通の偉人を二重登用した");

        var site = state.HeritageSites[0];
        site.SiteId = "new_echota";
        WorldLegacySystem.CheckDiscoveryAt(state, player, site.Coord);
        state.LastSavedAtIso = "2026-07-21T12:00:00";
        string json1 = SaveLoad.Serialize(state);
        var restored = SaveLoad.Deserialize(json1);
        string json2 = SaveLoad.Serialize(restored);
        if (json1 != json2) throw new Exception("遺産・偉人を含むセーブ往復が非決定的");
        var restoredPlayer = restored.GetPlayer(player.Id);
        if (restored.HeritageSites.Count != state.HeritageSites.Count ||
            restoredPlayer.GreatPersonPoints != player.GreatPersonPoints ||
            !restoredPlayer.RecruitedGreatPeople.Contains(person.Id) ||
            restoredPlayer.DiscoveredHeritageSites.Count != player.DiscoveredHeritageSites.Count)
            throw new Exception("遺産・偉人進行の復元に失敗");

        // 旧版番号を受け入れ、TurnManagerのBindで空配置を移行できることも確認する。
        var legacy = SaveLoad.Deserialize(json1.Replace("\"version\":9", "\"version\":7"));
        legacy.HeritageSites.Clear();
        new TurnManager(legacy, new AIController());
        if (legacy.HeritageSites.Count == 0)
            throw new Exception("旧セーブ相当の遺産配置移行に失敗");
        Debug.Log("[Legacy] 偉人世界一意・分野別効果・セーブv9往復・旧版移行OK");
    }

    static GameState BuildState(int seed)
    {
        var config = new GameConfig
        {
            Seed = seed,
            HumanPlayerIndex = -1,
            MapWidth = 40,
            MapHeight = 24,
            NumPlayers = 4,
        };
        var state = GameBootstrap.BuildNewGame(config);
        new TurnManager(state, new AIController());
        return state;
    }
}
