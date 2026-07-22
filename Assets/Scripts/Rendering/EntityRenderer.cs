using System.Collections.Generic;
using HexCiv.Core;
using UnityEngine;

namespace HexCiv.Render
{
    /// <summary>
    /// ユニットと都市のビュー描画。Refresh() で状態と差分同期する(Id 単位で生成/破棄)。
    /// ユニット = プレイヤー色の円盤 + グリフ TextMesh + HPバー(損傷時のみ)。
    /// 都市 = タイル上の灰色ブロック + プレイヤー色バナー + 「名前 (人口)」+ HPバー。
    /// HumanPlayer の視界外ユニットは非表示。探索済みの都市はゴースト表示(霧の下に残る)。
    /// HumanPlayer が null(観戦モード)なら全て表示。
    ///
    /// 2026-07-21 追加(表示のみ。シミュレーション状態には一切書き込まない):
    /// ・ユニット移動トゥイーン(非スケール時間の指数平滑。3タイル超の瞬間移動や8倍速以上ではスナップ)
    /// ・戦闘演出(GameState.OnCombatResolved 購読: 突進・被弾フラッシュ・ダメージ数字ポップ)
    /// ・撃破フェードアウト、都市占領フラッシュ(OnCityCaptured 購読)
    /// ・都市名/ユニット名ラベルの高解像度化+暗色影コピー(ワールドサイズ不変・明るい地形での視認性向上)
    /// ・都市成長パルス(Refresh 時に人口増加を検知してバナーを一瞬拡大。Core への書き込みなし)
    /// ・ユニット出現ポップイン(前回差分に存在しなかった新規ユニットのみ。スケール0→約1.15→1、
    ///   非スケール約0.25秒。再Init直後の一括生成と8倍速以上では行わない)
    /// ・都市誕生バースト(前回差分に存在しなかった新規都市のみ。バナーがスケール0→約1.2→1で
    ///   ポップイン(非スケール約0.35秒)+金色パーティクル6個が外側へ飛散・フェード(約0.6秒、
    ///   プール再利用でウォームアップ後はアロケーションなし)。再Init直後と8倍速以上では行わない)
    /// ・ユニット待機アニメーション(2026-07-21 追加): 生存ユニットがワールドY±0.02・周期約2.5秒で
    ///   ゆっくり上下(位相はId ハッシュでずらす)。防御態勢中は上下ゆれの代わりにリングの金色
    ///   グローがゆっくり明滅する。8倍速以上(既存 SnapTimeScale 判定)と移動トゥイーン未収束・
    ///   突進・出現ポップ中はスキップ。毎フレームのアロケーションなし
    /// ・高さ対応(2026-07-21 追加): ユニット/都市ビュー・トゥイーン目標・ダメージ数字・誕生バースト
    ///   の基準YをRenderUtil.TileVisualHeight(丘陵の立体化)に合わせ、盛り上がったタイルの上に乗せる。
    ///   待機ボブは高さの上に加算。表示のみでシミュレーション座標は不変
    /// ・都市ビジュアル成長(2026-07-21 追加): 灰色ブロック1個の代わりに人口段階(1-2/3-4/5-7/8+)で
    ///   1〜4個+中央の高層ブロックのクラスタを表示。配置・色は都市Idから決定的(毎フレームの
    ///   UnityEngine.Random なし)。城壁("walls")建設済みなら暗石色の低い六角壁リングを追加。
    ///   人口段階か城壁状態が変わった時のみ再構築(都市Idごとにキャッシュ)
    /// ・占領都市の炎上(2026-07-21 追加。OnCityCaptured 購読): 占領された都市に暗灰色の煙4個
    ///   (各周期約1.2秒・位相ずらし・同じクアッドをループ再利用)+バナー下の淡いオレンジの
    ///   残り火グローを付与。占領時の state.TurnNumber を記録し、Update() が現在ターンを
    ///   ポーリングして5ターンかけて徐々に鎮火(+5ターンで完全消滅・エフェクト破棄)。
    ///   8倍速以上では開始せず、進行中も非表示化。都市ビューコンテナ(Root)の子に付けるため
    ///   Refresh 差分・ビュー破棄・再Init で自動的に片付く
    /// ・生産完成スパークル(2026-07-21 追加): 成長パルスと同じキャッシュ+ポーリング方式で
    ///   Buildings.Count の増加を検知し、金色スパークル5個が都市ブロックから上昇(約0.5秒。
    ///   都市誕生バーストのパーティクルプールを再利用)。生成/ロード直後と8倍速以上ではスキップ
    /// ・ユニット補給状態マーカー(2026-07-22 追加): Core/LogisticsSystem がターン開始時に確定した
    ///   Unit.Supply を読み取り、逼迫=琥珀の弧・孤立=ゆっくり明滅する赤いひし形を円盤の西側へ表示する
    ///   (補給良好はマーカーなし)。対象は人間プレイヤーのユニット、観戦モードでは全ユニット。
    ///   ビューごとに1個だけ遅延生成して使い回し、8倍速以上では非表示・軽量演出モードでは明滅を停止する
    /// ・軽量演出モード対応(2026-07-22 追加): VisualQuality.LightMode(PlayerPrefs
    ///   "HexCiv.FxLight")が有効な間は待機ボブと新規ダメージ数字ポップを抑制する。
    ///   戦闘の突進・被弾フラッシュ・撃破フェードなど既存演出は軽量モードでも維持する
    /// Init(state) 内でイベントを購読し、再Init/OnDestroy で必ず解除する。
    /// </summary>
    public class EntityRenderer : MonoBehaviour
    {
        // ユニットの基準高さ(契約 §6: units y=0.1、テキストはその上)
        const float UnitY = 0.1f;

        // ---- 描画順(MapRenderer: terrain0 / deco1 / border3 / highlight4 / fog8) ----
        // 都市は霧(8)より下 → 探索済み・視界外の都市は霧で薄暗くゴースト表示になる。
        const int SortCityBlock = 2;
        const int SortCityBanner = 5;
        const int SortCityHpBg = 6;
        const int SortCityHpFill = 7;
        const int SortCityText = 7;
        const int SortCityTextShadow = 6;   // 都市ラベル影(本体 SortCityText の直下。HPバーとは位置が離れており重ならない)
        // ユニットは視界内でのみ表示するため霧より上。
        const int SortUnitOutline = 9;
        const int SortUnitDisc = 10;
        const int SortUnitGlyph = 11;
        const int SortUnitNameShadow = 10;  // ユニット名影(本体 SortUnitGlyph の直下。円盤の外側 z=-0.52 にあり重なりは無視できる)
        const int SortUnitHpBg = 12;
        const int SortUnitHpFill = 13;
        // 補給状態マーカー(2026-07-22 追加)。円盤の西側 r=0.62〜0.76 に置くため、
        // 北の HPバー(12/13)・南の名前ラベル(10/11)とは幾何的に重ならない。
        const int SortUnitSupply = 12;
        // 演出用(既存の順序は変更しない。フラッシュと数字は既存要素の上に重ねる)
        const int SortCityFlash = 6;
        const int SortUnitFlash = 14;
        const int SortDamageText = 15;
        const int SortFoundParticle = 15;   // 都市誕生パーティクル(ダメージ数字と同層の一時演出)
        const int SortCityEmber = 4;        // 炎上の残り火グロー(バナー5の直下)
        const int SortCitySmoke = 7;        // 炎上の煙(霧8より下 = 視界外のゴースト都市では霧に霞む)

        // ---- 演出パラメータ(すべて表示のみ・非スケール時間) ----
        const float TweenSharpness = 18f;      // 指数平滑の強さ。1タイル移動が約0.15〜0.2秒で収束
        const float SnapDistanceSq = 27f;      // 約3タイル(5.2world)^2 を超える移動は即スナップ
        const float SnapTimeScale = 8f;        // 8倍速以上はトゥイーン無効(高速観戦の追従遅れ防止)
        const float LungeDuration = 0.15f;     // 攻撃の突進(往復)
        const float LungeDistance = 0.38f;
        const float HitFlashDuration = 0.36f;  // 被弾フラッシュ(白→赤の2回点滅)
        const float CaptureFlashDuration = 0.5f; // 都市占領の白フラッシュ
        const float DeathFadeDuration = 0.4f;  // 撃破フェード(縮小+透明化)
        const float DamageRise = 0.8f;         // ダメージ数字の上昇量
        const float DamageLife = 0.9f;         // ダメージ数字の寿命
        const int DamagePoolSize = 24;         // ダメージ数字プール(最古から再利用)
        const int MaxFxPerFrame = 10;          // 同一フレームの演出上限(32倍速対策)
        const float GrowthPulseDuration = 0.4f;  // 都市成長パルスの長さ
        const float GrowthPulseScale = 1.15f;    // 都市成長パルスの最大拡大率
        const float SpawnPopDuration = 0.25f;    // ユニット出現ポップの長さ
        const float SpawnPopBack = 2.17f;        // 出現ポップの easeOutBack 係数(頂点で約1.15倍)
        const float FoundPopDuration = 0.35f;    // 都市誕生ポップの長さ(バナー 0→約1.2→1)
        const float FoundPopBack = 2.6f;         // 誕生ポップの easeOutBack 係数(頂点で約1.2倍)
        const float FoundParticleLife = 0.6f;    // 誕生パーティクルの寿命
        const int FoundParticleCount = 6;        // 1回の誕生で飛ばす金色パーティクル数
        const int FoundParticlePoolSize = 18;    // 誕生パーティクルプール(3都市同時分。最古から再利用)
        const float FoundParticleY = 0.5f;       // 誕生パーティクルの基準高さ(バナーの少し下)

        // ---- 占領都市の炎上(2026-07-21 Claude Code 追加。表示のみ・非スケール時間) ----
        const float BurnPuffCycle = 1.2f;        // 煙1個の上昇→消滅の周期(位相をずらしてループ)
        const int BurnPuffCount = 4;             // 都市1つあたりの煙クアッド数
        const float BurnPuffBaseY = 0.24f;       // 煙の開始高さ(都市ブロックの上端付近)
        const float BurnPuffRise = 0.55f;        // 1周期の上昇量
        const float BurnSmokeMaxAlpha = 0.55f;   // 煙の最大アルファ(鎮火の進行でさらに減衰)
        const int BurnFadeTurns = 5;             // 占領から完全鎮火までのターン数
        const float BurnEmberMaxAlpha = 0.35f;   // 残り火グローの最大アルファ

