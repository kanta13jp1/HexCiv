using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HexCiv.Core;

/// <summary>基礎12技術と研究史132件を結ぶ拡張技術ツリーをヘッドレス検証する。</summary>
public static class ResearchTechTreeSmokeTest
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
            ValidateBranches();
            ValidatePlayerAvailability();
            Debug.Log("RESEARCH TECH TREE SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("RESEARCH TECH TREE SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateCatalog()
    {
        int expected = GameRules.Techs.Count + ResearchMilestoneCatalog.All.Count;
        if (TechnologyCatalog.All.Count != expected || expected != 144)
            throw new Exception("全技術件数が不正: " + TechnologyCatalog.All.Count);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < TechnologyCatalog.All.Count; i++)
        {
            var tech = TechnologyCatalog.All[i];
            if (tech == null || string.IsNullOrEmpty(tech.Id) || string.IsNullOrEmpty(tech.NameJa) ||
                tech.Cost < 0 || tech.Prereqs == null || string.IsNullOrEmpty(tech.DescJa))
                throw new Exception("技術必須データ不足(index=" + i + ")");
            if (!ids.Add(tech.Id)) throw new Exception("技術ID重複: " + tech.Id);
            if (TechnologyCatalog.Get(tech.Id) != tech)
                throw new Exception("技術ID検索が不正: " + tech.Id);
        }

        for (int i = 0; i < TechnologyCatalog.All.Count; i++)
            for (int p = 0; p < TechnologyCatalog.All[i].Prereqs.Length; p++)
                if (!ids.Contains(TechnologyCatalog.All[i].Prereqs[p]))
                    throw new Exception("未知の前提技術: " + TechnologyCatalog.All[i].Id + " -> " +
                        TechnologyCatalog.All[i].Prereqs[p]);

        for (int i = 0; i < GameRules.Techs.Count; i++)
            if (TechnologyCatalog.All[i] != GameRules.Techs[i])
                throw new Exception("既存技術の順序・参照が変化: " + GameRules.Techs[i].Id);

        Debug.Log("[ResearchTree] 全144技術・既存12技術互換OK");
    }

    static void ValidateBranches()
    {
        var rootRequirements = new HashSet<string>(TechnologyCatalog.HistoricalRootPrerequisites,
            StringComparer.OrdinalIgnoreCase);

        for (int r = 0; r < Regions.Length; r++)
        {
            var milestones = ResearchMilestoneCatalog.ForRegion(Regions[r]);
            var techs = TechnologyCatalog.HistoricalForRegion(Regions[r]);
            if (milestones.Count != 22 || techs.Count != 22)
                throw new Exception("地域分岐件数が不正: " + Regions[r]);

            for (int i = 0; i < techs.Count; i++)
            {
                var tech = techs[i];
                if (TechnologyCatalog.MilestoneForTech(tech.Id) != milestones[i])
                    throw new Exception("研究史対応が不正: " + tech.Id);
                int expectedCost = TechnologyCatalog.HistoricalBaseCost +
                    i * TechnologyCatalog.HistoricalTierCost;
                if (tech.Cost != expectedCost)
                    throw new Exception("研究コストが不正: " + tech.Id);

                if (i == 0)
                {
                    if (!rootRequirements.SetEquals(tech.Prereqs))
                        throw new Exception("地域ルート前提が不正: " + tech.Id);
                }
                else if (tech.Prereqs.Length != 1 || tech.Prereqs[0] != techs[i - 1].Id)
                {
                    throw new Exception("地域内の時系列接続が不正: " + tech.Id);
                }
            }
        }

        Debug.Log("[ResearchTree] 6地域×22段階・前提関係・コスト上昇OK");
    }

    static void ValidatePlayerAvailability()
    {
        var player = new Player();
        player.KnownTechs.Clear();
        for (int i = 0; i < GameRules.Techs.Count; i++) player.KnownTechs.Add(GameRules.Techs[i].Id);

        var available = player.AvailableTechs();
        if (available.Count != Regions.Length)
            throw new Exception("基礎技術完了後の地域ルート数が不正: " + available.Count);

        for (int r = 0; r < Regions.Length; r++)
        {
            string rootId = TechnologyCatalog.HistoricalForRegion(Regions[r])[0].Id;
            if (!available.Any(t => t.Id == rootId))
                throw new Exception("地域ルートが研究可能にならない: " + Regions[r]);
        }

        var africa = TechnologyCatalog.HistoricalForRegion("アフリカ");
        player.KnownTechs.Add(africa[0].Id);
        available = player.AvailableTechs();
        if (!available.Any(t => t.Id == africa[1].Id) || available.Any(t => t.Id == africa[0].Id))
            throw new Exception("地域分岐の次段階解禁が不正");

        player.SetResearch(africa[1].Id);
        if (TechnologyCatalog.Get(player.CurrentResearchId) != africa[1])
            throw new Exception("研究選択の接続が不正");

        Debug.Log("[ResearchTree] Player研究可能判定・選択接続OK");
    }
}
