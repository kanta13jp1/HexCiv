using System.Collections.Generic;
using HexCiv.Core;
using UnityEngine;

namespace HexCiv.Render
{
    /// <summary>
    /// マップ描画。地形は起動時に1枚の結合メッシュ(頂点カラー)として構築し、
    /// 霧(戦場の霧)・領土国境・ハイライトは動的オーバーレイとして更新する。
    /// マウス下のタイル判定(XZ平面との数学的交差 + HexCoord.FromWorld)も担当する。
    /// 2026-07-22 追加: 補給範囲オーバーレイ(Lキー / SetSupplyOverlay)。
    /// 2026-07-23 追加: Coreの河川タイルを細い連結水路として装飾層へ描画。
    /// Core/LogisticsSystem の補給網を薄く着色して表示するだけで、状態は一切書き換えない。
    /// </summary>
    public class MapRenderer : MonoBehaviour
    {
        // ---- レイヤー高さ(契約 §6) ----
        const float TerrainY = 0f;
        const float DecoY = 0.02f;
        const float HydrologyY = 0.028f; // 季節洪水・橋梁(装飾の上、補給/国境の下)
        const float SupplyY = 0.03f;   // 補給オーバーレイ(装飾0.02の上・国境0.04の下)
        const float BorderY = 0.04f;
        const float HighlightY = 0.05f;
        const float FogY = 0.06f;

        // ---- 描画順(sortingOrder。Sprites/Default は ZWrite Off のため明示的に制御する) ----
        const int SortTerrain = 0;
        const int SortDeco = 1;
        const int SortHydrology = 2;
        const int SortSupply = 2;      // 補給オーバーレイ(装飾1の上・国境3の下)
        const int SortBorder = 3;
        const int SortHighlight = 4;
        const int SortSelection = 5;   // 選択リング(他ハイライトの上・霧8の下)
        const int SortFog = 8;

        // ---- 霧の色 ----
        static readonly Color32 FogUnexplored = new Color32(5, 8, 15, 255);   // (0.02,0.03,0.06,1)
        static readonly Color32 FogExplored = new Color32(0, 0, 0, 115);      // (0,0,0,0.45)
        static readonly Color32 FogVisible = new Color32(0, 0, 0, 0);

        GameState state;
        Material mat;

        Mesh terrainMesh;
        Mesh decoMesh;
        Mesh fogMesh;
        Mesh borderMesh;
        Mesh highlightMesh;
        Mesh hydrologyMesh;
        MeshRenderer hydrologyRenderer;
        MaterialPropertyBlock hydrologyMpb;
        const float FloodPulsePeriod = 2.4f;

        Tile[] tileOrder;        // 霧メッシュのタイル順(タイルごとの頂点範囲は fogVertBase/Count)
        Color32[] fogColors;
        int[] fogVertBase;       // 霧メッシュ内のタイル別先頭頂点添字
        int[] fogVertCount;      // 同・頂点数(平地=7、丘陵はスカート分を含む)

        // ---- 丘陵の立体化+領土の面塗り(2026-07-21 追加。表示のみ・シミュレーション非干渉) ----
        // 丘陵タイルは地形メッシュの7頂点を RenderUtil.TileVisualHeight 分だけ持ち上げ、
        // 低い隣接タイルとの段差にスカート壁(側面クアッド)を張って隙間を塞ぐ。
        // 領土の面塗りは、所有者のいるタイルの地形頂点色へ所有者色を約7%だけ混ぜる。
        // RefreshDynamic で国境と同じ可視ルール(未探索は塗らない)で更新する。
        // どちらもマウス判定(XZ平面の数学的交差)には影響しない。
        Color32[] tileBaseCols;              // タイルごとの基準色(明度ゆらぎ適用後・面塗り前)
        const float TerritoryTint = 0.07f;   // 領土面塗りの混合率(約7%)
        const float SkirtDarken = 0.72f;     // スカート壁は地形色より少し暗く

        // ---- 水面ゆらぎ(2026-07-21 追加。表示のみ・シミュレーション非干渉) ----
        // 地形メッシュの Ocean/Coast 頂点色だけを、座標ハッシュで位相をずらした
        // ゆっくりした正弦波(明度±約1.2%)で更新する。バッファは全てキャッシュし、
        // 毎フレームではなく最大10回/秒だけ書き込む。霧・国境・ハイライトの各メッシュ
        // およびマウス判定(数学的交差)には一切触れない。
        struct WaterEntry
        {
            public int VertBase;     // terrainColors 内の先頭添字(1タイル = 7頂点)
            public Color32 BaseCol;  // 揺らぎの基準色(ビルド時の最終色)
            public float Phase;      // タイル座標ハッシュ由来の位相
        }
        WaterEntry[] waterEntries;
        Color32[] terrainColors;     // 地形メッシュ頂点色のキャッシュ(水面ゆらぎ・領土面塗りが更新)
        float waterClock;
        float nextWaterTick;
        bool waterLightRestored;     // 軽量演出モード(VisualQuality)移行時に基準色へ戻し済みか(2026-07-22 追加)
        const float WaterPeriod = 5f;           // 約5秒周期(ゆっくり)
        const float WaterAmp = 0.012f;          // 明度±1.2%
        const float WaterTickInterval = 0.1f;   // 更新は最大10回/秒

        // ---- 選択タイルのパルス(2026-07-21 追加。表示のみ・シミュレーション非干渉) ----
        // SetHighlights の白い縁リングを独立メッシュへ分離し、非スケール時間の正弦波で
        // アルファを 0.6→1.0 に明滅させる。色は MaterialPropertyBlock の _Color 乗算で
        // 変えるため、毎フレームのメッシュ書き込み・マテリアル生成・割り当ては発生しない。
        Mesh selectionMesh;
        MeshRenderer selectionRenderer;
        bool selectionVisible;
        MaterialPropertyBlock selectionMpb;
        static readonly int ColorPropId = Shader.PropertyToID("_Color");
        const float SelectionPulsePeriod = 1.2f;  // 明滅の周期(秒)
        const float SelectionAlphaMin = 0.6f;
        const float SelectionAlphaMax = 1.0f;

