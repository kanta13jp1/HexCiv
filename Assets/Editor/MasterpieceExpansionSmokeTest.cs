using System;
using System.Collections.Generic;
using HexCiv;
using HexCiv.Core;
using HexCiv.Core.AI;
using UnityEditor;
using UnityEngine;

/// <summary>作品史追加126件の後方互換、地域・分野均衡、ゲーム収蔵、セーブ往復を検証する。</summary>
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

    static readonly string[] ThirdBatchIds =
    {
        "basekabaka_be_buganda", "buddhacarita", "hikayat_aceh",
        "hypnerotomachia_poliphili", "hombres_que_disperso_danza",
        "kohau_rongorongo_tablet",
        "asante_adinkra_cloth_1817", "kizil_cave_murals",
        "goryeo_water_moon_avalokiteshvara", "procession_st_marks_square",
        "chief_joseph_series_17", "ana_kai_tangata_rock_art",
        "sika_dwa_kofi", "gandhara_parinirvana_relief",
        "goryeo_gilt_bronze_buddha", "mars_neptune_doges_palace",
        "monte_alban_danzantes", "hoa_hakananai_a",
        "muzibu_azaala_mpanga", "takht_i_bahi_monastery",
        "baiturrahman_grand_mosque", "doges_palace_venice",
        "mitla_palace_group", "ahu_tongariki",
        "buganda_royal_drum_repertoire", "gurbani_kirtan", "saman_gayo",
        "vivaldi_four_seasons", "cherokee_syllabary_hymns", "fijian_meke",
        "the_burdens_ruganda", "sariputraprakarana",
        "talchum_mask_dance_drama", "servant_of_two_masters",
        "unto_these_hills", "last_virgin_in_paradise",
        "heritage_africa_film", "nanak_nam_jahaz_hai_film",
        "tjoet_nja_dhien_film", "mephisto_film",
        "cherokee_word_for_water_film", "land_has_eyes_film",
    };

    static readonly string[] FourthBatchIds =
    {
        "book_bornu_wars", "karnamag_ardashir", "sejarah_melayu",
        "tale_bygone_years", "black_elk_speaks", "ancient_tahiti",
        "merina_lamba_akotifahana", "akbar_hawai_akbarnama",
        "cheonmado_heavenly_horse", "saint_vincent_panels",
        "lone_dog_winter_count", "two_tahitian_women",
        "kuba_ndop_1760", "colossal_shapur_i", "seokguram_buddha",
        "tomb_ines_de_castro", "raven_first_men", "rarotonga_staff_god",
        "manjakamiadana_palace", "taq_kasra", "bunhwangsa_pagoda",
        "belem_tower", "powhatan_yehakin", "para_o_tane_palace",
        "hiragasy_music", "qawwali_qaul", "cheoyongga_silla",
        "portuguese_fado", "maple_leaf_rag", "te_atua_mou_e",
        "marriage_anansewa", "indar_sabha", "mak_yong_theatre",
        "auto_barca_inferno", "dry_lips_kapuskasing", "i_tai_henri_hiro",
        "tabataba_film", "chess_players_film", "hang_tuah_film",
        "aniki_bobo_film", "daughter_dawn_film", "mauri_film",
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
        if (MasterpieceCatalog.All.Count != 336)
            throw new Exception("作品史総数が不正: " + MasterpieceCatalog.All.Count);
        if (MasterpieceCatalog.All[209].Id != "vai_film")
            throw new Exception("既存210件の末尾または順序が変化した");
        if (MasterpieceCatalog.All[210].Id != NewIds[0] ||
            MasterpieceCatalog.All[251].Id != NewIds[NewIds.Length - 1])
            throw new Exception("追加42件が既存台帳の末尾に連続していない");
        if (MasterpieceCatalog.All[252].Id != ThirdBatchIds[0] ||
            MasterpieceCatalog.All[293].Id != ThirdBatchIds[ThirdBatchIds.Length - 1])
            throw new Exception("第3弾42件が既存252件の末尾に連続していない");
        if (MasterpieceCatalog.All[294].Id != FourthBatchIds[0] ||
            MasterpieceCatalog.All[335].Id != FourthBatchIds[FourthBatchIds.Length - 1])
            throw new Exception("第4弾42件が既存294件の末尾に連続していない");

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
        for (int i = 0; i < ThirdBatchIds.Length; i++)
            if (MasterpieceCatalog.Find(ThirdBatchIds[i]) == null)
                throw new Exception("第3弾作品が見つからない: " + ThirdBatchIds[i]);
        for (int i = 0; i < FourthBatchIds.Length; i++)
            if (MasterpieceCatalog.Find(FourthBatchIds[i]) == null)
                throw new Exception("第4弾作品が見つからない: " + FourthBatchIds[i]);
    }

    static void ValidateBalancedBatch()
    {
        if (FourthBatchIds.Length != 42) throw new Exception("第4弾ID数が42ではない");
        var batch = new HashSet<string>(FourthBatchIds, StringComparer.OrdinalIgnoreCase);
        foreach (MasterpieceKind kind in Enum.GetValues(typeof(MasterpieceKind)))
        {
            if (MasterpieceCatalog.ForKind(kind).Count != 48)
                throw new Exception(kind + "の総数が48ではない");
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
            if (MasterpieceCatalog.ForRegion(Regions[r]).Count != 56)
                throw new Exception(Regions[r] + "の総数が56ではない");
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
        if (!MasterpieceSystem.TryCollect(state, player, "book_bornu_wars"))
            throw new Exception("第4弾作品を収蔵できない");

        state.LastSavedAtIso = "2026-07-21T20:00:00";
        string json = SaveLoad.Serialize(state);
        var restored = SaveLoad.Deserialize(json);
        if (!restored.GetPlayer(player.Id).CollectedMasterpieces.Contains(
            "book_bornu_wars"))
            throw new Exception("第4弾作品の収蔵IDがセーブ往復で失われた");
    }
}
