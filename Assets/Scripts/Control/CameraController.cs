using UnityEngine;
using UnityEngine.EventSystems;

namespace HexCiv.Control
{
    /// <summary>
    /// ゲームカメラの制御。約55度に傾けた見下ろし視点の透視投影カメラを
    /// WASD/矢印キーでパン、ホイールでズーム(高さ8〜35)、中ボタンドラッグでパンする。
    /// 左/右ボタンのドラッグパンも同じ「地面つかみ」方式で行う(閾値判定は InputController が
    /// 担い、本クラスは TryBeginDragPan / UpdateDragPan / EndDragPan を提供する。2026-07-21 追加)。
    /// 注視点はマップ境界(XZ平面のRect)にクランプされる。
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        const float PitchDeg = 55f;
        const float MinHeight = 8f;
        const float MaxHeight = 35f;

        Camera cam;
        Rect bounds;
        /// <summary>カメラが注視する地面上の点(y=0)。</summary>
        Vector3 focus;
        float height = 18f;
        bool initialized;

        bool dragging;
        Vector3 dragAnchor;

        // ---- 外部(InputController)からのドラッグパン(2026-07-21 Claude Code 追加) ----
        // 中ボタン(dragging/dragAnchor)とは独立して状態を持ち、左/右ボタンのドラッグパンに使う。
        bool externalDragging;
        Vector3 externalDragAnchor;

        // ---- カメラシェイク(2026-07-21 Claude Code 追加) ----
        // 都市占領などの演出用。注視点(focus)や高さには一切触れず、Apply() の最後に
        // ポストオフセットとして加算するだけなので、パン/ズーム/FocusOn とは競合しない。
        // 非スケール時間基準(観戦モードの Time.timeScale 変更の影響を受けない)。
        float shakeAmplitude;
        float shakeDuration;
        float shakeStartTime = float.NegativeInfinity;
        float shakeSeed;

        /// <summary>制御中のカメラ。Init 前は null。</summary>
        public Camera Cam => cam;

