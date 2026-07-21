namespace HexCiv.Core
{
    /// <summary>
    /// 難易度によるAI補正テーブル(2026-07-20 追加)。GameConfig.Difficulty
    /// (0=やさしい 1=普通 2=むずかしい)に応じて、AIプレイヤーの生産・科学・戦闘実効力を
    /// 百分率で補正する。人間プレイヤーには一切適用しない。
    /// 普通(既定)は全テーブルが 100% / ±0 で、各ヘルパーは値を無変換で返すため、
    /// 全計算経路が従来と数値的に完全一致する(seed 42 スモーク結果のビット一致を保つ)。
    /// GameRules は凍結ファイルのため、補正テーブルはこの別ファイルに置く。
    /// </summary>
    public static class DifficultyRules
    {
        /// <summary>AI都市の生産蓄積の百分率(index = Difficulty)。</summary>
        public static readonly int[] AIProductionPercent = { 85, 100, 120 };
        /// <summary>AI文明の科学産出の百分率(index = Difficulty)。</summary>
        public static readonly int[] AISciencePercent = { 85, 100, 120 };
        /// <summary>AIの戦闘実効力へのボーナス百分率(index = Difficulty)。</summary>
        public static readonly int[] AICombatBonusPercent = { -10, 0, 10 };

        /// <summary>難易度を有効範囲(0..2)へ丸める。</summary>
        public static int ClampDifficulty(int difficulty)
        {
            if (difficulty < 0) return 0;
            if (difficulty > 2) return 2;
            return difficulty;
        }

        /// <summary>現在の難易度(config が無ければ 1=普通)。</summary>
        static int CurrentDifficulty(GameState s)
        {
            return ClampDifficulty(s != null && s.Config != null ? s.Config.Difficulty : 1);
        }

        /// <summary>
        /// AIプレイヤーの整数値(生産・科学など)を難易度で補正する。
        /// 人間・null・百分率100 の場合は値を無変換で返す(既定難易度では完全な no-op)。
        /// 補正時は整数演算 value * percent / 100。
        /// </summary>
        public static int ScaleForAI(GameState s, Player p, int value, int[] percentTable)
        {
            if (p == null || p.IsHuman || percentTable == null) return value;
            int d = CurrentDifficulty(s);
            if (d >= percentTable.Length) return value;
            int percent = percentTable[d];
            if (percent == 100) return value;
            return value * percent / 100;
        }

        /// <summary>
        /// AI所有者の戦闘実効力(float)を難易度で補正する。
        /// 人間・所有者不明・ボーナス0 の場合は値を無変換で返す
        /// (既定難易度では浮動小数点演算を一切行わず完全な no-op)。
        /// </summary>
        public static float ScaleCombatForAI(GameState s, int playerId, float value)
        {
            var p = s != null ? s.GetPlayer(playerId) : null;
            if (p == null || p.IsHuman) return value;
            int bonus = AICombatBonusPercent[CurrentDifficulty(s)];
            if (bonus == 0) return value;
            return value * (100 + bonus) / 100f;
        }
    }
}
