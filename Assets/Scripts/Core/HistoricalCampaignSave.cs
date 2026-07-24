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
            public int campaignDatasetVersion;
            public int completedTurns;
            public string stateJson;
            public string progressJson;
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
                campaignDatasetVersion = session.Definition.datasetVersion,
                completedTurns = session.CompletedTurns,
                stateJson = SaveLoad.Serialize(session.State),
                progressJson = JsonUtility.ToJson(session.Progress),
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
            // 第1段階の旧セーブはdatasetVersionフィールドがなく0になるため、
            // schemaVersionが一致する場合だけ現行データ1へ安全に補完する。
            int savedDatasetVersion = envelope.campaignDatasetVersion == 0
                ? 1
                : envelope.campaignDatasetVersion;
            if (definition.datasetVersion != savedDatasetVersion)
                throw new InvalidOperationException("史実データセットのバージョンがセーブと一致しない");
            var state = SaveLoad.Deserialize(envelope.stateJson);
            if (state == null) throw new InvalidOperationException("内側のゲーム状態を復元できない");
            if (state.Config == null || state.Config.NumPlayers != definition.factions.Length ||
                state.Config.MapWidth != definition.mapWidth || state.Config.MapHeight != definition.mapHeight)
                throw new InvalidOperationException("セーブ状態とキャンペーン定義の規模が一致しない");
            HistoricalCampaignFactory.ApplyDefinitionMetadata(definition, state);
            if (Math.Max(0, state.TurnNumber - 1) != envelope.completedTurns)
                throw new InvalidOperationException("セーブの完了ターン数が一致しない");
            var progress = string.IsNullOrWhiteSpace(envelope.progressJson)
                ? UrukCampaignSystem.CreateInitialProgress(definition)
                : JsonUtility.FromJson<UrukCampaignProgress>(envelope.progressJson);
            UrukCampaignSystem.ValidateProgress(definition, progress);
            return new HistoricalCampaignSession(definition, state, progress);
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

        public static bool TryReadMeta(string path, out int turnNumber,
            out string factionNameJa, out string savedAtIso)
        {
            turnNumber = 0;
            factionNameJa = null;
            savedAtIso = null;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                    return false;
                string json = System.IO.File.ReadAllText(path);
                if (!IsHistoricalCampaignSave(json)) return false;
                var envelope = JsonUtility.FromJson<Envelope>(json);
                if (envelope == null || string.IsNullOrWhiteSpace(envelope.stateJson))
                    return false;
                var inner = JsonUtility.FromJson<SaveData>(envelope.stateJson);
                if (inner == null) return false;
                turnNumber = inner.turnNumber;
                factionNameJa = "ウルク共同体（史実）";
                savedAtIso = inner.savedAtIso;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetInnerStateJson(string json, out string stateJson)
        {
            stateJson = null;
            try
            {
                if (!IsHistoricalCampaignSave(json)) return false;
                var envelope = JsonUtility.FromJson<Envelope>(json);
                if (envelope == null || string.IsNullOrWhiteSpace(envelope.stateJson))
                    return false;
                stateJson = envelope.stateJson;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
