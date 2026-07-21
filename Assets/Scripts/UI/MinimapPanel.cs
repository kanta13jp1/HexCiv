using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HexCiv.Core;
using HexCiv.Control;

namespace HexCiv.UI
{
    /// <summary>
    /// 画面右下のミニマップ独立UI(2026-07-21 Claude Code 追加)。
    /// Codexのパネル群(CulturePanel/LegacyPanel等)と同じ独立Canvas方式で、
    /// UIManager/GameBootstrapを変更せず、TurnManager構築時にBindされる
    /// CultureSystem.CurrentState を読み取り窓口として使う。そのため新規開始・
    /// リスタート・ロード・文明変更・観戦モードのすべての経路で自動的に追従する。
    ///
    /// 描画: マップ全体をTexture2Dへ描く(1タイルあたり約2×2px)。地形の基本色、
    /// 領土(所有者色の控えめなタイル色合成)、都市ドット(明るいプレイヤー色2×2)、
    /// ユニットドット(プレイヤー色1px)、人間プレイヤーの霧(未探索=ほぼ黒、
    /// 探索済み未視界=減光。観戦=霧なし)を反映する。
    /// 更新は state.Version 変化時のみ、かつ最大約5回/秒(非スケール時間)。
    /// テクスチャとColor32バッファは再利用し、初回生成後はアロケーションフリー。
    ///
    /// 視界枠(2026-07-21 追加): メインカメラの視錐台4隅の視線を地面(y=0)へ投影した
    /// 領域を細い白枠として重ね描きする。state.Version が変わらなくてもカメラの
    /// 位置・回転・ズームを約10回/秒の軽量チェックで検知し、地形バッファはそのままに
    /// 枠のみを再合成する(全タイル再描画なし・アロケーションフリー)。
    ///
    /// 操作: クリックでその地点へカメラをフォーカス(CameraController.FocusOn。
    /// 見つからない場合は Camera.main の平行移動でフォールバック)。
    /// Mキーで表示/非表示を切替(InputField入力中は無視)。
    /// </summary>
    public sealed class MinimapPanel : MonoBehaviour
    {
        /// <summary>ミニマップ画像の横幅(px)。高さはマップ縦横比から決まる。</summary>
        const float ImageWidth = 220f;
        /// <summary>枠の内側余白(px)。</summary>
        const float FramePadding = 4f;
        /// <summary>テクスチャ更新の最小間隔(秒)。約5回/秒に制限する(非スケール時間基準)。</summary>
        const float RefreshInterval = 0.2f;
        /// <summary>カメラ移動チェックの間隔(秒)。約10回/秒(非スケール時間基準。2026-07-21 追加)。</summary>
        const float CameraCheckInterval = 0.1f;
        /// <summary>地面(y=0)と交わらない視線の距離キャップ(カメラのfarクリップと同値)。</summary>
        const float MaxViewRayDistance = 300f;

        /// <summary>マップ外・未使用ピクセルの背景色(暗い紺。カメラ背景と揃える)。</summary>
        static readonly Color32 BackgroundColor = new Color32(10, 14, 26, 255);
        /// <summary>未探索タイルの色(MapRendererの未探索霧とほぼ同じ、ほぼ黒)。</summary>
        static readonly Color32 UnexploredColor = new Color32(5, 8, 15, 255);
        /// <summary>カメラ視界枠の色(細い白。2026-07-21 追加)。</summary>
        static readonly Color32 ViewRectColor = new Color32(255, 255, 255, 255);
        /// <summary>視錐台4隅のビューポート座標(外周を一筆書きできる順序)。</summary>
        static readonly Vector3[] ViewportCorners =
        {
            new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f), new Vector3(0f, 1f, 0f)
        };

        Canvas canvas;
        GameObject panel;
        RawImage mapImage;
        RectTransform mapImageRect;
        Texture2D texture;
        Color32[] buffer;
        /// <summary>視界枠合成用バッファ(地形buffer+白枠。テクスチャ転送はこちらから行う。2026-07-21 追加)。</summary>
        Color32[] composeBuffer;
        int texW, texH;
        int mapW, mapH;

        GameState shownState;
        int shownVersion = -1;
        /// <summary>Version変化を検知済みで、次の許可時刻に再描画すべきか。</summary>
        bool pendingRefresh;
        /// <summary>次に再描画してよい時刻(Time.unscaledTime 基準。観戦の timeScale に影響されない)。</summary>
        float nextRefreshAllowedAt;

        CameraController cameraController;

