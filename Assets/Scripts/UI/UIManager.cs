using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Core;
using HexCiv.Audio;

namespace HexCiv.UI
{
    /// <summary>
    /// ゲーム内UI全体(uGUI・全てコード生成)。
    /// トップバー / ユニットパネル / 都市パネル / 技術パネル / ログ / タイルツールチップ /
    /// ゲームオーバー画面 / ターン終了ボタンを管理する。
    /// RefreshAll は毎フレーム呼ばれても安全(テキスト更新は軽量、リスト再構築は
    /// パネル表示中かつ state.Version 変化時のみ)。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        static readonly string[] TutorialTitles =
        {
            "まずは自分の文明を確認",
            "ユニットを選択する",
            "移動・攻撃を命令する",
            "開拓者で最初の都市を建てる",
            "研究と都市生産を選ぶ",
            "ターンを進めて勝利を目指す"
        };

        static readonly string[] TutorialBodies =
        {
            "あなたの文明名と色は画面右上に表示されます。開始地点の周囲以外は「戦場の霧」で隠れています。まず画面中央付近にある自分の開拓者と戦士を探しましょう。",
            "自分の駒を左クリックすると選択できます。駒には「開拓者」「戦士」などの名前が表示され、画面左下にはHP・移動力・戦闘力が表示されます。",
            "選択後、黄色いマスへ右クリックすると移動します。赤いマスは攻撃できる敵です。右クリックした場所が遠い場合は、自動で経路を探して数ターンかけて移動します。",
            "開拓者を選び、左下の「都市建設」を押してください。都市を建てると周囲の土地を領有し、食料・生産・科学を生み出せるようになります。",
            "上部の「研究を選択」で技術を、都市を左クリックして開くパネルで生産物を選びます。最初は研究「陶器」または「弓術」、生産「戦士」がおすすめです。",
            "行動が終わったら右下の「ターン終了」またはEnterキーを押します。探索・都市拡張・技術研究・軍備を進め、最後まで生き残るか、他文明を制覇すれば勝利です。"
        };

        /// <summary>
        /// 各チュートリアルページが「完了」とみなす実操作イベントID(2026-07-20 Claude Code 追加)。
        /// null のページは対応する操作が無く、手動ナビゲーションのみ。
        /// イベントは InputController / GameBootstrap から NotifyTutorialEvent 経由で届く。
        /// </summary>
        static readonly string[][] TutorialEventIds =
        {
            null,                                              // 1: 文明の確認(操作なし・手動のみ)
            new[] { "unit_selected" },                         // 2: 自軍ユニットを選択
            new[] { "unit_moved" },                            // 3: 移動命令を出す
            new[] { "city_founded" },                          // 4: 都市を建設
            new[] { "research_chosen", "production_chosen" },  // 5: 研究または生産を選択
            new[] { "turn_ended" }                             // 6: ターン終了
        };

        GameState state;
        GameActions actions;

        Canvas canvas;
        RectTransform canvasRect;

        // ---- トップバー ----
        GameObject topBar;
        Text turnText;
        Text scienceText;
        Button researchButton;
        Text researchButtonLabel;
        Text civNameText;
        Image civSwatch;

        // ---- サウンド設定 ----
        GameObject audioPanel;
        Text musicVolumeLabel;
        Text sfxVolumeLabel;
        Text muteLabel;

        // ---- ターン終了 ----
        Button endTurnButton;

        // ---- 次のユニット(2026-07-20 Claude Code 追加) ----
        Button nextUnitButton;

        /// <summary>
        /// 「次のユニット」ボタンのコールバック(GameBootstrap が
        /// InputController.SelectNextIdleUnit へ配線する。null なら何もしない)。
        /// </summary>
        public Action OnNextUnit;

        // ---- セーブ/ロード ----
        Button saveButton;
        Button loadButton;
        Button civilizationButton;
        Button leaderButton;

        // ---- セーブスロット選択(2026-07-20 Claude Code 追加、3スロット) ----
        const int SaveSlotCount = 3;
        GameObject slotPanel;
        Text slotTitleText;
        readonly Button[] slotButtons = new Button[SaveSlotCount];
        readonly Text[] slotLabels = new Text[SaveSlotCount];
        bool slotSaveMode;   // true=セーブ先選択、false=ロード元選択

        // ---- セーブスロットのミニマップサムネイル(2026-07-21 Claude Code 追加) ----
        /// <summary>サムネイル画像サイズ(px)。標準マップ44×26でタイルあたり約2pxになる。</summary>
        const int SlotThumbWidth = 96;
        const int SlotThumbHeight = 56;
        /// <summary>各スロット行のサムネイル表示(空き・破損スロットでは非表示)。</summary>
        readonly RawImage[] slotThumbImages = new RawImage[SaveSlotCount];
        /// <summary>スロットごとのサムネイルテクスチャ(固定96×56。再Init後も再利用する)。</summary>
        readonly Texture2D[] slotThumbTextures = new Texture2D[SaveSlotCount];
        /// <summary>描画済みセーブファイルの LastWriteTimeUtc.Ticks(0=未描画)。同一なら再解析しない。</summary>
        readonly long[] slotThumbStamps = new long[SaveSlotCount];
        /// <summary>スタンプ時点で有効なサムネイルを描けたか(false=破損等でそのスタンプ中は非表示)。</summary>
        readonly bool[] slotThumbValid = new bool[SaveSlotCount];

        // ---- 文明選択 ----
        const int CivilizationsPerPage = 12;
        GameObject civilizationPanel;
        RectTransform civilizationListRoot;
        Text civilizationPageText;
        Button civilizationPrevButton;
        Button civilizationNextButton;
        int civilizationPage;

        /// <summary>文明IDを受け取り、その文明で新しいゲームを開始するコールバック。</summary>
        public Action<string> OnCivilizationChosen;

        // ---- 指導者選択 ----
        const int LeadersPerPage = 5;
        GameObject leaderPanel;
        RectTransform leaderListRoot;
        Text leaderHeaderText;
        Text leaderPageText;
        Button leaderPrevButton;
        Button leaderNextButton;
        int leaderPage;

        /// <summary>指導者IDを受け取り、その指導者で新しいゲームを開始するコールバック。</summary>
        public Action<string> OnLeaderChosen;

        // ---- ゲーム設定(2026-07-20 Claude Code 追加) ----
        /// <summary>マップサイズ設定の PlayerPrefs キー(0=小,1=標準,2=大)。
        /// キーと値の規約は GameBootstrap.CreateConfigFromSettings と同一に保つこと。</summary>
        const string MapSizeKey = "HexCiv.MapSize";
        /// <summary>文明数設定の PlayerPrefs キー(2/3/4/6/8)。</summary>
        const string NumPlayersKey = "HexCiv.NumPlayers";
        /// <summary>マップ種別設定の PlayerPrefs キー(0=大陸,1=パンゲア,2=群島。2026-07-20 追加)。
        /// キーと値の規約は GameBootstrap.CreateConfigFromSettings と同一に保つこと。</summary>
        const string MapTypeKey = "HexCiv.MapType";
        /// <summary>難易度設定の PlayerPrefs キー(0=やさしい,1=普通,2=むずかしい。2026-07-20 追加)。
        /// キーと値の規約は GameBootstrap.CreateConfigFromSettings と同一に保つこと。</summary>
        const string DifficultyKey = "HexCiv.Difficulty";
        static readonly string[] MapSizeLabelsJa = { "小 (40×24)", "標準 (44×26)", "大 (56×34)" };
        static readonly string[] MapTypeLabelsJa = { "大陸", "パンゲア", "群島" };
        static readonly string[] DifficultyLabelsJa = { "やさしい", "普通", "むずかしい" };
        static readonly int[] NumPlayersChoices = { 2, 3, 4, 6, 8 };
        GameObject settingsPanel;
        Button settingsButton;
        readonly Button[] mapSizeButtons = new Button[3];
        readonly Button[] mapTypeButtons = new Button[3];
        readonly Button[] difficultyButtons = new Button[3];
        readonly Button[] numPlayersButtons = new Button[5];
        InputField seedInput;

        // ---- 演出モード(2026-07-22 Claude Code 追加) ----
        /// <summary>演出モードの PlayerPrefs キー(0=標準,1=軽量)。読み側は Rendering/VisualQuality.cs。
        /// キー規約を同一に保つこと。軽量では雲影・水面ゆらぎ・待機ボブ・ダメージ数字を省略する。</summary>
        const string FxLightKey = "HexCiv.FxLight";
        Button fxQualityButton;
        Text fxQualityLabel;

        int settingsMapSizeIndex = 1;
        int settingsMapType = 0;
        int settingsDifficulty = 1;
        int settingsNumPlayers = 4;

        /// <summary>ゲーム設定画面の開始ボタンのコールバック。シード(0=ランダム)を受け取り、
        /// PlayerPrefs に保存済みのマップサイズ・文明数で新しいゲームを開始する(GameBootstrap が配線)。</summary>
        public Action<int> OnNewGameRequested;

        // ---- フルスクリーン(2026-07-21 Claude Code 追加) ----
        /// <summary>ゲーム設定画面の「フルスクリーン: ON/OFF」トグル行。実際の画面切替と
        /// PlayerPrefs "HexCiv.Fullscreen" の保存は GameBootstrap が行い、UIは要求とラベル表示のみ。</summary>
        Button fullscreenButton;
        Text fullscreenLabel;
        /// <summary>現在のフルスクリーン状態(SetFullscreenState で同期。再Init 後のラベル復元にも使う)。</summary>
        bool fullscreenOn;

        /// <summary>フルスクリーン切替の要求(設定画面のトグル行と F11 キーの両方が呼ぶ。GameBootstrap が配線)。</summary>
        public Action OnFullscreenToggled;

        // ---- シミュレーション観戦(2026-07-20 Claude Code 追加) ----
        GameObject simulationBar;
        Text simulationPauseLabel;
        Text simulationSpeedLabel;
        Text simulationTurnText;
        bool simulationModeActive;

        /// <summary>ゲーム設定画面の「シミュレーション観戦で開始」。シード(0=ランダム)を受け取り、
        /// 全文明AIの観戦ゲームを開始する(GameBootstrap が配線)。</summary>
        public Action<int> OnSimulationStartRequested;
        /// <summary>観戦バーの一時停止/再開トグル(GameBootstrap が配線)。</summary>
        public Action OnSimulationPauseToggled;
        /// <summary>観戦バーの速度切替 1倍→2倍→…→128倍→256倍→1倍(GameBootstrap が配線)。</summary>
        public Action OnSimulationSpeedCycled;
        /// <summary>「観戦を終了して新規ゲーム」(現在の設定で通常ゲームを開始。GameBootstrap が配線)。</summary>
        public Action OnSimulationExit;

        // ---- 戦況グラフ(2026-07-20 Claude Code 追加) ----
        ScoreGraphPanel scoreGraphPanel;
        /// <summary>通常プレイ用の「戦況」開閉ボタン(二段目)。観戦中は観戦バー内のボタンを使うため隠す。</summary>
        Button scoreGraphButton;

        // ---- 時代表示(2026-07-22 Claude Code 追加) ----
        /// <summary>トップバー二段目の時代インジケーター(小アイコン+ラベル)。表示専用・raycast無効。</summary>
        GameObject eraIndicator;
        Image eraIconImage;
        Text eraLabelText;
        /// <summary>表示中の時代index(-1=未設定。変化した時だけスプライト・文言を更新)。</summary>
        int eraShownIndex = -1;
        /// <summary>時代名(閾値は GameAudio のBGM3楽章と同じ: ターン1〜100/101〜180/181〜)。</summary>
        static readonly string[] EraNamesJa = { "古代", "中世", "近代" };
        /// <summary>時代色(古代=ブロンズ/中世=シルバー/近代=ゴールド)。</summary>
        static readonly Color[] EraTints =
        {
            new Color(0.78f, 0.52f, 0.28f, 1f),
            new Color(0.78f, 0.80f, 0.86f, 1f),
            new Color(0.95f, 0.80f, 0.34f, 1f),
        };
        /// <summary>時代アイコンSprite(白マスク生成・Image.color で着色。実行中キャッシュ)。</summary>
        static readonly Sprite[] eraIconSprites = new Sprite[3];
        /// <summary>通常時のX(二段目: ログ約38%幅の右、「戦況」ボタン x704 の左の空き)。</summary>
        const float EraIndicatorNormalX = 614f;
        /// <summary>観戦時のX(観戦バー x340〜940 を避けた二段目左端。ログは y-80 へ移動済み)。</summary>
        const float EraIndicatorSimulationX = 12f;

        // ---- 首位文明チップ(2026-07-22 Claude Code 追加) ----
        /// <summary>トップバー二段目の「首位: <文明名>」チップ(色スウォッチ+ラベル)。表示専用・raycast無効。</summary>
        GameObject leaderChip;
        Image leaderChipSwatch;
        Text leaderChipLabel;
        /// <summary>次に首位を再計算する時刻(最大毎秒1回。Time.unscaledTime 基準)。</summary>
        float nextLeaderChipAt;
        /// <summary>通常時のX(ログ 〜x478 の右、時代表示 x614 の左の空き x478〜614 に収める)。</summary>
        const float LeaderChipNormalX = 480f;
        /// <summary>観戦時のX(時代表示 x12〜98 の右、観戦バー x340 の左の空き x98〜340 に収める)。</summary>
        const float LeaderChipSimulationX = 104f;
        /// <summary>首位チップの幅(通常の空き x478〜614・観戦の空き x98〜340 の双方に収まる)。</summary>
        const float LeaderChipWidth = 132f;

        // ---- 国家運営チップ+警告通知(2026-07-22 Claude Code 追加) ----
        /// <summary>
        /// トップバー二段目・左端の国家運営チップ列(「国庫 152 (+8)」「安定度 62」)。
        /// 値は Core/AdministrationSystem の公開APIから読むだけの表示専用で(raycast無効)、
        /// シミュレーションには一切影響しない。観戦モード(HumanPlayer==null)・ゲーム終了時は隠す。
        /// 配置は二段目 y-38〜-64 の x12〜約364(首位チップ x480 の手前)。ログ先頭は
        /// LogTopNormalY を -42→-68 へ下げて重なりを避けている。
        /// </summary>
        GameObject adminChips;
        Text treasuryChipLabel;
        Image stabilityChipSwatch;
        Text stabilityChipLabel;
        /// <summary>次に国家運営チップと警告判定を行う時刻(最大毎秒1回。Time.unscaledTime 基準)。</summary>
        float nextAdminChipAt;

        /// <summary>安定度の中位色(琥珀)。安定度チップの色スケールの中間点。</summary>
        static readonly Color StabilityMidColor = new Color(0.95f, 0.78f, 0.35f, 1f);
        /// <summary>安定度の高位色(緑)。色スケールの上端。</summary>
        static readonly Color StabilityHighColor = new Color(0.52f, 0.85f, 0.48f, 1f);
        /// <summary>収支が黒字のときの国庫チップ色(緑)。</summary>
        static readonly Color BalancePositiveColor = new Color(0.52f, 0.85f, 0.48f, 1f);

        /// <summary>
        /// 「安定度が低い」とみなす閾値。独自に決めず、Core/AdministrationSystem.RecommendTaxPolicy が
        /// 減税を勧告する条件(`player.Stability &lt;= 25`)と同じ値を参照して用いる(表示・警告専用)。
        /// </summary>
        const int LowStabilityThreshold = 25;
        /// <summary>同種の警告を再通知するまでの最短ターン数(長期戦での連呼を防ぐ)。</summary>
        const int WarningCooldownTurns = 12;
        /// <summary>各警告の「発生中」ラッチ(条件が解消されるまで再発火しない=エッジ検出)。</summary>
        bool warnDeficitLatched;
        bool warnStabilityLatched;
        bool warnIsolationLatched;
        /// <summary>各警告を最後に通知したターン(WarningCooldownTurns の判定用。-999=未通知)。</summary>
        int lastDeficitWarnTurn = -999;
        int lastStabilityWarnTurn = -999;
        int lastIsolationWarnTurn = -999;

        // ---- 独立Canvas UIの開閉検知(2026-07-21 Claude Code 追加。参照のみ・一切変更しない) ----
        /// <summary>Codex 実装の世界史図鑑(独立Canvas)。Esc のフルスクリーン解除判定のため開閉のみ読む。</summary>
        WorldHistoryPanel worldHistoryRef;
        Transform worldHistoryPanelRoot;
        /// <summary>Codex 実装の文化・政策UI(独立Canvas)。同上、開閉のみ読む。</summary>
        CulturePanel culturePanelRef;
        Transform culturePanelRoot;

        // ---- パネル開閉ボタンのアイコン装飾(2026-07-21 Claude Code 追加) ----
        /// <summary>Codex 実装の遺産・偉人・作品UI(独立Canvas)。開くボタンのアイコン付与のためだけに参照する。</summary>
        LegacyPanel legacyPanelRef;
        /// <summary>左下の独立Canvasボタン3つ(世界史図鑑・文化・政策・遺産・偉人・作品)への
        /// アイコン付与と、最背面キャンバスへのZ順退避(2026-07-21 追加)が完了したか。
        /// どちらもコンポーネントの追加のみ(サイズ・位置・ハンドラ不変)で、
        /// 二重付与チェックにより何度試しても安全。</summary>
        bool externalButtonIconsDone;
        /// <summary>次にボタン探索を試す時刻(独立Canvasの構築完了を0.5秒間隔で軽量ポーリング)。</summary>
        float nextExternalButtonIconTryAt;

        // ---- イベントバナー(2026-07-20 Claude Code 追加。2026-07-21 コンパクト版対応) ----
        class EventBannerEntry
        {
            public GameObject Root;
            public CanvasGroup Group;
            public float BornAt;
            /// <summary>通常プレイ用の小型バナーか(積み上げ間隔・寿命が異なる)。</summary>
            public bool Compact;
            /// <summary>このバナーの表示時間(秒、非スケール時間)。</summary>
            public float Lifetime;
            /// <summary>このバナーのフェードアウト開始時刻(表示からの経過秒)。</summary>
            public float FadeStart;
            /// <summary>このバナーの高さ(px)。縦積みスロット計算に使う(2026-07-21 追加)。</summary>
            public float Height;
        }
        /// <summary>表示中のイベントバナー(発生順。最大 MaxEventBanners 枚)。</summary>
        readonly List<EventBannerEntry> eventBanners = new List<EventBannerEntry>();
        const int MaxEventBanners = 3;
        /// <summary>バナー縦積みの最上段Y(トップバー・観戦バー・二段目ボタン列の下。2026-07-21 追加)。</summary>
        const float EventBannerStackTop = -84f;
        /// <summary>縦積み時のバナー同士の隙間(px。2026-07-21 追加)。</summary>
        const float EventBannerStackGap = 8f;
        /// <summary>観戦用の大バナーの表示時間(秒、非スケール時間)。</summary>
        const float EventBannerLifetime = 2.5f;
        /// <summary>観戦用の大バナーのフェードアウト開始時刻(表示からの経過秒)。</summary>
        const float EventBannerFadeStart = 1.8f;
        /// <summary>通常プレイ用コンパクトバナーの表示時間(約2秒でフェード完了)。</summary>
        const float CompactEventBannerLifetime = 2.0f;
        /// <summary>通常プレイ用コンパクトバナーのフェードアウト開始時刻。</summary>
        const float CompactEventBannerFadeStart = 1.2f;
        /// <summary>モーダル表示中のバナー退避位置(画面最上端。2026-07-22 Claude Code 追加)。</summary>
        const float EventBannerStackTopModal = 0f;
        /// <summary>モーダル表示中のバナー不透明率(約65%。2026-07-22 Claude Code 追加)。</summary>
        const float EventBannerModalAlpha = 0.65f;
        /// <summary>現在の縦積みがモーダル退避配置か(状態変化時のみ再レイアウトする)。</summary>
        bool bannersInModalLayout;

        // ---- モーダル開閉カウンタ(2026-07-22 Claude Code 追加) ----
        /// <summary>
        /// 全画面/モーダルパネルの開放数。UIManager 自身のパネル(技術/文明/指導者/スロット/設定/
        /// ガイド/終了画面/戦況グラフ)は SyncSelfModalContribution がまとめて1件として増減し、
        /// 独立Canvasのパネル(実績一覧・図鑑など)は NotifyExternalPanel で加算・減算できる。
        /// 0より大きい間、イベントバナーは画面最上端へ退避し約65%不透明で表示される
        /// (実写スクショで確認された「バナーがパネルのタイトルを覆う」重なりの修正)。
        /// </summary>
        public static int ModalOpenCount => modalOpenCount;
        static int modalOpenCount;
        /// <summary>自分のモーダル開放を modalOpenCount へ計上済みか(重複計上の防止)。</summary>
        bool selfModalCounted;

        // ---- ユニットパネル ----
        GameObject unitPanel;
        Text unitNameText;
        Text unitStatsText;
        Button foundCityButton;
        Button fortifyButton;
        Button skipButton;
        Unit selectedUnit;

        // ---- 都市パネル ----
        GameObject cityPanel;
        Text cityNameText;
        Text cityStatsText;
        RectTransform productionListRoot;
        City shownCity;
        int cityListVersion = -1;

        // ---- 技術パネル ----
        GameObject techPanel;
        Text techSubtitleText;
        RectTransform techListRoot;
        int techListVersion = -1;

        // ---- ログ ----
        Text logText;
        readonly List<string> logLines = new List<string>();
        const int MaxLogLines = 6;
        /// <summary>通常時のログ先頭Y(トップバー高さ34の下)。2026-07-22 Claude Code 変更:
        /// 二段目左端(y-38〜-64)へ国家運営チップ列を追加したため、-42 から -68 へ下げて
        /// ログ先頭行との重なりを避ける(表示位置のみの変更。行数・幅・書式は従来どおり)。</summary>
        const float LogTopNormalY = -68f;
        /// <summary>観戦時のログ先頭Y(観戦バー y-38〜-72 の下に8px余白。2026-07-21 追加)。</summary>
        const float LogTopSimulationY = -80f;

        // ---- ツールチップ ----
        GameObject tooltip;
        Text tooltipText;
        RectTransform tooltipRect;
        bool tooltipVisible;

        // ---- ゲームオーバー ----
        GameObject gameOverOverlay;
        Text gameOverText;

