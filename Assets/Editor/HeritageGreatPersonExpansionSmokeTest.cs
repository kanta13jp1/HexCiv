using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>遺跡・偉人第2・3弾の後方互換、地域配分、文明参照、図鑑画像を検証する。</summary>
public static class HeritageGreatPersonExpansionSmokeTest
{
    static readonly Dictionary<string, string> SecondBatchSites =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "benin_iya", "benin" }, { "fasil_ghebbi", "ethiopia" },
            { "samarra", "abbasid" }, { "raigad_fort", "maratha" },
            { "ryukyu_gusuku", "ryukyu" }, { "kanbawzathadi", "toungoo" },
            { "speyer_cathedral", "holy_roman" },
            { "wawel_castle", "polish_lithuanian" },
            { "el_infiernito", "muisca" }, { "caguana", "taino" },
            { "haamonga_a_maui", "tonga" }, { "pulemelei_mound", "samoa" },
        };

    static readonly Dictionary<string, string> SecondBatchPeople =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "jacob_egharevba", "benin" }, { "giyorgis_segla", "ethiopia" },
            { "hunayn_ibn_ishaq", "abbasid" }, { "dnyaneshwar", "" },
            { "sai_on", "ryukyu" }, { "nawade_i", "toungoo" },
            { "hildegard_bingen", "holy_roman" },
            { "jan_kochanowski", "polish_lithuanian" },
            { "guaman_poma", "inca" }, { "edmonia_lewis", "" },
            { "epeli_hauofa", "tonga" }, { "albert_wendt", "samoa" },
        };

    static readonly Dictionary<string, string> ThirdBatchSites =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "asante_traditional_buildings", "asante" }, { "kasubi_tombs", "buganda" },
            { "surkh_kotal", "kushan" }, { "ram_bagh_palace", "sikh_empire" },
            { "kaesong_koryo_monuments", "goryeo" }, { "gunongan_aceh", "aceh" },
            { "venice_lagoon", "venice" }, { "visegrad_royal_palace", "hungary" },
            { "mitla", "zapotec" }, { "new_echota", "cherokee" },
            { "orongo_ceremonial_village", "rapa_nui" },
            { "levuka_historic_port", "fiji" },
        };

    static readonly Dictionary<string, string> ThirdBatchPeople =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "okomfo_anokye", "asante" }, { "apollo_kaggwa", "buganda" },
            { "ashvaghosha", "kushan" }, { "fakir_azizuddin", "sikh_empire" },
            { "uicheon", "goryeo" }, { "hamzah_fansuri", "aceh" },
            { "elena_cornaro_piscopia", "venice" }, { "janos_vitez", "hungary" },
            { "andres_henestrosa", "zapotec" }, { "mary_golda_ross", "cherokee" },
            { "juan_tepano", "rapa_nui" }, { "ratu_sukuna", "fiji" },
        };

    static readonly string[] Regions =
    {
        "アフリカ", "西・南アジア", "東・東南アジア",
        "ヨーロッパ・地中海", "アメリカ大陸", "オセアニア"
    };

    public static void Run()
    {
        try
        {
            ValidateCatalogs();
            ValidateRegionalBatch();
            ValidateVisualAsset();
            Debug.Log("HERITAGE GREAT PERSON EXPANSION SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("HERITAGE GREAT PERSON EXPANSION SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateCatalogs()
    {
        if (HeritageSiteCatalog.All.Count != 108 || GreatPersonCatalog.All.Count != 120)
            throw new Exception("拡張後の台帳件数が不正");

        if (HeritageSiteCatalog.All[95].Id != "pulemelei_mound" ||
            HeritageSiteCatalog.All[107].Id != "levuka_historic_port")
            throw new Exception("遺跡台帳の後方追加順序が不正");
        if (GreatPersonCatalog.All[107].Id != "albert_wendt" ||
            GreatPersonCatalog.All[119].Id != "ratu_sukuna")
            throw new Exception("偉人台帳の後方追加順序が不正");

        ValidateUniqueAndRequiredFields();
        ValidateSites(SecondBatchSites);
        ValidateSites(ThirdBatchSites);
        ValidatePeople(SecondBatchPeople);
        ValidatePeople(ThirdBatchPeople);
    }

    static void ValidateSites(Dictionary<string, string> sites)
    {
        foreach (var pair in sites)
        {
            var site = HeritageSiteCatalog.Find(pair.Key);
            if (site == null) throw new Exception("追加遺跡がない: " + pair.Key);
            if (!string.Equals(site.RelatedCivilizationId, pair.Value,
                StringComparison.OrdinalIgnoreCase))
                throw new Exception("追加遺跡の文明参照が不正: " + pair.Key);
            if (CivilizationCatalog.Find(pair.Value) == null)
                throw new Exception("追加遺跡の参照文明がない: " + pair.Value);
        }
    }

    static void ValidatePeople(Dictionary<string, string> people)
    {
        foreach (var pair in people)
        {
            var person = GreatPersonCatalog.Find(pair.Key);
            if (person == null) throw new Exception("追加偉人がない: " + pair.Key);
            if (!string.Equals(person.RelatedCivilizationId, pair.Value,
                StringComparison.OrdinalIgnoreCase))
                throw new Exception("追加偉人の文明参照が不正: " + pair.Key);
            if (!string.IsNullOrEmpty(pair.Value) && CivilizationCatalog.Find(pair.Value) == null)
                throw new Exception("追加偉人の参照文明がない: " + pair.Value);
            if (string.IsNullOrEmpty(WorldLegacySystem.EffectTextJa(person)))
                throw new Exception("追加偉人の効果がない: " + pair.Key);
        }
    }

    static void ValidateUniqueAndRequiredFields()
    {
        var siteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < HeritageSiteCatalog.All.Count; i++)
        {
            var site = HeritageSiteCatalog.All[i];
            if (!siteIds.Add(site.Id)) throw new Exception("遺跡ID重複: " + site.Id);
            if (string.IsNullOrEmpty(site.Id) || string.IsNullOrEmpty(site.NameJa) ||
                string.IsNullOrEmpty(site.RegionJa) || string.IsNullOrEmpty(site.LocationJa) ||
                string.IsNullOrEmpty(site.PeriodJa) || string.IsNullOrEmpty(site.TypeJa) ||
                string.IsNullOrEmpty(site.SummaryJa))
                throw new Exception("遺跡の必須情報不足: " + site.Id);
        }

        var personIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < GreatPersonCatalog.All.Count; i++)
        {
            var person = GreatPersonCatalog.All[i];
            if (!personIds.Add(person.Id)) throw new Exception("偉人ID重複: " + person.Id);
            if (string.IsNullOrEmpty(person.Id) || string.IsNullOrEmpty(person.NameJa) ||
                string.IsNullOrEmpty(person.RegionJa) || string.IsNullOrEmpty(person.PeriodJa) ||
                string.IsNullOrEmpty(person.CategoryJa) || string.IsNullOrEmpty(person.SummaryJa))
                throw new Exception("偉人の必須情報不足: " + person.Id);
        }
    }

    static void ValidateRegionalBatch()
    {
        var siteCounts = new Dictionary<string, int>();
        var peopleCounts = new Dictionary<string, int>();
        for (int i = 0; i < Regions.Length; i++)
        {
            siteCounts[Regions[i]] = 0;
            peopleCounts[Regions[i]] = 0;
        }

        var siteCivilizations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var personCivilizations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in ThirdBatchSites)
        {
            var site = HeritageSiteCatalog.Find(pair.Key);
            if (!siteCounts.ContainsKey(site.RegionJa))
                throw new Exception("追加遺跡の地域が不正: " + site.Id);
            siteCounts[site.RegionJa]++;
            siteCivilizations.Add(site.RelatedCivilizationId);
        }
        foreach (var pair in ThirdBatchPeople)
        {
            var person = GreatPersonCatalog.Find(pair.Key);
            if (!peopleCounts.ContainsKey(person.RegionJa))
                throw new Exception("追加偉人の地域が不正: " + person.Id);
            peopleCounts[person.RegionJa]++;
            personCivilizations.Add(person.RelatedCivilizationId);
        }

        for (int i = 0; i < Regions.Length; i++)
            if (siteCounts[Regions[i]] != 2 || peopleCounts[Regions[i]] != 2)
                throw new Exception(Regions[i] + "の追加配分が2件ずつではない");
        if (siteCivilizations.Count != 12 || personCivilizations.Count != 12)
            throw new Exception("第3弾の12文明対応が一対一ではない");
        Debug.Log("[Expansion] 第3弾を6地域×遺跡2件・偉人2人、12文明へ接続 OK");
    }

    static void ValidateVisualAsset()
    {
        var texture = Resources.Load<Texture2D>("History/heritage_great_people_emblems");
        if (texture == null) throw new Exception("遺跡・偉人エンブレムを読めない");
        if (texture.width < 1000 || texture.height < 500 ||
            Math.Abs(texture.width / (float)texture.height - 2f) > 0.02f)
            throw new Exception("エンブレム解像度・比率が不正: " + texture.width + "x" +
                texture.height);
        Debug.Log("[Expansion] 遺跡・偉人エンブレム " + texture.width + "x" +
            texture.height + " OK");
    }
}
