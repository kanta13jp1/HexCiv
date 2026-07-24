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
            if (Input.GetKeyDown(KeyCode.Escape)) BeginClose();
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
        /// フェードアウトを開始する。入力ブロック(レイキャスト・ボタン)は即時解除し、
        /// 下のゲームをすぐ操作可能にする(見た目のフェードだけが約0.4秒続く)。
        /// </summary>
        void BeginClose()
        {
            if (closing) return;
            closing = true;
            closeStartedAt = Time.unscaledTime;
            EndTitleIntroOnce();   // タイトルBGMイントロを通常BGMへ戻す(2026-07-22 追加)
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
    }
}
