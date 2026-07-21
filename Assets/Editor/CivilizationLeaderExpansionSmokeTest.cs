using System;
using System.Collections.Generic;
using HexCiv;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>文明・指導者第2～第4弾の件数、参照、地域均衡、旧順序と画像を検証する。</summary>
public static class CivilizationLeaderExpansionSmokeTest
{
    static readonly string[][] SecondBatchCivilizationsByRegion =
    {
        new[] { "アフリカ", "benin", "ethiopia" },
        new[] { "西・南アジア", "abbasid", "maratha" },
        new[] { "東・東南アジア", "ryukyu", "toungoo" },
        new[] { "ヨーロッパ・地中海", "holy_roman", "polish_lithuanian" },
        new[] { "アメリカ大陸", "muisca", "taino" },
        new[] { "オセアニア", "tonga", "samoa" },
    };

    static readonly string[][] ThirdBatchCivilizationsByRegion =
    {
        new[] { "アフリカ", "asante", "buganda" },
        new[] { "西・南アジア", "kushan", "sikh_empire" },
        new[] { "東・東南アジア", "goryeo", "aceh" },
        new[] { "ヨーロッパ・地中海", "venice", "hungary" },
        new[] { "アメリカ大陸", "zapotec", "cherokee" },
        new[] { "オセアニア", "rapa_nui", "fiji" },
    };

    static readonly string[] ThirdBatchOrder =
    {
        "asante", "buganda", "kushan", "sikh_empire", "goryeo", "aceh",
        "venice", "hungary", "zapotec", "cherokee", "rapa_nui", "fiji",
    };

    static readonly string[][] FourthBatchCivilizationsByRegion =
    {
        new[] { "アフリカ", "kanem_bornu", "merina" },
        new[] { "西・南アジア", "sasanian", "delhi_sultanate" },
        new[] { "東・東南アジア", "silla", "malacca" },
        new[] { "ヨーロッパ・地中海", "kyivan_rus", "portugal" },
        new[] { "アメリカ大陸", "lakota", "powhatan" },
        new[] { "オセアニア", "tahiti", "rarotonga" },
    };

    static readonly string[] FourthBatchOrder =
    {
        "kanem_bornu", "merina", "sasanian", "delhi_sultanate", "silla", "malacca",
        "kyivan_rus", "portugal", "lakota", "powhatan", "tahiti", "rarotonga",
    };

