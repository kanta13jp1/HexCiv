using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Audio;
using HexCiv.Core;

namespace HexCiv.UI
{
    /// <summary>
    /// 領土の変遷(タイムラプス)パネル(2026-07-22 Claude Code 追加)。
    /// 毎ターンのタイル領有をコンパクトに記録し、帝国の拡大を早送りで再生する独立Canvas UI。
    ///
    /// ■ 位置づけ
    /// MinimapPanel / ChroniclePanel と同じ「自己起動する独立Canvas」方式。UIManager も
    /// GameBootstrap も Core/ も一切変更しない。読み取り窓口は TurnManager 構築時に
    /// Bind される CultureSystem.CurrentState で、これにより新規開始・リスタート・
    /// ロード・文明変更・観戦モードのすべての経路へ自動追従する。
    /// シミュレーションへは完全に読み取り専用(state.Rng も一切使わない)。
    ///
    /// ■ 記録(Recording)
    /// state.TurnNumber の変化を検知して「1ターンにつき1回だけ」マップ全体を1パスし、
    /// タイルごとの所有者を1バイト(0=無所属、それ以外=state.Players 内の索引+1)へ
    /// 詰めた byte[width*height] を1標本として保持する。標本ごとにターン番号、
    /// キャプション文字列、文明別の都市数(byte[])も併せて持つ。
    ///   - 上限は MaxSamples(250)。超えたら「1つおきに間引いて刻み幅を倍にする」
    ///     (最古を捨てるのではなく全期間をカバーし続ける)。捨てたバッファはプールへ
    ///     戻して再利用するため、長期戦でも追加のアロケーションは発生しない。
    ///   - 56x34 マップなら 1標本 1904B、250標本で約 0.5MB(目標の10MBを大きく下回る)。
    ///   - 新規/ロード/文明変更(state オブジェクトの差し替え)、マップ寸法の変化、
    ///     ターン番号の巻き戻り(同一オブジェクトへのロード)を検知して記録をやり直す。
    ///   - 霧は適用しない(戦後に振り返る「歴史地図」として全領有を記録する)。
    ///
    /// ■ 再生(Playback)
    /// 中央パネルに Texture2D のマップ表示(1タイル 3x3px、odd-r の奇数行は1px右へ)。
    /// 地形は彩度を落としたグレースケールを土台とし、その上に所有者色を強く乗せるため、
    /// 国境が明確に読める。キャプションは「ターン N」、その下に文明色の凡例と都市数。
    /// 操作は ▶再生 / ⏸一時停止 / ⏮先頭へ、速度切替(1x/2x/4x = 6/12/24 fps、非スケール
    /// 時間基準なので観戦の8倍速でもテンポは一定)、および uGUI Slider のスクラブ
    /// (ドラッグすると一時停止して任意の記録ターンへジャンプする)。
    /// 末尾まで再生したら**先頭へループする**(短い記録でも変遷が繰り返し読めるため)。
    /// Esc または × で閉じる。
    /// 再生中のテキストは「値が変わった時だけ」書き換え、キャプションは記録時に
    /// 生成済みの文字列を使うため、定常再生中の GC アロケーションは発生しない。
    ///
    /// ■ 入口
    /// 左下パネルボタン群(y=176 の段は世界史図鑑〜政治で埋まっている)の一段上、
    /// y=216 に「変遷」ボタンを自前のCanvasへ置く(UIManager は読み取りのみで未変更)。
    /// 加えて public static StartPlaybackIfAvailable() を公開する。ゲーム終了画面から
    /// 呼ばれることを想定し、パネルを開いてターン1から再生を始める。標本が
    /// MinPlayableSamples(3)未満のときはパネル上に短い案内を出すだけで何もしない。
    ///
    /// ■ Z順
    /// Canvas sortingOrder=146。ゲームオーバーのオーバーレイ(UIManager のメインCanvas
    /// =100)、Codexの独立パネル群(130/135/140)、年表(145)より手前なので、終了画面の
    /// 上からでも開ける。常時表示の「変遷」ボタンだけは ChroniclePanel と同じ規約で
    /// ネストCanvas(overrideSorting, sortingOrder=-5)へ退避し、開いているモーダル
    /// パネルの上に浮かないようにする。
    ///
    /// ■ ヘッドレス安全性
    /// HumanPlayer が居ない(観戦・スモークテスト)状態でも一切参照しない。Texture2D は
    /// 初回表示時にだけ生成する(記録だけなら描画資源を作らない)ため、-batchmode でも
    /// 例外なく動作する。GameAudio / UIManager への参照はすべて null 安全。
    /// </summary>
    public sealed class TimelapsePanel : MonoBehaviour
    {
        // ---- 記録 ----
        /// <summary>保持する標本の上限。超えたら1つおきに間引いて刻み幅を倍にする。</summary>
        const int MaxSamples = 250;
        /// <summary>再生に必要な最小標本数(これ未満なら案内だけ出す)。</summary>
        const int MinPlayableSamples = 3;

