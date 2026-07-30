using System;
using UnityEditor;
using UnityEngine;
using HexCiv;
using HexCiv.Core;
using HexCiv.Core.AI;

/// <summary>
/// 30ターン体験版の専用検証。
/// 既存 SmokeTest.Run の出力を一切増やさず、別 executeMethod として実行する。
/// </summary>
public static class DemoModeSmokeTest
{
    public static void Run()
    {
        try
        {
            // 製品版と同じ条件で30ターン進めた基準状態。
            var fullReference = BuildState();
            RunCompletedTurns(fullReference, ProductEdition.DemoTurnLimit);
            string fullCheckpoint = SaveLoad.Serialize(fullReference);

            // 体験版側もCoreへは同じ入力だけを渡す。終了制限はこの進行の外側で判定する。
            var demoCandidate = BuildState();
            RunCompletedTurns(demoCandidate, ProductEdition.DemoTurnLimit);
            string demoCheckpoint = SaveLoad.Serialize(demoCandidate);

            if (!string.Equals(fullCheckpoint, demoCheckpoint, StringComparison.Ordinal))
                throw new Exception("製品版と体験版の30ターン時点の状態が一致しない");
            if (demoCandidate.TurnNumber != ProductEdition.DemoTurnLimit + 1)
                throw new Exception($"30ターン完了後のTurnNumberが不正: {demoCandidate.TurnNumber}");
            if (!ProductEdition.HasReachedTurnLimit(
                    demoCandidate, ProductEdition.DemoTurnLimit))
                throw new Exception("30ターン到達判定が成立しない");
            if (demoCandidate.IsGameOver)
                throw new Exception("体験版上限がGameState.IsGameOverを汚染している");

            // 体験版チェックポイントを製品版相当のTurnManagerで継続できること。
            var restored = SaveLoad.Deserialize(demoCheckpoint);
            if (restored == null || restored.IsGameOver ||
                restored.TurnNumber != ProductEdition.DemoTurnLimit + 1)
                throw new Exception("体験版セーブの復元状態が不正");
            new TurnManager(restored, new AIController()).RunHeadlessTurn();
            if (restored.TurnNumber != ProductEdition.DemoTurnLimit + 2)
                throw new Exception("製品版相当で31ターン目を継続できない");

            string summary = ProductEdition.BuildContinuationSummary(demoCandidate, false);
            if (!summary.Contains("残り 220 ターン") ||
                !summary.Contains("中世・近代") ||
                !summary.Contains("セーブデータ"))
                throw new Exception("体験版終了説明に具体的な継続内容が不足");

            Debug.Log($"[DemoSmoke] identical checkpoint: turn={demoCandidate.TurnNumber}, " +
                $"json={demoCheckpoint.Length}文字");
            Debug.Log("[DemoSmoke] save compatibility: continued to turn=" + restored.TurnNumber);
            Debug.Log("DEMO SMOKE OK");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.Log("DEMO SMOKE FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    static GameState BuildState()
    {
        var state = GameBootstrap.BuildNewGame(new GameConfig
        {
            Seed = 42,
            HumanPlayerIndex = -1,
            MapWidth = 40,
            MapHeight = 24,
            NumPlayers = 4,
            GameLength = GameSpeedRules.StandardLength,
            MaxTurns = GameSpeedRules.StandardMaxTurns,
        });
        new TurnManager(state, new AIController()).BeginGame();
        return state;
    }

    static void RunCompletedTurns(GameState state, int turns)
    {
        var manager = new TurnManager(state, new AIController());
        for (int i = 0; i < turns; i++)
        {
            if (state.IsGameOver)
                throw new Exception($"基準シミュレーションがターン{state.TurnNumber}で早期終了");
            manager.RunHeadlessTurn();
        }
    }
}
