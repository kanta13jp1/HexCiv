using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>AI実装の契約。TurnManager が各AIプレイヤーのターンに呼ぶ。</summary>
    public interface IAIController
    {
        /// <summary>そのプレイヤーの1ターン分の行動(移動・攻撃・生産/研究選択)をすべて実行する。</summary>
        void PlayTurn(GameState state, Player player);
    }

    /// <summary>
    /// UI・入力からシミュレーションへの操作コールバック集。GameBootstrap が実体を配線する。
    /// </summary>
    public class GameActions
    {
        public Action OnEndTurn;
        public Action<City, ProductionItem> OnChooseProduction;
        public Action<string> OnChooseResearch;   // techId
        public Action<Unit> OnFoundCity;
        public Action<Unit> OnFortify;
        public Action<Unit> OnSkip;
        public Action OnRestart;
        public Action OnSaveGame;   // 2026-07-20 追加: 現在のゲームをセーブ(Core/SaveLoad.cs)。スロット1の別名
        public Action OnLoadGame;   // 2026-07-20 追加: セーブデータをロード。スロット1の別名
        public Action<int> OnSaveGameSlot;   // 2026-07-20 追加: 指定スロット(1..3)へセーブ
        public Action<int> OnLoadGameSlot;   // 2026-07-20 追加: 指定スロット(1..3)からロード
    }
}