        // ---- 描画 ----
        /// <summary>1タイルの1辺のピクセル数。</summary>
        const int PixelsPerTile = 3;
        /// <summary>マップ表示に割り当てる最大の幅(px、参照解像度1280x720基準)。</summary>
        const float MapAreaWidth = 616f;
        /// <summary>マップ表示に割り当てる最大の高さ(px)。</summary>
        const float MapAreaHeight = 318f;
        /// <summary>凡例の最大表示文明数(4列×2行)。</summary>
        const int MaxLegendEntries = 8;

        /// <summary>速度段階ごとの再生フレーム毎秒(1x/2x/4x)。</summary>
        static readonly float[] SpeedFps = { 6f, 12f, 24f };
        /// <summary>速度段階ごとのボタン表示(文字列を毎回作らないよう定数化)。</summary>
        static readonly string[] SpeedLabels = { "速度 1x", "速度 2x", "速度 4x" };

        /// <summary>マップ外・未使用ピクセルの背景色(カメラ背景と揃えた暗い紺)。</summary>
        static readonly Color32 BackgroundColor = new Color32(10, 14, 26, 255);

        // ==================================================================
        // 記録状態
        // ==================================================================

        /// <summary>標本本体(1バイト=所有者索引+1、長さは width*height)。</summary>
        readonly List<byte[]> samples = new List<byte[]>();
        /// <summary>標本ごとのターン番号。</summary>
        readonly List<int> sampleTurns = new List<int>();
        /// <summary>標本ごとの文明別都市数(長さ=プレイヤー数、255で頭打ち)。</summary>
        readonly List<byte[]> sampleCities = new List<byte[]>();
        /// <summary>標本ごとのキャプション「ターン N」(記録時に1回だけ生成し再生中は再利用)。</summary>
        readonly List<string> sampleCaptions = new List<string>();

        /// <summary>間引きで空いた領有バッファの再利用プール。</summary>
        readonly List<byte[]> ownerBufferPool = new List<byte[]>();
        /// <summary>間引きで空いた都市数バッファの再利用プール。</summary>
        readonly List<byte[]> cityBufferPool = new List<byte[]>();

        /// <summary>現在記録中の状態(差し替え検知に使う)。</summary>
        GameState recordedState;
        /// <summary>記録中のマップ寸法(寸法が変わったら記録をやり直す)。</summary>
        int recordedWidth, recordedHeight;
        /// <summary>最後に観測したターン番号(-1=未観測)。</summary>
        int lastObservedTurn = -1;
        /// <summary>最初に記録したターン番号(刻み幅の基準)。</summary>
        int firstSampledTurn = 1;
        /// <summary>現在の記録刻み(1=毎ターン。間引きのたびに倍化する)。</summary>
        int sampleStride = 1;
        /// <summary>プレイヤーId→索引の対応表(Idが索引と一致しない保存形式でも安全に引ける)。</summary>
        int[] playerIndexById;
        /// <summary>記録中のプレイヤー数。</summary>
        int playerCount;

        /// <summary>地形のグレースケール土台(タイルごとに1バイト、地形は不変なので1回だけ計算)。</summary>
        byte[] terrainGray;
        /// <summary>terrainGray を計算した対象マップ(参照が変わったら作り直す)。</summary>
        HexMap grayMap;
        /// <summary>文明色(索引=プレイヤー索引)。</summary>
        Color[] ownerColors = new Color[0];
        /// <summary>凡例の見出し(「文明名 都市」まで。数値だけを後ろに足す)。</summary>
        string[] legendPrefixes = new string[0];

        // ==================================================================
        // 再生状態
        // ==================================================================

        bool playing;
        int frameIndex;
        int speedIndex;
        /// <summary>次のコマを表示してよい時刻(Time.unscaledTime 基準)。</summary>
        float nextFrameAt;
        /// <summary>スライダーへプログラムから値を書く間だけ true(コールバックの再入防止)。</summary>
        bool suppressScrub;
        /// <summary>スライダーへ設定済みの最大値(標本が増えた時だけ更新する)。</summary>
        int sliderMaxSet = -1;
        /// <summary>現在キャプションに表示中の標本索引(-1=未表示)。</summary>
        int shownCaptionIndex = -1;
        /// <summary>右下ラベルに表示中の記録件数(件数が変わった時だけ文字列を作り直す)。</summary>
        int shownSampleCount = -1;
        /// <summary>凡例に表示中の都市数(変化時のみ文字列を作り直す)。</summary>
        readonly int[] shownLegendCounts = new int[MaxLegendEntries];

        // ==================================================================
        // UI
        // ==================================================================

        Canvas canvas;
        GameObject panel;
        RawImage mapImage;
        RectTransform mapImageRect;
        Texture2D texture;
        Color32[] buffer;
        int texW, texH;
        int mapW, mapH;

        Text captionText;
        GameObject noticeRoot;
        Text noticeText;
        Text frameLabel;
        Button speedButton;
        Text speedLabel;
        Slider scrubSlider;

        readonly GameObject[] legendRoots = new GameObject[MaxLegendEntries];
        readonly Image[] legendSwatches = new Image[MaxLegendEntries];
        readonly Text[] legendTexts = new Text[MaxLegendEntries];

