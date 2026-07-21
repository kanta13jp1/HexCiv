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
        }

        void OnDestroy()
        {
            Unbind();
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