        // ---- 勝利画面の最終スコア一覧+紙吹雪(2026-07-21 Claude Code 追加) ----
        /// <summary>終了画面の最終スコア一覧(全文明の行)の親。行は ShowGameOver 時に再構築する。</summary>
        RectTransform gameOverStatsRoot;

        /// <summary>紙吹雪1枚分の状態(プール式。GOは gameOverOverlay の子として再利用する)。</summary>
        class ConfettiPiece
        {
            public RectTransform Rect;
            public Image Image;
            public float Delay;      // 落下開始までの遅延(秒)
            public float Duration;   // 落下時間(秒)
            public float SwayAmp;    // 横揺れ振幅(px)
            public float SwayFreq;   // 横揺れ角速度(rad/s)
            public float SwayPhase;  // 横揺れ位相
            public float RotSpeed;   // 回転速度(deg/s)
            public Color BaseColor;
        }
        /// <summary>紙吹雪プール(最大 ConfettiCount 枚。旧Canvasと共に破棄されるためInitで参照を掃除)。</summary>
        readonly List<ConfettiPiece> confettiPieces = new List<ConfettiPiece>();
        /// <summary>紙吹雪アニメーション進行中か(UpdateConfetti が非スケール時間で進める)。</summary>
        bool confettiActive;
        /// <summary>紙吹雪の開始時刻(Time.unscaledTime)。</summary>
        float confettiStartAt;
        const int ConfettiCount = 40;

        // ---- はじめてガイド ----
        GameObject tutorialPanel;
        Text tutorialPageText;
        Text tutorialTitleText;
        Text tutorialBodyText;
        Button tutorialPrevButton;
        Button tutorialNextButton;
        Text tutorialNextLabel;
        Button helpButton;
        int tutorialPage;

        /// <summary>
        /// はじめてガイドを一度でも閉じたかを記録する PlayerPrefs キー(2026-07-20 Claude Code 追加)。
        /// 0=未閲覧(初回起動時に自動表示する)、1=閲覧済み(ロード・リスタート・文明変更などの
        /// 再Init では自動表示しない)。「？ 遊び方」ボタンからの手動表示は常に可能。
        /// </summary>
        const string TutorialSeenKey = "HexCiv.TutorialSeen";

        // ---- チュートリアル実操作連動(2026-07-20 Claude Code 追加) ----
        bool tutorialAutoAdvancePending;   // 「✓ できました！」表示中(自動進行待ち)
        float tutorialAutoAdvanceAt;       // 自動進行する時刻(Time.unscaledTime)
        const float TutorialAutoAdvanceDelay = 1.0f;
        const string TutorialDonePrefix = "<color=#8ce68c>✓ できました！</color>\n";

        // ================= 初期化 =================

        /// <summary>Canvas(1280x720スケール)+EventSystemと全パネルを構築する。</summary>
        public void Init(GameState state, GameActions actions)
        {
            if (this.state != null) this.state.OnLog -= AddLog;

            this.state = state;
            this.actions = actions;
            if (state != null) state.OnLog += AddLog;

            // 再Init(リスタート)時は作り直す
            if (canvas != null) Destroy(canvas.gameObject);

            selectedUnit = null;
            shownCity = null;
            civilizationPage = 0;
            leaderPage = 0;
            slotSaveMode = false;
            simulationModeActive = false;   // 再Init後は GameBootstrap.ApplyState が改めて設定する
            tutorialPage = 0;
            tutorialAutoAdvancePending = false;
            tooltipVisible = false;
            cityListVersion = -1;
            techListVersion = -1;
            logLines.Clear();
            eventBanners.Clear();   // バナーのGOは旧Canvasと共に破棄済み
            confettiPieces.Clear(); // 紙吹雪プールも旧Canvasと共に破棄済み(2026-07-21 追加)
            confettiActive = false;
            bannersInModalLayout = false;
            // 旧Canvasのパネルは破棄済みのため、自分の計上分を静的カウンタから外す(2026-07-22 追加)
            ReleaseSelfModalContribution();
            // 独立Canvasボタンのアイコンは付与済みでも再確認する(AddButtonIcon が冪等なので安全。
            // 2026-07-21 Claude Code 追加)
            externalButtonIconsDone = false;
            nextExternalButtonIconTryAt = 0f;

            // 国家運営の警告ラッチ(2026-07-22 追加): 新規ゲーム・リスタート・ロードでは
            // 前ゲームの発生状態とクールダウンを一切持ち越さない
            warnDeficitLatched = false;
            warnStabilityLatched = false;
            warnIsolationLatched = false;
            lastDeficitWarnTurn = -999;
            lastStabilityWarnTurn = -999;
            lastIsolationWarnTurn = -999;
            nextAdminChipAt = 0f;

            BuildCanvas();
            BuildTopBar();
            BuildAudioControls();
            BuildLog();
            BuildUnitPanel();
            BuildCityPanel();
            BuildTechPanel();
            BuildCivilizationPanel();
            BuildLeaderPanel();
            BuildEndTurnButton();
            BuildSaveLoadButtons();
            BuildSaveSlotPanel();
            BuildGameSettingsPanel();
            BuildSimulationBar();
            BuildScoreGraph();
            BuildEraIndicator();
            BuildLeaderChip();
            BuildAdministrationChips();
            BuildControlHint();
            BuildTutorial();
            BuildTooltip();
            BuildGameOverOverlay();

            RefreshAll();

            // はじめてガイドの自動表示は「一度も閉じたことがない」場合のみ(初回起動時)。
            // ロード・リスタート・文明変更・設定変更による再Init では自動で再表示しない。
            // 「？ 遊び方」ボタンからはいつでも手動で開ける(2026-07-20 Claude Code 変更)。
            if (PlayerPrefs.GetInt(TutorialSeenKey, 0) == 0)
                ShowTutorial(0);
        }

        void OnDestroy()
        {
            if (state != null) state.OnLog -= AddLog;
            ReleaseSelfModalContribution();   // 静的カウンタへ計上済み分を戻す(2026-07-22 追加)
            // サムネイルテクスチャは HideAndDontSave のため明示的に破棄する(2026-07-21 追加)
            for (int i = 0; i < slotThumbTextures.Length; i++)
            {
                if (slotThumbTextures[i] != null) Destroy(slotThumbTextures[i]);
                slotThumbTextures[i] = null;
            }
        }

        void Update()
        {
            if (tooltipVisible) PositionTooltip();
            UpdateTutorialAutoAdvance();
            SyncSelfModalContribution();      // モーダル開閉→バナー退避判定(2026-07-22 追加)
            UpdateEventBanners();
            UpdateConfetti();                 // 勝利画面の紙吹雪(非表示時は即return。2026-07-21 追加)
            DecorateExternalPanelButtons();   // 左下ボタンのアイコン付与(完了後は即return。2026-07-21 追加)
            UpdateLeaderChip();               // 首位文明チップ(最大毎秒1回。2026-07-22 追加)
            UpdateAdministrationChips();      // 国庫・安定度チップ+警告(最大毎秒1回。2026-07-22 追加)
        }

