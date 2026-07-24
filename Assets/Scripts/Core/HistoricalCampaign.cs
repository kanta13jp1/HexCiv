using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HexCiv.Core
{
    /// <summary>
    /// 史実キャンペーンのデータ契約。JSON側は日本語表示文とローカライズキーを併記し、
    /// 将来の多言語追加時も安定IDを変更しない。
    /// </summary>
    [Serializable]
    public sealed class HistoricalLocalizedText
    {
        public string key;
        public string ja;
    }

    [Serializable]
    public sealed class HistoricalSourceDefinition
    {
        public string id;
        public string publisher;
        public string title;
        public string url;
        /// <summary>world_heritage / museum / academic / primary / other。</summary>
        public string sourceType;
        /// <summary>参照確認日。ISO 8601のYYYY-MM-DD。</summary>
        public string accessedDate;
        /// <summary>reference_only / reusable_asset。</summary>
        public string usage;
        /// <summary>素材自体を収録する場合に必須。参照のみなら空でよい。</summary>
        public string license;
        public string licenseUrl;
        /// <summary>draft / review / verified / rejected。</summary>
        public string reviewStatus;
    }

    [Serializable]
    public sealed class HistoricalCalendarSegment
    {
        public int firstTurn;
        public int lastTurn;
        public int yearsPerTurn;
    }

    [Serializable]
    public sealed class HistoricalMapPoint
    {
        public int col;
        public int row;
    }

    [Serializable]
    public sealed class HistoricalRiverDefinition
    {
        public string id;
        public HistoricalLocalizedText name;
        /// <summary>confirmed / probable / inferred。</summary>
        public string confidence;
        public string reviewStatus;
        public HistoricalMapPoint[] points;
        public string[] sourceRefs;
    }

    [Serializable]
    public sealed class HistoricalFactionDefinition
    {
        public string id;
        public string civilizationId;
        public HistoricalLocalizedText name;
        public HistoricalLocalizedText capitalName;
        public HistoricalLocalizedText leaderTitle;
        public string confidence;
        public string reviewStatus;
        public bool human;
        public int startCol;
        public int startRow;
        public int initialPopulation = 1;
        public string colorHex;
        public string aiArchetype;
        public HistoricalLocalizedText historicalNote;
        public string[] sourceRefs;
    }

    [Serializable]
    public sealed class HistoricalGoodDefinition
    {
        public string id;
        public HistoricalLocalizedText name;
        public string category;
        public string confidence;
        public string reviewStatus;
        public HistoricalLocalizedText historicalNote;
        public string[] sourceRefs;
    }

    [Serializable]
    public sealed class HistoricalPopulationRoles
    {
        public int farmers;
        public int pastoralists;
        public int fishers;
        public int artisans;
        public int priests;
        public int warriors;
        public int laborers;

        public int Total => farmers + pastoralists + fishers + artisans +
            priests + warriors + laborers;
    }

    [Serializable]
    public sealed class HistoricalPopulationStatuses
    {
        public int free;
        public int dependent;
        public int enslaved;

        public int Total => free + dependent + enslaved;
    }

    [Serializable]
    public sealed class HistoricalGoodAmount
    {
        public string id;
        public int amount;
    }

    [Serializable]
    public sealed class HistoricalImprovementDefinition
    {
        public string id;
        public string kind;
        public int col;
        public int row;
        public int condition;
        public string confidence;
        public string reviewStatus;
        public string[] sourceRefs;
    }

    [Serializable]
    public sealed class HistoricalStartingScenario
    {
        public int actualPopulation;
        public HistoricalPopulationRoles roles;
        public HistoricalPopulationStatuses statuses;
        public HistoricalGoodAmount[] stockpiles;
        public HistoricalImprovementDefinition[] improvements;
        public int stability;
        public bool populationAutomation;
        public bool productionAutomation;
        public bool tradeAutomation;
    }

    [Serializable]
    public sealed class HistoricalCampaignDefinition
    {
        public int schemaVersion;
        public int datasetVersion;
        /// <summary>製品ビルドへ読み込めるのはverifiedのみ。</summary>
        public string reviewStatus;
        public string id;
        public HistoricalLocalizedText title;
        public HistoricalLocalizedText summary;
        public int startYear;
        public int endYear;
        public int maxTurns;
        public int seed;
        public int mapWidth;
        public int mapHeight;
        public string mapConfidence;
        public HistoricalCalendarSegment[] calendar;
        /// <summary>
        /// 固定マップ。~=外洋、=沿岸/湿地水面、.=沖積平野、g=草地、
        /// d=乾燥地、h=丘陵、m=山岳。
        /// </summary>
        public string[] terrainRows;
        public HistoricalRiverDefinition[] rivers;
        public HistoricalFactionDefinition[] factions;
        public HistoricalGoodDefinition[] goods;
        public HistoricalStartingScenario startingScenario;
        public HistoricalSourceDefinition[] sources;
    }

    /// <summary>キャンペーンJSONをゲーム状態へ渡す前の決定的な整合検査。</summary>
    public static class HistoricalCampaignValidator
    {
        static readonly Regex StableId = new Regex(
            "^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant);
        static readonly Regex IsoDate = new Regex(
            @"^\d{4}-\d{2}-\d{2}$", RegexOptions.CultureInvariant);
        const string KnownTerrainChars = "~=.gdhm";

        public static IReadOnlyList<string> Validate(HistoricalCampaignDefinition definition)
        {
            var errors = new List<string>();
            if (definition == null)
            {
                errors.Add("キャンペーン定義がnull");
                return errors;
            }

            RequireStableId(errors, definition.id, "campaign.id");
            RequireLocalized(errors, definition.title, "campaign.title");
            RequireLocalized(errors, definition.summary, "campaign.summary");
            if (definition.schemaVersion != 1) errors.Add("schemaVersionは1でなければならない");
            if (definition.datasetVersion < 1) errors.Add("datasetVersionは1以上");
            ValidateReviewStatus(errors, definition.reviewStatus, definition.id, true);
            ValidateConfidence(errors, definition.mapConfidence, "campaign.mapConfidence");
            if (definition.mapWidth < 8 || definition.mapHeight < 8)
                errors.Add("固定マップは8x8以上でなければならない");
            if (definition.maxTurns <= 0) errors.Add("maxTurnsは1以上");
            if (definition.startYear >= definition.endYear)
                errors.Add("startYearはendYearより前でなければならない");
            ValidateMap(definition, errors);
            ValidateCalendar(definition, errors);

            var sourceIds = new HashSet<string>();
            if (definition.sources == null || definition.sources.Length == 0)
                errors.Add("出典が1件もない");
            else
            {
                foreach (var source in definition.sources)
                {
                    if (source == null) { errors.Add("nullの出典"); continue; }
                    RequireStableId(errors, source.id, "source.id");
                    if (!sourceIds.Add(source.id ?? "")) errors.Add("出典ID重複: " + source.id);
                    if (string.IsNullOrWhiteSpace(source.publisher)) errors.Add("出典publisher欠落: " + source.id);
                    if (string.IsNullOrWhiteSpace(source.title)) errors.Add("出典title欠落: " + source.id);
                    if (!Uri.TryCreate(source.url, UriKind.Absolute, out var uri) ||
                        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
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
                    if (source.usage == "reusable_asset")
                    {
                        if (string.IsNullOrWhiteSpace(source.license))
                            errors.Add("再利用素材のlicense欠落: " + source.id);
                        if (!Uri.TryCreate(source.licenseUrl, UriKind.Absolute, out var licenseUri) ||
                            (licenseUri.Scheme != Uri.UriSchemeHttp &&
                             licenseUri.Scheme != Uri.UriSchemeHttps))
                            errors.Add("再利用素材のlicenseUrl不正: " + source.id);
                    }
                    ValidateReviewStatus(errors, source.reviewStatus, source.id, true);
                }
            }

            ValidateFactions(definition, sourceIds, errors);
            ValidateGoods(definition, sourceIds, errors);
            ValidateRivers(definition, sourceIds, errors);
            ValidateStartingScenario(definition, sourceIds, errors);
            return errors;
        }

        public static void ThrowIfInvalid(HistoricalCampaignDefinition definition)
        {
            var errors = Validate(definition);
            if (errors.Count > 0)
                throw new InvalidOperationException(
                    "史実キャンペーンJSONが不正:\n- " + string.Join("\n- ", errors));
        }

        static void ValidateMap(HistoricalCampaignDefinition definition, List<string> errors)
        {
            if (definition.terrainRows == null ||
                definition.terrainRows.Length != definition.mapHeight)
            {
                errors.Add("terrainRowsの行数がmapHeightと一致しない");
                return;
            }
            for (int row = 0; row < definition.terrainRows.Length; row++)
            {
                string line = definition.terrainRows[row] ?? "";
                if (line.Length != definition.mapWidth)
                {
                    errors.Add($"terrainRows[{row}]の幅が{definition.mapWidth}ではない");
                    continue;
                }
                for (int col = 0; col < line.Length; col++)
                    if (KnownTerrainChars.IndexOf(line[col]) < 0)
                        errors.Add($"未知の地形記号 '{line[col]}' ({col},{row})");
            }
        }

        static void ValidateCalendar(HistoricalCampaignDefinition definition, List<string> errors)
        {
            if (definition.calendar == null || definition.calendar.Length == 0)
            {
                errors.Add("calendarが空");
                return;
            }
            int nextTurn = 1;
            long year = definition.startYear;
            foreach (var segment in definition.calendar)
            {
                if (segment == null) { errors.Add("nullのcalendar segment"); continue; }
                if (segment.firstTurn != nextTurn)
                    errors.Add($"calendarがターン{nextTurn}から連続していない");
                if (segment.lastTurn < segment.firstTurn || segment.yearsPerTurn <= 0)
                    errors.Add("calendar segmentの範囲または年数が不正");
                int turns = Math.Max(0, segment.lastTurn - segment.firstTurn + 1);
                year += (long)turns * segment.yearsPerTurn;
                nextTurn = segment.lastTurn + 1;
            }
            if (nextTurn != definition.maxTurns + 1)
                errors.Add("calendarがmaxTurnsまでを過不足なく覆っていない");
            if (year != definition.endYear)
                errors.Add($"calendar終端年がendYearと不一致: {year}");
        }

        static void ValidateFactions(HistoricalCampaignDefinition definition,
            HashSet<string> sourceIds, List<string> errors)
        {
            if (definition.factions == null || definition.factions.Length != 8)
            {
                errors.Add("ウルク縦切り版の勢力数は8");
                return;
            }
            var ids = new HashSet<string>();
            var starts = new HashSet<string>();
            int humans = 0;
            foreach (var faction in definition.factions)
            {
                if (faction == null) { errors.Add("nullの勢力"); continue; }
                RequireStableId(errors, faction.id, "faction.id");
                if (!ids.Add(faction.id ?? "")) errors.Add("勢力ID重複: " + faction.id);
                RequireLocalized(errors, faction.name, faction.id + ".name");
                RequireLocalized(errors, faction.capitalName, faction.id + ".capitalName");
                RequireLocalized(errors, faction.leaderTitle, faction.id + ".leaderTitle");
                RequireLocalized(errors, faction.historicalNote, faction.id + ".historicalNote");
                ValidateConfidence(errors, faction.confidence, faction.id);
                ValidateReviewStatus(errors, faction.reviewStatus, faction.id, true);
                if (faction.human) humans++;
                if (faction.initialPopulation < 1) errors.Add("初期人口が1未満: " + faction.id);
                if (!IsInBounds(definition, faction.startCol, faction.startRow))
                    errors.Add("開始位置が範囲外: " + faction.id);
                else
                {
                    char terrain = definition.terrainRows[faction.startRow][faction.startCol];
                    if (terrain == '~' || terrain == '=' || terrain == 'm')
                        errors.Add("開始位置が都市建設不能地形: " + faction.id);
                }
                string startKey = faction.startCol + "," + faction.startRow;
                if (!starts.Add(startKey)) errors.Add("開始位置重複: " + startKey);
                ValidateSourceRefs(faction.sourceRefs, sourceIds, faction.id, errors);
            }
            if (humans != 1) errors.Add("human勢力は正確に1件必要");
        }

        static void ValidateGoods(HistoricalCampaignDefinition definition,
            HashSet<string> sourceIds, List<string> errors)
        {
            if (definition.goods == null || definition.goods.Length < 8)
            {
                errors.Add("初期実在物資は8件以上必要");
                return;
            }
            var ids = new HashSet<string>();
            foreach (var good in definition.goods)
            {
                if (good == null) { errors.Add("nullの物資"); continue; }
                RequireStableId(errors, good.id, "good.id");
                if (!ids.Add(good.id ?? "")) errors.Add("物資ID重複: " + good.id);
                RequireLocalized(errors, good.name, good.id + ".name");
                RequireLocalized(errors, good.historicalNote, good.id + ".historicalNote");
                ValidateConfidence(errors, good.confidence, good.id);
                ValidateReviewStatus(errors, good.reviewStatus, good.id, true);
                if (string.IsNullOrWhiteSpace(good.category)) errors.Add("物資category欠落: " + good.id);
                ValidateSourceRefs(good.sourceRefs, sourceIds, good.id, errors);
            }
        }

        static void ValidateRivers(HistoricalCampaignDefinition definition,
            HashSet<string> sourceIds, List<string> errors)
        {
            if (definition.rivers == null || definition.rivers.Length < 2)
            {
                errors.Add("固定マップにはチグリス・ユーフラテスの2水系が必要");
                return;
            }
            var ids = new HashSet<string>();
            foreach (var river in definition.rivers)
            {
                if (river == null) { errors.Add("nullの河川"); continue; }
                RequireStableId(errors, river.id, "river.id");
                if (!ids.Add(river.id ?? "")) errors.Add("河川ID重複: " + river.id);
                RequireLocalized(errors, river.name, river.id + ".name");
                ValidateConfidence(errors, river.confidence, river.id);
                ValidateReviewStatus(errors, river.reviewStatus, river.id, true);
                if (river.points == null || river.points.Length < 2)
                {
                    errors.Add("河川経路が短すぎる: " + river.id);
                    continue;
                }
                for (int i = 0; i < river.points.Length; i++)
                {
                    var point = river.points[i];
                    if (point == null || !IsInBounds(definition, point.col, point.row))
                    {
                        errors.Add("河川座標が範囲外: " + river.id);
                        continue;
                    }
                    if (i > 0)
                    {
                        var previous = river.points[i - 1];
                        if (previous != null &&
                            HexCoord.FromOffset(previous.col, previous.row)
                                .DistanceTo(HexCoord.FromOffset(point.col, point.row)) != 1)
                            errors.Add($"河川経路が非隣接: {river.id}[{i - 1}->{i}]");
                    }
                }
                ValidateSourceRefs(river.sourceRefs, sourceIds, river.id, errors);
            }
        }

        static void ValidateStartingScenario(HistoricalCampaignDefinition definition,
            HashSet<string> sourceIds, List<string> errors)
        {
            var scenario = definition.startingScenario;
            if (scenario == null)
            {
                errors.Add("startingScenarioがない");
                return;
            }
            if (scenario.actualPopulation != 1500)
                errors.Add("ウルク開始実人口は1500人");
            if (scenario.roles == null || scenario.roles.Total != scenario.actualPopulation)
                errors.Add("開始時の役割別人口合計が実人口と一致しない");
            if (scenario.statuses == null ||
                scenario.statuses.Total != scenario.actualPopulation)
                errors.Add("開始時の地位別人口合計が実人口と一致しない");
            if (scenario.statuses != null && scenario.statuses.enslaved != 0)
                errors.Add("紀元前4000年開始時の奴隷人口は0でなければならない");
            if (!scenario.populationAutomation || scenario.productionAutomation ||
                scenario.tradeAutomation)
                errors.Add("初期自動化は人口ON・生産OFF・交易OFF");
            if (scenario.stability < 0 || scenario.stability > 100)
                errors.Add("開始安定度は0..100");

            var goodIds = new HashSet<string>();
            if (definition.goods != null)
                foreach (var good in definition.goods)
                    if (good != null && !string.IsNullOrWhiteSpace(good.id))
                        goodIds.Add(good.id);
            var stockIds = new HashSet<string>();
            if (scenario.stockpiles == null || scenario.stockpiles.Length != goodIds.Count)
                errors.Add("開始備蓄は実在物資台帳の全項目を1件ずつ持つ");
            else
            {
                foreach (var stock in scenario.stockpiles)
                {
                    if (stock == null) { errors.Add("nullの開始備蓄"); continue; }
                    if (!goodIds.Contains(stock.id ?? ""))
                        errors.Add("未登録の開始備蓄: " + stock.id);
                    if (!stockIds.Add(stock.id ?? ""))
                        errors.Add("開始備蓄ID重複: " + stock.id);
                    if (stock.amount < 0) errors.Add("開始備蓄が負数: " + stock.id);
                }
            }

            int farms = 0;
            int canals = 0;
            var improvementIds = new HashSet<string>();
            if (scenario.improvements == null || scenario.improvements.Length == 0)
            {
                errors.Add("開始施設がない");
                return;
            }
            foreach (var improvement in scenario.improvements)
            {
                if (improvement == null) { errors.Add("nullの開始施設"); continue; }
                RequireStableId(errors, improvement.id, "improvement.id");
                if (!improvementIds.Add(improvement.id ?? ""))
                    errors.Add("開始施設ID重複: " + improvement.id);
                if (improvement.kind == "farm") farms++;
                else if (improvement.kind == "canal") canals++;
                else errors.Add("開始施設kind不正: " + improvement.kind);
                if (!IsInBounds(definition, improvement.col, improvement.row))
                    errors.Add("開始施設がマップ範囲外: " + improvement.id);
                if (improvement.condition < 0 || improvement.condition > 100)
                    errors.Add("開始施設conditionは0..100: " + improvement.id);
                ValidateConfidence(errors, improvement.confidence, improvement.id);
                ValidateReviewStatus(errors, improvement.reviewStatus, improvement.id, true);
                ValidateSourceRefs(improvement.sourceRefs, sourceIds, improvement.id, errors);
            }
            if (farms != 2) errors.Add("開始農地は正確に2区画");
            if (canals < 1) errors.Add("未整備の開始運河が必要");
        }

        static bool IsInBounds(HistoricalCampaignDefinition definition, int col, int row)
        {
            return col >= 0 && col < definition.mapWidth &&
                row >= 0 && row < definition.mapHeight;
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

        static void ValidateConfidence(List<string> errors, string confidence, string owner)
        {
            if (confidence != "confirmed" && confidence != "probable" &&
                confidence != "inferred")
                errors.Add("史料確度が不正: " + owner);
        }

        static void ValidateReviewStatus(List<string> errors, string status,
            string owner, bool requireVerified)
        {
            bool known = status == "draft" || status == "review" ||
                status == "verified" || status == "rejected";
            if (!known)
                errors.Add("確認状態が不正: " + owner);
            else if (requireVerified && status != "verified")
                errors.Add("製品収録データはverified必須: " + owner);
        }

        static void RequireStableId(List<string> errors, string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id) || !StableId.IsMatch(id))
                errors.Add("安定IDが不正: " + label);
        }

        static void RequireLocalized(List<string> errors, HistoricalLocalizedText text, string label)
        {
            if (text == null || string.IsNullOrWhiteSpace(text.key) ||
                string.IsNullOrWhiteSpace(text.ja))
                errors.Add("ローカライズ情報欠落: " + label);
        }
    }

    /// <summary>可変年代をターン区間へ変換する純関数。</summary>
    public static class HistoricalCampaignCalendar
    {
        public static int YearAtTurnStart(HistoricalCampaignDefinition definition, int turnNumber)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            int clamped = Math.Clamp(turnNumber, 1, definition.maxTurns + 1);
            int completed = clamped - 1;
            int year = definition.startYear;
            foreach (var segment in definition.calendar)
            {
                int segmentTurns = segment.lastTurn - segment.firstTurn + 1;
                int used = Math.Min(completed, segmentTurns);
                if (used > 0) year += used * segment.yearsPerTurn;
                completed -= used;
                if (completed <= 0) break;
            }
            return year;
        }

        public static int YearAtTurnEnd(HistoricalCampaignDefinition definition, int turnNumber)
        {
            return YearAtTurnStart(definition, Math.Clamp(turnNumber + 1, 1, definition.maxTurns + 1));
        }

        public static string FormatYearJa(int astronomicalYear)
        {
            if (astronomicalYear < 0) return $"紀元前{Math.Abs(astronomicalYear)}年";
            if (astronomicalYear == 0) return "紀元1年";
            return $"西暦{astronomicalYear}年";
        }

        public static string TurnIntervalJa(HistoricalCampaignDefinition definition, int turnNumber)
        {
            return $"{FormatYearJa(YearAtTurnStart(definition, turnNumber))}～" +
                FormatYearJa(YearAtTurnEnd(definition, turnNumber));
        }
    }
}
