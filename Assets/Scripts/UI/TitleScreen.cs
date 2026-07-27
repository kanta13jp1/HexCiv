using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Audio;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// タイトル画面(2026-07-21 Claude Code 追加)。
    /// アプリ起動ごとに1回だけ、自動起動済みのゲームの上へオーバーレイ表示する
    /// (起動中のマップがそのまま「生きた背景」になる。60%の暗幕で減光)。
    ///
    /// MinimapPanel / ChroniclePanel と同じ独立Canvas方式: UIManager は変更せず、
    /// GameBootstrap のタイトル用公開フック(QuickLoadFromTitle / OpenSettingsFromTitle /
    /// StartSpectatorFromTitle)だけを呼ぶ。Canvas sortingOrder=200 で、UIManager(100)、
    /// Codexの独立パネル(130〜140)、年表(145)のすべてより手前に置く。
    ///
    /// メニュー:
    ///   つづける(クイックロード) … スロット1にセーブがある時のみ有効(SaveLoad.TryReadMeta)
    ///   新しいゲーム / 設定       … タイトルを閉じ、既存のゲーム設定パネルを開く
    ///   シミュレーション観戦     … 既存の観戦開始経路で全AIゲームを開始
    ///   そのまま遊ぶ             … タイトルを閉じるだけ(自動開始済みのゲームを続行)
    ///   終了                     … Application.Quit(エディタでは非表示)
    /// Esc = そのまま遊ぶ。どの選択でも全体を約0.4秒(非スケール時間)でフェードアウトし、
    /// フェード開始と同時に入力ブロックを解除してから自身を破棄する。
    ///
    /// 入力ブロックについて:
    ///   マウス: 全画面の暗幕Image(raycastTarget=true)+GraphicRaycaster により、
    ///   UIManager.IsPointerOverUI()(EventSystem.IsPointerOverGameObject)が常にtrueになる
    ///   ため、InputController のクリック選択・移動命令・ホバーツールチップは表示中すべて
    ///   ブロックされる。
    ///   キー: InputController / 各独立パネルのホットキー(Enter=ターン終了、Space/Tab=
    ///   ユニット巡回、F5/F9、C、M、Esc等)は本画面からは横取りできない(InputController /
    ///   UIManager は本ラウンドでは変更対象外のため)。既知の残余として、タイトル表示中の
    ///   キー入力は下のゲームへ届く(Escはタイトルを閉じると同時に、フルスクリーン中なら
    ///   ウィンドウ復帰も起きうる)。実害はゲーム進行の先行のみで、許容とする。
    /// シミュレーションへは完全に無干渉(読み取りもGameBootstrapフック呼び出しのみ)。
    ///
    /// 2026-07-22 Claude Code 追加: タイトル演出+タイトルBGMイントロ。
    ///   演出はすべて非スケール時間・アロケーションフリーの表示専用3種 —
    ///   ①タイトル文字の金色グロー(約3秒周期の色レープ) ②メニューボタンの段階
    ///   フェード+スライドイン(約80ms間隔) ③背景バナー帯の超低速ドリフト(約2px/s往復)。
    ///   BGMは表示中 GameAudio.PlayTitleIntro()(専用の静かなループ)を流し、閉じる時
    ///   (どの選択肢/Escでも)EndTitleIntro() で約1.5秒かけて通常の時代BGMへ戻す。
    ///   GameAudio が無い構成(ヘッドレス等)では null 判定で何もしない。
    ///
    /// 2026-07-24 Claude Code 追加: 「今回の更新」カード。
    ///   起動時に UI/UpdateDigest が「前回起動からの増分」を算出し、変化があるときだけ画面
    ///   右側へカードをスライドインさせる(差分ゼロならカード自体を作らない)。各行は前回値
    ///   から今回値へ約0.8秒でカウントアップし、完了後に増分「+n」が明るい色で現れる。
    ///   パネルキーを持つ行はアイコン付きでクリック可能で、押すとタイトルを閉じてから
    ///   UpdateDigest.TryOpenPanel(key) で該当パネルへ直行する。新設パネルには「NEW」バッジ。
    ///   カードが現れる瞬間に GameAudio.PlayUpdateChime() を一度だけ鳴らす。
    ///   配置は右端(1280x720基準で x≈282〜622)で、中央のメニュー列(x≈±170)にも右下のバージョン表記にも
    ///   重ならない。カード背景は raycastTarget=false、スライドイン完了まで
    ///   CanvasGroup.blocksRaycasts=false のため、下のボタンのクリックを奪うことはない。
    ///   Esc/メニュー選択(BeginClose)で即座に退場する。
    ///   UpdateDigest へはリフレクション経由でアクセスする(同ラウンドで別エージェントが
    ///   新規作成中のため、API形状の差異・未着地・実行時例外のいずれでもタイトル画面は
    ///   従来どおり動く。全経路 try/catch でカードは「あれば得」の付加要素に留める)。
    /// </summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        /// <summary>表示用バージョン文字列(2026-07-21 追加)。タイトル右下の表記に使う。
        /// 将来ほかの画面やログでも再利用できるよう公開定数にしている。</summary>
        public const string Version = "1.0";

        /// <summary>フェードアウトの長さ(秒、非スケール時間)。</summary>
        const float FadeOutSeconds = 0.4f;

        /// <summary>今回のアプリ起動中に表示済みか(リスタート等での再表示を防ぐ)。</summary>
        static bool shownThisLaunch;

        Canvas canvas;
        CanvasGroup group;
        GameBootstrap bootstrap;
        /// <summary>フェードアウト中か(選択済み。以後の操作は受け付けない)。</summary>
        bool closing;
        float closeStartedAt;

        // ---- 2026-07-22 Claude Code 追加: タイトル演出+タイトルBGMイントロ ----
        /// <summary>タイトル文字グローの周期(秒、非スケール時間)。</summary>
        const float TitlePulseCycleSeconds = 3f;
        /// <summary>メニューボタン出現の間隔(秒)。上から順に80msずつ遅らせる。</summary>
        const float MenuStaggerSeconds = 0.08f;
        /// <summary>メニューボタン1個のフェード+スライドの長さ(秒)。</summary>
        const float MenuAppearSeconds = 0.35f;
        /// <summary>メニューボタンのスライド開始オフセット(px、下から上へ)。</summary>
        const float MenuSlideOffset = 18f;
        /// <summary>バナー帯ドリフトの速度(px/秒)と片側振幅(px)。往復周期は約80秒。</summary>
        const float BannerDriftPixelsPerSecond = 2f;
        const float BannerDriftHalfRange = 40f;
        /// <summary>グローの明側の金色(暗側は UIStyle.Accent)。</summary>
        static readonly Color TitleGlowBright = new Color(1f, 0.95f, 0.62f, 1f);

        Text titleText;
        /// <summary>バナー帯のRect(テクスチャ欠落時はnull=ドリフトなし)。</summary>
        RectTransform bannerRect;
        float bannerBaseY;
        /// <summary>出現アニメ対象のメニューボタン(最大6個: つづける〜終了)。</summary>
        readonly RectTransform[] menuItemRects = new RectTransform[6];
        readonly CanvasGroup[] menuItemGroups = new CanvasGroup[6];
        readonly float[] menuItemTargetY = new float[6];
        int menuItemCount;
        /// <summary>表示開始時刻(Time.unscaledTime)。全演出の基準時刻。</summary>
        float shownAt;
        /// <summary>タイトルBGMイントロを開始済みか(GameAudio生成後に一度だけ開始する)。</summary>
        bool introStarted;
        /// <summary>タイトルBGMイントロの終了を通知済みか(多重通知の防止)。</summary>
        bool introEnded;

        // ---- 2026-07-24 Claude Code 追加: 「今回の更新」カード ----
        /// <summary>カードのスライドイン開始待ち時間(秒)。既存の出現演出
        /// (最大6項目 × 0.08秒 + 0.35秒 ≒ 0.75秒)が落ち着いてから動き出す。</summary>
        const float CardDelaySeconds = 1.0f;
        /// <summary>カードのスライドイン+フェードインの長さ(秒、非スケール時間)。</summary>
        const float CardSlideSeconds = 0.45f;
        /// <summary>カードの退場(Esc/メニュー選択)の長さ(秒)。全体フェード0.4秒より速く畳む。</summary>
        const float CardDismissSeconds = 0.18f;
        /// <summary>1行のカウントアップの長さ(秒)。</summary>
        const float CountUpSeconds = 0.8f;
        /// <summary>行ごとのカウントアップ開始のずらし幅(秒)。</summary>
        const float RowCountStaggerSeconds = 0.07f;
        /// <summary>増分「+n」のフェードインの長さ(秒)。</summary>
        const float DeltaFadeSeconds = 0.25f;
        /// <summary>カードに出す最大行数(超過分は「ほか n件」にまとめる)。</summary>
        const int MaxVisibleRows = 6;
        /// <summary>カードの幅(px)。右端から18px内側に置くため、左端は x≈282(1280x720基準)。
        /// メニューボタン(幅340・中央揃え=±170)とは約112px離れており重ならない。
        /// CanvasScaler(match=0.5)で横に狭い画面ほど余白は縮むが、4:3で約26px、5:4でも
        /// 約8pxの間隔が残る幅に決めてある。</summary>
        const float CardWidth = 340f;
        /// <summary>カードの内側余白(左右・上下、px)。</summary>
        const float CardPadding = 14f;
        /// <summary>ヘッダー/要約/行/脚注の高さ(px)。</summary>
        const float CardHeaderHeight = 26f;
        const float CardSummaryHeight = 18f;
        const float CardRowHeight = 32f;
        const float CardFooterHeight = 18f;
        /// <summary>表示位置(pivot=右中央基準のX、および中央からの縦オフセット)。</summary>
        const float CardVisibleX = -18f;
        const float CardCenterY = 40f;
        /// <summary>画面外(右)の待機位置。カード幅より外側=完全に隠れる。</summary>
        const float CardOffscreenX = CardWidth + 24f;
        static readonly Color CardBg = new Color(0.07f, 0.09f, 0.13f, 0.93f);
        /// <summary>増分「+n」の色(明るい緑=既存のAccent金/Danger赤と混同しない)。</summary>
        static readonly Color CardDeltaColor = new Color(0.44f, 1f, 0.62f, 1f);
        static readonly Color CardNewBadgeBg = new Color(0.85f, 0.30f, 0.26f, 1f);

        /// <summary>カード1行の演出状態(カウントアップ+増分フェード)。</summary>
        sealed class DigestRowView
        {
            public Text ValueText;
            public Text DeltaText;
            public int Previous;
            public int Current;
            /// <summary>カウントアップ開始時刻(Time.unscaledTime。カード出現時に確定する)。</summary>
            public float CountStartAt;
            /// <summary>最後に文字列化した値(同値なら Text へ書き込まない)。</summary>
            public int LastShown = int.MinValue;
            /// <summary>この行の演出が完了したか(以後は何もしない)。</summary>
            public bool Done;
        }

        readonly List<DigestRowView> digestRows = new List<DigestRowView>();
        RectTransform cardRect;
        CanvasGroup cardGroup;
        /// <summary>カードのスライドイン開始時刻(-1=未開始)。</summary>
        float cardShowAt = -1f;
        /// <summary>カード退場の開始時刻(-1=退場していない)。</summary>
        float cardDismissAt = -1f;
        float cardDismissFromAlpha = 1f;
        float cardDismissFromX;
        /// <summary>更新チャイムを鳴らし済みか(多重再生の防止)。</summary>
        bool cardChimePlayed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (shownThisLaunch) return;
            if (FindFirstObjectByType<TitleScreen>() != null) return;
            shownThisLaunch = true;
            new GameObject("TitleScreenUI").AddComponent<TitleScreen>();
        }

        void Start()
        {
            BuildUi();
        }

        void Update()
        {
            if (bootstrap == null) bootstrap = FindFirstObjectByType<GameBootstrap>();

            // タイトル演出(2026-07-22 追加)。フェードアウト中も継続する(closing分岐より前)。
            AnimateVisuals();

            // タイトルBGMイントロ(2026-07-22 追加): GameAudio 生成後に一度だけ開始する。
            // Start() の時点では GameBootstrap 側の初期化順によって Instance が未生成のことが
            // あるため、ここで遅延開始する。GameAudio の無い構成では introStarted のまま何もしない。
            if (!introStarted && !closing && GameAudio.Instance != null)
            {
                introStarted = true;
                GameAudio.Instance.PlayTitleIntro();
            }

            if (closing)
            {
                float alpha = 1f - (Time.unscaledTime - closeStartedAt) / FadeOutSeconds;
                if (group != null) group.alpha = Mathf.Clamp01(alpha);
                if (alpha <= 0f) Destroy(gameObject);
                return;
            }

            // Esc = そのまま遊ぶ(閉じるだけ)。クラス冒頭コメントのとおり InputController 側の
            // Esc 処理(選択解除/フルスクリーン解除)も同フレームに走りうるが、許容する。
            // ただし「成長の記録」が開いている間は、その画面が Esc を自分で消費するので
            // タイトルは閉じない(記録を閉じたつもりでゲームが始まってしまうのを防ぐ)。
            if (Input.GetKeyDown(KeyCode.Escape) && !GrowthHistoryView.IsOpen) BeginClose();
        }

        /// <summary>
        /// タイトル演出の毎フレーム更新(2026-07-22 Claude Code 追加)。表示専用・非スケール時間・
        /// アロケーションフリー(色/ベクトルは構造体、文字列生成なし)。ヘッドレスでも
        /// 単なる数値更新のみで安全。
        /// </summary>
        void AnimateVisuals()
        {
            float elapsed = Time.unscaledTime - shownAt;

            // ① タイトル文字の金色グロー: UIStyle.Accent ↔ 明るい金を約3秒周期で往復。
            //    cos基準で表示開始時は暗側(=従来色)から始まる。
            if (titleText != null)
            {
                float pulse = 0.5f - 0.5f * Mathf.Cos(elapsed * (2f * Mathf.PI / TitlePulseCycleSeconds));
                titleText.color = Color.Lerp(UIStyle.Accent, TitleGlowBright, pulse);
            }

            // ② メニューボタンの段階フェード+スライドイン(上から順に80msずらし、各0.35秒)。
            //    完了したボタンには書き込まない(以後この項は実質no-op)。
            for (int i = 0; i < menuItemCount; i++)
            {
                var itemGroup = menuItemGroups[i];
                var itemRect = menuItemRects[i];
                if (itemGroup == null || itemRect == null) continue;
                float t = Mathf.Clamp01((elapsed - i * MenuStaggerSeconds) / MenuAppearSeconds);
                if (t >= 1f && itemGroup.alpha >= 1f) continue;
                float ease = 1f - (1f - t) * (1f - t);   // ease-out(終端で速度0)
                itemGroup.alpha = ease;
                itemRect.anchoredPosition = new Vector2(0f,
                    menuItemTargetY[i] - MenuSlideOffset * (1f - ease));
            }

            // ③ 背景バナー帯の超低速ドリフト: 約2px/sで±40pxを往復(帯は96px広げてあるため
            //    画面端が露出しない)。PingPongの位相を+半振幅ずらし、中央から右へ動き始める。
            if (bannerRect != null)
            {
                float x = Mathf.PingPong(elapsed * BannerDriftPixelsPerSecond + BannerDriftHalfRange,
                    BannerDriftHalfRange * 2f) - BannerDriftHalfRange;
                bannerRect.anchoredPosition = new Vector2(x, bannerBaseY);
            }

            // ④ 「今回の更新」カードの出現・カウントアップ・退場(2026-07-24 追加)。
            //    カードが無い(差分ゼロ/生成失敗)場合は先頭で即returnするため完全にno-op。
            AnimateUpdateCard();
        }

        /// <summary>
        /// 「今回の更新」カードの毎フレーム更新(2026-07-24 Claude Code 追加)。
        /// 表示専用・非スケール時間。カード未生成なら即return(差分ゼロ・生成失敗時の通常経路)。
        /// 例外を投げうる処理は含まない(参照はすべて null 判定済み)。
        /// </summary>
        void AnimateUpdateCard()
        {
            if (cardRect == null) return;
            float now = Time.unscaledTime;

            // 退場中(Esc/メニュー選択): 素早く右へ抜けながら消える
            if (cardDismissAt >= 0f)
            {
                float dt = Mathf.Clamp01((now - cardDismissAt) / CardDismissSeconds);
                if (cardGroup != null) cardGroup.alpha = cardDismissFromAlpha * (1f - dt);
                cardRect.anchoredPosition =
                    new Vector2(Mathf.Lerp(cardDismissFromX, CardOffscreenX, dt), CardCenterY);
                if (dt >= 1f)
                {
                    cardRect.gameObject.SetActive(false);
                    cardRect = null;
                }
                return;
            }

            // 出現待ち → 出現開始(チャイムとカウントアップの基準時刻をここで確定する)
            if (cardShowAt < 0f)
            {
                if (now - shownAt < CardDelaySeconds) return;
                cardShowAt = now;
                PlayUpdateChimeOnce();
                for (int i = 0; i < digestRows.Count; i++)
                {
                    digestRows[i].CountStartAt =
                        cardShowAt + CardSlideSeconds * 0.55f + i * RowCountStaggerSeconds;
                }
            }

            // スライドイン+フェードイン(ease-out cubic)
            float t = Mathf.Clamp01((now - cardShowAt) / CardSlideSeconds);
            float ease = 1f - (1f - t) * (1f - t) * (1f - t);
            cardRect.anchoredPosition =
                new Vector2(Mathf.Lerp(CardOffscreenX, CardVisibleX, ease), CardCenterY);
            if (cardGroup != null)
            {
                cardGroup.alpha = ease;
                // スライド完了後にだけクリックを受け付ける(移動中の誤クリックを防ぐ)
                if (t >= 1f && !cardGroup.blocksRaycasts)
                {
                    cardGroup.blocksRaycasts = true;
                    cardGroup.interactable = true;
                }
            }

            for (int i = 0; i < digestRows.Count; i++) AnimateDigestRow(digestRows[i], now);
        }

        /// <summary>
        /// 1行のカウントアップ(前回値→今回値、ease-out)と、完了後の増分「+n」フェードイン。
        /// 値が変わったフレームだけ Text へ書き込む(毎フレームの文字列生成を避ける)。
        /// </summary>
        void AnimateDigestRow(DigestRowView row, float now)
        {
            if (row == null || row.Done) return;
            float t = (now - row.CountStartAt) / CountUpSeconds;
            if (t <= 0f) return;
            t = Mathf.Clamp01(t);
            float ease = 1f - (1f - t) * (1f - t) * (1f - t);
            int shown = row.Previous + Mathf.RoundToInt((row.Current - row.Previous) * ease);
            if (shown != row.LastShown)
            {
                row.LastShown = shown;
                if (row.ValueText != null) row.ValueText.text = FormatCount(shown);
            }
            if (t < 1f) return;

            if (row.DeltaText == null)
            {
                row.Done = true;
                return;
            }
            float fade = Mathf.Clamp01((now - (row.CountStartAt + CountUpSeconds)) / DeltaFadeSeconds);
            var color = CardDeltaColor;
            color.a = fade;
            row.DeltaText.color = color;
            if (fade >= 1f) row.Done = true;
        }

        /// <summary>更新チャイムを一度だけ鳴らす(ミュート/音量は GameAudio 側が一括処理。
        /// GameAudio が無い構成=ヘッドレス等では何もしない)。</summary>
        void PlayUpdateChimeOnce()
        {
            if (cardChimePlayed) return;
            cardChimePlayed = true;
            try
            {
                var audio = GameAudio.Instance;
                if (audio != null) audio.PlayUpdateChime();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TitleScreen] 更新チャイムの再生を省略しました: " + ex.Message);
            }
        }

        /// <summary>
        /// 「今回の更新」カードを即座に退場させる(Esc/メニュー選択のどの経路でも呼ばれる)。
        /// 入力の受け付けはこの時点で完全に切り、見た目だけが約0.18秒で右へ抜ける。
        /// 出現前(スライド未開始)なら演出せずそのまま消す。
        /// </summary>
        void DismissUpdateCard()
        {
            if (cardRect == null || cardDismissAt >= 0f) return;
            if (cardGroup != null)
            {
                cardGroup.interactable = false;
                cardGroup.blocksRaycasts = false;
            }
            if (cardShowAt < 0f)
            {
                cardRect.gameObject.SetActive(false);
                cardRect = null;
                return;
            }
            cardDismissAt = Time.unscaledTime;
            cardDismissFromAlpha = cardGroup != null ? cardGroup.alpha : 1f;
            cardDismissFromX = cardRect.anchoredPosition.x;
        }

        // ==================================================================
        // メニュー動作(GameBootstrap のタイトル用フックへ委譲)
        // ==================================================================

        void OnContinueClicked()
        {
            if (closing) return;
            if (bootstrap != null) bootstrap.QuickLoadFromTitle();
            BeginClose();
        }

        void OnNewGameClicked()
        {
            if (closing) return;
            BeginClose();
            // タイトルはフェード中も入力を通す(BeginCloseでブロック解除済み)ため、
            // 設定パネルはフェードの下ですぐ操作できる
            if (bootstrap != null) bootstrap.OpenSettingsFromTitle();
        }

        void OnHistoricalCampaignClicked()
        {
            if (closing) return;
            if (bootstrap != null) bootstrap.StartHistoricalCampaignFromTitle();
            BeginClose();
        }

        void OnSpectatorClicked()
        {
            if (closing) return;
            if (bootstrap != null) bootstrap.StartSpectatorFromTitle();
            BeginClose();
        }

        void OnPlayClicked()
        {
            if (closing) return;
            BeginClose();
        }

        void OnQuitClicked()
        {
            if (closing) return;
            Application.Quit();
        }

        /// <summary>
        /// 「今回の更新」カードの行クリック(2026-07-24 Claude Code 追加)。
        /// 既存メニューと同じくまずタイトルを閉じ(フェード中も下は操作可能)、続いて
        /// UpdateDigest.TryOpenPanel で該当パネルを開く。開けなくてもタイトルは閉じたままで、
        /// 「そのまま遊ぶ」を選んだのと同じ結果になる(失敗しても行き止まりにならない)。
        /// </summary>
        void OnDigestRowClicked(string panelKey)
        {
            if (closing) return;
            BeginClose();
            DigestBridge.TryOpenPanel(panelKey);
        }

        /// <summary>
        /// 「成長の記録」を開く(2026-07-27 追加)。**タイトル画面は閉じない** — 記録を見た後に
        /// そのままメニューへ戻れるようにするため。開けなかった場合(記録画面の生成失敗)は
        /// 何も起きないだけで、タイトル画面は従来どおり操作できる。
        /// </summary>
        void OnGrowthHistoryClicked()
        {
            if (closing) return;
            try
            {
                GrowthHistoryView.Open();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TitleScreen] 成長の記録を開けませんでした: " + ex.Message);
            }
        }

        /// <summary>
        /// 開いたままの「成長の記録」を閉じる(2026-07-27 追加)。記録画面は独立した
        /// ルートの GameObject なので、タイトル画面が消えても自動では片付かない。
        /// ゲーム画面の上に取り残されないよう、遷移時に明示的に畳む。
        /// </summary>
        void CloseGrowthHistory()
        {
            try
            {
                GrowthHistoryView.CloseIfOpen();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TitleScreen] 成長の記録を閉じられませんでした: " + ex.Message);
            }
        }

        /// <summary>
        /// フェードアウトを開始する。入力ブロック(レイキャスト・ボタン)は即時解除し、
        /// 下のゲームをすぐ操作可能にする(見た目のフェードだけが約0.4秒続く)。
        /// </summary>
        void BeginClose()
        {
            if (closing) return;
            closing = true;
            closeStartedAt = Time.unscaledTime;
            EndTitleIntroOnce();   // タイトルBGMイントロを通常BGMへ戻す(2026-07-22 追加)
            DismissUpdateCard();   // 「今回の更新」カードを即座に退場させる(2026-07-24 追加)
            CloseGrowthHistory();  // 開いたままの「成長の記録」を畳む(2026-07-27 追加)
            if (group != null)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// タイトルBGMイントロの終了を一度だけ GameAudio へ通知する(2026-07-22 Claude Code 追加)。
        /// GameAudio 側で約1.5秒かけて通常の時代BGMへクロスフェードで戻る。
        /// GameAudio が無い構成・イントロ未開始では何もしない(null安全)。
        /// </summary>
        void EndTitleIntroOnce()
        {
            if (!introStarted || introEnded) return;
            introEnded = true;
            var audio = GameAudio.Instance;
            if (audio != null) audio.EndTitleIntro();
        }

        /// <summary>
        /// 破棄時の安全網(2026-07-22 Claude Code 追加)。BeginClose を経由しない破棄
        /// (シーン破棄等)でもタイトルBGMイントロを終了させ、通常BGMが絞られたまま
        /// 残らないようにする。通常経路(BeginClose済み)では何もしない。
        /// </summary>
        void OnDestroy()
        {
            EndTitleIntroOnce();
            // BeginClose を経ずに破棄された経路(シーン遷移など)でも記録画面を取り残さない
            CloseGrowthHistory();
        }

        // ==================================================================
        // UI構築
        // ==================================================================

        void BuildUi()
        {
            var cgo = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;   // 既存の全UI(100〜145)より手前

            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 他パネルと同じ規約: EventSystem が未生成なら用意する(UIManager.Init 前でも安全)
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("TitleEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }

            group = cgo.AddComponent<CanvasGroup>();
            group.alpha = 1f;

            // 全画面の暗幕(60%)。raycastTarget=true(CreatePanelのImage既定)がマウス入力の
            // ブロッカーを兼ねる(クラス冒頭コメント参照)
            var overlay = UIStyle.CreatePanel(cgo.transform, "TitleOverlay", new Color(0f, 0f, 0f, 0.6f));
            UIStyle.StretchFull(overlay);

            // 世界史バナーのレターボックス帯(あれば)。タイトル文字の背景として約35%で敷く
            var bannerTexture = Resources.Load<Texture2D>("History/world_history_banner");
            if (bannerTexture != null)
            {
                var bannerGo = new GameObject("TitleBanner", typeof(RectTransform), typeof(RawImage));
                bannerGo.transform.SetParent(overlay.transform, false);
                var banner = bannerGo.GetComponent<RawImage>();
                banner.texture = bannerTexture;
                banner.uvRect = new Rect(0f, 0.26f, 1f, 0.48f);   // 横長帯として中央域を切り出す
                banner.color = new Color(1f, 1f, 1f, 0.35f);
                banner.raycastTarget = false;
                // ドリフト演出(2026-07-22 追加)のため、帯を左右±48px(96px)広げて中央基準で置く。
                // 往復振幅は±40pxなので、どの位置でも画面端に帯の切れ目は露出しない。
                bannerRect = UIStyle.SetRect(bannerGo, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 178f),
                    new Vector2(BannerDriftHalfRange * 2f + 16f, 210f));
                bannerBaseY = 178f;
            }

            var title = UIStyle.CreateText(overlay.transform, "TitleText", "HexCiv", 76,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            title.fontStyle = FontStyle.Bold;
            UIStyle.SetRect(title.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 206f), new Vector2(600f, 90f));
            titleText = title;   // 金色グロー演出の対象(2026-07-22 追加)

            var subtitle = UIStyle.CreateText(overlay.transform, "SubtitleText",
                "文明の歴史シミュレーション", 20, TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(subtitle.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 148f), new Vector2(600f, 30f));

            // ---- 縦並びメニュー ----
            const float buttonWidth = 340f;
            const float buttonHeight = 46f;
            const float buttonStep = 58f;
            float y = 42f;

            var continueButton = CreateMenuButton(overlay.transform, "ContinueButton",
                "つづける(クイックロード)", y, OnContinueClicked);
            // スロット1のセーブが存在する時のみ有効(UIManagerのスロット一覧と同じメタ読取)
            int savedTurn;
            string savedCivJa, savedAtIso;
            bool hasSlot1 = SaveLoad.TryReadMeta(SaveSlotPath(1),
                out savedTurn, out savedCivJa, out savedAtIso);
            if (!hasSlot1)
                hasSlot1 = HistoricalCampaignSave.TryReadMeta(SaveSlotPath(1),
                    out savedTurn, out savedCivJa, out savedAtIso);
            continueButton.interactable = hasSlot1;
            if (hasSlot1)
            {
                string metaJa = $"ターン{savedTurn}";
                if (!string.IsNullOrEmpty(savedCivJa)) metaJa += " " + savedCivJa;
                var meta = UIStyle.CreateText(continueButton.transform, "Meta", metaJa, 11,
                    TextAnchor.LowerCenter, UIStyle.TextDim);
                UIStyle.SetRect(meta.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 1f), new Vector2(0f, 14f));
            }
            y -= buttonStep;

            CreateMenuButton(overlay.transform, "HistoricalCampaignButton",
                "史実キャンペーン：ウルク—都市の夜明け", y,
                OnHistoricalCampaignClicked);
            y -= buttonStep;

            CreateMenuButton(overlay.transform, "NewGameButton", "新しいゲーム / 設定", y,
                OnNewGameClicked);
            y -= buttonStep;

            CreateMenuButton(overlay.transform, "SpectatorButton", "シミュレーション観戦", y,
                OnSpectatorClicked);
            y -= buttonStep;

            CreateMenuButton(overlay.transform, "PlayButton", "そのまま遊ぶ", y, OnPlayClicked);
            y -= buttonStep;

            // 終了(エディタでは Application.Quit が効かないため表示しない)
            if (!Application.isEditor)
            {
                CreateMenuButton(overlay.transform, "QuitButton", "終了", y, OnQuitClicked);
                y -= buttonStep;
            }

            var hint = UIStyle.CreateText(overlay.transform, "HintText", "Esc: そのまま遊ぶ", 13,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(hint.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, y - 6f), new Vector2(400f, 24f));

            // 右下のバージョン/クレジット表記(2026-07-21 追加)。小さな灰色文字。
            // raycastTarget は CreateText の既定でも無効だが、クリック透過の意図を明示する
            var credit = UIStyle.CreateText(overlay.transform, "VersionCreditText",
                "HexCiv v" + Version + " — 2026 / Claude Code × Codex 共同開発", 12,
                TextAnchor.LowerRight, UIStyle.TextDim);
            credit.raycastTarget = false;
            UIStyle.SetRect(credit.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-14f, 10f), new Vector2(460f, 18f));

            // 「成長の記録」ボタン(2026-07-27 追加)。左下に常設する。
            //
            // 「今回の更新」カードは差分があるときしか出ないため、そのままでは差分ゼロの日に
            // 過去の成長を振り返る導線が無くなる。この入口は**記録の有無にかかわらず常に出す**
            // (記録が空でも、その旨を説明する画面が開くだけで害はない)。
            // メニュー6個ぶんの出現アニメ枠(menuItemRects)は既に埋まっているので、
            // 右下のクレジットと対になる控えめな脇役として扱い、枠は消費しない。
            var growthButton = UIStyle.CreateButton(overlay.transform, "GrowthHistoryButton",
                "成長の記録", 13, OnGrowthHistoryClicked);
            UIStyle.SetRect(growthButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(14f, 10f), new Vector2(132f, 28f));

            // 「今回の更新」カード(2026-07-24 追加)。差分が無い/取得できない場合は何も作らない。
            // 計測(台帳の実行時カウント)に時間がかかっても演出の基準時刻がずれないよう、
            // shownAt の確定より前に実行する。
            TryBuildUpdateCard(overlay.transform);

            // 全UI構築完了=表示開始。ここを全演出(グロー/出現/ドリフト)の基準時刻にする
            // (2026-07-22 追加)
            shownAt = Time.unscaledTime;

            // ローカル関数: 中央揃えのメニューボタン1個(共通サイズ)
            Button CreateMenuButton(Transform parent, string name, string label, float posY,
                UnityEngine.Events.UnityAction onClick)
            {
                var b = UIStyle.CreateButton(parent, name, label, 17, onClick);
                UIStyle.SetRect(b.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, posY),
                    new Vector2(buttonWidth, buttonHeight));
                RegisterMenuItem(b.gameObject, posY);   // 出現アニメの対象に登録(2026-07-22 追加)
                return b;
            }
        }

        /// <summary>
        /// メニューボタンを出現アニメ(段階フェード+スライドイン)の対象として登録する
        /// (2026-07-22 Claude Code 追加)。CanvasGroup を付けて初期状態を透明+18px下に置く。
        /// 以後の進行は AnimateVisuals が担う。CanvasGroup は Button.interactable
        /// (つづけるボタンの無効化)や親CanvasGroupのフェードアウトと干渉しない。
        /// </summary>
        void RegisterMenuItem(GameObject go, float posY)
        {
            if (menuItemCount >= menuItemRects.Length) return;   // 想定6個を超えたら演出なしで表示
            var itemGroup = go.AddComponent<CanvasGroup>();
            itemGroup.alpha = 0f;
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = new Vector2(0f, posY - MenuSlideOffset);
            menuItemRects[menuItemCount] = rt;
            menuItemGroups[menuItemCount] = itemGroup;
            menuItemTargetY[menuItemCount] = posY;
            menuItemCount++;
        }

        /// <summary>
        /// スロットのセーブファイルパス。ファイル名規約は GameBootstrap.SaveSlotPath /
        /// UIManager.SaveSlotPath と同一に保つこと(表示用メタデータの読み取りにのみ使用)。
        /// </summary>
        static string SaveSlotPath(int slot)
        {
            return System.IO.Path.Combine(Application.persistentDataPath,
                $"hexciv_save_slot{slot}.json");
        }

        // ==================================================================
        // 「今回の更新」カード(2026-07-24 Claude Code 追加)
        //
        // 方針: このカードは「あれば得」の付加要素であり、絶対にタイトル画面の
        // ブロッカーにしない。差分の算出(UpdateDigest)から UI 構築まで全体を
        // try/catch で包み、失敗したら作りかけを片付けて従来どおりのタイトルに戻す。
        // ==================================================================

        /// <summary>
        /// 差分を取得し、変化があればカードを構築する。差分ゼロ・UpdateDigest 未着地・
        /// 算出中の例外・UI構築中の例外のいずれでも、カードを作らない(または作りかけを
        /// 破棄する)だけで、タイトル画面は従来と完全に同じ状態で動き続ける。
        /// </summary>
        void TryBuildUpdateCard(Transform parent)
        {
            try
            {
                List<DigestBridge.RowData> rows;
                int totalDelta;
                string summaryJa;
                if (!DigestBridge.TryCompute(out rows, out totalDelta, out summaryJa)) return;
                if (rows == null || rows.Count == 0) return;   // 差分ゼロ=何も描かない
                BuildUpdateCard(parent, rows, totalDelta, summaryJa);
                // カードが実際に組み上がってから基準値を確定する。ここへ到達する前に
                // 例外が出た場合は確定されないので、増分は次回起動へ持ち越される。
                DigestBridge.CommitPending();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TitleScreen] 「今回の更新」カードの生成を中止しました: " + ex.Message);
                DiscardPartialCard();
            }
        }

        /// <summary>構築途中で失敗したカードを完全に片付ける(従来のタイトル画面へ戻す)。</summary>
        void DiscardPartialCard()
        {
            digestRows.Clear();
            cardGroup = null;
            cardShowAt = -1f;
            cardDismissAt = -1f;
            if (cardRect != null)
            {
                Destroy(cardRect.gameObject);
                cardRect = null;
            }
        }

        /// <summary>
        /// カード本体を組み立てる。初期状態は画面外(右)・透明で、AnimateUpdateCard が
        /// 既存の出現演出の後にスライドインさせる。
        /// </summary>
        void BuildUpdateCard(Transform parent, List<DigestBridge.RowData> rows, int totalDelta,
            string summaryJa)
        {
            int visible = Mathf.Min(rows.Count, MaxVisibleRows);
            int extra = rows.Count - visible;
            bool anyClickable = false;
            for (int i = 0; i < visible; i++)
            {
                if (!string.IsNullOrEmpty(rows[i].PanelKey)) { anyClickable = true; break; }
            }

            summaryJa = Shorten(summaryJa, 24);   // カード幅に収まる1行へ丸める
            bool hasSummary = !string.IsNullOrEmpty(summaryJa);

            float height = CardPadding + CardHeaderHeight
                + (hasSummary ? CardSummaryHeight : 0f) + 6f
                + visible * CardRowHeight
                + (extra > 0 ? CardFooterHeight : 0f)
                + (anyClickable ? CardFooterHeight : 0f)
                + CardPadding;

            var cardGo = UIStyle.CreatePanel(parent, "UpdateDigestCard", CardBg);
            // 背景はクリックを受けない(下のメニューボタンのクリックを絶対に奪わないため)
            var cardImage = cardGo.GetComponent<Image>();
            if (cardImage != null) cardImage.raycastTarget = false;
            cardRect = UIStyle.SetRect(cardGo, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(CardOffscreenX, CardCenterY),
                new Vector2(CardWidth, height));
            cardGroup = cardGo.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0f;
            cardGroup.interactable = false;
            cardGroup.blocksRaycasts = false;   // スライドイン完了まで全体をクリック透過にする

            // 左端の金色アクセント帯(装飾のみ)
            var accentBar = UIStyle.CreatePanel(cardGo.transform, "AccentBar", UIStyle.Accent);
            var accentImage = accentBar.GetComponent<Image>();
            if (accentImage != null) accentImage.raycastTarget = false;
            UIStyle.SetRect(accentBar, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(3f, 0f));

            float y = CardPadding;

            var header = UIStyle.CreateText(cardGo.transform, "CardHeader", "今回の更新", 16,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            header.fontStyle = FontStyle.Bold;
            PlaceCardChild(header.gameObject, y, CardHeaderHeight);

            if (!hasSummary && totalDelta > 0)
            {
                // 要約が取れないときだけ、ヘッダー右に合計増分を出す(二重表示を避ける)
                var total = UIStyle.CreateText(cardGo.transform, "CardTotal",
                    "合計 +" + FormatCount(totalDelta), 13, TextAnchor.MiddleRight, CardDeltaColor);
                PlaceCardChild(total.gameObject, y, CardHeaderHeight);
            }
            y += CardHeaderHeight;

            if (hasSummary)
            {
                var summary = UIStyle.CreateText(cardGo.transform, "CardSummary", summaryJa, 11,
                    TextAnchor.MiddleLeft, UIStyle.TextDim);
                PlaceCardChild(summary.gameObject, y, CardSummaryHeight);
                y += CardSummaryHeight;
            }
            y += 6f;

            for (int i = 0; i < visible; i++)
            {
                BuildDigestRow(cardGo.transform, rows[i], i, y);
                y += CardRowHeight;
            }

            if (extra > 0)
            {
                var more = UIStyle.CreateText(cardGo.transform, "CardMore",
                    "ほか " + FormatCount(extra) + "件", 11, TextAnchor.MiddleLeft, UIStyle.TextDim);
                PlaceCardChild(more.gameObject, y, CardFooterHeight);
                y += CardFooterHeight;
            }

            if (anyClickable)
            {
                var hint = UIStyle.CreateText(cardGo.transform, "CardHint",
                    "行をクリックすると直接開きます", 11, TextAnchor.MiddleLeft, UIStyle.TextDim);
                PlaceCardChild(hint.gameObject, y, CardFooterHeight);
            }
        }

        /// <summary>
        /// カード1行を作る。パネルキーを持つ行はアイコン付きのボタン(クリックで直行)、
        /// 持たない行は素のコンテナ(クリック不可)。値は前回値から始め、演出側が
        /// 今回値までカウントアップする。
        /// </summary>
        void BuildDigestRow(Transform cardTransform, DigestBridge.RowData data, int index, float y)
        {
            bool clickable = !string.IsNullOrEmpty(data.PanelKey);
            GameObject rowGo;
            Text labelText;

            if (clickable)
            {
                string key = data.PanelKey;   // ラムダのキャプチャ用にループ外の複製を作る
                var button = UIStyle.CreateButton(cardTransform, "DigestRow" + index, "", 13,
                    () => OnDigestRowClicked(key));
                rowGo = button.gameObject;
                labelText = UIStyle.ButtonLabel(button);
            }
            else
            {
                rowGo = UIStyle.CreateContainer(cardTransform, "DigestRow" + index);
                labelText = UIStyle.CreateText(rowGo.transform, "Label", "", 13,
                    TextAnchor.MiddleLeft, UIStyle.TextDim);
            }
            PlaceCardChild(rowGo, y, CardRowHeight - 4f);

            float labelLeft = 6f;
            if (clickable)
            {
                // クリック可能な行の目印(新設パネルは星、既存パネルは本のアイコン)
                var sprite = UIStyle.CreateIconSprite(data.IsNewPanel ? "star" : "book");
                if (sprite != null)
                {
                    var iconGo = new GameObject("RowIcon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(rowGo.transform, false);
                    var icon = iconGo.GetComponent<Image>();
                    icon.sprite = sprite;
                    icon.raycastTarget = false;
                    icon.preserveAspect = true;
                    UIStyle.SetRect(iconGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(16f, 16f));
                    labelLeft = 26f;
                }
            }

            if (labelText != null)
            {
                // 「NEW」バッジがある行はバッジ位置(右から152px)に届かない長さまで詰める
                labelText.text = Shorten(data.LabelJa, data.IsNewPanel ? 7 : 9);
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.fontSize = 13;
                labelText.color = clickable ? UIStyle.TextMain : UIStyle.TextDim;
                UIStyle.SetRect(labelText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(labelLeft, 0f),
                    new Vector2(CardWidth - 2f * CardPadding - 190f - labelLeft, 22f));
            }

            if (data.IsNewPanel)
            {
                var badge = UIStyle.CreatePanel(rowGo.transform, "NewBadge", CardNewBadgeBg);
                var badgeImage = badge.GetComponent<Image>();
                if (badgeImage != null) badgeImage.raycastTarget = false;
                UIStyle.SetRect(badge, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f), new Vector2(-152f, 0f), new Vector2(34f, 15f));
                var badgeText = UIStyle.CreateText(badge.transform, "Text", "NEW", 10,
                    TextAnchor.MiddleCenter, Color.white);
                UIStyle.StretchFull(badgeText.gameObject);
            }

            var value = UIStyle.CreateText(rowGo.transform, "Value", FormatCount(data.Previous), 14,
                TextAnchor.MiddleRight, UIStyle.TextMain);
            UIStyle.SetRect(value.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-62f, 0f), new Vector2(86f, 22f));

            Text delta = null;
            int gain = data.Current - data.Previous;
            if (gain > 0)
            {
                // 増分はカウントアップ完了後にフェードインさせるため、初期状態は透明にする
                var deltaColor = CardDeltaColor;
                deltaColor.a = 0f;
                delta = UIStyle.CreateText(rowGo.transform, "Delta", "+" + FormatCount(gain), 12,
                    TextAnchor.MiddleRight, deltaColor);
                delta.fontStyle = FontStyle.Bold;
                UIStyle.SetRect(delta.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f), new Vector2(-2f, 0f), new Vector2(58f, 20f));
            }

            digestRows.Add(new DigestRowView
            {
                ValueText = value,
                DeltaText = delta,
                Previous = data.Previous,
                Current = data.Current,
                CountStartAt = float.MaxValue,   // 実際の開始時刻はカード出現時に確定する
            });
        }

        /// <summary>カード内の要素を「上端から topOffset だけ下、左右は内側余白」に置く。</summary>
        static void PlaceCardChild(GameObject go, float topOffset, float height)
        {
            UIStyle.SetRect(go, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -topOffset), new Vector2(-2f * CardPadding, height));
        }

        /// <summary>桁区切り付きの数値表記(1303 → "1,303")。ロケール非依存。</summary>
        static string FormatCount(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>長すぎる文字列を末尾「…」付きで丸める(null/空はそのまま返す)。</summary>
        static string Shorten(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || maxChars <= 1) return text;
            if (text.Length <= maxChars) return text;
            return text.Substring(0, maxChars - 1) + "…";
        }

        // ==================================================================
        // UpdateDigest への橋渡し(2026-07-24 Claude Code 追加)
        //
        // UI/UpdateDigest.cs は本ラウンドで別エージェントが新規作成中のため、
        // 型・メソッド名・戻り値の形状がこちらの想定と食い違う可能性がある。
        // そこで直接参照ではなくリフレクション経由で呼び、
        //   - 型が存在しない(未着地/名前変更)
        //   - ComputeAndCommit が無く ComputeOnly / Commit(result) に分かれている
        //   - フィールドではなくプロパティで公開されている
        //   - 算出中に例外が出る(台帳の読み取り失敗など)
        // のいずれでも false を返すだけにして、タイトル画面を従来どおり動かす。
        // ==================================================================
        static class DigestBridge
        {
            /// <summary>UpdateDigest から取り出した1行分(表示に必要な値だけを写し取る)。</summary>
            public struct RowData
            {
                public string LabelJa;
                public int Previous;
                public int Current;
                public string PanelKey;
                public bool IsNewPanel;
            }

            const BindingFlags StaticFlags =
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance;

            static bool resolved;
            static Type digestType;

            /// <summary>UpdateDigest 型を解決する(見つからなければ null。解決結果はキャッシュ)。</summary>
            static Type DigestType()
            {
                if (resolved) return digestType;
                resolved = true;
                try
                {
                    digestType = Type.GetType("HexCiv.UI.UpdateDigest", false);
                    if (digestType == null)
                        digestType = Type.GetType("HexCiv.UI.UpdateDigest, Assembly-CSharp", false);
                    if (digestType == null)
                    {
                        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                        for (int i = 0; i < assemblies.Length && digestType == null; i++)
                        {
                            try { digestType = assemblies[i].GetType("HexCiv.UI.UpdateDigest", false); }
                            catch { }
                        }
                    }
                }
                catch { digestType = null; }
                return digestType;
            }

            /// <summary>
            /// 差分を算出し(同時に今回値を保存し)、表示用の行データへ写し取る。
            /// 変化なし・算出不能・例外のいずれでも false を返し、呼び出し側は何も描かない。
            /// </summary>
            public static bool TryCompute(out List<RowData> rows, out int totalDelta,
                out string summaryJa)
            {
                rows = null;
                totalDelta = 0;
                summaryJa = null;

                var type = DigestType();
                if (type == null) return false;

                object result;
                try { result = InvokeCompute(type); }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TitleScreen] 更新差分の算出に失敗しました: " + ex.Message);
                    return false;
                }
                if (result == null) return false;

                try
                {
                    // HasChanges が明示的に false のときだけ打ち切る(欠落時は行数で判断する)
                    var hasChanges = GetMember(result, "HasChanges");
                    if (hasChanges is bool && !(bool)hasChanges) return false;

                    var newKeys = new HashSet<string>(StringComparer.Ordinal);
                    var keys = GetMember(result, "NewPanelKeys") as IEnumerable;
                    if (keys != null)
                    {
                        foreach (var key in keys)
                        {
                            var s = key as string;
                            if (!string.IsNullOrEmpty(s)) newKeys.Add(s);
                        }
                    }

                    var list = new List<RowData>();
                    var lines = GetMember(result, "Lines") as IEnumerable;
                    if (lines != null)
                    {
                        foreach (var line in lines)
                        {
                            if (line == null) continue;
                            var row = new RowData();
                            row.LabelJa = GetMember(line, "LabelJa") as string;
                            if (string.IsNullOrEmpty(row.LabelJa)) continue;
                            row.Previous = ToInt(GetMember(line, "Previous"));
                            row.Current = ToInt(GetMember(line, "Current"));
                            row.PanelKey = GetMember(line, "PanelKey") as string;
                            row.IsNewPanel = !string.IsNullOrEmpty(row.PanelKey)
                                && newKeys.Contains(row.PanelKey);
                            list.Add(row);
                        }
                    }
                    if (list.Count == 0) return false;

                    var total = GetMember(result, "TotalDelta");
                    totalDelta = ToInt(total);
                    if (totalDelta <= 0)
                    {
                        totalDelta = 0;
                        for (int i = 0; i < list.Count; i++)
                            totalDelta += Mathf.Max(0, list[i].Current - list[i].Previous);
                    }

                    summaryJa = BuildSummary(type, result);
                    rows = list;
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TitleScreen] 更新差分の読み取りに失敗しました: " + ex.Message);
                    rows = null;
                    return false;
                }
            }

            /// <summary>
            /// パネルを開く。UpdateDigest.TryOpenPanel(string) が無い/失敗した場合は false。
            /// 呼び出し側はタイトルを閉じた後なので、false でも「そのまま遊ぶ」と同じ結果になる。
            /// </summary>
            public static bool TryOpenPanel(string panelKey)
            {
                if (string.IsNullOrEmpty(panelKey)) return false;
                var type = DigestType();
                if (type == null) return false;
                try
                {
                    var method = type.GetMethod("TryOpenPanel", StaticFlags, null,
                        new[] { typeof(string) }, null);
                    if (method == null) return false;
                    object r = method.Invoke(null, new object[] { panelKey });
                    return !(r is bool) || (bool)r;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TitleScreen] パネルを開けませんでした: " + ex.Message);
                    return false;
                }
            }

            /// <summary>
            /// 差分算出の呼び出し。UpdateDigest 側の指定どおり
            /// ComputeOnly() + Commit(result) の2段を**優先**する。
            /// 一発の ComputeAndCommit() は分割APIが無い場合の代替に留める。
            ///
            /// 理由: 一発APIは差分を返す前に基準値を確定してしまうため、
            /// 直後のカード描画が例外で失敗すると(BuildUpdateCard の例外は
            /// 握り潰される)、その回の増分を二度と表示できなくなる。
            /// 2段なら表示が成立してから確定するので、失敗しても次回に持ち越せる。
            /// </summary>
            static object InvokeCompute(Type type)
            {
                pendingCommitType = null;
                pendingCommitResult = null;

                var compute = type.GetMethod("ComputeOnly", StaticFlags, null, Type.EmptyTypes, null);
                if (compute == null || compute.ReturnType == typeof(void))
                {
                    var direct = type.GetMethod("ComputeAndCommit", StaticFlags, null,
                        Type.EmptyTypes, null);
                    if (direct != null && direct.ReturnType != typeof(void))
                        return direct.Invoke(null, null);   // 分割APIが無い版。確定は即時になる
                }
                if (compute == null || compute.ReturnType == typeof(void))
                    compute = FindComputeFallback(type);
                if (compute == null) return null;

                object result = compute.Invoke(null, null);
                if (result == null) return null;

                // ここでは確定しない。カードが組み上がった後に CommitPending() で確定する。
                pendingCommitType = type;
                pendingCommitResult = result;
                return result;
            }

            static Type pendingCommitType;
            static object pendingCommitResult;

            /// <summary>
            /// 保留していた基準値の確定を実行する(カード生成が成功した後に呼ぶ)。
            /// 分割APIが無く既に確定済みの場合や、確定に失敗した場合も安全に無視する。
            /// </summary>
            public static void CommitPending()
            {
                var type = pendingCommitType;
                object result = pendingCommitResult;
                pendingCommitType = null;
                pendingCommitResult = null;
                if (type == null || result == null) return;

                try
                {
                    var methods = type.GetMethods(StaticFlags);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        if (methods[i].Name != "Commit") continue;
                        var parameters = methods[i].GetParameters();
                        if (parameters.Length != 1) continue;
                        if (!parameters[0].ParameterType.IsInstanceOfType(result)) continue;
                        methods[i].Invoke(null, new[] { result });
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TitleScreen] 更新差分の確定に失敗しました: " + ex.Message);
                }
            }

            /// <summary>引数なしで値を返す "Compute" を含む公開静的メソッドを探す(最後の手段)。</summary>
            static MethodInfo FindComputeFallback(Type type)
            {
                var methods = type.GetMethods(StaticFlags);
                for (int i = 0; i < methods.Length; i++)
                {
                    var method = methods[i];
                    if (method.ReturnType == typeof(void)) continue;
                    if (method.GetParameters().Length != 0) continue;
                    if (method.Name.IndexOf("Compute", StringComparison.Ordinal) < 0) continue;
                    return method;
                }
                return null;
            }

            /// <summary>BuildSummaryJa(result) があれば1行要約を取得する(無ければ null)。</summary>
            static string BuildSummary(Type type, object result)
            {
                try
                {
                    var methods = type.GetMethods(StaticFlags);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        var method = methods[i];
                        if (method.Name != "BuildSummaryJa") continue;
                        if (method.ReturnType != typeof(string)) continue;
                        var parameters = method.GetParameters();
                        if (parameters.Length != 1) continue;
                        if (!parameters[0].ParameterType.IsInstanceOfType(result)) continue;
                        return method.Invoke(null, new[] { result }) as string;
                    }
                }
                catch { }
                return null;
            }

            /// <summary>公開フィールド、無ければ公開プロパティの値を読む(いずれも無ければ null)。</summary>
            static object GetMember(object target, string name)
            {
                if (target == null || string.IsNullOrEmpty(name)) return null;
                try
                {
                    var type = target.GetType();
                    var field = type.GetField(name, InstanceFlags);
                    if (field != null) return field.GetValue(target);
                    var property = type.GetProperty(name, InstanceFlags);
                    if (property != null && property.CanRead) return property.GetValue(target, null);
                }
                catch { }
                return null;
            }

            /// <summary>数値らしきオブジェクトを int へ(変換できなければ0)。</summary>
            static int ToInt(object value)
            {
                if (value is int) return (int)value;
                if (value == null) return 0;
                try { return Convert.ToInt32(value, CultureInfo.InvariantCulture); }
                catch { return 0; }
            }
        }
    }
}
