using HexCiv.Core;
using UnityEngine;

namespace HexCiv.Render
{
    /// <summary>
    /// 雲の影+時間帯トーンのアンビエント演出(2026-07-21 Claude Code 追加。表示のみ)。
    /// MinimapPanel と同じ自己起動方式: RuntimeInitializeOnLoadMethod で自動生成され、
    /// GameBootstrap / MapRenderer を変更せず、TurnManager 構築時に Bind される
    /// CultureSystem.CurrentState を読み取り窓口として使う。新規開始・リスタート・ロードで
    /// GameState が差し替わる(またはマップ寸法が変わる)とマップ境界を読み直して再構築する。
    ///
    /// 機能1 雲の影: 5〜8個の大きく柔らかい暗色ブロブ(中心α0.07→縁α0の軟円2〜3枚重ね)が
    /// 約0.3ワールド単位/秒(個体差あり)でマップ上をゆっくり流れ、余白付き境界でラップする。
    /// 個数・形・配置・方向・速度は固定シード(12345)の専用 System.Random で決定的
    /// (state.Rng は一切使わない — シミュレーション結果に影響しない)。
    ///
    /// 機能2 時間帯トーン: 雲の下に敷く全マップの色調クアッド。非スケール時間で約240秒周期の
    /// 中立→微暖色(金、α0.04)→中立→微寒色(青、α0.05)→… を循環する。
    /// ポーズ・倍速に影響されない環境演出として常にゆっくり流れる。
    ///
    /// 描画順: MapRenderer(terrain0/deco1/border3/highlight4/fog8)と EntityRenderer
    /// (cityblock2/banner5/unit9+)の記載順に対し sortingOrder 4 を使う。地形・装飾・国境・
    /// 都市ブロックの上、都市バナー(5)・霧(8)・ユニット(9+)の下 — 霧より下に置くことで
    /// 未探索領域は黒いまま(意図どおり雲は霧に覆われる)。highlight と同値4だが、
    /// 同一 sortingOrder 内は renderQueue → 距離の順で解決されるため、
    /// トーン=3000(既定)/雲=3001 の renderQueue でハイライト(3000)より後、かつ
    /// トーン→雲の順に描かれる。α0.07 の淡い影がハイライトへ僅かに乗るだけで判読性は保たれる。
    /// 高さ y=0.5 は隆起丘陵(RenderUtil.HillHeight=0.14)+各地表レイヤーより上。
    /// Sprites/Default は ZWrite Off のため層は sortingOrder が決める。
    ///
    /// 毎フレーム処理は Transform の移動とマテリアル色の更新のみ(メッシュ再構築なし・
    /// アロケーションフリー)。更新は非スケール時間で最大約30回/秒に間引く。
    /// </summary>
    public sealed class CloudShadowRenderer : MonoBehaviour
    {
        // ---- 描画順・配置 ----
        /// <summary>雲・トーンの sortingOrder(クラス冒頭コメント参照。霧8より下が意図)。</summary>
        const int SortCloudLayer = 4;
        /// <summary>同一 sortingOrder 内で雲をトーン・ハイライト(3000)より後に描くための renderQueue。</summary>
        const int CloudRenderQueue = 3001;
        /// <summary>雲の高さ。隆起丘陵(0.14)や地表レイヤーより上(描画順自体は sortingOrder が決める)。</summary>
        const float CloudY = 0.5f;
        /// <summary>トーンクアッドの高さ(雲の少し下)。</summary>
        const float ToneY = 0.45f;
        /// <summary>マップ境界の外側余白。最大ブロブ半径(約6.3)より大きく、ラップの瞬間が見えない。</summary>
        const float BoundsMargin = 8f;

        // ---- 雲パラメータ(すべて表示専用の決定的値) ----
        /// <summary>演出専用の固定シード。state.Rng とは無関係。</summary>
        const int PresentationSeed = 12345;
        const int MinClouds = 5;
        const int MaxClouds = 8;
        /// <summary>ブロブ中心の影の濃さ(α)。縁へ向かって0へ減衰する。</summary>
        const float ShadowAlpha = 0.07f;
        /// <summary>漂流速度の範囲(ワールド単位/秒)。平均約0.3。</summary>
        const float MinDriftSpeed = 0.18f;
        const float MaxDriftSpeed = 0.42f;
        /// <summary>軟円の縁の分割数。</summary>
        const int CircleSegments = 20;

        // ---- 時間帯トーン ----
        /// <summary>1周期の長さ(秒、非スケール時間)。</summary>
        const float DayCycleSeconds = 240f;
        const float WarmPeakAlpha = 0.04f;
        const float CoolPeakAlpha = 0.05f;
        /// <summary>微暖色(金)。αはピーク値で、実際のαは正弦の重みで0↔ピークを往復する。</summary>
        static readonly Color WarmTone = new Color(1f, 0.82f, 0.50f, WarmPeakAlpha);
        /// <summary>微寒色(青)。</summary>
        static readonly Color CoolTone = new Color(0.55f, 0.70f, 1f, CoolPeakAlpha);