        // ---- カメラ視界枠(2026-07-21 Claude Code 追加) ----
        Camera mainCam;
        /// <summary>前回の視界枠描画時のカメラ位置・回転・ズーム(移動検知用スナップショット)。</summary>
        Vector3 lastCamPos;
        Quaternion lastCamRot;
        float lastCamZoom;
        /// <summary>次にカメラ移動チェックをしてよい時刻(Time.unscaledTime 基準)。</summary>
        float nextCameraCheckAt;
        /// <summary>視界4隅のミニマップピクセル座標(毎回再利用しアロケーションを避ける)。</summary>
        readonly Vector2[] viewCornersPx = new Vector2[4];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<MinimapPanel>() != null) return;
            new GameObject("MinimapUI").AddComponent<MinimapPanel>();
        }

        void Start()
        {
            BuildCanvas();
            BuildPanel();
        }

        void Update()
        {
            // Mキーで表示切替(シード入力欄などのInputFieldへ入力中は無視)
            if (panel != null && Input.GetKeyDown(KeyCode.M) && !IsTextInputFocused())
                panel.SetActive(!panel.activeSelf);

            if (panel == null || !panel.activeSelf) return;

            var current = CultureSystem.CurrentState;
            if (current == null || current.Map == null) return;

            bool stateChanged = current != shownState;
            if (stateChanged || current.Version != shownVersion) pendingRefresh = true;
            // 状態オブジェクト自体の差し替え(新規/リスタート/ロード)は即時反映、
            // それ以外のVersion変化は最大約5回/秒に間引く
            if (pendingRefresh && (stateChanged || Time.unscaledTime >= nextRefreshAllowedAt))
            {
                RenderMinimap(current);
            }
            else if (texture != null && Time.unscaledTime >= nextCameraCheckAt)
            {
                // カメラ視界枠のみの更新(2026-07-21 追加): state.Version が変わらなくても
                // カメラの移動・回転・ズームを約10回/秒の軽量チェックで検知し、
                // 地形バッファはそのままに白枠だけを再合成する
                nextCameraCheckAt = Time.unscaledTime + CameraCheckInterval;
                if (CameraViewChanged()) ComposeAndUpload();
            }
        }

        void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }

        // ==================================================================
        // UI構築
        // ==================================================================

        void BuildCanvas()
        {
            var go = new GameObject("MinimapCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // UIManagerのメインCanvas(100)より下に置く: 都市パネル・チュートリアル・
            // ゲームオーバー等の大きなパネルが開いた時はそちらを優先して覆わせる
            canvas.sortingOrder = 90;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("MinimapEventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(transform, false);
            }
        }

        void BuildPanel()
        {
            // 右下隅、「？ 遊び方」ボタン(y14〜60)の一段上。高さはテクスチャ生成時に確定する
            panel = UIStyle.CreatePanel(canvas.transform, "MinimapPanel",
                new Color(0.05f, 0.065f, 0.10f, 0.92f));
            UIStyle.SetRect(panel, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-14f, 66f),
                new Vector2(ImageWidth + FramePadding * 2f, 60f));

            var imgGo = new GameObject("MinimapImage", typeof(RectTransform), typeof(RawImage));
            imgGo.transform.SetParent(panel.transform, false);
            mapImage = imgGo.GetComponent<RawImage>();
            mapImage.color = Color.white;
            mapImage.raycastTarget = true;   // クリックでカメラフォーカスするため
            mapImageRect = UIStyle.SetRect(imgGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ImageWidth, 52f));

            var catcher = imgGo.AddComponent<MinimapClickCatcher>();
            catcher.Clicked = OnMinimapClick;
        }

        // ==================================================================
        // テクスチャ描画
        // ==================================================================

        /// <summary>
        /// マップサイズに合わせてテクスチャとバッファを(必要な時のみ)作り直す。
        /// 同サイズでのリスタート・ロードでは既存の資源を再利用する(アロケーションフリー)。
        /// odd-rオフセットの奇数行を1px右へずらすため、横幅は Width*2+1。
        /// </summary>
        void EnsureTexture(int width, int height)
        {
            if (texture != null && mapW == width && mapH == height) return;
            mapW = width;
            mapH = height;
            texW = width * 2 + 1;
            texH = height * 2;
            if (texture != null) Destroy(texture);
            texture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            buffer = new Color32[texW * texH];
            for (int i = 0; i < buffer.Length; i++) buffer[i] = BackgroundColor;
            // 視界枠合成用バッファ(2026-07-21 追加)。転送前に毎回bufferから全コピーするため初期化不要
            composeBuffer = new Color32[texW * texH];
            mapImage.texture = texture;

            // マップの縦横比から表示サイズを決める(横220px固定)
            float imageH = Mathf.Round(ImageWidth * texH / (float)texW);
            mapImageRect.sizeDelta = new Vector2(ImageWidth, imageH);
            ((RectTransform)panel.transform).sizeDelta =
                new Vector2(ImageWidth + FramePadding * 2f, imageH + FramePadding * 2f);
        }

        /// <summary>タイル1枚分(2×2px)を塗る。</summary>
        void PaintTile(int col, int row, Color32 c)
        {
            int px = col * 2 + (row & 1);
            int i = row * 2 * texW + px;
            buffer[i] = c;
            buffer[i + 1] = c;
            i += texW;
            buffer[i] = c;
            buffer[i + 1] = c;
        }

        /// <summary>ユニットドット(1px)。都市ブロックの上にも見えるよう最後に描く。</summary>
        void PaintUnitDot(int col, int row, Color32 c)
        {
            int px = col * 2 + (row & 1);
            buffer[(row * 2 + 1) * texW + px] = c;
        }

        void RenderMinimap(GameState state)
        {
            var map = state.Map;
            EnsureTexture(map.Width, map.Height);
            shownState = state;
            shownVersion = state.Version;
            pendingRefresh = false;
            nextRefreshAllowedAt = Time.unscaledTime + RefreshInterval;

            var viewer = state.HumanPlayer;   // null = 観戦モード(霧なし全表示)

            // 1) 地形+領土タント+霧
            for (int row = 0; row < mapH; row++)
            {
                for (int col = 0; col < mapW; col++)
                {
                    var coord = HexCoord.FromOffset(col, row);
                    var tile = map.Get(coord);
                    if (tile == null) continue;

                    Color32 c;
                    if (viewer != null && !viewer.Explored.Contains(coord))
                    {
                        c = UnexploredColor;
                    }
                    else
                    {
                        Color baseColor = tile.Def.Color;
                        if (tile.HasForest)
                            baseColor = Color.Lerp(baseColor, new Color(0.10f, 0.30f, 0.12f), 0.45f);
                        if (tile.HasHill) baseColor *= 0.88f;
                        // 領土: 所有者色を控えめに合成(境界線の代わりにタイル全体を淡く着色)
                        if (tile.OwnerPlayerId >= 0)
                        {
                            var owner = state.GetPlayer(tile.OwnerPlayerId);
                            if (owner != null) baseColor = Color.Lerp(baseColor, owner.Color, 0.35f);
                        }
                        // 探索済みだが視界外は減光
                        if (viewer != null && !viewer.Visible.Contains(coord)) baseColor *= 0.55f;
                        baseColor.a = 1f;
                        c = baseColor;
                    }
                    PaintTile(col, row, c);
                }
            }

            // 2) 都市(2×2、明るいプレイヤー色。探索済みなら表示 = 本編のゴースト都市と同じ規約)
            for (int pi = 0; pi < state.Players.Count; pi++)
            {
                var p = state.Players[pi];
                for (int ci = 0; ci < p.Cities.Count; ci++)
                {
                    var city = p.Cities[ci];
                    if (viewer != null && !viewer.Explored.Contains(city.Coord)) continue;
                    Color bright = Color.Lerp(p.Color, Color.white, 0.45f);
                    bright.a = 1f;
                    int col, row;
                    city.Coord.ToOffset(out col, out row);
                    PaintTile(col, row, bright);
                }
            }

            // 3) ユニット(1px、プレイヤー色。視界内のみ)
            for (int pi = 0; pi < state.Players.Count; pi++)
            {
                var p = state.Players[pi];
                for (int ui = 0; ui < p.Units.Count; ui++)
                {
                    var unit = p.Units[ui];
                    if (viewer != null && !viewer.Visible.Contains(unit.Coord)) continue;
                    Color solid = p.Color;
                    solid.a = 1f;
                    int col, row;
                    unit.Coord.ToOffset(out col, out row);
                    PaintUnitDot(col, row, solid);
                }
            }

            ComposeAndUpload();
        }

        // ==================================================================
        // カメラ視界枠(2026-07-21 Claude Code 追加)
        // ==================================================================

        /// <summary>
        /// 地形バッファ(buffer)を合成バッファへコピーし、カメラ視界枠を上書きしてから
        /// テクスチャへ転送する。バッファは再利用するためアロケーションフリー。
        /// </summary>
        void ComposeAndUpload()
        {
            System.Array.Copy(buffer, composeBuffer, buffer.Length);
            DrawCameraViewRect();
            texture.SetPixels32(composeBuffer);
            texture.Apply(false);
        }

        /// <summary>
        /// メインカメラの視錐台4隅の視線を地面(y=0)へ落とし、その交点をミニマップの
        /// ピクセル座標へ写像して細い白枠として描く。地面と交わらない視線(ほぼ水平以上)は
        /// 遠方距離でキャップする。描画のたびにカメラ状態のスナップショットも更新する。
        /// </summary>
        void DrawCameraViewRect()
        {
            var cam = MainCamera();
            if (cam == null) return;
            var tr = cam.transform;
            lastCamPos = tr.position;
            lastCamRot = tr.rotation;
            lastCamZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;

            for (int i = 0; i < 4; i++)
            {
                var ray = cam.ViewportPointToRay(ViewportCorners[i]);
                float t = MaxViewRayDistance;
                if (ray.direction.y < -0.0001f)
                    t = Mathf.Min(-ray.origin.y / ray.direction.y, MaxViewRayDistance);
                Vector3 w = ray.origin + ray.direction * t;
                // ワールド→ピクセルの線形写像: タイル中心 x=√3(col+0.5(row&1)), z=1.5row が
                // PaintTileの2×2ブロック中心 (col*2+(row&1)+1, row*2+1) に一致する
                viewCornersPx[i] = new Vector2(
                    2f * w.x / HexCoord.Sqrt3 + 1f,
                    2f * w.z / 1.5f + 1f);
            }
            for (int i = 0; i < 4; i++)
                DrawViewRectLine(viewCornersPx[i], viewCornersPx[(i + 1) & 3]);
        }

        /// <summary>合成バッファへ幅1pxの線分を描く(テクスチャ範囲外のピクセルはスキップ)。</summary>
        void DrawViewRectLine(Vector2 a, Vector2 b)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)));
            if (steps < 1) steps = 1;
            if (steps > 4096) steps = 4096;   // 異常な遠方座標でも有限時間で終える安全上限
            float inv = 1f / steps;
            for (int i = 0; i <= steps; i++)
            {
                int x = Mathf.RoundToInt(a.x + dx * (i * inv));
                int y = Mathf.RoundToInt(a.y + dy * (i * inv));
                if (x < 0 || x >= texW || y < 0 || y >= texH) continue;
                composeBuffer[y * texW + x] = ViewRectColor;
            }
        }

        /// <summary>前回の視界枠描画時からカメラの位置・回転・ズームが動いたか(軽量比較のみ)。</summary>
        bool CameraViewChanged()
        {
            var cam = MainCamera();
            if (cam == null) return false;
            var tr = cam.transform;
            if ((tr.position - lastCamPos).sqrMagnitude > 0.0004f) return true;
            if (Quaternion.Angle(tr.rotation, lastCamRot) > 0.05f) return true;
            float zoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
            return Mathf.Abs(zoom - lastCamZoom) > 0.01f;
        }

        /// <summary>メインカメラをキャッシュ付きで取得する(破棄されていたら取り直す)。</summary>
        Camera MainCamera()
        {
            if (mainCam == null) mainCam = Camera.main;
            return mainCam;
        }

        // ==================================================================
        // クリック → カメラフォーカス
        // ==================================================================

        void OnMinimapClick(PointerEventData eventData)
        {
            if (texture == null || mapImageRect == null) return;
            var state = shownState != null ? shownState : CultureSystem.CurrentState;
            if (state == null || state.Map == null) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mapImageRect, eventData.position, eventData.pressEventCamera, out local)) return;
            var r = mapImageRect.rect;
            if (r.width <= 0f || r.height <= 0f) return;

            // 画像内の正規化座標 → テクスチャpx → タイル(odd-rオフセット) → ワールド座標
            float u = Mathf.Clamp01((local.x - r.xMin) / r.width);
            float v = Mathf.Clamp01((local.y - r.yMin) / r.height);
            int row = Mathf.Clamp((int)(v * texH) / 2, 0, state.Map.Height - 1);
            int col = Mathf.Clamp(((int)(u * texW) - (row & 1)) / 2, 0, state.Map.Width - 1);
            FocusCamera(HexCoord.FromOffset(col, row).ToWorld());
        }

        /// <summary>
        /// カメラを指定ワールド座標へフォーカスする。共有オブジェクトの取得は他パネルと同様に
        /// シーン検索で行う(CameraRig は GameBootstrap が一度だけ生成し以後保持されるため、
        /// 見つけた参照をキャッシュして良い)。見つからない場合は Camera.main を
        /// 「現在の注視点との差分だけ平行移動」でフォールバックする。
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

        /// <summary>InputField(ゲーム設定のシード入力欄など)へ入力中か。</summary>
        static bool IsTextInputFocused()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var selected = es.currentSelectedGameObject;
            if (selected == null) return false;
            var field = selected.GetComponent<InputField>();
            return field != null && field.isFocused;
        }

        /// <summary>RawImage上のクリックを親パネルへ中継する小さなコンポーネント。</summary>
        sealed class MinimapClickCatcher : MonoBehaviour, IPointerClickHandler
        {
            public System.Action<PointerEventData> Clicked;

            public void OnPointerClick(PointerEventData eventData)
            {
                if (Clicked != null) Clicked(eventData);
            }
        }
    }
}
