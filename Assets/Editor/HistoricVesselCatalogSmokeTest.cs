using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

public static class HistoricVesselCatalogSmokeTest
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
            Require(HistoricVesselCatalog.All.Count == 36,
                "歴史船舶台帳が36件ではない");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < HistoricVesselCatalog.All.Count; i++)
            {
                HistoricVesselDef vessel = HistoricVesselCatalog.All[i];
                Require(vessel != null && !string.IsNullOrEmpty(vessel.Id) &&
                    !string.IsNullOrEmpty(vessel.NameJa) &&
                    !string.IsNullOrEmpty(vessel.RegionJa) &&
                    !string.IsNullOrEmpty(vessel.EraJa) &&
                    !string.IsNullOrEmpty(vessel.TraditionJa) &&
                    !string.IsNullOrEmpty(vessel.RoleJa) &&
                    !string.IsNullOrEmpty(vessel.SummaryJa),
                    "歴史船舶の必須データ不足(index=" + i + ")");
                Require(ids.Add(vessel.Id), "歴史船舶ID重複: " + vessel.Id);
                Require(HistoricVesselCatalog.Find(vessel.Id) == vessel,
                    "歴史船舶ID検索が不正: " + vessel.Id);
            }

            int regionalTotal = 0;
            for (int i = 0; i < Regions.Length; i++)
            {
                var regional = HistoricVesselCatalog.ForRegion(Regions[i]);
                Require(regional.Count == 6,
                    Regions[i] + "の歴史船舶が6件ではない: " + regional.Count);
                regionalTotal += regional.Count;
            }
            Require(regionalTotal == HistoricVesselCatalog.All.Count,
                "6地域の歴史船舶合計が全件と一致しない");
            Require(HistoricVesselCatalog.Find("greek_trireme") != null &&
                HistoricVesselCatalog.Find("chinese_junk") != null &&
                HistoricVesselCatalog.Find("double_hulled_vaka") != null,
                "代表船舶が台帳にない");

            Debug.Log("HISTORIC VESSEL CATALOG SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("HISTORIC VESSEL CATALOG SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
