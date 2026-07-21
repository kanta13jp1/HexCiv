using System;
using UnityEditor;
using UnityEngine;
using HexCiv.Core;

/// <summary>研究史・文化史第2・第3弾の後方追加、ゲーム接続、図鑑画像を検証する。</summary>
public static class ResearchCultureExpansionSmokeTest
{
    static readonly string[] Regions =
    {
        "アフリカ", "西・南アジア", "東・東南アジア",
        "ヨーロッパ・地中海", "アメリカ大陸", "オセアニア"
    };

    static readonly string[] PreviousResearchTails =
    {
        "african_paleontology", "kerala_infinite_series", "japan_microbiology",
        "radioactivity_research", "crispr_gene_editing", "indigenous_genomics_ethics"
    };

    static readonly string[,] PreviousExpansionResearch =
    {
        { "benin_earthwork_engineering", "aksum_coinage_metrology" },
        { "abbasid_bimaristan_medicine", "maratha_fort_network_design" },
        { "ryukyu_forest_administration", "burmese_calendar_computation" },
        { "agricola_mining_science", "hevelius_lunar_cartography" },
        { "muisca_lost_wax_metallurgy", "taino_conuco_agriculture" },
        { "tongan_voyaging_canoe_engineering", "samoan_agroforestry" },
    };

    static readonly string[,] AddedResearch =
    {
        { "asante_goldweight_casting_metrology", "buganda_barkcloth_making" },
        { "kushan_gold_coinage", "sikh_khalsa_artillery_reform" },
        { "goryeo_metal_movable_type", "aceh_ottoman_artillery_exchange" },
        { "venetian_arsenal_workflow", "hungarian_court_astronomy" },
        { "zapotec_calendar_epigraphy", "cherokee_syllabary_printing" },
        { "rapa_nui_moai_quarry_engineering", "fijian_drua_navigation" },
    };

    static readonly string[] PreviousCultureTails =
    {
        "ubuntu_philosophy", "yoga_traditions", "thai_khon",
        "bauhaus", "latin_american_magical_realism", "papua_new_guinea_bilum"
    };

    static readonly string[,] PreviousExpansionCulture =
    {
        { "benin_court_guild_culture", "ethiopian_epiphany_timkat" },
        { "majlis_social_space", "marathi_powada" },
        { "ryukyu_kumiodori", "myanmar_yoke_the" },
        { "meistersinger_guild_culture", "polonaise_dance" },
        { "muisca_offering_tradition", "taino_areito" },
        { "tongan_lakalaka", "faa_samoa" },
    };

    static readonly string[,] AddedCulture =
    {
        { "asante_stool_symbolism", "buganda_royal_drum_culture" },
        { "kushan_gandhara_cosmopolitan_art", "sikh_langar_seva" },
        { "goryeo_celadon_sanggam", "aceh_hikayat_court_literature" },
        { "venetian_mask_carnival", "matthias_corvina_humanism" },
        { "zapotec_guelaguetza_reciprocity", "cherokee_stomp_dance_community" },
        { "rapa_nui_kai_kai", "fijian_tabua_exchange" },
    };

    public static void Run()
    {
        try
        {
            ValidateResearch();
            ValidateCulture();
            ValidateVisualAsset();
            Debug.Log("RESEARCH CULTURE EXPANSION SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("RESEARCH CULTURE EXPANSION SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateResearch()
    {
        if (ResearchMilestoneCatalog.All.Count != 120 || TechnologyCatalog.All.Count != 132)
            throw new Exception("研究史または全技術件数が不正");

        for (int r = 0; r < Regions.Length; r++)
        {
            var milestones = ResearchMilestoneCatalog.ForRegion(Regions[r]);
            var technologies = TechnologyCatalog.HistoricalForRegion(Regions[r]);
            if (milestones.Count != 20 || technologies.Count != 20)
                throw new Exception("研究分岐件数が不正: " + Regions[r]);
            if (milestones[15].Id != PreviousResearchTails[r] ||
                milestones[16].Id != PreviousExpansionResearch[r, 0] ||
                milestones[17].Id != PreviousExpansionResearch[r, 1] ||
                milestones[18].Id != AddedResearch[r, 0] ||
                milestones[19].Id != AddedResearch[r, 1])
                throw new Exception("研究史の後方追加順が不正: " + Regions[r]);

            for (int tier = 18; tier < 20; tier++)
            {
                var technology = technologies[tier];
                int expectedCost = TechnologyCatalog.HistoricalBaseCost +
                    tier * TechnologyCatalog.HistoricalTierCost;
                if (technology.Cost != expectedCost || technology.Prereqs.Length != 1 ||
                    technology.Prereqs[0] != technologies[tier - 1].Id ||
                    TechnologyCatalog.MilestoneForTech(technology.Id) != milestones[tier])
                    throw new Exception("追加研究の接続が不正: " + technology.Id);
            }
        }
        Debug.Log("[Expansion] 研究史120件・全132技術・6地域×第3弾追加2件 OK");
    }

    static void ValidateCulture()
    {
        if (CulturalTraditionCatalog.All.Count != 120 || CulturePolicyCatalog.All.Count != 120)
            throw new Exception("文化史または文化政策件数が不正");

        for (int r = 0; r < Regions.Length; r++)
        {
            var traditions = CulturalTraditionCatalog.ForRegion(Regions[r]);
            var policies = CulturePolicyCatalog.ForRegion(Regions[r]);
            if (traditions.Count != 20 || policies.Count != 20)
                throw new Exception("文化分岐件数が不正: " + Regions[r]);
            if (traditions[15].Id != PreviousCultureTails[r] ||
                traditions[16].Id != PreviousExpansionCulture[r, 0] ||
                traditions[17].Id != PreviousExpansionCulture[r, 1] ||
                traditions[18].Id != AddedCulture[r, 0] ||
                traditions[19].Id != AddedCulture[r, 1])
                throw new Exception("文化史の後方追加順が不正: " + Regions[r]);

            for (int tier = 18; tier < 20; tier++)
            {
                var policy = policies[tier];
                int expectedCost = CulturePolicyCatalog.BaseCost +
                    tier * CulturePolicyCatalog.TierCost;
                if (policy.Cost != expectedCost || policy.Prereqs.Length != 1 ||
                    policy.Prereqs[0] != policies[tier - 1].Id ||
                    CulturePolicyCatalog.TraditionForPolicy(policy.Id) != traditions[tier])
                    throw new Exception("追加文化政策の接続が不正: " + policy.Id);
            }
        }
        Debug.Log("[Expansion] 文化史・文化政策120件・6地域×第3弾追加2件 OK");
    }

    static void ValidateVisualAsset()
    {
        var texture = Resources.Load<Texture2D>("History/research_culture_emblems");
        if (texture == null) throw new Exception("研究・文化アイコンをResourcesから読めない");
        if (texture.width < 1000 || texture.height < 500 ||
            Math.Abs(texture.width / (float)texture.height - 2f) > 0.02f)
            throw new Exception("研究・文化アイコンの解像度・比率が不正: " +
                texture.width + "x" + texture.height);
        Debug.Log("[Expansion] 研究・文化2分割アイコン " + texture.width + "x" +
            texture.height + " OK");
    }
}
