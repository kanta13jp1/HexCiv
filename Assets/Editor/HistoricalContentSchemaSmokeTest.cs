using System;
using System.Linq;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>共通史実台帳の安定ID・史料・効果・素材権利ゲートを検証する。</summary>
public static class HistoricalContentSchemaSmokeTest
{
    [MenuItem("HexCiv/Run Historical Content Schema Smoke Test")]
    public static void Run()
    {
        try
        {
            var dataset = CreateValidDataset();
            AssertValid(dataset);

            string json = JsonUtility.ToJson(dataset);
            var restored = JsonUtility.FromJson<HistoricalContentDataset>(json);
            AssertValid(restored);
            if (JsonUtility.ToJson(restored) != json)
                throw new Exception("共通史実台帳JSONが決定的に往復しない");

            dataset.records[0].reviewStatus = "review";
            if (!HistoricalContentValidator.ValidateForRelease(dataset)
                .Any(e => e.Contains("verified必須")))
                throw new Exception("要確認レコードが製品ゲートを通過した");
            dataset.records[0].reviewStatus = "verified";

            dataset.records[0].assets[0].reconstructionLabelJa = "";
            if (!HistoricalContentValidator.ValidateForRelease(dataset)
                .Any(e => e.Contains("生成復元素材")))
                throw new Exception("復元表示のない生成素材が製品ゲートを通過した");

            Debug.Log("HISTORICAL CONTENT SCHEMA SMOKE OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("HISTORICAL CONTENT SCHEMA SMOKE FAIL: " + ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static void AssertValid(HistoricalContentDataset dataset)
    {
        var errors = HistoricalContentValidator.ValidateForRelease(dataset);
        if (errors.Count != 0)
            throw new Exception("共通史実台帳検証エラー: " +
                string.Join(" | ", errors));
    }

    static HistoricalContentDataset CreateValidDataset()
    {
        return new HistoricalContentDataset
        {
            schemaVersion = 1,
            datasetVersion = 1,
            id = "uruk_vertical_slice_catalog",
            reviewStatus = "verified",
            sources = new[]
            {
                new HistoricalSourceDefinition
                {
                    id = "museum_reference",
                    publisher = "Example Museum",
                    title = "Example verified object record",
                    url = "https://example.org/object/1",
                    sourceType = "museum",
                    accessedDate = "2026-07-24",
                    usage = "reference_only",
                    reviewStatus = "verified",
                },
            },
            records = new[]
            {
                new HistoricalContentRecord
                {
                    id = "example_uruk_record",
                    category = "institution",
                    name = Text("content.example.name", "記録行政"),
                    originalName = "名称不詳",
                    transliteration = "—",
                    alternateNames = new[] { "初期記録制度" },
                    regionId = "southern_mesopotamia",
                    earliestYear = -3600,
                    latestYear = -3000,
                    confidence = "inferred",
                    reviewStatus = "verified",
                    summary = Text("content.example.summary",
                        "数量管理と再分配を支える記録実務。"),
                    alternativeView = Text("content.example.alternative",
                        "制度化の時期と範囲には異説がある。"),
                    sourceRefs = new[] { "museum_reference" },
                    relatedIds = new[] { "alluvial_clay" },
                    effects = new[]
                    {
                        new HistoricalGameplayEffect
                        {
                            id = "reduce_distribution_loss",
                            effectType = "percent_modifier",
                            target = "distribution_loss",
                            amount = -10,
                            unit = "percent",
                            benefit = Text("effect.example.benefit",
                                "配給時の損失を軽減する。"),
                            socialCost = Text("effect.example.cost",
                                "記録担当者と粘土を継続的に必要とする。"),
                        },
                    },
                    assets = new[]
                    {
                        new HistoricalAssetProvenance
                        {
                            id = "example_reconstruction_icon",
                            kind = "icon",
                            origin = "generated_reconstruction",
                            generatorId = "development_placeholder",
                            promptHash = "sha256_example",
                            reviewedBy = "development_review",
                            reconstructionLabelJa = "復元イメージ",
                            reviewStatus = "verified",
                        },
                    },
                },
            },
        };
    }

    static HistoricalLocalizedText Text(string key, string ja)
    {
        return new HistoricalLocalizedText { key = key, ja = ja };
    }
}
