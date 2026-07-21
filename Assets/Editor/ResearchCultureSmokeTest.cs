using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HexCiv.Core;

/// <summary>世界史図鑑の研究史・文化史台帳をヘッドレス検証する。</summary>
public static class ResearchCultureSmokeTest
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
            ValidateResearch();
            ValidateCulture();
            Debug.Log("RESEARCH CULTURE SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("RESEARCH CULTURE SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateResearch()
    {
        if (ResearchMilestoneCatalog.All.Count != 120)
            throw new Exception("研究史件数が不正: " + ResearchMilestoneCatalog.All.Count);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < ResearchMilestoneCatalog.All.Count; i++)
        {
            var item = ResearchMilestoneCatalog.All[i];
            if (item == null || string.IsNullOrEmpty(item.Id) || string.IsNullOrEmpty(item.NameJa) ||
                string.IsNullOrEmpty(item.RegionJa) || string.IsNullOrEmpty(item.PeriodJa) ||
                string.IsNullOrEmpty(item.DomainJa) || string.IsNullOrEmpty(item.SummaryJa))
                throw new Exception("研究史の必須データ不足(index=" + i + ")");
            if (!ids.Add(item.Id)) throw new Exception("研究史ID重複: " + item.Id);
            if (ResearchMilestoneCatalog.Find(item.Id) != item)
                throw new Exception("研究史ID検索が不正: " + item.Id);
        }

        ValidateRegionCounts("研究史", region => ResearchMilestoneCatalog.ForRegion(region).Count);
        if (ResearchMilestoneCatalog.ForRegion(null).Count != ResearchMilestoneCatalog.All.Count)
            throw new Exception("研究史の全地域フィルターが不正");

        Debug.Log("[WorldHistory] 研究史台帳OK: 120件（6地域×20件）");
    }

    static void ValidateCulture()
    {
        if (CulturalTraditionCatalog.All.Count != 120)
            throw new Exception("文化史件数が不正: " + CulturalTraditionCatalog.All.Count);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < CulturalTraditionCatalog.All.Count; i++)
        {
            var item = CulturalTraditionCatalog.All[i];
            if (item == null || string.IsNullOrEmpty(item.Id) || string.IsNullOrEmpty(item.NameJa) ||
                string.IsNullOrEmpty(item.RegionJa) || string.IsNullOrEmpty(item.PeriodJa) ||
                string.IsNullOrEmpty(item.DomainJa) || string.IsNullOrEmpty(item.SummaryJa))
                throw new Exception("文化史の必須データ不足(index=" + i + ")");
            if (!ids.Add(item.Id)) throw new Exception("文化史ID重複: " + item.Id);
            if (CulturalTraditionCatalog.Find(item.Id) != item)
                throw new Exception("文化史ID検索が不正: " + item.Id);
        }

        ValidateRegionCounts("文化史", region => CulturalTraditionCatalog.ForRegion(region).Count);
        if (CulturalTraditionCatalog.ForRegion(null).Count != CulturalTraditionCatalog.All.Count)
            throw new Exception("文化史の全地域フィルターが不正");

        Debug.Log("[WorldHistory] 文化史台帳OK: 120件（6地域×20件）");
    }

    static void ValidateRegionCounts(string label, Func<string, int> countForRegion)
    {
        for (int i = 0; i < Regions.Length; i++)
            if (countForRegion(Regions[i]) != 20)
                throw new Exception(label + "の地域件数が不正: " + Regions[i] +
                    "=" + countForRegion(Regions[i]));
    }
}
