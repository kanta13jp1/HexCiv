using System;
using System.Collections.Generic;
using HexCiv;
using HexCiv.Core;
using HexCiv.Core.AI;
using UnityEditor;
using UnityEngine;

/// <summary>作品史追加42件の後方互換、地域・分野均衡、ゲーム収蔵、セーブ往復を検証する。</summary>
public static class MasterpieceExpansionSmokeTest
{
    static readonly string[] Regions =
    {
        "アフリカ", "西・南アジア", "東・東南アジア",
        "ヨーロッパ・地中海", "アメリカ大陸", "オセアニア"
    };

    static readonly string[] NewIds =
    {
        "kebra_nagast", "dnyaneshwari", "omoro_soshi", "odyssey",
        "nueva_coronica_buen_gobierno", "tales_of_tikongs",
        "tutu_enwonwu", "bharat_mata", "eight_views_ryukyu", "arnolfini_portrait",
        "migration_series", "alhalker_suite",
        "idia_pendant_mask", "gommateshwara_bahubali", "ngoc_lu_bronze_drum",
        "marcus_aurelius_equestrian", "death_of_cleopatra_lewis", "aboriginal_memorial",
        "fasil_ghebbi_architecture", "raigad_fort_architecture", "shuri_castle_architecture",
        "wawel_castle_architecture", "caguana_ceremonial_architecture",
        "haamonga_trilithon_architecture",
        "tizita_music", "marathi_abhang", "ryukyu_classical_music", "heroic_polonaise",
        "taino_areito_music", "tongan_lakalaka_music",
        "lion_and_jewel", "ghashiram_kotwal", "nido_tekiuchi", "dziady_play",
        "dream_monkey_mountain", "songmakers_chair",
        "moolaade_film", "a_separation_film", "parasite_film",
        "passion_joan_arc_film", "roma_2018_film", "whale_rider_film",
    };

    public static void Run()
    {
        try
        {
            ValidateAppendOnlyCatalog();
            ValidateBalancedBatch();
            ValidateCollectionAndSave();
            Debug.Log("MASTERPIECE EXPANSION SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("MASTERPIECE EXPANSION SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateAppendOnlyCatalog()
    {
        if (MasterpieceCatalog.All.Count != 252)
            throw new Exception("作品史総数が不正: " + MasterpieceCatalog.All.Count);
        if (MasterpieceCatalog.All[209].Id != "vai_film")
            throw new Exception("既存210件の末尾または順序が変化した");
        if (MasterpieceCatalog.All[210].Id != NewIds[0] ||
            MasterpieceCatalog.All[251].Id != NewIds[NewIds.Length - 1])
            throw new Exception("追加42件が既存台帳の末尾に連続していない");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < MasterpieceCatalog.All.Count; i++)
        {
            var work = MasterpieceCatalog.All[i];
            if (!ids.Add(work.Id)) throw new Exception("作品ID重複: " + work.Id);
            if (string.IsNullOrEmpty(work.NameJa) || string.IsNullOrEmpty(work.RegionJa) ||
                string.IsNullOrEmpty(work.PeriodJa) || string.IsNullOrEmpty(work.CreatorJa) ||
                string.IsNullOrEmpty(work.SummaryJa))
                throw new Exception("作品情報不足: " + work.Id);
            if (!string.IsNullOrEmpty(work.RelatedCivilizationId) &&
                CivilizationCatalog.Find(work.RelatedCivilizationId) == null)
                throw new Exception("文明参照不正: " + work.Id);
            if (!string.IsNullOrEmpty(work.RelatedGreatPersonId) &&
                GreatPersonCatalog.Find(work.RelatedGreatPersonId) == null)
                throw new Exception("偉人参照不正: " + work.Id);
        }
        for (int i = 0; i < NewIds.Length; i++)
            if (MasterpieceCatalog.Find(NewIds[i]) == null)
                throw new Exception("追加作品が見つからない: " + NewIds[i]);
    }

    static void ValidateBalancedBatch()
    {
        if (NewIds.Length != 42) throw new Exception("追加ID数が42ではない");
        var batch = new HashSet<string>(NewIds, StringComparer.OrdinalIgnoreCase);
        foreach (MasterpieceKind kind in Enum.GetValues(typeof(MasterpieceKind)))
        {
            if (MasterpieceCatalog.ForKind(kind).Count != 36)
                throw new Exception(kind + "の総数が36ではない");
            for (int r = 0; r < Regions.Length; r++)
            {
                int count = 0;
                var regional = MasterpieceCatalog.ForRegion(Regions[r]);
                for (int i = 0; i < regional.Count; i++)
                    if (regional[i].Kind == kind && batch.Contains(regional[i].Id)) count++;
                if (count != 1)
                    throw new Exception(kind + " / " + Regions[r] + " の追加数が1ではない: " + count);
            }
        }
        for (int r = 0; r < Regions.Length; r++)
            if (MasterpieceCatalog.ForRegion(Regions[r]).Count != 42)
                throw new Exception(Regions[r] + "の総数が42ではない");
    }

    static void ValidateCollectionAndSave()
    {
        var config = new GameConfig
        {
            Seed = 20260721,
            HumanPlayerIndex = 0,
            MapWidth = 40,
            MapHeight = 24,
            NumPlayers = 4,
        };
        var state = GameBootstrap.BuildNewGame(config);
        new TurnManager(state, new AIController());
        var player = state.Players[0];
        player.MasterpiecePoints = 10000;
        if (player.Cities.Count == 0)
            player.Cities.Add(new City { Id = 9742, PlayerId = player.Id, NameJa = "追加作品検証都市", Population = 1 });
        if (!MasterpieceSystem.TryCollect(state, player, "kebra_nagast"))
            throw new Exception("追加作品を収蔵できない");

        state.LastSavedAtIso = "2026-07-21T20:00:00";
        string json = SaveLoad.Serialize(state);
        var restored = SaveLoad.Deserialize(json);
        if (!restored.GetPlayer(player.Id).CollectedMasterpieces.Contains("kebra_nagast"))
            throw new Exception("追加作品の収蔵IDがセーブ往復で失われた");
    }
}