        // ---- 補給範囲オーバーレイ(2026-07-22 Claude Code 追加。表示のみ・シミュレーション非干渉) ----
        // Codex の Core/LogisticsSystem が算出する都市起点の補給網を、盤面へ薄く着色して見せる。
        // 対象は人間プレイヤー。観戦モード(HumanPlayer が null)では首位文明を対象にする
        // (スコア = Σ人口*3 + 都市*8 + 技術*5。TurnManager のスコア勝利と同じ式。同点は Id 昇順)。
        // 分類は必ず LogisticsSystem.LevelAt() を呼んで決めるため、しきい値をこちらで再定義せず
        // シミュレーションと常に同一のルールになる(良好=薄い緑 a=0.12 / 逼迫=琥珀 a=0.14 /
        // 圏外・孤立相当=無着色)。敵ユニット・敵都市に遮断されたタイルは補給網から外れて
        // 自然に無着色となり、「補給線が切れた」ことがそのまま盤面で分かる。
        // 霧のルール(未探索タイルは塗らない)は国境・領土面塗りと同一。丘陵の持ち上げにも追従する。
        // 再計算は「state.Version が変わった」かつ「前回から0.5秒以上経過」の両方を満たす時だけで、
        // 変化のないフレームは補給辞書もメッシュも作り直さない(定常状態でアロケーションなし)。
        // 情報表示のため VisualQuality.LightMode でも抑制しない(演出ではないため)。
        static readonly Color SupplySuppliedColor = new Color(0.28f, 0.85f, 0.42f, 0.12f);  // 良好=薄緑
        static readonly Color SupplyStrainedColor = new Color(1f, 0.72f, 0.16f, 0.14f);     // 逼迫=琥珀
        const float SupplyTickInterval = 0.5f;   // 再計算は最大2回/秒
        const float SupplyHexRadius = 0.94f;     // タイル内に収める(国境リングと重ならない)

        Mesh supplyMesh;
        MeshRenderer supplyRenderer;
        bool supplyOverlayOn;
        /// <summary>直近に計算した補給コスト(座標→コスト)。再計算時のみ作り直す。</summary>
        Dictionary<HexCoord, int> supplyCosts;
        int supplyBuiltVersion = int.MinValue;   // supplyCosts を作った時の state.Version
        int supplyBuiltPlayerId = int.MinValue;  // 同・対象プレイヤーId(-1 = 対象なし)
        float supplyNextTick;

        readonly MeshBuilder builder = new MeshBuilder();

        /// <summary>静的な地形メッシュ群を構築する。再呼び出し(リスタート)にも対応。</summary>
        public void Init(GameState state)
        {
            this.state = state;

            // 再初期化対応:以前の子オブジェクトを破棄
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            if (mat == null) mat = RenderUtil.NewSpriteMaterial();

            BuildTerrain();
            BuildDecorations();
            BuildFog();

            borderMesh = builderEmpty(borderMesh);
            borderMesh.MarkDynamic();
            RenderUtil.NewMeshChild(transform, "Borders", borderMesh, mat, Vector3.zero, SortBorder);

            highlightMesh = builderEmpty(highlightMesh);
            highlightMesh.MarkDynamic();
            RenderUtil.NewMeshChild(transform, "Highlights", highlightMesh, mat, Vector3.zero, SortHighlight);

            // 選択リング(アルファ明滅)専用メッシュ。子は上で全破棄済みのため毎回作り直す
            selectionMesh = builderEmpty(selectionMesh);
            selectionMesh.MarkDynamic();
            selectionRenderer = RenderUtil.NewMeshChild(transform, "SelectionPulse", selectionMesh, mat, Vector3.zero, SortSelection);
            selectionVisible = false;

            // ターンごとに変わる増水・肥沃期と、都市建物に連動する橋梁を描く動的層。
            hydrologyMesh = builderEmpty(hydrologyMesh);
            hydrologyMesh.MarkDynamic();
            hydrologyRenderer = RenderUtil.NewMeshChild(
                transform, "SeasonalHydrology", hydrologyMesh, mat, Vector3.zero, SortHydrology);

            // 補給範囲オーバーレイ(子は上で全破棄済みのため毎回作り直す)。
            // 表示のON/OFFはリスタートを跨いで維持し、内容は次の Tick で必ず作り直す。
            supplyMesh = builderEmpty(supplyMesh);
            supplyMesh.MarkDynamic();
            supplyRenderer = RenderUtil.NewMeshChild(transform, "SupplyOverlay", supplyMesh, mat, Vector3.zero, SortSupply);
            supplyRenderer.gameObject.SetActive(supplyOverlayOn);
            InvalidateSupplyOverlay();

            RefreshDynamic();
        }

        Mesh builderEmpty(Mesh reuse)
        {
            builder.Clear();
            return builder.Build(reuse);
        }

        /// <summary>霧・国境・領土の面塗りを state.HumanPlayer 視点で更新する(null なら全て可視)。</summary>
        public void RefreshDynamic()
        {
            if (state == null || fogMesh == null) return;
            UpdateFog();
            RebuildBorders();
            UpdateTerritoryTint();
            RebuildHydrologyOverlay();
        }

