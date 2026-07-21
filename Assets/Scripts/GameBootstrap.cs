using System;
using System.Collections.Generic;
using UnityEngine;
using HexCiv.Core;
using HexCiv.Core.AI;
using HexCiv.Render;
using HexCiv.UI;
using HexCiv.Control;
using HexCiv.Audio;

namespace HexCiv
{
    /// <summary>
    /// ゲーム全体の起動と配線(ARCHITECTURE.md §9)。
    /// どのシーン(Untitled 含む)でも RuntimeInitializeOnLoadMethod で自動生成され、
    /// 状態構築 → 描画/UI/カメラ/入力の配線 → Version 監視による再同期 →
    /// ゲームオーバー表示 → シーン再読込なしのリスタートを担う。
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        const string SelectedCivilizationKey = "HexCiv.SelectedCivilization";
        const string SelectedLeaderKey = "HexCiv.SelectedLeader";

        /// <summary>セーブスロット数(2026-07-20 Claude Code 追加。UIManager のスロット選択画面と対応)。</summary>
        public const int SaveSlotCount = 3;

        // ---- ゲーム設定(2026-07-20 Claude Code 追加。UIManager のゲーム設定画面と同一規約) ----
        /// <summary>マップサイズ設定の PlayerPrefs キー(0=小40×24, 1=標準44×26, 2=大56×34)。</summary>
        const string MapSizeKey = "HexCiv.MapSize";
        /// <summary>文明数設定の PlayerPrefs キー(2..8)。</summary>
        const string NumPlayersKey = "HexCiv.NumPlayers";
        /// <summary>マップ種別設定の PlayerPrefs キー(0=大陸,1=パンゲア,2=群島。2026-07-20 追加)。
        /// キーと値の規約は UIManager のゲーム設定画面と同一に保つこと。</summary>
        const string MapTypeKey = "HexCiv.MapType";
        /// <summary>難易度設定の PlayerPrefs キー(0=やさしい,1=普通,2=むずかしい。2026-07-20 追加)。
        /// キーと値の規約は UIManager のゲーム設定画面と同一に保つこと。</summary>
        const string DifficultyKey = "HexCiv.Difficulty";
        /// <summary>フルスクリーン設定の PlayerPrefs キー(0=ウィンドウ,1=フルスクリーン。2026-07-21 追加)。
        /// 値の読み書きと実際の画面切替は本クラスが担い、UIManager はトグル要求とラベル表示のみを行う。</summary>
        const string FullscreenKey = "HexCiv.Fullscreen";
        static readonly int[] MapSizeWidths = { 40, 44, 56 };
        static readonly int[] MapSizeHeights = { 24, 26, 34 };

        GameState state;
        TurnManager turnManager;
        GameActions actions;

        MapRenderer mapRenderer;
        EntityRenderer entityRenderer;
        UIManager uiManager;
        CameraController cameraController;
        GameAudio audioManager;
        InputController inputController;

        int lastSeenVersion = -1;
        bool gameOverShown;
        /// <summary>フルスクリーン設定の現在値(PlayerPrefs と同期。エディタでは表示のみで実切替はしない)。</summary>
        bool fullscreenActive;
        string selectedHumanCivilizationId = "athens";
        string selectedHumanLeaderId = "pericles";

        // ---- シミュレーション観戦モード(2026-07-20 Claude Code 追加) ----
        // 全文明をAIが動かし、一定間隔で TurnManager.RunHeadlessTurn() を呼んで自動進行する。
        // simulationMode は ApplyState で state.HumanPlayer == null から導出する
        // (新規開始・リスタート・ロードの全経路で state と常に一致させるため)。
        /// <summary>観戦モード中か(state.HumanPlayer == null と同期)。</summary>
        bool simulationMode;
        /// <summary>観戦の速度倍率(1=等速, 2, 4, 8, 16, 32, 64, 128, 256=256倍速)。既定は2倍速。</summary>
        float simulationSpeed = 2f;
        /// <summary>観戦の一時停止中か(ターン自動進行のみ止める。カメラ等は操作可能)。</summary>
        bool simulationPaused;
        /// <summary>次にターンを自動進行する時刻(Time.unscaledTime 基準。timeScale の影響を受けない)。</summary>
        float nextSimulationTurnAt;

        // ---- 観戦演出:型付きイベント購読(2026-07-20 Claude Code 追加) ----
        /// <summary>イベント購読中の GameState(再 ApplyState 時の二重購読・購読漏れ防止)。</summary>
        GameState subscribedState;
        /// <summary>観戦演出イベント直後、ターン自動進行を止めておく期限(Time.unscaledTime 基準)。
        /// ユーザーの一時停止状態(simulationPaused)には触れず、期限が過ぎれば自動で再開する。</summary>
        float simulationEventHoldUntil;
        /// <summary>イベント発生時にターン進行を止める時間(秒)。</summary>
        const float SimulationEventHoldSeconds = 1.2f;
        /// <summary>各プレイヤーの直近の首都位置(滅亡は都市喪失後に通知されるため、
        /// 滅亡演出のカメラジャンプ先として直前の記録を使う。観戦モード中のみ更新)。</summary>
        readonly Dictionary<int, Vector3> lastKnownCapitalPos = new Dictionary<int, Vector3>();

        // 観戦演出バナーの配色(2026-07-20 Claude Code 追加)
        static readonly Color WarBannerColor = new Color(0.95f, 0.40f, 0.35f, 1f);
        static readonly Color PeaceBannerColor = new Color(0.45f, 0.85f, 0.55f, 1f);
        static readonly Color CaptureBannerColor = new Color(0.95f, 0.62f, 0.28f, 1f);
        static readonly Color EliminationBannerColor = new Color(0.78f, 0.78f, 0.82f, 1f);
        static readonly Color VictoryBannerColor = new Color(0.95f, 0.83f, 0.35f, 1f);

        /// <summary>ターン自動進行の間隔(秒)。速度2倍なら0.5秒ごとに1ターン。</summary>
        float SimulationIntervalSeconds => 1f / Mathf.Max(0.25f, simulationSpeed);

