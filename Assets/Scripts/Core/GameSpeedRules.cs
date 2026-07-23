namespace HexCiv.Core
{
    /// <summary>
    /// ゲーム長プリセット(標準250ターン / 短期100ターン)の換算規則。
    /// 方式は「歴史圧縮」: 同じ密度の歴史を 40%(2/5)のターン数で展開する。
    ///
    /// 設計上の約束:
    /// ・MonoBehaviour / GameObject / UnityEngine に依存しない純粋な静的クラス。整数演算のみ。
    /// ・乱数を一切消費しない(state.Rng に触れない)。ヘッドレスでも決定論。
    /// ・GameConfig.GameLength == 0(標準)のとき、全ての関数は恒等写像になる。
    ///   したがって標準モードの既存挙動はビット単位で不変。
    ///
    /// 換算の使い分け(通貨ごとに片側だけを換算し、両側を同時に換算しない):
    /// ・ScaleTurn   … 標準基準のターン定数(文化勝利150、AI宣戦25 など)を現モードへ写す。
    /// ・StandardTurn… 現モードのターン番号を標準基準へ戻す(報酬式の turn/6 などに使う逆変換)。
    /// ・ScaleOutput … 毎ターンの産出(生産・科学・文化・偉人P・作品P)を 2.5倍にする。
    /// ・ScaleCost   … 累積式のしきい値(人口成長の必要食料)を 0.4倍にする。
    /// ・ScaleInterval… 周期(移住間隔など)を 0.4倍にする。
    /// </summary>
    public static class GameSpeedRules
    {
        /// <summary>GameConfig.GameLength = 0。標準(250ターン)。</summary>
        public const int StandardLength = 0;
        /// <summary>GameConfig.GameLength = 1。短期(100ターン)。</summary>
        public const int ShortLength = 1;

        public const int StandardMaxTurns = 250;
        public const int ShortMaxTurns = 100;

        /// <summary>圧縮率 = ShortMaxTurns / StandardMaxTurns = 2/5 (0.4倍)。</summary>
        public const int CompressNumerator = 2;
        public const int CompressDenominator = 5;

        /// <summary>ゲーム長に対応する最大ターン数。0 → 250、それ以外(1=短期) → 100。</summary>
        public static int MaxTurnsFor(int gameLength)
        {
            return gameLength == StandardLength ? StandardMaxTurns : ShortMaxTurns;
        }

        /// <summary>設定から最大ターン数を得る。config が null なら標準。</summary>
        public static int MaxTurnsFor(GameConfig config)
        {
            return MaxTurnsFor(config != null ? config.GameLength : StandardLength);
        }

        /// <summary>短期(圧縮)モードかどうか。config が null なら常に false(=標準扱い)。</summary>
        public static bool IsCompressed(GameConfig config)
        {
            return config != null && config.GameLength != StandardLength;
        }

        /// <summary>短期モードの表示名。UI から使う。</summary>
        public static string GameLengthNameJa(int gameLength)
        {
            return gameLength == StandardLength ? "標準(250ターン)" : "短期(100ターン)";
        }

        /// <summary>
        /// 標準基準のターン値を現モードへ写す。標準モードでは恒等。
        /// 例(短期): 150 → 60、100 → 40、180 → 72、25 → 10。
        /// </summary>
        public static int ScaleTurn(GameConfig config, int standardTurn)
        {
            return IsCompressed(config) ? Compress(standardTurn) : standardTurn;
        }

        /// <summary>
        /// ScaleTurn の逆変換。現モードのターン番号を標準基準へ戻す。標準モードでは恒等。
        /// 例(短期): 60 → 150、40 → 100、72 → 180、10 → 25。
        /// 「28 + turn/6」のような、ターンに比例する報酬式の大きさを標準の曲線に載せるために使う。
        /// </summary>
        public static int StandardTurn(GameConfig config, int currentTurn)
        {
            return IsCompressed(config) ? Expand(currentTurn) : currentTurn;
        }

        /// <summary>毎ターン産出を 2.5倍にする(短期モードのみ)。0以下は素通し。</summary>
        public static int ScaleOutput(GameConfig config, int standardOutput)
        {
            return IsCompressed(config) ? Expand(standardOutput) : standardOutput;
        }

        /// <summary>累積しきい値・コストを 0.4倍にする(短期モードのみ)。最低1。</summary>
        public static int ScaleCost(GameConfig config, int standardCost)
        {
            return IsCompressed(config) ? Compress(standardCost) : standardCost;
        }

        /// <summary>周期(nターンごと)を 0.4倍にする(短期モードのみ)。最低1。</summary>
        public static int ScaleInterval(GameConfig config, int standardInterval)
        {
            return IsCompressed(config) ? Compress(standardInterval) : standardInterval;
        }

        /// <summary>
        /// 「%/ターン」で表された事象発生率を 2.5倍にする(短期モードのみ)。上限100%。
        ///
        /// ターン数が 0.4倍になると、毎ターン判定の事象(AIの宣戦・和平など)の期待発生回数も
        /// 0.4倍に減ってしまい、「同じ密度の歴史を100ターンで展開する」という前提が崩れる。
        /// 発生率側を 1/0.4 = 2.5倍にすることで、ゲーム全体での期待発生回数を標準と揃える。
        /// 標準モードでは恒等写像なので既存挙動はビット単位で不変。
        /// </summary>
        public static int ScaleChance(GameConfig config, int standardPercent)
        {
            if (!IsCompressed(config)) return standardPercent;
            int scaled = Expand(standardPercent);
            return scaled > 100 ? 100 : scaled;
        }

        /// <summary>×2/5。四捨五入((v*4+5)/10)。正の値は最低1を保証する。</summary>
        static int Compress(int value)
        {
            if (value <= 0) return value;
            int scaled = (value * CompressNumerator * 2 + CompressDenominator) /
                (CompressDenominator * 2);
            return scaled < 1 ? 1 : scaled;
        }

        /// <summary>×5/2。四捨五入((v*10+2)/4)。</summary>
        static int Expand(int value)
        {
            if (value <= 0) return value;
            return (value * CompressDenominator * 2 + CompressNumerator) /
                (CompressNumerator * 2);
        }
    }
}