        void BuildCanvas()
        {
            var cgo = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = (RectTransform)cgo.transform;

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }
        }

        void BuildTopBar()
        {
            topBar = UIStyle.CreatePanel(canvas.transform, "TopBar", UIStyle.PanelBg);
            UIStyle.SetRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 34f));

            turnText = UIStyle.CreateText(topBar.transform, "TurnText", "", 16, TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(turnText.gameObject, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(160f, 0f));

            scienceText = UIStyle.CreateText(topBar.transform, "ScienceText", "", 16, TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(scienceText.gameObject, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(172f, 0f), new Vector2(110f, 0f));

            researchButton = UIStyle.CreateButton(topBar.transform, "ResearchButton", "研究を選択", 14, OnResearchButtonClicked);
            UIStyle.SetRect(researchButton.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(288f, 0f), new Vector2(300f, 26f));
            researchButtonLabel = UIStyle.ButtonLabel(researchButton);
            // 研究=フラスコアイコン(2026-07-21 Claude Code 追加。既存余白内・サイズ/ハンドラ不変)
            AddTopBarIcon(researchButton, "flask", 18f, 5f);

            var sgo = new GameObject("CivSwatch", typeof(RectTransform), typeof(Image));
            sgo.transform.SetParent(topBar.transform, false);
            civSwatch = sgo.GetComponent<Image>();
            civSwatch.raycastTarget = false;
            UIStyle.SetRect(sgo, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(20f, 20f));

            civNameText = UIStyle.CreateText(topBar.transform, "CivNameText", "", 14, TextAnchor.MiddleRight, UIStyle.TextMain);
            civNameText.resizeTextForBestFit = true;
            civNameText.resizeTextMinSize = 10;
            civNameText.resizeTextMaxSize = 14;
            UIStyle.SetRect(civNameText.gameObject, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), new Vector2(-36f, 0f), new Vector2(220f, 0f));
        }

        void BuildAudioControls()
        {
            audioPanel = UIStyle.CreatePanel(canvas.transform, "AudioPanel", UIStyle.PanelBg);
            UIStyle.SetRect(audioPanel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-10f, -42f), new Vector2(326f, 36f));

            var music = UIStyle.CreateButton(audioPanel.transform, "MusicVolume", "BGM", 13, CycleMusicVolume);
            UIStyle.SetRect(music.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(100f, 28f));
            musicVolumeLabel = UIStyle.ButtonLabel(music);

            var sfx = UIStyle.CreateButton(audioPanel.transform, "SfxVolume", "SE", 13, CycleSfxVolume);
            UIStyle.SetRect(sfx.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(108f, 0f), new Vector2(100f, 28f));
            sfxVolumeLabel = UIStyle.ButtonLabel(sfx);

            var mute = UIStyle.CreateButton(audioPanel.transform, "Mute", "音：ON", 13, ToggleAudioMute);
            UIStyle.SetRect(mute.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(212f, 0f), new Vector2(110f, 28f));
            muteLabel = UIStyle.ButtonLabel(mute);

            RefreshAudioControls();
        }

        void BuildLog()
        {
            // Y位置はモードで切り替える(通常=トップバー直下、観戦=観戦バーの下。
            // 構築直後は通常位置で、GameBootstrap が Init 後に呼ぶ SetSimulationMode →
            // UpdateLogPosition が現在モードへ合わせる。2026-07-21 Claude Code 変更)
            logText = UIStyle.CreateText(canvas.transform, "LogText", "", 14, TextAnchor.UpperLeft, UIStyle.TextMain);
            // 幅を画面幅の約38%へアンカーで拘束し、長い行は折り返す(2026-07-21 Claude Code 変更。
            // 従来は固定520px+はみ出し許可のため、長いログ行が中央のイベントバナー領域まで
            // 伸びて重なっていた。フォントサイズ14・保持行数6は従来どおり)
            logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(logText.gameObject, new Vector2(0f, 1f), new Vector2(0.38f, 1f),
                new Vector2(0f, 1f), new Vector2(12f, LogTopNormalY), new Vector2(-20f, 130f));
            var shadow = logText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        void BuildUnitPanel()
        {
            unitPanel = UIStyle.CreatePanel(canvas.transform, "UnitPanel", UIStyle.PanelBg);
            UIStyle.SetRect(unitPanel, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(270f, 158f));

            unitNameText = UIStyle.CreateText(unitPanel.transform, "UnitNameText", "", 17, TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(unitNameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-20f, 22f));

            unitStatsText = UIStyle.CreateText(unitPanel.transform, "UnitStatsText", "", 14, TextAnchor.UpperLeft, UIStyle.TextMain);
            UIStyle.SetRect(unitStatsText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(-20f, 76f));

            foundCityButton = UIStyle.CreateButton(unitPanel.transform, "FoundCityButton", "都市建設", 13, OnFoundCityClicked);
            UIStyle.SetRect(foundCityButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(10f, 8f), new Vector2(80f, 30f));

            fortifyButton = UIStyle.CreateButton(unitPanel.transform, "FortifyButton", "防御態勢", 13, OnFortifyClicked);
            UIStyle.SetRect(fortifyButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(95f, 8f), new Vector2(80f, 30f));

            skipButton = UIStyle.CreateButton(unitPanel.transform, "SkipButton", "待機", 13, OnSkipClicked);
            UIStyle.SetRect(skipButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(180f, 8f), new Vector2(80f, 30f));

            unitPanel.SetActive(false);
        }

        void BuildCityPanel()
        {
            cityPanel = UIStyle.CreatePanel(canvas.transform, "CityPanel", UIStyle.PanelBg);
            UIStyle.SetRect(cityPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(300f, 560f));

            cityNameText = UIStyle.CreateText(cityPanel.transform, "CityNameText", "", 18, TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(cityNameText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(-8f, -8f), new Vector2(-40f, 24f));

            var closeBtn = UIStyle.CreateButton(cityPanel.transform, "CloseButton", "×", 16, CloseCityPanel);
            UIStyle.SetRect(closeBtn.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-4f, -4f), new Vector2(26f, 26f));

            cityStatsText = UIStyle.CreateText(cityPanel.transform, "CityStatsText", "", 14, TextAnchor.UpperLeft, UIStyle.TextMain);
            UIStyle.SetRect(cityStatsText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-24f, 120f));

            var header = UIStyle.CreateText(cityPanel.transform, "ProdHeader", "生産項目", 14, TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(header.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -166f), new Vector2(-24f, 20f));

            var listGo = UIStyle.CreateContainer(cityPanel.transform, "ProductionList");
            productionListRoot = UIStyle.SetRect(listGo, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(-20f, 360f));

            cityPanel.SetActive(false);
        }

        void BuildTechPanel()
        {
            techPanel = UIStyle.CreatePanel(canvas.transform, "TechPanel", UIStyle.PanelBgLight);
            UIStyle.SetRect(techPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 440f));

            var title = UIStyle.CreateText(techPanel.transform, "Title", "研究する技術を選択", 18, TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-30f, 28f));

            techSubtitleText = UIStyle.CreateText(techPanel.transform, "Subtitle", "", 13, TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(techSubtitleText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-30f, 20f));

            var closeBtn = UIStyle.CreateButton(techPanel.transform, "CloseButton", "×", 16, CloseTechPanel);
            UIStyle.SetRect(closeBtn.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-4f, -4f), new Vector2(26f, 26f));

            var listGo = UIStyle.CreateContainer(techPanel.transform, "TechList");
            techListRoot = UIStyle.SetRect(listGo, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(-32f, 360f));

            techPanel.SetActive(false);
        }

        void BuildEndTurnButton()
        {
            endTurnButton = UIStyle.CreateButton(canvas.transform, "EndTurnButton", "ターン終了", 20, OnEndTurnClicked);
            UIStyle.SetRect(endTurnButton.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-14f, 14f), new Vector2(160f, 46f));

            // 次のユニット(2026-07-20 Claude Code 追加):未行動ユニットを巡回選択する。
            // 右下ボタン列の一段上・「？ 遊び方」の左上に配置(都市パネル x970〜1270 と重ならない位置)。
            nextUnitButton = UIStyle.CreateButton(canvas.transform, "NextUnitButton", "次のユニット (Space)", 14, OnNextUnitClicked);
            UIStyle.SetRect(nextUnitButton.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-314f, 66f), new Vector2(170f, 40f));
        }

        /// <summary>
        /// セーブ/ロードボタン。トップバー内の研究ボタン(〜x588)と文明名(x1024〜)の間の
        /// 空きに配置する(右上のBGM/SEコントロール・右側の都市パネルとは重ならない)。
        /// F5/F9キーは InputController 側で処理する。
        /// </summary>
        void BuildSaveLoadButtons()
        {
            saveButton = UIStyle.CreateButton(topBar.transform, "SaveButton", "セーブ", 13, OnSaveClicked);
            UIStyle.SetRect(saveButton.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(604f, 0f), new Vector2(78f, 26f));

            loadButton = UIStyle.CreateButton(topBar.transform, "LoadButton", "ロード", 13, OnLoadClicked);
            UIStyle.SetRect(loadButton.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(690f, 0f), new Vector2(78f, 26f));

            // ボタンアイコン(2026-07-21 Claude Code 追加): ラベル左に小さな絵記号を添える。
            // ボタン幅78のうちアイコン領域23px+ラベル領域51px ≥ 3文字x13pt なので文字は縮めない
            UIStyle.AddButtonIcon(saveButton, "save", 18f, 5f);
            UIStyle.AddButtonIcon(loadButton, "load", 18f, 5f);

            civilizationButton = UIStyle.CreateButton(topBar.transform, "CivilizationButton", "文明変更", 13,
                OnCivilizationButtonClicked);
            UIStyle.SetRect(civilizationButton.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(776f, 0f), new Vector2(112f, 26f));

            leaderButton = UIStyle.CreateButton(topBar.transform, "LeaderButton", "指導者変更", 13,
                OnLeaderButtonClicked);
            UIStyle.SetRect(leaderButton.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(896f, 0f), new Vector2(120f, 26f));

            // トップバーのボタンアイコン(2026-07-21 Claude Code 追加): 文明変更=地球、指導者変更=王冠
            AddTopBarIcon(civilizationButton, "globe", 18f, 5f);
            AddTopBarIcon(leaderButton, "crown", 18f, 5f);
        }

        /// <summary>
        /// セーブスロット選択パネル(2026-07-20 Claude Code 追加)。中央表示・3行+閉じる×。
        /// セーブモードでは全スロット選択可(上書き)、ロードモードではデータのある行のみ有効。
        /// 行のメタデータ(ターン・文明・保存日時)は開くたびに SaveLoad.TryReadMeta で更新する。
        /// 2026-07-21 Claude Code 変更: 各行の左端に 96×56 のマップサムネイルを追加。
        /// 行を 52→64px へ広げ、パネルを 480×244→560×280 へ拡大した(ボタン・ハンドラは不変)。
        /// </summary>
        void BuildSaveSlotPanel()
        {
            slotPanel = UIStyle.CreatePanel(canvas.transform, "SaveSlotPanel",
                new Color(0.07f, 0.09f, 0.13f, 0.98f));
            UIStyle.SetRect(slotPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 280f));

            slotTitleText = UIStyle.CreateText(slotPanel.transform, "Title", "", 18,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(slotTitleText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-80f, 28f));

            var close = UIStyle.CreateButton(slotPanel.transform, "CloseButton", "×", 16,
                CloseSaveSlotPanel);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(28f, 28f));

            for (int i = 0; i < SaveSlotCount; i++)
            {
                int slot = i + 1;   // ラムダ用にローカルへ束縛
                var b = UIStyle.CreateButton(slotPanel.transform, "Slot" + slot, "", 15,
                    () => OnSlotClicked(slot));
                UIStyle.SetRect(b.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -48f - i * 74f), new Vector2(-40f, 64f));
                var label = UIStyle.ButtonLabel(b);
                if (label != null)
                {
                    label.alignment = TextAnchor.MiddleLeft;
                    // サムネイル(x10〜106)の右からテキストを開始する(2026-07-21 変更。
                    // 空きスロットでも同じ位置に揃え、行ごとの文字位置ずれを避ける)
                    ((RectTransform)label.transform).offsetMin = new Vector2(116f, 1f);
                }

                // マップサムネイル(2026-07-21 Claude Code 追加): ボタンの子の RawImage。
                // raycastTarget=false のためクリックは従来どおりボタン本体が受ける。
                // テクスチャは RefreshSaveSlotList → UpdateSlotThumbnail が割り当てる。
                var tgo = new GameObject("Thumbnail", typeof(RectTransform), typeof(RawImage));
                tgo.transform.SetParent(b.transform, false);
                var thumb = tgo.GetComponent<RawImage>();
                thumb.raycastTarget = false;
                thumb.color = Color.white;
                thumb.enabled = false;
                UIStyle.SetRect(tgo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(10f, 0f),
                    new Vector2(SlotThumbWidth, SlotThumbHeight));
                slotThumbImages[i] = thumb;

                slotButtons[i] = b;
                slotLabels[i] = label;
            }

            slotPanel.SetActive(false);
        }

        /// <summary>
        /// ゲーム設定画面(2026-07-20 Claude Code 追加)。マップサイズ・文明数・マップ種別・難易度・シードを選び、
        /// その設定で新しいゲームを開始する。マップサイズ・文明数・マップ種別・難易度は選択時に PlayerPrefs へ保存され、
        /// 「文明変更」「指導者変更」「もう一度プレイ」の新規ゲームにも適用される(シードは保存しない)。
        /// 開閉ボタンはトップバーが満杯(x604〜1016 使用済み)のため「文明変更」の真下の二段目に置く
        /// (左上ログ x12〜約478(38%幅)・右上サウンド x944〜1270 とは重ならない)。
        /// </summary>
        void BuildGameSettingsPanel()
        {
            settingsButton = UIStyle.CreateButton(canvas.transform, "GameSettingsButton", "ゲーム設定", 13,
                OnGameSettingsButtonClicked);
            UIStyle.SetRect(settingsButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(776f, -38f), new Vector2(112f, 26f));
            // ゲーム設定=歯車アイコン(2026-07-21 Claude Code 追加)
            AddTopBarIcon(settingsButton, "gear", 18f, 5f);

            // 高さは「シミュレーション観戦で開始」ボタン追加分(+40)と
            // 「マップ種別」行(+76)・「難易度」行(+76)の追加分を含む(2026-07-20 Claude Code 変更)。
            // さらに「画面表示(フルスクリーン)」行の追加分(+60)を含む(2026-07-21 Claude Code 変更)。
            // 「演出」行の追加で 664→700(基準解像度720の上下に10px余白。2026-07-22 Claude Code 変更)
            settingsPanel = UIStyle.CreatePanel(canvas.transform, "GameSettingsPanel",
                new Color(0.07f, 0.09f, 0.13f, 0.98f));
            UIStyle.SetRect(settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 700f));

            var title = UIStyle.CreateText(settingsPanel.transform, "Title", "ゲーム設定", 22,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-80f, 32f));

            var subtitle = UIStyle.CreateText(settingsPanel.transform, "Subtitle",
                "新しいゲームの生成条件(文明・指導者はトップバーの各ボタンで選択)", 13,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(subtitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(-40f, 22f));

            var close = UIStyle.CreateButton(settingsPanel.transform, "CloseButton", "×", 18,
                CloseGameSettingsPanel);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(34f, 34f));

            var sizeHeader = UIStyle.CreateText(settingsPanel.transform, "MapSizeHeader",
                "マップサイズ", 15, TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(sizeHeader.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -82f), new Vector2(240f, 22f));

            for (int i = 0; i < mapSizeButtons.Length; i++)
            {
                int index = i;   // ラムダ用にローカルへ束縛
                var b = UIStyle.CreateButton(settingsPanel.transform, "MapSize" + i,
                    MapSizeLabelsJa[i], 14, () => OnMapSizeClicked(index));
                UIStyle.SetRect(b.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(24f + i * 172f, -108f), new Vector2(164f, 34f));
                mapSizeButtons[i] = b;
            }

            var playersHeader = UIStyle.CreateText(settingsPanel.transform, "NumPlayersHeader",
                "文明数(自分を含む)", 15, TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(playersHeader.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -158f), new Vector2(300f, 22f));

            for (int i = 0; i < numPlayersButtons.Length; i++)
            {
                int index = i;   // ラムダ用にローカルへ束縛
                var b = UIStyle.CreateButton(settingsPanel.transform, "NumPlayers" + NumPlayersChoices[i],
                    NumPlayersChoices[i].ToString(), 15, () => OnNumPlayersClicked(index));
                UIStyle.SetRect(b.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(24f + i * 100f, -184f), new Vector2(92f, 34f));
                numPlayersButtons[i] = b;
            }

            // マップ種別(大陸/パンゲア/群島)の選択行(2026-07-20 Claude Code 追加)
            var typeHeader = UIStyle.CreateText(settingsPanel.transform, "MapTypeHeader",
                "マップ種別", 15, TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(typeHeader.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -234f), new Vector2(240f, 22f));

            for (int i = 0; i < mapTypeButtons.Length; i++)
            {
                int index = i;   // ラムダ用にローカルへ束縛
                var b = UIStyle.CreateButton(settingsPanel.transform, "MapType" + i,
                    MapTypeLabelsJa[i], 14, () => OnMapTypeClicked(index));
                UIStyle.SetRect(b.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(24f + i * 172f, -260f), new Vector2(164f, 34f));
                mapTypeButtons[i] = b;
            }

            // 難易度(やさしい/普通/むずかしい)の選択行(2026-07-20 Claude Code 追加)。
            // 普通(既定)はAI補正なしで従来と同一挙動(DifficultyRules 参照)
            var difficultyHeader = UIStyle.CreateText(settingsPanel.transform, "DifficultyHeader",
                "難易度", 15, TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(difficultyHeader.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -310f), new Vector2(240f, 22f));

            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                int index = i;   // ラムダ用にローカルへ束縛
                var b = UIStyle.CreateButton(settingsPanel.transform, "Difficulty" + i,
                    DifficultyLabelsJa[i], 14, () => OnDifficultyClicked(index));
                UIStyle.SetRect(b.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(24f + i * 172f, -336f), new Vector2(164f, 34f));
                difficultyButtons[i] = b;
            }

            var seedHeader = UIStyle.CreateText(settingsPanel.transform, "SeedHeader",
                "シード値", 15, TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(seedHeader.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -386f), new Vector2(240f, 22f));

            seedInput = CreateSeedInput(settingsPanel.transform);
            UIStyle.SetRect(seedInput.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -412f), new Vector2(200f, 34f));

            var seedNote = UIStyle.CreateText(settingsPanel.transform, "SeedNote",
                "空欄 = ランダム(同じシードは同じマップ)", 13, TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(seedNote.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(236f, -418f), new Vector2(300f, 22f));

            // 画面表示(フルスクリーン)行(2026-07-21 Claude Code 追加)。実切替・保存は
            // GameBootstrap(OnFullscreenToggled)が行い、ラベルは SetFullscreenState で更新される
            var displayHeader = UIStyle.CreateText(settingsPanel.transform, "DisplayHeader",
                "画面表示", 15, TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(displayHeader.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -462f), new Vector2(240f, 22f));

            fullscreenButton = UIStyle.CreateButton(settingsPanel.transform, "FullscreenToggle",
                "フルスクリーン: OFF", 14, () => OnFullscreenToggled?.Invoke());
            UIStyle.SetRect(fullscreenButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -488f), new Vector2(220f, 34f));
            fullscreenLabel = UIStyle.ButtonLabel(fullscreenButton);

            var fullscreenNote = UIStyle.CreateText(settingsPanel.transform, "FullscreenNote",
                "F11キーでも切り替えできます", 13, TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(fullscreenNote.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(256f, -494f), new Vector2(280f, 22f));

            SetFullscreenState(fullscreenOn);   // 再Init(リスタート等)後もラベルを現在状態へ復元

            // 演出モード(標準/軽量)行(2026-07-22 Claude Code 追加)。値は PlayerPrefs "HexCiv.FxLight" に
            // 保存し、各レンダラーが Rendering/VisualQuality 経由で参照する(表示のみ・シミュレーション不変)
            fxQualityButton = UIStyle.CreateButton(settingsPanel.transform, "FxQualityToggle",
                "演出: 標準", 14, OnFxQualityClicked);
            UIStyle.SetRect(fxQualityButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(24f, -530f), new Vector2(220f, 34f));
            fxQualityLabel = UIStyle.ButtonLabel(fxQualityButton);

            var fxNote = UIStyle.CreateText(settingsPanel.transform, "FxQualityNote",
                "軽量: 雲影・水面・待機揺れ・ダメージ数字を省略", 13, TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.SetRect(fxNote.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(256f, -536f), new Vector2(296f, 22f));

            RefreshFxQualityLabel();   // 再Init後もラベルを保存値へ復元

            var start = UIStyle.CreateButton(settingsPanel.transform, "StartButton",
                "この設定で新しいゲームを開始(現在のゲームは破棄)", 15, OnStartWithSettingsClicked);
            UIStyle.SetRect(start.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(500f, 46f));

            // シミュレーション観戦(全文明AI・自動進行)での開始(2026-07-20 Claude Code 追加)。
            // 通常開始ボタンの一段上に置く(シード行 -446 との間に余白あり)
            var simStart = UIStyle.CreateButton(settingsPanel.transform, "SimulationStartButton",
                "シミュレーション観戦で開始(全文明AI・自動進行)", 14, OnStartSimulationClicked);
            UIStyle.SetRect(simStart.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(500f, 40f));

            settingsPanel.SetActive(false);
        }

        /// <summary>
        /// シード入力欄(レガシー uGUI InputField・整数のみ・プレースホルダ「ランダム」)。
        /// characterLimit=9 なので入力が数値なら int.TryParse は必ず成功する(最大999999999)。
        /// フォント は UIStyle.CreateText 経由で JapaneseFont() を使用する。
        /// </summary>
        InputField CreateSeedInput(Transform parent)
        {
            var go = new GameObject("SeedInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.15f, 0.19f, 0.26f, 1f);

            var input = go.GetComponent<InputField>();

            var placeholder = UIStyle.CreateText(go.transform, "Placeholder", "ランダム", 15,
                TextAnchor.MiddleLeft, UIStyle.TextDim);
            UIStyle.StretchFull(placeholder.gameObject, 8f);

            var text = UIStyle.CreateText(go.transform, "Text", "", 15,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            text.supportRichText = false;
            UIStyle.StretchFull(text.gameObject, 8f);

            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 9;
            return input;
        }

        /// <summary>
        /// シミュレーション観戦中の操作バー(2026-07-20 Claude Code 追加)。
        /// トップバー直下の中央(x340〜940)に置く:二段目の「ゲーム設定」ボタン(x776〜888)と
        /// 重なるため観戦中は同ボタンを隠し(SetSimulationMode)、右上サウンド(x944〜)とは重ねない。
        /// 一時停止/再開・速度切替(等速〜256倍速の9段階)・現在ターン・観戦終了ボタンを持つ。
        /// 観戦中のみ表示(SetSimulationMode で切替)。
        /// </summary>
        void BuildSimulationBar()
        {
            simulationBar = UIStyle.CreatePanel(canvas.transform, "SimulationBar", UIStyle.PanelBg);
            UIStyle.SetRect(simulationBar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(600f, 34f));

            var pause = UIStyle.CreateButton(simulationBar.transform, "PauseButton", "⏸ 一時停止", 13,
                () => OnSimulationPauseToggled?.Invoke());
            UIStyle.SetRect(pause.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(104f, 26f));
            simulationPauseLabel = UIStyle.ButtonLabel(pause);

            var speed = UIStyle.CreateButton(simulationBar.transform, "SpeedButton", "速度: 2倍速", 13,
                () => OnSimulationSpeedCycled?.Invoke());
            UIStyle.SetRect(speed.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(116f, 0f), new Vector2(120f, 26f));
            simulationSpeedLabel = UIStyle.ButtonLabel(speed);

            simulationTurnText = UIStyle.CreateText(simulationBar.transform, "TurnText", "", 13,
                TextAnchor.MiddleCenter, UIStyle.TextMain);
            UIStyle.SetRect(simulationTurnText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(242f, 0f), new Vector2(100f, 26f));

            // 戦況グラフの開閉(2026-07-20 Claude Code 追加。バー幅600に収めるため
            // ターン表示を 130→100 に、終了ボタンを 216→182 に詰めた)
            var graphBtn = UIStyle.CreateButton(simulationBar.transform, "ScoreGraphButton", "戦況", 13,
                ToggleScoreGraph);
            UIStyle.SetRect(graphBtn.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(346f, 0f), new Vector2(62f, 26f));
            // 戦況=折れ線グラフアイコン(通常プレイ側の「戦況」ボタンと同じ。2026-07-21 追加)
            AddTopBarIcon(graphBtn, "graph", 16f, 4f);

            var exit = UIStyle.CreateButton(simulationBar.transform, "ExitButton",
                "観戦を終了して新規ゲーム", 13, () => OnSimulationExit?.Invoke());
            UIStyle.SetRect(exit.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(412f, 0f), new Vector2(182f, 26f));

            simulationBar.SetActive(false);
        }

        /// <summary>
        /// 戦況グラフ(2026-07-20 Claude Code 追加)。パネル本体は ScoreGraphPanel が構築する
        /// (Canvas の子なので新規ゲームの再Init時に履歴ごと破棄・再生成される)。
        /// 通常プレイの開閉ボタンは二段目(「ゲーム設定」x776 の左隣 x704〜768)。
        /// 観戦中はこのボタンを隠し(SetSimulationMode)、観戦バー内の「戦況」ボタンを使う。
        /// </summary>
        void BuildScoreGraph()
        {
            var go = new GameObject("ScoreGraphPanel", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            scoreGraphPanel = go.AddComponent<ScoreGraphPanel>();
            scoreGraphPanel.Init(state);

            scoreGraphButton = UIStyle.CreateButton(canvas.transform, "ScoreGraphButton", "戦況", 13,
                ToggleScoreGraph);
            UIStyle.SetRect(scoreGraphButton.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(704f, -38f), new Vector2(64f, 26f));
            // 戦況=折れ線グラフアイコン(2026-07-21 Claude Code 追加)
            AddTopBarIcon(scoreGraphButton, "graph", 16f, 4f);
        }

        /// <summary>「戦況」ボタン(通常プレイの二段目/観戦バー)から呼ばれる開閉トグル。</summary>
        void ToggleScoreGraph()
        {
            if (scoreGraphPanel != null) scoreGraphPanel.Toggle();
        }

        // ================= 時代表示(2026-07-22 Claude Code 追加) =================

        /// <summary>
        /// 時代インジケーター。トップバー二段目の空き(通常: ログ約38%幅の右 x614〜700、
        /// 観戦: 観戦バー x340〜940 を避けた左端 x12〜98)に、現在の時代
        /// (ターン1〜100=古代/101〜180=中世/181〜=近代 — GameAudio のBGM3楽章と同じ閾値)を
        /// 小アイコン+ラベルで表示する。表示専用(raycast無効)でクリックを一切遮らず、
        /// シミュレーションには影響しない。更新はターン変化時(RefreshTopBar 経由)のみ。
        /// </summary>
        void BuildEraIndicator()
        {
            eraIndicator = UIStyle.CreateContainer(canvas.transform, "EraIndicator");
            UIStyle.SetRect(eraIndicator, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(EraIndicatorNormalX, -38f), new Vector2(86f, 26f));

            var igo = new GameObject("EraIcon", typeof(RectTransform), typeof(Image));
            igo.transform.SetParent(eraIndicator.transform, false);
            eraIconImage = igo.GetComponent<Image>();
            eraIconImage.raycastTarget = false;
            eraIconImage.preserveAspect = true;
            UIStyle.SetRect(igo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(18f, 18f));

            eraLabelText = UIStyle.CreateText(eraIndicator.transform, "EraLabel", "", 13,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(eraLabelText.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(-22f, 0f));
            // マップ上に直接乗るため影で可読性を確保(ログ・操作ヒントと同じ流儀)
            var shadow = eraLabelText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1f);

            eraShownIndex = -1;
            UpdateEraIndicatorPosition();
            RefreshEraIndicator();
        }

        /// <summary>現在ターンから時代(0=古代/1=中世/2=近代)を求め、変化した時だけ表示を更新する。</summary>
        void RefreshEraIndicator()
        {
            if (eraLabelText == null || eraIconImage == null || state == null) return;
            int era = state.TurnNumber <= 100 ? 0 : (state.TurnNumber <= 180 ? 1 : 2);
            if (era == eraShownIndex) return;
            int prev = eraShownIndex;
            eraShownIndex = era;
            eraLabelText.text = EraNamesJa[era];
            eraLabelText.color = EraTints[era];
            eraIconImage.sprite = EraIconSprite(era);
            eraIconImage.color = EraTints[era];

            // 実プレイ中の実際の時代遷移のみ告知する(2026-07-22 Claude Code 追加)。
            // prev < 0 は Init/ロード直後の初回設定 → 告知せず基準として静かに表示するだけ。
            // era > prev の前進遷移かつゲーム進行中(未終了)の時だけ告知バナー+鐘を出す。
            if (prev >= 0 && era > prev && !state.IsGameOver)
                AnnounceEraChange(era);
        }

        /// <summary>
        /// 時代前進の告知(2026-07-22 Claude Code 追加)。中世(1)/近代(2)に入った時、
        /// 時代アクセント色のイベントバナー(観戦=大/通常=コンパクト)を出し、GameAudio の
        /// 時代の鐘を鳴らす。古代(0)は開始時代のため告知しない。表示専用で
        /// シミュレーションには一切影響しない。
        /// </summary>
        void AnnounceEraChange(int era)
        {
            if (era < 1 || era >= EraNamesJa.Length) return;
            string msg = "⏳ " + EraNamesJa[era] + "に入った";
            Color accent = EraTints[era];
            if (simulationModeActive) ShowEventBanner(msg, accent);
            else ShowEventBannerCompact(msg, accent);
            // Codex が本ラウンドで追加する時代の鐘。GameAudio 不在(ヘッドレス等)は ?. で null 安全。
            GameAudio.Instance?.PlayEraBell();
        }

        /// <summary>時代表示の位置を現在モードへ合わせる(通常=x614/観戦=x12。UpdateLogPosition と同じ流儀)。</summary>
        void UpdateEraIndicatorPosition()
        {
            if (eraIndicator == null) return;
            ((RectTransform)eraIndicator.transform).anchoredPosition = new Vector2(
                simulationModeActive ? EraIndicatorSimulationX : EraIndicatorNormalX, -38f);
        }

        /// <summary>時代アイコン(0=神殿の柱/1=城壁/2=歯車)を白Spriteで取得する(実行中キャッシュ)。</summary>
        static Sprite EraIconSprite(int era)
        {
            era = Mathf.Clamp(era, 0, eraIconSprites.Length - 1);
            if (eraIconSprites[era] == null) eraIconSprites[era] = BuildEraIconSprite(era);
            return eraIconSprites[era];
        }

        /// <summary>
        /// 時代アイコンSpriteを白色で手続き生成する(24px。内部96pxマスクを4x4アルファ平均縮小=
        /// アンチエイリアス。BuildChipSprite と同じ方式)。白生成のため実際の色は
        /// Image.color の乗算で時代色(ブロンズ/シルバー/ゴールド)へ着色する。
        /// </summary>
        static Sprite BuildEraIconSprite(int era)
        {
            const int size = 24;
            const int ss = 4;
            const int big = size * ss;
            var buf = new float[big * big];   // アルファのみ(形状マスク)
            for (int y = 0; y < big; y++)
            {
                float v = (y + 0.5f) / big;   // y上向き(SetPixels の行0=下端)
                for (int x = 0; x < big; x++)
                {
                    float u = (x + 0.5f) / big;
                    if (EraIconMask(era, u, v)) buf[y * big + x] = 1f;
                }
            }

            var outPx = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = 0f;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        int row = (y * ss + sy) * big + x * ss;
                        for (int sx = 0; sx < ss; sx++) a += buf[row + sx];
                    }
                    outPx[y * size + x] = new Color(1f, 1f, 1f, a / (ss * ss));
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "ui_era_icon_" + era;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(outPx);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>時代アイコンの形状マスク(u,v∈0..1、y上向き)。0=神殿の柱/1=城壁/2=歯車。</summary>
        static bool EraIconMask(int era, float u, float v)
        {
            switch (era)
            {
                case 0:   // 古代: 神殿の柱(基壇+3本の柱身+笠石)
                    if (v >= 0.08f && v <= 0.20f && u >= 0.14f && u <= 0.86f) return true;
                    if (v >= 0.76f && v <= 0.88f && u >= 0.14f && u <= 0.86f) return true;
                    if (v > 0.20f && v < 0.76f)
                    {
                        if (u >= 0.20f && u <= 0.32f) return true;
                        if (u >= 0.44f && u <= 0.56f) return true;
                        if (u >= 0.68f && u <= 0.80f) return true;
                    }
                    return false;
                case 1:   // 中世: 城壁(胸壁3つ+城体から門をくり抜き)
                    if (v >= 0.10f && v <= 0.58f && u >= 0.16f && u <= 0.84f)
                        return !(u >= 0.42f && u <= 0.58f && v <= 0.36f);   // 門
                    if (v > 0.58f && v <= 0.82f)
                    {
                        if (u >= 0.16f && u <= 0.30f) return true;
                        if (u >= 0.43f && u <= 0.57f) return true;
                        if (u >= 0.70f && u <= 0.84f) return true;
                    }
                    return false;
                default:  // 近代: 歯車(リング+8枚歯。設定アイコンと同型の白マスク版)
                {
                    float dx = u - 0.5f, dy = v - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 0.14f && d <= 0.30f) return true;
                    if (d > 0.26f && d <= 0.44f)
                    {
                        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        float near = Mathf.Abs(Mathf.DeltaAngle(ang, Mathf.Round(ang / 45f) * 45f));
                        if (near <= 11f) return true;
                    }
                    return false;
                }
            }
        }

        // ================= 首位文明チップ(2026-07-22 Claude Code 追加) =================

        /// <summary>
        /// 首位文明チップ。トップバー二段目の空きに「首位: <文明名>」を色スウォッチ付きで表示する。
        /// 勝利画面と同じスコア式(Σ人口×3+都市数×8+技術数×5 = GameOverScoreOf)で首位文明を求め、
        /// 最大毎秒1回だけ再計算する(UpdateLeaderChip)。ゲーム終了時は隠す。表示専用(raycast無効)で
        /// クリックを一切遮らず、シミュレーションには影響しない。位置は時代表示と同じくモードで
        /// 切り替える(通常=x480/観戦=x104。UpdateLeaderChipPosition)。
        /// </summary>
        void BuildLeaderChip()
        {
            leaderChip = UIStyle.CreateContainer(canvas.transform, "LeaderChip");
            UIStyle.SetRect(leaderChip, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(LeaderChipNormalX, -38f),
                new Vector2(LeaderChipWidth, 26f));

            var sgo = new GameObject("LeaderSwatch", typeof(RectTransform), typeof(Image));
            sgo.transform.SetParent(leaderChip.transform, false);
            leaderChipSwatch = sgo.GetComponent<Image>();
            leaderChipSwatch.raycastTarget = false;
            UIStyle.SetRect(sgo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(14f, 14f));

            leaderChipLabel = UIStyle.CreateText(leaderChip.transform, "LeaderLabel", "", 13,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            leaderChipLabel.resizeTextForBestFit = true;
            leaderChipLabel.resizeTextMinSize = 9;
            leaderChipLabel.resizeTextMaxSize = 13;
            UIStyle.SetRect(leaderChipLabel.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
            // マップ上に直接乗るため影で可読性を確保(時代表示・ログ・操作ヒントと同じ流儀)
            var shadow = leaderChipLabel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1f);

            nextLeaderChipAt = 0f;
            UpdateLeaderChipPosition();
            leaderChip.SetActive(false);   // 初回 RefreshLeaderChip が首位を求めてから表示する
        }

        /// <summary>首位チップの位置を現在モードへ合わせる(通常=x480/観戦=x104。UpdateEraIndicatorPosition と同じ流儀)。</summary>
        void UpdateLeaderChipPosition()
        {
            if (leaderChip == null) return;
            ((RectTransform)leaderChip.transform).anchoredPosition = new Vector2(
                simulationModeActive ? LeaderChipSimulationX : LeaderChipNormalX, -38f);
        }

        /// <summary>首位文明チップを最大毎秒1回だけ再計算する(Update から。非スケール時間基準)。</summary>
        void UpdateLeaderChip()
        {
            if (leaderChip == null || state == null) return;
            if (Time.unscaledTime < nextLeaderChipAt) return;
            nextLeaderChipAt = Time.unscaledTime + 1f;
            RefreshLeaderChip();
        }

        /// <summary>
        /// 首位文明(生存文明のうち GameOverScoreOf 最大、同点は Id 昇順)を求めてチップへ反映する。
        /// ゲーム終了時・首位不在時は隠す。表示専用でシミュレーションには影響しない。
        /// </summary>
        void RefreshLeaderChip()
        {
            if (leaderChip == null || state == null) return;
            if (state.IsGameOver)
            {
                if (leaderChip.activeSelf) leaderChip.SetActive(false);
                return;
            }

            Player best = null;
            int bestScore = 0;
            var players = state.Players;
            for (int i = 0; players != null && i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.IsEliminated) continue;
                int s = GameOverScoreOf(p);
                if (best == null || s > bestScore || (s == bestScore && p.Id < best.Id))
                {
                    best = p;
                    bestScore = s;
                }
            }

            if (best == null)
            {
                if (leaderChip.activeSelf) leaderChip.SetActive(false);
                return;
            }

            if (!leaderChip.activeSelf) leaderChip.SetActive(true);
            if (leaderChipSwatch != null) leaderChipSwatch.color = best.Color;
            if (leaderChipLabel != null) leaderChipLabel.text = "首位: " + best.NameJa;
        }

        // ================= 国家運営チップ+警告通知(2026-07-22 Claude Code 追加) =================

        /// <summary>
        /// 国庫・安定度チップ列を構築する。トップバー二段目の左端(x12〜約364、y-38〜-64)に置き、
        /// 首位チップ(x480〜)・時代表示(x614〜)・二段目のボタン列(x704〜888)・右上サウンド
        /// (x944〜)のいずれとも重ならない。ログ先頭行は LogTopNormalY(-68)でこの列の下から
        /// 始まる。表示専用(raycast無効)でクリックを一切遮らず、値は Core の公開APIを読むだけ。
        /// </summary>
        void BuildAdministrationChips()
        {
            adminChips = UIStyle.CreateContainer(canvas.transform, "AdministrationChips");
            UIStyle.SetRect(adminChips, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(12f, -38f), new Vector2(380f, 26f));

            treasuryChipLabel = UIStyle.CreateText(adminChips.transform, "TreasuryLabel", "", 13,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(treasuryChipLabel.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(206f, 22f));
            AddChipShadow(treasuryChipLabel);

            var swatchGo = new GameObject("StabilitySwatch", typeof(RectTransform), typeof(Image));
            swatchGo.transform.SetParent(adminChips.transform, false);
            stabilityChipSwatch = swatchGo.GetComponent<Image>();
            stabilityChipSwatch.raycastTarget = false;
            UIStyle.SetRect(swatchGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(212f, 0f), new Vector2(14f, 14f));

            stabilityChipLabel = UIStyle.CreateText(adminChips.transform, "StabilityLabel", "", 13,
                TextAnchor.MiddleLeft, UIStyle.TextMain);
            UIStyle.SetRect(stabilityChipLabel.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(232f, 0f), new Vector2(130f, 22f));
            AddChipShadow(stabilityChipLabel);

            nextAdminChipAt = 0f;
            adminChips.SetActive(false);   // 初回 RefreshAdministrationChips が可否を判定してから表示する
        }

        /// <summary>マップ上に直接乗るチップ文字の可読性確保(ログ・時代表示・首位チップと同じ流儀)。</summary>
        static void AddChipShadow(Text text)
        {
            if (text == null) return;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        /// <summary>
        /// 国家運営チップと警告判定を最大毎秒1回だけ実行する(Update から。非スケール時間基準)。
        /// 観戦モード・ゲーム終了時はチップを隠し、警告判定も行わない。
        /// </summary>
        void UpdateAdministrationChips()
        {
            if (adminChips == null || state == null) return;
            if (Time.unscaledTime < nextAdminChipAt) return;
            nextAdminChipAt = Time.unscaledTime + 1f;
            RefreshAdministrationChips();
        }

        /// <summary>
        /// 人間文明の国庫・収支・安定度をチップへ反映し、続けて警告条件を判定する。
        /// 値は Core/AdministrationSystem(Balance = Revenue - Expenses)と Player の
        /// 公開フィールドを読むだけで、いかなる状態も書き換えない。
        /// </summary>
        void RefreshAdministrationChips()
        {
            if (adminChips == null || state == null) return;

            var p = state.HumanPlayer;
            bool show = p != null && !p.IsEliminated && !state.IsGameOver && !simulationModeActive;
            if (!show)
            {
                if (adminChips.activeSelf) adminChips.SetActive(false);
                return;
            }
            if (!adminChips.activeSelf) adminChips.SetActive(true);

            int balance = AdministrationSystem.Balance(p);
            if (treasuryChipLabel != null)
            {
                treasuryChipLabel.text = $"国庫 {p.Treasury:N0} ({SignedAmount(balance)})";
                treasuryChipLabel.color = balance > 0
                    ? BalancePositiveColor
                    : (balance < 0 ? UIStyle.Danger : UIStyle.TextMain);
            }

            int stability = Mathf.Clamp(p.Stability, 0, AdministrationSystem.MaximumStability);
            var stabilityColor = StabilityChipColor(stability);
            if (stabilityChipLabel != null)
            {
                stabilityChipLabel.text = $"安定度 {stability}";
                stabilityChipLabel.color = stabilityColor;
            }
            if (stabilityChipSwatch != null) stabilityChipSwatch.color = stabilityColor;

            CheckAdministrationWarnings(p);
        }

        /// <summary>収支の符号付き表記(黒字は明示的に +)。</summary>
        static string SignedAmount(int value) => value >= 0 ? "+" + value.ToString("N0") : value.ToString("N0");

        /// <summary>
        /// 安定度の色スケール(0=赤 → 中間=琥珀 → AdministrationSystem.MaximumStability=緑)。
        /// 段階的な独自閾値を設けず、シミュレーション側の上限値で正規化した連続的な補間にする。
        /// </summary>
        static Color StabilityChipColor(int stability)
        {
            float t = Mathf.Clamp01(stability / (float)AdministrationSystem.MaximumStability);
            return t < 0.5f
                ? Color.Lerp(UIStyle.Danger, StabilityMidColor, t * 2f)
                : Color.Lerp(StabilityMidColor, StabilityHighColor, (t - 0.5f) * 2f);
        }

        /// <summary>
        /// 人間文明の警告条件(赤字転落・安定度低下・補給孤立)をエッジ検出で判定し、
        /// 該当時にコンパクトバナー+警告音を出す。各警告は「条件が解消されるまで再発火しない」
        /// ラッチと「同種は WarningCooldownTurns ターン以内に再通知しない」制限の両方を持つ。
        /// 読むのは Player の公開フィールドと Unit.Supply(LogisticsSystem がターン開始時に
        /// 確定させた値)だけで、補給網の再計算も状態変更も行わない。
        /// </summary>
        void CheckAdministrationWarnings(Player p)
        {
            int turn = state.TurnNumber;

            // (a) 国庫が赤字へ転落した
            bool deficit = p.Treasury < 0;
            if (!deficit) warnDeficitLatched = false;
            else if (!warnDeficitLatched)
            {
                warnDeficitLatched = true;
                if (turn - lastDeficitWarnTurn >= WarningCooldownTurns)
                {
                    lastDeficitWarnTurn = turn;
                    ShowAdministrationWarning("⚠ 国庫が赤字になった");
                }
            }

            // (b) 安定度がシミュレーション側の低安定閾値(RecommendTaxPolicy と同一)を下回った
            bool unstable = p.Stability <= LowStabilityThreshold;
            if (!unstable) warnStabilityLatched = false;
            else if (!warnStabilityLatched)
            {
                warnStabilityLatched = true;
                if (turn - lastStabilityWarnTurn >= WarningCooldownTurns)
                {
                    lastStabilityWarnTurn = turn;
                    ShowAdministrationWarning("⚠ 国内が不安定になっている");
                }
            }

            // (c) 自軍ユニットが補給から孤立した(SupplyLevel.Isolated が0→1件以上になった時)
            bool isolated = HasIsolatedUnit(p);
            if (!isolated) warnIsolationLatched = false;
            else if (!warnIsolationLatched)
            {
                warnIsolationLatched = true;
                if (turn - lastIsolationWarnTurn >= WarningCooldownTurns)
                {
                    lastIsolationWarnTurn = turn;
                    ShowAdministrationWarning("⚠ 部隊が補給から孤立した");
                }
            }
        }

        /// <summary>孤立(SupplyLevel.Isolated)状態の生存ユニットが1体でもいるか(読み取りのみ)。</summary>
        static bool HasIsolatedUnit(Player p)
        {
            if (p == null) return false;
            for (int i = 0; i < p.Units.Count; i++)
            {
                var u = p.Units[i];
                if (u == null || u.IsDead) continue;
                if (u.Supply == SupplyLevel.Isolated) return true;
            }
            return false;
        }

        /// <summary>警告バナー(既存のコンパクトバナー機構)+警告音。GameAudio 不在でも null 安全。</summary>
        void ShowAdministrationWarning(string messageJa)
        {
            ShowEventBannerCompact(messageJa, UIStyle.Danger);
            GameAudio.Instance?.PlayWarning();
        }

        void BuildCivilizationPanel()
        {
            civilizationPanel = UIStyle.CreatePanel(canvas.transform, "CivilizationPanel",
                new Color(0.07f, 0.09f, 0.13f, 0.98f));
            UIStyle.SetRect(civilizationPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850f, 590f));

            var title = UIStyle.CreateText(civilizationPanel.transform, "Title",
                "プレイする文明を選択", 22, TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-80f, 32f));

            var subtitle = UIStyle.CreateText(civilizationPanel.transform, "Subtitle",
                "選択すると現在のゲームを終了し、その文明で新しく開始します", 14,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(subtitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -43f), new Vector2(-60f, 24f));

            var close = UIStyle.CreateButton(civilizationPanel.transform, "CloseButton", "×", 18,
                CloseCivilizationPanel);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(34f, 34f));

            var listGo = UIStyle.CreateContainer(civilizationPanel.transform, "CivilizationList");
            civilizationListRoot = UIStyle.SetRect(listGo, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(-32f, 420f));

            civilizationPrevButton = UIStyle.CreateButton(civilizationPanel.transform, "Prev", "前のページ", 14,
                () => ChangeCivilizationPage(-1));
            UIStyle.SetRect(civilizationPrevButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(18f, 16f), new Vector2(140f, 40f));

            civilizationPageText = UIStyle.CreateText(civilizationPanel.transform, "Page", "", 15,
                TextAnchor.MiddleCenter, UIStyle.TextMain);
            UIStyle.SetRect(civilizationPageText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 17f), new Vector2(320f, 36f));

            civilizationNextButton = UIStyle.CreateButton(civilizationPanel.transform, "Next", "次のページ", 14,
                () => ChangeCivilizationPage(1));
            UIStyle.SetRect(civilizationNextButton.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-18f, 16f), new Vector2(140f, 40f));

            civilizationPanel.SetActive(false);
        }

        void BuildLeaderPanel()
        {
            leaderPanel = UIStyle.CreatePanel(canvas.transform, "LeaderPanel",
                new Color(0.07f, 0.09f, 0.13f, 0.98f));
            UIStyle.SetRect(leaderPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 570f));

            leaderHeaderText = UIStyle.CreateText(leaderPanel.transform, "Title",
                "指導者を選択", 22, TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(leaderHeaderText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-80f, 32f));

            var subtitle = UIStyle.CreateText(leaderPanel.transform, "Subtitle",
                "選択すると現在のゲームを終了し、その指導者で新しく開始します", 14,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.SetRect(subtitle.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -43f), new Vector2(-60f, 24f));

            var close = UIStyle.CreateButton(leaderPanel.transform, "CloseButton", "×", 18,
                CloseLeaderPanel);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(34f, 34f));

            var listGo = UIStyle.CreateContainer(leaderPanel.transform, "LeaderList");
            leaderListRoot = UIStyle.SetRect(listGo, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(-40f, 410f));

            leaderPrevButton = UIStyle.CreateButton(leaderPanel.transform, "Prev", "前のページ", 14,
                () => ChangeLeaderPage(-1));
            UIStyle.SetRect(leaderPrevButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(18f, 16f), new Vector2(140f, 40f));

            leaderPageText = UIStyle.CreateText(leaderPanel.transform, "Page", "", 15,
                TextAnchor.MiddleCenter, UIStyle.TextMain);
            UIStyle.SetRect(leaderPageText.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 17f), new Vector2(320f, 36f));

            leaderNextButton = UIStyle.CreateButton(leaderPanel.transform, "Next", "次のページ", 14,
                () => ChangeLeaderPage(1));
            UIStyle.SetRect(leaderNextButton.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-18f, 16f), new Vector2(140f, 40f));

            leaderPanel.SetActive(false);
        }

        void BuildControlHint()
        {
            // ドラッグ(左/右/中どのボタンでも)によるカメラ移動を追記(2026-07-21 Claude Code 変更。
            // 文言追加分は区切りを全角スペース1個に詰めて幅720に収める)
            var hint = UIStyle.CreateText(canvas.transform, "ControlHint",
                "左クリック：選択　右クリック：移動・攻撃　Enter：ターン終了　ドラッグ/WASD/矢印：カメラ移動",
                14, TextAnchor.MiddleCenter, UIStyle.TextMain);
            UIStyle.SetRect(hint.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(720f, 32f));
            var shadow = hint.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(1f, -1f);
        }

        void BuildTutorial()
        {
            helpButton = UIStyle.CreateButton(canvas.transform, "HelpButton", "？ 遊び方", 16,
                () => ShowTutorial(0));
            UIStyle.SetRect(helpButton.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-184f, 14f), new Vector2(120f, 46f));

            tutorialPanel = UIStyle.CreatePanel(canvas.transform, "TutorialPanel",
                new Color(0.06f, 0.08f, 0.12f, 0.97f));
            UIStyle.SetRect(tutorialPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(620f, 390f));

            var header = UIStyle.CreateText(tutorialPanel.transform, "Header", "はじめてガイド",
                18, TextAnchor.MiddleLeft, UIStyle.Accent);
            UIStyle.SetRect(header.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-36f, 30f));

            tutorialPageText = UIStyle.CreateText(tutorialPanel.transform, "Page", "",
                14, TextAnchor.MiddleRight, UIStyle.TextDim);
            UIStyle.SetRect(tutorialPageText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-28f, 26f));

            tutorialTitleText = UIStyle.CreateText(tutorialPanel.transform, "Title", "",
                22, TextAnchor.MiddleLeft, UIStyle.TextMain);
            tutorialTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIStyle.SetRect(tutorialTitleText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(-44f, 42f));

            tutorialBodyText = UIStyle.CreateText(tutorialPanel.transform, "Body", "",
                17, TextAnchor.UpperLeft, UIStyle.TextMain);
            tutorialBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tutorialBodyText.verticalOverflow = VerticalWrapMode.Truncate;
            tutorialBodyText.lineSpacing = 1.15f;
            UIStyle.SetRect(tutorialBodyText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(-48f, 190f));

            var legend = UIStyle.CreateText(tutorialPanel.transform, "Legend",
                "黄色＝移動可能　　赤＝攻撃可能　　白枠＝選択中",
                15, TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(legend.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(-40f, 26f));

            tutorialPrevButton = UIStyle.CreateButton(tutorialPanel.transform, "Prev", "前へ", 15,
                () => ChangeTutorialPage(-1));
            UIStyle.SetRect(tutorialPrevButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(22f, 18f), new Vector2(120f, 38f));

            var close = UIStyle.CreateButton(tutorialPanel.transform, "Close", "閉じる", 15,
                HideTutorial);
            UIStyle.SetRect(close.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(120f, 38f));

            tutorialNextButton = UIStyle.CreateButton(tutorialPanel.transform, "Next", "次へ", 15,
                () => ChangeTutorialPage(1));
            UIStyle.SetRect(tutorialNextButton.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-22f, 18f), new Vector2(120f, 38f));
            tutorialNextLabel = UIStyle.ButtonLabel(tutorialNextButton);

            tutorialPanel.SetActive(false);
        }

        void BuildTooltip()
        {
            tooltip = UIStyle.CreatePanel(canvas.transform, "TileTooltip", new Color(0.05f, 0.07f, 0.10f, 0.93f));
            tooltip.GetComponent<Image>().raycastTarget = false;
            tooltipRect = UIStyle.SetRect(tooltip, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 1f), Vector2.zero, new Vector2(210f, 90f));

            tooltipText = UIStyle.CreateText(tooltip.transform, "Text", "", 13, TextAnchor.UpperLeft, UIStyle.TextMain);
            UIStyle.StretchFull(tooltipText.gameObject, 8f);

            tooltip.SetActive(false);
        }

        void BuildGameOverOverlay()
        {
            gameOverOverlay = UIStyle.CreatePanel(canvas.transform, "GameOverOverlay", new Color(0f, 0f, 0f, 0.78f));
            UIStyle.StretchFull(gameOverOverlay);

            // 2026-07-21 Claude Code: 最終スコア一覧の追加に伴いメッセージを上段(y+235)へ、
            // ボタン2つを下段(y-140/-205)へ移動した(ハンドラ・文言・サイズは従来どおり)。
            // 中央(y+172〜約-46)は最終スコア一覧(最大8文明: ヘッダー22px+行24px×8)が使う。
            gameOverText = UIStyle.CreateText(gameOverOverlay.transform, "Message", "", 30, TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(gameOverText.gameObject, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 235f), new Vector2(-40f, 100f));

            // 最終スコア一覧(2026-07-21 Claude Code 追加): 全文明の最終成績。
            // 行の中身はゲームごとに変わるため ShowGameOver 時に再構築する。
            var statsGo = UIStyle.CreateContainer(gameOverOverlay.transform, "FinalStats");
            gameOverStatsRoot = UIStyle.SetRect(statsGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 1f), new Vector2(0f, 172f), new Vector2(620f, 232f));

            var restart = UIStyle.CreateButton(gameOverOverlay.transform, "RestartButton", "もう一度プレイ", 20, OnRestartClicked);
            UIStyle.SetRect(restart.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(240f, 54f));

            // 最終戦況(2026-07-21 Claude Code 追加):ゲーム終了画面から戦況グラフを開く。
            // ScoreGraphPanel.Show() は自ルートを SetAsLastSibling するため、同一Canvas内の
            // 暗幕オーバーレイ(gameOverOverlay。ShowGameOver で最前面化済み)よりさらに手前に
            // 描画される。閉じる(グラフの×ボタン/Esc)とオーバーレイ表示へ戻り、
            // 「もう一度プレイ」は従来どおり押せる。
            var finalGraph = UIStyle.CreateButton(gameOverOverlay.transform, "FinalScoreGraphButton",
                "最終戦況", 18, OnFinalScoreGraphClicked);
            UIStyle.SetRect(finalGraph.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-125f, -205f), new Vector2(240f, 48f));

            // 歴史ツアー(2026-07-21 Claude Code 追加): 最終戦況の右隣。ChroniclePanel の
            // 歴史ツアーをツアーモードで直接開始する(記録イベントの現場をカメラで順に巡る。
            // ツアーのラベルCanvasはこのオーバーレイより手前のため終了画面上でも見える)。
            // 横並びにするため最終戦況を左(-125)へ寄せた(ハンドラ・文言・サイズは従来どおり)。
            var historyTour = UIStyle.CreateButton(gameOverOverlay.transform, "HistoryTourButton",
                "歴史ツアー", 18, OnHistoryTourClicked);
            UIStyle.SetRect(historyTour.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(125f, -205f), new Vector2(240f, 48f));

            gameOverOverlay.SetActive(false);
        }

        // ================= 公開API =================

        /// <summary>全パネルの表示内容を更新する。毎フレーム呼んでも安全。</summary>
        public void RefreshAll()
        {
            if (state == null || canvas == null) return;
            RefreshTopBar();
            RefreshUnitPanel();
            RefreshCityPanel();
            RefreshTechPanelIfNeeded();
            RefreshAudioControls();
            RefreshSimulationBar();
            if (civilizationPanel != null && civilizationPanel.activeSelf) RefreshCivilizationList();
            if (leaderPanel != null && leaderPanel.activeSelf) RefreshLeaderList();
        }

        /// <summary>選択中ユニットを設定する。null でユニットパネルを隠す。</summary>
        public void SetSelectedUnit(Unit u)
        {
            selectedUnit = u;
            if (state != null && canvas != null) RefreshUnitPanel();
        }

        /// <summary>都市パネルを開く(生産ボタンは actions.OnChooseProduction を呼ぶ)。</summary>
        public void ShowCityPanel(City city)
        {
            if (city == null || state == null || canvas == null) return;
            if (simulationModeActive) return;   // 観戦中は都市パネルを開かない
            shownCity = city;
            techPanel.SetActive(false);
            if (civilizationPanel != null) civilizationPanel.SetActive(false);
            if (leaderPanel != null) leaderPanel.SetActive(false);
            if (slotPanel != null) slotPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            cityPanel.SetActive(true);
            // 同一Canvas内の最前面へ(常時表示ボタン等が上に被らないようにする。2026-07-21 Z順修正)
            cityPanel.transform.SetAsLastSibling();
            cityListVersion = -1;   // 強制再構築
            RefreshCityPanel();
        }

        /// <summary>技術パネルを開く(選択は actions.OnChooseResearch を呼ぶ)。</summary>
        public void ShowTechPanel()
        {
            if (state == null || canvas == null) return;
            if (simulationModeActive) return;   // 観戦中は技術パネルを開かない
            var p = state.HumanPlayer;
            if (p == null) return;
            if (civilizationPanel != null) civilizationPanel.SetActive(false);
            if (leaderPanel != null) leaderPanel.SetActive(false);
            if (slotPanel != null) slotPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            techPanel.SetActive(true);
            // 同一Canvas内の最前面へ(常時表示ボタン等が上に被らないようにする。2026-07-21 Z順修正)
            techPanel.transform.SetAsLastSibling();
            techListVersion = -1;   // 強制再構築
            RefreshTechPanelIfNeeded();
        }

        /// <summary>都市/技術パネルとツールチップを閉じる。</summary>
        public void CloseAllPanels()
        {
            if (canvas == null) return;
            cityPanel.SetActive(false);
            techPanel.SetActive(false);
            if (civilizationPanel != null) civilizationPanel.SetActive(false);
            if (leaderPanel != null) leaderPanel.SetActive(false);
            if (slotPanel != null) slotPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (scoreGraphPanel != null) scoreGraphPanel.Hide();   // Esc でも閉じる(2026-07-20 追加)
            shownCity = null;
            HideTileTooltip();
        }

        /// <summary>
        /// シミュレーション観戦モードのUI切替(2026-07-20 Claude Code 追加。GameBootstrap が呼ぶ)。
        /// 観戦中は操作バーを表示し、人間の手番操作に属するボタン
        /// (ターン終了・次のユニット・セーブ/ロード)と、観戦バーに重なる「ゲーム設定」ボタンを隠す。
        /// 併せて開いているパネル・選択・チュートリアルを閉じる(観戦中はパネルを開けない)。
        /// </summary>
        public void SetSimulationMode(bool active)
        {
            simulationModeActive = active;
            if (canvas == null) return;
            if (simulationBar != null) simulationBar.SetActive(active);
            if (endTurnButton != null) endTurnButton.gameObject.SetActive(!active);
            if (nextUnitButton != null) nextUnitButton.gameObject.SetActive(!active);
            if (saveButton != null) saveButton.gameObject.SetActive(!active);
            if (loadButton != null) loadButton.gameObject.SetActive(!active);
            if (settingsButton != null) settingsButton.gameObject.SetActive(!active);
            // 観戦中の戦況グラフ開閉は観戦バー内のボタンで行う(二段目ボタンは隠す)
            if (scoreGraphButton != null) scoreGraphButton.gameObject.SetActive(!active);
            UpdateLogPosition();   // ログ先頭行が観戦バーの下に隠れないよう移動(2026-07-21 追加)
            UpdateEraIndicatorPosition();   // 時代表示も観戦バーを避けて移動(2026-07-22 追加)
            UpdateLeaderChipPosition();     // 首位チップも観戦バーを避けて移動(2026-07-22 追加)
            // 国家運営チップは観戦中(人間文明なし)は非表示。モード切替直後に判定し直す
            // (観戦バー・観戦時の時代/首位チップ位置と二段目左端で重ならないようにする)
            nextAdminChipAt = 0f;
            RefreshAdministrationChips();
            if (active)
            {
                CloseAllPanels();
                SetSelectedUnit(null);
                HideTutorial();
            }
            RefreshSimulationBar();
        }

        /// <summary>
        /// ログ表示位置を現在モードへ合わせる(2026-07-21 Claude Code 追加)。
        /// 観戦中は中央上の観戦バー(y-38〜-72、x340〜940)がログ(x12〜約478)の先頭行と
        /// 重なって隠すため、ログをバーの下(y-80)から開始する。通常プレイは二段目左端の
        /// 国家運営チップ列(y-38〜-64)の下 y-68(LogTopNormalY。2026-07-22 に -42 から変更)へ
        /// 戻す。どちらのモードでもトップバー・二段目の要素とは重ならない。
        /// </summary>
        void UpdateLogPosition()
        {
            if (logText == null) return;
            ((RectTransform)logText.transform).anchoredPosition =
                new Vector2(12f, simulationModeActive ? LogTopSimulationY : LogTopNormalY);
        }

        /// <summary>
        /// 観戦バーの一時停止/速度表示を更新する(2026-07-20 Claude Code 追加。GameBootstrap が呼ぶ)。
        /// </summary>
        public void SetSimulationStatus(bool paused, float speed)
        {
            if (simulationPauseLabel != null)
                simulationPauseLabel.text = paused ? "▶ 再開" : "⏸ 一時停止";
            if (simulationSpeedLabel != null)
                simulationSpeedLabel.text = "速度: " + SpeedLabelJa(speed);
        }

        /// <summary>速度倍率の日本語ラベル(1=等速, 2, 4, 8, 16, 32, 64, 128, 256=256倍速)。
        /// 2026-07-21 Claude Code: 64倍速の表示抜けを修正し、128倍速を追加。同日256倍速を追加。</summary>
        static string SpeedLabelJa(float speed)
        {
            if (speed >= 255.5f) return "256倍速";
            if (speed >= 127.5f) return "128倍速";
            if (speed >= 63.5f) return "64倍速";
            if (speed >= 31.5f) return "32倍速";
            if (speed >= 15.5f) return "16倍速";
            if (speed >= 7.5f) return "8倍速";
            if (speed >= 3.5f) return "4倍速";
            if (speed >= 1.5f) return "2倍速";
            return "等速";
        }

        /// <summary>
        /// フルスクリーン切替の要求(2026-07-21 Claude Code 追加。InputController の F11 が呼ぶ)。
        /// 実処理(Screen 切替・PlayerPrefs 保存)は GameBootstrap が OnFullscreenToggled で行い、
        /// 完了後に SetFullscreenState でラベルへ反映される。未配線時は何もしない。
        /// </summary>
        public void RequestFullscreenToggle()
        {
            OnFullscreenToggled?.Invoke();
        }

        /// <summary>
        /// 現在のフルスクリーン状態をゲーム設定画面のトグル行ラベルへ反映する
        /// (2026-07-21 Claude Code 追加。GameBootstrap が起動時・切替時に呼ぶ)。
        /// F11・設定画面のどちらから切り替えてもラベルは常に一致する。
        /// </summary>
        public void SetFullscreenState(bool active)
        {
            fullscreenOn = active;
            if (fullscreenLabel != null)
                fullscreenLabel.text = "フルスクリーン: " + (active ? "ON" : "OFF");
        }

        /// <summary>
        /// 現在フルスクリーン状態か(2026-07-21 Claude Code 追加)。
        /// InputController の「Esc に仕事が無ければフルスクリーン解除」判定に使う。
        /// 状態は GameBootstrap が起動時・切替時に SetFullscreenState で同期している。
        /// </summary>
        public bool IsFullscreenActive => fullscreenOn;

        /// <summary>観戦バーの現在ターン表示(RefreshAll から毎回呼ばれる。非表示中は何もしない)。</summary>
        void RefreshSimulationBar()
        {
            if (simulationBar == null || !simulationBar.activeSelf) return;
            if (simulationTurnText == null || state == null) return;
            simulationTurnText.text =
                $"ターン {state.TurnNumber}/{(state.Config != null ? state.Config.MaxTurns : 0)}";
        }

        /// <summary>
        /// 都市パネルまたは技術パネルが表示中か(2026-07-20 Claude Code 追加)。
        /// ターン開始時の未行動ユニット自動選択が、開いているパネルを勝手に閉じないための判定に使う。
        /// </summary>
        public bool IsCityOrTechPanelOpen =>
            (cityPanel != null && cityPanel.activeSelf) ||
            (techPanel != null && techPanel.activeSelf) ||
            (civilizationPanel != null && civilizationPanel.activeSelf) ||
            (leaderPanel != null && leaderPanel.activeSelf) ||
            (slotPanel != null && slotPanel.activeSelf) ||
            (settingsPanel != null && settingsPanel.activeSelf);

        /// <summary>
        /// Esc で閉じる対象のパネルがいずれか開いているか(2026-07-21 Claude Code 追加)。
        /// UIManager 管轄のパネル(都市/技術/文明/指導者/スロット/設定)に加えて、
        /// 戦況グラフ・はじめてガイド・独立Canvas UI(世界史図鑑・文化政策)も含む。
        /// InputController が「Esc に仕事があったか」(何も無ければフルスクリーン解除)の判定に使う。
        /// </summary>
        public bool AnyPanelOpen =>
            IsCityOrTechPanelOpen ||
            (scoreGraphPanel != null && scoreGraphPanel.IsVisible) ||
            (tutorialPanel != null && tutorialPanel.activeSelf) ||
            IsExternalPanelOpen;

        /// <summary>
        /// 独立Canvas UI(Codex 実装の世界史図鑑・文化政策)が開いているか(2026-07-21 Claude Code 追加)。
        /// これらは Esc を自前の Update で処理して閉じるため、UIManager からは閉じない(読み取り専用)。
        /// 開閉クエリの公開APIが無いため、各 BuildPanel が生成する固定名の子パネルを参照する
        /// (未構築・不在の間は false = 開いていない扱いで安全)。
        /// </summary>
        public bool IsExternalPanelOpen
        {
            get
            {
                CacheExternalPanels();
                return (worldHistoryPanelRoot != null && worldHistoryPanelRoot.gameObject.activeInHierarchy) ||
                       (culturePanelRoot != null && culturePanelRoot.gameObject.activeInHierarchy);
            }
        }

        /// <summary>独立UIコンポーネントと内部パネルの参照を(未取得の間だけ)キャッシュする。
        /// 一度見つかれば以後は activeInHierarchy の読み取りのみで毎フレーム呼んでも軽量。</summary>
        void CacheExternalPanels()
        {
            if (worldHistoryRef == null) worldHistoryRef = FindFirstObjectByType<WorldHistoryPanel>();
            if (worldHistoryPanelRoot == null && worldHistoryRef != null)
                worldHistoryPanelRoot = worldHistoryRef.transform.Find("WorldHistoryCanvas/WorldHistoryPanel");
            if (culturePanelRef == null) culturePanelRef = FindFirstObjectByType<CulturePanel>();
            if (culturePanelRoot == null && culturePanelRef != null)
                culturePanelRoot = culturePanelRef.transform.Find("CultureCanvas/CulturePanel");
        }

        // ================= パネル開閉ボタンのアイコン装飾(2026-07-21 Claude Code 追加) =================

        /// <summary>
        /// 左下の独立Canvasボタン3つ(Codex 実装の世界史図鑑・文化・政策・遺産・偉人・作品)へ
        /// 種類別アイコンを添え、あわせて最背面キャンバスへZ順退避する(2026-07-21 追加)。
        /// 各ボタンは独立コンポーネントが自前のCanvasに構築するため、構築完了まで0.5秒間隔で
        /// 探索し、見つかった順にコンポーネントを追加するだけでサイズ・位置・ハンドラ・
        /// Codex側のコードには一切触れない。3つ揃ったら以後は何もしない。
        /// </summary>
        void DecorateExternalPanelButtons()
        {
            if (externalButtonIconsDone) return;
            if (Time.unscaledTime < nextExternalButtonIconTryAt) return;
            nextExternalButtonIconTryAt = Time.unscaledTime + 0.5f;

            CacheExternalPanels();
            if (legacyPanelRef == null) legacyPanelRef = FindFirstObjectByType<LegacyPanel>();

            bool book = TryDecorateOpenButton(
                worldHistoryRef != null ? worldHistoryRef.transform : null,
                "WorldHistoryCanvas/WorldHistoryButton", "book");
            bool flag = TryDecorateOpenButton(
                culturePanelRef != null ? culturePanelRef.transform : null,
                "CultureCanvas/CultureButton", "flag");
            bool star = TryDecorateOpenButton(
                legacyPanelRef != null ? legacyPanelRef.transform : null,
                "WorldLegacyCanvas/WorldLegacyButton", "star");
            externalButtonIconsDone = book && flag && star;
        }

        /// <summary>固定名の開閉ボタンを探してアイコン付与+Z順退避を行う(未構築なら false=後で再試行)。</summary>
        static bool TryDecorateOpenButton(Transform root, string path, string kind)
        {
            if (root == null) return false;
            var t = root.Find(path);
            if (t == null) return false;
            var button = t.GetComponent<Button>();
            if (button == null) return false;
            UIStyle.AddButtonIcon(button, kind, 20f, 6f);
            DemoteFloatingButtonBehindPanels(button.gameObject);
            return true;
        }

        /// <summary>
        /// 常時表示の左下ボタンを専用のネストCanvas(overrideSorting・sortingOrder=-5)へ退避する
        /// (2026-07-21 Claude Code 追加:Z順修正)。実プレイのスクリーンショットで、独立Canvas
        /// (世界史図鑑130/文化135/遺産140 — いずれも本Canvas=100より上)に属するこれらのボタンが、
        /// 開いているモーダルパネル(ゲーム設定や互いの図鑑パネル)の上へ浮いて描画されていた。
        /// ボタンGO自身へ Canvas を追加して sortingOrder=-5 にすると、Screen Space Overlay の
        /// 全Canvasの最背面(ただし3Dワールドよりは常に手前)になり、どのパネルを開いても
        /// ボタンが上へ被らない。ネストCanvas配下のGraphicは親CanvasのRaycaster対象から外れる
        /// ため GraphicRaycaster を併設し、クリックは従来どおり有効(パネルが上に重なっている間は
        /// sortingOrder の高いパネル側が優先して受ける=貫通クリックも防止)。
        /// 位置・サイズ・onClick・Codex側コードには一切触れない追加のみの処置で、冪等。
        /// </summary>
        static void DemoteFloatingButtonBehindPanels(GameObject go)
        {
            if (go == null || go.GetComponent<Canvas>() != null) return;
            var nested = go.AddComponent<Canvas>();
            nested.overrideSorting = true;
            nested.sortingOrder = -5;
            go.AddComponent<GraphicRaycaster>();
        }

        /// <summary>ポインタがUI要素上にあるか。</summary>
        public bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// テキスト入力欄(ゲーム設定のシード値)にフォーカスがあるか(2026-07-20 Claude Code 追加)。
        /// InputController がホットキー(Enter/Space/Tab/F5/F9 等)を入力中に反応させないための判定。
        /// </summary>
        public bool IsTextInputFocused => seedInput != null && seedInput.isFocused;

        /// <summary>タイルツールチップ(マウス追従)を表示する。</summary>
        public void ShowTileTooltip(Tile tile)
        {
            if (state == null || canvas == null) return;
            if (tile == null) { HideTileTooltip(); return; }

            var lines = new List<string>();

            string terrain = tile.Def.NameJa;
            if (tile.HasHill) terrain += "(丘陵)";
            if (tile.HasForest) terrain += "(森林)";
            lines.Add(terrain);

            var y = tile.GetYields();
            string yieldLine = $"食料{y.Food} 生産{y.Production}";
            if (y.Science > 0) yieldLine += $" 科学{y.Science}";
            lines.Add(yieldLine);

            if (tile.Resource != ResourceType.None)
                lines.Add($"資源: {GameRules.Resources[tile.Resource].NameJa}");

            if (tile.OwnerPlayerId >= 0)
            {
                var owner = state.GetPlayer(tile.OwnerPlayerId);
                if (owner != null) lines.Add($"領有: {owner.NameJa}");
            }

            if (tile.City != null)
                lines.Add($"都市: {tile.City.NameJa}(人口{tile.City.Population})");

            // ユニットは人間プレイヤーの視界内のみ表示(探索済みだが視界外の敵を漏らさない)
            var human = state.HumanPlayer;
            if (tile.Unit != null && (human == null || human.Visible.Contains(tile.Coord)))
            {
                var uo = state.GetPlayer(tile.Unit.PlayerId);
                lines.Add($"ユニット: {tile.Unit.Def.NameJa}({(uo != null ? uo.NameJa : "?")})");
            }

            tooltipText.text = string.Join("\n", lines);
            tooltipRect.sizeDelta = new Vector2(210f, 14f + lines.Count * 18f);
            tooltip.SetActive(true);
            tooltip.transform.SetAsLastSibling();
            if (gameOverOverlay.activeSelf) gameOverOverlay.transform.SetAsLastSibling();
            tooltipVisible = true;
            PositionTooltip();
        }

        /// <summary>タイルツールチップを隠す。</summary>
        public void HideTileTooltip()
        {
            tooltipVisible = false;
            if (tooltip != null) tooltip.SetActive(false);
        }

        /// <summary>ログ行を追加する(最新 約6行 を左上に表示)。</summary>
        public void AddLog(string messageJa)
        {
            if (string.IsNullOrEmpty(messageJa)) return;
            logLines.Add(messageJa);
            while (logLines.Count > MaxLogLines) logLines.RemoveAt(0);
            if (logText != null) logText.text = string.Join("\n", logLines);
        }

        /// <summary>ゲームオーバー画面を表示する。「もう一度プレイ」→ actions.OnRestart。</summary>
        public void ShowGameOver(string messageJa)
        {
            if (canvas == null) return;
            CloseAllPanels();
            HideTutorial();
            SetSelectedUnit(null);
            gameOverText.text = string.IsNullOrEmpty(messageJa) ? "ゲーム終了" : messageJa;
            RebuildGameOverStats();   // 全文明の最終成績(2026-07-21 追加)
            gameOverOverlay.SetActive(true);
            gameOverOverlay.transform.SetAsLastSibling();

            // 紙吹雪(2026-07-21 Claude Code 追加): 勝利文脈のみ表示する。
            // 終了メッセージ(GameOverMessageJa)は勝者名を含む勝利文か「ターン上限に到達。ゲーム終了」
            // (勝者なし)のみで敗北専用文言は存在しないため、判定は Winner フィールドで行う:
            //   観戦モード(HumanPlayer == null) = 勝者が決まれば常に表示(勝者の色パレット)
            //   通常プレイ = 人間プレイヤーが勝者の場合のみ表示(敗北時はスキップ)
            var human = state != null ? state.HumanPlayer : null;
            var winner = state != null ? state.Winner : null;
            bool celebrate = winner != null && (human == null || winner == human);
            if (celebrate) StartGameOverConfetti(winner);
            else StopConfetti();
        }

        // ================= 終了画面の最終スコア一覧+紙吹雪(2026-07-21 Claude Code 追加) =================

        /// <summary>
        /// 全文明(生存・滅亡とも)の最終成績をスコア降順で並べる。
        /// 列: 色スウォッチ+文明名 / スコア / 都市数 / 技術数 / 文化累計。
        /// 滅亡文明は ☠ 付きの灰色表示(戦況グラフの凡例と同じ流儀)。
        /// 最大8文明(ゲーム設定の上限)がヘッダー22px+行24px×8で領域(高さ232)に収まる。
        /// </summary>
        void RebuildGameOverStats()
        {
            if (gameOverStatsRoot == null || state == null) return;
            ClearChildren(gameOverStatsRoot);

            var players = new List<Player>(state.Players);
            players.Sort((a, b) =>
            {
                int sa = GameOverScoreOf(a), sb = GameOverScoreOf(b);
                if (sa != sb) return sb.CompareTo(sa);   // スコア降順
                return a.Id.CompareTo(b.Id);             // 同点はId昇順で安定
            });

            // ヘッダー行
            CreateStatsCell("HeadCiv", "文明", 30f, 210f, 0f, TextAnchor.MiddleLeft, UIStyle.TextDim, 12);
            CreateStatsCell("HeadScore", "スコア", 245f, 85f, 0f, TextAnchor.MiddleRight, UIStyle.TextDim, 12);
            CreateStatsCell("HeadCities", "都市", 335f, 65f, 0f, TextAnchor.MiddleRight, UIStyle.TextDim, 12);
            CreateStatsCell("HeadTechs", "技術", 405f, 65f, 0f, TextAnchor.MiddleRight, UIStyle.TextDim, 12);
            CreateStatsCell("HeadCulture", "文化累計", 475f, 120f, 0f, TextAnchor.MiddleRight, UIStyle.TextDim, 12);

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                float y = -26f - i * 24f;
                var textColor = p.IsEliminated ? UIStyle.TextDim : UIStyle.TextMain;

                // 色スウォッチ(滅亡文明は半透明にして灰色行に馴染ませる)
                var sgo = new GameObject("Swatch" + i, typeof(RectTransform), typeof(Image));
                sgo.transform.SetParent(gameOverStatsRoot, false);
                var swatch = sgo.GetComponent<Image>();
                swatch.raycastTarget = false;
                var sc = p.Color;
                sc.a = p.IsEliminated ? 0.35f : 1f;
                swatch.color = sc;
                UIStyle.SetRect(sgo, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(8f, y - 4f), new Vector2(14f, 14f));

                string name = (p.IsEliminated ? "☠ " : "") + p.NameJa;
                CreateStatsCell("Name" + i, name, 30f, 210f, y, TextAnchor.MiddleLeft, textColor, 14);
                CreateStatsCell("Score" + i, GameOverScoreOf(p).ToString(), 245f, 85f, y,
                    TextAnchor.MiddleRight, textColor, 14);
                CreateStatsCell("Cities" + i, p.Cities.Count.ToString(), 335f, 65f, y,
                    TextAnchor.MiddleRight, textColor, 14);
                CreateStatsCell("Techs" + i, p.KnownTechs.Count.ToString(), 405f, 65f, y,
                    TextAnchor.MiddleRight, textColor, 14);
                CreateStatsCell("Culture" + i, p.TotalCulture.ToString(), 475f, 120f, y,
                    TextAnchor.MiddleRight, textColor, 14);
            }
        }

        /// <summary>最終スコア一覧の1セル(gameOverStatsRoot の左上基準。行高24pxに収まる22px)。</summary>
        Text CreateStatsCell(string name, string text, float x, float width, float y,
            TextAnchor anchor, Color color, int fontSize)
        {
            var t = UIStyle.CreateText(gameOverStatsRoot, name, text, fontSize, anchor, color);
            UIStyle.SetRect(t.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, y), new Vector2(width, 22f));
            return t;
        }

        /// <summary>
        /// TurnManager のスコア勝利と同じ式(Σ人口×3 + 都市数×8 + 技術数×5)。
        /// ScoreGraphPanel.ScoreOf と同様の表示専用の複製で、シミュレーションには一切影響しない。
        /// </summary>
        static int GameOverScoreOf(Player p)
        {
            int score = 0;
            for (int i = 0; i < p.Cities.Count; i++)
                score += p.Cities[i].Population * 3;
            return score + p.Cities.Count * 8 + p.KnownTechs.Count * 5;
        }

        /// <summary>
        /// 勝利時の紙吹雪を開始する(2026-07-21 Claude Code 追加)。
        /// 約40枚の小さなUI Imageクアッドを画面上端から約3秒(非スケール時間)かけて、
        /// 横揺れ+回転しながら落下させる。色は勝者の文明色を基調としたパレット。
        /// GOはプール式で再利用し、落下終了・オーバーレイ非表示で自動的に隠れる。
        /// raycastTarget を持たないため「もう一度プレイ」等のクリックを一切遮らない。
        /// 乱数は演出専用の UnityEngine.Random のみ使用(state.Rng には触れない=決定論不変)。
        /// </summary>
        void StartGameOverConfetti(Player winner)
        {
            if (canvas == null || gameOverOverlay == null || winner == null) return;
            var palette = new[]
            {
                winner.Color,
                Color.Lerp(winner.Color, Color.white, 0.45f),
                Color.Lerp(winner.Color, UIStyle.Accent, 0.55f),
                UIStyle.Accent,
                new Color(0.95f, 0.95f, 0.98f, 1f),
            };

            EnsureConfettiPool();
            confettiStartAt = Time.unscaledTime;
            confettiActive = true;

            for (int i = 0; i < confettiPieces.Count; i++)
            {
                var piece = confettiPieces[i];
                if (piece == null || piece.Rect == null) continue;
                float ax = UnityEngine.Random.value;
                piece.Rect.anchorMin = new Vector2(ax, 1f);
                piece.Rect.anchorMax = new Vector2(ax, 1f);
                piece.Rect.pivot = new Vector2(0.5f, 0.5f);
                piece.Rect.sizeDelta = new Vector2(
                    UnityEngine.Random.Range(7f, 13f), UnityEngine.Random.Range(5f, 9f));
                piece.Rect.anchoredPosition = new Vector2(0f, 30f);   // オーバーレイ上端の少し上
                piece.Delay = UnityEngine.Random.Range(0f, 0.7f);
                piece.Duration = UnityEngine.Random.Range(2.1f, 3.0f);
                piece.SwayAmp = UnityEngine.Random.Range(10f, 36f);
                piece.SwayFreq = UnityEngine.Random.Range(2.2f, 4.6f);
                piece.SwayPhase = UnityEngine.Random.Range(0f, 6.2832f);
                piece.RotSpeed = UnityEngine.Random.Range(-320f, 320f);
                piece.BaseColor = palette[UnityEngine.Random.Range(0, palette.Length)];
                piece.Image.color = piece.BaseColor;
                piece.Rect.gameObject.SetActive(false);   // 遅延中は非表示(UpdateConfetti が表示する)
            }
        }

        /// <summary>不足分の紙吹雪GOを生成する(破棄済み参照は先に除去。冪等)。</summary>
        void EnsureConfettiPool()
        {
            for (int i = confettiPieces.Count - 1; i >= 0; i--)
                if (confettiPieces[i] == null || confettiPieces[i].Rect == null)
                    confettiPieces.RemoveAt(i);

            while (confettiPieces.Count < ConfettiCount)
            {
                var go = new GameObject("Confetti" + confettiPieces.Count, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(gameOverOverlay.transform, false);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                go.SetActive(false);
                confettiPieces.Add(new ConfettiPiece { Rect = (RectTransform)go.transform, Image = img });
            }
        }

        /// <summary>紙吹雪のアニメーション(Update から毎フレーム。非アクティブ時は即return)。</summary>
        void UpdateConfetti()
        {
            if (!confettiActive) return;
            if (gameOverOverlay == null || !gameOverOverlay.activeSelf)
            {
                StopConfetti();   // リスタート等でオーバーレイが閉じたら演出も止める
                return;
            }

            float fall = (canvasRect != null ? canvasRect.rect.height : 720f) + 80f;
            float now = Time.unscaledTime;
            bool anyAlive = false;

            for (int i = 0; i < confettiPieces.Count; i++)
            {
                var piece = confettiPieces[i];
                if (piece == null || piece.Rect == null) continue;
                float t = now - confettiStartAt - piece.Delay;
                if (t < 0f) { anyAlive = true; continue; }   // まだ落下開始前
                if (t >= piece.Duration)
                {
                    if (piece.Rect.gameObject.activeSelf) piece.Rect.gameObject.SetActive(false);
                    continue;
                }

                anyAlive = true;
                if (!piece.Rect.gameObject.activeSelf) piece.Rect.gameObject.SetActive(true);
                float p = t / piece.Duration;
                float eased = p * p * 0.35f + p * 0.65f;   // わずかに加速しながら落ちる
                float x = Mathf.Sin(t * piece.SwayFreq + piece.SwayPhase) * piece.SwayAmp;
                piece.Rect.anchoredPosition = new Vector2(x, 30f - fall * eased);
                piece.Rect.localRotation = Quaternion.Euler(0f, 0f, piece.RotSpeed * t);
                var c = piece.BaseColor;
                if (p > 0.82f) c.a = Mathf.Clamp01((1f - p) / 0.18f);   // 末尾でフェードアウト
                piece.Image.color = c;
            }

            if (!anyAlive) confettiActive = false;   // 全枚落下完了(GOはプールに残す)
        }

        /// <summary>紙吹雪を即座に止めて全て隠す(プールは保持)。</summary>
        void StopConfetti()
        {
            confettiActive = false;
            for (int i = 0; i < confettiPieces.Count; i++)
                if (confettiPieces[i] != null && confettiPieces[i].Rect != null)
                    confettiPieces[i].Rect.gameObject.SetActive(false);
        }

        // ================= イベントバナー(2026-07-20 Claude Code 追加) =================

        /// <summary>
        /// 画面上部中央(トップバー・観戦バーの下)に大きなイベントバナーを一時表示する。
        /// 観戦演出(宣戦布告・和平・都市陥落・滅亡・勝者)用に GameBootstrap が呼ぶ。
        /// 約2.5秒(非スケール時間)で自動フェードアウトし、同時表示は最大3枚
        /// (超過時は最古を除去)。同時表示中は8px間隔で上から縦に積まれ、重ならない
        /// (LayoutEventBanners)。raycastTarget を持たないためクリックを一切遮らない。
        /// </summary>
        public void ShowEventBanner(string messageJa, Color accent)
        {
            ShowEventBannerCore(messageJa, accent, false);
        }

        /// <summary>
        /// 通常プレイ用の控えめなイベント通知(2026-07-21 Claude Code 追加)。
        /// ShowEventBanner と同じ位置(画面上部中央)に、小さめのバナーを約2秒で
        /// フェードアウト表示する。カメラ操作・自動ターン進行には一切影響しない。
        /// </summary>
        public void ShowEventBannerCompact(string messageJa, Color accent)
        {
            ShowEventBannerCore(messageJa, accent, true);
        }

        /// <summary>バナー生成の共通処理(compact=true で小型・短寿命の通常プレイ版)。</summary>
        void ShowEventBannerCore(string messageJa, Color accent, bool compact)
        {
            if (canvas == null || string.IsNullOrEmpty(messageJa)) return;

            while (eventBanners.Count >= MaxEventBanners)
            {
                var oldest = eventBanners[0];
                eventBanners.RemoveAt(0);
                if (oldest.Root != null) Destroy(oldest.Root);
            }

            var root = UIStyle.CreatePanel(canvas.transform,
                compact ? "EventBannerCompact" : "EventBanner",
                new Color(0.05f, 0.07f, 0.11f, compact ? 0.80f : 0.88f));
            root.GetComponent<Image>().raycastTarget = false;
            // サイズは Height としてエントリにも記録し、縦積みスロット計算と常に一致させる
            var size = compact ? new Vector2(440f, 34f) : new Vector2(640f, 52f);
            UIStyle.SetRect(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, EventBannerStackTop), size);

            var stripe = UIStyle.CreatePanel(root.transform, "Stripe", accent);
            stripe.GetComponent<Image>().raycastTarget = false;
            UIStyle.SetRect(stripe, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(compact ? 4f : 6f, 0f));

            var text = UIStyle.CreateText(root.transform, "Text", messageJa, compact ? 15 : 24,
                TextAnchor.MiddleCenter, accent);
            UIStyle.StretchFull(text.gameObject, compact ? 6f : 8f);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1f, -1f);

            var group = root.AddComponent<CanvasGroup>();
            // モーダル表示中は生成時から減光する(2026-07-22 追加。位置は LayoutEventBanners が決める)
            group.alpha = modalOpenCount > 0 ? EventBannerModalAlpha : 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            eventBanners.Add(new EventBannerEntry
            {
                Root = root,
                Group = group,
                BornAt = Time.unscaledTime,
                Compact = compact,
                Lifetime = compact ? CompactEventBannerLifetime : EventBannerLifetime,
                FadeStart = compact ? CompactEventBannerFadeStart : EventBannerFadeStart,
                Height = size.y,
            });
            LayoutEventBanners();   // 新しいバナーは次の空きスロット(既存の下)へ入る
        }

        /// <summary>バナーの寿命管理とフェード(Update から毎フレーム呼ばれる。非スケール時間)。</summary>
        void UpdateEventBanners()
        {
            if (eventBanners.Count == 0) return;
            // モーダル開閉が変わったら退避配置⇔通常配置を切り替える(2026-07-22 追加)
            bool modal = modalOpenCount > 0;
            if (modal != bannersInModalLayout) LayoutEventBanners();
            float dim = modal ? EventBannerModalAlpha : 1f;   // モーダル中は約65%不透明
            bool removed = false;
            for (int i = eventBanners.Count - 1; i >= 0; i--)
            {
                var e = eventBanners[i];
                if (e.Root == null)
                {
                    eventBanners.RemoveAt(i);
                    removed = true;
                    continue;
                }
                float age = Time.unscaledTime - e.BornAt;
                if (age >= e.Lifetime)
                {
                    Destroy(e.Root);
                    eventBanners.RemoveAt(i);
                    removed = true;
                    continue;
                }
                if (e.Group != null)
                    e.Group.alpha = dim * (age <= e.FadeStart
                        ? 1f
                        : 1f - (age - e.FadeStart) / (e.Lifetime - e.FadeStart));
            }
            if (removed) LayoutEventBanners();
        }

        /// <summary>
        /// 同時表示中のバナーを発生順に上から縦へ積む(2026-07-21 Claude Code:重なり防止の縦積み化)。
        /// 最上段は y=EventBannerStackTop。各バナーは自身の実高さ+8pxの隙間で次のスロットを作り、
        /// 大小(観戦用/コンパクト)が混在しても重ならない。追加時と消滅時に呼ばれるため、
        /// 先行バナーがフェードアウトで消えるとスロットは上へ詰まる。
        /// </summary>
        void LayoutEventBanners()
        {
            // モーダル表示中は最上段を画面最上端へ退避する(2026-07-22 追加。
            // パネルのタイトル帯とバナーが重ならないようにする)
            bannersInModalLayout = modalOpenCount > 0;
            float y = bannersInModalLayout ? EventBannerStackTopModal : EventBannerStackTop;
            for (int i = 0; i < eventBanners.Count; i++)
            {
                var e = eventBanners[i];
                if (e.Root == null) continue;
                ((RectTransform)e.Root.transform).anchoredPosition = new Vector2(0f, y);
                // Height 未設定(0以下)の防御的フォールバックは種別ごとの生成サイズと同値
                float h = e.Height > 0f ? e.Height : (e.Compact ? 34f : 52f);
                y -= h + EventBannerStackGap;
            }
        }

        // ================= モーダル開閉カウンタ(2026-07-22 Claude Code 追加) =================

        /// <summary>
        /// 独立Canvasのパネル(実績一覧・図鑑など)が自分の開閉を通知するための公開静的API。
        /// 開いた時に true、閉じた時に false で必ず対にして呼ぶこと(UIManager 側からは呼ばない)。
        /// カウンタが0より大きい間、イベントバナーは画面最上端へ退避し約65%不透明になる。
        /// 負値へは決して下がらない(閉じ通知の過多は0で吸収)。
        /// </summary>
        public static void NotifyExternalPanel(bool open)
        {
            modalOpenCount = Mathf.Max(0, modalOpenCount + (open ? 1 : -1));
        }

        /// <summary>
        /// UIManager 管轄の全画面/モーダルパネルのいずれかが開いているか(バナー退避判定用)。
        /// 中央系のパネル+終了画面を対象とし、右側の都市パネルは中央上のバナーと重ならないため
        /// 含めない(従来挙動の維持)。戦況グラフは 2026-07-22 に自己申告
        /// (ScoreGraphPanel.SyncModalNotify → NotifyExternalPanel)へ一本化したためここには含めない
        /// (二重計上の回避。実績・年表パネルと同じ退避経路に揃えた)。
        /// </summary>
        bool IsSelfModalPanelOpen =>
            (techPanel != null && techPanel.activeSelf) ||
            (civilizationPanel != null && civilizationPanel.activeSelf) ||
            (leaderPanel != null && leaderPanel.activeSelf) ||
            (slotPanel != null && slotPanel.activeSelf) ||
            (settingsPanel != null && settingsPanel.activeSelf) ||
            (tutorialPanel != null && tutorialPanel.activeSelf) ||
            (gameOverOverlay != null && gameOverOverlay.activeSelf);

        /// <summary>
        /// 自分のモーダル開閉状態をまとめて1件として modalOpenCount へ反映する
        /// (Update から毎フレーム。bool比較のみで軽量。開閉箇所への個別配線を不要にする)。
        /// </summary>
        void SyncSelfModalContribution()
        {
            bool open = canvas != null && IsSelfModalPanelOpen;
            if (open == selfModalCounted) return;
            selfModalCounted = open;
            modalOpenCount = Mathf.Max(0, modalOpenCount + (open ? 1 : -1));
        }

        /// <summary>自分の計上分を取り消す(再Init・破棄時。外部パネルの計上分は保持される)。</summary>
        void ReleaseSelfModalContribution()
        {
            if (!selfModalCounted) return;
            selfModalCounted = false;
            modalOpenCount = Mathf.Max(0, modalOpenCount - 1);
        }

        public void ShowTutorial(int page = 0)
        {
            if (tutorialPanel == null) return;
            tutorialAutoAdvancePending = false;   // 再表示時は自動進行待ちを取り消す
            tutorialPage = Mathf.Clamp(page, 0, TutorialTitles.Length - 1);
            tutorialPanel.SetActive(true);
            tutorialPanel.transform.SetAsLastSibling();
            RefreshTutorial();
        }

        public void HideTutorial()
        {
            tutorialAutoAdvancePending = false;
            if (tutorialPanel != null && tutorialPanel.activeSelf)
            {
                tutorialPanel.SetActive(false);
                MarkTutorialSeen();   // 初めて閉じた時点で「閲覧済み」を記録(初回起動判定用)
            }
        }

        /// <summary>
        /// はじめてガイドの「閲覧済み」フラグを保存する(2026-07-20 Claude Code 追加)。
        /// 実際に表示中のガイドが閉じられた時のみ HideTutorial から呼ばれる。
        /// 既に記録済みなら何もしない(手動再表示→閉じるでは書き込みが発生しない)。
        /// </summary>
        void MarkTutorialSeen()
        {
            if (PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1) return;
            PlayerPrefs.SetInt(TutorialSeenKey, 1);
            PlayerPrefs.Save();
        }

        void ChangeTutorialPage(int delta)
        {
            tutorialAutoAdvancePending = false;   // 手動ナビゲーションは自動進行待ちより優先
            if (delta > 0 && tutorialPage >= TutorialTitles.Length - 1)
            {
                HideTutorial();
                return;
            }

            tutorialPage = Mathf.Clamp(tutorialPage + delta, 0, TutorialTitles.Length - 1);
            RefreshTutorial();
        }

        /// <summary>
        /// 実操作イベントの通知(2026-07-20 Claude Code 追加。既存APIには変更なし)。
        /// チュートリアル表示中に現在ページの期待操作が行われたら、本文に「✓ できました！」を
        /// 表示し、約1秒後に自動で次ページへ進む(最終ページでは「ゲーム開始」と同じ経路で閉じる)。
        /// 非表示中・対象外イベント時は何もしない軽量な no-op。手動の「前へ/次へ」も従来どおり有効。
        /// </summary>
        public void NotifyTutorialEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;
            if (tutorialPanel == null || !tutorialPanel.activeSelf) return;
            if (tutorialAutoAdvancePending) return;   // 確認表示中は二重発火を無視
            if (tutorialPage < 0 || tutorialPage >= TutorialEventIds.Length) return;

            var expected = TutorialEventIds[tutorialPage];
            if (expected == null) return;             // 操作対象のないページは手動のみ
            bool match = false;
            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] == eventId) { match = true; break; }
            }
            if (!match) return;

            tutorialAutoAdvancePending = true;
            tutorialAutoAdvanceAt = Time.unscaledTime + TutorialAutoAdvanceDelay;
            if (tutorialBodyText != null)
                tutorialBodyText.text = TutorialDonePrefix + TutorialBodies[tutorialPage];
        }

        /// <summary>自動進行タイマー(Update から毎フレーム呼ばれる)。</summary>
        void UpdateTutorialAutoAdvance()
        {
            if (!tutorialAutoAdvancePending) return;
            if (tutorialPanel == null || !tutorialPanel.activeSelf)
            {
                tutorialAutoAdvancePending = false;   // 表示が閉じられていたら取り消す
                return;
            }
            if (Time.unscaledTime < tutorialAutoAdvanceAt) return;
            tutorialAutoAdvancePending = false;
            ChangeTutorialPage(1);   // 最終ページなら従来どおり閉じる(ページ超過はしない)
        }

        void RefreshTutorial()
        {
            if (tutorialPanel == null || !tutorialPanel.activeSelf) return;
            tutorialPageText.text = (tutorialPage + 1) + " / " + TutorialTitles.Length;
            tutorialTitleText.text = TutorialTitles[tutorialPage];
            tutorialBodyText.text = TutorialBodies[tutorialPage];
            tutorialPrevButton.interactable = tutorialPage > 0;
            tutorialNextLabel.text = tutorialPage == TutorialTitles.Length - 1 ? "ゲーム開始" : "次へ";
        }

        // ================= ボタンハンドラ =================

        void OnEndTurnClicked()
        {
            if (state == null || state.IsGameOver) return;
            actions?.OnEndTurn?.Invoke();
        }

        void OnNextUnitClicked()
        {
            if (state == null || state.IsGameOver) return;
            OnNextUnit?.Invoke();
        }

        void OnResearchButtonClicked()
        {
            if (state == null || state.IsGameOver) return;
            if (techPanel.activeSelf) techPanel.SetActive(false);
            else ShowTechPanel();
        }

        void OnCivilizationButtonClicked()
        {
            if (state == null || state.IsGameOver) return;
            ShowCivilizationPanel();
        }

        void OnLeaderButtonClicked()
        {
            if (state == null || state.IsGameOver || state.HumanPlayer == null) return;
            ShowLeaderPanel();
        }

        void OnFoundCityClicked()
        {
            if (state == null || state.IsGameOver || selectedUnit == null) return;
            actions?.OnFoundCity?.Invoke(selectedUnit);
            RefreshAll();
        }

        void OnFortifyClicked()
        {
            if (state == null || state.IsGameOver || selectedUnit == null) return;
            actions?.OnFortify?.Invoke(selectedUnit);
            RefreshUnitPanel();
        }

        void OnSkipClicked()
        {
            if (state == null || state.IsGameOver || selectedUnit == null) return;
            actions?.OnSkip?.Invoke(selectedUnit);
            RefreshUnitPanel();
        }

        void OnRestartClicked()
        {
            actions?.OnRestart?.Invoke();
        }

        /// <summary>
        /// ゲーム終了画面の「最終戦況」ボタン(2026-07-21 Claude Code 追加)。
        /// 既存の戦況グラフを開閉する(Show 側が SetAsLastSibling するためオーバーレイより手前)。
        /// </summary>
        void OnFinalScoreGraphClicked()
        {
            if (scoreGraphPanel == null) return;
            scoreGraphPanel.Toggle();
        }

        /// <summary>
        /// ゲーム終了画面の「歴史ツアー」ボタン(2026-07-21 Claude Code 追加)。
        /// ChroniclePanel(独立Canvasの自己起動UI)の歴史ツアーを直接開始する。
        /// 静的入口が内部でインスタンスを検索するため、不在・未構築でも安全(何もしない)。
        /// </summary>
        void OnHistoryTourClicked()
        {
            ChroniclePanel.StartTourIfAvailable();
        }

        void OnSaveClicked()
        {
            if (state == null || state.IsGameOver) return;
            ShowSaveSlotPanel(true);
        }

        void OnLoadClicked()
        {
            if (state == null || state.IsGameOver) return;
            ShowSaveSlotPanel(false);
        }

        /// <summary>スロット選択パネルを開く(開くたびにメタデータを読み直す)。</summary>
        void ShowSaveSlotPanel(bool saveMode)
        {
            if (slotPanel == null) return;
            slotSaveMode = saveMode;
            cityPanel.SetActive(false);
            techPanel.SetActive(false);
            if (civilizationPanel != null) civilizationPanel.SetActive(false);
            if (leaderPanel != null) leaderPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            slotTitleText.text = saveMode ? "セーブするスロットを選択" : "ロードするスロットを選択";
            RefreshSaveSlotList();
            slotPanel.SetActive(true);
            slotPanel.transform.SetAsLastSibling();
        }

        void CloseSaveSlotPanel()
        {
            if (slotPanel != null) slotPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        /// <summary>
        /// 各スロット行の表示(「スロットN　ターン42 ローマ 07/20 18:55」/「スロットN　(空き)」)と
        /// 有効状態を更新する。ロードモードではデータのあるスロットのみ押せる。
        /// </summary>
        void RefreshSaveSlotList()
        {
            for (int i = 0; i < SaveSlotCount; i++)
            {
                if (slotButtons[i] == null || slotLabels[i] == null) continue;
                int slot = i + 1;
                int turn;
                string civNameJa, savedAtIso;
                bool has = SaveLoad.TryReadMeta(SaveSlotPath(slot), out turn, out civNameJa, out savedAtIso);

                string label;
                if (has)
                {
                    label = $"スロット{slot}　ターン{turn}";
                    if (!string.IsNullOrEmpty(civNameJa)) label += " " + civNameJa;
                    string when = FormatSaveTime(savedAtIso);
                    if (!string.IsNullOrEmpty(when)) label += " " + when;
                }
                else
                {
                    label = $"スロット{slot}　(空き)";
                }
                slotLabels[i].text = label;
                slotButtons[i].interactable = slotSaveMode || has;

                // マップサムネイル(2026-07-21 追加): ファイルの更新時刻でキャッシュし、
                // パネルを開いた時と保存直後のみ再解析する(データ無し・破損は非表示)
                UpdateSlotThumbnail(i, SaveSlotPath(slot));
            }
        }

        void OnSlotClicked(int slot)
        {
            if (state == null || state.IsGameOver) return;
            if (slotSaveMode)
            {
                actions?.OnSaveGameSlot?.Invoke(slot);
                RefreshSaveSlotList();   // 保存結果(ターン・日時)を行表示へ即時反映(パネルは開いたまま)
            }
            else
            {
                // ロード成功時は GameBootstrap がUIごと再構築するため、ここでは後処理をしない
                // (失敗時は現行ゲーム続行。ログに「ロードに失敗しました」が出る)
                actions?.OnLoadGameSlot?.Invoke(slot);
            }
        }

        /// <summary>
        /// スロットのセーブファイルパス。ファイル名規約は GameBootstrap.SaveSlotPath と
        /// 同一に保つこと(表示用メタデータの読み取りにのみ使用し、UIからは書き込まない)。
        /// </summary>
        static string SaveSlotPath(int slot)
        {
            return System.IO.Path.Combine(Application.persistentDataPath, $"hexciv_save_slot{slot}.json");
        }

        /// <summary>保存日時(ISO形式)を「07/20 18:55」形式にする。解析できなければ ""。</summary>
        static string FormatSaveTime(string savedAtIso)
        {
            if (string.IsNullOrEmpty(savedAtIso)) return "";
            DateTime dt;
            if (DateTime.TryParse(savedAtIso, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out dt))
                return dt.ToString("MM/dd HH:mm");
            return "";
        }

        // ================= セーブスロットのミニマップサムネイル(2026-07-21 Claude Code 追加) =================

        /// <summary>
        /// スロット行のサムネイルを更新する。セーブファイルの LastWriteTimeUtc をスタンプとして
        /// キャッシュし、変化した時(=パネルを開いた時か保存直後)のみセーブデータを解析して
        /// 描き直す。ファイル無し・破損時はサムネイルを非表示にするだけで例外は決して外へ出さず、
        /// スロットボタンの動作(セーブ/ロード)には一切影響しない。
        /// </summary>
        void UpdateSlotThumbnail(int index, string path)
        {
            var image = slotThumbImages[index];
            if (image == null) return;
            try
            {
                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                {
                    slotThumbStamps[index] = 0;
                    slotThumbValid[index] = false;
                    image.enabled = false;
                    return;
                }

                long stamp = System.IO.File.GetLastWriteTimeUtc(path).Ticks;
                if (stamp == slotThumbStamps[index])
                {
                    // 内容不変: 描画済みならテクスチャを(再Init後の新しいRawImageにも)当て直すだけ。
                    // 破損キャッシュ(valid=false)は同一スタンプの間は再解析しない
                    bool ok = slotThumbValid[index] && slotThumbTextures[index] != null;
                    if (ok) image.texture = slotThumbTextures[index];
                    image.enabled = ok;
                    return;
                }

                slotThumbStamps[index] = stamp;
                slotThumbValid[index] = RenderSlotThumbnail(index, path);
                if (slotThumbValid[index]) image.texture = slotThumbTextures[index];
                image.enabled = slotThumbValid[index];
            }
            catch
            {
                // I/O・解析のいかなる失敗もサムネイル非表示に留める(行の表示・選択は従来どおり)
                slotThumbValid[index] = false;
                image.enabled = false;
            }
        }

        /// <summary>
        /// セーブファイルを SaveLoad の DTO(SaveData)として解析し、地形色+都市ドットの
        /// 小さなマップ画像を 96×56 テクスチャへ描く。GameState は構築しない表示専用の
        /// 読み取りのみで、シミュレーションには一切影響しない。成功時 true。
        /// 色の写像は MinimapPanel と同じ流儀(GameRules.Terrains の地形色、森=暗緑45%合成、
        /// 丘陵=12%減光、領土=所有者色35%合成、都市=白45%合成の明色ドット)を
        /// ローカルに複製したもので、MinimapPanel には依存しない。
        /// </summary>
        bool RenderSlotThumbnail(int index, string path)
        {
            var data = JsonUtility.FromJson<SaveData>(System.IO.File.ReadAllText(path));
            if (data == null || data.version < 1 || data.mapWidth <= 0 || data.mapHeight <= 0)
                return false;
            int w = data.mapWidth;
            int h = data.mapHeight;
            int n = w * h;
            if (data.terrain == null || data.terrain.Length != n) return false;
            bool hasFlags = data.hasForest != null && data.hasForest.Length == n
                && data.hasHill != null && data.hasHill.Length == n;
            bool hasOwners = data.ownerPlayerId != null && data.ownerPlayerId.Length == n;

            // プレイヤーId → 文明色(領土の淡い着色と都市ドット用)
            var playerColors = new Dictionary<int, Color>();
            if (data.players != null)
            {
                for (int i = 0; i < data.players.Count; i++)
                {
                    var pd = data.players[i];
                    if (pd != null) playerColors[pd.id] = new Color(pd.colorR, pd.colorG, pd.colorB, 1f);
                }
            }

            // 1) 地形(最近傍サンプリングで 96×56 へ縮小。マップ縦横比はサムネイルとほぼ同じ)
            var px = new Color32[SlotThumbWidth * SlotThumbHeight];
            for (int y = 0; y < SlotThumbHeight; y++)
            {
                int row = Mathf.Clamp(y * h / SlotThumbHeight, 0, h - 1);
                for (int x = 0; x < SlotThumbWidth; x++)
                {
                    int col = Mathf.Clamp(x * w / SlotThumbWidth, 0, w - 1);
                    int ti = row * w + col;
                    TerrainDef def;
                    Color c = GameRules.Terrains.TryGetValue((TerrainType)data.terrain[ti], out def)
                        ? def.Color
                        : new Color(0.04f, 0.06f, 0.10f);
                    if (hasFlags)
                    {
                        if (data.hasForest[ti]) c = Color.Lerp(c, new Color(0.10f, 0.30f, 0.12f), 0.45f);
                        if (data.hasHill[ti]) c *= 0.88f;
                    }
                    if (hasOwners && data.ownerPlayerId[ti] >= 0)
                    {
                        Color oc;
                        if (playerColors.TryGetValue(data.ownerPlayerId[ti], out oc))
                            c = Color.Lerp(c, oc, 0.35f);
                    }
                    c.a = 1f;
                    px[y * SlotThumbWidth + x] = c;
                }
            }

            // 2) 都市ドット(所有者の明色 3×3px)
            if (data.cities != null)
            {
                for (int i = 0; i < data.cities.Count; i++)
                {
                    var cd = data.cities[i];
                    if (cd == null) continue;
                    Color oc;
                    if (!playerColors.TryGetValue(cd.playerId, out oc)) oc = Color.gray;
                    Color32 bright = Color.Lerp(oc, Color.white, 0.45f);
                    int col, row;
                    cd.coord.ToOffset(out col, out row);
                    if (col < 0 || col >= w || row < 0 || row >= h) continue;
                    int cx = (col * SlotThumbWidth + SlotThumbWidth / 2) / w;
                    int cy = (row * SlotThumbHeight + SlotThumbHeight / 2) / h;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = cy + dy;
                        if (yy < 0 || yy >= SlotThumbHeight) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = cx + dx;
                            if (xx < 0 || xx >= SlotThumbWidth) continue;
                            px[yy * SlotThumbWidth + xx] = bright;
                        }
                    }
                }
            }

            // 3) テクスチャへ転送(サイズ固定のため既存テクスチャを再利用する)
            var tex = slotThumbTextures[index];
            if (tex == null)
            {
                tex = new Texture2D(SlotThumbWidth, SlotThumbHeight, TextureFormat.RGBA32, false);
                tex.name = "save_slot_thumb_" + (index + 1);
                tex.hideFlags = HideFlags.HideAndDontSave;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Point;
                slotThumbTextures[index] = tex;
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return true;
        }

        // ================= ゲーム設定(2026-07-20 Claude Code 追加) =================

        void OnGameSettingsButtonClicked()
        {
            if (state == null || state.IsGameOver) return;
            if (settingsPanel != null && settingsPanel.activeSelf) CloseGameSettingsPanel();
            else ShowGameSettingsPanel();
        }

        /// <summary>ゲーム設定画面を開く(開くたびに保存済み設定を読み直し、シード欄は空にする)。</summary>
        void ShowGameSettingsPanel()
        {
            if (settingsPanel == null) return;
            cityPanel.SetActive(false);
            techPanel.SetActive(false);
            if (civilizationPanel != null) civilizationPanel.SetActive(false);
            if (leaderPanel != null) leaderPanel.SetActive(false);
            if (slotPanel != null) slotPanel.SetActive(false);
            HideTutorial();
            LoadGameSettingsFromPrefs();
            RefreshFxQualityLabel();   // 演出トグルのラベルも保存値へ合わせる(2026-07-22 追加)
            if (seedInput != null) seedInput.text = "";   // 前回シードは保存しない(空欄=ランダム)
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
            RefreshGameSettingsPanel();
        }

        void CloseGameSettingsPanel()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        /// <summary>保存済み設定を読み込む(不正値は既定の標準44×26・4文明・大陸・普通へ丸める)。</summary>
        void LoadGameSettingsFromPrefs()
        {
            settingsMapSizeIndex = Mathf.Clamp(PlayerPrefs.GetInt(MapSizeKey, 1), 0, mapSizeButtons.Length - 1);
            settingsMapType = Mathf.Clamp(PlayerPrefs.GetInt(MapTypeKey, 0), 0, mapTypeButtons.Length - 1);
            settingsDifficulty = Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, 1), 0, difficultyButtons.Length - 1);
            int saved = PlayerPrefs.GetInt(NumPlayersKey, 4);
            settingsNumPlayers = 4;
            for (int i = 0; i < NumPlayersChoices.Length; i++)
                if (NumPlayersChoices[i] == saved) { settingsNumPlayers = saved; break; }
        }

        /// <summary>マップサイズ選択(選択と同時に PlayerPrefs へ保存し、以後の新規ゲームに適用)。</summary>
        void OnMapSizeClicked(int index)
        {
            settingsMapSizeIndex = Mathf.Clamp(index, 0, mapSizeButtons.Length - 1);
            PlayerPrefs.SetInt(MapSizeKey, settingsMapSizeIndex);
            PlayerPrefs.Save();
            RefreshGameSettingsPanel();
        }

        /// <summary>マップ種別選択(選択と同時に PlayerPrefs へ保存し、以後の新規ゲームに適用)。
        /// 2026-07-20 Claude Code 追加。0=大陸/1=パンゲア/2=群島。</summary>
        void OnMapTypeClicked(int index)
        {
            settingsMapType = Mathf.Clamp(index, 0, mapTypeButtons.Length - 1);
            PlayerPrefs.SetInt(MapTypeKey, settingsMapType);
            PlayerPrefs.Save();
            RefreshGameSettingsPanel();
        }

        /// <summary>難易度選択(選択と同時に PlayerPrefs へ保存し、以後の新規ゲームに適用)。
        /// 2026-07-20 Claude Code 追加。0=やさしい/1=普通/2=むずかしい。</summary>
        void OnDifficultyClicked(int index)
        {
            settingsDifficulty = Mathf.Clamp(index, 0, difficultyButtons.Length - 1);
            PlayerPrefs.SetInt(DifficultyKey, settingsDifficulty);
            PlayerPrefs.Save();
            RefreshGameSettingsPanel();
        }

        /// <summary>
        /// 演出モードのトグル(2026-07-22 Claude Code 追加)。標準⇔軽量を切り替えて
        /// PlayerPrefs "HexCiv.FxLight" へ保存し、Rendering/VisualQuality のキャッシュを即時更新する
        /// (雲影・水面ゆらぎ・待機ボブ・ダメージ数字の表示側が次フレームから追従する)。
        /// 表示のみの設定で、シミュレーション結果には一切影響しない。
        /// </summary>
        void OnFxQualityClicked()
        {
            bool light = PlayerPrefs.GetInt(FxLightKey, 0) != 0;
            PlayerPrefs.SetInt(FxLightKey, light ? 0 : 1);
            PlayerPrefs.Save();
            HexCiv.Render.VisualQuality.Refresh();
            RefreshFxQualityLabel();
        }

        /// <summary>演出トグルのラベルを保存値に合わせる(構築時・パネルを開いた時・切替直後に呼ぶ)。</summary>
        void RefreshFxQualityLabel()
        {
            if (fxQualityLabel == null) return;
            fxQualityLabel.text = "演出: " +
                (PlayerPrefs.GetInt(FxLightKey, 0) != 0 ? "軽量" : "標準");
        }

        /// <summary>文明数選択(選択と同時に PlayerPrefs へ保存し、以後の新規ゲームに適用)。</summary>
        void OnNumPlayersClicked(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= NumPlayersChoices.Length) return;
            settingsNumPlayers = NumPlayersChoices[choiceIndex];
            PlayerPrefs.SetInt(NumPlayersKey, settingsNumPlayers);
            PlayerPrefs.Save();
            RefreshGameSettingsPanel();
        }

        /// <summary>現在の選択をボタンの強調表示へ反映する。</summary>
        void RefreshGameSettingsPanel()
        {
            if (settingsPanel == null || !settingsPanel.activeSelf) return;
            for (int i = 0; i < mapSizeButtons.Length; i++)
                SetChoiceHighlight(mapSizeButtons[i], i == settingsMapSizeIndex);
            for (int i = 0; i < mapTypeButtons.Length; i++)
                SetChoiceHighlight(mapTypeButtons[i], i == settingsMapType);
            for (int i = 0; i < difficultyButtons.Length; i++)
                SetChoiceHighlight(difficultyButtons[i], i == settingsDifficulty);
            for (int i = 0; i < numPlayersButtons.Length; i++)
                SetChoiceHighlight(numPlayersButtons[i], NumPlayersChoices[i] == settingsNumPlayers);
        }

        /// <summary>選択中の選択肢ボタンをアクセント色で強調する(文明一覧の選択表示と同じ方式)。</summary>
        static void SetChoiceHighlight(Button b, bool selected)
        {
            if (b == null) return;
            var colors = b.colors;
            colors.normalColor = selected
                ? Color.Lerp(UIStyle.ButtonNormal, UIStyle.Accent, 0.45f)
                : UIStyle.ButtonNormal;
            colors.selectedColor = colors.normalColor;
            b.colors = colors;
        }

        /// <summary>
        /// 「この設定で新しいゲームを開始」。シード欄を解析(空欄・解析不能は0=ランダム)し、
        /// OnNewGameRequested 経由で GameBootstrap が既存の新規ゲーム経路
        /// (BuildNewGame+文明・指導者ロスター+ApplyState)で再構築する。
        /// </summary>
        void OnStartWithSettingsClicked()
        {
            if (state == null || state.IsGameOver) return;
            int seed = 0;
            if (seedInput != null && !string.IsNullOrEmpty(seedInput.text))
            {
                int parsed;
                if (int.TryParse(seedInput.text.Trim(), out parsed)) seed = Mathf.Abs(parsed);
            }
            CloseGameSettingsPanel();
            OnNewGameRequested?.Invoke(seed);
        }

        /// <summary>
        /// 「シミュレーション観戦で開始」(2026-07-20 Claude Code 追加)。
        /// シード解析は通常開始と同一規約(空欄・解析不能は0=ランダム)。
        /// OnSimulationStartRequested 経由で GameBootstrap が HumanPlayerIndex=-1 の
        /// 新規ゲームを構築し、全文明AIの自動進行観戦を開始する。
        /// </summary>
        void OnStartSimulationClicked()
        {
            if (state == null || state.IsGameOver) return;
            int seed = 0;
            if (seedInput != null && !string.IsNullOrEmpty(seedInput.text))
            {
                int parsed;
                if (int.TryParse(seedInput.text.Trim(), out parsed)) seed = Mathf.Abs(parsed);
            }
            CloseGameSettingsPanel();
            OnSimulationStartRequested?.Invoke(seed);
        }

        void ShowCivilizationPanel()
        {
            if (civilizationPanel == null) return;
            cityPanel.SetActive(false);
            techPanel.SetActive(false);
            if (slotPanel != null) slotPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (leaderPanel != null) leaderPanel.SetActive(false);
            HideTutorial();
            civilizationPage = 0;
            civilizationPanel.SetActive(true);
            civilizationPanel.transform.SetAsLastSibling();
            RefreshCivilizationList();
        }

        void CloseCivilizationPanel()
        {
            if (civilizationPanel != null) civilizationPanel.SetActive(false);
        }

        void ShowLeaderPanel()
        {
            if (leaderPanel == null || state == null || state.HumanPlayer == null) return;
            cityPanel.SetActive(false);
            techPanel.SetActive(false);
            if (civilizationPanel != null) civilizationPanel.SetActive(false);
            if (slotPanel != null) slotPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            HideTutorial();
            leaderPage = 0;
            leaderPanel.SetActive(true);
            leaderPanel.transform.SetAsLastSibling();
            RefreshLeaderList();
        }

        void CloseLeaderPanel()
        {
            if (leaderPanel != null) leaderPanel.SetActive(false);
        }

        void ChangeLeaderPage(int delta)
        {
            var p = state != null ? state.HumanPlayer : null;
            var leaders = p != null ? LeaderCatalog.ForCivilization(p.CivilizationId) : new List<LeaderDef>();
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(leaders.Count / (float)LeadersPerPage));
            leaderPage = Mathf.Clamp(leaderPage + delta, 0, totalPages - 1);
            RefreshLeaderList();
        }

        void RefreshLeaderList()
        {
            if (leaderListRoot == null || leaderPanel == null || !leaderPanel.activeSelf) return;
            ClearChildren(leaderListRoot);

            var p = state != null ? state.HumanPlayer : null;
            if (p == null) return;
            var leaders = LeaderCatalog.ForCivilization(p.CivilizationId);
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(leaders.Count / (float)LeadersPerPage));
            leaderPage = Mathf.Clamp(leaderPage, 0, totalPages - 1);
            int start = leaderPage * LeadersPerPage;
            int end = Mathf.Min(leaders.Count, start + LeadersPerPage);
            leaderHeaderText.text = p.NameJa + "の指導者を選択";

            for (int index = start; index < end; index++)
            {
                var leader = leaders[index];
                int row = index - start;
                bool selected = string.Equals(p.LeaderId, leader.Id, StringComparison.OrdinalIgnoreCase);
                string label = (selected ? "● " : "") + leader.NameJa + "　" + leader.TitleJa + "\n" +
                    leader.PeriodJa + "｜" + leader.SummaryJa;
                string selectedId = leader.Id;
                var button = UIStyle.CreateButton(leaderListRoot, "Leader_" + leader.Id, label, 14,
                    () => OnLeaderChosen?.Invoke(selectedId));
                UIStyle.SetRect(button.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -row * 80f), new Vector2(-4f, 70f));

                var colors = button.colors;
                colors.normalColor = selected
                    ? Color.Lerp(UIStyle.ButtonNormal, p.Color, 0.58f)
                    : UIStyle.ButtonNormal;
                colors.selectedColor = colors.normalColor;
                button.colors = colors;
            }

            leaderPageText.text = $"{leaderPage + 1} / {totalPages}　全{leaders.Count}人";
            leaderPrevButton.interactable = leaderPage > 0;
            leaderNextButton.interactable = leaderPage < totalPages - 1;
        }

        void ChangeCivilizationPage(int delta)
        {
            int totalPages = Mathf.Max(1,
                Mathf.CeilToInt(CivilizationCatalog.All.Count / (float)CivilizationsPerPage));
            civilizationPage = Mathf.Clamp(civilizationPage + delta, 0, totalPages - 1);
            RefreshCivilizationList();
        }

        void RefreshCivilizationList()
        {
            if (civilizationListRoot == null || civilizationPanel == null || !civilizationPanel.activeSelf) return;
            ClearChildren(civilizationListRoot);

            var catalog = CivilizationCatalog.All;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(catalog.Count / (float)CivilizationsPerPage));
            civilizationPage = Mathf.Clamp(civilizationPage, 0, totalPages - 1);
            int start = civilizationPage * CivilizationsPerPage;
            int end = Mathf.Min(catalog.Count, start + CivilizationsPerPage);
            string currentId = state != null && state.HumanPlayer != null
                ? state.HumanPlayer.CivilizationId
                : null;

            for (int index = start; index < end; index++)
            {
                var civilization = catalog[index];
                int local = index - start;
                int col = local % 3;
                int row = local / 3;
                bool selected = string.Equals(currentId, civilization.Id, StringComparison.OrdinalIgnoreCase);
                string label = (selected ? "● " : "") + civilization.NameJa + "\n" +
                    civilization.RegionJa + " / " + civilization.EraJa;
                string selectedId = civilization.Id;

                var button = UIStyle.CreateButton(civilizationListRoot,
                    "Civilization_" + civilization.Id, label, 14,
                    () => OnCivilizationChosen?.Invoke(selectedId));
                UIStyle.SetRect(button.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(col * 267f, -row * 101f), new Vector2(252f, 86f));

                var colors = button.colors;
                colors.normalColor = Color.Lerp(UIStyle.ButtonNormal, civilization.Color, selected ? 0.58f : 0.25f);
                colors.selectedColor = colors.normalColor;
                button.colors = colors;
            }

            civilizationPageText.text = $"{civilizationPage + 1} / {totalPages}　全{catalog.Count}文明";
            civilizationPrevButton.interactable = civilizationPage > 0;
            civilizationNextButton.interactable = civilizationPage < totalPages - 1;
        }

        void CycleMusicVolume()
        {
            var audio = GameAudio.Instance;
            if (audio == null) return;
            int step = (Mathf.RoundToInt(audio.MusicVolume * 4f) + 1) % 5;
            audio.SetMusicVolume(step * 0.25f);
            RefreshAudioControls();
        }

        void CycleSfxVolume()
        {
            var audio = GameAudio.Instance;
            if (audio == null) return;
            int step = (Mathf.RoundToInt(audio.SfxVolume * 4f) + 1) % 5;
            audio.SetSfxVolume(step * 0.25f);
            RefreshAudioControls();
        }

        void ToggleAudioMute()
        {
            GameAudio.Instance?.ToggleMute();
            RefreshAudioControls();
        }

        void RefreshAudioControls()
        {
            var audio = GameAudio.Instance;
            if (audio == null || musicVolumeLabel == null || sfxVolumeLabel == null || muteLabel == null) return;
            musicVolumeLabel.text = "BGM：" + Mathf.RoundToInt(audio.MusicVolume * 100f) + "%";
            sfxVolumeLabel.text = "SE：" + Mathf.RoundToInt(audio.SfxVolume * 100f) + "%";
            muteLabel.text = audio.Muted ? "音：OFF" : "音：ON";
        }

        void CloseCityPanel()
        {
            cityPanel.SetActive(false);
            shownCity = null;
        }

        void CloseTechPanel()
        {
            techPanel.SetActive(false);
        }

        // ================= 更新処理 =================

        void RefreshTopBar()
        {
            turnText.text = $"ターン {state.TurnNumber}/{(state.Config != null ? state.Config.MaxTurns : 0)}";

            var p = state.HumanPlayer;
            if (p == null)
            {
                scienceText.text = "";
                researchButtonLabel.text = "観戦モード";
                researchButton.interactable = false;
                civNameText.text = "";
                civSwatch.enabled = false;
            }
            else
            {
                int spt = p.SciencePerTurn(state);
                scienceText.text = $"科学 +{spt}";

                if (!string.IsNullOrEmpty(p.CurrentResearchId))
                {
                    var t = TechnologyCatalog.Get(p.CurrentResearchId);
                    int turns = TurnsFor(t.Cost - p.ScienceStored, spt);
                    researchButtonLabel.text = $"研究中: {t.NameJa} (あと{FormatTurns(turns)})";
                }
                else
                {
                    researchButtonLabel.text = "研究を選択";
                }
                researchButton.interactable = !state.IsGameOver;
                civNameText.text = string.IsNullOrEmpty(p.LeaderNameJa)
                    ? p.NameJa
                    : p.NameJa + "｜" + p.LeaderNameJa;
                civSwatch.enabled = true;
                civSwatch.color = p.Color;
            }

            endTurnButton.interactable = !state.IsGameOver;
            if (nextUnitButton != null) nextUnitButton.interactable = !state.IsGameOver && p != null;
            if (saveButton != null) saveButton.interactable = !state.IsGameOver && p != null;
            if (loadButton != null) loadButton.interactable = !state.IsGameOver;
            if (civilizationButton != null) civilizationButton.interactable = !state.IsGameOver && p != null;
            if (leaderButton != null) leaderButton.interactable = !state.IsGameOver && p != null;
            if (settingsButton != null) settingsButton.interactable = !state.IsGameOver;

            RefreshEraIndicator();   // 時代表示(ターン変化時のみ実更新。2026-07-22 追加)
        }

        void RefreshUnitPanel()
        {
            if (selectedUnit == null || selectedUnit.IsDead)
            {
                selectedUnit = null;
                unitPanel.SetActive(false);
                return;
            }

            unitPanel.SetActive(true);
            var def = selectedUnit.Def;
            unitNameText.text = $"{def.Glyph} {def.NameJa}";

            var lines = new List<string>
            {
                $"HP {selectedUnit.Hp}/{GameRules.UnitMaxHp}",
                $"移動力 {selectedUnit.MovesLeft}/{def.Moves}"
            };
            if (def.IsRanged)
                lines.Add($"戦闘力 {def.Strength} / 遠隔 {def.RangedStrength}(射程{def.Range})");
            else if (def.IsCivilian)
                lines.Add("非戦闘ユニット");
            else
                lines.Add($"戦闘力 {def.Strength}");
            if (selectedUnit.Fortified)
                lines.Add("防御態勢中");
            else if (selectedUnit.GotoPath != null && selectedUnit.GotoPath.Count > 0)
                lines.Add("移動命令実行中");
            unitStatsText.text = string.Join("\n", lines);

            bool isSettler = selectedUnit.DefId == "settler";
            foundCityButton.gameObject.SetActive(isSettler);
            if (isSettler)
            {
                var owner = state.GetPlayer(selectedUnit.PlayerId);
                foundCityButton.interactable = !state.IsGameOver
                    && owner != null
                    && state.CanFoundCityAt(owner, selectedUnit.Coord);
            }
            fortifyButton.interactable = !state.IsGameOver && !selectedUnit.Fortified;
            skipButton.interactable = !state.IsGameOver;
        }

        void RefreshCityPanel()
        {
            if (shownCity == null || !cityPanel.activeSelf) return;

            // 都市が消滅・占領された場合は閉じる
            var tile = state.Map != null ? state.Map.Get(shownCity.Coord) : null;
            var human = state.HumanPlayer;
            bool valid = tile != null && tile.City == shownCity
                && (human == null || shownCity.PlayerId == human.Id);
            if (!valid)
            {
                CloseCityPanel();
                return;
            }

            cityNameText.text = shownCity.NameJa;
            cityStatsText.text = BuildCityStats(shownCity);

            if (cityListVersion != state.Version)
            {
                cityListVersion = state.Version;
                RebuildProductionList();
            }
        }

        string BuildCityStats(City c)
        {
            var y = c.ComputeYields(state);
            var lines = new List<string>
            {
                $"人口 {c.Population}(成長まで {FormatTurns(c.TurnsToGrow(state))})",
                $"産出: 食料{y.Food} 生産{y.Production} 科学{y.Science}"
            };
            if (c.Hp < c.MaxHp) lines.Add($"HP {c.Hp}/{c.MaxHp}");
            lines.Add($"防御力 {c.DefenseStrength(state)}");

            if (c.CurrentProduction != null)
            {
                lines.Add($"生産中: {c.CurrentProduction.NameJa} "
                    + $"{c.ProductionStored}/{c.CurrentProduction.Cost}(あと{FormatTurns(c.TurnsToComplete(state))})");
            }
            else
            {
                lines.Add("生産中: なし");
            }

            if (c.Buildings.Count > 0)
            {
                var names = new List<string>();
                for (int i = 0; i < c.Buildings.Count; i++)
                    names.Add(GameRules.GetBuilding(c.Buildings[i]).NameJa);
                lines.Add("建物: " + string.Join("、", names));
            }
            else
            {
                lines.Add("建物: なし");
            }
            return string.Join("\n", lines);
        }

        void RebuildProductionList()
        {
            ClearChildren(productionListRoot);
            var city = shownCity;
            if (city == null) return;

            var items = city.AvailableProduction(state);
            int prod = city.ComputeYields(state).Production;
            // ユニットチップの着色用(都市所有者の文明色。2026-07-21 Claude Code 追加)
            var chipOwner = state.GetPlayer(city.PlayerId);
            var chipOwnerColor = chipOwner != null ? chipOwner.Color : UIStyle.Accent;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int turns = TurnsFor(item.Cost - city.ProductionStored, prod);
                bool current = city.CurrentProduction != null
                    && city.CurrentProduction.Kind == item.Kind
                    && city.CurrentProduction.Id == item.Id;
                string label = (current ? "▶ " : "") + $"{item.NameJa} ({FormatTurns(turns)})";

                var capturedItem = item;
                var capturedCity = city;
                var b = UIStyle.CreateButton(productionListRoot, "Prod_" + item.Id, label, 14, () =>
                {
                    if (state == null || state.IsGameOver) return;
                    actions?.OnChooseProduction?.Invoke(capturedCity, capturedItem);
                    cityListVersion = -1;   // 次回更新で再構築(▶ 位置の反映)
                    RefreshCityPanel();
                });
                UIStyle.SetRect(b.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -i * 30f), new Vector2(0f, 28f));
                AddProductionChip(b, item, chipOwnerColor);   // 種類別アイコン(2026-07-21 追加)
            }
        }

        // ================= 生産リストのアイコンチップ(2026-07-21 Claude Code 追加) =================

        /// <summary>ユニット用の円チップSprite(白で生成し、Image.color で文明色に着色する)。</summary>
        static Sprite unitChipSprite;
        /// <summary>建物用の正方形チップSprite(白で生成し、Image.color で着色する)。</summary>
        static Sprite buildingChipSprite;
        /// <summary>建物チップの色(石材風の落ち着いたグレー。文明色の円=ユニットと見分けやすい)。</summary>
        static readonly Color BuildingChipColor = new Color(0.60f, 0.58f, 0.52f, 1f);

        /// <summary>
        /// 生産ボタンのラベル左へ種類別アイコンチップを添える。
        /// ユニット=文明色の円+Glyph1文字(EntityRenderer のユニット表示と同じ記号)、
        /// 建物=石材色の正方形。UIStyle.AddButtonIcon と同じ「子Imageの追加+ラベル左端の
        /// 押し出し」方式で、ボタンのサイズ・位置・onClick には一切触れない
        /// (既存のボタン内余白 28px 高に収まる 20px チップ)。
        /// </summary>
        void AddProductionChip(Button button, ProductionItem item, Color ownerColor)
        {
            if (button == null || item == null) return;
            const float chipSize = 20f;
            const float leftPadding = 5f;

            // ラベル参照はチップ(内部にTextを持つ)を追加する前に取得する
            var label = UIStyle.ButtonLabel(button);

            var go = new GameObject("ProdChip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(button.transform, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            UIStyle.SetRect(go, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(leftPadding, 0f), new Vector2(chipSize, chipSize));

            if (item.Kind == ProductionKind.Unit)
            {
                img.sprite = UnitChipSprite();
                img.color = ownerColor;
                var def = GameRules.GetUnit(item.Id);
                string glyphText = def != null && !string.IsNullOrEmpty(def.Glyph) ? def.Glyph : "?";
                var glyph = UIStyle.CreateText(go.transform, "Glyph", glyphText, 12,
                    TextAnchor.MiddleCenter, Color.white);
                UIStyle.StretchFull(glyph.gameObject);
                var shadow = glyph.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                shadow.effectDistance = new Vector2(1f, -1f);
            }
            else
            {
                img.sprite = BuildingChipSprite();
                img.color = BuildingChipColor;
            }

            if (label != null)
            {
                var lrt = (RectTransform)label.transform;
                float minLeft = leftPadding + chipSize + 4f;
                if (lrt.offsetMin.x < minLeft)
                    lrt.offsetMin = new Vector2(minLeft, lrt.offsetMin.y);
            }
        }

        static Sprite UnitChipSprite()
        {
            if (unitChipSprite == null) unitChipSprite = BuildChipSprite(true);
            return unitChipSprite;
        }

        static Sprite BuildingChipSprite()
        {
            if (buildingChipSprite == null) buildingChipSprite = BuildChipSprite(false);
            return buildingChipSprite;
        }

        /// <summary>
        /// チップSpriteを白色で手続き生成する(24px。内部96pxで描いて4x4アルファ平均縮小=
        /// アンチエイリアス。UIStyle.BuildIconSprite と同じ方式のミニ版。実行中はキャッシュ)。
        /// 白で生成するため、実際の色は Image.color の乗算で自由に着色できる。
        /// </summary>
        static Sprite BuildChipSprite(bool circle)
        {
            const int size = 24;
            const int ss = 4;
            const int big = size * ss;
            var buf = new float[big * big];   // アルファのみ(形状マスク)

            if (circle)
            {
                float c = (big - 1) * 0.5f;
                float r = big * 0.48f;
                float r2 = r * r;
                for (int y = 0; y < big; y++)
                {
                    for (int x = 0; x < big; x++)
                    {
                        float dx = x - c, dy = y - c;
                        if (dx * dx + dy * dy <= r2) buf[y * big + x] = 1f;
                    }
                }
            }
            else
            {
                int m = Mathf.RoundToInt(big * 0.08f);
                for (int y = m; y < big - m; y++)
                    for (int x = m; x < big - m; x++)
                        buf[y * big + x] = 1f;
            }

            var outPx = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = 0f;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        int row = (y * ss + sy) * big + x * ss;
                        for (int sx = 0; sx < ss; sx++) a += buf[row + sx];
                    }
                    outPx[y * size + x] = new Color(1f, 1f, 1f, a / (ss * ss));
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = circle ? "ui_chip_unit" : "ui_chip_building";
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(outPx);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ================= トップバーボタンのアイコン(2026-07-21 Claude Code 追加) =================

        /// <summary>トップバー用アイコンSpriteのキャッシュ(アプリ実行中は生成済みを使い回す)。</summary>
        static readonly Dictionary<string, Sprite> topBarIconCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// UIStyle.AddButtonIcon と同じ「子Imageの追加+ラベル左オフセットの押し出し」方式で、
        /// UIManager 内製のアイコン(gear=歯車/globe=地球/crown=王冠/flask=フラスコ/graph=折れ線)を
        /// 既存ボタンのラベル左へ添える。UIStyle.cs は共有ヘルパーのため今回は変更せず、
        /// 新種アイコンの描画は本クラス内で完結させる。ボタンのサイズ・位置・onClick は不変。
        /// 子オブジェクト名は UIStyle 側と同じ "ButtonIcon"(二重付与チェックを共有・冪等)。
        /// </summary>
        static void AddTopBarIcon(Button button, string kind, float size = 18f, float leftPadding = 4f)
        {
            if (button == null) return;
            if (button.transform.Find("ButtonIcon") != null) return;
            var sprite = TopBarIconSprite(kind);
            if (sprite == null) return;

            var go = new GameObject("ButtonIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(button.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;
            UIStyle.SetRect(go, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(leftPadding, 0f), new Vector2(size, size));

            var label = UIStyle.ButtonLabel(button);   // アイコンはTextを持たないため取得順は不問
            if (label != null)
            {
                var lrt = (RectTransform)label.transform;
                float minLeft = leftPadding + size + 2f;
                if (lrt.offsetMin.x < minLeft)
                    lrt.offsetMin = new Vector2(minLeft, lrt.offsetMin.y);
            }
        }

        static Sprite TopBarIconSprite(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            Sprite cached;
            if (topBarIconCache.TryGetValue(kind, out cached) && cached != null) return cached;
            var sprite = BuildTopBarIconSprite(kind);
            topBarIconCache[kind] = sprite;
            return sprite;
        }

        /// <summary>
        /// アイコンを 24px(内部96pxをピクセル判定で描き、4x4アルファ加重平均で縮小=
        /// アンチエイリアス)で生成する。UIStyle.BuildIconSprite/BuildChipSprite と同じ縮小方式。
        /// </summary>
        static Sprite BuildTopBarIconSprite(string kind)
        {
            const int size = 24;
            const int ss = 4;
            const int big = size * ss;

            var shader = TopBarIconShader(kind);
            if (shader == null) return null;

            var buf = new Color[big * big];
            for (int y = 0; y < big; y++)
            {
                float v = (y + 0.5f) / big;   // y上向き(SetPixels の行0=下端)
                for (int x = 0; x < big; x++)
                {
                    float u = (x + 0.5f) / big;
                    var c = shader(u, v);
                    if (c.a > 0f) buf[y * big + x] = c;
                }
            }

            var outPx = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        int row = (y * ss + sy) * big + x * ss;
                        for (int sx = 0; sx < ss; sx++)
                        {
                            var c = buf[row + sx];
                            r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                        }
                    }
                    outPx[y * size + x] = a > 0f
                        ? new Color(r / a, g / a, b / a, a / (ss * ss))
                        : Color.clear;
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "ui_topbar_icon_" + kind;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(outPx);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 種類別のピクセル判定関数(引数は正規化座標 u,v∈0..1、y上向き。返り値のα>0で塗る)。
        /// 未知の種類は null。関数は Sprite 生成時に一度だけ全ピクセルへ評価され、結果はキャッシュされる。
        /// </summary>
        static Func<float, float, Color> TopBarIconShader(string kind)
        {
            var ivory = new Color(0.93f, 0.91f, 0.83f, 1f);
            var gold = UIStyle.Accent;
            switch (kind)
            {
                case "gear":   // 歯車(8枚歯+中央穴。ゲーム設定)
                    return (u, v) =>
                    {
                        float dx = u - 0.5f, dy = v - 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d >= 0.14f && d <= 0.32f) return ivory;                  // 本体リング
                        if (d > 0.26f && d <= 0.46f)
                        {
                            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                            float near = Mathf.Abs(Mathf.DeltaAngle(ang, Mathf.Round(ang / 45f) * 45f));
                            if (near <= 11f) return ivory;                           // 歯(45°ごと)
                        }
                        return Color.clear;
                    };
                case "globe":  // 地球(輪郭円+赤道+子午線。文明変更)
                    return (u, v) =>
                    {
                        float dx = u - 0.5f, dy = v - 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        const float R = 0.40f;
                        if (Mathf.Abs(d - R) <= 0.045f) return gold;                 // 輪郭
                        if (d <= R)
                        {
                            if (Mathf.Abs(dy) <= 0.032f) return gold;                // 赤道
                            if (Mathf.Abs(dx) <= 0.032f) return gold;                // 中央子午線
                            float f = Mathf.Sqrt((dx / 0.20f) * (dx / 0.20f) + (dy / R) * (dy / R));
                            if (Mathf.Abs(f - 1f) * 0.20f <= 0.032f) return gold;    // 楕円子午線
                        }
                        return Color.clear;
                    };
                case "crown":  // 王冠(帯+3つの山。指導者変更)
                {
                    var left = new[] { IconV(0.18f, 0.30f), IconV(0.26f, 0.74f), IconV(0.40f, 0.30f) };
                    var mid = new[] { IconV(0.38f, 0.30f), IconV(0.50f, 0.88f), IconV(0.62f, 0.30f) };
                    var right = new[] { IconV(0.60f, 0.30f), IconV(0.74f, 0.74f), IconV(0.82f, 0.30f) };
                    return (u, v) =>
                    {
                        if (u >= 0.18f && u <= 0.82f && v >= 0.14f && v <= 0.32f) return gold;   // 帯
                        if (IconPointInPoly(left, u, v) || IconPointInPoly(mid, u, v) ||
                            IconPointInPoly(right, u, v)) return gold;
                        return Color.clear;
                    };
                }
                case "flask":  // 三角フラスコ+液体(研究を選択/研究中)
                {
                    var body = new[] { IconV(0.18f, 0.14f), IconV(0.82f, 0.14f), IconV(0.56f, 0.62f), IconV(0.44f, 0.62f) };
                    var liquid = new[] { IconV(0.18f, 0.14f), IconV(0.82f, 0.14f), IconV(0.712f, 0.34f), IconV(0.288f, 0.34f) };
                    return (u, v) =>
                    {
                        if (IconPointInPoly(liquid, u, v)) return gold;              // 液体
                        if (IconPointInPoly(body, u, v)) return ivory;               // 本体
                        if (u >= 0.44f && u <= 0.56f && v >= 0.60f && v <= 0.88f) return ivory;  // 首
                        if (u >= 0.38f && u <= 0.62f && v >= 0.86f && v <= 0.94f) return ivory;  // 口
                        return Color.clear;
                    };
                }
                case "graph":  // 折れ線グラフ(軸+3セグメント。戦況)
                {
                    var pts = new[] { IconV(0.26f, 0.34f), IconV(0.44f, 0.66f), IconV(0.60f, 0.42f), IconV(0.86f, 0.80f) };
                    return (u, v) =>
                    {
                        for (int i = 0; i + 1 < pts.Length; i++)
                            if (IconDistToSegment(u, v, pts[i], pts[i + 1]) <= 0.05f) return gold;  // 折れ線
                        if (u >= 0.12f && u <= 0.19f && v >= 0.12f && v <= 0.90f) return ivory;     // 縦軸
                        if (u >= 0.12f && u <= 0.90f && v >= 0.12f && v <= 0.19f) return ivory;     // 横軸
                        return Color.clear;
                    };
                }
                default:
                    return null;
            }
        }

        /// <summary>正規化座標のVector2省略記法(アイコン描画用)。</summary>
        static Vector2 IconV(float x, float y) => new Vector2(x, y);

        /// <summary>点が多角形の内側か(UIStyle.PointInPoly と同一の交差数判定。private のため複製)。</summary>
        static bool IconPointInPoly(Vector2[] poly, float x, float y)
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

        /// <summary>点から線分への最短距離(折れ線アイコン用)。</summary>
        static float IconDistToSegment(float px, float py, Vector2 a, Vector2 b)
        {
            float abx = b.x - a.x, aby = b.y - a.y;
            float apx = px - a.x, apy = py - a.y;
            float len2 = abx * abx + aby * aby;
            float t = len2 > 0f ? Mathf.Clamp01((apx * abx + apy * aby) / len2) : 0f;
            float cx = a.x + abx * t - px, cy = a.y + aby * t - py;
            return Mathf.Sqrt(cx * cx + cy * cy);
        }

        void RefreshTechPanelIfNeeded()
        {
            if (!techPanel.activeSelf) return;
            var p = state.HumanPlayer;
            if (p == null)
            {
                techPanel.SetActive(false);
                return;
            }
            if (techListVersion == state.Version) return;
            techListVersion = state.Version;

            int spt = p.SciencePerTurn(state);
            techSubtitleText.text = $"蓄積科学 {p.ScienceStored} / 毎ターン +{spt}";

            ClearChildren(techListRoot);
            var techs = p.AvailableTechs();
            if (techs.Count == 0)
            {
                var none = UIStyle.CreateText(techListRoot, "NoTech", "研究できる技術がない", 15,
                    TextAnchor.MiddleCenter, UIStyle.TextDim);
                UIStyle.SetRect(none.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(0f, 30f));
                return;
            }

            for (int i = 0; i < techs.Count; i++)
            {
                var t = techs[i];
                int turns = TurnsFor(t.Cost - p.ScienceStored, spt);
                bool current = t.Id == p.CurrentResearchId;
                string label = (current ? "▶ " : "")
                    + $"{t.NameJa} コスト{t.Cost} ({FormatTurns(turns)}) — {t.DescJa}";

                var techId = t.Id;
                var b = UIStyle.CreateButton(techListRoot, "Tech_" + t.Id, label, 15, () =>
                {
                    if (state == null || state.IsGameOver) return;
                    actions?.OnChooseResearch?.Invoke(techId);
                    techPanel.SetActive(false);
                });
                UIStyle.SetRect(b.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -i * 47f), new Vector2(0f, 44f));
            }
        }

        // ================= 内部ヘルパー =================

        void PositionTooltip()
        {
            if (canvasRect == null || tooltipRect == null) return;

            Vector2 lp;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, null, out lp);

            Vector2 pos = lp + new Vector2(18f, -14f);
            Vector2 half = canvasRect.rect.size * 0.5f;
            float w = tooltipRect.sizeDelta.x;
            float h = tooltipRect.sizeDelta.y;

            if (pos.x + w > half.x - 4f) pos.x = lp.x - w - 18f;
            if (pos.y - h < -half.y + 4f) pos.y = lp.y + h + 14f;

            tooltipRect.anchoredPosition = pos;
        }

        static void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        /// <summary>残りコストと毎ターン産出からターン数を求める(産出0なら99)。</summary>
        static int TurnsFor(int remaining, int perTurn)
        {
            if (remaining <= 0) return 1;
            if (perTurn <= 0) return 99;
            return Math.Min(99, (remaining + perTurn - 1) / perTurn);
        }

        static string FormatTurns(int t) => t >= 99 ? "—" : t + "T";
    }
}
