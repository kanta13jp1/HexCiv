using System;
using System.Diagnostics;
using HexCiv.Campaigns;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ウルク編の水・農地・AI台帳・輸送・移住・外交を決定論的に検証する。
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
        Require(progress.version == 6, "進捗versionが6ではない");
        Require(progress.obligations != null, "契約債務台帳が初期化されていない");
        Require(progress.diplomaticRecords != null &&
            progress.diplomaticReputation == 50,
            "外交履歴または初期評判が初期化されていない");
        Require(progress.regionalFactions.Length == 8, "8勢力台帳がない");
        Require(progress.farmPlots.Length == definition.farmPlots.Length,
            "農地定義が状態へ反映されていない");
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
        Require(migrated.version == 6 &&
            migrated.waterDisputes[0].claimantFarmId ==
                "lagash_hinterland_farm" &&
            migrated.waterDisputes[0].resolutionKind == "joint_maintenance" &&
            migrated.waterDisputes[0].agreementSettled,
            "version 5から水利紛争の原因・解決状態を補完できない");

        var chainedMigration = HistoricalCampaignFactory.Build(definition).Progress;
        chainedMigration.version = 3;
        chainedMigration.obligations = null;
        chainedMigration.diplomaticRecords = null;
        UrukCampaignSystem.MigrateProgress(definition, chainedMigration);
        Require(chainedMigration.version == 6 &&
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
            repairSession.Progress.diplomaticReputation == 54,
            "共同補修が水路・資材・労働・評判へ反映されない");

        var rejectSession = PreparedWaterDispute(definition);
        Require(UrukCampaignSystem.TryApplyAction(rejectSession,
            UrukRegionalSystem.RejectWaterAction, out _), "水利要求の拒否に失敗");
        Require(rejectSession.Progress.diplomaticReputation == 46 &&
            HasDiplomaticOutcome(rejectSession.Progress, "rejected"),
            "水利要求拒否が外交評判と履歴へ反映されない");
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