        /// <summary>移動可能・攻撃可能・経路・選択タイルのハイライトを表示する。引数はどれも null 可。</summary>
        public void SetHighlights(HashSet<HexCoord> reachable, HashSet<HexCoord> attackable, List<HexCoord> path, HexCoord? selected)
        {
            if (state == null || highlightMesh == null) return;

            // 初見でも意味が伝わるよう、チュートリアルの凡例と色を統一する。
            Color reachCol = new Color(1f, 0.82f, 0.12f, 0.36f);
            Color atkCol = new Color(1f, 0.12f, 0.08f, 0.52f);
            Color pathCol = new Color(0.25f, 0.95f, 0.72f, 0.92f);

            builder.Clear();

            if (reachable != null)
            {
                foreach (var h in reachable)
                {
                    if (!state.Map.InBounds(h)) continue;
                    var c = h.ToWorld(); c.y = HighlightY + VisualHeightAt(h);
                    builder.AddHex(c, 0.90f, reachCol);
                }
            }

            if (attackable != null)
            {
                foreach (var h in attackable)
                {
                    if (!state.Map.InBounds(h)) continue;
                    var c = h.ToWorld(); c.y = HighlightY + VisualHeightAt(h);
                    builder.AddHex(c, 0.90f, atkCol);
                }
            }

            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    if (!state.Map.InBounds(path[i])) continue;
                    var c = path[i].ToWorld(); c.y = HighlightY + VisualHeightAt(path[i]);
                    builder.AddDiamond(c, 0.16f, pathCol);
                }
            }

            highlightMesh = builder.Build(highlightMesh);

