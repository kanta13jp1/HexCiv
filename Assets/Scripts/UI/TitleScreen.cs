using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
            if (group != null)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
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
                UIStyle.SetRect(bannerGo, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 178f), new Vector2(0f, 210f));
            }

            var title = UIStyle.CreateText(overlay.transform, "TitleText", "HexCiv", 76,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            title.fontStyle = FontStyle.Bold;
            UIStyle.SetRect(title.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 206f), new Vector2(600f, 90f));

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

            // ローカル関数: 中央揃えのメニューボタン1個(共通サイズ)
            Button CreateMenuButton(Transform parent, string name, string label, float posY,
                UnityEngine.Events.UnityAction onClick)
            {
                var b = UIStyle.CreateButton(parent, name, label, 17, onClick);
                UIStyle.SetRect(b.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(0f, posY),
                    new Vector2(buttonWidth, buttonHeight));
                return b;
            }
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
