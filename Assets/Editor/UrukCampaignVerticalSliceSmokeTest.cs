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
            RunIntro(session);
            ValidateRoundTrip(session);
            ValidateDeterminism(definition);
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
            p.statuses.Total != 1500 || p.statuses.enslaved != 0)
            throw new Exception("開始実人口または社会的地位が不正");
        if (!p.populationAutomation || p.productionAutomation || p.tradeAutomation)
            throw new Exception("初期自動化が人口ON・生産OFF・交易OFFではない");
        if (UrukCampaignSystem.FarmCount(p) != 2 ||
            UrukCampaignSystem.CanalCondition(p) != 25 ||
            UrukCampaignSystem.IrrigatedFarmCount(p) != 1)
            throw new Exception("農地2区画・未整備運河が不正");
        if (UrukCampaignSystem.TotalFood(p) != 7 ||
            UrukCampaignSystem.GoodAmount(p, "reeds") != 8 ||
            UrukCampaignSystem.GoodAmount(p, "alluvial_clay") != 12)
            throw new Exception("開始備蓄が不正");
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
        if (p.actualPopulation <= 1500 || UrukCampaignSystem.TotalFood(p) <= 0)
            throw new Exception("導入後の人口・食料収支が不正");
        if (p.lastFoodProduced <= 0 || p.lastFoodConsumed <= 0)
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

    static void ValidateCityStatePath(HistoricalCampaignDefinition definition)
    {
        var session = HistoricalCampaignFactory.Build(definition);
        for (int currentTurn = 1; currentTurn <= 10; currentTurn++)
        {
            if (UrukCampaignSystem.CanalCondition(session.Progress) < 55 &&
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