            // 選択タイルの白リングは独立メッシュに構築する(Update() でアルファ明滅させるため)。
            // 頂点アルファは 1.0 で持ち、実際の明滅は _Color 乗算(0.6〜1.0)で行う。
            builder.Clear();
            selectionVisible = false;
            if (selected.HasValue && state.Map.InBounds(selected.Value))
            {
                Color selCol = new Color(1f, 1f, 1f, 1f);
                var c = selected.Value.ToWorld(); c.y = HighlightY + VisualHeightAt(selected.Value);
                for (int d = 0; d < 6; d++)
                {
                    Vector3 ca = RenderUtil.Corners[RenderUtil.EdgeCornerA[d]];
                    Vector3 cb = RenderUtil.Corners[RenderUtil.EdgeCornerB[d]];
                    builder.AddQuad(c + ca * 0.97f, c + cb * 0.97f, c + cb * 0.85f, c + ca * 0.85f, selCol);
                }
                selectionVisible = true;
            }
            if (selectionMesh != null) selectionMesh = builder.Build(selectionMesh);
        }

        /// <summary>座標のタイルの視覚的な持ち上げ高さ(表示専用。マップ外は 0)。</summary>
        float VisualHeightAt(HexCoord h)
        {
            return RenderUtil.TileVisualHeight(state.Map.Get(h));
        }

        /// <summary>ハイライトをすべて消す。</summary>
        public void ClearHighlights()
        {
            if (highlightMesh != null) highlightMesh.Clear();
            if (selectionMesh != null) selectionMesh.Clear();
            selectionVisible = false;
        }

        // ------------------------------------------------------------------
        // 補給範囲オーバーレイ(2026-07-22 Claude Code 追加。表示のみ)
        // ------------------------------------------------------------------

        /// <summary>補給範囲オーバーレイが表示中か。</summary>
        public bool SupplyOverlayEnabled => supplyOverlayOn;

        /// <summary>補給範囲オーバーレイの表示を切り替える。戻り値は切替後の状態(true = 表示)。</summary>
        public bool ToggleSupplyOverlay()
        {
            SetSupplyOverlay(!supplyOverlayOn);
            return supplyOverlayOn;
        }

        /// <summary>補給範囲オーバーレイの表示を設定する(表示のみ。シミュレーションには触れない)。</summary>
        public void SetSupplyOverlay(bool on)
        {
            if (supplyOverlayOn == on) return;
            supplyOverlayOn = on;
            if (supplyRenderer != null) supplyRenderer.gameObject.SetActive(on);
            if (on) InvalidateSupplyOverlay();   // 表示した瞬間に最新内容へ作り直す
        }

        /// <summary>次の Tick で補給オーバーレイを必ず作り直させる。</summary>
        void InvalidateSupplyOverlay()
        {
            supplyBuiltVersion = int.MinValue;
            supplyBuiltPlayerId = int.MinValue;
            supplyNextTick = 0f;
        }

        /// <summary>
        /// 補給オーバーレイの更新。非表示なら何もしない。表示中でも
        /// 「前回から SupplyTickInterval 秒以上経過」かつ「state.Version が前回と違う
        /// (または対象文明が変わった)」場合だけ補給網とメッシュを作り直す。
        /// それ以外のフレームは早期 return のみでアロケーションは発生しない。
        /// </summary>
        void TickSupplyOverlay()
        {
            if (!supplyOverlayOn || state == null || supplyMesh == null) return;

            float now = Time.unscaledTime;
            if (now < supplyNextTick) return;
            supplyNextTick = now + SupplyTickInterval;

            var target = SupplyOverlayPlayer();
            int pid = target != null ? target.Id : -1;
            if (pid == supplyBuiltPlayerId && state.Version == supplyBuiltVersion) return;
            supplyBuiltPlayerId = pid;
            supplyBuiltVersion = state.Version;

            if (target == null)
            {
                supplyCosts = null;
                builder.Clear();
                supplyMesh = builder.Build(supplyMesh);
                return;
            }

            supplyCosts = LogisticsSystem.BuildSupplyCosts(state, target);
            RebuildSupplyMesh(target);
        }

        /// <summary>
        /// オーバーレイの対象文明。通常プレイでは人間プレイヤー、
        /// 観戦モード(HumanPlayer が null)では首位文明(スコア = Σ人口*3 + 都市*8 + 技術*5、
        /// 同点は Id 昇順)。滅亡文明は対象にしない。全滅していれば null。
        /// </summary>
        Player SupplyOverlayPlayer()
        {
            var human = state.HumanPlayer;
            if (human != null) return human.IsEliminated ? null : human;

            Player best = null;
            int bestScore = int.MinValue;
            for (int i = 0; i < state.Players.Count; i++)
            {
                var p = state.Players[i];
                if (p == null || p.IsEliminated) continue;
                int score = p.Cities.Count * 8 + p.KnownTechs.Count * 5;
                for (int c = 0; c < p.Cities.Count; c++)
                    if (p.Cities[c] != null) score += p.Cities[c].Population * 3;
                if (best == null || score > bestScore)
                {
                    best = p;
                    bestScore = score;
                }
            }
            return best;
        }

        /// <summary>
        /// 補給コスト辞書から着色メッシュを構築する。分類は LogisticsSystem.LevelAt に委ねる
        /// (しきい値をこちらに複製しない)。孤立相当・補給網外のタイルは着色しないため、
        /// 敵に遮断された補給線の切れ目がそのまま見える。
        /// 未探索タイルは霧と同じルールで塗らない(通常プレイのみ。観戦モードは全タイル対象)。
        /// </summary>
        void RebuildSupplyMesh(Player target)
        {
            builder.Clear();
            if (supplyCosts != null && supplyCosts.Count > 0 && tileOrder != null)
            {
                var human = state.HumanPlayer;
                for (int i = 0; i < tileOrder.Length; i++)
                {
                    var t = tileOrder[i];
                    if (human != null && !human.Explored.Contains(t.Coord)) continue;

                    var level = LogisticsSystem.LevelAt(target, t.Coord, supplyCosts);
                    Color col;
                    if (level == SupplyLevel.Supplied) col = SupplySuppliedColor;
                    else if (level == SupplyLevel.Strained) col = SupplyStrainedColor;
                    else continue;   // 孤立相当・補給網外 = 無着色

                    var c = t.Coord.ToWorld();
                    c.y = SupplyY + RenderUtil.TileVisualHeight(t);
                    builder.AddHex(c, SupplyHexRadius, col);
                }
            }
            supplyMesh = builder.Build(supplyMesh);
        }

        /// <summary>
        /// マウス位置のタイルを数学的に求める(y=0 の XZ 平面とレイの交差 → HexCoord.FromWorld)。
        /// マップ外なら false。
        /// </summary>
        public bool TryGetTileUnderMouse(Camera cam, out HexCoord coord)
        {
            coord = default(HexCoord);
            if (cam == null || state == null || state.Map == null) return false;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Mathf.Abs(ray.direction.y) < 1e-6f) return false;
            float t = -ray.origin.y / ray.direction.y;
            if (t < 0f) return false;

            Vector3 hit = ray.origin + ray.direction * t;
            var c = HexCoord.FromWorld(hit);
            if (!state.Map.InBounds(c)) return false;

            coord = c;
            return true;
        }

        // ------------------------------------------------------------------
        // 構築
        // ------------------------------------------------------------------

        void BuildTerrain()
        {
            builder.Clear();
            var baseCols = new List<Color32>();      // タイルごとの最終色(水面ゆらぎ・領土面塗りキャッシュの元)
            var tileCols = new List<Color>();        // 同・float色(スカート壁の暗色計算用)
            var waterList = new List<WaterEntry>();
            int tileIndex = 0;
            foreach (var t in state.Map.AllTiles)
            {
                // 丘陵タイルは7頂点をまるごと持ち上げる(表示のみ。マウス判定はXZ平面のまま)
                var c = t.Coord.ToWorld(); c.y = TerrainY + RenderUtil.TileVisualHeight(t);
                var col = t.Def.Color;
                // タイルごとのわずかな明度ゆらぎ(決定的ハッシュ)
                float b = 0.93f + 0.10f * RenderUtil.Hash01(t.Coord);
                col = new Color(
                    Mathf.Clamp01(col.r * b),
                    Mathf.Clamp01(col.g * b),
                    Mathf.Clamp01(col.b * b),
                    1f);
                builder.AddHex(c, 1f, col);

                Color32 c32 = col;
                baseCols.Add(c32);
                tileCols.Add(col);
                if (t.Terrain == TerrainType.Ocean || t.Terrain == TerrainType.Coast)
                {
                    waterList.Add(new WaterEntry
                    {
                        VertBase = tileIndex * 7,
                        BaseCol = c32,
                        Phase = RenderUtil.Hash01(t.Coord) * (Mathf.PI * 2f),
                    });
                }
                tileIndex++;
            }

            // スカート壁: 持ち上げたタイルの縁のうち、低い隣(またはマップ外)へ落ちる辺に
            // 側面クアッドを張り、段差の隙間から下が透けないようにする。
            // ※必ず全ヘクスの後に追加する(WaterEntry.VertBase の「タイルi = 頂点7i」対応を保つ)。
            int skirtIndex = 0;
            foreach (var t in state.Map.AllTiles)
            {
                float h = RenderUtil.TileVisualHeight(t);
                if (h > 0f)
                {
                    var col = tileCols[skirtIndex];
                    var skirtCol = new Color(col.r * SkirtDarken, col.g * SkirtDarken, col.b * SkirtDarken, 1f);
                    var c = t.Coord.ToWorld();
                    for (int d = 0; d < 6; d++)
                    {
                        var n = state.Map.Get(t.Coord.Neighbor(d));
                        float nh = (n == null) ? 0f : RenderUtil.TileVisualHeight(n);
                        if (nh >= h) continue;   // 同じ高さの丘陵どうしは壁なしで連続させる

                        Vector3 ca = RenderUtil.Corners[RenderUtil.EdgeCornerA[d]];
                        Vector3 cb = RenderUtil.Corners[RenderUtil.EdgeCornerB[d]];
                        Vector3 topA = c + ca; topA.y = TerrainY + h;
                        Vector3 topB = c + cb; topB.y = TerrainY + h;
                        Vector3 botA = c + ca; botA.y = TerrainY + nh;
                        Vector3 botB = c + cb; botB.y = TerrainY + nh;
                        builder.AddQuad(topA, topB, botB, botA, skirtCol);
                    }
                }
                skirtIndex++;
            }

            terrainMesh = builder.Build(terrainMesh);
            terrainMesh.MarkDynamic();               // 水面ゆらぎ・領土面塗りで頂点色を更新するため

            // 頂点色キャッシュを一度だけ確保(先頭はタイルi = 頂点7i、末尾にスカート壁の頂点が続く)
            terrainColors = builder.ColorsToArray();
            tileBaseCols = baseCols.ToArray();
            waterEntries = waterList.ToArray();
            waterClock = 0f;
            nextWaterTick = 0f;

            RenderUtil.NewMeshChild(transform, "Terrain", terrainMesh, mat, Vector3.zero, SortTerrain);
        }

        /// <summary>毎フレームの表示演出(選択リングの明滅・水面のゆらぎ)。表示のみ。</summary>
        void Update()
        {
            TickSelectionPulse();
            TickWater();
            TickHydrology();
            TickSupplyOverlay();
        }

        /// <summary>
        /// 増水期だけ季節水面をゆっくり明滅させる。軽量モードでは固定表示にする。
        /// メッシュは更新せず、共有マテリアルを複製しない MaterialPropertyBlock だけを使う。
        /// </summary>
        void TickHydrology()
        {
            if (hydrologyRenderer == null || state == null) return;
            float alpha = 1f;
            if (!VisualQuality.LightMode &&
                NaturalGeographySystem.FloodStageAt(state.TurnNumber) == FloodStage.Inundated)
            {
                float phase = Time.unscaledTime * (Mathf.PI * 2f / FloodPulsePeriod);
                alpha = Mathf.Lerp(0.78f, 1f, 0.5f + 0.5f * Mathf.Sin(phase));
            }
            if (hydrologyMpb == null) hydrologyMpb = new MaterialPropertyBlock();
            hydrologyMpb.SetColor(ColorPropId, new Color(1f, 1f, 1f, alpha));
            hydrologyRenderer.SetPropertyBlock(hydrologyMpb);
        }

        /// <summary>
        /// 選択リングのアルファ明滅(0.6→1.0、約1.2秒周期の正弦波、非スケール時間)。
        /// MaterialPropertyBlock はキャッシュし、毎フレームの割り当てはゼロ。
        /// </summary>
        void TickSelectionPulse()
        {
            if (!selectionVisible || selectionRenderer == null) return;
            float phase = Time.unscaledTime * (Mathf.PI * 2f / SelectionPulsePeriod);
            float a = Mathf.Lerp(SelectionAlphaMin, SelectionAlphaMax, 0.5f + 0.5f * Mathf.Sin(phase));
            if (selectionMpb == null) selectionMpb = new MaterialPropertyBlock();
            selectionMpb.SetColor(ColorPropId, new Color(1f, 1f, 1f, a));
            selectionRenderer.SetPropertyBlock(selectionMpb);
        }

        /// <summary>
        /// 水面のゆらぎ(表示のみ)。Ocean/Coast タイルの頂点色を最大10回/秒だけ更新する。
        /// キャッシュ済みバッファのみを使い、毎tickの割り当てはゼロ。バッファと頂点数が
        /// 一致しない場合(再構築の途中など)は何もしない。
        /// </summary>
        void TickWater()
        {
            if (terrainMesh == null || waterEntries == null || waterEntries.Length == 0) return;
            if (terrainColors == null || terrainMesh.vertexCount != terrainColors.Length) return;

            // 軽量演出モード(2026-07-22 追加): ゆらぎを停止し、基準色(面塗り込み)へ一度だけ
            // 戻して以後は何もしない。標準へ戻せば次の tick から通常のゆらぎを再開する。
            if (VisualQuality.LightMode)
            {
                if (!waterLightRestored)
                {
                    waterLightRestored = true;
                    for (int i = 0; i < waterEntries.Length; i++)
                    {
                        int vb = waterEntries[i].VertBase;
                        var bc = waterEntries[i].BaseCol;
                        for (int k = 0; k < 7; k++) terrainColors[vb + k] = bc;
                    }
                    terrainMesh.colors32 = terrainColors;
                }
                return;
            }
            waterLightRestored = false;

            waterClock += Time.unscaledDeltaTime;
            if (waterClock < nextWaterTick) return;
            nextWaterTick = waterClock + WaterTickInterval;

            float w = waterClock * (Mathf.PI * 2f / WaterPeriod);
            for (int i = 0; i < waterEntries.Length; i++)
            {
                var e = waterEntries[i];
                float b = 1f + WaterAmp * Mathf.Sin(w + e.Phase);
                var c = e.BaseCol;
                var sc = new Color32(
                    (byte)Mathf.Clamp((int)(c.r * b), 0, 255),
                    (byte)Mathf.Clamp((int)(c.g * b), 0, 255),
                    (byte)Mathf.Clamp((int)(c.b * b), 0, 255),
                    c.a);
                int vb = e.VertBase;
                for (int k = 0; k < 7; k++) terrainColors[vb + k] = sc;
            }
            terrainMesh.colors32 = terrainColors;    // キャッシュ配列をそのまま書き込む
        }

        void BuildDecorations()
        {
            builder.Clear();
            foreach (var t in state.Map.AllTiles)
            {
                // 丘陵タイル上のデコレーション(内側ヘクス・森・資源)は持ち上げに追従する
                var c = t.Coord.ToWorld(); c.y = DecoY + RenderUtil.TileVisualHeight(t);

                if (t.HasFloodplain) AddFloodplain(c);

                if (t.Terrain == TerrainType.Mountain)
                {
                    AddMountain(c);
                }
                else
                {
                    if (t.HasHill) AddHill(c, t);
                    if (t.HasForest) AddForest(c, t.Coord);
                }

                if (t.HasRiver) AddRiver(t, c);

                if (t.Resource != ResourceType.None)
                    AddResource(c, t.Resource);
            }
            decoMesh = builder.Build(decoMesh);
            RenderUtil.NewMeshChild(transform, "Decorations", decoMesh, mat, Vector3.zero, SortDeco);
        }

        void AddRiver(Tile tile, Vector3 center)
        {
            var blue = new Color(0.18f, 0.64f, 0.88f, 0.96f);
            center.y += 0.004f;
            builder.AddHex(center, 0.13f, blue);

            Tile downstream = NaturalGeographySystem.RiverDestination(state.Map, tile);
            if (downstream == null || (!downstream.HasRiver && !downstream.IsWater)) return;

            Vector3 end = downstream.Coord.ToWorld();
            end.y = DecoY + RenderUtil.TileVisualHeight(downstream) + 0.004f;
            if (downstream.IsWater) end = Vector3.Lerp(center, end, 0.68f);
            Vector3 direction = end - center;
            if (direction.sqrMagnitude < 0.001f) return;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x).normalized * 0.065f;
            builder.AddQuad(center + side, end + side, end - side, center - side, blue);

            Vector3 tip = Vector3.Lerp(center, end, 0.74f);
            Vector3 tail = Vector3.Lerp(center, end, 0.56f);
            Color arrow = new Color(0.66f, 0.92f, 1f, 0.98f);
            builder.AddTriangle(tip, tail + side * 1.65f, tail - side * 1.65f, arrow);
        }

        void AddFloodplain(Vector3 center)
        {
            center.y += 0.002f;
            Color silt = new Color(0.48f, 0.68f, 0.28f, 0.48f);
            Color wet = new Color(0.30f, 0.58f, 0.30f, 0.62f);
            builder.AddQuad(
                center + new Vector3(-0.58f, 0f, -0.34f),
                center + new Vector3(0.58f, 0f, -0.34f),
                center + new Vector3(0.52f, 0f, -0.12f),
                center + new Vector3(-0.52f, 0f, -0.12f), silt);
            builder.AddQuad(
                center + new Vector3(-0.52f, 0f, 0.10f),
                center + new Vector3(0.52f, 0f, 0.10f),
                center + new Vector3(0.42f, 0f, 0.29f),
                center + new Vector3(-0.42f, 0f, 0.29f), wet);
        }

        /// <summary>ターンの季節状態と建物状態から氾濫・肥沃地・橋梁を再構築する。</summary>
        void RebuildHydrologyOverlay()
        {
            if (hydrologyMesh == null || state == null || state.Map == null) return;
            builder.Clear();
            FloodStage stage = NaturalGeographySystem.FloodStageAt(state.TurnNumber);
            foreach (Tile tile in state.Map.AllTiles)
            {
                Vector3 center = tile.Coord.ToWorld();
                center.y = HydrologyY + RenderUtil.TileVisualHeight(tile);

                if (tile.HasFloodplain)
                {
                    if (stage == FloodStage.Inundated)
                    {
                        builder.AddHex(center, 0.78f, new Color(0.20f, 0.64f, 0.92f, 0.22f));
                        AddFloodWave(center, -0.24f);
                        AddFloodWave(center, 0.18f);
                    }
                    else if (stage == FloodStage.Fertile)
                    {
                        builder.AddHex(center, 0.72f, new Color(0.62f, 0.78f, 0.24f, 0.19f));
                        Color silt = new Color(0.78f, 0.64f, 0.22f, 0.34f);
                        builder.AddQuad(
                            center + new Vector3(-0.48f, 0f, -0.05f),
                            center + new Vector3(0.48f, 0f, -0.05f),
                            center + new Vector3(0.42f, 0f, 0.05f),
                            center + new Vector3(-0.42f, 0f, 0.05f), silt);
                    }
                }

                if (!tile.HasRiver) continue;
                Player bridgeOwner = NaturalGeographySystem.BridgeOwnerAt(state, tile.Coord);
                if (bridgeOwner != null) AddBridge(tile, center, bridgeOwner.Color);
            }
            hydrologyMesh = builder.Build(hydrologyMesh);
        }

        void AddFloodWave(Vector3 center, float z)
        {
            Color wave = new Color(0.66f, 0.90f, 1f, 0.48f);
            builder.AddQuad(
                center + new Vector3(-0.45f, 0f, z - 0.025f),
                center + new Vector3(0.30f, 0f, z - 0.025f),
                center + new Vector3(0.36f, 0f, z + 0.025f),
                center + new Vector3(-0.39f, 0f, z + 0.025f), wave);
        }

        /// <summary>河道の流向に直交する橋桁と文明色の中央帯を描く。</summary>
        void AddBridge(Tile tile, Vector3 center, Color ownerColor)
        {
            Tile downstream = NaturalGeographySystem.RiverDestination(state.Map, tile);
            Vector3 flow;
            if (downstream != null)
                flow = downstream.Coord.ToWorld() - center;
            else if (tile.RiverOutflowDirection >= 0 && tile.RiverOutflowDirection < 6)
                flow = tile.Coord.Neighbor(tile.RiverOutflowDirection).ToWorld() - center;
            else
                flow = Vector3.forward;
            flow.y = 0f;
            if (flow.sqrMagnitude < 0.001f) flow = Vector3.forward;
            flow.Normalize();
            Vector3 span = new Vector3(-flow.z, 0f, flow.x) * 0.43f;
            Vector3 width = flow * 0.105f;
            center.y += 0.003f;
            Color deck = new Color(0.82f, 0.70f, 0.48f, 0.98f);
            builder.AddQuad(center - span + width, center + span + width,
                center + span - width, center - span - width, deck);

            ownerColor.a = 0.92f;
            Vector3 stripe = flow * 0.025f;
            builder.AddQuad(center - span * 0.92f + stripe, center + span * 0.92f + stripe,
                center + span * 0.92f - stripe, center - span * 0.92f - stripe, ownerColor);
        }

        void AddHill(Vector3 c, Tile t)
        {
            var baseCol = t.Def.Color;
            var col = new Color(baseCol.r * 0.78f, baseCol.g * 0.78f, baseCol.b * 0.78f, 1f);
            builder.AddHex(c, 0.55f, col);
        }

        static readonly Vector2[] TreeOffsets =
        {
            new Vector2(-0.30f, -0.08f),
            new Vector2(0.24f, 0.20f),
            new Vector2(0.06f, -0.38f),
        };

        void AddForest(Vector3 c, HexCoord coord)
        {
            Color g1 = new Color(0.08f, 0.30f, 0.12f, 1f);
            Color g2 = new Color(0.11f, 0.36f, 0.15f, 1f);
            int count = 2 + (RenderUtil.HashInt(coord) & 1);   // 2〜3本
            for (int i = 0; i < count; i++)
            {
                var o = TreeOffsets[i];
                var col = (i % 2 == 0) ? g1 : g2;
                builder.AddTriangle(
                    c + new Vector3(o.x - 0.15f, 0f, o.y - 0.16f),
                    c + new Vector3(o.x, 0f, o.y + 0.22f),
                    c + new Vector3(o.x + 0.15f, 0f, o.y - 0.16f),
                    col);
            }
        }

        void AddMountain(Vector3 c)
        {
            Color m1 = new Color(0.36f, 0.34f, 0.34f, 1f);
            Color m2 = new Color(0.48f, 0.46f, 0.45f, 1f);
            Color snow = new Color(0.92f, 0.93f, 0.96f, 1f);
            // 主峰
            builder.AddTriangle(
                c + new Vector3(-0.52f, 0f, -0.42f),
                c + new Vector3(-0.06f, 0f, 0.52f),
                c + new Vector3(0.34f, 0f, -0.42f), m1);
            // 副峰
            builder.AddTriangle(
                c + new Vector3(0.10f, 0f, -0.42f),
                c + new Vector3(0.36f, 0f, 0.12f),
                c + new Vector3(0.62f, 0f, -0.42f), m2);
            // 冠雪
            builder.AddTriangle(
                c + new Vector3(-0.20f, 0f, 0.23f),
                c + new Vector3(-0.06f, 0f, 0.52f),
                c + new Vector3(0.08f, 0f, 0.23f), snow);
        }

        // ---- 資源の形状アイコン(2026-07-21 一律ひし形から置き換え。表示のみ) ----
        // 小麦=金色の縦棒3本 / 牛・鹿=茶色の角(三角形ペア) / 鉄=濃灰の十字 / 馬=蹄鉄の弧。
        // 色は ResourceDef.Color を使い、既存デコレーション層(DecoY)にフラットメッシュで
        // 構築する。呼び出し元の c が丘陵の持ち上げ高さを含むため、丘陵上でも追従する。
        void AddResource(Vector3 c, ResourceType res)
        {
            var def = GameRules.Resources[res];
            Vector3 p = c + new Vector3(0f, 0f, -0.55f);

            // 視認性のための暗い下敷き(従来の縁取りひし形の役割を引き継ぐ)
            builder.AddHex(p, 0.21f, new Color(0.05f, 0.05f, 0.05f, 0.80f));

            switch (res)
            {
                case ResourceType.Wheat:
                    // 金色の小さな縦棒3本(麦の穂)
                    for (int i = -1; i <= 1; i++)
                    {
                        float x = i * 0.095f;
                        builder.AddQuad(
                            p + new Vector3(x - 0.028f, 0f, 0.13f),
                            p + new Vector3(x + 0.028f, 0f, 0.13f),
                            p + new Vector3(x + 0.028f, 0f, -0.13f),
                            p + new Vector3(x - 0.028f, 0f, -0.13f),
                            def.Color);
                    }
                    break;

                case ResourceType.Cattle:
                case ResourceType.Deer:
                    // 茶色の三角形ペア(角)
                    builder.AddTriangle(
                        p + new Vector3(-0.155f, 0f, -0.11f),
                        p + new Vector3(-0.105f, 0f, 0.14f),
                        p + new Vector3(-0.025f, 0f, -0.05f),
                        def.Color);
                    builder.AddTriangle(
                        p + new Vector3(0.155f, 0f, -0.11f),
                        p + new Vector3(0.025f, 0f, -0.05f),
                        p + new Vector3(0.105f, 0f, 0.14f),
                        def.Color);
                    break;

                case ResourceType.Iron:
                    // 濃灰の十字
                    builder.AddQuad(
                        p + new Vector3(-0.038f, 0f, 0.15f),
                        p + new Vector3(0.038f, 0f, 0.15f),
                        p + new Vector3(0.038f, 0f, -0.15f),
                        p + new Vector3(-0.038f, 0f, -0.15f),
                        def.Color);
                    builder.AddQuad(
                        p + new Vector3(-0.15f, 0f, 0.038f),
                        p + new Vector3(0.15f, 0f, 0.038f),
                        p + new Vector3(0.15f, 0f, -0.038f),
                        p + new Vector3(-0.15f, 0f, -0.038f),
                        def.Color);
                    break;

                case ResourceType.Horses:
                    // 黄褐色の弧(蹄鉄。下側が開いた約250°のリング)
                    {
                        const int segs = 8;
                        const float aStart = (-55f) * Mathf.Deg2Rad;   // 開口部は下(-Z)側
                        const float aEnd = (235f) * Mathf.Deg2Rad;
                        const float rIn = 0.085f;
                        const float rOut = 0.15f;
                        for (int i = 0; i < segs; i++)
                        {
                            float a0 = Mathf.Lerp(aStart, aEnd, i / (float)segs);
                            float a1 = Mathf.Lerp(aStart, aEnd, (i + 1) / (float)segs);
                            Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                            Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                            builder.AddQuad(p + d0 * rOut, p + d1 * rOut, p + d1 * rIn, p + d0 * rIn, def.Color);
                        }
                    }
                    break;

                default:
                    // 未知の資源種別は従来どおりのひし形(将来の追加に対する安全策)
                    builder.AddDiamond(p, 0.16f, def.Color);
                    break;
            }
        }

        void BuildFog()
        {
            var list = new List<Tile>();
            foreach (var t in state.Map.AllTiles) list.Add(t);
            tileOrder = list.ToArray();

            builder.Clear();
            fogVertBase = new int[tileOrder.Length];
            fogVertCount = new int[tileOrder.Length];
            for (int i = 0; i < tileOrder.Length; i++)
            {
                var t = tileOrder[i];
                float h = RenderUtil.TileVisualHeight(t);
                fogVertBase[i] = builder.VertexCount;

                var c = t.Coord.ToWorld(); c.y = FogY + h;
                builder.AddHex(c, 1f, Color.black);

                // 丘陵タイルは霧にも側面スカートを張り、傾いたカメラから見たときに
                // 持ち上げた地形の側面が霧の隙間から覗かないようにする。
                if (h > 0f)
                {
                    var w = t.Coord.ToWorld();
                    for (int d = 0; d < 6; d++)
                    {
                        var n = state.Map.Get(t.Coord.Neighbor(d));
                        float nh = (n == null) ? 0f : RenderUtil.TileVisualHeight(n);
                        if (nh >= h) continue;

                        Vector3 ca = RenderUtil.Corners[RenderUtil.EdgeCornerA[d]];
                        Vector3 cb = RenderUtil.Corners[RenderUtil.EdgeCornerB[d]];
                        Vector3 topA = w + ca; topA.y = FogY + h;
                        Vector3 topB = w + cb; topB.y = FogY + h;
                        Vector3 botA = w + ca; botA.y = FogY + nh;
                        Vector3 botB = w + cb; botB.y = FogY + nh;
                        builder.AddQuad(topA, topB, botB, botA, Color.black);
                    }
                }

                fogVertCount[i] = builder.VertexCount - fogVertBase[i];
            }
            fogMesh = builder.Build(fogMesh);
            fogMesh.MarkDynamic();
            fogColors = new Color32[fogMesh.vertexCount];
            RenderUtil.NewMeshChild(transform, "Fog", fogMesh, mat, Vector3.zero, SortFog);
        }

        // ------------------------------------------------------------------
        // 動的更新
        // ------------------------------------------------------------------

        void UpdateFog()
        {
            var human = state.HumanPlayer;
            for (int i = 0; i < tileOrder.Length; i++)
            {
                Color32 col;
                if (human == null)
                    col = FogVisible;                                        // 観戦モード:全て可視
                else if (!human.Explored.Contains(tileOrder[i].Coord))
                    col = FogUnexplored;
                else if (!human.Visible.Contains(tileOrder[i].Coord))
                    col = FogExplored;
                else
                    col = FogVisible;

                // タイル別頂点範囲(平地=7頂点、丘陵はスカート分を含む)へ一括適用
                int b = fogVertBase[i];
                int n = fogVertCount[i];
                for (int k = 0; k < n; k++) fogColors[b + k] = col;
            }
            fogMesh.colors32 = fogColors;
        }

        /// <summary>
        /// 領土の面塗り(ごく薄い所有者色のウォッシュ)。所有タイルの地形頂点色へ
        /// 所有者色を TerritoryTint(約7%)だけ混ぜる。可視ルールは国境と同じで、
        /// 通常プレイでは未探索タイルを塗らない。毎回タイル基準色から計算し直すため
        /// 混合が累積することはない。スカート壁の頂点色は変更しない(面塗りは上面のみ)。
        /// 水面ゆらぎの基準色も塗り後の色へ更新し、ゆらぎが面塗りを打ち消さないようにする。
        /// キャッシュ済み配列のみを使い、呼び出しごとの割り当てはゼロ。
        /// </summary>
        void UpdateTerritoryTint()
        {
            if (terrainMesh == null || terrainColors == null || tileBaseCols == null) return;
            if (tileOrder == null || tileOrder.Length != tileBaseCols.Length) return;
            if (terrainMesh.vertexCount != terrainColors.Length) return;

            var human = state.HumanPlayer;
            for (int i = 0; i < tileOrder.Length; i++)
            {
                var t = tileOrder[i];
                Color32 col = tileBaseCols[i];

                if (t.OwnerPlayerId >= 0 &&
                    (human == null || human.Explored.Contains(t.Coord)))
                {
                    var owner = state.GetPlayer(t.OwnerPlayerId);
                    if (owner != null)
                    {
                        Color oc = owner.Color;
                        col = new Color32(
                            (byte)Mathf.Clamp((int)(col.r * (1f - TerritoryTint) + oc.r * 255f * TerritoryTint), 0, 255),
                            (byte)Mathf.Clamp((int)(col.g * (1f - TerritoryTint) + oc.g * 255f * TerritoryTint), 0, 255),
                            (byte)Mathf.Clamp((int)(col.b * (1f - TerritoryTint) + oc.b * 255f * TerritoryTint), 0, 255),
                            col.a);
                    }
                }

                int vb = i * 7;
                for (int k = 0; k < 7; k++) terrainColors[vb + k] = col;
            }

            // 水面ゆらぎの基準色を面塗り後の色へ同期(領有された沿岸タイルなど)
            if (waterEntries != null)
            {
                for (int i = 0; i < waterEntries.Length; i++)
                    waterEntries[i].BaseCol = terrainColors[waterEntries[i].VertBase];
            }

            terrainMesh.colors32 = terrainColors;
        }

        void RebuildBorders()
        {
            var human = state.HumanPlayer;
            builder.Clear();

            for (int i = 0; i < tileOrder.Length; i++)
            {
                var t = tileOrder[i];
                if (t.OwnerPlayerId < 0) continue;
                if (human != null && !human.Explored.Contains(t.Coord)) continue;

                var owner = state.GetPlayer(t.OwnerPlayerId);
                if (owner == null) continue;
                Color col = owner.Color;
                col.a = 0.85f;

                Vector3 c = t.Coord.ToWorld(); c.y = BorderY + RenderUtil.TileVisualHeight(t);
                for (int d = 0; d < 6; d++)
                {
                    var n = state.Map.Get(t.Coord.Neighbor(d));
                    if (n != null && n.OwnerPlayerId == t.OwnerPlayerId) continue;   // 同じ所有者 → 境界なし

                    Vector3 ca = RenderUtil.Corners[RenderUtil.EdgeCornerA[d]];
                    Vector3 cb = RenderUtil.Corners[RenderUtil.EdgeCornerB[d]];
                    builder.AddQuad(c + ca * 0.96f, c + cb * 0.96f, c + cb * 0.80f, c + ca * 0.80f, col);
                }
            }

            borderMesh = builder.Build(borderMesh);
        }
    }
}