        /// <summary>どのシーンでも自動でゲームを起動する(既に存在すれば何もしない)。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindObjectOfType<GameBootstrap>() != null) return;
            var go = new GameObject("HexCivGame");
            go.AddComponent<GameBootstrap>();
        }

        void Start()
        {
            MigrateLegacySaveFile();
            ApplyFullscreenFromPrefs();   // 保存済みのフルスクリーン設定を起動時に1回適用(2026-07-21 追加)
            string savedCivilization = PlayerPrefs.GetString(SelectedCivilizationKey, "athens");
            if (CivilizationCatalog.Find(savedCivilization) != null)
                selectedHumanCivilizationId = savedCivilization;
            string savedLeader = PlayerPrefs.GetString(SelectedLeaderKey, "pericles");
            if (LeaderCatalog.BelongsTo(savedLeader, selectedHumanCivilizationId))
                selectedHumanLeaderId = savedLeader;
            else
            {
                var defaultLeader = LeaderCatalog.DefaultForCivilization(selectedHumanCivilizationId);
                selectedHumanLeaderId = defaultLeader != null ? defaultLeader.Id : "";
            }
            StartNewGame();
        }

        void Update()
        {
            if (state == null) return;

            if (inputController != null) inputController.Update();

            // シミュレーション観戦:スケジュール(1ターン=1/速度 秒、非スケール時間)が要求する分だけ
            // 自動進行する(2026-07-20 Claude Code 追加、2026-07-21 複数ターン/フレームのバッチ化)。
            // 60fpsでは1フレーム1ターンが上限=実効約60ターン/秒のため、128倍速以上はバッチ実行が必要。
            // 上限(ターン数ハードキャップ+約10msの時間予算)は RunSimulationTurnBatch 内で制御し、
            // 処理落ちのスパイラルにはならない。描画/UIはバッチ完了後、直後の Version 監視が
            // 同フレームに1回だけ再同期する(ターンごとには再描画しない)
            if (simulationMode && !state.IsGameOver && !simulationPaused &&
                turnManager != null && Time.unscaledTime >= nextSimulationTurnAt &&
                Time.unscaledTime >= simulationEventHoldUntil)   // 観戦演出中は少し待つ(2026-07-20 追加)
            {
                RunSimulationTurnBatch();
            }

            // Version 監視:シミュレーション状態が変わったら描画とUIを再同期する
            if (state.Version != lastSeenVersion)
            {
                lastSeenVersion = state.Version;
                if (simulationMode) RememberCapitalPositions();   // 滅亡演出のカメラ用(2026-07-20 追加)
                if (mapRenderer != null) mapRenderer.RefreshDynamic();
                if (entityRenderer != null) entityRenderer.Refresh();
                if (uiManager != null) uiManager.RefreshAll();
            }

            // ゲームオーバー(1回だけ表示)
            if (state.IsGameOver && !gameOverShown)
            {
                gameOverShown = true;
                if (inputController != null) inputController.ClearSelection();
                if (mapRenderer != null) mapRenderer.ClearHighlights();
                if (uiManager != null) uiManager.ShowGameOver(state.GameOverMessageJa);
                if (audioManager != null) audioManager.PlayGameOver(state.Winner == state.HumanPlayer);
            }
        }

        void OnDestroy()
        {
            // 観戦中(timeScale>1)に破棄された場合でも通常速度へ戻す(2026-07-20 Claude Code 追加)
            if (simulationMode) Time.timeScale = 1f;
            UnsubscribeStateEvents();
        }

        // ==================================================================
        // 状態構築(純シミュレーション。SmokeTest からも利用される)
        // ==================================================================

        /// <summary>
        /// GameState を組み立てる純粋シミュレーション処理。
        /// GameObject には一切触れないため、エディタのヘッドレステストからも呼べる。
        /// Seed=0 のときは起動時刻でランダム化する。
        /// </summary>
        public static GameState BuildNewGame(GameConfig config)
        {
            return BuildNewGame(config, null);
        }

        /// <summary>
        /// 文明IDを指定して新規ゲームを構築する。null/不足/不明IDは台帳の既定順で補完する。
        /// 既存のBuildNewGame(GameConfig)はこのオーバーロードへ委譲する。
        /// </summary>
        public static GameState BuildNewGame(GameConfig config, IList<string> civilizationIds)
        {
            return BuildNewGame(config, civilizationIds, null);
        }

        /// <summary>
        /// 文明IDと指導者IDを指定して新規ゲームを構築する。指導者IDが文明に属さない場合は
        /// その文明の既定指導者へ安全にフォールバックする。
        /// </summary>
        public static GameState BuildNewGame(GameConfig config, IList<string> civilizationIds,
            IList<string> leaderIds)
        {
            if (config == null) config = new GameConfig();
            if (config.Seed == 0) config.Seed = Environment.TickCount & 0x7fffffff;

            var s = new GameState
            {
                Config = config,
                Rng = new System.Random(config.Seed),
            };

            List<HexCoord> starts;
            s.Map = MapGenerator.Generate(config, s.Rng, out starts);

            // ---- プレイヤー生成(index 0 が人間。HumanPlayerIndex=-1 なら全員AI) ----
            for (int i = 0; i < config.NumPlayers; i++)
            {
                CivilizationDef civilization = null;
                if (civilizationIds != null && i < civilizationIds.Count)
                    civilization = CivilizationCatalog.Find(civilizationIds[i]);
                if (civilization == null) civilization = CivilizationCatalog.DefaultForSlot(i);

                LeaderDef leader = null;
                if (leaderIds != null && i < leaderIds.Count)
                    leader = LeaderCatalog.Find(leaderIds[i]);
                if (leader == null || civilization == null ||
                    !LeaderCatalog.BelongsTo(leader.Id, civilization.Id))
                    leader = civilization != null
                        ? LeaderCatalog.DefaultForCivilization(civilization.Id)
                        : null;

                var p = new Player
                {
                    Id = i,
                    CivilizationId = civilization != null ? civilization.Id : "",
                    NameJa = civilization != null
                        ? civilization.NameJa
                        : GameConfig.CivNames[i % GameConfig.CivNames.Length],
                    RegionJa = civilization != null ? civilization.RegionJa : "",
                    EraJa = civilization != null ? civilization.EraJa : "",
                    LeaderId = leader != null ? leader.Id : "",
                    LeaderNameJa = leader != null ? leader.NameJa : "",
                    LeaderTitleJa = leader != null ? leader.TitleJa : "",
                    Color = civilization != null
                        ? civilization.Color
                        : GameConfig.CivColors[i % GameConfig.CivColors.Length],
                    IsHuman = i == config.HumanPlayerIndex,
                };
                // KnownTechs はフィールド初期化子で GameRules.StartingTech を含む
                s.Players.Add(p);
            }

            // ---- 初期ユニット:開拓者(開始タイル)+ 戦士(空き隣接タイル) ----
            for (int i = 0; i < s.Players.Count; i++)
            {
                var p = s.Players[i];
                var start = (starts != null && starts.Count > 0)
                    ? starts[i % starts.Count]
                    : HexCoord.FromOffset(s.Map.Width / 2, s.Map.Height / 2);

                var settlerCoord = FindFreeTileNear(s, start);
                s.CreateUnit(p, "settler", settlerCoord);

                var warriorCoord = FindFreeNeighbor(s, settlerCoord);
                if (warriorCoord.HasValue)
                    s.CreateUnit(p, "warrior", warriorCoord.Value);
            }

            Visibility.RecomputeAll(s);
            return s;
        }

        /// <summary>指定座標が塞がっていたら近傍の空き通行可能タイルを探す(必ず何か返す)。</summary>
        static HexCoord FindFreeTileNear(GameState s, HexCoord coord)
        {
            var t = s.Map.Get(coord);
            if (t != null && t.IsPassable && t.Unit == null) return coord;
            for (int radius = 1; radius <= 6; radius++)
            {
                foreach (var c in coord.Ring(radius))
                {
                    var n = s.Map.Get(c);
                    if (n != null && n.IsPassable && n.Unit == null && n.City == null)
                        return c;
                }
            }
            return coord;   // 最終手段(理論上ここには来ない)
        }

        /// <summary>空いている通行可能な隣接タイル。無ければ null。</summary>
        static HexCoord? FindFreeNeighbor(GameState s, HexCoord coord)
        {
            foreach (var t in s.Map.NeighborsOf(coord))
                if (t.IsPassable && t.Unit == null && t.City == null)
                    return t.Coord;
            return null;
        }

        // ==================================================================
        // 起動・リスタート
        // ==================================================================

        /// <summary>
        /// 新しいゲームを開始する。リスタート時にも呼ばれる
        /// (シーン再読込では RuntimeInitializeOnLoadMethod が再実行されないため、その場で再構築する)。
        /// </summary>
        void StartNewGame()
        {
            // Seed=0 → BuildNewGame でランダム化。マップサイズ・文明数はゲーム設定画面の保存値を使う
            // (文明変更・指導者変更・リスタートもこの経路のため、同じ設定が適用される)
            StartNewGameWithConfig(CreateConfigFromSettings());
        }

        /// <summary>
        /// 指定 config で新しいゲームを構築して適用する(2026-07-20 Claude Code 追加)。
        /// 文明・指導者ロスターの割当は従来の StartNewGame と同一。
        /// </summary>
        void StartNewGameWithConfig(GameConfig config)
        {
            var civilizationRoster = CreateCivilizationRoster(config);
            ApplyState(BuildNewGame(config, civilizationRoster, CreateLeaderRoster(config, civilizationRoster)));
            turnManager.BeginGame();
        }

        /// <summary>
        /// ゲーム設定画面からの新規開始(2026-07-20 Claude Code 追加。UIManager.OnNewGameRequested に配線)。
        /// マップサイズ・文明数は PlayerPrefs の保存値、seed は 0 ならランダム。
        /// </summary>
        void StartNewGameWithSeed(int seed)
        {
            var config = CreateConfigFromSettings();
            config.Seed = seed > 0 ? seed : 0;
            StartNewGameWithConfig(config);
        }

        // ==================================================================
        // シミュレーション観戦モード(2026-07-20 Claude Code 追加)
        // ==================================================================

        /// <summary>
        /// ゲーム設定画面の「シミュレーション観戦で開始」(UIManager.OnSimulationStartRequested に配線)。
        /// 現在の設定(マップサイズ・文明数・シード)と文明・指導者選択をそのまま使い、
        /// HumanPlayerIndex = -1(全員AI)で新規ゲームを構築する。観戦フラグ等は ApplyState が
        /// state.HumanPlayer == null から導出するため、ここでは config を作るだけでよい。
        /// </summary>
        void StartSimulationGame(int seed)
        {
            var config = CreateConfigFromSettings();
            config.Seed = seed > 0 ? seed : 0;
            config.HumanPlayerIndex = -1;   // 全員AI → GameState.HumanPlayer == null
            StartNewGameWithConfig(config);
        }

        /// <summary>観戦の一時停止/再開(UIManager.OnSimulationPauseToggled に配線)。</summary>
        void ToggleSimulationPause()
        {
            if (!simulationMode) return;
            simulationPaused = !simulationPaused;
            // 再開時は「今から interval 後」に次ターン(停止中に溜まった分を一気に進めない)
            if (!simulationPaused) nextSimulationTurnAt = Time.unscaledTime + SimulationIntervalSeconds;
            if (uiManager != null) uiManager.SetSimulationStatus(simulationPaused, simulationSpeed);
        }

        /// <summary>観戦速度の切替 1倍→2倍→4倍→8倍→16倍→32倍→64倍→128倍→256倍→1倍
        /// (UIManager.OnSimulationSpeedCycled に配線)。ターン間隔(SimulationIntervalSeconds =
        /// 1/速度 の非スケール時間)と Time.timeScale の両方が追従する。フレームレートを超える
        /// 速度(256倍など)は RunSimulationTurnBatch が1フレームに複数ターン進めて実現する。</summary>
        void CycleSimulationSpeed()
        {
            if (!simulationMode) return;
            if (simulationSpeed >= 255.5f) simulationSpeed = 1f;
            else if (simulationSpeed >= 127.5f) simulationSpeed = 256f;
            else if (simulationSpeed >= 63.5f) simulationSpeed = 128f;
            else if (simulationSpeed >= 31.5f) simulationSpeed = 64f;
            else if (simulationSpeed >= 15.5f) simulationSpeed = 32f;
            else if (simulationSpeed >= 7.5f) simulationSpeed = 16f;
            else if (simulationSpeed >= 3.5f) simulationSpeed = 8f;
            else if (simulationSpeed >= 1.5f) simulationSpeed = 4f;
            else simulationSpeed = 2f;
            Time.timeScale = simulationSpeed;
            if (uiManager != null) uiManager.SetSimulationStatus(simulationPaused, simulationSpeed);
        }

        /// <summary>
        /// 観戦ターンのバッチ実行(2026-07-21 Claude Code 追加:128倍速対応。同日256倍速へ拡張)。
        /// スケジュール(nextSimulationTurnAt。1ターン消化ごとに 1/速度 秒進む)が要求する分だけ
        /// 1フレームで複数ターン進める。ただし次の3条件で打ち切る:
        ///  (a) ハード上限 ceil(速度/60)+1 ターン/フレーム(64倍速以下では1〜3に解決し、
        ///      従来の「1フレーム1ターン」と実質同等の挙動を保つ。128倍速=4、256倍速=6)
        ///  (b) フレーム時間予算:このフレームのターン処理が実測約10msを超えたら打ち切り
        ///      (Time 系は同一フレーム内で進まないため System.Diagnostics.Stopwatch で計測)
        ///  (c) イベントバナー発生(simulationEventHoldUntil の延長=宣戦・陥落等の演出)
        ///      またはゲーム終了で即打ち切り(バナーの~1.2秒ホールドを従来どおり効かせる)
        /// 打ち切りで消化しきれなかった予定は現在時刻へ丸め、バックログの無限成長を防ぐ。
        /// 描画/UIの再同期はループ内では行わず、Update 直後の Version 監視が1フレーム1回行う。
        /// </summary>
        void RunSimulationTurnBatch()
        {
            int hardCap = Mathf.CeilToInt(simulationSpeed / 60f) + 1;
            float interval = SimulationIntervalSeconds;
            float now = Time.unscaledTime;
            float holdBefore = simulationEventHoldUntil;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int ran = 0;
            while (ran < hardCap && now >= nextSimulationTurnAt && !state.IsGameOver)
            {
                turnManager.RunHeadlessTurn();
                ran++;
                nextSimulationTurnAt += interval;
                // 観戦演出イベントが発生したらこのフレームのバッチを即終了(残りは次フレーム以降)
                if (simulationEventHoldUntil > holdBefore) break;
                if (stopwatch.Elapsed.TotalMilliseconds >= 10.0) break;
            }
            // 消化しきれない予定が過去に溜まっていたら現在へ丸める(次フレームは即1ターン目が
            // 実行される。「今+間隔」ではなく「今」に丸めるのは、上限内で最大速度を保つため)
            if (nextSimulationTurnAt < now) nextSimulationTurnAt = now;
        }

        /// <summary>
        /// 「観戦を終了して新規ゲーム」(UIManager.OnSimulationExit に配線)。
        /// 現在の設定で通常の(人間プレイヤーありの)新規ゲームを開始する。
        /// timeScale の復帰と観戦UIの解除は ApplyState が行う。
        /// </summary>
        void ExitSimulationToNormalGame()
        {
            if (!simulationMode) return;
            StartNewGame();
        }

        // ==================================================================
        // タイトル画面フック(2026-07-21 Claude Code 追加)
        // ==================================================================
        // UI/TitleScreen.cs(独立Canvas)から呼ばれる最小の公開フック3つ。
        // いずれも既存の内部配線(スロットロード・ゲーム設定パネル・観戦開始)へ
        // 委譲するだけで、新しい状態や経路は持たない。

        /// <summary>タイトルの「つづける(クイックロード)」。スロット1をロードする
        /// (F9・OnLoadGame と同一経路。スロットが無い/壊れている場合は既存の安全側動作)。</summary>
        public void QuickLoadFromTitle()
        {
            LoadGameFromSlot(1);
        }

        /// <summary>
        /// タイトルの「新しいゲーム / 設定」。既存のゲーム設定パネルを開く。
        /// UIManager に公開の開閉APIが無いため(本ラウンドでは UIManager は変更対象外)、
        /// トップバーの「ゲーム設定」ボタン(固定名 GameSettingsButton)の onClick を
        /// 起動して既存の配線(OnGameSettingsButtonClicked)をそのまま使う。
        /// タイトル表示中は他パネルが閉じているため、トグルは常に「開く」動作になる。
        /// ボタンが見つからない場合は何もしない(安全な no-op)。
        /// </summary>
        public void OpenSettingsFromTitle()
        {
            if (uiManager == null || state == null || state.IsGameOver) return;
            var buttons = uiManager.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "GameSettingsButton")
                {
                    buttons[i].onClick.Invoke();
                    return;
                }
            }
        }

        /// <summary>タイトルの「シミュレーション観戦」。ゲーム設定パネルの
        /// 「シミュレーション観戦で開始」ボタンと同一経路(シードはランダム)。</summary>
        public void StartSpectatorFromTitle()
        {
            StartSimulationGame(0);
        }

        // ==================================================================
        // フルスクリーンモード(2026-07-21 Claude Code 追加)
        // ==================================================================

        /// <summary>
        /// 保存済みのフルスクリーン設定を起動時に1回だけ適用する(Start から呼ばれる)。
        /// エディタでは Game ビューの解像度を変えられないため実切替はスキップし、
        /// 状態(ラベル表示・PlayerPrefs)のみ同期する。
        /// </summary>
        void ApplyFullscreenFromPrefs()
        {
            fullscreenActive = PlayerPrefs.GetInt(FullscreenKey, 0) == 1;
            if (!Application.isEditor && fullscreenActive) ApplyDisplayMode(true);
        }

        /// <summary>
        /// フルスクリーン⇔ウィンドウの切替(UIManager.OnFullscreenToggled と F11 キーの両方から呼ばれる)。
        /// 設定を PlayerPrefs "HexCiv.Fullscreen" に保存し、UIのラベルを更新する。
        /// エディタでは実切替をせず状態のみ反転する(ビルドと同じ操作感を保つための安全な no-op)。
        /// </summary>
        void ToggleFullscreen()
        {
            fullscreenActive = !fullscreenActive;
            PlayerPrefs.SetInt(FullscreenKey, fullscreenActive ? 1 : 0);
            PlayerPrefs.Save();
            if (!Application.isEditor) ApplyDisplayMode(fullscreenActive);
            if (uiManager != null) uiManager.SetFullscreenState(fullscreenActive);
        }

        /// <summary>
        /// 実際の画面モード切替。フルスクリーンはディスプレイのネイティブ解像度で
        /// FullScreenWindow(ボーダーレス)、ウィンドウは 1280×720(画面中央へ移動を試みる)。
        /// </summary>
        static void ApplyDisplayMode(bool fullscreen)
        {
            if (fullscreen)
            {
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight,
                    FullScreenMode.FullScreenWindow);
            }
            else
            {
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                // 可能ならウィンドウを画面中央へ(失敗しても切替自体には影響しない)
                try
                {
                    var info = Screen.mainWindowDisplayInfo;
                    var pos = new Vector2Int(Mathf.Max(0, (info.width - 1280) / 2),
                        Mathf.Max(0, (info.height - 720) / 2));
                    Screen.MoveMainWindowTo(info, pos);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("ウィンドウ位置の調整に失敗(切替は継続): " + ex.Message);
                }
            }
        }

        /// <summary>
        /// PlayerPrefs のゲーム設定(マップサイズ・文明数・マップ種別・難易度)から GameConfig を作る(2026-07-20 Claude Code 追加)。
        /// 設定が無い・不正な場合は従来既定(44×26・4文明・大陸・普通)に丸める。Seed は 0(BuildNewGame がランダム化)。
        /// </summary>
        static GameConfig CreateConfigFromSettings()
        {
            var config = new GameConfig();
            int sizeIndex = Mathf.Clamp(PlayerPrefs.GetInt(MapSizeKey, 1), 0, MapSizeWidths.Length - 1);
            config.MapWidth = MapSizeWidths[sizeIndex];
            config.MapHeight = MapSizeHeights[sizeIndex];
            config.NumPlayers = Mathf.Clamp(PlayerPrefs.GetInt(NumPlayersKey, config.NumPlayers), 2, 8);
            config.MapType = Mathf.Clamp(PlayerPrefs.GetInt(MapTypeKey, 0), 0, 2);
            config.Difficulty = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, 1), 0, 2);
            return config;
        }

        IList<string> CreateCivilizationRoster(GameConfig config)
        {
            var roster = new List<string>();
            if (config == null) return roster;

            for (int i = 0; i < config.NumPlayers; i++) roster.Add(null);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 観戦モード(HumanPlayerIndex=-1)でも、選択中の文明はスロット0のAIとして登場させる
            // (2026-07-20 Claude Code 追加。人間ありの経路では従来どおり HumanPlayerIndex スロット)
            int preferredSlot = config.HumanPlayerIndex >= 0 ? config.HumanPlayerIndex : 0;
            if (preferredSlot < roster.Count &&
                CivilizationCatalog.Find(selectedHumanCivilizationId) != null)
            {
                roster[preferredSlot] = selectedHumanCivilizationId;
                used.Add(selectedHumanCivilizationId);
            }

            int candidateIndex = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                if (!string.IsNullOrEmpty(roster[i])) continue;
                CivilizationDef candidate = null;
                int attempts = 0;
                while (attempts < CivilizationCatalog.All.Count)
                {
                    var next = CivilizationCatalog.DefaultForSlot(candidateIndex++);
                    attempts++;
                    if (next != null && !used.Contains(next.Id))
                    {
                        candidate = next;
                        break;
                    }
                }
                if (candidate == null) candidate = CivilizationCatalog.DefaultForSlot(i);
                roster[i] = candidate != null ? candidate.Id : null;
                if (candidate != null) used.Add(candidate.Id);
            }

            return roster;
        }

        IList<string> CreateLeaderRoster(GameConfig config, IList<string> civilizationRoster)
        {
            var roster = new List<string>();
            if (civilizationRoster == null) return roster;
            // 観戦モードでは選択中の指導者もスロット0に適用する(文明ロスターと同じ規約)
            int preferredSlot = (config != null && config.HumanPlayerIndex >= 0)
                ? config.HumanPlayerIndex : 0;
            for (int i = 0; i < civilizationRoster.Count; i++)
            {
                string civilizationId = civilizationRoster[i];
                LeaderDef leader = null;
                if (config != null && i == preferredSlot &&
                    LeaderCatalog.BelongsTo(selectedHumanLeaderId, civilizationId))
                    leader = LeaderCatalog.Find(selectedHumanLeaderId);
                if (leader == null) leader = LeaderCatalog.DefaultForCivilization(civilizationId);
                roster.Add(leader != null ? leader.Id : null);
            }
            return roster;
        }

        void ChooseCivilization(string civilizationId)
        {
            var civilization = CivilizationCatalog.Find(civilizationId);
            if (civilization == null) return;
            selectedHumanCivilizationId = civilization.Id;
            var defaultLeader = LeaderCatalog.DefaultForCivilization(civilization.Id);
            selectedHumanLeaderId = defaultLeader != null ? defaultLeader.Id : "";
            PlayerPrefs.SetString(SelectedCivilizationKey, selectedHumanCivilizationId);
            PlayerPrefs.SetString(SelectedLeaderKey, selectedHumanLeaderId);
            PlayerPrefs.Save();
            StartNewGame();
        }

        void ChooseLeader(string leaderId)
        {
            if (!LeaderCatalog.BelongsTo(leaderId, selectedHumanCivilizationId)) return;
            selectedHumanLeaderId = leaderId;
            PlayerPrefs.SetString(SelectedLeaderKey, selectedHumanLeaderId);
            PlayerPrefs.Save();
            StartNewGame();
        }

        /// <summary>
        /// 指定の GameState を現行状態として世界を再構築する(新規開始・リスタート・ロードで共通)。
        /// 描画/UI/カメラ/入力を state に合わせて初期化し直す。
        /// </summary>
        void ApplyState(GameState newState)
        {
            UnsubscribeStateEvents();   // 旧stateの購読を解除(再Applyでの二重発火防止。2026-07-20 追加)
            state = newState;
            if (state != null && state.HumanPlayer != null &&
                CivilizationCatalog.Find(state.HumanPlayer.CivilizationId) != null)
                selectedHumanCivilizationId = state.HumanPlayer.CivilizationId;
            if (state != null && state.HumanPlayer != null &&
                LeaderCatalog.BelongsTo(state.HumanPlayer.LeaderId, selectedHumanCivilizationId))
                selectedHumanLeaderId = state.HumanPlayer.LeaderId;
            turnManager = new TurnManager(state, new AIController());

            EnsureWorldObjects();
            audioManager.Init(state);
            WireActions();

            mapRenderer.Init(state);
            entityRenderer.Init(state);
            uiManager.Init(state, actions);

            SetupCameraAndLight();

            inputController = new InputController(state, mapRenderer, entityRenderer, uiManager, actions, cameraController);

            // 「次のユニット」ボタン → 未行動ユニット巡回選択(2026-07-20 Claude Code 追加)。
            // フィールド参照のラムダなので、ロード等で inputController が差し替わっても常に現物を呼ぶ
            uiManager.OnNextUnit = () =>
            {
                if (inputController != null) inputController.SelectNextIdleUnit();
            };
            uiManager.OnCivilizationChosen = ChooseCivilization;
            uiManager.OnLeaderChosen = ChooseLeader;
            uiManager.OnNewGameRequested = StartNewGameWithSeed;   // ゲーム設定画面(2026-07-20 Claude Code 追加)

            // フルスクリーン切替の配線と現在状態のラベル反映(2026-07-21 Claude Code 追加)
            uiManager.OnFullscreenToggled = ToggleFullscreen;
            uiManager.SetFullscreenState(fullscreenActive);

            // ---- シミュレーション観戦の配線と状態反映(2026-07-20 Claude Code 追加) ----
            uiManager.OnSimulationStartRequested = StartSimulationGame;
            uiManager.OnSimulationPauseToggled = ToggleSimulationPause;
            uiManager.OnSimulationSpeedCycled = CycleSimulationSpeed;
            uiManager.OnSimulationExit = ExitSimulationToNormalGame;

            // 観戦モードは state 自体から導出する(新規/リスタート/ロードの全経路で一貫)。
            // 通常ゲームへ戻る経路では timeScale を必ず 1 に復帰させる。
            simulationMode = state != null && state.HumanPlayer == null;
            simulationPaused = false;
            Time.timeScale = simulationMode ? simulationSpeed : 1f;
            nextSimulationTurnAt = Time.unscaledTime + SimulationIntervalSeconds;
            uiManager.SetSimulationMode(simulationMode);
            uiManager.SetSimulationStatus(simulationPaused, simulationSpeed);

            // ---- 観戦演出:型付きイベントの購読(2026-07-20 Claude Code 追加) ----
            SubscribeStateEvents();
            simulationEventHoldUntil = 0f;
            lastKnownCapitalPos.Clear();
            if (simulationMode) RememberCapitalPositions();

            lastSeenVersion = -1;
            gameOverShown = false;
        }

        // ==================================================================
        // 観戦演出:カメラジャンプ+バナー(2026-07-20 Claude Code 追加)
        // ==================================================================

        /// <summary>現在の state の型付きイベントを購読する(subscribedState で二重購読を防ぐ)。</summary>
        void SubscribeStateEvents()
        {
            if (state == null || subscribedState == state) return;
            UnsubscribeStateEvents();
            subscribedState = state;
            subscribedState.OnWarDeclared += HandleWarDeclared;
            subscribedState.OnPeaceMade += HandlePeaceMade;
            subscribedState.OnCityCaptured += HandleCityCaptured;
            subscribedState.OnPlayerEliminated += HandlePlayerEliminated;
            subscribedState.OnGameEnded += HandleGameEnded;
        }

        void UnsubscribeStateEvents()
        {
            if (subscribedState == null) return;
            subscribedState.OnWarDeclared -= HandleWarDeclared;
            subscribedState.OnPeaceMade -= HandlePeaceMade;
            subscribedState.OnCityCaptured -= HandleCityCaptured;
            subscribedState.OnPlayerEliminated -= HandlePlayerEliminated;
            subscribedState.OnGameEnded -= HandleGameEnded;
            subscribedState = null;
        }

        void HandleWarDeclared(Player aggressor, Player defender)
        {
            if (aggressor == null || defender == null) return;
            ShowSimulationEvent(MidpointOfCapitals(aggressor, defender),
                $"⚔ {aggressor.NameJa} が {defender.NameJa} に宣戦布告!", WarBannerColor);
        }

        void HandlePeaceMade(Player a, Player b)
        {
            if (a == null || b == null) return;
            ShowSimulationEvent(MidpointOfCapitals(a, b),
                $"🕊 和平: {a.NameJa} と {b.NameJa}", PeaceBannerColor);
        }

        void HandleCityCaptured(City city, Player oldOwner, Player newOwner)
        {
            if (city == null || newOwner == null) return;
            ShowSimulationEvent(city.Coord.ToWorld(),
                $"🏰 都市「{city.NameJa}」陥落 → {newOwner.NameJa}", CaptureBannerColor);

            // 都市占領のカメラシェイク(2026-07-21 Claude Code 追加)。観戦モードでは常に、
            // 通常プレイでは人間プレイヤーが関与する陥落(自都市の喪失/自軍による占領)のみ。
            // Shake はカメラ最終位置へのポストオフセットのため、直前の FocusOn とも競合しない。
            var human = state != null ? state.HumanPlayer : null;
            bool involvesHuman = human != null &&
                ((oldOwner != null && oldOwner.Id == human.Id) || newOwner.Id == human.Id);
            if (cameraController != null && (simulationMode || involvesHuman))
                cameraController.Shake(0.15f, 0.35f);
        }

        void HandlePlayerEliminated(Player p)
        {
            if (p == null) return;
            // 滅亡時点で都市は失われているため、直前に記録した首都位置があればそこへ。無ければジャンプなし
            Vector3 last;
            Vector3? focus = lastKnownCapitalPos.TryGetValue(p.Id, out last) ? last : (Vector3?)null;
            ShowSimulationEvent(focus, $"☠ {p.NameJa} 滅亡", EliminationBannerColor);
        }

        void HandleGameEnded(Player winner, string messageJa)
        {
            if (winner == null) return;   // 勝者なしの終了はゲームオーバー表示に任せる
            ShowSimulationEvent(CapitalPosition(winner),
                $"👑 勝者: {winner.NameJa}", VictoryBannerColor);
        }

        /// <summary>
        /// イベント演出の共通処理(2026-07-21 Claude Code 変更:通常プレイの控えめ通知に対応)。
        /// 観戦モード中:カメラを1回だけ事件現場へジャンプし(focus が null ならジャンプなし。
        /// 以後の手動カメラ操作がそのまま優先され、強制的な再フォーカスはしない)、大きな
        /// バナーを表示し、ターン自動進行を約1.2秒(非スケール時間)停止する。ユーザーの
        /// 一時停止状態は変更しない。
        /// 通常プレイ中:小さめのコンパクトバナーのみ表示する(カメラジャンプ・ターン停止・
        /// 追加SEなし。人間プレイヤー絡みの警告SEは従来どおり GameAudio がログ経由で再生する)。
        /// </summary>
        void ShowSimulationEvent(Vector3? focus, string messageJa, Color accent)
        {
            if (state == null) return;
            if (!simulationMode)
            {
                if (uiManager != null) uiManager.ShowEventBannerCompact(messageJa, accent);
                return;
            }
            simulationEventHoldUntil = Time.unscaledTime + SimulationEventHoldSeconds;
            if (focus.HasValue && cameraController != null) cameraController.FocusOn(focus.Value);
            if (uiManager != null) uiManager.ShowEventBanner(messageJa, accent);
        }

        /// <summary>
        /// 生存プレイヤーの首都位置を記録する(観戦モード中、Version 変化ごとに呼ばれる)。
        /// 滅亡イベントは都市喪失後に発生するため、この直前の記録を滅亡演出のジャンプ先に使う。
        /// </summary>
        void RememberCapitalPositions()
        {
            if (state == null) return;
            for (int i = 0; i < state.Players.Count; i++)
            {
                var p = state.Players[i];
                if (p.IsEliminated) continue;
                var pos = CapitalPosition(p);
                if (pos.HasValue) lastKnownCapitalPos[p.Id] = pos.Value;
            }
        }

        /// <summary>プレイヤーの首都(無ければ先頭都市)のワールド座標。都市が無ければ null。</summary>
        static Vector3? CapitalPosition(Player p)
        {
            if (p == null) return null;
            if (p.CapitalCityId >= 0)
                for (int i = 0; i < p.Cities.Count; i++)
                    if (p.Cities[i].Id == p.CapitalCityId) return p.Cities[i].Coord.ToWorld();
            if (p.Cities.Count > 0) return p.Cities[0].Coord.ToWorld();
            return null;
        }

        /// <summary>両者の首都の中間点。片方しか無ければその首都、両方無ければ null。</summary>
        static Vector3? MidpointOfCapitals(Player a, Player b)
        {
            var pa = CapitalPosition(a);
            var pb = CapitalPosition(b);
            if (pa.HasValue && pb.HasValue) return (pa.Value + pb.Value) * 0.5f;
            return pa.HasValue ? pa : pb;
        }

        /// <summary>
        /// 指定スロット(1..3)のセーブファイルパス(永続データフォルダ直下)。
        /// ファイル名規約は UIManager.SaveSlotPath(スロット一覧のメタデータ表示)と同一に保つこと。
        /// </summary>
        static string SaveSlotPath(int slot)
        {
            return System.IO.Path.Combine(Application.persistentDataPath, $"hexciv_save_slot{slot}.json");
        }

        /// <summary>スロット化以前(1スロット時代)のセーブファイルパス。</summary>
        static string LegacySaveFilePath()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, "hexciv_save.json");
        }

        /// <summary>
        /// 旧1スロットセーブ(hexciv_save.json)が残っていて、かつスロット1が空なら
        /// スロット1へ移動する(2026-07-20 Claude Code 追加)。失敗しても起動は継続する(非致命)。
        /// </summary>
        static void MigrateLegacySaveFile()
        {
            try
            {
                string legacy = LegacySaveFilePath();
                string slot1 = SaveSlotPath(1);
                if (System.IO.File.Exists(legacy) && !System.IO.File.Exists(slot1))
                {
                    System.IO.File.Move(legacy, slot1);
                    Debug.Log("旧セーブデータをスロット1へ移行しました");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("旧セーブデータの移行に失敗(起動は継続): " + ex.Message);
            }
        }

        /// <summary>描画/UI/カメラのコンポーネントを子オブジェクトとして(初回のみ)生成する。</summary>
        void EnsureWorldObjects()
        {
            if (mapRenderer == null)
            {
                var go = new GameObject("MapRenderer");
                go.transform.SetParent(transform, false);
                mapRenderer = go.AddComponent<MapRenderer>();
            }
            if (entityRenderer == null)
            {
                var go = new GameObject("EntityRenderer");
                go.transform.SetParent(transform, false);
                entityRenderer = go.AddComponent<EntityRenderer>();
            }
            if (uiManager == null)
            {
                var go = new GameObject("UIManager");
                go.transform.SetParent(transform, false);
                uiManager = go.AddComponent<UIManager>();
            }
            if (cameraController == null)
            {
                var go = new GameObject("CameraRig");
                go.transform.SetParent(transform, false);
                cameraController = go.AddComponent<CameraController>();
            }
            if (audioManager == null)
            {
                var go = new GameObject("GameAudio");
                go.transform.SetParent(transform, false);
                audioManager = go.AddComponent<GameAudio>();
            }
        }

        /// <summary>UI/入力からシミュレーションへの操作コールバックを配線する。</summary>
        void WireActions()
        {
            actions = new GameActions
            {
                OnEndTurn = () =>
                {
                    if (state == null || state.IsGameOver) return;
                    audioManager?.PlayEndTurn();
                    turnManager.EndTurn();
                    if (uiManager != null) uiManager.NotifyTutorialEvent("turn_ended");   // チュートリアル連動
                    AutoSelectIdleUnitAtTurnStart();   // ターン開始時の未行動ユニット自動選択(2026-07-20 Claude Code 追加)
                },

                OnChooseProduction = (city, item) =>
                {
                    if (state == null || state.IsGameOver || city == null) return;
                    var human = state.HumanPlayer;
                    if (human != null && city.PlayerId != human.Id) return;
                    city.SetProduction(item);
                    state.Bump();
                    if (uiManager != null) uiManager.NotifyTutorialEvent("production_chosen");   // チュートリアル連動
                },

                OnChooseResearch = techId =>
                {
                    if (state == null || state.IsGameOver) return;
                    var human = state.HumanPlayer;
                    if (human == null) return;
                    human.SetResearch(techId);
                    state.Bump();
                    if (uiManager != null) uiManager.NotifyTutorialEvent("research_chosen");   // チュートリアル連動
                },

                OnFoundCity = u =>
                {
                    if (state == null || state.IsGameOver) return;
                    if (u == null || u.IsDead || u.DefId != "settler") return;
                    var owner = state.GetPlayer(u.PlayerId);
                    if (owner == null || !state.CanFoundCityAt(owner, u.Coord)) return;
                    state.FoundCity(owner, u.Coord);
                    state.KillUnit(u);   // 開拓者は消費される
                    audioManager?.PlayFoundCity();
                    state.Bump();
                    if (uiManager != null) uiManager.NotifyTutorialEvent("city_founded");   // チュートリアル連動
                },

                OnFortify = u =>
                {
                    if (state == null || state.IsGameOver || u == null || u.IsDead) return;
                    u.Fortified = true;
                    u.GotoPath = null;
                    u.MovesLeft = 0;
                    state.Bump();
                },

                OnSkip = u =>
                {
                    if (state == null || state.IsGameOver || u == null || u.IsDead) return;
                    u.GotoPath = null;
                    u.MovesLeft = 0;
                    state.Bump();
                },

                OnRestart = () =>
                {
                    // 観戦ゲームからの「もう一度プレイ」は新しい観戦ゲームを開始する
                    // (通常ゲームからは従来どおり人間ありの新規ゲーム。2026-07-20 Claude Code 追加)
                    if (simulationMode) StartSimulationGame(0);
                    else StartNewGame();
                },

                // 旧API(引数なし)はスロット1の別名として維持する(2026-07-20 スロット3枠化)
                OnSaveGame = () => SaveGameToSlot(1),
                OnLoadGame = () => LoadGameFromSlot(1),
                OnSaveGameSlot = slot => SaveGameToSlot(slot),
                OnLoadGameSlot = slot => LoadGameFromSlot(slot),
            };
        }

        /// <summary>
        /// 指定スロットへセーブする(2026-07-20 スロット3枠化)。
        /// 人間プレイヤーの手番中(=通常プレイ中)かつゲーム終了前のみ有効(従来のガードと同一)。
        /// </summary>
        void SaveGameToSlot(int slot)
        {
            if (slot < 1 || slot > SaveSlotCount) return;
            if (state == null || state.IsGameOver) return;
            var human = state.HumanPlayer;
            if (human == null || human.IsEliminated) return;
            try
            {
                SaveLoad.SaveToFile(state, SaveSlotPath(slot));
                state.EmitLog($"スロット{slot}にセーブしました");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("セーブ失敗: " + ex.Message);
                state.EmitLog("セーブに失敗しました");
            }
            state.Bump();
        }

        /// <summary>
        /// 指定スロットからロードする(2026-07-20 スロット3枠化)。
        /// 先に純シミュレーション側の復元を完了させ、成功した場合のみ世界を差し替える
        /// (失敗時は現行のゲームを継続。従来の安全側動作と同一)。
        /// </summary>
        void LoadGameFromSlot(int slot)
        {
            if (slot < 1 || slot > SaveSlotCount) return;
            if (state == null) return;
            string path = SaveSlotPath(slot);
            if (!System.IO.File.Exists(path))
            {
                state.EmitLog("セーブデータがありません");
                state.Bump();
                return;
            }
            try
            {
                var loaded = SaveLoad.LoadFromFile(path);
                if (loaded == null) throw new Exception("セーブデータを読み込めなかった");
                ApplyState(loaded);
                state.EmitLog($"スロット{slot}のデータをロードしました");
                state.Bump();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("ロード失敗: " + ex.Message);
                state.EmitLog("ロードに失敗しました");
                state.Bump();
            }
        }

        /// <summary>
        /// ターン開始時の未行動ユニット自動選択(2026-07-20 Claude Code 追加)。
        /// ターン終了処理が人間に手番を返した直後に1回だけ呼ばれる。
        /// ゲーム終了時・観戦モード(人間プレイヤー不在)時は何もしない。
        /// 都市/技術パネル表示中も何もしない(プレイヤーが開いているパネルを勝手に閉じないため)。
        /// </summary>
        void AutoSelectIdleUnitAtTurnStart()
        {
            if (state == null || state.IsGameOver) return;
            if (state.HumanPlayer == null) return;
            if (uiManager != null && uiManager.IsCityOrTechPanelOpen) return;
            if (inputController != null) inputController.SelectNextIdleUnit();
        }

        /// <summary>カメラ(暗い紺の背景・マップ境界クランプ・人間の開拓者へフォーカス)と平行光源。</summary>
        void SetupCameraAndLight()
        {
            var bounds = ComputeWorldBoundsXZ();
            var focus = ComputeStartFocus();

            cameraController.Init(bounds, focus);

            var cam = cameraController.Cam != null ? cameraController.Cam : Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.07f, 0.13f);   // 暗い紺
                if (FindObjectOfType<AudioListener>() == null)
                    cam.gameObject.AddComponent<AudioListener>();
            }

            if (FindObjectOfType<Light>() == null)
            {
                var lgo = new GameObject("Directional Light");
                var light = lgo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lgo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        /// <summary>マップ全タイルのワールド座標からXZ境界を求める(x→X, y→Z)。</summary>
        Rect ComputeWorldBoundsXZ()
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var t in state.Map.AllTiles)
            {
                var w = t.Coord.ToWorld();
                if (w.x < minX) minX = w.x;
                if (w.x > maxX) maxX = w.x;
                if (w.z < minZ) minZ = w.z;
                if (w.z > maxZ) maxZ = w.z;
            }
            if (minX > maxX) { minX = maxX = 0f; }
            if (minZ > maxZ) { minZ = maxZ = 0f; }
            return new Rect(minX, minZ, maxX - minX, maxZ - minZ);
        }

        /// <summary>
        /// 開始時の注視点:人間プレイヤーの首都(ロード時)→ 開拓者 → 先頭ユニット → マップ中央。
        /// 新規開始時は都市が無いため従来どおり開拓者にフォーカスする。
        /// </summary>
        Vector3 ComputeStartFocus()
        {
            var human = state.HumanPlayer;
            if (human != null && human.CapitalCityId >= 0)
            {
                for (int i = 0; i < human.Cities.Count; i++)
                    if (human.Cities[i].Id == human.CapitalCityId)
                        return human.Cities[i].Coord.ToWorld();
            }
            if (human != null && human.Units.Count > 0)
            {
                Unit focusUnit = null;
                for (int i = 0; i < human.Units.Count; i++)
                    if (human.Units[i].DefId == "settler") { focusUnit = human.Units[i]; break; }
                if (focusUnit == null) focusUnit = human.Units[0];
                return focusUnit.Coord.ToWorld();
            }
            return HexCoord.FromOffset(state.Map.Width / 2, state.Map.Height / 2).ToWorld();
        }
    }
}
