using System;
using System.Collections.Generic;
using UnityEngine;
using HexCiv.Core;

namespace HexCiv.Audio
{
    /// <summary>
    /// ゲーム全体のBGMと効果音を管理する。
    /// 現在の音は実行時に波形から生成しているため、外部素材やライセンス表記は不要。
    /// 将来AudioClip素材へ差し替える場合も、公開再生メソッドはそのまま利用できる。
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        const int SampleRate = 22050;
        const string MusicVolumeKey = "HexCiv.MusicVolume";
        const string SfxVolumeKey = "HexCiv.SfxVolume";
        const string MuteKey = "HexCiv.AudioMuted";

        public static GameAudio Instance { get; private set; }

        AudioSource musicSource;
        AudioSource sfxSource;
        GameState state;

        // D1修正: 都市陥落ログ「都市「X」が陥落した!」は都市名しか含まず、
        // EmitLog時点では所有権がすでに新所有者へ移っている(GameStateOps.CaptureCity)。
        // そのため人間プレイヤーの都市名を「捕獲前スナップショット」として保持し、照合に使う。
        // スナップショットはUpdate()でVersion変化時のみ更新する — OnLogは捕獲処理中に
        // 同期発火するため、その時点ではまだ更新前(=陥落した都市名を含む)状態が残っている。
        readonly HashSet<string> humanCityNames = new HashSet<string>(StringComparer.Ordinal);
        int humanCityNamesVersion = -1;

        AudioClip musicClip;
        AudioClip uiClickClip;
        AudioClip selectClip;
        AudioClip moveClip;
        AudioClip attackClip;
        AudioClip endTurnClip;
        AudioClip foundCityClip;
        AudioClip researchClip;
        AudioClip productionClip;
        AudioClip alertClip;
        AudioClip victoryClip;
        AudioClip defeatClip;

        // --- 2026-07-21 Claude Code 追加: 世界史イベントSE(遺産発見・偉人登用・作品収蔵) ---
        AudioClip heritageClip;
        AudioClip greatPersonClip;
        AudioClip masterpieceClip;

        // --- 2026-07-21 Claude Code 追加: パネル開閉音+ミニマップジャンプ音 ---
        // パネル開: 柔らかい紙めくり風の短いウーシュ(ローパスノイズの山形スイープ)。
        // 年表・ミニマップの「開く」操作と歴史ツアー開始が呼ぶ。
        // ミニマップジャンプ: 上行二音の小さなブリップ(UIクリック音の下降と対照的)。
        // どちらも音量・ミュートは既存の Play() が一括処理する。
        AudioClip panelOpenClip;
        AudioClip minimapJumpClip;

        // --- 2026-07-21 Claude Code 追加: 開幕ホルン ---
        // 新しいゲームの開始時(TurnManager.BeginGame の開幕ログ「文明の夜明け ―」)に一度だけ
        // 鳴らす、静かな二音のホルンコール(約1.2秒、C4→G4の上行五度)。祝祭的で長い勝利
        // ファンファーレとも、低音金管の宣戦スティングとも音域・長さ・進行で区別できる。
        AudioClip openingHornClip;

        // --- 2026-07-21 Claude Code 追加: ユニット種別別の戦闘SE ---
        // 戦闘解決イベント(GameState.OnCombatResolved)を購読し、攻撃側ユニットの種別で
        // 効果音を選ぶ: カタパルト=深い発射音+落下ホイッスル / その他遠隔(弓兵)=矢の
        // ヒュッ+突き刺さるトック / 近接(戦士・槍兵・剣士・斥候)=金属の打ち合い2打。
        // 攻撃側を特定できない場合や民間人は従来の汎用ヒット音(attackClip)へフォールバック。
        // InputController が攻撃直前に呼ぶ PlayAttack() は保留扱いにし、同フレームの戦闘解決で
        // 種別SEへ差し替える。戦闘解決が来なければ翌フレームの Update が従来どおり汎用ヒットを
        // 鳴らすため、既存の呼び出し経路は無音にならない。
        AudioClip siegeAttackClip;    // カタパルト(攻城)
        AudioClip rangedAttackClip;   // 弓兵など遠隔
        AudioClip meleeAttackClip;    // 近接
        /// <summary>PlayAttack() が保留した汎用ヒット音のフレーム番号(-1=保留なし)。</summary>
        int pendingGenericAttackFrame = -1;
        // 戦闘SEの1秒あたり回数キャップ(256倍速観戦などで戦闘が殺到した時の間引き。
        // EntityRenderer の演出はフレーム単位で間引くが、音は残響が重なるため秒単位にする)
        const int MaxCombatSoundsPerSecond = 8;
        float combatSoundWindowStart = -999f;
        int combatSoundCountInWindow;

        // --- 2026-07-21 Claude Code 追加: イベントスティング(宣戦布告・都市陥落・和平) ---
        // 宣戦・陥落は既存のアルペジオ系チャイムとは明確に異なる低音域の打撃系2種。
        // 和平は中音域の柔らかい解決二音(宣戦スティングと音域・進行とも対照的)。
        AudioClip warStingClip;
        AudioClip captureStingClip;
        AudioClip peaceStingClip;

        // --- 2026-07-21 Claude Code 追加: 勝利ファンファーレ+BGMダッキング ---
        // 勝利文脈(通常プレイで人間が勝利/観戦モードで勝者決定)のゲーム終了時に一度だけ、
        // 既存の短い勝利SEに代えて約2.5秒の祝祭ファンファーレ(上昇アルペジオ+柔らかい和音)を
        // 鳴らす。その間BGMは止めずに約40%へダッキングし、終了後は約2秒かけて復帰する。
        // 敗北(および勝者なし終了)の音と「音楽停止」挙動は従来のまま変えない。
        AudioClip victoryFanfareClip;
        const float FanfareDuckLevel = 0.4f;
        const float FanfareDuckAttackSeconds = 0.15f;
        const float FanfareDuckReleaseSeconds = 2f;
        /// <summary>現在のダッキング係数(1=通常。ApplyVolumes が音楽音量へ乗算する)。</summary>
        float duckWeight = 1f;
        /// <summary>ダッキングを保持する終了時刻(Time.unscaledTime。過ぎると復帰ランプ開始)。</summary>
        float duckHoldUntil = -1f;

        // --- 2026-07-21 Claude Code 追加: 環境音レイヤー(BGMの下の静かな風) ---
        // 専用AudioSourceで常時ループ再生する。音量は ApplyVolumes が「BGM音量×0.25」で
        // 追従するため、既存のBGM音量スライダーとミュートにそのまま従う(専用UIは持たない)。
        // クリップ自体の振幅もBGMより十分小さく、実効でBGMより約-24dB相当の背景レベル。
        const float AmbientVolumeScale = 0.25f;
        AudioSource ambientSource;
        AudioClip ambientClip;

        // --- 2026-07-21 Claude Code 追加: 時代BGM(同日、終盤の曲Cへ拡張) ---
        // ターン数に応じて3曲をクロスフェードする:
        //   曲A(Dawn) ターン100以下 / 曲B(Golden Age) 101〜180 / 曲C(Twilight) 181〜。
        // 新規ゲーム/リスタート/ロード(Init)では常に曲Aへ戻す。ヘッドレス/エディタ
        // バッチ構成ではこのコンポーネント自体が生成されないため従来どおり無音安全。
        const int EraBTurnThreshold = 100;
        const int EraCTurnThreshold = 180;
        const float EraCrossfadeSeconds = 2f;
        AudioSource musicSourceB;
        AudioSource musicSourceC;
        AudioClip musicClipB;
        AudioClip musicClipC;
        // 各曲の現在の音量比([0]=A, [1]=B, [2]=C。既定はAのみ=従来と同一挙動)。
        // 単一のブレンド値ではなく曲ごとの重みにすることで、超高速観戦でA→Bのフェード中に
        // 目標がCへ変わっても段差なく追従できる。
        readonly float[] eraWeights = { 1f, 0f, 0f };

        // --- 2026-07-21 Claude Code 追加: 自文明の地域別BGMフレーバー ---
        // ゲーム開始(Init)のたびに人間プレイヤーの文明地域(GlobalHistoryIndex.BroadRegion の
        // 共通6地域)を引き直し、時代BGM3曲の「鐘メロディの音程表・減衰・拍アクセント」だけを
        // 地域ごとに控えめに変える。和音進行・低音・テンポ・尺・音量バランスは全地域共通のため、
        // 差は並べて聴いて分かる程度にとどまる。ヨーロッパ・地中海、観戦モード(人間なし)、
        // 文明台帳で引けない旧セーブは既定=現行サウンドのまま。3曲を置き換えるだけなので
        // メモリ使用量も従来と同じ。
        RegionalMusicStyle musicStyle = RegionalMusicStyle.Default;

        readonly List<AudioClip> generatedClips = new List<AudioClip>();

