using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HexCiv.Core;

/// <summary>世界史図鑑の遺跡・偉人台帳をヘッドレス検証する。</summary>
public static class WorldHistorySmokeTest
{
    public static void Run()
    {
        try
        {
            ValidateSites();
            ValidatePeople();
            Debug.Log("WORLD HISTORY SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("WORLD HISTORY SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateSites()
    {
        if (HeritageSiteCatalog.All.Count != 120)
            throw new Exception("遺跡・史跡件数が不正: " + HeritageSiteCatalog.All.Count);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < HeritageSiteCatalog.All.Count; i++)
        {
            var item = HeritageSiteCatalog.All[i];
            if (item == null || string.IsNullOrEmpty(item.Id) || string.IsNullOrEmpty(item.NameJa) ||
                string.IsNullOrEmpty(item.RegionJa) || string.IsNullOrEmpty(item.LocationJa) ||
                string.IsNullOrEmpty(item.PeriodJa) || string.IsNullOrEmpty(item.TypeJa) ||
                string.IsNullOrEmpty(item.SummaryJa))
                throw new Exception("遺跡・史跡の必須データ不足(index=" + i + ")");
            if (!ids.Add(item.Id)) throw new Exception("遺跡・史跡ID重複: " + item.Id);
            if (!string.IsNullOrEmpty(item.RelatedCivilizationId) &&
                CivilizationCatalog.Find(item.RelatedCivilizationId) == null)
                throw new Exception("遺跡・史跡の文明参照が不正: " + item.Id);
        }

        if (HeritageSiteCatalog.ForRegion("アフリカ").Count != 20 ||
            HeritageSiteCatalog.ForRegion("西・南アジア").Count != 22 ||
            HeritageSiteCatalog.ForRegion("東・東南アジア").Count != 22 ||
            HeritageSiteCatalog.ForRegion("ヨーロッパ・地中海").Count != 22 ||
            HeritageSiteCatalog.ForRegion("アメリカ大陸").Count != 22 ||
            HeritageSiteCatalog.ForRegion("オセアニア").Count != 12 ||
            HeritageSiteCatalog.ForRegion(null).Count != HeritageSiteCatalog.All.Count)
            throw new Exception("遺跡・史跡の地域フィルターが不正");

        Debug.Log("[WorldHistory] 遺跡・史跡台帳OK: 120件");
    }

    static void ValidatePeople()
    {
        if (GreatPersonCatalog.All.Count != 132)
            throw new Exception("偉人件数が不正: " + GreatPersonCatalog.All.Count);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < GreatPersonCatalog.All.Count; i++)
        {
            var item = GreatPersonCatalog.All[i];
            if (item == null || string.IsNullOrEmpty(item.Id) || string.IsNullOrEmpty(item.NameJa) ||
                string.IsNullOrEmpty(item.RegionJa) || string.IsNullOrEmpty(item.PeriodJa) ||
                string.IsNullOrEmpty(item.CategoryJa) || string.IsNullOrEmpty(item.SummaryJa))
                throw new Exception("偉人の必須データ不足(index=" + i + ")");
            if (!ids.Add(item.Id)) throw new Exception("偉人ID重複: " + item.Id);
            if (!string.IsNullOrEmpty(item.RelatedCivilizationId) &&
                CivilizationCatalog.Find(item.RelatedCivilizationId) == null)
                throw new Exception("偉人の文明参照が不正: " + item.Id);
        }

        string[] regions =
        {
            "アフリカ", "西・南アジア", "東・東南アジア",
            "ヨーロッパ・地中海", "アメリカ大陸", "オセアニア"
        };
        for (int i = 0; i < regions.Length; i++)
            if (GreatPersonCatalog.ForRegion(regions[i]).Count != 22)
                throw new Exception("偉人の地域件数が不正: " + regions[i]);

        Debug.Log("[WorldHistory] 偉人台帳OK: 132人（6地域×22人）");
    }
}
