using System;
using System.Collections.Generic;

namespace HexCiv.Core
{
    /// <summary>
    /// ゲーム全体の状態(基盤部分)。ロジックは GameStateOps.cs(partial)側で実装する。
    /// Core 名前空間のクラスは MonoBehaviour を継承せず、GameObject にも触れないこと。
    /// </summary>
    public partial class GameState
    {
        public GameConfig Config;
        public HexMap Map;
        public List<Player> Players = new List<Player>();
        public int TurnNumber = 1;

        public bool IsGameOver;
        public string GameOverMessageJa;
        public Player Winner;

        /// <summary>シミュレーション用乱数。UnityEngine.Random はシミュレーションでは使わない。</summary>
        public System.Random Rng;

        /// <summary>状態が変わるたびにインクリメント。描画側はこれを監視して再同期する。</summary>
        public int Version { get; private set; }
        public void Bump() { Version++; }

        /// <summary>ゲームログ(日本語)。UIが購読する。</summary>
        public event Action<string> OnLog;
        public void EmitLog(string messageJa) { OnLog?.Invoke(messageJa); }

        int nextUnitId = 1;
        int nextCityId = 1;
        public int TakeNextUnitId() => nextUnitId++;
        public int TakeNextCityId() => nextCityId++;

        public Player HumanPlayer =>
            (Config != null && Config.HumanPlayerIndex >= 0 && Config.HumanPlayerIndex < Players.Count)
                ? Players[Config.HumanPlayerIndex]
                : null;

        public Player GetPlayer(int id)
        {
            for (int i = 0; i < Players.Count; i++)
                if (Players[i].Id == id) return Players[i];
            return null;
        }
    }
}
