using UnityEngine;

namespace HexCiv.Core
{
    public class GameConfig
    {
        public int MapWidth = 44;
        public int MapHeight = 26;
        public int NumPlayers = 4;
        /// <summary>0 なら起動時にランダム化。</summary>
        public int Seed = 0;
        public int MaxTurns = 250;
        /// <summary>-1 なら全員AI(ヘッドレステスト/観戦)。</summary>
        public int HumanPlayerIndex = 0;
        /// <summary>マップ種別(2026-07-20 追加)。0=大陸(従来既定) 1=パンゲア 2=群島。</summary>
        public int MapType = 0;
        /// <summary>難易度(2026-07-20 追加)。0=やさしい 1=普通(既定) 2=むずかしい。
        /// AIへの補正テーブルは DifficultyRules 参照。普通は補正なしで従来と完全に同一挙動。</summary>
        public int Difficulty = 1;
        /// <summary>ゲーム長(2026-07-23 追加)。0=標準(250ターン・既定) 1=短期(100ターン)。
        /// 換算規則は GameSpeedRules 参照。0 では全ての換算が恒等写像になり従来と完全に同一挙動。
        /// MaxTurns はこのフィールドから自動更新されない(新規ゲーム開始時にブートストラップが
        /// GameSpeedRules.MaxTurnsFor で設定する)。既定値250はスモークテストの基準として不変。</summary>
        public int GameLength = 0;

        public static readonly string[] CivNames = { "アテネ", "ローマ", "エジプト", "バビロン" };

        public static readonly Color[] CivColors =
        {
            new Color(0.20f, 0.45f, 0.90f),  // 青
            new Color(0.85f, 0.25f, 0.20f),  // 赤
            new Color(0.90f, 0.75f, 0.20f),  // 黄
            new Color(0.60f, 0.30f, 0.75f),  // 紫
        };

        public static readonly string[][] CityNames =
        {
            new[] { "アテネ", "スパルタ", "コリント", "テーベ", "デルフォイ", "アルゴス", "ミレトス" },
            new[] { "ローマ", "アンティウム", "クマエ", "ネアポリス", "ラヴェンナ", "ポンペイ", "オスティア" },
            new[] { "メンフィス", "ヘリオポリス", "ギザ", "アレクサンドリア", "ルクソール", "アスワン", "エレファンティネ" },
            new[] { "バビロン", "ウル", "ウルク", "ニップール", "アッカド", "ラガシュ", "エリドゥ" },
        };
    }
}
