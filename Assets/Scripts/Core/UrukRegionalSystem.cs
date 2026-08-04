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
        /// <summary>open / accepted_pending / departed / active / completed / defaulted / rejected / expired。</summary>
        public string status;
        public int createdTurn;
        public int expiresTurn;
        public string contractKind;
        public string targetId;
        public int durationTurns;
        public int intervalTurns;
        public int installmentCount;
        public string reasonJa;
        public string confidence;
    }

    [Serializable]
    public sealed class UrukTransportState
    {
        public string id;
        public string contractId;
        public string obligationId;
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
    public sealed class UrukObligationState
    {
        public string id;
        public string contractId;
        /// <summary>loan_repayment / labor_service / access_right / tribute。</summary>
        public string kind;
        public string debtorFactionId;
        public string creditorFactionId;
        public string goodId;
        public int amountPerInstallment;
        public int remainingInstallments;
        public int dueTurn;
        public int intervalTurns;
        public string targetId;
        public int fulfilledAmount;
        public int missedPayments;
        /// <summary>active / in_transit / completed / defaulted / expired。</summary>
        public string status;
        public string lastResultJa;
        public string confidence;
    }

    [Serializable]
    public sealed class UrukDiplomaticRecordState
    {
        public string id;
        /// <summary>contract / water_dispute。</summary>
        public string category;
        public string subjectId;
        public string counterpartyFactionId;
        public int turn;
        /// <summary>agreed / completed / defaulted / expired / negotiated / rejected。</summary>
        public string outcome;
        public int reputationDelta;
        public int reputationAfter;
        public string summaryJa;
        public string confidence;
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
        /// <summary>被害または不足が観測された農地。</summary>
        public string claimantFarmId;
        /// <summary>請求側の取水路。</summary>
        public string claimantSegmentId;
        /// <summary>問題の原因とみなされた応答側取水路。旧segmentId。</summary>
        public string segmentId;
        public string causeJa;
        public int createdTurn;
        public int claimantWaterDeficit;
        public int claimantCanalCondition;
        public int respondentFlowAtClaim;
        /// <summary>share_water / grain_compensation / joint_maintenance / rejected。</summary>
        public string resolutionKind;
        public int agreementStartTurn;
        public int agreementUntilTurn;
        public int waterSharePerTurn;
        public int waterShareExpectedTotal;
        public int waterSharedTotal;
        public int barleyTransferred;
        public int laborCommitted;
        public bool agreementSettled;
        public int trustAfter;
        public string confidence;
        /// <summary>open / shared / compensated / jointly_maintained / rejected / completed / defaulted。</summary>
        public string status;
        public string resultJa;
    }

    /// <summary>
    /// ウルク地域段階の水利グラフ、農地、8勢力台帳、輸送、移住、外交。
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
        public const string RequestLoanAction = "regional_request_loan";
        public const string OfferLaborAction = "regional_offer_labor";
        public const string AcquireAccessAction = "regional_acquire_access";
        public const string OfferTributeAction = "regional_offer_tribute";
        public const string NegotiateWaterAction = "regional_negotiate_water";
        public const string ShareWaterAction = "regional_share_water";
        public const string CompensateWaterAction = "regional_compensate_water";
        public const string RejectWaterAction = "regional_reject_water";
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
            progress.obligations ??= Array.Empty<UrukObligationState>();
            progress.diplomaticRecords ??=
                Array.Empty<UrukDiplomaticRecordState>();
            progress.migrationGroups ??= Array.Empty<UrukMigrationGroupState>();
            progress.waterDisputes ??= Array.Empty<UrukWaterDisputeState>();
            if (progress.nextRegionalId <= 0) progress.nextRegionalId = 1;
            SyncHumanFaction(progress);
        }

        public static void MigrateWaterDisputesV6(UrukCampaignProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.waterDisputes ??= Array.Empty<UrukWaterDisputeState>();
            foreach (var dispute in progress.waterDisputes)
            {
                if (dispute == null) continue;
                if (string.IsNullOrWhiteSpace(dispute.claimantFarmId))
                    dispute.claimantFarmId = "lagash_hinterland_farm";
                if (string.IsNullOrWhiteSpace(dispute.claimantSegmentId))
                    dispute.claimantSegmentId = "lagash_tigris_branch";
                if (string.IsNullOrWhiteSpace(dispute.confidence))
                    dispute.confidence = "inferred";
                if (dispute.trustAfter == 0)
                    dispute.trustAfter = FindFaction(progress,
                        dispute.claimantFactionId)?.diplomaticTrust ?? 50;
                if (string.IsNullOrWhiteSpace(dispute.resolutionKind) &&
                    dispute.status == "negotiated")
                {
                    dispute.resolutionKind = "joint_maintenance";
                    dispute.status = "jointly_maintained";
                    dispute.agreementSettled = true;
                }
                else if (dispute.status == "rejected")
                {
                    dispute.resolutionKind = "rejected";
                    dispute.agreementSettled = true;
                }
            }
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
            foreach (var obligation in progress.obligations)
            {
                if (obligation == null || string.IsNullOrWhiteSpace(obligation.id) ||
                    !factionIds.Contains(obligation.debtorFactionId) ||
                    !factionIds.Contains(obligation.creditorFactionId) ||
                    obligation.amountPerInstallment < 0 ||
                    obligation.remainingInstallments < 0 ||
                    obligation.fulfilledAmount < 0 || obligation.missedPayments < 0 ||
                    (obligation.kind != "loan_repayment" &&
                     obligation.kind != "labor_service" &&
                     obligation.kind != "access_right" &&
                     obligation.kind != "tribute"))
                    throw new InvalidOperationException("地域契約債務が不正");
            }
            var recordIds = new HashSet<string>();
            foreach (var record in progress.diplomaticRecords)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.id) ||
                    !recordIds.Add(record.id) || record.turn < 0 ||
                    string.IsNullOrWhiteSpace(record.category) ||
                    string.IsNullOrWhiteSpace(record.outcome) ||
                    string.IsNullOrWhiteSpace(record.summaryJa) ||
                    record.reputationAfter < 0 || record.reputationAfter > 100)
                    throw new InvalidOperationException("外交履歴が不正");
            }
            if (progress.diplomaticReputation < 0 ||
                progress.diplomaticReputation > 100)
                throw new InvalidOperationException("外交評判が範囲外");
            foreach (var dispute in progress.waterDisputes)
            {
                if (dispute == null || string.IsNullOrWhiteSpace(dispute.id) ||
                    string.IsNullOrWhiteSpace(dispute.claimantFactionId) ||
                    string.IsNullOrWhiteSpace(dispute.respondentFactionId) ||
                    string.IsNullOrWhiteSpace(dispute.segmentId) ||
                    string.IsNullOrWhiteSpace(dispute.claimantFarmId) ||
                    string.IsNullOrWhiteSpace(dispute.claimantSegmentId) ||
                    FindFarm(progress, dispute.claimantFarmId) == null ||
                    FindSegment(progress, dispute.claimantSegmentId) == null ||
                    FindSegment(progress, dispute.segmentId) == null ||
                    dispute.claimantWaterDeficit < 0 ||
                    dispute.claimantCanalCondition < 0 ||
                    dispute.claimantCanalCondition > 100 ||
                    dispute.respondentFlowAtClaim < 0 ||
                    dispute.waterSharePerTurn < 0 ||
                    dispute.waterShareExpectedTotal < 0 ||
                    dispute.waterSharedTotal < 0 ||
                    dispute.barleyTransferred < 0 || dispute.laborCommitted < 0 ||
                    dispute.trustAfter < 0 || dispute.trustAfter > 100 ||
                    string.IsNullOrWhiteSpace(dispute.confidence))
                    throw new InvalidOperationException("水利紛争状態が不正");
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
                case RequestLoanAction:
                    return CreateLoanRequest(progress, turn, out resultJa);
                case OfferLaborAction:
                    return CreateLaborOffer(progress, turn, out resultJa);
                case AcquireAccessAction:
                    return CreateAccessAgreement(progress, turn, out resultJa);
                case OfferTributeAction:
                    return CreateTributeAgreement(progress, turn, out resultJa);
                case NegotiateWaterAction:
                    return ResolveFirstDispute(progress, turn,
                        "joint_maintenance", out resultJa);
                case ShareWaterAction:
                    return ResolveFirstDispute(progress, turn,
                        "share_water", out resultJa);
                case CompensateWaterAction:
                    return ResolveFirstDispute(progress, turn,
                        "grain_compensation", out resultJa);
                case RejectWaterAction:
                    return ResolveFirstDispute(progress, turn,
                        "rejected", out resultJa);
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
            ResolveWater(progress, completedTurn);
            ResolveFarms(progress);
            ResolveAiLedgers(progress, flood);
            AdvanceObligations(session.Definition, progress, completedTurn);
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

        public static UrukObligationState FirstHumanObligation(
            UrukCampaignProgress progress)
        {
            if (progress?.obligations == null) return null;
            foreach (var obligation in progress.obligations)
                if ((obligation.debtorFactionId == HumanFactionId ||
                     obligation.creditorFactionId == HumanFactionId) &&
                    (obligation.status == "active" ||
                     obligation.status == "in_transit"))
                    return obligation;
            return null;
        }

        public static UrukDiplomaticRecordState LatestHumanDiplomaticRecord(
            UrukCampaignProgress progress)
        {
            if (progress?.diplomaticRecords == null) return null;
            for (int i = progress.diplomaticRecords.Length - 1; i >= 0; i--)
            {
                var record = progress.diplomaticRecords[i];
                if (record != null) return record;
            }
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

        public static void ResolveWaterForTest(UrukCampaignProgress progress,
            int turn = 0)
        {
            ResolveWater(progress, turn);
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

        static void ResolveWater(UrukCampaignProgress progress, int turn)
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
            ApplyWaterSharingAgreements(progress, turn);
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
                TryScheduleAiEmergencyLoan(progress, faction, turn);
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
                TryOpenObservedWaterDispute(progress, turn);
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

        static void TryOpenObservedWaterDispute(UrukCampaignProgress progress,
            int turn)
        {
            var claimantFarm = FindFarm(progress, "lagash_hinterland_farm");
            var claimantSegment = FindSegment(progress, "lagash_tigris_branch");
            var respondentSegment = FindSegment(progress, "uruk_intake_segment");
            if (claimantFarm == null || claimantSegment == null ||
                respondentSegment == null)
                return;
            int deficit = Math.Max(0,
                claimantFarm.waterDemand - claimantFarm.waterReceived);
            bool claimantAtRisk = deficit > 0 || claimantSegment.condition < 45;
            bool respondentDrawingWater = respondentSegment.currentFlow >= 3;
            if (!claimantAtRisk || !respondentDrawingWater) return;

            string cause = deficit > 0
                ? $"ラガシュ農地で必要{claimantFarm.waterDemand}に対し" +
                  $"取水{claimantFarm.waterReceived}（不足{deficit}）を観測。"
                : $"ラガシュ取水路が状態{claimantSegment.condition}%まで劣化し、" +
                  "次期の不足が懸念される。";
            cause += $"同じ期間にウルク取水路で流量" +
                $"{respondentSegment.currentFlow}を観測したため、分水または補償を要求。";
            var claimant = FindFaction(progress, "lagash_region");
            Append(ref progress.waterDisputes, new UrukWaterDisputeState
            {
                id = "lagash_upstream_diversion",
                claimantFactionId = "lagash_region",
                respondentFactionId = HumanFactionId,
                claimantFarmId = claimantFarm.id,
                claimantSegmentId = claimantSegment.id,
                segmentId = respondentSegment.id,
                causeJa = cause,
                createdTurn = turn,
                claimantWaterDeficit = deficit,
                claimantCanalCondition = claimantSegment.condition,
                respondentFlowAtClaim = respondentSegment.currentFlow,
                trustAfter = claimant?.diplomaticTrust ?? 50,
                confidence = "inferred",
                status = "open",
            });
        }

        static void ApplyWaterSharingAgreements(UrukCampaignProgress progress,
            int turn)
        {
            foreach (var dispute in progress.waterDisputes)
            {
                if (dispute == null || dispute.resolutionKind != "share_water" ||
                    dispute.status != "shared" || dispute.agreementSettled ||
                    turn < dispute.agreementStartTurn)
                    continue;
                if (turn <= dispute.agreementUntilTurn)
                {
                    var target = FindFarm(progress, dispute.claimantFarmId);
                    int needed = target == null ? 0 : Math.Min(
                        dispute.waterSharePerTurn,
                        Math.Max(0, target.waterDemand - target.waterReceived));
                    dispute.waterShareExpectedTotal += needed;
                    int remaining = needed;
                    foreach (var donor in progress.farmPlots)
                    {
                        if (remaining <= 0) break;
                        if (donor.ownerFactionId != dispute.respondentFactionId ||
                            donor.waterReceived <= 0)
                            continue;
                        int shifted = Math.Min(remaining, donor.waterReceived);
                        donor.waterReceived -= shifted;
                        donor.irrigated = donor.waterReceived >= 3;
                        target.waterReceived += shifted;
                        dispute.waterSharedTotal += shifted;
                        remaining -= shifted;
                    }
                    if (target != null)
                        target.irrigated = target.waterReceived >= 3;
                }
                if (turn < dispute.agreementUntilTurn) continue;

                dispute.agreementSettled = true;
                bool fulfilled = dispute.waterSharedTotal >=
                    dispute.waterShareExpectedTotal;
                dispute.status = fulfilled ? "completed" : "defaulted";
                var claimant = FindFaction(progress, dispute.claimantFactionId);
                int trustDelta = fulfilled ? 4 : -8;
                int reputationDelta = fulfilled ? 2 : -6;
                if (claimant != null)
                {
                    claimant.diplomaticTrust = Math.Clamp(
                        claimant.diplomaticTrust + trustDelta, 0, 100);
                    dispute.trustAfter = claimant.diplomaticTrust;
                }
                dispute.resultJa = fulfilled
                    ? $"分水合意を履行した（必要{dispute.waterShareExpectedTotal}／" +
                      $"実施{dispute.waterSharedTotal}）。"
                    : $"分水合意を履行できなかった（必要{dispute.waterShareExpectedTotal}／" +
                      $"実施{dispute.waterSharedTotal}）。";
                RecordDiplomaticEvent(progress, turn, "water_dispute", dispute.id,
                    dispute.claimantFactionId,
                    fulfilled ? "water_share_completed" : "water_share_defaulted",
                    reputationDelta, dispute.resultJa, dispute.confidence);
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

        static void TryScheduleAiEmergencyLoan(UrukCampaignProgress progress,
            UrukRegionalFactionState borrower, int turn)
        {
            if (borrower.lastFoodShortage <= 0 ||
                HasLiveContractBetween(progress, "loan", borrower.factionId))
                return;
            UrukRegionalFactionState lender = null;
            int lenderGrain = 0;
            foreach (var candidate in progress.regionalFactions)
            {
                if (candidate.factionId == borrower.factionId) continue;
                int grain = FactionGood(progress, candidate.factionId, "barley");
                if (grain < 5 || grain <= lenderGrain) continue;
                lender = candidate;
                lenderGrain = grain;
            }
            if (lender == null) return;
            Append(ref progress.tradeOffers, new UrukTradeOfferState
            {
                id = NextId(progress, "ai_loan"),
                proposerFactionId = lender.factionId,
                receiverFactionId = borrower.factionId,
                offeredGoodId = "barley",
                offeredAmount = 2,
                requestedGoodId = "barley",
                requestedAmount = 3,
                status = "accepted_pending",
                createdTurn = turn,
                expiresTurn = turn + 1,
                contractKind = "loan",
                durationTurns = 3,
                installmentCount = 1,
                reasonJa = "食料不足への現物貸付。備蓄余力と関係から受諾した。",
                confidence = "inferred",
            });
            borrower.lastDecisionJa =
                $"{lender.nameJa}から食料2単位を借り、3期後の現物返済を約した。";
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
            bool immediatePayment = offer.contractKind == "barter" ||
                offer.contractKind == "gift";
            if (immediatePayment && AvailableFactionGood(progress,
                offer.receiverFactionId, offer.requestedGoodId) <
                offer.requestedAmount)
            {
                resultJa = "相手が求める物資を用意できない。";
                return false;
            }
            offer.status = "accepted_pending";
            resultJa = offer.contractKind switch
            {
                "loan" => "貸付条件を受諾した。元本到着後、期限までに現物で返済する。",
                "tribute" => "朝貢条件を受諾した。定めた間隔で現物を積み出す。",
                _ => "物々交換を受諾した。ターン終了時に双方の物資を積み出す。",
            };
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

        static bool CreateLoanRequest(UrukCampaignProgress progress, int turn,
            out string resultJa)
        {
            if (progress.diplomaticReputation < 25)
            {
                resultJa = "契約不履行の評判が強く、エリドゥが新しい貸付を拒んだ。";
                return false;
            }
            if (HasLiveContractBetween(progress, "loan", HumanFactionId))
            {
                resultJa = "履行中の貸付がある。返済後に新しい貸付を求めてください。";
                return false;
            }
            if (FactionGood(progress, "eridu_community", "barley") < 2)
            {
                resultJa = "エリドゥに貸し出せる穀物余力がない。";
                return false;
            }
            Append(ref progress.tradeOffers, new UrukTradeOfferState
            {
                id = NextId(progress, "loan"),
                proposerFactionId = "eridu_community",
                receiverFactionId = HumanFactionId,
                offeredGoodId = "barley",
                offeredAmount = 2,
                requestedGoodId = "barley",
                requestedAmount = 3,
                status = "accepted_pending",
                createdTurn = turn,
                expiresTurn = turn + 1,
                contractKind = "loan",
                durationTurns = 4,
                installmentCount = 1,
                reasonJa = "食料2単位を受け取り、4期後に大麦3単位を現物返済する。",
                confidence = "inferred",
            });
            resultJa = "エリドゥとの穀物貸付を合意した。返済期限は4期後。";
            return true;
        }

        static bool CreateLaborOffer(UrukCampaignProgress progress, int turn,
            out string resultJa)
        {
            if (HasLiveContractBetween(progress, "labor", HumanFactionId))
            {
                resultJa = "履行中の労務契約がある。";
                return false;
            }
            if (progress.labor.trade < 10)
            {
                resultJa = "労務契約には交易労働10%以上が必要。人口配分を調整してください。";
                return false;
            }
            if (FactionGood(progress, "eridu_community", "barley") < 1)
            {
                resultJa = "エリドゥに報酬として渡せる大麦がない。";
                return false;
            }
            Append(ref progress.tradeOffers, new UrukTradeOfferState
            {
                id = NextId(progress, "labor"),
                proposerFactionId = HumanFactionId,
                receiverFactionId = "eridu_community",
                offeredGoodId = "labor_service",
                offeredAmount = 10,
                requestedGoodId = "barley",
                requestedAmount = 1,
                status = "accepted_pending",
                createdTurn = turn,
                expiresTurn = turn + 1,
                contractKind = "labor",
                durationTurns = 1,
                installmentCount = 1,
                reasonJa = "共同体が自発的に水路労務を提供し、大麦1単位を受け取る。",
                confidence = "inferred",
            });
            resultJa = "エリドゥの水路整備へ労務10を提供する契約を結んだ。";
            return true;
        }

        static bool CreateAccessAgreement(UrukCampaignProgress progress, int turn,
            out string resultJa)
        {
            if (progress.diplomaticReputation < 30)
            {
                resultJa = "外交評判が低く、エリドゥが水路通行権の供与を拒んだ。";
                return false;
            }
            if (HasLiveContractBetween(progress, "access", HumanFactionId))
            {
                resultJa = "有効な通行権契約がある。";
                return false;
            }
            if (AvailableFactionGood(progress, HumanFactionId, "reeds") < 1)
            {
                resultJa = "通行権の対価にする葦がない。";
                return false;
            }
            if (FindSegment(progress, "eridu_wetland_intake") == null)
            {
                resultJa = "対象水路が存在しない。";
                return false;
            }
            Append(ref progress.tradeOffers, new UrukTradeOfferState
            {
                id = NextId(progress, "access"),
                proposerFactionId = HumanFactionId,
                receiverFactionId = "eridu_community",
                offeredGoodId = "reeds",
                offeredAmount = 1,
                requestedGoodId = "access_right",
                requestedAmount = 0,
                status = "accepted_pending",
                createdTurn = turn,
                expiresTurn = turn + 1,
                contractKind = "access",
                targetId = "eridu_wetland_intake",
                durationTurns = 4,
                installmentCount = 1,
                reasonJa = "葦1単位を対価に、水路と舟運路の共同利用権を4期得る。",
                confidence = "inferred",
            });
            resultJa = "エリドゥ水路の4期通行権を合意した。";
            return true;
        }

        static bool CreateTributeAgreement(UrukCampaignProgress progress, int turn,
            out string resultJa)
        {
            if (HasLiveContractBetween(progress, "tribute", HumanFactionId))
            {
                resultJa = "履行中の朝貢合意がある。";
                return false;
            }
            if (AvailableFactionGood(progress, HumanFactionId, "barley") < 1)
            {
                resultJa = "朝貢に充てる大麦がない。";
                return false;
            }
            Append(ref progress.tradeOffers, new UrukTradeOfferState
            {
                id = NextId(progress, "tribute"),
                proposerFactionId = HumanFactionId,
                receiverFactionId = "ur_community",
                offeredGoodId = "barley",
                offeredAmount = 1,
                requestedGoodId = "",
                requestedAmount = 0,
                status = "accepted_pending",
                createdTurn = turn,
                expiresTurn = turn + 1,
                contractKind = "tribute",
                intervalTurns = 2,
                installmentCount = 3,
                reasonJa = "ウル共同体へ2期ごとに大麦1単位を計3回送る。",
                confidence = "inferred",
            });
            resultJa = "ウル共同体へ3回の現物朝貢を行う合意を結んだ。";
            return true;
        }

        static bool CanCommitOffer(UrukCampaignProgress progress,
            UrukTradeOfferState offer)
        {
            if (offer.contractKind == "loan")
                return FactionGood(progress, offer.proposerFactionId,
                    offer.offeredGoodId) >= offer.offeredAmount;
            if (offer.contractKind == "labor")
                return LaborCapacity(progress, offer.proposerFactionId) >=
                    offer.offeredAmount &&
                    FactionGood(progress, offer.receiverFactionId,
                        offer.requestedGoodId) >= offer.requestedAmount;
            if (offer.contractKind == "access")
                return FactionGood(progress, offer.proposerFactionId,
                    offer.offeredGoodId) >= offer.offeredAmount &&
                    FindSegment(progress, offer.targetId) != null;
            if (offer.contractKind == "tribute")
                return FactionGood(progress, offer.proposerFactionId,
                    offer.offeredGoodId) >= offer.offeredAmount;
            return FactionGood(progress, offer.proposerFactionId,
                       offer.offeredGoodId) >= offer.offeredAmount &&
                   FactionGood(progress, offer.receiverFactionId,
                       offer.requestedGoodId) >= offer.requestedAmount;
        }

        static void CreateObligation(UrukCampaignProgress progress,
            UrukTradeOfferState offer, string kind, string debtor,
            string creditor, string goodId, int amount, int installments,
            int dueTurn)
        {
            Append(ref progress.obligations, new UrukObligationState
            {
                id = NextId(progress, "obligation"),
                contractId = offer.id,
                kind = kind,
                debtorFactionId = debtor,
                creditorFactionId = creditor,
                goodId = goodId,
                amountPerInstallment = amount,
                remainingInstallments = installments,
                dueTurn = dueTurn,
                intervalTurns = Math.Max(1, offer.intervalTurns),
                targetId = offer.targetId,
                status = "active",
                lastResultJa = kind == "access_right"
                    ? "期限付き利用権が有効。" : "履行期限を待っている。",
                confidence = offer.confidence,
            });
        }

        static void AdvanceObligations(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress, int turn)
        {
            foreach (var obligation in progress.obligations)
            {
                if (obligation.status != "active" || turn < obligation.dueTurn)
                    continue;
                if (obligation.kind == "access_right")
                {
                    RemoveSegmentUser(progress, obligation.targetId,
                        obligation.creditorFactionId);
                    obligation.status = "expired";
                    obligation.lastResultJa = "期限満了により通行権を返還した。";
                    RecordObligationEvent(progress, obligation, turn, "expired", 2,
                        "合意した期限を守り、通行権を返還した。");
                    UpdateOfferStatus(progress, obligation.contractId);
                    continue;
                }
                if (obligation.kind == "labor_service")
                {
                    // 受諾時に確保した共同体労務。食料安全の自動配分が次期開始時に
                    // trade比率を戻しても、明示契約済みの1回分は勝手に破棄しない。
                    obligation.fulfilledAmount += obligation.amountPerInstallment;
                    obligation.remainingInstallments = 0;
                    obligation.status = "completed";
                    obligation.lastResultJa =
                        $"水路労務{obligation.amountPerInstallment}を履行した。";
                    AdjustCounterpartyTrust(progress, obligation, 4);
                    RecordObligationEvent(progress, obligation, turn,
                        "completed", 2, obligation.lastResultJa);
                    UpdateOfferStatus(progress, obligation.contractId);
                    continue;
                }
                if (FactionGood(progress, obligation.debtorFactionId,
                        obligation.goodId) < obligation.amountPerInstallment)
                {
                    DefaultObligation(progress, obligation, turn,
                        obligation.kind == "loan_repayment"
                            ? "期限までに返済用の現物を用意できなかった。"
                            : "期日の朝貢物資を用意できなかった。");
                    continue;
                }
                ConsumeFactionGood(progress, obligation.debtorFactionId,
                    obligation.goodId, obligation.amountPerInstallment);
                CreateTransport(definition, progress, obligation.contractId,
                    obligation.debtorFactionId, obligation.creditorFactionId,
                    obligation.goodId, obligation.amountPerInstallment, turn,
                    obligation.id);
                obligation.status = "in_transit";
                obligation.lastResultJa =
                    $"{obligation.goodId} {obligation.amountPerInstallment}を積み出した。";
                UpdateOfferStatus(progress, obligation.contractId);
            }
        }

        static void CompleteObligationDelivery(UrukCampaignProgress progress,
            UrukTransportState transport, int turn)
        {
            var obligation = FindObligation(progress, transport.obligationId);
            if (obligation == null || obligation.status != "in_transit") return;
            obligation.fulfilledAmount += transport.deliveredAmount;
            obligation.remainingInstallments = Math.Max(0,
                obligation.remainingInstallments - 1);
            string loss = transport.lostAmount > 0
                ? $"（輸送中に{transport.lostAmount}損失、共同負担）" : "";
            if (obligation.remainingInstallments > 0)
            {
                obligation.status = "active";
                obligation.dueTurn = turn + Math.Max(1, obligation.intervalTurns);
                obligation.lastResultJa =
                    $"第1回分が到着{loss}。次回期限は第{obligation.dueTurn}期。";
            }
            else
            {
                obligation.status = "completed";
                obligation.lastResultJa = "契約物資の到着を確認し、履行完了。" + loss;
                AdjustCounterpartyTrust(progress, obligation, 6);
                RecordObligationEvent(progress, obligation, turn, "completed",
                    ReputationForCompletion(obligation.kind),
                    obligation.lastResultJa);
            }
        }

        static void DefaultObligation(UrukCampaignProgress progress,
            UrukObligationState obligation, int turn, string reasonJa)
        {
            obligation.missedPayments++;
            obligation.status = "defaulted";
            obligation.lastResultJa = reasonJa;
            if (obligation.debtorFactionId == HumanFactionId)
            {
                progress.stability = Math.Max(0, progress.stability - 5);
                var creditor = FindFaction(progress, obligation.creditorFactionId);
                if (creditor != null)
                    creditor.diplomaticTrust = Math.Max(0,
                        creditor.diplomaticTrust - 15);
            }
            else
            {
                var debtor = FindFaction(progress, obligation.debtorFactionId);
                if (debtor != null) debtor.stability = Math.Max(0,
                    debtor.stability - 5);
            }
            RecordObligationEvent(progress, obligation, turn, "defaulted", -12,
                reasonJa);
            UpdateOfferStatus(progress, obligation.contractId);
        }

        static void CommitAcceptedOffers(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress, int turn)
        {
            foreach (var offer in progress.tradeOffers)
            {
                if (offer.status != "accepted_pending") continue;
                if (!CanCommitOffer(progress, offer))
                {
                    offer.status = "rejected";
                    continue;
                }

                if (offer.contractKind == "loan")
                {
                    ConsumeFactionGood(progress, offer.proposerFactionId,
                        offer.offeredGoodId, offer.offeredAmount);
                    CreateTransport(definition, progress, offer.id,
                        offer.proposerFactionId, offer.receiverFactionId,
                        offer.offeredGoodId, offer.offeredAmount, turn);
                    CreateObligation(progress, offer, "loan_repayment",
                        offer.receiverFactionId, offer.proposerFactionId,
                        offer.requestedGoodId, offer.requestedAmount, 1,
                        turn + Math.Max(1, offer.durationTurns));
                    offer.status = "departed";
                    RecordOfferEvent(progress, offer, turn, "agreed", 0,
                        "現物貸付と返済期限を合意した。");
                    continue;
                }
                if (offer.contractKind == "labor")
                {
                    ConsumeFactionGood(progress, offer.receiverFactionId,
                        offer.requestedGoodId, offer.requestedAmount);
                    CreateTransport(definition, progress, offer.id,
                        offer.receiverFactionId, offer.proposerFactionId,
                        offer.requestedGoodId, offer.requestedAmount, turn);
                    CreateObligation(progress, offer, "labor_service",
                        offer.proposerFactionId, offer.receiverFactionId,
                        "labor_service", offer.offeredAmount, 1, turn + 1);
                    offer.status = "active";
                    RecordOfferEvent(progress, offer, turn, "agreed", 0,
                        "共同水路の労務量と現物報酬を合意した。");
                    continue;
                }
                if (offer.contractKind == "access")
                {
                    ConsumeFactionGood(progress, offer.proposerFactionId,
                        offer.offeredGoodId, offer.offeredAmount);
                    CreateTransport(definition, progress, offer.id,
                        offer.proposerFactionId, offer.receiverFactionId,
                        offer.offeredGoodId, offer.offeredAmount, turn);
                    AddSegmentUser(progress, offer.targetId,
                        offer.proposerFactionId);
                    CreateObligation(progress, offer, "access_right",
                        offer.receiverFactionId, offer.proposerFactionId, "", 0, 0,
                        turn + Math.Max(1, offer.durationTurns));
                    offer.status = "active";
                    RecordOfferEvent(progress, offer, turn, "agreed", 0,
                        "期限付きの水路・舟運通行権を合意した。");
                    continue;
                }
                if (offer.contractKind == "tribute")
                {
                    CreateObligation(progress, offer, "tribute",
                        offer.proposerFactionId, offer.receiverFactionId,
                        offer.offeredGoodId, offer.offeredAmount,
                        Math.Max(1, offer.installmentCount), turn + 1);
                    offer.status = "active";
                    RecordOfferEvent(progress, offer, turn, "agreed", 0,
                        "現物朝貢の量・回数・間隔を合意した。");
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
                RecordOfferEvent(progress, offer, turn, "agreed", 0,
                    offer.contractKind == "gift"
                        ? "現物贈与を積み出した。"
                        : "交換する現物と数量を合意した。");
            }
        }

        static void CreateTransport(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress, string contractId, string origin,
            string destination, string goodId, int amount, int turn,
            string obligationId = null)
        {
            if (amount <= 0) return;
            var from = FindFaction(progress, origin);
            var to = FindFaction(progress, destination);
            string id = NextId(progress, "transport");
            int risk = 8 + StableHash(definition.seed, id, turn) % 13;
            if (HasActiveAccessRight(progress, origin, destination))
                risk = Math.Max(2, risk - 6);
            int lost = StableHash(definition.seed + 17, id, turn) % 100 < risk
                ? Math.Min(1, amount) : 0;
            Append(ref progress.transports, new UrukTransportState
            {
                id = id,
                contractId = contractId,
                obligationId = obligationId,
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
                if (!string.IsNullOrWhiteSpace(transport.obligationId))
                    CompleteObligationDelivery(progress, transport, turn);
                var offer = FindOffer(progress, transport.contractId);
                bool wasCompleted = offer?.status == "completed";
                UpdateOfferStatus(progress, transport.contractId);
                if (offer != null && !wasCompleted && offer.status == "completed" &&
                    !HasObligationForContract(progress, offer.id))
                    RecordOfferEvent(progress, offer, turn, "completed", 2,
                        offer.contractKind == "gift"
                            ? "贈与物資が相手共同体へ到着した。"
                            : "双方の交換物資が到着した。");
            }
        }

        static bool ResolveFirstDispute(UrukCampaignProgress progress, int turn,
            string resolutionKind, out string resultJa)
        {
            var dispute = FirstOpenDispute(progress);
            if (dispute == null)
            {
                resultJa = "交渉可能な水利問題がない。";
                return false;
            }
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            int trustDelta;
            int reputationDelta;
            string outcome;
            switch (resolutionKind)
            {
                case "share_water":
                    dispute.status = "shared";
                    dispute.agreementStartTurn = turn;
                    dispute.agreementUntilTurn = turn + 2;
                    dispute.waterSharePerTurn = 2;
                    dispute.resultJa =
                        "3期間、ウルク農地の取水から毎期2単位をラガシュ農地へ分けることで合意した。";
                    trustDelta = 12;
                    reputationDelta = 5;
                    outcome = "water_shared";
                    break;
                case "grain_compensation":
                    const int barley = 2;
                    if (AvailableFactionGood(progress, HumanFactionId, "barley") <
                        barley)
                    {
                        resultJa = "補償に必要な大麦2がない。";
                        return false;
                    }
                    ConsumeFactionGood(progress, HumanFactionId, "barley", barley);
                    AddFactionGood(progress, dispute.claimantFactionId,
                        "barley", barley);
                    dispute.barleyTransferred = barley;
                    dispute.status = "compensated";
                    dispute.agreementSettled = true;
                    dispute.resultJa =
                        "取水への補償として大麦2をラガシュへ引き渡した。";
                    trustDelta = 8;
                    reputationDelta = 3;
                    outcome = "compensated";
                    break;
                case "joint_maintenance":
                    if (AvailableFactionGood(progress, HumanFactionId, "reeds") < 1)
                    {
                        resultJa = "共同補修に必要な葦1がない。";
                        return false;
                    }
                    if (progress.labor.canal < 10)
                    {
                        resultJa = "共同補修には水利労働を10%以上配分する必要がある。";
                        return false;
                    }
                    var claimantSegment = FindSegment(progress,
                        dispute.claimantSegmentId);
                    if (claimantSegment == null)
                    {
                        resultJa = "共同補修の対象水路を確認できない。";
                        return false;
                    }
                    ConsumeFactionGood(progress, HumanFactionId, "reeds", 1);
                    dispute.laborCommitted = 10;
                    claimantSegment.condition = Math.Clamp(
                        claimantSegment.condition + 15, 0, 100);
                    claimantSegment.silt = Math.Max(0,
                        claimantSegment.silt - 15);
                    claimantSegment.lastMaintainedTurn = turn;
                    dispute.status = "jointly_maintained";
                    dispute.agreementSettled = true;
                    dispute.resultJa =
                        "ウルクが葦1と水利労働10%を出し、ラガシュ取水路を共同補修した。";
                    trustDelta = 10;
                    reputationDelta = 4;
                    outcome = "joint_maintenance";
                    break;
                case "rejected":
                    dispute.status = "rejected";
                    dispute.agreementSettled = true;
                    dispute.resultJa = "要求を拒否し、水利関係が悪化した。";
                    trustDelta = -12;
                    reputationDelta = -4;
                    outcome = "rejected";
                    break;
                default:
                    resultJa = "不明な水利紛争解決案。";
                    return false;
            }
            dispute.resolutionKind = resolutionKind;
            if (claimant != null)
            {
                claimant.diplomaticTrust = Math.Clamp(
                    claimant.diplomaticTrust + trustDelta, 0, 100);
                dispute.trustAfter = claimant.diplomaticTrust;
            }
            RecordDiplomaticEvent(progress, turn, "water_dispute", dispute.id,
                dispute.claimantFactionId, outcome, reputationDelta,
                dispute.resultJa, dispute.confidence);
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

        static bool HasLiveContractBetween(UrukCampaignProgress progress,
            string kind, string factionId)
        {
            foreach (var offer in progress.tradeOffers)
                if (offer.contractKind == kind &&
                    (offer.proposerFactionId == factionId ||
                     offer.receiverFactionId == factionId) &&
                    (offer.status == "accepted_pending" ||
                     offer.status == "departed" || offer.status == "active"))
                    return true;
            return false;
        }

        static UrukObligationState FindObligation(UrukCampaignProgress progress,
            string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var obligation in progress.obligations)
                if (obligation.id == id) return obligation;
            return null;
        }

        static int LaborCapacity(UrukCampaignProgress progress, string factionId)
        {
            return factionId == HumanFactionId
                ? progress.labor.trade
                : FindFaction(progress, factionId)?.labor?.trade ?? 0;
        }

        static void AddSegmentUser(UrukCampaignProgress progress, string segmentId,
            string factionId)
        {
            var segment = FindSegment(progress, segmentId);
            if (segment == null) return;
            var users = new List<string>(segment.userFactionIds ??
                Array.Empty<string>());
            if (!users.Contains(factionId)) users.Add(factionId);
            segment.userFactionIds = users.ToArray();
        }

        static void RemoveSegmentUser(UrukCampaignProgress progress,
            string segmentId, string factionId)
        {
            var segment = FindSegment(progress, segmentId);
            if (segment == null) return;
            var users = new List<string>();
            foreach (string user in segment.userFactionIds ?? Array.Empty<string>())
                if (user != factionId) users.Add(user);
            segment.userFactionIds = users.ToArray();
        }

        static bool HasActiveAccessRight(UrukCampaignProgress progress,
            string origin, string destination)
        {
            foreach (var obligation in progress.obligations)
            {
                if (obligation.kind != "access_right" ||
                    obligation.status != "active") continue;
                if ((obligation.creditorFactionId == origin &&
                     obligation.debtorFactionId == destination) ||
                    (obligation.creditorFactionId == destination &&
                     obligation.debtorFactionId == origin))
                    return true;
            }
            return false;
        }

        static int ReputationForCompletion(string obligationKind) =>
            obligationKind switch
            {
                "loan_repayment" => 4,
                "tribute" => 5,
                "labor_service" => 2,
                "access_right" => 2,
                _ => 1,
            };

        static void RecordOfferEvent(UrukCampaignProgress progress,
            UrukTradeOfferState offer, int turn, string outcome,
            int reputationDelta, string summaryJa)
        {
            string counterparty = offer.proposerFactionId == HumanFactionId
                ? offer.receiverFactionId
                : offer.receiverFactionId == HumanFactionId
                    ? offer.proposerFactionId : null;
            if (counterparty == null) return;
            RecordDiplomaticEvent(progress, turn, "contract", offer.id,
                counterparty, outcome, reputationDelta, summaryJa,
                offer.confidence);
        }

        static void RecordObligationEvent(UrukCampaignProgress progress,
            UrukObligationState obligation, int turn, string outcome,
            int reputationDelta, string summaryJa)
        {
            string counterparty = obligation.debtorFactionId == HumanFactionId
                ? obligation.creditorFactionId
                : obligation.creditorFactionId == HumanFactionId
                    ? obligation.debtorFactionId : null;
            if (counterparty == null) return;
            RecordDiplomaticEvent(progress, turn, "contract", obligation.contractId,
                counterparty, outcome, reputationDelta, summaryJa,
                obligation.confidence);
        }

        static void RecordDiplomaticEvent(UrukCampaignProgress progress, int turn,
            string category, string subjectId, string counterpartyFactionId,
            string outcome, int reputationDelta, string summaryJa,
            string confidence)
        {
            progress.diplomaticReputation = Math.Clamp(
                progress.diplomaticReputation + reputationDelta, 0, 100);
            Append(ref progress.diplomaticRecords, new UrukDiplomaticRecordState
            {
                id = NextId(progress, "diplomacy"),
                category = category,
                subjectId = subjectId,
                counterpartyFactionId = counterpartyFactionId,
                turn = Math.Max(0, turn),
                outcome = outcome,
                reputationDelta = reputationDelta,
                reputationAfter = progress.diplomaticReputation,
                summaryJa = summaryJa,
                confidence = string.IsNullOrWhiteSpace(confidence)
                    ? "inferred" : confidence,
            });
            const int maxRecords = 64;
            if (progress.diplomaticRecords.Length <= maxRecords) return;
            var recent = new UrukDiplomaticRecordState[maxRecords];
            Array.Copy(progress.diplomaticRecords,
                progress.diplomaticRecords.Length - maxRecords,
                recent, 0, maxRecords);
            progress.diplomaticRecords = recent;
        }

        static void AdjustCounterpartyTrust(UrukCampaignProgress progress,
            UrukObligationState obligation, int amount)
        {
            string other = obligation.debtorFactionId == HumanFactionId
                ? obligation.creditorFactionId :
                obligation.creditorFactionId == HumanFactionId
                    ? obligation.debtorFactionId : null;
            var faction = FindFaction(progress, other);
            if (faction != null)
                faction.diplomaticTrust = Math.Clamp(
                    faction.diplomaticTrust + amount, 0, 100);
        }

        static void UpdateOfferStatus(UrukCampaignProgress progress,
            string contractId)
        {
            UrukTradeOfferState offer = null;
            foreach (var candidate in progress.tradeOffers)
                if (candidate.id == contractId)
                {
                    offer = candidate;
                    break;
                }
            if (offer == null) return;
            bool hasObligation = false;
            bool active = false;
            bool defaulted = false;
            foreach (var obligation in progress.obligations)
            {
                if (obligation.contractId != contractId) continue;
                hasObligation = true;
                if (obligation.status == "active" ||
                    obligation.status == "in_transit") active = true;
                if (obligation.status == "defaulted") defaulted = true;
            }
            if (defaulted) offer.status = "defaulted";
            else if (active) offer.status = "active";
            else if (hasObligation || ContractArrived(progress, contractId))
                offer.status = "completed";
        }

        static UrukTradeOfferState FindOffer(UrukCampaignProgress progress,
            string contractId)
        {
            if (string.IsNullOrWhiteSpace(contractId)) return null;
            foreach (var offer in progress.tradeOffers)
                if (offer.id == contractId) return offer;
            return null;
        }

        static bool HasObligationForContract(UrukCampaignProgress progress,
            string contractId)
        {
            foreach (var obligation in progress.obligations)
                if (obligation.contractId == contractId) return true;
            return false;
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

        static UrukFarmPlotState FindFarm(UrukCampaignProgress progress,
            string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            foreach (var farm in progress.farmPlots)
                if (farm.id == id) return farm;
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
