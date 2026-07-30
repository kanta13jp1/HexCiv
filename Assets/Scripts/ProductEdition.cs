using System;
using HexCiv.Core;

namespace HexCiv
{
    /// <summary>
    /// 製品版と体験版の実行時ポリシー。
    ///
    /// 体験版かどうかはビルド時の HEXCIV_DEMO 定義だけで決まり、GameConfig や
    /// SaveLoad のDTOには一切書き込まない。したがって体験版で保存したデータは
    /// 製品版でそのまま読み込み、31ターン目から続きを遊べる。
    /// </summary>
    public static class ProductEdition
    {
        public const int DemoTurnLimit = 30;

        public static bool IsDemo
        {
            get
            {
#if HEXCIV_DEMO
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>開始時のターン1を0ターン完了として数える。</summary>
        public static int CompletedTurns(GameState state)
        {
            return state == null ? 0 : Math.Max(0, state.TurnNumber - 1);
        }

        public static bool HasReachedDemoLimit(GameState state)
        {
            return HasReachedTurnLimit(state, DemoTurnLimit);
        }

        /// <summary>専用テストとUIでも使える、エディション非依存の上限判定。</summary>
        public static bool HasReachedTurnLimit(GameState state, int turnLimit)
        {
            return state != null && turnLimit > 0 && CompletedTurns(state) >= turnLimit;
        }

        public static int RemainingDemoTurns(GameState state, int turnLimit = DemoTurnLimit)
        {
            return Math.Max(0, turnLimit - CompletedTurns(state));
        }

        /// <summary>
        /// 体験版終了画面へ出す具体的な継続内容。表示専用で状態を変更しない。
        /// </summary>
        public static string BuildContinuationSummary(GameState state, bool historicalCampaign)
        {
            int fullTurnLimit = state != null && state.Config != null
                ? Math.Max(DemoTurnLimit, state.Config.MaxTurns)
                : GameSpeedRules.ShortMaxTurns;
            int remainingFullTurns = Math.Max(0, fullTurnLimit - DemoTurnLimit);

            Player reference = state != null ? state.HumanPlayer : null;
            if (reference == null && state != null)
            {
                for (int i = 0; i < state.Players.Count; i++)
                {
                    if (!state.Players[i].IsEliminated)
                    {
                        reference = state.Players[i];
                        break;
                    }
                }
            }

            int knownTechs = reference != null ? reference.KnownTechs.Count : 0;
            int knownPolicies = reference != null ? reference.KnownCulturePolicies.Count : 0;
            int lockedTechs = Math.Max(0, TechnologyCatalog.All.Count - knownTechs);
            int lockedPolicies = Math.Max(0, CulturePolicyCatalog.All.Count - knownPolicies);
            string futurePeriod = historicalCampaign
                ? "・ウルク編の後半：都市国家成立後から紀元前3000年頃まで"
                : "・未到達の時代：中世・近代";

            return
                $"{DemoTurnLimit}ターンを完了しました。\n\n" +
                "製品版で続く内容\n" +
                $"・残り {remainingFullTurns} ターン\n" +
                futurePeriod + "\n" +
                $"・未解放の研究 {lockedTechs} 件、未採用の文化政策 {lockedPolicies} 件\n" +
                "・都市発展、外交、戦争、科学・文化・経済・軍事の勝利判定\n\n" +
                "体験版のセーブデータは製品版でそのまま読み込めます。";
        }
    }
}