    static readonly Dictionary<string, string[]> ExpectedLeaders =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "benin", new[] { "ewuare_i", "idia" } },
            { "ethiopia", new[] { "zara_yaqob_emperor", "menelik_ii" } },
            { "abbasid", new[] { "al_mansur", "harun_al_rashid" } },
            { "maratha", new[] { "shivaji", "tarabai" } },
            { "ryukyu", new[] { "sho_hashi", "sho_shin" } },
            { "toungoo", new[] { "tabinshwehti", "bayinnaung" } },
            { "holy_roman", new[] { "otto_i_hre", "frederick_ii_hre" } },
            { "polish_lithuanian", new[] { "jadwiga_poland", "wladyslaw_ii_jagiello" } },
            { "muisca", new[] { "nemequene", "tisquesusa" } },
            { "taino", new[] { "anacaona", "caonabo" } },
            { "tonga", new[] { "george_tupou_i", "salote_tupou_iii" } },
            { "samoa", new[] { "salamasina", "malietoa_laupepa" } },
            { "asante", new[] { "osei_tutu_i", "yaa_asantewaa" } },
            { "buganda", new[] { "mutesa_i_buganda", "mwanga_ii_buganda" } },
            { "kushan", new[] { "kujula_kadphises", "kanishka_i" } },
            { "sikh_empire", new[] { "ranjit_singh", "jind_kaur" } },
            { "goryeo", new[] { "taejo_wang_geon", "gwangjong_goryeo" } },
            { "aceh", new[] { "iskandar_muda", "taj_ul_alam" } },
            { "venice", new[] { "enrico_dandolo", "francesco_foscari" } },
            { "hungary", new[] { "stephen_i_hungary", "matthias_corvinus" } },
            { "zapotec", new[] { "cosijoeza", "cocijopii" } },
            { "cherokee", new[] { "nanyehi", "john_ross" } },
            { "rapa_nui", new[] { "hotu_matua", "ngaara_rapa_nui" } },
            { "fiji", new[] { "tanoa_visawaqa", "seru_cakobau" } },
            { "kanem_bornu", new[] { "dunama_dabbalemi", "idris_alooma" } },
            { "merina", new[] { "andrianampoinimerina", "ranavalona_i" } },
            { "sasanian", new[] { "ardashir_i", "khosrow_i" } },
            { "delhi_sultanate", new[] { "razia_sultan", "alauddin_khalji" } },
            { "silla", new[] { "queen_seondeok", "king_muyeol" } },
            { "malacca", new[] { "parameswara", "mansur_shah_malacca" } },
            { "kyivan_rus", new[] { "olga_of_kyiv", "yaroslav_the_wise" } },
            { "portugal", new[] { "joao_i_portugal", "manuel_i_portugal" } },
            { "lakota", new[] { "red_cloud", "sitting_bull" } },
            { "powhatan", new[] { "wahunsenacawh", "opechancanough" } },
            { "tahiti", new[] { "pomare_ii", "pomare_iv" } },
            { "rarotonga", new[] { "makea_pori", "makea_takau" } },
        };

    public static void Run()
    {
        try
        {
            ValidateCatalogCountsAndCompatibility();
            ValidateCivilizationsAndRegions();
            ValidateNewLeaders();
            ValidateFourthBatchSelectionAndSave();
            ValidateVisualAsset();
            Debug.Log("CIVILIZATION LEADER EXPANSION SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("CIVILIZATION LEADER EXPANSION SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateCatalogCountsAndCompatibility()
    {
        if (CivilizationCatalog.All.Count != 92)
            throw new Exception("文明件数が不正: " + CivilizationCatalog.All.Count);
        if (LeaderCatalog.All.Count != 179)
            throw new Exception("指導者件数が不正: " + LeaderCatalog.All.Count);
        if (CivilizationCatalog.All[0].Id != "athens" ||
            CivilizationCatalog.All[3].Id != "babylon" ||
            CivilizationCatalog.All[55].Id != "maori")
            throw new Exception("既存56文明の互換順序が変化した");
        if (CivilizationCatalog.All[67].Id != "samoa" ||
            CivilizationCatalog.All[79].Id != "fiji" ||
            CivilizationCatalog.All[91].Id != "rarotonga")
            throw new Exception("第2～第4弾の後方追加順序が変化した");
        if (LeaderCatalog.All[130].Id != "malietoa_laupepa" ||
            LeaderCatalog.All[154].Id != "seru_cakobau" ||
            LeaderCatalog.All[178].Id != "makea_takau")
            throw new Exception("指導者第2～第4弾の後方追加順序が変化した");
        if (CivilizationCatalog.DefaultForSlot(0).Id != "athens" ||
            CivilizationCatalog.DefaultForSlot(3).Id != "babylon")
            throw new Exception("既定スロットの互換性が壊れた");
    }

    static void ValidateCivilizationsAndRegions()
    {
        var allIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < CivilizationCatalog.All.Count; i++)
            if (!allIds.Add(CivilizationCatalog.All[i].Id))
                throw new Exception("文明ID重複: " + CivilizationCatalog.All[i].Id);

        ValidateCivilizationRows(SecondBatchCivilizationsByRegion, "第2弾");
        ValidateCivilizationRows(ThirdBatchCivilizationsByRegion, "第3弾");
        ValidateCivilizationRows(FourthBatchCivilizationsByRegion, "第4弾");
        for (int i = 0; i < ThirdBatchOrder.Length; i++)
            if (CivilizationCatalog.All[68 + i].Id != ThirdBatchOrder[i])
                throw new Exception("第3弾文明の後方追加順序が不正: " + i);
        for (int i = 0; i < FourthBatchOrder.Length; i++)
            if (CivilizationCatalog.All[80 + i].Id != FourthBatchOrder[i])
                throw new Exception("第4弾文明の後方追加順序が不正: " + i);
        Debug.Log("[CivilizationExpansion] 第2～第4弾 各6地域×2文明・旧80件互換 OK");
    }

    static void ValidateCivilizationRows(string[][] rows, string batchName)
    {
        for (int r = 0; r < rows.Length; r++)
        {
            var row = rows[r];
            for (int i = 1; i < row.Length; i++)
            {
                var civilization = CivilizationCatalog.Find(row[i]);
                if (civilization == null) throw new Exception(batchName + "文明がない: " + row[i]);
                if (string.IsNullOrEmpty(civilization.NameJa) ||
                    string.IsNullOrEmpty(civilization.RegionJa) ||
                    string.IsNullOrEmpty(civilization.EraJa) || civilization.CityNames.Length < 6)
                    throw new Exception(batchName + "文明の必須データ不足: " + row[i]);
                if (GlobalHistoryIndex.BroadRegion(civilization) != row[0])
                    throw new Exception("新文明の地域写像が不正: " + row[i] + " -> " +
                        GlobalHistoryIndex.BroadRegion(civilization));
            }
        }
    }

    static void ValidateNewLeaders()
    {
        var allIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < LeaderCatalog.All.Count; i++)
            if (!allIds.Add(LeaderCatalog.All[i].Id))
                throw new Exception("指導者ID重複: " + LeaderCatalog.All[i].Id);

        foreach (var pair in ExpectedLeaders)
        {
            var leaders = LeaderCatalog.ForCivilization(pair.Key);
            if (leaders.Count != 2)
                throw new Exception(pair.Key + "の指導者数が不正: " + leaders.Count);
            for (int i = 0; i < pair.Value.Length; i++)
            {
                var leader = LeaderCatalog.Find(pair.Value[i]);
                if (leader == null || !leader.NameKnown ||
                    !LeaderCatalog.BelongsTo(leader.Id, pair.Key) ||
                    string.IsNullOrEmpty(leader.NameJa) || string.IsNullOrEmpty(leader.TitleJa) ||
                    string.IsNullOrEmpty(leader.PeriodJa) || string.IsNullOrEmpty(leader.SummaryJa))
                    throw new Exception("新指導者の必須データ・所属が不正: " + pair.Value[i]);
                if (leaders[i].Id != pair.Value[i])
                    throw new Exception(pair.Key + "の指導者順序が不正");
            }
            if (LeaderCatalog.DefaultForCivilization(pair.Key).Id != pair.Value[0])
                throw new Exception(pair.Key + "の既定指導者が不正");
        }
        Debug.Log("[LeaderExpansion] 第2～第4弾 36文明×2指導者・参照・既定選択 OK");
    }

    static void ValidateFourthBatchSelectionAndSave()
    {
        var state = GameBootstrap.BuildNewGame(new GameConfig
        {
            Seed = 80,
            HumanPlayerIndex = 0,
            MapWidth = 20,
            MapHeight = 12,
            NumPlayers = 1,
        }, new[] { "rarotonga" }, new[] { "makea_takau" });
        if (state.HumanPlayer == null || state.HumanPlayer.CivilizationId != "rarotonga" ||
            state.HumanPlayer.LeaderId != "makea_takau" ||
            state.HumanPlayer.NameJa != "ラロトンガ王国")
            throw new Exception("第4弾文明・指導者の指定ゲーム構築に失敗");

        string json = SaveLoad.Serialize(state);
        var restored = SaveLoad.Deserialize(json);
        if (restored.HumanPlayer == null || restored.HumanPlayer.CivilizationId != "rarotonga" ||
            restored.HumanPlayer.LeaderId != "makea_takau" ||
            SaveLoad.Serialize(restored) != json)
            throw new Exception("第4弾文明・指導者の決定的セーブ往復に失敗");
        Debug.Log("[CivilizationExpansion] 第4弾選択・セーブ往復 OK");
    }

    static void ValidateVisualAsset()
    {
        var texture = Resources.Load<Texture2D>("History/civilization_leader_emblems");
        if (texture == null) throw new Exception("文明・指導者アイコンをResourcesから読めない");
        float ratio = texture.width / (float)texture.height;
        if (texture.width < 1000 || texture.height < 500 || Math.Abs(ratio - 2f) > 0.02f)
            throw new Exception("文明・指導者アイコンの解像度・比率が不正: " +
                texture.width + "x" + texture.height);
        Debug.Log("[CivilizationExpansion] 図鑑アイコン " + texture.width + "x" +
            texture.height + " OK");
    }
}