        // ---- 生産完成スパークル(2026-07-21 Claude Code 追加。表示のみ) ----
        const float SparkleLife = 0.5f;          // スパークルの寿命
        const int SparkleCount = 5;              // 建物1件完成ごとの金色スパークル数
        const float SparkleBaseScale = 0.7f;     // 誕生パーティクルより小さめの基準スケール

        // ---- 待機アニメーション(2026-07-21 Claude Code 追加。表示のみ・非スケール時間) ----
        const float IdleBobAmplitude = 0.02f;    // 上下ゆれの振幅(ワールドY±0.02)
        const float IdleBobPeriod = 2.5f;        // 上下ゆれの周期(秒)
        const float FortifyGlowPeriod = 3.2f;    // 防御態勢リングのグロー明滅周期(秒)
        const float FortifyGlowMaxAlpha = 0.55f; // グローの最大アルファ
        const float TweenSettleSq = 0.0004f;     // 移動トゥイーン収束判定((2cm)^2)。未収束中は揺れない
        static readonly Color FortifyGlowColor = new Color(1f, 0.85f, 0.45f, 1f);   // グローの金色

        // ---- 補給状態マーカー(2026-07-22 Claude Code 追加。表示のみ・非スケール時間) ----
        // Core/LogisticsSystem がターン開始時に確定した Unit.Supply をそのまま読むだけで、
        // 判定も再計算も行わない(シミュレーションへは一切書き込まない)。
        // 補給良好 = マーカーなし / 逼迫 = 琥珀の弧(点灯したまま) / 孤立 = 赤いひし形がゆっくり明滅。
        // 対象は人間プレイヤーのユニット(観戦モード = HumanPlayer が null のときは全ユニット)。
        // マーカーはユニットビューごとに1個だけ遅延生成して以後は使い回し、
        // 補給良好へ戻ったら非表示にするだけ(ビュー破棄・再Initでは Root の子として一緒に片付く)。
        // 8倍速以上(既存 SnapTimeScale 判定)では他のFXと同様に非表示。軽量演出モード
        // (VisualQuality.LightMode)では明滅を止めて一定輝度で表示する(情報は残す)。
        const float SupplyMarkInner = 0.62f;      // 弧の内半径(防御態勢リング0.58・グロー0.60の外側)
        const float SupplyMarkOuter = 0.76f;      // 弧の外半径(タイル内接円0.866より内側)
        const float SupplyMarkStartDeg = 108f;    // 弧の開始角(西側を中心にする)
        const float SupplyMarkSweepDeg = 144f;    // 弧の開き
        const float SupplyIsolatedRadius = 0.66f; // 孤立マーカー(ひし形)の中心距離(弧と同じ西側)
        const float SupplyPulsePeriod = 1.6f;     // 孤立マーカーの明滅周期(秒。ゆっくり)
        const float SupplyPulseAlphaMin = 0.35f;
        const float SupplyPulseAlphaMax = 1.0f;
        static readonly Color SupplyStrainedColor = new Color(1f, 0.72f, 0.16f, 0.92f);  // 逼迫=琥珀
        static readonly Color SupplyIsolatedColor = new Color(1f, 0.24f, 0.18f, 1f);     // 孤立=赤

        // ラベル影のオフセット。親テキストは X+90° 回転済みのため、
        // ローカル(+0.02, -0.02, +0.005)はワールドの(+0.02, -0.005, -0.02)に相当する(画面上は右下)。
        static readonly Vector3 TextShadowOffset = new Vector3(0.02f, -0.02f, 0.005f);
        static readonly Color TextShadowColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);

        static readonly Color HpLow = new Color(0.85f, 0.15f, 0.10f, 1f);
        static readonly Color HpHigh = new Color(0.20f, 0.85f, 0.20f, 1f);
        static readonly Color DamageColor = new Color(1f, 0.22f, 0.18f, 1f);
        static readonly Color FoundParticleColor = new Color(1f, 0.82f, 0.30f, 1f);   // 都市誕生の金色
        static readonly Color BurnSmokeColor = new Color(0.22f, 0.22f, 0.25f, 1f);    // 炎上の暗灰色の煙
        static readonly Color BurnEmberColor = new Color(1f, 0.45f, 0.10f, 1f);       // 残り火のオレンジ
        static readonly Color SparkleColor = new Color(1f, 0.90f, 0.40f, 1f);         // 完成スパークルの金色
        static readonly Vector3[] BurnPuffOffsets =    // 煙の水平配置(決定的。Idに依らず固定で十分)
        {
            new Vector3(-0.16f, 0f, -0.10f),
            new Vector3( 0.15f, 0f,  0.05f),
            new Vector3(-0.03f, 0f,  0.17f),
            new Vector3( 0.09f, 0f, -0.15f),
        };

        static MaterialPropertyBlock mpb;
        static readonly int ColorPropId = Shader.PropertyToID("_Color");

        class UnitView
        {
            public GameObject Root;
            public GameObject FortifyRing;
            public TextMesh NameLabel;
            public TextMesh NameShadow;   // 名前ラベルの影(本体と同文字列を維持する)
            public GameObject HpRoot;
            public Transform HpFill;
            public Material HpFillMat;
            // ---- 演出状態 ----
            public HexCoord Coord;        // 最後に Refresh したシミュレーション座標
            public Vector3 TargetPos;     // シミュレーション上のワールド位置
            public Vector3 SmoothPos;     // トゥイーン中の表示位置
            public MeshRenderer Flash;    // 被弾フラッシュのオーバーレイ
            public float FlashTime;
            public float FlashDuration;
            public bool FlashWithRed;
            public float LungeTime;       // 残り時間(>0 で突進中)
            public Vector3 LungeDir;
            public float SpawnPopTime;    // 残り時間(>0 で出現ポップ中)
            public MeshRenderer FortifyGlow;   // 防御態勢の金色グローリング(待機アニメ。2026-07-21 追加)
            public float BobPhase;             // Id ハッシュ由来の揺れ位相(全ユニットが同期して見えないように)
            // ---- 補給状態マーカー(2026-07-22 追加。必要になった時だけ生成し以後は使い回す) ----
            public MeshRenderer SupplyMark;    // null = まだ一度も逼迫/孤立になっていない
            public MeshFilter SupplyFilter;    // 逼迫(弧)/孤立(ひし形)のメッシュ差し替え用
            public SupplyLevel SupplyShown = SupplyLevel.Supplied;   // 現在マーカーが表しているレベル
        }

        class CityView
        {
            public GameObject Root;
            public MeshRenderer Banner;
            public TextMesh Label;
            public TextMesh LabelShadow;  // 都市ラベルの影(本体と同文字列を維持する)
            public GameObject HpRoot;
            public Transform HpFill;
            public Material HpFillMat;
            // ---- 演出状態 ----
            public HexCoord Coord;
            public MeshRenderer Flash;    // バナーの上のフラッシュ
            public float FlashTime;
            public float FlashDuration;
            public bool FlashWithRed;
            public int LastPop = -1;      // 成長パルス用の人口キャッシュ(-1 = 未観測。生成/ロード直後はパルスしない)
            public float GrowthPulseTime; // 残り時間(>0 でバナー拡大パルス中)
            public float FoundPopTime;    // 残り時間(>0 で誕生ポップ中。バナー 0→約1.2→1)
            // ---- 建物クラスタ(2026-07-21 追加。人口段階・城壁状態が変わった時のみ再構築) ----
            public GameObject Cluster;    // ブロック群+城壁リングのルート
            public int ClusterTier = -1;  // 構築済みの人口段階(-1 = 未構築)
            public bool ClusterWalls;     // 構築済みの城壁有無
            // ---- 炎上(占領)エフェクト(2026-07-21 追加) ----
            public GameObject BurnRoot;        // 煙+残り火のルート(都市Rootの子。鎮火時に破棄)
            public MeshRenderer[] BurnPuffs;   // 煙クアッド(ループ再利用)
            public float[] BurnPuffPhase;      // 各煙の位相(0..1。周期をずらす)
            public MeshRenderer BurnEmber;     // バナー下の残り火グロー
            public int BurnCaptureTurn = -1;   // 占領されたターン(-1 = 炎上していない)
            // ---- 生産完成スパークル(2026-07-21 追加) ----
            public int LastBuildings = -1;     // 建物数キャッシュ(-1 = 未観測。生成/ロード直後は発火しない)
        }

        /// <summary>撃破フェード中の旧ビュー(状態からは既に消えている)。</summary>
        class FadingView
        {
            public GameObject Root;
            public float Age;
            public MeshRenderer[] Renderers;   // TextMesh 以外(MaterialPropertyBlock でフェード)
            public Color[] BaseColors;
            public TextMesh[] Texts;           // TextMesh.color でフェード
            public Color[] TextColors;
            public Material OwnedMat;          // ビュー固有マテリアル(フェード完了時に破棄)
        }

        class DamageText
        {
            public GameObject Go;
            public Transform Tr;
            public TextMesh Tm;
            public float Age;
            public bool Active;
            public Vector3 BasePos;
        }

        /// <summary>
        /// 都市誕生バースト/生産完成スパークルの金色パーティクル(プール。DamageText と同じ再利用方式)。
        /// Life/Tint/BaseScale はスポーン時に必ず設定される(2026-07-21 スパークル対応で追加)。
        /// </summary>
        class FoundParticle
        {
            public GameObject Go;
            public Transform Tr;
            public MeshRenderer Mr;
            public float Age;
            public bool Active;
            public Vector3 Origin;
            public Vector3 Velocity;   // 寿命内の総変位(easeOut で飛散/上昇)
            public float Life;         // 寿命(誕生バースト=FoundParticleLife / スパークル=SparkleLife)
            public Color Tint;         // 色(フェードは毎フレーム Tint から計算)
            public float BaseScale;    // 基準スケール(スパークルは小さめ)
        }

        GameState state;
        GameState subscribedState;
        Material baseMat;
        Font font;