        /// <summary>
        /// 初期化。Camera.main を再利用し、無ければ新規作成する。
        /// worldBoundsXZ: 注視点の可動範囲(x→ワールドX, y→ワールドZ)。
        /// startFocus: 開始時の注視点。
        /// </summary>
        public void Init(Rect worldBoundsXZ, Vector3 startFocus)
        {
            bounds = worldBoundsXZ;

            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.07f, 0.13f);
            }

            cam.orthographic = false;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;

            focus = new Vector3(startFocus.x, 0f, startFocus.z);
            height = Mathf.Clamp(height, MinHeight, MaxHeight);
            ClampFocus();
            Apply();
            initialized = true;
        }

        /// <summary>
        /// 注視点を指定のワールド座標(XZ平面)へ即座に移動する(既存の境界クランプを適用)。
        /// 未行動ユニットへのフォーカス移動などに使う。Init 前は何もしない。
        /// (2026-07-20 Claude Code 追加)
        /// </summary>
        public void FocusOn(Vector3 worldPos)
        {
            if (!initialized || cam == null) return;
            focus = new Vector3(worldPos.x, 0f, worldPos.z);
            ClampFocus();
            Apply();
        }

        void Update()
        {
            if (!initialized || cam == null) return;

            HandleKeyboardPan();
            HandleZoom();
            HandleMiddleDrag();

            ClampFocus();
            Apply();
        }

        void HandleKeyboardPan()
        {
            float dx = 0f, dz = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dx -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dx += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dz += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dz -= 1f;
            if (dx == 0f && dz == 0f) return;

            // ズームアウトするほど速く動かす
            float speed = 2f + height * 1.1f;
            var dir = new Vector3(dx, 0f, dz).normalized;
            focus += dir * speed * Time.deltaTime;
        }

        void HandleZoom()
        {
            if (IsPointerOverUI()) return;
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.0001f) return;
            height = Mathf.Clamp(height - scroll * (4f + height * 0.5f), MinHeight, MaxHeight);
        }

        void HandleMiddleDrag()
        {
            if (Input.GetMouseButtonDown(2) && !IsPointerOverUI()
                && TryGetGroundPoint(Input.mousePosition, out var anchor))
            {
                dragging = true;
                dragAnchor = anchor;
            }

            if (dragging && Input.GetMouseButton(2))
                ApplyDragPanStep(Input.mousePosition, dragAnchor);

            if (Input.GetMouseButtonUp(2)) dragging = false;
        }

        /// <summary>
        /// 左/右ボタンドラッグによるパンを開始する(2026-07-21 Claude Code 追加。InputController が
        /// クリックとドラッグの閾値判定後に呼ぶ)。中ボタンと同じ「地面つかみ」方式:
        /// screenPos の真下にあるXZ平面(y=0)上の点を掴み、以後 UpdateDragPan で
        /// その点がカーソルの下に留まるようにカメラを動かす。
        /// 地面と交差しない(ほぼ水平・上向きの)視線や未初期化時は開始せず false を返す。
        /// </summary>
        public bool TryBeginDragPan(Vector3 screenPos)
        {
            if (!initialized || cam == null) return false;
            if (!TryGetGroundPoint(screenPos, out var anchor)) return false;
            externalDragging = true;
            externalDragAnchor = anchor;
            return true;
        }

        /// <summary>
        /// ドラッグパンの追従(2026-07-21 Claude Code 追加。ボタンを押している間、毎フレーム呼ぶ)。
        /// TryBeginDragPan 成功後のみ有効(それ以外は no-op)。
        /// </summary>
        public void UpdateDragPan(Vector3 screenPos)
        {
            if (!externalDragging || !initialized || cam == null) return;
            ApplyDragPanStep(screenPos, externalDragAnchor);
        }

        /// <summary>ドラッグパンの終了(2026-07-21 Claude Code 追加。ボタンを離した時に呼ぶ。未開始なら no-op)。</summary>
        public void EndDragPan()
        {
            externalDragging = false;
        }

        /// <summary>
        /// カメラを短時間揺らす(2026-07-21 Claude Code 追加。都市占領の演出などに使う)。
        /// amplitude: ワールド単位の最大振幅。duration: 秒(非スケール時間)。
        /// 減衰付きのパーリンノイズジッターを Apply() の最後にポストオフセットとして加えるだけ
        /// なので、注視点・高さ・各種パン/ズーム/FocusOn の状態には一切影響しない。
        /// シェイク中の再呼び出しは新しいシェイクで上書きする。非正の引数は no-op。
        /// Init 前に呼ばれても安全(パラメータを保存するのみで、描画は Init 後の Apply から)。
        /// </summary>
        public void Shake(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f) return;
            shakeAmplitude = amplitude;
            shakeDuration = duration;
            shakeStartTime = Time.unscaledTime;
            shakeSeed = (shakeSeed + 17.31f) % 256f;   // 毎回少し違う揺れ方にする
        }

        /// <summary>
        /// 現在のシェイクオフセット(シェイク中でなければ Vector3.zero)。
        /// 経過時間だけから決まる純関数的な計算のため、同一フレーム内で Apply() が
        /// 複数回呼ばれても(FocusOn・ドラッグパン等)同じオフセットになる。
        /// </summary>
        Vector3 CurrentShakeOffset()
        {
            float elapsed = Time.unscaledTime - shakeStartTime;
            if (elapsed < 0f || elapsed >= shakeDuration) return Vector3.zero;
            float envelope = shakeAmplitude * (1f - elapsed / shakeDuration);   // 線形減衰
            const float Frequency = 28f;   // 揺れの速さ(Hz相当)
            float t = elapsed * Frequency;
            float x = Mathf.PerlinNoise(shakeSeed, t) * 2f - 1f;
            float y = Mathf.PerlinNoise(shakeSeed + 71.7f, t) * 2f - 1f;
            // 画面上で自然に見えるよう、水平(ワールドX)+控えめな垂直(高さ)を揺らす
            return new Vector3(x * envelope, y * envelope * 0.6f, 0f);
        }

        /// <summary>
        /// 「掴んだ地点 anchor がカーソルの下に留まる」ようにカメラを1ステップ動かす
        /// (中ボタン・左/右ボタンドラッグ共通の数学。2026-07-21 Claude Code 抽出)。
        /// </summary>
        void ApplyDragPanStep(Vector3 screenPos, Vector3 anchor)
        {
            if (TryGetGroundPoint(screenPos, out var cur))
            {
                var delta = anchor - cur;
                delta.y = 0f;
                focus += delta;
                ClampFocus();
                Apply();
            }
        }

        /// <summary>スクリーン座標からXZ平面(y=0)上の点を求める。</summary>
        bool TryGetGroundPoint(Vector3 screenPos, out Vector3 world)
        {
            world = Vector3.zero;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(screenPos);
            if (ray.direction.y > -0.001f) return false;   // ほぼ水平・上向きは交差しない
            float t = -ray.origin.y / ray.direction.y;
            world = ray.origin + ray.direction * t;
            return true;
        }

        void ClampFocus()
        {
            focus.x = Mathf.Clamp(focus.x, bounds.xMin, bounds.xMax);
            focus.z = Mathf.Clamp(focus.z, bounds.yMin, bounds.yMax);
            focus.y = 0f;
        }

        /// <summary>注視点と高さからカメラのトランスフォームを更新する。
        /// シェイクオフセットは最後に加算するだけ(focus/height は変更しない。2026-07-21 追加)。</summary>
        void Apply()
        {
            float back = height / Mathf.Tan(PitchDeg * Mathf.Deg2Rad);
            cam.transform.rotation = Quaternion.Euler(PitchDeg, 0f, 0f);
            cam.transform.position = new Vector3(focus.x, height, focus.z - back)
                + CurrentShakeOffset();
        }

        static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
