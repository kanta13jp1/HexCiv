using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    [Serializable]
    public sealed class UrukRegionalFactionState
    {
        public string factionId;
        public string nameJa;
        public string aiArchetype;
        public bool human;
        public int startCol;
        public int startRow;
        public int population;
        public int stability;
        public UrukLaborAllocation labor;
        public HistoricalGoodAmount[] stockpiles;
        public int lastFoodProduced;
        public int lastFoodConsumed;
        public int lastFoodShortage;
        public int lastPopulationChange;
        public int diplomaticTrust = 50;
        public string currentGoalJa;
        public string lastDecisionJa;
        public string knownReasonJa;
    }

    [Serializable]
    public sealed class UrukCanalSegmentState
    {
        public string id;
        public string ownerFactionId;
        public string managerFactionId;
        public string[] userFactionIds;
        public int fromCol;
        public int fromRow;
        public int toCol;
        public int toRow;
        public int condition;
        public int capacity;
        public int currentFlow;
        public int lastLeakage;
        public int silt;
        public bool completed;
        public bool sourceIntake;
        public bool planned;
        public bool createdByPlan;
        public int lastMaintainedTurn;
        public string confidence;
    }

    [Serializable]
    public sealed class UrukFarmPlotState
    {
        public string id;
        public string ownerFactionId;
        public int col;
        public int row;
        public string crop;
        public int condition;
        public int drainage;
        public int salinity;
        public int waterDemand = 4;
        public int waterReceived;
        public int lastYield;
        public bool irrigated;
        public string confidence;
    }

    [Serializable]
    public sealed class UrukConstructionProjectState
    {
        public string id;
        public string factionId;
        public string kind;
        public string targetId;
        public int priority;
        public int progress;
        public int requiredWork;
        public int clayCost;
        public int reedsCost;
        /// <summary>planned / active / paused / completed / cancelled。</summary>
        public string status;
        public int createdTurn;
        public int committedTurn;
        public string pauseReasonJa;
    }

    [Serializable]
    public sealed class UrukTradeOfferState
    {
        public string id;
        public string proposerFactionId;
        public string receiverFactionId;
        public string offeredGoodId;
        public int offeredAmount;
        public string requestedGoodId;
        public int requestedAmount;
        /// <summary>open / accepted_pending / departed / completed / rejected / expired。</summary>
        public string status;
        public int createdTurn;
        public int expiresTurn;
        public string contractKind;
        public string reasonJa;
        public string confidence;
    }

    [Serializable]
    public sealed class UrukTransportState
    {
        public string id;
        public string contractId;
        public string originFactionId;
        public string destinationFactionId;
        public string goodId;
        public int shippedAmount;
        public int remainingAmount;
        public int lostAmount;
        public int deliveredAmount;
        public int departureTurn;
        public int arrivalTurn;
        public string mode;
        /// <summary>pending / en_route / arrived / cancelled。</summary>
        public string status;
        public int riskPercent;
        public HistoricalMapPoint[] path;
    }

    [Serializable]
    public sealed class UrukMigrationGroupState
    {
        public string id;
        public string originFactionId;
        public string destinationFactionId;
        public int people;
        public int departedPeople;
        public int arrivedPeople;
        public int departureTurn;
        public int arrivalTurn;
        /// <summary>waiting / in_transit / settled / rejected。</summary>
        public string status;
        public string causeJa;
        public string confidence;
    }

    [Serializable]
    public sealed class UrukWaterDisputeState
    {
        public string id;
        public string claimantFactionId;
        public string respondentFactionId;
        public string segmentId;
        public string causeJa;
        public int createdTurn;
        /// <summary>open / negotiated / rejected。</summary>
        public string status;
        public string resultJa;
    }

    /// <summary>
    /// ウルク第4A段階の水利グラフ、農地、8勢力台帳、輸送、移住。
    /// 通常4Xの状態・RNGには触れず、配列順と安定IDだけで決定的に解決する。
    /// </summary>
    public static class UrukRegionalSystem
    {
        public const string PlanCanalAction = "regional_plan_canal";
        public const string CancelCanalPlanAction = "regional_cancel_canal";
        public const string CropBarleyAction = "regional_crop_barley";
        public const string CropEmmerAction = "regional_crop_emmer";
        public const string CropFallowAction = "regional_crop_fallow";
        public const string AcceptOfferAction = "regional_accept_offer";
        public const string SendGiftAction = "regional_send_gift";
        public const string OfferBarterAction = "regional_offer_barter";
        public const string NegotiateWaterAction = "regional_negotiate_water";
        public const string AcceptMigrationAction = "regional_accept_migration";
        public const string RejectMigrationAction = "regional_reject_migration";

        const string HumanFactionId = "uruk_community";

        public static void EnsureInitialized(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.regionalFactions ??= BuildFactions(definition, progress);
            progress.canalSegments ??= BuildCanals(definition);
            progress.farmPlots ??= BuildFarms(definition);
            progress.constructionProjects ??= Array.Empty<UrukConstructionProjectState>();
            progress.tradeOffers ??= Array.Empty<UrukTradeOfferState>();
            progress.transports ??= Array.Empty<UrukTransportState>();
            progress.migrationGroups ??= Array.Empty<UrukMigrationGroupState>();
            progress.waterDisputes ??= Array.Empty<UrukWaterDisputeState>();
            if (progress.nextRegionalId <= 0) progress.nextRegionalId = 1;
            SyncHumanFaction(progress);
        }

        public static void Validate(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress)
        {
            EnsureInitialized(definition, progress);
            if (progress.regionalFactions.Length != definition.factions.Length)
                throw new InvalidOperationException("地域台帳の勢力数が不正");
            var factionIds = new HashSet<string>();
            foreach (var faction in progress.regionalFactions)
            {
                if (faction == null || string.IsNullOrWhiteSpace(faction.factionId) ||
                    !factionIds.Add(faction.factionId) || faction.population < 0 ||
                    faction.stability < 0 || faction.stability > 100 ||
                    faction.labor == null || faction.labor.Total != 100 ||
                    faction.stockpiles == null)
                    throw new InvalidOperationException("地域勢力台帳が不正");
                foreach (var stock in faction.stockpiles)
                    if (stock == null || string.IsNullOrWhiteSpace(stock.id) ||
                        stock.amount < 0)
                        throw new InvalidOperationException("地域物資台帳が不正");
            }
            foreach (var segment in progress.canalSegments)
            {
                if (segment == null || string.IsNullOrWhiteSpace(segment.id) ||
                    !factionIds.Contains(segment.ownerFactionId) ||
                    segment.condition < 0 || segment.condition > 100 ||
                    segment.capacity <= 0 || segment.currentFlow < 0 ||
                    HexCoord.FromOffset(segment.fromCol, segment.fromRow).DistanceTo(
                        HexCoord.FromOffset(segment.toCol, segment.toRow)) != 1)
                    throw new InvalidOperationException("水路区間状態が不正");
            }
            foreach (var farm in progress.farmPlots)
            {
                if (farm == null || !factionIds.Contains(farm.ownerFactionId) ||
                    (farm.crop != "barley" && farm.crop != "emmer" &&
                     farm.crop != "fallow") ||
                    !Percent(farm.condition) || !Percent(farm.drainage) ||
                    !Percent(farm.salinity) || farm.waterReceived < 0 ||
                    farm.lastYield < 0)
                    throw new InvalidOperationException("農地区画状態が不正");
            }
            foreach (var transport in progress.transports)
            {
                if (transport == null || transport.shippedAmount < 0 ||
                    transport.remainingAmount < 0 || transport.lostAmount < 0 ||
                    transport.deliveredAmount < 0 ||
                    transport.remainingAmount + transport.lostAmount +
                        transport.deliveredAmount != transport.shippedAmount)
                    throw new InvalidOperationException("輸送物資の保存則違反");
            }
            if (progress.lastRegionalSourceWater !=
                progress.lastRegionalFarmWater + progress.lastRegionalLeakage +
                progress.lastRegionalUnusedWater)
                throw new InvalidOperationException("水量保存則違反");
            if (progress.reservedClay < 0 || progress.reservedReeds < 0)
                throw new InvalidOperationException("予約資源が負数");
        }

        public static bool TryApplyAction(HistoricalCampaignSession session,
            string actionId, out string resultJa)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var progress = session.Progress;
            EnsureInitialized(session.Definition, progress);
            int turn = session.State.TurnNumber;

            if (actionId != null && actionId.StartsWith("regional_overlay_",
                StringComparison.Ordinal))
            {
                progress.regionalOverlayMode = actionId.EndsWith("water",
                    StringComparison.Ordinal) ? 1 :
                    actionId.EndsWith("farm", StringComparison.Ordinal) ? 2 :
                    actionId.EndsWith("logistics", StringComparison.Ordinal) ? 3 : 0;
                progress.regionalRevision++;
                resultJa = "地図情報レイヤーを切り替えた。";
                return true;
            }
            if (actionId != null && actionId.StartsWith("regional_select_intake_",
                StringComparison.Ordinal))
            {
                string[] parts = actionId.Split('_');
                if (parts.Length != 5 || !int.TryParse(parts[3], out int col) ||
                    !int.TryParse(parts[4], out int row))
                {
                    resultJa = "取水点の指定が不正。";
                    return false;
                }
                progress.selectedIntakeCol = col;
                progress.selectedIntakeRow = row;
                resultJa = $"取水点（{col},{row}）を選択。対象農地を選んでください。";
                return true;
            }
            if (actionId != null && actionId.StartsWith("regional_select_farm_",
                StringComparison.Ordinal))
            {
                progress.selectedFarmId = actionId.Substring(
                    "regional_select_farm_".Length);
                resultJa = "対象農地を選択した。経路提案を確認できます。";
                return true;
            }

            switch (actionId)
            {
                case PlanCanalAction:
                    return PlanCanal(session, turn, out resultJa);
                case CancelCanalPlanAction:
                    return CancelPlans(progress, out resultJa);
                case CropBarleyAction:
                    return SetHumanCrop(progress, "barley", out resultJa);
                case CropEmmerAction:
                    return SetHumanCrop(progress, "emmer", out resultJa);
                case CropFallowAction:
                    return SetHumanCrop(progress, "fallow", out resultJa);
                case AcceptOfferAction:
                    return AcceptFirstOffer(progress, turn, out resultJa);
                case SendGiftAction:
                    return CreateGift(progress, turn, out resultJa);
                case OfferBarterAction:
                    return CreateHumanBarter(progress, turn, out resultJa);
                case NegotiateWaterAction:
                    return ResolveFirstDispute(progress, true, out resultJa);
                case AcceptMigrationAction:
                    return RespondToMigration(progress, turn, true, out resultJa);
                case RejectMigrationAction:
                    return RespondToMigration(progress, turn, false, out resultJa);
                default:
                    resultJa = "不明な地域運営行動。";
                    return false;
            }
        }

        public static void OnLegacyAction(HistoricalCampaignSession session,
            string actionId)
        {
            if (session?.Progress == null) return;
            EnsureInitialized(session.Definition, session.Progress);
            if (actionId != UrukCampaignSystem.MaintainCanalAction) return;
            foreach (var segment in session.Progress.canalSegments)
            {
                if (segment.ownerFactionId != HumanFactionId || !segment.completed)
                    continue;
                segment.condition = Math.Clamp(segment.condition + 35, 0, 100);
                segment.silt = Math.Max(0, segment.silt - 20);
                segment.lastMaintainedTurn = session.State.TurnNumber;
            }
            session.Progress.regionalRevision++;
        }

        public static void Advance(HistoricalCampaignSession session,
            int completedTurn, UrukFloodTrend flood)
        {
            var progress = session.Progress;
            EnsureInitialized(session.Definition, progress);
            SyncHumanFaction(progress);
            CommitPlans(progress, completedTurn);
            CommitAcceptedOffers(session.Definition, progress, completedTurn);
            AdvanceProjects(progress, completedTurn);
            ResolveWater(progress);
            ResolveFarms(progress);
            ResolveAiLedgers(progress, flood);
            AdvanceTransports(progress, completedTurn);
            AdvanceMigrations(progress, completedTurn);
            PlanAi(session.Definition, progress, completedTurn);
            DegradeCanals(progress, completedTurn, flood);
            SyncHumanFaction(progress);
            progress.regionalRevision++;
            Validate(session.Definition, progress);
        }

        public static int HumanIrrigatedFarmCount(UrukCampaignProgress progress)
        {
            int count = 0;
            if (progress?.farmPlots == null) return 0;
            foreach (var farm in progress.farmPlots)
                if (farm.ownerFactionId == HumanFactionId && farm.irrigated) count++;
            return count;
        }

        public static int HumanFarmCount(UrukCampaignProgress progress)
        {
            int count = 0;
            if (progress?.farmPlots == null) return 0;
            foreach (var farm in progress.farmPlots)
                if (farm.ownerFactionId == HumanFactionId) count++;
            return count;
        }

        public static UrukRegionalFactionState FindFaction(
            UrukCampaignProgress progress, string factionId)
        {
            if (progress?.regionalFactions == null) return null;
            foreach (var faction in progress.regionalFactions)
                if (faction.factionId == factionId) return faction;
            return null;
        }

        public static UrukTradeOfferState FirstOpenOffer(
            UrukCampaignProgress progress)
        {
            if (progress?.tradeOffers == null) return null;
            foreach (var offer in progress.tradeOffers)
                if (offer.receiverFactionId == HumanFactionId &&
                    offer.status == "open") return offer;
            return null;
        }

        public static UrukMigrationGroupState FirstWaitingMigration(
            UrukCampaignProgress progress)
        {
            if (progress?.migrationGroups == null) return null;
            foreach (var group in progress.migrationGroups)
                if (group.destinationFactionId == HumanFactionId &&
                    group.status == "waiting") return group;
            return null;
        }

        public static UrukWaterDisputeState FirstOpenDispute(
            UrukCampaignProgress progress)
        {
            if (progress?.waterDisputes == null) return null;
            foreach (var dispute in progress.waterDisputes)
                if (dispute.respondentFactionId == HumanFactionId &&
                    dispute.status == "open") return dispute;
            return null;
        }

        public static void ResolveWaterForTest(UrukCampaignProgress progress)
        {
            ResolveWater(progress);
        }

        static UrukRegionalFactionState[] BuildFactions(
            HistoricalCampaignDefinition definition, UrukCampaignProgress progress)
        {
            var result = new UrukRegionalFactionState[definition.factions.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var source = definition.factions[i];
                int population = source.human
                    ? progress.actualPopulation
                    : Math.Max(650, source.initialPopulation * 550 + i * 35);
                var faction = new UrukRegionalFactionState
                {
                    factionId = source.id,
                    nameJa = source.name.ja,
                    aiArchetype = source.aiArchetype,
                    human = source.human,
                    startCol = source.startCol,
                    startRow = source.startRow,
                    population = population,
                    stability = source.human ? progress.stability : 52 + i * 3,
                    labor = InitialLabor(source.aiArchetype),
                    stockpiles = source.human
                        ? CopyGoods(progress.stockpiles)
                        : InitialAiGoods(definition, i, source.aiArchetype),
                    currentGoalJa = GoalFor(source.aiArchetype),
                    lastDecisionJa = "地理と備蓄を確認中。",
                    knownReasonJa = "接触後に推定可能。",
                };
                result[i] = faction;
            }
            return result;
        }

        static UrukCanalSegmentState[] BuildCanals(
            HistoricalCampaignDefinition definition)
        {
            var result = new UrukCanalSegmentState[definition.canalSegments.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var source = definition.canalSegments[i];
                result[i] = new UrukCanalSegmentState
                {
                    id = source.id,
                    ownerFactionId = source.factionId,
                    managerFactionId = source.factionId,
                    userFactionIds = new[] { source.factionId },
                    fromCol = source.fromCol,
                    fromRow = source.fromRow,
                    toCol = source.toCol,
                    toRow = source.toRow,
                    condition = source.condition,
                    capacity = source.capacity,
                    completed = source.completed,
                    sourceIntake = source.sourceIntake,
                    confidence = source.confidence,
                };
            }
            return result;
        }

        static UrukFarmPlotState[] BuildFarms(
            HistoricalCampaignDefinition definition)
        {
            var result = new UrukFarmPlotState[definition.farmPlots.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var source = definition.farmPlots[i];
                result[i] = new UrukFarmPlotState
                {
                    id = source.id,
                    ownerFactionId = source.factionId,
                    col = source.col,
                    row = source.row,
                    crop = source.crop,
                    condition = source.condition,
                    drainage = source.drainage,
                    salinity = source.salinity,
                    confidence = source.confidence,
                };
            }
            return result;
        }

        static UrukLaborAllocation InitialLabor(string archetype)
        {
            var labor = new UrukLaborAllocation();
            switch (archetype)
            {
                case "wetland_temple":
                    labor.food = 50; labor.canal = 15; labor.construction = 10;
                    labor.crafts = 10; labor.trade = 10; labor.militia = 5;
                    break;
                case "canal_border":
                    labor.food = 45; labor.canal = 25; labor.construction = 10;
                    labor.crafts = 5; labor.trade = 5; labor.militia = 10;
                    break;
                case "ritual_center":
                    labor.food = 50; labor.canal = 10; labor.construction = 15;
                    labor.crafts = 10; labor.trade = 10; labor.militia = 5;
                    break;
                case "eastern_exchange":
                    labor.food = 45; labor.canal = 10; labor.construction = 10;
                    labor.crafts = 15; labor.trade = 15; labor.militia = 5;
                    break;
                case "reed_fish_transport":
                    labor.food = 60; labor.canal = 10; labor.construction = 5;
                    labor.crafts = 10; labor.trade = 10; labor.militia = 5;
                    break;
            }
            return labor;
        }

        static string GoalFor(string archetype)
        {
            return archetype switch
            {
                "wetland_temple" => "祭祀・漁撈・湿地利用",
                "marsh_trade" => "農業・河口交易",
                "canal_border" => "水利・農地拡張",
                "ritual_center" => "祭祀・仲裁",
                "northern_exchange" => "陸路・防衛",
                "eastern_exchange" => "交易・工芸",
                "reed_fish_transport" => "漁撈・牧畜・水上移動",
                _ => "都市化・交易",
            };
        }

        static HistoricalGoodAmount[] InitialAiGoods(
            HistoricalCampaignDefinition definition, int index, string archetype)
        {
            var goods = new HistoricalGoodAmount[definition.goods.Length];
            for (int i = 0; i < goods.Length; i++)
            {
                string id = definition.goods[i].id;
                int amount = id switch
                {
                    "barley" => 7 + index % 3,
                    "emmer_wheat" => 2,
                    "reeds" => archetype == "reed_fish_transport" ? 12 : 6,
                    "alluvial_clay" => 7,
                    "fish" => archetype == "wetland_temple" ||
                        archetype == "reed_fish_transport" ? 4 : 1,
                    "sheep_wool" => 3,
                    "timber" => archetype == "eastern_exchange" ? 4 : 1,
                    "building_stone" => archetype == "eastern_exchange" ? 3 : 0,
                    "copper" => archetype == "eastern_exchange" ? 2 : 0,
                    _ => 0,
                };
                goods[i] = new HistoricalGoodAmount { id = id, amount = amount };
            }
            return goods;
        }

        static bool PlanCanal(HistoricalCampaignSession session, int turn,
            out string resultJa)
        {
            var progress = session.Progress;
            if (HasPlannedProjects(progress, HumanFactionId))
            {
                resultJa = "未確定の水路計画がある。取り消すかターンを終了してください。";
                return false;
            }
            var farm = SelectedHumanFarm(progress);
            if (farm == null)
            {
                resultJa = "対象となるウルク農地がない。";
                return false;
            }
            int fromCol = progress.selectedIntakeCol >= 0
                ? progress.selectedIntakeCol : 16;
            int fromRow = progress.selectedIntakeRow >= 0
                ? progress.selectedIntakeRow : 10;
            var path = FindPath(fromCol, fromRow, farm.col, farm.row);
            if (path.Length < 2)
            {
                resultJa = "取水点と農地が同じ位置か、経路を提案できない。";
                return false;
            }

            int clay = 0;
            int reeds = 0;
            int priority = 0;
            for (int i = 0; i < path.Length - 1; i++)
            {
                var segment = FindSegment(progress, path[i], path[i + 1]);
                string kind;
                if (segment != null && segment.completed)
                {
                    if (segment.condition >= 60) continue;
                    kind = "canal_repair";
                }
                else
                {
                    kind = "canal_build";
                    if (segment == null)
                    {
                        segment = CreatePlannedSegment(progress, path[i], path[i + 1],
                            i == 0, turn);
                    }
                    segment.planned = true;
                }
                var project = new UrukConstructionProjectState
                {
                    id = NextId(progress, "project"),
                    factionId = HumanFactionId,
                    kind = kind,
                    targetId = segment.id,
                    priority = priority++,
                    requiredWork = kind == "canal_repair" ? 20 : 30,
                    clayCost = kind == "canal_repair" ? 0 : 1,
                    reedsCost = 1,
                    status = "planned",
                    createdTurn = turn,
                };
                clay += project.clayCost;
                reeds += project.reedsCost;
                Append(ref progress.constructionProjects, project);
            }
            if (priority == 0)
            {
                resultJa = "提案経路はすでに通水可能。別の農地を選んでください。";
                return false;
            }
            if (HumanGood(progress, "alluvial_clay") < clay ||
                HumanGood(progress, "reeds") < reeds)
            {
                CancelPlans(progress, out _);
                resultJa = $"資源不足。計画には粘土{clay}・葦{reeds}が必要。";
                return false;
            }
            progress.reservedClay = clay;
            progress.reservedReeds = reeds;
            int work = Math.Max(5, progress.labor.construction);
            progress.estimatedCanalTurns = Math.Max(1,
                (priority * 30 + work - 1) / work);
            progress.regionalRevision++;
            resultJa = $"水路{priority}区間を提案。予約: 粘土{clay}・葦{reeds}、" +
                $"推定{progress.estimatedCanalTurns}期間。ターン終了まで取消可能。";
            return true;
        }

        static bool CancelPlans(UrukCampaignProgress progress, out string resultJa)
        {
            int cancelled = 0;
            var projects = new List<UrukConstructionProjectState>();
            foreach (var project in progress.constructionProjects)
            {
                if (project.factionId == HumanFactionId && project.status == "planned")
                {
                    cancelled++;
                    continue;
                }
                projects.Add(project);
            }
            progress.constructionProjects = projects.ToArray();
            var segments = new List<UrukCanalSegmentState>();
            foreach (var segment in progress.canalSegments)
            {
                if (segment.createdByPlan && segment.planned && !segment.completed)
                    continue;
                segments.Add(segment);
            }
            progress.canalSegments = segments.ToArray();
            progress.reservedClay = 0;
            progress.reservedReeds = 0;
            progress.estimatedCanalTurns = 0;
            progress.regionalRevision++;
            resultJa = cancelled > 0
                ? $"水路計画{cancelled}件を取り消した。予約資源は全量戻った。"
                : "取り消せる水路計画がない。";
            return cancelled > 0;
        }

        static bool SetHumanCrop(UrukCampaignProgress progress, string crop,
            out string resultJa)
        {
            var farm = SelectedHumanFarm(progress);
            if (farm == null)
            {
                resultJa = "作付け対象農地がない。";
                return false;
            }
            farm.crop = crop;
            progress.regionalRevision++;
            resultJa = crop == "barley" ? "次期作付けを大麦に設定した。" :
                crop == "emmer" ? "次期作付けをエンマー小麦に設定した。" :
                "次期を休耕にし、排水と塩害回復を優先した。";
            return true;
        }

        static void CommitPlans(UrukCampaignProgress progress, int turn)
        {
            foreach (var project in progress.constructionProjects)
            {
                if (project.status != "planned") continue;
                if (FactionGood(progress, project.factionId,
                        "alluvial_clay") < project.clayCost ||
                    FactionGood(progress, project.factionId,
                        "reeds") < project.reedsCost)
                {
                    project.status = "cancelled";
                    project.pauseReasonJa = "確定時に建設資源が不足。";
                    continue;
                }
                ConsumeFactionGood(progress, project.factionId,
                    "alluvial_clay", project.clayCost);
                ConsumeFactionGood(progress, project.factionId,
                    "reeds", project.reedsCost);
                project.status = "active";
                project.committedTurn = turn;
            }
            progress.reservedClay = 0;
            progress.reservedReeds = 0;
            progress.estimatedCanalTurns = 0;
        }

        static void AdvanceProjects(UrukCampaignProgress progress, int turn)
        {
            foreach (var faction in progress.regionalFactions)
            {
                UrukConstructionProjectState selected = null;
                foreach (var project in progress.constructionProjects)
                {
                    if (project.factionId != faction.factionId ||
                        (project.status != "active" && project.status != "paused"))
                        continue;
                    if (!faction.human && faction.lastFoodShortage > 0)
                    {
                        project.status = "paused";
                        project.pauseReasonJa = "食料危機のため工事を中断。";
                        continue;
                    }
                    if (selected == null || project.priority < selected.priority)
                        selected = project;
                }
                if (selected == null) continue;
                selected.status = "active";
                selected.pauseReasonJa = "";
                selected.progress += Math.Max(5, faction.labor.construction);
                if (selected.progress < selected.requiredWork) continue;
                selected.progress = selected.requiredWork;
                selected.status = "completed";
                var segment = FindSegment(progress, selected.targetId);
                if (segment != null)
                {
                    segment.completed = true;
                    segment.planned = false;
                    segment.condition = selected.kind == "canal_repair"
                        ? Math.Clamp(segment.condition + 45, 0, 100) : 65;
                    segment.silt = Math.Max(0, segment.silt - 30);
                    segment.lastMaintainedTurn = turn;
                }
            }
        }

        static void ResolveWater(UrukCampaignProgress progress)
        {
            var water = new Dictionary<string, int>();
            var processed = new HashSet<string>();
            int sourceTotal = 0;
            int leakageTotal = 0;
            foreach (var segment in progress.canalSegments)
            {
                segment.currentFlow = 0;
                segment.lastLeakage = 0;
                if (!segment.completed || !segment.sourceIntake) continue;
                string key = NodeKey(segment.fromCol, segment.fromRow);
                if (!water.ContainsKey(key))
                {
                    water[key] = 12;
                    sourceTotal += 12;
                }
            }

            for (int pass = 0; pass < progress.canalSegments.Length; pass++)
            {
                bool advanced = false;
                foreach (var segment in progress.canalSegments)
                {
                    if (!segment.completed || processed.Contains(segment.id) ||
                        segment.condition < 30)
                        continue;
                    string from = NodeKey(segment.fromCol, segment.fromRow);
                    if (!water.TryGetValue(from, out int available) || available <= 0)
                        continue;
                    int conditionFactor = segment.condition >= 60 ? 100 : 60;
                    int sent = Math.Min(available,
                        segment.capacity * conditionFactor / 100);
                    int leakRate = Math.Clamp((100 - segment.condition) / 4, 0, 20);
                    int leakage = sent * leakRate / 100;
                    int delivered = sent - leakage;
                    water[from] = available - sent;
                    string to = NodeKey(segment.toCol, segment.toRow);
                    water[to] = Get(water, to) + delivered;
                    segment.currentFlow = delivered;
                    segment.lastLeakage = leakage;
                    leakageTotal += leakage;
                    processed.Add(segment.id);
                    advanced = true;
                }
                if (!advanced) break;
            }

            int farmWater = 0;
            foreach (var farm in progress.farmPlots)
            {
                string node = NodeKey(farm.col, farm.row);
                int available = Get(water, node);
                int received = Math.Min(farm.waterDemand, available);
                farm.waterReceived = received;
                farm.irrigated = received >= 3;
                farmWater += received;
                water[node] = available - received;
            }
            int unused = 0;
            foreach (var pair in water) unused += pair.Value;
            progress.lastRegionalSourceWater = sourceTotal;
            progress.lastRegionalFarmWater = farmWater;
            progress.lastRegionalLeakage = leakageTotal;
            progress.lastRegionalUnusedWater = unused;
        }

        static void ResolveFarms(UrukCampaignProgress progress)
        {
            progress.lastRegionalHumanYield = 0;
            foreach (var farm in progress.farmPlots)
            {
                if (farm.crop == "fallow")
                {
                    farm.lastYield = 0;
                    farm.salinity = Math.Max(0, farm.salinity - 10);
                    farm.drainage = Math.Min(100, farm.drainage + 2);
                    continue;
                }
                int yield = farm.crop == "emmer" ? 10 : 8;
                yield = yield * farm.condition / 70;
                int saltPenalty = farm.crop == "barley"
                    ? farm.salinity / 3 : farm.salinity / 2;
                yield = yield * Math.Clamp(100 - saltPenalty, 20, 100) / 100;
                yield = yield * (farm.irrigated ? 110 :
                    farm.waterReceived > 0 ? 75 : 35) / 100;
                yield = yield * Math.Clamp(80 + farm.drainage / 5, 80, 100) / 100;
                farm.lastYield = Math.Max(0, yield);
                if (farm.irrigated && farm.drainage < 60)
                    farm.salinity = Math.Min(100, farm.salinity +
                        1 + (60 - farm.drainage) / 15);
                else if (!farm.irrigated)
                    farm.salinity = Math.Max(0, farm.salinity - 2);
                if (farm.ownerFactionId == HumanFactionId)
                    progress.lastRegionalHumanYield += farm.lastYield;
            }
        }

        static void ResolveAiLedgers(UrukCampaignProgress progress,
            UrukFloodTrend flood)
        {
            foreach (var faction in progress.regionalFactions)
            {
                if (faction.human) continue;
                int produced = 0;
                foreach (var farm in progress.farmPlots)
                    if (farm.ownerFactionId == faction.factionId)
                        produced += farm.lastYield;
                if (faction.aiArchetype == "wetland_temple" ||
                    faction.aiArchetype == "reed_fish_transport")
                    produced += 2;
                if (flood == UrukFloodTrend.Severe) produced = produced * 70 / 100;
                AddFactionGood(progress, faction.factionId, "barley", produced);
                int consumed = Math.Max(1, (faction.population + 449) / 450);
                int shortage = ConsumeFactionFood(progress, faction.factionId,
                    consumed);
                int grain = FactionGood(progress, faction.factionId, "barley");
                int spoil = grain * 20 / 100;
                ConsumeFactionGood(progress, faction.factionId, "barley", spoil);
                int change = shortage > 0
                    ? -Math.Min(faction.population / 20, 25 + shortage * 20)
                    : FactionFood(progress, faction.factionId) >= consumed * 3
                        ? Math.Max(10, faction.population / 20)
                        : Math.Max(4, faction.population / 50);
                faction.population = Math.Max(100, faction.population + change);
                faction.lastFoodProduced = produced;
                faction.lastFoodConsumed = consumed - shortage;
                faction.lastFoodShortage = shortage;
                faction.lastPopulationChange = change;
                faction.stability = Math.Clamp(faction.stability +
                    (shortage > 0 ? -8 : 1), 0, 100);
            }
        }

        static void PlanAi(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress, int turn)
        {
            foreach (var faction in progress.regionalFactions)
            {
                if (faction.human) continue;
                if (faction.lastFoodShortage > 0)
                {
                    SetLabor(faction.labor, 60, 15, 0, 10, 10, 5);
                    faction.currentGoalJa = "食料危機への対応";
                    faction.lastDecisionJa =
                        "食料労働を増やし、緊急性の低い工事を中断した。";
                    faction.knownReasonJa = "備蓄不足が推定される。";
                }
                else
                {
                    faction.currentGoalJa = GoalFor(faction.aiArchetype);
                    faction.lastDecisionJa =
                        $"{faction.currentGoalJa}を地理・資源条件に合わせて継続。";
                    faction.knownReasonJa = "接触済み情報から推定。";
                }
                TryScheduleAiRepair(progress, faction, turn);
            }

            if (turn >= 6 && !HasOffer(progress, "eridu_relief_barter"))
            {
                Append(ref progress.tradeOffers, new UrukTradeOfferState
                {
                    id = "eridu_relief_barter",
                    proposerFactionId = "eridu_community",
                    receiverFactionId = HumanFactionId,
                    offeredGoodId = "barley",
                    offeredAmount = 2,
                    requestedGoodId = "reeds",
                    requestedAmount = 1,
                    status = "open",
                    createdTurn = turn,
                    expiresTurn = turn + 4,
                    contractKind = "barter",
                    reasonJa = "エリドゥは湿地資材を確保し、ウルクへ穀物を回したい。",
                    confidence = "inferred",
                });
            }
            if (turn >= 8 && progress.waterDisputes.Length == 0)
            {
                Append(ref progress.waterDisputes, new UrukWaterDisputeState
                {
                    id = "lagash_upstream_diversion",
                    claimantFactionId = "lagash_region",
                    respondentFactionId = HumanFactionId,
                    segmentId = "uruk_intake_segment",
                    causeJa = "上流取水の増加が下流利用へ与える影響を懸念。",
                    createdTurn = turn,
                    status = "open",
                });
            }
            if (turn >= 9 && progress.migrationGroups.Length == 0)
            {
                Append(ref progress.migrationGroups, new UrukMigrationGroupState
                {
                    id = "marsh_flood_migrants",
                    originFactionId = "marsh_communities",
                    destinationFactionId = HumanFactionId,
                    people = 60,
                    status = "waiting",
                    causeJa = "湿地の洪水と食料不安による一時移住。",
                    confidence = "inferred",
                });
            }
        }

        static void TryScheduleAiRepair(UrukCampaignProgress progress,
            UrukRegionalFactionState faction, int turn)
        {
            if (faction.lastFoodShortage > 0 ||
                HasActiveProject(progress, faction.factionId))
                return;
            foreach (var segment in progress.canalSegments)
            {
                if (segment.ownerFactionId != faction.factionId ||
                    !segment.completed || segment.condition >= 45)
                    continue;
                if (FactionGood(progress, faction.factionId, "reeds") < 1)
                    return;
                Append(ref progress.constructionProjects,
                    new UrukConstructionProjectState
                    {
                        id = NextId(progress, "ai_project"),
                        factionId = faction.factionId,
                        kind = "canal_repair",
                        targetId = segment.id,
                        priority = 0,
                        requiredWork = 20,
                        reedsCost = 1,
                        status = "planned",
                        createdTurn = turn,
                    });
                faction.lastDecisionJa =
                    "実在備蓄と労働を使う水路補修を計画した。";
                return;
            }
        }

        static void DegradeCanals(UrukCampaignProgress progress, int turn,
            UrukFloodTrend flood)
        {
            foreach (var segment in progress.canalSegments)
            {
                if (!segment.completed) continue;
                int decay = segment.lastMaintainedTurn == turn ? 1 : 3;
                if (flood == UrukFloodTrend.Severe) decay += 5;
                segment.condition = Math.Max(0, segment.condition - decay);
                segment.silt = Math.Min(100, segment.silt + decay);
            }
        }

        static bool AcceptFirstOffer(UrukCampaignProgress progress, int turn,
            out string resultJa)
        {
            var offer = FirstOpenOffer(progress);
            if (offer == null)
            {
                resultJa = "受諾できる交易提案がない。";
                return false;
            }
            if (AvailableFactionGood(progress, offer.receiverFactionId,
                offer.requestedGoodId) < offer.requestedAmount)
            {
                resultJa = "相手が求める物資を用意できない。";
                return false;
            }
            offer.status = "accepted_pending";
            resultJa = "物々交換を受諾した。ターン終了時に双方の物資を積み出す。";
            return true;
        }

        static bool CreateGift(UrukCampaignProgress progress, int turn,
            out string resultJa)
        {
            if (HumanGood(progress, "barley") < 1)
            {
                resultJa = "贈与できる大麦がない。";
                return false;
            }
            var offer = new UrukTradeOfferState
            {
                id = NextId(progress, "gift"),
                proposerFactionId = HumanFactionId,
                receiverFactionId = "eridu_community",
                offeredGoodId = "barley",
                offeredAmount = 1,
                requestedGoodId = "",
                requestedAmount = 0,
                status = "accepted_pending",
                createdTurn = turn,
                expiresTurn = turn + 1,
                contractKind = "gift",
                reasonJa = "食料贈与により関係と威信を高める。",
                confidence = "confirmed",
            };
            Append(ref progress.tradeOffers, offer);
            resultJa = "エリドゥへの大麦贈与を計画した。ターン終了まで取消可能。";
            return true;
        }

        static bool CreateHumanBarter(UrukCampaignProgress progress, int turn,
            out string resultJa)
        {
            if (AvailableFactionGood(progress, HumanFactionId, "reeds") < 1)
            {
                resultJa = "交換に出す葦がない。";
                return false;
            }
            Append(ref progress.tradeOffers, new UrukTradeOfferState
            {
                id = NextId(progress, "barter"),
                proposerFactionId = HumanFactionId,
                receiverFactionId = "susiana_exchange",
                offeredGoodId = "reeds",
                offeredAmount = 1,
                requestedGoodId = "alluvial_clay",
                requestedAmount = 1,
                status = "accepted_pending",
                createdTurn = turn,
                expiresTurn = turn + 2,
                contractKind = "barter",
                reasonJa = "不足度・輸送費・関係から双方が受諾可能と推定。",
                confidence = "inferred",
            });
            resultJa = "スシアナ交易圏との物々交換を計画した。";
            return true;
        }

        static void CommitAcceptedOffers(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress, int turn)
        {
            foreach (var offer in progress.tradeOffers)
            {
                if (offer.status != "accepted_pending") continue;
                if (FactionGood(progress, offer.proposerFactionId,
                        offer.offeredGoodId) < offer.offeredAmount ||
                    FactionGood(progress, offer.receiverFactionId,
                        offer.requestedGoodId) < offer.requestedAmount)
                {
                    offer.status = "rejected";
                    continue;
                }
                ConsumeFactionGood(progress, offer.proposerFactionId,
                    offer.offeredGoodId, offer.offeredAmount);
                ConsumeFactionGood(progress, offer.receiverFactionId,
                    offer.requestedGoodId, offer.requestedAmount);
                CreateTransport(definition, progress, offer.id,
                    offer.proposerFactionId, offer.receiverFactionId,
                    offer.offeredGoodId, offer.offeredAmount, turn);
                if (offer.requestedAmount > 0)
                    CreateTransport(definition, progress, offer.id,
                        offer.receiverFactionId, offer.proposerFactionId,
                        offer.requestedGoodId, offer.requestedAmount, turn);
                offer.status = "departed";
            }
        }

        static void CreateTransport(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress, string contractId, string origin,
            string destination, string goodId, int amount, int turn)
        {
            if (amount <= 0) return;
            var from = FindFaction(progress, origin);
            var to = FindFaction(progress, destination);
            string id = NextId(progress, "transport");
            int risk = 8 + StableHash(definition.seed, id, turn) % 13;
            int lost = StableHash(definition.seed + 17, id, turn) % 100 < risk
                ? Math.Min(1, amount) : 0;
            Append(ref progress.transports, new UrukTransportState
            {
                id = id,
                contractId = contractId,
                originFactionId = origin,
                destinationFactionId = destination,
                goodId = goodId,
                shippedAmount = amount,
                remainingAmount = amount - lost,
                lostAmount = lost,
                departureTurn = turn,
                arrivalTurn = turn + 2,
                mode = IsWaterLinked(origin, destination)
                    ? "reed_boat" : "overland",
                status = "en_route",
                riskPercent = risk,
                path = new[]
                {
                    new HistoricalMapPoint { col = from.startCol, row = from.startRow },
                    new HistoricalMapPoint { col = to.startCol, row = to.startRow },
                },
            });
        }

        static void AdvanceTransports(UrukCampaignProgress progress, int turn)
        {
            foreach (var transport in progress.transports)
            {
                if (transport.status != "en_route" || turn < transport.arrivalTurn)
                    continue;
                int delivered = transport.remainingAmount;
                AddFactionGood(progress, transport.destinationFactionId,
                    transport.goodId, delivered);
                transport.deliveredAmount = delivered;
                transport.remainingAmount = 0;
                transport.status = "arrived";
                foreach (var offer in progress.tradeOffers)
                    if (offer.id == transport.contractId &&
                        ContractArrived(progress, offer.id))
                        offer.status = "completed";
            }
        }

        static bool ResolveFirstDispute(UrukCampaignProgress progress, bool negotiate,
            out string resultJa)
        {
            var dispute = FirstOpenDispute(progress);
            if (dispute == null)
            {
                resultJa = "交渉可能な水利問題がない。";
                return false;
            }
            dispute.status = negotiate ? "negotiated" : "rejected";
            dispute.resultJa = negotiate
                ? "取水量の事前通知と共同維持を合意した。"
                : "要求を拒否し、水利関係が悪化した。";
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            if (claimant != null)
                claimant.diplomaticTrust = Math.Clamp(claimant.diplomaticTrust +
                    (negotiate ? 8 : -12), 0, 100);
            resultJa = dispute.resultJa;
            return true;
        }

        static bool RespondToMigration(UrukCampaignProgress progress, int turn,
            bool accept, out string resultJa)
        {
            var group = FirstWaitingMigration(progress);
            if (group == null)
            {
                resultJa = "判断待ちの移住集団がいない。";
                return false;
            }
            if (!accept)
            {
                group.status = "rejected";
                resultJa = "移住集団の定住を拒否した。";
                return true;
            }
            var origin = FindFaction(progress, group.originFactionId);
            int departed = Math.Min(group.people,
                Math.Max(0, origin.population - 100));
            origin.population -= departed;
            group.departedPeople = departed;
            group.departureTurn = turn;
            group.arrivalTurn = turn + 1;
            group.status = "in_transit";
            resultJa = $"{departed}人の一時移住を受け入れた。次期間に到着予定。";
            return true;
        }

        static void AdvanceMigrations(UrukCampaignProgress progress, int turn)
        {
            foreach (var group in progress.migrationGroups)
            {
                if (group.status != "in_transit" || turn < group.arrivalTurn)
                    continue;
                if (group.destinationFactionId == HumanFactionId)
                    UrukSubsistenceSystem.ApplyExternalMigration(progress,
                        group.departedPeople, turn,
                        $"{group.originFactionId}からの移住者を受け入れた。",
                        group.confidence);
                else
                {
                    var destination = FindFaction(progress,
                        group.destinationFactionId);
                    destination.population += group.departedPeople;
                }
                group.arrivedPeople = group.departedPeople;
                group.status = "settled";
            }
        }

        static bool HasOffer(UrukCampaignProgress progress, string id)
        {
            foreach (var offer in progress.tradeOffers)
                if (offer.id == id) return true;
            return false;
        }

        static bool ContractArrived(UrukCampaignProgress progress, string contractId)
        {
            bool found = false;
            foreach (var transport in progress.transports)
            {
                if (transport.contractId != contractId) continue;
                found = true;
                if (transport.status != "arrived") return false;
            }
            return found;
        }

        static bool HasPlannedProjects(UrukCampaignProgress progress, string factionId)
        {
            foreach (var project in progress.constructionProjects)
                if (project.factionId == factionId && project.status == "planned")
                    return true;
            return false;
        }

        static bool HasActiveProject(UrukCampaignProgress progress, string factionId)
        {
            foreach (var project in progress.constructionProjects)
                if (project.factionId == factionId &&
                    (project.status == "planned" || project.status == "active" ||
                     project.status == "paused"))
                    return true;
            return false;
        }

        static UrukFarmPlotState SelectedHumanFarm(UrukCampaignProgress progress)
        {
            if (!string.IsNullOrWhiteSpace(progress.selectedFarmId))
                foreach (var farm in progress.farmPlots)
                    if (farm.ownerFactionId == HumanFactionId &&
                        farm.id == progress.selectedFarmId)
                        return farm;
            foreach (var farm in progress.farmPlots)
                if (farm.ownerFactionId == HumanFactionId) return farm;
            return null;
        }

        static HistoricalMapPoint[] FindPath(int fromCol, int fromRow,
            int toCol, int toRow)
        {
            var points = new List<HistoricalMapPoint>();
            var current = HexCoord.FromOffset(fromCol, fromRow);
            var target = HexCoord.FromOffset(toCol, toRow);
            current.ToOffset(out int col, out int row);
            points.Add(new HistoricalMapPoint { col = col, row = row });
            int guard = 0;
            while (current != target && guard++ < 64)
            {
                HexCoord best = current;
                int bestDistance = current.DistanceTo(target);
                for (int direction = 0; direction < 6; direction++)
                {
                    var candidate = current.Neighbor(direction);
                    int distance = candidate.DistanceTo(target);
                    if (distance < bestDistance)
                    {
                        best = candidate;
                        bestDistance = distance;
                    }
                }
                if (best == current) break;
                current = best;
                current.ToOffset(out col, out row);
                points.Add(new HistoricalMapPoint { col = col, row = row });
            }
            return points.ToArray();
        }

        static UrukCanalSegmentState FindSegment(UrukCampaignProgress progress,
            HistoricalMapPoint a, HistoricalMapPoint b)
        {
            foreach (var segment in progress.canalSegments)
                if (SameEdge(segment, a.col, a.row, b.col, b.row))
                    return segment;
            return null;
        }

        static UrukCanalSegmentState FindSegment(UrukCampaignProgress progress,
            string id)
        {
            foreach (var segment in progress.canalSegments)
                if (segment.id == id) return segment;
            return null;
        }

        static bool SameEdge(UrukCanalSegmentState segment, int aCol, int aRow,
            int bCol, int bRow)
        {
            return (segment.fromCol == aCol && segment.fromRow == aRow &&
                    segment.toCol == bCol && segment.toRow == bRow) ||
                (segment.fromCol == bCol && segment.fromRow == bRow &&
                 segment.toCol == aCol && segment.toRow == aRow);
        }

        static UrukCanalSegmentState CreatePlannedSegment(
            UrukCampaignProgress progress, HistoricalMapPoint from,
            HistoricalMapPoint to, bool sourceIntake, int turn)
        {
            var segment = new UrukCanalSegmentState
            {
                id = NextId(progress, "canal"),
                ownerFactionId = HumanFactionId,
                managerFactionId = HumanFactionId,
                userFactionIds = new[] { HumanFactionId },
                fromCol = from.col,
                fromRow = from.row,
                toCol = to.col,
                toRow = to.row,
                condition = 0,
                capacity = 8,
                sourceIntake = sourceIntake,
                planned = true,
                createdByPlan = true,
                confidence = "inferred",
            };
            Append(ref progress.canalSegments, segment);
            return segment;
        }

        static void SyncHumanFaction(UrukCampaignProgress progress)
        {
            var human = FindFaction(progress, HumanFactionId);
            if (human == null) return;
            human.population = progress.actualPopulation;
            human.stability = progress.stability;
            human.labor = CopyLabor(progress.labor);
            human.stockpiles = CopyGoods(progress.stockpiles);
            human.lastFoodProduced = progress.lastFoodProduced;
            human.lastFoodConsumed = progress.lastFoodConsumed;
            human.lastFoodShortage = progress.lastFoodShortage;
            human.lastPopulationChange = progress.lastPopulationChange;
            human.currentGoalJa = progress.isCityState
                ? "都市国家の水利・交易運営" : "食料安全と都市国家成立";
            human.lastDecisionJa = "プレイヤーの計画に従う。";
            human.knownReasonJa = "自勢力の記録。";
        }

        static UrukLaborAllocation CopyLabor(UrukLaborAllocation source)
        {
            return new UrukLaborAllocation
            {
                food = source.food,
                canal = source.canal,
                construction = source.construction,
                crafts = source.crafts,
                trade = source.trade,
                militia = source.militia,
            };
        }

        static HistoricalGoodAmount[] CopyGoods(HistoricalGoodAmount[] source)
        {
            var copy = new HistoricalGoodAmount[source.Length];
            for (int i = 0; i < source.Length; i++)
                copy[i] = new HistoricalGoodAmount
                {
                    id = source[i].id,
                    amount = source[i].amount,
                };
            return copy;
        }

        static int HumanGood(UrukCampaignProgress progress, string id)
        {
            return Good(progress.stockpiles, id);
        }

        static int FactionGood(UrukCampaignProgress progress, string factionId,
            string id)
        {
            return factionId == HumanFactionId
                ? HumanGood(progress, id)
                : Good(FindFaction(progress, factionId)?.stockpiles, id);
        }

        static int AvailableFactionGood(UrukCampaignProgress progress,
            string factionId, string id)
        {
            int available = FactionGood(progress, factionId, id);
            if (factionId != HumanFactionId) return available;
            if (id == "alluvial_clay") available -= progress.reservedClay;
            if (id == "reeds") available -= progress.reservedReeds;
            return Math.Max(0, available);
        }

        static int FactionFood(UrukCampaignProgress progress, string factionId)
        {
            return FactionGood(progress, factionId, "barley") +
                FactionGood(progress, factionId, "emmer_wheat") +
                FactionGood(progress, factionId, "fish");
        }

        static int Good(HistoricalGoodAmount[] goods, string id)
        {
            if (goods == null || string.IsNullOrWhiteSpace(id)) return 0;
            foreach (var good in goods)
                if (good.id == id) return Math.Max(0, good.amount);
            return 0;
        }

        static bool ConsumeFactionGood(UrukCampaignProgress progress,
            string factionId, string id, int amount)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(id)) return true;
            var goods = factionId == HumanFactionId
                ? progress.stockpiles
                : FindFaction(progress, factionId)?.stockpiles;
            if (Good(goods, id) < amount) return false;
            foreach (var good in goods)
                if (good.id == id)
                {
                    good.amount -= amount;
                    return true;
                }
            return false;
        }

        static int ConsumeFactionFood(UrukCampaignProgress progress,
            string factionId, int amount)
        {
            int remaining = amount;
            remaining -= ConsumeUpTo(progress, factionId, "fish", remaining);
            remaining -= ConsumeUpTo(progress, factionId, "barley", remaining);
            remaining -= ConsumeUpTo(progress, factionId, "emmer_wheat", remaining);
            return remaining;
        }

        static int ConsumeUpTo(UrukCampaignProgress progress, string factionId,
            string id, int amount)
        {
            int available = FactionGood(progress, factionId, id);
            int used = Math.Min(available, Math.Max(0, amount));
            ConsumeFactionGood(progress, factionId, id, used);
            return used;
        }

        static void AddFactionGood(UrukCampaignProgress progress,
            string factionId, string id, int amount)
        {
            if (amount <= 0) return;
            var goods = factionId == HumanFactionId
                ? progress.stockpiles
                : FindFaction(progress, factionId)?.stockpiles;
            if (goods == null) return;
            foreach (var good in goods)
                if (good.id == id)
                {
                    good.amount += amount;
                    return;
                }
        }

        static string NextId(UrukCampaignProgress progress, string prefix)
        {
            return prefix + "_" + progress.nextRegionalId++;
        }

        static string NodeKey(int col, int row) => col + "," + row;

        static int Get(Dictionary<string, int> values, string key)
        {
            return values.TryGetValue(key, out int value) ? value : 0;
        }

        static int StableHash(int seed, string id, int turn)
        {
            unchecked
            {
                int hash = seed * 397 ^ turn;
                for (int i = 0; i < id.Length; i++) hash = hash * 31 + id[i];
                return hash == int.MinValue ? 0 : Math.Abs(hash);
            }
        }

        static bool IsWaterLinked(string origin, string destination)
        {
            return origin == "marsh_communities" ||
                destination == "marsh_communities" ||
                origin == "eridu_community" || destination == "eridu_community" ||
                origin == "ur_community" || destination == "ur_community";
        }

        static void SetLabor(UrukLaborAllocation labor, int food, int canal,
            int construction, int crafts, int trade, int militia)
        {
            labor.food = food;
            labor.canal = canal;
            labor.construction = construction;
            labor.crafts = crafts;
            labor.trade = trade;
            labor.militia = militia;
        }

        static bool Percent(int value) => value >= 0 && value <= 100;

        static void Append<T>(ref T[] array, T value)
        {
            array ??= Array.Empty<T>();
            var next = new T[array.Length + 1];
            Array.Copy(array, next, array.Length);
            next[array.Length] = value;
            array = next;
        }
    }
}