        readonly Dictionary<int, UnitView> unitViews = new Dictionary<int, UnitView>();
        readonly Dictionary<int, CityView> cityViews = new Dictionary<int, CityView>();
        readonly Dictionary<Color, Material> colorMats = new Dictionary<Color, Material>();
        readonly HashSet<int> aliveIds = new HashSet<int>();
        readonly List<int> pruneBuf = new List<int>();
        readonly List<FadingView> fading = new List<FadingView>();

        DamageText[] damagePool;
        int damagePoolCursor;
        FoundParticle[] foundPool;   // 都市誕生パーティクル(初回バースト時に生成)
        int foundPoolCursor;
        int fxFrame = -1;
        int fxCountThisFrame;
        bool suppressSpawnPop;   // 再Init直後の一括生成中は出現ポップ/誕生バーストを抑止する

        // 共有メッシュ
        Mesh discMesh;
        Mesh outlineMesh;
        Mesh fortifyMesh;
        Mesh quadMesh;      // 中心原点の矩形(HPバー背景)
        Mesh fillMesh;      // 左端原点の矩形(HPバー本体、x スケールで増減)
        Mesh[] cityBlockMeshes;   // 都市クラスタの小ブロック(灰色を微妙に変えた3種)
        Mesh cityTallBlockMesh;   // 都市クラスタの中央高層ブロック(人口5以上)
        Mesh cityWallMesh;        // 城壁("walls")建設済み都市の六角壁リング(暗石色)
        Mesh bannerMesh;    // 都市バナー
        Mesh foundParticleMesh;   // 都市誕生パーティクルの小さな矩形
        Mesh fortifyGlowMesh;     // 防御態勢グロー用の白リング(色はMaterialPropertyBlockで着色)
        Mesh burnPuffMesh;        // 炎上の煙クアッド(色・フェードはMaterialPropertyBlockで着色)
        Mesh burnEmberMesh;       // 炎上の残り火グロー(バナー下の横長クアッド)
        Mesh supplyStrainedMesh;  // 補給逼迫の弧(頂点色は白。琥珀はMaterialPropertyBlockで着色)
        Mesh supplyIsolatedMesh;  // 補給孤立のひし形(内側=白・外周=暗色の二重。同上)

        /// <summary>初期化。再呼び出し(リスタート)にも対応。</summary>
        public void Init(GameState state)
        {
            UnsubscribeEvents();
            this.state = state;

            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            unitViews.Clear();
            cityViews.Clear();

            // フェード中ビューの子GOは上のループで破棄済み。固有マテリアルだけ破棄する。
            for (int i = 0; i < fading.Count; i++)
                if (fading[i].OwnedMat != null) Destroy(fading[i].OwnedMat);
            fading.Clear();
            damagePool = null;   // ダメージ数字プールも子ごと破棄されたため作り直す
            damagePoolCursor = 0;
            foundPool = null;    // 誕生パーティクルプールも同様に作り直す
            foundPoolCursor = 0;

            if (baseMat == null) baseMat = RenderUtil.NewSpriteMaterial();
            if (font == null) font = RenderUtil.JapaneseFont();

            if (discMesh == null)
            {
                discMesh = RenderUtil.BuildDisc(0.38f, 24, Color.white);
                outlineMesh = RenderUtil.BuildRing(0.36f, 0.46f, 24, new Color(0.08f, 0.08f, 0.10f, 1f));
                fortifyMesh = RenderUtil.BuildRing(0.48f, 0.58f, 24, new Color(0.05f, 0.05f, 0.05f, 0.90f));
                quadMesh = RenderUtil.BuildQuadXZ(1f, 1f, Color.white, false);
                fillMesh = RenderUtil.BuildQuadXZ(1f, 1f, Color.white, true);
                // 都市クラスタ用ブロック(2026-07-21 追加): 従来の単一ブロックと同系統の灰色を
                // 微妙に変えた3種+中央高層ブロック。城壁リングは暗い石色。
                cityBlockMeshes = new Mesh[]
                {
                    RenderUtil.BuildBox(0.30f, 0.20f, 0.30f,
                        new Color(0.68f, 0.68f, 0.72f, 1f), new Color(0.44f, 0.44f, 0.48f, 1f)),
                    RenderUtil.BuildBox(0.27f, 0.24f, 0.27f,
                        new Color(0.62f, 0.62f, 0.65f, 1f), new Color(0.40f, 0.40f, 0.43f, 1f)),
                    RenderUtil.BuildBox(0.32f, 0.17f, 0.32f,
                        new Color(0.72f, 0.72f, 0.75f, 1f), new Color(0.47f, 0.47f, 0.51f, 1f)),
                };
                cityTallBlockMesh = RenderUtil.BuildBox(0.26f, 0.46f, 0.26f,
                    new Color(0.75f, 0.75f, 0.78f, 1f), new Color(0.50f, 0.50f, 0.54f, 1f));
                cityWallMesh = BuildHexWallRing(0.54f, 0.64f, 0.13f,
                    new Color(0.42f, 0.41f, 0.38f, 1f), new Color(0.30f, 0.29f, 0.27f, 1f));
                bannerMesh = RenderUtil.BuildQuadXZ(2.2f, 0.5f, Color.white, false);
                foundParticleMesh = RenderUtil.BuildQuadXZ(0.14f, 0.14f, Color.white, false);
                // 待機アニメ用グローリング(2026-07-21 追加): 頂点色は白で作り、
                // 実際の色・アルファは MaterialPropertyBlock で毎フレーム変える(フラッシュと同方式)
                fortifyGlowMesh = RenderUtil.BuildRing(0.46f, 0.60f, 24, Color.white);
                // 炎上エフェクト用(2026-07-21 追加): 同じく頂点色は白で作り、
                // 煙の暗灰色・残り火のオレンジは MaterialPropertyBlock で毎フレーム変える
                burnPuffMesh = RenderUtil.BuildQuadXZ(0.20f, 0.20f, Color.white, false);
                burnEmberMesh = RenderUtil.BuildQuadXZ(2.4f, 0.62f, Color.white, false);
                // 補給状態マーカー(2026-07-22 追加)。共有メッシュを全ユニットで使い回す。
                supplyStrainedMesh = BuildArcRing(SupplyMarkInner, SupplyMarkOuter,
                    SupplyMarkStartDeg, SupplyMarkSweepDeg, 10, Color.white);
                supplyIsolatedMesh = BuildSupplyIsolatedMark();
            }

            SubscribeEvents(state);
            // 再Init(新規/リスタート/ロード)直後の一括生成では出現ポップを行わない。
            suppressSpawnPop = true;
            Refresh();
            suppressSpawnPop = false;
        }

        void OnDestroy()
        {
            UnsubscribeEvents();
        }

        void SubscribeEvents(GameState s)
        {
            if (s == null) return;
            s.OnCombatResolved += HandleCombatResolved;
            s.OnCityCaptured += HandleCityCaptured;
            subscribedState = s;
        }

        void UnsubscribeEvents()
        {
            if (subscribedState == null) return;
            subscribedState.OnCombatResolved -= HandleCombatResolved;
            subscribedState.OnCityCaptured -= HandleCityCaptured;
            subscribedState = null;
        }

        /// <summary>状態に合わせてユニット/都市ビューを生成・破棄・移動する。</summary>
        public void Refresh()
        {
            if (state == null) return;
            var human = state.HumanPlayer;

            // ---- ユニット ----
            aliveIds.Clear();
            foreach (var u in state.AllUnits)
            {
                aliveIds.Add(u.Id);
                UnitView v;
                if (!unitViews.TryGetValue(u.Id, out v))
                {
                    v = CreateUnitView(u);
                    // 生成(スポーン)時はトゥイーンせず即座に配置(丘陵タイルの視覚高さに乗せる)
                    var wp = u.Coord.ToWorld(); wp.y = TileHeightAt(u.Coord);
                    v.SmoothPos = wp;
                    v.Root.transform.localPosition = wp;
                    unitViews.Add(u.Id, v);
                    // 前回差分に存在しなかった新規ユニットの出現ポップ(表示のみ)。
                    // 再Init直後の一括生成と8倍速以上(移動トゥイーンと同じ判定)ではスキップ。
                    if (!suppressSpawnPop && Time.timeScale < SnapTimeScale)
                    {
                        v.SpawnPopTime = SpawnPopDuration;
                        v.Root.transform.localScale = Vector3.zero;
                    }
                }
                UpdateUnitView(v, u, human);
            }
            pruneBuf.Clear();
            foreach (var kv in unitViews)
                if (!aliveIds.Contains(kv.Key)) pruneBuf.Add(kv.Key);
            for (int i = 0; i < pruneBuf.Count; i++)
            {
                var v = unitViews[pruneBuf[i]];
                if (v.Root != null && v.Root.activeSelf && Time.timeScale < SnapTimeScale)
                {
                    // 表示中に消えたユニットはフェードアウト(HpFillMat はフェード完了時に破棄)
                    BeginDeathFade(v);
                }
                else
                {
                    if (v.Root != null) Destroy(v.Root);
                    if (v.HpFillMat != null) Destroy(v.HpFillMat);
                }
                unitViews.Remove(pruneBuf[i]);
            }

            // ---- 都市 ----
            aliveIds.Clear();
            foreach (var c in state.AllCities)
            {
                aliveIds.Add(c.Id);
                CityView v;
                bool created = false;
                if (!cityViews.TryGetValue(c.Id, out v))
                {
                    v = CreateCityView();
                    cityViews.Add(c.Id, v);
                    created = true;
                }
                UpdateCityView(v, c, human);
                // 都市誕生バースト(表示のみ): 前回差分に存在しなかった新規都市のみ。
                // 再Init直後の一括生成と8倍速以上(他の演出と同じ判定)ではスキップ。
                // 占領は同じ Id のビューを使い回すため誕生扱いにならない。
                if (created && !suppressSpawnPop && Time.timeScale < SnapTimeScale)
                {
                    v.FoundPopTime = FoundPopDuration;
                    if (v.Banner != null)
                        v.Banner.transform.localScale = new Vector3(0f, 1f, 0f);
                    // パーティクルは表示中(探索済み)の都市のみ。バナーのポップは非表示でも
                    // タイマーを進め、Update() 終了時に必ず等倍へ戻す。
                    if (v.Root != null && v.Root.activeSelf)
                    {
                        var wp = c.Coord.ToWorld();
                        wp.y = TileHeightAt(c.Coord);   // 丘陵上の都市は高い位置からバースト
                        SpawnFoundingBurst(wp);
                    }
                }
            }
            pruneBuf.Clear();
            foreach (var kv in cityViews)
                if (!aliveIds.Contains(kv.Key)) pruneBuf.Add(kv.Key);
            for (int i = 0; i < pruneBuf.Count; i++)
            {
                var v = cityViews[pruneBuf[i]];
                if (v.Root != null) Destroy(v.Root);
                if (v.HpFillMat != null) Destroy(v.HpFillMat);
                cityViews.Remove(pruneBuf[i]);
            }
        }

