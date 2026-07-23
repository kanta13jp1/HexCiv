using System;
using UnityEngine;

namespace HexCiv.Core
{
    /// <summary>
    /// 既存ランダムゲームと史実キャンペーンのセーブを混同しない外側エンベロープ。
    /// 内側のGameStateは既存SaveLoadへ委譲し、セーブ互換処理を重複させない。
    /// </summary>
    public static class HistoricalCampaignSave
    {
        public const int FormatVersion = 1;
        public const string ModeId = "historical_campaign";

        [Serializable]
        sealed class Envelope
        {
            public int formatVersion;
            public string mode;
            public string campaignId;
            public int campaignSchemaVersion;
            public int completedTurns;
            public string stateJson;
        }

        public static string Serialize(HistoricalCampaignSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var envelope = new Envelope
            {
                formatVersion = FormatVersion,
                mode = ModeId,
                campaignId = session.Definition.id,
                campaignSchemaVersion = session.Definition.schemaVersion,
                completedTurns = session.CompletedTurns,
                stateJson = SaveLoad.Serialize(session.State),
            };
            return JsonUtility.ToJson(envelope);
        }

        public static HistoricalCampaignSession Deserialize(string json,
            Func<string, HistoricalCampaignDefinition> resolveCampaign)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("セーブJSONが空", nameof(json));
            if (resolveCampaign == null) throw new ArgumentNullException(nameof(resolveCampaign));
            var envelope = JsonUtility.FromJson<Envelope>(json);
            if (envelope == null || envelope.formatVersion != FormatVersion ||
                envelope.mode != ModeId || string.IsNullOrWhiteSpace(envelope.campaignId))
                throw new InvalidOperationException("史実キャンペーン用セーブではない");
            var definition = resolveCampaign(envelope.campaignId);
            if (definition == null)
                throw new InvalidOperationException("キャンペーン定義が見つからない: " + envelope.campaignId);
            HistoricalCampaignValidator.ThrowIfInvalid(definition);
            if (definition.schemaVersion != envelope.campaignSchemaVersion)
                throw new InvalidOperationException("キャンペーン定義のバージョンがセーブと一致しない");
            var state = SaveLoad.Deserialize(envelope.stateJson);
            if (state == null) throw new InvalidOperationException("内側のゲーム状態を復元できない");
            if (state.Config == null || state.Config.NumPlayers != definition.factions.Length ||
                state.Config.MapWidth != definition.mapWidth || state.Config.MapHeight != definition.mapHeight)
                throw new InvalidOperationException("セーブ状態とキャンペーン定義の規模が一致しない");
            HistoricalCampaignFactory.ApplyDefinitionMetadata(definition, state);
            if (Math.Max(0, state.TurnNumber - 1) != envelope.completedTurns)
                throw new InvalidOperationException("セーブの完了ターン数が一致しない");
            return new HistoricalCampaignSession(definition, state);
        }

        public static bool IsHistoricalCampaignSave(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                var envelope = JsonUtility.FromJson<Envelope>(json);
                return envelope != null && envelope.formatVersion == FormatVersion &&
                    envelope.mode == ModeId && !string.IsNullOrWhiteSpace(envelope.campaignId);
            }
            catch
            {
                return false;
            }
        }
    }
}
