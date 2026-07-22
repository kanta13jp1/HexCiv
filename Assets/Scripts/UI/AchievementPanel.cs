using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Audio;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// 実績システム(2026-07-22 Claude Code 追加)。完全に表示専用の独立Canvas UI。
    ///
    /// MinimapPanel / ChroniclePanel と同じ独立Canvas方式: UIManager / GameBootstrap は
    /// 変更せず、TurnManager 構築時に Bind される CultureSystem.CurrentState を読み取り窓口に
    /// 使う。state の差し替わり(新規開始・リスタート・ロード・文明変更)を Update で検知して
    /// 型付きイベント(Core/GameEvents.cs)を再購読する。
    ///
    /// 検知: 型付きイベント(都市占領・文明滅亡・ゲーム終了・戦闘解決)の購読と、
    /// 約0.5秒ごとの HumanPlayer フィールドのポーリング(都市数・技術数・政策数・
    /// 遺産/偉人/作品数)の併用。すべて読み取りのみで、シミュレーション結果には一切
    /// 影響しない。観戦モード(HumanPlayer == null)では検知を完全に停止する
    /// (実績一覧パネルの閲覧だけは可能)。
    ///
    /// 永続化: PlayerPrefs "HexCiv.Ach.&lt;id&gt;" = 1。ゲームをまたいで共有され、
    /// 一度解除した実績は二度とトースト表示しない。
    ///
    /// UI: 解除時に画面下中央の小さなトーストが下からスライドイン(アイコン+
    /// 「実績解除: 名前」、約3秒、複数解除はキュー順次表示)。左下の「実績」ボタン
    /// (既存の独立パネルボタン列の右隣)で全実績の一覧パネルを開く。未解除は
    /// グレー表示、勝利系の隠し実績は解除まで「???」。アイコンは種類別の手続き生成
    /// (Texture2D。都市=ヘクス群 / 戦=交差する剣 / 技術=フラスコ / 文化=旗 /
    /// 遺産=石柱 / 偉人=胸像 / 作品=額縁 / 勝利=月桂冠 / 歴史=書物)。
    ///
    /// Z順: Canvas sortingOrder=45(指定値)。常時表示の「実績」ボタンだけは
    /// UIManager / ChroniclePanel の規約に合わせてネストCanvas(sortingOrder=-5)へ
    /// 退避し、開いているモーダルパネルの上に浮かないようにする。
    ///
    /// 歴史家実績: ChroniclePanel は編集せず、公開静的フック NotifyChronicleOpened()
    /// (将来の配線用)と、年表パネルの表示状態の自前ポーリングの両方で検知する。
    /// </summary>
    public sealed class AchievementPanel : MonoBehaviour
    {
        const string PrefsPrefix = "HexCiv.Ach.";
        /// <summary>ポーリング間隔(秒、非スケール時間。観戦倍速の影響を受けない)。</summary>
        const float PollInterval = 0.5f;

        // ---- トーストの時間パラメータ(すべて非スケール時間) ----
        const float ToastSlideInSeconds = 0.28f;
        const float ToastHoldSeconds = 2.4f;
        const float ToastSlideOutSeconds = 0.35f;
        const float ToastHiddenY = -70f;
        const float ToastShownY = 24f;

        // ---- 実績Id(検知コードから参照するもの) ----
        const string FirstCityId = "first_city";
        const string FiveCitiesId = "five_cities";
        const string FirstKillId = "first_kill";
        const string ConquerorId = "conqueror";
        const string AncientWisdomId = "ancient_wisdom";
        const string ScholarId = "scholar";
        const string CulturedId = "cultured";
        const string ThinkerId = "thinker";
        const string ArchaeologistId = "archaeologist";
        const string PatronId = "patron";
        const string CollectorId = "collector";
        const string DominatorId = "dominator";
        const string HistorianId = "historian";
        const string CultureVictoryId = "culture_victory";
        const string DominationVictoryId = "domination_victory";
        const string ScoreVictoryId = "score_victory";

        /// <summary>実績1件の定義(表示専用の静的データ)。</summary>
        sealed class AchievementDef
        {
            public string Id;
            public string NameJa;
            public string DescJa;
            public string IconKind;
            /// <summary>隠し実績(勝利系)。解除まで名前・説明を「???」で表示する。</summary>
            public bool Hidden;
        }

        /// <summary>基礎12技術のId(GameRules.Techs と同一。古代の知恵の判定に使う)。</summary>
        static readonly string[] BaseTechIds =
        {
            "agriculture", "pottery", "animal_husbandry", "archery", "mining", "writing",
            "wheel", "masonry", "bronze_working", "iron_working", "mathematics", "construction"
        };

        /// <summary>全実績の定義(16件、一覧パネルの表示順)。</summary>
        static readonly AchievementDef[] Defs =
        {
            new AchievementDef { Id = FirstCityId, NameJa = "最初の都市", DescJa = "初めて都市を建設した", IconKind = "city" },
            new AchievementDef { Id = FiveCitiesId, NameJa = "開拓者魂", DescJa = "同時に5都市を保有した", IconKind = "city" },
            new AchievementDef { Id = FirstKillId, NameJa = "初勝利", DescJa = "敵ユニットを初めて撃破した", IconKind = "war" },
            new AchievementDef { Id = ConquerorId, NameJa = "征服者", DescJa = "敵の都市を初めて占領した", IconKind = "war" },
            new AchievementDef { Id = AncientWisdomId, NameJa = "古代の知恵", DescJa = "基礎12技術をすべて研究した", IconKind = "tech" },
            new AchievementDef { Id = ScholarId, NameJa = "学究", DescJa = "技術を30件習得した", IconKind = "tech" },
            new AchievementDef { Id = CulturedId, NameJa = "文化人", DescJa = "初めて文化政策を採用した", IconKind = "culture" },
            new AchievementDef { Id = ThinkerId, NameJa = "思想家", DescJa = "文化政策を10件採用した", IconKind = "culture" },
            new AchievementDef { Id = ArchaeologistId, NameJa = "考古学者", DescJa = "初めて遺産を発見した", IconKind = "heritage" },
            new AchievementDef { Id = PatronId, NameJa = "後援者", DescJa = "初めて偉人を登用した", IconKind = "person" },
            new AchievementDef { Id = CollectorId, NameJa = "収集家", DescJa = "初めて作品を収蔵した", IconKind = "work" },
            new AchievementDef { Id = DominatorId, NameJa = "覇者", DescJa = "交戦中の他文明を滅亡させた", IconKind = "war" },
            new AchievementDef { Id = HistorianId, NameJa = "歴史家", DescJa = "戦史年表を開いた", IconKind = "book" },
            new AchievementDef { Id = CultureVictoryId, NameJa = "文化勝利", DescJa = "文化勝利を収めた", IconKind = "victory", Hidden = true },
            new AchievementDef { Id = DominationVictoryId, NameJa = "制覇勝利", DescJa = "制覇による勝利を収めた", IconKind = "victory", Hidden = true },
            new AchievementDef { Id = ScoreVictoryId, NameJa = "スコア勝利", DescJa = "スコア勝利を収めた", IconKind = "victory", Hidden = true },
        };

        public static AchievementPanel Instance { get; private set; }

        /// <summary>解除済み実績Id(PlayerPrefs から起動時に復元。ゲームをまたいで共有)。</summary>
        readonly HashSet<string> unlocked = new HashSet<string>();

        Canvas canvas;
        GameObject panel;
        Text counterText;
        readonly Image[] entryBgs = new Image[Defs.Length];
        readonly Image[] entryIcons = new Image[Defs.Length];
        readonly Text[] entryNames = new Text[Defs.Length];
        readonly Text[] entryDescs = new Text[Defs.Length];

        // ---- トースト ----
        GameObject toastRoot;
        RectTransform toastRect;
        CanvasGroup toastGroup;
        Image toastIconImage;
        Text toastText;
        readonly Queue<AchievementDef> toastQueue = new Queue<AchievementDef>();
        /// <summary>0=待機 1=スライドイン 2=表示保持 3=スライドアウト。</summary>
        int toastPhase;
        float toastPhaseStartedAt;

        /// <summary>現在イベント購読中の状態(差し替え検知に使う)。</summary>
        GameState boundState;
        float nextPollAt;

        /// <summary>
        /// 実績一覧パネルの表示を UIManager のモーダル計数へ通知済みか(2026-07-22 Claude Code 追加)。
        /// イベントバナーが実績パネルへ重なるのを防ぐため、開閉を必ず対で通知する
        /// (表示/非表示の全経路 — 「実績」ボタン・×・Esc — を毎フレームのポーリングで捕捉)。
        /// </summary>
        bool externalPanelNotified;

        // 購読解除のために保持するイベントハンドラ(Awakeで一度だけ生成)
        System.Action<City, Player, Player> captureHandler;
        System.Action<Player> eliminatedHandler;
        System.Action<Player, string> gameEndedHandler;
        System.Action<HexCoord, HexCoord, int, int> combatHandler;

        // ---- 歴史家実績: 年表パネルのポーリング検知 ----
        /// <summary>ChroniclePanel の表示ルート("ChronicleCanvas/ChroniclePanel"、見つかったらキャッシュ)。</summary>
        Transform chroniclePanelRoot;
        /// <summary>年表パネルの探索元(自己起動インスタンス。キャッシュ)。</summary>
        ChroniclePanel chroniclePanel;

        /// <summary>種類別アイコンSpriteのキャッシュ(アプリ実行中は生成済みを使い回す)。</summary>
        static readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<AchievementPanel>() != null) return;
            new GameObject("AchievementUI").AddComponent<AchievementPanel>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 解除済み実績の復元(グローバル永続化。復元分はトーストしない)
            for (int i = 0; i < Defs.Length; i++)
                if (PlayerPrefs.GetInt(PrefsPrefix + Defs[i].Id, 0) != 0)
                    unlocked.Add(Defs[i].Id);

            // イベントハンドラ(再購読のたびに同一インスタンスで購読/解除する)
            captureHandler = OnCityCapturedAch;
            eliminatedHandler = OnPlayerEliminatedAch;
            gameEndedHandler = OnGameEndedAch;
            combatHandler = OnCombatResolvedAch;
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
            BuildToast();
        }

        void Update()
        {
            // 状態の差し替わり(新規/リスタート/ロード/文明変更)を検知して再購読
            var current = CultureSystem.CurrentState;
            if (current != boundState) Rebind(current);

            // Esc で閉じる(他の独立パネルと同じく自前で処理)
            if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                Hide();

            // 約0.5秒ごとのポーリング検知(観戦モードでは HumanPlayer==null のため何もしない)
            if (Time.unscaledTime >= nextPollAt)
            {
                nextPollAt = Time.unscaledTime + PollInterval;
                PollAchievements();
            }

            UpdateToast();
            SyncModalNotify();   // 実績パネル表示中はイベントバナーを退避させる(2026-07-22 追加)
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
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 実績一覧パネルの表示状態を UIManager のモーダル計数へ反映する(2026-07-22 Claude Code 追加)。
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
        // 公開フック
        // ==================================================================

        /// <summary>
        /// 戦史年表が開かれたことを通知する公開静的フック(歴史家実績)。
        /// 現状は誰も呼ばなくてもよい — 本パネルが年表パネルの表示状態を自前で
        /// ポーリング検知するため。将来 ChroniclePanel 側から配線する場合の入口。
        /// </summary>
        public static void NotifyChronicleOpened()
        {
            var instance = Instance != null ? Instance : FindFirstObjectByType<AchievementPanel>();
            if (instance == null) return;
            if (instance.HumanPlayer() == null) return;   // 観戦モードでは解除しない
            instance.Unlock(HistorianId);
        }

        // ==================================================================
        // 検知(イベント購読+ポーリング)
        // ==================================================================

        Player HumanPlayer() => boundState != null ? boundState.HumanPlayer : null;

        bool IsUnlocked(string id) => unlocked.Contains(id);

        /// <summary>旧stateの購読を解除し、新stateへ購読し直す(解除済み実績はグローバルなので保持)。</summary>
        void Rebind(GameState next)
        {
            Unbind();
            boundState = next;
            if (boundState != null)
            {
                boundState.OnCityCaptured += captureHandler;
                boundState.OnPlayerEliminated += eliminatedHandler;
                boundState.OnGameEnded += gameEndedHandler;
                boundState.OnCombatResolved += combatHandler;
            }
        }

        void Unbind()
        {
            if (boundState == null) return;
            boundState.OnCityCaptured -= captureHandler;
            boundState.OnPlayerEliminated -= eliminatedHandler;
            boundState.OnGameEnded -= gameEndedHandler;
            boundState.OnCombatResolved -= combatHandler;
            boundState = null;
        }

        /// <summary>征服者: 人間プレイヤーが敵都市を占領した。</summary>
        void OnCityCapturedAch(City city, Player oldOwner, Player newOwner)
        {
            var human = HumanPlayer();
            if (human != null && newOwner == human) Unlock(ConquerorId);
        }

        /// <summary>覇者: 人間プレイヤーと交戦中の他文明が滅亡した(表示側の誠実な近似判定)。</summary>
        void OnPlayerEliminatedAch(Player p)
        {
            var human = HumanPlayer();
            if (human == null || p == null || p == human) return;
            if (!human.IsEliminated && human.IsAtWarWith(p.Id)) Unlock(DominatorId);
        }

        /// <summary>勝利実績: 人間プレイヤーが勝者のゲーム終了。種別は終了メッセージの文言で判定する。</summary>
        void OnGameEndedAch(Player winner, string messageJa)
        {
            var human = HumanPlayer();
            if (human == null || winner != human || string.IsNullOrEmpty(messageJa)) return;
            // 実メッセージ(Core側で確認済み):
            //   CultureSystem:  「X」が文化勝利を収めた!…
            //   GameStateOps:   「X」が制覇による勝利を収めた!…
            //   TurnManager:    ターン上限に到達。「X」がスコア勝利を収めた!…
            if (messageJa.Contains("文化勝利")) Unlock(CultureVictoryId);
            else if (messageJa.Contains("制覇")) Unlock(DominationVictoryId);
            else if (messageJa.Contains("スコア勝利")) Unlock(ScoreVictoryId);
        }

        /// <summary>
        /// 初勝利: 人間プレイヤーのユニットが敵ユニットを撃破した(最善努力の表示側判定)。
        /// イベントは Combat.PerformAttack の末尾で同期発火するため、攻撃直後の盤面から
        /// 次のいずれかで判定する(都市タイルへの攻撃はユニット撃破が起きないため除外):
        ///   A) 攻撃前座標に人間ユニットが残っている(遠隔/移動しない攻撃) かつ
        ///      対象タイルのユニットが消えた → 遠隔撃破。
        ///   B) 攻撃前座標が空で、対象タイルに人間ユニットがいる → 近接撃破後の進駐、
        ///      または人間側の反撃で攻撃側が倒れた(どちらも人間ユニットによる撃破)。
        /// 両者相討ちなど判定できないケースは解除しない(誤検知より取りこぼしを選ぶ)。
        /// </summary>
        void OnCombatResolvedAch(HexCoord attackerCoord, HexCoord targetCoord, int dmgToDefender, int dmgToAttacker)
        {
            if (IsUnlocked(FirstKillId)) return;
            var human = HumanPlayer();
            if (human == null || boundState == null || boundState.Map == null) return;

            var attackerTile = boundState.Map.Get(attackerCoord);
            var targetTile = boundState.Map.Get(targetCoord);
            if (targetTile == null || targetTile.City != null) return;   // 対都市攻撃は対象外

            var attackerUnit = attackerTile != null ? attackerTile.Unit : null;
            var targetUnit = targetTile.Unit;

            bool kill = false;
            if (attackerUnit != null && attackerUnit.PlayerId == human.Id)
                kill = dmgToDefender > 0 && targetUnit == null;                   // A: 遠隔撃破
            else if (attackerUnit == null && targetUnit != null && targetUnit.PlayerId == human.Id)
                kill = true;                                                      // B: 近接進駐/反撃撃破

            if (kill) Unlock(FirstKillId);
        }

        /// <summary>約0.5秒ごとの累積量ポーリング(HumanPlayer のフィールドの読み取りのみ)。</summary>
        void PollAchievements()
        {
            var human = HumanPlayer();
            if (human == null) return;   // 観戦モード: 実績システム無効

            if (human.Cities.Count >= 1) Unlock(FirstCityId);
            if (human.Cities.Count >= 5) Unlock(FiveCitiesId);

            if (human.KnownTechs.Count >= 30) Unlock(ScholarId);
            if (!IsUnlocked(AncientWisdomId))
            {
                bool all = true;
                for (int i = 0; i < BaseTechIds.Length; i++)
                    if (!human.KnownTechs.Contains(BaseTechIds[i])) { all = false; break; }
                if (all) Unlock(AncientWisdomId);
            }

            if (human.KnownCulturePolicies.Count >= 1) Unlock(CulturedId);
            if (human.KnownCulturePolicies.Count >= 10) Unlock(ThinkerId);

            if (human.DiscoveredHeritageSites.Count >= 1) Unlock(ArchaeologistId);
            if (human.RecruitedGreatPeople.Count >= 1) Unlock(PatronId);
            if (human.CollectedMasterpieces.Count >= 1) Unlock(CollectorId);

            PollChronicleOpen();
        }

        /// <summary>
        /// 歴史家: 年表パネル(ChroniclePanel の子 "ChronicleCanvas/ChroniclePanel")の表示状態を
        /// ポーリングで検知する。ChroniclePanel 本体は編集しない読み取りのみの検知。
        /// 階層名が変わった場合は単に解除されなくなるだけで、他への影響はない。
        /// </summary>
        void PollChronicleOpen()
        {
            if (IsUnlocked(HistorianId)) return;
            if (chroniclePanelRoot == null)
            {
                if (chroniclePanel == null) chroniclePanel = FindFirstObjectByType<ChroniclePanel>();
                if (chroniclePanel != null)
                    chroniclePanelRoot = chroniclePanel.transform.Find("ChronicleCanvas/ChroniclePanel");
            }
            if (chroniclePanelRoot != null && chroniclePanelRoot.gameObject.activeInHierarchy)
                Unlock(HistorianId);
        }

        // ==================================================================
        // 解除
        // ==================================================================

        static AchievementDef FindDef(string id)
        {
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].Id == id) return Defs[i];
            return null;
        }

        /// <summary>
        /// 実績を解除する。既に解除済みなら何もしない(トーストも鳴らさない)。
        /// PlayerPrefs へ即時保存し、トーストキューへ積む(解除音はトースト表示時に鳴る)。
        /// </summary>
        void Unlock(string id)
        {
            if (unlocked.Contains(id)) return;
            var def = FindDef(id);
            if (def == null) return;

            unlocked.Add(id);
            PlayerPrefs.SetInt(PrefsPrefix + id, 1);
            PlayerPrefs.Save();

            toastQueue.Enqueue(def);
            if (panel != null && panel.activeSelf) RefreshEntries();
        }

        // ==================================================================
        // UI構築
        // ==================================================================

        void BuildCanvas()
        {
            var go = new GameObject("AchievementCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 指定のZ順: 45。UIManagerのメインCanvas(100)やCodexのモーダル群(130+)より下で、
            // 地図の上に載る控えめなレイヤー(トーストは画面下中央の空き領域に出る)
            canvas.sortingOrder = 45;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("AchievementEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        /// <summary>
        /// 左下の「実績」開閉ボタン。既存の独立パネルボタン列
        /// (世界史図鑑 x=10 / 文化・政策 x=178 / 遺産・偉人・作品 x=346 / 年表 x=514 幅92、
        /// y=176、高さ36)の右隣(x=614)へ同じ段・同じ高さで置く。UIManager は変更しない。
        /// </summary>
        void BuildOpenButton()
        {
            var button = UIStyle.CreateButton(canvas.transform, "AchievementButton", "実績", 15, Show);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(614f, 176f), new Vector2(92f, 36f));

            // 小さな月桂冠アイコンをラベル左へ(UIStyle.AddButtonIcon と同じ見た目の自前実装。
            // UIStyle は変更しない)
            var iconGo = new GameObject("ButtonIcon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.sprite = GetIcon("victory");
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            UIStyle.SetRect(iconGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(20f, 20f));
            var label = UIStyle.ButtonLabel(button);
            if (label != null)
            {
                var lrt = (RectTransform)label.transform;
                if (lrt.offsetMin.x < 28f) lrt.offsetMin = new Vector2(28f, lrt.offsetMin.y);
            }

            DemoteBehindPanels(button.gameObject);
        }

        /// <summary>
        /// 常時表示ボタンを最背面のネストCanvas(sortingOrder=-5)へ退避する
        /// (UIManager / ChroniclePanel と同じ規約。モーダルパネルの上に浮かない)。
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
            panel = UIStyle.CreatePanel(canvas.transform, "AchievementListPanel",
                new Color(0.055f, 0.07f, 0.105f, 0.97f));
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(664f, 468f));

            var title = UIStyle.CreateText(panel.transform, "Title", "実績", 20,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-80f, 26f));

            counterText = UIStyle.CreateText(panel.transform, "Counter", "", 13,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(counterText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(-40f, 20f));

            var close = UIStyle.CreateButton(panel.transform, "Close", "×", 16, Hide);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(28f, 28f));

            // 4列×4行のグリッド(16件、内容は RefreshEntries が流し込む)
            for (int i = 0; i < Defs.Length; i++)
            {
                int col = i % 4;
                int row = i / 4;
                var cell = UIStyle.CreatePanel(panel.transform, "Cell" + i,
                    new Color(0.10f, 0.12f, 0.16f, 0.60f));
                UIStyle.SetRect(cell, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(12f + col * 160f, -58f - row * 98f),
                    new Vector2(152f, 92f));
                entryBgs[i] = cell.GetComponent<Image>();

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(cell.transform, false);
                var iconImage = iconGo.GetComponent<Image>();
                iconImage.raycastTarget = false;
                iconImage.preserveAspect = true;
                UIStyle.SetRect(iconGo, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(30f, 30f));
                entryIcons[i] = iconImage;

                var nameText = UIStyle.CreateText(cell.transform, "Name", "", 13,
                    TextAnchor.UpperLeft, UIStyle.TextMain);
                UIStyle.SetRect(nameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0f, 1f), new Vector2(44f, -10f), new Vector2(-50f, 30f));
                nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                nameText.verticalOverflow = VerticalWrapMode.Truncate;
                entryNames[i] = nameText;

                var descText = UIStyle.CreateText(cell.transform, "Desc", "", 11,
                    TextAnchor.UpperLeft, UIStyle.TextDim);
                UIStyle.SetRect(descText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(-16f, 44f));
                descText.horizontalOverflow = HorizontalWrapMode.Wrap;
                descText.verticalOverflow = VerticalWrapMode.Truncate;
                entryDescs[i] = descText;
            }

            panel.SetActive(false);
        }

        void BuildToast()
        {
            toastRoot = UIStyle.CreatePanel(canvas.transform, "AchievementToast",
                new Color(0.06f, 0.08f, 0.12f, 0.95f));
            toastRoot.GetComponent<Image>().raycastTarget = false;
            toastRect = UIStyle.SetRect(toastRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, ToastHiddenY), new Vector2(330f, 56f));
            toastGroup = toastRoot.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.interactable = false;
            toastGroup.blocksRaycasts = false;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(toastRoot.transform, false);
            toastIconImage = iconGo.GetComponent<Image>();
            toastIconImage.raycastTarget = false;
            toastIconImage.preserveAspect = true;
            UIStyle.SetRect(iconGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(32f, 32f));

            toastText = UIStyle.CreateText(toastRoot.transform, "Text", "", 15,
                TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(toastText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(20f, 0f), new Vector2(-64f, 0f));
            toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
            toastText.verticalOverflow = VerticalWrapMode.Truncate;

            toastRoot.SetActive(false);
        }

        // ==================================================================
        // 一覧パネルの表示
        // ==================================================================

        public void Show()
        {
            if (panel == null) return;
            if (!panel.activeSelf) GameAudio.Instance?.PlayPanelOpen();
            RefreshEntries();
            panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>全セルへ現在の解除状態を流し込む(解除=彩色、未解除=グレー、隠し=???)。</summary>
        void RefreshEntries()
        {
            int count = 0;
            for (int i = 0; i < Defs.Length; i++)
            {
                var def = Defs[i];
                bool isUnlocked = unlocked.Contains(def.Id);
                if (isUnlocked) count++;

                if (entryBgs[i] != null)
                    entryBgs[i].color = isUnlocked
                        ? new Color(0.17f, 0.17f, 0.10f, 0.85f)
                        : new Color(0.10f, 0.12f, 0.16f, 0.60f);
                if (entryIcons[i] != null)
                {
                    entryIcons[i].sprite = GetIcon(def.IconKind);
                    entryIcons[i].color = isUnlocked
                        ? Color.white
                        : new Color(0.55f, 0.57f, 0.62f, 0.45f);
                }
                bool masked = def.Hidden && !isUnlocked;
                if (entryNames[i] != null)
                {
                    entryNames[i].text = masked ? "???" : def.NameJa;
                    entryNames[i].color = isUnlocked ? UIStyle.Accent : UIStyle.TextDim;
                }
                if (entryDescs[i] != null)
                    entryDescs[i].text = masked ? "???(勝利すると判明する)" : def.DescJa;
            }

            if (counterText != null)
            {
                string note = HumanPlayer() == null ? " — 観戦モードでは解除されません" : "";
                counterText.text = $"解除 {count} / {Defs.Length}{note}";
            }
        }

        // ==================================================================
        // トースト(下からスライドイン→保持→スライドアウト、キュー順次表示)
        // ==================================================================

        void UpdateToast()
        {
            if (toastRoot == null) return;

            if (toastPhase == 0)
            {
                if (toastQueue.Count == 0) return;
                var def = toastQueue.Dequeue();
                if (toastIconImage != null) toastIconImage.sprite = GetIcon(def.IconKind);
                if (toastText != null) toastText.text = "実績解除: " + def.NameJa;
                toastRoot.SetActive(true);
                toastGroup.alpha = 0f;
                toastRect.anchoredPosition = new Vector2(0f, ToastHiddenY);
                toastPhase = 1;
                toastPhaseStartedAt = Time.unscaledTime;
                GameAudio.Instance?.PlayAchievement();   // 解除チャイム(音量/ミュートはGameAudioが一括処理)
                return;
            }

            float elapsed = Time.unscaledTime - toastPhaseStartedAt;
            if (toastPhase == 1)
            {
                float p = Mathf.Clamp01(elapsed / ToastSlideInSeconds);
                float s = p * p * (3f - 2f * p);   // smoothstep
                toastRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(ToastHiddenY, ToastShownY, s));
                toastGroup.alpha = p;
                if (p >= 1f) { toastPhase = 2; toastPhaseStartedAt = Time.unscaledTime; }
            }
            else if (toastPhase == 2)
            {
                if (elapsed >= ToastHoldSeconds) { toastPhase = 3; toastPhaseStartedAt = Time.unscaledTime; }
            }
            else if (toastPhase == 3)
            {
                float p = Mathf.Clamp01(elapsed / ToastSlideOutSeconds);
                float s = p * p * (3f - 2f * p);
                toastRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(ToastShownY, ToastHiddenY, s));
                toastGroup.alpha = 1f - p;
                if (p >= 1f)
                {
                    toastPhase = 0;
                    toastRoot.SetActive(false);   // 次のキューは次フレームの phase 0 が拾う
                }
            }
        }

        // ==================================================================
        // 手続き生成アイコン(32px、内部128pxで描いて4x4平均縮小=アンチエイリアス)
        // ==================================================================

        /// <summary>正規化座標(0..1、y上向き)のVector2省略記法。</summary>
        static Vector2 V(float x, float y) => new Vector2(x, y);

        static Sprite GetIcon(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            Sprite cached;
            if (iconCache.TryGetValue(kind, out cached) && cached != null) return cached;
            var sprite = BuildIconSprite(kind);
            iconCache[kind] = sprite;
            return sprite;
        }

        static Sprite BuildIconSprite(string kind)
        {
            const int size = 32;
            const int ss = 4;
            const int big = size * ss;
            var buf = new Color[big * big];   // 透明で初期化される

            var ivory = new Color(0.93f, 0.91f, 0.83f, 1f);
            var gold = UIStyle.Accent;

            switch (kind)
            {
                case "city":   // ヘクス群(都市): 小さな六角形3つの寄り集まり
                    FillPoly(buf, big, HexPoints(0.34f, 0.66f, 0.19f), ivory);
                    FillPoly(buf, big, HexPoints(0.66f, 0.66f, 0.19f), ivory);
                    FillPoly(buf, big, HexPoints(0.50f, 0.34f, 0.22f), gold);
                    break;
                case "war":    // 交差する2本の剣(斜めバー)
                    FillPoly(buf, big, new[] { V(0.12f, 0.22f), V(0.22f, 0.12f), V(0.88f, 0.78f), V(0.78f, 0.88f) }, ivory);
                    FillPoly(buf, big, new[] { V(0.78f, 0.12f), V(0.88f, 0.22f), V(0.22f, 0.88f), V(0.12f, 0.78f) }, gold);
                    break;
                case "tech":   // フラスコ(三角の胴+首)
                    FillRect(buf, big, 0.44f, 0.60f, 0.56f, 0.92f, ivory);
                    FillPoly(buf, big, new[] { V(0.18f, 0.10f), V(0.82f, 0.10f), V(0.58f, 0.60f), V(0.42f, 0.60f) }, gold);
                    break;
                case "culture": // 旗(竿+三角ペナント)
                    FillRect(buf, big, 0.18f, 0.10f, 0.25f, 0.92f, ivory);
                    FillPoly(buf, big, new[] { V(0.25f, 0.88f), V(0.88f, 0.70f), V(0.25f, 0.52f) }, gold);
                    break;
                case "heritage": // 石柱(基壇+柱身+柱頭)
                    FillRect(buf, big, 0.20f, 0.08f, 0.80f, 0.22f, ivory);
                    FillRect(buf, big, 0.34f, 0.22f, 0.66f, 0.76f, gold);
                    FillRect(buf, big, 0.22f, 0.76f, 0.78f, 0.90f, ivory);
                    break;
                case "person": // 胸像(頭の円+肩の台形)
                    FillCircle(buf, big, 0.50f, 0.66f, 0.17f, gold);
                    FillPoly(buf, big, new[] { V(0.22f, 0.10f), V(0.78f, 0.10f), V(0.66f, 0.42f), V(0.34f, 0.42f) }, ivory);
                    break;
                case "work":   // 額縁(金の枠+象牙のカンバス)
                    FillRect(buf, big, 0.14f, 0.14f, 0.86f, 0.86f, gold);
                    FillRect(buf, big, 0.27f, 0.27f, 0.73f, 0.73f, ivory);
                    break;
                case "victory": // 月桂冠(上が開いた環+中央の点)
                    FillCircle(buf, big, 0.50f, 0.48f, 0.40f, gold);
                    FillCircle(buf, big, 0.50f, 0.48f, 0.28f, Color.clear);
                    FillRect(buf, big, 0.40f, 0.78f, 0.60f, 1.00f, Color.clear);
                    FillCircle(buf, big, 0.50f, 0.48f, 0.09f, ivory);
                    break;
                case "book":   // 開いた本(左右2ページ)
                    FillPoly(buf, big, new[] { V(0.08f, 0.80f), V(0.48f, 0.68f), V(0.48f, 0.20f), V(0.08f, 0.32f) }, ivory);
                    FillPoly(buf, big, new[] { V(0.92f, 0.80f), V(0.52f, 0.68f), V(0.52f, 0.20f), V(0.92f, 0.32f) }, gold);
                    break;
                default:
                    return null;
            }

            // 4x4ブロックのアルファ加重平均で 32px へ縮小(縁が滑らかになる)
            var outPx = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        int rowStart = (y * ss + sy) * big + x * ss;
                        for (int sx = 0; sx < ss; sx++)
                        {
                            var c = buf[rowStart + sx];
                            r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                        }
                    }
                    outPx[y * size + x] = a > 0f
                        ? new Color(r / a, g / a, b / a, a / (ss * ss))
                        : Color.clear;
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "achievement_icon_" + kind;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(outPx);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>中心(cx,cy)・外接半径rの正六角形(頂点が上下)の頂点列。</summary>
        static Vector2[] HexPoints(float cx, float cy, float r)
        {
            var pts = new Vector2[6];
            for (int k = 0; k < 6; k++)
            {
                float ang = (30f + k * 60f) * Mathf.Deg2Rad;
                pts[k] = new Vector2(cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r);
            }
            return pts;
        }

        /// <summary>正規化座標(0..1、y上向き)の軸平行矩形を塗る(Color.clear で打ち抜きにも使う)。</summary>
        static void FillRect(Color[] buf, int big, float x0, float y0, float x1, float y1, Color c)
        {
            int px0 = Mathf.Clamp(Mathf.FloorToInt(x0 * big), 0, big);
            int py0 = Mathf.Clamp(Mathf.FloorToInt(y0 * big), 0, big);
            int px1 = Mathf.Clamp(Mathf.CeilToInt(x1 * big), 0, big);
            int py1 = Mathf.Clamp(Mathf.CeilToInt(y1 * big), 0, big);
            for (int y = py0; y < py1; y++)
            {
                float v = (y + 0.5f) / big;
                if (v < y0 || v > y1) continue;
                for (int x = px0; x < px1; x++)
                {
                    float u = (x + 0.5f) / big;
                    if (u < x0 || u > x1) continue;
                    buf[y * big + x] = c;
                }
            }
        }

        /// <summary>正規化座標(0..1、y上向き)の円を塗る(Color.clear で打ち抜きにも使う)。</summary>
        static void FillCircle(Color[] buf, int big, float cx, float cy, float r, Color c)
        {
            int px0 = Mathf.Clamp(Mathf.FloorToInt((cx - r) * big), 0, big);
            int py0 = Mathf.Clamp(Mathf.FloorToInt((cy - r) * big), 0, big);
            int px1 = Mathf.Clamp(Mathf.CeilToInt((cx + r) * big), 0, big);
            int py1 = Mathf.Clamp(Mathf.CeilToInt((cy + r) * big), 0, big);
            float r2 = r * r;
            for (int y = py0; y < py1; y++)
            {
                float v = (y + 0.5f) / big - cy;
                for (int x = px0; x < px1; x++)
                {
                    float u = (x + 0.5f) / big - cx;
                    if (u * u + v * v <= r2) buf[y * big + x] = c;
                }
            }
        }

        /// <summary>正規化座標(0..1、y上向き)の多角形を塗る(交差数判定)。</summary>
        static void FillPoly(Color[] buf, int big, Vector2[] poly, Color c)
        {
            float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
            for (int i = 0; i < poly.Length; i++)
            {
                if (poly[i].x < minX) minX = poly[i].x;
                if (poly[i].y < minY) minY = poly[i].y;
                if (poly[i].x > maxX) maxX = poly[i].x;
                if (poly[i].y > maxY) maxY = poly[i].y;
            }
            int px0 = Mathf.Clamp(Mathf.FloorToInt(minX * big), 0, big);
            int py0 = Mathf.Clamp(Mathf.FloorToInt(minY * big), 0, big);
            int px1 = Mathf.Clamp(Mathf.CeilToInt(maxX * big), 0, big);
            int py1 = Mathf.Clamp(Mathf.CeilToInt(maxY * big), 0, big);
            for (int y = py0; y < py1; y++)
            {
                float v = (y + 0.5f) / big;
                for (int x = px0; x < px1; x++)
                {
                    float u = (x + 0.5f) / big;
                    if (PointInPoly(poly, u, v)) buf[y * big + x] = c;
                }
            }
        }

        /// <summary>点が多角形の内側か(半直線との交差数の偶奇)。</summary>
        static bool PointInPoly(Vector2[] poly, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
