using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Audio;
using HexCiv.Control;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// 戦史年表(クロニクル)パネル(2026-07-21 Claude Code 追加)。
    /// ゲーム中の大事件(宣戦布告・和平・都市陥落・文明滅亡・勝敗決定)を
    /// GameState の型付きイベント(Core/GameEvents.cs)から発生時に記録し、
    /// ターン番号付きの年表として閲覧できる独立Canvas UI。
    ///
    /// MinimapPanel と同じ独立Canvas方式: UIManager / GameBootstrap は変更せず、
    /// TurnManager 構築時に Bind される CultureSystem.CurrentState を読み取り窓口に使う。
    /// state オブジェクトの差し替わり(新規開始・リスタート・ロード・文明変更)を
    /// Update で検知して旧stateのイベント購読を解除→新stateへ再購読し、記録をクリアする。
    /// 記録はイベント駆動の追記のみ(リングバッファ最大200件)。毎フレームの仕事は
    /// キー入力の確認と、表示中かつdirty時の可視行の再描画だけ。
    ///
    /// 表示: 中央パネル、10行/ページの簡易ページング(前/次ページ)、新しい事件ほど後ろ
    /// (最終ページの下端が最新)。各行は左端の色付きアクセントバー(宣戦=赤、和平=緑、
    /// 陥落=橙、滅亡=灰、勝利=金)+ターン番号列+本文。
    ///
    /// 開閉: Cキー(InputField入力中は無視)/左下の「年表」ボタン/×ボタン/Esc。
    /// Esc は WorldHistoryPanel 等の他の独立パネルと同様に自前の Update で処理する。
    ///
    /// Z順: Canvas sortingOrder=145(直近の規約 20以上)。ゲームオーバーのオーバーレイは
    /// UIManager のメインCanvas(sortingOrder=100)上にあり、Codexの独立パネル群は
    /// 130/135/140 のため、それらすべてより手前の145で「終了画面の上でも開ける」を保証する。
    /// 常時表示の「年表」ボタンだけは UIManager の Z順規約(2026-07-21)に合わせて
    /// ネストCanvas(overrideSorting, sortingOrder=-5)へ退避し、開いている
    /// どのモーダルパネルの上にも浮かないようにする。
    /// シミュレーションへは完全に読み取り専用(イベント購読は表示側のみ・no-op安全)。
    ///
    /// 歴史ツアー(2026-07-21 深夜 Claude Code 追加): 各記録に「意味のある世界座標」も
    /// 併せて保存する(陥落=当該都市 / 宣戦・和平=攻撃側(当事者A)の首都 / 滅亡=最後に
    /// 確認できた首都 / 終戦=勝者の首都。首都は各記録時のスナップショットで補完し、
    /// 場所が定まらない記録は座標なし)。ヘッダーの「歴史ツアー」ボタン(または
    /// ChroniclePanel.StartTourIfAvailable() — UIManager の終了画面ボタンが呼ぶ)で
    /// パネルを閉じ、座標付きの記録を時系列にカメラで巡る。1件あたり約1.6秒(非スケール
    /// 時間)で、専用の最前面Canvas(sortingOrder=150)へ大きなラベルをフェード表示する。
    /// Esc または任意クリックで中断、最後は「ツアー終了」を短く表示する。
    /// カメラ移動は CameraController.FocusOn の再利用(MinimapPanel と同じ取得方法)で、
    /// シミュレーション状態には一切影響しない。
    ///
    /// 書き出し(2026-07-22 Claude Code 追加): ヘッダーの「書き出し」ボタンで記録全件を
    /// Application.persistentDataPath/chronicles/hexciv_chronicle_yyyyMMdd_HHmmss.txt へ
    /// UTF-8テキストとして保存する(ヘッダーに日時と参加文明、本文は1行1件「ターンN: 本文」)。
    /// 完了時はパネル上の「書き出しました」ラベルをフェード表示し、UIManager が居れば
    /// ゲーム内ログにも通知する(F12スクリーンショット保存と同じ流儀)。読み取り専用。
    /// </summary>
    public sealed class ChroniclePanel : MonoBehaviour
    {
        /// <summary>記録の上限件数(リングバッファ。超過分は最古から捨てる)。</summary>
        const int MaxEntries = 200;
        /// <summary>1ページの行数(Codexのパネル群と同じ簡易ページング)。</summary>
        const int RowsPerPage = 10;

        // ---- 歴史ツアー(2026-07-21 Claude Code 追加) ----
        /// <summary>ツアー1件あたりの表示時間(秒、非スケール時間。高速観戦中も一定)。</summary>
        const float TourStepSeconds = 1.6f;
        /// <summary>終了表示「ツアー終了」の表示時間(秒)。</summary>
        const float TourEndSeconds = 1.1f;
        /// <summary>ラベルのフェードイン時間(秒)。</summary>
        const float TourFadeInSeconds = 0.25f;
        /// <summary>ラベルのフェードアウト時間(秒)。</summary>
        const float TourFadeOutSeconds = 0.3f;

        // ---- 書き出し(2026-07-22 Claude Code 追加) ----
        /// <summary>書き出し確認ラベルを不透明のまま保つ時間(秒、非スケール時間)。</summary>
        const float ExportNoticeHoldSeconds = 1.2f;
        /// <summary>書き出し確認ラベルのフェードアウト時間(秒)。</summary>
        const float ExportNoticeFadeSeconds = 0.7f;

        // ---- 事件種別ごとのアクセント色 ----
        static readonly Color WarColor     = UIStyle.Danger;                          // 宣戦=赤
        static readonly Color PeaceColor   = new Color(0.38f, 0.78f, 0.44f, 1f);      // 和平=緑
        static readonly Color CaptureColor = new Color(0.95f, 0.60f, 0.25f, 1f);      // 陥落=橙
        static readonly Color ElimColor    = new Color(0.55f, 0.58f, 0.63f, 1f);      // 滅亡=灰
        static readonly Color VictoryColor = UIStyle.Accent;                          // 勝利=金
        static readonly Color DawnColor    = new Color(0.55f, 0.74f, 0.95f, 1f);      // 開幕=空色

        /// <summary>年表の1項目。ターン番号・本文・アクセント色と、意味のある場所があれば
        /// その世界座標(HasCoord=true の時のみ有効。歴史ツアーの巡回先)を保持する。</summary>
        struct Entry
        {
            public int Turn;
            public string Text;
            public Color Accent;
            public HexCoord Coord;
            public bool HasCoord;
        }

        Canvas canvas;
        GameObject panel;
        Text pageText;
        Button prevButton;
        Button nextButton;

        // ---- 書き出し(2026-07-22 Claude Code 追加) ----
        /// <summary>「書き出しました」確認ラベル(ヘッダー右側。表示後にフェードアウト)。</summary>
        Text exportNoticeText;
        /// <summary>確認ラベルの表示開始時刻(Time.unscaledTime。負=非表示)。</summary>
        float exportNoticeShownAt = -1f;
        /// <summary>ゲーム内ログ通知先(CameraController と同じシーン検索+キャッシュ方式)。</summary>
        UIManager uiManager;

        // ---- 歴史ツアー(2026-07-21 Claude Code 追加) ----
        /// <summary>ツアーラベル専用の最前面Canvas(sortingOrder=150)。</summary>
        Canvas tourCanvas;
        GameObject tourLabelRoot;
        CanvasGroup tourLabelGroup;
        Text tourLabelText;
        /// <summary>ツアー中か。ツアー中は通常のキー処理を止め、Esc/クリックで中断する。</summary>
        bool tourActive;
        /// <summary>終了表示「ツアー終了」の段階か。</summary>
        bool tourEndPhase;
        int tourIndex;
        float tourStepStartedAt;
        readonly List<Entry> tourEntries = new List<Entry>();
        /// <summary>カメラ制御(MinimapPanel と同じシーン検索+キャッシュ方式)。</summary>
        CameraController cameraController;
        /// <summary>プレイヤーIdごとの「最後に確認できた首都座標」(滅亡記録の場所補完に使う)。</summary>
        readonly Dictionary<int, HexCoord> lastKnownCapitalCoords = new Dictionary<int, HexCoord>();

        // 10行分の再利用ビュー(内容だけを差し替える)
        readonly GameObject[] rowRoots = new GameObject[RowsPerPage];
        readonly Image[] rowAccents = new Image[RowsPerPage];
        readonly Text[] rowTurnTexts = new Text[RowsPerPage];
        readonly Text[] rowBodyTexts = new Text[RowsPerPage];

        readonly List<Entry> entries = new List<Entry>();
        /// <summary>現在イベント購読中の状態(差し替え検知に使う)。</summary>
        GameState boundState;
        /// <summary>表示中のページ(0始まり)。最終ページの下端が最新の事件。</summary>
        int page;
        /// <summary>表示内容の再構築が必要か(表示中のみ Update で消化する)。</summary>
        bool rowsDirty;

        /// <summary>
        /// 年表パネルの表示を UIManager のモーダル計数へ通知済みか(2026-07-22 Claude Code 追加)。
        /// イベントバナーが年表パネルへ重なるのを防ぐため、開閉を必ず対で通知する
        /// (Cキー・「年表」ボタン・×・Esc・歴史ツアー開始による非表示の全経路をポーリングで捕捉)。
        /// </summary>
        bool externalPanelNotified;

        // 購読解除のために保持するイベントハンドラ(Awakeで一度だけ生成)
        System.Action<Player, Player> warHandler;
        System.Action<Player, Player> peaceHandler;
        System.Action<City, Player, Player> captureHandler;
        System.Action<Player> eliminatedHandler;
        System.Action<Player, string> gameEndedHandler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<ChroniclePanel>() != null) return;
            new GameObject("ChronicleUI").AddComponent<ChroniclePanel>();
        }

        void Awake()
        {
            // 各記録に「意味のある世界座標」も併せて保存する(歴史ツアーの巡回先。2026-07-21 追加):
            // 宣戦・和平=攻撃側(当事者A)の首都 / 陥落=当該都市 / 滅亡=最後に確認できた首都 /
            // 終戦=勝者の首都。求められない場合は座標なし(ツアーではスキップ)。
            warHandler = (aggressor, defender) =>
                Record($"⚔ ターン{CurrentTurn()}: {NameOf(aggressor)}が{NameOf(defender)}に宣戦布告", WarColor,
                    CapitalCoordOf(aggressor));
            peaceHandler = (a, b) =>
                Record($"🕊 ターン{CurrentTurn()}: {NameOf(a)}と{NameOf(b)}が和平", PeaceColor,
                    CapitalCoordOf(a));
            captureHandler = (city, oldOwner, newOwner) =>
                Record($"🏰 ターン{CurrentTurn()}: 都市「{(city != null ? city.NameJa : "?")}」陥落 → {NameOf(newOwner)}", CaptureColor,
                    city != null ? city.Coord : (HexCoord?)null);
            eliminatedHandler = p =>
                Record($"☠ ターン{CurrentTurn()}: {NameOf(p)}が滅亡", ElimColor,
                    CapitalCoordOf(p));
            gameEndedHandler = (winner, messageJa) =>
                Record($"👑 ターン{CurrentTurn()}: {messageJa}", VictoryColor,
                    CapitalCoordOf(winner));
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
            BuildTourLabel();
        }

        void Update()
        {
            // 状態の差し替わり(新規/リスタート/ロード/文明変更)を検知して再購読
            var current = CultureSystem.CurrentState;
            if (current != boundState)
            {
                if (tourActive) StopTour();   // 旧ゲームのツアーは中断(記録もクリアされる)
                Rebind(current);
            }

            // 年表パネル表示中はイベントバナーを退避させる(ツアー中は Hide 済みで非表示=退避解除。
            // 早期returnより前に評価して全経路で対称に通知する。2026-07-22 追加)
            SyncModalNotify();

            if (panel == null) return;

            // 歴史ツアー中は巡回・フェード・中断(Esc/クリック)のみを処理する(2026-07-21 追加)
            if (tourActive)
            {
                UpdateTour();
                return;
            }

            // Cキーで開閉(シード入力欄などのInputFieldへ入力中は無視)
            if (Input.GetKeyDown(KeyCode.C) && !IsTextInputFocused())
            {
                if (panel.activeSelf) Hide();
                else Show();
            }

            // Esc で閉じる(他の独立パネル(WorldHistoryPanel等)と同じく自前で処理)
            if (panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                Hide();

            // 表示中かつdirtyの時だけ可視行を再描画(イベント駆動・毎フレーム仕事なし)
            if (panel.activeSelf && rowsDirty)
                RefreshRows();

            // 書き出し確認ラベルの保持→フェードアウト(表示中のみ動作。2026-07-22 追加)
            UpdateExportNotice();
        }

        void OnDestroy()
        {
            Unbind();
            // 表示中に破棄された場合でも計数を必ず戻す(退避カウンタの取り残し防止。2026-07-22 追加)
            if (externalPanelNotified)
            {
                externalPanelNotified = false;
                UIManager.NotifyExternalPanel(false);
            }
        }

        /// <summary>
        /// 年表パネルの表示状態を UIManager のモーダル計数へ反映する(2026-07-22 Claude Code 追加)。
        /// 開閉の全経路を毎フレームのポーリングで捕捉し、必ず true/false を対で通知する。
        /// UIManager.NotifyExternalPanel は静的で、ヘッドレスや UIManager 不在でも null 安全。
        /// </summary>
        void SyncModalNotify()
        {
            bool visible = panel != null && panel.activeSelf;
            if (visible == externalPanelNotified) return;
            externalPanelNotified = visible;
            UIManager.NotifyExternalPanel(visible);
        }

        // ==================================================================
        // 記録(イベント購読)
        // ==================================================================

        /// <summary>旧stateの購読を解除し、新stateへ購読し直して記録をクリアする。</summary>
        void Rebind(GameState next)
        {
            Unbind();
            boundState = next;
            entries.Clear();
            lastKnownCapitalCoords.Clear();   // 首都スナップショットも新しいゲームのものへ(2026-07-21 追加)
            page = 0;
            if (boundState != null)
            {
                boundState.OnWarDeclared += warHandler;
                boundState.OnPeaceMade += peaceHandler;
                boundState.OnCityCaptured += captureHandler;
                boundState.OnPlayerEliminated += eliminatedHandler;
                boundState.OnGameEnded += gameEndedHandler;
                // 開幕の1行(新規・リスタート・ロードのいずれでも年表の起点として置く)
                entries.Add(new Entry { Turn = 1, Text = "🌅 ターン1: 文明の夜明け", Accent = DawnColor });
            }
            rowsDirty = true;
        }

        void Unbind()
        {
            if (boundState == null) return;
            boundState.OnWarDeclared -= warHandler;
            boundState.OnPeaceMade -= peaceHandler;
            boundState.OnCityCaptured -= captureHandler;
            boundState.OnPlayerEliminated -= eliminatedHandler;
            boundState.OnGameEnded -= gameEndedHandler;
            boundState = null;
        }

        /// <summary>事件を1件追記する(上限超過は最古を捨てる)。閲覧中に最新ページへ追従する。
        /// coord: 歴史ツアーの巡回先になる世界座標(null=場所なし。2026-07-21 追加)。</summary>
        void Record(string text, Color accent, HexCoord? coord = null)
        {
            // 追記前に最終ページを見ていたら、追記後も最新へ追従する
            bool atNewest = page >= PageCount() - 1;
            entries.Add(new Entry
            {
                Turn = CurrentTurn(),
                Text = text,
                Accent = accent,
                Coord = coord ?? default,
                HasCoord = coord.HasValue,
            });
            while (entries.Count > MaxEntries) entries.RemoveAt(0);
            if (atNewest) page = PageCount() - 1;
            rowsDirty = true;
            SnapshotCapitals();   // 滅亡記録の場所補完用に、事件のたびに全首都を控える(2026-07-21 追加)
        }

        int CurrentTurn() => boundState != null ? boundState.TurnNumber : 0;

        static string NameOf(Player p) => p != null ? p.NameJa : "?";

        // ==================================================================
        // 首都座標の解決(歴史ツアー用。2026-07-21 Claude Code 追加)
        // ==================================================================

        /// <summary>
        /// プレイヤーの首都座標を返す(読み取りのみ)。生きた首都があればその座標
        /// (同時にスナップショットも更新)、無ければ「最後に確認できた首都」
        /// (lastKnownCapitalCoords)、それも無ければ null(=記録に場所なし)。
        /// </summary>
        HexCoord? CapitalCoordOf(Player p)
        {
            if (p == null) return null;
            var capital = FindCapitalCity(p);
            if (capital != null)
            {
                lastKnownCapitalCoords[p.Id] = capital.Coord;
                return capital.Coord;
            }
            HexCoord last;
            if (lastKnownCapitalCoords.TryGetValue(p.Id, out last)) return last;
            return null;
        }

        /// <summary>首都都市(CapitalCityId、見つからなければ先頭都市)を返す。無ければ null。</summary>
        static City FindCapitalCity(Player p)
        {
            if (p == null || p.Cities == null) return null;
            for (int i = 0; i < p.Cities.Count; i++)
            {
                var c = p.Cities[i];
                if (c != null && c.Id == p.CapitalCityId) return c;
            }
            return p.Cities.Count > 0 ? p.Cities[0] : null;
        }

        /// <summary>全プレイヤーの現在の首都座標をスナップショットへ控える(数プレイヤー分のみで軽量)。</summary>
        void SnapshotCapitals()
        {
            if (boundState == null || boundState.Players == null) return;
            for (int i = 0; i < boundState.Players.Count; i++)
            {
                var p = boundState.Players[i];
                var capital = FindCapitalCity(p);
                if (capital != null) lastKnownCapitalCoords[p.Id] = capital.Coord;
            }
        }

        int PageCount() => Mathf.Max(1, (entries.Count + RowsPerPage - 1) / RowsPerPage);

        // ==================================================================
        // UI構築
        // ==================================================================

        void BuildCanvas()
        {
            var go = new GameObject("ChronicleCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // クラス冒頭コメント参照: ゲームオーバーオーバーレイ(UIManagerのCanvas=100)や
            // Codexの独立パネル(130/135/140)より明確に手前へ(通常・観戦・終了画面すべてで開ける)
            canvas.sortingOrder = 145;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("ChronicleEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        /// <summary>
        /// 左下の「年表」開閉ボタン。既存の独立パネルボタン3つ
        /// (世界史図鑑 x=10 / 文化・政策 x=178 / 遺産・偉人・作品 x=346、y=176、高さ36)の
        /// 右隣(x=514)へ同じ段・同じ高さで置く小型ボタン。UIManager は変更しない。
        /// </summary>
        void BuildOpenButton()
        {
            var button = UIStyle.CreateButton(canvas.transform, "ChronicleButton", "年表", 15, Show);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(514f, 176f), new Vector2(92f, 36f));
            DemoteBehindPanels(button.gameObject);
        }

        /// <summary>
        /// 常時表示ボタンを最背面のネストCanvas(sortingOrder=-5)へ退避する。
        /// UIManager の Z順修正(2026-07-21)と同じ規約: これで本Canvas(145)にあるボタンが
        /// 開いているモーダルパネルの上へ浮かない。GraphicRaycaster併設でクリックは従来どおり。
        /// </summary>
        static void DemoteBehindPanels(GameObject go)
        {
            if (go == null || go.GetComponent<Canvas>() != null) return;
            var nested = go.AddComponent<Canvas>();
            nested.overrideSorting = true;
            nested.sortingOrder = -5;
            go.AddComponent<GraphicRaycaster>();
        }

        void BuildPanel()
        {
            panel = UIStyle.CreatePanel(canvas.transform, "ChroniclePanel",
                new Color(0.055f, 0.07f, 0.105f, 0.97f));
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(580f, 478f));

            var title = UIStyle.CreateText(panel.transform, "Title", "戦史年表", 20,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-80f, 28f));

            var close = UIStyle.CreateButton(panel.transform, "Close", "×", 16, Hide);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(28f, 28f));

            // 歴史ツアー(2026-07-21 追加): ヘッダー左端。パネルを閉じ、座標付きの記録を
            // 時系列にカメラで巡る(Esc/クリックで中断)。
            var tour = UIStyle.CreateButton(panel.transform, "TourButton", "歴史ツアー", 14, StartTour);
            UIStyle.SetRect(tour.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(6f, -6f), new Vector2(100f, 28f));

            // 書き出し(2026-07-22 追加): 「歴史ツアー」の右隣。記録全件をUTF-8テキストへ保存する。
            var export = UIStyle.CreateButton(panel.transform, "ExportButton", "書き出し", 14, ExportChronicle);
            UIStyle.SetRect(export.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(110f, -6f), new Vector2(86f, 28f));

            // 書き出し確認ラベル(ヘッダー右側・×ボタンの左。書き出し直後だけフェード表示)
            exportNoticeText = UIStyle.CreateText(panel.transform, "ExportNotice", "", 13,
                TextAnchor.MiddleRight, PeaceColor);
            UIStyle.SetRect(exportNoticeText.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-40f, -6f), new Vector2(170f, 28f));
            exportNoticeText.gameObject.SetActive(false);

            // 10行の再利用ビュー(左からアクセントバー・ターン列・本文)
            for (int i = 0; i < RowsPerPage; i++)
            {
                var row = UIStyle.CreatePanel(panel.transform, "Row" + i,
                    new Color(0.10f, 0.13f, 0.18f, 0.55f));
                UIStyle.SetRect(row, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -46f - i * 38f), new Vector2(-28f, 34f));

                var accentGo = new GameObject("Accent", typeof(RectTransform), typeof(Image));
                accentGo.transform.SetParent(row.transform, false);
                var accent = accentGo.GetComponent<Image>();
                accent.raycastTarget = false;
                UIStyle.SetRect(accentGo, new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(5f, 0f));

                var turnText = UIStyle.CreateText(row.transform, "Turn", "", 13,
                    TextAnchor.MiddleRight, UIStyle.TextDim);
                UIStyle.SetRect(turnText.gameObject, new Vector2(0f, 0f), new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(72f, 0f));

                var bodyText = UIStyle.CreateText(row.transform, "Body", "", 14,
                    TextAnchor.MiddleLeft, UIStyle.TextMain);
                var brt = (RectTransform)bodyText.transform;
                brt.anchorMin = new Vector2(0f, 0f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.offsetMin = new Vector2(94f, 0f);
                brt.offsetMax = new Vector2(-6f, 0f);
                bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;   // 長い勝利メッセージも枠内に収める
                bodyText.verticalOverflow = VerticalWrapMode.Truncate;

                rowRoots[i] = row;
                rowAccents[i] = accent;
                rowTurnTexts[i] = turnText;
                rowBodyTexts[i] = bodyText;
            }

            // ページング(前/次ページ+ページ表示)
            prevButton = UIStyle.CreateButton(panel.transform, "PrevPage", "前ページ", 14,
                () => ChangePage(-1));
            UIStyle.SetRect(prevButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(14f, 10f), new Vector2(110f, 32f));

            nextButton = UIStyle.CreateButton(panel.transform, "NextPage", "次ページ", 14,
                () => ChangePage(1));
            UIStyle.SetRect(nextButton.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-14f, 10f), new Vector2(110f, 32f));

            pageText = UIStyle.CreateText(panel.transform, "PageLabel", "", 14,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(pageText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(200f, 32f));

            panel.SetActive(false);
        }

        // ==================================================================
        // 表示
        // ==================================================================

        public void Show()
        {
            if (panel == null) return;
            if (!panel.activeSelf) GameAudio.Instance?.PlayPanelOpen();   // 開く時のみページ音(2026-07-21 追加)
            page = PageCount() - 1;   // 開いた時は最新(最終ページ)を表示
            panel.SetActive(true);
            RefreshRows();
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        void ChangePage(int delta)
        {
            page = Mathf.Clamp(page + delta, 0, PageCount() - 1);
            RefreshRows();
        }

        /// <summary>現在ページの行ビューへ記録内容を流し込む(古い順に上から下へ、最新は最終ページ下端)。</summary>
        void RefreshRows()
        {
            rowsDirty = false;
            int pageCount = PageCount();
            page = Mathf.Clamp(page, 0, pageCount - 1);
            int start = page * RowsPerPage;

            for (int i = 0; i < RowsPerPage; i++)
            {
                int idx = start + i;
                bool has = idx < entries.Count;
                if (rowRoots[i] != null) rowRoots[i].SetActive(has);
                if (!has) continue;
                var e = entries[idx];
                rowAccents[i].color = e.Accent;
                rowTurnTexts[i].text = e.Turn.ToString();
                rowBodyTexts[i].text = e.Text;
            }

            if (pageText != null) pageText.text = $"{page + 1} / {pageCount} ページ";
            if (prevButton != null) prevButton.interactable = page > 0;
            if (nextButton != null) nextButton.interactable = page < pageCount - 1;
        }

        // ==================================================================
        // 書き出し(2026-07-22 Claude Code 追加)
        // ==================================================================

        /// <summary>
        /// 年表の全記録をUTF-8テキストへ書き出す(記録の読み取りのみ。シミュレーションには
        /// 一切影響しない)。保存先: persistentDataPath/chronicles/
        /// hexciv_chronicle_yyyyMMdd_HHmmss.txt(フォルダは無ければ作成)。
        /// ヘッダーに書き出し日時と参加文明、本文は1行1件「ターンN: 本文」。
        /// 完了時は「書き出しました」ラベルをフェード表示し、UIManager が見つかれば
        /// ゲーム内ログにも通知する(F12スクリーンショット保存と同じ流儀)。
        /// 失敗してもゲーム進行には影響させない(警告ログ+失敗表示のみ)。
        /// </summary>
        void ExportChronicle()
        {
            try
            {
                string dir = System.IO.Path.Combine(Application.persistentDataPath, "chronicles");
                System.IO.Directory.CreateDirectory(dir);
                string file = System.IO.Path.Combine(dir,
                    "hexciv_chronicle_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("HexCiv 戦史年表");
                sb.AppendLine("書き出し日時: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                if (boundState != null && boundState.Players != null && boundState.Players.Count > 0)
                {
                    sb.Append("参加文明: ");
                    for (int i = 0; i < boundState.Players.Count; i++)
                    {
                        if (i > 0) sb.Append("、");
                        var p = boundState.Players[i];
                        sb.Append(p != null ? p.NameJa : "?");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("----------------------------------------");
                for (int i = 0; i < entries.Count; i++)
                    sb.AppendLine("ターン" + entries[i].Turn + ": " + ExportBody(entries[i]));

                System.IO.File.WriteAllText(file, sb.ToString(), System.Text.Encoding.UTF8);
                Debug.Log("年表を書き出しました: " + file);
                ShowExportNotice("書き出しました");
                NotifyLog("年表を書き出しました");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("年表の書き出しに失敗しました: " + e.Message);
                ShowExportNotice("書き出しに失敗しました");
            }
        }

        /// <summary>
        /// 記録本文から行頭列と二重になる「ターンN: 」表記だけを取り除いた書き出し用本文を返す
        /// (本文は「⚔ ターン12: …」形式のため。絵文字などの前置部分は保持し、
        /// 見つからない場合は本文をそのまま返す)。
        /// </summary>
        static string ExportBody(Entry e)
        {
            if (string.IsNullOrEmpty(e.Text)) return "";
            string marker = "ターン" + e.Turn + ": ";
            int idx = e.Text.IndexOf(marker, System.StringComparison.Ordinal);
            if (idx < 0) return e.Text;
            return e.Text.Substring(0, idx) + e.Text.Substring(idx + marker.Length);
        }

        /// <summary>確認ラベルを不透明で表示する(以後は Update の UpdateExportNotice がフェードさせる)。</summary>
        void ShowExportNotice(string messageJa)
        {
            if (exportNoticeText == null) return;
            exportNoticeText.text = messageJa;
            var c = exportNoticeText.color;
            c.a = 1f;
            exportNoticeText.color = c;
            exportNoticeText.gameObject.SetActive(true);
            exportNoticeShownAt = Time.unscaledTime;
        }

        /// <summary>確認ラベルの保持→フェードアウト→非表示(非スケール時間基準・非表示時は即return)。</summary>
        void UpdateExportNotice()
        {
            if (exportNoticeText == null || exportNoticeShownAt < 0f) return;
            float t = Time.unscaledTime - exportNoticeShownAt;
            if (t >= ExportNoticeHoldSeconds + ExportNoticeFadeSeconds)
            {
                exportNoticeShownAt = -1f;
                exportNoticeText.gameObject.SetActive(false);
                return;
            }
            var c = exportNoticeText.color;
            c.a = t <= ExportNoticeHoldSeconds
                ? 1f
                : Mathf.Clamp01(1f - (t - ExportNoticeHoldSeconds) / ExportNoticeFadeSeconds);
            exportNoticeText.color = c;
        }

        /// <summary>
        /// UIManager のゲーム内ログへ通知する(CameraController と同じシーン検索+キャッシュ方式。
        /// リスタートで破棄された参照は Unity の null 判定で再検索される。見つからなければ何もしない)。
        /// </summary>
        void NotifyLog(string messageJa)
        {
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null) uiManager.AddLog(messageJa);
        }

        // ==================================================================
        // 歴史ツアー(2026-07-21 Claude Code 追加)
        // ==================================================================

        /// <summary>
        /// ツアーラベル専用Canvas(sortingOrder=150)とラベルを構築する。
        /// 年表Canvas(145)やゲームオーバーオーバーレイ(UIManagerのCanvas=100)より手前。
        /// GraphicRaycaster を持たず、ラベルもraycast無効のため入力は一切遮らない
        /// (ツアー中断の「任意クリック」は Input のポーリングで判定する)。
        /// </summary>
        void BuildTourLabel()
        {
            var go = new GameObject("ChronicleTourCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            tourCanvas = go.GetComponent<Canvas>();
            tourCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tourCanvas.sortingOrder = 150;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 画面上部中央の大きなラベル(背景付き・CanvasGroupでフェード)
            tourLabelRoot = UIStyle.CreatePanel(tourCanvas.transform, "TourLabel",
                new Color(0.045f, 0.06f, 0.10f, 0.90f));
            tourLabelRoot.GetComponent<Image>().raycastTarget = false;
            UIStyle.SetRect(tourLabelRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(860f, 92f));
            tourLabelGroup = tourLabelRoot.AddComponent<CanvasGroup>();
            tourLabelGroup.alpha = 0f;
            tourLabelGroup.interactable = false;
            tourLabelGroup.blocksRaycasts = false;

            tourLabelText = UIStyle.CreateText(tourLabelRoot.transform, "Text", "", 24,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.StretchFull(tourLabelText.gameObject, 12f);
            tourLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tourLabelText.verticalOverflow = VerticalWrapMode.Truncate;

            tourLabelRoot.SetActive(false);
        }

        /// <summary>
        /// 歴史ツアーの公開静的入口(UIManager のゲーム終了画面「歴史ツアー」ボタンが呼ぶ)。
        /// 自己起動インスタンスを探して開始する。見つからない・未構築なら何もしない(null安全)。
        /// </summary>
        public static void StartTourIfAvailable()
        {
            var instance = FindFirstObjectByType<ChroniclePanel>();
            if (instance != null) instance.StartTour();
        }

        /// <summary>
        /// 歴史ツアーを開始する。パネルを閉じ、座標付きの記録を時系列に
        /// カメラで巡る(各約1.6秒、非スケール時間)。対象が無ければ「ツアー終了」だけを短く出す。
        /// </summary>
        public void StartTour()
        {
            if (panel == null || tourLabelRoot == null) return;

            tourEntries.Clear();
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].HasCoord) tourEntries.Add(entries[i]);

            Hide();
            tourActive = true;
            tourEndPhase = false;
            tourIndex = -1;
            GameAudio.Instance?.PlayPanelOpen();   // ツアー開始のページ音(2026-07-21 追加)
            tourLabelRoot.SetActive(true);
            tourLabelGroup.alpha = 0f;
            AdvanceTour();
        }

        /// <summary>次の記録へ進める。残りが無ければ「ツアー終了」表示へ移る。</summary>
        void AdvanceTour()
        {
            tourIndex++;
            if (tourIndex >= tourEntries.Count)
            {
                tourEndPhase = true;
                tourLabelText.text = "ツアー終了";
                tourStepStartedAt = Time.unscaledTime;
                return;
            }

            var e = tourEntries[tourIndex];
            FocusCamera(e.Coord.ToWorld());
            tourLabelText.text = e.Text;   // 本文には「ターンN:」が含まれる
            tourStepStartedAt = Time.unscaledTime;
        }

        /// <summary>
        /// ツアーの毎フレーム処理: Esc/任意クリックで中断、ラベルのフェードイン・アウト、
        /// 表示時間経過で次の記録(または終了表示→終了)へ。すべて非スケール時間基準のため
        /// 高速観戦中でも一定のテンポで巡回する。
        /// </summary>
        void UpdateTour()
        {
            // Esc または任意クリックで中断(開始ボタンのクリックはボタン離しで発火するため、
            // 同フレームの GetMouseButtonDown はここに来ない)
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0)
                || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                StopTour();
                return;
            }

            float elapsed = Time.unscaledTime - tourStepStartedAt;
            float duration = tourEndPhase ? TourEndSeconds : TourStepSeconds;
            if (tourLabelGroup != null)
            {
                tourLabelGroup.alpha = Mathf.Clamp01(elapsed / TourFadeInSeconds)
                    * Mathf.Clamp01((duration - elapsed) / TourFadeOutSeconds);
            }

            if (elapsed < duration) return;
            if (tourEndPhase) StopTour();
            else AdvanceTour();
        }

        /// <summary>ツアーを終了(中断・完走とも)し、ラベルを隠す。</summary>
        void StopTour()
        {
            tourActive = false;
            tourEndPhase = false;
            if (tourLabelGroup != null) tourLabelGroup.alpha = 0f;
            if (tourLabelRoot != null) tourLabelRoot.SetActive(false);
        }

        /// <summary>
        /// カメラを指定ワールド座標へフォーカスする(MinimapPanel と同じ方式:
        /// CameraController をシーン検索してキャッシュし FocusOn。見つからない場合は
        /// Camera.main を現在の注視点との差分だけ平行移動でフォールバック)。
        /// </summary>
        void FocusCamera(Vector3 world)
        {
            if (cameraController == null)
                cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController != null)
            {
                cameraController.FocusOn(world);
                return;
            }

            var cam = Camera.main;
            if (cam == null) return;
            var dir = cam.transform.forward;
            if (dir.y >= -0.001f) return;   // ほぼ水平・上向きの視線では注視点を求められない
            float t = -cam.transform.position.y / dir.y;
            var current = cam.transform.position + dir * t;
            var delta = world - current;
            delta.y = 0f;
            cam.transform.position += delta;
        }

        // ==================================================================
        // ヘルパー
        // ==================================================================

        /// <summary>InputField(ゲーム設定のシード入力欄など)へ入力中か(MinimapPanelと同じ判定)。</summary>
        static bool IsTextInputFocused()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var selected = es.currentSelectedGameObject;
            if (selected == null) return false;
            var field = selected.GetComponent<InputField>();
            return field != null && field.isFocused;
        }
    }
}