        float musicVolume;
        float sfxVolume;
        bool muted;

        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public bool Muted => muted;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.35f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.70f));
            muted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
            EnsureSources();
            EnsureClips();
            ApplyVolumes();
        }

        /// <summary>新しいゲーム状態へ接続し、戦略BGMを開始する。</summary>
        public void Init(GameState newState)
        {
            if (state != null)
            {
                state.OnLog -= OnGameLog;
                state.OnCombatResolved -= OnCombatResolvedSfx;   // ユニット種別戦闘SE(2026-07-21 追加)
            }
            state = newState;
            if (state != null)
            {
                state.OnLog += OnGameLog;
                state.OnCombatResolved += OnCombatResolvedSfx;   // ユニット種別戦闘SE(2026-07-21 追加)
            }
            RefreshHumanCityNames();   // 再Init(リスタート)時は前ゲームの名前を破棄して作り直す

            // ユニット種別戦闘SE(2026-07-21 追加): 前ゲームの保留・間引き状態を持ち越さない
            pendingGenericAttackFrame = -1;
            combatSoundWindowStart = -999f;
            combatSoundCountInWindow = 0;

            EnsureSources();
            EnsureClips();
            ApplyRegionalMusicStyle();   // 地域別BGMフレーバー(2026-07-21 追加。地域不変なら何もしない)
            musicSource.clip = musicClip;
            musicSource.loop = true;
            if (!musicSource.isPlaying) musicSource.Play();

            // 時代BGM: 新規ゲーム/リスタート/ロードでは常に曲Aから開始し直す。
            // ロード地点がターン100超/180超なら、以後のUpdateで改めて曲B/Cへクロスフェードする。
            eraWeights[0] = 1f;
            eraWeights[1] = 0f;
            eraWeights[2] = 0f;
            if (musicSourceB != null)
            {
                musicSourceB.Stop();
                musicSourceB.clip = musicClipB;
                musicSourceB.loop = true;
            }
            if (musicSourceC != null)
            {
                musicSourceC.Stop();
                musicSourceC.clip = musicClipC;
                musicSourceC.loop = true;
            }

            // 環境音(2026-07-21 追加): BGMと同様に常時ループ再生する。
            // 音量はこの後の ApplyVolumes が「BGM音量×0.25」で設定する。
            if (ambientSource != null)
            {
                ambientSource.clip = ambientClip;
                ambientSource.loop = true;
                if (!ambientSource.isPlaying) ambientSource.Play();
            }

            // 勝利ファンファーレのダッキングも新規ゲーム/リスタート/ロードでは常に解除する
            // (2026-07-21 追加。前ゲーム終了時の減衰状態を持ち越さない)
            duckWeight = 1f;
            duckHoldUntil = -1f;

            ApplyVolumes();
        }

        void Update()
        {
            // 状態が変わった(Versionが進んだ)フレームでのみスナップショットを更新する(軽量)。
            if (state != null && state.Version != humanCityNamesVersion)
                RefreshHumanCityNames();

            UpdateEraMusic();       // 時代BGMのクロスフェード進行(2026-07-21 追加)
            UpdateFanfareDucking(); // 勝利ファンファーレ中のBGMダッキング進行(2026-07-21 追加)
            FlushPendingGenericAttack(); // 保留中の汎用ヒット音の後始末(2026-07-21 追加)
        }

        void RefreshHumanCityNames()
        {
            humanCityNames.Clear();
            humanCityNamesVersion = state != null ? state.Version : -1;
            var human = state != null ? state.HumanPlayer : null;
            if (human == null) return;   // 観戦/ヘッドレス構成では空のままにする
            for (int i = 0; i < human.Cities.Count; i++)
            {
                var city = human.Cities[i];
                if (city != null && !string.IsNullOrEmpty(city.NameJa))
                    humanCityNames.Add(city.NameJa);
            }
        }

        /// <summary>ログ内の最初の「」内の名前が、人間プレイヤーの都市名スナップショットにあるか。</summary>
        bool ContainsHumanCityName(string message)
        {
            int open = message.IndexOf('「');
            if (open < 0) return false;
            int close = message.IndexOf('」', open + 1);
            if (close <= open + 1) return false;
            return humanCityNames.Contains(message.Substring(open + 1, close - open - 1));
        }

        void OnDestroy()
        {
            if (state != null)
            {
                state.OnLog -= OnGameLog;
                state.OnCombatResolved -= OnCombatResolvedSfx;   // ユニット種別戦闘SE(2026-07-21 追加)
            }
            if (Instance == this) Instance = null;

            for (int i = 0; i < generatedClips.Count; i++)
                if (generatedClips[i] != null) Destroy(generatedClips[i]);
            generatedClips.Clear();
        }

        void EnsureSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
                musicSource.priority = 128;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0f;
                sfxSource.priority = 64;
            }

            // 時代BGM用の第二音源(2026-07-21 追加)。普段は無音で、クロスフェード時のみ鳴る。
            if (musicSourceB == null)
            {
                musicSourceB = gameObject.AddComponent<AudioSource>();
                musicSourceB.playOnAwake = false;
                musicSourceB.loop = true;
                musicSourceB.spatialBlend = 0f;
                musicSourceB.priority = 128;
            }

            // 時代BGM用の第三音源(2026-07-21 追加、終盤曲C)。扱いは第二音源と同じ。
            if (musicSourceC == null)
            {
                musicSourceC = gameObject.AddComponent<AudioSource>();
                musicSourceC.playOnAwake = false;
                musicSourceC.loop = true;
                musicSourceC.spatialBlend = 0f;
                musicSourceC.priority = 128;
            }

            // 環境音用の音源(2026-07-21 追加)。BGMの下でごく小さくループ再生する。
            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.playOnAwake = false;
                ambientSource.loop = true;
                ambientSource.spatialBlend = 0f;
                ambientSource.priority = 200;
            }
        }

        void EnsureClips()
        {
            if (musicClip != null) return;

            // 時代BGM3曲は地域別フレーバー(2026-07-21 追加)に対応。初期化時点では既定スタイル
            // (=従来と同一波形)で生成し、Init が地域判明後に必要なら3曲だけ作り直す。
            musicClip = CreateMusic(musicStyle);
            musicClipB = CreateMusicEraB(musicStyle);   // 時代BGM第二曲(2026-07-21 追加、約2.1MB)
            musicClipC = CreateMusicEraC(musicStyle);   // 終盤BGM第三曲(2026-07-21 追加、16秒・約1.4MB)
            ambientClip = CreateAmbience();   // 環境音(2026-07-21 追加、10秒・約0.9MB)
            uiClickClip = CreateClip("UI Click", 0.075f, t =>
            {
                float p = t / 0.075f;
                return Triangle(Mathf.Lerp(940f, 700f, p), t) * Decay(p, 9f) * 0.32f;
            });
            selectClip = CreateClip("Unit Select", 0.16f, t =>
            {
                float p = t / 0.16f;
                float note = p < 0.48f ? 523.25f : 659.25f;
                return (Sine(note, t) + 0.25f * Triangle(note * 2f, t)) * Decay(p, 4.2f) * 0.30f;
            });
            moveClip = CreateMoveClip();
            attackClip = CreateAttackClip();
            endTurnClip = CreateClip("End Turn", 0.52f, t =>
            {
                float p = t / 0.52f;
                float f = p < 0.45f ? 392f : 293.66f;
                return (Sine(f, t) + 0.22f * Sine(f * 1.5f, t)) * SoftEnvelope(p, 0.03f, 0.58f) * 0.30f;
            });
            foundCityClip = CreateArpeggio("Found City", 1.05f,
                new[] { 261.63f, 329.63f, 392f, 523.25f }, 0.22f, 0.36f);
            researchClip = CreateArpeggio("Research Complete", 0.95f,
                new[] { 440f, 554.37f, 659.25f, 880f }, 0.18f, 0.30f);
            productionClip = CreateArpeggio("Production Complete", 0.62f,
                new[] { 329.63f, 392f, 493.88f }, 0.16f, 0.29f);
            alertClip = CreateArpeggio("Strategic Alert", 0.72f,
                new[] { 220f, 207.65f, 174.61f }, 0.18f, 0.34f);
            victoryClip = CreateArpeggio("Victory", 2.15f,
                new[] { 261.63f, 329.63f, 392f, 523.25f, 659.25f, 783.99f }, 0.29f, 0.38f);
            defeatClip = CreateArpeggio("Defeat", 2.0f,
                new[] { 392f, 311.13f, 261.63f, 196f, 155.56f }, 0.32f, 0.36f);

            // --- 2026-07-21 Claude Code 追加: 世界史イベント用の短いチャイム3種 ---
            // 遺産発見: 高音域のきらめく上昇(D5-F#5-A5-D6)。既存アルペジオと調が重ならない。
            heritageClip = CreateArpeggio("Heritage Discovery", 0.95f,
                new[] { 587.33f, 739.99f, 880f, 1174.66f }, 0.15f, 0.30f);
            // 偉人登用: 荘重な5音ファンファーレ(G4-B4-D5-G5-B5)。
            greatPersonClip = CreateArpeggio("Great Person Recruited", 1.05f,
                new[] { 392f, 493.88f, 587.33f, 783.99f, 987.77f }, 0.19f, 0.31f);
            // 作品収蔵: 柔らかい3音の輝き(E5-G#5-B5)。
            masterpieceClip = CreateArpeggio("Masterpiece Collected", 0.78f,
                new[] { 659.25f, 830.61f, 987.77f }, 0.16f, 0.29f);

            // --- 2026-07-21 Claude Code 追加: イベントスティング ---
            // 生成はこの初期化経路で一度だけ行い、RegisterClip 経由で破棄も既存と同じ扱い。
            warStingClip = CreateWarStingClip();
            captureStingClip = CreateCaptureStingClip();
            peaceStingClip = CreatePeaceStingClip();   // 和平スティング(同日追加)

            // 勝利ファンファーレ(2026-07-21 追加。約2.5秒、既存Victoryより長い祝祭曲)
            victoryFanfareClip = CreateVictoryFanfareClip();

            // 開幕ホルン(2026-07-21 追加。約1.2秒、静かな二音のホルンコール)
            openingHornClip = CreateOpeningHornClip();

            // --- 2026-07-21 Claude Code 追加: ユニット種別別の戦闘SE3種 ---
            // 生成はこの初期化経路で一度だけ行い、RegisterClip 経由で破棄も既存と同じ扱い。
            siegeAttackClip = CreateSiegeAttackClip();
            rangedAttackClip = CreateRangedAttackClip();
            meleeAttackClip = CreateMeleeAttackClip();

            // --- 2026-07-21 Claude Code 追加: パネル開閉音+ミニマップジャンプ音 ---
            // 生成はこの初期化経路で一度だけ行い、RegisterClip 経由で破棄も既存と同じ扱い。
            panelOpenClip = CreatePanelOpenClip();
            minimapJumpClip = CreateMinimapJumpClip();
        }

        public void PlayUiClick() => Play(uiClickClip);
        public void PlaySelect() => Play(selectClip);
        public void PlayMove() => Play(moveClip);
        public void PlayAttack()
        {
            // 2026-07-21 Claude Code 追加(ユニット種別戦闘SE): 唯一の既存呼び出し元
            // (InputController)はこの直後に Combat.PerformAttack を実行するため、同フレームの
            // OnCombatResolved が種別SE(不明時は従来の汎用ヒット音)を鳴らす。戦闘解決が来ない
            // 呼び出しでも、翌フレームの Update(FlushPendingGenericAttack)が従来どおり
            // 汎用ヒット音を鳴らすため無音にはならない。
            pendingGenericAttackFrame = Time.frameCount;
        }
        public void PlayEndTurn() => Play(endTurnClip);
        public void PlayFoundCity() => Play(foundCityClip);
        public void PlayResearchComplete() => Play(researchClip);
        public void PlayProductionComplete() => Play(productionClip);
        public void PlayAlert() => Play(alertClip);

        // 2026-07-21 Claude Code 追加: 世界史イベントSE
        public void PlayHeritageDiscovered() => Play(heritageClip);
        public void PlayGreatPersonRecruited() => Play(greatPersonClip);
        public void PlayMasterpieceCollected() => Play(masterpieceClip);

        // 2026-07-21 Claude Code 追加: 開幕ホルン(音量・ミュートは Play() が一括処理)
        public void PlayOpeningHorn() => Play(openingHornClip);

        // 2026-07-21 Claude Code 追加: パネル開閉音+ミニマップジャンプ音
        // (年表・ミニマップの「開く」と歴史ツアー開始/ミニマップのマップジャンプが呼ぶ。
        //  音量・ミュートは Play() が一括処理)
        public void PlayPanelOpen() => Play(panelOpenClip);
        public void PlayMinimapJump() => Play(minimapJumpClip);

        // 2026-07-21 Claude Code 追加: イベントスティング(音量・ミュートは Play() が一括処理)
        public void PlayWarDeclared() => Play(warStingClip);
        public void PlayCityCaptured() => Play(captureStingClip);
        public void PlayPeaceMade() => Play(peaceStingClip);

        public void PlayGameOver(bool humanWon)
        {
            // 勝利文脈の判定(2026-07-21 Claude Code 追加):
            //   通常プレイ = 人間プレイヤーが勝者(humanWon。GameBootstrap が Winner==HumanPlayer で渡す)
            //   観戦モード = HumanPlayer が null。勝者が決まっていれば誰の勝利でもファンファーレを鳴らす
            // GameAudio は Core 状態を読むだけで、呼び出し側(GameBootstrap)の引数仕様は変えない。
            bool spectator = state != null && state.HumanPlayer == null;
            bool victoryContext = humanWon || (spectator && state.Winner != null);

            if (victoryContext)
            {
                // 勝利: BGMは停止せず、ファンファーレの長さ(約2.5秒)だけ約40%へダッキングし、
                // その後 UpdateFanfareDucking が約2秒かけて復帰させる。ミュート/SE音量は
                // 従来どおり Play() が一括処理する(ミュート中でもダッキング進行自体は無害)。
                var clip = victoryFanfareClip != null ? victoryFanfareClip : victoryClip;
                duckHoldUntil = Time.unscaledTime + (clip != null ? clip.length : 2.5f);
                Play(clip);
                return;
            }

            // 敗北(および勝者なしのターン上限終了)は従来どおり: 全音楽を停止して敗北SE。
            if (musicSource != null) musicSource.Stop();
            if (musicSourceB != null) musicSourceB.Stop();   // 時代BGM側も停止(2026-07-21 追加)
            if (musicSourceC != null) musicSourceC.Stop();   // 終盤曲Cも停止(同日追加)
            if (ambientSource != null) ambientSource.Stop(); // 環境音も停止(同日追加。Initで再開する)
            Play(defeatClip);
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public void ToggleMute()
        {
            muted = !muted;
            PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        void ApplyVolumes()
        {
            // 時代BGM対応(2026-07-21): 音楽音量をA/B/C三曲へクロスフェード比率で配分する。
            // eraWeights={1,0,0}(既定)のとき musicSource.volume は従来と完全に同じ値になる。
            // 勝利ファンファーレ中は duckWeight(通常1=無影響)で音楽系のみ一括ダッキングする
            // (2026-07-21 追加。SEは減衰しない)。
            float musicBase = (muted ? 0f : musicVolume) * duckWeight;
            if (musicSource != null) musicSource.volume = musicBase * eraWeights[0];
            if (musicSourceB != null) musicSourceB.volume = musicBase * eraWeights[1];
            if (musicSourceC != null) musicSourceC.volume = musicBase * eraWeights[2];
            // 環境音(2026-07-21 追加): BGM音量×0.25。BGMのミュート/音量設定にそのまま追従する
            if (ambientSource != null) ambientSource.volume = musicBase * AmbientVolumeScale;
            if (sfxSource != null) sfxSource.volume = muted ? 0f : sfxVolume;
        }

        void Play(AudioClip clip)
        {
            if (clip == null || sfxSource == null || muted || sfxVolume <= 0.001f) return;
            sfxSource.PlayOneShot(clip);
        }

        /// <summary>
        /// 時代BGMのクロスフェード進行(2026-07-21 Claude Code 追加。同日、終盤曲Cへ拡張)。
        /// ターン数に応じた目標曲(A: 100以下 / B: 101〜180 / C: 181〜)へ、曲ごとの音量比を
        /// 約2秒で寄せる。曲別の重み方式のため、超高速観戦でA→Bのフェード完了前に目標がCへ
        /// 変わっても段差なく追従する。
        /// 観戦モードの倍速(Time.timeScale)に影響されないよう非スケール時間で進める。
        /// PlayGameOver で音楽停止中は何もしない(リスタート時は Init が曲Aを再開する)。
        /// </summary>
        void UpdateEraMusic()
        {
            if (musicSource == null || musicSourceB == null || musicSourceC == null
                || musicClip == null || musicClipB == null || musicClipC == null)
                return;
            if (!musicSource.isPlaying && !musicSourceB.isPlaying && !musicSourceC.isPlaying)
                return;   // ゲーム終了などで停止中

            int desired = 0;
            if (state != null)
            {
                if (state.TurnNumber > EraCTurnThreshold) desired = 2;
                else if (state.TurnNumber > EraBTurnThreshold) desired = 1;
            }

            float step = Time.unscaledDeltaTime / EraCrossfadeSeconds;
            bool changed = false;
            for (int i = 0; i < eraWeights.Length; i++)
            {
                float target = i == desired ? 1f : 0f;
                if (eraWeights[i] == target) continue;   // MoveTowardsは目標値へ正確に到達するため等値比較で良い

                // フェードインする側の音源が止まっていれば、再生中の音源と拍位置を合わせて開始する。
                if (target > eraWeights[i]) StartEraSourceSynced(i);
                eraWeights[i] = Mathf.MoveTowards(eraWeights[i], target, step);
                changed = true;
            }
            if (!changed) return;   // 全曲が目標比率に到達済み(従来の等値早期returnと同じ)
            ApplyVolumes();

            // フェードアウトが完了した音源は停止してミキサー負荷を戻す。
            if (eraWeights[0] <= 0f && musicSource.isPlaying) musicSource.Stop();
            if (eraWeights[1] <= 0f && musicSourceB.isPlaying) musicSourceB.Stop();
            if (eraWeights[2] <= 0f && musicSourceC.isPlaying) musicSourceC.Stop();
        }

        /// <summary>
        /// 勝利ファンファーレ中のBGMダッキング進行(2026-07-21 Claude Code 追加)。
        /// duckHoldUntil(ファンファーレ終了予定時刻)までは約40%(FanfareDuckLevel)へ素早く
        /// 下げ、過ぎたら約2秒(FanfareDuckReleaseSeconds)かけて100%へ戻す。
        /// 非スケール時間で進めるため観戦倍速(Time.timeScale)の影響を受けない。
        /// 通常時は duckWeight==1 で即returnし、既存の音量挙動と完全に一致する。
        /// </summary>
        void UpdateFanfareDucking()
        {
            float target = Time.unscaledTime < duckHoldUntil ? FanfareDuckLevel : 1f;
            if (duckWeight == target) return;   // MoveTowardsは目標値へ正確に到達するため等値比較で良い
            float range = 1f - FanfareDuckLevel;
            float speed = target < duckWeight
                ? range / FanfareDuckAttackSeconds     // ダッキング開始は素早く
                : range / FanfareDuckReleaseSeconds;   // 復帰は約2秒のランプ
            duckWeight = Mathf.MoveTowards(duckWeight, target, Time.unscaledDeltaTime * speed);
            ApplyVolumes();
        }

        AudioSource EraSource(int era) => era == 0 ? musicSource : era == 1 ? musicSourceB : musicSourceC;
        AudioClip EraClip(int era) => era == 0 ? musicClip : era == 1 ? musicClipB : musicClipC;

        /// <summary>
        /// 指定時代の音源が停止していれば、再生中の音源とサンプル位置を合わせて再生を始める
        /// (全曲同テンポなので位相の共有で自然に重なる。尺の異なる曲Cは剰余で折り返す)。
        /// </summary>
        void StartEraSourceSynced(int era)
        {
            var target = EraSource(era);
            if (target == null || target.isPlaying) return;
            var clip = EraClip(era);
            if (clip == null) return;
            target.clip = clip;
            target.loop = true;
            AudioSource reference = null;
            if (musicSource.isPlaying) reference = musicSource;
            else if (musicSourceB.isPlaying) reference = musicSourceB;
            else if (musicSourceC.isPlaying) reference = musicSourceC;
            target.timeSamples = reference != null ? reference.timeSamples % clip.samples : 0;
            target.Play();
        }

        /// <summary>
        /// 自文明の地域別BGMフレーバー適用(2026-07-21 Claude Code 追加)。
        /// Init(新規ゲーム・リスタート・文明変更・ロード)から呼ばれ、人間プレイヤーの文明地域を
        /// 引き直して、前回と異なる場合のみ時代BGM3曲を新しい地域パラメータで作り直す。
        /// 3曲は同尺・同テンポの置き換えなのでメモリ使用量は変わらず、時代A/B/Cのクロスフェードや
        /// 拍位置同期(StartEraSourceSynced)もそのまま機能する。既定地域(ヨーロッパ・地中海/観戦/
        /// 不明)から変わらない限り何もしないため、従来挙動は完全に保たれる。
        /// </summary>
        void ApplyRegionalMusicStyle()
        {
            var style = ResolveRegionalStyle();
            if (ReferenceEquals(style, musicStyle)) return;   // 地域が変わらない限り従来どおり
            musicStyle = style;

            // 旧3曲を鳴らしている音源を止めてから差し替える。この直後のInit本体が
            // 曲Aの割り当てと再生・時代重みのリセットを従来どおり行う。
            if (musicSource != null) musicSource.Stop();
            if (musicSourceB != null) musicSourceB.Stop();
            if (musicSourceC != null) musicSourceC.Stop();
            ReleaseGeneratedClip(musicClip);
            ReleaseGeneratedClip(musicClipB);
            ReleaseGeneratedClip(musicClipC);
            musicClip = CreateMusic(style);
            musicClipB = CreateMusicEraB(style);
            musicClipC = CreateMusicEraC(style);
        }

        /// <summary>
        /// 人間プレイヤーの文明から地域別BGMスタイルを決める(2026-07-21 Claude Code 追加)。
        /// 観戦モード(HumanPlayer==null)や文明台帳で引けない旧セーブは既定(現行サウンド)。
        /// </summary>
        RegionalMusicStyle ResolveRegionalStyle()
        {
            var human = state != null ? state.HumanPlayer : null;
            if (human == null) return RegionalMusicStyle.Default;
            var civilization = CivilizationCatalog.Find(human.CivilizationId)
                ?? CivilizationCatalog.FindByName(human.NameJa);
            return RegionalMusicStyle.ForRegion(GlobalHistoryIndex.BroadRegion(civilization));
        }

        /// <summary>再生成で不要になった生成済みクリップを破棄し、管理リストからも外す(2026-07-21 追加)。</summary>
        void ReleaseGeneratedClip(AudioClip clip)
        {
            if (clip == null) return;
            generatedClips.Remove(clip);
            Destroy(clip);
        }

        void OnGameLog(string message)
        {
            if (string.IsNullOrEmpty(message) || state == null) return;
            var human = state.HumanPlayer;

            // --- 2026-07-21 Claude Code 追加: 開幕ホルン ---
            // 実ログ文言(Core/TurnManager.cs BeginGame): 文明の夜明け ― 各文明の歴史が始まった
            // BeginGame は新規開始・リスタート・観戦開始でのみ呼ばれる(ロードでは呼ばれない)
            // ため、「新しいゲームが始まった時に一度だけ」の再生になる。観戦モードでも鳴らす
            // ので human==null の判定より前に置く。他の既存分岐の文言とは重複しない。
            if (message.StartsWith("文明の夜明け", StringComparison.Ordinal))
            {
                PlayOpeningHorn();
                return;
            }

            // --- 2026-07-21 Claude Code 追加: イベントスティング ---
            // 宣戦布告と都市陥落は当事者を問わず(観戦モード含む全文明で)専用スティングを鳴らす。
            // 人間が当事者の場合は従来の警告アルペジオ(PlayAlert)も従来どおり併せて鳴らす。
            // 両パターンは以下の既存分岐(研究/完成/世界史)の文言と重複しないため、
            // 先頭で処理しても既存分岐の挙動は変わらない。
            if (message.Contains("に宣戦布告した"))
            {
                PlayWarDeclared();
                if (human != null && message.Contains("「" + human.NameJa + "」"))
                    PlayAlert();   // 既存の人間向け警告を維持
                return;
            }
            if (message.Contains("が陥落した"))
            {
                PlayCityCaptured();
                // 陥落ログは文明名ではなく都市名を含むため、捕獲前スナップショットと照合する(D1修正)。
                if (human != null && ContainsHumanCityName(message))
                    PlayAlert();   // 既存の人間向け警告を維持
                return;
            }
            // --- 2026-07-21 Claude Code 追加: 和平スティング ---
            // 実ログ文言(Core/Player.cs MakePeaceWith): 「A」と「B」が和平した
            // 宣戦布告と同様、当事者を問わず(観戦モード含む全文明で)鳴らす。
            // 他の既存分岐の文言とは重複しないため、この位置での処理は既存挙動を変えない。
            if (message.Contains("が和平した"))
            {
                PlayPeaceMade();
                return;
            }

            if (human == null) return;

            if (message.StartsWith("「" + human.NameJa + "」", StringComparison.Ordinal) &&
                message.Contains("を研究した"))
            {
                PlayResearchComplete();
                return;
            }

            if (message.Contains("が完成した"))
            {
                for (int i = 0; i < human.Cities.Count; i++)
                {
                    if (message.Contains("「" + human.Cities[i].NameJa + "」"))
                    {
                        PlayProductionComplete();
                        return;
                    }
                }
            }

            // --- 2026-07-21 Claude Code 追加: 世界史イベントSE ---
            // 実ログ文言(Core側で確認済み):
            //   WorldLegacySystem: 「{文明}」が遺産「{名}」を発見！ 文化+... 科学+... 偉人P+...
            //   WorldLegacySystem: 「{文明}」が偉人「{名}」を登用！ {効果}...
            //   MasterpieceSystem: 「{文明}」が(偉人の活動により)作品「{名}」を収蔵した！ {効果}
            // 既存SEと同じ方針で、人間プレイヤー自身のイベントのみ鳴らす(観戦モードでは human==null
            // のため冒頭で復帰済み)。ミュート/SE音量は Play() が一括処理する。
            if (message.StartsWith("「" + human.NameJa + "」", StringComparison.Ordinal))
            {
                if (message.Contains("遺産「") && message.Contains("を発見"))
                {
                    PlayHeritageDiscovered();
                    return;
                }
                if (message.Contains("偉人「") && message.Contains("を登用"))
                {
                    PlayGreatPersonRecruited();
                    return;
                }
                if (message.Contains("を収蔵した"))
                {
                    PlayMasterpieceCollected();
                }
            }
        }

        // 2026-07-21 Claude Code 追加: 地域別BGMフレーバー対応のため、鐘メロディの音程表・
        // 減衰・拍アクセントを style から受け取る。既定スタイルでは従来と同一波形になる
        // (減衰倍率1・アクセント1は浮動小数点上も恒等)。和音進行・低音・テンポは共通。
        AudioClip CreateMusic(RegionalMusicStyle style)
        {
            const float duration = 24f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];

            float[] roots = { 220f, 174.61f, 130.81f, 196f };
            bool[] minor = { true, false, false, false };
            int[] melody = style.MelodyA;   // 既定 { 0, 3, 7, 10, 7, 3, 5, 7, 0, 3, 8, 7, 5, 3, 2, -2 }

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                int chordIndex = Mathf.Min(3, Mathf.FloorToInt(t / 6f));
                float chordTime = t - chordIndex * 6f;
                float root = roots[chordIndex];
                float third = root * (minor[chordIndex] ? 1.189207f : 1.259921f);
                float fifth = root * 1.498307f;

                float breathe = 0.74f + 0.26f * Mathf.Sin((chordTime / 6f) * Mathf.PI);
                float pad = Sine(root, t) + 0.62f * Sine(third, t) + 0.52f * Sine(fifth, t);
                float bass = Sine(root * 0.5f, t) * 0.46f;

                float beatLength = 1.5f;
                int beat = Mathf.FloorToInt(t / beatLength);
                float beatTime = t - beat * beatLength;
                float melodyFrequency = 440f * Mathf.Pow(2f, melody[beat % melody.Length] / 12f);
                float bell = (Sine(melodyFrequency, t) + 0.30f * Sine(melodyFrequency * 2f, t)) *
                    Mathf.Exp(-beatTime * (3.3f * style.BellDecayScale)) * style.AccentFor(beat);

                float loopFade = Mathf.Clamp01(t / 0.55f) * Mathf.Clamp01((duration - t) / 0.55f);
                data[i] = Mathf.Clamp((pad * 0.075f * breathe + bass * 0.08f + bell * 0.055f) * loopFade,
                    -0.75f, 0.75f);
            }

            return RegisterClip("Hex Empires - Dawn" + style.ClipSuffix, data);
        }

        /// <summary>
        /// 時代BGM第二曲(2026-07-21 Claude Code 追加)。CreateMusic と同じ構造・同じ長さ(24秒、
        /// 約2.1MB)で、コード進行を長調中心(C-G-Am-F)、鐘のメロディを1オクターブ上のハ長調系に
        /// 変えた「やや明るい」変奏。音量バランスは曲Aと揃え、クロスフェード時の段差を避ける。
        /// 2026-07-21 追加: 地域別フレーバーの鐘パラメータを style から受け取る(既定=従来波形)。
        /// </summary>
        AudioClip CreateMusicEraB(RegionalMusicStyle style)
        {
            const float duration = 24f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];

            float[] roots = { 261.63f, 196f, 220f, 174.61f };      // C - G - Am - F
            bool[] minor = { false, false, true, false };
            int[] melody = style.MelodyB;   // 既定 { 0, 4, 7, 12, 9, 7, 4, 5, 7, 9, 12, 9, 7, 5, 4, 2 }

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                int chordIndex = Mathf.Min(3, Mathf.FloorToInt(t / 6f));
                float chordTime = t - chordIndex * 6f;
                float root = roots[chordIndex];
                float third = root * (minor[chordIndex] ? 1.189207f : 1.259921f);
                float fifth = root * 1.498307f;

                float breathe = 0.76f + 0.24f * Mathf.Sin((chordTime / 6f) * Mathf.PI);
                float pad = Sine(root, t) + 0.62f * Sine(third, t) + 0.52f * Sine(fifth, t);
                float bass = Sine(root * 0.5f, t) * 0.42f;

                float beatLength = 1.5f;
                int beat = Mathf.FloorToInt(t / beatLength);
                float beatTime = t - beat * beatLength;
                float melodyFrequency = 523.25f * Mathf.Pow(2f, melody[beat % melody.Length] / 12f);
                float bell = (Sine(melodyFrequency, t) + 0.34f * Sine(melodyFrequency * 2f, t)) *
                    Mathf.Exp(-beatTime * (2.9f * style.BellDecayScale)) * style.AccentFor(beat);

                float loopFade = Mathf.Clamp01(t / 0.55f) * Mathf.Clamp01((duration - t) / 0.55f);
                data[i] = Mathf.Clamp((pad * 0.075f * breathe + bass * 0.08f + bell * 0.058f) * loopFade,
                    -0.75f, 0.75f);
            }

            return RegisterClip("Hex Empires - Golden Age" + style.ClipSuffix, data);
        }

        /// <summary>
        /// 終盤BGM第三曲(2026-07-21 Claude Code 追加)。ターン180超の終盤に流れる荘重な変奏。
        /// CreateMusic と同じ生成手法で、テンポを落とし(コード4秒・鐘2秒間隔)、短調中心の
        /// 進行(Am-F-Dm-E)と低めの鐘で「時代の黄昏」を表す。尺は16秒(約1.4MB)にとどめて
        /// 追加メモリを抑える。音量バランスは曲A/Bと揃え、クロスフェード時の段差を避ける。
        /// 2026-07-21 追加: 地域別フレーバーの鐘パラメータを style から受け取る(既定=従来波形)。
        /// </summary>
        AudioClip CreateMusicEraC(RegionalMusicStyle style)
        {
            const float duration = 16f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];

            float[] roots = { 220f, 174.61f, 146.83f, 164.81f };   // Am - F - Dm - E
            bool[] minor = { true, false, true, false };
            int[] melody = style.MelodyC;   // 既定 { 0, 3, 7, 5, 3, 2, 0, -2 }

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                int chordIndex = Mathf.Min(3, Mathf.FloorToInt(t / 4f));
                float chordTime = t - chordIndex * 4f;
                float root = roots[chordIndex];
                float third = root * (minor[chordIndex] ? 1.189207f : 1.259921f);
                float fifth = root * 1.498307f;

                float breathe = 0.72f + 0.28f * Mathf.Sin((chordTime / 4f) * Mathf.PI);
                float pad = Sine(root, t) + 0.62f * Sine(third, t) + 0.52f * Sine(fifth, t);
                float bass = Sine(root * 0.5f, t) * 0.50f;

                float beatLength = 2f;
                int beat = Mathf.FloorToInt(t / beatLength);
                float beatTime = t - beat * beatLength;
                float melodyFrequency = 330f * Mathf.Pow(2f, melody[beat % melody.Length] / 12f);
                float bell = (Sine(melodyFrequency, t) + 0.26f * Sine(melodyFrequency * 2f, t)) *
                    Mathf.Exp(-beatTime * (2.2f * style.BellDecayScale)) * style.AccentFor(beat);

                float loopFade = Mathf.Clamp01(t / 0.55f) * Mathf.Clamp01((duration - t) / 0.55f);
                data[i] = Mathf.Clamp((pad * 0.078f * breathe + bass * 0.085f + bell * 0.052f) * loopFade,
                    -0.75f, 0.75f);
            }

            return RegisterClip("Hex Empires - Twilight" + style.ClipSuffix, data);
        }

        /// <summary>
        /// 環境音(2026-07-21 Claude Code 追加)。BGMの下で流れるごく静かな風のループ(10秒)。
        /// 2段の一次ローパスで柔らかくしたノイズに、ループ周期と同期した遅いうねりを掛ける。
        /// うねりに合わせてローパスの係数も僅かに開閉し、風が強まる時に少し明るくなる質感にする。
        /// うねり・係数の変調はすべてループ周期の整数倍で位相が揃うため、ノイズ由来の継ぎ目は
        /// 末尾0.5秒を先頭へクロスフェードして消す。生成は初期化時に一度だけ(約0.9MB)。
        /// クリップ振幅はBGMより十分小さく、音源音量(BGM×0.25)と合わせて実効約-24dB相当。
        /// </summary>
        AudioClip CreateAmbience()
        {
            const float duration = 10f;
            const float TwoPi = 2f * Mathf.PI;
            int length = Mathf.CeilToInt(duration * SampleRate);
            int fade = Mathf.CeilToInt(0.5f * SampleRate);
            var raw = new float[length + fade];
            var random = new System.Random(20260722);
            float lp1 = 0f, lp2 = 0f;

            for (int i = 0; i < raw.Length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);

                // ループ周期と同期する遅いうねり(1周期の大きな風+3周期の小さな揺らぎ)
                float gust = 0.5f + 0.5f * Mathf.Sin(TwoPi * t / duration - 0.6f);
                float swell = 0.55f + 0.28f * Mathf.Sin(TwoPi * t / duration)
                    + 0.17f * Mathf.Sin(TwoPi * 3f * t / duration + 1.3f);

                // 2段の一次ローパス。係数はうねりに連動して開閉する(強い風ほど僅かに明るい)
                float a = 0.030f + 0.030f * gust;
                lp1 += (noise - lp1) * a;
                lp2 += (lp1 - lp2) * a;

                raw[i] = lp2 * swell * 0.8f;
            }

            // 末尾fade分を先頭へクロスフェードし、ノイズ由来のループ継ぎ目を消す
            // (うねり包絡は周期同期のため、末尾サンプルの音量感は先頭と一致している)
            for (int i = 0; i < fade; i++)
            {
                float w = i / (float)fade;
                raw[i] = raw[i] * w + raw[length + i] * (1f - w);
            }

            var data = new float[length];
            for (int i = 0; i < length; i++)
                data[i] = Mathf.Clamp(raw[i], -0.4f, 0.4f);

            return RegisterClip("Hex Empires - Wind Ambience", data);
        }

        AudioClip CreateMoveClip()
        {
            const float duration = 0.24f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var random = new System.Random(4061);
            float filteredNoise = 0f;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.16f);
                float tone = Triangle(Mathf.Lerp(310f, 190f, p), t) * 0.38f;
                data[i] = (tone + filteredNoise * 0.32f) * SoftEnvelope(p, 0.025f, 0.75f) * 0.38f;
            }

            return RegisterClip("Unit Move", data);
        }

        AudioClip CreateAttackClip()
        {
            const float duration = 0.34f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var random = new System.Random(9127);

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float thump = Sine(Mathf.Lerp(125f, 55f, p), t) * Mathf.Exp(-p * 7f);
                float metal = Triangle(730f, t) * Mathf.Exp(-p * 13f);
                float burst = noise * Mathf.Exp(-p * 18f);
                data[i] = Mathf.Clamp((thump * 0.62f + metal * 0.18f + burst * 0.32f) * 0.62f, -0.9f, 0.9f);
            }

            return RegisterClip("Combat Impact", data);
        }

        /// <summary>
        /// 戦闘解決イベントの受け口(2026-07-21 Claude Code 追加: ユニット種別別の戦闘SE)。
        /// Combat.PerformAttack の末尾から同期発火する表示専用イベントの購読で、
        /// シミュレーション状態は一切読み替え・変更しない(座標からの読み取りのみ)。
        /// </summary>
        void OnCombatResolvedSfx(HexCoord attackerCoord, HexCoord targetCoord, int dmgToDefender, int dmgToAttacker)
        {
            if (state == null) return;

            // InputController は攻撃直前に PlayAttack() を呼ぶ(=保留)。同フレームの戦闘解決は
            // その保留を消費し、汎用ヒット音の代わりに種別SEを鳴らす(人間自身の攻撃。
            // 視界内かつ高頻度になり得ないため、視界判定・間引きの対象外)。
            bool humanInitiated = pendingGenericAttackFrame == Time.frameCount;
            if (humanInitiated) pendingGenericAttackFrame = -1;

            if (!humanInitiated)
            {
                // AI同士などの戦闘は人間の視界内のみ鳴らす(EntityRenderer のダメージ数字表示と
                // 同じ基準。観戦モード human==null は全戦闘が対象)。従来この経路に戦闘音は
                // なかったため、視界外まで鳴らして「どこからともなく戦闘音」になるのを避ける。
                var human = state.HumanPlayer;
                bool audible = human == null
                    || human.Visible.Contains(targetCoord) || human.Visible.Contains(attackerCoord);
                if (!audible) return;

                // 1秒あたり最大8音の間引き(256倍速観戦などの戦闘殺到対策)。
                // 窓は非スケール時間で区切るため観戦倍速(Time.timeScale)の影響を受けない。
                float now = Time.unscaledTime;
                if (now - combatSoundWindowStart >= 1f)
                {
                    combatSoundWindowStart = now;
                    combatSoundCountInWindow = 0;
                }
                if (++combatSoundCountInWindow > MaxCombatSoundsPerSecond) return;
            }

            Play(SelectCombatClip(attackerCoord, targetCoord));
        }

        /// <summary>
        /// 攻撃側ユニットの種別から戦闘SEを選ぶ(2026-07-21 Claude Code 追加)。
        /// 攻撃側はイベントの「攻撃前」座標で引き、いなければ対象座標を見る(近接が敵を倒して
        /// 対象タイルへ移動済みのケース)。攻撃側が反撃で倒れた等で特定できない場合や民間人は、
        /// 従来の汎用ヒット音(attackClip)へフォールバックして無回帰にする。
        /// </summary>
        AudioClip SelectCombatClip(HexCoord attackerCoord, HexCoord targetCoord)
        {
            var map = state != null ? state.Map : null;
            var tile = map != null ? map.Get(attackerCoord) : null;
            var unit = tile != null ? tile.Unit : null;
            if (unit == null && map != null)
            {
                tile = map.Get(targetCoord);
                unit = tile != null ? tile.Unit : null;
            }

            var def = unit != null ? unit.Def : null;
            if (def == null || def.IsCivilian || (def.Strength <= 0 && def.RangedStrength <= 0))
                return attackClip;   // 不明・民間人: 従来の汎用ヒット音
            if (def.RangedStrength > 0)
                return def.Id == "catapult" ? siegeAttackClip : rangedAttackClip;
            return meleeAttackClip;   // 戦士・槍兵・剣士・斥候などの近接
        }

        /// <summary>
        /// 保留中の汎用ヒット音の後始末(2026-07-21 Claude Code 追加)。PlayAttack() と同フレームに
        /// 戦闘解決イベントが来なかった場合のみ、翌フレームに従来どおり汎用ヒット音を鳴らす。
        /// 通常は InputController 直後の Combat.PerformAttack が同フレームで解決するため、
        /// この経路は防御的なフォールバック(1フレーム≒16msの遅延は知覚できない)。
        /// </summary>
        void FlushPendingGenericAttack()
        {
            if (pendingGenericAttackFrame < 0 || Time.frameCount == pendingGenericAttackFrame) return;
            pendingGenericAttackFrame = -1;
            Play(attackClip);
        }

        /// <summary>
        /// カタパルト攻撃SE(2026-07-21 Claude Code 追加)。約0.5秒。
        /// 発射の深いホワンプ(急落する低域+短いノイズバースト)に、放物線落下を思わせる
        /// 下降ホイッスル(1400→450Hz)を続ける。掃引音は位相積算で生成し、周波数を直接
        /// 時刻に掛ける方式で起きる掃引の折り返しひずみを避ける。
        /// 汎用ヒット音(Combat Impact)より低く長く、発射→着弾の2段構成で聞き分けられる。
        /// </summary>
        AudioClip CreateSiegeAttackClip()
        {
            const float duration = 0.5f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var random = new System.Random(20260724);
            float whumpPhase = 0f;
            float whistlePhase = 0f;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);

                // 深いホワンプ: 95→36Hzへ急落する低域(位相積算)+発射の短いノイズバースト
                float whumpFrequency = Mathf.Lerp(95f, 36f, Mathf.Clamp01(t / 0.18f));
                whumpPhase += 2f * Mathf.PI * whumpFrequency / SampleRate;
                float sample = Mathf.Sin(whumpPhase) * Mathf.Exp(-t * 9f) * 0.95f
                    + noise * Mathf.Exp(-t * 34f) * 0.24f;

                // 落下ホイッスル: 0.10秒から1400→450Hzへ下降。弱いビブラートで空気感を足す
                float wt = t - 0.10f;
                if (wt >= 0f)
                {
                    float wp = Mathf.Clamp01(wt / 0.36f);
                    float whistleFrequency = Mathf.Lerp(1400f, 450f, wp) *
                        (1f + 0.010f * Mathf.Sin(2f * Mathf.PI * 24f * wt));
                    whistlePhase += 2f * Mathf.PI * whistleFrequency / SampleRate;
                    float whistleEnv = Mathf.Clamp01(wt / 0.05f) * Mathf.Lerp(0.30f, 0.06f, wp);
                    sample += Mathf.Sin(whistlePhase) * whistleEnv;
                }

                // クリップ末尾は短くフェードして途切れ音を防ぐ
                sample *= Mathf.Clamp01((duration - t) / 0.05f);
                data[i] = Mathf.Clamp(sample * 0.62f, -0.9f, 0.9f);
            }

            return RegisterClip("Siege Attack", data);
        }

        /// <summary>
        /// 遠隔攻撃SE(弓兵など。2026-07-21 Claude Code 追加)。約0.3秒。
        /// 矢が空気を切る短いヒュッ(2段ローパスの差分で帯域を絞った下降ノイズ)と、
        /// 突き刺さる木質のトック(短い低め実音+減衰の速い高域クリック)。
        /// 汎用ヒット音より軽く短く、金属クラッシュ(近接)とも質感で区別できる。
        /// </summary>
        AudioClip CreateRangedAttackClip()
        {
            const float duration = 0.30f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var random = new System.Random(20260725);
            float lp1 = 0f, lp2 = 0f;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float sample = 0f;

                // ヒュッ: 0〜0.15秒。ローパス係数を時間で下げて音色も下降させる(矢の通過感)
                if (t < 0.15f)
                {
                    float sp = t / 0.15f;
                    float a = Mathf.Lerp(0.42f, 0.16f, sp);
                    lp1 += (noise - lp1) * a;
                    lp2 += (lp1 - lp2) * a * 0.35f;
                    sample += (lp1 - lp2) * Mathf.Sin(sp * Mathf.PI) * 0.85f;
                }

                // トック: 0.15秒から。短い低めの実音+高域クリック+ごく短いノイズスナップ
                float tt = t - 0.15f;
                if (tt >= 0f)
                {
                    float body = Sine(205f, tt) * Mathf.Exp(-tt * 46f);
                    float click = Triangle(1150f, tt) * Mathf.Exp(-tt * 90f);
                    float snap = noise * Mathf.Exp(-tt * 120f);
                    sample += body * 0.9f + click * 0.30f + snap * 0.35f;
                }

                // クリップ末尾は短くフェードして途切れ音を防ぐ
                sample *= Mathf.Clamp01((duration - t) / 0.04f);
                data[i] = Mathf.Clamp(sample * 0.60f, -0.9f, 0.9f);
            }

            return RegisterClip("Arrow Attack", data);
        }

        /// <summary>
        /// 近接攻撃SE(戦士・槍兵・剣士・斥候。2026-07-21 Claude Code 追加)。約0.34秒。
        /// 明るい金属質の打ち合い2打(非整数倍音の重ね+速い減衰。2打目は少し低く弱く、
        /// 打ち返しに聞こえるように)+擦過ノイズ+打撃の重みの低いドン。
        /// 低域中心の汎用ヒット音(Combat Impact)より高域寄りで「刃が噛み合う」音にする。
        /// </summary>
        AudioClip CreateMeleeAttackClip()
        {
            const float duration = 0.34f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var random = new System.Random(20260726);

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);

                // 2打の金属トランジェント(t=0 と t=0.115秒)
                float sample = MetallicStrike(2350f, t) * 0.72f
                    + MetallicStrike(1880f, t - 0.115f) * 0.58f;

                // 擦過ノイズ(各打に短く付く)と、打撃の重みの低いドン
                float scrape = Mathf.Exp(-t * 60f) + (t >= 0.115f ? Mathf.Exp(-(t - 0.115f) * 60f) : 0f);
                float thump = Sine(105f, t) * Mathf.Exp(-t * 16f);
                sample += noise * scrape * 0.20f + thump * 0.30f;

                // クリップ末尾は短くフェードして途切れ音を防ぐ
                sample *= Mathf.Clamp01((duration - t) / 0.04f);
                data[i] = Mathf.Clamp(sample * 0.55f, -0.9f, 0.9f);
            }

            return RegisterClip("Melee Clash", data);
        }

        /// <summary>金属質の単打(非整数倍音1/1.83/2.71/3.92倍を速い減衰で重ねる。近接SE用)。</summary>
        static float MetallicStrike(float baseFrequency, float localTime)
        {
            if (localTime < 0f) return 0f;
            float sample = Sine(baseFrequency, localTime)
                + 0.70f * Sine(baseFrequency * 1.83f, localTime)
                + 0.45f * Sine(baseFrequency * 2.71f, localTime)
                + 0.28f * Sine(baseFrequency * 3.92f, localTime);
            return sample * Mathf.Exp(-localTime * 26f) * Mathf.Clamp01(localTime / 0.0025f);
        }

        /// <summary>
        /// 宣戦布告スティング(2026-07-21 Claude Code 追加)。
        /// 暗い二音のブラス風ヒット(A2→F2)。倍音を1/kで重ねた金管質感に、立ち上がりの
        /// 短いしゃくり上げを加える。既存のアルペジオ系チャイムより明確に低い音域。
        /// </summary>
        AudioClip CreateWarStingClip()
        {
            const float duration = 0.9f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                // 第1音 A2(110Hz)、第2音 F2(87.31Hz)。少し重ねて「ダ・ダーン」と繋げる。
                float sample = BrassHit(110f, t, 0.40f)
                    + BrassHit(87.31f, t - 0.30f, 0.60f) * 1.1f;
                data[i] = Mathf.Clamp(sample * 0.45f, -0.9f, 0.9f);
            }

            return RegisterClip("War Declaration Sting", data);
        }

        /// <summary>金管風の単音(倍音列1〜5倍を1/kで加算、短いアタック+緩い減衰)。</summary>
        static float BrassHit(float frequency, float localTime, float noteLength)
        {
            if (localTime < 0f || localTime >= noteLength) return 0f;
            float p = localTime / noteLength;
            // 立ち上がりで僅かに下からしゃくり上げる(金管のアタック感)
            float f = frequency * (1f - 0.06f * Mathf.Exp(-localTime * 55f));
            float sample = 0f;
            for (int k = 1; k <= 5; k++)
                sample += Sine(f * k, localTime) / k;
            float env = Mathf.Clamp01(localTime / 0.012f) * Mathf.Exp(-p * 3.2f) *
                Mathf.Clamp01((noteLength - localTime) / 0.08f);
            return sample * env;
        }

        /// <summary>
        /// 都市陥落スティング(2026-07-21 Claude Code 追加)。
        /// 重い太鼓風の打撃(ピッチが落ちる低域サイン+皮を叩くノイズ)に、
        /// 短い下降音(G4→C4)を続ける。宣戦布告スティングともチャイム群とも区別できる。
        /// </summary>
        AudioClip CreateCaptureStingClip()
        {
            const float duration = 1.0f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var random = new System.Random(20260721);

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;

                // 太鼓: ピッチが素早く落ちる低域サイン + 減衰の速いノイズバースト
                float thud = Sine(Mathf.Lerp(150f, 46f, Mathf.Clamp01(t / 0.22f)), t) *
                    Mathf.Exp(-t * 9f);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                float skin = noise * Mathf.Exp(-t * 40f);
                float sample = thud * 0.85f + skin * 0.30f;

                // 下降音: 0.18秒からG4(392Hz)→C4(261.63Hz)へ滑り落ちる短いトーン
                float toneTime = t - 0.18f;
                if (toneTime >= 0f)
                {
                    float dp = Mathf.Clamp01(toneTime / 0.55f);
                    float f = Mathf.Lerp(392f, 261.63f, dp);
                    float tone = (Sine(f, toneTime) + 0.25f * Triangle(f * 2f, toneTime)) *
                        Mathf.Exp(-toneTime * 4.5f) * Mathf.Clamp01(toneTime / 0.02f);
                    sample += tone * 0.34f;
                }

                // クリップ末尾は短くフェードして途切れ音を防ぐ
                sample *= Mathf.Clamp01((duration - t) / 0.06f);
                data[i] = Mathf.Clamp(sample * 0.62f, -0.9f, 0.9f);
            }

            return RegisterClip("City Capture Sting", data);
        }

        /// <summary>
        /// 和平スティング(2026-07-21 Claude Code 追加)。
        /// 柔らかく解決する二音(G3→C4)。正弦波中心の温かい音色で、第2音には長三度(E4)を
        /// 薄く重ねて安堵感を出す。低音金管で下降する宣戦布告スティングとは音域・進行・音色の
        /// いずれでも明確に区別できる。
        /// </summary>
        AudioClip CreatePeaceStingClip()
        {
            const float duration = 1.4f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                // 第1音 G3(196Hz)→第2音 C4(261.63Hz)。属音から主音への上行解決。
                float sample = WarmNote(196f, t, 0.55f)
                    + WarmNote(261.63f, t - 0.42f, 0.95f) * 1.05f
                    + WarmNote(329.63f, t - 0.42f, 0.95f) * 0.35f;   // 薄い長三度 E4
                data[i] = Mathf.Clamp(sample * 0.40f, -0.9f, 0.9f);
            }

            return RegisterClip("Peace Sting", data);
        }

        /// <summary>柔らかい単音(正弦波+弱い2倍音、緩やかなアタックと減衰。和平スティング用)。</summary>
        static float WarmNote(float frequency, float localTime, float noteLength)
        {
            if (localTime < 0f || localTime >= noteLength) return 0f;
            float p = localTime / noteLength;
            float sample = Sine(frequency, localTime) + 0.18f * Sine(frequency * 2f, localTime);
            float env = Mathf.Clamp01(localTime / 0.06f) * Mathf.Exp(-p * 2.6f) *
                Mathf.Clamp01((noteLength - localTime) / 0.12f);
            return sample * env;
        }

        /// <summary>
        /// 勝利ファンファーレ(2026-07-21 Claude Code 追加)。約2.5秒。
        /// 前半は上昇5音アルペジオ(C4-E4-G4-C5-E5、明るい倍音付き)、
        /// 1.25秒からCメジャーの柔らかい和音(C5+E5+G5+薄いC6)がゆるやかに立ち上がり、
        /// 減衰しながら末尾へ解決する。既存の短いVictoryアルペジオより長く祝祭的で、
        /// 音域・進行とも敗北SE(下降)とは明確に対照的。
        /// </summary>
        AudioClip CreateVictoryFanfareClip()
        {
            const float duration = 2.5f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            float[] arp = { 261.63f, 329.63f, 392f, 523.25f, 659.25f };   // C4-E4-G4-C5-E5
            float[] chord = { 523.25f, 659.25f, 783.99f, 1046.5f };       // C5+E5+G5+C6

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float sample = 0f;

                // 上昇アルペジオ(0.22秒間隔。短いアタック+緩い減衰)
                for (int n = 0; n < arp.Length; n++)
                {
                    float local = t - n * 0.22f;
                    if (local < 0f) continue;
                    float env = Mathf.Clamp01(local / 0.015f) * Mathf.Exp(-local * 3.4f);
                    sample += (Sine(arp[n], local) + 0.24f * Sine(arp[n] * 2f, local)) * env * 0.34f;
                }

                // 柔らかい和音(1.25秒から。ゆるいアタックで立ち上がり、末尾へ向けて減衰)
                float chordTime = t - 1.25f;
                if (chordTime >= 0f)
                {
                    float env = Mathf.Clamp01(chordTime / 0.18f) * Mathf.Exp(-chordTime * 1.1f)
                        * Mathf.Clamp01((duration - t) / 0.5f);
                    for (int n = 0; n < chord.Length; n++)
                        sample += Sine(chord[n], chordTime) * env * (n == 3 ? 0.10f : 0.22f);
                }

                // クリップ末尾は短くフェードして途切れ音を防ぐ
                sample *= Mathf.Clamp01((duration - t) / 0.06f);
                data[i] = Mathf.Clamp(sample * 0.9f, -0.9f, 0.9f);
            }

            return RegisterClip("Victory Fanfare", data);
        }

        /// <summary>
        /// 開幕ホルン(2026-07-21 Claude Code 追加)。約1.2秒。
        /// C4(261.63Hz)→G4(392Hz)の上行五度による柔らかい二音のホルンコール。
        /// 倍音を控えめ(2倍音0.30+3倍音0.12)にした丸い音色と、ゆるやかなアタックで
        /// 「夜明けの遠くの角笛」を表す。全体の音量は他のSEより小さく抑える。
        /// 二音とも上行で終わる点・音域・長さのいずれでも、勝利ファンファーレ(約2.5秒の
        /// アルペジオ+和音)や和平スティング(G3→C4)と聞き分けられる。
        /// </summary>
        AudioClip CreateOpeningHornClip()
        {
            const float duration = 1.2f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                // 第1音 C4 → 少し重ねて第2音 G4(上行五度のホルンコール)
                float sample = HornNote(261.63f, t, 0.55f)
                    + HornNote(392f, t - 0.38f, 0.80f) * 1.05f;
                data[i] = Mathf.Clamp(sample * 0.26f, -0.9f, 0.9f);
            }

            return RegisterClip("Opening Horn", data);
        }

        /// <summary>丸いホルン風の単音(基音+控えめな2・3倍音、ゆるいアタック。開幕ホルン用)。</summary>
        static float HornNote(float frequency, float localTime, float noteLength)
        {
            if (localTime < 0f || localTime >= noteLength) return 0f;
            float p = localTime / noteLength;
            float sample = Sine(frequency, localTime)
                + 0.30f * Sine(frequency * 2f, localTime)
                + 0.12f * Sine(frequency * 3f, localTime);
            float env = Mathf.Clamp01(localTime / 0.05f) * Mathf.Exp(-p * 2.4f) *
                Mathf.Clamp01((noteLength - localTime) / 0.10f);
            return sample * env;
        }

        /// <summary>
        /// パネル開閉音(2026-07-21 Claude Code 追加)。約0.22秒の柔らかい紙めくり風ウーシュ。
        /// 2段ローパスの係数を山形に開閉したノイズ(紙が空気を切る通過感)に、
        /// 紙のしなりを思わせる弱い上行トーンを薄く添える。音量は控えめで、
        /// UIクリック音(下降トリル)や移動音(下降トーン+ノイズ)と質感で区別できる。
        /// </summary>
        AudioClip CreatePanelOpenClip()
        {
            const float duration = 0.22f;
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            var random = new System.Random(20260728);
            float lp1 = 0f, lp2 = 0f;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float p = t / duration;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);

                // ローパス係数を山形に開閉(中央で最も明るく=「サッ」という紙の通過感)
                float a = Mathf.Lerp(0.10f, 0.55f, Mathf.Sin(p * Mathf.PI));
                lp1 += (noise - lp1) * a;
                lp2 += (lp1 - lp2) * a * 0.5f;
                float whoosh = (lp1 - lp2) * Mathf.Sin(p * Mathf.PI);

                // 紙のしなりの薄い実音(上行する弱い三角波)
                float flick = Triangle(Mathf.Lerp(340f, 520f, p), t) * Mathf.Exp(-p * 6f) * 0.10f;

                data[i] = Mathf.Clamp((whoosh * 0.9f + flick) * 0.5f, -0.8f, 0.8f);
            }

            return RegisterClip("Panel Page Flip", data);
        }

        /// <summary>
        /// ミニマップジャンプ音(2026-07-21 Claude Code 追加)。約0.09秒の小さな上行二音ブリップ
        /// (E5→B5)。既存のUIクリック音(940→700Hzの下降)とは進行が対照的で、
        /// 「地図上の別地点へ飛んだ」ことが耳でも分かる最小限の合図にする。
        /// </summary>
        AudioClip CreateMinimapJumpClip()
        {
            return CreateClip("Minimap Jump", 0.09f, t =>
            {
                float p = t / 0.09f;
                float f = p < 0.5f ? 659.25f : 987.77f;
                return (Triangle(f, t) * 0.7f + 0.2f * Sine(f * 2f, t)) * Decay(p, 7f) * 0.26f;
            });
        }

        AudioClip CreateArpeggio(string name, float duration, float[] notes, float spacing, float gain)
        {
            return CreateClip(name, duration, t =>
            {
                float sample = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float local = t - i * spacing;
                    if (local < 0f) continue;
                    float env = Mathf.Exp(-local * 4.2f);
                    sample += (Sine(notes[i], local) + 0.22f * Sine(notes[i] * 2f, local)) * env;
                }
                return Mathf.Clamp(sample * gain, -0.85f, 0.85f);
            });
        }

        AudioClip CreateClip(string name, float duration, Func<float, float> generator)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
                data[i] = Mathf.Clamp(generator(i / (float)SampleRate), -0.95f, 0.95f);
            return RegisterClip(name, data);
        }

        AudioClip RegisterClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            generatedClips.Add(clip);
            return clip;
        }

        static float Sine(float frequency, float time)
        {
            return Mathf.Sin(2f * Mathf.PI * frequency * time);
        }

        static float Triangle(float frequency, float time)
        {
            return 2f / Mathf.PI * Mathf.Asin(Mathf.Sin(2f * Mathf.PI * frequency * time));
        }

        static float Decay(float normalizedTime, float speed)
        {
            return Mathf.Exp(-normalizedTime * speed);
        }

        static float SoftEnvelope(float normalizedTime, float attack, float releaseStart)
        {
            float attackGain = Mathf.Clamp01(normalizedTime / Mathf.Max(0.001f, attack));
            float releaseGain = normalizedTime < releaseStart
                ? 1f
                : Mathf.Clamp01((1f - normalizedTime) / Mathf.Max(0.001f, 1f - releaseStart));
            return attackGain * releaseGain;
        }

        /// <summary>
        /// 地域別BGMフレーバーのパラメータ(2026-07-21 Claude Code 追加)。
        /// 変えるのは時代BGM3曲の「鐘メロディの音程表」「鐘の減衰速度」「拍アクセント」のみで、
        /// 和音進行・低音・テンポ・尺・音量バランスは全地域共通(=控えめな味付けにとどまる)。
        /// Default は現行実装と完全に同じ値(従来の音程表・減衰1倍・アクセントなし)のため、
        /// 既定経路の波形は従来と一致する。各音程表の長さはループ周期(曲A/B=16拍、曲C=8拍)と
        /// 一致または約数にして、ループの継ぎ目で旋律が途切れないようにする。
        /// </summary>
        sealed class RegionalMusicStyle
        {
            /// <summary>クリップ名への識別用付加(既定は空=従来のクリップ名のまま)。</summary>
            public readonly string ClipSuffix;
            /// <summary>曲A(Dawn、基準音A4)の鐘メロディ半音オフセット表。</summary>
            public readonly int[] MelodyA;
            /// <summary>曲B(Golden Age、基準音C5)の鐘メロディ半音オフセット表。</summary>
            public readonly int[] MelodyB;
            /// <summary>曲C(Twilight、基準音E4)の鐘メロディ半音オフセット表。</summary>
            public readonly int[] MelodyC;
            /// <summary>鐘の減衰速度倍率(1=従来。1未満は長く響き、1超は歯切れよく短い)。</summary>
            public readonly float BellDecayScale;
            /// <summary>拍ごとの鐘音量係数の循環表(null=全拍1=従来)。</summary>
            public readonly float[] BeatAccents;

            RegionalMusicStyle(string clipSuffix, int[] melodyA, int[] melodyB, int[] melodyC,
                float bellDecayScale, float[] beatAccents)
            {
                ClipSuffix = clipSuffix;
                MelodyA = melodyA;
                MelodyB = melodyB;
                MelodyC = melodyC;
                BellDecayScale = bellDecayScale;
                BeatAccents = beatAccents;
            }

            /// <summary>指定拍の鐘音量係数(アクセント表なしなら常に1=従来と同一)。</summary>
            public float AccentFor(int beat)
            {
                if (BeatAccents == null || BeatAccents.Length == 0) return 1f;
                return BeatAccents[beat % BeatAccents.Length];
            }

            /// <summary>既定=現行サウンド。ヨーロッパ・地中海、観戦モード、地域不明もこれを使う。</summary>
            public static readonly RegionalMusicStyle Default = new RegionalMusicStyle(
                "",
                new[] { 0, 3, 7, 10, 7, 3, 5, 7, 0, 3, 8, 7, 5, 3, 2, -2 },
                new[] { 0, 4, 7, 12, 9, 7, 4, 5, 7, 9, 12, 9, 7, 5, 4, 2 },
                new[] { 0, 3, 7, 5, 3, 2, 0, -2 },
                1f, null);

            /// <summary>東・東南アジア: 五音音階(短調系ペンタトニック、曲Bは長調系)の鐘と少し長い余韻。</summary>
            static readonly RegionalMusicStyle EastSoutheastAsia = new RegionalMusicStyle(
                " (東・東南アジア)",
                new[] { 0, 3, 5, 7, 10, 7, 5, 3, 0, 3, 7, 10, 12, 10, 7, -2 },
                new[] { 0, 4, 7, 12, 9, 7, 4, 2, 7, 9, 12, 9, 7, 4, 2, 0 },
                new[] { 0, 3, 7, 5, 3, 0, -2, -5 },
                0.92f, null);

            /// <summary>西・南アジア: 和声的短音階の色(増二度 8↔11 と導音 -1)を控えめに混ぜる。</summary>
            static readonly RegionalMusicStyle WestSouthAsia = new RegionalMusicStyle(
                " (西・南アジア)",
                new[] { 0, 3, 7, 8, 7, 5, 3, 2, 0, 3, 8, 11, 12, 8, 7, -1 },
                new[] { 0, 4, 7, 12, 8, 7, 4, 5, 7, 9, 12, 11, 7, 5, 4, 2 },
                new[] { 0, 3, 8, 7, 5, 2, 0, -1 },
                1f, null);

            /// <summary>アフリカ: 旋律は共通のまま、8拍循環のアクセントと短めの減衰でリズムの律動を出す。</summary>
            static readonly RegionalMusicStyle Africa = new RegionalMusicStyle(
                " (アフリカ)",
                Default.MelodyA, Default.MelodyB, Default.MelodyC,
                1.18f,
                new[] { 1.05f, 0.55f, 0.85f, 0.65f, 1f, 0.6f, 0.9f, 0.7f });

            /// <summary>アメリカ大陸: 完全四度・完全五度・オクターブ中心の開いた響き。</summary>
            static readonly RegionalMusicStyle Americas = new RegionalMusicStyle(
                " (アメリカ大陸)",
                new[] { 0, 7, 5, 12, 7, 0, 5, 7, 0, 5, 7, 12, 7, 5, 0, -5 },
                new[] { 0, 5, 7, 12, 7, 5, 0, 7, 5, 7, 12, 7, 5, 0, -5, 0 },
                new[] { 0, 5, 7, 12, 7, 5, 0, -5 },
                1f, null);

            /// <summary>オセアニア: 広い跳躍音程の鐘を1拍おき(アクセント{1,0})に、長い余韻で静かに鳴らす。</summary>
            static readonly RegionalMusicStyle Oceania = new RegionalMusicStyle(
                " (オセアニア)",
                new[] { 0, 7, 12, 7, 0, -5, -12, -5, 0, 12, 7, 12, 0, 7, -5, 0 },
                new[] { 0, 7, 12, 7, 0, -5, 0, 12, 7, 12, 0, 7, -5, 0, 7, 0 },
                new[] { 0, 7, 12, 7, 0, -5, -12, 0 },
                0.62f,
                new[] { 1f, 0f });

            /// <summary>共通6地域名(GlobalHistoryIndex.BroadRegion の戻り値)からスタイルを引く。</summary>
            public static RegionalMusicStyle ForRegion(string broadRegionJa)
            {
                switch (broadRegionJa)
                {
                    case "東・東南アジア": return EastSoutheastAsia;
                    case "西・南アジア": return WestSouthAsia;
                    case "アフリカ": return Africa;
                    case "アメリカ大陸": return Americas;
                    case "オセアニア": return Oceania;
                    default: return Default;   // ヨーロッパ・地中海と不明地域は現行サウンド
                }
            }
        }
    }
}
