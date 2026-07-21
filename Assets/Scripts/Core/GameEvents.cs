using System;

namespace HexCiv.Core
{
    /// <summary>
    /// GameState の型付きイベント(2026-07-20 Claude Code 追加)。
    /// 宣戦布告・和平・都市陥落・滅亡・ゲーム終了を、シミュレーション内の実際の変更箇所
    /// (Player.DeclareWarOn / MakePeaceWith、GameStateOps.CaptureCity / EliminateIfDefeated /
    /// CheckDominationVictory、TurnManager のスコア勝利)から通知する。
    /// 観戦演出(カメラジャンプ・バナー)などの表示側が購読する。購読者がいなければ
    /// 完全に no-op で、シミュレーション結果には一切影響しない(ヘッドレス実行でも安全)。
    /// </summary>
    public partial class GameState
    {
        /// <summary>宣戦布告(攻撃側, 防御側)。EmitLog の直後に発火する。</summary>
        public event Action<Player, Player> OnWarDeclared;
        /// <summary>和平(当事者A, 当事者B)。</summary>
        public event Action<Player, Player> OnPeaceMade;
        /// <summary>都市陥落(都市, 旧所有者, 新所有者)。所有権移転の完了後に発火する。</summary>
        public event Action<City, Player, Player> OnCityCaptured;
        /// <summary>プレイヤー滅亡。</summary>
        public event Action<Player> OnPlayerEliminated;
        /// <summary>ゲーム終了(勝者(いなければ null), 終了メッセージ日本語)。</summary>
        public event Action<Player, string> OnGameEnded;
        /// <summary>
        /// 戦闘解決(攻撃側座標, 対象座標, 防御側への与ダメージ, 攻撃側への反撃ダメージ(なければ0))。
        /// 座標はどちらも攻撃実行前のもの。Combat.PerformAttack の末尾で発火する(2026-07-21 追加)。
        /// </summary>
        public event Action<HexCoord, HexCoord, int, int> OnCombatResolved;

        internal void RaiseWarDeclared(Player aggressor, Player defender)
            => OnWarDeclared?.Invoke(aggressor, defender);

        internal void RaisePeaceMade(Player a, Player b)
            => OnPeaceMade?.Invoke(a, b);

        internal void RaiseCityCaptured(City city, Player oldOwner, Player newOwner)
            => OnCityCaptured?.Invoke(city, oldOwner, newOwner);

        internal void RaisePlayerEliminated(Player p)
            => OnPlayerEliminated?.Invoke(p);

        internal void RaiseGameEnded(Player winner, string messageJa)
            => OnGameEnded?.Invoke(winner, messageJa);

        internal void RaiseCombatResolved(HexCoord attackerCoord, HexCoord targetCoord, int damageToDefender, int damageToAttacker)
            => OnCombatResolved?.Invoke(attackerCoord, targetCoord, damageToDefender, damageToAttacker);
    }
}
