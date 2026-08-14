using System;
using HexCiv.Campaigns;
using HexCiv.Core;
using UnityEditor;
using UnityEngine;

/// <summary>ウルク縦切り版の実人口・導入3ターン・物資・専用セーブの決定的検証。</summary>
public static class UrukCampaignVerticalSliceSmokeTest
{
    [MenuItem("HexCiv/Run Uruk Campaign Vertical Slice Smoke Test")]
    public static void Run()
    {
        try
        {
            var definition = HistoricalCampaignRepository.LoadBuiltIn(
                HistoricalCampaignRepository.Uruk4000Id);
            var session = HistoricalCampaignFactory.Build(definition);
            ValidateInitialState(session);
            ValidateLaborAllocation(session);
            RunIntro(session);
            ValidateRoundTrip(session);
            ValidateDeterminism(definition);
            ValidateFoodLedger(definition);
            ValidateStatusCauseEvents(definition);
            ValidateUnrestRecovery(definition);
            ValidateVersionOneMigration(definition);
            ValidateCityStatePath(definition);
            Debug.Log("URUK CAMPAIGN VERTICAL SLICE SMOKE OK");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("URUK CAMPAIGN VERTICAL SLICE SMOKE FAIL: " + ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static void ValidateInitialState(HistoricalCampaignSession session)
    {
        var p = session.Progress;
        if (p.actualPopulation != 1500 || p.roles.Total != 1500 ||
            p.statuses.Total != 1500 || p.statuses.debtBound != 0 ||
            p.statuses.captive != 0 || p.statuses.enslaved != 0)
            throw new Exception("開始実人口または社会的地位が不正");
        if (!p.populationAutomation || p.productionAutomation || p.tradeAutomation)
            throw new Exception("初期自動化が人口ON・生産OFF・交易OFFではない");
        if (UrukCampaignSystem.FarmCount(p) != 2 ||
            UrukCampaignSystem.CanalCondition(p) != 25 ||
            UrukCampaignSystem.IrrigatedFarmCount(p) != 0)
            throw new Exception("農地2区画・未整備運河が不正");
        if (UrukCampaignSystem.TotalFood(p) != 7 ||
            UrukCampaignSystem.GoodAmount(p, "reeds") != 8 ||
            UrukCampaignSystem.GoodAmount(p, "alluvial_clay") != 12)
            throw new Exception("開始備蓄が不正");
        if (UrukSubsistenceSystem.FoodReserveTenthsMonths(p) < 10 ||
            UrukSubsistenceSystem.FoodReserveTenthsMonths(p) > 20 ||
            p.seedGrain != 2 || p.storageCapacityMonths != 3)
            throw new Exception("開始時の1～2か月備蓄・種籾・倉容量が不正");
        if (p.version != 14 || p.templeStage != 0 ||
            p.recordKnowledgeElements != 3)
            throw new Exception("第3段階の初期進捗が不正");
    }

    static void ValidateLaborAllocation(HistoricalCampaignSession session)
    {
        var p = session.Progress;
        if (p.labor == null || p.labor.Total != 100 ||
            p.labor.food < 30 || UrukSubsistenceSystem.MobilizablePopulation(p) <= 0)
            throw new Exception("初期労働配分または動員可能人口が不正");
        string message;
        if (!UrukCampaignSystem.TryApplyAction(session, "labor_canal_up", out message) ||
            p.labor.Total != 100 || p.labor.canal != 20 ||
            p.populationAutomation)
            throw new Exception("5%刻みの手動労働配分に失敗: " + message);
        if (!UrukCampaignSystem.TryApplyAction(session, "labor_canal_down", out message) ||
            p.labor.Total != 100 || p.labor.canal != 15)
            throw new Exception("労働配分の取り消しに失敗: " + message);
        p.populationAutomation = true;
    }

    static void RunIntro(HistoricalCampaignSession session)
    {
        string message;
        if (!UrukCampaignSystem.TryApplyAction(session,
            UrukCampaignSystem.MaintainCanalAction, out message))
            throw new Exception("ターン1の運河整備に失敗: " + message);
        session.State.TurnNumber = 2;
        UrukCampaignSystem.AdvanceAfterTurn(session);
        if (UrukCampaignSystem.IrrigatedFarmCount(session.Progress) != 2)
            throw new Exception("運河整備後に農地2区画が灌漑されていない");

        if (!UrukCampaignSystem.TryApplyAction(session,
            UrukCampaignSystem.PrioritizeFoodAction, out message))
            throw new Exception("ターン2の食料優先に失敗: " + message);
        session.State.TurnNumber = 3;
        UrukCampaignSystem.AdvanceAfterTurn(session);

        if (!UrukCampaignSystem.TryApplyAction(session,
            UrukCampaignSystem.PlanTempleAction, out message))
            throw new Exception("ターン3の神殿着工に失敗: " + message);
        session.State.TurnNumber = 4;
        UrukCampaignSystem.AdvanceAfterTurn(session);

        var p = session.Progress;
        if (!p.introTutorialCompleted || !p.templePlanned || p.templeProgress <= 5)
            throw new Exception("3ターン導入が完了していない");
        if (p.templeStage < 2 || p.templeStage > 5)
            throw new Exception("初期神殿の段階建設が進んでいない");
        if (p.actualPopulation <= 1500 || UrukCampaignSystem.TotalFood(p) <= 0)
            throw new Exception("導入後の人口・食料収支が不正");
        if (p.lastFoodProduced <= 0 || p.lastFoodConsumed <= 0 ||
            p.lastBirths <= 0 || p.lastNormalDeaths <= 0)
            throw new Exception("20年間の食料集計が記録されていない");
    }

    static void ValidateRoundTrip(HistoricalCampaignSession session)
    {
        string json = HistoricalCampaignSave.Serialize(session);
        var loaded = HistoricalCampaignSave.Deserialize(json,
            id => id == session.CampaignId ? session.Definition : null);
        if (loaded.State.TurnNumber != session.State.TurnNumber ||
            loaded.Progress.actualPopulation != session.Progress.actualPopulation ||
            loaded.Progress.templeProgress != session.Progress.templeProgress ||
            loaded.Progress.templeStage != session.Progress.templeStage ||
            loaded.Progress.labor.Total != 100 ||
            loaded.Progress.lastBirths != session.Progress.lastBirths ||
            UrukCampaignSystem.CanalCondition(loaded.Progress) !=
                UrukCampaignSystem.CanalCondition(session.Progress) ||
            UrukCampaignSystem.TotalFood(loaded.Progress) !=
                UrukCampaignSystem.TotalFood(session.Progress))
            throw new Exception("史実キャンペーン進捗のセーブ往復が不正");
        if (HistoricalCampaignSave.Serialize(loaded) != json)
            throw new Exception("史実キャンペーン進捗セーブが決定的でない");
    }

    static void ValidateDeterminism(HistoricalCampaignDefinition definition)
    {
        string a = RunThreeTurns(definition);
        string b = RunThreeTurns(definition);
        if (a != b) throw new Exception("同じ入力の3ターン進行が決定的でない");
    }

    static void ValidateFoodLedger(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        int openingFood = UrukCampaignSystem.TotalFood(session.Progress);
        UrukCampaignSystem.TryApplyAction(session,
            UrukCampaignSystem.MaintainCanalAction, out _);
        session.State.TurnNumber = 2;
        UrukCampaignSystem.AdvanceAfterTurn(session);
        var p = session.Progress;
        int expectedClosing = openingFood + p.lastFoodProduced -
            p.lastFoodConsumed - p.lastDistributionLoss - p.lastFoodSpoiled;
        if (expectedClosing != p.lastFoodCarryover ||
            p.lastFoodCarryover != UrukCampaignSystem.TotalFood(p))
            throw new Exception(
                $"食料台帳が不均衡: open={openingFood} produced={p.lastFoodProduced} " +
                $"consumed={p.lastFoodConsumed} loss={p.lastDistributionLoss} " +
                $"spoil={p.lastFoodSpoiled} close={p.lastFoodCarryover}");
        if (p.lastFoodSpoiled < 0 || p.lastDistributionLoss < 0 ||
            p.seedGrain < 0)
            throw new Exception("食料損失または種籾が負数");
        if (p.lastFoodLaborFactor <= 0 || p.lastIrrigationFactor != 110 ||
            p.lastFloodFactor != 100 || p.lastSoilFactor <= 0 ||
            p.lastSeedToolFactor <= 0 || p.lastForecastFood <= 0)
            throw new Exception("食料の乗算要因または予測が記録されていない");
    }

    static void ValidateStatusCauseEvents(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        var p = session.Progress;
        foreach (var stock in p.stockpiles)
            if (stock.id == "barley" || stock.id == "emmer_wheat" ||
                stock.id == "fish")
                stock.amount = 0;
        p.seedGrain = 0;
        p.populationAutomation = false;
        p.labor.food = 30;
        p.labor.canal = 25;
        p.labor.construction = 15;
        p.labor.crafts = 15;
        p.labor.trade = 10;
        p.labor.militia = 5;
        int restrictedBefore = p.statuses.debtBound + p.statuses.captive +
            p.statuses.enslaved;
        session.State.TurnNumber = 2;
        UrukCampaignSystem.AdvanceAfterTurn(session);
        if (p.lastFoodShortage <= 0 || p.statuses.dependent <= 0 ||
            p.events == null || p.events.Length == 0 ||
            p.events[p.events.Length - 1].kind != "status_dependency" ||
            p.statuses.debtBound + p.statuses.captive +
                p.statuses.enslaved != restrictedBefore)
            throw new Exception("食料支援への依存化が原因イベントとして記録されていない");
    }

    static void ValidateUnrestRecovery(HistoricalCampaignDefinition definition)
    {
        var progress = UrukCampaignSystem.CreateInitialProgress(definition);
        progress.stability = 19;
        UrukSubsistenceSystem.UpdateCriticalUnrest(progress,
            progress.actualPopulation);
        UrukSubsistenceSystem.UpdateCriticalUnrest(progress,
            progress.actualPopulation);
        if (progress.consecutiveCriticalUnrest != 2 ||
            string.IsNullOrWhiteSpace(progress.lastCriticalUnrestReasonJa))
            throw new Exception("重大不安の連続期間が記録されない");
        progress.stability = 35;
        progress.lastMigrationOut = 0;
        UrukSubsistenceSystem.UpdateCriticalUnrest(progress,
            progress.actualPopulation);
        if (progress.consecutiveCriticalUnrest != 0)
            throw new Exception("安定35以上で重大不安が回復しない");
    }

    static void ValidateVersionOneMigration(HistoricalCampaignDefinition definition)
    {
        var progress = UrukCampaignSystem.CreateInitialProgress(definition);
        progress.version = 1;
        progress.labor = null;
        progress.social = null;
        progress.events = null;
        progress.seedGrain = 0;
        progress.storageCapacityMonths = 0;
        progress.recordKnowledgeElements = 0;
        UrukCampaignSystem.MigrateProgress(definition, progress);
        UrukCampaignSystem.ValidateProgress(definition, progress);
        if (progress.version != 14 || progress.labor.Total != 100 ||
            progress.seedGrain != 2 || progress.storageCapacityMonths != 3 ||
            progress.events == null)
            throw new Exception("version 1から第3段階への進捗移行に失敗");
    }

    static void ValidateCityStatePath(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        for (int currentTurn = 1; currentTurn <= 10; currentTurn++)
        {
            if (UrukCampaignSystem.CanalCondition(session.Progress) < 60 &&
                UrukCampaignSystem.GoodAmount(session.Progress, "reeds") > 0)
                UrukCampaignSystem.TryApplyAction(session,
                    UrukCampaignSystem.MaintainCanalAction, out _);
            UrukCampaignSystem.TryApplyAction(session,
                UrukCampaignSystem.PrioritizeFoodAction, out _);
            if (currentTurn == 3)
                UrukCampaignSystem.TryApplyAction(session,
                    UrukCampaignSystem.PlanTempleAction, out _);
            if (!session.Progress.administrationAdopted &&
                session.Progress.templeProgress >=
                    UrukCampaignSystem.AdministrationUnlockProgress)
                UrukCampaignSystem.TryApplyAction(session,
                    UrukCampaignSystem.AdoptAdministrationAction, out _);
            session.State.TurnNumber = currentTurn + 1;
            UrukCampaignSystem.AdvanceAfterTurn(session);
        }
        if (!session.Progress.isCityState ||
            session.Progress.cityStateFoundedTurn < 8 ||
            session.Progress.cityStateFoundedTurn > 10 ||
            session.Progress.actualPopulation < UrukCampaignSystem.CityStatePopulation ||
            session.Progress.templeProgress < UrukCampaignSystem.CityStateTempleProgress ||
            session.Progress.templeStage < 5 ||
            session.Progress.consecutiveFoodSecurePeriods < 2 ||
            !session.Progress.administrationAdopted)
            throw new Exception(
                $"推奨手順でターン10までに都市国家が成立しない: " +
                $"turn={session.Progress.cityStateFoundedTurn} pop={session.Progress.actualPopulation} " +
                $"temple={session.Progress.templeProgress} admin={session.Progress.administrationAdopted}");
    }

    static string RunThreeTurns(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        UrukCampaignSystem.TryApplyAction(session,
            UrukCampaignSystem.MaintainCanalAction, out _);
        session.State.TurnNumber = 2;
        UrukCampaignSystem.AdvanceAfterTurn(session);
        UrukCampaignSystem.TryApplyAction(session,
            UrukCampaignSystem.PrioritizeFoodAction, out _);
        session.State.TurnNumber = 3;
        UrukCampaignSystem.AdvanceAfterTurn(session);
        UrukCampaignSystem.TryApplyAction(session,
            UrukCampaignSystem.PlanTempleAction, out _);
        session.State.TurnNumber = 4;
        UrukCampaignSystem.AdvanceAfterTurn(session);
        return HistoricalCampaignSave.Serialize(session);
    }
}
