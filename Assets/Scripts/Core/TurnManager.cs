using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>
    /// ターン進行。人間のターン終了で AI が順に行動し、ターンを進める。
    /// ヘッドレス実行(観戦/スモークテスト)では全プレイヤーを AI が動かす。
    /// </summary>
    public class TurnManager
    {
        readonly GameState state;
        readonly IAIController ai;

        public TurnManager(GameState state, IAIController ai)
        {
            this.state = state;
            this.ai = ai;
            CultureSystem.Bind(state);
            WorldLegacySystem.Bind(state);
            MasterpieceSystem.Bind(state);
        }

        /// <summary>ゲーム開始:視界を初期化し、開幕ログを出す。</summary>
        public void BeginGame()
        {
            Visibility.RecomputeAll(state);
            state.EmitLog("文明の夜明け ― 各文明の歴史が始まった");
            state.EmitLog($"世界には{state.HeritageSites.Count}件の遺産が眠っている");
            state.Bump();
        }

        /// <summary>人間がターンを終えた:AIプレイヤーが順に行動 → ターン進行処理。</summary>
        public void EndTurn()
        {
            if (state.IsGameOver) return;
            var human = state.HumanPlayer;
            for (int i = 0; i < state.Players.Count; i++)
            {
                if (state.IsGameOver) break;
                var p = state.Players[i];
                if (p.IsEliminated || p == human) continue;
                ai.PlayTurn(state, p);
            }
            AdvanceTurn();
        }

        /// <summary>全プレイヤー(人間枠含む)を AI が動かしてターンを進める。観戦/テスト用。</summary>
        public void RunHeadlessTurn()
        {
            if (state.IsGameOver) return;
            for (int i = 0; i < state.Players.Count; i++)
            {
                if (state.IsGameOver) break;
                var p = state.Players[i];
                if (p.IsEliminated) continue;
                ai.PlayTurn(state, p);
            }
            AdvanceTurn();
        }

        /// <summary>
        /// ターン進行:ユニット回復/移動継続、都市処理、研究進行、視界再計算、
        /// 滅亡・勝利判定、Bump()。
        /// </summary>
        void AdvanceTurn()
        {
            if (state.IsGameOver) return;
            state.TurnNumber++;

            for (int i = 0; i < state.Players.Count; i++)
            {
                var p = state.Players[i];
                if (p.IsEliminated) continue;

                foreach (var u in new List<Unit>(p.Units))
                    if (!u.IsDead) u.ResetForNewTurn(state);

                foreach (var c in new List<City>(p.Cities))
                {
                    c.ProcessTurnStart(state);
                    c.ExpandBordersIfNeeded(state);
                }

                // ---- 研究 ----
                p.ScienceStored += p.SciencePerTurn(state);
                if (!string.IsNullOrEmpty(p.CurrentResearchId))
                {
                    var tech = TechnologyCatalog.Get(p.CurrentResearchId);
                    if (p.ScienceStored >= tech.Cost)
                    {
                        p.ScienceStored -= tech.Cost;
                        p.KnownTechs.Add(tech.Id);
                        p.CurrentResearchId = null;
                        state.EmitLog($"「{p.NameJa}」が「{tech.NameJa}」を研究した");
                    }
                }

                // ---- 文化・政策 ----
                CultureSystem.AdvancePlayer(state, p);

                // ---- 遺産・偉人 ----
                WorldLegacySystem.AdvancePlayer(state, p);

                // ---- 作品収蔵 ----
                MasterpieceSystem.AdvancePlayer(state, p);

                Visibility.Recompute(state, p);
            }

            // ---- 滅亡判定 ----
            for (int i = 0; i < state.Players.Count; i++)
                state.EliminateIfDefeated(state.Players[i]);

            // ---- 制覇勝利 ----
            state.CheckDominationVictory();

            // ---- 文化交流・文化勝利 ----
            if (!state.IsGameOver)
            {
                CultureSystem.AdvanceExchange(state);
                CultureSystem.CheckCulturalVictory(state);
            }

            // ---- ターン上限:スコア勝利 ----
            if (!state.IsGameOver && state.Config != null && state.TurnNumber > state.Config.MaxTurns)
            {
                Player best = null;
                int bestScore = -1;
                for (int i = 0; i < state.Players.Count; i++)
                {
                    var p = state.Players[i];
                    if (p.IsEliminated) continue;
                    int score = 0;
                    for (int j = 0; j < p.Cities.Count; j++)
                        score += p.Cities[j].Population * 3;
                    score += p.Cities.Count * 8 + p.KnownTechs.Count * 5;
                    score += p.KnownCulturePolicies.Count * 4 + p.TotalCulture / 20;
                    score += p.DiscoveredHeritageSites.Count * 3 + p.RecruitedGreatPeople.Count * 8;
                    score += p.CollectedMasterpieces.Count * 6;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = p;
                    }
                }
                state.IsGameOver = true;
                state.Winner = best;
                state.GameOverMessageJa = best != null
                    ? $"ターン上限に到達。「{best.NameJa}」がスコア勝利を収めた!({bestScore}点)"
                    : "ターン上限に到達。ゲーム終了";
                state.EmitLog(state.GameOverMessageJa);
                state.RaiseGameEnded(best, state.GameOverMessageJa);   // 型付きイベント(2026-07-20 追加)
            }

            state.Bump();
        }
    }
}