        // ------------------------------------------------------------------
        // 毎フレームの演出更新(表示のみ。GameState には一切書き込まない)
        // ------------------------------------------------------------------

        void Update()
        {
            if (state == null) return;
            float dt = Time.unscaledDeltaTime;
            if (mpb == null) mpb = new MaterialPropertyBlock();

            bool snapAll = Time.timeScale >= SnapTimeScale;
            float k = 1f - Mathf.Exp(-TweenSharpness * dt);
            // 軽量演出モード(2026-07-22 追加): 待機ボブのみ抑制する(戦闘の突進・フラッシュ、
            // 撃破フェード、防御態勢グローなどの既存演出はそのまま)。フレームに1回だけ読む。
            bool lightFx = VisualQuality.LightMode;

            // ---- ユニット: 出現ポップ + 位置トゥイーン + 突進 + 被弾フラッシュ ----
            foreach (var kv in unitViews)
            {
                var v = kv.Value;
                if (v.Root == null) continue;

                // 出現ポップ: スケール 0→約1.15→1(easeOutBack)。霧内スポーン等の非表示中も
                // 時間は進め、終了時(または8倍速以上への切替時)は必ず等倍へ戻す。
                if (v.SpawnPopTime > 0f)
                {
                    v.SpawnPopTime -= dt;
                    if (v.SpawnPopTime <= 0f || snapAll)
                    {
                        v.SpawnPopTime = 0f;
                        v.Root.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        float su = -(v.SpawnPopTime / SpawnPopDuration);   // -1(開始)..0(終了)
                        float s = 1f + (SpawnPopBack + 1f) * su * su * su + SpawnPopBack * su * su;
                        v.Root.transform.localScale = new Vector3(s, s, s);
                    }
                }

                if (!v.Root.activeSelf) continue;

                if (snapAll || (v.TargetPos - v.SmoothPos).sqrMagnitude > SnapDistanceSq)
                    v.SmoothPos = v.TargetPos;
                else
                    v.SmoothPos = Vector3.Lerp(v.SmoothPos, v.TargetPos, k);

                var pos = v.SmoothPos;
                bool lunging = v.LungeTime > 0f;
                if (lunging)
                {
                    v.LungeTime -= dt;
                    float lt = 1f - Mathf.Clamp01(v.LungeTime / LungeDuration);
                    pos += v.LungeDir * (Mathf.Sin(lt * Mathf.PI) * LungeDistance);
                }

                // ---- 待機アニメーション(2026-07-21 追加。表示のみ・アロケーションなし) ----
                // 8倍速以上(snapAll=既存の移動トゥイーン無効判定と同一)・移動トゥイーン未収束・
                // 突進中・出現ポップ中はスキップ。SmoothPos.y はタイルの視覚高さ(丘陵)を含む
                // 基準値で、揺れはその上に加算する。適用しないフレームでは位置が自然に基準へ戻る。
                bool idle = !snapAll && !lunging && v.SpawnPopTime <= 0f
                    && (v.TargetPos - v.SmoothPos).sqrMagnitude <= TweenSettleSq;
                bool fortified = v.FortifyRing != null && v.FortifyRing.activeSelf;
                if (idle && !fortified && !lightFx)
                {
                    // 非防御の生存ユニット: ゆっくり上下(周期約2.5秒・位相はIdハッシュ)。
                    // 軽量演出モード中はスキップ(位置は毎フレーム基準値から計算するため自然に静止する)
                    pos.y += IdleBobAmplitude * Mathf.Sin(
                        Time.unscaledTime * (Mathf.PI * 2f / IdleBobPeriod) + v.BobPhase);
                }
                bool glowOn = idle && fortified;
                if (v.FortifyGlow != null)
                {
                    if (v.FortifyGlow.gameObject.activeSelf != glowOn)
                        v.FortifyGlow.gameObject.SetActive(glowOn);
                    if (glowOn)
                    {
                        // 防御態勢中: 上下ゆれの代わりにリングの金色グローをゆっくり明滅させる
                        var gc = FortifyGlowColor;
                        gc.a = FortifyGlowMaxAlpha * (0.5f + 0.5f * Mathf.Sin(
                            Time.unscaledTime * (Mathf.PI * 2f / FortifyGlowPeriod) + v.BobPhase));
                        mpb.SetColor(ColorPropId, gc);
                        v.FortifyGlow.SetPropertyBlock(mpb);
                    }
                }

                v.Root.transform.localPosition = pos;

                // ---- 補給状態マーカー(2026-07-22 追加。表示のみ・アロケーションなし) ----
                // 8倍速以上(snapAll)は他のFXと同じく非表示。逼迫は一定の琥珀、孤立は赤の
                // ゆっくりした明滅(位相は待機ボブと同じ Id ハッシュ)。軽量演出モードでは
                // 明滅を止めて最大輝度で固定し、情報表示としては残す。
                if (v.SupplyMark != null)
                {
                    bool showMark = v.SupplyShown != SupplyLevel.Supplied && !snapAll;
                    if (v.SupplyMark.gameObject.activeSelf != showMark)
                        v.SupplyMark.gameObject.SetActive(showMark);
                    if (showMark)
                    {
                        Color sc;
                        if (v.SupplyShown == SupplyLevel.Isolated)
                        {
                            sc = SupplyIsolatedColor;
                            sc.a = lightFx
                                ? SupplyPulseAlphaMax
                                : Mathf.Lerp(SupplyPulseAlphaMin, SupplyPulseAlphaMax,
                                    0.5f + 0.5f * Mathf.Sin(Time.unscaledTime
                                        * (Mathf.PI * 2f / SupplyPulsePeriod) + v.BobPhase));
                        }
                        else
                        {
                            sc = SupplyStrainedColor;
                        }
                        mpb.SetColor(ColorPropId, sc);
                        v.SupplyMark.SetPropertyBlock(mpb);
                    }
                }

                if (v.FlashTime > 0f)
                    TickFlash(v.Flash, ref v.FlashTime, v.FlashDuration, v.FlashWithRed, dt);
            }

            // ---- 都市: 誕生ポップ + フラッシュ(被弾・占領) + 成長パルス ----
            foreach (var kv in cityViews)
            {
                var v = kv.Value;
                if (v.Root == null) continue;

                // 誕生ポップ: バナーをスケール0→約1.2→1(easeOutBack)。非表示中も時間は進め、
                // 終了時(または8倍速以上への切替時)は必ず等倍へ戻す(ユニット出現ポップと同方式)。
                if (v.FoundPopTime > 0f && v.Banner != null)
                {
                    v.FoundPopTime -= dt;
                    if (v.FoundPopTime <= 0f || snapAll)
                    {
                        v.FoundPopTime = 0f;
                        v.Banner.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        float fu = -(v.FoundPopTime / FoundPopDuration);   // -1(開始)..0(終了)
                        float s = 1f + (FoundPopBack + 1f) * fu * fu * fu + FoundPopBack * fu * fu;
                        v.Banner.transform.localScale = new Vector3(s, 1f, s);
                    }
                }

                if (!v.Root.activeSelf) continue;
                if (v.FlashTime > 0f)
                    TickFlash(v.Flash, ref v.FlashTime, v.FlashDuration, v.FlashWithRed, dt);

                // 人口増加パルス: バナーを sin 波で一瞬拡大し、終了時は必ず等倍へ戻す
                // (誕生ポップ中はバナースケールの競合を避けるため待機し、ポップ完了後に再生する)
                if (v.GrowthPulseTime > 0f && v.Banner != null && v.FoundPopTime <= 0f)
                {
                    v.GrowthPulseTime -= dt;
                    if (v.GrowthPulseTime <= 0f)
                    {
                        v.GrowthPulseTime = 0f;
                        v.Banner.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        float gt = 1f - v.GrowthPulseTime / GrowthPulseDuration;   // 0..1
                        float s = 1f + (GrowthPulseScale - 1f) * Mathf.Sin(gt * Mathf.PI);
                        v.Banner.transform.localScale = new Vector3(s, 1f, s);
                    }
                }

                // 占領都市の炎上(2026-07-21 追加): 煙の上昇ループ+残り火の明滅。
                // 現在ターンをポーリングし、占領から5ターンかけて徐々に鎮火する(表示のみ)。
                if (v.BurnCaptureTurn >= 0)
                    TickBurning(v, snapAll);
            }

            // ---- ダメージ数字: 上昇 + フェード ----
            if (damagePool != null)
            {
                for (int i = 0; i < damagePool.Length; i++)
                {
                    var d = damagePool[i];
                    if (!d.Active) continue;
                    d.Age += dt;
                    float t = d.Age / DamageLife;
                    if (t >= 1f || d.Go == null)
                    {
                        d.Active = false;
                        if (d.Go != null) d.Go.SetActive(false);
                        continue;
                    }
                    var p = d.BasePos;
                    p.y += DamageRise * t;
                    d.Tr.localPosition = p;
                    var c = DamageColor;
                    c.a = 1f - t * t;
                    d.Tm.color = c;
                }
            }

            // ---- 都市誕生/生産完成パーティクル: 飛散・上昇(easeOut) + 縮小 + フェード ----
            // (寿命・色・基準スケールはスポーン時に設定された fp.Life / fp.Tint / fp.BaseScale を使う)
            if (foundPool != null)
            {
                for (int i = 0; i < foundPool.Length; i++)
                {
                    var fp = foundPool[i];
                    if (!fp.Active) continue;
                    fp.Age += dt;
                    float t = fp.Age / Mathf.Max(0.0001f, fp.Life);
                    if (t >= 1f || fp.Go == null)
                    {
                        fp.Active = false;
                        if (fp.Go != null) fp.Go.SetActive(false);
                        continue;
                    }
                    float ease = 1f - (1f - t) * (1f - t);   // 勢いよく飛び出して減速
                    fp.Tr.localPosition = fp.Origin + fp.Velocity * ease;
                    float sc = fp.BaseScale * (1f - 0.45f * t);
                    fp.Tr.localScale = new Vector3(sc, 1f, sc);
                    var c = fp.Tint;
                    c.a = 1f - t * t;
                    mpb.SetColor(ColorPropId, c);
                    fp.Mr.SetPropertyBlock(mpb);
                }
            }

            // ---- 撃破フェード ----
            for (int i = fading.Count - 1; i >= 0; i--)
            {
                var f = fading[i];
                f.Age += dt;
                float t = f.Age / DeathFadeDuration;
                if (t >= 1f || f.Root == null)
                {
                    if (f.Root != null) Destroy(f.Root);
                    if (f.OwnedMat != null) Destroy(f.OwnedMat);
                    fading.RemoveAt(i);
                    continue;
                }
                float alpha = 1f - t;
                f.Root.transform.localScale = Vector3.one * (1f - 0.55f * t);
                for (int r = 0; r < f.Renderers.Length; r++)
                {
                    var mr = f.Renderers[r];
                    if (mr == null) continue;
                    var c = f.BaseColors[r];
                    c.a *= alpha;
                    mpb.SetColor(ColorPropId, c);
                    mr.SetPropertyBlock(mpb);
                }
                for (int x = 0; x < f.Texts.Length; x++)
                {
                    var tm = f.Texts[x];
                    if (tm == null) continue;
                    var c = f.TextColors[x];
                    c.a *= alpha;
                    tm.color = c;
                }
            }
        }

