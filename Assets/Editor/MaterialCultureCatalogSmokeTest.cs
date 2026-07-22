using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>生活・技術史72件の一意性、12分類、6地域分割を検証する。</summary>
public static class MaterialCultureCatalogSmokeTest
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
            Validate();
            Debug.Log("MATERIAL CULTURE CATALOG SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("MATERIAL CULTURE CATALOG SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    public static void Validate()
    {
        if (MaterialCultureCatalog.All.Count != 72)
            throw new Exception("生活・技術史台帳件数が不正: " + MaterialCultureCatalog.All.Count);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var regionalTotal = 0;
        for (int r = 0; r < Regions.Length; r++)
        {
            var regional = MaterialCultureCatalog.ForRegion(Regions[r]);
            if (regional.Count != 12)
                throw new Exception(Regions[r] + "の件数が不正: " + regional.Count);
            regionalTotal += regional.Count;
            foreach (MaterialCultureKind kind in Enum.GetValues(typeof(MaterialCultureKind)))
                if (MaterialCultureCatalog.CountKind(regional, kind) != 1)
                    throw new Exception(Regions[r] + "の分類が1件でない: " + kind);
        }
        if (regionalTotal != MaterialCultureCatalog.All.Count)
            throw new Exception("6地域合計が全件と一致しない");

        for (int i = 0; i < MaterialCultureCatalog.All.Count; i++)
        {
            var item = MaterialCultureCatalog.All[i];
            if (string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id))
                throw new Exception("IDが空または重複: " + item.Id);
            if (string.IsNullOrWhiteSpace(item.NameJa) || !names.Add(item.NameJa))
                throw new Exception("名称が空または重複: " + item.NameJa);
            if (string.IsNullOrWhiteSpace(item.PeriodJa) ||
                string.IsNullOrWhiteSpace(item.PlaceJa) ||
                string.IsNullOrWhiteSpace(item.SummaryJa))
                throw new Exception("説明不足: " + item.Id);
            if (MaterialCultureCatalog.Find(item.Id) != item)
                throw new Exception("Find往復に失敗: " + item.Id);
        }
        Debug.Log("[MaterialCulture] 12分類×6地域=72件、一意性・完全分割 OK");
    }
}
