using System;
using HexCiv.Core;
using UnityEngine;

namespace HexCiv.Campaigns
{
    /// <summary>Resources/Campaigns 配下の検証済みJSONを読み込む入口。</summary>
    public static class HistoricalCampaignRepository
    {
        public const string Uruk4000Id = "uruk_4000";
        public static readonly string[] BuiltInIds = { Uruk4000Id };

        public static HistoricalCampaignDefinition LoadBuiltIn(string campaignId)
        {
            if (string.IsNullOrWhiteSpace(campaignId))
                throw new ArgumentException("campaignIdが空", nameof(campaignId));
            var asset = Resources.Load<TextAsset>("Campaigns/" + campaignId);
            if (asset == null)
                throw new InvalidOperationException("キャンペーンJSONが見つからない: " + campaignId);
            var definition = JsonUtility.FromJson<HistoricalCampaignDefinition>(asset.text);
            HistoricalCampaignValidator.ThrowIfInvalid(definition);
            if (definition.id != campaignId)
                throw new InvalidOperationException(
                    $"リソース名とキャンペーンIDが不一致: {campaignId} != {definition.id}");
            return definition;
        }
    }
}