        // ------------------------------------------------------------------
        // 戦闘イベント(GameState 購読)
        // ------------------------------------------------------------------

        void HandleCombatResolved(HexCoord attackerCoord, HexCoord targetCoord, int dmgToDefender, int dmgToAttacker)
        {
            // 同一フレームに大量発生した場合は演出を間引く(高倍速観戦対策)
            if (Time.frameCount != fxFrame)
            {
                fxFrame = Time.frameCount;
                fxCountThisFrame = 0;
            }
            if (++fxCountThisFrame > MaxFxPerFrame) return;

            // ダメージ数字等の基準は丘陵タイルの視覚高さに合わせる(表示のみ)
            Vector3 atkPos = attackerCoord.ToWorld(); atkPos.y = TileHeightAt(attackerCoord);
            Vector3 tgtPos = targetCoord.ToWorld(); tgtPos.y = TileHeightAt(targetCoord);

            // 攻撃側: 対象へ向けて突進して戻る(突進方向は水平のみ。高さは基準位置が持つ)
            var atkView = FindUnitViewAt(attackerCoord);
            if (atkView != null && atkView.Root != null && atkView.Root.activeSelf)
            {
                var dir = tgtPos - atkPos;
                dir.y = 0f;
                atkView.LungeTime = LungeDuration;
                atkView.LungeDir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.zero;
            }

            // 防御側: 白→赤の点滅(ユニット)またはバナー点滅(都市)
            var defView = FindUnitViewAt(targetCoord);
            if (defView != null && defView.Root != null && defView.Root.activeSelf)
            {
                StartFlash(defView.Flash, out defView.FlashTime, out defView.FlashDuration,
                    out defView.FlashWithRed, HitFlashDuration, true);
            }
            else
            {
                var cityView = FindCityViewAt(targetCoord);
                if (cityView != null && cityView.Root != null && cityView.Root.activeSelf)
                {
                    StartFlash(cityView.Flash, out cityView.FlashTime, out cityView.FlashDuration,
                        out cityView.FlashWithRed, HitFlashDuration, true);
                }
            }

            // ダメージ数字(人間の視界内のみ。観戦モードは常に表示)
            var human = state != null ? state.HumanPlayer : null;
            bool show = human == null
                || human.Visible.Contains(targetCoord) || human.Visible.Contains(attackerCoord);
            if (show)
            {
                if (dmgToDefender > 0) SpawnDamageText(tgtPos, dmgToDefender);
                if (dmgToAttacker > 0) SpawnDamageText(atkPos, dmgToAttacker);  // 近接の反撃は攻撃側にも表示
            }
        }

        void HandleCityCaptured(City city, Player oldOwner, Player newOwner)
        {
            CityView v;
            if (city == null || !cityViews.TryGetValue(city.Id, out v) || v.Root == null)
                return;
            if (v.Root.activeSelf)
            {
                StartFlash(v.Flash, out v.FlashTime, out v.FlashDuration,
                    out v.FlashWithRed, CaptureFlashDuration, false);
            }
            // 炎上開始(2026-07-21 追加。表示のみ): 占領ターンを記録し、Update() の TickBurning が
            // 以後5ターンかけて鎮火させる。8倍速以上では開始しない(他FXと同じ判定)。
            // 視界外(Root非表示)でも記録は行い、探索した時点でまだ燃えていれば煙が見える。
            if (state != null && Time.timeScale < SnapTimeScale)
            {
                v.BurnCaptureTurn = state.TurnNumber;
                EnsureBurnFx(v);
            }
        }

        UnitView FindUnitViewAt(HexCoord coord)
        {
            foreach (var kv in unitViews)
                if (kv.Value.Coord == coord) return kv.Value;
            return null;
        }

        CityView FindCityViewAt(HexCoord coord)
        {
            foreach (var kv in cityViews)
                if (kv.Value.Coord == coord) return kv.Value;
            return null;
        }

        // ------------------------------------------------------------------
        // フラッシュ(共有マテリアル + MaterialPropertyBlock。マテリアルは生成しない)
        // ------------------------------------------------------------------

