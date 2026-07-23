using System;
using System.Collections.Generic;
using HexCiv;
using HexCiv.Core;
using HexCiv.Core.AI;
using UnityEditor;
using UnityEngine;

/// <summary>作品史336件、7分野効果、世界一意収蔵、偉人連携、セーブ往復を検証する。</summary>
public static class MasterpieceSystemSmokeTest
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
            ValidateEffectsAndUniqueness();
            ValidateGreatPersonLinkAndSave();
            Debug.Log("MASTERPIECE SYSTEM SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("MASTERPIECE SYSTEM SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateCatalog()
    {
        if (MasterpieceCatalog.All.Count != 336)
            throw new Exception("作品史件数が不正: " + MasterpieceCatalog.All.Count);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kinds = new Dictionary<MasterpieceKind, int>();
        for (int i = 0; i < MasterpieceCatalog.All.Count; i++)
        {
            var work = MasterpieceCatalog.All[i];
            if (!ids.Add(work.Id)) throw new Exception("作品ID重複: " + work.Id);
            if (string.IsNullOrEmpty(work.NameJa) || string.IsNullOrEmpty(work.RegionJa) ||
                string.IsNullOrEmpty(work.PeriodJa) || string.IsNullOrEmpty(work.CreatorJa) ||
                string.IsNullOrEmpty(work.SummaryJa) || string.IsNullOrEmpty(work.KindNameJa))
                throw new Exception("作品の必須情報不足: " + work.Id);
            if (!string.IsNullOrEmpty(work.RelatedCivilizationId) &&
                CivilizationCatalog.Find(work.RelatedCivilizationId) == null)
                throw new Exception("作品の関連文明IDが不正: " + work.Id + " -> " +
                    work.RelatedCivilizationId);
            if (!string.IsNullOrEmpty(work.RelatedGreatPersonId) &&
                GreatPersonCatalog.Find(work.RelatedGreatPersonId) == null)
                throw new Exception("作品の関連偉人IDが不正: " + work.Id + " -> " +
                    work.RelatedGreatPersonId);
            kinds[work.Kind] = kinds.ContainsKey(work.Kind) ? kinds[work.Kind] + 1 : 1;
        }

        foreach (MasterpieceKind kind in Enum.GetValues(typeof(MasterpieceKind)))
            if (!kinds.ContainsKey(kind) || kinds[kind] != 48)
                throw new Exception(kind + "の作品数が不正: " +
                    (kinds.ContainsKey(kind) ? kinds[kind] : 0));
        for (int i = 0; i < Regions.Length; i++)
            if (MasterpieceCatalog.ForRegion(Regions[i]).Count != 56)
                throw new Exception(Regions[i] + "の作品数が不正");

        Debug.Log("[Masterpiece] 336件・7分野×48・6地域×56・参照ID OK");
    }

    static void ValidateEffectsAndUniqueness()
    {
        var state = BuildState(5217);
        var player = state.Players[0];
        var rival = state.Players[1];
        player.MasterpiecePoints = 10000;
        // 新規ゲームは開拓者から始まり都市がまだないため、全都市生産効果の検証用都市を置く。
        if (player.Cities.Count == 0)
            player.Cities.Add(new City
            {
                Id = 9001,
                PlayerId = player.Id,
                NameJa = "効果検証都市",
                Population = 1,
            });

        int science = player.ScienceStored;
        Collect(state, player, "book_of_dead");
        if (player.ScienceStored <= science || MasterpieceSystem.SciencePerTurnBonus(player) != 1)
            throw new Exception("書籍効果が不正");

        int culture = player.TotalCulture;
        int influence = CultureSystem.InfluenceOn(player, rival);
        Collect(state, player, "nebamun_paintings");
        if (player.TotalCulture <= culture || CultureSystem.InfluenceOn(player, rival) <= influence)
            throw new Exception("絵画効果が不正");

        int production = TotalProduction(player);
        Collect(state, player, "great_sphinx");
        if (TotalProduction(player) <= production) throw new Exception("彫刻効果が不正");

        production = TotalProduction(player);
        Collect(state, player, "giza_pyramid_complex");
        if (TotalProduction(player) <= production) throw new Exception("建築効果が不正");

        culture = player.TotalCulture;
        influence = CultureSystem.InfluenceOn(player, rival);
        Collect(state, player, "ahellil_gourara");
        if (player.TotalCulture <= culture || CultureSystem.InfluenceOn(player, rival) <= influence)
            throw new Exception("音楽効果が不正");

        culture = player.TotalCulture;
        influence = CultureSystem.InfluenceOn(player, rival);
        production = TotalProduction(player);
        Collect(state, player, "al_aragoz");
        if (player.TotalCulture <= culture || TotalProduction(player) <= production ||
            CultureSystem.InfluenceOn(player, rival) <= influence)
            throw new Exception("演劇効果が不正");

        culture = player.TotalCulture;
        influence = CultureSystem.InfluenceOn(player, rival);
        science = player.ScienceStored;
        Collect(state, player, "black_girl_1966");
        if (player.TotalCulture <= culture || player.ScienceStored <= science ||
            CultureSystem.InfluenceOn(player, rival) <= influence ||
            MasterpieceSystem.SciencePerTurnBonus(player) != 2)
            throw new Exception("映画効果が不正");
        if (MasterpieceSystem.CulturePerTurnBonus(player) < 12)
            throw new Exception("作品の毎ターン文化効果が不正");

        rival.MasterpiecePoints = 10000;
        if (MasterpieceSystem.TryCollect(state, rival, "book_of_dead"))
            throw new Exception("世界共通の作品を二重収蔵した");
        if (rival.Cities.Count == 0)
            rival.Cities.Add(new City
            {
                Id = 9002,
                PlayerId = rival.Id,
                NameJa = "AI収蔵検証都市",
                Population = 1,
            });
        MasterpieceSystem.AdvancePlayer(state, rival);
        if (rival.CollectedMasterpieces.Count != 1)
            throw new Exception("AI文明が作品を自動収蔵しない");

        Debug.Log("[Masterpiece] 7分野効果・継続効果・世界一意収蔵・AI自動収蔵 OK");
    }

    static void ValidateGreatPersonLinkAndSave()
    {
        var state = BuildState(8821);
        var player = state.Players[0];
        player.GreatPersonPoints = 10000;
        if (!WorldLegacySystem.TryRecruit(state, player, "hokusai"))
            throw new Exception("北斎を登用できない");
        if (!player.CollectedMasterpieces.Contains("great_wave"))
            throw new Exception("偉人から関連作品が収蔵されない");

        player.MasterpiecePoints = 321;
        player.TotalMasterpiecePoints = 654;
        state.LastSavedAtIso = "2026-07-21T15:00:00";
        string json1 = SaveLoad.Serialize(state);
        if (!json1.Contains("\"version\":16")) throw new Exception("セーブversion 16ではない");
        var restored = SaveLoad.Deserialize(json1);
        string json2 = SaveLoad.Serialize(restored);
        if (json1 != json2) throw new Exception("作品収蔵を含むセーブ往復が非決定的");
        var restoredPlayer = restored.GetPlayer(player.Id);
        if (restoredPlayer.MasterpiecePoints != 321 ||
            restoredPlayer.TotalMasterpiecePoints != 654 ||
            !restoredPlayer.CollectedMasterpieces.Contains("great_wave"))
            throw new Exception("作品進行の復元に失敗");

        string oldVersion = json1.Replace("\"version\":16", "\"version\":8");
        if (SaveLoad.Deserialize(oldVersion) == null)
            throw new Exception("version 8セーブを読み込めない");

        Debug.Log("[Masterpiece] 偉人連携・セーブv10決定往復・v8互換 OK");
    }

    static void Collect(GameState state, Player player, string id)
    {
        if (!MasterpieceSystem.TryCollect(state, player, id))
            throw new Exception("作品を収蔵できない: " + id);
    }

    static int TotalProduction(Player player)
    {
        int total = 0;
        for (int i = 0; i < player.Cities.Count; i++) total += player.Cities[i].ProductionStored;
        return total;
    }

    static GameState BuildState(int seed)
    {
        var config = new GameConfig
        {
            Seed = seed,
            HumanPlayerIndex = 0,
            MapWidth = 40,
            MapHeight = 24,
            NumPlayers = 4,
        };
        var state = GameBootstrap.BuildNewGame(config);
        new TurnManager(state, new AIController());
        return state;
    }
}
