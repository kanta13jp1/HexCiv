using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HexCiv.Core
{
    /// <summary>
    /// 文明・人物・場所・技術・制度・作品を同じ安定IDで結ぶ史実台帳の共通契約。
    /// 個別カタログはこの形へ段階移行し、表示名を変えてもIDは変えない。
    /// </summary>
    [Serializable]
    public sealed class HistoricalContentDataset
    {
        public int schemaVersion;
        public int datasetVersion;
        public string id;
        public string reviewStatus;
        public HistoricalContentRecord[] records;
        public HistoricalSourceDefinition[] sources;
    }

    [Serializable]
    public sealed class HistoricalContentRecord
    {
        public string id;
        public string category;
        public HistoricalLocalizedText name;
        public string originalName;
        public string transliteration;
        public string[] alternateNames;
        public string regionId;
        public int earliestYear;
        public int latestYear;
        public string confidence;
        public string reviewStatus;
        public HistoricalLocalizedText summary;
        public HistoricalLocalizedText alternativeView;
        public string[] sourceRefs;
        public string[] relatedIds;
        public HistoricalGameplayEffect[] effects;
        public HistoricalAssetProvenance[] assets;
    }

    [Serializable]
    public sealed class HistoricalGameplayEffect
    {
        public string id;
        public string effectType;
        public string target;
        public int amount;
        public string unit;
        public HistoricalLocalizedText benefit;
        public HistoricalLocalizedText socialCost;
    }

    [Serializable]
    public sealed class HistoricalAssetProvenance
    {
        public string id;
        /// <summary>image / icon / audio / video / model。</summary>
        public string kind;
        /// <summary>public_collection / original / generated_reconstruction。</summary>
        public string origin;
        public string sourceRef;
        public string creator;
        public string license;
        public string licenseUrl;
        public string generatorId;
        public string promptHash;
        public string reviewedBy;
        public string reconstructionLabelJa;
        public string reviewStatus;
    }

    /// <summary>製品収録前に史実・参照・権利・生成表示を一括検査する。</summary>
    public static class HistoricalContentValidator
    {
        static readonly Regex StableId = new Regex(
            "^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant);
        static readonly Regex IsoDate = new Regex(
            @"^\d{4}-\d{2}-\d{2}$", RegexOptions.CultureInvariant);

        public static IReadOnlyList<string> ValidateForRelease(
            HistoricalContentDataset dataset)
        {
            var errors = new List<string>();
            if (dataset == null)
            {
                errors.Add("史実台帳がnull");
                return errors;
            }

            RequireStableId(errors, dataset.id, "dataset.id");
            if (dataset.schemaVersion != 1) errors.Add("台帳schemaVersionは1");
            if (dataset.datasetVersion < 1) errors.Add("台帳datasetVersionは1以上");
            RequireVerified(errors, dataset.reviewStatus, dataset.id);

            var sourceIds = ValidateSources(dataset.sources, errors);
            if (dataset.records == null || dataset.records.Length == 0)
            {
                errors.Add("史実台帳recordsが空");
                return errors;
            }

            var recordIds = new HashSet<string>();
            foreach (var record in dataset.records)
            {
                if (record == null)
                {
                    errors.Add("nullの史実レコード");
                    continue;
                }
                RequireStableId(errors, record.id, "record.id");
                if (!recordIds.Add(record.id ?? ""))
                    errors.Add("史実レコードID重複: " + record.id);
                if (string.IsNullOrWhiteSpace(record.category))
                    errors.Add("category欠落: " + record.id);
                RequireLocalized(errors, record.name, record.id + ".name");
                if (string.IsNullOrWhiteSpace(record.originalName))
                    errors.Add("原語表記欠落: " + record.id);
                if (string.IsNullOrWhiteSpace(record.transliteration))
                    errors.Add("転写欠落: " + record.id);
                if (string.IsNullOrWhiteSpace(record.regionId))
                    errors.Add("regionId欠落: " + record.id);
                if (record.earliestYear > record.latestYear)
                    errors.Add("年代範囲が逆転: " + record.id);
                ValidateConfidence(errors, record.confidence, record.id);
                RequireVerified(errors, record.reviewStatus, record.id);
                RequireLocalized(errors, record.summary, record.id + ".summary");
                ValidateSourceRefs(record.sourceRefs, sourceIds, record.id, errors);
                ValidateEffects(record.effects, record.id, errors);
                ValidateAssets(record.assets, sourceIds, record.id, errors);
            }
            return errors;
        }

        static HashSet<string> ValidateSources(HistoricalSourceDefinition[] sources,
            List<string> errors)
        {
            var ids = new HashSet<string>();
            if (sources == null || sources.Length == 0)
            {
                errors.Add("史実台帳sourcesが空");
                return ids;
            }
            foreach (var source in sources)
            {
                if (source == null)
                {
                    errors.Add("nullの出典");
                    continue;
                }
                RequireStableId(errors, source.id, "source.id");
                if (!ids.Add(source.id ?? ""))
                    errors.Add("出典ID重複: " + source.id);
                if (string.IsNullOrWhiteSpace(source.publisher) ||
                    string.IsNullOrWhiteSpace(source.title))
                    errors.Add("出典書誌情報欠落: " + source.id);
                if (!AbsoluteHttpUrl(source.url))
                    errors.Add("出典URL不正: " + source.id);
                if (source.sourceType != "world_heritage" &&
                    source.sourceType != "museum" &&
                    source.sourceType != "academic" &&
                    source.sourceType != "primary" &&
                    source.sourceType != "other")
                    errors.Add("出典sourceType不正: " + source.id);
                if (string.IsNullOrWhiteSpace(source.accessedDate) ||
                    !IsoDate.IsMatch(source.accessedDate))
                    errors.Add("出典accessedDate不正: " + source.id);
                if (source.usage != "reference_only" &&
                    source.usage != "reusable_asset")
                    errors.Add("出典usage不正: " + source.id);
                if (source.usage == "reusable_asset" &&
                    (string.IsNullOrWhiteSpace(source.license) ||
                     !AbsoluteHttpUrl(source.licenseUrl)))
                    errors.Add("再利用素材の権利情報欠落: " + source.id);
                RequireVerified(errors, source.reviewStatus, source.id);
            }
            return ids;
        }

        static void ValidateEffects(HistoricalGameplayEffect[] effects,
            string ownerId, List<string> errors)
        {
            if (effects == null) return;
            var ids = new HashSet<string>();
            foreach (var effect in effects)
            {
                if (effect == null)
                {
                    errors.Add("nullの固有効果: " + ownerId);
                    continue;
                }
                RequireStableId(errors, effect.id, ownerId + ".effect.id");
                if (!ids.Add(effect.id ?? ""))
                    errors.Add("固有効果ID重複: " + ownerId + " -> " + effect.id);
                if (string.IsNullOrWhiteSpace(effect.effectType) ||
                    string.IsNullOrWhiteSpace(effect.target) ||
                    string.IsNullOrWhiteSpace(effect.unit))
                    errors.Add("固有効果の計算情報欠落: " + effect.id);
                RequireLocalized(errors, effect.benefit, effect.id + ".benefit");
                RequireLocalized(errors, effect.socialCost, effect.id + ".socialCost");
            }
        }

        static void ValidateAssets(HistoricalAssetProvenance[] assets,
            HashSet<string> sourceIds, string ownerId, List<string> errors)
        {
            if (assets == null) return;
            var ids = new HashSet<string>();
            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    errors.Add("nullの素材: " + ownerId);
                    continue;
                }
                RequireStableId(errors, asset.id, ownerId + ".asset.id");
                if (!ids.Add(asset.id ?? ""))
                    errors.Add("素材ID重複: " + ownerId + " -> " + asset.id);
                if (asset.kind != "image" && asset.kind != "icon" &&
                    asset.kind != "audio" && asset.kind != "video" &&
                    asset.kind != "model")
                    errors.Add("素材kind不正: " + asset.id);
                if (asset.origin != "public_collection" &&
                    asset.origin != "original" &&
                    asset.origin != "generated_reconstruction")
                    errors.Add("素材origin不正: " + asset.id);
                RequireVerified(errors, asset.reviewStatus, asset.id);

                if (asset.origin == "public_collection")
                {
                    if (!sourceIds.Contains(asset.sourceRef ?? ""))
                        errors.Add("公開素材の出典参照不正: " + asset.id);
                    if (string.IsNullOrWhiteSpace(asset.creator) ||
                        string.IsNullOrWhiteSpace(asset.license) ||
                        !AbsoluteHttpUrl(asset.licenseUrl))
                        errors.Add("公開素材の作者・ライセンス欠落: " + asset.id);
                }
                else if (asset.origin == "generated_reconstruction")
                {
                    if (string.IsNullOrWhiteSpace(asset.generatorId) ||
                        string.IsNullOrWhiteSpace(asset.promptHash) ||
                        string.IsNullOrWhiteSpace(asset.reviewedBy) ||
                        string.IsNullOrWhiteSpace(asset.reconstructionLabelJa) ||
                        !asset.reconstructionLabelJa.Contains("復元"))
                        errors.Add("生成復元素材の来歴・表示欠落: " + asset.id);
                }
            }
        }

        static void ValidateSourceRefs(string[] refs, HashSet<string> sourceIds,
            string ownerId, List<string> errors)
        {
            if (refs == null || refs.Length == 0)
            {
                errors.Add("sourceRefs欠落: " + ownerId);
                return;
            }
            foreach (string sourceRef in refs)
                if (!sourceIds.Contains(sourceRef ?? ""))
                    errors.Add($"存在しない出典参照: {ownerId} -> {sourceRef}");
        }

        static bool AbsoluteHttpUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        static void RequireStableId(List<string> errors, string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id) || !StableId.IsMatch(id))
                errors.Add("安定IDが不正: " + label);
        }

        static void RequireLocalized(List<string> errors,
            HistoricalLocalizedText text, string label)
        {
            if (text == null || string.IsNullOrWhiteSpace(text.key) ||
                string.IsNullOrWhiteSpace(text.ja))
                errors.Add("ローカライズ情報欠落: " + label);
        }

        static void ValidateConfidence(List<string> errors,
            string confidence, string owner)
        {
            if (confidence != "confirmed" && confidence != "probable" &&
                confidence != "inferred")
                errors.Add("史料確度が不正: " + owner);
        }

        static void RequireVerified(List<string> errors, string status, string owner)
        {
            if (status != "verified")
                errors.Add("製品収録データはverified必須: " + owner);
        }
    }
}