        /// <summary>更新の最小間隔(秒)。約30回/秒に制限(非スケール時間基準)。</summary>
        const float UpdateInterval = 1f / 30f;

        GameObject container;
        Material cloudMat;
        Material toneMat;
        Transform[] cloudTransforms;
        Vector3[] cloudVelocities;
        /// <summary>再構築・破棄時に解放するため生成メッシュを保持する。</summary>
        Mesh[] cloudMeshes;
        Mesh toneMesh;

        GameState shownState;
        int shownMapW = -1;
        int shownMapH = -1;
        float minX, maxX, minZ, maxZ;
        /// <summary>次に更新してよい時刻(Time.unscaledTime 基準)。</summary>
        float nextUpdateAt;
        /// <summary>前回更新時刻(移動距離の実測dt用)。</summary>
        float lastTickTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<CloudShadowRenderer>() != null) return;
            new GameObject("CloudShadowFX").AddComponent<CloudShadowRenderer>();
        }

        void Update()
        {
            var state = CultureSystem.CurrentState;
            if (state == null || state.Map == null)
            {
                if (container != null && container.activeSelf) container.SetActive(false);
                return;
            }

            // 軽量演出モード(2026-07-22 追加): 雲影と時間帯トーンをまとめて非表示にする。
            // 標準へ戻せば下の SetActive(true) で再表示される(dt は既存の clamp で跳ばない)。
            if (VisualQuality.LightMode)
            {
                if (container != null && container.activeSelf) container.SetActive(false);
                return;
            }

            // 新規開始・リスタート・ロードによる状態差し替え、またはマップ寸法変更で再構築
            if (state != shownState || state.Map.Width != shownMapW || state.Map.Height != shownMapH)
                Rebuild(state);
            if (!container.activeSelf) container.SetActive(true);

            float now = Time.unscaledTime;
            if (now < nextUpdateAt) return;
            nextUpdateAt = now + UpdateInterval;
            float dt = Mathf.Min(now - lastTickTime, 0.25f);   // ヒッチ時の跳びを抑制
            lastTickTime = now;

            MoveClouds(dt);
            UpdateTone(now);
        }

        void OnDestroy()
        {
            DestroyBuiltMeshes();
            if (cloudMat != null) Destroy(cloudMat);
            if (toneMat != null) Destroy(toneMat);
        }

        // ==================================================================
        // 構築(新規開始・リスタート・ロード時のみ。毎フレームでは呼ばれない)
        // ==================================================================

        void Rebuild(GameState state)
        {
            shownState = state;
            shownMapW = state.Map.Width;
            shownMapH = state.Map.Height;

            // マップのワールド境界(タイル中心 x=√3(col+0.5(row&1)), z=1.5row。端のヘクス幅を少し余分に)
            minX = -HexCoord.Sqrt3 * 0.5f;
            maxX = HexCoord.Sqrt3 * (shownMapW + 0.5f);
            minZ = -1f;
            maxZ = 1.5f * shownMapH + 1f;

            if (container == null)
            {
                container = new GameObject("CloudShadowLayer");
                container.transform.SetParent(transform, false);
            }
            if (cloudMat == null)
            {
                cloudMat = RenderUtil.NewSpriteMaterial();
                cloudMat.renderQueue = CloudRenderQueue;
            }
            if (toneMat == null)
            {
                toneMat = RenderUtil.NewSpriteMaterial();
                toneMat.color = new Color(1f, 1f, 1f, 0f);
            }

            // 以前の雲・トーンを破棄して作り直す(同寸マップの再開でも決定的に同じ配置へ戻る)
            DestroyBuiltMeshes();
            for (int i = container.transform.childCount - 1; i >= 0; i--)
                Destroy(container.transform.GetChild(i).gameObject);

            var rng = new System.Random(PresentationSeed);

            // 機能2: 時間帯トーンの全マップクアッド(雲の下)。頂点色は白、色相・αはマテリアル色で動かす
            toneMesh = RenderUtil.BuildQuadXZ(
                (maxX - minX) + BoundsMargin * 2f,
                (maxZ - minZ) + BoundsMargin * 2f,
                Color.white, false);
            RenderUtil.NewMeshChild(container.transform, "TimeOfDayTone", toneMesh, toneMat,
                new Vector3((minX + maxX) * 0.5f, ToneY, (minZ + maxZ) * 0.5f), SortCloudLayer);

            // 機能1: 雲の影ブロブ
            int count = MinClouds + rng.Next(MaxClouds - MinClouds + 1);
            cloudTransforms = new Transform[count];
            cloudVelocities = new Vector3[count];
            cloudMeshes = new Mesh[count];
            for (int i = 0; i < count; i++)
            {
                cloudMeshes[i] = BuildCloudMesh(rng);
                float x = Mathf.Lerp(minX - BoundsMargin, maxX + BoundsMargin, (float)rng.NextDouble());
                float z = Mathf.Lerp(minZ - BoundsMargin, maxZ + BoundsMargin, (float)rng.NextDouble());
                var mr = RenderUtil.NewMeshChild(container.transform, "CloudShadow" + i,
                    cloudMeshes[i], cloudMat, new Vector3(x, CloudY, z), SortCloudLayer);
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float speed = Mathf.Lerp(MinDriftSpeed, MaxDriftSpeed, (float)rng.NextDouble());
                cloudTransforms[i] = mr.transform;
                cloudVelocities[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * speed;
            }

            lastTickTime = Time.unscaledTime;
            nextUpdateAt = 0f;
        }

        /// <summary>
        /// 中心α→縁α0へ減衰する軟らかい円盤を2〜3枚重ねた雲ブロブのメッシュを作る。
        /// 濃淡は頂点カラーのみで表現(Sprites/Default・UVなし)。構築は再構築時の一度だけ。
        /// </summary>
        static Mesh BuildCloudMesh(System.Random rng)
        {
            int circles = 2 + rng.Next(2);   // 2〜3枚
            var verts = new Vector3[circles * (CircleSegments + 1)];
            var colors = new Color32[verts.Length];
            var tris = new int[circles * CircleSegments * 3];
            var centerColor = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(ShadowAlpha * 255f));
            var rimColor = new Color32(0, 0, 0, 0);

            int v = 0, t = 0;
            for (int c = 0; c < circles; c++)
            {
                float ox = ((float)rng.NextDouble() * 2f - 1f) * 1.8f;
                float oz = ((float)rng.NextDouble() * 2f - 1f) * 1.8f;
                float radius = Mathf.Lerp(2.6f, 4.4f, (float)rng.NextDouble());

                int center = v;
                verts[v] = new Vector3(ox, 0f, oz);
                colors[v] = centerColor;
                v++;
                for (int i = 0; i < CircleSegments; i++)
                {
                    float a = Mathf.PI * 2f * i / CircleSegments;
                    verts[v] = new Vector3(ox + Mathf.Cos(a) * radius, 0f, oz + Mathf.Sin(a) * radius);
                    colors[v] = rimColor;
                    v++;
                }
                // 巻き順は RenderUtil.MeshBuilder.AddHex と同じ(Sprites/Default は Cull Off のため両面可視)
                for (int i = 0; i < CircleSegments; i++)
                {
                    tris[t++] = center;
                    tris[t++] = center + 1 + (i + 1) % CircleSegments;
                    tris[t++] = center + 1 + i;
                }
            }

            var mesh = new Mesh();
            mesh.vertices = verts;
            mesh.colors32 = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>生成済みメッシュを解放する(GameObject 側の破棄は呼び出し元が行う)。</summary>
        void DestroyBuiltMeshes()
        {
            if (cloudMeshes != null)
            {
                for (int i = 0; i < cloudMeshes.Length; i++)
                    if (cloudMeshes[i] != null) Destroy(cloudMeshes[i]);
                cloudMeshes = null;
            }
            if (toneMesh != null)
            {
                Destroy(toneMesh);
                toneMesh = null;
            }
            cloudTransforms = null;
            cloudVelocities = null;
        }

        // ==================================================================
        // 毎フレーム更新(最大約30回/秒・アロケーションフリー)
        // ==================================================================

        /// <summary>雲を等速で流し、余白付き境界を越えたら反対側へラップする。</summary>
        void MoveClouds(float dt)
        {
            if (cloudTransforms == null) return;
            float loX = minX - BoundsMargin;
            float hiX = maxX + BoundsMargin;
            float loZ = minZ - BoundsMargin;
            float hiZ = maxZ + BoundsMargin;
            for (int i = 0; i < cloudTransforms.Length; i++)
            {
                var tr = cloudTransforms[i];
                if (tr == null) continue;
                Vector3 p = tr.localPosition + cloudVelocities[i] * dt;
                if (p.x > hiX) p.x = loX;
                else if (p.x < loX) p.x = hiX;
                if (p.z > hiZ) p.z = loZ;
                else if (p.z < loZ) p.z = hiZ;
                tr.localPosition = p;
            }
        }

        /// <summary>
        /// 約240秒周期(非スケール時間)で 中立→微暖色→中立→微寒色→… を循環する。
        /// αを正弦の重みで0↔ピークへ滑らかに往復させ、色相の切替は必ずα0の瞬間に行うため
        /// 不連続は見えない。マテリアル色の更新のみでメッシュは触らない。
        /// </summary>
        void UpdateTone(float now)
        {
            if (toneMat == null) return;
            float t = Mathf.Repeat(now, DayCycleSeconds) / DayCycleSeconds;
            Color c;
            if (t < 0.5f)
            {
                c = WarmTone;
                c.a = WarmPeakAlpha * Mathf.Sin(Mathf.PI * (t * 2f));
            }
            else
            {
                c = CoolTone;
                c.a = CoolPeakAlpha * Mathf.Sin(Mathf.PI * ((t - 0.5f) * 2f));
            }
            toneMat.color = c;
        }
    }
}
