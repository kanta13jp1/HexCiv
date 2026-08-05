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
        /// <summary>現在の整備・播種・収穫を統括する共同体。</summary>
        public string managerFactionId;
        /// <summary>季節利用・共同耕作を認められた共同体。</summary>
        public string[] userFactionIds;
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
        /// <summary>この輸送前に当事者間で受信済みだった情報伝達。</summary>
        public string informationDispatchId;
        /// <summary>-1は予測情報なし。危険率は史実値ではなくゲーム上の推定。</summary>
        public int forecastRiskMinPercent = -1;
        public int forecastRiskMaxPercent = -1;
        public string forecastConfidence;
        public string informationAssuranceJa;
        public bool termsExact;
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
        /// <summary>競合する取水をまとめる推定水系。</summary>
        public string basinId;
        /// <summary>復元モデル上の上流側共同体。</summary>
        public string upstreamFactionId;
        /// <summary>復元モデル上の下流側共同体。</summary>
        public string downstreamFactionId;
        /// <summary>第三者仲裁を担当した共同体。未仲裁なら空文字。</summary>
        public string arbitratorFactionId;
        public int arbitrationTurn;
        public string arbitrationReasonJa;
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
        public bool breached;
        public int renegotiationCount;
        public int trustAfter;
        public string retaliationJa;
        public string confidence;
        /// <summary>open / shared / compensated / jointly_managed / rejected / breached / completed / defaulted。</summary>
        public string status;
        public string resultJa;
    }

    [Serializable]
    public sealed class UrukLandDisputeState
    {
        public string id;
        public string claimantFactionId;
        public string respondentFactionId;
        public string plotId;
        public int createdTurn;
        public int observedYield;
        public int observedWater;
        public string claimantBasisJa;
        public string respondentBasisJa;
        /// <summary>joint_cultivation / grain_compensation / seasonal_mediation / rejected。</summary>
        public string resolutionKind;
        public int agreementStartTurn;
        public int agreementUntilTurn;
        public int yieldSharePerTurn;
        public int yieldShareExpectedTotal;
        public int yieldSharedTotal;
        public int barleyTransferred;
        public int laborCommitted;
        public string arbitratorFactionId;
        public int arbitrationTurn;
        public string arbitrationReasonJa;
        public bool agreementSettled;
        public bool breached;
        public int renegotiationCount;
        public int trustAfter;
        public string retaliationJa;
        public string confidence;
        /// <summary>open / jointly_cultivated / compensated / mediated / rejected / breached / completed / defaulted。</summary>
        public string status;
        public string resultJa;
    }

    /// <summary>
    /// 個人名・婚姻制度を直接復元せず、共同体間の親族連携を現物と信頼で表す推定モデル。
    /// 当人と双方の家族集団の同意を前提とし、人口移転・婚資・継承規則は仮定しない。
    /// </summary>
    [Serializable]
    public sealed class UrukKinshipTieState
    {
        public string id;
        public string proposerFactionId;
        public string partnerFactionId;
        public string relationKind;
        public string humanParticipantJa;
        public string partnerParticipantJa;
        public string consentBasisJa;
        public string evidenceNoteJa;
        public string[] sourceRefs;
        public int createdTurn;
        public int activeUntilTurn;
        public int humanBarleySpent;
        public int humanWoolSpent;
        public int partnerBarleySpent;
        public int trustAfter;
        public string confidence;
        /// <summary>active / established。</summary>
        public string status;
        public string resultJa;
    }

    /// <summary>
    /// 物資移送に先立つ情報伝達。特定の歴史的通信事件を再現せず、媒体の
    /// 考古学的な確認度と、当事者・内容の復元確度を分けて保存する。
    /// </summary>
    [Serializable]
    public sealed class UrukInformationDispatchState
    {
        public string id;
        public string senderFactionId;
        public string receiverFactionId;
        /// <summary>oral_message / clay_sealing / numerical_record。</summary>
        public string medium;
        public string subjectJa;
        public int createdTurn;
        public int arrivalTurn;
        public int activeUntilTurn;
        public int earliestYear;
        public int claySpent;
        public int reliabilityPercent;
        public int riskReductionPercent;
        public int linkedTransportCount;
        public int trustAfter;
        public bool exactQuantities;
        /// <summary>媒体そのものの史料確度。certain / inferred。</summary>
        public string mediumConfidence;
        /// <summary>この送受信を置いた復元モデルの確度。常にinferred。</summary>
        public string scenarioConfidence;
        public string mediumEvidenceJa;
        public string scenarioNoteJa;
        public string[] sourceRefs;
        /// <summary>pending / active / failed / archived。</summary>
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
        public const string BreachWaterAgreementAction =
            "regional_breach_water_agreement";
        public const string RenegotiateWaterAction = "regional_renegotiate_water";
        public const string NextWaterDisputeAction = "regional_next_water_dispute";
        public const string ArbitrateWaterDisputesAction =
            "regional_arbitrate_water_disputes";
        public const string JointCultivationLandAction =
            "regional_land_joint_cultivation";
        public const string CompensateLandAction =
            "regional_land_compensation";
        public const string MediateLandAction =
            "regional_land_mediation";
        public const string RejectLandAction = "regional_land_reject";
        public const string BreachLandAgreementAction =
            "regional_land_breach";
        public const string RenegotiateLandAction =
            "regional_land_renegotiate";
        public const string AcceptMigrationAction = "regional_accept_migration";
        public const string RejectMigrationAction = "regional_reject_migration";
        public const string NextKinshipPartnerAction =
            "regional_next_kinship_partner";
        public const string ProposeKinshipTieAction =
            "regional_propose_kinship_tie";
        public const string NextInformationPartnerAction =
            "regional_next_information_partner";
        public const string NextInformationMediumAction =
            "regional_next_information_medium";
        public const string SendInformationAction =
            "regional_send_information";

        public const string OralMessageMedium = "oral_message";
        public const string ClaySealingMedium = "clay_sealing";
        public const string NumericalRecordMedium = "numerical_record";
        public const int ClaySealingEarliestYear = -3500;
        public const int NumericalRecordEarliestYear = -3350;

        const string HumanFactionId = "uruk_community";
        const int MaxKinshipTies = 2;

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
            foreach (var transport in progress.transports)
            {
                if (transport == null) continue;
                transport.informationDispatchId ??= "";
                transport.forecastConfidence ??= "";
                transport.informationAssuranceJa ??= "";
            }
            progress.obligations ??= Array.Empty<UrukObligationState>();
            progress.diplomaticRecords ??=
                Array.Empty<UrukDiplomaticRecordState>();
            progress.migrationGroups ??= Array.Empty<UrukMigrationGroupState>();
            progress.waterDisputes ??= Array.Empty<UrukWaterDisputeState>();
            progress.selectedWaterDisputeId ??= "";
            progress.landDisputes ??= Array.Empty<UrukLandDisputeState>();
            progress.selectedLandDisputeId ??= "";
            progress.kinshipTies ??= Array.Empty<UrukKinshipTieState>();
            progress.selectedKinshipFactionId ??= "";
            progress.informationDispatches ??=
                Array.Empty<UrukInformationDispatchState>();
            progress.selectedInformationFactionId ??= "";
            if (!IsInformationMedium(progress.selectedInformationMedium))
                progress.selectedInformationMedium = OralMessageMedium;
            foreach (var farm in progress.farmPlots)
            {
                if (farm == null) continue;
                if (string.IsNullOrWhiteSpace(farm.managerFactionId))
                    farm.managerFactionId = farm.ownerFactionId;
                if (farm.userFactionIds == null || farm.userFactionIds.Length == 0)
                    farm.userFactionIds = new[] { farm.ownerFactionId };
            }
            if (progress.nextRegionalId <= 0) progress.nextRegionalId = 1;
            NormalizeSelectedWaterCase(progress);
            NormalizeSelectedKinshipPartner(progress);
            NormalizeSelectedInformationPartner(progress);
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

        public static void MigrateWaterDisputesV7(UrukCampaignProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.waterDisputes ??= Array.Empty<UrukWaterDisputeState>();
            foreach (var dispute in progress.waterDisputes)
            {
                if (dispute == null) continue;
                dispute.retaliationJa ??= "";
                if (dispute.renegotiationCount < 0)
                    dispute.renegotiationCount = 0;
            }
        }

        public static void MigrateWaterDisputesV8(UrukCampaignProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.waterDisputes ??= Array.Empty<UrukWaterDisputeState>();
            progress.selectedWaterDisputeId ??= "";
            foreach (var dispute in progress.waterDisputes)
            {
                if (dispute == null) continue;
                if (string.IsNullOrWhiteSpace(dispute.basinId))
                    dispute.basinId = "lower_alluvial_wetland_network";
                if (string.IsNullOrWhiteSpace(dispute.upstreamFactionId))
                    dispute.upstreamFactionId = dispute.respondentFactionId;
                if (string.IsNullOrWhiteSpace(dispute.downstreamFactionId))
                    dispute.downstreamFactionId = dispute.claimantFactionId;
                dispute.arbitratorFactionId ??= "";
                dispute.arbitrationReasonJa ??= "";
                if (string.IsNullOrWhiteSpace(progress.selectedWaterDisputeId) &&
                    dispute.respondentFactionId == HumanFactionId &&
                    dispute.status == "open")
                    progress.selectedWaterDisputeId = dispute.id;
            }
        }

        public static void MigrateLandRightsV9(UrukCampaignProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.landDisputes ??= Array.Empty<UrukLandDisputeState>();
            progress.selectedLandDisputeId ??= "";
            if (progress.farmPlots != null)
                foreach (var farm in progress.farmPlots)
                {
                    if (farm == null) continue;
                    if (string.IsNullOrWhiteSpace(farm.managerFactionId))
                        farm.managerFactionId = farm.ownerFactionId;
                    if (farm.userFactionIds == null || farm.userFactionIds.Length == 0)
                        farm.userFactionIds = new[] { farm.ownerFactionId };
                }
        }

        public static void MigrateKinshipV10(UrukCampaignProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.kinshipTies ??= Array.Empty<UrukKinshipTieState>();
            progress.selectedKinshipFactionId ??= "";
        }

        public static void MigrateInformationV11(UrukCampaignProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.informationDispatches ??=
                Array.Empty<UrukInformationDispatchState>();
            progress.selectedInformationFactionId ??= "";
            if (!IsInformationMedium(progress.selectedInformationMedium))
                progress.selectedInformationMedium = OralMessageMedium;
        }

        public static void MigrateTransportForecastsV12(
            UrukCampaignProgress progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            progress.transports ??= Array.Empty<UrukTransportState>();
            foreach (var transport in progress.transports)
            {
                if (transport == null) continue;
                transport.informationDispatchId = "";
                transport.forecastRiskMinPercent = -1;
                transport.forecastRiskMaxPercent = -1;
                transport.forecastConfidence = "";
                transport.informationAssuranceJa = "";
                transport.termsExact = false;
            }
            progress.informationDispatches ??=
                Array.Empty<UrukInformationDispatchState>();
            foreach (var dispatch in progress.informationDispatches)
                if (dispatch != null) dispatch.linkedTransportCount = 0;
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
                    !factionIds.Contains(farm.managerFactionId) ||
                    farm.userFactionIds == null ||
                    (farm.crop != "barley" && farm.crop != "emmer" &&
                     farm.crop != "fallow") ||
                    !Percent(farm.condition) || !Percent(farm.drainage) ||
                    !Percent(farm.salinity) || farm.waterReceived < 0 ||
                    farm.lastYield < 0)
                    throw new InvalidOperationException("農地区画状態が不正");
                var users = new HashSet<string>();
                foreach (string user in farm.userFactionIds)
                    if (!factionIds.Contains(user) || !users.Add(user))
                        throw new InvalidOperationException("農地利用権台帳が不正");
            }
            foreach (var transport in progress.transports)
            {
                if (transport == null || transport.shippedAmount < 0 ||
                    transport.remainingAmount < 0 || transport.lostAmount < 0 ||
                    transport.deliveredAmount < 0 ||
                    transport.remainingAmount + transport.lostAmount +
                        transport.deliveredAmount != transport.shippedAmount)
                    throw new InvalidOperationException("輸送物資の保存則違反");
                bool informed = !string.IsNullOrWhiteSpace(
                    transport.informationDispatchId);
                if (!informed && (transport.forecastRiskMinPercent != -1 ||
                    transport.forecastRiskMaxPercent != -1 ||
                    !string.IsNullOrWhiteSpace(transport.forecastConfidence) ||
                    !string.IsNullOrWhiteSpace(transport.informationAssuranceJa) ||
                    transport.termsExact))
                    throw new InvalidOperationException("情報なし輸送の予測状態が不正");
                if (informed)
                {
                    var dispatch = FindInformationDispatch(progress,
                        transport.informationDispatchId);
                    bool samePair = dispatch != null &&
                        ((dispatch.senderFactionId == transport.originFactionId &&
                          dispatch.receiverFactionId == transport.destinationFactionId) ||
                         (dispatch.receiverFactionId == transport.originFactionId &&
                          dispatch.senderFactionId == transport.destinationFactionId));
                    if (!samePair || transport.forecastRiskMinPercent < 2 ||
                        transport.forecastRiskMaxPercent <
                            transport.forecastRiskMinPercent ||
                        transport.forecastRiskMaxPercent > 35 ||
                        transport.riskPercent < transport.forecastRiskMinPercent ||
                        transport.riskPercent > transport.forecastRiskMaxPercent ||
                        transport.forecastConfidence != "inferred" ||
                        string.IsNullOrWhiteSpace(
                            transport.informationAssuranceJa) ||
                        transport.termsExact != dispatch.exactQuantities)
                        throw new InvalidOperationException("情報照合輸送の予測状態が不正");
                }
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
                    string.IsNullOrWhiteSpace(dispute.basinId) ||
                    !factionIds.Contains(dispute.upstreamFactionId) ||
                    !factionIds.Contains(dispute.downstreamFactionId) ||
                    dispute.arbitratorFactionId == null ||
                    (!string.IsNullOrWhiteSpace(dispute.arbitratorFactionId) &&
                     !factionIds.Contains(dispute.arbitratorFactionId)) ||
                    dispute.arbitrationTurn < 0 ||
                    dispute.arbitrationReasonJa == null ||
                    dispute.waterSharePerTurn < 0 ||
                    dispute.waterShareExpectedTotal < 0 ||
                    dispute.waterSharedTotal < 0 ||
                    dispute.barleyTransferred < 0 || dispute.laborCommitted < 0 ||
                    dispute.renegotiationCount < 0 ||
                    dispute.trustAfter < 0 || dispute.trustAfter > 100 ||
                    dispute.retaliationJa == null ||
                    string.IsNullOrWhiteSpace(dispute.confidence))
                    throw new InvalidOperationException("水利紛争状態が不正");
            }
            if (!string.IsNullOrWhiteSpace(progress.selectedWaterDisputeId) &&
                !IsActionableWaterCase(FindDispute(progress,
                    progress.selectedWaterDisputeId)))
                throw new InvalidOperationException("選択中の水利案件が不正");
            foreach (var dispute in progress.landDisputes)
            {
                if (dispute == null || string.IsNullOrWhiteSpace(dispute.id) ||
                    !factionIds.Contains(dispute.claimantFactionId) ||
                    !factionIds.Contains(dispute.respondentFactionId) ||
                    FindFarm(progress, dispute.plotId) == null ||
                    dispute.createdTurn < 0 || dispute.observedYield < 0 ||
                    dispute.observedWater < 0 ||
                    string.IsNullOrWhiteSpace(dispute.claimantBasisJa) ||
                    string.IsNullOrWhiteSpace(dispute.respondentBasisJa) ||
                    dispute.agreementStartTurn < 0 ||
                    dispute.agreementUntilTurn < 0 ||
                    dispute.yieldSharePerTurn < 0 ||
                    dispute.yieldShareExpectedTotal < 0 ||
                    dispute.yieldSharedTotal < 0 ||
                    dispute.barleyTransferred < 0 ||
                    dispute.laborCommitted < 0 ||
                    dispute.arbitratorFactionId == null ||
                    (!string.IsNullOrWhiteSpace(dispute.arbitratorFactionId) &&
                     !factionIds.Contains(dispute.arbitratorFactionId)) ||
                    dispute.arbitrationTurn < 0 ||
                    dispute.arbitrationReasonJa == null ||
                    dispute.renegotiationCount < 0 ||
                    dispute.trustAfter < 0 || dispute.trustAfter > 100 ||
                    dispute.retaliationJa == null ||
                    string.IsNullOrWhiteSpace(dispute.confidence) ||
                    string.IsNullOrWhiteSpace(dispute.status))
                    throw new InvalidOperationException("土地紛争状態が不正");
            }
            if (!string.IsNullOrWhiteSpace(progress.selectedLandDisputeId) &&
                FindOpenLandDispute(progress, progress.selectedLandDisputeId) == null)
                throw new InvalidOperationException("選択中の土地案件が不正");
            var kinshipIds = new HashSet<string>();
            foreach (var tie in progress.kinshipTies)
            {
                if (tie == null || string.IsNullOrWhiteSpace(tie.id) ||
                    !kinshipIds.Add(tie.id) ||
                    tie.proposerFactionId != HumanFactionId ||
                    !factionIds.Contains(tie.partnerFactionId) ||
                    tie.partnerFactionId == HumanFactionId ||
                    tie.relationKind != "community_kinship_tie" ||
                    string.IsNullOrWhiteSpace(tie.humanParticipantJa) ||
                    string.IsNullOrWhiteSpace(tie.partnerParticipantJa) ||
                    string.IsNullOrWhiteSpace(tie.consentBasisJa) ||
                    string.IsNullOrWhiteSpace(tie.evidenceNoteJa) ||
                    tie.sourceRefs == null || tie.sourceRefs.Length == 0 ||
                    tie.createdTurn < 0 || tie.activeUntilTurn < tie.createdTurn ||
                    tie.humanBarleySpent < 0 || tie.humanWoolSpent < 0 ||
                    tie.partnerBarleySpent < 0 ||
                    tie.trustAfter < 0 || tie.trustAfter > 100 ||
                    tie.confidence != "inferred" ||
                    (tie.status != "active" && tie.status != "established") ||
                    string.IsNullOrWhiteSpace(tie.resultJa))
                    throw new InvalidOperationException("親族連携状態が不正");
                foreach (string sourceRef in tie.sourceRefs)
                    if (!HasSource(definition, sourceRef))
                        throw new InvalidOperationException("親族連携の出典参照が不正");
            }
            if (!string.IsNullOrWhiteSpace(progress.selectedKinshipFactionId) &&
                !IsEligibleKinshipPartner(progress,
                    progress.selectedKinshipFactionId))
                throw new InvalidOperationException("選択中の親族連携候補が不正");
            var dispatchIds = new HashSet<string>();
            foreach (var dispatch in progress.informationDispatches)
            {
                if (dispatch == null || string.IsNullOrWhiteSpace(dispatch.id) ||
                    !dispatchIds.Add(dispatch.id) ||
                    dispatch.senderFactionId != HumanFactionId ||
                    !factionIds.Contains(dispatch.receiverFactionId) ||
                    dispatch.receiverFactionId == HumanFactionId ||
                    !IsInformationMedium(dispatch.medium) ||
                    string.IsNullOrWhiteSpace(dispatch.subjectJa) ||
                    dispatch.createdTurn < 0 ||
                    dispatch.arrivalTurn < dispatch.createdTurn ||
                    dispatch.activeUntilTurn < dispatch.arrivalTurn ||
                    dispatch.claySpent < 0 || dispatch.claySpent > 1 ||
                    !Percent(dispatch.reliabilityPercent) ||
                    dispatch.riskReductionPercent < 0 ||
                    dispatch.riskReductionPercent > 5 ||
                    dispatch.linkedTransportCount < 0 ||
                    dispatch.trustAfter < 0 || dispatch.trustAfter > 100 ||
                    (dispatch.mediumConfidence != "certain" &&
                     dispatch.mediumConfidence != "inferred") ||
                    dispatch.scenarioConfidence != "inferred" ||
                    string.IsNullOrWhiteSpace(dispatch.mediumEvidenceJa) ||
                    string.IsNullOrWhiteSpace(dispatch.scenarioNoteJa) ||
                    dispatch.sourceRefs == null || dispatch.sourceRefs.Length == 0 ||
                    (dispatch.status != "pending" && dispatch.status != "active" &&
                     dispatch.status != "failed" && dispatch.status != "archived") ||
                    string.IsNullOrWhiteSpace(dispatch.resultJa) ||
                    (dispatch.medium == NumericalRecordMedium) !=
                        dispatch.exactQuantities)
                    throw new InvalidOperationException("情報伝達状態が不正");
                int linkedTransportCount = 0;
                foreach (var transport in progress.transports)
                    if (transport != null &&
                        transport.informationDispatchId == dispatch.id)
                        linkedTransportCount++;
                if (dispatch.linkedTransportCount != linkedTransportCount)
                    throw new InvalidOperationException("情報伝達の輸送照合件数が不正");
                foreach (string sourceRef in dispatch.sourceRefs)
                    if (!HasSource(definition, sourceRef))
                        throw new InvalidOperationException("情報伝達の出典参照が不正");
            }
            if (!string.IsNullOrWhiteSpace(
                    progress.selectedInformationFactionId) &&
                !IsInformationPartner(progress,
                    progress.selectedInformationFactionId))
                throw new InvalidOperationException("選択中の情報伝達先が不正");
            if (!IsInformationMedium(progress.selectedInformationMedium))
                throw new InvalidOperationException("選択中の情報媒体が不正");
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
                case BreachWaterAgreementAction:
                    return BreachActiveWaterAgreement(progress, turn, out resultJa);
                case RenegotiateWaterAction:
                    return RenegotiateWaterDispute(progress, turn, out resultJa);
                case NextWaterDisputeAction:
                    return SelectNextWaterDispute(progress, out resultJa);
                case ArbitrateWaterDisputesAction:
                    return ArbitrateOpenWaterDisputes(progress, turn, out resultJa);
                case JointCultivationLandAction:
                    return ResolveOpenLandDispute(progress, turn,
                        "joint_cultivation", out resultJa);
                case CompensateLandAction:
                    return ResolveOpenLandDispute(progress, turn,
                        "grain_compensation", out resultJa);
                case MediateLandAction:
                    return ResolveOpenLandDispute(progress, turn,
                        "seasonal_mediation", out resultJa);
                case RejectLandAction:
                    return ResolveOpenLandDispute(progress, turn,
                        "rejected", out resultJa);
                case BreachLandAgreementAction:
                    return BreachActiveLandAgreement(progress, turn, out resultJa);
                case RenegotiateLandAction:
                    return RenegotiateLandDispute(progress, turn, out resultJa);
                case AcceptMigrationAction:
                    return RespondToMigration(progress, turn, true, out resultJa);
                case RejectMigrationAction:
                    return RespondToMigration(progress, turn, false, out resultJa);
                case NextKinshipPartnerAction:
                    return SelectNextKinshipPartner(progress, out resultJa);
                case ProposeKinshipTieAction:
                    return ProposeKinshipTie(progress, turn, out resultJa);
                case NextInformationPartnerAction:
                    return SelectNextInformationPartner(progress, out resultJa);
                case NextInformationMediumAction:
                    return SelectNextInformationMedium(progress, out resultJa);
                case SendInformationAction:
                    return SendInformation(session, turn, out resultJa);
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
            AdvanceLandAgreements(progress, completedTurn);
            ResolveAiLedgers(progress, flood);
            AdvanceObligations(session.Definition, progress, completedTurn);
            AdvanceTransports(progress, completedTurn);
            AdvanceMigrations(progress, completedTurn);
            AdvanceKinshipTies(progress, completedTurn);
            AdvanceInformationDispatches(session.Definition, progress,
                completedTurn);
            PlanAi(session.Definition, progress, completedTurn);
            DegradeCanals(progress, completedTurn, flood);
            SyncHumanFaction(progress);
            NormalizeSelectedWaterCase(progress);
            NormalizeSelectedKinshipPartner(progress);
            NormalizeSelectedInformationPartner(progress);
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

        public static UrukWaterDisputeState SelectedOpenDispute(
            UrukCampaignProgress progress)
        {
            var selected = SelectedWaterCase(progress);
            return selected != null && selected.status == "open"
                ? selected : null;
        }

        public static int OpenWaterDisputeCount(UrukCampaignProgress progress)
        {
            int count = 0;
            if (progress?.waterDisputes == null) return count;
            foreach (var dispute in progress.waterDisputes)
                if (dispute != null &&
                    dispute.respondentFactionId == HumanFactionId &&
                    dispute.status == "open") count++;
            return count;
        }

        /// <summary>
        /// 判断待ち、履行中、再交渉可能のいずれかで、現在UIが示す水利対象。
        /// 表示対象と実行対象を一致させ、複数案件で別の合意を誤操作しない。
        /// </summary>
        public static UrukWaterDisputeState SelectedWaterCase(
            UrukCampaignProgress progress)
        {
            var selected = FindDispute(progress,
                progress?.selectedWaterDisputeId);
            if (IsActionableWaterCase(selected)) return selected;
            if (progress?.waterDisputes == null) return null;
            foreach (var dispute in progress.waterDisputes)
                if (IsActionableWaterCase(dispute)) return dispute;
            return null;
        }

        public static int ActionableWaterCaseCount(
            UrukCampaignProgress progress)
        {
            int count = 0;
            if (progress?.waterDisputes == null) return count;
            foreach (var dispute in progress.waterDisputes)
                if (IsActionableWaterCase(dispute)) count++;
            return count;
        }

        public static int SelectedWaterCaseOrdinal(
            UrukCampaignProgress progress)
        {
            var selected = SelectedWaterCase(progress);
            if (selected == null || progress?.waterDisputes == null) return 0;
            int ordinal = 0;
            foreach (var dispute in progress.waterDisputes)
            {
                if (!IsActionableWaterCase(dispute)) continue;
                ordinal++;
                if (dispute.id == selected.id) return ordinal;
            }
            return 0;
        }

        public static UrukWaterDisputeState SelectedActiveWaterAgreement(
            UrukCampaignProgress progress)
        {
            var selected = SelectedWaterCase(progress);
            return IsActiveWaterAgreement(selected) ? selected : null;
        }

        public static UrukWaterDisputeState SelectedRecoverableWaterDispute(
            UrukCampaignProgress progress)
        {
            var selected = SelectedWaterCase(progress);
            return IsRecoverableWaterDispute(selected) ? selected : null;
        }

        public static UrukWaterDisputeState FirstActiveWaterAgreement(
            UrukCampaignProgress progress)
        {
            if (progress?.waterDisputes == null) return null;
            foreach (var dispute in progress.waterDisputes)
                if (IsActiveWaterAgreement(dispute)) return dispute;
            return null;
        }

        public static UrukWaterDisputeState LatestRecoverableWaterDispute(
            UrukCampaignProgress progress)
        {
            if (progress?.waterDisputes == null) return null;
            for (int i = progress.waterDisputes.Length - 1; i >= 0; i--)
            {
                var dispute = progress.waterDisputes[i];
                if (IsRecoverableWaterDispute(dispute)) return dispute;
            }
            return null;
        }

        public static UrukLandDisputeState FirstOpenLandDispute(
            UrukCampaignProgress progress)
        {
            if (progress?.landDisputes == null) return null;
            foreach (var dispute in progress.landDisputes)
                if (dispute != null &&
                    dispute.respondentFactionId == HumanFactionId &&
                    dispute.status == "open") return dispute;
            return null;
        }

        public static UrukLandDisputeState SelectedOpenLandDispute(
            UrukCampaignProgress progress)
        {
            var selected = FindOpenLandDispute(progress,
                progress?.selectedLandDisputeId);
            return selected ?? FirstOpenLandDispute(progress);
        }

        public static UrukLandDisputeState FirstActiveLandAgreement(
            UrukCampaignProgress progress)
        {
            if (progress?.landDisputes == null) return null;
            foreach (var dispute in progress.landDisputes)
                if (dispute != null && !dispute.agreementSettled &&
                    (dispute.status == "jointly_cultivated" ||
                     dispute.status == "mediated")) return dispute;
            return null;
        }

        public static UrukLandDisputeState LatestRecoverableLandDispute(
            UrukCampaignProgress progress)
        {
            if (progress?.landDisputes == null) return null;
            for (int i = progress.landDisputes.Length - 1; i >= 0; i--)
            {
                var dispute = progress.landDisputes[i];
                if (dispute != null && (dispute.status == "rejected" ||
                    dispute.status == "breached" ||
                    dispute.status == "defaulted")) return dispute;
            }
            return null;
        }

        public static int WaterRetaliationRiskPenalty(
            UrukCampaignProgress progress, string originFactionId,
            string destinationFactionId)
        {
            if (progress?.waterDisputes == null) return 0;
            foreach (var dispute in progress.waterDisputes)
            {
                if (string.IsNullOrWhiteSpace(dispute.retaliationJa)) continue;
                if ((dispute.claimantFactionId == originFactionId &&
                     dispute.respondentFactionId == destinationFactionId) ||
                    (dispute.claimantFactionId == destinationFactionId &&
                     dispute.respondentFactionId == originFactionId))
                    return 10;
            }
            return 0;
        }

        public static int LandRetaliationRiskPenalty(
            UrukCampaignProgress progress, string originFactionId,
            string destinationFactionId)
        {
            if (progress?.landDisputes == null) return 0;
            foreach (var dispute in progress.landDisputes)
            {
                if (dispute == null ||
                    string.IsNullOrWhiteSpace(dispute.retaliationJa)) continue;
                if ((dispute.claimantFactionId == originFactionId &&
                     dispute.respondentFactionId == destinationFactionId) ||
                    (dispute.respondentFactionId == originFactionId &&
                     dispute.claimantFactionId == destinationFactionId))
                    return 8;
            }
            return 0;
        }

        public static UrukRegionalFactionState SelectedKinshipCandidate(
            UrukCampaignProgress progress)
        {
            NormalizeSelectedKinshipPartner(progress);
            return FindFaction(progress, progress?.selectedKinshipFactionId);
        }

        public static UrukKinshipTieState LatestHumanKinshipTie(
            UrukCampaignProgress progress)
        {
            if (progress?.kinshipTies == null) return null;
            for (int i = progress.kinshipTies.Length - 1; i >= 0; i--)
                if (progress.kinshipTies[i] != null) return progress.kinshipTies[i];
            return null;
        }

        public static int KinshipTieCount(UrukCampaignProgress progress)
        {
            int count = 0;
            if (progress?.kinshipTies == null) return count;
            foreach (var tie in progress.kinshipTies)
                if (tie != null && (tie.status == "active" ||
                    tie.status == "established")) count++;
            return count;
        }

        public static bool CanProposeKinshipTie(UrukCampaignProgress progress)
        {
            var partner = SelectedKinshipCandidate(progress);
            return partner != null && KinshipTieCount(progress) < MaxKinshipTies &&
                progress.diplomaticReputation >= 40 &&
                partner.diplomaticTrust >= 45 &&
                AvailableFactionGood(progress, HumanFactionId, "barley") >= 1 &&
                AvailableFactionGood(progress, HumanFactionId, "sheep_wool") >= 1 &&
                AvailableFactionGood(progress, partner.factionId, "barley") >= 1;
        }

        public static int KinshipTransportRiskReduction(
            UrukCampaignProgress progress, string originFactionId,
            string destinationFactionId)
        {
            if (progress?.kinshipTies == null) return 0;
            foreach (var tie in progress.kinshipTies)
            {
                if (tie == null) continue;
                bool pair = (tie.proposerFactionId == originFactionId &&
                    tie.partnerFactionId == destinationFactionId) ||
                    (tie.partnerFactionId == originFactionId &&
                     tie.proposerFactionId == destinationFactionId);
                if (pair) return tie.status == "active" ? 5 :
                    tie.status == "established" ? 3 : 0;
            }
            return 0;
        }

        public static int TransportRiskForTest(
            HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress, string transportId,
            string originFactionId, string destinationFactionId, int turn)
        {
            return CalculateTransportRisk(definition, progress, transportId,
                originFactionId, destinationFactionId, turn);
        }

        static bool SelectNextKinshipPartner(UrukCampaignProgress progress,
            out string resultJa)
        {
            var candidates = KinshipCandidates(progress);
            if (candidates.Count == 0)
            {
                progress.selectedKinshipFactionId = "";
                resultJa = KinshipTieCount(progress) >= MaxKinshipTies
                    ? "親族連携は上限2共同体に達している。"
                    : "現在選べる親族連携候補がいない。";
                return false;
            }
            int current = -1;
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].factionId == progress.selectedKinshipFactionId)
                {
                    current = i;
                    break;
                }
            var next = candidates[(current + 1) % candidates.Count];
            progress.selectedKinshipFactionId = next.factionId;
            progress.regionalRevision++;
            resultJa = $"親族連携候補: {next.nameJa}（信頼{next.diplomaticTrust}）。";
            return true;
        }

        static bool ProposeKinshipTie(UrukCampaignProgress progress, int turn,
            out string resultJa)
        {
            var partner = SelectedKinshipCandidate(progress);
            if (partner == null)
            {
                resultJa = KinshipTieCount(progress) >= MaxKinshipTies
                    ? "親族連携は上限2共同体に達している。"
                    : "親族連携候補がいない。";
                return false;
            }
            if (progress.diplomaticReputation < 40)
            {
                resultJa = "親族連携の協議には外交評判40以上が必要。";
                return false;
            }
            if (partner.diplomaticTrust < 45)
            {
                resultJa = $"{partner.nameJa}との信頼45以上が必要。";
                return false;
            }
            if (AvailableFactionGood(progress, HumanFactionId, "barley") < 1 ||
                AvailableFactionGood(progress, HumanFactionId, "sheep_wool") < 1)
            {
                resultJa = "共同食と贈答に大麦1・羊毛1が必要。";
                return false;
            }
            if (AvailableFactionGood(progress, partner.factionId, "barley") < 1)
            {
                resultJa = $"{partner.nameJa}に共同食へ出せる大麦がない。";
                return false;
            }

            ConsumeFactionGood(progress, HumanFactionId, "barley", 1);
            ConsumeFactionGood(progress, HumanFactionId, "sheep_wool", 1);
            ConsumeFactionGood(progress, partner.factionId, "barley", 1);
            partner.diplomaticTrust = Math.Clamp(
                partner.diplomaticTrust + 8, 0, 100);
            var tie = new UrukKinshipTieState
            {
                id = NextId(progress, "kinship"),
                proposerFactionId = HumanFactionId,
                partnerFactionId = partner.factionId,
                relationKind = "community_kinship_tie",
                humanParticipantJa = "氏名不詳の成人家系構成員",
                partnerParticipantJa = "氏名不詳の成人家系構成員",
                consentBasisJa =
                    "双方の家族集団と当人の同意を前提とする復元モデル",
                evidenceNoteJa =
                    "第4千年紀の地域間交流は確認されるが、この二共同体の具体的婚姻・人物・婚資・継承規則を示す直接史料はない。",
                sourceRefs = new[]
                {
                    "cambridge_uruk_glocalization_2025",
                    "met_uruk_first_city",
                },
                createdTurn = Math.Max(0, turn),
                activeUntilTurn = Math.Max(0, turn) + 3,
                humanBarleySpent = 1,
                humanWoolSpent = 1,
                partnerBarleySpent = 1,
                trustAfter = partner.diplomaticTrust,
                confidence = "inferred",
                status = "active",
                resultJa =
                    "氏名不詳の成人家系構成員どうしの親族連携を協議。双方同意を前提とする推定復元で、4期間の往来安全化を始めた。",
            };
            Append(ref progress.kinshipTies, tie);
            partner.currentGoalJa = "親族連携の履行";
            partner.lastDecisionJa =
                "共同食へ大麦1を拠出し、家族集団間の往来を見守る。";
            partner.knownReasonJa = tie.evidenceNoteJa;
            RecordDiplomaticEvent(progress, turn, "kinship_tie", tie.id,
                partner.factionId, "kinship_tie_formed", 3,
                tie.resultJa, tie.confidence);
            progress.selectedKinshipFactionId = "";
            NormalizeSelectedKinshipPartner(progress);
            progress.regionalRevision++;
            resultJa = tie.resultJa;
            return true;
        }

        static void AdvanceKinshipTies(UrukCampaignProgress progress, int turn)
        {
            bool changed = false;
            foreach (var tie in progress.kinshipTies)
            {
                if (tie == null || tie.status != "active" ||
                    turn < tie.activeUntilTurn) continue;
                tie.status = "established";
                var partner = FindFaction(progress, tie.partnerFactionId);
                if (partner != null)
                {
                    partner.diplomaticTrust = Math.Clamp(
                        partner.diplomaticTrust + 3, 0, 100);
                    tie.trustAfter = partner.diplomaticTrust;
                }
                tie.resultJa =
                    "4期間の往来を終え、氏名不詳の家系構成員を介する共同体間連携が定着した（双方同意を前提とする推定復元）。";
                RecordDiplomaticEvent(progress, turn, "kinship_tie", tie.id,
                    tie.partnerFactionId, "kinship_tie_established", 2,
                    tie.resultJa, tie.confidence);
                changed = true;
            }
            if (changed) progress.regionalRevision++;
        }

        static List<UrukRegionalFactionState> KinshipCandidates(
            UrukCampaignProgress progress)
        {
            var candidates = new List<UrukRegionalFactionState>();
            if (progress?.regionalFactions == null ||
                KinshipTieCount(progress) >= MaxKinshipTies) return candidates;
            foreach (var faction in progress.regionalFactions)
                if (faction != null && IsEligibleKinshipPartner(progress,
                    faction.factionId)) candidates.Add(faction);
            return candidates;
        }

        static bool IsEligibleKinshipPartner(UrukCampaignProgress progress,
            string factionId)
        {
            var faction = FindFaction(progress, factionId);
            if (faction == null || faction.human || factionId == HumanFactionId)
                return false;
            if (progress?.kinshipTies == null) return true;
            foreach (var tie in progress.kinshipTies)
                if (tie != null && tie.partnerFactionId == factionId &&
                    (tie.status == "active" || tie.status == "established"))
                    return false;
            return true;
        }

        static void NormalizeSelectedKinshipPartner(
            UrukCampaignProgress progress)
        {
            if (progress == null) return;
            if (IsEligibleKinshipPartner(progress,
                progress.selectedKinshipFactionId) &&
                KinshipTieCount(progress) < MaxKinshipTies) return;
            progress.selectedKinshipFactionId = "";
            var candidates = KinshipCandidates(progress);
            if (candidates.Count > 0)
                progress.selectedKinshipFactionId = candidates[0].factionId;
        }

        static UrukKinshipTieState KinshipTieWith(
            UrukCampaignProgress progress, string factionId)
        {
            if (progress?.kinshipTies == null) return null;
            foreach (var tie in progress.kinshipTies)
                if (tie != null && tie.partnerFactionId == factionId &&
                    (tie.status == "active" || tie.status == "established"))
                    return tie;
            return null;
        }

        public static UrukRegionalFactionState SelectedInformationPartner(
            UrukCampaignProgress progress)
        {
            NormalizeSelectedInformationPartner(progress);
            return FindFaction(progress, progress?.selectedInformationFactionId);
        }

        public static UrukInformationDispatchState LatestHumanInformationDispatch(
            UrukCampaignProgress progress)
        {
            if (progress?.informationDispatches == null) return null;
            for (int i = progress.informationDispatches.Length - 1; i >= 0; i--)
                if (progress.informationDispatches[i] != null)
                    return progress.informationDispatches[i];
            return null;
        }

        public static UrukTransportState LatestHumanTransport(
            UrukCampaignProgress progress)
        {
            if (progress?.transports == null) return null;
            for (int i = progress.transports.Length - 1; i >= 0; i--)
            {
                var transport = progress.transports[i];
                if (transport != null &&
                    (transport.originFactionId == HumanFactionId ||
                     transport.destinationFactionId == HumanFactionId))
                    return transport;
            }
            return null;
        }

        public static string TransportForecastJa(UrukTransportState transport)
        {
            if (transport == null) return "輸送なし";
            if (string.IsNullOrWhiteSpace(transport.informationDispatchId) ||
                transport.forecastRiskMinPercent < 0)
                return "情報照合なし／危険率は不明";
            string range = transport.forecastRiskMinPercent ==
                transport.forecastRiskMaxPercent
                ? transport.forecastRiskMinPercent + "%"
                : $"{transport.forecastRiskMinPercent}〜" +
                  $"{transport.forecastRiskMaxPercent}%";
            return $"{transport.informationAssuranceJa}／危険{range}（推定）";
        }

        public static bool CanSendInformation(HistoricalCampaignSession session)
        {
            if (session?.Progress == null) return false;
            var progress = session.Progress;
            var partner = SelectedInformationPartner(progress);
            string medium = progress.selectedInformationMedium;
            if (partner == null || !IsInformationMedium(medium) ||
                HasPendingInformationFor(progress, partner.factionId)) return false;
            int currentYear = session.CurrentYear;
            if (currentYear < InformationMediumEarliestYear(
                session.Definition, medium)) return false;
            if (medium == NumericalRecordMedium &&
                !progress.administrationAdopted) return false;
            return medium == OralMessageMedium ||
                AvailableFactionGood(progress, HumanFactionId,
                    "alluvial_clay") >= 1;
        }

        public static string InformationRequirementJa(
            HistoricalCampaignSession session)
        {
            if (session?.Progress == null) return "セッションなし";
            var progress = session.Progress;
            var partner = SelectedInformationPartner(progress);
            string medium = progress.selectedInformationMedium;
            if (partner == null) return "伝達先なし";
            if (HasPendingInformationFor(progress, partner.factionId))
                return "同じ相手への伝達が到着待ち";
            int earliest = InformationMediumEarliestYear(session.Definition,
                medium);
            if (session.CurrentYear < earliest)
                return HistoricalCampaignCalendar.FormatYearJa(earliest) +
                    "以後に利用可能";
            if (medium == NumericalRecordMedium &&
                !progress.administrationAdopted)
                return "数量記録行政の採用が必要";
            if (medium != OralMessageMedium &&
                AvailableFactionGood(progress, HumanFactionId,
                    "alluvial_clay") < 1)
                return "沖積粘土1が必要";
            return $"到着{InformationMediumTravelTurns(medium)}期／" +
                $"信頼度{InformationMediumReliability(medium)}%";
        }

        public static string InformationMediumNameJa(string medium) => medium switch
        {
            OralMessageMedium => "口頭伝言（復元）",
            ClaySealingMedium => "封泥付き荷",
            NumericalRecordMedium => "数量記録板",
            _ => medium,
        };

        public static int InformationMediumEarliestYear(
            HistoricalCampaignDefinition definition, string medium)
        {
            return medium switch
            {
                ClaySealingMedium => ClaySealingEarliestYear,
                NumericalRecordMedium => NumericalRecordEarliestYear,
                _ => definition?.startYear ?? -4000,
            };
        }

        public static int InformationMediumTravelTurns(string medium) =>
            medium == OralMessageMedium ? 2 : 1;

        public static int InformationMediumReliability(string medium) => medium switch
        {
            OralMessageMedium => 65,
            ClaySealingMedium => 85,
            NumericalRecordMedium => 100,
            _ => 0,
        };

        public static string InformationMediumConfidence(string medium) =>
            medium == OralMessageMedium ? "inferred" : "certain";

        public static int CommunicationTransportRiskReduction(
            UrukCampaignProgress progress, string originFactionId,
            string destinationFactionId, int turn)
        {
            int best = 0;
            if (progress?.informationDispatches == null) return best;
            foreach (var dispatch in progress.informationDispatches)
            {
                if (dispatch == null || dispatch.status != "active" ||
                    turn > dispatch.activeUntilTurn) continue;
                bool pair = (dispatch.senderFactionId == originFactionId &&
                    dispatch.receiverFactionId == destinationFactionId) ||
                    (dispatch.receiverFactionId == originFactionId &&
                     dispatch.senderFactionId == destinationFactionId);
                if (pair) best = Math.Max(best,
                    dispatch.riskReductionPercent);
            }
            return best;
        }

        static bool SelectNextInformationPartner(UrukCampaignProgress progress,
            out string resultJa)
        {
            var candidates = InformationPartners(progress);
            if (candidates.Count == 0)
            {
                progress.selectedInformationFactionId = "";
                resultJa = "現在選べる情報伝達先がいない。";
                return false;
            }
            int current = -1;
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].factionId ==
                    progress.selectedInformationFactionId)
                {
                    current = i;
                    break;
                }
            var next = candidates[(current + 1) % candidates.Count];
            progress.selectedInformationFactionId = next.factionId;
            progress.regionalRevision++;
            resultJa = $"情報伝達先: {next.nameJa}。";
            return true;
        }

        static bool SelectNextInformationMedium(UrukCampaignProgress progress,
            out string resultJa)
        {
            string next = progress.selectedInformationMedium switch
            {
                OralMessageMedium => ClaySealingMedium,
                ClaySealingMedium => NumericalRecordMedium,
                _ => OralMessageMedium,
            };
            progress.selectedInformationMedium = next;
            progress.regionalRevision++;
            resultJa = "情報媒体: " + InformationMediumNameJa(next) + "。";
            return true;
        }

        static bool SendInformation(HistoricalCampaignSession session, int turn,
            out string resultJa)
        {
            var progress = session.Progress;
            var partner = SelectedInformationPartner(progress);
            string medium = progress.selectedInformationMedium;
            if (partner == null)
            {
                resultJa = "情報伝達先がいない。";
                return false;
            }
            if (HasPendingInformationFor(progress, partner.factionId))
            {
                resultJa = "同じ相手への伝達がまだ到着していない。";
                return false;
            }
            int currentYear = session.CurrentYear;
            int earliestYear = InformationMediumEarliestYear(
                session.Definition, medium);
            if (currentYear < earliestYear)
            {
                resultJa = InformationMediumNameJa(medium) + "は" +
                    HistoricalCampaignCalendar.FormatYearJa(earliestYear) +
                    "より前には使用できない。";
                return false;
            }
            if (medium == NumericalRecordMedium &&
                !progress.administrationAdopted)
            {
                resultJa = "数量記録板には数量記録行政の採用が必要。";
                return false;
            }
            int clayCost = medium == OralMessageMedium ? 0 : 1;
            if (clayCost > 0 && AvailableFactionGood(progress, HumanFactionId,
                "alluvial_clay") < clayCost)
            {
                resultJa = "封泥・記録媒体に使う沖積粘土1が必要。";
                return false;
            }
            if (clayCost > 0)
                ConsumeFactionGood(progress, HumanFactionId,
                    "alluvial_clay", clayCost);

            int travelTurns = InformationMediumTravelTurns(medium);
            int arrivalTurn = Math.Max(0, turn) + travelTurns;
            var dispatch = new UrukInformationDispatchState
            {
                id = NextId(progress, "information"),
                senderFactionId = HumanFactionId,
                receiverFactionId = partner.factionId,
                medium = medium,
                subjectJa = InformationSubjectJa(medium),
                createdTurn = Math.Max(0, turn),
                arrivalTurn = arrivalTurn,
                activeUntilTurn = arrivalTurn + 3,
                earliestYear = earliestYear,
                claySpent = clayCost,
                reliabilityPercent = InformationMediumReliability(medium),
                riskReductionPercent = medium == OralMessageMedium ? 1 :
                    medium == ClaySealingMedium ? 3 : 5,
                trustAfter = partner.diplomaticTrust,
                exactQuantities = medium == NumericalRecordMedium,
                mediumConfidence = InformationMediumConfidence(medium),
                scenarioConfidence = "inferred",
                mediumEvidenceJa = InformationMediumEvidenceJa(medium),
                scenarioNoteJa =
                    "この二共同体間の個別伝達・担当者・文言は直接史料がないため推定復元。",
                sourceRefs = InformationSourceRefs(medium),
                status = "pending",
                resultJa = $"{partner.nameJa}へ{InformationMediumNameJa(medium)}を発送。" +
                    $"{travelTurns}期間後に到着予定。",
            };
            Append(ref progress.informationDispatches, dispatch);
            partner.currentGoalJa = "ウルクからの情報到着待ち";
            partner.lastDecisionJa = dispatch.resultJa;
            partner.knownReasonJa = dispatch.scenarioNoteJa;
            progress.regionalRevision++;
            resultJa = dispatch.resultJa;
            return true;
        }

        static void AdvanceInformationDispatches(
            HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress, int turn)
        {
            bool changed = false;
            foreach (var dispatch in progress.informationDispatches)
            {
                if (dispatch == null) continue;
                if (dispatch.status == "pending" && turn >= dispatch.arrivalTurn)
                {
                    var partner = FindFaction(progress,
                        dispatch.receiverFactionId);
                    bool understood = StableHash(definition.seed + 31,
                        dispatch.id, dispatch.createdTurn) % 100 <
                        dispatch.reliabilityPercent;
                    if (understood)
                    {
                        dispatch.status = "active";
                        dispatch.activeUntilTurn = turn + 3;
                        int trustDelta = dispatch.medium == OralMessageMedium ? 1 :
                            dispatch.medium == ClaySealingMedium ? 2 : 3;
                        if (partner != null)
                        {
                            partner.diplomaticTrust = Math.Clamp(
                                partner.diplomaticTrust + trustDelta, 0, 100);
                            dispatch.trustAfter = partner.diplomaticTrust;
                            partner.currentGoalJa = "伝達済みの物資移送に備える";
                            partner.lastDecisionJa =
                                InformationMediumNameJa(dispatch.medium) +
                                "を照合した。";
                            partner.knownReasonJa = dispatch.subjectJa;
                        }
                        dispatch.resultJa =
                            $"{InformationMediumNameJa(dispatch.medium)}が到着し、" +
                            $"{dispatch.activeUntilTurn - turn + 1}期間、当事者間の" +
                            $"輸送危険を{dispatch.riskReductionPercent}%軽減する。";
                        RecordDiplomaticEvent(progress, turn,
                            "information_transfer", dispatch.id,
                            dispatch.receiverFactionId, "information_received",
                            dispatch.medium == OralMessageMedium ? 0 : 1,
                            dispatch.resultJa, dispatch.scenarioConfidence);
                    }
                    else
                    {
                        dispatch.status = "failed";
                        dispatch.riskReductionPercent = 0;
                        if (partner != null)
                        {
                            partner.diplomaticTrust = Math.Clamp(
                                partner.diplomaticTrust - 1, 0, 100);
                            dispatch.trustAfter = partner.diplomaticTrust;
                            partner.currentGoalJa = "伝言内容を再確認する";
                            partner.lastDecisionJa = "口頭伝言の内容を一致させられなかった。";
                        }
                        dispatch.resultJa =
                            "口頭伝言の内容が一致せず、物資移送の危険軽減は得られなかった。";
                        RecordDiplomaticEvent(progress, turn,
                            "information_transfer", dispatch.id,
                            dispatch.receiverFactionId, "information_failed", -1,
                            dispatch.resultJa, dispatch.scenarioConfidence);
                    }
                    changed = true;
                }
                else if (dispatch.status == "active" &&
                    turn > dispatch.activeUntilTurn)
                {
                    dispatch.status = "archived";
                    dispatch.resultJa = "伝達済み情報の有効期間が終わった。";
                    changed = true;
                }
            }
            if (changed) progress.regionalRevision++;
        }

        static List<UrukRegionalFactionState> InformationPartners(
            UrukCampaignProgress progress)
        {
            var result = new List<UrukRegionalFactionState>();
            if (progress?.regionalFactions == null) return result;
            foreach (var faction in progress.regionalFactions)
                if (faction != null && !faction.human &&
                    faction.factionId != HumanFactionId) result.Add(faction);
            return result;
        }

        static bool IsInformationPartner(UrukCampaignProgress progress,
            string factionId)
        {
            var faction = FindFaction(progress, factionId);
            return faction != null && !faction.human &&
                faction.factionId != HumanFactionId;
        }

        static void NormalizeSelectedInformationPartner(
            UrukCampaignProgress progress)
        {
            if (progress == null) return;
            if (IsInformationPartner(progress,
                progress.selectedInformationFactionId)) return;
            progress.selectedInformationFactionId = "";
            var candidates = InformationPartners(progress);
            if (candidates.Count > 0)
                progress.selectedInformationFactionId = candidates[0].factionId;
        }

        static bool HasPendingInformationFor(UrukCampaignProgress progress,
            string factionId)
        {
            if (progress?.informationDispatches == null) return false;
            foreach (var dispatch in progress.informationDispatches)
                if (dispatch != null && dispatch.receiverFactionId == factionId &&
                    dispatch.status == "pending") return true;
            return false;
        }

        static UrukInformationDispatchState FindInformationDispatch(
            UrukCampaignProgress progress, string dispatchId)
        {
            if (progress?.informationDispatches == null ||
                string.IsNullOrWhiteSpace(dispatchId)) return null;
            foreach (var dispatch in progress.informationDispatches)
                if (dispatch != null && dispatch.id == dispatchId)
                    return dispatch;
            return null;
        }

        static UrukInformationDispatchState ActiveInformationForPair(
            UrukCampaignProgress progress, string originFactionId,
            string destinationFactionId, int turn)
        {
            if (progress?.informationDispatches == null) return null;
            UrukInformationDispatchState best = null;
            int bestMargin = int.MaxValue;
            for (int i = progress.informationDispatches.Length - 1; i >= 0; i--)
            {
                var dispatch = progress.informationDispatches[i];
                if (dispatch == null || dispatch.status != "active" ||
                    turn > dispatch.activeUntilTurn) continue;
                bool samePair =
                    dispatch.senderFactionId == originFactionId &&
                    dispatch.receiverFactionId == destinationFactionId;
                bool reversePair =
                    dispatch.receiverFactionId == originFactionId &&
                    dispatch.senderFactionId == destinationFactionId;
                if (!samePair && !reversePair) continue;
                int margin = InformationForecastMargin(dispatch.medium);
                if (margin < bestMargin)
                {
                    best = dispatch;
                    bestMargin = margin;
                }
            }
            return best;
        }

        static int InformationForecastMargin(string medium) => medium switch
        {
            OralMessageMedium => 6,
            ClaySealingMedium => 4,
            NumericalRecordMedium => 2,
            _ => 6,
        };

        static string InformationAssuranceJa(string medium) => medium switch
        {
            OralMessageMedium => "口頭で移送予定を共有",
            ClaySealingMedium => "封泥で送受者・荷を識別",
            NumericalRecordMedium => "数量条件を記録",
            _ => "情報を共有",
        };

        static bool IsInformationMedium(string medium) =>
            medium == OralMessageMedium || medium == ClaySealingMedium ||
            medium == NumericalRecordMedium;

        static string InformationSubjectJa(string medium) => medium switch
        {
            OralMessageMedium => "次回の物資移送予定を口頭で伝える",
            ClaySealingMedium => "荷の送り手・宛先を封泥で識別する",
            NumericalRecordMedium => "大麦などの数量を記録して移送予定を照合する",
            _ => "物資移送予定",
        };

        static string InformationMediumEvidenceJa(string medium) => medium switch
        {
            OralMessageMedium =>
                "口頭伝達は考古資料に直接残らず、地域間接触から置いた復元モデル。",
            ClaySealingMedium =>
                "紀元前3500～3100年頃の円筒印章と封泥用途が博物館資料で確認される。",
            NumericalRecordMedium =>
                "紀元前3350年頃以後の原楔形文字会計と紀元前3300～3100年のウルク数量板が確認される。",
            _ => "根拠なし",
        };

        static string[] InformationSourceRefs(string medium) => medium switch
        {
            ClaySealingMedium => new[]
            {
                "met_late_uruk_cylinder_seal",
                "cambridge_seals_signs_2025",
            },
            NumericalRecordMedium => new[]
            {
                "british_museum_uruk_tablet",
                "cambridge_seals_signs_2025",
            },
            _ => new[]
            {
                "cambridge_uruk_glocalization_2025",
                "met_uruk_first_city",
            },
        };

        static bool HasSource(HistoricalCampaignDefinition definition,
            string sourceRef)
        {
            if (definition?.sources == null || string.IsNullOrWhiteSpace(sourceRef))
                return false;
            foreach (var source in definition.sources)
                if (source != null && source.id == sourceRef) return true;
            return false;
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
                    managerFactionId = source.factionId,
                    userFactionIds = new[] { source.factionId },
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
                else if (HasActiveLandAgreement(progress, faction.factionId))
                {
                    bool mediator = IsActiveLandArbitrator(progress,
                        faction.factionId);
                    faction.currentGoalJa = mediator
                        ? "耕作権の第三者仲裁" : "共同耕作の履行監視";
                    faction.lastDecisionJa = mediator
                        ? "境界を固定せず、季節利用と現物配分を裁定した。"
                        : "合意した農地利用と収穫配分を監視している。";
                    faction.knownReasonJa =
                        "灌漑・播種・収穫の観測と、推定される季節利用慣行。";
                }
                else if (HasLandRetaliation(progress, faction.factionId))
                {
                    faction.currentGoalJa = "耕作権の防衛";
                    faction.lastDecisionJa =
                        "境界農地への接近警戒と陸上輸送の監視を継続した。";
                    faction.knownReasonJa = "土地合意の拒否・不履行・破約。";
                }
                else if (HasActiveWaterArbitration(progress, faction.factionId))
                {
                    bool mediator = IsActiveWaterArbitrator(progress,
                        faction.factionId);
                    faction.currentGoalJa = mediator
                        ? "複数水利要求の仲裁" : "仲裁分水の履行監視";
                    faction.lastDecisionJa = mediator
                        ? "競合する分水要求を現物台帳と期限付き裁定へまとめた。"
                        : "第三者裁定の受水量と履行期限を監視している。";
                    faction.knownReasonJa =
                        "同時発生した水不足と推定水系上の上下流関係。";
                }
                else if (HasWaterRetaliation(progress, faction.factionId))
                {
                    faction.currentGoalJa = "水利権の防衛";
                    faction.lastDecisionJa =
                        "ウルクとの輸送警戒と水路警備を継続した。";
                    faction.knownReasonJa = "水利合意の拒否・不履行・破約。";
                }
                else if (KinshipTieWith(progress, faction.factionId) is var tie &&
                    tie != null)
                {
                    faction.currentGoalJa = tie.status == "active"
                        ? "親族連携の履行" : "親族関係を基盤とする交易";
                    faction.lastDecisionJa = tie.status == "active"
                        ? "家族集団間の往来を見守り、共同食の合意を履行している。"
                        : "定着した親族連携を通じて交易路の安全を支えている。";
                    faction.knownReasonJa =
                        "第4千年紀の地域間交流に基づく推定。具体的人物・婚姻制度は不詳。";
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
            if (turn >= 8)
                TryOpenObservedWaterDisputes(progress, turn);
            if (turn >= 14)
                TryOpenObservedLandDispute(progress, turn);
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

        static void TryOpenObservedWaterDisputes(UrukCampaignProgress progress,
            int turn)
        {
            TryOpenObservedWaterDispute(progress, turn,
                "lagash_upstream_diversion", "lagash_region",
                "lagash_hinterland_farm", "lagash_tigris_branch", 8,
                "下流農地帯");
            TryOpenObservedWaterDispute(progress, turn,
                "eridu_wetland_draw", "eridu_community",
                "eridu_hinterland_farm", "eridu_wetland_intake", 10,
                "南部湿地縁辺");
            TryOpenObservedWaterDispute(progress, turn,
                "ur_marsh_draw", "ur_community",
                "ur_hinterland_farm", "ur_marsh_branch", 12,
                "河口湿地側");
        }

        static void TryOpenObservedWaterDispute(UrukCampaignProgress progress,
            int turn, string id, string claimantFactionId, string claimantFarmId,
            string claimantSegmentId, int earliestTurn, string positionJa)
        {
            if (turn < earliestTurn || FindDispute(progress, id) != null) return;
            var claimantFarm = FindFarm(progress, claimantFarmId);
            var claimantSegment = FindSegment(progress, claimantSegmentId);
            var respondentSegment = FindSegment(progress, "uruk_intake_segment");
            if (claimantFarm == null || claimantSegment == null ||
                respondentSegment == null) return;
            int deficit = Math.Max(0,
                claimantFarm.waterDemand - claimantFarm.waterReceived);
            bool claimantAtRisk = deficit > 0 || claimantSegment.condition < 45;
            bool respondentDrawingWater = respondentSegment.currentFlow >= 3;
            if (!claimantAtRisk || !respondentDrawingWater) return;

            string claimantName = FindFaction(progress, claimantFactionId)?.nameJa ??
                claimantFactionId;
            string cause = deficit > 0
                ? $"{claimantName}の農地で必要{claimantFarm.waterDemand}に対し" +
                  $"取水{claimantFarm.waterReceived}（不足{deficit}）を観測。"
                : $"{claimantName}の取水路が状態{claimantSegment.condition}%まで" +
                  "劣化し、次期の不足が懸念される。";
            cause += $"同じ期間にウルク取水路で流量" +
                $"{respondentSegment.currentFlow}を観測。{positionJa}との推定水系関係から" +
                "分水または補償を要求した。";
            var claimant = FindFaction(progress, claimantFactionId);
            var dispute = new UrukWaterDisputeState
            {
                id = id,
                claimantFactionId = claimantFactionId,
                respondentFactionId = HumanFactionId,
                claimantFarmId = claimantFarm.id,
                claimantSegmentId = claimantSegment.id,
                segmentId = respondentSegment.id,
                causeJa = cause,
                createdTurn = turn,
                claimantWaterDeficit = deficit,
                claimantCanalCondition = claimantSegment.condition,
                respondentFlowAtClaim = respondentSegment.currentFlow,
                basinId = "lower_alluvial_wetland_network",
                upstreamFactionId = HumanFactionId,
                downstreamFactionId = claimantFactionId,
                arbitratorFactionId = "",
                arbitrationReasonJa = "",
                trustAfter = claimant?.diplomaticTrust ?? 50,
                confidence = "inferred",
                retaliationJa = "",
                status = "open",
            };
            Append(ref progress.waterDisputes, dispute);
            if (string.IsNullOrWhiteSpace(progress.selectedWaterDisputeId))
                progress.selectedWaterDisputeId = dispute.id;
        }

        static void TryOpenObservedLandDispute(UrukCampaignProgress progress,
            int turn)
        {
            const string id = "eridu_uruk_west_plot_claim";
            if (FindLandDispute(progress, id) != null) return;
            var plot = FindFarm(progress, "uruk_west_farm");
            if (plot == null || plot.managerFactionId != HumanFactionId ||
                plot.crop == "fallow" || plot.waterReceived <= 0 ||
                plot.lastYield < 3)
                return;
            var claimant = FindFaction(progress, "eridu_community");
            var dispute = new UrukLandDisputeState
            {
                id = id,
                claimantFactionId = "eridu_community",
                respondentFactionId = HumanFactionId,
                plotId = plot.id,
                createdTurn = turn,
                observedYield = plot.lastYield,
                observedWater = plot.waterReceived,
                claimantBasisJa =
                    "湿地縁辺からの季節利用慣行があった可能性（推定）。",
                respondentBasisJa =
                    "ウルク共同体が当期に灌漑・播種・収穫を実施した観測記録。",
                arbitratorFactionId = "",
                arbitrationReasonJa = "",
                trustAfter = claimant?.diplomaticTrust ?? 50,
                retaliationJa = "",
                confidence = "inferred",
                status = "open",
                resultJa =
                    $"ウルク西農地で取水{plot.waterReceived}・収穫{plot.lastYield}を観測。" +
                    "エリドゥ共同体が季節利用を主張したが、紀元前4000年頃の境界は確定できない。",
            };
            Append(ref progress.landDisputes, dispute);
            progress.selectedLandDisputeId = dispute.id;
        }

        static void AdvanceLandAgreements(UrukCampaignProgress progress,
            int turn)
        {
            foreach (var dispute in progress.landDisputes)
            {
                if (dispute == null || dispute.agreementSettled ||
                    (dispute.status != "jointly_cultivated" &&
                     dispute.status != "mediated") ||
                    turn < dispute.agreementStartTurn)
                    continue;
                if (turn <= dispute.agreementUntilTurn)
                {
                    int due = dispute.yieldSharePerTurn;
                    dispute.yieldShareExpectedTotal += due;
                    int available = AvailableFactionGood(progress,
                        dispute.respondentFactionId, "barley");
                    int paid = Math.Min(due, available);
                    if (paid > 0)
                    {
                        ConsumeFactionGood(progress, dispute.respondentFactionId,
                            "barley", paid);
                        AddFactionGood(progress, dispute.claimantFactionId,
                            "barley", paid);
                        dispute.yieldSharedTotal += paid;
                    }
                }
                if (turn < dispute.agreementUntilTurn) continue;

                var plot = FindFarm(progress, dispute.plotId);
                RemoveFarmUser(plot, dispute.claimantFactionId);
                dispute.agreementSettled = true;
                bool fulfilled = dispute.yieldSharedTotal >=
                    dispute.yieldShareExpectedTotal;
                dispute.status = fulfilled ? "completed" : "defaulted";
                var claimant = FindFaction(progress, dispute.claimantFactionId);
                int trustDelta = fulfilled ? 4 : -10;
                int reputationDelta = fulfilled ? 2 : -7;
                if (claimant != null)
                {
                    claimant.diplomaticTrust = Math.Clamp(
                        claimant.diplomaticTrust + trustDelta, 0, 100);
                    dispute.trustAfter = claimant.diplomaticTrust;
                }
                string kind = dispute.resolutionKind == "seasonal_mediation"
                    ? "仲裁による季節利用" : "共同耕作";
                dispute.resultJa = fulfilled
                    ? $"{kind}を履行した（約束{dispute.yieldShareExpectedTotal}／" +
                      $"移転{dispute.yieldSharedTotal}）。利用権は当期で終了した。"
                    : $"{kind}を履行できなかった（約束{dispute.yieldShareExpectedTotal}／" +
                      $"移転{dispute.yieldSharedTotal}）。";
                RecordDiplomaticEvent(progress, turn, "land_dispute", dispute.id,
                    dispute.claimantFactionId,
                    fulfilled ? "land_agreement_completed" :
                        "land_agreement_defaulted",
                    reputationDelta, dispute.resultJa, dispute.confidence);
                if (!fulfilled) ApplyLandRetaliation(progress, dispute);
            }
        }

        static void ApplyWaterSharingAgreements(UrukCampaignProgress progress,
            int turn)
        {
            foreach (var dispute in progress.waterDisputes)
            {
                if (dispute == null ||
                    (dispute.resolutionKind != "share_water" &&
                     dispute.resolutionKind != "arbitrated_share") ||
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
                bool arbitrated = dispute.resolutionKind == "arbitrated_share";
                int trustDelta = fulfilled ? (arbitrated ? 3 : 4) : -8;
                int reputationDelta = fulfilled ? (arbitrated ? 1 : 2) : -6;
                if (claimant != null)
                {
                    claimant.diplomaticTrust = Math.Clamp(
                        claimant.diplomaticTrust + trustDelta, 0, 100);
                    dispute.trustAfter = claimant.diplomaticTrust;
                }
                string agreementName = arbitrated ? "仲裁分水" : "分水合意";
                dispute.resultJa = fulfilled
                    ? $"{agreementName}を履行した（必要{dispute.waterShareExpectedTotal}／" +
                      $"実施{dispute.waterSharedTotal}）。"
                    : $"{agreementName}を履行できなかった（必要{dispute.waterShareExpectedTotal}／" +
                      $"実施{dispute.waterSharedTotal}）。";
                RecordDiplomaticEvent(progress, turn, "water_dispute", dispute.id,
                    dispute.claimantFactionId,
                    arbitrated
                        ? fulfilled ? "water_arbitration_completed" :
                            "water_arbitration_defaulted"
                        : fulfilled ? "water_share_completed" :
                            "water_share_defaulted",
                    reputationDelta, dispute.resultJa, dispute.confidence);
                if (!fulfilled) ApplyWaterRetaliation(progress, dispute);
            }

            foreach (var dispute in progress.waterDisputes)
            {
                if (dispute == null ||
                    dispute.resolutionKind != "joint_maintenance" ||
                    dispute.status != "jointly_managed" ||
                    dispute.agreementSettled ||
                    turn < dispute.agreementUntilTurn)
                    continue;
                RemoveSegmentUser(progress, dispute.claimantSegmentId,
                    dispute.respondentFactionId);
                dispute.agreementSettled = true;
                dispute.status = "completed";
                var claimant = FindFaction(progress, dispute.claimantFactionId);
                if (claimant != null)
                {
                    claimant.diplomaticTrust = Math.Clamp(
                        claimant.diplomaticTrust + 3, 0, 100);
                    dispute.trustAfter = claimant.diplomaticTrust;
                }
                dispute.resultJa =
                    "4期間の共同管理を完了し、水路利用権を返還した。";
                RecordDiplomaticEvent(progress, turn, "water_dispute", dispute.id,
                    dispute.claimantFactionId, "joint_management_completed", 2,
                    dispute.resultJa, dispute.confidence);
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
            int risk = CalculateTransportRisk(definition, progress, id,
                origin, destination, turn);
            var dispatch = ActiveInformationForPair(progress, origin,
                destination, turn);
            int forecastMin = -1;
            int forecastMax = -1;
            if (dispatch != null)
            {
                int margin = InformationForecastMargin(dispatch.medium);
                forecastMin = Math.Clamp(risk - margin, 2, 35);
                forecastMax = Math.Clamp(risk + margin, 2, 35);
                dispatch.linkedTransportCount++;
            }
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
                informationDispatchId = dispatch?.id ?? "",
                forecastRiskMinPercent = forecastMin,
                forecastRiskMaxPercent = forecastMax,
                forecastConfidence = dispatch == null ? "" : "inferred",
                informationAssuranceJa = dispatch == null ? "" :
                    InformationAssuranceJa(dispatch.medium),
                termsExact = dispatch?.exactQuantities ?? false,
                path = new[]
                {
                    new HistoricalMapPoint { col = from.startCol, row = from.startRow },
                    new HistoricalMapPoint { col = to.startCol, row = to.startRow },
                },
            });
        }

        static int CalculateTransportRisk(HistoricalCampaignDefinition definition,
            UrukCampaignProgress progress, string id, string origin,
            string destination, int turn)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("輸送IDが空", nameof(id));
            int risk = 8 + StableHash(definition.seed, id, turn) % 13;
            if (HasActiveAccessRight(progress, origin, destination))
                risk = Math.Max(2, risk - 6);
            return Math.Clamp(risk + WaterRetaliationRiskPenalty(progress,
                origin, destination) + LandRetaliationRiskPenalty(progress,
                origin, destination) - KinshipTransportRiskReduction(progress,
                origin, destination) - CommunicationTransportRiskReduction(
                progress, origin, destination, turn), 2, 35);
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
            var dispute = SelectedOpenDispute(progress);
            if (dispute == null)
            {
                resultJa = "交渉可能な水利問題がない。";
                return false;
            }
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            string claimantName = claimant?.nameJa ?? dispute.claimantFactionId;
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
                        $"3期間、ウルク農地の取水から毎期2単位を{claimantName}の" +
                        "農地へ分けることで合意した。";
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
                        $"取水への補償として大麦2を{claimantName}へ引き渡した。";
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
                    AddSegmentUser(progress, dispute.claimantSegmentId,
                        dispute.respondentFactionId);
                    dispute.agreementStartTurn = turn;
                    dispute.agreementUntilTurn = turn + 3;
                    dispute.status = "jointly_managed";
                    dispute.agreementSettled = false;
                    dispute.resultJa =
                        "ウルクが葦1と水利労働10%を出して補修し、4期間の共同管理権を得た。";
                    trustDelta = 10;
                    reputationDelta = 4;
                    outcome = "joint_management_started";
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
            if (resolutionKind == "rejected")
                ApplyWaterRetaliation(progress, dispute);
            SelectFirstOpenWaterDispute(progress);
            resultJa = dispute.resultJa;
            return true;
        }

        static bool SelectNextWaterDispute(UrukCampaignProgress progress,
            out string resultJa)
        {
            var actionable = new List<UrukWaterDisputeState>();
            foreach (var dispute in progress.waterDisputes)
                if (IsActionableWaterCase(dispute)) actionable.Add(dispute);
            if (actionable.Count < 2)
            {
                resultJa = actionable.Count == 0
                    ? "操作できる水利対象がない。"
                    : "操作できる水利対象は1件だけ。";
                return false;
            }
            int current = -1;
            for (int i = 0; i < actionable.Count; i++)
                if (actionable[i].id == progress.selectedWaterDisputeId)
                    current = i;
            var next = actionable[(current + 1 + actionable.Count) %
                actionable.Count];
            progress.selectedWaterDisputeId = next.id;
            progress.regionalRevision++;
            string claimantName = FindFaction(progress,
                next.claimantFactionId)?.nameJa ?? next.claimantFactionId;
            resultJa = $"水利対象を{claimantName}の" +
                $"{WaterCaseKindJa(next)}へ切り替えた。";
            return true;
        }

        static bool ArbitrateOpenWaterDisputes(UrukCampaignProgress progress,
            int turn, out string resultJa)
        {
            var open = new List<UrukWaterDisputeState>();
            foreach (var dispute in progress.waterDisputes)
                if (dispute != null &&
                    dispute.respondentFactionId == HumanFactionId &&
                    dispute.status == "open") open.Add(dispute);
            if (open.Count < 2)
            {
                resultJa = "第三者仲裁には競合する水利要求が2件以上必要。";
                return false;
            }
            if (progress.diplomaticReputation < 25)
            {
                resultJa = "外交評判が低く、中立仲介役が裁定を引き受けない。";
                return false;
            }
            if (AvailableFactionGood(progress, HumanFactionId, "barley") < 1)
            {
                resultJa = "仲裁協議の共同食に大麦1が必要。";
                return false;
            }
            const string arbitratorId = "nippur_community";
            var arbitrator = FindFaction(progress, arbitratorId);
            if (arbitrator == null || arbitrator.diplomaticTrust < 35)
            {
                resultJa = "仲介可能な第三者共同体との信頼が不足している。";
                return false;
            }

            ConsumeFactionGood(progress, HumanFactionId, "barley", 1);
            AddFactionGood(progress, arbitratorId, "barley", 1);
            string reason = $"同時に{open.Count}件の分水要求があり、" +
                "直接合意だけでは同じ期の配分が競合するため。";
            foreach (var dispute in open)
            {
                dispute.resolutionKind = "arbitrated_share";
                dispute.status = "shared";
                dispute.agreementStartTurn = turn;
                dispute.agreementUntilTurn = turn + 1;
                dispute.waterSharePerTurn = 1;
                dispute.arbitratorFactionId = arbitratorId;
                dispute.arbitrationTurn = turn;
                dispute.arbitrationReasonJa = reason;
                dispute.resultJa =
                    "ニップール共同体の推定上の仲介により、2期間・毎期1単位の分水裁定を受け入れた。";
                var claimant = FindFaction(progress, dispute.claimantFactionId);
                if (claimant != null)
                {
                    claimant.diplomaticTrust = Math.Clamp(
                        claimant.diplomaticTrust + 6, 0, 100);
                    dispute.trustAfter = claimant.diplomaticTrust;
                }
                RecordDiplomaticEvent(progress, turn, "water_dispute", dispute.id,
                    dispute.claimantFactionId, "water_arbitration_award", 1,
                    dispute.resultJa, dispute.confidence);
            }
            arbitrator.currentGoalJa = "複数水利要求の仲裁";
            arbitrator.lastDecisionJa =
                "競合する要求を期限付きの少量分水へまとめた。";
            arbitrator.knownReasonJa = reason;
            progress.selectedWaterDisputeId = open[0].id;
            progress.regionalRevision++;
            resultJa = $"{open.Count}件の水利要求を第三者仲裁へ付し、" +
                "大麦1を共同食としてニップールへ渡した。";
            return true;
        }

        static bool BreachActiveWaterAgreement(UrukCampaignProgress progress,
            int turn, out string resultJa)
        {
            var dispute = SelectedActiveWaterAgreement(progress);
            if (dispute == null)
            {
                resultJa = "選択中の水利対象に破約できる合意がない。";
                return false;
            }
            if (dispute.status == "jointly_managed")
                RemoveSegmentUser(progress, dispute.claimantSegmentId,
                    dispute.respondentFactionId);
            dispute.status = "breached";
            dispute.agreementSettled = true;
            dispute.breached = true;
            dispute.resultJa =
                "ウルクが水利合意を一方的に破棄し、相手共同体が水路警備と輸送警戒を強めた。";
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            if (claimant != null)
            {
                claimant.diplomaticTrust = Math.Clamp(
                    claimant.diplomaticTrust - 18, 0, 100);
                dispute.trustAfter = claimant.diplomaticTrust;
            }
            RecordDiplomaticEvent(progress, turn, "water_dispute", dispute.id,
                dispute.claimantFactionId, "water_agreement_breached", -10,
                dispute.resultJa, dispute.confidence);
            ApplyWaterRetaliation(progress, dispute);
            progress.regionalRevision++;
            resultJa = dispute.resultJa;
            return true;
        }

        static bool RenegotiateWaterDispute(UrukCampaignProgress progress, int turn,
            out string resultJa)
        {
            var dispute = SelectedRecoverableWaterDispute(progress);
            if (dispute == null)
            {
                resultJa = "選択中の水利対象は再交渉できない。";
                return false;
            }
            if (AvailableFactionGood(progress, HumanFactionId, "barley") < 1 ||
                AvailableFactionGood(progress, HumanFactionId, "reeds") < 1)
            {
                resultJa = "再交渉の手土産に大麦1と葦1が必要。";
                return false;
            }
            ConsumeFactionGood(progress, HumanFactionId, "barley", 1);
            ConsumeFactionGood(progress, HumanFactionId, "reeds", 1);
            AddFactionGood(progress, dispute.claimantFactionId, "barley", 1);
            AddFactionGood(progress, dispute.claimantFactionId, "reeds", 1);
            EaseWaterRetaliation(progress, dispute);

            dispute.status = "open";
            dispute.resolutionKind = "";
            dispute.agreementStartTurn = 0;
            dispute.agreementUntilTurn = 0;
            dispute.waterSharePerTurn = 0;
            dispute.waterShareExpectedTotal = 0;
            dispute.waterSharedTotal = 0;
            dispute.barleyTransferred = 0;
            dispute.laborCommitted = 0;
            dispute.agreementSettled = false;
            dispute.breached = false;
            dispute.renegotiationCount++;
            dispute.retaliationJa = "";
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            if (claimant != null)
            {
                claimant.diplomaticTrust = Math.Clamp(
                    claimant.diplomaticTrust + 6, 0, 100);
                dispute.trustAfter = claimant.diplomaticTrust;
            }
            dispute.resultJa =
                "大麦1と葦1を手土産に協議を再開した。新しい分水・補償・共同管理案を選べる。";
            RecordDiplomaticEvent(progress, turn, "water_dispute", dispute.id,
                dispute.claimantFactionId, "water_renegotiated", 1,
                dispute.resultJa, dispute.confidence);
            progress.regionalRevision++;
            resultJa = dispute.resultJa;
            return true;
        }

        static bool ResolveOpenLandDispute(UrukCampaignProgress progress,
            int turn, string resolutionKind, out string resultJa)
        {
            var dispute = SelectedOpenLandDispute(progress);
            if (dispute == null)
            {
                resultJa = "判断待ちの土地・耕作権問題がない。";
                return false;
            }
            var plot = FindFarm(progress, dispute.plotId);
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            string claimantName = claimant?.nameJa ?? dispute.claimantFactionId;
            int trustDelta;
            int reputationDelta;
            string outcome;
            switch (resolutionKind)
            {
                case "joint_cultivation":
                    if (progress.labor.food < 40)
                    {
                        resultJa = "共同耕作には食料労働40%以上の配分が必要。";
                        return false;
                    }
                    AddFarmUser(plot, dispute.claimantFactionId);
                    dispute.laborCommitted = 10;
                    dispute.yieldSharePerTurn = 1;
                    dispute.agreementStartTurn = turn;
                    dispute.agreementUntilTurn = turn + 2;
                    dispute.status = "jointly_cultivated";
                    dispute.agreementSettled = false;
                    dispute.resultJa =
                        $"{claimantName}へ3期間の共同耕作権を認め、毎期大麦1を収穫配分する。";
                    trustDelta = 9;
                    reputationDelta = 3;
                    outcome = "land_joint_cultivation_started";
                    break;
                case "grain_compensation":
                    if (AvailableFactionGood(progress, HumanFactionId,
                        "barley") < 2)
                    {
                        resultJa = "耕作権補償に必要な大麦2がない。";
                        return false;
                    }
                    ConsumeFactionGood(progress, HumanFactionId, "barley", 2);
                    AddFactionGood(progress, dispute.claimantFactionId,
                        "barley", 2);
                    dispute.barleyTransferred = 2;
                    dispute.status = "compensated";
                    dispute.agreementSettled = true;
                    dispute.resultJa =
                        $"大麦2を{claimantName}へ移転し、当期の整備・播種実績に基づく管理を維持した。";
                    trustDelta = 7;
                    reputationDelta = 2;
                    outcome = "land_compensated";
                    break;
                case "seasonal_mediation":
                    if (progress.diplomaticReputation < 30)
                    {
                        resultJa = "第三者仲裁を依頼できる外交評判がない。";
                        return false;
                    }
                    if (AvailableFactionGood(progress, HumanFactionId,
                        "barley") < 1)
                    {
                        resultJa = "仲裁協議の共同食に大麦1が必要。";
                        return false;
                    }
                    const string arbitratorId = "nippur_community";
                    var arbitrator = FindFaction(progress, arbitratorId);
                    if (arbitrator == null || arbitrator.diplomaticTrust < 35)
                    {
                        resultJa = "仲介可能な第三者共同体との信頼が不足している。";
                        return false;
                    }
                    ConsumeFactionGood(progress, HumanFactionId, "barley", 1);
                    AddFactionGood(progress, arbitratorId, "barley", 1);
                    AddFarmUser(plot, dispute.claimantFactionId);
                    dispute.laborCommitted = 5;
                    dispute.yieldSharePerTurn = 1;
                    dispute.agreementStartTurn = turn;
                    dispute.agreementUntilTurn = turn + 3;
                    dispute.arbitratorFactionId = arbitratorId;
                    dispute.arbitrationTurn = turn;
                    dispute.arbitrationReasonJa =
                        "紀元前4000年頃の固定境界を確定できないため、灌漑・播種・収穫の観測と推定季節利用を両立する期限付き裁定。";
                    dispute.status = "mediated";
                    dispute.agreementSettled = false;
                    dispute.resultJa =
                        "ニップール共同体の推定上の仲介で4期間の季節利用を認め、毎期大麦1を配分する。";
                    arbitrator.currentGoalJa = "耕作権の第三者仲裁";
                    arbitrator.lastDecisionJa =
                        "恒久国境ではなく、期限付き季節利用を裁定した。";
                    arbitrator.knownReasonJa = dispute.arbitrationReasonJa;
                    trustDelta = 8;
                    reputationDelta = 4;
                    outcome = "land_mediation_award";
                    break;
                case "rejected":
                    dispute.status = "rejected";
                    dispute.agreementSettled = true;
                    dispute.resultJa =
                        "季節利用の主張を拒否した。相手共同体は境界農地と陸上輸送の警戒を強めた。";
                    trustDelta = -14;
                    reputationDelta = -5;
                    outcome = "land_rejected";
                    break;
                default:
                    resultJa = "不明な土地紛争解決案。";
                    return false;
            }
            dispute.resolutionKind = resolutionKind;
            if (claimant != null)
            {
                claimant.diplomaticTrust = Math.Clamp(
                    claimant.diplomaticTrust + trustDelta, 0, 100);
                dispute.trustAfter = claimant.diplomaticTrust;
            }
            RecordDiplomaticEvent(progress, turn, "land_dispute", dispute.id,
                dispute.claimantFactionId, outcome, reputationDelta,
                dispute.resultJa, dispute.confidence);
            if (resolutionKind == "rejected")
                ApplyLandRetaliation(progress, dispute);
            progress.selectedLandDisputeId = "";
            progress.regionalRevision++;
            resultJa = dispute.resultJa;
            return true;
        }

        static bool BreachActiveLandAgreement(UrukCampaignProgress progress,
            int turn, out string resultJa)
        {
            var dispute = FirstActiveLandAgreement(progress);
            if (dispute == null)
            {
                resultJa = "破約できる土地・耕作権合意がない。";
                return false;
            }
            RemoveFarmUser(FindFarm(progress, dispute.plotId),
                dispute.claimantFactionId);
            dispute.status = "breached";
            dispute.agreementSettled = true;
            dispute.breached = true;
            dispute.resultJa =
                "ウルクが共同耕作合意を破棄し、相手共同体が境界農地と輸送路の警戒を強めた。";
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            if (claimant != null)
            {
                claimant.diplomaticTrust = Math.Clamp(
                    claimant.diplomaticTrust - 18, 0, 100);
                dispute.trustAfter = claimant.diplomaticTrust;
            }
            RecordDiplomaticEvent(progress, turn, "land_dispute", dispute.id,
                dispute.claimantFactionId, "land_agreement_breached", -10,
                dispute.resultJa, dispute.confidence);
            ApplyLandRetaliation(progress, dispute);
            progress.regionalRevision++;
            resultJa = dispute.resultJa;
            return true;
        }

        static bool RenegotiateLandDispute(UrukCampaignProgress progress,
            int turn, out string resultJa)
        {
            var dispute = LatestRecoverableLandDispute(progress);
            if (dispute == null)
            {
                resultJa = "再交渉できる土地・耕作権問題がない。";
                return false;
            }
            if (AvailableFactionGood(progress, HumanFactionId, "barley") < 1 ||
                AvailableFactionGood(progress, HumanFactionId,
                    "alluvial_clay") < 1)
            {
                resultJa = "再交渉には共同食の大麦1と境界標用の粘土1が必要。";
                return false;
            }
            ConsumeFactionGood(progress, HumanFactionId, "barley", 1);
            ConsumeFactionGood(progress, HumanFactionId, "alluvial_clay", 1);
            AddFactionGood(progress, dispute.claimantFactionId, "barley", 1);
            AddFactionGood(progress, dispute.claimantFactionId,
                "alluvial_clay", 1);
            EaseLandRetaliation(progress, dispute);
            dispute.status = "open";
            dispute.resolutionKind = "";
            dispute.agreementStartTurn = 0;
            dispute.agreementUntilTurn = 0;
            dispute.yieldSharePerTurn = 0;
            dispute.yieldShareExpectedTotal = 0;
            dispute.yieldSharedTotal = 0;
            dispute.barleyTransferred = 0;
            dispute.laborCommitted = 0;
            dispute.arbitratorFactionId = "";
            dispute.arbitrationTurn = 0;
            dispute.arbitrationReasonJa = "";
            dispute.agreementSettled = false;
            dispute.breached = false;
            dispute.renegotiationCount++;
            dispute.retaliationJa = "";
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            if (claimant != null)
            {
                claimant.diplomaticTrust = Math.Clamp(
                    claimant.diplomaticTrust + 6, 0, 100);
                dispute.trustAfter = claimant.diplomaticTrust;
            }
            dispute.resultJa =
                "大麦1と粘土1を移転して協議を再開した。共同耕作・補償・仲裁を改めて選べる。";
            progress.selectedLandDisputeId = dispute.id;
            RecordDiplomaticEvent(progress, turn, "land_dispute", dispute.id,
                dispute.claimantFactionId, "land_renegotiated", 1,
                dispute.resultJa, dispute.confidence);
            progress.regionalRevision++;
            resultJa = dispute.resultJa;
            return true;
        }

        static void ApplyLandRetaliation(UrukCampaignProgress progress,
            UrukLandDisputeState dispute)
        {
            if (dispute == null || !string.IsNullOrWhiteSpace(dispute.retaliationJa))
                return;
            dispute.retaliationJa =
                "境界農地の警戒を増強。両勢力間の陸上輸送損失リスク+8%。";
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            if (claimant == null) return;
            ShiftLaborTowardMilitia(claimant.labor, 8);
            claimant.currentGoalJa = "耕作権の防衛";
            claimant.lastDecisionJa = "境界農地と陸上輸送路の警戒を強化した。";
            claimant.knownReasonJa = "土地合意の拒否・不履行・破約。";
        }

        static void EaseLandRetaliation(UrukCampaignProgress progress,
            UrukLandDisputeState dispute)
        {
            var claimant = FindFaction(progress, dispute?.claimantFactionId);
            if (claimant == null || string.IsNullOrWhiteSpace(dispute.retaliationJa))
                return;
            int restored = Math.Min(4, Math.Max(0, claimant.labor.militia));
            claimant.labor.militia -= restored;
            claimant.labor.trade += restored;
            claimant.currentGoalJa = GoalFor(claimant.aiArchetype);
            claimant.lastDecisionJa =
                "共同食と境界標素材を受け取り、耕作権協議を再開した。";
            claimant.knownReasonJa = "現物移転と再交渉の記録。";
        }

        static void ApplyWaterRetaliation(UrukCampaignProgress progress,
            UrukWaterDisputeState dispute)
        {
            if (dispute == null || !string.IsNullOrWhiteSpace(dispute.retaliationJa))
                return;
            dispute.retaliationJa =
                "水路警備を増強。両勢力間の輸送損失リスク+10%。";
            var claimant = FindFaction(progress, dispute.claimantFactionId);
            if (claimant == null) return;
            ShiftLaborTowardMilitia(claimant.labor, 10);
            claimant.currentGoalJa = "水利権の防衛";
            claimant.lastDecisionJa =
                "ウルクとの輸送警戒と水路警備を強化した。";
            claimant.knownReasonJa = "水利合意の拒否・不履行・破約。";
        }

        static void EaseWaterRetaliation(UrukCampaignProgress progress,
            UrukWaterDisputeState dispute)
        {
            var claimant = FindFaction(progress, dispute?.claimantFactionId);
            if (claimant == null || string.IsNullOrWhiteSpace(dispute.retaliationJa))
                return;
            int restored = Math.Min(5, Math.Max(0, claimant.labor.militia));
            claimant.labor.militia -= restored;
            claimant.labor.trade += restored;
            claimant.currentGoalJa = GoalFor(claimant.aiArchetype);
            claimant.lastDecisionJa = "手土産を受け取り、水利協議を再開した。";
            claimant.knownReasonJa = "現物移転と再交渉の記録。";
        }

        static void ShiftLaborTowardMilitia(UrukLaborAllocation labor, int amount)
        {
            if (labor == null || amount <= 0) return;
            int remaining = amount;
            remaining -= TakeLabor(ref labor.trade, remaining);
            remaining -= TakeLabor(ref labor.crafts, remaining);
            remaining -= TakeLabor(ref labor.construction, remaining);
            remaining -= TakeLabor(ref labor.food, remaining);
            labor.militia += amount - remaining;
        }

        static int TakeLabor(ref int source, int requested)
        {
            int taken = Math.Min(Math.Max(0, source), Math.Max(0, requested));
            source -= taken;
            return taken;
        }

        static bool HasWaterRetaliation(UrukCampaignProgress progress,
            string factionId)
        {
            if (progress?.waterDisputes == null) return false;
            foreach (var dispute in progress.waterDisputes)
                if (dispute != null && dispute.claimantFactionId == factionId &&
                    !string.IsNullOrWhiteSpace(dispute.retaliationJa))
                    return true;
            return false;
        }

        static bool HasLandRetaliation(UrukCampaignProgress progress,
            string factionId)
        {
            if (progress?.landDisputes == null) return false;
            foreach (var dispute in progress.landDisputes)
                if (dispute != null && dispute.claimantFactionId == factionId &&
                    !string.IsNullOrWhiteSpace(dispute.retaliationJa))
                    return true;
            return false;
        }

        static bool HasActiveLandAgreement(UrukCampaignProgress progress,
            string factionId)
        {
            if (progress?.landDisputes == null) return false;
            foreach (var dispute in progress.landDisputes)
                if (dispute != null && !dispute.agreementSettled &&
                    (dispute.status == "jointly_cultivated" ||
                     dispute.status == "mediated") &&
                    (dispute.claimantFactionId == factionId ||
                     dispute.arbitratorFactionId == factionId)) return true;
            return false;
        }

        static bool IsActiveLandArbitrator(UrukCampaignProgress progress,
            string factionId)
        {
            if (progress?.landDisputes == null) return false;
            foreach (var dispute in progress.landDisputes)
                if (dispute != null && !dispute.agreementSettled &&
                    dispute.status == "mediated" &&
                    dispute.arbitratorFactionId == factionId) return true;
            return false;
        }

        static bool HasActiveWaterArbitration(UrukCampaignProgress progress,
            string factionId)
        {
            if (progress?.waterDisputes == null) return false;
            foreach (var dispute in progress.waterDisputes)
                if (dispute != null && dispute.status == "shared" &&
                    dispute.resolutionKind == "arbitrated_share" &&
                    !dispute.agreementSettled &&
                    (dispute.claimantFactionId == factionId ||
                     dispute.arbitratorFactionId == factionId)) return true;
            return false;
        }

        static bool IsActiveWaterArbitrator(UrukCampaignProgress progress,
            string factionId)
        {
            if (progress?.waterDisputes == null) return false;
            foreach (var dispute in progress.waterDisputes)
                if (dispute != null && dispute.status == "shared" &&
                    dispute.resolutionKind == "arbitrated_share" &&
                    !dispute.agreementSettled &&
                    dispute.arbitratorFactionId == factionId) return true;
            return false;
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

        static UrukWaterDisputeState FindDispute(UrukCampaignProgress progress,
            string id)
        {
            if (progress?.waterDisputes == null || string.IsNullOrWhiteSpace(id))
                return null;
            foreach (var dispute in progress.waterDisputes)
                if (dispute != null && dispute.id == id) return dispute;
            return null;
        }

        static bool IsActiveWaterAgreement(UrukWaterDisputeState dispute)
        {
            return dispute != null && !dispute.agreementSettled &&
                (dispute.status == "shared" ||
                 dispute.status == "jointly_managed");
        }

        static bool IsRecoverableWaterDispute(UrukWaterDisputeState dispute)
        {
            return dispute != null &&
                (dispute.status == "rejected" ||
                 dispute.status == "breached" ||
                 dispute.status == "defaulted");
        }

        static bool IsActionableWaterCase(UrukWaterDisputeState dispute)
        {
            return dispute != null &&
                dispute.respondentFactionId == HumanFactionId &&
                (dispute.status == "open" ||
                 IsActiveWaterAgreement(dispute) ||
                 IsRecoverableWaterDispute(dispute));
        }

        static string WaterCaseKindJa(UrukWaterDisputeState dispute)
        {
            if (dispute == null) return "案件";
            if (dispute.status == "open") return "要求";
            if (IsActiveWaterAgreement(dispute)) return "履行中合意";
            if (IsRecoverableWaterDispute(dispute)) return "再交渉案件";
            return "案件";
        }

        static void NormalizeSelectedWaterCase(UrukCampaignProgress progress)
        {
            if (progress == null) return;
            var selected = FindDispute(progress,
                progress.selectedWaterDisputeId);
            if (IsActionableWaterCase(selected)) return;
            progress.selectedWaterDisputeId = "";
            if (progress.waterDisputes == null) return;
            foreach (var dispute in progress.waterDisputes)
                if (IsActionableWaterCase(dispute))
                {
                    progress.selectedWaterDisputeId = dispute.id;
                    return;
                }
        }

        static UrukWaterDisputeState FindOpenDispute(UrukCampaignProgress progress,
            string id)
        {
            var dispute = FindDispute(progress, id);
            return dispute != null && dispute.respondentFactionId == HumanFactionId &&
                dispute.status == "open" ? dispute : null;
        }

        static UrukLandDisputeState FindLandDispute(
            UrukCampaignProgress progress, string id)
        {
            if (progress?.landDisputes == null || string.IsNullOrWhiteSpace(id))
                return null;
            foreach (var dispute in progress.landDisputes)
                if (dispute != null && dispute.id == id) return dispute;
            return null;
        }

        static UrukLandDisputeState FindOpenLandDispute(
            UrukCampaignProgress progress, string id)
        {
            var dispute = FindLandDispute(progress, id);
            return dispute != null &&
                dispute.respondentFactionId == HumanFactionId &&
                dispute.status == "open" ? dispute : null;
        }

        static void AddFarmUser(UrukFarmPlotState farm, string factionId)
        {
            if (farm == null || string.IsNullOrWhiteSpace(factionId)) return;
            var users = new List<string>(farm.userFactionIds ??
                Array.Empty<string>());
            if (!users.Contains(factionId)) users.Add(factionId);
            farm.userFactionIds = users.ToArray();
        }

        static void RemoveFarmUser(UrukFarmPlotState farm, string factionId)
        {
            if (farm == null || string.IsNullOrWhiteSpace(factionId)) return;
            var users = new List<string>();
            foreach (string user in farm.userFactionIds ?? Array.Empty<string>())
                if (user != factionId) users.Add(user);
            if (!users.Contains(farm.ownerFactionId)) users.Insert(0,
                farm.ownerFactionId);
            farm.userFactionIds = users.ToArray();
        }

        static void SelectFirstOpenWaterDispute(UrukCampaignProgress progress)
        {
            var first = FirstOpenDispute(progress);
            if (first != null)
            {
                progress.selectedWaterDisputeId = first.id;
                return;
            }
            NormalizeSelectedWaterCase(progress);
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
