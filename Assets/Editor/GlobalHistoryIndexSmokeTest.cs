using System;
using System.Collections.Generic;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>全世界史台帳の総合索引、6地域写像、図鑑画像をヘッドレス検証する。</summary>
public static class GlobalHistoryIndexSmokeTest
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
            ValidateAllCounts();
            ValidateRegionalPartition();
            ValidateVisualAsset();
            Debug.Log("GLOBAL HISTORY INDEX SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("GLOBAL HISTORY INDEX SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static void ValidateAllCounts()
    {
        var entries = GlobalHistoryIndex.Entries(GlobalHistoryIndex.AllRegions);
        if (entries.Count != 13) throw new Exception("総合分類数が不正: " + entries.Count);

        var expected = new Dictionary<string, int>
        {
            { "civilizations", 92 }, { "leaders", 179 }, { "heritage", 108 },
            { "great_people", 120 }, { "books", 42 }, { "paintings", 42 },
            { "sculptures", 42 }, { "architecture", 42 }, { "music", 42 },
            { "theater", 42 }, { "film", 42 }, { "research", 120 }, { "culture", 120 },
        };
        int total = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (!expected.ContainsKey(entry.Id) || expected[entry.Id] != entry.Count)
                throw new Exception("総合索引件数が不正: " + entry.Id + "=" + entry.Count);
            if (string.IsNullOrEmpty(entry.NameJa) || string.IsNullOrEmpty(entry.DetailJa))
                throw new Exception("総合索引説明が不足: " + entry.Id);
            total += entry.Count;
        }
        if (total != 1033) throw new Exception("台帳レコード総数が不正: " + total);
        Debug.Log("[GlobalHistory] 13分類・台帳1033件 OK");
    }

    static void ValidateRegionalPartition()
    {
        var all = GlobalHistoryIndex.Entries(GlobalHistoryIndex.AllRegions);
        var sums = new Dictionary<string, int>();
        for (int i = 0; i < all.Count; i++) sums[all[i].Id] = 0;

        for (int r = 0; r < Regions.Length; r++)
        {
            var regional = GlobalHistoryIndex.Entries(Regions[r]);
            if (regional.Count != all.Count)
                throw new Exception(Regions[r] + "の分類数が不正");
            for (int i = 0; i < regional.Count; i++)
            {
                if (regional[i].Count <= 0)
                    throw new Exception(Regions[r] + "に0件の分類: " + regional[i].Id);
                sums[regional[i].Id] += regional[i].Count;
            }
        }

        for (int i = 0; i < all.Count; i++)
            if (sums[all[i].Id] != all[i].Count)
                throw new Exception("6地域の合計が全件と不一致: " + all[i].Id + " " +
                    sums[all[i].Id] + "/" + all[i].Count);

        for (int i = 0; i < CivilizationCatalog.All.Count; i++)
            if (string.IsNullOrEmpty(GlobalHistoryIndex.BroadRegion(CivilizationCatalog.All[i])))
                throw new Exception("文明の6地域写像がない: " + CivilizationCatalog.All[i].Id);
        if (GlobalHistoryIndex.LeadersForRegion(GlobalHistoryIndex.AllRegions).Count !=
            LeaderCatalog.All.Count)
            throw new Exception("指導者の地域写像に欠落がある");

        Debug.Log("[GlobalHistory] 文明・指導者を含む6地域の完全分割 OK");
    }

    static void ValidateVisualAsset()
    {
        var texture = Resources.Load<Texture2D>("History/world_history_banner");
        if (texture == null) throw new Exception("世界史図鑑バナーをResourcesから読めない");
        if (texture.width < 1000 || texture.height < 500)
            throw new Exception("世界史図鑑バナーの解像度不足: " + texture.width + "x" + texture.height);
        var emblems = Resources.Load<Texture2D>("History/theater_film_emblems");
        if (emblems == null) throw new Exception("演劇・映画アイコンをResourcesから読めない");
        if (emblems.width < 1000 || emblems.height < 500 ||
            Math.Abs(emblems.width / (float)emblems.height - 2f) > 0.02f)
            throw new Exception("演劇・映画アイコンの解像度・比率が不正: " +
                emblems.width + "x" + emblems.height);
        var civilizationLeaderEmblems = Resources.Load<Texture2D>(
            "History/civilization_leader_emblems");
        if (civilizationLeaderEmblems == null)
            throw new Exception("文明・指導者アイコンをResourcesから読めない");
        if (civilizationLeaderEmblems.width < 1000 || civilizationLeaderEmblems.height < 500 ||
            Math.Abs(civilizationLeaderEmblems.width /
                (float)civilizationLeaderEmblems.height - 2f) > 0.02f)
            throw new Exception("文明・指導者アイコンの解像度・比率が不正: " +
                civilizationLeaderEmblems.width + "x" + civilizationLeaderEmblems.height);
        var heritageGreatPeopleEmblems = Resources.Load<Texture2D>(
            "History/heritage_great_people_emblems");
        if (heritageGreatPeopleEmblems == null)
            throw new Exception("遺跡・偉人アイコンをResourcesから読めない");
        if (heritageGreatPeopleEmblems.width < 1000 || heritageGreatPeopleEmblems.height < 500 ||
            Math.Abs(heritageGreatPeopleEmblems.width /
                (float)heritageGreatPeopleEmblems.height - 2f) > 0.02f)
            throw new Exception("遺跡・偉人アイコンの解像度・比率が不正: " +
                heritageGreatPeopleEmblems.width + "x" + heritageGreatPeopleEmblems.height);
        var researchCultureEmblems = Resources.Load<Texture2D>(
            "History/research_culture_emblems");
        if (researchCultureEmblems == null)
            throw new Exception("研究・文化アイコンをResourcesから読めない");
        if (researchCultureEmblems.width < 1000 || researchCultureEmblems.height < 500 ||
            Math.Abs(researchCultureEmblems.width /
                (float)researchCultureEmblems.height - 2f) > 0.02f)
            throw new Exception("研究・文化アイコンの解像度・比率が不正: " +
                researchCultureEmblems.width + "x" + researchCultureEmblems.height);
        var masterpieceEmblems = Resources.Load<Texture2D>("History/masterpiece_emblems");
        if (masterpieceEmblems == null)
            throw new Exception("作品7分類アイコンをResourcesから読めない");
        if (masterpieceEmblems.width < 1000 || masterpieceEmblems.height < 500 ||
            Math.Abs(masterpieceEmblems.width /
                (float)masterpieceEmblems.height - 2f) > 0.02f)
            throw new Exception("作品7分類アイコンの解像度・比率が不正: " +
                masterpieceEmblems.width + "x" + masterpieceEmblems.height);
        Debug.Log("[GlobalHistory] オリジナル図鑑バナー " + texture.width + "x" +
            texture.height + " / 演劇・映画アイコン " + emblems.width + "x" +
            emblems.height + " / 文明・指導者アイコン " +
            civilizationLeaderEmblems.width + "x" + civilizationLeaderEmblems.height +
            " / 遺跡・偉人アイコン " + heritageGreatPeopleEmblems.width + "x" +
            heritageGreatPeopleEmblems.height + " / 研究・文化アイコン " +
            researchCultureEmblems.width + "x" + researchCultureEmblems.height +
            " / 作品7分類アイコン " + masterpieceEmblems.width + "x" +
            masterpieceEmblems.height + " OK");
    }
}