        static void StartFlash(MeshRenderer flash, out float time, out float duration,
            out bool withRed, float dur, bool red)
        {
            time = dur;
            duration = dur;
            withRed = red;
            if (flash == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            mpb.SetColor(ColorPropId, new Color(1f, 1f, 1f, 0f));
            flash.SetPropertyBlock(mpb);
            flash.gameObject.SetActive(true);
        }

        static void TickFlash(MeshRenderer flash, ref float time, float duration, bool withRed, float dt)
        {
            if (flash == null) { time = 0f; return; }
            time -= dt;
            if (time <= 0f)
            {
                time = 0f;
                flash.gameObject.SetActive(false);
                return;
            }
            float t = 1f - time / Mathf.Max(0.0001f, duration);   // 0..1
            // 2回の点滅。前半は白、後半は赤(withRed 時のみ)
            float pulse = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f));
            Color c = (withRed && t >= 0.5f)
                ? new Color(1f, 0.25f, 0.20f, 0.72f * pulse)
                : new Color(1f, 1f, 1f, 0.72f * pulse);
            mpb.SetColor(ColorPropId, c);
            flash.SetPropertyBlock(mpb);
        }

        // ------------------------------------------------------------------
        // ダメージ数字(プール。最古から再利用)
        // ------------------------------------------------------------------

        void SpawnDamageText(Vector3 worldPos, int dmg)
        {
            if (dmg <= 0) return;
            // 軽量演出モード(2026-07-22 追加): 新規のダメージ数字ポップを出さない
            // (表示中の数字は既存の寿命処理で自然に消える)
            if (VisualQuality.LightMode) return;
            if (damagePool == null)
            {
                damagePool = new DamageText[DamagePoolSize];
                for (int i = 0; i < DamagePoolSize; i++) damagePool[i] = CreateDamageText();
            }
            var d = damagePool[damagePoolCursor];                 // ラウンドロビン = 最古を再利用
            damagePoolCursor = (damagePoolCursor + 1) % DamagePoolSize;
            if (d.Go == null) return;
            d.Active = true;
            d.Age = 0f;
            // わずかな水平ジッターで重なりを避ける(表示のみなので UnityEngine.Random で可)
            d.BasePos = worldPos + new Vector3(
                Random.Range(-0.18f, 0.18f), 1.0f, Random.Range(-0.12f, 0.12f));
            d.Tm.text = "-" + dmg;
            d.Tm.color = DamageColor;
            d.Tr.localPosition = d.BasePos;
            d.Go.SetActive(true);
        }

        DamageText CreateDamageText()
        {
            var d = new DamageText();
            var tm = CreateText(transform, "DamageText", Vector3.zero, "", 52, 0.055f,
                DamageColor, SortDamageText);
            d.Go = tm.gameObject;
            d.Tr = tm.transform;
            d.Tm = tm;
            d.Go.SetActive(false);
            return d;
        }

        // ------------------------------------------------------------------
        // 都市誕生バースト(プール。最古から再利用。表示のみ)
        // ------------------------------------------------------------------

        /// <summary>金色パーティクル6個を都市位置から放射状に飛ばす(ウォームアップ後はアロケーションなし)。</summary>
        void SpawnFoundingBurst(Vector3 worldPos)
        {
            if (foundPool == null)
            {
                foundPool = new FoundParticle[FoundParticlePoolSize];
                for (int i = 0; i < FoundParticlePoolSize; i++) foundPool[i] = CreateFoundParticle();
            }
            if (mpb == null) mpb = new MaterialPropertyBlock();

            for (int i = 0; i < FoundParticleCount; i++)
            {
                var fp = foundPool[foundPoolCursor];                  // ラウンドロビン = 最古を再利用
                foundPoolCursor = (foundPoolCursor + 1) % FoundParticlePoolSize;
                if (fp.Go == null) continue;
                fp.Active = true;
                fp.Age = 0f;
                fp.Life = FoundParticleLife;   // 誕生バーストは従来どおりの寿命・色・スケール
                fp.Tint = FoundParticleColor;
                fp.BaseScale = 1f;
                // 等間隔+わずかな角度ジッターで放射状に(表示のみなので UnityEngine.Random で可)
                float ang = (360f / FoundParticleCount) * i + Random.Range(-14f, 14f);
                float rad = ang * Mathf.Deg2Rad;
                float dist = Random.Range(0.85f, 1.25f);
                fp.Origin = worldPos + new Vector3(0f, FoundParticleY, 0f);
                fp.Velocity = new Vector3(
                    Mathf.Cos(rad) * dist, Random.Range(0.25f, 0.45f), Mathf.Sin(rad) * dist);
                fp.Tr.localPosition = fp.Origin;
                fp.Tr.localScale = Vector3.one;
                mpb.SetColor(ColorPropId, FoundParticleColor);
                fp.Mr.SetPropertyBlock(mpb);
                fp.Go.SetActive(true);
            }
        }

        FoundParticle CreateFoundParticle()
        {
            var fp = new FoundParticle();
            // 共有メッシュ+共有マテリアル。色・フェードは MaterialPropertyBlock で変える(フラッシュと同方式)
            var mr = RenderUtil.NewMeshChild(transform, "FoundParticle", foundParticleMesh, baseMat,
                Vector3.zero, SortFoundParticle);
            fp.Go = mr.gameObject;
            fp.Tr = mr.transform;
            fp.Mr = mr;
            fp.Go.SetActive(false);
            return fp;
        }

        /// <summary>
        /// 生産完成スパークル(2026-07-21 追加。表示のみ): 建物完成時に金色スパークル5個を
        /// 都市ブロック付近からほぼ真上へ飛ばす(約0.5秒)。都市誕生バーストと同じ
        /// パーティクルプールを再利用する(ウォームアップ後はアロケーションなし)。
        /// </summary>
        void SpawnBuildingSparkle(Vector3 worldPos)
        {
            if (foundPool == null)
            {
                foundPool = new FoundParticle[FoundParticlePoolSize];
                for (int i = 0; i < FoundParticlePoolSize; i++) foundPool[i] = CreateFoundParticle();
            }
            if (mpb == null) mpb = new MaterialPropertyBlock();

            for (int i = 0; i < SparkleCount; i++)
            {
                var fp = foundPool[foundPoolCursor];                  // ラウンドロビン = 最古を再利用
                foundPoolCursor = (foundPoolCursor + 1) % FoundParticlePoolSize;
                if (fp.Go == null) continue;
                fp.Active = true;
                fp.Age = 0f;
                fp.Life = SparkleLife;
                fp.Tint = SparkleColor;
                fp.BaseScale = SparkleBaseScale;
                // 都市ブロックのあたりから上昇(わずかな水平ジッター。表示のみなので UnityEngine.Random で可)
                fp.Origin = worldPos + new Vector3(
                    Random.Range(-0.22f, 0.22f), 0.25f, Random.Range(-0.22f, 0.22f));
                fp.Velocity = new Vector3(
                    Random.Range(-0.10f, 0.10f), Random.Range(0.55f, 0.85f), Random.Range(-0.10f, 0.10f));
                fp.Tr.localPosition = fp.Origin;
                fp.Tr.localScale = new Vector3(SparkleBaseScale, 1f, SparkleBaseScale);
                mpb.SetColor(ColorPropId, SparkleColor);
                fp.Mr.SetPropertyBlock(mpb);
                fp.Go.SetActive(true);
            }
        }

        // ------------------------------------------------------------------
        // 占領都市の炎上(2026-07-21 Claude Code 追加。表示のみ)
        // ------------------------------------------------------------------

        /// <summary>
        /// 炎上エフェクト(煙4個+バナー下の残り火グロー)を都市ビューコンテナに(なければ)作る。
        /// 都市Root の子のため、Refresh 差分でのビュー使い回し・ビュー破棄・再Init で自動的に片付く。
        /// 最初の TickBurning までの1フレームで素の白クアッドが見えないよう透明で初期化する。
        /// </summary>
        void EnsureBurnFx(CityView v)
        {
            if (v.BurnRoot != null) return;   // 再占領は BurnCaptureTurn の更新だけで良い
            if (mpb == null) mpb = new MaterialPropertyBlock();

            v.BurnRoot = new GameObject("Burning");
            v.BurnRoot.transform.SetParent(v.Root.transform, false);

            var clear = new Color(0f, 0f, 0f, 0f);
            v.BurnPuffs = new MeshRenderer[BurnPuffCount];
            v.BurnPuffPhase = new float[BurnPuffCount];
            for (int i = 0; i < BurnPuffCount; i++)
            {
                var start = BurnPuffOffsets[i % BurnPuffOffsets.Length];
                start.y = BurnPuffBaseY;
                var mr = RenderUtil.NewMeshChild(v.BurnRoot.transform, "Smoke" + i, burnPuffMesh,
                    baseMat, start, SortCitySmoke);
                mpb.SetColor(ColorPropId, clear);
                mr.SetPropertyBlock(mpb);
                v.BurnPuffs[i] = mr;
                v.BurnPuffPhase[i] = (float)i / BurnPuffCount;   // 均等に位相をずらす
            }

            v.BurnEmber = RenderUtil.NewMeshChild(v.BurnRoot.transform, "Ember", burnEmberMesh,
                baseMat, new Vector3(0f, 0.73f, 0.62f), SortCityEmber);
            mpb.SetColor(ColorPropId, clear);
            v.BurnEmber.SetPropertyBlock(mpb);
        }

        /// <summary>
        /// 炎上の毎フレーム更新(Update() の都市ループから呼ばれる。表示のみ)。
        /// 現在ターンをポーリングし、占領から BurnFadeTurns ターンで完全鎮火(エフェクト破棄)。
        /// それまでは経過ターンに比例して煙・残り火を減衰させる。
        /// 8倍速以上(snapAll = 他FXと同じ判定)の間は非表示にし、速度を戻せば再表示する。
        /// </summary>
        void TickBurning(CityView v, bool snapAll)
        {
            if (v.BurnRoot == null) { v.BurnCaptureTurn = -1; return; }

            int turnsSince = state.TurnNumber - v.BurnCaptureTurn;
            if (turnsSince >= BurnFadeTurns)
            {
                Destroy(v.BurnRoot);
                v.BurnRoot = null;
                v.BurnPuffs = null;
                v.BurnPuffPhase = null;
                v.BurnEmber = null;
                v.BurnCaptureTurn = -1;
                return;
            }

            bool show = !snapAll;
            if (v.BurnRoot.activeSelf != show) v.BurnRoot.SetActive(show);
            if (!show) return;

            // 経過ターンに応じた全体強度(1 → 0。ロード等で負になっても clamp で安全)
            float intensity = 1f - Mathf.Clamp01(turnsSince / (float)BurnFadeTurns);
            float now = Time.unscaledTime;

            for (int i = 0; i < v.BurnPuffs.Length; i++)
            {
                var mr = v.BurnPuffs[i];
                if (mr == null) continue;
                float t = Mathf.Repeat(now / BurnPuffCycle + v.BurnPuffPhase[i], 1f);   // 0..1 ループ
                var p = BurnPuffOffsets[i % BurnPuffOffsets.Length];
                p.y = BurnPuffBaseY + BurnPuffRise * t;
                mr.transform.localPosition = p;
                float sc = 0.7f + 0.6f * t;   // 上昇しながらゆっくり広がる
                mr.transform.localScale = new Vector3(sc, 1f, sc);
                var c = BurnSmokeColor;
                c.a = intensity * BurnSmokeMaxAlpha * Mathf.Sin(t * Mathf.PI);   // 出現→消滅を滑らかに
                mpb.SetColor(ColorPropId, c);
                mr.SetPropertyBlock(mpb);
            }

            if (v.BurnEmber != null)
            {
                // 残り火: 淡いオレンジのゆらぎ(位相は座標ハッシュで都市ごとにずらす)
                var ec = BurnEmberColor;
                ec.a = intensity * BurnEmberMaxAlpha
                    * (0.75f + 0.25f * Mathf.Sin(now * 7.3f + RenderUtil.Hash01(v.Coord) * (Mathf.PI * 2f)));
                mpb.SetColor(ColorPropId, ec);
                v.BurnEmber.SetPropertyBlock(mpb);
            }
        }

        // ------------------------------------------------------------------
        // 撃破フェード
        // ------------------------------------------------------------------

        void BeginDeathFade(UnitView v)
        {
            // グローリングは明滅途中の可能性があるため、消してからフェードへ渡す(2026-07-21 追加)
            if (v.FortifyGlow != null) v.FortifyGlow.gameObject.SetActive(false);
            // 補給マーカーも同様に消してからフェードへ渡す(2026-07-22 追加)
            if (v.SupplyMark != null) v.SupplyMark.gameObject.SetActive(false);

            var f = new FadingView();
            f.Root = v.Root;
            f.OwnedMat = v.HpFillMat;

            var all = v.Root.GetComponentsInChildren<MeshRenderer>(true);
            var texts = v.Root.GetComponentsInChildren<TextMesh>(true);

            // TextMesh は tm.color、それ以外は MaterialPropertyBlock の _Color でフェードする
            int n = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i].GetComponent<TextMesh>() == null) n++;
            f.Renderers = new MeshRenderer[n];
            f.BaseColors = new Color[n];
            int w = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].GetComponent<TextMesh>() != null) continue;
                f.Renderers[w] = all[i];
                var m = all[i].sharedMaterial;
                f.BaseColors[w] = m != null ? m.color : Color.white;   // 共有色マテリアルの色を保つ
                w++;
            }

            f.Texts = texts;
            f.TextColors = new Color[texts.Length];
            for (int i = 0; i < texts.Length; i++) f.TextColors[i] = texts[i].color;

            fading.Add(f);
        }

        // ------------------------------------------------------------------
        // ユニット
        // ------------------------------------------------------------------

        UnitView CreateUnitView(Unit u)
        {
            var v = new UnitView();
            v.Root = new GameObject("Unit_" + u.Id);
            v.Root.transform.SetParent(transform, false);

            var owner = state.GetPlayer(u.PlayerId);
            var ownerColor = owner != null ? owner.Color : Color.gray;

            RenderUtil.NewMeshChild(v.Root.transform, "Outline", outlineMesh, baseMat,
                new Vector3(0f, UnitY, 0f), SortUnitOutline);
            RenderUtil.NewMeshChild(v.Root.transform, "Disc", discMesh, GetColorMat(ownerColor),
                new Vector3(0f, UnitY, 0f), SortUnitDisc);

            v.FortifyRing = RenderUtil.NewMeshChild(v.Root.transform, "Fortify", fortifyMesh, baseMat,
                new Vector3(0f, UnitY, 0f), SortUnitOutline).gameObject;
            v.FortifyRing.SetActive(false);

            // 防御態勢のグローリング(待機アニメ。2026-07-21 追加): 暗色リングの少し上に重ね、
            // Update() が金色でゆっくり明滅させる。表示のオン/オフも Update() が管理する。
            v.FortifyGlow = RenderUtil.NewMeshChild(v.Root.transform, "FortifyGlow", fortifyGlowMesh,
                baseMat, new Vector3(0f, UnitY + 0.005f, 0f), SortUnitDisc);
            v.FortifyGlow.gameObject.SetActive(false);

            // 待機ゆれの位相を Id のハッシュから決める(0..2πへ一様分布させる)
            v.BobPhase = (unchecked((uint)u.Id * 2654435761u) & 0xFFFFu) * (Mathf.PI * 2f / 65536f);

            // 被弾フラッシュ(共有メッシュ+共有マテリアル。色は MaterialPropertyBlock で変える)
            v.Flash = RenderUtil.NewMeshChild(v.Root.transform, "Flash", discMesh, baseMat,
                new Vector3(0f, UnitY + 0.02f, 0f), SortUnitFlash);
            v.Flash.transform.localScale = new Vector3(1.25f, 1f, 1.25f);
            v.Flash.gameObject.SetActive(false);

            CreateText(v.Root.transform, "Glyph", new Vector3(0f, UnitY + 0.03f, 0.01f),
                u.Def.Glyph, 64, 0.085f, Color.white, SortUnitGlyph);

            // fontSize を倍にして characterSize を半減 → ワールドサイズ不変のままテクスチャ解像度2倍(にじみ軽減)
            v.NameLabel = CreateText(v.Root.transform, "UnitName", new Vector3(0f, UnitY + 0.04f, -0.52f),
                u.Def.NameJa, 68, 0.019f, Color.white, SortUnitGlyph);
            v.NameShadow = CreateTextShadow(v.NameLabel, SortUnitNameShadow);

            // HPバー(損傷時のみ表示)
            v.HpRoot = new GameObject("Hp");
            v.HpRoot.transform.SetParent(v.Root.transform, false);
            v.HpRoot.transform.localPosition = new Vector3(0f, UnitY + 0.05f, 0.58f);

            var bg = RenderUtil.NewMeshChild(v.HpRoot.transform, "Bg", quadMesh,
                GetColorMat(new Color(0.12f, 0.12f, 0.12f, 0.90f)), Vector3.zero, SortUnitHpBg);
            bg.transform.localScale = new Vector3(0.90f, 1f, 0.14f);

            v.HpFillMat = new Material(baseMat);
            var fill = RenderUtil.NewMeshChild(v.HpRoot.transform, "Fill", fillMesh,
                v.HpFillMat, new Vector3(-0.43f, 0.001f, 0f), SortUnitHpFill);
            v.HpFill = fill.transform;
            v.HpRoot.SetActive(false);

            return v;
        }

        void UpdateUnitView(UnitView v, Unit u, Player human)
        {
            bool visible = human == null || human.Visible.Contains(u.Coord);
            if (v.Root.activeSelf != visible) v.Root.SetActive(visible);

            var p = u.Coord.ToWorld();
            p.y = TileHeightAt(u.Coord);   // 丘陵タイルの視覚高さに乗せる(トゥイーンのYも補間される)
            v.Coord = u.Coord;
            v.TargetPos = p;

            if (!visible)
            {
                // 非表示中はトゥイーンせず即座に同期(再表示時に横滑りしない)
                v.SmoothPos = p;
                v.Root.transform.localPosition = p;
                return;
            }
            // 表示中の位置決定は Update() のトゥイーンに任せる(遠距離は自動スナップ)

            v.FortifyRing.SetActive(u.Fortified);
            if (v.NameLabel != null && v.NameLabel.text != u.Def.NameJa)
            {
                v.NameLabel.text = u.Def.NameJa;
                if (v.NameShadow != null) v.NameShadow.text = u.Def.NameJa;
            }

            bool damaged = u.Hp < GameRules.UnitMaxHp;
            if (v.HpRoot.activeSelf != damaged) v.HpRoot.SetActive(damaged);
            if (damaged)
            {
                float frac = Mathf.Clamp01(u.Hp / (float)GameRules.UnitMaxHp);
                v.HpFill.localScale = new Vector3(0.86f * frac, 1f, 0.10f);
                v.HpFillMat.color = Color.Lerp(HpLow, HpHigh, frac);
            }

            // 補給状態マーカー(2026-07-22 追加。表示のみ): Core がターン開始時に確定した
            // Unit.Supply をそのまま読む。対象は人間プレイヤーのユニット(観戦モードは全ユニット)。
            // 変化した時だけビューを作り替え、通常フレームは比較1回で終わる。
            var shown = (human == null || u.PlayerId == human.Id) ? u.Supply : SupplyLevel.Supplied;
            if (shown != v.SupplyShown)
            {
                v.SupplyShown = shown;
                ApplySupplyMark(v);
            }
        }

        // ------------------------------------------------------------------
        // 都市
        // ------------------------------------------------------------------

        CityView CreateCityView()
        {
            var v = new CityView();
            v.Root = new GameObject("City");
            v.Root.transform.SetParent(transform, false);

            // 建物ブロックは UpdateCityView → EnsureCityCluster が人口段階に応じて構築する

            v.Banner = RenderUtil.NewMeshChild(v.Root.transform, "Banner", bannerMesh, baseMat,
                new Vector3(0f, 0.75f, 0.62f), SortCityBanner);

            // 被弾/占領フラッシュ(バナーの少し上に重ねる)
            v.Flash = RenderUtil.NewMeshChild(v.Root.transform, "Flash", bannerMesh, baseMat,
                new Vector3(0f, 0.76f, 0.62f), SortCityFlash);
            v.Flash.gameObject.SetActive(false);

            // fontSize を倍にして characterSize を半減 → ワールドサイズ不変のままテクスチャ解像度2倍(にじみ軽減)
            v.Label = CreateText(v.Root.transform, "Label", new Vector3(0f, 0.78f, 0.62f),
                "", 96, 0.025f, Color.white, SortCityText);
            v.LabelShadow = CreateTextShadow(v.Label, SortCityTextShadow);

            v.HpRoot = new GameObject("Hp");
            v.HpRoot.transform.SetParent(v.Root.transform, false);
            v.HpRoot.transform.localPosition = new Vector3(0f, 0.80f, 1.02f);

            var bg = RenderUtil.NewMeshChild(v.HpRoot.transform, "Bg", quadMesh,
                GetColorMat(new Color(0.12f, 0.12f, 0.12f, 0.90f)), Vector3.zero, SortCityHpBg);
            bg.transform.localScale = new Vector3(1.40f, 1f, 0.13f);

            v.HpFillMat = new Material(baseMat);
            var fill = RenderUtil.NewMeshChild(v.HpRoot.transform, "Fill", fillMesh,
                v.HpFillMat, new Vector3(-0.67f, 0.001f, 0f), SortCityHpFill);
            v.HpFill = fill.transform;
            v.HpRoot.SetActive(false);

            return v;
        }

        void UpdateCityView(CityView v, City c, Player human)
        {
            // 都市は一度探索されればゴースト表示のまま残る(視界内かどうかは問わない)
            bool known = human == null || human.Explored.Contains(c.Coord);
            if (v.Root.activeSelf != known) v.Root.SetActive(known);
            if (!known) return;

            var p = c.Coord.ToWorld();
            p.y = TileHeightAt(c.Coord);   // 丘陵タイルの視覚高さに乗せる(バナー・ラベル・HPバーも追従)
            v.Coord = c.Coord;
            v.Root.transform.localPosition = p;

            // 建物クラスタ(人口段階か城壁状態が変わった時のみ再構築)
            EnsureCityCluster(v, c);

            // 占領で所有者が変わり得るため毎回色を当て直す
            var owner = state.GetPlayer(c.PlayerId);
            var ownerColor = owner != null ? owner.Color : Color.gray;
            v.Banner.sharedMaterial = GetColorMat(Color.Lerp(ownerColor, Color.black, 0.20f));

            v.Root.name = "City_" + c.Id;
            v.Label.text = c.NameJa + " (" + c.Population + ")";
            if (v.LabelShadow != null) v.LabelShadow.text = v.Label.text;

            // 都市成長パルス(表示のみ): Refresh 時に人口の増加を検知してバナー拡大を予約する
            if (v.LastPop >= 0 && c.Population > v.LastPop)
                v.GrowthPulseTime = GrowthPulseDuration;
            v.LastPop = c.Population;

            // 生産完成スパークル(2026-07-21 追加。表示のみ): 成長パルスと同じキャッシュ+
            // ポーリング方式で Buildings.Count の増加を検知して金色スパークルを飛ばす
            // (生成/ロード/再Init直後は LastBuildings=-1 で発火しない。8倍速以上はスキップ)
            int builtCount = c.Buildings != null ? c.Buildings.Count : 0;
            if (v.LastBuildings >= 0 && builtCount > v.LastBuildings
                && !suppressSpawnPop && Time.timeScale < SnapTimeScale)
            {
                SpawnBuildingSparkle(p);   // p はタイル視覚高さ込みの都市ワールド位置
            }
            v.LastBuildings = builtCount;

            bool damaged = c.Hp < c.MaxHp;
            if (v.HpRoot.activeSelf != damaged) v.HpRoot.SetActive(damaged);
            if (damaged)
            {
                float frac = Mathf.Clamp01(c.Hp / (float)Mathf.Max(1, c.MaxHp));
                v.HpFill.localScale = new Vector3(1.34f * frac, 1f, 0.09f);
                v.HpFillMat.color = Color.Lerp(HpLow, HpHigh, frac);
            }
        }

        // ------------------------------------------------------------------
        // 都市クラスタ + タイル視覚高さ(2026-07-21 追加。表示のみ)
        // ------------------------------------------------------------------

        /// <summary>タイルの視覚高さ(丘陵の立体化)。マップ外・未初期化は0。表示専用。</summary>
        float TileHeightAt(HexCoord c)
        {
            if (state == null || state.Map == null) return 0f;
            var t = state.Map.Get(c);
            return t != null ? RenderUtil.TileVisualHeight(t) : 0f;
        }

        /// <summary>人口→表示段階(1: 1-2 / 2: 3-4 / 3: 5-7 / 4: 8+)。</summary>
        static int CityPopTier(int pop)
        {
            if (pop >= 8) return 4;
            if (pop >= 5) return 3;
            if (pop >= 3) return 2;
            return 1;
        }

        /// <summary>
        /// 都市の建物クラスタを人口段階・城壁状態に合わせて(必要な時のみ)再構築する。
        /// 配置角・半径ジッター・ブロックの灰色は都市Idから決定的に決める
        /// (毎フレームの UnityEngine.Random は使わない)。
        /// </summary>
        void EnsureCityCluster(CityView v, City c)
        {
            int tier = CityPopTier(c.Population);
            bool walls = c.Buildings != null && c.Buildings.Contains("walls");
            if (v.Cluster != null && v.ClusterTier == tier && v.ClusterWalls == walls) return;

            if (v.Cluster != null) Destroy(v.Cluster);
            v.ClusterTier = tier;
            v.ClusterWalls = walls;

            v.Cluster = new GameObject("Cluster");
            v.Cluster.transform.SetParent(v.Root.transform, false);
            var parent = v.Cluster.transform;

            uint h = unchecked((uint)c.Id * 2654435761u);
            float baseAng = ((h >> 20) & 0x3FFu) * (Mathf.PI * 2f / 1024f);

            if (tier == 1)
            {
                // 単独ブロック(集落)。中心からわずかに決定的オフセット。
                float jx = (((h >> 4) & 0xFu) / 15f - 0.5f) * 0.10f;
                float jz = (((h >> 8) & 0xFu) / 15f - 0.5f) * 0.10f;
                var mr = AddClusterBlock(parent, h, 0, new Vector3(jx, 0f, jz), false);
                mr.transform.localScale = new Vector3(1.4f, 1.15f, 1.4f);
            }
            else
            {
                int n = tier == 2 ? 2 : (tier == 3 ? 3 : 4);
                float radius = tier == 2 ? 0.22f : (tier == 3 ? 0.28f : 0.31f);
                for (int i = 0; i < n; i++)
                {
                    float ang = baseAng + Mathf.PI * 2f * i / n;
                    float rj = radius + ((((h >> (i * 5)) & 0x1Fu) / 31f) - 0.5f) * 0.06f;
                    var pos = new Vector3(Mathf.Cos(ang) * rj, 0f, Mathf.Sin(ang) * rj);
                    AddClusterBlock(parent, h, i, pos, false);
                }
                if (tier >= 3)
                    AddClusterBlock(parent, h, n, Vector3.zero, true);   // 中央の高層ブロック
            }

            if (walls)
                RenderUtil.NewMeshChild(parent, "Walls", cityWallMesh, baseMat,
                    Vector3.zero, SortCityBlock);
        }

        /// <summary>クラスタへブロックを1個追加する(灰色の種類はIdハッシュとindexから決定的に選ぶ)。</summary>
        MeshRenderer AddClusterBlock(Transform parent, uint h, int index, Vector3 localPos, bool tall)
        {
            var mesh = tall ? cityTallBlockMesh
                : cityBlockMeshes[(int)((h >> (index * 2 + 1)) % 3u)];
            return RenderUtil.NewMeshChild(parent, tall ? "BlockTall" : "Block" + index,
                mesh, baseMat, localPos, SortCityBlock);
        }

        /// <summary>
        /// 底面 y=0、高さ height の低い六角壁リング(上面+外側面+内側面)。
        /// RenderUtil.Corners を使うためタイルの六角形と向きが揃う。
        /// Sprites/Default は両面描画のため巻き順は問わない。
        /// </summary>
        static Mesh BuildHexWallRing(float rInner, float rOuter, float height, Color topColor, Color sideColor)
        {
            var mb = new MeshBuilder();
            var up = Vector3.up * height;
            for (int i = 0; i < 6; i++)
            {
                Vector3 d0 = RenderUtil.Corners[i];
                Vector3 d1 = RenderUtil.Corners[(i + 1) % 6];
                Vector3 o0 = d0 * rOuter, o1 = d1 * rOuter;
                Vector3 n0 = d0 * rInner, n1 = d1 * rInner;
                mb.AddQuad(o0 + up, o1 + up, n1 + up, n0 + up, topColor);   // 上面
                mb.AddQuad(o0 + up, o1 + up, o1, o0, sideColor);            // 外側面
                mb.AddQuad(n1 + up, n0 + up, n0, n1, sideColor);            // 内側面
            }
            return mb.Build(null);
        }

        // ------------------------------------------------------------------
        // 補給状態マーカー(2026-07-22 Claude Code 追加。表示のみ)
        // ------------------------------------------------------------------

        /// <summary>XZ平面のリングの一部(弧)。角度は度、0°が+X・90°が+Z。</summary>
        static Mesh BuildArcRing(float rInner, float rOuter, float startDeg, float sweepDeg,
            int segments, Color color)
        {
            var mb = new MeshBuilder();
            for (int i = 0; i < segments; i++)
            {
                float a0 = (startDeg + sweepDeg * i / segments) * Mathf.Deg2Rad;
                float a1 = (startDeg + sweepDeg * (i + 1) / segments) * Mathf.Deg2Rad;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                mb.AddQuad(d0 * rOuter, d1 * rOuter, d1 * rInner, d0 * rInner, color);
            }
            return mb.Build(null);
        }

        /// <summary>
        /// 孤立マーカー(円盤の西側に置くひし形)。外周の頂点色を暗くしておくことで、
        /// MaterialPropertyBlock で赤く着色したときに暗い縁取り付きの赤マークになる
        /// (Sprites/Default は 頂点色 × _Color。明滅アルファも両方へ同時に効く)。
        /// メッシュ側にオフセットを持たせるため、ビューの子オブジェクトは原点のままで良い。
        /// </summary>
        static Mesh BuildSupplyIsolatedMark()
        {
            var mb = new MeshBuilder();
            float ang = (SupplyMarkStartDeg + SupplyMarkSweepDeg * 0.5f) * Mathf.Deg2Rad;
            var c = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * SupplyIsolatedRadius;
            mb.AddDiamond(c, 0.19f, new Color(0.30f, 0.28f, 0.28f, 1f));   // 暗い縁取り(タイル内に収まる)
            mb.AddDiamond(c + new Vector3(0f, 0.001f, 0f), 0.13f, Color.white);
            return mb.Build(null);
        }

        /// <summary>
        /// ビューの補給マーカーを現在の SupplyShown に合わせる(状態が変わった時だけ呼ぶ)。
        /// 初回の逼迫/孤立で1個だけ生成し、以後はメッシュ差し替えと表示切替のみで使い回す。
        /// 色と明滅は Update() が MaterialPropertyBlock で与える。
        /// </summary>
        void ApplySupplyMark(UnitView v)
        {
            if (v.SupplyShown == SupplyLevel.Supplied)
            {
                if (v.SupplyMark != null) v.SupplyMark.gameObject.SetActive(false);
                return;
            }
            if (v.Root == null) return;

            if (v.SupplyMark == null)
            {
                v.SupplyMark = RenderUtil.NewMeshChild(v.Root.transform, "SupplyMark",
                    supplyStrainedMesh, baseMat, new Vector3(0f, UnitY + 0.006f, 0f), SortUnitSupply);
                v.SupplyFilter = v.SupplyMark.GetComponent<MeshFilter>();
                // 最初の Update までの1フレームで素の白マークが見えないよう透明で初期化する
                if (mpb == null) mpb = new MaterialPropertyBlock();
                mpb.SetColor(ColorPropId, new Color(1f, 1f, 1f, 0f));
                v.SupplyMark.SetPropertyBlock(mpb);
            }
            if (v.SupplyFilter != null)
            {
                var mesh = v.SupplyShown == SupplyLevel.Isolated ? supplyIsolatedMesh : supplyStrainedMesh;
                if (v.SupplyFilter.sharedMesh != mesh) v.SupplyFilter.sharedMesh = mesh;
            }
            v.SupplyMark.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------
        // 共通ヘルパー
        // ------------------------------------------------------------------

        /// <summary>指定色の Sprites/Default マテリアル(色ごとにキャッシュ・共有)。</summary>
        Material GetColorMat(Color c)
        {
            Material m;
            if (colorMats.TryGetValue(c, out m) && m != null) return m;
            m = new Material(baseMat);
            m.color = c;
            colorMats[c] = m;
            return m;
        }

        /// <summary>XZ平面に寝かせた TextMesh(上から読める向き)。フォントマテリアル割り当て済み。</summary>
        TextMesh CreateText(Transform parent, string name, Vector3 localPos, string text,
            int fontSize, float characterSize, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = fontSize;
            tm.characterSize = characterSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;    // 動的OSフォントのマテリアルを必ず割り当てる
            mr.sortingOrder = sortingOrder;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return tm;
        }

        /// <summary>
        /// 本体テキストの暗色影コピー(明るい地形でのコントラスト確保)。
        /// 本体 TextMesh の子として作るため位置・回転・スケールに自動追従する。
        /// sortingOrder は必ず本体より小さい値を渡すこと(影が本体の下に描かれる)。
        /// 本体の text を変更したら影の text も更新すること。
        /// </summary>
        TextMesh CreateTextShadow(TextMesh main, int sortingOrder)
        {
            var go = new GameObject("Shadow");
            go.transform.SetParent(main.transform, false);
            go.transform.localPosition = TextShadowOffset;

            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = main.fontSize;
            tm.characterSize = main.characterSize;
            tm.anchor = main.anchor;
            tm.alignment = main.alignment;
            tm.color = TextShadowColor;
            tm.text = main.text;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;    // 動的OSフォントのマテリアルを必ず割り当てる
            mr.sortingOrder = sortingOrder;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return tm;
        }
    }
}
