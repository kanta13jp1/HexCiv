using System;
using System.Diagnostics;
using HexCiv.Campaigns;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ウルク編の水・農地・AI台帳・輸送・移住・外交・親族連携・情報伝達を決定論的に検証する。
/// </summary>
public static class UrukRegionalSimulationSmokeTest
{
    [MenuItem("HexCiv/Run Uruk Regional Simulation Smoke Test")]
    public static void Run()
    {
        try
        {
            var definition = HistoricalCampaignRepository.LoadBuiltIn(
                HistoricalCampaignRepository.Uruk4000Id);
            ValidateInitialLedgers(definition);
            ValidateBrokenCanal(definition);
            ValidateContractSuite(definition);
            ValidateWaterDisputeChoices(definition);
            ValidateMultipleWaterArbitration(definition);
            ValidateLandRightsDispute(definition);
            ValidateKinshipDiplomacy(definition);
            ValidateInformationTransmission(definition);
            ValidatePlayerSequence(definition);
            ValidateThreeSeedDeterminism();
            UnityEngine.Debug.Log("URUK REGIONAL SIMULATION SMOKE OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(
                "URUK REGIONAL SIMULATION SMOKE FAIL: " + ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static void ValidateInitialLedgers(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var progress = session.Progress;
        Require(progress.version == 11, "進捗versionが11ではない");
        Require(progress.obligations != null, "契約債務台帳が初期化されていない");
        Require(progress.diplomaticRecords != null &&
            progress.diplomaticReputation == 50,
            "外交履歴または初期評判が初期化されていない");
        Require(progress.regionalFactions.Length == 8, "8勢力台帳がない");
        Require(progress.kinshipTies != null &&
            !string.IsNullOrWhiteSpace(progress.selectedKinshipFactionId),
            "親族連携台帳または初期候補が初期化されていない");
        Require(progress.informationDispatches != null &&
            progress.selectedInformationFactionId == "eridu_community" &&
            progress.selectedInformationMedium ==
                UrukRegionalSystem.OralMessageMedium,
            "情報伝達台帳・初期送信先・初期媒体が初期化されていない");
        Require(progress.farmPlots.Length == definition.farmPlots.Length,
            "農地定義が状態へ反映されていない");
        foreach (var farm in progress.farmPlots)
            Require(farm.managerFactionId == farm.ownerFactionId &&
                farm.userFactionIds.Length == 1 &&
                farm.userFactionIds[0] == farm.ownerFactionId,
                farm.id + "の初期管理権・利用権が不正");
        Require(progress.canalSegments.Length == definition.canalSegments.Length,
            "水路定義が状態へ反映されていない");
        for (int i = 0; i < progress.regionalFactions.Length; i++)
        {
            var faction = progress.regionalFactions[i];
            Require(faction.labor.Total == 100,
                faction.factionId + "の労働配分が100%ではない");
            Require(!string.IsNullOrWhiteSpace(faction.currentGoalJa),
                faction.factionId + "の目的がない");
            Require(!string.IsNullOrWhiteSpace(faction.aiArchetype),
                faction.factionId + "のAI類型がない");
        }
        UrukRegionalSystem.ResolveWaterForTest(progress);
        ValidateConservation(definition, progress);
    }

    static void ValidateBrokenCanal(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var progress = session.Progress;
        var intake = Segment(progress, "ur_marsh_intake");
        var middle = Segment(progress, "ur_marsh_branch");
        var farm = Farm(progress, "ur_hinterland_farm");
        intake.condition = 80;
        middle.condition = 0;
        UrukRegionalSystem.ResolveWaterForTest(progress);
        Require(farm.waterReceived == 0,
            "中間水路が破断しても下流農地へ水が届いた");
        middle.condition = 80;
        UrukRegionalSystem.ResolveWaterForTest(progress);
        Require(farm.waterReceived > 0,
            "中間水路を修復しても下流農地へ水が届かない");
        ValidateConservation(definition, progress);
    }

    static void ValidatePlayerSequence(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var progress = session.Progress;
        int clayBefore = UrukCampaignSystem.GoodAmount(progress,
            "alluvial_clay");
        int reedsBefore = UrukCampaignSystem.GoodAmount(progress, "reeds");

        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.CropBarleyAction, out _), "大麦作付けに失敗");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.PlanCanalAction, out _), "水路提案に失敗");
        Require(progress.reservedClay > 0 && progress.reservedReeds > 0,
            "水路資源が予約されていない");
        Require(UrukCampaignSystem.GoodAmount(progress, "alluvial_clay") ==
            clayBefore && UrukCampaignSystem.GoodAmount(progress, "reeds") ==
            reedsBefore, "確定前の計画が資源を消費した");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.CancelCanalPlanAction, out _), "水路取消に失敗");
        Require(progress.reservedClay == 0 && progress.reservedReeds == 0,
            "取消後も資源予約が残った");
        Require(UrukCampaignSystem.GoodAmount(progress, "alluvial_clay") ==
            clayBefore && UrukCampaignSystem.GoodAmount(progress, "reeds") ==
            reedsBefore, "取消で資源量が変化した");

        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.PlanCanalAction, out _), "水路再提案に失敗");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukCampaignSystem.MaintainCanalAction, out _), "取水路整備に失敗");

        int completedTurn = 0;
        while (completedTurn < 7 &&
            UrukRegionalSystem.HumanIrrigatedFarmCount(progress) == 0)
        {
            completedTurn++;
            Advance(session, completedTurn);
            ValidateConservation(definition, progress);
        }
        Require(UrukRegionalSystem.HumanIrrigatedFarmCount(progress) > 0,
            "7期以内に計画水路から農地へ通水できない");
        Require(progress.lastRegionalHumanYield > 0,
            "灌漑農地の収穫が発生していない");

        while (completedTurn < 6)
        {
            completedTurn++;
            Advance(session, completedTurn);
        }
        var offer = UrukRegionalSystem.FirstOpenOffer(progress);
        Require(offer != null && offer.contractKind == "barter",
            "第6期のエリドゥ交易提案がない");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.AcceptOfferAction, out _), "物々交換受諾に失敗");
        completedTurn++;
        Advance(session, completedTurn);
        Require(progress.transports.Length >= 2,
            "交換物資が実体輸送として生成されていない");
        bool reedBoat = false;
        foreach (var transport in progress.transports)
        {
            ValidateTransport(transport);
            if (transport.mode == "reed_boat") reedBoat = true;
        }
        Require(reedBoat, "水域交易に葦船が使われていない");

        while (completedTurn < 9)
        {
            completedTurn++;
            Advance(session, completedTurn);
            ValidateConservation(definition, progress);
        }
        Require(UrukRegionalSystem.FirstOpenDispute(progress) != null,
            "取水と請求側リスクを観測しても水利紛争が生成されていない");
        Require(UrukRegionalSystem.FirstWaitingMigration(progress) != null,
            "第9期の移住集団が生成されていない");
        int reputationBeforeNegotiation = progress.diplomaticReputation;
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.ShareWaterAction, out _), "分水合意に失敗");
        Require(progress.diplomaticReputation == reputationBeforeNegotiation + 5 &&
            HasDiplomaticOutcome(progress, "water_shared"),
            "分水合意が外交評判と履歴へ反映されない");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.AcceptMigrationAction, out _), "移住受入に失敗");
        var migration = progress.migrationGroups[0];
        completedTurn++;
        Advance(session, completedTurn);
        completedTurn++;
        Advance(session, completedTurn);
        completedTurn++;
        Advance(session, completedTurn);
        Require(migration.status == "settled" &&
            migration.departedPeople == migration.arrivedPeople,
            "移住者数の保存則に違反");
        Require(progress.waterDisputes[0].agreementSettled &&
            progress.waterDisputes[0].status == "completed",
            "分水合意が3期間後に決済されない");

        string save = HistoricalCampaignSave.Serialize(session);
        var loaded = HistoricalCampaignSave.Deserialize(save,
            id => id == definition.id ? definition : null);
        Require(JsonUtility.ToJson(session.Progress) ==
            JsonUtility.ToJson(loaded.Progress),
            "地域状態のセーブ往復が一致しない");
        ValidateConservation(definition, loaded.Progress);
    }

    static void ValidateContractSuite(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var progress = session.Progress;
        SetGood(progress, "uruk_community", "barley", 20);
        SetGood(progress, "uruk_community", "reeds", 8);
        SetGood(progress, "eridu_community", "barley", 20);
        progress.labor.food = 50;
        progress.labor.trade = 10;

        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.RequestLoanAction, out _), "貸付契約の作成に失敗");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.OfferLaborAction, out _), "労務契約の作成に失敗");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.AcquireAccessAction, out _), "通行権契約の作成に失敗");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.OfferTributeAction, out _), "朝貢契約の作成に失敗");

        Advance(session, 1);
        Require(progress.obligations.Length == 4,
            "4種の契約債務が生成されていない");
        Require(CountDiplomaticOutcome(progress, "agreed") == 4,
            "4種の契約合意が外交履歴へ記録されていない");
        Require(HasSegmentUser(progress, "eridu_wetland_intake",
            "uruk_community"), "契約後も水路通行権が付与されていない");

        for (int turn = 2; turn <= 12; turn++)
        {
            if (UrukCampaignSystem.GoodAmount(progress, "barley") < 8)
                SetGood(progress, "uruk_community", "barley", 20);
            Advance(session, turn);
            ValidateConservation(definition, progress);
        }

        Require(Obligation(progress, "loan_repayment").status == "completed",
            "貸付の現物返済が完了していない");
        Require(Obligation(progress, "labor_service").status == "completed",
            "労務契約が完了していない");
        Require(Obligation(progress, "access_right").status == "expired",
            "通行権が期限満了していない");
        Require(!HasSegmentUser(progress, "eridu_wetland_intake",
            "uruk_community"), "期限後も水路通行権が残っている");
        Require(Obligation(progress, "tribute").status == "completed" &&
            Obligation(progress, "tribute").remainingInstallments == 0,
            "3回の朝貢が完了していない");
        Require(progress.diplomaticReputation == 63,
            "契約履行が外交評判へ正しく加算されていない");
        Require(HasDiplomaticOutcome(progress, "completed") &&
            HasDiplomaticOutcome(progress, "expired"),
            "契約履行・期限満了が外交履歴へ記録されていない");

        string save = HistoricalCampaignSave.Serialize(session);
        var loaded = HistoricalCampaignSave.Deserialize(save,
            id => id == definition.id ? definition : null);
        Require(JsonUtility.ToJson(progress) == JsonUtility.ToJson(loaded.Progress),
            "契約台帳のセーブ往復が一致しない");

        var defaultSession = HistoricalCampaignFactory.Build(definition);
        SetGood(defaultSession.Progress, "uruk_community", "barley", 1);
        Require(UrukCampaignSystem.TryApplyAction(defaultSession,
            UrukRegionalSystem.OfferTributeAction, out _), "不履行試験の契約作成に失敗");
        Advance(defaultSession, 1);
        SetGood(defaultSession.Progress, "uruk_community", "barley", 0);
        Advance(defaultSession, 2);
        Require(Obligation(defaultSession.Progress, "tribute").status == "defaulted",
            "物資不足でも朝貢が不履行にならない");
        Require(defaultSession.Progress.diplomaticReputation == 38 &&
            HasDiplomaticOutcome(defaultSession.Progress, "defaulted"),
            "契約不履行が評判低下と履歴へ反映されない");

        defaultSession.Progress.diplomaticReputation = 24;
        Require(!UrukCampaignSystem.TryApplyAction(defaultSession,
            UrukRegionalSystem.RequestLoanAction, out _),
            "低評判でも新規貸付を受けられる");
        defaultSession.Progress.diplomaticReputation = 29;
        Require(!UrukCampaignSystem.TryApplyAction(defaultSession,
            UrukRegionalSystem.AcquireAccessAction, out _),
            "低評判でも水路通行権を得られる");

        var migrated = HistoricalCampaignFactory.Build(definition).Progress;
        migrated.version = 5;
        migrated.waterDisputes = new[]
        {
            new UrukWaterDisputeState
            {
                id = "legacy_dispute",
                claimantFactionId = "lagash_region",
                respondentFactionId = "uruk_community",
                segmentId = "uruk_intake_segment",
                causeJa = "旧版の水利問題",
                status = "negotiated",
            },
        };
        UrukCampaignSystem.MigrateProgress(definition, migrated);
        Require(migrated.version == 11 &&
            migrated.waterDisputes[0].claimantFarmId ==
                "lagash_hinterland_farm" &&
            migrated.waterDisputes[0].resolutionKind == "joint_maintenance" &&
            migrated.waterDisputes[0].agreementSettled &&
            migrated.waterDisputes[0].retaliationJa == "",
            "version 5から水利紛争の原因・解決状態を補完できない");

        var v6Migration = HistoricalCampaignFactory.Build(definition).Progress;
        v6Migration.version = 6;
        v6Migration.waterDisputes = new[]
        {
            new UrukWaterDisputeState
            {
                id = "v6_dispute",
                claimantFactionId = "lagash_region",
                respondentFactionId = "uruk_community",
                status = "rejected",
                retaliationJa = null,
                renegotiationCount = -1,
            },
        };
        UrukCampaignSystem.MigrateProgress(definition, v6Migration);
        Require(v6Migration.version == 11 &&
            v6Migration.waterDisputes[0].retaliationJa == "" &&
            v6Migration.waterDisputes[0].renegotiationCount == 0,
            "version 6から報復・再交渉状態を補完できない");

        var v7Migration = HistoricalCampaignFactory.Build(definition).Progress;
        v7Migration.version = 7;
        v7Migration.selectedWaterDisputeId = null;
        v7Migration.waterDisputes = new[]
        {
            new UrukWaterDisputeState
            {
                id = "v7_dispute",
                claimantFactionId = "lagash_region",
                respondentFactionId = "uruk_community",
                claimantFarmId = "lagash_hinterland_farm",
                claimantSegmentId = "lagash_tigris_branch",
                segmentId = "uruk_intake_segment",
                status = "open",
                confidence = "inferred",
                retaliationJa = "",
            },
        };
        UrukCampaignSystem.MigrateProgress(definition, v7Migration);
        Require(v7Migration.version == 11 &&
            v7Migration.waterDisputes[0].basinId ==
                "lower_alluvial_wetland_network" &&
            v7Migration.waterDisputes[0].upstreamFactionId ==
                "uruk_community" &&
            v7Migration.waterDisputes[0].downstreamFactionId ==
                "lagash_region" &&
            v7Migration.waterDisputes[0].arbitratorFactionId == "" &&
            v7Migration.selectedWaterDisputeId == "v7_dispute",
            "version 7から水系関係・仲裁・選択案件を補完できない");

        var v8Migration = HistoricalCampaignFactory.Build(definition).Progress;
        v8Migration.version = 8;
        v8Migration.landDisputes = null;
        v8Migration.selectedLandDisputeId = null;
        foreach (var farm in v8Migration.farmPlots)
        {
            farm.managerFactionId = null;
            farm.userFactionIds = null;
        }
        UrukCampaignSystem.MigrateProgress(definition, v8Migration);
        Require(v8Migration.version == 11 &&
            v8Migration.landDisputes != null &&
            v8Migration.selectedLandDisputeId == "",
            "version 8から土地紛争台帳を補完できない");
        foreach (var farm in v8Migration.farmPlots)
            Require(farm.managerFactionId == farm.ownerFactionId &&
                farm.userFactionIds.Length == 1 &&
                farm.userFactionIds[0] == farm.ownerFactionId,
                farm.id + "の旧セーブ農地権利を補完できない");

        var v9Migration = HistoricalCampaignFactory.Build(definition).Progress;
        v9Migration.version = 9;
        v9Migration.kinshipTies = null;
        v9Migration.selectedKinshipFactionId = null;
        UrukCampaignSystem.MigrateProgress(definition, v9Migration);
        Require(v9Migration.version == 11 &&
            v9Migration.kinshipTies != null &&
            v9Migration.selectedKinshipFactionId == "eridu_community",
            "version 9から親族連携台帳・候補を補完できない");

        var v10Migration = HistoricalCampaignFactory.Build(definition).Progress;
        v10Migration.version = 10;
        v10Migration.informationDispatches = null;
        v10Migration.selectedInformationFactionId = null;
        v10Migration.selectedInformationMedium = null;
        UrukCampaignSystem.MigrateProgress(definition, v10Migration);
        Require(v10Migration.version == 11 &&
            v10Migration.informationDispatches != null &&
            v10Migration.selectedInformationFactionId == "eridu_community" &&
            v10Migration.selectedInformationMedium ==
                UrukRegionalSystem.OralMessageMedium,
            "version 10から情報伝達台帳・送信先・媒体を補完できない");

        var chainedMigration = HistoricalCampaignFactory.Build(definition).Progress;
        chainedMigration.version = 3;
        chainedMigration.obligations = null;
        chainedMigration.diplomaticRecords = null;
        UrukCampaignSystem.MigrateProgress(definition, chainedMigration);
        Require(chainedMigration.version == 11 &&
            chainedMigration.obligations != null &&
            chainedMigration.diplomaticRecords != null,
            "version 3から現行versionへ連続移行できない");

        var aiSession = HistoricalCampaignFactory.Build(definition);
        SetGood(aiSession.Progress, "lagash_region", "barley", 0);
        SetGood(aiSession.Progress, "lagash_region", "emmer_wheat", 0);
        SetGood(aiSession.Progress, "lagash_region", "fish", 0);
        foreach (var farm in aiSession.Progress.farmPlots)
            if (farm.ownerFactionId == "lagash_region") farm.crop = "fallow";
        Advance(aiSession, 1);
        bool aiLoan = false;
        foreach (var offer in aiSession.Progress.tradeOffers)
            if (offer.contractKind == "loan" &&
                offer.receiverFactionId == "lagash_region") aiLoan = true;
        Require(aiLoan, "食料不足AIが余剰勢力へ現物貸付を求めない");
    }

    static void ValidateWaterDisputeChoices(
        HistoricalCampaignDefinition definition)
    {
        var shareSession = PreparedWaterDispute(definition);
        var share = shareSession.Progress.waterDisputes[0];
        Require(share.claimantWaterDeficit > 0 &&
            share.respondentFlowAtClaim > 0 &&
            share.claimantCanalCondition == 0,
            "水路・農地の実測が水利紛争原因へ保存されていない");
        Require(UrukCampaignSystem.TryApplyAction(shareSession,
            UrukRegionalSystem.ShareWaterAction, out _), "分水案の採用に失敗");
        for (int turn = 9; turn <= 11; turn++)
        {
            Advance(shareSession, turn);
            ValidateConservation(definition, shareSession.Progress);
        }
        Require(share.status == "completed" && share.agreementSettled &&
            share.waterShareExpectedTotal > 0 &&
            share.waterSharedTotal == share.waterShareExpectedTotal &&
            shareSession.Progress.diplomaticReputation == 57 &&
            HasDiplomaticOutcome(shareSession.Progress,
                "water_share_completed"),
            $"3期間の分水履行と長期評判が一致しない: status={share.status}, " +
            $"settled={share.agreementSettled}, expected=" +
            $"{share.waterShareExpectedTotal}, shared={share.waterSharedTotal}, " +
            $"reputation={shareSession.Progress.diplomaticReputation}");

        var defaultShareSession = PreparedWaterDispute(definition);
        var defaultShare = defaultShareSession.Progress.waterDisputes[0];
        Require(UrukCampaignSystem.TryApplyAction(defaultShareSession,
            UrukRegionalSystem.ShareWaterAction, out _),
            "不履行試験の分水合意に失敗");
        Segment(defaultShareSession.Progress,
            "uruk_intake_segment").condition = 0;
        Segment(defaultShareSession.Progress,
            "uruk_north_branch").condition = 0;
        for (int turn = 9; turn <= 11; turn++)
            Advance(defaultShareSession, turn);
        Require(defaultShare.status == "defaulted" &&
            defaultShare.waterShareExpectedTotal > 0 &&
            defaultShare.waterSharedTotal == 0 &&
            defaultShareSession.Progress.diplomaticReputation == 49 &&
            !string.IsNullOrWhiteSpace(defaultShare.retaliationJa) &&
            UrukRegionalSystem.WaterRetaliationRiskPenalty(
                defaultShareSession.Progress, "uruk_community",
                "lagash_region") == 10 &&
            HasDiplomaticOutcome(defaultShareSession.Progress,
                "water_share_defaulted"),
            "分水不能時に不履行・信頼低下が記録されない");

        var compensationSession = PreparedWaterDispute(definition);
        SetGood(compensationSession.Progress, "uruk_community", "barley", 5);
        int claimantBarley = FactionGood(compensationSession.Progress,
            "lagash_region", "barley");
        Require(UrukCampaignSystem.TryApplyAction(compensationSession,
            UrukRegionalSystem.CompensateWaterAction, out _),
            "穀物補償案の採用に失敗");
        Require(UrukCampaignSystem.GoodAmount(compensationSession.Progress,
                "barley") == 3 &&
            FactionGood(compensationSession.Progress, "lagash_region",
                "barley") == claimantBarley + 2 &&
            compensationSession.Progress.waterDisputes[0].barleyTransferred == 2 &&
            compensationSession.Progress.diplomaticReputation == 53,
            "穀物補償が双方の実在備蓄と評判へ反映されない");

        var repairSession = PreparedWaterDispute(definition);
        SetGood(repairSession.Progress, "uruk_community", "reeds", 3);
        repairSession.Progress.labor.canal = 20;
        repairSession.Progress.labor.food -=
            repairSession.Progress.labor.Total - 100;
        var lagashBranch = Segment(repairSession.Progress,
            "lagash_tigris_branch");
        int conditionBefore = lagashBranch.condition;
        Require(UrukCampaignSystem.TryApplyAction(repairSession,
            UrukRegionalSystem.NegotiateWaterAction, out _),
            "共同補修案の採用に失敗");
        Require(lagashBranch.condition == conditionBefore + 15 &&
            repairSession.Progress.waterDisputes[0].laborCommitted == 10 &&
            UrukCampaignSystem.GoodAmount(repairSession.Progress, "reeds") == 2 &&
            repairSession.Progress.diplomaticReputation == 54 &&
            repairSession.Progress.waterDisputes[0].status ==
                "jointly_managed" &&
            HasSegmentUser(repairSession.Progress, "lagash_tigris_branch",
                "uruk_community"),
            "共同管理が水路・利用権・資材・労働・評判へ反映されない");
        for (int turn = 9; turn <= 12; turn++) Advance(repairSession, turn);
        Require(repairSession.Progress.waterDisputes[0].status == "completed" &&
            repairSession.Progress.waterDisputes[0].agreementSettled &&
            !HasSegmentUser(repairSession.Progress, "lagash_tigris_branch",
                "uruk_community") &&
            repairSession.Progress.diplomaticReputation == 56 &&
            HasDiplomaticOutcome(repairSession.Progress,
                "joint_management_completed"),
            "共同管理の期限終了・利用権返還・評判が一致しない");

        var breachSession = PreparedWaterDispute(definition);
        var breachFaction = UrukRegionalSystem.FindFaction(
            breachSession.Progress, "lagash_region");
        int militiaBeforeBreach = breachFaction.labor.militia;
        Require(UrukCampaignSystem.TryApplyAction(breachSession,
            UrukRegionalSystem.ShareWaterAction, out _), "破約試験の分水合意に失敗");
        Require(UrukCampaignSystem.TryApplyAction(breachSession,
            UrukRegionalSystem.BreachWaterAgreementAction, out _),
            "水利合意の破約に失敗");
        var breached = breachSession.Progress.waterDisputes[0];
        Require(breached.status == "breached" && breached.breached &&
            breached.agreementSettled &&
            !string.IsNullOrWhiteSpace(breached.retaliationJa) &&
            breachFaction.labor.militia == militiaBeforeBreach + 10 &&
            breachFaction.labor.Total == 100 &&
            breachSession.Progress.diplomaticReputation == 45 &&
            UrukRegionalSystem.WaterRetaliationRiskPenalty(
                breachSession.Progress, "uruk_community", "lagash_region") == 10 &&
            HasDiplomaticOutcome(breachSession.Progress,
                "water_agreement_breached"),
            "破約が報復AI・輸送危険度・評判へ反映されない");

        var rejectSession = PreparedWaterDispute(definition);
        var rejectFaction = UrukRegionalSystem.FindFaction(
            rejectSession.Progress, "lagash_region");
        int militiaBeforeReject = rejectFaction.labor.militia;
        Require(UrukCampaignSystem.TryApplyAction(rejectSession,
            UrukRegionalSystem.RejectWaterAction, out _), "水利要求の拒否に失敗");
        Require(rejectSession.Progress.diplomaticReputation == 46 &&
            rejectFaction.labor.militia == militiaBeforeReject + 10 &&
            !string.IsNullOrWhiteSpace(
                rejectSession.Progress.waterDisputes[0].retaliationJa) &&
            HasDiplomaticOutcome(rejectSession.Progress, "rejected"),
            "水利要求拒否が外交評判と履歴へ反映されない");

        SetGood(rejectSession.Progress, "uruk_community", "barley", 3);
        SetGood(rejectSession.Progress, "uruk_community", "reeds", 3);
        int claimantBarleyBefore = FactionGood(rejectSession.Progress,
            "lagash_region", "barley");
        int claimantReedsBefore = FactionGood(rejectSession.Progress,
            "lagash_region", "reeds");
        Require(UrukCampaignSystem.TryApplyAction(rejectSession,
            UrukRegionalSystem.RenegotiateWaterAction, out _),
            "水利再交渉の開始に失敗");
        var renegotiated = rejectSession.Progress.waterDisputes[0];
        Require(renegotiated.status == "open" &&
            !renegotiated.agreementSettled &&
            renegotiated.renegotiationCount == 1 &&
            renegotiated.retaliationJa == "" &&
            rejectFaction.labor.militia == militiaBeforeReject + 5 &&
            rejectFaction.labor.Total == 100 &&
            UrukCampaignSystem.GoodAmount(rejectSession.Progress, "barley") == 2 &&
            UrukCampaignSystem.GoodAmount(rejectSession.Progress, "reeds") == 2 &&
            FactionGood(rejectSession.Progress, "lagash_region", "barley") ==
                claimantBarleyBefore + 1 &&
            FactionGood(rejectSession.Progress, "lagash_region", "reeds") ==
                claimantReedsBefore + 1 &&
            rejectSession.Progress.diplomaticReputation == 47 &&
            UrukRegionalSystem.WaterRetaliationRiskPenalty(
                rejectSession.Progress, "uruk_community", "lagash_region") == 0 &&
            HasDiplomaticOutcome(rejectSession.Progress, "water_renegotiated"),
            "再交渉が現物移転・報復緩和・再選択・評判へ反映されない");
    }

    static void ValidateMultipleWaterArbitration(
        HistoricalCampaignDefinition definition)
    {
        var session = PreparedMultipleWaterDisputes(definition);
        var progress = session.Progress;
        Require(UrukRegionalSystem.OpenWaterDisputeCount(progress) == 3,
            "異なる地理条件から複数の水利要求が同時生成されない");
        var first = UrukRegionalSystem.SelectedOpenDispute(progress);
        Require(first != null && first.upstreamFactionId == "uruk_community" &&
            first.downstreamFactionId == first.claimantFactionId &&
            first.basinId == "lower_alluvial_wetland_network" &&
            first.confidence == "inferred",
            "上下流関係・推定水系・確度が水利案件へ保存されていない");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.NextWaterDisputeAction, out _),
            "複数水利案件の表示切替に失敗");
        Require(UrukRegionalSystem.SelectedOpenDispute(progress)?.id != first.id,
            "水利案件を切り替えても選択が変わらない");

        SetGood(progress, "uruk_community", "barley", 5);
        int nippurBarley = FactionGood(progress, "nippur_community", "barley");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.ArbitrateWaterDisputesAction, out _),
            "競合する水利要求の第三者仲裁に失敗");
        Require(UrukRegionalSystem.OpenWaterDisputeCount(progress) == 0 &&
            progress.selectedWaterDisputeId == progress.waterDisputes[0].id &&
            UrukRegionalSystem.SelectedActiveWaterAgreement(progress) ==
                progress.waterDisputes[0] &&
            UrukRegionalSystem.ActionableWaterCaseCount(progress) == 3 &&
            UrukCampaignSystem.GoodAmount(progress, "barley") == 4 &&
            FactionGood(progress, "nippur_community", "barley") ==
                nippurBarley + 1 && progress.diplomaticReputation == 53,
            "仲裁が案件選択・現物共同食・外交評判へ反映されない");
        foreach (var dispute in progress.waterDisputes)
            Require(dispute.resolutionKind == "arbitrated_share" &&
                dispute.status == "shared" &&
                dispute.arbitratorFactionId == "nippur_community" &&
                dispute.arbitrationTurn == 13 &&
                !string.IsNullOrWhiteSpace(dispute.arbitrationReasonJa) &&
                dispute.waterSharePerTurn == 1,
                dispute.id + "に第三者裁定と期限付き分水が保存されない");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.NextWaterDisputeAction, out _),
            "履行中の水利合意を切り替えられない");
        Require(UrukRegionalSystem.SelectedActiveWaterAgreement(progress) ==
                progress.waterDisputes[1] &&
            UrukRegionalSystem.SelectedWaterCaseOrdinal(progress) == 2,
            "水利対象切替が選択中の履行合意へ反映されない");
        Require(CountDiplomaticOutcome(progress,
                "water_arbitration_award") == 3,
            "各請求側への仲裁裁定が外交履歴へ残らない");

        Advance(session, 13);
        var nippur = UrukRegionalSystem.FindFaction(progress, "nippur_community");
        Require(nippur.currentGoalJa == "複数水利要求の仲裁",
            "仲介勢力AIが仲裁の履行を目的として保持しない");
        Advance(session, 14);
        foreach (var dispute in progress.waterDisputes)
            Require(dispute.status == "completed" && dispute.agreementSettled &&
                dispute.waterShareExpectedTotal > 0 &&
                dispute.waterSharedTotal == dispute.waterShareExpectedTotal,
                dispute.id + "の仲裁分水が物理水量で履行されない");
        Require(CountDiplomaticOutcome(progress,
                "water_arbitration_completed") == 3 &&
            progress.diplomaticReputation == 56 &&
            progress.selectedWaterDisputeId == "",
            "仲裁分水の完了と長期評判が一致しない");
        ValidateConservation(definition, progress);

        string save = HistoricalCampaignSave.Serialize(session);
        var loaded = HistoricalCampaignSave.Deserialize(save,
            id => id == definition.id ? definition : null);
        Require(JsonUtility.ToJson(progress) == JsonUtility.ToJson(loaded.Progress),
            "複数水利案件・仲裁状態のセーブ往復が一致しない");

        ValidateSelectedWaterAgreementActions(definition);
    }

    static void ValidateSelectedWaterAgreementActions(
        HistoricalCampaignDefinition definition)
    {
        var session = PreparedMultipleWaterDisputes(definition);
        var progress = session.Progress;
        SetGood(progress, "uruk_community", "barley", 10);
        SetGood(progress, "uruk_community", "reeds", 10);
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.ArbitrateWaterDisputesAction, out _),
            "個別操作試験の第三者仲裁に失敗");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.NextWaterDisputeAction, out _),
            "個別操作試験の履行中合意切替に失敗");

        var first = progress.waterDisputes[0];
        var selected = progress.waterDisputes[1];
        var third = progress.waterDisputes[2];
        string selectedId = selected.id;
        int claimantBarley = FactionGood(progress,
            selected.claimantFactionId, "barley");
        int claimantReeds = FactionGood(progress,
            selected.claimantFactionId, "reeds");
        Require(UrukRegionalSystem.SelectedActiveWaterAgreement(progress) ==
                selected &&
            UrukCampaignSystem.TryApplyAction(session,
                UrukRegionalSystem.BreachWaterAgreementAction, out _),
            "選択中の履行合意を破約できない");
        Require(selected.status == "breached" && first.status == "shared" &&
            third.status == "shared" &&
            UrukRegionalSystem.SelectedRecoverableWaterDispute(progress) ==
                selected,
            "破約が選択外の水利合意へ誤適用された");

        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.RenegotiateWaterAction, out _),
            "選択中の破約案件を再交渉できない");
        Require(selected.status == "open" &&
            progress.selectedWaterDisputeId == selectedId &&
            FactionGood(progress, selected.claimantFactionId, "barley") ==
                claimantBarley + 1 &&
            FactionGood(progress, selected.claimantFactionId, "reeds") ==
                claimantReeds + 1 &&
            first.status == "shared" && third.status == "shared",
            "再交渉が選択相手だけへの現物移転・再開にならない");
        Require(!UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.BreachWaterAgreementAction, out _),
            "未決案件の表示中に選択外の履行合意を破約した");

        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.NextWaterDisputeAction, out _),
            "未決案件から次の履行合意へ切り替えられない");
        Require(UrukRegionalSystem.SelectedActiveWaterAgreement(progress) ==
                third &&
            UrukCampaignSystem.TryApplyAction(session,
                UrukRegionalSystem.BreachWaterAgreementAction, out _),
            "切替先の履行合意を破約できない");
        Require(third.status == "breached" && first.status == "shared" &&
            selected.status == "open",
            "切替先以外の水利対象が破約で変化した");

        string save = HistoricalCampaignSave.Serialize(session);
        var loaded = HistoricalCampaignSave.Deserialize(save,
            id => id == definition.id ? definition : null);
        Require(JsonUtility.ToJson(progress) == JsonUtility.ToJson(loaded.Progress) &&
            UrukRegionalSystem.SelectedRecoverableWaterDispute(
                loaded.Progress)?.id == third.id,
            "個別水利対象と破約・再交渉状態のセーブ往復が一致しない");
    }

    static HistoricalCampaignSession PreparedMultipleWaterDisputes(
        HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var progress = session.Progress;
        var intake = Segment(progress, "uruk_intake_segment");
        var branch = Segment(progress, "uruk_north_branch");
        intake.condition = 80;
        branch.completed = true;
        branch.condition = 80;
        Segment(progress, "lagash_tigris_branch").condition = 0;
        Segment(progress, "eridu_wetland_intake").condition = 0;
        Segment(progress, "ur_marsh_branch").condition = 0;
        Advance(session, 12);
        return session;
    }

    static void ValidateLandRightsDispute(
        HistoricalCampaignDefinition definition)
    {
        var compensationSession = PreparedLandDispute(definition);
        var compensation =
            UrukRegionalSystem.SelectedOpenLandDispute(
                compensationSession.Progress);
        Require(compensation != null &&
            compensation.plotId == "uruk_west_farm" &&
            compensation.claimantFactionId == "eridu_community" &&
            compensation.respondentFactionId == "uruk_community" &&
            compensation.observedYield >= 3 &&
            compensation.observedWater > 0 &&
            compensation.confidence == "inferred" &&
            !string.IsNullOrWhiteSpace(compensation.claimantBasisJa) &&
            !string.IsNullOrWhiteSpace(compensation.respondentBasisJa),
            "灌漑・収穫の観測から推定土地紛争が生成されない");
        SetGood(compensationSession.Progress, "uruk_community", "barley", 5);
        int eriduBarley = FactionGood(compensationSession.Progress,
            "eridu_community", "barley");
        Require(UrukCampaignSystem.TryApplyAction(compensationSession,
            UrukRegionalSystem.CompensateLandAction, out _),
            "土地利用の現物補償に失敗");
        Require(compensation.status == "compensated" &&
            compensation.barleyTransferred == 2 &&
            compensationSession.Progress.diplomaticReputation == 52 &&
            UrukCampaignSystem.GoodAmount(compensationSession.Progress,
                "barley") == 3 &&
            FactionGood(compensationSession.Progress, "eridu_community",
                "barley") == eriduBarley + 2 &&
            HasDiplomaticOutcome(compensationSession.Progress,
                "land_compensated"),
            "土地補償が現物移転・評判・履歴へ反映されない");

        var jointSession = PreparedLandDispute(definition);
        var joint = UrukRegionalSystem.SelectedOpenLandDispute(
            jointSession.Progress);
        SetGood(jointSession.Progress, "uruk_community", "barley", 50);
        Require(UrukCampaignSystem.TryApplyAction(jointSession,
            UrukRegionalSystem.JointCultivationLandAction, out _),
            "期限付き共同耕作の開始に失敗");
        Require(joint.status == "jointly_cultivated" &&
            joint.laborCommitted == 10 && joint.yieldSharePerTurn == 1 &&
            HasFarmUser(jointSession.Progress, joint.plotId,
                "eridu_community"),
            "共同耕作権・労働・現物配分が保存されない");
        Advance(jointSession, 15);
        Advance(jointSession, 16);
        Advance(jointSession, 17);
        Require(joint.status == "completed" && joint.agreementSettled &&
            joint.yieldShareExpectedTotal == 3 &&
            joint.yieldSharedTotal == 3 &&
            !HasFarmUser(jointSession.Progress, joint.plotId,
                "eridu_community") &&
            jointSession.Progress.diplomaticReputation == 55 &&
            HasDiplomaticOutcome(jointSession.Progress,
                "land_agreement_completed"),
            "共同耕作の現物配分・期限満了・利用権返還が一致しない");

        var mediationSession = PreparedLandDispute(definition);
        var mediation = UrukRegionalSystem.SelectedOpenLandDispute(
            mediationSession.Progress);
        SetGood(mediationSession.Progress, "uruk_community", "barley", 5);
        int nippurBarley = FactionGood(mediationSession.Progress,
            "nippur_community", "barley");
        Require(UrukCampaignSystem.TryApplyAction(mediationSession,
            UrukRegionalSystem.MediateLandAction, out _),
            "土地・耕作権の第三者仲裁に失敗");
        Require(mediation.status == "mediated" &&
            mediation.arbitratorFactionId == "nippur_community" &&
            mediation.arbitrationTurn == 15 &&
            !string.IsNullOrWhiteSpace(mediation.arbitrationReasonJa) &&
            mediationSession.Progress.diplomaticReputation == 54 &&
            FactionGood(mediationSession.Progress, "nippur_community",
                "barley") == nippurBarley + 1 &&
            HasDiplomaticOutcome(mediationSession.Progress,
                "land_mediation_award"),
            "土地仲裁が共同食・根拠・評判・履歴へ反映されない");
        Advance(mediationSession, 15);
        Require(UrukRegionalSystem.FindFaction(mediationSession.Progress,
            "nippur_community").currentGoalJa == "耕作権の第三者仲裁",
            "仲介勢力AIが土地仲裁の履行を目的にしない");
        string save = HistoricalCampaignSave.Serialize(mediationSession);
        var loaded = HistoricalCampaignSave.Deserialize(save,
            id => id == definition.id ? definition : null);
        Require(JsonUtility.ToJson(mediationSession.Progress) ==
            JsonUtility.ToJson(loaded.Progress),
            "土地権利・仲裁状態のセーブ往復が一致しない");

        var breachSession = PreparedLandDispute(definition);
        var breach = UrukRegionalSystem.SelectedOpenLandDispute(
            breachSession.Progress);
        SetGood(breachSession.Progress, "uruk_community", "barley", 20);
        Require(UrukCampaignSystem.TryApplyAction(breachSession,
            UrukRegionalSystem.JointCultivationLandAction, out _),
            "土地破約試験の共同耕作開始に失敗");
        var breachFaction = UrukRegionalSystem.FindFaction(
            breachSession.Progress, "eridu_community");
        int militiaBeforeBreach = breachFaction.labor.militia;
        Require(UrukCampaignSystem.TryApplyAction(breachSession,
            UrukRegionalSystem.BreachLandAgreementAction, out _),
            "土地合意の破約に失敗");
        Require(breach.status == "breached" && breach.breached &&
            breach.agreementSettled &&
            !HasFarmUser(breachSession.Progress, breach.plotId,
                "eridu_community") &&
            breachFaction.labor.militia == militiaBeforeBreach + 8 &&
            breachFaction.labor.Total == 100 &&
            breachSession.Progress.diplomaticReputation == 43 &&
            UrukRegionalSystem.LandRetaliationRiskPenalty(
                breachSession.Progress, "uruk_community",
                "eridu_community") == 8 &&
            HasDiplomaticOutcome(breachSession.Progress,
                "land_agreement_breached"),
            "土地破約が利用権取消・報復AI・危険度・評判へ反映されない");

        var rejectSession = PreparedLandDispute(definition);
        var rejected = UrukRegionalSystem.SelectedOpenLandDispute(
            rejectSession.Progress);
        var rejectFaction = UrukRegionalSystem.FindFaction(
            rejectSession.Progress, "eridu_community");
        int militiaBeforeReject = rejectFaction.labor.militia;
        Require(UrukCampaignSystem.TryApplyAction(rejectSession,
            UrukRegionalSystem.RejectLandAction, out _),
            "土地利用要求の拒否に失敗");
        Require(rejected.status == "rejected" &&
            rejectFaction.labor.militia == militiaBeforeReject + 8 &&
            rejectSession.Progress.diplomaticReputation == 45 &&
            UrukRegionalSystem.LandRetaliationRiskPenalty(
                rejectSession.Progress, "uruk_community",
                "eridu_community") == 8,
            "土地拒否の評判・報復が反映されない");
        SetGood(rejectSession.Progress, "uruk_community", "barley", 2);
        SetGood(rejectSession.Progress, "uruk_community", "alluvial_clay", 2);
        int claimantBarley = FactionGood(rejectSession.Progress,
            "eridu_community", "barley");
        int claimantClay = FactionGood(rejectSession.Progress,
            "eridu_community", "alluvial_clay");
        Require(UrukCampaignSystem.TryApplyAction(rejectSession,
            UrukRegionalSystem.RenegotiateLandAction, out _),
            "土地問題の再交渉に失敗");
        Require(rejected.status == "open" &&
            rejected.renegotiationCount == 1 &&
            rejected.retaliationJa == "" &&
            rejectSession.Progress.selectedLandDisputeId == rejected.id &&
            rejectSession.Progress.diplomaticReputation == 46 &&
            FactionGood(rejectSession.Progress, "eridu_community",
                "barley") == claimantBarley + 1 &&
            FactionGood(rejectSession.Progress, "eridu_community",
                "alluvial_clay") == claimantClay + 1 &&
            UrukRegionalSystem.LandRetaliationRiskPenalty(
                rejectSession.Progress, "uruk_community",
                "eridu_community") == 0 &&
            HasDiplomaticOutcome(rejectSession.Progress,
                "land_renegotiated"),
            "土地再交渉が現物移転・報復緩和・再選択へ反映されない");
    }

    static HistoricalCampaignSession PreparedLandDispute(
        HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var progress = session.Progress;
        var plot = Farm(progress, "uruk_west_farm");
        plot.crop = "barley";
        progress.selectedFarmId = plot.id;
        var intake = Segment(progress, "uruk_intake_segment");
        var branch = Segment(progress, "uruk_west_branch");
        intake.completed = true;
        intake.condition = 80;
        branch.completed = true;
        branch.condition = 80;
        Advance(session, 14);
        Require(UrukRegionalSystem.FirstOpenLandDispute(progress) != null,
            "灌漑・収穫条件を満たしても土地紛争が発生しない");
        return session;
    }

    static void ValidateKinshipDiplomacy(
        HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var progress = session.Progress;
        var first = UrukRegionalSystem.SelectedKinshipCandidate(progress);
        Require(first != null && first.factionId == "eridu_community",
            "初期親族連携候補が定義順のエリドゥではない");
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.NextKinshipPartnerAction, out _) &&
            progress.selectedKinshipFactionId != first.factionId,
            "親族連携候補を個別に切り替えられない");
        progress.selectedKinshipFactionId = first.factionId;

        SetGood(progress, "uruk_community", "barley", 10);
        SetGood(progress, "uruk_community", "sheep_wool", 4);
        SetGood(progress, first.factionId, "barley", 10);
        var untouched = UrukRegionalSystem.FindFaction(progress,
            "ur_community");
        int untouchedTrust = untouched.diplomaticTrust;
        int humanBarley = FactionGood(progress, "uruk_community", "barley");
        int humanWool = FactionGood(progress, "uruk_community", "sheep_wool");
        int partnerBarley = FactionGood(progress, first.factionId, "barley");
        int partnerTrust = first.diplomaticTrust;
        int baselineRisk = UrukRegionalSystem.TransportRiskForTest(definition,
            HistoricalCampaignFactory.Build(definition).Progress, "risk_probe",
            "uruk_community", first.factionId, 1);

        string result;
        Require(UrukRegionalSystem.CanProposeKinshipTie(progress),
            "条件を満たしても親族連携を提案可能と判定されない");
        Require(UrukCampaignSystem.TryApplyAction(session,
                UrukRegionalSystem.ProposeKinshipTieAction, out result),
            "親族連携の提案に失敗");
        Require(progress.kinshipTies.Length == 1,
            "親族連携台帳へ合意が追加されない");
        var tie = progress.kinshipTies[0];
        Require(tie.partnerFactionId == first.factionId &&
            tie.relationKind == "community_kinship_tie" &&
            tie.humanParticipantJa.Contains("氏名不詳") &&
            tie.partnerParticipantJa.Contains("氏名不詳") &&
            tie.consentBasisJa.Contains("同意") &&
            tie.evidenceNoteJa.Contains("直接史料はない") &&
            tie.confidence == "inferred" && tie.status == "active" &&
            result.Contains("推定復元") &&
            Array.IndexOf(tie.sourceRefs,
                "cambridge_uruk_glocalization_2025") >= 0,
            "氏名不詳・同意・確度・史料限界・出典が保存されない");
        Require(FactionGood(progress, "uruk_community", "barley") ==
                humanBarley - 1 &&
            FactionGood(progress, "uruk_community", "sheep_wool") ==
                humanWool - 1 &&
            FactionGood(progress, first.factionId, "barley") ==
                partnerBarley - 1 &&
            first.diplomaticTrust == partnerTrust + 8 &&
            untouched.diplomaticTrust == untouchedTrust &&
            progress.diplomaticReputation == 53 &&
            HasDiplomaticOutcome(progress, "kinship_tie_formed"),
            "親族連携の現物消費・対象信頼・評判・履歴が不正");
        Require(UrukRegionalSystem.KinshipTransportRiskReduction(progress,
                "uruk_community", first.factionId) == 5 &&
            UrukRegionalSystem.TransportRiskForTest(definition, progress,
                "risk_probe", "uruk_community", first.factionId, 1) ==
                Math.Max(2, baselineRisk - 5),
            "履行中の親族連携が対象間の輸送危険を5%軽減しない");

        Advance(session, 1);
        Require(tie.status == "active" &&
            first.currentGoalJa == "親族連携の履行",
            "親族連携履行中の勢力別AI目標が維持されない");
        for (int turn = 2; turn <= 4; turn++) Advance(session, turn);
        Require(tie.status == "established" && tie.trustAfter == 61 &&
            first.diplomaticTrust == 61 &&
            progress.diplomaticReputation == 55 &&
            first.currentGoalJa == "親族関係を基盤とする交易" &&
            HasDiplomaticOutcome(progress, "kinship_tie_established") &&
            UrukRegionalSystem.KinshipTransportRiskReduction(progress,
                "uruk_community", first.factionId) == 3 &&
            UrukRegionalSystem.TransportRiskForTest(definition, progress,
                "risk_probe", "uruk_community", first.factionId, 1) ==
                Math.Max(2, baselineRisk - 3),
            "4期間後の連携定着・信頼・評判・AI・交易効果が不正");

        string save = HistoricalCampaignSave.Serialize(session);
        var loaded = HistoricalCampaignSave.Deserialize(save,
            id => id == definition.id ? definition : null);
        Require(JsonUtility.ToJson(progress) == JsonUtility.ToJson(loaded.Progress),
            "親族連携状態のセーブ往復が一致しない");

        var second = UrukRegionalSystem.SelectedKinshipCandidate(progress);
        Require(second != null && second.factionId != first.factionId,
            "成立済み相手が次の親族連携候補から除外されない");
        SetGood(progress, "uruk_community", "barley", 10);
        SetGood(progress, "uruk_community", "sheep_wool", 4);
        SetGood(progress, second.factionId, "barley", 10);
        Require(UrukCampaignSystem.TryApplyAction(session,
            UrukRegionalSystem.ProposeKinshipTieAction, out _) &&
            UrukRegionalSystem.KinshipTieCount(progress) == 2 &&
            UrukRegionalSystem.SelectedKinshipCandidate(progress) == null &&
            !UrukCampaignSystem.TryApplyAction(session,
                UrukRegionalSystem.ProposeKinshipTieAction, out _),
            "親族連携の2共同体上限が機能しない");

        var lowReputation = HistoricalCampaignFactory.Build(definition);
        SetGood(lowReputation.Progress, "uruk_community", "barley", 10);
        SetGood(lowReputation.Progress, "uruk_community", "sheep_wool", 4);
        lowReputation.Progress.diplomaticReputation = 39;
        Require(!UrukCampaignSystem.TryApplyAction(lowReputation,
            UrukRegionalSystem.ProposeKinshipTieAction, out _),
            "低評判でも親族連携を提案できる");

        var lowTrust = HistoricalCampaignFactory.Build(definition);
        SetGood(lowTrust.Progress, "uruk_community", "barley", 10);
        SetGood(lowTrust.Progress, "uruk_community", "sheep_wool", 4);
        UrukRegionalSystem.SelectedKinshipCandidate(lowTrust.Progress)
            .diplomaticTrust = 44;
        Require(!UrukCampaignSystem.TryApplyAction(lowTrust,
            UrukRegionalSystem.ProposeKinshipTieAction, out _),
            "低信頼でも親族連携を提案できる");

        var noGoods = HistoricalCampaignFactory.Build(definition);
        SetGood(noGoods.Progress, "uruk_community", "barley", 0);
        SetGood(noGoods.Progress, "uruk_community", "sheep_wool", 0);
        Require(!UrukCampaignSystem.TryApplyAction(noGoods,
            UrukRegionalSystem.ProposeKinshipTieAction, out _),
            "共同食・贈答物資なしで親族連携を提案できる");
    }

    static void ValidateInformationTransmission(
        HistoricalCampaignDefinition definition)
    {
        var oral = HistoricalCampaignFactory.Build(definition);
        var oralProgress = oral.Progress;
        var oralPartner =
            UrukRegionalSystem.SelectedInformationPartner(oralProgress);
        Require(oralPartner != null &&
            oralPartner.factionId == "eridu_community",
            "初期情報伝達先が定義順のエリドゥではない");
        int oralClay = FactionGood(oralProgress, "uruk_community",
            "alluvial_clay");
        Require(UrukCampaignSystem.TryApplyAction(oral,
                UrukRegionalSystem.SendInformationAction, out _) &&
            oralProgress.informationDispatches.Length == 1,
            "開始時に口頭伝言を送れない");
        var oralDispatch = oralProgress.informationDispatches[0];
        Require(oralDispatch.medium == UrukRegionalSystem.OralMessageMedium &&
            oralDispatch.status == "pending" &&
            oralDispatch.arrivalTurn == 3 && oralDispatch.claySpent == 0 &&
            oralDispatch.reliabilityPercent == 65 &&
            oralDispatch.mediumConfidence == "inferred" &&
            oralDispatch.scenarioConfidence == "inferred" &&
            !oralDispatch.exactQuantities &&
            FactionGood(oralProgress, "uruk_community", "alluvial_clay") ==
                oralClay,
            "口頭伝言の遅延・不確実性・無資源条件が不正");
        Advance(oral, 1);
        Require(oralDispatch.status == "pending" &&
            UrukRegionalSystem.CommunicationTransportRiskReduction(
                oralProgress, "uruk_community", oralPartner.factionId, 1) == 0,
            "未到着の口頭伝言が輸送危険へ反映された");
        Advance(oral, 2);
        Require(oralDispatch.status == "pending",
            "口頭伝言が予定より早く到着した");
        Advance(oral, 3);
        Require(oralDispatch.status == "active" ||
            oralDispatch.status == "failed",
            "口頭伝言が決定的に到着判定されない");
        int oralReduction = oralDispatch.status == "active" ? 1 : 0;
        Require(UrukRegionalSystem.CommunicationTransportRiskReduction(
                oralProgress, "uruk_community", oralPartner.factionId, 3) ==
                oralReduction &&
            HasDiplomaticOutcome(oralProgress, oralDispatch.status == "active"
                ? "information_received" : "information_failed"),
            "口頭伝言の到着結果・履歴・輸送効果が不正");

        var sealedSession = HistoricalCampaignFactory.Build(definition);
        var sealedProgress = sealedSession.Progress;
        sealedProgress.selectedInformationMedium =
            UrukRegionalSystem.ClaySealingMedium;
        sealedSession.State.TurnNumber = 25;
        Require(sealedSession.CurrentYear == -3520 &&
            !UrukRegionalSystem.CanSendInformation(sealedSession) &&
            !UrukCampaignSystem.TryApplyAction(sealedSession,
                UrukRegionalSystem.SendInformationAction, out _),
            "紀元前3500年より前に封泥付き荷を使用できる");
        sealedSession.State.TurnNumber = 26;
        SetGood(sealedProgress, "uruk_community", "alluvial_clay", 4);
        int sealedClay = FactionGood(sealedProgress, "uruk_community",
            "alluvial_clay");
        int sealedBaseline = UrukRegionalSystem.TransportRiskForTest(definition,
            sealedProgress, "sealed_risk_probe", "uruk_community",
            "eridu_community", 27);
        Require(sealedSession.CurrentYear == -3500 &&
            UrukRegionalSystem.CanSendInformation(sealedSession) &&
            UrukCampaignSystem.TryApplyAction(sealedSession,
                UrukRegionalSystem.SendInformationAction, out _),
            "紀元前3500年に封泥付き荷を発送できない");
        var sealedDispatch =
            UrukRegionalSystem.LatestHumanInformationDispatch(sealedProgress);
        Require(sealedDispatch.medium ==
                UrukRegionalSystem.ClaySealingMedium &&
            sealedDispatch.earliestYear == -3500 &&
            sealedDispatch.claySpent == 1 &&
            sealedDispatch.reliabilityPercent == 85 &&
            sealedDispatch.mediumConfidence == "certain" &&
            sealedDispatch.scenarioConfidence == "inferred" &&
            Array.IndexOf(sealedDispatch.sourceRefs,
                "met_late_uruk_cylinder_seal") >= 0 &&
            FactionGood(sealedProgress, "uruk_community",
                "alluvial_clay") == sealedClay - 1,
            "封泥の年代・粘土消費・確度・出典が保存されない");
        Advance(sealedSession, 26);
        Require(sealedDispatch.status == "pending",
            "封泥付き荷が予定より早く到着した");
        Advance(sealedSession, 27);
        Require(sealedDispatch.status == "active" &&
            UrukRegionalSystem.CommunicationTransportRiskReduction(
                sealedProgress, "uruk_community", "eridu_community", 27) == 3 &&
            UrukRegionalSystem.TransportRiskForTest(definition, sealedProgress,
                "sealed_risk_probe", "uruk_community", "eridu_community", 27) ==
                Math.Max(2, sealedBaseline - 3),
            "封泥到着後の3%輸送危険軽減が不正");

        var numericSession = HistoricalCampaignFactory.Build(definition);
        var numericProgress = numericSession.Progress;
        numericProgress.selectedInformationFactionId = "ur_community";
        numericProgress.selectedInformationMedium =
            UrukRegionalSystem.NumericalRecordMedium;
        numericSession.State.TurnNumber = 33;
        Require(numericSession.CurrentYear == -3360 &&
            !UrukCampaignSystem.TryApplyAction(numericSession,
                UrukRegionalSystem.SendInformationAction, out _),
            "紀元前3350年より前に数量記録板を使用できる");
        numericSession.State.TurnNumber = 34;
        Require(numericSession.CurrentYear == -3340 &&
            !UrukCampaignSystem.TryApplyAction(numericSession,
                UrukRegionalSystem.SendInformationAction, out _),
            "数量記録行政なしで数量記録板を使用できる");
        numericProgress.templePlanned = true;
        numericProgress.templeStage = 5;
        numericProgress.templeProgress = 100;
        numericProgress.administrationAdopted = true;
        SetGood(numericProgress, "uruk_community", "alluvial_clay", 4);
        int numericBaseline = UrukRegionalSystem.TransportRiskForTest(definition,
            numericProgress, "numeric_risk_probe", "uruk_community",
            "ur_community", 35);
        Require(UrukCampaignSystem.TryApplyAction(numericSession,
                UrukRegionalSystem.SendInformationAction, out _) &&
            FactionGood(numericProgress, "uruk_community",
                "alluvial_clay") == 3,
            "行政導入後に数量記録板を発送できない");
        var numericDispatch =
            UrukRegionalSystem.LatestHumanInformationDispatch(numericProgress);
        Require(numericDispatch.medium ==
                UrukRegionalSystem.NumericalRecordMedium &&
            numericDispatch.earliestYear == -3350 &&
            numericDispatch.reliabilityPercent == 100 &&
            numericDispatch.exactQuantities &&
            numericDispatch.mediumConfidence == "certain" &&
            numericDispatch.scenarioConfidence == "inferred" &&
            Array.IndexOf(numericDispatch.sourceRefs,
                "british_museum_uruk_tablet") >= 0 &&
            Array.IndexOf(numericDispatch.sourceRefs,
                "cambridge_seals_signs_2025") >= 0,
            "数量記録板の最速年代・数量性・確度・出典が不正");
        Advance(numericSession, 34);
        Advance(numericSession, 35);
        Require(numericDispatch.status == "active" &&
            UrukRegionalSystem.CommunicationTransportRiskReduction(
                numericProgress, "uruk_community", "ur_community", 35) == 5 &&
            UrukRegionalSystem.TransportRiskForTest(definition, numericProgress,
                "numeric_risk_probe", "uruk_community", "ur_community", 35) ==
                Math.Max(2, numericBaseline - 5) &&
            HasDiplomaticOutcome(numericProgress, "information_received"),
            "数量記録板到着後の信頼・履歴・5%輸送危険軽減が不正");

        string save = HistoricalCampaignSave.Serialize(numericSession);
        var loaded = HistoricalCampaignSave.Deserialize(save,
            id => id == definition.id ? definition : null);
        Require(JsonUtility.ToJson(numericProgress) ==
                JsonUtility.ToJson(loaded.Progress),
            "情報伝達状態のセーブ往復が一致しない");
    }

    static HistoricalCampaignSession PreparedWaterDispute(
        HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var progress = session.Progress;
        var intake = Segment(progress, "uruk_intake_segment");
        var branch = Segment(progress, "uruk_north_branch");
        intake.condition = 80;
        branch.completed = true;
        branch.condition = 80;
        Segment(progress, "lagash_tigris_branch").condition = 0;
        Advance(session, 8);
        Require(UrukRegionalSystem.FirstOpenDispute(progress) != null,
            "物理条件を満たしても水利紛争が発生しない");
        return session;
    }

    static void ValidateThreeSeedDeterminism()
    {
        int[] seeds = { 4107, 5209, 6401 };
        var watch = Stopwatch.StartNew();
        int turns = 0;
        foreach (int seed in seeds)
        {
            string first = RunFiftyTurns(seed);
            string second = RunFiftyTurns(seed);
            Require(first == second, $"seed {seed} の50期結果が非決定的");
            turns += 100;
        }
        watch.Stop();
        double averageMs = watch.Elapsed.TotalMilliseconds / Math.Max(1, turns);
        Require(averageMs < 500.0,
            $"平均ターン処理が500msを超過: {averageMs:F1}ms");
        UnityEngine.Debug.Log(
            $"URUK REGIONAL 3-SEED PERF: {averageMs:F2} ms/turn");
    }

    static string RunFiftyTurns(int seed)
    {
        var definition = HistoricalCampaignRepository.LoadBuiltIn(
            HistoricalCampaignRepository.Uruk4000Id);
        definition.seed = seed;
        var session = HistoricalCampaignFactory.Build(definition);
        for (int turn = 1; turn <= 50; turn++)
        {
            Advance(session, turn);
            ValidateConservation(definition, session.Progress);
        }
        return JsonUtility.ToJson(session.Progress);
    }

    static void Advance(HistoricalCampaignSession session, int completedTurn)
    {
        session.State.TurnNumber = completedTurn + 1;
        UrukCampaignSystem.AdvanceAfterTurn(session);
    }

    static void ValidateConservation(HistoricalCampaignDefinition definition,
        UrukCampaignProgress progress)
    {
        UrukRegionalSystem.Validate(definition, progress);
        Require(progress.lastRegionalSourceWater ==
            progress.lastRegionalFarmWater + progress.lastRegionalLeakage +
            progress.lastRegionalUnusedWater, "水量保存則に違反");
        foreach (var transport in progress.transports)
            ValidateTransport(transport);
        foreach (var faction in progress.regionalFactions)
            foreach (var stock in faction.stockpiles)
                Require(stock.amount >= 0,
                    faction.factionId + "の資源が負数");
    }

    static void ValidateTransport(UrukTransportState transport)
    {
        Require(transport.shippedAmount == transport.remainingAmount +
            transport.lostAmount + transport.deliveredAmount,
            transport.id + "の輸送保存則に違反");
    }

    static UrukCanalSegmentState Segment(UrukCampaignProgress progress,
        string id)
    {
        foreach (var segment in progress.canalSegments)
            if (segment.id == id) return segment;
        throw new Exception("水路が見つからない: " + id);
    }

    static UrukFarmPlotState Farm(UrukCampaignProgress progress, string id)
    {
        foreach (var farm in progress.farmPlots)
            if (farm.id == id) return farm;
        throw new Exception("農地が見つからない: " + id);
    }

    static UrukObligationState Obligation(UrukCampaignProgress progress,
        string kind)
    {
        foreach (var obligation in progress.obligations)
            if (obligation.kind == kind) return obligation;
        throw new Exception("契約債務が見つからない: " + kind);
    }

    static bool HasSegmentUser(UrukCampaignProgress progress, string segmentId,
        string factionId)
    {
        var segment = Segment(progress, segmentId);
        foreach (string user in segment.userFactionIds)
            if (user == factionId) return true;
        return false;
    }

    static bool HasFarmUser(UrukCampaignProgress progress, string plotId,
        string factionId)
    {
        var farm = Farm(progress, plotId);
        foreach (string user in farm.userFactionIds)
            if (user == factionId) return true;
        return false;
    }

    static bool HasDiplomaticOutcome(UrukCampaignProgress progress,
        string outcome) => CountDiplomaticOutcome(progress, outcome) > 0;

    static int CountDiplomaticOutcome(UrukCampaignProgress progress,
        string outcome)
    {
        int count = 0;
        foreach (var record in progress.diplomaticRecords)
            if (record.outcome == outcome) count++;
        return count;
    }

    static void SetGood(UrukCampaignProgress progress, string factionId,
        string goodId, int amount)
    {
        HistoricalGoodAmount[] goods;
        if (factionId == "uruk_community") goods = progress.stockpiles;
        else
        {
            var faction = UrukRegionalSystem.FindFaction(progress, factionId);
            if (faction == null) throw new Exception("勢力が見つからない: " + factionId);
            goods = faction.stockpiles;
        }
        foreach (var good in goods)
            if (good.id == goodId)
            {
                good.amount = amount;
                return;
            }
        throw new Exception("物資が見つからない: " + goodId);
    }

    static int FactionGood(UrukCampaignProgress progress, string factionId,
        string goodId)
    {
        if (factionId == "uruk_community")
            return UrukCampaignSystem.GoodAmount(progress, goodId);
        var faction = UrukRegionalSystem.FindFaction(progress, factionId);
        if (faction == null) throw new Exception("勢力が見つからない: " + factionId);
        foreach (var good in faction.stockpiles)
            if (good.id == goodId) return good.amount;
        throw new Exception("物資が見つからない: " + goodId);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
