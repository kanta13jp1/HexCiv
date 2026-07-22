using UnityEngine;

namespace HexCiv.Render
{
    /// <summary>
    /// 演出モード設定(2026-07-22 Claude Code 追加。表示のみ・シミュレーション非干渉)。
    /// PlayerPrefs "HexCiv.FxLight"(0=標準/1=軽量)を読み取り、軽量モードでは各レンダラーが
    /// 雲影・時間帯トーン・水面ゆらぎ・待機ボブ・ダメージ数字ポップを抑制する。
    /// 値はキャッシュし、約1秒より古くなったら自動で再読込する(設定UI側とのクロスファイル
    /// 配線なしでもトグルが速やかに反映される)。Refresh() で即時再読込もできる。
    /// 毎フレームの LightMode 参照は時刻比較のみで、PlayerPrefs には最大約1回/秒しか触れない。
    /// GameState / state.Rng には一切依存しないため、シミュレーション結果へ影響しない。
    /// </summary>
    public static class VisualQuality
    {
        const string PrefKey = "HexCiv.FxLight";
        /// <summary>キャッシュの有効期間(秒、Time.unscaledTime 基準)。</summary>
        const float CacheSeconds = 1f;

        static bool cachedLight;
        /// <summary>次に PlayerPrefs を読み直す時刻。初期値 -∞ で初回アクセス時は必ず読む。</summary>
        static float nextReadAt = float.NegativeInfinity;

        /// <summary>軽量演出モードか(true = 軽量)。約1秒キャッシュ付きで PlayerPrefs から読む。</summary>
        public static bool LightMode
        {
            get
            {
                float now = Time.unscaledTime;
                if (now >= nextReadAt)
                {
                    cachedLight = PlayerPrefs.GetInt(PrefKey, 0) != 0;
                    nextReadAt = now + CacheSeconds;
                }
                return cachedLight;
            }
        }

        /// <summary>設定変更直後などに PlayerPrefs を即時再読込する(呼ばなくても約1秒で追従する)。</summary>
        public static void Refresh()
        {
            cachedLight = PlayerPrefs.GetInt(PrefKey, 0) != 0;
            nextReadAt = Time.unscaledTime + CacheSeconds;
        }
    }
}