        /// <summary>UIManager のモーダル計数へ通知済みか(開閉を必ず対で通知する)。</summary>
        bool externalPanelNotified;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<TimelapsePanel>() != null) return;
            new GameObject("TimelapseUI").AddComponent<TimelapsePanel>();
        }

        void Start()
        {
            BuildCanvas();
            BuildOpenButton();
            BuildPanel();
        }

        void Update()
        {
            // 1) 記録(パネルを開いていなくても常に走る。仕事はターンが変わった時だけ)
            UpdateRecording();

            // 2) モーダル退避通知(開閉の全経路をポーリングで対称に捕捉)
            SyncModalNotify();

            if (panel == null || !panel.activeSelf) return;

            // 3) Esc で閉じる(他の独立パネルと同じく自前で処理)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
                return;
            }

            // 4) 再生(非スケール時間基準。定常再生中のアロケーションは無い)
            UpdatePlayback();
        }

        void OnDestroy()
        {
            if (texture != null) Destroy(texture);
            if (externalPanelNotified)
            {
                externalPanelNotified = false;
                UIManager.NotifyExternalPanel(false);
            }
        }

        /// <summary>
        /// パネルの表示状態を UIManager のモーダル計数へ反映する(ChroniclePanel と同じ契約)。
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
        // 記録
        // ==================================================================

        /// <summary>
        /// 状態の差し替え・巻き戻り・寸法変化を検知して記録を初期化し、
        /// ターンが進んでいれば1標本だけ取る(1ターンにつき1パス)。
        /// </summary>
        void UpdateRecording()
        {
            var state = CultureSystem.CurrentState;
            if (state == null || state.Map == null)
            {
                if (recordedState != null) ResetRecording(null);
                return;
            }

            var map = state.Map;
            bool fresh = state != recordedState
                || map.Width != recordedWidth || map.Height != recordedHeight
                || state.TurnNumber < lastObservedTurn;   // 同一オブジェクトへのロード(巻き戻り)
            if (fresh)
            {
                ResetRecording(state);
                CaptureSample(state);   // ターン1(またはロード直後の現在ターン)を必ず記録する
                return;
            }

            if (state.TurnNumber == lastObservedTurn) return;   // 同一ターン中は何もしない
            lastObservedTurn = state.TurnNumber;
            if (((state.TurnNumber - firstSampledTurn) % sampleStride) != 0) return;
            CaptureSample(state);
        }

        /// <summary>記録をすべて破棄して新しい状態へ張り替える(バッファはプールへ戻す)。</summary>
        void ResetRecording(GameState state)
        {
            for (int i = 0; i < samples.Count; i++) ownerBufferPool.Add(samples[i]);
            for (int i = 0; i < sampleCities.Count; i++) cityBufferPool.Add(sampleCities[i]);
            samples.Clear();
            sampleTurns.Clear();
            sampleCities.Clear();
            sampleCaptions.Clear();

            recordedState = state;
            recordedWidth = state != null && state.Map != null ? state.Map.Width : 0;
            recordedHeight = state != null && state.Map != null ? state.Map.Height : 0;
            lastObservedTurn = state != null ? state.TurnNumber : -1;
            firstSampledTurn = state != null ? state.TurnNumber : 1;
            sampleStride = 1;

            // マップ寸法が変わったらプールの中身は使えないので捨てる
            int need = recordedWidth * recordedHeight;
            for (int i = ownerBufferPool.Count - 1; i >= 0; i--)
                if (ownerBufferPool[i] == null || ownerBufferPool[i].Length != need)
                    ownerBufferPool.RemoveAt(i);

            BindPlayers(state);
            BuildTerrainGray(state);

            // 再生中だった場合は止めて表示を初期化する
            playing = false;
            frameIndex = 0;
            shownCaptionIndex = -1;
            shownSampleCount = -1;
            sliderMaxSet = -1;
            for (int i = 0; i < MaxLegendEntries; i++) shownLegendCounts[i] = -1;
            if (panel != null && panel.activeSelf) RefreshWhenNoFrames();
        }

        /// <summary>プレイヤーId→索引の対応表、文明色、凡例見出しを作る(状態の張り替え時のみ)。</summary>
        void BindPlayers(GameState state)
        {
            playerCount = state != null && state.Players != null ? state.Players.Count : 0;
            if (ownerColors.Length != playerCount) ownerColors = new Color[playerCount];
            if (legendPrefixes.Length != playerCount) legendPrefixes = new string[playerCount];

            int maxId = 0;
            for (int i = 0; i < playerCount; i++)
            {
                var p = state.Players[i];
                if (p != null && p.Id > maxId) maxId = p.Id;
            }
            if (playerIndexById == null || playerIndexById.Length < maxId + 1)
                playerIndexById = new int[maxId + 1];
            for (int i = 0; i < playerIndexById.Length; i++) playerIndexById[i] = -1;

            for (int i = 0; i < playerCount; i++)
            {
                var p = state.Players[i];
                if (p == null) continue;
                if (p.Id >= 0 && p.Id < playerIndexById.Length) playerIndexById[p.Id] = i;
                ownerColors[i] = p.Color;
                legendPrefixes[i] = (string.IsNullOrEmpty(p.NameJa) ? "?" : p.NameJa) + " 都市";
            }
            ApplyLegendIdentity();
        }

        /// <summary>
        /// 地形のグレースケール土台を1回だけ計算する。地形は対局中に変化しないため、
        /// 再生時は「グレー土台 + 所有者色」の合成だけで済む(毎コマのタイル走査が軽い)。
        /// </summary>
        void BuildTerrainGray(GameState state)
        {
            if (state == null || state.Map == null)
            {
                terrainGray = null;
                grayMap = null;
                return;
            }
            var map = state.Map;
            int need = map.Width * map.Height;
            if (terrainGray == null || terrainGray.Length != need) terrainGray = new byte[need];
            grayMap = map;

            for (int row = 0; row < map.Height; row++)
            {
                for (int col = 0; col < map.Width; col++)
                {
                    var tile = map.Get(HexCoord.FromOffset(col, row));
                    int idx = row * map.Width + col;
                    if (tile == null)
                    {
                        terrainGray[idx] = 30;
                        continue;
                    }
                    var c = tile.Def.Color;
                    float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                    if (tile.IsWater) lum *= 0.55f;      // 海は暗く沈めて陸との境界を出す
                    if (tile.HasForest) lum *= 0.86f;
                    if (tile.HasHill) lum *= 1.12f;
                    terrainGray[idx] = ToByte(38f + lum * 105f);   // 約 38..143 の落ち着いた帯域
                }
            }
        }

        /// <summary>現在のタイル領有と都市数を1標本として追記する(1ターンに1回だけ呼ばれる)。</summary>
        void CaptureSample(GameState state)
        {
            var map = state.Map;
            if (map == null) return;
            if (grayMap != map) BuildTerrainGray(state);
            if (samples.Count >= MaxSamples) Decimate();

            int need = map.Width * map.Height;
            byte[] owners = RentOwnerBuffer(need);
            for (int row = 0; row < map.Height; row++)
            {
                int rowBase = row * map.Width;
                for (int col = 0; col < map.Width; col++)
                {
                    var tile = map.Get(HexCoord.FromOffset(col, row));
                    byte v = 0;
                    if (tile != null && tile.OwnerPlayerId >= 0
                        && playerIndexById != null && tile.OwnerPlayerId < playerIndexById.Length)
                    {
                        int pi = playerIndexById[tile.OwnerPlayerId];
                        if (pi >= 0 && pi < 254) v = (byte)(pi + 1);
                    }
                    owners[rowBase + col] = v;
                }
            }

            byte[] cities = RentCityBuffer(playerCount);
            for (int i = 0; i < playerCount; i++)
            {
                var p = state.Players[i];
                int n = (p != null && p.Cities != null) ? p.Cities.Count : 0;
                cities[i] = n > 255 ? (byte)255 : (byte)n;
            }

            samples.Add(owners);
            sampleTurns.Add(state.TurnNumber);
            sampleCities.Add(cities);
            sampleCaptions.Add("ターン " + state.TurnNumber);   // 再生中に文字列を作らないための先行生成
        }

        /// <summary>
        /// 標本を1つおきに間引き(索引 0,2,4,… を残す)、刻み幅を倍にする。
        /// 最古を捨てる方式と違い、序盤から終盤までの全期間をカバーし続けられる。
        /// </summary>
        void Decimate()
        {
            int write = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                if ((i & 1) == 0)
                {
                    samples[write] = samples[i];
                    sampleTurns[write] = sampleTurns[i];
                    sampleCities[write] = sampleCities[i];
                    sampleCaptions[write] = sampleCaptions[i];
                    write++;
                }
                else
                {
                    ownerBufferPool.Add(samples[i]);
                    cityBufferPool.Add(sampleCities[i]);
                }
            }
            int remove = samples.Count - write;
            if (remove > 0)
            {
                samples.RemoveRange(write, remove);
                sampleTurns.RemoveRange(write, remove);
                sampleCities.RemoveRange(write, remove);
                sampleCaptions.RemoveRange(write, remove);
            }
            sampleStride *= 2;
            if (frameIndex >= samples.Count) frameIndex = Mathf.Max(0, samples.Count - 1);
            shownCaptionIndex = -1;
            shownSampleCount = -1;
            sliderMaxSet = -1;
        }

        byte[] RentOwnerBuffer(int length)
        {
            for (int i = ownerBufferPool.Count - 1; i >= 0; i--)
            {
                var b = ownerBufferPool[i];
                ownerBufferPool.RemoveAt(i);
                if (b != null && b.Length == length) return b;
            }
            return new byte[length];
        }

        byte[] RentCityBuffer(int length)
        {
            for (int i = cityBufferPool.Count - 1; i >= 0; i--)
            {
                var b = cityBufferPool[i];
                cityBufferPool.RemoveAt(i);
                if (b != null && b.Length == length) return b;
            }
            return new byte[Mathf.Max(1, length)];
        }

        // ==================================================================
        // UI構築
        // ==================================================================

        void BuildCanvas()
        {
            var go = new GameObject("TimelapseCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // クラス冒頭コメント参照: ゲームオーバーオーバーレイ(100)、Codexの独立パネル
            // (130/135/140)、年表(145)より手前。終了画面の上からでも開ける
            canvas.sortingOrder = 146;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("TimelapseEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        /// <summary>
        /// 左下の「変遷」開閉ボタン。y=176 の段(世界史図鑑 x=10 / 文化・政策 178 /
        /// 遺産・偉人・作品 346 / 年表 514 / 実績 614 / 国家運営 714 / 兵站 846 /
        /// 人口社会 978 / 政治 1110)は埋まっているため、同じ左端 x=10 の一段上
        /// (y=216、高さ36)へ置く。UIManager は読み取っただけで変更していない。
        /// </summary>
        void BuildOpenButton()
        {
            var button = UIStyle.CreateButton(canvas.transform, "TimelapseButton", "変遷", 15, Show);
            UIStyle.SetRect(button.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(10f, 216f), new Vector2(92f, 36f));
            DemoteBehindPanels(button.gameObject);
        }

        /// <summary>
        /// 常時表示ボタンを最背面のネストCanvas(sortingOrder=-5)へ退避する
        /// (ChroniclePanel と同じ規約。開いているモーダルパネルの上へ浮かせない)。
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
            panel = UIStyle.CreatePanel(canvas.transform, "TimelapsePanel",
                new Color(0.05f, 0.065f, 0.10f, 0.97f));
            UIStyle.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(664f, 544f));

            var title = UIStyle.CreateText(panel.transform, "Title", "領土の変遷", 20,
                TextAnchor.MiddleCenter, UIStyle.Accent);
            UIStyle.SetRect(title.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-90f, 28f));

            var close = UIStyle.CreateButton(panel.transform, "Close", "×", 16, Hide);
            UIStyle.SetRect(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(28f, 28f));

            // マップ表示(サイズはテクスチャ生成時にマップ縦横比から確定する)
            var imgGo = new GameObject("TimelapseImage", typeof(RectTransform), typeof(RawImage));
            imgGo.transform.SetParent(panel.transform, false);
            mapImage = imgGo.GetComponent<RawImage>();
            mapImage.color = Color.white;
            mapImage.raycastTarget = false;   // 入力はスライダーとボタンだけが受ける
            mapImageRect = UIStyle.SetRect(imgGo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(MapAreaWidth, MapAreaHeight));

            // 案内(記録不足・記録なしのとき、マップの上に重ねて表示する)。
            // マップが見えている状態でも読めるよう、半透明の下敷きを付ける。
            noticeRoot = UIStyle.CreatePanel(panel.transform, "Notice",
                new Color(0.04f, 0.055f, 0.09f, 0.88f));
            noticeRoot.GetComponent<Image>().raycastTarget = false;
            UIStyle.SetRect(noticeRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(560f, 72f));

            noticeText = UIStyle.CreateText(noticeRoot.transform, "Text", "", 16,
                TextAnchor.MiddleCenter, UIStyle.TextDim);
            UIStyle.StretchFull(noticeText.gameObject, 10f);
            noticeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            noticeText.verticalOverflow = VerticalWrapMode.Truncate;
            noticeRoot.SetActive(false);

            captionText = UIStyle.CreateText(panel.transform, "Caption", "", 19,
                TextAnchor.MiddleCenter, UIStyle.TextMain);
            UIStyle.SetRect(captionText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -366f), new Vector2(-20f, 28f));

            BuildLegend();
            BuildControls();
            BuildSlider();

            panel.SetActive(false);
        }

        /// <summary>文明色スウォッチ+「文明名 都市n」の凡例を4列×2行ぶん作る(内容は再生時に流し込む)。</summary>
        void BuildLegend()
        {
            var container = UIStyle.CreateContainer(panel.transform, "Legend");
            UIStyle.SetRect(container, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -396f), new Vector2(-20f, 52f));

            for (int i = 0; i < MaxLegendEntries; i++)
            {
                int col = i % 4;
                int row = i / 4;
                var entry = UIStyle.CreateContainer(container.transform, "Legend" + i);
                UIStyle.SetRect(entry, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(12f + col * 155f, -row * 25f),
                    new Vector2(150f, 22f));

                var swatchGo = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
                swatchGo.transform.SetParent(entry.transform, false);
                var swatch = swatchGo.GetComponent<Image>();
                swatch.raycastTarget = false;
                UIStyle.SetRect(swatchGo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(12f, 12f));

                var label = UIStyle.CreateText(entry.transform, "Label", "", 13,
                    TextAnchor.MiddleLeft, UIStyle.TextDim);
                UIStyle.SetRect(label.gameObject, new Vector2(0f, 0f), new Vector2(1f, 1f),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 0f));
                var lrt = (RectTransform)label.transform;
                lrt.offsetMin = new Vector2(18f, 0f);
                lrt.offsetMax = new Vector2(0f, 0f);

                legendRoots[i] = entry;
                legendSwatches[i] = swatch;
                legendTexts[i] = label;
                entry.SetActive(false);
            }
        }

        void BuildControls()
        {
            var play = UIStyle.CreateButton(panel.transform, "Play", "▶再生", 14, OnPlayClicked);
            UIStyle.SetRect(play.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(16f, 56f), new Vector2(104f, 34f));

            var pause = UIStyle.CreateButton(panel.transform, "Pause", "⏸一時停止", 14, OnPauseClicked);
            UIStyle.SetRect(pause.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(126f, 56f), new Vector2(124f, 34f));

            var rewind = UIStyle.CreateButton(panel.transform, "Rewind", "⏮先頭へ", 14, OnRewindClicked);
            UIStyle.SetRect(rewind.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(256f, 56f), new Vector2(110f, 34f));

            speedButton = UIStyle.CreateButton(panel.transform, "Speed", SpeedLabels[0], 14, OnSpeedClicked);
            UIStyle.SetRect(speedButton.gameObject, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(372f, 56f), new Vector2(104f, 34f));
            speedLabel = UIStyle.ButtonLabel(speedButton);

            frameLabel = UIStyle.CreateText(panel.transform, "FrameLabel", "", 13,
                TextAnchor.MiddleRight, UIStyle.TextDim);
            UIStyle.SetRect(frameLabel.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-16f, 56f), new Vector2(160f, 34f));
        }

        /// <summary>
        /// uGUI Slider をコードだけで組み立てる(背景 + FillArea/Fill + HandleSlideArea/Handle)。
        /// Slider は fillRect / handleRect の親を基準にアンカーを駆動するため、両者を
        /// 必ずコンテナの子として作る。値は「標本の索引」で、wholeNumbers=true。
        /// </summary>
        void BuildSlider()
        {
            var root = new GameObject("Scrub", typeof(RectTransform), typeof(Image), typeof(Slider));
            root.transform.SetParent(panel.transform, false);
            UIStyle.SetRect(root, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-32f, 22f));
            var bg = root.GetComponent<Image>();
            bg.color = new Color(0.16f, 0.20f, 0.27f, 1f);
            bg.raycastTarget = true;   // 溝クリック・ドラッグを受ける

            var fillArea = UIStyle.CreateContainer(root.transform, "FillArea");
            var far = (RectTransform)fillArea.transform;
            far.anchorMin = new Vector2(0f, 0.5f);
            far.anchorMax = new Vector2(1f, 0.5f);
            far.pivot = new Vector2(0.5f, 0.5f);
            far.offsetMin = new Vector2(7f, -5f);
            far.offsetMax = new Vector2(-7f, 5f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.color = UIStyle.Accent;
            fillImg.raycastTarget = false;
            var frt = (RectTransform)fillGo.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            frt.sizeDelta = new Vector2(14f, 0f);   // つまみ幅ぶんの補正(Unity既定のSliderと同じ考え方)

            var handleArea = UIStyle.CreateContainer(root.transform, "HandleSlideArea");
            var hart = (RectTransform)handleArea.transform;
            hart.anchorMin = Vector2.zero;
            hart.anchorMax = Vector2.one;
            hart.pivot = new Vector2(0.5f, 0.5f);
            hart.offsetMin = new Vector2(7f, 0f);
            hart.offsetMax = new Vector2(-7f, 0f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(handleArea.transform, false);
            var handleImg = handleGo.GetComponent<Image>();
            handleImg.color = Color.white;
            var hrt = (RectTransform)handleGo.transform;
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(0f, 1f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;
            hrt.sizeDelta = new Vector2(14f, 0f);

            scrubSlider = root.GetComponent<Slider>();
            scrubSlider.fillRect = frt;
            scrubSlider.handleRect = hrt;
            scrubSlider.targetGraphic = handleImg;
            scrubSlider.direction = Slider.Direction.LeftToRight;
            scrubSlider.wholeNumbers = true;
            scrubSlider.minValue = 0f;
            scrubSlider.maxValue = 0f;
            scrubSlider.value = 0f;
            var cb = scrubSlider.colors;
            cb.normalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
            cb.highlightedColor = Color.white;
            cb.pressedColor = UIStyle.Accent;
            cb.selectedColor = new Color(0.92f, 0.93f, 0.96f, 1f);
            cb.disabledColor = UIStyle.ButtonDisabled;
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            scrubSlider.colors = cb;
            scrubSlider.onValueChanged.AddListener(OnScrub);
        }

        // ==================================================================
        // 表示・操作
        // ==================================================================

        /// <summary>パネルを開く(初回のみテクスチャを生成する。再生状態は据え置き)。</summary>
        public void Show()
        {
            if (panel == null) return;
            if (!panel.activeSelf) GameAudio.Instance?.PlayPanelOpen();
            panel.SetActive(true);
            if (samples.Count > 0)
            {
                if (frameIndex >= samples.Count) frameIndex = samples.Count - 1;
                ApplyFrame(true);
            }
            else
            {
                RefreshWhenNoFrames();
            }
        }

        public void Hide()
        {
            playing = false;
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>
        /// 外部入口(ゲーム終了画面などが呼ぶ)。パネルを開いてターン1(先頭の記録)から
        /// 再生を始める。記録が MinPlayableSamples 未満なら案内を出すだけで何もしない。
        /// 自己起動インスタンスが居ない・未構築でも null 安全。
        /// </summary>
        public static void StartPlaybackIfAvailable()
        {
            var instance = FindFirstObjectByType<TimelapsePanel>();
            if (instance != null) instance.OpenAndPlayFromStart();
        }

        /// <summary>パネルを開いて先頭から再生する(記録不足なら案内のみ)。</summary>
        public void OpenAndPlayFromStart()
        {
            Show();
            if (panel == null) return;
            if (samples.Count < MinPlayableSamples)
            {
                ShowNotice("領土の記録がまだ足りません。数ターン進めると変遷を再生できます。");
                return;
            }
            frameIndex = 0;
            playing = true;
            nextFrameAt = Time.unscaledTime + FrameInterval();
            ApplyFrame(true);
        }

        void OnPlayClicked()
        {
            if (samples.Count < MinPlayableSamples)
            {
                ShowNotice("領土の記録がまだ足りません。数ターン進めると変遷を再生できます。");
                return;
            }
            // 末尾で押した場合は先頭から見直す
            if (frameIndex >= samples.Count - 1) frameIndex = 0;
            playing = true;
            nextFrameAt = Time.unscaledTime + FrameInterval();
            ApplyFrame(true);
        }

        void OnPauseClicked()
        {
            playing = false;
        }

        void OnRewindClicked()
        {
            if (samples.Count == 0) return;
            frameIndex = 0;
            ApplyFrame(true);
        }

        void OnSpeedClicked()
        {
            speedIndex = (speedIndex + 1) % SpeedFps.Length;
            if (speedLabel != null) speedLabel.text = SpeedLabels[speedIndex];
            nextFrameAt = Time.unscaledTime + FrameInterval();
        }

        /// <summary>スクラブ操作。ユーザーが動かした時は一時停止してそのターンへジャンプする。</summary>
        void OnScrub(float value)
        {
            if (suppressScrub || samples.Count == 0) return;
            int idx = Mathf.Clamp(Mathf.RoundToInt(value), 0, samples.Count - 1);
            if (idx == frameIndex) return;
            frameIndex = idx;
            playing = false;
            ApplyFrame(false);
        }

        float FrameInterval() => 1f / SpeedFps[speedIndex];

        /// <summary>再生の毎フレーム処理(コマ送りの時刻に達した時だけ描画する)。</summary>
        void UpdatePlayback()
        {
            if (!playing || samples.Count == 0) return;
            float now = Time.unscaledTime;
            if (now < nextFrameAt) return;

            float interval = FrameInterval();
            nextFrameAt += interval;
            if (nextFrameAt < now) nextFrameAt = now + interval;   // 大きな遅延後の早送り暴走を防ぐ

            frameIndex++;
            if (frameIndex >= samples.Count) frameIndex = 0;   // 末尾まで来たら先頭へループする
            ApplyFrame(false);
        }

        /// <summary>記録が無い/足りない時の表示(マップを消して案内を出す)。</summary>
        void RefreshWhenNoFrames()
        {
            playing = false;
            if (mapImage != null) mapImage.enabled = false;
            if (captionText != null) captionText.text = "";
            if (frameLabel != null) frameLabel.text = "";
            shownCaptionIndex = -1;
            shownSampleCount = -1;
            for (int i = 0; i < MaxLegendEntries; i++)
                if (legendRoots[i] != null) legendRoots[i].SetActive(false);
            if (scrubSlider != null)
            {
                suppressScrub = true;
                scrubSlider.minValue = 0f;
                scrubSlider.maxValue = 0f;
                scrubSlider.value = 0f;
                suppressScrub = false;
                sliderMaxSet = 0;
            }
            ShowNotice(samples.Count == 0
                ? "領土の記録がまだありません。ターンを進めると自動で記録されます。"
                : "領土の記録がまだ足りません。数ターン進めると変遷を再生できます。");
        }

        void ShowNotice(string messageJa)
        {
            if (noticeText == null || noticeRoot == null) return;
            noticeText.text = messageJa;
            noticeRoot.SetActive(true);
        }

        void HideNotice()
        {
            if (noticeRoot != null && noticeRoot.activeSelf) noticeRoot.SetActive(false);
        }

        // ==================================================================
        // コマ描画
        // ==================================================================

        /// <summary>
        /// 現在の frameIndex のコマを描画してキャプション・凡例・スライダーを更新する。
        /// forceText=true のときはテキストのキャッシュ判定を無視して必ず書き換える。
        /// 定常再生中はキャプション文字列を記録時のものから使い回すためアロケーションしない。
        /// </summary>
        void ApplyFrame(bool forceText)
        {
            if (samples.Count == 0)
            {
                RefreshWhenNoFrames();
                return;
            }
            frameIndex = Mathf.Clamp(frameIndex, 0, samples.Count - 1);
            HideNotice();

            EnsureTexture();
            if (texture == null) return;   // -nographics 等でテクスチャを作れなかった場合の保険
            if (mapImage != null) mapImage.enabled = true;

            RenderOwnership(samples[frameIndex]);

            // キャプション(記録時に生成済みの文字列。索引が変わった時だけ設定する)
            if (captionText != null && (forceText || shownCaptionIndex != frameIndex))
                captionText.text = sampleCaptions[frameIndex];

            // 記録件数(件数が変わった時だけ文字列を作る。コマ送りごとの生成は行わない)
            if (frameLabel != null && (forceText || shownSampleCount != samples.Count))
            {
                shownSampleCount = samples.Count;
                frameLabel.text = "記録 " + samples.Count + "件";
            }

            UpdateLegend(sampleCities[frameIndex], forceText);
            shownCaptionIndex = frameIndex;

            if (scrubSlider != null)
            {
                suppressScrub = true;
                if (sliderMaxSet != samples.Count - 1)
                {
                    sliderMaxSet = samples.Count - 1;
                    scrubSlider.maxValue = sliderMaxSet;
                }
                scrubSlider.value = frameIndex;
                suppressScrub = false;
            }
        }

        /// <summary>凡例(文明色スウォッチ+都市数)を更新する。数値が変わった時だけ文字列を作る。</summary>
        void UpdateLegend(byte[] cities, bool forceText)
        {
            int shown = Mathf.Min(playerCount, MaxLegendEntries);
            for (int i = 0; i < MaxLegendEntries; i++)
            {
                bool active = i < shown;
                if (legendRoots[i] != null && legendRoots[i].activeSelf != active)
                    legendRoots[i].SetActive(active);
                if (!active) continue;

                int count = (cities != null && i < cities.Length) ? cities[i] : 0;
                if (!forceText && shownLegendCounts[i] == count) continue;
                shownLegendCounts[i] = count;
                if (legendTexts[i] != null)
                {
                    string prefix = (legendPrefixes != null && i < legendPrefixes.Length)
                        ? legendPrefixes[i] : "文明 都市";
                    legendTexts[i].text = prefix + count;
                }
            }
        }

        /// <summary>凡例のスウォッチ色を現在の文明色へ合わせる(状態の張り替え時のみ)。</summary>
        void ApplyLegendIdentity()
        {
            for (int i = 0; i < MaxLegendEntries; i++)
            {
                if (legendSwatches[i] == null) continue;
                if (i < ownerColors.Length)
                {
                    var c = ownerColors[i];
                    c.a = 1f;
                    legendSwatches[i].color = c;
                }
                shownLegendCounts[i] = -1;
            }
        }

        /// <summary>マップ寸法に合わせてテクスチャとバッファを(必要な時だけ)作る。</summary>
        void EnsureTexture()
        {
            if (texture != null && mapW == recordedWidth && mapH == recordedHeight) return;
            if (recordedWidth <= 0 || recordedHeight <= 0) return;

            mapW = recordedWidth;
            mapH = recordedHeight;
            texW = mapW * PixelsPerTile + 1;   // odd-r の奇数行を1px右へずらすための余白
            texH = mapH * PixelsPerTile;
            if (texture != null) Destroy(texture);
            texture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            buffer = new Color32[texW * texH];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = BackgroundColor;
            if (mapImage != null) mapImage.texture = texture;

            // マップ縦横比を保ったまま表示枠へ収める
            float scale = Mathf.Min(MapAreaWidth / texW, MapAreaHeight / texH);
            if (mapImageRect != null)
                mapImageRect.sizeDelta = new Vector2(
                    Mathf.Round(texW * scale), Mathf.Round(texH * scale));
        }

        /// <summary>
        /// 1コマ分の領有バイト列をテクスチャへ描く。地形のグレースケール土台に
        /// 所有者色を強く乗せるため、隣り合う文明の境界がはっきり読める。
        /// バッファは再利用し、この処理自体はアロケーションフリー。
        /// </summary>
        void RenderOwnership(byte[] owners)
        {
            if (owners == null || buffer == null || terrainGray == null) return;
            int tiles = Mathf.Min(owners.Length, terrainGray.Length);

            for (int row = 0; row < mapH; row++)
            {
                int rowBase = row * mapW;
                for (int col = 0; col < mapW; col++)
                {
                    int idx = rowBase + col;
                    if (idx >= tiles) continue;
                    byte g = terrainGray[idx];
                    byte o = owners[idx];

                    Color32 c;
                    if (o == 0 || o > ownerColors.Length)
                    {
                        c = new Color32(g, g, g, 255);
                    }
                    else
                    {
                        var pc = ownerColors[o - 1];
                        // 地形の明暗を残しつつ文明色を強く出す(0.50〜1.35 の明度倍率)
                        float k = 0.50f + (g / 255f) * 0.85f;
                        c = new Color32(
                            ToByte(pc.r * 255f * k),
                            ToByte(pc.g * 255f * k),
                            ToByte(pc.b * 255f * k),
                            255);
                    }
                    PaintTile(col, row, c);
                }
            }

            texture.SetPixels32(buffer);
            texture.Apply(false);
        }

        /// <summary>タイル1枚(PixelsPerTile 四方)を塗る。odd-r の奇数行は1px右へずらす。</summary>
        void PaintTile(int col, int row, Color32 c)
        {
            int px = col * PixelsPerTile + (row & 1);
            int start = row * PixelsPerTile * texW + px;
            for (int y = 0; y < PixelsPerTile; y++)
            {
                int i = start + y * texW;
                for (int x = 0; x < PixelsPerTile; x++) buffer[i + x] = c;
            }
        }

        /// <summary>0..255 へ丸めた byte 変換(Color32 用)。</summary>
        static byte ToByte(float v)
        {
            if (v <= 0f) return 0;
            if (v >= 255f) return 255;
            return (byte)v;
        }
    }
}
